// Donjon de l'Oie — le marché de Valcreux en diorama, pour le mini-jeu de l'évadé : un socle de rue pavée, quatre
// maisons, trois étals et leurs marchands, une vingtaine d'habitants (dont deux ou trois sosies de l'évadé) et l'évadé
// lui-même, caché derrière des caisses, des tonneaux ou des sacs : seule sa tête dépasse. Il se bâtit une pièce par
// image pendant que la caméra zoome, puis surgit pièce après pièce (la vie du marché : Diorama.Vie.cs).
// Repère : origine au centre du sol, la rue le long de x, les maisons au fond (z < 0) ; la caméra le regarde de
// l'avant-droite (LacetVue), d'en haut (TangageVue).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Modeles;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Monde
{
    public sealed partial class Diorama : MonoBehaviour
    {
        public const float LacetVue = 28f, TangageVue = 38f;
        /// La direction de la caméra, à l'horizontale, dans le repère du diorama.
        public static readonly Vector3 VersCamera = new Vector3(Mathf.Sin(LacetVue * Mathf.Deg2Rad), 0f, Mathf.Cos(LacetVue * Mathf.Deg2Rad));

        public sealed class Habitant
        {
            public RigVue Rig; public Animateur Anim; public Allure Allure;
            public bool Evade, Sosie, Cache;
            public bool Marche; public Vector3 A, B; public float Vitesse, Phase;    // un passant va et vient de A à B
            public float Lacet;                                                     // orientation au repos (repère du diorama)
            public int Cachette = -1;
            /// Le milieu de la tête, dans le monde.
            public Vector3 Tete { get { return Rig.transform.TransformPoint(new Vector3(-0.025f, 0.99f, 0f)); } }
        }

        /// Une pièce qui surgira : 0 bondit, 1 pousse de bas en haut, 2 tombe du ciel.
        sealed class Piece { public Transform T; public Vector3 Echelle, Position; public float Delai; public int Genre; }

        public readonly List<Habitant> Foule = new List<Habitant>();
        public Habitant Evade { get; private set; }
        public bool Pret { get; private set; }
        public bool EnJeu;                                   // la recherche a commencé (les essais hors Unity le guettent)
        public static Diorama Actif { get; private set; }
        public Color32[] Portrait { get; private set; }
        readonly List<Piece> pieces = new List<Piece>();
        readonly List<Mesh> propres = new List<Mesh>();      // maillages des habitants, faits pour ce diorama seul
        System.Random r;
        Allure allure;

        // ------------------------------------------------------------------ l'agencement (mètres, repère du diorama)

        static readonly float[] MaisonsX = { -3.84f, -1.28f, 1.28f, 3.84f };
        const float MaisonsZ = -2.9f;
        static readonly float[] EtalsX = { -3.55f, 0f, 3.55f };
        const float EtalsZ = -1.0f;
        /// Les cachettes : où se tient celui qui se cache, l'abri posé entre lui et la caméra, la demi-épaisseur de l'abri.
        static readonly Vector3[] Cachettes = { new Vector3(-2.05f, 0, -1.25f), new Vector3(1.6f, 0, -1.25f), new Vector3(-4.3f, 0, 2.5f), new Vector3(0.05f, 0, 2.5f) };
        static readonly string[] Abris = { "caisses", "tonneaux", "sacs", "caisses" };
        static readonly float[] DemiAbris = { 0.3f, 0.3f, 0.28f, 0.3f };
        const float Recul = 0.3f;                                   // de l'abri au milieu du corps caché
        /// Celui qui se cache plie les genoux, selon l'abri et l'angle sous lequel la caméra voit la cachette : on voit
        /// son visage, à peu près du nez aux cheveux, et presque rien d'autre.
        static readonly float[] Accroupis = { 0.06f, 0.06f, 0.15f, 0.1f };
        static readonly Vector3 Puits = new Vector3(-1.9f, 0, 3.0f), Charrette = new Vector3(1.85f, 0, 3.0f), Avis = new Vector3(4.3f, 0, 3.05f);
        const float Allee1 = 0.7f, Allee2 = 1.65f;                 // les allées des passants

        public static Diorama Creer(Transform parent, Vector3 position, float lacet, int graine, Allure evade)
        {
            var go = new GameObject("Diorama du marché");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, lacet, 0);
            var d = go.AddComponent<Diorama>();
            d.r = new System.Random(graine * 13 + 5);
            d.allure = evade;
            Actif = d;
            return d;
        }

        void OnDestroy()
        {
            if (Actif == this) Actif = null;
            foreach (var m in propres) if (m != null) Destroy(m);
            propres.Clear();
        }

        /// Ce qui doit tenir dans l'image (repère du diorama) : la rue, des façades au bord avant du socle, et les étals
        /// jusqu'à leurs auvents ; les toits des maisons et le bas de la plinthe peuvent déborder.
        static readonly Vector3[] Emprise =
        {
            new Vector3(-5.3f, 0f, -1.9f), new Vector3(5.3f, 0f, -1.9f), new Vector3(-5.3f, 0f, 4.1f), new Vector3(5.3f, 0f, 4.1f),
            new Vector3(-5.3f, -0.45f, 4.1f), new Vector3(5.3f, -0.45f, 4.1f),
            new Vector3(-4.7f, 2.25f, -1.85f), new Vector3(4.7f, 2.25f, -1.85f), new Vector3(0f, 2.25f, -1.85f),
        };

        /// La caméra qui cadre le diorama : orientation fixe (LacetVue, TangageVue sur son repère) ; distance et cible
        /// cherchées pour que l'emprise tienne, perspective comprise, dans la fenêtre libre de l'écran : à droite de l'avis
        /// de recherche (« marge » : part de la largeur), sous le sablier (« haut ») et au-dessus de la consigne (« bas »).
        public static void Cadrage(Transform d, float champ, float aspect, float marge, float haut, float bas,
            out Vector3 cible, out float distance, out float lacet)
        {
            lacet = d.eulerAngles.y + 180f + LacetVue;
            var q = Quaternion.Euler(TangageVue, lacet, 0);
            Vector3 av = q * Vector3.forward, dr = q * Vector3.right, ht = q * Vector3.up;
            float ty = Mathf.Tan(champ * 0.5f * Mathf.Deg2Rad), tx = ty * aspect;
            float g = -1f + 2f * marge + 0.03f, dx = 0.97f, b = -1f + 2f * bas + 0.02f, hh = 1f - 2f * haut - 0.02f;
            var pts = new Vector3[Emprise.Length];
            cible = Vector3.zero;
            for (int i = 0; i < pts.Length; i++) { pts[i] = d.TransformPoint(Emprise[i]); cible += pts[i] / pts.Length; }
            distance = 40f;
            for (int it = 0; it < 14; it++)
            {
                var oeil = cible - av * distance;
                float x0 = 1e9f, x1 = -1e9f, y0 = 1e9f, y1 = -1e9f;
                foreach (var p in pts)
                {
                    var v = p - oeil;
                    float z = Mathf.Max(0.1f, Vector3.Dot(v, av));
                    float sx = Vector3.Dot(v, dr) / (z * tx), sy = Vector3.Dot(v, ht) / (z * ty);
                    x0 = Mathf.Min(x0, sx); x1 = Mathf.Max(x1, sx); y0 = Mathf.Min(y0, sy); y1 = Mathf.Max(y1, sy);
                }
                cible += dr * (((x0 + x1) - (g + dx)) * 0.5f * distance * tx) + ht * (((y0 + y1) - (b + hh)) * 0.5f * distance * ty);
                distance *= Mathf.Max((x1 - x0) / (dx - g), (y1 - y0) / (hh - b));
            }
        }

        // ------------------------------------------------------------------ la construction

        /// Bâtit le diorama, une pièce par image (« parImage » faux : tout d'un coup). Tout reste caché jusqu'à Apparaitre().
        public IEnumerator Batir(bool parImage)
        {
            Ajouter(RigVue.Creer(DecorsModeles.PlateauMarche(104, 80, -15), transform, "marche_plateau", true, 2.2f), Vector3.zero, 0f, 0f, 0);
            if (parImage) yield return null;
            int gm = r.Next(6);
            for (int i = 0; i < MaisonsX.Length; i++)
            {
                int g = 101 + (gm + i) % 6;
                Ajouter(RigVue.Creer(DecorsModeles.Maison(g, 12, false), transform, "marche_maison_" + g, true, 2.2f), new Vector3(MaisonsX[i], 0, MaisonsZ), 0f, 0.12f + 0.09f * i, 1);
                if (parImage) yield return null;
            }
            int ge = r.Next(5);
            for (int i = 0; i < EtalsX.Length; i++)
            {
                int g = 200 + (ge + i) % 5;
                Ajouter(RigVue.Creer(Catalogue.Decor("etal_marche", g), transform, "marche_etal_" + g, true, 2.2f), new Vector3(EtalsX[i], 0, EtalsZ), 0f, 0.5f + 0.08f * i, 1);
                if (parImage) yield return null;
            }
            Ajouter(RigVue.Creer(Catalogue.Decor("puits", 3), transform, "decor_puits_3", true, 2.2f), Puits, 15f, 0.78f, 1);
            Ajouter(RigVue.Creer(Catalogue.Decor("charrette_foin", 2), transform, "marche_charrette", true, 2.2f), Charrette, 90f, 0.84f, 0);
            for (int i = 0; i < Cachettes.Length; i++)
            {
                var pos = Cachettes[i] + VersCamera * (Recul + DemiAbris[i]);
                Ajouter(RigVue.Creer(Catalogue.Decor(Abris[i], 5 + i), transform, "marche_" + Abris[i] + "_" + (5 + i), true, 2.2f), pos, LacetVue, 0.9f + 0.05f * i, 0);
            }
            if (parImage) yield return null;
            // l'avis de recherche, tourné vers la caméra
            var ev = HerosModeles.Habitant(allure);
            Portrait = HerosModeles.Portrait(ev);
            var avis = RigVue.Creer(DecorsModeles.PanneauAvis(Portrait), transform, null, true, 2.2f);
            Propres(avis);
            Ajouter(avis, Avis, LacetVue - 8f, 1.06f, 1);
            if (parImage) yield return null;
            // la foule
            foreach (var s in Peupler(ev)) if (parImage && s) yield return null;
            Pret = true;
        }

        void Ajouter(RigVue rv, Vector3 pos, float lacet, float delai, int genre)
        {
            var t = rv.transform;
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, lacet, 0);
            pieces.Add(new Piece { T = t, Echelle = t.localScale, Position = pos, Delai = delai, Genre = genre });
            t.gameObject.SetActive(false);
        }

        void Propres(RigVue rv)
        {
            foreach (var r0 in rv.Rendus)
            {
                var mf = r0 != null ? r0.GetComponent<MeshFilter>() : null;
                if (mf != null && mf.sharedMesh != null) propres.Add(mf.sharedMesh);
            }
        }

        Habitant Placer(Allure a, VoxelRig rig, Vector3 pos, float lacet, float delai)
        {
            var rv = RigVue.Creer(rig ?? HerosModeles.Habitant(a), transform, null, true, 2.2f);
            rv.name = "Habitant";
            Propres(rv);
            var h = new Habitant { Rig = rv, Allure = a, Lacet = lacet };
            h.Anim = Animateur.Sur(rv);
            h.Anim.Vitesse = 0.75f + 0.5f * (float)r.NextDouble();
            Foule.Add(h);
            Ajouter(rv, pos, lacet, delai, 2);
            return h;
        }

        float Hasard(float a, float b) { return a + (b - a) * (float)r.NextDouble(); }

        /// Un visage qu'on ne confond pas avec celui de l'évadé : au moins trois pixels de différence sur l'avis, à partir
        /// de la ligne « depuis » (4 : du nez aux cheveux, ce qui dépasse d'une cachette).
        bool Distinct(VoxelRig rig, int depuis)
        {
            var p = HerosModeles.Portrait(rig);
            int n = 0;
            for (int i = depuis * 11; i < p.Length; i++)
            {
                Color32 a = p[i], b = Portrait[i];
                if (a.a != b.a || System.Math.Abs(a.r - b.r) + System.Math.Abs(a.g - b.g) + System.Math.Abs(a.b - b.b) > 40) n++;
            }
            return n >= 3;
        }

        /// Une allure tirée au hasard, ou un sosie de l'évadé, et son modèle — toujours un visage distinct du sien (pour
        /// qui se cache : distinct dans ce qui dépasse de la cachette).
        Allure Tirer(bool sosie, bool cache, out VoxelRig rig)
        {
            for (int k = 0; k < 40; k++)
            {
                var a = sosie ? allure.Sosie(r) : Allure.Hasard(r);
                rig = HerosModeles.Habitant(a);
                if (Distinct(rig, cache ? 4 : 0)) return a;
            }
            var b = Allure.Hasard(r);
            b.Peau = (allure.Peau + 1 + r.Next(Allure.NPeaux - 1)) % Allure.NPeaux;
            rig = HerosModeles.Habitant(b);
            return b;
        }

        Habitant Gens(bool sosie, Vector3 pos, float lacet, float delai, bool cache = false)
        {
            VoxelRig rig;
            var a = Tirer(sosie, cache, out rig);
            var h = Placer(a, rig, pos, lacet, delai);
            h.Sosie = sosie;
            return h;
        }

        /// La foule : marchands, chalands, passants, badauds, quelqu'un qui lit l'avis, un ou deux autres cachés, et
        /// l'évadé. Chaque habitant bâti rend « vrai » (une image de pause possible).
        IEnumerable<bool> Peupler(VoxelRig rigEvade)
        {
            float d = 1.2f;
            // l'évadé dans une cachette, un ou deux autres (dont peut-être un sosie) dans d'autres
            var cachettes = new List<int> { 0, 1, 2, 3 };
            int ce = cachettes[r.Next(cachettes.Count)]; cachettes.Remove(ce);
            Evade = Placer(allure, rigEvade, Cachettes[ce] + Vector3.down * Accroupis[ce], LacetVue + Hasard(-14f, 14f), d + 0.28f);
            Evade.Evade = true; Evade.Cache = true; Evade.Cachette = ce;
            Evade.Rig.name = "Évadé";
            yield return true;
            int nCaches = 1 + r.Next(2);
            for (int k = 0; k < nCaches && cachettes.Count > 0; k++)
            {
                int c = cachettes[r.Next(cachettes.Count)]; cachettes.Remove(c);
                var h = Gens(k == 0 && r.Next(2) == 0, Cachettes[c] + Vector3.down * Accroupis[c], LacetVue + Hasard(-18f, 18f), d + 0.3f + 0.25f * k, true);
                h.Cache = true; h.Cachette = c;
                yield return true;
            }
            // deux sosies parmi les visages bien visibles : un marchand, et un badaud
            int marchandSosie = r.Next(EtalsX.Length);
            for (int i = 0; i < EtalsX.Length; i++)
            {
                Gens(i == marchandSosie, new Vector3(EtalsX[i] + Hasard(-0.3f, 0.3f), 0, EtalsZ - 0.35f), Hasard(-12f, 12f), d + 0.05f * i);
                yield return true;
            }
            int badaudSosie = r.Next(2);
            Gens(badaudSosie == 0, new Vector3(-3.2f, 0, 2.45f), Hasard(45f, 70f), d + 0.2f);                // près du puits, ils causent
            Gens(false, new Vector3(-2.55f, 0, 2.25f), Hasard(-110f, -80f), d + 0.22f);
            Gens(badaudSosie == 1, new Vector3(-0.95f, 0, 2.55f), Hasard(10f, 35f), d + 0.26f);           // adossé au puits
            yield return true;
            // les chalands devant les étals, de dos ; l'un d'eux de profil
            for (int i = 0; i < EtalsX.Length; i++)
            {
                for (int k = 0; k < 2; k++)
                    Gens(false, new Vector3(EtalsX[i] + (k == 0 ? -0.45f : 0.4f) + Hasard(-0.1f, 0.1f), 0, Hasard(0.05f, 0.25f)), 180f + Hasard(-25f, 25f), d + 0.35f + 0.04f * (i * 2 + k));
                yield return true;
            }
            Gens(false, new Vector3(EtalsX[1] + 0.95f, 0, 0.35f), Hasard(-100f, -75f), d + 0.6f);
            // quelqu'un lit l'avis de recherche
            Gens(false, Avis - VersCamera * 0.75f + new Vector3(-0.2f, 0, 0), LacetVue + 180f + Hasard(-15f, 15f), d + 0.62f);
            yield return true;
            // les passants
            float[] allees = { Allee1, Allee1, Allee2, Allee2 };
            int nPassants = 3 + r.Next(2);
            for (int k = 0; k < nPassants; k++)
            {
                float z = allees[k] + Hasard(-0.08f, 0.08f);
                var a = new Vector3(-4.4f + Hasard(0f, 0.6f), 0, z); var b = new Vector3(4.4f - Hasard(0f, 0.6f), 0, z);
                float phase = Hasard(0f, 2f);
                var h = Gens(false, PositionPassant(a, b, phase), Mathf.Repeat(phase, 2f) < 1f ? 90f : -90f, d + 0.66f + 0.04f * k);
                h.Marche = true; h.A = a; h.B = b; h.Phase = phase;
                h.Vitesse = Hasard(0.42f, 0.62f);
                yield return true;
            }
        }
    }
}

// Les Dés du Dragon — l'île dans la scène : terrain, mer, dalles, décor, lieux, lampes, et les marqueurs du plateau
// (cases à choisir, pièges, embuscades, cible de quête).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DesDuDragon.Regles;
using DesDuDragon.Voxel;
using DesDuDragon.Modeles;

namespace DesDuDragon.Monde
{
    public sealed class IleVue : MonoBehaviour
    {
        public IleModele Modele;
        public readonly List<Transform> Dalles = new List<Transform>();
        public RigVue Dragon;
        Transform racineDecor, racineMarqueurs;

        public static IleVue Construire(IleModele m, Transform parent)
        {
            var go = new GameObject("Île");
            go.transform.SetParent(parent, false);
            var iv = go.AddComponent<IleVue>();
            iv.Modele = m;
            iv.Batir();
            return iv;
        }

        static GameObject Objet(string nom, Transform parent, Mesh mesh, Material mat, bool ombres, bool recoit = true)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ombres ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = recoit;
            return go;
        }

        void Batir()
        {
            var terrain = new GameObject("Terrain").transform;
            terrain.SetParent(transform, false);
            for (int mz = 0; mz * IleModele.Morceau < IleModele.NZ; mz++)
                for (int mx = 0; mx * IleModele.Morceau < IleModele.NX; mx++)
                {
                    var md = Modele.MaillageTerrain(mx, mz);
                    if (md.V.Count == 0) continue;
                    var o = Objet("Terrain_" + mx + "_" + mz, terrain, DDMateriaux.Maillage(md, "terrain"), DDMateriaux.Voxel, true);
                    o.isStatic = true;
                }
            Objet("Mer", transform, DDMateriaux.Maillage(Modele.MaillageMer(), "mer"), DDMateriaux.Eau, false, false);

            // Dalles et figurines des cases
            var cases = new GameObject("Cases").transform;
            cases.SetParent(transform, false);
            var maillagesDalles = new Dictionary<TypeCase, Mesh>();
            for (int c = 0; c < Modele.Plateau.Nombre; c++)
            {
                var t = Modele.Plateau[c].Type;
                Mesh mesh;
                if (!maillagesDalles.TryGetValue(t, out mesh)) { mesh = DDMateriaux.Maillage(IleModele.MaillageDalle(t), "dalle_" + t); maillagesDalles[t] = mesh; }
                var d = Objet("Case_" + c + "_" + t, cases, mesh, DDMateriaux.Voxel, true);
                d.transform.position = Modele.Cases[c];
                Dalles.Add(d.transform);
                var icone = RigVue.Creer(Catalogue.IconeCase(t), d.transform, "icone_" + t, true, 2.2f);
                icone.transform.localPosition = new Vector3(-0.62f, 0, 0.62f);
                icone.transform.localRotation = Quaternion.Euler(0, 200f, 0);
                icone.transform.localScale = Vector3.one * 0.72f;   // figurine de pion : ne pas cacher les héros
            }

            // Décor semé
            racineDecor = new GameObject("Décor").transform;
            racineDecor.SetParent(transform, false);
            foreach (var p in Modele.Decors)
            {
                int g = p.Graine % 6 + 1;
                var rig = Catalogue.Decor(p.Id, g);
                var rv = RigVue.Creer(rig, racineDecor, "decor_" + p.Id + "_" + g, true, 2.2f);
                Poser(rv.transform, p);
                if (rv.Piece("feuillage") != null) Animateur.Sur(rv);
            }
            // Lieux
            foreach (var p in Modele.Lieux)
            {
                if (p.Id.StartsWith("monstre:"))
                {
                    var id = p.Id.Substring(8);
                    var rv = RigVue.Creer(Catalogue.Monstre(id), racineDecor, "monstre_" + id, true, 2.6f);
                    Poser(rv.transform, p);
                    var an = Animateur.Sur(rv);
                    an.Vitesse = 0.45f;
                    if (id == "dragon") Dragon = rv;
                    continue;
                }
                var lr = RigVue.Creer(Catalogue.Decor(p.Id, p.Graine), racineDecor, "lieu_" + p.Id + "_" + p.Graine, true, 2.4f);
                Poser(lr.transform, p);
                if (lr.Piece("feuillage") != null) Animateur.Sur(lr);
            }
            // Lampes
            var lampes = new GameObject("Lampes").transform;
            lampes.SetParent(transform, false);
            foreach (var l in Modele.Lampes)
            {
                var go = new GameObject("Lampe");
                go.transform.SetParent(lampes, false);
                go.transform.position = l.Pos;
                var li = go.AddComponent<Light>();
                li.type = LightType.Point;
                li.color = l.Couleur;
                li.range = l.Portee;
                li.intensity = l.Intensite;
                li.shadows = LightShadows.None;
                go.AddComponent<Vacillement>().Base = l.Intensite;
            }
            racineMarqueurs = new GameObject("Marqueurs").transform;
            racineMarqueurs.SetParent(transform, false);
        }

        static void Poser(Transform t, Placement p)
        {
            t.position = p.Pos;
            t.rotation = Quaternion.Euler(0, p.RotY, 0);
            t.localScale = Vector3.one * (p.Echelle <= 0 ? 1f : p.Echelle);
        }

        // ------------------------------------------------------------------ positions

        public Vector3 PositionCase(int c) { return Modele.Cases[c]; }

        /// Place d'un pion sur une case partagée (jusqu'à six autour du centre).
        public Vector3 Emplacement(int c, int rang, int total)
        {
            var p = Modele.Cases[c];
            if (total <= 1) return p;
            float a = (rang / (float)total) * Mathf.PI * 2f + 0.4f;
            float r = total <= 2 ? 0.38f : 0.52f;
            return p + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
        }

        // ------------------------------------------------------------------ marqueurs

        readonly List<GameObject> choix = new List<GameObject>();
        readonly Dictionary<int, GameObject> pieges = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> embuscades = new Dictionary<int, GameObject>();
        GameObject quete;

        /// Allume des anneaux sur les cases proposées (choix d'embranchement, de piège, de case d'embuscade…).
        public void MontrerChoix(IList<int> cases, Color c)
        {
            EffacerChoix();
            foreach (int k in cases)
            {
                if (k < 0) continue;
                var go = Formes.Anneau(racineMarqueurs, 0.95f, 1.12f, c, DDMateriaux.FxDessus);
                go.transform.position = Modele.Cases[k] + Vector3.up * 0.04f;
                go.AddComponent<Pulsation>();
                var col = Formes.Colonne(go.transform, 0.9f, 2.6f, new Color(c.r, c.g, c.b, 0.35f));
                col.transform.localPosition = Vector3.zero;
                choix.Add(go);
            }
        }

        public void EffacerChoix()
        {
            foreach (var g in choix) if (g != null) Destroy(g);
            choix.Clear();
        }

        GameObject survol; int caseSurvolee = -1;

        /// Met en valeur une case (survol d'une option liée à une case) ; -1 pour effacer.
        public void Survoler(int c)
        {
            if (c == caseSurvolee) return;
            caseSurvolee = c;
            if (survol != null) Destroy(survol);
            survol = null;
            if (c < 0 || c >= Modele.Cases.Length) return;
            survol = new GameObject("Survol");
            survol.transform.SetParent(racineMarqueurs, false);
            survol.transform.position = Modele.Cases[c];
            Formes.Colonne(survol.transform, 1.05f, 5f, new Color(1f, 0.9f, 0.55f, 0.6f));
            var a = Formes.Anneau(survol.transform, 1.1f, 1.3f, new Color(1f, 0.92f, 0.6f, 1f), DDMateriaux.FxDessus);
            a.transform.localPosition = Vector3.up * 0.05f;
            a.AddComponent<Pulsation>();
        }

        /// Pièges : visibles seulement pour leur poseur (et pour un voleur).
        public void MontrerPieges(IList<Piege> liste, int joueurQuiRegarde, bool voitTout)
        {
            var vus = new HashSet<int>();
            foreach (var p in liste)
            {
                bool visible = voitTout || p.Proprietaire == joueurQuiRegarde;
                if (!visible) continue;
                vus.Add(p.Case);
                if (!pieges.ContainsKey(p.Case))
                {
                    var go = new GameObject("Piège");
                    go.transform.SetParent(racineMarqueurs, false);
                    go.transform.position = Modele.Cases[p.Case] + new Vector3(0.55f, 0.02f, -0.5f);
                    Formes.Icone(go.transform, "piege", 0.07f, true).transform.localPosition = new Vector3(0, 0.05f, 0);
                    var an = Formes.Anneau(go.transform, 0.18f, 0.3f, new Color(1f, 0.35f, 0.25f, 0.9f), DDMateriaux.FxAdd);
                    an.transform.localPosition = Vector3.up * 0.02f;
                    pieges[p.Case] = go;
                }
            }
            var aEnlever = new List<int>();
            foreach (var kv in pieges) if (!vus.Contains(kv.Key)) aEnlever.Add(kv.Key);
            foreach (int k in aEnlever) { Destroy(pieges[k]); pieges.Remove(k); }
        }

        /// Embuscades : deux cases teintées de la couleur du joueur embusqué, visibles de tous.
        public void MontrerEmbuscades(IList<Joueur> joueurs)
        {
            var vus = new HashSet<int>();
            foreach (var j in joueurs)
            {
                if (j.Embuscade == null) continue;
                vus.Add(j.Id);
                if (embuscades.ContainsKey(j.Id)) continue;
                var go = new GameObject("Embuscade_" + j.Nom);
                go.transform.SetParent(racineMarqueurs, false);
                Color c = Pal.Joueurs[j.Couleur];
                foreach (int k in new[] { j.Embuscade.Case, j.Embuscade.CaseVoisine })
                {
                    var a = Formes.Anneau(go.transform, 0.8f, 1.02f, new Color(c.r, c.g, c.b, 0.85f), DDMateriaux.FxAdd);
                    a.transform.position = Modele.Cases[k] + Vector3.up * 0.03f;
                    var ic = Formes.Icone(go.transform, "oeil", 0.06f, false);
                    ic.transform.position = Modele.Cases[k] + Vector3.up * 1.9f;
                    ic.AddComponent<Flotter>();
                }
                embuscades[j.Id] = go;
            }
            var aEnlever = new List<int>();
            foreach (var kv in embuscades) if (!vus.Contains(kv.Key)) aEnlever.Add(kv.Key);
            foreach (int k in aEnlever) { Destroy(embuscades[k]); embuscades.Remove(k); }
        }

        /// Le lieu à atteindre de la quête du joueur qui joue (ou rien).
        public void MontrerQuete(int caseCible)
        {
            if (quete != null) Destroy(quete);
            quete = null;
            if (caseCible < 0) return;
            quete = new GameObject("Quête");
            quete.transform.SetParent(racineMarqueurs, false);
            quete.transform.position = Modele.Cases[caseCible];
            var ic = Formes.Icone(quete.transform, "parchemin", 0.08f, false);
            ic.transform.localPosition = Vector3.up * 2.4f;
            ic.AddComponent<Flotter>();
            Formes.Colonne(quete.transform, 0.5f, 3.2f, new Color(1f, 0.85f, 0.4f, 0.28f));
        }
    }

    /// Vacillement doux d'une lampe (flammes, lave).
    public sealed class Vacillement : MonoBehaviour
    {
        public float Base = 1f;
        Light l; float graine;
        void Start() { l = GetComponent<Light>(); graine = Random.value * 100f; }
        void Update()
        {
            if (l == null) return;
            float t = Time.time * 3.1f + graine;
            l.intensity = Base * (0.86f + 0.1f * Mathf.Sin(t) + 0.06f * Mathf.Sin(t * 2.7f + 1.3f));
        }
    }

    public sealed class Pulsation : MonoBehaviour
    {
        Vector3 echelle; float graine;
        void Start() { echelle = transform.localScale; graine = Random.value * 3f; }
        void Update() { transform.localScale = echelle * (1f + 0.06f * Mathf.Sin(Time.time * 4f + graine)); }
    }

    public sealed class Flotter : MonoBehaviour
    {
        Vector3 basePos; float graine;
        void Start() { basePos = transform.localPosition; graine = Random.value * 3f; }
        void Update()
        {
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(Time.time * 2f + graine) * 0.12f);
            transform.Rotate(0, 50f * Time.deltaTime, 0, Space.World);
        }
    }
}

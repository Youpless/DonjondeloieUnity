// Donjon de l'Oie — la nuit à l'écran : la lumière tombe (lune, lampes plus vives), puis l'événement de la nuit — le
// voleur, le trou de ver, la bonne nuit ou l'arène du diable, qui flotte au-dessus de l'île et où les pions se battent
// l'un après l'autre — et le jour se lève.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Modeles;
using DonjonDeLOie.Interface;

namespace DonjonDeLOie.Pilote
{
    public sealed partial class DDJeu
    {
        static readonly Color CouleurNuit = new Color(0.6f, 0.7f, 1f);
        static VoxelRig modeleArene;      // l'arène (≈ 80 000 voxels) : construite et maillée une seule fois, au chargement
        RigVue arene;
        Vector3 areneCentre;
        bool nuitEnCours;                 // la nuit se joue à vitesse normale, même avec les bots rapides
        readonly List<int> areneOrdre = new List<int>();

        IEnumerator Nuit(SNuit s)
        {
            if (s.Tombe)
            {
                nuitEnCours = true;
                hud.Banniere("Fin du tour " + s.Tour + "  ·  la nuit tombe", CouleurNuit);
                cam.Regarder(cam.CentreCarte, Mathf.Max(30f, cam.TailleCarte * 0.62f), 52f);
                Sons.Jouer(Son.Sort, 0.5f);
                yield return StartCoroutine(Crepuscule(0f, 1f, 1.8f));
            }
            else
            {
                if (arene != null) yield return StartCoroutine(QuitterArene());
                hud.Banniere("Le jour se lève", UIK.Or);
                yield return StartCoroutine(Crepuscule(1f, 0f, 1.4f));
                nuitEnCours = false;
            }
        }

        IEnumerator Crepuscule(float de, float vers, float duree)
        {
            float t = 0;
            duree /= V;
            while (t < duree)
            {
                t += Time.deltaTime;
                Ambiance.Nuit(Mathf.Lerp(de, vers, Mathf.SmoothStep(0, 1, t / duree)), cam.Cam);
                yield return null;
            }
            Ambiance.Nuit(vers, cam.Cam);
        }

        /// Une partie gagnée en pleine nuit (le champion de l'arène atteint le niveau visé, un trou de ver mène au
        /// Dragonnoie) : chacun regagne sa case, l'arène s'enfonce et le jour se lève avant la fête.
        IEnumerator AubeVictoire()
        {
            if (arene != null) yield return StartCoroutine(QuitterArene());
            hud.Banniere("Le jour se lève", UIK.Or);
            yield return StartCoroutine(Crepuscule(1f, 0f, 1.2f));
            nuitEnCours = false;
        }

        /// Au chargement : le modèle de l'arène et ses maillages sont prêts d'avance (pas d'à-coup à la première nuit
        /// d'arène ; les nuits suivantes réutilisent le même modèle).
        void PreparerArene()
        {
            if (modeleArene == null) modeleArene = Catalogue.Decor("arene_diable", 7);
            var essai = RigVue.Creer(modeleArene, transitoires, "Arène du diable", true, 2.6f);
            Destroy(essai.gameObject);
        }

        /// Une partie interrompue en pleine nuit (retour au menu, nouvelle partie) : le jour revient d'un coup.
        void JourImmediat()
        {
            if (Ambiance.NuitActuelle > 0f) Ambiance.Nuit(0f, cam != null ? cam.Cam : null);
            if (arene != null) { Destroy(arene.gameObject); arene = null; }
            areneOrdre.Clear();
            nuitEnCours = false;
        }

        // ------------------------------------------------------------------ le voleur, le trou de ver, la bonne nuit

        IEnumerator VolNocturne(SEffet e)
        {
            if (!pions.ContainsKey(e.Joueur)) yield break;
            var p = pions[e.Joueur];
            cam.Regarder(p.transform.position + Vector3.up * 0.6f, 9f, 40f);
            yield return Attendre(0.5f);
            var loin = p.transform.position + (p.transform.position - cam.CentreCarte).normalized * 7f + Vector3.up * 2f;
            fumees.Fumee(p.transform.position, new Color(0.12f, 0.1f, 0.2f, 0.85f), 18, 0.6f);
            etincelles.Trait(Tete(e.Joueur), loin, new Color(1f, 0.82f, 0.3f), 30);
            Sons.Jouer(Son.Or, 0.8f);
            p.Anim.Jouer(Geste.Touche);
            hud.Flottant(Tete(e.Joueur) + Vector3.up * 0.4f, "Au voleur !", UIK.Rouge, 30);
            yield return Attendre(0.9f);
        }

        IEnumerator TrouDeVer(SEffet e)
        {
            if (!pions.ContainsKey(e.Joueur) || !pions.ContainsKey(e.Cible)) yield break;
            var a = pions[e.Joueur].transform.position; var b = pions[e.Cible].transform.position;
            float d = Vector3.Distance(a, b);
            cam.Regarder((a + b) * 0.5f + Vector3.up * 0.6f, Mathf.Clamp(d * 1.1f + 8f, 10f, 60f), 48f);
            Sons.Jouer(Son.Sort, 1f);
            var violet = new Color(0.7f, 0.4f, 1f);
            for (int k = 0; k < 3; k++)
            {
                etincelles.Spirale(a, violet, 30, 2.2f);
                etincelles.Spirale(b, violet, 30, 2.2f);
                yield return Attendre(0.25f);
            }
            etincelles.Trait(a + Vector3.up, b + Vector3.up, violet, 40);
            yield return Attendre(0.3f);
        }

        IEnumerator BonneNuit(SEffet e)
        {
            if (!pions.ContainsKey(e.Joueur)) yield break;
            var p = pions[e.Joueur];
            cam.Regarder(p.transform.position + Vector3.up * 0.6f, 8f, 36f);
            yield return Attendre(0.4f);
            for (int k = 0; k < 3; k++)
            {
                hud.Flottant(Tete(e.Joueur) + new Vector3(0.25f * k, 0.3f * k, 0), "Z", new Color(0.75f, 0.85f, 1f), 26 + 8 * k);
                yield return Attendre(0.35f);
            }
            etincelles.Spirale(p.transform.position, new Color(0.7f, 0.8f, 1f), 30, 2f);
            yield return Attendre(0.3f);
        }

        // ------------------------------------------------------------------ l'arène du diable

        /// L'arène flotte au-dessus de l'île ; les pions y sont transportés en cercle autour du centre.
        IEnumerator EntrerArene(SEffet e)
        {
            areneOrdre.Clear();
            foreach (var morceau in (e.Texte ?? "").Split(',')) { int id; if (int.TryParse(morceau, out id) && pions.ContainsKey(id)) areneOrdre.Add(id); }
            float hmax = 0;
            foreach (var h in modele.H) hmax = Mathf.Max(hmax, h);
            areneCentre = modele.Centre + Vector3.up * (hmax + 14f);
            if (modeleArene == null) modeleArene = Catalogue.Decor("arene_diable", 7);
            if (arene == null) arene = RigVue.Creer(modeleArene, transitoires, "Arène du diable", true, 2.6f);
            arene.transform.position = areneCentre - Vector3.up * 6f;
            arene.transform.rotation = Quaternion.Euler(0, cam.Lacet, 0);       // l'entrée, sans piliers, face à la caméra
            hud.Banniere("L'arène du diable", new Color(1f, 0.35f, 0.2f));
            cam.Regarder(areneCentre + Vector3.up * 1f, 14f, 46f);
            Sons.Jouer(Son.Monstre, 0.9f);
            // elle monte des ténèbres
            float t = 0;
            while (t < 1.2f / V)
            {
                t += Time.deltaTime;
                arene.transform.position = Vector3.Lerp(areneCentre - Vector3.up * 6f, areneCentre, Mathf.SmoothStep(0, 1, t * V / 1.2f));
                yield return null;
            }
            arene.transform.position = areneCentre;
            etincelles.Explosion(areneCentre + Vector3.up * 0.5f, new Color(1f, 0.4f, 0.15f), 50, 5f);
            // chacun est happé vers sa place autour du cercle
            for (int i = 0; i < areneOrdre.Count; i++)
            {
                var p = pions[areneOrdre[i]];
                etincelles.Spirale(p.transform.position, new Color(1f, 0.45f, 0.2f), 24, 1.6f);
                StartCoroutine(p.Teleporter(PlaceArene(i)));
                yield return Attendre(0.18f);
            }
            yield return Attendre(0.6f);
            for (int i = 0; i < areneOrdre.Count; i++) pions[areneOrdre[i]].Regarder(areneCentre);
        }

        /// La place d'attente d'un combattant : en arc de cercle derrière le centre (vu de la caméra), en deçà de la lave.
        Vector3 PlaceArene(int rang)
        {
            int n = Mathf.Max(1, areneOrdre.Count);
            float a = Mathf.PI * (0.08f + 0.84f * (n == 1 ? 0.5f : rang / (float)(n - 1)));
            var avant = arene != null ? arene.transform.forward : Vector3.forward;
            var droite = arene != null ? arene.transform.right : Vector3.right;
            return areneCentre + droite * (Mathf.Cos(a) * 2.0f) + avant * (0.6f + Mathf.Sin(a) * 1.0f);
        }

        /// Avant un duel de l'arène : les deux adversaires s'avancent au centre, la caméra les regarde d'assez haut pour
        /// passer au-dessus des cornes de l'entrée.
        IEnumerator AvancerDuelArene(SCombat c)
        {
            if (arene == null || !pions.ContainsKey(c.Attaquant) || !pions.ContainsKey(c.Defenseur)) yield break;
            var droite = arene.transform.right;
            var a = pions[c.Attaquant]; var b = pions[c.Defenseur];
            cam.Regarder(areneCentre + Vector3.up * 0.7f, 9.5f, 46f);
            StartCoroutine(a.Glisser(areneCentre - droite * 0.8f, 0.35f / V));
            yield return StartCoroutine(b.Glisser(areneCentre + droite * 0.8f, 0.35f / V));
            a.Regarder(b.transform.position); b.Regarder(a.transform.position);
        }

        /// Après un duel de l'arène : le vaincu regagne sa place, le champion attend le suivant.
        IEnumerator ApresDuelArene(SCombat c)
        {
            if (arene == null || c.Egalite) yield break;
            int perdant = c.AttaquantGagne ? c.Defenseur : c.Attaquant;
            int rang = areneOrdre.IndexOf(perdant);
            if (rang >= 0 && pions.ContainsKey(perdant)) StartCoroutine(pions[perdant].Glisser(PlaceArene(rang), 0.4f / V));
            cam.Regarder(areneCentre + Vector3.up * 1f, 14f, 46f);
            yield return Attendre(0.3f);
        }

        IEnumerator ChampionArene(SEffet e)
        {
            if (arene == null || !pions.ContainsKey(e.Joueur)) yield break;
            var p = pions[e.Joueur];
            yield return StartCoroutine(p.Glisser(areneCentre, 0.3f / V));
            cam.Regarder(areneCentre + Vector3.up * 0.8f, 9f, 40f);
            p.Anim.Jouer(Geste.Victoire, 1.2f);
            Sons.Jouer(Son.Victoire, 0.7f);
            for (int k = 0; k < 3; k++)
            {
                etincelles.Explosion(areneCentre + new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 3.5f), Random.Range(-1f, 1f)),
                    k % 2 == 0 ? new Color(1f, 0.4f, 0.15f) : new Color(1f, 0.85f, 0.4f), 40, 4f, 0.07f, 1.5f);
                yield return Attendre(0.3f);
            }
        }

        /// Au lever du jour : chacun retrouve sa case, l'arène s'enfonce dans les ténèbres.
        IEnumerator QuitterArene()
        {
            foreach (var id in areneOrdre)
            {
                if (!pions.ContainsKey(id)) continue;
                etincelles.Spirale(pions[id].transform.position, new Color(1f, 0.45f, 0.2f), 20, 1.4f);
                StartCoroutine(pions[id].Teleporter(Place(id)));
            }
            yield return Attendre(0.8f);
            foreach (var j in partie.Joueurs) RangerCase(j.Case, false);
            var depart = arene.transform.position;
            float t = 0;
            while (t < 0.9f / V)
            {
                t += Time.deltaTime;
                arene.transform.position = Vector3.Lerp(depart, depart - Vector3.up * 8f, t * V / 0.9f);
                yield return null;
            }
            Destroy(arene.gameObject);
            arene = null;
            areneOrdre.Clear();
            cam.Regarder(cam.CentreCarte, Mathf.Max(30f, cam.TailleCarte * 0.62f), 52f);
        }
    }
}

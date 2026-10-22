// Les Dés du Dragon — le pion d'un joueur : son héros animé, un socle à sa couleur, ses déplacements de case en case.
using System.Collections;
using UnityEngine;
using DesDuDragon.Regles;
using DesDuDragon.Voxel;
using DesDuDragon.Modeles;

namespace DesDuDragon.Monde
{
    public sealed class PionVue : MonoBehaviour
    {
        public int Joueur;
        public RigVue Rig;
        public Animateur Anim;
        public Color Couleur;
        GameObject socle, halo;
        Vector3 regard = Vector3.back;

        public static PionVue Creer(Joueur j, Transform parent)
        {
            var go = new GameObject("Pion_" + j.Nom);
            go.transform.SetParent(parent, false);
            var pv = go.AddComponent<PionVue>();
            pv.Joueur = j.Id;
            pv.Couleur = Pal.Joueurs[j.Couleur];
            pv.Rig = RigVue.Creer(Catalogue.Heros(j.Classe, Pal.Joueurs[j.Couleur]), go.transform, "heros_" + j.Classe + "_" + j.Couleur, true, 2.6f);
            pv.Anim = Animateur.Sur(pv.Rig);
            var c = pv.Couleur;
            pv.socle = Formes.Disque(go.transform, 0.36f, new Color(c.r, c.g, c.b, 0.55f), new Color(c.r, c.g, c.b, 0.0f), DDMateriaux.FxAlpha);
            pv.socle.transform.localPosition = Vector3.up * 0.015f;
            var anneau = Formes.Anneau(go.transform, 0.33f, 0.4f, new Color(c.r, c.g, c.b, 0.95f), DDMateriaux.FxAlpha);
            anneau.transform.localPosition = Vector3.up * 0.02f;
            pv.halo = Formes.Colonne(go.transform, 0.42f, 1.8f, new Color(c.r, c.g, c.b, 0.35f));
            pv.halo.SetActive(false);
            return pv;
        }

        /// Le joueur dont c'est le tour a une colonne de lumière à sa couleur.
        public void Actif(bool oui) { if (halo != null) halo.SetActive(oui); }

        public void Regarder(Vector3 point)
        {
            var d = point - transform.position; d.y = 0;
            if (d.sqrMagnitude < 1e-4f) return;
            regard = d.normalized;
        }

        void Update()
        {
            var cible = Quaternion.LookRotation(regard, Vector3.up);
            Rig.transform.rotation = Quaternion.Slerp(Rig.transform.rotation, cible, Time.deltaTime * 10f);
        }

        /// Saut d'une case à l'autre (arc), en durée fixe.
        public IEnumerator Sauter(Vector3 vers, float duree, float hauteur = 0.55f)
        {
            var de = transform.position;
            Regarder(vers);
            Anim.EnMarche = true;
            float t = 0;
            while (t < duree)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duree);
                float e = k * k * (3 - 2 * k);
                var p = Vector3.Lerp(de, vers, e);
                p.y += Mathf.Sin(k * Mathf.PI) * hauteur;
                transform.position = p;
                yield return null;
            }
            transform.position = vers;
            Anim.EnMarche = false;
        }

        /// Glisser doucement vers une place (quand plusieurs pions se partagent une case).
        public IEnumerator Glisser(Vector3 vers, float duree)
        {
            var de = transform.position;
            float t = 0;
            while (t < duree)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(de, vers, Mathf.SmoothStep(0, 1, t / duree));
                yield return null;
            }
            transform.position = vers;
        }

        /// Disparaître dans un éclair puis réapparaître ailleurs.
        public IEnumerator Teleporter(Vector3 vers)
        {
            float t = 0;
            var e0 = Rig.transform.localScale;
            while (t < 0.35f) { t += Time.deltaTime; Rig.transform.localScale = new Vector3(e0.x * (1 - t / 0.35f), e0.y * (1 + t / 0.35f), e0.z * (1 - t / 0.35f)); yield return null; }
            transform.position = vers;
            t = 0;
            while (t < 0.35f) { t += Time.deltaTime; float k = t / 0.35f; Rig.transform.localScale = new Vector3(e0.x * k, e0.y * (2 - k), e0.z * k); yield return null; }
            Rig.transform.localScale = e0;
        }
    }
}

// Donjon de l'Oie — le Dragonnoie à l'écran : il se tient au bord de sa case (les pions restent visibles), prend ses
// grandes envolées en arc au-dessus de l'île (la caméra le suit), et arrache leur or aux vaincus.
using System.Collections;
using UnityEngine;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Modeles;
using DonjonDeLOie.Interface;

namespace DonjonDeLOie.Pilote
{
    public sealed partial class DDJeu
    {
        RigVue oieVue;
        Animateur oieAnim;

        /// Où il se tient sur une case : à 2,3 m du centre, du côté le plus plat et le plus loin de la mer, face à la case.
        Vector3 PositionOie(int c, out Quaternion regard)
        {
            var p = modele.Cases[c];
            var k = partie.Plateau[c];
            var suiv = k.Suivants.Count > 0 ? modele.Cases[k.Suivants[0]] : p + Vector3.forward;
            var prec = k.Precedents.Count > 0 ? modele.Cases[k.Precedents[0]] : p - Vector3.forward;
            var t = suiv - prec; t.y = 0;
            t = t.sqrMagnitude > 1e-4f ? t.normalized : Vector3.forward;
            var n = new Vector3(-t.z, 0, t.x);
            Vector3 meilleur = p; float score = float.MaxValue;
            foreach (float cote in new[] { 1f, -1f })
            {
                var q = p + n * (2.3f * cote);
                float h = modele.HauteurEn(q.x, q.z);
                float s = Mathf.Abs(h - p.y) + (modele.Ellipse(q.x, q.z) > 0.95f ? 10f : 0f) + (Vector3.Dot(n * cote, cam.transform.forward) > 0.3f ? 0.5f : 0f);
                if (s < score) { score = s; meilleur = new Vector3(q.x, Mathf.Max(h, p.y - 0.4f), q.z); }
            }
            var d = p - meilleur; d.y = 0;
            regard = Quaternion.LookRotation(d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.back);
            return meilleur;
        }

        /// Une grande envolée (ou son arrivée sur l'île, De = -1) : un arc au-dessus de l'île, ailes battantes.
        IEnumerator Envol(SEnvol s)
        {
            var m = partie.Dragonnoie;
            if (m == null) yield break;
            Quaternion regardFin;
            var arrivee = PositionOie(s.Vers, out regardFin);
            Vector3 depart;
            bool premier = oieVue == null;
            if (premier)
            {
                oieVue = RigVue.Creer(Catalogue.Monstre(m.Modele), transitoires, "Dragonnoie", true, 2.6f);
                oieAnim = Animateur.Sur(oieVue);
                var dehors = arrivee - modele.Centre; dehors.y = 0;
                dehors = dehors.sqrMagnitude > 1f ? dehors.normalized : Vector3.back;
                depart = arrivee + dehors * 45f + Vector3.up * 28f;     // il arrive du large
                oieVue.transform.position = depart;
            }
            else depart = oieVue.transform.position;

            hud.Banniere(premier ? "Le Dragonnoie arrive !" : "Tour " + partie.Tour + "  ·  le Dragonnoie s'envole !", UIK.Or);
            Sons.Jouer(Son.Monstre, 1f);
            oieAnim.Vitesse = 1f;
            oieAnim.EnMarche = true;                                    // battements rapides
            if (!premier)
            {
                oieAnim.Jouer(Geste.Victoire, 0.5f);                    // il déploie ses ailes avant de bondir
                fumees.Fumee(depart, new Color(0.85f, 0.82f, 0.75f, 0.6f), 16, 1.2f);
                yield return Attendre(0.4f);
            }
            cam.Suivre(oieVue.transform, 26f);
            var plat = arrivee - depart; plat.y = 0;
            float distance = plat.magnitude;
            float duree = Mathf.Clamp(1.4f + distance / 22f, 1.8f, 3.4f) / V;
            float hauteur = premier ? 0f : Mathf.Clamp(7f + distance * 0.3f, 9f, 22f);
            float t = 0;
            while (t < duree)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duree);
                float doux = premier ? 1f - (1f - k) * (1f - k) : Mathf.SmoothStep(0, 1, k);
                var pos = Vector3.Lerp(depart, arrivee, doux) + Vector3.up * (4f * k * (1f - k) * hauteur);
                var dir = pos - oieVue.transform.position; dir.y = 0;
                if (dir.sqrMagnitude > 1e-5f)
                    oieVue.transform.rotation = Quaternion.Slerp(oieVue.transform.rotation, Quaternion.LookRotation(dir.normalized), 1f - Mathf.Exp(-Time.deltaTime * 6f));
                oieVue.transform.position = pos;
                yield return null;
            }
            oieVue.transform.position = arrivee;
            oieAnim.EnMarche = false;
            // l'atterrissage : poussière, cri, ailes déployées, face à la case
            fumees.Fumee(arrivee, new Color(0.8f, 0.74f, 0.62f, 0.75f), 26, 1.6f);
            etincelles.Explosion(arrivee + Vector3.up * 0.3f, new Color(1f, 0.9f, 0.7f), 18, 3f);
            Sons.Jouer(Son.Coup, 0.9f);
            float r0 = 0;
            var rot0 = oieVue.transform.rotation;
            while (r0 < 0.35f) { r0 += Time.deltaTime; oieVue.transform.rotation = Quaternion.Slerp(rot0, regardFin, r0 / 0.35f); yield return null; }
            oieVue.transform.rotation = regardFin;
            oieAnim.Jouer(Geste.Victoire, 1.1f);
            var milieu = (modele.Cases[s.Vers] + arrivee) * 0.5f;
            cam.Regarder(milieu + Vector3.up * 1.2f, 17f, 40f);
            hud.Rafraichir();
            yield return Attendre(premier ? 1.0f : 0.7f);
        }

        /// Le Dragonnoie arrache sa bourse au vaincu : les pièces volent jusqu'à lui.
        IEnumerator OrArrache(SEffet e)
        {
            if (oieVue == null || !pions.ContainsKey(e.Joueur)) yield break;
            var p = pions[e.Joueur];
            var bec = oieVue.transform.position + Vector3.up * (oieVue.Hauteur * 0.85f);
            etincelles.Trait(Tete(p.Joueur), bec, new Color(1f, 0.82f, 0.3f), 36);
            etincelles.Pieces(oieVue.transform.position, 18);
            Sons.Jouer(Son.Or, 1f);
            if (oieAnim != null) oieAnim.Jouer(Geste.Joie);
            p.Anim.Jouer(Geste.Touche);
            yield return Attendre(0.7f);
        }

        /// Le Dragonnoie a été retiré pendant la partie (catalogue appliqué en jeu, « Dragonnoie actif » décoché) : il quitte l'île.
        void VerifierOie()
        {
            if (oieVue == null || partie == null || partie.Dragonnoie != null) return;
            fumees.Fumee(oieVue.transform.position, new Color(0.85f, 0.82f, 0.75f, 0.6f), 16, 1.2f);
            if (monstreVue == oieVue) { monstreVue = null; monstreAnim = null; monstrePersistant = false; }
            Destroy(oieVue.gameObject);
            oieVue = null; oieAnim = null;
        }

        /// Clic sur l'encart du Dragonnoie : la caméra va le voir.
        void MontrerOie()
        {
            if (oieVue == null) return;
            cam.Regarder(oieVue.transform.position + Vector3.up * 1.2f, 15f, 38f);
        }
    }
}

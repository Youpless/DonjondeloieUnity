// Donjon de l'Oie — mise en scène des combats : le monstre surgit à côté du héros, les dés roulent, les coups portent.
using System.Collections;
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
        RigVue monstreVue;
        Animateur monstreAnim;
        bool monstrePersistant;

        /// Le monstre jaillit du sol à côté du héros (le dragon, lui, est déjà dans son antre, et le Dragonnoie sur sa case).
        IEnumerator FaireSurgir(DefMonstre m, Joueur j)
        {
            EnleverMonstre(true);
            var p = pions[j.Id];
            RigVue present = m.Errant && oieVue != null ? oieVue : (m.Boss ? ile.Dragon : null);
            if (present != null)
            {
                monstreVue = present;
                monstreAnim = present.GetComponent<Animateur>();
                monstrePersistant = true;
                if (monstreAnim != null) { monstreAnim.Vitesse = 1f; monstreAnim.Jouer(Geste.Victoire, 1.2f); }
                Sons.Jouer(Son.Monstre, 1f);
                cam.Cadrer(p.transform.position, monstreVue.transform.position);
                p.Regarder(monstreVue.transform.position);
                etincelles.Explosion(monstreVue.transform.position + Vector3.up * 2f, new Color(1f, 0.45f, 0.15f), 40, 4f);
                yield return Attendre(1.0f);
                yield break;
            }
            var droite = cam.transform.right; droite.y = 0; droite.Normalize();
            var avant = cam.transform.forward; avant.y = 0; avant.Normalize();
            var pos = p.transform.position + droite * 2.2f + avant * 0.4f;
            pos.y = Mathf.Max(modele.HauteurEn(pos.x, pos.z), p.transform.position.y - 0.3f);
            monstreVue = RigVue.Creer(Catalogue.Monstre(m.Modele), transitoires, "monstre_" + m.Id, true, 2.6f);
            monstreVue.transform.position = pos - Vector3.up * 1.5f;
            var d = p.transform.position - pos; d.y = 0;
            monstreVue.transform.rotation = Quaternion.LookRotation(d.sqrMagnitude > 0.01f ? d : Vector3.back);
            monstreAnim = Animateur.Sur(monstreVue);
            monstreAnim.Flotte = m.Modele == "spectre";
            monstrePersistant = false;
            p.Regarder(pos);
            cam.Cadrer(p.transform.position, pos);
            fumees.Fumee(pos, new Color(0.45f, 0.38f, 0.3f, 0.75f), 22, 0.8f);
            Sons.Jouer(Son.Monstre, 0.9f);
            float t = 0;
            var de = monstreVue.transform.position;
            while (t < 0.45f)
            {
                t += Time.deltaTime * V;
                float k = Mathf.Clamp01(t / 0.45f);
                monstreVue.transform.position = Vector3.Lerp(de, pos, 1f - (1f - k) * (1f - k));
                yield return null;
            }
            monstreVue.transform.position = pos;
            monstreAnim.Jouer(Geste.Attaque, 0.6f);
            yield return Attendre(0.5f);
        }

        void EnleverMonstre(bool immediat)
        {
            if (monstreVue == null) return;
            if (monstrePersistant)
            {
                if (monstreAnim != null) { monstreAnim.Relever(); monstreAnim.Vitesse = monstreVue == oieVue ? 1f : 0.45f; }   // le dragon se rendort
            }
            else if (immediat) Destroy(monstreVue.gameObject);
            else StartCoroutine(Enfouir(monstreVue.gameObject));
            monstreVue = null; monstreAnim = null; monstrePersistant = false;
        }

        IEnumerator Enfouir(GameObject go)
        {
            if (go == null) yield break;
            fumees.Fumee(go.transform.position, new Color(0.45f, 0.4f, 0.35f, 0.7f), 16, 0.7f);
            var de = go.transform.position;
            float t = 0;
            while (t < 0.5f && go != null)
            {
                t += Time.deltaTime;
                go.transform.position = de - Vector3.up * (t / 0.5f) * 1.8f;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        IEnumerator Combat(SCombat c)
        {
            var a = pions[c.Attaquant];
            Transform tb; Animateur animB; RigVue rigB;
            if (c.Duel)
            {
                var b = pions[c.Defenseur];
                tb = b.transform; animB = b.Anim; rigB = b.Rig;
                if (c.Arene) yield return StartCoroutine(AvancerDuelArene(c));
                // deux pions sur la même place : on les écarte un peu
                else if ((a.transform.position - b.transform.position).sqrMagnitude < 0.3f)
                {
                    var dec = cam.transform.right * 0.5f; dec.y = 0;
                    StartCoroutine(a.Glisser(a.transform.position - dec, 0.2f));
                    StartCoroutine(b.Glisser(b.transform.position + dec, 0.2f));
                    yield return new WaitForSeconds(0.22f);
                }
                a.Regarder(b.transform.position); b.Regarder(a.transform.position);
                if (!c.Arene) cam.Cadrer(a.transform.position, b.transform.position);
            }
            else
            {
                if (monstreVue == null) yield return StartCoroutine(FaireSurgir(c.Monstre, partie.Joueurs[c.Attaquant]));
                tb = monstreVue.transform; animB = monstreAnim; rigB = monstreVue;
                a.Regarder(tb.position);
            }
            fc.Montrer(c, partie);
            yield return Attendre(0.35f);

            // les deux dés roulent entre les adversaires
            var milieu = (a.transform.position + tb.position) * 0.5f;
            var avant = cam.transform.forward; avant.y = 0; avant.Normalize();
            var droite = cam.transform.right; droite.y = 0; droite.Normalize();
            var posA = milieu - droite * 0.5f - avant * 1.35f; posA.y = a.transform.position.y;
            var posB = milieu + droite * 0.5f - avant * 1.35f; posB.y = a.transform.position.y;
            var deA = DeVue.Creer(transitoires, c.FacesDe);
            var deB = DeVue.Creer(transitoires, c.FacesDe);
            Sons.Jouer(Son.De);
            StartCoroutine(deA.Lancer(posA - droite * 1.2f + Vector3.up * 1.4f - avant * 0.8f, posA, c.DeA, 0.95f / V));
            yield return StartCoroutine(deB.Lancer(posB + droite * 1.2f + Vector3.up * 1.4f - avant * 0.8f, posB, c.DeB, 1.0f / V));
            fc.Des(c);
            hud.Flottant(deA.transform.position + Vector3.up * 0.45f, c.DeA.ToString(), UIK.Or, 40);
            hud.Flottant(deB.transform.position + Vector3.up * 0.45f, c.DeB.ToString(), UIK.Or, 40);
            yield return Attendre(0.6f);
            fc.Verdict(c);

            if (c.Egalite)
            {
                a.Anim.Jouer(Geste.Attaque); if (animB != null) animB.Jouer(Geste.Attaque);
                Sons.Jouer(Son.Coup, 0.6f);
                etincelles.Explosion(milieu + Vector3.up * 0.9f, new Color(1f, 0.9f, 0.6f), 20, 3f);
                yield return Attendre(0.9f);
            }
            else
            {
                Animateur animGagnant = c.AttaquantGagne ? a.Anim : animB;
                Animateur animPerdant = c.AttaquantGagne ? animB : a.Anim;
                RigVue rigPerdant = c.AttaquantGagne ? rigB : a.Rig;
                Transform tPerdant = c.AttaquantGagne ? tb : a.transform;
                if (animGagnant != null) animGagnant.Jouer(Geste.Attaque);
                yield return Attendre(0.38f);
                Sons.Jouer(Son.Coup);
                if (animPerdant != null) animPerdant.Jouer(Geste.Touche);
                if (rigPerdant != null) rigPerdant.Eclair(new Color(1f, 0.25f, 0.15f), 0.4f);
                etincelles.Explosion(tPerdant.position + Vector3.up * Mathf.Max(0.6f, (rigPerdant != null ? rigPerdant.Hauteur : 1f) * 0.55f), new Color(1f, 0.7f, 0.35f), 30, 4f);
                yield return Attendre(0.7f);
                if (!c.Duel)
                {
                    if (c.AttaquantGagne)
                    {
                        // le monstre s'effondre
                        if (monstreAnim != null) monstreAnim.Jouer(Geste.Ko);
                        yield return Attendre(0.6f);
                        if (monstrePersistant && c.Monstre.Errant) { monstreVue = null; monstreAnim = null; monstrePersistant = false; }   // le Dragonnoie reste à terre
                        else if (monstrePersistant) { EnleverMonstre(false); }
                        else if (monstreVue != null)
                        {
                            fumees.Fumee(monstreVue.transform.position, new Color(0.35f, 0.32f, 0.3f, 0.8f), 30, 0.8f);
                            etincelles.Explosion(monstreVue.transform.position + Vector3.up * 0.6f, new Color(1f, 0.85f, 0.45f), 24, 3f);
                            var go = monstreVue.gameObject;
                            monstreVue = null; monstreAnim = null;
                            StartCoroutine(Disparaitre(go, 0.35f));
                        }
                        a.Anim.Jouer(Geste.Joie);
                    }
                    else
                    {
                        yield return Attendre(0.2f);
                        EnleverMonstre(false);
                    }
                }
                else
                {
                    var gagnant = c.AttaquantGagne ? a : pions[c.Defenseur];
                    gagnant.Anim.Jouer(Geste.Victoire, 1f);
                }
            }
            StartCoroutine(Disparaitre(deA.gameObject, 0.35f));
            StartCoroutine(Disparaitre(deB.gameObject, 0.35f));
            yield return Attendre(0.5f);
            fc.Cacher();
            if (c.Arene) { yield return StartCoroutine(ApresDuelArene(c)); yield break; }
            if (c.Duel && !c.Egalite)
            {
                // chacun reprend sa place
                RangerCase(partie.Joueurs[c.Attaquant].Case, true);
                RangerCase(partie.Joueurs[c.Defenseur].Case, true);
            }
            if (!c.Egalite) cam.Suivre(pions[partie.JoueurCourant.Id].transform, 12.5f);
        }
    }
}

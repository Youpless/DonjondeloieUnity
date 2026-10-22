// Donjon de l'Oie — chaque signal des règles devient ce que voient et entendent les joueurs.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Interface;

namespace DonjonDeLOie.Pilote
{
    public sealed partial class DDJeu
    {
        IEnumerator Boucle()
        {
            yield return new WaitForSeconds(0.6f);
            foreach (var s in partie.Debut()) yield return StartCoroutine(Presenter(s));
            while (!partie.Fini)
            {
                foreach (var s in partie.JouerTour())
                {
                    yield return StartCoroutine(Presenter(s));
                    if (partie == null) yield break;
                }
                hud.Rafraichir();
                yield return Attendre(0.2f);
            }
            yield return StartCoroutine(Victoire());
        }

        static Color CouleurTon(Ton t)
        {
            switch (t)
            {
                case Ton.Bon: return UIK.Vert;
                case Ton.Mauvais: return UIK.Rouge;
                case Ton.Combat: return new Color(1f, 0.62f, 0.35f);
                case Ton.Quete: return UIK.Or;
                case Ton.Systeme: return UIK.Bleu;
                default: return UIK.Creme;
            }
        }

        IEnumerator Presenter(Signal s)
        {
            if (s is STourDebut) yield return StartCoroutine(TourDebut((STourDebut)s));
            else if (s is SAnnonce)
            {
                var a = (SAnnonce)s;
                Color c = a.Joueur >= 0 ? (Color)Pal.Joueurs[partie.Joueurs[a.Joueur].Couleur] : CouleurTon(a.Ton);
                hud.Journal(a.Texte, c);
                if (a.Grande)
                {
                    hud.Annonce(a.Texte, CouleurTon(a.Ton) == UIK.Creme ? UIK.Or : CouleurTon(a.Ton));
                    yield return Attendre(1.1f);
                }
                else yield return Attendre(0.2f);
            }
            else if (s is SDe) yield return StartCoroutine(De((SDe)s));
            else if (s is SPas) yield return StartCoroutine(Pas((SPas)s));
            else if (s is STeleport) yield return StartCoroutine(Teleport((STeleport)s));
            else if (s is SEnvol) yield return StartCoroutine(Envol((SEnvol)s));
            else if (s is SStat) yield return StartCoroutine(Stat((SStat)s));
            else if (s is SCombat) yield return StartCoroutine(Combat((SCombat)s));
            else if (s is SEffet) yield return StartCoroutine(Effet((SEffet)s));
            else if (s is SQuestion) yield return StartCoroutine(Question((SQuestion)s));
            else if (s is SNuit) yield return StartCoroutine(Nuit((SNuit)s));
            VerifierOie();
            hud.Rafraichir();
        }

        IEnumerator TourDebut(STourDebut s)
        {
            var j = partie.Joueurs[s.Joueur];
            foreach (var p in pions.Values) p.Actif(p.Joueur == j.Id);
            hud.Rafraichir();
            Color c = Pal.Joueurs[j.Couleur];
            hud.Banniere("Tour " + s.Tour + "  ·  " + j.Nom + (j.Humain ? "" : "  (bot)"), c);
            hud.Journal("— " + j.Nom + " joue —", c);
            cam.Suivre(pions[j.Id].transform, 12.5f);
            MajMarqueurs(j);
            if (j.Humain && Humains() > 1) hud.Annonce("À vous, " + j.Nom + " !", c);
            yield return Attendre(j.Humain ? 0.5f : 0.45f);
        }

        IEnumerator De(SDe s)
        {
            var p = pions[s.Joueur];
            var droite = cam.transform.right; droite.y = 0; droite.Normalize();
            var avant = cam.transform.forward; avant.y = 0; avant.Normalize();
            var arrivee = p.transform.position + droite * 0.95f - avant * 0.35f;
            arrivee.y = p.transform.position.y;
            var depart = arrivee - droite * 1.6f - avant * 1.2f + Vector3.up * 1.6f;
            var de = DeVue.Creer(transitoires, s.Faces);
            Sons.Jouer(Son.De);
            float duree = 1.0f / V;
            yield return StartCoroutine(de.Lancer(depart, arrivee, s.Valeur, duree));
            string txt = s.Bonus > 0 ? s.Valeur + " + " + s.Bonus : s.Valeur.ToString();
            hud.Flottant(de.transform.position + Vector3.up * 0.5f, txt, UIK.Or, 44);
            if (s.Raison != null && s.Raison != "Déplacement") hud.Journal(s.Raison + " : " + s.Valeur + (s.Bonus > 0 ? " + " + s.Bonus : ""), UIK.Creme);
            yield return Attendre(0.55f);
            StartCoroutine(Disparaitre(de.gameObject, 0.4f));
        }

        IEnumerator Disparaitre(GameObject go, float duree)
        {
            if (go == null) yield break;
            var e0 = go.transform.localScale;
            float t = 0;
            while (t < duree && go != null)
            {
                t += Time.deltaTime;
                go.transform.localScale = e0 * (1f - t / duree);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        IEnumerator Pas(SPas s)
        {
            var p = pions[s.Joueur];
            var vers = Place(s.Joueur);
            // la caméra regardait ailleurs (un combat, le Dragonnoie montré d'un clic) : elle reprend le héros qui marche
            if (partie.JoueurCourant.Id == s.Joueur && !cam.Suit && !cam.VueEnsemble) cam.Suivre(p.transform, 12.5f);
            Sons.Jouer(Son.Pas, 0.7f);
            yield return StartCoroutine(p.Sauter(vers, 0.3f / V, 0.5f));
            fumees.Fumee(vers, new Color(0.75f, 0.68f, 0.55f, 0.55f), 4, 0.25f);
            RangerCase(s.De, true);
            RangerCase(s.Vers, true, s.Joueur);
        }

        IEnumerator Teleport(STeleport s)
        {
            var p = pions[s.Joueur];
            etincelles.Spirale(p.transform.position, new Color(0.6f, 0.75f, 1f), 30, 2.5f);
            Sons.Jouer(Son.Sort, 0.7f);
            var vers = Place(s.Joueur);
            yield return StartCoroutine(p.Teleporter(vers));
            etincelles.Spirale(vers, new Color(0.6f, 0.75f, 1f), 30, 2.5f);
            RangerCase(s.Vers, true, s.Joueur);
            foreach (var j in partie.Joueurs) RangerCase(j.Case, true);
            if (partie.JoueurCourant.Id == s.Joueur) cam.Suivre(p.transform, 12.5f);
            yield return Attendre(0.2f);
        }

        IEnumerator Stat(SStat s)
        {
            var j = partie.Joueurs[s.Joueur];
            var tete = Tete(s.Joueur);
            string signe = s.Delta > 0 ? "+" : "−";
            int v = Mathf.Abs(s.Delta);
            switch (s.Stat)
            {
                case Regles.Stat.PV:
                    hud.Flottant(tete, signe + v + " PV", s.Delta > 0 ? UIK.Vert : UIK.Rouge);
                    if (s.Delta < 0) { pions[j.Id].Rig.Eclair(new Color(1f, 0.15f, 0.1f), 0.35f); etincelles.Explosion(tete - Vector3.up * 0.5f, new Color(1f, 0.3f, 0.2f), 10, 2.5f); }
                    break;
                case Regles.Stat.Or:
                    hud.Flottant(tete, signe + v + " or", s.Delta > 0 ? UIK.Or : new Color(0.9f, 0.55f, 0.3f));
                    if (s.Delta > 0) { etincelles.Pieces(pions[j.Id].transform.position, Mathf.Clamp(v / 2, 4, 24)); Sons.Jouer(Son.Or, 0.8f); }
                    break;
                case Regles.Stat.XP:
                    hud.Flottant(tete, signe + v + " XP", s.Delta > 0 ? UIK.Bleu : UIK.Violet);
                    break;
                case Regles.Stat.Niveau:
                    hud.Flottant(tete + Vector3.up * 0.3f, s.Delta > 0 ? "NIVEAU " + j.Niveau + " !" : "Niveau " + j.Niveau, s.Delta > 0 ? UIK.Or : UIK.Violet, 38);
                    break;
            }
            hud.Rafraichir();
            yield return Attendre(0.15f);
        }

        IEnumerator Effet(SEffet e)
        {
            PionVue p = e.Joueur >= 0 && pions.ContainsKey(e.Joueur) ? pions[e.Joueur] : null;
            PionVue cible = e.Cible >= 0 && pions.ContainsKey(e.Cible) ? pions[e.Cible] : null;
            switch (e.Effet)
            {
                case Regles.Effet.PiegePose:
                    Sons.Jouer(Son.Piege, 0.5f);
                    if (p != null) p.Anim.Jouer(Geste.Lancer);
                    fumees.Fumee(modele.Cases[e.Case], new Color(0.55f, 0.45f, 0.32f, 0.7f), 10, 0.5f);
                    MajMarqueurs(partie.JoueurCourant);
                    yield return Attendre(0.5f);
                    break;
                case Regles.Effet.PiegeDeclenche:
                    Sons.Jouer(Son.Piege);
                    if (cible != null) { cible.Anim.Jouer(Geste.Touche); cible.Rig.Eclair(new Color(1, 0.2f, 0.1f), 0.5f); }
                    etincelles.Explosion(modele.Cases[e.Case] + Vector3.up * 0.3f, new Color(1f, 0.55f, 0.25f), 28, 4f);
                    fumees.Fumee(modele.Cases[e.Case], new Color(0.4f, 0.35f, 0.3f, 0.7f), 14, 0.6f);
                    MajMarqueurs(partie.JoueurCourant);
                    yield return Attendre(0.6f);
                    break;
                case Regles.Effet.EmbuscadeTendue:
                    Sons.Jouer(Son.Page);
                    if (p != null) p.Anim.Jouer(Geste.Sort, 0.6f);
                    MajMarqueurs(partie.JoueurCourant);
                    yield return Attendre(0.5f);
                    break;
                case Regles.Effet.EmbuscadeDeclenchee:
                    Sons.Jouer(Son.Monstre, 0.6f);
                    MajMarqueurs(partie.JoueurCourant);
                    if (p != null && cible != null) { p.Regarder(cible.transform.position); cible.Regarder(p.transform.position); p.Anim.Jouer(Geste.Joie); }
                    yield return Attendre(0.5f);
                    break;
                case Regles.Effet.SortLance:
                    Sons.Jouer(Son.Sort);
                    if (p != null) { p.Anim.Jouer(Geste.Sort); if (cible != null) p.Regarder(cible.transform.position); }
                    if (p != null && cible != null && cible != p)
                    {
                        var col = e.Objet == "foudre" ? new Color(0.65f, 0.85f, 1f) : e.Objet == "lenteur" ? new Color(0.5f, 0.9f, 1f) : new Color(0.75f, 0.45f, 1f);
                        etincelles.Trait(Tete(p.Joueur), Tete(cible.Joueur), col, 40);
                        yield return Attendre(0.4f);
                        etincelles.Explosion(Tete(cible.Joueur), col, 24, 3f, 0.08f, 1f);
                        if (e.Objet == "foudre") { cible.Anim.Jouer(Geste.Touche); cible.Rig.Eclair(new Color(0.6f, 0.8f, 1f), 0.5f); }
                    }
                    yield return Attendre(0.4f);
                    break;
                case Regles.Effet.SortBloque:
                    if (p != null) etincelles.Explosion(Tete(p.Joueur), new Color(0.95f, 0.9f, 0.6f), 30, 2.5f, 0.06f, 0f);
                    Sons.Jouer(Son.Soin);
                    yield return Attendre(0.4f);
                    break;
                case Regles.Effet.Malediction:
                    if (p != null)
                    {
                        etincelles.Spirale(p.transform.position, new Color(0.62f, 0.3f, 0.95f), 36, 1.6f);
                        fumees.Fumee(p.transform.position, new Color(0.25f, 0.1f, 0.35f, 0.6f), 16, 0.5f);
                        p.Rig.Eclair(new Color(0.5f, 0.15f, 0.8f), 0.6f);
                    }
                    Sons.Jouer(Son.Malus, 0.8f);
                    yield return Attendre(0.5f);
                    break;
                case Regles.Effet.NiveauGagne:
                    if (p != null)
                    {
                        etincelles.Spirale(p.transform.position, new Color(1f, 0.82f, 0.35f), 60, 3.2f);
                        p.Anim.Jouer(Geste.Joie);
                        p.Rig.Eclair(new Color(1f, 0.85f, 0.4f), 0.8f);
                    }
                    Sons.Jouer(Son.Niveau);
                    yield return Attendre(0.6f);
                    break;
                case Regles.Effet.NiveauPerdu:
                    if (p != null)
                    {
                        fumees.Fumee(p.transform.position + Vector3.up * 0.5f, new Color(0.3f, 0.2f, 0.35f, 0.7f), 18, 0.5f);
                        p.Anim.Jouer(Geste.Touche);
                    }
                    Sons.Jouer(Son.Malus);
                    yield return Attendre(0.6f);
                    break;
                case Regles.Effet.KO:
                    if (p != null)
                    {
                        p.Anim.Jouer(Geste.Ko);
                        fumees.Fumee(p.transform.position, new Color(0.5f, 0.5f, 0.5f, 0.7f), 24, 0.6f);
                    }
                    Sons.Jouer(Son.Malus);
                    yield return Attendre(1.1f);
                    if (p != null) p.Anim.Relever();
                    break;
                case Regles.Effet.Soin:
                    if (p != null) etincelles.Spirale(p.transform.position, new Color(0.45f, 1f, 0.55f), 20, 1.8f);
                    Sons.Jouer(Son.Soin, 0.6f);
                    yield return Attendre(0.3f);
                    break;
                case Regles.Effet.Tresor:
                    etincelles.Explosion(modele.Cases[e.Case] + Vector3.up * 0.4f, new Color(1f, 0.85f, 0.4f), 26, 3f, 0.07f, 2f);
                    if (p != null) p.Anim.Jouer(Geste.Joie);
                    yield return Attendre(0.3f);
                    break;
                case Regles.Effet.Achat:
                    Sons.Jouer(Son.Or, 0.6f);
                    yield return Attendre(0.1f);
                    break;
                case Regles.Effet.QueteAcceptee:
                    Sons.Jouer(Son.Page);
                    MajMarqueurs(partie.JoueurCourant);
                    yield return Attendre(0.3f);
                    break;
                case Regles.Effet.QueteFinie:
                    if (p != null) { etincelles.Spirale(p.transform.position, new Color(1f, 0.9f, 0.5f), 40, 2.6f); p.Anim.Jouer(Geste.Victoire, 1f); }
                    Sons.Jouer(Son.Niveau, 0.8f);
                    MajMarqueurs(partie.JoueurCourant);
                    yield return Attendre(0.5f);
                    break;
                case Regles.Effet.Evenement:
                    Sons.Jouer(Son.Page);
                    if (p != null) etincelles.Explosion(Tete(p.Joueur) + Vector3.up * 0.4f, new Color(0.45f, 0.7f, 1f), 18, 2f, 0.06f, 0f);
                    yield return Attendre(0.2f);
                    break;
                case Regles.Effet.Victoire:
                    yield return Attendre(0.1f);
                    break;
                case Regles.Effet.OrDragonnoie:
                    yield return StartCoroutine(OrArrache(e));
                    break;
                case Regles.Effet.VolNocturne: yield return StartCoroutine(VolNocturne(e)); break;
                case Regles.Effet.TrouDeVer: yield return StartCoroutine(TrouDeVer(e)); break;
                case Regles.Effet.BonneNuit: yield return StartCoroutine(BonneNuit(e)); break;
                case Regles.Effet.AreneDebut: yield return StartCoroutine(EntrerArene(e)); break;
                case Regles.Effet.AreneFin: yield return StartCoroutine(ChampionArene(e)); break;
                case Regles.Effet.QueteRatee: yield return StartCoroutine(QueteRatee(e)); break;
            }
        }

        // ------------------------------------------------------------------ questions

        IEnumerator Question(SQuestion q)
        {
            var j = partie.Joueurs[q.Joueur];
            Color c = Pal.Joueurs[j.Couleur];
            bool montrerCases = q.Genre == GenreQuestion.Embranchement || q.Genre == GenreQuestion.Rodeur || q.Genre == GenreQuestion.ChoixCase;
            if (q.Genre == GenreQuestion.Evade) { yield return StartCoroutine(ChercherEvade(q)); yield break; }
            if (q.Genre == GenreQuestion.Monstre || q.Genre == GenreQuestion.Dragonnoie) yield return StartCoroutine(FaireSurgir(q.Monstre, j));
            if (j.Humain)
            {
                if (montrerCases) ile.MontrerChoix(q.Cases, Color.Lerp(c, Color.white, 0.35f));
                bool fini = false;
                fq.Montrer(q, partie, i => { q.Reponse = i; fini = true; });
                while (!fini) yield return null;
                ile.EffacerChoix();
                ile.Survoler(-1);
            }
            else
            {
                fq.MontrerReflexion(j.Nom + " réfléchit…", c);
                if (montrerCases) ile.MontrerChoix(q.Cases, Color.Lerp(c, Color.white, 0.35f));
                yield return Attendre(q.Genre == GenreQuestion.Action ? 0.55f : 0.75f);
                q.Reponse = ia.Repondre(partie, q);
                if (montrerCases && q.Reponse >= 0 && q.Reponse < q.Cases.Count && q.Cases[q.Reponse] >= 0)
                {
                    ile.Survoler(q.Cases[q.Reponse]);
                    yield return Attendre(0.4f);
                    ile.Survoler(-1);
                }
                ile.EffacerChoix();
                fq.CacherReflexion();
                // un bot qui ne se contente pas de lancer le dé l'annonce
                string code = q.CodeChoisi;
                if (q.Genre == GenreQuestion.Action && code != null && code != "de" && q.Reponse >= 0)
                    hud.Journal(j.Nom + " : " + q.Options[q.Reponse].ToLowerInvariant() + ".", c);
            }
            if ((q.Genre == GenreQuestion.Monstre && q.CodeChoisi == "fuir") || (q.Genre == GenreQuestion.Dragonnoie && q.CodeChoisi == "passer"))
            {
                EnleverMonstre(false);
                cam.Suivre(pions[j.Id].transform, 12.5f);       // la caméra reprend le héros qui repart
            }
        }

        // ------------------------------------------------------------------ fin

        IEnumerator Victoire()
        {
            etat = Etat.Victoire;
            if (nuitEnCours) yield return StartCoroutine(AubeVictoire());   // gagnée en pleine nuit : le jour se lève d'abord
            var g = partie.Joueurs[partie.Gagnant];
            var p = pions[g.Id];
            Color c = Pal.Joueurs[g.Couleur];
            hud.Banniere("Victoire de " + g.Nom + " !", c);
            hud.Annonce(partie.VaincuDragonnoie ? g.Nom + " a terrassé le Dragonnoie !" : g.Nom + " atteint le niveau " + g.Niveau + " !", UIK.Or);
            if (partie.VaincuDragonnoie && oieVue != null) cam.Cadrer(p.transform.position, oieVue.transform.position);
            else cam.Regarder(p.transform.position + Vector3.up * 0.6f, 7f, 30f);
            Sons.Jouer(Son.Victoire);
            for (int k = 0; k < 6; k++)
            {
                p.Anim.Jouer(Geste.Victoire);
                var pos = p.transform.position + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(2.5f, 4f), Random.Range(-1f, 2.5f));
                etincelles.Explosion(pos, k % 2 == 0 ? (Color)c : new Color(1f, 0.85f, 0.4f), 50, 4.5f, 0.07f, 1.5f);
                yield return new WaitForSeconds(0.45f);
            }
            yield return new WaitForSeconds(0.8f);
            victoire = new EcranVictoire(ui, partie, () => Lancer(derniersReglages), AllerTitre);
            boutonsJeu.gameObject.SetActive(false);
        }
    }
}

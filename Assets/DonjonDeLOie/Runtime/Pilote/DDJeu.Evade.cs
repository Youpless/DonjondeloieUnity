// Donjon de l'Oie — le mini-jeu de l'évadé à l'écran : la caméra zoome sur la ville en vue presque isométrique (le
// champ se resserre pendant qu'elle recule), la ville se floute (flou de profondeur d'URP), le marché surgit en
// diorama au-dessus des toits, et l'on a trente secondes pour cliquer sur le visage de l'évadé, caché dans la foule.
// Un bot cherche à sa place, loupe à la main : il réussit ou non selon la réponse de l'IA, tirée d'avance.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Modeles;
using DonjonDeLOie.Interface;

namespace DonjonDeLOie.Pilote
{
    public sealed partial class DDJeu
    {
        /// Le champ de vision de la vue du diorama : étroit, presque une vue isométrique. La molette le resserre encore
        /// (jusqu'à ZoomMax fois), vers le point visé par la souris.
        const float ChampDiorama = 16f, ZoomMax = 2.6f;
        /// Le flou de la ville : net jusqu'à DebutFlou mètres derrière le point visé, flou complet à FinFlou.
        const float DebutFlou = 3.5f, FinFlou = 15f;
        Diorama diorama;
        EcranEvade ecranEvade;
        bool evadeEnCours;

        IEnumerator ChercherEvade(SQuestion q)
        {
            var j = partie.Joueurs[q.Joueur];
            var qu = q.Quete;
            int iTrouve = q.Codes.IndexOf("trouve"), iRate = q.Codes.IndexOf("rate");
            q.Reponse = iRate;
            if (qu == null || iTrouve < 0 || iRate < 0) yield break;
            bool humain = j.Humain;
            bool reussiteBot = !humain && ia.Repondre(partie, q) == iTrouve;
            if (ecranEvade == null) ecranEvade = new EcranEvade(ui, canvas);
            evadeEnCours = true;
            hud.ModeMiniJeu(true);
            boutonsJeu.gameObject.SetActive(false);
            cam.CommandesActives = false;
            if (cam.VueEnsemble) cam.BasculerEnsemble();
            hud.Annonce("Avis de recherche : " + qu.NomEvade, UIK.Or);
            Sons.Jouer(Son.Page);

            // le diorama flotte au-dessus de la ville, assez haut pour que toute la ville soit derrière lui (et floue)
            var ville = modele.Cases[j.Case];
            float sommet = ile.SommetAutour(ville, 8f);
            var centre = new Vector3(ville.x, Mathf.Max(ville.y + 13f, sommet + 4f), ville.z);
            float lacetCam = LacetDiagonal(cam.Lacet);
            var allure = HerosModeles.AllureEvade(qu.Graine, qu.NomEvade, qu.Feminin);
            diorama = Diorama.Creer(transitoires, centre, lacetCam + 180f - Diorama.LacetVue, qu.Graine, allure);
            float s = canvas.scaleFactor > 0 ? canvas.scaleFactor : 1f, w = Mathf.Max(1f, Screen.width), hEcran = Mathf.Max(1f, Screen.height);
            Vector3 cible; float distance, lacet;
            Diorama.Cadrage(diorama.transform, ChampDiorama, cam.Cam.aspect, Mathf.Clamp(EcranEvade.LargeurAvis * s / w, 0.1f, 0.4f),
                EcranEvade.HauteurSablier * s / hEcran, EcranEvade.HauteurConsigne * s / hEcran, out cible, out distance, out lacet);
            StartCoroutine(diorama.Batir(true));
            yield return StartCoroutine(Zoomer(cible, distance, lacet, Diorama.TangageVue, ChampDiorama, 1.7f / V, 1f));
            while (diorama != null && !diorama.Pret) yield return null;
            if (diorama == null) { FinirEvade(); yield break; }

            // il surgit, pièce après pièce
            ecranEvade.Montrer(qu, HerosModeles.IconePortrait(qu.Graine, qu.NomEvade, qu.Feminin), j.Nom, humain, Pal.Joueurs[j.Couleur]);
            ecranEvade.Temps(Donnees.Reglages.DureeEvade, Donnees.Reglages.DureeEvade);
            float dernierPop = -1f;
            yield return StartCoroutine(diorama.Apparaitre(pos =>
            {
                fumees.Fumee(pos, new Color(0.85f, 0.8f, 0.7f, 0.5f), 3, 0.2f);
                if (Time.time - dernierPop > 0.08f) { dernierPop = Time.time; Sons.Jouer(Son.Pas, 0.3f); }
            }, V));

            // la recherche
            diorama.EnJeu = true;
            var trouve = new bool[1];
            if (humain) yield return StartCoroutine(RechercheHumaine(qu, trouve));
            else { trouve[0] = reussiteBot; yield return StartCoroutine(RechercheBot(qu, reussiteBot)); }
            diorama.EnJeu = false;
            q.Reponse = trouve[0] ? iTrouve : iRate;
            // la molette avait zoomé ? on revoit tout le marché pour le dénouement
            if (cam.Cam.fieldOfView < ChampDiorama - 0.1f) yield return StartCoroutine(Zoomer(cible, distance, lacet, Diorama.TangageVue, ChampDiorama, 0.45f, 1f));

            // le dénouement
            var ev = diorama.Evade;
            if (trouve[0])
            {
                ecranEvade.Message(qu.Feminin ? "La voilà !" : "Le voilà !", UIK.Or);
                Sons.Jouer(Son.Niveau, 0.8f);
                ev.Rig.Eclair(new Color(1f, 0.85f, 0.4f), 1.2f);
                etincelles.Spirale(ev.Rig.transform.position, new Color(1f, 0.85f, 0.4f), 40, 1.4f);
                yield return StartCoroutine(diorama.Attrape());
                etincelles.Explosion(ev.Tete + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.4f), 30, 3f, 0.06f, 1f);
                yield return Attendre(1.0f);
            }
            else
            {
                ecranEvade.Temps(0f, Donnees.Reglages.DureeEvade);
                ecranEvade.Message(humain ? "Temps écoulé !" : "Raté !", UIK.Rouge);
                Sons.Jouer(Son.Malus, 0.8f);
                ev.Rig.Eclair(new Color(1f, 0.35f, 0.25f), 1.2f);
                yield return Attendre(0.7f);
                ecranEvade.Message(qu.Feminin ? "Elle s'enfuit !" : "Il s'enfuit !", UIK.Rouge);
                yield return StartCoroutine(diorama.Fuite());
                yield return Attendre(0.3f);
            }

            // le diorama se replie, la caméra revient au héros
            ecranEvade.Cacher();
            yield return StartCoroutine(diorama.Disparaitre(0.55f / V));
            Destroy(diorama.gameObject);
            diorama = null;
            var heros = pions[j.Id].transform.position + Vector3.up * 0.6f;
            yield return StartCoroutine(Zoomer(heros, 12.5f, cam.Lacet, 50f, CameraRig.ChampNormal, 1.1f / V, 0f));
            FinirEvade();
            cam.Suivre(pions[j.Id].transform, 12.5f);
        }

        /// Trente secondes (réglables) pour cliquer sur le bon visage ; une erreur en coûte trois. La molette zoome vers
        /// la souris (sans sortir du cadre de départ).
        IEnumerator RechercheHumaine(Quete qu, bool[] trouve)
        {
            float total = Mathf.Max(5f, Donnees.Reglages.DureeEvade), reste = total;
            int dernierTic = -1;
            Vector3 cible0 = cam.Cible; float zoom = 1f;
            while (reste > 0f && diorama != null)
            {
                if (!pause.Ouvert)
                {
                    reste -= Time.deltaTime;
                    bool surUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                    float molette = DDInput.Molette;
                    if (Mathf.Abs(molette) > 0.01f && !surUI) zoom = ZoomerVers(cible0, zoom, Mathf.Clamp(zoom * (1f + molette * 0.18f), 1f, ZoomMax));
                    if (DDInput.ClicGauche && !surUI)
                    {
                        var h = diorama.Toucher(DDInput.PositionSouris, cam.Cam);
                        if (h != null && h.Evade) { trouve[0] = true; break; }
                        if (h != null)
                        {
                            reste -= 3f;
                            Sons.Jouer(Son.Malus, 0.6f);
                            h.Rig.Eclair(new Color(1f, 0.25f, 0.2f), 0.5f);
                            h.Anim.Jouer(Geste.Touche);
                            ecranEvade.Message(qu.Feminin ? "Ce n'est pas elle !  −3 s" : "Ce n'est pas lui !  −3 s", UIK.Rouge);
                        }
                    }
                    int s = Mathf.CeilToInt(reste);
                    if (s <= 5 && s > 0 && s != dernierTic) { dernierTic = s; Sons.Jouer(Son.Clic, 0.6f); }
                }
                ecranEvade.Temps(reste, total);
                yield return null;
            }
        }

        /// Le zoom de la molette : le champ se resserre (ou s'élargit) et la cible glisse dans le plan de l'image pour que
        /// le point sous la souris reste sous la souris ; la vue ne sort jamais du cadre de départ (cible0, zoom 1).
        float ZoomerVers(Vector3 cible0, float zoom, float nouveau)
        {
            float t0 = Mathf.Tan(ChampDiorama * 0.5f * Mathf.Deg2Rad);
            float ty = t0 / zoom, ty1 = t0 / nouveau, asp = cam.Cam.aspect, d = cam.Distance;
            var souris = DDInput.PositionSouris;
            float sx = souris.x / Mathf.Max(1f, Screen.width) * 2f - 1f, sy = souris.y / Mathf.Max(1f, Screen.height) * 2f - 1f;
            var dr = cam.transform.right; var ht = cam.transform.up;
            var c = cam.Cible + dr * (sx * d * (ty - ty1) * asp) + ht * (sy * d * (ty - ty1));
            // rester dans le cadre de départ
            var ecart = c - cible0;
            float jx = d * (t0 - ty1) * asp, jy = d * (t0 - ty1);
            float ex = Mathf.Clamp(Vector3.Dot(ecart, dr), -jx, jx), ey = Mathf.Clamp(Vector3.Dot(ecart, ht), -jy, jy);
            c = cible0 + dr * ex + ht * ey;
            cam.Fixer(c, d, cam.Lacet, cam.Tangage, 2f * Mathf.Atan(ty1) * Mathf.Rad2Deg);
            return nouveau;
        }

        /// Un bot cherche, loupe à la main : quelques visages, puis l'évadé (réussite) ou un sosie (raté).
        IEnumerator RechercheBot(Quete qu, bool reussite)
        {
            float total = Mathf.Max(5f, Donnees.Reglages.DureeEvade);
            var cibles = new List<Diorama.Habitant>();
            int n = reussite ? 2 + Random.Range(0, 2) : 3;
            for (int i = 0; i < n; i++) cibles.Add(diorama.Badaud());
            cibles.Add(reussite ? diorama.Evade : diorama.UnSosie());
            float finSablier = reussite ? total * Random.Range(0.3f, 0.7f) : total * 0.15f;
            float duree = (reussite ? 3.2f : 3.6f) / V, ecoule = 0f;
            var pos = new Vector2(Screen.width * 0.62f, Screen.height * 0.35f);
            for (int i = 0; i < cibles.Count && diorama != null; i++)
            {
                var h = cibles[i];
                float seg = duree / cibles.Count, u = 0f;
                var de = pos;
                while (u < seg)
                {
                    float dt = Time.deltaTime;
                    u += dt; ecoule += dt;
                    var e = cam.Ecran(h.Tete);
                    pos = Vector2.Lerp(de, new Vector2(e.x, e.y), Mathf.SmoothStep(0, 1, Mathf.Clamp01(u / (seg * 0.7f))));
                    ecranEvade.Loupe(pos, true);
                    ecranEvade.Temps(Mathf.Lerp(total, finSablier, ecoule / duree), total);
                    yield return null;
                }
                if (i == cibles.Count - 1 && !reussite && h != null)
                {
                    Sons.Jouer(Son.Malus, 0.5f);
                    h.Rig.Eclair(new Color(1f, 0.25f, 0.2f), 0.5f);
                    h.Anim.Jouer(Geste.Touche);
                    ecranEvade.Message(qu.Feminin ? "Ce n'est pas elle !" : "Ce n'est pas lui !", UIK.Rouge);
                    // le temps file jusqu'au bout
                    float t = 0f;
                    while (t < 0.8f / V) { t += Time.deltaTime; ecranEvade.Temps(Mathf.Lerp(finSablier, 0f, t * V / 0.8f), total); yield return null; }
                }
            }
            ecranEvade.Loupe(pos, false);
        }

        /// Mouvement de caméra image par image : la cible, la distance, le lacet, le tangage et le champ glissent ensemble ;
        /// la hauteur vue au point visé varie régulièrement (un « travelling compensé » : le décor se tasse quand le champ
        /// se resserre). Le flou de profondeur va de sa force actuelle à « flou », réglé sur la distance du moment.
        IEnumerator Zoomer(Vector3 cible, float distance, float lacet, float tangage, float champ, float duree, float flou)
        {
            Vector3 c0 = cam.Cible; float d0 = cam.Distance, l0 = cam.Lacet, t0 = cam.Tangage, f0 = cam.Cam.fieldOfView;
            float h0 = 2f * d0 * Mathf.Tan(f0 * 0.5f * Mathf.Deg2Rad), h1 = 2f * distance * Mathf.Tan(champ * 0.5f * Mathf.Deg2Rad);
            float flou0 = Ambiance.ForceFlou;
            float t = 0;
            while (t < duree)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duree));
                float f = Mathf.Lerp(f0, champ, k);
                float d = Mathf.Lerp(h0, h1, k) / (2f * Mathf.Tan(f * 0.5f * Mathf.Deg2Rad));
                cam.Fixer(Vector3.Lerp(c0, cible, k), d, Mathf.LerpAngle(l0, lacet, k), Mathf.Lerp(t0, tangage, k), f);
                Ambiance.Flou(Mathf.Lerp(flou0, flou, k), d + DebutFlou, d + FinFlou);
                yield return null;
            }
            cam.Fixer(cible, distance, lacet, tangage, champ);
            Ambiance.Flou(flou, distance + DebutFlou, distance + FinFlou);
        }

        /// Le lacet de la vue du diorama : la diagonale (45° + k·90°) la plus proche, sauf celle qui fait face au soleil
        /// (les visages seraient à contre-jour).
        static float LacetDiagonal(float lacet)
        {
            float best = 45f, bd = 999f;
            foreach (var l in new[] { 45f, 135f, 315f })
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(lacet, l));
                if (d < bd) { bd = d; best = l; }
            }
            return lacet + Mathf.DeltaAngle(lacet, best);
        }

        /// Une quête d'avis de recherche ratée : un nuage de dépit au-dessus du héros.
        IEnumerator QueteRatee(SEffet e)
        {
            if (e.Joueur >= 0 && pions.ContainsKey(e.Joueur))
            {
                var p = pions[e.Joueur];
                fumees.Fumee(p.transform.position + Vector3.up * 0.5f, new Color(0.35f, 0.3f, 0.34f, 0.7f), 14, 0.5f);
                p.Anim.Jouer(Geste.Touche);
            }
            Sons.Jouer(Son.Malus, 0.7f);
            MajMarqueurs(partie.JoueurCourant);
            yield return Attendre(0.4f);
        }

        /// Fin du mini-jeu (ou partie interrompue) : plus de diorama, plus de flou, l'écran de jeu revient.
        void FinirEvade()
        {
            if (diorama != null) { Destroy(diorama.gameObject); diorama = null; }
            if (ecranEvade != null) ecranEvade.Cacher();
            if (Ambiance.ForceFlou > 0f) Ambiance.Flou(0f, 0f, 0f);
            if (!evadeEnCours) return;
            evadeEnCours = false;
            if (cam != null) { cam.RetablirChamp(); cam.CommandesActives = etat == Etat.Partie && !pause.Ouvert; }
            if (hud != null) hud.ModeMiniJeu(false);
            if (boutonsJeu != null) boutonsJeu.gameObject.SetActive(etat == Etat.Partie);
        }
    }
}

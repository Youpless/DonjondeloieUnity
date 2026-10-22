// Donjon de l'Oie — l'écran titre (l'île tourne lentement derrière) et le menu pause.
using System;
using UnityEngine;
using UnityEngine.UI;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Interface
{
    public sealed class EcranTitre
    {
        public RectTransform R;
        Text but;

        public EcranTitre(RectTransform racine, Action nouvelle, Action regles, Action quitter)
        {
            R = UIK.Remplir(UIK.Rect(racine, "Écran titre"));
            var voile = UIK.Image(R, "Voile", UIK.DegradeVertical, new Color(0.05f, 0.03f, 0.02f, 0.65f));
            UIK.Remplir(voile.rectTransform);
            voile.rectTransform.localScale = new Vector3(1, -1, 1);   // sombre en bas
            var lueur = UIK.Image(R, "Lueur", UIK.Lueur, new Color(0, 0, 0, 0.55f));
            UIK.Placer(lueur.rectTransform, 0.5f, 1f, 0, -40, 1500, 420, 0.5f, 1f);
            var titre = UIK.Texte(R, "Titre", "DONJON DE L'OIE", 104, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Placer(titre.rectTransform, 0.5f, 1f, 0, -90, 1600, 130, 0.5f, 1f);
            var ol = titre.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.25f, 0.1f, 0.02f, 1f); ol.effectDistance = new Vector2(3, -3);
            var sous = UIK.Texte(R, "Sous-titre", "Un jeu de plateau dans les donjons · de 1 à 6 aventuriers sur le même ordinateur", 28, UIK.Creme, TextAnchor.MiddleCenter, false);
            UIK.Placer(sous.rectTransform, 0.5f, 1f, 0, -224, 1500, 40, 0.5f, 1f);
            but = UIK.Texte(R, "But", TexteBut(), 22, new Color(0.95f, 0.85f, 0.65f), TextAnchor.MiddleCenter, false);
            UIK.Placer(but.rectTransform, 0.5f, 1f, 0, -266, 1500, 34, 0.5f, 1f);

            var b1 = UIK.Bouton(R, "Nouvelle partie", "Nouvelle partie", Style.BoutonOr, () => nouvelle(), 32);
            UIK.Placer((RectTransform)b1.transform, 0.5f, 0f, 0, 300, 420, 78, 0.5f, 0f);
            var b2 = UIK.Bouton(R, "Règles", "Règles du jeu", Style.Bouton, () => regles(), 26);
            UIK.Placer((RectTransform)b2.transform, 0.5f, 0f, 0, 210, 420, 64, 0.5f, 0f);
            var b3 = UIK.Bouton(R, "Quitter", "Quitter", Style.Bouton, () => quitter(), 26);
            UIK.Placer((RectTransform)b3.transform, 0.5f, 0f, 0, 134, 420, 64, 0.5f, 0f);
            var aide = UIK.Texte(R, "Commandes", "Molette : zoom  ·  clic droit glissé ou flèches : tourner  ·  clic milieu : déplacer  ·  Espace : tout le plateau  ·  Échap : menu", 19, new Color(0.85f, 0.8f, 0.7f), TextAnchor.MiddleCenter, false);
            UIK.Placer(aide.rectTransform, 0.5f, 0f, 0, 50, 1700, 30, 0.5f, 0f);
        }

        /// Le but du jeu, selon le catalogue (avec ou sans Dragonnoie).
        static string TexteBut()
        {
            return (Donnees.Dragonnoie != null ? "Premier au niveau 10 ou vainqueur du Dragonnoie : la couronne est à vous."
                                               : "Premier au niveau 10 : la couronne est à vous.")
                   + " Monstres, duels, pièges et malédictions vous attendent.";
        }

        public void Visible(bool v)
        {
            if (v) but.text = TexteBut();
            R.gameObject.SetActive(v);
        }
    }

    public sealed class MenuPause
    {
        public RectTransform R;
        Text musique, effets;

        public MenuPause(RectTransform racine, Action reprendre, Action regles, Action abandonner)
        {
            R = UIK.Remplir(UIK.Rect(racine, "Pause"));
            var voile = UIK.Image(R, "Voile", UIK.Blanc, new Color(0, 0, 0, 0.55f));
            UIK.Remplir(voile.rectTransform);
            voile.raycastTarget = true;
            var p = UIK.Placer(UIK.Panneau(R, "Panneau", Style.Bois), 0.5f, 0.5f, 0, 0, 520, 520);
            var t = UIK.Texte(p, "Titre", "Pause", 44, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Placer(t.rectTransform, 0.5f, 1f, 0, -24, 400, 56, 0.5f, 1f);
            var b1 = UIK.Bouton(p, "Reprendre", "Reprendre", Style.BoutonOr, () => reprendre(), 26);
            UIK.Placer((RectTransform)b1.transform, 0.5f, 1f, 0, -100, 380, 62, 0.5f, 1f);
            var b2 = UIK.Bouton(p, "Règles", "Règles du jeu", Style.Bouton, () => regles(), 24);
            UIK.Placer((RectTransform)b2.transform, 0.5f, 1f, 0, -176, 380, 58, 0.5f, 1f);
            musique = Reglage(p, -260, "Musique", () => Sons.VolumeMusique, v => Sons.ReglerVolumes(Sons.VolumeEffets, v));
            effets = Reglage(p, -320, "Effets", () => Sons.VolumeEffets, v => Sons.ReglerVolumes(v, Sons.VolumeMusique));
            var b3 = UIK.Bouton(p, "Abandonner", "Retour au menu principal", Style.BoutonRouge, () => abandonner(), 22);
            UIK.Placer((RectTransform)b3.transform, 0.5f, 0f, 0, 28, 380, 58, 0.5f, 0f);
            Visible(false);
        }

        Text Reglage(RectTransform p, float y, string nom, Func<float> lire, Action<float> ecrire)
        {
            var l = UIK.Texte(p, nom, "", 22, UIK.Creme, TextAnchor.MiddleCenter, false);
            UIK.Placer(l.rectTransform, 0.5f, 1f, 0, y, 240, 44, 0.5f, 1f);
            Action maj = () => l.text = nom + " : " + Mathf.RoundToInt(lire() * 100) + " %";
            maj();
            var moins = UIK.Bouton(p, "Moins", "−", Style.Bouton, () => { ecrire(Mathf.Clamp01(lire() - 0.1f)); maj(); }, 28);
            UIK.Placer((RectTransform)moins.transform, 0.5f, 1f, -170, y, 56, 44, 0.5f, 1f);
            var plus = UIK.Bouton(p, "Plus", "+", Style.Bouton, () => { ecrire(Mathf.Clamp01(lire() + 0.1f)); maj(); }, 28);
            UIK.Placer((RectTransform)plus.transform, 0.5f, 1f, 170, y, 56, 44, 0.5f, 1f);
            return l;
        }

        public bool Ouvert { get { return R.gameObject.activeSelf; } }
        public void Visible(bool v) { R.gameObject.SetActive(v); R.SetAsLastSibling(); }
    }
}

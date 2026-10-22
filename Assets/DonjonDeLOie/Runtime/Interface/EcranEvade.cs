// Donjon de l'Oie — l'écran du mini-jeu de l'évadé : l'avis de recherche à gauche (portrait, nom, récompense), le
// sablier en haut, la consigne en bas, les messages (« Ce n'est pas lui ! », « Le voilà ! ») et la loupe d'un bot
// qui cherche à sa place.
using UnityEngine;
using UnityEngine.UI;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Interface
{
    public sealed class EcranEvade
    {
        /// La largeur prise à gauche par l'avis (unités de l'interface, référence 1920 × 1080) : la caméra décale le
        /// diorama d'autant vers la droite.
        public const float LargeurAvis = 440f;
        /// Les bandes prises en haut (le sablier) et en bas (la consigne), mêmes unités.
        public const float HauteurSablier = 116f, HauteurConsigne = 76f;
        readonly RectTransform racine;
        readonly Canvas canvas;
        RectTransform p, loupe;
        Image barre; Text temps, message;
        CanvasGroup messageGroupe;
        float messageT = 99f;

        public EcranEvade(RectTransform racine, Canvas canvas) { this.racine = racine; this.canvas = canvas; }

        public bool Ouvert { get { return p != null; } }

        public void Cacher() { if (p != null) Object.Destroy(p.gameObject); p = null; loupe = null; message = null; }

        /// « icone » : l'icône du portrait ; « chercheur » : le nom du héros (pour un bot) ; humain : consigne et clics.
        public void Montrer(Quete qu, string icone, string chercheur, bool humain, Color couleur)
        {
            Cacher();
            p = UIK.Remplir(UIK.Rect(racine, "Mini-jeu de l'évadé"));
            var encre = new Color(0.3f, 0.2f, 0.12f);
            // l'avis de recherche
            var avis = UIK.Placer(UIK.Panneau(p, "Avis de recherche", Style.Parchemin), 0f, 0.5f, 24, 0, 400, 650, 0f, 0.5f);
            var rech = UIK.Texte(avis, "Recherché", qu.Feminin ? "RECHERCHÉE" : "RECHERCHÉ", 48, new Color(0.64f, 0.15f, 0.11f), TextAnchor.UpperCenter, true, false);
            UIK.Placer(rech.rectTransform, 0.5f, 1, 0, -16, 380, 58, 0.5f, 1);
            var cadre = UIK.Image(avis, "Cadre", UIK.Plein, new Color(0.36f, 0.24f, 0.14f));
            UIK.Placer(cadre.rectTransform, 0.5f, 1, 0, -82, 306, 306, 0.5f, 1);
            var fond = UIK.Image(cadre.transform, "Fond", UIK.Plein, new Color(0.86f, 0.8f, 0.66f));
            UIK.Remplir(fond.rectTransform, 8, 8, 8, 8);
            var portrait = UIK.IconeImage(cadre.transform, icone, 286);
            UIK.Placer(portrait.rectTransform, 0.5f, 0.5f, 0, 0, 286, 286);
            var nom = UIK.Texte(avis, "Nom", qu.NomEvade, 31, UIK.Encre, TextAnchor.UpperCenter, true, false);
            UIK.Placer(nom.rectTransform, 0.5f, 1, 0, -400, 380, 42, 0.5f, 1);
            var motif = UIK.Texte(avis, "Motif", (qu.Feminin ? "Évadée" : "Évadé") + " des geôles de " + (string.IsNullOrEmpty(qu.Ville) ? "Valcreux" : qu.Ville), 19, encre, TextAnchor.UpperCenter, false, false);
            UIK.Placer(motif.rectTransform, 0.5f, 1, 0, -444, 380, 26, 0.5f, 1);
            var rec = UIK.Texte(avis, "Récompense", "Récompense : <b>" + qu.RecompenseOr + " or</b> et <b>" + qu.RecompenseXP + " XP</b>", 23,
                new Color(0.5f, 0.32f, 0.05f), TextAnchor.UpperCenter, false, false);
            UIK.Placer(rec.rectTransform, 0.5f, 1, 0, -482, 380, 30, 0.5f, 1);
            var note = UIK.Texte(avis, "Indice", "Seule sa tête dépasse : comparez bien les visages.\nGare aux sosies !", 18, encre, TextAnchor.UpperCenter, false, false);
            UIK.Placer(note.rectTransform, 0.5f, 1, 0, -538, 360, 60, 0.5f, 1);
            // le sablier, au-dessus du diorama
            float cx = LargeurAvis * 0.5f;
            var haut = UIK.Placer(UIK.Panneau(p, "Sablier", Style.Bois), 0.5f, 1f, cx, -14, 660, 96, 0.5f, 1f);
            var bande = UIK.Image(haut, "Couleur", UIK.Plein, couleur);
            UIK.Placer(bande.rectTransform, 0f, 0.5f, 10, 0, 12, 72, 0f, 0.5f);
            var ic = UIK.IconeImage(haut, "affiche", 44);
            UIK.Placer(ic.rectTransform, 0f, 0.5f, 30, 0, 44, 44, 0f, 0.5f);
            string consigne = humain ? "Trouvez " + qu.NomEvade + " !" : chercheur + " cherche " + qu.NomEvade + "…";
            var titre = UIK.Texte(haut, "Titre", consigne, 25, UIK.Creme, TextAnchor.UpperLeft, true);
            UIK.Placer(titre.rectTransform, 0f, 1f, 88, -10, 540, 34, 0f, 1f);
            RectTransform cadreBarre;
            barre = UIK.Barre(haut, "Temps", UIK.Or, out cadreBarre);
            UIK.Placer(cadreBarre, 0f, 0f, 88, 16, 450, 24, 0f, 0f);
            temps = UIK.Texte(haut, "Secondes", "", 26, UIK.Or, TextAnchor.MiddleRight, true);
            UIK.Placer(temps.rectTransform, 1f, 0f, -16, 10, 90, 36, 1f, 0f);
            // la consigne
            if (humain)
            {
                var bas = UIK.Placer(UIK.Panneau(p, "Consigne", Style.Verre), 0.5f, 0f, cx, 18, 780, 52, 0.5f, 0f);
                var tb = UIK.Texte(bas, "Texte", "Cliquez sur son visage  ·  molette : zoomer  ·  une erreur coûte 3 secondes", 20, UIK.Creme, TextAnchor.MiddleCenter, false);
                UIK.Remplir(tb.rectTransform, 12, 2, 12, 2);
            }
            // les messages
            message = UIK.Texte(p, "Message", "", 46, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Placer(message.rectTransform, 0.5f, 1f, cx, -170, 1000, 70, 0.5f, 0.5f);
            var ol = message.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.12f, 0.06f, 0.02f, 0.9f); ol.effectDistance = new Vector2(2, -2);
            messageGroupe = message.gameObject.AddComponent<CanvasGroup>();
            messageGroupe.alpha = 0; messageGroupe.blocksRaycasts = false;
            messageT = 99f;
            // la loupe du bot
            if (!humain)
            {
                var im = UIK.IconeImage(p, "loupe", 104);
                loupe = im.rectTransform;
                loupe.anchorMin = loupe.anchorMax = Vector2.zero;
                loupe.pivot = new Vector2(4.5f / 11f, 6.5f / 11f);
                loupe.gameObject.SetActive(false);
            }
        }

        public void Temps(float reste, float total)
        {
            if (p == null) return;
            reste = Mathf.Max(0f, reste);
            barre.fillAmount = total > 0 ? Mathf.Clamp01(reste / total) : 0f;
            barre.color = reste <= 10f ? Color.Lerp(UIK.Rouge, UIK.Or, reste / 10f) : UIK.Or;
            temps.text = Mathf.CeilToInt(reste) + " s";
            temps.color = reste <= 5f ? UIK.Rouge : UIK.Or;
        }

        public void Message(string s, Color c)
        {
            if (message == null) return;
            message.text = s; message.color = c; messageT = 0f;
        }

        /// À chaque image : le message apparaît, reste, s'efface.
        public void Animer(float dt)
        {
            if (message == null) return;
            messageT += dt;
            messageGroupe.alpha = messageT < 0.12f ? messageT / 0.12f : messageT < 1.3f ? 1f : Mathf.Clamp01(1f - (messageT - 1.3f) / 0.4f);
            message.transform.localScale = Vector3.one * (messageT < 0.12f ? Mathf.Lerp(1.35f, 1f, messageT / 0.12f) : 1f);
        }

        /// La loupe du bot sur ce point de l'écran (en pixels) ; « montrer » faux : cachée.
        public void Loupe(Vector2 ecran, bool montrer)
        {
            if (loupe == null) return;
            loupe.gameObject.SetActive(montrer);
            float s = canvas != null && canvas.scaleFactor > 0 ? canvas.scaleFactor : 1f;
            loupe.anchoredPosition = ecran / s;
        }
    }
}

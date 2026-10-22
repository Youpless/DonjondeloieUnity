// Donjon de l'Oie — l'affichage en jeu : fiches des joueurs, bannière du tour, journal, grandes annonces,
// textes flottants au-dessus des pions et étiquettes de noms.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;
using DonjonDeLOie.Monde;

namespace DonjonDeLOie.Interface
{
    public sealed class Hud : MonoBehaviour
    {
        public RectTransform Racine;           // couvre l'écran
        public Canvas Canvas;
        public CameraRig Camera;
        Partie partie;
        readonly List<Fiche> fiches = new List<Fiche>();
        RectTransform banniere; Text banniereTexte; Image banniereCouleur;
        Button oie; Text oieTexte;
        RectTransform nuit; Text nuitTexte;              // la prochaine nuit, sous l'encart du Dragonnoie
        bool miniJeu;                                    // le mini-jeu de l'évadé : seuls restent les annonces et les textes flottants
        public System.Action MontrerOie;                // clic sur l'encart du Dragonnoie
        RectTransform journal; readonly List<Text> lignes = new List<Text>(); readonly List<string> messages = new List<string>();
        RectTransform annonce; Text annonceTexte; CanvasGroup annonceGroupe; float annonceT = 99f;
        RectTransform calqueMonde;
        readonly List<TexteFlottant> flottants = new List<TexteFlottant>();
        readonly Dictionary<int, Text> etiquettes = new Dictionary<int, Text>();
        public System.Func<int, Transform> PionDe;   // joueur → transform du pion
        public System.Func<int, float> HauteurPion;

        sealed class Fiche
        {
            public RectTransform R; public Image Halo, Bande; public Text Nom, Classe, Niveau, PV, Or, Atk, Def, Etats; public Image BarrePV;
            public Image[] XP; public int Joueur;
        }

        sealed class TexteFlottant { public Text T; public Vector3 Monde; public float Vie, Duree; public Vector2 Decalage; }

        public static Hud Creer(RectTransform racine, Canvas canvas)
        {
            var go = new GameObject("HUD", typeof(RectTransform));
            go.transform.SetParent(racine, false);
            var h = go.AddComponent<Hud>();
            h.Racine = UIK.Remplir((RectTransform)go.transform);
            h.Canvas = canvas;
            h.Batir();
            return h;
        }

        void Batir()
        {
            calqueMonde = UIK.Remplir(UIK.Rect(Racine, "Calque monde"));
            // Bannière du tour
            banniere = UIK.Placer(UIK.Panneau(Racine, "Bannière", Style.Bois), 0.5f, 1f, 0, -14, 640, 62, 0.5f, 1f);
            banniereCouleur = UIK.Image(banniere, "Couleur", UIK.Plein, Color.white);
            UIK.Placer(banniereCouleur.rectTransform, 0f, 0.5f, 10, 0, 14, 42, 0f, 0.5f);
            banniereCouleur.type = Image.Type.Sliced;
            banniereTexte = UIK.Texte(banniere, "Texte", "", 28, UIK.Creme, TextAnchor.MiddleCenter, true);
            UIK.Remplir(banniereTexte.rectTransform, 30, 0, 12, 0);
            // Le Dragonnoie, sous la chronique : sa force, sa case, son prochain envol, son nid (clic : la caméra le montre).
            // Même colonne et même largeur que la chronique : rien ne se chevauche, quel que soit le format de l'écran.
            oie = UIK.Bouton(Racine, "Dragonnoie", "", Style.Verre, () => { if (MontrerOie != null) MontrerOie(); }, 17);
            UIK.Placer((RectTransform)oie.transform, 1f, 1f, -16, -310, 470, 62, 1f, 1f);
            oieTexte = UIK.Libelle(oie);
            oieTexte.color = UIK.Creme;
            oieTexte.alignment = TextAnchor.MiddleLeft;
            oieTexte.horizontalOverflow = HorizontalWrapMode.Wrap;
            oieTexte.resizeTextForBestFit = true; oieTexte.resizeTextMinSize = 12; oieTexte.resizeTextMaxSize = 17;
            UIK.Remplir(oieTexte.rectTransform, 52, 4, 10, 4);
            var icOie = UIK.IconeImage(oie.transform, "oie", 32);
            UIK.Placer(icOie.rectTransform, 0, 0.5f, 12, 0, 32, 32, 0, 0.5f);
            oie.gameObject.SetActive(false);
            // La prochaine nuit : même colonne, juste en dessous (ou à la place de l'encart du Dragonnoie s'il n'y est pas)
            nuit = UIK.Placer(UIK.Panneau(Racine, "Nuit", Style.Verre), 1f, 1f, -16, -310, 470, 40, 1f, 1f);
            nuit.GetComponent<Image>().raycastTarget = false;
            var icNuit = UIK.IconeImage(nuit, "lune", 26);
            UIK.Placer(icNuit.rectTransform, 0, 0.5f, 14, 0, 26, 26, 0, 0.5f);
            nuitTexte = UIK.Texte(nuit, "Texte", "", 17, UIK.Creme, TextAnchor.MiddleLeft, false);
            UIK.Remplir(nuitTexte.rectTransform, 52, 2, 10, 2);
            nuit.gameObject.SetActive(false);
            // Journal
            journal = UIK.Placer(UIK.Panneau(Racine, "Journal", Style.Verre), 1f, 1f, -16, -16, 470, 286, 1f, 1f);
            var titre = UIK.Texte(journal, "Titre", "Chronique", 20, UIK.Or, TextAnchor.UpperLeft, true);
            UIK.Placer(titre.rectTransform, 0, 1, 16, -8, 300, 26, 0, 1);
            for (int i = 0; i < 9; i++)
            {
                var t = UIK.Texte(journal, "Ligne" + i, "", 17, UIK.Creme, TextAnchor.UpperLeft, false);
                UIK.Placer(t.rectTransform, 0, 1, 16, -38 - i * 27, 440, 26, 0, 1);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                lignes.Add(t);
            }
            // Grande annonce
            annonce = UIK.Placer(UIK.Rect(Racine, "Annonce"), 0.5f, 0.62f, 0, 0, 1400, 120);
            annonceGroupe = annonce.gameObject.AddComponent<CanvasGroup>();
            annonceGroupe.alpha = 0; annonceGroupe.blocksRaycasts = false;
            var lueur = UIK.Image(annonce, "Lueur", UIK.Lueur, new Color(0, 0, 0, 0.55f));
            UIK.Remplir(lueur.rectTransform, -60, -40, -60, -40);
            annonceTexte = UIK.Texte(annonce, "Texte", "", 46, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Remplir(annonceTexte.rectTransform);
            var ol = annonceTexte.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.15f, 0.08f, 0.02f, 0.9f); ol.effectDistance = new Vector2(2, -2);
        }

        public void Visible(bool v) { gameObject.SetActive(v); }

        /// Le mini-jeu de l'évadé occupe l'écran : fiches, bannière, chronique et encarts s'effacent le temps de la recherche.
        public void ModeMiniJeu(bool oui)
        {
            miniJeu = oui;
            foreach (var f in fiches) f.R.gameObject.SetActive(!oui);
            banniere.gameObject.SetActive(!oui);
            journal.gameObject.SetActive(!oui);
            Rafraichir();
        }

        // ------------------------------------------------------------------ fiches des joueurs

        public void Installer(Partie p)
        {
            partie = p;
            foreach (var f in fiches) Destroy(f.R.gameObject);
            fiches.Clear();
            foreach (var e in etiquettes.Values) if (e != null) Destroy(e.gameObject);
            etiquettes.Clear();
            messages.Clear();
            for (int i = 0; i < p.Joueurs.Count; i++) fiches.Add(CreerFiche(p.Joueurs[i], i));
            foreach (var j in p.Joueurs)
            {
                var t = UIK.Texte(calqueMonde, "Nom_" + j.Nom, j.Nom, 17, Pal.Joueurs[j.Couleur], TextAnchor.MiddleCenter, true);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
                t.rectTransform.sizeDelta = new Vector2(220, 24);
                var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0, 0, 0, 0.85f); ol.effectDistance = new Vector2(1.2f, -1.2f);
                etiquettes[j.Id] = t;
            }
            Rafraichir();
        }

        Fiche CreerFiche(Joueur j, int rang)
        {
            var f = new Fiche { Joueur = j.Id };
            f.R = UIK.Placer(UIK.Panneau(Racine, "Fiche_" + j.Nom, Style.Bois), 0, 1, 16, -16 - rang * 122, 352, 114, 0, 1);
            f.Halo = UIK.Image(f.R, "Halo", UIK.Lueur, new Color(1, 0.85f, 0.4f, 0));
            UIK.Remplir(f.Halo.rectTransform, -26, -22, -26, -22);
            f.Halo.transform.SetAsFirstSibling();
            Color c = Pal.Joueurs[j.Couleur];
            f.Bande = UIK.Image(f.R, "Bande", UIK.Plein, c);
            f.Bande.type = Image.Type.Sliced;
            UIK.Placer(f.Bande.rectTransform, 0, 0.5f, 7, 0, 12, 100, 0, 0.5f);
            // niveau dans une couronne
            var cour = UIK.IconeImage(f.R, "couronne", 30);
            UIK.Placer(cour.rectTransform, 0, 1, 26, -8, 34, 34, 0, 1);
            f.Niveau = UIK.Texte(f.R, "Niveau", "1", 34, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Placer(f.Niveau.rectTransform, 0, 1, 24, -40, 40, 40, 0, 1);
            f.XP = new Image[Donnees.XPParNiveau];
            for (int k = 0; k < f.XP.Length; k++)
            {
                var pip = UIK.Image(f.R, "XP" + k, UIK.Plein, new Color(1, 1, 1, 0.2f));
                pip.type = Image.Type.Sliced;
                UIK.Placer(pip.rectTransform, 0, 1, 26 + k * 19, -86, 15, 10, 0, 1);
                f.XP[k] = pip;
            }
            f.Nom = UIK.Texte(f.R, "Nom", j.Nom, 22, UIK.Creme, TextAnchor.UpperLeft, true);
            UIK.Placer(f.Nom.rectTransform, 0, 1, 74, -8, 180, 28, 0, 1);
            f.Nom.horizontalOverflow = HorizontalWrapMode.Overflow;
            f.Classe = UIK.Texte(f.R, "Classe", j.Def.Nom + (j.Humain ? "" : " · bot"), 16, new Color(0.85f, 0.78f, 0.65f), TextAnchor.UpperRight, false);
            UIK.Placer(f.Classe.rectTransform, 1, 1, -12, -11, 120, 22, 1, 1);
            // PV
            RectTransform cadre;
            f.BarrePV = UIK.Barre(f.R, "PV", new Color(0.82f, 0.25f, 0.22f), out cadre);
            UIK.Placer(cadre, 0, 1, 98, -42, 236, 20, 0, 1);
            var coeur = UIK.IconeImage(f.R, "coeur", 22);
            UIK.Placer(coeur.rectTransform, 0, 1, 72, -41, 22, 22, 0, 1);
            f.PV = UIK.Texte(cadre, "Texte", "", 15, Color.white, TextAnchor.MiddleCenter, true);
            UIK.Remplir(f.PV.rectTransform);
            // or, attaque, défense
            float y = -72;
            var piece = UIK.IconeImage(f.R, "piece", 20); UIK.Placer(piece.rectTransform, 0, 1, 72, y, 20, 20, 0, 1);
            f.Or = UIK.Texte(f.R, "Or", "", 19, UIK.Or, TextAnchor.MiddleLeft, true); UIK.Placer(f.Or.rectTransform, 0, 1, 96, y + 1, 70, 22, 0, 1);
            var epee = UIK.IconeImage(f.R, "epee", 20); UIK.Placer(epee.rectTransform, 0, 1, 166, y, 20, 20, 0, 1);
            f.Atk = UIK.Texte(f.R, "Atk", "", 19, UIK.Creme, TextAnchor.MiddleLeft, true); UIK.Placer(f.Atk.rectTransform, 0, 1, 190, y + 1, 50, 22, 0, 1);
            var bouc = UIK.IconeImage(f.R, "bouclier", 20); UIK.Placer(bouc.rectTransform, 0, 1, 236, y, 20, 20, 0, 1);
            f.Def = UIK.Texte(f.R, "Def", "", 19, UIK.Creme, TextAnchor.MiddleLeft, true); UIK.Placer(f.Def.rectTransform, 0, 1, 260, y + 1, 40, 22, 0, 1);
            f.Etats = UIK.Texte(f.R, "États", "", 15, new Color(0.9f, 0.82f, 0.7f), TextAnchor.MiddleLeft, false);
            UIK.Placer(f.Etats.rectTransform, 0, 1, 98, -94, 246, 20, 0, 1);
            f.Etats.horizontalOverflow = HorizontalWrapMode.Overflow;
            return f;
        }

        public void Rafraichir()
        {
            if (partie == null) return;
            foreach (var f in fiches)
            {
                var j = partie.Joueurs[f.Joueur];
                bool actif = partie.JoueurCourant == j && !partie.Fini;
                f.Halo.color = new Color(1f, 0.82f, 0.4f, actif ? 0.55f : 0f);
                f.R.anchoredPosition = new Vector2(actif ? 30 : 16, f.R.anchoredPosition.y);
                f.Niveau.text = j.Niveau.ToString();
                for (int k = 0; k < f.XP.Length; k++) f.XP[k].color = k < j.XP ? new Color(0.98f, 0.82f, 0.36f) : new Color(1, 1, 1, 0.18f);
                f.BarrePV.fillAmount = Mathf.Clamp01(j.PV / (float)j.PVMax);
                f.PV.text = Mathf.Max(0, j.PV) + " / " + j.PVMax;
                f.Or.text = j.Or.ToString();
                f.Atk.text = j.Attaque.ToString();
                f.Def.text = j.Defense.ToString();
                var e = new List<string>();
                if (j.Quete != null) e.Add(UIK.Colorer("Quête " + j.Quete.Progression, new Color(1f, 0.85f, 0.45f)));
                if (j.ToursMaudit > 0) e.Add(UIK.Colorer("Maudit " + j.ToursMaudit, UIK.Violet));
                if (j.ToursImmobilise > 0) e.Add(UIK.Colorer("Immobilisé", UIK.Rouge));
                if (j.Lenteur) e.Add(UIK.Colorer("Ralenti", UIK.Bleu));
                if (j.Embuscade != null) e.Add(UIK.Colorer("En embuscade", Pal.Joueurs[j.Couleur]));
                if (j.ProtegeDuel) e.Add(UIK.Colorer("Protégé", UIK.Vert));
                if (j.Benediction > 0) e.Add(UIK.Colorer("Béni +" + j.Benediction, UIK.Or));
                f.Etats.text = string.Join("  ", e.ToArray());
            }
            var m = partie.Dragonnoie;
            oie.gameObject.SetActive(m != null && !partie.Fini && !miniJeu);
            if (m != null)
                oieTexte.text = "<b>Dragonnoie</b> · force " + m.Force + " · sur « " + partie.Plateau[partie.CaseDragonnoie].Libelle + " »\n"
                    + "Envol au tour " + partie.TourEnvol + (partie.OrDragonnoie > 0 ? " · nid : " + partie.OrDragonnoie + " or" : "");
            int pn = partie.ProchaineNuit;
            bool montrerNuit = pn >= 0 && !partie.Fini && !miniJeu;
            nuit.gameObject.SetActive(montrerNuit);
            if (montrerNuit)
            {
                nuit.anchoredPosition = new Vector2(-16, oie.gameObject.activeSelf ? -380 : -310);
                nuitTexte.text = Ambiance.NuitActuelle > 0.5f ? "<b>La nuit est tombée</b>…"
                    : pn <= partie.Tour ? "<b>La nuit tombe</b> à la fin de ce tour" : "<b>Nuit</b> à la fin du tour " + pn;
            }
        }

        // ------------------------------------------------------------------ bannière, journal, annonces

        public void Banniere(string texte, Color couleur)
        {
            banniereTexte.text = texte;
            banniereCouleur.color = couleur;
        }

        public void Journal(string s, Color c)
        {
            if (s.Length > 58) s = s.Substring(0, 56).TrimEnd() + "…";
            messages.Add(UIK.Colorer("• ", c) + s);
            while (messages.Count > 60) messages.RemoveAt(0);
            int n = lignes.Count;
            int debut = Mathf.Max(0, messages.Count - n);
            for (int i = 0; i < n; i++)
            {
                int k = debut + i;
                lignes[i].text = k < messages.Count ? messages[k] : "";
            }
        }

        public void Annonce(string texte, Color c)
        {
            annonceTexte.text = texte;
            annonceTexte.color = c;
            annonceT = 0;
        }

        // ------------------------------------------------------------------ textes flottants

        public void Flottant(Vector3 monde, string texte, Color c, int taille = 30)
        {
            var t = UIK.Texte(calqueMonde, "Flottant", texte, taille, c, TextAnchor.MiddleCenter, true);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
            t.rectTransform.sizeDelta = new Vector2(300, 50);
            var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0, 0, 0, 0.9f); ol.effectDistance = new Vector2(1.6f, -1.6f);
            // les textes d'un même endroit s'empilent
            float empile = 0;
            foreach (var f in flottants) if ((f.Monde - monde).sqrMagnitude < 0.5f && f.Vie < 0.6f) empile += 34;
            flottants.Add(new TexteFlottant { T = t, Monde = monde, Vie = 0, Duree = 1.6f, Decalage = new Vector2(0, empile) });
        }

        Vector2 VersCanvas(Vector3 ecran)
        {
            float s = Canvas.scaleFactor <= 0 ? 1 : Canvas.scaleFactor;
            return new Vector2(ecran.x / s, ecran.y / s);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // grande annonce : apparaît, reste, s'efface
            annonceT += dt;
            float a = annonceT < 0.15f ? annonceT / 0.15f : annonceT < 1.7f ? 1f : Mathf.Clamp01(1f - (annonceT - 1.7f) / 0.5f);
            annonceGroupe.alpha = a;
            annonce.localScale = Vector3.one * (annonceT < 0.15f ? Mathf.Lerp(1.25f, 1f, annonceT / 0.15f) : 1f);
            if (Camera == null) return;
            for (int i = flottants.Count - 1; i >= 0; i--)
            {
                var f = flottants[i];
                f.Vie += dt;
                if (f.Vie >= f.Duree) { Destroy(f.T.gameObject); flottants.RemoveAt(i); continue; }
                var e = Camera.Ecran(f.Monde);
                f.T.enabled = e.z > 0;
                f.T.rectTransform.anchoredPosition = VersCanvas(e) + f.Decalage + new Vector2(0, f.Vie * 46f);
                var c = f.T.color; c.a = f.Vie < f.Duree - 0.4f ? 1f : (f.Duree - f.Vie) / 0.4f; f.T.color = c;
                f.T.transform.localScale = Vector3.one * (f.Vie < 0.12f ? Mathf.Lerp(1.5f, 1f, f.Vie / 0.12f) : 1f);
            }
            if (PionDe == null) return;
            foreach (var kv in etiquettes)
            {
                var tp = PionDe(kv.Key);
                if (tp == null || kv.Value == null) continue;
                var e = Camera.Ecran(tp.position + Vector3.up * (HauteurPion != null ? HauteurPion(kv.Key) + 0.3f : 1.8f));
                kv.Value.enabled = e.z > 0 && !Camera.VueEnsemble && !miniJeu;
                kv.Value.rectTransform.anchoredPosition = VersCanvas(e);
            }
        }
    }
}

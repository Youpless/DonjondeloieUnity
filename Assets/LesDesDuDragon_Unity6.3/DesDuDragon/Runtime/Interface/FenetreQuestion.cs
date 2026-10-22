// Les Dés du Dragon — les choix proposés au joueur humain : le bandeau du bas (action, embranchement, cible, case…)
// et les grandes fenêtres (boutique, quêtes, rencontre d'un monstre).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DesDuDragon.Regles;
using DesDuDragon.Voxel;

namespace DesDuDragon.Interface
{
    public sealed partial class FenetreQuestion
    {
        readonly RectTransform racine;
        RectTransform bandeau, fenetre, reflexion;
        Text reflexionTexte;
        Action<int> repondre;
        public bool Ouverte { get; private set; }
        public SQuestion Question { get; private set; }

        public FenetreQuestion(RectTransform racine) { this.racine = racine; }

        public void Cacher()
        {
            if (bandeau != null) UnityEngine.Object.Destroy(bandeau.gameObject);
            if (fenetre != null) UnityEngine.Object.Destroy(fenetre.gameObject);
            bandeau = fenetre = null;
            Ouverte = false;
            Question = null;
            CacherReflexion();
        }

        public void Choisir(int i)
        {
            if (!Ouverte || repondre == null) return;
            var r = repondre;
            Cacher();
            r(i);
        }

        // ------------------------------------------------------------------ un bot réfléchit

        public void MontrerReflexion(string texte, Color c)
        {
            if (reflexion == null)
            {
                reflexion = UIK.Placer(UIK.Panneau(racine, "Réflexion", Style.Verre), 0.5f, 0f, 0, 24, 520, 54, 0.5f, 0f);
                reflexionTexte = UIK.Texte(reflexion, "Texte", "", 21, UIK.Creme, TextAnchor.MiddleCenter, false);
                UIK.Remplir(reflexionTexte.rectTransform, 10, 0, 10, 0);
            }
            reflexionTexte.text = UIK.Colorer("● ", c) + texte;
        }

        public void CacherReflexion()
        {
            if (reflexion != null) UnityEngine.Object.Destroy(reflexion.gameObject);
            reflexion = null;
        }

        // ------------------------------------------------------------------ aiguillage

        public void Montrer(SQuestion q, Partie p, Action<int> rep)
        {
            Cacher();
            Question = q;
            repondre = rep;
            Ouverte = true;
            var j = p.Joueurs[q.Joueur];
            switch (q.Genre)
            {
                case GenreQuestion.Boutique: Boutique(q, p, j); break;
                case GenreQuestion.Quetes: Quetes(q, p, j); break;
                case GenreQuestion.Monstre: Monstre(q, p, j); break;
                default: Bandeau(q, p, j); break;
            }
        }

        // ------------------------------------------------------------------ bandeau du bas

        void Bandeau(SQuestion q, Partie p, Joueur j)
        {
            Color cj = Pal.Joueurs[j.Couleur];
            // largeur des boutons selon leur texte, rangés en lignes
            const float largeurMax = 1240f;
            var tailles = new List<float>();
            foreach (var o in q.Options) tailles.Add(Mathf.Clamp(46 + o.Length * 11.5f, 190, 440));
            var lignes = new List<List<int>>();
            var cur = new List<int>(); float w = 0;
            for (int i = 0; i < q.Options.Count; i++)
            {
                if (cur.Count > 0 && w + tailles[i] + 12 > largeurMax - 60) { lignes.Add(cur); cur = new List<int>(); w = 0; }
                cur.Add(i); w += tailles[i] + 12;
            }
            if (cur.Count > 0) lignes.Add(cur);
            float largeur = 0;
            foreach (var l in lignes) { float s = 0; foreach (int i in l) s += tailles[i] + 12; largeur = Mathf.Max(largeur, s); }
            largeur = Mathf.Clamp(largeur + 60, 640, largeurMax);
            float hauteur = 128 + lignes.Count * 66;
            bandeau = UIK.Placer(UIK.Panneau(racine, "Bandeau", Style.Bois), 0.5f, 0f, 0, 22, largeur, hauteur, 0.5f, 0f);
            var bande = UIK.Image(bandeau, "Couleur", UIK.Plein, cj);
            bande.type = Image.Type.Sliced;
            UIK.Placer(bande.rectTransform, 0.5f, 1f, 0, -6, largeur - 40, 6, 0.5f, 1f);
            var titre = UIK.Texte(bandeau, "Titre", q.Titre, 27, UIK.Or, TextAnchor.UpperCenter, true);
            UIK.Placer(titre.rectTransform, 0.5f, 1f, 0, -16, largeur - 40, 34, 0.5f, 1f);
            var texte = UIK.Texte(bandeau, "Texte", q.Texte, 19, UIK.Creme, TextAnchor.UpperCenter, false);
            UIK.Placer(texte.rectTransform, 0.5f, 1f, 0, -52, largeur - 60, 30, 0.5f, 1f);
            var aide = UIK.Texte(bandeau, "Aide", "", 17, new Color(0.86f, 0.8f, 0.68f), TextAnchor.LowerCenter, false);
            UIK.Placer(aide.rectTransform, 0.5f, 0f, 0, 10, largeur - 60, 24, 0.5f, 0f);
            for (int li = 0; li < lignes.Count; li++)
            {
                float s = 0; foreach (int i in lignes[li]) s += tailles[i] + 12;
                float x = -s / 2f + 6;
                foreach (int i in lignes[li])
                {
                    int k = i;
                    bool principal = q.Codes[i] == "de" || q.Codes[i] == "combat" || q.Codes[i] == "defi" || q.Codes[i] == "voie" || q.Codes[i] == "case" && q.Genre == GenreQuestion.Rodeur;
                    Style st = q.Codes[i] == "de" ? Style.BoutonOr : q.Codes[i] == "annuler" || q.Codes[i] == "non" ? Style.Bouton : Style.Bouton;
                    if (q.Genre == GenreQuestion.ChoixCible && q.Cibles[i] >= 0) st = Style.Parchemin;
                    var b = UIK.Bouton(bandeau, "Option" + i, q.Options[i], st, () => Choisir(k), 21);
                    UIK.Placer((RectTransform)b.transform, 0.5f, 1f, x + tailles[i] / 2f, -96 - li * 66, tailles[i], 54, 0.5f, 1f);
                    b.interactable = q.Actives[i];
                    if (q.Genre == GenreQuestion.ChoixCible && q.Cibles[i] >= 0)
                    {
                        var c = Pal.Joueurs[p.Joueurs[q.Cibles[i]].Couleur];
                        var pastille = UIK.Image(b.transform, "Pastille", UIK.Plein, c);
                        pastille.type = Image.Type.Sliced;
                        UIK.Placer(pastille.rectTransform, 0, 0.5f, 6, 0, 8, 40, 0, 0.5f);
                    }
                    if (q.Objets[i] != null)
                    {
                        var ic = UIK.IconeImage(b.transform, UIK.IconeObjet(q.Objets[i]), 30);
                        UIK.Placer(ic.rectTransform, 0, 0.5f, 10, 0, 30, 30, 0, 0.5f);
                        UIK.Remplir(UIK.Libelle(b).rectTransform, 44, 2, 8, 2);
                    }
                    if (principal && q.Codes[i] == "de")
                    {
                        var ic = UIK.IconeImage(b.transform, "de", 30);
                        UIK.Placer(ic.rectTransform, 0, 0.5f, 12, 0, 30, 30, 0, 0.5f);
                    }
                    string a = q.Aides[i];
                    var sv = b.gameObject.AddComponent<Survol>();
                    sv.Entree = () => { aide.text = a ?? ""; if (SurvolCase != null && k < q.Cases.Count) SurvolCase(q.Cases[k]); };
                    sv.Sortie = () => { aide.text = ""; if (SurvolCase != null) SurvolCase(-1); };
                    x += tailles[i] + 12;
                }
            }
            if (q.Cases.Exists(c => c >= 0) && q.Genre != GenreQuestion.Quetes)
                aide.text = "Vous pouvez aussi cliquer sur une case lumineuse du plateau.";
        }

        /// Appelé quand le pointeur survole une option liée à une case (pour la mettre en valeur sur le plateau).
        public Action<int> SurvolCase;
    }
}

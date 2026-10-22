// Les Dés du Dragon — le salon : six places (humain, bot ou fermée), un nom, une classe et une couleur pour chacune,
// le niveau à atteindre et la vitesse des bots.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DesDuDragon.Regles;
using DesDuDragon.Voxel;

namespace DesDuDragon.Interface
{
    public enum TypePlace { Humain, Bot, Fermee }

    public sealed class Place
    {
        public TypePlace Type; public string Nom; public ClasseHeros Classe; public int Couleur;
    }

    public sealed class EcranSalon
    {
        public RectTransform R;
        public readonly List<Place> Places = new List<Place>();
        public int NiveauCible = 10;
        public bool BotsRapides;
        readonly Action<List<ReglageJoueur>> lancer;
        readonly List<Action> majLignes = new List<Action>();
        Text niveauTexte, vitesseTexte, erreur;
        Button bLancer;
        static readonly string[] NomsBots = { "Aldric", "Brunehilde", "Cassian", "Doria", "Elric", "Fendrel", "Gwenaëlle", "Hrolf", "Isolde", "Jorund", "Kaelith", "Lysandre", "Morgane", "Nyx", "Osric", "Perrine", "Quillon", "Ragna" };

        public EcranSalon(RectTransform racine, Action retour, Action<List<ReglageJoueur>> lancer)
        {
            this.lancer = lancer;
            var r = new System.Random(Environment.TickCount);
            var noms = new List<string>(NomsBots);
            var classes = new List<int> { 0, 1, 2, 3, 4, 5 };
            for (int i = classes.Count - 1; i > 0; i--) { int k = r.Next(i + 1); int tmp = classes[i]; classes[i] = classes[k]; classes[k] = tmp; }
            for (int i = 0; i < 6; i++)
            {
                int k = r.Next(noms.Count);
                Places.Add(new Place
                {
                    Type = i == 0 ? TypePlace.Humain : i < 4 ? TypePlace.Bot : TypePlace.Fermee,
                    Nom = i == 0 ? "Joueur 1" : noms[k],
                    Classe = (ClasseHeros)classes[i],
                    Couleur = i
                });
                noms.RemoveAt(k);
            }
            R = UIK.Remplir(UIK.Rect(racine, "Salon"));
            var voile = UIK.Image(R, "Voile", UIK.Blanc, new Color(0, 0, 0, 0.35f));
            UIK.Remplir(voile.rectTransform);
            var p = UIK.Placer(UIK.Panneau(R, "Panneau", Style.Parchemin), 0.5f, 0.5f, 0, 0, 1320, 860);
            var bandeau = UIK.Placer(UIK.Panneau(p, "Titre", Style.Bois), 0.5f, 1f, 0, 28, 640, 66, 0.5f, 1f);
            var t = UIK.Texte(bandeau, "Texte", "Préparer l'aventure", 34, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Remplir(t.rectTransform);
            var aide = UIK.Texte(p, "Aide", "Chaque place peut être tenue par un humain (on se passe la souris à son tour) ou par un bot. Deux aventuriers au moins.", 18, new Color(0.35f, 0.25f, 0.15f), TextAnchor.MiddleCenter, false, false);
            UIK.Placer(aide.rectTransform, 0.5f, 1f, 0, -56, 1200, 28, 0.5f, 1f);
            for (int i = 0; i < 6; i++) Ligne(p, i);

            // Options
            float y = -660;
            niveauTexte = UIK.Texte(p, "Niveau", "", 22, UIK.Encre, TextAnchor.MiddleCenter, true, false);
            UIK.Placer(niveauTexte.rectTransform, 0, 1, 260, y, 320, 44, 0.5f, 1f);
            var nm = UIK.Bouton(p, "Niveau moins", "◄", Style.Parchemin, () => { NiveauCible = Mathf.Max(3, NiveauCible - 1); Maj(); }, 22);
            UIK.Placer((RectTransform)nm.transform, 0, 1, 80, y, 50, 44, 0.5f, 1f);
            var np = UIK.Bouton(p, "Niveau plus", "►", Style.Parchemin, () => { NiveauCible = Mathf.Min(10, NiveauCible + 1); Maj(); }, 22);
            UIK.Placer((RectTransform)np.transform, 0, 1, 440, y, 50, 44, 0.5f, 1f);
            var bv = UIK.Bouton(p, "Vitesse", "", Style.Parchemin, () => { BotsRapides = !BotsRapides; Maj(); }, 21);
            UIK.Placer((RectTransform)bv.transform, 0, 1, 700, y, 380, 44, 0.5f, 1f);
            vitesseTexte = UIK.Libelle(bv);
            erreur = UIK.Texte(p, "Erreur", "", 19, new Color(0.65f, 0.12f, 0.08f), TextAnchor.MiddleCenter, false, false);
            UIK.Placer(erreur.rectTransform, 0.5f, 0, 0, 104, 900, 28, 0.5f, 0);
            var br = UIK.Bouton(p, "Retour", "Retour", Style.Bouton, () => retour(), 24);
            UIK.Placer((RectTransform)br.transform, 0.5f, 0, -230, 30, 280, 62, 0.5f, 0);
            bLancer = UIK.Bouton(p, "Lancer", "Commencer la partie", Style.BoutonOr, Lancer, 26);
            UIK.Placer((RectTransform)bLancer.transform, 0.5f, 0, 170, 30, 420, 66, 0.5f, 0);
            Maj();
        }

        void Ligne(RectTransform p, int i)
        {
            var pl = Places[i];
            float y = -96 - i * 90;
            var ligne = UIK.Placer(UIK.Panneau(p, "Place" + i, Style.Carte), 0.5f, 1f, 0, y, 1240, 80, 0.5f, 1f);
            var pastille = UIK.Bouton(ligne, "Couleur", "", Style.Parchemin, null, 18);
            UIK.Placer((RectTransform)pastille.transform, 0, 0.5f, 14, 0, 52, 52, 0, 0.5f);
            var teinte = pastille.GetComponent<Image>();
            pastille.onClick.AddListener(() => { pl.Couleur = CouleurLibre(pl.Couleur, i); Maj(); });
            var type = UIK.Bouton(ligne, "Type", "", Style.Bouton, () => { pl.Type = (TypePlace)(((int)pl.Type + 1) % 3); Maj(); }, 21);
            UIK.Placer((RectTransform)type.transform, 0, 0.5f, 78, 0, 150, 52, 0, 0.5f);
            // champ du nom
            var champFond = UIK.Image(ligne, "Nom", UIK.Plein, new Color(1, 1, 1, 0.85f));
            champFond.type = Image.Type.Sliced;
            champFond.raycastTarget = true;
            UIK.Placer(champFond.rectTransform, 0, 0.5f, 244, 0, 250, 50, 0, 0.5f);
            var champTexte = UIK.Texte(champFond.transform, "Texte", "", 22, UIK.Encre, TextAnchor.MiddleLeft, true, false);
            UIK.Remplir(champTexte.rectTransform, 12, 2, 10, 2);
            champTexte.supportRichText = false;
            var champ = champFond.gameObject.AddComponent<InputField>();
            champ.textComponent = champTexte;
            champ.characterLimit = 14;
            champ.text = pl.Nom;
            champ.onValueChanged.AddListener(s => pl.Nom = s);
            // classe
            var moins = UIK.Bouton(ligne, "Classe moins", "◄", Style.Parchemin, () => { pl.Classe = (ClasseHeros)(((int)pl.Classe + 5) % 6); Maj(); }, 20);
            UIK.Placer((RectTransform)moins.transform, 0, 0.5f, 512, 0, 44, 50, 0, 0.5f);
            var classe = UIK.Texte(ligne, "Classe", "", 24, UIK.Encre, TextAnchor.MiddleCenter, true, false);
            UIK.Placer(classe.rectTransform, 0, 0.5f, 560, 0, 150, 50, 0, 0.5f);
            var plus = UIK.Bouton(ligne, "Classe plus", "►", Style.Parchemin, () => { pl.Classe = (ClasseHeros)(((int)pl.Classe + 1) % 6); Maj(); }, 20);
            UIK.Placer((RectTransform)plus.transform, 0, 0.5f, 714, 0, 44, 50, 0, 0.5f);
            var desc = UIK.Texte(ligne, "Description", "", 16, new Color(0.3f, 0.22f, 0.15f), TextAnchor.MiddleLeft, false, false);
            UIK.Placer(desc.rectTransform, 0, 0.5f, 774, 0, 450, 70, 0, 0.5f);
            var cg = ligne.gameObject.AddComponent<CanvasGroup>();
            majLignes.Add(() =>
            {
                teinte.color = Pal.Joueurs[pl.Couleur];
                UIK.Libelle(type).text = pl.Type == TypePlace.Humain ? "Humain" : pl.Type == TypePlace.Bot ? "Bot" : "Fermée";
                bool ouvert = pl.Type != TypePlace.Fermee;
                cg.alpha = ouvert ? 1f : 0.45f;
                champ.interactable = ouvert; moins.interactable = ouvert; plus.interactable = ouvert; pastille.interactable = ouvert;
                var d = Donnees.Classe(pl.Classe);
                classe.text = d.Nom;
                desc.text = "<b>Att " + d.Attaque + " · Déf " + d.Defense + " · PV " + d.Vie + "</b>  " + d.Passif;
            });
        }

        int CouleurLibre(int actuelle, int place)
        {
            for (int k = 1; k <= 6; k++)
            {
                int c = (actuelle + k) % 6;
                bool prise = false;
                for (int i = 0; i < Places.Count; i++) if (i != place && Places[i].Type != TypePlace.Fermee && Places[i].Couleur == c) prise = true;
                if (!prise) return c;
            }
            return actuelle;
        }

        void Maj()
        {
            foreach (var m in majLignes) m();
            niveauTexte.text = "Niveau à atteindre : " + NiveauCible;
            vitesseTexte.text = "Vitesse des bots : " + (BotsRapides ? "rapide" : "normale");
            int n = 0; var couleurs = new HashSet<int>(); bool doublon = false;
            foreach (var pl in Places) if (pl.Type != TypePlace.Fermee) { n++; if (!couleurs.Add(pl.Couleur)) doublon = true; }
            erreur.text = n < 2 ? "Ouvrez au moins deux places." : doublon ? "Deux aventuriers ont la même couleur : cliquez sur une pastille pour en changer." : "";
            bLancer.interactable = n >= 2 && !doublon;
        }

        void Lancer()
        {
            var l = new List<ReglageJoueur>();
            int k = 1;
            foreach (var pl in Places)
            {
                if (pl.Type == TypePlace.Fermee) continue;
                string nom = string.IsNullOrEmpty(pl.Nom) || pl.Nom.Trim().Length == 0 ? "Aventurier " + k : pl.Nom.Trim();
                l.Add(new ReglageJoueur(nom, pl.Classe, pl.Type == TypePlace.Humain, pl.Couleur));
                k++;
            }
            if (l.Count >= 2) lancer(l);
        }

        public void Visible(bool v) { R.gameObject.SetActive(v); }
    }
}

// Donjon de l'Oie — les cartes jouables : celle de l'éditeur de cartes de la scène, celle choisie sur DDJeu, la carte
// d'origine, et toutes celles du dossier Resources/Cartes. Le salon propose de choisir ; l'île est reconstruite au besoin.
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Cartes;

namespace DonjonDeLOie.Pilote
{
    public sealed partial class DDJeu
    {
        sealed class CarteJouable { public string Nom; public CarteDef Def; public int Niveau = 10; }   // Def null : la carte d'origine

        readonly List<CarteJouable> cartes = new List<CarteJouable>();
        int carteParDefaut, carteConstruite = -1;

        void PreparerCartes()
        {
            cartes.Clear();
            // 1. la carte en cours d'édition dans cette scène
            var ed = FindAnyObjectByType<DDEditeurCarte>();
            if (ed != null)
            {
                Ajouter(ed.NomCarte + " (éditeur)", ed.Exporter(), ed.NiveauConseille);
                ed.gameObject.SetActive(false);         // les marqueurs ne s'affichent pas en jeu
            }
            // 2. la carte choisie sur ce script
            if (Carte != null) Ajouter(string.IsNullOrEmpty(Carte.Carte.Nom) ? Carte.name : Carte.Carte.Nom, Carte.Carte, Carte.NiveauConseille);
            // 3. la carte d'origine
            cartes.Add(new CarteJouable { Nom = "Valcreux (carte d'origine)", Def = null });
            // 4. les cartes ajoutées au jeu
            foreach (var c in DDCarte.Toutes())
            {
                if (c == Carte) continue;
                Ajouter(string.IsNullOrEmpty(c.Carte.Nom) ? c.name : c.Carte.Nom, c.Carte, c.NiveauConseille);
            }
            carteParDefaut = 0;
        }

        void Ajouter(string nom, CarteDef def, int niveau)
        {
            var erreurs = new List<string>(); var conseils = new List<string>();
            Plateau.Verifier(def, erreurs, conseils);
            if (erreurs.Count > 0)
            {
                Debug.LogError("Donjon de l'Oie : la carte « " + nom + " » n'est pas jouable :\n• " + string.Join("\n• ", erreurs.ToArray()));
                return;
            }
            foreach (var c in conseils) Debug.LogWarning("Carte « " + nom + " » : " + c);
            cartes.Add(new CarteJouable { Nom = nom, Def = def, Niveau = Mathf.Clamp(niveau, 3, 10) });
        }

        List<string> NomsCartes()
        {
            var l = new List<string>();
            foreach (var c in cartes) l.Add(c.Nom);
            return l;
        }

        List<int> NiveauxConseilles()
        {
            var l = new List<int>();
            foreach (var c in cartes) l.Add(c.Niveau);
            return l;
        }

        /// (Re)construit l'île d'une carte de la liste.
        void ConstruireIle(int index)
        {
            index = Mathf.Clamp(index, 0, cartes.Count - 1);
            var c = cartes[index];
            if (ile != null) Destroy(ile.gameObject);
            var plateau = c.Def == null ? Plateau.Standard() : Plateau.Depuis(c.Def);
            modele = new IleModele(plateau, c.Def);
            ile = IleVue.Construire(modele, monde);
            carteConstruite = index;
            cam.CentreCarte = new Vector3(modele.X0 + modele.LargeurM / 2f, 0, modele.Z0 + modele.ProfondeurM / 2f);
            cam.TailleCarte = Mathf.Max(modele.LargeurM, modele.ProfondeurM) - 24f;   // la terre, sans la bande de mer de la grille
        }
    }
}

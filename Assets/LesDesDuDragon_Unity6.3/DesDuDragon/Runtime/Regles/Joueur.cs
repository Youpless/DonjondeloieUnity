// Les Dés du Dragon — un joueur (humain ou bot) et son héros.
using System;
using System.Collections.Generic;

namespace DesDuDragon.Regles
{
    public sealed class Quete
    {
        public enum Genre { Atteindre, Chasser }
        public Genre Type;
        public int CaseCible = -1;          // Atteindre
        public string MonstreCible;         // Chasser (id)
        public int Nombre = 1, Fait;
        public int RecompenseOr, RecompenseXP;
        public string Titre, Texte;

        public string Progression
        {
            get { return Type == Genre.Chasser ? (Fait + " / " + Nombre) : (Fait > 0 ? "atteint" : "en route"); }
        }
    }

    public sealed class Piege
    {
        public int Proprietaire;
        public DefObjet Objet;
        public int Case;
    }

    public sealed class Embuscade
    {
        public int Case, CaseVoisine;
        public bool Couvre(int c) { return c == Case || c == CaseVoisine; }
    }

    public sealed class Joueur
    {
        public int Id;
        public string Nom;
        public ClasseHeros Classe;
        public bool Humain;
        public int Couleur;              // index de couleur (0..5)

        public int Niveau = 1, XP, PV, Or;
        public int Case;
        public DefObjet Arme, Armure;
        public readonly List<DefObjet> Accessoires = new List<DefObjet>();
        public readonly List<DefObjet> Sac = new List<DefObjet>();
        public Quete Quete;

        // états
        public int ToursImmobilise;      // passe ses prochains tours
        public int ToursMaudit;          // −2 en attaque
        public bool Lenteur;             // prochain jet divisé par deux
        public bool ProtegeDuel;         // vient de perdre un duel : on ne peut plus le défier jusqu'à la fin de son tour
        public Embuscade Embuscade;
        public int Benediction;          // bonus au prochain combat

        // statistiques de fin de partie
        public int MonstresTues, DuelsGagnes, DuelsPerdus, QuetesFinies, OrGagne, PiegesDeclenches, KO, NiveauMax = 1;

        public DefClasse Def { get { return Donnees.Classe(Classe); } }

        public int PVMax
        {
            get
            {
                int v = Def.Vie + 2 * (Niveau - 1);
                for (int i = 0; i < Accessoires.Count; i++) if (Accessoires[i].Effet == EffetObjet.Anneau) v += Accessoires[i].Bonus;
                return v;
            }
        }

        public int Defense { get { return Def.Defense + (Armure != null ? Armure.Bonus : 0); } }

        /// Attaque affichée et utilisée en combat (sans le dé).
        public int Attaque
        {
            get
            {
                int a = Def.Attaque + (Arme != null ? Arme.Bonus : 0);
                if (ToursMaudit > 0) a -= 2;
                if (Classe == ClasseHeros.Barbare && PV * 2 <= PVMax) a += 2;
                return Math.Max(0, a);
            }
        }

        public bool A(EffetObjet e)
        {
            for (int i = 0; i < Accessoires.Count; i++) if (Accessoires[i].Effet == e) return true;
            return false;
        }

        public bool RetirerAccessoire(EffetObjet e)
        {
            for (int i = 0; i < Accessoires.Count; i++) if (Accessoires[i].Effet == e) { Accessoires.RemoveAt(i); return true; }
            return false;
        }

        public int CompterSac(TypeObjet t)
        {
            int n = 0;
            for (int i = 0; i < Sac.Count; i++) if (Sac[i].Type == t) n++;
            return n;
        }

        public int PrixPour(DefObjet o)
        {
            int p = o.Prix;
            if (Classe == ClasseHeros.Mage && o.Type == TypeObjet.Parchemin) p = (p + 1) / 2;
            if (Classe == ClasseHeros.Voleur && o.Type == TypeObjet.Piege) p = (p + 1) / 2;
            return p;
        }

        public string Titre { get { return Nom + " (" + Def.Nom + ")"; } }
    }
}

// Donjon de l'Oie — ce que le moteur de règles raconte à la présentation (ou à une simulation).
// Le moteur avance tour par tour en produisant des signaux ; une Question attend une réponse
// (d'un humain par l'interface, ou d'un bot) avant que le moteur reprenne.
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public enum Ton { Neutre, Bon, Mauvais, Combat, Quete, Systeme }

    public abstract class Signal { }

    public sealed class SAnnonce : Signal
    {
        public string Texte; public int Joueur = -1; public Ton Ton; public bool Grande;
        public SAnnonce(string texte, int joueur = -1, Ton ton = Ton.Neutre, bool grande = false)
        { Texte = texte; Joueur = joueur; Ton = ton; Grande = grande; }
    }

    public sealed class STourDebut : Signal { public int Joueur, Tour; }

    public sealed class SDe : Signal
    {
        public int Joueur; public int Faces = 6; public int Valeur; public int Bonus; public string Raison;
        public int Total { get { return Valeur + Bonus; } }
    }

    public sealed class SPas : Signal { public int Joueur, De, Vers; }

    public sealed class STeleport : Signal { public int Joueur, Vers; public string Raison; }

    /// Le Dragonnoie s'envole de « De » (-1 : il arrive sur l'île) et se pose sur « Vers ».
    public sealed class SEnvol : Signal { public int De = -1, Vers; }

    /// La nuit tombe (Tombe = vrai) après le tour de table « Tour », ou le jour se lève (Tombe = faux).
    public sealed class SNuit : Signal { public bool Tombe; public int Tour, Numero; }

    public enum Stat { PV, Or, XP, Niveau, Attaque }

    public sealed class SStat : Signal
    {
        public int Joueur; public Stat Stat; public int Delta;
        public SStat(int j, Stat s, int d) { Joueur = j; Stat = s; Delta = d; }
    }

    public sealed class SCombat : Signal
    {
        public bool Duel;                 // sinon contre un monstre
        public int Attaquant, Defenseur = -1;
        public DefMonstre Monstre;
        public int FacesDe;               // 3 en duel, 6 contre un monstre
        public int AtkA, BonusA, DeA, AtkB, BonusB, DeB;
        public string RaisonBonusA, RaisonBonusB;
        public bool Egalite, AttaquantGagne;
        public bool Embuscade;
        public bool Arene;                // duel du tournoi de l'arène du diable (la nuit)
        public int TotalA { get { return AtkA + BonusA + DeA; } }
        public int TotalB { get { return AtkB + BonusB + DeB; } }
    }

    public enum Effet
    {
        PiegePose, PiegeDeclenche, EmbuscadeTendue, EmbuscadeDeclenchee, SortLance, SortBloque,
        Malediction, NiveauGagne, NiveauPerdu, KO, Soin, Tresor, Achat, QueteAcceptee, QueteFinie, Evenement, Victoire,
        OrDragonnoie,       // le Dragonnoie arrache la bourse d'un vaincu (Texte : la somme)
        VolNocturne,        // la nuit, un voleur prend la moitié de l'or d'un joueur (Texte : la somme)
        TrouDeVer,          // la nuit, Joueur et Cible échangent leur place
        BonneNuit,          // la nuit, un joueur dort bien et gagne un niveau
        AreneDebut,         // l'arène du diable surgit (Texte : l'ordre d'entrée, identifiants séparés par des virgules)
        AreneFin,           // Joueur remporte l'arène du diable
        QueteRatee          // une quête échoue (l'évadé s'échappe) ; Texte : son titre
    }

    public sealed class SEffet : Signal
    {
        public Effet Effet; public int Joueur = -1, Cible = -1, Case = -1; public string Texte; public string Objet;
    }

    public enum GenreQuestion { Action, Embranchement, Rodeur, Defi, Monstre, Boutique, Quetes, ChoixCase, ChoixCible, Confirmer, ChoixEvenement, Dragonnoie, Evade }

    public sealed class SQuestion : Signal
    {
        public int Joueur;
        public GenreQuestion Genre;
        public string Contexte;          // précise la question : « embuscade », « piege », « rappel », « sort:<id> »
        public string Titre, Texte;
        public readonly List<string> Options = new List<string>();
        public readonly List<string> Aides = new List<string>();
        public readonly List<string> Codes = new List<string>();
        public readonly List<bool> Actives = new List<bool>();
        public readonly List<int> Cases = new List<int>();     // case associée à chaque option (-1 sinon)
        public readonly List<int> Cibles = new List<int>();    // joueur associé à chaque option (-1 sinon)
        public readonly List<DefObjet> Objets = new List<DefObjet>();
        public readonly List<int> Prix = new List<int>();
        public readonly List<Quete> Quetes = new List<Quete>();
        public readonly List<List<EffetEvenement>> EffetsChoix = new List<List<EffetEvenement>>();   // ChoixEvenement : les effets de chaque option
        public DefMonstre Monstre;
        public Quete Quete;              // Evade : la quête en jeu (nom et allure de l'évadé)
        public int ParDefaut;
        public int Reponse = -1;

        public SQuestion Ajouter(string option, string code, string aide = null, bool active = true,
                                 int caseId = -1, int cible = -1, DefObjet objet = null, int prix = 0, Quete quete = null)
        {
            Options.Add(option); Codes.Add(code); Aides.Add(aide); Actives.Add(active);
            Cases.Add(caseId); Cibles.Add(cible); Objets.Add(objet); Prix.Add(prix); Quetes.Add(quete);
            return this;
        }

        public string CodeChoisi { get { return Reponse >= 0 && Reponse < Codes.Count ? Codes[Reponse] : null; } }
    }

    public sealed class SFinPartie : Signal { public int Gagnant; }
}

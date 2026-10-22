// Donjon de l'Oie — données du jeu (classes, monstres, objets, cases, zones).
// Ce fichier ne dépend pas d'Unity : les règles tournent aussi hors du moteur (essais, simulations).
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public enum ClasseHeros { Guerrier, Mage, Voleur, Clerc, Rodeur, Barbare }

    public enum Zone { Village, Bois, Marais, Cryptes, Pics, Antre }

    public enum TypeCase { Ville, Chemin, Monstre, Tresor, Evenement, Malediction, Sanctuaire, Marchand, Taverne, Antre }

    public enum TypeObjet { Arme, Armure, Accessoire, Potion, Piege, Parchemin }

    public enum EffetObjet
    {
        Aucun,
        // accessoires
        Amulette, Bottes, Anneau,
        // potions
        Soin, SoinTotal,
        // pièges
        PiegeLoups, PiegeFosse, PiegeFilet, PiegeRune,
        // parchemins
        SortMalediction, SortFoudre, SortLenteur, SortRappel
    }

    [Serializable]
    public sealed class DefClasse
    {
        public ClasseHeros Classe;
        public string Nom, Passif, Description;
        public int Attaque, Defense, Vie;
        public DefClasse() { }
        public DefClasse Copie() { return (DefClasse)MemberwiseClone(); }
        public DefClasse(ClasseHeros c, string nom, int atk, int def, int vie, string passif, string desc)
        { Classe = c; Nom = nom; Attaque = atk; Defense = def; Vie = vie; Passif = passif; Description = desc; }
    }

    [Serializable]
    public sealed class DefMonstre
    {
        public string Id, Nom, Description;
        public int Force, Degats, Or, XP;
        public Zone[] Zones;
        public bool Boss;
        public bool Errant;                  // le Dragonnoie : il erre sur la carte au lieu de surgir des cases (Partie.Dragonnoie.cs)
        public string Apparence = "";        // modèle 3D d'un autre monstre (vide : celui de son identifiant)
        public string Modele { get { return string.IsNullOrEmpty(Apparence) ? Id : Apparence; } }
        public DefMonstre() { Zones = new Zone[0]; }
        public DefMonstre Copie() { var m = (DefMonstre)MemberwiseClone(); m.Zones = (Zone[])(Zones ?? new Zone[0]).Clone(); return m; }
        public DefMonstre(string id, string nom, int force, int degats, int or, int xp, Zone[] zones, string desc, bool boss = false)
        { Id = id; Nom = nom; Force = force; Degats = degats; Or = or; XP = xp; Zones = zones; Description = desc; Boss = boss; }
    }

    [Serializable]
    public sealed class DefObjet
    {
        public string Id, Nom, Description;
        public TypeObjet Type;
        public EffetObjet Effet;
        public int Prix, Bonus;
        public bool SeulementEnVille;
        public DefObjet() { }
        public DefObjet Copie() { return (DefObjet)MemberwiseClone(); }
        public DefObjet(string id, string nom, TypeObjet type, int prix, int bonus, EffetObjet effet, string desc, bool ville = false)
        { Id = id; Nom = nom; Type = type; Prix = prix; Bonus = bonus; Effet = effet; Description = desc; SeulementEnVille = ville; }
        public bool Consommable { get { return Type == TypeObjet.Potion || Type == TypeObjet.Piege || Type == TypeObjet.Parchemin; } }
    }

    [Serializable]
    public sealed class Reglages
    {
        public int XPParNiveau = 2;          // XP pour un niveau
        public int OrDepart = 20;            // or de départ
        public int SoldeVille = 10;          // or à chaque passage par la case de départ
        public int XPVille = 1;              // XP à chaque passage par la case de départ
        public int TailleSac = 5;            // objets dans le sac
        public int AccessoiresMax = 2;
        public int PiegesMaxParJoueur = 3;
        public bool DragonnoieActif = true;  // le Dragonnoie erre sur la carte
        public int ToursEntreEnvols = 3;     // il s'envole au début d'un tour de table sur N
        public bool NuitsActives = true;     // la nuit tombe tous les N tours de table, avec un événement
        public int ToursEntreNuits = 5;      // N
        public bool NuitVoleur = true, NuitTrouDeVer = true, NuitBonneNuit = true, NuitArene = true;   // les événements possibles
        public int OrArene = 25;             // l'or du vainqueur de l'arène du diable
        public int DureeEvade = 30;          // secondes pour retrouver l'évadé (mini-jeu)
        public Reglages Copie() { return (Reglages)MemberwiseClone(); }
    }

    public static class Donnees
    {
        static DefClasse[] ClassesParDefaut() { return new[]
        {
            new DefClasse(ClasseHeros.Guerrier, "Guerrier", 3, 1, 11, "Vétéran : +1 en combat contre les monstres.",
                "Solide et bien protégé, il nettoie les routes des bêtes qui les infestent."),
            new DefClasse(ClasseHeros.Mage, "Mage", 3, 0, 9, "Arcaniste : parchemins à moitié prix, sorts plus durs à esquiver, +1 contre les monstres (trait de feu).",
                "Fragile, mais ses parchemins font trembler les plus hauts niveaux."),
            new DefClasse(ClasseHeros.Voleur, "Voleur", 3, 0, 10, "Ombre : pièges à moitié prix, ne déclenche jamais un piège, +5 or sur les trésors.",
                "Il sème le plateau de pièges et se faufile entre ceux des autres."),
            new DefClasse(ClasseHeros.Clerc, "Clerc", 3, 1, 11, "Foi : +2 PV au début de chaque tour, résiste aux malédictions.",
                "Béni des dieux, il se relève de tout et brave les cases maudites."),
            new DefClasse(ClasseHeros.Rodeur, "Rôdeur", 2, 0, 10, "Pisteur : après son jet, avance d'une case de plus ou de moins s'il le veut.",
                "Il choisit où il pose le pied : trésors, marchands, proies."),
            new DefClasse(ClasseHeros.Barbare, "Barbare", 3, 0, 13, "Rage : +2 en attaque quand il lui reste la moitié de ses PV ou moins.",
                "Une force brute ; blessé, il devient terrifiant."),
        }; }

        /// Les classes en cours (celles du catalogue de l'éditeur si on l'a chargé). Rangées dans l'ordre de ClasseHeros.
        public static DefClasse[] Classes = ClassesParDefaut();

        public static DefClasse Classe(ClasseHeros c) { return Classes[(int)c]; }

        public static readonly string[] NomsZones = { "Valcreux", "Bois des Murmures", "Marais Putride", "Cryptes d'Ombrelune", "Pics de Braise", "Antre du Dragon" };

        static DefMonstre[] MonstresParDefaut() { return new[]
        {
            new DefMonstre("rat",       "Rat géant",        1, 2,  5, 1, new[]{ Zone.Village, Zone.Bois },          "Gros comme un chien, plus vorace encore."),
            new DefMonstre("gobelin",   "Gobelin",          2, 2,  7, 1, new[]{ Zone.Bois },                         "Petit, vicieux, toujours une lame rouillée à la main."),
            new DefMonstre("loup",      "Loup gris",        3, 3,  6, 2, new[]{ Zone.Bois, Zone.Village },          "Il chasse en silence entre les troncs."),
            new DefMonstre("araignee",  "Araignée géante",  4, 3, 10, 2, new[]{ Zone.Bois, Zone.Marais },           "Huit yeux, huit pattes, aucun scrupule."),
            new DefMonstre("gelee",     "Gelée verte",      5, 4,  9, 3, new[]{ Zone.Marais },                       "Elle dissout tout ce qu'elle avale, armure comprise."),
            new DefMonstre("squelette", "Squelette",        5, 3, 11, 3, new[]{ Zone.Marais, Zone.Cryptes },        "Un ancien soldat qui ne sait pas qu'il est mort."),
            new DefMonstre("orc",       "Orc pillard",      6, 4, 14, 3, new[]{ Zone.Pics, Zone.Bois },             "Brute couverte de cicatrices, hache au poing."),
            new DefMonstre("spectre",   "Spectre",          7, 4, 16, 4, new[]{ Zone.Cryptes },                      "Une plainte glacée qui traverse les murs."),
            new DefMonstre("troll",     "Troll des pics",   8, 5, 20, 5, new[]{ Zone.Pics },                         "Il se régénère plus vite qu'on ne le frappe."),
            new DefMonstre("wyverne",   "Wyverne",         10, 6, 28, 6, new[]{ Zone.Pics, Zone.Antre },            "Cousine du dragon, le venin en plus."),
            new DefMonstre("dragon",    "Ignathor, le Dragon rouge", 12, 7, 80, 8, new[]{ Zone.Antre },              "Le seigneur de l'antre. Son trésor est légendaire.", true),
            new DefMonstre("dragonnoie", "Le Dragonnoie",    9, 0,  0, 0, new Zone[0],
                "Mi-oie, mi-dragon, entièrement mauvais. Il fond sur les aventuriers et leur arrache leur bourse. Qui le terrasse gagne la partie.")
                { Errant = true },
        }; }

        public static DefMonstre[] Monstres = MonstresParDefaut();

        /// Le boss (le monstre de la case Antre) : le premier monstre marqué « Boss ».
        public static DefMonstre Boss
        {
            get
            {
                foreach (var m in Monstres) if (m.Boss && !m.Errant) return m;
                for (int i = Monstres.Length - 1; i >= 0; i--) if (!Monstres[i].Errant) return Monstres[i];
                return null;
            }
        }

        /// Le monstre errant (le premier marqué « Errant »), ou null s'il n'y en a pas ou si les réglages le désactivent.
        public static DefMonstre Dragonnoie
        {
            get
            {
                if (!Reglages.DragonnoieActif) return null;
                foreach (var m in Monstres) if (m.Errant) return m;
                return null;
            }
        }

        public static DefMonstre Monstre(string id)
        {
            for (int i = 0; i < Monstres.Length; i++) if (Monstres[i].Id == id) return Monstres[i];
            return null;
        }

        static DefObjet[] ObjetsParDefaut() { return new[]
        {
            // Armes : bonus d'attaque
            new DefObjet("dague",     "Dague",                 TypeObjet.Arme, 10, 1, EffetObjet.Aucun, "+1 en attaque."),
            new DefObjet("epee",      "Épée courte",           TypeObjet.Arme, 25, 2, EffetObjet.Aucun, "+2 en attaque."),
            new DefObjet("hache",     "Hache de guerre",       TypeObjet.Arme, 45, 3, EffetObjet.Aucun, "+3 en attaque."),
            new DefObjet("runique",   "Épée runique",          TypeObjet.Arme, 70, 4, EffetObjet.Aucun, "+4 en attaque."),
            new DefObjet("marteau",   "Marteau-tonnerre",      TypeObjet.Arme,100, 5, EffetObjet.Aucun, "+5 en attaque."),
            new DefObjet("lamedragon","Lame du Dragon",        TypeObjet.Arme,150, 7, EffetObjet.Aucun, "+7 en attaque. Forgée à Valcreux seulement.", true),
            // Armures : réduisent les dégâts des monstres et des sorts de foudre
            new DefObjet("cuir",      "Armure de cuir",        TypeObjet.Armure, 15, 1, EffetObjet.Aucun, "−1 aux dégâts subis."),
            new DefObjet("mailles",   "Cotte de mailles",      TypeObjet.Armure, 35, 2, EffetObjet.Aucun, "−2 aux dégâts subis."),
            new DefObjet("plates",    "Armure de plates",      TypeObjet.Armure, 65, 3, EffetObjet.Aucun, "−3 aux dégâts subis."),
            new DefObjet("ecailles",  "Écailles de dragon",    TypeObjet.Armure,110, 4, EffetObjet.Aucun, "−4 aux dégâts subis. Valcreux seulement.", true),
            // Accessoires (2 au plus)
            new DefObjet("amulette",  "Amulette de garde",     TypeObjet.Accessoire, 30, 0, EffetObjet.Amulette, "Bloque la prochaine malédiction, rune ou sort reçu, puis se brise."),
            new DefObjet("bottes",    "Bottes de célérité",    TypeObjet.Accessoire, 45, 1, EffetObjet.Bottes,   "+1 à chaque jet de déplacement."),
            new DefObjet("anneau",    "Anneau de vigueur",     TypeObjet.Accessoire, 35, 4, EffetObjet.Anneau,   "+4 PV maximum."),
            // Potions
            new DefObjet("potion",    "Potion de soin",        TypeObjet.Potion,  8, 5, EffetObjet.Soin,      "Rend 5 PV."),
            new DefObjet("elixir",    "Élixir de vie",         TypeObjet.Potion, 20, 0, EffetObjet.SoinTotal, "Rend tous les PV."),
            // Pièges (posés sur sa case ou une case voisine)
            new DefObjet("loups",     "Piège à loups",         TypeObjet.Piege, 12, 2, EffetObjet.PiegeLoups, "La victime perd 2 PV et passe son prochain tour."),
            new DefObjet("fosse",     "Fosse à pieux",         TypeObjet.Piege, 10, 4, EffetObjet.PiegeFosse, "La victime perd 4 PV."),
            new DefObjet("filet",     "Filet du voleur",       TypeObjet.Piege, 15,15, EffetObjet.PiegeFilet, "Vole 15 or à la victime, pour vous."),
            new DefObjet("rune",      "Rune d'oubli",          TypeObjet.Piege, 40, 1, EffetObjet.PiegeRune,  "La victime perd 1 niveau."),
            // Parchemins (sorts lancés sur n'importe quel joueur)
            new DefObjet("malediction","Parchemin de malédiction", TypeObjet.Parchemin, 40, 1, EffetObjet.SortMalediction, "La cible perd 1 niveau, sauf si elle résiste (5+ au dé)."),
            new DefObjet("foudre",    "Parchemin de foudre",   TypeObjet.Parchemin, 20, 5, EffetObjet.SortFoudre,  "La cible perd 5 PV (moins son armure)."),
            new DefObjet("lenteur",   "Parchemin de lenteur",  TypeObjet.Parchemin, 12, 0, EffetObjet.SortLenteur, "Le prochain jet de déplacement de la cible est divisé par deux."),
            new DefObjet("rappel",    "Parchemin de rappel",   TypeObjet.Parchemin, 15, 0, EffetObjet.SortRappel,  "Vous téléporte sur Valcreux ou un sanctuaire."),
        }; }

        public static DefObjet[] Objets = ObjetsParDefaut();

        public static DefObjet Objet(string id)
        {
            for (int i = 0; i < Objets.Length; i++) if (Objets[i].Id == id) return Objets[i];
            return null;
        }

        public static readonly string[] NomsTypesCase =
        { "Valcreux", "Chemin", "Monstre", "Trésor", "Événement", "Malédiction", "Sanctuaire", "Marchand", "Taverne", "Antre du dragon" };

        public static readonly string[] AidesTypesCase =
        {
            "La cité : boutique complète, soins gratuits et quêtes. De l'or et de l'XP de renommée à chaque passage.",
            "Un chemin tranquille.",
            "Un monstre de la région surgit. Combattez-le ou fuyez.",
            "Un coffre ! De l'or, parfois un objet.",
            "Un événement tiré au hasard, heureux ou non.",
            "Une malédiction vous frappe : niveau, or ou force peuvent y passer.",
            "Soins complets, malédictions levées.",
            "Un marchand itinérant et son étal.",
            "Une taverne : quêtes et rumeurs.",
            "Le repaire d'Ignathor. Un combat légendaire.",
        };

        /// Réglages globaux (modifiables dans la fenêtre du catalogue).
        public static Reglages Reglages = new Reglages();
        public static int XPParNiveau { get { return Math.Max(1, Reglages.XPParNiveau); } }
        public static int OrDepart { get { return Reglages.OrDepart; } }
        public static int SoldeVille { get { return Reglages.SoldeVille; } }
        public static int XPVille { get { return Reglages.XPVille; } }
        public static int TailleSac { get { return Math.Max(1, Reglages.TailleSac); } }
        public static int AccessoiresMax { get { return Math.Max(0, Reglages.AccessoiresMax); } }
        public static int PiegesMaxParJoueur { get { return Math.Max(0, Reglages.PiegesMaxParJoueur); } }

        /// Les événements (16 d'origine + ceux créés dans l'éditeur). Voir Evenements.cs.
        public static List<DefEvenement> Evenements = EvenementsParDefaut.Creer();

        public static DefEvenement Evenement(string id)
        {
            foreach (var e in Evenements) if (e.Id == id) return e;
            return null;
        }

        /// Remplace les données du jeu (catalogue de l'éditeur). Un argument null garde ce qui est en place.
        /// Les classes manquantes sont reprises des valeurs d'origine ; une liste vide garde les valeurs en place.
        public static void Appliquer(IList<DefClasse> classes, IList<DefMonstre> monstres, IList<DefObjet> objets,
                                     IList<DefEvenement> evenements, Reglages reglages)
        {
            if (classes != null)
            {
                var d = ClassesParDefaut();
                foreach (var c in classes) if (c != null && (int)c.Classe >= 0 && (int)c.Classe < d.Length) d[(int)c.Classe] = c.Copie();
                Classes = d;
            }
            if (monstres != null)
            {
                var l = new List<DefMonstre>();
                foreach (var m in monstres) if (m != null && !string.IsNullOrEmpty(m.Id)) l.Add(m.Copie());
                // Sans monstre errant, on garde le Dragonnoie d'origine (pour s'en passer : Reglages.DragonnoieActif).
                if (l.Count > 0 && !l.Exists(m => m.Errant))
                    foreach (var m in MonstresParDefaut()) if (m.Errant) l.Add(m);
                if (l.Count > 0) Monstres = l.ToArray();
            }
            if (objets != null)
            {
                var l = new List<DefObjet>();
                foreach (var o in objets) if (o != null && !string.IsNullOrEmpty(o.Id)) l.Add(o.Copie());
                if (l.Count > 0) Objets = l.ToArray();
            }
            if (evenements != null)
            {
                var l = new List<DefEvenement>();
                foreach (var e in evenements) if (e != null && !string.IsNullOrEmpty(e.Id)) l.Add(e.Copie());
                if (l.Count > 0) Evenements = l;
            }
            if (reglages != null) Reglages = reglages.Copie();
        }

        /// Revient aux valeurs écrites dans ce fichier.
        public static void Reinitialiser()
        {
            Classes = ClassesParDefaut(); Monstres = MonstresParDefaut(); Objets = ObjetsParDefaut();
            Evenements = EvenementsParDefaut.Creer(); Reglages = new Reglages();
        }

        public static DefClasse[] CopieClassesParDefaut() { return ClassesParDefaut(); }
        public static DefMonstre[] CopieMonstresParDefaut() { return MonstresParDefaut(); }
        public static DefObjet[] CopieObjetsParDefaut() { return ObjetsParDefaut(); }
    }
}

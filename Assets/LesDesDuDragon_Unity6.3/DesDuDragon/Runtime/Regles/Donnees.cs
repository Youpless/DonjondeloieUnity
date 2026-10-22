// Les Dés du Dragon — données du jeu (classes, monstres, objets, cases, zones).
// Ce fichier ne dépend pas d'Unity : les règles tournent aussi hors du moteur (essais, simulations).
using System;
using System.Collections.Generic;

namespace DesDuDragon.Regles
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

    public sealed class DefClasse
    {
        public ClasseHeros Classe;
        public string Nom, Passif, Description;
        public int Attaque, Defense, Vie;
        public DefClasse(ClasseHeros c, string nom, int atk, int def, int vie, string passif, string desc)
        { Classe = c; Nom = nom; Attaque = atk; Defense = def; Vie = vie; Passif = passif; Description = desc; }
    }

    public sealed class DefMonstre
    {
        public string Id, Nom, Description;
        public int Force, Degats, Or, XP;
        public Zone[] Zones;
        public bool Boss;
        public DefMonstre(string id, string nom, int force, int degats, int or, int xp, Zone[] zones, string desc, bool boss = false)
        { Id = id; Nom = nom; Force = force; Degats = degats; Or = or; XP = xp; Zones = zones; Description = desc; Boss = boss; }
    }

    public sealed class DefObjet
    {
        public string Id, Nom, Description;
        public TypeObjet Type;
        public EffetObjet Effet;
        public int Prix, Bonus;
        public bool SeulementEnVille;
        public DefObjet(string id, string nom, TypeObjet type, int prix, int bonus, EffetObjet effet, string desc, bool ville = false)
        { Id = id; Nom = nom; Type = type; Prix = prix; Bonus = bonus; Effet = effet; Description = desc; SeulementEnVille = ville; }
        public bool Consommable { get { return Type == TypeObjet.Potion || Type == TypeObjet.Piege || Type == TypeObjet.Parchemin; } }
    }

    public static class Donnees
    {
        public static readonly DefClasse[] Classes =
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
        };

        public static DefClasse Classe(ClasseHeros c) { return Classes[(int)c]; }

        public static readonly string[] NomsZones = { "Valcreux", "Bois des Murmures", "Marais Putride", "Cryptes d'Ombrelune", "Pics de Braise", "Antre du Dragon" };

        public static readonly DefMonstre[] Monstres =
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
        };

        public static DefMonstre Monstre(string id)
        {
            for (int i = 0; i < Monstres.Length; i++) if (Monstres[i].Id == id) return Monstres[i];
            return null;
        }

        public static readonly DefObjet[] Objets =
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
        };

        public static DefObjet Objet(string id)
        {
            for (int i = 0; i < Objets.Length; i++) if (Objets[i].Id == id) return Objets[i];
            return null;
        }

        public static readonly string[] NomsTypesCase =
        { "Valcreux", "Chemin", "Monstre", "Trésor", "Événement", "Malédiction", "Sanctuaire", "Marchand", "Taverne", "Antre du dragon" };

        public static readonly string[] AidesTypesCase =
        {
            "La cité : boutique complète, soins gratuits et quêtes. +10 or et +1 XP de renommée à chaque passage.",
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

        /// Taille d'XP par niveau et autres réglages globaux.
        public const int XPParNiveau = 2;
        public const int OrDepart = 20;
        public const int SoldeVille = 10;
        public const int TailleSac = 5;
        public const int AccessoiresMax = 2;
        public const int PiegesMaxParJoueur = 3;
    }
}

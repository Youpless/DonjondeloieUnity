// Donjon de l'Oie — les événements en données : un texte, une suite d'effets, et éventuellement un choix.
// Les 16 événements d'origine sont écrits ici ; l'éditeur (fenêtre du catalogue) permet d'en changer et d'en créer.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    /// Ce que fait un effet. « Valeur » et « Parametre » changent de sens selon le type (voir Aide).
    public enum TypeEffet
    {
        Annonce,            // Parametre : texte affiché ({joueur} = son nom)
        Or,                 // Valeur : or gagné (négatif : perdu)
        PV,                 // Valeur : PV rendus (négatif : blessure)
        SoinTotal,          // tous les PV
        XP,                 // Valeur : XP gagnée (négatif : perdue, sans perdre de niveau)
        Niveau,             // Valeur : niveaux gagnés (négatif : perdus)
        Avancer,            // Valeur : cases à avancer (la case d'arrivée fait son effet)
        Reculer,            // Valeur : cases à reculer
        TeleporterHasard,   // vers une case de la grande boucle ; Valeur 1 : la case d'arrivée fait son effet
        TeleporterDepart,   // vers la case de départ (Valcreux)
        TeleporterCase,     // Parametre : nom du lieu ; Valeur 1 : la case d'arrivée fait son effet
        Combat,             // Parametre : id du monstre (vide : un monstre de la zone)
        Boutique,           // Valeur : remise en % ; Parametre : ids d'objets séparés par des virgules (vide : 5 au hasard)
        Quetes,             // Parametre : nom de celui qui les propose
        ObjetHasard,        // Parametre : Potion, Piege ou Parchemin (vide : un consommable) ; sac plein : Valeur or à la place
        ObjetDonne,         // Parametre : id de l'objet ; sac plein : Valeur or à la place
        Faiblesse,          // Valeur : tours à −2 en attaque
        Immobiliser,        // Valeur : tours passés
        Lenteur,            // le prochain jet de dé est divisé par deux
        Benediction,        // Valeur : bonus au prochain combat contre un monstre
        JetMalediction,     // le tableau des malédictions (1D6)
        LeverMalediction,   // retire faiblesse, lenteur et immobilisation
    }

    /// Sur qui porte l'effet. Les effets de déplacement, de combat, de boutique et de quêtes portent toujours sur le joueur.
    public enum CibleEffet { Joueur, Tous, Autres, Meneur, Dernier }

    [Serializable]
    public sealed class EffetEvenement
    {
        public TypeEffet Type;
        public int Valeur;
        public string Parametre = "";
        public CibleEffet Cible;
        public int Chance = 100;            // % de chances que l'effet se produise

        public EffetEvenement() { }
        public EffetEvenement(TypeEffet t, int v = 0, string p = "", CibleEffet c = CibleEffet.Joueur, int chance = 100)
        { Type = t; Valeur = v; Parametre = p ?? ""; Cible = c; Chance = chance; }
        public EffetEvenement Copie() { return (EffetEvenement)MemberwiseClone(); }

        public static bool SeulementLeJoueur(TypeEffet t)
        {
            switch (t)
            {
                case TypeEffet.Avancer: case TypeEffet.Reculer: case TypeEffet.TeleporterHasard: case TypeEffet.TeleporterCase:
                case TypeEffet.Combat: case TypeEffet.Boutique: case TypeEffet.Quetes: case TypeEffet.Annonce:
                    return true;
            }
            return false;
        }

        /// Aide affichée dans l'éditeur.
        public static string Aide(TypeEffet t)
        {
            switch (t)
            {
                case TypeEffet.Annonce: return "Paramètre : le texte à afficher ({joueur} = son nom).";
                case TypeEffet.Or: return "Valeur : or gagné (négatif : perdu).";
                case TypeEffet.PV: return "Valeur : PV rendus (négatif : blessure, peut mettre K.O.).";
                case TypeEffet.SoinTotal: return "Rend tous les PV.";
                case TypeEffet.XP: return "Valeur : XP gagnée (négatif : perdue, sans perdre de niveau).";
                case TypeEffet.Niveau: return "Valeur : niveaux gagnés (négatif : perdus).";
                case TypeEffet.Avancer: return "Valeur : cases à avancer. La case d'arrivée fait son effet.";
                case TypeEffet.Reculer: return "Valeur : cases à reculer (sans effet d'arrivée).";
                case TypeEffet.TeleporterHasard: return "Vers une case de la grande boucle. Valeur 1 : la case d'arrivée fait son effet.";
                case TypeEffet.TeleporterDepart: return "Vers la case de départ.";
                case TypeEffet.TeleporterCase: return "Paramètre : nom du lieu (ex. Chapelle des Saules). Valeur 1 : la case fait son effet.";
                case TypeEffet.Combat: return "Paramètre : id du monstre (vide : un monstre de la zone de la case).";
                case TypeEffet.Boutique: return "Valeur : remise en %. Paramètre : ids d'objets séparés par des virgules (vide : 5 au hasard).";
                case TypeEffet.Quetes: return "Propose des quêtes. Paramètre : nom de celui qui les propose.";
                case TypeEffet.ObjetHasard: return "Paramètre : Potion, Piege ou Parchemin (vide : n'importe quel consommable). Sac plein : Valeur or.";
                case TypeEffet.ObjetDonne: return "Paramètre : id de l'objet. Sac plein : Valeur or à la place.";
                case TypeEffet.Faiblesse: return "Valeur : nombre de tours à −2 en attaque.";
                case TypeEffet.Immobiliser: return "Valeur : nombre de tours passés.";
                case TypeEffet.Lenteur: return "Le prochain jet de déplacement est divisé par deux.";
                case TypeEffet.Benediction: return "Valeur : bonus au prochain combat contre un monstre.";
                case TypeEffet.JetMalediction: return "Le tableau des malédictions (1D6) : niveau, or, faiblesse, pétrification, brûlure ou rien.";
                default: return "Retire faiblesse, lenteur et immobilisation.";
            }
        }
    }

    [Serializable]
    public sealed class ChoixEvenement
    {
        public string Libelle = "Choix", Aide = "";
        public List<EffetEvenement> Effets = new List<EffetEvenement>();

        public ChoixEvenement() { }
        public ChoixEvenement(string libelle, string aide, params EffetEvenement[] effets)
        { Libelle = libelle; Aide = aide ?? ""; Effets = new List<EffetEvenement>(effets); }
        public ChoixEvenement Copie()
        {
            var c = new ChoixEvenement { Libelle = Libelle, Aide = Aide };
            foreach (var e in Effets) c.Effets.Add(e.Copie());
            return c;
        }
    }

    [Serializable]
    public sealed class DefEvenement
    {
        public string Id = "", Nom = "Nouvel événement";
        public string Texte = "";                       // annonce ({joueur} = son nom)
        public Ton Ton = Ton.Neutre;
        public bool DansLaPioche = true;                // tiré sur les cases « Événement » qui n'ont pas leur propre liste
        public int Poids = 10;                          // plus il est grand, plus il sort souvent
        public List<Zone> Zones = new List<Zone>();     // vide : toutes les zones
        public List<EffetEvenement> Effets = new List<EffetEvenement>();
        public List<ChoixEvenement> Choix = new List<ChoixEvenement>();   // vide : pas de choix

        public DefEvenement() { }
        public DefEvenement(string id, string nom, string texte, Ton ton, params EffetEvenement[] effets)
        { Id = id; Nom = nom; Texte = texte; Ton = ton; Effets = new List<EffetEvenement>(effets); }

        public DefEvenement Copie()
        {
            var e = new DefEvenement { Id = Id, Nom = Nom, Texte = Texte, Ton = Ton, DansLaPioche = DansLaPioche, Poids = Poids, Zones = new List<Zone>(Zones ?? new List<Zone>()) };
            if (Effets != null) foreach (var x in Effets) e.Effets.Add(x.Copie());
            if (Choix != null) foreach (var c in Choix) e.Choix.Add(c.Copie());
            return e;
        }

        public bool PossibleEn(Zone z) { return Zones == null || Zones.Count == 0 || Zones.Contains(z); }
    }

    public static class EvenementsParDefaut
    {
        static EffetEvenement E(TypeEffet t, int v = 0, string p = "", CibleEffet c = CibleEffet.Joueur, int chance = 100) { return new EffetEvenement(t, v, p, c, chance); }

        public static List<DefEvenement> Creer()
        {
            return new List<DefEvenement>
            {
                new DefEvenement("fontaine", "Fontaine bénie", "Fontaine bénie : l'eau claire referme les plaies de {joueur}.", Ton.Bon, E(TypeEffet.PV, 5)),
                new DefEvenement("bourse", "Bourse oubliée", "Bourse oubliée au bord du chemin !", Ton.Bon, E(TypeEffet.Or, 12)),
                new DefEvenement("tirelaine", "Tire-laine", "Un tire-laine bouscule {joueur} et file avec sa bourse.", Ton.Mauvais, E(TypeEffet.Or, -10)),
                new DefEvenement("barde", "Barde errant", "Un barde chante les exploits de {joueur}.", Ton.Bon, E(TypeEffet.XP, 1)),
                new DefEvenement("orage", "Orage arcanique", "Orage arcanique ! La foudre frappe tous les aventuriers.", Ton.Mauvais, E(TypeEffet.PV, -2, "", CibleEffet.Tous)),
                new DefEvenement("vent", "Vent favorable", "Vent favorable : {joueur} avance de 3 cases.", Ton.Bon, E(TypeEffet.Avancer, 3)),
                new DefEvenement("eboulement", "Éboulement", "Éboulement ! {joueur} recule de 2 cases.", Ton.Mauvais, E(TypeEffet.Reculer, 2), E(TypeEffet.PV, -1)),
                new DefEvenement("chevalier", "Chevalier errant", "Un chevalier errant bénit l'arme de {joueur} : +2 à son prochain combat contre un monstre.", Ton.Bon, E(TypeEffet.Benediction, 2)),
                new DefEvenement("colporteur", "Colporteur", "Un colporteur ouvre sa carriole : tout à −30 % !", Ton.Bon, E(TypeEffet.Boutique, 30)),
                new DefEvenement("piece", "Pièce maudite", "Une pièce d'or brille dans la boue… maudite ! +20 or, mais −2 en attaque pendant 3 tours.", Ton.Mauvais, E(TypeEffet.Or, 20), E(TypeEffet.Faiblesse, 3)),
                new DefEvenement("ermite", "Vieil ermite", "Un vieil ermite a des missions à confier.", Ton.Quete, E(TypeEffet.Quetes, 0, "Le vieil ermite")),
                new DefEvenement("portail", "Portail instable", "Un portail instable aspire {joueur}…", Ton.Neutre, E(TypeEffet.TeleporterHasard, 1)),
                new DefEvenement("fees", "Festin des fées", "Festin des fées : {joueur} retrouve toutes ses forces.", Ton.Bon, E(TypeEffet.SoinTotal)),
                new DefEvenement("coffrepiege", "Coffre piégé", "Coffre piégé ! Une aiguille empoisonnée, mais 15 or à l'intérieur.", Ton.Neutre, E(TypeEffet.Or, 15), E(TypeEffet.PV, -3)),
                new DefEvenement("parchemin", "Parchemin perdu", "{joueur} trouve un parchemin dans les herbes.", Ton.Bon, E(TypeEffet.ObjetHasard, 10, "Parchemin")),
                new DefEvenement("gobelins", "Embuscade gobeline", "Des gobelins surgissent des fourrés !", Ton.Combat, E(TypeEffet.Combat, 0, "gobelin")),
                // un exemple d'événement à choix, à copier pour créer les vôtres
                new DefEvenement("idole", "Idole oubliée", "Une idole couverte de mousse veille au bord du chemin.", Ton.Neutre)
                {
                    Poids = 8,
                    Choix = new List<ChoixEvenement>
                    {
                        new ChoixEvenement("Prier (10 or d'offrande)", "−10 or, +1 XP, et vos malédictions sont levées.", E(TypeEffet.Or, -10), E(TypeEffet.XP, 1), E(TypeEffet.LeverMalediction)),
                        new ChoixEvenement("Piller ses gemmes", "+25 or… mais l'idole maudit les voleurs (1D6).", E(TypeEffet.Or, 25), E(TypeEffet.JetMalediction)),
                        new ChoixEvenement("Passer son chemin", "Il ne se passe rien."),
                    }
                },
            };
        }
    }
}

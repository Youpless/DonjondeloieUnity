// Donjon de l'Oie — le Dragonnoie : mi-oie, mi-dragon, il erre sur la carte. Au début d'un tour de table sur trois
// (réglable), il prend une grande envolée vers une case au hasard et défie tous ceux qui s'y trouvent. Qui passe sur sa
// case peut l'attaquer ou continuer sa route ; qui s'y arrête doit l'affronter. Le combat se joue comme contre un monstre
// (attaque + 1D6 contre sa force + 1D6). Vaincu, on perd tout son or, jamais de niveau ; vainqueur, on gagne la partie.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class Partie
    {
        public int CaseDragonnoie = -1;     // sa case ; -1 : pas de Dragonnoie dans cette partie
        public int OrDragonnoie;            // l'or arraché aux vaincus, entassé dans son nid
        public int TourEnvol;               // le tour de table au début duquel il s'envolera
        public bool VaincuDragonnoie;       // la partie s'est gagnée en le terrassant

        /// Sa fiche (catalogue), ou null s'il n'est pas sur la carte.
        public DefMonstre Dragonnoie { get { return CaseDragonnoie >= 0 ? Donnees.Dragonnoie : null; } }

        /// Tours de table entiers avant son prochain envol (0 : il s'envole au début du prochain tour de table).
        public int ToursAvantEnvol { get { return Math.Max(0, TourEnvol - Tour - (Courant == 0 ? 0 : 1)); } }

        static int Intervalle { get { return Math.Max(1, Donnees.Reglages.ToursEntreEnvols); } }

        // ------------------------------------------------------------------ ses vols

        /// Au début de la partie : il se pose loin de la case de départ.
        IEnumerable<Signal> ArriveeDragonnoie()
        {
            if (Donnees.Dragonnoie == null) yield break;
            var possibles = new List<int>();
            for (int i = 0; i < Plateau.Nombre; i++)
            {
                if (i == Plateau.Depart) continue;
                int d = Plateau.DistanceEnAvant(Plateau.Depart, i, 60);
                if (d < 0 || d >= 9) possibles.Add(i);           // hors d'atteinte au premier tour
            }
            if (possibles.Count == 0) for (int i = 0; i < Plateau.Nombre; i++) if (i != Plateau.Depart) possibles.Add(i);
            if (possibles.Count == 0) yield break;
            CaseDragonnoie = Des.Choisir(possibles);
            TourEnvol = Tour + Intervalle;
            yield return new SEnvol { De = -1, Vers = CaseDragonnoie };
            yield return new SAnnonce("Le Dragonnoie fond sur l'île et se pose sur « " + Plateau[CaseDragonnoie].Libelle
                + " ». Il change de perchoir tous les " + Intervalle + " tours. Qui le terrasse gagne la partie !", -1, Ton.Combat, true);
        }

        /// Au début d'un tour de table sur N : une grande envolée vers une case au hasard, puis il défie ceux qui y sont.
        IEnumerable<Signal> EnvolDragonnoie()
        {
            TourEnvol = Tour + Intervalle;
            int de = CaseDragonnoie;
            int vers = CaseAtterrissage(de);
            if (vers < 0) yield break;
            CaseDragonnoie = vers;
            yield return new SEnvol { De = de, Vers = vers };
            var defies = JoueursSur(vers);
            var noms = new List<string>();
            foreach (var j in defies) noms.Add(j.Nom);
            yield return new SAnnonce("Grande envolée ! Le Dragonnoie se pose sur « " + Plateau[vers].Libelle + " »"
                + (defies.Count > 0 ? " et défie " + string.Join(", ", noms.ToArray()) + " !" : "."), -1, Ton.Combat, true);
            foreach (var j in defies)
            {
                foreach (var s in CombatDragonnoie(j)) yield return s;
                if (Fini) yield break;
            }
        }

        /// Une case loin de la sienne (à plus de 12 m), jamais la case de départ.
        int CaseAtterrissage(int depuis)
        {
            var loin = new List<int>(); var autres = new List<int>();
            for (int i = 0; i < Plateau.Nombre; i++)
            {
                if (i == Plateau.Depart || i == depuis) continue;
                autres.Add(i);
                if (depuis < 0) { loin.Add(i); continue; }
                float dx = Plateau[i].X - Plateau[depuis].X, dz = Plateau[i].Z - Plateau[depuis].Z;
                if (dx * dx + dz * dz >= 12f * 12f) loin.Add(i);
            }
            if (loin.Count > 0) return Des.Choisir(loin);
            return autres.Count > 0 ? Des.Choisir(autres) : -1;
        }

        // ------------------------------------------------------------------ rencontres

        /// En passant sur sa case : l'attaquer (le déplacement s'arrête là) ou continuer sa route.
        IEnumerable<Signal> PasserDragonnoie(Joueur j, int pasRestants, Action<bool> attaque)
        {
            var m = Dragonnoie;
            var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Dragonnoie, Monstre = m, Titre = m.Nom,
                Texte = "Le Dragonnoie vous barre la route. Il vous reste " + pasRestants + " case" + (pasRestants > 1 ? "s" : "") + " à parcourir." };
            q.Ajouter("Attaquer le Dragonnoie", "combat", "Victoire : la partie est à vous. Défaite : il vous arrache tout votre or.");
            q.Ajouter("Continuer sa route", "passer", "Le contourner et finir votre déplacement.");
            q.ParDefaut = 1;
            yield return q;
            attaque(q.CodeChoisi == "combat");
        }

        /// S'arrêter sur sa case, quelle qu'en soit la raison (dé, recul, téléportation, fuite) : il faut l'affronter.
        /// « combat » (facultatif) reçoit vrai s'il y a eu combat.
        IEnumerable<Signal> ArretDragonnoie(Joueur j, Action<bool> combat)
        {
            bool la = Dragonnoie != null && !Fini && j.Case == CaseDragonnoie;
            if (combat != null) combat(la);
            if (!la) yield break;
            yield return new SAnnonce(j.Nom + " s'arrête devant le Dragonnoie : pas moyen de l'éviter !", j.Id, Ton.Combat, true);
            foreach (var s in CombatDragonnoie(j)) yield return s;
        }

        /// Le combat. Vainqueur : la partie est gagnée (et son nid avec). Vaincu : tout son or perdu, aucun niveau.
        IEnumerable<Signal> CombatDragonnoie(Joueur j)
        {
            var m = Dragonnoie;
            if (m == null || Fini) yield break;
            var c = CombatContreMonstre(j, m);
            yield return c;
            if (c.AttaquantGagne)
            {
                j.MonstresTues++;
                VaincuDragonnoie = true;
                yield return new SAnnonce(j.Nom + " terrasse le Dragonnoie !", j.Id, Ton.Combat, true);
                if (OrDragonnoie > 0) foreach (var s in GagnerOr(j, OrDragonnoie, "le nid du Dragonnoie")) yield return s;
                OrDragonnoie = 0;
                Fini = true; Gagnant = j.Id;
                yield return new SEffet { Effet = Effet.Victoire, Joueur = j.Id, Texte = "dragonnoie" };
                yield return new SFinPartie { Gagnant = j.Id };
            }
            else
            {
                int perte = j.Or;
                yield return new SAnnonce("Le Dragonnoie terrasse " + j.Nom + (perte > 0 ? " et lui arrache toute sa bourse : −" + perte + " or."
                    : ", qui n'avait heureusement pas un sou."), j.Id, Ton.Mauvais, true);
                if (perte > 0)
                {
                    j.Or = 0;
                    OrDragonnoie += perte;
                    yield return new SEffet { Effet = Effet.OrDragonnoie, Joueur = j.Id, Case = CaseDragonnoie, Texte = perte.ToString() };
                    yield return new SStat(j.Id, Stat.Or, -perte);
                }
            }
        }
    }
}

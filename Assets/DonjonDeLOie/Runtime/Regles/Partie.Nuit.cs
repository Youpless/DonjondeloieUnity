// Donjon de l'Oie — la nuit : elle tombe après chaque cinquième tour de table (réglable). Un événement l'occupe, tiré
// parmi ceux qui peuvent avoir lieu : le voleur de la nuit (un joueur perd la moitié de son or), le trou de ver (deux
// joueurs échangent leur place), la bonne nuit (un joueur gagne un niveau, jamais celui de la victoire) et l'arène du
// diable (un tournoi : chaque défaite coûte un niveau, le vainqueur gagne un niveau et de l'or).
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public enum EvenementNuit { Voleur, TrouDeVer, BonneNuit, Arene }

    public sealed partial class Partie
    {
        public int Nuits;                       // nuits déjà tombées
        public EvenementNuit DerniereNuit;      // l'événement de la dernière nuit (si Nuits > 0)
        int tourDerniereNuit;

        static int IntervalleNuits { get { return Math.Max(1, Donnees.Reglages.ToursEntreNuits); } }

        /// La nuit tombe au début du tour de table qui suit chaque N-ième : tours 6, 11, 16… pour N = 5.
        public static bool NuitAvantLeTour(int tour)
        {
            return Donnees.Reglages.NuitsActives && tour > 1 && (tour - 1) % IntervalleNuits == 0;
        }

        /// Le tour de table après lequel tombera la prochaine nuit (-1 : jamais).
        public int ProchaineNuit
        {
            get
            {
                if (!Donnees.Reglages.NuitsActives) return -1;
                int t = Tour + 1;
                if (Courant == 0 && tourDerniereNuit != Tour && NuitAvantLeTour(Tour)) t = Tour;   // elle tombe tout de suite
                while (!NuitAvantLeTour(t)) t++;
                return t - 1;
            }
        }

        IEnumerable<Signal> Nuit()
        {
            tourDerniereNuit = Tour;
            Nuits++;
            yield return new SNuit { Tombe = true, Tour = Tour - 1, Numero = Nuits };
            yield return new SAnnonce("Fin du tour " + (Tour - 1) + " : la nuit tombe sur l'île…", -1, Ton.Systeme, true);

            var possibles = new List<EvenementNuit>();
            var rg = Donnees.Reglages;
            if (rg.NuitVoleur && Joueurs.Exists(j => j.Or >= 2)) possibles.Add(EvenementNuit.Voleur);
            if (rg.NuitTrouDeVer && PaireSurDeuxCases() != null) possibles.Add(EvenementNuit.TrouDeVer);
            if (rg.NuitBonneNuit && Joueurs.Exists(j => j.Niveau + 1 < NiveauCible)) possibles.Add(EvenementNuit.BonneNuit);
            if (rg.NuitArene && Joueurs.Count >= 2) possibles.Add(EvenementNuit.Arene);
            if (possibles.Count > 0)
            {
                DerniereNuit = Des.Choisir(possibles);
                IEnumerable<Signal> ev;
                switch (DerniereNuit)
                {
                    case EvenementNuit.Voleur: ev = NuitVoleur(); break;
                    case EvenementNuit.TrouDeVer: ev = NuitTrouDeVer(); break;
                    case EvenementNuit.BonneNuit: ev = NuitBonneNuit(); break;
                    default: ev = NuitArene(); break;
                }
                foreach (var s in ev) yield return s;
                if (Fini) yield break;          // l'arène peut couronner un vainqueur en pleine nuit
            }
            yield return new SNuit { Tombe = false, Tour = Tour - 1, Numero = Nuits };
            yield return new SAnnonce("Le jour se lève.", -1, Ton.Systeme);
        }

        // ------------------------------------------------------------------ le voleur de la nuit

        IEnumerable<Signal> NuitVoleur()
        {
            var v = Des.Choisir(Joueurs.FindAll(j => j.Or >= 2));
            int vol = v.Or / 2;
            v.Or -= vol;
            yield return new SEffet { Effet = Effet.VolNocturne, Joueur = v.Id, Texte = vol.ToString() };
            yield return new SStat(v.Id, Stat.Or, -vol);
            yield return new SAnnonce("Un voleur se glisse dans le campement de " + v.Nom + " : la moitié de sa bourse disparaît (−" + vol + " or).",
                v.Id, Ton.Mauvais, true);
        }

        // ------------------------------------------------------------------ le trou de ver

        /// Deux joueurs sur deux cases différentes, tirés au hasard parmi toutes les paires possibles (null s'il n'y en a pas).
        Joueur[] PaireSurDeuxCases()
        {
            var paires = new List<Joueur[]>();
            for (int a = 0; a < Joueurs.Count; a++)
                for (int b = a + 1; b < Joueurs.Count; b++)
                    if (Joueurs[a].Case != Joueurs[b].Case) paires.Add(new[] { Joueurs[a], Joueurs[b] });
            return paires.Count > 0 ? Des.Choisir(paires) : null;
        }

        IEnumerable<Signal> NuitTrouDeVer()
        {
            var p = PaireSurDeuxCases();
            Joueur a = p[0], b = p[1];
            int ca = a.Case, cb = b.Case;
            yield return new SEffet { Effet = Effet.TrouDeVer, Joueur = a.Id, Cible = b.Id, Case = ca };
            yield return new SAnnonce("Un trou de ver s'ouvre dans la nuit : " + a.Nom + " et " + b.Nom + " échangent leur place !", -1, Ton.Systeme, true);
            foreach (var s in Teleporter(a, cb, false, "Trou de ver")) yield return s;
            if (Fini) yield break;
            foreach (var s in Teleporter(b, ca, false, "Trou de ver")) yield return s;
        }

        // ------------------------------------------------------------------ la bonne nuit

        IEnumerable<Signal> NuitBonneNuit()
        {
            var j = Des.Choisir(Joueurs.FindAll(o => o.Niveau + 1 < NiveauCible));
            yield return new SEffet { Effet = Effet.BonneNuit, Joueur = j.Id };
            yield return new SAnnonce(j.Nom + " dort comme un loir et se réveille plus fort.", j.Id, Ton.Bon, true);
            foreach (var s in GagnerNiveau(j, false)) yield return s;
        }

        // ------------------------------------------------------------------ l'arène du diable

        /// Un tournoi dans un ordre tiré au sort : le champion reste dans l'arène et affronte le suivant. Chaque défaite
        /// coûte un niveau ; le dernier champion gagne un niveau (même celui de la victoire) et de l'or.
        IEnumerable<Signal> NuitArene()
        {
            var ordre = new List<Joueur>(Joueurs);
            for (int i = ordre.Count - 1; i > 0; i--) { int k = Des.Entre(0, i); var t = ordre[i]; ordre[i] = ordre[k]; ordre[k] = t; }
            var ids = new List<string>(); var noms = new List<string>();
            foreach (var j in ordre) { ids.Add(j.Id.ToString()); noms.Add(j.Nom); }
            yield return new SEffet { Effet = Effet.AreneDebut, Texte = string.Join(",", ids.ToArray()) };
            yield return new SAnnonce("L'arène du diable surgit des ténèbres ! Tournoi nocturne, dans cet ordre : " + string.Join(", ", noms.ToArray()) + ".",
                -1, Ton.Combat, true);
            var champion = ordre[0];
            for (int k = 1; k < ordre.Count; k++)
            {
                var defi = ordre[k];
                yield return new SAnnonce(defi.Nom + " entre dans l'arène face à " + champion.Nom + ".", defi.Id, Ton.Combat);
                SCombat c = null;
                foreach (var s in DuelArene(defi, champion, r => c = r)) yield return s;
                var gagnant = c.AttaquantGagne ? defi : champion;
                var perdant = c.AttaquantGagne ? champion : defi;
                gagnant.DuelsGagnes++; perdant.DuelsPerdus++;
                yield return new SAnnonce(gagnant.Nom + " l'emporte sur " + perdant.Nom + " (" + Math.Max(c.TotalA, c.TotalB) + " contre "
                    + Math.Min(c.TotalA, c.TotalB) + ").", gagnant.Id, Ton.Combat, true);
                foreach (var s in PerdreNiveau(perdant, 1, "vaincu dans l'arène du diable")) yield return s;
                champion = gagnant;
            }
            yield return new SEffet { Effet = Effet.AreneFin, Joueur = champion.Id };
            yield return new SAnnonce(champion.Nom + " remporte l'arène du diable !", champion.Id, Ton.Combat, true);
            foreach (var s in GagnerOr(champion, Donnees.Reglages.OrArene, "l'arène du diable")) yield return s;
            foreach (var s in GagnerNiveau(champion, true)) yield return s;
        }

        /// Un duel de l'arène : attaque + 1D3 chacun ; deux relances en cas d'égalité, puis le champion tient bon.
        IEnumerable<Signal> DuelArene(Joueur defi, Joueur champion, Action<SCombat> resultat)
        {
            SCombat c = null;
            for (int lancer = 1; lancer <= 3; lancer++)
            {
                c = new SCombat { Duel = true, Arene = true, Attaquant = defi.Id, Defenseur = champion.Id, FacesDe = 3, AtkA = defi.Attaque, AtkB = champion.Attaque };
                c.DeA = Des.D3(); c.DeB = Des.D3();
                if (c.TotalA == c.TotalB && lancer < 3)
                {
                    c.Egalite = true;
                    yield return c;
                    yield return new SAnnonce("Égalité ! On relance.", defi.Id, Ton.Combat);
                    continue;
                }
                c.AttaquantGagne = c.TotalA > c.TotalB;
                yield return c;
                break;
            }
            resultat(c);
        }

        // ------------------------------------------------------------------ un niveau gagné d'un coup

        /// Un niveau de plus, sans passer par l'expérience. « peutGagner » faux : jamais le niveau de la victoire.
        IEnumerable<Signal> GagnerNiveau(Joueur j, bool peutGagner)
        {
            if (Fini || (!peutGagner && j.Niveau + 1 >= NiveauCible)) yield break;
            j.Niveau++;
            j.NiveauMax = Math.Max(j.NiveauMax, j.Niveau);
            j.PV = Math.Min(j.PVMax, j.PV + 2);
            yield return new SStat(j.Id, Stat.Niveau, 1);
            yield return new SEffet { Effet = Effet.NiveauGagne, Joueur = j.Id };
            yield return new SAnnonce(j.Nom + " passe au niveau " + j.Niveau + " !", j.Id, Ton.Bon, true);
            if (j.Niveau >= NiveauCible)
            {
                Fini = true; Gagnant = j.Id;
                yield return new SEffet { Effet = Effet.Victoire, Joueur = j.Id };
                yield return new SFinPartie { Gagnant = j.Id };
            }
        }
    }
}

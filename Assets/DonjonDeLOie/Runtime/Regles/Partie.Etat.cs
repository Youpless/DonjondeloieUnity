// Donjon de l'Oie — la partie : l'état des joueurs (soins, blessures, K.O., or, expérience, niveaux, amulette) et les dés.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class Partie
    {
        IEnumerable<Signal> Soigner(Joueur j, int n, string raison)
        {
            int avant = j.PV;
            j.PV = Math.Min(j.PVMax, j.PV + n);
            if (j.PV != avant)
            {
                yield return new SStat(j.Id, Stat.PV, j.PV - avant);
                if (raison != null) yield return new SEffet { Effet = Effet.Soin, Joueur = j.Id, Texte = raison };
            }
        }

        IEnumerable<Signal> Blesser(Joueur j, int n, string raison)
        {
            if (n <= 0) yield break;
            j.PV -= n;
            yield return new SStat(j.Id, Stat.PV, -n);
            if (j.PV <= 0)
                foreach (var s in Ko(j, raison)) yield return s;
        }

        IEnumerable<Signal> Ko(Joueur j, string raison)
        {
            j.KO++;
            int perte = j.Or / 2;
            yield return new SEffet { Effet = Effet.KO, Joueur = j.Id, Texte = raison };
            yield return new SAnnonce(j.Nom + " est K.O. (" + raison + ") ! Retour à " + Plateau[Plateau.Depart].Libelle + ", la moitié de son or perdue.", j.Id, Ton.Mauvais, true);
            if (perte > 0) { j.Or -= perte; yield return new SStat(j.Id, Stat.Or, -perte); }
            j.Embuscade = null;
            j.Case = Plateau.Depart;
            yield return new STeleport { Joueur = j.Id, Vers = Plateau.Depart, Raison = "K.O." };
            int pvAvant = j.PV;
            j.PV = Math.Max(1, j.PVMax / 2);
            yield return new SStat(j.Id, Stat.PV, j.PV - pvAvant);
            if (j == JoueurCourant) tourFini = true;
        }

        IEnumerable<Signal> GagnerOr(Joueur j, int n, string raison)
        {
            if (n <= 0) yield break;
            j.Or += n; j.OrGagne += n;
            yield return new SStat(j.Id, Stat.Or, n);
            if (raison != null) yield return new SAnnonce(j.Nom + " : +" + n + " or (" + raison + ").", j.Id, Ton.Bon);
        }

        IEnumerable<Signal> PerdreOr(Joueur j, int n, string raison)
        {
            n = Math.Min(n, j.Or);
            if (n <= 0) yield break;
            j.Or -= n;
            yield return new SStat(j.Id, Stat.Or, -n);
            if (raison != null) yield return new SAnnonce(j.Nom + " : −" + n + " or (" + raison + ").", j.Id, Ton.Mauvais);
        }

        IEnumerable<Signal> GagnerXP(Joueur j, int n)
        {
            if (n <= 0 || Fini) yield break;
            j.XP += n;
            yield return new SStat(j.Id, Stat.XP, n);
            while (j.XP >= Donnees.XPParNiveau && !Fini)
            {
                j.XP -= Donnees.XPParNiveau;
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

        IEnumerable<Signal> PerdreNiveau(Joueur j, int n, string raison)
        {
            int avant = j.Niveau;
            j.Niveau = Math.Max(1, j.Niveau - n);
            int perdu = avant - j.Niveau;
            if (perdu <= 0)
            {
                if (j.XP > 0) { int x = j.XP; j.XP = 0; yield return new SStat(j.Id, Stat.XP, -x); }
                yield return new SAnnonce(j.Nom + " est déjà au niveau 1 : il perd son expérience (" + raison + ").", j.Id, Ton.Mauvais);
                yield break;
            }
            j.PV = Math.Min(j.PV, j.PVMax);
            yield return new SStat(j.Id, Stat.Niveau, -perdu);
            yield return new SEffet { Effet = Effet.NiveauPerdu, Joueur = j.Id, Texte = raison };
            yield return new SAnnonce(j.Nom + " perd " + (perdu > 1 ? perdu + " niveaux" : "un niveau") + " (" + raison + ") → niveau " + j.Niveau + ".", j.Id, Ton.Mauvais, true);
        }

        /// Une protection (amulette) bloque l'effet néfaste ? Elle se brise alors.
        IEnumerable<Signal> Amulette(Joueur j, Action<bool> bloque)
        {
            if (j.A(EffetObjet.Amulette))
            {
                j.RetirerAccessoire(EffetObjet.Amulette);
                bloque(true);
                yield return new SEffet { Effet = Effet.SortBloque, Joueur = j.Id };
                yield return new SAnnonce("L'amulette de " + j.Nom + " absorbe le maléfice et se brise.", j.Id, Ton.Bon);
            }
            else bloque(false);
        }
    }

    public sealed class Des
    {
        readonly Random r;
        public Des(int graine) { r = new Random(graine); }
        public int D6() { return r.Next(1, 7); }
        public int D3() { return r.Next(1, 4); }
        /// Entier entre a et b inclus.
        public int Entre(int a, int b) { return r.Next(a, b + 1); }
        public double Reel() { return r.NextDouble(); }
        public T Choisir<T>(IList<T> l) { return l[r.Next(l.Count)]; }
    }
}

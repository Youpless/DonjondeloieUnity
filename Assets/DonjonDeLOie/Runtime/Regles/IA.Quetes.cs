// Donjon de l'Oie — les bots et les quêtes : laquelle prendre, et comment la mener (embuscade tendue exprès, défi lancé
// à l'adversaire désigné, détour par sa case). Au mini-jeu de l'évadé, un bot réussit un peu plus d'une fois sur deux.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class IA
    {
        /// Chance qu'un bot repère l'évadé dans la foule.
        public const double ChanceEvade = 0.55;

        int Quetes(Partie p, Joueur j, SQuestion q)
        {
            if (j.Quete != null && !QueteBloquee(p, j)) return Index(q, "non");
            int best = Index(q, "non"); double bv = 0;
            for (int i = 0; i < q.Options.Count; i++)
            {
                var qu = q.Quetes[i];
                if (qu == null) continue;
                double v = ValeurQuete(p, j, qu) * (0.7 + 0.6 * r.NextDouble());     // un peu de goût personnel
                if (v > bv) { bv = v; best = i; }
            }
            return best;
        }

        /// Une quête qu'on ne mènera pas à bien : une proie trop forte, un adversaire désigné hors de portée.
        static bool QueteBloquee(Partie p, Joueur j)
        {
            var qu = j.Quete;
            switch (qu.Type)
            {
                case Quete.Genre.Chasser:
                {
                    var m = Donnees.Monstre(qu.MonstreCible);
                    return m == null || ChanceMonstre(j.Attaque + BonusMonstre(j), m.Force) < 0.3;
                }
                case Quete.Genre.Defier:
                {
                    if (qu.JoueurCible < 0 || qu.JoueurCible >= p.Joueurs.Count) return true;
                    var o = p.Joueurs[qu.JoueurCible];
                    int d = p.Plateau.DistanceEnAvant(j.Case, o.Case, 12);
                    return ChanceDuel(j.Attaque, o.Attaque) < 0.25 || d < 0;      // trop fort, ou plus devant soi
                }
            }
            return false;
        }

        static double ValeurQuete(Partie p, Joueur j, Quete qu)
        {
            double base_ = qu.RecompenseXP + qu.RecompenseOr / 10.0;
            int atk = j.Attaque + BonusMonstre(j);
            switch (qu.Type)
            {
                case Quete.Genre.Chasser:
                {
                    var m = Donnees.Monstre(qu.MonstreCible);
                    double c = m == null ? 0 : ChanceMonstre(atk, m.Force);
                    return c < 0.4 ? 0 : (qu.RecompenseXP * 1.2 + qu.RecompenseOr / 10.0) * c / qu.Nombre;
                }
                case Quete.Genre.Vaincre:
                {
                    double c = ChanceMonstre(atk, 4);
                    return c < 0.45 ? 0.3 : base_ * c;
                }
                case Quete.Genre.Embuscade:
                    return base_ * 0.85;
                case Quete.Genre.Defier:
                {
                    if (qu.JoueurCible < 0 || qu.JoueurCible >= p.Joueurs.Count) return 0;
                    var o = p.Joueurs[qu.JoueurCible];
                    double c = ChanceDuel(j.Attaque, o.Attaque);
                    bool aPortee = p.Plateau.DistanceEnAvant(j.Case, o.Case, 10) >= 0;       // devant soi, rattrapable
                    return c < 0.4 ? 0 : base_ * (0.4 + c) * (aPortee ? 1.0 : 0.3);
                }
                case Quete.Genre.Evade:
                {
                    int d = qu.CaseCible == j.Case ? 0 : p.Plateau.DistanceEnAvant(j.Case, qu.CaseCible);
                    double loin = d < 0 ? 0.3 : d <= 12 ? 1.0 : 0.6;
                    return base_ * (0.3 + ChanceEvade) * loin;
                }
                default:
                {
                    int d = p.Plateau.DistanceEnAvant(j.Case, qu.CaseCible);
                    return base_ * (d >= 0 && d <= 12 ? 1.0 : 0.7);
                }
            }
        }

        /// Quête du guet-apens : tendre l'embuscade dès qu'un aventurier arrive (gagner le duel n'est pas exigé).
        bool EmbuscadePourQuete(Partie p, Joueur j)
        {
            if (j.Quete == null || j.Quete.Type != Quete.Genre.Embuscade) return false;
            foreach (var o in Poursuivants(p, j, 6))
            {
                if (o.ProtegeDuel) continue;
                if (ChanceDuel(j.Attaque + 2, o.Attaque) >= 0.35 && r.NextDouble() < 0.7) return true;
            }
            return false;
        }

        /// Quête de défi : provoquer l'adversaire désigné s'il est là et que le duel n'est pas perdu d'avance.
        static int DefiPourQuete(Partie p, Joueur j, SQuestion q)
        {
            if (j.Quete == null || j.Quete.Type != Quete.Genre.Defier) return -1;
            for (int i = 0; i < q.Options.Count; i++)
                if (q.Cibles[i] == j.Quete.JoueurCible && q.Actives[i] && ChanceDuel(j.Attaque, p.Joueurs[q.Cibles[i]].Attaque) >= 0.3) return i;
            return -1;
        }

        /// Une case où se tient l'adversaire d'une quête de défi vaut le détour.
        static double BonusCaseQuete(Partie p, Joueur j, int c)
        {
            if (j.Quete == null || j.Quete.Type != Quete.Genre.Defier) return 0;
            foreach (var o in p.JoueursSur(c, j)) if (o.Id == j.Quete.JoueurCible && !o.ProtegeDuel) return 4;
            return 0;
        }
    }
}

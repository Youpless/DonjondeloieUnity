// Donjon de l'Oie — les bots : où poser une embuscade ou un piège, quelle option prendre devant un événement, qui viser.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class IA
    {
        int ChoixCase(Partie p, Joueur j, SQuestion q)
        {
            switch (q.Contexte)
            {
                case "embuscade":
                    // Couvrir la case par où arrive la proie.
                    for (int i = 0; i < q.Cases.Count; i++)
                        if (q.Cases[i] >= 0 && p.Plateau[j.Case].Precedents.Contains(q.Cases[i])) return i;
                    break;
                case "piege":
                {
                    // Devant soi, là où les suiveurs passeront ; sinon sur sa case.
                    int surMoi = -1;
                    for (int i = 0; i < q.Cases.Count; i++)
                    {
                        if (q.Cases[i] < 0) continue;
                        if (q.Cases[i] == j.Case) surMoi = i;
                        else if (p.Plateau[j.Case].Suivants.Contains(q.Cases[i])) return i;
                    }
                    if (surMoi >= 0) return surMoi;
                    break;
                }
                case "rappel":
                    for (int i = 0; i < q.Cases.Count; i++) if (q.Cases[i] == p.Plateau.Depart) return i;
                    break;
            }
            for (int i = 0; i < q.Cases.Count; i++) if (q.Cases[i] >= 0) return i;
            return q.Options.Count - 1;
        }

        /// Un choix d'événement : la somme de ce que vaut chaque effet pour ce joueur, pondérée par sa chance.
        int ChoixEvenement(Partie p, Joueur j, SQuestion q)
        {
            int best = -1; double bv = double.MinValue;
            for (int i = 0; i < q.Options.Count; i++)
            {
                if (!q.Actives[i]) continue;
                double v = r.NextDouble() * 0.3;
                if (i < q.EffetsChoix.Count && q.EffetsChoix[i] != null)
                    foreach (var e in q.EffetsChoix[i]) v += ValeurEffet(p, j, e) * Math.Max(0, Math.Min(100, e.Chance)) / 100.0;
                if (v > bv) { bv = v; best = i; }
            }
            return best;
        }

        static double ValeurEffet(Partie p, Joueur j, EffetEvenement e)
        {
            double pourMoi;
            double vie = j.PV / (double)Math.Max(1, j.PVMax);
            switch (e.Type)
            {
                case TypeEffet.Or: pourMoi = e.Valeur / 8.0; break;
                case TypeEffet.PV: pourMoi = e.Valeur * (e.Valeur > 0 ? (1.2 - vie) : (j.PV + e.Valeur <= 0 ? 4 : 0.8)) * 0.6; break;
                case TypeEffet.SoinTotal: pourMoi = (j.PVMax - j.PV) * 0.4; break;
                case TypeEffet.XP: pourMoi = e.Valeur * 2.0; break;
                case TypeEffet.Niveau: pourMoi = e.Valeur * 4.5; break;
                case TypeEffet.Avancer: pourMoi = 0.6; break;
                case TypeEffet.Reculer: pourMoi = -0.6 * Math.Max(1, e.Valeur) / 2; break;
                case TypeEffet.Combat:
                {
                    var m = Donnees.Monstre(e.Parametre ?? "");
                    pourMoi = m == null ? 0.5 : (ChanceMonstre(j.Attaque + BonusMonstre(j), m.Force) - 0.5) * 6;
                    break;
                }
                case TypeEffet.ObjetHasard: case TypeEffet.ObjetDonne: pourMoi = 1.5; break;
                case TypeEffet.Faiblesse: pourMoi = -0.8 * Math.Max(1, e.Valeur); break;
                case TypeEffet.Immobiliser: pourMoi = -2.5 * Math.Max(1, e.Valeur); break;
                case TypeEffet.Lenteur: pourMoi = -1; break;
                case TypeEffet.Benediction: pourMoi = 0.7 * Math.Max(1, e.Valeur); break;
                case TypeEffet.JetMalediction: pourMoi = j.Classe == ClasseHeros.Clerc ? -1.5 : -3; break;
                case TypeEffet.LeverMalediction: pourMoi = (j.ToursMaudit > 0 || j.Lenteur ? 2 : 0.2); break;
                case TypeEffet.Boutique: pourMoi = j.Or >= 25 ? 1 : 0.1; break;
                case TypeEffet.Quetes: pourMoi = j.Quete == null ? 1 : 0.1; break;
                default: pourMoi = 0; break;
            }
            switch (e.Cible)
            {
                case CibleEffet.Autres: return -pourMoi * 0.6;
                case CibleEffet.Tous: return pourMoi * 0.4;
                case CibleEffet.Meneur: return MeneurAdverse(p, j) == null ? pourMoi : -pourMoi * 0.7;
                case CibleEffet.Dernier: return -pourMoi * 0.2;
                default: return pourMoi;
            }
        }

        int ChoixCible(Partie p, Joueur j, SQuestion q)
        {
            int best = -1; double bv = double.MinValue;
            bool foudre = q.Contexte == "sort:foudre";
            for (int i = 0; i < q.Options.Count; i++)
            {
                if (q.Cibles[i] < 0) continue;
                var o = p.Joueurs[q.Cibles[i]];
                double v = o.Niveau * 10 + o.XP;
                if (foudre && o.PV <= Math.Max(1, 5 - o.Defense)) v += 100;
                if (q.Contexte == "sort:malediction" && o.A(EffetObjet.Amulette)) v -= 15;
                if (v > bv) { bv = v; best = i; }
            }
            return best >= 0 ? best : q.Options.Count - 1;
        }
    }
}

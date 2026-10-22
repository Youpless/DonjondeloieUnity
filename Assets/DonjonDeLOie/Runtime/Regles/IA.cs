// Donjon de l'Oie — les bots : ils répondent aux mêmes questions que les joueurs humains.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class IA
    {
        readonly Random r;
        int actionsCeTour, dernierTour = -1, dernierJoueur = -1;

        public IA(int graine) { r = new Random(graine ^ 0x5bd1e995); }

        // ------------------------------------------------------------------ probabilités

        /// Chance que l'attaquant gagne un duel (attaque + D3 chacun, deux relances en cas d'égalité, le défenseur gagne la troisième).
        public static double ChanceDuel(int atkA, int atkB)
        {
            int gagne = 0, egal = 0;
            for (int a = 1; a <= 3; a++)
                for (int b = 1; b <= 3; b++)
                {
                    int d = atkA + a - (atkB + b);
                    if (d > 0) gagne++; else if (d == 0) egal++;
                }
            double pw = gagne / 9.0, pt = egal / 9.0;
            return pw * (1 + pt + pt * pt);
        }

        /// Chance de battre un monstre (attaque + D6 ≥ force + D6).
        public static double ChanceMonstre(int atk, int force)
        {
            int ok = 0;
            for (int a = 1; a <= 6; a++) for (int b = 1; b <= 6; b++) if (atk + a >= force + b) ok++;
            return ok / 36.0;
        }

        static int BonusMonstre(Joueur j) { return (j.Classe == ClasseHeros.Guerrier || j.Classe == ClasseHeros.Mage ? 1 : 0) + j.Benediction; }

        // ------------------------------------------------------------------ réponse

        public int Repondre(Partie p, SQuestion q)
        {
            var j = p.Joueurs[q.Joueur];
            if (dernierTour != p.TourJoueurs || dernierJoueur != j.Id) { dernierTour = p.TourJoueurs; dernierJoueur = j.Id; actionsCeTour = 0; }
            int rep;
            switch (q.Genre)
            {
                case GenreQuestion.Action: rep = Action(p, j, q); break;
                case GenreQuestion.Embranchement: rep = Embranchement(p, j, q); break;
                case GenreQuestion.Rodeur: rep = MeilleureCase(p, j, q); break;
                case GenreQuestion.Defi: rep = Defi(p, j, q); break;
                case GenreQuestion.Monstre: rep = Monstre(j, q); break;
                case GenreQuestion.Boutique: rep = Boutique(p, j, q); break;
                case GenreQuestion.Quetes: rep = Quetes(p, j, q); break;
                case GenreQuestion.ChoixCase: rep = ChoixCase(p, j, q); break;
                case GenreQuestion.ChoixCible: rep = ChoixCible(p, j, q); break;
                case GenreQuestion.ChoixEvenement: rep = ChoixEvenement(p, j, q); break;
                case GenreQuestion.Dragonnoie: rep = Index(q, VeutAffronterDragonnoie(p, j) ? "combat" : "passer"); break;
                case GenreQuestion.Evade: rep = Index(q, r.NextDouble() < ChanceEvade ? "trouve" : "rate"); break;
                default: rep = q.ParDefaut; break;
            }
            if (rep < 0 || rep >= q.Options.Count || !q.Actives[rep]) rep = PremiereActive(q, q.ParDefaut);
            return rep;
        }

        static int PremiereActive(SQuestion q, int prefere)
        {
            if (prefere >= 0 && prefere < q.Options.Count && q.Actives[prefere]) return prefere;
            for (int i = 0; i < q.Options.Count; i++) if (q.Actives[i]) return i;
            return 0;
        }

        static int Index(SQuestion q, string code)
        {
            for (int i = 0; i < q.Codes.Count; i++) if (q.Codes[i] == code && q.Actives[i]) return i;
            return -1;
        }

        /// Adversaires qui arriveront sur ma case ou juste derrière en moins de « portee » pas.
        static List<Joueur> Poursuivants(Partie p, Joueur j, int portee)
        {
            var l = new List<Joueur>();
            foreach (var o in p.Joueurs)
            {
                if (o == j) continue;
                int d = p.Plateau.DistanceEnAvant(o.Case, j.Case, portee);
                if (d >= 1 && d <= portee) l.Add(o);
            }
            return l;
        }

        int Action(Partie p, Joueur j, SQuestion q)
        {
            actionsCeTour++;
            int de = Index(q, "de");
            if (actionsCeTour > 4) return de;

            // Se soigner.
            if (j.PV * 10 <= j.PVMax * 4)
            {
                int i = Index(q, "objet:elixir"); if (i < 0) i = Index(q, "objet:potion");
                if (i >= 0) return i;
            }
            var meneur = MeneurAdverse(p, j);
            // Sorts contre le meneur.
            if (meneur != null)
            {
                int m = Index(q, "objet:malediction");
                if (m >= 0 && meneur.Niveau >= Math.Max(3, j.Niveau)) return m;
                int f = Index(q, "objet:foudre");
                if (f >= 0)
                {
                    foreach (var o in p.Joueurs)
                        if (o != j && o.PV <= Math.Max(1, 5 - o.Defense) && o.Niveau >= j.Niveau - 1) return f;
                    if (meneur.Niveau >= j.Niveau + 2 && r.NextDouble() < 0.5) return f;
                }
                int l = Index(q, "objet:lenteur");
                if (l >= 0 && meneur.Niveau >= j.Niveau + 1 && r.NextDouble() < 0.6) return l;
            }
            // Pièges si quelqu'un suit.
            var suiveurs = Poursuivants(p, j, 8);
            if (suiveurs.Count > 0)
                foreach (var code in new[] { "objet:rune", "objet:loups", "objet:fosse", "objet:filet" })
                {
                    int i = Index(q, code);
                    if (i >= 0 && (code != "objet:rune" || r.NextDouble() < 0.8)) return i;
                }
            // Embuscade : une proie arrive, je suis plus fort qu'elle (ou ma quête l'exige).
            int emb = Index(q, "embuscade");
            if (emb >= 0 && EmbuscadePourQuete(p, j)) return emb;
            if (emb >= 0)
            {
                foreach (var o in Poursuivants(p, j, 6))
                {
                    if (o.ProtegeDuel) continue;
                    double c = ChanceDuel(j.Attaque + 2, o.Attaque);
                    bool vaut = (o.Niveau > j.Niveau && o.Niveau >= 3) || (o == meneur && o.Niveau >= 4);
                    if (vaut && c >= 0.7 && r.NextDouble() < 0.4) return emb;
                }
            }
            if (j.PV * 2 <= j.PVMax && j.CompterSac(TypeObjet.Potion) == 0)
            {
                int rap = Index(q, "objet:rappel");
                if (rap >= 0 && p.Plateau[j.Case].Type != TypeCase.Ville) return rap;
            }
            return de;
        }

        static Joueur MeneurAdverse(Partie p, Joueur j)
        {
            Joueur m = null;
            foreach (var o in p.Joueurs)
                if (o != j && (m == null || o.Niveau > m.Niveau || (o.Niveau == m.Niveau && o.XP > m.XP))) m = o;
            return m;
        }

        int Embranchement(Partie p, Joueur j, SQuestion q)
        {
            int principal = -1, raccourci = -1;
            for (int i = 0; i < q.Cases.Count; i++)
            {
                if (q.Cases[i] < 0) continue;
                if (p.Plateau[q.Cases[i]].Raccourci) raccourci = i; else principal = i;
            }
            if (raccourci < 0) return Math.Max(0, principal);
            var z = p.Plateau[q.Cases[raccourci]].Zone;
            double vie = j.PV / (double)j.PVMax;
            int atk = j.Attaque + BonusMonstre(j);
            bool ok;
            if (z == Zone.Antre) ok = ChanceMonstre(atk, 12) >= 0.5 && vie >= 0.7;
            else if (z == Zone.Cryptes) ok = ChanceMonstre(atk, 6) >= 0.55 && vie >= 0.5;
            else ok = ChanceMonstre(atk, 4) >= 0.55 && vie >= 0.5;
            // Une quête vers un lieu du raccourci l'emporte.
            if (j.Quete != null && j.Quete.CaseCible >= 0 && p.Plateau[j.Quete.CaseCible].Raccourci
                && p.Plateau.DistanceEnAvant(q.Cases[raccourci], j.Quete.CaseCible, 6) >= 0) ok = true;
            return ok ? raccourci : (principal >= 0 ? principal : 0);
        }

        double ValeurCase(Partie p, Joueur j, int c)
        {
            var k = p.Plateau[c];
            double v = 0;
            int atk = j.Attaque + BonusMonstre(j);
            double vie = j.PV / (double)j.PVMax;
            switch (k.Type)
            {
                case TypeCase.Tresor: v = 3; break;
                case TypeCase.Sanctuaire: v = vie < 0.6 ? 4 : 0.5; break;
                case TypeCase.Ville: v = 2 + (vie < 0.6 ? 2 : 0) + (j.Or >= 40 ? 2 : 0); break;
                case TypeCase.Marchand: v = j.Or >= 30 ? 3 : 0; break;
                case TypeCase.Taverne: v = j.Quete == null ? 2 : 0.5; break;
                case TypeCase.Evenement: v = 1; break;
                case TypeCase.Malediction: v = -3; break;
                case TypeCase.Monstre: v = ChanceMonstre(atk, ForceTypique(k.Zone)) >= 0.6 && vie > 0.4 ? 3 : -2; break;
                case TypeCase.Antre: v = ChanceMonstre(atk, 12) >= 0.6 ? 6 : -6; break;
            }
            if (j.Quete != null && j.Quete.CaseCible == c) v += 6;
            v += BonusCaseQuete(p, j, c);
            if (c == p.CaseDragonnoie && p.Dragonnoie != null) v = VeutAffronterDragonnoie(p, j) ? 9 : -4 - j.Or / 10.0;   // s'y arrêter, c'est le combattre
            if (p.PiegeSur(c) != null && p.PiegeSur(c).Proprietaire == j.Id) v -= 0.5;
            foreach (var o in p.JoueursSur(c, j))
                if (!o.ProtegeDuel && ChanceDuel(j.Attaque, o.Attaque) >= 0.65) v += 2;
            return v;
        }

        static int ForceTypique(Zone z)
        {
            switch (z)
            {
                case Zone.Village: return 2;
                case Zone.Bois: return 3;
                case Zone.Marais: return 5;
                case Zone.Cryptes: return 6;
                case Zone.Pics: return 7;
                default: return 10;
            }
        }

        int MeilleureCase(Partie p, Joueur j, SQuestion q)
        {
            int best = q.ParDefaut; double bv = double.MinValue;
            for (int i = 0; i < q.Options.Count; i++)
            {
                if (q.Cases[i] < 0) continue;
                double v = ValeurCase(p, j, q.Cases[i]) + r.NextDouble() * 0.2;
                if (v > bv) { bv = v; best = i; }
            }
            return best;
        }

        int Defi(Partie p, Joueur j, SQuestion q)
        {
            int pourQuete = DefiPourQuete(p, j, q);
            if (pourQuete >= 0) return pourQuete;
            int best = q.ParDefaut; double bv = 0;
            for (int i = 0; i < q.Options.Count; i++)
            {
                if (q.Cibles[i] < 0) continue;
                var o = p.Joueurs[q.Cibles[i]];
                double c = ChanceDuel(j.Attaque, o.Attaque);
                if (c < 0.6) continue;
                double v = c + 0.15 * (o.Niveau - j.Niveau) + (o.Or >= 20 ? 0.1 : 0);
                if (o.Niveau <= 1 && o.XP == 0) v -= 0.3;
                if (v > bv) { bv = v; best = i; }
            }
            return best;
        }

        /// Affronter le Dragonnoie ? Le vaincre gagne la partie, perdre ne coûte que l'or : on tente sa chance d'autant plus
        /// volontiers que la bourse est légère, ou qu'un rival est à un niveau de la victoire.
        static bool VeutAffronterDragonnoie(Partie p, Joueur j)
        {
            var m = p.Dragonnoie;
            if (m == null) return false;
            double c = ChanceMonstre(j.Attaque + BonusMonstre(j), m.Force);
            double seuil = j.Or <= 3 ? 0.06 : j.Or <= 20 ? 0.17 : 0.3;
            foreach (var o in p.Joueurs) if (o != j && o.Niveau >= p.NiveauCible - 1) seuil = Math.Min(seuil, 0.1);
            return c >= seuil;
        }

        int Monstre(Joueur j, SQuestion q)
        {
            var m = q.Monstre;
            int fuir = Index(q, "fuir");
            if (fuir < 0 || m == null) return Index(q, "combat");
            double c = ChanceMonstre(j.Attaque + BonusMonstre(j), m.Force);
            int degats = Math.Max(1, m.Degats - j.Defense);
            bool survit = j.PV > degats;
            bool combat = m.Boss ? (c >= 0.55 && survit) : (c >= 0.45 || (survit && c >= 0.3));
            if (j.Quete != null && j.Quete.MonstreCible == m.Id && survit && c >= 0.3) combat = true;
            return combat ? Index(q, "combat") : fuir;
        }

        int Boutique(Partie p, Joueur j, SQuestion q)
        {
            int partir = Index(q, "partir");
            int best = partir; double bv = 0;
            for (int i = 0; i < q.Options.Count; i++)
            {
                if (!q.Actives[i] || q.Objets[i] == null) continue;
                var o = q.Objets[i]; int prix = q.Prix[i];
                int reste = j.Or - prix;
                double v = 0;
                switch (o.Type)
                {
                    case TypeObjet.Arme: v = 10 + 3 * (o.Bonus - (j.Arme != null ? j.Arme.Bonus : 0)); break;
                    case TypeObjet.Armure: v = 6 + 2 * (o.Bonus - (j.Armure != null ? j.Armure.Bonus : 0)); break;
                    case TypeObjet.Accessoire:
                        if (o.Effet == EffetObjet.Amulette) v = 5; else if (o.Effet == EffetObjet.Bottes) v = 4; else v = 4;
                        if (reste < 15) v -= 4;
                        break;
                    case TypeObjet.Potion:
                        v = j.CompterSac(TypeObjet.Potion) < 2 ? (o.Effet == EffetObjet.Soin ? 5 : 3) : 0;
                        break;
                    case TypeObjet.Piege:
                        v = j.CompterSac(TypeObjet.Piege) < (j.Classe == ClasseHeros.Voleur ? 3 : 1) ? 3 + (j.Classe == ClasseHeros.Voleur ? 2 : 0) : 0;
                        if (o.Effet == EffetObjet.PiegeRune) v += 1;
                        if (reste < 10) v -= 3;
                        break;
                    case TypeObjet.Parchemin:
                        if (o.Effet == EffetObjet.SortRappel) v = 0.5;
                        else v = j.CompterSac(TypeObjet.Parchemin) < (j.Classe == ClasseHeros.Mage ? 3 : 1) ? 3 + (j.Classe == ClasseHeros.Mage ? 2 : 0) : 0;
                        if (o.Effet == EffetObjet.SortMalediction) v += 1.5;
                        if (reste < 10) v -= 3;
                        break;
                }
                // Une arme reste la priorité : on ne dilapide pas l'or si une meilleure arme est presque à portée.
                if (o.Type != TypeObjet.Arme && reste < 25 && ProchaineArme(j) != null && ProchaineArme(j).Prix <= j.Or + 20) v -= 2.5;
                if (v > bv) { bv = v; best = i; }
            }
            return best < 0 ? q.Options.Count - 1 : best;
        }

        static DefObjet ProchaineArme(Joueur j)
        {
            DefObjet best = null;
            foreach (var o in Donnees.Objets)
                if (o.Type == TypeObjet.Arme && (j.Arme == null || o.Bonus > j.Arme.Bonus) && (best == null || o.Prix < best.Prix)) best = o;
            return best;
        }
    }
}

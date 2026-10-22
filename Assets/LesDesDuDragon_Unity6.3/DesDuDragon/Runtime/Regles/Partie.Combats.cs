// Les Dés du Dragon — duels entre joueurs, combats contre les monstres, pièges.
using System;
using System.Collections.Generic;

namespace DesDuDragon.Regles
{
    public sealed partial class Partie
    {
        /// Duel : chacun lance son attaque + 1D3 ; le plus haut gagne. Le perdant perd un niveau (deux si l'écart atteint 5)
        /// et de l'or ; le gagnant gagne 1 XP. En embuscade, l'attaquant a +2.
        IEnumerable<Signal> Duel(Joueur a, Joueur b, bool embuscade)
        {
            SCombat c = null;
            for (int lancer = 1; lancer <= 3; lancer++)
            {
                c = new SCombat { Duel = true, Attaquant = a.Id, Defenseur = b.Id, FacesDe = 3, AtkA = a.Attaque, AtkB = b.Attaque, Embuscade = embuscade };
                if (embuscade) { c.BonusA = 2; c.RaisonBonusA = "Embuscade"; }
                c.DeA = Des.D3(); c.DeB = Des.D3();
                if (c.TotalA == c.TotalB && lancer < 3)
                {
                    c.Egalite = true;
                    yield return c;
                    yield return new SAnnonce("Égalité ! On relance.", a.Id, Ton.Combat);
                    continue;
                }
                c.AttaquantGagne = c.TotalA > c.TotalB;   // troisième égalité : le défenseur tient bon
                yield return c;
                break;
            }
            var gagnant = c.AttaquantGagne ? a : b;
            var perdant = c.AttaquantGagne ? b : a;
            int ecart = Math.Abs(c.TotalA - c.TotalB);
            gagnant.DuelsGagnes++; perdant.DuelsPerdus++;
            yield return new SAnnonce(gagnant.Nom + " remporte le duel contre " + perdant.Nom + " (" + Math.Max(c.TotalA, c.TotalB) + " contre " + Math.Min(c.TotalA, c.TotalB) + ").",
                gagnant.Id, Ton.Combat, true);

            int vol = Math.Min(perdant.Or, 5 * Math.Max(1, ecart));
            if (vol > 0)
            {
                perdant.Or -= vol; gagnant.Or += vol; gagnant.OrGagne += vol;
                yield return new SStat(perdant.Id, Stat.Or, -vol);
                yield return new SStat(gagnant.Id, Stat.Or, vol);
            }
            foreach (var s in PerdreNiveau(perdant, ecart >= 5 ? 2 : 1, "duel perdu contre " + gagnant.Nom)) yield return s;
            perdant.ProtegeDuel = true;
            protectionNeeCeTour.Add(perdant.Id);
            foreach (var s in GagnerXP(gagnant, 1)) yield return s;
        }

        DefMonstre TirerMonstre(Joueur j, Zone z)
        {
            var possibles = new List<DefMonstre>();
            foreach (var m in Donnees.Monstres)
                if (!m.Boss && Array.IndexOf(m.Zones, z) >= 0) possibles.Add(m);
            if (possibles.Count == 0) possibles.Add(Donnees.Monstres[0]);
            // Une quête de chasse attire sa proie, si elle vit dans la région.
            if (j.Quete != null && j.Quete.Type == Quete.Genre.Chasser && Des.Reel() < 0.4)
                foreach (var m in possibles) if (m.Id == j.Quete.MonstreCible) return m;
            return Des.Choisir(possibles);
        }

        IEnumerable<Signal> CombatMonstre(Joueur j, DefMonstre m, bool fuitePossible)
        {
            var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Monstre, Monstre = m, Titre = m.Nom,
                Texte = m.Description + "\nForce " + m.Force + " · dégâts " + m.Degats + " · butin " + m.Or + " or, " + m.XP + " XP" };
            q.Ajouter("Combattre", "combat", "Votre attaque + 1D6 contre sa force + 1D6. L'égalité vous sourit.");
            if (fuitePossible && Plateau[j.Case].Precedents.Count > 0)
                q.Ajouter("Fuir", "fuir", "Reculer d'une case, sans butin.");
            yield return q;
            if (q.CodeChoisi == "fuir")
            {
                int de = j.Case, vers = Plateau[j.Case].Precedents[0];
                j.Case = vers;
                yield return new SPas { Joueur = j.Id, De = de, Vers = vers };
                yield return new SAnnonce(j.Nom + " fuit devant " + m.Nom + ".", j.Id, Ton.Neutre);
                yield break;
            }

            var c = new SCombat { Duel = false, Attaquant = j.Id, Monstre = m, FacesDe = 6, AtkA = j.Attaque, AtkB = m.Force };
            var raisons = new List<string>();
            if (j.Classe == ClasseHeros.Guerrier) { c.BonusA += 1; raisons.Add("Vétéran"); }
            if (j.Classe == ClasseHeros.Mage) { c.BonusA += 1; raisons.Add("Trait de feu"); }
            if (j.Benediction > 0) { c.BonusA += j.Benediction; raisons.Add("Bénédiction"); j.Benediction = 0; }
            c.RaisonBonusA = raisons.Count > 0 ? string.Join(", ", raisons.ToArray()) : null;
            c.DeA = Des.D6(); c.DeB = Des.D6();
            c.AttaquantGagne = c.TotalA >= c.TotalB;
            yield return c;

            if (c.AttaquantGagne)
            {
                j.MonstresTues++;
                yield return new SAnnonce(j.Nom + " terrasse " + (m.Boss ? m.Nom : "un " + m.Nom.ToLowerInvariant()) + " !", j.Id, Ton.Combat, m.Boss);
                foreach (var s in GagnerOr(j, m.Or, null)) yield return s;
                if (j.Quete != null && j.Quete.Type == Quete.Genre.Chasser && j.Quete.MonstreCible == m.Id)
                {
                    j.Quete.Fait++;
                    if (j.Quete.Fait >= j.Quete.Nombre) foreach (var s in FinirQuete(j)) yield return s;
                }
                foreach (var s in GagnerXP(j, m.XP)) yield return s;
            }
            else
            {
                int degats = Math.Max(1, m.Degats - j.Defense);
                yield return new SAnnonce(m.Nom + " repousse " + j.Nom + " : −" + degats + " PV.", j.Id, Ton.Mauvais);
                foreach (var s in Blesser(j, degats, "vaincu par " + m.Nom)) yield return s;
            }
        }

        IEnumerable<Signal> DeclencherPiege(Joueur j, Piege p)
        {
            Pieges.Remove(p);
            var poseur = Joueurs[p.Proprietaire];
            poseur.PiegesDeclenches++;
            yield return new SEffet { Effet = Effet.PiegeDeclenche, Joueur = poseur.Id, Cible = j.Id, Case = p.Case, Objet = p.Objet.Id };
            yield return new SAnnonce(j.Nom + " tombe dans un piège de " + poseur.Nom + " : " + p.Objet.Nom + " !", j.Id, Ton.Mauvais, true);
            switch (p.Objet.Effet)
            {
                case EffetObjet.PiegeLoups:
                    j.ToursImmobilise = Math.Max(j.ToursImmobilise, 1);
                    foreach (var s in Blesser(j, p.Objet.Bonus, "piège à loups")) yield return s;
                    break;
                case EffetObjet.PiegeFosse:
                    foreach (var s in Blesser(j, p.Objet.Bonus, "fosse à pieux")) yield return s;
                    break;
                case EffetObjet.PiegeFilet:
                {
                    int vol = Math.Min(j.Or, p.Objet.Bonus);
                    if (vol > 0)
                    {
                        j.Or -= vol; poseur.Or += vol; poseur.OrGagne += vol;
                        yield return new SStat(j.Id, Stat.Or, -vol);
                        yield return new SStat(poseur.Id, Stat.Or, vol);
                    }
                    break;
                }
                case EffetObjet.PiegeRune:
                {
                    bool bloque = false;
                    foreach (var s in Amulette(j, r => bloque = r)) yield return s;
                    if (!bloque) foreach (var s in PerdreNiveau(j, 1, "rune d'oubli")) yield return s;
                    break;
                }
            }
        }
    }
}

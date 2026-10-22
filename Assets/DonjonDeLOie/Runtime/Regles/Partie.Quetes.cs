// Donjon de l'Oie — les quêtes : une seule à la fois. Atteindre un lieu, chasser un monstre précis, vaincre des
// ennemis, tendre une embuscade qui prend un joueur, défier un joueur précis, retrouver un évadé caché dans la ville
// (un mini-jeu : la question « Evade », à laquelle répond l'interface après la partie de cache-cache, ou un bot).
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class Partie
    {
        static readonly string[] NomsEvades =
        {
            "Gaspard le Borgne", "Babette la Fouine", "Mortimer Deux-Doigts", "Lucien Main-Leste", "Ernestine la Rousse",
            "Oscar Sans-Sou", "Firmin la Belette", "Hortense Pied-Léger", "Anatole le Balafré", "Clémence la Taupe",
        };
        static readonly bool[] EvadeesFeminins = { false, true, false, false, true, false, false, true, false, true };

        /// Quatre quêtes au plus : un lieu, un combat (une chasse ou des ennemis à vaincre), et deux parmi l'embuscade,
        /// le défi et l'évadé.
        List<Quete> GenererQuetes(Joueur j)
        {
            var l = new List<Quete>();
            var lieu = QueteLieu(j);
            if (lieu != null) l.Add(lieu);
            var combat = Des.Reel() < 0.5 ? QueteChasse(j) : null;
            l.Add(combat ?? QueteVaincre(j));
            var autres = new List<Quete> { QueteEmbuscade() };
            var defi = QueteDefi(j); if (defi != null) autres.Add(defi);
            var evade = QueteEvade(j); if (evade != null) autres.Add(evade);
            for (int k = 0; k < 2 && autres.Count > 0; k++)
            {
                var q = Des.Choisir(autres);
                autres.Remove(q);
                l.Add(q);
            }
            return l;
        }

        /// Un lieu nommé à une distance raisonnable devant soi.
        Quete QueteLieu(Joueur j)
        {
            var lieux = new List<int>();
            for (int i = 0; i < Plateau.Nombre; i++)
            {
                if (Plateau[i].Nom == null || i == j.Case) continue;
                int d = Plateau.DistanceEnAvant(j.Case, i);
                if (d >= 5 && d <= 24) lieux.Add(i);
            }
            if (lieux.Count == 0) return null;
            int c = Des.Choisir(lieux);
            int dist = Plateau.DistanceEnAvant(j.Case, c);
            return new Quete
            {
                Type = Quete.Genre.Atteindre, CaseCible = c,
                RecompenseXP = 2 + dist / 8, RecompenseOr = 5 + dist,
                Titre = "Message pour " + Plateau[c].Nom,
                Texte = "Portez un pli scellé jusqu'à « " + Plateau[c].Nom + " » (passer dessus suffit)."
            };
        }

        /// Une chasse à la mesure du héros (null s'il n'y a pas de proie).
        Quete QueteChasse(Joueur j)
        {
            var proies = new List<DefMonstre>();
            foreach (var m in Donnees.Monstres)
                if (Chassable(m) && m.Force <= j.Attaque + 5 && m.Force >= j.Attaque - 3) proies.Add(m);
            if (proies.Count == 0) foreach (var m in Donnees.Monstres) if (Chassable(m)) { proies.Add(m); break; }
            if (proies.Count == 0) return null;
            var proie = Des.Choisir(proies);
            int n = proie.Force <= 4 ? 2 : 1;
            string ou = Donnees.NomsZones[(int)proie.Zones[0]];
            return new Quete
            {
                Type = Quete.Genre.Chasser, MonstreCible = proie.Id, Nombre = n,
                RecompenseXP = 2 + (proie.Force * n) / 4, RecompenseOr = proie.Force * 2 * n,
                Titre = "Chasse : " + (n > 1 ? n + " × " : "") + proie.Nom,
                Texte = "Abattez " + (n > 1 ? n + " " + proie.Nom.ToLowerInvariant() + "s" : "un " + proie.Nom.ToLowerInvariant()) + ". On en voit vers " + ou + "."
            };
        }

        /// Un monstre qu'une quête de chasse peut viser : ni le boss, ni le Dragonnoie, et il vit quelque part.
        static bool Chassable(DefMonstre m) { return !m.Boss && !m.Errant && m.Zones != null && m.Zones.Length > 0; }

        Quete QueteVaincre(Joueur j)
        {
            int n = j.Niveau >= 5 ? 3 : 2;
            return new Quete
            {
                Type = Quete.Genre.Vaincre, Nombre = n, RecompenseXP = 2 + n, RecompenseOr = 6 * n,
                Titre = "Vaincre " + n + " ennemis",
                Texte = "Terrassez " + n + " monstres, n'importe lesquels, où que ce soit."
            };
        }

        Quete QueteEmbuscade()
        {
            return new Quete
            {
                Type = Quete.Genre.Embuscade, RecompenseXP = 3, RecompenseOr = 15,
                Titre = "Le guet-apens",
                Texte = "Tendez une embuscade et surprenez un aventurier qui s'y engage. Gagner le duel n'est pas exigé."
            };
        }

        /// Défier un autre aventurier précis (null s'il est seul).
        Quete QueteDefi(Joueur j)
        {
            var autres = Joueurs.FindAll(o => o != j);
            if (autres.Count == 0) return null;
            var cible = Des.Choisir(autres);
            return new Quete
            {
                Type = Quete.Genre.Defier, JoueurCible = cible.Id, RecompenseXP = 4, RecompenseOr = 10 + 2 * cible.Niveau,
                Titre = "Défier " + cible.Nom,
                Texte = "Provoquez " + cible.Nom + " en duel (rejoignez sa case). Gagner ou perdre, l'honneur est sauf."
            };
        }

        /// Retrouver un évadé caché au marché de la ville (null si la carte n'a pas de ville).
        Quete QueteEvade(Joueur j)
        {
            int ville = VillePourEvade(j);
            if (ville < 0) return null;
            int k = Des.Entre(0, NomsEvades.Length - 1);
            string nom = NomsEvades[k];
            bool f = EvadeesFeminins[k];
            return new Quete
            {
                Type = Quete.Genre.Evade, CaseCible = ville, Ville = Plateau[ville].Libelle, Graine = Des.Entre(1, 1000000), NomEvade = nom, Feminin = f,
                RecompenseXP = 4, RecompenseOr = 25,
                Titre = "Avis de recherche : " + nom,
                Texte = nom + (f ? " s'est évadée" : " s'est évadé") + " et se cache au marché de " + Plateau[ville].Libelle
                    + ". Allez-y : vous aurez " + Donnees.Reglages.DureeEvade + " secondes pour " + (f ? "la" : "le") + " repérer dans la foule."
            };
        }

        /// La ville la plus proche devant soi (celle où l'on se trouve compte).
        int VillePourEvade(Joueur j)
        {
            int best = -1, bd = int.MaxValue;
            for (int i = 0; i < Plateau.Nombre; i++)
            {
                if (Plateau[i].Type != TypeCase.Ville) continue;
                int d = i == j.Case ? 0 : Plateau.DistanceEnAvant(j.Case, i);
                if (d < 0) d = 1000 + i;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        IEnumerable<Signal> ProposerQuetes(Joueur j, string lieu)
        {
            var offre = GenererQuetes(j);
            var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Quetes, Titre = lieu,
                Texte = j.Quete == null ? "Choisissez une quête (une seule à la fois)." : "Vous avez déjà : « " + j.Quete.Titre + " ». En prendre une autre l'abandonne." };
            foreach (var qu in offre)
                q.Ajouter(qu.Titre, "quete", qu.Texte + "\nRécompense : " + qu.RecompenseXP + " XP, " + qu.RecompenseOr + " or",
                    true, qu.CaseCible, -1, null, 0, qu);
            q.Ajouter(j.Quete == null ? "Aucune" : "Garder ma quête", "non");
            q.ParDefaut = q.Options.Count - 1;
            yield return q;
            if (q.CodeChoisi != "quete") yield break;
            j.Quete = q.Quetes[q.Reponse];
            yield return new SEffet { Effet = Effet.QueteAcceptee, Joueur = j.Id, Case = j.Quete.CaseCible };
            yield return new SAnnonce(j.Nom + " accepte : " + j.Quete.Titre + ".", j.Id, Ton.Quete);
            foreach (var s in ChasseEvade(j)) yield return s;          // déjà en ville : la chasse commence
        }

        IEnumerable<Signal> VerifierQueteLieu(Joueur j)
        {
            if (j.Quete == null || j.Quete.Type != Quete.Genre.Atteindre || j.Quete.CaseCible != j.Case) yield break;
            j.Quete.Fait = 1;
            foreach (var s in FinirQuete(j)) yield return s;
        }

        /// Un pas de plus vers une quête du genre donné (pour un défi : seulement contre son adversaire désigné).
        IEnumerable<Signal> ProgresQuete(Joueur j, Quete.Genre genre, int cible = -1)
        {
            var q = j.Quete;
            if (q == null || q.Type != genre || Fini) yield break;
            if (genre == Quete.Genre.Defier && q.JoueurCible != cible) yield break;
            q.Fait++;
            if (q.Fait >= q.Nombre) foreach (var s in FinirQuete(j)) yield return s;
        }

        /// La chasse à l'évadé : elle se joue dès que le héros est dans la ville de l'avis avec cette quête (en y passant,
        /// en s'y arrêtant, en y arrivant par un parchemin de rappel, au début de son tour s'il s'y trouve, ou aussitôt la
        /// quête acceptée sur place). Une autre ville ne compte pas : l'évadé n'y est pas.
        IEnumerable<Signal> ChasseEvade(Joueur j)
        {
            var qu = j.Quete;
            if (qu == null || qu.Type != Quete.Genre.Evade || Fini || j.Case != qu.CaseCible) yield break;
            yield return new SAnnonce(j.Nom + " fouille le marché de " + Plateau[j.Case].Libelle + " à la recherche de " + qu.NomEvade + "…", j.Id, Ton.Quete);
            var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Evade, Quete = qu, Titre = "Avis de recherche : " + qu.NomEvade,
                Texte = "Retrouvez " + qu.NomEvade + " dans la foule du marché : " + Donnees.Reglages.DureeEvade + " secondes pour cliquer sur "
                    + (qu.Feminin ? "elle." : "lui.") };
            q.Ajouter(qu.Feminin ? "Je l'ai trouvée" : "Je l'ai trouvé", "trouve");
            q.Ajouter(qu.Feminin ? "Elle m'a échappé" : "Il m'a échappé", "rate");
            q.ParDefaut = 1;
            yield return q;
            if (j.Quete != qu) yield break;
            if (q.CodeChoisi == "trouve")
            {
                qu.Fait = 1;
                yield return new SAnnonce(j.Nom + " démasque " + qu.NomEvade + " dans la foule !", j.Id, Ton.Quete, true);
                foreach (var s in FinirQuete(j)) yield return s;
            }
            else
            {
                j.Quete = null;
                yield return new SEffet { Effet = Effet.QueteRatee, Joueur = j.Id, Texte = qu.Titre };
                yield return new SAnnonce(qu.NomEvade + " se fond dans la foule et disparaît : la quête est perdue.", j.Id, Ton.Mauvais, true);
            }
        }

        IEnumerable<Signal> FinirQuete(Joueur j)
        {
            var q = j.Quete;
            j.Quete = null;
            j.QuetesFinies++;
            yield return new SEffet { Effet = Effet.QueteFinie, Joueur = j.Id, Texte = q.Titre };
            yield return new SAnnonce("Quête accomplie : " + q.Titre + " !", j.Id, Ton.Quete, true);
            foreach (var s in GagnerOr(j, q.RecompenseOr, null)) yield return s;
            foreach (var s in GagnerXP(j, q.RecompenseXP)) yield return s;
        }
    }
}

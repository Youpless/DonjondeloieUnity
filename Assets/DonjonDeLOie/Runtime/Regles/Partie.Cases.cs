// Donjon de l'Oie — ce qui arrive sur chaque type de case : trésors, événements, malédictions, boutiques, quêtes.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class Partie
    {
        IEnumerable<Signal> EffetCase(Joueur j)
        {
            var c = Plateau[j.Case];
            bool aSaListe = c.Evenements != null && c.Evenements.Count > 0;
            // Mode « Remplacer » : la case joue un de ses événements au lieu de l'effet de son type.
            if (c.Mode == ModeEvenements.Remplacer && aSaListe)
            {
                foreach (var s in JouerEvenement(j, TirerEvenement(c.Evenements, c.Zone))) yield return s;
                yield break;
            }
            foreach (var s in EffetType(j, c)) yield return s;
            // Mode « Ajouter » : un de ses événements après l'effet du type (si le joueur n'a pas été mis K.O. ou déplacé).
            if (c.Mode == ModeEvenements.Ajouter && aSaListe && !Arret && j.Case == c.Id && c.Type != TypeCase.Evenement)
                foreach (var s in JouerEvenement(j, TirerEvenement(c.Evenements, c.Zone))) yield return s;
        }

        IEnumerable<Signal> EffetType(Joueur j, Case c)
        {
            switch (c.Type)
            {
                case TypeCase.Ville:
                {
                    string ville = c.Libelle;
                    foreach (var s in Soigner(j, j.PVMax, "Soins de " + ville)) yield return s;
                    if (j.ToursMaudit > 0 || j.Lenteur) { j.ToursMaudit = 0; j.Lenteur = false; yield return new SAnnonce("Les prêtres de " + ville + " lèvent les malédictions de " + j.Nom + ".", j.Id, Ton.Bon); }
                    foreach (var s in ProposerQuetes(j, "Le panneau des quêtes de " + ville)) yield return s;
                    if (Arret) break;               // l'évadé retrouvé sur-le-champ a pu donner le niveau de la victoire
                    foreach (var s in Boutique(j, OffreVille(), "Bazar de " + ville, 1f)) yield return s;
                }
                    break;
                case TypeCase.Chemin:
                    yield return new SAnnonce(j.Nom + " fait halte : " + c.Libelle + ".", j.Id);
                    break;
                case TypeCase.Monstre:
                    foreach (var s in CombatMonstre(j, TirerMonstre(j, c.Zone), true)) yield return s;
                    break;
                case TypeCase.Antre:
                {
                    var boss = Donnees.Boss;
                    if (boss == null) break;
                    yield return new SAnnonce(boss.Nom + " s'éveille sur son tas d'or…", j.Id, Ton.Combat, true);
                    foreach (var s in CombatMonstre(j, boss, true)) yield return s;
                    break;
                }
                case TypeCase.Tresor:
                    foreach (var s in Tresor(j, c)) yield return s;
                    break;
                case TypeCase.Evenement:
                {
                    // la liste propre à la case, sinon la pioche commune
                    var ev = c.Evenements != null && c.Evenements.Count > 0 ? TirerEvenement(c.Evenements, c.Zone) : TirerEvenementPioche(c.Zone);
                    foreach (var s in JouerEvenement(j, ev)) yield return s;
                    break;
                }
                case TypeCase.Malediction:
                    foreach (var s in Malediction(j)) yield return s;
                    break;
                case TypeCase.Sanctuaire:
                    yield return new SAnnonce(j.Nom + " se recueille : " + c.Libelle + ".", j.Id, Ton.Bon);
                    foreach (var s in Soigner(j, j.PVMax, "Sanctuaire")) yield return s;
                    if (j.ToursMaudit > 0 || j.Lenteur) { j.ToursMaudit = 0; j.Lenteur = false; yield return new SAnnonce("Les malédictions de " + j.Nom + " sont levées.", j.Id, Ton.Bon); }
                    break;
                case TypeCase.Marchand:
                    foreach (var s in Boutique(j, OffreMarchand(c.Id), c.Libelle, 1f)) yield return s;
                    break;
                case TypeCase.Taverne:
                    foreach (var s in ProposerQuetes(j, c.Libelle)) yield return s;
                    if (Arret) break;
                    foreach (var s in Boutique(j, OffreTaverne(), c.Libelle + " — le comptoir", 1f)) yield return s;
                    break;
            }
        }

        // ------------------------------------------------------------------ trésors

        IEnumerable<Signal> Tresor(Joueur j, Case c)
        {
            int or = 5 + 3 * Des.D6();
            if (c.Zone == Zone.Antre) or += 25;
            else if (c.Raccourci) or += 10;
            if (j.Classe == ClasseHeros.Voleur) or += 5;
            yield return new SEffet { Effet = Effet.Tresor, Joueur = j.Id, Case = c.Id };
            foreach (var s in GagnerOr(j, or, "trésor")) yield return s;
            if (Des.Reel() < (c.Raccourci ? 0.5 : 0.25) && j.Sac.Count < Donnees.TailleSac)
            {
                var pool = new List<DefObjet>();
                foreach (var o in Donnees.Objets) if (o.Consommable) pool.Add(o);
                var trouve = Des.Choisir(pool);
                j.Sac.Add(trouve);
                yield return new SEffet { Effet = Effet.Achat, Joueur = j.Id, Objet = trouve.Id };
                yield return new SAnnonce("Au fond du coffre : " + trouve.Nom + " !", j.Id, Ton.Bon);
            }
        }

        // ------------------------------------------------------------------ malédictions

        IEnumerable<Signal> Malediction(Joueur j)
        {
            yield return new SAnnonce("Une ombre maudite fond sur " + j.Nom + "…", j.Id, Ton.Mauvais, true);
            bool bloque = false;
            foreach (var s in Amulette(j, r => bloque = r)) yield return s;
            if (bloque) yield break;
            int bonus = j.Classe == ClasseHeros.Clerc ? 2 : 0;
            int de = Des.D6();
            yield return new SDe { Joueur = j.Id, Faces = 6, Valeur = de, Bonus = bonus, Raison = "Résister à la malédiction" };
            yield return new SEffet { Effet = Effet.Malediction, Joueur = j.Id };
            switch (Math.Min(6, de + bonus))
            {
                case 1:
                    foreach (var s in PerdreNiveau(j, 1, "malédiction de l'oubli")) yield return s; break;
                case 2:
                    yield return new SAnnonce("Malédiction d'avarice : l'or de " + j.Nom + " fond.", j.Id, Ton.Mauvais);
                    foreach (var s in PerdreOr(j, 15, null)) yield return s; break;
                case 3:
                    j.ToursMaudit = Math.Max(j.ToursMaudit, 3);
                    yield return new SAnnonce("Malédiction de faiblesse : −2 en attaque pendant 3 tours.", j.Id, Ton.Mauvais); break;
                case 4:
                    j.ToursImmobilise = Math.Max(j.ToursImmobilise, 1);
                    yield return new SAnnonce("Pétrification : " + j.Nom + " passera son prochain tour.", j.Id, Ton.Mauvais); break;
                case 5:
                    yield return new SAnnonce("Brûlure noire : −3 PV.", j.Id, Ton.Mauvais);
                    foreach (var s in Blesser(j, 3, "brûlure noire")) yield return s; break;
                default:
                    yield return new SAnnonce(j.Nom + " résiste à la malédiction !", j.Id, Ton.Bon); break;
            }
        }

        // ------------------------------------------------------------------ boutiques

        List<DefObjet> OffreVille()
        {
            return new List<DefObjet>(Donnees.Objets);
        }

        List<DefObjet> OffreTaverne()
        {
            var l = new List<DefObjet>();
            foreach (var id in new[] { "potion", "elixir", "lenteur", "foudre" }) l.Add(Donnees.Objet(id));
            return l;
        }

        /// Un marchand change d'étal tous les trois tours.
        List<DefObjet> OffreMarchand(int caseId)
        {
            var r = new Random(caseId * 7919 + (Tour / 3) * 104729);
            var pool = new List<DefObjet>();
            foreach (var o in Donnees.Objets) if (!o.SeulementEnVille) pool.Add(o);
            var l = new List<DefObjet>();
            // Toujours au moins une arme et une armure.
            var armes = pool.FindAll(o => o.Type == TypeObjet.Arme);
            var armures = pool.FindAll(o => o.Type == TypeObjet.Armure);
            l.Add(armes[r.Next(armes.Count)]);
            l.Add(armures[r.Next(armures.Count)]);
            while (l.Count < 7)
            {
                var o = pool[r.Next(pool.Count)];
                if (!l.Contains(o)) l.Add(o);
            }
            l.Sort((x, y) => x.Type != y.Type ? x.Type.CompareTo(y.Type) : x.Prix.CompareTo(y.Prix));
            return l;
        }

        List<DefObjet> OffreColporteur()
        {
            var pool = new List<DefObjet>();
            foreach (var o in Donnees.Objets) if (!o.SeulementEnVille) pool.Add(o);
            var l = new List<DefObjet>();
            while (l.Count < 5)
            {
                var o = Des.Choisir(pool);
                if (!l.Contains(o)) l.Add(o);
            }
            l.Sort((x, y) => x.Type != y.Type ? x.Type.CompareTo(y.Type) : x.Prix.CompareTo(y.Prix));
            return l;
        }

        public string RaisonRefus(Joueur j, DefObjet o, int prix)
        {
            if (j.Or < prix) return "Pas assez d'or";
            switch (o.Type)
            {
                case TypeObjet.Arme: if (j.Arme != null && j.Arme.Bonus >= o.Bonus) return "Votre arme est meilleure"; break;
                case TypeObjet.Armure: if (j.Armure != null && j.Armure.Bonus >= o.Bonus) return "Votre armure est meilleure"; break;
                case TypeObjet.Accessoire:
                    if (j.A(o.Effet)) return "Déjà porté";
                    if (j.Accessoires.Count >= Donnees.AccessoiresMax) return "Deux accessoires au plus";
                    break;
                default: if (j.Sac.Count >= Donnees.TailleSac) return "Sac plein (" + Donnees.TailleSac + " objets)"; break;
            }
            return null;
        }

        public int PrixEnBoutique(Joueur j, DefObjet o, float remise)
        {
            return Math.Max(1, (int)Math.Round(j.PrixPour(o) * remise));
        }

        IEnumerable<Signal> Boutique(Joueur j, List<DefObjet> offre, string nom, float remise)
        {
            while (true)
            {
                var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Boutique, Titre = nom,
                    Texte = "Vous avez " + j.Or + " or." };
                foreach (var o in offre)
                {
                    int prix = PrixEnBoutique(j, o, remise);
                    string refus = RaisonRefus(j, o, prix);
                    q.Ajouter(o.Nom, "achat", refus ?? o.Description, refus == null, -1, -1, o, prix);
                }
                q.Ajouter("Partir", "partir");
                q.ParDefaut = q.Options.Count - 1;
                yield return q;
                if (q.CodeChoisi != "achat") yield break;
                var choisi = q.Objets[q.Reponse];
                int p = q.Prix[q.Reponse];
                if (RaisonRefus(j, choisi, p) != null) continue;
                j.Or -= p;
                yield return new SStat(j.Id, Stat.Or, -p);
                switch (choisi.Type)
                {
                    case TypeObjet.Arme: j.Arme = choisi; break;
                    case TypeObjet.Armure: j.Armure = choisi; break;
                    case TypeObjet.Accessoire:
                        j.Accessoires.Add(choisi);
                        if (choisi.Effet == EffetObjet.Anneau) { j.PV += choisi.Bonus; yield return new SStat(j.Id, Stat.PV, choisi.Bonus); }
                        break;
                    default: j.Sac.Add(choisi); break;
                }
                yield return new SEffet { Effet = Effet.Achat, Joueur = j.Id, Objet = choisi.Id };
                yield return new SAnnonce(j.Nom + " achète : " + choisi.Nom + " (" + p + " or).", j.Id, Ton.Neutre);
            }
        }
    }
}

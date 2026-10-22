// Les Dés du Dragon — ce qui arrive sur chaque type de case : trésors, événements, malédictions, boutiques, quêtes.
using System;
using System.Collections.Generic;

namespace DesDuDragon.Regles
{
    public sealed partial class Partie
    {
        IEnumerable<Signal> EffetCase(Joueur j)
        {
            var c = Plateau[j.Case];
            switch (c.Type)
            {
                case TypeCase.Ville:
                    foreach (var s in Soigner(j, j.PVMax, "Soins de Valcreux")) yield return s;
                    if (j.ToursMaudit > 0 || j.Lenteur) { j.ToursMaudit = 0; j.Lenteur = false; yield return new SAnnonce("Les prêtres de Valcreux lèvent les malédictions de " + j.Nom + ".", j.Id, Ton.Bon); }
                    foreach (var s in ProposerQuetes(j, "Le panneau des quêtes de Valcreux")) yield return s;
                    foreach (var s in Boutique(j, OffreVille(), "Bazar de Valcreux", 1f)) yield return s;
                    break;
                case TypeCase.Chemin:
                    yield return new SAnnonce(j.Nom + " fait halte : " + c.Libelle + ".", j.Id);
                    break;
                case TypeCase.Monstre:
                    foreach (var s in CombatMonstre(j, TirerMonstre(j, c.Zone), true)) yield return s;
                    break;
                case TypeCase.Antre:
                    yield return new SAnnonce("Ignathor s'éveille sur son tas d'or…", j.Id, Ton.Combat, true);
                    foreach (var s in CombatMonstre(j, Donnees.Monstre("dragon"), true)) yield return s;
                    break;
                case TypeCase.Tresor:
                    foreach (var s in Tresor(j, c)) yield return s;
                    break;
                case TypeCase.Evenement:
                    foreach (var s in Evenement(j)) yield return s;
                    break;
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

        // ------------------------------------------------------------------ événements

        public static readonly string[] Evenements =
        {
            "Fontaine bénie", "Bourse oubliée", "Tire-laine", "Barde errant", "Orage arcanique", "Vent favorable",
            "Éboulement", "Chevalier errant", "Colporteur", "Pièce maudite", "Vieil ermite", "Portail instable",
            "Festin des fées", "Coffre piégé", "Parchemin perdu", "Embuscade gobeline"
        };

        IEnumerable<Signal> Evenement(Joueur j)
        {
            int e = Des.Entre(0, Evenements.Length - 1);
            yield return new SEffet { Effet = Effet.Evenement, Joueur = j.Id, Texte = Evenements[e] };
            switch (e)
            {
                case 0:
                    yield return new SAnnonce("Fontaine bénie : l'eau claire referme les plaies de " + j.Nom + ".", j.Id, Ton.Bon, true);
                    foreach (var s in Soigner(j, 5, null)) yield return s; break;
                case 1:
                    yield return new SAnnonce("Bourse oubliée au bord du chemin !", j.Id, Ton.Bon, true);
                    foreach (var s in GagnerOr(j, 12, "bourse")) yield return s; break;
                case 2:
                    yield return new SAnnonce("Un tire-laine bouscule " + j.Nom + " et file avec sa bourse.", j.Id, Ton.Mauvais, true);
                    foreach (var s in PerdreOr(j, 10, "tire-laine")) yield return s; break;
                case 3:
                    yield return new SAnnonce("Un barde chante les exploits de " + j.Nom + " : +1 XP.", j.Id, Ton.Bon, true);
                    foreach (var s in GagnerXP(j, 1)) yield return s; break;
                case 4:
                    yield return new SAnnonce("Orage arcanique ! La foudre frappe tous les aventuriers (−2 PV).", j.Id, Ton.Mauvais, true);
                    foreach (var o in new List<Joueur>(Joueurs))
                    {
                        foreach (var s in Blesser(o, 2, "orage arcanique")) yield return s;
                        if (Fini) yield break;
                    }
                    break;
                case 5:
                    yield return new SAnnonce("Vent favorable : " + j.Nom + " avance de 3 cases.", j.Id, Ton.Bon, true);
                    foreach (var s in Marcher(j, 3, true)) yield return s; break;
                case 6:
                {
                    yield return new SAnnonce("Éboulement ! " + j.Nom + " recule de 2 cases (−1 PV).", j.Id, Ton.Mauvais, true);
                    for (int i = 0; i < 2 && Plateau[j.Case].Precedents.Count > 0; i++)
                    {
                        int de = j.Case, vers = Plateau[j.Case].Precedents[0];
                        j.Case = vers;
                        yield return new SPas { Joueur = j.Id, De = de, Vers = vers };
                    }
                    foreach (var s in Blesser(j, 1, "éboulement")) yield return s;
                    break;
                }
                case 7:
                    j.Benediction += 2;
                    yield return new SAnnonce("Un chevalier errant bénit l'arme de " + j.Nom + " : +2 à son prochain combat contre un monstre.", j.Id, Ton.Bon, true);
                    break;
                case 8:
                    yield return new SAnnonce("Un colporteur ouvre sa carriole : tout à −30 % !", j.Id, Ton.Bon, true);
                    foreach (var s in Boutique(j, OffreColporteur(), "Le colporteur", 0.7f)) yield return s; break;
                case 9:
                    yield return new SAnnonce("Une pièce d'or brille dans la boue… maudite ! +20 or, mais −2 en attaque pendant 3 tours.", j.Id, Ton.Mauvais, true);
                    j.ToursMaudit = Math.Max(j.ToursMaudit, 3);
                    yield return new SEffet { Effet = Effet.Malediction, Joueur = j.Id };
                    foreach (var s in GagnerOr(j, 20, null)) yield return s; break;
                case 10:
                    yield return new SAnnonce("Un vieil ermite a des missions à confier.", j.Id, Ton.Quete, true);
                    foreach (var s in ProposerQuetes(j, "Le vieil ermite")) yield return s; break;
                case 11:
                {
                    var cibles = new List<int>();
                    for (int i = 0; i < Plateau.Nombre; i++)
                        if (!Plateau[i].Raccourci && Plateau[i].Type != TypeCase.Ville && i != j.Case) cibles.Add(i);
                    int vers = Des.Choisir(cibles);
                    yield return new SAnnonce("Un portail instable aspire " + j.Nom + "…", j.Id, Ton.Neutre, true);
                    j.Case = vers;
                    yield return new STeleport { Joueur = j.Id, Vers = vers, Raison = "Portail" };
                    foreach (var s in VerifierQueteLieu(j)) yield return s;
                    foreach (var s in Arrivee(j)) yield return s;
                    break;
                }
                case 12:
                    yield return new SAnnonce("Festin des fées : " + j.Nom + " retrouve toutes ses forces.", j.Id, Ton.Bon, true);
                    foreach (var s in Soigner(j, j.PVMax, null)) yield return s; break;
                case 13:
                    yield return new SAnnonce("Coffre piégé ! Une aiguille empoisonnée (−3 PV), mais 15 or à l'intérieur.", j.Id, Ton.Neutre, true);
                    foreach (var s in GagnerOr(j, 15, null)) yield return s;
                    foreach (var s in Blesser(j, 3, "coffre piégé")) yield return s;
                    break;
                case 14:
                {
                    var pool = new List<DefObjet>();
                    foreach (var o in Donnees.Objets) if (o.Type == TypeObjet.Parchemin) pool.Add(o);
                    var p = Des.Choisir(pool);
                    if (j.Sac.Count < Donnees.TailleSac)
                    {
                        j.Sac.Add(p);
                        yield return new SEffet { Effet = Effet.Achat, Joueur = j.Id, Objet = p.Id };
                        yield return new SAnnonce(j.Nom + " trouve un " + p.Nom.ToLowerInvariant() + " !", j.Id, Ton.Bon, true);
                    }
                    else
                    {
                        yield return new SAnnonce(j.Nom + " trouve un parchemin, mais son sac est plein. Il le revend 10 or.", j.Id, Ton.Neutre, true);
                        foreach (var s in GagnerOr(j, 10, null)) yield return s;
                    }
                    break;
                }
                default:
                    yield return new SAnnonce("Des gobelins surgissent des fourrés !", j.Id, Ton.Combat, true);
                    foreach (var s in CombatMonstre(j, Donnees.Monstre("gobelin"), true)) yield return s;
                    break;
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

        // ------------------------------------------------------------------ quêtes

        List<Quete> GenererQuetes(Joueur j)
        {
            var l = new List<Quete>();
            // Deux quêtes de lieu, à une distance raisonnable devant soi.
            var lieux = new List<int>();
            for (int i = 0; i < Plateau.Nombre; i++)
            {
                if (Plateau[i].Nom == null || i == j.Case) continue;
                int d = Plateau.DistanceEnAvant(j.Case, i);
                if (d >= 5 && d <= 24) lieux.Add(i);
            }
            for (int k = 0; k < 2 && lieux.Count > 0; k++)
            {
                int c = lieux[Des.Entre(0, lieux.Count - 1)];
                lieux.Remove(c);
                int d = Plateau.DistanceEnAvant(j.Case, c);
                l.Add(new Quete
                {
                    Type = Quete.Genre.Atteindre, CaseCible = c,
                    RecompenseXP = 2 + d / 8, RecompenseOr = 5 + d,
                    Titre = "Message pour " + Plateau[c].Nom,
                    Texte = "Portez un pli scellé jusqu'à « " + Plateau[c].Nom + " » (passer dessus suffit)."
                });
            }
            // Une quête de chasse à la mesure du héros.
            var proies = new List<DefMonstre>();
            foreach (var m in Donnees.Monstres)
                if (!m.Boss && m.Force <= j.Attaque + 5 && m.Force >= j.Attaque - 3) proies.Add(m);
            if (proies.Count == 0) proies.Add(Donnees.Monstres[0]);
            var proie = Des.Choisir(proies);
            int n = proie.Force <= 4 ? 2 : 1;
            string ou = Donnees.NomsZones[(int)proie.Zones[0]];
            l.Add(new Quete
            {
                Type = Quete.Genre.Chasser, MonstreCible = proie.Id, Nombre = n,
                RecompenseXP = 2 + (proie.Force * n) / 4, RecompenseOr = proie.Force * 2 * n,
                Titre = "Chasse : " + (n > 1 ? n + " × " : "") + proie.Nom,
                Texte = "Abattez " + (n > 1 ? n + " " + proie.Nom.ToLowerInvariant() + "s" : "un " + proie.Nom.ToLowerInvariant()) + ". On en voit vers " + ou + "."
            });
            return l;
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
        }

        IEnumerable<Signal> VerifierQueteLieu(Joueur j)
        {
            if (j.Quete == null || j.Quete.Type != Quete.Genre.Atteindre || j.Quete.CaseCible != j.Case) yield break;
            j.Quete.Fait = 1;
            foreach (var s in FinirQuete(j)) yield return s;
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

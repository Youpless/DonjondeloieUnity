// Les Dés du Dragon — utiliser les objets du sac : potions, pièges, parchemins.
using System;
using System.Collections.Generic;

namespace DesDuDragon.Regles
{
    public sealed partial class Partie
    {
        public List<int> CasesPourPiege(Joueur j)
        {
            var l = new List<int>();
            var candidates = new List<int> { j.Case };
            foreach (int v in Plateau.Voisins(j.Case)) if (!candidates.Contains(v)) candidates.Add(v);
            foreach (int c in candidates)
            {
                var t = Plateau[c].Type;
                if (t == TypeCase.Ville || t == TypeCase.Sanctuaire || t == TypeCase.Antre) continue;
                if (PiegeSur(c) != null) continue;
                l.Add(c);
            }
            return l;
        }

        public int PiegesPosesPar(Joueur j)
        {
            int n = 0;
            foreach (var p in Pieges) if (p.Proprietaire == j.Id) n++;
            return n;
        }

        public bool ObjetUtilisable(Joueur j, DefObjet o)
        {
            switch (o.Type)
            {
                case TypeObjet.Potion: return j.PV < j.PVMax;
                case TypeObjet.Piege: return CasesPourPiege(j).Count > 0 && PiegesPosesPar(j) < Donnees.PiegesMaxParJoueur;
                case TypeObjet.Parchemin: return o.Effet == EffetObjet.SortRappel || Joueurs.Count > 1;
            }
            return false;
        }

        IEnumerable<Signal> UtiliserObjet(Joueur j, DefObjet o)
        {
            if (o == null || !j.Sac.Contains(o) || !ObjetUtilisable(j, o)) yield break;
            switch (o.Type)
            {
                case TypeObjet.Potion:
                    j.Sac.Remove(o);
                    yield return new SAnnonce(j.Nom + " boit : " + o.Nom + ".", j.Id, Ton.Bon);
                    foreach (var s in Soigner(j, o.Effet == EffetObjet.SoinTotal ? j.PVMax : o.Bonus, o.Nom)) yield return s;
                    break;

                case TypeObjet.Piege:
                {
                    var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.ChoixCase, Contexte = "piege", Titre = o.Nom,
                        Texte = "Où poser le piège ? (votre case ou une case voisine — vous seul le verrez)" };
                    foreach (int c in CasesPourPiege(j))
                        q.Ajouter(c == j.Case ? "Ma case : " + Plateau[c].Libelle : Plateau[c].Libelle, "case", null, true, c);
                    q.Ajouter("Annuler", "annuler");
                    yield return q;
                    if (q.CodeChoisi != "case") yield break;
                    int cible = q.Cases[q.Reponse];
                    j.Sac.Remove(o);
                    Pieges.Add(new Piege { Proprietaire = j.Id, Objet = o, Case = cible });
                    yield return new SEffet { Effet = Effet.PiegePose, Joueur = j.Id, Case = cible, Objet = o.Id };
                    yield return new SAnnonce(j.Nom + " dissimule quelque chose dans les herbes…", j.Id, Ton.Neutre);
                    break;
                }

                case TypeObjet.Parchemin:
                    if (o.Effet == EffetObjet.SortRappel)
                    {
                        var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.ChoixCase, Contexte = "rappel", Titre = o.Nom, Texte = "Où voulez-vous apparaître ?" };
                        q.Ajouter(Plateau[Plateau.Depart].Libelle, "case", "Valcreux (soins, boutique et quêtes à votre prochain arrêt).", true, Plateau.Depart);
                        foreach (int c in Plateau.CasesDeType(TypeCase.Sanctuaire)) if (c != j.Case) q.Ajouter(Plateau[c].Libelle, "case", null, true, c);
                        q.Ajouter("Annuler", "annuler");
                        yield return q;
                        if (q.CodeChoisi != "case") yield break;
                        j.Sac.Remove(o);
                        j.Case = q.Cases[q.Reponse];
                        j.Embuscade = null;
                        yield return new SEffet { Effet = Effet.SortLance, Joueur = j.Id, Cible = j.Id, Objet = o.Id };
                        yield return new STeleport { Joueur = j.Id, Vers = j.Case, Raison = "Rappel" };
                        foreach (var s in VerifierQueteLieu(j)) yield return s;
                        break;
                    }
                    else
                    {
                        var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.ChoixCible, Contexte = "sort:" + o.Id, Titre = o.Nom, Texte = o.Description + "\nSur qui ?" };
                        foreach (var cible in Joueurs)
                            if (cible != j)
                                q.Ajouter(cible.Nom + " — niveau " + cible.Niveau, "cible", cible.Def.Nom + " · " + cible.PV + "/" + cible.PVMax + " PV", true, cible.Case, cible.Id);
                        q.Ajouter("Annuler", "annuler");
                        yield return q;
                        if (q.CodeChoisi != "cible") yield break;
                        var t = Joueurs[q.Cibles[q.Reponse]];
                        j.Sac.Remove(o);
                        foreach (var s in Sort(j, t, o)) yield return s;
                    }
                    break;
            }
        }

        IEnumerable<Signal> Sort(Joueur lanceur, Joueur cible, DefObjet o)
        {
            yield return new SEffet { Effet = Effet.SortLance, Joueur = lanceur.Id, Cible = cible.Id, Objet = o.Id };
            yield return new SAnnonce(lanceur.Nom + " lit un " + o.Nom.ToLowerInvariant() + " contre " + cible.Nom + " !", lanceur.Id, Ton.Combat, true);
            bool bloque = false;
            foreach (var s in Amulette(cible, r => bloque = r)) yield return s;
            if (bloque) yield break;
            switch (o.Effet)
            {
                case EffetObjet.SortMalediction:
                {
                    // Résister : 5+ au dé ; 6 contre un mage ; le clerc résiste mieux.
                    int seuil = 5;
                    if (lanceur.Classe == ClasseHeros.Mage) seuil++;
                    if (cible.Classe == ClasseHeros.Clerc) seuil--;
                    int de = Des.D6();
                    yield return new SDe { Joueur = cible.Id, Faces = 6, Valeur = de, Raison = "Résister (" + seuil + "+)" };
                    if (de >= seuil) { yield return new SAnnonce(cible.Nom + " résiste à la malédiction !", cible.Id, Ton.Bon); yield break; }
                    yield return new SEffet { Effet = Effet.Malediction, Joueur = cible.Id };
                    foreach (var s in PerdreNiveau(cible, 1, "malédiction de " + lanceur.Nom)) yield return s;
                    break;
                }
                case EffetObjet.SortFoudre:
                {
                    int degats = Math.Max(1, o.Bonus + (lanceur.Classe == ClasseHeros.Mage ? 1 : 0) - cible.Defense);
                    yield return new SAnnonce("La foudre frappe " + cible.Nom + " : −" + degats + " PV.", cible.Id, Ton.Mauvais);
                    foreach (var s in Blesser(cible, degats, "foudre de " + lanceur.Nom)) yield return s;
                    break;
                }
                case EffetObjet.SortLenteur:
                    cible.Lenteur = true;
                    yield return new SAnnonce(cible.Nom + " est ralenti : son prochain déplacement sera divisé par deux.", cible.Id, Ton.Mauvais);
                    break;
            }
        }
    }
}

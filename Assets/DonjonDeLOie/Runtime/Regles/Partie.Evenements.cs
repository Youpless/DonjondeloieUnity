// Donjon de l'Oie — jouer un événement décrit en données : annonce, effets (avec chance et cible), choix éventuel.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    public sealed partial class Partie
    {
        /// Un événement de la liste, tiré au hasard selon les poids (ceux de la zone d'abord, s'il y en a).
        DefEvenement TirerEvenement(IList<string> ids, Zone zone)
        {
            var l = new List<DefEvenement>();
            foreach (var id in ids) { var e = Donnees.Evenement(id); if (e != null && e.PossibleEn(zone)) l.Add(e); }
            if (l.Count == 0) foreach (var id in ids) { var e = Donnees.Evenement(id); if (e != null) l.Add(e); }
            return Tirer(l);
        }

        /// Un événement de la pioche commune, possible dans cette zone.
        DefEvenement TirerEvenementPioche(Zone zone)
        {
            var l = new List<DefEvenement>();
            foreach (var e in Donnees.Evenements) if (e.DansLaPioche && e.PossibleEn(zone)) l.Add(e);
            return Tirer(l);
        }

        DefEvenement Tirer(List<DefEvenement> l)
        {
            if (l.Count == 0) return null;
            int tot = 0;
            foreach (var e in l) tot += Math.Max(1, e.Poids);
            int v = Des.Entre(1, tot);
            foreach (var e in l) { v -= Math.Max(1, e.Poids); if (v <= 0) return e; }
            return l[l.Count - 1];
        }

        static string Texte(string t, Joueur j) { return (t ?? "").Replace("{joueur}", j.Nom); }

        public IEnumerable<Signal> JouerEvenement(Joueur j, DefEvenement ev)
        {
            if (ev == null)
            {
                yield return new SAnnonce(j.Nom + " ne croise personne.", j.Id);
                yield break;
            }
            yield return new SEffet { Effet = Effet.Evenement, Joueur = j.Id, Texte = ev.Nom };
            if (!string.IsNullOrEmpty(ev.Texte)) yield return new SAnnonce(Texte(ev.Texte, j), j.Id, ev.Ton, true);
            if (ev.Effets != null)
                foreach (var ef in ev.Effets)
                {
                    foreach (var s in Appliquer(j, ef, ev.Nom)) yield return s;
                    if (Arret) yield break;
                }
            if (ev.Choix == null || ev.Choix.Count == 0) yield break;
            var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.ChoixEvenement, Titre = ev.Nom, Texte = Texte(ev.Texte, j) };
            for (int i = 0; i < ev.Choix.Count; i++)
            {
                q.Ajouter(ev.Choix[i].Libelle, "choix", ev.Choix[i].Aide, PeutChoisir(j, ev.Choix[i]));
                q.EffetsChoix.Add(ev.Choix[i].Effets);
            }
            if (!q.Actives.Contains(true)) yield break;
            yield return q;
            int r = q.Reponse;
            if (r < 0 || r >= ev.Choix.Count || !q.Actives[r]) r = q.Actives.IndexOf(true);
            var ch = ev.Choix[r];
            yield return new SAnnonce(j.Nom + " : " + ch.Libelle.ToLowerInvariant() + ".", j.Id, ev.Ton);
            foreach (var ef in ch.Effets)
            {
                foreach (var s in Appliquer(j, ef, ev.Nom)) yield return s;
                if (Arret) yield break;
            }
        }

        /// Un choix qui coûte de l'or n'est possible que si l'on a cet or.
        static bool PeutChoisir(Joueur j, ChoixEvenement c)
        {
            int cout = 0;
            foreach (var e in c.Effets) if (e.Type == TypeEffet.Or && e.Valeur < 0 && e.Chance >= 100 && e.Cible == CibleEffet.Joueur) cout -= e.Valeur;
            return j.Or >= cout;
        }

        List<Joueur> Cibles(Joueur j, CibleEffet c)
        {
            var l = new List<Joueur>();
            switch (c)
            {
                case CibleEffet.Tous: l.AddRange(Joueurs); break;
                case CibleEffet.Autres: foreach (var o in Joueurs) if (o != j) l.Add(o); break;
                case CibleEffet.Meneur:
                {
                    Joueur m = null;
                    foreach (var o in Joueurs) if (m == null || o.Niveau > m.Niveau || (o.Niveau == m.Niveau && o.XP > m.XP)) m = o;
                    l.Add(m); break;
                }
                case CibleEffet.Dernier:
                {
                    Joueur m = null;
                    foreach (var o in Joueurs) if (m == null || o.Niveau < m.Niveau || (o.Niveau == m.Niveau && o.XP < m.XP)) m = o;
                    l.Add(m); break;
                }
                default: l.Add(j); break;
            }
            return l;
        }

        IEnumerable<Signal> Appliquer(Joueur j, EffetEvenement ef, string raison)
        {
            if (ef == null) yield break;
            if (ef.Chance < 100 && Des.Entre(1, 100) > Math.Max(0, ef.Chance)) yield break;
            if (EffetEvenement.SeulementLeJoueur(ef.Type))
            {
                foreach (var s in AppliquerA(j, j, ef, raison)) yield return s;
                yield break;
            }
            foreach (var cible in Cibles(j, ef.Cible))
            {
                foreach (var s in AppliquerA(j, cible, ef, raison)) yield return s;
                if (Fini) yield break;
            }
        }

        IEnumerable<Signal> AppliquerA(Joueur j, Joueur t, EffetEvenement ef, string raison)
        {
            string r = raison != null ? raison.ToLowerInvariant() : null;
            switch (ef.Type)
            {
                case TypeEffet.Annonce:
                    yield return new SAnnonce(Texte(ef.Parametre, j), j.Id, Ton.Neutre);
                    break;
                case TypeEffet.Or:
                    if (ef.Valeur >= 0) foreach (var s in GagnerOr(t, ef.Valeur, r)) yield return s;
                    else foreach (var s in PerdreOr(t, -ef.Valeur, r)) yield return s;
                    break;
                case TypeEffet.PV:
                    if (ef.Valeur >= 0) foreach (var s in Soigner(t, ef.Valeur, raison)) yield return s;
                    else foreach (var s in Blesser(t, -ef.Valeur, r)) yield return s;
                    break;
                case TypeEffet.SoinTotal:
                    foreach (var s in Soigner(t, t.PVMax, raison)) yield return s;
                    break;
                case TypeEffet.XP:
                    if (ef.Valeur >= 0) foreach (var s in GagnerXP(t, ef.Valeur)) yield return s;
                    else
                    {
                        int x = Math.Min(t.XP, -ef.Valeur);
                        if (x > 0) { t.XP -= x; yield return new SStat(t.Id, Stat.XP, -x); }
                    }
                    break;
                case TypeEffet.Niveau:
                    if (ef.Valeur > 0) foreach (var s in GagnerXP(t, ef.Valeur * Donnees.XPParNiveau)) yield return s;
                    else if (ef.Valeur < 0)
                    {
                        bool bloque = false;
                        foreach (var s in Amulette(t, b => bloque = b)) yield return s;
                        if (!bloque) foreach (var s in PerdreNiveau(t, -ef.Valeur, r)) yield return s;
                    }
                    break;
                case TypeEffet.Avancer:
                    foreach (var s in Marcher(j, Math.Max(1, ef.Valeur), true)) yield return s;
                    break;
                case TypeEffet.Reculer:
                    for (int i = 0; i < Math.Max(1, ef.Valeur) && Plateau[j.Case].Precedents.Count > 0; i++)
                    {
                        int de = j.Case, vers = Plateau[j.Case].Precedents[0];
                        j.Case = vers;
                        yield return new SPas { Joueur = j.Id, De = de, Vers = vers };
                    }
                    foreach (var s in ArretDragonnoie(j, null)) yield return s;
                    break;
                case TypeEffet.TeleporterHasard:
                {
                    var cases = new List<int>();
                    foreach (var c in Plateau.Cases) if (!c.Raccourci && c.Type != TypeCase.Ville && c.Id != j.Case) cases.Add(c.Id);
                    if (cases.Count == 0) break;
                    foreach (var s in Teleporter(j, Des.Choisir(cases), ef.Valeur != 0, "Portail")) yield return s;
                    break;
                }
                case TypeEffet.TeleporterDepart:
                    foreach (var s in Teleporter(t, Plateau.Depart, false, "Rappel")) yield return s;
                    break;
                case TypeEffet.TeleporterCase:
                {
                    int vers = -1;
                    foreach (var c in Plateau.Cases) if (c.Nom != null && string.Equals(c.Nom, ef.Parametre, StringComparison.OrdinalIgnoreCase)) vers = c.Id;
                    if (vers < 0) { yield return new SAnnonce("(lieu introuvable : « " + ef.Parametre + " »)", j.Id, Ton.Systeme); break; }
                    foreach (var s in Teleporter(j, vers, ef.Valeur != 0, raison)) yield return s;
                    break;
                }
                case TypeEffet.Combat:
                {
                    var m = string.IsNullOrEmpty(ef.Parametre) ? TirerMonstre(j, Plateau[j.Case].Zone) : Donnees.Monstre(ef.Parametre.Trim());
                    if (m == null || m.Errant) m = TirerMonstre(j, Plateau[j.Case].Zone);     // le Dragonnoie ne se combat que sur sa case
                    foreach (var s in CombatMonstre(j, m, true)) yield return s;
                    break;
                }
                case TypeEffet.Boutique:
                {
                    var offre = new List<DefObjet>();
                    if (!string.IsNullOrEmpty(ef.Parametre))
                        foreach (var id in ef.Parametre.Split(','))
                        {
                            var o = Donnees.Objet(id.Trim());
                            if (o != null && !offre.Contains(o)) offre.Add(o);
                        }
                    if (offre.Count == 0) offre = OffreColporteur();
                    float remise = Math.Max(0f, 1f - Math.Max(0, Math.Min(100, ef.Valeur)) / 100f);
                    foreach (var s in Boutique(j, offre, raison ?? "Le colporteur", remise)) yield return s;
                    break;
                }
                case TypeEffet.Quetes:
                    foreach (var s in ProposerQuetes(j, string.IsNullOrEmpty(ef.Parametre) ? (raison ?? "Un voyageur") : ef.Parametre)) yield return s;
                    break;
                case TypeEffet.ObjetHasard:
                case TypeEffet.ObjetDonne:
                {
                    DefObjet o = null;
                    if (ef.Type == TypeEffet.ObjetDonne) o = Donnees.Objet((ef.Parametre ?? "").Trim());
                    else
                    {
                        var pool = new List<DefObjet>();
                        TypeObjet voulu;
                        bool filtre = Enum.TryParse(ef.Parametre ?? "", true, out voulu) && (ef.Parametre ?? "").Trim().Length > 0;
                        foreach (var x in Donnees.Objets) if (filtre ? x.Type == voulu : x.Consommable) pool.Add(x);
                        if (pool.Count > 0) o = Des.Choisir(pool);
                    }
                    if (o == null) break;
                    bool place = o.Consommable ? t.Sac.Count < Donnees.TailleSac : Donnees_PeutEquiper(t, o);
                    if (place)
                    {
                        Equiper(t, o);
                        yield return new SEffet { Effet = Effet.Achat, Joueur = t.Id, Objet = o.Id };
                        yield return new SAnnonce(t.Nom + " obtient : " + o.Nom + " !", t.Id, Ton.Bon);
                    }
                    else if (ef.Valeur > 0)
                    {
                        yield return new SAnnonce(t.Nom + " n'a pas de place pour " + o.Nom + " et le revend.", t.Id, Ton.Neutre);
                        foreach (var s in GagnerOr(t, ef.Valeur, null)) yield return s;
                    }
                    break;
                }
                case TypeEffet.Faiblesse:
                    t.ToursMaudit = Math.Max(t.ToursMaudit, Math.Max(1, ef.Valeur));
                    yield return new SEffet { Effet = Effet.Malediction, Joueur = t.Id };
                    break;
                case TypeEffet.Immobiliser:
                    t.ToursImmobilise = Math.Max(t.ToursImmobilise, Math.Max(1, ef.Valeur));
                    yield return new SAnnonce(t.Nom + " passera " + (t.ToursImmobilise > 1 ? t.ToursImmobilise + " tours." : "son prochain tour."), t.Id, Ton.Mauvais);
                    break;
                case TypeEffet.Lenteur:
                    t.Lenteur = true;
                    yield return new SAnnonce(t.Nom + " est ralenti.", t.Id, Ton.Mauvais);
                    break;
                case TypeEffet.Benediction:
                    t.Benediction += Math.Max(1, ef.Valeur);
                    yield return new SAnnonce(t.Nom + " est béni : +" + t.Benediction + " à son prochain combat.", t.Id, Ton.Bon);
                    break;
                case TypeEffet.JetMalediction:
                    foreach (var s in Malediction(t)) yield return s;
                    break;
                case TypeEffet.LeverMalediction:
                    if (t.ToursMaudit > 0 || t.Lenteur || t.ToursImmobilise > 0)
                    {
                        t.ToursMaudit = 0; t.Lenteur = false; t.ToursImmobilise = 0;
                        yield return new SAnnonce("Les malédictions de " + t.Nom + " sont levées.", t.Id, Ton.Bon);
                    }
                    break;
            }
        }

        IEnumerable<Signal> Teleporter(Joueur j, int vers, bool effetArrivee, string raison)
        {
            j.Case = vers;
            j.Embuscade = null;
            yield return new STeleport { Joueur = j.Id, Vers = vers, Raison = raison };
            foreach (var s in VerifierQueteLieu(j)) yield return s;
            bool oie = false;
            foreach (var s in ArretDragonnoie(j, r => oie = r)) yield return s;
            if (oie) yield break;                       // le Dragonnoie occupe la case : pas d'effet d'arrivée
            if (effetArrivee && !Arret && j == JoueurCourant) foreach (var s in Arrivee(j)) yield return s;
        }

        static bool Donnees_PeutEquiper(Joueur j, DefObjet o)
        {
            switch (o.Type)
            {
                case TypeObjet.Arme: return j.Arme == null || j.Arme.Bonus < o.Bonus;
                case TypeObjet.Armure: return j.Armure == null || j.Armure.Bonus < o.Bonus;
                case TypeObjet.Accessoire: return !j.A(o.Effet) && j.Accessoires.Count < Donnees.AccessoiresMax;
            }
            return j.Sac.Count < Donnees.TailleSac;
        }

        static void Equiper(Joueur j, DefObjet o)
        {
            switch (o.Type)
            {
                case TypeObjet.Arme: j.Arme = o; break;
                case TypeObjet.Armure: j.Armure = o; break;
                case TypeObjet.Accessoire:
                    j.Accessoires.Add(o);
                    if (o.Effet == EffetObjet.Anneau) j.PV += o.Bonus;
                    break;
                default: j.Sac.Add(o); break;
            }
        }
    }
}

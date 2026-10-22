// Les Dés du Dragon — la partie : l'état et le déroulé d'un tour.
using System;
using System.Collections.Generic;

namespace DesDuDragon.Regles
{
    public sealed class ReglageJoueur
    {
        public string Nom; public ClasseHeros Classe; public bool Humain; public int Couleur;
        public ReglageJoueur(string nom, ClasseHeros c, bool humain, int couleur) { Nom = nom; Classe = c; Humain = humain; Couleur = couleur; }
    }

    public sealed partial class Partie
    {
        public readonly Plateau Plateau;
        public readonly List<Joueur> Joueurs = new List<Joueur>();
        public readonly List<Piege> Pieges = new List<Piege>();
        public readonly Des Des;
        public int NiveauCible = 10;
        public int Courant;              // index dans Joueurs
        public int Tour = 1;             // un tour = chaque joueur a joué
        public int TourJoueurs;          // nombre total de tours individuels joués
        public bool Fini;
        public int Gagnant = -1;

        bool tourFini;                   // le joueur courant ne peut plus rien faire ce tour (K.O., piège…)
        bool Arret { get { return Fini || tourFini; } }

        public Partie(IList<ReglageJoueur> reglages, int graine, int niveauCible = 10, Plateau plateau = null)
        {
            Plateau = plateau ?? Plateau.Standard();
            Des = new Des(graine);
            NiveauCible = Math.Max(2, niveauCible);
            // L'ordre de jeu est tiré au sort.
            var ordre = new List<ReglageJoueur>(reglages);
            for (int i = ordre.Count - 1; i > 0; i--) { int k = Des.Entre(0, i); var t = ordre[i]; ordre[i] = ordre[k]; ordre[k] = t; }
            for (int i = 0; i < ordre.Count; i++)
            {
                var r = ordre[i];
                var j = new Joueur { Id = i, Nom = r.Nom, Classe = r.Classe, Humain = r.Humain, Couleur = r.Couleur, Or = Donnees.OrDepart, Case = Plateau.Depart };
                j.PV = j.PVMax;
                if (j.Classe == ClasseHeros.Mage) j.Sac.Add(Donnees.Objet("foudre"));
                if (j.Classe == ClasseHeros.Voleur) j.Sac.Add(Donnees.Objet("fosse"));
                j.Sac.Add(Donnees.Objet("potion"));
                Joueurs.Add(j);
            }
        }

        public Joueur JoueurCourant { get { return Joueurs[Courant]; } }

        public Joueur Meneur
        {
            get
            {
                Joueur m = null;
                foreach (var j in Joueurs)
                    if (m == null || j.Niveau > m.Niveau || (j.Niveau == m.Niveau && j.XP > m.XP)) m = j;
                return m;
            }
        }

        public List<Joueur> JoueursSur(int caseId, Joueur sauf = null)
        {
            var l = new List<Joueur>();
            foreach (var j in Joueurs) if (j.Case == caseId && j != sauf) l.Add(j);
            return l;
        }

        public Piege PiegeSur(int caseId)
        {
            for (int i = 0; i < Pieges.Count; i++) if (Pieges[i].Case == caseId) return Pieges[i];
            return null;
        }

        public Joueur EmbusqueSur(int caseId, Joueur sauf)
        {
            foreach (var j in Joueurs) if (j != sauf && j.Embuscade != null && j.Embuscade.Couvre(caseId)) return j;
            return null;
        }

        public IEnumerable<Signal> Debut()
        {
            var noms = new List<string>();
            foreach (var j in Joueurs) noms.Add(j.Nom);
            yield return new SAnnonce("L'ordre de jeu est tiré : " + string.Join(", ", noms.ToArray()) + ".", -1, Ton.Systeme);
            yield return new SAnnonce("Premier au niveau " + NiveauCible + " : la victoire !", -1, Ton.Systeme, true);
        }

        // ================================================================== un tour

        public IEnumerable<Signal> JouerTour()
        {
            if (Fini) yield break;
            tourFini = false;
            var j = JoueurCourant;
            j.Embuscade = null;  // une embuscade dure jusqu'au tour suivant de celui qui l'a tendue
            yield return new STourDebut { Joueur = j.Id, Tour = Tour };

            if (j.Classe == ClasseHeros.Clerc && j.PV < j.PVMax)
                foreach (var s in Soigner(j, 2, "Foi")) yield return s;

            if (j.ToursImmobilise > 0)
            {
                j.ToursImmobilise--;
                yield return new SAnnonce(j.Nom + " est immobilisé et passe son tour.", j.Id, Ton.Mauvais);
            }
            else
            {
                foreach (var s in PhaseAction(j)) yield return s;
            }
            if (Fini) yield break;
            FinDeTour(j);
        }

        void FinDeTour(Joueur j)
        {
            if (j.ToursMaudit > 0) j.ToursMaudit--;
            if (j.ProtegeDuel && !protectionNeeCeTour.Contains(j.Id)) j.ProtegeDuel = false;
            protectionNeeCeTour.Clear();
            TourJoueurs++;
            Courant = (Courant + 1) % Joueurs.Count;
            if (Courant == 0) Tour++;
        }

        readonly HashSet<int> protectionNeeCeTour = new HashSet<int>();

        IEnumerable<Signal> PhaseAction(Joueur j)
        {
            while (true)
            {
                var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Action, Titre = "À vous, " + j.Nom,
                    Texte = "Lancez le dé, tendez une embuscade ou utilisez un objet." };
                q.Ajouter("Lancer le dé", "de", "Avancer de 1 à 6 cases.");
                q.Ajouter("Tendre une embuscade", "embuscade",
                    "Restez sur place : votre case et une case voisine deviennent un piège vivant jusqu'à votre prochain tour (+2 en duel).");
                var vus = new HashSet<string>();
                for (int i = 0; i < j.Sac.Count; i++)
                {
                    var o = j.Sac[i];
                    if (vus.Contains(o.Id)) continue;
                    vus.Add(o.Id);
                    bool utile = ObjetUtilisable(j, o);
                    q.Ajouter((o.Type == TypeObjet.Piege ? "Poser : " : "Utiliser : ") + o.Nom, "objet:" + o.Id, o.Description, utile, -1, -1, o);
                }
                yield return q;
                string code = q.CodeChoisi ?? "de";
                if (code == "de") { foreach (var s in Avancer(j)) yield return s; yield break; }
                if (code == "embuscade")
                {
                    bool tendue = false;
                    foreach (var s in TendreEmbuscade(j, r => tendue = r)) yield return s;
                    if (tendue) yield break;
                    continue;
                }
                if (code.StartsWith("objet:"))
                {
                    var o = Donnees.Objet(code.Substring(6));
                    foreach (var s in UtiliserObjet(j, o)) yield return s;
                    if (Arret) yield break;
                }
            }
        }

        IEnumerable<Signal> TendreEmbuscade(Joueur j, Action<bool> resultat)
        {
            var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.ChoixCase, Contexte = "embuscade", Titre = "Embuscade",
                Texte = "Choisissez la case voisine que vous couvrez en plus de la vôtre." };
            foreach (int v in Plateau.Voisins(j.Case))
                q.Ajouter(Plateau[v].Libelle + (Plateau[j.Case].Suivants.Contains(v) ? " (devant)" : " (derrière)"), "case", null, true, v);
            q.Ajouter("Annuler", "annuler");
            yield return q;
            if (q.CodeChoisi != "case") { resultat(false); yield break; }
            int c = q.Cases[q.Reponse];
            j.Embuscade = new Embuscade { Case = j.Case, CaseVoisine = c };
            resultat(true);
            yield return new SEffet { Effet = Effet.EmbuscadeTendue, Joueur = j.Id, Case = c };
            yield return new SAnnonce(j.Nom + " se tapit en embuscade.", j.Id, Ton.Combat);
        }

        // ================================================================== déplacement

        IEnumerable<Signal> Avancer(Joueur j)
        {
            int de = Des.D6();
            int bonus = j.A(EffetObjet.Bottes) ? 1 : 0;
            var lancer = new SDe { Joueur = j.Id, Faces = 6, Valeur = de, Bonus = bonus, Raison = "Déplacement" };
            yield return lancer;
            int pas = de + bonus;
            if (j.Lenteur)
            {
                j.Lenteur = false;
                pas = Math.Max(1, (pas + 1) / 2);
                yield return new SAnnonce("Ralenti par un sort, " + j.Nom + " n'avance que de " + pas + ".", j.Id, Ton.Mauvais);
            }
            if (j.Classe == ClasseHeros.Rodeur)
            {
                var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Rodeur, Titre = "Pisteur", Texte = "Combien de cases parcourir ?" };
                if (pas > 1) q.Ajouter((pas - 1) + " cases", "-1", null, true, CaseApres(j.Case, pas - 1));
                q.Ajouter(pas + " cases", "0", null, true, CaseApres(j.Case, pas));
                q.Ajouter((pas + 1) + " cases", "+1", null, true, CaseApres(j.Case, pas + 1));
                q.ParDefaut = pas > 1 ? 1 : 0;
                yield return q;
                if (q.CodeChoisi == "-1") pas--; else if (q.CodeChoisi == "+1") pas++;
            }
            foreach (var s in Marcher(j, pas, true)) yield return s;
        }

        /// Case atteinte en suivant toujours la grande route (pour l'aperçu du Rôdeur).
        public int CaseApres(int depart, int pas)
        {
            int c = depart;
            for (int i = 0; i < pas; i++) c = Plateau[c].Suivants[0];
            return c;
        }

        int profondeurEffets;

        IEnumerable<Signal> Marcher(Joueur j, int pas, bool effetArrivee)
        {
            bool arrete = false;
            while (pas > 0 && !Arret)
            {
                var c = Plateau[j.Case];
                int suivante = c.Suivants[0];
                if (c.Suivants.Count > 1)
                {
                    var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Embranchement, Titre = "Embranchement",
                        Texte = "Il vous reste " + pas + " case" + (pas > 1 ? "s" : "") + ". Quel chemin prendre ?" };
                    foreach (int s in c.Suivants)
                        q.Ajouter(Plateau[s].Voie ?? Plateau[s].Libelle, "voie", AideVoie(s), true, s);
                    yield return q;
                    suivante = q.Reponse >= 0 ? q.Cases[q.Reponse] : c.Suivants[0];
                }
                int de = j.Case;
                j.Case = suivante;
                pas--;
                yield return new SPas { Joueur = j.Id, De = de, Vers = suivante };

                if (suivante == Plateau.Depart)
                {
                    foreach (var s in GagnerOr(j, Donnees.SoldeVille, "solde de Valcreux")) yield return s;
                    foreach (var s in GagnerXP(j, 1)) yield return s;
                }
                foreach (var s in VerifierQueteLieu(j)) yield return s;
                if (Arret) yield break;

                var embusque = EmbusqueSur(suivante, j);
                if (embusque != null)
                {
                    embusque.Embuscade = null;
                    yield return new SEffet { Effet = Effet.EmbuscadeDeclenchee, Joueur = embusque.Id, Cible = j.Id, Case = suivante };
                    yield return new SAnnonce("Embuscade ! " + embusque.Nom + " surgit devant " + j.Nom + ".", embusque.Id, Ton.Combat, true);
                    foreach (var s in Duel(embusque, j, true)) yield return s;
                    arrete = true;
                    break;
                }
                var piege = PiegeSur(suivante);
                if (piege != null && piege.Proprietaire != j.Id)
                {
                    if (j.Classe == ClasseHeros.Voleur)
                    {
                        if (pas == 0) yield return new SAnnonce(j.Nom + " enjambe un piège sans le déclencher.", j.Id, Ton.Bon);
                    }
                    else
                    {
                        foreach (var s in DeclencherPiege(j, piege)) yield return s;
                        arrete = true;
                        break;
                    }
                }
            }
            if (Arret || arrete || !effetArrivee) yield break;
            foreach (var s in Arrivee(j)) yield return s;
        }

        string AideVoie(int premiere)
        {
            var c = Plateau[premiere];
            if (!c.Raccourci) return "La route principale, plus longue mais plus sûre.";
            switch (c.Zone)
            {
                case Zone.Marais: return "Raccourci à travers le marais : monstres et trésor.";
                case Zone.Cryptes: return "Raccourci sous terre : trésor, monstres et malédiction.";
                case Zone.Antre: return "Le chemin le plus court… par l'antre d'Ignathor, le dragon.";
            }
            return "Un raccourci.";
        }

        IEnumerable<Signal> Arrivee(Joueur j)
        {
            if (profondeurEffets > 1) yield break;
            profondeurEffets++;
            try
            {
                // D'abord le défi aux joueurs présents sur la case.
                var presents = new List<Joueur>();
                foreach (var o in JoueursSur(j.Case, j)) if (!o.ProtegeDuel) presents.Add(o);
                if (presents.Count > 0 && Plateau[j.Case].Type != TypeCase.Ville)
                {
                    var q = new SQuestion { Joueur = j.Id, Genre = GenreQuestion.Defi, Titre = "Duel ?",
                        Texte = "Vous croisez d'autres aventuriers. Lancer un défi ? Chacun lance son attaque + 1D3 ; le perdant perd un niveau." };
                    foreach (var o in presents)
                        q.Ajouter("Défier " + o.Nom, "defi", "Niveau " + o.Niveau + " · attaque " + o.Attaque, true, -1, o.Id);
                    q.Ajouter("Passer son chemin", "non");
                    q.ParDefaut = q.Options.Count - 1;
                    yield return q;
                    if (q.CodeChoisi == "defi")
                    {
                        var cible = Joueurs[q.Cibles[q.Reponse]];
                        foreach (var s in Duel(j, cible, false)) yield return s;
                        if (Arret) yield break;
                    }
                }
                foreach (var s in EffetCase(j)) yield return s;
            }
            finally { profondeurEffets--; }
        }

        // ================================================================== état des joueurs

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
            yield return new SAnnonce(j.Nom + " est K.O. (" + raison + ") ! Retour à Valcreux, la moitié de son or perdue.", j.Id, Ton.Mauvais, true);
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

        public void TeleporterSansEffet(Joueur j, int c) { j.Case = c; }
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

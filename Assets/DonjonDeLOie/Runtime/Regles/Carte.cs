// Donjon de l'Oie — une carte de jeu en données (faite dans l'éditeur de cartes) et sa conversion en plateau.
// Pas de type d'Unity ici : la carte est aussi lisible par les essais et les simulations.
using System;
using System.Collections.Generic;

namespace DonjonDeLOie.Regles
{
    [Serializable]
    public sealed class CaseCarte
    {
        public string Nom = "";                 // lieu nommé (cible des quêtes de voyage) ; vide : une case ordinaire
        public TypeCase Type = TypeCase.Chemin;
        public Zone Zone = Zone.Village;
        public float X, Z;                      // position en mètres
        public List<int> Suivants = new List<int>();   // cases suivantes ; la première est la « grande route »
        public string Voie = "";                // nom du chemin quand cette case ouvre un embranchement
        public ModeEvenements Mode = ModeEvenements.Standard;
        public List<string> Evenements = new List<string>();
        public string Lieu = "";                // bâtiment : "" automatique, "-" aucun, sinon l'id d'un décor
    }

    [Serializable]
    public sealed class DecorPose
    {
        public string Id = "rocher";
        public int Graine = 1;
        public float X, Z, RotY, Echelle = 1f;
    }

    [Serializable]
    public sealed class CarteDef
    {
        public string Nom = "Nouvelle carte";
        public int Depart;                      // index de la case de départ (soins, boutique complète, solde)
        public List<CaseCarte> Cases = new List<CaseCarte>();
        public List<DecorPose> Decors = new List<DecorPose>();   // décors et bâtiments posés à la main
        public bool DecorAutomatique = true;    // arbres, rochers… semés selon les zones
        public float DensiteDecor = 1f;         // 0 : aucun, 1 : normal, 2 : très dense
        public bool LieuxAutomatiques = true;   // bâtiments posés selon le type des cases (sauf choix de la case)
        public float Marge = 12f;               // largeur de terre autour du chemin, en mètres
        public int Graine = 7;                  // relief et décor
    }

    public sealed partial class Plateau
    {
        /// Fabrique un plateau à partir d'une carte (sans vérifier : appelez Verifier avant).
        public static Plateau Depuis(CarteDef carte)
        {
            var p = new Plateau();
            for (int i = 0; i < carte.Cases.Count; i++)
            {
                var k = carte.Cases[i];
                var c = new Case { Id = i, Type = k.Type, Zone = k.Zone, Nom = string.IsNullOrEmpty(k.Nom) ? null : k.Nom, X = k.X, Z = k.Z,
                    Voie = string.IsNullOrEmpty(k.Voie) ? null : k.Voie, Mode = k.Mode, Lieu = k.Lieu ?? "" };
                if (k.Evenements != null) foreach (var e in k.Evenements) if (!string.IsNullOrEmpty(e)) c.Evenements.Add(e);
                p.Cases.Add(c);
            }
            for (int i = 0; i < carte.Cases.Count; i++)
                foreach (int s in carte.Cases[i].Suivants)
                    if (s >= 0 && s < p.Cases.Count && s != i && !p.Cases[i].Suivants.Contains(s)) Lier(p, i, s);
            p.Depart = Math.Max(0, Math.Min(carte.Depart, p.Cases.Count - 1));
            p.MarquerRaccourcis();
            return p;
        }

        /// La grande boucle part du départ en suivant toujours la première sortie ; tout le reste est un raccourci.
        public void MarquerRaccourcis()
        {
            foreach (var c in Cases) c.Raccourci = true;
            int k = Depart, garde = 0;
            while (garde++ <= Cases.Count && Cases.Count > 0)
            {
                Cases[k].Raccourci = false;
                if (Cases[k].Suivants.Count == 0) break;
                k = Cases[k].Suivants[0];
                if (k == Depart) break;
            }
            // la première case d'un embranchement porte le nom de la voie
            foreach (var c in Cases)
                if (c.Suivants.Count > 1)
                    foreach (int s in c.Suivants)
                        if (Cases[s].Voie == null) Cases[s].Voie = Cases[s].Raccourci ? "Raccourci" : "Grande route";
        }

        /// La carte d'un plateau (pour partir de la carte standard dans l'éditeur).
        public CarteDef VersCarte(string nom)
        {
            var d = new CarteDef { Nom = nom, Depart = Depart };
            foreach (var c in Cases)
            {
                var k = new CaseCarte { Nom = c.Nom ?? "", Type = c.Type, Zone = c.Zone, X = c.X, Z = c.Z, Voie = c.Voie ?? "", Mode = c.Mode, Lieu = c.Lieu ?? "" };
                k.Suivants.AddRange(c.Suivants);
                k.Evenements.AddRange(c.Evenements);
                d.Cases.Add(k);
            }
            return d;
        }

        /// Les problèmes d'une carte. Les « erreurs » empêchent de jouer ; les « conseils » non. Les cases sont numérotées à partir de 1.
        public static void Verifier(CarteDef carte, List<string> erreurs, List<string> conseils)
        {
            int n = carte.Cases.Count;
            if (n < 6) { erreurs.Add("Il faut au moins 6 cases (il y en a " + n + ")."); if (n == 0) return; }
            if (carte.Depart < 0 || carte.Depart >= n) { erreurs.Add("La case de départ n'existe pas."); return; }
            if (carte.Cases[carte.Depart].Type != TypeCase.Ville) conseils.Add("La case de départ n'est pas une « Ville » : pas de soins ni de grande boutique au départ.");
            for (int i = 0; i < n; i++)
            {
                var c = carte.Cases[i];
                string nom = "Case " + (i + 1) + (string.IsNullOrEmpty(c.Nom) ? "" : " (« " + c.Nom + " »)");
                if (c.Suivants == null || c.Suivants.Count == 0) erreurs.Add(nom + " : aucune case suivante, les pions seraient bloqués.");
                else foreach (int s in c.Suivants) if (s < 0 || s >= n || s == i) erreurs.Add(nom + " : un lien mène nulle part.");
                if (c.Suivants != null && c.Suivants.Count > 2) conseils.Add(nom + " : plus de deux chemins ; le joueur choisira parmi " + c.Suivants.Count + ".");
                if (c.Evenements != null)
                    foreach (var e in c.Evenements) if (Donnees.Evenement(e) == null) erreurs.Add(nom + " : événement inconnu « " + e + " ».");
                if (c.Mode != ModeEvenements.Standard && (c.Evenements == null || c.Evenements.Count == 0))
                    conseils.Add(nom + " : mode « " + c.Mode + " » sans événement dans sa liste (il ne fera rien de plus).");
                if (c.Type == TypeCase.Antre && Donnees.Boss == null) erreurs.Add(nom + " : une case Antre demande un monstre « Boss » dans le catalogue.");
            }
            if (erreurs.Count > 0) return;
            // tout doit être atteignable depuis le départ, et le départ atteignable depuis partout (pas de cul-de-sac)
            var vus = Atteints(carte, carte.Depart, false);
            for (int i = 0; i < n; i++) if (!vus[i]) erreurs.Add("Case " + (i + 1) + " : on ne peut pas l'atteindre depuis le départ.");
            var retour = Atteints(carte, carte.Depart, true);
            for (int i = 0; i < n; i++) if (vus[i] && !retour[i]) erreurs.Add("Case " + (i + 1) + " : on ne peut plus revenir au départ en partant d'ici (cul-de-sac).");
            var noms = new HashSet<string>();
            foreach (var c in carte.Cases)
                if (!string.IsNullOrEmpty(c.Nom) && !noms.Add(c.Nom)) conseils.Add("Deux cases s'appellent « " + c.Nom + " » : les quêtes de voyage visent la première.");
            // cases qui se chevauchent (les dalles font environ 2 m)
            int proches = 0;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    float dx = carte.Cases[i].X - carte.Cases[j].X, dz = carte.Cases[i].Z - carte.Cases[j].Z;
                    if (dx * dx + dz * dz < 2.1f * 2.1f && proches++ < 5)
                        conseils.Add("Cases " + (i + 1) + " et " + (j + 1) + " : à moins de 2 m l'une de l'autre, leurs dalles se chevauchent.");
                }
            int lieux = 0;
            foreach (var c in carte.Cases) if (!string.IsNullOrEmpty(c.Nom)) lieux++;
            if (lieux < 3) conseils.Add("Moins de 3 lieux nommés : il y aura peu de quêtes de voyage.");
        }

        static bool[] Atteints(CarteDef carte, int depuis, bool aRebours)
        {
            int n = carte.Cases.Count;
            var vus = new bool[n];
            var file = new Queue<int>();
            vus[depuis] = true; file.Enqueue(depuis);
            while (file.Count > 0)
            {
                int c = file.Dequeue();
                if (!aRebours)
                {
                    foreach (int s in carte.Cases[c].Suivants) if (s >= 0 && s < n && !vus[s]) { vus[s] = true; file.Enqueue(s); }
                }
                else
                {
                    for (int i = 0; i < n; i++)
                        if (!vus[i] && carte.Cases[i].Suivants.Contains(c)) { vus[i] = true; file.Enqueue(i); }
                }
            }
            return vus;
        }

        /// Une petite carte de départ pour l'éditeur : un cercle de cases autour d'une ville.
        public static CarteDef CarteVide(string nom, int nombre = 16, float rayon = 14f)
        {
            var d = new CarteDef { Nom = nom };
            var types = new[] { TypeCase.Ville, TypeCase.Tresor, TypeCase.Monstre, TypeCase.Evenement, TypeCase.Chemin, TypeCase.Monstre, TypeCase.Marchand, TypeCase.Tresor,
                                TypeCase.Malediction, TypeCase.Monstre, TypeCase.Taverne, TypeCase.Evenement, TypeCase.Monstre, TypeCase.Sanctuaire, TypeCase.Tresor, TypeCase.Monstre };
            for (int i = 0; i < nombre; i++)
            {
                double a = -Math.PI / 2 + 2 * Math.PI * i / nombre;
                var k = new CaseCarte { Type = types[i % types.Length], Zone = i < nombre / 2 ? Zone.Village : Zone.Bois,
                    X = (float)(Math.Cos(a) * rayon), Z = (float)(Math.Sin(a) * rayon * 0.8) };
                if (i == 0) k.Nom = "Valcreux";
                if (k.Type == TypeCase.Taverne) k.Nom = "La Taverne";
                if (k.Type == TypeCase.Sanctuaire) k.Nom = "Le Sanctuaire";
                if (k.Type == TypeCase.Marchand) k.Nom = "L'Échoppe";
                k.Suivants.Add((i + 1) % nombre);
                d.Cases.Add(k);
            }
            return d;
        }
    }
}

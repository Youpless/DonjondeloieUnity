// Les Dés du Dragon — le plateau : une grande boucle de 40 cases et trois raccourcis.
using System;
using System.Collections.Generic;

namespace DesDuDragon.Regles
{
    public sealed class Case
    {
        public int Id;
        public TypeCase Type;
        public Zone Zone;
        public string Nom;          // lieu nommé (cible possible des quêtes), sinon null
        public string Voie;         // nom du chemin quand cette case ouvre un raccourci ou la grande route
        public float X, Z;          // position sur l'île, en mètres (Unity)
        public readonly List<int> Suivants = new List<int>();
        public readonly List<int> Precedents = new List<int>();
        public bool Raccourci;      // case d'un raccourci

        public string Libelle
        {
            get { return Nom ?? Donnees.NomsTypesCase[(int)Type]; }
        }
    }

    public sealed class Plateau
    {
        public readonly List<Case> Cases = new List<Case>();
        public int Depart;          // Valcreux

        public Case this[int i] { get { return Cases[i]; } }
        public int Nombre { get { return Cases.Count; } }

        public IEnumerable<int> Voisins(int id)
        {
            var c = Cases[id];
            for (int i = 0; i < c.Suivants.Count; i++) yield return c.Suivants[i];
            for (int i = 0; i < c.Precedents.Count; i++) yield return c.Precedents[i];
        }

        public bool SontVoisines(int a, int b)
        {
            foreach (int v in Voisins(a)) if (v == b) return true;
            return false;
        }

        /// Distance minimale en avançant de a vers b (les raccourcis comptent). -1 si plus de max pas.
        public int DistanceEnAvant(int a, int b, int max = 60)
        {
            if (a == b) return 0;
            var dist = new Dictionary<int, int>();
            var file = new Queue<int>();
            dist[a] = 0; file.Enqueue(a);
            while (file.Count > 0)
            {
                int c = file.Dequeue();
                int d = dist[c];
                if (d >= max) continue;
                foreach (int s in Cases[c].Suivants)
                {
                    if (dist.ContainsKey(s)) continue;
                    dist[s] = d + 1;
                    if (s == b) return d + 1;
                    file.Enqueue(s);
                }
            }
            return -1;
        }

        public List<int> CasesDeType(TypeCase t)
        {
            var l = new List<int>();
            for (int i = 0; i < Cases.Count; i++) if (Cases[i].Type == t) l.Add(i);
            return l;
        }

        // ------------------------------------------------------------------ fabrication

        const int Boucle = 40;

        struct Pose { public TypeCase T; public Zone Z; public string Nom; public Pose(TypeCase t, Zone z, string n = null) { T = t; Z = z; Nom = n; } }

        public static Plateau Standard()
        {
            var p = new Plateau();
            var V = Zone.Village; var B = Zone.Bois; var M = Zone.Marais; var C = Zone.Cryptes; var P = Zone.Pics; var A = Zone.Antre;
            var poses = new Pose[]
            {
                new Pose(TypeCase.Ville, V, "Valcreux"),
                new Pose(TypeCase.Tresor, V), new Pose(TypeCase.Evenement, V), new Pose(TypeCase.Monstre, V),
                new Pose(TypeCase.Taverne, V, "Auberge du Sanglier"),
                new Pose(TypeCase.Monstre, B), new Pose(TypeCase.Tresor, B), new Pose(TypeCase.Evenement, B),
                new Pose(TypeCase.Chemin, B, "Carrefour des Brumes"),
                new Pose(TypeCase.Monstre, B), new Pose(TypeCase.Marchand, B, "Étal du colporteur"),
                new Pose(TypeCase.Malediction, B), new Pose(TypeCase.Monstre, B),
                new Pose(TypeCase.Sanctuaire, M, "Chapelle des Saules"),
                new Pose(TypeCase.Evenement, M), new Pose(TypeCase.Monstre, M), new Pose(TypeCase.Tresor, M),
                new Pose(TypeCase.Malediction, M), new Pose(TypeCase.Monstre, M),
                new Pose(TypeCase.Taverne, M, "Le Crapaud Borgne"),
                new Pose(TypeCase.Evenement, C),
                new Pose(TypeCase.Chemin, C, "Portail des Cryptes"),
                new Pose(TypeCase.Monstre, C), new Pose(TypeCase.Malediction, C), new Pose(TypeCase.Tresor, C),
                new Pose(TypeCase.Monstre, C), new Pose(TypeCase.Sanctuaire, C, "Autel d'Ombrelune"),
                new Pose(TypeCase.Marchand, C, "Comptoir des Fossoyeurs"),
                new Pose(TypeCase.Monstre, P), new Pose(TypeCase.Evenement, P), new Pose(TypeCase.Tresor, P),
                new Pose(TypeCase.Chemin, P, "Col du Dragon"),
                new Pose(TypeCase.Monstre, P), new Pose(TypeCase.Malediction, P),
                new Pose(TypeCase.Marchand, P, "Forge des Pics"), new Pose(TypeCase.Monstre, P),
                new Pose(TypeCase.Evenement, V), new Pose(TypeCase.Tresor, V),
                new Pose(TypeCase.Sanctuaire, V, "Fontaine de Valcreux"), new Pose(TypeCase.Monstre, V),
            };
            if (poses.Length != Boucle) throw new Exception("boucle : " + poses.Length);

            // Grande boucle : une courbe fermée irrégulière, échantillonnée à pas égaux.
            var pts = Echantillonner(Boucle);
            for (int i = 0; i < Boucle; i++)
            {
                var c = new Case { Id = i, Type = poses[i].T, Zone = poses[i].Z, Nom = poses[i].Nom, X = pts[i * 2], Z = pts[i * 2 + 1] };
                p.Cases.Add(c);
            }
            for (int i = 0; i < Boucle; i++) Lier(p, i, (i + 1) % Boucle);
            p.Cases[0].Voie = "Grande route";

            // Raccourcis
            Raccourci(p, 8, 16, "Sentier des Brumes", new[]
            {
                new Pose(TypeCase.Monstre, M), new Pose(TypeCase.Tresor, M, "Îlot des Brumes"), new Pose(TypeCase.Monstre, M)
            }, 0.55f);
            Raccourci(p, 21, 27, "Catacombes", new[]
            {
                new Pose(TypeCase.Monstre, C), new Pose(TypeCase.Tresor, C, "Ossuaire"), new Pose(TypeCase.Malediction, C)
            }, 0.6f);
            Raccourci(p, 31, 38, "Antre du Dragon", new[]
            {
                new Pose(TypeCase.Monstre, A), new Pose(TypeCase.Tresor, A),
                new Pose(TypeCase.Antre, A, "Antre d'Ignathor"), new Pose(TypeCase.Tresor, A, "Trésor d'Ignathor")
            }, 0.75f);

            // La grande route est la suite de la boucle à chaque embranchement.
            foreach (int b in new[] { 8, 21, 31 }) p.Cases[(b + 1) % Boucle].Voie = "Grande route";
            p.Depart = 0;
            return p;
        }

        static void Lier(Plateau p, int a, int b)
        {
            p.Cases[a].Suivants.Add(b);
            p.Cases[b].Precedents.Add(a);
        }

        static void Raccourci(Plateau p, int de, int vers, string nom, Pose[] poses, float creux)
        {
            var a = p.Cases[de]; var b = p.Cases[vers];
            // Courbe de Bézier quadratique tirée vers le centre de l'île.
            float mx = (a.X + b.X) * 0.5f, mz = (a.Z + b.Z) * 0.5f;
            float cx = mx * (1f - creux), cz = mz * (1f - creux);
            int prev = de;
            for (int k = 0; k < poses.Length; k++)
            {
                float t = (k + 1f) / (poses.Length + 1f);
                float u = 1f - t;
                float x = u * u * a.X + 2f * u * t * cx + t * t * b.X;
                float z = u * u * a.Z + 2f * u * t * cz + t * t * b.Z;
                var c = new Case { Id = p.Cases.Count, Type = poses[k].T, Zone = poses[k].Z, Nom = poses[k].Nom, X = x, Z = z, Raccourci = true };
                if (k == 0) c.Voie = nom;
                p.Cases.Add(c);
                Lier(p, prev, c.Id);
                prev = c.Id;
            }
            Lier(p, prev, vers);
        }

        /// Points (x, z) d'une courbe fermée, à intervalles réguliers. Valcreux est au sud (vers la caméra d'accueil).
        static float[] Echantillonner(int n)
        {
            const int fin = 2000;
            var xs = new double[fin + 1]; var zs = new double[fin + 1]; var cum = new double[fin + 1];
            for (int i = 0; i <= fin; i++)
            {
                double t = -Math.PI / 2 + 2 * Math.PI * i / fin;
                double r = 1.0 + 0.07 * Math.Sin(3 * t + 0.6) + 0.04 * Math.Cos(5 * t);
                xs[i] = 27.0 * Math.Cos(t) * r;
                zs[i] = 21.0 * Math.Sin(t) * r;
                if (i > 0) cum[i] = cum[i - 1] + Math.Sqrt((xs[i] - xs[i - 1]) * (xs[i] - xs[i - 1]) + (zs[i] - zs[i - 1]) * (zs[i] - zs[i - 1]));
            }
            var res = new float[n * 2];
            int j = 0;
            for (int k = 0; k < n; k++)
            {
                double cible = cum[fin] * k / n;
                while (j < fin && cum[j + 1] < cible) j++;
                double f = (cible - cum[j]) / Math.Max(1e-9, cum[j + 1] - cum[j]);
                res[k * 2] = (float)(xs[j] + (xs[j + 1] - xs[j]) * f);
                res[k * 2 + 1] = (float)(zs[j] + (zs[j + 1] - zs[j]) * f);
            }
            return res;
        }
    }
}

// Donjon de l'Oie — l'île, calculée sans Unity : relief en colonnes (cellules de 50 cm, marches de 25 cm), zones,
// chemin entre les cases, dalles des cases, emplacements du décor et des lieux, lampes.
// La vue Unity (IleVue) et le rendu hors Unity (outils d'aperçu) lisent le même modèle.
using System;
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Monde
{
    public struct Placement
    {
        public string Id; public int Graine; public Vector3 Pos; public float RotY; public float Echelle; public bool Lieu;
    }

    public struct Lampe
    {
        public Vector3 Pos; public Color32 Couleur; public float Portee, Intensite;
    }

    public sealed partial class IleModele
    {
        public const float Cell = 0.5f, Marche = 0.25f, NiveauEau = 0f;
        public const float EpaisseurDalle = 0.3f;
        public const int TailleMax = 900;                      // cellules par côté au plus (450 m)

        // La grille couvre les cases, plus la marge de terre et un peu de mer.
        public readonly int NX, NZ;
        public readonly float X0, Z0;
        public Vector3 Centre;                                 // milieu des cases

        public readonly Plateau Plateau;
        public readonly CarteDef Options;
        public readonly float[] H;
        public readonly byte[] Mat;
        public readonly Zone[] ZoneCell;
        public readonly float[] Dist;                          // distance au chemin
        public readonly float[] Cote;                          // < 1 : terre (0 au cœur de l'île), 1 : le rivage, > 1 : la mer
        public Vector3[] Cases;                                // centre du dessus de chaque dalle
        public readonly List<Placement> Decors = new List<Placement>();
        public readonly List<Placement> Lieux = new List<Placement>();
        public readonly List<Lampe> Lampes = new List<Lampe>();
        public Vector3 Montagne;                               // centre de la montagne de l'antre
        readonly int graine;

        /// options : la carte (marge, décor, graine, décors posés à la main) ; null pour les réglages de la carte standard.
        public IleModele(Plateau p, CarteDef options = null)
        {
            Plateau = p;
            Options = options ?? new CarteDef();
            graine = Options.Graine;
            float minX = 1e9f, maxX = -1e9f, minZ = 1e9f, maxZ = -1e9f, cx = 0, cz = 0; int n = 0;
            foreach (var c in p.Cases)
            {
                minX = Math.Min(minX, c.X); maxX = Math.Max(maxX, c.X); minZ = Math.Min(minZ, c.Z); maxZ = Math.Max(maxZ, c.Z);
                cx += c.X; cz += c.Z; n++;
            }
            if (p.Nombre == 0) { minX = minZ = -10; maxX = maxZ = 10; }
            Centre = n > 0 ? new Vector3(cx / n, 0, cz / n) : Vector3.zero;
            float bord = Math.Max(4f, Options.Marge) + 12f;
            X0 = Mathf.Floor((minX - bord) / Cell) * Cell; Z0 = Mathf.Floor((minZ - bord) / Cell) * Cell;
            NX = Math.Min(TailleMax, (int)Math.Ceiling((maxX + bord - X0) / Cell));
            NZ = Math.Min(TailleMax, (int)Math.Ceiling((maxZ + bord - Z0) / Cell));
            H = new float[NX * NZ]; Mat = new byte[NX * NZ]; ZoneCell = new Zone[NX * NZ]; Dist = new float[NX * NZ]; Cote = new float[NX * NZ];
            Construire();
        }

        int I(int x, int z) { return z * NX + x; }
        public float CX(int x) { return X0 + (x + 0.5f) * Cell; }
        public float CZ(int z) { return Z0 + (z + 0.5f) * Cell; }
        public float LargeurM { get { return NX * Cell; } }
        public float ProfondeurM { get { return NZ * Cell; } }

        public float HauteurEn(float x, float z)
        {
            int ix = Mathf.Clamp((int)((x - X0) / Cell), 0, NX - 1), iz = Mathf.Clamp((int)((z - Z0) / Cell), 0, NZ - 1);
            return H[I(ix, iz)];
        }

        public Zone ZoneEn(float x, float z)
        {
            int ix = Mathf.Clamp((int)((x - X0) / Cell), 0, NX - 1), iz = Mathf.Clamp((int)((z - Z0) / Cell), 0, NZ - 1);
            return ZoneCell[I(ix, iz)];
        }

        public float DistanceChemin(float x, float z)
        {
            int ix = Mathf.Clamp((int)((x - X0) / Cell), 0, NX - 1), iz = Mathf.Clamp((int)((z - Z0) / Cell), 0, NZ - 1);
            return Dist[I(ix, iz)];
        }

        // ------------------------------------------------------------------ bruit

        float Bruit(float x, float z, int o)
        {
            return Mathf.PerlinNoise(x + graine * 13.7f + o * 71.3f, z + graine * 5.3f + o * 37.9f);
        }

        float Fbm(float x, float z, int o)
        {
            return Bruit(x, z, o) * 0.55f + Bruit(x * 2.1f, z * 2.1f, o + 1) * 0.3f + Bruit(x * 4.3f, z * 4.3f, o + 2) * 0.15f;
        }

        static float Lisse(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3 - 2 * t); }

        /// < 1 sur terre (0 au cœur de l'île), 1 au rivage, > 1 en mer — calculé depuis le tracé du chemin (voir CalculerCote).
        public float Ellipse(float x, float z)
        {
            int ix = (int)Math.Floor((x - X0) / Cell), iz = (int)Math.Floor((z - Z0) / Cell);
            if (ix < 0 || iz < 0 || ix >= NX || iz >= NZ)
            {
                float dx = Math.Max(0, Math.Max(X0 - x, x - (X0 + LargeurM))), dz = Math.Max(0, Math.Max(Z0 - z, z - (Z0 + ProfondeurM)));
                return 1.4f + Mathf.Sqrt(dx * dx + dz * dz) / 30f;
            }
            return Cote[I(ix, iz)];
        }

        /// La terre = une bande de « Marge » mètres autour du chemin (bord bruité) + tout ce que les boucles entourent.
        /// Puis, pour chaque cellule, la distance au rivage : Cote = 1 − d/30 sur terre, 1 + d/30 en mer.
        void CalculerCote()
        {
            float marge = Math.Max(3f, Options.Marge);
            var terre = new bool[NX * NZ];
            for (int z = 0; z < NZ; z++)
                for (int x = 0; x < NX; x++)
                {
                    float px = CX(x), pz = CZ(z);
                    float m = marge * (0.8f + 0.45f * Fbm(px * 0.05f, pz * 0.05f, 3));
                    terre[I(x, z)] = Dist[I(x, z)] < m;
                }
            // ce que l'on ne peut pas rejoindre depuis le bord de la grille sans traverser la terre est enclos : c'est de la terre
            var mer = new bool[NX * NZ];
            var file = new Queue<int>();
            for (int x = 0; x < NX; x++) { Semer(x, 0, terre, mer, file); Semer(x, NZ - 1, terre, mer, file); }
            for (int z = 0; z < NZ; z++) { Semer(0, z, terre, mer, file); Semer(NX - 1, z, terre, mer, file); }
            while (file.Count > 0)
            {
                int k = file.Dequeue(); int x = k % NX, z = k / NX;
                if (x > 0) Semer(x - 1, z, terre, mer, file);
                if (x < NX - 1) Semer(x + 1, z, terre, mer, file);
                if (z > 0) Semer(x, z - 1, terre, mer, file);
                if (z < NZ - 1) Semer(x, z + 1, terre, mer, file);
            }
            for (int i = 0; i < terre.Length; i++) terre[i] = !mer[i];
            // distance au rivage (deux passes de chanfrein), en mètres
            var dTerre = Chanfrein(terre, true); var dMer = Chanfrein(terre, false);
            for (int i = 0; i < Cote.Length; i++) Cote[i] = terre[i] ? 1f - dTerre[i] / 30f : 1f + dMer[i] / 30f;
        }

        void Semer(int x, int z, bool[] terre, bool[] mer, Queue<int> file)
        {
            int k = I(x, z);
            if (terre[k] || mer[k]) return;
            mer[k] = true; file.Enqueue(k);
        }

        /// Distance (m) de chaque cellule « dedans » à la cellule la plus proche « dehors ».
        float[] Chanfrein(bool[] terre, bool dedansEstTerre)
        {
            const float A = Cell, D = Cell * 1.4142f, Inf = 1e6f;
            var d = new float[NX * NZ];
            for (int i = 0; i < d.Length; i++) d[i] = terre[i] == dedansEstTerre ? Inf : 0f;
            for (int z = 0; z < NZ; z++)
                for (int x = 0; x < NX; x++)
                {
                    int k = I(x, z); if (d[k] == 0) continue;
                    float v = d[k];
                    if (x > 0) v = Math.Min(v, d[k - 1] + A);
                    if (z > 0) v = Math.Min(v, d[k - NX] + A);
                    if (x > 0 && z > 0) v = Math.Min(v, d[k - NX - 1] + D);
                    if (x < NX - 1 && z > 0) v = Math.Min(v, d[k - NX + 1] + D);
                    d[k] = v;
                }
            for (int z = NZ - 1; z >= 0; z--)
                for (int x = NX - 1; x >= 0; x--)
                {
                    int k = I(x, z); if (d[k] == 0) continue;
                    float v = d[k];
                    if (x < NX - 1) v = Math.Min(v, d[k + 1] + A);
                    if (z < NZ - 1) v = Math.Min(v, d[k + NX] + A);
                    if (x < NX - 1 && z < NZ - 1) v = Math.Min(v, d[k + NX + 1] + D);
                    if (x > 0 && z < NZ - 1) v = Math.Min(v, d[k + NX - 1] + D);
                    d[k] = v;
                }
            for (int i = 0; i < d.Length; i++) if (d[i] >= Inf) d[i] = 30f;
            return d;
        }

        // ------------------------------------------------------------------ construction

        void Construire()
        {
            var tmp = new float[NX * NZ];
            Montagne = PlacerMontagne();

            // 1. distance au chemin, zone la plus proche
            for (int z = 0; z < NZ; z++)
                for (int x = 0; x < NX; x++)
                {
                    float px = CX(x), pz = CZ(z);
                    float best = 1e9f;
                    for (int c = 0; c < Plateau.Nombre; c++)
                        foreach (int s in Plateau[c].Suivants)
                        {
                            float d = DistSeg(px, pz, Plateau[c].X, Plateau[c].Z, Plateau[s].X, Plateau[s].Z);
                            if (d < best) best = d;
                        }
                    Dist[I(x, z)] = best;
                    float bz = 1e9f; Zone zo = Zone.Village;
                    for (int c = 0; c < Plateau.Nombre; c++)
                    {
                        float dx = px - Plateau[c].X, dz = pz - Plateau[c].Z;
                        float d = Mathf.Sqrt(dx * dx + dz * dz) + (Bruit(px * 0.18f, pz * 0.18f, 10 + (int)Plateau[c].Zone) - 0.5f) * 5f;
                        if (Plateau[c].Raccourci) d += 4f;      // la grande boucle donne le ton, les raccourcis colorent leurs abords
                        if (d < bz) { bz = d; zo = Plateau[c].Zone; }
                    }
                    float dm = Mathf.Sqrt((px - Montagne.x) * (px - Montagne.x) + (pz - Montagne.z) * (pz - Montagne.z));
                    if (aMontagne && dm < 10f + Bruit(px * 0.2f, pz * 0.2f, 20) * 3f) zo = Zone.Antre;
                    else if (zo == Zone.Antre)
                    {
                        // loin de la montagne, le raccourci de l'antre traverse les Pics
                        zo = Zone.Pics;
                    }
                    ZoneCell[I(x, z)] = zo;
                }

            CalculerCote();

            // 2. relief brut
            for (int z = 0; z < NZ; z++)
                for (int x = 0; x < NX; x++)
                {
                    float px = CX(x), pz = CZ(z);
                    tmp[I(x, z)] = Relief(px, pz, ZoneCell[I(x, z)], Dist[I(x, z)]);
                }
            // 3. lissage (deux passes) pour fondre les zones
            for (int passe = 0; passe < 2; passe++)
            {
                var b = (float[])tmp.Clone();
                for (int z = 1; z < NZ - 1; z++)
                    for (int x = 1; x < NX - 1; x++)
                    {
                        float s = 0;
                        for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) s += b[I(x + dx, z + dz)];
                        tmp[I(x, z)] = s / 9f;
                    }
            }
            // 4. hauteur des cases
            Cases = new Vector3[Plateau.Nombre];
            var hc = new float[Plateau.Nombre];
            for (int c = 0; c < Plateau.Nombre; c++)
            {
                float h = Echantillon(tmp, Plateau[c].X, Plateau[c].Z);
                h = Mathf.Max(0.5f, Mathf.Min(h, Plateau[c].Zone == Zone.Pics || Plateau[c].Zone == Zone.Antre ? 2.5f : 1.5f));
                hc[c] = Quant(h);
            }
            // 5. le chemin aplanit le terrain, la montagne s'élève
            for (int z = 0; z < NZ; z++)
                for (int x = 0; x < NX; x++)
                {
                    int i = I(x, z);
                    float px = CX(x), pz = CZ(z);
                    float h = tmp[i];
                    float cible = HauteurChemin(px, pz, hc);
                    float d = Dist[i];
                    if (d < 1.7f) h = cible;
                    else if (d < 3.6f) h = Mathf.Lerp(cible, h, Lisse(1.7f, 3.6f, d));
                    H[i] = Quant(h);
                }
            for (int c = 0; c < Plateau.Nombre; c++)
                Cases[c] = new Vector3(Plateau[c].X, hc[c] + EpaisseurDalle, Plateau[c].Z);

            // 6. matières
            for (int z = 0; z < NZ; z++)
                for (int x = 0; x < NX; x++)
                {
                    int i = I(x, z);
                    Mat[i] = Matiere(CX(x), CZ(z), ZoneCell[i], H[i], Dist[i]);
                }

            PlacerLieux();
            Semer();
        }

        bool aMontagne;

        /// Derrière la case Antre (s'il y en a une), du côté opposé au chemin, là où la montagne ne recouvre aucune route.
        Vector3 PlacerMontagne()
        {
            int antre = -1;
            for (int i = 0; i < Plateau.Nombre; i++) if (Plateau[i].Type == TypeCase.Antre) antre = i;
            aMontagne = antre >= 0;
            if (!aMontagne) return new Vector3(1e5f, 0, 1e5f);
            var pa = new Vector3(Plateau[antre].X, 0, Plateau[antre].Z);
            Vector3 best = pa; float bestScore = -1e9f;
            for (int a = 0; a < 24; a++)
            {
                float ang = a * Mathf.PI * 2f / 24f;
                var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                var p = pa + dir * 7.5f;
                float libre = 1e9f;
                for (int c = 0; c < Plateau.Nombre; c++)
                    foreach (int s in Plateau[c].Suivants)
                    {
                        if (c == antre || s == antre) continue;
                        libre = Math.Min(libre, DistSeg(p.x, p.z, Plateau[c].X, Plateau[c].Z, Plateau[s].X, Plateau[s].Z));
                    }
                // préférer : loin des autres routes, vers le cœur de l'île, et pas vers la caméra (sud)
                var versCentre = Centre - pa; versCentre.y = 0;
                float score = Math.Min(libre, 9f) * 2f + (versCentre.sqrMagnitude > 1f ? Vector3.Dot(dir, versCentre.normalized) * 3f : 0f) + dir.z * 1.5f;
                if (score > bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        static float Quant(float h) { return Mathf.Round(h / Marche) * Marche; }

        float Echantillon(float[] t, float x, float z)
        {
            int ix = Mathf.Clamp((int)((x - X0) / Cell), 0, NX - 1), iz = Mathf.Clamp((int)((z - Z0) / Cell), 0, NZ - 1);
            return t[I(ix, iz)];
        }

        static float DistSeg(float px, float pz, float ax, float az, float bx, float bz)
        {
            float vx = bx - ax, vz = bz - az, l2 = vx * vx + vz * vz;
            float t = l2 > 0 ? Mathf.Clamp01(((px - ax) * vx + (pz - az) * vz) / l2) : 0;
            float dx = ax + vx * t - px, dz = az + vz * t - pz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        float HauteurChemin(float px, float pz, float[] hc)
        {
            float best = 1e9f, h = 0.5f;
            for (int c = 0; c < Plateau.Nombre; c++)
                foreach (int s in Plateau[c].Suivants)
                {
                    float ax = Plateau[c].X, az = Plateau[c].Z, bx = Plateau[s].X, bz = Plateau[s].Z;
                    float vx = bx - ax, vz = bz - az, l2 = vx * vx + vz * vz;
                    float t = l2 > 0 ? Mathf.Clamp01(((px - ax) * vx + (pz - az) * vz) / l2) : 0;
                    float dx = ax + vx * t - px, dz = az + vz * t - pz, d = dx * dx + dz * dz;
                    if (d < best) { best = d; h = Mathf.Lerp(hc[c], hc[s], Lisse(0.25f, 0.75f, t)); }
                }
            return h;
        }

        float Relief(float x, float z, Zone zo, float d)
        {
            float e = Ellipse(x, z);
            float terre = 0.55f + (Fbm(x * 0.07f, z * 0.07f, 0) - 0.5f) * 0.7f;
            float f = Fbm(x * 0.11f, z * 0.11f, 4);
            switch (zo)
            {
                case Zone.Village: terre += 0.1f * f; break;
                case Zone.Bois: terre += 0.25f + 1.0f * f * Lisse(2f, 7f, d); break;
                case Zone.Marais: terre += -1.05f + 0.9f * f; break;
                case Zone.Cryptes: terre += 0.35f + 0.6f * f * Lisse(2f, 6f, d); break;
                case Zone.Pics:
                {
                    float crete = 1f - Mathf.Abs(Fbm(x * 0.09f, z * 0.09f, 6) * 2f - 1f);
                    terre += 0.5f + 3.2f * Lisse(2.5f, 11f, d) * (0.3f + crete * 0.8f);
                    break;
                }
                case Zone.Antre: terre += 0.9f + 1.8f * Lisse(3f, 10f, d) * f; break;
            }
            // le centre de l'île forme un plateau
            terre += 0.7f * Lisse(0.7f, 0.3f, e);
            // les falaises et la plage
            float plage = Lisse(0.9f, 1.03f, e);
            float h = Mathf.Lerp(terre, -1.4f, plage);
            if (e > 1.08f) h = -1.6f - (e - 1.08f) * 4f;
            return h;
        }

        // Matières : 0 herbe claire, 1 herbe de forêt, 2 mousse olive, 3 boue, 4 herbe morte, 5 terre sombre, 6 cendre, 7 roche,
        // 8 roche sombre, 9 sable, 10 fond mouillé, 11 chemin de terre, 12 pavés, 13 chemin de cendre, 14 chemin de boue, 15 fleurs
        byte Matiere(float x, float z, Zone zo, float h, float d)
        {
            float e = Ellipse(x, z);
            if (h < NiveauEau - 0.01f) return (byte)(e > 0.95f ? 10 : (zo == Zone.Marais ? 3 : 10));
            if (e > 0.93f && h < 0.6f) return 9;
            float bruit = Bruit(x * 0.6f, z * 0.6f, 30);
            float grosBruit = Bruit(x * 0.17f, z * 0.17f, 31);
            if (d < 0.75f + (bruit - 0.5f) * 0.3f)
            {
                switch (zo)
                {
                    case Zone.Village: return 12;
                    case Zone.Cryptes: return 12;
                    case Zone.Marais: return 14;
                    case Zone.Pics: case Zone.Antre: return 13;
                    default: return 11;
                }
            }
            switch (zo)
            {
                case Zone.Village: return (byte)(bruit > 0.78f && d > 2.5f ? 15 : 0);
                case Zone.Bois: return (byte)(grosBruit > 0.7f ? 0 : 1);
                case Zone.Marais: return (byte)(grosBruit > 0.66f ? 3 : 2);
                case Zone.Cryptes: return (byte)(grosBruit > 0.68f ? 5 : 4);
                case Zone.Pics: return (byte)(h > 2.2f && grosBruit > 0.45f ? 7 : (grosBruit > 0.72f ? 8 : 6));
                default: return (byte)(grosBruit > 0.6f ? 8 : 7);
            }
        }

        public static readonly Color32[] CouleursMat =
        {
            Pal.C("86ad4c"), Pal.C("557d33"), Pal.C("75773b"), Pal.C("5c4a31"), Pal.C("8c8a62"), Pal.C("5b4f45"), Pal.C("6e6058"), Pal.C("7c7068"),
            Pal.C("4a4240"), Pal.C("d8c48f"), Pal.C("9c8a64"), Pal.C("a07f56"), Pal.C("9a958b"), Pal.C("6b5e57"), Pal.C("6e5a3e"), Pal.C("8fb24e"),
        };

        public static readonly Color32[] CouleursFlanc =
        {
            Pal.C("7b5c3e"), Pal.C("6a4d33"), Pal.C("5d4a33"), Pal.C("4c3d2a"), Pal.C("6a5e4c"), Pal.C("4c433c"), Pal.C("564c48"), Pal.C("615851"),
            Pal.C("3c3534"), Pal.C("c2ad78"), Pal.C("85755a"), Pal.C("85694a"), Pal.C("7e7a72"), Pal.C("564b46"), Pal.C("594833"), Pal.C("7b5c3e"),
        };
    }
}

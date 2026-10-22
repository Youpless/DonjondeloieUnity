// Donjon de l'Oie — les douze monstres (onze, plus le Dragonnoie) : point d'entrée et outils de sculpture communs.
// Chaque monstre est construit dans un fichier partiel (MonstresModeles.<Nom>.cs) ; ici, le choix par identifiant
// et les primitives « taillées » (boîtes chanfreinées, tronçons octogonaux, chaînes de traits, membranes).
using System;
using UnityEngine;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static partial class MonstresModeles
    {
        public static VoxelRig Construire(string id)
        {
            switch (id)
            {
                case "rat": return Rat();
                case "gobelin": return Gobelin();
                case "loup": return Loup();
                case "araignee": return Araignee();
                case "gelee": return Gelee();
                case "squelette": return Squelette();
                case "orc": return Orc();
                case "spectre": return Spectre();
                case "troll": return Troll();
                case "wyverne": return Wyverne();
                case "dragon": return Dragon();
                case "dragonnoie": return Dragonnoie();
            }
            // Repli : un simple bloc rouge.
            var m = new VoxelModel();
            BoiteC(m, -5, 0, -5, 4, 9, 4, Pal.Rouge, 1);
            var r = new VoxelRig { Name = "inconnu", VoxelSize = 0.05f, Origin = Vector3.zero };
            r.Add("corps", m, new Vector3(0, 5, 0));
            return r;
        }

        // ------------------------------------------------------------------ couleurs

        static Color32 C(string h) { return Pal.C(h); }

        static float Lerp(float a, float b, float t) { return a + (b - a) * t; }

        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        // ------------------------------------------------------------------ rigs

        static VoxelRig NouveauRig(string nom, float taille)
        {
            return new VoxelRig { Name = nom, VoxelSize = taille };
        }

        /// Pose l'origine au sol, au centre de l'emprise en z (x = 0 : les corps sont centrés sur l'axe).
        static VoxelRig Fin(VoxelRig r)
        {
            float zmin = float.MaxValue, zmax = float.MinValue;
            foreach (var p in r.Parts)
            {
                Vector3Int a, b;
                if (!p.Model.Bounds(out a, out b)) continue;
                zmin = Math.Min(zmin, a.z); zmax = Math.Max(zmax, b.z + 1);
            }
            r.Origin = new Vector3(0, 0, zmin < zmax ? (float)Math.Round((zmin + zmax) * 0.5f) : 0);
            return r;
        }

        // ------------------------------------------------------------------ volumes taillés

        /// Boîte pleine aux arêtes coupées sur « ch » voxels (biseau).
        static void BoiteC(VoxelModel m, int x0, int y0, int z0, int x1, int y1, int z1, Color32 c, int ch)
        {
            if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
            if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
            if (z0 > z1) { int t = z0; z0 = z1; z1 = t; }
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
            {
                int dx = Math.Min(x - x0, x1 - x), dy = Math.Min(y - y0, y1 - y), dz = Math.Min(z - z0, z1 - z);
                if (dx + dy < ch || dx + dz < ch || dy + dz < ch) continue;
                m.Set(x, y, z, c);
            }
        }

        /// Une section octogonale (rectangle de demi-côtés a × b, coins coupés de ch) dans un plan.
        /// axe 0 : plan (y, z) à x = k ; axe 1 : plan (x, z) à y = k ; axe 2 : plan (x, y) à z = k. (u, v) : centre dans le plan.
        static void Section(VoxelModel m, int axe, int k, float cu, float cv, float a, float b, float ch, Color32 c)
        {
            int u0 = (int)Math.Floor(cu - a), u1 = (int)Math.Ceiling(cu + a);
            int v0 = (int)Math.Floor(cv - b), v1 = (int)Math.Ceiling(cv + b);
            for (int u = u0; u <= u1; u++) for (int v = v0; v <= v1; v++)
            {
                float du = Math.Abs(u + 0.5f - cu), dv = Math.Abs(v + 0.5f - cv);
                if (du > a || dv > b || du + dv > a + b - ch) continue;
                if (axe == 2) m.Set(u, v, k, c);
                else if (axe == 1) m.Set(u, k, v, c);
                else m.Set(k, u, v, c);
            }
        }

        /// Tronçon allongé selon z (de z0 à z1) : centre (cx, cy), demi-largeur w, demi-hauteur h, interpolés.
        static void TronconZ(VoxelModel m, int z0, int z1, float cx, float cy0, float cy1, float w0, float w1, float h0, float h1, float ch, Color32 c)
        {
            for (int z = z0; z <= z1; z++)
            {
                float t = z1 == z0 ? 0 : (z - z0) / (float)(z1 - z0);
                Section(m, 2, z, cx, Lerp(cy0, cy1, t), Lerp(w0, w1, t), Lerp(h0, h1, t), ch, c);
            }
        }

        /// Tronçon vertical (de y0 à y1) : centre (cx, cz), demi-largeur w (x), demi-profondeur d (z), interpolés.
        static void TronconY(VoxelModel m, int y0, int y1, float cx, float cz0, float cz1, float w0, float w1, float d0, float d1, float ch, Color32 c)
        {
            for (int y = y0; y <= y1; y++)
            {
                float t = y1 == y0 ? 0 : (y - y0) / (float)(y1 - y0);
                Section(m, 1, y, cx, Lerp(cz0, cz1, t), Lerp(w0, w1, t), Lerp(d0, d1, t), ch, c);
            }
        }

        /// Corps allongé selon z, profil par clés : k = { z, cy, demi-largeur, demi-hauteur,  z, cy, … } (z croissants).
        /// Le centre en x est cx ; les sections sont octogonales (coins coupés selon « ch » × la plus petite demi-taille).
        static void ProfilZ(VoxelModel m, float cx, float[] k, float ch, Color32 c)
        {
            int n = k.Length / 4;
            for (int i = 1; i < n; i++)
            {
                int z0 = (int)k[i * 4 - 4], z1 = (int)k[i * 4];
                for (int z = z0; z <= z1; z++)
                {
                    float t = z1 == z0 ? 0 : (z - z0) / (float)(z1 - z0);
                    float cy = Lerp(k[i * 4 - 3], k[i * 4 + 1], t), w = Lerp(k[i * 4 - 2], k[i * 4 + 2], t), h = Lerp(k[i * 4 - 1], k[i * 4 + 3], t);
                    Section(m, 2, z, cx, cy, w, h, ch * Math.Min(w, h), c);
                }
            }
        }

        /// Corps vertical, profil par clés : k = { y, cz, demi-largeur (x), demi-profondeur (z), … } (y croissants).
        static void ProfilY(VoxelModel m, float cx, float[] k, float ch, Color32 c)
        {
            int n = k.Length / 4;
            for (int i = 1; i < n; i++)
            {
                int y0 = (int)k[i * 4 - 4], y1 = (int)k[i * 4];
                for (int y = y0; y <= y1; y++)
                {
                    float t = y1 == y0 ? 0 : (y - y0) / (float)(y1 - y0);
                    float cz = Lerp(k[i * 4 - 3], k[i * 4 + 1], t), w = Lerp(k[i * 4 - 2], k[i * 4 + 2], t), d = Lerp(k[i * 4 - 1], k[i * 4 + 3], t);
                    Section(m, 1, y, cx, cz, w, d, ch * Math.Min(w, d), c);
                }
            }
        }

        /// Suite de traits épais passant par des points (x, y, z, x, y, z…), rayon passant de r0 à r1.
        static void Chaine(VoxelModel m, float[] p, float r0, float r1, Color32 c)
        {
            int n = p.Length / 3;
            float total = 0;
            for (int i = 1; i < n; i++) total += (V(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]) - V(p[i * 3 - 3], p[i * 3 - 2], p[i * 3 - 1])).magnitude;
            float acc = 0;
            for (int i = 1; i < n; i++)
            {
                var a = V(p[i * 3 - 3], p[i * 3 - 2], p[i * 3 - 1]); var b = V(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
                float l = (b - a).magnitude;
                float ra = Lerp(r0, r1, acc / total), rb = Lerp(r0, r1, (acc + l) / total);
                m.Line(a.x, a.y, a.z, b.x, b.y, b.z, ra, rb, c);
                acc += l;
            }
        }

        /// Membrane : triangle plein d'environ un voxel et demi d'épaisseur (ailes, voiles).
        static void Triangle(VoxelModel m, Vector3 a, Vector3 b, Vector3 c, Color32 col)
        {
            var n = Vector3.Cross(b - a, c - a).normalized * 0.55f;
            float l = Math.Max((b - a).magnitude, Math.Max((c - a).magnitude, (c - b).magnitude));
            int k = (int)Math.Ceiling(l * 2.2f) + 1;
            for (int i = 0; i <= k; i++) for (int j = 0; j <= k - i; j++)
            {
                var p = a + (b - a) * (i / (float)k) + (c - a) * (j / (float)k);
                var q = p + n;
                m.Set((int)Math.Floor(p.x), (int)Math.Floor(p.y), (int)Math.Floor(p.z), col);
                m.Set((int)Math.Floor(q.x), (int)Math.Floor(q.y), (int)Math.Floor(q.z), col);
            }
        }

        /// Disque face à +z (bouclier), de z0 à z1.
        static void DisqueZ(VoxelModel m, float cx, float cy, int z0, int z1, float r, Color32 c)
        {
            for (int z = z0; z <= z1; z++)
                for (int x = (int)Math.Floor(cx - r); x <= (int)Math.Ceiling(cx + r); x++)
                    for (int y = (int)Math.Floor(cy - r); y <= (int)Math.Ceiling(cy + r); y++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        if (dx * dx + dy * dy <= r * r) m.Set(x, y, z, c);
                    }
        }

        // ------------------------------------------------------------------ retouches

        /// z du voxel le plus en avant de la colonne (x, y), ou int.MinValue.
        static int Avant(VoxelModel m, int x, int y, int zmin, int zmax)
        {
            for (int z = zmax; z >= zmin; z--) if (m.Has(x, y, z)) return z;
            return int.MinValue;
        }

        /// y du voxel le plus haut de la colonne (x, z), ou int.MinValue.
        static int Haut(VoxelModel m, int x, int z, int ymin, int ymax)
        {
            for (int y = ymax; y >= ymin; y--) if (m.Has(x, y, z)) return y;
            return int.MinValue;
        }

        /// Retire de « a » tous les voxels présents dans « b » (évite les faces confondues entre pièces).
        static void Soustraire(VoxelModel a, VoxelModel b)
        {
            foreach (var kv in b.Tous()) a.Remove(kv.Key.x, kv.Key.y, kv.Key.z);
        }

        /// Pose un voxel lumineux dans « lueur » et le retire de la pièce porteuse (œil affleurant).
        static void Oeil(VoxelModel porteur, VoxelModel lueur, int x, int y, int z, Color32 c)
        {
            porteur.Remove(x, y, z); lueur.Set(x, y, z, c);
        }

        /// Repeint selon un bruit déterministe : au-dessus du seuil, couleur « b ».
        static void Taches(VoxelModel m, Color32 depuis, Color32 b, float seuil, int graine)
        {
            m.Paint((x, y, z, c) => (c.r == depuis.r && c.g == depuis.g && c.b == depuis.b && Pal.Hash01(x + graine, y * 3 + graine, z) > seuil) ? b : c);
        }

        static bool Meme(Color32 a, Color32 b) { return a.r == b.r && a.g == b.g && a.b == b.b; }
    }
}

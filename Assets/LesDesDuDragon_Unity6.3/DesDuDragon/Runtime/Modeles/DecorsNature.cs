// Les Dés du Dragon — décors de nature : aiguillage, ids par zone et outils de sculpture communs
// (masses de feuillage étagées, troncs à écorce en relief, rochers à facettes, prismes taillés).
// Les modèles eux-mêmes sont dans DecorsNature.Arbres.cs, .Roches.cs, .Plantes.cs, .Ombres.cs.
using System;
using System.Collections.Generic;
using UnityEngine;
using DesDuDragon.Regles;
using DesDuDragon.Voxel;

namespace DesDuDragon.Modeles
{
    public static partial class DecorsModeles
    {
        /// Ids semés au hasard dans chaque zone (terrain, eau et cases sont faits ailleurs).
        public static string[] DeZone(Zone z)
        {
            switch (z)
            {
                case Zone.Village: return new[] { "chene", "buisson", "fleurs", "rocher", "herbes_hautes" };
                case Zone.Bois: return new[] { "chene", "pin", "buisson", "champignon", "souche", "fougere", "rocher" };
                case Zone.Marais: return new[] { "saule", "arbre_mort", "roseaux", "champignon_marais", "rocher_mousse", "feu_follet" };
                case Zone.Cryptes: return new[] { "arbre_mort", "tombe", "croix_pierre", "os", "rocher", "cierge" };
                case Zone.Pics: return new[] { "rocher", "pic_rocheux", "pin_brule", "lave", "cristal_braise", "os" };
                default: return new[] { "rocher", "stalagmite", "os", "tas_or", "cristal_braise" };
            }
        }

        /// Un élément de nature, ou null si l'id n'en est pas un.
        public static VoxelRig ConstruireNature(string id, int graine)
        {
            var r = new System.Random(graine * 7919 + id.Length * 131 + (int)id[0] * 17);
            switch (id)
            {
                case "chene": return Chene(r);
                case "pin": return Pin(r);
                case "saule": return Saule(r);
                case "arbre_mort": return ArbreMort(r);
                case "pin_brule": return PinBrule(r);
                case "souche": return Souche(r);
                case "buisson": return Buisson(r);
                case "rocher": return Rocher(r, false);
                case "rocher_mousse": return Rocher(r, true);
                case "pic_rocheux": return PicRocheux(r);
                case "stalagmite": return Stalagmite(r);
                case "lave": return Lave(r);
                case "cristal_braise": return CristalBraise(r);
                case "fleurs": return Fleurs(r);
                case "herbes_hautes": return HerbesHautes(r);
                case "champignon": return Champignon(r);
                case "champignon_marais": return ChampignonMarais(r);
                case "fougere": return Fougere(r);
                case "roseaux": return Roseaux(r);
                case "tombe": return Tombe(r);
                case "croix_pierre": return CroixPierre(r);
                case "os": return Os(r);
                case "cierge": return Cierge(r);
                case "tas_or": return TasOr(r);
                case "feu_follet": return FeuFollet(r);
            }
            return null;
        }

        // ------------------------------------------------------------------ hasard

        static float NaF(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }
        static int NaI(System.Random r, int a, int b) { return r.Next(a, b + 1); }   // bornes incluses
        static bool NaP(System.Random r, float p) { return r.NextDouble() < p; }

        /// Teinte légèrement décalée (luminosité ± amp, petite dérive vers le chaud ou le froid).
        static Color32 NaTeinte(Color32 c, System.Random r, float amp)
        {
            float f = 1f + NaF(r, -amp, amp), d = NaF(r, -amp, amp) * 0.6f;
            return new Color32((byte)Mathf.Clamp(c.r * f * (1 + d), 0, 255), (byte)Mathf.Clamp(c.g * f, 0, 255),
                (byte)Mathf.Clamp(c.b * f * (1 - d), 0, 255), 255);
        }

        // ------------------------------------------------------------------ outils de modèle

        static bool NaEgal(Color32 a, Color32 b) { return a.r == b.r && a.g == b.g && a.b == b.b; }

        /// Éclaircit les voxels à ciel ouvert, assombrit ceux qui regardent le sol (lecture des volumes de loin).
        static void NaDessusDessous(VoxelModel m, float dessus, float dessous)
        {
            m.Paint((x, y, z, c) =>
            {
                if (!m.Has(x, y + 1, z)) return Pal.Shade(c, dessus);
                if (!m.Has(x, y - 1, z) && y > 0) return Pal.Shade(c, dessous);
                return c;
            });
        }

        /// Retire tout ce qui passe sous le sol.
        static void NaSol(VoxelModel m) { m.Carve((x, y, z) => y < 0); }

        static VoxelRig NaRig(string id, float taille)
        {
            return new VoxelRig { Name = id, VoxelSize = taille, Origin = Vector3.zero };
        }

        // ------------------------------------------------------------------ feuillage

        /// Masse de feuillage taillée : sections octogonales, étagée par paliers de « marche » voxels, dessous aplati.
        static void NaMasse(VoxelModel m, float cx, float cy, float cz, float rx, float ry, float rz, int marche, float rot, Color32 c)
        {
            int y0 = (int)Math.Floor(cy - ry * 0.75f), y1 = (int)Math.Ceiling(cy + ry);
            float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            for (int y = y0; y <= y1; y++)
            {
                // palier : la hauteur est arrondie à la marche, d'où des terrasses nettes
                float yq = (float)Math.Floor((y - cy) / marche + 0.5f) * marche;
                float t = yq >= 0 ? yq / ry : yq / (ry * 0.75f);
                if (t > 1f || t < -1f) continue;
                float s = Mathf.Sqrt(1f - t * t);
                if (t < 0) s = Mathf.Min(1f, s * 1.08f);
                int xa = (int)Math.Floor(cx - rx - 1), xb = (int)Math.Ceiling(cx + rx + 1);
                int za = (int)Math.Floor(cz - rz - 1), zb = (int)Math.Ceiling(cz + rz + 1);
                for (int x = xa; x <= xb; x++) for (int z = za; z <= zb; z++)
                {
                    float dx = x + 0.5f - cx, dz = z + 0.5f - cz;
                    float u = Mathf.Abs((dx * cs + dz * sn) / rx), v = Mathf.Abs((-dx * sn + dz * cs) / rz);
                    float d = Mathf.Max(Mathf.Max(u, v), (u + v) * 0.74f);
                    if (d <= s) m.Set(x, y, z, c);
                }
            }
        }

        /// Peint un feuillage : dessus clair, dessous sombre, quelques touffes d'un second ton.
        static void NaPeindreFeuillage(VoxelModel m, Color32 clair, Color32 sombre, Color32 touffe, int graine)
        {
            m.Paint((x, y, z, c) =>
            {
                bool haut = !m.Has(x, y + 1, z), bas = !m.Has(x, y - 1, z);
                float h = Pal.Hash01(x / 2 + graine, y / 2, z / 2);
                if (haut) return h < 0.3f ? Pal.Mix(clair, touffe, 0.5f) : clair;
                if (bas) return sombre;
                if (h < 0.18f) return touffe;
                return c;
            });
        }

        // ------------------------------------------------------------------ bois

        /// Tronc vertical taillé : centre qui ondule, évasement au pied, écorce à côtes (sillons creusés, deux tons).
        /// Rend le centre du tronc à la hauteur yHaut.
        static Vector3 NaTronc(VoxelModel m, System.Random r, int yHaut, float r0, float r1, float penteX, float penteZ,
            float ondule, int cotes, Color32 ecorce, Color32 sillon)
        {
            float ph = NaF(r, 0, 6.28f), tor = NaF(r, -0.04f, 0.04f);
            Vector3 c = Vector3.zero;
            for (int y = 0; y <= yHaut; y++)
            {
                float t = y / (float)yHaut;
                float cx = penteX * y + Mathf.Sin(y * 0.21f + ph) * ondule, cz = penteZ * y + Mathf.Cos(y * 0.17f + ph) * ondule;
                float rr = Mathf.Lerp(r0, r1, t) * (1f + 0.55f * Mathf.Exp(-y / 2.2f));
                int xa = (int)Math.Floor(cx - rr - 1), xb = (int)Math.Ceiling(cx + rr + 1);
                int za = (int)Math.Floor(cz - rr - 1), zb = (int)Math.Ceiling(cz + rr + 1);
                for (int x = xa; x <= xb; x++) for (int z = za; z <= zb; z++)
                {
                    float dx = x + 0.5f - cx, dz = z + 0.5f - cz;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d > rr) continue;
                    float ang = Mathf.Atan2(dz, dx) / 6.2832f + y * tor + Pal.Hash01(0, y / 3, 7) * 0.08f;
                    float g = ang * cotes - (float)Math.Floor(ang * cotes);
                    bool bord = d > rr - 1.1f;
                    if (bord && rr > 2.2f && g < 0.22f) continue;              // sillon creusé
                    m.Set(x, y, z, (bord && g < 0.42f) || !bord ? sillon : ecorce);
                }
                c = new Vector3(cx, y, cz);
            }
            return c;
        }

        /// Racines : traits effilés qui partent du tronc et plongent dans le sol.
        static void NaRacines(VoxelModel m, System.Random r, int n, float rTronc, float longueur, Color32 c)
        {
            float a0 = NaF(r, 0, 6.28f);
            for (int i = 0; i < n; i++)
            {
                float a = a0 + i * 6.2832f / n + NaF(r, -0.35f, 0.35f);
                float L = longueur * NaF(r, 0.7f, 1.15f), ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                float hy = NaF(r, 2.5f, 4.5f) * rTronc / 2.5f;
                m.Line(ca * rTronc * 0.4f, hy, sa * rTronc * 0.4f, ca * (rTronc + L * 0.5f), hy * 0.35f, sa * (rTronc + L * 0.5f), rTronc * 0.55f, rTronc * 0.38f, c);
                m.Line(ca * (rTronc + L * 0.5f), hy * 0.35f, sa * (rTronc + L * 0.5f), ca * (rTronc + L), -0.5f, sa * (rTronc + L), rTronc * 0.38f, 0.6f, c);
            }
        }

        /// Écorce striée sur des branches (traits) : sillons verticaux courts, deux tons.
        static void NaStrier(VoxelModel m, Color32 ecorce, Color32 sillon, int graine)
        {
            m.Paint((x, y, z, c) =>
            {
                float h = Pal.Hash01(x, (y + (int)(Pal.Hash01(x, graine, z) * 5)) / 4, z);
                return h < 0.38f ? sillon : ecorce;
            });
        }

        // ------------------------------------------------------------------ pierre

        /// Rocher à facettes : un ellipsoïde tranché par des plans au hasard ; chaque facette a son ton, strates inclinées.
        static void NaRoche(VoxelModel m, System.Random r, Vector3 c, Vector3 rad, int nPlans, Color32[] tons, float pasStrate, Color32 strate)
        {
            var normales = new Vector3[nPlans]; var dist = new float[nPlans];
            // des facettes plutôt latérales, bien réparties autour de l'axe
            float a0 = NaF(r, 0, 6.28f);
            for (int k = 0; k < nPlans; k++)
            {
                float ak = a0 + k * 6.2832f / Math.Max(1, nPlans - 1) + NaF(r, -0.3f, 0.3f);
                var n = new Vector3(Mathf.Cos(ak), NaF(r, -0.2f, 0.9f), Mathf.Sin(ak));
                if (k == 0) n = new Vector3(NaF(r, -0.3f, 0.3f), 1, NaF(r, -0.3f, 0.3f));   // un dessus à peu près plat
                normales[k] = n.normalized; dist[k] = k == 0 ? NaF(r, 0.42f, 0.62f) : NaF(r, 0.52f, 0.7f);
            }
            float incX = NaF(r, -0.25f, 0.25f), incZ = NaF(r, -0.25f, 0.25f);
            int x0 = (int)Math.Floor(c.x - rad.x), x1 = (int)Math.Ceiling(c.x + rad.x);
            int y0 = Math.Max(0, (int)Math.Floor(c.y - rad.y)), y1 = (int)Math.Ceiling(c.y + rad.y);
            int z0 = (int)Math.Floor(c.z - rad.z), z1 = (int)Math.Ceiling(c.z + rad.z);
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
            {
                var q = new Vector3((x + 0.5f - c.x) / rad.x, (y + 0.5f - c.y) / rad.y, (z + 0.5f - c.z) / rad.z);
                if (q.sqrMagnitude > 1f) continue;
                int face = -1; float meilleur = -9f; bool dedans = true;
                for (int k = 0; k < nPlans; k++)
                {
                    float e = Vector3.Dot(q, normales[k]) - dist[k];
                    if (e > 0) { dedans = false; break; }
                    if (e > meilleur) { meilleur = e; face = k; }
                }
                if (!dedans) continue;
                var col = tons[(face + 1) % tons.Length];
                if (pasStrate > 0)
                {
                    float s = (y + incX * x + incZ * z) / pasStrate;
                    if (s - (float)Math.Floor(s) < 0.22f) col = Pal.Mix(col, strate, 0.35f);
                }
                m.Set(x, y, z, col);
            }
        }

        /// Prisme taillé à K pans entre a et b (rayon r0 → r1), orienté librement. Chaque pan prend un ton de « tons ».
        static void NaPrisme(VoxelModel m, Vector3 a, Vector3 b, float r0, float r1, int K, float rot, Color32[] tons)
        {
            var ax = b - a; float L = ax.magnitude; if (L < 0.01f) return; var u = ax / L;
            var e1 = Vector3.Cross(u, Mathf.Abs(u.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var e2 = Vector3.Cross(u, e1);
            float rm = Mathf.Max(r0, r1) + 1;
            int xa = (int)Math.Floor(Mathf.Min(a.x, b.x) - rm), xb = (int)Math.Ceiling(Mathf.Max(a.x, b.x) + rm);
            int ya = (int)Math.Floor(Mathf.Min(a.y, b.y) - rm), yb = (int)Math.Ceiling(Mathf.Max(a.y, b.y) + rm);
            int za = (int)Math.Floor(Mathf.Min(a.z, b.z) - rm), zb = (int)Math.Ceiling(Mathf.Max(a.z, b.z) + rm);
            for (int x = xa; x <= xb; x++) for (int y = ya; y <= yb; y++) for (int z = za; z <= zb; z++)
            {
                var p = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) - a;
                float t = Vector3.Dot(p, u) / L;
                if (t < 0 || t > 1) continue;
                var q = p - u * (t * L);
                float qx = Vector3.Dot(q, e1), qy = Vector3.Dot(q, e2), rr = Mathf.Lerp(r0, r1, t);
                int face = 0; float meilleur = -99f; bool dedans = true;
                for (int k = 0; k < K; k++)
                {
                    float ph = rot + k * 6.2832f / K;
                    float e = qx * Mathf.Cos(ph) + qy * Mathf.Sin(ph) - rr;
                    if (e > 0) { dedans = false; break; }
                    if (e > meilleur) { meilleur = e; face = k; }
                }
                if (dedans) m.Set(x, y, z, tons[face % tons.Length]);
            }
        }
    }
}

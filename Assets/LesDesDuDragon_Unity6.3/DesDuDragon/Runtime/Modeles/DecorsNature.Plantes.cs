// Les Dés du Dragon — petite végétation : buisson et souche (0,1 m) ; fleurs, herbes hautes, champignons, fougère,
// roseaux (0,05 m).
using System;
using UnityEngine;
using DesDuDragon.Voxel;

namespace DesDuDragon.Modeles
{
    public static partial class DecorsModeles
    {
        // ------------------------------------------------------------------ buisson (0,8 – 1,2 m)

        static VoxelRig Buisson(System.Random r)
        {
            var rig = NaRig("buisson", 0.1f);
            var pied = new VoxelModel(); var feu = new VoxelModel();
            float H = NaF(r, 8, 10.5f), L = H * NaF(r, 0.72f, 0.92f);
            float t = NaF(r, 0, 1);
            var vBase = NaTeinte(Pal.Mix(Pal.C("527e36"), Pal.C("386638"), t), r, 0.05f);
            var vClair = Pal.Mix(vBase, Pal.C("94b058"), 0.35f);
            var vSombre = Pal.Shade(vBase, 0.6f);
            var vTouffe = Pal.Mix(vBase, Pal.C("2f5a34"), 0.5f);
            int n = 3; float a0 = NaF(r, 0, 6.28f);
            NaMasse(feu, 0, H * 0.55f, 0, L * 0.5f, H * 0.45f, L * 0.5f, 2, NaF(r, 0, 1.5f), vBase);
            for (int i = 0; i < n; i++)
            {
                float a = a0 + i * 6.2832f / n + NaF(r, -0.4f, 0.4f), d = L * NaF(r, 0.35f, 0.5f);
                NaMasse(feu, Mathf.Cos(a) * d, H * NaF(r, 0.3f, 0.45f), Mathf.Sin(a) * d, L * NaF(r, 0.34f, 0.44f), H * NaF(r, 0.28f, 0.36f), L * NaF(r, 0.34f, 0.44f), 2, NaF(r, 0, 1.5f), vBase);
            }
            NaSol(feu);
            NaPeindreFeuillage(feu, vClair, vSombre, vTouffe, r.Next(100));
            // baies (rouges, ou fleurs blanches au village) piquées sur la surface
            bool baies = NaP(r, 0.6f);
            var cBaie = baies ? Pal.C("b8392f") : Pal.C("ece4d0");
            foreach (var kv in feu.Clone().Tous())
            {
                var p = kv.Key;
                if (p.y > 2 && !feu.Has(p.x, p.y + 1, p.z) && Pal.Hash01(p.x, p.y, p.z + 3) < 0.035f) feu.Set(p.x, p.y + 1, p.z, cBaie);
            }
            // tiges visibles au pied
            for (int i = 0; i < 3; i++) { float a = NaF(r, 0, 6.28f); pied.Line(0, 0, 0, Mathf.Cos(a) * 2, 3, Mathf.Sin(a) * 2, 0.6f, NaEcorceSombre); }

            rig.Add("corps", pied, Vector3.zero);
            rig.Add("feuillage", feu, new Vector3(0, 1, 0), "corps");
            return rig;
        }

        // ------------------------------------------------------------------ souche (0,1 m)

        static VoxelRig Souche(System.Random r)
        {
            var rig = NaRig("souche", 0.1f);
            var m = new VoxelModel();
            int H = NaI(r, 5, 8);
            var ecorce = NaTeinte(NaEcorce, r, 0.06f);
            NaTronc(m, r, H, 3.4f, 3.0f, 0, 0, 0.2f, 8, ecorce, NaEcorceSombre);
            NaRacines(m, r, NaI(r, 4, 5), 2.0f, NaF(r, 2, 3f), ecorce);
            // coupe en biais, cernes au sommet, cœur un peu creusé
            float cx = NaF(r, -0.3f, 0.3f), cz = NaF(r, -0.3f, 0.3f);
            m.Carve((x, y, z) => y > H - 2 + cx * x + cz * z);
            Color32 bois = Pal.C("c8a070"), cerne = Pal.C("9a7448");
            m.Paint((x, y, z, c) =>
            {
                if (m.Has(x, y + 1, z)) return c;
                float d = Mathf.Sqrt((x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f));
                if (d > 3.0f) return c;
                return ((int)d) % 2 == 0 ? bois : cerne;
            });
            // un champignon de souche ou de la mousse
            if (NaP(r, 0.6f))
            {
                float a = NaF(r, 0, 6.28f); int x = (int)(Mathf.Cos(a) * 3.4f), z = (int)(Mathf.Sin(a) * 3.4f);
                m.Box(x - 1, H / 2, z - 1, x + 1, H / 2, z + 1, Pal.C("d8b07a"));
            }
            else m.Paint((x, y, z, c) => !m.Has(x, y + 1, z) && y < 3 && Pal.Hash01(x, y, z) < 0.7f ? Pal.C("5a7a34") : c);
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ fleurs (0,05 m)

        static VoxelRig Fleurs(System.Random r)
        {
            var rig = NaRig("fleurs", 0.05f);
            var m = new VoxelModel();
            var couleurs = new[] { Pal.C("e8c33a"), Pal.C("e8e2d4"), Pal.C("c75a8a"), Pal.C("8a6ac8"), Pal.C("d9573b"), Pal.C("6a8ad8") };
            var c1 = couleurs[r.Next(couleurs.Length)]; var c2 = couleurs[r.Next(couleurs.Length)];
            var tige = NaTeinte(Pal.C("5b8a36"), r, 0.08f); var feuille = Pal.Shade(tige, 1.12f);
            // touffe de feuilles au sol
            for (int i = 0; i < 6; i++)
            {
                float a = NaF(r, 0, 6.28f), L = NaF(r, 2, 4);
                m.Line(0, 0, 0, Mathf.Cos(a) * L, NaF(r, 1, 2), Mathf.Sin(a) * L, 0.7f, feuille);
            }
            int n = NaI(r, 5, 9);
            for (int i = 0; i < n; i++)
            {
                float a = NaF(r, 0, 6.28f), d = NaF(r, 0, 4.5f), h = NaF(r, 5, 10);
                int x = (int)Math.Round(Mathf.Cos(a) * d), z = (int)Math.Round(Mathf.Sin(a) * d), y = (int)h;
                m.Line(0, 0, 0, x + 0.5f, y, z + 0.5f, 0.5f, tige);
                var c = NaP(r, 0.65f) ? c1 : c2;
                // corolle en croix, cœur jaune
                m.Set(x, y, z, Pal.C("f0c040"));
                m.Set(x + 1, y, z, c); m.Set(x - 1, y, z, c); m.Set(x, y, z + 1, c); m.Set(x, y, z - 1, c);
                if (NaP(r, 0.5f)) m.Set(x, y + 1, z, Pal.Shade(c, 1.1f));
            }
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ herbes hautes (0,05 m)

        static VoxelRig HerbesHautes(System.Random r)
        {
            var rig = NaRig("herbes_hautes", 0.05f);
            var m = new VoxelModel();
            // vertes au village, paille morte (la graine en décide un tiers du temps, pour les cryptes)
            bool morte = NaP(r, 0.33f);
            var b1 = morte ? Pal.C("a8996a") : NaTeinte(Pal.C("7ea446"), r, 0.06f);
            var b2 = morte ? Pal.C("837650") : Pal.Shade(b1, 0.78f);
            var epi = morte ? Pal.C("c4b48a") : Pal.C("c8c070");
            int n = NaI(r, 14, 22);
            for (int i = 0; i < n; i++)
            {
                float a = NaF(r, 0, 6.28f), d = NaF(r, 0, 4f), h = NaF(r, 8, 16);
                float x0 = Mathf.Cos(a) * d, z0 = Mathf.Sin(a) * d;
                float pa = NaF(r, 0, 6.28f), pl = NaF(r, 1, 4);
                float xm = x0 + Mathf.Cos(pa) * pl * 0.4f, zm = z0 + Mathf.Sin(pa) * pl * 0.4f;
                float x1 = x0 + Mathf.Cos(pa) * pl, z1 = z0 + Mathf.Sin(pa) * pl;
                var c = NaP(r, 0.5f) ? b1 : b2;
                m.Line(x0, 0, z0, xm, h * 0.6f, zm, 0.5f, c);
                m.Line(xm, h * 0.6f, zm, x1, h, z1, 0.45f, Pal.Shade(c, 1.08f));
                if (NaP(r, 0.35f)) { m.Set((int)Math.Floor(x1), (int)h, (int)Math.Floor(z1), epi); m.Set((int)Math.Floor(x1), (int)h + 1, (int)Math.Floor(z1), epi); }
            }
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ champignons du bois (0,05 m)

        static void NaChapeau(VoxelModel m, float x, float y, float z, float rayon, float h, Color32 c, Color32 dessous, Color32 point, int graine)
        {
            NaMasse(m, x, y, z, rayon, h, rayon, 1, 0.4f, c);
            m.Carve((xx, yy, zz) => yy < (int)y && (xx - x) * (xx - x) + (zz - z) * (zz - z) < (rayon + 2) * (rayon + 2) && yy >= (int)(y - h));
            m.Paint((xx, yy, zz, cc) => NaEgal(cc, c) && !m.Has(xx, yy - 1, zz) ? dessous : cc);
            m.Paint((xx, yy, zz, cc) => NaEgal(cc, c) && !m.Has(xx, yy + 1, zz) && Pal.Hash01(xx, graine, zz) < 0.12f ? point : cc);
        }

        static VoxelRig Champignon(System.Random r)
        {
            var rig = NaRig("champignon", 0.05f);
            var m = new VoxelModel();
            int sorte = r.Next(3);
            Color32 ch, pt;
            if (sorte == 0) { ch = Pal.C("b8392f"); pt = Pal.C("efe6d2"); }           // amanite
            else if (sorte == 1) { ch = Pal.C("9a6a3c"); pt = Pal.C("b88a54"); }      // cèpe
            else { ch = Pal.C("6a5aa0"); pt = Pal.C("a898d8"); }                      // champignon des murmures
            ch = NaTeinte(ch, r, 0.06f);
            var pied = Pal.C("e6dcc3"); var lames = Pal.C("cbbd9c");
            int n = NaI(r, 1, 3);
            for (int i = 0; i < n; i++)
            {
                float a = NaF(r, 0, 6.28f), d = i == 0 ? 0 : NaF(r, 3, 5);
                float x = Mathf.Cos(a) * d, z = Mathf.Sin(a) * d;
                float h = i == 0 ? NaF(r, 6, 9) : NaF(r, 3, 6), rp = i == 0 ? 1.4f : 1f;
                m.Cylinder(x, z, 0, (int)h, rp, pied);
                m.Cylinder(x, z, 0, 1, rp + 0.7f, pied);
                NaChapeau(m, x, h + 0.5f, z, h * NaF(r, 0.55f, 0.75f), h * 0.42f, ch, lames, pt, r.Next(100));
            }
            // mousse au pied
            for (int i = 0; i < 6; i++) { float a = NaF(r, 0, 6.28f), d = NaF(r, 1, 5); m.Set((int)(Mathf.Cos(a) * d), 0, (int)(Mathf.Sin(a) * d), Pal.C("4f7a34")); }
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ champignons du marais (0,05 m, plus grands, livides)

        static VoxelRig ChampignonMarais(System.Random r)
        {
            var rig = NaRig("champignon_marais", 0.05f);
            var m = new VoxelModel(); var lueur = new VoxelModel();
            var ch = NaTeinte(Pal.C("7e8a4a"), r, 0.08f);
            var pt = Pal.C("b4be6a"); var pied = Pal.C("b8b090"); var lames = Pal.C("8a8a5a");
            int n = NaI(r, 2, 4);
            for (int i = 0; i < n; i++)
            {
                float a = NaF(r, 0, 6.28f), d = i == 0 ? 0 : NaF(r, 3.5f, 6);
                float x = Mathf.Cos(a) * d, z = Mathf.Sin(a) * d;
                float h = i == 0 ? NaF(r, 10, 15) : NaF(r, 4, 8);
                float pa = NaF(r, 0, 6.28f), pp = NaF(r, 0.5f, 2f);
                float xh = x + Mathf.Cos(pa) * pp, zh = z + Mathf.Sin(pa) * pp;
                m.Line(x, 0, z, xh, h, zh, 1.4f, 1.0f, pied);
                // chapeau plat en ombrelle
                NaChapeau(m, xh, h + 0.5f, zh, h * NaF(r, 0.42f, 0.6f), h * 0.22f, ch, lames, pt, r.Next(100));
                // de petites pustules phosphorescentes sur le grand
                if (i == 0)
                    foreach (var kv in m.Clone().Tous())
                    {
                        var p = kv.Key;
                        if (p.y > h && !m.Has(p.x, p.y + 1, p.z) && Pal.Hash01(p.x, p.y, p.z + 11) < 0.06f) lueur.Set(p.x, p.y + 1, p.z, Pal.C("b8f070"));
                    }
            }
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            if (lueur.Count > 0) rig.Add("lueurSpores", lueur, Vector3.zero, "corps");
            return rig;
        }

        // ------------------------------------------------------------------ fougère (0,05 m)

        static VoxelRig Fougere(System.Random r)
        {
            var rig = NaRig("fougere", 0.05f);
            var m = new VoxelModel();
            var v1 = NaTeinte(Pal.C("4f8a3e"), r, 0.07f); var v2 = Pal.Shade(v1, 0.75f); var tige = Pal.C("4a6a2a");
            int n = NaI(r, 6, 9); float a0 = NaF(r, 0, 6.28f);
            for (int i = 0; i < n; i++)
            {
                float a = a0 + i * 6.2832f / n + NaF(r, -0.2f, 0.2f), L = NaF(r, 9, 14), hmax = NaF(r, 6, 10);
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                // fronde : une nervure en arc, des folioles alternées de plus en plus courtes
                Vector3 prec = Vector3.zero;
                for (int k = 0; k <= 10; k++)
                {
                    float t = k / 10f, dist = t * L, y = hmax * Mathf.Sin(t * 2.6f) * (1 - 0.3f * t) + 0.5f;
                    var p = new Vector3(ca * dist, y, sa * dist);
                    if (k > 0) m.Line(prec.x, prec.y, prec.z, p.x, p.y, p.z, 0.5f, tige);
                    if (k >= 2 && k < 10)
                    {
                        float fl = (1 - t) * 3.5f + 0.6f;
                        float cote = (k % 2 == 0) ? 1 : -1;
                        m.Line(p.x, p.y, p.z, p.x - sa * fl * cote + ca * 0.8f, p.y - 0.4f, p.z + ca * fl * cote + sa * 0.8f, 0.55f, k % 3 == 0 ? v2 : v1);
                        m.Line(p.x, p.y, p.z, p.x + sa * fl * cote + ca * 0.8f, p.y - 0.4f, p.z - ca * fl * cote + sa * 0.8f, 0.55f, v1);
                    }
                    prec = p;
                }
            }
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ roseaux (0,05 m, peuvent pousser dans l'eau)

        static VoxelRig Roseaux(System.Random r)
        {
            var rig = NaRig("roseaux", 0.05f);
            var m = new VoxelModel();
            var v1 = NaTeinte(Pal.C("7a8a44"), r, 0.07f); var v2 = Pal.C("9a9a58"); var massette = Pal.C("6a4228");
            int n = NaI(r, 9, 15);
            for (int i = 0; i < n; i++)
            {
                float a = NaF(r, 0, 6.28f), d = NaF(r, 0, 4.5f), h = NaF(r, 14, 26);
                float x0 = Mathf.Cos(a) * d, z0 = Mathf.Sin(a) * d;
                float pa = NaF(r, 0, 6.28f), pl = NaF(r, 0.5f, 3f);
                float x1 = x0 + Mathf.Cos(pa) * pl, z1 = z0 + Mathf.Sin(pa) * pl;
                m.Line(x0, -2, z0, x1, h, z1, 0.55f, NaP(r, 0.6f) ? v1 : v2);
                if (NaP(r, 0.45f))
                {
                    // massette brune au bout
                    int mx = (int)Math.Floor(x1), mz = (int)Math.Floor(z1), my = (int)h - 4;
                    m.Box(mx, my, mz, mx + 1, my + 3, mz + 1, massette);
                    m.Set(mx, my + 4, mz, v2);
                }
                else if (NaP(r, 0.5f))
                {
                    // une feuille qui retombe
                    float y = h * NaF(r, 0.4f, 0.6f);
                    m.Line(x0 + (x1 - x0) * 0.5f, y, z0 + (z1 - z0) * 0.5f, x1 + Mathf.Cos(pa + 1.4f) * 5, y - 3, z1 + Mathf.Sin(pa + 1.4f) * 5, 0.5f, v1);
                }
            }
            m.Carve((x, y, z) => y < -2);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }
    }
}

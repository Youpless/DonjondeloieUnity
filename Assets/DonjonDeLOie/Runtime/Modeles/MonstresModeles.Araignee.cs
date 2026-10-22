// Donjon de l'Oie — l'araignée géante et la gelée verte (Bois et Marais).
using System;
using UnityEngine;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static partial class MonstresModeles
    {
        // ------------------------------------------------------------------ araignée géante (~1,35 m d'envergure, voxel 0,05)
        static VoxelRig Araignee()
        {
            var r = NouveauRig("araignee", 0.05f);
            Color32 chit = C("3b3230"), chitS = C("241e1e"), chitC = C("5e4c44"), poil = C("6a5546"), motif = C("c2482e"), motifC = C("e28a3a");
            Color32 croc = C("d9cdb0"), crocS = C("8f2a22"), oeil = C("ff3a2a");

            var corps = new VoxelModel();
            // Abdomen : gros, en poire facettée, relevé vers l'arrière.
            ProfilZ(corps, 0, new float[] { -21, 12, 2.5f, 2.5f, -19, 12.5f, 5.5f, 5, -15, 12.5f, 7.6f, 7, -10, 11.5f, 7.4f, 6.2f, -6, 10, 5, 4, -4, 9, 2.6f, 2.4f }, 0.5f, chit);
            // motif en sablier/crâne sur le dos de l'abdomen
            corps.Paint((x, y, z, c) =>
            {
                if (!Meme(c, chit) || y < 14) return c;
                float ax = Math.Abs(x + 0.5f), dz = z + 12.5f;
                if (ax < 1.5f && dz > -7 && dz < 5) return motif;                         // trait médian
                if (Math.Abs(dz + 1) < 1.2f && ax < 4.8f) return motif;                     // chevron
                if (Math.Abs(dz - 3.5f + ax * 0.6f) < 0.9f && ax < 4) return motifC;         // V avant
                if (Math.Abs(dz + 5.5f - ax * 0.5f) < 0.9f && ax < 4.5f) return motif;
                return c;
            });
            corps.Paint((x, y, z, c) => (Meme(c, chit) && y <= 8) ? chitC : c);
            // poils raides sur l'abdomen
            var rnd = new System.Random(11);
            for (int i = 0; i < 26; i++)
            {
                int x = rnd.Next(-7, 7), z = rnd.Next(-19, -6);
                int h = Haut(corps, x, z, 0, 30); if (h == int.MinValue) continue;
                corps.Line(x + 0.5f, h + 0.5f, z + 0.5f, x + 0.5f + (x + 0.5f) * 0.25f, h + 2.2f, z - 1.2f, 0.5f, poil);
            }
            // Céphalothorax : plaque bombée et anguleuse.
            ProfilZ(corps, 0, new float[] { -5, 9.2f, 3, 2.5f, -3, 9.5f, 4.8f, 3.4f, 1, 9.5f, 5, 3.2f, 4, 9, 4, 2.6f, 5, 8.6f, 3, 2 }, 0.45f, chitS);
            corps.Paint((x, y, z, c) => (Meme(c, chitS) && y >= 12) ? chit : c);
            r.Add("corps", corps, V(0, 9, -2));

            // Tête et yeux : 8 yeux en grappe (2 gros, 6 petits).
            var tete = new VoxelModel();
            ProfilZ(tete, 0, new float[] { 4, 9, 3.2f, 2.6f, 7, 9.2f, 2.8f, 2.6f, 8, 9, 2, 2 }, 0.5f, chitS);
            var lueur = new VoxelModel();
            int[,] yeux = { { -2, 10, 8 }, { 1, 10, 8 }, { -1, 11, 8 }, { 0, 11, 8 }, { -3, 11, 7 }, { 2, 11, 7 }, { -2, 12, 6 }, { 1, 12, 6 } };
            for (int i = 0; i < 8; i++) { tete.Remove(yeux[i, 0], yeux[i, 1], yeux[i, 2]); lueur.Set(yeux[i, 0], yeux[i, 1], yeux[i, 2], oeil); }
            lueur.Set(-2, 10, 9, oeil); lueur.Set(1, 10, 9, oeil);
            r.Add("tete", tete, V(0, 9, 4), "corps");
            r.Add("lueur_yeux", lueur, V(0, 9, 4), "tete");

            // Chélicères et crochets (machoire), pédipalpes
            var mach = new VoxelModel();
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1 : 1;
                Chaine(mach, new float[] { sx * 1.3f, 8.5f, 8, sx * 1.5f, 6.5f, 10, sx * 1.2f, 4.8f, 10.5f }, 1.3f, 1, chit);
                Chaine(mach, new float[] { sx * 1.2f, 4.8f, 10.5f, sx * 0.6f, 3.2f, 10, sx * 0.3f, 2.6f, 8.8f }, 0.7f, 0.45f, croc);
                mach.Set(s == 0 ? -1 : 0, 4, 11, crocS);
                Chaine(mach, new float[] { sx * 2.8f, 8, 7, sx * 4, 9, 10, sx * 3.4f, 6, 12 }, 0.8f, 0.6f, chitC);
            }
            Soustraire(tete, mach);
            r.Add("machoire", mach, V(0, 8.5f, 8), "tete");

            // Pattes : 4 de chaque côté, segments anguleux (coxa → fémur levé → genou haut → tibia → tarse au sol).
            // Avant : paires 1-2 ; arrière : paires 3-4.
            float[] angles = { 38, 12, -16, -44 };          // direction horizontale (degrés depuis +x vers +z)
            float[] portee = { 24, 22, 21, 23 };
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1 : 1;
                for (int grp = 0; grp < 2; grp++)
                {
                    var p = new VoxelModel();
                    for (int k = grp * 2; k < grp * 2 + 2; k++)
                    {
                        float a = angles[k] * Mathf.Deg2Rad;
                        float dx = (float)Math.Cos(a) * sx, dz = (float)Math.Sin(a);
                        float bx = sx * 3.2f, bz = 2.5f - k * 1.6f;
                        float R = portee[k];
                        var p0 = V(bx, 8.5f, bz);
                        var p1 = V(bx + dx * R * 0.18f, 11.5f, bz + dz * R * 0.18f);
                        var p2 = V(bx + dx * R * 0.48f, 18f - k * 0.4f, bz + dz * R * 0.48f);    // genou haut
                        var p3 = V(bx + dx * R * 0.8f, 7, bz + dz * R * 0.8f);
                        var p4 = V(bx + dx * R, 0.5f, bz + dz * R);
                        p.Line(p0.x, p0.y, p0.z, p1.x, p1.y, p1.z, 1.4f, 1.3f, chitS);
                        p.Line(p1.x, p1.y, p1.z, p2.x, p2.y, p2.z, 1.4f, 1.1f, chit);
                        p.Line(p2.x, p2.y, p2.z, p3.x, p3.y, p3.z, 1.1f, 0.8f, chit);
                        p.Line(p3.x, p3.y, p3.z, p4.x, p4.y, p4.z, 0.8f, 0.5f, chitS);
                        p.Set((int)Math.Floor(p2.x), (int)Math.Floor(p2.y), (int)Math.Floor(p2.z), motif);   // genou rouge
                        p.Set((int)Math.Floor(p2.x), (int)Math.Floor(p2.y) + 1, (int)Math.Floor(p2.z), chitC);
                        // bandes claires aux articulations
                        var b3 = Vector3.Lerp(p2, p3, 0.15f);
                        p.Set((int)Math.Floor(b3.x), (int)Math.Floor(b3.y), (int)Math.Floor(b3.z), motifC);
                    }
                    string nom = grp == 0 ? (s == 0 ? "pattesAvG" : "pattesAvD") : (s == 0 ? "pattesArG" : "pattesArD");
                    r.Add(nom, p, V(sx * 3.2f, 8.5f, grp == 0 ? 1.7f : -1.5f), "corps");
                }
            }
            return Fin(r);
        }

        // ------------------------------------------------------------------ gelée verte (~0,9 m, voxel 0,05)
        static VoxelRig Gelee()
        {
            var r = NouveauRig("gelee", 0.05f);
            Color32 vArete = C("b4e486"), vClair = C("8ccf62"), vMoyen = C("5fae4a"), vProf = C("357f3e"), vTres = C("1f5633"), reflet = C("dcf8b8");
            Color32 os = C("e6dcc3"), osS = C("b8ab8a"), creux = C("173f26"), lame = C("a9b2b8"), or = Pal.Or, cuir = Pal.CuirSombre;

            // Un cube taillé : larges biseaux, arêtes claires, faces « vitrées » enfoncées d'un voxel où l'on voit le contenu.
            var corps = new VoxelModel();
            const int R = 8, H = 17;
            for (int y = 0; y <= H; y++)
            {
                float aff = y >= H - 1 ? (y - (H - 2)) * 1.5f : 0;              // sommet légèrement bombé et chanfreiné
                float w = R - aff + (y <= 1 ? 1 - y * 0.5f : 0);                  // pied qui s'étale
                Section(corps, 1, y, 0, 0, w, w, y >= H - 1 ? 3 + aff : 2.5f, vMoyen);
            }
            Func<int, int, int, bool> bord = (x, y, z) => !corps.Has(x + 1, y, z) || !corps.Has(x - 1, y, z) || !corps.Has(x, y, z + 1) || !corps.Has(x, y, z - 1) || !corps.Has(x, y + 1, z);
            corps.Paint((x, y, z, c) =>
            {
                if (!bord(x, y, z)) return vTres;
                float ax = Math.Abs(x + 0.5f), az = Math.Abs(z + 0.5f);
                if (y >= H - 1) return vClair;
                if ((ax > R - 2.6f && az > R - 2.6f) || y <= 1) return vArete;             // arêtes verticales et pied
                if (y == H - 2) return vArete;
                return vMoyen;
            });
            // Panneaux : on retire la peau extérieure sur chaque face ; derrière, la masse profonde apparaît.
            corps.Carve((x, y, z) =>
            {
                if (y < 3 || y > H - 4) return false;
                float ax = Math.Abs(x + 0.5f), az = Math.Abs(z + 0.5f);
                bool faceZ = az > R - 0.6f && ax < R - 3;
                bool faceX = ax > R - 0.6f && az < R - 3;
                return faceZ || faceX;
            });
            corps.Paint((x, y, z, c) => (Meme(c, vTres) && bord(x, y, z)) ? (Pal.Hash01(x, y / 2, z) > 0.8f ? vMoyen : vProf) : c);

            // Objets dissous, affleurant dans les panneaux (plan z = R−1 devant, x = ±(R−1) sur les côtés, z = −R dans le dos).
            int f = R - 1;
            // Devant : un crâne humain, une épée en travers, des pièces
            BoiteC(corps, -5, 7, f - 1, -1, 11, f, os, 1);
            corps.Set(-4, 9, f, creux); corps.Set(-2, 9, f, creux); corps.Set(-3, 8, f, osS);
            corps.Box(-4, 6, f, -2, 6, f, osS); corps.Set(-3, 6, f, creux);
            corps.Line(-4.5f, 12.5f, f + 0.5f, 4.5f, 4.5f, f + 0.5f, 0.5f, lame);
            corps.Line(-5.5f, 13.5f, f + 0.5f, -3.5f, 11.5f, f + 0.5f, 0.5f, cuir);
            corps.Line(-6, 11, f + 0.5f, -3, 14, f + 0.5f, 0.5f, Pal.FerSombre);
            foreach (var p in new[] { new[] { 1, 4 }, new[] { 3, 4 }, new[] { 2, 5 }, new[] { 4, 10 }, new[] { 1, 12 } }) corps.Set(p[0], p[1], f, or);
            // Côté droit (+x) : une cage thoracique ; côté gauche : un casque et un fer de hache
            for (int k = 0; k < 4; k++) corps.Line(f + 0.5f, 5 + k * 2, -4, f + 0.5f, 5.5f + k * 2, 3.5f, 0.5f, os);
            corps.Line(f + 0.5f, 4, -4.5f, f + 0.5f, 13, -4.5f, 0.5f, osS);
            BoiteC(corps, -f - 1, 8, -3, -f, 12, 2, Pal.Fer, 1); corps.Box(-f - 1, 9, -2, -f - 1, 10, 1, creux);   // casque à visière
            corps.Line(-f - 0.5f, 5, 0, -f - 0.5f, 5, 4, 0.5f, Pal.FerSombre); corps.Set(-f - 1, 4, 3, or);
            // Dos : un bouclier rond et un fémur
            for (int x = -4; x <= 3; x++) for (int y = 6; y <= 12; y++) { float dx = x + 0.5f, dy = y - 9; if (dx * dx + dy * dy < 12) corps.Set(x, y, -f - 1, dx * dx + dy * dy > 7 ? Pal.FerSombre : Pal.Bois); }
            corps.Line(-5, 4, -f - 0.5f, 4, 6, -f - 0.5f, 0.6f, os);
            // Bulles et reflets sur le dessus, dégoulinures sur les arêtes
            var rnd = new System.Random(5);
            for (int i = 0; i < 16; i++)
            {
                int x = rnd.Next(-R + 2, R - 2), z = rnd.Next(-R + 2, R - 2);
                int h = Haut(corps, x, z, 0, 30); if (h == int.MinValue) continue;
                if (i < 4) { corps.Set(x, h + 1, z, vArete); }
                else corps.Set(x, h, z, i % 3 == 0 ? reflet : vArete);
            }
            for (int i = 0; i < 8; i++)
            {
                int x = rnd.Next(-R, R); int l = rnd.Next(2, 6);
                int fz = (i % 2 == 0) ? R : -R - 1;
                for (int y = 1; y < 1 + l; y++) { corps.Set(x, y + 1, fz + (i % 2 == 0 ? 1 : -1), vClair); }
            }
            // Flaque d'acide au sol, découpée en plaques
            for (int x = -12; x <= 11; x++) for (int z = -12; z <= 11; z++)
            {
                float d = Math.Max(Math.Abs(x + 0.5f), Math.Abs(z + 0.5f)) * 0.75f + (float)Math.Sqrt((x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f)) * 0.35f + Pal.Hash01(x / 2, 0, z / 2) * 2.2f;
                if (d < 12 && !corps.Has(x, 0, z)) corps.Set(x, 0, z, d > 10.6f ? vProf : vMoyen);
            }
            r.Add("corps", corps, V(0, 1, 0));

            // Noyaux luisants à l'intérieur (petits, visibles dans les panneaux)
            var lueur = new VoxelModel();
            lueur.Set(3, 8, f, C("d8ff7a")); lueur.Set(f, 11, 1, C("d8ff7a")); lueur.Set(-f - 1, 6, -3, C("d8ff7a"));
            Soustraire(corps, lueur);
            r.Add("lueur_noyau", lueur, V(0, 1, 0), "corps");
            return Fin(r);
        }
    }
}

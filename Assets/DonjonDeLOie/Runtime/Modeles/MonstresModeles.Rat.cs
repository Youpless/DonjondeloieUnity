// Donjon de l'Oie — le rat géant et le loup gris (quadrupèdes du Bois et de Valcreux).
using System;
using UnityEngine;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static partial class MonstresModeles
    {
        // ------------------------------------------------------------------ rat géant (~0,75 m, voxel 0,05)
        static VoxelRig Rat()
        {
            var r = NouveauRig("rat", 0.05f);
            Color32 poil = C("6f6157"), poilS = C("4a3f39"), poilC = C("a08f7c"), rose = C("c98d86"), roseS = C("9e6a66");
            Color32 dent = C("ead48c"), oeil = C("ff4a2a");

            // Corps : dos très voûté, croupe large, tête basse.
            var corps = new VoxelModel();
            ProfilZ(corps, 0, new float[] { -9, 4.5f, 2.6f, 2.6f, -6, 6.4f, 4.2f, 4.4f, -2, 7.4f, 4.4f, 4.8f, 2, 6.6f, 3.8f, 4, 5, 5.6f, 2.8f, 3 }, 0.45f, poil);
            corps.Paint((x, y, z, c) => y <= 3 ? poilC : c);
            // poils hérissés le long de l'échine, en mèches sombres
            for (int z = -7; z <= 3; z += 2)
            {
                int h = Haut(corps, 0, z, 0, 20);
                if (h == int.MinValue) continue;
                corps.Set(0, h + 1, z, poilS); corps.Set(-1, h + 1, z - 1, poilS);
                if (z % 4 == 1) { corps.Set(0, h + 2, z - 1, poilS); }
            }
            corps.Paint((x, y, z, c) => (Meme(c, poil) && y >= 10) ? poilS : c);
            // touffe d'épaule plus sombre
            corps.Paint((x, y, z, c) => (Meme(c, poil) && z >= 1 && z <= 3 && Math.Abs(x + 0.5f) >= 3) ? poilS : c);
            r.Add("corps", corps, V(0, 6, 0));

            // Tête : museau pointu, oreilles rondes, yeux rouges.
            var tete = new VoxelModel();
            ProfilZ(tete, 0, new float[] { 4, 6, 2.8f, 3, 7, 5.8f, 2.6f, 2.6f, 10, 5, 1.8f, 1.8f, 13, 4.6f, 1, 1.1f }, 0.45f, poil);
            tete.Paint((x, y, z, c) => (y <= 4 && z >= 7) ? poilC : c);
            tete.Box(-1, 4, 14, 0, 5, 14, rose);                         // truffe
            for (int s = 0; s < 2; s++)
            {
                int sx = s == 0 ? -1 : 1; int bx = s == 0 ? -4 : 3;
                // oreilles : disques roses cerclés
                tete.Box(bx, 8, 4, bx + (sx < 0 ? 1 : -1) * 0, 11, 6, poilS);
                tete.Box(bx, 9, 5, bx, 10, 5, rose);
                tete.Set(bx, 11, 5, poilS);
                // moustaches
                tete.Line(sx * 1.5f + 0.5f - 0.5f, 4.5f, 12.5f, sx * 5f, 5.5f, 11f, 0.45f, poilS);
            }
            var lueurYeux = new VoxelModel();
            Oeil(tete, lueurYeux, -3, 7, 8, oeil); Oeil(tete, lueurYeux, 2, 7, 8, oeil);
            tete.Set(-3, 8, 8, poilS); tete.Set(2, 8, 8, poilS);       // sourcils froncés
            r.Add("tete", tete, V(0, 6, 5), "corps");
            r.Add("lueur_yeux", lueurYeux, V(0, 6, 5), "tete");

            // Mâchoire et incisives
            var mach = new VoxelModel();
            ProfilZ(mach, 0, new float[] { 7, 3.3f, 1.6f, 0.9f, 12, 3.6f, 0.9f, 0.7f }, 0.3f, poilC);
            mach.Box(-1, 2, 12, 0, 3, 12, dent);
            tete.Box(-1, 3, 13, 0, 4, 13, dent);
            Soustraire(tete, mach);
            r.Add("machoire", mach, V(0, 4, 7), "tete");

            // Pattes : avant fines, arrière à cuisse ronde et long pied rose.
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1 : 1;
                var av = new VoxelModel();
                Chaine(av, new float[] { sx * 2.8f, 5, 3, sx * 3.2f, 2, 4, sx * 3.2f, 0.7f, 5.2f }, 1.3f, 0.8f, poilS);
                av.Box((int)Math.Floor(sx * 3.2f) - 1, 0, 5, (int)Math.Floor(sx * 3.2f) + 1, 0, 7, rose);
                av.Set((int)Math.Floor(sx * 3.2f) - 1, 0, 8, roseS); av.Set((int)Math.Floor(sx * 3.2f) + 1, 0, 8, roseS);
                r.Add(s == 0 ? "pattesAvG" : "pattesAvD", av, V(sx * 2.8f, 5, 3), "corps");

                var ar = new VoxelModel();
                ar.Ellipsoid(sx * 3.6f, 5, -5, 1.8f, 3, 3, poil);
                Chaine(ar, new float[] { sx * 3.8f, 4, -5, sx * 4f, 1.5f, -7, sx * 4f, 0.6f, -5 }, 1.3f, 0.9f, poilS);
                int px = (int)Math.Floor(sx * 4f);
                ar.Box(px - 1, 0, -7, px + 1, 0, -3, rose);
                ar.Set(px - 1, 0, -2, roseS); ar.Set(px + 1, 0, -2, roseS); ar.Set(px, 0, -2, roseS);
                r.Add(s == 0 ? "pattesArG" : "pattesArD", ar, V(sx * 3.4f, 6, -5), "corps");
            }

            // Queue : longue, annelée, rose sale.
            var queue = new VoxelModel();
            Chaine(queue, new float[] { 0, 4.5f, -9, 0, 2.6f, -13, 1.5f, 1.2f, -18, 4, 1, -22, 6.5f, 1.8f, -25 }, 1.3f, 0.55f, rose);
            queue.Paint((x, y, z, c) => ((x + z) % 2 == 0) ? roseS : c);
            r.Add("queue", queue, V(0, 4.5f, -9), "corps");
            return Fin(r);
        }

        // ------------------------------------------------------------------ loup gris (~1,15 m, voxel 0,05)
        static VoxelRig Loup()
        {
            var r = NouveauRig("loup", 0.05f);
            Color32 gris = C("7f7c78"), grisS = C("4f4d4c"), grisC = C("bdb6aa"), noir = C("2a2828"), oeil = C("ffc23a");
            Color32 croc = C("eee6cf"), gueule = C("6a2a2a");

            var corps = new VoxelModel();
            // Poitrail profond, taille fine, croupe un peu plus basse.
            ProfilZ(corps, 0, new float[] { -10, 10.5f, 3, 3, -6, 10.5f, 3.4f, 3.4f, -1, 10.8f, 3.2f, 3.2f, 3, 11.5f, 3.8f, 4.6f, 7, 12.5f, 3.6f, 4.4f, 9, 13.5f, 3, 3.5f }, 0.45f, gris);
            corps.Paint((x, y, z, c) => (y <= 8 || (z >= 4 && y <= 10 && Math.Abs(x + 0.5f) < 2)) ? grisC : ((y >= 14 && Math.Abs(x + 0.5f) < 2.5f) ? grisS : c));
            // Crinière hérissée : mèches pointues sur la nuque et le garrot, couchées vers l'arrière.
            var rnd = new System.Random(7);
            for (int z = -2; z <= 10; z++)
                for (int x = -4; x <= 3; x++)
                {
                    int h = Haut(corps, x, z, 0, 30);
                    if (h == int.MinValue || h < 13) continue;
                    if (rnd.NextDouble() < 0.5)
                    {
                        float l = 1.2f + (float)rnd.NextDouble() * (z > 2 ? 2.4f : 1.2f);
                        var mc = rnd.NextDouble() < 0.45 ? gris : (rnd.NextDouble() < 0.75 ? grisS : noir);
                        corps.Line(x + 0.5f, h + 0.5f, z + 0.5f, x + 0.5f + (x + 0.5f) * 0.35f, h + 0.5f + l, z - l * 0.9f, 0.55f, 0.4f, mc);
                    }
                }
            r.Add("corps", corps, V(0, 11, 0));

            // Tête : crâne en coin, museau long, oreilles dressées, yeux jaunes.
            var tete = new VoxelModel();
            ProfilZ(tete, 0, new float[] { 8, 15, 3.2f, 3.4f, 12, 15.5f, 3.4f, 3, 14, 14.8f, 2.4f, 2.2f, 19, 14, 1.6f, 1.5f }, 0.4f, gris);
            tete.Paint((x, y, z, c) => (y <= 13 && z >= 12) ? grisC : ((y >= 17 && z <= 11) ? grisS : c));
            tete.Box(-1, 14, 19, 0, 15, 20, noir);                        // truffe
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1 : 1;
                // oreilles triangulaires, pointées vers l'avant
                for (int y = 0; y < 5; y++)
                {
                    float w = 2.2f - y * 0.45f;
                    for (int x = (int)Math.Floor(sx * 2.6f - w); x <= (int)Math.Floor(sx * 2.6f + w); x++)
                        tete.Box(x, 18 + y, 9 - y / 2, x, 18 + y, 11 - y / 2, y >= 3 ? noir : grisS);
                }
                tete.Set((int)Math.Floor(sx * 2.6f), 19, 11 , C("8a6a64")); tete.Set((int)Math.Floor(sx * 2.6f), 20, 10, C("8a6a64"));
                // sourcil froncé et babine sombre
                tete.Box(s == 0 ? -4 : 2, 17, 13, s == 0 ? -3 : 3, 17, 13, grisS);
            }
            tete.Box(-2, 13, 14, 1, 13, 18, noir);                        // babines
            for (int z = 14; z <= 18; z += 2) { tete.Set(-2, 12, z, croc); tete.Set(1, 12, z, croc); }
            var lueur = new VoxelModel();
            Oeil(tete, lueur, -3, 16, 13, oeil); Oeil(tete, lueur, 2, 16, 13, oeil);
            r.Add("tete", tete, V(0, 14, 9), "corps");
            r.Add("lueur_yeux", lueur, V(0, 14, 9), "tete");

            var mach = new VoxelModel();
            ProfilZ(mach, 0, new float[] { 11, 11.8f, 2, 1.2f, 17, 12, 1.3f, 0.9f }, 0.3f, grisC);
            mach.Box(-1, 12, 12, 0, 12, 16, gueule);
            mach.Set(-2, 12, 16, croc); mach.Set(1, 12, 16, croc);
            Soustraire(tete, mach);
            r.Add("machoire", mach, V(0, 13, 11), "tete");

            // Pattes : avant droites, arrière à jarret.
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1 : 1;
                var av = new VoxelModel();
                Chaine(av, new float[] { sx * 2.6f, 11, 5, sx * 2.8f, 6, 5.5f, sx * 2.8f, 2, 5, sx * 2.8f, 0.8f, 6.5f }, 1.8f, 1.1f, gris);
                av.Paint((x, y, z, c) => y <= 2 ? grisS : c);
                int px = (int)Math.Floor(sx * 2.8f);
                av.Box(px - 1, 0, 5, px + 1, 1, 8, grisS);
                av.Set(px - 1, 0, 9, noir); av.Set(px + 1, 0, 9, noir); av.Set(px, 0, 9, noir);
                r.Add(s == 0 ? "pattesAvG" : "pattesAvD", av, V(sx * 2.6f, 11, 5), "corps");

                var ar = new VoxelModel();
                Chaine(ar, new float[] { sx * 2.8f, 11, -8, sx * 3.2f, 6.5f, -5.5f, sx * 3f, 3, -9, sx * 3f, 0.8f, -8 }, 2.4f, 1.1f, gris);
                ar.Paint((x, y, z, c) => y <= 3 ? grisS : c);
                px = (int)Math.Floor(sx * 3f);
                ar.Box(px - 1, 0, -9, px + 1, 1, -6, grisS);
                ar.Set(px - 1, 0, -5, noir); ar.Set(px + 1, 0, -5, noir);
                r.Add(s == 0 ? "pattesArG" : "pattesArD", ar, V(sx * 2.8f, 11, -8), "corps");
            }

            // Queue touffue, basse, bout sombre.
            var queue = new VoxelModel();
            Chaine(queue, new float[] { 0, 11.5f, -10, 0, 9.5f, -14, 0, 6.5f, -17, 0.5f, 4.5f, -18.5f }, 1.6f, 2.2f, gris);
            queue.Paint((x, y, z, c) => (y <= 6) ? noir : (y >= 11 ? grisS : c));
            r.Add("queue", queue, V(0, 11.5f, -10), "corps");
            return Fin(r);
        }
    }
}

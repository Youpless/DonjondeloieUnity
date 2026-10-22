// Donjon de l'Oie — Valcreux : tour de garde, porte fortifiée, rempart, puits, bannière, poteau indicateur, étal, pont.
using System;
using UnityEngine;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static partial class DecorsModeles
    {
        static readonly Color32 VcBleu = Pal.C("2f5596"), VcBleuSombre = Pal.C("223f73"), VcOr = Pal.C("e2b047");

        /// Créneaux sur le pourtour d'un rectangle plein (x0..x1, z0..z1) posé à la hauteur y.
        static void Creneaux(ChantierBat c, int x0, int z0, int x1, int z1, int y, int h, int pas, Color32 col)
        {
            for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++)
            {
                bool bord = x <= x0 + 1 || x >= x1 - 1 || z <= z0 + 1 || z >= z1 - 1;
                if (!bord) continue;
                int k = (x <= x0 + 1 || x >= x1 - 1) ? z - z0 : x - x0;
                bool merlon = ChantierBat.Mod(k, pas * 2) < pas;
                int hh = merlon ? h : 1;
                c.Pierre(x, y, z, x, y + hh - 1, z, col);
            }
        }

        /// Étendard pendant (plan x, épaisseur 1) : bleu, bordure et lion d'or, pointe en V.
        static void Etendard(ChantierBat c, int x, int yHaut, int z0, int larg, int haut)
        {
            for (int y = yHaut - haut; y <= yHaut; y++) for (int z = z0; z < z0 + larg; z++)
            {
                int dz = z - z0, dy = yHaut - y;
                int pointe = Math.Abs(dz * 2 + 1 - larg) / 2;
                if (dy > haut - (larg / 2 - pointe)) continue;
                bool bord = dz == 0 || dz == larg - 1 || dy == 1 || dy > haut - (larg / 2 - pointe) - 1;
                Color32 col = bord ? VcOr : (dy == 0 ? Pal.FerSombre : Pal.Shade(VcBleu, 1f - 0.05f * (dz % 2)));
                c.M.Set(x, y, z, col);
            }
            // lion rampant stylisé (silhouette 5 × 7), de face sur les deux côtés
            string[] lion = { ".##..", "####.", ".###.", "..##.", ".####", "#.#.#", "#...#" };
            int zc = z0 + larg / 2 - 2, yc = yHaut - 3;
            for (int r = 0; r < lion.Length; r++) for (int k = 0; k < 5; k++)
                if (lion[r][k] == '#') { c.M.Set(x - 1, yc - r, zc + k, VcOr); c.M.Set(x + 1, yc - r, zc + (4 - k), VcOr); }
        }

        // ------------------------------------------------------------------ tour de garde
        static VoxelRig TourGarde(int graine)
        {
            var c = new ChantierBat(graine) { Assise = 3, Longueur = 6, Mousse = 0.12f, Evidement = 2 };
            bool ronde = ChantierBat.Mod(graine, 2) == 0;   // graines paires : tour ronde à toit pointu
            int hFut = 54;
            if (ronde)
            {
                c.PierreCone(0, 0, 0, 3, 14.5f, 13.5f, VcPierreSombre);
                c.PierreCyl(0, 0, 4, hFut, 12.5f, VcPierre);
                c.Rond(0, 0, 0, hFut + 12, 14.5f);
                c.P.Carve((x, y, z) => y > 4 && y < hFut - 2 && (x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f) < 9.5f * 9.5f);
                c.PierreCyl(0, 0, hFut - 1, hFut + 1, 14.5f, VcPierreSombre);   // corniche
                for (int a = 0; a < 16; a++)   // mâchicoulis : corbeaux sous la corniche
                {
                    float ang = a * Mathf.PI * 2 / 16; int x = Mathf.RoundToInt(Mathf.Cos(ang) * 13.5f), z = Mathf.RoundToInt(Mathf.Sin(ang) * 13.5f);
                    c.Pierre(x, hFut - 4, z, x, hFut - 2, z, VcPierreSombre);
                }
                var cr = new VoxelModel(); cr.Cylinder(0, 0, hFut + 2, hFut + 6, 14.5f, VcPierre);
                cr.Carve((x, y, z) => (x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f) < 12.5f * 12.5f);
                cr.Carve((x, y, z) => y > hFut + 3 && ChantierBat.Mod(Mathf.FloorToInt((Mathf.Atan2(z + 0.5f, x + 0.5f) + Mathf.PI) * 14.5f / 5f), 2) == 0);
                foreach (var kv in cr.Tous()) c.P.Set(kv.Key.x, kv.Key.y, kv.Key.z, kv.Value);
                c.Pierre(-12, hFut + 1, -12, 12, hFut + 1, 12, VcPierreSombre);
                c.P.Carve((x, y, z) => y == hFut + 1 && (x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f) > 13.5f * 13.5f && (x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f) > 14.5f * 14.5f);
            }
            else
            {
                c.Pierre(-14, 0, -14, 13, 3, 13, VcPierreSombre);
                c.MursPierre(-13, 4, -13, 12, hFut, 12, 3, VcPierre);
                for (int y = 4; y < hFut; y++) foreach (int x in new[] { -13, 12 }) foreach (int z in new[] { -13, 12 })   // chaînages d'angle clairs
                    if (ChantierBat.Mod(y / 3, 2) == 0) c.Pierre(x, y, z, x, y, z, Pal.Shade(VcPierre, 1.12f));
                c.Pierre(-15, hFut - 1, -15, 14, hFut + 1, 14, VcPierreSombre);
                for (int k = -14; k <= 13; k += 4) { c.Pierre(k, hFut - 3, 13, k, hFut - 2, 13, VcPierreSombre); c.Pierre(k, hFut - 3, -14, k, hFut - 2, -14, VcPierreSombre); c.Pierre(13, hFut - 3, k, 13, hFut - 2, k, VcPierreSombre); c.Pierre(-14, hFut - 3, k, -14, hFut - 2, k, VcPierreSombre); }
                Creneaux(c, -15, -15, 14, 14, hFut + 2, 5, 3, VcPierre);
            }
            // porte et meurtrières (façade +z)
            int zf = ronde ? 12 : 12;
            var av = FaceBat.Avant(zf);
            c.Porte(av, -4, 1, 8, 16, ronde ? 4 : 3, Pal.C("5e3d26"), true, Pal.Shade(VcPierre, 1.1f));
            for (int k = 0; k < 2; k++)
            {
                int y = 24 + k * 14;
                for (int yy = y; yy < y + 7; yy++) for (int d = 0; d > -3; d--) { c.VideF(av, -1 + (k % 2) * 0, yy, d); c.VideF(av, 0, yy, d); }
                for (int yy = y; yy < y + 7; yy++) { c.SetF(av, -1, yy, -3, Pal.Noir); c.SetF(av, 0, yy, -3, Pal.Noir); }
            }
            // meurtrières latérales
            foreach (var f in new[] { FaceBat.Droite(ronde ? 12 : 12), FaceBat.Gauche(ronde ? -13 : -13) })
                for (int yy = 30; yy < 37; yy++) { c.VideF(f, 0, yy, 0); c.VideF(f, 0, yy, -1); c.VideF(f, 0, yy, -2); c.SetF(f, 0, yy, -3, Pal.Noir); }
            // toit pointu d'ardoise sur la tour ronde, ou mât au centre de la terrasse
            if (ronde)
            {
                for (int y = 0; y < 22; y++)
                {
                    float r = 11.5f * (1f - y / 22f) + 0.6f;
                    var t = new VoxelModel(); t.Cylinder(0, 0, hFut + 3 + y, hFut + 3 + y, r, VcArdoise);
                    foreach (var kv in t.Tous())
                    {
                        float ang = Mathf.Atan2(kv.Key.z + 0.5f, kv.Key.x + 0.5f);
                        int col = Mathf.FloorToInt((ang + Mathf.PI) * 4f);
                        float f = (ChantierBat.Mod(y, 2) == 0 ? 1f : 0.88f) * (0.92f + 0.12f * ChantierBat.H(col + y % 2 * 3, y / 2, 0));
                        c.M.Set(kv.Key.x, kv.Key.y, kv.Key.z, Pal.Shade(VcArdoise, f));
                    }
                }
                c.Poutre(0, hFut + 25, 0, 0, hFut + 34, 0, Pal.FerSombre);
                Etendard(c, 0, hFut + 33, 1, 9, 13);
            }
            else
            {
                c.Bois(-11, hFut + 1, -11, 10, hFut + 1, 10, Pal.C("7a5a3c"), 'x');
                c.Poutre(0, hFut + 2, 0, 0, hFut + 32, 0, Pal.BoisSombre);
                c.M.Set(0, hFut + 33, 0, VcOr);
                Etendard(c, 0, hFut + 31, 1, 11, 18);
            }
            // un peu de lierre sur la face gauche
            for (int y = 1; y < 26; y++) for (int z = -6; z <= 4; z++)
            {
                float h = ChantierBat.H(z, y, 31);
                if (h < 0.55f - y * 0.018f)
                {
                    int x = -14;
                    if (ronde) { float d = 12.5f * 12.5f - (z + 0.5f) * (z + 0.5f); x = -Mathf.FloorToInt(Mathf.Sqrt(Math.Max(0, d))) - 1; if (y < 4) x -= 2; }
                    else if (y < 4) x = -15;
                    c.M.Set(x, y, z, h < 0.2f ? Pal.C("4a6e30") : Pal.C("5d8a3c"));
                }
            }
            return c.Rig("tour_garde", 0.1f);
        }

        // ------------------------------------------------------------------ porte de ville
        static VoxelRig PorteVille(int graine)
        {
            var c = new ChantierBat(graine) { Assise = 3, Longueur = 6, Mousse = 0.06f, Evidement = 1 };
            int hm = 34;
            // corps central : murs épais (z de −8 à 7), passage de 14 de large
            c.Pierre(-17, 0, -8, 16, hm, 7, VcPierre);
            c.Vide(-7, 0, -9, 6, 22, 8);
            // arche en plein cintre
            for (int x = -7; x <= 6; x++) for (int y = 22; y <= 30; y++)
            {
                float dx = x + 0.5f, dy = y - 22f;
                if (dx * dx + dy * dy <= 7f * 7f) c.Vide(x, y, -9, x, y, 8);
            }
            // claveaux de l'arche (voussoirs clairs) sur les deux faces
            for (int x = -10; x <= 9; x++) for (int y = 16; y <= 33; y++)
            {
                float dx = x + 0.5f, dy = y - 22f; float q = dx * dx + dy * dy;
                if (y >= 22 && q > 7f * 7f && q <= 10f * 10f)
                {
                    float ang = Mathf.Atan2(dy, dx); int k = Mathf.FloorToInt(ang * 6f);
                    Color32 col = Pal.Shade(VcPierre, ChantierBat.Mod(k, 2) == 0 ? 1.12f : 1.0f);
                    if (Math.Abs(dx) < 1.1f && q > 8.4f * 8.4f) col = Pal.Shade(VcOr, 0.8f);   // clé de voûte
                    c.M.Set(x, y, 8, col); c.M.Set(x, y, -9, col);
                }
            }
            // herse à moitié levée
            for (int x = -7; x <= 6; x++) for (int y = 12; y <= 29; y++)
            {
                float dx = x + 0.5f, dy = y - 22f;
                if (y > 22 && dx * dx + dy * dy > 7f * 7f) continue;
                bool barreV = ChantierBat.Mod(x + 7, 3) == 1, barreH = ChantierBat.Mod(y - 12, 4) == 0;
                if (barreV || barreH) c.M.Set(x, y, 2, barreV && y == 12 ? Pal.Fer : Pal.FerSombre);
            }
            for (int x = -7; x <= 6; x++) if (ChantierBat.Mod(x + 7, 3) == 1) c.M.Set(x, 11, 2, Pal.Fer);
            c.Vide(-8, 11, 1, -8, 30, 3); c.Vide(7, 11, 1, 7, 30, 3);   // rainures
            // chemin de ronde crénelé
            c.Pierre(-18, hm - 1, -9, 17, hm, 8, VcPierreSombre);
            Creneaux(c, -18, -9, 17, 8, hm + 1, 4, 3, VcPierre);
            // deux tours rondes encadrant la porte
            foreach (int sx in new[] { -1, 1 })
            {
                float cx = sx < 0 ? -21.5f : 20.5f; float cz = 0f;
                c.PierreCone(cx, cz, 0, 4, 11f, 10f, VcPierreSombre);
                c.PierreCyl(cx, cz, 5, hm + 12, 9.5f, VcPierre);
                c.Rond(cx, cz, 0, hm + 30, 11f);
                c.PierreCyl(cx, cz, hm + 12, hm + 13, 11f, VcPierreSombre);
                var cr = new VoxelModel(); cr.Cylinder(cx, cz, hm + 14, hm + 17, 11f, VcPierre);
                float cxx = cx;
                cr.Carve((x, y, z) => (x + 0.5f - cxx) * (x + 0.5f - cxx) + (z + 0.5f) * (z + 0.5f) < 9f * 9f);
                cr.Carve((x, y, z) => y > hm + 15 && ChantierBat.Mod(Mathf.FloorToInt((Mathf.Atan2(z + 0.5f, x + 0.5f - cxx) + Mathf.PI) * 11f / 4f), 2) == 0);
                foreach (var kv in cr.Tous()) c.P.Set(kv.Key.x, kv.Key.y, kv.Key.z, kv.Value);
                // toit conique d'ardoise
                for (int y = 0; y < 18; y++)
                {
                    float r = 9.5f * (1f - y / 18f) + 0.5f;
                    var t = new VoxelModel(); t.Cylinder(cx, cz, hm + 14 + y, hm + 14 + y, r, VcArdoise);
                    foreach (var kv in t.Tous())
                    {
                        int col = Mathf.FloorToInt((Mathf.Atan2(kv.Key.z + 0.5f, kv.Key.x + 0.5f - cx) + Mathf.PI) * 4f);
                        c.M.Set(kv.Key.x, kv.Key.y, kv.Key.z, Pal.Shade(VcArdoise, (y % 2 == 0 ? 1f : 0.87f) * (0.92f + 0.12f * ChantierBat.H(col + y % 2 * 3, y / 2, sx))));
                    }
                }
                c.Poutre((int)Math.Floor(cx), hm + 32, 0, (int)Math.Floor(cx), hm + 38, 0, Pal.FerSombre);
                c.M.Set((int)Math.Floor(cx), hm + 39, 0, VcOr);
                // meurtrières
                for (int y = 16; y < 23; y++) { int x = (int)Math.Floor(cx); c.P.Remove(x, y, 9); c.P.Remove(x, y, 8); c.M.Set(x, y, 8, Pal.Noir); }
            }
            // bannières pendant de part et d'autre de l'arche
            foreach (int x0 in new[] { -15, 10 })
                for (int x = x0; x < x0 + 5; x++) for (int y = hm - 18; y <= hm - 2; y++)
                {
                    int dy = hm - 2 - y;
                    if (dy > 13 && Math.Abs(x - x0 - 2) < dy - 13) continue;
                    bool bord = x == x0 || x == x0 + 4 || dy == 0;
                    c.M.Set(x, y, 8, bord ? VcOr : (dy > 4 && dy < 9 && x == x0 + 2 ? VcOr : VcBleu));
                }
            // torches éteintes / bornes de pierre au pied du passage
            c.Pierre(-9, 0, 8, -8, 2, 10, VcPierreSombre); c.Pierre(7, 0, 8, 8, 2, 10, VcPierreSombre);
            c.Pierre(-9, 0, -11, -8, 2, -9, VcPierreSombre); c.Pierre(7, 0, -11, 8, 2, -9, VcPierreSombre);
            return c.Rig("porte_ville", 0.1f);
        }

        // ------------------------------------------------------------------ rempart
        static VoxelRig Rempart(int graine)
        {
            var c = new ChantierBat(graine) { Assise = 3, Longueur = 6, Mousse = 0.1f, Evidement = 2 };
            int hm = 34;
            c.Pierre(-20, 0, -5, 19, 3, 5, VcPierreSombre);   // fruit / empattement
            c.Pierre(-20, 4, -4, 19, hm, 4, VcPierre);
            c.Pierre(-20, hm - 1, -6, 19, hm, 5, VcPierreSombre);   // cordon
            for (int x = -20; x <= 19; x++) for (int z = -6; z <= 5; z++)
            {
                if (z > -5 && z < 4) continue;
                bool merlon = ChantierBat.Mod(x + 20, 8) < 5;
                c.Pierre(x, hm + 1, z, x, hm + (merlon ? 5 : 1), z, VcPierre);
            }
            c.Bois(-20, hm + 1, -4, 19, hm + 1, 3, Pal.C("7a5a3c"), 'x');   // chemin de ronde
            // corbeaux et meurtrières
            for (int x = -18; x <= 18; x += 8) { c.Pierre(x, hm - 3, 5, x + 1, hm - 2, 5, VcPierreSombre); c.Pierre(x, hm - 3, -6, x + 1, hm - 2, -6, VcPierreSombre); }
            foreach (int x in new[] { -12, 4 }) for (int y = 16; y < 22; y++) { c.Vide(x, y, 3, x, y, 4); c.M.Set(x, y, 2, Pal.Noir); }
            // lierre, au hasard selon la graine
            int x0 = -16 + c.R.Next(20);
            for (int y = 3; y < 24; y++) for (int x = x0; x < x0 + 9; x++)
            {
                float h = ChantierBat.H(x, y, 17 + graine);
                if (h < 0.6f - y * 0.022f) c.M.Set(x, y, y < 4 ? 6 : 5, h < 0.22f ? Pal.C("4a6e30") : Pal.C("5d8a3c"));
            }
            return c.Rig("rempart", 0.1f);
        }

        // ------------------------------------------------------------------ puits
        static VoxelRig Puits(int graine)
        {
            var c = new ChantierBat(graine) { Assise = 2, Longueur = 5, Mousse = 0.15f };
            c.PierreCyl(0, 0, 0, 0, 7.5f, VcPierreSombre);
            c.PierreCyl(0, 0, 1, 6, 6.5f, VcPierre);
            c.Rond(0, 0, 0, 8, 7.5f);
            c.P.Carve((x, y, z) => y > 0 && (x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f) < 4.5f * 4.5f);
            c.PierreCyl(0, 0, 7, 7, 7f, VcPierreClaireV);
            c.P.Carve((x, y, z) => y == 7 && (x + 0.5f) * (x + 0.5f) + (z + 0.5f) * (z + 0.5f) < 4.8f * 4.8f);
            var eau = new VoxelModel(); eau.Cylinder(0, 0, 3, 3, 4.6f, Pal.EauProfonde); c.M.Merge(eau, 0, 0, 0);
            // potence de bois, treuil, toit à deux pans
            foreach (int x in new[] { -7, 6 }) c.Poutre(x, 1, 0, x, 18, 0, Pal.BoisSombre);
            c.Poutre(-8, 12, 0, 7, 12, 0, Pal.Bois);   // treuil
            c.Poutre(8, 12, 0, 8, 12, 2, Pal.FerSombre); c.Poutre(8, 10, 2, 8, 12, 2, Pal.FerSombre);   // manivelle
            for (int x = -2; x <= 1; x++) c.M.Set(x, 12, -1, Pal.C("c9b07a"));   // corde enroulée
            c.Poutre(0, 6, 0, 0, 11, 0, Pal.C("c9b07a"));
            // seau suspendu
            c.Poutre(-2, 4, -2, 1, 6, 1, Pal.C("8a5a34")); c.Vide(-1, 6, -1, 0, 6, 0);
            c.Poutre(-2, 5, -2, 1, 5, 1, Pal.FerSombre); c.Vide(-1, 5, -1, 0, 5, 0);
            c.Toit(true, -9, 8, -5, 4, 18, 1, 2, VcBardeau, 3);
            c.Poutre(-8, 18, -1, 7, 18, 0, Pal.BoisSombre);
            // seau posé sur la margelle
            c.Bois(4, 8, 4, 6, 10, 5, Pal.C("8a5a34"), 'x', 1);
            return c.Rig("puits", 0.1f);
        }
        static readonly Color32 VcPierreClaireV = Pal.C("bdb2a0");

        // ------------------------------------------------------------------ bannière
        static VoxelRig Banniere(int graine)
        {
            var c = new ChantierBat(graine) { Assise = 2, Longueur = 3 };
            c.Pierre(-3, 0, -3, 2, 1, 2, VcPierreSombre);
            c.Pierre(-2, 2, -2, 1, 2, 1, VcPierre);
            c.Poutre(-1, 3, -1, 0, 30, 0, Pal.BoisSombre);
            c.Poutre(-1, 29, -1, 0, 29, 9, Pal.BoisSombre);   // traverse vers l'avant
            c.M.Set(-1, 31, -1, VcOr); c.M.Set(0, 31, 0, VcOr); c.M.Set(-1, 31, 0, VcOr); c.M.Set(0, 31, -1, VcOr); c.M.Set(0, 32, 0, VcOr);
            c.M.Set(-1, 29, 10, VcOr);
            Etendard(c, -1, 28, 1, 9, 17);
            // haubans
            c.Trait(-0.5f, 20, 0.5f, -5.5f, 0.5f, 3.5f, 0.62f, Pal.C("c9b07a"));
            c.Trait(-0.5f, 20, 0.5f, 4.5f, 0.5f, -2.5f, 0.62f, Pal.C("c9b07a"));
            c.Pierre(-6, 0, 3, -5, 0, 4, VcPierreSombre); c.Pierre(4, 0, -3, 5, 0, -2, VcPierreSombre);
            return c.Rig("banniere", 0.1f);
        }

        // ------------------------------------------------------------------ poteau indicateur
        static VoxelRig Panneau(int graine)
        {
            var c = new ChantierBat(graine);
            c.Poutre(-1, 0, -1, 0, 18, 0, Pal.C("6e4a2e"));
            c.Poutre(-1, 19, -1, 0, 19, 0, Pal.Shade(Pal.C("6e4a2e"), 0.8f));
            c.Trait(-0.5f, 0.5f, 0.5f, -3.5f, 0.5f, 2.5f, 0.6f, Pal.PierreSombre);
            c.M.Set(2, 0, -2, Pal.Pierre); c.M.Set(-3, 0, -2, Pal.Pierre); c.M.Set(1, 0, 2, Pal.PierreSombre);
            // trois flèches, orientées différemment
            Fleche(c, 15, 1, 0, Pal.C("a77a4a"));
            Fleche(c, 12, -1, 0, Pal.C("94683e"));
            Fleche(c, 9, 0, 1, Pal.C("a07040"));
            return c.Rig("panneau", 0.1f);
        }

        static void Fleche(ChantierBat c, int y, int sx, int sz, Color32 col)
        {
            for (int k = 0; k < 9; k++) for (int dy = 0; dy < 3; dy++)
            {
                if (k == 8 && dy != 1) continue;
                if (k == 7 && dy == 0) { }
                int u = k + 1;
                Color32 cc = dy == 1 && k > 1 && k < 6 && k % 2 == 0 ? Pal.Shade(col, 0.6f) : Pal.Shade(col, dy == 2 ? 1.05f : 1f);
                if (sx != 0) { int x = sx > 0 ? u : -1 - u; c.M.Set(x, y + dy, -1 + (sx > 0 ? 0 : -1) + 1, cc); }
                else { int z = sz > 0 ? u : -1 - u; c.M.Set(0, y + dy, z, cc); }
            }
        }

        // ------------------------------------------------------------------ étal
        static VoxelRig Etal(int graine)
        {
            var c = new ChantierBat(graine);
            var b = Pal.C("8b5e3c");
            // comptoir
            c.Bois(-9, 0, 1, 8, 6, 4, b, 'x');
            c.Bois(-10, 7, 0, 9, 7, 5, Pal.C("a07048"), 'z', 1);
            // poteaux et auvent rayé
            foreach (int x in new[] { -10, 9 }) { c.Poutre(x, 0, 5, x, 16, 5, Pal.BoisSombre); c.Poutre(x, 0, -5, x, 19, -5, Pal.BoisSombre); }
            var r1 = Pal.C("c25a3a"); var r2 = Pal.C("e8dcc0");
            for (int x = -11; x <= 10; x++) for (int z = -6; z <= 8; z++)
            {
                int y = 19 - (z + 6) / 4;
                c.M.Set(x, y, z, ChantierBat.Mod(x + 11, 4) < 2 ? r1 : r2);
                if (z == 8) { c.M.Set(x, y - 1, z, ChantierBat.Mod(x + 11, 4) < 2 ? r1 : r2); if (ChantierBat.Mod(x, 2) == 0) c.M.Set(x, y - 2, z, ChantierBat.Mod(x + 11, 4) < 2 ? r1 : r2); }
            }
            // marchandises sur le comptoir : pommes, pains, fioles, balance
            for (int x = -8; x <= -5; x++) for (int z = 1; z <= 3; z++) c.M.Set(x, 8, z, (x + z) % 2 == 0 ? Pal.C("c23a2a") : Pal.C("9ab03a"));
            c.M.Set(-7, 9, 2, Pal.C("c23a2a")); c.M.Set(-6, 9, 2, Pal.C("c23a2a"));
            c.Poutre(-3, 8, 1, -1, 9, 3, Pal.C("d8a85a"));
            foreach (int x in new[] { 1, 3 }) { c.M.Set(x, 8, 2, Pal.C("4a8ac0")); c.M.Set(x, 9, 2, Pal.C("4a8ac0")); c.M.Set(x, 10, 2, Pal.Os); }
            c.M.Set(2, 8, 3, Pal.C("8a3ab0")); c.M.Set(2, 9, 3, Pal.C("8a3ab0"));
            // balance
            c.Poutre(6, 8, 2, 6, 13, 2, Pal.OrSombre); c.Poutre(4, 13, 2, 8, 13, 2, Pal.OrSombre);
            c.Poutre(4, 10, 1, 4, 10, 3, Pal.Or); c.Poutre(8, 11, 1, 8, 11, 3, Pal.Or);
            c.M.Set(4, 11, 2, Pal.OrSombre); c.M.Set(4, 12, 2, Pal.OrSombre); c.M.Set(8, 12, 2, Pal.OrSombre);
            // caisses et sacs devant / à côté
            c.Bois(10, 0, 2, 14, 4, 6, Pal.C("9a7048"), 'y'); c.Poutre(10, 2, 6, 14, 2, 6, Pal.Shade(Pal.C("9a7048"), 0.7f));
            c.Bois(11, 5, 3, 13, 7, 5, Pal.C("a8804e"), 'y');
            var sac = Pal.C("c9b07a");
            c.M.Ellipsoid(-12.5f, 2.5f, 5.5f, 2.2f, 2.8f, 2.2f, sac); c.M.Set(-13, 5, 5, Pal.C("8a7048")); c.M.Set(-13, 6, 5, sac);
            c.M.Ellipsoid(-12f, 2f, 1.5f, 2.2f, 2.3f, 2f, Pal.Shade(sac, 0.9f));
            // arrière : un drap tendu
            for (int x = -9; x <= 8; x++) for (int y = 8; y <= 17; y++) c.M.Set(x, y, -5, Pal.Shade(Pal.C("6a7a9a"), ChantierBat.Mod(x, 3) == 0 ? 0.85f : 1f));
            return c.Rig("etal", 0.1f);
        }

        // ------------------------------------------------------------------ pont
        static VoxelRig Pont(int graine)
        {
            var c = new ChantierBat(graine) { Assise = 3, Longueur = 4, Mousse = 0.2f };
            int L = 22;   // demi-longueur (z)
            for (int z = -L; z < L; z++)
            {
                float t = (z + 0.5f) / L;
                int yTab = 6 + Mathf.RoundToInt(4f * (1f - t * t));
                // arche de pierre (deux têtes de pont et voûte)
                for (int x = -9; x <= 8; x++) for (int y = 0; y <= yTab - 1; y++)
                {
                    float dz = z + 0.5f, dy = y + 0.5f;
                    if (dz * dz / (13f * 13f) + dy * dy / (7.5f * 7.5f) < 1f) continue;   // voûte
                    c.Pierre(x, y, z, x, y, z, VcPierre);
                }
                // tablier de planches
                for (int x = -8; x <= 7; x++) c.M.Set(x, yTab, z, Pal.Shade(Pal.C("8b6040"), (ChantierBat.Mod(z, 2) == 0 ? 1f : 0.85f) * (0.94f + 0.1f * ChantierBat.H(x / 4, z, 2))));
                // parapets
                foreach (int x in new[] { -9, 8 }) c.Pierre(x, yTab, z, x, yTab + 3, z, VcPierreSombre);
            }
            foreach (int x in new[] { -9, 8 }) foreach (int z in new[] { -L, L - 1 }) c.Pierre(x, 0, z, x, 11, z, VcPierreSombre);
            // clé de voûte
            c.M.Set(-10, 8, 0, Pal.Shade(VcPierre, 1.15f)); c.M.Set(9, 8, 0, Pal.Shade(VcPierre, 1.15f));
            c.M.Set(-10, 8, -1, Pal.Shade(VcPierre, 1.15f)); c.M.Set(9, 8, -1, Pal.Shade(VcPierre, 1.15f));
            return c.Rig("pont", 0.1f);
        }
    }
}

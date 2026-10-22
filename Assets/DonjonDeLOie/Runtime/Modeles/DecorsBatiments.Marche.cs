// Donjon de l'Oie — le marché de Valcreux (le diorama du mini-jeu de l'évadé, et quelques décors de plus pour les
// cartes) : le socle du diorama, rue pavée et coupe de terrain sur une plinthe de bois ; l'étal du marché, haut sous
// son auvent pour qu'on voie le marchand ; caisses, tonneaux et sacs de 90 cm (juste de quoi cacher un homme jusqu'au
// menton) ; la charrette de foin ; le panneau de l'avis de recherche. 1 voxel = 10 cm (le panneau : 5 cm), origine au
// sol au centre, face avant vers +z.
using System;
using UnityEngine;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static partial class DecorsModeles
    {
        static readonly Color32 MaPave = Pal.C("8d8a84"), MaPaveSombre = Pal.C("6e6a64"), MaJoint = Pal.C("4e4a45"),
            MaDalle = Pal.C("b0a58e"), MaCadre = Pal.C("5a3a24"), MaCadreClair = Pal.C("8a5c36"), MaPlanche = Pal.C("9a7048"),
            MaFoin = Pal.C("d8b860"), MaFoinSombre = Pal.C("b8963e"), MaToile = Pal.C("d8c8a0"), MaTerre = Pal.C("7a5a3c"),
            MaSable = Pal.C("b49a6a"), MaJute = Pal.C("a88a5a");
        static readonly Color32[] MaRayures = { Pal.C("c25a3a"), Pal.C("3e6aa8"), Pal.C("5e8a3a"), Pal.C("c9a03a"), Pal.C("8a4a8a") };

        /// Hauteur des cachettes (caisses, tonneaux) : un homme debout derrière ne montre que la tête.
        public const float HauteurCachette = 0.9f;

        /// Le socle du diorama : largeur × profondeur voxels ; trottoir dallé au fond (z ≤ trottoir), bordure, rue pavée
        /// avec son caniveau ; sur les côtés, la coupe du terrain (lit de sable, terre caillouteuse) et une plinthe de bois.
        public static VoxelRig PlateauMarche(int largeur, int profondeur, int trottoir)
        {
            var m = new VoxelModel();
            int x0 = -largeur / 2, x1 = x0 + largeur - 1, z0 = -profondeur / 2, z1 = z0 + profondeur - 1;
            int caniveau = (trottoir + z1) / 2;
            for (int x = x0; x <= x1; x++)
                for (int z = z0; z <= z1; z++)
                {
                    Color32 dessus;
                    if (z <= trottoir)
                    {
                        bool joint = ChantierBat.Mod(x, 5) == 0 || ChantierBat.Mod(z - trottoir, 5) == 0;
                        dessus = joint ? Pal.Shade(MaDalle, 0.8f) : Pal.Var(MaDalle, ChantierBat.Div(x, 5), 3, ChantierBat.Div(z, 5), 0.06f);
                    }
                    else if (z == trottoir + 1) dessus = Pal.Var(Pal.C("9a958c"), x / 3, 1, 0, 0.05f);          // la bordure
                    else if (z == caniveau || z == caniveau + 1) dessus = Pal.Var(MaPaveSombre, x / 2, 2, z, 0.05f);
                    else
                    {
                        // pavés en quinconce (3 × 2) aux joints sombres, quelques-uns plus foncés
                        int rang = ChantierBat.Div(z, 2), dec = ChantierBat.Mod(rang, 2);
                        bool joint = ChantierBat.Mod(z, 2) == 1 || ChantierBat.Mod(x + dec, 3) == 0;
                        int pave = ChantierBat.Div(x + dec, 3) * 31 + rang * 7;
                        dessus = joint ? MaJoint : (Pal.Hash01(pave, 1, 2) < 0.22f ? MaPaveSombre : Pal.Var(MaPave, pave, 0, rang, 0.07f));
                    }
                    m.Set(x, -1, z, dessus);
                    if (z == trottoir + 1) m.Set(x, 0, z, Pal.Shade(dessus, 1.05f));
                    m.Set(x, -2, z, Pal.Var(MaSable, x, -2, z, 0.08f));
                    for (int y = -5; y <= -3; y++)
                    {
                        float h = Pal.Hash01(x, y, z);
                        m.Set(x, y, z, h < 0.1f ? Pal.Var(Pal.PierreSombre, x, y, z, 0.1f) : h < 0.18f ? Pal.Shade(MaTerre, 0.8f) : Pal.Var(MaTerre, x, y, z, 0.06f));
                    }
                }
            // la plinthe déborde d'un voxel tout autour ; un filet clair à son sommet
            for (int x = x0 - 1; x <= x1 + 1; x++)
                for (int z = z0 - 1; z <= z1 + 1; z++)
                    for (int y = -8; y <= -6; y++)
                        m.Set(x, y, z, y == -6 ? MaCadreClair : Pal.Shade(MaCadre, 0.92f + 0.08f * ((x + z) & 1)));
            return VoxelRig.Single("plateau_marche", m, 0.1f, Vector3.zero);
        }

        // ------------------------------------------------------------------ l'étal du marché

        /// Comptoir, auvent rayé sur quatre poteaux (de 2,2 m au fond à 2 m devant), marchandises selon la graine :
        /// 0 fruits et légumes, 1 poissons, 2 poteries, 3 étoffes, 4 pains et fromages.
        static VoxelRig EtalMarche(int graine)
        {
            var c = new ChantierBat(graine);
            var R = c.R;
            int genre = ChantierBat.Mod(graine, 5);
            c.Bois(-9, 0, 1, 8, 6, 4, Pal.C("8b5e3c"), 'x');
            c.Bois(-10, 7, 0, 9, 7, 5, Pal.C("a07048"), 'z', 1);
            foreach (int x in new[] { -10, 9 }) { c.Poutre(x, 0, 5, x, 20, 5, Pal.BoisSombre); c.Poutre(x, 0, -7, x, 22, -7, Pal.BoisSombre); }
            var r1 = MaRayures[R.Next(MaRayures.Length)]; var r2 = Pal.C("ece2c8");
            for (int x = -11; x <= 10; x++)
                for (int z = -8; z <= 7; z++)
                {
                    int y = 22 - (z + 8) / 8;
                    var col = ChantierBat.Mod(x + 11, 4) < 2 ? r1 : r2;
                    c.M.Set(x, y, z, col);
                    if (z == 7) { c.M.Set(x, y - 1, z, col); if (ChantierBat.Mod(x, 2) == 0) c.M.Set(x, y - 2, z, col); }
                }
            // une lanterne à chaque poteau de devant
            foreach (int x in new[] { -10, 9 }) { c.M.Set(x, 17, 6, Pal.FerSombre); c.Lueur("lueur").Set(x, 16, 6, Pal.C("ffcf6a")); c.M.Set(x, 15, 6, Pal.FerSombre); }
            Marchandises(c, genre);
            return c.Rig("etal_marche", 0.1f);
        }

        /// Les marchandises sur le comptoir (y 8 et plus, vers l'avant, assez basses pour qu'on voie le marchand).
        static void Marchandises(ChantierBat c, int genre)
        {
            var R = c.R;
            switch (genre)
            {
                case 0:
                {
                    Color32[] fruits = { Pal.C("c23a2a"), Pal.C("a8b83a"), Pal.C("6a9a3a"), Pal.C("e07a2a") };
                    for (int k = 0; k < 4; k++)
                    {
                        int xa = -9 + k * 5;
                        c.Bois(xa, 8, 2, xa + 3, 8, 5, Pal.C("b08a4e"), 'x', 1);
                        for (int x = xa; x <= xa + 3; x++) for (int z = 2; z <= 5; z++)
                            if (Pal.Hash01(x, k, z) < 0.8f) c.M.Set(x, 9, z, Pal.Var(fruits[k], x, 9, z, 0.08f));
                        c.M.Set(xa + 1, 10, 3, fruits[k]); c.M.Set(xa + 2, 10, 4, fruits[k]);
                    }
                    break;
                }
                case 1:
                    c.Poutre(-9, 8, 1, 8, 8, 5, Pal.C("dfeef2"));
                    for (int k = 0; k < 6; k++)
                    {
                        int z = 1 + (k % 3) + (k / 3);
                        int xa = -8 + k * 3;
                        for (int x = xa; x <= xa + 3; x++) c.M.Set(x, 9, z, x == xa ? Pal.C("6a7a86") : Pal.C("aab8c0"));
                        c.M.Set(xa + 1, 9, z + 1, Pal.C("5a6a76"));
                    }
                    break;
                case 2:
                    for (int k = 0; k < 6; k++)
                    {
                        int x = -8 + k * 3, h = 2 + R.Next(2);
                        var terre = Pal.Shade(Pal.C("b8643a"), 0.85f + 0.3f * (float)R.NextDouble());
                        var t = new VoxelModel(); t.Cylinder(x, 3 + (k % 2), 8, 7 + h, 1.4f, terre);
                        c.M.Merge(t, 0, 0, 0);
                        c.M.Set(x, 8 + h, 3 + (k % 2), Pal.Shade(terre, 0.6f));
                    }
                    break;
                case 3:
                    for (int k = 0; k < 5; k++)
                    {
                        var etoffe = MaRayures[(k + R.Next(2)) % MaRayures.Length];
                        int x = -8 + k * 4;
                        for (int z = 1; z <= 5; z++) { c.M.Box(x, 8, z, x + 2, 9, z, Pal.Shade(etoffe, z % 2 == 0 ? 1f : 0.85f)); }
                        if (k % 2 == 0) c.M.Box(x, 10, 2, x + 2, 10, 5, Pal.Shade(etoffe, 1.15f));
                    }
                    break;
                default:
                    for (int k = 0; k < 4; k++)
                    {
                        int x = -8 + k * 5;
                        c.M.Ellipsoid(x + 1, 8.6f, 3, 1.8f, 1.1f, 1.4f, Pal.C("c88a3e"));
                        c.M.Set(x + 1, 9, 4, Pal.C("e8b060"));
                        var fromage = new VoxelModel(); fromage.Cylinder(x + 3.5f, 4, 8, 8, 1.3f, Pal.C("e8c84a"));
                        c.M.Merge(fromage, 0, 0, 0);
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ les cachettes

        /// Deux caisses empilées (1 m de large, 90 cm de haut, 60 cm de fond), quelques fruits aux deux bouts.
        static VoxelRig Caisses(int graine)
        {
            var c = new ChantierBat(graine);
            var R = c.R;
            Caisse(c, -5, 0, -3, 4, 4, 2, MaPlanche);
            Caisse(c, -5, 5, -3, 4, 8, 1, Pal.Shade(MaPlanche, 0.88f + 0.1f * (float)R.NextDouble()));
            Color32 a = R.Next(2) == 0 ? Pal.C("7aa83e") : Pal.C("c23a2a");
            c.M.Set(-5, 9, -2, a); c.M.Set(-4, 9, -2, Pal.Shade(a, 0.85f)); c.M.Set(4, 9, -1, a);
            return c.Rig("caisses", 0.1f);
        }

        static void Caisse(ChantierBat c, int x0, int y0, int z0, int x1, int y1, int z1, Color32 b)
        {
            c.Bois(x0, y0, z0, x1, y1, z1, b, 'y', 2);
            var cadre = Pal.Shade(b, 0.7f);
            for (int x = x0; x <= x1; x++) { c.M.Set(x, y0, z1, cadre); c.M.Set(x, y1, z1, cadre); c.M.Set(x, y0, z0, cadre); c.M.Set(x, y1, z0, cadre); }
            for (int y = y0; y <= y1; y++) { c.M.Set(x0, y, z1, cadre); c.M.Set(x1, y, z1, cadre); c.M.Set(x0, y, z0, cadre); c.M.Set(x1, y, z0, cadre); }
            for (int k = 0; k <= Math.Min(x1 - x0, y1 - y0); k++) c.M.Set(x0 + k, y0 + k, z1 + 1, cadre);      // écharpe en diagonale
        }

        /// Deux tonneaux debout (80 cm) et une planche posée dessus (90 cm).
        static VoxelRig Tonneaux(int graine)
        {
            var c = new ChantierBat(graine);
            Tonneau(c, -3, 0, 0, 3, 8, false);
            Tonneau(c, 3, 0, 0, 3, 8, false);
            c.Bois(-6, 8, -3, 5, 8, 2, Pal.Shade(MaPlanche, 1.05f), 'z', 1);
            c.M.Set(-4, 9, -1, Pal.C("8a6a4a")); c.M.Set(3, 9, 0, Pal.C("c9b07a"));         // une écuelle, un torchon
            return c.Rig("tonneaux", 0.1f);
        }

        /// Une pile de sacs de grain en toile de jute, couchés sur trois rangs (un mètre de haut).
        static VoxelRig Sacs(int graine)
        {
            var c = new ChantierBat(graine);
            var R = c.R;
            // des coussins de toile (superellipsoïdes) : trois au sol, trois au milieu, deux en haut, un peu décalés
            float[][] pos =
            {
                new[] { -3.3f, 1.6f, 0.2f }, new[] { 0f, 1.6f, -0.2f }, new[] { 3.3f, 1.6f, 0.1f },
                new[] { -3.1f, 4.8f, 0f }, new[] { 0.2f, 4.8f, 0.2f }, new[] { 3.2f, 4.8f, -0.1f },
                new[] { -1.6f, 7.9f, 0.1f }, new[] { 1.7f, 7.9f, -0.1f },
            };
            foreach (var p in pos)
            {
                var jute = Pal.Shade(MaJute, 0.84f + 0.24f * (float)R.NextDouble());
                Coussin(c.M, p[0], p[1], p[2], 1.75f, 1.7f, 2.7f, jute);
                int x = (int)Math.Round(p[0]), y = (int)Math.Round(p[1]);
                c.M.Set(x, y, 3, Pal.C("6a5030")); c.M.Set(x, y + 1, 3, Pal.Shade(jute, 0.62f));      // le lien, noué devant
            }
            return c.Rig("sacs", 0.1f);
        }

        /// Un sac plein, en forme de coussin : |x|⁴ + |y|⁴ + |z|⁴ ≤ 1, les coutures plus sombres.
        static void Coussin(VoxelModel m, float cx, float cy, float cz, float rx, float ry, float rz, Color32 c)
        {
            for (int x = (int)Math.Floor(cx - rx); x <= (int)Math.Ceiling(cx + rx); x++)
                for (int y = (int)Math.Floor(cy - ry); y <= (int)Math.Ceiling(cy + ry); y++)
                    for (int z = (int)Math.Floor(cz - rz); z <= (int)Math.Ceiling(cz + rz); z++)
                    {
                        float a = (x + 0.5f - cx) / rx, b = (y + 0.5f - cy) / ry, d = (z + 0.5f - cz) / rz;
                        float k = a * a * a * a + b * b * b * b + d * d * d * d;
                        if (k > 1f) continue;
                        m.Set(x, y, z, k > 0.55f ? Pal.Shade(c, 0.86f) : Pal.Var(c, x, y, z, 0.05f));
                    }
        }

        /// Une charrette de foin : plateau sur deux roues, meule jusqu'à 1,2 m, brancards devant (vers +z).
        static VoxelRig CharretteFoin(int graine)
        {
            var c = new ChantierBat(graine);
            c.Bois(-6, 4, -4, 5, 5, 4, MaPlanche, 'x', 2);
            foreach (int x in new[] { -6, 5 }) c.Bois(x, 6, -4, x, 8, 4, Pal.Shade(MaPlanche, 0.8f), 'z', 2);
            foreach (int x in new[] { -7, 6 })
                for (int y = 0; y <= 8; y++)
                    for (int z = -4; z <= 4; z++)
                    {
                        float d = Mathf.Sqrt((y - 4f) * (y - 4f) + z * z);
                        if (d <= 4.2f && d >= 3.2f) c.M.Set(x, y, z, Pal.C("5a3a24"));
                        else if (d < 0.8f) c.M.Set(x, y, z, Pal.FerSombre);
                        else if ((y == 4 || z == 0) && d < 3.2f) c.M.Set(x, y, z, Pal.C("6a4a2e"));
                    }
            for (int x = -5; x <= 4; x++)
                for (int z = -3; z <= 3; z++)
                    for (int y = 6; y <= 12; y++)
                    {
                        float h = 12f - 0.054f * (x + 0.5f) * (x + 0.5f) - 0.25f * z * z;
                        if (y <= h) c.M.Set(x, y, z, Pal.Hash01(x, y, z) < 0.3f ? MaFoinSombre : MaFoin);
                    }
            c.M.Set(-2, 13, 0, MaFoin); c.M.Set(1, 13, -1, MaFoinSombre);                     // brins qui dépassent
            c.Poutre(-4, 4, 5, -4, 5, 10, Pal.C("6a4a2e")); c.Poutre(3, 4, 5, 3, 5, 10, Pal.C("6a4a2e"));
            c.Poutre(-4, 3, 10, -4, 3, 10, Pal.C("6a4a2e")); c.Poutre(3, 3, 10, 3, 3, 10, Pal.C("6a4a2e"));
            return c.Rig("charrette_foin", 0.1f);
        }

        // ------------------------------------------------------------------ l'avis de recherche

        /// Le panneau de l'avis de recherche : deux poteaux, une planche, l'affiche (le portrait, 11 × 11 pixels, ligne 0
        /// en bas) sous un bandeau rouge. 1 voxel = 5 cm ; face vers +z.
        public static VoxelRig PanneauAvis(Color32[] portrait)
        {
            var m = new VoxelModel();
            var bois = Pal.C("6e4a2e");
            foreach (int x in new[] { -10, 9 }) m.Box(x, 0, -1, x + 1, 34, 0, bois);
            m.Box(-11, 12, 1, 11, 36, 1, Pal.Shade(bois, 1.1f));                         // la planche
            m.Box(-12, 37, -1, 12, 37, 2, Pal.Shade(bois, 0.8f));                        // un petit toit
            m.Box(-8, 14, 2, 8, 34, 2, MaToile);                                         // l'affiche
            m.Box(-8, 31, 3, 8, 33, 3, Pal.C("b8302a"));                                 // le bandeau « RECHERCHÉ »
            for (int x = -6; x <= 6; x += 2) m.Set(x, 32, 4, Pal.C("f0e0c0"));           // ses lettres (suggérées)
            for (int y = 0; y < 11; y++)
                for (int x = 0; x < 11; x++)
                {
                    var c = portrait[y * 11 + x];
                    if (c.a == 0) continue;
                    m.Set(5 - x, 18 + y, 3, c);          // vue de face : la colonne 0 de l'image est du côté des x positifs
                }
            m.Box(-6, 15, 3, 6, 16, 3, Pal.C("8a6a3a"));                                 // la récompense (une ligne d'écriture)
            m.Set(-7, 33, 4, Pal.FerSombre); m.Set(7, 33, 4, Pal.FerSombre);              // deux clous
            return VoxelRig.Single("panneau_avis", m, 0.05f, Vector3.zero);
        }
    }
}

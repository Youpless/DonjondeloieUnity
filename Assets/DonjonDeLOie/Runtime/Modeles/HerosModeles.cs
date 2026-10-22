// Donjon de l'Oie — les six héros, en voxels sculptés (style Battlegrade).
// Repère commun à toutes les pièces : 1 voxel = 5 cm, origine au sol entre les pieds, le héros regarde vers +z,
// sa droite est +x. Gabarit commun (voxels, côté droit ; la gauche est en miroir x ↔ −1 − x) :
// jambe x 1..4, y 0..8 (pivot hanche y 8) ; buste x −5..4, y 8..15, z −3..3 ; bras x 5..7, y 4..15
// (pivot épaule y 14,5, main y 4..7) ; tête x −4..3, y 16..22, visage en z = 3, yeux en y = 19.
// Chaque classe est construite dans son propre fichier partiel (HerosModeles.<Classe>.cs).
using System;
using UnityEngine;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static partial class HerosModeles
    {
        public static VoxelRig Construire(ClasseHeros c, Color32 couleur)
        {
            switch (c)
            {
                case ClasseHeros.Guerrier: return Guerrier(couleur);
                case ClasseHeros.Mage: return Mage(couleur);
                case ClasseHeros.Voleur: return Voleur(couleur);
                case ClasseHeros.Clerc: return Clerc(couleur);
                case ClasseHeros.Rodeur: return Rodeur(couleur);
                default: return Barbare(couleur);
            }
        }

        // ------------------------------------------------------------------ atelier : les pièces d'un héros

        /// Les pièces d'un héros en cours de construction, avec leurs pivots (valeurs du gabarit commun par défaut).
        sealed class Atelier
        {
            public readonly VoxelModel Corps = new VoxelModel(), Tete = new VoxelModel();
            public readonly VoxelModel BrasG = new VoxelModel(), BrasD = new VoxelModel();
            public readonly VoxelModel JambeG = new VoxelModel(), JambeD = new VoxelModel();
            public readonly VoxelModel Arme = new VoxelModel(), Bouclier = new VoxelModel(), Cape = new VoxelModel();
            public readonly VoxelModel Lueur = new VoxelModel();
            public string NomLueur = "lueur_arme";
            public Vector3 PCorps = new Vector3(0, 8, 0.5f), PTete = new Vector3(0, 16, 0.5f);
            public Vector3 PBrasD = new Vector3(6.5f, 14.5f, 0.5f), PBrasG = new Vector3(-6.5f, 14.5f, 0.5f);
            public Vector3 PJambeD = new Vector3(3f, 8, 0.5f), PJambeG = new Vector3(-3f, 8, 0.5f);
            public Vector3 PArme = new Vector3(6.5f, 5.5f, 0.5f), PBouclier = new Vector3(-6.5f, 5.5f, 0.5f);
            public Vector3 PCape = new Vector3(0, 15.5f, -3.5f);
        }

        static VoxelRig Assembler(string nom, Atelier a)
        {
            var r = new VoxelRig { Name = nom, VoxelSize = 0.05f, Origin = Vector3.zero };
            r.Add("corps", a.Corps, a.PCorps, null);
            r.Add("tete", a.Tete, a.PTete, "corps");
            r.Add("brasG", a.BrasG, a.PBrasG, "corps");
            r.Add("brasD", a.BrasD, a.PBrasD, "corps");
            r.Add("jambeG", a.JambeG, a.PJambeG, "corps");
            r.Add("jambeD", a.JambeD, a.PJambeD, "corps");
            if (a.Arme.Count > 0) r.Add("arme", a.Arme, a.PArme, "brasD");
            if (a.Bouclier.Count > 0) r.Add("bouclier", a.Bouclier, a.PBouclier, "brasG");
            if (a.Cape.Count > 0) r.Add("cape", a.Cape, a.PCape, "corps");
            if (a.Lueur.Count > 0) r.Add(a.NomLueur, a.Lueur, a.PArme, "arme");
            return r;
        }

        // ------------------------------------------------------------------ outils de sculpture

        const int AretesV = 1, AretesHaut = 2, AretesBas = 4, AretesAvant = 8;

        /// Boîte taillée : la boîte pleine, puis les arêtes choisies retirées (chanfrein d'un voxel).
        /// AretesV : les 4 arêtes verticales ; AretesHaut / AretesBas : le pourtour de la face du dessus / du dessous ;
        /// AretesAvant : le pourtour de la face avant (+z).
        static void Taille(VoxelModel m, int x0, int y0, int z0, int x1, int y1, int z1, Color32 c, int aretes)
        {
            m.Box(x0, y0, z0, x1, y1, z1, c);
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
            {
                bool bx = x == x0 || x == x1, by0 = y == y0, by1 = y == y1, bz = z == z0 || z == z1;
                bool enleve = false;
                if ((aretes & AretesV) != 0 && bx && bz) enleve = true;
                if ((aretes & AretesHaut) != 0 && by1 && (bx || bz)) enleve = true;
                if ((aretes & AretesBas) != 0 && by0 && (bx || bz)) enleve = true;
                if ((aretes & AretesAvant) != 0 && z == z1 && (bx || by0 || by1)) enleve = true;
                if (enleve) m.Remove(x, y, z);
            }
        }

        /// Repeint les voxels existants d'une boîte.
        static void Peint(VoxelModel m, int x0, int y0, int z0, int x1, int y1, int z1, Color32 c)
        {
            if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
            if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
            if (z0 > z1) { int t = z0; z0 = z1; z1 = t; }
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
                if (m.Has(x, y, z)) m.Set(x, y, z, c);
        }

        /// Retire les voxels d'une boîte.
        static void Vide(VoxelModel m, int x0, int y0, int z0, int x1, int y1, int z1)
        {
            for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
                for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
                    for (int z = Math.Min(z0, z1); z <= Math.Max(z0, z1); z++) m.Remove(x, y, z);
        }

        /// Copie tournée d'un modèle autour d'un pivot (échantillonnage inverse : pas de trous).
        static VoxelModel Pivoter(VoxelModel src, Vector3 pivot, Quaternion q)
        {
            var res = new VoxelModel();
            Vector3Int a, b;
            if (!src.Bounds(out a, out b)) return res;
            // boîte englobante de la copie tournée
            var mn = new Vector3(1e9f, 1e9f, 1e9f); var mx = -mn;
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? a.x : b.x + 1, (i & 2) == 0 ? a.y : b.y + 1, (i & 4) == 0 ? a.z : b.z + 1);
                var r = q * (p - pivot) + pivot;
                mn = Vector3.Min(mn, r); mx = Vector3.Max(mx, r);
            }
            var inv = Quaternion.Inverse(q);
            for (int x = (int)Math.Floor(mn.x) - 1; x <= (int)Math.Ceiling(mx.x); x++)
                for (int y = (int)Math.Floor(mn.y) - 1; y <= (int)Math.Ceiling(mx.y); y++)
                    for (int z = (int)Math.Floor(mn.z) - 1; z <= (int)Math.Ceiling(mx.z); z++)
                    {
                        var s = inv * (new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) - pivot) + pivot;
                        Color32 c;
                        if (src.TryGet((int)Math.Floor(s.x), (int)Math.Floor(s.y), (int)Math.Floor(s.z), out c)) res.Set(x, y, z, c);
                    }
            return res;
        }

        /// Copie miroir d'un modèle (x ↔ −1 − x) : la pièce droite devient la gauche.
        static void Miroir(VoxelModel src, VoxelModel dst)
        {
            foreach (var kv in src.Tous()) dst.Set(-1 - kv.Key.x, kv.Key.y, kv.Key.z, kv.Value);
        }

        /// Symétrise un modèle construit sur sa moitié droite (x ≥ 0).
        static void Symetrise(VoxelModel m)
        {
            m.Carve((x, y, z) => x < 0);
            m.MirrorX(0);
        }


        // ------------------------------------------------------------------ gabarit commun : tête, jambes, bras

        /// Tête nue taillée : x −4..3, y 16..22, z −3..3 ; visage sur le plan z = 3, yeux en y = 19.
        static void TeteNue(VoxelModel t, Color32 peau, Color32 sourcils)
        {
            Taille(t, -4, 16, -3, 3, 22, 3, peau, AretesV | AretesHaut | AretesBas);
            t.Box(-4, 16, 3, 3, 16, 3, peau);                         // mâchoire franche devant
            t.Remove(-4, 16, 3); t.Remove(3, 16, 3);
            t.Box(-5, 18, 0, -5, 19, 0, Pal.PeauOmbre); t.Box(4, 18, 0, 4, 19, 0, Pal.PeauOmbre);   // oreilles
            Visage(t, 19, 3, sourcils, true);
        }

        /// Jambe droite (x 1..4) : chausse jusqu'à y 7 (le bas du buste couvre la hanche), botte jusqu'à hautBotte, pointe du pied en z = 3.
        static void Jambe(VoxelModel j, Color32 chausse, Color32 botte, int hautBotte, Color32 semelle)
        {
            Taille(j, 1, 2, -1, 4, 7, 1, chausse, AretesV);
            Taille(j, 1, 0, -2, 4, hautBotte, 2, botte, AretesV);
            j.Box(1, 0, 3, 4, 1, 3, botte); j.Remove(1, 1, 3); j.Remove(4, 1, 3);
            j.Box(1, 0, -2, 4, 0, 3, semelle);
        }

        /// Bras droit (x 5..7) : manche jusqu'au poignet (y 8), main y 4..7 (z −1..2), un peu grosse.
        static void Bras(VoxelModel b, Color32 manche, Color32 main)
        {
            Taille(b, 5, 8, -1, 7, 15, 1, manche, AretesV | AretesHaut);
            Taille(b, 5, 4, -1, 7, 7, 2, main, AretesV | AretesBas);
            Peint(b, 5, 4, -1, 7, 4, 2, Sh(main, 0.85f));
        }


        /// Capuche taillée (section octogonale qui se resserre en haut), ouverte devant sur le visage.
        /// À appeler avant de dessiner la tête : elle évide l'emplacement de la tête. pointe : longueur de la pointe dans la nuque.
        static void Capuche(VoxelModel t, Color32 c, Color32 cS, int pointe)
        {
            for (int y = 16; y <= 25; y++)
            {
                float h = y <= 21 ? 5.5f : y == 22 ? 5f : y == 23 ? 4.5f : y == 24 ? 3.5f : 2f;
                for (int x = -6; x <= 5; x++) for (int z = -7; z <= 6; z++)
                {
                    float dx = Mathf.Abs(x + 0.5f), dz = Mathf.Abs(z + 0.5f + (y >= 23 ? 0.6f : 0f));
                    if (dx <= h && dz <= h && dx + dz <= h * 1.45f) t.Set(x, y, z, y == 25 ? cS : c);
                }
            }
            // ouverture du visage et place de la tête
            Vide(t, -4, 16, -3, 3, 22, 3);
            Vide(t, -3, 16, 3, 2, 21, 6); Vide(t, -4, 16, 3, 3, 18, 6);
            // rebord sombre autour de l'ouverture (l'ombre de la capuche)
            t.Paint((x, y, z, col) => (z >= 3 && y <= 22) ? cS : col);
            for (int i = 0; i < pointe; i++) t.Box(-1, 21 - i * 2, -6 - i, 0, 23 - i * 2, -6 - i, i == pointe - 1 ? cS : c);
        }

        static Color32 Sh(Color32 c, float f) { return Pal.Shade(c, f); }

        /// Visage commun : yeux (2 voxels sombres), nez, sourcils. z = plan du visage.
        static void Visage(VoxelModel t, int yYeux, int z, Color32 sourcils, bool nez)
        {
            var oeil = Pal.C("221c1c");
            t.Set(-2, yYeux, z, oeil); t.Set(1, yYeux, z, oeil);
            // une étincelle claire au-dessus de l'œil côté soleil, pour l'expression
            if (sourcils.a > 0) { t.Box(-3, yYeux + 1, z, -2, yYeux + 1, z, sourcils); t.Box(1, yYeux + 1, z, 2, yYeux + 1, z, sourcils); }
            if (nez) { t.Box(-1, yYeux - 1, z + 1, 0, yYeux, z + 1, Pal.PeauOmbre); t.Box(-1, yYeux - 1, z + 1, 0, yYeux - 1, z + 1, Pal.Peau); }
        }
    }
}

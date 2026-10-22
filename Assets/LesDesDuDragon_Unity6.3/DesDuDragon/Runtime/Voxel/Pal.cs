// Les Dés du Dragon — couleurs : lecture hexadécimale, mélanges, nuances.
using System;
using UnityEngine;

namespace DesDuDragon.Voxel
{
    public static class Pal
    {
        /// "#a1b2c3" ou "a1b2c3".
        public static Color32 C(string hex)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            byte r = Convert.ToByte(hex.Substring(0, 2), 16), g = Convert.ToByte(hex.Substring(2, 2), 16), b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color32(r, g, b, 255);
        }

        public static Color32 Mix(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);
        }

        /// f < 1 assombrit, f > 1 éclaircit.
        public static Color32 Shade(Color32 c, float f)
        {
            return new Color32((byte)Mathf.Clamp(c.r * f, 0, 255), (byte)Mathf.Clamp(c.g * f, 0, 255), (byte)Mathf.Clamp(c.b * f, 0, 255), c.a);
        }

        /// Petite variation déterministe d'une couleur selon une position (texture de pierre, d'écorce…).
        public static Color32 Var(Color32 c, int x, int y, int z, float ampleur)
        {
            return Shade(c, 1f + (Hash01(x, y, z) - 0.5f) * 2f * ampleur);
        }

        public static float Hash01(int x, int y, int z)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(z * 83492791);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return (h & 0xffffff) / (float)0x1000000;
            }
        }

        // Une palette commune, chaude et un peu désaturée, dans l'esprit des cartes de Battlegrade.
        public static readonly Color32 Peau = C("e0ac86"), PeauOmbre = C("b9805f"), PeauClaire = C("f0c7a2");
        public static readonly Color32 Cheveux = C("5a3a22"), Barbe = C("8a5a32");
        public static readonly Color32 Fer = C("9aa3ab"), FerSombre = C("5f676f"), Acier = C("c4ccd3");
        public static readonly Color32 Or = C("e8b64a"), OrSombre = C("a77a23");
        public static readonly Color32 Cuir = C("7a4e2d"), CuirSombre = C("543320");
        public static readonly Color32 Bois = C("8b5e3c"), BoisSombre = C("5e3d26");
        public static readonly Color32 Pierre = C("8d8a84"), PierreSombre = C("5f5d59"), PierreClaire = C("b3afa6");
        public static readonly Color32 Herbe = C("6f9a43"), HerbeSombre = C("4c7330"), HerbeClaire = C("8fb555");
        public static readonly Color32 Terre = C("8a6a48"), Sable = C("d6c08a");
        public static readonly Color32 Eau = C("3f7fa6"), EauProfonde = C("2a587a");
        public static readonly Color32 Os = C("e6dcc3"), Noir = C("1c1a1e"), Blanc = C("f4f1ea");
        public static readonly Color32 Rouge = C("b8392f"), Bleu = C("3d64b0"), Vert = C("3f8a46"), Violet = C("6e4aa0");

        /// Couleurs des six joueurs (blasons, capes, socles).
        public static readonly Color32[] Joueurs =
        {
            C("d9483b"), // rouge
            C("3f78d1"), // bleu
            C("49a84f"), // vert
            C("e8b335"), // jaune
            C("9a5bd1"), // violet
            C("e07b2e"), // orange
        };

        public static readonly string[] NomsCouleurs = { "Rouge", "Bleu", "Vert", "Jaune", "Violet", "Orange" };
    }
}

// Les Dés du Dragon — transforme un VoxelModel en maillage : seules les faces visibles sont gardées,
// avec une occlusion ambiante par sommet (les creux s'assombrissent) et un léger grain de couleur par voxel.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesDuDragon.Voxel
{
    public sealed class MeshData
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Color32> C = new List<Color32>();
        public readonly List<int> T = new List<int>();

        public int Triangles { get { return T.Count / 3; } }

        /// Ajoute un autre maillage, tourné autour de y (degrés), mis à l'échelle et déplacé.
        public void Append(MeshData o, Vector3 decalage, float rotY = 0f, float echelle = 1f)
        {
            var q = Quaternion.AngleAxis(rotY, Vector3.up);
            int b = V.Count;
            for (int i = 0; i < o.V.Count; i++)
            {
                V.Add(q * (o.V[i] * echelle) + decalage);
                N.Add(q * o.N[i]);
                C.Add(o.C[i]);
            }
            for (int i = 0; i < o.T.Count; i++) T.Add(o.T[i] + b);
        }

        public void Append(MeshData o, Matrix4x4 m)
        {
            int b = V.Count;
            for (int i = 0; i < o.V.Count; i++)
            {
                V.Add(m.MultiplyPoint3x4(o.V[i]));
                N.Add(m.MultiplyVector(o.N[i]).normalized);
                C.Add(o.C[i]);
            }
            for (int i = 0; i < o.T.Count; i++) T.Add(o.T[i] + b);
        }

        public void Teinter(Func<Color32, Color32> f)
        {
            for (int i = 0; i < C.Count; i++) C[i] = f(C[i]);
        }
    }

    public static class VoxelMesher
    {
        static readonly float[] Ao = { 0.52f, 0.68f, 0.84f, 1f };

        // Pour chaque face : normale, puis deux axes tangents (u, v) et l'ordre des coins.
        static readonly int[][] Normales = { new[] { 1, 0, 0 }, new[] { -1, 0, 0 }, new[] { 0, 1, 0 }, new[] { 0, -1, 0 }, new[] { 0, 0, 1 }, new[] { 0, 0, -1 } };

        /// Construit le maillage. pivot : point du repère voxel qui devient l'origine du maillage. taille : mètres par voxel.
        public static MeshData Build(VoxelModel m, Vector3 pivot, float taille, float grain = 0.035f, bool occlusion = true)
        {
            var md = new MeshData();
            AppendTo(md, m, pivot, taille, grain, occlusion, null);
            return md;
        }

        /// Comme Build, mais une fonction peut dire qu'une face donnant sur (x, y, z) est cachée (voxels d'un voisin, terrain).
        public static void AppendTo(MeshData md, VoxelModel m, Vector3 pivot, float taille, float grain, bool occlusion, Func<int, int, int, bool> pleinDehors)
        {
            foreach (var kv in m.Tous())
            {
                int x = kv.Key.x, y = kv.Key.y, z = kv.Key.z;
                var col = kv.Value;
                if (grain > 0) col = Pal.Var(col, x, y, z, grain);
                for (int f = 0; f < 6; f++)
                {
                    var n = Normales[f];
                    int nx = x + n[0], ny = y + n[1], nz = z + n[2];
                    if (m.Has(nx, ny, nz)) continue;
                    if (pleinDehors != null && pleinDehors(nx, ny, nz)) continue;
                    Face(md, m, pleinDehors, x, y, z, f, col, pivot, taille, occlusion);
                }
            }
        }

        static bool Plein(VoxelModel m, Func<int, int, int, bool> dehors, int x, int y, int z)
        {
            return m.Has(x, y, z) || (dehors != null && dehors(x, y, z));
        }

        static void Face(MeshData md, VoxelModel m, Func<int, int, int, bool> dehors, int x, int y, int z, int f, Color32 col, Vector3 pivot, float taille, bool occlusion)
        {
            var n = Normales[f];
            // Axes tangents (u, v) tels que u × v = n, pour que les triangles regardent vers l'extérieur.
            int[] u, v;
            switch (f)
            {
                case 0: u = new[] { 0, 1, 0 }; v = new[] { 0, 0, 1 }; break;      // +x
                case 1: u = new[] { 0, 0, 1 }; v = new[] { 0, 1, 0 }; break;      // −x
                case 2: u = new[] { 0, 0, 1 }; v = new[] { 1, 0, 0 }; break;      // +y
                case 3: u = new[] { 1, 0, 0 }; v = new[] { 0, 0, 1 }; break;      // −y
                case 4: u = new[] { 1, 0, 0 }; v = new[] { 0, 1, 0 }; break;      // +z
                default: u = new[] { 0, 1, 0 }; v = new[] { 1, 0, 0 }; break;     // −z
            }
            // Coin de base de la face : sur le côté positif de la normale.
            float bx = x + (n[0] > 0 ? 1 : 0), by = y + (n[1] > 0 ? 1 : 0), bz = z + (n[2] > 0 ? 1 : 0);
            int b = md.V.Count;
            var ao = new int[4];
            // coins : (0,0) (1,0) (1,1) (0,1) en (u, v)
            for (int k = 0; k < 4; k++)
            {
                int cu = (k == 1 || k == 2) ? 1 : 0, cv = (k >= 2) ? 1 : 0;
                var p = new Vector3(bx + u[0] * cu + v[0] * cv, by + u[1] * cu + v[1] * cv, bz + u[2] * cu + v[2] * cv);
                md.V.Add((p - pivot) * taille);
                md.N.Add(new Vector3(n[0], n[1], n[2]));
                int a = 3;
                if (occlusion)
                {
                    int su = cu == 1 ? 1 : -1, sv = cv == 1 ? 1 : -1;
                    int ox = x + n[0], oy = y + n[1], oz = z + n[2];
                    bool s1 = Plein(m, dehors, ox + u[0] * su, oy + u[1] * su, oz + u[2] * su);
                    bool s2 = Plein(m, dehors, ox + v[0] * sv, oy + v[1] * sv, oz + v[2] * sv);
                    bool cc = Plein(m, dehors, ox + u[0] * su + v[0] * sv, oy + u[1] * su + v[1] * sv, oz + u[2] * su + v[2] * sv);
                    a = (s1 && s2) ? 0 : 3 - ((s1 ? 1 : 0) + (s2 ? 1 : 0) + (cc ? 1 : 0));
                }
                ao[k] = a;
                md.C.Add(Pal.Shade(col, Ao[a]));
            }
            // Diagonale choisie pour que l'occlusion se dégrade sans artefact.
            if (ao[0] + ao[2] >= ao[1] + ao[3])
            {
                md.T.Add(b); md.T.Add(b + 1); md.T.Add(b + 2);
                md.T.Add(b); md.T.Add(b + 2); md.T.Add(b + 3);
            }
            else
            {
                md.T.Add(b + 1); md.T.Add(b + 2); md.T.Add(b + 3);
                md.T.Add(b + 1); md.T.Add(b + 3); md.T.Add(b);
            }
        }
    }
}

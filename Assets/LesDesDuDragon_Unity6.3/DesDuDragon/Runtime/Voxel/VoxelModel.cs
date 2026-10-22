// Les Dés du Dragon — un modèle en voxels, construit en code (boîtes, ellipsoïdes, cylindres, cônes, traits).
// Les coordonnées sont des entiers ; un voxel occupe le cube [x, x+1] × [y, y+1] × [z, z+1].
// Convention : +y vers le haut, +z vers l'avant du personnage (il regarde vers +z), +x vers sa gauche… vue de face : +x à droite de l'écran.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesDuDragon.Voxel
{
    public sealed class VoxelModel
    {
        // Clé : 11 bits par axe, coordonnées de −1024 à 1023.
        readonly Dictionary<long, Color32> vox = new Dictionary<long, Color32>();
        const int Decal = 1024;

        static long Cle(int x, int y, int z) { return ((long)(x + Decal) << 22) + ((long)(y + Decal) << 11) + (long)(z + Decal); }
        static void Decle(long k, out int x, out int y, out int z)
        {
            x = (int)((k >> 22) & 2047) - Decal; y = (int)((k >> 11) & 2047) - Decal; z = (int)(k & 2047) - Decal;
        }

        public int Count { get { return vox.Count; } }

        public void Set(int x, int y, int z, Color32 c) { vox[Cle(x, y, z)] = c; }
        public void Remove(int x, int y, int z) { vox.Remove(Cle(x, y, z)); }
        public bool Has(int x, int y, int z) { return vox.ContainsKey(Cle(x, y, z)); }
        public bool TryGet(int x, int y, int z, out Color32 c) { return vox.TryGetValue(Cle(x, y, z), out c); }
        public void Clear() { vox.Clear(); }

        /// Parcourt tous les voxels : (x, y, z, couleur).
        public IEnumerable<KeyValuePair<Vector3Int, Color32>> Tous()
        {
            foreach (var kv in vox)
            {
                int x, y, z; Decle(kv.Key, out x, out y, out z);
                yield return new KeyValuePair<Vector3Int, Color32>(new Vector3Int(x, y, z), kv.Value);
            }
        }

        /// Boîte pleine, bornes incluses, dans n'importe quel ordre.
        public void Box(int x0, int y0, int z0, int x1, int y1, int z1, Color32 c)
        {
            if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
            if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
            if (z0 > z1) { int t = z0; z0 = z1; z1 = t; }
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++) Set(x, y, z, c);
        }

        /// Boîte creuse (seulement la coque d'épaisseur 1).
        public void Shell(int x0, int y0, int z0, int x1, int y1, int z1, Color32 c)
        {
            if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
            if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
            if (z0 > z1) { int t = z0; z0 = z1; z1 = t; }
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
                if (x == x0 || x == x1 || y == y0 || y == y1 || z == z0 || z == z1) Set(x, y, z, c);
        }

        /// Ellipsoïde plein centré en (cx, cy, cz) — les centres de voxels sont en (x + 0.5, …).
        public void Ellipsoid(float cx, float cy, float cz, float rx, float ry, float rz, Color32 c)
        {
            int x0 = (int)Math.Floor(cx - rx), x1 = (int)Math.Ceiling(cx + rx);
            int y0 = (int)Math.Floor(cy - ry), y1 = (int)Math.Ceiling(cy + ry);
            int z0 = (int)Math.Floor(cz - rz), z1 = (int)Math.Ceiling(cz + rz);
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry, dz = (z + 0.5f - cz) / rz;
                if (dx * dx + dy * dy + dz * dz <= 1f) Set(x, y, z, c);
            }
        }

        public void Sphere(float cx, float cy, float cz, float r, Color32 c) { Ellipsoid(cx, cy, cz, r, r, r, c); }

        /// Cylindre vertical de y0 à y1 inclus.
        public void Cylinder(float cx, float cz, int y0, int y1, float r, Color32 c) { Cone(cx, cz, y0, y1, r, r, c); }

        /// Tronc de cône vertical : rayon r0 en bas (y0), r1 en haut (y1).
        public void Cone(float cx, float cz, int y0, int y1, float r0, float r1, Color32 c)
        {
            if (y0 > y1) { int t = y0; y0 = y1; y1 = t; float tr = r0; r0 = r1; r1 = tr; }
            float rm = Math.Max(r0, r1);
            int x0 = (int)Math.Floor(cx - rm), x1 = (int)Math.Ceiling(cx + rm);
            int z0 = (int)Math.Floor(cz - rm), z1 = (int)Math.Ceiling(cz + rm);
            for (int y = y0; y <= y1; y++)
            {
                float t = y1 == y0 ? 0 : (y - y0) / (float)(y1 - y0);
                float r = r0 + (r1 - r0) * t;
                for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++)
                {
                    float dx = x + 0.5f - cx, dz = z + 0.5f - cz;
                    if (dx * dx + dz * dz <= r * r) Set(x, y, z, c);
                }
            }
        }

        /// Trait épais (capsule) entre deux points, rayon r (0.5 donne un trait d'un voxel).
        public void Line(float x0, float y0, float z0, float x1, float y1, float z1, float r, Color32 c)
        {
            Line(x0, y0, z0, x1, y1, z1, r, r, c);
        }

        /// Trait dont le rayon passe de r0 à r1 (cornes, queues, griffes).
        public void Line(float x0, float y0, float z0, float x1, float y1, float z1, float r0, float r1, Color32 c)
        {
            var a = new Vector3(x0, y0, z0); var b = new Vector3(x1, y1, z1);
            var ab = b - a; float l2 = Math.Max(1e-6f, ab.sqrMagnitude);
            float rm = Math.Max(r0, r1);
            int xa = (int)Math.Floor(Math.Min(x0, x1) - rm), xb = (int)Math.Ceiling(Math.Max(x0, x1) + rm);
            int ya = (int)Math.Floor(Math.Min(y0, y1) - rm), yb = (int)Math.Ceiling(Math.Max(y0, y1) + rm);
            int za = (int)Math.Floor(Math.Min(z0, z1) - rm), zb = (int)Math.Ceiling(Math.Max(z0, z1) + rm);
            for (int x = xa; x <= xb; x++) for (int y = ya; y <= yb; y++) for (int z = za; z <= zb; z++)
            {
                var p = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
                float r = r0 + (r1 - r0) * t;
                if ((a + ab * t - p).sqrMagnitude <= r * r) Set(x, y, z, c);
            }
        }

        /// Copie en miroir : chaque voxel en x va en (2·axe − 1 − x). Avec axe = 0, x = 0 ↔ x = −1 (symétrie entre deux colonnes).
        public void MirrorX(int axe = 0)
        {
            var copie = new List<KeyValuePair<long, Color32>>(vox);
            foreach (var kv in copie)
            {
                int x, y, z; Decle(kv.Key, out x, out y, out z);
                Set(2 * axe - 1 - x, y, z, kv.Value);
            }
        }

        /// Repeint les voxels existants. La fonction reçoit (x, y, z, couleur) et rend la nouvelle couleur.
        public void Paint(Func<int, int, int, Color32, Color32> f)
        {
            var cles = new List<long>(vox.Keys);
            foreach (long k in cles)
            {
                int x, y, z; Decle(k, out x, out y, out z);
                vox[k] = f(x, y, z, vox[k]);
            }
        }

        /// Retire les voxels pour lesquels le prédicat est vrai.
        public void Carve(Func<int, int, int, bool> f)
        {
            var cles = new List<long>(vox.Keys);
            foreach (long k in cles)
            {
                int x, y, z; Decle(k, out x, out y, out z);
                if (f(x, y, z)) vox.Remove(k);
            }
        }

        /// Ajoute les voxels d'un autre modèle, décalés.
        public void Merge(VoxelModel o, int dx, int dy, int dz)
        {
            foreach (var kv in o.vox)
            {
                int x, y, z; Decle(kv.Key, out x, out y, out z);
                Set(x + dx, y + dy, z + dz, kv.Value);
            }
        }

        public VoxelModel Clone() { var m = new VoxelModel(); m.Merge(this, 0, 0, 0); return m; }

        public bool Bounds(out Vector3Int min, out Vector3Int max)
        {
            min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            if (vox.Count == 0) { min = max = new Vector3Int(0, 0, 0); return false; }
            foreach (long k in vox.Keys)
            {
                int x, y, z; Decle(k, out x, out y, out z);
                if (x < min.x) min.x = x; if (y < min.y) min.y = y; if (z < min.z) min.z = z;
                if (x > max.x) max.x = x; if (y > max.y) max.y = y; if (z > max.z) max.z = z;
            }
            return true;
        }
    }

    /// Une pièce animable d'un personnage : un modèle, un pivot (dans le même repère que les voxels), un parent.
    public sealed class VoxelPart
    {
        public string Name;
        public VoxelModel Model;
        public Vector3 Pivot;       // en voxels, repère commun à toutes les pièces
        public string Parent;       // null : racine
    }

    /// Un personnage ou un objet découpé en pièces. Toutes les pièces sont modelées dans le même repère.
    /// Noms de pièces reconnus par l'animation : corps, tete, brasG, brasD, jambeG, jambeD, arme, bouclier,
    /// aileG, aileD, queue, machoire, pattesG, pattesD, cape. Les pièces manquantes sont simplement ignorées.
    public sealed class VoxelRig
    {
        public float VoxelSize = 0.05f;           // taille d'un voxel en mètres
        public Vector3 Origin;                    // point du repère (en voxels) posé au sol, au centre de la case
        public readonly List<VoxelPart> Parts = new List<VoxelPart>();
        public string Name;

        public VoxelPart Add(string name, VoxelModel m, Vector3 pivot, string parent = null)
        {
            var p = new VoxelPart { Name = name, Model = m, Pivot = pivot, Parent = parent };
            Parts.Add(p);
            return p;
        }

        public VoxelPart Get(string name)
        {
            for (int i = 0; i < Parts.Count; i++) if (Parts[i].Name == name) return Parts[i];
            return null;
        }

        /// Un rig d'une seule pièce.
        public static VoxelRig Single(string name, VoxelModel m, float voxelSize, Vector3 origin)
        {
            var r = new VoxelRig { Name = name, VoxelSize = voxelSize, Origin = origin };
            r.Add("corps", m, origin);
            return r;
        }
    }
}

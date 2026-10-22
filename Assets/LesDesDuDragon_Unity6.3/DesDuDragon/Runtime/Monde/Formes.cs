// Les Dés du Dragon — formes simples pour les effets et les marqueurs : anneau, colonne de lumière, disque, icône en voxels.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DesDuDragon.Voxel;

namespace DesDuDragon.Monde
{
    public static class Formes
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static GameObject Objet(string nom, Transform parent, Mesh m, Material mat)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// Anneau à plat (dans le plan xz), en couleur de sommets.
        public static GameObject Anneau(Transform parent, float r0, float r1, Color c, Material mat)
        {
            string cle = "anneau_" + r0 + "_" + r1 + "_" + ((Color32)c).r + "_" + ((Color32)c).g + "_" + ((Color32)c).b + "_" + ((Color32)c).a;
            Mesh mc;
            if (cache.TryGetValue(cle, out mc) && mc != null) return Objet("Anneau", parent, mc, mat);
            var md = new MeshData();
            const int n = 48;
            Color32 cc = c;
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                md.V.Add(d * r0); md.V.Add(d * r1);
                md.N.Add(Vector3.up); md.N.Add(Vector3.up);
                md.C.Add(cc); md.C.Add(cc);
                if (i < n)
                {
                    int k = i * 2;
                    md.T.Add(k); md.T.Add(k + 1); md.T.Add(k + 3);
                    md.T.Add(k); md.T.Add(k + 3); md.T.Add(k + 2);
                }
            }
            mc = DDMateriaux.Maillage(md, cle);
            cache[cle] = mc;
            return Objet("Anneau", parent, mc, mat);
        }

        /// Disque plein (ombre portée sous un pion, socle).
        public static GameObject Disque(Transform parent, float r, Color centre, Color bord, Material mat)
        {
            Color32 k0 = centre, k1 = bord;
            string cle = "disque_" + r + "_" + k0.r + "," + k0.g + "," + k0.b + "," + k0.a + "_" + k1.r + "," + k1.g + "," + k1.b + "," + k1.a;
            Mesh mc;
            if (cache.TryGetValue(cle, out mc) && mc != null) return Objet("Disque", parent, mc, mat);
            var md = new MeshData();
            const int n = 40;
            md.V.Add(Vector3.zero); md.N.Add(Vector3.up); md.C.Add(centre);
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                md.V.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)); md.N.Add(Vector3.up); md.C.Add(bord);
                if (i < n) { md.T.Add(0); md.T.Add(i + 2); md.T.Add(i + 1); }
            }
            mc = DDMateriaux.Maillage(md, cle);
            cache[cle] = mc;
            return Objet("Disque", parent, mc, mat);
        }

        /// Colonne de lumière qui s'efface vers le haut (additive).
        public static GameObject Colonne(Transform parent, float r, float h, Color c)
        {
            string cle = "colonne_" + r + "_" + h + "_" + ((Color32)c).GetHashCode();
            Mesh m;
            if (!cache.TryGetValue(cle, out m) || m == null)
            {
                var md = new MeshData();
                const int n = 24;
                Color32 bas = c, haut = new Color(c.r, c.g, c.b, 0f);
                bas = new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                haut = new Color(0, 0, 0, 0);
                for (int i = 0; i <= n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    md.V.Add(d * r); md.V.Add(d * r * 0.85f + Vector3.up * h);
                    md.N.Add(d); md.N.Add(d);
                    md.C.Add(bas); md.C.Add(haut);
                    if (i < n)
                    {
                        int k = i * 2;
                        md.T.Add(k); md.T.Add(k + 1); md.T.Add(k + 3);
                        md.T.Add(k); md.T.Add(k + 3); md.T.Add(k + 2);
                    }
                }
                m = DDMateriaux.Maillage(md, cle);
                cache[cle] = m;
            }
            return Objet("Colonne", parent, m, DDMateriaux.FxAdd);
        }

        /// Une icône en pixel art, extrudée en voxels (taille : mètres par pixel). debout : face vers −z ; sinon à plat.
        public static GameObject Icone(Transform parent, string id, float taille, bool aPlat)
        {
            string cle = "icone_" + id + "_" + taille + (aPlat ? "_plat" : "");
            Mesh m;
            if (!cache.TryGetValue(cle, out m) || m == null)
            {
                var vm = new VoxelModel();
                int n = PixelIcones.Taille, o = n / 2;
                for (int x = 0; x < n; x++)
                    for (int y = 0; y < n; y++)
                    {
                        var c = PixelIcones.Pixel(id, x, y);
                        if (c.a == 0) continue;
                        if (aPlat) vm.Set(x - o, 0, y - o, c);
                        else { vm.Set(x - o, y, 0, c); vm.Set(x - o, y, 1, Pal.Shade(c, 0.7f)); }
                    }
                m = DDMateriaux.Maillage(VoxelMesher.Build(vm, new Vector3(0.5f, aPlat ? 0 : n * 0.5f, 0.5f), taille, 0f, false), cle);
                cache[cle] = m;
            }
            var go = Objet("Icône_" + id, parent, m, DDMateriaux.Lueur(1.15f, 0f));
            return go;
        }
    }
}

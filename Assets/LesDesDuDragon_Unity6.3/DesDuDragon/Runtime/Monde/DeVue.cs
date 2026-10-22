// Les Dés du Dragon — un dé en voxels lancé sur le plateau : il roule, rebondit et s'arrête sur la valeur tirée par les règles.
// Le D6 est ivoire à points rouges ; le D3 des duels est doré, ses faces portent 1, 2 et 3 points (deux fois chacune).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DesDuDragon.Voxel;

namespace DesDuDragon.Monde
{
    public sealed class DeVue : MonoBehaviour
    {
        const int N = 10;                    // voxels par arête
        const float Taille = 0.05f;          // mètres par voxel : dé de 50 cm
        static Mesh d6, d3;

        // Faces : normale → valeur. D6 : faces opposées = 7.
        static readonly Vector3[] Normales = { Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down };
        static readonly int[] ValeursD6 = { 1, 2, 3, 4, 5, 6 };
        static readonly int[] ValeursD3 = { 1, 2, 3, 3, 2, 1 };

        public static Mesh Maillage(int faces)
        {
            if (faces == 3 && d3 != null) return d3;
            if (faces != 3 && d6 != null) return d6;
            var corpsC = faces == 3 ? Pal.C("e9bf55") : Pal.C("f1e9d6");
            var bordC = faces == 3 ? Pal.C("c9962f") : Pal.C("d9cfb8");
            var pointC = faces == 3 ? Pal.C("4a2a10") : Pal.C("b3241c");
            var m = new VoxelModel();
            for (int x = 0; x < N; x++) for (int y = 0; y < N; y++) for (int z = 0; z < N; z++)
            {
                int bx = (x == 0 || x == N - 1) ? 1 : 0, by = (y == 0 || y == N - 1) ? 1 : 0, bz = (z == 0 || z == N - 1) ? 1 : 0;
                if (bx + by + bz >= 2) continue;     // arêtes et coins arrondis
                bool surface = bx + by + bz == 1;
                m.Set(x, y, z, surface ? corpsC : bordC);
            }
            var valeurs = faces == 3 ? ValeursD3 : ValeursD6;
            for (int f = 0; f < 6; f++) Points(m, Normales[f], valeurs[f], pointC);
            var md = VoxelMesher.Build(m, new Vector3(N / 2f, N / 2f, N / 2f), Taille, 0.015f, true);
            var mesh = DDMateriaux.Maillage(md, "de" + faces);
            if (faces == 3) d3 = mesh; else d6 = mesh;
            return mesh;
        }

        /// Creuse les points d'une face (points de 2 × 2 voxels, sur une grille 3 × 3).
        static void Points(VoxelModel m, Vector3 n, int v, Color32 c)
        {
            var pos = new List<Vector2>();
            // positions sur la face, de −1 à 1
            switch (v)
            {
                case 1: pos.Add(new Vector2(0, 0)); break;
                case 2: pos.Add(new Vector2(-1, -1)); pos.Add(new Vector2(1, 1)); break;
                case 3: pos.Add(new Vector2(-1, -1)); pos.Add(new Vector2(0, 0)); pos.Add(new Vector2(1, 1)); break;
                case 4: pos.Add(new Vector2(-1, -1)); pos.Add(new Vector2(1, 1)); pos.Add(new Vector2(-1, 1)); pos.Add(new Vector2(1, -1)); break;
                case 5: pos.Add(new Vector2(-1, -1)); pos.Add(new Vector2(1, 1)); pos.Add(new Vector2(-1, 1)); pos.Add(new Vector2(1, -1)); pos.Add(new Vector2(0, 0)); break;
                default: pos.Add(new Vector2(-1, -1)); pos.Add(new Vector2(1, 1)); pos.Add(new Vector2(-1, 1)); pos.Add(new Vector2(1, -1)); pos.Add(new Vector2(-1, 0)); pos.Add(new Vector2(1, 0)); break;
            }
            foreach (var p in pos)
            {
                // centre du point : 5 ± 3 voxels ; un point = 2 × 2 voxels creusés d'un cran
                int u0 = 4 + (int)p.x * 3, w0 = 4 + (int)p.y * 3;
                for (int du = 0; du < 2; du++)
                    for (int dw = 0; dw < 2; dw++)
                    {
                        int u = u0 + du, w = w0 + dw;
                        int x, y, z, x2, y2, z2;
                        if (n == Vector3.up) { x = u; z = w; y = N - 1; x2 = x; y2 = y - 1; z2 = z; }
                        else if (n == Vector3.down) { x = u; z = w; y = 0; x2 = x; y2 = 1; z2 = z; }
                        else if (n == Vector3.right) { z = u; y = w; x = N - 1; x2 = x - 1; y2 = y; z2 = z; }
                        else if (n == Vector3.left) { z = u; y = w; x = 0; x2 = 1; y2 = y; z2 = z; }
                        else if (n == Vector3.forward) { x = u; y = w; z = N - 1; x2 = x; y2 = y; z2 = z - 1; }
                        else { x = u; y = w; z = 0; x2 = x; y2 = y; z2 = 1; }
                        m.Remove(x, y, z);
                        m.Set(x2, y2, z2, c);
                    }
            }
        }

        public static Vector3 NormalePour(int faces, int valeur)
        {
            var valeurs = faces == 3 ? ValeursD3 : ValeursD6;
            for (int f = 0; f < 6; f++) if (valeurs[f] == valeur) return Normales[f];
            return Vector3.up;
        }

        public static DeVue Creer(Transform parent, int faces)
        {
            var go = new GameObject("Dé");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Maillage(faces);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = DDMateriaux.Voxel;
            mr.shadowCastingMode = ShadowCastingMode.On;
            var dv = go.AddComponent<DeVue>();
            dv.faces = faces;
            return dv;
        }

        int faces;
        public bool Fini { get; private set; }

        /// Lance le dé depuis « depart » jusqu'à « arrivee » (sol) et le fait s'arrêter sur « valeur ».
        public IEnumerator Lancer(Vector3 depart, Vector3 arrivee, int valeur, float duree = 1.1f)
        {
            Fini = false;
            var finale = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * Quaternion.FromToRotation(NormalePour(faces, valeur), Vector3.up);
            var axe = Random.onUnitSphere;
            if (axe.sqrMagnitude < 0.01f) axe = Vector3.right;
            float tours = Random.Range(2.5f, 3.5f) * 360f;
            var rot0 = Random.rotation;
            float t = 0;
            float demi = N * Taille * 0.5f;
            while (t < duree)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duree);
                // trajectoire : arc puis deux rebonds amortis
                var p = Vector3.Lerp(depart, arrivee, 1f - (1f - k) * (1f - k));
                float h;
                if (k < 0.55f) h = Mathf.Lerp(depart.y - arrivee.y, 0, k / 0.55f) + Mathf.Sin(k / 0.55f * Mathf.PI) * 0.9f;
                else if (k < 0.8f) h = Mathf.Sin((k - 0.55f) / 0.25f * Mathf.PI) * 0.32f;
                else h = Mathf.Sin((k - 0.8f) / 0.2f * Mathf.PI) * 0.08f;
                p.y = arrivee.y + demi + Mathf.Max(0, h);
                transform.position = p;
                // rotation : la culbute ralentit puis rejoint la face tirée
                var culbute = Quaternion.AngleAxis(tours * (1f - (1f - k) * (1f - k)), axe) * rot0;
                transform.rotation = Quaternion.Slerp(culbute, finale, Mathf.SmoothStep(0, 1, (k - 0.55f) / 0.45f));
                yield return null;
            }
            transform.rotation = finale;
            transform.position = new Vector3(arrivee.x, arrivee.y + demi, arrivee.z);
            Fini = true;
        }
    }
}

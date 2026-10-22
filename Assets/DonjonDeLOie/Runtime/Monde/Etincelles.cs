// Donjon de l'Oie — particules en petits cubes, toutes dans un seul maillage rebâti à chaque image (peu d'objets).
// Deux systèmes : un additif (étincelles, magie, or) et un en transparence (fumée, poussière).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DonjonDeLOie.Monde
{
    public sealed class Etincelles : MonoBehaviour
    {
        struct P { public Vector3 pos, vel; public float vie, duree, taille, gravite, tournoie; public Color32 c; public Vector3 centre; }
        readonly List<P> ps = new List<P>();
        Mesh mesh;
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Color32> c = new List<Color32>();
        readonly List<int> t = new List<int>();
        bool additif;

        public static Etincelles Creer(Transform parent, bool additif)
        {
            var go = new GameObject(additif ? "Étincelles" : "Fumées");
            go.transform.SetParent(parent, false);
            var e = go.AddComponent<Etincelles>();
            e.additif = additif;
            e.mesh = new Mesh { name = "particules" };
            e.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = e.mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = additif ? DDMateriaux.FxAdd : DDMateriaux.FxAlpha;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return e;
        }

        public void Emettre(Vector3 pos, Vector3 vel, Color col, float duree, float taille, float gravite = 0f, float tournoie = 0f)
        {
            if (ps.Count > 1500) return;
            ps.Add(new P { pos = pos, vel = vel, vie = 0, duree = duree, taille = taille, gravite = gravite, c = col, tournoie = tournoie, centre = pos });
        }

        // ------------------------------------------------------------------ recettes

        public void Explosion(Vector3 p, Color col, int nombre, float vitesse, float taille = 0.07f, float gravite = 4f)
        {
            for (int i = 0; i < nombre; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.8f + 0.2f;
                Emettre(p, d * vitesse * Random.Range(0.4f, 1f), Teinte(col), Random.Range(0.4f, 0.9f), taille * Random.Range(0.6f, 1.3f), gravite);
            }
        }

        public void Pieces(Vector3 p, int nombre)
        {
            for (int i = 0; i < nombre; i++)
            {
                var d = new Vector3(Random.Range(-1f, 1f), Random.Range(2.2f, 3.6f), Random.Range(-1f, 1f));
                Emettre(p + Vector3.up * 0.8f, d * 1.1f, Teinte(new Color(1f, 0.78f, 0.25f)), Random.Range(0.8f, 1.2f), Random.Range(0.06f, 0.1f), 7f);
            }
        }

        /// Spirale montante (niveau gagné, soin, bénédiction).
        public void Spirale(Vector3 p, Color col, int nombre, float hauteur = 2.4f)
        {
            for (int i = 0; i < nombre; i++)
            {
                float a = i / (float)nombre * Mathf.PI * 6f;
                var off = new Vector3(Mathf.Cos(a) * 0.55f, i / (float)nombre * 0.4f, Mathf.Sin(a) * 0.55f);
                Emettre(p + off, new Vector3(0, hauteur * Random.Range(0.6f, 1.1f), 0), Teinte(col), Random.Range(0.9f, 1.4f), Random.Range(0.05f, 0.09f), 0f, 3.2f);
            }
        }

        public void Fumee(Vector3 p, Color col, int nombre, float rayon = 0.5f)
        {
            for (int i = 0; i < nombre; i++)
            {
                var d = Random.insideUnitSphere * rayon; d.y = Mathf.Abs(d.y);
                Emettre(p + d, new Vector3(d.x, 0.6f + Random.value * 0.6f, d.z), Teinte(col), Random.Range(0.8f, 1.6f), Random.Range(0.12f, 0.26f), -0.3f);
            }
        }

        /// Un jet d'un point à un autre (sorts lancés).
        public void Trait(Vector3 de, Vector3 vers, Color col, int nombre)
        {
            for (int i = 0; i < nombre; i++)
            {
                float k = i / (float)nombre;
                var p = Vector3.Lerp(de, vers, k) + Random.insideUnitSphere * 0.12f;
                Emettre(p, Random.insideUnitSphere * 0.4f, Teinte(col), 0.35f + k * 0.5f, Random.Range(0.05f, 0.1f), 0f);
            }
        }

        static Color Teinte(Color c)
        {
            float k = Random.Range(0.85f, 1.15f);
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        }

        // ------------------------------------------------------------------ animation

        void Update()
        {
            float dt = Time.deltaTime;
            v.Clear(); n.Clear(); c.Clear(); t.Clear();
            for (int i = ps.Count - 1; i >= 0; i--)
            {
                var p = ps[i];
                p.vie += dt;
                if (p.vie >= p.duree) { ps.RemoveAt(i); continue; }
                p.vel += Vector3.down * p.gravite * dt;
                p.vel *= 1f - Mathf.Min(0.9f, dt * 1.2f);
                p.pos += p.vel * dt;
                if (p.tournoie > 0)
                {
                    var r = p.pos - p.centre; r.y = 0;
                    p.pos = p.centre + Quaternion.AngleAxis(p.tournoie * 60f * dt, Vector3.up) * r + Vector3.up * (p.pos.y - p.centre.y);
                }
                ps[i] = p;
                float k = p.vie / p.duree;
                float s = p.taille * (k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f * 0.7f);
                var col = p.c;
                if (additif) { float f = 1f - k; col = new Color32((byte)(col.r * f), (byte)(col.g * f), (byte)(col.b * f), 255); }
                else col.a = (byte)(col.a * (1f - k));
                Cube(p.pos, s, col);
            }
            mesh.Clear();
            if (v.Count == 0) return;
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetColors(c); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
        }

        static readonly Vector3[] Coins =
        {
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
            new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1)
        };
        static readonly int[] Faces = { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2, 0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5 };

        void Cube(Vector3 p, float s, Color32 col)
        {
            int b = v.Count;
            float h = s * 0.5f;
            for (int i = 0; i < 8; i++) { v.Add(p + Coins[i] * h); n.Add(Coins[i]); c.Add(col); }
            for (int i = 0; i < Faces.Length; i++) t.Add(b + Faces[i]);
        }
    }
}

// Les Dés du Dragon — les matériaux, créés au lancement à partir des shaders du dossier Resources.
// Si un shader n'est pas pris en charge (autre pipeline, erreur de compilation), un shader d'Unity prend le relais.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DesDuDragon.Voxel;

namespace DesDuDragon.Monde
{
    public static class DDMateriaux
    {
        static Material voxel, fxAdd, fxAlpha, eau, fxDessus;
        static readonly Dictionary<int, Material> lueurs = new Dictionary<int, Material>();

        static Shader Trouver(params string[] noms)
        {
            foreach (var n in noms)
            {
                var s = Shader.Find(n);
                if (s != null && s.isSupported) return s;
            }
            Debug.LogWarning("Les Dés du Dragon : aucun des shaders " + string.Join(", ", noms) + " n'est disponible.");
            return Shader.Find("Sprites/Default");
        }

        public static Material Voxel
        {
            get
            {
                if (voxel == null)
                {
                    voxel = new Material(Trouver("DesDuDragon/Voxel", "Universal Render Pipeline/Particles/Simple Lit", "Particles/Standard Surface", "Sprites/Default"));
                    voxel.name = "DD_Voxel";
                    voxel.enableInstancing = true;
                }
                return voxel;
            }
        }

        /// Matériau lumineux. intensite : 1 = couleur telle quelle, 2–4 = rayonne avec le halo. pulsation : vitesse du battement (0 = fixe).
        public static Material Lueur(float intensite = 2.2f, float pulsation = 0f)
        {
            int cle = Mathf.RoundToInt(intensite * 100) * 1000 + Mathf.RoundToInt(pulsation * 100);
            Material m;
            if (lueurs.TryGetValue(cle, out m) && m != null) return m;
            m = new Material(Trouver("DesDuDragon/Lueur", "Universal Render Pipeline/Particles/Unlit", "Sprites/Default"));
            m.name = "DD_Lueur_" + cle;
            m.SetFloat("_Intensite", intensite);
            m.SetFloat("_Pulsation", pulsation);
            m.enableInstancing = true;
            lueurs[cle] = m;
            return m;
        }

        static Material Fx(bool additif, bool pardessus)
        {
            var m = new Material(Trouver("DesDuDragon/Fx", "Sprites/Default"));
            m.SetFloat("_SrcBlend", additif ? (float)BlendMode.One : (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additif ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZTest", pardessus ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            m.SetFloat("_Additif", additif ? 1f : 0f);
            m.renderQueue = pardessus ? 3100 : 3000;
            return m;
        }

        /// Effets additifs (étincelles, halos).
        public static Material FxAdd { get { if (fxAdd == null) { fxAdd = Fx(true, false); fxAdd.name = "DD_FxAdd"; } return fxAdd; } }

        /// Effets en transparence (fumée, ombres, anneaux pleins).
        public static Material FxAlpha { get { if (fxAlpha == null) { fxAlpha = Fx(false, false); fxAlpha.name = "DD_FxAlpha"; } return fxAlpha; } }

        /// Marqueurs toujours visibles, même derrière le décor (cases à choisir).
        public static Material FxDessus { get { if (fxDessus == null) { fxDessus = Fx(true, true); fxDessus.name = "DD_FxDessus"; } return fxDessus; } }

        public static Material Eau
        {
            get
            {
                if (eau == null)
                {
                    eau = Fx(false, false);
                    eau.name = "DD_Eau";
                    eau.SetFloat("_Vague", 0.16f);
                    eau.renderQueue = 2950;
                }
                return eau;
            }
        }

        /// Maillage Unity à partir d'un MeshData (index 32 bits au-delà de 65 000 sommets).
        public static Mesh Maillage(MeshData md, string nom)
        {
            var m = new Mesh();
            m.name = nom;
            if (md.V.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(md.V);
            m.SetNormals(md.N);
            m.SetColors(md.C);
            m.SetTriangles(md.T, 0);
            m.RecalculateBounds();
            return m;
        }

        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        /// Maillage d'une pièce de rig, gardé en cache par clé (les mêmes modèles ne sont maillés qu'une fois).
        public static Mesh MaillagePiece(string cle, VoxelPart p, float taille)
        {
            Mesh m;
            if (cle != null && cache.TryGetValue(cle, out m) && m != null) return m;
            m = Maillage(VoxelMesher.Build(p.Model, p.Pivot, taille), cle ?? p.Name);
            if (cle != null) cache[cle] = m;
            return m;
        }
    }
}

// Les Dés du Dragon — un VoxelRig posé dans la scène : une hiérarchie d'objets, une pièce par objet, pivots respectés.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DesDuDragon.Voxel;

namespace DesDuDragon.Monde
{
    public sealed class RigVue : MonoBehaviour
    {
        public VoxelRig Rig;
        public readonly Dictionary<string, Transform> Pieces = new Dictionary<string, Transform>();
        public readonly Dictionary<string, Quaternion> Repos = new Dictionary<string, Quaternion>();
        public readonly Dictionary<string, Vector3> PositionsRepos = new Dictionary<string, Vector3>();
        public readonly List<Renderer> Rendus = new List<Renderer>();
        public float Hauteur;          // hauteur du modèle en mètres (pour poser les bulles au-dessus)
        MaterialPropertyBlock bloc;
        Color teinte = Color.white, emission = Color.black;
        Coroutine eclairCo;

        /// Construit la hiérarchie. cle : préfixe du cache de maillages (null : pas de cache).
        public static RigVue Creer(VoxelRig rig, Transform parent, string cle, bool ombres = true, float intensiteLueur = 2.4f)
        {
            var go = new GameObject(rig.Name ?? "rig");
            go.transform.SetParent(parent, false);
            var rv = go.AddComponent<RigVue>();
            rv.Rig = rig;
            var restants = new List<VoxelPart>(rig.Parts);
            int garde = 0;
            float haut = 0;
            while (restants.Count > 0 && garde++ < 64)
            {
                for (int i = restants.Count - 1; i >= 0; i--)
                {
                    var p = restants[i];
                    Transform tp; Vector3 pivotParent;
                    if (p.Parent == null || rig.Get(p.Parent) == null) { tp = go.transform; pivotParent = rig.Origin; }
                    else if (rv.Pieces.ContainsKey(p.Parent)) { tp = rv.Pieces[p.Parent]; pivotParent = rig.Get(p.Parent).Pivot; }
                    else continue;
                    var pg = new GameObject(p.Name);
                    pg.transform.SetParent(tp, false);
                    pg.transform.localPosition = (p.Pivot - pivotParent) * rig.VoxelSize;
                    rv.Pieces[p.Name] = pg.transform;
                    rv.Repos[p.Name] = Quaternion.identity;
                    rv.PositionsRepos[p.Name] = pg.transform.localPosition;
                    if (p.Model.Count > 0)
                    {
                        var mf = pg.AddComponent<MeshFilter>();
                        mf.sharedMesh = DDMateriaux.MaillagePiece(cle != null ? cle + "/" + p.Name : null, p, rig.VoxelSize);
                        var mr = pg.AddComponent<MeshRenderer>();
                        bool lueur = p.Name.StartsWith("lueur");
                        mr.sharedMaterial = lueur ? DDMateriaux.Lueur(intensiteLueur, 1.6f) : DDMateriaux.Voxel;
                        mr.shadowCastingMode = (ombres && !lueur) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                        mr.receiveShadows = !lueur;
                        rv.Rendus.Add(mr);
                        Vector3Int a, b;
                        if (p.Model.Bounds(out a, out b)) haut = Mathf.Max(haut, (b.y + 1 - rig.Origin.y) * rig.VoxelSize);
                    }
                    restants.RemoveAt(i);
                }
            }
            rv.Hauteur = haut;
            return rv;
        }

        public Transform Piece(string nom)
        {
            Transform t;
            return Pieces.TryGetValue(nom, out t) ? t : null;
        }

        void Appliquer()
        {
            if (bloc == null) bloc = new MaterialPropertyBlock();
            foreach (var r in Rendus)
            {
                if (r == null || r.sharedMaterial != DDMateriaux.Voxel) continue;
                r.GetPropertyBlock(bloc);
                bloc.SetColor("_BaseColor", teinte);
                bloc.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(bloc);
            }
        }

        public void Teinte(Color c) { teinte = c; Appliquer(); }
        public void Emission(Color c) { emission = c; Appliquer(); }

        /// Éclair de couleur (coup reçu, soin, niveau) qui s'éteint en « duree » secondes.
        public void Eclair(Color c, float duree)
        {
            if (!gameObject.activeInHierarchy) return;
            if (eclairCo != null) StopCoroutine(eclairCo);
            eclairCo = StartCoroutine(EclairCo(c, duree));
        }

        IEnumerator EclairCo(Color c, float duree)
        {
            float t = 0;
            while (t < duree)
            {
                t += Time.deltaTime;
                Emission(c * (1f - t / duree));
                yield return null;
            }
            Emission(Color.black);
        }

        public void Visible(bool v)
        {
            foreach (var r in Rendus) if (r != null) r.enabled = v;
        }
    }
}

// Les Dés du Dragon — lumière et atmosphère : soleil d'après-midi, ciel en dégradé, brume légère, post-traitement d'URP
// (halo, tons ACES, couleurs, vignette) posé par réflexion pour que le jeu compile aussi sans URP.
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using DesDuDragon.Voxel;

namespace DesDuDragon.Monde
{
    public static class Ambiance
    {
        public static readonly Color Horizon = new Color(0.80f, 0.86f, 0.89f);
        public static readonly Color Zenith = new Color(0.36f, 0.56f, 0.80f);
        public static Light Soleil;

        public static void Installer(Transform parent, Camera cam)
        {
            // Soleil
            var sg = new GameObject("Soleil");
            sg.transform.SetParent(parent, false);
            sg.transform.rotation = Quaternion.Euler(48f, 38f, 0f);
            Soleil = sg.AddComponent<Light>();
            Soleil.type = LightType.Directional;
            Soleil.color = new Color(1f, 0.93f, 0.82f);
            Soleil.intensity = 1.35f;
            Soleil.shadows = LightShadows.Soft;
            Soleil.shadowStrength = 0.78f;
            RenderSettings.sun = Soleil;

            // Lumière ambiante en trois tons et brume
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.67f, 0.80f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.60f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.27f, 0.23f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Horizon;
            RenderSettings.fogStartDistance = 85f;
            RenderSettings.fogEndDistance = 320f;
            try { DynamicGI.UpdateEnvironment(); } catch (Exception) { }

            cam.backgroundColor = Horizon;
            Ciel(parent);
            PostTraitement(parent, cam);
        }

        /// Dôme du ciel : dégradé du zénith à l'horizon, sans éclairage.
        static void Ciel(Transform parent)
        {
            var md = new MeshData();
            const int seg = 48, anneaux = 16;
            const float r = 170f;
            Color32 zen = Zenith, hor = Horizon, bas = new Color(0.62f, 0.75f, 0.84f);
            for (int j = 0; j <= anneaux; j++)
            {
                float v = j / (float)anneaux;                         // 0 : bas (−10°), 1 : zénith
                float lat = Mathf.Lerp(-12f, 90f, v) * Mathf.Deg2Rad;
                Color32 c = v < 0.12f ? Color32.Lerp(bas, hor, v / 0.12f) : Color32.Lerp(hor, zen, Mathf.Pow((v - 0.12f) / 0.88f, 0.7f));
                for (int i = 0; i <= seg; i++)
                {
                    float lon = i / (float)seg * Mathf.PI * 2f;
                    md.V.Add(new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon)) * r);
                    md.N.Add(Vector3.down);
                    md.C.Add(c);
                }
            }
            for (int j = 0; j < anneaux; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1;
                    // vu de l'intérieur
                    md.T.Add(a); md.T.Add(b); md.T.Add(d);
                    md.T.Add(a); md.T.Add(d); md.T.Add(c);
                }
            var go = new GameObject("Ciel");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = DDMateriaux.Maillage(md, "ciel");
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(DDMateriaux.Lueur(1f, 0f));
            mat.renderQueue = 1990;
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<SuivreCamera>();
        }

        // ------------------------------------------------------------------ post-traitement (URP, par réflexion)

        static Type T(string nom, string assembly) { return Type.GetType(nom + ", " + assembly); }

        static void Regler(object composant, string champ, object valeur)
        {
            if (composant == null) return;
            var f = composant.GetType().GetField(champ, BindingFlags.Public | BindingFlags.Instance);
            if (f == null) return;
            var param = f.GetValue(composant);
            if (param == null) return;
            var pv = param.GetType().GetProperty("value", BindingFlags.Public | BindingFlags.Instance);
            var po = param.GetType().GetProperty("overrideState", BindingFlags.Public | BindingFlags.Instance);
            try
            {
                if (pv != null)
                {
                    var t = pv.PropertyType;
                    object v = valeur;
                    if (t.IsEnum && valeur is string) v = Enum.Parse(t, (string)valeur);
                    else if (t == typeof(float)) v = Convert.ToSingle(valeur);
                    pv.SetValue(param, v, null);
                }
                if (po != null) po.SetValue(param, true, null);
            }
            catch (Exception e) { Debug.LogWarning("Post-traitement : " + champ + " : " + e.Message); }
        }

        static void PostTraitement(Transform parent, Camera cam)
        {
            const string core = "Unity.RenderPipelines.Core.Runtime", urp = "Unity.RenderPipelines.Universal.Runtime";
            var tVolume = T("UnityEngine.Rendering.Volume", core);
            var tProfil = T("UnityEngine.Rendering.VolumeProfile", core);
            var tDonnees = T("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData", urp);
            if (tVolume == null || tProfil == null || tDonnees == null) return;   // pas d'URP : pas de post-traitement
            try
            {
                var donnees = cam.GetComponent(tDonnees);
                if (donnees == null) donnees = cam.gameObject.AddComponent(tDonnees);
                var p = tDonnees.GetProperty("renderPostProcessing");
                if (p != null) p.SetValue(donnees, true, null);
                var aa = tDonnees.GetProperty("antialiasing");
                if (aa != null) aa.SetValue(donnees, Enum.Parse(aa.PropertyType, "SubpixelMorphologicalAntiAliasing"), null);
                var ombres = tDonnees.GetProperty("renderShadows");
                if (ombres != null) ombres.SetValue(donnees, true, null);

                var go = new GameObject("Post-traitement");
                go.transform.SetParent(parent, false);
                var vol = go.AddComponent(tVolume);
                var pg = tVolume.GetProperty("isGlobal"); if (pg != null) pg.SetValue(vol, true, null);
                var profil = ScriptableObject.CreateInstance(tProfil);
                var pp = tVolume.GetProperty("sharedProfile");
                if (pp != null) pp.SetValue(vol, profil, null); else tVolume.GetField("sharedProfile").SetValue(vol, profil);
                var ajouter = tProfil.GetMethod("Add", new[] { typeof(Type), typeof(bool) });

                var bloom = ajouter.Invoke(profil, new object[] { T("UnityEngine.Rendering.Universal.Bloom", urp), true });
                Regler(bloom, "intensity", 0.55f); Regler(bloom, "threshold", 1.05f); Regler(bloom, "scatter", 0.62f);
                var ton = ajouter.Invoke(profil, new object[] { T("UnityEngine.Rendering.Universal.Tonemapping", urp), true });
                Regler(ton, "mode", "ACES");
                var coul = ajouter.Invoke(profil, new object[] { T("UnityEngine.Rendering.Universal.ColorAdjustments", urp), true });
                Regler(coul, "postExposure", 0.35f); Regler(coul, "contrast", 12f); Regler(coul, "saturation", 10f);
                var bal = ajouter.Invoke(profil, new object[] { T("UnityEngine.Rendering.Universal.WhiteBalance", urp), true });
                Regler(bal, "temperature", 6f);
                var vig = ajouter.Invoke(profil, new object[] { T("UnityEngine.Rendering.Universal.Vignette", urp), true });
                Regler(vig, "intensity", 0.22f); Regler(vig, "smoothness", 0.5f);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Les Dés du Dragon : post-traitement non installé (" + e.Message + ").");
            }
        }
    }

    public sealed class SuivreCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var c = Camera.main;
            if (c != null) transform.position = new Vector3(c.transform.position.x, 0f, c.transform.position.z);
        }
    }
}

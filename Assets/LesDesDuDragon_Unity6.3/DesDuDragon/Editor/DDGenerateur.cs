// Les Dés du Dragon — le générateur : un menu d'Unity qui fabrique la scène du jeu en un clic et règle le rendu.
// Menu : Les Dés du Dragon › Générer le jeu (puis Play). Le monde, les modèles et l'interface se construisent au lancement.
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using DesDuDragon.Pilote;

namespace DesDuDragon.Editeur
{
    public static class DDGenerateur
    {
        const string Dossier = "Assets/DesDuDragon/Scenes";
        const string Scene = Dossier + "/LesDesDuDragon.unity";

        [MenuItem("Les Dés du Dragon/Générer le jeu", false, 1)]
        public static void Generer()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var rapport = new List<string>();

            // 1. Vérifier les shaders
            foreach (var nom in new[] { "DesDuDragon/Voxel", "DesDuDragon/Lueur", "DesDuDragon/Fx" })
            {
                var s = Shader.Find(nom);
                if (s == null) rapport.Add("⚠ Shader introuvable : " + nom + " (le dossier Resources a-t-il été copié ?)");
                else if (!s.isSupported) rapport.Add("⚠ Shader non pris en charge par ce pipeline : " + nom + " (un shader d'Unity prendra le relais)");
            }
            bool urp = GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
            rapport.Add(urp ? "Pipeline : URP — ombres douces, halo et post-traitement actifs." : "Pipeline : intégré (sans URP) — le jeu fonctionne, sans post-traitement.");

            // 2. La scène
            if (!AssetDatabase.IsValidFolder(Dossier))
            {
                if (!AssetDatabase.IsValidFolder("Assets/DesDuDragon")) AssetDatabase.CreateFolder("Assets", "DesDuDragon");
                AssetDatabase.CreateFolder("Assets/DesDuDragon", "Scenes");
            }
            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Les Dés du Dragon");
            go.AddComponent<DDJeu>();
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc, Scene);
            rapport.Add("Scène créée : " + Scene);

            // 3. Première scène de la build
            var liste = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            liste.RemoveAll(s => s.path == Scene);
            liste.Insert(0, new EditorBuildSettingsScene(Scene, true));
            EditorBuildSettings.scenes = liste.ToArray();
            rapport.Add("Ajoutée en tête des scènes de la build.");

            // 4. Réglages du rendu (URP) : ombres jusqu'à 90 m pour voir tout le plateau ombré
            if (urp) rapport.Add(ReglerUrp(GraphicsSettings.currentRenderPipeline));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Les Dés du Dragon — génération terminée :\n• " + string.Join("\n• ", rapport.ToArray()));
            EditorUtility.DisplayDialog("Les Dés du Dragon", "Le jeu est prêt.\n\n• " + string.Join("\n• ", rapport.ToArray()) + "\n\nAppuyez sur Play.", "Parfait");
        }

        static string ReglerUrp(RenderPipelineAsset asset)
        {
            var faits = new List<string>();
            var t = asset.GetType();
            Regler(t, asset, "shadowDistance", 90f, faits);
            Regler(t, asset, "shadowCascadeCount", 2, faits);
            Regler(t, asset, "supportsHDR", true, faits);
            EditorUtility.SetDirty(asset);
            return faits.Count > 0 ? "Rendu URP réglé : " + string.Join(", ", faits.ToArray()) + "." : "Rendu URP laissé tel quel.";
        }

        static void Regler(System.Type t, object o, string nom, object valeur, List<string> faits)
        {
            var p = t.GetProperty(nom, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) return;
            try
            {
                var avant = p.GetValue(o, null);
                if (avant is float && valeur is float && (float)avant >= (float)valeur) return;
                p.SetValue(o, valeur, null);
                faits.Add(nom + " = " + valeur);
            }
            catch (System.Exception) { }
        }

        [MenuItem("Les Dés du Dragon/Ouvrir la scène", false, 2)]
        public static void Ouvrir()
        {
            if (!File.Exists(Scene)) { Generer(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene);
        }

        [MenuItem("Les Dés du Dragon/Jouer", false, 3)]
        public static void Jouer()
        {
            if (!File.Exists(Scene)) Generer();
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene);
            EditorApplication.isPlaying = true;
        }
    }
}

// Donjon de l'Oie — l'aperçu de l'île dans la scène de l'éditeur de cartes : la même île que celle du jeu, construite
// à partir des marqueurs, mais jamais enregistrée (HideFlags.DontSave) et effacée avant le lancement du jeu.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    [InitializeOnLoad]
    public static class DDApercuIle
    {
        public const string Nom = "Aperçu de l'île (non enregistré)";

        static DDApercuIle()
        {
            EditorApplication.playModeStateChanged += ModeChange;
            EditorSceneManager.sceneClosing += (scene, retiree) => Effacer();
            AssemblyReloadEvents.beforeAssemblyReload += Effacer;
        }

        static void ModeChange(PlayModeStateChange etat)
        {
            if (etat == PlayModeStateChange.ExitingEditMode) Effacer();
        }

        public static bool Visible { get { return GameObject.Find(Nom) != null; } }

        /// Construit l'aperçu ; rend un message d'erreur, ou null si tout va bien.
        public static string Montrer(DDEditeurCarte racine)
        {
            Effacer();
            DDOutilsEditeur.AppliquerCatalogue();   // les événements et le boss du catalogue
            var carte = racine.Exporter();
            var erreurs = new System.Collections.Generic.List<string>();
            Plateau.Verifier(carte, erreurs, new System.Collections.Generic.List<string>());
            if (erreurs.Count > 0) return "La carte n'est pas jouable :\n• " + string.Join("\n• ", erreurs.ToArray());
            var modele = new IleModele(Plateau.Depuis(carte), carte);
            var go = new GameObject(Nom);
            go.hideFlags = HideFlags.DontSave;
            IleVue.Construire(modele, go.transform);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            // les dalles du jeu sont au niveau du terrain : on descend l'île pour que le départ soit à hauteur des marqueurs
            var d = carte.Cases[carte.Depart];
            float dy = -modele.HauteurEn(d.X, d.Z);
            go.transform.position = new Vector3(0, dy, 0);
            // on clique à travers l'île (pour attraper les cases), et les marqueurs se dessinent sur son relief
            SceneVisibilityManager.instance.DisablePicking(go, true);
            DDCaseMarqueur.HauteurSol = (x, z) => modele.HauteurEn(x, z) + dy;
            SceneView.RepaintAll();
            return null;
        }

        public static void Effacer()
        {
            DDCaseMarqueur.HauteurSol = null;
            for (int garde = 0; garde < 8; garde++)
            {
                var go = GameObject.Find(Nom);
                if (go == null) break;
                // les maillages de l'aperçu ne servent qu'à lui (les caches du jeu les refont au besoin)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null && !AssetDatabase.Contains(mf.sharedMesh)) Object.DestroyImmediate(mf.sharedMesh);
                Object.DestroyImmediate(go);
            }
            SceneView.RepaintAll();
        }
    }
}

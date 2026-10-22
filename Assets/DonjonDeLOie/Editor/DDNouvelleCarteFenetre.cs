// Donjon de l'Oie — menu « Donjon de l'Oie › Nouvelle carte… » : une petite fenêtre qui crée, en un clic, une scène
// prête à éditer (le jeu + une carte de départ faite de marqueurs), rangée dans Assets/DonjonDeLOie/Cartes/<nom>/.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Pilote;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    public sealed class DDNouvelleCarteFenetre : EditorWindow
    {
        static readonly string[] Modeles = { "Une boucle simple à compléter", "Une copie de la carte d'origine (Valcreux)" };

        string nom = "Ma carte";
        int modele;
        int nombre = 24;
        float rayon = 20f;

        [MenuItem("Donjon de l'Oie/Nouvelle carte…", false, 2)]
        public static void Ouvrir()
        {
            var f = GetWindow<DDNouvelleCarteFenetre>(true, "Nouvelle carte", true);
            f.minSize = f.maxSize = new Vector2(420, 230);
            f.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Créer une carte de jeu", DDOutilsEditeur.Titre);
            nom = EditorGUILayout.TextField(new GUIContent("Nom", "Affiché dans le salon ; donne aussi son nom au dossier et à la scène."), nom);
            modele = EditorGUILayout.Popup("Pour commencer", modele, Modeles);
            GUI.enabled = modele == 0;
            nombre = EditorGUILayout.IntSlider(new GUIContent("Cases", "Nombre de cases de la boucle."), nombre, 8, 60);
            rayon = EditorGUILayout.Slider(new GUIContent("Taille (m)", "Rayon de la boucle, en mètres."), rayon, 10f, 60f);
            GUI.enabled = true;
            string dossier = Dossier(nom);
            EditorGUILayout.LabelField("Dossier : " + dossier, EditorStyles.wordWrappedMiniLabel);
            bool existe = AssetDatabase.IsValidFolder(dossier);
            if (existe) EditorGUILayout.HelpBox("Ce dossier existe déjà : choisissez un autre nom.", MessageType.Warning);
            GUILayout.FlexibleSpace();
            if (EditorApplication.isPlaying) EditorGUILayout.HelpBox("Quittez le mode Play pour créer une carte.", MessageType.Warning);
            GUI.enabled = !existe && nom.Trim().Length > 0 && !EditorApplication.isPlaying;
            if (GUILayout.Button("Créer la carte", GUILayout.Height(30)))
            {
                var n = nom.Trim(); int m = modele, c = nombre; float r = rayon;
                Close();
                Creer(n, m, c, r);
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
        }

        static string Dossier(string nom) { return DDOutilsEditeur.DossierCartes + "/" + DDEditeurCarteInspecteur.NomFichier(nom.Trim()); }

        /// Crée la scène de la carte et la sélectionne. Rend la racine de la carte (null si l'utilisateur a annulé).
        public static DDEditeurCarte Creer(string nom, int modele, int nombre, float rayon)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Nouvelle carte", "Quittez le mode Play pour créer une carte.", "OK");
                return null;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            string dossier = Dossier(nom);
            DDOutilsEditeur.AssurerDossier(dossier);
            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var jeu = new GameObject("Donjon de l'Oie");
            jeu.AddComponent<DDJeu>();

            var go = new GameObject("Carte");
            var racine = go.AddComponent<DDEditeurCarte>();
            racine.NomCarte = nom;
            DDOutilsEditeur.AppliquerCatalogue();   // les événements et le boss du catalogue
            CarteDef def;
            if (modele == 1) def = Plateau.Standard().VersCarte(nom);
            else def = Plateau.CarteVide(nom, nombre, rayon);
            def.Nom = nom;
            racine.Importer(def);
            DDCartesEdition.Renommer(racine);

            EditorSceneManager.MarkSceneDirty(sc);
            string chemin = dossier + "/" + DDEditeurCarteInspecteur.NomFichier(nom) + ".unity";
            EditorSceneManager.SaveScene(sc, chemin);
            Selection.activeGameObject = go;
            var sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                float taille = modele == 1 ? 60f : rayon * 1.4f;
                sv.LookAt(Vector3.zero, Quaternion.Euler(60f, 0f, 0f), taille);
                sv.Repaint();
            }
            Debug.Log("Donjon de l'Oie : nouvelle carte « " + nom + " » (" + chemin + ").");
            EditorUtility.DisplayDialog("Nouvelle carte",
                "La scène « " + nom + " » est prête.\n\n"
                + "• Sélectionnez une case dans la scène ou la hiérarchie et déplacez-la (W).\n"
                + "• L'inspecteur de chaque case règle son type, son bâtiment et ses événements.\n"
                + "• L'objet « Carte » vérifie la carte, montre l'aperçu de l'île et l'ajoute au jeu.\n"
                + "• Play : vous jouez directement sur cette carte.", "C'est parti");
            return racine;
        }
    }
}

// Donjon de l'Oie — l'inspecteur de la racine d'une carte (objet « Carte » de la scène) : réglages de l'île,
// vérification en direct, aperçu de l'île dans la scène, ajout de la carte au jeu et lancement d'une partie d'essai.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Pilote;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    [CustomEditor(typeof(DDEditeurCarte))]
    public sealed class DDEditeurCarteInspecteur : Editor
    {
        string decorChoisi = "tour_garde";
        static bool aideOuverte = true;

        public override void OnInspectorGUI()
        {
            var r = (DDEditeurCarte)target;
            aideOuverte = EditorGUILayout.Foldout(aideOuverte, "Comment faire une carte ?", true);
            if (aideOuverte)
                EditorGUILayout.HelpBox(
                    "1. Sélectionnez une case (objet « Case … ») et déplacez-la avec l'outil Déplacer (touche W).\n"
                    + "2. Dans l'inspecteur de la case : type, zone, nom du lieu, bâtiment et événements.\n"
                    + "3. Ses boutons ajoutent une case après elle, créent un embranchement ou la suppriment.\n"
                    + "4. Ici : « Aperçu de l'île » pour voir le résultat, puis « Ajouter la carte au jeu ».\n"
                    + "Flèche crème : la grande route. Flèche orange : un embranchement. Cadre jaune : le départ.", MessageType.Info);

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            var cases = r.Cases();
            var carte = r.Exporter();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("La carte", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Compte(cases) + " · " + carte.Decors.Count + " décor(s) posé(s) à la main", EditorStyles.wordWrappedMiniLabel);

            // vérification en direct
            var erreurs = new List<string>(); var conseils = new List<string>();
            DDOutilsEditeur.AppliquerCatalogue();   // les événements et le boss du catalogue
            Plateau.Verifier(carte, erreurs, conseils);
            if (r.Depart == null && cases.Count > 0) conseils.Insert(0, "Pas de case de départ choisie : la première case servira de départ.");
            if (erreurs.Count == 0) EditorGUILayout.HelpBox("La carte est jouable.", MessageType.Info);
            foreach (var e in erreurs) EditorGUILayout.HelpBox(e, MessageType.Error);
            foreach (var c in conseils) EditorGUILayout.HelpBox(c, MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Construire", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Ajouter une case", "Ajoute une case juste avant le départ (à la fin de la grande boucle).")))
            {
                var boucle = DDCartesEdition.GrandeBoucle(r);
                if (boucle.Count > 0) Selection.activeGameObject = DDCartesEdition.InsererApres(boucle[boucle.Count - 1]).gameObject;
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button(new GUIContent("Répartir les cases", "Replace les cases de la grande boucle à intervalles égaux le long de leur tracé.")))
                DDCartesEdition.Repartir(r);
            if (GUILayout.Button(new GUIContent("Renommer", "Renomme les objets « Case 01 · … » dans l'ordre de la hiérarchie.")))
                DDCartesEdition.Renommer(r);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            decorChoisi = DDOutilsEditeur.PopupDecor(null, decorChoisi);
            if (GUILayout.Button(new GUIContent("Poser ce décor", "Crée un décor au centre de la vue de la scène ; déplacez-le ensuite (W), tournez-le (E), agrandissez-le (R)."), GUILayout.Width(130)))
            { Selection.activeGameObject = DDCartesEdition.NouveauDecor(r, decorChoisi).gameObject; GUIUtility.ExitGUI(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Voir et jouer", EditorStyles.boldLabel);
            bool apercu = DDApercuIle.Visible;
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = erreurs.Count == 0 && !EditorApplication.isPlaying;
            if (GUILayout.Button(apercu ? "Mettre à jour l'aperçu" : "Aperçu de l'île", GUILayout.Height(26))) { MontrerApercu(r); GUIUtility.ExitGUI(); }
            GUI.enabled = apercu;
            if (GUILayout.Button("Cacher l'aperçu", GUILayout.Height(26))) DDApercuIle.Effacer();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            if (apercu) EditorGUILayout.LabelField("L'aperçu n'est pas enregistré avec la scène ; il disparaît au lancement du jeu.", EditorStyles.wordWrappedMiniLabel);

            GUI.enabled = erreurs.Count == 0 && !EditorApplication.isPlaying;
            if (GUILayout.Button(new GUIContent("Ajouter la carte au jeu", "Enregistre la carte dans " + DDOutilsEditeur.DossierCartesJeu + " : elle sera proposée dans le salon de toutes les scènes du jeu."), GUILayout.Height(30)))
            { AjouterAuJeu(r); GUIUtility.ExitGUI(); }
            if (GUILayout.Button(new GUIContent("Jouer cette carte", "Lance le jeu dans cette scène : la carte de l'éditeur est choisie d'office dans le salon."), GUILayout.Height(30)))
            { Jouer(r); GUIUtility.ExitGUI(); }
            GUI.enabled = true;
            var asset = AssetDatabase.LoadAssetAtPath<DDCarte>(CheminAsset(r));
            if (asset != null) EditorGUILayout.LabelField("Déjà dans le jeu : " + CheminAsset(r) + " (le bouton la met à jour).", EditorStyles.wordWrappedMiniLabel);
        }

        static string Compte(List<DDCaseMarqueur> cases)
        {
            var n = new int[Donnees.NomsTypesCase.Length];
            foreach (var c in cases) n[(int)c.Type]++;
            var parts = new List<string>();
            for (int i = 0; i < n.Length; i++) if (n[i] > 0) parts.Add(n[i] + " " + Donnees.NomsTypesCase[i].ToLowerInvariant());
            return cases.Count + " cases : " + string.Join(", ", parts.ToArray());
        }

        static void MontrerApercu(DDEditeurCarte r)
        {
            string erreur = DDApercuIle.Montrer(r);
            if (erreur != null) EditorUtility.DisplayDialog("Aperçu de l'île", erreur, "OK");
        }

        public static string CheminAsset(DDEditeurCarte r)
        {
            return DDOutilsEditeur.DossierCartesJeu + "/" + NomFichier(r.NomCarte) + ".asset";
        }

        /// Un nom de fichier sans caractère interdit.
        public static string NomFichier(string nom)
        {
            if (string.IsNullOrEmpty(nom)) nom = "Carte";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) nom = nom.Replace(c, '_');
            return nom.Replace('/', '_').Replace('\\', '_').Trim();
        }

        public static DDCarte AjouterAuJeu(DDEditeurCarte r)
        {
            DDOutilsEditeur.AssurerDossier(DDOutilsEditeur.DossierCartesJeu);
            string chemin = CheminAsset(r);
            var asset = AssetDatabase.LoadAssetAtPath<DDCarte>(chemin);
            bool nouveau = asset == null;
            if (nouveau) asset = CreateInstance<DDCarte>();
            else Undo.RecordObject(asset, "Mettre à jour la carte");
            asset.Carte = r.Exporter();
            asset.Description = r.Description;
            asset.NiveauConseille = r.NiveauConseille;
            if (nouveau) AssetDatabase.CreateAsset(asset, chemin);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Donjon de l'Oie : carte « " + r.NomCarte + " » " + (nouveau ? "ajoutée au jeu" : "mise à jour") + " (" + chemin + ").");
            EditorUtility.DisplayDialog("Carte ajoutée", "« " + r.NomCarte + " » " + (nouveau ? "fait maintenant partie du jeu" : "est à jour")
                + " : elle est proposée dans le salon (bouton « Carte »).\n\nFichier : " + chemin, "Parfait");
            return asset;
        }

        static void Jouer(DDEditeurCarte r)
        {
            if (Object.FindAnyObjectByType<DDJeu>() == null)
            {
                var go = new GameObject("Donjon de l'Oie");
                Undo.RegisterCreatedObjectUndo(go, "Ajouter le jeu");
                go.AddComponent<DDJeu>();
                EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
            }
            DDApercuIle.Effacer();
            EditorApplication.EnterPlaymode();
        }
    }
}

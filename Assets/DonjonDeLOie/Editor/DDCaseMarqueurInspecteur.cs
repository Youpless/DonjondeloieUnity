// Donjon de l'Oie — l'inspecteur d'une case de l'éditeur de cartes : type, zone, nom, bâtiment, événements propres,
// liens vers les cases suivantes, et les boutons de construction (ajouter après, embranchement, relier, supprimer).
// Plusieurs cases sélectionnées : type, zone et mode se règlent pour toutes à la fois.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    [CustomEditor(typeof(DDCaseMarqueur)), CanEditMultipleObjects]
    public sealed class DDCaseMarqueurInspecteur : Editor
    {
        static readonly string[] AidesModes =
        {
            "Standard : la case fait l'effet de son type. Une case « Événement » tire dans sa liste ci-dessous si elle en a une, sinon dans la pioche.",
            "Remplacer : au lieu de l'effet de son type, la case joue un événement de sa liste (tiré au hasard s'il y en a plusieurs).",
            "Ajouter : la case fait l'effet de son type, puis joue un événement de sa liste.",
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (targets.Length > 1) { Plusieurs(); serializedObject.ApplyModifiedProperties(); return; }
            var m = (DDCaseMarqueur)target;
            var racine = m.GetComponentInParent<DDEditeurCarte>();
            if (racine == null) EditorGUILayout.HelpBox("Cette case doit être rangée sous un objet « Carte » (composant DDEditeurCarte).", MessageType.Error);
            bool depart = racine != null && racine.Depart == m;
            EditorGUILayout.LabelField((depart ? "★ Case de départ — " : "") + m.gameObject.name, DDOutilsEditeur.Titre);

            EditorGUI.BeginChangeCheck();
            var type = serializedObject.FindProperty("Type");
            EditorGUILayout.PropertyField(type);
            EditorGUILayout.LabelField(Donnees.AidesTypesCase[type.intValue], EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Zone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("NomLieu"), new GUIContent("Nom du lieu", "Un nom rend la case célèbre : elle peut devenir la cible d'une quête de voyage. Vide : une case ordinaire."));
            bool renommer = EditorGUI.EndChangeCheck();
            var lieu = serializedObject.FindProperty("Lieu");
            lieu.stringValue = DDOutilsEditeur.PopupLieu("Bâtiment", lieu.stringValue);
            if (m.Suivants.Count > 1 || EstDebutDEmbranchement(m))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Voie"), new GUIContent("Nom du chemin", "Affiché quand le joueur choisit sa route (ex. « Sentier des Brumes »)."));

            EditorGUILayout.Space();
            Evenements(m);
            serializedObject.ApplyModifiedProperties();
            if (renommer && racine != null) DDCartesEdition.Renommer(racine);

            EditorGUILayout.Space();
            Liens(m);
            EditorGUILayout.Space();
            Construire(m, racine, depart);
        }

        /// Plusieurs cases : les réglages communs, et « relier » si elles sont deux.
        void Plusieurs()
        {
            EditorGUILayout.LabelField(targets.Length + " cases sélectionnées", DDOutilsEditeur.Titre);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Zone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Mode"));
            if (targets.Length == 2)
            {
                var a = (DDCaseMarqueur)targets[0]; var b = (DDCaseMarqueur)targets[1];
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Relier ces deux cases", EditorStyles.boldLabel);
                GUI.enabled = !a.Suivants.Contains(b);
                if (GUILayout.Button("« " + a.name + " »  →  « " + b.name + " »")) DDCartesEdition.Relier(a, b);
                GUI.enabled = !b.Suivants.Contains(a);
                if (GUILayout.Button("« " + b.name + " »  →  « " + a.name + " »")) DDCartesEdition.Relier(b, a);
                GUI.enabled = true;
            }
            else EditorGUILayout.HelpBox("Sélectionnez exactement deux cases (Ctrl + clic) pour les relier.", MessageType.None);
        }

        static bool EstDebutDEmbranchement(DDCaseMarqueur m)
        {
            foreach (var p in DDCartesEdition.Precedents(m)) if (p.Suivants.IndexOf(m) > 0) return true;
            return false;
        }

        void Evenements(DDCaseMarqueur m)
        {
            EditorGUILayout.LabelField("Événements de la case", EditorStyles.boldLabel);
            var mode = serializedObject.FindProperty("Mode");
            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));
            EditorGUILayout.LabelField(AidesModes[mode.intValue], EditorStyles.wordWrappedMiniLabel);
            var liste = serializedObject.FindProperty("Evenements");
            int retirer = -1;
            for (int i = 0; i < liste.arraySize; i++)
            {
                var e = liste.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                e.stringValue = DDOutilsEditeur.PopupEvenement(null, e.stringValue);
                if (GUILayout.Button(new GUIContent("Modifier", "Ouvre cet événement dans la fenêtre du catalogue."), GUILayout.Width(64)))
                {
                    serializedObject.ApplyModifiedProperties();
                    DDCatalogueFenetre.OuvrirEvenement(e.stringValue);
                    GUIUtility.ExitGUI();       // une autre fenêtre s'est ouverte : on arrête ce dessin proprement
                }
                if (GUILayout.Button("✕", GUILayout.Width(24))) retirer = i;
                EditorGUILayout.EndHorizontal();
            }
            if (retirer >= 0) { liste.DeleteArrayElementAtIndex(retirer); serializedObject.ApplyModifiedProperties(); GUIUtility.ExitGUI(); }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Ajouter un événement", "Ajoute un événement existant à la liste de la case.")))
            {
                liste.InsertArrayElementAtIndex(liste.arraySize);
                var evs = DDOutilsEditeur.Evenements();
                liste.GetArrayElementAtIndex(liste.arraySize - 1).stringValue = evs.Count > 0 ? evs[0].Id : "";
                if (m.Type != TypeCase.Evenement && mode.intValue == (int)ModeEvenements.Standard) mode.intValue = (int)ModeEvenements.Remplacer;
            }
            if (GUILayout.Button(new GUIContent("Créer un nouvel événement…", "Crée un événement propre à cette case dans le catalogue et l'ouvre pour le régler.")))
            {
                serializedObject.ApplyModifiedProperties();
                CreerEvenement(m);
                serializedObject.Update();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            if (liste.arraySize > 0 && m.Type != TypeCase.Evenement && mode.intValue == (int)ModeEvenements.Standard)
                EditorGUILayout.HelpBox("En mode Standard, seule une case « Événement » utilise sa liste. Choisissez « Remplacer » ou « Ajouter ».", MessageType.Warning);
        }

        static void CreerEvenement(DDCaseMarqueur m)
        {
            var cat = DDOutilsEditeur.Catalogue(true);
            var pris = new HashSet<string>();
            foreach (var e in cat.Evenements) pris.Add(e.Id);
            string baseId = "case_" + (string.IsNullOrEmpty(m.NomLieu) ? m.Type.ToString().ToLowerInvariant() : Simplifier(m.NomLieu));
            string id = DDOutilsEditeur.IdLibre(baseId, pris);
            Undo.RecordObject(cat, "Créer un événement");
            cat.Evenements.Add(new DefEvenement(id, string.IsNullOrEmpty(m.NomLieu) ? "Événement de la case" : m.NomLieu,
                "{joueur} découvre quelque chose…", Ton.Neutre, new EffetEvenement(TypeEffet.Or, 5)) { DansLaPioche = false });
            EditorUtility.SetDirty(cat);
            Undo.RecordObject(m, "Créer un événement");
            m.Evenements.Add(id);
            if (m.Type != TypeCase.Evenement && m.Mode == ModeEvenements.Standard) m.Mode = ModeEvenements.Remplacer;
            DDCatalogueFenetre.OuvrirEvenement(id);
        }

        /// « Chapelle des Saules » → « chapelle_des_saules ».
        public static string Simplifier(string s)
        {
            var d = s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var b = new System.Text.StringBuilder();
            foreach (char c in d)
            {
                if (char.IsLetterOrDigit(c) && c < 128) b.Append(c);
                else if ((c == ' ' || c == '-' || c == '\'') && b.Length > 0 && b[b.Length - 1] != '_') b.Append('_');
            }
            return b.ToString().Trim('_');
        }

        void Liens(DDCaseMarqueur m)
        {
            EditorGUILayout.LabelField("Chemins", EditorStyles.boldLabel);
            var suivants = serializedObject.FindProperty("Suivants");
            int retirer = -1;
            for (int i = 0; i < suivants.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(suivants.GetArrayElementAtIndex(i), new GUIContent(i == 0 ? "Grande route →" : "Embranchement →",
                    i == 0 ? "La case suivante sur le chemin principal." : "Un autre chemin : le joueur choisit sa route en passant ici."));
                if (GUILayout.Button("✕", GUILayout.Width(24))) retirer = i;
                EditorGUILayout.EndHorizontal();
            }
            if (retirer >= 0)
            {
                suivants.GetArrayElementAtIndex(retirer).objectReferenceValue = null;
                suivants.DeleteArrayElementAtIndex(retirer);
                serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }
            if (suivants.arraySize == 0) EditorGUILayout.HelpBox("Aucune case suivante : les pions seraient bloqués ici.", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            var prec = DDCartesEdition.Precedents(m);
            EditorGUILayout.LabelField("Arrivent ici : " + (prec.Count == 0 ? "aucune case" : Noms(prec)), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = prec.Count > 0;
            if (GUILayout.Button("◄ Case précédente")) Selection.activeGameObject = prec[0].gameObject;
            GUI.enabled = m.Suivants.Count > 0 && m.Suivants[0] != null;
            if (GUILayout.Button("Case suivante ►")) Selection.activeGameObject = m.Suivants[0].gameObject;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        static string Noms(List<DDCaseMarqueur> l)
        {
            var n = new List<string>();
            foreach (var c in l) n.Add(c.name);
            return string.Join(", ", n.ToArray());
        }

        void Construire(DDCaseMarqueur m, DDEditeurCarte racine, bool depart)
        {
            EditorGUILayout.LabelField("Construire", EditorStyles.boldLabel);
            if (racine == null) return;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Ajouter une case après", "Insère une case entre celle-ci et la suivante de la grande route.")))
            { Selection.activeGameObject = DDCartesEdition.InsererApres(m).gameObject; GUIUtility.ExitGUI(); }
            GUI.enabled = !depart;
            if (GUILayout.Button(new GUIContent("Définir comme départ", "Les pions commencent ici ; on y touche une solde à chaque passage.")))
            {
                Undo.RecordObject(racine, "Définir le départ");
                racine.Depart = m;
                if (m.Type != TypeCase.Ville && EditorUtility.DisplayDialog("Case de départ", "Faire aussi de cette case une « Ville » (soins, boutique complète, quêtes) ?", "Oui", "Non"))
                { Undo.RecordObject(m, "Définir le départ"); m.Type = TypeCase.Ville; DDCartesEdition.Renommer(racine); }
                GUIUtility.ExitGUI();           // après une boîte de dialogue, on arrête ce dessin proprement
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = m.Suivants.Count > 0;
            if (GUILayout.Button(new GUIContent("Raccourci vers l'intérieur", "Crée 3 cases qui partent d'ici et rejoignent la grande route 5 cases plus loin, du côté du centre de l'île.")))
            { var p = DDCartesEdition.Embranchement(m, 3, true); if (p != null) Selection.activeGameObject = p.gameObject; GUIUtility.ExitGUI(); }
            if (GUILayout.Button(new GUIContent("Détour vers l'extérieur", "Pareil, du côté de la mer.")))
            { var p = DDCartesEdition.Embranchement(m, 3, false); if (p != null) Selection.activeGameObject = p.gameObject; GUIUtility.ExitGUI(); }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Pour relier deux cases existantes : sélectionnez-les toutes les deux (Ctrl + clic).", MessageType.None);
            GUI.backgroundColor = new Color(1f, 0.75f, 0.7f);
            if (GUILayout.Button(new GUIContent("Supprimer la case (en recousant le chemin)", "Les cases qui menaient ici mèneront à la suivante.")))
            { DDCartesEdition.Supprimer(m); GUIUtility.ExitGUI(); }
            GUI.backgroundColor = Color.white;
        }
    }
}

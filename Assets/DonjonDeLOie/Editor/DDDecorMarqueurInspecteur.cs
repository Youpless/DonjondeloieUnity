// Donjon de l'Oie — l'inspecteur d'un décor posé à la main : le choix du modèle et sa variante.
// Position, rotation (autour de l'axe vertical) et taille se règlent avec les outils habituels (W, E, R).
using UnityEditor;
using UnityEngine;
using DonjonDeLOie.Cartes;

namespace DonjonDeLOie.Editeur
{
    [CustomEditor(typeof(DDDecorMarqueur)), CanEditMultipleObjects]
    public sealed class DDDecorMarqueurInspecteur : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var id = serializedObject.FindProperty("Id");
            if (id.hasMultipleDifferentValues) EditorGUILayout.PropertyField(id, new GUIContent("Décor"));
            else
            {
                string avant = id.stringValue;
                id.stringValue = DDOutilsEditeur.PopupDecor("Décor", id.stringValue);
                if (id.stringValue != avant)
                    foreach (var t in targets) { Undo.RecordObject(((Component)t).gameObject, "Changer de décor"); ((Component)t).gameObject.name = "Décor " + id.stringValue; }
            }
            var g = serializedObject.FindProperty("Graine");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(g, new GUIContent("Variante", "Fait varier la forme d'un exemplaire à l'autre."));
            if (GUILayout.Button("Au hasard", GUILayout.Width(80))) g.intValue = Random.Range(1, 10000);
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Déplacer : W · Tourner : E (seule la rotation verticale compte) · Taille : R (échelle X).\n"
                + "L'aperçu de l'île (objet « Carte ») montre le modèle.", MessageType.None);
        }
    }
}

// Donjon de l'Oie — juste après l'installation : une fenêtre propose de préparer le jeu (il ne reste qu'à appuyer sur
// Play), et une ancienne version (« Les Dés du Dragon ») restée dans le projet est signalée, avec la proposition de la
// mettre à la corbeille. Tout se fait au premier chargement des scripts, sans menu à chercher.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DonjonDeLOie.Editeur
{
    [InitializeOnLoad]
    public static class DDPremierLancement
    {
        // [InitializeOnLoad] : Unity appelle ce constructeur à chaque chargement des scripts (après l'import, à l'ouverture
        // du projet). On attend que l'éditeur ait fini de se mettre à jour avant de poser une question.
        static DDPremierLancement()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += Verifier;
        }

        /// Une clé par projet : on ne propose de préparer le jeu qu'une seule fois dans chaque projet.
        static string Cle { get { return "DonjonDeLOie.PreparationProposee." + Application.dataPath; } }

        public static void Verifier()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Verifier; return; }

            var anciens = AnciennesVersions();
            if (anciens.Count > 0 && !SessionState.GetBool("DonjonDeLOie.AnciennesSignalees", false))
            {
                SessionState.SetBool("DonjonDeLOie.AnciennesSignalees", true);     // une fois par séance d'Unity
                ProposerCorbeille(anciens);
            }

            if (File.Exists(DDGenerateur.Scene) || EditorPrefs.GetBool(Cle, false)) return;
            EditorPrefs.SetBool(Cle, true);
            if (EditorUtility.DisplayDialog("Donjon de l'Oie",
                    "Le Donjon de l'Oie est installé.\n\nPréparer le jeu maintenant ? La scène du jeu est créée et ouverte : "
                    + "il ne restera qu'à appuyer sur Play.\n\n(Plus tard : menu Donjon de l'Oie › Générer le jeu.)",
                    "Préparer le jeu", "Plus tard"))
                DDGenerateur.Generer();
        }

        /// Les dossiers d'une ancienne version : « DesDuDragon » ou « LesDesDuDragon… » à la racine d'Assets, et toute copie
        /// de l'ancien code rangée ailleurs (repérée à son générateur, écrit dans l'espace de noms DesDuDragon).
        public static List<string> AnciennesVersions()
        {
            var l = new List<string>();
            foreach (var d in AssetDatabase.GetSubFolders("Assets"))
            {
                string chemin = d.Replace('\\', '/');
                if (!chemin.StartsWith("Assets/")) chemin = "Assets/" + chemin;
                string nom = Path.GetFileName(chemin);
                if (nom == "DesDuDragon" || nom.StartsWith("LesDesDuDragon")) l.Add(chemin);
            }
            foreach (var guid in AssetDatabase.FindAssets("DDGenerateur t:MonoScript"))
            {
                string chemin = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                const string fin = "/Editor/DDGenerateur.cs";
                if (!chemin.EndsWith(fin)) continue;
                string texte;
                try { texte = File.ReadAllText(chemin); }
                catch (System.Exception) { continue; }
                if (!texte.Contains("namespace DesDuDragon")) continue;
                string racine = chemin.Substring(0, chemin.Length - fin.Length);
                bool deja = false;
                foreach (var a in l) if (racine == a || racine.StartsWith(a + "/")) deja = true;
                if (!deja) l.Add(racine);
            }
            return l;
        }

        static void ProposerCorbeille(List<string> anciens)
        {
            string liste = "• " + string.Join("\n• ", anciens.ToArray());
            if (!EditorUtility.DisplayDialog("Donjon de l'Oie",
                    "Une ancienne version du jeu (« Les Dés du Dragon ») est encore dans le projet :\n\n" + liste
                    + "\n\nLe Donjon de l'Oie la remplace. La mettre à la corbeille ? (Elle restera récupérable dans la corbeille de l'ordinateur.)",
                    "Mettre à la corbeille", "Garder"))
                return;
            var echecs = new List<string>();
            foreach (var d in anciens) if (!AssetDatabase.MoveAssetToTrash(d)) echecs.Add(d);
            AssetDatabase.Refresh();
            if (echecs.Count > 0)
                EditorUtility.DisplayDialog("Donjon de l'Oie", "Ces dossiers n'ont pas pu être supprimés :\n\n• " + string.Join("\n• ", echecs.ToArray())
                    + "\n\nSupprimez-les à la main dans la fenêtre Project (clic droit › Delete).", "D'accord");
        }
    }
}

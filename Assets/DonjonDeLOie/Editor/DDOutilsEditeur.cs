// Donjon de l'Oie — outils communs aux fenêtres et inspecteurs de l'éditeur : le fichier du catalogue, les listes
// d'identifiants (événements, monstres, objets, décors) et les menus déroulants qui les proposent.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Modeles;

namespace DonjonDeLOie.Editeur
{
    public static class DDOutilsEditeur
    {
        public const string Racine = "Assets/DonjonDeLOie";
        public const string CheminCatalogue = Racine + "/Resources/DDCatalogue.asset";
        public const string DossierCartesJeu = Racine + "/Resources/" + DDCarte.DossierResources;   // cartes proposées dans le salon
        public const string DossierCartes = Racine + "/Cartes";                                     // scènes des cartes

        /// Le catalogue du projet (null s'il n'existe pas et que creer est faux).
        public static DDCatalogue Catalogue(bool creer)
        {
            var c = AssetDatabase.LoadAssetAtPath<DDCatalogue>(CheminCatalogue);
            if (c != null || !creer) return c;
            AssurerDossier(Racine + "/Resources");
            c = ScriptableObject.CreateInstance<DDCatalogue>();
            c.RemplirParDefaut();
            AssetDatabase.CreateAsset(c, CheminCatalogue);
            AssetDatabase.SaveAssets();
            Debug.Log("Donjon de l'Oie : catalogue créé (" + CheminCatalogue + "), copie des valeurs d'origine.");
            return c;
        }

        /// Met le catalogue du projet en service dans l'éditeur (vérification des cartes, aperçu), hors partie en cours.
        public static void AppliquerCatalogue()
        {
            if (EditorApplication.isPlaying) return;
            var c = Catalogue(false);
            if (c != null) c.Appliquer(); else Donnees.Reinitialiser();
        }

        /// Crée un dossier et ses parents au besoin (« Assets/A/B/C »).
        public static void AssurerDossier(string chemin)
        {
            var parties = chemin.Split('/');
            string courant = parties[0];
            for (int i = 1; i < parties.Length; i++)
            {
                string suivant = courant + "/" + parties[i];
                if (!AssetDatabase.IsValidFolder(suivant)) AssetDatabase.CreateFolder(courant, parties[i]);
                courant = suivant;
            }
        }

        // ------------------------------------------------------------------ listes d'identifiants

        /// Les événements connus : ceux du catalogue s'il existe, sinon ceux d'origine.
        public static List<DefEvenement> Evenements()
        {
            var c = Catalogue(false);
            return c != null ? c.Evenements : Donnees.Evenements;
        }

        public static void Monstres(List<string> ids, List<string> noms)
        {
            var c = Catalogue(false);
            var l = c != null ? (IList<DefMonstre>)c.Monstres : Donnees.Monstres;
            foreach (var m in l) { ids.Add(m.Id); noms.Add(m.Nom + "  (" + m.Id + ")"); }
        }

        public static void Objets(List<string> ids, List<string> noms)
        {
            var c = Catalogue(false);
            var l = c != null ? (IList<DefObjet>)c.Objets : Donnees.Objets;
            foreach (var o in l) { ids.Add(o.Id); noms.Add(o.Nom + "  (" + o.Id + ")"); }
        }

        /// Les modèles de monstres existants (pour l'apparence d'un monstre créé dans le catalogue).
        public static readonly string[] ModelesMonstres =
            { "rat", "gobelin", "loup", "araignee", "gelee", "squelette", "orc", "spectre", "troll", "wyverne", "dragon", "dragonnoie" };

        /// Bâtiments puis éléments de nature : tout ce qu'on peut poser à côté d'une case ou à la main.
        public static List<string> IdsDecors()
        {
            var l = new List<string>(DecorsModeles.Speciaux);
            l.Remove("montagne_antre");
            for (int z = 0; z <= (int)Zone.Antre; z++)
                foreach (var id in DecorsModeles.DeZone((Zone)z)) if (!l.Contains(id)) l.Add(id);
            return l;
        }

        // ------------------------------------------------------------------ menus déroulants

        /// Un menu déroulant d'identifiants. « vide » (si non nul) est proposé en premier pour la valeur "".
        /// Un identifiant inconnu reste affiché (en rouge dans le libellé) au lieu d'être effacé.
        public static string PopupId(string libelle, string actuel, List<string> ids, List<string> noms, string vide)
        {
            var options = new List<string>(); var valeurs = new List<string>();
            if (vide != null) { options.Add(vide); valeurs.Add(""); }
            for (int i = 0; i < ids.Count; i++) { options.Add(noms[i]); valeurs.Add(ids[i]); }
            actuel = actuel ?? "";
            int sel = valeurs.IndexOf(actuel);
            if (sel < 0) { options.Insert(0, "⚠ inconnu : " + actuel); valeurs.Insert(0, actuel); sel = 0; }
            int n = libelle == null ? EditorGUILayout.Popup(sel, options.ToArray())
                                    : EditorGUILayout.Popup(libelle, sel, options.ToArray());
            return valeurs[n];
        }

        public static string PopupEvenement(string libelle, string actuel)
        {
            var ids = new List<string>(); var noms = new List<string>();
            foreach (var e in Evenements()) { ids.Add(e.Id); noms.Add(e.Nom + "  (" + e.Id + ")"); }
            return PopupId(libelle, actuel, ids, noms, null);
        }

        public static string PopupLieu(string libelle, string actuel)
        {
            var ids = new List<string> { "-" }; var noms = new List<string> { "Aucun bâtiment" };
            foreach (var id in IdsDecors()) { ids.Add(id); noms.Add(id); }
            return PopupId(libelle, actuel, ids, noms, "Automatique (selon le type de la case)");
        }

        public static string PopupDecor(string libelle, string actuel)
        {
            var ids = IdsDecors();
            return PopupId(libelle, actuel, ids, new List<string>(ids), null);
        }

        // ------------------------------------------------------------------ renommer un événement partout

        /// Remplace un identifiant d'événement dans les cases de la scène ouverte et dans les cartes enregistrées.
        public static int RenommerEvenement(string ancien, string nouveau)
        {
            int n = 0;
            foreach (var m in Object.FindObjectsByType<DDCaseMarqueur>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                for (int i = 0; i < m.Evenements.Count; i++)
                    if (m.Evenements[i] == ancien) { Undo.RecordObject(m, "Renommer l'événement"); m.Evenements[i] = nouveau; n++; }
            foreach (var guid in AssetDatabase.FindAssets("t:DDCarte"))
            {
                var c = AssetDatabase.LoadAssetAtPath<DDCarte>(AssetDatabase.GUIDToAssetPath(guid));
                if (c == null || c.Carte == null) continue;
                bool change = false;
                foreach (var k in c.Carte.Cases)
                    for (int i = 0; i < k.Evenements.Count; i++)
                        if (k.Evenements[i] == ancien) { if (!change) Undo.RecordObject(c, "Renommer l'événement"); k.Evenements[i] = nouveau; change = true; n++; }
                if (change) EditorUtility.SetDirty(c);
            }
            return n;
        }

        /// Nombre de cases (scène ouverte) qui utilisent cet événement.
        public static int Utilisations(string id)
        {
            int n = 0;
            foreach (var m in Object.FindObjectsByType<DDCaseMarqueur>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (m.Evenements.Contains(id)) n++;
            return n;
        }

        /// Un identifiant libre : base, base_2, base_3…
        public static string IdLibre(string baseId, ICollection<string> pris)
        {
            if (!pris.Contains(baseId)) return baseId;
            for (int i = 2; ; i++) if (!pris.Contains(baseId + "_" + i)) return baseId + "_" + i;
        }

        public static GUIStyle Titre
        {
            get { var s = new GUIStyle(EditorStyles.boldLabel); s.fontSize = 13; return s; }
        }
    }
}

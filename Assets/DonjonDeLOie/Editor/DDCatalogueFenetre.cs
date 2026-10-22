// Donjon de l'Oie — la fenêtre du catalogue (menu Donjon de l'Oie › Catalogue) : prix et valeurs des équipements,
// monstres, classes, événements et réglages de la partie. Tout est rangé dans Resources/DDCatalogue.asset, que le jeu lit
// au lancement. Chaque modification s'annule avec Ctrl+Z.
// Ce fichier : la fenêtre, ses onglets et la liste de gauche (commune à tous les onglets).
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    public sealed partial class DDCatalogueFenetre : EditorWindow
    {
        static readonly string[] Onglets = { "Équipements", "Monstres", "Classes", "Événements", "Réglages" };
        const int OngletEvenements = 3;

        int onglet;
        readonly int[] selection = new int[5];
        Vector2 defilListe, defilDetail;
        string filtre = "";
        DDCatalogue cat;
        SerializedObject so;
        string demande;               // événement à montrer (bouton « Modifier » d'une case)

        [MenuItem("Donjon de l'Oie/Catalogue (équipements, monstres, événements)…", false, 20)]
        public static DDCatalogueFenetre Ouvrir()
        {
            var f = GetWindow<DDCatalogueFenetre>("Catalogue");
            f.minSize = new Vector2(760, 460);
            f.Show();
            return f;
        }

        /// Ouvre la fenêtre sur un événement (bouton « Modifier » de l'inspecteur d'une case).
        public static void OuvrirEvenement(string id)
        {
            var f = Ouvrir();
            f.Charger(true);
            f.demande = id;           // montré au prochain dessin de la fenêtre (voir PlusTard)
            f.Repaint();
        }

        /// Ce qui change l'élément affiché (onglet, sélection, ajout, suppression…) attend la fin de l'événement en cours.
        /// Un champ « Identifiant » en cours de frappe est ainsi validé sur l'élément qu'il montrait, et non recopié
        /// sur le suivant (Unity retrouve ces champs par leur numéro de contrôle, le même d'un élément à l'autre).
        void PlusTard(Action a)
        {
            GUI.FocusControl(null);
            EditorApplication.delayCall += () =>
            {
                if (this == null || cat == null) return;    // fenêtre ou catalogue disparus entre-temps
                a();
                Repaint();
            };
        }

        /// Un changement de structure du catalogue (ajout, suppression, déplacement), annulable par Ctrl+Z.
        void Modifier(Action a)
        {
            PlusTard(() =>
            {
                so.ApplyModifiedProperties();
                Undo.RecordObject(cat, "Catalogue");
                a();
                EditorUtility.SetDirty(cat);
                so.Update();
            });
        }

        void Montrer(string id)
        {
            onglet = OngletEvenements;
            filtre = "";
            defilListe = defilDetail = Vector2.zero;
            int i = cat.Evenements.FindIndex(e => e.Id == id);
            if (i >= 0) selection[OngletEvenements] = i;
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Catalogue");
            Charger(false);
            Undo.undoRedoPerformed += Repaint;      // Ctrl+Z : la fenêtre se redessine aussitôt
        }

        void OnDisable() { Undo.undoRedoPerformed -= Repaint; }
        void OnFocus() { Charger(false); }
        void OnProjectChange() { Charger(false); Repaint(); }

        void Charger(bool creer)
        {
            var c = DDOutilsEditeur.Catalogue(creer);
            if (c == cat && so != null) return;
            cat = c;
            so = cat != null ? new SerializedObject(cat) : null;
        }

        void OnGUI()
        {
            if (cat == null || so == null)
            {
                Charger(false);
                if (cat == null) { SansCatalogue(); return; }
            }
            so.Update();
            if (demande != null) { var d = demande; demande = null; PlusTard(() => Montrer(d)); }
            Barre();
            EditorGUILayout.BeginHorizontal();
            switch (onglet)
            {
                case 0: Liste(so.FindProperty("Objets"), TitreObjet, AjouterObjet, true); break;
                case 1: Liste(so.FindProperty("Monstres"), TitreMonstre, AjouterMonstre, true); break;
                case 2: Liste(so.FindProperty("Classes"), TitreClasse, null, false); break;
                case 3: Liste(so.FindProperty("Evenements"), TitreEvenement, AjouterEvenement, true); break;
            }
            defilDetail = EditorGUILayout.BeginScrollView(defilDetail);
            switch (onglet)
            {
                case 0: Detail(so.FindProperty("Objets"), DetailObjet); break;
                case 1: Detail(so.FindProperty("Monstres"), DetailMonstre); break;
                case 2: Detail(so.FindProperty("Classes"), DetailClasse); break;
                case 3: Detail(so.FindProperty("Evenements"), DetailEvenement); break;
                default: DetailReglages(); break;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
            so.ApplyModifiedProperties();
        }

        void SansCatalogue()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Catalogue du Donjon de l'Oie", DDOutilsEditeur.Titre);
            EditorGUILayout.HelpBox("Il n'y a pas encore de catalogue dans ce projet : le jeu utilise les valeurs d'origine, écrites dans le code.\n\n"
                + "Créez-le pour modifier les prix, les bonus, les monstres, les classes et les événements. Il sera rangé dans\n"
                + DDOutilsEditeur.CheminCatalogue + " et lu par le jeu à chaque lancement.", MessageType.Info);
            if (GUILayout.Button("Créer le catalogue (copie des valeurs d'origine)", GUILayout.Height(32))) Charger(true);
        }

        void Barre()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            int o = GUILayout.Toolbar(onglet, Onglets, EditorStyles.toolbarButton, GUILayout.Width(470));
            if (o != onglet) PlusTard(() => { onglet = o; filtre = ""; defilListe = defilDetail = Vector2.zero; });
            GUILayout.FlexibleSpace();
            if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Appliquer en jeu", "Met ces valeurs en service tout de suite (la partie en cours les utilise dès maintenant)."), EditorStyles.toolbarButton))
                { so.ApplyModifiedProperties(); cat.Appliquer(); }
            if (GUILayout.Button(new GUIContent("Fichier", "Montre le fichier du catalogue dans le projet."), EditorStyles.toolbarButton))
                { Selection.activeObject = cat; EditorGUIUtility.PingObject(cat); }
            if (GUILayout.Button(new GUIContent("Enregistrer", "Écrit le catalogue sur le disque (Fichier › Enregistrer le projet le fait aussi)."), EditorStyles.toolbarButton))
                { so.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(cat); }
            if (GUILayout.Button(new GUIContent("Valeurs d'origine…", "Remet tout le catalogue aux valeurs écrites dans le code."), EditorStyles.toolbarButton))
                PlusTard(() =>      // la boîte de dialogue s'ouvre hors du dessin de la fenêtre
                {
                    if (!EditorUtility.DisplayDialog("Valeurs d'origine", "Remettre tout le catalogue (équipements, monstres, classes, événements, réglages) aux valeurs d'origine ?\n\nCtrl+Z permet de revenir en arrière.", "Remettre", "Annuler")) return;
                    Undo.RecordObject(cat, "Valeurs d'origine");
                    cat.RemplirParDefaut();
                    EditorUtility.SetDirty(cat);
                    so.Update();
                });
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ liste de gauche

        void Liste(SerializedProperty tableau, Func<SerializedProperty, string> titre, Action ajouter, bool modifiable)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            filtre = EditorGUILayout.TextField(filtre, EditorStyles.toolbarSearchField);
            defilListe = EditorGUILayout.BeginScrollView(defilListe, GUILayout.ExpandHeight(true));
            int sel = Mathf.Clamp(selection[onglet], 0, Mathf.Max(0, tableau.arraySize - 1));
            for (int i = 0; i < tableau.arraySize; i++)
            {
                string t = titre(tableau.GetArrayElementAtIndex(i));
                if (filtre.Length > 0 && t.IndexOf(filtre, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Toggle(i == sel, t, EditorStyles.miniButton) && i != sel) { int k = i, o = onglet; PlusTard(() => selection[o] = k); }
            }
            EditorGUILayout.EndScrollView();
            selection[onglet] = sel;
            if (modifiable)
            {
                int n = tableau.arraySize;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Ajouter", "Ajoute un élément à la fin de la liste."))) Modifier(() => { ajouter(); selection[onglet] = int.MaxValue; });
                GUI.enabled = n > 0;
                if (GUILayout.Button(new GUIContent("Dupliquer", "Copie l'élément choisi."))) { int k = sel; Modifier(() => Dupliquer(k)); }
                if (GUILayout.Button(new GUIContent("Supprimer", "Retire l'élément choisi (Ctrl+Z pour le récupérer).")))
                {
                    int k = sel;
                    Modifier(() => Supprimer(k));
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = sel > 0;
                if (GUILayout.Button("▲ Monter")) { int k = sel; Modifier(() => Deplacer(k, k - 1)); }
                GUI.enabled = sel < n - 1;
                if (GUILayout.Button("▼ Descendre")) { int k = sel; Modifier(() => Deplacer(k, k + 1)); }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        void Detail(SerializedProperty tableau, Action<SerializedProperty> dessiner)
        {
            if (onglet == 2) ClassesManquantes();
            if (tableau.arraySize == 0) { EditorGUILayout.HelpBox("La liste est vide : le jeu garde les valeurs d'origine. Cliquez sur « Ajouter ».", MessageType.Info); return; }
            int i = Mathf.Clamp(selection[onglet], 0, tableau.arraySize - 1);
            selection[onglet] = i;
            dessiner(tableau.GetArrayElementAtIndex(i));
        }

        /// Les six classes doivent être là (on ne peut ni en ajouter ni en retirer : leurs talents sont écrits dans les règles).
        void ClassesManquantes()
        {
            var manquantes = new List<DefClasse>();
            foreach (var d in Donnees.CopieClassesParDefaut())
                if (!cat.Classes.Exists(c => c != null && c.Classe == d.Classe)) manquantes.Add(d);
            if (manquantes.Count == 0) return;
            EditorGUILayout.HelpBox(manquantes.Count + " classe(s) absente(s) du catalogue : le jeu prend leurs valeurs d'origine.", MessageType.Warning);
            if (GUILayout.Button("Compléter avec les classes d'origine")) Modifier(() => cat.Classes.AddRange(manquantes));
        }

        /// La liste C# de l'onglet courant (pour les changements de structure).
        System.Collections.IList ListeCourante()
        {
            switch (onglet)
            {
                case 0: return cat.Objets;
                case 1: return cat.Monstres;
                case 2: return cat.Classes;
                default: return cat.Evenements;
            }
        }

        void Dupliquer(int i)
        {
            var l = ListeCourante();
            if (i < 0 || i >= l.Count) return;
            object copie;
            var pris = Ids();
            if (onglet == 0) { var o = cat.Objets[i].Copie(); o.Id = DDOutilsEditeur.IdLibre(o.Id + "_copie", pris); o.Nom += " (copie)"; copie = o; }
            else if (onglet == 1) { var m = cat.Monstres[i].Copie(); m.Id = DDOutilsEditeur.IdLibre(m.Id + "_copie", pris); m.Nom += " (copie)"; m.Boss = false; if (string.IsNullOrEmpty(m.Apparence)) m.Apparence = cat.Monstres[i].Id; copie = m; }
            else { var e = cat.Evenements[i].Copie(); e.Id = DDOutilsEditeur.IdLibre(e.Id + "_copie", pris); e.Nom += " (copie)"; copie = e; }
            l.Insert(i + 1, copie);
            selection[onglet] = i + 1;
        }

        void Supprimer(int i)
        {
            var l = ListeCourante();
            if (i < 0 || i >= l.Count) return;
            l.RemoveAt(i);
            selection[onglet] = Mathf.Max(0, i - 1);
        }

        void Deplacer(int de, int vers)
        {
            var l = ListeCourante();
            if (de < 0 || vers < 0 || de >= l.Count || vers >= l.Count) return;
            var x = l[de]; l.RemoveAt(de); l.Insert(vers, x);
            selection[onglet] = vers;
        }

        /// Les identifiants déjà pris dans l'onglet courant.
        HashSet<string> Ids()
        {
            var h = new HashSet<string>();
            if (onglet == 0) foreach (var o in cat.Objets) h.Add(o.Id);
            else if (onglet == 1) foreach (var m in cat.Monstres) h.Add(m.Id);
            else foreach (var e in cat.Evenements) h.Add(e.Id);
            return h;
        }

        /// Avertit si un identifiant est vide ou utilisé deux fois dans le tableau.
        static void VerifierId(SerializedProperty tableau, SerializedProperty element)
        {
            string id = element.FindPropertyRelative("Id").stringValue;
            if (string.IsNullOrEmpty(id)) { EditorGUILayout.HelpBox("Identifiant vide : cet élément sera ignoré par le jeu.", MessageType.Error); return; }
            int n = 0;
            for (int i = 0; i < tableau.arraySize; i++) if (tableau.GetArrayElementAtIndex(i).FindPropertyRelative("Id").stringValue == id) n++;
            if (n > 1) EditorGUILayout.HelpBox("L'identifiant « " + id + " » est utilisé " + n + " fois : seul le premier sera trouvé par le jeu.", MessageType.Error);
        }

        static GUIContent G(string texte, string aide) { return new GUIContent(texte, aide); }

        static void Champ(SerializedProperty parent, string nom, string texte, string aide)
        {
            EditorGUILayout.PropertyField(parent.FindPropertyRelative(nom), G(texte, aide), true);
        }

        static void Zones(SerializedProperty zones, string aide)
        {
            EditorGUILayout.LabelField(G("Zones", aide));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            for (int z = 0; z <= (int)Zone.Antre; z++)
            {
                int ou = -1;
                for (int i = 0; i < zones.arraySize; i++) if (zones.GetArrayElementAtIndex(i).intValue == z) ou = i;
                bool avant = ou >= 0;
                bool apres = GUILayout.Toggle(avant, Donnees.NomsZones[z], EditorStyles.miniButton);
                if (apres && !avant) { zones.InsertArrayElementAtIndex(zones.arraySize); zones.GetArrayElementAtIndex(zones.arraySize - 1).intValue = z; }
                if (!apres && avant) zones.DeleteArrayElementAtIndex(ou);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

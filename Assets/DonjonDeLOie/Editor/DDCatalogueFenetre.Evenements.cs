// Donjon de l'Oie — fenêtre du catalogue : l'onglet des événements. Un événement = un texte, une suite d'effets
// (or, PV, téléportation, combat…) et, si on veut, un choix proposé au joueur, chaque option ayant ses propres effets.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    public sealed partial class DDCatalogueFenetre
    {
        static readonly string[] NomsFrancais =
        {
            "Annonce (texte)", "Or", "PV", "Soin complet", "XP", "Niveau", "Avancer", "Reculer", "Téléporter au hasard",
            "Téléporter au départ", "Téléporter vers un lieu", "Combat", "Boutique", "Quêtes", "Objet au hasard", "Objet donné",
            "Faiblesse", "Immobiliser", "Lenteur", "Bénédiction", "Jet de malédiction", "Lever les malédictions"
        };
        /// Un nom par valeur de TypeEffet (un type ajouté au code sans nom français garde son nom d'origine).
        static readonly string[] NomsTypesEffet = NomsDesTypes();
        static string[] NomsDesTypes()
        {
            var valeurs = (TypeEffet[])System.Enum.GetValues(typeof(TypeEffet));
            int n = 0;
            foreach (var v in valeurs) n = Mathf.Max(n, (int)v + 1);
            var noms = new string[n];
            for (int i = 0; i < n; i++) noms[i] = i < NomsFrancais.Length ? NomsFrancais[i] : ((TypeEffet)i).ToString();
            return noms;
        }
        static readonly string[] NomsCibles = { "le joueur", "tous les joueurs", "les autres", "le meneur", "le dernier" };

        static string TitreEvenement(SerializedProperty e)
        {
            string t = e.FindPropertyRelative("Nom").stringValue + " (" + e.FindPropertyRelative("Id").stringValue + ")";
            if (!e.FindPropertyRelative("DansLaPioche").boolValue) t = "◇ " + t;
            if (e.FindPropertyRelative("Choix").arraySize > 0) t += " ⑂";
            return t;
        }

        void AjouterEvenement()
        {
            var e = new DefEvenement(DDOutilsEditeur.IdLibre("nouvel_evenement", Ids()), "Nouvel événement", "{joueur} trouve une bourse oubliée.", Ton.Bon,
                new EffetEvenement(TypeEffet.Or, 10));
            cat.Evenements.Add(e);
        }

        void DetailEvenement(SerializedProperty e)
        {
            var tableau = so.FindProperty("Evenements");
            int index = Mathf.Clamp(selection[OngletEvenements], 0, cat.Evenements.Count - 1);
            var def = cat.Evenements[index];
            EditorGUILayout.LabelField(e.FindPropertyRelative("Nom").stringValue, DDOutilsEditeur.Titre);
            VerifierId(tableau, e);
            var id = e.FindPropertyRelative("Id");
            string ancien = id.stringValue;
            string nouveau = EditorGUILayout.DelayedTextField(G("Identifiant", "Nom interne, utilisé par les cases de l'éditeur de cartes. Le changer met à jour les cases de la scène ouverte et les cartes enregistrées."), ancien);
            if (nouveau != ancien && !string.IsNullOrEmpty(nouveau))
            {
                id.stringValue = nouveau;
                if (!string.IsNullOrEmpty(ancien))
                {
                    int n = DDOutilsEditeur.RenommerEvenement(ancien, nouveau);
                    if (n > 0) Debug.Log("Événement « " + ancien + " » renommé « " + nouveau + " » : " + n + " case(s) mise(s) à jour.");
                }
            }
            int u = DDOutilsEditeur.Utilisations(id.stringValue);
            if (u > 0) EditorGUILayout.LabelField("Utilisé par " + u + " case(s) de la scène ouverte.", EditorStyles.miniLabel);
            Champ(e, "Nom", "Nom", "Titre de l'événement (affiché dans l'éditeur et les menus).");
            EditorGUILayout.LabelField(G("Texte", "L'annonce affichée quand l'événement arrive. {joueur} est remplacé par le nom du héros."));
            var t = e.FindPropertyRelative("Texte");
            t.stringValue = EditorGUILayout.TextArea(t.stringValue, GUILayout.MinHeight(40));
            Champ(e, "Ton", "Ton", "Couleur de l'annonce : Bon (vert), Mauvais (rouge), Neutre, Combat, Quête.");
            var pioche = e.FindPropertyRelative("DansLaPioche");
            EditorGUILayout.PropertyField(pioche, G("Dans la pioche", "Coché : peut sortir sur n'importe quelle case « Événement ». Décoché : seulement sur les cases qui le citent (éditeur de cartes)."));
            GUI.enabled = pioche.boolValue;
            EditorGUILayout.IntSlider(e.FindPropertyRelative("Poids"), 1, 100, G("Fréquence", "Plus le chiffre est grand, plus il sort souvent (les événements d'origine ont 10)."));
            Zones(e.FindPropertyRelative("Zones"), "Régions où il peut sortir. Aucune cochée : partout.");
            GUI.enabled = true;

            EditorGUILayout.Space();
            Effets(e.FindPropertyRelative("Effets"), def.Effets, "Effets (dans l'ordre)", "Appliqués dès que l'événement arrive.");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(G("Choix proposé au joueur", "Facultatif : des options entre lesquelles le joueur choisit, après les effets ci-dessus. Les bots choisissent seuls."), EditorStyles.boldLabel);
            var choix = e.FindPropertyRelative("Choix");
            for (int i = 0; i < choix.arraySize; i++)
            {
                var c = choix.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                c.isExpanded = EditorGUILayout.Foldout(c.isExpanded, "Option " + (i + 1) + " : " + c.FindPropertyRelative("Libelle").stringValue, true);
                int k = i;
                if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0) Modifier(() => Echanger(def.Choix, k, k - 1));
                if (GUILayout.Button("▼", GUILayout.Width(24)) && i < choix.arraySize - 1) Modifier(() => Echanger(def.Choix, k, k + 1));
                if (GUILayout.Button("✕", GUILayout.Width(24))) Modifier(() => def.Choix.RemoveAt(k));
                EditorGUILayout.EndHorizontal();
                if (c.isExpanded && i < def.Choix.Count)
                {
                    Champ(c, "Libelle", "Libellé", "Le texte du bouton.");
                    Champ(c, "Aide", "Aide", "Petite ligne sous le bouton (facultative).");
                    Effets(c.FindPropertyRelative("Effets"), def.Choix[i].Effets, "Effets de cette option", null);
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Ajouter une option")) Modifier(() => def.Choix.Add(new ChoixEvenement("Option " + (def.Choix.Count + 1), "", new EffetEvenement(TypeEffet.Or, 5))));
            if (choix.arraySize == 1) EditorGUILayout.HelpBox("Une seule option : le joueur n'aura pas vraiment le choix. Ajoutez-en une deuxième (par exemple « Passer son chemin », sans effet).", MessageType.Warning);
            EditorGUILayout.HelpBox("Une option qui coûte de l'or (effet Or négatif) est grisée pour un héros qui n'a pas assez.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("En résumé", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Resume(def), EditorStyles.wordWrappedLabel);
        }

        static void Echanger<T>(List<T> l, int a, int b)
        {
            if (a < 0 || b < 0 || a >= l.Count || b >= l.Count) return;
            var x = l[a]; l[a] = l[b]; l[b] = x;
        }

        /// La liste d'effets d'un événement ou d'une option.
        void Effets(SerializedProperty liste, List<EffetEvenement> l, string titre, string aide)
        {
            EditorGUILayout.LabelField(G(titre, aide ?? ""), EditorStyles.boldLabel);
            if (liste.arraySize == 0) EditorGUILayout.LabelField("Aucun effet.", EditorStyles.miniLabel);
            for (int i = 0; i < liste.arraySize && i < l.Count; i++)
            {
                var f = liste.GetArrayElementAtIndex(i);
                var type = f.FindPropertyRelative("Type");
                var t = (TypeEffet)type.intValue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                type.intValue = EditorGUILayout.Popup(type.intValue, NomsTypesEffet, GUILayout.Width(170));
                t = (TypeEffet)type.intValue;
                var cible = f.FindPropertyRelative("Cible");
                if (EffetEvenement.SeulementLeJoueur(t)) { GUILayout.Label("le joueur", GUILayout.Width(110)); cible.intValue = 0; }
                else cible.intValue = EditorGUILayout.Popup(cible.intValue, NomsCibles, GUILayout.Width(110));
                EditorGUILayout.IntSlider(f.FindPropertyRelative("Chance"), 1, 100, G("Chance %", "Probabilité que cet effet se produise."));
                int k = i;
                if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0) Modifier(() => Echanger(l, k, k - 1));
                if (GUILayout.Button("▼", GUILayout.Width(24)) && i < liste.arraySize - 1) Modifier(() => Echanger(l, k, k + 1));
                if (GUILayout.Button("✕", GUILayout.Width(24))) Modifier(() => l.RemoveAt(k));
                EditorGUILayout.EndHorizontal();
                string sensValeur = SensValeur(t);
                if (sensValeur != null) EditorGUILayout.PropertyField(f.FindPropertyRelative("Valeur"), G(sensValeur, EffetEvenement.Aide(t)));
                Parametre(f.FindPropertyRelative("Parametre"), t);
                EditorGUILayout.LabelField(EffetEvenement.Aide(t), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Ajouter un effet", GUILayout.Width(160))) Modifier(() => l.Add(new EffetEvenement(TypeEffet.Or, 5)));
        }

        /// Le libellé du champ « Valeur » pour ce type d'effet ; null s'il ne sert pas.
        static string SensValeur(TypeEffet t)
        {
            switch (t)
            {
                case TypeEffet.Or: return "Or (négatif : perdu)";
                case TypeEffet.PV: return "PV (négatif : blessure)";
                case TypeEffet.XP: return "XP (négatif : perdue)";
                case TypeEffet.Niveau: return "Niveaux (négatif : perdus)";
                case TypeEffet.Avancer: case TypeEffet.Reculer: return "Cases";
                case TypeEffet.TeleporterHasard: case TypeEffet.TeleporterCase: return "Effet d'arrivée (1 = oui)";
                case TypeEffet.Boutique: return "Remise (%)";
                case TypeEffet.ObjetHasard: case TypeEffet.ObjetDonne: return "Or si le sac est plein";
                case TypeEffet.Faiblesse: case TypeEffet.Immobiliser: return "Tours";
                case TypeEffet.Benediction: return "Bonus au prochain combat";
            }
            return null;
        }

        void Parametre(SerializedProperty p, TypeEffet t)
        {
            var ids = new List<string>(); var noms = new List<string>();
            switch (t)
            {
                case TypeEffet.Annonce:
                    p.stringValue = EditorGUILayout.TextField(G("Texte", "{joueur} = le nom du héros."), p.stringValue);
                    break;
                case TypeEffet.Quetes:
                    p.stringValue = EditorGUILayout.TextField(G("Proposées par", "Le nom affiché (ex. « L'ermite »)."), p.stringValue);
                    break;
                case TypeEffet.Combat:
                    MonstresDuCatalogue(ids, noms);
                    p.stringValue = DDOutilsEditeur.PopupId("Monstre", p.stringValue, ids, noms, "Un monstre de la zone de la case");
                    break;
                case TypeEffet.ObjetDonne:
                    ObjetsDuCatalogue(ids, noms);
                    p.stringValue = DDOutilsEditeur.PopupId("Objet", p.stringValue, ids, noms, null);
                    break;
                case TypeEffet.ObjetHasard:
                    ids.AddRange(new[] { "Potion", "Piege", "Parchemin" }); noms.AddRange(new[] { "Une potion", "Un piège", "Un parchemin" });
                    p.stringValue = DDOutilsEditeur.PopupId("Genre d'objet", p.stringValue, ids, noms, "N'importe quel consommable");
                    break;
                case TypeEffet.Boutique:
                    p.stringValue = EditorGUILayout.TextField(G("Objets vendus", "Identifiants séparés par des virgules (ex. potion,elixir,bottes). Vide : 5 objets au hasard."), p.stringValue);
                    ObjetsDuCatalogue(ids, noms);
                    string ajout = DDOutilsEditeur.PopupId("   ajouter…", "", ids, noms, "—");
                    if (ajout != "") p.stringValue = (string.IsNullOrEmpty(p.stringValue) ? "" : p.stringValue + ",") + ajout;
                    break;
                case TypeEffet.TeleporterCase:
                    LieuxNommes(ids);
                    if (ids.Count > 0) p.stringValue = DDOutilsEditeur.PopupId("Lieu", p.stringValue, ids, ids, null);
                    else p.stringValue = EditorGUILayout.TextField(G("Lieu", "Le nom d'une case nommée de la carte."), p.stringValue);
                    break;
                default:
                    if (!string.IsNullOrEmpty(p.stringValue)) p.stringValue = "";
                    break;
            }
        }

        /// Les monstres qu'un événement peut faire surgir (pas le Dragonnoie : il ne se combat que sur sa case).
        void MonstresDuCatalogue(List<string> ids, List<string> noms) { foreach (var m in cat.Monstres) if (!m.Errant) { ids.Add(m.Id); noms.Add(m.Nom + "  (" + m.Id + ")"); } }
        void ObjetsDuCatalogue(List<string> ids, List<string> noms) { foreach (var o in cat.Objets) { ids.Add(o.Id); noms.Add(o.Nom + "  (" + o.Id + ")"); } }

        /// Les lieux nommés : ceux de la carte ouverte dans l'éditeur, puis ceux de la carte d'origine.
        static void LieuxNommes(List<string> l)
        {
            foreach (var m in Object.FindObjectsByType<DDCaseMarqueur>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(m.NomLieu) && !l.Contains(m.NomLieu)) l.Add(m.NomLieu);
            var p = Plateau.Standard();
            for (int i = 0; i < p.Nombre; i++) if (!string.IsNullOrEmpty(p[i].Nom) && !l.Contains(p[i].Nom)) l.Add(p[i].Nom);
        }

        // ------------------------------------------------------------------ résumé lisible

        static string Resume(DefEvenement e)
        {
            var s = new System.Text.StringBuilder();
            s.Append(e.DansLaPioche ? "Peut sortir sur toute case « Événement »" + (e.Zones.Count > 0 ? " de ces zones" : "") + " (fréquence " + e.Poids + ")."
                                    : "Ne sort que sur les cases qui le citent.");
            if (e.Effets.Count > 0) s.Append("\nEffets : " + Phrase(e.Effets) + ".");
            for (int i = 0; i < e.Choix.Count; i++)
                s.Append("\nOption « " + e.Choix[i].Libelle + " » : " + (e.Choix[i].Effets.Count > 0 ? Phrase(e.Choix[i].Effets) : "rien") + ".");
            return s.ToString();
        }

        static string Phrase(List<EffetEvenement> l)
        {
            var parts = new List<string>();
            foreach (var f in l)
            {
                string qui = EffetEvenement.SeulementLeJoueur(f.Type) ? "" : NomsCibles[(int)f.Cible] + " : ";
                string v = f.Valeur > 0 ? "+" + f.Valeur : f.Valeur.ToString();
                string quoi;
                switch (f.Type)
                {
                    case TypeEffet.Or: quoi = v + " or"; break;
                    case TypeEffet.PV: quoi = v + " PV"; break;
                    case TypeEffet.XP: quoi = v + " XP"; break;
                    case TypeEffet.Niveau: quoi = v + " niveau(x)"; break;
                    case TypeEffet.Avancer: quoi = "avance de " + f.Valeur + " case(s)"; break;
                    case TypeEffet.Reculer: quoi = "recule de " + f.Valeur + " case(s)"; break;
                    case TypeEffet.Combat: quoi = "combat " + (string.IsNullOrEmpty(f.Parametre) ? "un monstre de la zone" : f.Parametre); break;
                    case TypeEffet.ObjetDonne: quoi = "reçoit " + f.Parametre; break;
                    case TypeEffet.TeleporterCase: quoi = "va à " + f.Parametre; break;
                    case TypeEffet.Faiblesse: case TypeEffet.Immobiliser: quoi = NomsTypesEffet[(int)f.Type].ToLowerInvariant() + " " + f.Valeur + " tour(s)"; break;
                    default: quoi = NomsTypesEffet[(int)f.Type].ToLowerInvariant(); break;
                }
                parts.Add(qui + quoi + (f.Chance < 100 ? " (" + f.Chance + " %)" : ""));
            }
            return string.Join(", ", parts.ToArray());
        }
    }
}

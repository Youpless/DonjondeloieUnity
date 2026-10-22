// Donjon de l'Oie — fenêtre du catalogue : les fiches des équipements, des monstres, des classes et les réglages.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    public sealed partial class DDCatalogueFenetre
    {
        // ------------------------------------------------------------------ équipements

        static string TitreObjet(SerializedProperty o)
        {
            var t = (TypeObjet)o.FindPropertyRelative("Type").intValue;
            return "[" + t + "] " + o.FindPropertyRelative("Nom").stringValue + " · " + o.FindPropertyRelative("Prix").intValue + " or";
        }

        void AjouterObjet()
        {
            var id = DDOutilsEditeur.IdLibre("nouvel_objet", Ids());
            cat.Objets.Add(new DefObjet(id, "Nouvel objet", TypeObjet.Arme, 20, 1, EffetObjet.Aucun, "+1 en attaque."));
        }

        /// Ce que veut dire « Bonus » pour un type et un effet d'objet ; null s'il ne sert pas.
        static string SensBonus(TypeObjet t, EffetObjet e)
        {
            if (t == TypeObjet.Arme) return "Points d'attaque ajoutés.";
            if (t == TypeObjet.Armure) return "Points de défense : dégâts des monstres et de la foudre réduits d'autant.";
            switch (e)
            {
                case EffetObjet.Bottes: return "Cases ajoutées à chaque jet de déplacement.";
                case EffetObjet.Anneau: return "PV maximum ajoutés.";
                case EffetObjet.Soin: return "PV rendus.";
                case EffetObjet.PiegeLoups: return "PV perdus par la victime (qui passe aussi son prochain tour).";
                case EffetObjet.PiegeFosse: return "PV perdus par la victime.";
                case EffetObjet.PiegeFilet: return "Or volé à la victime.";
                case EffetObjet.PiegeRune: return "Niveaux perdus par la victime.";
                case EffetObjet.SortMalediction: return "Niveaux perdus par la cible si elle ne résiste pas (5+ au dé).";
                case EffetObjet.SortFoudre: return "Dégâts (moins l'armure de la cible).";
            }
            return null;
        }

        /// Les effets qui vont avec un type d'objet (le premier est proposé par défaut).
        static EffetObjet[] EffetsDuType(TypeObjet t)
        {
            switch (t)
            {
                case TypeObjet.Accessoire: return new[] { EffetObjet.Amulette, EffetObjet.Bottes, EffetObjet.Anneau };
                case TypeObjet.Potion: return new[] { EffetObjet.Soin, EffetObjet.SoinTotal };
                case TypeObjet.Piege: return new[] { EffetObjet.PiegeLoups, EffetObjet.PiegeFosse, EffetObjet.PiegeFilet, EffetObjet.PiegeRune };
                case TypeObjet.Parchemin: return new[] { EffetObjet.SortMalediction, EffetObjet.SortFoudre, EffetObjet.SortLenteur, EffetObjet.SortRappel };
                default: return new[] { EffetObjet.Aucun };
            }
        }

        static readonly Dictionary<EffetObjet, string> NomsEffets = new Dictionary<EffetObjet, string>
        {
            { EffetObjet.Aucun, "Aucun" }, { EffetObjet.Amulette, "Amulette : bloque la prochaine malédiction, rune ou sort" },
            { EffetObjet.Bottes, "Bottes : bonus au déplacement" }, { EffetObjet.Anneau, "Anneau : PV maximum en plus" },
            { EffetObjet.Soin, "Soin : rend des PV" }, { EffetObjet.SoinTotal, "Soin total : rend tous les PV" },
            { EffetObjet.PiegeLoups, "Piège à loups : blesse et immobilise un tour" }, { EffetObjet.PiegeFosse, "Fosse : blesse" },
            { EffetObjet.PiegeFilet, "Filet : vole de l'or" }, { EffetObjet.PiegeRune, "Rune : fait perdre des niveaux" },
            { EffetObjet.SortMalediction, "Malédiction : fait perdre des niveaux (jet de résistance)" }, { EffetObjet.SortFoudre, "Foudre : dégâts" },
            { EffetObjet.SortLenteur, "Lenteur : divise le prochain déplacement" }, { EffetObjet.SortRappel, "Rappel : téléporte le lanceur à la ville ou un sanctuaire" },
        };

        void DetailObjet(SerializedProperty o)
        {
            var tableau = so.FindProperty("Objets");
            EditorGUILayout.LabelField(o.FindPropertyRelative("Nom").stringValue, DDOutilsEditeur.Titre);
            VerifierId(tableau, o);
            var id = o.FindPropertyRelative("Id");
            id.stringValue = EditorGUILayout.DelayedTextField(G("Identifiant", "Nom interne, sans espace ni accent (utilisé par les événements « Objet donné » et les boutiques)."), id.stringValue);
            if (id.stringValue == "potion" || id.stringValue == "foudre" || id.stringValue == "fosse")
                EditorGUILayout.HelpBox("Cet objet est donné au départ (potion à tous, foudre au mage, fosse au voleur) : gardez son identifiant.", MessageType.None);
            Champ(o, "Nom", "Nom", "Affiché dans les boutiques et le sac.");
            var type = o.FindPropertyRelative("Type");
            var effet = o.FindPropertyRelative("Effet");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(type, G("Type", "Arme, armure et accessoires s'équipent ; potions, pièges et parchemins s'utilisent une fois."));
            var effets = EffetsDuType((TypeObjet)type.intValue);
            if (EditorGUI.EndChangeCheck() && System.Array.IndexOf(effets, (EffetObjet)effet.intValue) < 0) effet.intValue = (int)effets[0];
            if (effets.Length > 1)
            {
                var noms = new string[effets.Length];
                int sel = 0;
                for (int i = 0; i < effets.Length; i++) { noms[i] = NomsEffets[effets[i]]; if ((int)effets[i] == effet.intValue) sel = i; }
                sel = EditorGUILayout.Popup(G("Effet", "Ce que fait l'objet. Le chiffre « Bonus » règle sa force."), sel, ToContents(noms));
                effet.intValue = (int)effets[sel];
            }
            else effet.intValue = (int)effets[0];
            EditorGUILayout.PropertyField(o.FindPropertyRelative("Prix"), G("Prix (or)", "Prix en boutique. Le mage paie les parchemins moitié prix, le voleur les pièges."));
            if (o.FindPropertyRelative("Prix").intValue < 0) o.FindPropertyRelative("Prix").intValue = 0;
            string sens = SensBonus((TypeObjet)type.intValue, (EffetObjet)effet.intValue);
            GUI.enabled = sens != null;
            EditorGUILayout.PropertyField(o.FindPropertyRelative("Bonus"), G("Bonus", sens ?? "Ne sert pas pour cet effet."));
            GUI.enabled = true;
            if (sens != null) EditorGUILayout.HelpBox("Bonus : " + sens, MessageType.None);
            Champ(o, "SeulementEnVille", "Seulement en ville", "Vendu uniquement à la case de départ (pièces légendaires), jamais chez les marchands ni dans les coffres.");
            EditorGUILayout.LabelField(G("Description", "Texte affiché dans la boutique."));
            var d = o.FindPropertyRelative("Description");
            d.stringValue = EditorGUILayout.TextArea(d.stringValue, GUILayout.MinHeight(40));
        }

        static GUIContent[] ToContents(string[] s)
        {
            var c = new GUIContent[s.Length];
            for (int i = 0; i < s.Length; i++) c[i] = new GUIContent(s[i]);
            return c;
        }

        // ------------------------------------------------------------------ monstres

        static string TitreMonstre(SerializedProperty m)
        {
            return (m.FindPropertyRelative("Boss").boolValue ? "★ " : "") + (m.FindPropertyRelative("Errant").boolValue ? "◆ " : "") + m.FindPropertyRelative("Nom").stringValue
                + " · force " + m.FindPropertyRelative("Force").intValue;
        }

        void AjouterMonstre()
        {
            var m = new DefMonstre(DDOutilsEditeur.IdLibre("nouveau_monstre", Ids()), "Nouveau monstre", 4, 2, 8, 1, new[] { Zone.Bois }, "Une créature des bois.");
            m.Apparence = "gobelin";
            cat.Monstres.Add(m);
        }

        void DetailMonstre(SerializedProperty m)
        {
            var tableau = so.FindProperty("Monstres");
            EditorGUILayout.LabelField(m.FindPropertyRelative("Nom").stringValue, DDOutilsEditeur.Titre);
            VerifierId(tableau, m);
            var id = m.FindPropertyRelative("Id");
            id.stringValue = EditorGUILayout.DelayedTextField(G("Identifiant", "Nom interne (quêtes de chasse, événements « Combat »)."), id.stringValue);
            Champ(m, "Nom", "Nom", "");
            // apparence
            var ap = m.FindPropertyRelative("Apparence");
            var ids = new List<string>(DDOutilsEditeur.ModelesMonstres);
            bool modelePropre = ids.Contains(id.stringValue);
            ap.stringValue = DDOutilsEditeur.PopupId("Apparence", ap.stringValue, ids, ids, modelePropre ? "Son propre modèle (" + id.stringValue + ")" : null);
            if (!modelePropre && string.IsNullOrEmpty(ap.stringValue)) ap.stringValue = "gobelin";
            EditorGUILayout.PropertyField(m.FindPropertyRelative("Force"), G("Force", "Le héros gagne si son attaque + 1D6 atteint au moins la force du monstre + 1D6."));
            EditorGUILayout.PropertyField(m.FindPropertyRelative("Degats"), G("Dégâts", "PV perdus par le héros vaincu (moins son armure)."));
            EditorGUILayout.PropertyField(m.FindPropertyRelative("Or"), G("Or", "Or gagné en le battant."));
            EditorGUILayout.PropertyField(m.FindPropertyRelative("XP"), G("XP", "Expérience gagnée (" + Donnees.XPParNiveau + " XP = 1 niveau, voir Réglages)."));
            Zones(m.FindPropertyRelative("Zones"), "Les régions où il peut surgir sur une case « Monstre ».");
            var boss = m.FindPropertyRelative("Boss");
            EditorGUILayout.PropertyField(boss, G("Boss de l'antre", "Le monstre de la case « Antre ». Il dort devant sa montagne et ne surgit pas ailleurs."));
            int nb = 0;
            for (int i = 0; i < tableau.arraySize; i++) if (tableau.GetArrayElementAtIndex(i).FindPropertyRelative("Boss").boolValue) nb++;
            if (nb == 0) EditorGUILayout.HelpBox("Aucun boss : la case « Antre » prendra le dernier monstre (non errant) de la liste.", MessageType.Warning);
            if (nb > 1) EditorGUILayout.HelpBox("Plusieurs boss : seul le premier de la liste garde l'antre.", MessageType.Warning);
            var errant = m.FindPropertyRelative("Errant");
            EditorGUILayout.PropertyField(errant, G("Errant (Dragonnoie)", "Il erre sur la carte : grandes envolées, défis, tout l'or des vaincus. Le vaincre gagne la partie. Ses zones, dégâts, or et XP ne servent pas."));
            int ne = 0;
            for (int i = 0; i < tableau.arraySize; i++) if (tableau.GetArrayElementAtIndex(i).FindPropertyRelative("Errant").boolValue) ne++;
            if (errant.boolValue)
                EditorGUILayout.HelpBox("Le monstre errant ne surgit jamais des cases : il change de perchoir au début d'un tour de table sur "
                    + Mathf.Max(1, cat.Reglages.ToursEntreEnvols) + " (voir Réglages). Seule sa force compte au combat."
                    + (ne > 1 ? "\nPlusieurs monstres errants : seul le premier de la liste est sur la carte." : ""), ne > 1 ? MessageType.Warning : MessageType.Info);
            else if (ne == 0)
                EditorGUILayout.HelpBox("Aucun monstre errant dans la liste : le jeu reprend le Dragonnoie d'origine. Pour jouer sans lui, décochez « Dragonnoie actif » dans les Réglages.", MessageType.None);
            EditorGUILayout.LabelField("Description");
            var d = m.FindPropertyRelative("Description");
            d.stringValue = EditorGUILayout.TextArea(d.stringValue, GUILayout.MinHeight(40));
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Equilibre(m.FindPropertyRelative("Force").intValue), MessageType.None);
        }

        /// Chances de victoire d'un héros débutant (attaque 3) et d'un héros bien équipé (attaque 8), 1D6 de chaque côté.
        static string Equilibre(int force)
        {
            return "Chances de victoire : héros débutant (att. 3) " + Chance(3, force) + " %, héros équipé (att. 6) " + Chance(6, force)
                + " %, héros fort (att. 9) " + Chance(9, force) + " % (le héros l'emporte en cas d'égalité).";
        }

        static int Chance(int atk, int force)
        {
            return Mathf.RoundToInt(100f * (float)IA.ChanceMonstre(atk, force));
        }

        // ------------------------------------------------------------------ classes

        static string TitreClasse(SerializedProperty c) { return c.FindPropertyRelative("Nom").stringValue; }

        void DetailClasse(SerializedProperty c)
        {
            EditorGUILayout.LabelField(c.FindPropertyRelative("Nom").stringValue, DDOutilsEditeur.Titre);
            Champ(c, "Nom", "Nom", "");
            Champ(c, "Attaque", "Attaque", "Attaque de base (s'ajoute à l'arme et au dé).");
            Champ(c, "Defense", "Défense", "Défense de base (s'ajoute à l'armure).");
            Champ(c, "Vie", "PV", "Points de vie au départ et après un soin complet.");
            EditorGUILayout.LabelField(G("Talent", "Texte affiché dans le salon. Le talent lui-même est écrit dans les règles."));
            var p = c.FindPropertyRelative("Passif");
            p.stringValue = EditorGUILayout.TextArea(p.stringValue, GUILayout.MinHeight(36));
            EditorGUILayout.HelpBox("L'effet du talent est écrit dans le code des règles : ce texte n'en est que la description.", MessageType.None);
            EditorGUILayout.LabelField("Description");
            var d = c.FindPropertyRelative("Description");
            d.stringValue = EditorGUILayout.TextArea(d.stringValue, GUILayout.MinHeight(36));
        }

        // ------------------------------------------------------------------ réglages

        void DetailReglages()
        {
            var r = so.FindProperty("Reglages");
            EditorGUILayout.LabelField("Réglages de la partie", DDOutilsEditeur.Titre);
            EditorGUILayout.Space();
            Champ(r, "XPParNiveau", "XP par niveau", "Expérience à réunir pour gagner un niveau.");
            Champ(r, "OrDepart", "Or de départ", "Or de chaque héros au début de la partie.");
            Champ(r, "SoldeVille", "Solde à la ville", "Or gagné à chaque passage par la case de départ.");
            Champ(r, "XPVille", "XP à la ville", "XP de renommée gagnée à chaque passage par la case de départ.");
            Champ(r, "TailleSac", "Taille du sac", "Objets à usage unique (potions, pièges, parchemins) qu'on peut porter.");
            Champ(r, "AccessoiresMax", "Accessoires portés", "Nombre d'accessoires équipés en même temps.");
            Champ(r, "PiegesMaxParJoueur", "Pièges par joueur", "Pièges posés en même temps par un même joueur (au-delà, il faut attendre qu'un piège se déclenche).");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Le Dragonnoie", EditorStyles.boldLabel);
            Champ(r, "DragonnoieActif", "Dragonnoie actif", "Décoché : pas de Dragonnoie, on ne gagne qu'au niveau.");
            EditorGUILayout.IntSlider(r.FindPropertyRelative("ToursEntreEnvols"), 1, 10, G("Tours entre deux envols", "Il change de perchoir au début d'un tour de table sur N (un tour de table : chaque joueur a joué)."));
            EditorGUILayout.HelpBox("Sa force se règle dans l'onglet Monstres (fiche marquée ◆). En simulation, à 4 bots sur la carte d'origine : force 9 → environ un tiers des parties gagnées en le terrassant.", MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("La nuit", EditorStyles.boldLabel);
            Champ(r, "NuitsActives", "Nuits actives", "Décoché : jamais de nuit, ni d'événement nocturne.");
            EditorGUILayout.IntSlider(r.FindPropertyRelative("ToursEntreNuits"), 1, 12, G("Tours entre deux nuits", "La nuit tombe à la fin de chaque N-ième tour de table (5 : après les tours 5, 10, 15…). Un seul événement par nuit, tiré parmi ceux qui sont cochés et possibles."));
            EditorGUI.indentLevel++;
            Champ(r, "NuitVoleur", "Le voleur", "Un joueur qui a au moins 2 or en perd la moitié.");
            Champ(r, "NuitTrouDeVer", "Le trou de ver", "Deux joueurs sur deux cases différentes échangent leur place.");
            Champ(r, "NuitBonneNuit", "La bonne nuit", "Un joueur gagne un niveau (jamais celui de la victoire).");
            Champ(r, "NuitArene", "L'arène du diable", "Tournoi en duels dans un ordre tiré au sort : chaque défaite coûte un niveau, le vainqueur gagne un niveau (celui de la victoire compris) et de l'or.");
            Champ(r, "OrArene", "Or du vainqueur de l'arène", "L'or que remporte le dernier champion de l'arène.");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Le mini-jeu de l'évadé", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(r.FindPropertyRelative("DureeEvade"), 10, 90, G("Durée de la recherche (s)", "Le temps pour cliquer sur l'évadé caché dans le marché (une erreur coûte 3 secondes). Les bots, eux, le trouvent un peu plus d'une fois sur deux."));
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Le niveau à atteindre se choisit dans le salon (et une carte peut en conseiller un).", MessageType.Info);
        }
    }
}

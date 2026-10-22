// Donjon de l'Oie — les opérations de l'éditeur de cartes sur les marqueurs de la scène : ajouter une case, créer un
// embranchement, relier, supprimer en recousant le chemin, répartir les cases. Tout passe par Undo (Ctrl+Z).
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DonjonDeLOie.Cartes;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Editeur
{
    public static class DDCartesEdition
    {
        public const float Pas = 3.2f;          // écart conseillé entre deux cases, en mètres

        /// Les cases qui mènent à celle-ci.
        public static List<DDCaseMarqueur> Precedents(DDCaseMarqueur m)
        {
            var l = new List<DDCaseMarqueur>();
            var r = m.GetComponentInParent<DDEditeurCarte>();
            if (r == null) return l;
            foreach (var c in r.Cases()) if (c.Suivants.Contains(m)) l.Add(c);
            return l;
        }

        /// Crée une case (type Chemin, même zone que le modèle) à cette position, rangée juste après le modèle.
        public static DDCaseMarqueur NouvelleCase(DDEditeurCarte racine, Vector3 position, DDCaseMarqueur modele, string action)
        {
            var dossier = racine.Dossier("Cases");
            var go = new GameObject("Case");
            Undo.RegisterCreatedObjectUndo(go, action);
            Undo.SetTransformParent(go.transform, dossier, action);
            go.transform.position = new Vector3(position.x, 0, position.z);
            if (modele != null && modele.transform.parent == dossier) go.transform.SetSiblingIndex(modele.transform.GetSiblingIndex() + 1);
            var m = go.AddComponent<DDCaseMarqueur>();
            if (modele != null) m.Zone = modele.Zone;
            return m;
        }

        /// Ajoute une case entre celle-ci et la suivante de la grande route.
        public static DDCaseMarqueur InsererApres(DDCaseMarqueur a)
        {
            const string action = "Ajouter une case";
            var racine = a.GetComponentInParent<DDEditeurCarte>();
            if (racine == null) return null;
            Undo.SetCurrentGroupName(action);
            int groupe = Undo.GetCurrentGroup();
            var b = a.Suivants.Count > 0 ? a.Suivants[0] : null;
            Vector3 pa = a.transform.position, pos;
            if (b == null) pos = pa + Vector3.right * Pas;
            else
            {
                var d = b.transform.position - pa; d.y = 0;
                float l = d.magnitude;
                // assez de place : à un pas de la case ; sinon au milieu, un peu écartée du chemin
                pos = l >= Pas * 2f ? pa + d / l * Pas : (pa + b.transform.position) * 0.5f + new Vector3(-d.z, 0, d.x).normalized * 1.2f;
            }
            var n = NouvelleCase(racine, pos, a, action);
            Undo.RecordObject(a, action);
            if (b != null) { n.Suivants.Add(b); a.Suivants[0] = n; }
            else a.Suivants.Add(n);
            Renommer(racine);
            Undo.CollapseUndoOperations(groupe);
            return n;
        }

        /// Un embranchement de « longueur » cases qui part d'ici et rejoint la grande route plus loin
        /// (autant de cases plus loin que sa longueur + 1, pour que le raccourci soit utile).
        public static DDCaseMarqueur Embranchement(DDCaseMarqueur a, int longueur, bool versLInterieur)
        {
            const string action = "Créer un embranchement";
            var racine = a.GetComponentInParent<DDEditeurCarte>();
            if (racine == null || a.Suivants.Count == 0) return null;
            Undo.SetCurrentGroupName(action);
            int groupe = Undo.GetCurrentGroup();
            var cible = a;
            for (int k = 0; k < longueur + 2 && cible.Suivants.Count > 0 && cible.Suivants[0] != null; k++) cible = cible.Suivants[0];
            if (cible == a) return null;
            Vector3 p0 = a.transform.position, p1 = cible.transform.position;
            var centre = Vector3.zero;
            var cases = racine.Cases();
            foreach (var c in cases) centre += c.transform.position;
            centre /= Mathf.Max(1, cases.Count);
            var milieu = (p0 + p1) * 0.5f;
            var cote = new Vector3(-(p1 - p0).z, 0, (p1 - p0).x).normalized;
            if (Vector3.Dot(centre - milieu, cote) < 0 == versLInterieur) cote = -cote;
            float creux = Mathf.Max(3f, Vector3.Distance(p0, p1) * 0.35f);
            var controle = milieu + cote * creux;      // courbe de Bézier entre les deux cases
            DDCaseMarqueur precedent = a, premiere = null;
            for (int k = 1; k <= longueur; k++)
            {
                float t = k / (float)(longueur + 1);
                var p = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * controle + t * t * p1;
                var n = NouvelleCase(racine, p, precedent, action);
                n.Type = k == (longueur + 1) / 2 ? TypeCase.Tresor : TypeCase.Monstre;
                Undo.RecordObject(precedent, action);
                precedent.Suivants.Add(n);
                if (premiere == null) { premiere = n; n.Voie = "Raccourci"; }
                precedent = n;
            }
            precedent.Suivants.Add(cible);
            Renommer(racine);
            Undo.CollapseUndoOperations(groupe);
            return premiere;
        }

        public static void Relier(DDCaseMarqueur de, DDCaseMarqueur vers)
        {
            if (de == null || vers == null || de == vers || de.Suivants.Contains(vers)) return;
            Undo.RecordObject(de, "Relier deux cases");
            de.Suivants.Add(vers);
        }

        /// Supprime la case ; ses cases précédentes mènent désormais à sa suivante.
        public static void Supprimer(DDCaseMarqueur m)
        {
            const string action = "Supprimer une case";
            var racine = m.GetComponentInParent<DDEditeurCarte>();
            Undo.SetCurrentGroupName(action);
            int groupe = Undo.GetCurrentGroup();
            var suivante = m.Suivants.Count > 0 ? m.Suivants[0] : null;
            foreach (var p in Precedents(m))
            {
                Undo.RecordObject(p, action);
                int i = p.Suivants.IndexOf(m);
                if (suivante != null && suivante != p && !p.Suivants.Contains(suivante)) p.Suivants[i] = suivante;
                else p.Suivants.RemoveAt(i);
            }
            if (racine != null && racine.Depart == m)
            {
                Undo.RecordObject(racine, action);
                racine.Depart = suivante;
            }
            Selection.activeGameObject = suivante != null ? suivante.gameObject : (racine != null ? racine.gameObject : null);
            Undo.DestroyObjectImmediate(m.gameObject);
            if (racine != null) Renommer(racine);
            Undo.CollapseUndoOperations(groupe);
        }

        /// La grande boucle, depuis le départ, en suivant toujours le premier lien.
        public static List<DDCaseMarqueur> GrandeBoucle(DDEditeurCarte racine)
        {
            var l = new List<DDCaseMarqueur>();
            var c = racine.Depart;
            if (c == null) { var toutes = racine.Cases(); if (toutes.Count == 0) return l; c = toutes[0]; }
            var vus = new HashSet<DDCaseMarqueur>();
            while (c != null && vus.Add(c)) { l.Add(c); c = c.Suivants.Count > 0 ? c.Suivants[0] : null; }
            return l;
        }

        /// Replace les cases de la grande boucle à intervalles égaux le long de leur tracé (les embranchements ne bougent pas).
        public static void Repartir(DDEditeurCarte racine)
        {
            var boucle = GrandeBoucle(racine);
            int n = boucle.Count;
            if (n < 3) return;
            bool fermee = boucle[n - 1].Suivants.Count > 0 && boucle[n - 1].Suivants[0] == boucle[0];
            var pts = new List<Vector3>();
            foreach (var c in boucle) pts.Add(c.transform.position);
            if (fermee) pts.Add(pts[0]);
            var cumul = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) cumul[i] = cumul[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            float total = cumul[pts.Count - 1];
            if (total < 0.01f) return;
            int segments = fermee ? n : n - 1;
            var nouvelles = new Vector3[n];
            int s = 0;
            for (int k = 0; k < n; k++)
            {
                float d = total * k / segments;
                while (s < pts.Count - 2 && cumul[s + 1] < d) s++;
                float l = cumul[s + 1] - cumul[s];
                nouvelles[k] = Vector3.Lerp(pts[s], pts[s + 1], l > 0 ? (d - cumul[s]) / l : 0);
            }
            for (int k = 0; k < n; k++)
            {
                Undo.RecordObject(boucle[k].transform, "Répartir les cases");
                boucle[k].transform.position = nouvelles[k];
            }
        }

        /// Renomme les objets de la scène dans l'ordre de la hiérarchie (« Case 07 · Taverne »).
        public static void Renommer(DDEditeurCarte racine)
        {
            var cases = racine.Cases();
            for (int i = 0; i < cases.Count; i++)
            {
                string nom = DDEditeurCarte.NomMarqueur(i, cases[i].Type, cases[i].NomLieu);
                if (cases[i].gameObject.name == nom) continue;
                Undo.RecordObject(cases[i].gameObject, "Renommer les cases");
                cases[i].gameObject.name = nom;
            }
        }

        /// Un décor posé à la main, au point visé par la vue de la scène.
        public static DDDecorMarqueur NouveauDecor(DDEditeurCarte racine, string id)
        {
            const string action = "Ajouter un décor";
            var sv = SceneView.lastActiveSceneView;
            var pos = sv != null ? sv.pivot : racine.transform.position;
            var go = new GameObject("Décor " + id);
            Undo.RegisterCreatedObjectUndo(go, action);
            Undo.SetTransformParent(go.transform, racine.Dossier("Décors"), action);
            go.transform.position = new Vector3(pos.x, 0, pos.z);
            var d = go.AddComponent<DDDecorMarqueur>();
            d.Id = id;
            d.Graine = Random.Range(1, 1000);
            return d;
        }
    }
}

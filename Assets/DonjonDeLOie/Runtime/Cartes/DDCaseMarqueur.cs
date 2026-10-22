// Donjon de l'Oie — une case de la carte, posée dans la scène de l'éditeur de cartes. On la déplace comme n'importe quel
// objet (outil Déplacer, touche W) ; son inspecteur règle son type, sa zone, ses événements, son bâtiment et ses liens.
// En jeu, ces marqueurs ne s'affichent pas : la carte est lue une fois au lancement.
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DonjonDeLOie.Cartes
{
    [DisallowMultipleComponent]
    public sealed class DDCaseMarqueur : MonoBehaviour
    {
        [Tooltip("Ce qui se passe quand on s'arrête sur la case.")]
        public TypeCase Type = TypeCase.Chemin;
        [Tooltip("La région : décor, monstres tirés, couleurs du sol.")]
        public Zone Zone = Zone.Village;
        [Tooltip("Nom du lieu (cible des quêtes de voyage). Vide : une case ordinaire.")]
        public string NomLieu = "";
        [Tooltip("Nom du chemin affiché quand cette case ouvre un embranchement.")]
        public string Voie = "";
        [Tooltip("Cases suivantes. La première est la grande route ; les autres sont des embranchements.")]
        public List<DDCaseMarqueur> Suivants = new List<DDCaseMarqueur>();
        [Tooltip("Standard : l'effet du type. Remplacer : un de ses événements à la place. Ajouter : l'effet du type, puis un de ses événements.")]
        public ModeEvenements Mode = ModeEvenements.Standard;
        [Tooltip("Identifiants des événements propres à la case (voir la fenêtre du catalogue).")]
        public List<string> Evenements = new List<string>();
        [Tooltip("Bâtiment posé à côté : vide = automatique selon le type, « - » = aucun, sinon l'id d'un décor.")]
        public string Lieu = "";

        public static Color CouleurType(TypeCase t)
        {
            switch (t)
            {
                case TypeCase.Ville: return new Color(0.24f, 0.39f, 0.69f);
                case TypeCase.Monstre: return new Color(0.72f, 0.22f, 0.18f);
                case TypeCase.Tresor: return new Color(0.89f, 0.68f, 0.24f);
                case TypeCase.Evenement: return new Color(0.25f, 0.53f, 0.79f);
                case TypeCase.Malediction: return new Color(0.48f, 0.25f, 0.66f);
                case TypeCase.Sanctuaire: return new Color(0.9f, 0.93f, 0.95f);
                case TypeCase.Marchand: return new Color(0.88f, 0.48f, 0.18f);
                case TypeCase.Taverne: return new Color(0.55f, 0.37f, 0.24f);
                case TypeCase.Antre: return new Color(0.48f, 0.12f, 0.1f);
                default: return new Color(0.55f, 0.54f, 0.52f);
            }
        }

        DDEditeurCarte Racine { get { return GetComponentInParent<DDEditeurCarte>(); } }

        /// Hauteur du sol en (x, z) quand l'aperçu de l'île est affiché dans l'éditeur (sinon null) : les marqueurs se
        /// dessinent alors sur le relief au lieu d'être cachés dessous.
        public static System.Func<float, float, float> HauteurSol;

        public static Vector3 PositionDessin(Vector3 p)
        {
            if (HauteurSol != null) p.y = Mathf.Max(p.y, HauteurSol(p.x, p.z));
            return p;
        }

        void OnDrawGizmos()
        {
            var r = Racine;
            bool depart = r != null && r.Depart == this;
            var p = PositionDessin(transform.position);
            Gizmos.color = CouleurType(Type);
            Gizmos.DrawCube(p + Vector3.up * 0.15f, new Vector3(1.8f, 0.3f, 1.8f));
            Gizmos.color = depart ? Color.yellow : new Color(0, 0, 0, 0.6f);
            Gizmos.DrawWireCube(p + Vector3.up * 0.15f, new Vector3(depart ? 2.3f : 1.9f, 0.32f, depart ? 2.3f : 1.9f));
            for (int i = 0; i < Suivants.Count; i++)
            {
                var s = Suivants[i];
                if (s == null) continue;
                Gizmos.color = i == 0 ? new Color(1f, 0.95f, 0.75f) : new Color(1f, 0.55f, 0.2f);   // grande route / embranchement
                Fleche(p + Vector3.up * 0.35f, PositionDessin(s.transform.position) + Vector3.up * 0.35f);
            }
#if UNITY_EDITOR
            string titre = (depart ? "[DÉPART] " : "") + (string.IsNullOrEmpty(NomLieu) ? Donnees.NomsTypesCase[(int)Type] : NomLieu);
            if (Evenements.Count > 0) titre += "  [" + Mode + " : " + Evenements.Count + " év.]";
            Handles.Label(p + Vector3.up * 1.1f, titre);
#endif
        }

        static void Fleche(Vector3 a, Vector3 b)
        {
            var d = b - a; d.y = 0;
            float l = d.magnitude;
            if (l < 0.01f) return;
            d /= l;
            var debut = a + d * 1.0f; var fin = b - d * 1.05f;
            Gizmos.DrawLine(debut, fin);
            var cote = new Vector3(-d.z, 0, d.x);
            Gizmos.DrawLine(fin, fin - d * 0.5f + cote * 0.3f);
            Gizmos.DrawLine(fin, fin - d * 0.5f - cote * 0.3f);
        }
    }
}

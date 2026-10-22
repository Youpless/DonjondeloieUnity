// Donjon de l'Oie — un décor ou un bâtiment posé à la main dans l'éditeur de cartes (tour, ruine, rocher, arbre…).
using UnityEngine;

namespace DonjonDeLOie.Cartes
{
    [DisallowMultipleComponent]
    public sealed class DDDecorMarqueur : MonoBehaviour
    {
        [Tooltip("Identifiant du décor (ex. tour_garde, maison, ruine, chene, rocher). Voir la liste dans l'inspecteur.")]
        public string Id = "maison";
        [Tooltip("Fait varier la forme d'un exemplaire à l'autre.")]
        public int Graine = 1;

        void OnDrawGizmos()
        {
            float r = DonjonDeLOie.Monde.IleModele.RayonLieu(Id);
            var p = DDCaseMarqueur.PositionDessin(transform.position);
            Gizmos.color = new Color(0.45f, 0.75f, 0.35f, 0.8f);
            Gizmos.DrawWireSphere(p + Vector3.up * r * 0.5f, r);
            Gizmos.DrawLine(p, p + transform.forward * (r + 0.6f));
#if UNITY_EDITOR
            UnityEditor.Handles.Label(p + Vector3.up * (r + 0.4f), Id);
#endif
        }
    }
}

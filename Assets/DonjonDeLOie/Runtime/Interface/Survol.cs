// Donjon de l'Oie — survol d'un élément d'interface : appelle une action à l'entrée et à la sortie du pointeur.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DonjonDeLOie.Interface
{
    public sealed class Survol : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action Entree, Sortie;
        public void OnPointerEnter(PointerEventData e) { if (Entree != null) Entree(); }
        public void OnPointerExit(PointerEventData e) { if (Sortie != null) Sortie(); }
    }
}

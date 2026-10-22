// Donjon de l'Oie — lecture de la souris et du clavier, avec le nouveau système d'entrées d'Unity 6 (par défaut)
// ou l'ancien gestionnaire, selon le réglage « Active Input Handling » du projet.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DonjonDeLOie.Pilote
{
    public enum KeyCodeDD { Gauche, Droite, Espace, Echap, Entree, F1, Tab }

    public static class DDInput
    {
        static int image = -1;
        static Vector2 dernierePos, delta;

        static void MettreAJour()
        {
            if (image == Time.frameCount) return;
            image = Time.frameCount;
            var p = PositionSouris;
            delta = p - dernierePos;
            dernierePos = p;
        }

        public static Vector2 PositionSouris
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }

        public static Vector2 DeplacementSouris
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
                MettreAJour(); return delta;
#endif
            }
        }

        /// Crans de molette depuis l'image précédente (positif : vers l'avant).
        public static float Molette
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current == null) return 0;
                float v = Mouse.current.scroll.ReadValue().y;
                return Mathf.Abs(v) > 5f ? v / 120f : v;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mouseScrollDelta.y;
#else
                return 0;
#endif
            }
        }

        public static bool BoutonDroitMaintenu
        {
            get
            {
                MettreAJour();
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.rightButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(1);
#else
                return false;
#endif
            }
        }

        public static bool BoutonMilieuMaintenu
        {
            get
            {
                MettreAJour();
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.middleButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(2);
#else
                return false;
#endif
            }
        }

        public static bool ClicGauche
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }

        public static bool Touche(KeyCodeDD k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            var c = Controle(kb, k);
            return c != null && c.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(Legacy(k));
#else
            return false;
#endif
        }

        public static bool ToucheAppuyee(KeyCodeDD k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            var c = Controle(kb, k);
            return c != null && c.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(Legacy(k));
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static UnityEngine.InputSystem.Controls.KeyControl Controle(Keyboard kb, KeyCodeDD k)
        {
            switch (k)
            {
                case KeyCodeDD.Gauche: return kb.leftArrowKey;
                case KeyCodeDD.Droite: return kb.rightArrowKey;
                case KeyCodeDD.Espace: return kb.spaceKey;
                case KeyCodeDD.Echap: return kb.escapeKey;
                case KeyCodeDD.Entree: return kb.enterKey;
                case KeyCodeDD.F1: return kb.f1Key;
                case KeyCodeDD.Tab: return kb.tabKey;
            }
            return null;
        }
#endif

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        static KeyCode Legacy(KeyCodeDD k)
        {
            switch (k)
            {
                case KeyCodeDD.Gauche: return KeyCode.LeftArrow;
                case KeyCodeDD.Droite: return KeyCode.RightArrow;
                case KeyCodeDD.Espace: return KeyCode.Space;
                case KeyCodeDD.Echap: return KeyCode.Escape;
                case KeyCodeDD.Entree: return KeyCode.Return;
                case KeyCodeDD.F1: return KeyCode.F1;
                default: return KeyCode.Tab;
            }
        }
#endif
    }
}

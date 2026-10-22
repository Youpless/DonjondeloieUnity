// Les Dés du Dragon — la caméra : suit le joueur actif en vue plongeante, cadre les combats, montre tout le plateau.
// Molette : zoom · clic droit glissé (ou flèches gauche / droite) : tourner · clic milieu glissé : déplacer · Espace : vue d'ensemble.
using UnityEngine;
using DesDuDragon.Pilote;

namespace DesDuDragon.Monde
{
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public Vector3 Cible;              // point regardé (lissé)
        public float Distance = 13f, Lacet = 0f, Tangage = 50f;
        Vector3 cibleVoulue; float distanceVoulue = 13f, lacetVoulu = 0f, tangageVoulu = 50f;
        Transform suivi; Vector3 decalageLibre;
        public bool VueEnsemble { get; private set; }
        public bool CommandesActives = true;
        float distanceAvantEnsemble = 13f;

        public static CameraRig Creer(Transform parent)
        {
            var go = new GameObject("Caméra");
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 600f;
            cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
            var r = go.AddComponent<CameraRig>();
            r.Cam = cam;
            return r;
        }

        public void Suivre(Transform t, float distance = 13f)
        {
            suivi = t; decalageLibre = Vector3.zero; VueEnsemble = false;
            distanceVoulue = distance; tangageVoulu = 50f;
        }

        public void Regarder(Vector3 p, float distance, float tangage = 50f)
        {
            suivi = null; decalageLibre = Vector3.zero; VueEnsemble = false;
            cibleVoulue = p; distanceVoulue = distance; tangageVoulu = tangage;
        }

        /// Cadre deux points (duel, combat contre un monstre), vue un peu plus rasante.
        public void Cadrer(Vector3 a, Vector3 b)
        {
            suivi = null; decalageLibre = Vector3.zero; VueEnsemble = false;
            cibleVoulue = (a + b) * 0.5f + Vector3.up * 0.6f;
            distanceVoulue = Mathf.Clamp(Vector3.Distance(a, b) * 1.6f + 5.5f, 6.5f, 12f);
            tangageVoulu = 32f;
            // tourner pour voir les deux de profil
            var d = b - a; d.y = 0;
            if (d.sqrMagnitude > 0.01f)
            {
                float cap = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg - 90f;
                // choisir le côté le plus proche du lacet actuel
                if (Mathf.Abs(Mathf.DeltaAngle(lacetVoulu, cap)) > 90f) cap += 180f;
                lacetVoulu = cap;
            }
        }

        /// Fait tourner lentement la caméra (écran titre).
        public void Orbiter(float degresParSeconde) { lacetVoulu += degresParSeconde * Time.unscaledDeltaTime; }

        public void BasculerEnsemble()
        {
            if (VueEnsemble) { VueEnsemble = false; distanceVoulue = distanceAvantEnsemble; tangageVoulu = 50f; }
            else { distanceAvantEnsemble = distanceVoulue; VueEnsemble = true; distanceVoulue = 62f; tangageVoulu = 56f; }
        }

        public void Placer()
        {
            Cible = cibleVoulue; Distance = distanceVoulue; Lacet = lacetVoulu; Tangage = tangageVoulu;
            Appliquer();
        }

        void LateUpdate()
        {
            if (CommandesActives)
            {
                float molette = DDInput.Molette;
                if (Mathf.Abs(molette) > 0.01f)
                {
                    distanceVoulue = Mathf.Clamp(distanceVoulue * (1f - molette * 0.12f), 5f, 75f);
                    if (VueEnsemble && distanceVoulue < 40f) VueEnsemble = false;
                }
                if (DDInput.BoutonDroitMaintenu) lacetVoulu += DDInput.DeplacementSouris.x * 0.25f;
                if (DDInput.Touche(KeyCodeDD.Gauche)) lacetVoulu += 70f * Time.unscaledDeltaTime;
                if (DDInput.Touche(KeyCodeDD.Droite)) lacetVoulu -= 70f * Time.unscaledDeltaTime;
                if (DDInput.BoutonMilieuMaintenu)
                {
                    var dm = DDInput.DeplacementSouris * (Distance * 0.0016f);
                    var q = Quaternion.Euler(0, Lacet, 0);
                    decalageLibre -= q * new Vector3(dm.x, 0, dm.y);
                }
                if (DDInput.ToucheAppuyee(KeyCodeDD.Espace)) BasculerEnsemble();
            }
            if (VueEnsemble) cibleVoulue = new Vector3(0, 0, -2f);
            else if (suivi != null) cibleVoulue = suivi.position + Vector3.up * 0.6f;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Cible = Vector3.Lerp(Cible, cibleVoulue + decalageLibre, 1f - Mathf.Exp(-dt * 4.5f));
            Distance = Mathf.Lerp(Distance, distanceVoulue, 1f - Mathf.Exp(-dt * 3.5f));
            Lacet = Mathf.LerpAngle(Lacet, lacetVoulu, 1f - Mathf.Exp(-dt * 4f));
            Tangage = Mathf.Lerp(Tangage, tangageVoulu, 1f - Mathf.Exp(-dt * 3f));
            Appliquer();
        }

        void Appliquer()
        {
            var q = Quaternion.Euler(Tangage, Lacet, 0);
            transform.position = Cible - q * Vector3.forward * Distance;
            transform.rotation = q;
        }

        /// Position écran d'un point du monde (pour les bulles et les étiquettes). z < 0 : derrière la caméra.
        public Vector3 Ecran(Vector3 p) { return Cam.WorldToScreenPoint(p); }
    }
}

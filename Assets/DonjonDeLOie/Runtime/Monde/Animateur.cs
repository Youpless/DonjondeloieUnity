// Donjon de l'Oie — animation procédurale des rigs voxel : respiration, marche, battements d'ailes, gestes (attaque,
// coup reçu, victoire, K.O.). Chaque pièce est retrouvée par son nom ; celles qui manquent sont ignorées.
using UnityEngine;

namespace DonjonDeLOie.Monde
{
    public enum Geste { Aucun, Attaque, Touche, Victoire, Ko, Joie, Sort, Lancer }

    public sealed class Animateur : MonoBehaviour
    {
        public RigVue Rig;
        public bool EnMarche;           // pendant un saut d'une case à l'autre
        public bool Flotte;             // spectre, feu follet : ondule au lieu de respirer
        public bool AilesActives;       // battement permanent (wyverne, dragon)
        public float Vitesse = 1f;
        public float Graine;

        Geste geste; float tGeste, dureeGeste; bool koTenu;
        float t, marche;
        float echelle = 1f;

        Transform corps, tete, brasG, brasD, jambeG, jambeD, arme, cape, machoire, aileG, aileD, queue, avG, avD, arG, arD, feuillage, bouclier;

        public static Animateur Sur(RigVue rv)
        {
            var a = rv.gameObject.AddComponent<Animateur>();
            a.Rig = rv;
            a.Graine = Random.value * 10f;
            a.Prendre();
            return a;
        }

        void Prendre()
        {
            corps = Rig.Piece("corps"); tete = Rig.Piece("tete"); brasG = Rig.Piece("brasG"); brasD = Rig.Piece("brasD");
            jambeG = Rig.Piece("jambeG"); jambeD = Rig.Piece("jambeD"); arme = Rig.Piece("arme"); cape = Rig.Piece("cape");
            machoire = Rig.Piece("machoire"); aileG = Rig.Piece("aileG"); aileD = Rig.Piece("aileD"); queue = Rig.Piece("queue");
            avG = Rig.Piece("pattesAvG"); avD = Rig.Piece("pattesAvD"); arG = Rig.Piece("pattesArG"); arD = Rig.Piece("pattesArD");
            feuillage = Rig.Piece("feuillage"); bouclier = Rig.Piece("bouclier");
            echelle = Mathf.Clamp(Rig.Hauteur / 1.3f, 0.4f, 3f);
            AilesActives = aileG != null && aileD != null;
        }

        public void Jouer(Geste g, float duree = -1f)
        {
            geste = g; tGeste = 0;
            koTenu = g == Geste.Ko;
            switch (g)
            {
                case Geste.Attaque: dureeGeste = 0.75f; break;
                case Geste.Touche: dureeGeste = 0.5f; break;
                case Geste.Victoire: dureeGeste = 1.6f; break;
                case Geste.Ko: dureeGeste = 0.6f; break;
                case Geste.Joie: dureeGeste = 0.9f; break;
                case Geste.Sort: dureeGeste = 0.9f; break;
                case Geste.Lancer: dureeGeste = 0.5f; break;
                default: dureeGeste = 0; break;
            }
            if (duree > 0) dureeGeste = duree;
        }

        public void Relever() { koTenu = false; geste = Geste.Aucun; }

        public bool EnGeste { get { return geste != Geste.Aucun && tGeste < dureeGeste; } }

        static Quaternion E(float x, float y, float z) { return Quaternion.Euler(x, y, z); }

        void Poser(Transform p, Quaternion rot, Vector3 decalage)
        {
            if (p == null) return;
            p.localRotation = Rig.Repos[p.name] * rot;
            p.localPosition = Rig.PositionsRepos[p.name] + decalage;
        }

        void LateUpdate()
        {
            if (Rig == null) return;
            float dt = Time.deltaTime * Vitesse;
            t += dt;
            if (EnMarche) marche += dt * 9f; else marche = Mathf.MoveTowards(marche, Mathf.Round(marche / Mathf.PI) * Mathf.PI, dt * 6f);
            if (geste != Geste.Aucun) { tGeste += dt; if (tGeste >= dureeGeste && !koTenu) geste = Geste.Aucun; }
            float g = dureeGeste > 0 ? Mathf.Clamp01(tGeste / dureeGeste) : 0;

            // ---- base : respiration et marche
            float souffle = Mathf.Sin(t * 2.1f + Graine);
            float pas = Mathf.Sin(marche);
            float amp = EnMarche ? 1f : 0f;
            Vector3 dCorps = new Vector3(0, (Flotte ? Mathf.Sin(t * 1.6f + Graine) * 0.06f : souffle * 0.008f) * echelle, 0);
            Quaternion rCorps = E(EnMarche ? 6f : souffle * 0.6f, 0, Flotte ? Mathf.Sin(t * 1.1f) * 3f : 0);
            Quaternion rTete = E(Mathf.Sin(t * 0.9f + Graine) * 3f, Mathf.Sin(t * 0.47f + Graine * 2f) * 10f, 0);
            Quaternion rBrasG = E(-pas * 28f * amp + souffle * 2.5f, 0, -3f - souffle);
            Quaternion rBrasD = E(pas * 28f * amp + souffle * 2.5f, 0, 3f + souffle);
            Quaternion rJambeG = E(pas * 32f * amp, 0, 0), rJambeD = E(-pas * 32f * amp, 0, 0);
            Quaternion rAv = E(pas * 26f * amp + souffle, 0, 0), rAr = E(-pas * 26f * amp - souffle, 0, 0);
            float battement = AilesActives ? (EnMarche ? 35f : 12f) * Mathf.Sin(t * (EnMarche ? 9f : 2.4f)) + (EnMarche ? 25f : 6f) : 0f;
            Quaternion rAileG = E(0, 0, -battement), rAileD = E(0, 0, battement);   // aile gauche (côté −x) : un angle négatif la lève
            Quaternion rQueue = E(0, Mathf.Sin(t * 1.4f + Graine) * 14f, 0);
            Quaternion rMach = E(Mathf.Max(0, Mathf.Sin(t * 0.8f + Graine)) * 6f, 0, 0);
            Quaternion rCape = E(8f + Mathf.Sin(t * 2.7f + Graine) * 5f + amp * 18f, 0, 0);
            Quaternion rArme = Quaternion.identity;
            Vector3 dCorpsGeste = Vector3.zero;

            // ---- gestes
            switch (geste)
            {
                case Geste.Attaque:
                {
                    // armer (0–40 %), frapper (40–60 %), revenir
                    float arm = Mathf.SmoothStep(0, 1, g / 0.4f), frappe = Mathf.Clamp01((g - 0.4f) / 0.2f), retour = Mathf.Clamp01((g - 0.6f) / 0.4f);
                    float bras = Mathf.Lerp(Mathf.Lerp(0, -150f, arm), 30f, frappe);
                    bras = Mathf.Lerp(bras, 0, retour);
                    rBrasD = E(bras, 0, 6f);
                    rBrasG = E(Mathf.Lerp(0, -40f, arm) * (1 - retour), 0, -6f);
                    float avance = (frappe - retour) * 0.28f * echelle;
                    dCorpsGeste = new Vector3(0, 0, avance);
                    rCorps = E(Mathf.Lerp(-8f * arm, 14f, frappe) * (1 - retour), Mathf.Lerp(-12f * arm, 10f, frappe) * (1 - retour), 0);
                    rMach = E(Mathf.Lerp(0, 38f, Mathf.Max(arm * 0.6f, frappe)) * (1 - retour), 0, 0);
                    rTete = rTete * E(Mathf.Lerp(-10f * arm, 18f, frappe) * (1 - retour), 0, 0);
                    rAv = E(-30f * frappe * (1 - retour), 0, 0);
                    if (AilesActives) { rAileG = E(0, 0, -55f * arm * (1 - retour)); rAileD = E(0, 0, 55f * arm * (1 - retour)); }
                    break;
                }
                case Geste.Touche:
                {
                    float k = Mathf.Sin(g * Mathf.PI);
                    dCorpsGeste = new Vector3(0, 0, -0.18f * echelle * k);
                    rCorps = E(-16f * k, 0, 6f * k);
                    rTete = rTete * E(-14f * k, 0, 0);
                    rBrasG = E(-30f * k, 0, -20f * k); rBrasD = E(-30f * k, 0, 20f * k);
                    break;
                }
                case Geste.Victoire:
                {
                    float saut = Mathf.Abs(Mathf.Sin(g * Mathf.PI * 2f));
                    dCorpsGeste = new Vector3(0, saut * 0.22f * echelle, 0);
                    rBrasD = E(-165f, 0, 12f); rBrasG = E(-165f * Mathf.Min(1, g * 3f), 0, -12f);
                    rCorps = E(-6f, Mathf.Sin(g * Mathf.PI * 4f) * 8f, 0);
                    if (AilesActives) { rAileG = E(0, 0, -70f); rAileD = E(0, 0, 70f); }
                    break;
                }
                case Geste.Joie:
                {
                    float saut = Mathf.Abs(Mathf.Sin(g * Mathf.PI * 2f));
                    dCorpsGeste = new Vector3(0, saut * 0.15f * echelle, 0);
                    rBrasD = E(-120f * saut, 0, 10f); rBrasG = E(-120f * saut, 0, -10f);
                    break;
                }
                case Geste.Sort:
                {
                    float k = Mathf.Sin(Mathf.Clamp01(g * 1.2f) * Mathf.PI);
                    rBrasD = E(-100f * k, -10f * k, 0); rBrasG = E(-100f * k, 10f * k, 0);
                    rCorps = E(-6f * k, 0, 0);
                    dCorpsGeste = new Vector3(0, 0.06f * k * echelle, 0);
                    break;
                }
                case Geste.Lancer:
                {
                    float k = Mathf.Sin(g * Mathf.PI);
                    rBrasD = E(-70f * k, 0, 10f);
                    rCorps = E(8f * k, 0, 0);
                    break;
                }
                case Geste.Ko:
                {
                    float k = Mathf.SmoothStep(0, 1, g);
                    rCorps = E(-82f * k, 0, 10f * k);
                    dCorpsGeste = new Vector3(0, -0.25f * echelle * k * (Rig.Hauteur > 0 ? 1 : 0), -0.1f * k);
                    rBrasD = E(-60f * k, 0, 40f * k); rBrasG = E(-60f * k, 0, -40f * k);
                    rTete = E(-20f * k, 25f * k, 0);
                    break;
                }
            }

            Poser(corps, rCorps, dCorps + dCorpsGeste);
            Poser(tete, rTete, Vector3.zero);
            Poser(brasG, rBrasG, Vector3.zero);
            Poser(brasD, rBrasD, Vector3.zero);
            Poser(jambeG, rJambeG, Vector3.zero);
            Poser(jambeD, rJambeD, Vector3.zero);
            Poser(avG, rAv, Vector3.zero); Poser(avD, rAr, Vector3.zero);
            Poser(arG, rAr, Vector3.zero); Poser(arD, rAv, Vector3.zero);
            Poser(aileG, rAileG, Vector3.zero); Poser(aileD, rAileD, Vector3.zero);
            Poser(queue, rQueue, Vector3.zero);
            Poser(machoire, rMach, Vector3.zero);
            Poser(cape, rCape, Vector3.zero);
            Poser(arme, rArme, Vector3.zero);
            Poser(bouclier, Quaternion.identity, Vector3.zero);
            if (feuillage != null)
                Poser(feuillage, E(Mathf.Sin(t * 0.9f + Graine) * 1.6f, 0, Mathf.Sin(t * 0.7f + Graine * 1.7f) * 2.2f), Vector3.zero);
        }
    }
}

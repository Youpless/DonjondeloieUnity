// Donjon de l'Oie — la vie du marché en diorama : il surgit pièce après pièce, les passants vont et viennent, les
// marchands haranguent, l'évadé guette en tournant la tête ; un clic sur une tête dit qui est là. À la fin, l'évadé
// est démasqué (il bondit hors de sa cachette) ou s'enfuit en courant hors du diorama, qui se replie.
using System.Collections;
using UnityEngine;

namespace DonjonDeLOie.Monde
{
    public sealed partial class Diorama
    {
        const float DureePiece = 0.42f;
        bool vivant, fini;
        float temps, prochainGeste = 1f;

        static Vector3 PositionPassant(Vector3 a, Vector3 b, float phase) { return Vector3.Lerp(a, b, Mathf.PingPong(phase, 1f)); }

        // ------------------------------------------------------------------ apparaître, disparaître

        /// Le diorama surgit : le socle bondit, les maisons et les étals poussent, les habitants tombent du ciel.
        /// « pop » est appelé au surgissement de chaque pièce (sa position dans le monde), pour un son ou un nuage ;
        /// « vitesse » accélère le tout (les bots rapides).
        public IEnumerator Apparaitre(System.Action<Vector3> pop, float vitesse = 1f)
        {
            float fin = 0;
            foreach (var p in pieces) fin = Mathf.Max(fin, p.Delai + DureePiece);
            var vus = new bool[pieces.Count];
            float t = 0;
            while (t < fin)
            {
                t += Time.deltaTime * vitesse;
                for (int i = 0; i < pieces.Count; i++)
                {
                    var p = pieces[i];
                    if (p.T == null || t < p.Delai) continue;
                    if (!vus[i]) { vus[i] = true; p.T.gameObject.SetActive(true); if (pop != null) pop(p.T.position); }
                    Poser(p, Mathf.Clamp01((t - p.Delai) / DureePiece));
                }
                yield return null;
            }
            Montrer();
        }

        /// Tout en place d'un coup (essais hors Unity, ou partie qui se termine pendant l'animation).
        public void Montrer()
        {
            foreach (var p in pieces) if (p.T != null) { p.T.gameObject.SetActive(true); Poser(p, 1f); }
            vivant = true;
        }

        static float Rebond(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        static void Poser(Piece p, float k)
        {
            switch (p.Genre)
            {
                case 1:
                    p.T.localScale = new Vector3(p.Echelle.x, p.Echelle.y * Mathf.Max(0.01f, Rebond(k)), p.Echelle.z);
                    break;
                case 2:
                    float h = 1f - k;
                    p.T.localPosition = p.Position + Vector3.up * (1.4f * h * h);
                    p.T.localScale = p.Echelle * Mathf.Lerp(0.5f, 1f, Mathf.Min(1f, k * 2f));
                    break;
                default:
                    p.T.localScale = p.Echelle * Mathf.Max(0.01f, Rebond(k));
                    break;
            }
        }

        /// Le diorama se replie sur lui-même en tournant, puis s'éteint.
        public IEnumerator Disparaitre(float duree)
        {
            vivant = false;
            var e0 = transform.localScale;
            var r0 = transform.rotation;
            float t = 0;
            while (t < duree)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duree);
                transform.localScale = e0 * Mathf.Max(0.001f, 1f - k * k);
                transform.rotation = r0 * Quaternion.Euler(0, 50f * k * k, 0);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ la vie du marché

        void Update()
        {
            if (!vivant) return;
            float dt = Time.deltaTime;
            temps += dt;
            foreach (var h in Foule)
            {
                if (h.Rig == null) continue;
                var tr = h.Rig.transform;
                if (h.Marche)
                {
                    h.Phase += dt * h.Vitesse / Mathf.Max(0.5f, Vector3.Distance(h.A, h.B));
                    tr.localPosition = PositionPassant(h.A, h.B, h.Phase);
                    float cap = Mathf.Repeat(h.Phase, 2f) < 1f ? 90f : -90f;
                    tr.localRotation = Quaternion.Slerp(tr.localRotation, Quaternion.Euler(0, cap, 0), Mathf.Min(1f, dt * 5f));
                    h.Anim.EnMarche = true;
                }
                else if (h.Cache && !fini)
                    tr.localRotation = Quaternion.Euler(0, h.Lacet + Mathf.Sin(temps * 0.8f + h.Anim.Graine) * 12f, 0);   // il guette
            }
            prochainGeste -= dt;
            if (prochainGeste <= 0f && Foule.Count > 0)
            {
                prochainGeste = 0.7f + 2f * (float)r.NextDouble();
                var h = Foule[r.Next(Foule.Count)];
                if (!h.Cache && !h.Marche && h.Anim != null && !h.Anim.EnGeste)
                {
                    bool marchand = h.Rig.transform.localPosition.z < EtalsZ;
                    h.Anim.Jouer(marchand ? (r.Next(2) == 0 ? Geste.Sort : Geste.Lancer) : (r.Next(3) == 0 ? Geste.Joie : Geste.Lancer), 1.1f);
                }
            }
        }

        // ------------------------------------------------------------------ le clic

        /// L'habitant dont la tête est sous ce point de l'écran (le plus proche de la caméra), ou null.
        public Habitant Toucher(Vector2 souris, Camera cam)
        {
            Habitant best = null; float bz = float.MaxValue;
            foreach (var h in Foule)
            {
                if (h.Rig == null || !h.Rig.gameObject.activeInHierarchy) continue;
                var c = h.Tete;
                var e = cam.WorldToScreenPoint(c);
                if (e.z <= 0) continue;
                var e2 = cam.WorldToScreenPoint(c + cam.transform.right * (0.24f * transform.lossyScale.x));
                float rayon = Mathf.Max(10f, Mathf.Abs(e2.x - e.x));
                if ((new Vector2(e.x, e.y) - souris).sqrMagnitude > rayon * rayon) continue;
                if (e.z < bz) { bz = e.z; best = h; }
            }
            return best;
        }

        /// Un habitant au hasard dont on voit le visage (la loupe d'un bot qui cherche), sauf l'évadé.
        public Habitant Badaud()
        {
            for (int k = 0; k < 20; k++)
            {
                var h = Foule[r.Next(Foule.Count)];
                if (!h.Evade && !h.Marche && Vector3.Angle(h.Rig.transform.forward, transform.rotation * VersCamera) < 80f) return h;
            }
            return Foule[0] == Evade && Foule.Count > 1 ? Foule[1] : Foule[0];
        }

        /// Un sosie de l'évadé (ou n'importe qui), pour l'erreur d'un bot.
        public Habitant UnSosie()
        {
            foreach (var h in Foule) if (h.Sosie) return h;
            return Badaud();
        }

        // ------------------------------------------------------------------ la fin

        /// Démasqué : l'évadé bondit par-dessus sa cachette et atterrit devant, penaud.
        public IEnumerator Attrape()
        {
            fini = true;
            var h = Evade;
            var tr = h.Rig.transform;
            var depart = tr.localPosition;
            var arrivee = depart + VersCamera * (Recul + 2f * DemiAbris[h.Cachette] + 0.42f);
            var r0 = tr.localRotation;
            h.Anim.Jouer(Geste.Touche, 0.5f);
            float t = 0;
            while (t < 0.75f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.75f);
                tr.localPosition = Vector3.Lerp(depart, arrivee, k) + Vector3.up * (1.15f * Mathf.Sin(k * Mathf.PI));
                tr.localRotation = r0 * Quaternion.Euler(0, 360f * Mathf.SmoothStep(0, 1, k), 0);
                yield return null;
            }
            tr.localPosition = arrivee;
            tr.localRotation = Quaternion.Euler(0, LacetVue, 0);
            h.Anim.Jouer(Geste.Ko);
        }

        /// Raté : l'évadé saute dans la rue, file vers le bord le plus proche et saute dans le vide.
        public IEnumerator Fuite()
        {
            fini = true;
            var h = Evade;
            var tr = h.Rig.transform;
            var depart = tr.localPosition;
            var rue = new Vector3(depart.x, 0, 1.15f);
            float cote = depart.x < 0 ? -1f : 1f;
            float t = 0;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.55f);
                tr.localPosition = Vector3.Lerp(depart, rue, k) + Vector3.up * (1.05f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            tr.localRotation = Quaternion.Euler(0, cote * 90f, 0);
            h.Anim.EnMarche = true;
            h.Anim.Vitesse = 2.4f;
            var pos = rue;
            while (Mathf.Abs(pos.x) < 5.4f)
            {
                pos.x += cote * 2.6f * Time.deltaTime;
                tr.localPosition = pos;
                yield return null;
            }
            // il saute dans le vide et rapetisse
            var e0 = tr.localScale;
            t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.5f);
                tr.localPosition = pos + new Vector3(cote * 0.8f * k, 0.5f * Mathf.Sin(k * Mathf.PI) - 2.2f * k * k, 0);
                tr.localScale = e0 * (1f - k);
                yield return null;
            }
            h.Rig.gameObject.SetActive(false);
        }
    }
}

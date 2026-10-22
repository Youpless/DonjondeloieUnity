// Donjon de l'Oie — sons et musique synthétisés au lancement (aucun fichier audio à importer).
// Bruitages : dé, pas, or, coup, niveau, malus, piège, sort, clic, victoire, monstre. Musique : une boucle au luth
// (cordes pincées, algorithme de Karplus-Strong) en ré dorien, sur un bourdon doux.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeLOie.Monde
{
    public enum Son { De, Pas, Or, Coup, Niveau, Malus, Piege, Sort, Clic, Victoire, Monstre, Soin, Page }

    public sealed class Sons : MonoBehaviour
    {
        const int F = 44100;
        static Sons instance;
        AudioSource effets, musique;
        readonly Dictionary<Son, AudioClip> clips = new Dictionary<Son, AudioClip>();
        public static float VolumeEffets = 0.8f, VolumeMusique = 0.35f;
        System.Random r = new System.Random(5);

        public static Sons Installer(GameObject hote)
        {
            instance = hote.AddComponent<Sons>();
            instance.effets = hote.AddComponent<AudioSource>();
            instance.effets.playOnAwake = false;
            instance.musique = hote.AddComponent<AudioSource>();
            instance.musique.loop = true;
            instance.musique.playOnAwake = false;
            instance.Generer();
            return instance;
        }

        public static void Jouer(Son s, float volume = 1f)
        {
            if (instance == null) return;
            AudioClip c;
            if (instance.clips.TryGetValue(s, out c) && c != null)
            {
                instance.effets.pitch = 1f;
                instance.effets.PlayOneShot(c, volume * VolumeEffets);
            }
        }

        public static void Musique(bool oui)
        {
            if (instance == null || instance.musique.clip == null) return;
            instance.musique.volume = VolumeMusique;
            if (oui && !instance.musique.isPlaying) instance.musique.Play();
            if (!oui) instance.musique.Stop();
        }

        public static void ReglerVolumes(float effets, float musique)
        {
            VolumeEffets = effets; VolumeMusique = musique;
            if (instance != null) instance.musique.volume = musique;
        }

        static AudioClip Clip(string nom, float[] d)
        {
            var c = AudioClip.Create(nom, d.Length, 1, F, false);
            c.SetData(d, 0);
            return c;
        }

        float Bruit() { return (float)(r.NextDouble() * 2 - 1); }

        static float Env(float t, float att, float dec) { return t < att ? t / att : Mathf.Exp(-(t - att) / dec); }

        float[] Tonalite(float duree, Func<float, float> f)
        {
            var d = new float[(int)(duree * F)];
            for (int i = 0; i < d.Length; i++) d[i] = f(i / (float)F);
            return d;
        }

        static float Sin(float hz, float t) { return Mathf.Sin(2f * Mathf.PI * hz * t); }

        float[] Corde(float hz, float duree, float amort = 0.996f, float brillance = 0.5f)
        {
            int n = (int)(duree * F);
            var d = new float[n];
            int p = Mathf.Max(2, (int)(F / hz));
            var buf = new float[p];
            for (int i = 0; i < p; i++) buf[i] = Bruit() * (1f - brillance * 0.5f);
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                float a = buf[k], b = buf[(k + 1) % p];
                float v = (a + b) * 0.5f * amort;
                buf[k] = v;
                d[i] = a;
                k = (k + 1) % p;
            }
            return d;
        }

        void Generer()
        {
            // Dé : cliquetis de petits chocs
            var de = new float[(int)(0.55f * F)];
            for (int k = 0; k < 7; k++)
            {
                int debut = (int)((0.03f + k * 0.065f + (float)r.NextDouble() * 0.02f) * F);
                float hz = 900 + (float)r.NextDouble() * 900, amp = 0.5f * (1f - k / 8f);
                for (int i = 0; i < 1600 && debut + i < de.Length; i++)
                {
                    float t = i / (float)F;
                    de[debut + i] += (Sin(hz, t) * 0.6f + Bruit() * 0.4f) * amp * Mathf.Exp(-t * 90f);
                }
            }
            clips[Son.De] = Clip("de", de);
            clips[Son.Pas] = Clip("pas", Tonalite(0.12f, t => (Sin(95, t) * 0.7f + Bruit() * 0.3f) * Env(t, 0.004f, 0.03f) * 0.55f));
            clips[Son.Or] = Clip("or", Tonalite(0.5f, t => (Sin(1568, t) * Env(t, 0.002f, 0.12f) + Sin(2093, Mathf.Max(0, t - 0.07f)) * (t > 0.07f ? Env(t - 0.07f, 0.002f, 0.18f) : 0)) * 0.32f));
            clips[Son.Coup] = Clip("coup", Tonalite(0.32f, t => (Bruit() * Env(t, 0.002f, 0.05f) * 0.8f + Sin(70 + 60 * Mathf.Exp(-t * 20), t) * Env(t, 0.003f, 0.09f)) * 0.7f));
            clips[Son.Niveau] = Clip("niveau", Arpege(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f, 0.9f, 0.3f));
            clips[Son.Victoire] = Clip("victoire", Arpege(new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f }, 0.14f, 1.8f, 0.3f));
            clips[Son.Malus] = Clip("malus", Arpege(new[] { 392f, 349.23f, 311.13f, 233.08f }, 0.12f, 0.8f, 0.26f));
            clips[Son.Piege] = Clip("piege", Tonalite(0.4f, t => (Bruit() * Env(t, 0.001f, 0.02f) + Sin(220, t) * Sin(7, t) * Env(t, 0.002f, 0.12f) * 0.6f + Sin(1200, t) * Env(t, 0.001f, 0.03f) * 0.5f) * 0.6f));
            clips[Son.Sort] = Clip("sort", Tonalite(0.9f, t => (Sin(600 + 900 * t, t) * 0.25f + Sin(1200 + 1600 * t, t) * 0.15f + Bruit() * 0.15f) * Env(t, 0.15f, 0.25f) * 0.5f));
            clips[Son.Soin] = Clip("soin", Arpege(new[] { 659.25f, 880f, 1318.5f }, 0.1f, 0.8f, 0.2f));
            clips[Son.Clic] = Clip("clic", Tonalite(0.05f, t => Sin(1400, t) * Env(t, 0.001f, 0.008f) * 0.35f));
            clips[Son.Page] = Clip("page", Tonalite(0.25f, t => Bruit() * Env(t, 0.03f, 0.06f) * 0.25f));
            // Monstre : grognement (bruit filtré modulé, grave)
            var gr = new float[(int)(0.8f * F)];
            float lp = 0;
            for (int i = 0; i < gr.Length; i++)
            {
                float t = i / (float)F;
                lp += (Bruit() - lp) * 0.04f;
                gr[i] = (lp * 3f * (0.6f + 0.4f * Sin(28, t)) + Sin(55 + 15 * Sin(3, t), t) * 0.4f) * Env(t, 0.06f, 0.3f) * 0.6f;
            }
            clips[Son.Monstre] = Clip("monstre", gr);
            musique.clip = Clip("musique", Luth());
        }

        float[] Arpege(float[] notes, float ecart, float duree, float vol)
        {
            var d = new float[(int)(duree * F)];
            for (int k = 0; k < notes.Length; k++)
            {
                int debut = (int)(k * ecart * F);
                var c = Corde(notes[k], duree - k * ecart, 0.997f, 0.3f);
                for (int i = 0; i < c.Length && debut + i < d.Length; i++)
                {
                    float t = i / (float)F;
                    d[debut + i] += (c[i] * 0.6f + Sin(notes[k], t) * 0.4f * Mathf.Exp(-t * 4f)) * vol;
                }
            }
            return d;
        }

        /// Boucle de 32 mesures à 3/4, ~84 battements par minute, ré dorien : basse, arpèges, mélodie simple.
        float[] Luth()
        {
            const float bpm = 84f;
            float temps = 60f / bpm;
            int mesures = 16;
            float duree = mesures * 3 * temps;
            var d = new float[(int)(duree * F)];
            // degrés de ré dorien
            float[] gamme = { 146.83f, 164.81f, 174.61f, 196.00f, 220.00f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 493.88f, 523.25f, 587.33f };
            int[][] accords = { new[] { 0, 2, 4 }, new[] { 6, 1, 3 }, new[] { 3, 5, 0 }, new[] { 4, 6, 1 } };   // Rém, Do, Sol, Lam
            int[] suite = { 0, 0, 1, 1, 2, 2, 3, 0, 0, 0, 1, 1, 2, 3, 0, 0 };
            var rm = new System.Random(11);
            for (int m = 0; m < mesures; m++)
            {
                var ac = accords[suite[m]];
                float t0 = m * 3 * temps;
                // basse au premier temps
                Ajouter(d, Corde(gamme[ac[0]] / 2f, 3 * temps, 0.998f, 0.2f), t0, 0.33f);
                // arpège en croches
                for (int k = 0; k < 6; k++)
                {
                    int deg = ac[k % 3] + (k >= 3 ? 7 : 0);
                    deg = Mathf.Min(deg, gamme.Length - 1);
                    Ajouter(d, Corde(gamme[deg], 1.2f, 0.995f, 0.4f), t0 + k * temps * 0.5f, 0.14f);
                }
                // mélodie : une note par temps, sur l'accord ou en passage
                if (m % 4 != 3)
                    for (int b = 0; b < 3; b++)
                    {
                        if (rm.NextDouble() < 0.25) continue;
                        int deg = ac[rm.Next(3)] + 7 + (rm.NextDouble() < 0.3 ? 1 : 0);
                        deg = Mathf.Min(deg, gamme.Length - 1);
                        Ajouter(d, Corde(gamme[deg] * 2f, 1.0f, 0.996f, 0.6f), t0 + b * temps + 0.01f, 0.1f);
                    }
            }
            // bourdon très doux et fondu pour une boucle sans clic
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)F;
                d[i] += (Sin(73.42f, t) + Sin(110f, t) * 0.5f) * 0.035f;
                float bord = Mathf.Min(1f, Mathf.Min(t, duree - t) / 0.05f);
                d[i] = Mathf.Clamp(d[i] * bord, -0.95f, 0.95f);
            }
            return d;
        }

        static void Ajouter(float[] d, float[] s, float t0, float vol)
        {
            int debut = (int)(t0 * F);
            for (int i = 0; i < s.Length && debut + i < d.Length; i++) d[debut + i] += s[i] * vol;
        }
    }
}

// Donjon de l'Oie — roches : rocher (teinte selon la zone), rocher moussu, pic rocheux, stalagmite,
// coulée de lave, cristal de braise.
using System;
using UnityEngine;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static partial class DecorsModeles
    {
        static readonly Color32 NaLaveClaire = Pal.C("ffb347"), NaLave = Pal.C("ff6a1e"), NaLaveSombre = Pal.C("c23a14");
        static readonly Color32 NaBasalte = Pal.C("3a3236"), NaBasalteClair = Pal.C("554a4c"), NaRocheRouge = Pal.C("6e4438");

        /// Jeux de tons de pierre : 0 gris chaud (prés, bois), 1 gris bleuté (cryptes), 2 volcanique (pics), 3 roche d'antre.
        static Color32[] NaTonsPierre(int famille, System.Random r)
        {
            Color32 a, b, c, d;
            switch (famille)
            {
                case 1: a = Pal.C("7a8088"); b = Pal.C("8e949b"); c = Pal.C("666c75"); d = Pal.C("a2a6aa"); break;
                case 2: a = Pal.C("5a4744"); b = Pal.C("6e544c"); c = Pal.C("463a3a"); d = Pal.C("806458"); break;
                case 3: a = Pal.C("5a5058"); b = Pal.C("6c6068"); c = Pal.C("483f48"); d = Pal.C("82746e"); break;
                default: a = Pal.C("8a867e"); b = Pal.C("9a958b"); c = Pal.C("7a766f"); d = Pal.C("a8a397"); break;
            }
            float f = NaF(r, -0.06f, 0.06f);
            return new[] { Pal.Shade(a, 1 + f), Pal.Shade(b, 1 + f), Pal.Shade(c, 1 + f), Pal.Shade(d, 1 + f), Pal.Shade(a, 0.92f + f) };
        }

        // ------------------------------------------------------------------ rocher (0,6 – 1,8 m)

        /// La graine choisit aussi la famille de teinte (le même id sert dans plusieurs zones) : on garde une pierre
        /// chaude pour la plupart, et parfois une variante plus sombre. Le rocher moussu est celui du marais.
        static VoxelRig Rocher(System.Random r, bool mousse)
        {
            var rig = NaRig(mousse ? "rocher_mousse" : "rocher", 0.1f);
            var m = new VoxelModel();
            int famille = 0;
            var tons = NaTonsPierre(famille, r);
            float taille = NaF(r, 0.6f, 1.7f) * 10f;           // hauteur visée en voxels
            float large = taille * NaF(r, 0.55f, 0.8f);
            var strate = Pal.Shade(tons[0], 0.78f);
            // bloc principal posé à moitié enfoncé, et un ou deux éclats
            NaRoche(m, r, new Vector3(0, taille * 0.35f, 0), new Vector3(large, taille * 0.72f, large * NaF(r, 0.65f, 0.95f)), NaI(r, 6, 8), tons, NaF(r, 3f, 4.5f), strate);
            int n = NaI(r, 1, 2);
            for (int i = 0; i < n; i++)
            {
                float a = NaF(r, 0, 6.28f), s = NaF(r, 0.35f, 0.55f);
                NaRoche(m, r, new Vector3(Mathf.Cos(a) * large * 0.95f, taille * 0.15f, Mathf.Sin(a) * large * 0.9f),
                    new Vector3(large * s, taille * s * 0.8f, large * s), NaI(r, 6, 8), tons, 0, strate);
            }
            // cailloux au pied
            for (int i = 0; i < 3; i++)
            {
                float a = NaF(r, 0, 6.28f); int x = (int)(Mathf.Cos(a) * (large + 2)), z = (int)(Mathf.Sin(a) * (large + 2));
                m.Box(x, 0, z, x + NaI(r, 0, 1), 0, z + NaI(r, 0, 1), tons[2]);
            }
            NaSol(m);
            if (mousse)
            {
                var vm = NaTeinte(Pal.C("5f7234"), r, 0.06f); var vm2 = Pal.Mix(vm, Pal.C("8a8f48"), 0.4f);
                m.Paint((x, y, z, c) =>
                {
                    bool dessus = !m.Has(x, y + 1, z);
                    float h = Pal.Hash01(x / 3, y / 4, z / 3), h2 = Pal.Hash01(x, y, z);
                    if (dessus && y > 2 && h < 0.75f) return h2 < 0.25f ? vm2 : vm;
                    if (!dessus && !m.Has(x, y + 2, z) && y > 3 && h < 0.45f) return vm;   // la mousse déborde sur les bords
                    if (y <= 1 && h < 0.5f) return Pal.Mix(c, Pal.C("4a4a2c"), 0.6f);
                    return c;
                });
                // quelques brins
                foreach (var kv in m.Clone().Tous())
                {
                    var p = kv.Key;
                    if (!m.Has(p.x, p.y + 1, p.z) && Pal.Hash01(p.x, p.y, p.z) < 0.05f) m.Set(p.x, p.y + 1, p.z, vm2);
                }
            }
            else NaDessusDessous(m, 1.06f, 0.85f);

            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ pic rocheux des Pics de Braise (3 – 6 m)

        static VoxelRig PicRocheux(System.Random r)
        {
            var rig = NaRig("pic_rocheux", 0.1f);
            var m = new VoxelModel();
            var tons = NaTonsPierre(2, r);
            var tonsSombres = new[] { tons[2], Pal.Shade(tons[2], 0.85f), tons[0], NaBasalteClair };
            float H = NaF(r, 30, 52), pied = Mathf.Min(5.8f, H * NaF(r, 0.12f, 0.14f));
            float a = NaF(r, 0, 6.28f), inc = NaF(r, 0.05f, 0.2f);
            var sommet = new Vector3(Mathf.Cos(a) * H * inc, H, Mathf.Sin(a) * H * inc);
            NaPrisme(m, new Vector3(0, -2, 0), sommet, pied, 1.2f, NaI(r, 5, 7), NaF(r, 0, 3), new[] { tons[0], tons[1], tons[2], tons[3], tons[4] });
            // aiguilles secondaires
            int n = NaI(r, 1, 3);
            for (int i = 0; i < n; i++)
            {
                float b = NaF(r, 0, 6.28f), d = pied * NaF(r, 0.8f, 1.1f), h = H * NaF(r, 0.25f, 0.5f);
                var base0 = new Vector3(Mathf.Cos(b) * d, -2, Mathf.Sin(b) * d);
                var pointe = base0 + new Vector3(Mathf.Cos(b) * h * 0.18f, h, Mathf.Sin(b) * h * 0.18f);
                NaPrisme(m, base0, pointe, pied * NaF(r, 0.4f, 0.6f), 0.8f, NaI(r, 4, 6), NaF(r, 0, 3), tonsSombres);
            }
            // éboulis au pied
            for (int i = 0; i < 4; i++)
            {
                float b = NaF(r, 0, 6.28f), d = pied * NaF(r, 1.1f, 1.5f);
                NaRoche(m, r, new Vector3(Mathf.Cos(b) * d, 1, Mathf.Sin(b) * d), new Vector3(3.2f, 2.6f, 3.2f), 6, tonsSombres, 0, tons[2]);
            }
            // strates inclinées et cendre sur les replats
            float sx = NaF(r, -0.2f, 0.2f), sz = NaF(r, -0.2f, 0.2f);
            var cendre = Pal.C("8c8580");
            m.Paint((x, y, z, c) =>
            {
                float s = (y + sx * x + sz * z) / 5f;
                if (s - (float)Math.Floor(s) < 0.2f) c = Pal.Mix(c, NaRocheRouge, 0.55f);
                if (!m.Has(x, y + 1, z) && y < 8) c = Pal.Mix(c, cendre, 0.5f);
                return c;
            });
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ stalagmite de l'antre (1,5 – 3 m)

        static VoxelRig Stalagmite(System.Random r)
        {
            var rig = NaRig("stalagmite", 0.1f);
            var m = new VoxelModel();
            var tons = NaTonsPierre(3, r);
            float H = NaF(r, 15, 30), pied = H * NaF(r, 0.15f, 0.2f);
            float a = NaF(r, 0, 6.28f), inc = NaF(r, 0.0f, 0.12f);
            var sommet = new Vector3(Mathf.Cos(a) * H * inc, H, Mathf.Sin(a) * H * inc);
            // en anneaux empilés : chaque tranche un peu décalée, comme du calcaire qui a coulé
            int nAnneaux = NaI(r, 4, 6);
            for (int i = 0; i < nAnneaux; i++)
            {
                float t0 = i / (float)nAnneaux, t1 = (i + 1) / (float)nAnneaux;
                var p0 = Vector3.Lerp(new Vector3(0, -1, 0), sommet, t0); var p1 = Vector3.Lerp(new Vector3(0, -1, 0), sommet, t1 + 0.04f);
                float r0 = pied * (1 - t0 * 0.9f) * NaF(r, 0.95f, 1.1f), r1 = pied * (1 - t1 * 0.9f) * 0.88f + 0.6f;
                NaPrisme(m, p0, p1, r0, r1, 6, i * 0.5f, new[] { tons[i % 4], tons[(i + 1) % 4], tons[(i + 2) % 4] });
            }
            // petite sœur
            if (NaP(r, 0.75f))
            {
                float b = a + NaF(r, 2, 4), d = pied * 1.3f, h = H * NaF(r, 0.3f, 0.5f);
                NaPrisme(m, new Vector3(Mathf.Cos(b) * d, -1, Mathf.Sin(b) * d), new Vector3(Mathf.Cos(b) * d, h, Mathf.Sin(b) * d), pied * 0.5f, 0.6f, 5, 0, new[] { tons[1], tons[2] });
            }
            // la pointe est plus claire (calcaire mouillé), le pied plus sombre
            m.Paint((x, y, z, c) => y > H * 0.8f ? Pal.Mix(c, tons[3], 0.5f) : (y < 3 ? Pal.Shade(c, 0.85f) : c));
            NaSol(m);
            rig.Add("corps", m, Vector3.zero);
            return rig;
        }

        // ------------------------------------------------------------------ coulée de lave (voxel de 0,1 m)

        static VoxelRig Lave(System.Random r)
        {
            var rig = NaRig("lave", 0.1f);
            var croute = new VoxelModel(); var feu = new VoxelModel();
            float R = NaF(r, 5.5f, 8), allonge = NaF(r, 1.0f, 1.5f), rot = NaF(r, 0, 3.14f);
            float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            int ph = r.Next(1000);
            for (int x = -16; x <= 16; x++) for (int z = -16; z <= 16; z++)
            {
                float u = (x * cs + z * sn) / (R * allonge), v = (-x * sn + z * cs) / R;
                float bord = 1f + (Pal.Hash01(x / 2 + ph, 0, z / 2) - 0.5f) * 0.35f;
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > bord) continue;
                // plaques de croûte noire séparées par des fissures (réseau de Voronoï grossier)
                float best = 9, second = 9;
                for (int k = 0; k < 9; k++)
                {
                    float kx = (Pal.Hash01(k, ph, 1) - 0.5f) * 2.2f * R * allonge, kz = (Pal.Hash01(k, ph, 2) - 0.5f) * 2.2f * R;
                    float e = Mathf.Sqrt((x - kx) * (x - kx) + (z - kz) * (z - kz));
                    if (e < best) { second = best; best = e; } else if (e < second) second = e;
                }
                bool fissure = second - best < 0.9f || (d > bord - 0.1f && Pal.Hash01(x, 7, z) < 0.5f);
                int h = d < 0.6f ? 2 : 1;
                if (fissure)
                {
                    // la lave est à la surface, un peu en retrait
                    feu.Set(x, h - 1, z, second - best < 0.35f ? NaLaveClaire : NaLave);
                    for (int y = 0; y < h - 1; y++) croute.Set(x, y, z, NaLaveSombre);
                }
                else
                {
                    var c = Pal.Hash01(x, 1, z) < 0.25f ? NaBasalteClair : NaBasalte;
                    for (int y = 0; y < h; y++) croute.Set(x, y, z, Pal.Shade(c, y == h - 1 ? 1.0f : 0.8f));
                    // bord rougeoyant des plaques
                    if (second - best < 2.2f) croute.Set(x, h - 1, z, Pal.C("6a2a1e"));
                }
            }
            // quelques blocs de croûte soulevés
            int nb = NaI(r, 2, 3);
            for (int i = 0; i < nb; i++)
            {
                float a = NaF(r, 0, 6.28f), d = NaF(r, 0.2f, 0.6f) * R;
                NaRoche(croute, r, new Vector3(Mathf.Cos(a) * d, 2, Mathf.Sin(a) * d), new Vector3(2.4f, 2.2f, 2.4f), 6, new[] { NaBasalte, NaBasalteClair, Pal.Shade(NaBasalte, 0.8f) }, 0, NaBasalte);
            }
            foreach (var kv in croute.Tous()) feu.Remove(kv.Key.x, kv.Key.y, kv.Key.z);
            rig.Add("corps", croute, Vector3.zero);
            rig.Add("lueurLave", feu, Vector3.zero, "corps");
            return rig;
        }

        // ------------------------------------------------------------------ cristal de braise (voxel de 0,05 m)

        static VoxelRig CristalBraise(System.Random r)
        {
            var rig = NaRig("cristal_braise", 0.05f);
            var gangue = new VoxelModel(); var coeur = new VoxelModel();
            var tons = NaTonsPierre(NaP(r, 0.5f) ? 2 : 3, r);
            NaRoche(gangue, r, new Vector3(0, 1, 0), new Vector3(7, 4, 6), 7, tons, 0, tons[2]);
            var verre = new[] { Pal.C("8a2a20"), Pal.C("a83a22"), Pal.C("6e2018") };
            int n = NaI(r, 3, 5);
            for (int i = 0; i < n; i++)
            {
                float a = NaF(r, 0, 6.28f), inc = i == 0 ? NaF(r, 0, 0.2f) : NaF(r, 0.35f, 0.75f);
                float L = i == 0 ? NaF(r, 14, 19) : NaF(r, 7, 12), rr = i == 0 ? 2.6f : NaF(r, 1.4f, 2.0f);
                var dir = new Vector3(Mathf.Cos(a) * inc, 1, Mathf.Sin(a) * inc).normalized;
                var p0 = new Vector3(Mathf.Cos(a) * 1.5f, 2, Mathf.Sin(a) * 1.5f);
                var p1 = p0 + dir * (L * 0.78f); var p2 = p0 + dir * L;
                // fût en verre sombre (corps) ; arêtes et pointe incandescentes (lueur)
                NaPrisme(gangue, p0, p1, rr, rr * 0.9f, 6, a, verre);
                NaPrisme(coeur, p1 - dir * 0.6f, p2, rr * 0.92f, 0.4f, 6, a, new[] { Pal.C("ff8a30"), Pal.C("ffb050"), Pal.C("e8601e") });
                // cœur visible par une fente
                coeur.Line(p0.x + dir.x * 2, p0.y + dir.y * 2, p0.z + dir.z * 2, p1.x, p1.y, p1.z, 0.5f, Pal.C("ff9a3a"));
            }
            foreach (var kv in coeur.Tous()) gangue.Remove(kv.Key.x, kv.Key.y, kv.Key.z);
            NaSol(gangue); NaSol(coeur);
            rig.Add("corps", gangue, Vector3.zero);
            rig.Add("lueurCristal", coeur, Vector3.zero, "corps");
            return rig;
        }
    }
}

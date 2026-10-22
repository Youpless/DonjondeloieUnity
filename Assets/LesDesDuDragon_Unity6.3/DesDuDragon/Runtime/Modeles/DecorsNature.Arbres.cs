// Les Dés du Dragon — arbres : chêne, pin, saule, arbre mort, pin brûlé (voxel de 0,1 m).
// Pièces : « corps » (tronc, racines, branches maîtresses) et « feuillage » (houppier, parent corps, pivot en haut du tronc).
using System;
using UnityEngine;
using DesDuDragon.Voxel;

namespace DesDuDragon.Modeles
{
    public static partial class DecorsModeles
    {
        // écorces et feuillages
        static readonly Color32 NaEcorce = Pal.C("6e5038"), NaEcorceSombre = Pal.C("4a3426");
        static readonly Color32 NaPinEcorce = Pal.C("7a4a32"), NaPinSillon = Pal.C("4e2e22");
        static readonly Color32 NaMortEcorce = Pal.C("7d7468"), NaMortSillon = Pal.C("4f4a45");
        static readonly Color32 NaCharbon = Pal.C("2e2826"), NaCharbonClair = Pal.C("4a403a"), NaCendre = Pal.C("8a817a");
        static readonly Color32 NaSauleEcorce = Pal.C("5f5440"), NaSauleSillon = Pal.C("3e3628");

        static void NaAjouterArbre(VoxelRig rig, VoxelModel corps, VoxelModel feuillage, Vector3 pivot)
        {
            NaSol(corps); NaSol(feuillage);
           
            rig.Add("corps", corps, Vector3.zero);
            rig.Add("feuillage", feuillage, pivot, "corps");
        }

        // ------------------------------------------------------------------ chêne (3 – 4,5 m)

        static VoxelRig Chene(System.Random r)
        {
            var rig = NaRig("chene", 0.1f);
            var corps = new VoxelModel(); var feu = new VoxelModel();
            int H = NaI(r, 13, 18);
            float px = NaF(r, -0.06f, 0.06f), pz = NaF(r, -0.06f, 0.06f);
            var haut = NaTronc(corps, r, H, 2.7f, 2.0f, px, pz, 0.5f, 7, NaEcorce, NaEcorceSombre);
            NaRacines(corps, r, NaI(r, 4, 6), 2.6f, NaF(r, 3.5f, 5f), NaEcorceSombre);

            // teinte : du vert frais des prés au vert profond du bois
            float t = NaF(r, 0, 1);
            var vClair = NaTeinte(Pal.Mix(Pal.C("7da246"), Pal.C("568e44"), t), r, 0.05f);
            var vBase = NaTeinte(Pal.Mix(Pal.C("587f34"), Pal.C("386a36"), t), r, 0.05f);
            var vSombre = Pal.Mix(Pal.C("3c5e2a"), Pal.C("2a4e2c"), t);
            var vTouffe = Pal.Mix(Pal.C("74983c"), Pal.C("4f8a50"), t);

            // branches maîtresses, chacune portant une masse de feuillage
            int nb = NaI(r, 3, 5); float a0 = NaF(r, 0, 6.28f);
            float largeur = NaF(r, 0.85f, 1.15f);
            for (int i = 0; i < nb; i++)
            {
                float a = a0 + i * 6.2832f / nb + NaF(r, -0.4f, 0.4f);
                float L = NaF(r, 7f, 10f) * largeur, monte = NaF(r, 0.55f, 1.0f);
                float y0 = H - NaF(r, 1f, 4f);
                var b = new Vector3(haut.x + Mathf.Cos(a) * L, y0 + L * monte, haut.z + Mathf.Sin(a) * L);
                corps.Line(haut.x, y0, haut.z, b.x, b.y, b.z, 1.7f, 0.9f, NaEcorce);
                NaMasse(feu, b.x, b.y + 3.5f, b.z, NaF(r, 6f, 8f), NaF(r, 5f, 6.5f), NaF(r, 6f, 8f), 2, NaF(r, 0, 1.6f), vBase);
            }
            // masse du sommet, plus une couronne basse qui épaissit la silhouette
            NaMasse(feu, haut.x + NaF(r, -1, 1), H + NaF(r, 13, 16), haut.z + NaF(r, -1, 1), NaF(r, 8, 10) * largeur, NaF(r, 6, 7.5f), NaF(r, 8, 10) * largeur, 2, NaF(r, 0, 1.6f), vBase);
            NaMasse(feu, haut.x, H + 5, haut.z, 9f * largeur, 4f, 9f * largeur, 2, 0.4f, vBase);
            NaPeindreFeuillage(feu, vClair, vSombre, vTouffe, r.Next(100));
            NaStrier(corps, NaEcorce, NaEcorceSombre, r.Next(100));
            // remettre le tronc taillé par-dessus les branches (garde les côtes au pied)
            NaTronc(corps, new System.Random(1), 3, 2.7f, 2.0f, 0, 0, 0, 7, NaEcorce, NaEcorceSombre);
            NaAjouterArbre(rig, corps, feu, new Vector3(haut.x, H, haut.z));
            return rig;
        }

        // ------------------------------------------------------------------ pin (4 – 6 m)

        /// Étage de pin : disque à pans dont les bords retombent (cône aplati), dents irrégulières.
        static void NaEtagePin(VoxelModel m, float cx, float cy, float cz, float rayon, float h, int pans, float rot, int graine, Color32 c)
        {
            int y0 = (int)Math.Floor(cy), y1 = (int)Math.Ceiling(cy + h);
            for (int y = y0; y <= y1; y++)
            {
                // rayon quantifié par marches de 2 voxels : des gradins nets au lieu de stries
                float t = (float)Math.Floor((y - cy) / 2f) * 2f / h;
                float rr = rayon * (1f - t) + 0.8f;
                int xa = (int)Math.Floor(cx - rr - 1), xb = (int)Math.Ceiling(cx + rr + 1);
                int za = (int)Math.Floor(cz - rr - 1), zb = (int)Math.Ceiling(cz + rr + 1);
                for (int x = xa; x <= xb; x++) for (int z = za; z <= zb; z++)
                {
                    float dx = x + 0.5f - cx, dz = z + 0.5f - cz;
                    float ang = Mathf.Atan2(dz, dx) - rot;
                    // polygone à « pans » côtés : distance au bord du polygone
                    float sect = 6.2832f / pans;
                    float aa = ang - sect * (float)Math.Floor(ang / sect + 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dz * dz) * Mathf.Cos(aa) / Mathf.Cos(sect * 0.5f);
                    // pointes : le bord avance aux coins des pans
                    float dent = (Pal.Hash01((int)((ang + 7) * 3), graine, y0) - 0.5f) * 1.6f;
                    if (d <= rr + (t < 0.25f ? dent : 0)) m.Set(x, y, z, c);
                }
            }
            // le dessous retombe : on creuse l'intérieur du premier niveau pour un bord pendant
            for (int x = (int)(cx - rayon); x <= (int)(cx + rayon); x++) for (int z = (int)(cz - rayon); z <= (int)(cz + rayon); z++)
            {
                float dx = x + 0.5f - cx, dz = z + 0.5f - cz;
                if (dx * dx + dz * dz < (rayon * 0.6f) * (rayon * 0.6f)) m.Remove(x, y0, z);
            }
        }

        static VoxelRig Pin(System.Random r)
        {
            var rig = NaRig("pin", 0.1f);
            var corps = new VoxelModel(); var feu = new VoxelModel();
            int Htot = NaI(r, 40, 58);
            int H = (int)(Htot * NaF(r, 0.86f, 0.92f));
            float px = NaF(r, -0.035f, 0.035f), pz = NaF(r, -0.035f, 0.035f);
            var haut = NaTronc(corps, r, H, 1.9f, 0.9f, px, pz, 0.25f, 6, NaPinEcorce, NaPinSillon);
            NaRacines(corps, r, NaI(r, 3, 5), 1.9f, NaF(r, 2.5f, 3.5f), NaPinSillon);
            NaStrier(corps, NaPinEcorce, NaPinSillon, r.Next(100));
            NaTronc(corps, new System.Random(2), 3, 1.9f, 1.5f, 0, 0, 0, 6, NaPinEcorce, NaPinSillon);

            var vBase = NaTeinte(Pal.C("2f5f3c"), r, 0.07f);
            var vClair = Pal.Shade(Pal.Mix(vBase, Pal.C("6f9a58"), 0.35f), 1.08f);
            var vSombre = Pal.Shade(vBase, 0.62f);
            var vTouffe = Pal.Mix(vBase, Pal.C("3b6a4e"), 0.6f);

            int nEtages = NaI(r, 4, 6);
            float yBas = Htot * NaF(r, 0.22f, 0.3f), largeur = NaF(r, 10f, 12.5f);
            float pasY = (Htot - yBas - 6) / (nEtages - 0.4f);
            float rot = NaF(r, 0, 1);
            for (int i = 0; i < nEtages; i++)
            {
                float t = i / (float)(nEtages - 1);
                float y = yBas + i * pasY, rayon = Mathf.Lerp(largeur, 3.2f, Mathf.Pow(t, 0.9f)) * NaF(r, 0.92f, 1.06f);
                float fx = px * y, fz = pz * y;
                NaEtagePin(feu, fx + NaF(r, -0.6f, 0.6f), y, fz + NaF(r, -0.6f, 0.6f), rayon, pasY * NaF(r, 1.35f, 1.55f), NaI(r, 7, 9), rot + i * 0.7f, r.Next(1000), vBase);
            }
            // pointe
            feu.Line(px * (Htot - 8), Htot - 8, pz * (Htot - 8), px * Htot, Htot, pz * Htot, 2.2f, 0.6f, vBase);
            NaPeindreFeuillage(feu, vClair, vSombre, vTouffe, r.Next(100));
            NaAjouterArbre(rig, corps, feu, new Vector3(px * yBas, yBas, pz * yBas));
            return rig;
        }

        // ------------------------------------------------------------------ saule du marais (3 – 4 m)

        static VoxelRig Saule(System.Random r)
        {
            var rig = NaRig("saule", 0.1f);
            var corps = new VoxelModel(); var feu = new VoxelModel();
            int H = NaI(r, 15, 20);
            float a = NaF(r, 0, 6.28f), pente = NaF(r, 0.1f, 0.22f);
            float px = Mathf.Cos(a) * pente, pz = Mathf.Sin(a) * pente;
            var haut = NaTronc(corps, r, H, 3.2f, 2.3f, px, pz, 0.8f, 6, NaSauleEcorce, NaSauleSillon);
            NaRacines(corps, r, NaI(r, 5, 7), 3f, NaF(r, 4f, 6f), NaSauleSillon);

            var vBase = NaTeinte(Pal.C("6b7a3a"), r, 0.06f);
            var vClair = Pal.Mix(vBase, Pal.C("a3a85a"), 0.4f);
            var vSombre = Pal.Shade(Pal.Mix(vBase, Pal.C("3c4428"), 0.5f), 0.85f);
            var vTouffe = Pal.Mix(vBase, Pal.C("55642e"), 0.7f);

            // branches qui s'arquent
            int nb = NaI(r, 4, 6); float a0 = NaF(r, 0, 6.28f);
            var bouts = new Vector3[nb];
            for (int i = 0; i < nb; i++)
            {
                float ab = a0 + i * 6.2832f / nb + NaF(r, -0.3f, 0.3f), L = NaF(r, 7, 10);
                var b = new Vector3(haut.x + Mathf.Cos(ab) * L, H + NaF(r, 5, 8), haut.z + Mathf.Sin(ab) * L);
                corps.Line(haut.x, H - 2, haut.z, b.x, b.y, b.z, 1.6f, 0.8f, NaSauleEcorce);
                bouts[i] = b;
                NaMasse(feu, b.x, b.y + 2, b.z, NaF(r, 6, 7.5f), 3.5f, NaF(r, 6, 7.5f), 2, NaF(r, 0, 1.5f), vBase);
            }
            NaMasse(feu, haut.x, H + 9, haut.z, 9, 4.5f, 9, 2, 0.3f, vBase);
            // rideaux qui pendent : lanières sous le bord du houppier
            var fins = new System.Collections.Generic.List<Vector3Int>();
            foreach (var kv in feu.Tous())
            {
                var p = kv.Key;
                if (feu.Has(p.x, p.y - 1, p.z)) continue;
                float dx = p.x + 0.5f - haut.x, dz = p.z + 0.5f - haut.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < 7.5f) continue;
                if (Pal.Hash01(p.x / 2, 3, p.z / 2) < 0.42f && Pal.Hash01(p.x, 4, p.z) < 0.7f) fins.Add(p);
            }
            foreach (var p in fins)
            {
                int L = (int)(NaF(r, 0.2f, 0.6f) * (p.y - 4));
                L = Math.Max(3, L);
                for (int k = 1; k <= L; k++) feu.Set(p.x, p.y - k, p.z, k > L - 2 ? vClair : vTouffe);
            }
            NaPeindreFeuillage(feu, vClair, vSombre, vTouffe, r.Next(100));
            // la mousse et les lanières gardent leur ton pendant
            foreach (var p in fins) for (int k = 1; k < 3; k++) if (feu.Has(p.x, p.y - k, p.z)) feu.Set(p.x, p.y - k, p.z, vTouffe);
            NaStrier(corps, NaSauleEcorce, NaSauleSillon, r.Next(100));
            NaTronc(corps, new System.Random(3), 3, 3.2f, 2.8f, 0, 0, 0, 6, NaSauleEcorce, NaSauleSillon);
            // mousse au pied
            corps.Paint((x, y, z, c) => y < 3 && !corps.Has(x, y + 1, z) ? Pal.C("5d6b34") : c);
            NaAjouterArbre(rig, corps, feu, new Vector3(haut.x, H, haut.z));
            return rig;
        }

        // ------------------------------------------------------------------ branches nues (arbre mort, pin brûlé)

        /// Branche ramifiée récursive : un trait effilé, puis 1 à 3 rameaux qui s'écartent.
        static void NaBranche(VoxelModel m, System.Random r, Vector3 a, Vector3 dir, float L, float rayon, int prof, Color32 c)
        {
            var b = a + dir * L;
            m.Line(a.x, a.y, a.z, b.x, b.y, b.z, rayon, Mathf.Max(0.5f, rayon * 0.68f), c);
            if (prof <= 0 || rayon < 0.6f) return;
            int n = NaI(r, 1, prof >= 2 ? 3 : 2);
            for (int i = 0; i < n; i++)
            {
                var d2 = (dir + new Vector3(NaF(r, -0.8f, 0.8f), NaF(r, -0.15f, 0.55f), NaF(r, -0.8f, 0.8f))).normalized;
                NaBranche(m, r, b, d2, L * NaF(r, 0.55f, 0.8f), rayon * 0.68f, prof - 1, c);
            }
        }

        static VoxelRig ArbreMort(System.Random r)
        {
            var rig = NaRig("arbre_mort", 0.1f);
            var corps = new VoxelModel(); var bra = new VoxelModel();
            int H = NaI(r, 12, 17);
            float a = NaF(r, 0, 6.28f), pente = NaF(r, 0.04f, 0.16f);
            var ecorce = NaTeinte(Pal.Mix(NaMortEcorce, Pal.C("6b5a48"), NaF(r, 0, 1)), r, 0.06f);
            var sillon = Pal.Shade(ecorce, 0.62f);
            var haut = NaTronc(corps, r, H, 2.6f, 1.6f, Mathf.Cos(a) * pente, Mathf.Sin(a) * pente, 1.0f, 5, ecorce, sillon);
            NaRacines(corps, r, NaI(r, 4, 6), 2.4f, NaF(r, 4f, 6f), sillon);
            // un creux sombre dans le tronc
            float ac = NaF(r, 0, 6.28f);
            int yc = NaI(r, 4, 8);
            corps.Ellipsoid(Mathf.Cos(ac) * 2.4f + Mathf.Cos(a) * pente * yc, yc, Mathf.Sin(ac) * 2.4f + Mathf.Sin(a) * pente * yc, 1.1f, 2.2f, 1.1f, Pal.C("231e1c"));
            // branches maîtresses (corps) et ramure (feuillage, qui grince au vent)
            int nb = NaI(r, 2, 4); float a0 = NaF(r, 0, 6.28f);
            for (int i = 0; i < nb; i++)
            {
                float ab = a0 + i * 6.2832f / nb + NaF(r, -0.5f, 0.5f);
                var dir = new Vector3(Mathf.Cos(ab), NaF(r, 0.5f, 1.3f), Mathf.Sin(ab)).normalized;
                var depart = new Vector3(haut.x, H - NaF(r, 0, 4), haut.z);
                float L = NaF(r, 5, 8);
                corps.Line(depart.x, depart.y, depart.z, depart.x + dir.x * L, depart.y + dir.y * L, depart.z + dir.z * L, 1.6f, 1.1f, ecorce);
                NaBranche(bra, r, depart + dir * L, (dir + new Vector3(0, 0.3f, 0)).normalized, NaF(r, 6, 9), 1.1f, 3, ecorce);
            }
            // une flèche cassée au sommet
            NaBranche(bra, r, new Vector3(haut.x, H, haut.z), new Vector3(NaF(r, -0.3f, 0.3f), 1, NaF(r, -0.3f, 0.3f)).normalized, NaF(r, 6, 10), 1.5f, 2, ecorce);
            NaStrier(corps, ecorce, sillon, r.Next(100));
            NaStrier(bra, ecorce, Pal.Shade(ecorce, 0.8f), r.Next(100));
            NaTronc(corps, new System.Random(4), 3, 2.6f, 2.3f, 0, 0, 0, 5, ecorce, sillon);
            NaAjouterArbre(rig, corps, bra, new Vector3(haut.x, H, haut.z));
            return rig;
        }

        // ------------------------------------------------------------------ pin brûlé des Pics (3,5 – 5 m)

        static VoxelRig PinBrule(System.Random r)
        {
            var rig = NaRig("pin_brule", 0.1f);
            var corps = new VoxelModel(); var bra = new VoxelModel(); var braise = new VoxelModel();
            int H = NaI(r, 34, 48);
            float a = NaF(r, 0, 6.28f), pente = NaF(r, 0.0f, 0.07f);
            float px = Mathf.Cos(a) * pente, pz = Mathf.Sin(a) * pente;
            var haut = NaTronc(corps, r, H, 2.4f, 1.0f, px, pz, 0.3f, 6, NaCharbonClair, NaCharbon);
            NaRacines(corps, r, NaI(r, 3, 5), 2f, 3f, NaCharbon);
            NaStrier(corps, NaCharbonClair, NaCharbon, r.Next(100));
            NaTronc(corps, new System.Random(5), 3, 2.0f, 1.8f, 0, 0, 0, 6, NaCharbonClair, NaCharbon);
            // l'écorce rousse survit en bas, côté abrité
            float abri = NaF(r, 0, 6.28f);
            corps.Paint((x, y, z, c) =>
            {
                float an = Mathf.Atan2(z, x) - abri; float co = Mathf.Cos(an);
                if (y < H * 0.35f && co > 0.4f && Pal.Hash01(x, y / 2, z) < 0.7f) return Pal.Mix(NaPinEcorce, NaCharbonClair, 0.45f);
                return c;
            });
            // moignons de branches qui pointent vers le bas, de moins en moins longs vers le haut
            int n = NaI(r, 8, 13);
            for (int i = 0; i < n; i++)
            {
                float t = NaF(r, 0.3f, 0.95f), y = H * t, ab = NaF(r, 0, 6.28f);
                float L = Mathf.Lerp(8f, 3f, t) * NaF(r, 0.6f, 1.1f);
                var dep = new Vector3(px * y, y, pz * y);
                var fin = dep + new Vector3(Mathf.Cos(ab) * L, -L * NaF(r, 0.1f, 0.5f), Mathf.Sin(ab) * L);
                bra.Line(dep.x, dep.y, dep.z, fin.x, fin.y, fin.z, 0.9f, 0.5f, NaCharbon);
                bra.Set((int)Math.Floor(fin.x), (int)Math.Floor(fin.y), (int)Math.Floor(fin.z), NaCendre);
            }
            // tête cassée, pointe de cendre
            bra.Line(haut.x, H, haut.z, haut.x + NaF(r, -2, 2), H + NaF(r, 3, 5), haut.z + NaF(r, -2, 2), 0.8f, 0.5f, NaCendre);
            // quelques braises qui couvent dans les fentes
            int nbr = NaI(r, 2, 4);
            for (int i = 0; i < nbr; i++)
            {
                float y = NaF(r, 3, H * 0.5f), ab = NaF(r, 0, 6.28f), rr = Mathf.Lerp(2.0f, 0.7f, y / H) * (1f + 0.55f * Mathf.Exp(-y / 2.2f)) + 0.1f;
                int bx = (int)Math.Floor(px * y + Mathf.Cos(ab) * rr), bz = (int)Math.Floor(pz * y + Mathf.Sin(ab) * rr), by = (int)y;
                braise.Set(bx, by, bz, Pal.C("ff7a2a"));
                if (NaP(r, 0.6f)) braise.Set(bx, by + 1, bz, Pal.C("e0521c"));
            }
            NaSol(corps); NaSol(bra);
            rig.Add("corps", corps, Vector3.zero);
            rig.Add("feuillage", bra, new Vector3(haut.x * 0.3f, H * 0.3f, haut.z * 0.3f), "corps");
            if (braise.Count > 0) rig.Add("lueurBraises", braise, Vector3.zero, "corps");
            return rig;
        }
    }
}

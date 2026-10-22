// Les Dés du Dragon — où poser les lieux remarquables, le décor semé et les lampes.
using System;
using System.Collections.Generic;
using UnityEngine;
using DesDuDragon.Regles;
using DesDuDragon.Modeles;
using DesDuDragon.Voxel;

namespace DesDuDragon.Monde
{
    public sealed partial class IleModele
    {
        readonly List<KeyValuePair<Vector3, float>> zonesInterdites = new List<KeyValuePair<Vector3, float>>();

        Vector3 PosCase(int c) { return new Vector3(Plateau[c].X, 0, Plateau[c].Z); }

        /// Direction de la route au passage de la case c (moyenne entrée/sortie).
        Vector3 Tangente(int c)
        {
            var p = PosCase(c);
            var avant = Plateau[c].Suivants.Count > 0 ? PosCase(Plateau[c].Suivants[0]) : p;
            var arriere = Plateau[c].Precedents.Count > 0 ? PosCase(Plateau[c].Precedents[0]) : p;
            var t = avant - arriere; t.y = 0;
            return t.sqrMagnitude > 1e-4f ? t.normalized : Vector3.forward;
        }

        /// Point au bord de la route : côté extérieur de l'île (exterieur = true) ou intérieur.
        Vector3 Bord(int c, float dist, bool exterieur, float glisse = 0f)
        {
            var p = PosCase(c);
            var t = Tangente(c);
            var n = new Vector3(-t.z, 0, t.x);
            float s = Vector3.Dot(n, p) >= 0 ? 1f : -1f;
            if (!exterieur) s = -s;
            // Rien de haut entre la caméra (au sud, par défaut) et la case : on passe de l'autre côté du chemin.
            if ((n * s).z < -0.45f) s = -s;
            return p + n * (s * dist) + t * glisse;
        }

        static float CapVers(Vector3 de, Vector3 vers)
        {
            var d = vers - de;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        void Lieu(string id, Vector3 pos, float rotY, float rayon, int g = 1, float echelle = 1f)
        {
            Aplanir(pos, rayon * 0.8f);
            pos.y = HauteurEn(pos.x, pos.z);
            Lieux.Add(new Placement { Id = id, Graine = g, Pos = pos, RotY = rotY, Echelle = echelle, Lieu = true });
            zonesInterdites.Add(new KeyValuePair<Vector3, float>(pos, rayon));
        }

        void LieuFace(string id, int c, float dist, bool exterieur, float rayon, int g = 1, float glisse = 0f)
        {
            var pos = Bord(c, dist, exterieur, glisse);
            Lieu(id, pos, CapVers(pos, PosCase(c) + Tangente(c) * glisse * 0.5f), rayon, g);
        }

        /// Met le terrain d'un disque à la hauteur de son centre (sans toucher au chemin).
        void Aplanir(Vector3 centre, float rayon)
        {
            float h = Mathf.Max(0.25f, HauteurEn(centre.x, centre.z));
            int r = Mathf.CeilToInt(rayon / Cell) + 1;
            int cx = (int)((centre.x - X0) / Cell), cz = (int)((centre.z - Z0) / Cell);
            for (int z = cz - r; z <= cz + r; z++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || z < 0 || x >= NX || z >= NZ) continue;
                    float dx = CX(x) - centre.x, dz = CZ(z) - centre.z;
                    if (dx * dx + dz * dz > rayon * rayon) continue;
                    if (Dist[I(x, z)] < 1.6f) continue;
                    H[I(x, z)] = Quant(h);
                }
        }

        void PlacerLieux()
        {
            // Valcreux : la porte sur la route, des maisons hors de la boucle, côté sud
            var p39 = PosCase(39); var p0 = PosCase(0);
            // la porte de Valcreux enjambe la route au sud-ouest, là où elle ne cache aucune case
            var porte = (PosCase(35) + PosCase(36)) * 0.5f;
            Lieu("porte_ville", porte, CapVers(PosCase(35), PosCase(36)), 2.2f);
            int maison = 0;
            for (int k = -3; k <= 3; k++)
            {
                var b = Bord(k < 0 ? 40 + k : k, 6.2f + (Math.Abs(k) % 2) * 1.6f, true, 0);
                if (k == 0) continue;
                Lieu("maison", b, CapVers(b, PosCase(k < 0 ? 40 + k : k)), 2.4f, 11 + maison++);
                var b2 = Bord(k < 0 ? 40 + k : k, 10.8f, true, 1.2f);
                if (Ellipse(b2.x, b2.z) < 0.9f) Lieu("maison", b2, CapVers(b2, PosCase(0)), 2.4f, 31 + maison++);
            }
            Lieu("tour_garde", Bord(2, 7.5f, false, 0), 0, 2.2f, 1);
            Lieu("banniere", Bord(0, 3.4f, true, 1.6f), CapVers(Bord(0, 3.4f, true, 1.6f), p0), 0.8f, 1);
            Lieu("banniere", Bord(0, 3.4f, true, -1.6f), CapVers(Bord(0, 3.4f, true, -1.6f), p0), 0.8f, 2);
            Lieu("maison", Bord(1, 4.6f, false, 0), CapVers(Bord(1, 4.6f, false, 0), PosCase(1)), 2.4f, 51);
            Lieu("maison", Bord(38, 4.8f, false, 0.8f), CapVers(Bord(38, 4.8f, false, 0.8f), PosCase(38)), 2.4f, 52);
            LieuFace("puits", 38, 3.0f, true, 1.2f);

            LieuFace("taverne", 4, 4.0f, true, 2.8f, 1);
            LieuFace("taverne", 19, 4.0f, true, 2.8f, 2);
            LieuFace("chapelle", 13, 4.2f, true, 2.8f);
            LieuFace("etal", 10, 3.0f, true, 1.5f);
            LieuFace("autel_ombrelune", 26, 3.0f, true, 1.5f);
            LieuFace("comptoir_fossoyeurs", 27, 3.6f, true, 2.2f);
            LieuFace("forge", 34, 3.6f, true, 2.4f);
            LieuFace("mausolee", 24, 4.2f, true, 2.4f);
            LieuFace("ruine", 29, 4.4f, true, 2.4f);
            LieuFace("ruine", 23, 4.6f, false, 2.4f, 2);
            foreach (int c in new[] { 8, 21, 31 }) LieuFace("panneau", c, 1.7f, true, 0.6f, c);

            // L'entrée des catacombes ouvre le raccourci des cryptes.
            int cata = Plateau[21].Suivants.Count > 1 ? Plateau[21].Suivants[1] : -1;
            if (cata >= 0) LieuFace("portail_cryptes", cata, 2.9f, false, 2.0f);

            // Pontons dans le marais
            foreach (int c in new[] { 15, 17 }) LieuFace("ponton", c, 3.2f, false, 1.8f, c);

            // La montagne de l'antre, la gueule tournée vers la case de l'Antre, et Ignathor endormi devant.
            int antre = -1;
            for (int i = 0; i < Plateau.Nombre; i++) if (Plateau[i].Type == TypeCase.Antre) antre = i;
            if (antre >= 0)
            {
                var pa = PosCase(antre);
                Lieu("montagne_antre", Montagne, CapVers(Montagne, pa), 7.5f);
                var dragon = Vector3.Lerp(pa, Montagne, 0.42f);
                Lieu("monstre:dragon", dragon, CapVers(dragon, pa) + 20f, 2.6f);
            }
        }

        // ------------------------------------------------------------------ décor semé

        static float Poids(string id)
        {
            switch (id)
            {
                case "chene": case "pin": case "herbes_hautes": case "roseaux": return 3f;
                case "saule": case "arbre_mort": case "buisson": case "fleurs": case "fougere": case "tombe": case "pin_brule": case "stalagmite": return 2f;
                case "rocher": case "rocher_mousse": case "pic_rocheux": case "champignon_marais": return 1.5f;
                case "os": case "lave": case "tas_or": return 0.7f;
                case "cristal_braise": case "cierge": case "feu_follet": return 0.5f;
                default: return 1f;
            }
        }

        static float Densite(Zone z)
        {
            switch (z)
            {
                case Zone.Bois: return 0.8f;
                case Zone.Village: return 0.22f;
                case Zone.Marais: return 0.45f;
                case Zone.Cryptes: return 0.42f;
                case Zone.Pics: return 0.42f;
                default: return 0.32f;
            }
        }

        bool Libre(Vector3 p, float marge)
        {
            foreach (var kv in zonesInterdites)
            {
                float dx = p.x - kv.Key.x, dz = p.z - kv.Key.z, r = kv.Value + marge;
                if (dx * dx + dz * dz < r * r) return false;
            }
            for (int c = 0; c < Plateau.Nombre; c++)
            {
                float dx = p.x - Plateau[c].X, dz = p.z - Plateau[c].Z;
                if (dx * dx + dz * dz < 2.2f * 2.2f) return false;
            }
            return true;
        }

        static readonly HashSet<string> Grands = new HashSet<string> { "chene", "pin", "saule", "arbre_mort", "pin_brule", "pic_rocheux", "stalagmite" };

        void Semer()
        {
            var r = new System.Random(graine * 977 + 3);
            const float pas = 1.7f;
            for (float z = Z0 + 1; z < Z0 + NZ * Cell - 1; z += pas)
                for (float x = X0 + 1; x < X0 + NX * Cell - 1; x += pas)
                {
                    float px = x + (float)(r.NextDouble() - 0.5) * pas * 0.9f, pz = z + (float)(r.NextDouble() - 0.5) * pas * 0.9f;
                    float e = Ellipse(px, pz);
                    if (e > 0.95f) continue;
                    var zo = ZoneEn(px, pz);
                    float d = DistanceChemin(px, pz);
                    float h = HauteurEn(px, pz);
                    if (d < 2.3f) continue;
                    var ids = Catalogue.DecorsDeZone(zo);
                    if (ids == null || ids.Length == 0) continue;
                    if (r.NextDouble() > Densite(zo) * (d < 4f ? 0.55f : 1f)) continue;
                    string id = Tirer(ids, r);
                    bool eau = h < NiveauEau + 0.01f;
                    if (eau && id != "roseaux") { if (zo == Zone.Marais && r.NextDouble() < 0.5) id = "roseaux"; else continue; }
                    if (!eau && id == "roseaux" && zo == Zone.Marais && r.NextDouble() < 0.6) continue;
                    // les grands éléments s'écartent du chemin pour ne pas cacher les cases
                    if (Grands.Contains(id) && d < 3.6f) continue;
                    if (Grands.Contains(id) && CacheUneCase(px, pz)) continue;
                    var pos = new Vector3(px, h, pz);
                    if (!Libre(pos, Grands.Contains(id) ? 1.2f : 0.4f)) continue;
                    Decors.Add(new Placement { Id = id, Graine = r.Next(1, 100000), Pos = pos, RotY = (float)r.NextDouble() * 360f, Echelle = 0.85f + (float)r.NextDouble() * 0.35f });
                }
            // Petits détails le long du chemin
            for (int c = 0; c < Plateau.Nombre; c++)
                foreach (int s in Plateau[c].Suivants)
                {
                    var a = PosCase(c); var b = PosCase(s);
                    for (int k = 0; k < 3; k++)
                    {
                        float t = (float)r.NextDouble();
                        var p = Vector3.Lerp(a, b, t);
                        var dir = (b - a).normalized; var n = new Vector3(-dir.z, 0, dir.x);
                        p += n * ((r.NextDouble() < 0.5 ? -1f : 1f) * (1.6f + (float)r.NextDouble() * 1.2f));
                        if (DistanceChemin(p.x, p.z) < 1.4f || !Libre(p, 0.2f)) continue;
                        var zo = ZoneEn(p.x, p.z);
                        string id = null;
                        switch (zo)
                        {
                            case Zone.Village: id = r.NextDouble() < 0.6 ? "fleurs" : "herbes_hautes"; break;
                            case Zone.Bois: id = r.NextDouble() < 0.5 ? "fougere" : "champignon"; break;
                            case Zone.Marais: id = r.NextDouble() < 0.6 ? "roseaux" : "champignon_marais"; break;
                            case Zone.Cryptes: id = r.NextDouble() < 0.5 ? "os" : "herbes_hautes"; break;
                            case Zone.Pics: id = r.NextDouble() < 0.5 ? "os" : "rocher"; break;
                            default: id = r.NextDouble() < 0.5 ? "os" : "tas_or"; break;
                        }
                        p.y = HauteurEn(p.x, p.z);
                        if (p.y < NiveauEau && id != "roseaux") continue;
                        Decors.Add(new Placement { Id = id, Graine = r.Next(1, 100000), Pos = p, RotY = (float)r.NextDouble() * 360f, Echelle = 0.8f + (float)r.NextDouble() * 0.3f });
                    }
                }
            PlacerLampes();
        }

        /// Un grand élément juste au sud d'une case la cacherait à la caméra.
        bool CacheUneCase(float x, float z)
        {
            for (int c = 0; c < Plateau.Nombre; c++)
            {
                float dz = Plateau[c].Z - z, dx = Mathf.Abs(Plateau[c].X - x);
                if (dz > 0 && dz < 7.5f && dx < 3.2f) return true;
            }
            return false;
        }

        static string Tirer(string[] ids, System.Random r)
        {
            float tot = 0;
            foreach (var id in ids) tot += Poids(id);
            float v = (float)r.NextDouble() * tot;
            foreach (var id in ids) { v -= Poids(id); if (v <= 0) return id; }
            return ids[ids.Length - 1];
        }

        void PlacerLampes()
        {
            foreach (var l in Lieux)
            {
                switch (l.Id)
                {
                    case "taverne": Lampes.Add(new Lampe { Pos = l.Pos + Vector3.up * 2.2f + Quaternion.AngleAxis(l.RotY, Vector3.up) * new Vector3(0, 0, 2.3f), Couleur = Pal.C("ffb45a"), Portee = 7f, Intensite = 2.2f }); break;
                    case "forge": Lampes.Add(new Lampe { Pos = l.Pos + Vector3.up * 1.2f, Couleur = Pal.C("ff7a2a"), Portee = 7f, Intensite = 3f }); break;
                    case "chapelle": Lampes.Add(new Lampe { Pos = l.Pos + Vector3.up * 2.5f + Quaternion.AngleAxis(l.RotY, Vector3.up) * new Vector3(0, 0, 2.6f), Couleur = Pal.C("cfe4ff"), Portee = 6f, Intensite = 1.4f }); break;
                    case "autel_ombrelune": Lampes.Add(new Lampe { Pos = l.Pos + Vector3.up * 1.5f, Couleur = Pal.C("b9a6ff"), Portee = 5f, Intensite = 1.6f }); break;
                    case "comptoir_fossoyeurs": Lampes.Add(new Lampe { Pos = l.Pos + Vector3.up * 2f, Couleur = Pal.C("b5ff9a"), Portee = 5f, Intensite = 1.3f }); break;
                    case "porte_ville":
                    {
                        var q = Quaternion.AngleAxis(l.RotY, Vector3.up);
                        Lampes.Add(new Lampe { Pos = l.Pos + q * new Vector3(2.6f, 2.6f, -1f), Couleur = Pal.C("ffb45a"), Portee = 6f, Intensite = 2f });
                        Lampes.Add(new Lampe { Pos = l.Pos + q * new Vector3(-2.6f, 2.6f, -1f), Couleur = Pal.C("ffb45a"), Portee = 6f, Intensite = 2f });
                        break;
                    }
                    case "montagne_antre": Lampes.Add(new Lampe { Pos = l.Pos + Quaternion.AngleAxis(l.RotY, Vector3.up) * new Vector3(0, 1.5f, 5.5f), Couleur = Pal.C("ff5a1e"), Portee = 12f, Intensite = 3.5f }); break;
                }
            }
            int restantes = 30 - Lampes.Count;
            foreach (var d in Decors)
            {
                if (restantes <= 0) break;
                Color32 c; float portee, intensite;
                switch (d.Id)
                {
                    case "lave": c = Pal.C("ff6a1e"); portee = 4.5f; intensite = 2.4f; break;
                    case "cristal_braise": c = Pal.C("ff8a3a"); portee = 3.5f; intensite = 1.8f; break;
                    case "feu_follet": c = Pal.C("7ff5d0"); portee = 4f; intensite = 2f; break;
                    default: continue;
                }
                bool proche = false;
                foreach (var l in Lampes) if ((l.Pos - d.Pos).sqrMagnitude < 36f) { proche = true; break; }
                if (proche) continue;
                Lampes.Add(new Lampe { Pos = d.Pos + Vector3.up * 0.8f, Couleur = c, Portee = portee, Intensite = intensite });
                restantes--;
            }
        }
    }
}

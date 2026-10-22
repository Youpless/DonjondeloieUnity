// Les Dés du Dragon — boîte à outils d'interface (uGUI construit en code) : panneaux de bois et de parchemin,
// textes ombrés, boutons, icônes en pixel art, barres. Tous les sprites sont dessinés au lancement.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DesDuDragon.Voxel;

namespace DesDuDragon.Interface
{
    public enum Style { Bois, Parchemin, Sombre, Bouton, BoutonOr, BoutonRouge, Carte, Puce, Verre }

    public static class UIK
    {
        public static Font Police;
        static Sprite plein, contour, lueur, degrade, blanc;
        static readonly Dictionary<string, Sprite> icones = new Dictionary<string, Sprite>();

        public static readonly Color Or = new Color(0.93f, 0.76f, 0.36f);
        public static readonly Color OrSombre = new Color(0.62f, 0.45f, 0.18f);
        public static readonly Color Encre = new Color(0.17f, 0.11f, 0.07f);
        public static readonly Color Creme = new Color(0.97f, 0.93f, 0.84f);
        public static readonly Color BoisFonce = new Color(0.13f, 0.095f, 0.07f, 0.94f);
        public static readonly Color Vert = new Color(0.46f, 0.82f, 0.42f);
        public static readonly Color Rouge = new Color(0.93f, 0.38f, 0.32f);
        public static readonly Color Bleu = new Color(0.45f, 0.7f, 0.98f);
        public static readonly Color Violet = new Color(0.76f, 0.55f, 0.98f);
        public static readonly Color Gris = new Color(0.62f, 0.6f, 0.56f);

        public static void Initialiser()
        {
            if (Police != null && plein != null) return;
            icones.Clear();
            Police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            plein = Arrondi(64, 15, 0f); plein.name = "plein";
            contour = Arrondi(64, 15, 3f); contour.name = "contour";
            lueur = Radial(64); lueur.name = "lueur";
            degrade = Degrade(8, 64); degrade.name = "degrade";
            var t = new Texture2D(4, 4); var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px); t.Apply();
            blanc = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            blanc.name = "blanc";
        }

        // ------------------------------------------------------------------ sprites dessinés

        static Sprite Arrondi(int n, int rayon, float epaisseur)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, rayon, n - rayon), cy = Mathf.Clamp(y + 0.5f, rayon, n - rayon);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float a = Mathf.Clamp01(rayon - d + 0.5f);
                    if (epaisseur > 0) a *= Mathf.Clamp01(d - (rayon - epaisseur) + 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            float b = rayon + 1;
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite Radial(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite Degrade(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = new Color32(255, 255, 255, (byte)(255 * y / (h - 1)));
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite Icone(string id)
        {
            Sprite s;
            if (icones.TryGetValue(id, out s)) return s;
            int n = PixelIcones.Taille;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) px[y * n + x] = PixelIcones.Pixel(id, x, y);
            t.SetPixels32(px); t.Apply();
            s = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            s.name = "icone:" + id;
            icones[id] = s;
            return s;
        }

        public static Sprite Plein { get { return plein; } }
        public static Sprite Lueur { get { return lueur; } }
        public static Sprite Blanc { get { return blanc; } }
        public static Sprite DegradeVertical { get { return degrade; } }

        // ------------------------------------------------------------------ placement

        public static RectTransform Rect(Transform parent, string nom)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// Ancre en un point (ax, ay de 0 à 1), décalage et taille en pixels de référence (1920 × 1080).
        public static RectTransform Placer(RectTransform r, float ax, float ay, float x, float y, float w, float h, float px = 0.5f, float py = 0.5f)
        {
            r.anchorMin = r.anchorMax = new Vector2(ax, ay);
            r.pivot = new Vector2(px, py);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        /// Remplit le parent avec des marges.
        public static RectTransform Remplir(RectTransform r, float g = 0, float b = 0, float d = 0, float h = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(g, b); r.offsetMax = new Vector2(-d, -h);
            return r;
        }

        // ------------------------------------------------------------------ éléments

        public static Image Image(Transform parent, string nom, Sprite s, Color c)
        {
            var r = Rect(parent, nom);
            var im = r.gameObject.AddComponent<Image>();
            im.sprite = s;
            im.color = c;
            if (s == plein || s == contour) im.type = UnityEngine.UI.Image.Type.Sliced;
            im.raycastTarget = false;
            return im;
        }

        /// Panneau stylé : fond + liseré. Rend le RectTransform du panneau.
        public static RectTransform Panneau(Transform parent, string nom, Style st)
        {
            Color fond, bord;
            switch (st)
            {
                case Style.Parchemin: fond = new Color(0.93f, 0.87f, 0.73f, 0.98f); bord = new Color(0.45f, 0.3f, 0.16f); break;
                case Style.Sombre: fond = new Color(0.05f, 0.04f, 0.04f, 0.72f); bord = new Color(0.55f, 0.45f, 0.3f, 0.6f); break;
                case Style.Carte: fond = new Color(0.98f, 0.94f, 0.84f, 1f); bord = new Color(0.55f, 0.38f, 0.2f); break;
                case Style.Verre: fond = new Color(0.1f, 0.08f, 0.07f, 0.62f); bord = new Color(0.93f, 0.76f, 0.36f, 0.35f); break;
                case Style.Puce: fond = new Color(0.2f, 0.15f, 0.11f, 0.9f); bord = new Color(0.72f, 0.55f, 0.28f, 0.8f); break;
                default: fond = BoisFonce; bord = new Color(0.78f, 0.6f, 0.3f); break;
            }
            var im = Image(parent, nom, plein, fond);
            im.raycastTarget = true;
            var b = Image(im.transform, "Liseré", contour, bord);
            Remplir(b.rectTransform);
            if (st == Style.Bois)
            {
                var ref_ = Image(im.transform, "Reflet", degrade, new Color(1f, 0.85f, 0.6f, 0.06f));
                Remplir(ref_.rectTransform, 3, 3, 3, 3);
            }
            return im.rectTransform;
        }

        public static Text Texte(Transform parent, string nom, string s, int taille, Color c, TextAnchor ancre = TextAnchor.MiddleCenter, bool gras = false, bool ombre = true)
        {
            var r = Rect(parent, nom);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Police;
            t.text = s;
            t.fontSize = taille;
            t.color = c;
            t.alignment = ancre;
            t.fontStyle = gras ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            if (ombre)
            {
                var sh = r.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, c.grayscale > 0.5f ? 0.75f : 0.18f);
                sh.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }

        public static Button Bouton(Transform parent, string nom, string libelle, Style st, UnityAction clic, int taille = 24)
        {
            Color fond, bord, encre;
            switch (st)
            {
                case Style.BoutonOr: fond = new Color(0.9f, 0.68f, 0.27f); bord = new Color(0.45f, 0.28f, 0.1f); encre = Encre; break;
                case Style.BoutonRouge: fond = new Color(0.62f, 0.2f, 0.16f); bord = new Color(0.95f, 0.6f, 0.4f); encre = Creme; break;
                case Style.Parchemin: fond = new Color(0.98f, 0.94f, 0.84f); bord = new Color(0.55f, 0.38f, 0.2f); encre = Encre; break;
                default: fond = new Color(0.25f, 0.18f, 0.13f, 0.97f); bord = new Color(0.82f, 0.64f, 0.32f); encre = Creme; break;
            }
            var im = Image(parent, nom, plein, fond);
            im.raycastTarget = true;
            var b = Image(im.transform, "Liseré", contour, bord);
            Remplir(b.rectTransform);
            var btn = im.gameObject.AddComponent<Button>();
            btn.targetGraphic = im;
            var cb = btn.colors;
            cb.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.highlightedColor = Color.white;
            cb.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            cb.colorMultiplier = 1.15f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            if (clic != null) btn.onClick.AddListener(clic);
            btn.onClick.AddListener(() => DesDuDragon.Monde.Sons.Jouer(DesDuDragon.Monde.Son.Clic, 0.6f));
            var t = Texte(im.transform, "Libellé", libelle, taille, encre, TextAnchor.MiddleCenter, st == Style.BoutonOr, st != Style.Parchemin && st != Style.BoutonOr);
            Remplir(t.rectTransform, 10, 2, 10, 2);
            return btn;
        }

        public static Text Libelle(Button b) { return b.GetComponentInChildren<Text>(); }

        public static Image IconeImage(Transform parent, string id, float taille)
        {
            var im = Image(parent, "Icône_" + id, Icone(id), Color.white);
            im.rectTransform.sizeDelta = new Vector2(taille, taille);
            im.preserveAspect = true;
            return im;
        }

        /// Barre de jauge : fond sombre + remplissage (Image en mode « Filled »).
        public static Image Barre(Transform parent, string nom, Color c, out RectTransform cadre)
        {
            var fond = Image(parent, nom, plein, new Color(0, 0, 0, 0.55f));
            cadre = fond.rectTransform;
            var r = Image(fond.transform, "Remplissage", blanc, c);
            Remplir(r.rectTransform, 2, 2, 2, 2);
            r.type = UnityEngine.UI.Image.Type.Filled;
            r.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            r.fillOrigin = 0;
            r.fillAmount = 1f;
            return r;
        }

        public static string Hex(Color c)
        {
            Color32 k = c;
            return k.r.ToString("x2") + k.g.ToString("x2") + k.b.ToString("x2");
        }

        public static string Colorer(string s, Color c) { return "<color=#" + Hex(c) + ">" + s + "</color>"; }

        public static void Vider(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string IconeObjet(DesDuDragon.Regles.DefObjet o)
        {
            switch (o.Type)
            {
                case DesDuDragon.Regles.TypeObjet.Arme: return "epee";
                case DesDuDragon.Regles.TypeObjet.Armure: return "bouclier";
                case DesDuDragon.Regles.TypeObjet.Accessoire: return o.Effet == DesDuDragon.Regles.EffetObjet.Amulette ? "oeil" : o.Effet == DesDuDragon.Regles.EffetObjet.Bottes ? "fleche" : "couronne";
                case DesDuDragon.Regles.TypeObjet.Potion: return "fiole";
                case DesDuDragon.Regles.TypeObjet.Piege: return "piege";
                default: return o.Effet == DesDuDragon.Regles.EffetObjet.SortFoudre ? "eclair" : o.Effet == DesDuDragon.Regles.EffetObjet.SortMalediction ? "crane" : "parchemin";
            }
        }
    }
}

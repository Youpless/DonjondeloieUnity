// Les Dés du Dragon — grandes fenêtres : boutique (fiches d'objets), panneau des quêtes, rencontre d'un monstre.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DesDuDragon.Regles;
using DesDuDragon.Voxel;

namespace DesDuDragon.Interface
{
    public sealed partial class FenetreQuestion
    {
        RectTransform Fenetre(string titre, float w, float h, Joueur j, bool enBas = false)
        {
            var voile = UIK.Image(racine, "Voile", UIK.Blanc, new Color(0, 0, 0, 0.35f));
            UIK.Remplir(voile.rectTransform);
            voile.raycastTarget = true;
            fenetre = voile.rectTransform;
            if (enBas) voile.color = new Color(0, 0, 0, 0.12f);
            var f = enBas ? UIK.Placer(UIK.Panneau(fenetre, "Fenêtre", Style.Parchemin), 0.5f, 0f, 0, 26, w, h, 0.5f, 0f)
                          : UIK.Placer(UIK.Panneau(fenetre, "Fenêtre", Style.Parchemin), 0.5f, 0.5f, 0, 0, w, h);
            var bandeau = UIK.Placer(UIK.Panneau(f, "Titre", Style.Bois), 0.5f, 1f, 0, 26, Mathf.Min(w - 120, 620), 62, 0.5f, 1f);
            var t = UIK.Texte(bandeau, "Texte", titre, 30, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Remplir(t.rectTransform, 10, 0, 10, 0);
            var bande = UIK.Image(bandeau, "Couleur", UIK.Plein, Pal.Joueurs[j.Couleur]);
            bande.type = Image.Type.Sliced;
            UIK.Placer(bande.rectTransform, 0.5f, 0f, 0, 6, Mathf.Min(w - 160, 560), 5, 0.5f, 0f);
            return f;
        }

        // ------------------------------------------------------------------ boutique

        void Boutique(SQuestion q, Partie p, Joueur j)
        {
            int n = 0;
            for (int i = 0; i < q.Options.Count; i++) if (q.Objets[i] != null) n++;
            int cols = n > 12 ? 4 : 3;
            int rangs = Mathf.CeilToInt(n / (float)cols);
            float cw = cols == 4 ? 286 : 330, ch = 104;
            float w = cols * (cw + 10) + 50, h = 196 + rangs * (ch + 10);
            var f = Fenetre(q.Titre, w, h, j);
            // en-tête : or et équipement
            var entete = UIK.Texte(f, "Équipement", Equipement(j), 18, UIK.Encre, TextAnchor.UpperLeft, false, false);
            UIK.Placer(entete.rectTransform, 0, 1, 30, -52, w - 330, 50, 0, 1);
            var bourse = UIK.Texte(f, "Bourse", "", 30, new Color(0.55f, 0.36f, 0.06f), TextAnchor.MiddleRight, true, false);
            UIK.Placer(bourse.rectTransform, 1, 1, -76, -50, 220, 40, 1, 1);
            bourse.text = j.Or + " or";
            var piece = UIK.IconeImage(f, "piece", 34);
            UIK.Placer(piece.rectTransform, 1, 1, -30, -52, 34, 34, 1, 1);
            int k = 0;
            for (int i = 0; i < q.Options.Count; i++)
            {
                var o = q.Objets[i];
                if (o == null) continue;
                int col = k % cols, rang = k / cols;
                k++;
                var carte = UIK.Placer(UIK.Panneau(f, "Objet_" + o.Id, Style.Carte), 0, 1, 25 + col * (cw + 10), -112 - rang * (ch + 10), cw, ch, 0, 1);
                var ic = UIK.IconeImage(carte, UIK.IconeObjet(o), 44);
                UIK.Placer(ic.rectTransform, 0, 1, 12, -12, 44, 44, 0, 1);
                var nom = UIK.Texte(carte, "Nom", o.Nom, 19, UIK.Encre, TextAnchor.UpperLeft, true, false);
                UIK.Placer(nom.rectTransform, 0, 1, 64, -8, cw - 74, 24, 0, 1);
                nom.horizontalOverflow = HorizontalWrapMode.Overflow;
                var desc = UIK.Texte(carte, "Desc", q.Actives[i] ? o.Description : UIK.Colorer(q.Aides[i], new Color(0.62f, 0.15f, 0.1f)), 14, new Color(0.3f, 0.22f, 0.15f), TextAnchor.UpperLeft, false, false);
                UIK.Placer(desc.rectTransform, 0, 1, 64, -32, cw - 172, 60, 0, 1);
                int choix = i;
                var b = UIK.Bouton(carte, "Acheter", q.Prix[i] + " or", q.Actives[i] ? Style.BoutonOr : Style.Bouton, () => Choisir(choix), 19);
                UIK.Placer((RectTransform)b.transform, 1, 0, -10, 10, 88, 40, 1, 0);
                b.interactable = q.Actives[i];
                if (o.Prix != q.Prix[i] && q.Actives[i])
                {
                    var barre = UIK.Texte(carte, "Ancien prix", "<size=13>" + o.Prix + "</size>", 13, new Color(0.45f, 0.35f, 0.25f), TextAnchor.LowerRight, false, false);
                    UIK.Placer(barre.rectTransform, 1, 0, -102, 12, 60, 20, 1, 0);
                }
            }
            int partir = q.Codes.IndexOf("partir");
            var bp = UIK.Bouton(f, "Partir", "Partir", Style.Bouton, () => Choisir(partir), 24);
            UIK.Placer((RectTransform)bp.transform, 0.5f, 0, 0, 22, 260, 56, 0.5f, 0);
        }

        static string Equipement(Joueur j)
        {
            string arme = j.Arme != null ? j.Arme.Nom + " (+" + j.Arme.Bonus + ")" : "aucune";
            string armure = j.Armure != null ? j.Armure.Nom + " (−" + j.Armure.Bonus + ")" : "aucune";
            var acc = new List<string>(); foreach (var a in j.Accessoires) acc.Add(a.Nom);
            var sac = new List<string>(); foreach (var o in j.Sac) sac.Add(o.Nom);
            return "<b>Arme</b> " + arme + "   <b>Armure</b> " + armure + "   <b>Accessoires</b> " + (acc.Count > 0 ? string.Join(", ", acc.ToArray()) : "aucun")
                + "\n<b>Sac</b> (" + j.Sac.Count + "/" + Donnees.TailleSac + ") " + (sac.Count > 0 ? string.Join(", ", sac.ToArray()) : "vide");
        }

        // ------------------------------------------------------------------ quêtes

        void Quetes(SQuestion q, Partie p, Joueur j)
        {
            int n = 0;
            for (int i = 0; i < q.Options.Count; i++) if (q.Quetes[i] != null) n++;
            float cw = 340, chh = 300;
            float w = Mathf.Max(760, n * (cw + 16) + 60), h = 520;
            var f = Fenetre(q.Titre, w, h, j);
            var t = UIK.Texte(f, "Texte", q.Texte, 19, UIK.Encre, TextAnchor.UpperCenter, false, false);
            UIK.Placer(t.rectTransform, 0.5f, 1, 0, -54, w - 80, 40, 0.5f, 1);
            int k = 0;
            float x0 = -(n * (cw + 16) - 16) / 2f;
            for (int i = 0; i < q.Options.Count; i++)
            {
                var qu = q.Quetes[i];
                if (qu == null) continue;
                var carte = UIK.Placer(UIK.Panneau(f, "Quête" + i, Style.Carte), 0.5f, 1, x0 + k * (cw + 16) + cw / 2f, -104, cw, chh, 0.5f, 1);
                k++;
                var ic = UIK.IconeImage(carte, qu.Type == Quete.Genre.Chasser ? "epees_croisees" : "parchemin", 52);
                UIK.Placer(ic.rectTransform, 0.5f, 1, 0, -14, 52, 52, 0.5f, 1);
                var titre = UIK.Texte(carte, "Titre", qu.Titre, 21, UIK.Encre, TextAnchor.UpperCenter, true, false);
                UIK.Placer(titre.rectTransform, 0.5f, 1, 0, -72, cw - 30, 52, 0.5f, 1);
                var texte = UIK.Texte(carte, "Texte", qu.Texte, 16, new Color(0.3f, 0.22f, 0.15f), TextAnchor.UpperCenter, false, false);
                UIK.Placer(texte.rectTransform, 0.5f, 1, 0, -128, cw - 36, 80, 0.5f, 1);
                var rec = UIK.Texte(carte, "Récompense", "<b>" + qu.RecompenseXP + " XP</b> · <b>" + qu.RecompenseOr + " or</b>", 19, new Color(0.5f, 0.32f, 0.05f), TextAnchor.MiddleCenter, false, false);
                UIK.Placer(rec.rectTransform, 0.5f, 0, 0, 72, cw - 30, 28, 0.5f, 0);
                int choix = i;
                var b = UIK.Bouton(carte, "Accepter", "Accepter", Style.BoutonOr, () => Choisir(choix), 21);
                UIK.Placer((RectTransform)b.transform, 0.5f, 0, 0, 16, 200, 48, 0.5f, 0);
                int caseCible = qu.CaseCible;
                var sv = carte.gameObject.AddComponent<Survol>();
                sv.Entree = () => { if (SurvolCase != null) SurvolCase(caseCible); };
                sv.Sortie = () => { if (SurvolCase != null) SurvolCase(-1); };
            }
            int non = q.Codes.IndexOf("non");
            var bn = UIK.Bouton(f, "Aucune", q.Options[non], Style.Bouton, () => Choisir(non), 22);
            UIK.Placer((RectTransform)bn.transform, 0.5f, 0, 0, 22, 280, 54, 0.5f, 0);
        }

        // ------------------------------------------------------------------ monstre

        void Monstre(SQuestion q, Partie p, Joueur j)
        {
            var m = q.Monstre;
            var f = Fenetre(q.Titre, 780, 400, j, true);
            var ic = UIK.IconeImage(f, m.Boss ? "dragon" : "crane", 64);
            UIK.Placer(ic.rectTransform, 0, 1, 40, -60, 64, 64, 0, 1);
            var desc = UIK.Texte(f, "Description", "<i>" + m.Description + "</i>", 20, UIK.Encre, TextAnchor.UpperLeft, false, false);
            UIK.Placer(desc.rectTransform, 0, 1, 124, -62, 610, 60, 0, 1);
            int bonus = (j.Classe == ClasseHeros.Guerrier || j.Classe == ClasseHeros.Mage ? 1 : 0) + j.Benediction;
            double chance = IA.ChanceMonstre(j.Attaque + bonus, m.Force);
            int degats = Math.Max(1, m.Degats - j.Defense);
            var stats = UIK.Texte(f, "Stats",
                "<b>Force</b> " + m.Force + "      <b>Dégâts</b> " + m.Degats + " (vous en subiriez " + degats + ")      <b>Butin</b> " + m.Or + " or · " + m.XP + " XP\n"
                + "<b>Votre attaque</b> " + j.Attaque + (bonus > 0 ? " +" + bonus : "") + " + 1D6   contre   <b>sa force</b> " + m.Force + " + 1D6\n"
                + "Vos chances : <b>" + Mathf.RoundToInt((float)chance * 100) + " %</b>" + (j.PV <= degats ? UIK.Colorer("   — une défaite vous mettrait K.O. !", new Color(0.7f, 0.1f, 0.05f)) : ""),
                20, UIK.Encre, TextAnchor.UpperLeft, false, false);
            UIK.Placer(stats.rectTransform, 0, 1, 40, -150, 700, 110, 0, 1);
            float x = q.Options.Count > 1 ? -150 : 0;
            for (int i = 0; i < q.Options.Count; i++)
            {
                int k = i;
                var b = UIK.Bouton(f, q.Codes[i], q.Options[i], q.Codes[i] == "combat" ? Style.BoutonRouge : Style.Bouton, () => Choisir(k), 26);
                UIK.Placer((RectTransform)b.transform, 0.5f, 0, x + i * 300, 30, 260, 62, 0.5f, 0);
                var aide = UIK.Texte(f, "Aide" + i, q.Aides[i] ?? "", 15, new Color(0.35f, 0.25f, 0.15f), TextAnchor.UpperCenter, false, false);
                UIK.Placer(aide.rectTransform, 0.5f, 0, x + i * 300, 98, 290, 40, 0.5f, 0);
            }
        }
    }
}

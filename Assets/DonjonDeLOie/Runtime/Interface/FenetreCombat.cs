// Donjon de l'Oie — le tableau d'un combat : les deux adversaires, leur attaque, leurs dés, les totaux et le verdict.
using UnityEngine;
using UnityEngine.UI;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Interface
{
    public sealed class FenetreCombat
    {
        readonly RectTransform racine;
        RectTransform p;
        Text deA, deB, totA, totB, verdict;
        Image fondA, fondB;

        public FenetreCombat(RectTransform racine) { this.racine = racine; }

        public void Cacher() { if (p != null) Object.Destroy(p.gameObject); p = null; }

        Text Cote(string nom, Color c, string attaque, float x, out Text de, out Text total, out Image fond)
        {
            var bloc = UIK.Placer(UIK.Rect(p, "Côté"), 0.5f, 0.5f, x, 0, 330, 180);
            var n = UIK.Texte(bloc, "Nom", nom, 26, c, TextAnchor.UpperCenter, true);
            UIK.Placer(n.rectTransform, 0.5f, 1, 0, -8, 330, 32, 0.5f, 1);
            var a = UIK.Texte(bloc, "Attaque", attaque, 18, UIK.Creme, TextAnchor.UpperCenter, false);
            UIK.Placer(a.rectTransform, 0.5f, 1, 0, -42, 330, 26, 0.5f, 1);
            fond = UIK.Image(bloc, "Dé", UIK.Plein, new Color(0.95f, 0.91f, 0.82f));
            fond.type = Image.Type.Sliced;
            UIK.Placer(fond.rectTransform, 0.5f, 0, -60, 16, 74, 74, 0.5f, 0);
            de = UIK.Texte(fond.transform, "Valeur", "?", 44, UIK.Encre, TextAnchor.MiddleCenter, true, false);
            UIK.Remplir(de.rectTransform);
            total = UIK.Texte(bloc, "Total", "", 46, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Placer(total.rectTransform, 0.5f, 0, 56, 16, 120, 74, 0.5f, 0);
            return n;
        }

        public void Montrer(SCombat c, Partie partie)
        {
            Cacher();
            p = UIK.Placer(UIK.Panneau(racine, "Combat", Style.Bois), 0.5f, 1f, 0, -90, 860, 196, 0.5f, 1f);
            var ja = partie.Joueurs[c.Attaquant];
            string atkA = "Attaque " + c.AtkA + (c.BonusA > 0 ? " + " + c.BonusA + (c.RaisonBonusA != null ? " (" + c.RaisonBonusA + ")" : "") : "") + " + 1D" + c.FacesDe;
            Cote(ja.Nom, Pal.Joueurs[ja.Couleur], atkA, -230, out deA, out totA, out fondA);
            string nomB; Color colB; string atkB;
            if (c.Duel)
            {
                var jb = partie.Joueurs[c.Defenseur];
                nomB = jb.Nom; colB = Pal.Joueurs[jb.Couleur];
                atkB = "Attaque " + c.AtkB + (c.BonusB > 0 ? " + " + c.BonusB : "") + " + 1D3";
            }
            else
            {
                nomB = c.Monstre.Nom; colB = new Color(1f, 0.45f, 0.38f);
                atkB = "Force " + c.AtkB + " + 1D6";
            }
            Cote(nomB, colB, atkB, 230, out deB, out totB, out fondB);
            verdict = UIK.Texte(p, "VS", c.Duel ? (c.Arene ? "ARÈNE" : c.Embuscade ? "EMBUSCADE" : "DUEL") : "COMBAT", 30, new Color(1f, 0.6f, 0.35f), TextAnchor.MiddleCenter, true);
            UIK.Placer(verdict.rectTransform, 0.5f, 0.5f, 0, 0, 160, 120);
        }

        public void Des(SCombat c)
        {
            if (p == null) return;
            deA.text = c.DeA.ToString(); deB.text = c.DeB.ToString();
            totA.text = "= " + c.TotalA; totB.text = "= " + c.TotalB;
        }

        public void Verdict(SCombat c)
        {
            if (p == null) return;
            if (c.Egalite)
            {
                verdict.text = "ÉGALITÉ";
                verdict.color = UIK.Creme;
                return;
            }
            bool a = c.AttaquantGagne;
            totA.color = a ? UIK.Vert : UIK.Rouge;
            totB.color = a ? UIK.Rouge : UIK.Vert;
            fondA.color = a ? new Color(0.75f, 0.95f, 0.65f) : new Color(0.95f, 0.7f, 0.65f);
            fondB.color = a ? new Color(0.95f, 0.7f, 0.65f) : new Color(0.75f, 0.95f, 0.65f);
            verdict.text = a ? "◄ VICTOIRE" : "VICTOIRE ►";
            verdict.color = UIK.Or;
            verdict.fontSize = 26;
        }
    }
}

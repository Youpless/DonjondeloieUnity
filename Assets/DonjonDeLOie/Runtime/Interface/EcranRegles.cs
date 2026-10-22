// Donjon de l'Oie — les règles du jeu, en six pages, et l'écran de victoire.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Interface
{
    public sealed class EcranRegles
    {
        public RectTransform R;
        Text titre, corps;
        readonly List<Button> onglets = new List<Button>();

        static readonly string[] Titres = { "Le but", "Un tour", "Combats", "Les cases", "Nuit et quêtes", "Objets", "Les héros" };

        static string[] Pages()
        {
            var o = new List<string>();
            o.Add("<b>Premier au niveau 10, premier couronné</b> (le niveau à atteindre se règle dans le salon)"
                + (Donnees.Dragonnoie != null ? " — <b>à moins qu'un aventurier ne terrasse le Dragonnoie</b>." : ".") + "\n\n"
                + "Jusqu'à six aventuriers parcourent l'île de Valcreux, case par case, sur une grande boucle qui traverse le Bois des Murmures, "
                + "le Marais Putride, les Cryptes d'Ombrelune et les Pics de Braise. Trois raccourcis coupent la boucle : plus courts, "
                + "mais plus dangereux — le dernier passe par l'antre d'Ignathor, le dragon rouge.\n\n"
                + "On gagne de l'<b>expérience</b> (" + Donnees.XPParNiveau + " XP font un niveau) en battant des monstres, en accomplissant des quêtes, en "
                + "remportant des duels et à chaque passage par Valcreux (+1 XP et +" + Donnees.SoldeVille + " or).\n\n"
                + "On peut aussi <b>perdre des niveaux</b> : en perdant un duel contre un autre joueur, sur une case maudite, par une rune "
                + "d'oubli ou par un parchemin de malédiction. Rien n'est jamais joué !\n\n"
                + "Les places vides sont tenues par des bots, et plusieurs humains peuvent jouer sur le même ordinateur : on se passe la souris."
                + TexteDragonnoie());
            o.Add("<b>À votre tour</b>, avant de bouger, vous pouvez utiliser autant d'objets de votre sac que vous voulez : boire une potion, "
                + "poser un piège, lire un parchemin. Puis vous faites l'une de ces deux choses :\n\n"
                + "<b>• Lancer le dé</b> (1D6) et avancer d'autant de cases. Aux embranchements, vous choisissez votre chemin. "
                + "Si vous passez sur un piège ou dans une embuscade, votre course s'arrête là.\n\n"
                + "<b>• Tendre une embuscade</b> : vous restez sur place, et votre case plus une case voisine de votre choix deviennent un "
                + "piège vivant jusqu'à votre prochain tour. Le premier joueur qui y entre est arrêté et doit vous affronter en duel, "
                + "et vous avez +2 grâce à la surprise.\n\n"
                + "<b>Arrivé sur une case</b>, si d'autres aventuriers s'y trouvent, vous pouvez en défier un. Puis la case fait son effet.\n\n"
                + "<b>K.O.</b> : à 0 PV, vous retournez à Valcreux avec la moitié de vos PV, et vous perdez la moitié de votre or.");
            o.Add("<b>Duel entre joueurs</b> : chacun lance son <b>attaque + 1D3</b>. Le plus haut l'emporte (en cas d'égalité, on relance ; "
                + "à la troisième égalité, le défenseur tient bon). Le perdant <b>perd un niveau</b> — deux si l'écart atteint 5 — et le "
                + "vainqueur lui prend 5 or par point d'écart et gagne 1 XP. Le perdant est ensuite protégé des défis jusqu'à la fin de son prochain tour.\n\n"
                + "<b>Contre un monstre</b> : votre <b>attaque + 1D6</b> contre sa <b>force + 1D6</b> ; l'égalité vous sourit. Victoire : son or et son "
                + "expérience. Défaite : il vous inflige ses dégâts, moins votre armure. Vous pouvez toujours fuir (vous reculez d'une case).\n\n"
                + "<b>Votre attaque</b> = celle de votre classe + votre arme. Votre <b>défense</b> (classe + armure) réduit les dégâts des monstres et de la foudre.\n\n"
                + "<b>Ignathor</b>, le dragon rouge, garde son antre au cœur de l'île : force 12, dégâts 7, mais 80 or et 8 XP pour qui le terrasse.");
            var cases = new System.Text.StringBuilder();
            for (int i = 0; i < Donnees.NomsTypesCase.Length; i++)
            {
                Color c = DonjonDeLOie.Monde.IleModele.CouleurType((TypeCase)i);
                cases.Append(UIK.Colorer("■ ", c) + "<b>" + Donnees.NomsTypesCase[i] + "</b> — " + (i == 0 ? "La cité de départ : boutique complète, soins gratuits et quêtes. +" + Donnees.SoldeVille + " or et +" + Donnees.XPVille + " XP de renommée à chaque passage." : Donnees.AidesTypesCase[i]) + "\n");
            }
            cases.Append("\n<b>Quêtes</b> à Valcreux, dans les tavernes ou auprès d'un ermite : voir l'onglet « Nuit et quêtes ».");
            o.Add(cases.ToString());
            o.Add(TexteNuit() + TexteQuetes());
            var obj = new System.Text.StringBuilder();
            obj.Append("<b>Armes</b> : ");
            Lister(obj, TypeObjet.Arme); obj.Append("\n<b>Armures</b> : "); Lister(obj, TypeObjet.Armure);
            obj.Append("\n<b>Accessoires</b> (deux au plus) : "); Lister(obj, TypeObjet.Accessoire);
            obj.Append("\n<b>Potions</b> : "); Lister(obj, TypeObjet.Potion);
            obj.Append("\n<b>Pièges</b> (sur votre case ou une case voisine, trois au plus ; invisibles pour les autres) : "); Lister(obj, TypeObjet.Piege);
            obj.Append("\n<b>Parchemins</b> (sur n'importe quel joueur) : "); Lister(obj, TypeObjet.Parchemin);
            obj.Append("\n\nLe sac contient " + Donnees.TailleSac + " objets. L'<b>amulette de garde</b> bloque la prochaine malédiction, rune ou sort, puis se brise. "
                + "Les marchands changent d'étal tous les trois tours ; Valcreux vend de tout, y compris les pièces légendaires.");
            o.Add(obj.ToString());
            var h = new System.Text.StringBuilder();
            foreach (var d in Donnees.Classes)
                h.Append("<b>" + d.Nom + "</b>  (attaque " + d.Attaque + ", défense " + d.Defense + ", " + d.Vie + " PV)\n" + d.Passif + " <i>" + d.Description + "</i>\n\n");
            o.Add(h.ToString());
            return o.ToArray();
        }

        /// Le Dragonnoie, avec les valeurs du catalogue (vide s'il est désactivé).
        static string TexteDragonnoie()
        {
            var m = Donnees.Dragonnoie;
            if (m == null) return "";
            int n = System.Math.Max(1, Donnees.Reglages.ToursEntreEnvols);
            return "\n\n<b>Le Dragonnoie</b>, mi-oie, mi-dragon, se pose au hasard sur l'île. Au début d'un tour de table sur " + n
                + ", il prend une grande envolée vers une autre case et <b>défie tous ceux qui s'y trouvent</b>. Si vous <b>passez</b> sur sa case, "
                + "vous choisissez : l'attaquer (votre course s'arrête là) ou continuer votre route. Si vous vous <b>arrêtez</b> sur sa case, il faut l'affronter. "
                + "Combat comme contre un monstre (force " + m.Force + ") : vaincu, il vous arrache <b>tout votre or</b> (jamais de niveau) ; "
                + "vainqueur, <b>vous gagnez la partie</b>.";
        }

        /// La nuit et ses événements, selon les réglages du catalogue.
        static string TexteNuit()
        {
            var r = Donnees.Reglages;
            if (!r.NuitsActives) return "<b>La nuit</b> ne tombe jamais dans cette partie (réglage du catalogue).\n\n";
            var sb = new System.Text.StringBuilder();
            int n = System.Math.Max(1, r.ToursEntreNuits);
            sb.Append("<b>La nuit tombe</b> à la fin de chaque " + (n == 1 ? "" : n + "e ") + "tour de table (un encart sous la chronique dit "
                + "quand) ; un seul événement l'occupe, tiré parmi ceux qui peuvent avoir lieu :\n");
            if (r.NuitVoleur) sb.Append("  • <b>Le voleur</b> : un aventurier se fait voler la moitié de son or.\n");
            if (r.NuitTrouDeVer) sb.Append("  • <b>Le trou de ver</b> : deux aventuriers sur deux cases différentes échangent leur place.\n");
            if (r.NuitBonneNuit) sb.Append("  • <b>La bonne nuit</b> : un aventurier dort bien et gagne un niveau (jamais celui de la victoire).\n");
            if (r.NuitArene) sb.Append("  • <b>L'arène du diable</b> : tous s'y affrontent en duels, dans un ordre tiré au sort ; le vainqueur reste et affronte "
                + "le suivant. Chaque défaite coûte un niveau ; le dernier vainqueur gagne un niveau (même celui de la victoire) et " + r.OrArene + " or.\n");
            return sb.Append("\n").ToString();
        }

        static string TexteQuetes()
        {
            return "<b>Les quêtes</b> — une seule à la fois ; en prendre une autre abandonne la première. À Valcreux, dans les tavernes ou auprès d'un "
                + "ermite, jusqu'à quatre propositions : <b>un lieu</b> à atteindre (y passer suffit), <b>une chasse</b> ou des ennemis à vaincre, et deux "
                + "parmi <b>le guet-apens</b> (votre embuscade surprend un aventurier), <b>le défi</b> (provoquer en duel l'aventurier désigné, gagner ou perdre) "
                + "et <b>l'avis de recherche</b>.\n\n"
                + "<b>L'avis de recherche</b> se joue dans la ville qu'il nomme (Valcreux sur la carte d'origine) : dès que vous y êtes avec cette quête, "
                + "même ramené par un parchemin de rappel, le marché apparaît en diorama. L'évadé se cache "
                + "dans la foule, derrière des caisses, des tonneaux ou des sacs : seul le haut de sa tête dépasse. Comparez les visages à son portrait "
                + "(gare aux sosies !) et <b>cliquez sur lui</b> : vous avez " + Donnees.Reglages.DureeEvade + " secondes, une erreur en coûte 3, la molette "
                + "zoome. Sinon, il s'enfuit et la quête est perdue.";
        }

        static void Lister(System.Text.StringBuilder sb, TypeObjet t)
        {
            var l = new List<string>();
            foreach (var o in Donnees.Objets) if (o.Type == t) l.Add(o.Nom + " (" + o.Prix + " or" + (o.Bonus > 0 && (t == TypeObjet.Arme) ? ", +" + o.Bonus : "") + ")");
            sb.Append(string.Join(", ", l.ToArray()) + ".");
        }

        public EcranRegles(RectTransform racine, Action fermer)
        {
            R = UIK.Remplir(UIK.Rect(racine, "Règles"));
            var voile = UIK.Image(R, "Voile", UIK.Blanc, new Color(0, 0, 0, 0.5f));
            UIK.Remplir(voile.rectTransform);
            voile.raycastTarget = true;
            var p = UIK.Placer(UIK.Panneau(R, "Panneau", Style.Parchemin), 0.5f, 0.5f, 0, 0, 1320, 860);
            var bandeau = UIK.Placer(UIK.Panneau(p, "Titre", Style.Bois), 0.5f, 1f, 0, 28, 600, 66, 0.5f, 1f);
            titre = UIK.Texte(bandeau, "Texte", "Règles du jeu", 34, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Remplir(titre.rectTransform);
            var pages = Pages();
            for (int i = 0; i < Titres.Length; i++)
            {
                int k = i;
                var b = UIK.Bouton(p, "Onglet" + i, Titres[i], Style.Bouton, () => Page(k, pages), 20);
                UIK.Placer((RectTransform)b.transform, 0.5f, 1f, -540 + i * 180, -64, 170, 50, 0.5f, 1f);
                onglets.Add(b);
            }
            corps = UIK.Texte(p, "Corps", "", 21, UIK.Encre, TextAnchor.UpperLeft, false, false);
            UIK.Placer(corps.rectTransform, 0.5f, 1f, 0, -136, 1210, 610, 0.5f, 1f);
            corps.lineSpacing = 1.12f;
            var bf = UIK.Bouton(p, "Fermer", "Fermer", Style.BoutonOr, () => fermer(), 24);
            UIK.Placer((RectTransform)bf.transform, 0.5f, 0f, 0, 26, 260, 58, 0.5f, 0f);
            Page(0, pages);
            Visible(false);
        }

        void Page(int i, string[] pages)
        {
            corps.text = pages[i];
            for (int k = 0; k < onglets.Count; k++)
                onglets[k].GetComponent<Image>().color = k == i ? new Color(0.92f, 0.7f, 0.3f) : new Color(0.25f, 0.18f, 0.13f, 0.97f);
        }

        public bool Ouvert { get { return R.gameObject.activeSelf; } }
        public void Visible(bool v) { R.gameObject.SetActive(v); if (v) R.SetAsLastSibling(); }
    }

    public sealed class EcranVictoire
    {
        public RectTransform R;

        public EcranVictoire(RectTransform racine, Partie p, Action rejouer, Action menu)
        {
            R = UIK.Remplir(UIK.Rect(racine, "Victoire"));
            var voile = UIK.Image(R, "Voile", UIK.Blanc, new Color(0, 0, 0, 0.45f));
            UIK.Remplir(voile.rectTransform);
            voile.raycastTarget = true;
            var g = p.Joueurs[p.Gagnant];
            Color cg = Pal.Joueurs[g.Couleur];
            var lueur = UIK.Image(R, "Lueur", UIK.Lueur, new Color(cg.r, cg.g, cg.b, 0.35f));
            UIK.Placer(lueur.rectTransform, 0.5f, 1f, 0, -40, 1200, 300, 0.5f, 1f);
            var t = UIK.Texte(R, "Titre", "VICTOIRE !", 96, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Placer(t.rectTransform, 0.5f, 1f, 0, -70, 1200, 120, 0.5f, 1f);
            var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.25f, 0.1f, 0.02f); ol.effectDistance = new Vector2(3, -3);
            string exploit = p.VaincuDragonnoie ? "terrasse le Dragonnoie" : "atteint le niveau " + g.Niveau;
            var n = UIK.Texte(R, "Gagnant", g.Nom + ", " + g.Def.Nom.ToLowerInvariant() + ", " + exploit + " et devient la légende de Valcreux.", 32, cg, TextAnchor.MiddleCenter, true);
            UIK.Placer(n.rectTransform, 0.5f, 1f, 0, -196, 1600, 50, 0.5f, 1f);
            n.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            var tab = UIK.Placer(UIK.Panneau(R, "Tableau", Style.Bois), 0.5f, 0.5f, 0, -60, 1260, 120 + p.Joueurs.Count * 56);
            string[] cols = { "Aventurier", "Niveau", "Monstres", "Duels", "Quêtes", "Or gagné", "Pièges", "K.O." };
            float[] xs = { 40, 420, 560, 700, 820, 940, 1070, 1180 };
            for (int c = 0; c < cols.Length; c++)
            {
                var h = UIK.Texte(tab, "Col" + c, cols[c], 20, UIK.Or, c == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, true);
                UIK.Placer(h.rectTransform, 0, 1, xs[c], -40, c == 0 ? 360 : 120, 30, c == 0 ? 0 : 0.5f, 0.5f);
            }
            var tri = new List<Joueur>(p.Joueurs);
            tri.Sort((a, b) => a == g ? -1 : b == g ? 1 : b.Niveau != a.Niveau ? b.Niveau.CompareTo(a.Niveau) : b.XP.CompareTo(a.XP));
            for (int i = 0; i < tri.Count; i++)
            {
                var j = tri[i];
                float y = -92 - i * 56;
                string[] v = { j.Nom + "  <size=16>(" + j.Def.Nom + (j.Humain ? "" : ", bot") + ")</size>", j.Niveau.ToString(), j.MonstresTues.ToString(), j.DuelsGagnes + " / " + (j.DuelsGagnes + j.DuelsPerdus), j.QuetesFinies.ToString(), j.OrGagne.ToString(), j.PiegesDeclenches.ToString(), j.KO.ToString() };
                for (int c = 0; c < v.Length; c++)
                {
                    var tx = UIK.Texte(tab, "V" + i + "_" + c, v[c], 22, c == 0 ? (Color)Pal.Joueurs[j.Couleur] : UIK.Creme, c == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, c == 0);
                    UIK.Placer(tx.rectTransform, 0, 1, xs[c], y, c == 0 ? 360 : 120, 40, c == 0 ? 0 : 0.5f, 0.5f);
                }
            }
            var b1 = UIK.Bouton(R, "Rejouer", "Rejouer", Style.BoutonOr, () => rejouer(), 28);
            UIK.Placer((RectTransform)b1.transform, 0.5f, 0f, -170, 60, 300, 70, 0.5f, 0f);
            var b2 = UIK.Bouton(R, "Menu", "Menu principal", Style.Bouton, () => menu(), 26);
            UIK.Placer((RectTransform)b2.transform, 0.5f, 0f, 170, 60, 300, 70, 0.5f, 0f);
        }

        public void Detruire() { if (R != null) UnityEngine.Object.Destroy(R.gameObject); }
    }
}

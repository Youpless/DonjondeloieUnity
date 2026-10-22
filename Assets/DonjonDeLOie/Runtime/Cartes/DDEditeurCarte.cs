// Donjon de l'Oie — la racine d'une carte dans la scène de l'éditeur : ses réglages, sa case de départ, et la conversion
// des marqueurs (enfants DDCaseMarqueur et DDDecorMarqueur) en une carte de jeu (CarteDef).
// En jeu, DDJeu lit la carte de cette racine s'il en trouve une dans la scène, puis la masque.
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Cartes
{
    [DisallowMultipleComponent]
    public sealed class DDEditeurCarte : MonoBehaviour
    {
        [Tooltip("Le nom de la carte, affiché dans le salon.")]
        public string NomCarte = "Nouvelle carte";
        [TextArea] public string Description = "";
        [Tooltip("La case de départ : les pions y commencent ; on y gagne de l'or et de l'XP à chaque passage.")]
        public DDCaseMarqueur Depart;
        [Tooltip("Niveau à atteindre proposé dans le salon pour cette carte.")]
        [Range(3, 10)] public int NiveauConseille = 10;

        [Header("Île")]
        [Tooltip("Largeur de terre autour du chemin, en mètres.")]
        [Range(4f, 30f)] public float Marge = 12f;
        [Tooltip("Arbres, rochers, fleurs… semés selon les zones.")]
        public bool DecorAutomatique = true;
        [Range(0f, 2f)] public float DensiteDecor = 1f;
        [Tooltip("Bâtiments posés selon le type des cases (taverne, chapelle, échoppe…), sauf choix contraire d'une case.")]
        public bool LieuxAutomatiques = true;
        [Tooltip("Change le relief et le décor semé.")]
        public int Graine = 7;

        public List<DDCaseMarqueur> Cases()
        {
            var l = new List<DDCaseMarqueur>(GetComponentsInChildren<DDCaseMarqueur>(true));
            return l;
        }

        /// La carte de jeu décrite par les marqueurs. Les positions sont celles de la scène (en mètres, sur le plan horizontal).
        public CarteDef Exporter()
        {
            var cases = Cases();
            var d = new CarteDef
            {
                Nom = NomCarte, Marge = Marge, DecorAutomatique = DecorAutomatique, DensiteDecor = DensiteDecor,
                LieuxAutomatiques = LieuxAutomatiques, Graine = Graine
            };
            var index = new Dictionary<DDCaseMarqueur, int>();
            for (int i = 0; i < cases.Count; i++) index[cases[i]] = i;
            for (int i = 0; i < cases.Count; i++)
            {
                var m = cases[i];
                var p = m.transform.position;
                var k = new CaseCarte
                {
                    Nom = m.NomLieu ?? "", Type = m.Type, Zone = m.Zone, X = p.x, Z = p.z, Voie = m.Voie ?? "",
                    Mode = m.Mode, Lieu = m.Lieu ?? ""
                };
                foreach (var s in m.Suivants) { int j; if (s != null && index.TryGetValue(s, out j) && !k.Suivants.Contains(j)) k.Suivants.Add(j); }
                foreach (var e in m.Evenements) if (!string.IsNullOrEmpty(e)) k.Evenements.Add(e.Trim());
                d.Cases.Add(k);
            }
            int dep;
            d.Depart = Depart != null && index.TryGetValue(Depart, out dep) ? dep : 0;
            foreach (var dm in GetComponentsInChildren<DDDecorMarqueur>(true))
            {
                var p = dm.transform.position;
                d.Decors.Add(new DecorPose { Id = dm.Id, Graine = dm.Graine, X = p.x, Z = p.z, RotY = dm.transform.eulerAngles.y, Echelle = dm.transform.localScale.x });
            }
            return d;
        }

        /// Crée les marqueurs d'une carte de jeu sous cette racine (cases puis décors). Rend les objets créés,
        /// pour que l'éditeur puisse les inscrire dans l'historique d'annulation.
        public List<GameObject> Importer(CarteDef d)
        {
            var crees = new List<GameObject>();
            NomCarte = string.IsNullOrEmpty(d.Nom) ? NomCarte : d.Nom;
            Marge = d.Marge; DecorAutomatique = d.DecorAutomatique; DensiteDecor = d.DensiteDecor;
            LieuxAutomatiques = d.LieuxAutomatiques; Graine = d.Graine;
            var cases = Dossier("Cases");
            var marqueurs = new List<DDCaseMarqueur>();
            for (int i = 0; i < d.Cases.Count; i++)
            {
                var k = d.Cases[i];
                var go = new GameObject(NomMarqueur(i, k.Type, k.Nom));
                go.transform.SetParent(cases, false);
                go.transform.position = new Vector3(k.X, 0, k.Z);
                var m = go.AddComponent<DDCaseMarqueur>();
                m.Type = k.Type; m.Zone = k.Zone; m.NomLieu = k.Nom ?? ""; m.Voie = k.Voie ?? ""; m.Mode = k.Mode;
                m.Evenements = new List<string>(k.Evenements); m.Lieu = k.Lieu ?? "";
                marqueurs.Add(m); crees.Add(go);
            }
            for (int i = 0; i < d.Cases.Count; i++)
                foreach (int j in d.Cases[i].Suivants) if (j >= 0 && j < marqueurs.Count) marqueurs[i].Suivants.Add(marqueurs[j]);
            if (d.Depart >= 0 && d.Depart < marqueurs.Count) Depart = marqueurs[d.Depart];
            if (d.Decors.Count > 0)
            {
                var decors = Dossier("Décors");
                foreach (var dp in d.Decors)
                {
                    var go = new GameObject("Décor " + dp.Id);
                    go.transform.SetParent(decors, false);
                    go.transform.position = new Vector3(dp.X, 0, dp.Z);
                    go.transform.rotation = Quaternion.Euler(0, dp.RotY, 0);
                    go.transform.localScale = Vector3.one * (dp.Echelle <= 0 ? 1f : dp.Echelle);
                    var m = go.AddComponent<DDDecorMarqueur>();
                    m.Id = dp.Id; m.Graine = dp.Graine;
                    crees.Add(go);
                }
            }
            return crees;
        }

        /// Le sous-dossier (objet vide) « Cases » ou « Décors » de la racine, créé au besoin.
        public Transform Dossier(string nom)
        {
            var t = transform.Find(nom);
            if (t != null) return t;
            var go = new GameObject(nom);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        public static string NomMarqueur(int i, TypeCase t, string nom)
        {
            return "Case " + (i + 1).ToString("00") + " · " + (string.IsNullOrEmpty(nom) ? Donnees.NomsTypesCase[(int)t] : nom);
        }
    }
}

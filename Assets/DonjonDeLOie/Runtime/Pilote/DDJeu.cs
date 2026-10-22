// Donjon de l'Oie — le chef d'orchestre : il bâtit le monde et l'interface, mène les écrans (titre, salon, partie,
// victoire) et fait jouer la partie en traduisant chaque signal des règles en animation, son et affichage.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;
using DonjonDeLOie.Monde;
using DonjonDeLOie.Interface;

namespace DonjonDeLOie.Pilote
{
    public sealed partial class DDJeu : MonoBehaviour
    {
        [Tooltip("Carte jouée par défaut. Vide : la carte d'origine (ou celle de l'éditeur de cartes présent dans la scène).")]
        public DonjonDeLOie.Cartes.DDCarte Carte;

        enum Etat { Chargement, Titre, Salon, Partie, Victoire }
        Etat etat = Etat.Chargement;

        Transform monde, transitoires;
        CameraRig cam;
        IleModele modele;
        IleVue ile;
        Etincelles etincelles, fumees;
        Canvas canvas;
        RectTransform ui;
        Hud hud;
        FenetreQuestion fq;
        FenetreCombat fc;
        EcranTitre titre;
        EcranSalon salon;
        EcranRegles regles;
        MenuPause pause;
        EcranVictoire victoire;
        RectTransform boutonsJeu;
        Text chargement;

        Partie partie;
        IA ia;
        List<ReglageJoueur> derniersReglages;
        readonly Dictionary<int, PionVue> pions = new Dictionary<int, PionVue>();
        Coroutine boucle;
        bool botsRapides;

        void Awake()
        {
            Application.targetFrameRate = 60;
            DonjonDeLOie.Cartes.DDCatalogue.Charger();   // le catalogue de l'éditeur, s'il existe
            UIK.Initialiser();
            monde = new GameObject("Monde").transform;
            cam = CameraRig.Creer(transform);
            Ambiance.Installer(monde, cam.Cam);
            Sons.Installer(cam.gameObject);
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                var mod = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                mod.AssignDefaultActions();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
            var cg = new GameObject("Interface", typeof(RectTransform));
            canvas = cg.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var sc = cg.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            cg.AddComponent<GraphicRaycaster>();
            ui = (RectTransform)cg.transform;
            chargement = UIK.Texte(ui, "Chargement", "Donjon de l'Oie\n<size=26>L'île se dessine…</size>", 56, UIK.Or, TextAnchor.MiddleCenter, true);
            UIK.Remplir(chargement.rectTransform);
            cam.Cam.backgroundColor = new Color(0.12f, 0.09f, 0.07f);
        }

        IEnumerator Start()
        {
            yield return null;
            yield return null;
            PreparerCartes();
            ConstruireIle(carteParDefaut);
            transitoires = new GameObject("Transitoires").transform;
            transitoires.SetParent(monde, false);
            PreparerArene();
            etincelles = Etincelles.Creer(monde, true);
            fumees = Etincelles.Creer(monde, false);
            cam.Cam.backgroundColor = Ambiance.Horizon;
            hud = Hud.Creer(ui, canvas);
            hud.Camera = cam;
            hud.MontrerOie = MontrerOie;
            hud.Visible(false);
            fc = new FenetreCombat(ui);
            fq = new FenetreQuestion(ui);
            fq.SurvolCase = c => ile.Survoler(c);
            BoutonsJeu();
            titre = new EcranTitre(ui, AllerSalon, () => regles.Visible(true), Quitter);
            salon = new EcranSalon(ui, AllerTitre, Lancer);
            salon.DefinirCartes(NomsCartes(), carteParDefaut, NiveauxConseilles());
            salon.Visible(false);
            regles = new EcranRegles(ui, () => regles.Visible(false));
            pause = new MenuPause(ui, () => Pause(false), () => regles.Visible(true), () => { Pause(false); AllerTitre(); });
            Destroy(chargement.gameObject);
            Sons.Musique(true);
            AllerTitre();
        }

        void BoutonsJeu()
        {
            boutonsJeu = UIK.Placer(UIK.Rect(ui, "Boutons du jeu"), 1f, 0f, -16, 16, 210, 196, 1f, 0f);
            var b1 = UIK.Bouton(boutonsJeu, "Plateau", "Tout le plateau", Style.Bouton, () => cam.BasculerEnsemble(), 19);
            UIK.Placer((RectTransform)b1.transform, 1, 0, 0, 132, 210, 54, 1, 0);
            var b2 = UIK.Bouton(boutonsJeu, "Règles", "Règles", Style.Bouton, () => regles.Visible(true), 19);
            UIK.Placer((RectTransform)b2.transform, 1, 0, 0, 66, 210, 54, 1, 0);
            var b3 = UIK.Bouton(boutonsJeu, "Menu", "Menu (Échap)", Style.Bouton, () => Pause(!pause.Ouvert), 19);
            UIK.Placer((RectTransform)b3.transform, 1, 0, 0, 0, 210, 54, 1, 0);
            boutonsJeu.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ écrans

        void AllerTitre()
        {
            Nettoyer();
            etat = Etat.Titre;
            titre.Visible(true);
            salon.Visible(false);
            hud.Visible(false);
            boutonsJeu.gameObject.SetActive(false);
            cam.CommandesActives = false;
            cam.Regarder(cam.CentreCarte, cam.TailleCarte * 0.8f, 42f);
            cam.Placer();
        }

        void AllerSalon()
        {
            etat = Etat.Salon;
            titre.Visible(false);
            salon.Visible(true);
        }

        void Quitter()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Lancer(List<ReglageJoueur> reglages)
        {
            Nettoyer();
            if (salon.Carte != carteConstruite) ConstruireIle(salon.Carte);
            derniersReglages = reglages;
            botsRapides = salon.BotsRapides;
            int graine = System.Environment.TickCount & 0x7fffffff;
            partie = new Partie(reglages, graine, salon.NiveauCible, modele.Plateau);
            ia = new IA(graine);
            foreach (var j in partie.Joueurs)
            {
                var pv = PionVue.Creer(j, monde);
                pions[j.Id] = pv;
            }
            RangerCase(partie.Plateau.Depart, false);
            hud.Installer(partie);
            hud.PionDe = id => pions.ContainsKey(id) ? pions[id].transform : null;
            hud.HauteurPion = id => pions.ContainsKey(id) ? pions[id].Rig.Hauteur : 1.5f;
            hud.Visible(true);
            titre.Visible(false);
            salon.Visible(false);
            boutonsJeu.gameObject.SetActive(true);
            cam.CommandesActives = true;
            etat = Etat.Partie;
            cam.Regarder(modele.Cases[partie.Plateau.Depart], 30f, 48f);
            boucle = StartCoroutine(Boucle());
        }

        void Nettoyer()
        {
            StopAllCoroutines();     // la boucle de jeu et toutes les présentations en cours
            boucle = null;
            foreach (var p in pions.Values) if (p != null) Destroy(p.gameObject);
            pions.Clear();
            EnleverMonstre(true);
            FinirEvade();
            JourImmediat();
            if (transitoires != null) foreach (Transform t in transitoires) Destroy(t.gameObject);
            oieVue = null; oieAnim = null;           // il vivait sous « transitoires »
            if (fq != null) fq.Cacher();
            UIK.OublierIcones("portrait_");          // les avis de recherche de la partie finie
            if (fc != null) fc.Cacher();
            if (victoire != null) { victoire.Detruire(); victoire = null; }
            if (ile != null)
            {
                ile.EffacerChoix(); ile.Survoler(-1);
                ile.MontrerPieges(new List<Piege>(), -1, false);
                ile.MontrerEmbuscades(new List<Joueur>());
                ile.MontrerQuete(-1);
            }
            Time.timeScale = 1f;
            partie = null;
        }

        void Pause(bool oui)
        {
            if (etat != Etat.Partie && etat != Etat.Victoire) { pause.Visible(false); return; }
            pause.Visible(oui);
            Time.timeScale = oui ? 0f : 1f;
            cam.CommandesActives = !oui && !evadeEnCours;
        }

        void Update()
        {
            if (etat == Etat.Titre || etat == Etat.Salon) cam.Orbiter(3.5f);
            if (ecranEvade != null) ecranEvade.Animer(Time.unscaledDeltaTime);
            if (DDInput.ToucheAppuyee(KeyCodeDD.Echap))
            {
                if (regles != null && regles.Ouvert) regles.Visible(false);
                else if (etat == Etat.Partie) Pause(!pause.Ouvert);
                else if (etat == Etat.Salon) AllerTitre();
            }
            // cliquer une case lumineuse répond à la question
            if (etat == Etat.Partie && fq != null && fq.Ouverte && fq.Question != null && DDInput.ClicGauche && !pause.Ouvert)
            {
                bool surUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!surUI) CliquerCase(fq.Question);
            }
        }

        void CliquerCase(SQuestion q)
        {
            if (q.Genre != GenreQuestion.Embranchement && q.Genre != GenreQuestion.Rodeur && q.Genre != GenreQuestion.ChoixCase) return;
            var souris = DDInput.PositionSouris;
            int best = -1; float bd = 75f * 75f;
            for (int i = 0; i < q.Cases.Count; i++)
            {
                if (q.Cases[i] < 0 || !q.Actives[i]) continue;
                var e = cam.Ecran(modele.Cases[q.Cases[i]]);
                if (e.z <= 0) continue;
                float d = (new Vector2(e.x, e.y) - souris).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            if (best >= 0) fq.Choisir(best);
        }

        // ------------------------------------------------------------------ outils

        float V { get { return botsRapides && partie != null && !partie.JoueurCourant.Humain && !nuitEnCours ? 2.4f : 1f; } }

        IEnumerator Attendre(float s) { yield return new WaitForSeconds(s / V); }

        /// Range les pions d'une case sur leurs places (anime ceux qui bougent).
        void RangerCase(int c, bool animer, int sauf = -1)
        {
            if (partie == null) return;
            var l = partie.JoueursSur(c);
            for (int i = 0; i < l.Count; i++)
            {
                PionVue p;
                if (!pions.TryGetValue(l[i].Id, out p) || l[i].Id == sauf) continue;
                var pos = ile.Emplacement(c, i, l.Count);
                if (animer) StartCoroutine(p.Glisser(pos, 0.25f)); else p.transform.position = pos;
            }
        }

        Vector3 Place(int joueur)
        {
            var j = partie.Joueurs[joueur];
            var l = partie.JoueursSur(j.Case);
            return ile.Emplacement(j.Case, Mathf.Max(0, l.IndexOf(j)), l.Count);
        }

        Vector3 Tete(int joueur)
        {
            var p = pions[joueur];
            return p.transform.position + Vector3.up * (p.Rig.Hauteur + 0.2f);
        }

        void MajMarqueurs(Joueur j)
        {
            bool humain = j != null && j.Humain;
            ile.MontrerPieges(partie.Pieges, humain ? j.Id : -1, humain && j.Classe == ClasseHeros.Voleur);
            ile.MontrerEmbuscades(partie.Joueurs);
            bool lieu = j != null && j.Quete != null && (j.Quete.Type == Quete.Genre.Atteindre || j.Quete.Type == Quete.Genre.Evade);
            ile.MontrerQuete(humain && lieu ? j.Quete.CaseCible : -1);
        }

        int Humains()
        {
            int n = 0;
            foreach (var j in partie.Joueurs) if (j.Humain) n++;
            return n;
        }
    }
}

// Donjon de l'Oie — le catalogue modifiable : équipements, monstres, classes, événements et réglages, dans un fichier
// Assets/DonjonDeLOie/Resources/DDCatalogue.asset. On le modifie avec la fenêtre « Donjon de l'Oie › Catalogue ».
// Au lancement, le jeu le lit et remplace les valeurs écrites dans Regles/Donnees.cs. Sans ce fichier, rien ne change.
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Cartes
{
    // Pas d'entrée dans le menu Assets › Create : le jeu ne lit que Resources/DDCatalogue.asset, que la fenêtre du
    // catalogue crée (rempli des valeurs d'origine) d'un clic.
    public sealed class DDCatalogue : ScriptableObject
    {
        public const string CheminResources = "DDCatalogue";

        public List<DefObjet> Objets = new List<DefObjet>();
        public List<DefMonstre> Monstres = new List<DefMonstre>();
        public List<DefClasse> Classes = new List<DefClasse>();
        public List<DefEvenement> Evenements = new List<DefEvenement>();
        public Reglages Reglages = new Reglages();

        /// Appelé par Unity quand on choisit « Reset » dans l'inspecteur du fichier : les valeurs d'origine.
        void Reset() { RemplirParDefaut(); }

        /// Remplit le catalogue avec les valeurs écrites dans le code.
        public void RemplirParDefaut()
        {
            Objets = new List<DefObjet>(Donnees.CopieObjetsParDefaut());
            Monstres = new List<DefMonstre>(Donnees.CopieMonstresParDefaut());
            Classes = new List<DefClasse>(Donnees.CopieClassesParDefaut());
            Evenements = EvenementsParDefaut.Creer();
            Reglages = new Reglages();
        }

        /// Met ce catalogue en service (les règles lisent ensuite ces valeurs).
        public void Appliquer()
        {
            Donnees.Reinitialiser();
            Donnees.Appliquer(Classes, Monstres, Objets, Evenements, Reglages);
        }

        /// Lit le catalogue du dossier Resources et le met en service. Rend false s'il n'existe pas (valeurs du code).
        public static bool Charger()
        {
            var c = Resources.Load<DDCatalogue>(CheminResources);
            if (c == null) { Donnees.Reinitialiser(); return false; }
            c.Appliquer();
            return true;
        }
    }
}

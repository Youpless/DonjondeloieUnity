// Donjon de l'Oie — une carte enregistrée dans un fichier. Celles du dossier Resources/Cartes sont proposées dans le salon.
using UnityEngine;
using DonjonDeLOie.Regles;

namespace DonjonDeLOie.Cartes
{
    public sealed class DDCarte : ScriptableObject
    {
        public const string DossierResources = "Cartes";

        public CarteDef Carte = new CarteDef();
        [TextArea] public string Description = "";
        public int NiveauConseille = 10;

        public static DDCarte[] Toutes() { return Resources.LoadAll<DDCarte>(DossierResources); }
    }
}

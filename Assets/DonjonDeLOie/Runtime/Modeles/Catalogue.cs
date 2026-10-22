// Donjon de l'Oie — point d'entrée de tous les modèles voxel du jeu.
// Chaque modèle est un VoxelRig : des pièces modelées dans un même repère, avec pivots, que la présentation anime.
using System.Collections.Generic;
using UnityEngine;
using DonjonDeLOie.Regles;
using DonjonDeLOie.Voxel;

namespace DonjonDeLOie.Modeles
{
    public static class Catalogue
    {
        /// Héros d'une classe ; « couleur » est celle du joueur (cape, tabard, plumet…).
        public static VoxelRig Heros(ClasseHeros c, Color32 couleur) { return HerosModeles.Construire(c, couleur); }

        /// Monstre par son identifiant (voir Donnees.Monstres : rat, gobelin, loup, araignee, gelee, squelette, orc, spectre, troll, wyverne, dragon).
        public static VoxelRig Monstre(string id) { return MonstresModeles.Construire(id); }

        /// Élément de décor (arbre, rocher, maison…). « graine » fait varier la forme d'un exemplaire à l'autre.
        public static VoxelRig Decor(string id, int graine) { return DecorsModeles.Construire(id, graine); }

        /// Petite figurine posée au bord d'une case pour en montrer le type.
        public static VoxelRig IconeCase(TypeCase t) { return IconesCases.Construire(t); }

        /// Identifiants des décors disponibles pour une zone.
        public static string[] DecorsDeZone(Zone z) { return DecorsModeles.DeZone(z); }
    }
}

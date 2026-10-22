// Les Dés du Dragon — décors : aiguillage vers la nature (DecorsNature*.cs) et les bâtiments (DecorsBatiments*.cs).
using UnityEngine;
using DesDuDragon.Regles;
using DesDuDragon.Voxel;
namespace DesDuDragon.Modeles
{
    public static partial class DecorsModeles
    {
        public static VoxelRig Construire(string id, int graine)
        {
            var r = ConstruireNature(id, graine);
            if (r == null) r = ConstruireBatiment(id, graine);
            if (r == null) { var m = new VoxelModel(); m.Box(-2, 0, -2, 1, 3, 1, Pal.Rouge); r = VoxelRig.Single(id, m, 0.1f, Vector3.zero); }
            return r;
        }
    }
}

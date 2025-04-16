using System.Collections.Generic;
using CoreSystems.Projectiles;
using VRageMath;

namespace CoreSystems.Support
{
    internal class DynTrees
    {
        internal static void RegisterProjectile(Projectile projectile)
        {
            if (projectile.PruningProxyId != -1)
                return;

            var s = Session.I;
            BoundingSphereD sphere = new BoundingSphereD(projectile.Position, projectile.Info.AmmoDef.Const.LargestHitSize);
            BoundingBoxD result;
            BoundingBoxD.CreateFromSphere(ref sphere, out result);
            projectile.PruningProxyId = s.ProjectileTree.AddProxy(ref result, projectile, 0U);
        }

        internal static void UnregisterProjectile(Projectile projectile)
        {
            if (projectile.PruningProxyId == -1)
                return;
            var s = Session.I;
            s.ProjectileTree.RemoveProxy(projectile.PruningProxyId);
            projectile.PruningProxyId = -1;
        }
        internal static void GetAllProjectilesInSphere(Session session, ref BoundingSphereD sphere, List<Projectile> result, bool clearList = true)
        {
            session.ProjectileTree.OverlapAllBoundingSphere(ref sphere, result, clearList);
        }
    }
}

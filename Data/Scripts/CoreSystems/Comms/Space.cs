using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Comms;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    internal class SpaceTrees
    {
        internal MyDynamicAABBTreeD DividedSpace = new MyDynamicAABBTreeD(Vector3D.One * 10.0, 10.0);
        internal void RegisterSignal(RadioStation station, ref BoundingSphereD volume)
        {
            if (station.PruningProxyId != -1)
                return;
            BoundingBoxD result;
            BoundingBoxD.CreateFromSphere(ref volume, out result);
            station.PruningProxyId = DividedSpace.AddProxy(ref result, station, 0U);
        }

        internal void UnregisterSignal(RadioStation station)
        {
            if (station.PruningProxyId == -1)
                return;
            DividedSpace.RemoveProxy(station.PruningProxyId);
            station.PruningProxyId = -1;
        }

        internal void OnSignalMoved(RadioStation station, ref Vector3 velocity, ref BoundingSphereD volume)
        {
            if (station.PruningProxyId == -1)
                return;
            BoundingBoxD result;
            BoundingBoxD.CreateFromSphere(ref volume, out result);
            DividedSpace.MoveProxy(station.PruningProxyId, ref result, velocity);
        }
    }
}

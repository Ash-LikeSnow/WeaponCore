using CoreSystems.Support;
using Sandbox.ModAPI;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Platform
{
    public partial class ControlSys : Part
    {
        internal readonly ControlComponent Comp;
        internal readonly ControlSystem System;
        internal readonly MyStringHash PartHash;

        internal IMyMotorStator BaseMap;
        internal IMyMotorStator OtherMap;
        internal Ai TopAi;
        internal ProtoControlPartState PartState;
        internal bool IsAimed;

        internal ControlSys(ControlSystem system, ControlComponent comp, int partId)
        {
            System = system;
            Comp = comp;
            Init(comp, system, partId);
            PartHash = Comp.Structure.PartHashes[partId];
        }


        internal void CleanControl()
        {
            if (TopAi != null)
            {
                if (TopAi?.RootComp?.PrimaryWeapon != null)
                    TopAi.RootComp.PrimaryWeapon.RotorTurretTracking = false;

                if (TopAi?.RootComp?.Ai?.ControlComp != null)
                    TopAi.RootComp.Ai.ControlComp = null;

                if (TopAi?.RootComp != null)
                    TopAi.RootComp = null;

                TopAi = null;
            }
        }

        internal bool RefreshRootComp()
        {
            for (int i = 0; i < TopAi.WeaponComps.Count; i++)
            {
                var comp = TopAi.WeaponComps[i];
                if (comp.Ai.ControlComp != null)
                {
                    var distSqr = Vector3.DistanceSquared(comp.Cube.PositionComp.LocalAABB.Center, comp.TopEntity.PositionComp.LocalAABB.Center);
                    if (distSqr < comp.Ai.ClosestFixedWeaponCompSqr)
                    {
                        comp.Ai.ClosestFixedWeaponCompSqr = distSqr;
                        comp.Ai.RootComp = comp;
                        comp.UpdateControlInfo();
                    }
                }
            }
            return TopAi.RootComp?.CoreEntity != null && !TopAi.RootComp.CoreEntity.MarkedForClose;
        }
    }
}

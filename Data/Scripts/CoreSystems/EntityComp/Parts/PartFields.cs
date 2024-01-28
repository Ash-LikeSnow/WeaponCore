using System;
using System.Collections.Generic;
using CoreSystems.Support;
using VRageMath;

namespace CoreSystems.Platform
{
    public partial class Part
    {
        //internal readonly List<Action<long, int, ulong, long, Vector3D, bool>> Monitors = new List<Action<long, int, ulong, long, Vector3D, bool>>();

        internal CoreComponent BaseComp;
        internal CoreSystem CoreSystem;
        internal PartAcquire Acquire;
        internal float MaxCharge;
        internal float DesiredPower;
        internal float AssignedPower;
        internal float EstimatedCharge;
        internal uint PartCreatedTick;
        internal uint PartReadyTick;
        internal int ShortLoadId;
        internal int UniquePartId;
        internal int PartId;
        internal bool IsPrime;
        internal bool Loading;
        internal bool ExitCharger;
        internal bool NewPowerNeeds;
        internal bool InCharger;
        internal bool Charging;
        internal bool StayCharged;

        internal void Init(CoreComponent comp, CoreSystem system, int partId)
        {
            CoreSystem = system;
            StayCharged = system.StayCharged;
            BaseComp = comp;
            PartCreatedTick = Session.I.Tick;
            PartId = partId;
            IsPrime = partId == comp.Platform.Structure.PrimaryPart;
            Acquire = new PartAcquire(this);
            UniquePartId = Session.I.UniquePartId;
            ShortLoadId = Session.I.ShortLoadAssigner();
            //for (int i = 0; i < BaseComp.Monitors[PartId].Count; i++)
            //    Monitors.Add(BaseComp.Monitors[PartId][i]);
        }


        internal class PartAcquire
        {
            internal readonly Part Part;
            internal uint CreatedTick;
            internal int SlotId;
            internal bool IsSleeping;
            internal bool Monitoring;

            internal PartAcquire(Part part)
            {
                Part = part;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Support;
using ProtoBuf;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;

namespace CoreSystems
{
    [ProtoInclude(1999, typeof(ProtoWeaponRepo))]
    [ProtoInclude(1998, typeof(ProtoUpgradeRepo))]
    [ProtoInclude(1997, typeof(ProtoSupportRepo))]
    [ProtoInclude(1996, typeof(ProtoControlRepo))]
    [ProtoContract]
    public class ProtoRepo
    {
        [ProtoMember(1)] public int Version = Session.VersionControl;
    }

    #region packet BaseData
    [ProtoContract]
    internal class DataReport
    {
        [ProtoMember(1)] internal Dictionary<string, string> Session = new Dictionary<string, string>();
        [ProtoMember(2)] internal Dictionary<string, string> Ai = new Dictionary<string, string>();
        [ProtoMember(3)] internal Dictionary<string, string> Comp = new Dictionary<string, string>();
        [ProtoMember(4)] internal Dictionary<string, string> Platform = new Dictionary<string, string>();
        [ProtoMember(5)] internal Dictionary<string, string> Weapon = new Dictionary<string, string>();
    }

    [ProtoContract]
    internal class InputStateData
    {
        [ProtoMember(1)] internal bool MouseButtonLeft;
        [ProtoMember(2)] internal bool MouseButtonMenu;
        [ProtoMember(3)] internal bool MouseButtonRight;
        [ProtoMember(4)] internal bool InMenu;

        internal InputStateData() { }

        internal InputStateData(InputStateData createFrom)
        {
            Sync(createFrom);
        }

        internal void Sync(InputStateData syncFrom)
        {
            MouseButtonLeft = syncFrom.MouseButtonLeft;
            MouseButtonMenu = syncFrom.MouseButtonMenu;
            MouseButtonRight = syncFrom.MouseButtonRight;
            InMenu = syncFrom.InMenu;
        }
    }

    [ProtoContract]
    internal class PlayerMouseData
    {
        [ProtoMember(1)] internal long PlayerId;
        [ProtoMember(2)] internal InputStateData MouseStateData;
    }


    [ProtoContract]
    public class WeaponRandomGenerator
    {
        //[ProtoMember(1)] public int TurretCurrentCounter;
        //[ProtoMember(2)] public int ClientProjectileCurrentCounter;
        //[ProtoMember(3)] public int AcquireCurrentCounter;
        [ProtoMember(4)] public int CurrentSeed;
        public XorShiftRandomStruct TurretRandom;
        public XorShiftRandomStruct AcquireRandom;

        public enum RandomType
        {
            Deviation,
            ReAcquire,
            Acquire,
        }

        public void Init(Weapon w)
        {
            if (Session.I.IsServer)
                CurrentSeed = int.MaxValue - w.UniquePartId;
            else
                Session.I.WeaponLookUp[w.PartState.Id] = w;

            TurretRandom = new XorShiftRandomStruct((ulong)CurrentSeed);
            AcquireRandom = new XorShiftRandomStruct((ulong)CurrentSeed);
            AcquireRandom.NextBoolean();
        }

        public void Sync(WeaponRandomGenerator syncFrom)
        {
            CurrentSeed = syncFrom.CurrentSeed;
            TurretRandom = new XorShiftRandomStruct((ulong)CurrentSeed);
        }

        internal void ReInitRandom()
        {
            CurrentSeed = TurretRandom.Range(1, int.MaxValue);
            TurretRandom = new XorShiftRandomStruct((ulong)CurrentSeed);
            AcquireRandom = new XorShiftRandomStruct((ulong)CurrentSeed);
        }
    }

    [ProtoContract]
    public class EwarValues
    {
        [ProtoMember(1)] public long FiringBlockId;
        [ProtoMember(2)] public long EwaredBlockId;
        [ProtoMember(3)] public int SystemId;
        [ProtoMember(4)] public int AmmoId;
        [ProtoMember(5)] public uint EndTick;
    }
    #endregion
}

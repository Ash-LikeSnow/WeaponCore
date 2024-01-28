using System;
using System.ComponentModel;
using CoreSystems.Platform;
using CoreSystems.Support;
using ProtoBuf;
using static CoreSystems.Support.WeaponDefinition.TargetingDef;
using static CoreSystems.Support.CoreComponent;

namespace CoreSystems
{

    [ProtoContract]
    public class ProtoUpgradeRepo : ProtoRepo
    {
        [ProtoMember(1)] public ProtoUpgradeComp Values;

        public void ResetToFreshLoadState()
        {
            Values.State.TrackingReticle = false;
            for (int i = 0; i < Values.State.Upgrades.Length; i++)
            {
                var ws = Values.State.Upgrades[i];
                ws.Heat = 0;
                ws.Overheated = false;
            }
            ResetCompBaseRevisions();
        }

        public void ResetCompBaseRevisions()
        {
            Values.Revision = 0;
            Values.State.Revision = 0;
            for (int i = 0; i < Values.State.Upgrades.Length; i++)
            {
                var p = Values.State.Upgrades[i];
            }
        }
    }


    [ProtoContract]
    public class ProtoUpgradeComp
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoUpgradeSettings Set;
        [ProtoMember(3)] public ProtoUpgradeState State;

        public bool Sync(Upgrade.UpgradeComponent comp, ProtoUpgradeComp sync)
        {
            if (sync.Revision > Revision)
            {

                Revision = sync.Revision;
                Set.Sync(comp, sync.Set);
                State.Sync(comp, sync.State, ProtoUpgradeState.Caller.CompData);
                return true;
            }
            return false;
        }

        public void UpdateCompPacketInfo(Upgrade.UpgradeComponent comp, bool clean = false)
        {
            ++Revision;
            ++State.Revision;
            Session.PacketInfo info;
            if (clean && Session.I.PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out info))
            {
                Session.I.PrunedPacketsToClient.Remove(comp.Data.Repo.Values.State);
                Session.I.PacketUpgradeStatePool.Return((UpgradeStatePacket)info.Packet);
            }
        }
    }


    [ProtoContract]
    public class ProtoUpgradeSettings
    {
        [ProtoMember(1), DefaultValue(true)] public bool Guidance = true;
        [ProtoMember(2), DefaultValue(1)] public int Overload = 1;
        [ProtoMember(3), DefaultValue(1)] public float DpsModifier = 1;
        [ProtoMember(4), DefaultValue(1)] public float RofModifier = 1;
        [ProtoMember(5), DefaultValue(100)] public float Range = 100;
        [ProtoMember(6)] public ProtoUpgradeOverrides Overrides;


        public ProtoUpgradeSettings()
        {
            Overrides = new ProtoUpgradeOverrides();
        }

        public void Sync(Upgrade.UpgradeComponent comp, ProtoUpgradeSettings sync)
        {
            Guidance = sync.Guidance;
            Range = sync.Range;
            //Weapon.WeaponComponent.SetRange(comp);

            Overrides.Sync(sync.Overrides);

            var rofChange = Math.Abs(RofModifier - sync.RofModifier) > 0.0001f;
            var dpsChange = Math.Abs(DpsModifier - sync.DpsModifier) > 0.0001f;

            if (Overload != sync.Overload || rofChange || dpsChange)
            {
                Overload = sync.Overload;
                RofModifier = sync.RofModifier;
                DpsModifier = sync.DpsModifier;
                //if (rofChange) Weapon.WeaponComponent.SetRof(comp);
            }
        }

    }

    [ProtoContract]
    public class ProtoUpgradeState
    {
        public enum Caller
        {
            Direct,
            CompData,
        }

        public enum ControlMode
        {
            None,
            Ui,
            Toolbar,
            Camera
        }

        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoUpgradePartState[] Upgrades;
        [ProtoMember(3)] public bool TrackingReticle; //don't save
        [ProtoMember(4), DefaultValue(-1)] public long PlayerId = -1;
        [ProtoMember(5), DefaultValue(ControlMode.None)] public ControlMode Control = ControlMode.None;
        [ProtoMember(6)] public Trigger Terminal;

        public bool Sync(Upgrade.UpgradeComponent comp, ProtoUpgradeState sync, Caller caller)
        {
            if (sync.Revision > Revision || caller == Caller.CompData)
            {
                Revision = sync.Revision;
                TrackingReticle = sync.TrackingReticle;
                PlayerId = sync.PlayerId;
                Control = sync.Control;
                Terminal = sync.Terminal;
                for (int i = 0; i < sync.Upgrades.Length; i++)
                    comp.Platform.Upgrades[i].PartState.Sync(sync.Upgrades[i]);
                return true;
            }
            return false;
        }

        public void TerminalActionSetter(Upgrade.UpgradeComponent comp, Trigger action, bool syncWeapons = false, bool updateWeapons = true)
        {
            Terminal = action;

            if (updateWeapons)
            {
                for (int i = 0; i < Upgrades.Length; i++)
                    Upgrades[i].Action = action;
            }

            if (syncWeapons)
                Session.I.SendState(comp);
        }
    }

    [ProtoContract]
    public class ProtoUpgradePartState
    {
        [ProtoMember(1)] public float Heat; // don't save
        [ProtoMember(2)] public bool Overheated; //don't save
        [ProtoMember(3), DefaultValue(Trigger.Off)] public Trigger Action = Trigger.Off; // save

        public void Sync(ProtoUpgradePartState sync)
        {
            Heat = sync.Heat;
            Overheated = sync.Overheated;
            Action = sync.Action;
        }
    }

    [ProtoContract]
    public class ProtoUpgradeOverrides
    {
        public enum MoveModes
        {
            Any,
            Moving,
            Mobile,
            Moored,
        }

        public enum ControlModes
        {
            Auto,
            Manual,
            Painter,
        }

        [ProtoMember(1)] public bool Neutrals;
        [ProtoMember(2)] public bool Unowned;
        [ProtoMember(3)] public bool Friendly;
        [ProtoMember(4)] public bool FocusTargets;
        [ProtoMember(5)] public bool FocusSubSystem;
        [ProtoMember(6)] public int MinSize;
        [ProtoMember(7), DefaultValue(ControlModes.Auto)] public ControlModes Control = ControlModes.Auto;
        [ProtoMember(8), DefaultValue(BlockTypes.Any)] public BlockTypes SubSystem = BlockTypes.Any;
        [ProtoMember(9), DefaultValue(true)] public bool Meteors = true;
        [ProtoMember(10), DefaultValue(true)] public bool Biologicals = true;
        [ProtoMember(11), DefaultValue(true)] public bool Projectiles = true;
        [ProtoMember(12), DefaultValue(16384)] public int MaxSize = 16384;
        [ProtoMember(13), DefaultValue(MoveModes.Any)] public MoveModes MoveMode = MoveModes.Any;
        [ProtoMember(14), DefaultValue(true)] public bool Grids = true;
        [ProtoMember(15), DefaultValue(true)] public bool ArmorShowArea;

        public void Sync(ProtoUpgradeOverrides syncFrom)
        {
            MoveMode = syncFrom.MoveMode;
            MaxSize = syncFrom.MaxSize;
            MinSize = syncFrom.MinSize;
            Neutrals = syncFrom.Neutrals;
            Unowned = syncFrom.Unowned;
            Friendly = syncFrom.Friendly;
            Control = syncFrom.Control;
            FocusTargets = syncFrom.FocusTargets;
            FocusSubSystem = syncFrom.FocusSubSystem;
            SubSystem = syncFrom.SubSystem;
            Meteors = syncFrom.Meteors;
            Grids = syncFrom.Grids;
            ArmorShowArea = syncFrom.ArmorShowArea;
            Biologicals = syncFrom.Biologicals;
            Projectiles = syncFrom.Projectiles;
        }
    }
}

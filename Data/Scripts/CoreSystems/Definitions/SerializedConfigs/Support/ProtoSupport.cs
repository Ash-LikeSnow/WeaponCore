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
    public class ProtoSupportRepo : ProtoRepo
    {
        [ProtoMember(1)] public ProtoSupportComp Values;

        public void ResetToFreshLoadState()
        {
            Values.State.TrackingReticle = false;
            for (int i = 0; i < Values.State.Support.Length; i++)
            {
                var ws = Values.State.Support[i];
                ws.Heat = 0;
                ws.Overheated = false;
            }
            ResetCompBaseRevisions();
        }

        public void ResetCompBaseRevisions()
        {
            Values.Revision = 0;
            Values.State.Revision = 0;
            for (int i = 0; i < Values.State.Support.Length; i++)
            {
                var p = Values.State.Support[i];
            }
        }
    }


    [ProtoContract]
    public class ProtoSupportComp
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoSupportSettings Set;
        [ProtoMember(3)] public ProtoSupportState State;

        public bool Sync(SupportSys.SupportComponent comp, ProtoSupportComp sync)
        {
            if (sync.Revision > Revision)
            {

                Revision = sync.Revision;
                Set.Sync(comp, sync.Set);
                State.Sync(comp, sync.State, ProtoSupportState.Caller.CompData);
                return true;
            }
            return false;
        }

        public void UpdateCompPacketInfo(SupportSys.SupportComponent comp, bool clean = false)
        {
            ++Revision;
            ++State.Revision;
            Session.PacketInfo info;
            if (clean && Session.I.PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out info))
            {
                Session.I.PrunedPacketsToClient.Remove(comp.Data.Repo.Values.State);
                Session.I.PacketSupportStatePool.Return((SupportStatePacket)info.Packet);
            }
        }
    }


    [ProtoContract]
    public class ProtoSupportSettings
    {
        [ProtoMember(1), DefaultValue(true)] public bool Guidance = true;
        [ProtoMember(2), DefaultValue(1)] public int Overload = 1;
        [ProtoMember(3), DefaultValue(1)] public float DpsModifier = 1;
        [ProtoMember(4), DefaultValue(1)] public float RofModifier = 1;
        [ProtoMember(5), DefaultValue(100)] public float Range = 100;
        [ProtoMember(6)] public ProtoSupportOverrides Overrides;


        public ProtoSupportSettings()
        {
            Overrides = new ProtoSupportOverrides();
        }

        public void Sync(SupportSys.SupportComponent comp, ProtoSupportSettings sync)
        {
            Guidance = sync.Guidance;
            Range = sync.Range;
            SupportSys.SupportComponent.SetRange(comp);

            Overrides.Sync(sync.Overrides);

            var rofChange = Math.Abs(RofModifier - sync.RofModifier) > 0.0001f;
            var dpsChange = Math.Abs(DpsModifier - sync.DpsModifier) > 0.0001f;

            if (Overload != sync.Overload || rofChange || dpsChange)
            {
                Overload = sync.Overload;
                RofModifier = sync.RofModifier;
                DpsModifier = sync.DpsModifier;
                if (rofChange) SupportSys.SupportComponent.SetRof(comp);
            }
        }

    }

    [ProtoContract]
    public class ProtoSupportState
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
        [ProtoMember(2)] public ProtoSupportPartState[] Support;
        [ProtoMember(3)] public bool TrackingReticle; //don't save
        [ProtoMember(4), DefaultValue(-1)] public long PlayerId = -1;
        [ProtoMember(5), DefaultValue(ControlMode.None)] public ControlMode Control = ControlMode.None;
        [ProtoMember(6)] public Trigger Terminal;

        public bool Sync(CoreComponent comp, ProtoSupportState sync, Caller caller)
        {
            if (sync.Revision > Revision || caller == Caller.CompData)
            {
                Revision = sync.Revision;
                TrackingReticle = sync.TrackingReticle;
                PlayerId = sync.PlayerId;
                Control = sync.Control;
                Terminal = sync.Terminal;
                for (int i = 0; i < sync.Support.Length; i++)
                    comp.Platform.Support[i].PartState.Sync(sync.Support[i]);

                return true;
            }
            return false;
        }

        public void TerminalActionSetter(SupportSys.SupportComponent comp, Trigger action, bool syncWeapons = false, bool updateWeapons = true)
        {
            Terminal = action;

            if (updateWeapons)
            {
                for (int i = 0; i < Support.Length; i++)
                    Support[i].Action = action;
            }

            if (syncWeapons)
                Session.I.SendState(comp);
        }
    }

    [ProtoContract]
    public class ProtoSupportPartState
    {
        [ProtoMember(1)] public float Heat; // don't save
        [ProtoMember(2)] public bool Overheated; //don't save
        [ProtoMember(3), DefaultValue(Trigger.Off)] public Trigger Action = Trigger.Off; // save

        public void Sync(ProtoSupportPartState sync)
        {
            Heat = sync.Heat;
            Overheated = sync.Overheated;
            Action = sync.Action;
        }

        public void WeaponMode(SupportSys.SupportComponent comp, Trigger action, bool resetTerminalAction = true, bool syncCompState = true)
        {
            if (resetTerminalAction)
                comp.Data.Repo.Values.State.Terminal = Trigger.Off;

            Action = action;
            if (Session.I.MpActive && Session.I.IsServer && syncCompState)
                Session.I.SendState(comp);
        }

    }

    [ProtoContract]
    public class ProtoSupportOverrides
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

        public void Sync(ProtoSupportOverrides syncFrom)
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

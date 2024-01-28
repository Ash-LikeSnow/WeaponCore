using CoreSystems.Platform;
using CoreSystems.Support;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.ComponentModel;
using static CoreSystems.Support.CoreComponent;
using static CoreSystems.Support.WeaponDefinition.TargetingDef;

namespace CoreSystems
{

    [ProtoContract]
    public class ProtoControlRepo : ProtoRepo
    {
        [ProtoMember(1)] public ProtoControlComp Values;

        public void ResetToFreshLoadState(ControlSys.ControlComponent comp)
        {
            Values.State.TrackingReticle = false;
            var ws = Values.State.Control;
            ws.Heat = 0;
            ws.Overheated = false;
            Values.Other.Rotor1 = comp.Controller.AzimuthRotor?.EntityId ?? 0;
            Values.Other.Rotor2 = comp.Controller.ElevationRotor?.EntityId ?? 0;
            ResetCompBaseRevisions();
        }

        public void ResetCompBaseRevisions()
        {
            Values.Revision = 0;
            Values.State.Revision = 0;
            var p = Values.State.Control;
        }
    }


    [ProtoContract]
    public class ProtoControlComp
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoWeaponSettings Set;
        [ProtoMember(3)] public ProtoControlState State;
        [ProtoMember(4)] public ProtoControlOtherSettings Other;

        public bool Sync(ControlSys.ControlComponent comp, ProtoControlComp sync)
        {
            if (sync.Revision > Revision)
            {

                Revision = sync.Revision;
                Set.Sync(comp, sync.Set);
                State.Sync(comp, sync.State, ProtoControlState.Caller.CompData);
                Other.Sync(comp, sync.Other);
                return true;
            }
            return false;
        }

        public void UpdateCompPacketInfo(ControlSys.ControlComponent comp, bool clean = false)
        {
            ++Revision;
            ++State.Revision;
            Session.PacketInfo info;
            if (clean && Session.I.PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out info))
            {
                Session.I.PrunedPacketsToClient.Remove(comp.Data.Repo.Values.State);
                Session.I.PacketControlStatePool.Return((ControlStatePacket)info.Packet);
            }
        }
    }


    [ProtoContract]
    public class ProtoControlState
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
        [ProtoMember(2)] public ProtoControlPartState Control;
        [ProtoMember(3)] public bool TrackingReticle; //don't save
        [ProtoMember(4), DefaultValue(-1)] public long PlayerId = -1;
        [ProtoMember(5), DefaultValue(ControlMode.None)] public ControlMode Mode = ControlMode.None;
        [ProtoMember(6)] public Trigger Terminal;

        public bool Sync(CoreComponent comp, ProtoControlState sync, Caller caller)
        {
            if (sync.Revision > Revision || caller == Caller.CompData)
            {
                Revision = sync.Revision;
                TrackingReticle = sync.TrackingReticle;
                PlayerId = sync.PlayerId;
                Mode = sync.Mode;
                Terminal = sync.Terminal;
                comp.Platform.Control.PartState.Sync(sync.Control);

                return true;
            }
            return false;
        }

        public void TerminalActionSetter(ControlSys.ControlComponent comp, Trigger action, bool syncWeapons = false, bool updateWeapons = true)
        {
            Terminal = action;

            if (updateWeapons)
            {
                Control.Action = action;
            }

            if (syncWeapons)
                Session.I.SendState(comp);
        }
    }

    [ProtoContract]
    public class ProtoControlPartState
    {
        [ProtoMember(1)] public float Heat; // don't save
        [ProtoMember(2)] public bool Overheated; //don't save
        [ProtoMember(3), DefaultValue(Trigger.Off)] public Trigger Action = Trigger.Off; // save

        public void Sync(ProtoControlPartState sync)
        {
            Heat = sync.Heat;
            Overheated = sync.Overheated;
            Action = sync.Action;
        }

        public void WeaponMode(ControlSys.ControlComponent comp, Trigger action, bool resetTerminalAction = true, bool syncCompState = true)
        {
            if (resetTerminalAction)
                comp.Data.Repo.Values.State.Terminal = Trigger.Off;

            Action = action;
            if (Session.I.MpActive && Session.I.IsServer && syncCompState)
                Session.I.SendState(comp);
        }

    }

    [ProtoContract]
    public class ProtoControlOtherSettings
    {
        [ProtoMember(1)] public float GravityOffset;
        [ProtoMember(2)] public long Rotor1;
        [ProtoMember(3)] public long Rotor2;


        public void Sync(CoreComponent comp, ProtoControlOtherSettings sync)
        {
            GravityOffset = sync.GravityOffset;
            Rotor1 = sync.Rotor1;
            Rotor2 = sync.Rotor2;
        }
    }

}

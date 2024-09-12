using System;
using System.Collections.Generic;
using System.ComponentModel;
using CoreSystems.Platform;
using CoreSystems.Support;
using ProtoBuf;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRageMath;
using static CoreSystems.Support.WeaponDefinition.TargetingDef;
using static CoreSystems.Support.CoreComponent;
using static CoreSystems.Platform.Weapon.WeaponComponent;

namespace CoreSystems
{
    [ProtoContract]
    public class ProtoWeaponRepo : ProtoRepo
    {
        [ProtoMember(1)] public ProtoWeaponAmmo[] Ammos;
        [ProtoMember(2)] public ProtoWeaponComp Values;

        public void ResetToFreshLoadState(Weapon.WeaponComponent comp)
        {
            Values.State.Tasks.Clean();
            Values.State.TrackingReticle = comp.IsBot;
            Values.State.ToggleCount = 0;
            Values.State.Trigger = Trigger.Off;
            Values.State.Control = ProtoWeaponState.ControlMode.Ui;
            if (!comp.PrimaryWeapon.System.TrackCharacters)
                Values.Set.Overrides.Biologicals = false;

            if (!comp.PrimaryWeapon.System.TrackProjectile)
                Values.Set.Overrides.Projectiles = false;

            if (!comp.PrimaryWeapon.System.TrackMeteors)
                Values.Set.Overrides.Meteors = false;

            if (!comp.PrimaryWeapon.System.TrackGrids)
                Values.Set.Overrides.Grids = false;

            if (comp.DisableSupportingPD)
                Values.Set.Overrides.SupportingPD = false;

            if (comp.Ai.AiType == Ai.AiTypes.Player)
            {
                Values.State.PlayerId = comp.Rifle.OwnerIdentityId;
            }

            if (comp.DefaultTrigger != Trigger.Off)
                Values.State.Trigger = comp.DefaultTrigger;

            if (Values.Set.Overrides.BurstCount <= 0)
                Values.Set.Overrides.BurstCount = 1;

            for (int i = 0; i < comp.Collection.Count; i++)
            {
                var we = comp.Collection[i];
                if (Session.I.IsCreative)
                {
                    we.Reload.LifetimeLoads = we.ProtoWeaponAmmo.CurrentAmmo > 0 ? 1 : 0;
                    //we.ProtoWeaponAmmo.CurrentAmmo = 0;
                    //we.ProtoWeaponAmmo.CurrentCharge = 0;
                }

                if (comp.DefaultReloads != 0)
                {
                    we.Reload.CurrentMags = comp.DefaultReloads;
                }
            }

            for (int i = 0; i < Values.Reloads.Length; i++)
            {
                var wr = Values.Reloads[i];
                
                wr.StartId = 0;
                wr.EndId = 0;
                wr.WaitForClient = false;

                var we = comp.Collection[i];
                if (wr.AmmoTypeId >= we.System.AmmoTypes.Length)
                    wr.AmmoTypeId = 0;
            }

            for (int i = 0; i < Values.State.Weapons.Length; i++)
            {
                var w = comp.Collection[i];
                var ws = Values.State.Weapons[i];
                ws.Heat = 0;
                ws.Overheated = false;
                ws.Id = Session.I.SyncWeaponId;
                w.ProPositionSync.WeaponSyncId = ws.Id;
                w.ProTargetSync.WeaponSyncId = ws.Id;
                Session.I.WeaponLookUp[ws.Id] = w;
            }

            ResetCompBaseRevisions();
        }

        public void ResetCompBaseRevisions()
        {
            Values.Revision = 0;
            for (int i = 0; i < Ammos.Length; i++)
                Ammos[i].Revision = 0;

            for (int i = 0; i < Values.Targets.Length; i++)
                Values.Targets[i].Revision = 0;

            for (int i = 0; i < Values.Reloads.Length; i++)
                Values.Reloads[i].Revision = 0;
        }
    }

    [ProtoContract]
    public class ProtoWeaponAmmo
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public int CurrentAmmo; //save
        [ProtoMember(3)] public float CurrentCharge; //save
        //[ProtoMember(4)] public long CurrentMags; // save
        //[ProtoMember(5)] public int AmmoTypeId; //remove me
        //[ProtoMember(6)] public int AmmoCycleId; //remove me

        public bool Sync(Weapon w, ProtoWeaponAmmo sync)
        {
            if (sync.Revision > Revision)
            {
                Revision = sync.Revision;

                CurrentAmmo = sync.CurrentAmmo;
                CurrentCharge = sync.CurrentCharge;
                w.ClientMakeUpShots = 0;

                return true;
            }
            return false;
        }
    }


    [ProtoContract]
    public class ProtoProPositionSync
    {

        [ProtoMember(1)] public uint WeaponSyncId;
        [ProtoMember(2)] public readonly List<ProtoProPosition> Collection = new List<ProtoProPosition>();
    }

    [ProtoContract]
    public class ProtoProTargetSync
    {

        [ProtoMember(1)] public uint WeaponSyncId;
        [ProtoMember(2)] public readonly List<ProtoProTarget> Collection = new List<ProtoProTarget>();
    }

    [ProtoContract]
    public class ProtoProPosition 
    {
        public enum ProSyncState
        {
            Alive,
            Dead,
            Return,
            Stored,
        }

        [ProtoMember(1)] public ulong ProId;
        //[ProtoMember(2)] public ushort PartId;
        //[ProtoMember(3)] public uint CoreEntityId;
        [ProtoMember(4)] public Vector3D Position;
        [ProtoMember(5)] public Vector3 Velocity;
        [ProtoMember(6)] public ProSyncState State;
    }

    [ProtoContract]
    public class ProtoProTarget
    {
        [ProtoMember(1)] public ulong ProId;
        [ProtoMember(2)] public long EntityId;
    }

    [ProtoContract]
    public class ProtoWeaponComp
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoWeaponSettings Set;
        [ProtoMember(3)] public ProtoWeaponState State;
        [ProtoMember(4)] public ProtoWeaponTransferTarget[] Targets;
        [ProtoMember(5)] public ProtoWeaponReload[] Reloads;

        public void Sync(Weapon.WeaponComponent comp, ProtoWeaponComp sync)
        {

            Revision = sync.Revision;
            Set.Sync(comp, sync.Set);
            State.Sync(comp, sync.State);

            for (int i = 0; i < Targets.Length; i++)
            {
                var w = comp.Collection[i];
                sync.Targets[i].SyncTarget(w);
                Reloads[i].Sync(w, sync.Reloads[i], true);
            }
        }

        public void UpdateCompPacketInfo(Weapon.WeaponComponent comp, bool clean = false, bool resetRnd = false, int partId = -1)
        {
            ++Revision;
            Session.PacketInfo info;
            if (clean && Session.I.PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out info))
            {
                Session.I.PrunedPacketsToClient.Remove(comp.Data.Repo.Values.State);
                Session.I.PacketWeaponStatePool.Return((WeaponStatePacket)info.Packet);
            }

            for (int i = 0; i < Targets.Length; i++)
            {

                var t = Targets[i];
                var wr = Reloads[i];
                var validPart = partId == -1 || partId == i;

                if (clean && validPart)
                {
                    if (Session.I.PrunedPacketsToClient.TryGetValue(t, out info))
                    {
                        Session.I.PrunedPacketsToClient.Remove(t);
                        Session.I.PacketTargetPool.Return((TargetPacket)info.Packet);
                    }
                    if (Session.I.PrunedPacketsToClient.TryGetValue(wr, out info))
                    {
                        Session.I.PrunedPacketsToClient.Remove(wr);
                        Session.I.PacketReloadPool.Return((WeaponReloadPacket)info.Packet);
                    }
                }
                ++wr.Revision;
                ++t.Revision;
                
                if (resetRnd)
                    t.WeaponRandom.ReInitRandom();
            }
        }
    }

    [ProtoContract]
    public class ProtoWeaponReload
    {
        [ProtoMember(1)] public uint Revision;
        //[ProtoMember(2)] public int StartId; //save
        //[ProtoMember(3)] public int EndId; //save
        [ProtoMember(9)] public ushort StartId; //save
        [ProtoMember(10)] public ushort EndId; //save
        [ProtoMember(4)] public int MagsLoaded = 1;
        [ProtoMember(5)] public bool WaitForClient; //don't save
        [ProtoMember(6)] public int AmmoTypeId; //save
        [ProtoMember(7)] public int CurrentMags; // save
        [ProtoMember(8)] public int LifetimeLoads; // save

        public void Sync(Weapon w, ProtoWeaponReload sync, bool force)
        {
            if (sync.Revision > Revision || force)
            {
                Revision = sync.Revision;
                var newReload = StartId != sync.StartId && EndId == sync.EndId;

                if (newReload && w.ClientReloading && !w.Charging)
                    w.Reloaded(6);

                StartId = sync.StartId;
                EndId = sync.EndId;
                LifetimeLoads = sync.LifetimeLoads;

                MagsLoaded = sync.MagsLoaded;
                
                WaitForClient = sync.WaitForClient;
                
                var oldAmmoId = AmmoTypeId;
                AmmoTypeId = sync.AmmoTypeId;

                if (oldAmmoId != AmmoTypeId)
                {
                    w.ServerQueuedAmmo = newReload;

                    if (newReload)
                    {
                        w.AmmoName = w.System.AmmoTypes[AmmoTypeId].AmmoDef.AmmoRound;
                        w.AmmoNameTerminal = w.System.AmmoTypes[AmmoTypeId].AmmoDef.Const.TerminalName;
                        w.DelayedCycleId = -1;
                    }

                    w.ChangeActiveAmmoClient();
                }

                w.ClientReload(true);
            }
        }
    }

    [ProtoContract]
    public class ProtoWeaponSettings
    {
        [ProtoMember(1), DefaultValue(true)] public bool ReportTarget = true;
        [ProtoMember(2), DefaultValue(1)] public int Overload = 1;
        //[ProtoMember(3), DefaultValue(1)] public float DpsModifier = 1;
        [ProtoMember(4), DefaultValue(1)] public float RofModifier = 1;
        [ProtoMember(5), DefaultValue(100)] public float Range = 100;
        [ProtoMember(6)] public ProtoWeaponOverrides Overrides;
        [ProtoMember(7), DefaultValue(1)] public float BaseDamageMultiplier = 1;
        [ProtoMember(8), DefaultValue(1)] public float AreaDamageMultiplier = 1;
        [ProtoMember(9), DefaultValue(1)] public float AreaRadiusMultiplier = 1;
        [ProtoMember(10), DefaultValue(1)] public float VelocityMultiplier = 1;
        [ProtoMember(11), DefaultValue(true)] public bool FiringAllowed = true;

        public ProtoWeaponSettings()
        {
            Overrides = new ProtoWeaponOverrides();
        }

        public void Sync(Weapon.WeaponComponent comp, ProtoWeaponSettings sync)
        {
            ReportTarget = sync.ReportTarget;
            Range = sync.Range;
            SetRange(comp);

            if (Overrides.WeaponGroupId != sync.Overrides.WeaponGroupId)
                comp.ChangeWeaponGroup(sync.Overrides.WeaponGroupId);

            if (Overrides.SequenceId != sync.Overrides.SequenceId)
                comp.ChangeSequenceId();

            Overrides.Sync(sync.Overrides);

            var rofChange = Math.Abs(RofModifier - sync.RofModifier) > 0.0001f;

            if (Overload != sync.Overload || rofChange)
            {
                Overload = sync.Overload;
                RofModifier = sync.RofModifier;
                if (rofChange) SetRof(comp);
            }

            var dmgChange = ((BaseDamageMultiplier - sync.BaseDamageMultiplier) + (AreaDamageMultiplier - sync.AreaDamageMultiplier) + (AreaRadiusMultiplier - sync.AreaRadiusMultiplier)) != 0;

            if (dmgChange)
            {
                BaseDamageMultiplier = sync.BaseDamageMultiplier;
                AreaDamageMultiplier = sync.AreaDamageMultiplier;
                AreaRadiusMultiplier = sync.AreaRadiusMultiplier;
                SetDmg(comp);
            }

            if (VelocityMultiplier != sync.VelocityMultiplier)
            {
                VelocityMultiplier = sync.VelocityMultiplier;
                SetVel(comp);
            }

            if (FiringAllowed != sync.FiringAllowed)
            {
                FiringAllowed = sync.FiringAllowed;
                SetFiringAllowed(comp);
            }

            var wValues = comp.Data.Repo.Values;
            comp.ManualMode = wValues.State.TrackingReticle && wValues.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual; // needs to be set everywhere dedicated and non-tracking clients receive TrackingReticle or Control updates.

        }

        public void Sync(ControlSys.ControlComponent comp, ProtoWeaponSettings sync)
        {
            ReportTarget = sync.ReportTarget;
            Range = sync.Range;
            Overrides.Sync(sync.Overrides);

            var wValues = comp.Data.Repo.Values;
            comp.ManualMode = wValues.State.TrackingReticle && wValues.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual; // needs to be set everywhere dedicated and non-tracking clients receive TrackingReticle or Control updates.
        }

    }

    [ProtoContract]
    public class ProtoWeaponState
    {
        public enum ControlMode
        {
            None,
            Ui,
            Camera
        }

        //[ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoWeaponPartState[] Weapons;
        [ProtoMember(3)] public bool TrackingReticle; //don't save
        [ProtoMember(4), DefaultValue(-1)] public long PlayerId = -1;
        [ProtoMember(5), DefaultValue(ControlMode.Ui)] public ControlMode Control = ControlMode.Ui;
        [ProtoMember(6)] public Trigger Trigger;
        [ProtoMember(7)] public bool CountingDown;
        [ProtoMember(8)] public bool CriticalReaction;
        [ProtoMember(9)] public uint ToggleCount;
        [ProtoMember(10)] public ProtoWeaponCompTasks Tasks;

        public void Sync(Weapon.WeaponComponent comp, ProtoWeaponState sync)
        {
            TrackingReticle = sync.TrackingReticle;
            PlayerId = sync.PlayerId;
            Control = sync.Control;
            Trigger = sync.Trigger;
            CountingDown = sync.CountingDown;
            CriticalReaction = sync.CriticalReaction;
            ToggleCount = sync.ToggleCount;

            var wValues = comp.Data.Repo.Values;

            if (ToggleCount > comp.ShootManager.ClientToggleCount) 
                comp.ShootManager.ClientToggleCount = ToggleCount;

            Tasks.Sync(comp, sync.Tasks);
            for (int i = 0; i < sync.Weapons.Length; i++)
            {
                var w = comp.Platform.Weapons[i];
                w.PartState.Sync(w, sync.Weapons[i]);
            }

            comp.ManualMode = wValues.State.TrackingReticle && wValues.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual; // needs to be set everywhere dedicated and non-tracking clients receive TrackingReticle or Control updates.
        }
    }

    [ProtoContract]
    public class ProtoWeaponCompTasks
    {
        public enum Tasks
        {
            None,
            Defend,
            Attack,
            RoamAtPoint,
            Screen,
            Recall,
        }

        [ProtoMember(5)] public long EnemyId;
        [ProtoMember(6)] public long FriendId;
        [ProtoMember(3)] public Vector3D Position;
        [ProtoMember(4)] public Tasks Task;
        public MyEntity Enemy;
        public MyEntity Friend;
        public uint UpdatedTick;
        public void Sync(Weapon.WeaponComponent weaponComponent, ProtoWeaponCompTasks sync)
        {
            var oldEnemyId = EnemyId;
            var oldFriendId = FriendId;
            var oldPosition = Position;

            EnemyId = sync.EnemyId;
            FriendId = sync.FriendId;
            Position = sync.Position;
            Task = sync.Task;
            Update(weaponComponent);

            if (EnemyId != oldEnemyId)
                weaponComponent.AssignEnemy(EnemyId, weaponComponent.Data.Repo.Values.State.PlayerId);

            if (FriendId != oldFriendId)
                weaponComponent.AssignFriend(FriendId, weaponComponent.Data.Repo.Values.State.PlayerId);

            //if (FriendId != oldFriendId)
            //    weaponComponent.AssignPoint(Position, weaponComponent.Data.Repo.Values.State.PlayerId);
        }

        public void Update(Weapon.WeaponComponent weaponComponent)
        {
            if (EnemyId <= 0 || !MyEntities.TryGetEntityById(EnemyId, out Enemy))
            {
                Enemy = null;
                EnemyId = 0;
            }

            if (FriendId <= 0 || !MyEntities.TryGetEntityById(FriendId, out Friend))
            {
                Friend = null;
                FriendId = 0;
            }

            UpdatedTick = Session.I.Tick;
        }

        public bool GetFriend(Session s, out MyEntity friend, out Ai ai)
        {
            friend = Friend;
            var valid = FriendId > 0 && friend != null && !friend.MarkedForClose;
            if (valid && s.EntityAIs.TryGetValue(friend, out ai))
            {
                return true;
            }

            ai = null;
            return valid;
        }

        public bool GetEnemy(Session s, out MyEntity enemy, out Ai ai)
        {
            enemy = Enemy;
            var valid = EnemyId > 0 && enemy != null && !enemy.MarkedForClose;
            if (valid && s.EntityAIs.TryGetValue(enemy, out ai))
            {
                return true;
            }

            ai = null;
            return valid;
        }

        public void Clean()
        {
            UpdatedTick = 0;
            Task = Tasks.None;
            EnemyId = 0;
            FriendId = 0;
            Position = Vector3D.Zero;
            Enemy = null;
            Friend = null;
        }
    }

    [ProtoContract]
    public class ProtoWeaponPartState
    {
        [ProtoMember(1)] public float Heat; // don't save
        [ProtoMember(2)] public bool Overheated; //don't save
        [ProtoMember(4)] public uint Id; //don't save

        public void Sync(Weapon w, ProtoWeaponPartState sync)
        {
            Heat = sync.Heat;
            var wasOver = Overheated;
            Overheated = sync.Overheated;
            var oldId = Id;
            Id = sync.Id;

            if (oldId != Id)
            {
                Session.I.WeaponLookUp[Id] = w;
                w.ProPositionSync.WeaponSyncId = Id;
                w.ProTargetSync.WeaponSyncId = Id;
            }

            if (!wasOver && Overheated)
                w.OverHeatCountDown = 15;
        }
    }

    [ProtoContract]
    public class ProtoWeaponTransferTarget
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public long EntityId;
        //[ProtoMember(3)] public Vector3 TargetPos;
        [ProtoMember(4)] public int PartId;
        [ProtoMember(5)] public WeaponRandomGenerator WeaponRandom; // save
        [ProtoMember(6)] public Vector3D TargetPos;

        internal void SyncTarget(Weapon w)
        {
            w.TargetData.Revision = Revision;
            w.TargetData.EntityId = EntityId;
            w.TargetData.TargetPos = TargetPos;
            w.PartId = PartId;
            w.TargetData.WeaponRandom.Sync(WeaponRandom);

            var target = w.Target;

            var isProjectile = EntityId == -1;
            var noTarget = EntityId == 0;
            var isFakeTarget = EntityId == -2;
            var aConst = w.ActiveAmmoDef.AmmoDef.Const;

            if (!aConst.Reloadable && !noTarget)
                w.ProjectileCounter = 0;

            if (isProjectile)
            {
                target.ProjectileEndTick = 0;
                target.SoftProjetileReset = false;
                target.TargetState = Target.TargetStates.IsProjectile;
            }
            else if (noTarget && target.TargetState == Target.TargetStates.IsProjectile)
            {
                target.SoftProjetileReset = true;
                target.ProjectileEndTick = Session.I.Tick + 62;
                target.TargetState = Target.TargetStates.WasProjectile;
            }
            else
            {
                target.ProjectileEndTick = 0;
                target.SoftProjetileReset = false;

                if (noTarget)
                    target.TargetState = Target.TargetStates.None;
                else if (isFakeTarget)
                    target.TargetState = Target.TargetStates.IsFake;
                else
                    target.TargetState = Target.TargetStates.IsEntity;
            }

            target.TargetPos = TargetPos;
            target.ClientDirty = true;
        }

        internal void ClearTarget()
        {
            ++Revision;
            EntityId = 0;
            TargetPos = Vector3D.Zero;
        }
    }

    [ProtoContract]
    public class ProtoWeaponOverrides
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

        public enum ObjectiveModes
        {
            Default,
            Disabled,
            Destroyed,
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
        [ProtoMember(15)] public bool ShareFireControl;
        [ProtoMember(16)] public bool Repel;
        //[ProtoMember(17)] public long CameraChannel;
        [ProtoMember(18)] public bool Debug;
        //[ProtoMember(19)] public long LeadGroup;
        [ProtoMember(20)] public bool Armed;
        //[ProtoMember(26)] public long ArmedTimer;
        [ProtoMember(22)] public bool Override;
        [ProtoMember(23), DefaultValue(1)] public int BurstCount = 1;
        [ProtoMember(24)] public int BurstDelay;
        [ProtoMember(25), DefaultValue(-1)] public int SequenceId = -1;
        [ProtoMember(26)] public int ArmedTimer;
        [ProtoMember(27)] public int LeadGroup;
        [ProtoMember(28), DefaultValue(Weapon.ShootManager.ShootModes.AiShoot)] public Weapon.ShootManager.ShootModes ShootMode = Weapon.ShootManager.ShootModes.AiShoot;
        [ProtoMember(29)] public int CameraChannel;
        [ProtoMember(30)] public int WeaponGroupId;
        [ProtoMember(31)] public bool AiEnabled = true;
        [ProtoMember(32), DefaultValue(true)] public bool LargeGrid = true;
        [ProtoMember(33), DefaultValue(true)] public bool SmallGrid = true;
        [ProtoMember(34)] public bool AngularTracking;
        [ProtoMember(35), DefaultValue(true)] public bool SupportingPD = true;
        [ProtoMember(36), DefaultValue(ObjectiveModes.Default)] public ObjectiveModes ObjectiveMode = ObjectiveModes.Default;




        public void Sync(ProtoWeaponOverrides syncFrom)
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
            Biologicals = syncFrom.Biologicals;
            Projectiles = syncFrom.Projectiles;
            Repel = syncFrom.Repel;
            CameraChannel = syncFrom.CameraChannel;
            Debug = syncFrom.Debug;
            Override = syncFrom.Override;
            LeadGroup = syncFrom.LeadGroup;
            Armed = syncFrom.Armed;
            ArmedTimer = syncFrom.ArmedTimer;
            BurstCount = syncFrom.BurstCount;
            BurstDelay = syncFrom.BurstDelay;
            SequenceId = syncFrom.SequenceId;
            ShootMode = syncFrom.ShootMode;
            WeaponGroupId = syncFrom.WeaponGroupId;
            AiEnabled = syncFrom.AiEnabled;
            ShareFireControl = syncFrom.ShareFireControl;
            LargeGrid = syncFrom.LargeGrid;
            SmallGrid = syncFrom.SmallGrid;
            SupportingPD = syncFrom.SupportingPD;
            ObjectiveMode = syncFrom.ObjectiveMode;
        }
    }
}

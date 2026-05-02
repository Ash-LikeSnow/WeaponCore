using System;
using System.Collections.Generic;
using System.ComponentModel;
using CoreSystems.Projectiles;
using CoreSystems.Settings;
using CoreSystems.Support;
using ProtoBuf;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Support;
using static CoreSystems.Support.CoreComponent;

namespace CoreSystems
{
    public enum PacketType
    {
        Invalid,
        WeaponComp,
        WeaponState,
        WeaponReload,
        WeaponAmmo,
        UpgradeComp,
        UpgradeState,
        SupportComp,
        SupportState,
        AiData,
        Construct,
        ConstructFoci,
        TargetChange,
        RequestSetDps,
        RequestSetReportTarget,
        RequestSetRof,
        RequestSetGravity,
        RequestSetOverload,
        RequestSetRange,
        OverRidesUpdate,
        AimTargetUpdate,
        PaintedTargetUpdate,
        ActiveControlUpdate,
        PlayerIdUpdate,
        FocusUpdate,
        FocusLockUpdate,
        ReticleUpdate,
        CountingDownUpdate,
        ClientAiAdd,
        ClientAiRemove,
        RequestShootUpdate,
        ReleaseActiveUpdate,
        AmmoCycleRequest,
        PlayerControlRequest,
        FixedWeaponHitEvent,
        ProblemReport,
        TerminalMonitor,
        ClientNotify,
        ServerData,
        ShootSync,
        EwaredBlocks,
        ClientReady,
        ControlComp,
        ControlState,
        ForceReload,
        ControlOnOff,
        BlackListRequest,
        RequestDroneSet,
        PingPong,
        ShootingChanged,
        AdvProjectileSpawnSyncs,
        AdvProjectileDeathSyncs,
        AdvProjectileUpdateTargetSyncs,
        AdvProjectilePositionSyncs,
        ClientAmmoRequest,
        WeaponHeatSync
    }

    #region Packets
    
    [ProtoContract]
    [ProtoInclude(6, typeof(BoolUpdatePacket))]
    [ProtoInclude(7, typeof(FakeTargetPacket))]
    [ProtoInclude(8, typeof(FocusPacket))]
    [ProtoInclude(9, typeof(WeaponIdPacket))]
    [ProtoInclude(11, typeof(AiDataPacket))]
    [ProtoInclude(12, typeof(FixedWeaponHitPacket))]
    [ProtoInclude(13, typeof(ProblemReportPacket))]
    [ProtoInclude(14, typeof(AmmoCycleRequestPacket))]
    [ProtoInclude(15, typeof(ShootStatePacket))]
    [ProtoInclude(16, typeof(OverRidesPacket))]
    [ProtoInclude(17, typeof(PlayerControlRequestPacket))]
    [ProtoInclude(18, typeof(TerminalMonitorPacket))]
    [ProtoInclude(19, typeof(WeaponCompPacket))]
    [ProtoInclude(20, typeof(WeaponStatePacket))]
    [ProtoInclude(21, typeof(TargetPacket))]
    [ProtoInclude(22, typeof(ConstructPacket))]
    [ProtoInclude(23, typeof(ConstructFociPacket))]
    [ProtoInclude(24, typeof(FloatUpdatePacket))]
    [ProtoInclude(25, typeof(ClientNotifyPacket))]
    [ProtoInclude(26, typeof(ServerPacket))]
    [ProtoInclude(27, typeof(WeaponReloadPacket))]
    [ProtoInclude(28, typeof(IntUpdatePacket))]
    [ProtoInclude(29, typeof(WeaponAmmoPacket))]
    [ProtoInclude(30, typeof(UpgradeCompPacket))]
    [ProtoInclude(31, typeof(UpgradeStatePacket))]
    [ProtoInclude(32, typeof(SupportCompPacket))]
    [ProtoInclude(33, typeof(SupportStatePacket))]
    [ProtoInclude(34, typeof(EwaredBlocksPacket))]
    [ProtoInclude(35, typeof(ClientReadyPacket))]
    [ProtoInclude(36, typeof(PaintedTargetPacket))]
    [ProtoInclude(38, typeof(ULongUpdatePacket))]
    [ProtoInclude(39, typeof(ControlCompPacket))]
    [ProtoInclude(40, typeof(ControlStatePacket))]
    [ProtoInclude(41, typeof(BlackListPacket))]
    [ProtoInclude(42, typeof(DronePacket))]
    [ProtoInclude(44, typeof(PingPacket))]
    [ProtoInclude(46, typeof(ShootingChangedPacket))]
    [ProtoInclude(47, typeof(AdvProjectileSpawnPacket))]
    [ProtoInclude(48, typeof(AdvProjectileDeathPacket))]
    [ProtoInclude(49, typeof(AdvProjectileUpdateTargetPacket))]
    [ProtoInclude(50, typeof(AdvProjectilePositionBatchPacket))]
    [ProtoInclude(51, typeof(ClientAmmoRequestPacket))]
    [ProtoInclude(52, typeof(WeaponAmmoPacket))]
    [ProtoInclude(53, typeof(WeaponHeatSyncPacket))]
    public class Packet
    {
        [ProtoMember(1)] internal long EntityId;
        [ProtoMember(2)] internal ulong SenderId;
        [ProtoMember(3)] internal PacketType PType;

        public virtual void CleanUp()
        {
            EntityId = 0;
            SenderId = 0;
            PType = PacketType.Invalid;
        }

        //can override in other packet
        protected bool Equals(Packet other)
        {
            return (EntityId.Equals(other.EntityId) && SenderId.Equals(other.SenderId) && PType.Equals(other.PType));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Packet)obj);
        }

        public override int GetHashCode()
        {
            return (EntityId.GetHashCode() + PType.GetHashCode() + SenderId.GetHashCode());
        }
    }

    [ProtoContract]
    public class PingPacket : Packet
    {
        [ProtoMember(1)] internal float RelativeTime;
        [ProtoMember(2)] internal float OwlTicks;

        
        public override void CleanUp()
        {
            base.CleanUp();
            OwlTicks = 0;
        }
    }

    internal enum AdvSyncFakeType : byte
    {
        Invalid = 0,
        Manual = 1,
        Painted = 2
    }
    
    [ProtoContract]
    internal struct AdvSyncTargetInfo
    {
        [ProtoMember(1)] public AdvTargetType Type;
        /// <summary>
        ///     The EntityId of the grid (directly taken from the <see cref="Target.TargetObject"/> when it is an entity.
        /// </summary>
        [ProtoMember(2)] public long EntityId;
        /// <summary>
        ///     The NetId of the projectile (directly taken from the <see cref="Target.TargetObject"/> when it is an AdvSync projectile.
        /// </summary>
        [ProtoMember(3)] public ulong ProjectileNetId;
        /// <summary>
        ///     The grid (actually, mik painted a suit once?) the fake target is attached to:
        /// </summary>
        [ProtoMember(4)] public long FakeEntityId;
        /// <summary>
        ///     The targeted point in the entity's local frame:
        /// </summary>
        [ProtoMember(5)] public Vector3D FakeLocalPos;
        /// <summary>
        ///     The current position of the target in the world frame:
        /// </summary>
        [ProtoMember(6)] public Vector3D FakeWorldPos;
        /// <summary>
        ///     The fake target type. Must be <see cref="AdvSyncFakeType.Manual"/> or <see cref="AdvSyncFakeType.Painted"/>.
        /// </summary>
        [ProtoMember(7)] public AdvSyncFakeType FakeType;
        
        [ProtoMember(8)] public Vector3D LinearVelocity;
        [ProtoMember(9)] public Vector3D Acceleration;

        /// <summary>
        ///     Extracts target info from a projectile's current state.
        /// </summary>
        public static AdvSyncTargetInfo FromProjectile(Projectile p)
        {
            var info = new AdvSyncTargetInfo();
            var target = p.Info.Target;

            var pTarget = target.TargetObject as Projectile;
            var ent = target.TargetObject as MyEntity;
            
            // This should pass:
            if (ent != null && target.TargetState == Target.TargetStates.IsFake)
            {
                DebugLog.Error("AdvSyncTargetInfo$FromProjectile Uncleaned entity target on fake state");
            }
            
            // Determines target type:
            if (pTarget != null && pTarget.Info.AdvSyncId != 0)
            {
                info.Type = AdvTargetType.Projectile;
                info.ProjectileNetId = pTarget.Info.AdvSyncId;
                info.LinearVelocity = pTarget.Velocity;
                
                //DebugLog.Debug($"[FromProjectile] Projectile {p.Info.AdvSyncId} - Projectile Target: {pTarget.Info.AdvSyncId}");
            }
            else if (ent != null)
            {
                info.Type = AdvTargetType.Entity;
                info.EntityId = ent.EntityId;
                info.FakeWorldPos = target.TargetPos; // Reuse for entity center
                
                if (ent.Physics != null)
                {
                    info.LinearVelocity = ent.Physics.LinearVelocity;
                    info.Acceleration = ent.Physics.LinearAcceleration;
                }
                
                //DebugLog.Debug($"[FromProjectile] Projectile {p.Info.AdvSyncId} - Entity Target: {ent.EntityId}");
            }
            else if (target.TargetState == Target.TargetStates.IsFake)
            {
                info.Type = AdvTargetType.Fake;
                info.FakeWorldPos = target.TargetPos;

                // Gets the actual needed information:
                var storage = p.Info.Storage;
               
                if (storage.DummyTargets != null)
                {
                    var fakeTarget = !storage.ManualMode && storage.DummyTargets.PaintedTarget.EntityId != 0 
                        ? storage.DummyTargets.PaintedTarget 
                        : storage.DummyTargets.ManualTarget;
                    
                    info.FakeEntityId = fakeTarget.EntityId;
                    info.FakeLocalPos = fakeTarget.LocalPosition;
                    info.FakeType = fakeTarget.Type == Ai.FakeTarget.FakeType.Painted 
                        ? AdvSyncFakeType.Painted 
                        : AdvSyncFakeType.Manual;
                    
                    var fakeInfo = fakeTarget.FakeInfo;
                    info.LinearVelocity = fakeInfo.LinearVelocity;
                    info.Acceleration = fakeInfo.Acceleration;
                    
                    //DebugLog.Debug($"[FromProjectile] Projectile {p.Info.AdvSyncId} - Fake target: Type={info.FakeType}, EntityId={info.FakeEntityId}, LocalPos={info.FakeLocalPos}, WorldPos={info.FakeWorldPos}");
                }
                else
                {
                    //DebugLog.Debug("AdvSyncTargetInfo$FromProjectile Dummy targets null");
                }
            }
            else
            {
                info.Type = AdvTargetType.None;
                
                //DebugLog.Debug($"[FromProjectile] Projectile {p.Info.AdvSyncId} - NO TARGET");
            }
            
            return info;
        }

        /// <summary>
        ///     Applies this target info to a projectile on the client.
        /// </summary>
        public bool ApplyTo(Projectile p)
        {
            var target = p.Info.Target;
            var storage = p.Info.Storage;
            
            switch (Type)
            {
                case AdvTargetType.Entity:
                {
                    MyEntity targetEnt;
                    if (MyEntities.TryGetEntityById(EntityId, out targetEnt))
                    {
                        target.TargetObject = targetEnt;
                        target.TargetState = Target.TargetStates.IsEntity;
                        target.TargetPos = FakeWorldPos;
                        return true;
                    }

                    DebugLog.Warning($"AdvSyncTargetInfo$ApplyTo could not get Entity {EntityId}");
                    return false;
                }
                case AdvTargetType.Projectile:
                {
                    Projectile targetPro;
                    if (Session.I.ProjectilesByNetId.TryGetValue(ProjectileNetId, out targetPro))
                    {
                        target.TargetObject = targetPro;
                        target.TargetState = Target.TargetStates.IsProjectile;
                        target.TargetPos = targetPro.Position;
                        //targetPro.Seekers.Add(p);
                        return true;
                    }

                    DebugLog.Warning($"AdvSyncTargetInfo$ApplyTo could not get Projectile {ProjectileNetId}");
                    return false;
                }
                case AdvTargetType.Fake:
                {
                    target.TargetObject = null;
                    target.TargetState = Target.TargetStates.IsFake;
                    target.TargetPos = FakeWorldPos;

                    if (storage.DummyTargets == null)
                    {
                        storage.DummyTargets = new Ai.FakeTargets();
                    }

                    if (FakeType == AdvSyncFakeType.Invalid || (byte)FakeType > (byte)AdvSyncFakeType.Painted)
                    {
                        DebugLog.Critical("AdvSyncTargetInfo$ApplyTo got Invalid FakeType");
                        return false;
                    }

                    var fakeTarget = FakeType == AdvSyncFakeType.Painted
                        ? storage.DummyTargets.PaintedTarget
                        : storage.DummyTargets.ManualTarget;

                    fakeTarget.EntityId = FakeEntityId;
                    fakeTarget.LocalPosition = FakeLocalPos;
                    fakeTarget.FakeInfo.WorldPosition = FakeWorldPos;
                    fakeTarget.FakeInfo.LinearVelocity = LinearVelocity;
                    fakeTarget.FakeInfo.Acceleration = Acceleration;

                    storage.ManualMode = FakeType == AdvSyncFakeType.Invalid;
                    
                    if (FakeEntityId != 0)
                    {
                        MyEntities.TryGetEntityById(FakeEntityId, out fakeTarget.TmpEntity);

                        if (fakeTarget.TmpEntity == null)
                        {
                            DebugLog.Warning($"AdvSyncTargetInfo$ApplyTo failed to get fake entity {FakeEntityId}");
                            return false;
                        }
                    }
                    
                    return true;
                }
                case AdvTargetType.None:
                {
                    target.Reset(Session.I.Tick, Target.States.ProjectileNewTarget);
                    return true;
                }
                case AdvTargetType.Invalid:
                default:
                {
                    throw new Exception($"Invalid AdvTargetType {Type}");
                }
            }
        }
    }

    [ProtoContract]
    public class AdvProjectileSpawnPacket : Packet
    {
        [ProtoMember(1)] internal uint WeaponId;
        [ProtoMember(2)] internal int MuzzleId;
        [ProtoMember(3)] internal int AmmoIndex;
        [ProtoMember(4)] internal Vector3D Position;
        [ProtoMember(5)] internal Vector3D Direction;
        [ProtoMember(6)] internal Vector3D Velocity;
        [ProtoMember(7)] internal ulong NetId;
        [ProtoMember(8)] internal ushort SpawnDepth;
        [ProtoMember(9)] internal AdvSyncTargetInfo TargetInfo;
        [ProtoMember(10)] internal XorShiftRandomStruct RandomState;
        
        public override void CleanUp()
        {
            base.CleanUp();
            WeaponId = 0;
            MuzzleId = 0;
            AmmoIndex = 0;
            Position = Vector3D.Zero;
            Direction = Vector3D.Zero;
            Velocity = Vector3D.Zero;
            NetId = 0;
            SpawnDepth = 0;
            TargetInfo = default(AdvSyncTargetInfo);
            RandomState = default(XorShiftRandomStruct);
        }
    }
    
    [ProtoContract]
    public class AdvProjectileDeathPacket : Packet
    {
        [ProtoMember(1)] public ulong NetId;
        [ProtoMember(2)] public long HitEntityId;
        [ProtoMember(3)] public Vector3D HitPositionTarget;
        [ProtoMember(4)] public Vector3D HitVelocityTarget;
        
        public override void CleanUp()
        {
            base.CleanUp();
            NetId = 0;
            HitEntityId = 0;
            HitPositionTarget = Vector3D.Zero;
            HitVelocityTarget = Vector3D.Zero;
        }
    }

    public enum AdvTargetType : byte
    {
        Invalid = 0,
        None = 1,
        Entity = 2,
        Projectile = 3,
        Fake = 4
    }

    [ProtoContract]
    internal class AdvProjectileUpdateTargetPacket : Packet
    {
        [ProtoMember(1)] public ulong NetId;
        [ProtoMember(2)] public AdvSyncTargetInfo Info;

        public override void CleanUp()
        {
            base.CleanUp();
            NetId = 0;
            Info = default(AdvSyncTargetInfo);
        }
    }

    [ProtoContract]
    public struct AdvProjectilePositionFrame
    {
        [ProtoMember(1)] public ulong NetId;
        [ProtoMember(2)] public Vector3D WorldPosition;
        [ProtoMember(3)] public Vector3 Velocity;
        [ProtoMember(4)] public Vector3 PrevVelocity0;
        [ProtoMember(5)] public Vector3 PrevVelocity1;
        [ProtoMember(6)] public Vector3 RandOffsetDir;
        [ProtoMember(7)] public Vector3D OffsetTarget;
    }

    internal struct AdvProjectilePositionSyncEntry
    {
        public MyEntity TopEntity;
        public Projectile Pro;
    }
    
    [ProtoContract]
    public class AdvProjectilePositionBatchPacket : Packet
    {
        [ProtoMember(1)] public uint SequenceId;
        [ProtoMember(2)] public List<AdvProjectilePositionFrame> Data = new List<AdvProjectilePositionFrame>();
        
        public override void CleanUp()
        {
            base.CleanUp();
            SequenceId = 0;
            Data.Clear();
        }
    }
    
    [ProtoContract]
    public class OverRidesPacket : Packet
    {
        [ProtoMember(1)] internal ProtoWeaponOverrides Data;
        [ProtoMember(2), DefaultValue("")] internal string GroupName = "";
        [ProtoMember(3), DefaultValue("")] internal string Setting = "";
        [ProtoMember(4)] internal int Value;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
            GroupName = string.Empty;
            Setting = string.Empty;
            Value = 0;
        }
    }

    [ProtoContract]
    public class DronePacket : Packet
    {
        [ProtoMember(1), DefaultValue("")] internal string Setting = "";
        [ProtoMember(2)] internal long Value;
        public override void CleanUp()
        {
            base.CleanUp();
            Setting = string.Empty;
            Value = 0;
        }
    }

    [ProtoContract]
    public class TargetPacket : Packet
    {
        [ProtoMember(1)] internal ProtoWeaponTransferTarget Target;

        public override void CleanUp()
        {
            base.CleanUp();
            Target = null;
        }
    }


    [ProtoContract]
    public class ConstructPacket : Packet
    {
        [ProtoMember(1)] internal ConstructDataValues Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class ConstructFociPacket : Packet
    {
        [ProtoMember(1)] internal FocusData Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class AmmoCycleRequestPacket : Packet
    {
        [ProtoMember(1)] internal int PartId;
        [ProtoMember(2)] internal int NewAmmoId;
        [ProtoMember(3), DefaultValue(-1)] internal long PlayerId;


        public override void CleanUp()
        {
            base.CleanUp();
            PartId = 0;
            NewAmmoId = 0;
            PlayerId = -1;
        }
    }

    [ProtoContract]
    public class QueuedShotPacket : Packet
    {
        [ProtoMember(1)] internal int PartId;
        [ProtoMember(2), DefaultValue(-1)] internal long PlayerId;


        public override void CleanUp()
        {
            base.CleanUp();
            PartId = 0;
            PlayerId = -1;
        }
    }

    [ProtoContract]
    public class EwaredBlocksPacket : Packet
    {
        [ProtoMember(1)] internal List<EwarValues> Data = new List<EwarValues>(32);

        public EwaredBlocksPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data.Clear();
        }
    }

    [ProtoContract]
    public class WeaponAmmoPacket : Packet
    {
        [ProtoMember(1)] internal ProtoWeaponAmmo Data;
        [ProtoMember(2)] internal int PartId;
        [ProtoMember(3)] internal uint SequenceId;
        /// <summary>
        ///     If true, this is the special packet the server sends after the weapon stops shooting.
        ///     This syncs the final ammo count.
        /// </summary>
        [ProtoMember(4)] internal bool IsBurstStopMarker;
        /// <summary>
        ///     If true, this is an ammo packet from the active sync loop.
        ///     The sending is timed to be after each shot is sent (with a cooldown), so the client resets the firing sequence when seeing this flag.
        /// </summary>
        [ProtoMember(5)] internal bool IsSyncStepMarker;
        
        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
            PartId = 0;
            SequenceId = 0;
            IsBurstStopMarker = false;
            IsSyncStepMarker = false;
        }
    }

    [ProtoContract]
    public class PlayerControlRequestPacket : Packet
    {
        [ProtoMember(1)] internal long PlayerId;
        [ProtoMember(2)] internal ProtoWeaponState.ControlMode Mode;

        public override void CleanUp()
        {
            base.CleanUp();
            Mode = ProtoWeaponState.ControlMode.Ui;
            PlayerId = -1;
        }
    }

    [ProtoContract]
    public class WeaponReloadPacket : Packet
    {
        [ProtoMember(1)] internal ProtoWeaponReload Data;
        [ProtoMember(2)] internal int PartId;
        [ProtoMember(3)] internal uint SequenceId;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
            PartId = 0;
            SequenceId = 0;
        }
    }

    [ProtoContract]
    public class WeaponCompPacket : Packet
    {
        [ProtoMember(1)] internal ProtoWeaponComp Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class WeaponStatePacket : Packet
    {
        [ProtoMember(1)] internal ProtoWeaponState Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class UpgradeCompPacket : Packet
    {
        [ProtoMember(1)] internal ProtoUpgradeComp Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class UpgradeStatePacket : Packet
    {
        [ProtoMember(1)] internal ProtoUpgradeState Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class SupportCompPacket : Packet
    {
        [ProtoMember(1)] internal ProtoSupportComp Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class SupportStatePacket : Packet
    {
        [ProtoMember(1)] internal ProtoSupportState Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class ControlCompPacket : Packet
    {
        [ProtoMember(1)] internal ProtoControlComp Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class ControlStatePacket : Packet
    {
        [ProtoMember(1)] internal ProtoControlState Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class BlackListPacket : Packet
    {
        [ProtoMember(1)] internal string Data;
        [ProtoMember(2)] internal bool Enable;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
            Enable = false;
        }
    }

    [ProtoContract]
    public class ProblemReportPacket : Packet
    {
        public enum RequestType
        {
            SendReport,
            RequestServerReport,
            RequestAllReport,
        }

        [ProtoMember(1)] internal RequestType Type;
        [ProtoMember(2)] internal DataReport Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Type = RequestType.RequestServerReport;
            Data = null;
        }
    }

    [ProtoContract]
    public class BoolUpdatePacket : Packet
    {
        [ProtoMember(1)] internal bool Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = false;
        }
    }

    [ProtoContract]
    public class FloatUpdatePacket : Packet
    {
        [ProtoMember(1)] internal float Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = 0;
        }
    }


    [ProtoContract]
    public class IntUpdatePacket : Packet
    {
        [ProtoMember(1)] internal int Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = 0;
        }
    }


    [ProtoContract]
    public class ULongUpdatePacket : Packet
    {
        [ProtoMember(1)] internal ulong Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = 0;
        }
    }


    [ProtoContract]
    public class ClientNotifyPacket : Packet
    {
        [ProtoMember(1)] internal string Message;
        [ProtoMember(2)] internal string Color;
        [ProtoMember(3)] internal int Duration;
        [ProtoMember(4)] internal bool SoundClick;

        public override void CleanUp()
        {
            base.CleanUp();
            Message = string.Empty;
            Color = string.Empty;
            SoundClick = false;
            Duration = 0;
        }
    }

    [ProtoContract]
    public class ServerPacket : Packet
    {
        [ProtoMember(1)] internal CoreSettings.ServerSettings Data;
        [ProtoMember(2)] internal string VersionString;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
            VersionString = string.Empty;
        }
    }

    [ProtoContract]
    public class FakeTargetPacket : Packet
    {
        [ProtoMember(1)] internal Vector3 Pos;
        [ProtoMember(2)] internal long TargetId;

        public override void CleanUp()
        {
            base.CleanUp();
            Pos = new Vector3();
            TargetId = 0;
        }
    }


    [ProtoContract]
    public class PaintedTargetPacket : Packet
    {
        [ProtoMember(1)] internal Vector3 Pos;
        [ProtoMember(2)] internal long TargetId;

        public override void CleanUp()
        {
            base.CleanUp();
            Pos = new Vector3();
            TargetId = 0;
        }
    }

    [ProtoContract]
    public class FocusPacket : Packet
    {
        [ProtoMember(1)] internal long TargetId;
        [ProtoMember(2), DefaultValue(-1)] internal int FocusId;

        public override void CleanUp()
        {
            base.CleanUp();
            TargetId = 0;
            FocusId = -1;
        }
    }

    [ProtoContract]
    public class WeaponIdPacket : Packet
    {
        [ProtoMember(1)] internal int WeaponId;

        public override void CleanUp()
        {
            base.CleanUp();
            WeaponId = 0;
        }
    }

    [ProtoContract]
    public class ClientReadyPacket : Packet
    {
        [ProtoMember(1)] internal int WeaponId;

        public override void CleanUp()
        {
            base.CleanUp();
            WeaponId = 0;
        }
    }

    [ProtoContract]
    public class AiDataPacket : Packet
    {
        [ProtoMember(1)] internal AiDataValues Data;

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }


    [ProtoContract]
    public class FixedWeaponHitPacket : Packet
    {
        [ProtoMember(1)] internal long HitEnt;
        [ProtoMember(2)] internal Vector3D HitOffset;
        [ProtoMember(3)] internal Vector3 Up;
        [ProtoMember(4)] internal Vector3 Velocity;
        [ProtoMember(5)] internal int MuzzleId;
        [ProtoMember(6)] internal int WeaponId;
        [ProtoMember(7)] internal int AmmoIndex;
        [ProtoMember(8)] internal float MaxTrajectory;
        [ProtoMember(9)] internal double RelativeAge;


        public override void CleanUp()
        {
            base.CleanUp();
            HitEnt = 0;
            HitOffset = Vector3D.Zero;
            Up = Vector3.Zero;
            MuzzleId = 0;
            WeaponId = 0;
            AmmoIndex = 0;
            MaxTrajectory = 0;
            RelativeAge = 0;
        }
    }


    [ProtoContract]
    public class TerminalMonitorPacket : Packet
    {
        public enum Change
        {
            Update,
            Clean,
        }

        [ProtoMember(1)] internal Change State;

        public override void CleanUp()
        {
            base.CleanUp();
            State = Change.Update;
        }
    }

    [ProtoContract]
    public class ShootStatePacket : Packet
    {
        [ProtoMember(1)] internal Trigger Action = Trigger.Off;
        [ProtoMember(2), DefaultValue(-1)] internal long PlayerId = -1;

        public override void CleanUp()
        {
            base.CleanUp();
            Action = Trigger.Off;
            PlayerId = -1;
        }
    }

    [ProtoContract]
    public class ShootingChangedPacket : Packet
    {
        [ProtoMember(1)] internal bool Value;

        public override void CleanUp()
        {
            base.CleanUp();
            Value = false;
        }
    }

    [ProtoContract]
    public class ClientAmmoRequestPacket : Packet
    {
        [ProtoMember(1)] internal int  PartId;
        [ProtoMember(2)] internal uint LastSequenceId;
        
        public override void CleanUp()
        {
            base.CleanUp();
            PartId = 0;
            LastSequenceId = 0;
        }
    }

    [ProtoContract]
    public class WeaponHeatSyncPacket : Packet
    {
        [ProtoMember(1)] internal int PartId;
        [ProtoMember(2)] internal float Heat;
        [ProtoMember(3)] internal bool Overheated;

        public override void CleanUp()
        {
            base.CleanUp();
            PartId = 0;
            Heat = 0;
            Overheated = false;
        }
    }
    
    #endregion
}

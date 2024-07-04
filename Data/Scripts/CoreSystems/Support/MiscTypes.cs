using System;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Comms;

namespace CoreSystems.Support
{
    public class Target
    {
        internal Weapon Weapon;
        internal object TargetObject;
        internal Vector3D TargetPos;
        internal States CurrentState = States.NotSet;
        internal TargetStates TargetState;
        internal bool HasTarget;
        internal bool IsAligned;
        public bool ImpossibleToHit;
        internal bool SoftProjetileReset;
        internal bool TargetChanged;
        internal bool ClientDirty;
        internal bool IsDrone;
        internal uint ChangeTick;
        internal uint ProjectileEndTick;
        internal long TargetId;
        internal long TopEntityId;

        internal double HitShortDist;
        internal double OrigDistance;

        public enum TargetStates
        {
            None,
            IsProjectile,
            WasProjectile,
            IsFake,
            WasFake,
            IsEntity,
            WasEntity,
        }

        public enum States
        {
            NotSet,
            ControlReset,
            Expired,
            WeaponNotReady,
            AnimationOff,
            Designator,
            Acquired,
            NoTargetsSeen,
            RayCheckFailed,
            RayCheckSelfHit,
            RayCheckFriendly,
            RayCheckDistOffset,
            RayCheckVoxel,
            RayCheckProjectile,
            RayCheckDeadBlock,
            RayCheckDistExceeded,
            RayCheckOther,
            RayCheckMiss,
            ServerReset,
            Transfered,
            Fake,
            FiredBurst,
            AiLost,
            Offline,
            LostTracking,
            ProjectileClean,
            ProjetileIntercept,
            ProjectileClose,
            ProjectileNewTarget,
            Projectile,
            Roid,
            Planet,
            FriendlyGrid,
            NeutralGrid,
            EnemyGrid,
            UnOwnedGrid,
            YourGrid,
            FriendlyCharacter,
            NeutralCharacter,
            EnemyCharacter,
            SendingHome,
        }

        internal Target(Weapon weapon = null)
        {
            Weapon = weapon;
        }

        internal void PushTargetToClient(Weapon w)
        {
            w.TargetData.TargetPos = TargetPos;
            w.TargetData.PartId = w.PartId;
            w.TargetData.EntityId = w.Target.TargetId;
            
            if (!w.ActiveAmmoDef.AmmoDef.Const.Reloadable && w.Target.TargetId != 0)
                w.ProjectileCounter = 0;

            Session.I.SendTargetChange(w.Comp, w.PartId);
        }

        internal void ClientUpdate(Weapon w, ProtoWeaponTransferTarget tData)
        {
            if (Session.I.Tick < w.Target.ProjectileEndTick)
            {
                var first = w.Target.SoftProjetileReset;
                if (first)
                {
                    w.TargetData.WeaponRandom.AcquireRandom = new XorShiftRandomStruct((ulong)w.TargetData.WeaponRandom.CurrentSeed);
                    w.Target.SoftProjetileReset = false;
                }


                if (first || Session.I.Tick20)
                {
                    if (Ai.AcquireProjectile(w))
                    {
                        if (w.NewTarget.CurrentState != States.NoTargetsSeen)
                            w.NewTarget.Reset(Session.I.Tick, States.NoTargetsSeen);

                        if (w.Target.CurrentState != States.NoTargetsSeen)
                            w.Target.Reset(Session.I.Tick, States.NoTargetsSeen, !w.Comp.Data.Repo.Values.State.TrackingReticle && w.Comp.Data.Repo.Values.Set.Overrides.Control != ProtoWeaponOverrides.ControlModes.Painter);
                    }
                }
                return;
            }

            MyEntity targetEntity = null;
            if (tData.EntityId <= 0 || MyEntities.TryGetEntityById(tData.EntityId, out targetEntity, true))
            {
                TargetObject = targetEntity;

                if (tData.EntityId == 0)
                {
                    if (w.Target.TargetState == TargetStates.IsFake || w.Target.TargetState == TargetStates.WasFake)
                        w.Target.Reset(Session.I.Tick, States.ServerReset);
                    else
                        w.DelayedTargetResetTick = Session.I.Tick + 30;
                }
                else
                {
                    StateChange(true, tData.EntityId == -2 ? States.Fake : States.Acquired);
                    if (w.Target.TargetState == TargetStates.None && tData.EntityId != 0)
                        w.TargetData.SyncTarget(w);

                    if (w.Target.TargetState == TargetStates.IsProjectile)
                    {
                        if (!Ai.AcquireProjectile(w))
                        {
                            if (w.NewTarget.CurrentState != States.NoTargetsSeen)
                                w.NewTarget.Reset(Session.I.Tick, States.NoTargetsSeen);

                            if (w.Target.CurrentState != States.NoTargetsSeen)
                            {
                                w.Target.Reset(Session.I.Tick, States.NoTargetsSeen, !w.Comp.Data.Repo.Values.State.TrackingReticle && w.Comp.Data.Repo.Values.Set.Overrides.Control != ProtoWeaponOverrides.ControlModes.Painter);
                            }
                        }
                    }
                }

                if (Session.I.Tick != w.Target.ProjectileEndTick)
                    w.TargetData.WeaponRandom.AcquireRandom = new XorShiftRandomStruct((ulong)w.TargetData.WeaponRandom.CurrentSeed);

                ClientDirty = false;
            }

            w.Target.ChangeTick = Session.I.Tick;
        }

        internal void TransferTo(Target target, uint expireTick, bool drone = false)
        {
            if (target.Weapon != null && target.Weapon.System.RadioType == Radio.RadioTypes.Master)
                target.Weapon.CheckForOverLimit();

            target.IsDrone = drone;
            target.TargetObject = TargetObject;
            target.TargetPos = TargetPos;

            target.HitShortDist = HitShortDist;
            target.OrigDistance = OrigDistance;
            target.TopEntityId = TopEntityId;
            target.TargetState = TargetState;

            target.StateChange(HasTarget, CurrentState);
            Reset(expireTick, States.Transfered);
        }

        internal void Set(object target, Vector3D pos, double shortDist, double origDist, long topEntId, bool isFakeTarget = false)
        {
            TargetObject = target;
            TargetPos = pos;
            HitShortDist = shortDist;
            OrigDistance = origDist;
            TopEntityId = topEntId;
            if (TargetObject is Projectile)
                TargetState = TargetStates.IsProjectile;
            else if (isFakeTarget)
                TargetState = TargetStates.IsFake;
            else if (TargetObject  is MyEntity)
                TargetState = TargetStates.IsEntity;
            else
                TargetState = TargetStates.None;

            StateChange(true, States.Acquired);
        }


        internal void SetFake(uint expiredTick, Vector3D pos, Vector3D targetingOrigin)
        {
            Reset(expiredTick, States.Fake, false);
            TargetState = TargetStates.IsFake;
            TargetPos = pos;
            StateChange(true, States.Fake);
        }

        internal void Reset(uint expiredTick, States reason, bool expire = true)
        {
            switch (TargetState)
            {
                case TargetStates.IsProjectile:
                    TargetState = TargetStates.WasProjectile;
                    break;
                case TargetStates.IsFake:
                    TargetState = TargetStates.WasFake;
                    break;
                case TargetStates.IsEntity:
                    TargetState = TargetStates.WasEntity;
                    break;
                default:
                    TargetState = TargetStates.None;
                    break;
            }

            if (Weapon != null)
            {
                if (IsAligned)
                    Weapon.EventTriggerStateChanged(WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TargetAligned, false);

                if (Weapon.RangeEventActive)
                    Weapon.EventTriggerStateChanged(Weapon.PrevRangeEvent, false);

                Weapon.JerkRunningAverage.Clean();
            }

            IsDrone = false;
            IsAligned = false;
            TargetPos = Vector3D.Zero;
            HitShortDist = 0;
            OrigDistance = 0;
            TargetId = 0;
            ChangeTick = expiredTick;
            SoftProjetileReset = false;
            if (expire)
            {
                StateChange(false, reason);
            }
            TopEntityId = 0;
            TargetObject = null;
        }

        internal void StateChange(bool setTarget, States reason)
        {
            SetTargetId(setTarget, reason);
            TargetChanged = !HasTarget && setTarget || HasTarget && !setTarget;

            if (TargetChanged && Weapon != null)
            {
                ChangeTick = Session.I.Tick;

                if (setTarget) {
                    
                    Weapon.FailedAcquires = 0;
                    Weapon.LookAtFailCount = 0;
                    Weapon.HiddenTargets.Clear();
                    Weapon.BaseComp.Ai.WeaponsTracking++;
                    Weapon.BaseComp.PartTracking++;
                }
                else {
                    Weapon.BaseComp.Ai.WeaponsTracking--;
                    Weapon.BaseComp.PartTracking--;
                }
            }

            if (Weapon != null)
            {
                if (Weapon.System.RadioType == Radio.RadioTypes.None && Weapon.System.UniqueTargetPerWeapon)
                    Weapon.AddTargetToBlock(setTarget);
                else if (Weapon.System.RadioType == Radio.RadioTypes.Master)
                    Weapon.StoreTargetOnConstruct(setTarget);
                else if (Weapon.System.RadioType == Radio.RadioTypes.Slave)
                    Weapon.RecordConnection(setTarget);
            }

            HasTarget = setTarget;
            CurrentState = reason;
        }

        internal void SetTargetId(bool setTarget, States reason)
        {
            switch (TargetState)
            {
                case TargetStates.IsProjectile:
                    TargetId = -1;
                    break;
                case TargetStates.IsFake:
                    TargetId = -2;
                    break;
                case TargetStates.IsEntity:
                    TargetId = ((MyEntity)TargetObject).EntityId;
                    break;
                default:
                    TargetId = 0;
                    break;
            }
        }
    }

    public class HitParticleEvent
    {
        public readonly MyParticleEffect Effect;
        public readonly Vector3D Velocity;
        public bool MarkedForClose = false;

        public HitParticleEvent(MyParticleEffect effect, Vector3D velocity)
        {
            Effect = effect;
            Velocity = velocity;
            Effect.OnDelete += Effect_OnDelete;
        }

        private void Effect_OnDelete(MyParticleEffect obj)
        {
            MarkedForClose = true;
            Effect.OnDelete -= Effect_OnDelete;
        }
    }

    public class ParticleEvent
    {
        private readonly Guid _uid;
        public readonly Dummy MyDummy;
        public readonly Vector4 Color;
        public readonly Vector3 Offset;
        public readonly Vector3D EmptyPos;
        public readonly string ParticleName;
        public readonly string EmptyNames;
        public readonly string[] MuzzleNames;
        public readonly string PartName;
        public readonly float MaxPlayTime;
        public readonly uint StartDelay;
        public readonly uint LoopDelay;
        public readonly float Scale;
        public readonly float Distance;
        public readonly bool DoesLoop;
        public readonly bool Restart;
        public readonly bool ForceStop;

        public bool Playing;
        public bool Stop;
        public bool Triggered;
        public uint PlayTick;
        public MyParticleEffect Effect;

        public ParticleEvent(string particleName, string emptyName, Vector4 color, Vector3 offset, float scale, float distance, float maxPlayTime, uint startDelay, uint loopDelay, bool loop, bool restart, bool forceStop, params string[] muzzleNames)
        {
            ParticleName = particleName;
            EmptyNames = emptyName;
            MuzzleNames = muzzleNames;
            Color = color;
            Offset = offset;
            Scale = scale;
            Distance = distance;
            MaxPlayTime = maxPlayTime > 0 ? maxPlayTime : float.MaxValue;
            StartDelay = startDelay;
            LoopDelay = loopDelay;
            DoesLoop = loop;
            Restart = restart;
            ForceStop = forceStop;
            _uid = Guid.NewGuid();
        }

        public ParticleEvent(ParticleEvent copyFrom, Dummy myDummy, string partName, Vector3 pos)
        {
            MyDummy = myDummy;
            PartName = partName;
            EmptyNames = copyFrom.EmptyNames;
            MuzzleNames = copyFrom.MuzzleNames;
            ParticleName = copyFrom.ParticleName;
            Color = copyFrom.Color;
            Offset = copyFrom.Offset;
            EmptyPos = pos;
            Scale = copyFrom.Scale;
            Distance = copyFrom.Distance;
            MaxPlayTime = copyFrom.MaxPlayTime;
            StartDelay = copyFrom.StartDelay;
            LoopDelay = copyFrom.LoopDelay;
            DoesLoop = copyFrom.DoesLoop;
            Restart = copyFrom.Restart;
            ForceStop = copyFrom.ForceStop;
            _uid = Guid.NewGuid();
        }

        protected bool Equals(ParticleEvent other)
        {
            return Equals(_uid, other._uid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ParticleEvent)obj);
        }

        public override int GetHashCode()
        {
            return _uid.GetHashCode();
        }
    }
}

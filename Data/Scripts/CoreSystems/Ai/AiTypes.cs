using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreSystems.Platform;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
namespace CoreSystems.Support
{
    public partial class Ai
    {

        internal class WeaponObStorage
        {
            public int CurrentMagazineAmmunition;
            public int CurrentMagazineAmount;
            public int CurrentAmmunition;
        }

        internal class AiCharger
        {
            internal readonly List<Part> ChargeGroup0 = new List<Part>();
            internal readonly List<Part> ChargeGroup1 = new List<Part>();
            internal readonly List<Part> ChargeGroup2 = new List<Part>();
            internal readonly float[] G0Power = { 1f, 0.75f, 0.5f, 0f };
            internal readonly float[] G1Power = { 1f, 0.25f, 0.25f, 0.5f };
            internal readonly float[] G2Power = { 1f, 0.25f, 0.25f, 0.5f };

            internal float TotalDesired;
            internal float GroupRequested0;
            internal float GroupRequested1;
            internal float GroupRequested2;
            internal int State;
            internal bool Rebalance;

            internal Ai Ai;
            internal AiCharger(Ai ai)
            {
                Ai = ai;
            }

            internal void Add(Part part)
            {
                part.InCharger = true;
                if (ChargeGroup0.Count == 0 && ChargeGroup1.Count == 0 && ChargeGroup2.Count == 0) {
                    Session.I.ChargingParts.Add(this);
                    Session.I.ChargingParts.ApplyAdditions();
                }

                switch (part.BaseComp.PowerGroupId)
                {
                    case 0:
                        ChargeGroup0.Add(part);
                        GroupRequested0 += part.DesiredPower;

                        break;
                    case 1:
                        ChargeGroup1.Add(part);
                        GroupRequested1 += part.DesiredPower;
                        break;
                    case 2:
                        ChargeGroup2.Add(part);
                        GroupRequested2 += part.DesiredPower;
                        break;
                }

                TotalDesired += part.DesiredPower;
                Rebalance = true;

                if (!MyUtils.IsZero(GroupRequested0) && !MyUtils.IsZero(GroupRequested1) && !MyUtils.IsZero(GroupRequested2))
                    State = 2;
                else if (!MyUtils.IsZero(GroupRequested0) && (!MyUtils.IsZero(GroupRequested1) || !MyUtils.IsZero(GroupRequested2)))
                    State = 1;
                else if (!MyUtils.IsZero(GroupRequested1) && !MyUtils.IsZero(GroupRequested2))
                    State = 3;
                else State = 0;
            }

            internal void Remove(Part part, int i)
            {
                switch (part.BaseComp.PowerGroupId)
                {
                    case 0:
                        ChargeGroup0.RemoveAtFast(i);
                        GroupRequested0 -= part.DesiredPower;

                        break;
                    case 1:
                        ChargeGroup1.RemoveAtFast(i);
                        GroupRequested1 -= part.DesiredPower;

                        break;
                    case 2:
                        ChargeGroup2.RemoveAtFast(i);
                        GroupRequested2 -= part.DesiredPower;
                        break;
                }

                Rebalance = true;

                if (ChargeGroup0.Count == 0 && ChargeGroup1.Count == 0 && ChargeGroup2.Count == 0) {

                    Session.I.ChargingParts.Remove(this);
                    GroupRequested0 = 0;
                    GroupRequested1 = 0;
                    GroupRequested2 = 0;
                    Rebalance = false;
                    TotalDesired = 0;
                }

                part.ExitCharger = false;
                part.InCharger = false;

                if (part.BaseComp.Type != CoreComponent.CompType.Weapon || ((Weapon)part).ReloadEndTick >= uint.MaxValue - 1)
                    part.Loading = false;

                if (!MyUtils.IsZero(GroupRequested0) && !MyUtils.IsZero(GroupRequested1) && !MyUtils.IsZero(GroupRequested2))
                    State = 2;
                else if (!MyUtils.IsZero(GroupRequested0) && (!MyUtils.IsZero(GroupRequested1) || !MyUtils.IsZero(GroupRequested2)))
                    State = 1;
                else if (!MyUtils.IsZero(GroupRequested1) && !MyUtils.IsZero(GroupRequested2))
                    State = 3;
                else State = 0;
            }

            internal void Clean()
            {
                ChargeGroup0.Clear();
                ChargeGroup1.Clear();
                ChargeGroup2.Clear();
                GroupRequested0 = 0;
                GroupRequested1 = 0;
                GroupRequested2 = 0;
                Rebalance = false;
                TotalDesired = 0;
            }
        }

        internal struct DbScan
        {
            internal Ai Ai;   
            internal int Version;
        }

        internal class PartCounter
        {
            internal int Current;
            internal int Max;
        }

        public class FakeTargets
        {
            public readonly FakeTarget ManualTarget = new FakeTarget(FakeTarget.FakeType.Manual);
            public readonly FakeTarget PaintedTarget = new FakeTarget(FakeTarget.FakeType.Painted);
        }

        public class FakeTarget
        {

            public FakeTarget(FakeType type)
            {
                Type = type;
            }

            public enum FakeType
            {
                Manual,
                Painted,
            }

            public readonly FakeWorldTargetInfo FakeInfo = new FakeWorldTargetInfo();
            public readonly FakeType Type;
            public Vector3D LocalPosition;
            public long EntityId;
            public MyEntity TmpEntity;
            public uint LastUpdateTick;
            public uint LastInfoTick;
            public int MissCount;

            internal void Update(Vector3D hitPos, uint tick, MyEntity ent = null, long entId = 0)
            {
                if ((ent?.Physics != null || entId != 0 && MyEntities.TryGetEntityById(entId, out ent)) && ent.Physics != null)
                {
                    var referenceWorldMatrix = ent.PositionComp.WorldMatrixRef;
                    Vector3D referenceWorldPosition = referenceWorldMatrix.Translation;
                    Vector3D worldDirection = hitPos - referenceWorldPosition;
                    TmpEntity = ent;
                    EntityId = ent.EntityId;
                    LocalPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(referenceWorldMatrix));
                }
                else
                {
                    if (Type == FakeType.Manual)
                        FakeInfo.WorldPosition = hitPos;

                    TmpEntity = null;
                    EntityId = 0;
                    LocalPosition = Vector3D.Zero;
                    FakeInfo.LinearVelocity = Vector3.Zero;
                    FakeInfo.Acceleration = Vector3.Zero;
                }

                LastInfoTick = 0;
                MissCount = 0;
                LastUpdateTick = tick;
            }

            internal void Sync(FakeTargetPacket packet, Ai ai)
            {
                if (packet.TargetId == 0)
                {

                    EntityId = 0;
                    LocalPosition = Vector3D.Zero;
                    FakeInfo.WorldPosition = packet.Pos;
                    FakeInfo.LinearVelocity = Vector3.Zero;
                    FakeInfo.Acceleration = Vector3.Zero;
                }
                else
                {
                    EntityId = packet.TargetId;
                    MyEntities.TryGetEntityById(EntityId, out TmpEntity, true);
                    LocalPosition = packet.Pos;
                }

                LastInfoTick = 0;
                MissCount = 0;
                LastUpdateTick = Session.I.Tick;
            }

            internal void Sync(PaintedTargetPacket packet, Ai ai)
            {
                if (packet.TargetId == 0)
                {
                    EntityId = 0;
                    LocalPosition = Vector3D.Zero;
                    FakeInfo.WorldPosition = packet.Pos;
                    FakeInfo.LinearVelocity = Vector3.Zero;
                    FakeInfo.Acceleration = Vector3.Zero;
                }
                else
                {
                    EntityId = packet.TargetId;
                    MyEntities.TryGetEntityById(EntityId, out TmpEntity, true);
                    LocalPosition = packet.Pos;
                }
                LastInfoTick = 0;
                MissCount = 0;
                LastUpdateTick = Session.I.Tick;
            }

            internal FakeWorldTargetInfo GetFakeTargetInfo(Ai ai)
            {
                var cleared = false;
                if (TmpEntity != null && EntityId == TmpEntity.EntityId && TmpEntity.Physics != null && !TmpEntity.MarkedForClose && !TmpEntity.Closed)
                {
                    if (Session.I.Tick != LastInfoTick)
                    {
                        LastInfoTick = Session.I.Tick;
                        if (Type != FakeType.Painted || ai.Targets.ContainsKey(TmpEntity))
                        {
                            FakeInfo.WorldPosition = Vector3D.Transform(LocalPosition, TmpEntity.PositionComp.WorldMatrixRef);
                            FakeInfo.LinearVelocity = TmpEntity.Physics.LinearVelocity;
                            FakeInfo.Acceleration = TmpEntity.Physics.LinearAcceleration;
                        }
                        else if (Type == FakeType.Painted && EntityId != 0)
                        {
                            ClearMark(Session.I.Tick, MarkClearResons.PaintSwitchOrNoTargetInDb);
                            cleared = true;
                        }
                    }
                }
                else if (Type == FakeType.Painted && EntityId != 0)
                {
                    ClearMark(Session.I.Tick, MarkClearResons.NoTarget);
                    cleared = true;
                }

                return cleared ? null : FakeInfo;
            }

            internal enum MarkClearResons
            {
                PaintSwitchOrNoTargetInDb,
                NoTarget,
                NoStickyRayFailure,
            }
            internal void ClearMark(uint tick, MarkClearResons reason)
            {
                TmpEntity = null;
                EntityId = 0;
                MissCount = 0;
                LastInfoTick = 0;
                LastUpdateTick = tick;
                LocalPosition = Vector3D.Zero;
                FakeInfo.WorldPosition = Vector3D.Zero;
                FakeInfo.LinearVelocity = Vector3.Zero;
                FakeInfo.Acceleration = Vector3.Zero;
            }

            public class FakeWorldTargetInfo
            {
                public Vector3D WorldPosition;
                public Vector3 LinearVelocity;
                public Vector3 Acceleration;
            }
        }


        public class AiDetectionInfo
        {
            internal bool PriorityInRange;
            internal double PriorityRangeSqr;
            internal bool OtherInRange;
            internal double OtherRangeSqr;
            internal bool DroneInRange;
            internal double DroneRangeSqr;
            internal bool SomethingInRange;
            internal bool RamProximity;
            internal int DroneCount;

            internal bool ValidSignalExists(Weapon w)
            {
                var signalInRange = !w.Comp.DetectOtherSignals ? PriorityRangeSqr <= w.MaxTargetDistanceSqr : (OtherRangeSqr <= w.MaxTargetDistanceSqr || PriorityRangeSqr <= w.MaxTargetDistanceSqr);
                return signalInRange || w.Comp.MasterAi.Construct.HadFocus || w.Comp.MasterAi.LiveProjectile.Count > 0 && (w.System.TrackProjectile || w.Comp.Ai.ControlComp != null) && w.Comp.Data.Repo.Values.Set.Overrides.Projectiles; 
            }

            internal bool TargetInRange(Weapon w)
            {
                return !w.Comp.DetectOtherSignals ? PriorityRangeSqr <= w.MaxTargetDistanceSqr : (OtherRangeSqr <= w.MaxTargetDistanceSqr || PriorityRangeSqr <= w.MaxTargetDistanceSqr);
            }

            internal void DroneAdd(Ai ai, TargetInfo info)
            {
                var rootConstruct = ai.Construct.RootAi.Construct;
                DroneCount++;

                if (DroneCount > rootConstruct.DroneCount)
                {
                    rootConstruct.DroneCount = DroneCount;
                    rootConstruct.LastDroneTick = Session.I.Tick + 1;
                }

                if (info.DistSqr < 36000000)
                    rootConstruct.DroneAlert = true;
            }

            internal void Clean(Ai ai)
            {
                PriorityRangeSqr = double.MaxValue;
                PriorityInRange = false;
                OtherRangeSqr = double.MaxValue;
                OtherInRange = false;
                DroneInRange = false;
                DroneRangeSqr = double.MaxValue;
                SomethingInRange = false;
                RamProximity = false;
                DroneCount = 0;

                var rootConstruct = ai.Construct.RootAi.Construct;
                if (rootConstruct.DroneCount != 0 && Session.I.Tick - rootConstruct.LastDroneTick > 30) //Was 200, dropped for faster updates on current threats
                    rootConstruct.DroneCleanup();
            }
        }


        internal struct DetectInfo
        {
            internal readonly MyEntity Target;
            internal readonly MyDetectedEntityInfo EntInfo;
            internal readonly int PartCount;
            internal readonly int FatCount;
            internal readonly bool Armed;
            internal readonly bool IsGrid;
            internal readonly bool LargeGrid;
            internal readonly bool SuspectedDrone;

            public DetectInfo(MyEntity target, MyDetectedEntityInfo entInfo, int partCount, int fatCount, bool suspectedDrone, bool loneWarhead)
            {
                Target = target;
                EntInfo = entInfo;
                PartCount = partCount;
                FatCount = fatCount;
                SuspectedDrone = suspectedDrone;
                var armed = false;
                var isGrid = false;
                var largeGrid = false;
                var grid = target as MyCubeGrid;
                if (grid != null)
                {
                    isGrid = true;
                    largeGrid = grid.GridSizeEnum == MyCubeSize.Large;
                    ConcurrentDictionary<WeaponDefinition.TargetingDef.BlockTypes, ConcurrentCachingList<MyCubeBlock>> blockTypeMap;
                    if (Session.I.GridToBlockTypeMap.TryGetValue((MyCubeGrid)Target, out blockTypeMap))
                    {
                        ConcurrentCachingList<MyCubeBlock> weaponBlocks;
                        if (blockTypeMap.TryGetValue(WeaponDefinition.TargetingDef.BlockTypes.Offense, out weaponBlocks) && weaponBlocks.Count > 0)
                            armed = true;
                        else if (blockTypeMap.TryGetValue(WeaponDefinition.TargetingDef.BlockTypes.Utility, out weaponBlocks) && weaponBlocks.Count > 0)
                            armed = true;
                    }
                }
                else if (target is MyMeteor || target is IMyCharacter || loneWarhead) armed = true;

                Armed = armed;
                IsGrid = isGrid;
                LargeGrid = largeGrid;
            }
        }

        internal class TargetCompare : IComparer<TargetInfo>
        {
            public int Compare(TargetInfo x, TargetInfo y)
            {
                var xDroneThreat = (x.Approaching && x.DistSqr < 9000000 || x.DistSqr < 1000000) && x.Drone;
                var yDroneThreat = (y.Approaching && y.DistSqr < 9000000 || y.DistSqr < 1000000) && y.Drone;
                var droneCompare = xDroneThreat.CompareTo(yDroneThreat);

                if (droneCompare != 0) return -droneCompare;

                var gridCompare = (x.Target is MyCubeGrid).CompareTo(y.Target is MyCubeGrid);
                if (gridCompare != 0) return -gridCompare;
                var xCollision = x.Approaching && x.DistSqr < 90000 && x.VelLenSqr > 100;
                var yCollision = y.Approaching && y.DistSqr < 90000 && y.VelLenSqr > 100;
                var collisionRisk = xCollision.CompareTo(yCollision);
                if (collisionRisk != 0) return collisionRisk;

                var xIsImminentThreat = x.Approaching && x.DistSqr < 640000 && x.OffenseRating > 0;
                var yIsImminentThreat = y.Approaching && y.DistSqr < 640000 && y.OffenseRating > 0;
                var imminentThreat = -xIsImminentThreat.CompareTo(yIsImminentThreat);
                if (imminentThreat != 0) return imminentThreat;

                var compareOffense = x.OffenseRating.CompareTo(y.OffenseRating);
                return -compareOffense;
            }
        }

        internal class TargetInfo
        {
            internal Sandbox.ModAPI.Ingame.MyDetectedEntityInfo EntInfo;
            internal Vector3D TargetHeading;
            internal Vector3D TargetPos;
            internal Vector3D Velocity;
            internal double DistSqr;
            internal double VelLenSqr;
            internal double TargetRadius;
            internal bool IsGrid;
            internal bool LargeGrid;
            internal bool Approaching;
            internal bool IsStatic;
            internal bool Drone;
            internal int PartCount;
            internal int FatCount;
            internal int LosHits;
            internal float OffenseRating;
            internal MyEntity Target;
            internal Ai MyAi;
            internal Ai TargetAi;
            internal void Init(ref DetectInfo detectInfo, Ai myAi, Ai targetAi)
            {
                EntInfo = detectInfo.EntInfo;
                Target = detectInfo.Target;
                PartCount = detectInfo.PartCount;
                FatCount = detectInfo.FatCount;

                IsStatic = Target.Physics.IsStatic;
                IsGrid = detectInfo.IsGrid;
                LargeGrid = detectInfo.LargeGrid;
                MyAi = myAi;
                TargetAi = targetAi;
                Velocity = Target.Physics.LinearVelocity;
                VelLenSqr = Velocity.LengthSquared();
                LosHits = 0;
                var targetSphere = Target.PositionComp.WorldVolume;
                TargetPos = targetSphere.Center;
                TargetRadius = targetSphere.Radius;
                var myCenter = myAi.TopEntityVolume.Center;

                if (!MyUtils.IsZero(Velocity, 1E-01F))
                {
                    var targetMag = myCenter - TargetPos;
                    Approaching = MathFuncs.IsDotProductWithinTolerance(ref Velocity, ref targetMag, Session.I.ApproachDegrees);
                }
                else
                {
                    Approaching = false;
                    TargetHeading = Vector3D.Zero;
                }

                if (targetAi != null)
                {
                    OffenseRating = targetAi.Construct.OptimalDps / myAi.Construct.OptimalDps;
                    if (OffenseRating <= 0 && detectInfo.Armed)
                        OffenseRating = 0.0001f;
                }
                else if (detectInfo.Armed) OffenseRating = 0.0001f;
                else if (Approaching && VelLenSqr >= 1225) OffenseRating = 0.0001f;
                else OffenseRating = 0;
                var myRadius = myAi.TopEntity.PositionComp.LocalVolume.Radius;
                var sphereDistance = MyUtils.GetSmallestDistanceToSphere(ref myCenter, ref targetSphere);
                if (sphereDistance <= myRadius)
                    sphereDistance = 0;
                else sphereDistance -= myRadius;
                DistSqr = sphereDistance * sphereDistance;

                Drone = (VelLenSqr >= 900 && Approaching && DistSqr < 1000000) && detectInfo.SuspectedDrone;

                if (Drone && OffenseRating < 10)
                    OffenseRating = 10;
            }

            internal void Clean()
            {
                Target = null;
                MyAi = null;
                TargetAi = null;
            }
        }


        internal class WeaponGroup
        {
            internal readonly Dictionary<int, WeaponSequence> Sequences = new Dictionary<int, WeaponSequence>();
            internal readonly List<int> OrderSequencesIds = new List<int>();

            internal int SequenceStep;
            internal int WeaponsFinished;

            public void Clean()
            {
                foreach (var s in Sequences.Values)
                   s.Clean();

                OrderSequencesIds.Clear();
                Sequences.Clear();
                WeaponsFinished = 0;
                SequenceStep = 0;
                Session.I.GroupPool.Push(this);
            }
        }

        internal class WeaponSequence
        {
            internal int TotalWeapons;
            internal int WeaponsFinished;
            internal int ShotsFired;
            internal Dictionary<Weapon.WeaponComponent, int> Weapons = new Dictionary<Weapon.WeaponComponent, int>();

            public void AddWeapon(Weapon.WeaponComponent comp)
            {
                Weapons.Add(comp, 0);
                TotalWeapons = Weapons.Count;
            }

            public void Clean()
            {
                WeaponsFinished = 0;
                TotalWeapons = 0;
                ShotsFired = 0;
                Weapons.Clear();
                Session.I.SequencePool.Push(this);
            }
        }

        internal struct QueuedSoundEvent
        {
            internal enum SoundTypes
            {
                HardPointStart,
                HardPointStop,
            }

            internal SoundTypes Type;
            internal Weapon Weapon;
        }

        internal struct Shields
        {
            internal long Id;
            internal MyEntity ShieldEnt;
            internal MyCubeBlock ShieldBlock;
        }

        public struct PlayerController
        {
            public enum ChangeType
            {
                Add,
                Remove,
            }

            internal long Id;
            internal long EntityId;
            internal uint ChangeTick;
            internal bool ShareControl;
            internal MyEntity ControlEntity;
            internal ChangeType LastChangeReason;
        }

        public struct PlayerControllerEntity
        {
            internal long Id;
            internal long EntityId;
            internal uint ChangeTick;
            internal MyEntity ControllEntity;
        }
    }
}

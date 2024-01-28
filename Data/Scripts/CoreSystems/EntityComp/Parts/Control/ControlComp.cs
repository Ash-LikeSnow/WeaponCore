using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;

namespace CoreSystems.Platform
{
    public partial class ControlSys
    {
        public class ControlComponent : CoreComponent
        {
            internal readonly List<Sandbox.ModAPI.Interfaces.ITerminalAction> Actions = new List<Sandbox.ModAPI.Interfaces.ITerminalAction>();
            internal readonly HashSet<MyEntity> ToolsAndWeapons = new HashSet<MyEntity>();

            internal readonly ControlCompData Data;
            internal readonly ControlStructure Structure;
            internal readonly IMyTurretControlBlock Controller;
            internal bool RotorsMoving;
            internal uint LastOwnerRequestTick;
            internal uint LastAddTick;

            internal ControlComponent(MyEntity coreEntity, MyDefinitionId id)
            {
                Controller = (IMyTurretControlBlock)coreEntity;
                Controller.AIEnabled = false;
                //Bellow order is important
                Data = new ControlCompData(this);
                Init(coreEntity, true, Data, id);
                Structure = (ControlStructure)Platform.Structure;
            }

            internal void DetectStateChanges()
            {
                if (Platform.State != CorePlatform.PlatformState.Ready)
                    return;

                if (Session.I.Tick - Ai.LastDetectEvent > 59)
                {
                    if (Data.Repo.Values.Set.Overrides.Projectiles)
                        Ai.PointDefense = true;

                    Ai.LastDetectEvent = Session.I.Tick;
                    Ai.SleepingComps = 0;
                    Ai.AwakeComps = 0;
                    Ai.DetectOtherSignals = false;
                }

                UpdatedState = true;

                DetectOtherSignals = false;
                if (DetectOtherSignals)
                    Ai.DetectOtherSignals = true;

                var wasAsleep = IsAsleep;
                IsAsleep = false;
                IsDisabled = false;

                if (!Session.I.IsServer)
                    return;

                var otherRangeSqr = Ai.DetectionInfo.OtherRangeSqr;
                var priorityRangeSqr = Ai.DetectionInfo.PriorityRangeSqr;
                var somethingInRange = DetectOtherSignals ? otherRangeSqr <= MaxDetectDistanceSqr && otherRangeSqr >= MinDetectDistanceSqr || priorityRangeSqr <= MaxDetectDistanceSqr && priorityRangeSqr >= MinDetectDistanceSqr : priorityRangeSqr <= MaxDetectDistanceSqr && priorityRangeSqr >= MinDetectDistanceSqr;

                if (Session.I.Settings.Enforcement.ServerSleepSupport && !somethingInRange && PartTracking == 0 && Ai.Construct.RootAi.Construct.ControllingPlayers.Count <= 0 && Session.I.TerminalMon.Comp != this && Data.Repo.Values.State.Terminal == Trigger.Off)
                {

                    IsAsleep = true;
                    Ai.SleepingComps++;
                }
                else if (wasAsleep)
                {

                    Ai.AwakeComps++;
                }
                else
                    Ai.AwakeComps++;
            }

            internal void StopRotors()
            {
                RotorsMoving = false;
                if (Platform.Control.BaseMap != null)
                    Platform.Control.BaseMap.TargetVelocityRad = 0;

                if (Platform.Control.OtherMap != null)
                    Platform.Control.OtherMap.TargetVelocityRad = 0;
            }

            internal static void RequestSetValue(ControlComponent comp, string setting, int value, long playerId)
            {
                if (Session.I.IsServer)
                {
                    SetValue(comp, setting, value, playerId);
                }
                else if (Session.I.IsClient)
                {
                    Session.I.SendOverRidesClientComp(comp, setting, value);
                }
            }

            internal static void SetValue(ControlComponent comp, string setting, int v, long playerId)
            {
                var o = comp.Data.Repo.Values.Set.Overrides;
                var enabled = v > 0;
                var clearTargets = false;

                switch (setting)
                {
                    case "MaxSize":
                        o.MaxSize = v;
                        break;
                    case "MinSize":
                        o.MinSize = v;
                        break;
                    case "SubSystems":
                        o.SubSystem = (WeaponDefinition.TargetingDef.BlockTypes)v;
                        break;
                    case "MovementModes":
                        o.MoveMode = (ProtoWeaponOverrides.MoveModes)v;
                        clearTargets = true;
                        break;
                    case "ControlModes":
                        o.Control = (ProtoWeaponOverrides.ControlModes)v;
                        clearTargets = true;
                        break;
                    case "FocusSubSystem":
                        o.FocusSubSystem = enabled;
                        break;
                    case "FocusTargets":
                        o.FocusTargets = enabled;
                        clearTargets = true;
                        break;
                    case "Unowned":
                        o.Unowned = enabled;
                        break;
                    case "Friendly":
                        o.Friendly = enabled;
                        clearTargets = true;
                        break;
                    case "Meteors":
                        o.Meteors = enabled;
                        break;
                    case "Grids":
                        o.Grids = enabled;
                        break;
                    case "Biologicals":
                        o.Biologicals = enabled;
                        break;
                    case "Projectiles":
                        o.Projectiles = enabled;
                        clearTargets = true;
                        break;
                    case "Neutrals":
                        o.Neutrals = enabled;
                        clearTargets = true;
                        break;
                    case "ShareFireControl":
                        o.ShareFireControl = enabled;
                        break;
                    case "AiEnabled":
                        o.AiEnabled = enabled;
                        break;
                    case "ShootMode":
                        o.ShootMode = (Weapon.ShootManager.ShootModes)v;
                        break;
                    case "LargeGrid":
                        o.LargeGrid = enabled;
                        clearTargets = true;
                        break;
                    case "SmallGrid":
                        o.SmallGrid = enabled;
                        clearTargets = true;
                        break;
                }

                ResetCompState(comp, playerId, clearTargets);

                if (Session.I.MpActive)
                    Session.I.SendComp(comp);
            }

            internal static void ResetCompState(ControlComponent comp, long playerId, bool resetTarget, Dictionary<string, int> settings = null)
            {
                var o = comp.Data.Repo.Values.Set.Overrides;
                var userControl = o.Control != ProtoWeaponOverrides.ControlModes.Auto;

                if (userControl)
                {
                    comp.Data.Repo.Values.State.PlayerId = playerId;
                    comp.Data.Repo.Values.State.Mode = ProtoControlState.ControlMode.Ui;
                    if (settings != null) settings["ControlModes"] = (int)o.Control;
                    comp.Data.Repo.Values.State.TerminalActionSetter(comp, Trigger.Off);
                }
                else
                {
                    comp.Data.Repo.Values.State.PlayerId = -1;
                    comp.Data.Repo.Values.State.Mode = ProtoControlState.ControlMode.None;
                }

                if (resetTarget)
                    ClearParts(comp);
            }

            private static void ClearParts(ControlComponent comp)
            {
                for (int i = 0; i < comp.Platform.Upgrades.Count; i++)
                {
                    var part = comp.Platform.Upgrades[i];
                }
            }

            internal void TookControl(long playerId)
            {
                LastControllingPlayerId = playerId;

                if (Session.I.IsServer)
                {

                    if (Data.Repo != null)
                    {
                        Data.Repo.Values.State.PlayerId = playerId;
                        Data.Repo.Values.State.Mode = ProtoControlState.ControlMode.Camera;

                        if (Session.I.MpActive)
                            Session.I.SendComp(this);
                    }
                    else
                        Log.Line($"OnPlayerController enter Repo null");

                }

                if (Session.I.HandlesInput && playerId == Session.I.PlayerId)
                    Session.I.GunnerAcquire(Cube, playerId);
            }

            internal void ReleaseControl(long playerId)
            {
                LastControllingPlayerId = playerId;
                if (Session.I.IsServer)
                {

                    if (Data.Repo != null)
                    {

                        //Data.Repo.Values.State.PlayerId = -1;
                        Data.Repo.Values.State.Mode = ProtoControlState.ControlMode.Camera;

                        if (Session.I.MpActive)
                            Session.I.SendComp(this);
                    }
                    else
                        Log.Line($"OnPlayerController exit Repo null");
                }

                if (Session.I.HandlesInput && playerId == Session.I.PlayerId)
                {
                    Session.I.GunnerRelease(playerId);
                }
            }

            internal bool TakeOwnerShip()
            {
                if (LastOwnerRequestTick > 0 && Session.I.Tick - LastOwnerRequestTick < 120)
                    return true;

                LastOwnerRequestTick = Session.I.Tick;
                if (Session.I.IsClient)
                {
                    Session.I.SendPlayerControlRequest(this, Session.I.PlayerId, ProtoWeaponState.ControlMode.Ui);
                    return true;
                }
                Data.Repo.Values.State.PlayerId = Session.I.PlayerId;

                if (Session.I.MpActive)
                    Session.I.SendState(this);

                return true;
            }


            internal bool TrackTarget(IMyMotorStator root, IMyMotorStator other, ref Vector3D desiredDirection)
            {
                var topAi = Platform.Control.TopAi;

                var trackingWeapon = topAi.RootComp.PrimaryWeapon;

                RotorsMoving = true;
                var targetPos = topAi.RotorTargetPosition;
                var targetDistSqr = Vector3D.DistanceSquared(root.PositionComp.WorldAABB.Center, targetPos);

                var epsilon = Session.I.Tick120 ? 1E-06d : targetDistSqr <= 640000 ? 1E-03d : targetDistSqr <= 3240000 ? 1E-04d : 1E-05d;

                var currentDirection = trackingWeapon.GetScope.Info.Direction;
                var axis = Vector3D.Cross(desiredDirection, currentDirection);
                var deviationRads = MathHelper.ToRadians(Controller.AngleDeviation);

                //Root control
                var up = root.PositionComp.WorldMatrixRef.Up;
                var upZero = Vector3D.IsZero(up);
                var desiredFlat = upZero || Vector3D.IsZero(desiredDirection) ? Vector3D.Zero : desiredDirection - desiredDirection.Dot(up) * up;
                var currentFlat = upZero || Vector3D.IsZero(currentDirection) ? Vector3D.Zero : currentDirection - currentDirection.Dot(up) * up;
                var rootAngle = Vector3D.IsZero(desiredFlat) || Vector3D.IsZero(currentFlat) ? 0 : Math.Acos(MathHelper.Clamp(desiredFlat.Dot(currentFlat) / Math.Sqrt(desiredFlat.LengthSquared() * currentFlat.LengthSquared()), -1, 1));

                var rootOutsideLimits = false;
                if (MyUtils.IsZero((float) rootAngle, (float)epsilon))
                {
                    if (Session.I.IsServer)
                        root.TargetVelocityRad = 0;
                }
                else
                {
                    rootAngle *= Math.Sign(Vector3D.Dot(axis, up));

                    var desiredAngle = root.Angle + rootAngle;
                    rootOutsideLimits = desiredAngle < root.LowerLimitRad && desiredAngle + MathHelper.TwoPi > root.UpperLimitRad;

                    if ((desiredAngle < root.LowerLimitRad && desiredAngle + MathHelper.TwoPi < root.UpperLimitRad) || (desiredAngle > root.UpperLimitRad && desiredAngle - MathHelper.TwoPi > root.LowerLimitRad))
                        rootAngle = -Math.Sign(rootAngle) * (MathHelper.TwoPi - Math.Abs(rootAngle));

                    if (Session.I.IsServer)
                        root.TargetVelocityRad = rootOutsideLimits ? 0 : Math.Abs(Controller.VelocityMultiplierAzimuthRpm) * (float)rootAngle;
                }

                currentDirection = trackingWeapon.GetScope.Info.Direction;
                up = other.PositionComp.WorldMatrixRef.Up;
                upZero = Vector3D.IsZero(up);
                desiredFlat = upZero || Vector3D.IsZero(desiredDirection) ? Vector3D.Zero : desiredDirection - desiredDirection.Dot(up) * up;
                currentFlat = upZero || Vector3D.IsZero(currentDirection) ? Vector3D.Zero : currentDirection - currentDirection.Dot(up) * up;
                var subAngle = Vector3D.IsZero(desiredFlat) || Vector3D.IsZero(currentFlat) ? 0 : Math.Acos(MathHelper.Clamp(desiredFlat.Dot(currentFlat) / Math.Sqrt(desiredFlat.LengthSquared() * currentFlat.LengthSquared()), -1, 1));

                if (MyUtils.IsZero((float) subAngle, (float)epsilon) || !rootOutsideLimits && Math.Abs(rootAngle) > MathHelper.PiOver2)
                {
                    if (Session.I.IsServer)
                        other.TargetVelocityRad = 0;
                }
                else
                {
                    subAngle *= Math.Sign(Vector3D.Dot(axis, up));
                    var desiredAngle = other.Angle + subAngle;
                    var subOutsideLimits = desiredAngle < other.LowerLimitRad && desiredAngle + MathHelper.TwoPi > other.UpperLimitRad;

                    if ((desiredAngle < other.LowerLimitRad && desiredAngle + MathHelper.TwoPi < other.UpperLimitRad) || (desiredAngle > other.UpperLimitRad && desiredAngle - MathHelper.TwoPi > other.LowerLimitRad))
                        subAngle = -Math.Sign(subAngle) * (MathHelper.TwoPi - Math.Abs(subAngle));

                    if (Session.I.IsServer)
                        other.TargetVelocityRad = subOutsideLimits ? 0 : Math.Abs(Controller.VelocityMultiplierElevationRpm) * (float)subAngle;
                }
                if (rootAngle * rootAngle + subAngle * subAngle < deviationRads * deviationRads)
                {
                    var scopeInfo = trackingWeapon.GetScope.Info;
                    var targetDir = targetPos - scopeInfo.Position;

                    Platform.Control.IsAimed = MathFuncs.IsDotProductWithinTolerance(ref scopeInfo.Direction, ref targetDir, Session.I.ApproachDegrees);
                }

                return true;
            }
        }
    }
}

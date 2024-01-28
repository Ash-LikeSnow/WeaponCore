using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Entity;

namespace CoreSystems.Platform
{
    public partial class Upgrade 
    {
        public class UpgradeComponent : CoreComponent
        {
            internal readonly UpgradeCompData Data;
            internal readonly UpgradeStructure Structure;
            internal UpgradeComponent(MyEntity coreEntity, MyDefinitionId id)
            {
                //Bellow order is important
                Data = new UpgradeCompData(this);
                Init(coreEntity, true, Data, id);
                Structure = (UpgradeStructure)Platform.Structure;
            }

            internal void DetectStateChanges()
            {
                if (Platform.State != CorePlatform.PlatformState.Ready)
                    return;

                if (Session.I.Tick - Ai.LastDetectEvent > 59)
                {
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


            internal static void RequestSetValue(UpgradeComponent comp, string setting, int value, long playerId)
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

            internal static void SetValue(UpgradeComponent comp, string setting, int v, long playerId)
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
                        o.MoveMode = (ProtoUpgradeOverrides.MoveModes)v;
                        clearTargets = true;
                        break;
                    case "ControlModes":
                        o.Control = (ProtoUpgradeOverrides.ControlModes)v;
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
                    case "ArmorShowArea":
                        o.ArmorShowArea = enabled;
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
                }

                ResetCompState(comp, playerId, clearTargets);

                if (Session.I.MpActive)
                    Session.I.SendComp(comp);
            }

            internal static void ResetCompState(UpgradeComponent comp, long playerId, bool resetTarget, Dictionary<string, int> settings = null)
            {
                var o = comp.Data.Repo.Values.Set.Overrides;
                var userControl = o.Control != ProtoUpgradeOverrides.ControlModes.Auto;

                if (userControl)
                {
                    comp.Data.Repo.Values.State.PlayerId = playerId;
                    comp.Data.Repo.Values.State.Control = ProtoUpgradeState.ControlMode.Ui;
                    if (settings != null) settings["ControlModes"] = (int)o.Control;
                    comp.Data.Repo.Values.State.TerminalActionSetter(comp, Trigger.Off);
                }
                else
                {
                    comp.Data.Repo.Values.State.PlayerId = -1;
                    comp.Data.Repo.Values.State.Control = ProtoUpgradeState.ControlMode.None;
                }

                if (resetTarget)
                    ClearParts(comp);
            }

            private static void ClearParts(UpgradeComponent comp)
            {
                for (int i = 0; i < comp.Platform.Upgrades.Count; i++)
                {
                    var part = comp.Platform.Upgrades[i];
                }
            }
        }
    }
}

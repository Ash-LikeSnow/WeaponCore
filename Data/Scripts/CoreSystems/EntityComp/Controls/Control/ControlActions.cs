using System;
using System.Text;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRageMath;
using static CoreSystems.Support.CoreComponent.Trigger;

namespace CoreSystems.Control
{
    public static partial class CustomActions
    {
        #region Call Actions
        
        internal static void TerminalActionToggleShareFireControlControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.ShareFireControl;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "ShareFireControl", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleAiEnabledControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.AiEnabled;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "AiEnabled", newValue, Session.I.PlayerId);
        }

        internal static void TerminActionCycleShootModeControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var numValue = (int)comp.Data.Repo.Values.Set.Overrides.ShootMode;
            var value = numValue + 1 <= 3 ? numValue + 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "ShootMode", value, Session.I.PlayerId);
        }
        internal static void ShootModeWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var altAiControlName = !comp.HasAim && comp.Data.Repo.Values.Set.Overrides.ShootMode == Weapon.ShootManager.ShootModes.AiShoot ? InActive : comp.Data.Repo.Values.Set.Overrides.ShootMode.ToString();
            sb.Append(altAiControlName);
        }



        internal static void TerminalActionMovementModeControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var numValue = (int)comp.Data.Repo.Values.Set.Overrides.MoveMode;
            var value = numValue + 1 <= 3 ? numValue + 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "MovementModes", value, Session.I.PlayerId);
        }

        internal static void TerminActionCycleSubSystemControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var numValue = (int)comp.Data.Repo.Values.Set.Overrides.SubSystem;
            var value = numValue + 1 <= 7 ? numValue + 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "SubSystems", value, Session.I.PlayerId);
        }

        internal static void TerminalActionControlModeControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var numValue = (int)comp.Data.Repo.Values.Set.Overrides.Control;
            var value = numValue + 1 <= 2 ? numValue + 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "ControlModes", value, Session.I.PlayerId);
        }


        internal static void TerminalActionToggleNeutralsControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Neutrals;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Neutrals", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleProjectilesControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Projectiles;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Projectiles", newValue, Session.I.PlayerId);
        }
        internal static void TerminalActionToggleSupportingPDControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.SupportingPD;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "SupportingPD", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleBiologicalsControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Biologicals;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Biologicals", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleMeteorsControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Meteors;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Meteors", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleGridsControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Grids;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Grids", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleFriendlyControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Friendly;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Friendly", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleUnownedControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Unowned;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Unowned", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleFocusTargetsControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.FocusTargets;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "FocusTargets", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleFocusSubSystemControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.FocusSubSystem;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "FocusSubSystem", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionMaxSizeIncreaseControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MaxSize * 2;
            var newValue = nextValue > 0 && nextValue < 16384 ? nextValue : 16384;

            ControlSys.ControlComponent.RequestSetValue(comp, "MaxSize", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionMaxSizeDecreaseControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MaxSize / 2;
            var newValue = nextValue > 0 && nextValue < 16384 ? nextValue : 1;

            ControlSys.ControlComponent.RequestSetValue(comp, "MaxSize", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionMinSizeIncreaseControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MinSize == 0 ? 1 : comp.Data.Repo.Values.Set.Overrides.MinSize * 2;
            var newValue = nextValue > 0 && nextValue < 128 ? nextValue : 128;

            ControlSys.ControlComponent.RequestSetValue(comp, "MinSize", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionMinSizeDecreaseControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MinSize / 2;
            var newValue = nextValue > 0 && nextValue < 128 ? nextValue : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "MinSize", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleRepelModeControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent; 
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Repel;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "Repel", newValue, Session.I.PlayerId);
        }

        internal static void TerminalActionToggleLargeGridControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.LargeGrid;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "LargeGrid", newValue, Session.I.PlayerId);
        }
        internal static void TerminalActionToggleSmallGridControl(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.SmallGrid;
            var newValue = newBool ? 1 : 0;

            ControlSys.ControlComponent.RequestSetValue(comp, "SmallGrid", newValue, Session.I.PlayerId);
        }


        #endregion

        #region Writters

        internal static void ShareFireControlWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.ShareFireControl)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void AiEnabledWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.AiEnabled)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void NeutralWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Neutrals)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void ProjectilesWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Projectiles)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }
        internal static void SupportingPDWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.SupportingPD)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void BiologicalsWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Biologicals)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void MeteorsWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Meteors)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void GridsWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Grids)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void FriendlyWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Friendly)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void UnownedWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Unowned)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void FocusTargetsWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.FocusTargets)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void FocusSubSystemWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.FocusSubSystem)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void MaxSizeWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            sb.Append(comp.Data.Repo.Values.Set.Overrides.MaxSize);
        }

        internal static void MinSizeWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            sb.Append(comp.Data.Repo.Values.Set.Overrides.MinSize);
        }

        internal static void ControlStateWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            sb.Append(comp.Data.Repo.Values.Set.Overrides.Control);
        }

        internal static void MovementModeWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            sb.Append(comp.Data.Repo.Values.Set.Overrides.MoveMode);
        }

        internal static void SubSystemWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            sb.Append(comp.Data.Repo.Values.Set.Overrides.SubSystem);
        }

        internal static void RepelWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Repel)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }


        internal static void LargeGridWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.LargeGrid)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }
        internal static void SmallGridWriterControl(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as ControlSys.ControlComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.SmallGrid)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }
        #endregion
    }
}

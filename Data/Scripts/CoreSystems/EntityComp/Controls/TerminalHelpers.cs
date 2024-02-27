using System;
using System.Collections.Generic;
using System.Text;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Control
{
    public static class TerminalHelpers
    {
        internal static void AddUiControls<T>(Session session) where T : IMyTerminalBlock
        {

            //AddComboboxNoAction<T>(session, "Shoot Mode", Localization.GetText("TerminalShootModeTitle"), Localization.GetText("TerminalShootModeTooltip"), BlockUi.GetShootModes, BlockUi.RequestShootModes, BlockUi.ListShootModesNoBurst, Istrue);
            AddComboboxNoAction<T>(session, "Shoot Mode", Localization.GetText("TerminalShootModeTitle"), Localization.GetText("TerminalShootModeTooltip"), BlockUi.GetShootModes, BlockUi.RequestShootModes, BlockUi.ListShootModes, IsNotBomb);

            AddSliderRof<T>(session, "Weapon ROF", Localization.GetText("TerminalWeaponROFTitle"), Localization.GetText("TerminalWeaponROFTooltip"), BlockUi.GetRof, BlockUi.RequestSetRof, UiRofSlider, BlockUi.GetMinRof, BlockUi.GetMaxRof);

            AddCheckbox<T>(session, "Overload", Localization.GetText("TerminalOverloadTitle"), Localization.GetText("TerminalOverloadTooltip"), BlockUi.GetOverload, BlockUi.RequestSetOverload, true, UiOverLoad);


            AddWeaponCrticalTimeSliderRange<T>(session, "Detonation", Localization.GetText("TerminalDetonationTitle"), Localization.GetText("TerminalDetonationTooltip"), BlockUi.GetArmedTimer, BlockUi.RequestSetArmedTimer, NotCounting, CanBeArmed, BlockUi.GetMinCriticalTime, BlockUi.GetMaxCriticalTime, true);
            AddButtonNoAction<T>(session, "StartCount", Localization.GetText("TerminalStartCountTitle"), Localization.GetText("TerminalStartCountTooltip"), BlockUi.StartCountDown, NotCounting, CanBeArmed);
            AddButtonNoAction<T>(session, "StopCount", Localization.GetText("TerminalStopCountTitle"), Localization.GetText("TerminalStopCountTooltip"), BlockUi.StopCountDown, IsCounting, CanBeArmed);
            AddCheckboxNoAction<T>(session, "Arm", Localization.GetText("TerminalArmTitle"), Localization.GetText("TerminalArmTooltip"), BlockUi.GetArmed, BlockUi.RequestSetArmed, true, CanBeArmed);
            AddButtonNoAction<T>(session, "Trigger", Localization.GetText("TerminalTriggerTitle"), Localization.GetText("TerminalTriggerTooltip"), BlockUi.TriggerCriticalReaction, IsArmed, CanBeArmed);
        }


        internal static void AddTurretOrTrackingControls<T>(Session session) where T : IMyTerminalBlock
        {
            AddComboboxNoAction<T>(session, "ControlModes", Localization.GetText("TerminalControlModesTitle"), Localization.GetText("TerminalControlModesTooltip"), BlockUi.GetControlMode, BlockUi.RequestControlMode, BlockUi.ListControlModes, TurretOrGuidedAmmo);

            AddComboboxNoAction<T>(session, "PickAmmo", Localization.GetText("TerminalPickAmmoTitle"), Localization.GetText("TerminalPickAmmoTooltip"), BlockUi.GetAmmos, BlockUi.RequestSetAmmo, BlockUi.ListAmmos, AmmoSelection);

            AddComboboxNoAction<T>(session, "PickSubSystem", Localization.GetText("TerminalPickSubSystemTitle"), Localization.GetText("TerminalPickSubSystemTooltip"), BlockUi.GetSubSystem, BlockUi.RequestSubSystem, BlockUi.ListSubSystems, HasTracking);

            AddComboboxNoAction<T>(session, "TrackingMode", Localization.GetText("TerminalTrackingModeTitle"), Localization.GetText("TerminalTrackingModeTooltip"), BlockUi.GetMovementMode, BlockUi.RequestMovementMode, BlockUi.ListMovementModes, HasTracking);

            AddWeaponRangeSliderNoAction<T>(session, "Weapon Range", Localization.GetText("TerminalWeaponRangeTitle"), Localization.GetText("TerminalWeaponRangeTooltip"), BlockUi.GetRange, BlockUi.RequestSetRange, BlockUi.ShowRange, BlockUi.GetMinRange, BlockUi.GetMaxRange, true, false);

            Separator<T>(session, "WC_sep2", HasTracking);

            AddOnOffSwitchNoAction<T>(session, "ReportTarget", Localization.GetText("TerminalReportTargetTitle"), Localization.GetText("TerminalReportTargetTooltip"), BlockUi.GetReportTarget, BlockUi.RequestSetReportTarget, true, UiReportTarget);

            AddOnOffSwitchNoAction<T>(session, "Neutrals", Localization.GetText("TerminalNeutralsTitle"), Localization.GetText("TerminalNeutralsTooltip"), BlockUi.GetNeutrals, BlockUi.RequestSetNeutrals, true, HasTrackingNeutrals);

            AddOnOffSwitchNoAction<T>(session, "Unowned", Localization.GetText("TerminalUnownedTitle"), Localization.GetText("TerminalUnownedTooltip"), BlockUi.GetUnowned, BlockUi.RequestSetUnowned, true, HasTrackingUnowned);

            //AddOnOffSwitchNoAction<T>(session, "Friendly", Localization.GetText("TerminalFriendlyTitle"), Localization.GetText("TerminalFriendlyTooltip"), BlockUi.GetFriendly, BlockUi.RequestSetFriendly, true, HasTrackingAndTrackFriendly);

            AddOnOffSwitchNoAction<T>(session, "Biologicals", Localization.GetText("TerminalBiologicalsTitle"), Localization.GetText("TerminalBiologicalsTooltip"), BlockUi.GetBiologicals, BlockUi.RequestSetBiologicals, true, TrackBiologicals);

            AddOnOffSwitchNoAction<T>(session,  "Projectiles", Localization.GetText("TerminalProjectilesTitle"), Localization.GetText("TerminalProjectilesTooltip"), BlockUi.GetProjectiles, BlockUi.RequestSetProjectiles, true, TrackProjectiles);
            
            AddOnOffSwitchNoAction<T>(session, "Supporting PD", Localization.GetText("TerminalSupportingPDTitle"), Localization.GetText("TerminalSupportingPDTooltip"), BlockUi.GetSupportingPD, BlockUi.RequestSetSupportingPD, true, UiDisableSupportingPD);

            AddOnOffSwitchNoAction<T>(session, "Meteors", Localization.GetText("TerminalMeteorsTitle"), Localization.GetText("TerminalMeteorsTooltip"), BlockUi.GetMeteors, BlockUi.RequestSetMeteors, true, TrackMeteors);

            AddOnOffSwitchNoAction<T>(session,  "Grids", Localization.GetText("TerminalGridsTitle"), Localization.GetText("TerminalGridsTooltip"), BlockUi.GetGrids, BlockUi.RequestSetGrids, true, TrackGrids);

            AddOnOffSwitchNoAction<T>(session, "FocusFire", Localization.GetText("TerminalFocusFireTitle"), Localization.GetText("TerminalFocusFireTooltip"), BlockUi.GetFocusFire, BlockUi.RequestSetFocusFire, true, HasTracking);

            AddOnOffSwitchNoAction<T>(session, "SubSystems", Localization.GetText("TerminalSubSystemsTitle"), Localization.GetText("TerminalSubSystemsTooltip"), BlockUi.GetSubSystems, BlockUi.RequestSetSubSystems, true, HasTracking);

            AddOnOffSwitchNoAction<T>(session, "Repel", Localization.GetText("TerminalRepelTitle"), Localization.GetText("TerminalRepelTooltip"), BlockUi.GetRepel, BlockUi.RequestSetRepel, true, HasTracking);

            AddOnOffSwitchNoAction<T>(session, "LargeGrid", "Large Grid", "Target large grids", BlockUi.GetLargeGrid, BlockUi.RequestSetLargeGrid, true, HasTracking);

            AddOnOffSwitchNoAction<T>(session, "SmallGrid", "Small Grid", "Target small grids", BlockUi.GetSmallGrid, BlockUi.RequestSetSmallGrid, true, HasTracking);


            Separator<T>(session, "WC_sep3", IsTrue);

            AddWeaponBurstCountSliderRange<T>(session, "Burst Count", Localization.GetText("TerminalBurstShotsTitle"), Localization.GetText("TerminalBurstShotsTooltip"), BlockUi.GetBurstCount, BlockUi.RequestSetBurstCount, CanBurstIsNotBomb, BlockUi.GetMinBurstCount, BlockUi.GetMaxBurstCount, true);
            AddWeaponBurstDelaySliderRange<T>(session, "Burst Delay", Localization.GetText("TerminalBurstDelayTitle"), Localization.GetText("TerminalBurstDelayTooltip"), BlockUi.GetBurstDelay, BlockUi.RequestSetBurstDelay, IsNotBomb, BlockUi.GetMinBurstDelay, BlockUi.GetMaxBurstDelay, true);
            AddWeaponSequenceIdSliderRange<T>(session, "Sequence Id", Localization.GetText("TerminalSequenceIdTitle"), Localization.GetText("TerminalSequenceIdTooltip"), BlockUi.GetSequenceId, BlockUi.RequestSetSequenceId, IsNotBomb, BlockUi.GetMinSequenceId, BlockUi.GetMaxSequenceId, false);
            AddWeaponGroupIdIdSliderRange<T>(session, "Weapon Group Id", Localization.GetText("TerminalWeaponGroupIdTitle"), Localization.GetText("TerminalWeaponGroupIdTooltip"), BlockUi.GetWeaponGroupId, BlockUi.RequestSetWeaponGroupId, IsNotBomb, BlockUi.GetMinWeaponGroupId, BlockUi.GetMaxWeaponGroupId, true);

            Separator<T>(session, "WC_sep4", IsTrue);

            AddLeadGroupSliderRange<T>(session, "Target Group", Localization.GetText("TerminalTargetGroupTitle"), Localization.GetText("TerminalTargetGroupTooltip"), BlockUi.GetLeadGroup, BlockUi.RequestSetLeadGroup, TargetLead, BlockUi.GetMinLeadGroup, BlockUi.GetMaxLeadGroup, true);
            AddWeaponCameraSliderRange<T>(session, "Camera Channel", Localization.GetText("TerminalCameraChannelTitle"), Localization.GetText("TerminalCameraChannelTooltip"), BlockUi.GetWeaponCamera, BlockUi.RequestSetBlockCamera, HasTracking, BlockUi.GetMinCameraChannel, BlockUi.GetMaxCameraChannel, true);
            
            AddListBoxNoAction<T>(session, "Friend", "Friend", "Friend list", BlockUi.FriendFill, BlockUi.FriendSelect, IsDrone, 1, true, true);
            AddListBoxNoAction<T>(session, "Enemy", "Enemy", "Enemy list", BlockUi.EnemyFill, BlockUi.EnemySelect, IsDrone, 1, true, true);
            //AddListBoxNoAction<T>(session, "Position", "Position", "Position list", BlockUi.PositionFill, BlockUi.PositionSelect, IsDrone, 1, true, true); Suppressed for now as it's inop


            Separator<T>(session, "WC_sep5", HasTracking);
        }


        internal static void AddTurretControlBlockControls<T>(Session session) where T : IMyTerminalBlock
        {
            CtcAddListBoxNoAction<T>(session, "ToolsAndWeapons", "Available tools and weapons", "Auto populated by weaponcore", BlockUi.ToolWeaponFill, BlockUi.ToolWeaponSelect, CtcIsReady, 4, true);

            CtcAddCheckboxNoAction<T>(session, "Advanced", Localization.GetText("TerminalAdvancedTitle"), Localization.GetText("TerminalAdvancedTooltip"), BlockUi.GetAdvancedControl, BlockUi.RequestAdvancedControl, true, CtcIsReady);
            CtcAddOnOffSwitchNoAction<T>(session, "ShareFireControlEnabled", Localization.GetText("TerminalShareFireControlTitle"), Localization.GetText("TerminalShareFireControlTooltip"), BlockUi.GetShareFireControlControl, BlockUi.RequestShareFireControlControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "WCAiEnabled", Localization.GetText("TerminalAiEnabledTitle"), Localization.GetText("TerminalAiEnabledTooltip"), BlockUi.GetAiEnabledControl, BlockUi.RequestSetAiEnabledControl, true, CtcIsReady);

            Separator<T>(session, "WC_sep2", IsTrue);

            AddWeaponCtcRangeSliderNoAction<T>(session, "Weapon Range", Localization.GetText("TerminalWeaponRangeTitle"), Localization.GetText("TerminalWeaponRangeTooltip"), BlockUi.GetRangeControl, BlockUi.RequestSetRangeControl, CtcIsReady, BlockUi.GetMinRangeControl, BlockUi.GetMaxRangeControl, true, false);

            CtcAddOnOffSwitchNoAction<T>(session, "ReportTarget", Localization.GetText("TerminalReportTargetTitle"), Localization.GetText("TerminalReportTargetTooltip"), BlockUi.GetReportTargetControl, BlockUi.RequestSetReportTargetControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Neutrals", Localization.GetText("TerminalNeutralsTitle"), Localization.GetText("TerminalNeutralsTooltip"), BlockUi.GetNeutralsControl, BlockUi.RequestSetNeutralsControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Unowned", Localization.GetText("TerminalUnownedTitle"), Localization.GetText("TerminalUnownedTooltip"), BlockUi.GetUnownedControl, BlockUi.RequestSetUnownedControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Biologicals", Localization.GetText("TerminalBiologicalsTitle"), Localization.GetText("TerminalBiologicalsTooltip"), BlockUi.GetBiologicalsControl, BlockUi.RequestSetBiologicalsControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Projectiles", Localization.GetText("TerminalProjectilesTitle"), Localization.GetText("TerminalProjectilesTooltip"), BlockUi.GetProjectilesControl, BlockUi.RequestSetProjectilesControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Supporting PD", Localization.GetText("TerminalSupportingPDTitle"), Localization.GetText("TerminalSupportingPDTooltip"), BlockUi.GetSupportingPDControl, BlockUi.RequestSetSupportingPDControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Meteors", Localization.GetText("TerminalMeteorsTitle"), Localization.GetText("TerminalMeteorsTooltip"), BlockUi.GetMeteorsControl, BlockUi.RequestSetMeteorsControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Grids", Localization.GetText("TerminalGridsTitle"), Localization.GetText("TerminalGridsTooltip"), BlockUi.GetGridsControl, BlockUi.RequestSetGridsControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "FocusFire", Localization.GetText("TerminalFocusFireTitle"), Localization.GetText("TerminalFocusFireTooltip"), BlockUi.GetFocusFireControl, BlockUi.RequestSetFocusFireControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "SubSystems", Localization.GetText("TerminalSubSystemsTitle"), Localization.GetText("TerminalSubSystemsTooltip"), BlockUi.GetSubSystemsControl, BlockUi.RequestSetSubSystemsControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "Repel", Localization.GetText("TerminalRepelTitle"), Localization.GetText("TerminalRepelTooltip"), BlockUi.GetRepelControl, BlockUi.RequestSetRepelControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "LargeGrid", "Large Grid", "Target large grids", BlockUi.GetLargeGridControl, BlockUi.RequestSetLargeGridControl, true, CtcIsReady);

            CtcAddOnOffSwitchNoAction<T>(session, "SmallGrid", "Small Grid", "Target small grids", BlockUi.GetSmallGridControl, BlockUi.RequestSetSmallGridControl, true, CtcIsReady);

            Separator<T>(session, "WC_sep3", IsTrue);

            CtcAddComboboxNoAction<T>(session, "PickSubSystem", Localization.GetText("TerminalPickSubSystemTitle"), Localization.GetText("TerminalPickSubSystemTooltip"), BlockUi.GetSubSystemControl, BlockUi.RequestSubSystemControl, BlockUi.ListSubSystems, CtcIsReady);

            CtcAddComboboxNoAction<T>(session, "TrackingMode", Localization.GetText("TerminalTrackingModeTitle"), Localization.GetText("TerminalTrackingModeTooltip"), BlockUi.GetMovementModeControl, BlockUi.RequestMovementModeControl, BlockUi.ListMovementModes, CtcIsReady);

            //CtcAddComboboxNoAction<T>(session, "Shoot Mode", Localization.GetText("TerminalShootModeTitle"), Localization.GetText("TerminalShootModeTooltip"), BlockUi.CtcGetShootModes, BlockUi.CtcRequestShootModes, BlockUi.CtcListShootModes, CtcIsReady);

            CtcAddComboboxNoAction<T>(session, "ControlModes", Localization.GetText("TerminalControlModesTitle"), Localization.GetText("TerminalControlModesTooltip"), BlockUi.GetControlModeControl, BlockUi.RequestControlModeControl, BlockUi.ListControlModes, CtcIsReady);

            //AddWeaponCameraSliderRange<T>(session, "Camera Channel", Localization.GetText("TerminalCameraChannelTitle"), Localization.GetText("TerminalCameraChannelTooltip"), BlockUi.GetWeaponCamera, BlockUi.RequestSetBlockCamera, HasTracking, BlockUi.GetMinCameraChannel, BlockUi.GetMaxCameraChannel, true);

            //AddLeadGroupSliderRange<T>(session, "Target Group", Localization.GetText("TerminalTargetGroupTitle"), Localization.GetText("TerminalTargetGroupTooltip"), BlockUi.GetLeadGroup, BlockUi.RequestSetLeadGroup, TargetLead, BlockUi.GetMinLeadGroup, BlockUi.GetMaxLeadGroup, true);

            Separator<T>(session, "WC_sep4", IsTrue);
        }

        internal static void AddSearchlightControls<T>(Session session) where T : IMyTerminalBlock
        {
            Separator<T>(session, "WC_sep2", HasTracking);

            AddWeaponRangeSliderNoAction<T>(session, "Weapon Range", Localization.GetText("TerminalWeaponRangeTitle"), Localization.GetText("TerminalWeaponRangeTooltip"), BlockUi.GetRange, BlockUi.RequestSetRange, BlockUi.ShowRange, BlockUi.GetMinRange, BlockUi.GetMaxRange, true, false);

            AddOnOffSwitchNoAction<T>(session, "Neutrals", Localization.GetText("TerminalNeutralsTitle"), Localization.GetText("TerminalNeutralsTooltip"), BlockUi.GetNeutrals, BlockUi.RequestSetNeutrals, true, HasTrackingNeutrals);

            AddOnOffSwitchNoAction<T>(session, "Unowned", Localization.GetText("TerminalUnownedTitle"), Localization.GetText("TerminalUnownedTooltip"), BlockUi.GetUnowned, BlockUi.RequestSetUnowned, true, HasTrackingUnowned);

            //AddOnOffSwitchNoAction<T>(session, "Friendly", Localization.GetText("TerminalFriendlyTitle"), Localization.GetText("TerminalFriendlyTooltip"), BlockUi.GetFriendly, BlockUi.RequestSetFriendly, true, HasTrackingAndTrackFriendly);

            AddOnOffSwitchNoAction<T>(session, "Biologicals", Localization.GetText("TerminalBiologicalsTitle"), Localization.GetText("TerminalBiologicalsTooltip"), BlockUi.GetBiologicals, BlockUi.RequestSetBiologicals, true, TrackBiologicals);

            AddOnOffSwitchNoAction<T>(session, "Projectiles", Localization.GetText("TerminalProjectilesTitle"), Localization.GetText("TerminalProjectilesTooltip"), BlockUi.GetProjectiles, BlockUi.RequestSetProjectiles, true, TrackProjectiles);

            AddOnOffSwitchNoAction<T>(session, "Meteors", Localization.GetText("TerminalMeteorsTitle"), Localization.GetText("TerminalMeteorsTooltip"), BlockUi.GetMeteors, BlockUi.RequestSetMeteors, true, TrackMeteors);

            AddOnOffSwitchNoAction<T>(session, "Grids", Localization.GetText("TerminalGridsTitle"), Localization.GetText("TerminalGridsTooltip"), BlockUi.GetGrids, BlockUi.RequestSetGrids, true, TrackGrids);

            AddOnOffSwitchNoAction<T>(session, "LargeGrid", "Large Grid", "Target large grids", BlockUi.GetLargeGrid, BlockUi.RequestSetLargeGrid, true, HasTracking);

            AddOnOffSwitchNoAction<T>(session, "SmallGrid", "Small Grid", "Target small grids", BlockUi.GetSmallGrid, BlockUi.RequestSetSmallGrid, true, HasTracking);
        }
        internal static void AddDecoyControls<T>(Session session) where T : IMyTerminalBlock
        {
            Separator<T>(session, "WC_decoySep1", IsTrue);
            AddComboboxDecoyNoAction<T>(session, "PickSubSystem", Localization.GetText("TerminalDecoyPickSubSystemTitle"), Localization.GetText("TerminalDecoyPickSubSystemTooltip"), BlockUi.GetDecoySubSystem, BlockUi.RequestDecoySubSystem, BlockUi.ListDecoySubSystems, IsDecoy);
        }

        internal static void AddOffenseBlockControls<T>(Session session) where T : IMyTerminalBlock
        {
            CtcAddListBoxNoAction<T>(session, "FixedWeapons", "Fixed weapons", "Auto populated by weaponcore", BlockUi.CombatWeaponFill, BlockUi.ToolWeaponSelect, CombatAiActive, 4, true);
        }
            internal static void AddCameraControls<T>(Session session) where T : IMyTerminalBlock
        {
            Separator<T>(session,  "WC_cameraSep1", IsTrue);
            AddBlockCameraSliderRange<T>(session, "WC_PickCameraChannel", Localization.GetText("TerminalCameraCameraChannelTitle"), Localization.GetText("TerminalCameraCameraChannelTooltip"), BlockUi.GetBlockCamera, BlockUi.RequestBlockCamera, BlockUi.ShowCamera, BlockUi.GetMinCameraChannel, BlockUi.GetMaxCameraChannel, true);
        }

        internal static void CreateGenericControls<T>(Session session) where T : IMyTerminalBlock
        {
            AddCheckboxNoAction<T>(session, "Advanced", Localization.GetText("TerminalAdvancedTitle"), Localization.GetText("TerminalAdvancedTooltip"), BlockUi.GetAdvanced, BlockUi.RequestAdvanced, true, IsReadyAndIsNotTrackingTurret);
            AddOnOffSwitchNoAction<T>(session,  "Debug", Localization.GetText("TerminalDebugTitle"), Localization.GetText("TerminalDebugTooltip"), BlockUi.GetDebug, BlockUi.RequestDebug, true, GuidedAmmoNoTurret);

            Separator<T>(session, "WC_sep4", IsTrue);
            AddOnOffSwitchNoAction<T>(session, "ShareFireControlEnabled", Localization.GetText("TerminalShareFireControlTitle"), Localization.GetText("TerminalShareFireControlTooltip"), BlockUi.GetShareFireControl, BlockUi.RequestShareFireControl, true, HasTracking);

            AddOnOffSwitchNoAction<T>(session,  "Shoot", Localization.GetText("TerminalShootTitle"), Localization.GetText("TerminalShootTooltip"), BlockUi.GetShoot, BlockUi.RequestSetShoot, true, IsNotBomb);
            AddOnOffSwitchNoAction<T>(session, "Override", Localization.GetText("TerminalOverrideTitle"), Localization.GetText("TerminalOverrideTooltip"), BlockUi.GetOverride, BlockUi.RequestOverride, true, OverrideTarget);
            AddOnOffSwitchNoAction<T>(session, "AngularTracking", Localization.GetText("TerminalAngularTitle"), Localization.GetText("TerminalAngularTooltip"), BlockUi.GetAngularTracking, BlockUi.RequestAngularTracking, true, IsNotBomb);
        }

        internal static void CreateGenericArmor<T>(Session session) where T : IMyTerminalBlock
        {
            AddOnOffSwitchNoAction<T>(session, "Show Enhanced Area", "Area Influence", "Show On/Off", BlockUi.GetShowArea, BlockUi.RequestSetShowArea, true, SupportIsReady);
        }

        internal static bool IsCamera(IMyTerminalBlock block)
        {
            return block is IMyCameraBlock;
        }

        internal static bool SupportIsReady(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>();
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.TypeSpecific == CoreComponent.CompTypeSpecific.Support;
        }

        internal static bool IsDecoy(IMyTerminalBlock block)
        {
            return block is IMyDecoy;
        }

        internal static bool CombatAiActive(IMyTerminalBlock block)
        {
            return true;
        }


        internal static bool KeyShootWeapon(IMyTerminalBlock block)
        {
            var comp = block.Components.Get<CoreComponent>();

            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Type == CoreComponent.CompType.Weapon;
        }

        internal static bool UiRofSlider(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.HasRofSlider && !comp.HasAlternateUi;
        }

        internal static bool UiOverLoad(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>();
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.CanOverload;
        }

        internal static bool UiReportTarget(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.HasRequireTarget && !comp.HasAlternateUi;
        }

        internal static bool TrackMeteors(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.IsBlock && (comp.HasTurret || comp.PrimaryWeapon.System.HasGuidedAmmo) && comp.PrimaryWeapon.System.TrackMeteors &&!comp.HasAlternateUi;
        }

        internal static bool TrackGrids(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            return block is IMyTurretControlBlock || comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.IsBlock && (comp.HasTurret || comp.PrimaryWeapon.System.HasGuidedAmmo) && comp.PrimaryWeapon.System.TrackGrids && !comp.HasAlternateUi;
        }

        internal static bool TrackProjectiles(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.IsBlock && (comp.HasTurret || comp.PrimaryWeapon.System.HasGuidedAmmo) && comp.PrimaryWeapon.System.TrackProjectile && !comp.HasAlternateUi;
        }
        internal static bool UiDisableSupportingPD(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.IsBlock && (comp.HasTurret || comp.PrimaryWeapon.System.HasGuidedAmmo) && comp.PrimaryWeapon.System.TrackProjectile && !comp.HasAlternateUi && !comp.DisableSupportingPD;
        }

        internal static bool TrackBiologicals(IMyTerminalBlock block)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.IsBlock && (comp.HasTurret || comp.PrimaryWeapon.System.HasGuidedAmmo) && comp.PrimaryWeapon.System.TrackCharacters && !comp.HasAlternateUi;
        }

        internal static bool AmmoSelection(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Type == CoreComponent.CompType.Weapon && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.ConsumableSelectionPartIds.Count > 0;
        }

        internal static bool CanBurst(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return !comp.HasDisabledBurst;
        }

        internal static bool CanBurstIsNotBomb(IMyTerminalBlock block)
        {
            return CanBurst(block) && IsNotBomb(block);

        }
        internal static bool CanBeArmed(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.HasArming;
        }

        internal static bool IsCounting(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.Data.Repo.Values.State.CountingDown;
        }

        internal static bool NotCounting(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return !comp.Data.Repo.Values.State.CountingDown;
        }

        internal static bool HasTurret(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Type == CoreComponent.CompType.Weapon && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.HasTurret && !comp.HasAlternateUi;
        }

        internal static bool NoTurret(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;


            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Type == CoreComponent.CompType.Weapon && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return !comp.HasTurret;
        }


        internal static bool IsTrue(IMyTerminalBlock block)
        {
            return true;
        }

        internal static bool IsFalse(IMyTerminalBlock block)
        {
            return false;
        }

        internal static bool WeaponIsReadyAndSorter(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Type == CoreComponent.CompType.Weapon && comp.TypeSpecific == CoreComponent.CompTypeSpecific.SorterWeapon && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return true;
        }


        internal static bool IsReady(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return true;
        }

        internal static bool IsReadyAndIsNotTrackingTurret(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return !comp.HasAlternateUi;
        }

        internal static bool IsDrone(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.HasDrone;
        }

        internal static bool CtcIsReady(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as ControlSys.ControlComponent;
            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return true;
        }

        internal static bool HasTracking(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;

            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return (comp.HasTracking || comp.HasGuidance) && !comp.HasAlternateUi;
        }


        internal static bool HasTrackingUnowned(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;

            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return (comp.HasTracking || comp.HasGuidance) && !comp.HasAlternateUi;
        }

        internal static bool HasTrackingNeutrals(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;

            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return (comp.HasTracking || comp.HasGuidance) && !comp.HasAlternateUi;
        }
        internal static bool HasTrackingAndTrackFriendly(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;

            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return (comp.HasTracking || comp.HasGuidance || comp.HasAlternateUi);
        }

        internal static bool IsArmed(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.Data.Repo.Values.Set.Overrides.Armed;
        }


        internal static bool GuidedAmmo(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.HasGuidance && !comp.HasAlternateUi;
        }


        internal static bool OverrideTarget(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return comp.HasRequireTarget && !comp.HasAlternateUi;
        }

        internal static bool IsNotBomb(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            var valid = comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Data?.Repo != null;
            if (!valid || Session.I.PlayerId != comp.Data.Repo.Values.State.PlayerId && !comp.TakeOwnerShip())
                return false;

            return !comp.IsBomb && !comp.HasAlternateUi;
        }
        internal static bool HasSupport(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>();
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Type == CoreComponent.CompType.Support;
        }

        internal static bool TargetLead(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>();
            return comp != null && comp.Platform.State == CorePlatform.PlatformState.Ready && comp.Type == CoreComponent.CompType.Weapon && !comp.IsBomb && (!comp.HasTurret && !comp.OverrideLeads || comp.HasTurret && comp.OverrideLeads);
        }

        internal static bool TurretOrGuidedAmmo(IMyTerminalBlock block)
        {
            return HasTurret(block) || GuidedAmmo(block);
        }

        internal static bool TurretOrGuidedAmmoAny(IMyTerminalBlock block)
        {
            return HasTurret(block) || GuidedAmmo(block);
        }
        internal static bool GuidedAmmoNoTurret(IMyTerminalBlock block)
        {
            return NoTurret(block) && GuidedAmmo(block);
        }


        internal static void SliderWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {
            builder.Append(BlockUi.GetRange(block).ToString("N2"));
        }

        internal static void SliderCtcWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {
            builder.Append(BlockUi.GetRangeControl(block).ToString("N0"));
        }

        internal static void SliderWriterRof(IMyTerminalBlock block, StringBuilder builder)
        {
            builder.Append(BlockUi.GetRof(block).ToString("N2"));
        }

        internal static void KeyShootStringBuilder(IMyTerminalBlock block, StringBuilder builder)
        {
            builder.Append(BlockUi.GetStringShootStatus(block));
        }

        internal static void EmptyStringBuilder(IMyTerminalBlock block, StringBuilder builder)
        {
            builder.Append("");
        }

        internal static bool NotWcBlock(IMyTerminalBlock block)
        {
            CoreComponent comp;
            return !block.Components.TryGet(out comp); 
        }

        internal static bool NotWcOrIsTurret(IMyTerminalBlock block)
        {
            CoreComponent comp;
            return !block.Components.TryGet(out comp) || comp is ControlSys.ControlComponent || comp.HasTurret;
        }

        internal static void SliderBlockCameraWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {
            long value = -1;
            string message;
            if (string.IsNullOrEmpty(block.CustomData) || long.TryParse(block.CustomData, out value))
            {
                var group = value >= 0 ? value : 0;
                message = value == 0 ? "Disabled" : group.ToString();
            }
            else message = "Invalid CustomData";

            builder.Append(message);
        }

        internal static void SliderWeaponCameraWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {

            var value = (long)Math.Round(BlockUi.GetWeaponCamera(block), 0);
            var message = value > 0 ? value.ToString() : "Disabled";

            builder.Append(message);
        }

        internal static void SliderWeaponBurstCountWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {

            var value = (long)Math.Round(BlockUi.GetBurstCount(block), 0);
            var message = value > 0 ? value.ToString() : "Disabled";

            builder.Append(message);
        }

        internal static void SliderWeaponBurstDelayWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {

            var value = (long)Math.Round(BlockUi.GetBurstDelay(block), 0);
            var message = value > 0 ? value.ToString() : "Disabled";

            builder.Append(message);
        }

        internal static void SliderWeaponSequenceIdWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {

            var value = (long)Math.Round(BlockUi.GetSequenceId(block), 0);
            var valid = BlockUi.ValidSequenceId(block);
            var message = value == -1 ? "Disabled" : valid ? value.ToString() : $"DuplicateId: {value}";

            builder.Append(message);
        }

        internal static void SliderWeaponGroupIdWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {

            var value = (long)Math.Round(BlockUi.GetWeaponGroupId(block), 0);
            var message = value > 0 ? value.ToString() : "Disabled";

            builder.Append(message);
        }

        internal static void SliderCriticalTimerWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {

            var value = BlockUi.GetArmedTimeRemaining(block);

            if (value >= 59.95)
                builder.Append("00:01:00");
            else if (value < 0.33)
                builder.Append("00:00:00");
            else
            {
                builder.Append("00:")
                    .Append(value.ToString("00:00"));
            }
        }

        internal static void SliderLeadGroupWriterRange(IMyTerminalBlock block, StringBuilder builder)
        {

            var value = (long)Math.Round(BlockUi.GetLeadGroup(block), 0);
            var message = value > 0 ? value.ToString() : "Disabled";

            builder.Append(message);
        }

        #region terminal control methods
        internal static IMyTerminalControlSlider AddBlockCameraSliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsCamera;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderBlockCameraWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            CreateCustomActions<T>.CreateSliderActionSet(session, c, title, 0, 1, .1f, visibleGetter, group);
            return c;
        }

        internal static IMyTerminalControlSlider AddWeaponCameraSliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderWeaponCameraWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddWeaponBurstCountSliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderWeaponBurstCountWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddWeaponBurstDelaySliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderWeaponBurstDelayWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddWeaponSequenceIdSliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderWeaponSequenceIdWriterRange;
            c.SupportsMultipleBlocks = group;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddWeaponGroupIdIdSliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderWeaponGroupIdWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }


        internal static IMyTerminalControlSlider AddWeaponCrticalTimeSliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> enableGetter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = enableGetter;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderCriticalTimerWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddLeadGroupSliderRange<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderLeadGroupWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);
            return c;
        }

        internal static IMyTerminalControlOnOffSwitch AddWeaponOnOff<T>(Session session, string name, string title, string tooltip, string onText, string offText, Func<IMyTerminalBlock, int, bool> getter, Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visibleGetter) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>($"WC_Enable");

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.OnText = MyStringId.GetOrCompute(onText);
            c.OffText = MyStringId.GetOrCompute(offText);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = IsReady;
            c.Setter = setter;
            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            CreateCustomActions<T>.CreateOnOffActionSet(session, c, name, visibleGetter);

            return c;
        }

        internal static IMyTerminalControlSeparator Separator<T>(Session session, string name, Func<IMyTerminalBlock,bool> visibleGettter) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, T>(name);

            c.Enabled = IsTrue;
            c.Visible = visibleGettter;
            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddWeaponRangeSliderNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false, bool addAction = true) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddWeaponCtcRangeSliderNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null, bool group = false, bool addAction = true) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsTrue;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderCtcWriterRange;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddSliderRof<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> visibleGetter, Func<IMyTerminalBlock, float> minGetter = null, Func<IMyTerminalBlock, float> maxGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = IsReady;
            c.Visible = visibleGetter;
            c.Getter = getter;
            c.Setter = setter;
            c.Writer = SliderWriterRof;

            if (minGetter != null)
                c.SetLimits(minGetter, maxGetter);

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            CreateCustomActions<T>.CreateSliderActionSet(session, c, name, 0, 1, .1f, visibleGetter, false);
            return c;
        }

        internal static IMyTerminalControlCheckbox AddCheckbox<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, bool allowGroup, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Getter = getter;
            c.Setter = setter;
            c.Visible = visibleGetter;
            c.Enabled = IsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            CreateCustomActions<T>.CreateOnOffActionSet(session, c, name, visibleGetter, allowGroup);

            return c;
        }

        internal static IMyTerminalControlCheckbox AddCheckboxNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, bool allowGroup, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Getter = getter;
            c.Setter = setter;
            c.Visible = visibleGetter;
            c.Enabled = IsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlOnOffSwitch AddOnOffSwitchNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, bool allowGroup, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>("WC_" + name);
            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.OnText = MyStringId.GetOrCompute(Localization.GetText("TerminalSwitchOn"));
            c.OffText = MyStringId.GetOrCompute(Localization.GetText("TerminalSwitchOff"));
            c.Getter = getter;
            c.Setter = setter;
            c.Visible = visibleGetter;
            c.Enabled = IsReady;
            
            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }


        internal static IMyTerminalControlCheckbox CtcAddCheckboxNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, bool allowGroup, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Getter = getter;
            c.Setter = setter;
            c.Visible = visibleGetter;
            c.Enabled = CtcIsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlListbox CtcAddListBoxNoAction<T>(Session session, string name, string title, string tooltip, Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> fillAction, Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>> selectAction, Func<IMyTerminalBlock, bool> visibleGetter = null, int visibleRowCount = 5, bool multiSelect = false, bool groups = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.ListContent = fillAction;
            c.ItemSelected = selectAction;
            c.Multiselect = multiSelect;
            c.VisibleRowsCount = visibleRowCount;
            c.SupportsMultipleBlocks = groups;
            c.Visible = visibleGetter;
            c.Enabled = CtcIsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }


        internal static IMyTerminalControlOnOffSwitch CtcAddOnOffSwitchNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, bool allowGroup, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>("WC_" + name);
            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.OnText = MyStringId.GetOrCompute(Localization.GetText("TerminalSwitchOn"));
            c.OffText = MyStringId.GetOrCompute(Localization.GetText("TerminalSwitchOff"));
            c.Getter = getter;
            c.Setter = setter;
            c.Visible = visibleGetter;
            c.Enabled = CtcIsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlCombobox AddComboboxNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, long> getter, Action<IMyTerminalBlock, long> setter, Action<List<MyTerminalControlComboBoxItem>> fillAction, Func<IMyTerminalBlock,  bool> visibleGetter = null) where T : IMyTerminalBlock {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.ComboBoxContent = fillAction;
            c.Getter = getter;
            c.Setter = setter;

            c.Visible = visibleGetter;
            c.Enabled = IsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        internal static IMyTerminalControlCombobox CtcAddComboboxNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, long> getter, Action<IMyTerminalBlock, long> setter, Action<List<MyTerminalControlComboBoxItem>> fillAction, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.ComboBoxContent = fillAction;
            c.Getter = getter;
            c.Setter = setter;

            c.Visible = visibleGetter;
            c.Enabled = CtcIsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }
        internal static IMyTerminalControlListbox AddListBoxNoAction<T>(Session session, string name, string title, string tooltip, Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> fillAction, Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>> selectAction, Func<IMyTerminalBlock, bool> visibleGetter = null, int visibleRowCount = 5, bool multiSelect = false, bool groups = false) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.ListContent = fillAction;
            c.ItemSelected = selectAction;
            c.Multiselect = multiSelect;
            c.VisibleRowsCount = visibleRowCount;
            c.SupportsMultipleBlocks = groups;
            c.Visible = visibleGetter;
            c.Enabled = IsReady;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }


        internal static IMyTerminalControlCombobox AddComboboxDecoyNoAction<T>(Session session, string name, string title, string tooltip, Func<IMyTerminalBlock, long> getter, Action<IMyTerminalBlock, long> setter, Action<List<MyTerminalControlComboBoxItem>> fillAction, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.ComboBoxContent = fillAction;
            c.Getter = getter;
            c.Setter = setter;

            c.Visible = visibleGetter;
            c.Enabled = IsDecoy;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }


        internal static IMyTerminalControlButton AddButtonNoAction<T>(Session session, string name, string title, string tooltip, Action<IMyTerminalBlock> action, Func<IMyTerminalBlock, bool> enableGetter, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, T>("WC_" + name);

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Action = action;
            c.Visible = visibleGetter;
            c.Enabled = enableGetter;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            session.CustomControls.Add(c);

            return c;
        }

        #endregion
    }
}

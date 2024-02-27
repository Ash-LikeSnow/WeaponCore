using System;
using System.Text;
using CoreSystems.Support;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace CoreSystems.Control
{
    public static class CreateCustomActions<T>
    {

        public static void CreateArmReaction(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Arm");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder("Arm Critical Reaction");
            action.Action = CustomActions.RequestSetArmed;
            action.Writer = CustomActions.ArmWriter;
            action.Enabled = TerminalHelpers.CanBeArmed;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        internal static void CreateShootMode(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("WCShootMode");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionWCShootMode"));
            action.Action = CustomActions.TerminActionCycleShootMode;
            action.Writer = CustomActions.ShootModeWriter;
            action.Enabled = TerminalHelpers.IsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        internal static void CreateMouseToggle(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("WCMouseToggle");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionWCMouseToggle"));
            action.Action = CustomActions.TerminActionCycleMouseControl;
            action.Writer = CustomActions.MouseToggleWriter;
            action.Enabled = TerminalHelpers.IsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateKeyShoot(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ActionFire");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFire"));
            action.Action = CustomActions.TerminalActionKeyShoot;
            action.Writer = TerminalHelpers.KeyShootStringBuilder;
            action.Enabled = TerminalHelpers.KeyShootWeapon;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateForceReload(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ForceReload");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Name = new StringBuilder(Localization.GetText("ForceReload"));
            action.Action = CustomActions.TerminalRequestReload;
            action.Writer = TerminalHelpers.EmptyStringBuilder;
            action.Enabled = TerminalHelpers.IsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }


        public static void CreateSelectTarget(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ActionTarget");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionTarget"));
            action.Action = CustomActions.TerminalActionFriend;
            action.Writer = TerminalHelpers.EmptyStringBuilder;
            action.Enabled = TerminalHelpers.IsDrone;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateSelectFriend(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ActionFriend");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFriend"));
            action.Action = CustomActions.TerminalActionFriend;
            action.Writer = TerminalHelpers.EmptyStringBuilder;
            action.Enabled = TerminalHelpers.IsDrone;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateSelectEnemy(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ActionEnemy");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionEnemy"));
            action.Action = CustomActions.TerminalActionEnemy;
            action.Writer = TerminalHelpers.EmptyStringBuilder;
            action.Enabled = TerminalHelpers.IsDrone;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateSelectPosition(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ActionPosition");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionPosition"));
            action.Action = CustomActions.TerminalActionPosition;
            action.Writer = TerminalHelpers.EmptyStringBuilder;
            action.Enabled = TerminalHelpers.IsDrone;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateShootToggle(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ShootToggle");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionShoot"));
            action.Action = CustomActions.TerminalActionToggleShoot;
            action.Writer = CustomActions.ShootStateWriter;
            action.Enabled = TerminalHelpers.WeaponIsReadyAndSorter;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }


        public static void CreateShootOff(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Shoot_Off");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionShoot_Off"));
            action.Action = CustomActions.TerminalActionShootOff;
            action.Writer = CustomActions.ShootStateWriter;
            action.Enabled = TerminalHelpers.WeaponIsReadyAndSorter;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateShootOn(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Shoot_On");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionShoot_On"));
            action.Action = CustomActions.TerminalActionShootOn;
            action.Writer = CustomActions.ShootStateWriter;
            action.Enabled = TerminalHelpers.WeaponIsReadyAndSorter;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateSubSystems(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("SubSystems");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionSubSystems"));
            action.Action = CustomActions.TerminActionCycleSubSystem;
            action.Writer = CustomActions.SubSystemWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateCamera(Session session)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>($"Next Camera Channel");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionNextCameraChannel"));
            action0.Action = CustomActions.TerminalActionCameraIncrease;
            action0.Writer = CustomActions.CameraWriter;
            action0.Enabled = TerminalHelpers.IsTrue;
            action0.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>($"Previous Camera Channel");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionPreviousCameraChannel"));
            action1.Action = CustomActions.TerminalActionCameraDecrease;
            action1.Writer = CustomActions.CameraWriter;
            action1.Enabled = TerminalHelpers.IsReady;
            action1.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        public static void CreateControlModes(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ControlModes");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionControlModes"));
            action.Action = CustomActions.TerminalActionControlMode;
            action.Writer = CustomActions.ControlStateWriter;
            action.Enabled = TerminalHelpers.TurretOrGuidedAmmoAny;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateNeutrals(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Neutrals");
            action.Icon = @"Textures\GUI\Icons\Actions\NeutralToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionNeutrals"));
            action.Action = CustomActions.TerminalActionToggleNeutrals;
            action.Writer = CustomActions.NeutralWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateProjectiles(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Projectiles");
            action.Icon = @"Textures\GUI\Icons\Actions\MissileToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionProjectiles"));
            action.Action = CustomActions.TerminalActionToggleProjectiles;
            action.Writer = CustomActions.ProjectilesWriter;
            action.Enabled = TerminalHelpers.TrackProjectiles;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }
        public static void CreateSupportingPD(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("SupportingPD");
            action.Icon = @"Textures\GUI\Icons\Actions\MissileToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionSupportingPD"));
            action.Action = CustomActions.TerminalActionToggleSupportingPD;
            action.Writer = CustomActions.SupportingPDWriter;
            action.Enabled = TerminalHelpers.UiDisableSupportingPD;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateBiologicals(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Biologicals");
            action.Icon = @"Textures\GUI\Icons\Actions\CharacterToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionBiologicals"));
            action.Action = CustomActions.TerminalActionToggleBiologicals;
            action.Writer = CustomActions.BiologicalsWriter;
            action.Enabled = TerminalHelpers.TrackBiologicals;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateMeteors(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Meteors");
            action.Icon = @"Textures\GUI\Icons\Actions\MeteorToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionMeteors"));
            action.Action = CustomActions.TerminalActionToggleMeteors;
            action.Writer = CustomActions.MeteorsWriter;
            action.Enabled = TerminalHelpers.TrackMeteors;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateGrids(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Grids");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionGrids"));
            action.Action = CustomActions.TerminalActionToggleGrids;
            action.Writer = CustomActions.GridsWriter;
            action.Enabled = TerminalHelpers.TrackGrids;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateArmorShowArea(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Grids");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder("Grids On/Off");
            action.Action = CustomActions.SupportActionToggleShow;
            action.Writer = CustomActions.GridsWriter;
            action.Enabled = TerminalHelpers.HasSupport;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateFriendly(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Friendly");
            action.Icon = @"Textures\GUI\Icons\Actions\NeutralToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFriendly"));
            action.Action = CustomActions.TerminalActionToggleFriendly;
            action.Writer = CustomActions.FriendlyWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateUnowned(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Unowned");
            action.Icon = @"Textures\GUI\Icons\Actions\NeutralToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionUnowned"));
            action.Action = CustomActions.TerminalActionToggleUnowned;
            action.Writer = CustomActions.UnownedWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateLargeGrid(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("LargeGrid");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder("Target Large Grids");
            action.Action = CustomActions.TerminalActionToggleLargeGrid;
            action.Writer = CustomActions.LargeGridWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateSmallGrid(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("SmallGrid");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder("Target Small Grids");
            action.Action = CustomActions.TerminalActionToggleSmallGrid;
            action.Writer = CustomActions.SmallGridWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }
        public static void CreateAngularTracking(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("AngularTracking");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder("Predict Targets Angular Motion");
            action.Action = CustomActions.TerminalActionToggleAngularTracking;
            action.Writer = CustomActions.AngularTrackingWriter;
            action.Enabled = TerminalHelpers.IsNotBomb;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        
        public static void CreateFocusTargets(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("FocusTargets");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFocusTargets"));
            action.Action = CustomActions.TerminalActionToggleFocusTargets;
            action.Writer = CustomActions.FocusTargetsWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateFocusSubSystem(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("FocusSubSystem");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFocusSubSystem"));
            action.Action = CustomActions.TerminalActionToggleFocusSubSystem;
            action.Writer = CustomActions.FocusSubSystemWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateMaxSize(Session session)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>("MaxSize Increase");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionMaxSizeIncrease"));
            action0.Action = CustomActions.TerminalActionMaxSizeIncrease;
            action0.Writer = CustomActions.MaxSizeWriter;
            action0.Enabled = TerminalHelpers.HasTracking;
            action0.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>("MaxSize Decrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionMaxSizeDecrease"));
            action1.Action = CustomActions.TerminalActionMaxSizeDecrease;
            action1.Writer = CustomActions.MaxSizeWriter;
            action1.Enabled = TerminalHelpers.HasTracking;
            action1.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        public static void CreateMinSize(Session session)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>("MinSize Increase");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionMinSizeIncrease"));
            action0.Action = CustomActions.TerminalActionMinSizeIncrease;
            action0.Writer = CustomActions.MinSizeWriter;
            action0.Enabled = TerminalHelpers.HasTracking;
            action0.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);
            
            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>("MinSize Decrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionMinSizeDecrease"));
            action1.Action = CustomActions.TerminalActionMinSizeDecrease;
            action1.Writer = CustomActions.MinSizeWriter;
            action1.Enabled = TerminalHelpers.HasTracking;
            action1.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        public static void CreateMovementState(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("TrackingMode");
            action.Icon = @"Textures\GUI\Icons\Actions\MovingObjectToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionTrackingMode"));
            action.Action = CustomActions.TerminalActionMovementMode;
            action.Writer = CustomActions.MovementModeWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        internal static void CreateCycleAmmo(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("WC_CycleAmmo");
            action.Icon = session.ModPath() + @"\Textures\GUI\Icons\Actions\Cycle_Ammo.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionWC_CycleAmmo"));
            action.Action = CustomActions.TerminalActionCycleAmmo;
            action.Writer = CustomActions.AmmoSelectionWriter;
            action.Enabled = TerminalHelpers.AmmoSelection;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }
        internal static void CreateRepelMode(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_RepelMode");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionWC_RepelMode"));
            action.Action = CustomActions.TerminalActionToggleRepelMode;
            action.Writer = CustomActions.RepelWriter;
            action.Enabled = TerminalHelpers.HasTracking;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateWeaponCameraChannels(Session session)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>("WC_Increase_CameraChannel");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionWC_Increase_CameraChannel"));
            action0.Action = CustomActions.TerminalActionCameraChannelIncrease;
            action0.Writer = CustomActions.WeaponCameraChannelWriter;
            action0.Enabled = TerminalHelpers.HasTracking;
            action0.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_Decrease_CameraChannel");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionWC_Decrease_CameraChannel"));
            action1.Action = CustomActions.TerminalActionCameraChannelDecrease;
            action1.Writer = CustomActions.WeaponCameraChannelWriter;
            action1.Enabled = TerminalHelpers.HasTracking;
            action1.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        public static void CreateLeadGroups(Session session)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>("WC_Increase_LeadGroup");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionWC_Increase_LeadGroup"));
            action0.Action = CustomActions.TerminalActionLeadGroupIncrease;
            action0.Writer = CustomActions.LeadGroupWriter;
            action0.Enabled = TerminalHelpers.TargetLead;
            action0.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_Decrease_LeadGroup");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionWC_Decrease_LeadGroup"));
            action1.Action = CustomActions.TerminalActionLeadGroupDecrease;
            action1.Writer = CustomActions.LeadGroupWriter;
            action1.Enabled = TerminalHelpers.TargetLead;
            action1.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        public static void CreateDecoy(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>($"Mask");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionMask"));
            action.Action = CustomActions.TerminActionCycleDecoy;
            action.Writer = CustomActions.DecoyWriter;
            action.Enabled = TerminalHelpers.IsTrue;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        internal static void CreateOnOffActionSet(Session session, IMyTerminalControlOnOffSwitch tc, string name, Func<IMyTerminalBlock, bool> enabler, bool group = false)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Toggle");
            action0.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action0.Name = new StringBuilder(Localization.GetTextWithoutFallback($"{name} Toggle On/Off"));
            action0.Action = b => tc.Setter(b, !tc.Getter(b));
            action0.Writer = (b, t) => t.Append(tc.Getter(b) ? tc.OnText : tc.OffText);
            action0.Enabled = enabler;
            action0.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Toggle_On");
            action1.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action1.Name = new StringBuilder(Localization.GetTextWithoutFallback($"{name} On"));
            action1.Action = b => tc.Setter(b, true);
            action1.Writer = (b, t) => t.Append(tc.Getter(b) ? tc.OnText : tc.OffText);
            action1.Enabled = enabler;
            action1.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);

            var action2 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Toggle_Off");
            action2.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            action2.Name = new StringBuilder(Localization.GetTextWithoutFallback($"{name} Off"));
            action2.Action = b => tc.Setter(b, true);
            action2.Writer = (b, t) => t.Append(tc.Getter(b) ? tc.OnText : tc.OffText);
            action2.Enabled = enabler;
            action2.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action2);
            session.CustomActions.Add(action2);

        }

        internal static void CreateOnOffActionSet(Session session, IMyTerminalControlCheckbox tc, string name, Func<IMyTerminalBlock, bool> enabler, bool group = false)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Toggle");
            action0.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action0.Name = new StringBuilder(Localization.GetTextWithoutFallback($"{name} Toggle On/Off"));
            action0.Action = b => tc.Setter(b, !tc.Getter(b));
            action0.Writer = (b, t) => t.Append(tc.Getter(b) ? tc.OnText : tc.OffText);
            action0.Enabled = enabler;
            action0.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Toggle_On");
            action1.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action1.Name = new StringBuilder(Localization.GetTextWithoutFallback($"{name} On"));
            action1.Action = b => tc.Setter(b, true);
            action1.Writer = (b, t) => t.Append(tc.Getter(b) ? tc.OnText : tc.OffText);
            action1.Enabled = enabler;
            action1.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);

            var action2 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Toggle_Off");
            action2.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            action2.Name = new StringBuilder(Localization.GetTextWithoutFallback($"{name} Off"));
            action2.Action = b => tc.Setter(b, true);
            action2.Writer = (b, t) => t.Append(tc.Getter(b) ? tc.OnText : tc.OffText);
            action2.Enabled = enabler;
            action2.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action2);
            session.CustomActions.Add(action2);

        }

        internal static void CreateSliderActionSet(Session session, IMyTerminalControlSlider tc, string name, int min, int max, float incAmt, Func<IMyTerminalBlock, bool> enabler, bool group)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Increase");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText(name));
            action0.Action = b => tc.Setter(b, tc.Getter(b) + incAmt <= max ? tc.Getter(b) + incAmt : max);
            action0.Writer = TerminalHelpers.EmptyStringBuilder;
            action0.Enabled = enabler;
            action0.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_{name}_Decrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText(name));
            action1.Action = b => tc.Setter(b, tc.Getter(b) - incAmt >= min ? tc.Getter(b) - incAmt : min);
            action1.Writer = TerminalHelpers.EmptyStringBuilder;
            action1.Enabled = enabler;
            action1.ValidForGroups = group;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        // Control Block Actions

        
        public static void CreateShareFireControlControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("WCShareFireControl");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionShareFireControl"));
            action.Action = CustomActions.TerminalActionToggleShareFireControlControl;
            action.Writer = CustomActions.ShareFireControlWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateAiEnabledControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("WCAiEnabled");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionWCAiEnabled"));
            action.Action = CustomActions.TerminalActionToggleAiEnabledControl;
            action.Writer = CustomActions.AiEnabledWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateSubSystemsControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("SubSystems");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionSubSystems"));
            action.Action = CustomActions.TerminActionCycleSubSystemControl;
            action.Writer = CustomActions.SubSystemWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateControlModesControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("ControlModes");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionControlModes"));
            action.Action = CustomActions.TerminalActionControlModeControl;
            action.Writer = CustomActions.ControlStateWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateNeutralsControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Neutrals");
            action.Icon = @"Textures\GUI\Icons\Actions\NeutralToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionNeutrals"));
            action.Action = CustomActions.TerminalActionToggleNeutralsControl;
            action.Writer = CustomActions.NeutralWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateProjectilesControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Projectiles");
            action.Icon = @"Textures\GUI\Icons\Actions\MissileToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionProjectiles"));
            action.Action = CustomActions.TerminalActionToggleProjectilesControl;
            action.Writer = CustomActions.ProjectilesWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }
        public static void CreateSupportingPDControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("SupportingPD");
            action.Icon = @"Textures\GUI\Icons\Actions\MissileToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionSupportingPD"));
            action.Action = CustomActions.TerminalActionToggleSupportingPDControl;
            action.Writer = CustomActions.SupportingPDWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateBiologicalsControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Biologicals");
            action.Icon = @"Textures\GUI\Icons\Actions\CharacterToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionBiologicals"));
            action.Action = CustomActions.TerminalActionToggleBiologicalsControl;
            action.Writer = CustomActions.BiologicalsWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateMeteorsControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Meteors");
            action.Icon = @"Textures\GUI\Icons\Actions\MeteorToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionMeteors"));
            action.Action = CustomActions.TerminalActionToggleMeteorsControl;
            action.Writer = CustomActions.MeteorsWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateGridsControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Grids");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionGrids"));
            action.Action = CustomActions.TerminalActionToggleGridsControl;
            action.Writer = CustomActions.GridsWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateFriendlyControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Friendly");
            action.Icon = @"Textures\GUI\Icons\Actions\NeutralToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFriendly"));
            action.Action = CustomActions.TerminalActionToggleFriendlyControl;
            action.Writer = CustomActions.FriendlyWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateUnownedControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("Unowned");
            action.Icon = @"Textures\GUI\Icons\Actions\NeutralToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionUnowned"));
            action.Action = CustomActions.TerminalActionToggleUnownedControl;
            action.Writer = CustomActions.UnownedWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateFocusTargetsControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("FocusTargets");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFocusTargets"));
            action.Action = CustomActions.TerminalActionToggleFocusTargetsControl;
            action.Writer = CustomActions.FocusTargetsWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateFocusSubSystemControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("FocusSubSystem");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionFocusSubSystem"));
            action.Action = CustomActions.TerminalActionToggleFocusSubSystemControl;
            action.Writer = CustomActions.FocusSubSystemWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateMaxSizeControl(Session session)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>("MaxSize Increase");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionMaxSizeIncrease"));
            action0.Action = CustomActions.TerminalActionMaxSizeIncreaseControl;
            action0.Writer = CustomActions.MaxSizeWriterControl;
            action0.Enabled = TerminalHelpers.CtcIsReady;
            action0.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>("MaxSize Decrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionMaxSizeDecrease"));
            action1.Action = CustomActions.TerminalActionMaxSizeDecreaseControl;
            action1.Writer = CustomActions.MaxSizeWriterControl;
            action1.Enabled = TerminalHelpers.CtcIsReady;
            action1.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        public static void CreateMinSizeControl(Session session)
        {
            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>("MinSize Increase");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionMinSizeIncrease"));
            action0.Action = CustomActions.TerminalActionMinSizeIncreaseControl;
            action0.Writer = CustomActions.MinSizeWriterControl;
            action0.Enabled = TerminalHelpers.CtcIsReady;
            action0.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);
            session.CustomActions.Add(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>("MinSize Decrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionMinSizeDecrease"));
            action1.Action = CustomActions.TerminalActionMinSizeDecreaseControl;
            action1.Writer = CustomActions.MinSizeWriterControl;
            action1.Enabled = TerminalHelpers.CtcIsReady;
            action1.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
            session.CustomActions.Add(action1);
        }

        public static void CreateMovementStateControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("TrackingMode");
            action.Icon = @"Textures\GUI\Icons\Actions\MovingObjectToggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionTrackingMode"));
            action.Action = CustomActions.TerminalActionMovementModeControl;
            action.Writer = CustomActions.MovementModeWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        internal static void CreateRepelModeControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>($"WC_RepelMode");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionWC_RepelMode"));
            action.Action = CustomActions.TerminalActionToggleRepelModeControl;
            action.Writer = CustomActions.RepelWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateLargeGridControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("LargeGrid");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder("Target Large Grids");
            action.Action = CustomActions.TerminalActionToggleLargeGridControl;
            action.Writer = CustomActions.LargeGridWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        public static void CreateSmallGridControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("SmallGrid");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder("Target Small Grids");
            action.Action = CustomActions.TerminalActionToggleSmallGridControl;
            action.Writer = CustomActions.SmallGridWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }

        internal static void CreateShootModeControl(Session session)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<T>("WCShootMode");
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Name = new StringBuilder(Localization.GetText("ActionWCShootMode"));
            action.Action = CustomActions.TerminActionCycleShootModeControl;
            action.Writer = CustomActions.ShootModeWriterControl;
            action.Enabled = TerminalHelpers.CtcIsReady;
            action.ValidForGroups = true;

            MyAPIGateway.TerminalControls.AddAction<T>(action);
            session.CustomActions.Add(action);
        }
    }
}

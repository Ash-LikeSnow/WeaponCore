using System;
using System.Collections.Generic;
using CoreSystems.Control;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;

namespace CoreSystems
{
    internal static partial class BlockUi
    {
        internal static void RequestSetRof(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            if (!MyUtils.IsEqual(newValue, comp.Data.Repo.Values.Set.RofModifier))
            {

                if (Session.I.IsServer)
                {
                    comp.Data.Repo.Values.Set.RofModifier = newValue;
                    Weapon.WeaponComponent.SetRof(comp);
                }
                else
                    Session.I.SendSetCompFloatRequest(comp, newValue, PacketType.RequestSetRof);
            }
        }


        internal static void RequestSetRange(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            if (!MyUtils.IsEqual(newValue, comp.Data.Repo.Values.Set.Range))
            {

                if (Session.I.IsServer)
                {

                    comp.Data.Repo.Values.Set.Range = newValue;
                    Weapon.WeaponComponent.SetRange(comp);
                    if (Session.I.MpActive)
                        Session.I.SendComp(comp);
                }
                else
                    Session.I.SendSetCompFloatRequest(comp, newValue, PacketType.RequestSetRange);
            }

        }

        internal static void FriendFill(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> arg1, List<MyTerminalControlListBoxItem> arg2)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            foreach (var f in comp.Friends)
            {
                arg1.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(f.DisplayName), MyStringId.NullOrEmpty, f));
            }
        }

        internal static void FriendSelect(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> list)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            foreach (var item in list)
            {
                var data = item.UserData as MyEntity;

                if (data != null)
                {
                    Log.Line($"{item.Text} - {data.DisplayName}");
                }
            }
            Weapon.WeaponComponent.RequestDroneSetValue(comp, "Friend", 0, Session.I.PlayerId);
        }

        internal static void EnemyFill(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> arg1, List<MyTerminalControlListBoxItem> arg2)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            foreach (var f in comp.Enemies)
            {
                arg1.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(f.DisplayName), MyStringId.NullOrEmpty, f));
            }
        }

        internal static void EnemySelect(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> list)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            foreach (var item in list)
            {
                var data = item.UserData as MyEntity;

                if (data != null)
                {
                    Log.Line($"{item.Text} - {data.DisplayName}");
                }
            }
            Weapon.WeaponComponent.RequestDroneSetValue(comp, "Enemy", 0, Session.I.PlayerId);
        }

        internal static void PositionFill(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> arg1, List<MyTerminalControlListBoxItem> arg2)
        {

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            foreach (var f in comp.Positions)
            {
                arg1.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(f.Key), MyStringId.NullOrEmpty, f.Value));
            }
        }

        internal static void PositionSelect(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> list)
        {
            foreach (var item in list)
            {
                var data = item.UserData as Vector3D? ?? new Vector3D();

                Log.Line($"{item.Text} - {data}");
            }

        }

        internal static void RequestSetOverload(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            if (Session.I.IsServer)  {

                comp.Data.Repo.Values.Set.Overload = newValue ? 2 : 1;
                Weapon.WeaponComponent.SetRof(comp);
                if (Session.I.MpActive)
                    Session.I.SendComp(comp);
            }
            else
                Session.I.SendSetCompBoolRequest(comp, newValue, PacketType.RequestSetOverload);
        }

        internal static void RequestSetReportTarget(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            if (Session.I.IsServer)
            {
                comp.Data.Repo.Values.Set.ReportTarget = newValue;
                if (Session.I.MpActive)
                    Session.I.SendComp(comp);
            }
            else
                Session.I.SendSetCompBoolRequest(comp, newValue, PacketType.RequestSetReportTarget);
        }

        internal static bool GetReportTarget(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.ReportTarget;
        }

        private const string KeyDisable = "Inactive";
        private const string KeyShoot = "Once";
        private const string KeyToggle = "Toggle";

        internal static string GetStringShootStatus(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return string.Empty;

            var value = ((int)comp.Data.Repo.Values.Set.Overrides.ShootMode);
            var active = value >= 2;

            return !active ? KeyDisable : value == 2 ? KeyToggle : KeyShoot;
        }

        internal static float GetRof(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return comp.Data.Repo.Values.Set.RofModifier;
        }
        internal static float GetMinRof(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            var minROF = 0f;
            for (int i = 0; i < comp.Collection.Count; i++)
            {
                var w = comp.Collection[i];
                if (w.ActiveAmmoDef == null)
                    return 0;

                var curMin = w.System.WConst.MinRateOfFire;
                if (curMin > minROF)
                    minROF = (float)curMin;
            }
            return minROF;
        }

        internal static float GetMaxRof(IMyTerminalBlock block)
        {
            return 1;
        }
        internal static bool GetOverload(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overload == 2;
        }


        internal static float GetRange(IMyTerminalBlock block) {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 100;
            return comp.Data.Repo.Values.Set.Range;
        }

        internal static bool ShowRange(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>();
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;

            return comp.HasTurret;
        }

        internal static float GetMinRange(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            var minTrajectory = float.MaxValue;
            for (int i = 0; i < comp.Collection.Count; i++)
            {
                var w = comp.Collection[i];
                if (w.ActiveAmmoDef == null)
                    return 0;

                var curMin = w.System.WConst.MinTargetDistance;
                if (curMin < minTrajectory)
                    minTrajectory = (float)curMin;
            }
            return minTrajectory;
        }

        internal static float GetMaxRange(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            var maxTrajectory = 0f;
            for (int i = 0; i < comp.Collection.Count; i++)
            {
                var w = comp.Collection[i];
                if (w.ActiveAmmoDef == null)
                    return 0;

                var curMax = w.GetMaxWeaponRange();
                if (curMax > maxTrajectory)
                    maxTrajectory = (float)curMax;
            }
            return maxTrajectory;
        }

        internal static bool GetNeutrals(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Neutrals;
        }

        internal static void RequestSetNeutrals(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Neutrals", value, Session.I.PlayerId);
        }

        internal static bool GetAdvanced(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || Session.I.Settings?.ClientConfig == null) return false;
            return Session.I.Settings.ClientConfig.AdvancedMode;
        }

        internal static void RequestAdvanced(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || Session.I.Settings?.ClientConfig == null || Session.I.Tick == Session.I.AdvancedToggleTick) return;
            var s = Session.I;

            s.AdvancedToggleTick = s.Tick;
            s.Settings.ClientConfig.AdvancedMode = !s.Settings.ClientConfig.AdvancedMode;
            s.Settings.VersionControl.UpdateClientCfgFile();
            comp.Cube.UpdateTerminal();
        }

        internal static bool GetDebug(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Debug;
        }

        internal static void RequestDebug(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Debug", value, Session.I.PlayerId);
        }

        internal static bool GetOverride(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Override;
        }

        
        internal static void RequestOverride(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Override", value, Session.I.PlayerId);
        }

        internal static bool GetAngularTracking(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.AngularTracking;
        }

        internal static void RequestAngularTracking(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "AngularTracking", value, Session.I.PlayerId);
        }
        internal static bool GetUnowned(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Unowned;
        }

        internal static void RequestSetUnowned(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Unowned", value, Session.I.PlayerId);
        }

        internal static bool GetFriendly(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Friendly;
        }

        internal static void RequestSetFriendly(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Friendly", value, Session.I.PlayerId);
        }

        internal static bool GetFocusFire(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.FocusTargets;
        }

        internal static void RequestSetFocusFire(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "FocusTargets", value, Session.I.PlayerId);
        }

        internal static bool GetSubSystems(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.FocusSubSystem;
        }

        internal static void RequestSetSubSystems(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            var value = newValue ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "FocusSubSystem", value, Session.I.PlayerId);
        }

        internal static bool GetBiologicals(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Biologicals;
        }

        internal static void RequestSetBiologicals(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Biologicals", value, Session.I.PlayerId);
        }

        internal static bool GetProjectiles(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Projectiles;
        }

        internal static void RequestSetProjectiles(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Projectiles", value, Session.I.PlayerId);
        }
        internal static bool GetSupportingPD(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.SupportingPD;
        }

        internal static void RequestSetSupportingPD(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "SupportingPD", value, Session.I.PlayerId);
        }

        internal static bool GetMeteors(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Meteors;
        }

        internal static void RequestSetMeteors(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Meteors", value, Session.I.PlayerId);
        }

        internal static bool GetGrids(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Grids;
        }

        internal static void RequestSetGrids(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Grids", value, Session.I.PlayerId);
        }

        internal static bool GetShareFireControl(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
            {
                return false;
            }
            return comp.Data.Repo.Values.Set.Overrides.ShareFireControl;
        }

        internal static bool GetShoot(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.State.Trigger == CoreComponent.Trigger.On;
        }

        internal static void RequestShareFireControl(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "ShareFireControl", value, Session.I.PlayerId);
        }

        internal static void RequestSetShoot(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            
            if (newValue)
                CustomActions.TerminalActionShootOn(block);
            else
                CustomActions.TerminalActionShootOff(block);

        }

        internal static long GetAmmos(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            for (int i = 0; i < comp.Collection.Count; i++)
            {
                var wep = comp.Collection[i];
                if (!wep.System.HasAmmoSelection)
                    continue;

                var ammos = wep.System.AmmoTypes;
                int ammoId = -1;
                for (int j = 0; j < ammos.Length; j++)
                {
                    var ammo = ammos[j];
                    if (!ammo.AmmoDef.Const.IsTurretSelectable) continue;
                    if (wep.DelayedCycleId != -1 && wep.AmmoName.EndsWith(ammo.AmmoDef.AmmoRound))
                    {
                        ammoId = j;
                        break;
                    }
                }
                return ammoId  == -1 ? wep.Reload.AmmoTypeId : ammoId;
            }
            return 0;
        }

        internal static void RequestSetAmmo(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            for (int i = 0; i < comp.Collection.Count; i++)
            {
                var wep = comp.Collection[i];
                if (!wep.System.HasAmmoSelection || newValue >= wep.System.AmmoTypes.Length || !wep.System.AmmoTypes[newValue].AmmoDef.Const.IsTurretSelectable)
                    continue;

                wep.QueueAmmoChange((int)newValue);
            }
        }


        internal static void ListAmmos(List<MyTerminalControlComboBoxItem> ammoList)
        {
            foreach (var ammo in AmmoList) ammoList.Add(ammo);
        }

        internal static readonly List<MyTerminalControlComboBoxItem> AmmoList = new List<MyTerminalControlComboBoxItem>();

        internal static long GetShootModes(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return (int)comp.Data.Repo.Values.Set.Overrides.ShootMode;
        }

        internal static void RequestShootModes(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || !ShootModeChangeReady(comp)) return;
            Weapon.WeaponComponent.RequestSetValue(comp, "ShootMode", (int)newValue, Session.I.PlayerId);
        }

        internal static void ListShootModes(List<MyTerminalControlComboBoxItem> shootModeList)
        {
            foreach (var sub in ShootModeList)
            {
                shootModeList.Add(sub);
            }
        }

        private static readonly List<MyTerminalControlComboBoxItem> ShootModeList = new List<MyTerminalControlComboBoxItem>
        {
            new MyTerminalControlComboBoxItem { Key = 0, Value = MyStringId.GetOrCompute($"{(Weapon.ShootManager.ShootModes)0}") },
            new MyTerminalControlComboBoxItem { Key = 1, Value = MyStringId.GetOrCompute($"{(Weapon.ShootManager.ShootModes)1}") },
            new MyTerminalControlComboBoxItem { Key = 2, Value = MyStringId.GetOrCompute($"{(Weapon.ShootManager.ShootModes)2}") },
            new MyTerminalControlComboBoxItem { Key = 3, Value = MyStringId.GetOrCompute($"{(Weapon.ShootManager.ShootModes)3}") },
        };

        internal static long GetSubSystem(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return (int)comp.Data.Repo.Values.Set.Overrides.SubSystem;
        }

        internal static void RequestSubSystem(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            Weapon.WeaponComponent.RequestSetValue(comp, "SubSystems", (int) newValue, Session.I.PlayerId);
        }

        internal static void ListSubSystems(List<MyTerminalControlComboBoxItem> subSystemList)
        {
            foreach (var sub in SubList) subSystemList.Add(sub);
        }

        private static readonly List<MyTerminalControlComboBoxItem> SubList = new List<MyTerminalControlComboBoxItem>
        {
            new MyTerminalControlComboBoxItem { Key = 0, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)0}") },
            new MyTerminalControlComboBoxItem { Key = 1, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)1}") },
            new MyTerminalControlComboBoxItem { Key = 2, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)2}") },
            new MyTerminalControlComboBoxItem { Key = 3, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)3}") },
            new MyTerminalControlComboBoxItem { Key = 4, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)4}") },
            new MyTerminalControlComboBoxItem { Key = 5, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)5}") },
            new MyTerminalControlComboBoxItem { Key = 6, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)6}") },
            new MyTerminalControlComboBoxItem { Key = 7, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)7}") },
        };

        internal static long GetMovementMode(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return (int)comp.Data.Repo.Values.Set.Overrides.MoveMode;
        }

        internal static void RequestMovementMode(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            Weapon.WeaponComponent.RequestSetValue(comp, "MovementModes", (int)newValue, Session.I.PlayerId);
        }

        internal static void ListMovementModes(List<MyTerminalControlComboBoxItem> moveList)
        {
            foreach (var sub in MoveList) moveList.Add(sub);
        }

        private static readonly List<MyTerminalControlComboBoxItem> MoveList = new List<MyTerminalControlComboBoxItem>
        {
            new MyTerminalControlComboBoxItem { Key = 0, Value = MyStringId.GetOrCompute($"{(ProtoWeaponOverrides.MoveModes)0}") },
            new MyTerminalControlComboBoxItem { Key = 1, Value = MyStringId.GetOrCompute($"{(ProtoWeaponOverrides.MoveModes)1}") },
            new MyTerminalControlComboBoxItem { Key = 2, Value = MyStringId.GetOrCompute($"{(ProtoWeaponOverrides.MoveModes)2}") },
            new MyTerminalControlComboBoxItem { Key = 3, Value = MyStringId.GetOrCompute($"{(ProtoWeaponOverrides.MoveModes)3}") },
        };

        internal static long GetControlMode(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return (int)comp.Data.Repo.Values.Set.Overrides.Control;
        }

        internal static void RequestControlMode(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            Weapon.WeaponComponent.RequestSetValue(comp, "ControlModes", (int)newValue, Session.I.PlayerId);
        }
        internal static long GetDecoySubSystem(IMyTerminalBlock block)
        {
            long value;
            long.TryParse(block.CustomData, out value);
            return value;
        }

        internal static void RequestDecoySubSystem(IMyTerminalBlock block, long newValue)
        {
            block.CustomData = newValue.ToString();
            block.RefreshCustomInfo();
        }

        internal static float GetBlockCamera(IMyTerminalBlock block)
        {
            long value;
            var group = long.TryParse(block.CustomData, out value) ? value : 0;
            return group;
        }

        internal static void RequestBlockCamera(IMyTerminalBlock block, float newValue)
        {
            var value = (long)Math.Round(newValue, 0);
            var customData = block.CustomData;
            long valueLong;
            if (string.IsNullOrEmpty(customData) || long.TryParse(block.CustomData, out valueLong))
            {
                block.CustomData = value.ToString();
                block.RefreshCustomInfo();
            }
        }

        internal static float GetWeaponCamera(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return comp.Data.Repo.Values.Set.Overrides.CameraChannel;
        }

        internal static void RequestSetBlockCamera(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = (int)Math.Round(newValue);
            if (value != comp.Data.Repo.Values.Set.Overrides.CameraChannel)
            {
                Weapon.WeaponComponent.RequestSetValue(comp, "CameraChannel", value, Session.I.PlayerId);
            }
        }

        internal static float GetBurstCount(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return comp.Data.Repo.Values.Set.Overrides.BurstCount;
        }

        internal static void RequestSetBurstCount(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || !ShootModeChangeReady(comp)) return;

            var roundedInt = (int)Math.Round(newValue);
            var values = comp.Data.Repo.Values;

            if (roundedInt != values.Set.Overrides.BurstCount)
            {
                Weapon.WeaponComponent.RequestSetValue(comp, "BurstCount", roundedInt, Session.I.PlayerId);
            }
        }

        internal static float GetBurstDelay(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return comp.Data.Repo.Values.Set.Overrides.BurstDelay;
        }

        internal static void RequestSetBurstDelay(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || !ShootModeChangeReady(comp)) return;

            var roundedInt = (int)Math.Round(newValue);
            var values = comp.Data.Repo.Values;

            if (roundedInt != values.Set.Overrides.BurstDelay)
            {
                Weapon.WeaponComponent.RequestSetValue(comp, "BurstDelay", roundedInt, Session.I.PlayerId);
            }
        }

        internal static float GetSequenceId(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return -1;
            return comp.Data.Repo.Values.Set.Overrides.SequenceId;
        }

        internal static bool ValidSequenceId(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;

            return !comp.DuplicateSequenceId();
        }

        internal static void RequestSetSequenceId(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || !ShootModeChangeReady(comp)) return;

            var roundedInt = (int)Math.Round(newValue);
            var values = comp.Data.Repo.Values;

            if (roundedInt != values.Set.Overrides.SequenceId)
            {
                Weapon.WeaponComponent.RequestSetValue(comp, "SequenceId", roundedInt, Session.I.PlayerId);
            }
        }

        internal static float GetWeaponGroupId(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return comp.Data.Repo.Values.Set.Overrides.WeaponGroupId;
        }

        internal static void RequestSetWeaponGroupId(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || !ShootModeChangeReady(comp)) return;

            var roundedInt = (int)Math.Round(newValue);
            var values = comp.Data.Repo.Values;

            if (roundedInt != values.Set.Overrides.WeaponGroupId)
            {
                Weapon.WeaponComponent.RequestSetValue(comp, "WeaponGroupId", roundedInt, Session.I.PlayerId);
            }
        }

        internal static bool ShootModeChangeReady(Weapon.WeaponComponent comp)
        {
            var values = comp.Data.Repo.Values;
            var higherClientCount = comp.ShootManager.ClientToggleCount > values.State.ToggleCount;
            var ready =  !comp.ShootManager.WaitingShootResponse && !comp.ShootManager.FreezeClientShoot && !higherClientCount;

            if (!ready)
            {
                var ammoState = comp.AmmoStatus();
                Log.Line($"Shoot failed: wait:{comp.ShootManager.WaitingShootResponse} - freeze:{comp.ShootManager.FreezeClientShoot} - lockTime:{Session.I.Tick - comp.ShootManager.WaitingTick} - shootTime:{Session.I.Tick - comp.ShootManager.LastShootTick} - cycles:{comp.ShootManager.CompletedCycles} - ammoState:{ammoState} ", Session.InputLog);
                var overWaitTime = comp.ShootManager.WaitingTick > 0 && Session.I.Tick - comp.ShootManager.WaitingTick > 180;
                var overFreezeTime = comp.ShootManager.FreezeTick > 0 && Session.I.Tick - comp.ShootManager.FreezeTick > 180;

                var freezeOver = comp.ShootManager.FreezeClientShoot && overFreezeTime;
                var waitOver = (comp.ShootManager.WaitingShootResponse || higherClientCount) && overWaitTime;

                if (freezeOver || waitOver)
                {
                    Log.Line($"freezeOver:{freezeOver} - waitOver:{waitOver} - higherClientCount:{higherClientCount}", Session.InputLog);
                    comp.ShootManager.FailSafe();
                }
            }
            return ready;
        }

        internal static float GetArmedTimeRemaining(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            var value = (float)Math.Round(comp.Data.Repo.Values.Set.Overrides.ArmedTimer * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, 2);

            return value;
        }


        internal static float GetLeadGroup(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;
            return comp.Data.Repo.Values.Set.Overrides.LeadGroup;
        }

        internal static void RequestSetLeadGroup(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = (int)Math.Round(newValue, 0);
            if (value != comp.Data.Repo.Values.Set.Overrides.LeadGroup)
            {
                if (Session.I.HandlesInput)
                    Session.I.LeadGroupsDirty = true;

                Weapon.WeaponComponent.RequestSetValue(comp, "LeadGroup", value, Session.I.PlayerId);
            }
        }


        internal static float GetArmedTimer(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0f;
            return comp.Data.Repo.Values.Set.Overrides.ArmedTimer;
        }

        internal static void RequestSetArmedTimer(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = (int)Math.Round(newValue, 0);
            if (value != comp.Data.Repo.Values.Set.Overrides.ArmedTimer)
            {
                Weapon.WeaponComponent.RequestSetValue(comp, "ArmedTimer", value, Session.I.PlayerId);
            }
        }

        internal static bool GetArmed(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Armed;
        }

        internal static void RequestSetArmed(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Armed", value, Session.I.PlayerId);
            if (Session.I.IsServer) comp.Cube.UpdateTerminal();
        }

        internal static void TriggerCriticalReaction(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            Weapon.WeaponComponent.RequestCriticalReaction(comp);
        }

        internal static void StartCountDown(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            Weapon.WeaponComponent.RequestCountDown(comp, true);
            if (Session.I.IsServer) comp.Cube.UpdateTerminal();
        }

        internal static void StopCountDown(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            Weapon.WeaponComponent.RequestCountDown(comp, false);
            if (Session.I.IsServer) comp.Cube.UpdateTerminal();
        }

        internal static bool ShowCamera(IMyTerminalBlock block)
        {
            return true;
        }

        internal static float GetMinCameraChannel(IMyTerminalBlock block)
        {
            return 0;
        }

        internal static float GetMaxCameraChannel(IMyTerminalBlock block)
        {
            return 24;
        }

        internal static float GetMinBurstCount(IMyTerminalBlock block)
        {
            return 1;
        }

        internal static float GetMaxBurstCount(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            return comp.MaxAmmoCount;
        }

        internal static float GetMinBurstDelay(IMyTerminalBlock block)
        {
            return 0;
        }

        internal static float GetMaxBurstDelay(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            return 300;
        }

        internal static float GetMinSequenceId(IMyTerminalBlock block)
        {
            return -1;
        }

        internal static float GetMaxSequenceId(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            return 99;
        }

        internal static float GetMinWeaponGroupId(IMyTerminalBlock block)
        {
            return 0;
        }

        internal static float GetMaxWeaponGroupId(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return 0;

            return 100;
        }

        internal static float GetMinCriticalTime(IMyTerminalBlock block)
        {
            return 0;
        }

        internal static float GetMaxCriticalTime(IMyTerminalBlock block)
        {
            return 3600;
        }

        internal static float GetMinLeadGroup(IMyTerminalBlock block)
        {
            return 0;
        }

        internal static float GetMaxLeadGroup(IMyTerminalBlock block)
        {
            return 5;
        }

        internal static bool GetRepel(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.Repel;
        }

        internal static void RequestSetRepel(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "Repel", value, Session.I.PlayerId);
        }

        internal static bool GetSmallGrid(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.SmallGrid;
        }

        internal static void RequestSetSmallGrid(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "SmallGrid", value, Session.I.PlayerId);
        }

        internal static bool GetLargeGrid(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.LargeGrid;
        }

        internal static void RequestSetLargeGrid(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            Weapon.WeaponComponent.RequestSetValue(comp, "LargeGrid", value, Session.I.PlayerId);
        }

        internal static void ListControlModes(List<MyTerminalControlComboBoxItem> controlList)
        {
            foreach (var sub in ControlList) controlList.Add(sub);
        }

        private static readonly List<MyTerminalControlComboBoxItem> ControlList = new List<MyTerminalControlComboBoxItem>
        {
            new MyTerminalControlComboBoxItem { Key = 0, Value = MyStringId.GetOrCompute($"{(ProtoWeaponOverrides.ControlModes)0}") },
            new MyTerminalControlComboBoxItem { Key = 1, Value = MyStringId.GetOrCompute($"{(ProtoWeaponOverrides.ControlModes)1}") },
            new MyTerminalControlComboBoxItem { Key = 2, Value = MyStringId.GetOrCompute($"{(ProtoWeaponOverrides.ControlModes)2}") },
        };

        internal static void ListDecoySubSystems(List<MyTerminalControlComboBoxItem> subSystemList)
        {
            foreach (var sub in DecoySubList) subSystemList.Add(sub);
        }

        private static readonly List<MyTerminalControlComboBoxItem> DecoySubList = new List<MyTerminalControlComboBoxItem>()
        {
            new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)1}") },
            new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)2}") },
            new MyTerminalControlComboBoxItem() { Key = 3, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)3}") },
            new MyTerminalControlComboBoxItem() { Key = 4, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)4}") },
            new MyTerminalControlComboBoxItem() { Key = 5, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)5}") },
            new MyTerminalControlComboBoxItem() { Key = 6, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)6}") },
            new MyTerminalControlComboBoxItem() { Key = 7, Value = MyStringId.GetOrCompute($"{(WeaponDefinition.TargetingDef.BlockTypes)7}") },
        };
    }
}

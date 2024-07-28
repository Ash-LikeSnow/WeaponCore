using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using static CoreSystems.Platform.CorePlatform.PlatformState;
using VRage.Collections;
using static CoreSystems.Api.WcApi.DamageHandlerHelper;
using static CoreSystems.Projectiles.Projectile;

namespace CoreSystems.Api
{
    internal class ApiBackend
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;
        internal readonly Dictionary<string, Delegate> PbApiMethods;
        private readonly ImmutableDictionary<string, Delegate> _safeDictionary;

        internal ApiBackend()
        {

            ModApiMethods = new Dictionary<string, Delegate>
            {
                ["UnMonitorProjectile"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>>(UnMonitorProjectileCallbackLegacy),
                ["MonitorProjectile"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>>(MonitorProjectileCallbackLegacy),
                ["GetBlockWeaponMap"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, IDictionary<string, int>, bool>(GetBlockWeaponMap),

                ["GetAllWeaponDefinitions"] = new Action<IList<byte[]>>(GetAllWeaponDefinitions),
                ["GetAllWeaponMagazines"] = new Action<IDictionary<MyDefinitionId, List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>>>(GetAllWeaponMagazines),

                ["GetAllNpcSafeWeaponMagazines"] = new Action<IDictionary<MyDefinitionId, List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>>>(GetAllNpcSafeWeaponMagazines),
                ["GetNpcSafeWeapons"] = new Action<ICollection<MyDefinitionId>>(NpcSafeWeapons),

                ["GetCoreWeapons"] = new Action<ICollection<MyDefinitionId>>(GetCoreWeapons),
                ["GetCoreStaticLaunchers"] = new Action<ICollection<MyDefinitionId>>(GetCoreStaticLaunchers),
                ["GetCoreTurrets"] = new Action<ICollection<MyDefinitionId>>(GetCoreTurrets),
                ["GetCorePhantoms"] = new Action<ICollection<MyDefinitionId>>(GetCorePhantoms),
                ["GetCoreRifles"] = new Action<ICollection<MyDefinitionId>>(GetCoreRifles),
                ["GetCoreArmors"] = new Action<IList<byte[]>>(GetCoreArmors),

                ["GetMaxPower"] = new Func<MyDefinitionId, float>(GetMaxPower),
                ["RegisterProjectileAdded"] = new Action<Action<Vector3, float>>(RegisterProjectileAddedCallback),
                ["UnRegisterProjectileAdded"] = new Action<Action<Vector3, float>>(UnRegisterProjectileAddedCallback),
                ["RemoveMonitorProjectile"] = new Action<MyEntity, int, Action<long, int, ulong, long, Vector3D, bool>>(UnMonitorProjectileCallback),
                ["AddMonitorProjectile"] = new Action<MyEntity, int, Action<long, int, ulong, long, Vector3D, bool>>(MonitorProjectileCallback),
                ["GetProjectileState"] = new Func<ulong, MyTuple<Vector3D, Vector3D, float, float, long, string>>(GetProjectileState),
                ["ReleaseAiFocusBase"] = new Func<MyEntity, long, bool>(ReleaseAiFocus),

                ["TargetFocusHandler"] = new Func<long, bool, Func<MyEntity, IMyCharacter, long, int, bool>, bool>(TargetFocusHandler),
                ["HudHandler"] = new Func<long, bool, Func<IMyCharacter, long, int, bool>, bool>(HudHandler),
                ["ShootHandler"] = new Func<long, bool, Func<Vector3D, Vector3D, int, bool, object, int, int, int, bool>, bool>(ShootHandler),
                ["ShootRequest"] = new Func<MyEntity, object, int, double, bool>(ShootRequest),

                ["ReleaseAiFocusBase"] = new Func<MyEntity, long, bool>(ReleaseAiFocus),
                ["GetMagazineMap"] = new Func<MyEntity, int, MyTuple<MyDefinitionId, string, string, bool>>(GetMagazineMap),
                ["SetMagazine"] = new Func<MyEntity, int, MyDefinitionId, bool, bool>(SetMagazine),
                ["ForceReload"] = new Func<MyEntity, int, bool>(ForceReload),

                // Entity converted
                ["ToggleInfiniteAmmoBase"] = new Func<MyEntity, bool>(ToggleInfiniteResources),
                ["GetProjectilesLockedOnBase"] = new Func<MyEntity, MyTuple<bool, int, int>>(GetProjectilesLockedOn),
                ["GetProjectilesLockedOnPos"] = new Action<MyEntity, ICollection<Vector3D>>(GetProjectilesLockedOnPos),
                ["GetProjectilesLockedOn"] = new Func<IMyEntity, MyTuple<bool, int, int>>(GetProjectilesLockedOnLegacy),
                ["SetAiFocusBase"] = new Func<MyEntity, MyEntity, int, bool>(SetAiFocus),
                ["SetAiFocus"] = new Func<IMyEntity, IMyEntity, int, bool>(SetAiFocusLegacy),
                ["GetShotsFiredBase"] = new Func<MyEntity, int, int>(GetShotsFired),
                ["GetShotsFired"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, int>(GetShotsFiredLegacy),
                ["GetMuzzleInfoBase"] = new Action<MyEntity, int, List<MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>>>(GetMuzzleInfo),
                ["GetMuzzleInfo"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, int, List<MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>>>(GetMuzzleInfoLegacy),
                ["IsWeaponShootingBase"] = new Func<MyEntity, int, bool>(IsWeaponShooting),
                ["IsWeaponShooting"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, bool>(IsWeaponShootingLegacy),
                ["IsTargetValidBase"] = new Func<MyEntity, MyEntity, bool, bool, bool>(IsTargetValid),
                ["IsTargetValid"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, IMyEntity, bool, bool, bool>(IsTargetValidLegacy),
                ["GetWeaponScopeBase"] = new Func<MyEntity, int, MyTuple<Vector3D, Vector3D>>(GetWeaponScope),
                ["GetWeaponScope"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, MyTuple<Vector3D, Vector3D>>(GetWeaponScopeLegacy),
                ["GetCurrentPowerBase"] = new Func<MyEntity, float>(GetCurrentPower),
                ["GetCurrentPower"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, float>(GetCurrentPowerLegacy),
                ["DisableRequiredPowerBase"] = new Action<MyEntity>(ModOverride),
                ["DisableRequiredPower"] = new Action<Sandbox.ModAPI.IMyTerminalBlock>(ModOverrideLegacy),
                ["HasCoreWeaponBase"] = new Func<MyEntity, bool>(HasCoreWeapon),
                ["HasCoreWeapon"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, bool>(HasCoreWeaponLegacy),
                ["GetActiveAmmoBase"] = new Func<MyEntity, int, string>(GetActiveAmmo),
                ["GetActiveAmmo"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, string>(GetActiveAmmoLegacy),
                ["SetActiveAmmoBase"] = new Action<MyEntity, int, string>(SetActiveAmmo),
                ["SetActiveAmmo"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, int, string>(SetActiveAmmoLegacy),
                ["GetPlayerControllerBase"] = new Func<MyEntity, long>(GetPlayerController),
                ["GetPlayerController"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, long>(GetPlayerControllerLegacy),
                ["IsTargetAlignedBase"] = new Func<MyEntity, MyEntity, int, bool>(IsTargetAligned),
                ["IsTargetAligned"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, IMyEntity, int, bool>(IsTargetAlignedLegacy),
                ["IsTargetAlignedExtendedBase"] = new Func<MyEntity, MyEntity, int, MyTuple<bool, Vector3D?>>(IsTargetAlignedExtended),
                ["IsTargetAlignedExtended"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, IMyEntity, int, MyTuple<bool, Vector3D?>>(IsTargetAlignedExtendedLegacy),
                ["CanShootTargetBase"] = new Func<MyEntity, MyEntity, int, bool>(CanShootTarget),
                ["CanShootTarget"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, IMyEntity, int, bool>(CanShootTargetLegacy),
                ["GetPredictedTargetPositionBase"] = new Func<MyEntity, MyEntity, int, Vector3D?>(GetPredictedTargetPosition),
                ["GetPredictedTargetPosition"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, IMyEntity, int, Vector3D?>(GetPredictedTargetPositionLegacy),
                ["GetHeatLevelBase"] = new Func<MyEntity, float>(GetHeatLevel),
                ["GetHeatLevel"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, float>(GetHeatLevelLegacy),
                ["GetMaxWeaponRangeBase"] = new Func<MyEntity, int, float>(GetMaxWeaponRange),
                ["GetMaxWeaponRange"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, float>(GetMaxWeaponRangeLegacy),
                ["GetTurretTargetTypesBase"] = new Func<MyEntity, ICollection<string>, int, bool>(GetTurretTargetTypes),
                ["GetTurretTargetTypes"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, ICollection<string>, int, bool>(GetTurretTargetTypesLegacy),
                ["SetTurretTargetTypesBase"] = new Action<MyEntity, ICollection<string>, int>(SetTurretTargetTypes),
                ["SetTurretTargetTypes"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, ICollection<string>, int>(SetTurretTargetTypesLegacy),
                ["SetBlockTrackingRangeBase"] = new Action<MyEntity, float>(SetBlockTrackingRange),
                ["SetBlockTrackingRange"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, float>(SetBlockTrackingRangeLegacy),
                ["FireWeaponOnceBase"] = new Action<MyEntity, bool, int>(FireWeaponBurst),
                ["FireWeaponOnce"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, bool, int>(FireWeaponBurstLegacy),
                ["ToggleWeaponFireBase"] = new Action<MyEntity, bool, bool, int>(ToggleWeaponFire),
                ["ToggleWeaponFire"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, bool, bool, int>(ToggleWeaponFireLegacy),
                ["IsWeaponReadyToFireBase"] = new Func<MyEntity, int, bool, bool, bool>(IsWeaponReadyToFire),
                ["IsWeaponReadyToFire"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, bool, bool, bool>(IsWeaponReadyToFireLegacy),
                ["GetWeaponTargetBase"] = new Func<MyEntity, int, MyTuple<bool, bool, bool, MyEntity>>(GetWeaponTarget),
                ["GetWeaponTarget"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, MyTuple<bool, bool, bool, IMyEntity>>(GetWeaponTargetLegacy),
                ["SetWeaponTargetBase"] = new Action<MyEntity, MyEntity, int>(SetWeaponTarget),
                ["SetWeaponTarget"] = new Action<Sandbox.ModAPI.IMyTerminalBlock, IMyEntity, int>(SetWeaponTargetLegacy),
                ["GetWeaponAzimuthMatrixBase"] = new Func<MyEntity, int, Matrix>(GetWeaponAzimuthMatrix),
                ["GetWeaponAzimuthMatrix"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, Matrix>(GetWeaponAzimuthMatrixLegacy),
                ["GetWeaponElevationMatrixBase"] = new Func<MyEntity, int, Matrix>(GetWeaponElevationMatrix),
                ["GetWeaponElevationMatrix"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int, Matrix>(GetWeaponElevationMatrixLegacy),
                ["GetSortedThreatsBase"] = new Action<MyEntity, ICollection<MyTuple<MyEntity, float>>>(GetSortedThreats),
                ["GetSortedThreats"] = new Action<IMyEntity, ICollection<MyTuple<IMyEntity, float>>>(GetSortedThreatsLegacy),
                ["GetObstructionsBase"] = new Action<MyEntity, ICollection<MyEntity>>(GetObstructions),
                ["GetObstructions"] = new Action<IMyEntity, ICollection<IMyEntity>>(GetObstructionsLegacy),
                ["GetOptimalDpsBase"] = new Func<MyEntity, float>(GetOptimalDps),
                ["GetOptimalDps"] = new Func<IMyEntity, float>(GetOptimalDpsLegacy),
                ["HasGridAiBase"] = new Func<MyEntity, bool>(HasGridAi),
                ["HasGridAi"] = new Func<IMyEntity, bool>(HasGridAiLegacy),
                ["GetAiFocusBase"] = new Func<MyEntity, int, MyEntity>(GetAiFocus),
                ["GetAiFocus"] = new Func<IMyEntity, int, IMyEntity>(GetAiFocusLegacy),
                ["IsInRangeBase"] = new Func<MyEntity, MyTuple<bool, bool>>(IsInRange),
                ["IsInRange"] = new Func<IMyEntity, MyTuple<bool, bool>>(IsInRangeLegacy),
                ["GetConstructEffectiveDpsBase"] = new Func<MyEntity, float>(GetConstructEffectiveDps),
                ["GetConstructEffectiveDps"] = new Func<IMyEntity, float>(GetConstructEffectiveDpsLegacy),

                // New Additions
                ["SetRofMultiplier"] = new Action<MyEntity, float>(SetRofMultiplier),
                ["SetBaseDmgMultiplier"] = new Action<MyEntity, float>(SetBaseDmgMultiplier),
                ["SetAreaDmgMultiplier"] = new Action<MyEntity, float>(SetAreaDmgMultiplier),
                ["SetAreaRadiusMultiplier"] = new Action<MyEntity, float>(SetAreaRadiusMultiplier),
                ["SetVelocityMultiplier"] = new Action<MyEntity, float>(SetVelocityMultiplier),
                ["SetFiringAllowed"] = new Action<MyEntity, bool>(SetFiringAllowed),
                ["RegisterTerminalControl"] = new Action<string>(RegisterTerminalControl),

                ["GetRofMultiplier"] = new Func<MyEntity, float>(GetRofMultiplier),
                ["GetBaseDmgMultiplier"] = new Func<MyEntity, float>(GetBaseDmgMultiplier),
                ["GetAreaDmgMultiplier"] = new Func<MyEntity, float>(GetAreaDmgMultiplier),
                ["GetAreaRadiusMultiplier"] = new Func<MyEntity, float>(GetAreaRadiusMultiplier),
                ["GetVelocityMultiplier"] = new Func<MyEntity, float>(GetVelocityMultiplier),
                ["GetFiringAllowed"] = new Func<MyEntity, bool>(GetFiringAllowed),

                // Phantoms
                ["GetTargetAssessment"] = new Func<MyEntity, MyEntity, int, bool, bool, MyTuple<bool, bool, Vector3D?>>(GetPhantomTargetAssessment),
                //["GetPhantomInfo"] = new Action<string, ICollection<MyTuple<MyEntity, long, int, float, uint, long>>>(GetPhantomInfo),
                ["SetTriggerState"] = new Action<MyEntity, int>(SetPhantomTriggerState),
                ["AddMagazines"] = new Action<MyEntity, int, long>(AddPhantomMagazines),
                ["SetAmmo"] = new Action<MyEntity, int, string>(SetPhantomAmmo),
                ["ClosePhantom"] = new Func<MyEntity, bool>(ClosePhantom),
                ["SetFocusTarget"] = new Func<MyEntity, MyEntity, int, bool>(SetPhantomFocusTarget),
                ["SpawnPhantom"] = new Func<string, uint, bool, long, string, int, float?, MyEntity, bool, bool, long, bool, MyEntity>(SpawnPhantom),
                ["ToggleDamageEvents"] = new Action<Dictionary<MyEntity, MyTuple<Vector3D, Dictionary<MyEntity, List<MyTuple<int, float, Vector3I>>>>>>(ToggleDamageEvents),
                ["SetProjectileState"] = new Action<ulong, MyTuple<bool, Vector3D, Vector3D, float>>(SetProjectileState),
                ["DamageHandler"] = new Action<long, int, Action<ListReader<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>>>>(RegisterForDamageEvents),
                ["RegisterEventMonitor"] = new Action<MyEntity, int, Action<int, bool>>(RegisterEventMonitorCallback),
                ["UnRegisterEventMonitor"] = new Action<MyEntity, int, Action<int, bool>>(UnRegisterEventMonitorCallback),

            };
            PbApiMethods = new Dictionary<string, Delegate> 
            {
                ["GetCoreWeapons"] = new Action<ICollection<MyDefinitionId>>(GetCoreWeapons),
                ["GetCoreStaticLaunchers"] = new Action<ICollection<MyDefinitionId>>(GetCoreStaticLaunchers),
                ["GetCoreTurrets"] = new Action<ICollection<MyDefinitionId>>(GetCoreTurrets),
                ["GetBlockWeaponMap"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, IDictionary<string, int>, bool>(PbGetBlockWeaponMap),
                ["GetProjectilesLockedOn"] = new Func<long, MyTuple<bool, int, int>>(PbGetProjectilesLockedOn),
                ["GetSortedThreats"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, IDictionary<MyDetectedEntityInfo, float>>(PbGetSortedThreats),
                ["GetObstructions"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<MyDetectedEntityInfo>>(PbGetObstructions),
                ["GetAiFocus"] = new Func<long, int, MyDetectedEntityInfo>(PbGetAiFocus),
                ["SetAiFocus"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool>(PbSetAiFocus),
                ["ReleaseAiFocus"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool>(PbReleaseAiFocus),
                ["GetWeaponTarget"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, MyDetectedEntityInfo>(PbGetWeaponTarget),
                ["SetWeaponTarget"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int>(PbSetWeaponTarget),
                ["FireWeaponOnce"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, int>(PbFireWeaponBurst),
                ["ToggleWeaponFire"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, bool, int>(PbToggleWeaponFire),
                ["IsWeaponReadyToFire"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, bool, bool, bool>(PbIsWeaponReadyToFire),
                ["GetMaxWeaponRange"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, float>(PbGetMaxWeaponRange),
                ["GetTurretTargetTypes"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int, bool>(PbGetTurretTargetTypes),
                ["SetTurretTargetTypes"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int>(PbSetTurretTargetTypes),
                ["SetBlockTrackingRange"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(PbSetBlockTrackingRange),
                ["IsTargetAligned"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool>(PbIsTargetAligned),
                ["IsTargetAlignedExtended"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, MyTuple<bool, Vector3D?>>(PbIsTargetAlignedExtended),
                ["CanShootTarget"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool>(PbCanShootTarget),
                ["GetPredictedTargetPosition"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, Vector3D?>(PbGetPredictedTargetPosition),
                ["GetHeatLevel"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(PbGetHeatLevel),
                ["GetCurrentPower"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>(PbGetCurrentPower),
                ["GetMaxPower"] = new Func<MyDefinitionId, float>(GetMaxPower),
                ["HasGridAi"] = new Func<long, bool>(PbHasGridAi),
                ["HasCoreWeapon"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>(PbHasCoreWeapon),
                ["GetOptimalDps"] = new Func<long, float>(PbGetOptimalDps),
                ["GetActiveAmmo"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string>(PbGetActiveAmmo),
                ["SetActiveAmmo"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string>(PbSetActiveAmmo),
                ["RegisterProjectileAdded"] = new Action<Action<Vector3, float>>(RegisterProjectileAddedCallback),
                ["UnRegisterProjectileAdded"] = new Action<Action<Vector3, float>>(UnRegisterProjectileAddedCallback),
                ["UnMonitorProjectile"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>>(PbUnMonitorProjectileCallback),
                ["MonitorProjectile"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>>(PbMonitorProjectileCallback),
                ["GetProjectileState"] = new Func<ulong, MyTuple<Vector3D, Vector3D, float, float, long, string>>(GetProjectileState),
                ["GetConstructEffectiveDps"] = new Func<long, float>(PbGetConstructEffectiveDps),
                ["GetPlayerController"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long>(PbGetPlayerController),
                ["GetWeaponAzimuthMatrix"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix>(PbGetWeaponAzimuthMatrix),
                ["GetWeaponElevationMatrix"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix>(PbGetWeaponElevationMatrix),
                ["IsTargetValid"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool, bool, bool>(PbIsTargetValid),
                ["GetWeaponScope"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, MyTuple<Vector3D, Vector3D>>(PbGetWeaponScope),
                ["IsInRange"] = new Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, MyTuple<bool, bool>>(PbIsInRange),
                ["RegisterEventMonitor"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<int, bool>>(PbRegisterEventMonitorCallback),
                ["UnRegisterEventMonitor"] = new Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<int, bool>>(PbUnRegisterEventMonitorCallback),
            };

            var builder = ImmutableDictionary.CreateBuilder<string, Delegate>();

            foreach (var whyMe in PbApiMethods)
            {
                builder.Add(whyMe.Key, whyMe.Value);
            }

            _safeDictionary = builder.ToImmutable();

        }


        internal void PbInit()
        {
            var pb = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyDictionary<string, Delegate>, Sandbox.ModAPI.IMyTerminalBlock>("WcPbAPI");
            pb.Getter = b => _safeDictionary;
            MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(pb);
            Session.I.PbApiInited = true;
        }

        private void SetRofMultiplier(MyEntity blockEntity, float newRofModifier)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.Data.Repo.Values.Set.RofModifier = newRofModifier;
            Weapon.WeaponComponent.SetRof(comp);
            if (Session.I.MpActive)
                Session.I.SendComp(comp);
        }

        private float GetRofMultiplier(MyEntity blockEntity)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return -1;

            return comp.Data.Repo.Values.Set.RofModifier;
        }

        private void SetBaseDmgMultiplier(MyEntity blockEntity, float newDmgModifier)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.Data.Repo.Values.Set.BaseDamageMultiplier = newDmgModifier;
            Weapon.WeaponComponent.SetDmg(comp);
            if (Session.I.MpActive)
                Session.I.SendComp(comp);
        }

        private float GetBaseDmgMultiplier(MyEntity blockEntity)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return -1;

            return comp.Data.Repo.Values.Set.BaseDamageMultiplier;
        }

        private void SetAreaDmgMultiplier(MyEntity blockEntity, float newDmgModifier)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.Data.Repo.Values.Set.AreaDamageMultiplier = newDmgModifier;
            Weapon.WeaponComponent.SetDmg(comp);
            if (Session.I.MpActive)
                Session.I.SendComp(comp);
        }

        private void SetAreaRadiusMultiplier(MyEntity blockEntity, float newRadiusModifier)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.Data.Repo.Values.Set.AreaRadiusMultiplier = newRadiusModifier;
            Weapon.WeaponComponent.SetDmg(comp);
            if (Session.I.MpActive)
                Session.I.SendComp(comp);
        }

        private float GetAreaDmgMultiplier(MyEntity blockEntity)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return -1;

            return comp.Data.Repo.Values.Set.AreaDamageMultiplier;
        }

        private float GetAreaRadiusMultiplier(MyEntity blockEntity)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return -1;

            return comp.Data.Repo.Values.Set.AreaRadiusMultiplier;
        }

        private void SetVelocityMultiplier(MyEntity blockEntity, float newVelocityModifier)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.Data.Repo.Values.Set.VelocityMultiplier = newVelocityModifier;
            Weapon.WeaponComponent.SetVel(comp);
            if (Session.I.MpActive)
                Session.I.SendComp(comp);
        }

        private float GetVelocityMultiplier(MyEntity blockEntity)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return -1;

            return comp.Data.Repo.Values.Set.VelocityMultiplier;
        }

        private void SetFiringAllowed(MyEntity blockEntity, bool isFiringAllowed)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.Data.Repo.Values.Set.FiringAllowed = isFiringAllowed;
            Weapon.WeaponComponent.SetVel(comp);
            if (Session.I.MpActive)
                Session.I.SendComp(comp);
        }

        private bool GetFiringAllowed(MyEntity blockEntity)
        {
            IMyCubeBlock block = blockEntity as IMyCubeBlock;

            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;

            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return false;

            return comp.Data.Repo.Values.Set.FiringAllowed;
        }

        private void RegisterTerminalControl(string controlId)
        {
            if (!Session.VisibleControls.Contains(controlId))
                Session.VisibleControls.Add(controlId);
        }

        private void GetObstructionsLegacy(IMyEntity shooter, ICollection<IMyEntity> collection) => GetObstructions((MyEntity) shooter, (ICollection<MyEntity>) collection);
        private void GetObstructions(MyEntity shooter, ICollection<MyEntity> collection)
        {
            var grid = shooter?.GetTopMostParent() as MyCubeGrid;
            Ai gridAi;
            if (grid != null && collection != null && Session.I.EntityAIs.TryGetValue(grid, out gridAi))
            {
                for (int i = 0; i < gridAi.Obstructions.Count; i++)
                    collection.Add(gridAi.Obstructions[i].Target);
            }
        }

        private readonly ICollection<MyEntity> _tmpPbGetObstructions = new List<MyEntity>();
        private void PbGetObstructions(Sandbox.ModAPI.Ingame.IMyTerminalBlock shooter, ICollection<MyDetectedEntityInfo> collection)
        {
            if (shooter != null && collection != null)
            {
                collection.Clear();
                GetObstructions((MyEntity)shooter, _tmpPbGetObstructions);
                foreach (var i in _tmpPbGetObstructions)
                    collection.Add(GetDetailedEntityInfo(new MyTuple<bool, bool, bool, MyEntity>(true, false, false, i), (MyEntity)shooter));

                _tmpPbGetObstructions.Clear();
            }
        }

        private float PbGetConstructEffectiveDps(long arg)
        {
            return GetConstructEffectiveDps(MyEntities.GetEntityById(arg));
        }

        private void PbSetActiveAmmo(object arg1, int arg2, string arg3)
        {
            SetActiveAmmo((MyEntity) arg1, arg2, arg3);
        }

        private string PbGetActiveAmmo(object arg1, int arg2)
        {
            return GetActiveAmmo((MyEntity) arg1, arg2);
        }

        private float PbGetOptimalDps(long arg)
        {
            return GetOptimalDps(MyEntities.GetEntityById(arg));
        }

        private bool PbHasCoreWeapon(object arg)
        {
            return HasCoreWeapon((MyEntity) arg);
        }

        private bool PbHasGridAi(long arg)
        {
            return HasGridAi(MyEntities.GetEntityById(arg));
        }

        private float PbGetCurrentPower(object arg)
        {
            return GetCurrentPower((MyEntity) arg);
        }

        private float PbGetHeatLevel(object arg)
        {
            return GetHeatLevel((MyEntity) arg);
        }

        private Vector3D? PbGetPredictedTargetPosition(object arg1, long arg2, int arg3)
        {
            var block = arg1 as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            var target = MyEntities.GetEntityById(arg2);
            return GetPredictedTargetPositionOffset((MyEntity) block, target, arg3);
        }

        private bool PbCanShootTarget(object arg1, long arg2, int arg3)
        {
            var block = arg1 as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            var target = MyEntities.GetEntityById(arg2);

            return CanShootTarget((MyEntity) block, target, arg3);
        }

        private bool PbIsTargetAligned(object arg1, long arg2, int arg3)
        {
            var block = arg1 as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            var target = MyEntities.GetEntityById(arg2);

            return IsTargetAligned((MyEntity) block, target, arg3);
        }

        private MyTuple<bool, Vector3D?> PbIsTargetAlignedExtended(object arg1, long arg2, int arg3)
        {
            var block = arg1 as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            var target = MyEntities.GetEntityById(arg2);

            return IsTargetAlignedExtendedOffset((MyEntity) block, target, arg3);
        }

        private void PbSetBlockTrackingRange(object arg1, float arg2)
        {
            SetBlockTrackingRange((MyEntity) arg1, arg2);
        }

        private void PbSetTurretTargetTypes(object arg1, object arg2, int arg3)
        {
            SetTurretTargetTypes((MyEntity) arg1, (ICollection<string>) arg2, arg3);
        }

        private bool PbGetTurretTargetTypes(object arg1, object arg2, int arg3)
        {
            return GetTurretTargetTypes((MyEntity) arg1, (ICollection<string>) arg2, arg3);
        }

        private float PbGetMaxWeaponRange(object arg1, int arg2)
        {
            return GetMaxWeaponRange((MyEntity) arg1, arg2);
        }

        private bool PbIsWeaponReadyToFire(object arg1, int arg2, bool arg3, bool arg4)
        {
            return IsWeaponReadyToFire((MyEntity) arg1, arg2, arg3, arg4);
        }

        private void PbToggleWeaponFire(object arg1, bool arg2, bool arg3, int arg4)
        {
            ToggleWeaponFire((MyEntity) arg1, arg2, arg3, arg4);
        }

        private void PbFireWeaponBurst(object arg1, bool arg2, int arg3)
        {
            FireWeaponBurst((MyEntity) arg1, arg2, arg3);
        }

        private void PbSetWeaponTarget(object arg1, long arg2, int arg3)
        {
            SetWeaponTarget((MyEntity) arg1, MyEntities.GetEntityById(arg2), arg3);
        }

        private MyDetectedEntityInfo PbGetWeaponTarget(object arg1, int arg2)
        {
            var block = arg1 as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            var target = GetWeaponTarget((MyEntity) block, arg2);


            var result = GetDetailedEntityInfo(target, (MyEntity)arg1);

            return result;
        }

        private bool PbSetAiFocus(object arg1, long arg2, int arg3)
        {
            return SetAiFocus((MyEntity)arg1, MyEntities.GetEntityById(arg2), arg3);
        }

        private MyDetectedEntityInfo PbGetAiFocus(long arg1, int arg2)
        {
            var shooter = MyEntities.GetEntityById(arg1);
            return GetEntityInfo(GetAiFocus(shooter, arg2), shooter);
        }

        private MyDetectedEntityInfo GetDetailedEntityInfo(MyTuple<bool, bool, bool, MyEntity> target, MyEntity shooter)
        {
            var e = target.Item4;
            var shooterGrid = shooter.GetTopMostParent();
            var topTarget = e?.GetTopMostParent();
            var block = e as Sandbox.ModAPI.IMyTerminalBlock;
            var player = e as IMyCharacter;
            long entityId = 0;
            var relation = MyRelationsBetweenPlayerAndBlock.NoOwnership;
            var type = MyDetectedEntityType.Unknown;
            var name = string.Empty;

            Ai ai;
            Ai.TargetInfo info = null;

            if (shooterGrid != null && topTarget != null && Session.I.EntityToMasterAi.TryGetValue(shooterGrid, out ai) && ai.Construct.GetConstructTargetInfo(topTarget, out info)) {
                relation = info.EntInfo.Relationship;
                type = info.EntInfo.Type;
                var maxDist = ai.MaxTargetingRange + shooterGrid.PositionComp.WorldAABB.Extents.Max();
                if (Vector3D.DistanceSquared(e.PositionComp.WorldMatrixRef.Translation, shooterGrid.PositionComp.WorldMatrixRef.Translation) > (maxDist * maxDist))
                {
                    return new MyDetectedEntityInfo();
                }
            }

            if (!target.Item1 || e == null || topTarget?.Physics == null) {
                var projectile = target.Item2;
                var fake = target.Item3;
                if (fake) {
                    name = "ManualTargeting";
                    type = MyDetectedEntityType.None;
                    entityId = -2;
                }
                else if (projectile) {
                    name = "Projectile";
                    type = MyDetectedEntityType.Missile;
                    entityId = -1;
                }
                return new MyDetectedEntityInfo(entityId, name, type, info?.TargetPos, MatrixD.Zero, info != null ? (Vector3)info.Velocity : Vector3.Zero, relation, BoundingBoxD.CreateInvalid(), Session.I.Tick);
            }
            entityId = e.EntityId;
            var grid = topTarget as MyCubeGrid;
            if (grid != null) name = block != null ? block.CustomName : grid.DisplayName;
            else if (player != null) name = player.GetFriendlyName();
            else name = e.GetFriendlyName();

            return new MyDetectedEntityInfo(entityId, name, type, e.PositionComp.WorldAABB.Center, e.PositionComp.WorldMatrixRef, topTarget.Physics.LinearVelocity, relation, e.PositionComp.WorldAABB, Session.I.Tick);
        }

        private MyDetectedEntityInfo GetEntityInfo(MyEntity target, MyEntity shooter)
        {
            var e = target;
            if (e?.Physics == null)
                return new MyDetectedEntityInfo();

            var shooterGrid = shooter.GetTopMostParent();

            Ai ai;
            if (shooterGrid != null && Session.I.EntityToMasterAi.TryGetValue(shooterGrid, out ai))
            {
                var maxDist = ai.MaxTargetingRange + target.PositionComp.WorldAABB.Extents.Max();
                if (Vector3D.DistanceSquared(target.PositionComp.WorldMatrixRef.Translation, shooterGrid.PositionComp.WorldMatrixRef.Translation) > (maxDist * maxDist))
                {
                    return new MyDetectedEntityInfo();
                }
            }

            var grid = e.GetTopMostParent() as MyCubeGrid;
            var block = e as Sandbox.ModAPI.IMyTerminalBlock;
            var player = e as IMyCharacter;

            string name;
            MyDetectedEntityType type;
            var relation = MyRelationsBetweenPlayerAndBlock.Enemies;

            if (grid != null) {
                name = block != null ? block.CustomName : grid.DisplayName;
                type = grid.GridSizeEnum == MyCubeSize.Large ? MyDetectedEntityType.LargeGrid : MyDetectedEntityType.SmallGrid;
            }
            else if (player != null) {
                type = MyDetectedEntityType.CharacterOther;
                name = player.GetFriendlyName();

            }
            else {
                type = MyDetectedEntityType.Unknown;
                name = e.GetFriendlyName();
            }
            return new MyDetectedEntityInfo(e.EntityId, name, type, e.PositionComp.WorldAABB.Center, e.PositionComp.WorldMatrixRef, e.Physics.LinearVelocity, relation, e.PositionComp.WorldAABB, Session.I.Tick);
        }

        private readonly List<MyTuple<MyEntity, float>> _tmpTargetList = new List<MyTuple<MyEntity, float>>();
        private void PbGetSortedThreats(object arg1, object arg2)
        {
            var shooter = (MyEntity)arg1;
            GetSortedThreats(shooter, _tmpTargetList);
            
            var dict = (IDictionary<MyDetectedEntityInfo, float>) arg2;
            
            foreach (var i in _tmpTargetList)
                dict[GetDetailedEntityInfo(new MyTuple<bool, bool, bool, MyEntity>(true, false, false , i.Item1), shooter)] = i.Item2;

            _tmpTargetList.Clear();

        }

        private MyTuple<bool, int, int> PbGetProjectilesLockedOn(long arg)
        {
            return GetProjectilesLockedOn(MyEntities.GetEntityById(arg));
        }

        private bool PbGetBlockWeaponMap(object arg1, object arg2)
        {
            return GetBlockWeaponMap((Sandbox.ModAPI.IMyTerminalBlock) arg1, (IDictionary<string, int>)arg2);
        }

        private long PbGetPlayerController(object arg1)
        {
            return GetPlayerController((MyEntity)arg1);
        }

        private Matrix PbGetWeaponAzimuthMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, int arg2)
        {
            return GetWeaponAzimuthMatrix((MyEntity)arg1, arg2);
        }

        private Matrix PbGetWeaponElevationMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, int arg2)
        {
            return GetWeaponElevationMatrix((MyEntity)arg1, arg2);
        }

        private bool PbIsTargetValid(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, long arg2, bool arg3, bool arg4)
        {

            var block = arg1;
            var target = MyEntities.GetEntityById(arg2);

            return IsTargetValid((MyEntity) block, target, arg3, arg4);
        }

        private MyTuple<Vector3D, Vector3D> PbGetWeaponScope(Sandbox.ModAPI.Ingame.IMyTerminalBlock arg1, int arg2)
        {
            return GetWeaponScope((MyEntity)arg1, arg2);
        }

        // Block EntityId, PartId, ProjectileId, LastHitId, LastPos, Start 
        internal static void PbMonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponBlock, int weaponId, Action<long, int, ulong, long, Vector3D, bool> callback)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>();
            if (comp?.Platform != null && comp.Platform.Weapons.Count > weaponId)
                comp.ProjectileMonitors[weaponId].Add(callback);
        }

        // Block EntityId, PartId, ProjectileId, LastHitId, LastPos, Start 
        internal static void PbUnMonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponBlock, int weaponId, Action<long, int, ulong, long, Vector3D, bool> callback)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>();
            if (comp?.Platform != null && comp.Platform.Weapons.Count > weaponId)
                comp.ProjectileMonitors[weaponId].Remove(callback);
        }

        // terminalBlock, Threat, Other, Something 
        private MyTuple<bool, bool> PbIsInRange(object arg1)
        {
            var tBlock = arg1 as MyEntity;
            
            return tBlock != null ? IsInRange(MyEntities.GetEntityById(tBlock.EntityId)) : new MyTuple<bool, bool>();
        }
        
        // Non-PB Methods
        private void GetAllWeaponDefinitions(IList<byte[]> collection)
        {
            foreach (var wepDef in Session.I.WeaponDefinitions)
                collection.Add(MyAPIGateway.Utilities.SerializeToBinary(wepDef));
        }

        private void GetCoreWeapons(ICollection<MyDefinitionId> collection)
        {
            foreach (var def in Session.I.CoreSystemsDefs.Values)
                collection.Add(def);
        }

        private void NpcSafeWeapons(ICollection<MyDefinitionId> collection)
        {
            foreach (var def in Session.I.NpcSafeWeaponDefs.Values)
                collection.Add(def);
        }

        private void GetAllWeaponMagazines(IDictionary<MyDefinitionId, List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>> dictionary)
        {
            dictionary.Clear();
            foreach (var def in Session.I.SubTypeIdToWeaponMagMap)
            {
                var list = new List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>();
                dictionary[def.Key] = list;
                foreach (var map in def.Value)
                {
                    list.Add(new MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>
                    {
                        Item1 = map.WeaponId,
                        Item2 = new MyTuple<MyDefinitionId, string, string, bool>
                        {
                            Item1 = map.AmmoType.AmmoDefinitionId, Item2 = map.AmmoType.AmmoDef.AmmoMagazine,
                            Item3 = map.AmmoType.AmmoDef.AmmoRound, Item4 = map.AmmoType.AmmoDef.Const.SkipAimChecks
                        }
                    });
                }
            }
        }

        private void GetAllNpcSafeWeaponMagazines(IDictionary<MyDefinitionId, List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>> dictionary)
        {
            dictionary.Clear();
            foreach (var def in Session.I.SubTypeIdToNpcSafeWeaponMagMap)
            {
                var list = new List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>();
                dictionary[def.Key] = list;
                foreach (var map in def.Value)
                {
                    list.Add(new MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>
                    {
                        Item1 = map.WeaponId,
                        Item2 = new MyTuple<MyDefinitionId, string, string, bool>
                        {
                            Item1 = map.AmmoType.AmmoDefinitionId,
                            Item2 = map.AmmoType.AmmoDef.AmmoMagazine,
                            Item3 = map.AmmoType.AmmoDef.AmmoRound,
                            Item4 = map.AmmoType.AmmoDef.Const.SkipAimChecks
                        }
                    });
                }
            }
        }

        private void GetCoreStaticLaunchers(ICollection<MyDefinitionId> collection)
        {
            foreach (var def in Session.I.CoreSystemsFixedBlockDefs)
                collection.Add(def);
        }

        private void GetCorePhantoms(ICollection<MyDefinitionId> collection)
        {
            foreach (var def in Session.I.CoreSystemsPhantomDefs)
                collection.Add(def);
        }

        private void GetCoreRifles(ICollection<MyDefinitionId> collection)
        {
            foreach (var def in Session.I.CoreSystemsRifleDefs)
                collection.Add(def);
        }

        private void GetCoreArmors(IList<byte[]> collection)
        {
            foreach (var def in Session.I.CoreSystemsArmorDefs)
                collection.Add(MyAPIGateway.Utilities.SerializeToBinary(def));
        }
        private void GetCoreTurrets(ICollection<MyDefinitionId> collection)
        {
            foreach (var def in Session.I.CoreSystemsTurretBlockDefs)
                collection.Add(def);
        }

        internal long GetPlayerControllerLegacy(IMyEntity weaponBlock) => GetPlayerController((MyEntity) weaponBlock);
        internal long GetPlayerController(MyEntity weaponBlock)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready)
                return comp.Data.Repo.Values.State.PlayerId;

            return -1;
        }

        internal Matrix GetWeaponAzimuthMatrixLegacy(IMyEntity weaponBlock, int weaponId = 0) => GetWeaponAzimuthMatrix((MyEntity) weaponBlock, weaponId);
        internal Matrix GetWeaponAzimuthMatrix(MyEntity weaponBlock, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready)
            {
                var weapon = comp.Platform.Weapons[weaponId];
                var matrix = weapon.AzimuthPart.Entity?.PositionComp.LocalMatrixRef ?? Matrix.Zero;
                return matrix;
            }

            return Matrix.Zero;
        }

        internal Matrix GetWeaponElevationMatrixLegacy(IMyEntity weaponBlock, int weaponId = 0) => GetWeaponElevationMatrix((MyEntity) weaponBlock, weaponId);
        internal Matrix GetWeaponElevationMatrix(MyEntity weaponBlock, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready)
            {
                var weapon = comp.Platform.Weapons[weaponId];
                var matrix = weapon.ElevationPart.Entity?.PositionComp.LocalMatrixRef ?? Matrix.Zero;
                return matrix;
            }

            return Matrix.Zero;
        }

        private bool ToggleInfiniteResources(MyEntity entity)
        {
            CoreComponent comp;
            if (Session.I.IdToCompMap.TryGetValue(entity.EntityId, out comp))
            {
                comp.InfiniteResource = !comp.InfiniteResource;
                return comp.InfiniteResource;
            }

            Ai ai;
            if (Session.I.EntityToMasterAi.TryGetValue(entity, out ai))
            {
                return ai.Construct.GiveAllCompsInfiniteResources();
            }
            return false;
        }

        // handleEntityId is the EntityId of the grid you want to suppress target focus add/release/lock on.
        // All grids in that grids subgrid network are affected
        //
        // return type controls if the request is allowed to proceed
        private bool TargetFocusHandler(long handledEntityId, bool unRegister, Func<MyEntity, IMyCharacter, long, int, bool> callback)
        {
            if (unRegister)
                return Session.I.TargetFocusHandlers.Remove(handledEntityId);

            if (Session.I.TargetFocusHandlers.ContainsKey(handledEntityId))
                return false;

            Session.I.TargetFocusHandlers.Add(handledEntityId, callback);
            return true;
        }

        // handleEntityId is the EntityId of the grid you want to suppress hud on.
        // All grids in that grids subgrid network are affected
        //
        // return type determines if this hud element is restricted for player
        private bool HudHandler(long handledEntityId, bool unRegister, Func<IMyCharacter, long, int, bool> callback)
        {
            if (unRegister)
                return Session.I.HudHandlers.Remove(handledEntityId);

            if (Session.I.HudHandlers.ContainsKey(handledEntityId))
                return false;

            Session.I.HudHandlers.Add(handledEntityId, callback);
            return true;
        }

        // handleEntityId is the EntityId of the grid you want to suppress hud on.
        // All grids in that grids subgrid network are affected
        //
        // return type determines if this hud element is restricted for player
        private bool ShootHandler(long handledEntityId, bool unRegister, Func<Vector3D, Vector3D, int, bool, object, int, int, int, bool> callback)
        {
            if (unRegister)
                return Session.I.ShootHandlers.Remove(handledEntityId);

            if (Session.I.ShootHandlers.ContainsKey(handledEntityId))
                return false;

            Session.I.ShootHandlers.Add(handledEntityId, callback);
            return true;
        }

        private bool ShootRequest(MyEntity weaponEntity, object target, int weaponId, double additionalDeviateShotAngle)
        {
            var comp = weaponEntity.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.Weapons.Count > weaponId)
            {
                var weapon = comp.Platform.Weapons[weaponId];
                return weapon.ShootRequest.Update(target, additionalDeviateShotAngle);
            }
            return false;
        }


        private void PbRegisterEventMonitorCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponEntity, int weaponId, Action<int, bool> callBack) => RegisterEventMonitorCallback((MyEntity) weaponEntity, weaponId, callBack);
        private void RegisterEventMonitorCallback(MyEntity weaponEntity, int weaponId, Action<int, bool> callBack)
        {
            var comp = weaponEntity.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.Weapons.Count > weaponId)
                comp.EventMonitors[weaponId]?.Add(callBack);
        }

        private void PbUnRegisterEventMonitorCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponEntity, int weaponId, Action<int, bool> callBack) => UnRegisterEventMonitorCallback((MyEntity) weaponEntity, weaponId, callBack);
        private void UnRegisterEventMonitorCallback(MyEntity weaponEntity, int weaponId, Action<int, bool> callBack)
        {
            var comp = weaponEntity.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.Weapons.Count > weaponId)
                comp.EventMonitors[weaponId]?.Remove(callBack);
        }

        private bool GetBlockWeaponMap(Sandbox.ModAPI.IMyTerminalBlock weaponBlock, IDictionary<string, int> collection)
        {
            CoreStructure coreStructure;
            if (Session.I.PartPlatforms.TryGetValue(weaponBlock.SlimBlock.BlockDefinition.Id, out coreStructure) && (coreStructure is WeaponStructure))
            {
                foreach (var weaponSystem in coreStructure.PartSystems.Values)
                {
                    var system = weaponSystem;
                    if (!collection.ContainsKey(system.PartName))
                        collection.Add(system.PartName, ((WeaponSystem)system).WeaponId);
                }
                return true;
            }
            return false;
        }

        private MyTuple<bool, int, int> GetProjectilesLockedOnLegacy(IMyEntity entity) => GetProjectilesLockedOn((MyEntity) entity);
        private MyTuple<bool, int, int> GetProjectilesLockedOn(MyEntity entity)
        {
            var victim = entity;
            var grid = victim.GetTopMostParent();
            Ai ai;
            MyTuple<bool, int, int> tuple;
            if (grid != null && Session.I.EntityToMasterAi.TryGetValue(grid, out ai))
            {
                int count = 0;
                foreach (var proj in ai.LiveProjectile)
                {
                    if (proj.Value)
                        count++;
                }
                tuple = count > 0 ? new MyTuple<bool, int, int>(true, count, (int) (Session.I.Tick - ai.LiveProjectileTick)) : new MyTuple<bool, int, int>(false, 0, -1);
            }
            else tuple = new MyTuple<bool, int, int>(false, 0, -1);
            return tuple;
        }

        private void GetProjectilesLockedOnPos(MyEntity entity, ICollection<Vector3D> collection)
        {
            var victim = entity;
            var grid = victim.GetTopMostParent();
            Ai ai;
            collection.Clear();
            if (grid != null && Session.I.EntityToMasterAi.TryGetValue(grid, out ai))
            {
                foreach (var proj in ai.LiveProjectile)
                {
                    if(proj.Value)
                        collection.Add(proj.Key.Position);
                }
            }
            return;
        }

        private void GetSortedThreatsLegacy(IMyEntity shooter, ICollection<MyTuple<IMyEntity, float>> collection) => GetSortedThreatsConvert((MyEntity) shooter, collection);
        private void GetSortedThreatsConvert(MyEntity shooter, object collection) => GetSortedThreats(shooter, (ICollection<MyTuple<MyEntity, float>>) collection);

        private void GetSortedThreats(MyEntity shooter, ICollection<MyTuple<MyEntity, float>> collection)
        {
            var grid = shooter.GetTopMostParent();
            Ai ai;
            if (grid != null && Session.I.EntityAIs.TryGetValue(grid, out ai))
            {
                for (int i = 0; i < ai.SortedTargets.Count; i++)
                {
                    var targetInfo = ai.SortedTargets[i];
                    collection.Add(new MyTuple<MyEntity, float>(targetInfo.Target, targetInfo.OffenseRating));
                }
            }
        }

        private MyEntity GetAiFocusLegacy(IMyEntity shooter, int priority = 0) => GetAiFocus((MyEntity) shooter, priority);
        private MyEntity GetAiFocus(MyEntity shooter, int priority = 0)
        {
            var shootingGrid = shooter.GetTopMostParent();

            if (shootingGrid != null)
            {
                Ai ai;
                if (Session.I.EntityToMasterAi.TryGetValue(shootingGrid, out ai))
                    return MyEntities.GetEntityById(ai.Construct.Data.Repo.FocusData.Target);
            }
            return null;
        }

        private bool SetAiFocusLegacy(IMyEntity shooter, IMyEntity target, int priority = 0) => SetAiFocus((MyEntity) shooter, (MyEntity) target, priority);
        private bool SetAiFocus(MyEntity shooter, MyEntity target, int priority = 0)
        {
            var topEntity = shooter.GetTopMostParent();

            if (topEntity != null && target != null)
            {
                Ai ai;
                if (Session.I.EntityToMasterAi.TryGetValue(topEntity, out ai))
                {
                    var validAi = ai.Construct.RootAi?.Data?.Repo != null && !ai.Construct.RootAi.MarkedForClose && !ai.MarkedForClose && ai.Data.Repo != null;
                    if (!Session.I.IsServer || !validAi)
                        return false;

                    ai.Construct.Focus.ServerChangeFocus(target, ai, 0, Focus.ChangeMode.Add, true);
                    return true;
                }
            }
            return false;
        }

        private bool PbReleaseAiFocus(Sandbox.ModAPI.Ingame.IMyTerminalBlock shooter, long playerId) => ReleaseAiFocus((MyEntity) shooter, playerId);
        private bool ReleaseAiFocus(MyEntity shooter, long playerId)
        {
            var shootingGrid = shooter.GetTopMostParent();

            if (shootingGrid != null)
            {
                Ai ai;
                if (Session.I.EntityToMasterAi.TryGetValue(shootingGrid, out ai))
                {
                    var validAi = ai.Construct.RootAi?.Data?.Repo != null && !ai.Construct.RootAi.MarkedForClose && !ai.MarkedForClose && ai.Data.Repo != null;
                    if (!Session.I.IsServer || !validAi)
                        return false;

                    ai.Construct.Focus.RequestReleaseActive(ai, playerId);
                    return true;
                }
            }
            return false;
        }

        private MyTuple<MyDefinitionId, string, string, bool> GetMagazineMap(MyEntity weaponBlock, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];
                WeaponSystem.AmmoType ammoType;
                var ammoDef = w.ActiveAmmoDef;
                if (ammoDef != null && Session.I.AmmoDefIds.TryGetValue(ammoDef.AmmoDefinitionId, out ammoType))
                {
                    var result = new MyTuple<MyDefinitionId, string, string, bool>
                    {
                        Item1 = ammoType.AmmoDefinitionId, 
                        Item2 = ammoDef.AmmoDef.AmmoMagazine, 
                        Item3 = ammoDef.AmmoDef.AmmoRound, 
                        Item4 = ammoDef.AmmoDef.Const.SkipAimChecks
                    };
                    return result;
                }
                
            }
            return new MyTuple<MyDefinitionId, string, string, bool>();
        }

        private bool ForceReload(MyEntity weaponBlock, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var weapon = comp.Platform.Weapons[weaponId];
                weapon.Comp.ForceReload();
                return true;
            }
            return false;
        }

        private bool SetMagazine(MyEntity weaponBlock, int weaponId, MyDefinitionId id, bool forceReload)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            WeaponSystem.AmmoType ammoType;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId && Session.I.AmmoDefIds.TryGetValue(id, out ammoType))
            {

                var weapon = comp.Platform.Weapons[weaponId];
                weapon.QueueAmmoChange(ammoType.AmmoDef.Const.AmmoIdxPos);

                if (forceReload)
                    weapon.Comp.ForceReload();

                return true;
            }
            return false;
        }

        private static MyTuple<bool, bool, bool, IMyEntity> GetWeaponTargetLegacy(IMyEntity weaponBlock, int weaponId = 0)
        {
            var result = GetWeaponTarget((MyEntity) weaponBlock, weaponId);

            return new MyTuple<bool, bool, bool, IMyEntity>(result.Item1, result.Item2, result.Item3, result.Item4);
        }

        private static MyTuple<bool, bool, bool, MyEntity> GetWeaponTarget(MyEntity weaponBlock, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var weapon = comp.Platform.Weapons[weaponId];
                if (weapon.Target.TargetState == Target.TargetStates.IsFake)
                    return new MyTuple<bool, bool, bool, MyEntity>(true, false, true, null);
                if (weapon.Target.TargetState == Target.TargetStates.IsProjectile)
                    return new MyTuple<bool, bool, bool, MyEntity>(true, true, false, null);
                return new MyTuple<bool, bool, bool, MyEntity>(weapon.Target.TargetState == Target.TargetStates.IsEntity, false, false, (MyEntity) weapon.Target.TargetObject);
            }

            return new MyTuple<bool, bool, bool, MyEntity>(false, false, false, null);
        }

        private static void SetWeaponTargetLegacy(IMyEntity weaponBlock, IMyEntity target, int weaponId = 0) => SetWeaponTarget((MyEntity) weaponBlock, (MyEntity) target, weaponId);
        private static void SetWeaponTarget(MyEntity weaponBlock, MyEntity target, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
                Ai.AcquireTarget(comp.Platform.Weapons[weaponId], false, target);
        }

        private static void FireWeaponBurstLegacy(IMyEntity weaponBlock, bool allWeapons = true, int weaponId = 0) => FireWeaponBurst((MyEntity) weaponBlock, allWeapons, weaponId);
        private static void FireWeaponBurst(MyEntity weaponBlock, bool allWeapons = true, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                comp.ShootManager.RequestShootSync(Session.I.PlayerId, Weapon.ShootManager.RequestType.Once, Weapon.ShootManager.Signals.Once);
            }
        }
        private static void ToggleWeaponFireLegacy(IMyEntity weaponBlock, bool on, bool allWeapons = true, int weaponId = 0) => ToggleWeaponFire((MyEntity) weaponBlock,on, allWeapons, weaponId);
        private static void ToggleWeaponFire(MyEntity weaponBlock, bool on, bool allWeapons = true, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {

                comp.ShootManager.RequestShootSync(0, on ? Weapon.ShootManager.RequestType.On : Weapon.ShootManager.RequestType.Off, Weapon.ShootManager.Signals.On);
            }
        }
        private static bool IsWeaponReadyToFireLegacy(IMyEntity weaponBlock, int weaponId = 0, bool anyWeaponReady = true, bool shotReady = false) => IsWeaponReadyToFire((MyEntity) weaponBlock, weaponId, anyWeaponReady, shotReady);
        private static bool IsWeaponReadyToFire(MyEntity weaponBlock, int weaponId = 0, bool anyWeaponReady = true, bool shotReady = false)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId && comp.IsWorking)
            {
                for (int i = 0; i < comp.Platform.Weapons.Count; i++)
                {
                    if (!anyWeaponReady && i != weaponId) continue;
                    var w = comp.Platform.Weapons[i];
                    if (w.ShotReady) return true;
                }
            }

            return false;
        }
        private static float GetMaxWeaponRangeLegacy(IMyEntity weaponBlock, int weaponId = 0) => GetMaxWeaponRange((MyEntity) weaponBlock, weaponId);
        private static float GetMaxWeaponRange(MyEntity weaponBlock, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
                return (float)comp.Platform.Weapons[weaponId].MaxTargetDistance;

            return 0f;
        }
        private static bool GetTurretTargetTypesLegacy(IMyEntity weaponBlock, ICollection<string> collection, int weaponId = 0) => GetTurretTargetTypes((MyEntity) weaponBlock, collection, weaponId);
        private static bool GetTurretTargetTypes(MyEntity weaponBlock, ICollection<string> collection, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var weapon = comp.Platform.Weapons[weaponId];
                var threats = weapon.System.Values.Targeting.Threats;
                for (int i = 0; i < threats.Length; i++) collection.Add(threats[i].ToString());
                return true;
            }
            return false;
        }

        private void SetTurretTargetTypesLegacy(IMyEntity weaponBlock, ICollection<string> collection, int weaponId = 0) => SetTurretTargetTypes((MyEntity) weaponBlock, collection, weaponId);

        private readonly HashSet<WeaponDefinition.TargetingDef.Threat> _validRequests = new HashSet<WeaponDefinition.TargetingDef.Threat>();
        private void SetTurretTargetTypes(MyEntity weaponBlock, ICollection<string> collection, int weaponId = 0)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var weapon = comp.Platform.Weapons[weaponId];
                var threats = weapon.System.Values.Targeting.Threats;

                foreach (var request in collection)
                {
                    foreach (var validThreats in threats)
                    {
                        if (request == validThreats.ToString())
                            _validRequests.Add(validThreats);
                    }
                }

                foreach (var request in _validRequests)
                {
                    bool enabled;
                    string primaryName;
                    if (Weapon.WeaponComponent.GetThreatValue(comp, request.ToString(), out enabled, out primaryName))
                    {
                        Weapon.WeaponComponent.SetValue(comp, primaryName, enabled ? 0 : 1, 0);
                    }
                }
            }
            _validRequests.Clear();
        }

        private static void SetBlockTrackingRangeLegacy(IMyEntity weaponBlock, float range) =>SetBlockTrackingRange((MyEntity) weaponBlock, range);
        private static void SetBlockTrackingRange(MyEntity weaponBlock, float range)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready)
            {
                double maxBlockRange = 0d;
                for (int i = 0; i < comp.Platform.Weapons.Count; i++) {
                    var w = comp.Platform.Weapons[i];
                    
                    if (w.ActiveAmmoDef == null)
                        return;

                    var hardPointMax = w.System.WConst.MaxTargetDistance;
                    var ammoMax = w.ActiveAmmoDef.AmmoDef.Const.MaxTrajectory;
                    var weaponRange = Math.Min(hardPointMax, ammoMax);

                    if (weaponRange > maxBlockRange)
                        maxBlockRange = weaponRange;
                }
                range = (float)(maxBlockRange > range ? range : maxBlockRange);

                BlockUi.RequestSetRange(weaponBlock as Sandbox.ModAPI.IMyTerminalBlock, range);
            }
        }
        private static bool IsTargetAlignedLegacy(IMyEntity weaponBlock, IMyEntity targetEnt, int weaponId) => IsTargetAligned((MyEntity) weaponBlock, (MyEntity) targetEnt, weaponId);
        private static bool IsTargetAligned(MyEntity weaponBlock, MyEntity targetEnt, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];

                w.NewTarget.TargetObject = targetEnt;
                var dist = Vector3D.DistanceSquared(comp.CoreEntity.PositionComp.WorldMatrixRef.Translation, targetEnt.PositionComp.WorldMatrixRef.Translation);
                if (dist > w.MaxTargetDistanceSqr)
                {
                    return false;
                }

                Vector3D targetPos;
                return Weapon.TargetAligned(w, w.NewTarget, out targetPos);
            }
            return false;
        }
        private static MyTuple<bool, Vector3D?> IsTargetAlignedExtendedLegacy(IMyEntity weaponBlock, IMyEntity targetEnt, int weaponId) => IsTargetAlignedExtended((MyEntity) weaponBlock, (MyEntity) targetEnt, weaponId);
        private static MyTuple<bool, Vector3D?> IsTargetAlignedExtended(MyEntity weaponBlock, MyEntity targetEnt, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];

                w.NewTarget.TargetObject = targetEnt;

                Vector3D targetPos;
                var targetAligned = Weapon.TargetAligned(w, w.NewTarget, out targetPos);
                
                return new MyTuple<bool, Vector3D?>(targetAligned, targetAligned ? targetPos : (Vector3D?)null);
            }
            return new MyTuple<bool, Vector3D?>(false, null);
        }

        private static MyTuple<bool, Vector3D?> IsTargetAlignedExtendedOffset(MyEntity weaponBlock, MyEntity targetEnt, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];

                w.NewTarget.TargetObject = targetEnt;

                Vector3D targetPos;
                var targetAligned = Weapon.TargetAligned(w, w.NewTarget, out targetPos);

                return new MyTuple<bool, Vector3D?>(targetAligned, targetAligned ? w.PbRandomizePredictedPosition(targetPos) : (Vector3D?)null);
            }
            return new MyTuple<bool, Vector3D?>(false, null);
        }

        private static bool CanShootTargetLegacy(IMyEntity weaponBlock, IMyEntity targetEnt, int weaponId) => CanShootTarget((MyEntity) weaponBlock, (MyEntity) targetEnt, weaponId);
        private static bool CanShootTarget(MyEntity weaponBlock, MyEntity targetEnt, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {

                var w = comp.Platform.Weapons[weaponId];
                var dist = Vector3D.DistanceSquared(comp.CoreEntity.PositionComp.WorldMatrixRef.Translation, targetEnt.PositionComp.WorldMatrixRef.Translation);
                
                if (dist > w.MaxTargetDistanceSqr)
                {
                    return false;
                }

                var topMost = targetEnt.GetTopMostParent();
                var targetVel = topMost.Physics?.LinearVelocity ?? Vector3.Zero;
                var targetAccel = topMost.Physics?.AngularAcceleration ?? Vector3.Zero;
                Vector3D predictedPos;
                return Weapon.CanShootTargetObb(w, targetEnt, targetVel, targetAccel,  out predictedPos);
            }
            return false;
        }

        private static Vector3D? GetPredictedTargetPositionLegacy(IMyEntity weaponBlock, IMyEntity targetEnt, int weaponId) => GetPredictedTargetPosition((MyEntity) weaponBlock, (MyEntity) targetEnt, weaponId);
        private static Vector3D? GetPredictedTargetPosition(MyEntity weaponBlock, MyEntity targetEnt, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];
                if (Vector3D.DistanceSquared(targetEnt.PositionComp.WorldAABB.Center, weaponBlock.PositionComp.WorldAABB.Center) > w.ActiveAmmoDef.AmmoDef.Const.MaxTrajectorySqr)
                    return null;
                w.NewTarget.TargetObject = targetEnt;

                Vector3D targetPos;
                Weapon.TargetAligned(w, w.NewTarget, out targetPos);
                return targetPos;
            }
            return null;
        }

        private static Vector3D? GetPredictedTargetPositionOffset(MyEntity weaponBlock, MyEntity targetEnt, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];
                if (Vector3D.DistanceSquared(targetEnt.PositionComp.WorldAABB.Center, weaponBlock.PositionComp.WorldAABB.Center) > w.ActiveAmmoDef.AmmoDef.Const.MaxTrajectorySqr)
                    return null;
                w.NewTarget.TargetObject = targetEnt;

                Vector3D targetPos;
                Weapon.TargetAligned(w, w.NewTarget, out targetPos);
                
                return w.PbRandomizePredictedPosition(targetPos);
            }
            return null;
        }



        private static float GetHeatLevelLegacy(IMyEntity weaponBlock) => GetHeatLevel((MyEntity) weaponBlock);
        private static float GetHeatLevel(MyEntity weaponBlock)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.MaxHeat > 0)
            {
                return comp.CurrentHeat;
            }
            return 0f;
        }

        private static float GetCurrentPowerLegacy(IMyEntity weaponBlock) => GetCurrentPower((MyEntity) weaponBlock);
        private static float GetCurrentPower(MyEntity weaponBlock)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready)
                return comp.SinkPower;

            return 0f;
        }

        private float GetMaxPower(MyDefinitionId weaponDef)
        {
            return 0f; //Need to implement
        }

        private static void ModOverrideLegacy(IMyEntity weaponBlock) => ModOverride((MyEntity) weaponBlock);
        private static void ModOverride(MyEntity weaponBlock)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready)
            {
                comp.ModOverride = true;
                if (comp.Ai != null)
                    comp.Ai.ModOverride = true;
            }
        }

        private bool HasGridAiLegacy(IMyEntity entity) => HasGridAi((MyEntity) entity);
        private bool HasGridAi(MyEntity entity)
        {
            var grid = entity.GetTopMostParent();

            return grid != null && Session.I.EntityAIs.ContainsKey(grid);
        }

        private static bool HasCoreWeaponLegacy(IMyEntity weaponBlock) => HasCoreWeapon((MyEntity) weaponBlock);
        private static bool HasCoreWeapon(MyEntity weaponBlock)
        {
            return weaponBlock.Components.Has<CoreComponent>();
        }

        private float GetOptimalDpsLegacy(IMyEntity entity) => GetOptimalDps((MyEntity) entity);
        private float GetOptimalDps(MyEntity entity)
        {
            var weaponBlock = entity as Sandbox.ModAPI.IMyTerminalBlock;
            if (weaponBlock != null)
            {
                var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
                if (comp?.Platform != null && comp.Platform.State == Ready)
                    return comp.PeakDps;
            }
            else
            {
                var grid = entity.GetTopMostParent() as MyCubeGrid;
                Ai ai;
                if (grid != null && Session.I.EntityAIs.TryGetValue(grid, out ai))
                    return ai.OptimalDps;
            }
            return 0f;
        }

        private static string GetActiveAmmoLegacy(IMyEntity weaponBlock, int weaponId) => GetActiveAmmo((MyEntity) weaponBlock, weaponId);
        private static string GetActiveAmmo(MyEntity weaponBlock, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
                return comp.Platform.Weapons[weaponId].ActiveAmmoDef.AmmoDef.AmmoRound;

            return null;
        }

        private static void SetActiveAmmoLegacy(IMyEntity weaponBlock, int weaponId, string ammoTypeStr) => SetActiveAmmo((MyEntity) weaponBlock, weaponId, ammoTypeStr);
        private static void SetActiveAmmo(MyEntity weaponBlock, int weaponId, string ammoTypeStr)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && Session.I.IsServer && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];
                for (int i = 0; i < w.System.AmmoTypes.Length; i++)
                {
                    var ammoType = w.System.AmmoTypes[i];
                    if (ammoType.AmmoDef.AmmoRound == ammoTypeStr && ammoType.AmmoDef.Const.IsTurretSelectable)
                    {
                        if (Session.I.IsServer) {
                            w.Reload.AmmoTypeId = i;
                            if (Session.I.MpActive)
                                Session.I.SendWeaponReload(w);
                        }


                        break;
                    }
                }
            }
        }

        private void RegisterProjectileAddedCallback(Action<Vector3, float> callback)
        {
            Session.I.ProjectileAddedCallback += callback;
        }

        private void UnRegisterProjectileAddedCallback(Action<Vector3, float> callback)
        {
            try
            {
                Session.I.ProjectileAddedCallback -= callback;
            }
            catch (Exception e)
            {
                Log.Line($"Cannot remove Action, Action is not registered: {e}");
            }
        }

        // Block EntityId, PartId, ProjectileId, LastHitId, LastPos, Start 
        internal static void MonitorProjectileCallbackLegacy(Sandbox.ModAPI.IMyTerminalBlock weaponBlock, int weaponId, Action<long, int, ulong, long, Vector3D, bool> callback)
        {
            MonitorProjectileCallback(weaponBlock as MyEntity, weaponId, callback);
        }

        // Block EntityId, PartId, ProjectileId, LastHitId, LastPos, Start 
        internal static void UnMonitorProjectileCallbackLegacy(Sandbox.ModAPI.IMyTerminalBlock weaponBlock, int weaponId, Action<long, int, ulong, long, Vector3D, bool> callback)
        {
            UnMonitorProjectileCallback(weaponBlock as MyEntity, weaponId, callback);
        }

        // Block EntityId, PartId, ProjectileId, LastHitId, LastPos, Start 
        internal static void MonitorProjectileCallback(MyEntity entity, int weaponId, Action<long, int, ulong, long, Vector3D, bool> callback)
        {
            var comp = entity.Components.Get<CoreComponent>();
            if (comp?.Platform != null && comp.Platform.Weapons.Count > weaponId)
                comp.ProjectileMonitors[weaponId]?.Add(callback);
        }

        // Block EntityId, PartId, ProjectileId, LastHitId, LastPos, Start 
        internal static void UnMonitorProjectileCallback(MyEntity entity, int weaponId, Action<long, int, ulong, long, Vector3D, bool> callback)
        {
            var comp = entity.Components.Get<CoreComponent>();
            if (comp?.Platform != null && comp.Platform.Weapons.Count > weaponId)
                comp.ProjectileMonitors[weaponId]?.Remove(callback);
        }

        // POs, Velocity, baseDamageLeft, HealthLeft, TargetEntityId, AmmoName 
        private MyTuple<Vector3D, Vector3D, float, float, long, string> GetProjectileState(ulong projectileId)
        {
            Projectile p;
            if (Session.I.MonitoredProjectiles.TryGetValue(projectileId, out p))
                return new MyTuple<Vector3D, Vector3D, float, float, long, string>(p.Position, p.Velocity, p.Info.BaseDamagePool, p.Info.BaseHealthPool, p.Info.Target.TargetId, p.Info.AmmoDef.AmmoRound);

            return new MyTuple<Vector3D, Vector3D, float, float, long, string>();
        }

        // EndEarly, Pos, Additive Velocity, BaseDamagePool 
        private void SetProjectileState(ulong projectileId, MyTuple<bool, Vector3D, Vector3D, float> adjustments)
        {
            Projectile p;
            if (Session.I.MonitoredProjectiles.TryGetValue(projectileId, out p))
            {
                if (adjustments.Item1)
                {
                    p.EndState = EndStates.EarlyEnd;
                    p.DistanceToTravelSqr = (p.Info.DistanceTraveled * p.Info.DistanceTraveled);
                }
                else
                {
                    if (adjustments.Item2 != Vector3D.MinValue)
                    {
                        p.Position = adjustments.Item2;
                    }

                    if (adjustments.Item3 != Vector3D.MinValue)
                    {
                        p.Velocity += adjustments.Item3;
                    }

                    if (adjustments.Item4 > float.MinValue)
                    {
                        p.Info.BaseDamagePool = adjustments.Item4;
                    }
                }
            }
        }

        private float GetConstructEffectiveDpsLegacy(IMyEntity entity) => GetConstructEffectiveDps((MyEntity) entity);
        private float GetConstructEffectiveDps(MyEntity entity)
        {
            var topEntity = entity.GetTopMostParent();
            Ai ai;
            if (topEntity != null && Session.I.EntityToMasterAi.TryGetValue(topEntity, out ai))
                return ai.EffectiveDps;

            return 0;
        }

        private bool IsTargetValidLegacy(IMyEntity weaponBlock, IMyEntity targetEntity, bool onlyThreats, bool checkRelations) => IsTargetValid((MyEntity) weaponBlock, (MyEntity) targetEntity, onlyThreats, checkRelations);
        private bool IsTargetValid(MyEntity weaponBlock, MyEntity targetEntity, bool onlyThreats, bool checkRelations)
        {

            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready) {
                
                var ai = comp.MasterAi;
                
                Ai.TargetInfo targetInfo;
                if (ai.Targets.TryGetValue(targetEntity, out targetInfo)) {
                    var marked = targetInfo.Target?.MarkedForClose;
                    if (!marked.HasValue || marked.Value)
                        return false;
                    
                    if (!onlyThreats && !checkRelations)
                        return true;
                    
                    var isThreat = targetInfo.OffenseRating > 0;
                    var relation = targetInfo.EntInfo.Relationship;

                    var o = comp.Data.Repo.Values.Set.Overrides;
                    var shootNoOwners = o.Unowned && relation == MyRelationsBetweenPlayerAndBlock.NoOwnership;
                    var shootNeutrals = o.Neutrals && relation == MyRelationsBetweenPlayerAndBlock.Neutral;
                    var shootFriends = o.Friendly && relation == MyRelationsBetweenPlayerAndBlock.Friends;
                    var shootEnemies = relation == MyRelationsBetweenPlayerAndBlock.Enemies;
                    
                    if (onlyThreats && checkRelations)
                        return isThreat && (shootEnemies || shootNoOwners || shootNeutrals || shootFriends);

                    if (onlyThreats)
                        return isThreat;

                    if (shootEnemies || shootNoOwners || shootNeutrals || shootFriends)
                        return true;
                }
            }
            return false;
        }

        internal MyTuple<Vector3D, Vector3D> GetWeaponScopeLegacy(IMyEntity weaponBlock, int weaponId) => GetWeaponScope((MyEntity) weaponBlock, weaponId);
        internal MyTuple<Vector3D, Vector3D> GetWeaponScope(MyEntity weaponBlock, int weaponId)
        {
            var comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Platform.Weapons[weaponId];
                var info = w.GetScope.Info;
                return new MyTuple<Vector3D, Vector3D>(info.Position, info.Direction);
            }
            return new MyTuple<Vector3D, Vector3D>();
        }

        // block/grid entityId, Threat, Other 
        private MyTuple<bool, bool> IsInRangeLegacy(VRage.ModAPI.IMyEntity entity) => IsInRange((MyEntity) entity);
        private MyTuple<bool, bool> IsInRange(MyEntity entity)
        {
            var grid = entity.GetTopMostParent();
            Ai ai;
            if (grid != null && Session.I.EntityAIs.TryGetValue(grid, out ai))
            {
                return new MyTuple<bool, bool>(ai.DetectionInfo.PriorityInRange, ai.DetectionInfo.OtherInRange);
            }
            return new MyTuple<bool, bool>();
        }
        ///
        /// Phantoms
        /// 
        private static MyTuple<bool, bool, Vector3D?> GetPhantomTargetAssessment(MyEntity phantom, MyEntity target, int weaponId = 0, bool mustBeInRange = false, bool checkTargetObb = false)
        {
            var result = new MyTuple<bool, bool, Vector3D?>(false, false, null);
            var comp = phantom.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform != null && target != null && comp.Platform.State == Ready && comp.Platform.Weapons.Count > weaponId)
            {
                var w = comp.Collection[weaponId];

                var dist = Vector3D.DistanceSquared(comp.CoreEntity.PositionComp.WorldMatrixRef.Translation, target.PositionComp.WorldMatrixRef.Translation);
                var topMost = target.GetTopMostParent();
                var inRange = dist <= w.MaxTargetDistanceSqr;

                if (!inRange && mustBeInRange || topMost?.Physics == null)
                    return result;

                Vector3D targetPos;
                bool targetAligned;
                if (checkTargetObb) {

                    var targetVel = topMost.Physics.LinearVelocity;
                    var targetAccel = topMost.Physics.AngularAcceleration;
                    targetAligned =  Weapon.CanShootTargetObb(w, target, targetVel, targetAccel, out targetPos);
                }
                else
                {
                    targetAligned = Weapon.TargetAligned(w, w.NewTarget, out targetPos);
                }

                return new MyTuple<bool, bool, Vector3D?>(targetAligned, inRange, targetAligned ? targetPos : (Vector3D?)null);
            }
            return result;
        }


        private bool SetPhantomFocusTarget(MyEntity phantom, MyEntity target, int focusId)
        {
            Ai ai;
            if (target != null && !target.MarkedForClose && Session.I.EntityToMasterAi.TryGetValue(phantom, out ai))
            {
                if (!Session.I.IsServer)
                    return false;

                ai.Construct.Focus.ServerChangeFocus(target, ai, 0, Focus.ChangeMode.Add, true);
                return true;
            }

            return false;
        }
        private MyEntity SpawnPhantom(string phantomType, uint maxAge, bool closeWhenOutOfAmmo, long defaultReloads, string ammoOverideName, int trigger, float? modelScale, MyEntity parnet, bool addToPrunning, bool shadows, long identityId = 0, bool sync = false)
        {
            var ent = Session.I.CreatePhantomEntity(phantomType, maxAge, closeWhenOutOfAmmo, defaultReloads, ammoOverideName, (CoreComponent.Trigger)trigger, modelScale, parnet, addToPrunning, shadows, identityId, sync);
            return ent;
        }

        private bool ClosePhantom(MyEntity phantom)
        {
            Ai ai;
            CoreComponent comp;
            if (Session.I.EntityAIs.TryGetValue(phantom, out ai) && ai.CompBase.TryGetValue(phantom, out comp) && !comp.CloseCondition)
            {
                comp.ForceClose(comp.SubtypeName);
                return true;
            }
            return false;
        }

        private void SetPhantomAmmo(MyEntity phantom, int weaponId, string ammoName)
        {
            Ai ai;
            CoreComponent comp;
            if (Session.I.EntityAIs.TryGetValue(phantom, out ai) && ai.CompBase.TryGetValue(phantom, out comp) && comp is Weapon.WeaponComponent)
            {
                var wComp = (Weapon.WeaponComponent)comp;
                if (weaponId < wComp.Collection.Count)
                {
                    var w = wComp.Collection[weaponId];
                    foreach (var ammoType in w.System.AmmoTypes)
                    {
                        if (ammoType.AmmoDef.AmmoRound == ammoName)
                        {
                            w.ChangeAmmo(ammoType.AmmoDef.Const.AmmoIdxPos);
                            break;
                        }
                    }
                }
            }
        }

        private void AddPhantomMagazines(MyEntity phantom, int weaponId, long magCount)
        {
            Ai ai;
            CoreComponent comp;
            if (Session.I.EntityAIs.TryGetValue(phantom, out ai) && ai.CompBase.TryGetValue(phantom, out comp) && comp is Weapon.WeaponComponent)
            {
                var wComp = (Weapon.WeaponComponent)comp;
                if (weaponId < wComp.Collection.Count)
                {
                    var w = wComp.Collection[weaponId];
                    w.Reload.CurrentMags = (int)magCount;
                }
            }
        }

        private void SetPhantomTriggerState(MyEntity phantom, int trigger)
        {
            Ai ai;
            CoreComponent comp;
            if (Session.I.IsServer && Session.I.EntityAIs.TryGetValue(phantom, out ai) && ai.CompBase.TryGetValue(phantom, out comp) && comp is Weapon.WeaponComponent)
            {
                var wComp = (Weapon.WeaponComponent)comp;
                wComp.ResetShootState((CoreComponent.Trigger) trigger, ai.AiOwner);
            }
            else 
                Log.Line($"failed to set phantom trigger: {(CoreComponent.Trigger)trigger} - isServer:{Session.I.IsServer}");
        }

        private void GetPhantomInfo(string phantomSubtypeId, ICollection<MyTuple<MyEntity, long, int, float, uint, long>> collection)
        {
        }

        private void ToggleDamageEvents(Dictionary<MyEntity, MyTuple<Vector3D, Dictionary<MyEntity, List<MyTuple<int, float, Vector3I>>>>> obj)
        {
        }

        ///
        /// Hakerman;s Beam Logic
        /// 
        private bool IsWeaponShootingLegacy(IMyEntity weaponBlock, int weaponId) => IsWeaponShooting((MyEntity) weaponBlock, weaponId);
        private bool IsWeaponShooting(MyEntity weaponBlock, int weaponId)
        {
            Weapon.WeaponComponent comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (weaponId < comp.Collection.Count)
            {
                var w = comp.Collection[weaponId];

                return w.IsShooting;
            }
            return false;
        }
        private int GetShotsFiredLegacy(IMyEntity weaponBlock, int weaponId) => GetShotsFired((MyEntity) weaponBlock, weaponId);
        private int GetShotsFired(MyEntity weaponBlock, int weaponId)
        {
            Weapon.WeaponComponent comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp != null && weaponId < comp.Collection.Count)
            {
                var w = comp.Collection[weaponId];

                return w.ShotsFired;
            }
            return -1;
        }

        /// returns: A list that contains every muzzle's Position, LocalPosition, Direction, UpDirection, ParentMatrix, DummyMatrix
        private void GetMuzzleInfoLegacy(IMyEntity weaponBlock, int weaponId, List<MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>> output) => GetMuzzleInfo((MyEntity) weaponBlock, weaponId, output);
        private void GetMuzzleInfo(MyEntity weaponBlock, int weaponId, List<MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>> output)
        {
            Weapon.WeaponComponent comp = weaponBlock.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (weaponId < comp.Collection.Count)
            {
                var w = comp.Collection[weaponId];
                foreach (var m in w.Dummies)
                {
                    output.Add(new MyTuple<Vector3D, Vector3D, Vector3D, Vector3D, MatrixD, MatrixD>(
                        m.Info.Position,
                        m.Info.LocalPosition,
                        m.Info.Direction,
                        m.Info.UpDirection,
                        m.Info.ParentMatrix,
                        m.Info.DummyMatrix
                        ));
                }
            }
        }

        internal List<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>> ProjectileDamageEvents = new List<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>>();
        private void RegisterForDamageEvents(long modId, int eventType, Action<ListReader<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>>> callback)
        {
            DamageHandlerRegistrant oldEventReq;
            if (Session.I.DamageHandlerRegistrants.TryGetValue(modId, out oldEventReq))
            {
                Session.I.DamageHandlerRegistrants.Remove(modId);
                Session.I.SystemWideDamageRegistrants.Remove(modId);
                Session.I.GlobalDamageHandlerActive = Session.I.SystemWideDamageRegistrants.Count > 0;
            }

            if (eventType == (int)EventType.Unregister)
                return;

            oldEventReq = oldEventReq ?? new DamageHandlerRegistrant(callback);

            Session.I.DamageHandlerRegistrants[modId] = oldEventReq;

            if (eventType == (int)EventType.SystemWideDamageEvents) {

                Session.I.GlobalDamageHandlerActive = true;
                Session.I.SystemWideDamageRegistrants[modId] = oldEventReq;
            }
        }
    }
}

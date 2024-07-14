using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRage.Game;
using VRageMath;

namespace CoreSystems.Api
{
    /// <summary>
    /// https://github.com/Ash-LikeSnow/WeaponCore/blob/master/Data/Scripts/CoreSystems/Api/CoreSystemsPbApi.cs
    /// </summary>
    public class WcPbApi
    {
        private Action<ICollection<MyDefinitionId>> _getCoreWeapons;
        private Action<ICollection<MyDefinitionId>> _getCoreStaticLaunchers;
        private Action<ICollection<MyDefinitionId>> _getCoreTurrets;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, IDictionary<string, int>, bool> _getBlockWeaponMap;
        private Func<long, MyTuple<bool, int, int>> _getProjectilesLockedOn;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, IDictionary<MyDetectedEntityInfo, float>> _getSortedThreats;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo>> _getObstructions;
        private Func<long, int, MyDetectedEntityInfo> _getAiFocus;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _setAiFocus;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool> _releaseAiFocus;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, MyDetectedEntityInfo> _getWeaponTarget;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int> _setWeaponTarget;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, int> _fireWeaponOnce;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, bool, int> _toggleWeaponFire;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, bool, bool, bool> _isWeaponReadyToFire;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, float> _getMaxWeaponRange;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int, bool> _getTurretTargetTypes;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int> _setTurretTargetTypes;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _setBlockTrackingRange;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _isTargetAligned;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, MyTuple<bool, Vector3D?>> _isTargetAlignedExtended;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _canShootTarget;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, Vector3D?> _getPredictedTargetPos;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getHeatLevel;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _currentPowerConsumption;
        private Func<MyDefinitionId, float> _getMaxPower;
        private Func<long, bool> _hasGridAi;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> _hasCoreWeapon;
        private Func<long, float> _getOptimalDps;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _getActiveAmmo;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _setActiveAmmo;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>> _monitorProjectile;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>> _unMonitorProjectile;
        private Func<ulong, MyTuple<Vector3D, Vector3D, float, float, long, string>> _getProjectileState;
        private Func<long, float> _getConstructEffectiveDps;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long> _getPlayerController;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix> _getWeaponAzimuthMatrix;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix> _getWeaponElevationMatrix;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool, bool, bool> _isTargetValid;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, MyTuple<Vector3D, Vector3D>> _getWeaponScope;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, MyTuple<bool, bool>> _isInRange;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<int, bool>> _monitorEvents;
        private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<int, bool>> _unmonitorEvents;

        // Descriptions made by Aristeas, with Sigmund Froid's https://steamcommunity.com/sharedfiles/filedetails/?id=2178802013 as a reference.
        // PR accepted after prolific begging by Aryx
        
        /// <summary>
        /// Activates the WcPbAPI using <see cref="IMyTerminalBlock"/> <paramref name="pbBlock"/>.
        /// </summary>
        /// <remarks>
        /// Recommended to use 'Me' in <paramref name="pbBlock"/> for simplicity.
        /// </remarks>
        /// <param name="pbBlock"></param>
        /// <returns><see cref="true"/>  if all methods assigned correctly, <see cref="false"/>  otherwise</returns>
        /// <exception cref="Exception">Throws exception if WeaponCore is not present</exception>
        public bool Activate(Sandbox.ModAPI.Ingame.IMyTerminalBlock pbBlock)
        {
            var dict = pbBlock.GetProperty("WcPbAPI")?.As<IReadOnlyDictionary<string, Delegate>>().GetValue(pbBlock);
            if (dict == null) throw new Exception("WcPbAPI failed to activate");
            return ApiAssign(dict);
        }

        /// <summary>
        /// Bulk calls <see cref="AssignMethod" /> for all WcPbAPI methods.
        /// </summary>
        /// <remarks>
        /// Not useful for most scripts, but is public nonetheless.
        /// </remarks>
        /// <param name="delegates"></param>
        /// <returns><see cref="true"/>  if all methods assigned correctly, <see cref="false"/>  otherwise</returns>
        public bool ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            if (delegates == null)
                return false;

            AssignMethod(delegates, "GetCoreWeapons", ref _getCoreWeapons);
            AssignMethod(delegates, "GetCoreStaticLaunchers", ref _getCoreStaticLaunchers);
            AssignMethod(delegates, "GetCoreTurrets", ref _getCoreTurrets);
            AssignMethod(delegates, "GetBlockWeaponMap", ref _getBlockWeaponMap);
            AssignMethod(delegates, "GetProjectilesLockedOn", ref _getProjectilesLockedOn);
            AssignMethod(delegates, "GetSortedThreats", ref _getSortedThreats);
            AssignMethod(delegates, "GetObstructions", ref _getObstructions);
            AssignMethod(delegates, "GetAiFocus", ref _getAiFocus);
            AssignMethod(delegates, "SetAiFocus", ref _setAiFocus);
            AssignMethod(delegates, "ReleaseAiFocus", ref _releaseAiFocus);
            AssignMethod(delegates, "GetWeaponTarget", ref _getWeaponTarget);
            AssignMethod(delegates, "SetWeaponTarget", ref _setWeaponTarget);
            AssignMethod(delegates, "FireWeaponOnce", ref _fireWeaponOnce);
            AssignMethod(delegates, "ToggleWeaponFire", ref _toggleWeaponFire);
            AssignMethod(delegates, "IsWeaponReadyToFire", ref _isWeaponReadyToFire);
            AssignMethod(delegates, "GetMaxWeaponRange", ref _getMaxWeaponRange);
            AssignMethod(delegates, "GetTurretTargetTypes", ref _getTurretTargetTypes);
            AssignMethod(delegates, "SetTurretTargetTypes", ref _setTurretTargetTypes);
            AssignMethod(delegates, "SetBlockTrackingRange", ref _setBlockTrackingRange);
            AssignMethod(delegates, "IsTargetAligned", ref _isTargetAligned);
            AssignMethod(delegates, "IsTargetAlignedExtended", ref _isTargetAlignedExtended);
            AssignMethod(delegates, "CanShootTarget", ref _canShootTarget);
            AssignMethod(delegates, "GetPredictedTargetPosition", ref _getPredictedTargetPos);
            AssignMethod(delegates, "GetHeatLevel", ref _getHeatLevel);
            AssignMethod(delegates, "GetCurrentPower", ref _currentPowerConsumption);
            AssignMethod(delegates, "GetMaxPower", ref _getMaxPower);
            AssignMethod(delegates, "HasGridAi", ref _hasGridAi);
            AssignMethod(delegates, "HasCoreWeapon", ref _hasCoreWeapon);
            AssignMethod(delegates, "GetOptimalDps", ref _getOptimalDps);
            AssignMethod(delegates, "GetActiveAmmo", ref _getActiveAmmo);
            AssignMethod(delegates, "SetActiveAmmo", ref _setActiveAmmo);
            AssignMethod(delegates, "MonitorProjectile", ref _monitorProjectile);
            AssignMethod(delegates, "UnMonitorProjectile", ref _unMonitorProjectile);
            AssignMethod(delegates, "GetProjectileState", ref _getProjectileState);
            AssignMethod(delegates, "GetConstructEffectiveDps", ref _getConstructEffectiveDps);
            AssignMethod(delegates, "GetPlayerController", ref _getPlayerController);
            AssignMethod(delegates, "GetWeaponAzimuthMatrix", ref _getWeaponAzimuthMatrix);
            AssignMethod(delegates, "GetWeaponElevationMatrix", ref _getWeaponElevationMatrix);
            AssignMethod(delegates, "IsTargetValid", ref _isTargetValid);
            AssignMethod(delegates, "GetWeaponScope", ref _getWeaponScope);
            AssignMethod(delegates, "IsInRange", ref _isInRange);
            AssignMethod(delegates, "RegisterEventMonitor", ref _monitorEvents);
            AssignMethod(delegates, "UnRegisterEventMonitor", ref _unmonitorEvents);
            return true;
        }

        /// <summary>
        /// Links method <paramref name="field"/> to internal API method of name <paramref name="name"/>
        /// </summary>
        /// <remarks>
        /// Not useful for most scripts, but is public nonetheless.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegates"></param>
        /// <param name="name"></param>
        /// <param name="field"></param>
        /// <exception cref="Exception"></exception>
        private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;
            if (field == null)
                throw new Exception(
                    $"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }

        /// <summary>
        /// Populates <paramref name="collection"/> with <see cref="MyDefinitionId"/> of all loaded WeaponCore weapons.
        /// </summary>
        /// <param name="collection"></param>
        /// <seealso cref="GetAllCoreStaticLaunchers"/>
        /// <seealso cref="GetAllCoreTurrets"/>
        public void GetAllCoreWeapons(ICollection<MyDefinitionId> collection) => _getCoreWeapons?.Invoke(collection);

        /// <summary>
        /// Populates <paramref name="collection"/> with <see cref="MyDefinitionId"/> of all loaded WeaponCore fixed weapons.
        /// </summary>
        /// <param name="collection"></param>
        /// <seealso cref="GetAllCoreWeapons"/>
        /// <seealso cref="GetAllCoreTurrets"/>
        public void GetAllCoreStaticLaunchers(ICollection<MyDefinitionId> collection) =>
            _getCoreStaticLaunchers?.Invoke(collection);

        /// <summary>
        /// Populates <paramref name="collection"/> with <see cref="MyDefinitionId"/> of all loaded WeaponCore turret weapons.
        /// </summary>
        /// <param name="collection"></param>
        /// <seealso cref="GetAllCoreWeapons"/>
        /// <seealso cref="GetAllCoreStaticLaunchers"/>
        public void GetAllCoreTurrets(ICollection<MyDefinitionId> collection) => _getCoreTurrets?.Invoke(collection);

        /// <summary>
        /// Populates <paramref name="collection"/> with <see cref="IDictionary{String, Int32}"/> of contents:
        /// <list type="bullet">
        /// <item>Key: Name of weapon.</item>
        /// <item>Value: ID of weapon within <paramref name="weaponBlock"/>.</item>
        /// </list>
        /// </summary>
        /// <param name="weaponBlock"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public bool GetBlockWeaponMap(Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponBlock, IDictionary<string, int> collection) =>
            _getBlockWeaponMap?.Invoke(weaponBlock, collection) ?? false;

        /// <summary>
        /// Returns a <see cref="MyTuple{bool, int, int}"/> containing information about projectiles targeting <paramref name="victim"/>.
        /// </summary>
        /// <param name="victim"></param>
        /// <returns>
        /// <see cref="MyTuple{bool, int, int}"/> with contents:
        /// <list type="number">
        /// <item><see cref="bool"/> Is being locked?</item>
        /// <item><see cref="int"/> Number of locked projectiles.</item>
        /// <item><see cref="int"/> Time (in ticks) locked.</item>
        /// </list>
        /// </returns>
        public MyTuple<bool, int, int> GetProjectilesLockedOn(long victim) =>
            _getProjectilesLockedOn?.Invoke(victim) ?? new MyTuple<bool, int, int>();

        /// <summary>
        /// Populates <paramref name="collection"/> with contents:
        /// <list type="bullet">
        /// <item>Key: Hostile <see cref="MyDetectedEntityInfo"/> within targeting range of <paramref name="pBlock"/>'s grid</item>
        /// <item>Value: Threat level of Key</item>
        /// </list>
        /// </summary>
        /// <param name="pBlock"></param>
        /// <param name="collection"></param>
        public void GetSortedThreats(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, IDictionary<MyDetectedEntityInfo, float> collection) =>
            _getSortedThreats?.Invoke(pBlock, collection);

        /// <summary>
        /// Populates <paramref name="collection"/> with contents:
        /// <list type="bullet">
        /// <item>Friendly <see cref="MyDetectedEntityInfo"/> within targeting range of <paramref name="pBlock"/>'s <see cref="IMyCubeGrid"/></item>
        /// </list>
        /// </summary>
        /// <param name="pBlock"></param>
        /// <param name="collection"></param>
        public void GetObstructions(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> collection) =>
            _getObstructions?.Invoke(pBlock, collection);

        /// <summary>
        /// Returns the GridAi Target with priority <paramref name="priority"/> of <see cref="IMyCubeGrid"/> with EntityID <paramref name="shooter"/>.
        /// </summary>
        /// <remarks>
        /// If the grid is valid but does not have a target, an empty <see cref="MyDetectedEntityInfo"/> is returned.
        /// <para>
        /// Default <paramref name="priority"/> = 0 returns the player-selected target.
        /// </para>
        /// </remarks>
        /// <param name="shooter"></param>
        /// <param name="priority"></param>
        /// <returns>Nullable <see cref="MyDetectedEntityInfo"/>. Null if <paramref name="shooter"/> does not exist or lacks GridAi.</returns>
        public MyDetectedEntityInfo? GetAiFocus(long shooter, int priority = 0) => _getAiFocus?.Invoke(shooter, priority);

        /// <summary>
        /// Sets the GridAi Target of <paramref name="pBlock"/>'s <see cref="IMyCubeGrid"/> to EntityID <paramref name="target"/>.
        /// </summary>
        /// <remarks>
        /// Default <paramref name="priority"/> = 0 sets the player-visible target.
        /// </remarks>
        /// <param name="pBlock"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        /// <returns><see cref="true"/>  if successful, <see cref="false"/>  otherwise.</returns>
        public bool SetAiFocus(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, long target, int priority = 0) =>
            _setAiFocus?.Invoke(pBlock, target, priority) ?? false;

        /// <summary>
        /// Unsets the GridAi Target of <paramref name="pBlock"/>'s <see cref="IMyCubeGrid"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="playerId"/> may be set to 0.
        /// </remarks>
        /// <param name="pBlock"></param>
        /// <param name="playerId"></param>
        /// <returns><see cref="true"/>  if successful, <see cref="false"/>  otherwise.</returns>
        public bool ReleaseAiFocus(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, long playerId) =>
            _releaseAiFocus?.Invoke(pBlock, playerId) ?? false;

        /// <summary>
        /// Returns the WeaponAi target of <paramref name="weaponId"/> on <paramref name="weapon"/>.
        /// </summary>
        /// <remarks>
        /// Seems to always return null for static weapons.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <returns>Nullable <see cref="MyDetectedEntityInfo"/>.</returns>
        public MyDetectedEntityInfo? GetWeaponTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId = 0) =>
            _getWeaponTarget?.Invoke(weapon, weaponId);

        /// <summary>
        /// Sets the WeaponAi target of <paramref name="weaponId"/> on <paramref name="weapon"/> to EntityID <paramref name="target"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="target"></param>
        /// <param name="weaponId"></param>
        public void SetWeaponTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long target, int weaponId = 0) =>
            _setWeaponTarget?.Invoke(weapon, target, weaponId);

        /// <summary>
        /// (DEPRECATED, use ToggleWeaponFire) Fires <paramref name="weaponId"/> on <paramref name="weapon"/> once.
        /// </summary>
        /// <remarks>
        /// <paramref name="allWeapons"/> uses all weapons on <paramref name="weapon"/>.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="allWeapons"></param>
        /// <param name="weaponId"></param>
        public void FireWeaponOnce(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, bool allWeapons = true, int weaponId = 0) =>
            _fireWeaponOnce?.Invoke(weapon, allWeapons, weaponId);

        /// <summary>
        /// Sets the Shoot On/Off toggle of <paramref name="weaponId"/> on <paramref name="weapon"/> to <see cref="bool"/> <paramref name="on"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="allWeapons"/> uses all weapons on <paramref name="weapon"/>.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="on"></param>
        /// <param name="allWeapons"></param>
        /// <param name="weaponId"></param>
        public void ToggleWeaponFire(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, bool on, bool allWeapons, int weaponId = 0) =>
            _toggleWeaponFire?.Invoke(weapon, on, allWeapons, weaponId);

        /// <summary>
        /// Returns whether or not <paramref name="weaponId"/> on <paramref name="weapon"/> is ready to fire.
        /// </summary>
        /// <remarks>
        /// <paramref name="anyWeaponReady"/> uses all weapons on <paramref name="weapon"/>.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <param name="anyWeaponReady"></param>
        /// <param name="shootReady"></param>
        /// <returns><see cref="true"/> if ready to fire, <see cref="false"/> otherwise.</returns>
        public bool IsWeaponReadyToFire(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId = 0, bool anyWeaponReady = true,
            bool shootReady = false) =>
            _isWeaponReadyToFire?.Invoke(weapon, weaponId, anyWeaponReady, shootReady) ?? false;

        /// <summary>
        /// Returns the current Aiming Radius of <paramref name="weaponId"/> on <paramref name="weapon"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <returns><see cref="float"/> range in meters.</returns>
        public float GetMaxWeaponRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
            _getMaxWeaponRange?.Invoke(weapon, weaponId) ?? 0f;

        /// <summary>
        /// Populates <paramref name="collection"/> with contents:
        /// <list type="bullet">
        /// <item><see cref="string"/> Allowed target type name for <paramref name="weaponId"/> on <paramref name="weapon"/>.</item>
        /// </list>
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="collection"></param>
        /// <param name="weaponId"></param>
        /// <returns>true if <paramref name="weaponId"/> and <paramref name="weapon"/> are valid, false otherwise.</returns>
        public bool GetTurretTargetTypes(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, IList<string> collection, int weaponId = 0) =>
            _getTurretTargetTypes?.Invoke(weapon, collection, weaponId) ?? false;

        /// <summary>
        /// Sets allowed target types for <paramref name="weaponId"/> on <paramref name="weapon"/> to <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// Invalid target types are ignored.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="collection"></param>
        /// <param name="weaponId"></param>
        public void SetTurretTargetTypes(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, IList<string> collection, int weaponId = 0) =>
            _setTurretTargetTypes?.Invoke(weapon, collection, weaponId);

        /// <summary>
        /// Sets the current Aiming Range of <paramref name="weapon"/> to <paramref name="range"/>.
        /// </summary>
        /// <remarks>
        /// Values over the maximum possible Aiming Range of <paramref name="weapon"/> will set it to the maximum possible.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="range"></param>
        public void SetBlockTrackingRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, float range) =>
            _setBlockTrackingRange?.Invoke(weapon, range);

        /// <summary>
        /// Returns whether or not <paramref name="weaponId"/> on <paramref name="weapon"/> is aligned with EntityID <paramref name="targetEnt"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="targetEnt"></param>
        /// <param name="weaponId"></param>
        /// <returns>true if aligned and valid, false otherwise.</returns>
        public bool IsTargetAligned(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
            _isTargetAligned?.Invoke(weapon, targetEnt, weaponId) ?? false;

        /// <summary>
        /// Returns whether or not <paramref name="weaponId"/> on <paramref name="weapon"/> is aligned with EntityID <paramref name="targetEnt"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="targetEnt"></param>
        /// <param name="weaponId"></param>
        /// <returns>
        /// <see cref="MyTuple{bool, Vector3D}"/> with contents:
        /// <list type="number">
        /// <item><see cref="bool"/> Is aligned? False if invalid.</item>
        /// <item><see cref="Vector3D"/> Position of target. Null if invalid.</item>
        /// </list>
        /// </returns>
        public MyTuple<bool, Vector3D?> IsTargetAlignedExtended(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
            _isTargetAlignedExtended?.Invoke(weapon, targetEnt, weaponId) ?? new MyTuple<bool, Vector3D?>();

        /// <summary>
        /// Returns whether or not <paramref name="weaponId"/> on <paramref name="weapon"/> is aligned with EntityID <paramref name="targetEnt"/>.
        /// </summary>
        /// <remarks>
        /// Like <see cref="IsTargetAligned"/>, but takes target velocity and acceleration into account.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="targetEnt"></param>
        /// <param name="weaponId"></param>
        /// <returns>true if aligned and valid, false otherwise</returns>
        public bool CanShootTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
            _canShootTarget?.Invoke(weapon, targetEnt, weaponId) ?? false;

        /// <summary>
        /// Returns the lead position of <paramref name="weaponId"/> on <paramref name="weapon"/>, with target EntityId <paramref name="targetEnt"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="targetEnt"></param>
        /// <param name="weaponId"></param>
        /// <returns>Nullable <see cref="Vector3D"/> target lead position. Null if target or weapon invalid.</returns>
        public Vector3D? GetPredictedTargetPosition(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
            _getPredictedTargetPos?.Invoke(weapon, targetEnt, weaponId) ?? null;

        /// <summary>
        /// Returns the heat level of <paramref name="weapon"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="weapon"/> is invalid or does not have heat, returns 0f. Heat may exceed 1.0f in case of overheat.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <returns><see cref="float"/> heat as percentage between 0.0f and 1.0f</returns>
        public float GetHeatLevel(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _getHeatLevel?.Invoke(weapon) ?? 0f;

        /// <summary>
        /// Returns current power consumption of <paramref name="weapon"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns><see cref="float"/> Power in MW</returns>
        public float GetCurrentPower(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _currentPowerConsumption?.Invoke(weapon) ?? 0f;

        /// <summary>
        /// Returns maximum power consumption of <paramref name="weapon"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns><see cref="float"/> Power in MW</returns>
        public float GetMaxPower(MyDefinitionId weaponDef) => _getMaxPower?.Invoke(weaponDef) ?? 0f;

        /// <summary>
        /// Returns whether or not EntityId <paramref name="entity"/> has a GridAi.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true if GridAi present, false otherwise.</returns>
        public bool HasGridAi(long entity) => _hasGridAi?.Invoke(entity) ?? false;

        /// <summary>
        /// Returns whether or not <see cref="IMyTerminalBlock"/> <paramref name="weapon"/> has a WeaponCore weapon.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true if weapon present, false otherwise.</returns>
        public bool HasCoreWeapon(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _hasCoreWeapon?.Invoke(weapon) ?? false;

        /// <summary>
        /// Returns the total optimal DPS of <see cref="IMyCubeGrid"/> with EntityId <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns><see cref="float"/> DPS.</returns>
        public float GetOptimalDps(long entity) => _getOptimalDps?.Invoke(entity) ?? 0f;

        /// <summary>
        /// Returns the active ammo name of <paramref name="weaponId"/> on <paramref name="weapon"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <returns><see cref="string"/> AmmoName</returns>
        public string GetActiveAmmo(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
            _getActiveAmmo?.Invoke(weapon, weaponId) ?? null;

        /// <summary>
        /// Sets the active ammo name of <paramref name="weaponId"/> on <paramref name="weapon"/> to <see cref="string"/> <paramref name="ammoType"/>.
        /// </summary>
        /// <remarks>
        /// Does nothing if <paramref name="ammoType"/> is invalid.
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        public void SetActiveAmmo(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, string ammoType) =>
            _setActiveAmmo?.Invoke(weapon, weaponId, ammoType);

        /// <summary>
        /// Assigns projectile callback <paramref name="action"/> to <paramref name="weaponId"/> on <paramref name="weapon"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="action"/> has parameters:
        /// <list type="number">
        /// <item><see cref="long"/> Parent weapon EntityId?</item>
        /// <item><see cref="int"/> Parent weapon partId</item>
        /// <item><see cref="ulong"/> Projectile EntityId</item>
        /// <item><see cref="long"/> Target EntityId</item>
        /// <item><see cref="Vector3D"/> ProjectilePosition if active, LastHit if destroyed</item>
        /// <item><see cref="bool"/> ProjectileExists?</item>
        /// </list>
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <param name="action"></param>
        public void MonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, Action<long, int, ulong, long, Vector3D, bool> action) =>
            _monitorProjectile?.Invoke(weapon, weaponId, action);

        /// <summary>
        /// Unassigns projectile callback <paramref name="action"/> to <paramref name="weaponId"/> on <paramref name="weapon"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="action"/> has parameters:
        /// <list type="number">
        /// <item><see cref="long"/> Parent weapon EntityId?</item>
        /// <item><see cref="int"/> Parent weapon partId</item>
        /// <item><see cref="ulong"/> Projectile EntityId</item>
        /// <item><see cref="long"/> Target EntityId</item>
        /// <item><see cref="Vector3D"/> ProjectilePosition if active, LastHit if destroyed</item>
        /// <item><see cref="bool"/> ProjectileExists?</item>
        /// </list>
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <param name="action"></param>
        public void UnMonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, Action<long, int, ulong, long, Vector3D, bool> action) =>
            _unMonitorProjectile?.Invoke(weapon, weaponId, action);

        /// <summary>
        /// Returns ProjectileState of <paramref name="projectileId"/>.
        /// </summary>
        /// <param name="projectileId"></param>
        /// <returns>
        /// <see cref="MyTuple{Vector3D, Vector3D, float, float, long, string}"/> with contents:
        /// <list type="number">
        /// <item><see cref="Vector3D"/> Position</item>
        /// <item><see cref="Vector3D"/> Velocity</item>
        /// <item><see cref="float"/> BaseDamagePool</item>
        /// <item><see cref="float"/> BaseHealthPool</item>
        /// <item><see cref="long"/> Target EntityId</item>
        /// <item><see cref="string"/> AmmoRound Name</item>
        /// </list>
        /// </returns>
        public MyTuple<Vector3D, Vector3D, float, float, long, string> GetProjectileState(ulong projectileId) =>
            _getProjectileState?.Invoke(projectileId) ?? new MyTuple<Vector3D, Vector3D, float, float, long, string>();

        /// <summary>
        /// Returns the total effective DPS of <see cref="IMyCubeGrid"/> with EntityId <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns><see cref="float"/> DPS.</returns>
        public float GetConstructEffectiveDps(long entity) => _getConstructEffectiveDps?.Invoke(entity) ?? 0f;

        /// <summary>
        /// Returns the Id of <paramref name="weapon"/>'s controlling player.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns><see cref="long"/> PlayerId. -1 if invalid or uncontrolled.</returns>
        public long GetPlayerController(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _getPlayerController?.Invoke(weapon) ?? -1;

        /// <summary>
        /// Returns the rotation matrix of <paramref name="weaponId"/> on <paramref name="weapon"/>'s azimuth part.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <returns><see cref="Matrix"/> AzimuthMatrix</returns>
        public Matrix GetWeaponAzimuthMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
            _getWeaponAzimuthMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

        /// <summary>
        /// Returns the rotation matrix of <paramref name="weaponId"/> on <paramref name="weapon"/>'s elevation part.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <returns><see cref="Matrix"/> ElevationMatrix</returns>
        public Matrix GetWeaponElevationMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
            _getWeaponElevationMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

        /// <summary>
        /// Returns whether or not <paramref name="targetId"/> is a valid target for <paramref name="weapon"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="onlyThreats"/> will return false if <paramref name="targetId"/>'s threat score is zero.
        /// <para><paramref name="checkRelations"/> will return false if <paramref name="targetId"/> is non-hostile.</para>
        /// </remarks>
        /// <param name="weapon"></param>
        /// <param name="targetId"></param>
        /// <param name="onlyThreats"></param>
        /// <param name="checkRelations"></param>
        /// <returns></returns>
        public bool IsTargetValid(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetId, bool onlyThreats, bool checkRelations) =>
            _isTargetValid?.Invoke(weapon, targetId, onlyThreats, checkRelations) ?? false;

        /// <summary>
        /// Returns scope information of <paramref name="weaponId"/> on <paramref name="weapon"/>.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="weaponId"></param>
        /// <returns>
        /// <see cref="MyTuple{Vector3D, Vector3D}"/> with contents:
        /// <list type="number">
        /// <item><see cref="Vector3D"/> Position</item>
        /// <item><see cref="Vector3D"/> Direction</item>
        /// </list>
        /// </returns>
        public MyTuple<Vector3D, Vector3D> GetWeaponScope(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
            _getWeaponScope?.Invoke(weapon, weaponId) ?? new MyTuple<Vector3D, Vector3D>();

        // terminalBlock, Threat, Other, Something 
        /// <summary>
        /// Returns whether or not <paramref name="block"/>'s <see cref="IMyCubeGrid"/>'s GridAi's PrimaryTarget is in range.
        /// </summary>
        /// <remarks>
        /// The second value in the returned <see cref="MyTuple"/> might be legacy from when players could select two targets.
        /// </remarks>
        /// <param name="block"></param>
        /// <returns>
        /// <see cref="MyTuple{bool, bool}"/> with contents:
        /// <list type="number">
        /// <item><see cref="bool"/> PrimaryTarget In Range?</item>
        /// <item><see cref="bool"/> OtherTarget In Range?</item>
        /// </list>
        /// </returns>
        public MyTuple<bool, bool> IsInRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock block) =>
            _isInRange?.Invoke(block) ?? new MyTuple<bool, bool>();

        /// <summary>
        /// Adds event monitor <paramref name="action"/> to weapon <paramref name="partId"/> on <paramref name="entity"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="action"/> has parameters:
        /// <list type="number">
        /// <item><see cref="int"/> State</item>
        /// <item><see cref="bool"/> Active</item>
        /// </list>
        /// 
        /// <para>
        /// List of event triggers:<br/>
        /// 0  Reloading<br/>
        /// 1  Firing<br/>
        /// 2  Tracking<br/>
        /// 3  Overheated<br/>
        /// 4  TurnOn<br/>
        /// 5  TurnOff<br/>
        /// 6  BurstReload<br/>
        /// 7  NoMagsToLoad<br/>
        /// 8  PreFire<br/>
        /// 9  EmptyOnGameLoad<br/>
        /// 10 StopFiring<br/>
        /// 11 StopTracking<br/>
        /// 12 LockDelay<br/>
        /// 13 Init<br/>
        /// 14 Homing<br/>
        /// 15 TargetAligned<br/>
        /// 16 WhileOn<br/>
        /// 17 TargetRanged100<br/>
        /// 18 TargetRanged75<br/>
        /// 19 TargetRanged50<br/>
        /// 20 TargetRanged25
        /// </para>
        /// </remarks>
        /// <param name="entity"></param>
        /// <param name="partId"></param>
        /// <param name="action"></param>
        public void MonitorEvents(Sandbox.ModAPI.Ingame.IMyTerminalBlock entity, int partId, Action<int, bool> action) =>
            _monitorEvents?.Invoke(entity, partId, action);

        /// <summary>
        /// Removes event monitor <paramref name="action"/> from weapon <paramref name="partId"/> on <paramref name="entity"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="action"/> has parameters:
        /// <list type="number">
        /// <item><see cref="int"/> State</item>
        /// <item><see cref="bool"/> Active</item>
        /// </list>
        /// </remarks>
        /// <param name="entity"></param>
        /// <param name="partId"></param>
        /// <param name="action"></param>
        public void UnMonitorEvents(Sandbox.ModAPI.Ingame.IMyTerminalBlock entity, int partId, Action<int, bool> action) =>
            _unmonitorEvents?.Invoke(entity, partId, action);

    }
}

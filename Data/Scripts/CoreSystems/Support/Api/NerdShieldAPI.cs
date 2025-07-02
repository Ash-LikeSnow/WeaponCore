using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace ShieldAPI
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class NerdShieldAPI : MySessionComponentBase
    {
        public static string ModAPIVersion = "v1";
        public const long ModAPIMessageID = 3514216898;

        public override void LoadData()
        {
            MyAPIUtilities.Static.RegisterMessageHandler(ModAPIMessageID, OnModMessageRecieved);
        }

        /// <summary>
        /// True when API contains the proper delegates, false otherwise.
        /// </summary>
        public static bool IsReady
        {
            get; private set;
        }

        private static Func<IMyCubeGrid, Vector3D, MyStringHash, float, float, float, float> _ShieldDoDamage;
        private static Func<IMyCubeGrid, Vector3D, MyStringHash, float, float, float, float, float> _ShieldDoDamageExplosion;
        private static Func<IMyCubeGrid, bool> _GridHasShields;
        private static Func<IMyCubeGrid, float> _GetCurrentShieldHP;
        private static Func<IMyCubeGrid, float> _GetMaximumShieldHP;
        private static Func<IMyCubeGrid, float> _GetCurrentShieldRegen;
        private static Func<IMyCubeGrid, float> _GetMaximumShieldRegen;
        private static Func<IMyCubeGrid, int> _GetTicksUntilShieldRegen;
        private static Action<IMyCubeGrid, float> _SetShieldHP;
        private static Action<IMyCubeGrid, int> _SetTicksUntilShieldRegen;
        /// <summary>
        /// Deals damage to the shield owned by the target grid, and if spawnParticleLocation != Vector3D.Zero, spawns a shield hit particle at the specified position.
        /// </summary>
        /// <param name="targetgrid">Owner of the shield to damage</param>
        /// <param name="spawnParticleLocation">Position to spawn particle, if any</param>
        /// <param name="damageType">Damage type to damage the shield by</param>
        /// <param name="damageToDo">Damage to do</param>
        /// <param name="shieldDamageMultiplier">Custom provided multiplier to multiply the shield HP lost</param>
        /// <param name="shieldPassthroughMultiplier">Custom provided multiplier for damage passthrough regardless of shield HP</param>
        /// <returns>the unblocked damage.</returns>
        public static float ShieldDoDamage(IMyCubeGrid targetgrid, Vector3D spawnParticleLocation, MyStringHash damageType, float damageToDo, float shieldDamageMultiplier = -1, float shieldPassthroughMultiplier = -1)
        {
            return _ShieldDoDamage?.Invoke(targetgrid, spawnParticleLocation, damageType, damageToDo, shieldDamageMultiplier, shieldPassthroughMultiplier) ?? 0;
        }
        /// <summary>
        /// Deals damage modified by the given radius to the shield owned by the target grid, and if spawnParticleLocation != Vector3D.Zero, spawns a shield hit particle at the specified position.
        /// </summary>
        /// <param name="targetgrid">Owner of the shield to damage</param>
        /// <param name="spawnParticleLocation">Position to spawn particle, if any</param>
        /// <param name="damageType">Damage type to damage the shield by</param>
        /// <param name="damageToDo">Damage to do</param>
        /// <param name="radius">Explosion radius</param>
        /// <param name="shieldDamageMultiplier">Custom provided multiplier to multiply the shield HP lost</param>
        /// <param name="shieldPassthroughMultiplier">Custom provided multiplier for damage passthrough regardless of shield HP</param>
        /// <returns>the unblocked damage.</returns>
        public static float ShieldDoDamageExplosion(IMyCubeGrid targetgrid, Vector3D spawnParticleLocation, MyStringHash damageType, float damageToDo, float radius, float shieldDamageMultiplier = -1, float shieldPassthroughMultiplier = -1)
        {
            return _ShieldDoDamageExplosion?.Invoke(targetgrid, spawnParticleLocation, damageType, damageToDo, radius, shieldDamageMultiplier, shieldPassthroughMultiplier) ?? 0;
        }
        /// <summary>
        /// Check to see if the given grid has shields.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <returns>Whether the grid has shields.</returns>
        public static bool GridHasShields(IMyCubeGrid grid)
        {
            return _GridHasShields?.Invoke(grid) ?? false;
        }
        /// <summary>
        /// Gets the current shield HP of the given grid, or 0 if it has none.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <returns>current shield HP of the given grid, or 0 if it has none</returns>
        public static float GetCurrentShieldHP(IMyCubeGrid grid)
        {
            return _GetCurrentShieldHP?.Invoke(grid) ?? 0;
        }
        /// <summary>
        /// Gets the maximum shield HP of the given grid, or 0 if it has none.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <returns>maximum shield HP of the given grid, or 0 if it has none</returns>
        public static float GetMaximumShieldHP(IMyCubeGrid grid)
        {
            return _GetMaximumShieldHP?.Invoke(grid) ?? 0;
        }
        /// <summary>
        /// Gets the current shield HP regen of the given grid, or 0 if it has none.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <returns>current shield HP regen of the given grid, or 0 if it has none</returns>
        public static float GetCurrentShieldRegen(IMyCubeGrid grid)
        {
            return _GetCurrentShieldRegen?.Invoke(grid) ?? 0;
        }
        /// <summary>
        /// Gets the maximum shield HP regen of the given grid, or 0 if it has none.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <returns>maximum shield HP regen of the given grid, or 0 if it has none</returns>
        public static float GetMaximumShieldRegen(IMyCubeGrid grid)
        {
            return _GetMaximumShieldRegen?.Invoke(grid) ?? 0;
        }
        /// <summary>
        /// Gets the ticks until the grid starts regenning of the given grid, or 0 if it has none, or is regenning.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <returns>ticks until the grid starts regenning of the given grid, or 0 if it has none, or is regenninge</returns>
        public static int GetTicksUntilShieldRegen(IMyCubeGrid grid)
        {
            return _GetTicksUntilShieldRegen?.Invoke(grid) ?? 0;
        }
        /// <summary>
        /// Sets the grid's shield HP, if any. There are no checks to make sure the HP is in bounds.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <param name="HP">Value to set to</param>
        public static void SetShieldHP(IMyCubeGrid grid, float HP)
        {
            _SetShieldHP?.Invoke(grid, HP);
        }
        /// <summary>
        /// Sets the ticks until the grid starts regenning. There are no checks to make sure the value is in bounds.
        /// </summary>
        /// <param name="grid">Target grid</param>
        /// <param name="ticks">Value to set to</param>
        public static void SetTicksUntilShieldRegen(IMyCubeGrid grid, int ticks)
        {
            _SetTicksUntilShieldRegen?.Invoke(grid, ticks);
        }
        protected override void UnloadData()
        {
            MyAPIUtilities.Static.UnregisterMessageHandler(ModAPIMessageID, OnModMessageRecieved);
        }

        private static void OnModMessageRecieved(object obj)
        {
            if (IsReady)
            {
                return;
            }

            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
                return;
            try
            {
                ApiAssign(dict);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("NerdShieldAPI connection failed!");
                MyLog.Default.WriteLine(e);
                return;
            }
            MyLog.Default.WriteLine("NerdShieldAPI connection success!");
            IsReady = true;
        }
        // core systems assign method

        private static void ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            // base methods
            AssignMethod(delegates, "ShieldDoDamage", ref _ShieldDoDamage);
            AssignMethod(delegates, "ShieldDoDamageExplosion", ref _ShieldDoDamageExplosion);
            AssignMethod(delegates, "GridHasShields", ref _GridHasShields);
            AssignMethod(delegates, "GetCurrentShieldHP", ref _GetCurrentShieldHP);
            AssignMethod(delegates, "GetMaximumShieldHP", ref _GetMaximumShieldHP);
            AssignMethod(delegates, "GetCurrentShieldRegen", ref _GetCurrentShieldRegen);
            AssignMethod(delegates, "GetMaximumShieldRegen", ref _GetMaximumShieldRegen);
            AssignMethod(delegates, "GetTicksUntilShieldRegen", ref _GetTicksUntilShieldRegen);
            AssignMethod(delegates, "SetShieldHP", ref _SetShieldHP);
            AssignMethod(delegates, "SetTicksUntilShieldRegen", ref _SetTicksUntilShieldRegen);
        }
        // core systems assign method
        protected static void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"NerdShieldAPI ERROR: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;

            if (field == null)
                throw new Exception($"NerdShieldAPI ERROR: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }
        private static void SubscribeToEvent<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, T field) where T : class
        {
            if (delegates == null)
            {
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"NerdShieldAPI ERROR: Couldn't find {name} delegate of type {typeof(T)}");

            if (del as Action<T> == null)
                throw new Exception($"NerdShieldAPI ERROR: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
            (del as Action<T>).Invoke(field);
        }
    }
}

using System.Collections.Generic;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Projectiles;

namespace WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution
{
    internal static class FireDistributionSupport
    {
        public static void InsertionSortByDistance<T>(FireDistributionSystem.ThreatStorage<T> storage) where T : FireDistributionSystem.Threat
        {
            var threats = storage.Threats;
            var count = threats.Count;
            
            for (var i = 1; i < count; i++)
            {
                var key = threats[i];
                var keyDist = key.DistanceToGridCenter;
                var j = i - 1;
        
                while (j >= 0 && threats[j].DistanceToGridCenter > keyDist)
                {
                    threats[j + 1] = threats[j];
                    j--;
                }
                
                threats[j + 1] = key;
            }
        }
        
        public static void LoadWeaponTargets<T>(
            List<FireDistributionSystem.LogicalWeapon> weapons,
            Projectile[] assignments,
            bool[] isWeaponAssigned,
            ref int weaponsRemaining, 
            FireDistributionSystem.ThreatStorage<T> storage,
            bool[] isThreatAssigned
            ) where T : FireDistributionSystem.Threat
        {
            var currentTick = Session.I.Tick;
            var weaponsCount = weapons.Count;
            var threatByProjectile = storage.ThreatsByProjectile;
            
            // Merge the current game state into the representation we have:
            for (var weaponIndex = 0; weaponIndex < weaponsCount; weaponIndex++)
            {
                var weapon = weapons[weaponIndex];
                var weaponTarget = weapon.Ref.Target;
                
                // ReSharper disable once MergeSequentialChecks
                if (weaponTarget != null && weaponTarget.TargetObject != null) // need some help here. What conditions do we impose here to make sure the target is not expired or something?
                {
                    var targetProjectile = weaponTarget.TargetObject as Projectile;

                    if (targetProjectile != null)
                    {
                        if (targetProjectile.State == Projectile.ProjectileState.Alive && currentTick - weapon.Ref.Target.ChangeTick < weapon.Ref.Comp.MasterOverrides.MinLockTime)
                        {
                            // If true, then the weapon must keep the current target locked; we are not allowed to reassign.
                            // We will write it in our data structure:
                            assignments[weaponIndex] = targetProjectile;
                            isWeaponAssigned[weapon.Index] = true;
                            --weaponsRemaining;

                            T correspondingThreat;
                            if (threatByProjectile.TryGetValue(targetProjectile, out correspondingThreat))
                            {
                                isThreatAssigned[correspondingThreat.Index] = true;
                            }
                        }
                        
                        // else, we are free to reassign
                    }
                    else
                    {
                        // It's aiming for something (maybe grids), so we will make sure we skip it in calculations:
                        isWeaponAssigned[weapon.Index] = true;
                        --weaponsRemaining;
                    }
                }
            }
        }

        /// <summary>
        ///     Checks if a weapon is configured and set to use fire distribution, and if it's a valid PDC as well.
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public static bool IsValidWeaponForFireDistribution(Weapon w)
        {
            var system = w.System;
            var comp = w.Comp;

            if (system == null || comp == null)
            {
                return false;
            }

            var mOverrides = comp.MasterOverrides;
    
            if (mOverrides == null || !system.AllowFireDistribution || !mOverrides.EnableFireDistribution) 
            {
                return false;
            }
            
            if (!((system.TrackProjectile || comp.Ai?.ControlComp != null) && mOverrides.Projectiles && !system.FocusOnly))
            {
                return false;
            }

            return comp.IsBlock && 
                   comp.FunctionalBlock != null && 
                   comp.FunctionalBlock.Enabled &&
                   comp.FunctionalBlock.IsFunctional;
        }

        public const int MaxTurnCost = 1000;
        public const int MinMinLockTime = 15;
        public const int MaxMinLockTime = 1200;
    }
}
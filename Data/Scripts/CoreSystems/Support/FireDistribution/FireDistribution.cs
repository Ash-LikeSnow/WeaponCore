using System;
using System.Collections.Generic;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution.Implementation;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable InlineTemporaryVariable

namespace WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution
{
    /// <summary>
    ///     Manages multiple <see cref="FireDistributionSystem"/>s.
    ///     Each system will manage a specific subset of the grid's weapons. The subsets are guaranteed to be disjoint.
    /// </summary>
    internal sealed class FireDistributionManager
    {
        public readonly Ai MasterAi;
        private readonly FireDistributionSystem[] _systems;
        
        public FireDistributionManager(Ai masterAi)
        {
            MasterAi = masterAi;

            _systems = new FireDistributionSystem[]
            {
                //new AdvancedClosestFireDistributionSystem(this),
                //new AdvancedScreeningFireDistributionSystem(this)
                new BasicFireDistributionSystem(this)
            };
        }

        /// <summary>
        ///     Creates an accessor to the system that handles the weapon.
        ///     Returns an invalid accessor if none of the systems handle the weapon.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public FireDistributionSystem.Accessor CreateAccessor(Weapon weapon)
        {
            for (var systemIndex = 0; systemIndex < _systems.Length; systemIndex++)
            {
                var system = _systems[systemIndex];

                if (system.IsWeaponForSystem(weapon))
                {
                    return system.CreateAccessor(weapon);
                }
            }
            
            return new FireDistributionSystem.Accessor();
        }

        /// <summary>
        ///     Executes the active loop on all systems.
        /// </summary>
        public void ActiveLoop()
        {
            for (var systemIndex = 0; systemIndex < _systems.Length; systemIndex++)
            {
                _systems[systemIndex].ActiveLoop();
            }
        }
        
        // Not strictly necessary because we discard the whole manager, it's not reused
        public void CleanUp()
        {
            for (var systemIndex = 0; systemIndex < _systems.Length; systemIndex++)
            {
                _systems[systemIndex].CleanUp();
            }
        }
    }
    
    /// <summary>
    ///     Fire distribution system handling a subset of the PDCs. Implements specific behavior based on how advanced the weapons are.
    /// </summary>
    internal abstract class FireDistributionSystem
    {
        public sealed class LogicalWeapon
        {
            public Weapon Ref;
            
            /// <summary>
            ///     The index in the <see cref="FireDistributionSystem.Weapons"/> list and the <see cref="FireDistributionSystem.IsWeaponAssignedToAnything"/>.
            /// </summary>
            public int Index;
        }

        private int _weaponCompsVersion = -1;
        
        protected readonly FireDistributionManager Manager;
        
        public FireDistributionSystem(FireDistributionManager manager)
        {
            Manager = manager;
        }

        private readonly FastResourceLock _lock = new FastResourceLock();
        protected readonly List<LogicalWeapon> Weapons = new List<LogicalWeapon>();
        protected readonly Dictionary<Weapon, LogicalWeapon> LogicalWeaponByWeapon = new Dictionary<Weapon, LogicalWeapon>();
        protected bool[] IsWeaponAssignedToAnything = Array.Empty<bool>();
        protected Projectile[] Assignments = Array.Empty<Projectile>();
        
        public uint LastUpdateTick { get; private set; } = uint.MaxValue;
        
        #region Weapon List Acquisition

        /// <summary>
        ///     Gets the valid weapons from the weapon comps, to rebuild the weapon list.
        ///     The condition <see cref="IsWeaponForSystem"/> must be true for each returned result.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Weapon> ScanForValidWeapons()
        {
            var weaponComps = Manager.MasterAi.WeaponComps;
            
            for (var componentIndex = 0; componentIndex < weaponComps.Count; componentIndex++)
            {
                var comp = weaponComps[componentIndex];
                
                for (var weaponIndex = 0; weaponIndex < comp.Collection.Count; weaponIndex++)
                {
                    var w = comp.Collection[weaponIndex];
                    
                    if (FireDistributionSupport.IsValidWeaponForFireDistribution(w) && IsWeaponForSystem(w))
                    {
                        yield return w;
                    }
                }
            }
        }

        /// <summary>
        ///     Checks if the existing weapon list is still valid. It may be invalidated when settings change.
        /// </summary>
        /// <returns></returns>
        private bool IsCurrentWeaponListStillValid()
        {
            var weaponComps = Manager.MasterAi.WeaponComps;
            var validCount = 0;

            for (var componentIndex = 0; componentIndex < weaponComps.Count; componentIndex++)
            {
                var comp = weaponComps[componentIndex];
        
                for (var weaponIndex = 0; weaponIndex < comp.Collection.Count; weaponIndex++)
                {
                    var w = comp.Collection[weaponIndex];
            
                    if (FireDistributionSupport.IsValidWeaponForFireDistribution(w) && IsWeaponForSystem(w))
                    {
                        validCount++;
                
                        // If we find a valid weapon that isn't tracked by us, then the terminal settings changed:
                        if (!LogicalWeaponByWeapon.ContainsKey(w))
                        {
                            return false;
                        }
                    }
                }
            }

            return validCount == Weapons.Count;   
        }

        /// <summary>
        ///     Checks if this manager handles the specified weapon.
        ///     All of these results must be self-exclusive across the different systems.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns>True if this system handles the specified weapon. Otherwise, false.</returns>
        public abstract bool IsWeaponForSystem(Weapon weapon);
        
        #endregion

        private readonly HashSet<LogicalWeapon> _aliveWeaponsTemp = new HashSet<LogicalWeapon>();
        
        protected virtual void RebuildWeapons()
        {
            var logicalWeapons = Weapons;
            var logicalWeaponByWeapon = LogicalWeaponByWeapon;
            var aliveWeaponsTemp = _aliveWeaponsTemp;

            foreach (var validWeapon in ScanForValidWeapons())
            {
                LogicalWeapon existingWeapon;
                if (logicalWeaponByWeapon.TryGetValue(validWeapon, out existingWeapon))
                {
                    aliveWeaponsTemp.Add(existingWeapon);
                }
                else
                {
                    var logicalWeapon = new LogicalWeapon
                    {
                        Ref = validWeapon,
                        // This index is strictly invalid if any weapons are removed.
                        // We handle that below.
                        Index = logicalWeapons.Count,
                    };
                    
                    // Create it immediately:
                    logicalWeapons.Add(logicalWeapon);
                    logicalWeaponByWeapon.Add(validWeapon, logicalWeapon);
                    aliveWeaponsTemp.Add(logicalWeapon);
                }
            }

            // We can afford this slow remove here:
            var anyWeaponsRemoved = Weapons.RemoveAll(x =>
            {
                if (aliveWeaponsTemp.Contains(x))
                {
                    return false;
                }
                
                //x.IsClosed = true; only write usage here
                logicalWeaponByWeapon.Remove(x.Ref);
                
                return true;
            }) > 0;
            
            aliveWeaponsTemp.Clear();
            
            if (anyWeaponsRemoved)
            {
                // We just rebuild indices in this case.
                
                for (var weaponIndex = 0; weaponIndex < Weapons.Count; weaponIndex++)
                {
                    Weapons[weaponIndex].Index = weaponIndex;
                }                
            }
            
            if (logicalWeapons.Count != IsWeaponAssignedToAnything.Length)
            {
                IsWeaponAssignedToAnything = new bool[logicalWeapons.Count];
            }

            if (logicalWeapons.Count != Assignments.Length)
            {
                Assignments = new Projectile[logicalWeapons.Count];
            }
        }
        
        /// <summary>
        ///     Runs the algorithm for the first time in a tick, if necessary.
        /// </summary>
        private void SetupTickStart()
        {
            var sessionTick = Session.I.Tick;

            if (LastUpdateTick == sessionTick)
            {
                return;
            }
            
            _lock.AcquireExclusive();

            try
            {
                if (LastUpdateTick == sessionTick)
                {
                    return;
                }

                if (_weaponCompsVersion != Manager.MasterAi.WeaponCompsVersion || !IsCurrentWeaponListStillValid())
                {
                    RebuildWeapons();
                    _weaponCompsVersion = Manager.MasterAi.WeaponCompsVersion;
                }
                        
                ClearAssignmentState();
                
                var grid = Manager.MasterAi.GridEntity;
                var fullProjectileList = Manager.MasterAi.GetProCache(null, true);
                var lockedOnMap = Manager.MasterAi.LiveProjectile;
                
                if (grid != null && Weapons.Count > 0 && fullProjectileList.Count > 0)
                {
                    UpdateDataStructure(fullProjectileList, lockedOnMap, grid);
                    SetupTickStartCore();
                }
                else
                {
                    ClearDataStructure();
                }
                        
                LastUpdateTick = sessionTick;
            }
            finally
            {
                _lock.ReleaseExclusive();
            }
        }

        /// <summary>
        ///     Called at most once per frame when there is a valid engagement ongoing, to update the internal data needed for the planner.
        /// </summary>
        /// <param name="fullProjectileList"></param>
        /// <param name="lockedOn"></param>
        /// <param name="grid"></param>
        protected abstract void UpdateDataStructure(List<Projectile> fullProjectileList, Dictionary<Projectile, bool> lockedOn, MyCubeGrid grid);

        /// <summary>
        ///     Called at most once per frame when there isn't a valid engagement ongoing, to clear any references and other thins.
        /// </summary>
        protected abstract void ClearDataStructure();
        
        /// <summary>
        ///     Runs the algorithm the first time in a tick.
        ///     The weapon settings, target positions and states are considered constant throughout the tick.
        /// </summary>
        protected abstract void SetupTickStartCore();

        /// <summary>
        ///     Clears the assignments and the flags.
        /// </summary>
        protected void ClearAssignmentState()
        {
            Array.Clear(Assignments, 0, Assignments.Length);
            Array.Clear(IsWeaponAssignedToAnything, 0, IsWeaponAssignedToAnything.Length);
        }

        /// <summary>
        ///     Computes the assignments based on the recorded data. Always called after the data structure has been updated.
        ///     It is a good idea to all <see cref="ClearAssignmentState"/> unless you plan to use the previous state.
        /// </summary>
        protected abstract void ComputeAssignments();
        
        /// <summary>
        ///     Creates an accessor that is valid <i>throught the tick</i>. Trying to use it after the current tick will lead to undefined behavior.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns>An invalid accessor, if the system doesn't handle the specified weapon, or a valid accessor otherwise. This also computes the assignments, if not done already.</returns>
        public Accessor CreateAccessor(Weapon weapon)
        {
            SetupTickStart();
            
            _lock.AcquireShared();
            
            LogicalWeapon logicalWeapon;

            try
            {
                if (!LogicalWeaponByWeapon.TryGetValue(weapon, out logicalWeapon))
                {
                    return new Accessor();
                }
            }
            finally
            {
                _lock.ReleaseShared();
            }
            
            return new Accessor(this, logicalWeapon, _lock);
        }
        
        /// <summary>
        ///     Accessor that can be called in parallel to interact with the manager:
        ///         - Fetching assignments for the weapon
        ///         - Marking an assignment as invalid (cannot shoot it), which recomputes new assignments.
        /// </summary>
        public struct Accessor
        {
            public bool IsValid;
            private readonly FireDistributionSystem _system;
            private readonly LogicalWeapon _logicalWeapon;
            private readonly FastResourceLock _lock;
            
            public Accessor(FireDistributionSystem system, LogicalWeapon logicalWeapon, FastResourceLock accessLock)
            {
                IsValid = true;
                _system = system;
                _logicalWeapon = logicalWeapon;
                _lock = accessLock;
            }

            public bool TryGetAssignment(out Projectile assignedProjectile)
            {
                // We assume the reference is atomic, so we don't take a lock.
                // This should be valid under the condition the accessor is being used in the tick it was created in.
                
                assignedProjectile = _system.Assignments[_logicalWeapon.Index];

                return assignedProjectile != null;
            }
            
            public void MarkCannotShootAndRecompute(Projectile projectile)
            {
                _lock.AcquireExclusive();

                try
                {
                    IsValid = _system.MarkCannotShootCore(_logicalWeapon, projectile);
                    _system.ComputeAssignments();
                }
                finally
                {
                    _lock.ReleaseExclusive();
                }
            }
        }

        /// <summary>
        ///     Called by the accessor in a synchronized context to mark the projectile as unreachable by a PDC in the internal data structure.
        ///     The assignments should also be updated.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="projectile"></param>
        /// <returns>True if the accessor is still valid.</returns>
        protected abstract bool MarkCannotShootCore(LogicalWeapon weapon, Projectile projectile);

        /// <summary>
        ///     Periodically computes assignments. If the assignment differs from the PDC's current target, then it is guaranteed the minimum lock time condition is met, and we should attempt reassignment of the PDC.
        /// </summary>
        public void ActiveLoop()
        {
            // Will compute assignments:
            SetupTickStart();
            
            _lock.AcquireShared();

            try
            {
                // Now, we need to see which weapons don't match the calculated assignments.
                var weapons = Weapons;
                var weaponsCount = weapons.Count;
                var currentTick = Session.I.Tick;
                
                for (var weaponIndex = 0; weaponIndex < weaponsCount; weaponIndex++)
                {
                    var logicalWeapon = weapons[weaponIndex];
                    var weaponRef = logicalWeapon.Ref;

                    if (!IsWeaponAssignedToAnything[logicalWeapon.Index]) 
                    {
                        // Skip if we don't know anything about the weapon:
                        continue;
                    }

                    var assignedProjectile = Assignments[weaponIndex];
                    
                    if (assignedProjectile == null)
                    {
                        continue;
                    }
                
                    var weaponTarget = weaponRef.Target;
                    var currentTargetProjectile = weaponTarget?.TargetObject as Projectile;
                    
                    if (currentTargetProjectile != assignedProjectile)
                    {   
                        // We assign it.
                        // The target acquisition will run, and the weapon will get its assignment via AcquireProjectile.
                        weaponRef.FastTargetResetTick = currentTick + 2;
                    }
                }
            }
            finally
            {
                _lock.ReleaseShared();   
            }
        }
        
        public virtual void CleanUp()
        {
            Weapons.Clear();
            LogicalWeaponByWeapon.Clear();
            Array.Clear(Assignments, 0, Assignments.Length);
            ClearDataStructure();
            LastUpdateTick = uint.MaxValue;
        }

        public class Threat
        {
            public Projectile Ref;
            public int Index;
            public double DistanceToGridCenter; // Actual distance, not squared!
            public bool IsLockedOn;
        }
        
        /// <summary>
        ///     Data structure for storing all internal state needed by the assignment algorithm.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class ThreatStorage<T> where T : Threat
        {
            public readonly List<T> Threats = new List<T>();
            public readonly Dictionary<Projectile, T> ThreatsByProjectile = new Dictionary<Projectile, T>();
            private readonly HashSet<Projectile> _aliveProjectilesTemp = new HashSet<Projectile>();
            private readonly List<Projectile> _newProjectilesTemp = new List<Projectile>();

            public bool[] IsThreatLockedOn = Array.Empty<bool>();
            public bool[] SupportivePd = Array.Empty<bool>();
            
            protected abstract T CreateInstance(Projectile projectile, int weaponCount);
            
            private void DestroyDeadProjectiles()
            {
                // Inline List.RemoveAll:
                
                var list = Threats;
                var alive = _aliveProjectilesTemp;
                var freeIndex = 0;

                while (freeIndex < list.Count && alive.Contains(list[freeIndex].Ref))
                {
                    freeIndex++;
                }

                if (freeIndex >= list.Count)
                {
                    return;
                }

                var a = list[freeIndex];
                ThreatsByProjectile.Remove(a.Ref);

                for (var readIndex = freeIndex + 1; readIndex < list.Count; readIndex++)
                {
                    var currentItem = list[readIndex];

                    if (alive.Contains(currentItem.Ref))
                    {
                        list[freeIndex] = currentItem;
                        freeIndex++;
                    }
                    else
                    {
                       ThreatsByProjectile.Remove(currentItem.Ref);
                    }
                }
    
                var itemsToRemove = list.Count - freeIndex;
               
                // ReSharper disable once InvertIf
                if (itemsToRemove > 0)
                {
                    list.RemoveRange(freeIndex, itemsToRemove);
                }
            }

            private void LoadProjectiles(List<Projectile> projectiles, Dictionary<Projectile, bool> lockedOn, MyCubeGrid grid, int weaponCount)
            {
                var threats = Threats;
                var threatsByProjectile = ThreatsByProjectile;
                var aliveProjectilesTemp = _aliveProjectilesTemp;
                var newProjectilesTemp = _newProjectilesTemp;
                
                // Find out which projectiles are still valid, and which ones are new:
                for (var projectileIndex = 0; projectileIndex < projectiles.Count; projectileIndex++)
                {
                    var validProjectile = projectiles[projectileIndex];

                    if (validProjectile.State == Projectile.ProjectileState.Dead)
                    {
                        // What the fuck? The projectile list has stale projectiles!
                        continue;
                    }

                    T existingThreat;
                    if (threatsByProjectile.TryGetValue(validProjectile, out existingThreat))
                    {
                        aliveProjectilesTemp.Add(validProjectile);
                    }
                    else
                    {
                        newProjectilesTemp.Add(validProjectile);
                    }
                }
                
                DestroyDeadProjectiles();
                aliveProjectilesTemp.Clear();

                for (var newProjectileIndex = 0; newProjectileIndex < newProjectilesTemp.Count; newProjectileIndex++)
                {
                    var projectile = newProjectilesTemp[newProjectileIndex];
                    var threat = CreateInstance(projectile, weaponCount);
                    
                    threats.Add(threat);
                    threatsByProjectile.Add(projectile, threat);
                }
                
                newProjectilesTemp.Clear();
                
                // Recompute distances and indices for every threat, and also see if they are locked onto the grid:
                var gridCenter = grid.PositionComp.WorldAABB.Center;
                for (var threatIndex = 0; threatIndex < threats.Count; threatIndex++)
                {
                    var threat = threats[threatIndex];
                    
                    threat.Index = threatIndex;
                    threat.DistanceToGridCenter = Vector3D.Distance(threat.Ref.Position, gridCenter);

                    bool isLockedOn;
                    if (!lockedOn.TryGetValue(threat.Ref, out isLockedOn))
                    {
                        isLockedOn = true;
                    }

                    threat.IsLockedOn = isLockedOn;
                }
            }
            
            public void LoadSupport(List<LogicalWeapon> weapons)
            {
                var threats = Threats;
                var threatsCount = threats.Count;
                if (IsThreatLockedOn.Length < threatsCount)
                {
                    IsThreatLockedOn = new bool[threatsCount];
                }

                var isThreatLockedOn = IsThreatLockedOn;
                for (var index = 0; index < threatsCount; index++)
                {
                    isThreatLockedOn[index] = threats[index].IsLockedOn;
                }
                
                var weaponsCount = weapons.Count;
                if (SupportivePd.Length != weaponsCount)
                {
                    SupportivePd = new bool[weaponsCount];
                }
                
                var supportivePd = SupportivePd;
                for (var weaponIndex = 0; weaponIndex < weaponsCount; weaponIndex++)
                {
                    supportivePd[weaponIndex] = weapons[weaponIndex].Ref?.Comp?.MasterOverrides?.SupportingPD ?? true;
                }
            }

            public void CopyIndices()
            {
                var threats = Threats;

                for (var threatIndex = 0; threatIndex < threats.Count; threatIndex++)
                {
                    threats[threatIndex].Index = threatIndex;
                }
            }

            /// <summary>
            ///     Updates the data structure using the fresh projectile information.
            /// </summary>
            /// <param name="projectileList"></param>
            /// <param name="lockedOn"></param>
            /// <param name="grid"></param>
            /// <param name="weapons"></param>
            public virtual void UpdateDataStructure(List<Projectile> projectileList, Dictionary<Projectile, bool> lockedOn, MyCubeGrid grid, List<LogicalWeapon> weapons)
            {
                /*
                 * Loads new projectiles and removes stale projectiles. Preserves order of persisted projectiles.
                 * Also updates the distances for all projectiles.
                 */
                LoadProjectiles(projectileList, lockedOn, grid, weapons.Count);
            }
            
            public virtual void Clear()
            {
                Threats.Clear();
                ThreatsByProjectile.Clear();
            }
        }
    }
}
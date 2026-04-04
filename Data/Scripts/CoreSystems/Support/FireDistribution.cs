using System;
using System.Collections.Generic;
using System.Diagnostics;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRageMath;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable InlineTemporaryVariable

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    /// <summary>
    ///     Manages multiple <see cref="FireDistributionSystem"/>s.
    ///     Each system will manage a specific subset of the grid's weapons. The subsets are guaranteed to be disjoint.
    /// </summary>
    internal sealed class FireDistributionManager
    {
        // Would be kind of a mess to inline this one, won't lie
        public static bool IsValidPdc(Weapon w)
        {
            var system = w.System;
            var comp = w.Comp;

            if (system == null || comp == null)
            {
                return false;
            }

            var mOverrides = comp.MasterOverrides;
    
            if (mOverrides == null) 
            {
                return false;
            }
            
            if (!((system.TrackProjectile || comp.Ai?.ControlComp != null) && mOverrides.Projectiles && !system.FocusOnly))
            {
                return false;
            }

            if (comp.IsBlock && comp.FunctionalBlock != null)
            {
                return comp.FunctionalBlock.Enabled && comp.FunctionalBlock.IsFunctional; 
            }

            return true;
        }
        
        public readonly Ai MasterAi;
        private readonly FireDistributionSystem[] _systems;
        
        public FireDistributionManager(Ai masterAi)
        {
            MasterAi = masterAi;

            _systems = new FireDistributionSystem[]
            {
                new ClosestTargetingFireDistributionSystem(this),
                new MaximumSpreadTargetingFireDistributionSystem(this)
            };
        }

        /// <summary>
        ///     Creates an acessor to the system that handles the weapon.
        ///     Returns an invalid accessor if none of the systems handle the weapon.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public FireDistributionSystem.Accessor CreateAccessor(Weapon weapon)
        {
            for (var systemIndex = 0; systemIndex < _systems.Length; systemIndex++)
            {
                var system = _systems[systemIndex];

                if (system.IsValidWeapon(weapon))
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
    
    internal abstract class FireDistributionSystem
    {
        public sealed class LogicalWeapon
        {
            public Weapon Ref;
            
            /// <summary>
            ///     Value set by the player. The larger it is, the costlier it is for the weapon to switch target.
            /// </summary>
            public float TurnCostMultiplier;

            /// <summary>
            ///     Value set by the player. The larger it is, the fewer of these weapons are assigned to a torp.
            /// </summary>
            public float WeaponValue;

            /// <summary>
            ///     Value set by the player. This is the minimum duration to wait before assigning a new target.
            ///     It is to prevent the weapon juking without being able to fire.
            /// </summary>
            public int MinimumLockDuration;
            
            // Temporary value used by the local sort. Invalid to access anywhere else!
            public float TempCurrentTurnCost;
            
            /// <summary>
            ///     The index in the <see cref="FireDistributionSystem.Weapons"/> list and the <see cref="FireDistributionSystem.IsWeaponAssignedToAnything"/>.
            /// </summary>
            public int Index;

            /// <summary>
            ///     If true, then the weapon was removed from the manager.
            ///     Used by the <see cref="Network"/> in its rebuild process, to remove the references to destroyed weapons from the repositories.
            /// </summary>
            public bool IsClosed;
            
            public enum RepoRebuildState : byte
            {
                Empty = 0,
                IsMarkedInRepo = 1,
                KeepInRepo = 2
            }
            
            /// <summary>
            ///     Used locally in the rebuild process.
            ///     The data is invalid outside of that.
            /// </summary>
            public RepoRebuildState RepoRebuildStateTemp;
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
        protected readonly Dictionary<Weapon, Projectile> Assignments = new Dictionary<Weapon, Projectile>();
        public readonly ThreatGraph Network = new ThreatGraph();
        
        public uint LastUpdateTick { get; private set; } = uint.MaxValue;
        
        // P.S. depending on how often this runs, we may want to have a dedicated loop that clears those references.

        protected static void Log(string message) => MyAPIGateway.Utilities.ShowMessage("FDS", message);

        // P.S. It doesn't just use a single predicate method because of profiler...
        #region Weapon List Acquisition
        
        /// <summary>
        ///     Gets the valid weapons from the weapon comps, to rebuild the weapon list.
        ///     The condition <see cref="IsValidWeapon"/> must be true for each returned result.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<Weapon> ScanForValidWeapons();

        /// <summary>
        ///     Checks if the existing weapon list is still valid. It may be invalidated when settings change.
        /// </summary>
        /// <returns></returns>
        protected abstract bool IsCurrentWeaponListStillValid();

        /// <summary>
        ///     For now, we use this in <see cref="IsCurrentWeaponListStillValid"/>.
        ///     I doubt we will have performance issues because of the predicate. But if we do, we will inline.
        /// </summary>
        /// <param name="predicate"></param>
        protected bool CheckForWeaponStateChangesOrUnmatchedConditions(Func<Weapon, bool> predicate)
        {
            var weaponComps = Manager.MasterAi.WeaponComps;
            var validCount = 0;

            for (var componentIndex = 0; componentIndex < weaponComps.Count; componentIndex++)
            {
                var comp = weaponComps[componentIndex];
        
                for (var weaponIndex = 0; weaponIndex < comp.Collection.Count; weaponIndex++)
                {
                    var w = comp.Collection[weaponIndex];
            
                    if (FireDistributionManager.IsValidPdc(w) && predicate(w))
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
        public abstract bool IsValidWeapon(Weapon weapon);
        
        #endregion
        
        private static double Measure(Action a)
        {
            var sw = Stopwatch.StartNew();
            a.Invoke();
            return sw.Elapsed.TotalMilliseconds * 1000;
        }

        private readonly HashSet<LogicalWeapon> _aliveWeaponsTemp = new HashSet<LogicalWeapon>();
        
        private bool RebuildWeaponList()
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
                        TurnCostMultiplier = 1.0f,
                        WeaponValue = 1.0f,
                        MinimumLockDuration = 20,
                        // This index is strictly invalid if any weapons are removed.
                        // We handle that below.
                        Index = logicalWeapons.Count,
                        IsClosed = false
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
                
                x.IsClosed = true;
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
            
            if (logicalWeapons.Count > IsWeaponAssignedToAnything.Length)
            {
                IsWeaponAssignedToAnything = new bool[logicalWeapons.Count];
            }
            
            return anyWeaponsRemoved;
        }
        
        private void LoadWeaponSettings()
        {
            // TODO read all the sliders here
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

                var anyWeaponsRemoved = false;    
                if (_weaponCompsVersion != Manager.MasterAi.WeaponCompsVersion || !IsCurrentWeaponListStillValid())
                {
                    anyWeaponsRemoved = RebuildWeaponList();
                    _weaponCompsVersion = Manager.MasterAi.WeaponCompsVersion;
                    
                    MyAPIGateway.Utilities.ShowMessage("FCS", $"Rebuild weapons: {Weapons.Count}, ver {_weaponCompsVersion}");
                }
                        
                LoadWeaponSettings();
                ClearAssignmentState();
                
                var grid = Manager.MasterAi.GridEntity;
                var projectileList = Manager.MasterAi.ProjectileCache;
                
                if (grid != null && Weapons.Count > 0 && projectileList.Count > 0)
                {
                    Network.UpdateDataStructure(projectileList, grid, Weapons, anyWeaponsRemoved);
                    SetupTickStartCore();
                }
                else
                {
                    Network.Clear();
                }
                        
                LastUpdateTick = sessionTick;
            }
            finally
            {
                _lock.ReleaseExclusive();
            }
        }

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
            Assignments.Clear();
            Array.Clear(IsWeaponAssignedToAnything, 0, Weapons.Count);
        }

        /// <summary>
        ///     Called when a weapon finds it cannot shoot the assigned target, after that issue has already been marked.
        ///     Uses a fast greedy (suboptimal) assignment algorithm.
        /// </summary>
        protected virtual void ComputeAssignments()
        {
             ClearAssignmentState();
            
            var weapons = Weapons;
            var weaponsCount = Weapons.Count;
            var threats = Network.Threats;
            var isAssigned = IsWeaponAssignedToAnything;
            var currentTick = Session.I.Tick;

            // Unassigned weapons left. Used to early-exit.
            var weaponsRemaining = weaponsCount;
            
            // Merge the current game state into the representation we have:
            for (var weaponIndex = 0; weaponIndex < weaponsCount; weaponIndex++)
            {
                var weapon = weapons[weaponIndex];
                var weaponTarget = weapon.Ref.Target;
                
                if (weaponTarget != null && weaponTarget.TargetObject != null) // need some help here. What conditions do we impose here to make sure the target is not expired or something?
                {
                    var targetProjectile = weaponTarget.TargetObject as Projectile;

                    if (targetProjectile != null)
                    {
                        if (targetProjectile.State == Projectile.ProjectileState.Alive && currentTick - weapon.Ref.Target.ChangeTick < weapon.MinimumLockDuration)
                        {
                            // If true, then the weapon must keep the current target locked; we are not allowed to reassign.
                            // We will write it in our datastructure:
                            Assignments.Add(weapon.Ref, targetProjectile);
                            isAssigned[weapon.Index] = true;
                            --weaponsRemaining;
                        }
                        
                        // else, we are free to reassign
                    }
                    else
                    {
                        // It's aiming for something (maybe grids), so we will make sure we skip it in calculations:
                        isAssigned[weapon.Index] = true;
                        --weaponsRemaining;
                    }
                }
            }
            
            // Greedily assign whatever we can:
            for (var threatIndex = 0; threatIndex < threats.Count && weaponsRemaining > 0; threatIndex++)
            {
                var threat = threats[threatIndex];

                var remainingThreatValue = 1.0f;
                
                // Apply committed weapons.
                // These are the weapons whose locks we cannot change yet.
                for (var candidateWeaponIndex = 0; candidateWeaponIndex < threat.WeaponCandidates.Count; candidateWeaponIndex++)
                {
                    var candidateWeapon = threat.WeaponCandidates[candidateWeaponIndex];

                    Projectile projectileCommitted;
                    if (isAssigned[candidateWeapon.Index] && Assignments.TryGetValue(candidateWeapon.Ref, out projectileCommitted) && projectileCommitted == threat.Ref)
                    {
                        remainingThreatValue -= candidateWeapon.WeaponValue;
                    }
                }
                
                if (remainingThreatValue <= 0.0f || Math.Abs(remainingThreatValue) < 1e-4)
                {
                    // The torpedo is fully engaged by the committed weapons
                    continue;
                }
                
                for (var candidateWeaponIndex = 0; candidateWeaponIndex < threat.WeaponCandidates.Count; candidateWeaponIndex++)
                {
                    var candidateWeapon = threat.WeaponCandidates[candidateWeaponIndex];

                    if (isAssigned[candidateWeapon.Index])
                    {
                        continue;
                    }

                    remainingThreatValue -= candidateWeapon.WeaponValue;
                    
                    Assignments.Add(candidateWeapon.Ref, threat.Ref);
                    isAssigned[candidateWeapon.Index] = true;
                    weaponsRemaining--;
                    
                    if (remainingThreatValue <= 0.0f || Math.Abs(remainingThreatValue) < 1e-4)
                    {
                        break;
                    }
                }
            }
        }
        
        public Accessor CreateAccessor(Weapon weapon)
        {
            SetupTickStart();
            return new Accessor(this, weapon, _lock);
        }
        
        /// <summary>
        ///     Accessor that can be called in parallel to interact with the manager:
        ///         - Fetching assignments for the weapon
        ///         - Marking an assignment as invalid (cannot shoot it)
        ///         - Recalculating after that
        /// </summary>
        public struct Accessor
        {
            public readonly bool IsValid;
            
            private readonly FireDistributionSystem _system;
            private readonly Weapon _weapon;
            private readonly FastResourceLock _lock;
            public bool IsDirty;
            
            public Accessor(FireDistributionSystem system, Weapon weapon, FastResourceLock accessLock)
            {
                IsValid = true;
                _system = system;
                _weapon = weapon;
                _lock = accessLock;
                IsDirty = false;
            }

            public bool TryGetAssignment(out Projectile assignedProjectile)
            {
                _lock.AcquireShared();

                try
                {
                    return _system.Assignments.TryGetValue(_weapon, out assignedProjectile);
                }
                finally
                {
                    _lock.ReleaseShared();
                }
            }
            
            public void MarkCannotShoot(Projectile projectile)
            {
                _lock.AcquireExclusive();

                try
                {
                    ThreatGraph.Threat threat;
                    if (!_system.Network.ThreatsByProjectile.TryGetValue(projectile, out threat))
                    {
                        return;
                    }
                    
                    var weapon = _weapon;
                    var index = threat.WeaponCandidates.FindIndex(x => x.Ref == weapon);

                    if (index != -1)
                    {
                        threat.WeaponCandidates.RemoveAtFast(index);
                        IsDirty = true;
                    }
                }
                finally
                {
                    _lock.ReleaseExclusive();
                }
            }
            
            public void RecalculateAssignments()
            {
                _lock.AcquireExclusive();
                
                try
                {
                    _system.ComputeAssignments();                   
                }
                finally
                {   
                    _lock.ReleaseExclusive();
                }
            }
        }

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

                    Projectile assignedProjectile;
                    if (!Assignments.TryGetValue(weaponRef, out assignedProjectile))
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
            Assignments.Clear();
            Network.Clear();
            LastUpdateTick = uint.MaxValue;
        }

        public sealed class ThreatGraph
        {
            public sealed class Threat
            {
                public Projectile Ref;
                public List<LogicalWeapon> WeaponCandidates;
                public double DistanceToGridCenter; // Actual distance, not squared!
            }
            
            public readonly List<Threat> Threats = new List<Threat>();
            public Dictionary<Projectile, Threat> ThreatsByProjectile = new Dictionary<Projectile, Threat>();
            private readonly Stack<List<LogicalWeapon>> _pool = new Stack<List<LogicalWeapon>>();

            /**
             * Hold your pitchfork, BD!
             * We will hit INLINE on the IDE once this logic is done.
             */
            
            private List<LogicalWeapon> GetPooledWeaponList() => _pool.Count > 0 
                ? _pool.Pop()
                : new List<LogicalWeapon>();

            private void FreePooledWeaponList(List<LogicalWeapon> list)
            {
                list.Clear();
                _pool.Push(list);
            }

            private readonly HashSet<Projectile> _aliveProjectilesTemp = new HashSet<Projectile>();
            private readonly List<Projectile> _newProjectilesTemp = new List<Projectile>();

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
                FreePooledWeaponList(a.WeaponCandidates);
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
                       FreePooledWeaponList(currentItem.WeaponCandidates);
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
            
            private void LoadProjectiles(List<Projectile> projectiles, MyCubeGrid grid)
            {
                var threats = Threats;
                var threatsByProjectile = ThreatsByProjectile;
                var aliveProjectilesTemp = _aliveProjectilesTemp;
                var newProjectilesTemp = _newProjectilesTemp;
                
                // Find out which projectiles are still valid, and which ones are new:
                for (var projectileIndex = 0; projectileIndex < projectiles.Count; projectileIndex++)
                {
                    var validProjectile = projectiles[projectileIndex];

                    Threat existingThreat;
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
                    
                    var threat = new Threat
                    {
                        Ref = projectile,
                        WeaponCandidates = GetPooledWeaponList(),
                        DistanceToGridCenter = double.NaN
                    };
                    
                    threats.Add(threat);
                    threatsByProjectile.Add(projectile, threat);
                }
                
                newProjectilesTemp.Clear();
                
                // Recompute distances for every threat:
                var gridCenter = grid.PositionComp.WorldAABB.Center;
                for (var threatIndex = 0; threatIndex < threats.Count; threatIndex++)
                {
                    var threat = threats[threatIndex];
                    
                    threat.DistanceToGridCenter = Vector3D.Distance(threat.Ref.Position, gridCenter);
                }
            }

            private static void RemoveDestroyedWeaponsFromRepo(List<LogicalWeapon> repo)
            {
                // Inline List.RemoveAll
                
                var freeIndex = 0;
                
                while (freeIndex < repo.Count && !repo[freeIndex].IsClosed)
                {
                    freeIndex++;
                }

                if (freeIndex >= repo.Count)
                {
                    return;
                }

                for (var readIndex = freeIndex + 1; readIndex < repo.Count; readIndex++)
                {
                    var currentWeapon = repo[readIndex];

                    if (!currentWeapon.IsClosed)
                    {
                        repo[freeIndex] = currentWeapon;
                        freeIndex++;
                    }
                }

                var itemsToRemove = repo.Count - freeIndex;

                // ReSharper disable once InvertIf
                if (itemsToRemove > 0)
                {
                    repo.RemoveRange(freeIndex, itemsToRemove);
                }
            }
            
            public void UpdateDataStructure(List<Projectile> projectileList, MyCubeGrid grid, List<LogicalWeapon> weapons, bool anyWeaponsRemoved)
            {
                /*
                 * - Loads new projectiles with an empty candidate list.
                 * - Removes stale projectiles.
                 * - Projectiles that are valid aren't touched so we can use the preserved sort order.
                 * - Updates the distances for all projectiles.
                 *
                 * Since it's an active loop, it's optimized.
                 */
                var loadProjectiles = Measure(() =>
                {
                    LoadProjectiles(projectileList, grid);
                });

                /*
                 * If any weapons were removed (their IsClosed is set), this will remove them from all repositories.
                 * Since it's infrequent, it's not optimized.
                 */
                var removeWeapons = Measure(() =>
                {
                    if (anyWeaponsRemoved)
                    {
                        for (var index = 0; index < Threats.Count; index++)
                        {
                            RemoveDestroyedWeaponsFromRepo(Threats[index].WeaponCandidates);
                        }
                    }
                });
                    
                /*
                 * Initializes and maintains the weapons by cheap checks, to lower the number of failed acquires.
                 * The maintenance preserves the existing weapons in their original order.
                 * Since this order is expected to be coherent in time, this should massively lower the downstream sorting cost.
                 * Since it's an active loop, it's optimized.
                 */
                var updateRepositoryWeapons = Measure(() =>
                {
                    UpdateRepositoryWeapons(weapons, grid);
                });
                
                /*
                 * This sorts EACH repository by the cost of turning the PDC to the torp.
                 * It's a massive cost, so we have a warm start thanks to the preserved order.
                 *
                 * P.S. This can be very easily dispatched in parallel; however, the overhead might be big (this takes from a few tens to a few hundreds of microseconds in my tests).
                 */
                var sortRepos = Measure(() =>
                {
                    SortRepositoriesByAngleCost();
                });

                if (Session.I.Tick30)
                {
                    MyAPIGateway.Utilities.ShowMessage($"FDS {this}", $"LP: {loadProjectiles:F}, WR: {removeWeapons:F}, URW: {updateRepositoryWeapons:F}, SR: {sortRepos:F}");
                }
            }
            
            private struct RangeBand
            {
                /// <summary>
                ///     The truncated value (since it's a double).
                ///     This allows us to separate the network into discrete ranges. 
                /// </summary>
                public int TruncatedRange;

                /// <summary>
                ///     All weapons that have their range match this floor.
                /// </summary>
                public List<LogicalWeapon> Weapons;
            }

            private readonly List<RangeBand> _rangeBands = new List<RangeBand>();
            private readonly Dictionary<int, RangeBand> _rangeBandsByRange = new Dictionary<int, RangeBand>();

            public void Clear()
            {
                Threats.Clear();
                ThreatsByProjectile.Clear();
                _pool.Clear();
                ClearBands();
            }
            
            public void ClearBands()
            {
                var bands = _rangeBands;

                for (var bandIndex = 0; bandIndex < bands.Count; bandIndex++)
                {
                    FreePooledWeaponList(bands[bandIndex].Weapons);
                }

                bands.Clear();
                _rangeBandsByRange.Clear();
            }

            /// <summary>
            ///     Builds a "range band" data structure.
            ///     Simply put, we quantize the range of each weapon in one-meter increments, then we collect weapons with the same quantized range in discrete (disjoint!) sets.
            ///     This gives us a way to very cheaply discard weapons that are definitely not in range to shoot a given torp.
            /// </summary>
            /// <param name="weapons"></param>
            private void BuildRangeBands(List<LogicalWeapon> weapons)
            {
                ClearBands();
                
                var bands = _rangeBands;
                var bandsByRange = _rangeBandsByRange;

                for (var weaponIndex = 0; weaponIndex < weapons.Count; weaponIndex++)
                {
                    var weapon = weapons[weaponIndex];
                    var range = Math.Max((int)weapon.Ref.MaxTargetDistance, 1);

                    RangeBand band;
                    if (!bandsByRange.TryGetValue(range, out band))
                    {
                        band = new RangeBand
                        {
                            TruncatedRange = range,
                            Weapons = GetPooledWeaponList()
                        };
                        
                        bands.Add(band);
                        bandsByRange.Add(range, band);
                    }
                    
                    band.Weapons.Add(weapon);
                }
                
                // Sort descending. We can afford this since we only have a few bands.
                // We will use this to optimize the repository update algorithm.
                bands.Sort((a, b) => b.TruncatedRange.CompareTo(a.TruncatedRange));
            }
            
            private void UpdateRepositoryWeapons(List<LogicalWeapon> weapons, MyCubeGrid grid)
            {
                // We need to send the MCRN Hypersonic PMW to keen's headquaarters...

                BuildRangeBands(weapons);

                var bands = _rangeBands;
                var gridRadius = grid.PositionComp.WorldVolume.Radius;
                
                // First, filter by range:
                for (var threatIndex = 0; threatIndex < Threats.Count; threatIndex++)
                {
                    var threat = Threats[threatIndex];
                    var distanceToCenter = threat.DistanceToGridCenter;
                    var repo = threat.WeaponCandidates;
                    
                    // This gives us the range in the list that holds the weapons we started with:
                    var initialRepoSize = repo.Count;
                    
                    // First, we mark each existing weapon.
                    // We will use this in the algorithm blow. It will allow us to see if a weapon should be kept in the repo.
                    for (var existingWeaponIndex = 0; existingWeaponIndex < initialRepoSize; existingWeaponIndex++)
                    {
                        repo[existingWeaponIndex].RepoRebuildStateTemp = LogicalWeapon.RepoRebuildState.IsMarkedInRepo;
                    }

                    // Lower and upper bounds on the distance to weapons:
                    var minimumDistance = distanceToCenter - gridRadius;
                    var maximumDistance = distanceToCenter + gridRadius;

                    for (var bandIndex = 0; bandIndex < bands.Count; bandIndex++)
                    {
                        var rangeBand = bands[bandIndex];
                        
                        if (minimumDistance > rangeBand.TruncatedRange)
                        {
                            // Guaranteed to be out of range.
                            // We sorted the bands in descending order, so we can safely prune the rest of the list:
                            break;
                        }

                        // If true, the weapon is guaranteed to be in range. Otherwise, we need to do a distance test.
                        var skipDistanceCheck = maximumDistance <= rangeBand.TruncatedRange;

                        for (var index = 0; index < rangeBand.Weapons.Count; index++)
                        {
                            var weapon = rangeBand.Weapons[index];

                            var weaponRef = weapon.Ref;
                            var targetPosition = threat.Ref.Position;

                            if (!skipDistanceCheck && Vector3D.DistanceSquared(weaponRef.GetScope.Info.Position, targetPosition) > weapon.Ref.MaxTargetDistanceSqr)
                            {
                                continue;
                            }
                            
                            // Range-of-motion check: seems a bit involved. TODO

                            if (weapon.RepoRebuildStateTemp == LogicalWeapon.RepoRebuildState.IsMarkedInRepo)
                            {
                                // The weapon already exists in the repo.
                                // We will now mark it as seen (i.e. it passes the checks to be kept in the repo).
                                weapon.RepoRebuildStateTemp = LogicalWeapon.RepoRebuildState.KeepInRepo;
                            }   
                            else
                            {
                                // This guarantees the weapon isn't present in the repo.
                                repo.Add(weapon);
                            }
                        }
                    }
                    
                    // The list now has two segments. The first one has the previous weapons, with their order preserved. Then, after that, we have the new weapons.
                    // The first segment is [0, initialRepoSize). We need to remove all elements in it that don't have their rebuild state set to KeepInRepo, but also reset this state to Empty for all of them.
                    // The second segment won't be touched.
                    // Inline RemoveAll modified to reset the temp state:
                    
                    var freeIndex = 0;
                    
                    while (freeIndex < initialRepoSize)
                    {
                        var weapon = repo[freeIndex];
                        
                        if (weapon.RepoRebuildStateTemp == LogicalWeapon.RepoRebuildState.KeepInRepo)
                        {
                            weapon.RepoRebuildStateTemp = LogicalWeapon.RepoRebuildState.Empty;
                            freeIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // ReSharper disable once InvertIf
                    if (freeIndex < initialRepoSize)
                    {
                        repo[freeIndex].RepoRebuildStateTemp = LogicalWeapon.RepoRebuildState.Empty;

                        for (var readIndex = freeIndex + 1; readIndex < initialRepoSize; readIndex++)
                        {
                            var weapon = repo[readIndex];
                            var shouldKeep = weapon.RepoRebuildStateTemp == LogicalWeapon.RepoRebuildState.KeepInRepo;
                            weapon.RepoRebuildStateTemp = LogicalWeapon.RepoRebuildState.Empty;

                            if (shouldKeep)
                            {
                                repo[freeIndex] = weapon;
                                freeIndex++;
                            }
                        }

                        var itemsToRemove = initialRepoSize - freeIndex;
                        
                        if (itemsToRemove > 0)
                        { 
                            repo.RemoveRange(freeIndex, itemsToRemove);
                        }
                    }
                }
            }
            
            private void SortRepositoriesByAngleCost()
            {
                for (var threatIndex = 0; threatIndex < Threats.Count; threatIndex++)
                {
                    var threat = Threats[threatIndex];
                    var weapons = threat.WeaponCandidates;
                    var weaponCount = weapons.Count;
                    
                    if (weaponCount < 2)
                    {
                        continue;
                    } 

                    var targetPos = threat.Ref.Position;

                    for (var weaponIndex = 0; weaponIndex < weaponCount; weaponIndex++)
                    {
                        var weapon = weapons[weaponIndex];

                        var weaponPosition = weapon.Ref.GetScope.Info.Position;
                        var dirToTarget = targetPos - weaponPosition;
                        
                        if (dirToTarget.LengthSquared() > 1)
                        {
                            dirToTarget.Normalize();
                            var currentForward = weapon.Ref.GetScope.Info.Direction; // I hope I am doing this right... We'll debug draw it to be sure
                            weapon.TempCurrentTurnCost = (float)((1.0 - Vector3D.Dot(currentForward, dirToTarget)) * weapon.TurnCostMultiplier);
                        }
                        else
                        {
                            weapon.TempCurrentTurnCost = 0f;
                        }
                    }

                    // Insertion sort:
                    for (var i = 1; i < weaponCount; i++)
                    {
                        var key = weapons[i];
                        var keyCost = key.TempCurrentTurnCost;
                        var j = i - 1;
                        
                        while (j >= 0 && weapons[j].TempCurrentTurnCost > keyCost)
                        {
                            weapons[j + 1] = weapons[j];
                            j--;
                        }
                        
                        weapons[j + 1] = key;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     This is the actual bread and butter of saving the ship.
    ///     It acts as a me-first system. Torpedoes that are closest are obviously the biggest danger to the ship, so that's our threat heuristic.
    /// </summary>
    internal sealed class ClosestTargetingFireDistributionSystem : FireDistributionSystem
    {
        /// <summary>
        ///     If true, when the threats outnumber the PDCs, the PDC value is ignored and one PDC is assigned to each threat. 
        /// </summary>
        //private readonly bool _preferFairness; // We'd need to determine if there are unengaged torpedoes exactly. It's a bit more difficult for now
        
        public ClosestTargetingFireDistributionSystem(FireDistributionManager manager) : base(manager)
        {
            
        }

        #region Weapon List Acquisition
        
        protected override IEnumerable<Weapon> ScanForValidWeapons()
        {
            var weaponComps = Manager.MasterAi.WeaponComps;
            
            for (var componentIndex = 0; componentIndex < weaponComps.Count; componentIndex++)
            {
                var comp = weaponComps[componentIndex];
                
                // We would check the terminal here, if the DF is enabled
                // BD pls add
                
                for (var weaponIndex = 0; weaponIndex < comp.Collection.Count; weaponIndex++)
                {
                    var w = comp.Collection[weaponIndex];
                    
                    if (FireDistributionManager.IsValidPdc(w) && w.System.ClosestFirst)
                    {
                        yield return w;
                    }
                }
            }
        }

        protected override bool IsCurrentWeaponListStillValid() => CheckForWeaponStateChangesOrUnmatchedConditions(w => w.System.ClosestFirst);

        public override bool IsValidWeapon(Weapon weapon)
        {
            return FireDistributionManager.IsValidPdc(weapon) && weapon.System.ClosestFirst;
        }

        #endregion

        /// <summary>
        ///     Sorts the threats by distance ascending (so, highest threat to the lowest threat).
        /// </summary>
        protected override void SetupTickStartCore()
        {
            // Another fucking local sort because the profiler would (probably) catch the comparer...
            var threats = Network.Threats;
            var count = threats.Count;
    
            for (int i = 1; i < count; i++)
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
            
            ComputeAssignments();
        }
    }

    /// <summary>
    ///     Fire distribution system that tries to distribute fire evenly, using seekers.
    /// </summary>
    internal sealed class MaximumSpreadTargetingFireDistributionSystem : FireDistributionSystem
    {
        public MaximumSpreadTargetingFireDistributionSystem(FireDistributionManager manager) : base(manager)
        {
            
        }

        #region Weapon List Acquisition
        
        protected override IEnumerable<Weapon> ScanForValidWeapons()
        {
            var weaponComps = Manager.MasterAi.WeaponComps;
            
            for (var componentIndex = 0; componentIndex < weaponComps.Count; componentIndex++)
            {
                var comp = weaponComps[componentIndex];
                
                // We would check the terminal here, if the DF is enabled
                // BD pls add
                
                for (var weaponIndex = 0; weaponIndex < comp.Collection.Count; weaponIndex++)
                {
                    var w = comp.Collection[weaponIndex];
                    
                    if (FireDistributionManager.IsValidPdc(w) && !w.System.ClosestFirst)
                    {
                        yield return w;
                    }
                }
            }
        }
        
        protected override bool IsCurrentWeaponListStillValid() => CheckForWeaponStateChangesOrUnmatchedConditions(w => !w.System.ClosestFirst);

        public override bool IsValidWeapon(Weapon weapon)
        {
            return FireDistributionManager.IsValidPdc(weapon) && !weapon.System.ClosestFirst;
        }

        #endregion

        protected override void SetupTickStartCore()
        {
            // Another fucking local sort because the profiler would (probably) catch the comparer...
            var threats = Network.Threats;
            var count = threats.Count;
    
            for (var i = 1; i < count; i++)
            {
                var key = threats[i];
                var keySeekers = key.Ref.Seekers.Count;
                
                // The torps will enter range with 0 seekers, we use a secondary heuristic here:
                var keyDist = key.DistanceToGridCenter; 
                var j = i - 1;
        
                while (j >= 0)
                {
                    var compSeekers = threats[j].Ref.Seekers.Count;
                    
                    // Sort primarily by lowest seekers so we spread the fire to everything:
                    if (compSeekers > keySeekers)
                    {
                        threats[j + 1] = threats[j];
                        j--;
                    }
                    // If seekers are tied, sort by closest distance:
                    else if (compSeekers == keySeekers && threats[j].DistanceToGridCenter > keyDist)
                    {
                        threats[j + 1] = threats[j];
                        j--;
                    }
                    else
                    {
                        break;
                    }
                }
                
                threats[j + 1] = key;
            }
            
            ComputeAssignments();
        }
    }
}
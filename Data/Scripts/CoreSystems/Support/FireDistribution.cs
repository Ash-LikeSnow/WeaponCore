using System;
using System.Collections.Generic;
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
    internal static class FireDistributionConst
    {
        public const int UiWeaponValueFactor = 10;
        public const int UiTurnCostFactor = 1000;
        public const int MinMinLockTime = 15;
        public const int MaxMinLockTime = 1200;
    }
    
    /// <summary>
    ///     Manages multiple <see cref="FireDistributionSystem"/>s.
    ///     Each system will manage a specific subset of the grid's weapons. The subsets are guaranteed to be disjoint.
    /// </summary>
    internal sealed class FireDistributionManager
    {
        // Would be kind of a mess to inline this one, won't lie
        public static bool IsValidWeaponForFireDistribution(Weapon w)
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

            return system.AllowFireDistribution;
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
        
            /// <summary>
            ///     The index in the <see cref="FireDistributionSystem.Weapons"/> list and the <see cref="FireDistributionSystem.IsWeaponAssignedToAnything"/> and the <see cref="ThreatGraph.Threat.RowData"/>.
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
        public readonly ThreatGraph Network = new ThreatGraph();
        
        public uint LastUpdateTick { get; private set; } = uint.MaxValue;
        
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
            
                    if (FireDistributionManager.IsValidWeaponForFireDistribution(w) && predicate(w))
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

        private readonly HashSet<LogicalWeapon> _aliveWeaponsTemp = new HashSet<LogicalWeapon>();
        
        private void RebuildWeaponListAndLookups()
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

                if (_weaponCompsVersion != Manager.MasterAi.WeaponCompsVersion || !IsCurrentWeaponListStillValid())
                {
                    RebuildWeaponListAndLookups();
                    _weaponCompsVersion = Manager.MasterAi.WeaponCompsVersion;
                    
                    MyAPIGateway.Utilities.ShowMessage("FCS", $"Rebuild weapons: {Weapons.Count}, ver {_weaponCompsVersion}");
                }
                        
                LoadWeaponSettings();
                ClearAssignmentState();
                
                var grid = Manager.MasterAi.GridEntity;
                var projectileList = Manager.MasterAi.ProjectileCache;
                
                if (grid != null && Weapons.Count > 0 && projectileList.Count > 0)
                {
                    Network.UpdateDataStructure(projectileList, grid, Weapons);
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
            Array.Clear(Assignments, 0, Assignments.Length);
            Array.Clear(IsWeaponAssignedToAnything, 0, IsWeaponAssignedToAnything.Length);
        }

        /// <summary>
        ///     First, the torpedoes are prioritized by the specific system implementation using a time-coherent sort.
        ///     Then, the algorithm starts assigning PDCs to the torpedoes by priority, based on the cost of assignment.
        ///     The value of the PDCs will also determine how many are assigned.
        ///     This greedy cost problem assignment algorithm produces good results with very low cost, compared to an optimal assignment algorithm.
        ///     In turn, we can run it multiple times per frame as the expensive LoS checks inform us that the PDCs cannot shoot the assigned torpedoes (the real cost is building the matrix, which is done once for the frame).
        /// </summary>
        protected virtual void ComputeAssignments()
        { 
            ClearAssignmentState();
                
            var weapons = Weapons;
            var weaponsCount = Weapons.Count;
            var threats = Network.Threats;
            var isAssigned = IsWeaponAssignedToAnything;
            var assignments = Assignments;
            var currentTick = Session.I.Tick;

            // Unassigned weapons left. Used to early-exit.
            var weaponsRemaining = weaponsCount;
            
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
                        if (targetProjectile.State == Projectile.ProjectileState.Alive && currentTick - weapon.Ref.Target.ChangeTick < weapon.MinimumLockDuration)
                        {
                            // If true, then the weapon must keep the current target locked; we are not allowed to reassign.
                            // We will write it in our data structure:
                            assignments[weaponIndex] = targetProjectile;
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
                var rowData = threat.RowData;

                var remainingThreatValue = 1.0f;

                // Apply committed weapons. These are the weapons whose locks we cannot change yet.
                // If they are locked onto this torp, then by all means, take them into account:
                for (var committedWeaponIndex = 0; committedWeaponIndex < weaponsCount; committedWeaponIndex++)
                {
                    // We can elide the isAssigned in this case.
                    
                    if (assignments[committedWeaponIndex] == threat.Ref && rowData[committedWeaponIndex] != 0)
                    {
                        remainingThreatValue -= weapons[committedWeaponIndex].WeaponValue;
                    }
                }
                
                if (remainingThreatValue < 1e-4f)
                {
                    // The torpedo is fully engaged by the committed weapons.
                    continue;
                }
                
                // Assign using the cost. The LSB is 1 if the weapon can shoot the torp, and the rest is a quantized cost.
                while (weaponsRemaining > 0 && remainingThreatValue > 1e-4)
                {
                    // Finds the weapon that can shoot the torp and has minimum cost.
                    // P.S. This minimum-cost extraction means the entire algorithm's complexity is a factor of how many PDCs a torp needs to be assigned.
                    // So we will want to place a reasonable lower limit in the PDC value slider.
                    var bestIndex = -1;
                    var bestCost = int.MaxValue;
                    for (var candidateIndex = 0; candidateIndex < weaponsCount; candidateIndex++)
                    {
                        if (isAssigned[candidateIndex])
                        {
                            continue;
                        }

                        var cell = rowData[candidateIndex];

                        if (cell == 0)
                        {
                            // Weapon cannot shoot the torp:
                            continue;
                        }

                        var cost = cell >> 1;

                        if (bestIndex == -1 || cost < bestCost)
                        {
                            bestIndex = candidateIndex;
                            bestCost = cost;
                        }
                    }

                    if (bestIndex == -1)
                    {
                        // There are no other weapons to assign:
                        break;
                    }
                    
                    // And assigns it:
                    var candidateWeapon = weapons[bestIndex];
                    remainingThreatValue -= candidateWeapon.WeaponValue;
                    assignments[bestIndex] = threat.Ref;
                    isAssigned[candidateWeapon.Index] = true;
                    weaponsRemaining--;
                }
            }
        }
        
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
            
            public void MarkCannotShoot(Projectile projectile)
            {
                _lock.AcquireExclusive();

                try
                {
                    ThreatGraph.Threat threat;
                    if (!_system.Network.ThreatsByProjectile.TryGetValue(projectile, out threat))
                    {
                        IsValid = false;
                        return;
                    }
                    
                    threat.RowData[_logicalWeapon.Index] = 0;
                
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
            Network.Clear();
            LastUpdateTick = uint.MaxValue;
        }

        public sealed class ThreatGraph
        {
            public sealed class Threat
            {
                public Projectile Ref;
                public ushort[] RowData;
                public double DistanceToGridCenter; // Actual distance, not squared!
            }
            
            public readonly List<Threat> Threats = new List<Threat>();
            public Dictionary<Projectile, Threat> ThreatsByProjectile = new Dictionary<Projectile, Threat>();
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
            
            private void LoadProjectiles(List<Projectile> projectiles, MyCubeGrid grid, int weaponCount)
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
                        RowData = new ushort[weaponCount],
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

            private void ResizeColumnsAndClearMatrix(int weaponCount)
            {
                var threats = Threats;
                for (var threatIndex = 0; threatIndex < threats.Count; threatIndex++)
                {
                    var threat = threats[threatIndex];
                    var rowData = threat.RowData;

                    if (rowData.Length == weaponCount)
                    {
                        Array.Clear(rowData, 0, weaponCount);
                    }
                    else
                    {
                        threat.RowData = new ushort[weaponCount];
                    }
                }
            }
            
            /// <summary>
            ///     Builds the N×M matrix, where N is the number of torpedoes and M is the number of weapons.
            ///     Each cell encodes whether the PDC can engage the torpedo, and the cost of doing so.
            /// </summary>
            /// <param name="projectileList"></param>
            /// <param name="grid"></param>
            /// <param name="weapons"></param>
            public void UpdateDataStructure(List<Projectile> projectileList, MyCubeGrid grid, List<LogicalWeapon> weapons)
            {
                /*
                 * Loads new projectiles and removes stale projectiles. Preserves order of persisted projectiles.
                 * Also updates the distances for all projectiles.
                 */
                LoadProjectiles(projectileList, grid, weapons.Count);

                /*
                 * Zeroes out the matrix and resizes each row data array if needed.
                 */
                ResizeColumnsAndClearMatrix(weapons.Count);
                
                /*
                 * Computes the visibility and cost for each cell.
                 */
                ComputeMatrix(weapons, grid);
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
                ///     These indices are the global ones, that we can use.
                /// </summary>
                public List<int> WeaponIndices;
            }

            private readonly List<RangeBand> _rangeBands = new List<RangeBand>();
            private readonly Dictionary<int, RangeBand> _rangeBandsByRange = new Dictionary<int, RangeBand>();
            private readonly Stack<List<int>> _indexListPool = new Stack<List<int>>();

            public void Clear()
            {
                Threats.Clear();
                ThreatsByProjectile.Clear();
                ClearBands();
            }
            
            public void ClearBands()
            {
                var bands = _rangeBands;

                for (var bandIndex = 0; bandIndex < bands.Count; bandIndex++)
                {
                    var list = bands[bandIndex].WeaponIndices;
                    list.Clear();
                    _indexListPool.Push(list);
                }

                bands.Clear();
                _rangeBandsByRange.Clear();
            }

            // ReSharper disable InconsistentNaming
            private int __matrixComputeLocalCacheSize = -1;
            private Vector3D[] __weaponPositions;
            private Vector3[] __weaponDirections;
            private float[] __weaponMaxSqrDists;
            private float[] __weaponTurnCosts;
            // ReSharper restore InconsistentNaming
            
            private void ComputeMatrix(List<LogicalWeapon> weapons, MyCubeGrid grid)
            {
                ClearBands();

                var weaponCount = weapons.Count;

                if (weaponCount != __matrixComputeLocalCacheSize)
                {
                    __weaponPositions = new Vector3D[weaponCount];
                    __weaponDirections = new Vector3[weaponCount];
                    __weaponMaxSqrDists = new float[weaponCount];
                    __weaponTurnCosts = new float[weaponCount];
                    __matrixComputeLocalCacheSize = weaponCount;
                }

                var weaponPositions = __weaponPositions;
                var weaponDirections = __weaponDirections;
                var weaponMaxSqrDists = __weaponMaxSqrDists;
                var weaponTurnCosts = __weaponTurnCosts;
               
                var bands = _rangeBands;
                var bandsByRange = _rangeBandsByRange;

                // Prefetch all the data behind those references into these small arrays, and build the bands:
                for (var weaponIndex = 0; weaponIndex < weaponCount; weaponIndex++)
                {
                    var weapon = weapons[weaponIndex];
                    var scopeInfo = weapon.Ref.GetScope.Info;

                    weaponPositions[weaponIndex] = scopeInfo.Position;
                    weaponDirections[weaponIndex] = scopeInfo.Direction;
                    weaponMaxSqrDists[weaponIndex] = (float)weapon.Ref.MaxTargetDistanceSqr;
                    weaponTurnCosts[weaponIndex] = 1.0f; // TODO pls bd
                    
                    var quantizedRange = Math.Max((int)weapon.Ref.MaxTargetDistance, 1);

                    RangeBand band;
                    if (!bandsByRange.TryGetValue(quantizedRange, out band))
                    {
                        band = new RangeBand
                        {
                            TruncatedRange = quantizedRange,
                            WeaponIndices = _indexListPool.Count > 0 ? _indexListPool.Pop() : new List<int>()
                        };
                        
                        bands.Add(band);
                        bandsByRange.Add(quantizedRange, band);
                    }
                    
                    band.WeaponIndices.Add(weaponIndex);
                }
                
                // Sort descending. We can afford this since we only have a few bands.
                // We will use this to optimize the repository update algorithm.
                bands.Sort((a, b) => b.TruncatedRange.CompareTo(a.TruncatedRange));
                
                var gridRadius = grid.PositionComp.WorldVolume.Radius;
                
                // With this band data structure, the inner loop is not exactly O(N * M), unless all of the torps really are in range.
                for (var threatIndex = 0; threatIndex < Threats.Count; threatIndex++)
                {
                    var threat = Threats[threatIndex];
                    var threatPosition = threat.Ref.Position;
                    var threatDistance = threat.DistanceToGridCenter;
                    
                    // Lower and upper bounds on the distance to weapons:
                    var minimumDistance = threatDistance - gridRadius;
                    var rowData = threat.RowData;

                    for (var bandIndex = 0; bandIndex < bands.Count; bandIndex++)
                    {
                        var rangeBand = bands[bandIndex];
                        
                        if (minimumDistance > rangeBand.TruncatedRange)
                        {
                            // Guaranteed to be out of range.
                            // We sorted the bands in descending order, so we can safely prune the rest of the list:
                            break;
                        }

                        var bandWeaponIndices = rangeBand.WeaponIndices;
                        var bandWeaponCount = bandWeaponIndices.Count;
                        for (var weaponIdIndex = 0; weaponIdIndex < bandWeaponCount; weaponIdIndex++)
                        {
                            var weaponIndex = bandWeaponIndices[weaponIdIndex];
                            
                            var weaponPosition = weaponPositions[weaponIndex];
                            var dirToTarget = threatPosition - weaponPosition;
                            var distanceToTargetSqr = dirToTarget.LengthSquared();
                            
                            if (distanceToTargetSqr > weaponMaxSqrDists[weaponIndex])
                            {
                                continue;
                            }
                            
                            // Range-of-motion check: seems a bit involved. TODO
                            
                            // Turn cost multiplier must be less than or equal to 1 !
                            
                            ushort quantizedCost = 0;
                            if (distanceToTargetSqr > 1.0)
                            {
                                var weaponDirection = weaponDirections[weaponIndex];
    
                                double u;
                                Vector3D.Dot(ref weaponDirection, ref dirToTarget, out u);

                                double costMultiplier;
    
                                if (u > 0)
                                {
                                    // Maps [0, 90] degrees to cost [0, 1]
                                    costMultiplier = 1.0 - u * u / distanceToTargetSqr;
                                }
                                else
                                {
                                    // Maps [90, 180] degrees to cost [1, 2]
                                    costMultiplier = 1.0 + u * u / distanceToTargetSqr;
                                }

                                var cost = costMultiplier * weaponTurnCosts[weaponIndex];

                                quantizedCost = (ushort) Math.Min(cost * 16383.5, 32767.0);
                            }

                            rowData[weaponIndex] = (ushort)((quantizedCost << 1) | 1);
                        }
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
                    
                    if (FireDistributionManager.IsValidWeaponForFireDistribution(w) && w.PrioritizeClosestTarget)
                    {
                        yield return w;
                    }
                }
            }
        }

        protected override bool IsCurrentWeaponListStillValid() => CheckForWeaponStateChangesOrUnmatchedConditions(w => w.PrioritizeClosestTarget);

        public override bool IsValidWeapon(Weapon weapon)
        {
            return FireDistributionManager.IsValidWeaponForFireDistribution(weapon) && weapon.PrioritizeClosestTarget;
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
                    
                    if (FireDistributionManager.IsValidWeaponForFireDistribution(w) && !w.PrioritizeClosestTarget)
                    {
                        yield return w;
                    }
                }
            }
        }
        
        protected override bool IsCurrentWeaponListStillValid() => CheckForWeaponStateChangesOrUnmatchedConditions(w => !w.PrioritizeClosestTarget);

        public override bool IsValidWeapon(Weapon weapon)
        {
            return FireDistributionManager.IsValidWeaponForFireDistribution(weapon) && !weapon.PrioritizeClosestTarget;
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
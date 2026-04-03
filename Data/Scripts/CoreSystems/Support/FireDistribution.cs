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
                new ClosestTargetingFireDistributionSystem(this)
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
        }

        private int _weaponCompsVersion = -1;
        
        protected readonly FireDistributionManager Manager;

        public FireDistributionSystem(FireDistributionManager manager)
        {
            Manager = manager;
        }

        private readonly FastResourceLock _lock = new FastResourceLock();
        
        protected readonly List<LogicalWeapon> Weapons = new List<LogicalWeapon>();
        protected readonly Dictionary<Weapon, int> IndexByWeapon = new Dictionary<Weapon, int>();
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
        ///     Checks if this manager handles the specified weapon.
        ///     All of these results must be self-exclusive across the different systems.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns>True if this system handles the specified weapon. Otherwise, false.</returns>
        public abstract bool IsValidWeapon(Weapon weapon);
        
        #endregion
        
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

                var weapons = Weapons;
                var indexByWeapon = IndexByWeapon;

                // Also, this doesn't run unless we receive updates (obviously). We need the active loop
                if (_weaponCompsVersion != Manager.MasterAi.WeaponCompsVersion || !IsCurrentWeaponListStillValid())
                {
                    Log($"Update comps {_weaponCompsVersion} -> {Manager.MasterAi.WeaponCompsVersion}");
                        
                    weapons.Clear();
                    indexByWeapon.Clear();
                    
                    foreach (var weapon in ScanForValidWeapons())
                    {
                        var index = weapons.Count;
                        
                        weapons.Add(new LogicalWeapon
                        {
                            Ref = weapon,
                            TurnCostMultiplier = 1.0f,
                            WeaponValue = 1.0f,
                            MinimumLockDuration = 20,
                            Index = index
                        });
                        
                        indexByWeapon.Add(weapon, index);
                    }

                    if (weapons.Count > IsWeaponAssignedToAnything.Length)
                    {
                        IsWeaponAssignedToAnything = new bool[weapons.Count];
                    }
                    else
                    {
                        Array.Clear(IsWeaponAssignedToAnything, 0, Weapons.Count);
                    }   
                        
                    _weaponCompsVersion = Manager.MasterAi.WeaponCompsVersion;
                }
                        
                // TODO read all the sliders here
                
                Assignments.Clear();
                var grid = Manager.MasterAi.GridEntity;
                    
                if (grid != null && weapons.Count > 0 && Network.LoadProjectilesFromMasterAi(Manager.MasterAi, grid) > 0)
                {
                    Network.InitializeRoughWeapons(weapons, grid);
                    Network.TrimUnreachableThreats();
                    Network.SortRepositoriesByAngleCost();
                    
                    SetupTickStartCore();
                }
                else
                {
                    Network.ClearBands();
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

        public virtual void CleanUp()
        {
            Weapons.Clear();
            IndexByWeapon.Clear();
            Assignments.Clear();
            Network.ClearBands();
            LastUpdateTick = uint.MaxValue;
        }

        public sealed class ThreatGraph
        {
            public struct Threat
            {
                public Projectile Ref;
                public List<LogicalWeapon> WeaponCandidates;
                public double DistanceToGridCenter; // Actual distance, not squared!
            }
            
            public List<Threat> Threats = new List<Threat>();
            public Dictionary<Projectile, Threat> ThreatsByProjectile = new Dictionary<Projectile, Threat>();
            private readonly Stack<List<LogicalWeapon>> _pool = new Stack<List<LogicalWeapon>>();

            // Used for trimming. Reference swapped with Threats.
            private List<Threat> _threatsTrim = new List<Threat>();

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
            
            public int LoadProjectilesFromMasterAi(Ai ai, MyCubeGrid grid)
            {
                for (var threatIndex = 0; threatIndex < Threats.Count; threatIndex++)
                {
                    FreePooledWeaponList(Threats[threatIndex].WeaponCandidates);
                }
                
                Threats.Clear();
                ThreatsByProjectile.Clear();
                
                var gridCenter = grid.PositionComp.WorldAABB.Center;
                var projectiles = ai.ProjectileCache;
                
                for (var projectileIndex = 0; projectileIndex < projectiles.Count; projectileIndex++)
                {
                    var projectile = projectiles[projectileIndex];
                    
                    var threat = new Threat
                    {
                        Ref = projectile,
                        WeaponCandidates = GetPooledWeaponList(),
                        DistanceToGridCenter = Vector3D.Distance(projectile.Position, gridCenter)
                    };
                    
                    Threats.Add(threat);
                    ThreatsByProjectile.Add(projectile, threat);
                }

                return Threats.Count;
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

            public void ClearBands()
            {
                var bands = _rangeBands;

                for (var bandIndex = 0; bandIndex < bands.Count; bandIndex++)
                {
                    FreePooledWeaponList(bands[bandIndex].Weapons);
                }

                bands.Clear();
                _rangeBandsByRange.Clear();
                
                _threatsTrim.Clear();
            }
            
            /// <summary>
            ///     Initializes the <see cref="Threat.WeaponCandidates"/> by range checks.
            /// 
            ///     Comparing each PDC's range with each torp's distance would be O(NM).
            ///     We can take advantage of the fact the PDC network usually has a few discrete ranges all around.
            ///     The idea is, we will quantize the ranges, then separate them into "bands".
            ///     With this, we can do some simple spatial partitioning to determine which PDCs are in range of which torpedoes.
            /// </summary>
            /// <param name="weapons"></param>
            /// <param name="grid"></param>
            public void InitializeRoughWeapons(List<LogicalWeapon> weapons, MyCubeGrid grid)
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
                
                var gridRadius = grid.PositionComp.WorldVolume.Radius;

                for (var threatIndex = 0; threatIndex < Threats.Count; threatIndex++)
                {
                    var threat = Threats[threatIndex];
                    var distanceToCenter = threat.DistanceToGridCenter;
                    
                    // Lower and upper bounds on the distance to weapons:
                    var minimumDistance = distanceToCenter - gridRadius;
                    var maximumDistance = distanceToCenter + gridRadius;

                    for (var bandIndex = 0; bandIndex < bands.Count; bandIndex++)
                    {
                        var rangeBand = bands[bandIndex];
                        
                        if (minimumDistance > rangeBand.TruncatedRange)
                        {
                            // Guaranteed to be out of range:
                            continue; 
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
                            
                            threat.WeaponCandidates.Add(weapon);
                        }
                    }
                }
            }
            
            /// <summary>
            ///     Removes the threats which cannot be targeted at all.
            ///     This will lower the load on the code downstream.
            /// </summary>
            public void TrimUnreachableThreats()
            {
                var trimmedList = _threatsTrim;
                trimmedList.Clear();
                
                var threatsSource = Threats;
                for (var sourceThreatIndex = 0; sourceThreatIndex < threatsSource.Count; sourceThreatIndex++)
                {
                    var sourceThreat = threatsSource[sourceThreatIndex];

                    if (sourceThreat.WeaponCandidates.Count > 0)
                    {
                        trimmedList.Add(sourceThreat);
                    }
                    else
                    {
                        ThreatsByProjectile.Remove(sourceThreat.Ref);
                    }
                }

                var temp = Threats;
                Threats = trimmedList;
                _threatsTrim = temp;
            }

            /// <summary>
            ///     Sorts the PDCs able to engage each torp by the cost of turning.
            /// </summary>
            public void SortRepositoriesByAngleCost()
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
                    
                    if (w.System.ClosestFirst)
                    {
                        yield return w;
                    }
                }
            }
        }

        protected override bool IsCurrentWeaponListStillValid()
        {
            for (var weaponIndex = 0; weaponIndex < Weapons.Count; weaponIndex++)
            {
                var weapon = Weapons[weaponIndex];

                if (!weapon.Ref.System.ClosestFirst)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool IsValidWeapon(Weapon weapon)
        {
            return weapon.System.ClosestFirst;
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
}
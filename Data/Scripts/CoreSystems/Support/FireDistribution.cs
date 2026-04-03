using System;
using System.Collections.Generic;
using System.Threading;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage;
using VRageMath;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable InlineTemporaryVariable

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    internal sealed class FireDistributionManager
    {
        public readonly Ai MasterAi;

        private readonly FireDistributionSystem _closestSystem;
        
        public FireDistributionManager(Ai masterAi)
        {
            MasterAi = masterAi;
            
            _closestSystem = new ClosestTargetingFireDistributionSystem(this, GetClosestWeaponsIterable);
        }

        private IEnumerable<Weapon> GetClosestWeaponsIterable()
        {
            for (var componentIndex = 0; componentIndex < MasterAi.WeaponComps.Count; componentIndex++)
            {
                var comp = MasterAi.WeaponComps[componentIndex];

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

        public FireDistributionSystem.Accessor CreateAccessor(Weapon weapon)
        {
            if (weapon.System.ClosestFirst)
            {
                return _closestSystem.CreateAccessor(weapon);
            }

            return new FireDistributionSystem.Accessor();
        }
        
        public void CleanUp()
        {
            _closestSystem.CleanUp();
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
            
            // Temporary value used by the local sort. Invalid to access anywhere else!
            public float CurrentTurnCost;
            
            public int Index;
        }
        
        protected readonly FireDistributionManager Manager;
        private readonly Func<IEnumerable<Weapon>> _weaponListSupplier;

        public FireDistributionSystem(FireDistributionManager manager, Func<IEnumerable<Weapon>> weaponListSupplier)
        {
            Manager = manager;
            _weaponListSupplier = weaponListSupplier;
        }

        private readonly FastResourceLock _lock = new FastResourceLock();
       
        protected readonly List<LogicalWeapon> Weapons = new List<LogicalWeapon>();

        protected readonly Dictionary<Weapon, int> IndexByWeapon = new Dictionary<Weapon, int>();
        
        protected bool[] WeaponsAssigned { get; private set; } = Array.Empty<bool>();
        
        protected readonly Dictionary<Weapon, Projectile> Assignments = new Dictionary<Weapon, Projectile>();
        
        public readonly ThreatGraph Network = new ThreatGraph();
        
        public uint LastUpdateTick { get; private set; } = uint.MaxValue;
        
        // P.S. depending on how often this runs, we may want to have a dedicated loop that clears those references.
        
        /// <summary>
        ///     Runs the algorithm for the first time in a tick, if necessary.
        /// </summary>
        private void SetupInitialRun()
        {
            var sessionTick = Session.I.Tick;

            if (LastUpdateTick != sessionTick)
            {
                _lock.AcquireExclusive();

                try
                {
                    if (LastUpdateTick == sessionTick)
                    {
                        return;
                    }

                    var weapons = Weapons;
                    var indexByWeapon = IndexByWeapon;
                    weapons.Clear();
                    indexByWeapon.Clear();
                    
                    foreach (var weapon in _weaponListSupplier.Invoke())
                    {
                        var index = weapons.Count;
                        
                        weapons.Add(new LogicalWeapon
                        {
                            Ref = weapon,
                            TurnCostMultiplier = 1.0f, // Todo read sliders
                            WeaponValue = 1.0f,
                            Index = index
                        });
                        
                        indexByWeapon.Add(weapon, index);
                    }

                    if (weapons.Count > WeaponsAssigned.Length)
                    {
                        WeaponsAssigned = new bool[weapons.Count];
                    }
                    else
                    {
                        Array.Clear(WeaponsAssigned, 0, Weapons.Count);
                    }
                        
                    Assignments.Clear();
                    var grid = Manager.MasterAi.GridEntity;
                    
                    if (grid != null && weapons.Count > 0 && Network.LoadProjectilesFromMasterAi(Manager.MasterAi, grid) > 0)
                    {
                        Network.InitializeRoughWeapons(weapons, grid);
                        Network.TrimUnreachableThreats();
                        Network.SortRepositoriesByAngleCost();
                        
                        InitialRun();
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
        }

        /// <summary>
        ///     Runs the algorithm the first time in a tick.
        ///     The weapon settings, target positions and states are considered constant throughout the tick.
        /// </summary>
        protected abstract void InitialRun();

        /// <summary>
        ///     Called when a weapon finds it cannot shoot the assigned target, after that issue has already been marked.
        /// </summary>
        protected abstract void Recalculate();
        
        public Accessor CreateAccessor(Weapon weapon)
        {
            SetupInitialRun();
            return new Accessor(this, weapon, _lock);
        }
        
        public struct Accessor
        {
            public bool IsValid;
            
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
                    _system.Recalculate();                   
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
                            weapon.CurrentTurnCost = (float)((1.0 - Vector3D.Dot(currentForward, dirToTarget)) * weapon.TurnCostMultiplier);
                        }
                        else
                        {
                            weapon.CurrentTurnCost = 0f;
                        }
                    }

                    // Insertion sort:
                    for (var i = 1; i < weaponCount; i++)
                    {
                        var key = weapons[i];
                        var keyCost = key.CurrentTurnCost;
                        var j = i - 1;
                        
                        while (j >= 0 && weapons[j].CurrentTurnCost > keyCost)
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
        
        public ClosestTargetingFireDistributionSystem(FireDistributionManager manager, Func<IEnumerable<Weapon>> weaponListSupplier) : base(manager, weaponListSupplier)
        {
            
        }

        /// <summary>
        ///     Sorts the threats by distance ascending (so, highest threat to lowest threat).
        /// </summary>
        protected override void InitialRun()
        {
            Network.Threats.SortNoAlloc(CompareThreatsByDistance);
            
            TargetAssignment();
        }

        protected override void Recalculate()
        {
            Assignments.Clear();
            TargetAssignment();
        }

        /// <summary>
        ///     Fast suboptimal greedy target assignment.
        /// </summary>
        private void TargetAssignment()
        {
            var threats = Network.Threats;
            var isAssigned = WeaponsAssigned;

            var weaponsRemaining = Weapons.Count;

            for (var threatIndex = 0; threatIndex < threats.Count && weaponsRemaining > 0; threatIndex++)
            {
                var threat = threats[threatIndex];

                var remainingThreatValue = 1.0f;

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
                    
                    if (remainingThreatValue <= 0.0f || Math.Abs(remainingThreatValue) < 1e-4)
                    {
                        break;
                    }
                }
            }
        }

        // How the fuck am I supposed to get around the profiler here?
        // I guess another local sort...
        private static int CompareThreatsByDistance(ThreatGraph.Threat a, ThreatGraph.Threat b)
        {
            return a.DistanceToGridCenter.CompareTo(b.DistanceToGridCenter);
        }
    }
}
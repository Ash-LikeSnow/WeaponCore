using System;
using System.Collections.Generic;
using System.Threading;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRageMath;

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

        public void CleanUp()
        {
            _closestSystem.CleanUp();
        }
    }
    
    internal abstract class FireDistributionSystem
    {
        protected struct LogicalWeapon
        {
            public Weapon Ref;
            public int Index;
        }
        
        protected readonly FireDistributionManager Manager;
        private readonly Func<IEnumerable<Weapon>> _weaponListSupplier;

        public FireDistributionSystem(FireDistributionManager manager, Func<IEnumerable<Weapon>> weaponListSupplier)
        {
            Manager = manager;
            _weaponListSupplier = weaponListSupplier;
        }

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
       
        protected readonly List<LogicalWeapon> Weapons = new List<LogicalWeapon>();
        protected bool[] WeaponsAssigned { get; private set; } = Array.Empty<bool>();
        protected readonly Dictionary<Weapon, Projectile> Assignments = new Dictionary<Weapon, Projectile>();
        
        protected readonly ThreatGraph Threats = new ThreatGraph();
        
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
                _lock.EnterWriteLock();

                try
                {
                    if (LastUpdateTick == sessionTick)
                    {
                        return;
                    }

                    var weapons = Weapons;
                    weapons.Clear();
                    
                    foreach (var weapon in _weaponListSupplier.Invoke())
                    {
                        weapons.Add(new LogicalWeapon
                        {
                            Ref = weapon,
                            Index = weapons.Count
                        });
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
                    
                    if (grid != null && weapons.Count > 0 && Threats.LoadProjectilesFromMasterAi(Manager.MasterAi, grid) > 0)
                    {
                        Threats.InitializeRoughWeaponNetwork(weapons, grid);
                        
                        InitialRun();
                    }
                    else
                    {
                        Threats.Clear();
                    }
                        
                    LastUpdateTick = sessionTick;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        ///     Runs the algorithm the first time in a tick.
        ///     The weapon settings, target positions and states are considered constant throughout the tick.
        /// </summary>
        protected abstract void InitialRun();
        
        public Accessor CreateAccessor(Weapon weapon)
        {
            SetupInitialRun();
            return new Accessor(this, weapon, _lock);
        }
        
        public struct Accessor
        {
            private readonly FireDistributionSystem _system;
            private readonly Weapon _weapon;
            private readonly ReaderWriterLockSlim _lock;

            public Accessor(FireDistributionSystem system, Weapon weapon, ReaderWriterLockSlim accessLock)
            {
                _system = system;
                _weapon = weapon;
                _lock = accessLock;
            }

            public bool TryGetAssignment(out Projectile assignedProjectile)
            {
                _lock.EnterReadLock();

                try
                {
                    return _system.Assignments.TryGetValue(_weapon, out assignedProjectile);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public virtual void CleanUp()
        {
            Weapons.Clear();
            Assignments.Clear();
            LastUpdateTick = uint.MaxValue;
        }

        protected sealed class ThreatGraph
        {
            public struct Threat
            {
                public Projectile Target;
                public List<LogicalWeapon> WeaponCandidates;
                public double DistanceToGridCenter; // Actual distance, not squared!
            }
            
            public readonly List<Threat> Threats = new List<Threat>();
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
            
            public int LoadProjectilesFromMasterAi(Ai ai, MyCubeGrid grid)
            {
                for (var threatIndex = 0; threatIndex < Threats.Count; threatIndex++)
                {
                    FreePooledWeaponList(Threats[threatIndex].WeaponCandidates);
                }
                
                Threats.Clear();
                
                var gridCenter = grid.PositionComp.WorldAABB.Center;
                var projectiles = ai.ProjectileCache;
                
                for (var projectileIndex = 0; projectileIndex < projectiles.Count; projectileIndex++)
                {
                    var projectile = projectiles[projectileIndex];
                    
                    Threats.Add(new Threat
                    {
                        Target = projectile,
                        WeaponCandidates = GetPooledWeaponList(),
                        DistanceToGridCenter = Vector3D.Distance(projectile.Position, gridCenter)
                    });
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

            public void Clear()
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
            ///     Initializes the <see cref="Threat.WeaponCandidates"/> by range checks.
            /// 
            ///     Comparing each PDC's range with each torp's distance would be O(NM).
            ///     We can take advantage of the fact the PDC network usually has a few discrete ranges all around.
            ///     The idea is, we will quantize the ranges, then separate them into "bands".
            ///     With this, we can do some simple spatial partitioning to determine which PDCs are in range of which torpedoes.
            /// </summary>
            /// <param name="weapons"></param>
            /// <param name="grid"></param>
            public void InitializeRoughWeaponNetwork(List<LogicalWeapon> weapons, MyCubeGrid grid)
            {
                Clear();
                
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
                        
                        if (maximumDistance <= rangeBand.TruncatedRange)
                        {
                            // Guaranteed to be in range:
                            threat.WeaponCandidates.AddRange(rangeBand.Weapons);
                            continue;
                        }

                        // We can't say for certain. We need to scan:
                        for (var weaponIndex = 0; weaponIndex < rangeBand.Weapons.Count; weaponIndex++)
                        {
                            var weapon = rangeBand.Weapons[weaponIndex];
                                
                            if (Vector3D.DistanceSquared(weapon.Ref.GetScope.Info.Position, threat.Target.Position) <= weapon.Ref.MaxTargetDistanceSqr)
                            {
                                threat.WeaponCandidates.Add(weapon);
                            }
                        }
                    }
                }
            }
        }
    }

    internal sealed class ClosestTargetingFireDistributionSystem : FireDistributionSystem
    {
        
        public ClosestTargetingFireDistributionSystem(FireDistributionManager manager, Func<IEnumerable<Weapon>> weaponListSupplier) : base(manager, weaponListSupplier) { }

        protected override void InitialRun()
        {
            
        }
    }
}
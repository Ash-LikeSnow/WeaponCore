using System;
using System.Collections.Generic;
using System.Threading;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using CoreSystems.Support;
// ReSharper disable ForCanBeConvertedToForeach

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
            public readonly Weapon Weapon;
            public readonly int Index;

            public LogicalWeapon(Weapon weapon, int index)
            {
                Weapon = weapon;
                Index = index;
            }
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
        
        public uint LastUpdateTick { get; private set; } = uint.MaxValue;
        
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
                    
                    Weapons.Clear();

                    foreach (var weapon in _weaponListSupplier.Invoke())
                    {
                        Weapons.Add(new LogicalWeapon(weapon, Weapons.Count));
                    }

                    if (Weapons.Count > WeaponsAssigned.Length)
                    {
                        WeaponsAssigned = new bool[Weapons.Count];
                    }
                    else
                    {
                        for (var i = 0; i < Weapons.Count; i++)
                        {
                            WeaponsAssigned[i] = false;
                        }
                    }
                        
                    Assignments.Clear();

                    if (Weapons.Count > 0 && Manager.MasterAi.ProjectileCache.Count > 0)
                    {
                        InitialRun();
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

        protected sealed class ThreatGraphDataStructure
        {
            
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
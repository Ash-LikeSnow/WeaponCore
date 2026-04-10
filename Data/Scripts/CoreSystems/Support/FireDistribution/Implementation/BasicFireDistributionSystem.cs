using System;
using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using VRageMath;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable ForCanBeConvertedToForeach

namespace WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution.Implementation
{
    internal sealed class BasicFireDistributionSystem : FireDistributionSystem
    {
        private readonly Storage _distanceSortedStorage = new Storage();

        private bool[] _isThreatHandled = Array.Empty<bool>();

        // Sparse visibility matrix:
        private HashSet<int>[] _cannotShootByThreat = Array.Empty<HashSet<int>>();
        private Vector4D[] _weaponPositionAndRangeSqr = Array.Empty<Vector4D>();
        
        public BasicFireDistributionSystem(FireDistributionManager manager) : base(manager)
        {
            
        }

        protected override void RebuildWeapons()
        {
            base.RebuildWeapons();

            if (_weaponPositionAndRangeSqr.Length != Weapons.Count)
            {
                _weaponPositionAndRangeSqr = new Vector4D[Weapons.Count];
            }
        }

        public override bool IsValidWeaponForSystem(Weapon weapon)
        {
            return FireDistributionSupport.IsValidWeaponForFireDistribution(weapon);
        }

        protected override void UpdateDataStructure(List<Projectile> fullProjectileList, Dictionary<Projectile, bool> lockedOn, MyCubeGrid grid)
        {
            _distanceSortedStorage.UpdateDataStructure(fullProjectileList, lockedOn, grid, Weapons);

            if (fullProjectileList.Count > _cannotShootByThreat.Length)
            {
                _cannotShootByThreat = new HashSet<int>[fullProjectileList.Count + 10];
            }
        }

        protected override void ClearDataStructure()
        {
            _distanceSortedStorage.Clear();
        }

        protected override void SetupTickStartCore()
        {
            var threats = _distanceSortedStorage.Threats;
            var threatCount = threats.Count;
            
            FireDistributionSupport.InsertionSortByDistance(_distanceSortedStorage);

            // Copy new indices and support after sorting:
            _distanceSortedStorage.CopyIndices();
            _distanceSortedStorage.LoadSupport(Weapons);
            
            if (_isThreatHandled.Length < threatCount)
            {
                _isThreatHandled = new bool[threatCount];
            }
            
            for (var index = 0; index < _distanceSortedStorage.Threats.Count; index++)
            {
                _cannotShootByThreat[index]?.Clear();
            }
            
            var weapons = Weapons;
            var weaponsCount = Weapons.Count;
            var weaponPositionAndRange = _weaponPositionAndRangeSqr;
            for (var weaponIndex = 0; weaponIndex < weaponsCount; weaponIndex++)
            {
                var weapon = weapons[weaponIndex].Ref;
                var position = weapon.GetScope.Info.Position;
                weaponPositionAndRange[weaponIndex] = new Vector4D(position.X, position.Y, position.Z, weapon.MaxTargetDistanceSqr);
            }
            
            ComputeAssignments();
        }

        /// <summary>
        ///     Assigns one PDC per torpedo by distance.
        ///     Doesn't take into account turn costs.
        /// </summary>
        protected override void ComputeAssignments()
        {
            ClearAssignmentState();
            
            var weapons = Weapons;
            var weaponsCount = Weapons.Count;
            var weaponPositionAndRange = _weaponPositionAndRangeSqr;
            var threats = _distanceSortedStorage.Threats;
            var isWeaponAssigned = IsWeaponAssignedToAnything;
            var assignments = Assignments;
            var cannotShootByThreat = _cannotShootByThreat;
            
            var isThreatHandled = _isThreatHandled;
            Array.Clear(isThreatHandled, 0, threats.Count);
            
            // Unassigned weapons left. Used to early-exit.
            var weaponsRemaining = weaponsCount;
            
            FireDistributionSupport.LoadWeaponTargets(
                weapons,
                assignments, 
                isWeaponAssigned,
                ref weaponsRemaining,
                _distanceSortedStorage,
                isThreatHandled
            );

            // First pass: Assign remaining PDCs per torp by distance.
            // The other passes: overkill the torpedoes.
            bool isMakingAssignments;

            var isLockedOn = _distanceSortedStorage.IsThreatLockedOn;
            var supportivePd = _distanceSortedStorage.SupportivePd;
            
            do
            {
                isMakingAssignments = false;
                
                for (var threatIndex = 0; threatIndex < threats.Count && weaponsRemaining > 0; threatIndex++)
                {
                    if (isThreatHandled[threatIndex])
                    {
                        continue;
                    }

                    var weaponBlacklist = cannotShootByThreat[threatIndex];
                    var threatPosition = threats[threatIndex].Ref.Position;
                    var isThreatLockedOn = isLockedOn[threatIndex];
                    
                    // Finds the weapon can shoot the torp:
                    for (var candidateWeaponIndex = 0; candidateWeaponIndex < weaponsCount; candidateWeaponIndex++)
                    {
                        if (isWeaponAssigned[candidateWeaponIndex])
                        {
                            continue;
                        }

                        if (!isThreatLockedOn && !supportivePd[candidateWeaponIndex])
                        {
                            continue;
                        }

                        var positionAndRange = weaponPositionAndRange[candidateWeaponIndex];
                        var dx = threatPosition.X - positionAndRange.X;
                        var dy = threatPosition.Y - positionAndRange.Y;
                        var dz = threatPosition.Z - positionAndRange.Z;

                        if (dx * dx + dy * dy + dz * dz > positionAndRange.W)
                        {
                            continue;
                        }

                        if (weaponBlacklist != null && weaponBlacklist.Contains(candidateWeaponIndex))
                        {
                            continue;
                        }
                        
                        var candidateWeapon = weapons[candidateWeaponIndex];
                    
                        // And assigns it:
                        isThreatHandled[threatIndex] = true;
                        assignments[candidateWeaponIndex] = threats[threatIndex].Ref;
                        isWeaponAssigned[candidateWeapon.Index] = true;
                        weaponsRemaining--;

                        isMakingAssignments = true;
                            
                        break;
                    }
                }

                Array.Clear(isThreatHandled, 0, threats.Count);
            } while (isMakingAssignments && weaponsRemaining > 0);
        }

        protected override bool MarkCannotShootCore(LogicalWeapon weapon, Projectile projectile)
        {
            Threat threat;
            if (!_distanceSortedStorage.ThreatsByProjectile.TryGetValue(projectile, out threat))
            {
                return false;
            }

            var blacklist = _cannotShootByThreat[threat.Index];

            if (blacklist == null)
            {
                blacklist = new HashSet<int>();
                _cannotShootByThreat[threat.Index] = blacklist;
            }

            blacklist.Add(weapon.Index);
            
            return true;
        }

        private sealed class Storage : ThreatStorage<Threat>
        {
            protected override Threat CreateInstance(Projectile projectile, int weaponCount) => new Threat
            {
                Ref = projectile
            };
        }
    }
}
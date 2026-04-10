using System;
using System.Collections.Generic;
using System.Linq;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable ForCanBeConvertedToForeach

namespace WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution.Implementation
{
    internal sealed class BasicFireDistributionSystem : FireDistributionSystem
    {
        private readonly Storage _distanceSortedStorage = new Storage();

        // ReSharper disable InconsistentNaming
        private int __assignmentAlgorithmThreatCount = -1;
        private bool[] __isThreatHandled;
        // ReSharper restore InconsistentNaming

        // Sparse visibility matrix:
        private HashSet<int>[] _cannotShootByThreat = Array.Empty<HashSet<int>>();
        private Vector4D[] _weaponPositionAndRangeSqr = Array.Empty<Vector4D>();
        
        public BasicFireDistributionSystem(FireDistributionManager manager) : base(manager)
        {
            
        }

        protected override void RebuildWeapons()
        {
            base.RebuildWeapons();

            if (_weaponPositionAndRangeSqr.Length < Weapons.Count)
            {
                _weaponPositionAndRangeSqr = new Vector4D[Weapons.Count];
            }
        }

        public override bool IsValidWeaponForSystem(Weapon weapon)
        {
            return FireDistributionSupport.IsValidWeaponForFireDistribution(weapon);
        }

        protected override void UpdateDataStructure(List<Projectile> projectileList, MyCubeGrid grid)
        {
            _distanceSortedStorage.UpdateDataStructure(projectileList, grid, Weapons);

            if (projectileList.Count > _cannotShootByThreat.Length)
            {
                _cannotShootByThreat = new HashSet<int>[projectileList.Count + 10];
            }
        }

        protected override void ClearDataStructure()
        {
            _distanceSortedStorage.Clear();
        }

        protected override void SetupTickStartCore()
        {
            FireDistributionSupport.InsertionSortByDistance(_distanceSortedStorage);

            // Copy new indices after sorting:
            _distanceSortedStorage.CopyIndices();
            
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
                
                weaponPositionAndRange[weaponIndex] = new Vector4D(
                    position.X,
                    position.Y,
                    position.Z,
                    weapon.MaxTargetDistanceSqr
                );
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

            if (__assignmentAlgorithmThreatCount < threats.Count)
            {
                __isThreatHandled = new bool[threats.Count];
                __assignmentAlgorithmThreatCount = threats.Count;
            }
            else
            {
                Array.Clear(__isThreatHandled, 0, __isThreatHandled.Length);
            }

            var isThreatAssigned = __isThreatHandled;
            
            // Unassigned weapons left. Used to early-exit.
            var weaponsRemaining = weaponsCount;
            
            FireDistributionSupport.LoadWeaponTargets(
                weapons,
                assignments, 
                isWeaponAssigned,
                ref weaponsRemaining,
                _distanceSortedStorage,
                isThreatAssigned
            );

            // First pass: Assign remaining PDCs per torp by distance.
            // The other passes: overkill the torpedoes.
            bool isMakingAssignments;
            
            do
            {
                isMakingAssignments = false;
                
                for (var threatIndex = 0; threatIndex < threats.Count && weaponsRemaining > 0; threatIndex++)
                {
                    if (isThreatAssigned[threatIndex])
                    {
                        continue;
                    }

                    var weaponBlacklist = cannotShootByThreat[threatIndex];
                    var threatPosition = threats[threatIndex].Ref.Position;
                
                    // Finds the weapon can shoot the torp:
                    for (var candidateWeaponIndex = 0; candidateWeaponIndex < weaponsCount; candidateWeaponIndex++)
                    {
                        if (isWeaponAssigned[candidateWeaponIndex])
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
                        isThreatAssigned[threatIndex] = true;
                        assignments[candidateWeaponIndex] = threats[threatIndex].Ref;
                        isWeaponAssigned[candidateWeapon.Index] = true;
                        weaponsRemaining--;

                        isMakingAssignments = true;
                            
                        break;
                    }
                }

                Array.Clear(isThreatAssigned, 0, threats.Count);
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
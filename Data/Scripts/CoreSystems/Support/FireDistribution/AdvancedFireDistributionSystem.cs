using System;
using System.Collections.Generic;
using CoreSystems;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using VRageMath;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable InlineTemporaryVariable

namespace WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution
{
    internal abstract class AdvancedFireDistributionSystem : FireDistributionSystem
    {
        protected readonly ThreatMatrix Matrix = new ThreatMatrix();
        
        // Used during the assignment algorithm. Tells us if a threat has no more PDCs that can be assigned to it. 
        // ReSharper disable InconsistentNaming
        private int __assignmentAlgorithmThreatCount = -1;
        private bool[] __isThreatHandled;
        // ReSharper restore InconsistentNaming
        
        protected AdvancedFireDistributionSystem(FireDistributionManager manager) : base(manager)
        {
            
        }
        
        protected override void UpdateDataStructure(List<Projectile> projectileList, MyCubeGrid grid)
        {
            Matrix.UpdateDataStructure(projectileList, grid, Weapons);
        }

        protected override void ClearDataStructure()
        {
            Matrix.Clear();
        }
        
        /// <summary>
        ///     First, the torpedoes are prioritized by the specific system implementation using a (hopefully) time-coherent sort.
        ///     Then, the algorithm starts assigning PDCs to the torpedoes by priority, based on the cost of assignment.
        ///     The value of the PDCs will also determine how many are assigned.
        ///     This greedy cost problem assignment algorithm produces good results with very low cost, compared to an optimal assignment algorithm.
        ///     In turn, we can run it multiple times per frame as the expensive LoS checks inform us that the PDCs cannot shoot the assigned torpedoes (the real cost is building the matrix, which is done once for the frame).
        /// </summary>
        protected override void ComputeAssignments()
        { 
            ClearAssignmentState();
            
            var weapons = Weapons;
            var weaponsCount = Weapons.Count;
            var threats = Matrix.Threats;
            var isWeaponAssigned = IsWeaponAssignedToAnything;
            var assignments = Assignments;

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
                Matrix,
                isThreatAssigned
            );

            // First pass: Greedily assigns whatever we can, while integrating the game state.
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
                
                    var threat = threats[threatIndex];
                    var rowData = threat.RowData;
                
                    // Finds the weapon that can shoot the torp and has minimum cost.
                    var bestPdcIndex = -1;
                    var bestPdcCost = int.MaxValue;
                    for (var candidateWeaponIndex = 0; candidateWeaponIndex < weaponsCount; candidateWeaponIndex++)
                    {
                        if (isWeaponAssigned[candidateWeaponIndex])
                        {
                            continue;
                        }

                        var cell = rowData[candidateWeaponIndex];

                        if (cell == 0)
                        {
                            // Weapon cannot shoot the torp:
                            continue;
                        }

                        var cost = cell >> 1;

                        if (bestPdcIndex == -1 || cost < bestPdcCost)
                        {
                            bestPdcIndex = candidateWeaponIndex;
                            bestPdcCost = cost;
                        }
                    }

                    if (bestPdcIndex == -1)
                    {
                        continue;
                    }
                    
                    var candidateWeapon = weapons[bestPdcIndex];
                    
                    // And assigns it:
                    isThreatAssigned[threatIndex] = true;
                    assignments[bestPdcIndex] = threat.Ref;
                    isWeaponAssigned[candidateWeapon.Index] = true;
                    weaponsRemaining--;

                    isMakingAssignments = true;
                }

                Array.Clear(isThreatAssigned, 0, threats.Count);
            } while (isMakingAssignments && weaponsRemaining > 0);
        }

        protected override bool MarkCannotShootCore(LogicalWeapon weapon, Projectile projectile)
        {
            ThreatRow threat;
            if (!Matrix.ThreatsByProjectile.TryGetValue(projectile, out threat))
            {
                return false;
            }
                    
            threat.RowData[weapon.Index] = 0;

            return true;
        }
        
        protected sealed class ThreatRow : Threat
        {
            public ushort[] RowData;
        }

        protected sealed class ThreatMatrix : ThreatStorage<ThreatRow>
        {
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
            
            // ReSharper disable InconsistentNaming
            private int __matrixComputeLocalCacheSize = -1;
            private Vector3D[] __weaponPositions;
            private Vector3[] __weaponDirections;
            private float[] __weaponMaxSqrDists;
            private float[] __weaponTurnCosts;
            // ReSharper restore InconsistentNaming
            
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
                    weaponTurnCosts[weaponIndex] = weapon.Ref.Comp.MasterOverrides.TurnCost / (float)FireDistributionSupport.MaxTurnCost;
                    
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
                            
                            ushort quantizedCost = 0;
                            if (distanceToTargetSqr > 1.0)
                            {
                                var weaponDirection = weaponDirections[weaponIndex];
    
                                double u;
                                Vector3D.Dot(ref weaponDirection, ref dirToTarget, out u);

                                double angleFunction;
    
                                if (u > 0)
                                {
                                    // Maps [0, 90] degrees to cost [0, 1]
                                    angleFunction = 1.0 - u * u / distanceToTargetSqr;
                                }
                                else
                                {
                                    // Maps [90, 180] degrees to cost [1, 2]
                                    angleFunction = 1.0 + u * u / distanceToTargetSqr;
                                }

                                var cost = angleFunction * weaponTurnCosts[weaponIndex];

                                quantizedCost = (ushort) Math.Min(cost * 16383.5, 32767.0);
                            }

                            rowData[weaponIndex] = (ushort)((quantizedCost << 1) | 1);
                        }
                    }
                }
            }
            
            protected override ThreatRow CreateInstance(Projectile projectile, int weaponCount) => new ThreatRow
            {
                Ref = projectile,
                RowData = new ushort[weaponCount],
                DistanceToGridCenter = double.NaN
            };

            /// <summary>
            ///     Builds the N×M matrix, where N is the number of torpedoes and M is the number of weapons.
            ///     Each cell encodes whether the PDC can engage the torpedo, and the cost of doing so.
            /// </summary>
            /// <param name="projectileList"></param>
            /// <param name="grid"></param>
            /// <param name="weapons"></param>
            public override void UpdateDataStructure(List<Projectile> projectileList, MyCubeGrid grid, List<LogicalWeapon> weapons)
            {
                base.UpdateDataStructure(projectileList, grid, weapons);
                
                /*
                 * Zeroes out the matrix and resizes each row data array if needed.
                 */
                ResizeColumnsAndClearMatrix(weapons.Count);
                
                /*
                 * Computes the visibility and cost for each cell.
                 */
                ComputeMatrix(weapons, grid);
            }

            public override void Clear()
            {
                base.Clear();
                ClearBands();
            }
        }
    }
}
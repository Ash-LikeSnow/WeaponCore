using CoreSystems.Platform;

namespace WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution.Implementation
{
    /// <summary>
    ///     This is the actual bread and butter of saving the ship.
    ///     It acts as a me-first system. Torpedoes that are closest are obviously the biggest danger to the ship, so that's our threat heuristic.
    /// </summary>
    internal sealed class AdvancedClosestFireDistributionSystem : AdvancedFireDistributionSystem
    {
        /// <summary>
        ///     If true, when the threats outnumber the PDCs, the PDC value is ignored and one PDC is assigned to each threat. 
        /// </summary>
        //private readonly bool _preferFairness; // We'd need to determine if there are unengaged torpedoes exactly. It's a bit more difficult for now
        
        public AdvancedClosestFireDistributionSystem(FireDistributionManager manager) : base(manager)
        {
            
        }

        public override bool IsValidWeaponForSystem(Weapon weapon)
        {
            return FireDistributionManager.IsValidWeaponForFireDistribution(weapon) && (weapon.System.AllowSwitchTargetPriority ? weapon.Comp?.MasterOverrides?.TargetClosest ?? weapon.System.ClosestFirst : weapon.System.ClosestFirst);
        }

        /// <summary>
        ///     Sorts the threats by distance ascending (so, highest threat to the lowest threat).
        /// </summary>
        protected override void SetupTickStartCore()
        {
            // Another fucking local sort because the profiler would (probably) catch the comparer...
            var threats = Matrix.Threats;
            var count = threats.Count;
            // P.S. we should copy these into local
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
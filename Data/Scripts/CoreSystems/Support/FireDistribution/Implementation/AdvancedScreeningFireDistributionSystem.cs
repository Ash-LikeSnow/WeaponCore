using CoreSystems.Platform;

namespace WeaponCore.Data.Scripts.CoreSystems.Support.FireDistribution.Implementation
{
    /// <summary>
    ///     Fire distribution system that tries to distribute fire evenly, using seekers.
    /// </summary>
    internal sealed class AdvancedScreeningFireDistributionSystem : AdvancedFireDistributionSystem
    {
        public AdvancedScreeningFireDistributionSystem(FireDistributionManager manager) : base(manager)
        {
            
        }
        
        public override bool IsValidWeaponForSystem(Weapon weapon)
        {
            return FireDistributionManager.IsValidWeaponForFireDistribution(weapon) && !(weapon.System.AllowSwitchTargetPriority ? weapon.Comp?.MasterOverrides?.TargetClosest ?? weapon.System.ClosestFirst : weapon.System.ClosestFirst);
        }

        protected override void SetupTickStartCore()
        {
            // P.S. These values are probably not coherent in time, might be better to load up some arrays and call a framework sort
            var threats = Matrix.Threats;
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
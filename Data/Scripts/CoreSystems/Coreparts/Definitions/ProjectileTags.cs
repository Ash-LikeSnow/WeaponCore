using CoreSystems.Support;
using static CoreSystems.Support.ProjectileTagDefinition;
using static CoreSystems.Support.ProjectileTagAssignment;
using CoreSystems;

namespace Scripts
{
    partial class Parts
    {
        // Don't edit above this line
        ProjectileTagDefinition WCAutoAssignedTags => new ProjectileTagDefinition
        {
            Namespace = new Tag
            {
                ID = Session.WC_NAMESPACE,
                PublicName = "",
            },
            DefinitionPriority = int.MaxValue,
            Tags = new[]
            {
                new Tag 
                {
                    ID = Session.WC_DUMBTAG,
                    PublicName = "Dumb"
                },
                new Tag 
                {
                    ID = Session.WC_SMARTTAG,
                    PublicName = "Smart"
                },
                new Tag 
                {
                    ID = Session.WC_DRONETAG,
                    PublicName = "Drone"
                },
                new Tag
                {
                    ID = Session.WC_MINETAG,
                    PublicName = "Mine"
                },
                new Tag
                {
                    ID = Session.WC_TRAVELTOTAG,
                    PublicName = "Travel To"
                },
            },
        };
    }
}

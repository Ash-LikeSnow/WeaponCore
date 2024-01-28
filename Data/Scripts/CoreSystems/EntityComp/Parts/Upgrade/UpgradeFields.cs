using CoreSystems.Support;

namespace CoreSystems.Platform
{
    internal class Upgrades : Part
    {
        internal readonly Upgrade.UpgradeComponent Comp;
        internal ProtoUpgradePartState PartState;
        internal Upgrades(UpgradeSystem system, Upgrade.UpgradeComponent comp, int partId)
        {
            Comp = comp;
            Init(comp, system, partId);

            Log.Line($"init Upgrades: {system.PartName}");
        }
    }
}

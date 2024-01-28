using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.ModAPI;

namespace CoreSystems
{
    internal static partial class BlockUi
    {
        internal static bool GetShowArea(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as SupportSys.SupportComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return false;
            return comp.Data.Repo.Values.Set.Overrides.ArmorShowArea;

        }

        internal static void RequestSetShowArea(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.Components?.Get<CoreComponent>() as SupportSys.SupportComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            var value = newValue ? 1 : 0;
            SupportSys.SupportComponent.RequestSetValue(comp, "ArmorShowArea", value, Session.I.PlayerId);
        }
    }
}

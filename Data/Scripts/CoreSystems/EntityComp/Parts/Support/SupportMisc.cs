using System.Collections.Concurrent;
using VRage.Game.ModAPI;
using static CoreSystems.Support.SupportDefinition.SupportEffect.Protections;

namespace CoreSystems.Platform
{
    public partial class SupportSys
    {
        private ConcurrentDictionary<IMySlimBlock, SupportSys> GetSupportCollection()
        {
            switch (System.Values.Effect.Protection)
            {
                case EnergeticProt:
                case KineticProt:
                case GenericProt:
                    return Session.I.ProtSupports;
                case Regenerate:
                    return Session.I.RegenSupports;
                case Structural:
                    return Session.I.StructalSupports;
            }
            return null;
        }
    }
}

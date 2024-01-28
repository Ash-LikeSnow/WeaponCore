using Sandbox.Game.Entities;

namespace CoreSystems.Platform
{
    public partial class SupportSys
    {
        internal void Charge()
        {
            if (_charges > 0 && !Info.Idle)
                Info.Update(_charges);
        }
    }
}

using CoreSystems.Support;
using VRageMath;

namespace CoreSystems.Platform
{
    public partial class Part
    {
        internal void DrawPower(float assignedPower, Ai ai)
        {
            AssignedPower = MathHelper.Clamp(assignedPower, 0, DesiredPower);
            if (ai.ModOverride)
                return;

            BaseComp.SinkPower += AssignedPower;
            ai.GridAssignedPower += AssignedPower;
            Charging = true;

            if (BaseComp.CoreEntity.MarkedForClose)
                return;
            if (BaseComp.IsBlock)
                BaseComp.Cube.ResourceSink.Update();
        }

        internal void AdjustPower(float assignedPower, Ai ai)
        {
            if (ai.ModOverride) {
                AssignedPower = MathHelper.Clamp(assignedPower, 0, DesiredPower);
                return;
            }

            BaseComp.SinkPower -= AssignedPower;
            ai.GridAssignedPower -= AssignedPower;

            AssignedPower = MathHelper.Clamp(assignedPower, 0, DesiredPower);

            BaseComp.SinkPower += AssignedPower;
            ai.GridAssignedPower += AssignedPower;

            NewPowerNeeds = false;

            if (BaseComp.CoreEntity.MarkedForClose)
                return;

            if (BaseComp.IsBlock)
                BaseComp.Cube.ResourceSink.Update();
        }

        internal void StopPowerDraw(bool hardStop, Ai ai)
        {
            if (!Charging) {
                return;
            }

            BaseComp.SinkPower -= AssignedPower;
            ai.GridAssignedPower -= AssignedPower;
            AssignedPower = 0;

            if (BaseComp.SinkPower < BaseComp.IdlePower) BaseComp.SinkPower = BaseComp.IdlePower;
            Charging = false;

            if (BaseComp.CoreEntity.MarkedForClose)
                return;

            if (BaseComp.IsBlock)
                BaseComp.Cube.ResourceSink.Update();
        }


    }
}

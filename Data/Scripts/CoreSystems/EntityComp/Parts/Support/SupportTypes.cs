using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage.Utils;
using VRageMath;
namespace CoreSystems.Platform
{
    internal struct BlockBackup
    {
        internal MyCube MyCube;
        internal Vector3 OriginalColor;
        internal MyStringHash OriginalSkin;
    }

    internal class SupportInfo
    {
        internal readonly int[] RunningTotal = new int[60];
        internal int LastStep = 59;
        internal int TimeStep;
        internal int UsedThisSecond;
        internal int UsedLastMinute;
        internal int IdleTime;

        internal int MaxPoints;
        internal int PointsPerCharge;
        internal bool Idle;
        internal int CurrentPoints;

        internal void Update(int charges)
        {
            CurrentPoints = MathHelper.Clamp(CurrentPoints + (charges * PointsPerCharge), 0, MaxPoints);
            UsedLastMinute += UsedThisSecond;

            if (UsedThisSecond > 0 || CurrentPoints < MaxPoints)
            {
                IdleTime = 0;
                Idle = false;
            }
            else if (++IdleTime > 59)
            {
                Idle = true;
            }

            if (TimeStep > LastStep)
            {
                LastStep = TimeStep;
                UsedLastMinute -= RunningTotal[LastStep];
            }

            if (TimeStep < 59)
            {
                RunningTotal[TimeStep++] = UsedThisSecond;
            }
            else
            {
                TimeStep = 0;
                RunningTotal[TimeStep] = UsedThisSecond;
            }
            Log.Line($"used: {UsedLastMinute} - {UsedThisSecond}");
            UsedThisSecond = 0;
        }

    }
}

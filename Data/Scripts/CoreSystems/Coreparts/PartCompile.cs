using System.Collections.Generic;
using CoreSystems.Support;
using VRageMath;

namespace Scripts
{
    partial class Parts
    {
        internal ContainerDefinition Container = new ContainerDefinition();
        internal void PartDefinitions(params WeaponDefinition[] defs)
        {
            Container.WeaponDefs = defs;
        }

        internal void ArmorDefinitions(params ArmorDefinition[] defs)
        {
            Container.ArmorDefs = defs;
        }

        internal void SupportDefinitions(params SupportDefinition[] defs)
        {
            Container.SupportDefs = defs;
        }

        internal void UpgradeDefinitions(params UpgradeDefinition[] defs)
        {
            Container.UpgradeDefs = defs;
        }

        internal static void GetBaseDefinitions(out ContainerDefinition baseDefs)
        {
            baseDefs = new Parts().Container;
        }
        
        internal static void SetModPath(ContainerDefinition baseDefs, string modContext)
        {
            if (baseDefs.WeaponDefs != null)
                for (int i = 0; i < baseDefs.WeaponDefs.Length; i++)
                    baseDefs.WeaponDefs[i].ModPath = modContext;

            if (baseDefs.SupportDefs != null)
                for (int i = 0; i < baseDefs.SupportDefs.Length; i++)
                    baseDefs.SupportDefs[i].ModPath = modContext;

            if (baseDefs.UpgradeDefs != null)
                for (int i = 0; i < baseDefs.UpgradeDefs.Length; i++)
                    baseDefs.UpgradeDefs[i].ModPath = modContext;
        }

        internal WeaponDefinition.AmmoDef.Randomize Random(float start, float end)
        {
            return new WeaponDefinition.AmmoDef.Randomize { Start = start, End = end };
        }

        internal Vector4 Color(float red, float green, float blue, float alpha)
        {
            return new Vector4(red, green, blue, alpha);
        }

        internal Vector3D Vector(double x, double y, double z)
        {
            return new Vector3D(x, y, z);
        }

        internal WeaponDefinition.AnimationDef.RelMove.XYZ Transformation(double X, double Y, double Z)
        {
            return new WeaponDefinition.AnimationDef.RelMove.XYZ { x = X, y = Y, z = Z };
        }

        internal Dictionary<WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers, uint> Delays(uint FiringDelay = 0, uint ReloadingDelay = 0, uint OverheatedDelay = 0, uint TrackingDelay = 0, uint LockedDelay = 0, uint OnDelay = 0, uint OffDelay = 0, uint BurstReloadDelay = 0, uint OutOfAmmoDelay = 0, uint PreFireDelay = 0, uint StopFiringDelay = 0, uint StopTrackingDelay = 0, uint InitDelay = 0, uint HomingDelay = 0, uint TargetAlignedDelay = 0, uint WhileOnDelay = 0, uint TargetRanged100Delay = 0, uint TargetRanged75Delay = 0, uint TargetRanged50Delay = 0, uint TargetRanged25Delay = 0)
        {
            return new Dictionary<WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers, uint>
            {
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.Firing] = FiringDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.Reloading] = ReloadingDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.Overheated] = OverheatedDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.Tracking] = TrackingDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TurnOn] = OnDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TurnOff] = OffDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.BurstReload] = BurstReloadDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.NoMagsToLoad] = OutOfAmmoDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.PreFire] = PreFireDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.EmptyOnGameLoad] = 0,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.StopFiring] = StopFiringDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.StopTracking] = StopTrackingDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.LockDelay] = LockedDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.Init] = InitDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.Homing] = HomingDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TargetAligned] = TargetAlignedDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.WhileOn] = WhileOnDelay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TargetRanged100] = TargetRanged100Delay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TargetRanged75] = TargetRanged75Delay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TargetRanged50] = TargetRanged50Delay,
                [WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers.TargetRanged25] = TargetRanged25Delay,
            };
        }

        internal WeaponDefinition.AnimationDef.PartEmissive Emissive(string EmissiveName, bool CycleEmissiveParts, bool LeavePreviousOn, Vector4[] Colors, float IntensityFrom, float IntensityTo, string[] EmissivePartNames)
        {
            return new WeaponDefinition.AnimationDef.PartEmissive
            {
                EmissiveName = EmissiveName,
                Colors = Colors,
                CycleEmissivesParts = CycleEmissiveParts,
                LeavePreviousOn = LeavePreviousOn,
                EmissivePartNames = EmissivePartNames,
                IntensityRange = new[]{ IntensityFrom, IntensityTo }
            };
        }

        internal WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers[] Events(params WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers[] events)
        {
            return events;
        }

        internal string[] Names(params string[] names)
        {
            return names;
        }

        internal string[] AmmoRounds(params string[] names)
        {
            return names;
        }
    }
}

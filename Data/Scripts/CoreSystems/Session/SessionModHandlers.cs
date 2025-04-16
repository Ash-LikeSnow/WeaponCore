using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Utils;

namespace CoreSystems
{
    public partial class Session
    {
        public void Handler(object o)
        {
            try
            {
                var message = o as byte[];
                if (message == null) return;

                ContainerDefinition baseDefArray = null;
                try { baseDefArray = MyAPIGateway.Utilities.SerializeFromBinary<ContainerDefinition>(message); }
                catch (Exception e) {
                    // ignored
                }

                if (baseDefArray != null) {

                    PickDef(baseDefArray);
                }
                else {
                    var legacyArray = MyAPIGateway.Utilities.SerializeFromBinary<WeaponDefinition[]>(message);
                    if (legacyArray != null)
                        AssemblePartDefinitions(legacyArray);
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLine($"Exception in Handler: {ex}"); }
        }

        private void PickDef(ContainerDefinition baseDefArray)
        {
            if (baseDefArray.WeaponDefs != null)
                AssemblePartDefinitions(baseDefArray.WeaponDefs);

            if (baseDefArray.SupportDefs != null)
                AssemblePartDefinitions(baseDefArray.SupportDefs);

            if (baseDefArray.UpgradeDefs != null)
                AssemblePartDefinitions(baseDefArray.UpgradeDefs);

            if (baseDefArray.ArmorDefs != null)
                AssembleArmorDefinitions(baseDefArray.ArmorDefs);
        }

        public void AssemblePartDefinitions(WeaponDefinition[] partDefs)
        {
            if (DuplicateReplacer(partDefs))
                return;

            var subTypes = new HashSet<string>();
            foreach (var wepDef in partDefs)
            {
                WeaponDefinitions.Add(wepDef);

                for (int i = 0; i < wepDef.Assignments.MountPoints.Length; i++)
                    subTypes.Add(wepDef.Assignments.MountPoints[i].SubtypeId);
            }
            var group = MyStringHash.GetOrCompute("Charging");

            foreach (var def in AllDefinitions)
            {
                if (subTypes.Contains(def.Id.SubtypeName))
                {
                    if (def is MyLargeTurretBaseDefinition)
                    {
                        var weaponDef = def as MyLargeTurretBaseDefinition;
                        weaponDef.ResourceSinkGroup = group;
                    }
                    else if (def is MyConveyorSorterDefinition)
                    {
                        var weaponDef = def as MyConveyorSorterDefinition;
                        weaponDef.ResourceSinkGroup = group;
                    }
                }
            }
        }

        private int _replacerCount;
        private bool DuplicateReplacer(WeaponDefinition[] partDefs)
        {
            foreach (var wepDef in partDefs)
            {
                bool detected = false;
                foreach (var mount in wepDef.Assignments.MountPoints)
                {
                    if (VanillaSubtypes.Contains(mount.SubtypeId))
                    {
                        detected = true;
                        break;
                    }
                }

                if (detected)
                    return _replacerCount++ > 0;
            }

            return false;
        }

        public void AssemblePartDefinitions(UpgradeDefinition[] partDefs)
        {
            var subTypes = new HashSet<string>();
            foreach (var upgradeDef in partDefs)
            {
                UpgradeDefinitions.Add(upgradeDef);

                for (int i = 0; i < upgradeDef.Assignments.MountPoints.Length; i++)
                    subTypes.Add(upgradeDef.Assignments.MountPoints[i].SubtypeId);
            }
            var group = MyStringHash.GetOrCompute("Charging");

            foreach (var def in AllDefinitions)
            {
                if (subTypes.Contains(def.Id.SubtypeName))
                {
                    if (def is MyLargeTurretBaseDefinition)
                    {
                        var weaponDef = def as MyLargeTurretBaseDefinition;
                        weaponDef.ResourceSinkGroup = group;
                    }
                    else if (def is MyConveyorSorterDefinition)
                    {
                        var weaponDef = def as MyConveyorSorterDefinition;
                        weaponDef.ResourceSinkGroup = group;
                    }
                }
            }
        }

        public void AssemblePartDefinitions(SupportDefinition[] partDefs)
        {
            var subTypes = new HashSet<string>();
            foreach (var supportDef in partDefs)
            {
                SupportDefinitions.Add(supportDef);

                for (int i = 0; i < supportDef.Assignments.MountPoints.Length; i++)
                    subTypes.Add(supportDef.Assignments.MountPoints[i].SubtypeId);
            }
            var group = MyStringHash.GetOrCompute("Charging");

            foreach (var def in AllDefinitions)
            {
                if (subTypes.Contains(def.Id.SubtypeName))
                {
                    if (def is MyLargeTurretBaseDefinition)
                    {
                        var weaponDef = def as MyLargeTurretBaseDefinition;
                        weaponDef.ResourceSinkGroup = group;
                    }
                    else if (def is MyConveyorSorterDefinition)
                    {
                        var weaponDef = def as MyConveyorSorterDefinition;
                        weaponDef.ResourceSinkGroup = group;
                    }
                }
            }
        }

        public void AssembleArmorDefinitions(ArmorDefinition[] armorDefs)
        {
            foreach (var armorDef in armorDefs)
            {
                CoreSystemsArmorDefs.Add(armorDef);
                var values = new ResistanceValues();
                var resistanceEnabled = !((armorDef.KineticResistance == 0 && armorDef.EnergeticResistance == 0) || (armorDef.KineticResistance == 1 && armorDef.EnergeticResistance == 1));

                if (resistanceEnabled)
                {
                    values.EnergeticResistance = armorDef.EnergeticResistance > 0.0001 ? (float)armorDef.EnergeticResistance : 1;
                    values.KineticResistance = armorDef.KineticResistance > 0.0001 ? (float)armorDef.KineticResistance : 1;

                    ArmorCoreActive = true;
                }

                foreach (var subtype in armorDef.SubtypeIds)
                {
                    var type = MyStringHash.GetOrCompute(subtype);

                    if (armorDef.Kind == ArmorDefinition.ArmorType.Heavy)
                    {
                        CustomArmorSubtypes.Add(type);
                        CustomHeavyArmorSubtypes.Add(type);
                    }
                    else if (armorDef.Kind == ArmorDefinition.ArmorType.Light)
                    {
                        CustomArmorSubtypes.Add(type);
                    }

                    if (resistanceEnabled) ArmorCoreBlockMap.Add(type, values);
                }
            }
        }
    }
}

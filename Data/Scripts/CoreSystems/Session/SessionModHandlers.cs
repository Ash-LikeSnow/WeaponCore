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
                    {
                        var subTypes = new HashSet<string>();
                        AssemblePartDefinitions(legacyArray, subTypes);

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
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLine($"Exception in Handler: {ex}"); }
        }

        private void PickDef(ContainerDefinition baseDefArray)
        {
            var subTypes = new HashSet<string>();

            if (baseDefArray.WeaponDefs != null)
                AssemblePartDefinitions(baseDefArray.WeaponDefs, subTypes);

            if (baseDefArray.SupportDefs != null)
                AssemblePartDefinitions(baseDefArray.SupportDefs, subTypes);

            if (baseDefArray.UpgradeDefs != null)
                AssemblePartDefinitions(baseDefArray.UpgradeDefs, subTypes);

            if (baseDefArray.ArmorDefs != null)
                AssembleArmorDefinitions(baseDefArray.ArmorDefs);

            if (baseDefArray.ProjectileTags != null)
                AssembleTagDefinitions(baseDefArray.ProjectileTags);

            if (baseDefArray.TagAssigmnents != null)
                AssembleTagAssignments(baseDefArray.TagAssigmnents);

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

        public void AssembleTagDefinitions(ProjectileTagDefinition[] arr)
        {
            foreach (var def in arr)
            {
                if (def == null)
                    continue;

                var nsp = def.Namespace.ID;

                if (string.IsNullOrEmpty(nsp) || def.Tags == null || def.Tags.Length == 0)
                    continue;

                ProjectileTagDefinition prio;
                if (!ProjectileTagDefs.TryGetValue(nsp, out prio) || prio.DefinitionPriority < def.DefinitionPriority)
                {
                    ProjectileTagDefs[nsp] = def;
                }
            }
        }

        public void AssembleTagAssignments(ProjectileTagAssignment[] arr)
        {
            foreach (var assignment in arr)
            {
                foreach (var ammo in assignment.ProjectileAmmoNames)
                {
                    HashSet<string> tags;
                    if (AmmoTags.TryGetValue(ammo, out tags))
                    {
                        tags.Add(assignment.Tag);
                    }
                    else
                    {
                        AmmoTags[ammo] = new HashSet<string>() { assignment.Tag };
                    }
                }
            }
        }

        public void AssemblePartDefinitions(WeaponDefinition[] partDefs, HashSet<string> subTypes)
        {
            foreach (var wepDef in partDefs)
            {
                WeaponDefinitions.Add(wepDef);

                for (int i = 0; i < wepDef.Assignments.MountPoints.Length; i++)
                    subTypes.Add(wepDef.Assignments.MountPoints[i].SubtypeId);
            }
        }

        public void AssemblePartDefinitions(UpgradeDefinition[] partDefs, HashSet<string> subTypes)
        {
            foreach (var upgradeDef in partDefs)
            {
                int prevPrio = int.MinValue;
                UpgradeDefinition prevDef = null;
                foreach (var def in UpgradeDefinitions)
                {
                    if (def.HardPoint.PartName == upgradeDef.HardPoint.PartName)
                    {
                        prevPrio = def.HardPoint.DefinitionPriority;
                        prevDef = def;
                        break;
                    }
                }
                if (prevPrio >= upgradeDef.HardPoint.DefinitionPriority)
                {
                    continue;
                }
                if (prevDef != null)
                    UpgradeDefinitions.Remove(prevDef);

                UpgradeDefinitions.Add(upgradeDef);

                for (int i = 0; i < upgradeDef.Assignments.MountPoints.Length; i++)
                    subTypes.Add(upgradeDef.Assignments.MountPoints[i].SubtypeId);
            }
        }

        public void AssemblePartDefinitions(SupportDefinition[] partDefs, HashSet<string> subTypes)
        {
            foreach (var supportDef in partDefs)
            {
                int prevPrio = int.MinValue;
                SupportDefinition prevDef = null;
                foreach (var def in SupportDefinitions)
                {
                    if (def.HardPoint.PartName == supportDef.HardPoint.PartName)
                    {
                        prevPrio = def.HardPoint.DefinitionPriority;
                        prevDef = def;
                        break;
                    }
                }
                if (prevPrio >= supportDef.HardPoint.DefinitionPriority)
                {
                    continue;
                }
                if (prevDef != null)
                    SupportDefinitions.Remove(prevDef);

                SupportDefinitions.Add(supportDef);

                for (int i = 0; i < supportDef.Assignments.MountPoints.Length; i++)
                    subTypes.Add(supportDef.Assignments.MountPoints[i].SubtypeId);
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

                    int prevPrio;
                    if (ArmorCorePriorityMap.TryGetValue(type, out prevPrio));
                        prevPrio = int.MinValue;

                    if (prevPrio >= armorDef.DefinitionPriority)
                    {
                        continue;
                    }

                    if (armorDef.Kind == ArmorDefinition.ArmorType.Heavy)
                    {
                        CustomArmorSubtypes.Add(type);
                        CustomHeavyArmorSubtypes.Add(type);
                    }
                    else if (armorDef.Kind == ArmorDefinition.ArmorType.Light)
                    {
                        CustomArmorSubtypes.Add(type);

                        if (prevPrio != int.MinValue) // previous definition existed
                            CustomHeavyArmorSubtypes.Remove(type);
                    }
                    else if (prevPrio != int.MinValue)
                    {
                        CustomArmorSubtypes.Remove(type);
                        CustomHeavyArmorSubtypes.Remove(type);
                    }
                    ArmorCorePriorityMap[type] = armorDef.DefinitionPriority;
                    if (resistanceEnabled) ArmorCoreBlockMap[type] = values;
                }
            }
        }
    }
}

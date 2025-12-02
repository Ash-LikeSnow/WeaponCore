using CoreSystems.Platform;
using Sandbox.Definitions;
using Sandbox.Game;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using static CoreSystems.CompData;
namespace CoreSystems.Support
{
    public partial class CoreComponent
    {
        private void PowerInit()
        {
            Cube.ResourceSink.SetRequiredInputFuncByType(GId, () => Cube.IsWorking ? SinkPower : 0);
            Cube.ResourceSink.SetMaxRequiredInputByType(GId, 0);

            Cube.ResourceSink.Update();
        }

        private void StorageSetup()
        {
            if (CoreEntity.Storage == null)
                BaseData.StorageInit();

            BaseData.DataManager(DataState.Load);

            if (Session.I.IsServer)
                BaseData.DataManager(DataState.Reset);
        }

        private void InventoryInit()
        {
            using (InventoryEntity.Pin())
            {
                if (InventoryInited || !InventoryEntity.HasInventory || InventoryEntity.MarkedForClose || (Platform.State != CorePlatform.PlatformState.Inited && Platform.State != CorePlatform.PlatformState.Incomplete) || CoreInventory == null)
                {
                    Platform.PlatformCrash(this, false, true, $"InventoryInit failed: IsInitted:{InventoryInited} - NoInventory:{!InventoryEntity.HasInventory} - Marked:{InventoryEntity.MarkedForClose} - PlatformNotReady:{Platform.State != CorePlatform.PlatformState.Ready}({Platform.State}) - nullInventory:{CoreInventory == null}");
                    return;
                }

                if (TypeSpecific == CompTypeSpecific.Rifle)
                {
                    InventoryInited = true;
                    return;
                }
                var constraintList = new List<MyDefinitionId>();
                var constraintNames = new List<string>();
                if (Type == CompType.Weapon)
                {
                    var collect = TypeSpecific != CompTypeSpecific.Phantom ? Platform.Weapons : Platform.Phantoms;
                    for (int i = 0; i < collect.Count; i++)
                    {
                        var w = collect[i];

                        if (w == null)
                        {
                            Log.Line("InventoryInit weapon null");
                            continue;
                        }
                        for (int j = 0; j < w.System.AmmoTypes.Length; j++)
                        {
                            var ammo = w.System.AmmoTypes[j];
                            if (ammo.AmmoDef.Const.MagazineDef != null)
                            {
                                constraintList.Add(ammo.AmmoDef.Const.MagazineDef.Id);
                                if (ammo.AmmoDef.HardPointUsable && !constraintNames.Contains(ammo.AmmoDef.Const.MagazineDef.DisplayNameText))
                                    constraintNames.Add(ammo.AmmoDef.Const.MagazineDef.DisplayNameText);
                            }
                        }
                    }
                }

                var constraintName = "Ammo:";
                constraintNames.Sort();
                foreach (var name in constraintNames)
                    constraintName += "\n  - " + name;

                //if (InventoryEntity is IMyConveyorSorter || CoreInventory.Constraint == null)
                CoreInventory.Constraint = new MyInventoryConstraint(constraintName);

                var wepDef = MyDefinitionManager.Static.GetCubeBlockDefinition(Id) as MyWeaponBlockDefinition;
                if (wepDef != null)
                    CoreInventory.MaxVolume = (MyFixedPoint)wepDef.InventoryMaxVolume;                
                
                CoreInventory.Constraint.m_useDefaultIcon = false;
                CoreInventory.Refresh();
                CoreInventory.Constraint.Clear();

                if (!string.IsNullOrEmpty(CustomIcon)) {
                    var iconPath = Platform.Structure.ModPath + "\\Textures\\GUI\\Icons\\" + CustomIcon;
                    CoreInventory.Constraint.Icon = iconPath;
                    CoreInventory.Constraint.UpdateIcon();
                }

                foreach (var constraint in constraintList)
                    CoreInventory.Constraint.Add(constraint);

                CoreInventory.Refresh();

                InventoryInited = true;
            }
        }
    }
}

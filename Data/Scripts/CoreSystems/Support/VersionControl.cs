using System;
using System.Collections.Generic;
using System.IO;
using CoreSystems;
using CoreSystems.Settings;
using CoreSystems.Support;
using Sandbox.ModAPI;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    internal class VersionControl
    {
        public CoreSettings Core;
        private readonly Dictionary<WeaponDefinition.AmmoDef, Dictionary<string,string>> _tmpAmmoModiferMap = new Dictionary<WeaponDefinition.AmmoDef, Dictionary<string, string>>();
        private readonly Dictionary<WeaponDefinition, Dictionary<string, string>> _tmpWeaponModiferMap = new Dictionary<WeaponDefinition, Dictionary<string, string>>();
        public bool VersionChange;
        public VersionControl(CoreSettings core)
        {
            Core = core;
        }

        public void InitSettings()
        {
            if (MyAPIGateway.Utilities.FileExistsInGlobalStorage(Session.ClientCfgName))
            {

                var writer = MyAPIGateway.Utilities.ReadFileInGlobalStorage(Session.ClientCfgName);
                var xmlData = MyAPIGateway.Utilities.SerializeFromXML<CoreSettings.ClientSettings>(writer.ReadToEnd());
                writer.Dispose();

                if (xmlData?.Version == Session.ClientCfgVersion)
                {

                    Core.ClientConfig = xmlData;
                    Core.Session.UiInput.ControlKey = Core.Session.KeyMap[xmlData.ControlKey];
                    Core.Session.UiInput.ActionKey = Core.Session.KeyMap[xmlData.ActionKey];
                    Core.Session.UiInput.MouseButtonMenu = Core.Session.MouseMap[xmlData.MenuButton];
                    Core.Session.UiInput.InfoKey = Core.Session.KeyMap[xmlData.InfoKey];
                    Core.Session.UiInput.CycleNextKey = Core.Session.KeyMap[xmlData.CycleNextKey];
                    Core.Session.UiInput.CyclePrevKey = Core.Session.KeyMap[xmlData.CyclePrevKey];
                }
                else
                    WriteNewClientCfg();
            }
            else WriteNewClientCfg();

            if (Core.Session.IsServer)
            {

                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(Session.ServerCfgName, typeof(CoreSettings.ServerSettings)))
                {

                    var writer = MyAPIGateway.Utilities.ReadFileInWorldStorage(Session.ServerCfgName, typeof(CoreSettings.ServerSettings));

                    CoreSettings.ServerSettings xmlData = null;

                    try { xmlData = MyAPIGateway.Utilities.SerializeFromXML<CoreSettings.ServerSettings>(writer.ReadToEnd()); }
                    catch (Exception e) { writer.Dispose(); }

                    writer.Dispose();

                    if (xmlData?.Version == Session.ModVersion)
                    {
                        Core.Enforcement = xmlData;
                        CorruptionCheck(true);
                    }
                    else
                        GenerateConfig(xmlData);
                }
                else GenerateConfig();


                GenerateBlockDmgMap();
                GenerateWeaponValuesMap();
                GenerateAmmoValuesMap();
            }

            if (VersionChange)
            {
                Core.Session.PlayerStartMessage = true;
                Core.Session.PlayerMessage = "You may access WeaponCore client settings with the /wc chat command\n- for helpful tips goto: https://github.com/Ash-LikeSnow/WeaponCore/wiki/Player-Tips";
            }
        }

        public void UpdateClientEnforcements(CoreSettings.ServerSettings data)
        {
            Core.Enforcement = data;
            Core.ClientWaiting = false;
            GenerateBlockDmgMap();
            GenerateWeaponValuesMap();
            GenerateAmmoValuesMap();
            Core.Session.AdvSync = Core.Enforcement.AdvancedProjectileSync && Core.Session.MpActive;
            Core.Session.AdvSyncClient = Core.Session.AdvSync;
        }

        private void GenerateConfig(CoreSettings.ServerSettings oldSettings = null)
        {

            if (oldSettings != null) RebuildConfig(oldSettings);
            else
                Core.Enforcement = new CoreSettings.ServerSettings { Version = Session.ModVersion };

            CorruptionCheck();
            SaveServerCfg();
            VersionChange = true;
        }

        private void WriteNewClientCfg()
        {
            VersionChange = true;
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(Session.ClientCfgName);
            Core.ClientConfig = new CoreSettings.ClientSettings { Version = Session.ClientCfgVersion };
            var writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(Session.ClientCfgName);
            var data = MyAPIGateway.Utilities.SerializeToXML(Core.ClientConfig);
            Write(writer, data);
        }

        internal void UpdateClientCfgFile()
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(Session.ClientCfgName);
            var writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(Session.ClientCfgName);
            var data = MyAPIGateway.Utilities.SerializeToXML(Core.ClientConfig);
            Write(writer, data);
        }

        public void SaveServerCfg()
        {
            MyAPIGateway.Utilities.DeleteFileInWorldStorage(Session.ServerCfgName, typeof(CoreSettings.ServerSettings));
            var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(Session.ServerCfgName, typeof(CoreSettings.ServerSettings));
            var data = MyAPIGateway.Utilities.SerializeToXML(Core.Enforcement);
            Write(writer, data);
        }

        private static void Write(TextWriter writer, string data)
        {
            writer.Write(data);
            writer.Flush();
            writer.Dispose();
        }

        private void RebuildConfig(CoreSettings.ServerSettings oldSettings)
        {
            var oldModifers = oldSettings.ServerModifiers;
            var oldBlockModifers = oldSettings.BlockModifers;
            var oldShipSizes = oldSettings.ShipSizes;
            var oldSleep = oldSettings.ServerSleepSupport;
            var oldBaseOptimize = oldSettings.BaseOptimizations;
            var oldAdvancedOptimize = oldSettings.AdvancedOptimizations;

            var oldFocusDist = oldSettings.MinHudFocusDistance;
            var oldMaxFocusDist = oldSettings.MaxHudFocusDistance;
            var oldDisableAi = oldSettings.DisableAi;
            var oldDisableLeads = oldSettings.DisableLeads;
            var oldDisableHudReload = oldSettings.DisableHudReload;
            var oldDisableHudTargetInfo = oldSettings.DisableHudTargetInfo;
            var oldPointDefenseSyncMonitor = oldSettings.AdvancedProjectileSync;
            var oldUnsupportedMode = oldSettings.UnsupportedMode;
            var oldDisableSmallVsLargeBuff = oldSettings.DisableSmallVsLargeBuff;
            Core.Enforcement = new CoreSettings.ServerSettings { Version = Session.ModVersion };

            if (oldModifers != null)
                Core.Enforcement.ServerModifiers = oldModifers;

            if (oldBlockModifers != null)
                Core.Enforcement.BlockModifers = oldBlockModifers;

            if (oldShipSizes != null)
                Core.Enforcement.ShipSizes = oldShipSizes;

            Core.Enforcement.ServerSleepSupport = oldSleep;

            Core.Enforcement.BaseOptimizations = oldBaseOptimize;
            Core.Enforcement.AdvancedOptimizations = oldAdvancedOptimize;

            Core.Enforcement.MinHudFocusDistance = oldFocusDist;
            Core.Enforcement.MaxHudFocusDistance = oldMaxFocusDist;
            Core.Enforcement.DisableAi = oldDisableAi;
            Core.Enforcement.DisableLeads = oldDisableLeads;
            Core.Enforcement.DisableHudReload = oldDisableHudReload;
            Core.Enforcement.DisableHudTargetInfo = oldDisableHudTargetInfo;
            Core.Enforcement.AdvancedProjectileSync = oldPointDefenseSyncMonitor;
            Core.Enforcement.UnsupportedMode = oldUnsupportedMode;
            Core.Enforcement.DisableSmallVsLargeBuff = oldDisableSmallVsLargeBuff;
        }

        private void CorruptionCheck(bool write = false)
        {
            if (Core.Enforcement.AreaDamageModifer < 0)
                Core.Enforcement.AreaDamageModifer = 1f;

            if (Core.Enforcement.DirectDamageModifer < 0)
                Core.Enforcement.DirectDamageModifer = 1f;

            if (Core.Enforcement.ShieldDamageModifer < 0)
                Core.Enforcement.ShieldDamageModifer = 1f;

            if (Core.Enforcement.ShipSizes == null)
            {
                Core.Enforcement.ShipSizes = Array.Empty<CoreSettings.ServerSettings.ShipSize>();
            }

            if (Core.Enforcement.BlockModifers == null)
            {
                Core.Enforcement.BlockModifers = new[]
                {
                    new CoreSettings.ServerSettings.BlockModifer { SubTypeId = "TestSubId1", DirectDamageModifer = 0.5f, AreaDamageModifer = 0.1f },
                    new CoreSettings.ServerSettings.BlockModifer { SubTypeId = "TestSubId2", DirectDamageModifer = -1f, AreaDamageModifer = 0f }
                };
            }

            if (Core.Enforcement.ServerModifiers == null)
            {
                Core.Enforcement.ServerModifiers = new CoreSettings.ServerSettings.Modifiers
                {
                    Ammos = new[] {
                    new CoreSettings.ServerSettings.AmmoMod { AmmoName = "AmmoRound1", Variable = "BaseDamage", Value = "1" },
                    new CoreSettings.ServerSettings.AmmoMod { AmmoName = "AmmoRound1", Variable = "AreaDamageType", Value = "Kinetic" },
                    new CoreSettings.ServerSettings.AmmoMod { AmmoName = "AmmoRound2", Variable = "DesiredSpeed", Value = "750" } 
                    },
                    Weapons = new[]
                    {
                    new CoreSettings.ServerSettings.WeaponMod {PartName = "PartName1", Variable = "MaxTargetDistance", Value = "1500"},
                    new CoreSettings.ServerSettings.WeaponMod {PartName = "PartName2", Variable = "DeviateShotAngle", Value = "0.25"},
                    new CoreSettings.ServerSettings.WeaponMod {PartName = "PartName2", Variable = "AimingTolerance", Value = "0.1"},
                    },
                };
            }

            if (write)
                SaveServerCfg();
        }


        private void GenerateBlockDmgMap()
        {
            if (Core.Enforcement.BlockModifers == null)
                return;

            foreach (var def in Core.Session.AllDefinitions)
            {
                foreach (var blockModifer in Core.Enforcement.BlockModifers)
                {
                    if ((blockModifer.AreaDamageModifer >= 0 || blockModifer.DirectDamageModifer >= 0) && def.Id.SubtypeId.String == blockModifer.SubTypeId)
                    {
                        Core.Session.GlobalDamageModifed = true;
                        Core.Session.BlockDamageMap[def] = new Session.BlockDamage { DirectModifer = blockModifer.DirectDamageModifer >= 0 ? blockModifer.DirectDamageModifer : 1, AreaModifer = blockModifer.AreaDamageModifer >= 0 ? blockModifer.AreaDamageModifer : 1 };
                    }
                }
            }
        }

        private void GenerateWeaponValuesMap()
        {
            if (Core.Enforcement.ServerModifiers.Weapons == null)
                return;

            foreach (var mod in Core.Enforcement.ServerModifiers.Weapons)
                foreach (var pair in Core.Session.WeaponValuesMap)
                    if (mod.PartName == pair.Key.HardPoint.PartName)
                    {
                        if (_tmpWeaponModiferMap.ContainsKey(pair.Key))
                            _tmpWeaponModiferMap[pair.Key].Add(mod.Variable, mod.Value);
                        else
                            _tmpWeaponModiferMap[pair.Key] = new Dictionary<string, string>() { { mod.Variable, mod.Value } };
                    }

            foreach (var t in _tmpWeaponModiferMap)
                Core.Session.WeaponValuesMap[t.Key] = t.Value;

            _tmpWeaponModiferMap.Clear();
        }

        private void GenerateAmmoValuesMap()
        {
            if (Core.Enforcement.ServerModifiers.Ammos == null)
                return;

            foreach (var mod in Core.Enforcement.ServerModifiers.Ammos)
                foreach (var pair in Core.Session.AmmoValuesMap)
                    if (mod.AmmoName == pair.Key.AmmoRound)
                    {
                        if (_tmpAmmoModiferMap.ContainsKey(pair.Key))
                            _tmpAmmoModiferMap[pair.Key].Add(mod.Variable, mod.Value);
                        else
                            _tmpAmmoModiferMap[pair.Key] = new Dictionary<string, string>() { { mod.Variable, mod.Value } };
                    }

            foreach (var t in _tmpAmmoModiferMap)
                Core.Session.AmmoValuesMap[t.Key] = t.Value;

            _tmpAmmoModiferMap.Clear();
        }

    }
}

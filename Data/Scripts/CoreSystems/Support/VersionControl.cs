using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreSystems;
using CoreSystems.Settings;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRage.Utils;
using static VRage.Game.MyObjectBuilder_SessionComponentMission;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    internal class VersionControl
    {
        public CoreSettings Core;
        private readonly Dictionary<WeaponDefinition.AmmoDef, CoreSettings.ServerSettings.AmmoOverride> _tmpAmmoModiferMap = new Dictionary<WeaponDefinition.AmmoDef, CoreSettings.ServerSettings.AmmoOverride>();
        private readonly Dictionary<WeaponDefinition, CoreSettings.ServerSettings.WeaponOverride> _tmpWeaponModiferMap = new Dictionary<WeaponDefinition, CoreSettings.ServerSettings.WeaponOverride>();
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
            var oldBlockModifers = oldSettings.BlockModifers;
            var oldShipSizes = oldSettings.ShipSizes;
            var oldOverrides = oldSettings.DefinitionOverrides;

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

            if (oldSettings.ServerModifiers?.Weapons != null)
                RemapLegacyWeaponValues(oldSettings.ServerModifiers.Weapons);

            if (oldSettings.ServerModifiers?.Ammos != null)
                RemapLegacyAmmoValues(oldSettings.ServerModifiers.Ammos);

            if (oldBlockModifers != null)
                Core.Enforcement.BlockModifers = oldBlockModifers;

            if (oldShipSizes != null)
                Core.Enforcement.ShipSizes = oldShipSizes;

            if (oldOverrides != null)
                Core.Enforcement.DefinitionOverrides = oldOverrides;

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
            Core.Enforcement.LargeGridDamageMultiplier = 1f;
            Core.Enforcement.SmallGridDamageMultiplier = 1f;
        }

        private void CorruptionCheck(bool write = false)
        {
            if (Core.Enforcement.AreaDamageModifer < 0)
                Core.Enforcement.AreaDamageModifer = 1f;

            if (Core.Enforcement.DirectDamageModifer < 0)
                Core.Enforcement.DirectDamageModifer = 1f;

            if (Core.Enforcement.ShieldDamageModifer < 0)
                Core.Enforcement.ShieldDamageModifer = 1f;

            if (Core.Enforcement.LargeGridDamageMultiplier < 0)
                Core.Enforcement.LargeGridDamageMultiplier = 1f;

            if (Core.Enforcement.SmallGridDamageMultiplier < 0)
                Core.Enforcement.SmallGridDamageMultiplier = 1f;

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

            if (Core.Enforcement.DefinitionOverrides == null)
            {
                Core.Enforcement.DefinitionOverrides = new CoreSettings.ServerSettings.Overrides
                {
                    AmmoOverrides = new[]
                    {
                        new CoreSettings.ServerSettings.AmmoOverride { AmmoName = "AmmoRound1", BaseDamage = 1f, EnergyBaseDamage = true },
                        new CoreSettings.ServerSettings.AmmoOverride { AmmoName = "AmmoRound2", AreaEffectDamage = 100f, AreaEffectRadius = 2.5, EnergyAreaEffectDamage = false },
                    },
                    WeaponOverrides = new[]
                    {
                        new CoreSettings.ServerSettings.WeaponOverride { PartName = "PartName1", RateOfFire = 600 },
                        new CoreSettings.ServerSettings.WeaponOverride { PartName = "PartName2", AimingTolerance = 10, DeviateShotAngle = 5f },
                    },
                    ArmorOverrides = new[]
                    {
                        new CoreSettings.ServerSettings.ArmorOverride { SubtypeIds = new[] { "Subtype1", "Subtype2" }, EnergeticResistance = 0.35f, KineticResistance = 1f },
                        new CoreSettings.ServerSettings.ArmorOverride { SubtypeIds = new[] { "Subtype3" }, KineticResistance = 0.9f },
                    }
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
            if (Core.Enforcement.DefinitionOverrides?.WeaponOverrides == null)
                return;

            foreach (var wepOverride in Core.Enforcement.DefinitionOverrides.WeaponOverrides)
            {
                foreach (var wepDef in Core.Session.WeaponValuesMap.Keys)
                {
                    if (!wepOverride.PartName.Equals(wepDef.HardPoint.PartName))
                        continue;

                    if (!_tmpWeaponModiferMap.ContainsKey(wepDef))
                        _tmpWeaponModiferMap[wepDef] = wepOverride;
                }
            }

            foreach (var pair in _tmpWeaponModiferMap)
            {
                Core.Session.WeaponValuesMap[pair.Key] = pair.Value;
            }
            _tmpWeaponModiferMap.Clear();
        }

        private void GenerateAmmoValuesMap()
        {
            if (Core.Enforcement.DefinitionOverrides?.AmmoOverrides == null)
                return;

            foreach (var ammoOverride in Core.Enforcement.DefinitionOverrides.AmmoOverrides)
            {
                foreach (var ammoDef in Core.Session.AmmoValuesMap.Keys)
                {
                    if (!ammoOverride.AmmoName.Equals(ammoDef.AmmoRound))
                        continue;

                    if (!_tmpAmmoModiferMap.ContainsKey(ammoDef))
                        _tmpAmmoModiferMap[ammoDef] = ammoOverride;
                }
            }

            foreach (var pair in _tmpAmmoModiferMap)
            {
                Core.Session.AmmoValuesMap[pair.Key] = pair.Value;
            }
            _tmpAmmoModiferMap.Clear();
        }

        private void RemapLegacyAmmoValues(CoreSettings.ServerSettings.AmmoMod[] modifiers)
        {
            var overrides = new Dictionary<string, CoreSettings.ServerSettings.AmmoOverride>();
            foreach (var modifier in modifiers)
            {
                CoreSettings.ServerSettings.AmmoOverride ammoOverride;
                if (!overrides.TryGetValue(modifier.AmmoName, out ammoOverride))
                {
                    ammoOverride = new CoreSettings.ServerSettings.AmmoOverride();
                    ammoOverride.AmmoName = modifier.AmmoName;
                    overrides.Add(modifier.AmmoName, ammoOverride);
                }

                float floatValue;
                int intValue;
                bool boolValue;
                switch (modifier.Variable)
                {
                    case "BaseDamage":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.BaseDamage = floatValue;
                        break;
                    case "AreaEffectDamage":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.AreaEffectDamage = floatValue;
                        break;
                    case "AreaEffectRadius":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.AreaEffectRadius = (double)floatValue;
                        break;
                    case "DetonationDamage":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.DetonationDamage = floatValue;
                        break;
                    case "DetonationRadius":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.DetonationRadius = floatValue;
                        break;
                    case "Health":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.Health = floatValue;
                        break;
                    case "MaxTrajectory":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.MaxTrajectory = floatValue;
                        break;
                    case "DesiredSpeed":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.DesiredSpeed = floatValue;
                        break;
                    case "EnergyCost":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.EnergyCost = floatValue;
                        break;
                    case "GravityMultiplier":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.GravityMultiplier = floatValue;
                        break;
                    case "ShieldModifier":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.ShieldModifier = (double)floatValue;
                        break;
                    case "EnergyBaseDamage":
                        if (bool.TryParse(modifier.Value, out boolValue))
                            ammoOverride.EnergyBaseDamage = boolValue;
                        break;
                    case "EnergyAreaEffectDamage":
                        if (bool.TryParse(modifier.Value, out boolValue))
                            ammoOverride.EnergyAreaEffectDamage = boolValue;
                        break;
                    case "EnergyDetonationDamage":
                        if (bool.TryParse(modifier.Value, out boolValue))
                            ammoOverride.EnergyDetonationDamage = boolValue;
                        break;
                    case "EnergyShieldDamage":
                        if (bool.TryParse(modifier.Value, out boolValue))
                            ammoOverride.EnergyShieldDamage = boolValue;
                        break;
                    case "DisableClientPredictedAmmo":
                        if (bool.TryParse(modifier.Value, out boolValue))
                            ammoOverride.DisableClientPredictedAmmo = boolValue;
                        break;
                    case "FallOffDistance":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.FallOffDistance = floatValue;
                        break;
                    case "FallOffMinMultipler":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.FallOffMinMultipler = floatValue;
                        break;
                    case "ShieldBypass":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.ShieldBypass = floatValue;
                        break;
                    case "Mass":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.Mass = floatValue;
                        break;
                    case "HealthHitModifier":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.HealthHitModifier = (double)floatValue;
                        break;
                    case "ByBlockHitMaxAbsorb":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.ByBlockHitMaxAbsorb = floatValue;
                        break;
                    case "EndOfLifeMaxAbsorb":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.EndOfLifeMaxAbsorb = floatValue;
                        break;
                    case "BackKickForce":
                        if (float.TryParse(modifier.Value, out floatValue))
                            ammoOverride.BackKickForce = floatValue;
                        break;
                }
            }

            if (Core.Enforcement.DefinitionOverrides == null)
                Core.Enforcement.DefinitionOverrides = new CoreSettings.ServerSettings.Overrides();

            Core.Enforcement.DefinitionOverrides.AmmoOverrides = overrides.Values.ToArray();
        }

        private void RemapLegacyWeaponValues(CoreSettings.ServerSettings.WeaponMod[] modifiers)
        {
            var overrides = new Dictionary<string, CoreSettings.ServerSettings.WeaponOverride>();
            foreach (var modifier in modifiers)
            {
                CoreSettings.ServerSettings.WeaponOverride wepOverride;
                if (!overrides.TryGetValue(modifier.PartName, out wepOverride))
                {
                    wepOverride = new CoreSettings.ServerSettings.WeaponOverride();
                    wepOverride.PartName = modifier.PartName;
                    overrides.Add(modifier.PartName, wepOverride);
                }

                float floatValue;
                int intValue;
                switch (modifier.Variable)
                {
                    case "MaxTargetDistance":
                        if (float.TryParse(modifier.Value, out floatValue))
                            wepOverride.MaxTargetDistance = floatValue;
                        break;
                    case "MinTargetDistance":
                        if (float.TryParse(modifier.Value, out floatValue))
                            wepOverride.MinTargetDistance = floatValue;
                        break;
                    case "RateOfFire":
                        if (int.TryParse(modifier.Value, out intValue))
                            wepOverride.RateOfFire = intValue;
                        break;
                    case "ReloadTime":
                        if (int.TryParse(modifier.Value, out intValue))
                            wepOverride.ReloadTime = intValue;
                        break;
                    case "DeviateShotAngle":
                        if (float.TryParse(modifier.Value, out floatValue))
                            wepOverride.DeviateShotAngle = floatValue;
                        break;
                    case "AimingTolerance":
                        if (float.TryParse(modifier.Value, out floatValue))
                            wepOverride.AimingTolerance = (double)floatValue;
                        break;
                    case "HeatPerShot":
                        if (int.TryParse(modifier.Value, out intValue))
                            wepOverride.HeatPerShot = intValue;
                        break;
                    case "HeatSinkRate":
                        if (float.TryParse(modifier.Value, out floatValue))
                            wepOverride.HeatSinkRate = floatValue;
                        break;
                    case "IdlePower":
                        if (float.TryParse(modifier.Value, out floatValue))
                            wepOverride.IdlePower = floatValue;
                        break;
                    default:
                        break;
                }

            }

            if (Core.Enforcement.DefinitionOverrides == null)
                Core.Enforcement.DefinitionOverrides = new CoreSettings.ServerSettings.Overrides();

            Core.Enforcement.DefinitionOverrides.WeaponOverrides = overrides.Values.ToArray();
        }

    }
}

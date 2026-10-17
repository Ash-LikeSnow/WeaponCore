using CoreSystems.Support;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.ModelAssignmentsDef;
using static CoreSystems.Support.WeaponDefinition.HardPointDef;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.BlockTypes;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.Threat;
using static CoreSystems.Support.WeaponDefinition.HardPointDef.HardwareDef.HardwareType;

namespace Scripts
{
    partial class Parts
    {
        // Don't edit above this line
        WeaponDefinition LargeFlareLauncher => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "LargeFlareLauncher", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    new MountPointDef {
                        SubtypeId = "LargeInsetFlareLauncher", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                 },
                Muzzles = new[] {
                    "muzzle_missile_033",
                    "muzzle_missile_034",
                    "muzzle_missile_035",
                    "muzzle_missile_036",
                    "muzzle_missile_037",
                    "muzzle_missile_038",
                    "muzzle_missile_039",
                    "muzzle_missile_040",
                    "muzzle_missile_041",
                    "muzzle_missile_042",
                    "muzzle_missile_043",
                    "muzzle_missile_044",
                    "muzzle_missile_045",
                    "muzzle_missile_046",
                    "muzzle_missile_047",
                    "muzzle_missile_048",
                    "muzzle_missile_049",
                    "muzzle_missile_050",
                    "muzzle_missile_051",
                    "muzzle_missile_052",
                    "muzzle_missile_053",
                    "muzzle_missile_054",
                    "muzzle_missile_055",
                    "muzzle_missile_056",
                    "muzzle_missile_057",
                    "muzzle_missile_058",
                    "muzzle_missile_059",
                    "muzzle_missile_060",
                    "muzzle_missile_061",
                    "muzzle_missile_062",
                    "muzzle_missile_063",
                    "muzzle_missile_064",
                },
                Scope = "muzzle_missile_052", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Projectiles, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Power, Utility, Offense, Thrust, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
            },
            HardPoint = new HardPointDef
            {
                PartName = "Large Flare Launcher", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).

                // If there are multiple definitions with the same part name & subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                // For people making their own weapon mod, its recommended to leave this at zero.
                // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero (ie. 1).
                // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                DefinitionPriority = int.MinValue,
                DeviateShotAngle = 12f, // Projectile inaccuracy in degrees.

                HardWare = new HardwareDef
                {
                    InventorySize = 0.032f, // Inventory capacity in kL.
                    IdlePower = 0.0025f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 140, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 60, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 3, // Number of physical magazines to consume on reload.
                    AllowAnyAmmmo = true,
                    AllowAnyAmmmoDefault = true,
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "ArcWeFireworkShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                Flare, FireworksRainbow, FireworksBlue, FireworksGreen, FireworksRed, FireworksYellow, FireworksPink // Must list all primary, shrapnel, and pattern ammos.
            },
        };
        WeaponDefinition SmallFlareLauncher => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SmallFlareLauncher", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    new MountPointDef {
                        SubtypeId = "SmallInsetFlareLauncher", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                 },
                Muzzles = new[] {
                    "muzzle_missile_033",
                    "muzzle_missile_034",
                    "muzzle_missile_035",
                    "muzzle_missile_036",
                    "muzzle_missile_037",
                    "muzzle_missile_038",
                    "muzzle_missile_039",
                    "muzzle_missile_040",
                    "muzzle_missile_041",
                 },
                Scope = "muzzle_missile_037", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Projectiles, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Power, Utility, Offense, Thrust, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
            },
            HardPoint = new HardPointDef
            {
                PartName = "Small Flare Launcher", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).

                // If there are multiple definitions with the same part name & subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                // For people making their own weapon mod, its recommended to leave this at zero.
                // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero (ie. 1).
                // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                DefinitionPriority = int.MinValue,
                DeviateShotAngle = 12f, // Projectile inaccuracy in degrees.

                HardWare = new HardwareDef
                {
                    InventorySize = 0.006f, // Inventory capacity in kL.
                    IdlePower = 0.0025f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 80, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 60, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    AllowAnyAmmmo = true,
                    AllowAnyAmmmoDefault = true,
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "ArcWeFireworkShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                Flare, FireworksRainbow, FireworksBlue, FireworksGreen, FireworksRed, FireworksYellow, FireworksPink // Must list all primary, shrapnel, and pattern ammos.
            },
        };
    }
}
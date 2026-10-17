using CoreSystems.Support;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.ModelAssignmentsDef;
using static CoreSystems.Support.WeaponDefinition.HardPointDef;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.BlockTypes;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.Threat;
using static CoreSystems.Support.WeaponDefinition.HardPointDef.HardwareDef.HardwareType;

namespace Scripts {   
    partial class Parts {
        // Don't edit above this line
        WeaponDefinition LargeTorpedoLauncher => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "LargeBlockTorpedoLauncher", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    
                 },
                Muzzles = new[] {
                    "muzzle_missile_001", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
                },
                Scope = "muzzle_missile_001", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, Characters, Meteors, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Thrust, Utility, Offense, Power, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
                TopTargets = 4, // Maximum number of targets to randomize between; 0 = unlimited.
                TopBlocks = 4, // Maximum number of blocks to randomize between; 0 = unlimited.
                StopTrackingSpeed = 1000, // Do not track threats traveling faster than this speed; 0 = unlimited.
            },
            HardPoint = new HardPointDef
            {
                PartName = "Large Torpedo Launcher", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).

                // If there are multiple definitions with the same part name & subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                // For people making their own weapon mod, its recommended to leave this at zero.
                // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero (ie. 1).
                // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                DefinitionPriority = int.MinValue,
                DeviateShotAngle = 0.1f, // Projectile inaccuracy in degrees.
                AimingTolerance = 4f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                HardWare = new HardwareDef
                {
                    InventorySize = 1.4f, // Inventory capacity in kL.
                    IdlePower = 0.001f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 6, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 1200, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    AllowAnyAmmmo = true,
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepShipSmallMissileShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipRailgunNoAmmo",
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                TorpedoAmmo, TorpedoHeavyAmmo, TorpedoPiercingAmmo, TorpedoSeekingAmmo, // Must list all primary, shrapnel, and pattern ammos.
            },
        };      
       

        WeaponDefinition SmallTorpedoLauncher => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SmallBlockTorpedoLauncher", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.25f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                 },
                Muzzles = new[] {
                    "muzzle_missile_001", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
                },
                Scope = "muzzle_missile_001", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, Characters, Meteors, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Thrust, Utility, Offense, Power, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
                TopTargets = 4, // Maximum number of targets to randomize between; 0 = unlimited.
                TopBlocks = 4, // Maximum number of blocks to randomize between; 0 = unlimited.
                StopTrackingSpeed = 1000, // Do not track threats traveling faster than this speed; 0 = unlimited.
            },
            HardPoint = new HardPointDef
            {
                PartName = "Small Torpedo Launcher", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                // If there are multiple definitions with the same part name & subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                // For people making their own weapon mod, its recommended to leave this at zero.
                // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero (ie. 1).
                // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                DefinitionPriority = int.MinValue,
                DeviateShotAngle = 0.1f, // Projectile inaccuracy in degrees.
                AimingTolerance = 4f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                HardWare = new HardwareDef
                {
                    InventorySize = 0.35f, // Inventory capacity in kL.
                    IdlePower = 0.001f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 6, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 1200, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    AllowAnyAmmmo = true,
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepShipSmallMissileShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipRailgunNoAmmo",
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                TorpedoAmmo, TorpedoHeavyAmmo, TorpedoPiercingAmmo, TorpedoSeekingAmmo, // Must list all primary, shrapnel, and pattern ammos.
            },
        };

        // Don't edit below this line.
    }
}

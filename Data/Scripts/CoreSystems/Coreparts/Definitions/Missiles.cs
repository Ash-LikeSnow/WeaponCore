using CoreSystems.Support;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.ModelAssignmentsDef;
using static CoreSystems.Support.WeaponDefinition.HardPointDef;
using static CoreSystems.Support.WeaponDefinition.HardPointDef.Prediction;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.BlockTypes;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.Threat;
using static CoreSystems.Support.WeaponDefinition.HardPointDef.HardwareDef.HardwareType;

namespace Scripts {   
    partial class Parts {
        // Don't edit above this line
        WeaponDefinition LargeMissileLauncher => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "LargeMissileLauncher", // Block Subtypeid. Your Cubeblocks contain this information
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
					"muzzle_missile_002",
					"muzzle_missile_003",
					"muzzle_missile_004",
					"muzzle_missile_005",
					"muzzle_missile_006",
					"muzzle_missile_007",					
                    "muzzle_missile_008",
					"muzzle_missile_009",
					"muzzle_missile_010",
					"muzzle_missile_011",
					"muzzle_missile_012",
					"muzzle_missile_013",
					"muzzle_missile_014",
					"muzzle_missile_015",
					"muzzle_missile_016",
					"muzzle_missile_017",
					"muzzle_missile_018",
					"muzzle_missile_019",
                },
                Scope = "muzzle_missile_010", // Where line of sight checks are performed from. Must be clear of block collision.
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
                PartName = "Large Missile Launcher", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.1f, // Projectile inaccuracy in degrees.
                AimingTolerance = 4f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                HardWare = new HardwareDef
                {
                    InventorySize = 1.14f, // Inventory capacity in kL.
                    IdlePower = 0.001f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 120, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 240, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 19, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepShipSmallMissileShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    HardPointRotationSound = "WepTurretGatlingRotate", // Audio played when turret is moving.
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                MissileAmmo, // Must list all primary, shrapnel, and pattern ammos.
            },
        };
        
        WeaponDefinition LargeMissileTurret => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "LargeMissileTurret", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "MissileTurretBarrels", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "MissileTurretBase1", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "MissileTurretBarrels",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                        new MountPointDef {
                        SubtypeId = "LargeMissileTurretReskin", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "MissileTurretBarrels", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "MissileTurretBase1", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "MissileTurretBarrels",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },

                 },
                Muzzles = new[] {
                    "muzzle_missile_001", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
					"muzzle_missile_002",
					"muzzle_missile_003",
					"muzzle_missile_004",
					"muzzle_missile_005",
					"muzzle_missile_006",
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
                PartName = "Large Missile Turret", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.5f, // Projectile inaccuracy in degrees.
                AimingTolerance = 4f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                AimLeadingPrediction = Accurate, // Level of turret aim prediction; Off, Basic, Accurate, Advanced
                Ai = new AiDef
                {
                    TrackTargets = true, // Whether this weapon tracks its own targets, or (for multiweapons) relies on the weapon with PrimaryTracking enabled for target designation. Turrets Need this set to True.
                    TurretAttached = true, // Whether this weapon is a turret and should have the UI and API options for such. Turrets Need this set to True.
                    TurretController = true, // Whether this weapon can physically control the turret's movement. Turrets Need this set to True.
                    PrimaryTracking = true, // For multiweapons: whether this weapon should designate targets for other weapons on the platform without their own tracking.
                },
                HardWare = new HardwareDef
                {
                    RotateRate = 0.03f, // Max traversal speed of azimuth subpart in radians per tick (0.1 is approximately 360 degrees per second).
                    ElevateRate = 0.03f, // Max traversal speed of elevation subpart in radians per tick.
                    MinAzimuth = -180,
                    MaxAzimuth = 180,
                    MinElevation = -58,
                    MaxElevation = 90,
                    InventorySize = 0.768f, // Inventory capacity in kL.
                    IdlePower = 0.01f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 90, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 240, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 6, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepTurretMissileShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    HardPointRotationSound = "WepTurretGatlingRotate", // Audio played when turret is moving.
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                MissileAmmo, // Must list all primary, shrapnel, and pattern ammos.
            },
        };

        WeaponDefinition SmallMissileLauncher => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SmallMissileLauncher", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.25f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    new MountPointDef {
                        SubtypeId = "SmallMissileLauncherWarfare2", // Block Subtypeid. Your Cubeblocks contain this information
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
					"muzzle_missile_002",
					"muzzle_missile_003",
					"muzzle_missile_004",
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
                PartName = "Small Missile Launcher", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.1f, // Projectile inaccuracy in degrees.
                AimingTolerance = 4f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                HardWare = new HardwareDef
                {
                    InventorySize = 0.24f, // Inventory capacity in kL.
                    IdlePower = 0.001f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 60, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 60, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 4, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepShipSmallMissileShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    HardPointRotationSound = "WepTurretGatlingRotate", // Audio played when turret is moving.
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                MissileAmmo, // Must list all primary, shrapnel, and pattern ammos.
            },
        };

        WeaponDefinition SmallRocketLauncherReload => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SmallRocketLauncherReload", // Block Subtypeid. Your Cubeblocks contain this information
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
					"muzzle_missile_002",
					"muzzle_missile_003",
					"muzzle_missile_004",
                },
                Scope = "muzzle_missile_002", // Where line of sight checks are performed from. Must be clear of block collision.
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
                PartName = "Reloadable Missile Launcher", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.1f, // Projectile inaccuracy in degrees.
                AimingTolerance = 4f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                HardWare = new HardwareDef
                {
                    InventorySize = 0.245f, // Inventory capacity in kL.
                    IdlePower = 0.001f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 60, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 60, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 4, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepShipSmallMissileShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    HardPointRotationSound = "WepTurretGatlingRotate", // Audio played when turret is moving.
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                MissileAmmo, // Must list all primary, shrapnel, and pattern ammos.
            },
        };
        
        WeaponDefinition SmallMissileTurret => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SmallMissileTurret", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "MissileTurretBarrels", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "MissileTurretBase1", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "MissileTurretBarrels",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    new MountPointDef {
                        SubtypeId = "SmallMissileTurretReskin", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "MissileTurretBarrels", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "MissileTurretBase1", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "MissileTurretBarrels",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },

                 },
                Muzzles = new[] {
                    "muzzle_missile_001", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
					"muzzle_missile_002",
                },
                Ejector = "", // Optional; empty from which to eject "shells" if specified.
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
                PartName = "Small Missile Turret", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.5f, // Projectile inaccuracy in degrees.
                AimingTolerance = 4f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                AimLeadingPrediction = Accurate, // Level of turret aim prediction; Off, Basic, Accurate, Advanced
                Ai = new AiDef
                {
                    TrackTargets = true, // Whether this weapon tracks its own targets, or (for multiweapons) relies on the weapon with PrimaryTracking enabled for target designation. Turrets Need this set to True.
                    TurretAttached = true, // Whether this weapon is a turret and should have the UI and API options for such. Turrets Need this set to True.
                    TurretController = true, // Whether this weapon can physically control the turret's movement. Turrets Need this set to True.
                    PrimaryTracking = true, // For multiweapons: whether this weapon should designate targets for other weapons on the platform without their own tracking.
                },
                HardWare = new HardwareDef
                {
                    RotateRate = 0.03f, // Max traversal speed of azimuth subpart in radians per tick (0.1 is approximately 360 degrees per second).
                    ElevateRate = 0.03f, // Max traversal speed of elevation subpart in radians per tick.
                    MinAzimuth = -180,
                    MaxAzimuth = 180,
                    MinElevation = -8,
                    MaxElevation = 90,
                    InventorySize = 0.36f, // Inventory capacity in kL.
                    IdlePower = 0.005f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 90, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 360, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 2, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepTurretMissileShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    HardPointRotationSound = "WepTurretGatlingRotate", // Audio played when turret is moving.
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
            },
            Ammos = new[] {
                MissileAmmo, // Must list all primary, shrapnel, and pattern ammos.
            },
        };
        // Don't edit below this line.
    }
}

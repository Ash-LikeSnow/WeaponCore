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
        WeaponDefinition LargeBlockAssaultCannonTurret => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "LargeBlockMediumCalibreTurret", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "MissileTurretBarrels", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "MissileTurretBase1", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "MissileTurretBarrels",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    
                 },
                Muzzles = new[] {
                    "Muzzle_Missile_Left", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
                    "Muzzle_Missile_Right",
                },
                Scope = "Muzzle_Missile_Left", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Utility, Offense, Thrust, Power, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
                TopTargets = 4, // Maximum number of targets to randomize between; 0 = unlimited.
                TopBlocks = 4, // Maximum number of blocks to randomize between; 0 = unlimited.
                StopTrackingSpeed = 1000, // Do not track threats traveling faster than this speed; 0 = unlimited.
            },
            HardPoint = new HardPointDef
            {
                PartName = "Large Assault Cannon Turret", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.35f, // Projectile inaccuracy in degrees.
                AimingTolerance = 2f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                AimLeadingPrediction = Advanced, // Level of turret aim prediction; Off, Basic, Accurate, Advanced
                Ai = new AiDef
                {
                    TrackTargets = true, // Whether this weapon tracks its own targets, or (for multiweapons) relies on the weapon with PrimaryTracking enabled for target designation. Turrets Need this set to True.
                    TurretAttached = true, // Whether this weapon is a turret and should have the UI and API options for such. Turrets Need this set to True.
                    TurretController = true, // Whether this weapon can physically control the turret's movement. Turrets Need this set to True.
                    PrimaryTracking = true, // For multiweapons: whether this weapon should designate targets for other weapons on the platform without their own tracking.
                },
                HardWare = new HardwareDef
                {
                    RotateRate = 0.01f, // Max traversal speed of azimuth subpart in radians per tick (0.1 is approximately 360 degrees per second).
                    ElevateRate = 0.01f, // Max traversal speed of elevation subpart in radians per tick.
                    MinAzimuth = -180,
                    MaxAzimuth = 180,
                    MinElevation = -20,
                    MaxElevation = 75,
                    InventorySize = 0.18f, // Inventory capacity in kL.
                    IdlePower = 0.01f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 180, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 360, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 2, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepMediumCalibreShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    HardPointRotationSound = "WepTurretGatlingRotate", // Audio played when turret is moving.
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
                Graphics = new HardPointParticleDef
                {
                    Effect1 = new ParticleDef
                    {
                        Name = "Muzzle_Flash_MediumCalibre", // SubtypeId of muzzle particle effect.
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1f, // Scale of effect.
                        },
                    },
                },
            },
            Ammos = new[] {
                AssaultCannonShell, // Must list all primary, shrapnel, and pattern ammos.
            },
        };

        WeaponDefinition SmallBlockAssaultCannon => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SmallBlockMediumCalibreGun", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.25f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    
                 },
                Muzzles = new[] {
                    "Muzzle_Missile", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
                },
                Scope = "Muzzle_Missile", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Utility, Offense, Thrust, Power, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
                TopTargets = 4, // Maximum number of targets to randomize between; 0 = unlimited.
                TopBlocks = 4, // Maximum number of blocks to randomize between; 0 = unlimited.
                StopTrackingSpeed = 1000, // Do not track threats traveling faster than this speed; 0 = unlimited.
            },
            HardPoint = new HardPointDef
            {
                PartName = "Assault Cannon", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.2f, // Projectile inaccuracy in degrees.
                AimingTolerance = 2f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                HardWare = new HardwareDef
                {
                    InventorySize = 0.09f, // Inventory capacity in kL.
                    IdlePower = 0.001f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 200, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 360, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepMediumCalibreShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
                Graphics = new HardPointParticleDef
                {
                    Effect1 = new ParticleDef
                    {
                        Name = "Muzzle_Flash_MediumCalibre", // SubtypeId of muzzle particle effect.
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1f, // Scale of effect.
                        },
                    },
                },
            },
            Ammos = new[] {
                AssaultCannonShell, // Must list all primary, shrapnel, and pattern ammos.
            },
        };

        WeaponDefinition SmallBlockAssaultCannonTurret => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SmallBlockMediumCalibreTurret", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns. Subpart_Boomsticks must be written as Boomsticks.
                        MuzzlePartId = "MissileTurretBarrels", // The subpart where your muzzle empties are located. This is often the elevation subpart. Subpart_Boomsticks must be written as Boomsticks.
                        AzimuthPartId = "MissileTurretBase1", // Your Rotating Subpart, the bit that moves sideways.
                        ElevationPartId = "MissileTurretBarrels",// Your Elevating Subpart, that bit that moves up.
                        DurabilityMod = 0.5f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    
                 },
                Muzzles = new[] {
                    "Muzzle_Missile", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
                },
                Scope = "Muzzle_Missile", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Utility, Offense, Thrust, Power, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
                TopTargets = 4, // Maximum number of targets to randomize between; 0 = unlimited.
                TopBlocks = 4, // Maximum number of blocks to randomize between; 0 = unlimited.
                StopTrackingSpeed = 1000, // Do not track threats traveling faster than this speed; 0 = unlimited.
            },
            HardPoint = new HardPointDef
            {
                PartName = "Small Assault Cannon Turret", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.5f, // Projectile inaccuracy in degrees.
                AimingTolerance = 2f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                AimLeadingPrediction = Advanced, // Level of turret aim prediction; Off, Basic, Accurate, Advanced
                Ai = new AiDef
                {
                    TrackTargets = true, // Whether this weapon tracks its own targets, or (for multiweapons) relies on the weapon with PrimaryTracking enabled for target designation. Turrets Need this set to True.
                    TurretAttached = true, // Whether this weapon is a turret and should have the UI and API options for such. Turrets Need this set to True.
                    TurretController = true, // Whether this weapon can physically control the turret's movement. Turrets Need this set to True.
                    PrimaryTracking = true, // For multiweapons: whether this weapon should designate targets for other weapons on the platform without their own tracking.
                },
                HardWare = new HardwareDef
                {
                    RotateRate = 0.02f, // Max traversal speed of azimuth subpart in radians per tick (0.1 is approximately 360 degrees per second).
                    ElevateRate = 0.02f, // Max traversal speed of elevation subpart in radians per tick.
                    MinAzimuth = -180,
                    MaxAzimuth = 180,
                    MinElevation = -10,
                    MaxElevation = 50,
                    InventorySize = 0.09f, // Inventory capacity in kL.
                    IdlePower = 0.005f, // Constant base power draw in MW.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 200, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    ReloadTime = 360, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                },
                Audio = new HardPointAudioDef
                {
                    FiringSound = "WepMediumCalibreShot", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    NoAmmoSound = "WepShipGatlingNoAmmo",
                    HardPointRotationSound = "WepTurretGatlingRotate", // Audio played when turret is moving.
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
                Graphics = new HardPointParticleDef
                {
                    Effect1 = new ParticleDef
                    {
                        Name = "Muzzle_Flash_MediumCalibre", // SubtypeId of muzzle particle effect.
                        Extras = new ParticleOptionDef
                        {
                            Restart = false, // Whether to end a looping effect instantly when firing stops.
                        },
                    },
                },
            },
            Ammos = new[] {
                AssaultCannonShell, // Must list all primary, shrapnel, and pattern ammos.
            },
        };
        // Don't edit below this line.
    }
}

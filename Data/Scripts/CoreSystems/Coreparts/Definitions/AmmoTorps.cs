using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AmmoDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef.Falloff;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef.AoeShape;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.TrajectoryDef;
namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        private AmmoDef TorpedoAmmo => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "Torpedo", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Standard Torpedo", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 1f, // Direct damage; one steel plate is worth 100.
            Mass = 50, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 15000,
            Health = 2, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 4f, // Radius of AOE effect, in meters.
                    Damage = 2500f,
                    Depth = 2f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                    MaxAbsorb = 2500f, // Soft cutoff for damage, except for pooled falloff.  If pooled falloff, limits max damage per block.
                    Falloff = Curve, //.NoFalloff applies the same damage to all blocks in radius
                    MinArmingTime = 1, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    ParticleScale = 1,
                    Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                },
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 45, // Degrees, Is pointed forward
                TargetLossTime = 30,
                MaxLifeTime = 1200, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 35, // Meters Per Second. This is the spawning Speed of the Projectile, and used by turning.
                DesiredSpeed = 150, // voxel phasing if you go above 5100
                MaxTrajectory = 2000, // Max Distance the projectile or beam can Travel.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 15, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Aggressiveness = 3f, // controls how responsive tracking is, recommended value 3-5.
                    TrackingDelay = 15, // Measured in Shape diameter units traveled.
                    MaxTargets = 1, // Number of targets allowed before ending, 0 = unlimited, 1 = stay with first target when fired
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    FocusOnly = true, // Only target the HUD or AI focused target (this includes changes to the hud-selected target.  Set MaxTargets = 1 to keep it from switching (aka fire and forget)
                    MinTurnSpeed = 35, // set this to a reasonable value to avoid projectiles from spinning in place or being too aggressive turing at slow speeds 
                },
            },

            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\Torpedo_Standard.mwm", // Model Path goes here.  
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "Smoke_Torpedo", //ShipWelderArc
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "TorpedoFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
            },
        };
        private AmmoDef TorpedoHeavyAmmo => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "Torpedo_Heavy", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Heavy Torpedo", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 1f, // Direct damage; one steel plate is worth 100.
            Mass = 2000, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 15000,
            Health = 2, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 15f, // Radius of AOE effect, in meters.
                    Damage = 8000f,
                    Depth = 7.5f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                    MaxAbsorb = 8000f, // Soft cutoff for damage, except for pooled falloff.  If pooled falloff, limits max damage per block.
                    Falloff = Curve, //.NoFalloff applies the same damage to all blocks in radius
                    MinArmingTime = 1, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    ParticleScale = 1,
                    Shape = Round, // Round or Diamond shape.  Diamond is more performance friendly.
                },
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 45, // Degrees, Is pointed forward
                TargetLossTime = 30,
                MaxLifeTime = 1200, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 30, // Meters Per Second. This is the spawning Speed of the Projectile, and used by turning.
                DesiredSpeed = 150, // voxel phasing if you go above 5100
                MaxTrajectory = 2000, // Max Distance the projectile or beam can Travel.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 6, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Aggressiveness = 3f, // controls how responsive tracking is, recommended value 3-5.
                    TrackingDelay = 15, // Measured in Shape diameter units traveled.
                    MaxTargets = 1, // Number of targets allowed before ending, 0 = unlimited, 1 = stay with first target when fired
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    FocusOnly = true, // Only target the HUD or AI focused target (this includes changes to the hud-selected target.  Set MaxTargets = 1 to keep it from switching (aka fire and forget)
                    MinTurnSpeed = 35, // set this to a reasonable value to avoid projectiles from spinning in place or being too aggressive turing at slow speeds 
                },
            },

            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\Torpedo_Heavy.mwm", // Model Path goes here.  
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "Smoke_Torpedo", //ShipWelderArc
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "TorpedoFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
            },
        };
        private AmmoDef TorpedoSeekingAmmo => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "Torpedo_Seeking", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Seeking Torpedo", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 1f, // Direct damage; one steel plate is worth 100.
            Mass = 2000, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 15000,
            Health = 2, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 4f, // Radius of AOE effect, in meters.
                    Damage = 500f,
                    Depth = 2f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                    MaxAbsorb = 500f, // Soft cutoff for damage, except for pooled falloff.  If pooled falloff, limits max damage per block.
                    Falloff = Curve, //.NoFalloff applies the same damage to all blocks in radius
                    MinArmingTime = 1, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    ParticleScale = 1,
                    Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                },
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 45, // Degrees, Is pointed forward
                TargetLossTime = 30,
                MaxLifeTime = 1200, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 55, // Meters Per Second. This is the spawning Speed of the Projectile, and used by turning.
                DesiredSpeed = 220, // voxel phasing if you go above 5100
                MaxTrajectory = 2000, // Max Distance the projectile or beam can Travel.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 25, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Aggressiveness = 3f, // controls how responsive tracking is, recommended value 3-5.
                    TrackingDelay = 15, // Measured in Shape diameter units traveled.
                    MaxTargets = 1, // Number of targets allowed before ending, 0 = unlimited, 1 = stay with first target when fired
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    FocusOnly = true, // Only target the HUD or AI focused target (this includes changes to the hud-selected target.  Set MaxTargets = 1 to keep it from switching (aka fire and forget)
                    MinTurnSpeed = 35, // set this to a reasonable value to avoid projectiles from spinning in place or being too aggressive turing at slow speeds 
                },
            },

            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\Torpedo_Seeking.mwm", // Model Path goes here.  
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "Smoke_Torpedo", //ShipWelderArc
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "TorpedoFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
            },
        };
        private AmmoDef TorpedoPiercingAmmo => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "Torpedo_Piercing", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Piercing Torpedo", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 20000, // Direct damage; one steel plate is worth 100.
            Mass = 2000, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 15000,
            Health = 2, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 4f, // Radius of AOE effect, in meters.
                    Damage = 1500f,
                    MaxAbsorb = 1500f, // Soft cutoff for damage, except for pooled falloff.  If pooled falloff, limits max damage per block.
                    Falloff = Curve, //.NoFalloff applies the same damage to all blocks in radius
                    MinArmingTime = 1, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    ParticleScale = 1,
                    Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                },
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 45, // Degrees, Is pointed forward
                TargetLossTime = 30,
                MaxLifeTime = 1200, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 45, // Meters Per Second. This is the spawning Speed of the Projectile, and used by turning.
                DesiredSpeed = 180, // voxel phasing if you go above 5100
                MaxTrajectory = 2000, // Max Distance the projectile or beam can Travel.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 12, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Aggressiveness = 3f, // controls how responsive tracking is, recommended value 3-5.
                    TrackingDelay = 15, // Measured in Shape diameter units traveled.
                    MaxTargets = 1, // Number of targets allowed before ending, 0 = unlimited, 1 = stay with first target when fired
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    FocusOnly = true, // Only target the HUD or AI focused target (this includes changes to the hud-selected target.  Set MaxTargets = 1 to keep it from switching (aka fire and forget)
                    MinTurnSpeed = 35, // set this to a reasonable value to avoid projectiles from spinning in place or being too aggressive turing at slow speeds 
                },
            },

            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\Torpedo_Piercing.mwm", // Model Path goes here.  
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "Smoke_Torpedo", //ShipWelderArc
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "TorpedoFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
            },
        };
    }
}

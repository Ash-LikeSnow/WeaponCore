using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AmmoDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.ShapeDef.Shapes;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.DamageScaleDef.CustomScalesDef.SkipMode;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef.DecalDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.DamageScaleDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.DamageScaleDef.ShieldDef.ShieldType;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.DamageScaleDef.DeformDef.DeformTypes;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef.Falloff;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef.AoeShape;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef.LineDef.Texture;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.DamageScaleDef.DamageTypes.Damage;
namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        private AmmoDef SpotLight => new AmmoDef
        {
            AmmoMagazine = "Energy",
            AmmoRound = "SpotLight",
            BaseDamage = 0,
            NoGridOrArmorScaling = true,
            EnergyMagazineSize = 1,
            Trajectory = new TrajectoryDef
            {
                MaxLifeTime = 3600,
                MaxTrajectory = 3000,
                DesiredSpeed = 1000,
            },
        };
        private AmmoDef GatlingAmmo => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "NATO_25x184mm", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "25mm NATO", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 90f, // Direct damage; one steel plate is worth 100.
            Mass = 1f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 200f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.           
            DamageScales = new DamageScaleDef
            {
                HealthHitModifier = 1, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = 0.33f, // Character damage multiplier; defaults to 1 if zero or less.
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = -1f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Deform = new DeformDef
                {
                    DeformType = HitBlock,
                    DeformDelay = 30,
                },
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 180f, // Degrees, Is pointed forward
                MaxLifeTime = 300, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 800, // voxel phasing if you go above 5100
                MaxTrajectory = 800f, // Max Distance the projectile or beam can Travel.
                SpeedVariance = Random(start: -5, end: 5), // subtracts value from DesiredSpeed. Be warned, you can make your projectile go backwards.
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                ShieldHitDraw = false,
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new []
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "GunBullet",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "GunBullet",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal_GatlingGun",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 0.5f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0.5f, end: 2.5f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0.05f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 2.5f, //
                        Width = 0.1f, //
                        Color = Color(red: 12.5f, green: 10, blue: 8f, alpha: 1), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 240, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "ProjectileTrailLine", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 0.5f,
                HitPlayShield = true,
            },
        };

        private AmmoDef InteriorAmmo => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "RapidFireAutomaticRifleGun_Mag_50rd", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "MR-50A", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            EnergyCost = 0.1f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 30f, // Direct damage; one steel plate is worth 100.
            Mass = 0.1f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 1f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            DamageScales = new DamageScaleDef
            {
                HealthHitModifier = 0.5, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = 0.3f, // Multiplier for damage against light armor.
                    Heavy = 0.1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = 0.2f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Deform = new DeformDef
                {
                    DeformType = HitBlock,
                    DeformDelay = 30,
                },
            },
            Trajectory = new TrajectoryDef
            {
                MaxLifeTime = 100, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 600, // voxel phasing if you go above 5100
                MaxTrajectory = 400f, // Max Distance the projectile or beam can Travel.
            },
            AmmoGraphics = new GraphicDef
            {
                VisualProbability = 1f, // %
                ShieldHitDraw = false,
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "GunBullet",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "GunBullet",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 0.5f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 2.5f, end: 5f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0.05f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 1f, //
                        Width = 0.05f, //
                        Color = Color(red: 1.8f, green: 1.4f, blue: 1f, alpha: 1), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 240, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "ProjectileTrailLine", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 0.5f,
                HitPlayShield = true,
            },
        };

        private AmmoDef InteriorAmmoOld => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "NATO_5p56x45mm", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "5.56x45mm", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 30f, // Direct damage; one steel plate is worth 100.
            Mass = 0.1f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 1f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 0.5, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = 0.3f, // Multiplier for damage against light armor.
                    Heavy = 0.1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = 0.2f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Deform = new DeformDef
                {
                    DeformType = HitBlock,
                    DeformDelay = 30,
                },
            },
            Trajectory = new TrajectoryDef
            {
                MaxLifeTime = 100, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 600, // voxel phasing if you go above 5100
                MaxTrajectory = 400f, // Max Distance the projectile or beam can Travel.
            },
            AmmoGraphics = new GraphicDef
            {
                VisualProbability = 1f, // %
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "GunBullet",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "GunBullet",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 0.5f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 2.5f, end: 5f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0.05f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 1f, //
                        Width = 0.05f, //
                        Color = Color(red: 1.8f, green: 1.4f, blue: 1f, alpha: 1), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 240, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "ProjectileTrailLine", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 0.5f,
                HitPlayShield = true,
            },
        };

        private AmmoDef MissileAmmo => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "Missile200mm", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "200mm Missile", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            HybridRound = false, // Use both a physical ammo magazine and energy per shot.
            BaseDamage = 1f, // Direct damage; one steel plate is worth 100.
            Mass = 45f, // In kilograms; how much force the impact will apply to the target.
            Health = 2, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
            {
                Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                Diameter = 1, // Diameter is minimum length of LineShape or minimum diameter of SphereShape.
            },
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 2, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                FallOff = new FallOffDef
                {
                    Distance = 0f, // Distance at which damage begins falling off.
                    MinMultipler = -1f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                },
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = -1f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Deform = new DeformDef
                {
                    DeformType = HitBlock,
                    DeformDelay = 30,
                },
                Custom = new CustomScalesDef
                {
                    SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                    Types = new[] // List of blocks to apply custom damage multipliers to.
                    {
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test1",
                            Modifier = -1f,
                        },
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test2",
                            Modifier = -1f,
                        },
                    },
                },
            },
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 4f, // Radius of AOE effect, in meters.
                    Damage = 500f,
                    Depth = 1f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                    MaxAbsorb = 0f, // Soft cutoff for damage, except for pooled falloff.  If pooled falloff, limits max damage per block.
                    Falloff = Linear, //.NoFalloff applies the same damage to all blocks in radius
                    MinArmingTime = 1, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    ParticleScale = 1,
                    Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                }, 
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 15, // Degrees, Is pointed forward
                MaxLifeTime = 1200, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 0, // Meters Per Second. This is the spawning Speed of the Projectile, and used by turning.
                DesiredSpeed = 200, // voxel phasing if you go above 5100
                MaxTrajectory = 800f, // Max Distance the projectile or beam can Travel.
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\Projectile_Missile.mwm", // Model Path goes here.  
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "Smoke_Missile", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "MissileFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
            },
        };
	  
        private AmmoDef AutocannonShell => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "AutocannonClip", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Autocannon Shell", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 500f, // Direct damage; one steel plate is worth 100.
            Mass = 3f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 250f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 2, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = 0.2f, // Character damage multiplier; defaults to 1 if zero or less.
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = -1f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Deform = new DeformDef
                {
                    DeformType = HitBlock,
                    DeformDelay = 30,
                },
                Custom = new CustomScalesDef
                {
                    SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                    Types = new[] // List of blocks to apply custom damage multipliers to.
                    {
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test1",
                            Modifier = -1f,
                        },
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test2",
                            Modifier = -1f,
                        },
                    },
                },
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                MaxLifeTime = 420, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 800, // voxel phasing if you go above 5100
                MaxTrajectory = 800f, // Max Distance the projectile or beam can Travel.
                SpeedVariance = Random(start: -5, end: 5), // subtracts value from DesiredSpeed. Be warned, you can make your projectile go backwards.
                RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
             },
            AmmoGraphics = new GraphicDef
            {
                VisualProbability = 1f, // %
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "GunAutocannonBullet",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "GunAutocannonBullet",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal_GatlingGun",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 0.5f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0.5f, end: 2.5f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0.05f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 5f, //
                        Width = 0.2f, //
                        Color = Color(red: 12.5f, green: 8, blue: 5f, alpha: 1), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 240, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "ProjectileTrailLine", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 1f,
                HitPlayShield = true,
            },
        };
	  
        private AmmoDef AssaultCannonShell => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "MediumCalibreAmmo", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Assault Cannon Shell", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 4000f, // Direct damage; one steel plate is worth 100.
            Mass = 300f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 10000f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            Trajectory = new TrajectoryDef
            {
                Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 180f, // Degrees, Is pointed forward
                MaxLifeTime = 480, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 500, // voxel phasing if you go above 5100
                MaxTrajectory = 1400f, // Max Distance the projectile or beam can Travel.
                GravityMultiplier = 1f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable. Natural Gravity Only.
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\MediumCalibreShell.mwm", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "GunBullet",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "GunBullet",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "MediumCalibreGun_Tracer", //ShipWelderArc
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal_GatlingGun",
                        ApplyToShield = true,
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "MediumShellFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 1f,
                HitPlayShield = true,
            },
        };
	  
        private AmmoDef ArtilleryShell => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "LargeCalibreAmmo", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Artillery Shell", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            BaseDamage = 17000f, // Direct damage; one steel plate is worth 100.
            Mass = 800f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 30000f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            Trajectory = new TrajectoryDef
            {
                Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                MaxLifeTime = 600, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 500, // voxel phasing if you go above 5100
                MaxTrajectory = 2000f, // Max Distance the projectile or beam can Travel.
                GravityMultiplier = 0.5f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable. Natural Gravity Only.
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\LargeCalibreShell.mwm", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                ShieldHitDraw = false,
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "LargeShell",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "LargeShell",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "LargeCalibreGun_Tracer", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal_GatlingGun",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "HeavyShellFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 1f,
                HitPlayShield = true,
            },
        };
	  
        private AmmoDef LargeRailgunSabot => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "LargeRailgunAmmo", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Large Railgun Slug", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            HybridRound = true, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0.1368f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 50000f, // Direct damage; one steel plate is worth 100.
            Mass = 100f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 120000f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            Trajectory = new TrajectoryDef
            {
                MaxLifeTime = 300, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 2000, // voxel phasing if you go above 5100
                MaxTrajectory = 2000f, // Max Distance the projectile or beam can Travel.
                SpeedVariance = Random(start: 0, end: 0), // subtracts value from DesiredSpeed. Be warned, you can make your projectile go backwards.
                RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\RailgunAmmoLarge.mwm", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "LargeShell",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "LargeShell",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal_GatlingGun",
                        ApplyToShield = true,
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0.1f, end: 2.0f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0.1f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 80f, //
                        Width = 0.3f, //
                        Color = Color(red: 20f, green: 20f, blue: 40f, alpha: 1f), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeEnd = 240, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal,
                        DecayTime = 30, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                        Color = Color(red: 15f, green: 15f, blue: 30f, alpha: 1f),
                        CustomWidth = 0.3f,
                        UseColorFade = true,
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "HeavyShellFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 1f,
                HitPlayShield = true,
            },
        };
	  
        private AmmoDef SmallRailgunSabot => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "SmallRailgunAmmo", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Small Railgun Slug", // Name of ammo in terminal, should be different for each ammo type used by the same weapon. Is used by Shrapnel.
            HybridRound = true, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0.081f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 8000f, // Direct damage; one steel plate is worth 100.
            Mass = 25f, // In kilograms; how much force the impact will apply to the target.
            BackKickForce = 30000f, // Recoil. This is applied to the Parent Grid.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            NoGridOrArmorScaling = true,
            Trajectory = new TrajectoryDef
            {
                MaxLifeTime = 300, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                DesiredSpeed = 1000, // voxel phasing if you go above 5100
                MaxTrajectory = 1400f, // Max Distance the projectile or beam can Travel.
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\RailgunAmmo.mwm", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                ShieldHitDraw = false,
                Decals = new DecalDef
                {
                    MaxAge = 3600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "MediumShell",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "MediumShell",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Hit = new ParticleDef
                    {
                        Name = "MaterialHit_Metal_GatlingGun",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0.1f, end: 1.8f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0.1f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 50f, //
                        Width = 0.2f, //
                        Color = Color(red: 20f, green: 20f, blue: 30f, alpha: 1f), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 240, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal,
                        DecayTime = 60, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                        Color = Color(red: 5f, green: 5f, blue: 18f, alpha: 1f),
                        Back = false,
                        CustomWidth = 0.2f,
                        UseWidthVariance = false,
                        UseColorFade = true,
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "MediumShellFlightSound", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "ImpMetalMetalCat0",
                VoxelHitSound = "ImpMetalRockCat0",
                HitPlayChance = 1f,
                HitPlayShield = true,
            },
        };
    }
}


using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AmmoDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef;

namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {

        private AmmoDef Flare => new AmmoDef
        {
            AmmoMagazine = "FlareBox",
            AmmoRound = "Countermeasure Flare",
            HardPointUsable = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    ParticleScale = 1,
                    CustomParticle = "Explosion_Flare",
                    CustomSound = ""
                },
            },
            Trajectory = new TrajectoryDef
            {
                MaxLifeTime = 600,
                AccelPerSec = 50,
                DesiredSpeed = 50,
                SpeedVariance = Random(-15, 15),
                RangeVariance = Random(-20, 20),
                MaxTrajectory = 480,
                GravityMultiplier = 1,
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "Models\\Weapons\\FlareAmmo.mwm",
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "Smoke_Flare",
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                },
            },
            AmmoAudio = FireworkAudio
        };
        private TrajectoryDef FireworkTrajectory => new TrajectoryDef
        {
            MaxLifeTime = 1200,
            AccelPerSec = 60,
            DesiredSpeed = 60,
            SpeedVariance = Random(-30, 30),
            RangeVariance = Random(-20, 20),
            MaxTrajectory = 150,
            GravityMultiplier = 1,
        };

        private GraphicDef FireworkGraphic => new GraphicDef
        {
            ModelName = "Models\\Weapons\\FlareAmmo.mwm",
            VisualProbability = 1f, // %
            ShieldHitDraw = true,
            Particles = new AmmoParticleDef
            {
                Ammo = new ParticleDef
                {
                    Name = "Smoke_Firework",
                    Extras = new ParticleOptionDef
                    {
                        Scale = 1,
                    },
                },
            },
        };

        private AmmoAudioDef FireworkAudio => new AmmoAudioDef
        {
            TravelSound = "FlareFlightSound",
        };

        private AmmoDef FireworksBlue => new AmmoDef
        {
            AmmoMagazine = "FireworksBoxBlue",
            AmmoRound = "Blue Fireworks",
            HardPointUsable = true,            
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    ParticleScale = 1,
                    CustomParticle = "Explosion_Firework_Blue",
                    CustomSound = "WepFireworkExplosion"
                },
            },
            Trajectory = FireworkTrajectory,
            AmmoGraphics = FireworkGraphic,
            AmmoAudio = FireworkAudio
        };
        private AmmoDef FireworksGreen => new AmmoDef
        {
            AmmoMagazine = "FireworksBoxGreen",
            AmmoRound = "Green Fireworks",
            HardPointUsable = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    ParticleScale = 1,
                    CustomParticle = "Explosion_Firework_Green",
                    CustomSound = "WepFireworkExplosion"
                },
            },
            Trajectory = FireworkTrajectory,
            AmmoGraphics = FireworkGraphic,
            AmmoAudio = FireworkAudio
        };
        private AmmoDef FireworksRed => new AmmoDef
        {
            AmmoMagazine = "FireworksBoxRed",
            AmmoRound = "Red Fireworks",
            HardPointUsable = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    ParticleScale = 1,
                    CustomParticle = "Explosion_Firework_Red",
                    CustomSound = "WepFireworkExplosion"
                },
            },
            Trajectory = FireworkTrajectory,
            AmmoGraphics = FireworkGraphic,
            AmmoAudio = FireworkAudio
        };
        private AmmoDef FireworksPink => new AmmoDef
        {
            AmmoMagazine = "FireworksBoxPink",
            AmmoRound = "Pink Fireworks",
            HardPointUsable = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    ParticleScale = 1,
                    CustomParticle = "Explosion_Firework_Pink",
                    CustomSound = "WepFireworkExplosion"
                },
            },
            Trajectory = FireworkTrajectory,
            AmmoGraphics = FireworkGraphic,
            AmmoAudio = FireworkAudio
        };
        private AmmoDef FireworksYellow => new AmmoDef
        {
            AmmoMagazine = "FireworksBoxYellow",
            AmmoRound = "Yellow Fireworks",
            HardPointUsable = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    ParticleScale = 1,
                    CustomParticle = "Explosion_Firework_Yellow",
                    CustomSound = "WepFireworkExplosion"
                },
            },
            Trajectory = FireworkTrajectory,
            AmmoGraphics = FireworkGraphic,
            AmmoAudio = FireworkAudio
        };
        private AmmoDef FireworksRainbow => new AmmoDef
        {
            AmmoMagazine = "FireworksBoxRainbow",
            AmmoRound = "Rainbow Fireworks",
            HardPointUsable = true,
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    ParticleScale = 1,
                    CustomParticle = "Explosion_Firework_Rainbow",
                    CustomSound = "WepFireworkExplosion"
                },
            },
            Trajectory = FireworkTrajectory,
            AmmoGraphics = FireworkGraphic,
            AmmoAudio = FireworkAudio
        };
    }
}
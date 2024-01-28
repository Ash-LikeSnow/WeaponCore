using System.Collections.Generic;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AnimationDef;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.RelMove.MoveType;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.RelMove;
namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        private AnimationDef LargeRailgunAnimation => new AnimationDef
        {
			
            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [PreFire] = new[]{ //This particle fires in the Prefire state, during the 2 second windup.
                                   //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
                       new EventParticle
                       {
                           EmptyNames = Names("barrel_001"), //If you want an effect on your own dummy
                           MuzzleNames = Names("barrel_001"), //If you want an effect on the muzzle
                           StartDelay = 0, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "Muzzle_Flash_RailgunLargeVaRe", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = true, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 1000, //meters
                                   MaxDuration = 0, //ticks 60 = 1 second
                                   Scale = 1, //How chunky the particle is.
                               }
                           }
                       },
                   },
            },

        };
        
        private AnimationDef SmallRailgunAnimation => new AnimationDef
        {
			
            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [PreFire] = new[]{ //This particle fires in the Prefire state, during the 2 second windup.
                                   //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
                       new EventParticle
                       {
                           EmptyNames = Names("barrel_001"), //If you want an effect on your own dummy
                           MuzzleNames = Names("barrel_001"), //If you want an effect on the muzzle
                           StartDelay = 0, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "Muzzle_Flash_RailgunSmallVaRe", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = true, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 1000, //meters
                                   MaxDuration = 0, //ticks 60 = 1 second
                                   Scale = 1, //How chunky the particle is.
                               }
                           }
                       },
                   },
            },

        };
    }
}

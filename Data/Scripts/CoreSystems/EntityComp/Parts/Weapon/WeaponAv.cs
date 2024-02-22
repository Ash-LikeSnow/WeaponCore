using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;
using static CoreSystems.Support.PartAnimation;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;

namespace CoreSystems.Platform
{
    public partial class Weapon
    {
        public void PlayEmissives(PartAnimation animation)
        {
            EmissiveState LastEmissive = new EmissiveState();
            for (int i = 0; i < animation.MoveToSetIndexer.Length; i++)
            {
                EmissiveState currentEmissive;
                if (System.PartEmissiveSet.TryGetValue(animation.EmissiveIds[animation.MoveToSetIndexer[i][(int)Indexer.EmissiveIndex]], out currentEmissive))
                {
                    currentEmissive.CurrentPart = animation.CurrentEmissivePart[animation.MoveToSetIndexer[i][(int)Indexer.EmissivePartIndex]];

                    if (currentEmissive.EmissiveParts != null && LastEmissive.EmissiveParts != null && currentEmissive.CurrentPart == LastEmissive.CurrentPart && currentEmissive.CurrentColor == LastEmissive.CurrentColor && Math.Abs(currentEmissive.CurrentIntensity - LastEmissive.CurrentIntensity) < 0.001)
                        currentEmissive = new EmissiveState();

                    LastEmissive = currentEmissive;


                    if (currentEmissive.EmissiveParts != null && currentEmissive.EmissiveParts.Length > 0)
                    {
                        if (currentEmissive.CycleParts)
                        {
                            animation.Part.SetEmissiveParts(currentEmissive.EmissiveParts[currentEmissive.CurrentPart], currentEmissive.CurrentColor,
                                currentEmissive.CurrentIntensity);
                            if (!currentEmissive.LeavePreviousOn)
                            {
                                var prev = currentEmissive.CurrentPart - 1 >= 0 ? currentEmissive.CurrentPart - 1 : currentEmissive.EmissiveParts
                                    .Length - 1;
                                animation.Part.SetEmissiveParts(currentEmissive.EmissiveParts[prev],
                                    Color.Transparent,
                                    0);
                            }
                        }
                        else
                        {
                            for (int j = 0; j < currentEmissive.EmissiveParts.Length; j++)
                                animation.Part.SetEmissiveParts(currentEmissive.EmissiveParts[j], currentEmissive.CurrentColor, currentEmissive.CurrentIntensity);
                        }
                    }
                }
            }
        }

        public void StopShootingAv(bool burst)
        {
            var stopSounds = !burst || !System.WConst.FireSoundNoBurst;
            if (System.WConst.FireSoundEndDelay > 0 && stopSounds)
                Session.I.FutureEvents.Schedule(StopFiringSound, null, System.WConst.FireSoundEndDelay);
            else if (stopSounds) StopFiringSound(false);

            if (stopSounds)
            {
                if (AvCapable && BarrelRotateEmitter != null && BarrelRotateEmitter.IsPlaying) 
                    StopBarrelRotateSound();
                if (HasHardPointSound)
                    StopHardPointSound();
            }

            BurstAvDelay = !stopSounds;

            StopPreFiringSound();

            StopBarrelAvTick = Session.I.Tick;

            for (int i = 0; i < Muzzles.Length; i++) {
                var muzzle = Muzzles[i];
                MyParticleEffect effect;
                if (Session.I.Av.BeamEffects.TryGetValue(muzzle.UniqueId, out effect)) {
                    effect.Stop();
                    Session.I.Av.BeamEffects.Remove(muzzle.UniqueId);
                }
            }
        }

        private const string AnyStr = "Any";
        internal void PlayParticleEvent(EventTriggers eventTrigger, bool active, double distance, HashSet<string> muzzles)
        {
            if (ParticleEvents.ContainsKey(eventTrigger))
            {
                for (int i = 0; i < ParticleEvents[eventTrigger].Length; i++)
                {
                    var particle = ParticleEvents[eventTrigger][i];

                    if (active && particle.Restart && particle.Triggered) continue;


                    if (muzzles != null)
                    {
                        for (int j = 0; j < particle.MuzzleNames.Length; j++)
                        {
                            if (particle.MuzzleNames[j] == AnyStr || muzzles.Contains(particle.MuzzleNames[j]))
                            {
                                break;
                            }
                        }
                    }

                    if (active && !particle.Playing && distance <= particle.Distance)
                    {
                        particle.PlayTick = Session.I.Tick + particle.StartDelay;
                        Session.I.Av.ParticlesToProcess.Add(particle);
                        particle.Playing = true;
                        particle.Triggered = true;
                    }
                    else if (!active)
                    {
                        if (particle.Playing)
                            particle.Stop = true;

                        particle.Triggered = false;
                    }
                }
            }
        }


        internal void DelayedStart(object o)
        {
            var enabled = o as bool? ?? false;
            var state = enabled ? EventTriggers.Init : EventTriggers.TurnOff;
            EventTriggerStateChanged(state, true);
        }

        internal void EventTriggerStateChanged(EventTriggers state, bool active, HashSet<string> muzzles = null)
        {
            if (Comp.Data.Repo == null || Comp.CoreEntity == null || Comp.CoreEntity.MarkedForClose || Comp.Ai == null || Comp.Platform.State != CorePlatform.PlatformState.Ready && Comp.Platform.State != CorePlatform.PlatformState.Inited) return;
            try
            {
                var session = Session.I;
                var distance = Vector3D.DistanceSquared(session.CameraPos, Comp.CoreEntity.PositionComp.WorldAABB.Center);
                var canPlay = !session.DedicatedServer && 64000000 >= distance; //8km max range, will play regardless of range if it moves PivotPos and is loaded
                switch (state)
                {
                    case EventTriggers.Firing:
                        if (Comp.TypeSpecific == CoreComponent.CompTypeSpecific.Rifle)
                            Comp.HandhelShoot(this, state, active);
                        break;
                    case EventTriggers.Reloading:
                    case EventTriggers.NoMagsToLoad:
                    case EventTriggers.EmptyOnGameLoad:
                        if (Comp.TypeSpecific == CoreComponent.CompTypeSpecific.Rifle && (!Session.I.IsCreative || state == EventTriggers.Reloading))
                        {
                            Comp.HandheldReload(this, state, active);
                        }
                        break;
                }

                if (canPlay)
                    PlayParticleEvent(state, active, distance, muzzles);

                //  Vector3D scopePos, Vector3D scopeDirection, int requestState, bool hasLos, object target, int currentAmmo, int remainingMags, int requestStage
                Func<Vector3D, Vector3D, int, bool, object, int, int, int, bool> shootHandler;
                if (Session.I.ShootHandlers.Count > 0 && (Session.I.ShootHandlers.TryGetValue(Comp.CoreEntity.EntityId, out shootHandler) || Session.I.ShootHandlers.TryGetValue(Comp.TopEntity.EntityId, out shootHandler)))
                {
                    var scope = GetScope.Info;
                    var proceed = shootHandler.Invoke(scope.Position, scope.Direction, active ? 0 : 1, true, ShootRequest.RawTarget ?? Target.TargetObject, ProtoWeaponAmmo.CurrentAmmo, Reload.CurrentMags, (int) state);
                    if (state == EventTriggers.StopFiring && active || !proceed) {
                        if (Comp.ShootRequestDirty)
                            Comp.ClearShootRequest();
                    }
                }

                var monitor = Comp.EventMonitors[PartId];
                var hasMonitors = monitor?.Count > 0;
                if (hasMonitors)
                {
                    for (int i = 0; i < monitor.Count; i++)
                        monitor[i].Invoke((int) state, active);
                }

                var prevRangeEvent = PrevRangeEvent;
                var stopOldEvent = false;
                switch (state)
                {
                    case EventTriggers.TargetRanged100:
                        PrevRangeEvent = state;
                        stopOldEvent = prevRangeEvent != PrevRangeEvent && RangeEventActive;
                        RangeEventActive = active;
                        break;
                    case EventTriggers.TargetRanged75:
                        PrevRangeEvent = state;
                        stopOldEvent = prevRangeEvent != PrevRangeEvent && RangeEventActive;
                        RangeEventActive = active;
                        break;

                    case EventTriggers.TargetRanged50:
                        PrevRangeEvent = state;
                        stopOldEvent = prevRangeEvent != PrevRangeEvent && RangeEventActive;
                        RangeEventActive = active;
                        break;

                    case EventTriggers.TargetRanged25:
                        PrevRangeEvent = state;
                        stopOldEvent = prevRangeEvent != PrevRangeEvent && RangeEventActive;
                        RangeEventActive = active;
                        break;
                }

                if (!AnimationsSet.ContainsKey(state)) return;
                if (AnimationDelayTick < Session.I.Tick)
                    AnimationDelayTick = Session.I.Tick;

                var set = false;
                uint startDelay = 0;

                switch (state)
                {
                    case EventTriggers.TargetRanged100:
                    case EventTriggers.TargetRanged75:
                    case EventTriggers.TargetRanged50:
                    case EventTriggers.TargetRanged25:
                        {
                            var loopCount = stopOldEvent ? 2 : 1;
                            for (int j = 0; j < loopCount; j++)
                            {
                                var xstate = stopOldEvent && j == 0 ? prevRangeEvent : PrevRangeEvent;
                                var xactive = (!stopOldEvent || j != 0) && active;

                                for (int i = 0; i < AnimationsSet[xstate].Length; i++)
                                {
                                    var animation = AnimationsSet[xstate][i];
                                    if (animation == null) continue;

                                    if (xactive && !animation.Running)
                                    {
                                        if (animation.TriggerOnce && animation.Triggered) continue;
                                        animation.Triggered = true;

                                        set = true;

                                        animation.StartTick = session.Tick + animation.MotionDelay;

                                        session.AnimationsToProcess.Add(animation);

                                        animation.Running = true;
                                        animation.CanPlay = canPlay;

                                        if (animation.DoesLoop)
                                            animation.Looping = true;
                                    }
                                    else if (xactive && animation.DoesLoop)
                                        animation.Looping = true;
                                    else if (!xactive)
                                    {
                                        animation.Looping = false;
                                        animation.Triggered = false;
                                    }
                                }
                            }

                            break;
                        }
                    case EventTriggers.StopFiring:
                    case EventTriggers.PreFire:
                    case EventTriggers.Firing:
                        {
                            var addToFiring = AnimationsSet.ContainsKey(EventTriggers.StopFiring) && state == EventTriggers.Firing;

                            for (int i = 0; i < AnimationsSet[state].Length; i++)
                            {
                                var animation = AnimationsSet[state][i];

                                if (active && !animation.Running && (animation.Muzzle == "Any" || (muzzles != null && muzzles.Contains(animation.Muzzle))))
                                {
                                    if (animation.TriggerOnce && animation.Triggered) continue;
                                    animation.Triggered = true;

                                    set = true;

                                    if (animation.Muzzle != "Any" && addToFiring) _muzzlesFiring.Add(animation.Muzzle);

                                    animation.StartTick = session.Tick + animation.MotionDelay;
                                    if (state == EventTriggers.StopFiring)
                                    {
                                        startDelay = AnimationDelayTick - session.Tick;
                                        animation.StartTick += startDelay;
                                    }

                                    Session.I.AnimationsToProcess.Add(animation);
                                    animation.Running = true;
                                    animation.CanPlay = canPlay;

                                    if (animation.DoesLoop)
                                        animation.Looping = true;
                                }
                                else if (active && animation.DoesLoop)
                                    animation.Looping = true;
                                else if (!active)
                                {
                                    animation.Looping = false;
                                    animation.Triggered = false;
                                }
                            }
                            if (active && state == EventTriggers.StopFiring)
                                _muzzlesFiring.Clear();
                            break;
                        }
                    case EventTriggers.Tracking:
                        {
                            for (int x = 0; x < 2; x++)
                            {
                                var statex = x == 0 ? EventTriggers.Tracking : EventTriggers.StopTracking;
                                var activex = statex == EventTriggers.Tracking && active || statex == EventTriggers.StopTracking && !active;

                                for (int i = 0; i < AnimationsSet[statex].Length; i++)
                                {
                                    var animation = AnimationsSet[statex][i];
                                    if (activex)
                                    {
                                        if (animation.TriggerOnce && animation.Triggered) continue;

                                        set = true;
                                        animation.Triggered = true;
                                        animation.CanPlay = canPlay;

                                        startDelay = TrackingDelayTick > session.Tick ? (TrackingDelayTick - session.Tick) : 0;

                                        if (!animation.Running)
                                            animation.PlayTicks[0] = session.Tick + animation.MotionDelay + startDelay;
                                        else
                                            animation.PlayTicks.Add(session.Tick + animation.MotionDelay + startDelay);

                                        Session.I.AnimationsToProcess.Add(animation);
                                        animation.Running = true;

                                        if (animation.DoesLoop)
                                            animation.Looping = true;
                                    }
                                    else
                                    {
                                        animation.Looping = false;
                                        animation.Triggered = false;
                                    }
                                }

                                if (hasMonitors && statex == EventTriggers.StopTracking) {
                                    for (int i = 0; i < monitor.Count; i++)
                                        monitor[i].Invoke((int)statex, activex);
                                }
                            }

                            break;
                        }
                    case EventTriggers.TurnOn:
                    case EventTriggers.TurnOff:
                    case EventTriggers.Init:
                        if (active)
                        {
                            for (int i = 0; i < AnimationsSet[state].Length; i++)
                            {
                                var animation = AnimationsSet[state][i];

                                animation.CanPlay = true;
                                set = true;

                                startDelay = AnimationDelayTick - session.Tick;
                                if (state == EventTriggers.TurnOff)
                                    startDelay += OffDelay;

                                if (!animation.Running)
                                    animation.PlayTicks[0] = session.Tick + animation.MotionDelay + startDelay;
                                else
                                    animation.PlayTicks.Add(session.Tick + animation.MotionDelay + startDelay);

                                animation.Running = true;
                                session.AnimationsToProcess.Add(animation);
                            }
                        }
                        break;
                    case EventTriggers.EmptyOnGameLoad:
                    case EventTriggers.Overheated:
                    case EventTriggers.NoMagsToLoad:
                    case EventTriggers.BurstReload:
                    case EventTriggers.Reloading:
                    case EventTriggers.Homing:
                    case EventTriggers.TargetAligned:
                    case EventTriggers.WhileOn:
                    {
                        for (int i = 0; i < AnimationsSet[state].Length; i++)
                        {
                            var animation = AnimationsSet[state][i];
                            if (animation == null) continue;

                            if (active && !animation.Running)
                            {
                                if (animation.TriggerOnce && animation.Triggered) continue;
                                animation.Triggered = true;

                                set = true;

                                animation.StartTick = session.Tick + animation.MotionDelay;

                                session.AnimationsToProcess.Add(animation);

                                animation.Running = true;
                                animation.CanPlay = canPlay;

                                if (animation.DoesLoop)
                                    animation.Looping = true;
                            }
                            else if (active && animation.DoesLoop)
                                animation.Looping = true;
                            else if (!active)
                            {
                                animation.Looping = false;
                                animation.Triggered = false;
                            }
                        }

                        break;
                    }

                }

                if ((active || state == EventTriggers.Tracking) && set)
                {
                    uint animationLength;

                    LastEvent = state;

                    if (System.PartAnimationLengths.TryGetValue(state, out animationLength))
                    {
                        var delay = session.Tick + animationLength + startDelay;
                        if (delay > AnimationDelayTick)
                            AnimationDelayTick = delay;
                        if (state == EventTriggers.Tracking && delay > TrackingDelayTick)
                            TrackingDelayTick = delay;
                    }

                }
            }
            catch (Exception e)
            {
                Log.Line($"Exception in Event Triggered: {e}");
            }
        }

        public void StartPreFiringSound()
        {
            if (PreFiringEmitter == null)
                return;
            PreFiringEmitter.PlaySound(System.PreFireSoundPair);
        }

        public void StopPreFiringSound()
        {
            if (PreFiringEmitter == null)
                return;

            if (PreFiringEmitter.Loop)
            {
                PreFiringEmitter.StopSound(true);
                PreFiringEmitter.PlaySound(System.PreFireSoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
            }
            else
                PreFiringEmitter.StopSound(false);
        }

        public void StartFiringSound()
        {
            if (FiringEmitter == null || ActiveAmmoDef?.AmmoDef.Const.ShotSoundPair == null)
                return;

            FiringEmitter.PlaySound(ActiveAmmoDef.AmmoDef.Const.ShotSoundPair);
        }

        public void StartHardPointSound()
        {

            if (HardPointEmitter == null || Comp == null || Comp.Cube.MarkedForClose)//Guess at squishing an NRE if a block is destroyed before a queued sound plays
                return;

            if (Environment.CurrentManagedThreadId != Session.I.MainThreadId)
            {
                Comp.Ai.QueuedSounds.Add(new Ai.QueuedSoundEvent {Type = Ai.QueuedSoundEvent.SoundTypes.HardPointStart, Weapon = this});
                return;
            }

            PlayingHardPointSound = true;
            HardPointEmitter.PlaySound(System.HardPointSoundPair);
        }


        public void StopHardPointSound(object o = null)
        {
            if (HardPointEmitter == null || Comp == null || Comp.Cube != null && Comp.Cube.MarkedForClose)//Guess at squishing an NRE if a block is destroyed before a queued sound plays
                return;

            if (Environment.CurrentManagedThreadId != Session.I.MainThreadId)
            {
                Comp.Ai.QueuedSounds.Add(new Ai.QueuedSoundEvent { Type = Ai.QueuedSoundEvent.SoundTypes.HardPointStop, Weapon = this });
                return;
            }

            PlayingHardPointSound = false;

            if (HardPointEmitter.Loop)
            {
                HardPointEmitter.StopSound(true);
                HardPointEmitter.PlaySound(System.HardPointSoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
            }
            else
                HardPointEmitter.StopSound(false);
        }

        public void StopFiringSound(object o = null)
        {
            if (FiringEmitter == null || ActiveAmmoDef?.AmmoDef.Const.ShotSoundPair == null)
                return;

            if (FiringEmitter.Loop)
			{
                FiringEmitter.StopSound(true);
                FiringEmitter.PlaySound(ActiveAmmoDef.AmmoDef.Const.ShotSoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
            }
            else
                FiringEmitter.StopSound(false);
        }

        public void StopReloadSound()
        {
            if (ReloadEmitter == null)
                return;

            if (ReloadEmitter.Loop)
            {
                ReloadEmitter.StopSound(true);
                ReloadEmitter.PlaySound(System.ReloadSoundPairs, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
            }
            else
                ReloadEmitter.StopSound(false);
        }

        public void StopBarrelRotateSound()
        {
            if (BarrelRotateEmitter == null)
                return;

            if (BarrelRotateEmitter.Loop)
			{
				BarrelRotateEmitter.StopSound(true);
                BarrelRotateEmitter.PlaySound(System.BarrelRotateSoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
            }
            else
                BarrelRotateEmitter.StopSound(false);
        }

    }
}

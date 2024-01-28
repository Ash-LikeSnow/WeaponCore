using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;
namespace CoreSystems.Platform
{
    public partial class Weapon 
    {
        internal void Shoot() // Inlined due to keens mod profiler
        {
            try
            {
                var s = Session.I;
                var tick = s.Tick;
                #region Prefire
                var aConst = ActiveAmmoDef.AmmoDef.Const;
                if (_ticksUntilShoot++ < System.DelayToFire) {

                    if (AvCapable && System.PreFireSound && !PreFiringEmitter.IsPlaying)
                        StartPreFiringSound();

                    if (aConst.MustCharge && aConst.Reloadable || System.AlwaysFireFull)
                        FinishShots = true;

                    if (!PreFired)
                        SetPreFire();
                    return;
                } 

                if (PreFired)
                    UnSetPreFire();
                #endregion

                var notReadyToShoot = Session.I.RelativeTime < ShootTime && !MyUtils.IsZero(Session.I.RelativeTime - ShootTime, 1E-04F);
                #region Weapon timing
                if (System.HasBarrelRotation && !SpinBarrel() || notReadyToShoot)
                    return;

                if (PosChangedTick != Session.I.SimulationCount)
                    UpdatePivotPos();

                ShootTime = TicksPerShot * Session.StepConst + Session.I.RelativeTime;

                LastShootTick = tick;
                if (!IsShooting) StartShooting();

                if (Comp.Ai.VelocityUpdateTick != tick) {
                    Comp.Ai.TopEntityVolume.Center = Comp.TopEntity.PositionComp.WorldVolume.Center;
                    Comp.Ai.TopEntityVel = Comp.TopEntity.Physics?.LinearVelocity ?? Vector3D.Zero;
                    Comp.Ai.IsStatic = Comp.TopEntity.Physics?.IsStatic ?? false;
                    Comp.Ai.VelocityUpdateTick = tick;
                }

                #endregion

                #region Projectile Creation

                var wValues = Comp.Data.Repo.Values;

                var rnd = wValues.Targets[PartId].WeaponRandom;
                var pattern = ActiveAmmoDef.AmmoDef.Pattern;
                var loading = System.Values.HardPoint.Loading;
                var reqDeviantMod = ShootRequest.ExtraShotAngle > 0;
                var forceShotDirection = ShootRequest.Type == ApiShootRequest.TargetType.Position;

                FireCounter++;
                List<NewVirtual> vProList = null;
                var selfDamage = 0f;
                LastShootTick = Session.I.Tick;
                Comp.ShootManager.LastShootTick = Session.I.Tick;

                for (int i = 0; i < loading.BarrelsPerShot; i++) {

                    #region Update ProtoWeaponAmmo state
                    if (aConst.Reloadable) {

                        if (ProtoWeaponAmmo.CurrentAmmo == 0) {

                            if (ClientMakeUpShots == 0) {
                                if (s.MpActive && s.IsServer)
                                    s.SendWeaponReload(this);
                                break;
                            }
                        }

                        if (ProtoWeaponAmmo.CurrentAmmo > 0) {

                            --ProtoWeaponAmmo.CurrentAmmo;


                            if (ShootCount > 0)
                                Comp.ShootManager.UpdateShootSync(this);

                            if (ProtoWeaponAmmo.CurrentAmmo == 0) {
                                ClientLastShotId = Reload.StartId;
                            }
                        }
                        else if (ClientMakeUpShots > 0) {
                            --ClientMakeUpShots;

                            if (ShootCount > 0)
                                Comp.ShootManager.UpdateShootSync(this);
                        }

                        if (System.HasEjector && aConst.HasEjectEffect)  {
                            if (ActiveAmmoDef.AmmoDef.Ejection.SpawnChance >= 1 || rnd.AcquireRandom.Range(0f, 1f) >= ActiveAmmoDef.AmmoDef.Ejection.SpawnChance)
                                SpawnEjection();
                        }
                    }
                    else if (ShootCount > 0)
                            Comp.ShootManager.UpdateShootSync(this);

                    #endregion

                    #region Next muzzle
                    var current = NextMuzzle;
                    var muzzle = Muzzles[current];
                    if (muzzle.LastUpdateTick != tick) {
                        var dummy = Dummies[current];
                        var newInfo = dummy.Info;
                        muzzle.Direction = newInfo.Direction;
                        muzzle.UpDirection = newInfo.UpDirection;
                        muzzle.Position = newInfo.Position;
                        muzzle.LastUpdateTick = tick;

                        //if (Comp.Session.DebugVersion && Comp.Ai.AiType == Ai.AiTypes.Player)
                        //    Comp.Session.AddHandHitDebug(muzzle.Position, muzzle.Position + (muzzle.Direction * 10), true);
                    }
                    #endregion

                    if (aConst.HasBackKickForce && !Comp.Ai.IsStatic && !Comp.Ai.ShieldFortified && s.IsServer)
                        Comp.TopEntity.Physics?.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, -muzzle.Direction * ActiveAmmoDef.AmmoDef.Const.BackKickForce, muzzle.Position, Vector3D.Zero);

                    if (PlayTurretAv) {
                        if (System.BarrelEffect1 && muzzle.LastAv1Tick == 0 && !muzzle.Av1Looping) {

                            muzzle.LastAv1Tick = tick;
                            var avBarrel = s.Av.AvEffectPool.Count > 0 ? s.Av.AvEffectPool.Pop() : new RunAv.AvEffect();
                            avBarrel.Weapon = this;
                            avBarrel.Muzzle = muzzle;
                            avBarrel.StartTick = tick;
                            s.Av.Effects1.Add(avBarrel);
                        }
                        if (System.BarrelEffect2 && muzzle.LastAv2Tick == 0 && !muzzle.Av2Looping) {

                            muzzle.LastAv2Tick = tick;
                            var avBarrel = s.Av.AvEffectPool.Count > 0 ? s.Av.AvEffectPool.Pop() : new RunAv.AvEffect();
                            avBarrel.Weapon = this;
                            avBarrel.Muzzle = muzzle;
                            avBarrel.StartTick = tick;
                            s.Av.Effects2.Add(avBarrel);
                        }
                    }

                    if (forceShotDirection)
                        muzzle.Direction = Vector3D.Normalize(ShootRequest.Position - muzzle.Position);

                    for (int j = 0; j < loading.TrajectilesPerBarrel; j++) {

                        #region Pick projectile direction
                        if (System.WConst.DeviateShotAngleRads > 0 || reqDeviantMod) {
                            var dirMatrix = Matrix.CreateFromDir(muzzle.Direction);
                            var rnd1 = rnd.TurretRandom.NextDouble();
                            var rnd2 = rnd.TurretRandom.NextDouble();
                            var deviatePlus = !reqDeviantMod ? System.WConst.DeviateShotAngleRads + System.WConst.DeviateShotAngleRads :  System.WConst.DeviateShotAngleRads + System.WConst.DeviateShotAngleRads + ShootRequest.ExtraShotAngle + ShootRequest.ExtraShotAngle;
                            var deviateMinus = !reqDeviantMod ? System.WConst.DeviateShotAngleRads : System.WConst.DeviateShotAngleRads + ShootRequest.ExtraShotAngle;
                            var randomFloat1 = (float)(rnd1 * deviatePlus - deviateMinus);
                            
                            var randomFloat2 = (float)(rnd2 * MathHelper.TwoPi);
                            var r1Sin = Math.Sin(randomFloat1);
                            muzzle.DeviatedDir = Vector3.TransformNormal(-new Vector3D(r1Sin * Math.Cos(randomFloat2), r1Sin * Math.Sin(randomFloat2), Math.Cos(randomFloat1)), dirMatrix);
                        }
                        else muzzle.DeviatedDir = muzzle.Direction;
                        #endregion

                        #region Pick ProtoWeaponAmmo Pattern
                        var patternIndex = aConst.WeaponPatternCount;

                        if (aConst.WeaponPattern) {

                            if (pattern.Random) {

                                if (pattern.TriggerChance >= 1 || pattern.TriggerChance >= rnd.TurretRandom.NextDouble()) {
                                    patternIndex = rnd.TurretRandom.Range(pattern.RandomMin, pattern.RandomMax);
                                }

                                for (int w = 0; w < aConst.WeaponPatternCount; w++) {
                                    var y = rnd.TurretRandom.Range(0, w + 1);
                                    AmmoShufflePattern[w] = AmmoShufflePattern[y];
                                    AmmoShufflePattern[y] = w;
                                }
                            }
                            else if (pattern.PatternSteps > 0 && pattern.PatternSteps <= aConst.WeaponPatternCount) {

                                patternIndex = pattern.PatternSteps;
                                for (int p = 0; p < aConst.WeaponPatternCount; ++p)
                                    AmmoShufflePattern[p] = (AmmoShufflePattern[p] + patternIndex) % aConst.WeaponPatternCount;
                            }
                        }
                        #endregion

                        #region Generate Projectiles

                        if (!System.ShootBlanks)
                        {
                            for (int k = 0; k < patternIndex; k++)
                            {

                                var ammoPattern = aConst.WeaponPattern ? aConst.AmmoPattern[AmmoShufflePattern[k]] : ActiveAmmoDef.AmmoDef;

                                if (ammoPattern.DecayPerShot >= float.MaxValue) selfDamage = float.MaxValue;
                                else selfDamage += ammoPattern.DecayPerShot;

                                long patternCycle = FireCounter;
                                if (ammoPattern.AmmoGraphics.Lines.Tracer.VisualFadeStart > 0 && ammoPattern.AmmoGraphics.Lines.Tracer.VisualFadeEnd > 0)
                                    patternCycle = ((FireCounter - 1) % ammoPattern.AmmoGraphics.Lines.Tracer.VisualFadeEnd) + 1;

                                if (ammoPattern.Const.VirtualBeams && j == 0)
                                {

                                    if (i == 0)
                                    {
                                        vProList = s.Projectiles.VirtInfoPools.Count > 0 ? s.Projectiles.VirtInfoPools.Pop() : new List<NewVirtual>();
                                        s.Projectiles.NewProjectiles.Add(new NewProjectile { NewVirts = vProList, AmmoDef = ammoPattern, Muzzle = muzzle, PatternCycle = patternCycle, Direction = muzzle.DeviatedDir, Type = NewProjectile.Kind.Virtual });
                                    }

                                    double shotFade;
                                    if (ammoPattern.Const.HasShotFade)
                                    {
                                        if (patternCycle > ammoPattern.AmmoGraphics.Lines.Tracer.VisualFadeStart)
                                            shotFade = MathHelper.Clamp(((patternCycle - ammoPattern.AmmoGraphics.Lines.Tracer.VisualFadeStart)) * ammoPattern.Const.ShotFadeStep, 0, 1);
                                        else if (System.DelayCeaseFire && CeaseFireDelayTick != tick)
                                            shotFade = MathHelper.Clamp(((tick - CeaseFireDelayTick) - ammoPattern.AmmoGraphics.Lines.Tracer.VisualFadeStart) * ammoPattern.Const.ShotFadeStep, 0, 1);
                                        else shotFade = 0;
                                    }
                                    else shotFade = 0;

                                    var maxTrajectory = ammoPattern.Const.MaxTrajectoryGrows && FireCounter < ammoPattern.Trajectory.MaxTrajectoryTime ? ammoPattern.Const.TrajectoryStep * FireCounter : ammoPattern.Const.MaxTrajectory;
                                    var info = s.Projectiles.VirtInfoPool.Count > 0 ? s.Projectiles.VirtInfoPool.Pop() : new ProInfo();
                                    info.AvShot = s.Av.AvShotPool.Count > 0 ? s.Av.AvShotPool.Pop() : new AvShot(s);
                                    info.InitVirtual(this, ammoPattern,  muzzle, maxTrajectory, shotFade);
                                    vProList.Add(new NewVirtual { Info = info, Rotate = !ammoPattern.Const.RotateRealBeam && i == _nextVirtual, Muzzle = muzzle, VirtualId = _nextVirtual });
                                }
                                else
                                {
                                    s.Projectiles.NewProjectiles.Add(new NewProjectile { AmmoDef = ammoPattern, Muzzle = muzzle, PatternCycle = patternCycle, Direction = muzzle.DeviatedDir, Type = NewProjectile.Kind.Normal });
                                    if (aConst.IsDrone || aConst.IsSmart) LiveSmarts++;
                                }
                            }
                        }
                        #endregion
                    }

                    _muzzlesToFire.Add(MuzzleIdToName[current]);

                    if (HeatPShot > 0) {

                        if (!HeatLoopRunning) {
                            s.FutureEvents.Schedule(UpdateWeaponHeat, null, 20);
                            HeatLoopRunning = true;
                        }

                        PartState.Heat += HeatPShot;
                        Comp.CurrentHeat += HeatPShot;
                        if (PartState.Heat >= System.MaxHeat || PartState.Overheated) {
                            OverHeat();
                            break;
                        }
                    }
                    
                    if (i == System.Values.HardPoint.Loading.BarrelsPerShot) NextMuzzle++;

                    NextMuzzle = (NextMuzzle + (System.Values.HardPoint.Loading.SkipBarrels + 1)) % _numOfMuzzles;
                }

                #endregion

                #region Reload and Animation
                EventTriggerStateChanged(state: EventTriggers.Firing, active: true, muzzles: _muzzlesToFire);

                _muzzlesToFire.Clear();
                _nextVirtual = _nextVirtual + 1 < System.Values.HardPoint.Loading.BarrelsPerShot ? _nextVirtual + 1 : 0;

                if (s.IsServer && selfDamage > 0 && !Comp.IsBomb)
                {
                    if (Comp.IsBlock)
                    {
                        IMySlimBlock currCube = Comp.Cube.SlimBlock as IMySlimBlock;
                        if (selfDamage >= float.MaxValue)
                        {
                            currCube.DecreaseMountLevel(currCube.MaxIntegrity, null, true);
                            currCube.ClearConstructionStockpile(null);
                        }
                        else if (selfDamage >= currCube.Integrity) Comp.Cube.CubeGrid.RemoveBlock(Comp.Cube.SlimBlock, true); //Cleaner removal of wep block without a "bang" and deformation of neighbors
                        else currCube.DoDamage(selfDamage, MyDamageType.Grind, true, null, Comp.CoreEntity.EntityId);
                    }
                    else
                        ((IMyDestroyableObject)Comp.TopEntity as IMyCharacter).DoDamage(selfDamage, MyDamageType.Grind, true, null, Comp.CoreEntity.EntityId);
                }

                if (ActiveAmmoDef.AmmoDef.Const.HasShotReloadDelay && System.ShotsPerBurst > 0 && ++ShotsFired == System.ShotsPerBurst)
                {
                    var burstDelay = (uint)System.Values.HardPoint.Loading.DelayAfterBurst;
                    ShotsFired = 0;
                    ShootTime = burstDelay > TicksPerShot ? burstDelay * Session.StepConst + Session.I.RelativeTime : TicksPerShot * Session.StepConst + Session.I.RelativeTime;
                    if (System.Values.HardPoint.Loading.GiveUpAfter)
                        GiveUpTarget();
                }

                if (System.AlwaysFireFull || ActiveAmmoDef.AmmoDef.Const.BurstMode)
                    FinishMode();

                #endregion
            }
            catch (Exception e) { Log.Line($"Error in shoot: {e}", null, true); }
        }

        private void FinishMode()
        {
            if (ActiveAmmoDef.AmmoDef.Const.BurstMode && ++ShotsFired > System.ShotsPerBurst) { // detect when the "first" burst cycle has ended and reset it to shot == 1 so that it can repeat multiple times within a reload window
                ShotsFired = 1;
                EventTriggerStateChanged(EventTriggers.BurstReload, false);
            }

            var outOfShots = ProtoWeaponAmmo.CurrentAmmo == 0 && ClientMakeUpShots == 0;
            var burstReset = ActiveAmmoDef.AmmoDef.Const.BurstMode && ShotsFired == System.ShotsPerBurst;
            var genericReset = !ActiveAmmoDef.AmmoDef.Const.BurstMode && outOfShots;

            if (burstReset) {

                EventTriggerStateChanged(EventTriggers.BurstReload, true);
                var burstDelay =  (uint)System.WConst.DelayAfterBurst;
                ShootTime = burstDelay > TicksPerShot ? burstDelay * Session.StepConst + Session.I.RelativeTime : TicksPerShot * Session.StepConst + Session.I.RelativeTime;
                if (System.WConst.GiveUpAfter)
                     GiveUpTarget();
            }
            else if (System.AlwaysFireFull)
                FinishShots = true;

            if (burstReset || genericReset)
                StopShooting(burstReset && !outOfShots);
        }

        private void GiveUpTarget()
        {
            if (Session.I.IsServer)
            {
                Target.Reset(Session.I.Tick, Target.States.FiredBurst);
                FastTargetResetTick = Session.I.Tick + 1;
            }
        }

        private void OverHeat()
        {
            if (!Session.I.IsClient && Comp.Data.Repo.Values.Set.Overload > 1)
            {
                var dmg = .02f * Comp.MaxIntegrity;
                Comp.Slim.DoDamage(dmg, MyDamageType.Environment, true, null, Comp.TopEntity.EntityId);
            }

            EventTriggerStateChanged(EventTriggers.Overheated, true);
            Comp.ShootManager.EndShootMode(ShootManager.EndReason.Overheat, true);


            if (Session.I.IsServer)
            {
                var wasOver = PartState.Overheated;
                if (!wasOver)
                    OverHeatCountDown = 15;

                PartState.Overheated = true;
                if (Session.I.MpActive && !wasOver)
                    Session.I.SendState(Comp);
            }

        }

        private void UnSetPreFire()
        {
            EventTriggerStateChanged(EventTriggers.PreFire, false);
            _muzzlesToFire.Clear();
            PreFired = false;
            if (AvCapable && System.PreFireSound && PreFiringEmitter.IsPlaying)
                StopPreFiringSound();
        }

        private void SetPreFire()
        {
            var nxtMuzzle = NextMuzzle;
            for (int i = 0; i < System.Values.HardPoint.Loading.BarrelsPerShot; i++)
            {
                _muzzlesToFire.Clear();
                _muzzlesToFire.Add(MuzzleIdToName[NextMuzzle]);
                if (i == System.Values.HardPoint.Loading.BarrelsPerShot) NextMuzzle++;
                nxtMuzzle = (nxtMuzzle + (System.Values.HardPoint.Loading.SkipBarrels + 1)) % _numOfMuzzles;
            }

            EventTriggerStateChanged(EventTriggers.PreFire, true, _muzzlesToFire);

            PreFired = true;
        }

        private void SpawnEjection()
        {
            var eInfo = Ejector.Info;
            var ejectDef = ActiveAmmoDef.AmmoDef.Ejection;
            if (ejectDef.Type == WeaponDefinition.AmmoDef.EjectionDef.SpawnType.Item && Session.I.IsServer)
            {
                var delay = (uint)ejectDef.CompDef.Delay;
                if (delay <= 0)
                    MyFloatingObjects.Spawn(ActiveAmmoDef.AmmoDef.Const.EjectItem, eInfo.Position, eInfo.Direction, MyPivotUp, Comp.TopEntity.Physics, EjectionSpawnCallback);
                else 
                    Session.I.FutureEvents.Schedule(EjectionDelayed, null, delay);
            }
            else if (Session.I.HandlesInput) {
                
                var particle = ActiveAmmoDef.AmmoDef.AmmoGraphics.Particles.Eject;
                var keenStrikesAgain = particle.Offset == Vector3D.MaxValue;

                MyParticleEffect ejectEffect;
                var matrix = !keenStrikesAgain ? MatrixD.CreateTranslation(eInfo.Position) : MatrixD.CreateWorld(eInfo.Position, eInfo.Direction, eInfo.UpDirection); ;
                
                if (MyParticlesManager.TryCreateParticleEffect(particle.Name, ref matrix, ref eInfo.Position, uint.MaxValue, out ejectEffect)) {
                    ejectEffect.UserScale = particle.Extras.Scale;
                    ejectEffect.Velocity = eInfo.Direction * ActiveAmmoDef.AmmoDef.Ejection.Speed;
                }
            }
        }

        private void EjectionDelayed(object o)
        {
            if (Comp.IsWorking && ActiveAmmoDef?.AmmoDef != null && ActiveAmmoDef.AmmoDef.Ejection.Type == WeaponDefinition.AmmoDef.EjectionDef.SpawnType.Item && !Ejector.NullEntity) 
                MyFloatingObjects.Spawn(ActiveAmmoDef.AmmoDef.Const.EjectItem, Ejector.Info.Position, Ejector.Info.Direction, MyPivotUp, Comp.TopEntity?.Physics, EjectionSpawnCallback);
        }

        private void EjectionSpawnCallback(MyEntity entity)
        {
            if (ActiveAmmoDef?.AmmoDef != null) {
                
                var ejectDef = ActiveAmmoDef.AmmoDef.Ejection;
                var itemTtl = ejectDef.CompDef.ItemLifeTime;

                if (ejectDef.Speed > 0) 
                    SetSpeed(entity);

                if (itemTtl > 0)
                    Session.I.FutureEvents.Schedule(RemoveEjection, entity, (uint)itemTtl);
            }
        }

        private void SetSpeed(object o)
        {
            var entity = (MyEntity)o;

            if (entity?.Physics != null && ActiveAmmoDef?.AmmoDef != null && !entity.MarkedForClose) {
                
                var ejectDef = ActiveAmmoDef.AmmoDef.Ejection;
                entity.Physics.SetSpeeds(Ejector.CachedDir * (ejectDef.Speed), Vector3.Zero);
            }
        }

        private static void RemoveEjection(object o)
        {
            var entity = (MyEntity) o;
            
            if (entity?.Physics != null) {
                using (entity.Pin())  {
                    if (!entity.MarkedForClose && !entity.Closed)
                        entity.Close();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Projectiles;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;
using static VRageRender.MyBillboard;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static CoreSystems.Projectiles.Projectile;

namespace CoreSystems.Support
{
    public class AvShot
    {
        public AvShot(Session session)
        {
            Session = session;
        }

        internal readonly Session Session;
        internal readonly Queue<Shrink> TracerShrinks = new Queue<Shrink>(8);
        internal readonly MyQueue<AfterTrail> TrailSteps = new MyQueue<AfterTrail>();
        internal WeaponDefinition.AmmoDef AmmoDef;
        internal MyEntity PrimeEntity;
        internal MyEntity TriggerEntity;
        internal MyEntity3DSoundEmitter FireEmitter;
        internal MyEntity3DSoundEmitter TravelEmitter;

        internal List<Vector3D> Offsets;
        internal MyParticleEffect AmmoEffect;
        internal MyParticleEffect FieldEffect;
        internal Weapon Weapon;
        internal bool TravelSound;
        internal bool HasTravelSound;
        internal bool HitSoundActive;
        internal bool HitSoundInitted;
        internal bool Triggered;
        internal bool Cloaked;
        internal bool Active;
        internal bool ShrinkInited;
        internal bool TrailActivated;
        internal bool Hitting;
        internal bool Back;
        internal bool LastStep;
        internal bool IsFragment;
        internal bool AmmoParticleStopped;
        internal bool AmmoParticleInited;
        internal bool FieldParticleStopped;
        internal bool FieldParticleInited;
        internal bool ModelOnly;
        internal bool LastHitShield;
        internal bool ForceHitParticle;
        internal bool HitParticleActive;
        internal bool MarkForClose;
        internal bool ProEnded;
        internal bool AccelClearance;
        internal bool HasModel;
        internal double MaxTracerLength;
        internal double MaxGlowLength;
        internal double StepSize;
        internal double ShortStepSize;
        internal double TotalLength;
        internal double TracerWidth;
        internal double SegmentWidth;
        internal double TrailWidth;
        internal double VisualLength;
        internal double MaxSpeed;
        internal double TracerLengthSqr;
        internal double EstTravel;
        internal double ShortEstTravel;
        internal double MaxTrajectory;
        internal double ShotFade;
        internal float TrailScaler;
        internal float TrailShrinkSize;
        internal float DistanceToLine;
        internal ulong UniqueMuzzleId;
        internal int LifeTime;
        internal int MuzzleId;
        internal int TracerStep;
        internal int TracerSteps;
        internal int DecayTime;
        internal int ActiveBillBoards;
        internal uint LastTick;
        internal uint LastHit = uint.MaxValue / 2;
        internal ParticleState HitParticle;
        internal TracerState Tracer;
        internal TrailState Trail;
        internal ModelState Model;
        internal Screen OnScreen;
        internal MatrixD OffsetMatrix;
        internal Vector3D Origin;
        internal Vector3D OriginUp;
        internal Vector3D OriginDir;
        internal Vector3D Direction;
        internal Vector3D VisualDir;
        internal Vector3D HitVelocity;
        internal Vector3D ShootVelStep;
        internal Vector3D TracerFront;
        internal Vector3D TracerBack;
        internal Vector4 Color;
        internal Vector4 SegmentColor;
        internal Vector4 FgFactionColor;
        internal Vector4 BgFactionColor;
        internal Hit Hit;
        internal AvClose EndState;
        internal MatrixD PrimeMatrix = MatrixD.Identity;
        internal BoundingSphereD ModelSphereCurrent;
        internal MatrixD TriggerMatrix = MatrixD.Identity;
        internal BoundingSphereD TestSphere = new BoundingSphereD(Vector3D.Zero, 200f);

        public bool SegmentGaped;
        public bool TextureReverse;
        public int TextureIdx = -1;
        public int StageIdx = -1;
        public uint TextureLastUpdate;
        public double SegmentLenTranserved = 1;
        public double SegMeasureStep;

        internal enum ParticleState
        {
            None,
            Custom,
            Dirty,
        }

        internal enum TracerState
        {
            Off,
            Full,
            Grow,
            Shrink,
        }

        internal enum ModelState
        {
            None,
            Exists,
        }

        internal enum TrailState
        {
            Off,
            Front,
            Back,
        }

        internal enum Screen // Tracer includes Tail;
        {
            None,
            ModelOnly,
            ProxyDraw,
            InProximity,
            Tracer,
            Trail,
        }

        #region Run
        internal void Init(ProInfo info, double firstStepSize, double maxSpeed, ref Vector3D originDir)
        {
            AmmoDef = info.AmmoDef;
            IsFragment = info.IsFragment;
            Model = (info.AmmoDef.Const.PrimeModel || info.AmmoDef.Const.TriggerModel) ? Model = ModelState.Exists : Model = ModelState.None;
            Origin = info.Origin;
            OriginUp = info.OriginUp;
            MaxTracerLength = info.TracerLength;
            MuzzleId = info.MuzzleId;
            UniqueMuzzleId = info.UniqueMuzzleId;
            MaxSpeed = maxSpeed;
            var useShooterVel = !info.IsFragment && !AmmoDef.Const.AvDropVelocity;
            ShootVelStep = useShooterVel ? info.ShooterVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS : Vector3D.Zero;
            Weapon = info.Weapon;

            if (Weapon.Comp.Ai != null) {
                FgFactionColor = Weapon.Comp.Ai.FgFactionColor;
                BgFactionColor = Weapon.Comp.Ai.BgFactionColor;
            }

            MaxTrajectory = info.MaxTrajectory;
            ShotFade = info.ShotFade;
            ShrinkInited = false;
            OriginDir = originDir;
            StageIdx = info.Storage.RequestedStage;
            HasModel = AmmoDef.Const.PrimeModel || AmmoDef.Const.TriggerModel;
            var defaultDecayTime = AmmoDef.Const.DecayTime;

            if (defaultDecayTime > 1 && Session.ClientAvLevel > 0)
            {
                if (AmmoDef.Const.RareTrail) {
                    if (Session.ClientAvLevel > 7 && AmmoDef.Const.LongTrail) {
                        var div = Session.ClientAvLevel - 7;
                        DecayTime = MathHelper.Clamp(defaultDecayTime / div, defaultDecayTime / 3, int.MaxValue);
                    }
                    else DecayTime = defaultDecayTime;
                }
                else if (AmmoDef.Const.ShortTrail)
                    DecayTime = MathHelper.Clamp(defaultDecayTime - Session.ClientAvLevel, 1, int.MaxValue);
                else if (AmmoDef.Const.TinyTrail && Session.ClientAvLevel > 5)
                    DecayTime = MathHelper.Clamp(defaultDecayTime + 5 - Session.ClientAvLevel, 1, int.MaxValue);
                else if (AmmoDef.Const.LongTrail)
                    DecayTime = MathHelper.Clamp(defaultDecayTime / Session.ClientAvDivisor, 1, int.MaxValue);
                else
                    DecayTime = defaultDecayTime;
            }
            else 
                DecayTime = defaultDecayTime;

            if (AmmoDef.Const.DrawLine) Tracer = !AmmoDef.Const.IsBeamWeapon && firstStepSize < MaxTracerLength && !MyUtils.IsZero(firstStepSize - MaxTracerLength, 1E-01F) ? TracerState.Grow : TracerState.Full;
            else Tracer = TracerState.Off;

            if (AmmoDef.Const.Trail)
            {
                MaxGlowLength = MathHelperD.Clamp(DecayTime * MaxSpeed * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, 0.1f, MaxTrajectory);
                Trail = AmmoDef.AmmoGraphics.Lines.Trail.Back ? TrailState.Back : Trail = TrailState.Front;
                TrailShrinkSize = !AmmoDef.AmmoGraphics.Lines.Trail.UseColorFade ? AmmoDef.Const.TrailWidth / DecayTime : 1f / DecayTime;
                Back = Trail == TrailState.Back;
            }
            else Trail = TrailState.Off;
            TotalLength = MathHelperD.Clamp(MaxTracerLength + MaxGlowLength, 0.1f, MaxTrajectory);

            AvInfoCache infoCache;
            if (AmmoDef.Const.IsBeamWeapon && AmmoDef.Const.TracerMode != AmmoConstants.Texture.Normal && Session.AvShotCache.TryGetValue(info.UniqueMuzzleId, out infoCache))
                UpdateCache(infoCache);
        }

        internal static void DeferedAvStateUpdates()
        {
            var s = Session.I;
            var drawCnt = s.Projectiles.DeferedAvDraw.Count;
            for (int x = 0; x < drawCnt; x++)
            {
                var d = s.Projectiles.DeferedAvDraw[x];
                var a = d.AvShot;
                var storage = d.Info.Storage;
                var stage = storage.RequestedStage;
                var aConst = a.AmmoDef.Const;
                var triggerGrowthSteps = d.Info.TriggerGrowthSteps;
                var lineEffect = aConst.Trail || aConst.DrawLine;
                var saveHit = d.Hit;

                ++a.LifeTime;
                a.LastTick = s.Tick;
                a.AccelClearance = !aConst.AccelClearance || storage.SmartReady;

                var createdPrimeEntity = false;
                if (aConst.PrimeModel && a.PrimeEntity == null) {
                    
                    var stageInactive = stage <= -1 || stage >= aConst.ApproachesCount;
                    ApproachConstants def = stageInactive ? null : a.AmmoDef.Const.Approaches[stage];
                    a.PrimeEntity = def == null || !def.AlternateModel ? aConst.PrimeEntityPool.Get() : aConst.Approaches[stage].ModelPool.Get(); 
                    a.ModelSphereCurrent.Radius = a.PrimeEntity.PositionComp.WorldVolume.Radius * 2;
                    createdPrimeEntity = true;
                }

                if (aConst.TriggerModel && a.TriggerEntity == null) {
                    a.TriggerEntity = Session.I.TriggerEntityPool.Get();
                    if (a.TriggerEntity.PositionComp.WorldVolume.Radius * 2 > a.ModelSphereCurrent.Radius)
                        a.ModelSphereCurrent.Radius = a.TriggerEntity.PositionComp.WorldVolume.Radius * 2;
                }

                var stagesOver = aConst.ApproachesCount == stage && a.StageIdx == -1;
                if (a.StageIdx != stage && !stagesOver && aConst.HasApproaches)
                    a.StageChange(stage, createdPrimeEntity);

                if (!saveHit)
                    a.ShortStepSize = a.StepSize;

                a.EstTravel = a.StepSize * a.LifeTime;

                a.ShortEstTravel = MathHelperD.Clamp((a.EstTravel - a.StepSize) + a.ShortStepSize, 0, double.MaxValue);
                
                if (aConst.IsSmart || aConst.IsDrone || aConst.IsBeamWeapon && aConst.ConvergeBeams)
                    a.VisualDir = d.Direction;
                else if (a.LifeTime == 1)
                    a.VisualDir = a.OriginDir;
                else if (!MyUtils.IsEqual(d.Direction, a.Direction) && !saveHit) {
                    var relativeDifference = (d.TracerFront - a.TracerFront) - a.ShootVelStep;
                    Vector3D.Normalize(ref relativeDifference, out a.VisualDir);
                }
                a.Direction = d.Direction;

                a.TracerFront = d.TracerFront;
                a.TracerBack = a.TracerFront + (-a.VisualDir * a.VisualLength);
                a.OnScreen = Screen.None; // clear OnScreen

                if (a.ModelOnly)
                {
                    a.ModelSphereCurrent.Center = a.TracerFront;
                    if (a.Triggered)
                        a.ModelSphereCurrent.Radius = triggerGrowthSteps < aConst.EwarRadius ? a.TriggerMatrix.Scale.AbsMax() : aConst.EwarRadius;

                    if (s.Camera.IsInFrustum(ref a.ModelSphereCurrent))
                        a.OnScreen = Screen.ModelOnly;
                }
                else if (lineEffect || aConst.AmmoParticle)
                {
                    var rayTracer = new RayD(a.TracerBack, a.VisualDir);
                    var rayTrail = new RayD(a.TracerFront + (-a.VisualDir * a.ShortEstTravel), a.VisualDir);

                    double? dist;
                    s.CameraFrustrum.Intersects(ref rayTracer, out dist);

                    if (aConst.AlwaysDraw)
                        a.OnScreen = Screen.ProxyDraw;

                    if (dist != null && dist <= a.VisualLength)
                        a.OnScreen = Screen.Tracer;
                    else if (aConst.Trail)
                    {
                        s.CameraFrustrum.Intersects(ref rayTrail, out dist);
                        if (dist != null && dist <= a.ShortEstTravel + a.ShortStepSize + a.MaxGlowLength)
                            a.OnScreen = Screen.Trail;
                    }


                    if (a.OnScreen != Screen.None && !a.TrailActivated && aConst.Trail) a.TrailActivated = true;

                    if (a.OnScreen == Screen.None && a.TrailActivated) a.OnScreen = Screen.Trail;

                    if (a.Model != ModelState.None)
                    {
                        a.ModelSphereCurrent.Center = a.TracerFront;
                        if (a.Triggered)
                            a.ModelSphereCurrent.Radius = triggerGrowthSteps < aConst.EwarRadius ? a.TriggerMatrix.Scale.AbsMax() : aConst.EwarRadius;

                        if (a.OnScreen == Screen.None && s.Camera.IsInFrustum(ref a.ModelSphereCurrent))
                            a.OnScreen = Screen.ModelOnly;
                    }
                }

                if (a.OnScreen == Screen.None || a.OnScreen == Screen.ProxyDraw)
                {
                    if (aConst.AmmoParticle && aConst.AmmoParticleNoCull)
                    {
                        a.OnScreen = a.ModelOnly ? Screen.ModelOnly : Screen.Trail;
                    }
                    else
                    {
                        a.TestSphere.Center = a.TracerFront;
                        if (s.Camera.IsInFrustum(ref a.TestSphere))
                            a.OnScreen = Screen.InProximity;
                        else if (Vector3D.DistanceSquared(a.TracerFront, s.CameraPos) <= 225)
                            a.OnScreen = Screen.InProximity;
                    }
                }

                if (a.MuzzleId == -1)
                    return;

                if (saveHit)
                {
                    a.HitVelocity = a.Hit.HitVelocity;
                    a.Hitting = !a.ShrinkInited && a.ProEnded;
                    a.HitEffects();
                    a.LastHit = s.Tick;
                }
                a.LastStep = a.Hitting || MyUtils.IsZero(a.MaxTrajectory - a.ShortEstTravel, 1E-01F);

                if (aConst.DrawLine)
                {
                    if (aConst.IsBeamWeapon || !saveHit && MyUtils.IsZero(a.MaxTracerLength - a.VisualLength, 1E-01F))
                    {
                        a.Tracer = TracerState.Full;
                    }
                    else if (a.Tracer != TracerState.Off && a.VisualLength <= 0)
                    {
                        a.Tracer = TracerState.Off;
                    }
                    else if (a.Hitting  && !a.ModelOnly && lineEffect && a.VisualLength / a.StepSize > 1 && !MyUtils.IsZero(a.EstTravel - a.ShortEstTravel))
                    {
                        a.Tracer = TracerState.Shrink;
                        a.TotalLength = MathHelperD.Clamp(a.VisualLength + a.MaxGlowLength, 0.1f, Vector3D.Distance(a.Origin, a.TracerFront));
                    }
                    else if (a.Tracer == TracerState.Grow && a.LastStep)
                    {
                        a.Tracer = TracerState.Full;
                    }
                }

                var lineOnScreen = a.OnScreen > (Screen)2;

                if (!a.Active && (a.OnScreen != Screen.None || a.HitSoundInitted || a.TravelSound || aConst.AmmoParticleNoCull || saveHit && aConst.HitParticleNoCull || aConst.FieldParticle && aConst.FieldParticleNoCull)) {
                    a.Active = true;
                    s.Av.AvShots.Add(a);
                }
                
                if (lineEffect && (a.Active || lineOnScreen))
                    a.LineVariableEffects();

                if (a.Tracer != TracerState.Off && lineOnScreen)
                {
                    if (a.Tracer == TracerState.Shrink && !a.ShrinkInited)
                        a.Shrink();
                    else if (aConst.IsBeamWeapon && aConst.HitParticle && !(a.MuzzleId != 0 && (aConst.ConvergeBeams || aConst.OneHitParticle)))
                    {
                        MyParticleEffect effect;
                        if (a.Hitting)
                        {
                            ContainmentType containment;
                            s.CameraFrustrum.Contains(ref a.Hit.SurfaceHit, out containment);
                            if (containment != ContainmentType.Disjoint) a.RunBeam();
                        }
                        else if (s.Av.BeamEffects.TryGetValue(a.UniqueMuzzleId, out effect))
                        {
                            effect.Stop();
                            s.Av.BeamEffects.Remove(a.UniqueMuzzleId);
                        }
                    }

                    if (aConst.OffsetEffect)
                        a.PrepOffsetEffect(a.TracerFront, a.VisualDir, a.VisualLength);
                }

                var backAndGrowing = a.Back && a.Tracer == TracerState.Grow;
                if (a.Trail != TrailState.Off && !backAndGrowing && (lineOnScreen || aConst.TrailAlwaysDraw))
                    a.RunTrail(null, false, saveHit);

                if (aConst.AmmoParticle && a.Active)
                {
                    if (a.OnScreen != Screen.None && a.AccelClearance)
                    {
                        if ((a.AmmoParticleStopped || !a.AmmoParticleInited))
                            a.PlayAmmoParticle();
                    }
                    else if (!a.AmmoParticleStopped && a.AmmoEffect != null)
                        a.DisposeAmmoEffect(false, true);
                }

                if (aConst.FieldParticle && a.Active)
                {
                    if (a.OnScreen != Screen.None)
                    {
                        if ((a.FieldParticleStopped || !a.FieldParticleInited))
                            a.PlayFieldParticle();
                    }
                    else if (!a.FieldParticleStopped && a.FieldEffect != null)
                        a.DisposeFieldEffect(false, true);
                }

                a.Hitting = false;
            }
            s.Projectiles.DeferedAvDraw.Clear();
        }

        internal void RunTrail(Shrink shrink, bool shrinking = false, bool hit = false)
        {
            var trailCount = TrailSteps.Count;
            var firstStep = trailCount == 0;
            var onlyStep = firstStep && LastStep;
            var extEnd = !Back && Hitting;
            var extStart = Back && firstStep && VisualLength < ShortStepSize;
            Vector3D frontPos;
            Vector3D backPos;
            var stopVel = shrinking || hit;
            var velStep = !stopVel ? ShootVelStep : Vector3D.Zero;
            if (shrinking)
            {
                frontPos = shrink.NewFront;
                backPos = !shrink.Last ? shrink.NewFront : TracerFront;
            }
            else
            {
                var futureStep = (VisualDir * ShortStepSize);
                var pastStep = (-VisualDir * ShortStepSize);
                if (!Back) futureStep -= velStep;
                frontPos = Back && !onlyStep ? TracerBack + futureStep : TracerFront;
                backPos = Back && !extStart ? TracerBack : TracerFront + pastStep;
            }

            if (trailCount <= DecayTime)
            {
                var av = Session.Av;
                var trail = av.Trails.Count > 0 ? av.Trails.Pop() : new AfterTrail();
                trail.TailPos = backPos;
                TrailSteps.Enqueue(trail);
                ++trailCount;
            }
            var idxStart = trailCount - 1;
            var idxEnd = 0;
            for (int i = idxStart; i >= idxEnd; i--)
            {
                var t = TrailSteps[i];

                if (i != idxEnd)
                {
                    var extend = extEnd && i == idxStart;
                    t.Parent = TrailSteps[i - 1];
                    t.Line = new LineD(extend ? t.Parent.TailPos: t.Parent.TailPos += velStep, extend ? TracerFront + velStep : t.TailPos);
                }
                else if (i != idxStart)
                    t.Line = new LineD(t.Line.From + velStep, t.TailPos);
                else
                    t.Line = new LineD(frontPos, backPos);
            }
        }

        internal void Shrink()
        {
            ShrinkInit();
            var rnd = AmmoDef.Const.Random;
            for (int i = 0; i < TracerSteps; i++)
            {
                var last = (i == TracerSteps - 1);
                var shrink = GetLine();
                if (shrink != null)
                {

                    var color = AmmoDef.AmmoGraphics.Lines.Tracer.Color;
                    if (AmmoDef.Const.LineColorVariance)
                    {
                        var cv = AmmoDef.AmmoGraphics.Lines.ColorVariance;
                        // gross inlined random
                        var tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        var tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var randomValue = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (cv.End - cv.Start) + cv.Start;

                        color.X *= randomValue;
                        color.Y *= randomValue;
                        color.Z *= randomValue;
                    }

                    if (ShotFade > 0)
                        color *= (float)MathHelperD.Clamp(1d - ShotFade, 0.005d, 1d);

                    var width = AmmoDef.AmmoGraphics.Lines.Tracer.Width;
                    if (AmmoDef.Const.LineWidthVariance)
                    {
                        var wv = AmmoDef.AmmoGraphics.Lines.WidthVariance;
                        // gross inlined random
                        var tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        var tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var randomValue = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (wv.End - wv.Start) + wv.Start;
                        width += randomValue;
                    }

                    width = (float)Math.Max(width, 0.10f * Session.ScaleFov * (DistanceToLine / 100));
                    shrink.Color = color;
                    shrink.Thickness = width;
                    shrink.Last = last;
                    TracerShrinks.Enqueue(shrink);
                }
            }
        }

        private void ShrinkInit()
        {
            ShrinkInited = true;

            var fractualSteps = VisualLength / StepSize;
            TracerSteps = (int)Math.Floor(fractualSteps);
            TracerStep = TracerSteps;
            if (TracerSteps <= 0 || fractualSteps < StepSize && !MyUtils.IsZero(fractualSteps - StepSize, 1E-01F))
                Tracer = TracerState.Off;
        }

        internal Shrink GetLine()
        {
            if (TracerStep > 0)
            {
                Hit.LastHit += ShootVelStep;
                var newTracerFront = Hit.LastHit + -(VisualDir * (TracerStep * StepSize));
                var reduced = TracerStep-- * StepSize;
                if (reduced >= 0.1)
                {
                    var av = Session.Av;
                    var shrink = av.Shrinks.Count > 0 ? av.Shrinks.Pop() : new Shrink();
                    shrink.NewFront = newTracerFront;
                    shrink.Length = (float) reduced;
                    return shrink;
                }
            }
            return null;
        }
        #endregion

        internal void LineVariableEffects()
        {
            var aConst = AmmoDef.Const;
            var rnd = aConst.Random;

            if (aConst.TracerMode != AmmoConstants.Texture.Normal && TextureLastUpdate != Session.Tick)
            {
                if (Session.Tick - TextureLastUpdate > 1)
                    AmmoInfoClean();

                TextureLastUpdate = Session.Tick;

                switch (aConst.TracerMode) {
                    case AmmoConstants.Texture.Resize:
                        var wasGapped = SegmentGaped;
                        var segSize = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation;
                        var thisLen = wasGapped ? segSize.SegmentGap : segSize.SegmentLength;
                        var oldmStep = SegMeasureStep;

                        if (oldmStep > thisLen) {
                            wasGapped = !wasGapped && segSize.SegmentGap > 0;
                            SegmentGaped = wasGapped;
                            SegMeasureStep = 0;
                        }
                        SegMeasureStep += aConst.SegmentStep;
                        SegmentLenTranserved = wasGapped ? MathHelperD.Clamp(segSize.SegmentGap, 0, Math.Min(SegMeasureStep, segSize.SegmentGap)) : MathHelperD.Clamp(segSize.SegmentLength, 0, Math.Min(SegMeasureStep, segSize.SegmentLength));
                        break;
                    case AmmoConstants.Texture.Cycle:
                    case AmmoConstants.Texture.Wave:
                        if (aConst.TracerMode == AmmoConstants.Texture.Cycle) {
                            var current = TextureIdx;
                            if (current + 1 < aConst.TracerTextures.Length)
                                TextureIdx = current + 1;
                            else
                                TextureIdx = 0;
                        }
                        else {
                            var current = TextureIdx;
                            if (!TextureReverse) {
                                if (current + 1 < aConst.TracerTextures.Length)
                                    TextureIdx = current + 1;
                                else {
                                    TextureReverse = true;
                                    TextureIdx = current - 1;
                                }
                            }
                            else {
                                if (current - 1 >= 0)
                                    TextureIdx = current - 1;
                                else {
                                    TextureReverse = false;
                                    TextureIdx = current + 1;
                                }
                            }
                        }
                        break;
                    case AmmoConstants.Texture.Chaos:
                        TextureIdx = MyUtils.GetRandomInt(0, aConst.TracerTextures.Length);
                        break;
                }

                if (aConst.IsBeamWeapon)
                    Session.AvShotCache[UniqueMuzzleId] = new AvInfoCache {SegMeasureStep = SegMeasureStep, SegmentGaped = SegmentGaped, SegmentLenTranserved = SegmentLenTranserved, TextureIdx = TextureIdx, TextureLastUpdate = TextureLastUpdate, TextureReverse = TextureReverse};
            }

            var skipTracerFactionColor = aConst.TracerFactionColor == FactionColor.DontUse || FgFactionColor == Vector4.Zero && BgFactionColor == Vector4.Zero;
            var skipSegFactionColor = aConst.SegFactionColor == FactionColor.DontUse || Weapon.Comp.Ai.FgFactionColor == Vector4.Zero && Weapon.Comp.Ai.BgFactionColor == Vector4.Zero;

            var color = skipTracerFactionColor ? aConst.LinearTracerColor : aConst.TracerFactionColor == FactionColor.Foreground ? FgFactionColor : BgFactionColor;
            var segmentColor = skipSegFactionColor ? aConst.LinearSegmentColor : aConst.SegFactionColor == FactionColor.Foreground ? FgFactionColor : BgFactionColor;
            if (aConst.LineColorVariance && skipTracerFactionColor)
            {
                var tracerStart = aConst.LinearTracerColorStart;
                var tracerEnd = aConst.LinearTracerColorEnd;
                // gross inlined random
                var tempX = rnd.Y;
                rnd.X ^= rnd.X << 23; 
                var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                var tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;
                
                var result = XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ);

                Vector4.Lerp(ref tracerStart, ref tracerEnd, (float)result, out color);
                if (aConst.TracerMode == AmmoConstants.Texture.Resize && AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.UseLineVariance)
                {
                    var segStart = aConst.LinearSegmentColorStart;
                    var segEnd = aConst.LinearSegmentColorEnd;
                    Vector4.Lerp(ref segStart, ref segEnd, (float)result, out segmentColor);
                }
            }

            if (aConst.SegmentColorVariance && skipSegFactionColor)
            {
                // gross inlined random
                var tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                var tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                var result = XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ);

                var segStart = aConst.LinearSegmentColorStart;
                var segEnd = aConst.LinearSegmentColorEnd;
                Vector4.Lerp(ref segStart, ref segEnd, (float)result, out segmentColor);
            }

            Color = color;
            SegmentColor = segmentColor;
            var tracerWidth = AmmoDef.AmmoGraphics.Lines.Tracer.Width;
            var trailWidth = aConst.TrailWidth;
            if (aConst.LineWidthVariance)
            {
                var wv = AmmoDef.AmmoGraphics.Lines.WidthVariance;
                // gross inlined random
                var tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                var tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                var randomValue = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (wv.End - wv.Start) + wv.Start;
                tracerWidth += randomValue;
                if (AmmoDef.AmmoGraphics.Lines.Trail.UseWidthVariance)
                    trailWidth += randomValue;
            }

            var longBeam = aConst.IsBeamWeapon && Vector3D.DistanceSquared(TracerFront, TracerBack) > 640000; //800meters
            var checkPos = TracerFront + (!longBeam ? -VisualDir * TotalLength : -VisualDir * (TotalLength - MathHelperD.Clamp(DistanceToLine * 6, DistanceToLine, MaxTrajectory * 0.5)));

            var closestPointOnLine = MyUtils.GetClosestPointOnLine(ref TracerFront, ref checkPos, ref Session.CameraPos);
            DistanceToLine = (float)Vector3D.Distance(closestPointOnLine, Session.CameraMatrix.Translation);

            double scale = 0.1f;
            var widthScaler = !Session.GunnerBlackList ? 1f : (Session.ScaleFov * 1.3f);

            TracerWidth = MathHelperD.Clamp(scale * Session.ScaleFov * (DistanceToLine / 100), tracerWidth * widthScaler, double.MaxValue);
            TrailWidth = MathHelperD.Clamp(scale * Session.ScaleFov * (DistanceToLine / 100), trailWidth * widthScaler, double.MaxValue);

            TrailScaler = ((float)TrailWidth / trailWidth);

            var seg = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation;
            SegmentWidth = seg.WidthMultiplier > 0 ? TracerWidth * seg.WidthMultiplier : TracerWidth;

            if (aConst.SegmentWidthVariance)
            {
                var wv = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.WidthVariance;

                // gross inlined random
                var tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                var tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                var randomValue = XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (wv.End - wv.Start) + wv.Start;
                SegmentWidth += randomValue;
            }
        }

        internal void PrepOffsetEffect(Vector3D tracerStart, Vector3D direction, double tracerLength)
        {
            var up = MatrixD.Identity.Up;
            var startPos = tracerStart + -(direction * tracerLength);
            MatrixD.CreateWorld(ref startPos, ref direction, ref up, out OffsetMatrix);
            TracerLengthSqr = tracerLength * tracerLength;
            var maxOffset = AmmoDef.Const.MaxOffset;
            var negMaxOffset = maxOffset * -1;
            var minLength = AmmoDef.Const.MinOffsetLength;

            var dyncMaxLength = MathHelperD.Clamp(AmmoDef.Const.MaxOffsetLength * Session.ClientAvDivisor, 0, Math.Max(tracerLength * 0.5d, AmmoDef.Const.MaxOffsetLength));
            var maxLength = MathHelperD.Clamp(dyncMaxLength, 0, tracerLength);
            var av = Session.Av;
            var aConst = AmmoDef.Const;
            var rnd = aConst.Random;

            if (Offsets == null)
                Offsets = av.OffSetLists.Count > 0 ? av.OffSetLists.Pop() : new List<Vector3D>();

            double currentForwardDistance = 0;
            while (currentForwardDistance <= tracerLength)
            {
                // grossly inlined fast random

                var tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                var tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                currentForwardDistance += XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (maxLength - minLength) + minLength;

                tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                var lateralXDistance = XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (maxOffset - negMaxOffset) + negMaxOffset;

                tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                var lateralYDistance = XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (maxOffset - negMaxOffset) + negMaxOffset;

                Offsets.Add(new Vector3D(lateralXDistance, lateralYDistance, currentForwardDistance * -1));
            }
        }


        internal void DrawLineOffsetEffect(Vector3D pos, Vector3D direction, double tracerLength, float beamRadius, Vector4 color)
        {
            MatrixD matrix;
            var up = MatrixD.Identity.Up;
            var startPos = pos + -(direction * tracerLength);
            MatrixD.CreateWorld(ref startPos, ref direction, ref up, out matrix);
            var offsetMaterial = AmmoDef.Const.TracerTextures[0];
            var tracerLengthSqr = tracerLength * tracerLength;
            var maxOffset = AmmoDef.Const.MaxOffset;
            var negMaxOffset = maxOffset * -1;

            var minLength = AmmoDef.Const.MinOffsetLength;
            var dyncMaxLength = MathHelperD.Clamp(AmmoDef.Const.MaxOffsetLength * Session.ClientAvDivisor, 0, Math.Max(tracerLength * 0.5d, AmmoDef.Const.MaxOffsetLength));
            var aConst = AmmoDef.Const;
            var maxLength = MathHelperD.Clamp(dyncMaxLength, 0.1, tracerLength);

            double currentForwardDistance = 0;
            var av = Session.Av;
            var rnd = aConst.Random;
            var test = 0;
            while (currentForwardDistance < tracerLength)
            {
                // grossly inlined fast random

                var tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                var tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                currentForwardDistance += XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (maxLength - minLength) + minLength;

                tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                var lateralXDistance = XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (maxOffset - negMaxOffset) + negMaxOffset;

                tempX = rnd.Y;
                rnd.X ^= rnd.X << 23;
                tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                tempZ = tempY + rnd.Y;
                rnd.X = tempX;
                rnd.Y = tempY;

                var lateralYDistance = XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * (maxOffset - negMaxOffset) + negMaxOffset;
                var overCount = ++test > 5000;
                if (overCount || currentForwardDistance < 0.1) {
                    
                    if (overCount)
                        Log.Line($"this tracer tried offsetting less than 0.1 or more than 5000 times.... this is bad: {Weapon.System.ShortName} - {AmmoDef.AmmoRound} - len:{currentForwardDistance}");
                    break;
                }

                Offsets.Add(new Vector3D(lateralXDistance, lateralYDistance, currentForwardDistance * -1));

            }

            for (int i = 0; i < Offsets.Count; i++)
            {
                Vector3D fromBeam;
                Vector3D toBeam;

                if (i == 0)
                {
                    fromBeam = matrix.Translation;
                    toBeam = Vector3D.Transform(Offsets[i], matrix);
                }
                else
                {
                    fromBeam = Vector3D.Transform(Offsets[i - 1], matrix);
                    toBeam = Vector3D.Transform(Offsets[i], matrix);
                }

                var qc = av.QuadCachePool.Count > 0 ? av.QuadCachePool.Pop() : new QuadCache();

                Vector3 dir = (toBeam - fromBeam);
                var length = dir.Length();
                var normDir = dir / length;
                qc.Shot = this;
                qc.Material = offsetMaterial;
                qc.Color = color;
                qc.StartPos = fromBeam;
                qc.Direction = normDir;
                qc.Length = length;
                qc.Width = beamRadius;
                qc.Type = QuadCache.EffectTypes.Offset;
                Session.Av.PreAddOneFrame.Add(qc);
                ++ActiveBillBoards;
                if (Vector3D.DistanceSquared(matrix.Translation, toBeam) > tracerLengthSqr) break;
            }
            Offsets.Clear();
        }

        internal void ShortStepAvUpdate(Projectile p, bool useCollisionSize, bool hit, bool earlyEnd, Vector3D position)
        {

            var stepSize = (p.Info.DistanceTraveled - p.Info.PrevDistanceTraveled);
            var avSize = useCollisionSize ? AmmoDef.Const.CollisionSize : p.Info.TracerLength;

            var endPos = hit ? Hit.LastHit : !earlyEnd ? position + -p.Direction * (p.Info.DistanceTraveled - p.Info.MaxTrajectory) : position;

            double remainingTracer;
            double stepSizeToHit;
            if (AmmoDef.Const.IsBeamWeapon)
            {
                double beamLength;
                Vector3D.Distance(ref Origin, ref endPos, out beamLength);
                remainingTracer = MathHelperD.Clamp(beamLength, 0, avSize);
                stepSizeToHit = remainingTracer;
            }
            else
            {
                double overShot;
                Vector3D.Distance(ref endPos, ref position, out overShot);
                stepSizeToHit = Math.Abs(stepSize - overShot);
                if (avSize < stepSize && !MyUtils.IsZero(avSize - stepSize, 1E-01F))
                {
                    remainingTracer = MathHelperD.Clamp(avSize - stepSizeToHit, 0, stepSizeToHit);
                }
                else if (avSize >= overShot)
                {
                    remainingTracer = MathHelperD.Clamp(avSize - overShot, 0, Math.Min(avSize, p.Info.PrevDistanceTraveled + stepSizeToHit));
                }
                else remainingTracer = 0;
            }

            if (MyUtils.IsZero(remainingTracer, 1E-01F)) remainingTracer = 0;

            StepSize = stepSize;
            VisualLength = remainingTracer;
            ShortStepSize = stepSizeToHit;

            Session.Projectiles.DeferedAvDraw.Add(new DeferedAv { AvShot = this, Info = p.Info, TracerFront = endPos, Hit = hit,  Direction = p.Direction });
        }

        internal void HitEffects(bool force = false)
        {
            if (Session.Tick - LastHit > 4 || force) {

                double distToCameraSqr;
                Vector3D.DistanceSquared(ref Hit.SurfaceHit, ref Session.CameraPos, out distToCameraSqr);

                if (Hit.EventType == HitEntity.Type.Water) 
                {
                    HitParticleActive = true;//FML... didn't know there was rand for impacts.
                }

                if (OnScreen == Screen.Tracer  || AmmoDef.Const.HitParticleNoCull || distToCameraSqr < 360000) {
                    if (HitParticleActive && AmmoDef.Const.HitParticle && !(LastHitShield && !AmmoDef.AmmoGraphics.Particles.Hit.ApplyToShield))
                            HitParticle = ParticleState.Custom;
                }


                var hitSound = AmmoDef.Const.HitSound && HitSoundActive && distToCameraSqr < AmmoDef.Const.HitSoundDistSqr && (!LastHitShield || AmmoDef.AmmoAudio.HitPlayShield);
                if (hitSound) {

                    MySoundPair pair = null;
                    var shield = Hit.Entity as IMyUpgradeModule;
                    var voxel = Hit.Entity as MyVoxelBase;
                    var player = Hit.Entity as IMyCharacter;
                    var floating = Hit.Entity as MyFloatingObject;

                    if (voxel != null && AmmoDef.Const.VoxelSound) {
                        pair = AmmoDef.Const.VoxelSoundPair;
                    }
                    else if (player != null && AmmoDef.Const.PlayerSound) {
                        pair = AmmoDef.Const.PlayerSoundPair;
                    }
                    else if (floating != null && AmmoDef.Const.FloatingSound) {
                        pair = AmmoDef.Const.FloatingSoundPair;
                    }
                    else if (shield != null && AmmoDef.Const.ShieldSound) {
                        pair = AmmoDef.Const.ShieldSoundPair;
                    }
                    else if (AmmoDef.Const.HitSound) {
                        pair = AmmoDef.Const.HitSoundPair;
                    }

                    if (pair != null) {

                        var hitEmitter = Session.Av.PersistentEmitters.Count > 0 ? Session.Av.PersistentEmitters.Pop() : new MyEntity3DSoundEmitter(null);

                        var pos = Session.Tick - Hit.HitTick <= 1 && !MyUtils.IsZero(Hit.SurfaceHit) ? Hit.SurfaceHit : TracerFront;
                        hitEmitter.Entity = Hit.Entity;
                        hitEmitter.SetPosition(pos);
                        hitEmitter.PlaySound(pair);

                        Session.SoundsToClean.Add(new Session.CleanSound { DelayedReturn = true, Emitter = hitEmitter, Pair = pair, EmitterPool = Session.I.Av.PersistentEmitters, SpawnTick = Session.I.Tick });

                        HitSoundInitted = true;
                    }
                }
                LastHitShield = false;
            }
        }


        internal void SetupSounds(double distanceFromCameraSqr)
        {

            if (!AmmoDef.Const.IsBeamWeapon && AmmoDef.Const.AmmoTravelSound) {
                HasTravelSound = true;
                TravelEmitter = Session.Av.TravelEmitters.Count > 0 ? Session.Av.TravelEmitters.Pop() : new MyEntity3DSoundEmitter(null);

                TravelEmitter.CanPlayLoopSounds = true;
            }
            else HasTravelSound = false;

            if (AmmoDef.Const.HitSound) {
                var hitSoundChance = AmmoDef.AmmoAudio.HitPlayChance;
                HitSoundActive = (hitSoundChance >= 1 || hitSoundChance >= MyUtils.GetRandomDouble(0.0f, 1f));
            }

            if (AmmoDef.Const.ShotSound)
            {
                try //SharpDX bughunting
                {
                    if (distanceFromCameraSqr <= AmmoDef.Const.ShotSoundDistSqr && (IsFragment || Weapon.System.FiringSound == WeaponSystem.FiringSoundState.None))
                    {
                        FireEmitter = Session.Av.FireEmitters.Count > 0 ? Session.Av.FireEmitters.Pop() : new MyEntity3DSoundEmitter(null);
                        FireEmitter.CanPlayLoopSounds = true;
                        FireEmitter.Entity = null;
                        FireEmitter.SetPosition(Origin);
                        FireEmitter.PlaySound(AmmoDef.Const.ShotSoundPair, true);
                    }
                    else if (Weapon.System.FiringSound == WeaponSystem.FiringSoundState.PerShot && distanceFromCameraSqr <= Weapon.System.FiringSoundDistSqr)
                    {
                        FireEmitter = Session.Av.FireEmitters.Count > 0 ? Session.Av.FireEmitters.Pop() : new MyEntity3DSoundEmitter(null);
                        FireEmitter.CanPlayLoopSounds = true;
                        FireEmitter.Entity = Weapon.Comp.CoreEntity;
                        FireEmitter.SetPosition(Origin);
                        FireEmitter.PlaySound(AmmoDef.Const.ShotSoundPair, true);
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.Error($"Sound error with ammo: {AmmoDef.AmmoRound} from {Weapon.Comp.TerminalBlock.DisplayName} soundID {AmmoDef.Const.ShotSoundPair.SoundId} cuename {AmmoDef.Const.ShotSoundPair.GetCueName()}" +
                        $"FireEmitter.Entity null? {FireEmitter?.Entity == null} \n Origin: {Origin}");
                    throw e;
                }
            }
        }

        internal void TravelSoundStart()
        {
            TravelEmitter.SetPosition(TracerFront);
            TravelEmitter.Entity = PrimeEntity;
            ApproachConstants def = StageIdx <= -1 ? null : AmmoDef.Const.Approaches[StageIdx];
            var pair = def == null  || !def.AlternateTravelSound ? AmmoDef.Const.TravelSoundPair : def.SoundPair;

            TravelEmitter.PlaySound(pair, true);
            TravelSound = true;
        }

        internal void PlayAmmoParticle()
        {
            ApproachConstants def = StageIdx <= -1 ? null : AmmoDef.Const.Approaches[StageIdx];

            var particleDef = def == null || !def.AlternateTravelParticle ? AmmoDef.AmmoGraphics.Particles.Ammo : def.Definition.AlternateParticle;

            MatrixD matrix;
            if (Model != ModelState.None && PrimeEntity != null)
                matrix = PrimeMatrix;
            else {
                matrix = MatrixD.CreateWorld(TracerFront, Direction, OriginUp);
                var offVec = TracerFront + Vector3D.Rotate(particleDef.Offset, matrix);
                matrix.Translation = offVec;
            }

            var renderId = AmmoDef.Const.PrimeModel && PrimeEntity != null ? PrimeEntity.Render.GetRenderObjectID() : uint.MaxValue;

            if (MyParticlesManager.TryCreateParticleEffect(particleDef.Name, ref matrix, ref TracerFront, renderId, out AmmoEffect))
            {
                AmmoEffect.UserScale = particleDef.Extras.Scale;
                AmmoParticleStopped = false;
                AmmoParticleInited = true;
                var loop = AmmoEffect.Loop || AmmoEffect.DurationMax <= 0;
                if (!loop)
                    AmmoEffect = null;
            }

        }

        internal void PlayFieldParticle()
        {
            var pos = TriggerEntity.PositionComp.WorldAABB.Center;
            if (MyParticlesManager.TryCreateParticleEffect(AmmoDef.Ewar.Field.Particle.Name, ref TriggerMatrix, ref pos, uint.MaxValue, out FieldEffect))
            {
                FieldEffect.UserScale = AmmoDef.Ewar.Field.Particle.Extras.Scale;

                FieldParticleStopped = false;
                FieldParticleInited = true;
            }
        }

        internal void DisposeAmmoEffect(bool instant, bool pause)
        {
            if (AmmoEffect != null)
            {
                AmmoEffect.Stop(instant);
                AmmoEffect = null;
            }

            if (pause)
                AmmoParticleStopped = true;
        }

        internal void DisposeFieldEffect(bool instant, bool pause)
        {
            if (FieldEffect != null)
            {
                FieldEffect.Stop(instant);
                FieldEffect = null;
            }

            if (pause)
                FieldParticleStopped = true;
        }

        internal void ResetHit()
        {
            ShrinkInited = false;
            TotalLength = MathHelperD.Clamp(MaxTracerLength + MaxGlowLength, 0.1f, MaxTrajectory);
        }

        private void StageChange(int newStageIdx, bool createdPrimeEntity)
        {
            var aConst = AmmoDef.Const;
            var lastApproach = AmmoDef.Const.Approaches.Length;
            var oldStage = StageIdx;
            StageIdx = newStageIdx < lastApproach ? newStageIdx : -1;

            ApproachConstants oldDef = oldStage <= -1 ? null : AmmoDef.Const.Approaches[oldStage];
            ApproachConstants newDef = StageIdx <= -1 ? null : AmmoDef.Const.Approaches[StageIdx];


            if (Model == ModelState.Exists && PrimeEntity != null)
            {
                if (!createdPrimeEntity)
                {
                    if (PrimeEntity.InScene)
                    {
                        PrimeEntity.InScene = false;
                        PrimeEntity.Render.RemoveRenderObjects();
                    }

                    if (oldDef == null || !oldDef.AlternateModel)
                         aConst.PrimeEntityPool.Return(PrimeEntity); 
                    else
                        oldDef.ModelPool.Return(PrimeEntity);

                    PrimeEntity = newDef == null || !newDef.AlternateModel ? aConst.PrimeEntityPool.Get() : newDef.ModelPool.Get();

                    if (PrimeEntity.PositionComp.WorldVolume.Radius * 2 > ModelSphereCurrent.Radius)
                        ModelSphereCurrent.Radius = PrimeEntity.PositionComp.WorldVolume.Radius * 2;
                }

            }

            if (aConst.AmmoParticle && Active && (newDef != null && newDef.AlternateTravelParticle))
            {
                DisposeAmmoEffect(false, false);
                AmmoParticleInited = false;
            }
            if (TravelSound && TravelEmitter != null)
            {
                TravelEmitter.StopSound(true);
                TravelSound = false;
            }
        }


        internal void RunBeam()
        {
            MyParticleEffect effect;
            MatrixD matrix;
            var vel = HitVelocity;
            if (!Session.Av.BeamEffects.TryGetValue(UniqueMuzzleId, out effect)) {

                MatrixD.CreateTranslation(ref TracerFront, out matrix);
                if (!MyParticlesManager.TryCreateParticleEffect(AmmoDef.AmmoGraphics.Particles.Hit.Name, ref matrix, ref TracerFront, uint.MaxValue, out effect)) {
                    return;
                }

                if (effect.Loop || effect.DurationMax <= 0)
                    Session.Av.BeamEffects[UniqueMuzzleId] = effect;

                effect.UserScale = AmmoDef.AmmoGraphics.Particles.Hit.Extras.Scale;

                Vector3D.ClampToSphere(ref vel, (float)MaxSpeed);
            }
            else if (effect != null && !effect.IsEmittingStopped) {
                MatrixD.CreateTranslation(ref Hit.SurfaceHit, out matrix);
                Vector3D.ClampToSphere(ref vel, (float)MaxSpeed);
                effect.WorldMatrix = matrix;
            }
        }

        internal static void UpdateVirtualBeams(Projectile p, ProInfo info, HitEntity hitEntity, Vector3D visualHitPos, Vector3D lastHitVel, bool intersecting)
        {
            Vector3D? hitPos = null;
            if (!Vector3D.IsZero(info.ProHit.LastHit)) hitPos = info.ProHit.LastHit;
            var aConst = info.AmmoDef.Const;

            for (int v = 0; v < p.VrPros.Count; v++)
            {
                var vp = p.VrPros[v];
                var vs = vp.AvShot;

                vp.TracerLength = info.TracerLength;
                var visDir = aConst.ConvergeBeams ? p.Direction : vp.OriginFwd;
                vs.Init(vp, (aConst.DeltaVelocityPerTick * Session.I.DeltaTimeRatio), p.MaxSpeed, ref visDir);
                if (info.BaseDamagePool <= 0 || p.State == ProjectileState.Depleted)
                    vs.ProEnded = true;

                vs.LastHitShield = intersecting && hitEntity.EventType == HitEntity.Type.Shield;
                vs.Hit = new Hit { Entity = intersecting ? hitEntity.Entity : null, EventType = intersecting ? hitEntity.EventType : HitEntity.Type.Shield, HitTick = Session.I.Tick, HitVelocity = lastHitVel, LastHit = visualHitPos, SurfaceHit = visualHitPos };
                vs.StepSize = info.DistanceTraveled - info.PrevDistanceTraveled;
                if (aConst.ConvergeBeams)
                {
                    LineD beam;
                    if (intersecting)
                    {
                        beam = new LineD(vs.Origin, hitPos ?? p.Position);
                        vs.ShortStepSize = beam.Length;
                    }
                    else
                        beam = new LineD(vs.Origin, p.Position);

                    vs.VisualLength = beam.Length;

                    Session.I.Projectiles.DeferedAvDraw.Add(new DeferedAv { AvShot = vs, Info = info, TracerFront = beam.To, Hit = intersecting, Direction = beam.Direction });
                }
                else
                {
                    Vector3D beamEnd;
                    var hit = intersecting && hitPos.HasValue;
                    if (!hit)
                        beamEnd = vs.Origin + (visDir * info.MaxTrajectory);
                    else
                        beamEnd = vs.Origin + (visDir * info.Weapon.WeaponCache.HitDistance);

                    var line = new LineD(vs.Origin, beamEnd, !hit ? info.MaxTrajectory : info.Weapon.WeaponCache.HitDistance);

                    vs.VisualLength = line.Length;

                    if (intersecting && hitPos.HasValue)
                    {
                        vs.ShortStepSize = line.Length;
                        Session.I.Projectiles.DeferedAvDraw.Add(new DeferedAv { AvShot = vs, Info = info, TracerFront = line.To, Hit = true, Direction = line.Direction });
                    }
                    else
                        Session.I.Projectiles.DeferedAvDraw.Add(new DeferedAv { AvShot = vs, Info = info, TracerFront = line.To, Hit = false, Direction = line.Direction });
                }
            }
        }

        internal void AvClose()
        {
            if (MarkForClose)
                return;

            if (Vector3D.IsZero(TracerFront)) TracerFront = EndState.EndPos;

            if (AmmoDef.Const.AmmoParticle)
            {
                ApproachConstants def = StageIdx <= -1 ? null : AmmoDef.Const.Approaches[StageIdx];
                var particleDef = def == null || !def.AlternateTravelParticle ? AmmoDef.AmmoGraphics.Particles.Ammo : def.Definition.AlternateParticle;
                DisposeAmmoEffect(particleDef.Extras.Restart, false);
            }

            if (EndState.DetonateEffect)
            {
                HitParticle = ParticleState.Dirty;
                if (OnScreen != Screen.None)
                {
                    var a = AmmoDef;
                    var c = a.Const;
                    var hit = Session.Tick - Hit.HitTick <= 1 && !MyUtils.IsZero(Hit.SurfaceHit) && Hit.Entity != null;
                    var pos = hit ? Hit.SurfaceHit : TracerFront;
                    if (a.Const.DetonationSound && Vector3D.DistanceSquared(Session.CameraPos, pos) < a.Const.DetonationSoundDistSqr)
                    {
                        var detEmitter = Session.Av.PersistentEmitters.Count > 0 ? Session.Av.PersistentEmitters.Pop() : new MyEntity3DSoundEmitter(null);
                        detEmitter.Entity = Hit.Entity;
                        detEmitter.SetPosition(pos);
                        detEmitter.PlaySound(a.Const.DetSoundPair);
                        Session.SoundsToClean.Add(new Session.CleanSound { DelayedReturn = true, Emitter = detEmitter, EmitterPool = Session.Av.PersistentEmitters, SpawnTick = Session.I.Tick });
                    }

                    if (a.Const.CustomDetParticle || Session.Av.ExplosionReady)
                    {
                        
                        var particle = AmmoDef.AmmoGraphics.Particles.Hit;
                        var keenStrikesAgain = particle.Offset == Vector3D.MaxValue;
                        MatrixD matrix = MatrixD.CreateTranslation(pos);
                        if(keenStrikesAgain)
                        {
                            matrix = MatrixD.CreateWorld(pos, VisualDir, OriginUp);
                        }
                        else if (particle.Offset == Vector3D.MinValue)
                        {
                            float interference;
                            Vector3D localGrav = Session.I.Physics.CalculateNaturalGravityAt(pos, out interference);
                            localGrav.Normalize();
                            if(localGrav != Vector3D.Zero)
                                matrix = MatrixD.CreateWorld(pos, Vector3D.CalculatePerpendicularVector(localGrav), -localGrav);
                        }
                        MyParticleEffect detEffect;
                        if (MyParticlesManager.TryCreateParticleEffect(a.Const.DetParticleStr, ref matrix, ref pos, uint.MaxValue, out detEffect))
                        {
                            detEffect.UserScale = a.AreaOfDamage.EndOfLife.ParticleScale;

                            if (hit)
                                detEffect.Velocity = Hit.HitVelocity;


                            if (detEffect.Loop)
                                detEffect.Stop();
                        }
                    }

                }
            }

            if (FireEmitter != null)
            {
                var loop = FireEmitter.Loop;
                if (loop)
                {
                    FireEmitter.StopSound(true);
                    FireEmitter.PlaySound(AmmoDef.Const.ShotSoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
                }

                Session.SoundsToClean.Add(new Session.CleanSound { DelayedReturn = true, Emitter = FireEmitter, EmitterPool = Session.Av.FireEmitters, SpawnTick = Session.Tick });

                FireEmitter = null;
            }

            if (TravelEmitter != null)
            {
                if (TravelSound)
                {
                    var loop = TravelEmitter.Loop;
                    if (loop)
                    {
                        TravelEmitter.StopSound(true);
                        ApproachConstants def = StageIdx <= -1 ? null : AmmoDef.Const.Approaches[StageIdx];
                        TravelEmitter.PlaySound(def == null  || !def.AlternateTravelSound ? AmmoDef.Const.TravelSoundPair : def.SoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
                    }
                }

                Session.SoundsToClean.Add(new Session.CleanSound { JustClean = !TravelSound, DelayedReturn = TravelSound, Emitter = TravelEmitter, EmitterPool = Session.Av.TravelEmitters, SpawnTick = Session.Tick });

                TravelSound = false;
                TravelEmitter = null;
            }

            if (PrimeEntity != null && PrimeEntity.InScene)
            {
                PrimeEntity.InScene = false;
                PrimeEntity.Render.RemoveRenderObjects();
            }

            if (Triggered && TriggerEntity != null && TriggerEntity.InScene)
            {
                TriggerEntity.InScene = false;
                TriggerEntity.Render.RemoveRenderObjects();
            }

            MarkForClose = true;
        }

        public void AmmoInfoClean()
        {
            SegmentGaped = false;
            TextureReverse = false;
            SegmentLenTranserved = 1;
            TextureIdx = -1;
            SegMeasureStep = 0;
            TextureLastUpdate = 0;
        }

        internal void UpdateCache(AvInfoCache avInfoCache)
        {
            SegmentGaped = avInfoCache.SegmentGaped;
            TextureReverse = avInfoCache.TextureReverse;
            SegmentLenTranserved = avInfoCache.SegmentLenTranserved;
            TextureIdx = avInfoCache.TextureIdx;
            SegMeasureStep = avInfoCache.SegMeasureStep;
            TextureLastUpdate = avInfoCache.TextureLastUpdate;
        }


        internal void Close()
        {
            // Reset only vars that are not always set
            Hit = new Hit();
            EndState = new AvClose();

            if (FireEmitter != null)
            {
                var loop = FireEmitter.Loop;

                if (loop)
                {
                    FireEmitter.StopSound(true);
                    FireEmitter.PlaySound(AmmoDef.Const.ShotSoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
                }

                Session.SoundsToClean.Add(new Session.CleanSound { DelayedReturn = true, Emitter = FireEmitter, EmitterPool = Session.Av.FireEmitters, SpawnTick = Session.Tick });
            }

            if (TravelEmitter != null) {
                if (TravelSound)
                {
                    var loop = TravelEmitter.Loop;
                    if (loop)
                    {
                        TravelEmitter.StopSound(true);
                        ApproachConstants def = StageIdx <= -1 ? null : AmmoDef.Const.Approaches[StageIdx];

                        TravelEmitter.PlaySound(def == null || !def.AlternateTravelSound ? AmmoDef.Const.TravelSoundPair : def.SoundPair, stopPrevious: false, skipIntro: true, force2D: false, alwaysHearOnRealistic: false, skipToEnd: true);
                    }
                }

                Session.SoundsToClean.Add(new Session.CleanSound { JustClean = !TravelSound, DelayedReturn = TravelSound, Emitter = TravelEmitter, EmitterPool = Session.Av.TravelEmitters, SpawnTick = Session.Tick });
                
                TravelSound = false;
            }

            if (AmmoEffect != null)
                DisposeAmmoEffect(true, false);

            if (PrimeEntity != null && PrimeEntity.InScene)
            {
                PrimeEntity.InScene = false;
                PrimeEntity.Render.RemoveRenderObjects();
            }

            if (Triggered && TriggerEntity != null && TriggerEntity.InScene)
            {
                TriggerEntity.InScene = false;
                TriggerEntity.Render.RemoveRenderObjects();
            }


            if (PrimeEntity != null)
            {
                AmmoDef.Const.PrimeEntityPool.Return(PrimeEntity);
            }

            if (TriggerEntity != null)
            {
                Session.TriggerEntityPool.Return(TriggerEntity);
            }

            for (int j = TrailSteps.Count - 1; j >= 0; j--)
                Session.Av.Trails.Push(TrailSteps[j]);

            TrailSteps.Clear();

            if (Offsets != null)
            {
                Offsets.Clear();
                Session.Av.OffSetLists.Push(Offsets);
            }

            HitVelocity = Vector3D.Zero;
            TracerBack = Vector3D.Zero;
            TracerFront = Vector3D.Zero;
            Color = Vector4.Zero;
            SegmentColor = Vector4.Zero;
            FgFactionColor = Vector4.Zero;
            BgFactionColor = Vector4.Zero;

            OnScreen = Screen.None;
            Tracer = TracerState.Off;
            Trail = TrailState.Off;
            LifeTime = 0;
            TracerSteps = 0;
            TracerStep = 0;
            DistanceToLine = 0;
            TracerWidth = 0;
            TrailWidth = 0;
            SegmentWidth = 0;
            TrailScaler = 0;
            MaxTrajectory = 0;
            ShotFade = 0;
            UniqueMuzzleId = 0;
            DecayTime = 0;
            LastHit = uint.MaxValue / 2;
            LastHitShield = false;
            TravelSound = false;
            HitSoundActive = false;
            HitSoundInitted = false;
            IsFragment = false;
            HasTravelSound = false;
            HitParticle = ParticleState.None;
            Triggered = false;
            Cloaked = false;
            Active = false;
            TrailActivated = false;
            ShrinkInited = false;
            Hitting = false;
            Back = false;
            LastStep = false;
            AmmoParticleStopped = false;
            AmmoParticleInited = false;
            FieldParticleStopped = false;
            FieldParticleInited = false;
            ModelOnly = false;
            ForceHitParticle = false;
            HitParticleActive = false;
            MarkForClose = false;
            ProEnded = false;
            HasModel = false;
            TracerShrinks.Clear();

            //
            SegmentGaped = false;
            TextureReverse = false;
            SegmentLenTranserved = 1;
            TextureIdx = -1;
            StageIdx = -1;
            SegMeasureStep = 0;
            TextureLastUpdate = 0;
            ActiveBillBoards = 0;
            //

            Weapon = null;
            PrimeEntity = null;
            TriggerEntity = null;
            AmmoDef = null;
            FireEmitter = null;
            TravelEmitter = null;
            Session.Av.AvShotCoolDown[Session.Tick % Session.Av.AvShotCoolDown.Length].Add(this);
        }
    }

    public class QuadCache
    {
        public enum EffectTypes
        {
            Tracer,
            Trail,
            Shrink,
            Offset,
            Segment,
        }

        public readonly MyBillboard BillBoard = new MyBillboard();
        public AvShot Shot;
        public EffectTypes Type;
        public MyStringId Material;
        public Vector4 Color;
        public Vector3D StartPos;
        public Vector3D Direction;
        public Vector3D Left;
        public Vector2 UvOff;
        public Vector2 UvSize;
        public float Length;
        public float Width;
        public float TextureSize;
        public BlendTypeEnum Blend = BlendTypeEnum.Standard;
    }

    public class QuadPersistentCache
    {
        public enum EffectTypes
        {
            Tracer,
            Trail,
            Shrink,
            Offset,
            Segment,
        }

        public readonly MyBillboard BillBoard = new MyBillboard();
        public object Owner;
        public AvShot Shot;
        public EffectTypes Type;
        public MyStringId Material;
        public Vector4 Color;
        public Vector3D StartPos;
        public Vector3D Up;
        public Vector3D Left;
        public Vector2 UvOff;
        public Vector2 UvSize;
        public float Width;
        public float Height;
        public float TextureSize;
        public bool Updated;
        public bool Added;
        public int Age;
        public int MarkedForCloseIn = int.MaxValue;
        public int LifeTime = int.MaxValue;
        public BlendTypeEnum Blend = BlendTypeEnum.Standard;
    }

    internal class AfterTrail
    {
        internal AfterTrail Parent;
        internal Vector3D TailPos;
        internal LineD Line;
        internal int Step;
    }

    internal class Shrink
    {
        internal Vector3D NewFront;
        internal Vector4 Color;
        internal float Length;
        internal float Thickness;
        internal bool Last;
    }

    internal struct AvInfoCache
    {
        internal bool SegmentGaped;
        internal bool TextureReverse;
        internal double SegmentLenTranserved;
        internal double SegMeasureStep;
        internal int TextureIdx;
        internal uint TextureLastUpdate;
    }

    internal struct AvClose
    {
        internal bool Dirty;
        internal bool DetonateEffect;
        internal Vector3D EndPos;
    }

    internal struct DeferedAv
    {
        internal AvShot AvShot;
        internal ProInfo Info;
        internal bool Hit;
        internal Vector3D TracerFront;
        internal Vector3D Direction;
    }
}

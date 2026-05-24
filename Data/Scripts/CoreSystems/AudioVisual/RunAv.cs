using System.Collections.Generic;
using CoreSystems.Platform;
using Jakaria.API;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;
using static VRageRender.MyBillboard;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static CoreSystems.Support.WeaponDefinition;
using VRage.Collections;
using VRage.ModAPI;
using System;
namespace CoreSystems.Support
{
    class RunAv
    {
        internal readonly Dictionary<ulong, MyParticleEffect> BeamEffects = new Dictionary<ulong, MyParticleEffect>();
        internal readonly Stack<AvShot> AvShotPool = new Stack<AvShot>(1024);
        internal readonly List<AvShot>[] AvShotCoolDown = new List<AvShot>[5];
        internal readonly List<QuadCache>[] QuadCacheCoolDown = new List<QuadCache>[5];
        internal readonly List<QuadPersistentCache>[] QuadPersistentCacheCoolDown = new List<QuadPersistentCache>[5];

        internal readonly Stack<AvEffect> AvEffectPool = new Stack<AvEffect>(128);
        internal readonly Stack<AfterTrail> Trails = new Stack<AfterTrail>(128);
        internal readonly Stack<Shrink> Shrinks = new Stack<Shrink>(128);
        internal readonly Stack<QuadCache> QuadCachePool = new Stack<QuadCache>(128);
        internal readonly Stack<List<Vector3D>> OffSetLists = new Stack<List<Vector3D>>(128);

        internal readonly Stack<AvAdvBillboards> AvAdvBillboardsEffectPool = new Stack<AvAdvBillboards>(512);
        internal readonly List<AvAdvBillboards> AdvBillboards = new List<AvAdvBillboards>(512);
        internal readonly Stack<AdvBLineCache> AdvBLinesPool = new Stack<AdvBLineCache>(512);
        internal readonly List<AdvBLineCache>[] AdvBLineCacheCooldown = new List<AdvBLineCache>[5];
        internal readonly Stack<MyQueue<AdvBLineCache>> AdvBLongTrailCacheLists = new Stack<MyQueue<AdvBLineCache>>(32);
        internal readonly Stack<MyQueue<AdvBLineCache>> AdvBTrailCacheLists = new Stack<MyQueue<AdvBLineCache>>(128);
        internal readonly List<AdvBLineCache> AdvBLines = new List<AdvBLineCache>(512);
        internal readonly Dictionary<float, MatrixD> ComputedRotationMatricies = new Dictionary<float, MatrixD>();
        internal readonly Stack<BillboardInfo> BillboardCache = new Stack<BillboardInfo>(512);

        internal readonly Stack<MyEntity3DSoundEmitter> FireEmitters = new Stack<MyEntity3DSoundEmitter>();
        internal readonly Stack<MyEntity3DSoundEmitter> TravelEmitters = new Stack<MyEntity3DSoundEmitter>();
        internal readonly Stack<MyEntity3DSoundEmitter> PersistentEmitters = new Stack<MyEntity3DSoundEmitter>();


        internal readonly List<AvEffect> Effects1 = new List<AvEffect>(128);
        internal readonly List<AvEffect> Effects2 = new List<AvEffect>(128);
        internal readonly List<ParticleEvent> ParticlesToProcess = new List<ParticleEvent>(128);
        internal readonly List<AvShot> AvShots = new List<AvShot>(1024);
        internal readonly List<HitParticleEvent> HitParticles = new List<HitParticleEvent>(128);

        internal readonly List<QuadPersistentCache> PreAddPersistent = new List<QuadPersistentCache>();
        internal readonly List<QuadPersistentCache> ActiveBillBoards = new List<QuadPersistentCache>();
        internal readonly List<QuadCache> PreAddOneFrame = new List<QuadCache>();


        internal readonly List<MyBillboard> BillBoardsToAdd = new List<MyBillboard>();
        internal readonly List<MyBillboard> BillBoardsToRemove = new List<MyBillboard>();

        internal int ExplosionCounter;
        internal int MaxExplosions = 100;
        internal int NearBillBoardLimit;


        internal bool ExplosionReady
        {
            get
            {
                if (ExplosionCounter + 1 <= MaxExplosions)
                {
                    ExplosionCounter++;
                    return true;
                }
                return false;
            }
        }

        internal RunAv()
        {
            for (int i = 0; i < AvShotCoolDown.Length; i++)
                AvShotCoolDown[i] = new List<AvShot>();

            for (int i = 0; i < QuadCacheCoolDown.Length; i++)
                QuadCacheCoolDown[i] = new List<QuadCache>();

            for (int i = 0; i < AdvBLineCacheCooldown.Length; i++)
                AdvBLineCacheCooldown[i] = new List<AdvBLineCache>();
        }

        private int _onScreens;
        private int _shrinks;
        private int _previousTrailCount;
        private int _models;

        internal void End()
        {
            if (Effects1.Count > 0) RunAvEffects1();
            if (Effects2.Count > 0) RunAvEffects2();
            if (ParticlesToProcess.Count > 0) Session.I.ProcessParticles();
            if (HitParticles.Count > 0) RunAvHitEffects();

            for (int i = AvShots.Count - 1; i >= 0; i--)
            {
                var av = AvShots[i];
                var refreshed = av.LastTick == Session.I.Tick && !av.MarkForClose;
                if (refreshed)
                {
                    if (av.PrimeEntity != null)
                    {
                        _models++;

                        if (av.OnScreen != AvShot.Screen.None)
                        {
                            if (!av.PrimeEntity.InScene && !av.Cloaked)
                            {
                                av.PrimeEntity.InScene = true;
                                av.PrimeEntity.Render.UpdateRenderObject(true, false);
                            }
                            av.PrimeEntity.PositionComp.SetWorldMatrix(ref av.PrimeMatrix, null, false, false, false);
                        }

                        if ((av.Cloaked || av.OnScreen == AvShot.Screen.None) && av.PrimeEntity.InScene)
                        {
                            av.PrimeEntity.InScene = false;
                            av.PrimeEntity.Render.RemoveRenderObjects();
                        }
                    }

                    if (av.Triggered && av.TriggerEntity != null)
                    {
                        if (!av.AmmoDef.Ewar.Field.HideModel && (!av.TriggerEntity.InScene))
                        {
                            av.TriggerEntity.InScene = true;
                            av.TriggerEntity.Render.UpdateRenderObject(true, false);
                        }
                        av.TriggerEntity.PositionComp.SetWorldMatrix(ref av.TriggerMatrix, null, false, false, false);

                        if (av.OnScreen != AvShot.Screen.None && av.AmmoDef.Const.FieldParticle && av.FieldEffect != null)
                            av.FieldEffect.WorldMatrix = av.PrimeMatrix;
                    }

                    if (av.HasTravelSound)
                    {
                        if (!av.TravelSound)
                        {
                            double distSqr;
                            Vector3D.DistanceSquared(ref av.TracerFront, ref Session.I.CameraPos, out distSqr);
                            if (distSqr <= av.AmmoDef.Const.AmmoTravelSoundDistSqr && av.AccelClearance)
                                av.TravelSoundStart();
                        }
                        else av.TravelEmitter.SetPosition(av.TracerFront);
                    }

                    if (!(av.HitParticle == AvShot.ParticleState.Dirty || av.HitParticle == AvShot.ParticleState.None))
                    {
                        if (av.OnScreen != AvShot.Screen.None)
                        {
                            var pos = Session.I.Tick - av.Hit.HitTick <= 1 && !MyUtils.IsZero(av.Hit.SurfaceHit) ? av.Hit.SurfaceHit : av.TracerFront;
                            ParticleDef particle;
                            bool gravPerp = false;
                            MatrixD matrix = MatrixD.CreateTranslation(pos);
                            switch (av.HitParticle)
                            {
                                case AvShot.ParticleState.Shield:
                                    particle = av.AmmoDef.AmmoGraphics.Particles.ShieldHit;
                                    if (av.ShieldHitAngle == Vector3D.Zero)
                                    {
                                        var grid = av.Hit.Entity.GetTopMostParent() as IMyCubeGrid;
                                        var lineToShield = pos - grid.PositionComp.WorldAABB.Center;
                                        lineToShield.Normalize();
                                        matrix = MatrixD.CreateWorld(pos, lineToShield, Vector3D.CalculatePerpendicularVector(lineToShield));
                                    }
                                    else
                                        matrix = MatrixD.CreateWorld(pos, av.ShieldHitAngle, Vector3D.CalculatePerpendicularVector(av.ShieldHitAngle));
                                    break;
                                case AvShot.ParticleState.Water:
                                    particle = av.AmmoDef.AmmoGraphics.Particles.WaterHit;
                                    gravPerp = true;
                                    break;
                                case AvShot.ParticleState.Voxel:
                                    particle = av.AmmoDef.AmmoGraphics.Particles.VoxelHit;
                                    gravPerp = true;
                                    break;
                                case AvShot.ParticleState.Custom:
                                default:
                                    particle = av.AmmoDef.AmmoGraphics.Particles.Hit;
                                    if (particle.Offset == Vector3D.MaxValue)
                                        matrix = MatrixD.CreateWorld(pos, av.VisualDir, Vector3D.CalculatePerpendicularVector(av.VisualDir));
                                    else if (particle.Offset == Vector3D.MinValue)
                                        gravPerp = true;
                                    break;
                            }

                            if (gravPerp)
                            {
                                if (av.Weapon?.Comp?.Ai?.MyPlanet != null)
                                {
                                    var planetDir = pos - av.Weapon.Comp.Ai.MyPlanet.PositionComp.WorldAABB.Center;
                                    planetDir.Normalize();
                                    matrix = MatrixD.CreateWorld(pos, Vector3D.CalculatePerpendicularVector(planetDir), -planetDir);
                                }
                                else
                                {
                                    float interference;
                                    Vector3D localGrav = Session.I.Physics.CalculateNaturalGravityAt(pos, out interference);
                                    localGrav.Normalize();
                                    if (localGrav != Vector3D.Zero)
                                        matrix = MatrixD.CreateWorld(pos, Vector3D.CalculatePerpendicularVector(localGrav), -localGrav);
                                }
                            }

                            MyParticleEffect hitEffect;
                            if (MyParticlesManager.TryCreateParticleEffect(particle.Name, ref matrix, ref pos, uint.MaxValue, out hitEffect))
                            {
                                hitEffect.UserScale = particle.Extras.Scale;
                                var tickVelo = av.Hit.HitVelocity / 60;
                                HitParticles.Add(new HitParticleEvent(hitEffect, tickVelo));
                                if (hitEffect.Loop)
                                    hitEffect.Stop();
                            }
                        }
                        av.HitParticle = AvShot.ParticleState.Dirty;
                    }
                    if (av.Hit.Entity != null && av.AmmoDef.AmmoGraphics.Decals.MaxAge > 0 && !Vector3D.IsZero(av.Hit.SurfaceHit) && av.AmmoDef.Const.TextureHitMap.Count > 0 && !av.Hit.Entity.MarkedForClose && av.Hit.Entity.InScene)
                    {
                        var shield = av.Hit.Entity as IMyUpgradeModule;
                        var floating = av.Hit.Entity as MyFloatingObject;
                        if (shield == null && floating == null)
                        {
                            MySurfaceImpactEnum surfaceImpact;
                            MyStringHash materialType;
                            var beam = new LineD(av.TracerFront + -(av.Direction * av.StepSize), av.TracerFront + (av.Direction * 0.1f));
                            MyAPIGateway.Projectiles.GetSurfaceAndMaterial(av.Hit.Entity, ref beam, ref av.Hit.SurfaceHit, 0, out surfaceImpact, out materialType);

                            MyStringHash projectileMaterial;
                            if (av.AmmoDef.Const.TextureHitMap.TryGetValue(materialType, out projectileMaterial))
                            {
                                MyStringHash voxelMaterial = MyStringHash.NullOrEmpty;
                                var voxelBase = av.Hit.Entity as MyVoxelBase;
                                if (voxelBase != null)
                                {
                                    Vector3D position = av.Hit.SurfaceHit;
                                    MyVoxelMaterialDefinition materialAt = voxelBase.GetMaterialAt(ref position);
                                    if (materialAt != null)
                                        voxelMaterial = materialAt.Id.SubtypeId;
                                }

                                var hitInfo = new MyHitInfo
                                {
                                    Position = av.Hit.SurfaceHit + (av.Direction * 0.01),
                                    Normal = (Vector3)av.Direction,
                                };
                                MyDecals.HandleAddDecal(av.Hit.Entity, hitInfo, Vector3.Zero, materialType, projectileMaterial, null, -1, voxelMaterial, false, MyDecalFlags.IgnoreOffScreenDeletion, MyAPIGateway.Session.GameplayFrameCounter + av.AmmoDef.AmmoGraphics.Decals.MaxAge);
                            }
                        }
                    }

                    if (av.Model != AvShot.ModelState.None)
                    {
                        if (av.AmmoEffect != null && av.AmmoDef.Const.AmmoParticle && av.AmmoDef.Const.PrimeModel)
                        {
                            ApproachConstants def = av.StageIdx <= -1 ? null : av.AmmoDef.Const.Approaches[av.StageIdx];
                            var particleDef =  def == null || !def.AlternateTravelParticle ? av.AmmoDef.AmmoGraphics.Particles.Ammo : def.Definition.AlternateParticle;

                            var offVec = av.TracerFront + Vector3D.Rotate(particleDef.Offset, av.PrimeMatrix);
                            av.AmmoEffect.WorldMatrix = av.PrimeMatrix;
                            av.AmmoEffect.SetTranslation(ref offVec);
                        }
                    }
                    else if (av.AmmoEffect != null && av.AmmoDef.Const.AmmoParticle)
                    {
                        av.AmmoEffect.SetTranslation(ref av.TracerFront);
                    }
                }

                if (av.EndState.Dirty)
                    av.AvClose();
            }
        }

        internal void Draw()
        {
            UpdateOneFrameQuads();

            if (ActiveBillBoards.Count > 0 || PreAddPersistent.Count > 0)
                MyTransparentGeometry.ApplyActionOnPersistentBillboards(UpdatePersistentQuads);

            if (Session.I.Tick10 && Session.I.DebugMod && false)
            {
                var os = 0;
                var m = 0;
                var d = 0;
                var p = 0;
                var t = 0;
                foreach (var a in AvShots)
                {
                    if (a.OnScreen == AvShot.Screen.None)
                        os++;

                    if (a.MarkForClose)
                        m++;

                    if (a.EndState.Dirty)
                        d++;

                    if (a.OnScreen == AvShot.Screen.ProxyDraw)
                        p++;

                    if (a.TrailSteps.Count > 0)
                        t++;
                }
                Session.I.ShowLocalNotify($"cacheRemaining:{QuadCachePool.Count} - onScreen:{_onScreens}({os})[{p}] - hasTrail:{t} - dirty:{d} - marked:{m}", 160);

            }
        }

        internal void Run()
        {
            if (Session.I.Tick180)
            {
                Log.LineShortDate($"(DRAWS) --------------- AvShots:[{AvShots.Count}] OnScreen:[{_onScreens}] Shrinks:[{_shrinks}] Glows:[{_previousTrailCount}] Models:[{_models}] P:[{Session.I.Projectiles.ActiveProjetiles.Count}] P-Pool:[{Session.I.Projectiles.ProjectilePool.Count}] AvPool:[{AvShotPool.Count}] (AvBarrels 1:[{Effects1.Count}] 2:[{Effects2.Count}])", "stats");
                _previousTrailCount = 0;
                _shrinks = 0;
            }
            
            
            uint tick = Session.I.Tick;
            var advBLineCooldown = AdvBLineCacheCooldown[Session.I.Tick % AdvBLineCacheCooldown.Length];


            for (int j = AdvBLines.Count - 1; j >= 0; j--)
            {
                if (AdvBLines[j].EndTick <= tick)
                {
                    advBLineCooldown.Add(AdvBLines[j]);
                    AdvBLines.RemoveAtFast(j);
                }
                else
                {
                    var vel = AdvBLines[j].Velocity;
                    AdvBLines[j].Start += vel;
                    AdvBLines[j].End += vel;
                }
            }

            int advBillboardsAdded = AdvBLines.Count;
            var camPos = Session.I.CameraPos;
            for (int i = AdvBillboards.Count - 1; i >= 0; i--)
            {
                var av = AdvBillboards[i];
                var rnd = av?.Av?.AmmoDef?.Const?.Random;

                if (rnd == null || av.Av.MarkForClose)
                {
                    AdvBillboards.RemoveAtFast(i);
                    av.Av.AdvBillboards = null;
                    av.Clean();
                    continue;
                }

                advBillboardsAdded += av.Billboards.Count;
                bool calculatedAccel = false;
                Vector3 accel = Vector3D.Zero;

                for (int j = av.DrawnLines.Count - 1; j >= 0; j--)
                {
                    if (av.DrawnLines[j].EndTick <= tick)
                    {
                        advBLineCooldown.Add(av.DrawnLines[j]);
                        av.DrawnLines.RemoveAtFast(j);
                    }
                    else
                    {
                        var vel = av.DrawnLines[j].Velocity;
                        av.DrawnLines[j].Start += vel;
                        av.DrawnLines[j].End += vel;
                    }
                }

                for (int j = 0; j < av.LineDefs.Length; j++)
                {
                    var def = av.LineDefs[j];

                    if (def.DelayBetweenSpawns != 0 && av.CurrentLifetime % (def.DelayBetweenSpawns + 1) != 0)
                        continue;

                    if ((def.MaxViewDistanceSq > 0 && def.MaxViewDistanceSq < Vector3D.DistanceSquared(av.ProjectileMatrix.Translation, camPos))
                        || (def.MinViewDistanceSq > Vector3D.DistanceSquared(av.ProjectileMatrix.Translation, camPos)))
                        continue;

                    var mat = av.ProjectileMatrix;
                    mat.Translation = Vector3D.Zero;

                    var P0 = def.P0;
                    var P1 = def.P1;
                    var width = def.Width;
                    if (def.HasRotateSpeed)
                    {
                        MatrixD rotationMat;
                        if (!ComputedRotationMatricies.TryGetValue(def.RotateSpeed, out rotationMat))
                        {
                            rotationMat = MatrixD.CreateFromAxisAngle(mat.Forward, def.RotateSpeed * av.CurrentLifetime);
                            ComputedRotationMatricies[def.RotateSpeed] = rotationMat;
                        }

                        mat = MatrixD.CreateWorld(Vector3D.Zero, mat.Forward, Vector3D.TransformNormal(mat.Up, rotationMat));
                    }
                    if (def.OnlyDrawIfAccelerationAligned)
                    {
                        if (!calculatedAccel)
                        {
                            accel = Vector3D.TransformNormal((av.Velocity - av.PrevVelocity) * 0.5f - (def.AccelAccountForGrav ? av.Grav : Vector3.Zero), MatrixD.Transpose(mat));
                            calculatedAccel = true;
                        }

                        var dir = P1 - P0;
                        float dotProduct;
                        if (def.LengthAffectedByAccelAlignment)
                        {
                            var len = dir.Normalize();
                            dotProduct = Vector3.Dot(accel * def.AccelerationSizeMultiplier, dir);

                            var len2 = Math.Min(len, Math.Abs(dotProduct));
                            P1 = P0 + dir * len2;
                            width *= len2 / len;
                        }
                        else
                        {
                            dotProduct = Vector3.Dot(accel * def.AccelerationSizeMultiplier, dir);
                        }

                        if (dotProduct >= def.AccelerationDotReq)
                            continue;
                    }
                    

                    var line = AdvBLinesPool.Count > 0 ? AdvBLinesPool.Pop() : new AdvBLineCache();

                    line.LerpColor = def.ColorFade;
                    line.LerpWidth = def.WidthFade;
                    line.AlwaysDraw = def.AlwaysDraw;
                    line.StartTick = tick;
                    line.EndTick = tick + def.TimeRendered;

                    // pov mod profiler
                    Vector3 P0RndOffset = Vector3.Zero, P1RndOffset = Vector3.Zero;
                    if (def.P0RandomOffset > 0)
                    {
                        // gross inlined random
                        var tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        var tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var angle1 = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * MathHelper.Pi;

                        // gross inlined random
                        tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var angle2 = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * MathHelper.TwoPi;

                        // gross inlined random
                        tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var r = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * 2 * def.P0RandomOffset - def.P0RandomOffset;

                        var a1sin = MyMath.FastSin(angle1);
                        var a2sin = MyMath.FastSin(angle2);
                        var a1cos = MyMath.FastCos(angle1);
                        var a2cos = MyMath.FastCos(angle2);

                        P0RndOffset = new Vector3(a1sin * a2cos * r, a1sin * a2sin * r, a1cos * r);
                    }
                    if (def.P1RandomOffset > 0)
                    {
                        // gross inlined random
                        var tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        var tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var angle1 = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * MathHelper.Pi;

                        // gross inlined random
                        tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var angle2 = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * MathHelper.TwoPi;

                        // gross inlined random
                        tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var r = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * 2 * def.P1RandomOffset - def.P1RandomOffset;

                        var a1sin = MyMath.FastSin(angle1);
                        var a2sin = MyMath.FastSin(angle2);
                        var a1cos = MyMath.FastCos(angle1);
                        var a2cos = MyMath.FastCos(angle2);

                        P1RndOffset = new Vector3(a1sin * a2cos * r, a1sin * a2sin * r, a1cos * r);
                    }


                    line.Start = av.ProjectileMatrix.Translation + (def.TransformP0 ? (Vector3D)P0RndOffset : Vector3D.TransformNormal(P0 + P0RndOffset, mat));
                    line.End = av.ProjectileMatrix.Translation + (def.TransformP1 ? (Vector3D)P1RndOffset : Vector3D.TransformNormal(P1 + P1RndOffset, mat));

                    line.StartWidth = width;
                    line.StartColor = def.FactionColor == FactionColor.DontUse ? def.Color :
                        def.FactionColor == FactionColor.Foreground ? av.Av.FgFactionColor * def.Color : av.Av.BgFactionColor * def.Color;

                    line.Velocity = av.Velocity * def.VelocityInheritence * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
                    line.Material = def.Materials[(av.CurrentLifetime / (def.DelayBetweenSpawns + 1)) % def.Materials.Length];

                    av.DrawnLines.Add(line);
                }
                advBillboardsAdded += av.DrawnLines.Count;

                for (int j = 0; j < av.TrailDefs.Length; j++)
                {
                    var def = av.TrailDefs[j];
                    var trails = av.Trails[j];

                    if (MyUtils.IsZero(av.PrevPosition)) // first tick
                        continue;

                    if (trails.Count > 0 && trails.Peek().EndTick <= tick)
                    {
                        advBLineCooldown.Add(trails.Dequeue());
                    }
                    uint divisor = (uint)av.ClientAVLevel + def.DelayBetweenSpawns + 1;
                    if ((divisor > 1 && av.CurrentLifetime % divisor != 0)
                        || (def.MaxViewDistanceSq > 0 && def.MaxViewDistanceSq < Vector3D.DistanceSquared(av.ProjectileMatrix.Translation, camPos))
                        || (def.MinViewDistanceSq > Vector3D.DistanceSquared(av.ProjectileMatrix.Translation, camPos)))
                    {
                        
                        if (trails.Count > 0)
                        {
                            var prevTrail = trails.Last();
                            if (tick - prevTrail.StartTick <= divisor) // only extend trail up to divisor ticks so it doesn't bug out when rapidly traversing min/max view dists
                                prevTrail.Start = prevTrail.Start - av.PrevPosition + av.ProjectileMatrix.Translation; // with this simple trick I sawed the billboard count in half!
                        }
                        continue;
                    }

                    var line = AdvBLinesPool.Count > 0 ? AdvBLinesPool.Pop() : new AdvBLineCache();

                    line.LerpColor = def.ColorFade;
                    line.LerpWidth = def.WidthFade;
                    line.AlwaysDraw = def.AlwaysDraw;
                    line.StartTick = tick;
                    line.EndTick = tick + def.TimeRendered;

                    var mat = av.ProjectileMatrix;
                    if (def.HasRotateSpeed)
                    {
                        MatrixD rotationMat;
                        if (!ComputedRotationMatricies.TryGetValue(def.RotateSpeed, out rotationMat))
                        {
                            rotationMat = MatrixD.CreateFromAxisAngle(mat.Forward, def.RotateSpeed * av.CurrentLifetime);
                            ComputedRotationMatricies[def.RotateSpeed] = rotationMat;
                        }

                        mat = MatrixD.CreateWorld(mat.Translation, mat.Forward, Vector3D.TransformNormal(mat.Up, rotationMat));
                    }

                    // pov mod profiler
                    Vector3 P0RndOffset = Vector3.Zero;
                    if (def.P0RandomOffset > 0)
                    {
                        // gross inlined random
                        var tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        var tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        var tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var angle1 = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * MathHelper.Pi;

                        // gross inlined random
                        tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var angle2 = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * MathHelper.TwoPi;

                        // gross inlined random
                        tempX = rnd.Y;
                        rnd.X ^= rnd.X << 23;
                        tempY = rnd.X ^ rnd.Y ^ (rnd.X >> 17) ^ (rnd.Y >> 26);
                        tempZ = tempY + rnd.Y;
                        rnd.X = tempX;
                        rnd.Y = tempY;

                        var r = (float)XorShiftRandom.DoubleUnit * (0x7FFFFFFF & tempZ) * 2 * def.P0RandomOffset - def.P0RandomOffset;

                        var a1sin = MyMath.FastSin(angle1);
                        var a2sin = MyMath.FastSin(angle2);
                        var a1cos = MyMath.FastCos(angle1);
                        var a2cos = MyMath.FastCos(angle2);

                        P0RndOffset = new Vector3(a1sin * a2cos * r, a1sin * a2sin * r, a1cos * r);
                    }

                    var p0Transformed = def.TransformP0 ? (Vector3D)P0RndOffset : Vector3D.TransformNormal(def.P0 + P0RndOffset, mat);
                    line.Start = av.ProjectileMatrix.Translation + p0Transformed;

                    if (trails.Count > 0)
                    {
                        var lastTrail = trails.Last();
                        line.End = tick - lastTrail.StartTick <= divisor ? lastTrail.Start : av.PrevPosition + p0Transformed;
                    }
                    else
                        line.End = av.PrevPosition + p0Transformed;

                    line.StartWidth = def.Width;
                    line.StartColor = def.FactionColor == FactionColor.DontUse ? def.Color :
                    def.FactionColor == FactionColor.Foreground ? av.Av.FgFactionColor * def.Color : av.Av.BgFactionColor * def.Color;

                    line.Velocity = Vector3.Zero;
                    line.Material = def.Materials[(av.CurrentLifetime / divisor) % def.Materials.Length];

                    trails.Enqueue(line);
                    advBillboardsAdded += trails.Count;
                }

                for (int j = 0; j < av.BillboardDefs.Length; j++)
                {
                    av.Billboards[j].Render = false;

                    var def = av.BillboardDefs[j];
                    if ((def.MaxViewDistanceSq > 0 && def.MaxViewDistanceSq < Vector3D.DistanceSquared(av.ProjectileMatrix.Translation, camPos))
                        || (def.MinViewDistanceSq > Vector3D.DistanceSquared(av.ProjectileMatrix.Translation, camPos)))
                    {
                        continue;
                    }

                    var mat = av.ProjectileMatrix;
                    if (def.HasRotateSpeed)
                    {
                        MatrixD rotationMat;
                        if (!ComputedRotationMatricies.TryGetValue(def.RotateSpeed, out rotationMat))
                        {
                            rotationMat = MatrixD.CreateFromAxisAngle(mat.Forward, def.RotateSpeed * av.CurrentLifetime);
                            ComputedRotationMatricies[def.RotateSpeed] = rotationMat;
                        }

                        mat = MatrixD.CreateWorld(mat.Translation, mat.Forward, Vector3D.TransformNormal(mat.Up, rotationMat));
                    }

                    bool isTri = def.IsTri;
                    var P0 = av.ProjectileMatrix.Translation + Vector3D.TransformNormal(def.P0, mat);
                    var P1 = av.ProjectileMatrix.Translation + Vector3D.TransformNormal(def.P1, mat);
                    var P2 = av.ProjectileMatrix.Translation + Vector3D.TransformNormal(def.P2, mat);
                    var P3 = isTri ? P2 : av.ProjectileMatrix.Translation + Vector3D.TransformNormal(def.P3, mat);

                    var b = av.Billboards[j].Billboard;
                    b.Material = def.Materials[av.CurrentLifetime % def.Materials.Length];
                    b.LocalType = LocalTypeEnum.Custom;
                    b.Position0 = P0;
                    b.Position1 = P1;
                    b.Position2 = P2;
                    b.Position3 = P3;
                    b.UVOffset = Vector2.Zero;
                    b.UVSize = Vector2.One;
                    b.DistanceSquared = 0;
                    b.Color = def.FactionColor == FactionColor.DontUse ? def.Color :
                        def.FactionColor == FactionColor.Foreground ? av.Av.FgFactionColor : av.Av.BgFactionColor;
                    b.Reflectivity = 0;
                    b.CustomViewProjection = -1;
                    b.ParentID = uint.MaxValue;
                    b.ColorIntensity = 1f;
                    b.SoftParticleDistanceScale = 1f;
                    b.BlendType = BlendTypeEnum.Standard;

                    av.Billboards[j].Render = true;
                }
                av.CurrentLifetime++;
                
                ComputedRotationMatricies.Clear();
            }

            _onScreens = 0;
            _models = 0;
            for (int i = AvShots.Count - 1; i >= 0; i--)
            {
                var av = AvShots[i];
                if (av.OnScreen != AvShot.Screen.None && av.OnScreen != AvShot.Screen.ProxyDraw) _onScreens++;
                var refreshed = av.LastTick == Session.I.Tick;
                var aConst = av.AmmoDef.Const;
                var overDrawLimit = PreAddOneFrame.Count + advBillboardsAdded > 32000;
                if (overDrawLimit && av.Offsets?.Count > 0)
                    av.Offsets.Clear();

                if (refreshed && av.Tracer != AvShot.TracerState.Off && av.OnScreen != AvShot.Screen.None && !overDrawLimit)
                {
                    var color = av.Color;
                    var segColor = av.SegmentColor;

                    if (av.ShotFade > 0)
                    {
                        var fade = (float)MathHelperD.Clamp(1d - av.ShotFade, 0.005d, 1d);
                        color *= fade;
                        segColor *= fade;
                    }

                    if (!aConst.OffsetEffect)
                    {
                        if (av.OnScreen != AvShot.Screen.ProxyDraw && av.VisualLength >= 0.1)
                        {
                            if (av.Tracer != AvShot.TracerState.Shrink)
                            {
                                if (aConst.TracerMode == AmmoConstants.Texture.Normal)
                                {

                                    var qc = QuadCachePool.Count > 0 ? QuadCachePool.Pop() : new QuadCache();
                                    qc.Shot = av;
                                    qc.Material = aConst.TracerTextures[0];
                                    qc.Color = color;
                                    qc.StartPos = av.TracerBack;
                                    qc.Direction = av.VisualDir;
                                    qc.Length = (float)av.VisualLength;
                                    qc.Width = (float)av.TracerWidth;

                                    qc.Type = QuadCache.EffectTypes.Tracer;
                                    PreAddOneFrame.Add(qc);
                                    ++av.ActiveBillBoards;
                                }
                                else if (aConst.TracerMode != AmmoConstants.Texture.Resize)
                                {
                                    var qc = QuadCachePool.Count > 0 ? QuadCachePool.Pop() : new QuadCache();
                                    qc.Shot = av;
                                    qc.Material = aConst.TracerTextures[av.TextureIdx];
                                    qc.Color = color;
                                    qc.StartPos = av.TracerBack;
                                    qc.Direction = av.VisualDir;
                                    qc.Length = (float)av.VisualLength;
                                    qc.Width = (float)av.TracerWidth;
                                    qc.Type = QuadCache.EffectTypes.Tracer;
                                    PreAddOneFrame.Add(qc);
                                    ++av.ActiveBillBoards;
                                }
                                else if (av.VisualLength >= 0.2)
                                {
                                    var seg = av.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation;
                                    var stepPos = av.TracerBack;
                                    var segTextureCnt = aConst.SegmentTextures.Length;
                                    var gapTextureCnt = aConst.TracerTextures.Length;
                                    var segStepLen = seg.SegmentLength / segTextureCnt;
                                    var gapStepLen = seg.SegmentGap / gapTextureCnt;
                                    var gapEnabled = gapStepLen > 0;
                                    int j = 0;
                                    double travel = 0;
                                    while (travel < av.VisualLength)
                                    {

                                        var mod = j++ % 2;
                                        var gap = gapEnabled && (av.SegmentGaped && mod == 0 || !av.SegmentGaped && mod == 1);
                                        var first = travel <= 0;

                                        double width;
                                        double rawLen;
                                        Vector4 dyncColor;
                                        if (!gap)
                                        {
                                            rawLen = first ? av.SegmentLenTranserved * Session.I.ClientAvDivisor : seg.SegmentLength * Session.I.ClientAvDivisor;
                                            if (rawLen <= 0)
                                                break;
                                            width = av.SegmentWidth;
                                            dyncColor = segColor;
                                        }
                                        else
                                        {
                                            rawLen = first ? av.SegmentLenTranserved * Session.I.ClientAvDivisor : seg.SegmentGap * Session.I.ClientAvDivisor;
                                            if (rawLen <= 0)
                                                break;
                                            width = av.TracerWidth;
                                            dyncColor = color;
                                        }

                                        var notLast = travel + rawLen < av.VisualLength;
                                        var len = notLast ? rawLen : av.VisualLength - travel;
                                        var clampStep = !gap ? MathHelperD.Clamp((int)((len / segStepLen) + 0.5) - 1, 0, segTextureCnt - 1) : MathHelperD.Clamp((int)((len / gapStepLen) + 0.5) - 1, 0, gapTextureCnt - 1);
                                        var material = !gap ? aConst.SegmentTextures[(int)clampStep] : aConst.TracerTextures[(int)clampStep];
                                        var overCount = j > 5000;

                                        if (overCount || len < 0.1)
                                        {
                                            if (overCount)
                                                Log.Line($"segment looped until len was less than 0.1 or 5000 times... this is bad:{av.Weapon.System.ShortName} - {av.AmmoDef.AmmoRound} - len:{len}");
                                            break;
                                        }

                                        var qc = QuadCachePool.Count > 0 ? QuadCachePool.Pop() : new QuadCache();
                                        qc.Shot = av;
                                        qc.Material = material;
                                        qc.Color = dyncColor;
                                        qc.StartPos = stepPos;
                                        qc.Direction = av.VisualDir;
                                        qc.Length = (float)len;
                                        qc.Width = (float)width;
                                        qc.Type = QuadCache.EffectTypes.Segment;

                                        PreAddOneFrame.Add(qc);
                                        ++av.ActiveBillBoards;

                                        if (!notLast)
                                            travel = av.VisualLength;
                                        else
                                            travel += len;
                                        stepPos += (av.VisualDir * len);


                                    }
                                }
                            }
                        }
                    }
                    else if (av.Offsets != null)
                    {
                        var list = av.Offsets;
                        if (av.OnScreen != AvShot.Screen.ProxyDraw)
                        {
                            for (int x = 0; x < list.Count; x++)
                            {
                                Vector3D fromBeam;
                                Vector3D toBeam;
                                if (x == 0)
                                {
                                    fromBeam = av.OffsetMatrix.Translation;
                                    toBeam = Vector3D.Transform(list[x], av.OffsetMatrix);
                                }
                                else
                                {
                                    fromBeam = Vector3D.Transform(list[x - 1], av.OffsetMatrix);
                                    toBeam = Vector3D.Transform(list[x], av.OffsetMatrix);
                                }

                                Vector3 dir = (Vector3)(toBeam - fromBeam);
                                var length = dir.Length();
                                if (length >= 0.1)
                                {
                                    var normDir = dir / length;

                                    var qc = QuadCachePool.Count > 0 ? QuadCachePool.Pop() : new QuadCache();
                                    qc.Shot = av;
                                    qc.Material = aConst.TracerTextures[0];
                                    qc.Color = color;
                                    qc.StartPos = fromBeam;
                                    qc.Direction = normDir;
                                    qc.Length = length;
                                    qc.Width = (float)av.TracerWidth;
                                    qc.Type = QuadCache.EffectTypes.Offset;
                                    PreAddOneFrame.Add(qc);
                                    ++av.ActiveBillBoards;
                                }


                                if (Vector3D.DistanceSquared(av.OffsetMatrix.Translation, toBeam) > av.TracerLengthSqr) break;
                            }
                        }
                        list.Clear();
                    }
                }

                var shrinkCnt = av.TracerShrinks.Count;
                if (shrinkCnt > _shrinks) _shrinks = shrinkCnt;

                if (shrinkCnt > 0)
                    RunShrinks(av, overDrawLimit);

                var trailCount = av.TrailSteps?.Count ?? 0;

                if (trailCount > _previousTrailCount)
                    _previousTrailCount = trailCount;

                if (av.Trail != AvShot.TrailState.Off)
                {
                    var steps = av.DecayTime;
                    var widthScaler = !aConst.TrailColorFade;
                    var remove = false;
                    for (int j = trailCount - 1; j >= 0; j--)
                    {
                        var trail = av.TrailSteps[j];

                        if (!refreshed)
                            trail.Line = new LineD(trail.Line.From + av.ShootVelStep, trail.Line.To + av.ShootVelStep, trail.Line.Length);

                        if (av.OnScreen != AvShot.Screen.None && av.OnScreen != AvShot.Screen.ProxyDraw && !overDrawLimit)
                        {
                            var reduction = (av.TrailShrinkSize * trail.Step);
                            var width = widthScaler ? (aConst.TrailWidth - reduction) * av.TrailScaler : aConst.TrailWidth * av.TrailScaler;
                            var skipFactionColor = aConst.TrailFactionColor == FactionColor.DontUse || av.FgFactionColor == Vector4.Zero && av.BgFactionColor == Vector4.Zero;
                            var color = skipFactionColor ? aConst.LinearTrailColor : aConst.TrailFactionColor == FactionColor.Foreground ? av.FgFactionColor : av.BgFactionColor;
                            if (!widthScaler)
                            {
                                color *= MathHelper.Clamp(1f - reduction, 0.01f, 1f);
                            }

                            if (trail.Line.Length >= 0.1)
                            {
                                var qCache = QuadCachePool.Count > 0 ? QuadCachePool.Pop() : new QuadCache();

                                qCache.Shot = av;
                                qCache.Material = aConst.TrailTextures[0];
                                qCache.Color = color;
                                qCache.StartPos = trail.Line.From;
                                qCache.Direction = trail.Line.Direction;
                                qCache.Length = (float)trail.Line.Length;
                                qCache.Width = width;
                                qCache.Type = QuadCache.EffectTypes.Trail;
                                PreAddOneFrame.Add(qCache);
                                ++av.ActiveBillBoards;
                            }
                        }

                        if (++trail.Step >= steps)
                        {
                            trail.Parent = null;
                            trail.Step = 0;
                            remove = true;
                            trailCount--;
                            Trails.Push(trail);
                        }
                    }

                    if (remove) 
                        av.TrailSteps.Dequeue();
                }

                if (trailCount == 0 && shrinkCnt == 0 && av.MarkForClose)
                {
                    av.Close();
                    AvShots.RemoveAtFast(i);
                }
            }
        }

        private void RunShrinks(AvShot av, bool overDrawLimit)
        {
            var s = av.TracerShrinks.Dequeue();
            if (av.LastTick != Session.I.Tick)
            {
                if (av.OnScreen != AvShot.Screen.ProxyDraw && s.Length >= 0.1)
                {
                    if (!av.AmmoDef.Const.OffsetEffect)
                    {
                        if (av.OnScreen != AvShot.Screen.None && !overDrawLimit)
                        {
                            var qc = QuadCachePool.Count > 0 ? QuadCachePool.Pop() : new QuadCache();
                            qc.Shot = av;
                            qc.Material = av.AmmoDef.Const.TracerTextures[0];
                            qc.Color = s.Color;
                            qc.StartPos = s.NewFront;
                            qc.Direction = av.VisualDir;
                            qc.Length = s.Length;
                            qc.Width = s.Thickness;
                            qc.Type = QuadCache.EffectTypes.Shrink;
                            PreAddOneFrame.Add(qc);
                            ++av.ActiveBillBoards;
                        }
                    }
                    else if (av.OnScreen != AvShot.Screen.None && !overDrawLimit && s.Length >= 0.2)
                        av.DrawLineOffsetEffect(s.NewFront, -av.Direction, s.Length, s.Thickness, s.Color);
                }

                if (av.Trail != AvShot.TrailState.Off && av.Back)
                    av.RunTrail(s, true);
            }

            if (av.TracerShrinks.Count == 0) av.ResetHit();
        }

        internal int AddAdvLineBillboards(List<MyBillboard> BillBoardsToAdd, IEnumerable<AdvBLineCache> lines, ref BoundingSphereD testSphere, uint tick, IMyCamera cam)
        {
            int ret = 0;
            foreach (var line in lines)
            {
                var midpoint = (line.End + line.Start) * 0.5f;
                testSphere = new BoundingSphereD(midpoint, 1); // just want to test a point
                if (cam.IsInFrustum(ref testSphere))
                {
                    var b = line.Billboard;
                    float oneMinust = 1;
                    if (line.LerpColor || line.LerpWidth)
                    {
                        oneMinust = 1 - (float)(tick - line.StartTick) / (line.EndTick - line.StartTick);
                    }
                    var color = line.StartColor * oneMinust;
                    var width = line.StartWidth * oneMinust;

                    var d = cam.Position - midpoint;
                    var dist = d.Normalize();
                    var up = Vector3D.Cross(line.Start - line.End, d).Normalized() * width;

                    b.Material = line.Material;
                    b.LocalType = LocalTypeEnum.Custom;
                    b.Position0 = line.Start - up;
                    b.Position1 = line.End   - up;
                    b.Position2 = line.End   + up;
                    b.Position3 = line.Start + up;
                    b.UVOffset = Vector2.Zero;
                    b.UVSize = Vector2.One;
                    b.DistanceSquared = (float)(dist * dist);
                    b.Color = color;
                    b.Reflectivity = 0;
                    b.CustomViewProjection = -1;
                    b.ParentID = uint.MaxValue;
                    b.ColorIntensity = 1f;
                    b.SoftParticleDistanceScale = 1f;
                    b.BlendType = BlendTypeEnum.Standard;

                    BillBoardsToAdd.Add(b);
                    ret++;
                }

                line.Start += line.Velocity;
                line.End += line.Velocity;
            }
            return ret;
        }
        
        internal void UpdateOneFrameQuads()
        {
            BoundingSphereD testSphere = new BoundingSphereD();
            
            var cam = Session.I.Camera;
            var tick = Session.I.Tick;
            int advBillboardsDrawn = 0;
            int advBillboardsLines = 0;
            int advBillboardsTrails = 0;
            int advBillboardsOrphans = AddAdvLineBillboards(BillBoardsToAdd, AdvBLines, ref testSphere, tick, cam);
            // manually test every line as these may be far from the actual projectile
            // and frustum check is relatively quick compared to how slow rendering a billboard is (thanks keen)
            for (int n = 0; n < AdvBillboards.Count; n++)
            {
                var av = AdvBillboards[n];

                advBillboardsLines += AddAdvLineBillboards(BillBoardsToAdd, av.DrawnLines, ref testSphere, tick, cam);

                foreach (var trail in av.Trails)
                {
                    advBillboardsTrails += AddAdvLineBillboards(BillBoardsToAdd, trail, ref testSphere, tick, cam);
                }

                foreach (var b in av.Billboards)
                {
                    if (!b.Render)
                        continue;

                    var midpoint = b.IsTri ?
                        (b.Billboard.Position0 + b.Billboard.Position1 + b.Billboard.Position2) / 3 : 
                        (b.Billboard.Position0 + b.Billboard.Position1 + b.Billboard.Position2 + b.Billboard.Position3) / 4;
                    testSphere = new BoundingSphereD(midpoint, 1); // just want to test a point
                    if (cam.IsInFrustum(ref testSphere))
                    {
                        b.Billboard.DistanceSquared = (float)Vector3D.DistanceSquared(cam.Position, midpoint);
                        BillBoardsToAdd.Add(b.Billboard);
                        advBillboardsDrawn++;
                    }
                }
            }
            if (Session.I.Tick180)
            {
                Log.LineShortDate($"(ADV DRAWS) --------------- O:{advBillboardsOrphans} L:{advBillboardsLines} T:{advBillboardsTrails} B:{advBillboardsDrawn}", "stats");
                _previousTrailCount = 0;
                _shrinks = 0;
            }
            advBillboardsDrawn += advBillboardsLines + advBillboardsTrails + advBillboardsOrphans;
            
            var requestCount = PreAddOneFrame.Count;
            if (requestCount > 0)
            {
                var coolDown = QuadCacheCoolDown[Session.I.Tick % QuadCacheCoolDown.Length];
                for (int i = PreAddOneFrame.Count - 1; i >= 0; i--)
                {
                    var q = PreAddOneFrame[i];
                    coolDown.Add(q);

                    var b = q.BillBoard;

                    if (q.Shot != null)
                    {
                        --q.Shot.ActiveBillBoards;
                        q.Shot = null;
                    }

                    var cameraPosition = Session.I.CameraPos;
                    if (!Vector3D.IsZero(cameraPosition - q.StartPos, 1E-06))
                    {
                        var polyLine = new MyPolyLineD
                        {
                            LineDirectionNormalized = (Vector3)q.Direction,
                            Point0 = q.StartPos,
                            Point1 = q.StartPos + q.Direction * q.Length,
                            Thickness = q.Width
                        };

                        MyQuadD retQuad;
                        MyUtils.GetPolyLineQuad(out retQuad, ref polyLine, cameraPosition);

                        b.Material = q.Material;
                        b.LocalType = LocalTypeEnum.Custom;
                        b.Position0 = retQuad.Point0;
                        b.Position1 = retQuad.Point1;
                        b.Position2 = retQuad.Point2;
                        b.Position3 = retQuad.Point3;
                        b.UVOffset = Vector2.Zero;
                        b.UVSize = Vector2.One;
                        b.DistanceSquared = (float)Vector3D.DistanceSquared(cameraPosition, q.StartPos);
                        b.Color = q.Color;
                        b.Reflectivity = 0;
                        b.CustomViewProjection = -1;
                        b.ParentID = uint.MaxValue;
                        b.ColorIntensity = 1f;
                        b.SoftParticleDistanceScale = 1f;
                        b.BlendType = BlendTypeEnum.Standard;

                        BillBoardsToAdd.Add(b);
                    }
                }
                PreAddOneFrame.Clear();
            }
            if (requestCount + advBillboardsDrawn > NearBillBoardLimit)
                NearBillBoardLimit = requestCount + advBillboardsDrawn;

            if (BillBoardsToAdd.Count > 0)
            {
                MyTransparentGeometry.AddBillboards(BillBoardsToAdd, false);
                BillBoardsToAdd.Clear();
            }
        }

        internal void UpdatePersistentQuads()
        {
            PersistentQuadsUpdate(BillBoardRequest.Update);

            if (PreAddPersistent.Count > 0)
                BillBoardPrePersistentQuads();
        }

        public enum BillBoardRequest
        {
            Add,
            Update,
            Purge
        }


        internal void BillBoardPrePersistentQuads()
        {
            for (int i = PreAddPersistent.Count - 1; i >= 0; i--)
            {
                var q = PreAddPersistent[i];
                var b = q.BillBoard;

                if (q.Age > 0 || q.LifeTime == 0 || q.Updated)
                {
                    Log.Line($"should not be possible: updated:{q.Updated} - age:{q.Age} - lifeTime:{q.LifeTime} - type:{q.Type} - marked:{q.MarkedForCloseIn} - owner:{q.Owner != null}");
                }
                else
                {
                    if (q.MarkedForCloseIn <= 0)
                        Log.Line($"adding while marked: {q.Type} - age:{q.Age} - marked:{q.MarkedForCloseIn} - owner:{q.Owner != null}");
                }

                q.Updated = true;

                var cameraPosition = Session.I.CameraPos;
                if (Vector3D.IsZero(cameraPosition - q.StartPos, 1E-06))
                    return;


                var polyLine = new MyPolyLineD
                {
                    LineDirectionNormalized = (Vector3)q.Up,
                    Point0 = q.StartPos,
                    Point1 = q.StartPos + q.Up * q.Width,
                    Thickness = q.Height
                };

                MyQuadD retQuad;
                MyUtils.GetPolyLineQuad(out retQuad, ref polyLine, cameraPosition);

                b.Material = q.Material;
                b.LocalType = LocalTypeEnum.Custom;
                b.Position0 = retQuad.Point0;
                b.Position1 = retQuad.Point1;
                b.Position2 = retQuad.Point2;
                b.Position3 = retQuad.Point3;
                b.UVOffset = Vector2.Zero;
                b.UVSize = Vector2.One;
                b.DistanceSquared = (float)Vector3D.DistanceSquared(cameraPosition, q.StartPos);
                b.Color = q.Color;
                b.Reflectivity = 0;
                b.CustomViewProjection = -1;
                b.ParentID = uint.MaxValue;
                b.ColorIntensity = 1f;
                b.SoftParticleDistanceScale = 1f;
                b.BlendType = BlendTypeEnum.Standard;

                BillBoardsToAdd.Add(b);
                ActiveBillBoards.Add(q);

            }

            MyTransparentGeometry.AddBillboards(BillBoardsToAdd, true);
            BillBoardsToAdd.Clear();
            PreAddPersistent.Clear();
        }

        internal void PersistentQuadsUpdate(BillBoardRequest request)
        {
            for (int i = ActiveBillBoards.Count - 1; i >= 0; i--)
            {
                var q = ActiveBillBoards[i];
                var b = q.BillBoard;
                var a = q.Shot;

                if (request == BillBoardRequest.Purge)
                {
                    BillBoardsToRemove.Add(b);
                    continue;
                }
                var timeExpired = ++q.Age >= q.LifeTime;
                if (timeExpired || q.MarkedForCloseIn-- == 0 || !q.Updated)
                {
                    --a.ActiveBillBoards;
                    BillBoardsToRemove.Add(b);
                    ActiveBillBoards.RemoveAtFast(i);
                    switch (q.Type)
                    {
                        case QuadPersistentCache.EffectTypes.Tracer:
                            break;
                        default:
                            q.LifeTime = 0;
                            QuadPersistentCacheCoolDown[Session.I.Tick % QuadPersistentCacheCoolDown.Length].Add(q);
                            q.LifeTime = int.MaxValue;
                            q.Added = false;
                            q.MarkedForCloseIn = int.MaxValue;
                            q.Age = 0;
                            q.Updated = false;
                            q.Owner = null;
                            q.Shot = null;
                            break;
                    }
                    continue;
                }
                
                q.Updated = false;

                var cameraPosition = Session.I.CameraPos;
                if (Vector3D.IsZero(cameraPosition - q.StartPos, 1E-06))
                    return;

                var polyLine = new MyPolyLineD
                {
                    LineDirectionNormalized = (Vector3)q.Up,
                    Point0 = q.StartPos,
                    Point1 = q.StartPos + q.Up * q.Width,
                    Thickness = q.Height
                };

                MyQuadD retQuad;
                MyUtils.GetPolyLineQuad(out retQuad, ref polyLine, cameraPosition);

                b.Material = q.Material;
                b.LocalType = LocalTypeEnum.Custom;
                b.Position0 = retQuad.Point0;
                b.Position1 = retQuad.Point1;
                b.Position2 = retQuad.Point2;
                b.Position3 = retQuad.Point3;
                b.UVOffset = Vector2.Zero;
                b.UVSize = Vector2.One;
                b.DistanceSquared = (float)Vector3D.DistanceSquared(cameraPosition, q.StartPos);
                b.Color = q.Color;
                b.Reflectivity = 0;
                b.CustomViewProjection = -1;
                b.ParentID = uint.MaxValue;
                b.ColorIntensity = 1f;
                b.SoftParticleDistanceScale = 1f;
                b.BlendType = BlendTypeEnum.Standard;

            }

            if (BillBoardsToRemove.Count > 0)
            {
                MyTransparentGeometry.RemovePersistentBillboards(BillBoardsToRemove);
                BillBoardsToRemove.Clear();
            }
        }

        internal void RunAvEffects1()
        {
            for (int i = Effects1.Count - 1; i >= 0; i--)
            {

                var avEffect = Effects1[i];
                var weapon = avEffect.Weapon;
                var muzzle = avEffect.Muzzle;
                var ticksAgo = Session.I.Tick - avEffect.StartTick;
                var ammoParticleOverride = weapon.ActiveAmmoDef.AmmoDef.Const.OverrideWeaponEffect;
                var bAv = ammoParticleOverride ? weapon.ActiveAmmoDef.AmmoDef.AmmoGraphics.Particles.WeaponEffect1Override : weapon.System.Values.HardPoint.Graphics.Effect1;
                var effect = weapon.Effects1[muzzle.MuzzleId];

                var effectExists = effect != null;
                if (effectExists && avEffect.EndTick == 0 && weapon.StopBarrelAvTick >= Session.I.Tick - 1)
                    avEffect.EndTick = weapon.StopBarrelAvTick;

                var info = weapon.Dummies[muzzle.MuzzleId].Info;
                var somethingEnded = avEffect.EndTick != 0 && avEffect.EndTick <= Session.I.Tick || !weapon.PlayTurretAv || info.Entity == null || info.Entity.MarkedForClose || weapon.Comp.Ai == null || weapon.MuzzlePart.Entity?.Parent == null && weapon.Comp.GunBase == null || weapon.Comp.CoreEntity.MarkedForClose || weapon.MuzzlePart.Entity == null || weapon.MuzzlePart.Entity.MarkedForClose;

                var effectStale = effectExists && (effect.IsEmittingStopped || effect.IsStopped) || !effectExists && ticksAgo > 0;
                if (effectStale || somethingEnded || !weapon.Comp.IsWorking)
                {
                    if (effectExists)
                    {
                        effect.Stop(bAv.Extras.Restart);
                        weapon.Effects1[muzzle.MuzzleId] = null;
                    }
                    muzzle.Av1Looping = false;
                    muzzle.LastAv1Tick = 0;
                    Effects1.RemoveAtFast(i);
                    avEffect.Clean(AvEffectPool);
                    continue;
                }

                if (weapon.Comp.Ai.VelocityUpdateTick != Session.I.Tick)
                {
                    weapon.Comp.Ai.TopEntityVolume.Center = weapon.Comp.TopEntity.PositionComp.WorldVolume.Center;
                    weapon.Comp.Ai.TopEntityVel = weapon.Comp.TopEntity.Physics?.LinearVelocity ?? Vector3.Zero;
                    weapon.Comp.Ai.IsStatic = weapon.Comp.TopEntity.Physics?.IsStatic ?? false;
                    weapon.Comp.Ai.VelocityUpdateTick = Session.I.Tick;
                }


                var renderId = info.Entity.Render.GetRenderObjectID();
                var matrix = info.DummyMatrix;
                var pos = info.Position;
                matrix.Translation = info.LocalPosition + bAv.Offset;

                if (!effectExists && ticksAgo <= 0)
                {
                    MyParticleEffect newEffect;
                    if (MyParticlesManager.TryCreateParticleEffect(bAv.Name, ref matrix, ref pos, renderId, out newEffect))
                    {
                        newEffect.UserScale = bAv.Extras.Scale;
                        if (newEffect.Loop)
                        {
                            weapon.Effects1[muzzle.MuzzleId] = newEffect;
                            muzzle.Av1Looping = true;
                        }
                        else
                        {
                            muzzle.Av1Looping = false;
                            muzzle.LastAv1Tick = 0;
                            Effects1.RemoveAtFast(i);
                            avEffect.Clean(AvEffectPool);
                        }
                    }
                }
                else if (effectExists)
                {
                    effect.WorldMatrix = matrix;
                }
            }
        }

        internal void RunAvEffects2()
        {
            for (int i = Effects2.Count - 1; i >= 0; i--)
            {
                var av = Effects2[i];
                var weapon = av.Weapon;
                var muzzle = av.Muzzle;
                var ticksAgo = Session.I.Tick - av.StartTick;
                var bAv = weapon.System.Values.HardPoint.Graphics.Effect2;

                var effect = weapon.Effects2[muzzle.MuzzleId];
                var effectExists = effect != null;
                if (effectExists && av.EndTick == 0 && weapon.StopBarrelAvTick >= Session.I.Tick - 1)
                    av.EndTick = weapon.StopBarrelAvTick;

                var info = weapon.Dummies[muzzle.MuzzleId].Info;
                var somethingEnded = av.EndTick != 0 && av.EndTick <= Session.I.Tick || !weapon.PlayTurretAv || info.Entity == null || info.Entity.MarkedForClose || weapon.Comp.Ai == null || weapon.MuzzlePart.Entity?.Parent == null && weapon.Comp.GunBase == null || weapon.Comp.CoreEntity.MarkedForClose || weapon.MuzzlePart.Entity == null || weapon.MuzzlePart.Entity.MarkedForClose;

                var effectStale = effectExists && (effect.IsEmittingStopped || effect.IsStopped) || !effectExists && ticksAgo > 0;

                if (effectStale || somethingEnded || !weapon.Comp.IsWorking)
                {
                    if (effectExists)
                    {
                        effect.Stop(bAv.Extras.Restart);
                        weapon.Effects2[muzzle.MuzzleId] = null;
                    }
                    muzzle.Av2Looping = false;
                    muzzle.LastAv2Tick = 0;
                    Effects2.RemoveAtFast(i);
                    av.Clean(AvEffectPool);
                    continue;
                }

                if (weapon.Comp.Ai.VelocityUpdateTick != Session.I.Tick)
                {
                    weapon.Comp.Ai.TopEntityVolume.Center = weapon.Comp.TopEntity.PositionComp.WorldVolume.Center;
                    weapon.Comp.Ai.TopEntityVel = weapon.Comp.TopEntity.Physics?.LinearVelocity ?? Vector3.Zero;
                    weapon.Comp.Ai.IsStatic = weapon.Comp.TopEntity.Physics?.IsStatic ?? false;
                    weapon.Comp.Ai.VelocityUpdateTick = Session.I.Tick;
                }

                var renderId = info.Entity.Render.GetRenderObjectID();
                var matrix = info.DummyMatrix;
                var pos = info.Position;
                matrix.Translation = info.LocalPosition + bAv.Offset;

                if (!effectExists && ticksAgo <= 0)
                {
                    MyParticleEffect newEffect;
                    if (MyParticlesManager.TryCreateParticleEffect(bAv.Name, ref matrix, ref pos, renderId, out newEffect))
                    {
                        newEffect.UserScale = bAv.Extras.Scale;

                        if (newEffect.Loop)
                        {
                            weapon.Effects2[muzzle.MuzzleId] = newEffect;
                            muzzle.Av2Looping = true;
                        }
                        else
                        {
                            muzzle.Av2Looping = false;
                            muzzle.LastAv2Tick = 0;
                            Effects2.RemoveAtFast(i);
                            av.Clean(AvEffectPool);
                        }
                    }
                }
                else if (effectExists)
                {

                    effect.WorldMatrix = matrix;
                }
            }
        }

        internal void RunAvHitEffects()
        {
            for (int i = HitParticles.Count - 1; i >= 0; i--)
            {
                var av = HitParticles[i];
                if (av.MarkedForClose)
                {
                    HitParticles.RemoveAtFast(i);
                    continue;
                }
                var position = av.Effect.WorldMatrix.Translation + av.Velocity;
                av.Effect.SetTranslation(ref position);
            }
        }

        internal void AvShotCleanUp()
        {
            var avShotCollection = AvShotCoolDown[Session.I.Tick % AvShotCoolDown.Length];

            for (int i = 0; i < avShotCollection.Count; i++)
                AvShotPool.Push(avShotCollection[i]);
            avShotCollection.Clear();

            var quadCacheCollection = QuadCacheCoolDown[Session.I.Tick % QuadCacheCoolDown.Length];

            for (int i = 0; i < quadCacheCollection.Count; i++)
                QuadCachePool.Push(quadCacheCollection[i]);
            quadCacheCollection.Clear();

            var lineCacheCollection = AdvBLineCacheCooldown[Session.I.Tick % AdvBLineCacheCooldown.Length];

            for (int i = 0; i < lineCacheCollection.Count; i++)
                AdvBLinesPool.Push(lineCacheCollection[i]);
            lineCacheCollection.Clear();
        }

        internal void Clean()
        {
            foreach (var p in Session.I.Projectiles.ProjectilePool)
                p.Info?.AvShot?.AmmoEffect?.Stop();

            foreach (var av in AvShots)
                av.Close();
            AvShots.Clear();
            HitParticles.Clear();

            BeamEffects.Clear();
            AvShotPool.Clear();
            AvEffectPool.Clear();
            Trails.Clear();
            Shrinks.Clear();
            QuadCachePool.Clear();
            OffSetLists.Clear();

            FireEmitters.Clear();
            TravelEmitters.Clear();
            PersistentEmitters.Clear();

            Effects1.Clear();
            Effects2.Clear();

            ParticlesToProcess.Clear();
            PreAddPersistent.Clear();
            BillBoardsToAdd.Clear();
            BillBoardsToRemove.Clear();

            AvShotPool.Clear();

            ActiveBillBoards.Clear();
        }

        internal class AvEffect
        {
            internal Weapon Weapon;
            internal Weapon.Muzzle Muzzle;
            internal uint StartTick;
            internal uint EndTick;

            internal void Clean(Stack<AvEffect> avEffectPool)
            {
                Weapon = null;
                Muzzle = null;
                StartTick = 0;
                EndTick = 0;
                avEffectPool.Push(this);
            }
        }
    }
}

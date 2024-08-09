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

                    if (av.HitParticle == AvShot.ParticleState.Custom)
                    {
                        av.HitParticle = AvShot.ParticleState.Dirty;
                        if (av.OnScreen != AvShot.Screen.None)
                        {
                            var pos = Session.I.Tick - av.Hit.HitTick <= 1 && !MyUtils.IsZero(av.Hit.SurfaceHit) ? av.Hit.SurfaceHit : av.TracerFront;
                            var particle = av.AmmoDef.AmmoGraphics.Particles.Hit;
                            var keenStrikesAgain = particle.Offset == Vector3D.MaxValue;
                            MatrixD matrix = MatrixD.CreateTranslation(pos);
                            if (keenStrikesAgain)
                            {
                                matrix = MatrixD.CreateWorld(pos, av.VisualDir, av.OriginUp);
                            }
                            else if (particle.Offset == Vector3D.MinValue)
                            {
                                float interference;
                                Vector3D localGrav = Session.I.Physics.CalculateNaturalGravityAt(pos, out interference);
                                localGrav.Normalize();
                                if (localGrav != Vector3D.Zero)
                                    matrix = MatrixD.CreateWorld(pos, Vector3D.CalculatePerpendicularVector(localGrav), -localGrav);
                            }

                            MyParticleEffect hitEffect;
                            if (MyParticlesManager.TryCreateParticleEffect(av.AmmoDef.Const.HitParticleStr, ref matrix, ref pos, uint.MaxValue, out hitEffect))
                            {
                                hitEffect.UserScale = av.AmmoDef.AmmoGraphics.Particles.Hit.Extras.Scale;
                                var tickVelo = av.Hit.HitVelocity / 60;
                                HitParticles.Add(new HitParticleEvent(hitEffect, tickVelo));
                                if (hitEffect.Loop)
                                    hitEffect.Stop();
                            }
                        }
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
                                    Normal = av.Direction,
                                };

                                //MyDecals.HandleAddDecal(av.Hit.Entity, hitInfo, Vector3.Zero, materialType, projectileMaterial, null, -1, voxelMaterial, false, MyDecalFlags.IgnoreOffScreenDeletion, MyAPIGateway.Session.GameplayFrameCounter + av.AmmoDef.AmmoGraphics.Decals.MaxAge);
                            }
                        }
                    }

                    if (av.Hit.EventType == HitEntity.Type.Water)
                    {
                        var splashHit = av.Hit.SurfaceHit;//Hopefully we can get a more precise surface intercept or correction?
                        var ammoInfo = av.AmmoDef;
                        var radius = ammoInfo.Const.CollisionSize > ammoInfo.Const.LargestHitSize ? (float)ammoInfo.Const.CollisionSize : (float)ammoInfo.Const.LargestHitSize;
                        if (radius < 3)
                            radius = 3;

                        WaterModAPI.CreateSplash(splashHit, radius, true);
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
            _onScreens = 0;
            _models = 0;
            for (int i = AvShots.Count - 1; i >= 0; i--)
            {
                var av = AvShots[i];
                if (av.OnScreen != AvShot.Screen.None && av.OnScreen != AvShot.Screen.ProxyDraw) _onScreens++;
                var refreshed = av.LastTick == Session.I.Tick;
                var aConst = av.AmmoDef.Const;
                var overDrawLimit = PreAddOneFrame.Count > 32000;
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

                                Vector3 dir = (toBeam - fromBeam);
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

        internal void UpdateOneFrameQuads()
        {
            var requestCount = PreAddOneFrame.Count;
            if (requestCount > 0)
            {
                if (requestCount > NearBillBoardLimit)
                    NearBillBoardLimit = requestCount;
                
                BillBoardOneFrameQuads();
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

        internal void BillBoardOneFrameQuads()
        {
            var coolDown = QuadCacheCoolDown[Session.I.Tick % QuadCacheCoolDown.Length];
            for (int i = PreAddOneFrame.Count - 1; i >= 0; i--)
            {
                var q = PreAddOneFrame[i];
                coolDown.Add(q);

                var b = q.BillBoard;

                if (q.Shot != null) {
                    --q.Shot.ActiveBillBoards;
                    q.Shot = null;
                }

                var cameraPosition = Session.I.CameraPos;
                if (!Vector3D.IsZero(cameraPosition - q.StartPos, 1E-06))
                {
                    var polyLine = new MyPolyLineD {
                        LineDirectionNormalized = q.Direction,
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

            MyTransparentGeometry.AddBillboards(BillBoardsToAdd, false);
            BillBoardsToAdd.Clear();
            PreAddOneFrame.Clear();
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
                    LineDirectionNormalized = q.Up,
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
                    LineDirectionNormalized = q.Up,
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
                    weapon.Comp.Ai.TopEntityVel = weapon.Comp.TopEntity.Physics?.LinearVelocity ?? Vector3D.Zero;
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
                    weapon.Comp.Ai.TopEntityVel = weapon.Comp.TopEntity.Physics?.LinearVelocity ?? Vector3D.Zero;
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

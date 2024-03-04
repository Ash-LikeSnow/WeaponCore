using System;
using System.Collections.Generic;
using CoreSystems.Support;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.PartAnimation;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AnimationDef;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;

namespace CoreSystems
{
    public partial class Session
    {
        internal static void CreateAnimationSets(AnimationDef animations, CoreSystem system, out Dictionary<EventTriggers, PartAnimation[]> weaponAnimationSets, out Dictionary<string, EmissiveState> weaponEmissivesSet, out Dictionary<string, Matrix[]> weaponLinearMoveSet, out HashSet<string> animationIdLookup, out Dictionary<EventTriggers, uint> animationLengths, out string[] heatingSubpartNames, out Dictionary<EventTriggers, ParticleEvent[]> particleEvents)
        {
            weaponAnimationSets = new Dictionary<EventTriggers, PartAnimation[]>();
            particleEvents = new Dictionary<EventTriggers, ParticleEvent[]>();
            weaponEmissivesSet = new Dictionary<string, EmissiveState>();
            animationIdLookup = new HashSet<string>();
            animationLengths = new Dictionary<EventTriggers, uint>();
            weaponLinearMoveSet = new Dictionary<string, Matrix[]>();

            var emissiveLookup = new Dictionary<string, PartEmissive>();

            CompileHeating(animations, emissiveLookup, out heatingSubpartNames);

            CompileParticles(animations, particleEvents);

            if (animations.AnimationSets == null)
                return;

            var allAnimationSet = new Dictionary<EventTriggers, HashSet<PartAnimation>>();
            CompileAnimationSets(system, animations.AnimationSets, allAnimationSet, animationLengths, animationIdLookup, weaponEmissivesSet, emissiveLookup, weaponLinearMoveSet);

            FinalizeAnimationSets(allAnimationSet, weaponAnimationSets);
        }

        internal static Dictionary<EventTriggers, PartAnimation[]> CreateWeaponAnimationSet(WeaponSystem system, RecursiveSubparts parts)
        {
            if (!system.AnimationsInited)
            {
                var allAnimationSet = new Dictionary<EventTriggers, PartAnimation[]>();
                foreach (var animationSet in system.WeaponAnimationSet)
                {
                    allAnimationSet[animationSet.Key] = new PartAnimation[animationSet.Value.Length];

                    for (int i = 0; i < animationSet.Value.Length; i++)
                    {
                        var animation = animationSet.Value[i];

                        MyEntity part;
                        if (!parts.NameToEntity.TryGetValue(animation.SubpartId, out part)) continue;

                        var rotations = new Matrix[animation.RotationSet.Length];
                        var rotCenters = new Matrix[animation.RotCenterSet.Length];
                        animation.RotationSet.CopyTo(rotations, 0);
                        animation.RotCenterSet.CopyTo(rotCenters, 0);

                        var rotCenterNames = animation.RotCenterNameSet;

                        if (!animation.SubpartId.Equals("None"))
                        {
                            var partMatrix = GetPartDummy("subpart_" + animation.SubpartId, part.Parent.Model, I.DummyList)?.Matrix ?? Matrix.Identity;
                            var partCenter = partMatrix.Translation;

                            for (int j = 0; j < rotations.Length; j++)
                            {
                                if (rotations[j] != Matrix.Zero)
                                {
                                    rotations[j] = Matrix.CreateTranslation(-partCenter) * rotations[j] * Matrix.CreateTranslation(partCenter);

                                    Matrix.AlignRotationToAxes(ref rotations[j], ref partMatrix);
                                }
                            }

                            for (int j = 0; j < rotCenters.Length; j++)
                            {
                                if (rotCenters[j] != Matrix.Zero && rotCenterNames != null)
                                {
                                    var dummyMatrix = GetPartDummy(rotCenterNames[j], part.Model, I.DummyList)?.Matrix ?? Matrix.Identity;
                                    rotCenters[j] = Matrix.CreateTranslation(-(partCenter + dummyMatrix.Translation)) * rotCenters[j] * Matrix.CreateTranslation((partCenter + dummyMatrix.Translation));


                                    Matrix.AlignRotationToAxes(ref rotCenters[j], ref dummyMatrix);
                                }
                            }
                        }

                        allAnimationSet[animationSet.Key][i] = new PartAnimation(animation.EventTrigger, animation.AnimationId, rotations, rotCenters,
                            animation.TypeSet, animation.EmissiveIds, animation.CurrentEmissivePart, animation.MoveToSetIndexer, animation.SubpartId, part, parts.Entity,
                            animation.Muzzle, animation.MotionDelay, system, animation.DoesLoop,
                            animation.DoesReverse, animation.TriggerOnce, animation.ResetEmissives);
                    }
                }

                system.WeaponAnimationSet.Clear();

                foreach (var animationKv in allAnimationSet)
                {
                    system.WeaponAnimationSet[animationKv.Key] = new PartAnimation[animationKv.Value.Length];
                    animationKv.Value.CopyTo(system.WeaponAnimationSet[animationKv.Key], 0);
                }

                system.AnimationsInited = true;
                return allAnimationSet;
            }

            var returnAnimations = new Dictionary<EventTriggers, PartAnimation[]>();
            foreach (var animationKv in system.WeaponAnimationSet)
            {
                returnAnimations[animationKv.Key] = new PartAnimation[animationKv.Value.Length];
                for (int i = 0; i < animationKv.Value.Length; i++)
                {
                    var animation = animationKv.Value[i];
                    MyEntity part;
                    parts.NameToEntity.TryGetValue(animation.SubpartId, out part);

                    if (part == null) continue;
                    returnAnimations[animationKv.Key][i] = new PartAnimation(animation)
                    {
                        Part = part,
                        MainEnt = parts.Entity,
                    };
                }
            }
            //Log.Line("Copying Animations");
            return returnAnimations;
        }

        internal static Dictionary<EventTriggers, ParticleEvent[]> CreateWeaponParticleEvents(WeaponSystem system, RecursiveSubparts parts)
        {
            var particles = new Dictionary<EventTriggers, ParticleEvent[]>();

            foreach (var particleDef in system.ParticleEvents)
            {
                var particleEvents = particleDef.Value;
                particles[particleDef.Key] = new ParticleEvent[particleEvents.Length];

                for (int i = 0; i < particles[particleDef.Key].Length; i++)
                {
                    var systemParticle = particleEvents[i];

                    Dummy particleDummy;
                    string partName;
                    if (CreateParticleDummy(parts.Entity, systemParticle.EmptyNames, out particleDummy, out partName))
                    {
                        Vector3 pos = GetPartLocation(systemParticle.EmptyNames, particleDummy.Entity.Model, I.DummyList);
                        particles[particleDef.Key][i] = new ParticleEvent(systemParticle, particleDummy, partName, pos);
                    }
                }
            }
            return particles;
        }

        internal readonly Dictionary<string, IMyModelDummy> DummyList = new Dictionary<string, IMyModelDummy>();
        internal static Vector3 GetPartLocation(string partName, IMyModel model, Dictionary<string, IMyModelDummy> dummyList)
        {
            dummyList.Clear();
            model.GetDummies(dummyList);

            IMyModelDummy dummy;
            if (dummyList.TryGetValue(partName, out dummy))
                return dummy.Matrix.Translation;

            return Vector3.Zero;
        }

        internal static IMyModelDummy GetPartDummy(string partName, IMyModel model, Dictionary<string, IMyModelDummy> dummyList)
        {
            dummyList.Clear();
            model.GetDummies(dummyList);

            IMyModelDummy dummy;
            if (dummyList.TryGetValue(partName, out dummy))
                return dummy;

            return null;
        }

        internal void ProcessParticles()
        {
            try
            {
                for (int i = Av.ParticlesToProcess.Count - 1; i >= 0; i--)
                {
                    var particleEvent = Av.ParticlesToProcess[i];
                    var playedFull = Tick - particleEvent.PlayTick > particleEvent.MaxPlayTime;
                    if (particleEvent.MyDummy.NullEntity)
                    { 
                        if (particleEvent.Effect != null)
                        {
                            particleEvent.Effect.Stop();
                            MyParticlesManager.RemoveParticleEffect(particleEvent.Effect);
                        }

                        particleEvent.Effect = null;
                        particleEvent.Playing = false;
                        particleEvent.Stop = false;
                        Av.ParticlesToProcess.RemoveAtFast(i);
                        continue;
                    }

                    var obb = particleEvent.MyDummy.Entity.PositionComp.WorldAABB;
                    var playable = Vector3D.DistanceSquared(CameraPos, obb.Center) <= particleEvent.Distance;
                    if (particleEvent.PlayTick <= Tick && !playedFull && !particleEvent.Stop && playable)
                    {
                        var dummyInfo = particleEvent.MyDummy.Info;
                        var ent = particleEvent.MyDummy.Entity;
                        var pos = dummyInfo.Position;
                        var matrix = dummyInfo.DummyMatrix;
                        matrix.Translation = dummyInfo.LocalPosition + particleEvent.Offset;
                        var renderId = ent.Render.GetRenderObjectID();

                        if (particleEvent.Effect == null)
                        {
                            if (ent == null || !MyParticlesManager.TryCreateParticleEffect(particleEvent.ParticleName,
                                    ref matrix, ref pos, renderId, out particleEvent.Effect))
                            {
                                Log.Line($"Failed to Create Particle! Particle: {particleEvent.ParticleName}");
                                particleEvent.Playing = false;
                                Av.ParticlesToProcess.RemoveAtFast(i);
                                continue;
                            }

                            particleEvent.Effect.WorldMatrix = matrix;
                            particleEvent.Effect.UserScale = particleEvent.Scale;

                        }
                        else if (particleEvent.Effect.IsStopped)
                        {
                            particleEvent.Effect.StopEmitting();
                            particleEvent.Effect.Play();
                        }
                    }
                    else if (playedFull && particleEvent.DoesLoop && !particleEvent.Stop && playable)
                    {
                        particleEvent.PlayTick = Tick + particleEvent.LoopDelay;

                        if (particleEvent.LoopDelay > 0 && particleEvent.Effect != null &&
                            !particleEvent.Effect.IsStopped && particleEvent.ForceStop)
                        {
                            particleEvent.Effect.Stop();
                            particleEvent.Effect.StopEmitting();
                        }
                    }
                    else if (playedFull || particleEvent.Stop)
                    {

                        if (particleEvent.Effect != null)
                        {
                            particleEvent.Effect.Stop();
                            MyParticlesManager.RemoveParticleEffect(particleEvent.Effect);
                        }

                        particleEvent.Effect = null;
                        particleEvent.Playing = false;
                        particleEvent.Stop = false;
                        Av.ParticlesToProcess.RemoveAtFast(i);
                    }
                    else if (!playable && particleEvent.Effect != null && !particleEvent.Effect.IsStopped)
                    {
                        particleEvent.Effect.Stop();
                        particleEvent.Effect.StopEmitting();
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Line($"Exception in ProcessParticles: {ex}", null, true);
                Av.ParticlesToProcess.Clear();
            }
        }

        private static void CompileHeating(AnimationDef animations, Dictionary<string, PartEmissive> emissiveLookup, out string[] heatingSubpartNames)
        {
            if (animations.HeatingEmissiveParts != null && animations.HeatingEmissiveParts.Length > 0)
                heatingSubpartNames = animations.HeatingEmissiveParts;
            else
                heatingSubpartNames = Array.Empty<string>();

            var wepEmissivesSet = animations.Emissives;
            if (wepEmissivesSet != null) {
                foreach (var emissive in wepEmissivesSet)
                    emissiveLookup.Add(emissive.EmissiveName, emissive);
            }
        }
        
        private static void CompileParticles(AnimationDef animations, Dictionary<EventTriggers, ParticleEvent[]> particleEvents)
        {
            if (animations.EventParticles != null) {
                
                var tmpEvents = new Dictionary<EventTriggers, List<ParticleEvent>>();

                foreach (var particleEvent in animations.EventParticles) {
                    
                    tmpEvents[particleEvent.Key] = new List<ParticleEvent>();

                    for (int i = 0; i<particleEvent.Value.Length; i++) {
                        
                        var eventParticle = particleEvent.Value[i];

                        if (eventParticle.MuzzleNames == null)
                            eventParticle.MuzzleNames = Array.Empty<string>();

                        if (eventParticle.EmptyNames.Length == eventParticle.MuzzleNames.Length) {
                            
                            for (int j = 0; j<eventParticle.EmptyNames.Length; j++)
                                tmpEvents[particleEvent.Key].Add(new ParticleEvent(eventParticle.Particle.Name, eventParticle.EmptyNames[j], eventParticle.Particle.Color, eventParticle.Particle.Offset, eventParticle.Particle.Extras.Scale, (eventParticle.Particle.Extras.MaxDistance* eventParticle.Particle.Extras.MaxDistance), (uint) eventParticle.Particle.Extras.MaxDuration, eventParticle.StartDelay, eventParticle.LoopDelay, eventParticle.Particle.Extras.Loop, eventParticle.Particle.Extras.Restart, eventParticle.ForceStop, eventParticle.MuzzleNames[j]));
                        }
                        else {
                            
                            for (int j = 0; j<eventParticle.EmptyNames.Length; j++)
                                tmpEvents[particleEvent.Key].Add(new ParticleEvent(eventParticle.Particle.Name, eventParticle.EmptyNames[j], eventParticle.Particle.Color, eventParticle.Particle.Offset, eventParticle.Particle.Extras.Scale, (eventParticle.Particle.Extras.MaxDistance* eventParticle.Particle.Extras.MaxDistance), (uint) eventParticle.Particle.Extras.MaxDuration, eventParticle.StartDelay, eventParticle.LoopDelay, eventParticle.Particle.Extras.Loop, eventParticle.Particle.Extras.Restart, eventParticle.ForceStop, eventParticle.MuzzleNames));
                        }
                    }                    
                }

                foreach (var particleEvent in tmpEvents)
                    particleEvents[particleEvent.Key] = particleEvent.Value.ToArray();
            }  
        }

        private static void CompileAnimationSets(CoreSystem system, PartAnimationSetDef[] wepAnimationSets, Dictionary<EventTriggers, HashSet<PartAnimation>> allAnimationSet, Dictionary<EventTriggers, uint> animationLengths, HashSet<string> animationIdLookup, Dictionary<string, EmissiveState> weaponEmissivesSet, Dictionary<string, PartEmissive> emissiveLookup, Dictionary<string, Matrix[]> weaponLinearMoveSet)
        {
            
            foreach (var animationSet in wepAnimationSets) {
                for (int t = 0; t < animationSet.SubpartId.Length; t++) {

                    foreach (var moves in animationSet.EventMoveSets) {
                        
                        if (!allAnimationSet.ContainsKey(moves.Key)) {
                            allAnimationSet[moves.Key] = new HashSet<PartAnimation>();
                            animationLengths[moves.Key] = 0;
                        }

                        Guid guid = Guid.NewGuid();
                        var id = Convert.ToBase64String(guid.ToByteArray());
                        animationIdLookup.Add(id);

                        List<Matrix> moveSet;
                        List<Matrix> rotationSet;
                        List<Matrix> rotCenterSet;
                        List<string> rotCenterNameSet;
                        List<string> emissiveIdSet;
                        List<int[]> moveIndexer;
                        List<int> currentEmissivePart;
                        
                        CompileAnimationMoves(moves, emissiveLookup, animationSet, weaponEmissivesSet, animationLengths, id, out moveSet, out rotationSet, out rotCenterSet, out rotCenterNameSet, out emissiveIdSet, out moveIndexer, out currentEmissivePart);


                        var typeSet = new[]
{
                            AnimationType.Movement,
                            AnimationType.ShowInstant,
                            AnimationType.HideInstant,
                            AnimationType.ShowFade,
                            AnimationType.HideFade,
                            AnimationType.Delay,
                            AnimationType.EmissiveOnly
                        };

                        var loop = animationSet.Loop != null && animationSet.Loop.Contains(moves.Key);
                        var reverse = animationSet.Reverse != null && animationSet.Reverse.Contains(moves.Key);
                        var triggerOnce = animationSet.TriggerOnce != null && animationSet.TriggerOnce.Contains(moves.Key);
                        var resetEmissives = animationSet.ResetEmissives != null && animationSet.ResetEmissives.Contains(moves.Key);
                        
                        var partAnim = new PartAnimation(moves.Key, id, rotationSet.ToArray(), rotCenterSet.ToArray(), typeSet, emissiveIdSet.ToArray(), currentEmissivePart.ToArray(), moveIndexer.ToArray(), animationSet.SubpartId[t], null, null, animationSet.BarrelId, animationSet.AnimationDelays[moves.Key], system, loop, reverse, triggerOnce, resetEmissives);

                        weaponLinearMoveSet.Add(id, moveSet.ToArray());

                        partAnim.RotCenterNameSet = rotCenterNameSet.ToArray();
                        allAnimationSet[moves.Key].Add(partAnim);
                    }
                }
            }
        }
        
        private static void CompileAnimationMoves(KeyValuePair<EventTriggers, RelMove[]> moves, Dictionary<string, PartEmissive> emissiveLookup, PartAnimationSetDef animationSet, Dictionary<string, EmissiveState> weaponEmissivesSet, Dictionary<EventTriggers, uint> animationLengths, string id, out List<Matrix> moveSet, out List<Matrix> rotationSet, out List<Matrix> rotCenterSet, out List<string> rotCenterNameSet, out List<string> emissiveIdSet, out List<int[]> moveIndexer, out List<int> currentEmissivePart)
        {
            moveSet = new List<Matrix>();
            rotationSet = new List<Matrix>();
            rotCenterSet = new List<Matrix>();
            rotCenterNameSet = new List<string>();
            emissiveIdSet = new List<string>();
            moveIndexer = new List<int[]>();
            currentEmissivePart = new List<int>();
            
            var totalPlayLength = animationSet.AnimationDelays[moves.Key];

            for (int i = 0; i < moves.Value.Length; i++) {
                
                var move = moves.Value[i];
                totalPlayLength += move.TicksToMove;
                var hasEmissive = !string.IsNullOrEmpty(move.EmissiveName);

                if (move.MovementType == RelMove.MoveType.Delay || move.MovementType == RelMove.MoveType.Show || move.MovementType == RelMove.MoveType.Hide) 
                    Absolute(move, emissiveLookup, weaponEmissivesSet, rotCenterSet, id, moveSet, rotationSet, emissiveIdSet, moveIndexer, currentEmissivePart, hasEmissive);
                else {
                    
                    var type = 6;
                    Vector3D rotChanged = Vector3D.Zero;
                    Vector3D rotCenterChanged = Vector3D.Zero;

                    if (move.LinearPoints != null && move.LinearPoints.Length > 0) {

                        double[][] tmpDirVec;
                        double distance;
                        ComputeLinearPoints(move, out tmpDirVec, out distance);
                        
                        switch (move.MovementType) {

                            case RelMove.MoveType.ExpoDecay:
                            case RelMove.MoveType.ExpoGrowth:
                                Expo(move, tmpDirVec, emissiveLookup, weaponEmissivesSet, id, moveSet, rotationSet, rotCenterSet, rotCenterNameSet, emissiveIdSet, moveIndexer, currentEmissivePart, hasEmissive, distance, ref type, ref rotCenterChanged, ref rotChanged);
                                break;
                            case RelMove.MoveType.Linear:
                                Linear(move, tmpDirVec, emissiveLookup, weaponEmissivesSet, id, moveSet, rotationSet, rotCenterSet, rotCenterNameSet, emissiveIdSet, moveIndexer, currentEmissivePart, hasEmissive, distance, ref type, ref rotCenterChanged, ref rotChanged);
                                break;
                        }
                    }
                    else 
                        NonLinear(move,  emissiveLookup, weaponEmissivesSet, id, moveSet, rotationSet, rotCenterSet, rotCenterNameSet, emissiveIdSet, moveIndexer, currentEmissivePart, hasEmissive, ref type, ref rotCenterChanged, ref rotChanged);
                }

            }

            if (animationLengths[moves.Key] < totalPlayLength)
                animationLengths[moves.Key] = totalPlayLength;
        }

        private static void Expo(RelMove move, double[][] tmpDirVec, Dictionary<string, PartEmissive> emissiveLookup, Dictionary<string, EmissiveState> weaponEmissivesSet, string id, List<Matrix> moveSet, List<Matrix> rotationSet, List<Matrix> rotCenterSet, List<string> rotCenterNameSet, List<string> emissiveIdSet, List<int[]> moveIndexer, List<int> currentEmissivePart, bool hasEmissive, double distance, ref int type, ref Vector3D rotCenterChanged, ref Vector3D rotChanged)
        {
            var traveled = 0d;
            var rate = 0d;
            var decay = move.MovementType == RelMove.MoveType.ExpoDecay;
            var check = decay ? 1d : 0d;
            
            while (decay ? check > 0 : check < distance) {
                rate += 0.001;
                check = decay ? distance * Math.Pow(1 - rate, move.TicksToMove) : 0.001 * Math.Pow(1 + rate, move.TicksToMove);

                if (decay && check < 0.001) 
                    check = 0;
            }

            var vectorCount = 0;
            var remaining = 0d;
            var vecTotalMoved = 0d;
            rate = decay ? 1 - rate : rate + 1;

            for (int j = 0; j < move.TicksToMove; j++) {

                var toPow = Math.Pow(rate, j + 1);
                var step = decay ? distance * toPow : 0.001 * toPow;
                var reset = decay ? step < 0.001 : step > distance;
                if (reset) step = decay ? 0 : distance;

                var lastTraveled = traveled;
                traveled = decay ? distance - step : step;
                var changed = traveled - lastTraveled;

                float progress;
                if (move.TicksToMove == 1 || j == move.TicksToMove - 1)
                    progress = 1;
                else
                    progress = (float)(traveled / distance);

                changed += remaining;
                if (changed > tmpDirVec[vectorCount][0] - vecTotalMoved) {

                    var origMove = changed;
                    changed = changed - (tmpDirVec[vectorCount][0] - vecTotalMoved);
                    remaining = origMove - changed;
                    vecTotalMoved = 0;
                }
                else {
                    vecTotalMoved += changed;
                    remaining = 0;
                }


                var vector = new Vector3(tmpDirVec[vectorCount][1] * changed, tmpDirVec[vectorCount][2] * changed, tmpDirVec[vectorCount][3] * changed);
                var matrix = Matrix.CreateTranslation(vector);

                moveSet.Add(matrix);

                PartEmissive emissive;
                if (hasEmissive && emissiveLookup.TryGetValue(move.EmissiveName, out emissive))
                    CreateEmissiveStep(emissive, id + moveIndexer.Count, progress, ref weaponEmissivesSet, ref currentEmissivePart);
                else {
                    weaponEmissivesSet[id + moveIndexer.Count] = new EmissiveState();
                    currentEmissivePart.Add(-1);
                }

                emissiveIdSet.Add(id + moveIndexer.Count);

                CreateRotationSets(move, progress, ref type, ref rotCenterNameSet, ref rotCenterSet, ref rotationSet, ref rotCenterChanged, ref rotChanged);

                moveIndexer.Add(new[] { moveSet.Count - 1, rotationSet.Count - 1, rotCenterSet.Count - 1, 0, emissiveIdSet.Count - 1, currentEmissivePart.Count - 1 });

                if (remaining > 0)
                    vectorCount++;

            }
        }

        private static void Absolute(RelMove move, Dictionary<string, PartEmissive> emissiveLookup, Dictionary<string, EmissiveState> weaponEmissivesSet, List<Matrix> rotCenterSet, string id, List<Matrix> moveSet, List<Matrix> rotationSet, List<string> emissiveIdSet, List<int[]> moveIndexer, List<int> currentEmissivePart, bool hasEmissive)
        {
            moveSet.Add(Matrix.Zero);
            rotationSet.Add(Matrix.Zero);
            rotCenterSet.Add(Matrix.Zero);
            for (var j = 0; j < move.TicksToMove; j++) {

                var type = 5;
                switch (move.MovementType) {

                    case RelMove.MoveType.Delay:
                        break;

                    case RelMove.MoveType.Show:
                        type = move.Fade ? 3 : 1;
                        break;

                    case RelMove.MoveType.Hide:
                        type = move.Fade ? 4 : 2;
                        break;
                }

                PartEmissive emissive;
                if (hasEmissive && emissiveLookup.TryGetValue(move.EmissiveName, out emissive)) {

                    float progress;
                    if (move.TicksToMove == 1)
                        progress = 1;
                    else
                        progress = (float)j / (move.TicksToMove - 1);

                    CreateEmissiveStep(emissive, id + moveIndexer.Count, progress, ref weaponEmissivesSet, ref currentEmissivePart);
                }
                else {
                    weaponEmissivesSet[id + moveIndexer.Count] = new EmissiveState();
                    currentEmissivePart.Add(-1);
                }

                emissiveIdSet.Add(id + moveIndexer.Count);

                moveIndexer.Add(new[] { moveSet.Count - 1, rotationSet.Count - 1, rotCenterSet.Count - 1, type, emissiveIdSet.Count - 1, currentEmissivePart.Count - 1 });
            }
        }
        
        private static void NonLinear(RelMove move, Dictionary<string, PartEmissive> emissiveLookup, Dictionary<string, EmissiveState> weaponEmissivesSet, string id, List<Matrix> moveSet, List<Matrix> rotationSet, List<Matrix> rotCenterSet, List<string> rotCenterNameSet, List<string> emissiveIdSet, List<int[]> moveIndexer, List<int> currentEmissivePart, bool hasEmissive, ref int type, ref Vector3D rotCenterChanged, ref Vector3D rotChanged)
        {
            moveSet.Add(Matrix.Zero);
            MatrixD rotation = MatrixD.Zero;
            MatrixD centerRotation = MatrixD.Zero;

            var hasX = !MyUtils.IsZero(move.Rotation.x, 1E-04f);
            var hasY = !MyUtils.IsZero(move.Rotation.y, 1E-04f);
            var hasZ = !MyUtils.IsZero(move.Rotation.z, 1E-04f);

            if (hasX)
                rotation = MatrixD.CreateRotationX(MathHelperD.ToRadians(move.Rotation.x));

            if (hasY) {

                if (hasX)
                    rotation *= MatrixD.CreateRotationY(MathHelperD.ToRadians(move.Rotation.y));
                else
                    rotation = MatrixD.CreateRotationY(MathHelperD.ToRadians(move.Rotation.y));
            }

            if (hasZ) {

                if (hasX || hasY)
                    rotation *= MatrixD.CreateRotationY(MathHelperD.ToRadians(move.Rotation.z));
                else
                    rotation = MatrixD.CreateRotationY(MathHelperD.ToRadians(move.Rotation.z));
            }

            hasX = !MyUtils.IsZero(move.RotAroundCenter.x, 1E-04f);
            hasY = !MyUtils.IsZero(move.RotAroundCenter.y, 1E-04f);
            hasZ = !MyUtils.IsZero(move.RotAroundCenter.z, 1E-04f);

            if (hasX)
                centerRotation = MatrixD.CreateRotationX(MathHelperD.ToRadians(move.RotAroundCenter.x));

            if (hasY) {

                if (hasX)
                    centerRotation *= MatrixD.CreateRotationY(MathHelperD.ToRadians(move.RotAroundCenter.y));
                else
                    centerRotation = MatrixD.CreateRotationY(MathHelperD.ToRadians(move.RotAroundCenter.y));
            }

            if (hasZ) {

                if (hasX || hasY)
                    centerRotation *= MatrixD.CreateRotationY(MathHelperD.ToRadians(move.RotAroundCenter.z));
                else
                    centerRotation = MatrixD.CreateRotationY(MathHelperD.ToRadians(move.RotAroundCenter.z));
            }

            var angle = Math.Round(MathHelperD.ToDegrees(Math.Acos(((rotation.Rotation.M11 + rotation.Rotation.M22 + rotation.Rotation.M33) - 1) / 2)), 2);
            var centerAngle = Math.Round(MathHelperD.ToDegrees(Math.Acos(((centerRotation.Rotation.M11 + centerRotation.Rotation.M22 + centerRotation.Rotation.M33) - 1) / 2)), 2);

            var rateAngle = centerAngle > angle ? centerAngle : angle;

            var rate = GetRate(move.MovementType, rateAngle, move.TicksToMove);

            for (int j = 0; j < move.TicksToMove; j++) {

                double progress;
                double traveled;
                if (move.MovementType == RelMove.MoveType.ExpoGrowth) { // This if does nothing, because progress is overwritten

                    var step = 0.001 * Math.Pow(rate, j + 1);
                    if (step > angle) step = angle;
                    traveled = step;

                    if (move.TicksToMove == 1 || j == move.TicksToMove - 1)
                        progress = 1;
                    else
                        progress = (traveled / angle);
                }
                else  if (move.MovementType == RelMove.MoveType.ExpoDecay) {

                    var step = angle * Math.Pow(rate, j + 1);
                    if (step < 0.001) step = 0;

                    traveled = angle - step;

                    if (move.TicksToMove == 1 || j == move.TicksToMove - 1)
                        progress = 1;
                    else
                        progress = traveled / angle;
                }
                else
                    progress = (double)j / (move.TicksToMove - 1);

                if (move.TicksToMove == 1 || j == move.TicksToMove - 1)
                    progress = 1;

                PartEmissive emissive;
                if (hasEmissive && emissiveLookup.TryGetValue(move.EmissiveName, out emissive))
                    CreateEmissiveStep(emissive, id + moveIndexer.Count, (float)progress, ref weaponEmissivesSet, ref currentEmissivePart);
                else
                {
                    weaponEmissivesSet[id + moveIndexer.Count] = new EmissiveState();
                    currentEmissivePart.Add(-1);
                }

                emissiveIdSet.Add(id + moveIndexer.Count);

                CreateRotationSets(move, progress, ref type, ref rotCenterNameSet, ref rotCenterSet, ref rotationSet, ref rotCenterChanged, ref rotChanged);

                moveIndexer.Add(new[] { moveSet.Count - 1, rotationSet.Count - 1, rotCenterSet.Count - 1, type, emissiveIdSet.Count - 1, currentEmissivePart.Count - 1 });
            }
        }
        
        private static void Linear(RelMove move, double[][] tmpDirVec, Dictionary<string, PartEmissive> emissiveLookup, Dictionary<string, EmissiveState> weaponEmissivesSet, string id, List<Matrix> moveSet, List<Matrix> rotationSet, List<Matrix> rotCenterSet, List<string> rotCenterNameSet, List<string> emissiveIdSet, List<int[]> moveIndexer, List<int> currentEmissivePart, bool hasEmissive, double distance, ref int type, ref Vector3D rotCenterChanged, ref Vector3D rotChanged)
        {
            var distancePerTick = distance / move.TicksToMove;
            var vectorCount = 0;
            var remaining = 0d;
            var vecTotalMoved = 0d;
            var totalChanged = 0d;

            for (int j = 0; j < move.TicksToMove; j++) {
                
                var changed = distancePerTick + remaining;
                if (changed > tmpDirVec[vectorCount][0] - vecTotalMoved) {
                    
                    var origMove = changed;
                    changed = changed - (tmpDirVec[vectorCount][0] - vecTotalMoved);
                    remaining = origMove - changed;
                    vecTotalMoved = 0;
                }
                else {
                    vecTotalMoved += changed;
                    remaining = 0;
                }

                if (j == move.TicksToMove - 1) {
                    if (totalChanged + changed != distance)
                        changed += (distance - (totalChanged + changed));
                }

                totalChanged += changed;

                var vector = new Vector3(tmpDirVec[vectorCount][1] * changed, tmpDirVec[vectorCount][2] * changed, tmpDirVec[vectorCount][3] * changed);

                var matrix = Matrix.CreateTranslation(vector);

                moveSet.Add(matrix);

                float progress;
                if (move.TicksToMove == 1)
                    progress = 1;
                else
                    progress = (float)j / (move.TicksToMove - 1);

                PartEmissive emissive;
                if (hasEmissive && emissiveLookup.TryGetValue(move.EmissiveName, out emissive))
                    CreateEmissiveStep(emissive, id + moveIndexer.Count, progress, ref weaponEmissivesSet, ref currentEmissivePart);
                else {
                    weaponEmissivesSet[id + moveIndexer.Count] = new EmissiveState();
                    currentEmissivePart.Add(-1);
                }

                emissiveIdSet.Add(id + moveIndexer.Count);

                CreateRotationSets(move, progress, ref type, ref rotCenterNameSet, ref rotCenterSet, ref rotationSet, ref rotCenterChanged, ref rotChanged);

                moveIndexer.Add(new[] {moveSet.Count - 1, rotationSet.Count - 1, rotCenterSet.Count - 1, 0, emissiveIdSet.Count - 1, currentEmissivePart.Count - 1});

                if (remaining > 0)
                    vectorCount++;
            }
        }

        private static void FinalizeAnimationSets(Dictionary<EventTriggers, HashSet<PartAnimation>> allAnimationSet, Dictionary<EventTriggers, PartAnimation[]> weaponAnimationSets)
        {
            foreach (var animationsKv in allAnimationSet)
            {
                weaponAnimationSets[animationsKv.Key] = new PartAnimation[animationsKv.Value.Count];
                animationsKv.Value.CopyTo(weaponAnimationSets[animationsKv.Key], 0);
            }
        }

        private static void ComputeLinearPoints(RelMove move, out double[][] tmpDirVec, out double distance)
        {
            distance = 0;
            tmpDirVec = new double[move.LinearPoints.Length][];

            for (int j = 0; j < move.LinearPoints.Length; j++) {

                var point = move.LinearPoints[j];

                var d = Math.Sqrt((point.x * point.x) + (point.y * point.y) + (point.z * point.z));

                distance += d;

                var dv = new[] { d, point.x / d, point.y / d, point.z / d };

                tmpDirVec[j] = dv;
            }
        }

        private static double GetRate(RelMove.MoveType move, double fullRotAmount, uint ticksToMove)
        {
            var rate = 0d;
            if (move == RelMove.MoveType.ExpoGrowth)
            {
                var check = 0d;

                while (check < fullRotAmount)
                {
                    rate += 0.001;
                    check = 0.001 * Math.Pow(1 + rate, ticksToMove);
                }
                rate += 1;
            }
            else if (move == RelMove.MoveType.ExpoDecay)
            {
                var check = 1d;
                while (check > 0)
                {
                    rate += 0.001;
                    check = fullRotAmount * Math.Pow(1 - rate, ticksToMove);
                    if (check < 0.001) check = 0;
                }
                rate = 1 - rate;
            }

            return rate;
        }

        private static bool CreateParticleDummy(MyEntity cube, string emptyName, out Dummy particleDummy, out string partName)
        {
            var head = -1;
            var tmp = new Dictionary<string, IMyModelDummy>();
            var nameLookup = new Dictionary<MyEntity, string> { [cube] = "None" };
            var subparts = new List<MyEntity>();
            MyEntity dummyPart = null;
            particleDummy = null;

            while (head < subparts.Count)
            {
                var query = head == -1 ? cube : subparts[head];
                head++;
                if (query.Model == null)
                    continue;
                tmp.Clear();
                ((IMyEntity)query).Model.GetDummies(tmp);
                foreach (var kv in tmp)
                {
                    if (kv.Key.Equals(emptyName))
                    {
                        dummyPart = query;
                        break;
                    }

                    if (kv.Key.StartsWith("subpart_", StringComparison.Ordinal))
                    {
                        var name = kv.Key.Substring("subpart_".Length);
                        MyEntitySubpart res;
                        if (query.TryGetSubpart(name, out res))
                        {
                            subparts.Add(res);
                            nameLookup[res] = name;
                        }
                    }
                }
            }

            if (dummyPart != null)
            {
                particleDummy = new Dummy(dummyPart, null, emptyName);
                partName = nameLookup[dummyPart];
                return true;
            }

            partName = "";
            return false;
        }

        private static Matrix CreateRotation(double x, double y, double z)
        {

            var rotation = MatrixD.Zero;

            if (x > 0 || x < 0)
                rotation = MatrixD.CreateRotationX(MathHelperD.ToRadians(x));

            if (y > 0 || y < 0)
                if (x > 0 || x < 0)
                    rotation *= MatrixD.CreateRotationY(MathHelperD.ToRadians(y));
                else
                    rotation = MatrixD.CreateRotationY(MathHelperD.ToRadians(y));

            if (z > 0 || z < 0)
                if (x > 0 || x < 0 || y > 0 || y < 0)
                    rotation *= MatrixD.CreateRotationZ(MathHelperD.ToRadians(z));
                else
                    rotation = MatrixD.CreateRotationZ(MathHelperD.ToRadians(z));

            return rotation;
        }

        private static void CreateRotationSets(RelMove move, double progress, ref int type, ref List<string> rotCenterNameSet, ref List<Matrix> rotCenterSet, ref List<Matrix> rotationSet, ref Vector3D centerChanged, ref Vector3D changed)
        {
            type = 6;

            if (!string.IsNullOrEmpty(move.CenterEmpty) && (move.RotAroundCenter.x > 0 || move.RotAroundCenter.y > 0 ||
                                                            move.RotAroundCenter.z > 0 || move.RotAroundCenter.x < 0 ||
                                                            move.RotAroundCenter.y < 0 || move.RotAroundCenter.z < 0))
            {
                rotCenterNameSet.Add(move.CenterEmpty);                
                
                var newX = MathHelper.Lerp(0, move.RotAroundCenter.x, progress) - centerChanged.X;
                var newY = MathHelper.Lerp(0, move.RotAroundCenter.y, progress) - centerChanged.Y;
                var newZ = MathHelper.Lerp(0, move.RotAroundCenter.z, progress) - centerChanged.Z;

                centerChanged.X += newX;
                centerChanged.Y += newY;
                centerChanged.Z += newZ;

                rotCenterSet.Add(CreateRotation(newX, newY, newZ));

                type = 0;
            }
            else
            {
                rotCenterNameSet.Add(null);
                rotCenterSet.Add(Matrix.Zero);
            }

            if (move.Rotation.x > 0 || move.Rotation.y > 0 || move.Rotation.z > 0 ||
                move.Rotation.x < 0 || move.Rotation.y < 0 || move.Rotation.z < 0)
            {
                var newX = MathHelper.Lerp(0, move.Rotation.x, progress) - changed.X;
                var newY = MathHelper.Lerp(0, move.Rotation.y, progress) - changed.Y;
                var newZ = MathHelper.Lerp(0, move.Rotation.z, progress) - changed.Z;

                changed.X += newX;
                changed.Y += newY;
                changed.Z += newZ;

                rotationSet.Add(CreateRotation(newX, newY, newZ));

                type = 0;
            }
            else
                rotationSet.Add(Matrix.Zero);
        }

        private static void CreateEmissiveStep(PartEmissive emissive, string id, float progress, ref Dictionary<string, EmissiveState> allEmissivesSet, ref List<int> currentEmissivePart)
        {
            var setColor = (Color)emissive.Colors[0];
            if (emissive.Colors.Length > 1)
            {
                if (progress < 1)
                {
                    float scaledTime = progress * (emissive.Colors.Length - 1);
                    Color lastColor = emissive.Colors[(int)scaledTime];
                    Color nextColor = emissive.Colors[(int)(scaledTime + 1f)];
                    float scaledProgress = scaledTime * progress;
                    setColor = Color.Lerp(lastColor, nextColor, scaledProgress);
                }
                else
                    setColor = emissive.Colors[emissive.Colors.Length - 1];
            }

            var intensity = MathHelper.Lerp(emissive.IntensityRange[0],
                emissive.IntensityRange[1], progress);

            var currPart = (int)Math.Round(MathHelper.Lerp(0, emissive.EmissivePartNames.Length - 1, progress));

            allEmissivesSet.Add(id, new EmissiveState { CurrentColor = setColor, CurrentIntensity = intensity, EmissiveParts = emissive.EmissivePartNames, CycleParts = emissive.CycleEmissivesParts, LeavePreviousOn = emissive.LeavePreviousOn });
            currentEmissivePart.Add(currPart);
        }

        private static Color[] CreateHeatEmissive()
        {
            var colors = new[]
            {
                new Color(10, 0, 0, 150),
                new Color(30, 0, 0, 150),
                new Color(250, .01f, 0, 180),
                new Color(240, .02f, 0, 200f),
                new Color(240, .03f, 0, 210f),
                new Color(220, .04f, 0, 230f),
                new Color(210, .05f, .01f, 240f),
                new Color(210, .05f, .02f, 255f),
                new Color(210, .05f, .03f, 255f),
                new Color(210, .04f, .04f, 255f),
                new Color(210, .03f, .05f, 255f)
            };

            var setColors = new Color[68];

            for (int i = 0; i <= 67; i++)
            {
                var progress = (float)i / 67;

                if (progress < 1)
                {
                    float scaledTime = progress * (colors.Length - 1);
                    Color lastColor = colors[(int)scaledTime];
                    Color nextColor = colors[(int)(scaledTime + 1f)];
                    float scaledProgress = scaledTime * progress;
                    setColors[i] = Color.Lerp(lastColor, nextColor, scaledProgress);
                }
                else
                    setColors[i] = colors[colors.Length - 1];
            }

            return setColors;
        }

        private void ProcessAnimations()
        {
            for (int i = AnimationsToProcess.Count - 1; i >= 0; i--)
            {
                var animation = AnimationsToProcess[i];

                if (animation?.MainEnt != null && !animation.MainEnt.MarkedForClose && animation.Part != null)
                {
                    if (animation.StartTick > Tick || animation.PlayTicks[0] > Tick) continue;

                    if (animation.MovesPivotPos || animation.CanPlay)
                    {
                        var localMatrix = animation.Part.PositionComp.LocalMatrixRef;
                        Matrix rotation;
                        Matrix rotAroundCenter;
                        Vector3D translation;
                        AnimationType animationType;
                        EmissiveState currentEmissive;

                        animation.GetCurrentMove(out translation, out rotation, out rotAroundCenter, out animationType, out currentEmissive);

                        if (animation.Reverse)
                        {
                            if (animationType == AnimationType.Movement) localMatrix.Translation -= translation;

                            animation.Previous();
                            if (animation.Previous(false) == animation.NumberOfMoves - 1)
                                animation.Reverse = false;
                        }
                        else
                        {
                            if (animationType == AnimationType.Movement) localMatrix.Translation += translation;

                            animation.Next();
                            if (animation.DoesReverse && animation.Next(false) == 0)
                                animation.Reverse = true;
                        }

                        if (rotation != Matrix.Zero)
                            localMatrix *= animation.Reverse ? Matrix.Invert(rotation) : rotation;

                        if (rotAroundCenter != Matrix.Zero)
                            localMatrix *= animation.Reverse ? Matrix.Invert(rotAroundCenter) : rotAroundCenter;

                        if (animationType == AnimationType.Movement)
                        {
                            animation.Part.PositionComp.SetLocalMatrix(ref localMatrix);
                        }
                        else if (!DedicatedServer && (animationType == AnimationType.ShowInstant || animationType == AnimationType.ShowFade))
                        {
                            animation.Part.Render.FadeIn = animationType == AnimationType.ShowFade;
                            var matrix = animation.Part.PositionComp.LocalMatrixRef;

                            animation.Part.Render.AddRenderObjects();

                            animation.Part.PositionComp.SetLocalMatrix(ref matrix);
                        }
                        else if (!DedicatedServer && (animationType == AnimationType.HideInstant || animationType == AnimationType.HideFade))
                        {
                            animation.Part.Render.FadeOut = animationType == AnimationType.HideFade;
                            var matrix = animation.Part.PositionComp.LocalMatrixRef;
                            animation.Part.Render.RemoveRenderObjects();
                            animation.Part.PositionComp.SetLocalMatrix(ref matrix);
                        }


                        if (!DedicatedServer && currentEmissive.EmissiveParts != null && currentEmissive.EmissiveParts.Length > 0)
                        {
                            if (currentEmissive.CycleParts)
                            {
                                animation.Part.SetEmissiveParts(currentEmissive.EmissiveParts[currentEmissive.CurrentPart], currentEmissive.CurrentColor,
                                    currentEmissive.CurrentIntensity);
                                if (!currentEmissive.LeavePreviousOn)
                                {
                                    var prev = currentEmissive.CurrentPart - 1 >= 0 ? currentEmissive.CurrentPart - 1 : currentEmissive.EmissiveParts.Length - 1;
                                    animation.Part.SetEmissiveParts(currentEmissive.EmissiveParts[prev], Color.Transparent, 0);
                                }
                            }
                            else
                            {

                                for (int j = 0; j < currentEmissive.EmissiveParts.Length; j++)
                                    animation.Part.SetEmissiveParts(currentEmissive.EmissiveParts[j], currentEmissive.CurrentColor, currentEmissive.CurrentIntensity);
                            }
                        }
                    }
                    else
                    {
                        if (animation.Reverse)
                        {
                            animation.Previous();
                            if (animation.Previous(false) == animation.NumberOfMoves - 1)
                                animation.Reverse = false;
                        }
                        else
                        {
                            animation.Next();
                            if (animation.DoesReverse && animation.Next(false) == 0)
                                animation.Reverse = true;
                        }
                    }


                    if (!animation.Reverse && !animation.Looping && animation.CurrentMove == 0)
                    {
                        AnimationsToProcess.RemoveAtFast(i);
                        if (animation.PlayTicks.Count > 1) animation.PlayTicks.RemoveAt(0);
                        else animation.Running = false;

                        if (!DedicatedServer && animation.ResetEmissives && animation.EmissiveParts != null)
                        {
                            for (int j = 0; j < animation.EmissiveParts.Length; j++)
                            {
                                var emissivePart = animation.EmissiveParts[j];
                                animation.Part.SetEmissiveParts(emissivePart, Color.Transparent, 0);
                            }
                        }
                    }
                }
                else
                    AnimationsToProcess.RemoveAtFast(i);

            }
        }
    }
}

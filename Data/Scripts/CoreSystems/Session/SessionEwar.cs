using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.EwarDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.EwarDef.PushPullDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.EwarDef.EwarType;
using static CoreSystems.Projectiles.Projectiles;

namespace CoreSystems
{
    public partial class Session
    {
        private readonly Dictionary<MyCubeGrid, Dictionary<EwarType, GridEffect>> _gridEffects = new Dictionary<MyCubeGrid, Dictionary<EwarType, GridEffect>>(128);
        internal readonly MyConcurrentPool<Dictionary<EwarType, GridEffect>> GridEffectsPool = new MyConcurrentPool<Dictionary<EwarType, GridEffect>>(128, effect => effect.Clear());
        internal readonly MyConcurrentPool<GridEffect> GridEffectPool = new MyConcurrentPool<GridEffect>(128, effect => effect.Clean());
        internal readonly Dictionary<long, BlockState> EffectedCubes = new Dictionary<long, BlockState>();
        internal readonly Dictionary<long, EwarValues> CurrentClientEwaredCubes = new Dictionary<long, EwarValues>();
        internal readonly Dictionary<long, EwarValues> DirtyEwarData = new Dictionary<long, EwarValues>();
        internal readonly ConcurrentDictionary<long, BlockState> ActiveEwarCubes = new ConcurrentDictionary<long, BlockState>();
        private readonly Queue<long> _effectPurge = new Queue<long>();
        internal bool ClientEwarStale;

        private void ForceFields(HitEntity hitEnt, ProInfo info)
        {
            var depletable = info.AmmoDef.Ewar.Depletable;
            var healthPool = depletable && info.BaseHealthPool > 0 ? info.BaseHealthPool : float.MaxValue;
            if (healthPool <= 0) return;
            var aConst = info.AmmoDef.Const;
            if (hitEnt.Entity.Physics == null || !hitEnt.Entity.Physics.Enabled || hitEnt.Entity.Physics.IsStatic || !hitEnt.HitPos.HasValue)
                return;

            if (IsServer)
            {
                var massMulti = 1f;

                Ai.TargetInfo tInfo;
                if (info.Ai.Targets.TryGetValue(hitEnt.Entity, out tInfo) && tInfo.TargetAi?.ShieldBlock != null && SApi.IsFortified(tInfo.TargetAi.ShieldBlock))
                    massMulti = 5f;

                var forceDef = info.AmmoDef.Ewar.Force;

                Vector3D forceFrom = Vector3D.Zero;
                Vector3D forceTo = Vector3D.Zero;
                Vector3D forcePosition = Vector3D.Zero;
                Vector3D normHitDir;

                if (forceDef.ForceFrom == Force.ProjectileLastPosition) forceFrom = hitEnt.PruneSphere.Center;
                else if (forceDef.ForceFrom == Force.ProjectileOrigin) forceFrom = info.Origin;
                else if (forceDef.ForceFrom == Force.HitPosition) forceFrom = hitEnt.HitPos.Value;
                else if (forceDef.ForceFrom == Force.TargetCenter) forceFrom = hitEnt.Entity.PositionComp.WorldAABB.Center;
                else if (forceDef.ForceFrom == Force.TargetCenterOfMass) forceFrom = hitEnt.Entity.Physics.CenterOfMassWorld;

                if (forceDef.ForceTo == Force.ProjectileLastPosition) forceTo = hitEnt.PruneSphere.Center;
                else if (forceDef.ForceTo == Force.ProjectileOrigin) forceTo = info.Origin;
                else if (forceDef.ForceTo == Force.HitPosition) forceTo = hitEnt.HitPos.Value;
                else if (forceDef.ForceTo == Force.TargetCenter) forceTo = hitEnt.Entity.PositionComp.WorldAABB.Center;
                else if (forceDef.ForceTo == Force.TargetCenterOfMass) forceTo = hitEnt.Entity.Physics.CenterOfMassWorld;

                if (forceDef.Position == Force.ProjectileLastPosition) forcePosition = hitEnt.PruneSphere.Center;
                else if (forceDef.Position == Force.ProjectileOrigin) forcePosition = info.Origin;
                else if (forceDef.Position == Force.HitPosition) forcePosition = hitEnt.HitPos.Value;
                else if (forceDef.Position == Force.TargetCenter) forcePosition = hitEnt.Entity.PositionComp.WorldAABB.Center;
                else if (forceDef.Position == Force.TargetCenterOfMass) forcePosition = hitEnt.Entity.Physics.CenterOfMassWorld;

                var hitDir = forceTo - forceFrom;

                Vector3D.Normalize(ref hitDir, out normHitDir);

                double force;
                if (info.AmmoDef.Const.EwarType != Tractor)
                {
                    normHitDir = aConst.EwarType == Push ? normHitDir : -normHitDir;
                    force = aConst.EwarStrength;
                }
                else
                {
                    var distFromFocalPoint = forceDef.TractorRange - hitEnt.HitDist ?? info.ProjectileDisplacement;
                    var positive = distFromFocalPoint > 0;
                    normHitDir = positive ? normHitDir : -normHitDir;
                    force = positive ?MathHelper.Lerp(distFromFocalPoint, forceDef.TractorRange, aConst.EwarStrength) : MathHelper.Lerp(Math.Abs(distFromFocalPoint), forceDef.TractorRange, info.AmmoDef.Const.EwarStrength);
                }
                var massMod = !forceDef.DisableRelativeMass ? hitEnt.Entity.Physics.Mass : 1;
                
                hitEnt.Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, (Vector3)(normHitDir * (((force * massMod)) / massMulti)), forcePosition, Vector3.Zero);
                if (forceDef.ShooterFeelsForce && info.Ai?.GridEntity != null)
                {

                    if (forceDef.Position == Force.HitPosition) forcePosition = info.Origin;
                    else if (forceDef.Position == Force.TargetCenter) forcePosition = info.Ai.GridEntity.PositionComp.WorldAABB.Center;
                    else forcePosition = info.Ai.GridEntity.Physics.CenterOfMassWorld;

                    hitDir = forceFrom - forceTo;
                    Vector3D.Normalize(ref hitDir, out normHitDir);

                    if (aConst.EwarType != Tractor)
                        normHitDir = aConst.EwarType == Push ? normHitDir : -normHitDir;
                    else {
                        var distFromFocalPoint = forceDef.TractorRange - info.ProjectileDisplacement;
                        var positive = distFromFocalPoint > 0;
                        normHitDir = positive ? normHitDir : -normHitDir;

                    }

                    info.Ai.GridEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, (Vector3)(- normHitDir * ((force * massMod) * massMod)), forcePosition, Vector3.Zero);
                }
            }

            if (depletable)
                info.BaseHealthPool = healthPool <= info.BaseHealthPool ? info.BaseHealthPool - healthPool : 0;
        }

        private void UpdateField(HitEntity hitEnt, ProInfo info)
        {
            if (info.AmmoDef.Const.EwarType == Pull || info.AmmoDef.Const.EwarType == Push || info.AmmoDef.Const.EwarType == Tractor)
            {
                ForceFields(hitEnt, info);
                return;
            }

            var grid = hitEnt.Entity as MyCubeGrid;
            if (grid?.Physics == null || grid.MarkedForClose) return;
            var attackerId = info.Weapon.Comp.CoreEntity.EntityId;
            GetAndSortBlocksInSphere(info.AmmoDef, hitEnt.Info.Weapon.System, grid, hitEnt.PruneSphere, !hitEnt.DamageOverTime, hitEnt.Blocks);

            var depletable = info.AmmoDef.Ewar.Depletable;
            var healthPool = depletable && info.BaseHealthPool > 0 ? info.BaseHealthPool : double.MaxValue;
            ComputeEffects(grid, info.AmmoDef, info.AmmoDef.Const.EwarStrength, ref healthPool, attackerId, info.Weapon.System.WeaponIdHash, hitEnt.Blocks);
            
            if (depletable)
                info.BaseHealthPool = healthPool <= info.BaseHealthPool ? info.BaseHealthPool - (float)healthPool : 0f;

        }

        private void UpdateEffect(HitEntity hitEnt, ProInfo info)
        {
            if (info.AmmoDef.Const.EwarType == Pull || info.AmmoDef.Const.EwarType == Push || info.AmmoDef.Const.EwarType == Tractor)
            {
                ForceFields(hitEnt, info);
                return;
            }
            var grid = hitEnt.Entity as MyCubeGrid;
            if (grid == null || grid.MarkedForClose) return;

            if (IsServer)
            {

                Dictionary<EwarType, GridEffect> effects;
                var attackerId = info.Weapon.Comp.CoreEntity.EntityId;
                if (_gridEffects.TryGetValue(grid, out effects))
                {
                    GridEffect gridEffect;
                    if (effects.TryGetValue(info.AmmoDef.Ewar.Type, out gridEffect))
                    {
                        gridEffect.Damage += (float)info.AmmoDef.Const.EwarStrength;
                        gridEffect.Ai = info.Ai;
                        gridEffect.AttackerId = attackerId;
                        gridEffect.Hits++;
                        var hitPos = hitEnt.HitPos ?? info.ProHit.LastHit;
                        gridEffect.HitPos = (gridEffect.HitPos + hitPos) / 2;

                    }
                }
                else
                {

                    effects = GridEffectsPool.Get();
                    var gridEffect = GridEffectPool.Get();
                    gridEffect.System = info.Weapon.System;
                    gridEffect.Damage = (float)info.AmmoDef.Const.EwarStrength;
                    gridEffect.Ai = info.Ai;
                    gridEffect.AmmoDef = info.AmmoDef;
                    gridEffect.AttackerId = attackerId;
                    gridEffect.Hits++;
                    var hitPos = hitEnt.HitPos ?? info.ProHit.LastHit;

                    gridEffect.HitPos = hitPos;
                    effects.Add(info.AmmoDef.Ewar.Type, gridEffect);
                    _gridEffects.Add(grid, effects);
                }
            }

            info.BaseHealthPool = 0;
            info.BaseDamagePool = 0;
        }


        private void ComputeEffects(MyCubeGrid grid, AmmoDef ammoDef, double damagePool, ref double healthPool, long attackerId, int sysmteId, List<HitEntity.RootBlocks> blocks)
        {
            DeferredBlockDestroy dInfo = null;
            var largeGrid = grid.GridSizeEnum == MyCubeSize.Large;
            var gridBlockCount = grid.CubeBlocks.Count;
            var eWarInfo = ammoDef.Ewar;
            var duration = (uint)eWarInfo.Duration;
            var stack = eWarInfo.StackDuration;
            var maxStack = eWarInfo.MaxStacks;
            var nextTick = Tick + 1;
            var maxTick = stack ? (uint)(nextTick + (duration * maxStack)) : nextTick + duration;
            var fieldType = ammoDef.Ewar.Type;
            var sync = MpActive && (DedicatedServer || IsServer);
            foreach (var rootBlock in blocks)
            {
                var block = rootBlock.Block;
                var cubeBlock = block.FatBlock;
                if (damagePool <= 0 || healthPool <= 0) break;
                IMyFunctionalBlock funcBlock = null;
                if (fieldType != Dot)
                {
                    if (cubeBlock == null || cubeBlock is IMyConveyor || cubeBlock.MarkedForClose)
                        continue;

                    funcBlock = cubeBlock as IMyFunctionalBlock;
                    
                    var ewared = EffectedCubes.ContainsKey(cubeBlock.EntityId);

                    if (funcBlock == null || !cubeBlock.IsWorking && !ewared || ewared && !stack) continue;
                }

                var blockHp = block.Integrity - block.AccumulatedDamage;
                float damageScale = 1;
                var aConst = ammoDef.Const;
                if (aConst.DamageScaling)
                {
                    var d = ammoDef.DamageScales;
                    if (d.MaxIntegrity > 0 && blockHp > d.MaxIntegrity || blockHp <= 0) continue;

                    if (aConst.LargeGridDmgScale >= 0 && largeGrid) damageScale *= aConst.LargeGridDmgScale;
                    else if (aConst.SmallGridDmgScale >= 0 && !largeGrid) damageScale *= aConst.SmallGridDmgScale;

                    MyDefinitionBase blockDef = null;
                    if (aConst.ArmorScaling)
                    {
                        blockDef = block.BlockDefinition;
                        var isArmor = AllArmorBaseDefinitions.Contains(blockDef) || CustomArmorSubtypes.Contains(blockDef.Id.SubtypeId);
                        if (isArmor && d.Armor.Armor >= 0) damageScale *= d.Armor.Armor;
                        else if (!isArmor && d.Armor.NonArmor >= 0) damageScale *= d.Armor.NonArmor;

                        if (isArmor && (d.Armor.Light >= 0 || d.Armor.Heavy >= 0))
                        {
                            var isHeavy = HeavyArmorBaseDefinitions.Contains(blockDef) || CustomHeavyArmorSubtypes.Contains(blockDef.Id.SubtypeId);
                            if (isHeavy && d.Armor.Heavy >= 0) damageScale *= d.Armor.Heavy;
                            else if (!isHeavy && d.Armor.Light >= 0) damageScale *= d.Armor.Light;
                        }
                    }
                    if (aConst.CustomDamageScales)
                    {
                        if (blockDef == null) blockDef = block.BlockDefinition;
                        float modifier;
                        var found = ammoDef.Const.CustomBlockDefinitionBasesToScales.TryGetValue(blockDef, out modifier);

                        if (found) damageScale *= modifier;
                        else if (ammoDef.DamageScales.Custom.IgnoreAllOthers) continue;
                    }
                }

                var scaledDamage = damagePool * damageScale;
                healthPool -= 1;

                if (fieldType == Dot && IsServer)
                {
                    if (scaledDamage < blockHp || gridBlockCount < 1000)
                    {
                        block.DoDamage((float) scaledDamage, MyDamageType.Explosion, true, null, attackerId, 0, false);
                    }
                    else
                    {
                        if (dInfo == null && !DeferredDestroy.TryGetValue(grid, out dInfo))
                        {
                            dInfo = DefferedDestroyPool.Count > 0 ? DefferedDestroyPool.Pop() : new DeferredBlockDestroy();
                            DeferredDestroy[grid] = dInfo;
                        }

                        if (dInfo.DestroyBlocks.Count == 0)
                            dInfo.DestroyTick = Tick + 10;

                        dInfo.DestroyBlocks.Add(new BlockDestroyInfo { Block = block, AttackerId = attackerId, DamageType = MyDamageType.Explosion, ScaledDamage = (float) scaledDamage, DetonateAmmo = false });
                    }

                    //block.DoDamage((float) scaledDamage, MyDamageType.Explosion, sync, null, attackerId);
                    continue;
                }

                if (funcBlock != null)
                {
                    BlockState blockState;
                    var cubeId = cubeBlock.EntityId;
                    if (EffectedCubes.TryGetValue(cubeId, out blockState))
                    {
                        if (blockState.Health - scaledDamage > 0)
                        {
                            damagePool = 0;
                            blockState.Health -= (float)scaledDamage;
                            blockState.Endtick = Tick + (duration + 1);
                        }
                        else if (blockState.Endtick + (duration + 1) < maxTick)
                        {
                            damagePool -= (blockHp * damageScale);
                            blockState.Health = 0;
                            blockState.Endtick += (duration + 1);
                        }
                        else
                        {
                            if (blockState.Endtick != maxTick)
                            {
                                var size = (duration + 1);
                                var diff = blockState.Endtick + size;
                                var scaler = (maxTick - diff) + 1;
                                if (scaler > 0)
                                    damagePool -= ((blockHp * damageScale) / scaler);
                            }
                            blockState.Health = 0;
                            blockState.Endtick = maxTick;
                        }
                    }
                    else
                    {
                        blockState.FunctBlock = funcBlock;
                        var originState = blockState.FunctBlock.Enabled;
                        blockState.FirstTick = Tick + 1;
                        blockState.FirstState = originState;
                        blockState.NextTick = nextTick;
                        blockState.Endtick = Tick + (duration + 1);
                        blockState.Session = this;
                        blockState.AmmoDef = ammoDef;
                        blockState.SystemId = sysmteId;

                        if (scaledDamage <= blockHp)
                        {
                            damagePool = 0;
                            blockState.Health = (blockHp - (float)scaledDamage);
                        }
                        else
                        {
                            damagePool -= (blockHp * damageScale);
                            blockState.Health = 0;
                        }
                    }
                    EffectedCubes[cubeId] = blockState;
                }
                else
                {
                    if (scaledDamage <= blockHp)
                        damagePool = 0;
                    else
                        damagePool -= blockHp;
                }
            }

            if (!IsServer)
                EffectedCubes.Clear();
        }

        internal void GridEffects()
        {
            foreach (var ge in _gridEffects)
            {
                foreach (var v in ge.Value)
                {
                    GetCubesForEffect(v.Value.Ai, ge.Key, v.Value.HitPos, v.Key, _tmpEffectCubes);
                    var healthPool = v.Value.AmmoDef.Const.Health > 0 ? v.Value.AmmoDef.Const.Health : double.MaxValue;
                    ComputeEffects(ge.Key, v.Value.AmmoDef, v.Value.Damage * v.Value.Hits, ref healthPool, v.Value.AttackerId, v.Value.System.WeaponIdHash, _tmpEffectCubes);
                    _tmpEffectCubes.Clear();
                    GridEffectPool.Return(v.Value);
                }
                GridEffectsPool.Return(ge.Value);
            }
            _gridEffects.Clear();
        }

        internal void ApplyGridEffect()
        {
            var tick = Tick;
            foreach (var item in EffectedCubes)
            {
                var cubeid = item.Key;
                var blockInfo = item.Value;
                var functBlock = blockInfo.FunctBlock;
                var health = blockInfo.Health;
                 if (functBlock?.SlimBlock == null || functBlock.SlimBlock.IsDestroyed || blockInfo.FunctBlock == null || blockInfo.FunctBlock.MarkedForClose || blockInfo.FunctBlock.Closed || blockInfo.FunctBlock.CubeGrid.MarkedForClose || !blockInfo.FunctBlock.IsFunctional || !blockInfo.FunctBlock.InScene)
                { // keen is failing to check for null when they null out functional block types
                    _effectPurge.Enqueue(cubeid);
                    continue;
                }

                if (health <= 0)
                {

                    if (functBlock.IsWorking)
                    {

                        functBlock.Enabled = false;
                        functBlock.EnabledChanged += ForceDisable;

                        if (MpActive && IsServer)
                        {
                            var ewarData = EwarDataPool.Get();
                            ewarData.FiringBlockId = blockInfo.FiringBlockId;
                            ewarData.EwaredBlockId = cubeid;
                            ewarData.EndTick = blockInfo.Endtick - Tick;
                            ewarData.AmmoId = blockInfo.AmmoDef.Const.AmmoIdxPos;
                            ewarData.SystemId = blockInfo.SystemId;
                            DirtyEwarData.Add(cubeid, ewarData);
                            EwarNetDataDirty = true;
                        }

                        if (IsHost)
                        {
                            functBlock.RefreshCustomInfo();

                            if (blockInfo.AmmoDef.Ewar.Field.ShowParticle)
                                functBlock.SetDamageEffect(true);
                        }
                    }
                }

                if (IsHost && Tick60 && HandlesInput && LastTerminal == functBlock)
                    functBlock.RefreshCustomInfo();

                if (tick >= blockInfo.Endtick)
                {

                    functBlock.EnabledChanged -= ForceDisable;

                    if (IsHost)
                    {

                        functBlock.RefreshCustomInfo();

                        if (blockInfo.AmmoDef.Ewar.Field.ShowParticle)
                            functBlock.SetDamageEffect(false);
                    }

                    functBlock.Enabled = blockInfo.FirstState;

                    _effectPurge.Enqueue(cubeid);
                }

            }

            while (_effectPurge.Count != 0)
            {
                var queue = _effectPurge.Dequeue();

                if (MpActive && IsServer)
                {

                    EwarValues ewarValue;
                    if (DirtyEwarData.TryGetValue(queue, out ewarValue))
                        EwarDataPool.Return(ewarValue);

                    EwarNetDataDirty = true;
                }

                EffectedCubes.Remove(queue);
            }
        }

        internal void SyncClientEwarBlocks()
        {
            foreach (var ewarPair in CurrentClientEwaredCubes)
            {
                BlockState state;
                MyEntity ent;
                var entId = ewarPair.Key;
                if (MyEntities.TryGetEntityById(entId, out ent) && ValidEwarBlock(ent as IMyTerminalBlock))
                {

                    var cube = (IMyCubeBlock)ent;
                    var func = (IMyFunctionalBlock)cube;
                    func.RefreshCustomInfo();

                    if (!ActiveEwarCubes.ContainsKey(entId))
                    {

                        state = new BlockState { FunctBlock = func, FirstState = func.Enabled, Endtick = Tick + ewarPair.Value.EndTick, Session = this };
                        ActiveEwarCubes[entId] = state;
                        ActivateClientEwarState(ref state);
                    }
                }
                else if (ActiveEwarCubes.TryGetValue(entId, out state))
                {

                    DeactivateClientEwarState(ref state);
                    ActiveEwarCubes.Remove(entId);
                }

                ClientEwarStale = false;
            }

            foreach (var activeEwar in ActiveEwarCubes)
            {

                if (!CurrentClientEwaredCubes.ContainsKey(activeEwar.Key))
                {
                    var state = activeEwar.Value;
                    DeactivateClientEwarState(ref state);
                    ActiveEwarCubes.Remove(activeEwar.Key);
                }
            }
        }

        private static void ActivateClientEwarState(ref BlockState state)
        {
            var functBlock = state.FunctBlock;
            functBlock.Enabled = false;
            functBlock.EnabledChanged += ForceDisable;
            functBlock.RefreshCustomInfo();
            functBlock.SetDamageEffect(true);
        }

        private static void DeactivateClientEwarState(ref BlockState state)
        {
            state.FunctBlock.EnabledChanged -= ForceDisable;
            state.Endtick = 0;
            var valid = ValidEwarBlock(state.FunctBlock);

            if (valid)
            {
                state.FunctBlock.Enabled = state.FirstState;
                state.FunctBlock.RefreshCustomInfo();
            }

            if (valid)
            {
                state.FunctBlock.RefreshCustomInfo();
                state.FunctBlock.SetDamageEffect(false);
            }
        }

        private static void ForceDisable(IMyTerminalBlock myTerminalBlock)
        {
            var cube = (IMyCubeBlock)myTerminalBlock;
            if (cube == null || myTerminalBlock?.SlimBlock == null || myTerminalBlock.SlimBlock.IsDestroyed || cube.MarkedForClose || cube.Closed || cube.CubeGrid.MarkedForClose || !cube.IsFunctional || !cube.InScene) // keen is failing to check for null when they null out functional block types
                return;

            ((IMyFunctionalBlock)myTerminalBlock).Enabled = false;
        }

        private static bool ValidEwarBlock(IMyTerminalBlock myTerminalBlock)
        {
            return myTerminalBlock?.SlimBlock != null && !myTerminalBlock.SlimBlock.IsDestroyed && !myTerminalBlock.MarkedForClose && !myTerminalBlock.Closed && !myTerminalBlock.CubeGrid.MarkedForClose && myTerminalBlock.IsFunctional && myTerminalBlock.InScene;
        }

        private readonly List<HitEntity.RootBlocks> _tmpEffectCubes = new List<HitEntity.RootBlocks>();
        internal static void GetCubesForEffect(Ai ai, MyCubeGrid grid, Vector3D hitPos, EwarType effectType, List<HitEntity.RootBlocks> cubes)
        {
            var fats = QueryBlockCaches(ai, grid, effectType);
            if (fats == null) return;

            for (int i = 0; i < fats.Count; i++)
            {
                var block = (IMySlimBlock)fats[i].SlimBlock;
                cubes.Add(new HitEntity.RootBlocks {Block = block, QueryPos = block.Position});
            }

            cubes.Sort((a, b) =>
            {
                var aPos = grid.GridIntegerToWorld(a.Block.Position);
                var bPos = grid.GridIntegerToWorld(b.Block.Position);
                return Vector3D.DistanceSquared(aPos, hitPos).CompareTo(Vector3D.DistanceSquared(bPos, hitPos));
            });
        }

        private static ConcurrentCachingList<MyCubeBlock> QueryBlockCaches(Ai ai, MyCubeGrid targetGrid, EwarType effectType)
        {
            ConcurrentDictionary<TargetingDef.BlockTypes, ConcurrentCachingList<MyCubeBlock>> blockTypeMap;
            if (!I.GridToBlockTypeMap.TryGetValue(targetGrid, out blockTypeMap)) return null;

            ConcurrentCachingList<MyCubeBlock> cubes;
            switch (effectType)
            {
                case JumpNull:
                    if (blockTypeMap.TryGetValue(TargetingDef.BlockTypes.Jumping, out cubes))
                        return cubes;
                    break;
                case EnergySink:
                    if (blockTypeMap.TryGetValue(TargetingDef.BlockTypes.Power, out cubes))
                        return cubes;
                    break;
                case Anchor:
                    if (blockTypeMap.TryGetValue(TargetingDef.BlockTypes.Thrust, out cubes))
                        return cubes;
                    break;
                case Nav:
                    if (blockTypeMap.TryGetValue(TargetingDef.BlockTypes.Steering, out cubes))
                        return cubes;
                    break;
                case Offense:
                    if (blockTypeMap.TryGetValue(TargetingDef.BlockTypes.Offense, out cubes))
                        return cubes;
                    break;
                case Emp:
                case Dot:
                    TopMap topMap;
                    if (I.TopEntityToInfoMap.TryGetValue(targetGrid, out topMap))
                        return topMap.MyCubeBocks;
                    break;
            }

            return null;
        }
    }

    internal struct BlockState
    {
        public Session Session;
        public AmmoDef AmmoDef;
        public IMyFunctionalBlock FunctBlock;
        public bool FirstState;
        public uint FirstTick;
        public uint NextTick;
        public uint Endtick;
        public float Health;
        public long FiringBlockId;
        public int SystemId;
    }

    internal class GridEffect
    {
        internal Vector3D HitPos;
        internal WeaponSystem System;
        internal Ai Ai;
        internal AmmoDef AmmoDef;
        internal long AttackerId;
        internal float Damage;
        internal int Hits;

        internal void Clean()
        {
            System = null;
            HitPos = Vector3D.Zero;
            Ai = null;
            AmmoDef = null;
            AttackerId = 0;
            Damage = 0;
            Hits = 0;
        }
    }
}

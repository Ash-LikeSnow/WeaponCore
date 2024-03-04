using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AmmoDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.EwarDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.ShapeDef.Shapes;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.DamageScaleDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.FragmentDef.TimedSpawnDef;
using static CoreSystems.Support.WeaponDefinition.HardPointDef;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static CoreSystems.Settings.CoreSettings.ServerSettings;

namespace CoreSystems.Support
{
    public sealed class AmmoConstants
    {
        public enum Texture
        {
            Normal,
            Cycle,
            Chaos,
            Resize,
            Wave,
        }

        private const string Arc = "Arc";
        private const string BackSlash = "\\";

        public readonly Stack<ApproachInfo> ApproachInfoPool;
        public readonly MyConcurrentPool<MyEntity> PrimeEntityPool;
        public readonly Dictionary<MyDefinitionBase, float> CustomBlockDefinitionBasesToScales;
        public readonly Dictionary<MyStringHash, MyStringHash> TextureHitMap = new Dictionary<MyStringHash, MyStringHash>();
        public readonly PreComputedMath PreComputedMath;
        public readonly MySoundPair TravelSoundPair;
        public readonly Stack<int[]> PatternShuffleArray = new Stack<int[]>();
        public readonly MySoundPair ShotSoundPair;
        public readonly MySoundPair HitSoundPair;
        public readonly MySoundPair DetSoundPair;
        public readonly MySoundPair ShieldSoundPair;
        public readonly MySoundPair VoxelSoundPair;
        public readonly MySoundPair PlayerSoundPair;
        public readonly MySoundPair FloatingSoundPair;
        public readonly MyAmmoMagazineDefinition MagazineDef;
        public readonly ApproachConstants[] Approaches;
        public readonly AmmoDef[] AmmoPattern;
        public readonly XorShiftRandom Random;
        public readonly AmmoOverride Overrides;

        public readonly MyStringId[] TracerTextures;
        public readonly MyStringId[] TrailTextures;
        public readonly MyStringId[] SegmentTextures;
        public readonly MyPhysicalInventoryItem AmmoItem;
        public readonly MyPhysicalInventoryItem EjectItem;
        public readonly Vector3D FragOffset;
        public readonly EwarType EwarType;
        public readonly Texture TracerMode;
        public readonly Texture TrailMode;
        public readonly PointTypes FragPointType;
        public readonly FactionColor TracerFactionColor;
        public readonly FactionColor SegFactionColor;
        public readonly FactionColor TrailFactionColor;
        public readonly Vector4 LinearTracerColor;
        public readonly Vector4 LinearTracerColorStart;
        public readonly Vector4 LinearTracerColorEnd;
        public readonly Vector4 LinearSegmentColor;
        public readonly Vector4 LinearSegmentColorStart;
        public readonly Vector4 LinearSegmentColorEnd;
        public readonly Vector4 LinearTrailColor;
        public readonly string ModelPath;
        public readonly string HitParticleStr;
        public readonly string DetParticleStr;
        public readonly string DetSoundStr;
        public readonly string ShotSoundStr;
        public readonly int ApproachesCount;
        public readonly int MaxObjectsHit;
        public readonly int TargetLossTime;
        public readonly int MaxLifeTime;
        public readonly int MinArmingTime;
        public readonly int MaxTargets;
        public readonly int PulseInterval;
        public readonly int PulseChance;
        public readonly int FieldGrowTime;
        public readonly int EnergyMagSize;
        public readonly int FragmentId = -1;
        public readonly int MaxChaseTime;
        public readonly int MagazineSize;
        public readonly int WeaponPatternCount;
        public readonly int FragPatternCount;
        public readonly int AmmoIdxPos;
        public readonly int MagsToLoad;
        public readonly int MaxAmmo;
        public readonly int DecayTime;
        public readonly int FragMaxChildren;
        public readonly int FragStartTime;
        public readonly int FragInterval;
        public readonly int MaxFrags;
        public readonly int FragGroupSize;
        public readonly int FragGroupDelay;
        public readonly int DeformDelay;
        public readonly int OffsetTime;
        public readonly uint OnHitProcInterval;
        public readonly uint OnHitDuration;
        public readonly uint FakeVoxelHitTicks;
        public readonly bool HasApproaches;
        public readonly bool KeepAliveAfterTargetLoss;
        public readonly bool ArmedWhenHit;
        public readonly bool UseAimCone;
        public readonly bool TracerAlwaysDraw;
        public readonly bool TrailAlwaysDraw;
        public readonly bool AvDropVelocity;
        public readonly bool FocusEviction;
        public readonly bool FocusOnly;
        public readonly bool CheckFutureIntersection;
        public readonly bool OverrideTarget;
        public readonly bool HasEjectEffect;
        public readonly bool EwarField;
        public readonly bool PrimeModel;
        public readonly bool TriggerModel;
        public readonly bool CollisionIsLine;
        public readonly bool SelfDamage;
        public readonly bool VoxelDamage;
        public readonly bool OffsetEffect;
        public readonly bool Trail;
        public readonly bool TrailColorFade;
        public readonly bool IsMine;
        public readonly bool IsField;
        public readonly bool AmmoParticle;
        public readonly bool HitParticle;
        public readonly bool CustomDetParticle;
        public readonly bool FieldParticle;
        public readonly bool AmmoSkipAccel;
        public readonly bool LineWidthVariance;
        public readonly bool LineColorVariance;
        public readonly bool SegmentWidthVariance;
        public readonly bool SegmentColorVariance;
        public readonly bool OneHitParticle;
        public readonly bool DamageScaling;
        public readonly bool ArmorScaling;
        public readonly bool GridScaling;
        public readonly bool ExpandingField;
        public readonly bool ArmorCoreActive;
        public readonly bool FallOffScaling;
        public readonly bool CustomDamageScales;
        public readonly bool SpeedVariance;
        public readonly bool RangeVariance;
        public readonly bool VirtualBeams;
        public readonly bool IsBeamWeapon;
        public readonly bool ConvergeBeams;
        public readonly bool RotateRealBeam;
        public readonly bool AmmoParticleNoCull;
        public readonly bool FieldParticleNoCull;
        public readonly bool HitParticleNoCull;
        public readonly bool DrawLine;
        public readonly bool Ewar;
        public readonly bool NonAntiSmartEwar;
        public readonly bool TargetOffSet;
        public readonly bool HasBackKickForce;
        public readonly bool BurstMode;
        public readonly bool EnergyAmmo;
        public readonly bool Reloadable;
        public readonly bool MustCharge;
        public readonly bool HasShotReloadDelay;
        public readonly bool HitSound;
        public readonly bool AmmoTravelSound;
        public readonly bool ShotSound;
        public readonly bool IsHybrid;
        public readonly bool IsTurretSelectable;
        public readonly bool CanZombie;
        public readonly bool FeelsGravity;
        public readonly bool StoreGravity;
        public readonly bool MaxTrajectoryGrows;
        public readonly bool HasShotFade;
        public readonly bool CustomExplosionSound;
        public readonly bool GuidedAmmoDetected;
        public readonly bool AntiSmartDetected;
        public readonly bool TargetOverrideDetected;
        public readonly bool AlwaysDraw;
        public readonly bool FixedFireAmmo;
        public readonly bool ClientPredictedAmmo;
        public readonly bool IsCriticalReaction;
        public readonly bool AmmoModsFound;
        public readonly bool EnergyBaseDmg;
        public readonly bool EnergyAreaDmg;
        public readonly bool EnergyDetDmg;
        public readonly bool EnergyShieldDmg;
        public readonly bool SlowFireFixedWeapon;
        public readonly bool HasNegFragmentOffset;
        public readonly bool HasFragmentOffset;
        public readonly bool FragReverse;
        public readonly bool FragDropVelocity;
        public readonly bool FragOnEnd;
        public readonly bool ArmOnlyOnEolHit;
        public readonly bool FragIgnoreArming;
        public readonly bool FragOnEolArmed;
        public readonly bool LongTrail;
        public readonly bool ShortTrail;
        public readonly bool TinyTrail;
        public readonly bool RareTrail;
        public readonly bool EndOfLifeAv;
        public readonly bool EndOfLifeAoe;
        public readonly bool TimedFragments;
        public readonly bool HasFragProximity;
        public readonly bool FragParentDies;
        public readonly bool FragPointAtTarget;
        public readonly bool FullSync;
        public readonly bool PdDeathSync;
        public readonly bool OnHitDeathSync;
        public readonly bool HasFragGroup;
        public readonly bool HasFragment;
        public readonly bool FragmentPattern;
        public readonly bool WeaponPattern;
        public readonly bool SkipAimChecks;
        public readonly bool SkipRayChecks;
        public readonly bool RequiresTarget;
        public readonly bool HasAdvFragOffset;
        public readonly bool DetonationSound;
        public readonly bool CanReportTargetStatus;
        public readonly bool VoxelSound;
        public readonly bool PlayerSound;
        public readonly bool FloatingSound;
        public readonly bool ShieldSound;
        public readonly bool IsDrone;
        public readonly bool IsSmart;
        public readonly bool AccelClearance;
        public readonly bool NoTargetApproach;
        public readonly bool DynamicGuidance;
        public readonly bool TravelTo;
        public readonly bool IsGuided;
        public readonly bool AdvancedSmartSteering;
        public readonly bool NoSteering;
        public readonly bool Roam;
        public readonly bool NoTargetExpire;
        public readonly bool EwarFieldTrigger;
        public readonly bool ZeroEffortNav;
        public readonly bool ProjectilesFirst;
        public readonly bool OnHit;
        public readonly float LargeGridDmgScale;
        public readonly float SmallGridDmgScale;
        public readonly float OffsetRatio;
        public readonly float PowerPerTick;
        public readonly float DirectAimCone;
        public readonly float FragRadial;
        public readonly float FragDegrees;
        public readonly float FragmentOffset;
        public readonly float FallOffDistance;
        public readonly float FallOffMinMultiplier;
        public readonly float EnergyCost;
        public readonly float ChargSize;
        public readonly float RealShotsPerMin;
        public readonly float TargetLossDegree;
        public readonly float TrailWidth;
        public readonly float ShieldDamageBypassMod;
        public readonly float ShieldAntiPenMod;
        public readonly float MagMass;
        public readonly float MagVolume;
        public readonly float Health;
        public readonly float BaseDamage;
        public readonly float Mass;
        public readonly float DetMaxAbsorb;
        public readonly float AoeMaxAbsorb;
        public readonly float ByBlockHitDamage;
        public readonly float EndOfLifeDamage;
        public readonly float EndOfLifeRadius;
        public readonly float DesiredProjectileSpeed;
        public readonly float HitSoundDistSqr;
        public readonly float AmmoTravelSoundDistSqr;
        public readonly float ShotSoundDistSqr;
        public readonly float AmmoSoundMaxDistSqr;
        public readonly float BaseDps;
        public readonly float AreaDps;
        public readonly float EffectiveDps;
        public readonly float PerfectDps;
        public readonly float DetDps;
        public readonly float PeakDps;
        public readonly float RealShotsPerSec;
        public readonly float ShotsPerSec;
        public readonly float MaxTrajectory;
        public readonly float MaxTrajectorySqr;
        public readonly float ShotFadeStep;
        public readonly float TrajectoryStep;
        public readonly float GravityMultiplier;
        public readonly float EndOfLifeDepth;
        public readonly float ByBlockHitDepth;
        public readonly float DetonationSoundDistSqr;
        public readonly float BackKickForce;
        public readonly double MinTurnSpeedSqr;
        public readonly double Aggressiveness;
        public readonly double NavAcceleration;
        public readonly double ScanRange;
        public readonly double DeltaVelocityPerTick;
        public readonly double LargestHitSize;
        public readonly double EwarRadius;
        public readonly double EwarStrength;
        public readonly double ByBlockHitRadius;
        public readonly double ShieldModifier;
        public readonly double MaxLateralThrust;
        public readonly double TracerLength;
        public readonly double CollisionSize;
        public readonly double SmartsDelayDistSqr;
        public readonly double SegmentStep;
        public readonly double HealthHitModifier;
        public readonly double VoxelHitModifier;
        public readonly double MaxOffset;
        public readonly double MinOffsetLength;
        public readonly double MaxOffsetLength;
        public readonly double FragProximity;
        public readonly double SmartOffsetSqr;
        public readonly double HeatModifier;
        public readonly double AccelInMetersPerSec;
        public readonly double MaxAcceleration;
        public readonly double MaxAccelerationSqr;
        public readonly double OffsetMinRangeSqr;
        public readonly double FutureIntersectionRange;
        public readonly double ShieldHeatScaler;

        internal AmmoConstants(WeaponSystem.AmmoType ammo, WeaponDefinition wDef, WeaponSystem system, int ammoIndex)
        {
            AmmoIdxPos = ammoIndex;
            MyInventory.GetItemVolumeAndMass(ammo.AmmoDefinitionId, out MagMass, out MagVolume);
            MagazineDef = MyDefinitionManager.Static.GetAmmoMagazineDefinition(ammo.AmmoDefinitionId);

            IsCriticalReaction = wDef.HardPoint.HardWare.CriticalReaction.Enable;

            ComputeTextures(ammo, out TracerTextures, out SegmentTextures, out TrailTextures, out TracerMode, out TrailMode);

            if (ammo.AmmoDefinitionId.SubtypeId.String != "Energy" || ammo.AmmoDefinitionId.SubtypeId.String == string.Empty) AmmoItem = new MyPhysicalInventoryItem { Amount = 1, Content = VRage.ObjectBuilders.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AmmoMagazine>(ammo.AmmoDefinitionId.SubtypeName) };

            if (!string.IsNullOrEmpty(ammo.EjectionDefinitionId.SubtypeId.String))
            {
                var itemEffect = ammo.AmmoDef.Ejection.Type == AmmoDef.EjectionDef.SpawnType.Item;
                if (itemEffect)
                    EjectItem = new MyPhysicalInventoryItem { Amount = 1, Content = VRage.ObjectBuilders.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Component>(ammo.EjectionDefinitionId.SubtypeId.String) };
                HasEjectEffect = itemEffect && EjectItem.Content != null;
            }
            else if (ammo.AmmoDef.Ejection.Type == AmmoDef.EjectionDef.SpawnType.Particle && !string.IsNullOrEmpty(ammo.AmmoDef.AmmoGraphics.Particles.Eject.Name))
                HasEjectEffect = true;

            if (AmmoItem.Content != null && !Session.I.AmmoItems.ContainsKey(AmmoItem.ItemId))
                Session.I.AmmoItems[AmmoItem.ItemId] = AmmoItem;

            var fragGuidedAmmo = false;
            var fragAntiSmart = false;
            var fragTargetOverride = false;
            var fragHasTimedSpawn = false;
            var fragHasGravity = false;

            for (int i = 0; i < wDef.Ammos.Length; i++)
            {
                var ammoType = wDef.Ammos[i];

                if (ammoType.AmmoRound.Equals(ammo.AmmoDef.Fragment.AmmoRound))
                {
                    FragmentId = i;
                    var hasGuidance = ammoType.Trajectory.Guidance != TrajectoryDef.GuidanceType.None;
                    if (hasGuidance)
                        fragGuidedAmmo = true;

                    if (ammoType.Trajectory.GravityMultiplier > 0)
                        fragHasGravity = true;

                    var hasTimed = ammoType.Fragment.TimedSpawns.Enable;
                    if (hasTimed)
                        fragHasTimedSpawn = true;

                    if (ammoType.Ewar.Type == EwarType.AntiSmart)
                        fragAntiSmart = true;

                    if (hasGuidance && ammoType.Trajectory.Smarts.OverideTarget)
                        fragTargetOverride = true;
                }
            }

            var fragHasAutonomy = fragGuidedAmmo || fragAntiSmart || fragTargetOverride || fragHasTimedSpawn;

            HasFragment = FragmentId > -1;

            float shieldBypassRaw;
            LoadModifiers(ammo.AmmoDef, out Overrides, out AmmoModsFound, out BaseDamage, out Health, out GravityMultiplier, out MaxTrajectory, 
                out MaxTrajectorySqr, out EnergyBaseDmg, out EnergyAreaDmg, out EnergyDetDmg, out EnergyShieldDmg, 
                out ShieldModifier, out FallOffDistance, out FallOffMinMultiplier, out Mass, out shieldBypassRaw);
            
            FixedFireAmmo = system.TurretMovement == WeaponSystem.TurretType.Fixed && ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.None;
            IsMine = ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.DetectFixed || ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.DetectSmart || ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.DetectTravelTo;
            IsField = ammo.AmmoDef.Ewar.Mode == EwarMode.Field || ammo.AmmoDef.Trajectory.DeaccelTime > 0;
            IsHybrid = ammo.AmmoDef.HybridRound;
            IsDrone = ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.DroneAdvanced;
            TravelTo = ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.TravelTo;
            IsTurretSelectable = !ammo.IsShrapnel && ammo.AmmoDef.HardPointUsable;

            ComputeSmarts(ammo, out IsSmart, out Roam, out NoTargetApproach, out AccelClearance, out OverrideTarget, out TargetOffSet,
                out FocusOnly, out FocusEviction, out NoSteering, out AdvancedSmartSteering, out KeepAliveAfterTargetLoss, out NoTargetExpire, out ZeroEffortNav, out ScanRange, out OffsetMinRangeSqr,
                out Aggressiveness, out NavAcceleration, out MinTurnSpeedSqr, out OffsetRatio, out MaxChaseTime, out MaxTargets, out OffsetTime);

            IsGuided = TravelTo || IsMine || IsDrone || IsSmart;

            RequiresTarget = ammo.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.None && !OverrideTarget || system.TrackTargets;


            AmmoParticleNoCull = ammo.AmmoDef.AmmoGraphics.Particles.Ammo.DisableCameraCulling;
            HitParticleNoCull = ammo.AmmoDef.AmmoGraphics.Particles.Hit.DisableCameraCulling;
            FieldParticleNoCull = ammo.AmmoDef.Ewar.Field.Particle.DisableCameraCulling;

            AmmoParticle = !string.IsNullOrEmpty(ammo.AmmoDef.AmmoGraphics.Particles.Ammo.Name);
            HitParticle = !string.IsNullOrEmpty(ammo.AmmoDef.AmmoGraphics.Particles.Hit.Name);
            HitParticleStr = ammo.AmmoDef.AmmoGraphics.Particles.Hit.Name;
            EndOfLifeAv = !ammo.AmmoDef.AreaOfDamage.EndOfLife.NoVisuals && ammo.AmmoDef.AreaOfDamage.EndOfLife.Enable;

            DrawLine = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Enable;
            
            ComputeColors(ammo, out LineColorVariance, out SegmentColorVariance, out LinearTracerColor, out LinearTracerColorStart, out LinearTracerColorEnd, out LinearSegmentColor, out LinearSegmentColorStart, out LinearSegmentColorEnd, out LinearTrailColor, out TracerFactionColor, out SegFactionColor, out TrailFactionColor);
            Random = new XorShiftRandom((ulong)ammo.GetHashCode());

            LineWidthVariance = ammo.AmmoDef.AmmoGraphics.Lines.WidthVariance.Start > 0 || ammo.AmmoDef.AmmoGraphics.Lines.WidthVariance.End > 0;
            SegmentWidthVariance = TracerMode == Texture.Resize && ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.WidthVariance.Start > 0 || ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.WidthVariance.End > 0;

            SegmentStep = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Speed * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            SpeedVariance = ammo.AmmoDef.Trajectory.SpeedVariance.Start > 0 || ammo.AmmoDef.Trajectory.SpeedVariance.End > 0;
            RangeVariance = ammo.AmmoDef.Trajectory.RangeVariance.Start > 0 || ammo.AmmoDef.Trajectory.RangeVariance.End > 0;


            TargetLossTime = ammo.AmmoDef.Trajectory.TargetLossTime > 0 ? ammo.AmmoDef.Trajectory.TargetLossTime : int.MaxValue;
            CanZombie = TargetLossTime > 0 && TargetLossTime != int.MaxValue && !IsMine;
            MaxLifeTime = ammo.AmmoDef.Trajectory.MaxLifeTime > 0 ? ammo.AmmoDef.Trajectory.MaxLifeTime : int.MaxValue;

            AccelInMetersPerSec = ammo.AmmoDef.Trajectory.AccelPerSec;
            DeltaVelocityPerTick = AccelInMetersPerSec * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxAcceleration = ammo.AmmoDef.Trajectory.TotalAcceleration > 0 ? ammo.AmmoDef.Trajectory.TotalAcceleration : double.MaxValue;
            MaxAccelerationSqr = MaxAcceleration * MaxAcceleration;

            MaxObjectsHit = ammo.AmmoDef.ObjectsHit.MaxObjectsHit > 0 ? ammo.AmmoDef.ObjectsHit.MaxObjectsHit : int.MaxValue;
            ArmOnlyOnEolHit = ammo.AmmoDef.AreaOfDamage.EndOfLife.ArmOnlyOnHit;

            TargetLossDegree = ammo.AmmoDef.Trajectory.TargetLossDegree > 0 ? (float)Math.Cos(MathHelper.ToRadians(ammo.AmmoDef.Trajectory.TargetLossDegree)) : 0;

            Fragments(ammo, out HasFragmentOffset, out HasNegFragmentOffset, out FragmentOffset, out FragRadial, out FragDegrees, out FragReverse, out FragDropVelocity, out FragMaxChildren, out FragIgnoreArming, out FragOnEolArmed, out ArmedWhenHit, out FragOnEnd, out HasAdvFragOffset, out FragOffset);
            TimedSpawn(ammo, out TimedFragments, out FragStartTime, out FragInterval, out MaxFrags, out FragGroupSize, out FragGroupDelay, out FragProximity, out HasFragProximity, out FragParentDies, out FragPointAtTarget, out HasFragGroup, out FragPointType, out DirectAimCone, out UseAimCone);

            ArmorCoreActive = Session.I.ArmorCoreActive;

            AmmoSkipAccel = ammo.AmmoDef.Trajectory.AccelPerSec <= 0;
            FeelsGravity = GravityMultiplier > 0;
            StoreGravity = FeelsGravity || fragHasGravity;
            SmartOffsetSqr = ammo.AmmoDef.Trajectory.Smarts.Inaccuracy * ammo.AmmoDef.Trajectory.Smarts.Inaccuracy;
            BackKickForce = AmmoModsFound && Overrides.BackKickForce.HasValue ? Math.Max(Overrides.BackKickForce.Value, 0f) : ammo.AmmoDef.BackKickForce;
            HasBackKickForce = !MathHelper.IsZero(BackKickForce);
            MaxLateralThrust = MathHelperD.Clamp(ammo.AmmoDef.Trajectory.Smarts.MaxLateralThrust >= 1 ? double.MaxValue : ammo.AmmoDef.Trajectory.Smarts.MaxLateralThrust, 0.0001, double.MaxValue);

            CustomDetParticle = !string.IsNullOrEmpty(ammo.AmmoDef.AreaOfDamage.EndOfLife.CustomParticle);
            DetParticleStr = !string.IsNullOrEmpty(ammo.AmmoDef.AreaOfDamage.EndOfLife.CustomParticle) ? ammo.AmmoDef.AreaOfDamage.EndOfLife.CustomParticle : "Explosion_Missile";
            CustomExplosionSound = !string.IsNullOrEmpty(ammo.AmmoDef.AreaOfDamage.EndOfLife.CustomSound);
            DetSoundStr = CustomExplosionSound ? ammo.AmmoDef.AreaOfDamage.EndOfLife.CustomSound : !ammo.IsShrapnel ? "WepSmallMissileExpl" : string.Empty;
            FieldParticle = !string.IsNullOrEmpty(ammo.AmmoDef.Ewar.Field.Particle.Name);

            Fields(ammo.AmmoDef, out PulseInterval, out PulseChance, out EwarField, out FieldGrowTime, out ExpandingField);
            AreaEffects(ammo.AmmoDef, out ByBlockHitDepth, out EndOfLifeDepth, out EwarType, out ByBlockHitDamage, out ByBlockHitRadius, out EndOfLifeDamage, out EndOfLifeRadius, out EwarStrength, out LargestHitSize, out EwarRadius, out Ewar, out NonAntiSmartEwar, out EwarFieldTrigger, out MinArmingTime, out AoeMaxAbsorb, out DetMaxAbsorb, out EndOfLifeAoe);
            Beams(ammo.AmmoDef, out IsBeamWeapon, out VirtualBeams, out RotateRealBeam, out ConvergeBeams, out OneHitParticle, out OffsetEffect, out FakeVoxelHitTicks);

            var givenSpeed = AmmoModsFound && Overrides.DesiredSpeed.HasValue ? Math.Max(Overrides.DesiredSpeed.Value, 0f) : ammo.AmmoDef.Trajectory.DesiredSpeed;
            DesiredProjectileSpeed = !IsBeamWeapon ? givenSpeed : MaxTrajectory * MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            HeatModifier = ammo.AmmoDef.HeatModifier > 0 ? ammo.AmmoDef.HeatModifier : 1;
            ShieldHeatScaler = MyUtils.IsZero(ammo.AmmoDef.DamageScales.Shields.HeatModifier) ? 1 : ammo.AmmoDef.DamageScales.Shields.HeatModifier;

            ComputeShieldBypass(shieldBypassRaw, out ShieldDamageBypassMod, out ShieldAntiPenMod);
            ComputeApproaches(ammo, wDef, out ApproachesCount, out Approaches, out ApproachInfoPool, out HasApproaches);
            ComputeAmmoPattern(ammo, system, wDef, fragGuidedAmmo, fragAntiSmart, fragTargetOverride, out AntiSmartDetected, out TargetOverrideDetected, out AmmoPattern, out WeaponPatternCount, out FragPatternCount, out GuidedAmmoDetected, out WeaponPattern, out FragmentPattern);

            DamageScales(ammo.AmmoDef, out DamageScaling, out FallOffScaling, out ArmorScaling, out GridScaling, out CustomDamageScales, out CustomBlockDefinitionBasesToScales, out SelfDamage, out VoxelDamage, out HealthHitModifier, out VoxelHitModifier, out DeformDelay, out LargeGridDmgScale, out SmallGridDmgScale);
            CollisionShape(ammo.AmmoDef, out CollisionIsLine, out CollisionSize, out TracerLength);
            
            SmartsDelayDistSqr = (CollisionSize * ammo.AmmoDef.Trajectory.Smarts.TrackingDelay) * (CollisionSize * ammo.AmmoDef.Trajectory.Smarts.TrackingDelay);
            PrimeEntityPool = Models(ammo.AmmoDef, wDef, out PrimeModel, out TriggerModel, out ModelPath);

            CheckFutureIntersection = ammo.AmmoDef.Trajectory.Smarts.CheckFutureIntersection;
            FutureIntersectionRange = ammo.AmmoDef.Trajectory.Smarts.FutureIntersectionRange > 0 ? ammo.AmmoDef.Trajectory.Smarts.FutureIntersectionRange + CollisionSize : DesiredProjectileSpeed + CollisionSize;

            Energy(ammo, system, wDef, out EnergyAmmo, out MustCharge, out Reloadable, out EnergyCost, out EnergyMagSize, out ChargSize, out BurstMode, out HasShotReloadDelay, out PowerPerTick);
            Sound(ammo, system,out HitSound, out HitSoundPair, out AmmoTravelSound, out TravelSoundPair, out ShotSound, out ShotSoundPair, out DetonationSound, out DetSoundPair, out HitSoundDistSqr, out AmmoTravelSoundDistSqr, out AmmoSoundMaxDistSqr,
                out ShotSoundDistSqr, out DetonationSoundDistSqr, out ShotSoundStr, out VoxelSound, out VoxelSoundPair, out FloatingSound, out FloatingSoundPair, out PlayerSound, out PlayerSoundPair, out ShieldSound, out ShieldSoundPair);

            MagazineSize = EnergyAmmo ? EnergyMagSize : MagazineDef.Capacity;
            MagsToLoad = wDef.HardPoint.Loading.MagsToLoad > 0 ? wDef.HardPoint.Loading.MagsToLoad : 1;
            MaxAmmo = MagsToLoad * MagazineSize;

            GetPeakDps(ammo, system, wDef, out PeakDps, out EffectiveDps, out PerfectDps, out ShotsPerSec, out RealShotsPerSec, out BaseDps, out AreaDps, out DetDps, out RealShotsPerMin);
            var clientPredictedAmmoDisabled = AmmoModsFound && Overrides.DisableClientPredictedAmmo.HasValue && Overrides.DisableClientPredictedAmmo.Value;
            var predictionEligible = Session.I.IsClient || Session.I.DedicatedServer;


            var predictedShotLimit = system.PartType != HardwareDef.HardwareType.HandWeapon ? 120 : 450;
            var predictedReloadLimit = system.PartType != HardwareDef.HardwareType.HandWeapon ? 120 : 60;

            ClientPredictedAmmo = predictionEligible && FixedFireAmmo && !fragHasAutonomy && !ammo.IsShrapnel && RealShotsPerMin <= predictedShotLimit && !clientPredictedAmmoDisabled;

            if (!ClientPredictedAmmo && predictionEligible)
                Log.Line($"{ammo.AmmoDef.AmmoRound} is NOT enabled for client prediction");

            SlowFireFixedWeapon = system.TurretMovement == WeaponSystem.TurretType.Fixed && (RealShotsPerMin <= predictedShotLimit || Reloadable && system.WConst.ReloadTime >= predictedReloadLimit);

            if (!SlowFireFixedWeapon && system.TurretMovement == WeaponSystem.TurretType.Fixed && predictionEligible)
                Log.Line($"{ammo.AmmoDef.AmmoRound} does not qualify for fixed weapon client reload verification");

            SkipAimChecks = (ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.Smart || ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.DroneAdvanced) && system.TurretMovement == WeaponSystem.TurretType.Fixed && !system.TargetSlaving;
            SkipRayChecks = (ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.Smart || ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.DroneAdvanced) && system.TurretMovement == WeaponSystem.TurretType.Fixed && system.TargetSlaving;
            
            Trail = ammo.AmmoDef.AmmoGraphics.Lines.Trail.Enable;
            HasShotFade = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.VisualFadeStart > 0 && ammo.AmmoDef.AmmoGraphics.Lines.Tracer.VisualFadeEnd > 1;
            MaxTrajectoryGrows = ammo.AmmoDef.Trajectory.MaxTrajectoryTime > 1;
            ComputeSteps(ammo, out ShotFadeStep, out TrajectoryStep, out AlwaysDraw, out TracerAlwaysDraw, out TrailAlwaysDraw, out AvDropVelocity);

            TrailWidth = ammo.AmmoDef.AmmoGraphics.Lines.Trail.CustomWidth > 0 ? ammo.AmmoDef.AmmoGraphics.Lines.Trail.CustomWidth : ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Width;
            DecayTime = ammo.AmmoDef.AmmoGraphics.Lines.Trail.DecayTime;
            LongTrail = DecayTime > 20;
            TinyTrail = DecayTime <= 5;
            ShortTrail = !TinyTrail && DecayTime <= 10;
            RareTrail = DecayTime > 0 && ShotsPerSec * 60 <= 6;
            TrailColorFade = ammo.AmmoDef.AmmoGraphics.Lines.Trail.UseColorFade;

            MaxOffset = ammo.AmmoDef.AmmoGraphics.Lines.OffsetEffect.MaxOffset;
            MinOffsetLength = ammo.AmmoDef.AmmoGraphics.Lines.OffsetEffect.MinLength;
            MaxOffsetLength = ammo.AmmoDef.AmmoGraphics.Lines.OffsetEffect.MaxLength;
            CanReportTargetStatus = RequiresTarget && system.TrackGrids && !system.DesignatorWeapon && PeakDps > 0;
            DynamicGuidance = ammo.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.None && ammo.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.TravelTo && !IsBeamWeapon;

            if (CollisionSize > 5 && !Session.I.LocalVersion) Log.Line($"{ammo.AmmoDef.AmmoRound} has large largeCollisionSize: {CollisionSize} meters");
            if (FeelsGravity && !IsSmart && system.TrackTargets && (system.Prediction == Prediction.Off || system.Prediction == Prediction.Basic) && ammo.AmmoDef.Trajectory.MaxTrajectory / ammo.AmmoDef.Trajectory.DesiredSpeed > 0.5f)
            {
                var flightTime = ammo.AmmoDef.Trajectory.MaxTrajectory / ammo.AmmoDef.Trajectory.DesiredSpeed;
                Log.Line($"{ammo.AmmoDef.AmmoRound} has {(int)(0.5 * 9.8 * flightTime * flightTime)}m grav drop at 1g.  {system.PartName} needs Accurate/Advanced aim prediction to account for gravity.");
            }

            FullSync = ammo.AmmoDef.Sync.Full && Session.I.MpActive && (IsDrone || IsSmart);
            PdDeathSync = !FullSync && ammo.AmmoDef.Sync.PointDefense && Session.I.MpActive && Health > 0 && !IsBeamWeapon && !Ewar;
            OnHitDeathSync = !FullSync && ammo.AmmoDef.Sync.OnHitDeath && Session.I.MpActive && !IsBeamWeapon && !Ewar;

            ProjectilesFirst = system.ProjectilesFirst;

            PreComputedMath = new PreComputedMath(ammo, this);

            OnHit = false;
            OnHitProcInterval = 0;
        }

        internal void Purge()
        {
            if (AmmoPattern != null)
            {
                for (int i = 0; i < AmmoPattern.Length; i++)
                    AmmoPattern[i] = null;
            }

            if (PatternShuffleArray != null)
            {
                for (int i = 0; i < PatternShuffleArray.Count; i++)
                    PatternShuffleArray.Pop();
            }


            CustomBlockDefinitionBasesToScales?.Clear();
            PrimeEntityPool?.Clean();

            if (Approaches != null)
            {
                for (int i = 0; i < Approaches.Length; i++)
                {
                    if (Approaches[i] != null)
                    {
                        Approaches[i].Clean();
                        Approaches[i] = null;
                    }
                }
            }
        }


        private void ComputeSmarts(WeaponSystem.AmmoType ammo, out bool isSmart, out bool roam, out bool noTargetApproach, out bool accelClearance, out bool overrideTarget, out bool targetOffSet,
            out bool focusOnly, out bool focusEviction, out bool noSteering, out bool advancedSmartSteering, out bool keepAliveAfterTargetLoss, out bool noTargetExpire, out bool zeroEffortNav, out double scanRange, out double offsetMinRangeSqr,
            out double aggressiveness, out double navAcceleration, out double minTurnSpeedSqr, out float offsetRatio, out int maxChaseTime, out int maxTargets, out int offsetTime)
        {
            isSmart = ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.Smart || ammo.AmmoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.DetectSmart;

            roam = ammo.AmmoDef.Trajectory.Smarts.Roam;
            accelClearance = ammo.AmmoDef.Trajectory.Smarts.AccelClearance;
            overrideTarget = ammo.AmmoDef.Trajectory.Smarts.OverideTarget;

            targetOffSet = ammo.AmmoDef.Trajectory.Smarts.Inaccuracy > 0;
            focusOnly = ammo.AmmoDef.Trajectory.Smarts.FocusOnly;
            focusEviction = ammo.AmmoDef.Trajectory.Smarts.FocusEviction;
            noSteering = ammo.AmmoDef.Trajectory.Smarts.NoSteering;
            advancedSmartSteering = ammo.AmmoDef.Trajectory.Smarts.SteeringLimit > 0;
            keepAliveAfterTargetLoss = ammo.AmmoDef.Trajectory.Smarts.KeepAliveAfterTargetLoss;
            noTargetExpire = ammo.AmmoDef.Trajectory.Smarts.NoTargetExpire;

            scanRange = ammo.AmmoDef.Trajectory.Smarts.ScanRange;
            offsetMinRangeSqr = ammo.AmmoDef.Trajectory.Smarts.OffsetMinRange * ammo.AmmoDef.Trajectory.Smarts.OffsetMinRange;
            aggressiveness = ammo.AmmoDef.Trajectory.Smarts.Aggressiveness;
            
            var disableNavAcceleration = ammo.AmmoDef.Trajectory.Smarts.NavAcceleration < 0;
            var useNavAccelerationValue = ammo.AmmoDef.Trajectory.Smarts.NavAcceleration > 0;
            
            var defaultAccelerationValue = !disableNavAcceleration && !useNavAccelerationValue && aggressiveness > 0 ? aggressiveness / 2 : 0;
            navAcceleration = disableNavAcceleration ? 0 : useNavAccelerationValue ? ammo.AmmoDef.Trajectory.Smarts.NavAcceleration : defaultAccelerationValue;
            
            minTurnSpeedSqr = ammo.AmmoDef.Trajectory.Smarts.MinTurnSpeed * ammo.AmmoDef.Trajectory.Smarts.MinTurnSpeed;

            offsetRatio = ammo.AmmoDef.Trajectory.Smarts.OffsetRatio;

            maxChaseTime = ammo.AmmoDef.Trajectory.Smarts.MaxChaseTime > 0 ? ammo.AmmoDef.Trajectory.Smarts.MaxChaseTime : int.MaxValue;
            maxTargets = ammo.AmmoDef.Trajectory.Smarts.MaxTargets;
            offsetTime = ammo.AmmoDef.Trajectory.Smarts.OffsetTime;
            noTargetApproach = ammo.AmmoDef.Trajectory.Smarts.NoTargetApproach;
            zeroEffortNav = ammo.AmmoDef.Trajectory.Smarts.AltNavigation;
        }


        internal void ComputeColors(WeaponSystem.AmmoType ammo, out bool lineColorVariance, out bool segmentColorVariance, out Vector4 linearTracerColor, out Vector4 linearTracerColorStart, out Vector4 linearTracerColorEnd, out Vector4 linearSegmentColor, out Vector4 linearSegmentColorStart, out Vector4 linearSegmentColorEnd, out Vector4 linearTrailColor, out FactionColor tracerFactionColor, out FactionColor segFactionColor, out FactionColor trailFactionColor)
        {
            lineColorVariance = ammo.AmmoDef.AmmoGraphics.Lines.ColorVariance.Start > 0 && ammo.AmmoDef.AmmoGraphics.Lines.ColorVariance.End > 0;

            var lines = ammo.AmmoDef.AmmoGraphics.Lines;
            var tracerColor = lines.Tracer.Color;
            tracerFactionColor = lines.Tracer.FactionColor;
            linearTracerColor = tracerColor.ToLinearRGB();
            if (lineColorVariance)
            {

                var startColor = tracerColor;
                var endColor = tracerColor;

                startColor.X *= lines.ColorVariance.Start;
                startColor.Y *= lines.ColorVariance.Start;
                startColor.Z *= lines.ColorVariance.Start;
                linearTracerColorStart = startColor.ToLinearRGB();

                endColor.X *= lines.ColorVariance.End;
                endColor.Y *= lines.ColorVariance.End;
                endColor.Z *= lines.ColorVariance.End;
                linearTracerColorEnd = endColor.ToLinearRGB();
            }
            else
            {
                linearTracerColorStart = Vector4.Zero;
                linearTracerColorEnd = Vector4.Zero;
            }

            segmentColorVariance = TracerMode == Texture.Resize && ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.ColorVariance.Start > 0 && ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.ColorVariance.End > 0;

            var seg = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation;
            var segColor = seg.Color;
            segFactionColor = lines.Tracer.Segmentation.FactionColor;
            linearSegmentColor = segColor.ToLinearRGB();

            if (segmentColorVariance)
            {
                var startColor = segColor;
                var endColor = segColor;

                startColor.X *= seg.ColorVariance.Start;
                startColor.Y *= seg.ColorVariance.Start;
                startColor.Z *= seg.ColorVariance.Start;
                linearSegmentColorStart = startColor.ToLinearRGB();

                endColor.X *= seg.ColorVariance.End;
                endColor.Y *= seg.ColorVariance.End;
                endColor.Z *= seg.ColorVariance.End;
                linearSegmentColorEnd = endColor.ToLinearRGB();
            }
            else
            {
                linearSegmentColorStart = Vector4.Zero;
                linearSegmentColorEnd = Vector4.Zero;
            }

            var trailColor = lines.Trail.Color;
            trailFactionColor = lines.Trail.FactionColor;
            linearTrailColor = trailColor.ToLinearRGB();

        }
        internal void ComputeShieldBypass(float shieldBypassRaw, out float shieldDamageBypassMod, out float shieldAntiPenMod)
        {
            shieldAntiPenMod = 0;
            if (shieldBypassRaw <= 0)
            {
                if (shieldBypassRaw < -1)
                    shieldAntiPenMod = MathHelper.Clamp(Math.Abs(shieldBypassRaw + 1), 0, 1);


                shieldDamageBypassMod = 0;
            }
            else if (shieldBypassRaw > 1)
                shieldDamageBypassMod = 0.00001f;
            else
                shieldDamageBypassMod = MathHelper.Clamp(1 - shieldBypassRaw, 0.00001f, 0.99999f);
        }

        internal void ComputeTextures(WeaponSystem.AmmoType ammo, out MyStringId[] tracerTextures, out MyStringId[] segmentTextures, out MyStringId[] trailTextures, out Texture tracerTexture, out Texture trailTexture)
        {
            var lineSegments = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Enable && ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.SegmentLength > 0;

            if (lineSegments)
                tracerTexture = Texture.Resize;
            else if (ammo.AmmoDef.AmmoGraphics.Lines.Tracer.TextureMode == AmmoDef.GraphicDef.LineDef.Texture.Normal)
                tracerTexture = Texture.Normal;
            else if (ammo.AmmoDef.AmmoGraphics.Lines.Tracer.TextureMode == AmmoDef.GraphicDef.LineDef.Texture.Cycle)
                tracerTexture = Texture.Cycle;
            else if (ammo.AmmoDef.AmmoGraphics.Lines.Tracer.TextureMode == AmmoDef.GraphicDef.LineDef.Texture.Wave)
                tracerTexture = Texture.Wave;
            else tracerTexture = Texture.Chaos;
            trailTexture = (Texture)ammo.AmmoDef.AmmoGraphics.Lines.Trail.TextureMode;

            if (ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Textures != null && ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Textures.Length > 0)
            {
                tracerTextures = new MyStringId[ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Textures.Length];
                for (int i = 0; i < ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Textures.Length; i++)
                {
                    var value = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Textures[i];
                    if (string.IsNullOrEmpty(value))
                        value = ammo.AmmoDef.AmmoGraphics.Lines.TracerMaterial;
                    tracerTextures[i] = MyStringId.GetOrCompute(value);
                }
            }
            else tracerTextures = new[] { MyStringId.GetOrCompute(ammo.AmmoDef.AmmoGraphics.Lines.TracerMaterial) };

            if (ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Textures != null && ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Textures.Length > 0)
            {
                segmentTextures = new MyStringId[ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Textures.Length];
                for (int i = 0; i < ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Textures.Length; i++)
                {
                    var value = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Textures[i];
                    if (string.IsNullOrEmpty(value))
                        value = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Material;
                    segmentTextures[i] = MyStringId.GetOrCompute(value);
                }
            }
            else segmentTextures = new[] { MyStringId.GetOrCompute(ammo.AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Material) };

            if (ammo.AmmoDef.AmmoGraphics.Lines.Trail.Textures != null && ammo.AmmoDef.AmmoGraphics.Lines.Trail.Textures.Length > 0)
            {
                trailTextures = new MyStringId[ammo.AmmoDef.AmmoGraphics.Lines.Trail.Textures.Length];
                for (int i = 0; i < ammo.AmmoDef.AmmoGraphics.Lines.Trail.Textures.Length; i++)
                {
                    var value = ammo.AmmoDef.AmmoGraphics.Lines.Trail.Textures[i];
                    if (string.IsNullOrEmpty(value))
                        value = ammo.AmmoDef.AmmoGraphics.Lines.Trail.Material;
                    trailTextures[i] = MyStringId.GetOrCompute(value);
                }
            }
            else trailTextures = new[] { MyStringId.GetOrCompute(ammo.AmmoDef.AmmoGraphics.Lines.Trail.Material) };

            if (ammo.AmmoDef.AmmoGraphics.Decals.Map != null && ammo.AmmoDef.AmmoGraphics.Decals.MaxAge > 0)
            {
                foreach (var textureMapDef in ammo.AmmoDef.AmmoGraphics.Decals.Map)
                {
                    if (!string.IsNullOrEmpty(textureMapDef.HitMaterial))
                        TextureHitMap[MyStringHash.GetOrCompute(textureMapDef.HitMaterial)] = MyStringHash.GetOrCompute(textureMapDef.DecalMaterial);
                }
            }
        }

        private void ComputeSteps(WeaponSystem.AmmoType ammo, out float shotFadeStep, out float trajectoryStep, out bool alwaysDraw, out bool tracerAlwaysDraw, out bool trailAlwaysDraw, out bool avDropVelocity)
        {
            var changeFadeSteps = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.VisualFadeEnd - ammo.AmmoDef.AmmoGraphics.Lines.Tracer.VisualFadeStart;
            shotFadeStep = 1f / changeFadeSteps;

            trajectoryStep = MaxTrajectoryGrows ? MaxTrajectory / ammo.AmmoDef.Trajectory.MaxTrajectoryTime : MaxTrajectory;
            tracerAlwaysDraw = ammo.AmmoDef.AmmoGraphics.Lines.Tracer.AlwaysDraw;
            trailAlwaysDraw = ammo.AmmoDef.AmmoGraphics.Lines.Trail.AlwaysDraw;
            avDropVelocity = ammo.AmmoDef.AmmoGraphics.Lines.DropParentVelocity;

            alwaysDraw = (Trail || HasShotFade) && RealShotsPerSec < 0.1 || tracerAlwaysDraw || trailAlwaysDraw;
        }

        private void Fragments(WeaponSystem.AmmoType ammo, out bool hasFragmentOffset, out bool hasNegFragmentOffset, out float fragmentOffset, out float fragRadial, out float fragDegrees, out bool fragReverse, out bool fragDropVelocity, out int fragMaxChildren, out bool fragIgnoreArming, out bool fragOnEolArmed, out bool armWhenHit, out bool fragOnEnd, out bool hasFragOffset, out Vector3D fragOffset)
        {
            hasFragmentOffset = !MyUtils.IsZero(ammo.AmmoDef.Fragment.Offset);
            hasNegFragmentOffset = ammo.AmmoDef.Fragment.Offset < 0;
            fragmentOffset = Math.Abs(ammo.AmmoDef.Fragment.Offset);
            fragRadial = MathHelper.ToRadians(MathHelper.Clamp(ammo.AmmoDef.Fragment.Radial, 0, 360));
            fragDegrees = MathHelper.ToRadians(MathHelper.Clamp(ammo.AmmoDef.Fragment.Degrees, 0, 360));
            fragReverse = ammo.AmmoDef.Fragment.Reverse;
            fragDropVelocity = ammo.AmmoDef.Fragment.DropVelocity;
            fragMaxChildren = ammo.AmmoDef.Fragment.MaxChildren > 0 ? ammo.AmmoDef.Fragment.MaxChildren : int.MaxValue;
            fragIgnoreArming = ammo.AmmoDef.Fragment.IgnoreArming;
            armWhenHit = ammo.AmmoDef.Fragment.ArmWhenHit;

            fragOnEolArmed = ammo.AmmoDef.AreaOfDamage.EndOfLife.Enable && ArmOnlyOnEolHit && !FragIgnoreArming && HasFragment;
            fragOnEnd = !FragOnEolArmed && (!ammo.AmmoDef.Fragment.TimedSpawns.Enable || armWhenHit) && HasFragment;
            hasFragOffset = !Vector3D.IsZero(ammo.AmmoDef.Fragment.AdvOffset);
            fragOffset = ammo.AmmoDef.Fragment.AdvOffset;
        }

        private void TimedSpawn(WeaponSystem.AmmoType ammo, out bool timedFragments, out int startTime, out int interval, out int maxSpawns, out int groupSize, out int groupDelay, out double proximity, out bool hasProximity, out bool parentDies, out bool pointAtTarget, out bool hasGroup, out PointTypes pointType, out float directAimCone, out bool useAimCone)
        {
            timedFragments = ammo.AmmoDef.Fragment.TimedSpawns.Enable && HasFragment;
            startTime = ammo.AmmoDef.Fragment.TimedSpawns.StartTime;
            interval = ammo.AmmoDef.Fragment.TimedSpawns.Interval;
            maxSpawns = ammo.AmmoDef.Fragment.TimedSpawns.MaxSpawns;
            proximity = ammo.AmmoDef.Fragment.TimedSpawns.Proximity;
            hasProximity = proximity > 0;
            parentDies = ammo.AmmoDef.Fragment.TimedSpawns.ParentDies;
            pointAtTarget = ammo.AmmoDef.Fragment.TimedSpawns.PointAtTarget;
            groupSize = ammo.AmmoDef.Fragment.TimedSpawns.GroupSize;
            groupDelay = ammo.AmmoDef.Fragment.TimedSpawns.GroupDelay;
            hasGroup = groupSize > 0 && groupDelay > 0;
            pointType = ammo.AmmoDef.Fragment.TimedSpawns.PointType;
            useAimCone = ammo.AmmoDef.Fragment.TimedSpawns.DirectAimCone > 0 && pointType == PointTypes.Direct;
            directAimCone = MathHelper.ToRadians(Math.Max(ammo.AmmoDef.Fragment.TimedSpawns.DirectAimCone, 1));
        }

        private void ComputeApproaches(WeaponSystem.AmmoType ammo, WeaponDefinition wDef, out int approachesCount, out ApproachConstants[] approaches, out Stack<ApproachInfo> approachInfoPool, out bool hasApproaches)
        {
            if (IsSmart && ammo.AmmoDef.Trajectory.Approaches != null && ammo.AmmoDef.Trajectory.Approaches.Length > 0)
            {
                approachesCount = ammo.AmmoDef.Trajectory.Approaches.Length;
                approaches = new ApproachConstants[approachesCount];

                for (int i = 0; i < approaches.Length; i++)
                    approaches[i] = new ApproachConstants(ammo, i, wDef);

                approachInfoPool = new Stack<ApproachInfo>(approachesCount);
                hasApproaches = true;
            }
            else
            {
                approachesCount = 0;
                approaches = null;
                approachInfoPool = null;
                hasApproaches = false;
            }
        }

        private void ComputeAmmoPattern(WeaponSystem.AmmoType ammo, WeaponSystem system, WeaponDefinition wDef, bool fragGuidedAmmo, bool fragAntiSmart, bool fragTargetOverride, out bool hasAntiSmart, out bool hasTargetOverride, out AmmoDef[] ammoPattern, out int weaponPatternCount, out int fragmentPatternCount, out bool hasGuidedAmmo, out bool weaponPattern, out bool fragmentPattern)
        {
            var pattern = ammo.AmmoDef.Pattern;
            var indexPos = 0;
            int indexCount;

            weaponPattern = pattern.Enable || pattern.Mode == AmmoDef.PatternDef.PatternModes.Both || pattern.Mode == AmmoDef.PatternDef.PatternModes.Weapon;
            fragmentPattern = pattern.Mode == AmmoDef.PatternDef.PatternModes.Both || pattern.Mode == AmmoDef.PatternDef.PatternModes.Fragment;
            var enabled = weaponPattern || fragmentPattern;

            if (!weaponPattern && !fragmentPattern)
                indexCount = 1;
            else
            {
                indexCount = pattern.Patterns.Length;
                if (!pattern.SkipParent) indexCount += 1;
            }

            weaponPatternCount = weaponPattern ? indexCount : 1;

            fragmentPatternCount = fragmentPattern ? indexCount : 1;
            if (!pattern.SkipParent && fragmentPattern) fragmentPatternCount--;
            ammoPattern = new AmmoDef[indexCount];

            if (!pattern.SkipParent && pattern.Mode != AmmoDef.PatternDef.PatternModes.Fragment)
                ammoPattern[indexPos++] = ammo.AmmoDef;

            var validPatterns = 0;
            var patternTargetOverride = false;
            var patternGuidedAmmo = false;
            var patternAntiSmart = false;

            if (enabled)
            {
                for (int j = 0; j < ammo.AmmoDef.Pattern.Patterns.Length; j++)
                {
                    var aPattern = ammo.AmmoDef.Pattern.Patterns[j];
                    if (string.IsNullOrEmpty(aPattern))
                        continue;

                    ++validPatterns;

                    for (int i = 0; i < wDef.Ammos.Length; i++)
                    {
                        var ammoDef = wDef.Ammos[i];
                        if (aPattern.Equals(ammoDef.AmmoRound))
                        {
                            
                            ammoPattern[indexPos++] = ammoDef;
                            var hasGuidance = ammoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.None;
                            if (!patternGuidedAmmo && hasGuidance)
                                patternGuidedAmmo = true;

                            if (!patternAntiSmart && ammoDef.Ewar.Type == EwarType.AntiSmart)
                                patternAntiSmart = true;
                            if (hasGuidance && ammoDef.Trajectory.Smarts.OverideTarget)
                                patternTargetOverride = true;
                        }
                    }
                }
            }

            if (validPatterns == 0) {
                weaponPattern = false;
                fragmentPattern = false;
            }

            hasGuidedAmmo = fragGuidedAmmo || patternGuidedAmmo || ammo.AmmoDef.Trajectory.Guidance != TrajectoryDef.GuidanceType.None;
            hasAntiSmart = fragAntiSmart || patternAntiSmart || ammo.AmmoDef.Ewar.Type == EwarType.AntiSmart;
            hasTargetOverride = fragTargetOverride || patternTargetOverride || OverrideTarget;
        }

        private void Fields(AmmoDef ammoDef, out int pulseInterval, out int pulseChance, out bool ewarField, out int growTime, out bool expandingField)
        {
            pulseInterval = ammoDef.Ewar.Field.Interval;
            growTime = ammoDef.Ewar.Field.GrowTime;
            ewarField = ammoDef.Ewar.Enable && ammoDef.Ewar.Mode == EwarMode.Field;
            expandingField = growTime > 0 && ewarField;
            pulseChance = ammoDef.Ewar.Field.PulseChance;
        }

        private void AreaEffects(AmmoDef ammoDef, out float byBlockHitDepth, out float endOfLifeDepth, out EwarType ewarType, out float byBlockHitDamage, out double byBlockHitRadius, out float endOfLifeDamage, out float endOfLifeRadius, out double ewarEffectStrength, out double largestHitSize, out double ewarEffectSize, out bool eWar, out bool nonAntiSmart, out bool eWarFieldTrigger, out int minArmingTime, out float aoeMaxAbsorb, out float detMaxAbsorb, out bool endOfLifeAoe)
        {
            ewarType = ammoDef.Ewar.Type;

            if (AmmoModsFound && Overrides.AreaEffectDamage.HasValue)
                byBlockHitDamage = Math.Max(Overrides.AreaEffectDamage.Value, 0f);
            else
                byBlockHitDamage = ammoDef.AreaOfDamage.ByBlockHit.Damage;

            if (AmmoModsFound && Overrides.AreaEffectRadius.HasValue)
                byBlockHitRadius = Math.Max(Overrides.AreaEffectRadius.Value, 0);
            else
                byBlockHitRadius = ammoDef.AreaOfDamage.ByBlockHit.Enable ? ammoDef.AreaOfDamage.ByBlockHit.Radius : 0;

            if (AmmoModsFound && Overrides.DetonationDamage.HasValue)
                endOfLifeDamage = Math.Max(Overrides.DetonationDamage.Value, 0f);
            else
                endOfLifeDamage = ammoDef.AreaOfDamage.EndOfLife.Damage;

            if (AmmoModsFound && Overrides.DetonationRadius.HasValue)
                endOfLifeRadius = Math.Max(Overrides.DetonationRadius.Value, 0f);
            else
                endOfLifeRadius = ammoDef.AreaOfDamage.EndOfLife.Enable ? (float)ammoDef.AreaOfDamage.EndOfLife.Radius : 0;

            if (AmmoModsFound && Overrides.ByBlockHitMaxAbsorb.HasValue)
                aoeMaxAbsorb = Math.Max(Overrides.ByBlockHitMaxAbsorb.Value, 0f);
            else
                aoeMaxAbsorb = ammoDef.AreaOfDamage.ByBlockHit.MaxAbsorb > 0 ? ammoDef.AreaOfDamage.ByBlockHit.MaxAbsorb : 0;

            if (AmmoModsFound && Overrides.EndOfLifeMaxAbsorb.HasValue)
                detMaxAbsorb = Math.Max(Overrides.EndOfLifeMaxAbsorb.Value, 0f);
            else
                detMaxAbsorb = ammoDef.AreaOfDamage.EndOfLife.MaxAbsorb > 0 ? ammoDef.AreaOfDamage.EndOfLife.MaxAbsorb : 0;

            ewarEffectStrength = ammoDef.Ewar.Strength;
            ewarEffectSize = ammoDef.Ewar.Radius;
            largestHitSize = Math.Max(byBlockHitRadius, Math.Max(endOfLifeRadius, ewarEffectSize));

            eWar = ammoDef.Ewar.Enable;
            nonAntiSmart = !eWar || ewarType != EwarType.AntiSmart;
            eWarFieldTrigger = eWar && EwarField && ammoDef.Ewar.Field.TriggerRange > 0;
            minArmingTime = ammoDef.AreaOfDamage.EndOfLife.MinArmingTime;
            if (ammoDef.AreaOfDamage.ByBlockHit.Enable) byBlockHitDepth = ammoDef.AreaOfDamage.ByBlockHit.Depth <= 0 ? (float)ammoDef.AreaOfDamage.ByBlockHit.Radius : ammoDef.AreaOfDamage.ByBlockHit.Depth;
            else byBlockHitDepth = 0;
            if (ammoDef.AreaOfDamage.EndOfLife.Enable) endOfLifeDepth = ammoDef.AreaOfDamage.EndOfLife.Depth <= 0 ? (float)ammoDef.AreaOfDamage.EndOfLife.Radius : ammoDef.AreaOfDamage.EndOfLife.Depth;
            else endOfLifeDepth = 0;

            endOfLifeAoe = ammoDef.AreaOfDamage.EndOfLife.Enable;
        }

        private MyConcurrentPool<MyEntity> Models(AmmoDef ammoDef, WeaponDefinition wDef, out bool primeModel, out bool triggerModel, out string primeModelPath)
        {
            if (ammoDef.Ewar.Type > 0 && IsField) triggerModel = true;
            else triggerModel = false;
            primeModel = !string.IsNullOrEmpty(ammoDef.AmmoGraphics.ModelName);
            var vanillaModel = primeModel && !ammoDef.AmmoGraphics.ModelName.StartsWith(BackSlash);
            primeModelPath = vanillaModel ? ammoDef.AmmoGraphics.ModelName : primeModel ? wDef.ModPath + ammoDef.AmmoGraphics.ModelName : string.Empty;
            return primeModel ? new MyConcurrentPool<MyEntity>(64, PrimeEntityClear, 6400, PrimeEntityActivator) : null;
        }

        private void Beams(AmmoDef ammoDef, out bool isBeamWeapon, out bool virtualBeams, out bool rotateRealBeam, out bool convergeBeams, out bool oneHitParticle, out bool offsetEffect, out uint fakeVoxelHits)
        {
            isBeamWeapon = ammoDef.Beams.Enable && ammoDef.Trajectory.Guidance == TrajectoryDef.GuidanceType.None;
            virtualBeams = ammoDef.Beams.VirtualBeams && IsBeamWeapon;
            rotateRealBeam = ammoDef.Beams.RotateRealBeam && VirtualBeams;
            convergeBeams = !RotateRealBeam && ammoDef.Beams.ConvergeBeams && VirtualBeams;
            oneHitParticle = ammoDef.Beams.OneParticle && IsBeamWeapon;
            offsetEffect = ammoDef.AmmoGraphics.Lines.OffsetEffect.MaxOffset > 0.2 && ammoDef.AmmoGraphics.Lines.OffsetEffect.MinLength > 0.1;
            fakeVoxelHits = (uint) ammoDef.Beams.FakeVoxelHitTicks;
        }

        private void CollisionShape(AmmoDef ammoDef, out bool collisionIsLine, out double collisionSize, out double tracerLength)
        {
            var isLine = ammoDef.Shape.Shape == LineShape;
            var size = ammoDef.Shape.Diameter;

            if (IsBeamWeapon)
                tracerLength = MaxTrajectory;
            else tracerLength = ammoDef.AmmoGraphics.Lines.Tracer.Length > 0 ? ammoDef.AmmoGraphics.Lines.Tracer.Length : 0.1;

            if (size <= 0)
            {
                if (!isLine) isLine = true;
                size = 1;
            }
            else if (!isLine) size *= 0.5;
            collisionIsLine = isLine;
            collisionSize = size;
        }

        private void DamageScales(AmmoDef ammoDef, out bool damageScaling, out bool fallOffScaling, out bool armorScaling, out bool gridScaling, out bool customDamageScales, out Dictionary<MyDefinitionBase, float> customBlockDef, out bool selfDamage, out bool voxelDamage, out double healthHitModifer, out double voxelHitModifer, out int deformDelay, out float largeGridDmgScale, out float smallGridDmgScale)
        {
            var d = ammoDef.DamageScales;
            customBlockDef = null;
            customDamageScales = false;
            armorScaling = false;
            gridScaling = false;
            fallOffScaling = false;
            largeGridDmgScale = 0;
            smallGridDmgScale = 0;

            if (d.Custom.Types != null && d.Custom.Types.Length > 0)
            {
                foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
                    foreach (var customDef in d.Custom.Types)
                        if (customDef.Modifier >= 0 && def.Id.SubtypeId.String == customDef.SubTypeId)
                        {
                            if (customBlockDef == null) customBlockDef = new Dictionary<MyDefinitionBase, float>();
                            customBlockDef.Add(def, customDef.Modifier);
                            customDamageScales = customBlockDef.Count > 0;
                        }
            }

            damageScaling = FallOffMinMultiplier > 0 && !MyUtils.IsZero(FallOffMinMultiplier - 1) || d.MaxIntegrity > 0 || d.Armor.Armor >= 0 || d.Armor.NonArmor >= 0 || d.Armor.Heavy >= 0 || d.Armor.Light >= 0 || d.Grids.Large >= 0 || d.Grids.Small >= 0 || customDamageScales || ArmorCoreActive;

            if (damageScaling)
            {
                armorScaling = !ammoDef.NoGridOrArmorScaling && (d.Armor.Armor >= 0 || d.Armor.NonArmor >= 0 || d.Armor.Heavy >= 0 || d.Armor.Light >= 0);
                fallOffScaling = FallOffMinMultiplier > 0 && !MyUtils.IsZero(FallOffMinMultiplier - 1);
                gridScaling = !ammoDef.NoGridOrArmorScaling && (d.Grids.Large >= 0 || d.Grids.Small >= 0);
                largeGridDmgScale = d.Grids.Large;
                smallGridDmgScale = d.Grids.Small;
            }
            selfDamage = d.SelfDamage;
            voxelDamage = d.DamageVoxels;
            var healthHitModiferRaw = AmmoModsFound && Overrides.HealthHitModifier.HasValue ? Math.Max(Overrides.HealthHitModifier.Value, 0) : d.HealthHitModifier;
            healthHitModifer = healthHitModiferRaw > 0 ? healthHitModiferRaw : 1;
            voxelHitModifer = d.VoxelHitModifier > 0 ? d.VoxelHitModifier : 1;

            deformDelay = d.Deform.DeformDelay <= 0 ? 30 : d.Deform.DeformDelay;
        }

        private void Energy(WeaponSystem.AmmoType ammoPair, WeaponSystem system, WeaponDefinition wDef, out bool energyAmmo, out bool mustCharge, out bool reloadable, out float energyCost, out int energyMagSize, out float chargeSize, out bool burstMode, out bool shotReload, out float requiredPowerPerTick)
        {
            energyAmmo = ammoPair.AmmoDefinitionId.SubtypeId.String == "Energy" || ammoPair.AmmoDefinitionId.SubtypeId.String == string.Empty;
            mustCharge = (energyAmmo || IsHybrid);

            burstMode = wDef.HardPoint.Loading.ShotsInBurst > 0 && (energyAmmo || MagazineDef.Capacity >= wDef.HardPoint.Loading.ShotsInBurst);

            reloadable = !energyAmmo || mustCharge && system.WConst.ReloadTime > 0;

            shotReload = !burstMode && wDef.HardPoint.Loading.ShotsInBurst > 0 && wDef.HardPoint.Loading.DelayAfterBurst > 0;

            if (mustCharge)
            {
                var ewar = ammoPair.AmmoDef.Ewar.Enable;
                energyCost = AmmoModsFound && Overrides.EnergyCost.HasValue ? Math.Max(Overrides.EnergyCost.Value, 0f) : ammoPair.AmmoDef.EnergyCost;
                var shotEnergyCost = ewar ? energyCost * ammoPair.AmmoDef.Ewar.Strength : energyCost * BaseDamage;
                var shotsPerTick = system.WConst.RateOfFire / MyEngineConstants.UPDATE_STEPS_PER_MINUTE;
                var energyPerTick = shotEnergyCost * shotsPerTick;
                requiredPowerPerTick = (energyPerTick * wDef.HardPoint.Loading.BarrelsPerShot) * wDef.HardPoint.Loading.TrajectilesPerBarrel;

                var reloadTime = system.WConst.ReloadTime > 0 ? system.WConst.ReloadTime : 1;
                chargeSize = requiredPowerPerTick * reloadTime;
                var chargeCeil = (int)Math.Ceiling(requiredPowerPerTick * reloadTime);

                energyMagSize = ammoPair.AmmoDef.EnergyMagazineSize > 0 ? ammoPair.AmmoDef.EnergyMagazineSize : chargeCeil;
                return;
            }
            energyCost = 0;
            chargeSize = 0;
            energyMagSize = 0;
            requiredPowerPerTick = 0;
        }

        private void Sound(WeaponSystem.AmmoType ammo, WeaponSystem system, out bool hitSound, out MySoundPair hitSoundPair, out bool ammoTravelSound, out MySoundPair travelSoundPair, out bool shotSound, out MySoundPair shotSoundPair, 
            out bool detSound, out MySoundPair detSoundPair, out float hitSoundDistSqr, out float ammoTravelSoundDistSqr, out float ammoSoundMaxDistSqr, out float shotSoundDistSqr, out float detSoundDistSqr, out string rawShotSoundStr, 
            out bool voxelSound, out MySoundPair voxelSoundPair, out bool floatingSound, out MySoundPair floatingSoundPair, out bool playerSound, out MySoundPair playerSoundPair, out bool shieldSound, out MySoundPair shieldSoundPair)
        {
            var ammoDef = ammo.AmmoDef;
            var weaponShotSound = !string.IsNullOrEmpty(system.Values.HardPoint.Audio.FiringSound);
            var ammoShotSound = !string.IsNullOrEmpty(ammoDef.AmmoAudio.ShotSound);
            var useWeaponShotSound = !ammo.IsShrapnel && weaponShotSound && !ammoShotSound;


            rawShotSoundStr = useWeaponShotSound ? system.Values.HardPoint.Audio.FiringSound : ammoDef.AmmoAudio.ShotSound;

            hitSound = !string.IsNullOrEmpty(ammoDef.AmmoAudio.HitSound);
            hitSoundPair = hitSound ? new MySoundPair(ammoDef.AmmoAudio.HitSound, false) : null;


            ammoTravelSound = !string.IsNullOrEmpty(ammoDef.AmmoAudio.TravelSound);
            travelSoundPair = ammoTravelSound ? new MySoundPair(ammoDef.AmmoAudio.TravelSound, false) : null;
            
            shotSound = !string.IsNullOrEmpty(rawShotSoundStr);
            shotSoundPair = shotSound ? new MySoundPair(rawShotSoundStr, false) : null;

            detSound = !string.IsNullOrEmpty(DetSoundStr) && !ammoDef.AreaOfDamage.EndOfLife.NoSound;
            detSoundPair = detSound ? new MySoundPair(DetSoundStr, false) : null;


            var hitSoundStr = string.Concat(Arc, ammoDef.AmmoAudio.HitSound);
            var travelSoundStr = string.Concat(Arc, ammoDef.AmmoAudio.TravelSound);
            var shotSoundStr = string.Concat(Arc, rawShotSoundStr);
            var detSoundStr = string.Concat(Arc, DetSoundStr);

            hitSoundDistSqr = 0;
            ammoTravelSoundDistSqr = 0;
            ammoSoundMaxDistSqr = 0;
            shotSoundDistSqr = 0;
            detSoundDistSqr = 0;

            foreach (var def in Session.I.SoundDefinitions)
            {
                var id = def.Id.SubtypeId.String;
                if (hitSound && (id == hitSoundStr || id == ammoDef.AmmoAudio.HitSound))
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) hitSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (hitSoundDistSqr > ammoSoundMaxDistSqr) ammoSoundMaxDistSqr = hitSoundDistSqr;
                }
                else if (ammoTravelSound && (id == travelSoundStr || id == ammoDef.AmmoAudio.TravelSound))
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) ammoTravelSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (ammoTravelSoundDistSqr > ammoSoundMaxDistSqr) ammoSoundMaxDistSqr = ammoTravelSoundDistSqr;
                }
                else if (shotSound && (id == shotSoundStr || id == rawShotSoundStr))
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) shotSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (shotSoundDistSqr > ammoSoundMaxDistSqr) ammoSoundMaxDistSqr = shotSoundDistSqr;
                }
                else if (detSound && (id == detSoundStr || id == DetSoundStr))
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) detSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (detSoundDistSqr > ammoSoundMaxDistSqr) ammoSoundMaxDistSqr = detSoundDistSqr;
                }
            }

            voxelSound = !string.IsNullOrEmpty(ammoDef.AmmoAudio.VoxelHitSound);
            voxelSoundPair = voxelSound ? new MySoundPair(ammoDef.AmmoAudio.VoxelHitSound, false) : hitSound ? new MySoundPair(ammoDef.AmmoAudio.HitSound, false) : null;

            playerSound = !string.IsNullOrEmpty(ammoDef.AmmoAudio.PlayerHitSound);
            playerSoundPair = playerSound ? new MySoundPair(ammoDef.AmmoAudio.PlayerHitSound, false) : null;

            floatingSound = !string.IsNullOrEmpty(ammoDef.AmmoAudio.FloatingHitSound);
            floatingSoundPair = floatingSound ? new MySoundPair(ammoDef.AmmoAudio.FloatingHitSound, false) : null;

            shieldSound = !string.IsNullOrEmpty(ammoDef.AmmoAudio.ShieldHitSound);
            shieldSoundPair = shieldSound ? new MySoundPair(ammoDef.AmmoAudio.ShieldHitSound, false) : hitSound ? new MySoundPair(ammoDef.AmmoAudio.HitSound, false) :  null;
        }

        private MyEntity PrimeEntityActivator()
        {
            var ent = new MyEntity();
            ent.Init(null, ModelPath, null, null);
            ent.Render.CastShadows = false;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, false);
            return ent;
        }

        private static void PrimeEntityClear(MyEntity myEntity)
        {
            myEntity.PositionComp.SetWorldMatrix(ref MatrixD.Identity, null, false, false, false);
            myEntity.InScene = false;
            myEntity.Render.RemoveRenderObjects();
        }

        private void LoadModifiers(AmmoDef ammoDef, out AmmoOverride overrides, out bool modsFound, out float baseDamage, out float health, 
            out float gravityMultiplier, out float maxTrajectory, out float maxTrajectorySqr, out bool energyBaseDmg, 
            out bool energyAreaDmg, out bool energyDetDmg, out bool energyShieldDmg, out double shieldModifier, 
            out float fallOffDistance, out float fallOffMinMult, out float mass, out float shieldBypassRaw)
        {
            overrides = null;
            modsFound = false;

            baseDamage = Math.Max(ammoDef.BaseDamage, 0.000001f);
            health = ammoDef.Health;
            gravityMultiplier = ammoDef.Trajectory.GravityMultiplier;
            maxTrajectory = ammoDef.Trajectory.MaxTrajectory;
            maxTrajectorySqr = maxTrajectory * maxTrajectory;

            energyBaseDmg = ammoDef.DamageScales.DamageType.Base != DamageTypes.Damage.Kinetic;
            energyAreaDmg = ammoDef.DamageScales.DamageType.AreaEffect != DamageTypes.Damage.Kinetic;
            energyDetDmg = ammoDef.DamageScales.DamageType.Detonation != DamageTypes.Damage.Kinetic;
            energyShieldDmg = ammoDef.DamageScales.DamageType.Shield != DamageTypes.Damage.Kinetic;
            shieldModifier = ammoDef.DamageScales.Shields.Modifier < 0 ? 1 : ammoDef.DamageScales.Shields.Modifier;

            fallOffDistance = ammoDef.DamageScales.FallOff.Distance;
            fallOffMinMult = ammoDef.DamageScales.FallOff.MinMultipler;
            mass = ammoDef.Mass;
            shieldBypassRaw = ammoDef.DamageScales.Shields.BypassModifier;

            if (!Session.I.AmmoValuesMap.TryGetValue(ammoDef, out overrides) || overrides == null)
                return;

            modsFound = true;

            if (overrides.BaseDamage.HasValue) baseDamage = Math.Max(overrides.BaseDamage.Value, 0.000001f);
            if (overrides.Health.HasValue) health = Math.Max(overrides.Health.Value, 0);
            if (overrides.GravityMultiplier.HasValue) gravityMultiplier = Math.Max(overrides.GravityMultiplier.Value, 0f);
            if (overrides.MaxTrajectory.HasValue)
            {
                maxTrajectory = Math.Max(overrides.MaxTrajectory.Value, 0f);
                maxTrajectorySqr = maxTrajectory * maxTrajectory;
            }

            if (overrides.EnergyBaseDamage.HasValue) energyBaseDmg = overrides.EnergyBaseDamage.Value;
            if (overrides.EnergyAreaEffectDamage.HasValue) energyAreaDmg = overrides.EnergyAreaEffectDamage.Value;
            if (overrides.EnergyDetonationDamage.HasValue) energyDetDmg = overrides.EnergyDetonationDamage.Value;
            if (overrides.EnergyShieldDamage.HasValue) energyShieldDmg = overrides.EnergyShieldDamage.Value;
            if (overrides.ShieldModifier.HasValue) shieldModifier = overrides.ShieldModifier.Value < 0 ? 1 : overrides.ShieldModifier.Value;

            if (overrides.FallOffDistance.HasValue) fallOffDistance = Math.Max(overrides.FallOffDistance.Value, 0f);
            if (overrides.FallOffMinMultipler.HasValue) fallOffMinMult = Math.Max(overrides.FallOffMinMultipler.Value, 0f);
            if (overrides.Mass.HasValue) mass = Math.Max(overrides.Mass.Value, 0f);
            if (overrides.ShieldBypass.HasValue) shieldBypassRaw = Math.Max(overrides.ShieldBypass.Value, 0f);

        }

        //private void GetModifiableValues(AmmoDef ammoDef, out float baseDamage, out float health, out float gravityMultiplier, out float maxTrajectory, out float maxTrajectorySqr, out bool energyBaseDmg, out bool energyAreaDmg, out bool energyDetDmg, out bool energyShieldDmg, out double shieldModifier, out float fallOffDistance, out float fallOffMinMult, out float mass, out float shieldBypassRaw)
        //{
        //    baseDamage = AmmoModsFound && _modifierMap[BaseDmgStr].HasData() ? _modifierMap[BaseDmgStr].GetAsFloat : ammoDef.BaseDamage;

        //    if (baseDamage < 0.000001)
        //        baseDamage = 0.000001f;

        //    health = AmmoModsFound && _modifierMap[HealthStr].HasData() ? _modifierMap[HealthStr].GetAsFloat : ammoDef.Health;
        //    gravityMultiplier = AmmoModsFound && _modifierMap[GravityStr].HasData() ? _modifierMap[GravityStr].GetAsFloat : ammoDef.Trajectory.GravityMultiplier;
        //    maxTrajectory = AmmoModsFound && _modifierMap[MaxTrajStr].HasData() ? _modifierMap[MaxTrajStr].GetAsFloat : ammoDef.Trajectory.MaxTrajectory;
        //    maxTrajectorySqr = maxTrajectory * maxTrajectory;
        //    energyBaseDmg = AmmoModsFound && _modifierMap[EnergyBaseDmgStr].HasData() ? _modifierMap[EnergyBaseDmgStr].GetAsBool : ammoDef.DamageScales.DamageType.Base != DamageTypes.Damage.Kinetic;
        //    energyAreaDmg = AmmoModsFound && _modifierMap[EnergyAreaDmgStr].HasData() ? _modifierMap[EnergyAreaDmgStr].GetAsBool : ammoDef.DamageScales.DamageType.AreaEffect != DamageTypes.Damage.Kinetic;
        //    energyDetDmg = AmmoModsFound && _modifierMap[EnergyDetDmgStr].HasData() ? _modifierMap[EnergyDetDmgStr].GetAsBool : ammoDef.DamageScales.DamageType.Detonation != DamageTypes.Damage.Kinetic;
        //    energyShieldDmg = AmmoModsFound && _modifierMap[EnergyShieldDmgStr].HasData() ? _modifierMap[EnergyShieldDmgStr].GetAsBool : ammoDef.DamageScales.DamageType.Shield != DamageTypes.Damage.Kinetic;

        //    var givenShieldModifier = AmmoModsFound && _modifierMap[ShieldModStr].HasData() ? _modifierMap[ShieldModStr].GetAsDouble : ammoDef.DamageScales.Shields.Modifier;
        //    shieldModifier = givenShieldModifier < 0 ? 1 : givenShieldModifier;

        //    fallOffDistance = AmmoModsFound && _modifierMap[FallOffDistanceStr].HasData() ? _modifierMap[FallOffDistanceStr].GetAsFloat : ammoDef.DamageScales.FallOff.Distance;
        //    fallOffMinMult = AmmoModsFound && _modifierMap[FallOffMinMultStr].HasData() ? _modifierMap[FallOffMinMultStr].GetAsFloat : ammoDef.DamageScales.FallOff.MinMultipler;

        //    mass = AmmoModsFound && _modifierMap[MassStr].HasData() ? _modifierMap[MassStr].GetAsFloat : ammoDef.Mass;

        //    shieldBypassRaw = AmmoModsFound && _modifierMap[ShieldBypassStr].HasData() ? _modifierMap[ShieldBypassStr].GetAsFloat : ammoDef.DamageScales.Shields.BypassModifier;
        //}


        private int mexLogLevel = 0;
        private void GetPeakDps(WeaponSystem.AmmoType ammoDef, WeaponSystem system, WeaponDefinition wDef, out float peakDps, out float effectiveDps, out float dpsWoInaccuracy, out float shotsPerSec, out float realShotsPerSec, out float baseDps, out float areaDps, out float detDps, out float realShotsPerMin)
        {
            var s = system;
            var a = ammoDef.AmmoDef;
            var hasShrapnel = HasFragment;
            var l = wDef.HardPoint.Loading;


            if (mexLogLevel >= 1) Log.Line("-----");
            if (mexLogLevel >= 1) Log.Line($"Name = {s.PartName}"); //a.EnergyMagazineSize
            if (mexLogLevel >= 2) Log.Line($"EnergyMag = {a.EnergyMagazineSize}");

            var baselineRange = a.Trajectory.MaxTrajectory * 0.5f; // 1000; ba

            //Inaccuracy
            var inaccuracyRadius = Math.Tan(system.WConst.DeviateShotAngleRads) * baselineRange;
            var targetRadius = 10;
            var inaccuracyScore = ((Math.PI * targetRadius * targetRadius) / (Math.PI * inaccuracyRadius * inaccuracyRadius));
            inaccuracyScore = inaccuracyScore > 1 ? 1 : inaccuracyScore;
            inaccuracyScore = system.WConst.DeviateShotAngleRads <= 0 ? 1 : inaccuracyScore;

            //EffectiveRange
            var effectiveRangeScore = 1;

            //TrackingScore
            var coverageScore = ((Math.Abs(s.MinElevation) + (float)Math.Abs(s.MaxElevation)) * ((Math.Abs(s.MinAzimuth) + Math.Abs(s.MaxAzimuth)))) / (360 * 90);
            coverageScore = coverageScore > 1 ? 1 : coverageScore;

            var speedEl = (wDef.HardPoint.HardWare.ElevateRate * (180 / Math.PI)) * 60;
            var coverageElevateScore = speedEl / (180d / 5d);
            var speedAz = (wDef.HardPoint.HardWare.RotateRate * (180 / Math.PI)) * 60;
            var coverageRotateScore = speedAz / (180d / 5d);

            var trackingScore = (coverageScore + ((coverageRotateScore + coverageElevateScore) * 0.5d)) * 0.5d;
            //if a sorter weapon use several barrels with only elevation or rotation the score should be uneffected since its designer to work
            if (MyUtils.IsZero(Math.Abs(s.MinElevation) + (float)Math.Abs(s.MaxElevation)))
                trackingScore = (coverageScore + ((coverageRotateScore + 1) * 0.5d)) * 0.5d;

            if ((Math.Abs(s.MinAzimuth) + Math.Abs(s.MaxAzimuth)) == 0)
                trackingScore = (coverageScore + ((coverageElevateScore + 1) * 0.5d)) * 0.5d;

            if (MyUtils.IsZero(Math.Abs(s.MinElevation) + (float)Math.Abs(s.MaxElevation) + (Math.Abs(s.MinAzimuth) + Math.Abs(s.MaxAzimuth))))
                trackingScore = 1.0d;

            trackingScore = trackingScore > 1 ? 1 : trackingScore;
            trackingScore = 1;

            //FinalScore
            var effectiveModifier = (effectiveRangeScore * inaccuracyScore) * trackingScore;

            // static weapons get a tracking score of 50%
            if (MyUtils.IsZero(Math.Abs(Math.Abs(s.MinElevation) + (float)Math.Abs(s.MaxElevation))) || Math.Abs(s.MinAzimuth) + Math.Abs(s.MaxAzimuth) == 0)
                trackingScore = 0.5f;

            //Logs for effective dps
            if (mexLogLevel >= 2) Log.Line($"newInaccuracyRadius = {inaccuracyRadius}");
            if (mexLogLevel >= 2) Log.Line($"DeviationAngle = { system.WConst.DeviateShotAngleRads}");
            if (mexLogLevel >= 1) Log.Line($"InaccuracyScore = {inaccuracyScore}");
            if (mexLogLevel >= 1) Log.Line($"effectiveRangeScore = {effectiveRangeScore}");
            if (mexLogLevel >= 2) Log.Line($"coverageScore = {coverageScore}");
            if (mexLogLevel >= 2) Log.Line($"ElevateRate = {(wDef.HardPoint.HardWare.ElevateRate * (180 / Math.PI))}");
            if (mexLogLevel >= 2) Log.Line($"coverageElevate = {speedEl}");
            if (mexLogLevel >= 2) Log.Line($"coverageElevateScore = {coverageElevateScore}");
            if (mexLogLevel >= 2) Log.Line($"RotateRate = {(wDef.HardPoint.HardWare.RotateRate * (180 / Math.PI))}");
            if (mexLogLevel >= 2) Log.Line($"coverageRotate = {speedAz}");
            if (mexLogLevel >= 2) Log.Line($"coverageRotateScore = {coverageRotateScore}");

            if (mexLogLevel >= 2) Log.Line($"CoverageScore = {(coverageScore + ((coverageRotateScore + coverageElevateScore) * 0.5d)) * 0.5d}");
            if (mexLogLevel >= 1) Log.Line($"trackingScore = {trackingScore}");
            if (mexLogLevel >= 1) Log.Line($"effectiveModifier = {effectiveModifier}");

            //DPS Calc
            if (!EnergyAmmo && MagazineSize > 0 || IsHybrid)
            {
                realShotsPerSec = GetShotsPerSecond(MagazineSize, wDef.HardPoint.Loading.MagsToLoad, s.WConst.RateOfFire, s.WConst.ReloadTime, s.BarrelsPerShot, l.TrajectilesPerBarrel, l.ShotsInBurst, l.DelayAfterBurst);
            }
            else if (EnergyAmmo && a.EnergyMagazineSize > 0)
            {
                realShotsPerSec = GetShotsPerSecond(a.EnergyMagazineSize, 1, s.WConst.RateOfFire, s.WConst.ReloadTime, s.BarrelsPerShot, l.TrajectilesPerBarrel, l.ShotsInBurst, l.DelayAfterBurst);
            }
            else
            {
                realShotsPerSec = GetShotsPerSecond(1, 1, s.WConst.RateOfFire, 0, s.BarrelsPerShot, l.TrajectilesPerBarrel, s.ShotsPerBurst, l.DelayAfterBurst);
            }
            shotsPerSec = realShotsPerSec;
            var shotsPerSecPreHeat = shotsPerSec;

            if (s.WConst.HeatPerShot * HeatModifier > 0)
            {
                var heatGenPerSec = (s.WConst.HeatPerShot * HeatModifier * realShotsPerSec) - system.WConst.HeatSinkRate; //heat - cooldown
                if (heatGenPerSec > 0)
                {

                    var safeToOverheat = (l.MaxHeat - (l.MaxHeat * l.Cooldown)) / heatGenPerSec;
                    var cooldownTime = (l.MaxHeat - (l.MaxHeat * l.Cooldown)) / system.WConst.HeatSinkRate;
                    var timeHeatCycle = (safeToOverheat + cooldownTime);

                    realShotsPerSec = (float) ((safeToOverheat / timeHeatCycle) * realShotsPerSec);

                    if ((mexLogLevel >= 1))
                    {
                        Log.Line($"Name = {s.PartName}");
                        Log.Line($"HeatPerShot = {s.WConst.HeatPerShot * HeatModifier}");
                        Log.Line($"HeatGenPerSec = {heatGenPerSec}");

                        Log.Line($"WepCoolDown = {l.Cooldown}");

                        Log.Line($"safeToOverheat = {safeToOverheat}");
                        Log.Line($"cooldownTime = {cooldownTime}");


                        Log.Line($"timeHeatCycle = {timeHeatCycle}s");

                        Log.Line($"realShotsPerSec wHeat = {realShotsPerSec}");
                    }

                }

            }
            var avgArmorModifier = GetAverageArmorModifier(a.DamageScales.Armor);

            realShotsPerMin = (realShotsPerSec * 60);
            baseDps = BaseDamage * realShotsPerSec * avgArmorModifier;
            areaDps = 0; //TODO: Add back in some way
            detDps = (GetDetDmg(a) * realShotsPerSec) * avgArmorModifier;

            if (hasShrapnel)//Add damage from fragments
            {
                var sAmmo = wDef.Ammos[FragmentId];
                var fragments = a.Fragment.Fragments;

                Vector2 FragDmg = new Vector2(0, 0);
                Vector2 patternDmg = new Vector2(0, 0);

                FragDmg = FragDamageLoopCheck(wDef, realShotsPerSec, FragDmg, 0, a, a.Fragment.Fragments);

                //TODO: fix when fragDmg is split
                baseDps += FragDmg.X;
                detDps += FragDmg.Y;
            }



            if (a.Pattern.Enable || a.Pattern.Mode != AmmoDef.PatternDef.PatternModes.Never) //make into function
            {
                Vector2 totalPatternDamage = new Vector2();
                foreach (var patternName in a.Pattern.Patterns)
                {
                    for (int j = 0; j < wDef.Ammos.Length; j++)
                    {
                        var patternAmmo = wDef.Ammos[j];
                        if (patternAmmo.AmmoRound.Equals(patternName))
                        {


                            Vector2 tempDmg = new Vector2();
                            Vector2 tempFragDmg = new Vector2();
                            tempDmg.X += patternAmmo.BaseDamage;
                            tempDmg.Y += GetDetDmg(patternAmmo);
                            if (patternAmmo.Fragment.Fragments != 0)
                            {
                                tempFragDmg += FragDamageLoopCheck(wDef, 1, tempFragDmg, 0, a, a.Fragment.Fragments);
                            }

                            totalPatternDamage += tempFragDmg + tempDmg;

                        }

                    }

                }

                var numPatterns = a.Pattern.Patterns.Length;
                var stepModifier = a.Pattern.PatternSteps;

                totalPatternDamage *= realShotsPerSec * avgArmorModifier; //convert to DPS of combined patterns

                if (numPatterns != a.Pattern.PatternSteps) stepModifier = a.Pattern.PatternSteps == 0 ? 1 : a.Pattern.PatternSteps;

                if (!a.Pattern.SkipParent)
                {
                    numPatterns++;

                    totalPatternDamage.X += baseDps;
                    totalPatternDamage.Y += detDps;

                    totalPatternDamage /= numPatterns; //get average dps of all patterns and base ammo
                    totalPatternDamage *= stepModifier; //Multiply with how many

                    baseDps = totalPatternDamage.X;
                    areaDps = 0; //TODO: Add back in some way
                    detDps = totalPatternDamage.Y;

                }
                else
                {

                    totalPatternDamage /= numPatterns;
                    totalPatternDamage *= stepModifier;

                    baseDps = totalPatternDamage.X;
                    areaDps = 0; //TODO: Add back in some way
                    detDps = totalPatternDamage.Y;

                }
            }

            if (mexLogLevel >= 1) Log.Line($"Got Area damage={ByBlockHitDamage} det={GetDetDmg(a)} @ {realShotsPerSec} areadps={areaDps} basedps={baseDps} detdps={detDps}");

            peakDps = (baseDps + areaDps + detDps);
            effectiveDps = (float)(peakDps * effectiveModifier);
            dpsWoInaccuracy = (float)(effectiveModifier / inaccuracyScore) * peakDps;

            if (mexLogLevel >= 1) Log.Line($"peakDps= {peakDps}");

            if (mexLogLevel >= 1) Log.Line($"Effective DPS(mult) = {effectiveDps}");

            if (wDef.HardPoint.Other.Debug && a.HardPointUsable)
            {

                Log.Line($"[========================]");
                Log.Line($":::::[{wDef.HardPoint.PartName}]:::::");
                Log.Line($"AmmoMagazine: {a.AmmoMagazine}");
                Log.Line($"AmmoRound: {a.AmmoRound}");
                Log.Line($"InaccuracyScore: {Math.Round(inaccuracyScore * 100, 2)}% | ShotAngle: {wDef.HardPoint.DeviateShotAngle}  @: { baselineRange}m vs { targetRadius}m Circle");
                Log.Line($"--------------------------");
                Log.Line($"Shots per second(w/Heat): {Math.Round(shotsPerSecPreHeat, 2)} ({Math.Round(realShotsPerSec, 2)})");
                Log.Line($"Peak DPS: {Math.Round(peakDps)}");
                Log.Line($"Effective DPS: {Math.Round(effectiveDps)} | without Inaccuracy: {Math.Round(dpsWoInaccuracy)}");
                Log.Line($"Base Damage DPS: {Math.Round(baseDps)}");
                Log.Line($"Area Damage DPS: {Math.Round(areaDps)}");
                Log.Line($"Explosive Dmg DPS: {Math.Round(detDps)}");
                Log.Line($"[=========== Ammo End =============]");



            }
        }

        private Vector2 FragDamageLoopCheck(WeaponDefinition wDef, float shotsPerSec, Vector2 FragDmg, int pastI, AmmoDef parentAmmo, int parentFragments)
        {
            pastI++; //max fragment depth

            for (int j = 0; j < wDef.Ammos.Length; j++)
            {
                var fragmentAmmo = wDef.Ammos[j];
                if (fragmentAmmo.AmmoRound.Equals(parentAmmo.Fragment.AmmoRound) && pastI < 10)
                {
                    var tempDmg = GetShrapnelDamage(fragmentAmmo, parentFragments, shotsPerSec, parentFragments);
                    var fragFrags = 1.0f;
                    if (parentAmmo.Fragment.Fragments > 0) fragFrags = parentAmmo.Fragment.Fragments;
                    if (parentAmmo.Fragment.TimedSpawns.Enable)
                    {
                        var b = parentAmmo.Fragment.TimedSpawns;

                        float cycleTime = (b.Interval * ((b.GroupSize > 0 ? b.GroupSize : 1) - (b.GroupDelay > 0 ? 1 : 0))) + b.GroupDelay;
                        tempDmg *= (1.0f / ((cycleTime / 60))) * (b.GroupSize > 0 ? b.GroupSize : 1);

                        fragFrags = (1.0f / ((cycleTime / 60))) * b.GroupSize;
                    }



                    FragDmg += tempDmg;
                    parentFragments *= fragmentAmmo.Fragment.Fragments;
                    FragDmg = FragDamageLoopCheck(wDef, shotsPerSec, FragDmg, pastI, fragmentAmmo, parentFragments);


                }
            }

            return FragDmg;
        }

        private Vector2 GetShrapnelDamage(AmmoDef fAmmo, int frags, float sps, int parentFragments)
        {
            Vector2 fragDmg = new Vector2(0, 0);

            fragDmg.X += (fAmmo.BaseDamage * frags) * sps;
            //fragDmg += 0;
            fragDmg.Y += (GetDetDmg(fAmmo) * frags) * sps;
            float avgArmorModifier = GetAverageArmorModifier(fAmmo.DamageScales.Armor);

            fragDmg *= avgArmorModifier;

            return fragDmg;
        }

        private static float GetAverageArmorModifier(AmmoDef.DamageScaleDef.ArmorDef armor)
        {
            var avgArmorModifier = 0.0f;
            if (armor.Heavy < 0) { avgArmorModifier += 1.0f; }
            else { avgArmorModifier += armor.Heavy; }
            if (armor.Light < 0) { avgArmorModifier += 1.0f; }
            else { avgArmorModifier += armor.Light; }
            if (armor.Armor < 0) { avgArmorModifier += 1.0f; }
            else { avgArmorModifier += armor.Armor; }
            if (armor.NonArmor < 0) { avgArmorModifier += 1.0f; }
            else { avgArmorModifier += armor.NonArmor; }

            avgArmorModifier *= 0.25f;
            return avgArmorModifier;
        }

        private float GetShotsPerSecond(int magCapacity, int magPerReload, int rof, int reloadTime, int barrelsPerShot, int trajectilesPerBarrel, int shotsInBurst, int delayAfterBurst)
        {
            if (true) //WHy is this required ;_;
            {
                if (magPerReload < 1) magPerReload = 1;
                var reloadsPerRoF = rof / ((magCapacity * magPerReload) / (float)barrelsPerShot);
                var burstsPerRoF = shotsInBurst == 0 ? 0 : rof / (float)shotsInBurst;
                var ticksReloading = reloadsPerRoF * reloadTime;

                var ticksDelaying = burstsPerRoF * delayAfterBurst;

                if (mexLogLevel > 0) Log.Line($"burstsPerRof={burstsPerRoF} reloadsPerRof={reloadsPerRoF} ticksReloading={ticksReloading} ticksDelaying={ticksDelaying}");
                float shotsPerSecond = rof / (60f + (ticksReloading / 60) + (ticksDelaying / 60));
            }

            var totMagCap = magCapacity * magPerReload;

            // How many times will the weapon shoot per magazine
            var shotsPerMagazine = totMagCap == 1 ? 0 : (Math.Ceiling((float)totMagCap / barrelsPerShot) - 1);

            // How many bursts per magazine
            var burstPerMagazine = shotsInBurst == 0 ? 0 : Math.Ceiling(((float)totMagCap / (float)shotsInBurst) - 1); // how many bursts per magazine

            //Case of no reload time
            if (reloadTime == 0)
            {
                shotsPerMagazine = totMagCap == 1 ? 0 : (Math.Ceiling((float)totMagCap / barrelsPerShot));
                burstPerMagazine = shotsInBurst == 0 ? 0 : Math.Ceiling(((float)totMagCap / (float)shotsInBurst));
            }

            //in tick - time spent shooting magazine
            var timeShots = shotsPerMagazine == 0 ? 0 : shotsPerMagazine * ((float)3600 / rof);
            // in tick - time spent on burst
            var timeBurst = burstPerMagazine == 0 ? 0 : burstPerMagazine * ((float)delayAfterBurst);
            // total time per mag
            var timePerCycle = timeShots + timeBurst + reloadTime; //add delayed fire

            //if 0 its a non magazine weapon so a cycle will be base on rof
            timePerCycle = timePerCycle == 0 ? ((float)3600 / rof) : timePerCycle;

            //this part might be shit
            timePerCycle = timePerCycle < ((float)3600 / rof) ? ((float)3600 / rof) : timePerCycle;

            // Convert to seconds
            timePerCycle = (float)timePerCycle / 60f;

            //Shots per cycle
            var shotsPerSecondV2 = (float)timePerCycle / (totMagCap);
            //Shots per second
            shotsPerSecondV2 = 1.0f / shotsPerSecondV2;

            return shotsPerSecondV2 * trajectilesPerBarrel;
        }


        private float GetDetDmg(AmmoDef a)
        {
            var dmgOut = 0.0d;
            var dmgByBlockHit = a.AreaOfDamage.ByBlockHit;
            var dmgEndOfLife = a.AreaOfDamage.EndOfLife;


            if (dmgByBlockHit.Enable)
            {
                if (mexLogLevel >= 1) Log.Line($"ByBlockHit = {dmgByBlockHit.Falloff.ToString()}");

                dmgOut += dmgByBlockHit.Damage * GetFalloffModifier(dmgByBlockHit.Falloff.ToString(), (float)dmgByBlockHit.Radius); ;

            };

            if (dmgEndOfLife.Enable)
            {
                if (mexLogLevel >= 1) Log.Line($"EndOffLife = {dmgEndOfLife.Falloff.ToString()}");
                dmgOut += dmgEndOfLife.Damage * GetFalloffModifier(dmgEndOfLife.Falloff.ToString(), (float)dmgEndOfLife.Radius); ;
            };
            if (mexLogLevel >= 1) Log.Line($"dmgOut = {dmgOut}");
            return (float)dmgOut;
        }

        private static double GetFalloffModifier(string falloffType, float radius)
        {
            double falloffModifier;
            //Sphere
            double blocksHit = Math.Round(((4d / 3d) * Math.PI * Math.Pow(radius, 3d)) / Math.Pow(2.5d, 3d)) / 2d;
            //Pyramid


            switch (falloffType)
            {
                case "NoFalloff":
                    falloffModifier = blocksHit * 1.0d;
                    break;
                case "Linear":
                    falloffModifier = blocksHit * 0.55d;
                    break;
                case "Curve":
                    falloffModifier = blocksHit * 0.81d;
                    break;
                case "InvCurve":
                    falloffModifier = blocksHit * 0.39d;
                    break;
                case "Squeeze":
                    falloffModifier = blocksHit * 0.22d;
                    break;
                case "Exponential":
                    falloffModifier = blocksHit * 0.29d;
                    break;
                default:
                    falloffModifier = 1;
                    break;
            }

            return falloffModifier;
        }

    }

    public class ApproachConstants
    {
        public readonly int Index;
        public readonly MySoundPair SoundPair;
        public readonly MyConcurrentPool<MyEntity> ModelPool;
        public readonly TrajectoryDef.ApproachDef Definition;
        public readonly string ModelPath;
        public readonly bool AlternateTravelSound;
        public readonly bool AlternateTravelParticle;
        public readonly bool AlternateModel;
        public readonly bool StartParticle;
        public readonly bool EndAnd;
        public readonly bool StartAnd;
        public readonly bool NoSpawns;
        public readonly bool LeadRotateElevatePositionB;
        public readonly bool LeadRotateElevatePositionC;
        public readonly bool NoElevationLead;
        public readonly double ModFutureStep;
        public readonly TrajectoryDef.ApproachDef.FwdRelativeTo Forward;
        public readonly TrajectoryDef.ApproachDef.UpRelativeTo Up;
        public readonly TrajectoryDef.ApproachDef.RelativeTo PositionB;
        public readonly TrajectoryDef.ApproachDef.RelativeTo PositionC;
        public readonly TrajectoryDef.ApproachDef.RelativeTo Elevation;
        public readonly TrajectoryDef.ApproachDef.Conditions StartCon1;
        public readonly TrajectoryDef.ApproachDef.Conditions StartCon2;
        public readonly TrajectoryDef.ApproachDef.Conditions EndCon1;
        public readonly TrajectoryDef.ApproachDef.Conditions EndCon2;
        public readonly TrajectoryDef.ApproachDef.Conditions EndCon3;
        public readonly bool AdjustForward;
        public readonly bool AdjustUp;
        public readonly bool DisableAvoidance;
        public readonly bool AdjustPositionB;
        public readonly bool AdjustPositionC;
        public readonly bool Orbit;
        public readonly bool CanExpireOnceStarted;
        public readonly bool PushLeadByTravelDistance;
        public readonly bool IgnoreAntiSmart;
        public readonly bool ModAngleOffset;
        public readonly bool HasAngleOffset;
        public readonly bool ModelRotate;
        public readonly bool TrajectoryRelativeToB;
        public readonly bool ElevationRelatveToC;
        public readonly bool ToggleIngoreVoxels;
        public readonly bool SelfAvoidance;
        public readonly bool TargetAvoidance;
        public readonly bool SelfPhasing;
        public readonly bool SwapNavigationType;
        public readonly double OrbitRadius;
        public readonly double AngleOffset;
        public readonly double DesiredElevation;
        public readonly double OffsetMinRadius;
        public readonly double OffsetMaxRadius;

        public readonly double TrackingDistance;
        public readonly double LeadDistance;
        public readonly double AccelMulti;
        public readonly double SpeedCapMulti;

        public readonly double Start1Value;
        public readonly double Start2Value;
        public readonly double End1Value;
        public readonly double End2Value;
        public readonly double End3Value;
        public readonly double ElevationTolerance;

        public readonly int OffsetTime;
        public readonly int StoredStartId;
        public readonly int StoredEndId;
        public readonly int ModelRotateTime;
        public ApproachConstants(WeaponSystem.AmmoType ammo, int index, WeaponDefinition wDef)
        {
            var def = ammo.AmmoDef.Trajectory.Approaches[index];
            Index = index;
            Definition = def;
            AlternateTravelSound = !string.IsNullOrEmpty(def.AlternateSound);
            AlternateTravelParticle = !string.IsNullOrEmpty(def.AlternateParticle.Name);
            StartParticle = !string.IsNullOrEmpty(def.StartParticle.Name);
            AlternateModel = !string.IsNullOrEmpty(def.AlternateModel);
            NoSpawns = def.NoTimedSpawns;
            ModAngleOffset = !MyUtils.IsZero(def.AngleVariance.Start) || !MyUtils.IsZero(def.AngleVariance.End);
            HasAngleOffset = ModAngleOffset || !MyUtils.IsZero(def.AngleOffset);
            LeadRotateElevatePositionB = def.LeadRotateElevatePositionB;
            LeadRotateElevatePositionC = def.LeadRotateElevatePositionC;
            NoElevationLead = def.NoElevationLead;
            IgnoreAntiSmart = def.IgnoreAntiSmart;
            ModelRotate = def.ModelRotateTime > 0;
            ToggleIngoreVoxels = def.ToggleIngoreVoxels;
            SelfAvoidance = def.SelfAvoidance;
            TargetAvoidance = def.TargetAvoidance;
            SelfPhasing = def.SelfPhasing;
            SwapNavigationType = def.SwapNavigationType;
            TrajectoryRelativeToB = def.TrajectoryRelativeToB;
            ElevationRelatveToC = def.ElevationRelativeToC;
            Forward = def.Forward;
            Up = def.Up;
            PositionB = def.PositionB;
            PositionC = def.PositionC;
            Elevation = def.Elevation;
            StartCon1 = def.StartCondition1;
            StartCon2 = def.StartCondition2;
            EndCon1 = def.EndCondition1;
            EndCon2 = def.EndCondition2;
            EndCon3 = def.EndCondition3;
            AdjustForward = def.AdjustForward;
            AdjustUp = def.AdjustUp;
            Orbit = def.Orbit;
            CanExpireOnceStarted = def.CanExpireOnceStarted;
            PushLeadByTravelDistance = def.PushLeadByTravelDistance;
            DisableAvoidance = def.DisableAvoidance;
            AdjustPositionB = def.AdjustPositionB;
            AdjustPositionC = def.AdjustPositionC;
            OrbitRadius = def.OrbitRadius;
            AngleOffset = def.AngleOffset;
            DesiredElevation = def.DesiredElevation;
            OffsetMinRadius = def.OffsetMinRadius;
            OffsetMaxRadius = def.OffsetMaxRadius;
            
            OffsetTime = def.OffsetTime;
            StoredStartId = def.StoredStartId;
            StoredEndId = def.StoredEndId;
            ModelRotateTime = def.ModelRotateTime;
            Start1Value = def.Start1Value;
            Start2Value = def.Start2Value;
            End1Value = def.End1Value;
            End2Value = def.End2Value;
            End3Value = def.End3Value;
            TrackingDistance = def.TrackingDistance;
            LeadDistance = def.LeadDistance;
            AccelMulti = def.AccelMulti;
            SpeedCapMulti = def.SpeedCapMulti;

            ElevationTolerance = def.ElevationTolerance;
            if (AlternateModel)
            {
                ModelPath = wDef.ModPath + def.AlternateModel;
            }

            if (AlternateTravelSound)
            {
                SoundPair = new MySoundPair(def.AlternateSound);
            }

            ModelPool = AlternateModel ? new MyConcurrentPool<MyEntity>(64, AlternateEntityClear, 640, AlternateEntityActivator) : null;

            var stepConst = MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            var desiredSpeed = ammo.AmmoDef.Trajectory.DesiredSpeed;
            var accel = ammo.AmmoDef.Trajectory.AccelPerSec;

            var desiredSpeedStep = desiredSpeed * stepConst;
            var maxStepLimit = accel * def.AccelMulti > 0 ? def.AccelMulti : accel;
            var speedStepLimit = def.SpeedCapMulti * desiredSpeedStep;
            var futureStepLimit = maxStepLimit <= speedStepLimit ? maxStepLimit : speedStepLimit;

            ModFutureStep = futureStepLimit;
            GetOperators(def, out StartAnd, out EndAnd);
        }

        private static void GetOperators(TrajectoryDef.ApproachDef def, out bool startAnd, out bool endAnd)
        {
            switch (def.Operators)
            {
                case TrajectoryDef.ApproachDef.ConditionOperators.StartEnd_And:
                    startAnd = true;
                    endAnd = true;
                    return;
                case TrajectoryDef.ApproachDef.ConditionOperators.StartEnd_Or:
                    startAnd = false;
                    endAnd = false;
                    return;
                case TrajectoryDef.ApproachDef.ConditionOperators.StartAnd_EndOr:
                    startAnd = true;
                    endAnd = false;
                    return;
                case TrajectoryDef.ApproachDef.ConditionOperators.StartOr_EndAnd:
                    startAnd = false;
                    endAnd = true;
                    return;
                default:
                    startAnd = true;
                    endAnd = true;
                    break;
            }
        }

        public int GetRestartId(ProInfo info, bool end1, bool end2, bool end3)
        {
            var array = Definition.RestartList;
            
            var rngSelectedId = -1;
            var lowestRuns = int.MaxValue;
            var runsSelectedId = -1;
            float highestRoll = float.MinValue;
            var aStorageArray = info.Storage.ApproachInfo.Storage;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    var runCount = aStorageArray[item.ApproachId].RunCount;
                    
                    if (runCount >= item.MaxRuns && item.MaxRuns > 0)
                        continue;

                    var mod1Enabled = end1 && !MyUtils.IsZero(item.End1WeightMod);
                    var mod2Enabled = end2 && !MyUtils.IsZero(item.End2WeightMod);
                    var mod3Enabled = end3 && !MyUtils.IsZero(item.End3WeightMod);

                    float rng;
                    if (mod1Enabled && mod2Enabled && mod3Enabled)
                    {
                        var rng1 = (float)info.Random.Range(item.Weight.Start * item.End1WeightMod, item.Weight.End * item.End1WeightMod);
                        var rng2 = (float)info.Random.Range(item.Weight.Start * item.End2WeightMod, item.Weight.End * item.End2WeightMod);
                        var rng3 = (float)info.Random.Range(item.Weight.Start * item.End3WeightMod, item.Weight.End * item.End3WeightMod);
                        rng = Math.Max(rng1, Math.Max(rng2, rng3));
                    }
                    else if (mod1Enabled && mod3Enabled)
                    {
                        var rng1 = (float)info.Random.Range(item.Weight.Start * item.End1WeightMod, item.Weight.End * item.End1WeightMod);
                        var rng3 = (float)info.Random.Range(item.Weight.Start * item.End3WeightMod, item.Weight.End * item.End3WeightMod);
                        rng = Math.Max(rng1, rng3);
                    }
                    else if (mod2Enabled && mod3Enabled)
                    {
                        var rng2 = (float)info.Random.Range(item.Weight.Start * item.End2WeightMod, item.Weight.End * item.End2WeightMod);
                        var rng3 = (float)info.Random.Range(item.Weight.Start * item.End3WeightMod, item.Weight.End * item.End3WeightMod);
                        rng = Math.Max(rng2, rng3);
                    }
                    else if (mod1Enabled && mod2Enabled) {
                        var rng1 = (float)info.Random.Range(item.Weight.Start * item.End1WeightMod, item.Weight.End * item.End1WeightMod);
                        var rng2 = (float)info.Random.Range(item.Weight.Start * item.End2WeightMod, item.Weight.End * item.End2WeightMod);
                        rng = Math.Max(rng1, rng2);
                    }
                    else if (mod1Enabled)
                        rng = (float) info.Random.Range(item.Weight.Start * item.End1WeightMod, item.Weight.End * item.End1WeightMod);
                    else if (mod2Enabled)
                        rng = (float)info.Random.Range(item.Weight.Start * item.End2WeightMod, item.Weight.End * item.End2WeightMod);
                    else if (mod3Enabled)
                        rng = (float)info.Random.Range(item.Weight.Start * item.End3WeightMod, item.Weight.End * item.End3WeightMod);
                    else 
                        rng = info.Random.Range(item.Weight.Start, item.Weight.End);

                    if (rng > highestRoll)
                    {
                        highestRoll = rng;
                        rngSelectedId = item.ApproachId;
                    }
                    
                    if (MyUtils.IsZero(rng) && runCount < lowestRuns)
                    {
                        lowestRuns = runCount;
                        runsSelectedId = item.ApproachId;
                    }
                }
            }
            else
                rngSelectedId = Definition.OnRestartRevertTo;

            var selected = !MyUtils.IsZero(highestRoll) ? rngSelectedId : runsSelectedId;

            return selected;
        }

        public void Clean()
        {
            ModelPool?.Clean();
        }

        private MyEntity AlternateEntityActivator()
        {
            var ent = new MyEntity();
            ent.Init(null, ModelPath, null, null);
            ent.Render.CastShadows = false;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, false);
            return ent;
        }

        private static void AlternateEntityClear(MyEntity myEntity)
        {
            myEntity.PositionComp.SetWorldMatrix(ref MatrixD.Identity, null, false, false, false);
            myEntity.InScene = false;
            myEntity.Render.RemoveRenderObjects();
        }

    }

    public class PreComputedMath
    {
        internal readonly int SteeringSign;
        internal readonly double SteeringNormLen;
        internal readonly double SteeringParallelLen;
        internal readonly double SteeringCos;
        internal PreComputedMath(WeaponSystem.AmmoType ammo, AmmoConstants aConst)
        {
            if (aConst.AdvancedSmartSteering)
                ComputeSteering(ammo, aConst, out SteeringSign, out SteeringNormLen, out SteeringParallelLen, out SteeringCos);
        }

        internal void ComputeSteering(WeaponSystem.AmmoType ammo, AmmoConstants aConst, out int steeringSign, out double steeringNormLen, out double steeringParallelLen, out double steeringCos)
        {

            var steeringMaxAngleRad = MathHelperD.ToRadians(ammo.AmmoDef.Trajectory.Smarts.SteeringLimit);
            steeringCos = Math.Cos(steeringMaxAngleRad);

            steeringSign = 1;
            var maxAngleRadMod = steeringMaxAngleRad;
            if (maxAngleRadMod > Math.PI / 2)
            {
                maxAngleRadMod = Math.PI - maxAngleRadMod;
                steeringSign = -1;
            }

            steeringNormLen = Math.Sin(maxAngleRadMod);
            steeringParallelLen = Math.Sqrt(1 - steeringNormLen * steeringNormLen);
        }
    }

}

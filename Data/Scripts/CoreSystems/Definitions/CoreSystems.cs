using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Settings.CoreSettings.ServerSettings;
using static CoreSystems.Support.PartAnimation;
using static CoreSystems.Support.WeaponDefinition;
using static CoreSystems.Support.WeaponDefinition.AmmoDef.AreaOfDamageDef;
using static CoreSystems.Support.WeaponDefinition.AnimationDef;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;
using static CoreSystems.Support.WeaponDefinition.HardPointDef;
using static CoreSystems.Support.WeaponDefinition.TargetingDef.CommunicationDef;
using static WeaponCore.Data.Scripts.CoreSystems.Comms.Radio;

namespace CoreSystems.Support
{
    public class CoreSystem
    {
        public const string Arc = "Arc";
        public HardwareDef.HardwareType PartType;
        public MyStringHash PartNameIdHash;
        public int PartIdHash;
        public int PartId;
        public bool StayCharged;
        public string PartName;

        public Dictionary<EventTriggers, PartAnimation[]> WeaponAnimationSet;
        public Dictionary<EventTriggers, uint> PartAnimationLengths;
        public Dictionary<EventTriggers, ParticleEvent[]> ParticleEvents;
        public HashSet<string> AnimationIdLookup;
        public Dictionary<string, EmissiveState> PartEmissiveSet;
        public Dictionary<string, Matrix[]> PartLinearMoveSet;
        public string[] HeatingSubparts;
        public Dictionary<string, PartEmissive> EmissiveLookup;
    }

    internal class UpgradeSystem : CoreSystem
    {
        public readonly UpgradeDefinition Values;

        public float IdlePower;

        public bool AnimationsInited;

        public UpgradeSystem(MyStringHash partNameIdHash, UpgradeDefinition values, string partName, int partIdHash, int partId)
        {

            PartNameIdHash = partNameIdHash;

            Values = values;
            PartIdHash = partIdHash;
            PartId = partId;
            PartName = partName;
            PartType = (HardwareDef.HardwareType)Values.HardPoint.HardWare.Type;
            StayCharged = values.HardPoint.Other.StayCharged;
            IdlePower = values.HardPoint.HardWare.IdlePower > 0 ? values.HardPoint.HardWare.IdlePower : 0.001f;

            Session.CreateAnimationSets(Values.Animations, this, out WeaponAnimationSet, out PartEmissiveSet, out PartLinearMoveSet, out AnimationIdLookup, out PartAnimationLengths, out HeatingSubparts, out ParticleEvents, out EmissiveLookup);

        }
    }

    internal class SupportSystem : CoreSystem
    {
        public readonly SupportDefinition Values;

        public float IdlePower;

        public bool AnimationsInited;

        public SupportSystem(MyStringHash partNameIdHash, SupportDefinition values, string partName, int partIdHash, int partId)
        {
            PartNameIdHash = partNameIdHash;

            Values = values;
            PartIdHash = partIdHash;
            PartId = partId;
            PartName = partName;
            StayCharged = values.HardPoint.Other.StayCharged;
            IdlePower = values.HardPoint.HardWare.IdlePower > 0 ? values.HardPoint.HardWare.IdlePower : 0.001f;

            Session.CreateAnimationSets(Values.Animations, this, out WeaponAnimationSet, out PartEmissiveSet, out PartLinearMoveSet, out AnimationIdLookup, out PartAnimationLengths, out HeatingSubparts, out ParticleEvents, out EmissiveLookup);

        }
    }

    internal class ControlSystem : CoreSystem
    {
        public ControlSystem()
        {
            
        }
    }

    public class WeaponSystem : CoreSystem
    {
        public sealed class AmmoType
        {
            public AmmoType(AmmoDef ammoDef, MyDefinitionId ammoDefinitionId, MyDefinitionId ejectionDefinitionId, string ammoNameQueued, bool isShrapnel)
            {
                AmmoDefinitionId = ammoDefinitionId;
                EjectionDefinitionId = ejectionDefinitionId;
                AmmoDef = ammoDef;
                AmmoNameQueued = ammoNameQueued;
                IsShrapnel = isShrapnel;
            }

            public readonly MyDefinitionId AmmoDefinitionId;
            public readonly MyDefinitionId EjectionDefinitionId;
            public readonly AmmoDef AmmoDef;
            public readonly string AmmoNameQueued;
            public readonly bool IsShrapnel;
        }

        internal WeaponConstants WConst;

        public readonly MyStringHash MuzzlePartName;
        public readonly MyStringHash AzimuthPartName;
        public readonly MyStringHash ElevationPartName;
        public readonly MyStringHash SpinPartName;
        public readonly MyStringHash StorageLocation;
        public readonly MyStringHash BroadCastChannel;
        public readonly MyStringHash RelayChannel;
        public readonly WeaponDefinition Values;
        public readonly AmmoType[] AmmoTypes;
        public readonly MySoundPair PreFireSoundPair;
        public readonly MySoundPair HardPointSoundPair;
        public readonly MySoundPair ReloadSoundPairs;
        public readonly MySoundPair BarrelRotateSoundPair;
        public readonly MySoundPair NoSoundPair;
        public readonly HashSet<int> Threats = new HashSet<int>();
        public readonly Prediction Prediction;
        public readonly TurretType TurretMovement;
        public readonly FiringSoundState FiringSound;
        public readonly RadioTypes RadioType;
        public readonly string AltScopeName;
        public readonly string AltEjectorName;
        public readonly string ShortName;
        public readonly string[] Muzzles;
        public readonly uint MaxTrackingTicks;
        public readonly int TopTargets;
        public readonly int CycleTargets;
        public readonly int TopBlocks;
        public readonly int CycleBlocks;
        public readonly int MaxActiveProjectiles;
        public readonly int StorageLimit;
        public readonly int MaxReloads;
        public readonly int DelayToFire;
        public readonly int CeaseFireDelay;
        public readonly int MinAzimuth;
        public readonly int MaxAzimuth;
        public readonly int MinElevation;
        public readonly int MaxElevation;
        public readonly int MaxHeat;
        public readonly int WeaponIdHash;
        public readonly int WeaponId;
        public readonly int BarrelsPerShot;
        public readonly int BarrelSpinRate;
        public readonly int ShotsPerBurst;
        public readonly int MaxAmmoCount;
        public readonly int MaxConnections;
        public readonly bool ProjectilesFirst;
        public readonly bool ProjectilesOnly;
        public readonly bool StorageLimitPerBlock;
        public readonly bool MaxTrackingTime;
        public readonly bool HasAntiSmart;
        public readonly bool HasAmmoSelection;
        public readonly bool HasEjector;
        public readonly bool HasScope;
        public readonly bool HasBarrelRotation;
        public readonly bool BarrelEffect1;
        public readonly bool BarrelEffect2;
        public readonly bool HasBarrelShootAv;
        public readonly bool TargetSubSystems;
        public readonly bool OnlySubSystems;
        public readonly bool ClosestFirst;
        public readonly bool DegRof;
        public readonly bool ProhibitCoolingWhenOff;
        public readonly bool TrackProjectile;
        public readonly bool DisableSupportingPD;
        public readonly bool ScanTrackOnly;
        public readonly bool AlternateUi;
        public readonly bool NonThreatsOnly;
        public readonly bool StoreTargets;
        public readonly bool TrackTopMostEntities;
        public readonly bool TrackGrids;
        public readonly bool TrackCharacters;
        public readonly bool TrackMeteors;
        public readonly bool UniqueTargetPerWeapon;
        public readonly bool TrackNeutrals;
        public readonly bool DisableLosCheck;
        public readonly bool ScanNonThreats;
        public readonly bool TargetGridCenter;
        public readonly bool ScanThreats;
        public readonly bool TrackTargets;
        public readonly bool HasRequiresTarget;
        public readonly bool HasDrone;
        public readonly bool DesignatorWeapon;
        public readonly bool DelayCeaseFire;
        public readonly bool AlwaysFireFull;
        public readonly bool WeaponReloadSound;
        public readonly bool NoAmmoSound;
        public readonly bool HardPointRotationSound;
        public readonly bool BarrelRotateSound;
        public readonly bool PreFireSound;
        public readonly bool HasGuidedAmmo;
        public readonly bool SuppressFire;
        public readonly bool NoSubParts;
        public readonly bool HasSpinPart;
        public readonly bool DebugMode;
        public readonly bool ShootBlanks;
        public readonly bool HasProjectileSync;
        public readonly bool TargetSlaving;
        public readonly bool TargetPersists;
        public readonly bool DisableStatus;
        public readonly bool FocusOnly;
        public readonly bool EvictUniqueTargets;
        public readonly bool GoHomeToReload;
        public readonly bool DropTargetUntilLoaded;
        public readonly bool NoVoxelLosCheck;
        public readonly double MaxTargetSpeed;
        public readonly double AzStep;
        public readonly double ElStep;
        public readonly double HomeAzimuth;
        public readonly double HomeElevation;
        public readonly double MaxLockRange;
        public readonly float Barrel1AvTicks;
        public readonly float Barrel2AvTicks;
        public readonly float WepCoolDown;
        public readonly float MinTargetRadius;
        public readonly float MaxTargetRadius;
        public readonly float MaxAmmoVolume;
        public readonly float FullAmmoVolume;
        public readonly float LowAmmoVolume;
        public readonly float FiringSoundDistSqr;
        public readonly float ReloadSoundDistSqr;
        public readonly float BarrelSoundDistSqr;
        public readonly float HardPointSoundDistSqr;
        public readonly float NoAmmoSoundDistSqr;
        public readonly float HardPointAvMaxDistSqr;
        public readonly float ApproximatePeakPower;

        public bool AnimationsInited;

        public enum FiringSoundState
        {
            None,
            PerShot,
            WhenDone
        }

        public enum TurretType
        {
            Full,
            AzimuthOnly,
            ElevationOnly,
            Fixed //not used yet
        }

        public WeaponSystem(WeaponStructure structure, MyStringHash partNameIdHash, MyStringHash muzzlePartName, MyStringHash azimuthPartName, MyStringHash elevationPartName, MyStringHash spinPartName, WeaponDefinition values, string partName, AmmoType[] weaponAmmoTypes, int weaponIdHash, int weaponId)
        {
            WConst = new WeaponConstants(values);

            DisableLosCheck = values.HardPoint.Other.DisableLosCheck;
            DebugMode = values.HardPoint.Other.Debug;
            PartNameIdHash = partNameIdHash;
            MuzzlePartName = muzzlePartName;
            DesignatorWeapon = muzzlePartName.String == "Designator";
            AzimuthPartName = azimuthPartName;
            ElevationPartName = elevationPartName;
            SpinPartName = spinPartName;
            NoSubParts = (muzzlePartName.String == "None" || string.IsNullOrEmpty(muzzlePartName.String)) && (AzimuthPartName.String == "None" || string.IsNullOrEmpty(AzimuthPartName.String)) && (ElevationPartName.String == "None" || string.IsNullOrEmpty(ElevationPartName.String));
            HasSpinPart = !string.IsNullOrEmpty(SpinPartName.String) && SpinPartName.String != "None";
            Values = values;
            Muzzles = values.Assignments.Muzzles;
            WeaponIdHash = weaponIdHash;
            WeaponId = weaponId;
            PartName = partName;
            AmmoTypes = weaponAmmoTypes;
            MaxAmmoVolume = Values.HardPoint.HardWare.InventorySize * (values.HardPoint.Loading.UseWorldInventoryVolumeMultiplier ? MyAPIGateway.Session.BlocksInventorySizeMultiplier : 1);
            FullAmmoVolume = MaxAmmoVolume * (values.HardPoint.Loading.InventoryFillAmount > 0 ? values.HardPoint.Loading.InventoryFillAmount : 0.75f);
            LowAmmoVolume = MaxAmmoVolume * (values.HardPoint.Loading.InventoryLowAmount > 0 ? values.HardPoint.Loading.InventoryLowAmount : 0.25f); 
            CeaseFireDelay = values.HardPoint.DelayCeaseFire;
            DelayCeaseFire = CeaseFireDelay > 0;
            DelayToFire = values.HardPoint.Loading.DelayUntilFire;
            StayCharged = values.HardPoint.Loading.StayCharged || WConst.ReloadTime == 0;
            MaxTargetSpeed = values.Targeting.StopTrackingSpeed > 0 ? values.Targeting.StopTrackingSpeed : double.MaxValue;
            ClosestFirst = values.Targeting.ClosestFirst;
            UniqueTargetPerWeapon = Values.Targeting.UniqueTargetPerWeapon;
            AlwaysFireFull = values.HardPoint.Loading.FireFull;
            Prediction = Values.HardPoint.AimLeadingPrediction;
            ShootBlanks = Values.Targeting.ShootBlanks;
            FocusOnly = Values.Targeting.FocusOnly;
            EvictUniqueTargets = Values.Targeting.EvictUniqueTargets;
            AlternateUi = Values.HardPoint.Ui.AlternateUi;
            DisableStatus = Values.HardPoint.Ui.DisableStatus;
            GoHomeToReload = Values.HardPoint.Loading.GoHomeToReload;
            DropTargetUntilLoaded = Values.HardPoint.Loading.DropTargetUntilLoaded;
            NoVoxelLosCheck = Values.HardPoint.Other.NoVoxelLosCheck;

            TopTargets = Values.Targeting.TopTargets;
            CycleTargets = Values.Targeting.CycleTargets;
            TopBlocks = Values.Targeting.TopBlocks;
            CycleBlocks = Values.Targeting.CycleBlocks;
            MaxReloads = Values.HardPoint.Loading.MaxReloads;
            MaxActiveProjectiles = Values.HardPoint.Loading.MaxActiveProjectiles > 0 ? Values.HardPoint.Loading.MaxActiveProjectiles : int.MaxValue;
            TargetGridCenter = Values.HardPoint.Ai.TargetGridCenter;
            var comms = Values.Targeting.Communications;

            if (comms.Mode == Comms.NoComms)
                RadioType = RadioTypes.None;
            else
            {
                var hasBroadCastChanel = !string.IsNullOrEmpty(comms.BroadCastChannel);
                var hasRelayChannel = !string.IsNullOrEmpty(comms.RelayChannel);
                if (!string.IsNullOrEmpty(comms.StorageLocation) && comms.Mode == Comms.LocalNetwork)
                {
                    StorageLimit = comms.StorageLimit > 0 ? comms.StorageLimit : int.MaxValue;
                    StoreTargets = comms.StoreTargets;
                    StorageLimitPerBlock = comms.StoreLimitPerBlock;
                    StorageLocation = MyStringHash.GetOrCompute(comms.StorageLocation);
                    RadioType = !StoreTargets ? RadioTypes.Slave : RadioTypes.Master;
                }
                else if (hasBroadCastChanel && comms.BroadCastRange > 0 && comms.Mode == Comms.BroadCast)
                {
                    BroadCastChannel = MyStringHash.GetOrCompute(comms.BroadCastChannel);
                    RadioType = comms.JammingStrength > 0 ? RadioTypes.Jammer : RadioTypes.Transmitter;
                }
                else if (hasBroadCastChanel && comms.BroadCastRange > 0 && comms.Mode == Comms.Repeat)
                {
                    BroadCastChannel = MyStringHash.GetOrCompute(comms.BroadCastChannel);
                    RadioType = RadioTypes.Repeater;
                }
                else if (hasBroadCastChanel && hasRelayChannel && comms.RelayRange > 0 && comms.Mode == Comms.Relay)
                {
                    RelayChannel = MyStringHash.GetOrCompute(comms.RelayChannel);
                    RadioType = RadioTypes.Relay;
                }
                else if (hasBroadCastChanel || hasRelayChannel) {

                    if (hasBroadCastChanel)
                        BroadCastChannel = MyStringHash.GetOrCompute(comms.BroadCastChannel);
                    if (hasRelayChannel)
                        MyStringHash.GetOrCompute(comms.RelayChannel);

                    RadioType = RadioTypes.Receiver;
                }

                MaxConnections = comms.MaxConnections == 0 ? int.MaxValue : comms.MaxConnections;
                TargetPersists = comms.TargetPersists;
                TargetSlaving = RadioType != RadioTypes.Master;
                //Log.Line($"{partName} - radio:{RadioType} - location:{StorageLocation} - perBlock:{StorageLimitPerBlock} - limit:{StorageLimit} - persists:{TargetPersists} - maxConn:{MaxConnections} - unique:{UniqueTargetPerWeapon}");
            }

            SuppressFire = Values.HardPoint.Ai.SuppressFire;
            PartType = Values.HardPoint.HardWare.Type;
            HasEjector = !string.IsNullOrEmpty(Values.Assignments.Ejector);
            AltEjectorName = HasEjector ? "subpart_" + Values.Assignments.Ejector : string.Empty;
            HasScope = !string.IsNullOrEmpty(Values.Assignments.Scope);
            AltScopeName = HasScope ? "subpart_" + Values.Assignments.Scope : string.Empty;
            TurretMovements(out AzStep, out ElStep, out MinAzimuth, out MaxAzimuth, out MinElevation, out MaxElevation, out HomeAzimuth, out HomeElevation, out TurretMovement);
            Heat(out DegRof, out MaxHeat, out WepCoolDown, out ProhibitCoolingWhenOff);
            BarrelValues(out BarrelsPerShot, out ShotsPerBurst);
            BarrelsAv(out BarrelEffect1, out BarrelEffect2, out Barrel1AvTicks, out Barrel2AvTicks, out BarrelSpinRate, out HasBarrelRotation);
            Track(out ScanTrackOnly, out NonThreatsOnly, out TrackProjectile, out TrackGrids, out TrackCharacters, out TrackMeteors, out TrackNeutrals, out ScanNonThreats, out ScanThreats, out MaxTrackingTime, out MaxTrackingTicks, out TrackTopMostEntities);
            GetThreats(out Threats, out ProjectilesFirst, out ProjectilesOnly);
            SubSystems(out TargetSubSystems, out OnlySubSystems);
            ValidTargetSize(out MinTargetRadius, out MaxTargetRadius);
            Session.CreateAnimationSets(Values.Animations, this, out WeaponAnimationSet, out PartEmissiveSet, out PartLinearMoveSet, out AnimationIdLookup, out PartAnimationLengths, out HeatingSubparts, out ParticleEvents, out EmissiveLookup);

            // CheckForBadAnimations();

            ApproximatePeakPower = WConst.IdlePower;

            var ammoSelections = 0;
            for (int i = 0; i < AmmoTypes.Length; i++) // remap old configs
                RemapLegacy(AmmoTypes[i].AmmoDef);

            TrackTargets = Values.HardPoint.Ai.TrackTargets;

            var requiresTarget = TrackTargets;
            if (values.HardPoint.Ai.LockOnFocus && values.Targeting.MaxTargetDistance > MaxLockRange)
                MaxLockRange = values.Targeting.MaxTargetDistance;

            for (int i = 0; i < AmmoTypes.Length; i++)
            {

                var ammo = AmmoTypes[i];
                ammo.AmmoDef.Const = new AmmoConstants(ammo, Values, this, i);

                var aConst = ammo.AmmoDef.Const;
                if (aConst.GuidedAmmoDetected)
                    HasGuidedAmmo = true;

                if (aConst.FullSync)
                    HasProjectileSync = true;

                if (aConst.AntiSmartDetected)
                    HasAntiSmart = true;

                if (aConst.IsDrone)
                    HasDrone = true;

                if (aConst.IsTurretSelectable)
                {
                    ++ammoSelections;

                    var targetAmmoSize = aConst.MagsToLoad * aConst.MagazineSize;
                    var fireFull = aConst.MustCharge && aConst.Reloadable || AlwaysFireFull || structure.MultiParts;
                    var ammoLoadSize = MathHelper.Clamp(targetAmmoSize, 1, fireFull ? 1 : targetAmmoSize);

                    if (ammoLoadSize > MaxAmmoCount)
                        MaxAmmoCount = ammoLoadSize;
                }

                if (aConst.ChargSize > ApproximatePeakPower)
                    ApproximatePeakPower = ammo.AmmoDef.Const.ChargSize;

                if (aConst.RequiresTarget)
                    requiresTarget = true;
            }

            HasRequiresTarget = requiresTarget;

            HasAmmoSelection = ammoSelections > 1;
            HardPointSoundSetup(out WeaponReloadSound, out ReloadSoundPairs, out HardPointRotationSound, out HardPointSoundPair, out BarrelRotateSound, out BarrelRotateSoundPair, out NoAmmoSound, out NoSoundPair, out PreFireSound, out PreFireSoundPair, out HardPointAvMaxDistSqr, out FiringSound);
            HardPointSoundDistMaxSqr(AmmoTypes, out FiringSoundDistSqr, out ReloadSoundDistSqr, out BarrelSoundDistSqr, out HardPointSoundDistSqr, out NoAmmoSoundDistSqr, out HardPointAvMaxDistSqr);

            HasBarrelShootAv = BarrelEffect1 || BarrelEffect2 || HardPointRotationSound || FiringSound != FiringSoundState.None;

            var nameLen = partName.Length;
            ShortName = nameLen > 21 ? partName.Remove(21, nameLen - 21) : PartName;
        }


        private void RemapLegacy(AmmoDef ammoDef)
        {
            var oldDetDetected = ammoDef.AreaEffect.Detonation.DetonateOnEnd;
            var oldType = ammoDef.AreaEffect.AreaEffect;
            var oldDamageType = oldType == AmmoDef.AreaDamageDef.AreaEffectType.Explosive || oldType == AmmoDef.AreaDamageDef.AreaEffectType.Radiant;
            if (oldDamageType)
            {
                var checkold = Math.Max(ammoDef.AreaEffect.Base.EffectStrength, ammoDef.AreaEffect.AreaEffectDamage);
                var currentDamage = checkold <= 0 ? ammoDef.BaseDamage : checkold;
                var currentRadius = Math.Max(ammoDef.AreaEffect.Base.Radius, ammoDef.AreaEffect.AreaEffectRadius);
                if (currentDamage > 0 && currentRadius > 0)
                {
                    ammoDef.AreaOfDamage.ByBlockHit.Enable = true;
                    ammoDef.AreaOfDamage.ByBlockHit.Damage = currentDamage;
                    ammoDef.AreaOfDamage.ByBlockHit.Radius = currentRadius;
                    ammoDef.AreaOfDamage.ByBlockHit.Depth = 1;
                    ammoDef.AreaOfDamage.ByBlockHit.Falloff = Falloff.Exponential;
                }
            }

            if (oldDetDetected)
            {
                ammoDef.AreaOfDamage.EndOfLife.Enable = true;
                ammoDef.AreaOfDamage.EndOfLife.Damage = ammoDef.AreaEffect.Detonation.DetonationDamage;
                ammoDef.AreaOfDamage.EndOfLife.Radius = ammoDef.AreaEffect.Detonation.DetonationRadius;
                ammoDef.AreaOfDamage.EndOfLife.Depth = 1;
                ammoDef.AreaOfDamage.EndOfLife.MinArmingTime = ammoDef.AreaEffect.Detonation.MinArmingTime;
                ammoDef.AreaOfDamage.EndOfLife.ArmOnlyOnHit = ammoDef.AreaEffect.Detonation.ArmOnlyOnHit;
                ammoDef.AreaOfDamage.EndOfLife.CustomParticle = ammoDef.AreaEffect.Explosions.CustomParticle;
                ammoDef.AreaOfDamage.EndOfLife.CustomSound = ammoDef.AreaEffect.Explosions.CustomSound;
                ammoDef.AreaOfDamage.EndOfLife.ParticleScale = ammoDef.AreaEffect.Explosions.Scale;
                ammoDef.AreaOfDamage.EndOfLife.NoVisuals = ammoDef.AreaEffect.Explosions.NoVisuals;
                ammoDef.AreaOfDamage.EndOfLife.NoSound = ammoDef.AreaEffect.Explosions.NoSound;
                ammoDef.AreaOfDamage.EndOfLife.Falloff = Falloff.Exponential;
            }
        }



        private void GetThreats(out HashSet<int> set, out bool projectilesFirst, out bool projectilesOnly)
        {
            set = new HashSet<int>(Values.Targeting.Threats.Length);
            foreach (var t in Values.Targeting.Threats)
            {
                set.Add((int)t);
            }

            projectilesFirst = Values.Targeting.Threats.Length > 0 && Values.Targeting.Threats[0] == TargetingDef.Threat.Projectiles;
            projectilesOnly = projectilesFirst && Values.Targeting.Threats.Length == 1;
        }

        private void Heat(out bool degRof, out int maxHeat, out float wepCoolDown, out bool coolWhenOff)
        {
            coolWhenOff = Values.HardPoint.Loading.ProhibitCoolingWhenOff;
            degRof = Values.HardPoint.Loading.DegradeRof;
            maxHeat = Values.HardPoint.Loading.MaxHeat;
            wepCoolDown = Values.HardPoint.Loading.Cooldown;
            if (wepCoolDown < 0) wepCoolDown = 0;
            if (wepCoolDown > .95f) wepCoolDown = .95f;
        }

        private void BarrelsAv(out bool barrelEffect1, out bool barrelEffect2, out float barrel1AvTicks, out float barrel2AvTicks, out int barrelSpinRate, out bool hasBarrelRotation)
        {
            barrelEffect1 = Values.HardPoint.Graphics.Effect1.Name != string.Empty;
            barrelEffect2 = Values.HardPoint.Graphics.Effect2.Name != string.Empty;
            barrel1AvTicks = Values.HardPoint.Graphics.Effect1.Extras.MaxDuration;
            barrel2AvTicks = Values.HardPoint.Graphics.Effect2.Extras.MaxDuration;

            barrelSpinRate = 0;
            if (Values.HardPoint.Other.RotateBarrelAxis != 0)
            {
                if (Values.HardPoint.Loading.BarrelSpinRate > 0) barrelSpinRate = Values.HardPoint.Loading.BarrelSpinRate < 3600 ? Values.HardPoint.Loading.BarrelSpinRate : 3599;
                else barrelSpinRate = WConst.RateOfFire < 3699 ? WConst.RateOfFire : 3599;
            }
            hasBarrelRotation = barrelSpinRate > 0 && (NoSubParts || (MuzzlePartName.String != "None" && !string.IsNullOrEmpty(MuzzlePartName.String)));
        }

        private void BarrelValues(out int barrelsPerShot, out int shotsPerBurst)
        {
            barrelsPerShot = Values.HardPoint.Loading.BarrelsPerShot;
            shotsPerBurst = Values.HardPoint.Loading.ShotsInBurst;
        }

        private void TurretMovements(out double azStep, out double elStep, out int minAzimuth, out int maxAzimuth, out int minElevation, out int maxElevation, out double homeAzimuth, out double homeElevation, out TurretType turretMove)
        {
            azStep = Values.HardPoint.HardWare.RotateRate;
            elStep = Values.HardPoint.HardWare.ElevateRate;
            minAzimuth = Values.HardPoint.HardWare.MinAzimuth;
            maxAzimuth = Values.HardPoint.HardWare.MaxAzimuth;
            minElevation = Values.HardPoint.HardWare.MinElevation;
            maxElevation = Values.HardPoint.HardWare.MaxElevation;

            homeAzimuth = MathHelperD.ToRadians((((Values.HardPoint.HardWare.HomeAzimuth + 180) % 360) - 180));
            homeElevation = MathHelperD.ToRadians((((Values.HardPoint.HardWare.HomeElevation + 180) % 360) - 180));

            turretMove = TurretType.Full;

            if (minAzimuth == maxAzimuth)
                turretMove = TurretType.ElevationOnly;
            if (minElevation == maxElevation && TurretMovement != TurretType.Full)
                turretMove = TurretType.Fixed;
            else if (minElevation == maxElevation)
                turretMove = TurretType.AzimuthOnly;
            else if (NoSubParts)
                turretMove = TurretType.Fixed;
        }

        private void Track(out bool scanTrackOnly, out bool nonThreatsOnly, out bool trackProjectile, out bool trackGrids, out bool trackCharacters, out bool trackMeteors, out bool trackNeutrals, out bool scanNonThreats, out bool scanThreats, out bool maxTrackingTime, out uint maxTrackingTicks, out bool trackTopMostEntities)
        {
            trackProjectile = false;
            trackGrids = false;
            trackCharacters = false;
            trackMeteors = false;
            trackNeutrals = false;
            maxTrackingTicks = (uint)Values.Targeting.MaxTrackingTime;
            maxTrackingTime = maxTrackingTicks > 0;
            trackTopMostEntities = false;
            var threats = Values.Targeting.Threats;
            scanTrackOnly = Values.HardPoint.ScanTrackOnly;
            nonThreatsOnly = false;
            scanNonThreats = false;
            scanThreats = false;
            foreach (var threat in threats)
            {
                if (threat == TargetingDef.Threat.Projectiles)
                    trackProjectile = true;
                else if (threat == TargetingDef.Threat.Grids)
                {
                    trackGrids = true;
                    trackTopMostEntities = true;
                }
                else if (threat == TargetingDef.Threat.Characters)
                {
                    trackCharacters = true;
                    trackTopMostEntities = true;
                }
                else if (threat == TargetingDef.Threat.Meteors)
                {
                    trackMeteors = true;
                    trackTopMostEntities = true;
                }
                else if (threat == TargetingDef.Threat.Neutrals)
                {
                    trackNeutrals = true;
                    trackTopMostEntities = true;
                }
                else if (threat == TargetingDef.Threat.ScanEnemyGrid || threat == TargetingDef.Threat.ScanNeutralCharacter || threat == TargetingDef.Threat.ScanEnemyCharacter || threat == TargetingDef.Threat.ScanNeutralGrid || threat == TargetingDef.Threat.ScanUnOwnedGrid)
                {
                    trackTopMostEntities = true;
                    scanThreats = true;
                }
                else if (threat == TargetingDef.Threat.ScanFriendlyCharacter || threat == TargetingDef.Threat.ScanFriendlyGrid || threat == TargetingDef.Threat.ScanOwnersGrid)
                {
                    trackTopMostEntities = true;
                    scanNonThreats = true;
                }
                else if (threat == TargetingDef.Threat.ScanRoid || threat == TargetingDef.Threat.ScanPlanet)
                {
                    trackTopMostEntities = true;
                    scanNonThreats = true;
                }
                nonThreatsOnly = scanNonThreats && !scanThreats && !trackNeutrals && !trackMeteors && !trackCharacters && !trackGrids && !trackProjectile;
            }
        }

        private void SubSystems(out bool targetSubSystems, out bool onlySubSystems)
        {
            targetSubSystems = false;
            var anySystemDetected = false;
            if (Values.Targeting.SubSystems.Length > 0)
            {
                foreach (var system in Values.Targeting.SubSystems)
                {
                    if (system != TargetingDef.BlockTypes.Any) targetSubSystems = true;
                    else anySystemDetected = true;
                }
            }
            if (TargetSubSystems && anySystemDetected) onlySubSystems = false;
            else onlySubSystems = true;
        }

        private void ValidTargetSize(out float minTargetRadius, out float maxTargetRadius)
        {
            var minDiameter = Values.Targeting.MinimumDiameter;
            var maxDiameter = Values.Targeting.MaximumDiameter;

            minTargetRadius = (float)(minDiameter > 0 ? minDiameter * 0.5d : 0);
            maxTargetRadius = (float)(maxDiameter > 0 ? maxDiameter * 0.5d : 8192);
        }

        private void HardPointSoundSetup(out bool weaponReloadSound, out MySoundPair reloadSoundPair, out bool hardPointRotationSound, out MySoundPair hardPointSoundPair, out bool barrelRotationSound, out MySoundPair barrelSoundPair, out bool noAmmoSound, out MySoundPair noAmmoSoundPair, out bool preFireSound, out MySoundPair preFireSoundPair, out float hardPointAvMaxDistSqr, out FiringSoundState firingSound)
        {
            weaponReloadSound = Values.HardPoint.Audio.ReloadSound != string.Empty;
            reloadSoundPair = weaponReloadSound ? new MySoundPair(Values.HardPoint.Audio.ReloadSound, false) : null;

            hardPointRotationSound = Values.HardPoint.Audio.HardPointRotationSound != string.Empty;
            hardPointSoundPair = hardPointRotationSound  ? new MySoundPair(Values.HardPoint.Audio.HardPointRotationSound, false) : null;

            barrelRotationSound = Values.HardPoint.Audio.BarrelRotationSound != string.Empty;
            barrelSoundPair = barrelRotationSound ? new MySoundPair(Values.HardPoint.Audio.BarrelRotationSound, false) : null;
            
            noAmmoSound = Values.HardPoint.Audio.NoAmmoSound != string.Empty;
            noAmmoSoundPair = noAmmoSound ? new MySoundPair(Values.HardPoint.Audio.NoAmmoSound, false) : null;

            preFireSound = Values.HardPoint.Audio.PreFiringSound != string.Empty;
            preFireSoundPair = preFireSound ? new MySoundPair(Values.HardPoint.Audio.PreFiringSound, false) : null;

            var fSoundStart = Values.HardPoint.Audio.FiringSound;
            if (fSoundStart != string.Empty && Values.HardPoint.Audio.FiringSoundPerShot)
                firingSound = FiringSoundState.PerShot;
            else if (fSoundStart != string.Empty && !Values.HardPoint.Audio.FiringSoundPerShot)
                firingSound = FiringSoundState.WhenDone;
            else firingSound = FiringSoundState.None;

            hardPointAvMaxDistSqr = 0;
            if (Values.HardPoint.Graphics.Effect1.Extras.MaxDistance * Values.HardPoint.Graphics.Effect1.Extras.MaxDistance > HardPointAvMaxDistSqr)
                hardPointAvMaxDistSqr = Values.HardPoint.Graphics.Effect1.Extras.MaxDistance * Values.HardPoint.Graphics.Effect1.Extras.MaxDistance;

            if (Values.HardPoint.Graphics.Effect2.Extras.MaxDistance * Values.HardPoint.Graphics.Effect2.Extras.MaxDistance > HardPointAvMaxDistSqr)
                hardPointAvMaxDistSqr = Values.HardPoint.Graphics.Effect2.Extras.MaxDistance * Values.HardPoint.Graphics.Effect2.Extras.MaxDistance;
        }

        private void HardPointSoundDistMaxSqr(AmmoType[] weaponAmmo, out float firingSoundDistSqr, out float reloadSoundDistSqr, out float barrelSoundDistSqr, out float hardPointSoundDistSqr, out float noAmmoSoundDistSqr, out float hardPointAvMaxDistSqr)
        {
            var fireSound = string.Concat(Arc, Values.HardPoint.Audio.FiringSound);
            var reloadSound = string.Concat(Arc, Values.HardPoint.Audio.ReloadSound);
            var barrelSound = string.Concat(Arc, Values.HardPoint.Audio.BarrelRotationSound);
            var hardPointSound = string.Concat(Arc, Values.HardPoint.Audio.HardPointRotationSound);
            var noAmmoSound = string.Concat(Arc, Values.HardPoint.Audio.NoAmmoSound);

            firingSoundDistSqr = 0f;
            reloadSoundDistSqr = 0f;
            barrelSoundDistSqr = 0f;
            hardPointSoundDistSqr = 0f;
            noAmmoSoundDistSqr = 0f;
            hardPointAvMaxDistSqr = HardPointAvMaxDistSqr;

            foreach (var def in Session.I.SoundDefinitions)
            {
                var id = def.Id.SubtypeId.String;

                if (FiringSound != FiringSoundState.None && id == fireSound)
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) firingSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (firingSoundDistSqr > hardPointAvMaxDistSqr) hardPointAvMaxDistSqr = FiringSoundDistSqr;
                }
                if (WeaponReloadSound && id == reloadSound)
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) reloadSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (reloadSoundDistSqr > hardPointAvMaxDistSqr) hardPointAvMaxDistSqr = ReloadSoundDistSqr;

                }
                if (BarrelRotateSound && id == barrelSound)
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) barrelSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (barrelSoundDistSqr > hardPointAvMaxDistSqr) hardPointAvMaxDistSqr = BarrelSoundDistSqr;
                }
                if (HardPointRotationSound && id == hardPointSound)
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) hardPointSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (hardPointSoundDistSqr > hardPointAvMaxDistSqr) hardPointAvMaxDistSqr = HardPointSoundDistSqr;
                }
                if (NoAmmoSound && id == noAmmoSound)
                {
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_AudioDefinition;
                    if (ob != null) noAmmoSoundDistSqr = ob.MaxDistance * ob.MaxDistance;
                    if (noAmmoSoundDistSqr > hardPointAvMaxDistSqr) hardPointAvMaxDistSqr = NoAmmoSoundDistSqr;
                }
            }

            if (firingSoundDistSqr <= 0)
                foreach (var ammoType in weaponAmmo)
                    if (ammoType.AmmoDef.Trajectory.MaxTrajectory * ammoType.AmmoDef.Trajectory.MaxTrajectory > firingSoundDistSqr)
                        firingSoundDistSqr = ammoType.AmmoDef.Trajectory.MaxTrajectory * ammoType.AmmoDef.Trajectory.MaxTrajectory;
        }
    
    }

    internal class WeaponConstants
    {
        internal readonly double MaxTargetDistance;
        internal readonly double AimingToleranceRads;

        internal readonly float MinTargetDistance;
        internal readonly float DeviateShotAngleRads;
        internal readonly float IdlePower;
        internal readonly float HeatSinkRate;
        internal readonly float MinRateOfFire;
        internal readonly int ReloadTime;
        internal readonly int RateOfFire;
        internal readonly int HeatPerShot;
        public readonly int DelayAfterBurst;

        internal readonly uint FireSoundEndDelay;

        public readonly bool GiveUpAfter;
        internal bool SpinFree;
        internal bool DebugMode;
        internal bool HasServerOverrides;
        internal bool FireSoundNoBurst;
        internal bool HasDrone;

        internal WeaponConstants(WeaponDefinition values)
        {
            FireSoundNoBurst = values.HardPoint.Audio.FireSoundNoBurst;
            FireSoundEndDelay = values.HardPoint.Audio.FireSoundEndDelay;
            DelayAfterBurst = values.HardPoint.Loading.DelayAfterBurst;
            GiveUpAfter = values.HardPoint.Loading.GiveUpAfter;

            SpinFree = values.HardPoint.Loading.SpinFree;
            MinRateOfFire = values.HardPoint.Ui.RateOfFireMin > 1 || values.HardPoint.Ui.RateOfFireMin < 0 ? 0 : values.HardPoint.Ui.RateOfFireMin;


            MaxTargetDistance = values.Targeting.MaxTargetDistance > 0 ? values.Targeting.MaxTargetDistance : double.MaxValue;
            MinTargetDistance = Math.Max(values.Targeting.MinTargetDistance, 0f);
            RateOfFire = Math.Max(values.HardPoint.Loading.RateOfFire, 0);
            ReloadTime = Math.Max(values.HardPoint.Loading.ReloadTime, 0);

            DeviateShotAngleRads = MathHelper.ToRadians(values.HardPoint.DeviateShotAngle);
            AimingToleranceRads = MathHelperD.ToRadians(values.HardPoint.AimingTolerance <= 0 ? 180 : values.HardPoint.AimingTolerance);

            HeatPerShot = values.HardPoint.Loading.HeatPerShot;
            HeatSinkRate = values.HardPoint.Loading.HeatSinkRate;

            IdlePower = Math.Max(values.HardPoint.HardWare.IdlePower, 0.001f);
            DebugMode = values.HardPoint.Other.Debug;

            WeaponOverride wO;
            if (!Session.I.WeaponValuesMap.TryGetValue(values, out wO) || wO == null)
                return;

            HasServerOverrides = true;

            if (wO.MaxTargetDistance.HasValue) MaxTargetDistance = wO.MaxTargetDistance.Value > 0 ? wO.MaxTargetDistance.Value : double.MaxValue;
            if (wO.MinTargetDistance.HasValue) MinTargetDistance = Math.Max(wO.MinTargetDistance.Value, 0f);
            if (wO.RateOfFire.HasValue) RateOfFire = Math.Max(wO.RateOfFire.Value, 0);
            if (wO.ReloadTime.HasValue) ReloadTime = Math.Max(wO.ReloadTime.Value, 0);

            if (wO.DeviateShotAngle.HasValue) DeviateShotAngleRads = MathHelper.ToRadians(Math.Max(wO.DeviateShotAngle.Value, 0f));
            if (wO.AimingTolerance.HasValue) AimingToleranceRads = MathHelperD.ToRadians(wO.AimingTolerance.Value <= 0 ? 180 : wO.AimingTolerance.Value);
            //if (wO.InventorySize.HasValue) 

            if (wO.HeatPerShot.HasValue) HeatPerShot = Math.Max(wO.HeatPerShot.Value, 0);
            //if (wO.MaxHeat.HasValue) 
            if (wO.HeatSinkRate.HasValue) HeatSinkRate = Math.Max(wO.HeatSinkRate.Value, 0);
            //if (wO.Cooldown.HasValue) 

            //if (wO.ConstructPartCap.HasValue) 
            //if (wO.RestrictionRadius.HasValue) 
            //if (wO.CheckInflatedBox.HasValue) 
            //if (wO.CheckForAnyWeapon.HasValue) 
            //if (wO.MuzzleCheck.HasValue) 

            if (wO.IdlePower.HasValue) IdlePower = Math.Max(wO.IdlePower.Value, 0.001f);
        }
    }
}

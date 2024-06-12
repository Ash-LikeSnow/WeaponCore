using ProtoBuf;
using System.Xml.Serialization;
using VRage.Input;
using VRageMath;
using WeaponCore.Data.Scripts.CoreSystems.Support;

namespace CoreSystems.Settings
{
    public class CoreSettings
    {
        internal readonly VersionControl VersionControl;
        internal ServerSettings Enforcement;
        internal ClientSettings ClientConfig;
        internal Session Session;
        internal bool ClientWaiting;
        internal CoreSettings(Session session)
        {
            Session = session;
            VersionControl = new VersionControl(this);
            VersionControl.InitSettings();
            if (Session.IsClient)
                ClientWaiting = true;
            else {
                Session.AdvSync = Enforcement.AdvancedProjectileSync && (Session.MpActive || Session.LocalVersion);
                Session.AdvSyncServer = Session.AdvSync;
                Session.AdvSyncClient = Session.AdvSync && Session.LocalVersion;
            }
        }

        [ProtoContract]
        public class ServerSettings
        {
            [ProtoContract]
            public class BlockModifer
            {
                [ProtoMember(1)] public string SubTypeId;
                [ProtoMember(2)] public float DirectDamageModifer;
                [ProtoMember(3)] public float AreaDamageModifer;
            }

            [ProtoContract]
            public class ShipSize
            {
                [ProtoMember(1)] public string Name;
                [ProtoMember(2)] public int BlockCount;
                [ProtoMember(3)] public bool LargeGrid;
            }

            [ProtoContract]
            public class Modifiers
            {
                [ProtoMember(1)] public AmmoMod[] Ammos;
                [ProtoMember(2)] public WeaponMod[] Weapons;
            }

            [ProtoContract]
            public struct AmmoMod
            {
                [ProtoMember(1)] public string AmmoName;
                [ProtoMember(2)] public string Variable;
                [ProtoMember(3)] public string Value;
            }

            [ProtoContract]
            public struct WeaponMod
            {
                [ProtoMember(1)] public string PartName;
                [ProtoMember(2)] public string Variable;
                [ProtoMember(3)] public string Value;
            }

            [ProtoContract]
            public class Overrides
            {
                [ProtoMember(1)] public AmmoOverride[] AmmoOverrides;
                [ProtoMember(2)] public WeaponOverride[] WeaponOverrides;
                [ProtoMember(3)] public ArmorOverride[] ArmorOverrides;
            }

            [ProtoContract]
            public class AmmoOverride
            {
                [XmlAttribute]
                [ProtoMember(1)] public string AmmoName;

                public bool ShouldSerializeBaseDamage() => BaseDamage.HasValue;
                [ProtoMember(2)] public float? BaseDamage;
                public bool ShouldSerializeAreaEffectDamage() => AreaEffectDamage.HasValue;
                [ProtoMember(3)] public float? AreaEffectDamage;
                public bool ShouldSerializeAreaEffectRadius() => AreaEffectRadius.HasValue;
                [ProtoMember(4)] public double? AreaEffectRadius;
                public bool ShouldSerializeDetonationDamage() => DetonationDamage.HasValue;
                [ProtoMember(5)] public float? DetonationDamage;
                public bool ShouldSerializeDetonationRadius() => DetonationRadius.HasValue;
                [ProtoMember(6)] public float? DetonationRadius;
                public bool ShouldSerializeHealth() => Health.HasValue;
                [ProtoMember(7)] public float? Health;
                public bool ShouldSerializeMaxTrajectory() => MaxTrajectory.HasValue;
                [ProtoMember(8)] public float? MaxTrajectory;
                public bool ShouldSerializeDesiredSpeed() => DesiredSpeed.HasValue;
                [ProtoMember(9)] public float? DesiredSpeed;
                public bool ShouldSerializeEnergyCost() => EnergyCost.HasValue;
                [ProtoMember(10)] public float? EnergyCost;
                public bool ShouldSerializeGravityMultiplier() => GravityMultiplier.HasValue;
                [ProtoMember(11)] public float? GravityMultiplier;
                public bool ShouldSerializeShieldModifier() => ShieldModifier.HasValue;
                [ProtoMember(12)] public double? ShieldModifier;
                public bool ShouldSerializeEnergyBaseDamage() => EnergyBaseDamage.HasValue;
                [ProtoMember(13)] public bool? EnergyBaseDamage;
                public bool ShouldSerializeEnergyAreaEffectDamage() => EnergyAreaEffectDamage.HasValue;
                [ProtoMember(14)] public bool? EnergyAreaEffectDamage;
                public bool ShouldSerializeEnergyDetonationDamage() => EnergyDetonationDamage.HasValue;
                [ProtoMember(15)] public bool? EnergyDetonationDamage;
                public bool ShouldSerializeEnergyShieldDamage() => EnergyShieldDamage.HasValue;
                [ProtoMember(16)] public bool? EnergyShieldDamage;
                public bool ShouldSerializeDisableClientPredictedAmmo() => DisableClientPredictedAmmo.HasValue;
                [ProtoMember(17)] public bool? DisableClientPredictedAmmo;
                public bool ShouldSerializeFallOffDistance() => FallOffDistance.HasValue;
                [ProtoMember(18)] public float? FallOffDistance;
                public bool ShouldSerializeFallOffMinMultipler() => FallOffMinMultipler.HasValue;
                [ProtoMember(19)] public float? FallOffMinMultipler;
                public bool ShouldSerializeShieldBypass() => ShieldBypass.HasValue;
                [ProtoMember(20)] public float? ShieldBypass;
                public bool ShouldSerializeMass() => Mass.HasValue;
                [ProtoMember(21)] public float? Mass;
                public bool ShouldSerializeHealthHitModifier() => HealthHitModifier.HasValue;
                [ProtoMember(22)] public double? HealthHitModifier;
                public bool ShouldSerializeByBlockHitMaxAbsorb() => ByBlockHitMaxAbsorb.HasValue;
                [ProtoMember(23)] public float? ByBlockHitMaxAbsorb;
                public bool ShouldSerializeEndOfLifeMaxAbsorb() => EndOfLifeMaxAbsorb.HasValue;
                [ProtoMember(24)] public float? EndOfLifeMaxAbsorb;
                public bool ShouldSerializeBackKickForce() => BackKickForce.HasValue;
                [ProtoMember(25)] public float? BackKickForce;
            }

            [ProtoContract]
            public class WeaponOverride
            {
                [XmlAttribute]
                [ProtoMember(1)] public string PartName;

                public bool ShouldSerializeMaxTargetDistance() => MaxTargetDistance.HasValue;
                [ProtoMember(2)] public float? MaxTargetDistance;
                public bool ShouldSerializeMinTargetDistance() => MinTargetDistance.HasValue;
                [ProtoMember(3)] public float? MinTargetDistance;
                public bool ShouldSerializeRateOfFire() => RateOfFire.HasValue;
                [ProtoMember(4)] public int? RateOfFire;
                public bool ShouldSerializeReloadTime() => ReloadTime.HasValue;
                [ProtoMember(5)] public int? ReloadTime;
                public bool ShouldSerializeDeviateShotAngle() => DeviateShotAngle.HasValue;
                [ProtoMember(6)] public float? DeviateShotAngle;
                public bool ShouldSerializeAimingTolerance() => AimingTolerance.HasValue;
                [ProtoMember(7)] public double? AimingTolerance;
                public bool ShouldSerializeInventorySize() => InventorySize.HasValue;
                [ProtoMember(8)] public float? InventorySize;
                public bool ShouldSerializeHeatPerShot() => HeatPerShot.HasValue;
                [ProtoMember(9)] public int? HeatPerShot;
                public bool ShouldSerializeMaxHeat() => MaxHeat.HasValue;
                [ProtoMember(10)] public int? MaxHeat;
                public bool ShouldSerializeHeatSinkRate() => HeatSinkRate.HasValue;
                [ProtoMember(11)] public float? HeatSinkRate;
                public bool ShouldSerializeCooldown() => Cooldown.HasValue;
                [ProtoMember(12)] public float? Cooldown;
                public bool ShouldSerializeConstructPartCap() => ConstructPartCap.HasValue;
                [ProtoMember(13)] public int? ConstructPartCap;
                public bool ShouldSerializeRestrictionRadius() => RestrictionRadius.HasValue;
                [ProtoMember(14)] public double? RestrictionRadius;
                public bool ShouldSerializeCheckInflatedBox() => CheckInflatedBox.HasValue;
                [ProtoMember(15)] public bool? CheckInflatedBox;
                public bool ShouldSerializeCheckForAnyWeapon() => CheckForAnyWeapon.HasValue;
                [ProtoMember(16)] public bool? CheckForAnyWeapon;
                public bool ShouldSerializeMuzzleCheck() => MuzzleCheck.HasValue;
                [ProtoMember(17)] public bool? MuzzleCheck;
                public bool ShouldSerializeIdlePower() => IdlePower.HasValue;
                [ProtoMember(18)] public float? IdlePower;
            }

            [ProtoContract]
            public class ArmorOverride
            {
                [XmlArrayItem("SubtypeId")]
                [ProtoMember(1)] public string[] SubtypeIds;
                public bool ShouldSerializeKineticResistance() => KineticResistance.HasValue;
                [ProtoMember(3)] public float? KineticResistance;
                public bool ShouldSerializeEnergeticResistance() => EnergeticResistance.HasValue;
                [ProtoMember(4)] public float? EnergeticResistance;
            }

            [ProtoMember(1)] public int Version = -1;
            [ProtoMember(2)] public int Debug = -1;
            [ProtoMember(3)] public bool AdvancedOptimizations = true;
            [ProtoMember(4)] public float DirectDamageModifer = 1;
            [ProtoMember(5)] public float AreaDamageModifer = 1;
            [ProtoMember(6)] public float ShieldDamageModifer = 1;
            [ProtoMember(7)] public bool BaseOptimizations = true;
            [ProtoMember(8)] public bool ServerSleepSupport = false;
            [ProtoMember(9)] public bool DisableAi;
            [ProtoMember(10)] public bool DisableLeads;
            [ProtoMember(11)] public double MinHudFocusDistance;
            [ProtoMember(12)] public double MaxHudFocusDistance = 10000;
            [ProtoMember(13)] public BlockModifer[] BlockModifers = { }; //legacy
            [ProtoMember(14)] public ShipSize[] ShipSizes = { }; //legacy

            [ProtoMember(15)] public Modifiers ServerModifiers; // legacy
            [ProtoMember(16)] public bool DisableTargetCycle;
            [ProtoMember(17)] public bool DisableHudTargetInfo;
            [ProtoMember(18)] public bool DisableHudReload;
            [ProtoMember(19)] public bool AdvancedProjectileSync;
            [ProtoMember(20)] public bool UnsupportedMode;
            [ProtoMember(21)] public bool DisableSmallVsLargeBuff = false;
            [ProtoMember(22)] public Overrides DefinitionOverrides;
            [ProtoMember(23)] public float LargeGridDamageMultiplier = 1;
            [ProtoMember(24)] public float SmallGridDamageMultiplier = 1;
        }

        [ProtoContract]
        public class ClientSettings
        {
            [ProtoMember(1)] public int Version = -1;
            [ProtoMember(2)] public bool ClientOptimizations = true;
            [ProtoMember(3)] public int AvLimit = 0;
            [ProtoMember(4)] public string MenuButton = MyMouseButtonsEnum.Middle.ToString();
            [ProtoMember(5)] public string ControlKey = MyKeys.R.ToString();
            [ProtoMember(6)] public bool ShowHudTargetSizes; // retired
            [ProtoMember(7)] public string ActionKey = MyKeys.NumPad0.ToString();
            [ProtoMember(8)] public Vector2 HudPos = new Vector2(0, 0);
            [ProtoMember(9)] public float HudScale = 1f;
            [ProtoMember(10)] public string InfoKey = MyKeys.Decimal.ToString();
            [ProtoMember(11)] public bool MinimalHud = false;
            [ProtoMember(12)] public bool StikcyPainter = true;
            [ProtoMember(13)] public string CycleNextKey = MyKeys.PageDown.ToString();
            [ProtoMember(14)] public string CyclePrevKey = MyKeys.PageUp.ToString();
            [ProtoMember(15)] public bool AdvancedMode;
            [ProtoMember(16)] public bool HideReload = false;
        }
    }
}

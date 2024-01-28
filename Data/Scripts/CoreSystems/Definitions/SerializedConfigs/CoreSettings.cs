using ProtoBuf;
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

            [ProtoMember(15)] public Modifiers ServerModifiers = new Modifiers(); // legacy
            [ProtoMember(16)] public bool DisableTargetCycle;
            [ProtoMember(17)] public bool DisableHudTargetInfo;
            [ProtoMember(18)] public bool DisableHudReload;
            [ProtoMember(19)] public bool AdvancedProjectileSync;
            [ProtoMember(20)] public bool UnsupportedMode;
            [ProtoMember(21)] public bool DisableSmallVsLargeBuff = false;
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

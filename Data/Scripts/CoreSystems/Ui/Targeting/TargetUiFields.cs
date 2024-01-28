using System.Collections.Generic;
using CoreSystems;
using CoreSystems.Support;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting
{
    internal partial class TargetUi
    {
        internal Vector3D HandPointerOffset = new Vector3D(0,0,-0.1);
        internal Vector3D PointerOffset;
        internal Vector3D AimPosition;
        internal Vector3D AimDirection;
        internal double PointerAdjScale = 0.05f;
        internal double HitIncrease;
        internal double HandHitIncrease;
        internal Color HitMarkerColor = Color.White;
        internal Vector4 HandHitMarkerColor = new Vector4(2,2,2,2);
        internal bool DrawReticle;
        internal bool HandHitMarkerActive;
        internal uint ReticleOnSelfTick;
        internal uint MasterUpdateTick;
        internal uint LastSelectableTick;
        internal uint LastManualTick;
        internal uint LastTargetNoticeTick;
        internal MyEntity LastSelectedEntity;
        internal MyEntity SelectedEntity;
        internal int ReticleAgeOnSelf;
        internal readonly char FocusChar = "_"[0];
        internal Hud.Hud.TextureMap FocusTextureMap;

        public enum TargetControl
        {
            None,
            Player,
            Drone,
            Trash,
            Other
        }

        private const string InfoKeyStr = "Detail Key: ";
        private const string ActiveNoShield = "ActiveNoShield";
        private const string ActiveShield = "ActiveShield";
        private const string InactiveNoShield = "InactiveNoShield";
        private const string InactiveShield = "InactiveShield";
        private const double CircleSize = 0.075d;
        private const double FullPulseSize = 0.1d;
        private const double PulseSize = 0.003125d;

        private const double HandCircleSize = 0.025d;
        private const double HandFullPulseSize = 0.1d;
        private const double HandPulseSize = 0.003125d;

        private readonly MyStringId _reticle = MyStringId.GetOrCompute("TargetReticle");
        private readonly MyStringId _targetCircle = MyStringId.GetOrCompute("DS_ActiveTarget");
        private readonly MyStringId _laserLine = MyStringId.GetOrCompute("LeadingLine");
        private readonly MyStringId _whiteDot = MyStringId.GetOrCompute("AnamorphicFlare");

        private readonly Vector2 _targetDrawPosition = new Vector2(0, 0.25f);
        private readonly List<IHitInfo> _hitInfo = new List<IHitInfo>();
        private readonly Dictionary<MyEntity, Ai.TargetInfo> _toPruneMasterDict = new Dictionary<MyEntity, Ai.TargetInfo>(64);
        private readonly List<Ai.TargetInfo> _toSortMasterList = new List<Ai.TargetInfo>(64);
        private readonly List<MyEntity> _sortedMasterList = new List<MyEntity>(64);

        private readonly Dictionary<MyEntity, MyTuple<float, TargetControl, MyRelationsBetweenPlayerAndBlock>> _masterTargets = new Dictionary<MyEntity, MyTuple<float, TargetControl, MyRelationsBetweenPlayerAndBlock>>(64);
        private Vector2 _pointerPosition = new Vector2(0, 0.0f);
        private Vector2 _3RdPersonPos = new Vector2(0, 0.5f);
        private Color _reticleColor = Color.White;
        private readonly HudInfo _alertHudInfo = new HudInfo(MyStringId.GetOrCompute("WC_HUD_DroneAlert"), new Vector2(0.55f, 0.66f), 0.33f);

        internal readonly int[] ExpChargeReductions = { 1, 2, 3, 5, 8, 10, 12, 14, 16, 18, 20 };
        internal readonly string[] TargetControllerNames = { "N:", "P:", "D: ", "T:", "O:" };

        private readonly Dictionary<string, HudInfo> _primaryMinimalHuds = new Dictionary<string, HudInfo>
        {
            {"ActiveNoShield", new HudInfo (MyStringId.GetOrCompute("WC_HUD_Minimal_Active"), new Vector2(0f, 0.57f), 0.42f)},
            {"InactiveNoShield", new HudInfo(MyStringId.GetOrCompute("WC_HUD_Minimal"),  new Vector2(0f, 0.57f), 0.42f)},
        };

        private readonly Dictionary<string, HudInfo> _primaryTargetHuds = new Dictionary<string, HudInfo>
        {
            {"ActiveNoShield", new HudInfo (MyStringId.GetOrCompute("WC_HUD_NoShield_Active"), new Vector2(0f, 0.57f), 0.42f)},
            {"ActiveShield",  new HudInfo(MyStringId.GetOrCompute("WC_HUD_Shield_Active"),  new Vector2(0f, 0.57f), 0.42f)},
            {"InactiveNoShield", new HudInfo(MyStringId.GetOrCompute("WC_HUD_NoShield"),  new Vector2(0f, 0.57f), 0.42f)},
            {"InactiveShield",  new HudInfo(MyStringId.GetOrCompute("WC_HUD_Shield"),  new Vector2(0f, 0.57f), 0.42f)},
        };


        private uint _cacheIdleTicks;
        private uint _lastDrawTick;

        private int _delay;
        private int _currentIdx;
        private int _endIdx = -1;
        private bool _cachedPointerPos;
        private ThirdPersonModes _3RdPersonDraw;

        private enum ThirdPersonModes
        {
            DotTarget,
            Crosshair,
            None
        }

        internal TargetUi()
        {
            var cm = Session.I.HudUi.CharacterMap;
            Dictionary<char, Hud.Hud.TextureMap> monoText;
            if (cm.TryGetValue(Hud.Hud.FontType.Shadow, out monoText))
            {
                FocusTextureMap = monoText[FocusChar];
            }
        }
    }
}

using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreSystems.Platform;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Hud
{
    partial class Hud
    {
        private const float MetersInPixel = 0.0002645833f;
        private const float PaddingConst = 10 * MetersInPixel;
        private const float WeaponHudFontSize = 7f;
        private const float WeaponHudFontHeight = WeaponHudFontSize * MetersInPixel;
        private const float ReloadHeightConst = 4f * MetersInPixel;
        private const float ReloadWidthConst = ReloadHeightConst;
        private const float HeatWidthConst = 35 * MetersInPixel;
        private const float HeatWidthOffset = HeatWidthConst + (PaddingConst * 3f);
        private const float HeatHeightConst = HeatWidthConst * 0.0625f;
        private const float InfoPanelOffset = WeaponHudFontHeight + (HeatHeightConst * 2f);
        private const float BgBorderRatio = .166f;
        private const float MonoWidthScaler = 0.75f;
        private const float ShadowWidthScaler = 0.7f;
        private const float ShadowHeightScaler = 0.65f;
        private const float ShadowSizeScaler = 1.5f;
        private const float SymbolWidthScaler = 1.28f;
        private const float BgWidthPosOffset = 1.0225f;
        private const int WeaponLimit = 50;
        private const int StackThreshold = 12;
        private const int InitialPoolCapacity = 512;
        private const uint MinUpdateTicks = 60;
        private const char FailSafeChar = ' ';

        private readonly MyConcurrentPool<AgingTextRequest> _agingTextRequestPool = new MyConcurrentPool<AgingTextRequest>(64, data => data.Clean());
        private readonly MyConcurrentPool<TextData> _textDataPool = new MyConcurrentPool<TextData>(128);
        private readonly Queue<TextureDrawData> _textureDrawPool = new Queue<TextureDrawData>();
        private readonly Queue<TextDrawRequest> _textDrawPool = new Queue<TextDrawRequest>();
        private readonly Queue<List<Weapon>> _weaponSortingListPool = new Queue<List<Weapon>>(InitialPoolCapacity);
        private readonly Queue<StackedWeaponInfo> _weaponStackedInfoPool = new Queue<StackedWeaponInfo>(InitialPoolCapacity);
        private readonly Queue<List<StackedWeaponInfo>> _weaponInfoListPool = new Queue<List<StackedWeaponInfo>>(InitialPoolCapacity);
        private readonly Queue<List<List<Weapon>>> _weaponSubListsPool = new Queue<List<List<Weapon>>>(InitialPoolCapacity);
        private readonly List<TextureDrawData> _textureAddList = new List<TextureDrawData>(256);
        private readonly List<TextDrawRequest> _textAddList = new List<TextDrawRequest>(256);
        private readonly List<TextureDrawData> _drawList = new List<TextureDrawData>(InitialPoolCapacity);
        private List<StackedWeaponInfo> _weapontoDraw = new List<StackedWeaponInfo>(256);

        private readonly ConcurrentDictionary<long, AgingTextRequest> _agingTextRequests = new ConcurrentDictionary<long, AgingTextRequest>();

        private readonly MyStringId _monoEnglishFontAtlas1 = MyStringId.GetOrCompute("EnglishFontMono");
        private readonly MyStringId _shadowEnglishFontAtlas1 = MyStringId.GetOrCompute("EnglishFontShadow");

        private MatrixD _cameraWorldMatrix;
        private Vector2D _currWeaponDisplayPos;
        private Vector3D _viewPortSize;
        private uint _lastHudUpdateTick;

        private Vector4 _bgColor = new Vector4(1, 1, 1, 0);

        private readonly FontType _hudFont = FontType.Shadow;
        private int _currentLargestName;
        private float _paddingReload;
        private float _padding;
        private float _reloadHeight;
        private float _reloadWidth;
        private float _reloadOffset;
        private float _heatOffsetX;
        private float _heatOffsetY;
        private float _textSize;
        private float _sTextSize;
        private float _textWidth;
        private float _stextWidth;
        private float _stackPadding;
        private float _heatWidth;
        private float _heatHeight;
        private float _infoPaneloffset;
        private float _bgWidth;
        private float _bgBorderHeight;
        private float _bgCenterHeight;
        private float _symbolWidth;


        ///
        /// 
        ///

        internal readonly Dictionary<FontType, Dictionary<char, TextureMap>> CharacterMap;
        internal readonly List<Weapon> WeaponsToDisplay = new List<Weapon>(128);
        internal readonly TextureMap[] PaintedTexture = new TextureMap[10];
        internal readonly TextureMap[] ReloadingTexture = new TextureMap[6];
        internal readonly TextureMap[] OutofAmmoTexture = new TextureMap[2];
        internal readonly TextureMap[] ChargingTexture = new TextureMap[10];
        internal readonly TextureMap[] InfoBackground = new TextureMap[3];
        internal readonly TextureMap[] HeatBarTexture = new TextureMap[12];

        internal int TexturesToAdd;
        internal bool NeedsUpdate = true;
        internal bool AgingTextures;

        internal enum HudMode
        {
            Selector,
            Reload,
            TargetInfo,
            Lead,
            Drone,
            PainterMarks,
        }

        internal enum FontType
        {
            Mono,
            Shadow,
            Whitespace
        }

        internal enum Justify
        {
            None,
            Left,
            Center,
            Right,
        }

        internal Hud()
        {
            LoadTextMaps("EN", out CharacterMap); // possible translations in future

            BuildMap(MyStringId.GetOrCompute("WeaponStatWindow"), 0, 0, 0, 128, 768, 128, 768, 384, ref InfoBackground);
            BuildMap(MyStringId.GetOrCompute("HeatAtlasBar"), 0, 0, 0, 64, 1024, 64, 1024, 1024, ref HeatBarTexture);
            BuildMap(MyStringId.GetOrCompute("ReloadingIcons"), 0, 0, 0, 64, 64, 64, 64, 512, ref ReloadingTexture);
            BuildMap(MyStringId.GetOrCompute("ReloadingIcons"), 0, 384, 0, 64, 64, 64, 64, 512, ref OutofAmmoTexture);
            BuildMap(MyStringId.GetOrCompute("RechargingIcons"), 0, 0, 0, 64, 64, 64, 64, 640, ref ChargingTexture);
            BuildMap(MyStringId.GetOrCompute("BlockTargetAtlas"), 0, 0, 0, 256, 256, 256, 256, 2560, ref PaintedTexture); // InitOffset X,Y offset X,Y uv X,Y textureSize X,Y

            for (int i = 0; i < InitialPoolCapacity; i++)
            {
                _textureDrawPool.Enqueue(new TextureDrawData() { Position = new Vector3D(), Blend = BlendTypeEnum.PostPP });
                _textDrawPool.Enqueue(new TextDrawRequest() { Position = new Vector3D() });
                _weaponSortingListPool.Enqueue(new List<Weapon>());
                _weaponStackedInfoPool.Enqueue(new StackedWeaponInfo());
                _weaponInfoListPool.Enqueue(new List<StackedWeaponInfo>());
                _weaponSubListsPool.Enqueue(new List<List<Weapon>>());
            }
        }

        internal void BuildMap(MyStringId material, float initOffsetX, float initOffsetY, float offsetX, float OffsetY, float uvSizeX, float uvSizeY, float textureSizeX, float textureSizeY, ref TextureMap[] textureArr)
        {
            for (int i = 0; i < textureArr.Length; i++)
            {
                var offX = initOffsetX + (offsetX * i);
                var offY = initOffsetY + (OffsetY * i);
                textureArr[i] = GenerateMap(material, offX, offY, uvSizeX, uvSizeY, textureSizeX, textureSizeY);
            }
        }

        internal class TextureMap
        {
            internal MyStringId Material;
            internal Vector2 P0;
            internal Vector2 P1;
            internal Vector2 P2;
            internal Vector2 P3;
        }


        internal class AgingTextRequest
        {
            internal readonly CachingList<TextData> Data = new CachingList<TextData>(32);
            internal string Text;
            internal Vector4 Color;
            internal Vector3D Position;
            internal FontType Font;
            internal long ElementId;
            internal Justify Justify;
            internal float FontSize;
            internal float MessageWidth;
            internal float HeightScale;
            internal int Ttl;
            internal void Clean()
            {
                Data.Clear();
                MessageWidth = 0;
            }

            internal void RefreshTtl(int ttl)
            {
                Ttl = ttl;
            }
        }

        internal class TextData
        {
            internal MyStringId Material;
            internal Vector3D WorldPos;
            internal Vector2 P0;
            internal Vector2 P1;
            internal Vector2 P2;
            internal Vector2 P3;
            internal float ScaledWidth;
            internal bool UvDraw;
            internal bool ReSize;
            internal BlendTypeEnum Blend = BlendTypeEnum.PostPP;
        }

        internal class StackedWeaponInfo
        {
            internal Weapon HighestValueWeapon;
            internal int WeaponStack;
            internal TextureDrawData CachedReloadTexture;
            internal TextureDrawData CachedHeatTexture;
            internal int ReloadIndex;
        }

        internal class TextDrawRequest
        {
            internal string Text;
            internal Vector4 Color;
            internal Vector3D Position;
            internal float FontSize;
            internal FontType Font;
        }

        internal class TextureDrawData
        {
            internal MyStringId Material;
            internal Vector4 Color;
            internal Vector3D Position = new Vector3D();
            internal Vector3 Up;
            internal Vector3 Left;
            internal Vector2 P0;
            internal Vector2 P1;
            internal Vector2 P2;
            internal Vector2 P3;
            internal float Width;
            internal float Height;
            internal bool Persistant;
            internal bool UvDraw;
            internal BlendTypeEnum Blend = BlendTypeEnum.PostPP;
        }
    }
}

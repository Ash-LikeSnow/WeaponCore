using System;
using System.Collections.Generic;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Hud
{
    partial class Hud
    {
        internal uint TicksSinceUpdated => Session.I.Tick - _lastHudUpdateTick;
        internal bool KeepBackground => Session.I.Tick - _lastHudUpdateTick < MinUpdateTicks;

        internal void UpdateHudSettings()
        {
            //runs once on first draw then only again if a menu is closed
            var fovScale = (float)(0.1 * Session.I.ScaleFov);

            var fovModifier = (float)((Session.I.Settings.ClientConfig.HudScale * 1.4) * Session.I.ScaleFov);
            var normScaler = (float)(Session.I.Settings.ClientConfig.HudScale * Session.I.ScaleFov);
            var aspectScale = (2.37037f / Session.I.AspectRatio);

            NeedsUpdate = false;
            _lastHudUpdateTick = 0;
            _viewPortSize.X = (fovScale * Session.I.AspectRatio);
            _viewPortSize.Y = fovScale;
            _viewPortSize.Z = -0.1f;

            _currWeaponDisplayPos.X = _viewPortSize.X * BgWidthPosOffset;
            _currWeaponDisplayPos.Y = _viewPortSize.Y * .6f;

            _padding = PaddingConst * ((float)Session.I.ScaleFov * Session.I.AspectRatio);
            _reloadWidth = ReloadWidthConst * fovModifier;
            _reloadHeight = ReloadHeightConst * fovModifier;
            _reloadOffset = _reloadWidth * fovModifier;

            _textSize = WeaponHudFontHeight * fovModifier;
            _sTextSize = _textSize * .75f;
            _textWidth = (WeaponHudFontHeight * Session.I.AspectRatioInv) * fovScale;
            _stextWidth = (_textWidth * .75f);
            _stackPadding = _stextWidth * 6; // gives max limit of 6 characters (x999)

            _heatWidth = (HeatWidthConst * fovModifier) ;
            _heatHeight = HeatHeightConst * fovModifier;
            _heatOffsetX = (HeatWidthOffset * fovModifier) * aspectScale;
            _heatOffsetY = (_heatHeight * 3f);

            _infoPaneloffset = InfoPanelOffset * normScaler;
            //_paddingHeat = _session.CurrentFovWithZoom < 1 ? MathHelper.Clamp(_session.CurrentFovWithZoom * 0.0001f, 0.0001f, 0.0003f) : 0;
            _paddingReload = Session.I.CurrentFovWithZoom < 1 ? MathHelper.Clamp(Session.I.CurrentFovWithZoom * 0.002f, 0.0002f, 0.001f) : 0.001f;

            _symbolWidth = ((_heatWidth + _padding) * aspectScale);
            _bgColor = new Vector4(1f, 1f, 1f, 0f);
        }

        internal bool RestrictHudHandlers(Ai ai, long playerId, HudMode mode)
        {
            foreach (var handler in Session.I.HudHandlers)
            {
                var handledTopEntityId = handler.Key;
                MyEntity handledTopEntity;
                MyEntity playerEnt;

                if (MyEntities.TryGetEntityById(handledTopEntityId, out handledTopEntity) && MyEntities.TryGetEntityById(playerId, out playerEnt) && playerEnt is IMyCharacter && ai.TopEntityMap.GroupMap.Construct.ContainsKey(handledTopEntity))
                    return handler.Value.Invoke((IMyCharacter)playerEnt, handledTopEntityId, (int)mode);
            }

            return false;
        }

        internal void AddText(string text, float x, float y, long elementId, int ttl, Vector4 color, Justify justify = Justify.None, FontType fontType = FontType.Shadow, float fontSize = 10f, float heightScale = 0.65f)
        {
            if (_agingTextRequests.ContainsKey(elementId) || string.IsNullOrEmpty(text))
                return;

            AgingTextures = true;

            var request = _agingTextRequestPool.Get();

            var pos = GetScreenSpace(new Vector2(x, y));
            request.Text = text;
            request.Color = color;
            request.Position.X = pos.X;
            request.Position.Y = pos.Y;
            request.FontSize = fontSize * MetersInPixel;
            request.Font = fontType;
            request.Ttl = ttl;
            request.ElementId = elementId;
            request.Justify = justify;
            request.HeightScale = ShadowHeightScaler;
            _agingTextRequests.TryAdd(elementId, request);
        }

        internal Vector2 GetScreenSpace(Vector2 offset)
        {
            var fovScale = (float)(0.1 * Session.I.ScaleFov);

            var position = new Vector2(offset.X, offset.Y);
            position.X *= fovScale * Session.I.AspectRatio;
            position.Y *= fovScale;
            return position;
        }

        private readonly WeaponCompare _weaponCompare = new WeaponCompare();
        private readonly WeaponListCompare _weaponListCompare = new WeaponListCompare();
        private readonly Dictionary<int, StackedWeaponInfo> _collapseNoTarget = new Dictionary<int, StackedWeaponInfo>();
        private readonly HashSet<int> _collapseNoTargetCache = new HashSet<int>();

        private void CheckNoTarget(Weapon weapon, StackedWeaponInfo swi, List<Weapon> weapons)
        {
            var matches = 0;
            var noTargetMatches = 0;
            foreach (var w in weapons)
            {
                if (w.System.WeaponIdHash == weapon.System.WeaponIdHash)
                {
                    matches++;
                    if (!w.Target.HasTarget)
                        noTargetMatches++;
                }
            }
            if (matches == noTargetMatches)
                _collapseNoTarget.Add(weapon.System.WeaponIdHash, swi);
        }

        internal List<StackedWeaponInfo> SortDisplayedWeapons(List<Weapon> list)
        {
            int finalCount = 0;
            List<StackedWeaponInfo> finalList;
            if (!_weaponInfoListPool.TryDequeue(out finalList))
                finalList = new List<StackedWeaponInfo>();

            list.Sort(_weaponCompare);
            if (list.Count > WeaponLimit) //limit to top 50 based on heat
                list.RemoveRange(WeaponLimit, list.Count - WeaponLimit);
            
            if (list.Count <= StackThreshold)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var w = list[i];

                    if (w.System.PartName.Length > _currentLargestName) 
                        _currentLargestName = w.System.PartName.Length;

                    StackedWeaponInfo swi;
                    if (!_weaponStackedInfoPool.TryDequeue(out swi))
                        swi = new StackedWeaponInfo();

                    if (!w.Target.HasTarget)
                    {
                        if (_collapseNoTarget.ContainsKey(w.System.WeaponIdHash))
                        {
                            var first = _collapseNoTarget[w.System.WeaponIdHash];
                            first.WeaponStack++;
                            _weaponStackedInfoPool.Enqueue(swi);
                            continue;
                        }
                        if (_collapseNoTargetCache.Add(w.System.WeaponIdHash))
                            CheckNoTarget(w, swi, list);
                    }


                    if (!_textureDrawPool.TryDequeue(out swi.CachedReloadTexture))
                        swi.CachedReloadTexture = new TextureDrawData();

                    if (!_textureDrawPool.TryDequeue(out swi.CachedHeatTexture))
                        swi.CachedHeatTexture = new TextureDrawData();


                    swi.CachedHeatTexture.Persistant = true;
                    swi.CachedReloadTexture.Persistant = true;
                    swi.ReloadIndex = 0;
                    swi.HighestValueWeapon = w;
                    swi.WeaponStack = 1;
                    finalList.Add(swi);
                }
                _collapseNoTarget.Clear();
                _collapseNoTargetCache.Clear();
                return finalList;
            }

            Dictionary<long, List<Weapon>> weaponTypes = new Dictionary<long, List<Weapon>>();
            for (int i = 0; i < list.Count; i++) //sort list into groups of same weapon type
            {
                var w = list[i];

                var targetId = w.Target.HasTarget ? 1 : 2;
                var hashId = (long)w.System.WeaponIdHash << 32 | (uint)targetId;
                if (!weaponTypes.ContainsKey(hashId))
                {
                    List<Weapon> tmp;
                    if (!_weaponSortingListPool.TryDequeue(out tmp))
                        tmp = new List<Weapon>();

                    weaponTypes[hashId] = tmp;
                }

                weaponTypes[hashId].Add(w);
            }

            foreach (var weaponType in weaponTypes)
            {
                var weapons = weaponType.Value;
                if (weapons[0].System.PartName.Length > _currentLargestName) _currentLargestName = weapons[0].System.PartName.Length;


                if (weapons.Count > 1)
                {
                    List<List<Weapon>> subLists;
                    List<Weapon> subList;
                    var last = weapons[0];

                    if (!_weaponSubListsPool.TryDequeue(out subLists))
                        subLists = new List<List<Weapon>>();

                    if (!_weaponSortingListPool.TryDequeue(out subList))
                        subList = new List<Weapon>();

                    for (int i = 0; i < weapons.Count; i++)
                    {
                        var w = weapons[i];

                        if (i == 0)
                            subList.Add(w);
                        else
                        {
                            var lastLoading = last.Loading || last.Reload.WaitForClient;
                            var thisLoading = w.Loading || w.Reload.WaitForClient;

                            if (last.HeatPerc - w.HeatPerc > .05f || lastLoading != thisLoading || last.PartState.Overheated != w.PartState.Overheated || last.Target.HasTarget != w.Target.HasTarget)
                            {
                                subLists.Add(subList);
                                if (!_weaponSortingListPool.TryDequeue(out subList))
                                    subList = new List<Weapon>();
                            }

                            last = w;
                            subList.Add(w);

                            if (i == weapons.Count - 1)
                                subLists.Add(subList);
                        }
                    }
                    weapons.Clear();
                    _weaponSortingListPool.Enqueue(weapons);

                    subLists.Sort(_weaponListCompare);

                    for (int i = 0; i < subLists.Count; i++)
                    {
                        var subL = subLists[i];

                        if (finalCount < StackThreshold)
                        {
                            StackedWeaponInfo swi;
                            if (!_weaponStackedInfoPool.TryDequeue(out swi))
                                swi = new StackedWeaponInfo();

                            if (!_textureDrawPool.TryDequeue(out swi.CachedReloadTexture))
                                swi.CachedReloadTexture = new TextureDrawData();

                            if (!_textureDrawPool.TryDequeue(out swi.CachedHeatTexture))
                                swi.CachedHeatTexture = new TextureDrawData();

                            swi.CachedHeatTexture.Persistant = true;
                            swi.CachedReloadTexture.Persistant = true;
                            swi.ReloadIndex = 0;
                            swi.HighestValueWeapon = subL[0];
                            swi.WeaponStack = subL.Count;
                            finalList.Add(swi);
                            finalCount++;
                        }

                        subL.Clear();
                        _weaponSortingListPool.Enqueue(subL);
                    }

                    subLists.Clear();
                    _weaponSubListsPool.Enqueue(subLists);
                }
                else
                {
                    if (finalCount < StackThreshold)
                    {
                        StackedWeaponInfo swi;
                        if (!_weaponStackedInfoPool.TryDequeue(out swi))
                            swi = new StackedWeaponInfo();

                        if (!_textureDrawPool.TryDequeue(out swi.CachedReloadTexture))
                            swi.CachedReloadTexture = new TextureDrawData();

                        if (!_textureDrawPool.TryDequeue(out swi.CachedHeatTexture))
                            swi.CachedHeatTexture = new TextureDrawData();

                        swi.CachedHeatTexture.Persistant = true;
                        swi.CachedReloadTexture.Persistant = true;
                        swi.ReloadIndex = 0;

                        swi.HighestValueWeapon = weapons[0];
                        swi.WeaponStack = 1;


                        finalList.Add(swi);
                        finalCount++;
                    }

                    weapons.Clear();
                    _weaponSortingListPool.Enqueue(weapons);
                }
            }

            return finalList;
        }

        internal void Purge()
        {

            _textureDrawPool.Clear();
            _textDrawPool.Clear();
            _weaponSortingListPool.Clear();
            _weaponStackedInfoPool.Clear();
            CharacterMap.Clear();
            _textureAddList.Clear();
            _textAddList.Clear();
            _drawList.Clear();
            _weapontoDraw.Clear();
            WeaponsToDisplay.Clear();

            List<StackedWeaponInfo> removeList;
            while (_weaponInfoListPool.TryDequeue(out removeList))
                removeList.Clear();

            List<List<Weapon>> removeList1;
            while (_weaponSubListsPool.TryDequeue(out removeList1))
            {
                for (int i = 0; i < removeList1.Count; i++)
                    removeList1[i].Clear();

                removeList1.Clear();
            }
        }
    }

    internal class WeaponCompare : IComparer<Weapon>
    {
        public int Compare(Weapon x, Weapon y)
        {
            var xHeatLevel = x.PartState.Heat > 0;
            var yHeatLevel = y.PartState.Heat > 0;
            var diffStates = xHeatLevel != yHeatLevel;

            var heatCompare = xHeatLevel.CompareTo(yHeatLevel);
            if (diffStates && heatCompare != 0) return -heatCompare;

            var xOverHeat = x.PartState.Overheated;
            var yOverHeat = y.PartState.Overheated;
            var overHeatCompare = xOverHeat.CompareTo(yOverHeat);
            if (overHeatCompare != 0) return -overHeatCompare;


            var chargeCompare = x.Charging.CompareTo(y.Charging);
            if (chargeCompare != 0) return -chargeCompare;

            var targetCompare = x.Target.HasTarget.CompareTo(y.Target.HasTarget);
            if (targetCompare != 0) return -targetCompare;

            return targetCompare;
        }
    }

    internal class WeaponListCompare : IComparer<List<Weapon>>
    {
        public int Compare(List<Weapon> x, List<Weapon> y)
        {
            var chargeCompare = x[0].Charging.CompareTo(y[0].Charging);
            if (chargeCompare != 0) return -chargeCompare;

            var xHeatLevel = x[0].System.MaxHeat - x[0].PartState.Heat;
            var yHeatLevel = y[0].System.MaxHeat - y[0].PartState.Heat;
            var hasHeat = x[0].PartState.Heat > 0 || y[0].PartState.Heat > 0;
            var heatCompare = xHeatLevel.CompareTo(yHeatLevel);
            if (hasHeat && heatCompare != 0) return -heatCompare;

            var xReload = (x[0].Loading || x[0].Reload.WaitForClient || Session.I.Tick - x[0].LastLoadedTick < 60);
            var yReload = (y[0].Loading || y[0].Reload.WaitForClient || Session.I.Tick - y[0].LastLoadedTick < 60);
            var reloadCompare = xReload.CompareTo(yReload);
            if (reloadCompare != 0) return -reloadCompare;

            var targetCompare = x[0].Target.HasTarget.CompareTo(y[0].Target.HasTarget);
            if (targetCompare != 0) return -targetCompare;

            var dpsCompare = x[0].Comp.PeakDps.CompareTo(y[0].Comp.PeakDps);
            return -dpsCompare;
        }
    }
}

using System;
using System.Collections.Generic;
using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting
{
    internal partial class TargetUi
    {
        internal void DrawTargetUi()
        {
            var s = Session.I;

            SelectedEntity = null;
            DrawReticle = false;
            if (!s.InGridAiBlock && !s.UpdateLocalAiAndCockpit()) return;

            if (ActivateMarks()) DrawActiveMarks();
            if (ActivateDroneNotice()) DrawDroneNotice();
            
            ActivateSelector();



            if (s.CheckTarget(s.TrackingAi) && GetTargetState(s))
            {
                if (ActivateLeads())
                    DrawActiveLeads();

                DrawTarget();
                HandHitMarkerActive = false;
            }
            else if (s.TrackingAi.AiType == Ai.AiTypes.Player && HandHitMarkerActive)
            {
                HandWeaponMarker();
            }
        }

        private void DrawSelector(bool enableActivator)
        {
            var s = Session.I;
            if (!_cachedPointerPos) InitPointerOffset(0.05);

            var offetPosition = Vector3D.Transform(PointerOffset, s.CameraMatrix);
            AimPosition = offetPosition;
            AimDirection = Vector3D.Normalize(AimPosition - s.CameraPos);

            if (s.UiInput.CameraBlockView)
            {
                _pointerPosition.Y = 0f;
                InitPointerOffset(0.05);
            }
            else if (s.UiInput.FirstPersonView || s.UiInput.TurretBlockView)
            {
                if (!MyUtils.IsZero(_pointerPosition.Y))
                {
                    _pointerPosition.Y = 0f;
                    InitPointerOffset(0.05);
                }
            }
            else if (s.UiInput.CtrlPressed)
            {
                if (!s.UiInput.PlayerWeapon)
                {
                    if (s.UiInput.PreviousWheel != s.UiInput.CurrentWheel)
                    {
                        var currentPos = _pointerPosition.Y;
                        if (s.UiInput.CurrentWheel > s.UiInput.PreviousWheel) currentPos += 0.05f;
                        else currentPos -= 0.05f;
                        var clampPos = MathHelper.Clamp(currentPos, -1.25f, 1.25f);
                        _3RdPersonPos.Y = clampPos;
                        InitPointerOffset(0.05);
                    }

                    if (!MyUtils.IsEqual(_pointerPosition, _3RdPersonPos))
                    {
                        _pointerPosition = _3RdPersonPos;
                        InitPointerOffset(0.05);
                    }
                }
            }
            else if (!MyUtils.IsEqual(_pointerPosition, _3RdPersonPos))
            {
                _pointerPosition = _3RdPersonPos;
                InitPointerOffset(0.05);
            }

            SelectTarget(manualSelect: false);


            if (s.Tick - _lastDrawTick > 1 && _delay++ < 10 || s.HudHandlers.Count > 0 && s.HudUi.RestrictHudHandlers(s.TrackingAi, s.PlayerId, Hud.Hud.HudMode.Selector)) return;
            
            _delay = 0;
            _lastDrawTick = s.Tick;
            
            if (enableActivator)
                MyTransparentGeometry.AddBillboardOriented(_reticle, _reticleColor, offetPosition, s.CameraMatrix.Left, s.CameraMatrix.Up, (float)PointerAdjScale, BlendTypeEnum.PostPP);


            Vector3D targetCenter;
            if (GetSelectableEntity(out targetCenter, out SelectedEntity))
            {
                if (Vector3D.Transform(targetCenter, s.Camera.ViewMatrix).Z < 0 && _reticleColor != Color.White)
                {
                    var screenPos = s.Camera.WorldToScreen(ref targetCenter);
                    var dotpos = new Vector2D(MathHelper.Clamp(screenPos.X, -0.98, 0.98), MathHelper.Clamp(screenPos.Y, -0.98, 0.98));
                    var screenScale = 0.1 * s.ScaleFov;
                    dotpos.X *= (float)(screenScale * Session.I.AspectRatio);
                    dotpos.Y *= (float)screenScale;
                    screenPos = Vector3D.Transform(new Vector3D(dotpos.X, dotpos.Y, -0.1), s.CameraMatrix);
                    var radius = (float)(screenScale * MarkerSize());
                    MyTransparentGeometry.AddBillboardOriented(_targetCircle, _reticleColor, screenPos, s.CameraMatrix.Left, s.CameraMatrix.Up, radius, BlendTypeEnum.PostPP);
                }

                if (_3RdPersonDraw == ThirdPersonModes.DotTarget)
                {
                    var handHit = s.UiInput.PlayerWeapon && HandHitMarkerActive;
                    var scaler = handHit ? 0.75f : s.UiInput.TurretBlockView ? 0.5f : 0.35f;
                    if (handHit)
                        _reticleColor = Color.White;
                    
                    MyTransparentGeometry.AddBillboardOriented(_whiteDot, _reticleColor, offetPosition, s.CameraMatrix.Left, s.CameraMatrix.Up, (float)PointerAdjScale * scaler, BlendTypeEnum.PostPP);
                }
            }
            else if (!enableActivator && _3RdPersonDraw != ThirdPersonModes.None)
            {
                var scaler = s.UiInput.TurretBlockView ? 0.5f : 0.35f;
                MyTransparentGeometry.AddBillboardOriented(_whiteDot, _reticleColor, offetPosition, s.CameraMatrix.Left, s.CameraMatrix.Up, (float)PointerAdjScale * scaler, BlendTypeEnum.PostPP);
            }

            DrawReticle = true;
        }

        private void DrawDroneNotice()
        {
            var s = Session.I;
            Vector3D offset;
            Vector2 localOffset;

            float scale;
            float screenScale;
            float fontScale;
            MyStringId textureName;

            var hudOpacity = MathHelper.Clamp(Session.I.UiHudOpacity, 0.25f, 1f);
            var color = new Vector4(1, 1, 1, hudOpacity);

            _alertHudInfo.GetTextureInfo(s, out textureName, out scale, out screenScale, out fontScale, out offset, out localOffset);

            MyTransparentGeometry.AddBillboardOriented(textureName, color, offset, s.CameraMatrix.Left, s.CameraMatrix.Up, screenScale, BlendTypeEnum.PostPP);
            string textLine1;
            string textLine2;
            Vector2 textOffset1;
            Vector2 textOffset2;
            Vector4 text1Color;
            Vector4 text2Color;
            if (s.Tick20 && DroneText(localOffset, scale, out textLine1, out textLine2, out textOffset1, out textOffset2, out text1Color, out text2Color))
            {
                var fontSize = (float)Math.Round(24 * fontScale, 2);
                var fontHeight = 0.75f;
                var fontAge = 18;
                var fontJustify = Hud.Hud.Justify.None;
                var fontType = Hud.Hud.FontType.Shadow;
                var elementId = 1;

                s.HudUi.AddText(text: textLine1, x: textOffset1.X, y: textOffset1.Y, elementId: elementId, ttl: fontAge, color: text1Color, justify: fontJustify, fontType: fontType, fontSize: fontSize, heightScale: fontHeight);
                s.HudUi.AddText(text: textLine2, x: textOffset2.X, y: textOffset2.Y, elementId: elementId + 1, ttl: fontAge, color: text2Color, justify: fontJustify, fontType: fontType, fontSize: fontSize, heightScale: fontHeight);
            }
        }

        private void DrawActiveMarks()
        {
            var s = Session.I;

            var time = s.Tick % 20; // forward and backward total time
            var increase = time < 10;
            var directionalTimeStep = increase ? time : 19 - time;
            var textureMap = s.HudUi.PaintedTexture[directionalTimeStep];
            var colorStep = s.Tick % 120;
            var amplify = colorStep <= 60;
            var modifyStep = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            var cMod1 = MathHelper.Clamp(amplify ? (colorStep * modifyStep) : 2 - (+(colorStep * modifyStep)), 0.1f, 1f);
            var left = (Vector3)s.CameraMatrix.Left;
            var up = (Vector3)s.CameraMatrix.Up;
            var scale = s.Settings.ClientConfig.HudScale;
            var screenScale = 0.1 * s.ScaleFov;
            var size = (float)((0.0025f * scale) * s.ScaleFov);
            var invScaler = MathHelper.Clamp(1 / s.ScaleFov, 0, 20);
            var fontScale = scale * s.ScaleFov;
            var invScaleLimit = 10;
            if (invScaler >= invScaleLimit)
            {
                fontScale *= (invScaler / invScaleLimit);
                size *= (float)(invScaler / invScaleLimit);
                invScaler = MathHelper.Clamp(20f / invScaler, 1, 20);
            }

            var fontYOffset = (float)((-0.05f * scale) * invScaler);
            var element = 0;
            for (int i = 0; i < s.ActiveMarks.Count; i++)
            {
                var mark = s.ActiveMarks[i];

                var player = mark.Item1;
                var repColor = mark.Item2;
                var fakeTarget = mark.Item3;
                if (fakeTarget.EntityId == 0)
                    continue;

                var textColor = new Vector4(repColor.X, repColor.Y, repColor.Z, cMod1 * repColor.W);
                var fakeTargInfo = fakeTarget.GetFakeTargetInfo(s.TrackingAi);
                if (fakeTargInfo == null)
                    continue;
                var targetCenter = fakeTargInfo.WorldPosition;
                var viewSphere = new BoundingSphereD(targetCenter, 100f);
                if (!s.Camera.IsInFrustum(ref viewSphere))
                    continue;

                var screenPos = s.Camera.WorldToScreen(ref targetCenter);

                Vector3D drawPos = screenPos;
                if (Vector3D.Transform(targetCenter, s.Camera.ViewMatrix).Z > 0)
                {
                    drawPos.X *= -1;
                    drawPos.Y = -1;
                }

                var dotpos = new Vector2D(MathHelper.Clamp(drawPos.X, -0.98, 0.98), MathHelper.Clamp(drawPos.Y, -0.98, 0.98));
                dotpos.X *= (float)(screenScale * Session.I.AspectRatio);
                dotpos.Y *= (float)screenScale;
                drawPos = Vector3D.Transform(new Vector3D(dotpos.X, dotpos.Y, -0.1), s.CameraMatrix);


                MyQuadD quad;
                MyUtils.GetBillboardQuadOriented(out quad, ref drawPos, size, size, ref left, ref up);
                MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureMap.P0, textureMap.P1, textureMap.P3, textureMap.Material, 0, drawPos, repColor, BlendTypeEnum.PostPP);
                MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureMap.P0, textureMap.P2, textureMap.P3, textureMap.Material, 0, drawPos, repColor, BlendTypeEnum.PostPP);

                string textLine1 = player.DisplayName;
                var fontSize = (float)Math.Round(10 * fontScale, 2);
                var fontHeight = 0.75f;
                var fontAge = -1;
                var fontJustify = Hud.Hud.Justify.Center;
                var fontType = Hud.Hud.FontType.Shadow;
                var elementId = 1000 + element++;
                s.HudUi.AddText(text: textLine1, x: (float)screenPos.X, y: (float)screenPos.Y + fontYOffset, elementId: elementId, ttl: fontAge, color: textColor, justify: fontJustify, fontType: fontType, fontSize: fontSize, heightScale: fontHeight);
            }
        }
        private readonly List<LeadInfo> _leadInfos = new List<LeadInfo>();

        private void DrawActiveLeads()
        {
            var s = Session.I;
            var focus = s.TrackingAi.Construct.Data.Repo.FocusData;

            MyEntity target;
            if (!MyEntities.TryGetEntityById(focus.Target, out target) && target.Physics == null)
                return;

            var targetSphere = target.PositionComp.WorldVolume;

            float maxLeadLength;
            Vector3D fullAveragePos;

            if (!ComputeLead(target, targetSphere.Center, out maxLeadLength, out fullAveragePos))
                return;

            
            var lineStart = targetSphere.Center;
            var lineNormDir = Vector3D.Normalize(fullAveragePos - lineStart);
            var ray = new RayD(lineStart, lineNormDir);

            var rayDist = s.CameraFrustrum.Intersects(ray);
            if (!s.Camera.IsInFrustum(ref targetSphere) || rayDist == null)
                return;

            var lineEnd = lineStart + (lineNormDir * maxLeadLength);
            var endScreenPos = s.Camera.WorldToScreen(ref lineEnd);

            var worldLine = new LineD(lineEnd, lineStart, maxLeadLength);

            var obb = new MyOrientedBoundingBoxD(target.PositionComp.LocalAABB, target.PositionComp.WorldMatrixRef);
            var lineLength = obb.Intersects(ref worldLine) ?? 0;
            if (lineLength < 0.2f)
                return;

            var lineScale = (float)(0.1 * s.ScaleFov);

            var startScreenPos = s.Camera.WorldToScreen(ref lineStart);
            var scaledAspect = lineScale * Session.I.AspectRatio;

            var scale = s.Settings.ClientConfig.HudScale;
            var fontScale = scale * s.ScaleFov;
            Vector3D fursthestScreenPos = Vector3D.Zero;
            double furthestDist = 0;
            var element = 0;

            for (int i = 0; i < _leadInfos.Count; i++)
            {
                var info = _leadInfos[i];

                if (obb.Contains(ref info.Position))
                    continue;

                var screenPos = s.Camera.WorldToScreen(ref info.Position);
                var lockedScreenPos = MyUtils.GetClosestPointOnLine(ref startScreenPos, ref endScreenPos, ref screenPos);

                var distSqr = Vector3D.DistanceSquared(lockedScreenPos, startScreenPos);
                if (distSqr > furthestDist)
                {
                    furthestDist = distSqr;
                    fursthestScreenPos = lockedScreenPos;
                }

                var textColor = !info.WillHit ? new Vector4(1, 1, 1, 1) : new Vector4(1, 0.025f, 0.025f, 1);
                var leadChannel = i + 1;

                var textLine1 = string.Concat("  ", leadChannel.ToString());
                var fontFocusSize = !info.WillHit ? 13 : 15;
                var fontSize = (float)Math.Round(fontFocusSize * fontScale, 2);
                var fontHeight = 0.75f;
                var fontAge = -1;
                var fontJustify = Hud.Hud.Justify.Center;
                var fontType = Hud.Hud.FontType.Shadow;
                var elementId = 2000 + element++;
                s.HudUi.AddText(text: textLine1, x: (float)lockedScreenPos.X, y: (float)lockedScreenPos.Y, elementId: elementId, ttl: fontAge, color: textColor, justify: fontJustify, fontType: fontType, fontSize: fontSize, heightScale: fontHeight);
            }

            var endDotPos = new Vector2D(fursthestScreenPos.X, fursthestScreenPos.Y);
            endDotPos.X *= scaledAspect;
            endDotPos.Y *= lineScale;

            var lineEndScreenPos = Vector3D.Transform(new Vector3D(endDotPos.X, endDotPos.Y, -0.1), s.CameraMatrix);

            var culledLineStart = lineEnd - (lineNormDir * lineLength);
            var culledStartScreenPos = s.Camera.WorldToScreen(ref culledLineStart);

            var culledStartDotPos = new Vector2D(culledStartScreenPos.X, culledStartScreenPos.Y);
            culledStartDotPos.X *= scaledAspect;
            culledStartDotPos.Y *= lineScale;

            var lineStartScreenPos = Vector3D.Transform(new Vector3D(culledStartDotPos.X, culledStartDotPos.Y, -0.1), s.CameraMatrix);

            var lineColor = new Vector4(0.5f, 0.5f, 1, 1);
            var lineMagnitude = lineEndScreenPos - lineStartScreenPos;

            MyTransparentGeometry.AddLineBillboard(_laserLine, lineColor, lineStartScreenPos, lineMagnitude, 1f, lineScale * 0.005f);

            var avgScreenPos = s.Camera.WorldToScreen(ref fullAveragePos);
            var avgLockedScreenPos = MyUtils.GetClosestPointOnLine(ref startScreenPos, ref endScreenPos, ref avgScreenPos);

            var dotpos = new Vector2D(avgLockedScreenPos.X, avgLockedScreenPos.Y);
            dotpos.X *= scaledAspect;
            dotpos.Y *= lineScale;
            avgLockedScreenPos = Vector3D.Transform(new Vector3D(dotpos.X, dotpos.Y, -0.1), s.CameraMatrix);

            var size = (float)((0.00225f * scale) * s.ScaleFov);
            var left = (Vector3)s.CameraMatrix.Left;
            var up = (Vector3)s.CameraMatrix.Up;
            var repColor = new Vector4(1, 1, 1, 1);
            var time = s.Tick % 20; // forward and backward total time
            var increase = time < 10;
            var directionalTimeStep = increase ? time : 19 - time;
            var textureMap = s.HudUi.PaintedTexture[directionalTimeStep];

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref avgLockedScreenPos, size, size, ref left, ref up);
            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureMap.P0, textureMap.P1, textureMap.P3, textureMap.Material, 0, avgLockedScreenPos, repColor, BlendTypeEnum.PostPP);
            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureMap.P0, textureMap.P2, textureMap.P3, textureMap.Material, 0, avgLockedScreenPos, repColor, BlendTypeEnum.PostPP);

            _leadInfos.Clear();
        }

        private void DrawTarget()
        {
            var s = Session.I;

            var ai = s.TrackingAi;
            var focus = ai.Construct.Data.Repo.FocusData;
            var detailedHud = !s.Settings.ClientConfig.MinimalHud && (s.Settings.ClientConfig.AdvancedMode || s.MinimalHudOverride);
            var element = 0;

            if (focus.Target == 0) return;
            var lockMode = focus.Locked;

            var targetState = ai.TargetState;
            var shielded = detailedHud && targetState.ShieldHealth >= 0;

            var collection = detailedHud ? _primaryTargetHuds : _primaryMinimalHuds;
            var infoKey = s.UiInput.InfoKey == MyKeys.Decimal ? MyKeys.Delete : s.UiInput.InfoKey;
            var drawInfo = UpdateKeyInfo(detailedHud);
            var dumbHandHeld = s.UiInput.PlayerWeapon && !ai.SmartHandheld && s.LeadGroupActive;

            var handheldHud = (s.UiInput.IronLock || dumbHandHeld);
            var showHud = !s.Settings.Enforcement.DisableHudTargetInfo && (!s.UiInput.PlayerWeapon || handheldHud) && !(s.HudHandlers.Count > 0 && s.HudUi.RestrictHudHandlers(ai, Session.I.PlayerId, Hud.Hud.HudMode.TargetInfo));

            if (showHud)
            {
                foreach (var hud in collection.Keys)
                {
                    if ((hud == InactiveShield || hud == InactiveNoShield))
                        continue;

                    if (shielded && (hud == ActiveNoShield || hud == InactiveNoShield))
                        continue;
                    if (!shielded && (hud == ActiveShield || hud == InactiveShield))
                        continue;

                    Vector3D offset;
                    Vector2 localOffset;

                    float scale;
                    float screenScale;
                    float fontScale;
                    MyStringId textureName;
                    var hudInfo = collection[hud];
                    hudInfo.GetTextureInfo(s, out textureName, out scale, out screenScale, out fontScale, out offset, out localOffset);

                    var color = Color.White;
                    var lockColor = Color.White;

                    switch (lockMode)
                    {
                        case FocusData.LockModes.None:
                            lockColor = Color.White;
                            break;
                        case FocusData.LockModes.Locked:
                            lockColor = Color.White;
                            break;
                    }
                    MyTransparentGeometry.AddBillboardOriented(textureName, color, offset, s.CameraMatrix.Left, s.CameraMatrix.Up, screenScale, BlendTypeEnum.PostPP);

                    for (int j = 0; j < 11; j++)
                    {
                        string text;
                        Vector2 textOffset;
                        if (TargetTextStatus(j, targetState, scale, localOffset, shielded, detailedHud, out text, out textOffset))
                        {
                            var textColor = j != 10 ? Color.White : lockColor;
                            var fontSize = (float)Math.Round(21 * fontScale, 2);
                            var fontHeight = 0.75f;
                            var fontAge = -1;
                            var fontJustify = Hud.Hud.Justify.None;
                            var fontType = Hud.Hud.FontType.Shadow;
                            var elementId = 3000 + element++;

                            if (j == 10 && drawInfo)
                            {
                                textColor = s.Count < 60 ? Color.White : Color.DarkGray;
                                text = InfoKeyStr + infoKey;
                            }

                            s.HudUi.AddText(text: text, x: textOffset.X, y: textOffset.Y, elementId: elementId, ttl: fontAge, color: textColor, justify: fontJustify, fontType: fontType, fontSize: fontSize, heightScale: fontHeight);

                        }
                    }
                }
            }

            MyEntity target;
            if (MyEntities.TryGetEntityById(focus.Target, out target))
            {

                var targetSphere = target.PositionComp.WorldVolume;
                var targetCenter = targetSphere.Center;
                var screenPos = s.Camera.WorldToScreen(ref targetCenter);

                if (Vector3D.Transform(targetCenter, s.Camera.ViewMatrix).Z > 0)
                {
                    screenPos.X *= -1;
                    screenPos.Y = -1;
                }

                var dotpos = new Vector2D(MathHelper.Clamp(screenPos.X, -0.98, 0.98), MathHelper.Clamp(screenPos.Y, -0.98, 0.98));
                var screenScale = 0.1 * s.ScaleFov;
                dotpos.X *= (float)(screenScale * Session.I.AspectRatio);
                dotpos.Y *= (float)screenScale;
                screenPos = Vector3D.Transform(new Vector3D(dotpos.X, dotpos.Y, -0.1), s.CameraMatrix);

                var radius = (float) (screenScale * MarkerSize());
                MyTransparentGeometry.AddBillboardOriented(_targetCircle, Color.White, screenPos, s.CameraMatrix.Left, s.CameraMatrix.Up, radius, BlendTypeEnum.PostPP);
            }
        }

        private bool UpdateKeyInfo(bool detailedHud)
        {
            var s = Session.I;
            var drawAge = s.Tick - s.TargetLastDrawTick;
            var infoExpire = 420;

            s.TargetDrawAge = drawAge > infoExpire && !s.TargetInfoKeyLock ? 0 : s.TargetDrawAge + 1;
            s.TargetLastDrawTick = s.Tick;
            var drawInfo = !detailedHud && s.TargetDrawAge <= infoExpire;
            s.TargetInfoKeyLock = !drawInfo;
            return drawInfo;
        }

        internal void SetHit(ProInfo info)
        {
            if (Session.I.TrackingAi.AiType == Ai.AiTypes.Player)
            {
                if (Session.I.UiInput.FirstPersonView)
                    CheckHandWeaponEntityHit(info);
                return;
            }

            var focus = Session.I.TrackingAi.Construct.Data.Repo.FocusData;
            MyEntity target;
            if (focus.Target != 0 && MyEntities.TryGetEntityById(focus.Target, out target) && CheckBlockWeaponEntityHit(target, info))
                    return;

            if (LastSelectedEntity != null)
                CheckBlockWeaponEntityHit(LastSelectedEntity, info);
        }

        private bool CheckBlockWeaponEntityHit(MyEntity ent, ProInfo info)
        {
            var grid = ent as MyCubeGrid;

            foreach (var hitEnt in info.HitList)
            {
                var hitGrid = hitEnt.Entity as MyCubeGrid;
                var bothGrids = grid != null && hitGrid != null;

                var check = hitEnt.Entity == ent || bothGrids && grid.IsSameConstructAs(hitGrid);

                if (!check && hitEnt.EventType == HitEntity.Type.Shield && bothGrids)
                {
                    var shield = Session.I.SApi.MatchEntToShieldFast(hitGrid, true); 
                    if (shield != null)
                        check = grid.IsSameConstructAs(shield.CubeGrid);
                }

                if (!check) continue;
                HitMarkerColor = Color.White;
                HitIncrease = FullPulseSize - CircleSize;
                return true;
            }

            return false;
        }

        private void CheckHandWeaponEntityHit(ProInfo info)
        {
            foreach (var hitEnt in info.HitList)
            {
                if (info.Weapon.Comp.Ai.Construct.RootAi != Session.I.TrackingAi || hitEnt.Entity is MyVoxelBase)
                    continue;

                HandHitIncrease = HandFullPulseSize - HandCircleSize;
                HandHitMarkerActive = true;
                return;
            }
        }

        private void HandWeaponMarker()
        {
            var s = Session.I;
            var offetPosition = Vector3D.Transform(HandPointerOffset, s.CameraMatrix);
            var screenScale = (s.UiInput.IronSights ? 0.0375 : 0.025) * s.ScaleFov;
            var radius = (float)(screenScale * HandMarkerSize());
            MyTransparentGeometry.AddBillboardOriented(_targetCircle, HandHitMarkerColor, offetPosition, s.CameraMatrix.Left, s.CameraMatrix.Up, radius, BlendTypeEnum.PostPP);
        }

        private double MarkerSize()
        {
            var increase = HitIncrease;

            if (HitIncrease > PulseSize)
            {
                HitIncrease = !MyUtils.IsZero(HitIncrease - PulseSize) ? HitIncrease - PulseSize : 0d;
            }
            else
            {
                HitIncrease = 0d;
            }

            return CircleSize + increase;
        }

        private double HandMarkerSize()
        {
            var increase = HandHitIncrease;

            if (HandHitIncrease > HandPulseSize)
            {
                HandHitIncrease = !MyUtils.IsZero(HandHitIncrease - HandPulseSize) ? HandHitIncrease - HandPulseSize : 0d;
            }
            else
            {
                HandHitIncrease = 0d;
                HandHitMarkerActive = false;
            }

            return HandCircleSize + increase;
        }

        private bool TargetTextStatus(int slot, TargetStatus targetState, float scale, Vector2 localOffset, bool shielded, bool details, out string textStr, out Vector2 textOffset)
        {
            var showAll = details && shielded;
            var minimal = !details;
            var skipShield = !showAll && !minimal;
            var skip = minimal && slot != 0 && slot != 1 && slot != 10 || skipShield && slot > 5 && slot != 10;
            textStr = string.Empty;

            if (skip)
            {
                textOffset = Vector2.Zero;
                return false;
            }

            textOffset = localOffset;

            var aspectScale = (2.37037f / Session.I.AspectRatio);

            var xOdd = 0.3755f * scale;
            var xEven = 0.07f * scale;
            var xCenter = 0.19f * scale;
            var yStart = 0.9f * scale;
            var yStep = 0.151f * scale;

            switch (slot)
            {
                case 0:
                    var speed = MathHelper.Clamp(targetState.Speed, 0, int.MaxValue);
                    var inKmspeed = speed >= 1000;
                    var unitspeed = inKmspeed ? "km/s" : "m/s";
                    var measurespeed = inKmspeed ? speed / 1000 : speed;
                    textStr = $"SPEED: {measurespeed:#.0} {unitspeed}";
                    textOffset.X -= xOdd * aspectScale;
                    textOffset.Y += yStart;
                    break;
                case 1:
                    var inKm = targetState.RealDistance >= 1000;
                    var unit = inKm ? "km" : "m";
                    var measure = inKm ? targetState.RealDistance / 1000 : targetState.RealDistance;
                    textStr = $"RANGE: {measure:#.0} {unit}";
                    textOffset.X += xEven * aspectScale;
                    textOffset.Y += yStart;
                    break;
                case 2:
                    var inKmsize = targetState.SizeExtended >= 1000;
                    var unitsize = inKmsize ? "km" : "m";
                    var measuresize = inKmsize ? targetState.SizeExtended / 1000 : targetState.SizeExtended;
                    textStr = $"SIZE: {measuresize:#.0} {unitsize}";
                    textOffset.X -= xOdd * aspectScale;
                    textOffset.Y += yStart - (yStep * 1);
                    break;
                case 3:
                    if (targetState.Speed < 1)
                        textStr = "STATIONARY";
                    else if (targetState.Engagement == 0)
                        textStr = "INTERCEPT";
                    else if (targetState.Engagement == 1)
                        textStr = "RETREATING";
                    else
                        textStr = "PARALLEL";

                    textOffset.X += xEven * aspectScale;
                    textOffset.Y += yStart - (yStep * 1);
                    break;
                case 4:
                    var threatLvl = targetState.ThreatLvl > 0 ? targetState.ThreatLvl : 0;
                    textStr = $"THREAT: {threatLvl}";
                    textOffset.X -= xOdd * aspectScale;
                    textOffset.Y += yStart - (yStep * 2);
                    break;
                case 5:
                    textStr = targetState.Aware;
                    textOffset.X += xEven * aspectScale;
                    textOffset.Y += yStart - (yStep * 2);
                    break;
                case 6:
                    var hp = targetState.ShieldHealth < 0 ? 0 : targetState.ShieldHealth;
                    textStr = $"SHIELD HP: {hp}%";
                    textOffset.X -= xOdd * aspectScale;
                    textOffset.Y += yStart - (yStep * 3);
                    break;
                case 7:
                    var type = targetState.ShieldMod > 0 ? "ENERGY" : targetState.ShieldMod < 0 ? "KINETIC" : "NEUTRAL";
                    var value = !MyUtils.IsZero(targetState.ShieldMod) ? Math.Round(Math.Abs(targetState.ShieldMod), 2) : 1;
                    textStr = $"{type}: {value}x";
                    textOffset.X += xEven * aspectScale;
                    textOffset.Y += yStart - (yStep * 3);
                    break;
                case 8:
                    textStr = ShieldSides(targetState.ShieldFaces);
                    textOffset.X -= xOdd * aspectScale;
                    textOffset.Y += yStart - (yStep * 4);
                    break;
                case 9:
                    var reduction = ExpChargeReductions[targetState.ShieldHeat];
                    textStr = $"CHARGE RATE: {Math.Round(1f / reduction, 1)}x";
                    textOffset.X += xEven * aspectScale;
                    textOffset.Y += yStart - (yStep * 4);
                    break;
                case 10:
                    textStr = targetState.Name;
                    textOffset.X -= xCenter * aspectScale;
                    if (minimal)
                        textOffset.Y += yStart - (yStep * 1);
                    else if (shielded)
                        textOffset.Y += yStart - (yStep * 5);
                    else
                        textOffset.Y += yStart - (yStep * 3);
                    break;
                default:
                    textStr = string.Empty;
                    textOffset = Vector2.Zero;
                    return false;
            }

            if (textStr == null)
                textStr = string.Empty;

            return true;
        }

        private bool DroneText(Vector2 localOffset, float scale, out string text1, out string text2, out Vector2 offset1, out Vector2 offset2, out Vector4 color1, out Vector4 color2)
        {
            var aspectScale = (2.37037f / Session.I.AspectRatio);
            var xCenter = 0.22f * scale;
            var yStart = 0.885f * scale;
            var yStep = 0.12f * scale;

            offset1 = localOffset;
            offset1.X -= xCenter * aspectScale;
            offset1.Y += yStart;
            offset2.X = offset1.X;
            offset2.Y = offset1.Y - yStep;

            text1 = $"Incoming drones detected!";
            text2 = $"              Threats: {Session.I.TrackingAi.Construct.RootAi.Construct.DroneCount}";

            color1 = new Vector4(1, 1, 1, 1);
            color2 = Session.I.Count < 60 ? new Vector4(1, 1, 1, 1) : new Vector4(1, 0, 0, 1);
            return true;
        }


        public string ShieldSides(Vector3I shunts)
        {
            string text = string.Empty;
            var left = shunts.X == -1 || shunts.X == 2;
            var right = shunts.X == 1 || shunts.X == 2;
            var up = shunts.Y == 1 || shunts.Y == 2;
            var down = shunts.Y == -1 || shunts.Y == 2;
            var forward = shunts.Z == -1 || shunts.Y == 2;
            var backward = shunts.Z == 1 || shunts.Y == 2;

            if (forward || backward)
            {

                var both = forward && backward;

                if (both)
                {
                    text += "FR:BA:";
                }
                else if (forward)
                    text += "FR:";
                else
                    text += "BA:";
            }

            if (up || down)
            {

                var both = up && down;

                if (both)
                {
                    text += "TO:BO:";
                }
                else if (up)
                    text += "TO:";
                else
                    text += "BO:";
            }

            if (left || right)
            {

                var both = left && right;

                if (both)
                {
                    text += "LE:RI:";
                }
                else if (left)
                    text += "LE:";
                else
                    text += "RI:";
            }
            return text;
        }

        private bool ComputeLead(MyEntity target, Vector3D targetPos, out float maxLeadLength, out Vector3D fullAveragePos)
        {
            _leadInfos.Clear();
            maxLeadLength = 0;
            fullAveragePos = Vector3D.Zero;
            for (var gId = 0; gId < Session.I.LeadGroups.Length; gId++)
            {

                var group = Session.I.LeadGroups[gId];
                var leadAvg = Vector3D.Zero;
                var somethingWillHit = false;
                var addLead = 0;

                for (var i = 0; i < group.Count; i++)
                {

                    var w = group[i];
                    var invalid = w.Comp.IsBlock && (!w.Comp.Cube.IsWorking || w.Comp.Cube.MarkedForClose || w.Comp.Cube.CubeGrid.MarkedForClose) || w.Comp.CoreEntity.MarkedForClose || w.Comp.TopEntity.MarkedForClose;
                    if (invalid)
                        continue;
                    Vector3D predictedPos;
                    bool canHit;
                    bool willHit;
                    Weapon.LeadTarget(w, target, out predictedPos, out canHit, out willHit);

                    if (canHit)
                    {

                        ++addLead;
                        leadAvg += predictedPos;
                        if (!somethingWillHit && willHit)
                        {
                            somethingWillHit = true;
                        }
                    }

                    if (i == group.Count - 1 && !MyUtils.IsZero(leadAvg))
                    {

                        leadAvg /= addLead;
                        var leadLength = Vector3.Distance(leadAvg, targetPos);
                        if (leadLength > maxLeadLength)
                            maxLeadLength = leadLength;

                        fullAveragePos += leadAvg;

                        _leadInfos.Add(new LeadInfo { Group = gId, Position = leadAvg, Length = leadLength, WillHit = somethingWillHit });
                    }
                }
            }

            if (_leadInfos.Count > 0)
                fullAveragePos /= _leadInfos.Count;

            return _leadInfos.Count != 0;
        }

        internal bool GetTargetState(Session s)
        {
            var ai = s.TrackingAi;
            var maxNameLength = 21;

            if (s.Tick - MasterUpdateTick > 120 || MasterUpdateTick < 120 && _masterTargets.Count == 0)
                BuildMasterCollections(ai);
            var targetId = ai.Construct.Data.Repo.FocusData.Target;
            MyTuple<float, TargetControl, MyRelationsBetweenPlayerAndBlock> targetInfo;
            MyEntity target;
            if (targetId == 0 || !MyEntities.TryGetEntityById(targetId, out target) || !_masterTargets.TryGetValue(target, out targetInfo)) 
                return false;

            if (!s.Tick20) return true;
            var grid = target as MyCubeGrid;
            var partCount = 1;
            var largeGrid = false;
            Ai targetAi = null;
            if (grid != null)
            {
                largeGrid = grid.GridSizeEnum == MyCubeSize.Large;
                TopMap topMap;
                if (s.EntityToMasterAi.TryGetValue(grid, out targetAi))
                    partCount = targetAi.Construct.BlockCount;
                else if (s.TopEntityToInfoMap.TryGetValue(grid, out topMap))
                    partCount = topMap.MostBlocks;
            }

            var state = ai.TargetState;

            state.Aware = targetAi != null ? AggressionState(ai, targetAi) : "UNARMED";
            var displayName = target.DisplayName;
            var name = string.IsNullOrEmpty(displayName) ? string.Empty : displayName.Length <= maxNameLength ? displayName : displayName.Substring(0, maxNameLength);
            var targetVel = target.Physics?.LinearVelocity ?? Vector3.Zero;
            if (MyUtils.IsZero(targetVel, 1E-01F)) targetVel = Vector3.Zero;
            var targetDir = Vector3D.Normalize(targetVel);
            var targetRevDir = -targetDir;
            var targetPos = target.PositionComp.WorldAABB.Center;
            var myPos = ai.TopEntity.PositionComp.WorldAABB.Center;
            var myHeading = Vector3D.Normalize(myPos - targetPos);

            var intercept = MathFuncs.IsDotProductWithinTolerance(ref targetDir, ref myHeading, s.ApproachDegrees);
            var retreat = MathFuncs.IsDotProductWithinTolerance(ref targetRevDir, ref myHeading, s.ApproachDegrees);

            var distanceFromCenters = Vector3D.Distance(ai.TopEntity.PositionComp.WorldAABB.Center, target.PositionComp.WorldAABB.Center);
            distanceFromCenters -= ai.TopEntity.PositionComp.LocalVolume.Radius;
            distanceFromCenters -= target.PositionComp.LocalVolume.Radius;
            distanceFromCenters = distanceFromCenters <= 0 ? 0 : distanceFromCenters;

            var speed = (float)Math.Round(target.Physics?.Speed ?? 0, 1);

            state.Name = name;

            state.RealDistance = distanceFromCenters;

            state.SizeExtended = (float)(target.PositionComp.WorldVolume.Radius * 2);

            state.Speed = speed;

            if (intercept) state.Engagement = 0;
            else if (retreat) state.Engagement = 1;
            else state.Engagement = 2;

            MyTuple<bool, bool, float, float, float, int> shieldInfo = new MyTuple<bool, bool, float, float, float, int>();
            if (s.ShieldApiLoaded) shieldInfo = s.SApi.GetShieldInfo(target);
            if (shieldInfo.Item1)
            {
                var modInfo = s.SApi.GetModulationInfo(target);
                var modValue = MyUtils.IsEqual(modInfo.Item3, modInfo.Item4) ? 0 : modInfo.Item3 > modInfo.Item4 ? modInfo.Item3 : -modInfo.Item4;
                var faceInfo = s.SApi.GetFacesFast(target);
                state.ShieldFaces = faceInfo.Item1 ? faceInfo.Item2 : Vector3I.Zero;
                state.ShieldHeat = shieldInfo.Item6 / 10;
                state.ShieldMod = modValue;
                state.ShieldHealth = (float)Math.Round(shieldInfo.Item5);
            }
            else
            {
                state.ShieldHeat = 0;
                state.ShieldMod = 0;
                state.ShieldHealth = -1;
                state.ShieldFaces = Vector3I.Zero;
            }

            var friend = false;
            if (grid != null && grid.BigOwners.Count != 0)
            {
                var relation = MyIDModule.GetRelationPlayerBlock(ai.AiOwner, grid.BigOwners[0], MyOwnershipShareModeEnum.Faction);
                if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.Friends) friend = true;
            }

            if (friend) state.ThreatLvl = -1;
            else
            {
                int shieldBonus = 0;
                if (s.ShieldApiLoaded)
                {
                    var myShieldInfo = s.SApi.GetShieldInfo(ai.GridEntity);
                    if (shieldInfo.Item1 && myShieldInfo.Item1)
                        shieldBonus = shieldInfo.Item5 > myShieldInfo.Item5 ? 1 : -1;
                    else if (shieldInfo.Item1) shieldBonus = 1;
                    else if (myShieldInfo.Item1) shieldBonus = -1;
                }

                if (targetInfo.Item1 > 5) state.ThreatLvl = shieldBonus < 0 ? 8 : 9;
                else if (targetInfo.Item1 > 4) state.ThreatLvl = 8 + shieldBonus;
                else if (targetInfo.Item1 > 3) state.ThreatLvl = 7 + shieldBonus;
                else if (targetInfo.Item1 > 2) state.ThreatLvl = 6 + shieldBonus;
                else if (targetInfo.Item1 > 1) state.ThreatLvl = 5 + shieldBonus;
                else if (targetInfo.Item1 > 0.5) state.ThreatLvl = 4 + shieldBonus;
                else if (targetInfo.Item1 > 0.25) state.ThreatLvl = 3 + shieldBonus;
                else if (targetInfo.Item1 > 0.125) state.ThreatLvl = 2 + shieldBonus;
                else if (targetInfo.Item1 > 0.0625) state.ThreatLvl = 1 + shieldBonus;
                else if (targetInfo.Item1 > 0) state.ThreatLvl = shieldBonus > 0 ? 1 : 0;
                else state.ThreatLvl = -1;
            }
            return true;
        }

        private string AggressionState(Ai ai, Ai targetAi)
        {

            if (targetAi.Construct.Data.Repo.FocusData.HasFocus)
            {
                var fd = targetAi.Construct.Data.Repo.FocusData;
                foreach (var sub in ai.SubGridCache)
                {
                    if (sub.EntityId == fd.Target)
                        return "FOCUSED ON YOU";
                }
            }
            var tracking = targetAi.Targets.ContainsKey(ai.TopEntity);

            if (tracking)
                return "TRACKING YOU";

            return "NOT TRACKING YOU";
        }
    }
}

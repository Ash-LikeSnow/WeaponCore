using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using static CoreSystems.Support.Ai;

namespace CoreSystems
{
    public partial class Session
    {
        internal bool CheckTarget(Ai ai)
        {
            if (!ai.Construct.Focus.ClientIsFocused(ai)) return false;
            if (ai != TrackingAi)
            {
                TrackingAi = null;
                return false;
            }

            return ai.Construct.Data.Repo.FocusData.HasFocus;
        }

        internal void SetTarget(MyEntity entity, Ai ai)
        {
            TrackingAi = ai;
            ai.Construct.Focus.RequestAddFocus(entity, ai, PlayerId);
        }

        internal bool UpdateLocalAiAndCockpit(bool force = false)
        {
            InGridAiBlock = false;
            ActiveControlBlock = ControlledEntity as MyCubeBlock;
            PlayerHandWeapon =  LocalCharacter?.EquippedTool as IMyAutomaticRifleGun;
            var cockPit = ControlledEntity as MyCockpit;
            if (cockPit != null && cockPit.EnableShipControl)
                ActiveCockPit = cockPit;
            else ActiveCockPit = null;

            PlayerControllerEntity oldControlId;
            var controlledEntity = ActiveCockPit ?? ActiveControlBlock ?? PlayerHandWeapon?.Owner;
            var topEntity = ActiveControlBlock != null ? controlledEntity?.GetTopMostParent() : controlledEntity;

            if (topEntity != null && EntityToMasterAi.TryGetValue(topEntity, out TrackingAi) && !TrackingAi.MarkedForClose && TrackingAi.Data.Repo != null)
            {
                var camera = Session.CameraController?.Entity as MyCameraBlock;
                if (camera == null || !GroupedCamera(camera))
                    ActiveCameraBlock = null;
                InGridAiBlock = true;

                TrackingAi.PlayerControl.TryGetValue(PlayerId, out oldControlId);

                if (oldControlId.EntityId != controlledEntity.EntityId || force)
                {
                    if (ActiveControlBlock is MyShipController)
                        CheckToolbarForVanilla(ActiveControlBlock);

                    SendActiveControlUpdate(TrackingAi, controlledEntity, true);
                    TargetLeadUpdate();

                }
                else if (LeadGroupsDirty || !MyUtils.IsEqual(LastOptimalDps, TrackingAi.Construct.OptimalDps))
                    TargetLeadUpdate();

                TrackingAi.PlayerControl[PlayerId] = new PlayerControllerEntity { ChangeTick = Tick, ControllEntity = controlledEntity, EntityId = controlledEntity.EntityId, Id = PlayerId };
            }
            else
            {
                if (TrackingAi?.Data.Repo != null)
                {
                    TrackingAi.Construct.Focus.ClientIsFocused(TrackingAi);

                    if (TrackingAi.PlayerControl.TryGetValue(PlayerId, out oldControlId))
                    {
                        if (IsServer) TrackingAi.Construct.NetRefreshAi();
                        SendActiveControlUpdate(TrackingAi, oldControlId.ControllEntity, false);
                        foreach (var list in LeadGroups) list.Clear();
                        LeadGroupActive = false;
                        TrackingAi.PlayerControl.Remove(PlayerId);
                    }
                }

                if (TrackingAi != null && GunnerBlackList)
                    GunnerRelease(PlayerId);

                TrackingAi = null;
                ActiveCockPit = null;
                ActiveControlBlock = null;
                PlayerHandWeapon = null;
                ActiveCameraBlock = null;
                TargetInfoKeyLock = false;
            }
            return InGridAiBlock;
        }

        private void TargetLeadUpdate()
        {
            LeadGroupActive = false;
            LeadGroupsDirty = false;

            LastOptimalDps = TrackingAi.Construct.OptimalDps;

            foreach (var list in LeadGroups)
                list.Clear();
            
            if (Settings.Enforcement.DisableLeads)
                return;

            foreach (var ai in TrackingAi.TopEntityMap.GroupMap.Ais)
            {
                if (ai.MarkedForClose)
                    continue;

                foreach (var comp in ai.WeaponComps)
                {
                    if (comp.CoreEntity == null || comp.CoreEntity.MarkedForClose || comp.Ai.ControlComp != null)
                        continue;

                    var collection = comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
                    foreach (var w in collection)
                    {
                        if ((!comp.HasTurret && !comp.OverrideLeads || comp.HasTurret && comp.OverrideLeads) && comp.Data.Repo != null && comp.Data.Repo.Values.Set.Overrides.LeadGroup > 0)
                        {
                            LeadGroups[MathHelper.Clamp(comp.Data.Repo.Values.Set.Overrides.LeadGroup - 1, 0, 3)].Add(w);
                            LeadGroupActive = true;
                        }
                    }
                }
            }
        }

        private bool GroupedCamera(MyCameraBlock camera)
        {
            long cameraGroupId;
            if (CameraChannelMappings.TryGetValue(camera, out cameraGroupId))
            {
                ActiveCameraBlock = camera;
                return true;
            }
            ActiveCameraBlock = null;
            return false;
        }

        internal void GunnerAcquire(MyEntity entity, long playerId)
        {
            if (PlayerId == -1)
                return;
            GunnerBlackList = true;
            var block = entity as MyCubeBlock;
            var rifle = entity as IMyAutomaticRifleGun;
            ActiveControlBlock = block ?? null;
            PlayerHandWeapon = rifle ?? null;
            var controlStringLeft = MyAPIGateway.Input.GetControl(MyMouseButtonsEnum.Left).GetGameControlEnum().String;
            CustomBlackListRequestBecauseKeenIsBrainDead(controlStringLeft, PlayerId);
            if (rifle == null)
            {
                var controlStringRight = MyAPIGateway.Input.GetControl(MyMouseButtonsEnum.Right).GetGameControlEnum().String;
                CustomBlackListRequestBecauseKeenIsBrainDead(controlStringRight, PlayerId);
            }
            else
            {
                var controlStringR = MyAPIGateway.Input.GetControl(UiInput.ControlKey).GetGameControlEnum().String;
                CustomBlackListRequestBecauseKeenIsBrainDead(controlStringR, PlayerId);
            }

            var controlStringMenu = MyAPIGateway.Input.GetControl(UiInput.MouseButtonMenu).GetGameControlEnum().String;
            CustomBlackListRequestBecauseKeenIsBrainDead(controlStringMenu, PlayerId);
        }

        internal void GunnerRelease(long playerId)
        {
            if (!GunnerBlackList || PlayerId == -1)
                return;

            GunnerBlackList = false;
            ActiveControlBlock = null;
            if (PlayerHandWeapon != null)
            {
                var controlStringR = MyAPIGateway.Input.GetControl(UiInput.ControlKey).GetGameControlEnum().String;
                CustomBlackListRequestBecauseKeenIsBrainDead(controlStringR, PlayerId, true);
            }
            PlayerHandWeapon = null;
            var controlStringLeft = MyAPIGateway.Input.GetControl(MyMouseButtonsEnum.Left).GetGameControlEnum().String;
            CustomBlackListRequestBecauseKeenIsBrainDead(controlStringLeft, PlayerId, true);
            var controlStringRight = MyAPIGateway.Input.GetControl(MyMouseButtonsEnum.Right).GetGameControlEnum().String;
            CustomBlackListRequestBecauseKeenIsBrainDead(controlStringRight, PlayerId, true);
            var controlStringMenu = MyAPIGateway.Input.GetControl(UiInput.MouseButtonMenu).GetGameControlEnum().String;
            CustomBlackListRequestBecauseKeenIsBrainDead(controlStringMenu, PlayerId, true);
        }

        internal void EntityControlUpdate()
        {
            var lastControlledEnt = ControlledEntity;
            ControlledEntity = (MyEntity)MyAPIGateway.Session.ControlledObject;

            var entityChanged = lastControlledEnt != null && lastControlledEnt != ControlledEntity;

            if (entityChanged)
            {
                if (ControlledEntity is MyCockpit || ControlledEntity is MyRemoteControl)
                    PlayerControlNotify(ControlledEntity);
            }
        }

        private void FovChanged()
        {
            HudUi.NeedsUpdate = true;
            TargetUi.ResetCache();
        }

        internal void RequestNotify(string message, int duration, string color = null, long specificPlayerId = 0, bool soundClick = false)
        {
            if (specificPlayerId == 0 && !DedicatedServer || specificPlayerId == PlayerId)
                ShowLocalNotify(message, duration, color, soundClick);
            else
                SendClientNotify(specificPlayerId, message, specificPlayerId != 0, color, duration, soundClick);
            
        }

        private void ShowClientNotify(ClientNotifyPacket notify)
        {
            MyAPIGateway.Utilities.ShowNotification(notify.Message, notify.Duration > 0 ? notify.Duration : 1000, notify.Color == string.Empty ? "White" : notify.Color);

            if (notify.SoundClick)
                MyVisualScriptLogicProvider.PlayHudSoundLocal();
        }

        internal void ShowLocalNotify(string message, int duration, string color = null, bool soundClick = false)
        {
            MyAPIGateway.Utilities.ShowNotification(message, duration, string.IsNullOrEmpty(color) ? "White" : color);

            if (soundClick)
                MyVisualScriptLogicProvider.PlayHudSoundLocal();
        }

        private readonly Color _restrictionAreaColor = new Color(128, 0, 128, 96);
        private readonly Color _uninitializedColor = new Color(255, 0, 0, 200);
        private BoundingSphereD _nearbyGridsTestSphere = new BoundingSphereD(Vector3D.Zero, 350);
        private readonly List<MyEntity> _gridsNearCamera = new List<MyEntity>();
        private readonly List<MyCubeBlock> _uninitializedBlocks = new List<MyCubeBlock>();
        private readonly List<Weapon.WeaponComponent> _debugBlocks = new List<Weapon.WeaponComponent>();
        private void DrawDisabledGuns()
        {
            if (Tick600 || Tick60 && QuickDisableGunsCheck)
            {

                QuickDisableGunsCheck = false;
                _nearbyGridsTestSphere.Center = CameraPos;
                _gridsNearCamera.Clear();
                _uninitializedBlocks.Clear();
                _debugBlocks.Clear();

                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref _nearbyGridsTestSphere, _gridsNearCamera);
                for (int i = _gridsNearCamera.Count - 1; i >= 0; i--)
                {
                    var grid = _gridsNearCamera[i] as MyCubeGrid;
                    if (grid?.Physics != null && !grid.MarkedForClose && !grid.IsPreview && !grid.Physics.IsPhantom)
                    {

                        var fatBlocks = grid.GetFatBlocks();
                        for (int j = 0; j < fatBlocks.Count; j++)
                        {

                            var block = fatBlocks[j];
                            if (block.IsFunctional && PartPlatforms.ContainsKey(block.BlockDefinition.Id))
                            {

                                Ai gridAi;
                                CoreComponent comp;
                                if (!EntityAIs.TryGetValue(block.CubeGrid, out gridAi) || !gridAi.CompBase.TryGetValue(block, out comp))
                                    _uninitializedBlocks.Add(block);
                                else {

                                    var wComp = comp as Weapon.WeaponComponent;
                                    if (wComp != null && wComp.Data.Repo.Values.Set.Overrides.Debug)
                                        _debugBlocks.Add(wComp);
                                }
                            }
                        }
                    }

                }
            }
            for (int i = _uninitializedBlocks.Count - 1; i >= 0; i--)
            {

                var badBlock = _uninitializedBlocks[i];

                Ai gridAi;
                CoreComponent comp;
                if (EntityAIs.TryGetValue(badBlock.CubeGrid, out gridAi) && gridAi.CompBase.TryGetValue(badBlock, out comp))
                {
                    _uninitializedBlocks.RemoveAtFast(i);
                    continue;
                }

                if (badBlock.InScene)
                {
                    var lookSphere = new BoundingSphereD(badBlock.PositionComp.WorldAABB.Center, 30f);
                    if (Camera.IsInFrustum(ref lookSphere))
                    {
                        MyOrientedBoundingBoxD blockBox;
                        SUtils.GetBlockOrientedBoundingBox(badBlock, out blockBox);
                        DsDebugDraw.DrawBox(blockBox, _uninitializedColor);
                    }
                }
            }

            for (int i = 0; i < _debugBlocks.Count; i++)
            {

                var comp = _debugBlocks[i];
                if (comp.Cube.InScene)
                {

                    var lookSphere = new BoundingSphereD(comp.Cube.PositionComp.WorldAABB.Center, 100f);

                    if (Camera.IsInFrustum(ref lookSphere))
                    {
                        var collection = comp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom ? comp.Platform.Weapons : comp.Platform.Phantoms;
                        foreach (var w in collection)
                        {

                            if (!w.TurretController && w.ActiveAmmoDef.AmmoDef.Trajectory.Guidance == WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType.Smart)
                                w.SmartLosDebug();
                        }
                    }
                }
            }
        }

        private void UpdatePlacer()
        {
            if (!Placer.Visible) Placer = null;
            if (!MyCubeBuilder.Static.DynamicMode && MyCubeBuilder.Static.HitInfo.HasValue)
            {
                var hit = MyCubeBuilder.Static.HitInfo.Value as IHitInfo;
                var grid = hit.HitEntity as MyCubeGrid;

                if (grid != null && MyCubeBuilder.Static.CurrentBlockDefinition != null)
                {

                    var blockId = MyCubeBuilder.Static.CurrentBlockDefinition.Id;
                    var subtypeIdHash = blockId.SubtypeId;

                    Ai ai;
                    if (EntityToMasterAi.TryGetValue(grid, out ai))
                    {
                        PartCounter partCounter;
                        if (ai.PartCounting.TryGetValue(subtypeIdHash, out partCounter))
                        {
                            if (partCounter.Max > 0 && ai.Construct.GetPartCount(subtypeIdHash) >= partCounter.Max)
                            {
                                MyCubeBuilder.Static.NotifyPlacementUnable();
                                MyCubeBuilder.Static.Deactivate();
                                return;
                            }
                        }
                        if (AreaRestrictions.ContainsKey(subtypeIdHash) && !SearchLightHashes.Contains(blockId))
                        {
                            MyOrientedBoundingBoxD restrictedBox;
                            MyOrientedBoundingBoxD buildBox = MyCubeBuilder.Static.GetBuildBoundingBox();
                            BoundingSphereD restrictedSphere;
                            if (IsPartAreaRestricted(subtypeIdHash, buildBox, grid, 0, null, out restrictedBox, out restrictedSphere))
                            {
                                DsDebugDraw.DrawBox(buildBox, _uninitializedColor);
                            }

                            if (MyAPIGateway.Session.Config.HudState == 1)
                            {
                                if (restrictedBox.HalfExtent.AbsMax() > 0)
                                {
                                    DsDebugDraw.DrawBox(restrictedBox, _restrictionAreaColor);
                                }
                                if (restrictedSphere.Radius > 0)
                                {
                                    DsDebugDraw.DrawSphere(restrictedSphere, _restrictionAreaColor);
                                }

                                for (int i = 0; i < ai.WeaponComps.Count; i++)
                                {
                                    var comp = ai.WeaponComps[i];

                                    if (comp.IsBlock)
                                    {
                                        MyOrientedBoundingBoxD blockBox;
                                        SUtils.GetBlockOrientedBoundingBox(comp.Cube, out blockBox);

                                        BoundingSphereD s;
                                        MyOrientedBoundingBoxD b;
                                        CalculateRestrictedShapes(comp.SubTypeId, blockBox, out b, out s);

                                        if (s.Radius > 0)
                                        {
                                            DsDebugDraw.DrawSphere(s, _restrictionAreaColor);
                                        }
                                        if (b.HalfExtent.AbsMax() > 0)
                                        {
                                            DsDebugDraw.DrawBox(b, _restrictionAreaColor);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }
    }
}

using CoreSystems;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Noise.Patterns;
using VRage.Utils;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting
{
    internal partial class TargetUi
    {
        internal void ActivateSelector()
        {
            var s = Session.I;
            if (s.TrackingAi.AiType == Ai.AiTypes.Phantom || s.UiInput.FirstPersonView && !s.UiInput.TurretBlockView && !s.UiInput.IronSights && (!s.UiInput.AltPressed || s.UiInput.PlayerWeapon)) return;
            if (s.UiInput.CtrlReleased && !s.UiInput.FirstPersonView && !s.UiInput.CameraBlockView && !s.UiInput.TurretBlockView)
            {
                switch (_3RdPersonDraw)
                {
                    case ThirdPersonModes.None:
                        _3RdPersonDraw = !s.UiInput.PlayerWeapon ? ThirdPersonModes.DotTarget : ThirdPersonModes.None;
                        break;
                    case ThirdPersonModes.DotTarget:
                        _3RdPersonDraw = !s.UiInput.PlayerWeapon ? ThirdPersonModes.Crosshair : ThirdPersonModes.None;
                        break;
                    case ThirdPersonModes.Crosshair:
                        _3RdPersonDraw = ThirdPersonModes.None;
                        break;
                }
            }
            
            if (s.UiInput.TurretBlockView || s.UiInput.CameraBlockView)
                _3RdPersonDraw = ThirdPersonModes.DotTarget;

            var enableActivator = _3RdPersonDraw == ThirdPersonModes.Crosshair || s.UiInput.FirstPersonView && s.UiInput.AltPressed && !s.UiInput.IronSights || s.UiInput.CameraBlockView;

            if (enableActivator || !s.UiInput.FirstPersonView && !s.UiInput.CameraBlockView && !s.UiInput.PlayerWeapon || s.UiInput.TurretBlockView || s.UiInput.IronSights)
                DrawSelector(enableActivator);
        }

        internal void TargetSelection()
        {
            var s = Session.I;
            if (!s.InGridAiBlock) return;
            var ai = s.TrackingAi;

            if (ai.AiType == Ai.AiTypes.Player)
            {
                Ai.FakeTargets fakeTargets;
                if ((s.UiInput.MouseButtonMenuNewPressed || s.UiInput.MouseButtonMenuReleased) && s.PlayerDummyTargets.TryGetValue(s.PlayerId, out fakeTargets))
                {
                    var painter = fakeTargets.PaintedTarget;
                    if (DrawReticle && s.UiInput.FirstPersonView && s.UiInput.IronSights)
                    {
                        var newTarget = SelectTarget(true, s.UiInput.MouseButtonMenuNewPressed);

                        if (s.UiInput.MouseButtonMenuReleased && painter.EntityId == 0 && !newTarget)
                            ai.Construct.Focus.RequestReleaseActive(ai, s.PlayerId);
                    }
                    else if (s.UiInput.MouseButtonMenuReleased && !DrawReticle && !s.UiInput.IronSights && !SelectTarget(true, true, true))
                    {
                        if (painter.EntityId == 0)
                            ai.Construct.Focus.RequestReleaseActive(ai, s.PlayerId);
                        else
                            painter.Update(Vector3D.Zero, s.Tick);
                    }
                }

                return;
            }

            MyEntity focusEnt;
            if (s.UiInput.AltPressed && s.UiInput.ShiftReleased  || s.Tick120 && ai.Construct.Focus.GetPriorityTarget(ai, out focusEnt) && VoxelInLos(ai, focusEnt) || DrawReticle && s.UiInput.ClientInputState.MouseButtonRight && s.PlayerDummyTargets[s.PlayerId].PaintedTarget.EntityId == 0 && !SelectTarget(true, true, true))
                ai.Construct.Focus.RequestReleaseActive(ai, s.PlayerId);

            if (s.UiInput.MouseButtonRightNewPressed || s.UiInput.MouseButtonRightReleased && (DrawReticle || s.UiInput.FirstPersonView))
                SelectTarget(true, s.UiInput.MouseButtonRightNewPressed);
            else if (!s.Settings.Enforcement.DisableTargetCycle)
            {
                if (s.UiInput.CurrentWheel != s.UiInput.PreviousWheel && !s.UiInput.CameraBlockView || s.UiInput.CycleNextKeyPressed || s.UiInput.CyclePrevKeyPressed)
                    SelectNext();
            }

        }


        private MyEntity _firstStageEnt;
        internal bool SelectTarget(bool manualSelect = true, bool firstStage = false, bool checkOnly = false)
        {
            var s = Session.I;
            var ai = s.TrackingAi;
            if (s.Tick - MasterUpdateTick > 120 || MasterUpdateTick < 120 && _masterTargets.Count == 0)
                BuildMasterCollections(ai);
            if (!_cachedPointerPos) InitPointerOffset(0.05);
            var cockPit = s.ActiveCockPit;
            Vector3D end;
            if (s.UiInput.CameraBlockView)
            {
                var offetPosition = Vector3D.Transform(PointerOffset, s.CameraMatrix);
                AimPosition = offetPosition;
                AimDirection = Vector3D.Normalize(AimPosition - s.CameraPos);
                end = offetPosition + (AimDirection * ai.MaxTargetingRange);
            }
            else if (!s.UiInput.FirstPersonView)
            {
                var offetPosition = Vector3D.Transform(PointerOffset, s.CameraMatrix);
                AimPosition = offetPosition;
                AimDirection = Vector3D.Normalize(AimPosition - s.CameraPos);
                end = offetPosition + (AimDirection * ai.MaxTargetingRange);

            }
            else
            {
                if (!s.UiInput.AltPressed && !s.UiInput.TurretBlockView && ai.IsGrid && cockPit != null)
                {
                    AimDirection = cockPit.PositionComp.WorldMatrixRef.Forward;
                    AimPosition = cockPit.PositionComp.WorldAABB.Center;
                    end = AimPosition + (AimDirection * s.TrackingAi.MaxTargetingRange);
                }
                else
                {
                    var offetPosition = Vector3D.Transform(PointerOffset, s.CameraMatrix);
                    AimPosition = offetPosition;
                    AimDirection = Vector3D.Normalize(AimPosition - s.CameraPos);

                    end = offetPosition + (AimDirection * ai.MaxTargetingRange);
                }
            }
            var foundTarget = false;
            var possibleTarget = false;
            var rayOnlyHitSelf = false;
            var rayHitSelf = false;
            var manualTarget = Session.I.PlayerDummyTargets[Session.I.PlayerId].ManualTarget;
            var paintTarget = Session.I.PlayerDummyTargets[Session.I.PlayerId].PaintedTarget;
            var mark = s.UiInput.MouseButtonRightReleased && !ai.SmartHandheld || ai.SmartHandheld && s.UiInput.MouseButtonMenuReleased;
            var friendCheckVolume = ai.TopEntityVolume;
            friendCheckVolume.Radius *= 2;

            var advanced = s.Settings.ClientConfig.AdvancedMode || s.UiInput.IronLock;
            MyEntity closestEnt = null;
            MyEntity rootEntity = null;
            if (ai.MyPlanet != null && Session.I.Tick90 && s.UiInput.AltPressed)
            {
                var rayLine = new LineD(AimPosition, ai.MaxTargetingRange > s.PreFetchMaxDist ? AimPosition + AimDirection * s.PreFetchMaxDist : end);
                ai.MyPlanet.PrefetchShapeOnRay(ref rayLine);
            }
            Session.I.Physics.CastRay(AimPosition, end, _hitInfo);

            for (int i = 0; i < _hitInfo.Count; i++)
            {

                var hit = _hitInfo[i];
                var hitVoxel = hit.HitEntity is IMyVoxelBase;
                if(hitVoxel)
                {
                    end = hit.Position;
                    break;
                }
                closestEnt = hit.HitEntity.GetTopMostParent() as MyEntity;
                if (closestEnt == null)
                    continue;


                if (ai.TopEntityMap.GroupMap.Construct.ContainsKey(closestEnt))
                {
                    rayHitSelf = true;
                    rayOnlyHitSelf = true;
                    continue;
                }

                if (rayOnlyHitSelf) rayOnlyHitSelf = false;

                Ai masterAi;
                if (s.EntityToMasterAi.TryGetValue(closestEnt, out masterAi) && masterAi.Construct.RootAi.Construct.LargestAi?.TopEntity != null)
                    rootEntity = masterAi.Construct.RootAi.Construct.LargestAi.TopEntity;
                else 
                    rootEntity = closestEnt;

                var hitGrid = closestEnt as MyCubeGrid;
                var character = closestEnt as IMyCharacter;

                if (hitGrid != null && ((uint)hitGrid.Flags & 0x20000000) > 0) continue;

                if (manualSelect)
                {
                    if (character == null && hitGrid == null || !_masterTargets.ContainsKey(rootEntity))
                    {
                        continue;
                    }
                    if (firstStage)
                    {
                        _firstStageEnt = closestEnt;
                        possibleTarget = true;
                    }
                    else
                    {
                        if (closestEnt == _firstStageEnt) {

                            if (mark && advanced && !checkOnly && ai.Construct.Focus.EntityIsFocused(ai, rootEntity)) 
                                paintTarget.Update(hit.Position, s.Tick, closestEnt);

                            if (!checkOnly)
                            {
                                s.SetTarget(rootEntity, ai);
                            }
                            possibleTarget = true;
                        }

                        _firstStageEnt = null;
                    }

                    return possibleTarget;
                }

                if (ai.TopEntityMap.GroupMap.Construct.ContainsKey(closestEnt) || closestEnt.PositionComp.WorldVolume.Intersects(friendCheckVolume))
                    continue;

                foundTarget = true;
                if (!checkOnly)
                    manualTarget.Update(hit.Position, s.Tick, closestEnt);
                break;
            }

            if (rayHitSelf)
            {
                ReticleOnSelfTick = s.Tick;
                ReticleAgeOnSelf++;
                if (rayOnlyHitSelf && !mark && !checkOnly) 
                    manualTarget.Update(end, s.Tick);
            }
            else ReticleAgeOnSelf = 0;

            Vector3D hitPos;
            bool foundOther = false;
            if (!foundTarget && RayCheckTargets(AimPosition, AimDirection, out closestEnt, out rootEntity, out hitPos, out foundOther, !manualSelect))
            {
                foundTarget = true;
                if (manualSelect)
                {
                    if (firstStage)
                        _firstStageEnt = closestEnt;
                    else
                    {
                        if (!checkOnly && closestEnt == _firstStageEnt)
                            s.SetTarget(rootEntity, ai);

                        _firstStageEnt = null;
                    }

                    return true;
                }
                if (!checkOnly)
                    manualTarget.Update(hitPos, s.Tick, closestEnt);
            }

            if (!manualSelect)
            {
                MyTuple<float, TargetControl, MyRelationsBetweenPlayerAndBlock> tInfo = new MyTuple<float, TargetControl, MyRelationsBetweenPlayerAndBlock>();
                var activeColor = rootEntity != null && !_masterTargets.TryGetValue(rootEntity, out tInfo) || foundOther ? Color.DeepSkyBlue : Color.Red;

                var voxel = closestEnt as MyVoxelBase;
                var dumbHand = s.UiInput.PlayerWeapon && !ai.SmartHandheld;
                var playerIgnore = dumbHand && (tInfo.Item2 != TargetControl.None && tInfo.Item3 != MyRelationsBetweenPlayerAndBlock.Enemies);
                
                _reticleColor = closestEnt != null && (voxel == null && !playerIgnore) ? activeColor : Color.White;
                if (dumbHand && _reticleColor == Color.DeepSkyBlue)
                    _reticleColor = Color.White;

                if (voxel == null)
                {
                    LastSelectableTick = Session.I.Tick;
                    LastSelectedEntity = closestEnt;
                }

                if (!foundTarget && !checkOnly)
                {
                    if (mark)
                    {
                        paintTarget.Update(end, s.Tick);
                    }
                    else
                    {
                        manualTarget.Update(end, s.Tick);
                    }
                }
            }

            return foundTarget;
        }

        internal bool GetSelectableEntity(out Vector3D position, out MyEntity selected)
        {

            if (Session.I.Tick - LastSelectableTick < 60)
            {
                var skip = false;
                var ai = Session.I.TrackingAi;

                MyEntity focusEnt;
                if (ai != null && LastSelectedEntity != null && ai.Construct.Focus.GetPriorityTarget(ai, out focusEnt))
                {
                    var focusGrid = focusEnt as MyCubeGrid;
                    var lastEntityGrid = LastSelectedEntity as MyCubeGrid;

                    if (LastSelectedEntity.MarkedForClose || focusEnt == LastSelectedEntity || focusGrid != null && lastEntityGrid != null && focusGrid.IsSameConstructAs(lastEntityGrid))
                        skip = true;
                }

                if (LastSelectedEntity != null && !skip && Session.I.CameraFrustrum.Contains(LastSelectedEntity.PositionComp.WorldVolume) != ContainmentType.Disjoint)
                {
                    position = LastSelectedEntity.PositionComp.WorldAABB.Center;
                    selected = LastSelectedEntity;
                    return true;
                }
            }

            position = Vector3D.Zero;
            selected = null;
            return false;
        }

        internal bool ActivateDroneNotice()
        {
            var s = Session.I;
            var alert = s.TrackingAi.IsGrid && s.TrackingAi.Construct.DroneAlert;
            var showAlert = alert && !(s.HudHandlers.Count > 0 && s.HudUi.RestrictHudHandlers(s.TrackingAi, s.PlayerId, Hud.Hud.HudMode.Drone));
            return showAlert;
        }

        internal bool ActivateMarks()
        {
            var s = Session.I;
            var mark = s.TrackingAi.AiType != Ai.AiTypes.Phantom && s.ActiveMarks.Count > 0;
            var showAlert = mark && !(s.HudHandlers.Count > 0 && s.HudUi.RestrictHudHandlers(s.TrackingAi, s.PlayerId, Hud.Hud.HudMode.PainterMarks));
            return showAlert;
        }

        internal bool ActivateLeads()
        {
            var s = Session.I;
            var leads = s.LeadGroupActive;
            var showAlert = leads && !(s.HudHandlers.Count > 0 && s.HudUi.RestrictHudHandlers(s.TrackingAi, s.PlayerId, Hud.Hud.HudMode.Lead));
            return showAlert;
        }

        internal void ResetCache()
        {
            _cachedPointerPos = false;
        }

        private void InitPointerOffset(double adjust)
        {
            var position = new Vector3D(_pointerPosition.X, _pointerPosition.Y, 0);
            var scale = 0.075 * Session.I.ScaleFov;

            position.X *= scale * Session.I.AspectRatio;
            position.Y *= scale;

            PointerAdjScale = adjust * scale;

            PointerOffset = new Vector3D(position.X, position.Y, -0.1);
            _cachedPointerPos = true;
        }

        internal void SelectNext()
        {
            var s = Session.I;
            var ai = s.TrackingAi;

            if (!_cachedPointerPos) InitPointerOffset(0.05);
            var updateTick = s.Tick - _cacheIdleTicks > 300 || _endIdx == -1 || _sortedMasterList.Count - 1 < _endIdx;

            if (updateTick && !UpdateCache(s.Tick) || s.UiInput.ShiftPressed || s.UiInput.ControlKeyPressed || s.UiInput.AltPressed || s.UiInput.CtrlPressed) return;

            var canMoveForward = _currentIdx + 1 <= _endIdx;
            var canMoveBackward = _currentIdx - 1 >= 0;
            if (s.UiInput.WheelForward || s.UiInput.CycleNextKeyPressed)
                if (canMoveForward)
                    _currentIdx += 1;
                else _currentIdx = 0;
            else if (s.UiInput.WheelBackward || s.UiInput.CyclePrevKeyPressed)
                if (canMoveBackward)
                    _currentIdx -= 1;
                else _currentIdx = _endIdx;

            MyEntity ent;
            if (!GetValidNextEntity(ai, out ent))
            {
                _endIdx = -1;
                return;
            }

            s.SetTarget(ent, ai);
        }

        private bool GetValidNextEntity(Ai ai, out MyEntity entity)
        {
            var loop = _sortedMasterList.Count;
            var count = 0;
            entity = null;
            while (count++ < loop)
            {
                entity = _sortedMasterList[_currentIdx];
                if (entity == null || entity.MarkedForClose )
                {
                    return false;
                }

                if (VoxelInLos(ai, entity))
                {
                    if (++_currentIdx >= loop)
                        _currentIdx = 0;

                    continue;
                }

                return true;

            }

            return false;
        }

        private readonly Vector3D[] _fromObbCorners = new Vector3D[8];
        private readonly Vector3D[] _toObbCorners = new Vector3D[8];

        internal bool VoxelInLos(Ai ai, MyEntity target)
        {
            var mySphere = ai.TopEntity.PositionComp.WorldVolume;
            var targetSphere = target.PositionComp.WorldVolume;
            var targetDir = Vector3D.Normalize(targetSphere.Center - mySphere.Center);
            var testPos = mySphere.Center + (targetDir * mySphere.Radius);
            var distSqr = Vector3D.DistanceSquared(testPos, targetSphere.Center) - (targetSphere.Radius * targetSphere.Radius);

            if (distSqr > 250000)
            {
                if ((ai.Construct.TrackedTargets.Count > 0 || Session.I.ActiveMarks.Count > 0) && TargetExcludedFromVoxelLos(ai, target))
                    return false;

                var topEnt = ai.Construct.LargestAi?.TopEntity ?? ai.TopEntity;
                var fromEntObb = new MyOrientedBoundingBoxD(topEnt.PositionComp.LocalAABB, topEnt.PositionComp.WorldMatrixRef);
                fromEntObb.GetCorners(_fromObbCorners, 0);
                var toEntObb = new MyOrientedBoundingBoxD(target.PositionComp.LocalAABB, target.PositionComp.WorldMatrixRef);
                toEntObb.GetCorners(_toObbCorners, 0);
                
                for (int i = 0; i < 9; i++)
                {
                    var from = i == 0 ? topEnt.PositionComp.WorldAABB.Center : _fromObbCorners[i - 1];
                    var to = i == 0 ? target.PositionComp.WorldAABB.Center : _toObbCorners[i - 1];
                    IHitInfo hitInfo;
                    if (Session.I.Physics.CastRay(from, to, out hitInfo, CollisionLayers.CollideWithStaticLayer))
                        continue;

                    return false;
                }
                return true;
            }

            return false;
        }

        private bool TargetExcludedFromVoxelLos(Ai ai, MyEntity target)
        {
            var targetGrid = target as MyCubeGrid;
            if (targetGrid != null)
            {
                foreach (var m in Session.I.ActiveMarks)
                {
                    var grid = m.Item3.TmpEntity as MyCubeGrid;
                    if ((grid != null || (m.Item3.EntityId > 0 || m.Item3.EntityId <= -3)&& MyEntities.TryGetEntityById(m.Item3.EntityId, out grid) && grid != null) && targetGrid.IsSameConstructAs(grid))
                        return true;
                }
            }

            foreach (var pair in ai.Construct.TrackedTargets)
            {
                foreach (var map in pair.Value)
                {
                    var ent = map.Key as MyEntity;
                    if (ent != null)
                    {
                        var topEnt = ent.GetTopMostParent();
                        if (topEnt == target)
                            return true;
                    }
                }
            }

            return false;
        }

        private bool UpdateCache(uint tick)
        {
            _cacheIdleTicks = tick;
            var ai = Session.I.TrackingAi;
            var focus = ai.Construct.Data.Repo.FocusData;
            _currentIdx = 0;
            BuildMasterCollections(ai);

            for (int i = 0; i < _sortedMasterList.Count; i++)
                if (focus.Target == _sortedMasterList[i].EntityId) _currentIdx = i;
            _endIdx = _sortedMasterList.Count - 1;

            return _endIdx >= 0;
        }

        internal void BuildMasterCollections(Ai ai)
        {
            _masterTargets.Clear();
            var ais = ai.TopEntityMap.GroupMap.Ais;
            for (int i = 0; i < ais.Count; i++)
            {
                var subTargets = ais[i].SortedTargets;
                for (int j = 0; j < subTargets.Count; j++)
                {
                    var tInfo = subTargets[j];
                    var character = tInfo.Target as IMyCharacter;
                    if (tInfo.Target.MarkedForClose || character?.ControllerInfo != null && (!Session.I.UiInput.PlayerWeapon && Session.I.Players.ContainsKey(character.ControllerInfo.ControllingIdentityId))) continue;

                    Ai topAi;
                    MyEntity target;
                    if (tInfo.IsGrid && tInfo.TargetAi != null && Session.I.EntityToMasterAi.TryGetValue(tInfo.Target, out topAi))
                    {
                        if (topAi != tInfo.TargetAi)
                            continue;
                        target = topAi.Construct.LargestAi?.TopEntity ?? topAi.Construct.RootAi?.TopEntity ?? tInfo.Target;
                    }
                    else
                        target = tInfo.Target;

                    TopMap topMap;
                    var controlType = tInfo.Drone ? TargetControl.Drone : tInfo.IsGrid && Session.I.TopEntityToInfoMap.TryGetValue((MyCubeGrid)target, out topMap) && topMap.PlayerControllers.Count > 0 ? TargetControl.Player : tInfo.IsGrid && !Session.I.GridHasPower((MyCubeGrid)target) ? TargetControl.Trash : TargetControl.Other;
                    
                    _masterTargets[target] = new MyTuple<float, TargetControl, MyRelationsBetweenPlayerAndBlock>(tInfo.OffenseRating, controlType, tInfo.EntInfo.Relationship);
                    _toPruneMasterDict[target] = tInfo;
                }
            }

            _sortedMasterList.Clear();
            _toSortMasterList.AddRange(_toPruneMasterDict.Values);
            _toPruneMasterDict.Clear();

            _toSortMasterList.Sort(Session.I.TargetCompare);

            for (int i = 0; i < _toSortMasterList.Count; i++)
                _sortedMasterList.Add(_toSortMasterList[i].Target);

            _toSortMasterList.Clear();
            MasterUpdateTick = Session.I.Tick;
        }

        private MyEntity _lastEntityBehindVoxel;
        private bool RayCheckTargets(Vector3D origin, Vector3D dir, out MyEntity closestEnt, out MyEntity rootEntity, out Vector3D hitPos, out bool foundOther, bool checkOthers = false)
        {
            var ai = Session.I.TrackingAi;
            var closestDist1 = double.MaxValue;
            var closestDist2 = double.MaxValue;
            closestEnt = null;
            MyEntity backUpEnt = null;
            foreach (var info in _masterTargets.Keys)
            {
                var hit = info as MyCubeGrid;
                if (hit == null) continue;
                var ray = new RayD(origin, dir);

                var entVolume = info.PositionComp.WorldVolume;
                var entCenter = entVolume.Center;
                var dist1 = ray.Intersects(entVolume);
                if (dist1 < closestDist1)
                {
                    closestDist1 = dist1.Value;
                    closestEnt = hit;
                }

                double dist;
                Vector3D.DistanceSquared(ref entCenter, ref origin, out dist);

                if (dist > 360000)
                {
                    var inflated = info.PositionComp.WorldVolume;
                    var clamped = MathHelperD.Clamp(inflated.Radius * 3, 100f, double.MaxValue);
                    inflated.Radius = clamped;
                    var dist2 = ray.Intersects(inflated);
                    if (dist2 < closestDist2)
                    {
                        closestDist2 = dist2.Value;
                        backUpEnt = hit;
                    }
                }

            }

            foundOther = false;
            if (checkOthers)
            {
                var friendCheckVolume = ai.TopEntityVolume;
                friendCheckVolume.Radius *= 2;
                for (int i = 0; i < ai.Obstructions.Count; i++)
                {
                    var info = ai.Obstructions[i];
                    var otherEnt = info.Target;
                    if (otherEnt is MyCubeGrid)
                    {
                        var ray = new RayD(origin, dir);
                        var entVolume = otherEnt.PositionComp.WorldVolume;
                        var entCenter = entVolume.Center;

                        var dist1 = ray.Intersects(entVolume);
                        if (dist1 < closestDist1)
                        {

                            if (ai.TopEntityMap?.GroupMap == null || ai.TopEntityMap.GroupMap.Construct.ContainsKey(otherEnt) || otherEnt.PositionComp.WorldVolume.Intersects(friendCheckVolume))
                                continue;

                            closestDist1 = dist1.Value;
                            closestEnt = otherEnt;
                            foundOther = true;
                        }

                        double dist;
                        Vector3D.DistanceSquared(ref entCenter, ref origin, out dist);

                        if (dist > 360000)
                        {
                            var inflated = entVolume;
                            var clamped = MathHelperD.Clamp(inflated.Radius * 3, 100f, double.MaxValue);
                            inflated.Radius = clamped;
                            var dist2 = ray.Intersects(inflated);
                            if (dist2 < closestDist2)
                            {
                                closestDist1 = dist2.Value;
                                backUpEnt = otherEnt;
                                foundOther = true;
                            }
                        }
                    }
                }
            }

            if (closestEnt == null)
                closestEnt = backUpEnt;

            if (closestDist1 < double.MaxValue)
                hitPos = origin + (dir * closestDist1);
            else if (closestDist2 < double.MaxValue)
                hitPos = origin + (dir * closestDist2);
            else hitPos = Vector3D.Zero;

            Ai masterAi;
            if (closestEnt != null && Session.I.EntityToMasterAi.TryGetValue(closestEnt, out masterAi) && masterAi.Construct.RootAi?.Construct.LargestAi?.TopEntity != null)
                rootEntity = masterAi.Construct.RootAi.Construct.LargestAi.TopEntity;
            else 
                rootEntity = closestEnt;

            if (Session.I.Tick60)
                _lastEntityBehindVoxel = null;

            if (rootEntity != null)
            {
                if (_lastEntityBehindVoxel == rootEntity || VoxelInLos(ai, rootEntity))
                {
                    _lastEntityBehindVoxel = rootEntity;
                    closestEnt = null;
                    rootEntity = null;
                }
            }

            return closestEnt != null;
        }
    }
}

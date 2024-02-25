using System;
using CoreSystems.Platform;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace CoreSystems.Support
{
    public partial class Ai
    {
        internal void RequestDbUpdate()
        {
            if (TopEntity == null)
                return;
            
            using (TopEntity.Pin()) {
                if (TopEntity.MarkedForClose || !TopEntity.InScene)
                    return;

                if (AiType == AiTypes.Grid) {
                    var oldOwner = AiOwner;
                    var bigOwners = GridEntity.BigOwners;
                    AiOwner = bigOwners.Count > 0 ? bigOwners[0] : 0;
                    if (oldOwner != AiOwner)
                        UpdateFactionColors();
                }
            }

            ScanInProgress = true;

            TopEntityVolume = TopEntity.PositionComp.WorldVolume;
            ScanVolume = TopEntityVolume;
            ScanVolume.Radius = MaxTargetingRange;
            Session.I.DbsToUpdate.Add(new DbScan {Ai = this, Version = Version});
            TargetsUpdatedTick = Session.I.Tick;
        }

        internal void Scan()
        {
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref ScanVolume, _possibleTargets);
            NearByEntitiesTmp = _possibleTargets.Count;

            for (int i = 0; i < NearByEntitiesTmp; i++)
            {
                var ent = _possibleTargets[i];
                if (ent == null)
                {
                    Log.Line($"scan had null entity");
                    continue;
                }
                using (ent.Pin())
                {

                    if (ent is MyVoxelBase || ent is MyFloatingObject || ent.Physics == null || ent.Physics.IsPhantom || ent.MarkedForClose || !ent.InScene || ent.IsPreview  || ((uint)ent.Flags & 0x20000000) > 0) continue;
                    var grid = ent as MyCubeGrid;

                    TopMap topMap = null;
                    if (grid != null)
                    {
                        if (AiType == AiTypes.Grid && GridEntity.IsSameConstructAs(grid))
                            continue;

                        if (!Session.I.TopEntityToInfoMap.TryGetValue(grid, out topMap) || topMap.Trash && topMap.GroupMap?.Construct.Count <= 1)
                            continue;
                    }

                    Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entInfo;
                    if (!CreateEntInfo(ent, AiOwner, out entInfo))
                        continue;

                    switch (entInfo.Relationship)
                    {
                        case MyRelationsBetweenPlayerAndBlock.Owner:
                        case MyRelationsBetweenPlayerAndBlock.FactionShare:
                        case MyRelationsBetweenPlayerAndBlock.Friends:
                            continue;
                    }

                    if (topMap != null)
                    {

                        var allFat = topMap.MyCubeBocks;
                        if (allFat == null)
                            continue;

                        var fatCount = allFat.Count;
                        if (fatCount <= 0)
                            continue;

                        if (Session.I.Tick - topMap.PowerCheckTick > 600)
                            Session.I.CheckGridPowerState(grid, topMap);

                        var loneWarhead = topMap.Warheads && fatCount == 1;

                        int partCount;
                        Ai targetAi;
                        if (Session.I.EntityAIs.TryGetValue(grid, out targetAi))
                        {
                            targetAi.TargetAisTmp.Add(this);
                            TargetAisTmp.Add(targetAi);
                            partCount = targetAi.Construct.BlockCount;
                        }
                        else
                            partCount = topMap.MostBlocks;

                        NewEntities.Add(new DetectInfo(ent, entInfo, partCount, !loneWarhead ? fatCount : 2, topMap.SuspectedDrone, loneWarhead));// bump warhead to 2 fatblocks so its not ignored by targeting
                        ValidGrids.Add(ent);
                    }
                    else NewEntities.Add(new DetectInfo( ent, entInfo, 1, 0, false, false));
                }
            }
            FinalizeTargetDb();
        }

        private void FinalizeTargetDb()
        {
            MyPlanetTmp = MyGamePruningStructure.GetClosestPlanet(ScanVolume.Center);
            ObstructionsTmp.Clear();
            StaticsInRangeTmp.Clear();
            for (int i = 0; i < NearByEntitiesTmp; i++) {

                var ent = _possibleTargets[i];
                using (ent.Pin()) {

                    if (ent is MyFloatingObject || ent.MarkedForClose || !ent.InScene)
                        continue;

                    if (Session.I.ShieldApiLoaded && ent.DefinitionId?.SubtypeId == Session.I.ShieldHash && ent.Render.Visible)
                    {
                        var shieldblock = Session.I.SApi.MatchEntToShieldFast(ent, false);
                        if (shieldblock != null)
                            NearByShieldsTmp.Add(new Shields { Id = ent.Hierarchy.ChildId, ShieldEnt = ent, ShieldBlock = (MyCubeBlock)shieldblock });
                    }


                    var voxel = ent as MyVoxelBase;
                    var grid = ent as MyCubeGrid;
                    var safeZone = ent as MySafeZone;
                    var character = ent as IMyCharacter;
                    var blockingThings = safeZone != null || ent.Physics != null && !ent.Physics.IsPhantom && (grid != null || character != null || ent.DefinitionId?.SubtypeId == Session.I.CustomEntityHash) || voxel != null && voxel == voxel.RootVoxel;
                    if (!blockingThings || voxel != null && (voxel.RootVoxel is MyPlanet || voxel.PositionComp.LocalVolume.Radius < 15) || ent.IsPreview || ((uint)ent.Flags & 0x20000000) > 0) continue;

                    if (voxel != null || safeZone != null || ent.Physics.IsStatic)
                        StaticsInRangeTmp.Add(ent);

                    TopMap map;
                    if (grid != null && AiType != AiTypes.Phantom && (TopEntityMap.GroupMap.Construct.ContainsKey(grid) || ValidGrids.Contains(ent) || grid.PositionComp.LocalVolume.Radius <= 7.5 || Session.I.TopEntityToInfoMap.TryGetValue(grid, out map) && map.Trash && map.GroupMap?.Construct.Count <= 1)) continue;

                    Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entInfo;
                    if (CreateEntInfo(ent, AiOwner, out entInfo))
                    {
                        ObstructionsTmp.Add(new DetectInfo(ent, entInfo, 2, 2, false, false));
                    }
                }
            }

            if (MyPlanetTmp != null)
            {
                Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entInfo;
                if (CreateEntInfo(MyPlanetTmp, AiOwner, out entInfo))
                    ObstructionsTmp.Add(new DetectInfo(MyPlanetTmp, entInfo, 2, 2, false, false));
            }

            ValidGrids.Clear();
            _possibleTargets.Clear();
        }


        internal void NearByShield()
        {
            NearByFriendlyShields.Clear();
            NearByFriendlyShieldsCache.Clear();
            ShieldFortified = false;
            for (int i = 0; i < NearByShieldsTmp.Count; i++) {

                var shield = NearByShieldsTmp[i];
                var shieldGrid = MyEntities.GetEntityByIdOrDefault(shield.Id) as MyCubeGrid;
                
                if (shieldGrid != null) {

                    if (shield.Id == TopEntity.EntityId || AiType == AiTypes.Grid && GridEntity.IsSameConstructAs(shieldGrid))  {
                        ShieldBlock = shield.ShieldBlock as IMyTerminalBlock;
                        MyShield = shield.ShieldEnt;
                        ShieldFortified = Session.I.SApi.IsFortified(ShieldBlock);
                    }
                    else {
                        TargetInfo info;
                        var found = Targets.TryGetValue(shield.ShieldBlock.CubeGrid, out info);
                        var relation = found ? info.EntInfo.Relationship : shield.ShieldBlock.IDModule.GetUserRelationToOwner(AiOwner);
                        var friendly = relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Friends;
                        
                        if (friendly) {
                            NearByFriendlyShields.Add(shield.ShieldEnt);
                            NearByFriendlyShieldsCache.Add(shield.ShieldEnt);
                            FriendlyShieldNear = true;
                        }
                        ShieldNear = true;
                    }
                }

            }
            NearByShieldsTmp.Clear();
        }

        internal void MyPlanetInfo(bool clear = false)
        {
            if (!clear) {

                MyPlanet = MyPlanetTmp;
                var gridVolume = TopEntity.PositionComp.WorldVolume;
                var gridRadius = gridVolume.Radius;
                var gridCenter = gridVolume.Center;
                var planetCenter = MyPlanet.PositionComp.WorldAABB.Center;
                ClosestPlanetSqr = double.MaxValue;

                if (new BoundingSphereD(planetCenter, MyPlanet.AtmosphereRadius + gridRadius).Intersects(gridVolume) || AiType == AiTypes.Phantom) {

                    InPlanetGravity = true;
                    PlanetClosestPoint = MyPlanet.GetClosestSurfacePointGlobal(gridCenter);
                    ClosestPlanetCenter = planetCenter;
                    double pointDistSqr;
                    Vector3D.DistanceSquared(ref PlanetClosestPoint, ref gridCenter, out pointDistSqr);

                    pointDistSqr -= (gridRadius * gridRadius);
                    if (pointDistSqr < 0) pointDistSqr = 0;
                    ClosestPlanetSqr = pointDistSqr;
                    PlanetSurfaceInRange = pointDistSqr <= MaxTargetingRangeSqr;
                    TouchingWater = Session.I.WaterApiLoaded && GridTouchingWater();
                }
                else {
                    InPlanetGravity = false;
                    PlanetClosestPoint = MyPlanet.GetClosestSurfacePointGlobal(gridCenter);
                    ClosestPlanetCenter = planetCenter;
                    double pointDistSqr;
                    Vector3D.DistanceSquared(ref PlanetClosestPoint, ref gridCenter, out pointDistSqr);
                    pointDistSqr -= (gridRadius * gridRadius);
                    if (pointDistSqr < 0) pointDistSqr = 0;
                    ClosestPlanetSqr = pointDistSqr;
                    TouchingWater = false;
                }
            }
            else {
                MyPlanet = null;
                PlanetClosestPoint = Vector3D.Zero;
                PlanetSurfaceInRange = false;
                InPlanetGravity = false;
                ClosestPlanetSqr = double.MaxValue;
                TouchingWater = false;
            }
        }

        private bool GridTouchingWater()
        {
            WaterData water;
            if (Session.I.WaterMap.TryGetValue(MyPlanet.EntityId, out water)) {
                WaterVolume = new BoundingSphereD(MyPlanet.PositionComp.WorldAABB.Center, water.Radius + water.WaveHeight);
                return new MyOrientedBoundingBoxD(TopEntity.PositionComp.LocalAABB, TopEntity.PositionComp.WorldMatrixRef).Intersects(ref WaterVolume);
            }
            return false;
        }

        internal void MyStaticInfo()
        {
            StaticEntitiesInRange = StaticsInRangeTmp.Count > 0;
            ClosestStaticSqr = double.MaxValue;
            ClosestVoxelSqr = double.MaxValue;
            StaticEntityInRange = false;
            MyEntity closestEnt = null;
            var closestCenter = Vector3D.Zero;
            double closestDistSqr = double.MaxValue;
            CanShoot = true;

            MyVoxelMap roid = null;
            var closestRoidDistSqr = double.MaxValue;
            var closestRoidCenter = Vector3D.Zero;

            for (int i = 0; i < StaticsInRangeTmp.Count; i++) {

                var ent = StaticsInRangeTmp[i];
                if (ent == null)
                {
                    Log.Line($"MyStaticInfo had null entity");
                    continue;
                }
                if (ent.MarkedForClose) continue;
                var safeZone = ent as MySafeZone;
                

                var staticCenter = ent.PositionComp.WorldAABB.Center;
                if (ent is MyCubeGrid || ent.DefinitionId?.SubtypeId == Session.I.CustomEntityHash) 
                    StaticEntityInRange = true;

                double distSqr;
                Vector3D.DistanceSquared(ref staticCenter, ref ScanVolume.Center, out distSqr);
                if (distSqr < closestDistSqr) {
                    closestDistSqr = distSqr;
                    closestEnt = ent;
                    closestCenter = staticCenter;
                }

                var map = ent as MyVoxelMap;
                if (map != null && distSqr < closestRoidDistSqr)
                {
                    closestRoidDistSqr = distSqr;
                    roid = map;
                    closestRoidCenter = staticCenter;
                }

                if (CanShoot && safeZone != null && safeZone.Enabled) {

                    if (safeZone.PositionComp.WorldVolume.Contains(TopEntity.PositionComp.WorldVolume) != ContainmentType.Disjoint && ((Session.SafeZoneAction)safeZone.AllowedActions & Session.SafeZoneAction.Shooting) == 0)
                        CanShoot = !TouchingSafeZone(safeZone);
                }
            }

            if (roid != null)
            {
                var dist = Vector3D.Distance(ScanVolume.Center, closestRoidCenter);
                dist -= roid.PositionComp.LocalVolume.Radius;
                dist -= TopEntityVolume.Radius;
                if (dist < 0) dist = 0;

                var distSqr = dist * dist;
                if (ClosestPlanetSqr < distSqr) distSqr = ClosestPlanetSqr;

                ClosestVoxelSqr = distSqr;
            }

            if (closestEnt != null) {

                var dist = Vector3D.Distance(ScanVolume.Center, closestCenter);
                dist -= closestEnt.PositionComp.LocalVolume.Radius;
                dist -= TopEntityVolume.Radius;
                if (dist < 0) dist = 0;

                var distSqr = dist * dist;
                if (ClosestPlanetSqr < distSqr) distSqr = ClosestPlanetSqr;

                ClosestStaticSqr = distSqr;
            }
            else if (ClosestPlanetSqr < ClosestStaticSqr) ClosestStaticSqr = ClosestPlanetSqr;

            StaticsInRangeTmp.Clear();
        }

        private bool TouchingSafeZone(MySafeZone safeZone)
        {
            var myObb = new MyOrientedBoundingBoxD(TopEntity.PositionComp.LocalAABB, TopEntity.PositionComp.WorldMatrixRef);
            
            if (safeZone.Shape == MySafeZoneShape.Sphere) {
                var sphere = new BoundingSphereD(safeZone.PositionComp.WorldVolume.Center, safeZone.Radius);
                return myObb.Intersects(ref sphere);
            }

            return new MyOrientedBoundingBoxD(safeZone.PositionComp.LocalAABB, safeZone.PositionComp.WorldMatrixRef).Contains(ref myObb) != ContainmentType.Disjoint;
        }

        internal bool CreateEntInfo(MyEntity entity, long gridOwner, out Sandbox.ModAPI.Ingame.MyDetectedEntityInfo entInfo)
        {

            try
            {
                MyRelationsBetweenPlayerAndBlock relationship = MyRelationsBetweenPlayerAndBlock.Neutral;
                if (entity == null)
                {
                    entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo();
                    return false;
                }
                var grid = entity.GetTopMostParent() as MyCubeGrid;
                if (grid != null)
                {
                    if (!grid.DestructibleBlocks || grid.Immune || grid.GridGeneralDamageModifier <= 0)
                    {
                        entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo();
                        return false;
                    }

                    var bigOwners = grid.BigOwners;
                    var topOwner = bigOwners.Count > 0 ? bigOwners[0] : long.MaxValue;

                    relationship = topOwner != long.MaxValue ? MyIDModule.GetRelationPlayerBlock(gridOwner, topOwner, MyOwnershipShareModeEnum.Faction) : MyRelationsBetweenPlayerAndBlock.NoOwnership;
                    var type = grid.GridSizeEnum != MyCubeSize.Small ? Sandbox.ModAPI.Ingame.MyDetectedEntityType.LargeGrid : Sandbox.ModAPI.Ingame.MyDetectedEntityType.SmallGrid;
                    entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo(grid.EntityId, string.Empty, type, null, MatrixD.Zero, Vector3.Zero, relationship, new BoundingBoxD(), Session.I.Tick);
                    return true;
                }

                var myCharacter = entity as IMyCharacter;
                if (myCharacter != null)
                {
                    var type = !myCharacter.IsPlayer ? Sandbox.ModAPI.Ingame.MyDetectedEntityType.CharacterOther : Sandbox.ModAPI.Ingame.MyDetectedEntityType.CharacterHuman;

                    var getComponentOwner = entity as IMyComponentOwner<MyIDModule>;

                    long playerId;
                    MyIDModule targetIdModule;
                    if (getComponentOwner != null && getComponentOwner.GetComponent(out targetIdModule))
                        playerId = targetIdModule.Owner;
                    else {
                        var controllingId = myCharacter.ControllerInfo?.ControllingIdentityId;
                        playerId = controllingId ?? 0;
                    }
                    
                    relationship = MyIDModule.GetRelationPlayerBlock(gridOwner, playerId, MyOwnershipShareModeEnum.Faction);
                    entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo(entity.EntityId, string.Empty, type, null, MatrixD.Zero, Vector3.Zero, relationship, new BoundingBoxD(), Session.I.Tick);
                    
                    return !myCharacter.IsDead && myCharacter.Integrity > 0;
                }

                var myPlanet = entity as MyPlanet;

                if (myPlanet != null)
                {
                    const Sandbox.ModAPI.Ingame.MyDetectedEntityType type = Sandbox.ModAPI.Ingame.MyDetectedEntityType.Planet;
                    entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo(entity.EntityId, string.Empty, type, null, MatrixD.Zero, Vector3.Zero, relationship, new BoundingBoxD(), Session.I.Tick);
                    return true;
                }
                if (entity is MyVoxelMap)
                {
                    const Sandbox.ModAPI.Ingame.MyDetectedEntityType type = Sandbox.ModAPI.Ingame.MyDetectedEntityType.Asteroid;
                    entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo(entity.EntityId, string.Empty, type, null, MatrixD.Zero, Vector3.Zero, relationship, new BoundingBoxD(), Session.I.Tick);
                    return true;
                }
                if (entity is MyMeteor)
                {
                    const Sandbox.ModAPI.Ingame.MyDetectedEntityType type = Sandbox.ModAPI.Ingame.MyDetectedEntityType.Meteor;
                    entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo(entity.EntityId, string.Empty, type, null, MatrixD.Zero, Vector3.Zero, MyRelationsBetweenPlayerAndBlock.Enemies, new BoundingBoxD(), Session.I.Tick);
                    return true;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateEntInfo: {ex}", null, true); }
            
            entInfo = new Sandbox.ModAPI.Ingame.MyDetectedEntityInfo();
            return false;
        }
    }
}

using System.Collections.Generic;
using Sandbox.Game.Entities;
using VRage.ModAPI;
using VRage.Voxels;
using VRageMath;

namespace CoreSystems.Support
{
    internal static class VoxelIntersect
    {
        internal static bool PosHasVoxel(MyVoxelBase voxel, Vector3D testPos)
        {
            var planet = voxel as MyPlanet;
            var map = voxel as MyVoxelMap;
            var hit = new VoxelHit();

            if (planet != null)
            {
                var from = testPos;
                var localPosition = (Vector3)(from - planet.PositionLeftBottomCorner);
                var v = localPosition / 1f;
                Vector3I voxelCoord;
                Vector3I.Floor(ref v, out voxelCoord);
                planet.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);
            }
            else if (map != null)
            {
                var from = testPos;
                var localPosition = (Vector3)(from - map.PositionLeftBottomCorner);
                var v = localPosition / 1f;
                Vector3I voxelCoord;
                Vector3I.Floor(ref v, out voxelCoord);
                map.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);
            }

            return hit.HasHit;
        }

        internal static bool CheckPointsOnLine(MyVoxelBase voxel, LineD testLine, int distBetweenPoints)
        {
            var planet = voxel as MyPlanet;
            var map = voxel as MyVoxelMap;
            var hit = new VoxelHit();
            var checkPoints = (int)(testLine.Length / distBetweenPoints);
            for (int i = 0; i < checkPoints; i++)
            {
                var testPos = testLine.From + (testLine.Direction * (distBetweenPoints * i));
                //Log.Line($"i: {i} - lookAhead:{(distBetweenPoints * i)}");
                if (planet != null)
                {
                    var from = testPos;
                    var localPosition = (Vector3)(from - planet.PositionLeftBottomCorner);
                    var v = localPosition / 1f;
                    Vector3I voxelCoord;
                    Vector3I.Floor(ref v, out voxelCoord);
                    planet.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);
                    if (hit.HasHit) return true;
                }
                else if (map != null)
                {
                    var from = testPos;
                    var localPosition = (Vector3)(from - map.PositionLeftBottomCorner);
                    var v = localPosition / 1f;
                    Vector3I voxelCoord;
                    Vector3I.Floor(ref v, out voxelCoord);
                    map.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);
                    if (hit.HasHit) return true;
                }
            }

            return false;
        }

        internal static bool CheckPointsOnLine(MyVoxelBase voxel, LineD testLine, MyStorageData tmpStorage, int distBetweenPoints)
        {
            var voxelMatrix = voxel.PositionComp.WorldMatrixInvScaled;
            var vecMax = new Vector3I(int.MaxValue);
            var vecMin = new Vector3I(int.MinValue);

            var checkPoints = (int)(testLine.Length / distBetweenPoints);
            for (int i = 0; i < checkPoints; i++)
            {
                var point = testLine.From + (testLine.Direction * (distBetweenPoints * i));
                Vector3D result;
                Vector3D.Transform(ref point, ref voxelMatrix, out result);
                var r = result + (Vector3D)(voxel.Size / 2);
                var v1 = Vector3D.Floor(r);
                Vector3D.Fract(ref r, out r);
                var v2 = v1 + voxel.StorageMin;
                var v3 = v2 + 1;
                if (v2 != vecMax && v3 != vecMin)
                {
                    tmpStorage.Resize(v2, v3);
                    voxel.Storage.ReadRange(tmpStorage, MyStorageDataTypeFlags.Content, 0, v2, v3);
                    vecMax = v2;
                    vecMin = v3;
                }
                var num1 = tmpStorage.Content(0, 0, 0);
                var num2 = tmpStorage.Content(1, 0, 0);
                var num3 = tmpStorage.Content(0, 1, 0);
                var num4 = tmpStorage.Content(1, 1, 0);
                var num5 = tmpStorage.Content(0, 0, 1);
                var num6 = tmpStorage.Content(1, 0, 1);
                var num7 = tmpStorage.Content(0, 1, 1);
                var num8 = tmpStorage.Content(1, 1, 1);
                var num9 = num1 + (num2 - num1) * r.X;
                var num10 = num3 + (num4 - num3) * r.X;
                var num11 = num5 + (num6 - num5) * r.X;
                var num12 = num7 + (num8 - num7) * r.X;
                var num13 = num9 + (num10 - num9) * r.Y;
                var num14 = num11 + (num12 - num11) * r.Y;
                if (num13 + (num14 - num13) * r.Z >= sbyte.MaxValue)
                    return true;
            }
            return false;
        }

        internal static bool CheckSurfacePointsOnLine(MyPlanet planet, ref LineD testLine, double distBetweenPoints)
        {
            var checkPoints = (int)((testLine.Length / distBetweenPoints) + distBetweenPoints);
            var lastPoint = (checkPoints - 1);

            for (int i = 0; i < checkPoints; i++)
            {
                var planetInPath = i != lastPoint;
                var extend = (distBetweenPoints * i);
                if (extend > testLine.Length) extend = testLine.Length;
                var testPos = testLine.From + (testLine.Direction * extend);

                if (planetInPath)
                {
                    var closestSurface = planet.GetClosestSurfacePointGlobal(ref testPos);
                    double surfaceDistToTest;
                    Vector3D.DistanceSquared(ref closestSurface, ref testPos, out surfaceDistToTest);
                    if (surfaceDistToTest < 4) return true;
                }

                if (!planetInPath)
                {
                    var closestSurface = planet.GetClosestSurfacePointGlobal(ref testPos);
                    var reverseLine = testLine;
                    reverseLine.Direction = -reverseLine.Direction;
                    reverseLine.From = testPos;

                    double closestRevDist;
                    Vector3D.Distance(ref closestSurface, ref reverseLine.From, out closestRevDist);
                    reverseLine.To = reverseLine.From + (reverseLine.Direction * (closestRevDist + distBetweenPoints));
                    Vector3D? voxelHit;
                    planet.GetIntersectionWithLine(ref reverseLine, out voxelHit);
                    return voxelHit.HasValue;
                }

            }
            return false;
        }

        internal static Vector3D? ProcessVoxel(LineD trajectile, MyVoxelBase voxel, WeaponSystem system, List<Vector3I> testPoints)
        {
            var planet = voxel as MyPlanet;
            var voxelMap = voxel as MyVoxelMap;
            var ray = new RayD(trajectile.From, trajectile.Direction);
            var voxelAabb = voxel.PositionComp.WorldAABB;
            var rayVoxelDist = ray.Intersects(voxelAabb);
            if (rayVoxelDist.HasValue)
            {
                var voxelMaxLen = voxel.PositionComp.WorldVolume.Radius * 2;
                var start = trajectile.From + (ray.Direction * rayVoxelDist.Value);
                var lenRemain = trajectile.Length - rayVoxelDist.Value;
                var end = voxelMaxLen > lenRemain ? start + (ray.Direction * lenRemain) : start + (ray.Direction * voxelMaxLen);
                var testLine = new LineD(trajectile.From + (ray.Direction * rayVoxelDist.Value), end);
                var rotMatrix = Quaternion.CreateFromRotationMatrix(voxel.WorldMatrix);
                var obb = new MyOrientedBoundingBoxD(voxel.PositionComp.WorldAABB.Center, voxel.PositionComp.LocalAABB.HalfExtents, rotMatrix);
                if (obb.Intersects(ref testLine) != null)
                {
                    if (planet != null)
                    {
                        var startPos = trajectile.From - planet.PositionLeftBottomCorner;
                        var startInt = Vector3I.Round(startPos);
                        var endPos = trajectile.To - planet.PositionLeftBottomCorner;
                        var endInt = Vector3I.Round(endPos);

                        BresenhamLineDraw(startInt, endInt, testPoints);

                        for (int i = 0; i < testPoints.Count; ++i)
                        {
                            var voxelCoord = testPoints[i];
                            var voxelHit = new VoxelHit();
                            planet.Storage.ExecuteOperationFast(ref voxelHit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);
                            if (voxelHit.HasHit)
                                return (Vector3D)voxelCoord + planet.PositionLeftBottomCorner;
                        }
                    }
                    else if (voxelMap != null)
                    {
                        var startPos = trajectile.From - voxelMap.PositionLeftBottomCorner;
                        var startInt = Vector3I.Round(startPos);
                        var endPos = trajectile.To - voxelMap.PositionLeftBottomCorner;
                        var endInt = Vector3I.Round(endPos);

                        BresenhamLineDraw(startInt, endInt, testPoints);

                        for (int i = 0; i < testPoints.Count; ++i)
                        {
                            var voxelCoord = testPoints[i];
                            var voxelHit = new VoxelHit();
                            voxelMap.Storage.ExecuteOperationFast(ref voxelHit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);
                            if (voxelHit.HasHit)
                                return (Vector3D)voxelCoord + voxelMap.PositionLeftBottomCorner;
                        }
                    }
                }
            }

            return null;
        }

        internal static bool PointInsideVoxel(MyVoxelBase voxel, MyStorageData tmpStorage, Vector3D pos)
        {
            var voxelMatrix = voxel.PositionComp.WorldMatrixInvScaled;
            var vecMax = new Vector3I(int.MaxValue);
            var vecMin = new Vector3I(int.MinValue);

            var point = pos;
            Vector3D result;
            Vector3D.Transform(ref point, ref voxelMatrix, out result);
            var r = result + (Vector3D)(voxel.Size / 2);
            var v1 = Vector3D.Floor(r);
            Vector3D.Fract(ref r, out r);
            var v2 = v1 + voxel.StorageMin;
            var v3 = v2 + 1;
            if (v2 != vecMax && v3 != vecMin)
            {
                tmpStorage.Resize(v2, v3);
                voxel.Storage.ReadRange(tmpStorage, MyStorageDataTypeFlags.Content, 0, v2, v3);
            }
            var num1 = tmpStorage.Content(0, 0, 0);
            var num2 = tmpStorage.Content(1, 0, 0);
            var num3 = tmpStorage.Content(0, 1, 0);
            var num4 = tmpStorage.Content(1, 1, 0);
            var num5 = tmpStorage.Content(0, 0, 1);
            var num6 = tmpStorage.Content(1, 0, 1);
            var num7 = tmpStorage.Content(0, 1, 1);
            var num8 = tmpStorage.Content(1, 1, 1);
            var num9 = num1 + (num2 - num1) * r.X;
            var num10 = num3 + (num4 - num3) * r.X;
            var num11 = num5 + (num6 - num5) * r.X;
            var num12 = num7 + (num8 - num7) * r.X;
            var num13 = num9 + (num10 - num9) * r.Y;
            var num14 = num11 + (num12 - num11) * r.Y;
            return num13 + (num14 - num13) * r.Z >= sbyte.MaxValue;
        }

        internal static int PointsInsideVoxel(MyVoxelBase voxel, MyStorageData tmpStorage, List<Vector3D> points)
        {
            var voxelMatrix = voxel.PositionComp.WorldMatrixInvScaled;
            var vecMax = new Vector3I(int.MaxValue);
            var vecMin = new Vector3I(int.MinValue);

            var results = 0;
            for (int index = 0; index < points.Count; ++index)
            {
                var point = points[index];
                Vector3D result;
                Vector3D.Transform(ref point, ref voxelMatrix, out result);
                var r = result + (Vector3D)(voxel.Size / 2);
                var v1 = Vector3D.Floor(r);
                Vector3D.Fract(ref r, out r);
                var v2 = v1 + voxel.StorageMin;
                var v3 = v2 + 1;
                if (v2 != vecMax && v3 != vecMin)
                {
                    tmpStorage.Resize(v2, v3);
                    voxel.Storage.ReadRange(tmpStorage, MyStorageDataTypeFlags.Content, 0, v2, v3);
                    vecMax = v2;
                    vecMin = v3;
                }
                var num1 = tmpStorage.Content(0, 0, 0);
                var num2 = tmpStorage.Content(1, 0, 0);
                var num3 = tmpStorage.Content(0, 1, 0);
                var num4 = tmpStorage.Content(1, 1, 0);
                var num5 = tmpStorage.Content(0, 0, 1);
                var num6 = tmpStorage.Content(1, 0, 1);
                var num7 = tmpStorage.Content(0, 1, 1);
                var num8 = tmpStorage.Content(1, 1, 1);
                var num9 = num1 + (num2 - num1) * r.X;
                var num10 = num3 + (num4 - num3) * r.X;
                var num11 = num5 + (num6 - num5) * r.X;
                var num12 = num7 + (num8 - num7) * r.X;
                var num13 = num9 + (num10 - num9) * r.Y;
                var num14 = num11 + (num12 - num11) * r.Y;
                if (num13 + (num14 - num13) * r.Z >= sbyte.MaxValue)
                    ++results;
            }
            return results;
        }
        
        // Math magic by Whiplash
        internal static void BresenhamLineDraw(Vector3I start, Vector3I end, List<Vector3I> points)
        {
            points.Clear();
            points.Add(start);
            Vector3I delta = end - start;
            Vector3I step = Vector3I.Sign(delta);
            delta *= step;
            int max = delta.AbsMax();

            if (max == delta.X)
            {
                int p1 = 2 * delta.Y - delta.X;
                int p2 = 2 * delta.Z - delta.X;
                while (start.X != end.X)
                {
                    start.X += step.X;
                    if (p1 >= 0)
                    {
                        start.Y += step.Y;
                        p1 -= 2 * delta.X;
                    }

                    if (p2 >= 0)
                    {
                        start.Z += step.Z;
                        p2 -= 2 * delta.X;
                    }
                    p1 += 2 * delta.Y;
                    p2 += 2 * delta.Z;
                    points.Add(start);
                }
            }
            else if (max == delta.Y)
            {
                int p1 = 2 * delta.X - delta.Y;
                int p2 = 2 * delta.Z - delta.Y;
                while (start.Y != end.Y)
                {
                    start.Y += step.Y;
                    if (p1 >= 0)
                    {
                        start.X += step.X;
                        p1 -= 2 * delta.Y;
                    }

                    if (p2 >= 0)
                    {
                        start.Z += step.Z;
                        p2 -= 2 * delta.Y;
                    }
                    p1 += 2 * delta.X;
                    p2 += 2 * delta.Z;
                    points.Add(start);
                }
            }
            else
            {
                int p1 = 2 * delta.X - delta.Z;
                int p2 = 2 * delta.Y - delta.Z;
                while (start.Z != end.Z)
                {
                    start.Z += step.Z;
                    if (p1 >= 0)
                    {
                        start.X += step.X;
                        p1 -= 2 * delta.Z;
                    }

                    if (p2 >= 0)
                    {
                        start.Y += step.Y;
                        p2 -= 2 * delta.Z;
                    }
                    p1 += 2 * delta.X;
                    p2 += 2 * delta.Y;
                    points.Add(start);
                }
            }
        }

        internal struct VoxelHit : IVoxelOperator
        {
            internal bool HasHit;

            public void Op(ref Vector3I pos, MyStorageDataTypeEnum dataType, ref byte content)
            {
                if (content != MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                {
                    HasHit = true;
                }
            }

            public VoxelOperatorFlags Flags
            {
                get { return VoxelOperatorFlags.Read; }
            }
        }
    }
}

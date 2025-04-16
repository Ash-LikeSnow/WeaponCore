using System;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace CoreSystems.Support
{

    public static class GridIntersection
    {
        internal static bool BresenhamGridIntersection(MyCubeGrid grid, ref Vector3D worldStart, ref Vector3D worldEnd, out Vector3D? hitPos, MyEntity part = null, Ai ai = null)
        {
            var weaponBlock = part as MyCubeBlock;
            var start = grid.WorldToGridInteger(worldStart);
            var end = grid.WorldToGridInteger(worldEnd);
            Vector3I delta = end - start;
            Vector3I step = Vector3I.Sign(delta);
            delta *= step;
            int max = delta.AbsMax();
            hitPos = null;
            var gMinX = grid.Min.X;
            var gMinY = grid.Min.Y;
            var gMinZ = grid.Min.Z;
            var gMaxX = grid.Max.X;
            var gMaxY = grid.Max.Y;
            var gMaxZ = grid.Max.Z;
            if (ai != null)
            {
                var dir = (worldEnd - worldStart);
                var ray = new RayD(ref worldStart, ref dir);
                var gridMatrix = ai.TopEntity.PositionComp.WorldMatrixRef;

                foreach (var sub in ai.SubGridCache)
                {
                    if (sub == grid) continue;
                    var subDist = sub.PositionComp.WorldVolume.Intersects(ray);
                    if (subDist.HasValue)
                    {
                        var box = ai.TopEntity.PositionComp.LocalAABB;
                        var obb = new MyOrientedBoundingBoxD(box, gridMatrix);

                        Vector3D? ignoreHit;
                        if (obb.Intersects(ref ray) != null && BresenhamGridIntersection(sub, ref worldStart, ref worldEnd, out ignoreHit, part))
                            return true;
                    }
                }
            }

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
                    var contained = gMinX <= start.X && start.X <= gMaxX && (gMinY <= start.Y && start.Y <= gMaxY) && (gMinZ <= start.Z && start.Z <= gMaxZ);
                    if (!contained) return false;

                    MyCube cube;
                    if (grid.TryGetCube(start, out cube) && cube.CubeBlock != weaponBlock?.SlimBlock)
                    {
                        return true;
                    }
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

                    var contained = gMinX <= start.X && start.X <= gMaxX && (gMinY <= start.Y && start.Y <= gMaxY) && (gMinZ <= start.Z && start.Z <= gMaxZ);
                    if (!contained) return false;

                    MyCube cube;
                    if (grid.TryGetCube(start, out cube) && cube.CubeBlock != weaponBlock?.SlimBlock)
                    {
                        return true;
                    }
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

                    var contained = gMinX <= start.X && start.X <= gMaxX && (gMinY <= start.Y && start.Y <= gMaxY) && (gMinZ <= start.Z && start.Z <= gMaxZ);
                    if (!contained) return false;

                    MyCube cube;
                    if (grid.TryGetCube(start, out cube) && cube.CubeBlock != weaponBlock?.SlimBlock)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public struct CellEnumerator
    {
        private double invDx, xFrac, invDy, yFrac, invDz, zFrac;
        private Vector3I unsignedCurrent, directionInt;
        private Vector3I unsignedEnd;

        private static Vector3 SignNonZero(Vector3 tmp)
        {
            return new Vector3(tmp.X >= 0f ? 1 : -1, tmp.Y >= 0f ? 1 : -1, tmp.Z >= 0f ? 1 : -1);
        }

        private static Vector3I SignIntNonZero(Vector3 tmp)
        {
            return new Vector3I(tmp.X >= 0f ? 1 : -1, tmp.Y >= 0f ? 1 : -1, tmp.Z >= 0f ? 1 : -1);
        }

        private static Vector3I GetGridPoint(ref Vector3D v, Vector3I min, Vector3I max)
        {
            Vector3I result = default(Vector3I);
            if (v.X < min.X)
            {
                v.X = result.X = min.X;
            }
            else if (v.X >= max.X + 1)
            {
                v.X = max.X + 1;
                result.X = max.X;
            }
            else
            {
                result.X = (int)Math.Floor(v.X);
            }

            if (v.Y < min.Y)
            {
                v.Y = result.Y = min.Y;
            }
            else if (v.Y >= max.Y + 1)
            {
                v.Y = max.Y + 1;
                result.Y = max.Y;
            }
            else
            {
                result.Y = (int)Math.Floor(v.Y);
            }

            if (v.Z < min.Z)
            {
                v.Z = result.Z = min.Z;
            }
            else if (v.Z >= max.Z + 1)
            {
                v.Z = max.Z + 1;
                result.Z = max.Z;
            }
            else
            {
                result.Z = (int)Math.Floor(v.Z);
            }

            return result;
        }


        public static CellEnumerator EnumerateGridCells(IMyCubeGrid grid, Vector3D worldStart, Vector3D worldEnd,
            Vector3I? gridSizeInflate = null)
        {
            MatrixD worldMatrixNormalizedInv = grid.PositionComp.WorldMatrixNormalizedInv;
            Vector3D localStart;
            Vector3D.Transform(ref worldStart, ref worldMatrixNormalizedInv, out localStart);
            Vector3D localEnd;
            Vector3D.Transform(ref worldEnd, ref worldMatrixNormalizedInv, out localEnd);
            Vector3 gridSizeHalfVector = new Vector3(grid.GridSize / 2);
            localStart += gridSizeHalfVector;
            localEnd += gridSizeHalfVector;
            Vector3I minInflate = grid.Min - Vector3I.One;
            Vector3I maxInflate = grid.Max + Vector3I.One;
            if (gridSizeInflate.HasValue)
            {
                minInflate -= gridSizeInflate.Value;
                maxInflate += gridSizeInflate.Value;
            }
            return new CellEnumerator(localStart, localEnd, minInflate, maxInflate, grid.GridSize);
        }

        public CellEnumerator(Vector3D localStart, Vector3D localEnd, Vector3I min, Vector3I max, float gridSize)
        {
            Vector3D delta = localEnd - localStart;
            Vector3D blockStart = localStart / gridSize;
            Vector3D blockEnd = localEnd / gridSize;

            Vector3 direction = SignNonZero(delta);
            directionInt = SignIntNonZero(delta);
            unsignedCurrent = GetGridPoint(ref blockStart, min, max) * directionInt;
            unsignedEnd = GetGridPoint(ref blockEnd, min, max) * directionInt;
            delta *= direction;
            blockStart *= direction;

            invDx = 1.0 / delta.X;
            xFrac = invDx * (Math.Floor(blockStart.X + 1.0) - blockStart.X);
            invDy = 1.0 / delta.Y;
            yFrac = invDy * (Math.Floor(blockStart.Y + 1.0) - blockStart.Y);
            invDz = 1.0 / delta.Z;
            zFrac = invDz * (Math.Floor(blockStart.Z + 1.0) - blockStart.Z);
        }


        public void MoveNext()
        {
            if (xFrac < zFrac)
            {
                if (xFrac < yFrac)
                {
                    xFrac += invDx;
                    ++unsignedCurrent.X;
                }
                else
                {
                    yFrac += invDy;
                    ++unsignedCurrent.Y;
                }
            }
            else if (zFrac < yFrac)
            {
                zFrac += invDz;
                ++unsignedCurrent.Z;
            }
            else
            {
                yFrac += invDy;
                ++unsignedCurrent.Y;
            }
        }

        public Vector3I Current => unsignedCurrent * directionInt;

        public bool IsValid
        {
            get
            {
                if (xFrac < zFrac)
                {
                    if (xFrac < yFrac)
                        return unsignedCurrent.X <= unsignedEnd.X;
                    return unsignedCurrent.Y <= unsignedEnd.Y;
                }

                if (zFrac < yFrac)
                    return unsignedCurrent.Z <= unsignedEnd.Z;
                return unsignedCurrent.Y <= unsignedEnd.Y;
            }
        }
    }
}

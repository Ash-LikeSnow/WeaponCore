using System;
using System.Collections.Generic;
using System.Text;
using CoreSystems.Projectiles;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace CoreSystems.Support
{
    internal static class SUtils
    {
        public static bool ModActivate(IMyModContext context, IMySession session)
        {
            var priority1 = 2734980390ul;
            var priority2 = 2189703321ul;
            var isP0 = context.ModName == "WeaponCore" || context.ModName == "CoreSystems";
            if (isP0) return true;

            var isP1 = false;
            var isP2 = false;
            var isP3 = false;
            var p0Exists = false;
            var p1Exists = false;
            var p2Exists = false;

            foreach (var mod in session.Mods)
            {
                if (mod.Name == "WeaponCore" || mod.Name == "CoreSystems")
                    p0Exists = true;

                if (mod.PublishedFileId == priority1)
                    p1Exists = true;

                if (mod.PublishedFileId == priority2)
                    p2Exists = true;

                if (mod.Name == context.ModId)
                {
                    if (mod.PublishedFileId == priority1)
                    {
                        isP1 = true;
                    }
                    else if (mod.PublishedFileId == priority2)
                    {
                        isP2 = true;
                    }
                    else if (mod.PublishedFileId == 1918681825 || mod.PublishedFileId == 2496225055 || mod.PublishedFileId == 2726343161)
                    {
                        isP3 = true;
                    }
                }
            }

            if (isP1 && !p0Exists || isP2 && !p0Exists && !p1Exists) return true;
            var validP3 = isP3 && (!p0Exists && !p1Exists && !p2Exists);
            return validP3;
        }

        public static void GetFourInt16FromLong(long id, out int w, out int x, out int y, out int z)
        {

            w = (int)(id >> 48);

            x = (int)((id << 16) >> 48);
            y = (int)((id << 32) >> 48);
            z = (int)((id << 48) >> 48);

        }

        public static void FourInt16ToLong(int w, int x, int y, int z, out long id)
        {

            id = ((long)(w << 16 | x) << 32) | (uint)(y << 16 | z);

        }

        public static double Clamp01(double value)
        {
            if (value < 0.0)
                return 0.0d;
            return value > 1.0 ? 1d : value;
        }

        public static void ReplaceAll(StringBuilder sb, char[] charlist, char replacewith)
        {
            for (int i = 0; i < sb.Length; i++)
            {
                if (charlist.Contains(sb[i]))
                    sb[i] = replacewith;
            }
        }

        public static double Lerp(double a, double b, double t) => a + (b - a) * Clamp01(t);

        public static double InverseLerp(double a, double b, double value) => a != b ? Clamp01((value - a) / (b - a)) : 0.0f;

        public static Vector3 ColorToHSVOffset(Color color)
        {
            return MyColorPickerConstants.HSVToHSVOffset(color.ColorToHSV());
        }

        public static long MakeLong(int left, int right)
        {
            long res = left;
            res <<= 32;
            res |= (uint)right; //uint first to prevent loss of signed bit
            return res;
        }

        static void ShellSort(List<Projectile> list, Vector3D weaponPos)
        {
            int length = list.Count;

            for (int h = length / 2; h > 0; h /= 2)
            {
                for (int i = h; i < length; i += 1)
                {
                    var tempValue = list[i];
                    double temp;
                    Vector3D.DistanceSquared(ref list[i].Position, ref weaponPos, out temp);

                    int j;
                    for (j = i; j >= h && Vector3D.DistanceSquared(list[j - h].Position, weaponPos) > temp; j -= h)
                    {
                        list[j] = list[j - h];
                    }

                    list[j] = tempValue;
                }
            }
        }

        public static IMyTerminalControlOnOffSwitch RefreshToggle;
        public static MyCubeBlock RefreshToggleCube;

        public static void UpdateTerminal(this MyCubeBlock block)
        {
            ((IMyTerminalBlock)block).SetDetailedInfoDirty();
            /*
            try
            {
                if (block == RefreshToggleCube && RefreshToggle != null)
                {
                    RefreshTerminalControls((IMyTerminalBlock)block);
                    return;
                }

                if (!GetRefreshToggle())
                    return;

                RefreshToggleCube = block;
                RefreshTerminalControls((IMyTerminalBlock)block);
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateTerminal: {ex}"); }
            */
        }

        public static bool GetRefreshToggle()
        {

            List<IMyTerminalControl> items;
            MyAPIGateway.TerminalControls.GetControls<IMyTerminalBlock>(out items);

            foreach (var item in items) {
                
                if (item.Id == "ShowInToolbarConfig") {
                    RefreshToggle = (IMyTerminalControlOnOffSwitch)item;
                    break;
                }
            }
            return RefreshToggle != null;
        }
        
        //forces GUI refresh
        public static void RefreshTerminalControls(IMyTerminalBlock b)
        {
            if (RefreshToggle != null) {
                
                var originalSetting = RefreshToggle.Getter(b);
                RefreshToggle.Setter(b, !originalSetting);
                RefreshToggle.Setter(b, originalSetting);

            }
        }

        /*
        public static void UpdateTerminal(this MyCubeBlock block)
        {
            MyOwnershipShareModeEnum shareMode;
            long ownerId;
            if (block.IDModule != null)
            {
                ownerId = block.IDModule.Owner;
                shareMode = block.IDModule.ShareMode;
            }
            else
            {
                var sorter = block as IMyTerminalBlock;
                if (sorter != null)
                {
                    sorter.ShowOnHUD = !sorter.ShowOnHUD;
                    sorter.ShowOnHUD = !sorter.ShowOnHUD;
                }
                return;
            }
            block.ChangeOwner(ownerId, shareMode == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
            block.ChangeOwner(ownerId, shareMode);
        }
        */
        public static void SphereCloud(int pointLimit, Vector3D[] physicsArray, MyEntity shieldEnt, bool transformAndScale, bool debug, Random rnd = null)
        {
            if (pointLimit > 10000) pointLimit = 10000;
            if (rnd == null) rnd = new Random(0);

            var sPosComp = shieldEnt.PositionComp;
            var unscaledPosWorldMatrix = MatrixD.Rescale(MatrixD.CreateTranslation(sPosComp.WorldAABB.Center), sPosComp.WorldVolume.Radius);
            var radius = sPosComp.WorldVolume.Radius;
            for (int i = 0; i < pointLimit; i++)
            {
                var value = rnd.Next(0, physicsArray.Length - 1);
                var phi = 2 * Math.PI * i / pointLimit;
                var x = (float)(radius * Math.Sin(phi) * Math.Cos(value));
                var z = (float)(radius * Math.Sin(phi) * Math.Sin(value));
                var y = (float)(radius * Math.Cos(phi));
                var v = new Vector3D(x, y, z);

                if (transformAndScale) v = Vector3D.Transform(Vector3D.Normalize(v), unscaledPosWorldMatrix);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
                physicsArray[i] = v;
            }
        }

        public static void UnitSphereCloudQuick(int pointLimit, ref Vector3D[] physicsArray, MyEntity shieldEnt, bool translateAndScale, bool debug, Random rnd = null)
        {
            if (pointLimit > 10000) pointLimit = 10000;
            if (rnd == null) rnd = new Random(0);

            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;
            var v = Vector3D.Zero;

            for (int i = 0; i < pointLimit; i++)
            {
                while (true)
                {
                    v.X = (rnd.NextDouble() * 2) - 1;
                    v.Y = (rnd.NextDouble() * 2) - 1;
                    v.Z = (rnd.NextDouble() * 2) - 1;
                    var len2 = v.LengthSquared();
                    if (len2 < .0001) continue;
                    v *= radius / Math.Sqrt(len2);
                    break;
                }

                if (translateAndScale) physicsArray[i] = v += center;
                else physicsArray[i] = v;
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void UnitSphereRandomOnly(ref Vector3D[] physicsArray, Random rnd = null)
        {
            if (rnd == null) rnd = new Random(0);
            var v = Vector3D.Zero;

            for (int i = 0; i < physicsArray.Length; i++)
            {
                v.X = 0;
                v.Y = 0;
                v.Z = 0;
                while ((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z) < 0.0001)
                {
                    v.X = (rnd.NextDouble() * 2) - 1;
                    v.Y = (rnd.NextDouble() * 2) - 1;
                    v.Z = (rnd.NextDouble() * 2) - 1;
                }
                v.Normalize();
                physicsArray[i] = v;
            }
        }

        public static void UnitSphereTranslateScale(int pointLimit, ref Vector3D[] physicsArray, ref Vector3D[] scaledCloudArray, MyEntity shieldEnt, bool debug)
        {
            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;

            for (int i = 0; i < pointLimit; i++)
            {
                var v = physicsArray[i];
                scaledCloudArray[i] = v = center + (radius * v);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void UnitSphereTranslateScaleList(int pointLimit, ref Vector3D[] physicsArray, ref List<Vector3D> scaledCloudList, MyEntity shieldEnt, bool debug, MyEntity grid, bool rotate = true)
        {
            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;
            var gMatrix = grid.WorldMatrix;
            for (int i = 0; i < pointLimit; i++)
            {
                var v = physicsArray[i];
                if (rotate) Vector3D.Rotate(ref v, ref gMatrix, out v);
                v = center + (radius * v);
                scaledCloudList.Add(v);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void DetermisticSphereCloud(List<Vector3D> physicsArray, int pointsInSextant)
        {
            physicsArray.Clear();
            int stepsPerCoord = (int)Math.Sqrt(pointsInSextant);
            double radPerStep = MathHelperD.PiOver2 / stepsPerCoord;

            for (double az = -MathHelperD.PiOver4; az < MathHelperD.PiOver4; az += radPerStep)
            {
                for (double el = -MathHelperD.PiOver4; el < MathHelperD.PiOver4; el += radPerStep)
                {
                    Vector3D vec;
                    Vector3D.CreateFromAzimuthAndElevation(az, el, out vec);
                    Vector3D vec2 = new Vector3D(vec.Z, vec.X, vec.Y);
                    Vector3D vec3 = new Vector3D(vec.Y, vec.Z, vec.X);
                    physicsArray.Add(vec); //first sextant
                    physicsArray.Add(vec2); //2nd sextant
                    physicsArray.Add(vec3); //3rd sextant
                    physicsArray.Add(-vec); //4th sextant
                    physicsArray.Add(-vec2); //5th sextant
                    physicsArray.Add(-vec3); //6th sextant
                }
            }
        }

        public static Vector3D? GetLineIntersectionExactAll(MyCubeGrid grid, ref LineD line, out double distance, out IMySlimBlock intersectedBlock)
        {
            intersectedBlock = null;
            distance = 3.40282346638529E+38;
            Vector3I? nullable = new Vector3I?();
            Vector3I zero = Vector3I.Zero;
            double distanceSquared = double.MaxValue;
            if (grid.GetLineIntersectionExactGrid(ref line, ref zero, ref distanceSquared))
            {
                distanceSquared = Math.Sqrt(distanceSquared);
                nullable = zero;
            }
            if (!nullable.HasValue)
                return new Vector3D?();
            distance = distanceSquared;
            intersectedBlock = grid.GetCubeBlock(nullable.Value);
            if (intersectedBlock == null)
                return new Vector3D?();
            return zero;
        }

        public static void CreateVoxelExplosion(Session session, float damage, double radius, Vector3D position, Vector3D direction, MyEntity owner, MyEntity hitEnt, WeaponDefinition.AmmoDef ammoDef, bool forceNoDraw = false)
        {

            var sphere = new BoundingSphereD(position, radius);
            var eFlags = MyExplosionFlags.AFFECT_VOXELS;

            var explosionInfo = new MyExplosionInfo
            {
                PlayerDamage = 0.1f,
                Damage = damage,
                ExplosionType = MyExplosionTypeEnum.MISSILE_EXPLOSION,
                ExplosionSphere = sphere,
                LifespanMiliseconds = 0,
                HitEntity = hitEnt,
                OwnerEntity = owner,
                Direction = direction,
                VoxelExplosionCenter = sphere.Center,
                ExplosionFlags = eFlags,
                VoxelCutoutScale = 0.3f,
                PlaySound = false,
                ApplyForceAndDamage = true,
                KeepAffectedBlocks = true,
                CreateParticleEffect = false,
            };
            if (hitEnt?.Physics != null)
                explosionInfo.Velocity = hitEnt.Physics.LinearVelocity;
            MyExplosions.AddExplosion(ref explosionInfo);
        }

        public static void GetBlockOrientedBoundingBox(MyCubeBlock block, out MyOrientedBoundingBoxD blockBox)
        {
            var quat = Quaternion.CreateFromRotationMatrix(block.PositionComp.WorldMatrixRef);
            double factor = (block.BlockDefinition.CubeSize == MyCubeSize.Large ? 2.5d : 0.5d);
            var halfExtents = new Vector3D(block.BlockDefinition.Size) * factor / 2d;
            var worldMin = Vector3D.Transform(new Vector3D(block.Min) * factor, block.CubeGrid.PositionComp.WorldMatrixRef);
            var worldMax = Vector3D.Transform(new Vector3D(block.Max) * factor, block.CubeGrid.PositionComp.WorldMatrixRef);
            blockBox = new MyOrientedBoundingBoxD((worldMin + worldMax) / 2d, halfExtents, quat);
        }
    }
}

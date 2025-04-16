using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace CoreSystems.Support
{
    internal static class SUtils
    {
        public static bool ModActivate(IMyModContext context, IMySession session)
        {
            var priority1 = 3149625043ul; //Starcore
            //var priority2 = 3154371364ul;
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

                //if (mod.PublishedFileId == priority2)
                //    p2Exists = true;

                if (mod.Name == context.ModId)
                {
                    if (mod.PublishedFileId == priority1)
                    {
                        isP1 = true;
                    }
                    //else if (mod.PublishedFileId == priority2)
                    //{
                    //    isP2 = true;
                    //}
                    else if (mod.PublishedFileId == 3154371364ul)
                    {
                        isP3 = true;
                    }
                }
            }

            if (isP1 && !p0Exists || isP2 && !p0Exists && !p1Exists) return true;
            var validP3 = isP3 && (!p0Exists && !p1Exists && !p2Exists);
            return validP3;
        }

        public static void ReplaceAll(StringBuilder sb, char[] charlist, char replacewith)
        {
            for (int i = 0; i < sb.Length; i++)
            {
                if (charlist.Contains(sb[i]))
                    sb[i] = replacewith;
            }
        }
        public static Vector3 ColorToHSVOffset(Color color)
        {
            return MyColorPickerConstants.HSVToHSVOffset(color.ColorToHSV());
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

        public static void UpdateTerminalForced(this MyCubeBlock block)
        {
            ((IMyTerminalBlock)block).SetDetailedInfoDirty();
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

using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Support
{
    public static class DsDebugDraw
    {
        #region Debug and Utils
        private static MyStringId _square = MyStringId.GetOrCompute("Square");

        public static void DrawX(Vector3D center, MatrixD referenceMatrix, double lineLength)
        {
            var halfLineLength = lineLength * 0.5;
            var lineWdith = (float)(lineLength * 0.1);
            var color1 = (Vector4)Color.Red;
            var color2 = (Vector4)Color.Yellow;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var testDir1 = Vector3D.Normalize(referenceMatrix.Left - referenceMatrix.Right);
            var line0Vec0 = center + (testDir0 * -halfLineLength);
            var line0Vec1 = center + (testDir0 * halfLineLength);

            var line1Vec0 = center + (testDir1 * -halfLineLength);
            var line1Vec1 = center + (testDir1 * halfLineLength);
            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, _square, ref color1, lineWdith);
            MySimpleObjectDraw.DrawLine(line1Vec0, line1Vec1, _square, ref color2, lineWdith);
        }

        public static void DrawLosBlocked(Vector3D center, MatrixD referenceMatrix, double length)
        {
            var halfLength = length * 0.5;
            var width = (float)length * 0.05f;
            var color1 = (Vector4)Color.DarkOrange;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var line0Vec0 = center + (testDir0 * -halfLength);
            var line0Vec1 = center + (testDir0 * halfLength);

            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, _square, ref color1, width);
        }

        public static void DrawLosClear(Vector3D center, MatrixD referenceMatrix, double length)
        {
            var halfLength = length * 0.5;
            var width = (float)length * 0.05f;
            var color1 = (Vector4)Color.Green;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var line0Vec0 = center + (testDir0 * -halfLength);
            var line0Vec1 = center + (testDir0 * halfLength);

            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, _square, ref color1, width);
        }

        public static void DrawMark(Vector3D center, MatrixD referenceMatrix, int length)
        {
            var halfLength = length * 0.5;
            var width = (float)(halfLength * 0.1);

            var color1 = (Vector4)Color.Green;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var line0Vec0 = center + (testDir0 * -halfLength);
            var line0Vec1 = center + (testDir0 * halfLength);

            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, _square, ref color1, width);
        }

        public static void DrawLine(Vector3D start, Vector3D end, Vector4 color, float width)
        {
            var c = color;
            MySimpleObjectDraw.DrawLine(start, end, _square, ref c, width);
        }

        public static void DrawLine(LineD line, Vector4 color, float width)
        {
            var c = color;
            MySimpleObjectDraw.DrawLine(line.From, line.To, _square, ref c, width);
        }

        public static void DrawLine(Vector3D start, Vector3D dir, Vector4 color, float width, float length)
        {
            var c = color;
            MySimpleObjectDraw.DrawLine(start, start + (dir * length), _square, ref c, width);
        }
        
        public static void DrawRay(RayD ray, Vector4 color, float width, float length = float.MaxValue)
        {
            var c = color;
            MyTransparentGeometry.AddLineBillboard(_square, c, ray.Position, ray.Direction, length, width);
        }

        public static void DrawBox(MyOrientedBoundingBoxD obb, Color color)
        {
            var box = new BoundingBoxD(-obb.HalfExtent, obb.HalfExtent);
            var wm = MatrixD.CreateFromTransformScale(obb.Orientation, obb.Center, Vector3D.One);
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref box, ref color, MySimpleObjectRasterizer.Solid, 1);
        }

        public static void DrawAABB(MatrixD worldMatrix, BoundingBoxD localbox, Color color, MySimpleObjectRasterizer raster = MySimpleObjectRasterizer.Wireframe, float thickness = 0.01f)
        {
            MySimpleObjectDraw.DrawTransparentBox(ref worldMatrix, ref localbox, ref color, raster, 1, thickness, MyStringId.GetOrCompute("Square"), MyStringId.GetOrCompute("Square"));
        }

        public static void DrawSingleVec(Vector3D vec, float size, Color color, bool solid = true, int divideRatio = 20, float lineWidth = 0.5f)
        {
            DrawScaledPoint(vec, size, color, divideRatio, solid, lineWidth);
        }

        public static void DrawScaledPoint(Vector3D pos, double radius, Color color, int divideRatio = 1, bool solid = true, float lineWidth = -1)
        {
            var posMatCenterScaled = MatrixD.CreateTranslation(pos);
            var posMatScaler = MatrixD.Rescale(posMatCenterScaled, radius);
            var material = MyStringId.GetOrCompute("square");
            MySimpleObjectDraw.DrawTransparentSphere(ref posMatScaler, 1f, ref color, solid ? MySimpleObjectRasterizer.Solid : MySimpleObjectRasterizer.Wireframe, divideRatio, null, material, lineWidth);
        }

        public static void DrawSphere(BoundingSphereD sphere, Color color)
        {
            var rangeGridResourceId = MyStringId.GetOrCompute("Sqaure");
            var radius = sphere.Radius;
            var transMatrix = MatrixD.CreateTranslation(sphere.Center);
            //var wm = MatrixD.Rescale(transMatrix, radius);

            MySimpleObjectDraw.DrawTransparentSphere(ref transMatrix, (float)radius, ref color, MySimpleObjectRasterizer.Solid, 20, null, rangeGridResourceId);
        }
        #endregion
    }
}

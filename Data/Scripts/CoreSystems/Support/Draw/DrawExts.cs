using System;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace CoreSystems.Support
{
    public static class TransparentRenderExt
    {
        public static void DrawTransparentCylinder(ref MatrixD worldMatrix,
    float radiusBase, float radiusTop, float length, int wireDivideRatio,
    Vector4 faceColor, Vector4 lineColor,
    MyStringId? faceMaterial = null, MyStringId? lineMaterial = null, float lineThickness = 0.01f,
    BlendTypeEnum lineBlendType = BlendTypeEnum.Standard, BlendTypeEnum faceBlendType = BlendTypeEnum.Standard,
    bool capEnds = true)
        {
            Vector3D dir = worldMatrix.Forward;
            Vector3D centerBottom = worldMatrix.Translation;
            Vector3D centerTop = worldMatrix.Translation + dir * length;

            Vector3D currBottom = Vector3D.Zero;
            Vector3D currTop = Vector3D.Zero;

            Vector3D prevBottom = Vector3D.Zero;
            Vector3D prevTop = Vector3D.Zero;

            float stepDeg = 360f / wireDivideRatio;

            for (int i = 0; i <= wireDivideRatio; i++)
            {
                float angle = MathHelper.ToRadians(i * stepDeg);

                currBottom.X = radiusBase * Math.Cos(angle);
                currBottom.Y = radiusBase * Math.Sin(angle);
                currBottom.Z = 0;
                currBottom = Vector3D.Transform(currBottom, worldMatrix);

                currTop.X = radiusTop * Math.Cos(angle);
                currTop.Y = radiusTop * Math.Sin(angle);
                currTop.Z = -length;
                currTop = Vector3D.Transform(currTop, worldMatrix);

                if (lineMaterial.HasValue)
                    MyTransparentGeometry.AddLineBillboard(lineMaterial.Value, lineColor, currBottom, (currTop - currBottom), 1f, lineThickness, lineBlendType);

                if (i > 0)
                {
                    if (lineMaterial.HasValue)
                    {
                        MyTransparentGeometry.AddLineBillboard(lineMaterial.Value, lineColor, prevBottom, (currBottom - prevBottom), 1f, lineThickness, lineBlendType);
                        MyTransparentGeometry.AddLineBillboard(lineMaterial.Value, lineColor, prevTop, (currTop - prevTop), 1f, lineThickness, lineBlendType);
                    }

                    if (faceMaterial.HasValue)
                    {
                        var quad = new MyQuadD
                        {
                            Point0 = prevTop,
                            Point1 = currTop,
                            Point2 = currBottom,
                            Point3 = prevBottom
                        };
                        MyTransparentGeometry.AddQuad(faceMaterial.Value, ref quad, faceColor, ref currTop, blendType: faceBlendType);

                        if (capEnds)
                        {
                            var color = faceColor.ToLinearRGB(); // HACK keeping color consistent with AddQuad() and AddLineBillboard()

                            MyTransparentGeometry.AddTriangleBillboard(centerTop, currTop, prevTop, dir, dir, dir,
                                Vector2.Zero, Vector2.Zero, Vector2.Zero, faceMaterial.Value, 0, currTop, color, faceBlendType);

                            MyTransparentGeometry.AddTriangleBillboard(centerBottom, currBottom, prevBottom, -dir, -dir, -dir,
                                Vector2.Zero, Vector2.Zero, Vector2.Zero, faceMaterial.Value, 0, currBottom, color, faceBlendType);
                        }
                    }
                }

                prevBottom = currBottom;
                prevTop = currTop;
            }
        }
    }
}

using System.Collections.Generic;
using CoreSystems;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Hud
{
    partial class Hud
    {
        internal void DrawText()
        {
            _cameraWorldMatrix = Session.I.Camera.WorldMatrix;

            if (NeedsUpdate)
                UpdateHudSettings();

            AddAgingText();
            AgingTextDraw();
        }

        private void AddAgingText()
        {
            foreach (var aging in _agingTextRequests)
            {

                var textAdd = aging.Value;

                if (textAdd.Data.Count > 0)
                    continue;

                var scaleShadow = textAdd.Font == FontType.Shadow;
                var remap = scaleShadow ? _shadowCharWidthMap : _monoCharWidthMap;
                float messageLength = 0;
                for (int j = 0; j < textAdd.Text.Length; j++)
                {

                    var c = textAdd.Text[j];

                    float size;
                    var needResize = remap.TryGetValue(c, out size);

                    var scaledWidth = textAdd.FontSize * (needResize ? size : scaleShadow ? ShadowWidthScaler : MonoWidthScaler);
                    messageLength += scaledWidth;

                    var map = CharacterMap[textAdd.Font];

                    TextureMap cm;
                    if (!map.TryGetValue(c, out cm))
                        continue;

                    var td = _textDataPool.Get();

                    td.Material = cm.Material;
                    td.P0 = cm.P0;
                    td.P1 = cm.P1;
                    td.P2 = cm.P2;
                    td.P3 = cm.P3;
                    td.UvDraw = true;
                    td.ReSize = needResize;
                    td.ScaledWidth = scaledWidth;
                    textAdd.Data.Add(td);
                }
                textAdd.MessageWidth = messageLength;
                textAdd.Data.ApplyAdditions();
            }
        }

        private void AgingTextDraw()
        {
            var up = (Vector3)_cameraWorldMatrix.Up;
            var left = (Vector3)_cameraWorldMatrix.Left;

            foreach (var textAdd in _agingTextRequests.Values)
            {

                textAdd.Position.Z = _viewPortSize.Z;
                var requestPos = textAdd.Position;
                requestPos.Z = _viewPortSize.Z;
                var widthScaler = textAdd.Font == FontType.Shadow ? ShadowSizeScaler : 1f;

                var textPos = Vector3D.Transform(requestPos, _cameraWorldMatrix);
                switch (textAdd.Justify)
                {
                    case Justify.Center:
                        textPos += _cameraWorldMatrix.Left * (((textAdd.MessageWidth * ShadowWidthScaler) * 0.5f) * widthScaler);
                        break;
                    case Justify.Right:
                        textPos -= _cameraWorldMatrix.Left * ((textAdd.MessageWidth * ShadowWidthScaler) * widthScaler);
                        break;
                    case Justify.Left:
                        textPos -= _cameraWorldMatrix.Right * ((textAdd.MessageWidth * ShadowWidthScaler) * widthScaler);
                        break;
                    case Justify.None:
                        textPos -= _cameraWorldMatrix.Left * ((textAdd.FontSize * 0.5f) * widthScaler);
                        break;
                }

                var height = textAdd.FontSize * textAdd.HeightScale;
                var remove = textAdd.Ttl-- < 0;

                for (int i = 0; i < textAdd.Data.Count; i++)
                {

                    var textData = textAdd.Data[i];
                    textData.WorldPos.Z = _viewPortSize.Z;

                    if (textData.UvDraw)
                    {

                        var width = (textData.ScaledWidth * widthScaler) * Session.I.AspectRatioInv;
                        MyQuadD quad;
                        MyUtils.GetBillboardQuadOriented(out quad, ref textPos, width, height, ref left, ref up);

                        if (textAdd.Color != Vector4.Zero)
                        {
                            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textData.P0, textData.P1, textData.P3, textData.Material, 0, textPos, textAdd.Color, textData.Blend);
                            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textData.P0, textData.P2, textData.P3, textData.Material, 0, textPos, textAdd.Color, textData.Blend);
                        }
                        else
                        {
                            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textData.P0, textData.P1, textData.P3, textData.Material, 0, textPos, textData.Blend);
                            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textData.P0, textData.P2, textData.P3, textData.Material, 0, textPos, textData.Blend);
                        }
                    }

                    textPos -= _cameraWorldMatrix.Left * textData.ScaledWidth;

                    if (remove)
                    {
                        textAdd.Data.Remove(textData);
                        _textDataPool.Return(textData);
                    }
                }

                textAdd.Data.ApplyRemovals();
                AgingTextRequest request;
                if (textAdd.Data.Count == 0 && _agingTextRequests.TryRemove(textAdd.ElementId, out request))
                {
                    _agingTextRequests.Remove(textAdd.ElementId);
                    _agingTextRequestPool.Return(request);
                }

            }
            AgingTextures = _agingTextRequests.Count > 0;
        }

    }
}

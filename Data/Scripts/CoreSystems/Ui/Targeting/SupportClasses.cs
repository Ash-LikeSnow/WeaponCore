using CoreSystems;
using VRage.Utils;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Targeting
{
    public class TargetStatus
    {
        public enum Awareness
        {
            SEEKING,
            FOCUSFIRE,
            TRACKING,
            STALKING,
            OBLIVIOUS,
            WONDERING,
        }

        public float ShieldHealth;
        public int ShieldHeat;
        public Vector3I ShieldFaces;
        public int ThreatLvl;
        public float Speed;
        public int Engagement;
        public float ShieldMod;
        public float SizeExtended;
        public double RealDistance;
        public Awareness Aware;
        public string Name;
    }

    public struct LeadInfo
    {
        public bool WillHit;
        public long Group;
        public Vector3D Position;
        public float Length;
    }

    public class HudInfo
    {
        private readonly MyStringId _textureName;
        private readonly Vector2 _screenPosition;
        private readonly float _definedScale;

        public HudInfo(MyStringId textureName, Vector2 screenPosition, float scale)
        {
            _definedScale = scale;
            _textureName = textureName;
            _screenPosition = screenPosition;
        }

        public void GetTextureInfo(Session session, out MyStringId textureName, out float scale, out float screenScale, out float fontScale, out Vector3D offset, out Vector2 localOffset)
        {
            var fovScale = (float)(0.1 * session.ScaleFov);

            localOffset = _screenPosition + session.Settings.ClientConfig.HudPos;

            scale = session.Settings.ClientConfig.HudScale * _definedScale;
            screenScale = scale * fovScale;
            fontScale = (float)(scale * session.ScaleFov);
            var position = new Vector2(localOffset.X, localOffset.Y);
            position.X *= fovScale * session.AspectRatio;
            position.Y *= fovScale;

            offset = Vector3D.Transform(new Vector3D(position.X, position.Y, -.1), session.CameraMatrix);
            textureName = _textureName;
        }
    }
}

using VRage.Game.Entity;
using VRageMath;

namespace CoreSystems.Support
{
    class PartInfo
    {
        internal PartInfo(MyEntity entity, bool isCoreEntity, bool parentIsCoreEntity, PartTypes type)
        {
            Entity = entity;
            IsCoreEntity = isCoreEntity;
            ParentIsCoreEntity = parentIsCoreEntity;
            ParentNull = entity.Parent == null;
            Parent = entity.Parent;
            Type = type;
        }

        public enum PartTypes
        {
            Muzzle,
            Az,
            El,
            Spin,
        }

        internal readonly PartTypes Type;
        internal readonly bool ParentNull;
        internal readonly bool IsCoreEntity;
        internal readonly bool ParentIsCoreEntity;
        internal MyEntity Entity;
        internal MyEntity Parent;
        internal Matrix ToTransformation;
        internal Matrix FromTransformation;
        internal Matrix FullRotationStep;
        internal Matrix RevFullRotationStep;
        internal Matrix OriginalPosition;
        internal Vector3 PartLocalLocation;
        internal Vector3 RotationAxis;

        internal void Reset(MyEntity entity)
        {
            Entity = entity;
            Parent = entity.Parent;
        }
    }
}

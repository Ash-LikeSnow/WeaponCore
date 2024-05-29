using System.Collections.Generic;
using CoreSystems.Platform;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Support.CoreComponent;

namespace CoreSystems.Support
{
    // based on code of Equinox's
    public class Dummy
    {
        internal bool NullEntity => _entity == null || _entity.MarkedForClose;

        internal MyEntity Entity
        {
            get
            {
                if (_entity == null || _entity.MarkedForClose || _entity.Model == null) {
                    _part.BaseComp?.Platform?.ResetParts();
                }

                return _entity;
            }
            set
            {
                if (value?.Model == null && _part.BaseComp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom)
                    Log.Line($"DummyModel null for weapon on set: {_part.CoreSystem.PartName}");
                _entity = value; 

            }
        }
        //internal MyEntity Entity ;

        private IMyModel _cachedModel;
        private IMyModel _cachedSubpartModel;
        private MyEntity _cachedSubpart;
        private MatrixD? _cachedDummyMatrix;
        internal Vector3D CachedPos;
        internal Vector3D CachedDir;
        internal Vector3D CachedUpDir;
        internal bool Ejector;
        

        private readonly string[] _path;
        private readonly Dictionary<string, IMyModelDummy> _tmp1 = new Dictionary<string, IMyModelDummy>();
        private readonly Dictionary<string, IMyModelDummy> _tmp2 = new Dictionary<string, IMyModelDummy>();
        private readonly Part _part;
        private readonly DummyInfo _info = new DummyInfo();
        private MyEntity _entity;
        public Dummy(MyEntity e, Part w, bool ejector, params string[] path)
        {
            _part = w;
            Entity = e;
            _path = path;
            Ejector = ejector;
        }

        private bool _failed = true;
        internal void UpdateModel()
        {
            if (_entity == null || _entity.MarkedForClose)
                return;

            _cachedModel = _entity.Model;
            _cachedSubpart = _entity;
            _cachedSubpartModel = _cachedSubpart?.Model;
            for (var i = 0; i < _path.Length - 1; i++)
            {
                MyEntitySubpart part;
                if (_cachedSubpart != null && _cachedSubpart.TryGetSubpart(_path[i], out part))
                    _cachedSubpart = part;
                else
                {
                    _tmp2.Clear();
                    ((IMyModel) _cachedSubpart?.Model)?.GetDummies(_tmp2);
                    _failed = true;
                    return;
                }
            }

            _cachedSubpartModel = _cachedSubpart?.Model;
            _cachedDummyMatrix = null;
            _tmp1.Clear();
            _cachedSubpartModel?.GetDummies(_tmp1);

            IMyModelDummy dummy;
            if (_tmp1.TryGetValue(_path[_path.Length - 1], out dummy))
            {
                _cachedDummyMatrix = MatrixD.Normalize(dummy.Matrix);
                _failed = false;
                return;
            }
            _failed = true;
        }

        internal void UpdatePhantom()
        {
            if (_entity == null || _entity.MarkedForClose)
                return;

            _cachedSubpart = _entity;
            _cachedDummyMatrix = MatrixD.Identity;
            _failed = false;
        }


        public DummyInfo Info
        {
            get
            {

                if (_part == null || _part.BaseComp.TypeSpecific != CoreComponent.CompTypeSpecific.Phantom) {

                    if (!(_cachedModel == _entity?.Model && _cachedSubpartModel == _cachedSubpart?.Model)) UpdateModel();

                    if (_entity == null || _entity.MarkedForClose || _cachedSubpart == null)
                    {
                        Log.Line("DummyInfo invalid");
                        return new DummyInfo();
                    }

                }
                else
                    UpdatePhantom();

                var dummyMatrix = _cachedDummyMatrix ?? MatrixD.Identity;
                var rifle = _part != null && _part.BaseComp.TypeSpecific == CompTypeSpecific.Rifle;
                var rifleIsDedidcatedOrDebug = rifle && (Session.I.DedicatedServer || Session.I.DebugMod);
                var localPos = dummyMatrix.Translation;
                var localDir = dummyMatrix.Forward;
                var localUpDir = dummyMatrix.Up;
                var partWorldMatrix = _cachedSubpart.PositionComp.WorldMatrixRef;
                if (rifleIsDedidcatedOrDebug) { // blame keen
                    var wComp = (Weapon.WeaponComponent)_part.BaseComp;
                    var offset = !wComp.Rifle.GunBase.HasIronSightsActive;
                    partWorldMatrix = wComp.GetHandWeaponApproximateWorldMatrix(offset);
                    Vector3D.Transform(ref localPos, ref partWorldMatrix, out CachedPos);
                    if (Ejector)
                        Vector3D.TransformNormal(ref localDir, ref partWorldMatrix, out CachedDir);
                    else
                        CachedDir = Vector3D.Normalize(wComp.CharacterPosComp.LogicalCrosshairPoint - CachedPos);
                    CachedUpDir = partWorldMatrix.Up;
                }
                else
                {
                    if (rifle)
                        partWorldMatrix.Translation += (_part.BaseComp.TopEntity.Physics.LinearVelocity * (float) Session.I.DeltaStepConst);

                    Vector3D.Transform(ref localPos, ref partWorldMatrix, out CachedPos);
                    bool clientRifleOffset = false;
                    if (rifle) // I lack the words to describe the level of my disgust
                    {
                        var wComp = (Weapon.WeaponComponent)_part.BaseComp;
                        clientRifleOffset = !wComp.Rifle.GunBase.HasIronSightsActive && !Ejector;
                        var keenWhyPartWorldMatrix = wComp.GetHandWeaponApproximateWorldMatrix(clientRifleOffset);
                        CachedUpDir = keenWhyPartWorldMatrix.Up;
                        if (clientRifleOffset)
                            CachedDir = Vector3D.Normalize(wComp.CharacterPosComp.LogicalCrosshairPoint - CachedPos);
                    }
                    else
                        Vector3D.TransformNormal(ref localUpDir, ref partWorldMatrix, out CachedUpDir);

                    if (!clientRifleOffset)
                        Vector3D.TransformNormal(ref localDir, ref partWorldMatrix, out CachedDir);
                }

                _info.Position = CachedPos;
                _info.LocalPosition = localPos;
                _info.Direction = CachedDir;
                _info.UpDirection = CachedUpDir;
                _info.ParentMatrix = partWorldMatrix;
                _info.DummyMatrix = dummyMatrix;
                _info.Entity = _entity;

                return _info;
            }
        }

        public class DummyInfo
        {
            public Vector3D Position;
            public Vector3D LocalPosition;
            public Vector3D Direction;
            public Vector3D UpDirection;
            public MatrixD ParentMatrix;
            public MatrixD DummyMatrix;
            public MyEntity Entity;
        }
    }
}

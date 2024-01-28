using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using static CoreSystems.Session;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;
namespace CoreSystems.Support { 
    public class PartAnimation
    {
        internal readonly string AnimationId;
        internal readonly EventTriggers EventTrigger;
        internal readonly Matrix[] RotationSet;
        internal readonly Matrix[] RotCenterSet;
        internal readonly Matrix FinalPos;
        internal readonly Matrix HomePos;
        internal readonly AnimationType[] TypeSet;
        internal readonly Dictionary<EventTriggers, string> EventIdLookup = new Dictionary<EventTriggers, string>();
        internal readonly CoreSystem System;
        internal readonly int[] CurrentEmissivePart;
        internal readonly int[][] MoveToSetIndexer;
        internal readonly int NumberOfMoves;
        internal readonly uint MotionDelay;
        internal readonly bool DoesLoop;
        internal readonly bool DoesReverse;
        internal readonly bool TriggerOnce;
        internal readonly bool HasMovement;
        internal readonly bool MovesPivotPos;
        internal readonly bool ResetEmissives;
        internal readonly string Muzzle;
        internal readonly string SubpartId;
        internal readonly string[] EmissiveIds;
        internal readonly string[] EmissiveParts;

        internal enum Indexer
        {
            MoveIndex,
            RotationIndex,
            RotCenterIndex,
            TypeIndex,
            EmissiveIndex,
            EmissivePartIndex,
        }

        public struct EmissiveState
        {
            internal string[] EmissiveParts;
            internal int CurrentPart;
            internal Color CurrentColor;
            internal float CurrentIntensity;
            internal bool CycleParts;
            internal bool LeavePreviousOn;
        }

        internal MyEntity MainEnt;
        internal MyEntity Part;
        internal string[] RotCenterNameSet;
        internal bool Reverse;
        internal bool Looping;
        internal bool Running;
        internal bool Triggered;
        internal bool CanPlay;
        //internal bool Paused;
        internal uint StartTick;
        internal List<uint> PlayTicks;

        private int _currentMove;
        private EmissiveState LastEmissive;
        private Guid _uid;

        internal int CurrentMove
        {
            get { return _currentMove; }
        }

        internal PartAnimation(EventTriggers eventTrigger, string animationId, Matrix[] rotationSet, Matrix[] rotCeterSet, AnimationType[] typeSet,string[] emissiveIds, int[] currentEmissivePart, int[][] moveToSetIndexer, string subpartId, MyEntity part, MyEntity mainEnt, string muzzle, uint motionDelay, CoreSystem system, bool loop = false, bool reverse = false, bool triggerOnce = false, bool resetEmissives = false)
        {
            EventTrigger = eventTrigger;
            RotationSet = rotationSet;
            RotCenterSet = rotCeterSet;
            CurrentEmissivePart = currentEmissivePart;
            AnimationId = animationId;
            ResetEmissives = resetEmissives;
            EmissiveIds = emissiveIds;

            //Unique Animation ID
            _uid = Guid.NewGuid();

            TypeSet = typeSet;
            Muzzle = muzzle;
            MoveToSetIndexer = moveToSetIndexer;
            NumberOfMoves = MoveToSetIndexer.Length;
            Part = part;
            System = system;
            SubpartId = subpartId;
            MotionDelay = motionDelay;
            MainEnt = mainEnt;
            DoesLoop = loop;
            DoesReverse = reverse;
            TriggerOnce = triggerOnce;
            PlayTicks = new List<uint>() { 0 };
            _currentMove = 0;

            if (part != null)
            {                
                FinalPos = HomePos = part.PositionComp.LocalMatrixRef;
                var emissivePartCheck = new HashSet<string>();
                var emissiveParts = new List<string>();
                for (int i = 0; i < NumberOfMoves; i++)
                {
                    Matrix rotation;
                    Matrix rotAroundCenter;
                    Vector3D translation;
                    AnimationType animationType;
                    EmissiveState currentEmissive;
                    GetCurrentMove(out translation, out rotation, out rotAroundCenter, out animationType, out currentEmissive);

                    if (animationType == AnimationType.Movement)
                    {
                        HasMovement = true;
                        FinalPos.Translation += translation;
                    }

                    if (rotation != Matrix.Zero)
                    {
                        HasMovement = true;
                        FinalPos *= rotation;
                    }

                    if (rotAroundCenter != Matrix.Zero)
                    {
                        HasMovement = true;
                        FinalPos *= rotAroundCenter;
                    }

                    if (currentEmissive.EmissiveParts != null)
                    {
                        for (int j = 0; j < currentEmissive.EmissiveParts.Length; j++)
                        {
                            var currEmissive = currentEmissive.EmissiveParts[j];

                            if (emissivePartCheck.Contains(currEmissive)) continue;

                            emissivePartCheck.Add(currEmissive);
                            emissiveParts.Add(currEmissive);
                        }
                    }

                    Next();
                }
                EmissiveParts = emissiveParts.ToArray();
                Reset();

                foreach (var evnt in Enum.GetNames(typeof(EventTriggers)))
                {
                    EventTriggers trigger;
                    Enum.TryParse(evnt, out trigger);
                    EventIdLookup.Add(trigger, evnt + SubpartId);
                }

                if (System.PartType == WeaponDefinition.HardPointDef.HardwareDef.HardwareType.BlockWeapon || System.PartType == WeaponDefinition.HardPointDef.HardwareDef.HardwareType.HandWeapon)
                    CheckAffectPivot(part, out MovesPivotPos);
            }

        }

        internal PartAnimation(PartAnimation copyFromAnimation)
        {
            EventTrigger = copyFromAnimation.EventTrigger;
            RotationSet = copyFromAnimation.RotationSet;
            RotCenterSet = copyFromAnimation.RotCenterSet;
            CurrentEmissivePart = copyFromAnimation.CurrentEmissivePart;
            AnimationId = copyFromAnimation.AnimationId;
            ResetEmissives = copyFromAnimation.ResetEmissives;
            EmissiveIds = copyFromAnimation.EmissiveIds;

            //Unique Animation ID
            _uid = Guid.NewGuid();

            TypeSet = copyFromAnimation.TypeSet;
            Muzzle = copyFromAnimation.Muzzle;
            MoveToSetIndexer = copyFromAnimation.MoveToSetIndexer;
            NumberOfMoves = copyFromAnimation.NumberOfMoves;
            System = copyFromAnimation.System;
            SubpartId = copyFromAnimation.SubpartId;
            MotionDelay = copyFromAnimation.MotionDelay;
            DoesLoop = copyFromAnimation.DoesLoop;
            DoesReverse = copyFromAnimation.DoesReverse;
            TriggerOnce = copyFromAnimation.TriggerOnce;
            PlayTicks = new List<uint>() { 0 };
            _currentMove = 0;
            MovesPivotPos = copyFromAnimation.MovesPivotPos;
            FinalPos = copyFromAnimation.FinalPos;
            HomePos = copyFromAnimation.HomePos;
            HasMovement = copyFromAnimation.HasMovement;
            EmissiveParts = copyFromAnimation.EmissiveParts;
            EventIdLookup = copyFromAnimation.EventIdLookup;
        }

        internal void GetCurrentMove(out Vector3D translation, out Matrix rotation, out Matrix rotAroundCenter, out AnimationType type, out EmissiveState emissiveState)
        {
            type = TypeSet[MoveToSetIndexer[_currentMove][(int)Indexer.TypeIndex]];
            var moveSet = System.PartLinearMoveSet[AnimationId];

            if (type == AnimationType.Movement)
            {
                if (moveSet[MoveToSetIndexer[_currentMove][(int)Indexer.MoveIndex]] != Matrix.Zero)
                    translation = moveSet[MoveToSetIndexer[_currentMove][(int)Indexer.MoveIndex]].Translation;
                else
                    translation = Vector3D.Zero;

                rotation = RotationSet[MoveToSetIndexer[_currentMove][(int)Indexer.RotationIndex]];
                rotAroundCenter = RotCenterSet[MoveToSetIndexer[_currentMove][(int)Indexer.RotCenterIndex]];

            }
            else
            {
                translation = Vector3D.Zero;
                rotation = Matrix.Zero;
                rotAroundCenter = Matrix.Zero;
            }

            if (System.PartEmissiveSet.TryGetValue(EmissiveIds[MoveToSetIndexer[_currentMove][(int)Indexer.EmissiveIndex]], out emissiveState))
            {
                emissiveState.CurrentPart = CurrentEmissivePart[MoveToSetIndexer[_currentMove][(int)Indexer.EmissivePartIndex]];

                if (emissiveState.EmissiveParts != null && LastEmissive.EmissiveParts != null && emissiveState.CurrentPart == LastEmissive.CurrentPart && emissiveState.CurrentColor == LastEmissive.CurrentColor && Math.Abs(emissiveState.CurrentIntensity - LastEmissive.CurrentIntensity) < 0.001)
                    emissiveState = new EmissiveState();

                LastEmissive = emissiveState;

            }
            else
                emissiveState = LastEmissive = new EmissiveState();

        }

        internal int Next(bool inc = true)
        {
            if (inc)
            {
                _currentMove = _currentMove + 1 < NumberOfMoves ? _currentMove + 1 : 0;
                return _currentMove;
            }

            return _currentMove + 1 < NumberOfMoves ? _currentMove + 1 : 0;
        }

        internal int Previous(bool dec = true)
        {
            if (dec)
            {
                _currentMove = _currentMove - 1 >= 0 ? _currentMove - 1 : NumberOfMoves - 1;
                return _currentMove;
            }

            return _currentMove - 1 >= 0 ? _currentMove - 1 : NumberOfMoves - 1; 
        }

        internal void Reset(bool reverse = false, bool resetPos = true, bool resetMove = true)
        {
            Looping = false;
            Reverse = reverse;
            LastEmissive = new EmissiveState();

            if (resetMove) _currentMove = 0;
            if (resetPos) Part.PositionComp.LocalMatrix = HomePos;
            
        }

        private void CheckAffectPivot(MyEntity part, out bool movesPivotPos)
        {
            var head = -1;
            var tmp = new Dictionary<string, IMyModelDummy>();
            var subparts = new List<MyEntity>();
            movesPivotPos = false;

            while (head < subparts.Count)
            {
                var query = head == -1 ? part : subparts[head];
                head++;
                if (query.Model == null)
                    continue;
                tmp.Clear();
                ((IMyEntity)query).Model.GetDummies(tmp);
                foreach (var kv in tmp)
                {
                    if (kv.Key.StartsWith("subpart_", StringComparison.Ordinal))
                    {
                        if (kv.Key.Contains(((WeaponSystem)System).AzimuthPartName.String) || kv.Key.Contains(((WeaponSystem)System).ElevationPartName.String))
                            movesPivotPos = true;

                        var name = kv.Key.Substring("subpart_".Length);
                        MyEntitySubpart res;
                        if (query.TryGetSubpart(name, out res))
                            subparts.Add(res);
                    }
                }
            }
        }

        protected bool Equals(PartAnimation other)
        {
            return Equals(_uid, other._uid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PartAnimation)obj);
        }

        public override int GetHashCode()
        {
            return _uid.GetHashCode();
        }
    }
}

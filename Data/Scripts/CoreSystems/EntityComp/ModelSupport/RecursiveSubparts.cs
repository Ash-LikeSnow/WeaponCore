using System;
using System.Collections;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace CoreSystems.Support
{
    // Courtesy Equinox
    /// <summary>
    /// Maintains a list of all recursive subparts of the given entity.  Respects changes to the model.
    /// </summary>
    internal class RecursiveSubparts : IEnumerable<MyEntity>
    {
        private readonly List<MyEntity> _subparts = new List<MyEntity>();
        private readonly Dictionary<string, IMyModelDummy> _tmp1 = new Dictionary<string, IMyModelDummy>();
        private readonly Dictionary<string, IMyModelDummy> _tmp2 = new Dictionary<string, IMyModelDummy>();
        internal readonly Dictionary<string, MyEntity> NameToEntity = new Dictionary<string, MyEntity>();
        internal readonly Dictionary<MyEntity, string> EntityToName = new Dictionary<MyEntity, string>();
        internal readonly Dictionary<MyEntity, string> VanillaSubparts = new Dictionary<MyEntity, string>();
        internal readonly Dictionary<MyEntity, string> NeedsWorld = new Dictionary<MyEntity, string>();

        internal const string MissileVanillaBase = "MissileTurretBase1";
        internal const string MissileVanillaBarrels = "MissileTurretBarrels";
        internal const string GatVanillaBase = "GatlingTurretBase1";
        internal const string GatVanillaBarrels = "GatlingTurretBase2";
        internal const string NoneStr = "None";


        private IMyModel _trackedModel;
        internal MyEntity Entity;

        internal void Clean(MyEntity myEntity)
        {
            _subparts.Clear();
            _tmp1.Clear();
            NameToEntity.Clear();
            EntityToName.Clear();
            VanillaSubparts.Clear();
            NeedsWorld.Clear();
            _trackedModel = null;
            Entity = myEntity;
        }

        internal void CheckSubparts()
        {
            if (_trackedModel == Entity?.Model)
                return;
            _trackedModel = Entity?.Model;
            _subparts.Clear();
            NameToEntity.Clear();
            EntityToName.Clear();
            VanillaSubparts.Clear();
            NeedsWorld.Clear();
            if (Entity != null)
            {
                var head = -1;
                _tmp1.Clear();
                while (head < _subparts.Count)
                {
                    var query = head == -1 ? Entity : _subparts[head];
                    head++;
                    if (query.Model == null)
                        continue;
                    _tmp1.Clear();
                    ((IMyEntity)query).Model.GetDummies(_tmp1);
                    foreach (var kv in _tmp1)
                    {
                        if (kv.Key.StartsWith("subpart_", StringComparison.Ordinal))
                        {
                            var name = kv.Key.Substring("subpart_".Length);
                            MyEntitySubpart res;
                            if (query.TryGetSubpart(name, out res))
                            {
                                _subparts.Add(res);
                                NameToEntity[name] = res;
                                EntityToName[res] = name;
                                var sorter = Entity as MyConveyorSorter;
                                if (sorter == null && (name.Equals(MissileVanillaBase) || name.Equals(MissileVanillaBarrels) || name.Equals(GatVanillaBase) || name.Equals(GatVanillaBarrels)))
                                    VanillaSubparts[res] = name;
                            }
                        }
                        else NameToEntity[kv.Key] = Entity;
                    }
                }
                NameToEntity[NoneStr] = Entity;
                EntityToName[Entity] = NoneStr;
            }

            foreach (var ent in EntityToName)
            {
                if (!string.IsNullOrWhiteSpace(ent.Value) && !ent.Value.Equals(NoneStr) && VanillaSubparts.ContainsKey(ent.Key.Parent) && !VanillaSubparts.ContainsKey(ent.Key))
                    NeedsWorld[ent.Key] = ent.Value;
            }
        }

        internal bool FindFirstDummyByName(string name1, string name2, out MyEntity entity, out string matched)
        {
            entity = null;
            matched = string.Empty;
            var checkSecond = !string.IsNullOrEmpty(name2);
            foreach (var parts in NameToEntity) {
                
                ((IMyEntity)parts.Value)?.Model?.GetDummies(_tmp2);
                
                foreach (var pair in _tmp2) {

                    var firstCheck = pair.Key == name1;
                    var secondCheck = checkSecond && pair.Key == name2;
                    if (firstCheck || secondCheck) {
                        
                        matched = firstCheck ? name1 : name2;
                        
                        entity = parts.Value;
                        break;
                    }
                }
                _tmp2.Clear();
                
                if (entity != null)
                    break;
            }

            return entity != null && !string.IsNullOrEmpty(matched);
        }
        
        IEnumerator<MyEntity> IEnumerable<MyEntity>.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal List<MyEntity>.Enumerator GetEnumerator()
        {
            CheckSubparts();
            return _subparts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Sets the emissive value of a specific emissive material on entity, and all recursive subparts.
        /// </summary>
        /// <param name="emissiveName">The name of the emissive material (ie. "Emissive0")</param>
        /// <param name="emissivity">Level of emissivity (0 is off, 1 is full brightness)</param>
        /// <param name="emissivePartColor">Color to emit</param>
        internal void SetEmissiveParts(string emissiveName, Color emissivePartColor, float emissivity)
        {
            Entity.SetEmissiveParts(emissiveName, emissivePartColor, emissivity);
            SetEmissivePartsForSubparts(emissiveName, emissivePartColor, emissivity);
        }

        /// <summary>
        /// Sets the emissive value of a specific emissive material on all recursive subparts.
        /// </summary>
        /// <param name="emissiveName">The name of the emissive material (ie. "Emissive0")</param>
        /// <param name="emissivity">Level of emissivity (0 is off, 1 is full brightness).</param>
        /// <param name="emissivePartColor">Color to emit</param>
        internal void SetEmissivePartsForSubparts(string emissiveName, Color emissivePartColor, float emissivity)
        {
            foreach (var k in this)
                k.SetEmissiveParts(emissiveName, emissivePartColor, emissivity);
        }
    }
}

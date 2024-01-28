using CoreSystems.Platform;
using CoreSystems.Support;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Comms
{
    internal class RadioStation
    {
        internal readonly Radios Radios = new Radios();
        private readonly Dictionary<MyStringHash, HashSet<RadioStation>> _detectedStationsOnChannel = new Dictionary<MyStringHash, HashSet<RadioStation>>(MyStringHash.Comparer);
        private readonly Dictionary<MyStringHash, List<RadioStation>> _stationAdds = new Dictionary<MyStringHash, List<RadioStation>>(MyStringHash.Comparer);
        private readonly Dictionary<MyStringHash, List<RadioStation>> _stationRemoves = new Dictionary<MyStringHash, List<RadioStation>>(MyStringHash.Comparer);

        private readonly List<MyStringHash> _listening = new List<MyStringHash>();
        private readonly List<MyStringHash> _broadasting = new List<MyStringHash>();
        private readonly Ai.Constructs _station;
        private readonly Spectrum _spectrum;
        private Vector3D _lastUpdatePosition;
        internal int PruningProxyId = -1;

        internal RadioStation(Ai.Constructs rootConstruct, Spectrum spectrum)
        {
            _station = rootConstruct;
            _spectrum = spectrum;
        }

        internal void RegisterAndUpdateVolume()
        {
            var topEntity = _station.Ai.TopEntity;
            var center = topEntity.PositionComp.WorldAABB.Center;
            var sizeSqr = topEntity.PositionComp.LocalVolume.Radius * topEntity.PositionComp.LocalVolume.Radius;
            var vel = topEntity.Physics.LinearVelocity;
            foreach (var channel in Radios.RadioMap.Keys)
            {
                var freq = _spectrum.Channels[channel];
                var volume = new BoundingSphereD(center, Radios.MaxInfluenceRange);
                if (PruningProxyId == -1)
                    freq.Tree.RegisterSignal(this, ref volume);
                else if (Vector3D.DistanceSquared(_lastUpdatePosition, center) > sizeSqr)
                {
                    _lastUpdatePosition = center;
                    freq.Tree.OnSignalMoved(this, ref vel, ref volume);
                }
            }
        }

        private readonly List<RadioStation> _queryList = new List<RadioStation>();
        private readonly HashSet<RadioStation> _discardSet = new HashSet<RadioStation>();

        private void DetectStationsInRange()
        {
            var center = _station.Ai.TopEntity.PositionComp.WorldAABB.Center;
            var volume = new BoundingSphereD(center, Radios.MaxInfluenceRange);

            foreach (var channel in _listening)
            {
                Frequency freq;
                if (_spectrum.Channels.TryGetValue(channel, out freq))
                {
                    var detectedOnChannel = _detectedStationsOnChannel[channel];
                    freq.Tree.DividedSpace.OverlapAllBoundingSphere(ref volume, _queryList, false);
                    var adds = _stationAdds[channel];
                    var removes = _stationRemoves[channel];

                    for (int i = _queryList.Count - 1; i >= 0; i--)
                    {
                        var station = _queryList[i];

                        if (detectedOnChannel.Contains(station)) 
                            continue;

                        adds.Add(station);
                        _queryList.RemoveAtFast(i);
                    }

                    for (int i = 0; i < _queryList.Count; i++)
                        _discardSet.Add(_queryList[i]);

                    foreach (var station in detectedOnChannel) {
                        if (!_discardSet.Contains(station))
                            removes.Add(station);
                    }

                    foreach (var r in removes)
                        detectedOnChannel.Remove(r);

                    foreach (var r in adds)
                        detectedOnChannel.Add(r);

                    _queryList.Clear();
                }
            }

            UpdateChannelMatrix();
        }

        private void UpdateChannelMatrix()
        {

        }

        internal void UnRegisterAll()
        {
            foreach (var map in Radios.RadioMap)
            {
                var freq = _spectrum.Channels[map.Key];
                if (PruningProxyId != -1)
                    freq.Tree.UnregisterSignal(this);
            }
        }

        internal void UnRegisterAll(MyStringHash id)
        {
            var freq = _spectrum.Channels[id];
            if (PruningProxyId != -1)
                freq.Tree.UnregisterSignal(this);
        }

        internal void Clean()
        {

        }
    }

    public class Radios
    {
        internal readonly Dictionary<int, Radio> RadioTypeMap = new Dictionary<int, Radio>();
        internal readonly Dictionary<MyStringHash, List<Radio>> RadioMap = new Dictionary<MyStringHash, List<Radio>>(MyStringHash.Comparer);

        internal double FurthestTransmiter;
        internal double FurthestReceiver;
        internal double FurthestJammer;
        internal double MaxInfluenceRange;

        internal void UpdateLocalInfluenceBounds()
        {
            FurthestTransmiter = 0;
            FurthestReceiver = 0;
            FurthestJammer = 0;
            MaxInfluenceRange = 0;
            foreach (var pair in RadioTypeMap)
            {
                var type = (Radio.RadioTypes)pair.Key;
                var radio = pair.Value;
                switch (type)
                {
                    case Radio.RadioTypes.Transmitter:
                        if (radio.TransmitRange > FurthestTransmiter)
                            FurthestTransmiter = radio.TransmitRange;

                        if (radio.TransmitRange > MaxInfluenceRange)
                            MaxInfluenceRange = radio.TransmitRange;
                        break;
                    case Radio.RadioTypes.Receiver:
                        if (radio.ReceiveRange > FurthestReceiver)
                            FurthestReceiver = radio.ReceiveRange;

                        if (radio.ReceiveRange > MaxInfluenceRange)
                            MaxInfluenceRange = radio.ReceiveRange;
                        break;
                    case Radio.RadioTypes.Jammer:
                        if (radio.JamRange > FurthestJammer)
                            FurthestJammer = radio.JamRange;

                        if (radio.JamRange > MaxInfluenceRange)
                            MaxInfluenceRange = radio.JamRange;
                        break;
                }
            }
        }
        internal void Clean()
        {

        }
    }

    public class Radio
    {
        private Weapon _weapon;
        internal Radio(Weapon w)
        {
            _weapon = w;
            Type = w.System.RadioType;
        }

        public enum RadioTypes
        {
            None,
            Slave,
            Master,
            Transmitter,
            Repeater,
            Receiver,
            Jammer,
            Relay
        }

        internal RadioTypes Type;
        internal double TransmitRange;
        internal double ReceiveRange;
        internal double JamRange;

        internal void Clean()
        {

        }
    }
}

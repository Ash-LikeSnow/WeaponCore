using System;
using System.Collections.Generic;
using CoreSystems;
using VRage.Utils;
using WeaponCore.Data.Scripts.CoreSystems.Support;

namespace WeaponCore.Data.Scripts.CoreSystems.Comms
{
    internal class Spectrum
    {
        internal Dictionary<MyStringHash, Frequency> Channels = new Dictionary<MyStringHash, Frequency>(MyStringHash.Comparer);
        internal Stack<Radios> RadiosPool = new Stack<Radios>(32);
        internal Stack<Radio> RadioPool = new Stack<Radio>(32);
        internal Stack<RadioStation> RadioStationPool = new Stack<RadioStation>(32);
    }

    internal class Frequency
    {
        public enum LicensedFor
        {
            BroadCasters,
            Relayers,
            Both,
        }
        internal readonly Dictionary<LicensedFor, List<RadioStation>> Nodes = new Dictionary<LicensedFor, List<RadioStation>>();
        internal readonly SpaceTrees Tree = new SpaceTrees();
        internal readonly LicensedFor Rights;
        internal readonly MyStringHash HashId;
        internal readonly LicensedFor[] Licenses;
        internal readonly string Id;
        internal bool Dirty;

        public Frequency(LicensedFor rights, MyStringHash hashId)
        {
            Rights = rights;
            HashId = hashId;
            Id = hashId.String;
            Licenses = new LicensedFor[Enum.GetNames(typeof(LicensedFor)).Length];

            for (int i = 0; i < Licenses.Length; i++)
            {
                var license = (LicensedFor) i;
                Licenses[i] = license;
                Nodes[license] = new List<RadioStation>();
            }
        }

        public bool TryAddOrUpdateSource(RadioStation station)
        {
            switch (Rights)
            {
                case LicensedFor.Both:
                    break;
                case LicensedFor.BroadCasters:
                    break;
                case LicensedFor.Relayers:
                    break;
            }

            return false;
        }

        public bool TryRemoveSource(RadioStation station)
        {
            switch (Rights)
            {
                case LicensedFor.Both:
                    break;
                case LicensedFor.BroadCasters:
                    break;
                case LicensedFor.Relayers:
                    break;
            }

            return false;
        }
    }

}

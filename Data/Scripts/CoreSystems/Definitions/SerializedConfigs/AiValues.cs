using System.Collections.Generic;
using ProtoBuf;

namespace CoreSystems
{
    [ProtoContract]
    public class AiDataValues
    {
        //[ProtoMember(1)] public uint Revision;
        //[ProtoMember(2)] public int Version = Session.VersionControl;
        [ProtoMember(3)] public long ActiveTerminal;

        public bool Sync(AiDataValues sync)
        {
            ActiveTerminal = sync.ActiveTerminal;

            return true;
        }
    }

}

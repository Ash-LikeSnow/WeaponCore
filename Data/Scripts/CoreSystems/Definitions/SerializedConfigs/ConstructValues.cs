using CoreSystems.Support;
using ProtoBuf;
using static CoreSystems.Support.Ai;
namespace CoreSystems
{
    [ProtoContract]
    public class ConstructDataValues
    {
        [ProtoMember(2)] public FocusData FocusData;

        public bool Sync(Constructs construct, ConstructDataValues sync, bool localCall = false)
        {
            FocusData.Sync(construct.RootAi, sync.FocusData, localCall);
            return true;
        }
    }

    [ProtoContract]
    public class FocusData
    {
        public enum LockModes
        {
            None,
            Locked,
        }

        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public long Target;
        [ProtoMember(4)] public bool HasFocus;
        [ProtoMember(5)] public float DistToNearestFocusSqr;
        [ProtoMember(6)] public LockModes Locked;


        public bool Sync(Ai ai, FocusData sync, bool localCall = false)
        {
            if (Session.I.IsServer || sync.Revision > Revision)
            {
                Revision = sync.Revision;
                HasFocus = sync.HasFocus;
                DistToNearestFocusSqr = sync.DistToNearestFocusSqr;

                Target = sync.Target;
                Locked = sync.Locked;

                if (ai == ai.Construct.RootAi && localCall)
                    ai.Construct.UpdateLeafFoci();

                return true;
            }
            return false;
        }
    }
}

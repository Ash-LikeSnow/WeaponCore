using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreSystems.Support;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
namespace CoreSystems.Platform
{
    public partial class SupportSys : Part
    {

        internal readonly HashSet<IMySlimBlock> SuppotedBlocks = new HashSet<IMySlimBlock>();
        internal readonly Dictionary<IMySlimBlock, BlockBackup> BlockColorBackup = new Dictionary<IMySlimBlock, BlockBackup>();
        internal readonly SupportInfo Info = new SupportInfo();
        internal readonly SupportComponent Comp;
        internal readonly SupportSystem System;
        internal readonly MyStringHash PartHash;

        private readonly HashSet<IMySlimBlock> _updatedBlocks = new HashSet<IMySlimBlock>();
        private readonly HashSet<IMySlimBlock> _newBlocks = new HashSet<IMySlimBlock>();
        private readonly HashSet<IMySlimBlock> _lostBlocks = new HashSet<IMySlimBlock>();
        private readonly HashSet<IMySlimBlock> _agedBlocks = new HashSet<IMySlimBlock>();
        private readonly ConcurrentDictionary<IMySlimBlock, SupportSys> _activeSupports;
        private int _charges;

        
        internal uint LastBlockRefreshTick;
        internal bool ShowAffectedBlocks;
        internal bool Active;
        internal Vector3I Min;
        internal Vector3I Max;
        internal BoundingBox Box = BoundingBox.CreateInvalid();
        internal ProtoSupportPartState PartState;

        internal SupportSys(SupportSystem system, SupportComponent comp, int partId)
        {
            System = system;
            Comp = comp;

            Init(comp, system, partId);
            PartHash = Comp.Structure.PartHashes[partId];

            _activeSupports = GetSupportCollection();

            if (!BaseComp.Ai.BlockMonitoring)
                BaseComp.Ai.DelayedEventRegistration(true);
        }
    }
}

using System;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRageMath;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;

namespace CoreSystems.Platform
{
    public partial class ControlSys 
    {
        internal class ControlCompData : CompData
        {
            internal readonly ControlComponent Comp;
            internal ProtoControlRepo Repo;

            internal ControlCompData(ControlComponent comp)
            {
                Init(comp);
                Comp = comp;
            }

            internal void Load()
            {
                if (Comp.CoreEntity.Storage == null) return;

                ProtoControlRepo load = null;
                string rawData;
                bool validData = false;
                if (Comp.CoreEntity.Storage.TryGetValue(Session.I.CompDataGuid, out rawData))
                {
                    try
                    {
                        var base64 = Convert.FromBase64String(rawData);
                        load = MyAPIGateway.Utilities.SerializeFromBinary<ProtoControlRepo>(base64);
                        validData = load?.Values.Other != null;
                    }
                    catch (Exception e)
                    {
                        Log.Line("Invalid PartState Loaded, Re-init");
                    }
                }

                if (validData && load.Version == Session.VersionControl)
                {
                    Repo = load;
                    var p = Comp.Platform.Control;
                    p.PartState = Repo.Values.State.Control;
                }
                else
                {
                    Repo = new ProtoControlRepo
                    {
                        Values = new ProtoControlComp
                        {
                            State = new ProtoControlState { Control = new ProtoControlPartState() },
                            Set = new ProtoWeaponSettings(),
                            Other = new ProtoControlOtherSettings(),
                        },
                    };

                    var state = Repo.Values.State.Control = new ProtoControlPartState();
                    var p = Comp.Platform.Control;

                    if (p != null)
                    {
                        p.PartState = state;
                    }

                    Repo.Values.Set.Range = -1;
                }
                ProtoRepoBase = Repo;
            }

            internal void Change(DataState state)
            {
                switch (state)
                {
                    case DataState.Load:
                        Load();
                        break;
                    case DataState.Reset:
                        Repo.ResetToFreshLoadState(Comp);
                        break;
                }
            }
        }
        internal bool ValidFakeTargetInfo(long playerId, out Ai.FakeTarget.FakeWorldTargetInfo fakeTargetInfo, bool preferPainted = true)
        {
            fakeTargetInfo = null;
            Ai.FakeTargets fakeTargets;
            if (Session.I.PlayerDummyTargets.TryGetValue(playerId, out fakeTargets))
            {
                var validManual = Comp.Data.Repo.Values.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Manual && Comp.Data.Repo.Values.State.TrackingReticle && fakeTargets.ManualTarget.FakeInfo.WorldPosition != Vector3D.Zero;
                var validPainter = Comp.Data.Repo.Values.Set.Overrides.Control == ProtoWeaponOverrides.ControlModes.Painter && fakeTargets.PaintedTarget.LocalPosition != Vector3D.Zero;
                var fakeTarget = validPainter && preferPainted ? fakeTargets.PaintedTarget : validManual ? fakeTargets.ManualTarget : null;
                if (fakeTarget == null)
                    return false;

                fakeTargetInfo = fakeTarget.LastInfoTick != Session.I.Tick ? fakeTarget.GetFakeTargetInfo(Comp.Ai) : fakeTarget.FakeInfo;
            }

            return fakeTargetInfo != null;
        }
    }
}

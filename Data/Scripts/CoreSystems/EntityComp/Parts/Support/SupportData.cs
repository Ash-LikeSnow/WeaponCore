using System;
using CoreSystems.Support;
using Sandbox.ModAPI;

namespace CoreSystems.Platform
{
    public partial class SupportSys 
    {
        internal class SupportCompData : CompData
        {
            internal readonly SupportComponent Comp;
            internal ProtoSupportRepo Repo;

            internal SupportCompData(SupportComponent comp)
            {
                Init(comp);
                Comp = comp;
            }

            internal void Load()
            {
                if (Comp.CoreEntity.Storage == null) return;

                ProtoSupportRepo load = null;
                string rawData;
                bool validData = false;
                if (Comp.CoreEntity.Storage.TryGetValue(Session.I.CompDataGuid, out rawData))
                {
                    try
                    {
                        var base64 = Convert.FromBase64String(rawData);
                        load = MyAPIGateway.Utilities.SerializeFromBinary<ProtoSupportRepo>(base64);
                        validData = load != null;
                    }
                    catch (Exception e)
                    {
                        //Log.Line("Invalid PartState Loaded, Re-init");
                    }
                }

                if (validData && load.Version == Session.VersionControl)
                {
                    Log.Line("loading something");
                    Repo = load;

                    for (int i = 0; i < Comp.Platform.Support.Count; i++)
                    {
                        var p = Comp.Platform.Support[i];

                        p.PartState = Repo.Values.State.Support[i];
                    }
                }
                else
                {
                    Log.Line("creating something");
                    Repo = new ProtoSupportRepo
                    {
                        Values = new ProtoSupportComp
                        {
                            State = new ProtoSupportState { Support = new ProtoSupportPartState[Comp.Platform.Support.Count] },
                            Set = new ProtoSupportSettings(),
                        },
                    };

                    for (int i = 0; i < Comp.Platform.Support.Count; i++)
                    {
                        var state = Repo.Values.State.Support[i] = new ProtoSupportPartState();
                        var p = Comp.Platform.Support[i];

                        if (p != null)
                        {
                            p.PartState = state;
                        }
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
                        Repo.ResetToFreshLoadState();
                        break;
                }
            }
        }
    }
}

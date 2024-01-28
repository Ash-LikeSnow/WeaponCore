using System;
using Sandbox.ModAPI;

namespace CoreSystems.Platform
{
    public partial class Upgrade
    {
        internal class UpgradeCompData : CompData
        {
            internal readonly UpgradeComponent Comp;
            internal ProtoUpgradeRepo Repo;

            internal UpgradeCompData(UpgradeComponent comp)
            {
                Init(comp);
                Comp = comp;
            }

            internal void Load()
            {
                if (Comp.CoreEntity.Storage == null) return;

                ProtoUpgradeRepo load = null;
                string rawData;
                bool validData = false;
                if (Comp.CoreEntity.Storage.TryGetValue(Session.I.CompDataGuid, out rawData))
                {
                    try
                    {
                        var base64 = Convert.FromBase64String(rawData);
                        load = MyAPIGateway.Utilities.SerializeFromBinary<ProtoUpgradeRepo>(base64);
                        validData = load != null;
                    }
                    catch (Exception e)
                    {
                        //Log.Line("Invalid PartState Loaded, Re-init");
                    }
                }

                if (validData && load.Version == Session.VersionControl)
                {
                    Repo = load;

                    for (int i = 0; i < Comp.Platform.Upgrades.Count; i++)
                    {
                        var p = Comp.Platform.Upgrades[i];

                        p.PartState = Repo.Values.State.Upgrades[i];
                    }
                }
                else
                {
                    Repo = new ProtoUpgradeRepo
                    {
                        Values = new ProtoUpgradeComp
                        {
                            State = new ProtoUpgradeState { Upgrades = new ProtoUpgradePartState[Comp.Platform.Support.Count] },
                            Set = new ProtoUpgradeSettings(),
                        },
                    };

                    for (int i = 0; i < Comp.Platform.Support.Count; i++)
                    {
                        var state = Repo.Values.State.Upgrades[i] = new ProtoUpgradePartState();
                        var p = Comp.Platform.Upgrades[i];

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

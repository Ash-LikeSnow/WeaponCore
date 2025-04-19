using CoreSystems.Platform;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;

namespace CoreSystems.Support
{
    public partial class CoreComponent
    {
        internal void HealthCheck()
        {
            if (Platform.State != CorePlatform.PlatformState.Ready || TopEntity.MarkedForClose)
                return;

            switch (Status)
            {
                case Start.Starting:
                    Startup();
                    break;
                case Start.ReInit:

                    if (Type == CompType.Weapon) 
                        Platform.ResetParts();

                    Ai.Construct.RootAi.Construct.DirtyWeaponGroups = true;
                    Status = NeedsWorldReset ? Start.ReInit : Start.Started;
                    NeedsWorldReset = false;
                    break;
            }

            if (Session.I.HandlesInput)
                Session.I.LeadGroupsDirty = true;
        }

        private void Startup()
        {
            IsWorking = !IsBlock || Cube.IsWorking;

            if (IsBlock && FunctionalBlock.Enabled) {
                FunctionalBlock.Enabled = false;
                FunctionalBlock.Enabled = true;
                LastOnOffState = true;
            }

            Status = Start.ReInit;
        }

        internal void WakeupComp()
        {
            if (IsAsleep) {
                IsAsleep = false;
                Ai.AwakeComps += 1;
                Ai.SleepingComps -= 1;
            }
        }


        internal void SubpartClosed(MyEntity ent)
        {
            if (ent == null)
            {
                Log.Line($"SubpartClosed had null entity");
                return;
            }

            using (CoreEntity.Pin())
            {
                ent.OnClose -= SubpartClosed;
                if (!CoreEntity.MarkedForClose && Platform.State == CorePlatform.PlatformState.Ready)
                {
                    if (Type == CompType.Weapon)
                        Platform.ResetParts();
                    Status = Start.Started;

                    foreach (var w in Platform.Weapons)
                    {
                        w.Azimuth = 0;
                        w.Elevation = 0;
                        w.Elevation = 0;

                        if (w.ActiveAmmoDef.AmmoDef.Const.MustCharge)
                            w.ExitCharger = true;

                        if (!FunctionalBlock.Enabled)
                            w.EventTriggerStateChanged(EventTriggers.TurnOff, true);
                        else if (w.AnimationsSet.ContainsKey(EventTriggers.TurnOn))
                            Session.I.FutureEvents.Schedule(w.TurnOnAV, null, 100);

                        if (w.ProtoWeaponAmmo.CurrentAmmo == 0)
                        {
                            w.EventTriggerStateChanged(EventTriggers.EmptyOnGameLoad, true);
                        }
                    }
                }
            }
        }

        internal void ForceClose(object o)
        {
            var subtypeId = o as string;
            if (TypeSpecific != CompTypeSpecific.Phantom) Log.Line($"closing: {subtypeId} - critical:{CloseCondition}");
            CloseCondition = true;
            MyEntities.SendCloseRequest(CoreEntity);
        }
    }
}

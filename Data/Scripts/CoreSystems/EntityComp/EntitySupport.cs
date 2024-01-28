using System;
using System.Collections.ObjectModel;
using CoreSystems.Platform;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Weapons;
using VRage.Game.Entity;
using VRageMath;
using static CoreSystems.Support.Ai;

namespace CoreSystems.Support
{
    public partial class CoreComponent 
    {
        internal MyEntity GetTopEntity()
        {
            var cube = CoreEntity as MyCubeBlock;

            if (cube != null)
                return cube.CubeGrid;

            var gun = CoreEntity as IMyAutomaticRifleGun;
            return gun != null ? ((Weapon.WeaponComponent)this).Rifle.Owner : CoreEntity;
        }

        internal void TerminalRefresh(bool update = true)
        {
            if (Platform.State != CorePlatform.PlatformState.Ready || Status != Start.Started)
                return;

            if (Ai?.LastTerminal == CoreEntity)  {

                TerminalBlock.RefreshCustomInfo();

                if (update && InControlPanel)
                {
                    Cube.UpdateTerminal();
                }
            }
        }

        internal bool ValidDummies()
        {
            var weaponComp = this as Weapon.WeaponComponent;
            if (weaponComp == null) return true;

            foreach (var w in Platform.Weapons)
            {
                for (int i = 0; i < w.Dummies.Length; i++)
                {
                    if (w.Dummies[i].NullEntity)
                    {
                        var forceResetRequest = w.Dummies[i].Entity;
                        return false;
                    }
                }
            }

            return true;
        }

        internal void RemoveFromReInit()
        {
            InReInit = false;
            Session.I.CompsDelayedReInit.Remove(this);
        }

        internal void RemoveComp()
        {

            if (InReInit) {
                RemoveFromReInit();
                return;
            }

            if (Registered) 
                RegisterEvents(false);

            if (Ai != null) {

                try
                {
                    if (Type == CompType.Weapon)
                    {
                        var wComp = ((Weapon.WeaponComponent) this);
                        Ai.OptimalDps -= wComp.PeakDps;
                        Ai.EffectiveDps -= wComp.EffectiveDps;
                        Ai.PerfectDps -= wComp.PerfectDps;


                        if (TypeSpecific == CompTypeSpecific.Rifle)
                        {
                            Session.I.OnPlayerControl(CoreEntity, null);
                            wComp.AmmoStorage();
                        }

                        Constructs.WeaponGroupsMarkDirty(Ai.TopEntityMap?.GroupMap);
                        wComp.MasterOverrides = null;
                    }

                    PartCounter wCount;
                    if (Ai.PartCounting.TryGetValue(SubTypeId, out wCount))
                    {
                        wCount.Current--;

                        if (IsBlock)
                            Constructs.BuildAiListAndCounters(Ai);

                        if (wCount.Current == 0)
                        {
                            Ai.PartCounting.Remove(SubTypeId);
                            Session.I.PartCountPool.Return(wCount);
                        }
                    }
                    else if (Session.I.LocalVersion) Log.Line($"didnt find counter for: {SubTypeId} - MarkedForClose:{Ai.MarkedForClose} - AiAge:{Session.I.Tick - Ai.AiSpawnTick} - CubeMarked:{CoreEntity.MarkedForClose} - GridMarked:{TopEntity.MarkedForClose}");

                    if (Ai.Data.Repo.ActiveTerminal == CoreEntity.EntityId)
                        Ai.Data.Repo.ActiveTerminal = 0;

                    if (Ai.CompBase.Remove(CoreEntity))
                    {
                        if (Platform.State == CorePlatform.PlatformState.Ready)
                        {

                            var collection = TypeSpecific != CompTypeSpecific.Phantom ? Platform.Weapons : Platform.Phantoms;

                            for (int i = 0; i < collection.Count; i++)
                            {
                                var w = collection[i];
                                w.StopShooting();
                                w.TurretActive = false;
                                if (!Session.I.IsClient) w.Target.Reset(Session.I.Tick, Target.States.AiLost);

                                if (w.InCharger)
                                    w.ExitCharger = true;
                                if (w.CriticalReaction && w.Comp.Slim.IsDestroyed)                                  
                                    w.CriticalOnDestruction();
                            }
                        }
                        Ai.CompChange(false, this);
                    }

                    if (Ai.CompBase.Count == 0 && TypeSpecific != CompTypeSpecific.Rifle)
                    {
                        if (Ai.TopEntity != null)
                        {
                            Ai ai;
                            Session.I.EntityAIs.TryRemove(Ai.TopEntity, out ai);
                        }
                        else 
                            Log.Line($"Ai.TopEntity was Null - marked:{Ai.MarkedForClose} - closed:{Ai.Closed}");
                    }

                    if (Session.I.TerminalMon.Comp == this)
                        Session.I.TerminalMon.Clean(true);

                    Ai = null;
                    MasterAi = null;
                }
                catch (Exception ex) { Log.Line($"Exception in RemoveComp Inner: {ex} - Name:{Platform?.Comp?.SubtypeName} - AiNull:{Ai == null}  - CoreEntNull:{CoreEntity == null} - PlatformNull: {Platform == null} - AiTopNull:{Ai?.TopEntity == null} - TopEntityNull:{TopEntity == null}", null, true); }

            }
            else if (Platform.State != CorePlatform.PlatformState.Delay && TypeSpecific != CompTypeSpecific.Rifle) Log.Line($"CompRemove: Ai already null - PartState:{Platform.State} - Status:{Status} - LastRemoveFromScene:{Session.I.Tick - LastRemoveFromScene}");

            LastRemoveFromScene = Session.I.Tick;
        }


        internal void ReCalculateMaxTargetingRange(double maxRange)
        {
            var expandedMaxTrajectory2 = maxRange + Ai.TopEntity.PositionComp.LocalVolume.Radius;
            if (expandedMaxTrajectory2 > Ai.MaxTargetingRange)
            {

                Ai.MaxTargetingRange = MathHelperD.Min(expandedMaxTrajectory2, Session.I.Settings.Enforcement.MaxHudFocusDistance);
                Ai.MaxTargetingRangeSqr = Ai.MaxTargetingRange * Ai.MaxTargetingRange;
            }
        }
    }
}

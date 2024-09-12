using System;
using CoreSystems.Support;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using static Sandbox.Definitions.MyDefinitionManager;

namespace CoreSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation, int.MaxValue - 1)]
    public partial class Session : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            if (!SuppressWc)
                BeforeStartInit();
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            ResetVisualAreas();
            return base.GetObjectBuilder();
        }


        public override void UpdatingStopped()
        {
            ResetVisualAreas();
            if (!SuppressWc)
                Paused();
        }

        public override void UpdateBeforeSimulation()
        {

            if (SuppressWc)
                return;

            if (SApi.Compromised)
                SuppressWc = true;

            if (!DelayedHandWeaponsSpawn.IsEmpty)
                InitDelayedHandWeapons();

            if (DeformProtection.Count > 0 && Tick - LastDeform > 0)
                DeformProtection.Clear();

            Timings();

            if (IsClient) {

                if (Tick - ClientDestroyBlockTick == 30) {
                    _slimHealthClient.Clear();
                    _destroyedSlimsClient.Clear();
                }

                if (ClientSideErrorPkt.Count > 0)
                    ReproccessClientErrorPackets();

                if (ClientPacketsToClean.Count > 0)
                    CleanClientPackets();
            }

            if (IsServer) {
                if (Tick60) AcqManager.Observer();
                if (Tick600) AcqManager.ReorderSleep();
                if (Tick10 && PlayersToAdd.Count > 0) CheckPlayersToAdd();
            }

            if (!DedicatedServer && TerminalMon.Active)
                TerminalMon.Monitor();

            MyCubeBlock cube;
            if (Tick60 && UiInput.ControlKeyPressed && UiInput.CtrlPressed && GetAimedAtBlock(out cube) && cube.BlockDefinition != null && CoreSystemsDefs.ContainsKey(cube.BlockDefinition.Id.SubtypeName))
                ProblemRep.GenerateReport(cube);

            if (!IsClient && !InventoryUpdate && PartToPullConsumable.Count > 0 && ITask.IsComplete)
                StartAmmoTask();

            if (!CompsToStart.IsEmpty)
                StartComps();

            if (GridGroupUpdates.Count > 0)
                GroupUpdates();

            if ((Tick120 || ReInitTick > 0 && Tick - ReInitTick < 10) && (CompsDelayedReInit.Count > 0 || CompsDelayedInit.Count > 0))
            {
                InitDelayedComps();
            }

            if (Tick10 && !DelayedAiClean.IsEmpty) {
                InitDelayedComps();
                DelayedAiCleanup();
            }

            if (CompReAdds.Count > 0) {
                InitDelayedComps();
                if (!DelayedAiClean.IsEmpty) DelayedAiCleanup();
                ChangeReAdds();
            }

            if (Tick3600 && MpActive) 
                NetReport();

            if (Tick180) 
                ProfilePerformance();

            if (HandlesInput)
            {
                Av.AvShotCleanUp();
                if (Tick90)
                    ClientMonitor();
            }

            FutureEvents.Tick(Tick);

            if (HomingWeapons.Count > 0)
                UpdateHomingWeapons();

            if (MpActive)
            {
                if (IsServer && Tick30 && LastProSyncSendTick > 0 && Tick - LastProSyncSendTick < 7200)
                    PingPong(Session.GameplayFrameCounter);

                if (PacketsToClient.Count > 0 || PrunedPacketsToClient.Count > 0)
                    ProccessServerPacketsForClients();
                if (PacketsToServer.Count > 0)
                    ProccessClientPacketsForServer();
            }
        }

        public override void Simulate()
        {
            if (SuppressWc)
                return;

            ++SimulationCount;

            if (!DedicatedServer) {
                EntityControlUpdate();
                CameraMatrix = Session.Camera.WorldMatrix;
                CameraPos = CameraMatrix.Translation;
                UpdateLocalCharacterInfo();

                if (Tick120 && DisplayAffectedArmor.Count > 0)
                    ColorAreas();
            }

            if (GameLoaded) {

                DsUtil.Start("ai");

                if (AimingAi.Count > 0) 
                    AimAi();

                AiLoop();
                DsUtil.Complete("ai", true);


                DsUtil.Start("charge");
                if (ChargingParts.Count > 0) UpdateChargeWeapons();
                DsUtil.Complete("charge", true);

                DsUtil.Start("acquire");
                if (AcquireTargets.Count > 0) CheckAcquire();
                DsUtil.Complete("acquire", true);

                DsUtil.Start("shoot");
                if (ShootingWeapons.Count > 0) ShootWeapons();
                DsUtil.Complete("shoot", true);
            }

            if (!DedicatedServer && !InMenu) {
                UpdateLocalAiAndCockpit();
                if ((UiInput.PlayerCamera && (ActiveCockPit != null || TrackingAi != null && (TrackingAi.SmartHandheld || TrackingAi.AiType == Ai.AiTypes.Player && LeadGroupActive)) || ActiveControlBlock is MyRemoteControl && !UiInput.PlayerCamera || UiInput.CameraBlockView) && PlayerDummyTargets.ContainsKey(PlayerId))
                    TargetUi.TargetSelection();
            }

            DsUtil.Start("ps");
            Projectiles.SpawnAndMove();
            DsUtil.Complete("ps", true);

            DsUtil.Start("pi");
            Projectiles.Intersect();
            DsUtil.Complete("pi", true);

            DsUtil.Start("pd");
            Projectiles.Damage();
            DsUtil.Complete("pd", true);

            DsUtil.Start("pa");
            Projectiles.AvUpdate();
            DsUtil.Complete("pa", true);

            DsUtil.Start("av");
            if (!DedicatedServer) Av.End();
            DsUtil.Complete("av", true);

            if (AdvSyncServer && ProtoDeathSyncMonitor.Collection.Count > 0)
                ProcessDeathSyncsForClients();

            if (MpActive)  {
                
                DsUtil.Start("network1");

                if (GlobalProPosSyncs.Count > 0)
                    SendProjectilePosSyncs();

                if (GlobalProTargetSyncs.Count > 0)
                    SendProjectileTargetSyncs();

                if (PacketsToClient.Count > 0 || PrunedPacketsToClient.Count > 0) 
                    ProccessServerPacketsForClients();
                if (PacketsToServer.Count > 0) 
                    ProccessClientPacketsForServer();
                if (EwarNetDataDirty)
                    SendEwaredBlocks();
                DsUtil.Complete("network1", true);
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (SuppressWc)
                return;

            if (Placer != null) UpdatePlacer();

            if (AnimationsToProcess.Count > 0) 
                ProcessAnimations();

            if (GridTask.IsComplete)
                CheckDirtyGridInfos();

            if (!DirtyPowerGrids.IsEmpty)
                UpdateGridPowerState();

            if (WaterApiLoaded && (Tick3600 || WaterMap.IsEmpty))
                UpdateWaters();

            if (HandlesInput && Tick30)
                UpdatePlayerPainters();

            if (DebugLos && Tick1800) {
                var averageMisses = RayMissAmounts > 0 ? RayMissAmounts / Rays : 0; 
                Log.Line($"RayMissAverage: {averageMisses} - tick:{Tick}");
            }
        }

        public override void Draw()
        {
            if (SuppressWc || DedicatedServer || _lastDrawTick == Tick || _paused) return;


            if (DebugLos || DebugMod)
                VisualDebuging();

            _lastDrawTick = Tick;
            DsUtil.Start("draw");
            CameraMatrix = Session.Camera.WorldMatrix;
            CameraPos = CameraMatrix.Translation;
            CameraFrustrum.Matrix = (Camera.ViewMatrix * Camera.ProjectionMatrix);
            var newFov = Camera.FovWithZoom;

            if (!MyUtils.IsEqual(newFov, CurrentFovWithZoom))
                FovChanged();

            CurrentFovWithZoom = newFov;
            AspectRatio = Camera.ViewportSize.X / Camera.ViewportSize.Y;
            AspectRatioInv = Camera.ViewportSize.Y / Camera.ViewportSize.X;

            ScaleFov = Math.Tan(CurrentFovWithZoom * 0.5);

            if (!Session.Config.MinimalHud && InGridAiBlock) {

                if (!Settings.Enforcement.DisableHudReload && (HudUi.TexturesToAdd > 0 || HudUi.KeepBackground)) 
                    HudUi.DrawTextures();

                if ((UiInput.PlayerCamera || UiInput.FirstPersonView || UiInput.CameraBlockView) && !InMenu && !MyAPIGateway.Gui.IsCursorVisible && PlayerDummyTargets.ContainsKey(PlayerId))
                    TargetUi.DrawTargetUi();

                if (HudUi.AgingTextures)
                    HudUi.DrawText();
            }

            Av.Run();
            Av.Draw();
            DrawDisabledGuns();
            DsUtil.Complete("draw", true);
        }

        public override void HandleInput()  
        {
            if (HandlesInput && !SuppressWc) {

                if (ControlRequest != ControlQuery.None)
                    UpdateControlKeys();

                UiInput.UpdateInputState();
                if (MpActive)  {

                    Ai.FakeTargets fakeTargets;
                    if (TrackingAi != null && PlayerDummyTargets.TryGetValue(PlayerId, out fakeTargets)) {

                        if (fakeTargets.ManualTarget.LastUpdateTick == Tick && Tick - TargetUi.LastManualTick <= 1)
                            SendAimTargetUpdate(TrackingAi, fakeTargets.ManualTarget);

                        if (fakeTargets.PaintedTarget.LastUpdateTick == Tick)
                            SendPaintedTargetUpdate(TrackingAi, fakeTargets.PaintedTarget);
                    }

                    if (PacketsToServer.Count > 0)
                        ProccessClientPacketsForServer();
                }

                if (Tick60 && SoundsToClean.Count > 0)
                    CleanSounds();
            }
        }

        public override void LoadData()
        {
            AllDefinitions = Static.GetAllDefinitions();
            foreach (var t in AllDefinitions)
            {
                var searchLight = t as MySearchlightDefinition;
                var turretBase = t as MyLargeTurretBaseDefinition;
                if (turretBase != null)
                    turretBase.MaxRangeMeters = 0.1f;

                if (searchLight != null)
                    searchLight.MaxRangeMeters = 0.1f;
            }

            ModChecker();

            if (SuppressWc)
                return;

            ApiServer.Load();

            IsServer = MyAPIGateway.Multiplayer.IsServer;
            DedicatedServer = MyAPIGateway.Utilities.IsDedicated;

            SoundDefinitions = Static.GetSoundDefinitions();
            MyEntities.OnEntityCreate += OnEntityCreate;
            MyEntities.OnCloseAll += OnCloseAll;

            MyAPIGateway.Gui.GuiControlCreated += MenuOpened;
            MyAPIGateway.Gui.GuiControlRemoved += MenuClosed;

            MyAPIGateway.Utilities.RegisterMessageHandler(7771, Handler);
            MyAPIGateway.Utilities.SendModMessage(7772, null);

            TriggerEntityModel = ModContext.ModPath + "\\Models\\Environment\\JumpNullField.mwm";
            TriggerEntityPool = new MyConcurrentPool<MyEntity>(0, TriggerEntityClear, 10000, TriggerEntityActivator);
        }

        protected override void UnloadData()
        {
            if (SuppressWc && !Inited)
                return;

            if (!PTask.IsComplete)
                PTask.Wait();

            if (!CTask.IsComplete)
                CTask.Wait();

            if (!ITask.IsComplete)
                ITask.Wait();

            if (IsServer || DedicatedServer)
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ServerPacketId, ProccessServerPacket);
            else
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ClientPacketId, ClientReceivedPacket);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(StringPacketId, StringReceived);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ClientPdPacketId, ClientReceivedDeathPacket);
            }

            if (HandlesInput)
                MyAPIGateway.Utilities.MessageEntered -= ChatMessageSet;

            MyAPIGateway.Utilities.UnregisterMessageHandler(7771, Handler);

            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlHandler;
            MyAPIGateway.TerminalControls.CustomActionGetter -= CustomActionHandler;

            MyEntities.OnEntityCreate -= OnEntityCreate;
            MyEntities.OnCloseAll -= OnCloseAll;


            MyAPIGateway.Gui.GuiControlCreated -= MenuOpened;
            MyAPIGateway.Gui.GuiControlRemoved -= MenuClosed;

            MyVisualScriptLogicProvider.PlayerDisconnected -= PlayerDisconnected;
            MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            MyVisualScriptLogicProvider.PlayerRespawnRequest -= PlayerConnected;

            foreach (var pair in DmgLog)
            {
                var x = pair.Value;
                var total = x.Primary + x.AOE + x.Shield + x.Projectile;
                if (total>0)Log.Stats($"{x.TerminalName}, {x.WepCount}, {total}, {x.Primary}, {x.AOE}, {x.Shield}, {x.Projectile}", "dmgstats");
            }
            ApiServer.Unload();

            PurgeAll();

            Log.Line("Logging stopped.");
            Log.Close();
            I = null;
        }
    }
}


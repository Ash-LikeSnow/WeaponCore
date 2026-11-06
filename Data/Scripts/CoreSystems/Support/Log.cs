using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Utils;

namespace CoreSystems.Support
{
    public static class Log
    {
        public const string PerfLog = "perf";
        public const string StatsLog = "stats";
        public const string NetLog = "net";
        public const string ReportLog = "report";
        public const string CombatLog = "combat";
        public const string AmmoStatsLog = "ammostats";
        public const string OverridesLog = "overrides";
        public const string WepStatsLog = "wepstats";
        public const string DmgStatsLog = "dmgstats";
        public const string GridDmgStatsLog = "griddmgstats";
        public const string ShootLog = "shoot";
        public const string ShootGateLog = "shootgate";
        public const string ReloadSyncLog = "reloadsync";
        public const string TargetSyncLog = "targetsync";
        public const string CycleSyncLog = "cyclesync";
        public const string InputLog = "input";
        public const string DebugLog = "debug";
        public const string CustomLog = "custom";

        private static MyConcurrentPool<LogInstance> _logPool = new MyConcurrentPool<LogInstance>(128);
        private static ConcurrentDictionary<string, LogInstance> _instances = new ConcurrentDictionary<string, LogInstance>();
        private static ConcurrentQueue<string[]> _threadedLineQueue = new ConcurrentQueue<string[]>();
        private static string _defaultInstance;

        public class LogInstance
        {
            internal TextWriter TextWriter;
            internal Session Session;
            internal uint CheckTick;
            internal uint StartTick;
            internal uint Messages;
            internal int LastExceptionCount;
            internal int Exceptions;
            internal bool Suppress;
            internal bool ExceptionReported;

            internal bool Paused()
            {
                if (!ExceptionReported && Session.HandlesInput && Exceptions != LastExceptionCount)
                {
                    ExceptionReported = true;
                    Session.ShowLocalNotify("WeaponCore is crashing, please report your issue/logs to the WeaponCore discord", 10000);
                }
                if (Session.Tick < 3600)
                    return false;

                var checkInTime = Session.Tick - CheckTick > 119;
                var threshold = 180;

                if (!Session.DebugMod && (!Suppress && checkInTime && Messages > threshold || !Suppress && Messages > threshold * 3))
                    return Pause();

                if (Suppress && StartTick >= Session.Tick)
                    return true;

                if (Suppress && Exceptions > 0 && Exceptions != LastExceptionCount) {

                    LastExceptionCount = Exceptions;
                    StartTick = Session.Tick + 7200;
                    return true;
                }

                ++Messages;

                if (Suppress)
                    UnPause();
                else if (checkInTime) {
                    CheckTick = Session.Tick;
                    Messages = 0;
                }

                return false;
            }

            internal bool Pause()
            {
                Suppress = true;
                StartTick = Session.Tick + 7200;
                LastExceptionCount = Exceptions;
                var message = $"{DateTime.Now:HH-mm-ss-fff} - " + "Debug flooding detected, supressing logs for two minutes.  Please report the first 500 lines of this file";
                TextWriter.WriteLine(message);
                TextWriter.Flush();
                return true;
            }

            internal void UnPause()
            {
                Suppress = false;
                Messages = 0;
                CheckTick = Session.Tick;
                ExceptionReported = false;
                LastExceptionCount = Exceptions;
            }

            internal void Clean()
            {
                CheckTick = Session.Tick;
                StartTick = CheckTick;
                TextWriter = null;
                Session = null;
                Suppress = false;
                Messages = 0;
            }
        }

        public static void Init(string name, Session session, bool defaultInstance = true)
        {
            try
            {
                var filename = name + ".log";
                if (_instances.ContainsKey(name)) return;
                RenameFileInLocalStorageLimited(filename, name + $"_1.log", typeof(LogInstance));

                if (defaultInstance) _defaultInstance = name;
                var instance = _logPool.Get();

                instance.Session = session;
                _instances[name] = instance;

                instance.TextWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage(filename, typeof(LogInstance));
                if (name == WepStatsLog)
                {
                    Stats("Name\tMaxDist\tMinDist\tDevShotAngle\tAimTolerance\tAimLeadingPrediction\tRotateRate\tElevateRate\tIdlePower\tRateOfFire\tReloadTime\tHeatPerShot\tMaxHeat\tHeatSinkRate\tShotsInBurst\tDelayAfterBurst\tAmmoName", name);
                }
                else if (name == AmmoStatsLog)
                {
                    Stats("Name\tBaseDamage\tBaseDamageType\tAreaDamageType\tDetDamageType\tShieldDamageType\tLargeGridModifier\tSmallGridModifier\tArmorModifier\tLightArmorModifier\tHeavyArmorModifier\tNonArmorModifier\tShieldsModifier\tShieldBypass\t" +
                        "FragmentName\tFragmentQuanty\tFragmentDegrees\tBBHRadius\tBBHDamage\tBBHDepth\tBBHMaxAbsorb\tBBHFalloff\tEOLRadius\tEOLDamage\tEOLDepth\tEOLMaxAbsorb\tEOLFalloff\tAccelPerSec\tMaxSpeed\tMaxTrajectory\tMaxLifeTime", name);
                }
                else if (name == DmgStatsLog)
                {
                    Stats("WeaponName\tQuantity\tTotalDamage\tPrimaryDamage\tAOEDamage\tShieldDamage\tProjectileDamage", name);
                }
                else if (name == GridDmgStatsLog)
                {
                    Stats("GridName\tMainOwner\tTotalDamage\tPrimaryDamage\tAOEDamage\tShieldDamage\tProjectileDamage", name);
                }
                else if (name == ShootLog)
                {
                    Stats("Time\tEntity Id\tBarrel Rotation\tNot Spun\tNot Ready\tShooting\tRelative Time\tShoot Time", name);
                }
                else if (name == ShootGateLog)
                {
                    Stats("ShootGate - EntityId,PartId,Tick,Shoot,canShoot,shootRequest,aiCanShoot,requiresTarget,hasTarget,finish,overRide,noFireTarget,sig,reloadingGuard,overHeat,needsHeat,sequenceReady,sMode,ShootCount,AiShooting,Trigger,Freeze,WaitResp,ammo,makeup,loading,waitClnt,waitingSrv,relStart,cliStart,relEnd,cliEnd | quickSkip: EntityId,PartId,Tick,quickSkip,invalid,losBlocked,pause,maxSmarts,noLoadedAmmo,ammo,makeup,loading,waitClnt,waitingSrv | shoot-block(client): EntityId,PartId,Tick,shoot-block,canShoot,anyShot,autoShot,aiShooting,aiCanShoot,hasTarget,state,target,trigger,sMode,shootCount,onConf,noShootDelay,finish,freeze,waitResp,sig,blk,tickSinceChange | shot-opp(client,Tick20): EntityId,PartId,Tick,shot-opp,hasTarget,state,objNull,objMFC,centerZero,lock,aimed,rotorDist,maxDet,aiShooting,anyShot,autoShot,shootRequest,shootCount,canShoot,canB(1=overHeat,2=reloadingGuard,4=designator,8=!seqReady,16=needsHeat),loading,noAmmo,waitClnt,waitingSrv,makeup,ammo,finishShots,heat,ohCd,sig,target,packetEnt,tickSinceChange,validEst,resetSub,manual,painter,inRge,rt(readyToTrack approx),ae(AimAi 934 skip mask),cam(Control==Camera),dotT(barrel·clientPred),dotC(barrel·trueCenter),dotPkt(barrel·packet/serverIntercept),pktV(packet pos valid),pktMatch(packet ent==target),leadT(pred·center dot),az,el,minAz,maxAz,minEl,maxEl,lookAtFail | target-opp(client,Tick20): EntityId,PartId,Tick,target-opp,logs when shot-opp sample gate (hasTarget+IsEntity) is FALSE,hasTarget,state,tsc(tickSinceChange),objNull,target | shot-opp(server,Tick20): side:srv ...,lock=TargetLock,rt/ae/cam same,sDotT,sDotC,sLead,az,el,minAz,maxAz,minEl,maxEl,lookAtFail", name);
                }
                else if (name == ReloadSyncLog)
                {
                    Stats("ReloadSync - EntityId,PartId,Tick,event | events: wait-set,reload-out-of-seq,reload-sync,ammo-out-of-seq,ammo-sync,client-reload-start,over-fire | fields: packetSeq,lastSeq,clearWait,ammo,makeup,loading,waitingSrv,relStart,cliStart,cliEnd,relEnd,runAmmoToMakeUp,burstStop,syncStep,cliLastShot,srv,cli", name);
                }
                else if (name == TargetSyncLog)
                {
                    Stats("TargetSync - EntityId,PartId,Tick,event | events: server-push,client-target-sync,target-apply,client-reset | fields: target,state,posId,packetEnt,applied,hasTarget,syncId,curState,delayReset,reason,cond,projState | reset reasons: ServerReset(MarkedForClose/TargetObjectNull/DelayedClear),Expired,NoTargetsSeen", name);
                }
                else if (name == CycleSyncLog)
                {
                    Stats("CycleSync - EntityId,PartId,Tick,event | events: cycle-end,end-mode,arm,decline,freeze-set | fields: endAction,completed,lastCycle,weaponsFired,toggled,overCount,shootCount,burst,reloading,skipReload,isShooting,canShoot,alreadyShooting,ammo,makeup,burstDelay,reason,freeze,waitResp,Trigger,Count,CliToggle,sig,toggleCnt,cliToggle", name);
                }
                else
                {
                    Line("Logging Started", name);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.Error($"Exception: WC failed to initialize log {name} \n {e.Message}");
            }
        }

        public static void RenameFileInLocalStorageLimited(string oldName, string newName, Type anyObjectInYourMod)
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(oldName, anyObjectInYourMod))
                return;

            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(newName, anyObjectInYourMod))
                MyAPIGateway.Utilities.DeleteFileInLocalStorage(newName, anyObjectInYourMod);

            using (var read = MyAPIGateway.Utilities.ReadFileInLocalStorage(oldName, anyObjectInYourMod))
            {
                var sb = new StringBuilder(newName);
                SUtils.ReplaceAll(sb, Path.GetInvalidFileNameChars(), '_');

                using (var write = MyAPIGateway.Utilities.WriteFileInLocalStorage(sb.ToString(), anyObjectInYourMod))
                {
                    int n = 0;
                    while (read.Peek() != -1)
                    {
                        write.Write(Convert.ToChar(read.Read()));
                        n++;

                        if (n > short.MaxValue)
                        {
                            write.Flush();
                            n = 0;
                        }
                    }
                    write.Flush();
                    write.Dispose();
                }
            }

            MyAPIGateway.Utilities.DeleteFileInLocalStorage(oldName, anyObjectInYourMod);
        }

        public static void RenameFileInLocalStorage(string oldName, string newName, Type anyObjectInYourMod)
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(oldName, anyObjectInYourMod))
                return;

            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(newName, anyObjectInYourMod))
                return;

            using (var read = MyAPIGateway.Utilities.ReadFileInLocalStorage(oldName, anyObjectInYourMod))
            {
                var sb = new StringBuilder(newName);
                SUtils.ReplaceAll(sb, Path.GetInvalidFileNameChars(), '_');

                using (var write = MyAPIGateway.Utilities.WriteFileInLocalStorage(sb.ToString(), anyObjectInYourMod))
                {
                    write.Write(read.ReadToEnd());
                    write.Flush();
                    write.Dispose();
                }
            }

            MyAPIGateway.Utilities.DeleteFileInLocalStorage(oldName, anyObjectInYourMod);
        }

        public static void NetLogger(Session session, string message, string name, ulong directedSteamId = ulong.MaxValue)
        {
            switch (name) {
                case PerfLog:
                    message = "1" + message;
                    break;
                case StatsLog:
                    message = "2" + message;
                    break;
                case NetLog:
                    message = "3" + message;
                    break;
                case CustomLog:
                    message = "4" + message;
                    break;
                default:
                    message = "0" + message;
                    break;
            }

            var encodedString = Encoding.UTF8.GetBytes(message);

            if (directedSteamId == ulong.MaxValue) {
                foreach (var a in session.ConnectedAuthors)
                    MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(Session.StringPacketId, encodedString, a.Value, true);
            }
            else MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(Session.StringPacketId, encodedString, directedSteamId, true);
        }

        public static void Line(string text, string instanceName = null, bool exception = false, bool tab = false)
        {
            try
            {
                var name  = instanceName ?? _defaultInstance;
                var instance = _instances[name];
                if (instance.TextWriter != null) {

                    if (name == _defaultInstance && !instance.Session.LocalVersion && instance.Paused())
                        return;

                    var message = $"{DateTime.Now:MM-dd-yy_HH-mm-ss-fff}{(tab ? "\t" : " - ")}" + text;
                    instance.TextWriter.WriteLine(message);
                    instance.TextWriter.Flush();
                    var set = instance.Session.AuthorSettings;
                    var netEnabled = instance.Session.AuthLogging && name == _defaultInstance && set[0] >= 0 || name == PerfLog && set[1] >= 0 || name == StatsLog && set[2] >= 0 || name == NetLog && set[3] >= 0;
                    if (netEnabled)
                        NetLogger(instance.Session, "[R-LOG] " + text, name);
                }
            }
            catch (Exception e)
            {
            }
        }
        public static void Stats(string text, string instanceName = null, bool exception = false)
        {
            try
            {
                var name = instanceName ?? _defaultInstance;
                var instance = _instances[name];
                if (instance.TextWriter != null)
                {

                    if (name == _defaultInstance && !instance.Session.LocalVersion && instance.Paused())
                        return;

                    var message = text;
                    instance.TextWriter.WriteLine(message);
                    instance.TextWriter.Flush();
                    var set = instance.Session.AuthorSettings;
                }
            }
            catch (Exception e)
            {
            }
        }

        public static void LineShortDate(string text, string instanceName = null)
        {
            try
            {
                var name = instanceName ?? _defaultInstance;
                var instance = _instances[name];
                if (instance.TextWriter != null) {

                    if (name == _defaultInstance && !instance.Session.LocalVersion && instance.Paused())
                        return;

                    var message = $"{DateTime.Now:HH-mm-ss-fff} - " + text;
                    instance.TextWriter.WriteLine(message);
                    instance.TextWriter.Flush();

                    var set = instance.Session.AuthorSettings;
                    var netEnabled = instance.Session.AuthLogging && name == _defaultInstance && set[0] >= 0 || name == PerfLog && set[1] >= 0 || name == StatsLog && set[2] >= 0 || name == NetLog && set[3] >= 0;
                    if (netEnabled)
                        NetLogger(instance.Session, "[R-LOG] " + text, name);
                }
            }
            catch (Exception e)
            {
            }
        }

        public static void CleanLine(string text, string instanceName = null)
        {
            try
            {
                var name = instanceName ?? _defaultInstance;
                var instance = _instances[name];
                if (instance.TextWriter != null) {

                    if (name == _defaultInstance && !instance.Session.LocalVersion && instance.Paused())
                        return;

                    instance.TextWriter.WriteLine(text);
                    instance.TextWriter.Flush();

                    var set = instance.Session.AuthorSettings;
                    var netEnabled = instance.Session.AuthLogging && name == _defaultInstance && set[0] >= 0 || name == PerfLog && set[1] >= 0 || name == StatsLog && set[2] >= 0 || name == NetLog && set[3] >= 0;
                    if (netEnabled)
                        NetLogger(instance.Session, "[R-LOG] " + text, name);
                }
            }
            catch (Exception e)
            {
            }
        }

        public static void Close()
        {
            try
            {
                _threadedLineQueue.Clear();
                foreach (var pair in _instances)
                {
                    pair.Value.TextWriter.Flush();
                    pair.Value.TextWriter.Close();
                    pair.Value.TextWriter.Dispose();
                    pair.Value.Clean();

                    _logPool.Return(pair.Value);

                }
                _instances.Clear();
                _logPool.Clean();
                _logPool = null;
                _instances = null;
                _threadedLineQueue = null;
                _defaultInstance = null;
            }
            catch (Exception e)
            {
            }
        }
    }
}

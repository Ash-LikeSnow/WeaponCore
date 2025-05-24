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
                if (name == "wepstats")
                {
                    Stats("Name, MaxDist, MinDist, DevShotAngle, AimTolerance, AimLeadingPrediction, RotateRate, ElevateRate, IdlePower, RateOfFire, ReloadTime, HeatPerShot, MaxHeat, HeatSinkRate, ShotsInBurst, DelayAfterBurst, AmmoName", name);
                }
                else if (name == "ammostats")
                {
                    Stats("Name, BaseDamage, BaseDamageType, AreaDamageType, DetDamageType, ShieldDamageType, LargeGridModifier, SmallGridModifier, ArmorModifier, LightArmorModifier, HeavyArmorModifier, NonArmorModifier, ShieldsModifier, ShieldBypass, " +
                        "FragmentName, FragmentQuanty, FragmentDegrees, BBHRadius, BBHDamage, BBHDepth, BBHMaxAbsorb, BBHFalloff, EOLRadius, EOLDamage, EOLDepth, EOLMaxAbsorb, EOLFalloff, AccelPerSec, MaxSpeed, MaxTrajectory, MaxLifeTime", name);
                }
                else if (name == "dmgstats")
                {
                    Stats("WeaponName, Quantity, TotalDamage, PrimaryDamage, AOEDamage, ShieldDamage, ProjectileDamage", name);
                }
                else if (name == "griddmgstats")
                {
                    Stats("GridName, MainOwner, TotalDamage, PrimaryDamage, AOEDamage, ShieldDamage, ProjectileDamage", name);
                }
                else
                {
                    Line("Logging Started", name);
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification(e.Message, 5000);
                MyLog.Default.Error($"WC failed to initialize log {name} \n {e.Message}");
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
                    write.Write(read.ReadToEnd());
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
                case "perf":
                    message = "1" + message;
                    break;
                case "stats":
                    message = "2" + message;
                    break;
                case "net":
                    message = "3" + message;
                    break;
                case "custom":
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

        public static void Line(string text, string instanceName = null, bool exception = false)
        {
            try
            {
                var name  = instanceName ?? _defaultInstance;
                var instance = _instances[name];
                if (instance.TextWriter != null) {

                    if (name == _defaultInstance && !instance.Session.LocalVersion && instance.Paused())
                        return;

                    var message = $"{DateTime.Now:MM-dd-yy_HH-mm-ss-fff} - " + text;
                    instance.TextWriter.WriteLine(message);
                    instance.TextWriter.Flush();
                    var set = instance.Session.AuthorSettings;
                    var netEnabled = instance.Session.AuthLogging && name == _defaultInstance && set[0] >= 0 || name == "perf" && set[1] >= 0 || name == "stats" && set[2] >= 0 || name == "net" && set[3] >= 0;
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
                    var netEnabled = instance.Session.AuthLogging && name == _defaultInstance && set[0] >= 0 || name == "perf" && set[1] >= 0 || name == "stats" && set[2] >= 0 || name == "net" && set[3] >= 0;
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
                    var netEnabled = instance.Session.AuthLogging && name == _defaultInstance && set[0] >= 0 || name == "perf" && set[1] >= 0 || name == "stats" && set[2] >= 0 || name == "net" && set[3] >= 0;
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

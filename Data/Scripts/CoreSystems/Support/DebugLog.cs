using CoreSystems;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRage.Utils;

namespace WeaponCore.Data.Scripts.CoreSystems.Support
{
    public static class DebugLog
    {
        public const bool ForceDebug = true;
        
        public static void LogWithSeverity(MyLogSeverity severity, string message)
        {
            message = $"WC DebugLog {severity}: {message}";
            
            Log.Line(message);

            if (severity == MyLogSeverity.Error || severity == MyLogSeverity.Critical)
            {
                if (ForceDebug)
                {
                    if (Session.I.IsClient)
                    {
                        if (ForceDebug)
                        {
                            MyAPIGateway.Utilities.ShowMessage($"WC {severity}", message);
                        }
                    }
                    else
                    {
                        MyLog.Default.Log(severity, message);
                    }   
                }
            }
            else if (ForceDebug)
            {
                if (Session.I.IsClient)
                {
                    MyAPIGateway.Utilities.ShowMessage($"WC {severity}", message);
                }
                else
                {
                    MyLog.Default.Log(severity, message);
                }   
            }
        }

        public static void Debug(string message) => LogWithSeverity(MyLogSeverity.Debug, message);
        public static void Info(string message) => LogWithSeverity(MyLogSeverity.Info, message);
        public static void Warning(string message) => LogWithSeverity(MyLogSeverity.Warning, message);
        public static void Error(string message) => LogWithSeverity(MyLogSeverity.Error, message);
        public static void Critical(string message) => LogWithSeverity(MyLogSeverity.Critical, message);
    }
}
using System;
using System.Diagnostics;

namespace DbSyncService.Utilities
{
    public static class Logging
    {
        public const string LogSource = "MySqlDataMigService";
        public static bool DebugMode { get; set; }

        public static void WriteToApplicationLog(this Exception exception)
        {
            try
            {
                EventLog.WriteEntry("MySqlDataMigService", exception.ToString(), EventLogEntryType.Error);
                if (DebugMode)
                {
                    Console.WriteLine(exception.ToString());
                }
            }
            catch
            {
            }
        }

        public static void WriteMessageToApplicationLog(string message, EventLogEntryType eventLogEntryType)
        {
            try
            {
                EventLog.WriteEntry("MySqlDataMigService", message, eventLogEntryType);
                if (DebugMode)
                {
                    Console.WriteLine("{0} : {1}", eventLogEntryType.ToString(), message);
                }
            }
            catch
            {
            }
        }
    }
}

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
            EventLog.WriteEntry("MySqlDataMigService", exception.ToString(), EventLogEntryType.Error);
            if (DebugMode)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        public static void WriteMessageToApplicationLog(string message, EventLogEntryType eventLogEntryType)
        {
            EventLog.WriteEntry("MySqlDataMigService", message, eventLogEntryType);
            if (DebugMode)
            {
                Console.WriteLine("{0} : {1}", eventLogEntryType.ToString(), message);
            }
        }
    }
}

using System;
using System.Configuration;
using System.Diagnostics;

namespace DbSyncService.Utilities
{
    public static class PerformanceCounters
    {
        private const string categoryName = "DbSync";
        private static bool EnablePerformanceCounters;
        private static PerformanceCounter insertCounter;
        private static PerformanceCounter updateCounter;
        private static PerformanceCounter deleteCounter;
        private static PerformanceCounter errorCounter;

        public static bool DebugMode { get; set; }

        static PerformanceCounters()
        {
            try
            {
                DebugMode = false;
                EnablePerformanceCounters = bool.Parse(ConfigurationManager.AppSettings["enablePerformanceCounters"]);
                if (EnablePerformanceCounters)
                {

                    insertCounter = new PerformanceCounter(categoryName, "RowsInserted", false);
                    updateCounter = new PerformanceCounter(categoryName, "RowsUpdated", false);
                    deleteCounter = new PerformanceCounter(categoryName, "RowsDeleted", false);
                    errorCounter = new PerformanceCounter(categoryName, "RowsErrored", false);
                }
            }
            catch (Exception exception)
            {
                exception.WriteToApplicationLog();
                EnablePerformanceCounters = false;
            }
        }

        public static void AddRowsInserted(long numRows)
        {
            if (numRows > 0)
            {
                if (EnablePerformanceCounters)
                {
                    insertCounter.IncrementBy(numRows);
                }
                if (DebugMode)
                {
                    Console.WriteLine(" {0} rows inserted.", numRows);
                }
            }
        }

        public static void AddRowsUpdated(long numRows)
        {
            if (numRows > 0)
            {
                if (EnablePerformanceCounters)
                {
                    updateCounter.IncrementBy(numRows);
                }
                if (DebugMode)
                {
                    Console.WriteLine(" {0} rows updated.", numRows);
                }
            }
        }

        public static void AddRowsDeleted(long numRows)
        {
            if (numRows > 0)
            {
                if (EnablePerformanceCounters)
                {
                    deleteCounter.IncrementBy(numRows);
                }
                if (DebugMode)
                {
                    Console.WriteLine(" {0} rows deleted.", numRows);
                }
            }
        }

        public static void AddRowsErrored(long numRows)
        {
            if (numRows > 0)
            {
                if (EnablePerformanceCounters)
                {
                    errorCounter.IncrementBy(numRows);
                }
                if (DebugMode)
                {
                    Console.WriteLine(" {0} rows errored.", numRows);
                }
            }
        }
    }
}

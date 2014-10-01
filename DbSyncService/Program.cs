using System;
using System.Collections.Generic;
using System.ServiceProcess;
using DbSyncService.Models;
using DbSyncService.Report;
using DbSyncService.SyncProvider;
using DbSyncService.Utilities;

namespace DbSyncService
{
    static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].ToLower().Contains("report"))
            {
                var tableSetData = new TableSetData();
                var tableSets = new List<TableSet>();
                tableSets = tableSetData.LoadTableSets();
                foreach (var tableSet in tableSets)
                {
                    var statusReport = new SyncStatusReport(tableSet);
                    string report = statusReport.GenerateStatusReport();
                    statusReport.SendEmail(report);
                }
            }
            else if (args.Length > 0 && args[0].ToLower().Contains("debug"))
            {
                using (var manager = new SyncManager())
                {
                    manager.DebugMode = true;
                    PerformanceCounters.DebugMode = true;
                    Logging.DebugMode = true;

                    manager.RunTableSets();
                    manager.Start();

                    Console.WriteLine("Hit enter to stop...");
                    Console.ReadLine();
                }
            }
            else
            {
                ServiceBase.Run(new SyncService());
            }
        }
    }
}

using System;
using System.ServiceProcess;
using DbSyncService.SyncProvider;
using DbSyncService.Utilities;

namespace DbSyncService
{
    static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].ToLower() == "debug")
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

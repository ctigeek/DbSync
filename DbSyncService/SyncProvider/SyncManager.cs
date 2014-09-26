using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using DbSyncService.Models;
using DbSyncService.Utilities;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace DbSyncService.SyncProvider
{
    public class SyncManager : IDisposable
    {
        private static object lockObject = new object();
        private readonly Timer timer;

        public bool DebugMode { get; set; }

        public SyncManager()
        {
            DebugMode = false;
            var period = Int32.Parse(ConfigurationManager.AppSettings["intervalInSeconds"]);
            timer = new Timer(period * 1000);
            timer.Elapsed += timer_Elapsed;
        }

        public void Start()
        {
            timer.Start();
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                timer.Stop();
            }
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RunTableSets();
        }

        public void RunTableSets()
        {
            
            if (Monitor.TryEnter(lockObject))
            {
                try
                {
                    if (DebugMode)
                    {
                        Console.Write("{0} Syncing.... ", DateTime.Now.ToLongTimeString());
                    }
                    var tableSets = loadTableSets();
                    DoBulkLoad(tableSets);

                    foreach (var tableSet in tableSets)
                    {
                        if (tableSet.Enabled)
                        {
                            var migtool = new DbSyncProvider(tableSet);
                            migtool.ProcessSyncChanges();
                        }
                    }
                    if (DebugMode)
                    {
                        Console.WriteLine("     ...done.");
                    }
                }
                catch (Exception ex)
                {
                    ex.WriteToApplicationLog();
                }
                Monitor.Exit(lockObject);
            }
        }

        private void DoBulkLoad(List<TableSet> tableSets)
        {
            if (tableSets.Any(ts => ts.Enabled && ts.Mappings.Any(tm => tm.TruncateDestinationAndBulkLoadFromSource)))
            {
                try
                {
                    ResetTruncateSettingToFalse();
                    foreach (var tableset in tableSets.Where(ts => ts.Enabled && ts.Mappings.Any(tm => tm.TruncateDestinationAndBulkLoadFromSource)))
                    {
                        var bulkLoader = new BulkLoader(tableset);
                        bulkLoader.TruncateDestinationTables();
                        bulkLoader.BulkLoadDestinationTables();
                    }
                }
                catch (Exception exception)
                {
                    exception.WriteToApplicationLog();
                }
            }
        }

        private List<TableSet> loadTableSets()
        {
            var configFile = ConfigurationManager.AppSettings["tableSetConfigFile"];
            using (var reader = new StreamReader(configFile))
            {
                var jsonData = reader.ReadToEnd();
                reader.Close();
                var tableSets = JsonConvert.DeserializeObject<List<TableSet>>(jsonData);
                return tableSets;
            }
        }

        private void ResetTruncateSettingToFalse()
        {
            var tableSets = loadTableSets();
            foreach (var tableSet in tableSets)
            {
                foreach (var tableMap in tableSet.Mappings)
                {
                    tableMap.TruncateDestinationAndBulkLoadFromSource = false;
                }
            }
            var configFile = ConfigurationManager.AppSettings["tableSetConfigFile"];
            var serializeObject = JsonConvert.SerializeObject(tableSets, Formatting.Indented);
            using (var writer = new StreamWriter(configFile, false))
            {
                writer.WriteLine(serializeObject);
                writer.Flush();
                writer.Close();
            }
        }
    }
}

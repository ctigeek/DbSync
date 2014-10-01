using System.Collections.Generic;
using System.Linq;
using DbSyncService.Models;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;

namespace DbSyncService.SyncProvider
{
    public class TableSetData
    {
        public List<TableSet> LoadTableSets()
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

        public void ResetTruncateSettingToFalse()
        {
            var tableSets = LoadTableSets();
            foreach (var tableMap in tableSets.SelectMany(tableSet => tableSet.Mappings))
            {
                tableMap.TruncateDestinationAndBulkLoadFromSource = false;
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

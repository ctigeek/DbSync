using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DbSyncService.Models
{
    public class TableSet
    {
        public TableSet()
        {
            Mappings = new List<TableMapping>();
        }

        public string Name { get; set; }
        public string SourceConnectionStringName { get; set; }
        public string DestinationConnectionStringName { get; set; }
        public bool Enabled { get; set; }
        public string SyncTableSchema { get; set; }
        public string SyncTableName { get; set; }
        public List<TableMapping> Mappings { get; set; }

        [JsonIgnore]
        public string FullyQualifiedSyncTableName
        {
            get { return string.IsNullOrEmpty(SyncTableSchema) ? SyncTableName : SyncTableSchema + "." + SyncTableName; }
        }

        public TableMapping GetTableMapping(SyncChange change)
        {
            var tableMap = Mappings.FirstOrDefault(m => m.SourceTable == change.TableName && m.SourceSchema == change.SchemaName);
            if (tableMap == null)
            {
                return new TableMapping
                {
                    SourceTable = change.TableName,
                    DestinationTable = change.TableName,
                    SourceSchema = string.Empty,
                    DestinationSchema = string.Empty
                };
            }
            return tableMap;
        }

        public TableMapping GetTableMapping(string schema, string tableName)
        {
            var tableMap = Mappings.FirstOrDefault(m => m.SourceTable == tableName && m.SourceSchema == schema);
            return tableMap;
        }
    }
}

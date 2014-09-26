using Newtonsoft.Json;

namespace DbSyncService.Models
{
    public class TableMapping
    {
        public string SourceSchema { get; set; }
        public string SourceTable { get; set; }
        public string CustomSourceSQLForSyncOnly { get; set; }
        public string DestinationSchema { get; set; }
        public string DestinationTable { get; set; }
        public bool TruncateDestinationAndBulkLoadFromSource { get; set; }
        public string CustomSourceSQLForBulkLoadOnly { get; set; }
        public int Ordinal { get; set; }

        [JsonIgnore]
        public string FullyQualifiedSourceTable
        {
            get { return string.IsNullOrEmpty(SourceSchema) ? SourceTable : SourceSchema + "." + SourceTable; }
        }
        [JsonIgnore]
        public string FullyQualifiedDestinationTable
        {
            get { return string.IsNullOrEmpty(DestinationSchema) ? "[" + DestinationTable + "]" : "[" + DestinationSchema + "].[" + DestinationTable + "]"; }
        }
    }
}

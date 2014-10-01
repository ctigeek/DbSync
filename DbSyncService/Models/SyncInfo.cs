using System.Collections.Generic;

namespace DbSyncService.Models
{
    public class SyncInfo
    {
        public readonly string SourceTableName;
        public readonly string SourceSchemaName;
        public List<object> SourceDataList;
        public readonly string DestinationTableName;
        public readonly string DestinationSchemaName;
        public List<object> DestinationDataList;
        public readonly int PrimaryKey1; 
        public readonly int PrimaryKey2;
        public List<int> differentColumns;

        public SyncInfo(string tableName, string schemaName, int primaryKey1, int primaryKey2 = -1)
        {
            SourceTableName = tableName;
            SourceSchemaName = schemaName;
            PrimaryKey1 = primaryKey1;
            PrimaryKey2 = primaryKey2;
            DestinationDataList = new List<object>();
            SourceDataList = new List<object>();
        }
    }
}

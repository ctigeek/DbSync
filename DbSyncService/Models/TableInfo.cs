namespace DbSyncService.Models
{
    public class TableInfo
    {
        public readonly string ConnectionString;
        public readonly string TableName;
        public readonly string SchemaName;
        public readonly ColumnInfo[] Columns;

        public TableInfo(string connectionString, string tableName, string schemaName, ColumnInfo[] columns)
        {
            ConnectionString = connectionString;
            TableName = tableName;
            SchemaName = schemaName;
            Columns = columns;
        }
    }

    public class ColumnInfo
    {
        public readonly string ColumnName;
        public readonly int ColumnID;
        public readonly string DataType;
        public readonly int MaxLength;
        public readonly int Precision;
        public readonly int Scale;
        public readonly bool Nullable;
        public readonly bool PrimaryKey;
        public readonly bool Identity;

        public ColumnInfo(string columnName, int columnId, string dataType, int maxLength, int precision, int scale, bool nullable, bool primaryKey, bool identity)
        {
            ColumnName = columnName;
            ColumnID = columnId;
            DataType = dataType;
            MaxLength = maxLength;
            Precision = precision;
            Scale = scale;
            Nullable = nullable;
            PrimaryKey = primaryKey;
            Identity = identity;
        }
    }
}

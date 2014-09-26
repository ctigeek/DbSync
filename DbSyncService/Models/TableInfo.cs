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
            this.ConnectionString = connectionString;
            this.TableName = tableName;
            this.SchemaName = schemaName;
            this.Columns = columns;
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
            this.ColumnName = columnName;
            this.ColumnID = columnId;
            this.DataType = dataType;
            this.MaxLength = maxLength;
            this.Precision = precision;
            this.Scale = scale;
            this.Nullable = nullable;
            this.PrimaryKey = primaryKey;
            this.Identity = identity;
        }
    }
}

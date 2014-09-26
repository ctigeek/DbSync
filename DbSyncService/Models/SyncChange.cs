namespace DbSyncService.Models
{
    public enum Operation
    {
        insert,
        update,
        delete
    }

    public enum RowChangeStatus
    {
        none = 0,
        processing = 1,
        complete = 99,
        error = 13
    }

    public class SyncChange
    {
        public readonly int RowId;
        public readonly string TableName;
        public readonly string SchemaName;
        public readonly Operation Operation;
        public readonly RowChangeStatus RowChangeStatus;
        public readonly int PrimaryKey1;  //TODO: these need to be objects, not ints.
        public readonly int PrimaryKey2;
        public readonly int TransactionId;

        public SyncChange(int rowId, string tableName, string schemaName, Operation operation, RowChangeStatus rowChangeStatus, int primaryKey1, int transactionId, int primaryKey2 = -1)
        {
            this.TableName = tableName;
            this.SchemaName = schemaName;
            this.RowId = rowId;
            this.Operation = operation;
            this.RowChangeStatus = rowChangeStatus;
            this.PrimaryKey1 = primaryKey1;
            this.PrimaryKey2 = primaryKey2;
            this.TransactionId = transactionId;
        }
    }
}

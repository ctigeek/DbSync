using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using DbSyncService.Models;
using DbSyncService.Utilities;
using QueryHelper;

namespace DbSyncService.SyncProvider
{
    class SyncChangesData
    {
        private readonly QueryRunner SourceDb;
        private readonly TableSet tableSet;
        public SyncChangesData(TableSet tableSet)
        {
            this.tableSet = tableSet;
            this.SourceDb = new QueryRunner(tableSet.SourceConnectionStringName);
        }

        public List<SyncChange> GetNextSetOfChanges()
        {
            var changes = new List<SyncChange>();
            var allTheChanges = GetChanges();
            if (allTheChanges.Count > 0)
            {
                var transactionId = allTheChanges.First().TransactionId;
                changes = allTheChanges.Where(c => c.TransactionId == transactionId).OrderBy(c => c.RowId).ToList();
                UpdateStatusOfChanges(changes, RowChangeStatus.processing);
            }
            return changes;
        }

        public SQLQuery BuildQueryToRemoveChangesForTable(string schemaName, string tableName)
        {
            string sql = "delete from " + tableSet.FullyQualifiedSyncTableName + " where `schema` = ?schema AND `table` = ?table;";
            var query = new SQLQuery(sql);
            query.Parameters.Add("schema", schemaName);
            query.Parameters.Add("table", tableName);
            return query;
        }

        public void UpdateStatusOfChanges(IEnumerable<SyncChange> changes, RowChangeStatus newStatus)
        {
            string sql = "update " + tableSet.FullyQualifiedSyncTableName + " set status = ?status where id = ?id;";
            var queryList = new List<SQLQuery>();
            //TODO:this can be a single query...
            foreach (var change in changes)
            {
                var query = new SQLQuery(sql, SQLQueryType.NonQuery);
                query.Parameters.Add("status", (int) newStatus);
                query.Parameters.Add("id", change.RowId);
                queryList.Add(query);
            }
            SourceDb.RunQuery(queryList, true);
        }

        private List<SyncChange> GetChanges()
        {
            string sql = "select * from " + tableSet.FullyQualifiedSyncTableName + " where status = ?status order by id;";
            var changes = new List<SyncChange>();

            var query = new SQLQuery(sql);
            query.Parameters.Add("status", (int)RowChangeStatus.none);
            query.ProcessRow = reader =>
            {
                changes.Add(SyncChangeFromDataReader(reader));
                return true;
            };

            SourceDb.RunQuery(query);
            return changes;
        }
        private SyncChange SyncChangeFromDataReader(DbDataReader reader)
        {
            var primaryKey1 = Convert.ToInt32(reader["pk1"]);
            var primaryKey2 = reader["pk2"] == DBNull.Value ? -1 : Convert.ToInt32(reader["pk2"]);

            return new SyncChange(
                rowId: Convert.ToInt32(reader["id"]),
                tableName: (string) reader["table"],
                schemaName: (string) reader["schema"],
                operation: (Operation)Enum.Parse(typeof(Operation), (string)reader["operation"]),
                rowChangeStatus: (RowChangeStatus)(Convert.ToInt32(reader["status"])),
                primaryKey1: primaryKey1,
                transactionId: Convert.ToInt32(reader["transactionId"]),
                primaryKey2: primaryKey2
                );
        }

    }
}

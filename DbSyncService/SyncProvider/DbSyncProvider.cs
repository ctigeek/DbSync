using System;
using System.Collections.Generic;
using System.Linq;
using DbSyncService.Models;
using DbSyncService.Utilities;
using QueryHelper;

namespace DbSyncService.SyncProvider
{
    internal class DbSyncProvider
    {
        private QueryRunner sourceDb;
        private QueryRunner destinationDB;
        
        private readonly SyncChangesData syncChangesData;
        private readonly TableSet tableSet;
        public bool Debug { get; set; }

        public DbSyncProvider(TableSet tableSet)
        {
            this.tableSet = tableSet;
            sourceDb = new QueryRunner(tableSet.SourceConnectionStringName);
            destinationDB = new QueryRunner(tableSet.DestinationConnectionStringName);
            syncChangesData = new SyncChangesData(tableSet);
        }

        public void ProcessSyncChanges()
        {
            try
            {
                var changes = syncChangesData.GetNextSetOfChanges();
                while (changes.Count > 0)
                {
                    ProcessChangeSet(changes);
                    changes = syncChangesData.GetNextSetOfChanges();
                }
            }
            catch (Exception exception)
            {
                exception.WriteToApplicationLog();
            }
        }

        private void ProcessChangeSet(List<SyncChange> changes)
        {
            try
            {
                var queries = new List<SQLQuery>();
                foreach (var change in changes)
                {
                    var data = GetDataFromSource(change);
                    if (data.Count > 0)
                    {
                        var query = GetQueryForChange(change, data);
                        queries.Add(query);
                    }
                }
                if (queries.Count > 0)
                {
                    destinationDB.RunQuery(queries, true);
                }
                else 
                {
                    //log???
                }
                SetPerformanceCounters(changes);
                syncChangesData.UpdateStatusOfChanges(changes, RowChangeStatus.complete);
            }
            catch (Exception ex)
            {
                try
                {
                    MakeSureIdentityInsertIsOff(changes);
                    ex.WriteToApplicationLog();
                    PerformanceCounters.AddRowsErrored(changes.Count);
                    syncChangesData.UpdateStatusOfChanges(changes, RowChangeStatus.error);
                }
                catch (Exception ex2)
                {
                    ex2.WriteToApplicationLog();
                }
            }
        }

        private void SetPerformanceCounters(List<SyncChange> changes)
        {
            try
            {
                var inserts = changes.Count(c => c.Operation == Operation.insert);
                var deletes = changes.Count(c => c.Operation == Operation.delete);
                var updates = changes.Count(c => c.Operation == Operation.update);
                PerformanceCounters.AddRowsInserted(inserts);
                PerformanceCounters.AddRowsDeleted(deletes);
                PerformanceCounters.AddRowsUpdated(updates);
            }
            catch (Exception exception)
            {
                exception.WriteToApplicationLog();
            }
        }

        private List<Tuple<string, object>> GetDataFromSource(SyncChange change)
        {
            var tableMap = tableSet.GetTableMapping(change); 
            var tableInfo = destinationDB.GetTableInfo(tableMap.DestinationSchema, tableMap.DestinationTable);
            if (change.Operation == Operation.delete)
            {
                return GetDataForDelete(change);
            }
            string sql = null;
            if (!string.IsNullOrEmpty(tableMap.CustomSourceSQLForSyncOnly))
            {
                sql = tableMap.CustomSourceSQLForSyncOnly;
            }
            else
            {
                var whereColumns = tableInfo.Columns.Where(c => c.PrimaryKey).Select(c => "(" + c.ColumnName + " = ?" + c.ColumnName + ")");
                sql = "select * from " + change.TableName + " where " + string.Join(" AND ", whereColumns) + ";";
            }
            var query = new SQLQuery(sql);
            bool firstKey = true; //yes, it's hacky
            foreach (var column in tableInfo.Columns.Where(c => c.PrimaryKey).OrderBy(c => c.ColumnID))
            {
                if (sql.Contains("?" + column.ColumnName)) //get rid of this and just throw.....
                {
                    query.Parameters.Add(column.ColumnName, firstKey ? change.PrimaryKey1 : change.PrimaryKey2);
                }
                firstKey = false;
            }
            var dataList = new List<Tuple<string, object>>();
            query.ProcessRow = reader =>
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dataList.Add(new Tuple<string, object>(reader.GetName(i), reader[i]));
                }
                return false;
                //return true here, but have a separate variable to see if this gets called again, if so, throw error...
            };
            sourceDb.RunQuery(query);
            return ConvertDataTypes(dataList);
        }

        private List<Tuple<string, object>> GetDataForDelete(SyncChange change)
        {
            var tableMap = tableSet.GetTableMapping(change);
            var tableInfo = destinationDB.GetTableInfo(tableMap.DestinationSchema, tableMap.DestinationTable);
            bool firstKey = true; //yes, it's hacky
            var tupleList = new List<Tuple<string, object>>();
            foreach (var column in tableInfo.Columns.Where(c => c.PrimaryKey).OrderBy(c => c.ColumnID))
            {
                tupleList.Add(new Tuple<string, object>(column.ColumnName, firstKey ? change.PrimaryKey1 : change.PrimaryKey2));
                firstKey = false;
            }
            return tupleList;
        }

        private SQLQuery GetQueryForChange(SyncChange change, List<Tuple<string, object>> data)
        {
            if (change.Operation == Operation.insert)
            {
                return GetInsertQuery(change, data);
            }
            else if (change.Operation == Operation.delete)
            {
                return GetDeleteQuery(change, data);
            }
            else if (change.Operation == Operation.update)
            {
                return GetUpdateQuery(change, data);
            }
            return null;
        }

        private SQLQuery GetInsertQuery(SyncChange change, List<Tuple<string, object>> rowData)
        {
            var tableMap = tableSet.GetTableMapping(change);
            var tableInfo = destinationDB.GetTableInfo(tableMap.DestinationSchema, tableMap.DestinationTable);
            var columnList = string.Join(",", tableInfo.Columns.OrderBy(c => c.ColumnID).Select(c => "[" + c.ColumnName + "]"));
            var parameterList = string.Join(",", tableInfo.Columns.OrderBy(c => c.ColumnID).Select(c => "@" + c.ColumnName));

            var sql = "insert into " + tableMap.FullyQualifiedDestinationTable + " ( " + columnList + ") values (" + parameterList + ");";
            if (tableInfo.Columns.Any(c => c.Identity))
            {
                sql = "set identity_insert " + tableMap.FullyQualifiedDestinationTable + " ON; " + sql + " set identity_insert " + tableMap.FullyQualifiedDestinationTable + " OFF;";
            }

            var query = new SQLQuery(sql, SQLQueryType.NonQuery);
            foreach (var columnInfo in tableInfo.Columns)
            {
                var tuple = rowData.First(t => t.Item1 == columnInfo.ColumnName);
                query.Parameters.Add("@" + columnInfo.ColumnName, tuple.Item2);
            }
            return query;
        }

        private SQLQuery GetDeleteQuery(SyncChange change, List<Tuple<string, object>> data)
        {
            var tableMap = tableSet.GetTableMapping(change);
            var tableInfo = destinationDB.GetTableInfo(tableMap.DestinationSchema, tableMap.DestinationTable);
            var whereColumns = tableInfo.Columns.Where(c => c.PrimaryKey).Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName);
            var sql = "delete from " + tableMap.FullyQualifiedDestinationTable + " where " +
                      string.Join(" AND ", whereColumns) + ";";
            var query = new SQLQuery(sql, SQLQueryType.NonQuery);
            AddColumnParametersToQuery(query, tableInfo.Columns.Where(c => c.PrimaryKey), data);
            return query;
        }

        private SQLQuery GetUpdateQuery(SyncChange change, List<Tuple<string, object>> data)
        {
            var tableMap = tableSet.GetTableMapping(change);
            var tableInfo = destinationDB.GetTableInfo(tableMap.DestinationSchema, tableMap.DestinationTable);
            var whereColumns = tableInfo.Columns.Where(c => c.PrimaryKey).Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName);
            var updates = tableInfo.Columns.Where(c => !c.PrimaryKey).Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName);
            var sql = "update " + tableMap.FullyQualifiedDestinationTable + " set " + string.Join(", ", updates) + " where " + string.Join(" AND ", whereColumns) + ";";
            var query = new SQLQuery(sql, SQLQueryType.NonQuery);
            AddColumnParametersToQuery(query, tableInfo.Columns, data);

            return query;
        }

        private void AddColumnParametersToQuery(SQLQuery query, IEnumerable<ColumnInfo> columnInfos, List<Tuple<string, object>> data)
        {
            foreach (var columnInfo in columnInfos)
            {
                var tuple = data.First(t => t.Item1 == columnInfo.ColumnName);
                query.Parameters.Add(columnInfo.ColumnName, tuple.Item2);
            }
        }

        private List<Tuple<string, object>> ConvertDataTypes(List<Tuple<string, object>> rowData)
        {
            foreach (var tuple in rowData.ToArray())
            {
                if (tuple.Item2 is uint)
                {
                    rowData.Remove(tuple);
                    rowData.Add(new Tuple<string, object>(tuple.Item1, Convert.ToInt32(tuple.Item2)));
                }
                else if (tuple.Item2 is ulong)
                {
                    rowData.Remove(tuple);
                    rowData.Add(new Tuple<string, object>(tuple.Item1, Convert.ToInt64(tuple.Item2)));
                }
                else if (tuple.Item2 is MySql.Data.Types.MySqlDateTime)
                {
                    rowData.Remove(tuple);
                    var datetime = Convert.ToDateTime(tuple.Item2.ToString());
                    if (datetime.Year < 1753)
                    {
                        datetime = new DateTime(1753, 1, 1);
                    }
                    rowData.Add(new Tuple<string, object>(tuple.Item1, datetime));
                }
            }
            return rowData;
        }

        private void MakeSureIdentityInsertIsOff(List<SyncChange> changes)
        {
            foreach (var change in changes)
            {
                var tableMap = tableSet.GetTableMapping(change);
                var tableInfo = destinationDB.GetTableInfo(tableMap.DestinationSchema, tableMap.DestinationTable);
                if (tableInfo.Columns.Any(c => c.Identity))
                {
                    try
                    {
                        sourceDb.RunNonQuery(" set identity_insert " + tableMap.FullyQualifiedSourceTable + " OFF;");
                    }
                    catch (Exception exception)
                    {
                        exception.WriteToApplicationLog();
                    }
                }
            }
        }
    }
}

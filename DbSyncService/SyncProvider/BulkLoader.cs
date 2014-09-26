using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Channels;
using DbSyncService.Models;
using DbSyncService.Utilities;
using QueryHelper;

namespace DbSyncService.SyncProvider
{
    public class BulkLoader
    {
        private readonly TableSet tableSet;
        private readonly string DestinationConnectionString;
        private readonly ConnectionStringSettings SourceConnectionSetting;
        private readonly DbProviderFactory sourceDbFactory;
        private readonly SyncChangesData syncChangesData;

        public BulkLoader(TableSet tableSet)
        {
            this.tableSet = tableSet;
            DestinationConnectionString = ConfigurationManager.ConnectionStrings[tableSet.DestinationConnectionStringName].ConnectionString;
            SourceConnectionSetting = ConfigurationManager.ConnectionStrings[tableSet.SourceConnectionStringName];
            syncChangesData = new SyncChangesData(tableSet);
            sourceDbFactory = DbProviderFactories.GetFactory(SourceConnectionSetting.ProviderName);
        }

        public void TruncateDestinationTables()
        {
            var destinationQueryRunner = new QueryRunner(tableSet.DestinationConnectionStringName);
            var sourceQueryRunner = new QueryRunner(tableSet.SourceConnectionStringName);

            var destinationQueries = new List<SQLQuery>();
            var sourceQueries = new List<SQLQuery>();

            foreach (var tableMap in tableSet.Mappings.Where(ts=>ts.TruncateDestinationAndBulkLoadFromSource).OrderByDescending(ts => ts.Ordinal))
            {
                Logging.WriteMessageToApplicationLog("About to delete all rows in table " + tableMap.FullyQualifiedDestinationTable, EventLogEntryType.Information);
                var destinationSql = "delete from " + tableMap.FullyQualifiedDestinationTable + ";";
                var destinationQuery = new SQLQuery(destinationSql, SQLQueryType.NonQuery);
                destinationQueries.Add(destinationQuery);
                
                sourceQueries.Add(syncChangesData.BuildQueryToRemoveChangesForTable(tableMap.SourceSchema, tableMap.SourceTable));
            }
            destinationQueryRunner.RunQuery(destinationQueries, true);
            sourceQueryRunner.RunQuery(sourceQueries, true);
        }

        public void BulkLoadDestinationTables()
        {
            foreach (var tableMap in tableSet.Mappings.Where(ts=>ts.TruncateDestinationAndBulkLoadFromSource).OrderBy(ts => ts.Ordinal))
            {
                Logging.WriteMessageToApplicationLog("About to bulk load data from " + tableMap.FullyQualifiedSourceTable + " to " + tableMap.FullyQualifiedDestinationTable, EventLogEntryType.Information);
                using (var bulkCopy = new SqlBulkCopy(DestinationConnectionString, SqlBulkCopyOptions.KeepIdentity))
                {
                    bulkCopy.DestinationTableName = tableMap.FullyQualifiedDestinationTable;
                    bulkCopy.EnableStreaming = true;

                    using (var conn = openSourceConnection())
                    {
                        var sql = "select * from " + tableMap.FullyQualifiedSourceTable + ";";
                        if (!string.IsNullOrEmpty(tableMap.CustomSourceSQLForBulkLoadOnly))
                        {
                            sql = tableMap.CustomSourceSQLForBulkLoadOnly;
                        }
                        var command = createCommand(conn, sql);
                        using (var reader = command.ExecuteReader())
                        {
                            bulkCopy.WriteToServer(reader);
                        }
                    }
                    bulkCopy.Close();
                }
            }
        }

        private DbCommand createCommand(DbConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            return command;
        }
        private DbConnection openSourceConnection()
        {
            var connection = sourceDbFactory.CreateConnection();
            connection.ConnectionString = SourceConnectionSetting.ConnectionString;
            connection.Open();
            return connection;
        }
    }
}

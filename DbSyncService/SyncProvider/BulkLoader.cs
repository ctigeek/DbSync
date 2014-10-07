﻿using System;
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
        private readonly int BulkCopyTimeout;

        public BulkLoader(TableSet tableSet)
        {
            this.tableSet = tableSet;
            DestinationConnectionString = ConfigurationManager.ConnectionStrings[tableSet.DestinationConnectionStringName].ConnectionString;
            SourceConnectionSetting = ConfigurationManager.ConnectionStrings[tableSet.SourceConnectionStringName];
            syncChangesData = new SyncChangesData(tableSet);
            sourceDbFactory = DbProviderFactories.GetFactory(SourceConnectionSetting.ProviderName);
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["BulkCopyTimeoutInSeconds"]))
            {
                BulkCopyTimeout = 180;
            }
            else
            {
                BulkCopyTimeout = int.Parse(ConfigurationManager.AppSettings["BulkCopyTimeoutInSeconds"]);
            }
        }

        public void TruncateDestinationTables()
        {
            var destinationQueryRunner = new QueryRunner(tableSet.DestinationConnectionStringName);
            var destinationQueries = new List<SQLQuery>();

            foreach (var tableMap in tableSet.Mappings.Where(ts=>ts.TruncateDestinationAndBulkLoadFromSource).OrderByDescending(ts => ts.Ordinal))
            {
                Logging.WriteMessageToApplicationLog("About to delete all rows in table " + tableMap.FullyQualifiedDestinationTable, EventLogEntryType.Information);
                var destinationSql = "delete from " + tableMap.FullyQualifiedDestinationTable + ";";
                var destinationQuery = new SQLQuery(destinationSql, SQLQueryType.NonQuery);
                destinationQueries.Add(destinationQuery);
            }
            destinationQueryRunner.RunQuery(destinationQueries, true);
        }

        public void BulkLoadDestinationTables()
        {
            var sourceQueryRunner = new QueryRunner(tableSet.SourceConnectionStringName);

            foreach (var tableMap in tableSet.Mappings.Where(ts=>ts.TruncateDestinationAndBulkLoadFromSource).OrderBy(ts => ts.Ordinal))
            {
                var query = syncChangesData.BuildQueryToRemoveChangesForTable(tableMap.SourceSchema, tableMap.SourceTable);
                sourceQueryRunner.RunQuery(query);

                Logging.WriteMessageToApplicationLog("About to bulk load data from " + tableMap.FullyQualifiedSourceTable + " to " + tableMap.FullyQualifiedDestinationTable, EventLogEntryType.Information);
                using (var bulkCopy = new SqlBulkCopy(DestinationConnectionString, SqlBulkCopyOptions.KeepIdentity))
                {
                    bulkCopy.DestinationTableName = tableMap.FullyQualifiedDestinationTable;
                    bulkCopy.EnableStreaming = true;
                    bulkCopy.BulkCopyTimeout = BulkCopyTimeout;
                    using (var conn = openSourceConnection())
                    {
                        var sql = "select * from " + tableMap.FullyQualifiedSourceTable + ";";
                        if (!string.IsNullOrEmpty(tableMap.CustomSourceSQLForBulkLoadOnly))
                        {
                            sql = tableMap.CustomSourceSQLForBulkLoadOnly;
                        }
                        var command = createCommand(conn, sql);
                        command.CommandTimeout = BulkCopyTimeout;
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

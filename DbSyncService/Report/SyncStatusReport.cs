using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using DbSyncService.Models;
using QueryHelper;
using RestSharp;

namespace DbSyncService.Report
{
    public class SyncStatusReport
    {
        private const string BaseUrl = "";
        private const string ApiKey = "";
        private const string Sender = "";
        private const string Recipient = "";
        private const string CountSql = "select count(*) from ";
        private HttpClient client;
        private readonly IRestClient restClient; 
        private readonly QueryRunner sourceDB;
        private readonly QueryRunner destinationDB;
        private readonly TableSet tableSet;
        
        public SyncStatusReport(TableSet tableSet)
        {
            this.tableSet = tableSet;
            sourceDB = new QueryRunner(tableSet.SourceConnectionStringName);
            destinationDB = new QueryRunner(tableSet.DestinationConnectionStringName);
            restClient = new RestClient(BaseUrl);
        }

        public string GenerateStatusReport()
        {
            var report = new StringBuilder();
            report.Append(GetRowCountReport());
            report.Append(GetSyncChangesReport());
            return report.ToString();
        }

        public string GetRowCountReport()
        {
            var differences = new StringBuilder();

            foreach (var tableMap in tableSet.Mappings)
            {
               CompareRowCounts(tableMap, differences);
            }

            differences.Insert(0, differences.Length > 0
                ? "\nThe following tables have different row counts:\n"
                : "\nAll row counts are equal.");
            return differences.ToString();
        }

        private void CompareRowCounts(TableMapping tableMap, StringBuilder differences)
        {
            var sourceCount = sourceDB.RunScalerQuery<string>(CountSql + tableMap.FullyQualifiedSourceTable);
            var destinationCount = destinationDB.RunScalerQuery<string>(CountSql + tableMap.FullyQualifiedDestinationTable);

            if (Convert.ToInt32(sourceCount) != Convert.ToInt32(destinationCount))
            {
                differences.Append(string.Format("\t-[{0}].[{1}] : {2}\n\t [{3}].[{4}] : {5}\n\n", tableMap.SourceSchema,
                    tableMap.SourceTable, sourceCount, tableMap.DestinationSchema, tableMap.DestinationTable,
                    destinationCount));
            }
        }

        private IEnumerable<SyncInfo> GetSyncChangedList()
        {
            string sql = "select distinct `schema`, `table`, `pk1`, `pk2` from "
                + tableSet.FullyQualifiedSyncTableName + " where `datetime` >= DATE_SUB(curdate(), INTERVAL 1 DAY);";
            var syncChangeList = new List<SyncInfo>();
            var syncChangesQuery = new SQLQuery(sql)
            {
                ProcessRow = reader =>
                {
                    syncChangeList.Add(PopulateChangeListFromDataReader(reader));
                    return true;
                }
            };

            sourceDB.RunQuery(syncChangesQuery);
            return syncChangeList;
        }

        private void GetDataFromTable(SyncInfo reportInfo, TableInfo tableInfo, TableMapping tableMap, bool isSource, string tableName)
        {
            string sql;
            string parameterSymbol = isSource ? "?" : "@";
            bool firstKey = true;

            if (isSource && !string.IsNullOrEmpty(tableMap.CustomSourceSQLForSyncOnly))
            {
                sql = tableMap.CustomSourceSQLForSyncOnly;
            }
            else
            {
                var whereColumns = tableInfo.Columns.Where(c => c.PrimaryKey).Select(c => "(" + c.ColumnName + " = " + parameterSymbol + c.ColumnName + ")");
                sql = "select * from " + tableName + " where " +
                      string.Join(" AND ", whereColumns) + ";";
            }
            var query = new SQLQuery(sql);

            foreach (var column in tableInfo.Columns.Where(c => c.PrimaryKey).OrderBy(c => c.ColumnID))
            {
                if (sql.Contains(parameterSymbol + column.ColumnName))
                {
                    query.Parameters.Add(column.ColumnName, firstKey ? reportInfo.PrimaryKey1 : reportInfo.PrimaryKey2);
                }
                firstKey = false;
            }

            if (isSource)
            {
                query.ProcessRow = reader =>
                {

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        reportInfo.SourceDataList.Insert(i, ConvertDataTypes(reader[i]));
                    }
                    return false;
                };
                sourceDB.RunQuery(query);
            }
            else
            {
                query.ProcessRow = reader =>
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        reportInfo.DestinationDataList.Insert(i, ConvertDataTypes(reader[i]));
                    }
                    return false;
                };
                destinationDB.RunQuery(query);
            }
        }

        public string GetSyncChangesReport()
        {
            var differences = new StringBuilder();
            var syncChangeList = GetSyncChangedList();
            
            foreach (var syncChangeData in syncChangeList)
            {
                var tableMap = tableSet.GetTableMapping(syncChangeData.SourceSchemaName, syncChangeData.SourceTableName);
                var tableInfo = destinationDB.GetTableInfo(tableMap.DestinationSchema, tableMap.DestinationTable);
                string failedMessage = string.Format("sync to Destination Failed. Table : {0}. PK : {1}", 
                    tableMap.FullyQualifiedDestinationTable,syncChangeData.PrimaryKey1);

                GetDataFromTable(syncChangeData, tableInfo, tableMap, true, tableMap.FullyQualifiedSourceTable);
                GetDataFromTable(syncChangeData, tableInfo, tableMap, false, tableMap.FullyQualifiedDestinationTable);
                CheckData(syncChangeData, differences, failedMessage);
            }

            differences.Insert(0, differences.Length > 0
                ? "\nThe following differences were in the sync table:"
                : "\nThe sync table has been synced sucessfully.");

            return differences.ToString();
        }

        private void CheckData(SyncInfo syncChangeData, StringBuilder differences, string failedMessage)
        {
            if (syncChangeData.DestinationDataList.Count() > syncChangeData.SourceDataList.Count())
            {
                differences.Append("\n\t-Delete " + failedMessage);
            }
            else if (syncChangeData.DestinationDataList.Count() < syncChangeData.SourceDataList.Count())
            {
                differences.Append("\n\t-Insert " + failedMessage);
            }
            else if (syncChangeData.DestinationDataList.Count() == syncChangeData.SourceDataList.Count() &&
                     syncChangeData.SourceDataList.Count() != 0)
            {
                for (int i = 0; i < syncChangeData.DestinationDataList.Count(); i++)
                {
                    if (ConvertBoolToStringInt(syncChangeData.DestinationDataList[i].ToString().Trim()) != ConvertBoolToStringInt(syncChangeData.SourceDataList[i].ToString().Trim()))
                    {
                        differences.Append(
                            string.Format("\n\t-Column mismatch. Source Table: {0}, PK: {1}, Destination Value:{2}, Source Value:{3}",
                                syncChangeData.SourceSchemaName + "." + syncChangeData.SourceTableName, syncChangeData.PrimaryKey1, syncChangeData.DestinationDataList[i], syncChangeData.SourceDataList[i]));
                    }
                }

            }
        }

        private string ConvertBoolToStringInt(string boolValue)
        {
            string result = boolValue;
            if (boolValue.ToLower() == "true")
            {
                result = "1";
            }
            if (boolValue.ToLower() == "false")
            {
                result = "0";
            }
            return result;
        }

        private SyncInfo PopulateChangeListFromDataReader(DbDataReader reader)
        {
            var primaryKey1 = Convert.ToInt32(reader["pk1"]);
            var primaryKey2 = reader["pk2"] == DBNull.Value ? -1 : Convert.ToInt32(reader["pk2"]);

            return new SyncInfo(
                (string)reader["table"],
                (string)reader["schema"],
                primaryKey1,
                primaryKey2
                );
        }

        private object ConvertDataTypes(object columnData)
        {
            if (columnData is uint)
            {
                columnData = Convert.ToInt32(columnData);
            }
            else if (columnData is ulong)
            {
                columnData = Convert.ToInt64(columnData);
            }
            else if (columnData is ushort)
            {
                columnData = Convert.ToInt32(columnData);
            }
            else if (columnData is sbyte)
            {
                columnData = Convert.ToInt32(columnData);
            }
            else if (columnData is MySql.Data.Types.MySqlDateTime)
            {
                var datetime = Convert.ToDateTime(columnData.ToString());
                if (datetime.Year < 1753)
                {
                    datetime = new DateTime(1753, 1, 1);
                }
                columnData = datetime;
            }
            return columnData;
        }

        public void SendEmail(string message)
        {
            client = new HttpClient()
            {
                BaseAddress = new Uri(BaseUrl)
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", string.Format("DbSyncService; Process {0};", Process.GetCurrentProcess().ProcessName));
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

            SendEmail("DbSync "+ tableSet.Name +" Nightly Report", Sender, Recipient, message);
        }

        private IRestResponse SendEmail(string subject, string fromAddress, string recipients, string text)
        {
            var request = new RestRequest("", Method.POST)
            {
                Credentials = new NetworkCredential("api", ApiKey)
            };

            request.AddHeader("Accept", "application/json");
            request.AddParameter("from", fromAddress);
            request.AddParameter("subject", subject);
            request.AddParameter("to", recipients); 

            if (text != null)
            {
                request.AddParameter("text", text);
            }

            var response = restClient.Execute(request);

            if (response.ErrorException != null)
            {
                throw new ApplicationException("An error occurred when communicating with the email provider.");
            }

            if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new ApplicationException("An error occurred when communicating with the email provider.");
            }
            return response;
        }
    }
}

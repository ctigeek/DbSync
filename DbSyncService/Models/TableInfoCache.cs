using System;
using System.Collections.Generic;
using System.Linq;
using QueryHelper;

namespace DbSyncService.Models
{
    
    public static class TableInfoCache
    {
        private static List<TableInfo> tableInfoCache = new List<TableInfo>();

        //TODO: get foreign key info...

        public static TableInfo GetTableInfo(this QueryRunner database, string schema, string tableName)
        {
            var sql = @"SELECT distinct ISNULL(sch.name, '') 'SchemaName', tables.name 'TableName', c.name 'ColumnName', c.column_id 'ColumnID', 
    t.Name 'Datatype', c.max_length 'MaxLength',  c.precision 'Precision' , c.scale 'Scale', c.is_nullable 'Nullable', ISNULL(i.is_primary_key, 0) 'PrimaryKey', ISNULL(c.is_identity,0) 'IsIdentity'
	FROM  sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id 
	INNER JOIN sys.tables tables ON (tables.object_id = c.object_id)
	LEFT OUTER JOIN sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
	LEFT OUTER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
	LEFT OUTER JOIN sys.schemas sch on (sch.schema_id = tables.schema_id)
	WHERE c.object_id = OBJECT_ID(@tableName) order by TableName, ColumnID;";

            var tableInfo = tableInfoCache.FirstOrDefault(t => t.ConnectionString == database.ConnectionString && t.SchemaName == schema && t.TableName == tableName);

            if (tableInfo == null)
            {
                var columnList = new List<ColumnInfo>();
                string actualSchemaName = string.Empty;
                string actualTableName  = string.Empty;

                var query = new SQLQuery(sql);
                var schemaTableName = string.IsNullOrEmpty(schema) ? tableName : schema + "." + tableName;
                query.Parameters.Add("tableName", schemaTableName);
                query.ProcessRow = reader =>
                {
                    columnList.Add(new ColumnInfo(
                        (string) reader["ColumnName"],
                        Convert.ToInt32(reader["ColumnID"]),
                        (string) reader["DataType"],
                        Convert.ToInt32(reader["MaxLength"]),
                        Convert.ToInt32(reader["Precision"]),
                        Convert.ToInt32(reader["Scale"]),
                        (bool) reader["Nullable"],
                        (bool) reader["PrimaryKey"],
                        (bool) reader["IsIdentity"]));
                    actualSchemaName = (string) reader["SchemaName"];
                    actualTableName = (string) reader["TableName"];
                    return true;
                };
                database.RunQuery(query);
                if (columnList.Count == 0)
                {
                    throw new ApplicationException("Couldn't find any columns for table " + schemaTableName);
                }
                tableInfo = new TableInfo(database.ConnectionString, actualTableName, actualSchemaName, columnList.ToArray());
                tableInfoCache.Add(tableInfo);
            }
            return tableInfo;
        }
    }
}

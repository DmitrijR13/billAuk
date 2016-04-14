using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Migration;
using ECM7.Migrator.Framework;
using ECM7.Migrator.Providers.Validation;
using IBM.Data.Informix;

namespace ECM7.Migrator.Providers.Informix
{
    using System.Text;

    [ProviderValidation(typeof(IfxConnection), true)]
    public class InformixTransformationProvider : TransformationProvider, IMigratorProvider
    {
        public InformixTransformationProvider(IfxConnection connection, int DbConnectionTimeout)
            : base(connection, DbConnectionTimeout)
        {
            typeMap.Put(DbType.AnsiStringFixedLength, "char(255)");
            typeMap.Put(DbType.AnsiStringFixedLength, 8000, "char($l)");
            typeMap.Put(DbType.AnsiString, "varchar(255)");
            typeMap.Put(DbType.AnsiString, 8000, "varchar($l)");
            typeMap.Put(DbType.AnsiString, int.MaxValue, "text");
            typeMap.Put(DbType.Binary, "bytea");
            typeMap.Put(DbType.Binary, 2147483647, "bytea");
            typeMap.Put(DbType.Boolean, "boolean");
            typeMap.Put(DbType.Byte, "int2");
            typeMap.Put(DbType.Currency, "DECIMAL(16,4)");
            typeMap.Put(DbType.Date, "DATE");
            typeMap.Put(DbType.DateTime, "DATETIME YEAR to SECOND");
            typeMap.Put(DbType.Decimal, "DECIMAL(18,5)");
            typeMap.Put(DbType.Decimal, 18, "DECIMAL($l, $s)");
            typeMap.Put(DbType.Double, "FLOAT");
            typeMap.Put(DbType.Int16, "SMALLINT");
            typeMap.Put(DbType.Int32, "INTEGER");
            typeMap.Put(DbType.Int64, "int8");
            typeMap.Put(DbType.Single, "SMALLFLOAT");
            typeMap.Put(DbType.StringFixedLength, "char(255)");
            typeMap.Put(DbType.StringFixedLength, 4000, "char($l)");
            typeMap.Put(DbType.String, "varchar(255)");
            typeMap.Put(DbType.String, 4000, "char($l)");
            typeMap.Put(DbType.String, int.MaxValue, "text");
            typeMap.Put(DbType.Time, "time");

            propertyMap.RegisterPropertySql(ColumnProperty.Identity, "serial");
        }

        public void RegisterMigrator(IBaseMigrator migrator)
        {
            
        }

        #region Особенности СУБД

        public override string BatchSeparator { get { return ";"; } }
        public bool NeedQuotesForNames { get { return false; } }

        public override bool IdentityNeedsType { get { return false; } }

        protected override string NamesQuoteTemplate { get { return "{0}"; } }

        protected string GetQuotedName(object name)
        {
            var format = NeedQuotesForNames ? NamesQuoteTemplate : "{0}";
            return string.Format(format, name);
        }
        #endregion

        #region SQL Generator
        protected override string GetSqlAddTable(SchemaQualifiedObjectName table, string columnsSql)
        {
            string nspname = String.IsNullOrEmpty(table.Schema) ? String.Empty : string.Format("{0}:", table.Schema);
            return String.Format("CREATE TABLE {0}{1} ({2})", nspname, table.Name, columnsSql);
        }

        protected override string GetSqlRemoveTable(SchemaQualifiedObjectName table)
        {
            string nspname = String.IsNullOrEmpty(table.Schema) ? String.Empty : string.Format("{0}:", table.Schema);
            return String.Format("DROP TABLE {0}{1}", nspname, table.Name);
        }

        public override string GetSqlColumnDef(Column column, bool compoundPrimaryKey)
        {
            var sqlBuilder = new ColumnSqlBuilder(column, typeMap, propertyMap);

            sqlBuilder.AddColumnName(NamesQuoteTemplate);
            sqlBuilder.AddColumnType(IdentityNeedsType);

            // identity нуждается в типе
            sqlBuilder.AddSqlForIdentityWhichNeedsType(IdentityNeedsType);
            sqlBuilder.AddUniqueSql();
            sqlBuilder.AddDefaultValueSql(GetSqlDefaultValue);

            // identity не нуждается в типе
            sqlBuilder.AddSqlForIdentityWhichNotNeedsType(IdentityNeedsType);
            sqlBuilder.AddUnsignedSql();
            sqlBuilder.AddPrimaryKeySql(true);
            sqlBuilder.AddNotNullSql(NeedsNotNullForIdentity);

            return sqlBuilder.ToString();
        }

        protected override string GetSqlPrimaryKey(string pkName, List<string> primaryKeyColumns)
        {
            return FormatSql("PRIMARY KEY ({0:COLS})", primaryKeyColumns);
        }

        protected override string GetSqlAddColumn(SchemaQualifiedObjectName table, string columnSql)
        {
            string nspname = String.IsNullOrEmpty(table.Schema) ? String.Empty : string.Format("{0}:", table.Schema);
            return String.Format("ALTER TABLE {0}{1} ADD COLUMN {2}", nspname, table.Name, columnSql);
        }

        protected override string GetSqlRenameTable(SchemaQualifiedObjectName oldName, string newName)
        {
            string nspname = String.IsNullOrEmpty(oldName.Schema) ? String.Empty : string.Format("{0}:", oldName.Schema);
            return String.Format("RENAME TABLE {0}{1} TO {2}", nspname, oldName.Name, newName);
        }

        protected override string GetSqlAddIndex(string name, bool unique, SchemaQualifiedObjectName table, params string[] columns)
        {
            string nspname = String.IsNullOrEmpty(table.Schema) ? String.Empty : string.Format("{0}:", table.Schema);
            return FormatSql("CREATE {0}INDEX {1} ON {2}{3} ({4:COLS})", unique ? "UNIQUE " : String.Empty, name, nspname, table.Name, columns);
        }

        protected override string GetSqlChangeColumnType(SchemaQualifiedObjectName table, string column, ColumnType columnType)
        {
            string columnTypeSql = typeMap.Get(columnType);
            string nspname = String.IsNullOrEmpty(table.Schema) ? String.Empty : string.Format("{0}:", table.Schema);
            return String.Format("ALTER TABLE {0}{1} ALTER COLUMN {2} {3}", nspname, table.Name, column, columnTypeSql);
        }
        #endregion

        #region custom sql
        protected override string GetSqlRemoveIndex(string indexName, SchemaQualifiedObjectName tableName)
        {
            SchemaQualifiedObjectName ixName = indexName.WithSchema(tableName.Schema);
            return FormatSql("DROP INDEX {0:NAME}", ixName);
        }

        protected override string GetSqlChangeNotNullConstraint(SchemaQualifiedObjectName table, string column, bool notNull, ref string sqlChangeColumnType)
        {
            // если изменение типа колонки и признака NOT NULL происходит одним запросом,
            // то изменяем параметр sqlChangeColumnType и возвращаем NULL
            // иначе возвращаем запрос, меняющий признак NOT NULL

            string sqlNotNull = notNull ? "SET NOT NULL" : "DROP NOT NULL";
            return FormatSql("ALTER TABLE {0:NAME} ALTER COLUMN {1:NAME} {2}", table, column, sqlNotNull);
        }

        public override bool IndexExists(string indexName, SchemaQualifiedObjectName tableName)
        {
            string nspname = tableName.SchemaIsEmpty ? String.Empty : string.Format("{0}:", tableName.Schema);
            string tablename = NeedQuotesForNames ? tableName.Name : tableName.Name.ToLower();
            string indname = NeedQuotesForNames ? indexName : indexName.ToLower();

            var builder = new StringBuilder();

            builder.AppendFormat("SELECT COUNT(*) FROM {0}sysindices ind ", nspname);
            builder.AppendFormat("INNER JOIN {0}systables tbl on (tbl.tabid = ind.tabid) ", nspname);
            builder.AppendFormat("WHERE tbl.tabname = '{0}' ", tablename);
            builder.AppendFormat("AND ind.idxname = '{0}' ", indname);

            int count = Convert.ToInt32(ExecuteScalar(builder.ToString()));
            return count > 0;
        }

        public override bool ConstraintExists(SchemaQualifiedObjectName table, string name)
        {
            throw new NotSupportedException("Возможность не реализована для Informix.");
            /* 
            string nspname = table.SchemaIsEmpty ? "current_schema()" : string.Format("'{0}'", table.Schema);
            string tablename = NeedQuotesForNames ? table.Name : table.Name.ToLower();
            string keyname = NeedQuotesForNames ? name : name.ToLower();

            string sql = FormatSql(
                            "SELECT {0:NAME} FROM {1:NAME}.{2:NAME} WHERE {3:NAME} = {4} AND {5:NAME} = '{6}' AND {7:NAME} = '{8}'",
                                    "constraint_name", "information_schema", "table_constraints", "table_schema",
                                    nspname, "constraint_name", keyname, "table_name", tablename);

            using (IDataReader reader = ExecuteReader(sql))
            {
                return reader.Read();
            }
            */
        }

        public override bool ColumnExists(SchemaQualifiedObjectName table, string column)
        {
            string nspname = table.SchemaIsEmpty ? String.Empty : string.Format("{0}:", table.Schema);
            string tablename = NeedQuotesForNames ? table.Name : table.Name.ToLower();
            string colname = NeedQuotesForNames ? column : column.ToLower();

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("SELECT colname FROM {0}syscolumns col ", nspname);
            sql.AppendFormat("INNER JOIN {0}systables tbl on (tbl.tabid = col.tabid) ", nspname);
            sql.AppendFormat("WHERE tbl.tabname = '{0}' ", tablename);
            sql.AppendFormat("AND col.colname = '{0}'", colname);

            using (IDataReader reader = ExecuteReader(sql.ToString()))
            {
                return reader.Read();
            }
        }

        public override bool TableExists(SchemaQualifiedObjectName table)
        {
            string nspname = table.SchemaIsEmpty ? String.Empty : string.Format("{0}:", table.Schema);
            string tablename = NeedQuotesForNames ? table.Name : table.Name.ToLower();

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("SELECT tabname FROM {0}systables ", nspname);
            sql.AppendFormat("WHERE tabname = '{0}'", tablename);

            using (IDataReader reader = ExecuteReader(sql.ToString()))
            {
                return reader.Read();
            }
        }

        public override SchemaQualifiedObjectName[] GetTables(string schema = null)
        {
            string nspname = String.IsNullOrEmpty(schema) ? String.Empty : string.Format("{0}:", schema);

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("SELECT tabname, {0} AS tabschema ", schema);
            sql.AppendFormat("FROM {0}systables ", nspname);
            sql.Append("WHERE locklevel = 'P';");

            var tables = new List<SchemaQualifiedObjectName>();

            using (IDataReader reader = ExecuteReader(sql.ToString()))
            {
                while (reader.Read())
                {
                    string tableName = reader.GetString(0);
                    string tableSchema = reader.GetString(1);
                    tables.Add(tableName.WithSchema(tableSchema));
                }
            }
            return tables.ToArray();
        }
        #endregion
    }
}

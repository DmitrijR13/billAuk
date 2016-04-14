using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using KP50.DataBase.Migrator.Providers.Validation;
using IBM.Data.Informix;
using ForeignKeyConstraint = KP50.DataBase.Migrator.Framework.ForeignKeyConstraint;

namespace KP50.DataBase.Migrator.Providers.Informix
{
    using System.Text;

    [ProviderValidation(typeof(IfxConnection), true)]
    public class InformixTransformationProvider : TransformationProvider
    {
        public InformixTransformationProvider(IfxConnection connection)
            : base(connection)
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
            typeMap.Put(DbType.Decimal, 18, "DECIMAL($l, $s)", 5);
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

        #region Особенности СУБД

        public override string BatchSeparator { get { return ";"; } }

        public override bool IdentityNeedsType { get { return false; } }

        protected override string NamesQuoteTemplate { get { return "{0}"; } }

        public override char TableDelimiter { get { return ':'; } }

        public override string ProviderName { get { return "Informix"; } }
        #endregion
        
        #region SQL Generator
        protected override string GetSqlAddColumn(SchemaQualifiedObjectName table, string columnSql)
        {
            return FormatSql("ALTER TABLE {0:NAME} ADD {1}", table, columnSql);
        }

        public override string GetSqlColumnDef(Column column, bool compoundPrimaryKey)
        {
            var sqlBuilder = new ColumnSqlBuilder(column, typeMap, propertyMap, GetQuotedName);

            sqlBuilder.AppendColumnName();
            sqlBuilder.AppendColumnType(IdentityNeedsType);

            // identity нуждается в типе
            sqlBuilder.AppendSqlForIdentityWhichNeedsType(IdentityNeedsType);
            sqlBuilder.AppendUniqueSql();
            sqlBuilder.AppendDefaultValueSql(GetSqlDefaultValue);

            // identity не нуждается в типе
            sqlBuilder.AppendSqlForIdentityWhichNotNeedsType(IdentityNeedsType);
            sqlBuilder.AppendUnsignedSql();
            sqlBuilder.AppendPrimaryKeySql(true);
            sqlBuilder.AppendNotNullSql(NeedsNotNullForIdentity);

            return sqlBuilder.ToString();
        }

        protected override string GetSqlPrimaryKey(string pkName, List<string> primaryKeyColumns)
        {
            return FormatSql("PRIMARY KEY ({0:COLS})", primaryKeyColumns);
        }

        protected override string GetSqlAddPrimaryKey(string name, SchemaQualifiedObjectName table, string[] columns)
        {
            return FormatSql(
              "ALTER TABLE {0:NAME} ADD CONSTRAINT PRIMARY KEY ({1:COLS}) CONSTRAINT {2:NAME}", table, columns, name);
        }

        public override bool ProcedureExists(string SchemaName, string Name)
        {
            using (IDataReader reader = ExecuteReader(FormatSql("SELECT procname FROM {0:NAME} WHERE procname = '{1}'", new SchemaQualifiedObjectName() { Name = "sysprocedures", Schema = SchemaName }, Name)))
            {
                return reader.Read();
            }
        }
        #endregion

        #region custom sql
        public override void AddForeignKey(string name,
          SchemaQualifiedObjectName primaryTable, string[] primaryColumns,
          SchemaQualifiedObjectName refTable, string[] refColumns,
          ForeignKeyConstraint onDeleteConstraint = ForeignKeyConstraint.NoAction,
          ForeignKeyConstraint onUpdateConstraint = ForeignKeyConstraint.NoAction)
        {
            string onDeleteConstraintResolved = null; // fkActionMap.GetSqlOnDelete(onDeleteConstraint);
            string onUpdateConstraintResolved = null; // fkActionMap.GetSqlOnUpdate(onUpdateConstraint);

             string sql = GetSqlAddForeignKey(name, primaryTable, primaryColumns, refTable, refColumns, onUpdateConstraintResolved, onDeleteConstraintResolved);

            ExecuteNonQuery(sql);
        }

        protected override string GetSqlAddForeignKey(string name, SchemaQualifiedObjectName primaryTable, string[] primaryColumns, SchemaQualifiedObjectName refTable, string[] refColumns, string onUpdateConstraintSql, string onDeleteConstraintSql)
        {
            return FormatSql(
              "ALTER TABLE {0:NAME} ADD CONSTRAINT FOREIGN KEY ({1:COLS}) REFERENCES {2:NAME} ({3:COLS}) CONSTRAINT {4:NAME} {5} {6}",
              primaryTable, primaryColumns, refTable, refColumns, name, onUpdateConstraintSql, onDeleteConstraintSql);
        }

        protected override string GetSqlRemoveIndex(string indexName, SchemaQualifiedObjectName tableName)
        {
            SchemaQualifiedObjectName ixName = indexName.WithSchema(tableName.Schema);
            return FormatSql("DROP INDEX {0:NAME}", ixName);
        }

        protected override string GetSqlRenameColumn(SchemaQualifiedObjectName tableName, string oldColumnName, string newColumnName)
        {
            return FormatSql("RENAME COLUMN {0:NAME}.{1:NAME} TO {2:NAME}",
                tableName, oldColumnName, newColumnName);
        }

        protected override string GetSqlRemoveColumn(SchemaQualifiedObjectName table, string column)
        {
            return FormatSql("ALTER TABLE {0:NAME} DROP {1:NAME}", table, column);
        }

        protected override string GetSqlChangeNotNullConstraint(SchemaQualifiedObjectName table, string column, bool notNull, ref string sqlChangeColumnType)
        {
            // если изменение типа колонки и признака NOT NULL происходит одним запросом,
            // то изменяем параметр sqlChangeColumnType и возвращаем NULL
            // иначе возвращаем запрос, меняющий признак NOT NULL

            string sqlNotNull = notNull ? "NOT NULL" : string.Empty;
            return FormatSql("{0} {1}", sqlChangeColumnType, sqlNotNull);
        }

        protected override string GetSqlChangeColumnType(SchemaQualifiedObjectName table, string column, ColumnType columnType)
        {
            string columnTypeSql = typeMap.Get(columnType);

            return FormatSql("ALTER TABLE {0:NAME} MODIFY {1:NAME} {2}", table, column, columnTypeSql);
        }

        public override bool IndexExists(string indexName, SchemaQualifiedObjectName tableName)
        {
            string nspname = tableName.SchemaIsEmpty ? string.Empty : string.Format("{0}", tableName.Schema);
            string tablename = NeedQuotesForNames ? tableName.Name : tableName.Name.ToLower();
            string indname = NeedQuotesForNames ? indexName : indexName.ToLower();

            int count = Convert.ToInt32(ExecuteScalar(FormatSql(
                "SELECT COUNT(*) FROM {0:NAME} ind INNER JOIN {1:NAME} tbl on (tbl.tabid = ind.tabid) " +
                "WHERE tbl.tabname = '{2}' AND ind.idxname = '{3}'",
                new SchemaQualifiedObjectName() { Name = "sysindices", Schema = nspname },
                new SchemaQualifiedObjectName() { Name = "systables", Schema = nspname },
                tablename,
                indname
                )));

            return count > 0;
        }

        public override bool ConstraintExists(SchemaQualifiedObjectName table, string name)
        {
            string nspname = table.SchemaIsEmpty ? string.Empty : string.Format("{0}:", table.Schema);
            string tablename = NeedQuotesForNames ? table.Name : table.Name.ToLower();
            string keyname = NeedQuotesForNames ? name : name.ToLower();

            string sql = FormatSql(
                "SELECT c.constrname FROM {0}sysconstraints c INNER JOIN {0}systables t ON c.tabid = t.tabid WHERE t.tabname = '{1}' AND c.constrname = '{2}'",
                nspname, tablename, keyname);

            using (IDataReader reader = ExecuteReader(sql))
            {
                return reader.Read();
            }
        }

        public override bool ColumnExists(SchemaQualifiedObjectName table, string column)
        {
            string nspname = table.SchemaIsEmpty ? String.Empty : string.Format("{0}:", table.Schema);
            string tablename = table.Name.ToLower();
            string colname = column.ToLower();

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

        public override bool SequenceExists(string schema, string name)
        {
            schema = string.IsNullOrWhiteSpace(schema)? string.Empty:string.Format("{0}:", schema);
            string sql = string.Format("SELECT t.tabname FROM {0}systables t INNER JOIN {0}syssequences s ON s.tabid = t.tabid WHERE t.tabname = '{1}';", schema, name);

            using (IDataReader reader = ExecuteReader(sql))
            {
                return reader.Read();
            }
        }

        public override void AddSequence(string schema, string name)
        {
            ExecuteNonQuery(string.Format("CREATE SEQUENCE {0}:{1}", schema, name));
        }

        public override void RemoveSequence(string schema, string name)
        {
            ExecuteNonQuery(string.Format("DROP SEQUENCE {0}:{1}", schema, name));
        }

        public override bool TableExists(SchemaQualifiedObjectName table)
        {
            string nspname = table.SchemaIsEmpty ? String.Empty : string.Format("{0}:", table.Schema);
            string tablename = NeedQuotesForNames ? table.Name : table.Name.ToLower();

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("SELECT tabname FROM {0}systables ", nspname);
            sql.AppendFormat("WHERE tabname = '{0}'", tablename.ToLower());

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

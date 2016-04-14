namespace STCLINE.KP50.DataBase
{
    using STCLINE.KP50.Global;
    using System;
    using System.Data;
    using STCLINE.KP50.Interfaces;

    public class DataBaseHeadServer: DataBaseHead, IDisposable
    {
        private IDbConnection _serverConnection;

        protected IDbConnection ServerConnection
        {
            get
            {
                if (_serverConnection == null)
                {
                    _serverConnection = GetConnection(Constants.cons_Kernel);
                    Returns ret = OpenDb(_serverConnection, true);
                }
                return _serverConnection;
            }
        }
        protected IDbTransaction Transaction;

        public DataBaseHeadServer()
            : base()
        {
            _serverConnection = null;
            Transaction = null;
        }

        ~DataBaseHeadServer()
        {
            Rollback();
            _serverConnection = null;
            Close();
        }

        public override void Dispose()
        {
            Rollback();
            _serverConnection = null;
            base.Dispose();
        }

        protected Returns ExecSQL(string sql)
        {
            return ExecSQL(sql, true);
        }

        protected Returns ExecSQL(string sql, bool inlog)
        {
            return ExecSQL(ServerConnection, Transaction, sql, inlog);
        }

        protected Returns ExecRead(out MyDataReader reader, string sql)
        {
            return ExecRead(ServerConnection, Transaction, out reader, sql, true);
        }

        protected object ExecScalar(string sql, out Returns ret)
        {
            return ExecScalar(ServerConnection, Transaction, sql, out ret, true);
        }

        public bool TempTableInWebCashe(string tab)
        {
            return TempTableInWebCashe(ServerConnection, Transaction, tab);
        }

        public string GetTableFullName(string dbName, string tableName)
        {
            return GetTableFullName(ServerConnection, dbName, tableName);
        }

        public DbTables GetDbTablesInstance()
        {
            return new DbTables(ServerConnection);
        }

        protected void BeginTransaction()
        {
            if (Transaction != null) return;
            Transaction = ServerConnection.BeginTransaction();
        }

        protected void Commit()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
                Transaction = null;
            }
        }

        protected void Rollback()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
                Transaction = null;
            }
        }

        protected int GetSerialValue()
        {
            return GetSerialValue(ServerConnection, Transaction);
        }

        public IntfResultTableType OpenSQL(string sqlString)
        {
            try
            {
                DataTable table = DBManager.ExecSQLToTable(ServerConnection, sqlString);
                return new IntfResultTableType(table);
            }
            catch (Exception ex)
            {
                return new IntfResultTableType(null, -1, ex.Message);
            }
        }
    }
}

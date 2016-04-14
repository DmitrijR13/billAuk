namespace STCLINE.KP50.DataBase
{
    using STCLINE.KP50.Global;
using System;
using System.Data;

    public class DataBaseHeadClient: DataBaseHead, IDisposable
    {
        private IDbConnection _clientConnection;

        protected IDbConnection ClientConnection
        {
            get
            {
                if (_clientConnection == null)
                {
                    _clientConnection = GetConnection(Constants.cons_Webdata);
                    Returns ret = OpenDb(_clientConnection, true);
                }
                return _clientConnection;
            }
        }
        private IDbTransaction Transaction;

        public DataBaseHeadClient()
            : base()
        {
            _clientConnection = null;
            Transaction = null;
        }

        ~DataBaseHeadClient()
        {
            Rollback();
            _clientConnection = null;
            Close();
        }

        public override void Dispose()
        {
            Rollback();
            _clientConnection = null;
            base.Dispose();
        }

        protected Returns ExecSQL(string sql)
        {
            return ExecSQL(ClientConnection, Transaction, sql, true);
        }

        protected Returns ExecSQL(string sql, bool inlog)
        {
            return ExecSQL(ClientConnection, Transaction, sql, inlog);
        }

        protected Returns ExecRead(out MyDataReader reader, string sql)
        {
            return ExecRead(ClientConnection, Transaction, out reader, sql, true);
        }

        protected object ExecScalar(string sql, out Returns ret)
        {
            return ExecScalar(ClientConnection, Transaction, sql, out ret, true);
        }

        public string GetTableFullName(string tableName)
        {
            return GetTableFullName(ClientConnection, ClientConnection.Database, tableName);
        }

        protected bool TempTableInWebCashe(string tableName)
        {
            return TempTableInWebCashe(ClientConnection, Transaction, tableName);
        }

        protected void BeginTransaction()
        {
            if (Transaction != null) return;
            Transaction = ClientConnection.BeginTransaction();
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
            return GetSerialValue(ClientConnection, Transaction);
        }
    }
}

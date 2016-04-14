using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    public partial class DbSpravKernel : DbSpravClient
    {
        public Returns SaveSupplier(Supplier finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.name_supp.Trim() == "") return new Returns(false, "Не задано наименование поставщика", -1);
            #endregion

            Returns ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;

            finder.name_supp = finder.name_supp.Trim();

#if PG
            string table = finder.pref + "_kernel.supplier";
#else
            string table = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
#endif
            string sql;
            if (finder.nzp_supp != 0)
            {
                sql = "select nzp_supp from " + table + " where nzp_supp = " + finder.nzp_supp;
                IDataReader reader;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read()) sql = "update " + table + " set name_supp = " + Utils.EStrNull(finder.name_supp) + " where nzp_supp = " + finder.nzp_supp;
                else sql = "insert into " + table + " (nzp_supp, name_supp) values (" + finder.nzp_supp + ", " + Utils.EStrNull(finder.name_supp) + ")";
                CloseReader(ref reader);
            }
            else
            {
                var db = new DbSpravKernel();
                ret = db.GetNewId(conn_db, transaction, Series.Types.Supplier);
                if (!ret.result) return ret;
                finder.nzp_supp = ret.tag;

                sql = "insert into " + table + " (nzp_supp, name_supp) values (" + finder.nzp_supp.ToString() + ", " + Utils.EStrNull(finder.name_supp) + ")";
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result) ret.tag = Convert.ToInt32(finder.nzp_supp);//GetSerialValue(conn_db);
            return ret;
        }

        public Returns SaveSupplier(Supplier finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDbTransaction transaction = conn_db.BeginTransaction();
            ret = SaveSupplier(finder, transaction, conn_db);
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }
    }
}

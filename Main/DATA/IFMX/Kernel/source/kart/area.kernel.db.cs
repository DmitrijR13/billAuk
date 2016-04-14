using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdresKernel : DbAdresClient
    {
        public Returns SaveArea(Area finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
            if (finder.area.Trim() == "") return new Returns(false, "Не задано наименование Управляющей организации");
            finder.area = finder.area.Trim();

            Returns ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;
#if PG
            string table = finder.pref + "_data.s_area";
#else
            string table = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":s_area";
#endif

            string sql;

            //поставщик
            string nzp_supp = finder.nzp_supp != 0 ? finder.nzp_supp.ToString() : "null";

            if (finder.nzp_area > 0)
            {
                sql = "select nzp_area from " + table + " where nzp_area = " + finder.nzp_area;
                IDataReader reader;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (reader.Read())
                {
                    sql = "update " + table + " set area = " + Utils.EStrNull(finder.area);
                    sql += ", nzp_supp = " + nzp_supp;
                    sql += " where nzp_area = " + finder.nzp_area;
                }
                else
                {
                    sql = "insert into " + table + " (nzp_area, area,nzp_supp) values (" + finder.nzp_area + ", " + Utils.EStrNull(finder.area) + ", " + nzp_supp + ")";
                }
                CloseReader(ref reader);
            }
            else
            {
                var db = new DbSpravKernel();
                ret = db.GetNewId(conn_db, transaction, Series.Types.Area);
                if (!ret.result)
                {
                    return ret;
                }

                finder.nzp_area = ret.tag;

                sql = "insert into " + table + " (nzp_area, area, nzp_supp) values (" + finder.nzp_area.ToString() + ", " + Utils.EStrNull(finder.area) + ", " + nzp_supp + ")";
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                ret.tag = finder.nzp_area;
            }


            return ret;
        }

        public Returns SaveArea(Area finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDbTransaction transaction = conn_db.BeginTransaction();
            ret = SaveArea(finder, transaction, conn_db);
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }
    }
}

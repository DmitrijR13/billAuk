using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdresKernel : DataBaseHeadServer
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
            Returns ret;
            using (IDbTransaction transaction = ServerConnection.BeginTransaction())
            {
                ret = SaveArea(finder, transaction, ServerConnection);
                if (ret.result) transaction.Commit();
                else transaction.Rollback();
            }
            return ret;
        }

        private Returns SaveAreaPayer(Area finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            if (finder.nzp_payer < 1) return new Returns(false, "Не задан контрагент");     
#if PG
            string table = finder.pref + "_data.s_area";
#else
            string table = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":s_area";
#endif

            string sql;
            Returns ret;

            //контрагент
            string nzp_payer = finder.nzp_payer.ToString();// != 0 ? finder.nzp_payer.ToString() : "null";

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
                    sql += ", nzp_payer = " + nzp_payer;
                    sql += " where nzp_area = " + finder.nzp_area;
                }
                else
                {
                    sql = "insert into " + table + " (nzp_area, area,nzp_payer) values (" + finder.nzp_area + ", " + Utils.EStrNull(finder.area) + ", " + nzp_payer + ")";
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

                sql = "insert into " + table + " (nzp_area, area, nzp_payer) values (" + finder.nzp_area.ToString() + ", " + Utils.EStrNull(finder.area) + ", " + nzp_payer + ")";
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                ret.tag = finder.nzp_area;
            }


            return ret;
        }

        public Returns SaveAreaPayer(Area finder)
        {
            Returns ret;
            using (IDbTransaction transaction = ServerConnection.BeginTransaction())
            {
                finder.pref = Points.Pref;
                ret = SaveAreaPayer(finder, transaction, ServerConnection);
                if (!ret.result)
                {
                    transaction.Rollback();
                    return ret;
                }

                foreach (_Point p in Points.PointList)
                {
                    finder.pref = p.pref;
                    ret = SaveAreaPayer(finder, transaction, ServerConnection);
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        return ret;
                    }
                }
                if (ret.result) transaction.Commit();
                else transaction.Rollback();
            }
            return ret;
        }


        public Returns DeleteArea(Area finder)
        {
            if (finder.nzp_area <= 0) return new Returns(false, "Не выбрана управляющая организация", -1);

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            bool nolinks = NoLinks(finder.nzp_area, conn_db, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (!nolinks)
            {
                conn_db.Close();
                return new Returns(false, "Можно удалить только те управляющие организации, на которые нет ссылок", -1);
            }

            IDbTransaction transaction = conn_db.BeginTransaction();

            ret = DeleteArea(finder, transaction, conn_db);
            if (ret.result)
            {
                //продублировать в локальные банки
                foreach (_Point p in Points.PointList)
                {
                    finder.pref = p.pref;
                    ret = DeleteArea(finder, transaction, conn_db);
                    if (!ret.result) break;
                }
            }
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }

        public Returns DeleteArea(Area finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;

#if PG
            string table = finder.pref + "_data.s_area";
#else
            string table = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":s_area";
#endif
            string sql = "delete from " + table + " where nzp_area = " + finder.nzp_area;

            ret = ExecSQL(conn_db, transaction, sql, true);
            return ret;
        }

        private bool NoLinks(int nzp_area, IDbConnection conn_db, out Returns ret)
        {
            if (!NoLinksInPrm(nzp_area, conn_db, out ret))
            {
                return false;
            }

            if (!NoLinksInDom(nzp_area, conn_db, out ret))
            {
                return false;
            }

            if (!NoLinksInKvar(nzp_area, conn_db, out ret))
            {
                return false;
            }

            return true;


        }

        private bool NoLinksInPrm(int nzp_area, IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = true;
            foreach (_Point p in Points.PointList)
            {
                res = NoLinksInPrm(nzp_area, p.pref, conn_db, out ret);
                if (!ret.result) break;
                if (!res) break;
            }
            return res;
        }

        private bool NoLinksInPrm(int nzp_area, string pref, IDbConnection conn_db, out Returns ret)
        {
            bool res = true;
            string sql = "select count(*)  from " + pref + "_data" + tableDelimiter + "prm_7 " +
                " where nzp = " + nzp_area;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка NoLinksInPrm " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            if (recordsTotalCount == 0) res = true;
            else res = false;
            return res;
        }

        private bool NoLinksInDom(int nzp_area, IDbConnection conn_db, out Returns ret)
        {
            bool res = true;
            string sql = "select count(*)  from " + Points.Pref + "_data" + tableDelimiter + "dom " +
                " where nzp_area = " + nzp_area;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка NoLinksInDom " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            if (recordsTotalCount == 0) res = true;
            else res = false;
            return res;
        }

        private bool NoLinksInKvar(int nzp_area, IDbConnection conn_db, out Returns ret)
        {
            bool res = true;
            string sql = "select count(*)  from " + Points.Pref + "_data" + tableDelimiter + "kvar " +
                " where nzp_area = " + nzp_area;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка NoLinksInKvar " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            if (recordsTotalCount == 0) res = true;
            else res = false;
            return res;
        }
    }
}

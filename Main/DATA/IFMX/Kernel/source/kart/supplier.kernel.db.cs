using System.Collections.Generic;
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

        public Returns SaveContract(ContractFinder finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            #region проверка входных параметров
            if (finder.nzp_user <= 0) return new Returns(false, "Не определен пользователь", -1);
            if (finder.nzp_payer_agent <= 0) return new Returns(false, "Не задан агент", -1);
            if (finder.nzp_payer_supp <= 0) return new Returns(false, "Не задан поставщик услуги", -1);
            if (finder.nzp_payer_princip <= 0) return new Returns(false, "Не задан принципал", -1);
            if (finder.nzp_wp.Count <= 0) return new Returns(false, "Не задан банк", -1);
            #endregion

            Returns ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;

            finder.name_supp = finder.name_supp.Trim();

#if PG
            string table_supp = finder.pref + "_kernel.supplier";
#else
            string table_supp = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
#endif
            string sql;
            if (finder.nzp_supp != 0)
            {
                sql = "select nzp_supp from " + table_supp + " where nzp_supp = " + finder.nzp_supp;
                IDataReader reader;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read())
                    sql = "update " + table_supp + " set name_supp = " + Utils.EStrNull(finder.name_supp) + 
                        ", nzp_payer_agent = " +finder.nzp_payer_agent +
                        ", nzp_payer_princip = " + finder.nzp_payer_princip +
                        ", nzp_payer_supp = " + finder.nzp_payer_supp +
                        " where nzp_supp = " + finder.nzp_supp;
                else sql = "insert into " + table_supp + " (nzp_supp, name_supp, nzp_payer_agent, nzp_payer_princip, nzp_payer_supp) " +
                    "values (" + finder.nzp_supp + ", " + Utils.EStrNull(finder.name_supp) + ","+finder.nzp_payer_agent +
                    ","+finder.nzp_payer_princip+","+finder.nzp_payer_supp +")";
                CloseReader(ref reader);
            }
            else
            {
                var db = new DbSpravKernel();
                ret = db.GetNewId(conn_db, transaction, Series.Types.Supplier);
                if (!ret.result) return ret;
                finder.nzp_supp = ret.tag;

                sql = "insert into " + table_supp + " (nzp_supp, name_supp, nzp_payer_agent, nzp_payer_princip, nzp_payer_supp) " +
                     "values (" + finder.nzp_supp + ", " + Utils.EStrNull(finder.name_supp) + "," + finder.nzp_payer_agent +
                     "," + finder.nzp_payer_princip + "," + finder.nzp_payer_supp + ")";
            }


            ret = ExecSQL(conn_db, transaction, sql, true);

            if (!ret.result) new Returns(false, "Ошибка при загрузке договора", Convert.ToInt32(finder.nzp_supp));
            //прописываем банк договору 
            if (ret.result)
            {
                ret = SaveBanksForContract(finder, transaction, conn_db);
            }

            ret.tag = Convert.ToInt32(finder.nzp_supp);
            return ret;
        }

        private Returns SaveBanksForContract(ContractFinder finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            string sql;
            Returns ret = Utils.InitReturns();
#if PG
            string table_supp_point = Points.Pref + "_kernel.supplier_point";
#else
            string table_supp_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
#endif
            string newBanks = "";
            foreach (var b in finder.nzp_wp)
            {
                newBanks += b.ToString() + ",";
            }
            if (newBanks != "")
            {
                newBanks = newBanks.Substring(0, newBanks.Length - 1);
                string where = " AND nzp_wp in (" + newBanks + ")";

                sql = "DELETE FROM " + table_supp_point + " WHERE nzp_supp = " +
                      Utils.EStrNull(finder.nzp_supp.ToString()) + @where;
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                    return ret;
            }
            foreach (int i in finder.nzp_wp)
            {
                sql = " INSERT INTO " + table_supp_point + "(nzp_supp, nzp_wp) " +
                      " VALUES ( " + Utils.EStrNull(finder.nzp_supp.ToString()) + "," + Utils.EStrNull(i.ToString()) + ")";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                    return ret;
            }

            if (finder.pref == "") finder.pref = Points.Pref;
#if PG
            string table_supp = finder.pref + "_kernel.supplier";
#else
            string table_supp = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
#endif
            if (finder.pref == Points.Pref)
            {
                sql = " UPDATE " + table_supp + "" +
                      " SET dpd = " + finder.dpd +
                      " WHERE nzp_supp = " + finder.nzp_supp;
                ret = ExecSQL(conn_db, transaction, sql, true);
            }
            return ret;
        }

        public Returns SaveContract(ContractFinder finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            if (finder.nzp_supp > 0) //редактирование
            {
                bool nolinks = NoLinksInTarif(finder.nzp_supp, conn_db, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (!nolinks)
                {
                    ret = SaveBanksForContract(finder, null, conn_db);
                    conn_db.Close();
                    return new Returns(true, "", Convert.ToInt32(finder.nzp_supp));
                }
            }

            IDbTransaction transaction = conn_db.BeginTransaction();

            ret = SaveContract(finder, transaction, conn_db);
            finder.nzp_supp = ret.tag;
            if (ret.result)
            {
                //продублировать в локальные банки
                foreach(_Point p in Points.PointList)
                {
                    finder.pref = p.pref;
                    ret = SaveContract(finder, transaction, conn_db);
                    if (!ret.result) break;
                }
            }
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }

        public List<int> BanksForOneSuppLoad(Supplier finder, out Returns ret, out bool IfCanChangePayers)
        {
            List<int> result = new List<int>();
            IfCanChangePayers = false;
            ret = new Returns();

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                IfCanChangePayers = false;
                return null;
                
            }

            try
            {
                //выбираем банки для кадого поставщика
                string sql = " SELECT nzp_wp FROM " + Points.Pref + DBManager.sKernelAliasRest + "supplier_point " +
                              " WHERE nzp_supp = " + finder.nzp_supp.ToString().Trim();
                DataTable dtLoad = DBManager.ExecSQLToTable(connDB, sql);
                foreach (DataRow r in dtLoad.Rows)
                { result.Add(Convert.ToInt32(r["nzp_wp"])); }
                IfCanChangePayers = NoLinksInTarif(finder.nzp_supp, connDB, out ret);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения списка банков для поставщика " +
                    (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                IfCanChangePayers = false;
                return null;
            }
            finally
            {
                connDB.Close();
            }
            return result;
        }
        
        private bool NoLinksInTarif(long nzp_supp, IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = true;
            foreach (_Point p in Points.PointList)
            {
                res = NoLinksInTarif(nzp_supp, p.pref, conn_db, out ret);
                if (!ret.result) break;
                if (!res) break;
            }
            return res;
        }

        private bool NoLinksInTarif(long nzp_supp, string pref, IDbConnection conn_db, out Returns ret)
        {
            bool res = true;
            string sql = "select count(*)  from "+pref + "_data"+tableDelimiter+"tarif "+ 
                " where nzp_supp = " + nzp_supp;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка CheckLinksInTarif " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);            
            }
            if (recordsTotalCount == 0) res = true;
            else res = false;
            return res;
        }

        public Returns DeleteContract(ContractFinder finder)
        {
            if (finder.nzp_supp <= 0) return new Returns(false, "Не выбран договор", -1);

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
                       
            bool nolinks = NoLinksInTarif(finder.nzp_supp, conn_db, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (!nolinks)
            {
                conn_db.Close();
                return new Returns(false, "Можно удалить только те договоры, на которые нет ссылок из перечня услуг", -1);
            }

            //проверка отсутствия в правилах удержания 
            nolinks = NoLinksInFnPercentDom(finder.nzp_supp, conn_db, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (!nolinks)
            {
                conn_db.Close();
                return new Returns(false, "Можно удалить только те договоры, на которые нет ссылок из перечня правил удержания", -1);
            }


            IDbTransaction transaction = conn_db.BeginTransaction();

            ret = DeleteContract(finder, transaction, conn_db);
            if (ret.result)
            {
                //продублировать в локальные банки
                foreach (_Point p in Points.PointList)
                {
                    finder.pref = p.pref;
                    ret = DeleteContract(finder, transaction, conn_db);
                    if (!ret.result) break;
                }
            }
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }

        private bool NoLinksInFnPercentDom(long nzpSupp, IDbConnection conn_db, out Returns ret)
        {
            bool res = true;
            string sql = 
                " SELECT count(*)" +
                " FROM " + Points.Pref + sDataAliasRest + "fn_percent_dom " +
                " WHERE nzp_supp = " + nzpSupp + " OR nzp_supp_snyat = " + nzpSupp;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка NoLinksInFnPercentDom " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            res = recordsTotalCount == 0;
            return res;
        }

        public Returns DeleteContract(ContractFinder finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;

#if PG
            string table_supp = finder.pref + "_kernel.supplier";
            string table_supp_point = Points.Pref + "_kernel.supplier_point";
#else
            string table_supp = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
            string table_supp_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier_point ";
#endif
            string sql = "delete from " + table_supp + " where nzp_supp = " + finder.nzp_supp;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                sql = "delete from " + table_supp_point + " where nzp_supp = " + finder.nzp_supp;
                ret = ExecSQL(conn_db, transaction, sql, true);
            }
            return ret;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Kernel.source.kart
{
    public class DbSupplierNew : DbSpravClient
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
            if (finder.nzp_scope <= 0) return new Returns(false, "Не задана область действия договора", -1);
            if (finder.fn_dogovor_bank_lnk_id <= 0) return new Returns(false, "Не задан расчетный счет", -1);
            #endregion

            Returns ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;
            // Колонки их значения
            Dictionary<string, string> colsValues = new Dictionary<string, string>
            {
                { "fn_dogovor_bank_lnk_id", finder.fn_dogovor_bank_lnk_id.ToString() },
                {"nzp_scope", finder.nzp_scope.ToString()}
            };
            string columns;
            string values;

#if PG
            string table_supp = finder.pref + "_kernel.supplier";
#else
            string table_supp = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
#endif
            string sql;
            // редактируется сущ. договор
            if (finder.nzp_supp != 0)
            {
                sql = "select nzp_supp from " + table_supp + " where nzp_supp = " + finder.nzp_supp;
                IDataReader reader=null;
                try
                {
                    ret = ExecRead(conn_db, transaction, out reader, sql, true);
                    if (!ret.result) return ret;
                    if (reader.Read())
                    {
                        string updateString = String.Join(",", colsValues.Select(kvp => kvp.Key + "=" + kvp.Value));
                        sql = "update " + table_supp + " set " + updateString + " where nzp_supp = " + finder.nzp_supp;
                    }
                    else
                    {
                        colsValues.Add("nzp_payer_princip", "(Select max(nzp_payer_princip) from  " + Points.Pref + sDataAliasRest + "fn_dogovor where nzp_fd=" + finder.nzp_fd + ")");
                        colsValues.Add("nzp_payer_agent", "(Select max(nzp_payer_agent) from  " + Points.Pref + sDataAliasRest + "fn_dogovor where nzp_fd=" + finder.nzp_fd + ") ");
                        colsValues.Add("nzp_supp", finder.nzp_supp.ToString());
                        colsValues.Add("name_supp", Utils.EStrNull(finder.name_supp));
                        colsValues.Add("nzp_payer_supp", finder.nzp_payer_supp.ToString());
                        if (finder.nzp_payer_podr > 0)
                        {
                            colsValues.Add("nzp_payer_podr", finder.nzp_payer_podr.ToString());
                        }
                        columns = String.Join(",", colsValues.Keys);
                        values = String.Join(",", colsValues.Values);
                        sql = "insert into " + table_supp + "(" + columns + ") Values (" + values + ")";
                    }
                }
                finally
                {
                    CloseReader(ref reader);
                }
            }
            else
            {
                //добавляется новый договор
                if (finder.nzp_payer_supp <= 0) return new Returns(false, "Не задан поставщик услуги", -1);
                var db = new DbSpravKernel();
                ret = db.GetNewId(conn_db, transaction, Series.Types.Supplier);
                if (!ret.result) return ret;
                finder.nzp_supp = ret.tag;
                colsValues.Add("nzp_supp", finder.nzp_supp.ToString());
                colsValues.Add("nzp_payer_princip", "(Select max(nzp_payer_princip) from  " + Points.Pref + sDataAliasRest + "fn_dogovor where nzp_fd=" + finder.nzp_fd + ")");
                colsValues.Add("nzp_payer_agent", "(Select max(nzp_payer_agent) from  " + Points.Pref + sDataAliasRest + "fn_dogovor where nzp_fd=" + finder.nzp_fd + ") ");
                colsValues.Add("name_supp", Utils.EStrNull(finder.name_supp));
                colsValues.Add("nzp_payer_supp", finder.nzp_payer_supp.ToString());
                if (finder.nzp_payer_podr > 0)
                {
                    colsValues.Add("nzp_payer_podr", finder.nzp_payer_podr.ToString());
                }
                columns = String.Join(",", colsValues.Keys);
                values = String.Join(",", colsValues.Values);
                sql = "insert into " + table_supp + "(" + columns + ") Values (" + values + ")";
            }
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) new Returns(false, "Ошибка при загрузке договора", Convert.ToInt32(finder.nzp_supp));
            //прописываем банк договору 
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
            string sqlBanks = "select distinct nzp_wp from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope=" + finder.nzp_scope;

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sqlBanks, true);
            if (!ret.result)
            {
                return ret;
            }
            try
            {
                finder.nzp_wp = new List<int>();
                while (reader.Read())
                {
                    if (reader["nzp_wp"] != DBNull.Value) finder.nzp_wp.Add(Convert.ToInt32(reader["nzp_wp"]));
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            sql = "DELETE FROM " + table_supp_point + " WHERE nzp_supp = " + finder.nzp_supp;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
                return ret;
            foreach (int i in finder.nzp_wp)
            {
                sql = " INSERT INTO " + table_supp_point + "(nzp_supp, nzp_wp) " +
                      " VALUES ( " + finder.nzp_supp + "," + i+ ")";
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
            IDbConnection conn_db = null;
            IDbTransaction transaction = null;
            Returns ret = Utils.InitReturns();
            try
            {
                #region подключение к базе
                string conn_kernel = Points.GetConnByPref(Points.Pref);
                conn_db = GetConnection(conn_kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return ret;
                #endregion

                transaction = conn_db.BeginTransaction();
                ret = SaveContract(finder, transaction, conn_db);
                finder.nzp_supp = ret.tag;
                if (ret.result)
                {
                    ret = SaveBanksForContract(finder, transaction, conn_db);
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        return ret;
                    }
                    //продублировать в локальные банки
                    foreach (_Point p in Points.PointList)
                    {
                        finder.pref = p.pref;
                        ret = SaveContract(finder, transaction, conn_db);
                        if (!ret.result) break;
                    }
                }
                if (ret.result)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции SaveContract() " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db!=null) conn_db.Close();
            }
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
            string sql = "select count(*)  from " + pref + "_data" + tableDelimiter + "tarif " +
                " where nzp_supp = " + nzp_supp;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка CheckLinksInTarif " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            res = recordsTotalCount == 0;
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
        #region Обновление таблицы supplier_point после изменения области действия

        public Returns RefreshBanksForContract(ScopeAdress finderScopeAdress)
        {
            Returns ret = Utils.InitReturns();
            if (finderScopeAdress.cur_nzp_scope <= 0)
            {
                ret.text = "Не уазана область действия";
                ret.result = false;
                return ret;
            }
            IDbConnection conn_db = null;
            IDbTransaction transaction = null;
            IDataReader reader = null;

            try
            {
                #region подключение к базе
                string conn_kernel = Points.GetConnByPref(Points.Pref);
                conn_db = GetConnection(conn_kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }
                #endregion
                // Извлечь договоры ЖКУ, которые имеют указанную область действия
                List<int> list_nzp_supp = getListNzpSupp(conn_db, finderScopeAdress, out ret);
                if (!ret.result)
                {
                    return ret;
                }
                // Если список договоров ЖКУ с указанной областью действия оказался пуст 
                if (list_nzp_supp == null || list_nzp_supp.Count == 0)
                {
                    return ret;
                }
#if PG
            string table_supp_point = Points.Pref + "_kernel.supplier_point";
#else
            string table_supp_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
#endif
                transaction = conn_db.BeginTransaction();
                // Удалить из supplier_point извлеченные nzp_supp
                string sql = "delete from " +  table_supp_point + " where nzp_supp in (" + String.Join(",", list_nzp_supp) + ")";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    transaction.Rollback();
                    return ret;
                }
                // извлечь все банки для указанной области действия
                sql = "select distinct nzp_wp from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope=" + finderScopeAdress.cur_nzp_scope;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    transaction.Rollback();
                    return ret;
                }
                List<int> list_nzp_wp = new List<int>();
                while (reader.Read())
                {
                    if (reader["nzp_wp"] != DBNull.Value) list_nzp_wp.Add(Convert.ToInt32(reader["nzp_wp"]));
                }
                // Вставить связку для извлеченных договоров
                foreach (int nzp_supp in list_nzp_supp)
                {
                    foreach (int nzp_wp in list_nzp_wp)
                    {
                        sql = " INSERT INTO " + table_supp_point + "(nzp_supp, nzp_wp) " +
                              " VALUES ( " + nzp_supp + "," +nzp_wp + ")";
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (ret.result) continue;
                        transaction.Rollback();
                        return ret;
                    }
                }
                transaction.Commit();
                return ret;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (conn_db != null) 
                {
                    conn_db.Close();
                }
            }
        }

        private List<int> getListNzpSupp(IDbConnection connection, ScopeAdress finderScopeAdress, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader = null;
            string sql = "select nzp_supp from " + Points.Pref + sKernelAliasRest + "supplier  where nzp_scope=" + finderScopeAdress.cur_nzp_scope;
            try
            {
                ret = ExecRead(connection, out reader, sql, true);
                if (!ret.result)
                {
                    return new List<int>();
                }
                List<int> list_nzp_supp= new List<int>();
                while (reader.Read())
                {
                    if (reader["nzp_supp"]!=DBNull.Value)
                        list_nzp_supp.Add((int)reader["nzp_supp"]);
                }
                return list_nzp_supp;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return new List<int>();
            }
            finally
            {
                if (reader!=null) reader.Close();
            }
        }

        # endregion Обновление таблицы supplier_point после изменения области действия
    }

}

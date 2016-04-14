using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.IFMX.Report.SOURCE.Samara
{
    public class SamaraGroupCalcReport : SamaraBaseReportClass
    {
        
        private readonly int _year;
        private readonly int _month;
        private readonly int _nzpUser;

        /// <summary>
        /// Получение таблицы со списом лицевых счетов
        /// </summary>
        /// <returns></returns>
        private string GetWebUserTable()
        {
            string result = String.Empty;
            if (ConnDb == null) return result;
            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            try
            {
                
                if (!DBManager.OpenDb(connWeb, true).result)
                {
                    throw new Exception("Ошибка при открытии соединения с БД, при подсчете данных для отчета");
                }
                result = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter
                          + "t" + _nzpUser + "_spls";
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog(e.Message, MonitorLog.typelog.Error, true);
                return result;
            }
            finally
            {
                connWeb.Close();
            }
            return result;
        }


        /// <summary>
        /// Получение списка префиксов БД
        /// </summary>
        /// <returns></returns>
        private List<string> GetSelKvarPref()
        {
            var result = new List<string>();  
            string webUserTable = GetWebUserTable();
            if (String.IsNullOrEmpty(webUserTable)) return result;



            if (!DBManager.OpenDb(ConnDb, true).result)
                throw new Exception("Ошибка при открытии соединения с БД, при подсчете данных для отчета");

            RunSql("drop table t_adr", true);
            string sql = " select nzp_dom,nzp_kvar, nzp_geu, nzp_area, pref from " + webUserTable +
                        " into temp t_adr with no log";
            RunSql(sql, true);
            RunSql("create index ix_t_adr01 on t_adr(nzp_kvar)", true);
            RunSql("create index ix_t_adr02 on t_adr(nzp_kvar, nzp_dom)", true);
            RunSql("create index ix_t_adr02 on t_adr(nzp_kvar, nzp_dom,nzp_geu, nzp_area)", true);
            RunSql("update statistics for table t_adr", true);
            

            DataTable tablePref = DBManager.ExecSQLToTable(ConnDb, "select pref from t_adr where pref is not null group by 1");
            result.AddRange(from DataRow dr in tablePref.Rows where dr != null select dr["pref"].ToString().Trim());
            return result;
        }

        /// <summary>
        /// Создание временных таблиц
        /// </summary>
        private void CreateTempTable(bool local)
        {
            RunSql("drop table t_calcreport" + (local ? "l" : ""), false);
            string sql = " create temp table t_calcreport" + (local ? "l" : "") + "( " +
                         " nzp_area integer," +
                         " nzp_geu integer," +
                         " nzp_serv integer," +
                         " nzp_supp integer," +
                         " nzp_frm integer," +
                         " nzp_dom integer," +
                         " tarif " + DBManager.sDecimalType + "(14,4)," +
                         " tarif_gkal " + DBManager.sDecimalType + "(14,4)," +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,4)," +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,4)," +
                         " sum_tarif " + DBManager.sDecimalType + "(14,4)," +
                         " sum_nedop " + DBManager.sDecimalType + "(14,4)," +
                         " sum_charge " + DBManager.sDecimalType + "(14,4)," +
                         " reval_d " + DBManager.sDecimalType + "(14,4)," +
                         " reval_k " + DBManager.sDecimalType + "(14,4)," +
                         " real_charge_d " + DBManager.sDecimalType + "(14,4)," +
                         " real_charge_k " + DBManager.sDecimalType + "(14,4)," +
                         " sum_money " + DBManager.sDecimalType + "(14,4)," +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,4)," +

                         " sum_insaldo_odn " + DBManager.sDecimalType + "(14,4)," +
                         " rsum_tarif_odn " + DBManager.sDecimalType + "(14,4)," +
                         " sum_tarif_odn " + DBManager.sDecimalType + "(14,4)," +
                         " sum_nedop_odn " + DBManager.sDecimalType + "(14,4)," +
                         " sum_charge_odn " + DBManager.sDecimalType + "(14,4)," +
                         " reval_d_odn " + DBManager.sDecimalType + "(14,4)," +
                         " reval_k_odn " + DBManager.sDecimalType + "(14,4)," +
                         " real_charge_d_odn " + DBManager.sDecimalType + "(14,4)," +
                         " real_charge_k_odn " + DBManager.sDecimalType + "(14,4)," +
                         " sum_money_odn " + DBManager.sDecimalType + "(14,4)," +
                         " sum_outsaldo_odn " + DBManager.sDecimalType + "(14,4)," +

                         " c_calc " + DBManager.sDecimalType + "(14,4)," +
                         " c_calc_odn " + DBManager.sDecimalType + "(14,4)," +
                         " c_calc_gkal " + DBManager.sDecimalType + "(14,4)," +
                         " c_calc_gkal_odn " + DBManager.sDecimalType + "(14,4)," +

                         " gil_calc integer," +
                         " pl_isol " + DBManager.sDecimalType + "(14,4)," +
                         " pl_komm " + DBManager.sDecimalType + "(14,4)," +
                         " pl_calc " + DBManager.sDecimalType + "(14,4)) " + DBManager.sUnlogTempTable;

            RunSql( sql, true);
            
            RunSql("drop table t_domreport" + (local ? "l" : ""), false);
            sql = " create temp table t_domreport" + (local ? "l" : "") + " ( " +
                  " nzp_area integer," +
                  " nzp_geu integer," +
                  " nzp_dom integer," +
                  " nzp_dom_base integer default 0," +
                  " pl_dom " + DBManager.sDecimalType + "(14,4)," +
                  " pl_mop " + DBManager.sDecimalType + "(14,4)) " + DBManager.sUnlogTempTable;

            RunSql(sql, true);
        }

        private void FillTempTable()
        {
            List<string> prefs = GetSelKvarPref();
            if (prefs.Count == 0) return;

            CreateTempTable(false);
            foreach (string pref in prefs)
            {
                CreateTempTable(true);
                var localBank = new SamaraCalcLocal(ConnDb, pref, _year, _month);
                localBank.SetSumNach("t_adr");
                TestSql += localBank.TestSql;
                //localBank.Close();
                DropTempTable(true);
            }
        }

        public void DropTempTable(bool local)
        {
            RunSql("drop table t_calcreport" + (local ? "l" : ""), false);
            RunSql("drop table t_domreport" + (local ? "l" : ""), false); 
        }



        public Returns PrepareTempTable()
        {
            Returns ret = Utils.InitReturns();

            GetWebUserTable();
            try
            {
                FillTempTable();
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Ошибка при подготовке данных для группового отчета " + e.Message,
                    MonitorLog.typelog.Error, true);
            }

            return ret;
        }

        public SamaraGroupCalcReport(IDbConnection connDb, int year, int month, int nzpUser) :
            base(connDb,"")
        {
            _year = year;
            _month = month;
            _nzpUser = nzpUser;
        }

    }
}

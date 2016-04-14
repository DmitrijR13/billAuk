using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using FastReport;
using System.IO;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbCounter 
    //----------------------------------------------------------------------
    {
        private string GetWhereKvar(CounterVal finder, List<Dom> houseList)
        {
            string where_kvar = "";
            
            // роли
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                        switch (role.kod)
                        {
                            case Constants.role_sql_area:
                                where_kvar += " and k.nzp_area in (" + role.val + ")";
                                break;
                            case Constants.role_sql_geu:
                                where_kvar += " and k.nzp_geu in (" + role.val + ")";
                                break;
                        }
            }
            
            // улица
            if (finder.nzp_ul > 0) where_kvar += " and d.nzp_ul = " + finder.nzp_ul;

            // дома
            string whereHouse = "";
            if (houseList != null)
            {
                if (houseList.Count > 0)
                {
                    foreach (Dom t in houseList)
                    {
                        if (whereHouse.Length > 0) whereHouse += ",";
                        whereHouse += t.nzp_dom;
                    }
                }
            }
            if (whereHouse.Length > 0) where_kvar += " and k.nzp_dom in (" + whereHouse + ")";

            // квартира
            if (finder.nkvar_po != "")
            {
                int i = Utils.GetInt(finder.nkvar_po);
                if (i > 0) where_kvar += " and k.ikvar <= " + i;

                i = Utils.GetInt(finder.nkvar);
                if (i > 0) where_kvar += " and k.ikvar >= " + i;
            }
            else
            {
                if (finder.nkvar != "") where_kvar += " and k.nkvar = " + Utils.EStrNull(finder.nkvar);
            }

            // управляющая компания
            if (finder.nzp_area > 0) where_kvar += " and k.nzp_area = " + finder.nzp_area;
            
            return where_kvar;
        }

        private string GetWhereServ(CounterVal finder, List<Dom> houseList)
        {
            string where_serv = "";

            // роли
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                        switch (role.kod)
                        {
                            case Constants.role_sql_serv:
                                where_serv += " and cs.nzp_serv in (" + role.val + ")";
                                break;
                        }
            }
            
            // услуга
            if (finder.nzp_serv > 0) where_serv += " and cs.nzp_serv = " + finder.nzp_serv;

            return where_serv;
        }

        private string GetWhereReport(CounterVal finder, List<Dom> houseList)
        {
           return GetWhereKvar(finder, houseList) + " " + GetWhereServ(finder, houseList);
        }

        private List<string> GetPrefList(CounterVal finder, List<Dom> houseList, IDbConnection conn_db)
        {
            string sql = " select distinct k.pref from " +
                Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                Points.Pref + "_data" + DBManager.tableDelimiter + "dom  d " +
                " Where k.nzp_dom = d.nzp_dom " + GetWhereKvar(finder, houseList);

            IDataReader reader;
            Returns ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return new List<string>();

            List<string> prefList = new List<string>();
            while (reader.Read())
            {
                prefList.Add(Convert.ToString(reader["pref"]).Trim());
            }

            return prefList;
        }
        
        public Returns PrepareReportPuDataRt(CounterVal finder, List<Dom> houseList)
        {
            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1); 

            // проверка даты учета
            if (finder.dat_uchet.Length <= 0) return new Returns(false, "Не задана дата начала учета", -3); 

            if (finder.dat_uchet_po.Length <= 0) return new Returns(false, "Не задана дата окончания учета", -4); 

            DateTime datUchet;
            DateTime datUchetPo;

            try
            {
                datUchet = Convert.ToDateTime(finder.dat_uchet);
            }
            catch
            {
                return new Returns(false, "Неверный формат даты начала учета", -5);
            }

            try
            {
                datUchetPo = Convert.ToDateTime(finder.dat_uchet_po);
            }
            catch
            {
                return new Returns(false, "Неверный формат даты окончания учета", -6);
            }
            //-----------------------------------------------------------------------
            #endregion

            Returns ret;
            IDataReader reader = null;

            string connKernel = Points.GetConnByPref(Points.Pref);
            IDbConnection connDB = GetConnection(connKernel);

            ret = OpenDb(connDB, true);
            if (!ret.result) return new Returns(false, "Не удалось установить подключение", -8);

            string roleClause = GetWhereKvar(finder, houseList);
            string servClause = GetWhereServ(finder, houseList);

            List<string> prefList = GetPrefList(finder, houseList, connDB);
            
            var table = new DataTable {TableName = "Q_master"};

            table.Columns.Add("nzp_ul", typeof(Int64));
            table.Columns.Add("nzp_dom", typeof(Int64));
            table.Columns.Add("geu", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("ndom", typeof(string));
            table.Columns.Add("nkor", typeof(string));
            table.Columns.Add("num_ls", typeof(string));
            table.Columns.Add("nkvar", typeof(string));
            table.Columns.Add("nkvar_n", typeof(string));
            table.Columns.Add("num_cnt", typeof(string));
            table.Columns.Add("name_type", typeof(string));
            table.Columns.Add("cnt_stage", typeof(string));
            table.Columns.Add("mmnog", typeof(string));
            table.Columns.Add("service", typeof(string));
            table.Columns.Add("measure", typeof(string));
            table.Columns.Add("dat_uchet", typeof(string));
            table.Columns.Add("val_cnt", typeof(string));
            table.Columns.Add("dat_uchet_pred", typeof(string));
            table.Columns.Add("val_cnt_pred", typeof(string));
            table.Columns.Add("rashod", typeof(decimal));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("ngp_cnt", typeof(decimal));

            var cv = new CounterVal();

            foreach (string curPref in prefList)
            {
                #region сформировать sql

                ExecSQL(connDB, "drop table sel_kvar", false);
                string sql = " Create temp table sel_kvar (nzp_kvar integer)" + sUnlogTempTable;
                ExecSQL(connDB, sql, false);

                sql = "insert into sel_kvar " +
                      " select nzp_kvar " +
                      " from " + curPref + sDataAliasRest + "kvar k, " +
                      curPref + sDataAliasRest + "dom d," +
                      curPref + sDataAliasRest + "s_ulica u " + 
                      " where k.nzp_dom=d.nzp_dom " +
                      " and d.nzp_ul=u.nzp_ul " + 
                      " " + roleClause +
                      " and (select count(*) from " + curPref + sDataAliasRest + "prm_3 p3 " +
                      " where p3.nzp_prm = 51 " +
                      " and p3.val_prm ='1' " +
                      " and " + sCurDateTime + " between p3.dat_s " +
                      " and p3.dat_po and p3.nzp = k.nzp_kvar " +
                      " and p3.is_actual <> 100) > 0 ";
                ExecSQL(connDB, sql, true);

                ret = ExecSQL(connDB, "create index ix_tmpsk_01 on sel_kvar(nzp_kvar)", false);
                ret = ExecSQL(connDB, DBManager.sUpdStat + " sel_kvar", false);

                ret = ExecSQL(connDB, "drop table t_couns", false);

                sql = " Create temp table t_couns(" +
                      " nzp_kvar integer," +
                      " nzp_counter integer," +
                      " nzp_serv integer," +
                      " num_cnt char(20), " +
                      " cnt_stage integer," +
                      " mmnog " + sDecimalType + "(8,4)," +
                      " measure char(20)," +
                      " name_type char(40)," +
                      " ngp_cnt " + sDecimalType + "(14,4)," +
                      " val_cnt " + sDecimalType + "(14,4)," +
                      " val_cnt_pred " + sDecimalType + "(14,4)," +
                      " val_cnt_pred2 " + sDecimalType + "(14,4)," +
                      " dat_uchet Date, " +
                      " dat_uchet_pred Date, " +
                      " dat_uchet_pred2 Date " +
                      ")" + sUnlogTempTable;
                ret = ExecSQL(connDB, sql, true);

                sql = "insert into t_couns (nzp_kvar, nzp_serv, nzp_counter, num_cnt, name_type, cnt_stage, mmnog, measure,  " +
                      " ngp_cnt, dat_uchet)" +
                    " Select cs.nzp, cs.nzp_serv, cs.nzp_counter, cs.num_cnt, t.name_type, t.cnt_stage, t.mmnog, m.measure,  " +
                    (Points.IsIpuHasNgpCnt ? " a.ngp_cnt" : "0") + " , max(a.dat_uchet) as dat_uchet " +
                    " From " +
                    curPref + sDataAliasRest + "counters_spis cs, " +
                    curPref + sKernelAliasRest + "s_counts cc, " +
                    curPref + sKernelAliasRest + "s_counttypes t, " +
                    curPref + sKernelAliasRest + "s_measure m, " +
                    curPref + sDataAliasRest + "counters a, sel_kvar k " +
                    " Where cs.nzp=k.nzp_kvar " +
                    " and  cs.nzp_cnttype = t.nzp_cnttype " +
                    " and cs.nzp_serv = cc.nzp_serv " +
                    " and cc.nzp_measure = m.nzp_measure " +
                    " and a.nzp_counter = cs.nzp_counter " +
                    " and cs.nzp_type = 3 " + // квартирные ПУ
                    // только открытые лицевые счета
                    " and a.dat_uchet between " + Utils.EStrNull(datUchet.AddDays(1).ToShortDateString()) +
                    " and " + Utils.EStrNull(datUchetPo.AddMonths(1).ToShortDateString()) +
                    " and a.val_cnt is not null and a.is_actual <> 100 " +
                    "  " + servClause;
                // только новые ПУ
                if (finder.is_new == 1)
                {
                    sql += " and (select min(cd.dat_uchet) from " + curPref + sDataAliasRest + "counters cd " +
                           " where cd.nzp_counter = cs.nzp_counter " +
                           " and cd.is_actual <> 100) between '" + datUchet.ToShortDateString() + "'" +
                           " and " + "'" + datUchet.AddMonths(1).ToShortDateString() + "'";
                }
                sql += " group by 1,2,3,4,5,6,7,8,9 ";
                ret = ExecSQL(connDB, sql, true);

                sql = "update t_couns set val_cnt=(select max(b.val_cnt) " +
                 " from " + curPref + sDataAliasRest + " counters b" +
                 " where t_couns.nzp_counter=b.nzp_counter " +
                 " and b.dat_uchet = t_couns.dat_uchet " +
                 " and b.val_cnt is not null and b.is_actual <> 100)";
                ret = ExecSQL(connDB, sql, true);

                //sql = "update t_couns set dat_uchet_pred=(select max(dat_uchet) " +
                //    " from " + curPref + sDataAliasRest + " counters b" +
                //    " where t_couns.nzp_counter=b.nzp_counter " +
                //    " and b.dat_uchet <= " + Utils.EStrNull(datUchet.ToShortDateString()) +
                //    " and b.val_cnt is not null and b.is_actual <> 100)";
                //ExecSQL(connDB, sql, true);


                sql = "update t_couns set dat_uchet_pred=(select max(dat_uchet) " +
                    " from " + curPref + sDataAliasRest + " counters b" +
                    " where t_couns.nzp_counter=b.nzp_counter " +
                    " and b.dat_uchet < t_couns.dat_uchet " + 
                    " and b.val_cnt is not null and b.is_actual <> 100)";
                ret = ExecSQL(connDB, sql, true);


                sql = "update t_couns set val_cnt_pred=(select max(b.val_cnt) " +
                      " from " + curPref + sDataAliasRest + " counters b" +
                      " where t_couns.nzp_counter=b.nzp_counter " +
                      " and b.dat_uchet = t_couns.dat_uchet_pred " +
                      " and b.val_cnt is not null and b.is_actual <> 100) where dat_uchet_pred is not null";
                ret = ExecSQL(connDB, sql, true);

                //#region Цель запроса неясна
                //sql = "update t_couns set dat_uchet_pred2=(select min(t.dat_uchet) " +
                //      " from " + curPref + sDataAliasRest + "counters t " +
                //      " where t.nzp_counter = t_couns.nzp_counter " +
                //      " and t.dat_uchet between " + Utils.EStrNull(datUchet.AddDays(1).ToShortDateString()) +
                //      " and " + Utils.EStrNull(datUchet.AddMonths(1).ToShortDateString()) +
                //      " and t.val_cnt is not null and t.is_actual <> 100)";
                //ExecSQL(connDB, sql, true);


                //sql = " update t_couns set val_cnt_pred2=(select min(t.val_cnt) " +
                //      " from " + curPref + sDataAliasRest + "counters t " +
                //      " where t.nzp_counter = t_couns.nzp_counter " +
                //      " and t.dat_uchet =  t_couns.dat_uchet_pred2 " +
                //      " and t.val_cnt is not null and t.is_actual <> 100) where dat_uchet_pred is not null";
                //ExecSQL(connDB, sql, true);
                //#endregion

                sql = " Select g.geu, u.ulica, d.idom, d.ndom, d.nkor, k.ikvar, k.nkvar, " +
                      "       k.nkvar_n, u.nzp_ul, d.nzp_dom, cs.nzp_counter, " +
                      (Points.IsSmr
                          ? "substr(pkod, 6, 5) || ' ' || case when substr(pkod, 11, 1) = '0' then '' else substr(pkod, 11, 1) end as num_ls"
                          : "k.num_ls") +
                      ", k.fio, cs.num_cnt, cs.name_type, cs.cnt_stage, cs.mmnog, cs.measure, s.service, cs.ngp_cnt" +
                      //", val_cnt_pred2, dat_uchet_pred2 " +
                      ", val_cnt_pred, dat_uchet_pred " +
                      ", val_cnt, dat_uchet " +
                      " From t_couns cs, " +
                      Points.Pref + sDataAliasRest + "kvar k, " +
                      Points.Pref + sDataAliasRest + "dom d, " +
                      Points.Pref + sDataAliasRest + " s_ulica u, " +
                      curPref + sKernelAliasRest + " services s, " +
                      curPref + sDataAliasRest + "s_geu g " +
                      " Where cs.nzp_kvar = k.nzp_kvar " +
                      " and k.nzp_dom = d.nzp_dom " +
                      " and d.nzp_ul = u.nzp_ul " +
                      " and cs.nzp_serv = s.nzp_serv " +
                      " and k.nzp_geu = g.nzp_geu " +
                      " order by 1,2,3,4,5,6,7,8, service, num_cnt, dat_uchet ";
          

                //----------------------------------------------------------------------------
                #endregion

                //записать текст sql в лог-журнал
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    reader.Close();
                    ExecSQL(connDB, "drop table sel_kvar", false);
                    ExecSQL(connDB, "drop table t_couns", false);
                    connDB.Close();
                    prefList.Clear();
                    return new Returns(false, "Не удалось выполнить запрос");
                }

                while (reader.Read())
                {
                    if (reader["nzp_ul"] != DBNull.Value) cv.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["nzp_dom"] != DBNull.Value) cv.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["geu"] != DBNull.Value) cv.geu = Convert.ToString(reader["geu"]).Trim();
                    if (reader["ulica"] != DBNull.Value) cv.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ndom"] != DBNull.Value) cv.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) cv.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["num_ls"] != DBNull.Value) cv.pkod = Convert.ToString(reader["num_ls"]).Trim();
                    if (reader["nkvar"] != DBNull.Value) cv.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                    if (reader["nkvar_n"] != DBNull.Value) cv.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                    if (reader["num_cnt"] != DBNull.Value) cv.num_cnt = Convert.ToString(reader["num_cnt"]).Trim();
                    if (reader["name_type"] != DBNull.Value) cv.name_type = Convert.ToString(reader["name_type"]).Trim();
                    if (reader["cnt_stage"] != DBNull.Value) cv.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);
                    if (reader["mmnog"] != DBNull.Value) cv.mmnog = Convert.ToDecimal(reader["mmnog"]);
                    if (reader["service"] != DBNull.Value) cv.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["measure"] != DBNull.Value) cv.measure = Convert.ToString(reader["measure"]).Trim();
                    if (reader["fio"] != DBNull.Value) cv.fio = Convert.ToString(reader["fio"]).Trim();

                    cv.fio = cv.fio.ToLower();
                    int k = cv.fio.IndexOf(" ", StringComparison.Ordinal);
                    if (k > 0) cv.fio = cv.fio.Substring(0, k) + " " + cv.fio.Substring(k + 1, 1).ToUpper() + cv.fio.Substring(k + 2);
                    k = cv.fio.LastIndexOf(" ", StringComparison.Ordinal);
                    if (k > 0) cv.fio = cv.fio.Substring(0, k) + " " + cv.fio.Substring(k + 1, 1).ToUpper() + cv.fio.Substring(k + 2);
                    if (cv.fio.Length > 0) cv.fio = cv.fio.Substring(0, 1).ToUpper() + cv.fio.Substring(1);

                    // cпециально для Лениногорска, в котором нет разделителей между инициалами
                    k = cv.fio.LastIndexOf(".", StringComparison.Ordinal);
                    if (k > 0) cv.fio = cv.fio.Substring(0, k - 1) + cv.fio.Substring(k - 1, 1).ToUpper() + cv.fio.Substring(k);

                    cv.dat_uchet = "";
                    cv.dat_uchet_pred = "";
                    cv.val_cnt = 0;
                    cv.val_cnt_pred = 0;

                    if (reader["dat_uchet"] != DBNull.Value) cv.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                    if (reader["val_cnt"] != DBNull.Value) cv.val_cnt = Convert.ToDecimal(reader["val_cnt"]);

                    if (reader["dat_uchet_pred"] != DBNull.Value) cv.dat_uchet_pred = Convert.ToDateTime(reader["dat_uchet_pred"]).ToShortDateString();
                    if (reader["val_cnt_pred"] != DBNull.Value) cv.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred"]);

                    if (cv.val_cnt_pred < 0)
                    {
                        //if (reader["val_cnt_pred2"] != DBNull.Value) cv.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred2"]);
                    }

                    if (cv.dat_uchet_pred == "")
                    {
                        //if (reader["dat_uchet_pred2"] != DBNull.Value) cv.dat_uchet_pred = Convert.ToDateTime(reader["dat_uchet_pred2"]).ToShortDateString();
                    }

                    if (Points.IsIpuHasNgpCnt)
                    {
                        if (reader["ngp_cnt"] != DBNull.Value) cv.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
                    }

                    if (cv.dat_uchet != "")
                    {
                        table.Rows.Add(
                            cv.nzp_ul,
                            cv.nzp_dom,
                            cv.geu,
                            cv.ulica,
                            cv.ndom,
                            cv.nkor,
                            cv.pkod,
                            cv.nkvar,
                            cv.nkvar_n,
                            cv.num_cnt,
                            cv.name_type,
                            cv.cnt_stage,
                            cv.mmnog.ToString(".####"),
                            cv.service,
                            cv.measure,
                            cv.dat_uchet,
                            cv.val_cnt.ToString("0.####"),
                            cv.dat_uchet_pred,
                            cv.val_cnt_pred.ToString("0.####"),
                            cv.calculatedRashod,
                            cv.fio,
                            cv.ngp_cnt
                        );
                    }
                }
            }

            if (reader != null) reader.Close();
            ExecSQL(connDB, "drop table sel_kvar", false);
            ExecSQL(connDB, "drop table t_couns", false);


            connDB.Close();
            prefList.Clear();

            var fDataSet = new DataSet();
            fDataSet.Tables.Add(table);

            var report = new Report();

            report.Load(PathHelper.GetReportTemplatePath("pu_data.frx"));

            //report.Load(@"template/pu_data.frx");

            // дом
            string pdom = "<Все>";
            if (finder.ndom.Length > 0 || finder.ndom_po.Length > 0)
            {
                if (finder.ndom == finder.ndom_po || finder.ndom.Length > 0 && finder.ndom_po.Length == 0) pdom = finder.ndom;
                else
                {
                    if (finder.ndom.Length > 0) pdom = "c " + finder.ndom;
                    if (finder.ndom_po.Length > 0) pdom += " по " + finder.ndom_po;
                }
            }

            // месяц
            string pmonth;

            if (finder.dat_uchet == finder.dat_uchet_po) pmonth = datUchet.ToString("MMMM yyyy").ToLower() + " г.";
            else pmonth = datUchet.ToString("MMMM yyyy").ToLower() + " г." + " - " + datUchetPo.ToString("MMMM yyyy").ToLower() + " г.";

            report.SetParameterValue("pulica", finder.ulica);
            report.SetParameterValue("pservice", finder.service);
            report.SetParameterValue("pdom", pdom);
            report.SetParameterValue("pmonth", pmonth);
            report.SetParameterValue("isSmr", (Points.IsSmr ? "1" : "0"));
            report.SetParameterValue("parea", finder.area);

            report.SetParameterValue("pcounter", finder.is_new == 1 ? "Новые" : "Все");

            report.RegisterData(fDataSet);
            report.GetDataSource("Q_master").Enabled = true;
            report.Prepare();

            try
            {
                string path = InputOutput.useFtp ? InputOutput.GetOutputDir() : Constants.ExcelDir;

                string destinationFilename = DateTime.Now.Ticks + "_" + finder.nzp_user + "_pu_data.fpx";
                // директория
                ret.text = destinationFilename;
                report.SavePrepared(Path.Combine(path, destinationFilename));
                if (InputOutput.useFtp) ret.text = InputOutput.SaveOutputFile(Path.Combine(path, destinationFilename));
            }
            catch (Exception ex)
            {
                ret.text = "";
                ret.result = false;
                MonitorLog.WriteLog("Ошибка формирования отчета \"Данные приборов учета по жилым домам\" " + ex.Message, MonitorLog.typelog.Error, 20, 401, true);
            }
            return ret;
        }

    }
}

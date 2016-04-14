using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Samara.Properties;

namespace Bars.KP50.Report.Samara.Reports
{
    public class ReportCounters : BaseSqlReport
    {
        public override string Name
        {
            get { return "Сведения о расходах по приборам учета"; }
        }

        public override string Description
        {
            get { return "Сведения о расходах по приборам учета"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.ReportCounters; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }


        /// <summary>Расчетный месяц</summary>
        private int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        private int YearS { get; set; }

        /// <summary>Расчетный месяц</summary>
        private int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        private int YearPo { get; set; }

        /// <summary>Услуги</summary>
        private List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        private List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string AreaHeader { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }

        /// <summary>
        /// Дата начала периода
        /// </summary>
        private DateTime DatUchet { get; set; }
        
        /// <summary>
        /// Дата окончания периода
        /// </summary>
        private DateTime DatUchetPo { get; set; }

        // только новые ПУ
        private bool IsNew { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AreaParameter(),
                new ComboBoxParameter
                {
                    
                    Code = "isNew",
                    Name = "Только новые ПУ",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "Нет"},
                        new {Id = "2", Name = "Да"},
                    }
                }
            };
        }

        public override DataSet GetData()
        {
            string servClause = GetWhereServ("a.");

            List<string> prefList = GetPrefList();
            GetSelectedKvars();

            var table = new DataTable { TableName = "Q_master" };

            table.Columns.Add("nzp_ul", typeof(Int64));
            table.Columns.Add("nzp_dom", typeof(Int64));
            table.Columns.Add("geu", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("ndom", typeof(string));
            table.Columns.Add("nkor", typeof(string));
            table.Columns.Add("entrance", typeof(string));
            table.Columns.Add("num_ls", typeof(string));
            table.Columns.Add("nkvar", typeof(string));
            table.Columns.Add("nkvar_n", typeof(string));
            table.Columns.Add("num_cnt", typeof(string));
            table.Columns.Add("name_type", typeof(string));
            table.Columns.Add("cnt_stage", typeof(string));
            table.Columns.Add("mmnog", typeof(string));
            table.Columns.Add("service", typeof(string));
            table.Columns.Add("dat_close", typeof(string));
            table.Columns.Add("measure", typeof(string));
            table.Columns.Add("dat_uchet", typeof(string));
            table.Columns.Add("val_cnt", typeof(string));
            table.Columns.Add("dat_uchet_pred", typeof(string));
            table.Columns.Add("val_cnt_pred", typeof(string));
            table.Columns.Add("rashod", typeof(decimal));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("ngp_cnt", typeof(decimal));

            var cv = new STCLINE.KP50.Interfaces.CounterVal();

            foreach (string curPref in prefList)
            {
                #region сформировать sql


                string sql = " Create temp table t_couns(" +
                      " nzp_kvar integer," +
                      " entrance integer," +
                      " nzp_counter integer," +
                      " nzp_serv integer," +
                      " num_cnt char(20), " +
                      " cnt_stage integer," +
                      " mmnog " + DBManager.sDecimalType + "(8,4)," +
                      " measure char(20)," +
                      " name_type char(40)," +
                      " ngp_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt_pred " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt_pred2 " + DBManager.sDecimalType + "(14,4)," +
                      " dat_uchet Date, " +
                      " dat_close Date, " +
                      " dat_uchet_pred Date, " +
                      " dat_uchet_pred2 Date " +
                      ")" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                sql = "Create temp table selected_kvar (nzp_kvar integer, ikvar integer)" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                if (!String.IsNullOrEmpty(SupplierHeader))
                {
                    sql =  " insert into selected_kvar (nzp_kvar, ikvar) "+
                           " select a.nzp_kvar, a.ikvar " +
                           " from " + curPref + DBManager.sDataAliasRest + "tarif a, sel_kvar b" +
                           " where is_actual=1  and a.nzp_kvar=b.nzp_kvar " +
                           " and a.dat_uchet between " +
                           STCLINE.KP50.Global.Utils.EStrNull(DatUchet.AddDays(1).ToShortDateString()) +
                           " and " + STCLINE.KP50.Global.Utils.EStrNull(DatUchetPo.AddMonths(1).ToShortDateString()) +
                           "  " + GetWhereSupp("")+ servClause;
                    ExecSQL(sql);
                }
                else
                {
                    sql = " insert into selected_kvar (nzp_kvar, ikvar) " +
                          " select nzp_kvar, ikvar  from sel_kvar ";
                    ExecSQL(sql);
                }
                ExecSQL("create index ix_tskvs_01 on selected_kvar (nzp_kvar)");
                ExecSQL(DBManager.sUpdStat + " selected_kvar");

                sql = "insert into t_couns (nzp_kvar, entrance, nzp_serv,  nzp_counter, num_cnt, name_type, cnt_stage, mmnog, measure,  " +
                      " ngp_cnt, dat_uchet_pred, dat_close)" +
                    " Select cs.nzp, CASE WHEN k.ikvar <= 128 THEN 1 ELSE 2 END as entrance, cs.nzp_serv, cs.nzp_counter, cs.num_cnt, t.name_type, t.cnt_stage, t.mmnog, m.measure,  " +
                    " 0 , max(a.dat_uchet) as dat_uchet_pred, cs.dat_close as dat_close " +
                    " From " +
                    curPref + DBManager.sDataAliasRest + "counters_spis cs, " +
                    curPref + DBManager.sKernelAliasRest + "s_counts cc, " +
                    curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                    curPref + DBManager.sKernelAliasRest + "s_measure m, " +
                    curPref + DBManager.sDataAliasRest + "counters a, selected_kvar k " +
                    " Where cs.nzp=k.nzp_kvar " +
                    " and  cs.nzp_cnttype = t.nzp_cnttype " +
                    " and cs.nzp_serv = cc.nzp_serv " +
                    " and cc.nzp_measure = m.nzp_measure " +
                    " and a.nzp_counter = cs.nzp_counter " +
                    " and cs.nzp_type = 3 " + // квартирные ПУ
                    // только открытые лицевые счета
                    " and a.dat_uchet between " + STCLINE.KP50.Global.Utils.EStrNull(DatUchet.AddDays(1).AddMonths(-1).ToShortDateString()) +
                    " and " + STCLINE.KP50.Global.Utils.EStrNull(DatUchetPo.AddMonths(-1).ToShortDateString()) +
                    " and a.val_cnt is not null and a.is_actual <> 100 " +
                    "  " + servClause;
               

                // только новые ПУ
                if (IsNew)
                {
                    sql += " and (select min(cd.dat_uchet) from " + curPref + DBManager.sDataAliasRest + "counters cd " +
                           " where cd.nzp_counter = cs.nzp_counter " +
                           " and cd.is_actual <> 100) between '" + DatUchet.ToShortDateString() + "'" +
                           " and " + "'" + DatUchet.AddMonths(1).ToShortDateString() + "'";
                }
                sql += " group by 1,2,3,4,5,6,7,8,9 ";
                ExecSQL(sql);

                sql = "update t_couns set val_cnt_pred=(select max(b.val_cnt) " +
                 " from " + curPref + DBManager.sDataAliasRest + " counters b" +
                 " where t_couns.nzp_counter=b.nzp_counter " +
                 " and b.dat_uchet = t_couns.dat_uchet_pred " +
                 " and b.val_cnt is not null and b.is_actual <> 100)";
                ExecSQL(sql);


                sql = "update t_couns set dat_uchet=(select max(dat_uchet) " +
                    " from " + curPref + DBManager.sDataAliasRest + " counters b" +
                    " where t_couns.nzp_counter=b.nzp_counter " +
                    " and b.dat_uchet > t_couns.dat_uchet_pred " +
                    " and b.val_cnt is not null and b.is_actual <> 100 and b.dat_uchet <= '" + DatUchetPo.ToShortDateString() + "')";
                ExecSQL(sql);


                sql = "update t_couns set val_cnt=(select max(b.val_cnt) " +
                      " from " + curPref + DBManager.sDataAliasRest + " counters b" +
                      " where t_couns.nzp_counter=b.nzp_counter " +
                      " and b.dat_uchet = t_couns.dat_uchet " +
                      " and b.val_cnt is not null and b.is_actual <> 100) where dat_uchet_pred is not null";
                ExecSQL(sql);


                sql = " Select g.geu, u.ulica, d.idom, d.ndom, d.nkor, k.ikvar, k.nkvar, " +
                      "       k.nkvar_n, u.nzp_ul, d.nzp_dom, entrance, cs.nzp_counter, k.num_ls" +
                      ", k.fio, cs.num_cnt, cs.name_type, cs.cnt_stage, cs.mmnog, cs.measure, s.service, cs.dat_close, cs.ngp_cnt " +
                      ", val_cnt_pred, dat_uchet_pred " +
                      ", val_cnt, dat_uchet " +
                      " From t_couns cs, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + " s_ulica u, " +
                      curPref + DBManager.sKernelAliasRest + " services s, " +
                      curPref + DBManager.sDataAliasRest + "s_geu g " +
                      " Where cs.nzp_kvar = k.nzp_kvar " +
                      " and k.nzp_dom = d.nzp_dom " +
                      " and d.nzp_ul = u.nzp_ul " +
                      " and cs.nzp_serv = s.nzp_serv " +
                      " and k.nzp_geu = g.nzp_geu " +
                      " order by 1,2,3,4,entrance,5,6,7,8, service, num_cnt, dat_uchet";


                //----------------------------------------------------------------------------
                #endregion

                //записать текст sql в лог-журнал
                MyDataReader reader;
                ExecRead(out reader, sql);
                while (reader.Read())
                {
                    Int32 entrance = 0;
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
                    if (reader["entrance"] != DBNull.Value) entrance = Convert.ToInt32(reader["entrance"]);
                    if (reader["dat_close"] != DBNull.Value)
                        cv.dat_close = Convert.ToDateTime(reader["dat_close"]).ToShortDateString(); 
                    else 
                        cv.dat_close = "";
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



                    if (cv.dat_uchet != "11111111")
                    {
                        table.Rows.Add(
                            cv.nzp_ul,
                            cv.nzp_dom,
                            cv.geu,
                            cv.ulica,
                            cv.ndom,
                            cv.nkor,
                            entrance,
                            cv.pkod,
                            cv.nkvar,
                            cv.nkvar_n,
                            cv.num_cnt,
                            cv.name_type,
                            cv.cnt_stage,
                            cv.mmnog.ToString(".####"),
                            cv.service,
                            cv.dat_close,
                            cv.measure,
                            cv.dat_uchet,
                            cv.val_cnt != 0 ? cv.val_cnt.ToString("0.####") : "",
                            cv.dat_uchet_pred,
                            cv.val_cnt_pred != 0 ? cv.val_cnt_pred.ToString("0.####") : "",
                            cv.calculatedRashod,
                            cv.fio,
                            cv.ngp_cnt
                        );
                    }
                    
                }
                reader.Close();
                ExecSQL("drop table selected_kvar");
                ExecSQL("drop table t_couns");
            }


            var fDataSet = new DataSet();
            fDataSet.Tables.Add(table);


            return fDataSet;
        }

        /// <summary>
        /// Получить список выбранных квартир
        /// </summary>
        private void GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into sel_kvar (nzp_kvar, ikvar, nzp_area) " +
                                     " select nzp_kvar, ikvar, nzp_area from " + tSpls;
                        ExecSQL(sql);
                    }
                }
            }
            else
            {

                string sql = " insert into sel_kvar (nzp_kvar, ikvar, nzp_area) " +
                             " select nzp_kvar, ikvar, nzp_area " +
                             " from " + ReportParams.Pref + DBManager.sDataAliasRest +"kvar "+
                             " Where nzp_kvar>0 " +
                             GetWhereArea("") +
                             GetWhereWp();
                                     
                ExecSQL(sql);
            }
            ExecSQL("create index ix_tskvs_02 on sel_kvar (nzp_kvar)");
            ExecSQL(DBManager.sUpdStat + " sel_kvar");

        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea(string tablePrefix)
        {
            string whereArea = String.Empty;
            whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND " + tablePrefix + "nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') + " WHERE nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string tablePrefix)
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND " + tablePrefix + "nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " + 
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ(string tablePrefix)
        {
            
            string whereServ = String.Empty;
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND " + tablePrefix + "nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                string sql = " SELECT service " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a" +
                             " WHERE a.nzp_serv > 1 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
            }
            return whereServ;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS);
           
            
            

            // дом
            string pdom;
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                pdom = "По списку ЛС";
            else
                pdom = "";
            // месяц
            string pmonth;

            if (DatUchet == DatUchetPo) pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г.";
            else pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г." 
                + " - " + DatUchetPo.ToString("MMMM yyyy").ToLower() + " г.";
            report.SetParameterValue("psupplier", SupplierHeader);
            report.SetParameterValue("pservice", ServiceHeader);
            report.SetParameterValue("pdom", pdom);
            report.SetParameterValue("pmonth", pmonth);
            report.SetParameterValue("isSmr",  "0");
            report.SetParameterValue("parea", AreaHeader);
            report.SetParameterValue("pcounter", IsNew ? "Новые" : "Все");

        }
        
        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            if (MonthS == 12)
            {
                MonthPo = 1;
                YearPo = YearS + 1;
            }
            else
            {
                MonthPo = MonthS + 1;
                YearPo = YearS;
            }
            DatUchet = new DateTime(YearS, MonthS, 1);
            DatUchetPo = new DateTime(YearPo, MonthPo, 1);
            
            Services = UserParamValues["Services"].Value.To<List<int>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            IsNew = UserParamValues["isNew"].Value.To<int>() == 2;

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
    
        }
        protected override void CreateTempTable()
        {
                string sql = " create temp table sel_kvar(" +
                    " nzp_kvar integer, " +
                    " ikvar integer, " +
                    " nzp_geu integer, " +
                    " nzp_area integer) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);
            
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table sel_kvar ", true);
        }


        /// <summary>
        /// Получение префиксов БД
        /// </summary>
        /// <returns></returns>
        private List<string> GetPrefList()
        {
            var prefList = new List<string>();
            MyDataReader reader; 
            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point "+
                         " where nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
               prefList.Add(Convert.ToString(reader["pref"]).Trim());
            }
            reader.Close();
            return prefList;
        }

     
    }
}

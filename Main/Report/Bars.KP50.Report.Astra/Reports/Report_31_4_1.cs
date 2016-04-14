using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Astra.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Astra.Reports
{
    public class Report3141 : BaseSqlReport
    {
        public override string Name
        {
            get { return "31.4.1 Отчетная форма по индивидуальным приборам учета"; }
        }

        public override string Description
        {
            get { return "31.4.1 Отчетная форма по индивидуальным приборам учета"; }
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
            get { return Resources.Report_31_4_1; }
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

        /// <summary> Дата начала периода </summary>
        private DateTime DatUchet { get; set; }
        
        /// <summary> Дата окончания периода </summary>
        private DateTime DatUchetPo { get; set; }
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AreaParameter(),
            };
        }

        public override DataSet GetData()
        {
            string servClause = GetWhereServ("a.");

            List<string> prefList = GetPrefList();
            GetSelectedKvars();

            string sql;
            foreach (string curPref in prefList)
            {
                sql = "Create temp table selected_kvar (nzp_kvar integer)" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                if (!String.IsNullOrEmpty(SupplierHeader))
                {
                    sql =  " insert into selected_kvar (nzp_kvar) "+
                           " select a.nzp_kvar " +
                           " from " + curPref + DBManager.sDataAliasRest + "tarif a, sel_kvar b" +
                           " where is_actual=1  and a.nzp_kvar=b.nzp_kvar " +
                           " and a.dat_uchet between " +
                           STCLINE.KP50.Global.Utils.EStrNull(DatUchet.AddDays(1).ToShortDateString()) +
                           " and " + STCLINE.KP50.Global.Utils.EStrNull(DatUchetPo.AddMonths(1).ToShortDateString()) +
                           "  " + GetWhereSupp("")+ servClause+
                           "group by 1";
                    ExecSQL(sql);
                }
                else
                {
                    sql = " insert into selected_kvar (nzp_kvar) " +
                          " select nzp_kvar  from sel_kvar ";
                    ExecSQL(sql);
                }
                ExecSQL("create index ix_tskvs_01 on selected_kvar (nzp_kvar)");
                ExecSQL(DBManager.sUpdStat + " selected_kvar");

                for (var cur_dt = DatUchet; cur_dt <= DatUchetPo; cur_dt=cur_dt.AddMonths(1))
                {
                    sql = "Create temp table t_sum( " +
                          " nzp_count INTEGER, " +
                          " month_ integer," +
                          " num_ls integer," +
                          "sum_money " + DBManager.sDecimalType + "(14,2))" +
                          DBManager.sUnlogTempTable;
                    ExecSQL(sql);

                    sql = "Create temp table t_couns(" +
                            " month_ INTEGER, " +
                            " num_cnt char(20)," +
                            " num_ls integer," +
                            " val_cnt " + DBManager.sDecimalType + "(14,2))" +
                            DBManager.sUnlogTempTable;
                    ExecSQL(sql);
                    
                    string sqlsum = "insert into t_sum (month_ ,num_ls, sum_money) " +
                                    "select  "+ cur_dt.ToString("MM")  +", num_ls, sum(sum_money)" +
                                    "From selected_kvar sk," + curPref + "_charge_" + cur_dt.ToString("yy") + DBManager.tableDelimiter + "charge_" + cur_dt.ToString("MM") + " a " +
                                    "Where a.dat_charge is null and sk.nzp_kvar=a.nzp_kvar " + servClause +
                                    "group by num_ls";
                    ExecSQL(sqlsum);
                    var d1 = ExecSQLToTable("select * from t_sum order by num_ls;");
                    var dv1 = new DataView(d1);
                    d1 = dv1.ToTable();
                    

                    string sqlcouns = "insert into t_couns (month_ , num_ls, num_cnt, val_cnt)" +
                                      "select " + cur_dt.ToString("MM") + ", a.num_ls, a.num_cnt, a.val_cnt  " +
                                      "From     selected_kvar sk," +
                                      curPref + DBManager.sDataAliasRest + "counters a "+ 
                                      "Where    sk.nzp_kvar=a.nzp_kvar and a.is_actual=1 " + servClause +
                                      " and a.dat_uchet between " +
                                      STCLINE.KP50.Global.Utils.EStrNull(cur_dt.AddDays(1).ToShortDateString()) +
                                      " and " +
                                      STCLINE.KP50.Global.Utils.EStrNull(cur_dt.AddMonths(1).ToShortDateString());
                    ExecSQL( sqlcouns);                    
                    var d2 = ExecSQLToTable("select * from t_couns order by num_ls ;");
                    var dv2 = new DataView(d2);
                    d2 = dv2.ToTable();


                   
                    ExecSQL("create index ix_t_sum01 on t_sum(month_, num_ls)");
                    ExecSQL(DBManager.sUpdStat + " t_sum");
                    ExecSQL("create index ix_t_couns01 on t_couns(month_, num_ls)");
                    ExecSQL(DBManager.sUpdStat + " t_couns");

                    
                    string sqlall = " insert into t_all (month_ ,num_ls, num_cnt, val_cnt, sum_money ) " +
                                    " select tc.month_ ,tc.num_ls, num_cnt, val_cnt, sum_money "+
                                    " from t_couns tc LEFT JOIN t_sum ts on tc.num_ls=ts.num_ls and tc.month_=ts.month_ ";
                    
                    ExecSQL(sqlall);
                    var d3 = ExecSQLToTable("select * from t_all order by num_ls;");
                    var dv3 = new DataView(d2);
                    d3 = dv3.ToTable();

                    ExecSQL(" drop table t_sum ", true);
                    ExecSQL(" drop table t_couns ", true);
                }

                ExecSQL("drop table selected_kvar");

                string svod = " insert into t_svod_counters( "+
                              " ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n, "+
                              " num_ls,num_cnt, month_, val_cnt)" +
                              " Select ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n,  " +
                              "        a.num_ls,num_cnt, month_, val_cnt" +
                              " From t_all a, " +
                              curPref + DBManager.sDataAliasRest + "kvar k, " +
                              curPref + DBManager.sDataAliasRest + "dom d, " +
                              curPref + DBManager.sDataAliasRest + "s_ulica u "+
                              "Where a.num_ls=k.num_ls and k.nzp_dom = d.nzp_dom "+  
                              " and d.nzp_ul = u.nzp_ul  ";
                ExecSQL(svod);

                string upd = " update t_svod_counters set max_val_cnt = (select max(val_cnt) " +
                " from t_svod_counters t where t.num_ls=t_svod_counters.num_ls and  t.month_=t_svod_counters.month_ group by num_ls, month_) ";
                ExecSQL(upd);

                upd = " update t_svod_counters set sum_money = (select " +
                             " max(sum_money) from t_all t where t.num_ls=t_svod_counters.num_ls and  t.month_=t_svod_counters.month_ ) where val_cnt=max_val_cnt ";
                ExecSQL(upd);

            }

            string res = " Select ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n, month_, " +
                         "        num_ls, num_cnt,  val_cnt, sum_money " +
                         " From t_svod_counters t"+
                         " order by ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n,  " +
                         "        num_ls, num_cnt, month_ ,  val_cnt";

                DataTable dt;
                try
                {
                    dt = ExecSQLToTable(res);
                    var dv = new DataView(dt);
                    dt = dv.ToTable();
                }
                catch (Exception)
                {
                    dt = ExecSQLToTable(DBManager.SetLimitOffset(res, 100000, 0));
                    var dv = new DataView(dt);
                    dt = dv.ToTable();
                }
                dt.TableName = "Q_master";
            
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
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
                        string sql = " insert into sel_kvar (nzp_kvar, nzp_area) " +
                                     " select nzp_kvar, nzp_area from " + tSpls;
                        ExecSQL(sql);
                    }
                }
            }
            else
            {

                string sql = " insert into sel_kvar (nzp_kvar, nzp_area) " +
                             " select nzp_kvar, nzp_area " +
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

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Управляющие компании: " + AreaHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("pdom", pdom);
            report.SetParameterValue("pmonth", pmonth);
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

            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }

            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            DatUchet = new DateTime(YearS, MonthS, 1);
            DatUchetPo = new DateTime(YearPo, MonthPo, 1);
            
            Services = UserParamValues["Services"].Value.To<List<int>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();

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
                    " nzp_geu integer, " +
                    " nzp_area integer) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);
           
            sql = "Create temp table t_all("+ 
                  " num_cnt char(20),"+
                  " month_ integer,"+
                  " num_ls integer,"+
                  " val_cnt " + DBManager.sDecimalType + "(14,2),"+
                  " sum_money_first " + DBManager.sDecimalType + "(14,2),"+
                  " sum_money " + DBManager.sDecimalType + "(14,2))"+
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " Create temp table t_svod_counters(" +
                  " num_cnt char(20)," +
                  " month_ integer," +
                  " num_ls integer," +
                  " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                  " max_val_cnt " + DBManager.sDecimalType + "(14,2)," +
                  " sum_money " + DBManager.sDecimalType + "(12,2)," +
                  " ulica char(40), " +
                  " idom integer, " +
                  " ikvar integer, " +
                  " ndom char(15), " +
                  " nkor char(15), " +
                  " nkvar char(40), " +
                  " nkvar_n char(40) " +
                  ")" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table sel_kvar ", true);
            ExecSQL(" drop table t_all ", true);
            ExecSQL(" drop table t_svod_counters ", true);
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.Core.Internal;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.Windsor.Installer;
using STCLINE.KP50.DataBase;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3001018 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.18 Расход по ОДПУ"; }
        }

        public override string Description
        {
            get { return "Расход по ОДПУ"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_30_1_18; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }



        /// <summary>Районы</summary>
        private string Raions { get; set; }

        /// <summary>Улицы</summary>
        private string Streets { get; set; }

        /// <summary>Дома</summary>
        private string Houses { get; set; }

        /// <summary>Расчетный месяц</summary>
        private int Month { get; set; }

        /// <summary>Расчетный год</summary>
        private int Year { get; set; }

        /// <summary>УК</summary>
        private List<int> Areas { get; set; }

        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new AddressParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

            var adr = UserParamValues["Address"].GetValue<AddressParameterValue>();

            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = adr.Raions.Aggregate(Raions, (current, nzpRajon) => current + (nzpRajon + ","));
                Raions = Raions.TrimEnd(',');
                Raions = "and u.nzp_raj in (" + Raions + ") ";
            }
            else return;


            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = adr.Streets.Aggregate(Streets, (current, nzpStreet) => current + (nzpStreet + ","));
                Streets = Streets.TrimEnd(',');
                Streets = "and u.nzp_ul in (" + Streets + ") ";
            }
            else return;

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                    Houses = "and d.ndom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
        }


        public void MakeSelectedKvar()
        {
            string sql;
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                var tSpls = WebBase + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";

                sql = " insert into selected_kvar(nzp_kvar, nzp_dom, nzp_ul, nzp_area)" +
                             " select nzp_kvar, nzp_dom, nzp_ul, nzp_area " +
                             " from " + tSpls;
            }
            else
            {
                sql = " insert into selected_kvar(nzp_kvar, nzp_dom, nzp_ul, nzp_area)" +
                             " select k.nzp_kvar, d.nzp_dom, u.nzp_ul, k.nzp_area " +
                             " from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k," +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "dom d " +
                             " where k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul " +
                             Raions + Streets + Houses + GetWhereArea();
            }
            ExecSQL(sql);
            
            ExecSQL(" create index ix_tmp_ls_01 on selected_kvar(nzp_kvar)");
            ExecSQL(DBManager.sUpdStat + " selected_kvar");
        }

        public override DataSet GetData()
        {


            #region выборка в temp таблицу

            MakeSelectedKvar();

            MyDataReader reader;

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                     "counters_" + Month.ToString("00");

                sql = " INSERT INTO temp_svod (nzp_dom, nzp_counter, nzp_serv, stek1) " +
                      " SELECT a.nzp_dom, a.nzp_counter, a.nzp_serv, val1 " +
                      " FROM " + chargeTable + " a " +
                      " WHERE stek in (1) and nzp_type = 1 ";
                ExecSQL(sql);

                sql = " INSERT INTO temp_svod (nzp_dom, nzp_counter, nzp_serv, stek2) " +
                      " SELECT a.nzp_dom, a.nzp_counter, a.nzp_serv, val1 " +
                      " FROM " + chargeTable + " a " +
                      " WHERE stek in (2) and nzp_type = 1 ";
                ExecSQL(sql);

                sql = " INSERT INTO temp_svod2 (nzp_dom, nzp_counter, nzp_serv, stek1, stek2) " +
                      " SELECT nzp_dom, nzp_counter, nzp_serv, SUM(stek1) as stek1, SUM(stek2) as stek2 " +
                      " FROM temp_svod " + 
                      " GROUP BY 1,2,3 ";
                ExecSQL(sql);

                sql = " INSERT INTO t_svod (nzp_area, nzp_ul, nzp_dom, nzp_serv, nzp_counter, rash) " +
                      " SELECT nzp_area, nzp_ul, k.nzp_dom, nzp_serv, nzp_counter, " +
                      " CASE WHEN stek1 <> 0 THEN stek1 WHEN stek2 <> 0 THEN stek2 ELSE 0 END AS rash " +
                      " FROM selected_kvar k, temp_svod2 t " +
                      " WHERE k.nzp_dom = t.nzp_dom  ";
                ExecSQL(sql);
            }

            reader.Close();
            #endregion

            sql = " SELECT DISTINCT area, ulica, idom, ndom, nkor, service, sum(rash) as rash FROM ( " + 
                  " SELECT DISTINCT area, ulica, idom, ndom,  CASE WHEN nkor<>'-' THEN nkor END as nkor, service, rash " +
                  " FROM t_svod tr, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "dom d " +
                  " WHERE tr.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND tr.nzp_area = a.nzp_area " +
                  " AND tr.nzp_serv = s.nzp_serv) " +
                  " GROUP BY 1,2,3,4,5,6 ORDER BY 1,2,3,4,5,6";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereArea = whereArea.TrimEnd(',');
            return !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty; 
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_svod ( " +
                              " nzp_area integer, " +
                              " nzp_ul integer, " +
                              " nzp_dom integer, " +
                              " nzp_serv integer, " +
                              " nzp_counter integer, " +
                              " rash " + DBManager.sDecimalType + "(14,4)) " +
                              DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE temp_svod ( " +
                              " nzp_dom integer, " +
                              " nzp_counter integer, " +
                              " nzp_serv integer, " +
                              " stek1 decimal(14,7), " +
                              " stek2 decimal(14,7)) " +
                              DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE temp_svod2 ( " +
                              " nzp_dom integer, " +
                              " nzp_counter integer, " +
                              " nzp_serv integer, " +
                              " stek1 decimal(14,7), " +
                              " stek2 decimal(14,7)) " +
                              DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE selected_kvar ( " +
                  " nzp_kvar integer, " +
                  " nzp_area integer, " +
                  " nzp_ul integer, " +
                  " nzp_dom integer) " +
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE temp_svod ");
            ExecSQL(" DROP TABLE temp_svod2 ");
            ExecSQL(" DROP TABLE t_svod ");
            ExecSQL(" DROP TABLE selected_kvar ");
        }
    }
}

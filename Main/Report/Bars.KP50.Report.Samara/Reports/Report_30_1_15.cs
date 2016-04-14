using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using STCLINE.KP50.DataBase;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3001015 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.15 Реестр передачи-приема счетов-квитанций"; }
        }

        public override string Description
        {
            get { return "Реестр передачи-приема счетов-квитанций"; }
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
            get { return Resources.Report_30_1_15; }
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

        /// <summary>Расчетный год</summary>
        private int Filter { get; set; }

        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new AddressParameter(),
                new ComboBoxParameter(false)
                {
                    Name = "Фильтр", 
                    Code = "Filter",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 1, Name = "Не фильтровать"},
                        new { Id = 2, Name = "Начислено"},
                        new { Id = 3, Name = "Начислено с учетом долга"}
                    }
                }
            };
        }

        protected override void PrepareParams()
        {


            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Filter = UserParamValues["Filter"].GetValue<int>();

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
            report.SetParameterValue("dat", DateTime.Now); 
            var agent = ExecSQLToTable(" select distinct val_prm from " +
                              ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 where nzp_prm = 80 and is_actual = 1 " +
                            " and dat_s <'" + DateTime.Now.ToShortDateString() + "' and dat_po> '" + DateTime.Now.ToShortDateString() + "' ");
            report.SetParameterValue("agent", agent.Rows.Count > 0 ? agent.Rows[0][0].ToString().Trim() : "");
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу

            var datS = new DateTime(Year, Month, 1);
            var datPo = datS.AddMonths(1).AddDays(-1);
            bool listLc = GetSelectedKvars();

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") +
                    DBManager.tableDelimiter + "charge_" + Month.ToString("00");

                if (TempTableInWebCashe(chargeTable))
                {
                    sql = " INSERT INTO t_reestr(nzp_dom,  tickets)" +
                          " SELECT k.nzp_dom, count(distinct c.nzp_kvar) as tickets " +
                          " FROM " +
                          pref + DBManager.sDataAliasRest + "kvar k, " +
                          chargeTable + " c " +
                          " WHERE c.nzp_kvar = k.nzp_kvar " +
                          " And k.nzp_kvar in (select nzp from "+ pref + DBManager.sDataAliasRest + "prm_3 p " +
                          " WHERE  k.nzp_kvar = p.nzp and nzp_prm = 51 and val_prm = '1' and is_actual = 1 " +
                          " AND dat_s <= '" + datPo.ToShortDateString() + "' and dat_po>= '" + datS.ToShortDateString() + "') ";
                    switch (Filter)
                    {
                        case 2: sql+= " AND c.sum_charge > 0 "; break;
                        case 3: sql+= " AND c.sum_charge + c.sum_insaldo - sum_money > 0 "; break;
                    } 
                    sql+= " GROUP BY 1 ";
                    ExecSQL(sql);

                    sql = "update t_reestr set indecs = (select max(val_prm" + DBManager.sConvToInt + ")" +
                          " from " + pref + DBManager.sDataAliasRest + "prm_2 where nzp_prm= 68 and is_actual=1)"+
                          " where nzp_dom in (select nzp from "+ pref + DBManager.sDataAliasRest + "prm_2 " +
                          "where nzp_prm= 68 and is_actual=1)";
                    ExecSQL(sql);

                }
            }

            reader.Close();
            #endregion

            sql = " SELECT CASE WHEN rajon='-' THEN town ELSE rajon END as rajon, " +
                  " ulica, ndom, " + 
                  " CASE WHEN nkor<>'-' and nkor<>' -' THEN nkor END as nkor, tr.indecs, tickets " +
                  " FROM t_reestr tr, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    (listLc ? " selected_kvars d " : ReportParams.Pref + DBManager.sDataAliasRest + "dom d ") +
                  " WHERE tr.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                    Raions + Streets + Houses +
                  " ORDER BY 5,2,3,4,1,6 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                int startIndex = Constants.cons_Webdata.IndexOf("Database=", System.StringComparison.Ordinal) + 9;
                int endIndex = Constants.cons_Webdata.Substring(startIndex, Constants.cons_Webdata.Length - startIndex).IndexOf(";", System.StringComparison.Ordinal);
                var tSpls = Constants.cons_Webdata.Substring(startIndex, endIndex) + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";
                if (TempTableInWebCashe(tSpls))
                {
                    string sql = " insert into selected_kvars (nzp_dom, nzp_ul, ndom, nkor) " +
                                 " select distinct nzp_dom, nzp_ul, " +
                                 DBManager.SetSubString("ndom", "0", "length(ndom)-2") + " as ndom, " +
                                 DBManager.SetSubString("ndom", "length(ndom)-1", "length(ndom)") + " as nkor " +
                                 " from " + tSpls;
                    ExecSQL(sql);
                    return true;
                }
            }
            return false;
        }



        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp;
            whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_reestr ( " +
                                " nzp_dom integer, " +
                                " indecs char(6), " +
                                " tickets integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " create temp table selected_kvars(" +
                    " nzp_dom integer, " +
                    " nzp_ul integer, " +
                    " ndom char(20), " +
                    " nkor char(20)) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_reestr ");
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Sakha.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Sakha.Reports
{
    public class Report1416 : BaseSqlReport
    {
        public override string Name
        {
            get { return "14.1.6 Оборотная ведомость"; }
        }

        public override string Description
        {
            get { return "14.1.6 Оборотная ведомость"; }
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
            get { return Resources.Report14_1_6; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }

        /// <summary>Банки данных</summary>
        private string AreasHeader { get; set; }
        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }
        /// <summary>Территория</summary>
        protected List<int> Banks { get; set; }
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new AreaParameter(),
                new BankParameter()
            };
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
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND d.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area d  WHERE d.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreasHeader = AreasHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("area", AreasHeader);
            string limit = ReportParams.ExportFormat == ExportFormat.Excel2007 ? "40000" : "100000";

            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
        }


        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
        }

        public override DataSet GetData()
        {
            var ds = new DataSet();
            string whereArea = GetWhereArea();
            var PrefList = GetPrefList();
            var sql = "";
            foreach (string pref in PrefList)
            {
                sql = string.Format(" insert into t_rev_statement (select distinct d.nzp_dom,u.ulica||' '||d.ndom||' '||d.nkor  address, " +
                    " (case when cast((select val_prm from {0}{1}prm_1 where nzp_prm = 8 and is_actual <> 100 and k.nzp_kvar = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} ) as integer) = 1 then 'есть' else 'нет' end) privatization, " +
                    " sum(ch.sum_insaldo) sum_insaldo, " +
                    " sum(ch.sum_tarif) sum_tarif, " +
                    " sum(ch.real_charge - ch.sum_tarif) sum_sn, " +
                    " sum(ch.sum_lgota) sum_lgota, " +
                    " sum(ch.sum_money) sum_money, " +
                    " sum(ch.sum_real) sum_real, " +
                    " sum(ch.sum_outsaldo) sum_outsaldo, " +
                    " {6}((select max(cast(val_prm as decimal)) from {0}{1}prm_2 where nzp_prm = 40 and is_actual <> 100 and d.nzp_dom = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} ),0) square, " +
                    " {6}((select max(cast(val_prm as decimal)) from {0}{1}prm_2 where nzp_prm = 40 and is_actual <> 100 and d.nzp_dom = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} ),0) square_soc_norm, " +
                    " 0 square_without_soc_norm, " +
                    " {6}(sum((select cast(max(val_prm) as integer) from {0}{1}prm_1 where nzp_prm = 5 and is_actual <> 100 and k.nzp_kvar = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} )),0) kol_gil_prop, " +
                    " {6}(sum((select cast(max(val_prm) as integer) from {0}{1}prm_1 where nzp_prm = 10 and is_actual <> 100 and k.nzp_kvar = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} )),0) kol_gil_ot, " +
                    " {6}(sum((select cast(max(val_prm) as integer) from {0}{1}prm_1 where nzp_prm = 131 and is_actual <> 100 and k.nzp_kvar = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} )),0) kol_gil_dop, " +
                    " {6}(sum((select cast(max(val_prm) as integer) from {0}{1}prm_1 where nzp_prm = 5 and is_actual <> 100 and k.nzp_kvar = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} )),0) +  " +
                    " {6}(sum((select cast(max(val_prm) as integer) from {0}{1}prm_1 where nzp_prm = 131 and is_actual <> 100 and k.nzp_kvar = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} )),0) - " +
                    " {6}(sum((select cast(max(val_prm) as integer) from {0}{1}prm_1 where nzp_prm = 10 and is_actual <> 100 and k.nzp_kvar = nzp and dat_s <= {7} " +
                     " and dat_po >= {7} )),0) fact_gil " +
                    "  from {4}dom d left outer join {4}s_ulica u on u.nzp_ul = d.nzp_ul, " +
                    " {0}{1}kvar k,{0}_charge_{3}.charge_{2} ch where d.nzp_dom = k.nzp_dom {5} and ch.nzp_kvar = k.nzp_kvar " +
                    " group by 1,2,3 order by d.nzp_dom)",
               pref, DBManager.sDataAliasRest, Month < 10 ? "0" + Month : Month.ToString(), Year.ToString().Substring(2, 2), ReportParams.Pref + DBManager.sDataAliasRest, whereArea, DBManager.sNvlWord, DBManager.sCurDate);
                ExecSQL(sql);
            }

            sql = string.Format("select * from t_rev_statement order by address");
            DataTable dt = ExecSQLToTable(sql);

            dt.TableName = "Q_master";
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void CreateTempTable()
        {
            var sql = string.Format(" create temp table t_rev_statement( " +
                  "  nzp_dom integer, " +
                  "  address varchar(50), " +
                  "  privatization varchar(4), " +
                  "  sum_insaldo decimal, " +
                  "  sum_tarif decimal, " +
                  "  sum_sn decimal, " +
                  "  sum_lgota decimal, " +
                  "  sum_money decimal, " +
                  "  sum_real decimal, " +
                 "   sum_outsaldo decimal, " +
                 "   square decimal, " +
                 "   square_soc_norm decimal, " +
                 "   square_without_soc_norm decimal, " +
                 "   kol_gil_prop integer, " +
                 "   kol_gil_ot integer, " +
                 "   kol_gil_dop integer, " +
                 "   fact_gil integer " +
              "  );");
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            try
            {
                if (TempTableInWebCashe("t_rev_statement"))
                {
                    ExecSQL(" drop table t_rev_statement ", true);
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет Оборотная ведомость " + e.Message, MonitorLog.typelog.Error, false);
            }
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
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                prefList.Add(Convert.ToString(reader["pref"]).Trim());
            }
            reader.Close();
            return prefList;
        }

        /// <summary>Ограничение по банкам данных</summary>
        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            whereWp = Banks != null
                ? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            TerritoryHeader = "";
            if (!string.IsNullOrEmpty(whereWp))
            {
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Constants = STCLINE.KP50.Global.Constants;
using DataTable = System.Data.DataTable;
using globalsUtils = STCLINE.KP50.Global.Utils;
using Points = STCLINE.KP50.Interfaces.Points;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71generatorPoIPU : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.Генератор по большим расходам ИПУ"; }
        }

        public override string Description
        {
            get { return "71.Генератор по большим расходам ИПУ"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_generatorPoIPU; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }
        /// <summary>Банк УК</summary>
        List<int> Banks { get; set; }
        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }
        /// <summary>Показание, считающиеся большим</summary>
        protected decimal BigValue { get; set; }
        /// <summary>Услуга</summary>
        protected int Service { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankParameter(),
              	new ServiceParameter(false,false){Require = true},     
                new StringParameter
				{
					Name = "Большое показание",
					Code = "BigValue",
					DefaultValue = 0m,
					Require = true,
					TypeValue = typeof(decimal),
					Value = 0m
				},
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());

            string limit = ReportParams.ExportFormat == ExportFormat.Excel2007 ? "40000" : "100000";

            report.SetParameterValue("pMonth", months[Month]);
            report.SetParameterValue("pYear", Year);
        }


        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            Service = UserParamValues["Services"].GetValue<int>();
            BigValue = UserParamValues["BigValue"].GetValue<decimal>();
        }

        public override DataSet GetData()
        {
            GetwhereWp();
            string sql;

            foreach (string pref in PrefBanks)
            {
                
                string chargeXX = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                      "charge_" + Month.ToString("00");
                string counters_spis = pref + DBManager.sDataAliasRest + "counters_spis";
                string s_counttypes = pref + DBManager.sKernelAliasRest + "s_counttypes ";
                int nzp_wp = Points.PointList.Where(x => x.pref == pref).Select(x => x.nzp_wp).FirstOrDefault();

                if (TempTableInWebCashe(chargeXX))
                {
                    sql = " DELETE FROM t_big_values";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_big_values (nzp_serv, nzp_kvar, rashod)" +
                          " SELECT c.nzp_serv, c.nzp_kvar, c.c_calc " +
                          " FROM " + chargeXX + " c " +
                          " WHERE is_device=1 AND c.nzp_serv = " + Service +
                          " AND c.c_calc >= " + BigValue;
                    ExecSQL(sql);

                    sql = " INSERT INTO t_big_ipu (nzp_serv, nzp_kvar, rashod, nzp_counter, num_cnt, mmnog, cnt_stage, nzp_wp)" +
                          " SELECT t.nzp_serv, t.nzp_kvar, t.rashod, c.nzp_counter, c.num_cnt, s.mmnog, s.cnt_stage, " + nzp_wp +
                          " FROM t_big_values t, " + 
                          counters_spis + " c," +
                          s_counttypes + " s" +
                          " WHERE t.nzp_kvar = c.nzp AND t.nzp_serv = c.nzp_serv AND c.nzp_cnttype = s.nzp_cnttype ";
                    ExecSQL(sql);

                    DateTime DatUchet = new DateTime(Year, Month, 1);
                    sql =
                        string.Format(
                            "update t_big_ipu tbi set initial_reading = " +
                            "   (select  min(cast(c.val_cnt as decimal)) " +
                            "   from {0}counters c" +
                            "   where c.nzp_kvar = tbi.nzp_kvar and c.is_actual <>100 " +
                            "   and c.nzp_serv = tbi.nzp_serv and tbi.nzp_counter = c.nzp_counter " +
                            "   and c.dat_uchet BETWEEN " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) +
                            "   AND " + globalsUtils.EStrNull(DatUchet.AddMonths(1).ToShortDateString()) + " ) " +
                            " WHERE nzp_wp = " + nzp_wp,
                            pref + DBManager.sDataAliasRest);
                    ExecSQL(sql);

                    sql =
                        string.Format(
                            "update t_big_ipu tbi set ending_reading = " +
                            "   (select  max(cast(val_cnt as decimal)) " +
                            "   from {0}counters c" +
                            "   where c.nzp_kvar = tbi.nzp_kvar and c.is_actual <>100  " +
                            "   and c.nzp_serv = tbi.nzp_serv and tbi.nzp_counter = c.nzp_counter  " +
                            "   and c.dat_uchet BETWEEN " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) +
                            "   AND " + globalsUtils.EStrNull(DatUchet.AddMonths(1).ToShortDateString()) + " ) " +
                            " WHERE  nzp_wp = " + nzp_wp,
                            pref + DBManager.sDataAliasRest);
                    ExecSQL(sql);

                    sql =
                        string.Format(
                            "update t_big_ipu tbi set rashod = " +
                            "   (CASE WHEN ending_reading >= initial_reading" +
                            "    THEN (ending_reading - initial_reading)*mmnog" +
                            "    ELSE (POWER(10, cnt_stage) - initial_reading + ending_reading)*mmnog END )  " +
                            " WHERE  nzp_wp = " + nzp_wp,
                            pref + DBManager.sDataAliasRest);
                    ExecSQL(sql);

                    //удаляем неподходящие строки
                    sql =
                        " DELETE FROM  t_big_ipu tbi" +
                        " WHERE NOT (ABS(ending_reading - initial_reading) >= " + BigValue +
                        " AND NOT (ending_reading IS NULL AND initial_reading IS NULL)) AND nzp_wp = " + nzp_wp;
                    ExecSQL(sql);
                }
            }

            sql = " SELECT (CASE WHEN TRIM(town)<>'-' THEN TRIM(town) ELSE '' END) as town, " +
                  " (CASE WHEN TRIM(rajon)<>'-' THEN TRIM(rajon) ELSE '' END) as rajon," +
                  " TRIM(ulica) as ulica, TRIM(ulicareg) as ulicareg, TRIM(ndom) as ndom," +
                  " (CASE WHEN TRIM(nkvar_n)<>'-' THEN ' корп.'||TRIM(nkor) ELSE '' END) as nkor, " +
                  " nkvar, " +
                  " (CASE WHEN TRIM(nkvar_n)<>'-' THEN ' ком.'||TRIM(nkvar_n) ELSE '' END) as nkvar_n, " +
                  " k.fio, k.nzp_kvar, s.service, t.rashod, t.num_cnt, t.initial_reading, t.ending_reading " +
                  " FROM t_big_ipu t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                  " WHERE t.nzp_kvar=k.nzp_kvar " +
                  " AND k.nzp_dom=d.nzp_dom " +
                  " AND d.nzp_ul=u.nzp_ul " +
                  " AND u.nzp_raj=r.nzp_raj " +
                  " and r.nzp_town=st.nzp_town " +
                  " AND s.nzp_serv=t.nzp_serv " +
                  " ORDER BY 1,2,3,4,idom,5,6,ikvar,7,8 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }


        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null )
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());

                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
            string whereWpRes = !String.IsNullOrEmpty(whereWp) ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s,"
                       + ReportParams.Pref + DBManager.sDataAliasRest + "kvar kv " +
                       "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )" : String.Empty;
            return whereWpRes;
        }
        protected override void CreateTempTable()
        {
            string sql = " Create temp table t_big_ipu (" +
             " nzp_kvar integer, " +
             " nzp_counter integer," +
             " nzp_serv integer," +
             " nzp_wp integer," +
             " num_cnt " + DBManager.sCharType + "(40)," +
             " mmnog " + DBManager.sDecimalType + "(16,7)," +
             " cnt_stage integer," +
             " rashod "+DBManager.sDecimalType+"(13,4)," +
             " initial_reading " + DBManager.sDecimalType + "(13,4)," +
             " ending_reading " + DBManager.sDecimalType + "(13,4))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " Create temp table t_big_values (" +
             " nzp_kvar integer, " +
             " nzp_serv integer," +
             " rashod " + DBManager.sDecimalType + "(13,4))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_big_ipu ", true);
            ExecSQL(" drop table t_big_values ", true);
        }

    }
}

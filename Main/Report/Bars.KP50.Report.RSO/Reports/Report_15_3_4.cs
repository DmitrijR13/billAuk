using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RSO.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RSO.Reports
{
    public class Report1534 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.3.4 Отчет по поступившим реестрам от поставщиков по суммам"; }
        }

        public override string Description
        {
            get { return "Отчет по поступившим реестрам от поставщиков по суммам"; }
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
            get { return Resources.Report_15_3_4; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        
        /// <summary> с расчетного дня </summary>
        private DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        private DateTime DatPo { get; set; }

        /// <summary>Услуги</summary>
        private List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            DateTime datS = curCalcMonthYear != null
                ? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
                    Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
                : DateTime.Now;
            DateTime datPo = curCalcMonthYear != null
                ? datS.AddMonths(1).AddDays(-1)
                : DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new SupplierAndBankParameter(),
                new ServiceParameter()
            };
        }

        public override DataSet GetData()
        {
            var whereServ = GetWhereServ();
            var whereSupp = GetWhereSupp("sp.");
            var whereWp = GetwhereWp();
            string sql;

            for (var i = DatS.Year * 12 + DatS.Month; i <DatPo.Year * 12 + DatPo.Month + 1; i++)
            {
                var year = i / 12;
                var month = i % 12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }

                var distribTable = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                        DBManager.tableDelimiter + "fn_distrib_dom_" + month.ToString("00");

                if (TempTableInWebCashe(distribTable))
                {
                    sql = " INSERT INTO t_reestr (nzp_dom, nzp_supp, nzp_serv, dat_oper, sum_rasp)" +
                            " SELECT dd.nzp_dom, sp.nzp_supp, nzp_serv, dat_oper, sum_rasp " +
                            " FROM " + distribTable + " dd, " +
                              ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp " +
                            " WHERE dat_oper >= '" + DatS.ToShortDateString() + "' " +
                            " AND dat_oper <= '" + DatPo.ToShortDateString() + "' " +
                            " AND nzp_serv > 1 " +
                            " AND dd.nzp_payer = sp.nzp_payer " +
                            whereServ + whereSupp;
                    ExecSQL(sql);
                }
            }
            #region Выборка на экран

            ExecSQL(" create index reestr_ndx on t_reestr (nzp_dom) ");
            ExecSQL(DBManager.sUpdStat + " t_reestr ");

            sql = " INSERT INTO t_adr (nzp_dom, rajon) " +
                  " SELECT d.nzp_dom, case when rajon <>'-' then rajon else town end as rajon " + 
                  " FROM " + 
                    ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
                  " WHERE d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " + whereWp;
            ExecSQL(sql);

            sql = " SELECT " +
                  " rajon, service, name_supp, dat_oper, sum(sum_rasp) as sum_rasp " +
                  " FROM t_reestr t, t_adr a, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  " AND t.nzp_serv = s.nzp_serv " +
                  " AND t.nzp_dom = a.nzp_dom " +
                  " GROUP BY 1,2,3,4 ORDER BY 1,2,3,4  ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            #endregion

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private string GetWhereServ()
        {
            var result = String.Empty;
            if (Services != null)
            {
                result = Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND nzp_serv in (" + result + ")" : String.Empty;
            return result;
        }


        private string GetWhereSupp(string tablePrefix)
        {
            var result = String.Empty;
            if (Suppliers != null)
            {
                result = Suppliers.Aggregate(result, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            result = result.TrimEnd(',');

            if (String.IsNullOrEmpty(result)) return result;

            result = " AND " + tablePrefix + "nzp_supp in (" + result + ")";
            return result;
        }

        private string GetwhereWp()
        {
            IDataReader reader;
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

            string sql = " SELECT bd_kernel as pref " +
             " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
             " WHERE nzp_wp>1 " + whereWp;
            ExecRead(out reader, sql);
            whereWp = "";
            while (reader.Read())
            {
                whereWp = whereWp + " '" + reader["pref"].ToStr().Trim() + "', ";
            }
            whereWp = whereWp.TrimEnd(',', ' ');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND d.pref in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
        }

        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;

            Services = UserParamValues["Services"].GetValue<List<int>>();

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
            string sql = " create temp table t_reestr( " +
                               " nzp_dom integer default 0, " +
                               " nzp_serv integer default 0, " +
                               " nzp_supp integer default 0, " +
                               " dat_oper date, " +
                               " sum_rasp " + DBManager.sDecimalType + "(14,2) default 0.00) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_adr( " +
                    " nzp_dom integer default 0, " +
                    " rajon char(30)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_reestr ", false);
            ExecSQL(" drop table t_adr ", false);
        }
    }
}

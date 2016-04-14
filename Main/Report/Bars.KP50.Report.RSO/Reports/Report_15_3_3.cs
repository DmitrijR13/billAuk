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
    public class Report1533 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.3.3 Свод поступлений по поставщикам"; }
        }

        public override string Description
        {
            get { return "Свод поступлений по поставщикам"; }
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
            get { return Resources.Report_15_3_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

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
                new ServiceParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            string sql;

            for (var i = DatS.Year*12 + DatS.Month;
                i <
                DatPo.Year*12 + DatPo.Month + 1;
                i++)
            {
                var year = i/12;
                var month = i%12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }

                var distribXx = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                DBManager.tableDelimiter + "fn_distrib_dom_" + month.ToString("00");



                sql = " INSERT INTO t_distrib (nzp_dom, nzp_area,nzp_serv, " +
                      "         nzp_supp, sum_rasp, sum_ud, sum_charge )" +
                      " SELECT a.nzp_dom, a.nzp_area,a.nzp_serv, sp.nzp_supp, " +
                      "         sum(a.sum_rasp), sum(a.sum_ud), sum(a.sum_charge)  " +
                      " FROM " + distribXx + " a,  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp" +
                      "  WHERE  dat_oper>='" + DatS.ToShortDateString() + "' " +
                      "         AND dat_oper<='" + DatPo.ToShortDateString() + "'" +
                      "         AND a.nzp_payer=sp.nzp_payer  " +
                      GetWhereAdr() + GetWhereSupp() + GetWhereServ() +
                      " GROUP BY  1,2,3,4 ";
                if (TempTableInWebCashe(distribXx)) ExecSQL(sql);

            }


            #region Выборка на экран

            sql = " SELECT sp.point as pref, s.service, su.name_supp, " +
                  "        sum(t.sum_charge) as sum_charge, sum(sum_rasp) as sum_rasp, sum(t.sum_ud) as sum_ud  " +
                  " FROM t_distrib t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica sl, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_point sp " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  "        AND t.nzp_serv = s.nzp_serv " +
                  "        and t.nzp_dom=d.nzp_dom " +
                  "        and d.nzp_ul=sl.nzp_ul " +
                  "        AND sp.bd_kernel = d.pref " + GetwhereWp() +
                  "        GROUP BY 1,2,3 ORDER BY 1,2,3  ";
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

        private string GetWhereAdr()
        {
            var result = String.Empty;
            if (Areas != null)
            {
                result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            result = result.TrimEnd(',');
            if (String.IsNullOrEmpty(result)) return result;
            result = " AND nzp_area in (" + result + ")";
            AreaHeader = String.Empty;
            var sql = " SELECT area from " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                         " WHERE nzp_area > 0 " + result;
            DataTable area = ExecSQLToTable(sql);
            foreach (DataRow dr in area.Rows)
            {
                AreaHeader += dr["area"].ToString().Trim() + ",";
            }
            AreaHeader = AreaHeader.TrimEnd(',');
            return result;
        }

        private string GetWhereSupp()
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

            result = " AND nzp_supp in (" + result + ")";
            SupplierHeader = "";
            //Поставщики
            SupplierHeader = String.Empty;
            var sql = " SELECT name_supp from " +
                      ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                      " WHERE nzp_supp > 0 " + result;
            var supp = ExecSQLToTable(sql);
            foreach (DataRow dr in supp.Rows)
            {
                SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
            }
            SupplierHeader = SupplierHeader.TrimEnd(',');
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
             " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
             " WHERE nzp_wp>1 " + whereWp;
            ExecRead(out reader, sql);
            whereWp = "";
            while (reader.Read())
            {
                whereWp = whereWp + "'"+ reader["pref"].ToStr().Trim() + "', ";
            }
            whereWp = whereWp.TrimEnd(',',' ');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND d.pref in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dateBegin", DatS.ToShortDateString());
            report.SetParameterValue("dateEnd", DatPo.ToShortDateString());

            string headers = "";
            if ((String.IsNullOrEmpty(headers)) && (!String.IsNullOrEmpty(AreaHeader))) headers = AreaHeader;
            if ((String.IsNullOrEmpty(headers)) && (!String.IsNullOrEmpty(SupplierHeader))) headers = SupplierHeader;
            if (String.IsNullOrEmpty(headers)) headers = "Всем лс";

            report.SetParameterValue("headers", headers);
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
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

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
            const string sql = " create temp table t_distrib (     " +
                               " nzp_dom integer default 0," +
                               " nzp_area integer default 0," +
                               " nzp_serv integer default 0," +
                               " nzp_supp integer default 0," +
                               " sum_rasp " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Рапределено
                               " sum_ud " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Удержано
                               " sum_charge " + DBManager.sDecimalType + "(14,2) default 0.00 " + //К перечислению
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_distrib ", false);
        }
    }
}

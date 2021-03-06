﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report7133 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.3.3 Свод поступлений по поставщикам"; }
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
            get { return Resources.Report_71_3_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }


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
                new BankSupplierParameter(),
                new ServiceParameter()
            };
        }

        public override DataSet GetData()
        {
            string sql;
            string whereSupp = GetWhereSupp("a.nzp_supp"),
                    whereServ = GetWhereServ(),
                     wherePayer = GetWherePayer("a.nzp_payer");

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

                sql = " INSERT INTO t_distrib (nzp_dom, nzp_serv, " +
                      "         nzp_supp, sum_rasp, sum_ud, sum_charge )" +
                      " SELECT a.nzp_dom, a.nzp_serv, a.nzp_supp, " +
                      "         sum(a.sum_rasp), sum(a.sum_ud), sum(a.sum_charge)  " +
                      " FROM " + distribXx + " a,   " +
                      ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s " +
                      "  WHERE  dat_oper>='" + DatS.ToShortDateString() + "'  " +
                      "         and a.nzp_supp=s.nzp_supp " +
                      "         and a.nzp_payer=s.nzp_payer_princip " +
                      "         AND dat_oper<='" + DatPo.ToShortDateString() + "'" +
                      whereSupp + whereServ + wherePayer +
                      " GROUP BY  1,2,3 ";
                if (TempTableInWebCashe(distribXx))
                    ExecSQL(sql);

            }

            #region Выборка на экран

            sql = " SELECT  sp.point as pref, pa.payer as agent, pp.payer as principal,  s.service," +
                  "        p.payer as name_supp, " +
                  "        sum(t.sum_charge) as sum_charge, sum(sum_rasp) as sum_rasp, " +
                  "        sum(t.sum_ud) as sum_ud  " +
                  " FROM t_distrib t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pa, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pp, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica sl, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_point sp " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  "        AND su.nzp_payer_supp = p.nzp_payer " +
                  "        AND su.nzp_payer_agent = pa.nzp_payer " +
                  "        AND su.nzp_payer_princip = pp.nzp_payer " +
                  "        AND t.nzp_serv = s.nzp_serv " +
                  "        and t.nzp_dom=d.nzp_dom " +
                  "        and d.nzp_ul=sl.nzp_ul " +
                  "        AND sp.bd_kernel = d.pref " + GetwhereWp() +
                  "        GROUP BY 1,2,3,4,5 ORDER BY 1,2,3,4,5  ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            #endregion
            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private string GetWhereServ()
        {
            var result = String.Empty;
            result = Services != null 
                ? Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ",")) 
                : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND nzp_serv in (" + result + ")" : String.Empty;
            if (!String.IsNullOrEmpty(result))
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + result;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');

            }
            return result;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            
            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',',' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp+")";
        }

        /// <summary>
        /// Получает условия ограничения по контрагенту, который получает деньги
        /// </summary>
        /// <returns></returns>
        private string GetWherePayer(string fieldPref)
        {
            string wherePayer = String.Empty;
            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                wherePayer += BankSupplier.Principals.Aggregate(wherePayer, (current, nzpSupp) => current + (nzpSupp + ","));

            }

            wherePayer = wherePayer.TrimEnd(',');
            string rolePayer = ReportParams.GetRolesCondition(Constants.role_sql_payer);

            if (!String.IsNullOrEmpty(wherePayer) || !String.IsNullOrEmpty(rolePayer))
            {
                wherePayer = " WHERE nzp_payer in  (" + wherePayer + ")";
                if (!String.IsNullOrEmpty(rolePayer))
                    wherePayer += " and nzp_payer in (" + rolePayer + ")";

                string sql = " and " + fieldPref + " in ( SELECT nzp_payer " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer " +
                             wherePayer + ")";
                return sql;
            }
            return String.Empty;
        }

        private string GetwhereWp()
        {
            MyDataReader reader;
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }

            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;

            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql1 = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql1);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }

            string sql = " SELECT bd_kernel as pref " +
             " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
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

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            if (!string.IsNullOrEmpty(SupplierHeader) && SupplierHeader.Length <= 2000)
            {
                headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
                report.SetParameterValue("suppParam", "");
            }
            else report.SetParameterValue("suppParam", !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty);
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
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

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
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

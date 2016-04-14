﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.RT.Properties;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Bars.KP50.Report.RT.Reports
{
    class Report16515 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.5.15. Сводка по услугам с группировкой по тарифам"; }
        }

        public override string Description
        {
            get { return "5.15. Сводка по услугам с группировкой по тарифам"; }
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

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>УК</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Районы</summary>
        protected List<int> Banks { get; set; }


        /// <summary>УК</summary>
        protected string AreasHeader { get; set; }

        /// <summary>Поставщики</summary>
        protected string SuppliersHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] { "","Январь", "Февраль", "Март",
            "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
            "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("area", AreasHeader);
            report.SetParameterValue("supp", SuppliersHeader);
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_5_15; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
                 var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            string whereArea = GetWhereArea();
            string whereSupp = GetWhereSupp();

            var datS = new DateTime(Year,Month,1);
            DateTime datPo = datS.AddMonths(1).AddDays(-1);

            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                sql = " insert into group_dom (nzp_serv, tarif, dom_count, ob_s, kvar_count, gil) " +
                    " select ch.nzp_serv, ch.tarif, " +
                    " k.nzp_dom as dom_count, " +
                    " val_prm" + DBManager.sConvToInt + " as ob_s, " +
                    " k.nzp_kvar as kvar_count, " +
                    " round(gil) as gil " +
                    " from  " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00") + " ch, " +
                      pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00") + " cg, " +
                      pref + DBManager.sDataAliasRest + "kvar k, " +
                      pref + DBManager.sDataAliasRest + "prm_1 p " + 
                    " where k.nzp_kvar = ch.nzp_kvar and k.nzp_kvar = cg.nzp_kvar and k.nzp_kvar = p.nzp and nzp_prm = 4 " +
                    " and ch.dat_charge is null and cg.dat_charge is null and ch.nzp_serv>1 and cg.nzp_serv>1 and p.is_actual<>100 " +
                    " and p.dat_s <= '" + datPo.ToShortDateString() + "' " +
                    " and p.dat_po >= '" + datS.ToShortDateString() + "' " + 
                      whereArea + whereSupp;
                ExecSQL(sql);
            }

            reader.Close();
            sql = " insert into t_svod (nzp_serv, tarif, dom_count, ob_s, kvar_count, gil) " +
                  " select distinct nzp_serv, tarif, dom_count, ob_s, kvar_count, gil " +
                  " from group_dom "; 
            ExecSQL(sql);
            ExecSQL(" delete from group_dom ");
            sql = " insert into group_dom (nzp_serv, tarif, dom_count, ob_s, kvar_count, gil) " +
                  " select nzp_serv, tarif, dom_count," +
                  " sum(ob_s) as ob_s, " +
                  " count(kvar_count) as kvar_count, " +
                  " sum(gil) as gil " +
                  " from t_svod t " +
                  " group by 1,2,3 ";
            ExecSQL(sql);
            sql = " select service, round(tarif,2) as tarif, " +
                   " count(dom_count) as dom_count," +
                   " sum(ob_s) as ob_s, " +
                   " sum(kvar_count) as kvar_count, " +
                   " sum(gil) as gil " +
                   " from group_dom t, " +
                     ReportParams.Pref + DBManager.sKernelAliasRest + "services se " +
                   " where se.nzp_serv = t.nzp_serv " +
                   " group by 1,2 " +
                   " order by 1,2 ";
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
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreasHeader = AreasHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        private string GetwhereWp()
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

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND ch.nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier ch WHERE ch.nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
        }

        protected override void CreateTempTable()
        {
            string sql = "create temp table t_svod (" +
                               " nzp_serv integer, " +
                               " tarif " + DBManager.sDecimalType + "(14,4), " +
                               " dom_count integer, " +
                               " ob_s " + DBManager.sDecimalType + "(14,2), " +
                               " kvar_count integer, " +
                               " gil integer " +
                               ")";
            ExecSQL(sql); 
            sql = "create temp table group_dom (" +
                    " nzp_serv integer, " +
                    " tarif " + DBManager.sDecimalType + "(14,4), " +
                    " dom_count integer, " +
                    " ob_s " + DBManager.sDecimalType + "(14,2), " +
                    " kvar_count integer, " +
                    " gil integer " +
                    ")";
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod; drop table group_dom; ");
        }

    }
}

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
    public class Report1535 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.3.5 Сводный развернутый отчет"; }
        }

        public override string Description
        {
            get { return "Сводный развернутый отчет"; }
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
            get { return Resources.Report_15_3_5; }
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
            string sql;

            for (var i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
            {
                var year = i / 12;
                var month = i % 12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }

                string sumInsaldo = "0";
                if (year == DatS.Year && month == DatS.Month) sumInsaldo = "sum_insaldo";

                string sumOutsaldo = "0";
                if (year == DatPo.Year && month == DatPo.Month) sumOutsaldo = "sum_outsaldo";

                var distribTable = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                        DBManager.tableDelimiter + "fn_distrib_dom_" + month.ToString("00");

                if (TempTableInWebCashe(distribTable))
                {
                    sql = " INSERT INTO t_reestr (nzp_dom, nzp_supp, nzp_serv, unalloc, sum_ud, sum_charge, sum_send)" +
                            " SELECT dd.nzp_dom, sp.nzp_supp, nzp_serv, 0 as unalloc, sum_ud, sum_charge, sum_send " +
                            " FROM " + distribTable + " dd, " +
                              ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp " +
                            " WHERE dat_oper >= '" + DatS.ToShortDateString() + "' " +
                            " AND dat_oper <= '" + DatPo.ToShortDateString() + "' " +
                            " AND nzp_serv > 1 " +
                            " AND dd.nzp_payer = sp.nzp_payer " +
                            whereServ + whereSupp;
                    ExecSQL(sql);

                    sql = " INSERT INTO t_reestr (nzp_dom, nzp_supp, nzp_serv, bank, post, other)" +
                            " SELECT dd.nzp_dom, sp.nzp_supp, nzp_serv, " +
                            " CASE WHEN nzp_bank = 110000 then sum_rasp end as bank, " +
                            " CASE WHEN nzp_bank = 2011 then sum_rasp end as post, " +
                            " CASE WHEN nzp_bank not in (110000, 2011) then sum_rasp end as other " +
                            " FROM " + distribTable + " dd, " +
                              ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp " +
                            " WHERE dat_oper >= '" + DatS.ToShortDateString() + "' " +
                            " AND dat_oper <= '" + DatPo.ToShortDateString() + "' " +
                            " AND nzp_serv > 1 " +
                            " AND dd.nzp_payer = sp.nzp_payer " +
                            whereServ + whereSupp;
                    ExecSQL(sql);

                    sql = " SELECT * " +
                          " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                          " WHERE nzp_wp>1 " + GetwhereWp("");
                    MyDataReader reader;
                    ExecRead(out reader, sql);

                    while (reader.Read())
                    {
                        var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                        var chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                        "charge_" + month.ToString("00");
                        var fromSupplierTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                        "from_supplier ";

                        if (TempTableInWebCashe(chargeTable))
                        {
                            sql =
                                " INSERT INTO t_reestr (nzp_dom, nzp_serv, nzp_supp, sum_in, sum_real, sum_nedop, real_charge, reval, sum_out)" +
                                " SELECT d.nzp_dom, a.nzp_serv, a.nzp_supp, " +
                                " SUM(" + sumInsaldo + "), SUM(sum_real), SUM(sum_nedop)," +
                                " SUM(real_charge), SUM(reval), SUM(" + sumOutsaldo + ")  " +
                                " FROM " + chargeTable + " a, " +
                                pref + DBManager.sDataAliasRest + "kvar k," +
                                pref + DBManager.sDataAliasRest + "dom d " +
                                " WHERE a.num_ls=k.num_ls " +
                                " AND a.nzp_serv>1 " +
                                " AND a.dat_charge is null " +
                                " AND k.nzp_dom = d.nzp_dom  " +
                                whereServ + GetWhereSupp("a.") +
                                " GROUP BY 1,2,3 ";
                            ExecSQL(sql);
                        } 
                    
                        if (TempTableInWebCashe(fromSupplierTable))
                        {
                            sql = " INSERT INTO t_reestr (nzp_dom, nzp_serv, nzp_supp, sum_prih)" +
                                  " SELECT d.nzp_dom, a.nzp_serv, a.nzp_supp, sum(sum_prih)  " +
                                  " FROM " + fromSupplierTable + " a, " +
                                  pref + DBManager.sDataAliasRest + "kvar k," +
                                  pref + DBManager.sDataAliasRest + "dom d " +
                                  " WHERE a.num_ls=k.num_ls " +
                                  " AND a.nzp_serv>1 " +
                                  " AND k.nzp_dom = d.nzp_dom  " +
                                  " AND a.dat_uchet>='01." + month + "." + year + "'" +
                                  " AND a.dat_uchet<='" + DateTime.DaysInMonth(year, month) + "." + month + "." + year +"'" +
                                  whereServ + GetWhereSupp("a.") +
                                  " GROUP BY 1,2,3 ";
                            ExecSQL(sql);
                        }
                    }
                    reader.Close();
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
                  " AND r.nzp_town = t.nzp_town " + GetwhereWp("d.");
            ExecSQL(sql);

            sql = " SELECT " +
                  " rajon, name_supp, service, " +
                  " sum(sum_in) as sum_in, " +
                  " sum(sum_real) as sum_real, " +
                  " sum(sum_nedop) as sum_nedop, " +
                  " sum(real_charge) as real_charge, " +
                  " sum(reval) as reval, " +
                  " sum(sum_prih) as sum_prih, " +
                  " sum(bank) as bank, " +
                  " sum(post) as post, " +
                  " sum(other) as other, " +
                  " sum(unalloc) as unalloc, " +
                  " sum(sum_ud) as sum_ud, " +
                  " sum(sum_charge) as sum_charge, " +
                  " sum(sum_send) as sum_send, " +
                  " sum(sum_out) as sum_out " +
                  " FROM t_reestr t, t_adr a, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  " AND t.nzp_serv = s.nzp_serv " +
                  " AND t.nzp_dom = a.nzp_dom " +
                  " GROUP BY 1,2,3 ORDER BY 1,2,3  ";
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

        private string GetwhereWp(string tablePrefix)
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
            if (String.IsNullOrEmpty(tablePrefix))
            {
                return whereWp;
            }
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
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND " + tablePrefix + "pref in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("dats", DatS.ToShortDateString());
            report.SetParameterValue("datpo", DatPo.ToShortDateString());
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
                               " sum_in " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_real " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_nedop " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " real_charge " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " reval " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_prih " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " bank " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " post " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " other " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " unalloc " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_ud " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_charge " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_send " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_out " + DBManager.sDecimalType + "(14,2) default 0.00) " + DBManager.sUnlogTempTable;
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

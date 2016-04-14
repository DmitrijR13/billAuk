using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Samara.Properties;
using Castle.Core.Internal;

namespace Bars.KP50.Report.Samara.Reports
{
    public class ReportDoorSpeaker : BaseSqlReport
    {
        public override string Name
        {
            get { return "Отчет по начислению и оплате  за домофон"; }
        }

        public override string Description
        {
            get { return "Отчет по начислению и оплате  за домофон"; }
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
            get { return Resources.ReportDoorSpeaker; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }


        /// <summary>Расчетный месяц</summary>
        private int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        private int YearS { get; set; }

        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new AddressParameter()            
            };
        }

        public override DataSet GetData()
        {
            var pref = "fbill";
            var localPref = "bill01";
            string chargeTable = localPref + "_charge_" + (YearS - 2000).ToString("00") + ".charge_" + (MonthS).ToString("00");
            string chargeTable2 = "";
            string perekidkaTable = localPref + "_charge_" + (YearS - 2000).ToString("00") + ".perekidka";
            string sql = " SELECT idom, ikvar, ulica || ' д.' || ndom  as address, " +
                " ulica || ' д.' || ndom || ' кв. ' || nkvar || ' ком. ' || CASE WHEN nkvar_n is null THEN '' ELSE nkvar_n END as flat, k.num_ls, " +
                         " 'Домофон' as serv, sum(rsum_tarif) + sum(reval) + sum(sum_nedop) as sum_charge, coalesce(p.sum_rcl, 0) as sum_rcl, " +
                         " sum(sum_outsaldo) as sum_outsaldo, 0 as sum_money, k.nzp_kvar " +
                        " FROM " + pref + "_data.kvar k " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                        " INNER JOIN " + chargeTable + " c on c.nzp_kvar = k.nzp_kvar " +
                        " LEFT JOIN (SELECT nzp_kvar, sum(sum_rcl) as sum_rcl from " + perekidkaTable + " where month_ = " + MonthS + " and nzp_serv = 26 group by 1) p on p.nzp_kvar = k.nzp_kvar " +
                        " where nzp_serv = 26 and dat_charge is null " + Raions + Streets + Houses +
                        " GROUP BY 1,2,3,4,5,6,8,10,11 " +
                        " ORDER BY 1,2";
            //StreamWriter sw = new StreamWriter(@"C:/Temp/generator.txt");
            //sw.WriteLine("Start");
            var dt = ExecSQLToTable(sql);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["sum_charge"] = Convert.ToDecimal(dt.Rows[i]["sum_charge"]) + Convert.ToDecimal(dt.Rows[i]["sum_rcl"]);
            }
            var ds = new DataSet();
            if (YearS == 2015 && MonthS == 9)
            {
                chargeTable = localPref + "_charge_" + (YearS - 2000).ToString("00") + ".charge_" + (MonthS + 1).ToString("00");
                chargeTable2 = localPref + "_charge_" + (YearS - 2000).ToString("00") + ".charge_" + (MonthS).ToString("00");
                sql = " SELECT sum(sum_money) as sum_money, k.nzp_kvar, 10 as mon" +
                       " FROM " + pref + "_data.kvar k " +
                       " INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                       " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                       " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                       " INNER JOIN " + chargeTable + " c on c.nzp_kvar = k.nzp_kvar " +
                       " where nzp_serv = 26 and dat_charge is null " + Raions + Streets + Houses +
                       " GROUP BY 2" +
                       " UNION ALL " +
                       " SELECT sum(sum_money) as sum_money, k.nzp_kvar, 9 as mon" +
                       " FROM " + pref + "_data.kvar k " +
                       " INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                       " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                       " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                       " INNER JOIN " + chargeTable2 + " c on c.nzp_kvar = k.nzp_kvar " +
                       " where nzp_serv = 26 and dat_charge is null " + Raions + Streets + Houses +
                       " GROUP BY 2";
            }
            else
            {
                if (MonthS == 12)
                {
                    MonthS = 0;
                    YearS = 2016;
                }
                chargeTable = localPref + "_charge_" + (YearS - 2000).ToString("00") + ".charge_" + (MonthS+1).ToString("00");
                sql = " SELECT sum(sum_money) as sum_money, k.nzp_kvar" +
                        " FROM " + pref + "_data.kvar k " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                        " INNER JOIN " + chargeTable + " c on c.nzp_kvar = k.nzp_kvar " +
                        " where nzp_serv = 26 and dat_charge is null " + Raions + Streets + Houses +
                        " GROUP BY 2";
            }
            var dt2 = ExecSQLToTable(sql);
            
            DataTable d1 = new DataTable();
            d1.Columns.Add("address");
            d1.Columns.Add("flat");
            d1.Columns.Add("num_ls");
            d1.Columns.Add("serv");
            d1.Columns.Add("sum_charge", typeof(decimal));
            d1.Columns.Add("sum_rcl", typeof(decimal));
            d1.Columns.Add("sum_outsaldo", typeof(decimal));
            d1.Columns.Add("sum_money", typeof(decimal));
            d1.Columns.Add("nzp_kvar");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row;
                row = d1.NewRow();
                row["address"] = dt.Rows[i]["address"];
                row["flat"] = dt.Rows[i]["flat"];
                row["num_ls"] = dt.Rows[i]["num_ls"];
                row["serv"] = dt.Rows[i]["serv"];
                row["sum_charge"] = dt.Rows[i]["sum_charge"];
                row["sum_rcl"] = dt.Rows[i]["sum_rcl"];
                row["sum_outsaldo"] = dt.Rows[i]["sum_outsaldo"];
                row["sum_money"] = dt.Rows[i]["sum_money"];
                row["nzp_kvar"] = dt.Rows[i]["nzp_kvar"];
                d1.Rows.Add(row);
            }
            for (int i = 0; i < d1.Rows.Count; i++)
            {
                for (int j = 0; j < dt2.Rows.Count; j++)
                {
                    if (d1.Rows[i]["nzp_kvar"].ToString() == dt2.Rows[j]["nzp_kvar"].ToString())
                    {
                        d1.Rows[i]["sum_money"] = Convert.ToDecimal(d1.Rows[i]["sum_money"]) + Convert.ToDecimal(dt2.Rows[j]["sum_money"]);
                        if (YearS == 2015 && MonthS == 9)
                        {
                            if (dt2.Rows[j]["mon"].ToString() == "10")
                            {
                                d1.Rows[i]["sum_outsaldo"] = Convert.ToDecimal(d1.Rows[i]["sum_outsaldo"]) - Convert.ToDecimal(dt2.Rows[j]["sum_money"]);
                            }
                        }
                        else
                        {
                            d1.Rows[i]["sum_outsaldo"] = Convert.ToDecimal(d1.Rows[i]["sum_outsaldo"]) - Convert.ToDecimal(dt2.Rows[j]["sum_money"]);
                        }
                    }
                }
                
            }
            for (int i = 0; i < d1.Rows.Count; i++)
            {
                if (YearS == 2015 && MonthS == 9)
                {

                }
                
            }
            d1.TableName = "Q_master";
            ds.Tables.Add(d1);
            return ds;
        }


       

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь", "Январь"};
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS);        
        }
        
        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            AddressParameterValue adr = JsonConvert.DeserializeObject<AddressParameterValue>(UserParamValues["Address"].Value.ToString());
            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = "and r.nzp_raj in (" + String.Join(",", adr.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = "and u.nzp_ul in (" + String.Join(",", adr.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                    Houses = "and d.nzp_dom in (" + String.Join(",", goodHouses.Select(x => "" + x + "").ToArray()) + ") ";
            }
    
        }
        protected override void CreateTempTable()
        {
                string sql = " create temp table sel_kvar(" +
                    " nzp_kvar integer, " +
                    " ikvar integer, " +
                    " nzp_geu integer, " +
                    " nzp_area integer) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);
            
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table sel_kvar ", true);
        }     
    }
}

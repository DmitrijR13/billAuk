using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.MariEl.Properties;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.MariEl.Reports
{
    class Report120114 : BaseSqlReport
    {
        public override string Name
        {
            get { return "12.1.14 Выписка из ЛС"; }
        }

        public override string Description
        {
            get { return "Выписка из ЛС"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }
        public override bool IsPreview
        {
            get { return false; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_12_1_14; }
        }




        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Кому выдана</summary>
        protected string Whom { get; set; }

        /// <summary>Кому выдана</summary>
        protected string Adres { get; set; }

        /// <summary>Количество проживающих</summary>
        protected string KolGil { get; set; }

        /// <summary>Жилая площадь</summary>
        protected string GilPl { get; set; }

        /// <summary>Общая площадь</summary>
        protected string ObPl { get; set; }



        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>()
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new StringParameter { Code="Vidana", Name = "Выдана "},
            };
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            Whom = UserParamValues["Vidana"].GetValue<string>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("ls", ReportParams.NzpObject);
            report.SetParameterValue("fio", Whom.Trim());
            report.SetParameterValue("adres", Adres);
            report.SetParameterValue("kol_pr", KolGil.Trim());
            report.SetParameterValue("ob_pl", ObPl.Trim());
            report.SetParameterValue("gil_pl", GilPl.Trim());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("username", ReportParams.User.uname);
        }

        public override DataSet GetData()
        {
            MyDataReader reader ;

            var months = new[] {"","янв","фев","мар","апр","май","июн","июл","авг","сен","окт","ноя","дек"};

            var sql = " SELECT pref " +
                      " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject;
            ExecRead(out reader, sql);

            if(reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                for (int i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    string charge = pref + "_charge_" + (year - 2000).ToString("00") + 
                        DBManager.tableDelimiter + "charge_" + month.ToString("00");
                    string period = months[month] + "." + (year - 2000).ToString("00");

                    sql = 
                        " INSERT INTO t_report_12_1_12( " +
                                " period, yearr, monthh, sum_insaldo," +
                                " sum_real_hvs, sum_real_vo, sum_real_tbo, sum_real_sj, sum_real_otop, sum_real_total," +
                                " reval, real_charge, sum_nach, sum_money, sum_itogo)" +
                        " SELECT '" + period + "', " + year + ", " + month + ", c.sum_insaldo," +
                                " (CASE WHEN c.nzp_serv = 6 THEN c.sum_real ELSE 0 END) AS sum_real_hvs, " +
                                " (CASE WHEN c.nzp_serv = 7 THEN c.sum_real ELSE 0 END) AS sum_real_vo, " +
                                " (CASE WHEN c.nzp_serv = 16 THEN c.sum_real ELSE 0 END) AS sum_real_tbo, " +
                                " (CASE WHEN c.nzp_serv = 2 THEN c.sum_real ELSE 0 END) AS sum_real_sj, " +
                                " (CASE WHEN c.nzp_serv = 8 THEN c.sum_real ELSE 0 END) AS sum_real_otop, " +
                                " c.sum_real AS sum_real_total, " +
                        //" (CASE c.nzp_serv IN (6,7,16,2,8) THEN c.sum_real ELSE 0 END) AS sum_real_total, " +
                                " c.reval, c.real_charge, c.sum_real + c.reval + c.real_charge, c.sum_money," +
                                " c.sum_insaldo + c.sum_real + c.reval + c.real_charge - c.sum_money" +
                        " FROM " + charge + " c " +
                        " WHERE c.nzp_kvar =" + ReportParams.NzpObject + 
                                " AND c.nzp_serv > 1 AND c.dat_charge is null";
                    ExecSQL(sql);
                }

                #region заполнение параметров

                sql = 
                    " SELECT k.fio, " +
                    " " + DBManager.sNvlWord + "(trim(t.town)||', ','')||" + DBManager.sNvlWord + "(trim(r.rajon)||', ','')||" +
                    "' ул.'||" + DBManager.sNvlWord + "(trim(u.ulica)||', ','')||" +
                    "' д.'||" + DBManager.sNvlWord + "(trim(d.ndom),'')||'/'||" + DBManager.sNvlWord + "(trim(d.nkor),'')||" +
                    "' кв.'||" + DBManager.sNvlWord + "(trim(k.nkvar),'') as adres" +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar k" +
                    " LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "dom d ON d.nzp_dom = k.nzp_dom" +
                    " LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "s_ulica u ON u.nzp_ul = d.nzp_ul" +
                    " LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "s_rajon r ON r.nzp_raj = u.nzp_raj" +
                    " LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "s_town t ON t.nzp_town = r.nzp_town" +
                    " WHERE k.nzp_kvar =" + ReportParams.NzpObject;
                DataTable dt = ExecSQLToTable(sql);
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["adres"] != DBNull.Value)
                        Adres = dt.Rows[0]["adres"].ToString();
                    if (string.IsNullOrEmpty(Whom) && dt.Rows[0]["fio"] != DBNull.Value)
                        Whom = dt.Rows[0]["fio"].ToString();
                }

                DateTime operDay = Points.DateOper;
                string gilXX = pref + "_charge_" + (operDay.Year - 2000).ToString("00") +
                    DBManager.tableDelimiter + "gil_" + operDay.Month.ToString("00");

                sql = 
                    " SELECT sum(g.cnt2) as countGil " +
                    " FROM " + gilXX + " g " +
                    " WHERE g.stek=3 and g.nzp_kvar=" + ReportParams.NzpObject;
                dt = ExecSQLToTable(sql); 
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["countGil"] != DBNull.Value)
                        KolGil = dt.Rows[0]["countGil"].ToString();
                }

                sql = " SELECT val_prm as obpl" +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p" +
                      " WHERE p.nzp_prm = 4" +
                      "        AND nzp=" + ReportParams.NzpObject +
                      "        AND is_actual=1  " +
                      "        AND dat_s<='01." + operDay.Month.ToString("00") + "." + operDay.Year + "'" +
                      "        AND dat_po>='01." + operDay.Month.ToString("00") + "." + operDay.Year + "'" +
                      " ORDER BY dat_s desc LIMIT 1";
                dt = ExecSQLToTable(sql);
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["obpl"] != DBNull.Value)
                        ObPl = dt.Rows[0]["obpl"].ToString();
                }

                sql = " SELECT val_prm as gilpl" +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p" +
                      " WHERE p.nzp_prm = 6" +
                      "        AND nzp=" + ReportParams.NzpObject +
                      "        AND is_actual=1  " +
                      "        AND dat_s<='01." + operDay.Month.ToString("00") + "." + operDay.Year + "'" +
                      "        AND dat_po>='01." + operDay.Month.ToString("00") + "." + operDay.Year + "'" +
                      " ORDER BY dat_s desc LIMIT 1";
                dt = ExecSQLToTable(sql);
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["gilpl"] != DBNull.Value)
                        GilPl = dt.Rows[0]["gilpl"].ToString();
                }
          
                #endregion
            }

            sql = " SELECT period, yearr, monthh, SUM(sum_insaldo) as sum_insaldo, SUM(sum_real_hvs) as sum_real_hvs," +
                         " SUM(sum_real_vo) as sum_real_vo, SUM(sum_real_tbo) as sum_real_tbo," +
                         " SUM(sum_real_sj) as sum_real_sj, SUM(sum_real_otop) as sum_real_otop," +
                         " SUM(sum_real_total) as sum_real_total, SUM(reval) as reval," +
                         " SUM(real_charge) as real_charge, SUM(sum_nach) as sum_nach," +
                         " SUM(sum_money) as sum_money, SUM(sum_itogo) as sum_itogo" +
                  " FROM t_report_12_1_12 " +
                  " GROUP BY 1,2,3" +
                  " ORDER BY 2,3";
            var tempTable = ExecSQLToTable(sql);
            tempTable.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(tempTable);
            return ds;
        }

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_report_12_1_12( " +
                               " period varchar(6)," +
                               " yearr integer," +
                               " monthh integer," +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real_hvs " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real_vo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real_tbo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real_sj " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real_otop " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real_total " + DBManager.sDecimalType + "(14,2)," +
                               " reval " + DBManager.sDecimalType + "(14,2)," +
                               " real_charge " + DBManager.sDecimalType + "(14,2)," +
                               " sum_nach " + DBManager.sDecimalType + "(14,2)," +
                               " sum_money " + DBManager.sDecimalType + "(14,2)," +
                               " sum_itogo " + DBManager.sDecimalType + "(14,2)" +
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_12_1_12");
        }
    }
}

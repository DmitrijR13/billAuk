using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Sakha.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
    public class Report_14_2_5 : BaseSqlReport
    {
        public override string Name
        {
            get { return "14.2.5 Счет-извещение"; }
        }

        public override string Description
        {
            get { return "2.5 Счет-извещение"; }
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

        protected override byte[] Template
        {
            get { return Resources.Report_14_2_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Адрес</summary>
        protected string Adr { get; set; }

        /// <summary>УК</summary>
        protected string Area { get; set; }

        /// <summary>Код адреса</summary>
        protected string KodAdr { get; set; }

        /// <summary>Общая площадь</summary>
        protected decimal ObSquare { get; set; }

        /// <summary>Общая площадь</summary>
        protected decimal Peni { get; set; }

        /// <summary>Количество зарегистрированных</summary>
        protected int KolGil { get; set; }

        /// <summary>Количество проживающих</summary>
        protected int KolLivGil { get; set; }

        /// <summary>
        /// название временной таблицы
        /// </summary>
        private string table = "schet_izv_14_2_5";
        private string tablePrm = "schet_izv_14_2_5_prm";


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year }
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            string str_date = "за " + months[Month] + " месяц " + Year;

            report.SetParameterValue("str_date", str_date);
            report.SetParameterValue("num_date", "01." + Month.ToString("00") + "." + Year);
            report.SetParameterValue("kod_adr", KodAdr);
            report.SetParameterValue("total_sq", ObSquare);
            report.SetParameterValue("more_soc_sq", 0);
            report.SetParameterValue("kol_zareg", KolGil);
            report.SetParameterValue("fact_prozh", KolLivGil);
            report.SetParameterValue("report_date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("area", Area);
            report.SetParameterValue("adr", Adr);
            report.SetParameterValue("peni", Peni == 0 ? "" : Peni.ToString()); 
        }

        public override DataSet GetData()
        {
            string pref;
            string sql = 
                " SELECT k.pref, k.nkvar, k.pkod, d.ndom, d.nkor, u.ulica, a.area " +
                " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k," +
                ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                " WHERE k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul" +
                " AND a.nzp_area = k.nzp_area " +
                " AND k.nzp_kvar = " + ReportParams.NzpObject;
            DataTable dtkvar = ExecSQLToTable(sql);
            if (dtkvar.Rows.Count > 0)
            {
                pref = dtkvar.Rows[0]["pref"].ToString().Trim();

                Adr = dtkvar.Rows[0]["ulica"].ToString().Trim() + " дом " + dtkvar.Rows[0]["ndom"].ToString().Trim() +
                    "/" + dtkvar.Rows[0]["nkor"].ToString().Trim() + " кв." + dtkvar.Rows[0]["nkvar"].ToString().Trim();
                KodAdr = dtkvar.Rows[0]["pkod"] == DBNull.Value ? "0" : dtkvar.Rows[0]["pkod"].ToString();
                Area = dtkvar.Rows[0]["area"].ToString().Trim();
            }
            else
            {
                throw new DataException("Ошибка получения данных из БД - нет данных по ЛС");
            }

            sql =
                " INSERT INTO " + tablePrm +
                " (nzp_prm, val_prm)" +
                " SELECT nzp_prm, val_prm " +
                " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                " WHERE nzp=" + ReportParams.NzpObject +
                "        AND is_actual <> 100 " +
                "        AND dat_s <= (" + "'01." + Month.ToString("00") + "." + Year + "'" + DBManager.sConvToDate + ")" +
                "        AND dat_po >= (" + "'01." + Month.ToString("00") + "." + Year + "'" + DBManager.sConvToDate + ")" +
                "        AND nzp_prm in (4,5,10,131)";
            ExecSQL(sql);

            sql = " SELECT p1.val_prm as ob_s, " +
                  " p2.val_prm as kol_gil, " +
                  " p3.val_prm as vr_vib, " +
                  " p4.val_prm as vr_pr " +
                  " FROM " + tablePrm + " p1 " +
                  " LEFT OUTER JOIN " + tablePrm + " p2 on p2.nzp_prm = 5 " +
                  " LEFT OUTER JOIN " + tablePrm + " p3 on p3.nzp_prm = 10 " +
                  " LEFT OUTER JOIN " + tablePrm + " p4 on p4.nzp_prm = 131 " +
                  " WHERE p1.nzp_prm = 4";
            DataTable dtprm = ExecSQLToTable(sql);
            if (dtprm.Rows.Count > 0)
            {
                ObSquare = dtprm.Rows[0]["ob_s"] == DBNull.Value ? 0 : Convert.ToDecimal(dtprm.Rows[0]["ob_s"]);
                KolGil = dtprm.Rows[0]["kol_gil"] == DBNull.Value ? 0 : dtprm.Rows[0]["kol_gil"].ToInt();
                KolLivGil = 
                    (dtprm.Rows[0]["kol_gil"] == DBNull.Value ? 0 : dtprm.Rows[0]["kol_gil"].ToInt()) - 
                    (dtprm.Rows[0]["vr_vib"] == DBNull.Value ? 0 : dtprm.Rows[0]["vr_vib"].ToInt()) +
                    (dtprm.Rows[0]["vr_pr"] == DBNull.Value ? 0 : dtprm.Rows[0]["vr_pr"].ToInt());
            }
            else
            {
                ObSquare = KolGil = KolLivGil = 0;
            }

            string tarif = pref + DBManager.sDataAliasRest + "tarif";
            string charge = pref + "_charge_" + Year.ToString().Substring(2,2) + 
                DBManager.tableDelimiter + "charge_" + Month.ToString("00");

            sql =
                " INSERT INTO " + table + " (nzp_supp, nzp_serv, serv, payer_supp, tarif) " + 
                " SELECT su.nzp_supp, s.nzp_serv, s.service, p.payer, t.tarif " +
                " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services s," +
                ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p," +
                ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su," + 
                tarif + " t " +
                " WHERE" +
                " t.nzp_serv = s.nzp_serv AND p.nzp_payer = su.nzp_payer_supp" +
                " AND su.nzp_supp = t.nzp_supp" +
                " AND t.nzp_kvar =" + ReportParams.NzpObject + " AND t.is_actual <> 100 " +
                " AND t.dat_s <= (" + "'01." + Month.ToString("00") + "." + Year + "'" + DBManager.sConvToDate + ")" +
                " AND t.dat_po >= (" + "'01." + Month.ToString("00") + "." + Year + "'" + DBManager.sConvToDate + ")";
            ExecSQL(sql);

            sql = 
                " UPDATE " + table + 
                " SET (sum_insaldo, sum_tarif, removal, sum_real, sum_money, sum_charge) = " +

                " ((SELECT c.sum_insaldo" +
                " FROM " + charge + " c " +
                " WHERE c.nzp_kvar = " + ReportParams.NzpObject +
                " AND c.nzp_supp = " + table + ".nzp_supp " +
                " AND c.nzp_serv = " + table + ".nzp_serv)," +

                " (SELECT c.sum_tarif" +
                " FROM " + charge + " c " +
                " WHERE c.nzp_kvar = " + ReportParams.NzpObject +
                " AND c.nzp_supp = " + table + ".nzp_supp " +
                " AND c.nzp_serv = " + table + ".nzp_serv)," +

                " (SELECT c.real_charge + c.izm_saldo + c.reval" +
                " FROM " + charge + " c " +
                " WHERE c.nzp_kvar = " + ReportParams.NzpObject +
                " AND c.nzp_supp = " + table + ".nzp_supp " +
                " AND c.nzp_serv = " + table + ".nzp_serv)," +

                " (SELECT c.sum_real" +
                " FROM " + charge + " c " +
                " WHERE c.nzp_kvar = " + ReportParams.NzpObject +
                " AND c.nzp_supp = " + table + ".nzp_supp " +
                " AND c.nzp_serv = " + table + ".nzp_serv)," +

                " (SELECT c.sum_money" +
                " FROM " + charge + " c " +
                " WHERE c.nzp_kvar = " + ReportParams.NzpObject +
                " AND c.nzp_supp = " + table + ".nzp_supp " +
                " AND c.nzp_serv = " + table + ".nzp_serv)," +

                "(SELECT c.sum_charge" +
                " FROM " + charge + " c " +
                " WHERE c.nzp_kvar = " + ReportParams.NzpObject +
                " AND c.nzp_supp = " + table + ".nzp_supp " +
                " AND c.nzp_serv = " + table + ".nzp_serv)) ";
            ExecSQL(sql);

            sql = 
                " SELECT " + DBManager.sNvlWord + "(SUM(sum_charge),0) as peni FROM " + charge +
                " WHERE nzp_kvar = " + ReportParams.NzpObject +
                " AND nzp_serv = 500";
            DataTable dtpeni = ExecSQLToTable(sql);
            Peni = dtpeni.Rows.Count > 0 ? dtpeni.Rows[0]["peni"].ToDecimal() : 0;
            
            
            sql = " SELECT DISTINCT serv, payer_supp, tarif, sum_insaldo, sum_tarif, removal, sum_real, sum_money, sum_charge " +
                  " FROM " + table;

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" CREATE TEMP TABLE " + table + " ( ");
            sql.Append(" nzp_supp INTEGER, ");
            sql.Append(" nzp_serv INTEGER, ");
            sql.Append(" serv CHARACTER(100), ");
            sql.Append(" area CHARACTER(40), ");
            sql.Append(" payer_supp CHARACTER(40), ");
            sql.Append(" tarif  " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" removal " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_real " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable); 

            ExecSQL(sql.ToString());

            sql = new StringBuilder();

            sql.Append(" CREATE TEMP TABLE " + tablePrm + " ( ");
            sql.Append(" nzp_prm INTEGER, ");
            sql.Append(" val_prm CHARACTER(10))" + DBManager.sUnlogTempTable);

            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE " + table + " ");
            ExecSQL(" DROP TABLE " + tablePrm + " ");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Configuration;
using System.Web.UI.WebControls;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Bars.KP50.Utils;


namespace Bars.KP50.Report.Tula.Reports
{
    public class Report710128 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.28 Расшифровка расхода  "; }
        }

        public override string Description
        {
            get { return "Расшифровка расхода"; }
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
            get { return Resources.Report_71_1_28; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        /// <summary>Заголовок территории</summary>
        protected string nzp_dom { get; set; }
        protected string FIO { get; set; }
        protected string Post { get; set; }
        protected double tsgil { get; set; }
        protected double opu_potr { get; set; }
        protected double ng_potr { get; set; }
        protected double potr { get; set; }
        protected double ipu_potr { get; set; }
        protected double sum_reval { get; set; }

        /// <summary>Расчетный месяц</summary>
        private int Month { get; set; }

        /// <summary>Расчетный год</summary>
        private int Year { get; set; }

        /// <summary>Услуга</summary>
        private int Service { get; set; }

        /// <summary>Адрес</summary>
        private string AddressHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new ComboBoxParameter(false) {
                    Name = "Услуга", 
                    Code = "Service",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 1, Name = "Горячая вода"},
                        new { Id = 2, Name = "Холодная вода"},
                        new { Id = 3, Name = "Электроснабжение"}
                    }
                },
                new StringParameter{Name="Должность", Code = "Post"},
                new StringParameter{Name="ФИО", Code = "FIO"}
            };
        }


        public override DataSet GetData()
        { 
            #region Выборка дома и локального банка

            string sql =
                " SELECT d.nzp_dom, (case when rajon ='-' then ''||trim(town) else trim(town)||', '||trim(rajon) end) as rajon, " +
                " trim(" + DBManager.sNvlWord + "(ulicareg,'ул.'))||' '||trim(ulica) as ulica," +
                " ndom, (case when " + DBManager.sNvlWord +
                "(nkor,'-') ='-' then '' else 'к.'||trim(nkor) end) as nkor, d.pref " +
                " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d ON k.nzp_dom=d.nzp_dom" +
                " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town t ON t.nzp_town = r.nzp_town" +
                " WHERE nzp_kvar=" + ReportParams.NzpObject;

            MyDataReader reader;

            ExecRead(out reader, sql);
            string pref="";
            while (reader.Read())
            {
                if (reader["pref"] !=DBNull.Value) pref=reader["pref"].ToString().ToLower().Trim();
                if (reader["nzp_dom"] != DBNull.Value) nzp_dom=reader["nzp_dom"].ToString().Trim();
                AddressHeader = reader["rajon"].ToString() + " " + reader["ulica"].ToString() + " " +reader["ndom"].ToString() + " "+reader["nkor"].ToString();
            }
            reader.Close();
            
            #endregion

            #region Выборка по локальным банкам

            string serv="0", iserv="",oserv="";
            switch (Service)
            {
                case 1:
                    serv = "9,513";
                    iserv = "9";
                    oserv = "513";
                    break;
                case 2:
                    serv="6,510";
                    iserv = "6";
                    oserv = "510";
                    break;
                case 3:
                    serv="25,516";
                    iserv = "25";
                    oserv = "516";
                    break;
            }

            sql = " INSERT INTO t_svod (nzp_kvar, nzp_serv, rashod, squ, kolgil ) " +
                  " SELECT cl.nzp_kvar, nzp_serv, rashod_full, squ, round(gil) " +
                  " FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" +
                  Month.ToString("00") + " cl" +
                  " WHERE stek=3 and cl.nzp_dom= " + nzp_dom +
                  " and nzp_serv in (" + serv + ")";
            ExecSQL(sql);

            sql = " INSERT INTO t_svod (nzp_kvar, nzp_serv, rashod_rub, reval, sum_charge ) " +
                  " SELECT ch.nzp_kvar, nzp_serv, sum_tarif, reval, sum_charge " +
                  " FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" +
                  Month.ToString("00") + " ch" +
                  " WHERE dat_charge is null " +
                  " and nzp_kvar in (select nzp_kvar from " + pref + DBManager.sDataAliasRest + "kvar WHERE nzp_dom= " +
                  nzp_dom + ")" +
                  " and nzp_serv in (" + serv + ")";
                ExecSQL(sql);

                sql = " INSERT INTO t_svod (nzp_kvar, val_s, val_po, rashod_odn, c_reval) " +
                  " SELECT ch.nzp_kvar, val_s, val_po, dop87, dlt_reval " +
                  " FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "counters_" +
                  Month.ToString("00") + " ch " +
                  " WHERE dat_charge is null and stek=3 and nzp_type=3 " +
                  " and nzp_dom= " + nzp_dom + 
                  " and nzp_serv = " + iserv + "";
            ExecSQL(sql);


            #endregion

            #region Выборка на экран

            sql = " SELECT val1, val2, val4, val3-val4 as val3,  dlt_reval, kf_dpu_kg " +
                  " FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "counters_" +
                  Month.ToString("00") + " ch " +
                  " WHERE stek=3 and  nzp_type=1 and dat_charge is null and nzp_serv in(" + serv +
                  ") and nzp_dom=" + nzp_dom;
            ExecRead(out reader, sql);
            double kf_dpu_kg = 0;
            while (reader.Read())
            {
                if (reader["val4"] != DBNull.Value) opu_potr = Convert.ToDouble(reader["val4"]);
                if (reader["val3"] != DBNull.Value) ng_potr = Convert.ToDouble(reader["val3"]);
                if (reader["val1"] != DBNull.Value) potr = Convert.ToDouble(reader["val1"]);
                if (reader["val2"] != DBNull.Value) ipu_potr = Convert.ToDouble(reader["val2"]);
                if (reader["dlt_reval"] != DBNull.Value) sum_reval = Convert.ToDouble(reader["dlt_reval"]);
                if (reader["kf_dpu_kg"] != DBNull.Value) kf_dpu_kg = Convert.ToDouble(reader["kf_dpu_kg"]) ;
            }
            reader.Close();

            int b = kf_dpu_kg > 0 ? 1 : -1; 

            var t =
                ExecScalar("SELECT sum(case when " + b +
                           "<0 then kolgil else squ end) FROM t_svod WHERE nzp_serv in(" +
                           iserv + ")");
            tsgil = t != DBNull.Value ? Convert.ToDouble(t) : 0;

            sql = " SELECT  ikvar, k.num_ls, nkvar, " +
                  " ( case when nkvar_n='' or " + DBManager.sNvlWord +
                  " (nkvar_n,'-') ='-' then '' else 'к.'||trim(nkvar_n)  end) as nkvar_n," +
                  " max( case when " + b + "<0 then kolgil else squ end ) as sqgil," +
                  " sum( case when nzp_serv=" + iserv + " then rashod end ) as rashod_ls," +
                  " sum(rashod_odn) as rashod_od," +
                  " sum(case when nzp_serv=" + iserv + " then rashod_rub end ) as rashod_ls_rub," +
                  " sum(case when nzp_serv=" + oserv + " then rashod_rub end ) as rashod_od_rub," +
                  " sum(reval) as reval," +
                  " sum(c_reval) as c_reval," +
                  " max(val_s) as val_s," +
                  " max(val_po) as val_po," +
                  " sum(sum_charge) as sum_charge " +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                  " LEFT OUTER JOIN t_svod a ON a.nzp_kvar=k.nzp_kvar " +
                  " WHERE  k.nzp_dom=" + nzp_dom +
                  " group BY 1,2,3,4 " +
                  " ORDER BY 1,3 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }   
 
        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
				 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
				 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("pMonth", months[Month]);
            report.SetParameterValue("pYear", Year);

            report.SetParameterValue("adr", AddressHeader);
            switch (Service)
            {
              case 1:
                    report.SetParameterValue("service", "горячей воды");
                    break;
             case 2:
                    report.SetParameterValue("service", "холодной воды");
                    break;              
                case 3:
                    report.SetParameterValue("service", "электроэнергии");
                    break;  
            }
            report.SetParameterValue("ed", Service == 3 ? "кВт*час" : "куб.м");
            report.SetParameterValue("Post", Post);
            report.SetParameterValue("FIO", FIO);
            report.SetParameterValue("tsgil", tsgil);
            report.SetParameterValue("opu_potr", opu_potr);
            report.SetParameterValue("ng_potr", ng_potr);
            report.SetParameterValue("potr", potr);
            report.SetParameterValue("ipu_potr", ipu_potr);
            report.SetParameterValue("sum_reval", sum_reval);

        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Service = UserParamValues["Service"].GetValue<int>();
            Post= UserParamValues["Post"].GetValue<string>();
            FIO = UserParamValues["FIO"].GetValue<string>();
            if (Month == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный месяц\"");
            }

            if (Year == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный год\"");
            }
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_svod (  " +
                               " nzp_kvar integer default 0," +
                               " nzp_serv integer default 0," +
                               " kolgil integer default 0," +
                               " rashod " + DBManager.sDecimalType + "(15,7) default 0.00, " +
                               " rashod_odn " + DBManager.sDecimalType + "(15,7) default 0.00, " +
                               " rashod_rub " + DBManager.sDecimalType + "(15,7) default 0.00, " +
                               " squ " + DBManager.sDecimalType + "(14,7) default 0.00, " +
                               " reval " + DBManager.sDecimalType + "(14,7) default 0.00, " +
                               " c_reval " + DBManager.sDecimalType + "(14,7) default 0.00, " +
                               " sum_charge " + DBManager.sDecimalType + "(14,7) default 0.00, " +
                               " val_s " + DBManager.sDecimalType + "(14,1) default 0.00, " +
                               " val_po " + DBManager.sDecimalType + "(14,1) default 0.00, " +
                               " sum_nach " + DBManager.sDecimalType + "(14,7) default 0.00 " +
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}

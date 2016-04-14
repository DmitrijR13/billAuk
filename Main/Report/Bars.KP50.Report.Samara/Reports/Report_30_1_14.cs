using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using FastReport.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report30114 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.14 Реестр неприватизированных лицевых счетов"; }
        }

        public override string Description
        {
            get { return "Реестр неприватизированных лицевых счетов"; }
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

        protected override byte[] Template
        {
            get { return Resources.Report_30_1_14; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Районы</summary>
        protected string RaionsHeader { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }

        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Платежный код администрации</summary>
        protected string Pkod { get; set; }

        /// <summary>Начислено Администрации</summary>
        protected decimal SumTicket { get; set; }

        /// <summary>Начислено Администрации</summary>
        protected decimal SumTicket5 { get; set; }

        /// <summary>Начислено Администрации</summary>
        protected decimal SumTicket6 { get; set; }

        /// <summary>Начислено Администрации</summary>
        protected decimal SumArea { get; set; }

        /// <summary>Начислено Администрации</summary>
        protected decimal SumArea5 { get; set; }

        /// <summary>Начислено Администрации</summary>
        protected decimal SumArea6 { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = DateTime.Today.Year },
                new SupplierAndBankParameter(),
                //new AddressParameter(),
                new StringParameter{Code ="Pkod", Name = "Пл. код администрации"}
            };
        }

        protected override void PrepareParams()
        {


            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].GetValue<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].GetValue<int>();
            Pkod = UserParamValues["Pkod"].GetValue<string>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;

        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            var months2 = new[] {"","Января","Февраля",
                 "Марта","Апреля","Мая","Июня","Июля","Августа","Сентября",
                 "Октября","Ноября","Декабря"};
            if (MonthS == MonthPo && YearS == YearPo)
            {
                report.SetParameterValue("period", months[MonthS] + " " + YearS + "г.");
            }
            else
            {
                report.SetParameterValue("period", "период с " + months2[MonthS] + " " + YearS + "г. по " + months[MonthPo] + " " + YearPo + "г.");
            }
            report.SetParameterValue("raion", RaionsHeader);
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("pkod", Pkod == null ? "" : "№ 00 " + Pkod);
            report.SetParameterValue("calc_date", months[Points.CalcMonth.month_] + " " + Points.CalcMonth.year_ + "г. ");
            report.SetParameterValue("tsum_real", SumTicket);
            report.SetParameterValue("t5sum_real", SumTicket5);
            report.SetParameterValue("t6sum_real", SumTicket6);
            report.SetParameterValue("tpl_kvar", SumArea);
            report.SetParameterValue("t5pl_kvar", SumArea5);
            report.SetParameterValue("t6pl_kvar", SumArea6);
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel AS pref, point " +
                         " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();
            string whereSupp = GetWhereSupp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                string bank = reader["point"].ToStr().Trim();

                int monthCount = (YearPo * 12 + MonthPo) - (YearS * 12 + MonthS) + 1;

                string chargeTable = pref + "_charge_" + (Points.CalcMonth.year_ - 2000).ToString("00") +
                                     DBManager.tableDelimiter + "charge_" + Points.CalcMonth.month_.ToString("00");

                if (TempTableInWebCashe(chargeTable))
                {

                    sql = "CREATE TEMP TABLE t_prms( " +
                          " nzp_kvar integer, " +
                          " nzp_dom integer, " +
                          " typ integer, " +
                          " komf char(20), " +
                          " nzp_status integer, " +
                          " floor integer)" + DBManager.sUnlogTempTable;
                    ExecSQL(sql);

                    sql = " INSERT INTO t_prms(nzp_dom, nzp_kvar, typ, komf, nzp_status)" +
                          " SELECT k.nzp_dom, k.nzp_kvar, typek," +
                          " MAX(case when " + DBManager.sNvlWord + "(nzp_prm,0) = 3 " +
                          "     AND " + DBManager.sNvlWord +
                          "         (val_prm,'')='2' then 'коммунальная' else 'изолированная' end)," +
                          " MAX(case when " + DBManager.sNvlWord + "(nzp_prm,0) = 8 " +
                          "     AND " + DBManager.sNvlWord + "(val_prm,'')='1' then 1 else 0 end) " +
                          " FROM " +
                          pref + DBManager.sDataAliasRest + "s_ulica u," +
                          pref + DBManager.sDataAliasRest + "dom d," +
                          pref + DBManager.sDataAliasRest + "kvar k LEFT OUTER JOIN " +
                          pref + DBManager.sDataAliasRest + "prm_1 p" +
                          " ON k.nzp_kvar = p.nzp " +
                          "     AND p.nzp_prm in (3,8) and p.is_actual = 1 " +
                          "     AND dat_s <= '" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) + "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          "     AND dat_po >= '01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " WHERE u.nzp_ul = d.nzp_ul " +
                          "     AND d.nzp_dom = k.nzp_dom " +
                          " GROUP BY 1,2,3 ";
                    ExecSQL(sql);

                    ExecSQL("CREATE INDEX ix_tmpprm_01 ON t_prms(nzp_dom)");
                    ExecSQL("CREATE INDEX ix_tmpprm_02 ON t_prms(nzp_kvar, nzp_status)");
                    ExecSQL(DBManager.sUpdStat + " t_prms");

                    sql = " UPDATE t_prms SET floor = (SELECT MAX(val_prm" + DBManager.sConvToInt + ")" +
                          "                            FROM " + pref + DBManager.sDataAliasRest + "prm_2 p" +
                          " WHERE t_prms.nzp_dom=p.nzp  " +
                          "     AND nzp_prm=37 " +
                          "     AND is_actual=1" +
                          "     AND dat_s <= '" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) + "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          "     AND dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_reestr(bank, nzp_supp, nzp_dom, nzp_kvar, typ, " +
                          "         sum_real,  tarif,  pl_kvar, komf, status, floor) " +
                          " SELECT '" + bank + "' as bank, nzp_supp, " +
                          " b.nzp_dom, a.nzp_kvar, typ, a.sum_real * " + monthCount + ", a.tarif, c_calc, b.komf, 'Неприватизированная', floor " +
                          " FROM t_prms b, " + chargeTable + " a " +
                          " WHERE dat_charge is null " +
                          "     AND nzp_serv = 268 and tarif > 0 " +
                          "     AND a.nzp_kvar = b.nzp_kvar " +
                          "     AND b.nzp_status = 0 " + whereSupp;
                    ExecSQL(sql);
                    var delete = ExecSQLToTable("select * from t_reestr");
                    delete = ExecSQLToTable("select * from t_prms");

                    sql = " UPDATE t_reestr SET sobstw_name = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 1324 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET contract_num = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 1322 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET contract_date = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 1323 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET inn = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 445 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET kpp = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 870 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET bik = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 1326 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET bank_name = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 1328 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET ras_schet = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 1325 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    sql = " UPDATE t_reestr SET korr_schet = (SELECT MAX(val_prm) FROM " +
                            pref + DBManager.sDataAliasRest + " prm_11 p " +
                          " WHERE p.nzp = t_reestr.nzp_supp " +
                          " AND " + DBManager.sNvlWord + "(nzp_prm,0) = 1327 " +
                          " AND p.is_actual=1 " +
                          " AND p.dat_s<='" + DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) +
                          "." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "'" +
                          " AND p.dat_po>='01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_ + "')";
                    ExecSQL(sql);

                    delete = ExecSQLToTable("select * from t_reestr");
                    ExecSQL(sql);
                    ExecSQL("DROP TABLE t_prms");
                }
                
            }

            reader.Close();
            #endregion

            sql = " INSERT INTO t_totals (nzp_supp, t5pl_kvar, t6pl_kvar, tpl_kvar, t5sum_real, t6sum_real, tsum_real) " +
                 " SELECT nzp_supp, " +
                 " SUM(CASE WHEN tarif=5.07 THEN pl_kvar END) as t5pl_kvar, " +
                 " SUM(CASE WHEN tarif=5.84 THEN pl_kvar END) as t6pl_kvar, " +
                 " SUM(pl_kvar) as tpl_kvar, " +
                 " SUM(CASE WHEN tarif=5.07 THEN sum_real END) as t5sum_real, " +
                 " SUM(CASE WHEN tarif=5.84 THEN sum_real END) as t6sum_real, " +
                 " SUM(sum_real) as tsum_real " +
                 " FROM t_reestr " +
                 " GROUP BY 1 ";
            ExecSQL(sql);

            sql = " SELECT bank, name_supp, sobstw_name, " +
                  " CASE WHEN trim(contract_num) <> '' THEN '№ '||contract_num END as contract_num, contract_date, " +
                          " inn, kpp, bik, bank_name, ras_schet, korr_schet, " +
                  " CASE WHEN rajon='-' THEN trim(town) ELSE trim(rajon) END as raion, " +
                  "        'ул.'||ulica as ulica, idom, ndom, " +
                  "        CASE WHEN nkor<>'-' THEN nkor END as nkor, ikvar, " +
                  "        CASE WHEN nkvar<>'-' AND nkvar<>'0' THEN nkvar END as nkvar, fio, " +
                  "        tarif, sum_real, pl_kvar, komf, status, " + DBManager.sNvlWord + "(floor,0) as floor, " +
                  "        (case when tarif=5.07 then 1 " +
                  "             when tarif=5.84 then 2 else 0 end ) as kod_tarif, " +
                  " (case when typ=1 then 'жилое' else 'нежилое' end) as typ, " +
                  " t5pl_kvar, t6pl_kvar, tpl_kvar, t5sum_real, t6sum_real, tsum_real " +
                  " FROM t_reestr v, t_totals tot, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                  " WHERE v.nzp_kvar = k.nzp_kvar " +
                  "     AND k.nzp_dom = d.nzp_dom " +
                  "     AND d.nzp_ul = u.nzp_ul " +
                  "     AND u.nzp_raj = r.nzp_raj " +
                  "     AND r.nzp_town = t.nzp_town " +
                  "     AND s.nzp_supp = v.nzp_supp " +
                  "     AND tot.nzp_supp = v.nzp_supp " +
                  " ORDER BY 1,2,3,4,5,6,7";
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
        private string GetWhereAdr()
        {
            return "";
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
            return whereSupp;
        }



        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
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

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_reestr ( " +
                                " bank char(100), " +
                                " nzp_supp integer, " +
                                " nzp_kvar integer, " +
                                " nzp_dom integer, " +
                                " typ integer, " +
                                " pl_kvar " + DBManager.sDecimalType + "(14,2), " +
                                " tarif " + DBManager.sDecimalType + "(14,2), " +
                                " sum_real " + DBManager.sDecimalType + "(14,2), " +
                                " komf char(20), " +
                                " status char(20), " +
                                " floor integer, " +
                                " sobstw_name char(250), " +
                                " contract_num char(200), " +
                                " contract_date char(50), " +
                                " inn char(30), " +
                                " kpp char(30), " +
                                " bik char(30), " +
                                " bank_name char(200), " +
                                " ras_schet char(100), " +
                                " korr_schet char(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = " CREATE TEMP TABLE t_totals ( " +
                     " nzp_supp integer, " +
                     " t5pl_kvar char(100), " +
                     " t6pl_kvar char(100), " +
                     " tpl_kvar char(100), " +
                     " t5sum_real char(100), " +
                     " t6sum_real char(100), " +
                     " tsum_real char(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_reestr ");
            ExecSQL(" DROP TABLE t_totals ");
        }
    }
}

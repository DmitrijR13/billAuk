using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Bars.KP50.Report;
using Bars.KP50.Report.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Castle.MicroKernel.Registration;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

using System.Globalization;
//using Bars.KP50.Utils;
using Castle.Core.Internal;
using Constants = STCLINE.KP50.Global.Constants;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;

namespace Bars.KP50.Report.Samara.Reports
{
    /// <summary>Пример написания отчета</summary>
    class ReportLsCharge : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Выписка"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по распределению платежей"; }
        }

        /// <summary>К каким группам относится отчет, определяет подсистему из которой доступен отчет</summary>
        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        /// <summary>Вид отчета</summary>
        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>
        /// Предпросмотрт.
        /// Если true, то отчет принудительно формируется в формате fpx и выводится пользователю на просмотр.
        /// </summary>
        public override bool IsPreview
        {
            get { return false; }
        }

        /// <summary>Шаблон отчета</summary>
        protected override byte[] Template
        {
            get { return Resources.ReportLsCharge; }
        }

        #region Значения параметров отчета

        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

        /// <summary>Значение параметра "Год"</summary>
        protected int YearFrom { get; set; }

        /// <summary>Значение параметра "Месяц"</summary>
        protected int MonthFrom { get; set; }

        /// <summary>Значение параметра "Год"</summary>
        protected int YearTo { get; set; }

        /// <summary>Значение параметра "Месяц"</summary>
        protected int MonthTo { get; set; }

        /// <summary>Значение параметра "Квартира"</summary>
        protected string Nkvar { get; set; }

        /// <summary>Значение параметра "Комната"</summary>
        protected string Nkvar_n { get; set; }

        /// <summary>Значение параметра "PKOD"</summary>
        protected string Pkod { get; set; }
        #endregion

        /// <summary>
        /// Пользовательские параметры.
        /// Отображаются на форме печати.
        /// </summary>
        /// <returns>Список пользовательских параметров</returns>
        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new YearParameter { Code = "YearFrom", Name = "Год от", Value = DateTime.Today.Year },
                new MonthParameter { Code = "MonthFrom", Name = "Месяц от", Value = DateTime.Today.Month },
                new YearParameter { Code = "YearTo", Name = "Год до", Value = DateTime.Today.Year },
                new MonthParameter { Code = "MonthTo", Name = "Месяц до", Value = DateTime.Today.Month },
                new AddressParameter(),
                new StringParameter{Code ="Nkvar", Name = "квартира"},
                new StringParameter{Code ="Nkvar_n", Name = "комната"},
                new StringParameter{Code ="Pkod", Name = "платежный код"}
            };
        }

        /// <summary>
        /// Осносной метод по формированию данных для генерации отчета. 
        /// </summary>
        /// <returns>Заполненный DataSet</returns>
        public override DataSet GetData()
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            var pref = "fbill";
            var localPref = "bill01";
            string dataTable = pref + "_data";

            if (Nkvar_n == null || Nkvar_n == " " || Nkvar_n == String.Empty)
                Nkvar_n = "-";
            string sql = "";
            string part_sql = "";
            part_sql += Nkvar == "" ? " 1=1 " : " upper(k.nkvar) = upper('" + Nkvar + "') ";
            if (Nkvar != "")
            {
                part_sql += Nkvar_n == "-"
                    ? " AND (upper(k.nkvar_n) = upper('" + Nkvar_n +
                      "') or nkvar_n is null or replace(k.nkvar_n, ' ','') = '')"
                    : " AND (upper(k.nkvar_n) = upper('" + Nkvar_n + "') or  2=1)";
            }

            if (Pkod == "")
            {
                sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar, town || ', ' || rajon || ',' || ulica || ' д.' || ndom || ' кв. ' || nkvar || ' ком. ' || CASE WHEN nkvar_n is null THEN '' ELSE nkvar_n END as address, num_ls || '' as num_ls, fio
                FROM " + localPref + DBManager.sDataAliasRest + "kvar k " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_town t on t.nzp_town = r.nzp_town " +
                      "where 1=1 " + Raions + Streets + Houses + " AND " + part_sql;
            }
            else
            {
                sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar, town || ', ' || rajon || ',' || ulica || ' д.' || ndom || ' кв. ' || nkvar || ' ком. ' || CASE WHEN nkvar_n is null THEN '' ELSE nkvar_n END as address, num_ls || '' as num_ls, fio
                FROM " + localPref + DBManager.sDataAliasRest + "kvar k " +
                        "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_town t on t.nzp_town = r.nzp_town " +
                    "where pkod = " + Pkod;
            }
            ExecSQL(sql.ToString());

            List<Int32> servList = new List<int>(){2,6,7,8,10,14,15,17,22,25,26,210,500,510,513,514,515,516,100021,100022};

            if (YearFrom == YearTo)
            {
                for (int i = MonthFrom; i <= MonthTo; i++)
                {
                    string month = months[i] + " " + YearTo;
                    sql = @"INSERT INTO t_svod_pack
                        SELECT c.nzp_kvar, '" + month + @"', 0 as people_count, c.sum_insaldo, s2.sum_charge as serv2, s6.sum_charge as serv6, s6.c_calc as serv6_1, s7.sum_charge as serv7, s8.sum_charge as serv8, s10.sum_charge as serv10, 
                        s14.sum_charge as serv14, s14.c_calc as serv14_1, s15.sum_charge as serv15, s17.sum_charge as serv17, s22.sum_charge as serv22, s25.sum_charge as serv25,
                        s25.c_calc as serv25_1, s26.sum_charge as serv26, s210.sum_charge as serv210, s500.sum_charge as serv500, s510.sum_charge as serv510, s513.sum_charge as serv513, s514.sum_charge as serv514,
                        s515.sum_charge as serv515, s516.sum_charge as serv516, s100021.sum_charge as serv100021, s100022.sum_charge as serv100022, 
                        s2.sum_charge + s6.sum_charge + s7.sum_charge + s8.sum_charge + s10.sum_charge + s14.sum_charge + s15.sum_charge + s17.sum_charge + s22.sum_charge + s25.sum_charge + 
                        s26.sum_charge + s210.sum_charge + s500.sum_charge + s513.sum_charge + s514.sum_charge + s515.sum_charge + s516.sum_charge + s100021.sum_charge + s100022.sum_charge as sum_charge,
                        c.sum_money, c.sum_outsaldo, ttk.address, ttk.fio";
                    sql +=
                        " FROM (SELECT sum_insaldo, sum_outsaldo, sum_money, nzp_kvar FROM bill01_charge_" + (YearFrom - 2000).ToString("00")+ ".charge_" + i.ToString("00") + " where nzp_serv = 1) c " +
                        " INNER JOIN t_temp_kvar ttk on ttk.nzp_kvar = c.nzp_kvar ";
                    foreach (var serv in servList)
                    {
                        if (serv == 6 || serv == 14 || serv == 25)
                        {
                            sql +=
                                " LEFT JOIN (SELECT sum(rsum_tarif) + sum(reval) - sum(sum_nedop) + coalesce(sum(p.sum_rcl), 0) as sum_charge, max(c_calc) as c_calc, c.nzp_kvar FROM bill01_charge_" +
                                (YearFrom - 2000).ToString("00") + ".charge_" + i.ToString("00") +
                                " c LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_rcl) as sum_rcl from bill01_charge_" +
                                (YearFrom - 2000).ToString("00") + ".perekidka " +
                                " where month_ = " + i.ToString("00") + " AND nzp_serv = " + serv +
                                " group by 1,2) p on p.nzp_kvar = c.nzp_kvar and p.nzp_serv = c.nzp_serv " +
                                " where c.nzp_serv = " + serv + " and dat_charge is null group by 3) s" + serv + " on s" + serv +
                                ".nzp_kvar = c.nzp_kvar ";
                        }
                        else
                        {
                            sql +=
                                " LEFT JOIN (SELECT sum(rsum_tarif) + sum(reval) - sum(sum_nedop) + coalesce(sum(p.sum_rcl), 0) as sum_charge, c.nzp_kvar FROM bill01_charge_" +
                                (YearFrom - 2000).ToString("00") + ".charge_" + i.ToString("00") +
                                " c LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_rcl) as sum_rcl from bill01_charge_" +
                                (YearFrom - 2000).ToString("00") + ".perekidka " +
                                " where month_ = " + i.ToString("00") + " AND nzp_serv = " + serv +
                                " group by 1,2) p on p.nzp_kvar = c.nzp_kvar and p.nzp_serv = c.nzp_serv " +
                                " where c.nzp_serv = " + serv + " and dat_charge is null group by 2) s" + serv + " on s" + serv +
                                ".nzp_kvar = c.nzp_kvar ";
                        }
                    }
                    sql += " where c.nzp_kvar in (SELECT nzp_kvar from t_temp_kvar)";
                    ExecSQL(sql.ToString());
                }
            }

            var ds = new DataSet();
            var dt2 = ExecSQLToTable("SELECT * FROM t_svod_pack");
            dt2.TableName = "Q_master";
            ds.Tables.Add(dt2);
            return ds;
        }

        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }


        /// <summary>
        /// Метод для обработки значений пользовательских параметров.
        /// Вызывается перед методом GetData()
        /// </summary>
        protected override void PrepareParams()
        {
            YearFrom = UserParamValues["YearFrom"].GetValue<int>();
            MonthFrom = UserParamValues["MonthFrom"].GetValue<int>();
            YearTo = UserParamValues["YearTo"].GetValue<int>();
            MonthTo = UserParamValues["MonthTo"].GetValue<int>();
            Nkvar = UserParamValues["Nkvar"].GetValue<string>() ?? String.Empty;
            Nkvar_n = UserParamValues["Nkvar_n"].GetValue<string>() ?? String.Empty;
            Pkod = UserParamValues["Pkod"].GetValue<string>() ?? String.Empty;

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

        /// <summary>Подготовить отчет, например, добавить параметры вызова отчета, произвести другие действия перед сохранением</summary>
        /// <param name="report">Отчет</param>
        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("date", DateTime.Today.ToShortDateString());
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            ExecSQL("create temp table t_svod_pack (nzp_kvar INTEGER, month CHARACTER(100)," +
                "people_count CHARACTER(10)," +
                "sum_insaldo NUMERIC(14,2) default 0," +
                "serv2 NUMERIC(14,2) default 0," +
                "serv6 NUMERIC(14,2) default 0," +
                "serv6_1 NUMERIC(14,2) default 0," +
                "serv7 NUMERIC(14,2) default 0," +
                "serv8 NUMERIC(14,2) default 0," +
                "serv10 NUMERIC(14,2) default 0," +
                "serv14 NUMERIC(14,2) default 0," +
                "serv14_1 NUMERIC(14,2) default 0," +
                "serv15 NUMERIC(14,2) default 0," +
                "serv17 NUMERIC(14,2) default 0," +
                "serv22 NUMERIC(14,2) default 0," +
                "serv25 NUMERIC(14,2) default 0," +
                "serv25_1 NUMERIC(14,2) default 0," +
                "serv26 NUMERIC(14,2) default 0," +
                "serv210 NUMERIC(14,2) default 0," +
                "serv500 NUMERIC(14,2) default 0," +
                "serv510 NUMERIC(14,2) default 0," +
                "serv513 NUMERIC(14,2) default 0," +
                "serv514 NUMERIC(14,2) default 0," +
                "serv515 NUMERIC(14,2) default 0," +
                "serv516 NUMERIC(14,2) default 0," +
                "serv100021 NUMERIC(14,2) default 0," +
                "serv100022 NUMERIC(14,2) default 0," +
                "sum_charge NUMERIC(14,2) default 0," +
                "sum_prih NUMERIC(14,2) default 0," +
                "sum_outsaldo NUMERIC(14,2) default 0, address CHARACTER(100),fio CHARACTER(100))");

            ExecSQL("create temp table t_temp_kvar (nzp_kvar INTEGER, address CHARACTER(100),num_ls CHARACTER(12),fio CHARACTER(100))");
        }

        /// <summary>
        /// Удаление временных таблиц.
        /// Вызывается после метода GetData()
        /// </summary>
        protected override void DropTempTable()
        {
            ExecSQL("drop table t_svod_pack");
            ExecSQL("drop table t_temp_kvar");
        }
    }
}
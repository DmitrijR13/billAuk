using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
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
    class ReportCommCharge : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Отчет по начислению и оплате"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по начислению и оплате"; }
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
            get { return Resources.ReportCommCharge; }
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
                SELECT k.nzp_kvar as nzp_kvar, town || ', ' || rajon || ',' || ulica || ' д.' || ndom || ' кв. ' || nkvar || ' ком. ' || CASE WHEN nkvar_n is null THEN '' ELSE nkvar_n END as address, num_ls || '' as num_ls, fio, p.val_prm
                FROM " + localPref + DBManager.sDataAliasRest + "kvar k " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_town t on t.nzp_town = r.nzp_town " +
                      " LEFT JOIN (SELECT nzp, max(val_prm) as val_prm from bill01_data.prm_1 where is_actual = 1 AND Extract(year from dat_po) = 3000 AND nzp_prm = 5 group by 1) p on p.nzp = k.nzp_kvar " +
                      "where 1=1 " + Raions + Streets + Houses + " AND " + part_sql;
            }
            else
            {
                sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar, town || ', ' || rajon || ',' || ulica || ' д.' || ndom || ' кв. ' || nkvar || ' ком. ' || CASE WHEN nkvar_n is null THEN '' ELSE nkvar_n END as address, num_ls || '' as num_ls, fio, p.val_prm
                FROM " + localPref + DBManager.sDataAliasRest + "kvar k " +
                        "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_town t on t.nzp_town = r.nzp_town " +
                      " LEFT JOIN (SELECT nzp, max(val_prm) as val_prm from bill01_data.prm_1 where is_actual = 1 AND Extract(year from dat_po) = 3000 AND nzp_prm = 5 group by 1) p on p.nzp = k.nzp_kvar " +
                    "where pkod = " + Pkod;
            }
            ExecSQL(sql.ToString());

            List<Int32> servList = new List<int>(){2,6,7,8,9,10,14,15,17,22,25,26,210,500,510,515,516,100021,100022};

            if (YearFrom == YearTo)
            {
                for (int i = MonthFrom; i <= MonthTo; i++)
                {
                    string month = i.ToString().PadLeft(2, '0') + "." + YearTo;
                    sql = @"INSERT INTO t_svod_pack
                        SELECT c.nzp_kvar, '" + month + @"', " + i + @", p.val_prm as people_count, c.sum_insaldo, s2.sum_charge as serv2, s6.sum_charge as serv6, s6.c_calc as serv6_1, s7.sum_charge as serv7, s8.sum_charge as serv8, s10.sum_charge as serv10, 
                        s14.sum_charge as serv14, s14.c_calc as serv14_1, s15.sum_charge as serv15, s17.sum_charge as serv17, s22.sum_charge as serv22, s25.sum_charge as serv25,
                        s25.c_calc as serv25_1, s26.sum_charge as serv26, s210.sum_charge as serv210, s500.sum_charge as serv500, s510.sum_charge as serv510, s9.odn as serv513, s14.odn as serv514,
                        s515.sum_charge as serv515, s516.sum_charge as serv516, s100021.sum_charge as serv100021, s100022.sum_charge as serv100022, s9.sum_charge as serv9, s9.c_calc as serv9_1,
                        coalesce(s2.sum_charge, 0) + coalesce(s6.sum_charge,0) + coalesce(s7.sum_charge ,0) +  coalesce(s8.sum_charge, 0) + coalesce(s10.sum_charge, 0) + 
                        coalesce(s14.sum_charge,0) + coalesce(s15.sum_charge,0) + coalesce(s17.sum_charge,0) + coalesce(s22.sum_charge,0) + coalesce(s25.sum_charge,0) + 
                        coalesce(s26.sum_charge,0) + coalesce(s210.sum_charge,0) + coalesce(s500.sum_charge,0) + coalesce(s510.sum_charge,0) + coalesce(s9.odn,0) + coalesce(s14.odn,0) +
                        coalesce(s515.sum_charge,0) + coalesce(s516.sum_charge,0) + coalesce(s100021.sum_charge,0) + coalesce(s100022.sum_charge,0) + coalesce(s9.sum_charge,0) 
                        as sum_charge, c.sum_money, c.sum_outsaldo, ttk.address, ttk.fio, p.val_prm";
                    sql +=
                        " FROM (SELECT sum_insaldo, sum_outsaldo, sum_money, nzp_kvar FROM bill01_charge_" +
                        (YearFrom - 2000).ToString("00") + ".charge_" + i.ToString("00") + " where nzp_serv = 1) c " +
                        " INNER JOIN t_temp_kvar ttk on ttk.nzp_kvar = c.nzp_kvar " +
                        " LEFT JOIN (SELECT nzp, max(val_prm) as val_prm from bill01_data.prm_1 where is_actual = 1 AND dat_po >= '" +
                        YearFrom + "-" +
                        i + "-01' AND dat_s < '" + YearFrom + "-" + (i + 1).ToString() +
                        "-01' AND nzp_prm = 5 group by 1) p on p.nzp = c.nzp_kvar ";
                    foreach (var serv in servList)
                    {
                        if (serv == 9 || serv == 14)
                        {
                            sql +=
                                " LEFT JOIN (SELECT max(c.tarif) * max(c.valm) + sum(c1.reval) - sum(c1.sum_nedop) + coalesce(sum(p.sum_rcl), 0) as sum_charge, max(c.valm) as c_calc, max(c.tarif) * max(c.dop87) as odn, c.nzp_kvar FROM bill01_charge_" +
                                (YearFrom - 2000).ToString("00") + ".calc_gku_" + i.ToString("00") +
                                " c INNER JOIN bill01_charge_" +
                                (YearFrom - 2000).ToString("00") + ".charge_" + i.ToString("00") +
                                " c1 on c1.nzp_serv = c.nzp_serv and c.nzp_kvar = c1.nzp_kvar and c1.nzp_serv = " + serv + " and c1.dat_charge is null LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_rcl) as sum_rcl from bill01_charge_" +
                                (YearFrom - 2000).ToString("00") + ".perekidka " +
                                " where month_ = " + i.ToString("00") + " AND nzp_serv = " + serv +
                                " group by 1,2) p on p.nzp_kvar = c.nzp_kvar and p.nzp_serv = c.nzp_serv " +
                                " where c.nzp_serv = " + serv + " and c.dat_charge is null and stek = 3 group by 4) s" + serv + " on s" + serv +
                                ".nzp_kvar = c.nzp_kvar ";
                        }
                        else if (serv == 6 || serv == 25)
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
                    ExecSQL(sql);

                    sql = @"UPDATE t_svod_pack set sum_charge = coalesce(serv2,0) + coalesce(serv6,0) + coalesce(serv7,0) + coalesce(serv8,0) + coalesce(serv10,0) + coalesce(serv14,0) 
                            + coalesce(serv15,0) + coalesce(serv17,0) + coalesce(serv22,0) + coalesce(serv25,0) + coalesce(serv26,0) + coalesce(serv210,0) + coalesce(serv500, 0) 
                            + coalesce(serv510,0) + coalesce(serv513,0) + coalesce(serv514,0) + coalesce(serv515,0) + coalesce(serv516,0) + coalesce(serv100021,0) 
                            + coalesce(serv100022,0) + coalesce(serv9,0) where month = '" + month + @"'";
                    ExecSQL(sql);
                    if (i == 9)
                    {
                        sql = "UPDATE t_svod_pack SET sum_prih = (SELECT sum_money FROM bill01_charge_" + (YearFrom - 2000).ToString("00") + ".charge_" + i.ToString("00")
                           + " t where nzp_serv = 1 and t.nzp_kvar = t_svod_pack.nzp_kvar) where month = '" + i.ToString().PadLeft(2, '0') + "." + YearTo + "'";
                        ExecSQL(sql);
                        sql = "UPDATE t_svod_pack SET sum_prih = sum_prih + (SELECT sum_money FROM bill01_charge_" + (YearFrom - 2000).ToString("00") + ".charge_" + (i + 1).ToString("00")
                           + " t where nzp_serv = 1 and t.nzp_kvar = t_svod_pack.nzp_kvar) where month = '" + i.ToString().PadLeft(2, '0') + "." + YearTo + "'";
                        ExecSQL(sql);
                    }
                    else
                    {
                        sql = "UPDATE t_svod_pack SET sum_prih = (SELECT sum_money FROM bill01_charge_" + (YearFrom - 2000).ToString("00") + ".charge_" + (i + 1).ToString("00")
                            + " t where nzp_serv = 1 and t.nzp_kvar = t_svod_pack.nzp_kvar) where month = '" + i.ToString().PadLeft(2, '0') + "." + YearTo + "'";
                        ExecSQL(sql);
                    }
                }
            }

            var ds = new DataSet();
            var dt2 = ExecSQLToTable("SELECT * FROM t_svod_pack order by address, mon_");
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
            ExecSQL("create temp table t_svod_pack (nzp_kvar INTEGER, month CHARACTER(100), mon_ INTEGER, " +
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
                "serv9 NUMERIC(14,2) default 0," +
                "serv9_1 NUMERIC(14,4) default 0," +
                "sum_charge NUMERIC(14,2) default 0," +
                "sum_prih NUMERIC(14,2) default 0," +
                "sum_outsaldo NUMERIC(14,2) default 0, address CHARACTER(100),fio CHARACTER(100), val_prm CHARACTER(8))");

            ExecSQL("create temp table t_temp_kvar (nzp_kvar INTEGER, address CHARACTER(100),num_ls CHARACTER(12),fio CHARACTER(100), val_prm CHARACTER(8))");
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
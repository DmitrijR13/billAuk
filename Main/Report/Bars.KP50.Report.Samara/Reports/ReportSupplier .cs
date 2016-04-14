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

namespace Bars.KP50.Report.Samara.Reports
{
    /// <summary>Пример написания отчета</summary>
    class ReportSupplier : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Справка по поставщикам коммунальных услуг"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование справки по поставщикам коммунальных услуг"; }
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
            get { return Resources.ReportSupplier; }
        }

        #region Значения параметров отчета

        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

        /// <summary>Значение параметра "Месяц"</summary>
        protected int MonthFrom { get; set; }

        /// <summary>Значение параметра "Месяц"</summary>
        protected int MonthTo { get; set; }

        /// <summary>Значение параметра "Год"</summary>
        protected int Year { get; set; }


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
                new MonthParameter { Code = "MonthFrom", Name = "Месяц от", Value = DateTime.Today.Month },
                new YearParameter { Code = "Year", Name = "Год", Value = DateTime.Today.Year },
                new MonthParameter { Code = "MonthTo", Name = "Месяц до", Value = DateTime.Today.Month },
                new AddressParameter()
            };
        }

        /// <summary>
        /// Осносной метод по формированию данных для генерации отчета. 
        /// </summary>
        /// <returns>Заполненный DataSet</returns>
        public override DataSet GetData()
        {
            var pref = "bill01";
            string sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar
                FROM " + pref + DBManager.sDataAliasRest + "kvar k " +
                "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                "where 1=1 " + Raions + Streets + Houses;
            ExecSQL(sql.ToString());
            if (MonthTo == MonthFrom)
            {
                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" + MonthTo.ToString("00");
                sql = @"insert into t_temp_serv(name_supp, nzp_supp, service, nzp_serv) 
                        SELECT su.name_supp, su.nzp_supp, s.service, s.nzp_serv 
 FROM " + chargeTable + @" c 
 INNER JOIN " + pref + DBManager.sKernelAliasRest + @"services s on s.nzp_serv = c.nzp_serv 
 INNER JOIN " + pref + DBManager.sKernelAliasRest + @"supplier su on su.nzp_supp = c.nzp_supp 
 where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND c.nzp_serv not in (1, 500)";
                ExecSQL(sql.ToString());
            }
            else
            {
                for (int i = MonthFrom; i <= MonthTo; i++)
                {
                    string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" + i.ToString("00");
                    sql = @"insert into t_temp_serv(name_supp, nzp_supp, service, nzp_serv) 
                        SELECT su.name_supp, su.nzp_supp, s.service, s.nzp_serv 
 FROM " + chargeTable + @" c 
 INNER JOIN " + pref + DBManager.sKernelAliasRest + @"services s on s.nzp_serv = c.nzp_serv 
 INNER JOIN " + pref + DBManager.sKernelAliasRest + @"supplier su on su.nzp_supp = c.nzp_supp 
 where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND c.nzp_serv not in (1, 500)";
                    ExecSQL(sql.ToString());
                }
            }

            sql = @"insert into t_svod(name_supp, nzp_supp, service, nzp_serv) 
 SELECT name_supp, nzp_supp, service, nzp_serv FROM t_temp_serv GROUP BY name_supp, nzp_supp, service, nzp_serv ORDER BY name_supp, service";
            ExecSQL(sql.ToString());
            if (MonthTo == MonthFrom)
            {
                
                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" + MonthTo.ToString("00");
                sql = @"UPDATE t_svod SET sum_charge_" + MonthTo.ToString() + 
                    @" = (SELECT sum(sum_charge) as sum_charge 
 FROM " + chargeTable + @" c 
 where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND nzp_serv not in (1, 500)  
 AND c.nzp_serv = t_svod.nzp_serv AND c.nzp_supp = t_svod.nzp_supp 
 group by nzp_supp, nzp_serv)";
                ExecSQL(sql.ToString());
                sql = @"UPDATE t_svod SET sum_money_" + MonthTo.ToString() + 
                    @" = (SELECT sum(sum_money) as sum_money 
 FROM " + chargeTable + @" c 
 where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND nzp_serv not in (1, 500) 
 AND c.nzp_serv = t_svod.nzp_serv AND c.nzp_supp = t_svod.nzp_supp 
 group by nzp_supp, nzp_serv)";
                ExecSQL(sql.ToString());
            }
            else
            {
                for (int i = MonthFrom; i <= MonthTo; i++)
                {
                    string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" + i.ToString("00");
//                    if (i == 2 && Year == 2015)
//                    {
//                        sql = @"UPDATE t_svod SET sum_charge_" + i.ToString() +
//                       @" = (SELECT sum(sum_charge) as sum_charge 
// FROM " + chargeTable + @" c 
// where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND nzp_serv not in (1, 500) 
// AND c.nzp_serv = t_svod.nzp_serv AND c.nzp_supp = t_svod.nzp_supp 
// group by nzp_supp, nzp_serv)";
//                        ExecSQL(sql.ToString());
//                        sql = @"UPDATE t_svod SET sum_charge_" + i.ToString() +
//                      @" = sum_charge_" + i.ToString() + @" + (SELECT sum(reval) as reval 
// FROM " + chargeTable + @" c 
// where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND nzp_serv in (8, 15, 100018) and dat_charge is null
// AND c.nzp_serv = t_svod.nzp_serv AND c.nzp_supp = t_svod.nzp_supp 
// group by nzp_supp, nzp_serv)";
//                        ExecSQL(sql.ToString());
//                        sql = @"UPDATE t_svod SET sum_money_" + i.ToString() +
//                            @" = (SELECT sum(sum_money) as sum_money 
// FROM " + chargeTable + @" c 
// where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND nzp_serv not in (1, 500)  
// AND c.nzp_serv = t_svod.nzp_serv AND c.nzp_supp = t_svod.nzp_supp 
// group by nzp_supp, nzp_serv)";
//                        ExecSQL(sql.ToString());
//                    }
                        sql = @"UPDATE t_svod SET sum_charge_" + i.ToString() +
                        @" = (SELECT sum(sum_charge) as sum_charge 
 FROM " + chargeTable + @" c 
 where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND nzp_serv not in (1, 500) 
 AND c.nzp_serv = t_svod.nzp_serv AND c.nzp_supp = t_svod.nzp_supp 
 group by nzp_supp, nzp_serv)";
                        ExecSQL(sql.ToString());
                        sql = @"UPDATE t_svod SET sum_money_" + i.ToString() +
                            @" = (SELECT sum(sum_money) as sum_money 
 FROM " + chargeTable + @" c 
 where nzp_kvar in(SELECT nzp_kvar FROM t_temp_kvar) AND nzp_serv not in (1, 500)  
 AND c.nzp_serv = t_svod.nzp_serv AND c.nzp_supp = t_svod.nzp_supp 
 group by nzp_supp, nzp_serv)";
                        ExecSQL(sql.ToString());
                    }
            }

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 2338.64 where nzp_serv = 25 AND nzp_supp = 101184";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 7192.29 where nzp_serv = 9 AND nzp_supp = 101185";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 2545.5 where nzp_serv = 14 AND nzp_supp = 101185";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 2.78 where nzp_serv = 100018 AND nzp_supp = 101179";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 1450.01 where nzp_serv = 15 AND nzp_supp = 101179";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 1711.46 where nzp_serv = 9 AND nzp_supp = 101178";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 1102.55 where nzp_serv = 8 AND nzp_supp = 101178";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 361.41 where nzp_serv = 14 AND nzp_supp = 101178";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_2  = sum_charge_2 - 608.28 where nzp_serv = 25 AND nzp_supp = 101188";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET sum_charge_total = sum_charge_1 + sum_charge_2 + sum_charge_3 + sum_charge_4 + sum_charge_5
                    +sum_charge_6 + sum_charge_7 + sum_charge_8 + sum_charge_9 + sum_charge_10 + sum_charge_11 +sum_charge_12,
                    sum_money_total = sum_money_1 + sum_money_2 + sum_money_3 + sum_money_4 + sum_money_5 + sum_money_6 + 
                    sum_money_7 + sum_money_8 + sum_money_9 + sum_money_10 + sum_money_11 + sum_money_12";
            ExecSQL(sql.ToString());
            var ds = new DataSet();

            string sql2 = "SELECT * FROM t_svod ORDER BY name_supp, service";
            var dt = ExecSQLToTable(sql2.ToString());
            dt.TableName = "Q_master";
            ds.Tables.Add(dt);

            sql2 = @"SELECT service, sum(sum_charge_1) as sum_charge_1, sum(sum_charge_2) as sum_charge_2, sum(sum_charge_3) as sum_charge_3,
sum(sum_charge_4) as sum_charge_4, sum(sum_charge_5) as sum_charge_5, sum(sum_charge_6) as sum_charge_6, sum(sum_charge_7) as sum_charge_7,
sum(sum_charge_8) as sum_charge_8, sum(sum_charge_9) as sum_charge_9, sum(sum_charge_10) as sum_charge_10, sum(sum_charge_11) as sum_charge_11,
sum(sum_charge_12) as sum_charge_12, sum(sum_money_1) as sum_money_1, sum(sum_money_2) as sum_money_2, sum(sum_money_3) as sum_money_3,
sum(sum_money_4) as sum_money_4, sum(sum_money_5) as sum_money_5, sum(sum_money_6) as sum_money_6, sum(sum_money_7) as sum_money_7,
sum(sum_money_8) as sum_money_8, sum(sum_money_9) as sum_money_9, sum(sum_money_10) as sum_money_10, sum(sum_money_11) as sum_money_11,
sum(sum_money_12) as sum_money_12, sum(sum_charge_total) as sum_charge_total, sum(sum_money_total) as sum_money_total 
FROM t_svod GROUP BY 1";
            var dt2 = ExecSQLToTable(sql2.ToString());
            dt2.TableName = "Q_master2";
            ds.Tables.Add(dt2);
            return ds;
        }

        /// <summary>
        /// Метод для обработки значений пользовательских параметров.
        /// Вызывается перед методом GetData()
        /// </summary>
        protected override void PrepareParams()
        {
            MonthFrom = UserParamValues["MonthFrom"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            MonthTo = UserParamValues["MonthTo"].GetValue<int>();

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

            report.SetParameterValue("year", Year);
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            ExecSQL("create temp table t_svod (name_supp CHARACTER(80), " +
                "nzp_supp INTEGER," +
                "service CHARACTER(80)," +
                "nzp_serv INTEGER," +
                "sum_charge_1 NUMERIC(14,2) default 0," +
                "sum_money_1 NUMERIC(14,2) default 0," +
                "sum_charge_2 NUMERIC(14,2) default 0," +
                "sum_money_2 NUMERIC(14,2) default 0," +
                "sum_charge_3 NUMERIC(14,2) default 0," +
                "sum_money_3 NUMERIC(14,2) default 0," +
                "sum_charge_4 NUMERIC(14,2) default 0," +
                "sum_money_4 NUMERIC(14,2) default 0," +
                "sum_charge_5 NUMERIC(14,2) default 0," +
                "sum_money_5 NUMERIC(14,2) default 0," +
                "sum_charge_6 NUMERIC(14,2) default 0," +
                "sum_money_6 NUMERIC(14,2) default 0," +
                "sum_charge_7 NUMERIC(14,2) default 0," +
                "sum_money_7 NUMERIC(14,2) default 0," +
                "sum_charge_8 NUMERIC(14,2) default 0," +
                "sum_money_8 NUMERIC(14,2) default 0," +
                "sum_charge_9 NUMERIC(14,2) default 0," +
                "sum_money_9 NUMERIC(14,2) default 0," +
                "sum_charge_10 NUMERIC(14,2) default 0," +
                "sum_money_10 NUMERIC(14,2) default 0," +
                "sum_charge_11 NUMERIC(14,2) default 0," +
                "sum_money_11 NUMERIC(14,2) default 0," +
                "sum_charge_12 NUMERIC(14,2) default 0," +
                "sum_money_12 NUMERIC(14,2) default 0," +
                "sum_charge_total NUMERIC(14,2) default 0," +
                "sum_money_total NUMERIC(14,2) default 0)");

            ExecSQL("create temp table t_temp_serv (name_supp CHARACTER(80), " +
               "nzp_supp INTEGER," +
               "service CHARACTER(80)," +
               "nzp_serv INTEGER)");

            ExecSQL("create temp table t_temp_kvar (nzp_kvar INTEGER)");
        }

        /// <summary>
        /// Удаление временных таблиц.
        /// Вызывается после метода GetData()
        /// </summary>
        protected override void DropTempTable()
        {
            ExecSQL("drop table t_svod");
            ExecSQL("drop table t_temp_kvar");
            ExecSQL("drop table t_temp_serv");
        }
    }
}
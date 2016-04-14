using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using Newtonsoft.Json;
using System.Linq;
using STCLINE.KP50.DataBase;

using System.Globalization;
//using Bars.KP50.Utils;
using Castle.Core.Internal;
using Constants = STCLINE.KP50.Global.Constants;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;

namespace Bars.KP50.Report.Samara.Reports
{
    /// <summary>Пример написания отчета</summary>
    class ReportChargePrihServ : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Начисления по контрактам в разрезе услуг"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по начислению по контрактам в разрезе услуг"; }
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
            get { return Resources.ReportChargePrihServ; }
        }

        #region Значения параметров отчета
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

        /// <summary>Параметры</summary>
        private List<long> ServType { get; set; }
        #endregion

        /// <summary>
        /// Пользовательские параметры.
        /// Отображаются на форме печати.
        /// </summary>
        /// <returns>Список пользовательских параметров</returns>
        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new AddressParameter(),
                 new ComboBoxParameter(true)             
                {
                    Code = "Serv",
                    Name = "Услуги",
                    Value = "Холодная вода",
                    StoreData = new List<object>
                    {
                        new { Id = "6", Name = "Холодная вода"},
                        new { Id = "9", Name = "Горячая вода" },
                        new { Id = "14", Name = "Холодная вода для нужд ГВС" },
                        new { Id = "25", Name = "Электроснабжение" },
                        new { Id = "210", Name = "Электроснабжение ночное" }
                    }
                },
            };
        }

        /// <summary>
        /// Осносной метод по формированию данных для генерации отчета. 
        /// </summary>
        /// <returns>Заполненный DataSet</returns>
        public override DataSet GetData()
        {
            var pref = "fbill";
            var localPref = "bill01";
            //string finTable = pref + "_fin_" + (YearS - 2000).ToString("00");
            //string dataTable = pref + "_data";

            string sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar, town || ', ' || rajon || ',' || ulica || ' д.' || ndom as address, 
                town || ', ' || rajon || ',' || ulica || ' д.' || ndom || ' кв. ' || nkvar || ' ком. ' || CASE WHEN nkvar_n is null THEN '' ELSE nkvar_n END as kvar, 
                num_ls || '' as num_ls, fio, ikvar
                FROM " + pref + DBManager.sDataAliasRest + "kvar k " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_town t on t.nzp_town = r.nzp_town " +
                      "where 1=1 " + Raions + Streets + Houses;

            ExecSQL(sql);

            String month = MonthS.ToString().PadLeft(2, '0');
            Int32 year = YearS;
            Int32 days = DateTime.DaysInMonth(year, MonthS);
            string chargeTable = localPref + "_charge_" + (year - 2000).ToString("00");
            string kernelTable = pref + "_kernel";
            sql = @"INSERT INTO t_svod_pack
                    SELECT s.service, ttk.num_ls, p1.val as rsum_tarif_res, p1.odn as rsum_tarif, 0 as reval_res, c.reval + coalesce(p.sum_rcl, 0) as reval, 
                    CASE WHEN c.tarif = 0 THEN 0 ELSE c.sum_nedop/c.tarif END as sum_nedop_res, 
                    c.sum_nedop as sum_nedop, 0 as one_time_res, 0 as one_time, 0 as sum_charge, ttk.address, ttk.kvar, c.nzp_kvar , 
                    c.nzp_serv, ttk.ikvar
                    FROM " + chargeTable + @".charge_" + month + @" c
                    INNER JOIN t_temp_kvar ttk on ttk.nzp_kvar = c.nzp_kvar
                    INNER JOIN " + kernelTable + @".services s on s.nzp_serv = c.nzp_serv
                    INNER JOIN " + kernelTable + @".supplier sup on sup.nzp_supp = c.nzp_supp
                    LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_peni) as sum_peni FROM " + chargeTable +
                  @".peni_debt_2015_25 
                    where date_from >= '2015-" + month + @"-01' AND date_from <= '2015-" + month + "-" + days + @"'  
                    group by 1,2) peni on peni.nzp_kvar = c.nzp_kvar and peni.nzp_serv = c.nzp_serv
                    LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_rcl) as sum_rcl from " + chargeTable + @".perekidka 
	                    where month_ = " + month +
                  @" group by 1,2) p on p.nzp_kvar = c.nzp_kvar and p.nzp_serv = c.nzp_serv
                    LEFT JOIN (SELECT sum(val) as val, sum(odn) as odn, nzp_serv, nzp_kvar FROM (SELECT max(c.valm) as val, max(c.tarif) * max(c.valm) as odn, 
                    c.nzp_serv, c.nzp_kvar FROM " + chargeTable + @".calc_gku_" + month +
                  @" c where c.dat_charge is null and stek = 3 
	                group by 3, 4
	                UNION ALL 
	                SELECT max(c.dop87) as val, max(c.tarif) * max(c.dop87) as odn, CASE WHEN c.nzp_serv = 6 THEN 510 WHEN c.nzp_serv = 9 THEN 513 WHEN c.nzp_serv = 14 
                    THEN 514 WHEN c.nzp_serv = 25 THEN 515 WHEN c.nzp_serv = 210 THEN 516 ELSE c.nzp_serv END as nzp_serv, 
	                c.nzp_kvar FROM " + chargeTable + @".calc_gku_" + month +
                  @" c where c.dat_charge is null and stek = 3 and c.nzp_serv in (6,9,14,25,210)
	                group by 3, 4) t group by 3,4)
	                 p1 on p1.nzp_kvar = c.nzp_kvar and p1.nzp_serv = c.nzp_serv
                    where c.nzp_kvar in (SELECT nzp_kvar from t_temp_kvar) and c.nzp_serv != 1  and c.dat_charge is null and c.nzp_serv in (" + GetWhereServType() + ")";
            ExecSQL(sql);
            
            var ds = new DataSet();
            var dt2 = ExecSQLToTable(@"SELECT address, ikvar, kvar, num_ls, service as serv, sum(rsum_tarif_res) as rsum_tarif_res, sum(rsum_tarif) as rsum_tarif, sum(reval_res) as reval_res, 
                        sum(reval) as reval, sum(sum_nedop_res) as sum_nedop_res, sum(sum_nedop) as sum_nedop, sum(one_time_res) as one_time_res, sum(one_time) as one_time, 
                        sum(rsum_tarif) + sum(reval) - sum(sum_nedop) as sum_charge FROM t_svod_pack group by 1,2,3,4,5 order by 1,2,3,4,5");
            dt2.TableName = "Q_master";
            ds.Tables.Add(dt2);
            return ds;
        }


        private string GetWhereServType()
        {
            string whereServ = String.Empty;
            if (ServType != null)
            {
                foreach (Int64 nzp_serv in ServType)
                {
                    if (nzp_serv == 6)
                        whereServ += whereServ.Length == 0 ? nzp_serv.ToString() + ", 510" : "," + nzp_serv + ", 510";
                    else if (nzp_serv == 9)
                        whereServ += whereServ.Length == 0 ? nzp_serv.ToString() + ", 513" : "," + nzp_serv + ", 513";
                    else if (nzp_serv == 14)
                        whereServ += whereServ.Length == 0 ? nzp_serv.ToString() + ", 514" : "," + nzp_serv + ", 514";
                    else if (nzp_serv == 25)
                        whereServ += whereServ.Length == 0 ? nzp_serv.ToString() + ", 515" : "," + nzp_serv + ", 515";
                    else if (nzp_serv == 210)
                        whereServ += whereServ.Length == 0 ? nzp_serv.ToString() + ", 516" : "," + nzp_serv + ", 516";
                }
            }
            else
                whereServ = "6,9,14,25,210, 510,513,514,515,516";
            return whereServ;
        }

        /// <summary>
        /// Метод для обработки значений пользовательских параметров.
        /// Вызывается перед методом GetData()
        /// </summary>
        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();

            ServType = UserParamValues["Serv"].GetValue<List<long>>();

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

            //report.SetParameterValue("datS", DatS.ToShortDateString());

            //report.SetParameterValue("datPo", DatPo.ToShortDateString());
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            ExecSQL("create temp table t_svod_pack (service CHARACTER(100)," +
                "num_ls CHARACTER(100), " +
                "rsum_tarif_res NUMERIC(14,6) default 0," +
                "rsum_tarif NUMERIC(14,2) default 0," +
                "reval_res NUMERIC(14,6) default 0," +
                "reval NUMERIC(14,2) default 0," +
                "sum_nedop_res NUMERIC(14,6) default 0," +
                "sum_nedop NUMERIC(14,2) default 0," +
                "one_time_res NUMERIC(14,6) default 0, " +
                "one_time NUMERIC(14,2) default 0, " +
                "sum_charge NUMERIC(14,2) default 0, " +
                "address CHARACTER(100), kvar CHARACTER(100)," +
                "nzp_kvar INTEGER, nzp_serv INTEGER, ikvar INTEGER)");

            ExecSQL("create temp table t_temp_kvar (nzp_kvar INTEGER, address CHARACTER(100), kvar CHARACTER(100),num_ls CHARACTER(12),fio CHARACTER(100), ikvar INTEGER)");
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
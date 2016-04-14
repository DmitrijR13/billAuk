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
    class Report2 : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Выгрузка в 1С"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по выгрузке в 1С"; }
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
            get { return Resources.Report2; }
        }

        #region Значения параметров отчета

        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

        /// <summary>Значение параметра "Год"</summary>
        protected int Year { get; set; }

        /// <summary>Значение параметра "Месяц"</summary>
        protected int Month { get; set; }


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
                new YearParameter { Code = "Year", Name = "Год", Value = DateTime.Today.Year },
                new MonthParameter { Code = "Month", Name = "Месяц", Value = DateTime.Today.Month },
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
            string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" + Month.ToString("00");
            string sql = @"insert into t_svod
            SELECT s.service, u.ulica, d.ndom, k.nkvar, k.num_ls, k.fio, c.sum_charge, CASE WHEN s.nzp_serv = 100016 OR s.nzp_serv = 100015 THEN '90.01.1' ELSE '76.06' END as schet 
FROM " + pref + DBManager.sDataAliasRest + "kvar k " +
"INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
"INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
"INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
"INNER JOIN " + chargeTable + " c on c.nzp_kvar = k.nzp_kvar " +
"INNER JOIN fbill" + DBManager.sKernelAliasRest + "services s on s.nzp_serv = c.nzp_serv " +
"where s.nzp_serv != 1 " + Raions + Streets + Houses;
            ExecSQL(sql.ToString());
            string sql2 = "SELECT * FROM t_svod order by 2,3,4,6,1";

            var dt = ExecSQLToTable(sql2.ToString());
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>
        /// Метод для обработки значений пользовательских параметров.
        /// Вызывается перед методом GetData()
        /// </summary>
        protected override void PrepareParams()
        {
            Year = UserParamValues["Year"].GetValue<int>();
            Month = UserParamValues["Month"].GetValue<int>();

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

            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            ExecSQL("create temp table t_svod (serv CHARACTER(50), ulica CHARACTER(100), dom CHARACTER(10), kvar CHARACTER(10), num_ls CHARACTER(10), fio CHARACTER(100), " +
                "sum_charge NUMERIC(14,2), " +
                "schet CHARACTER(10))");
        }

        /// <summary>
        /// Удаление временных таблиц.
        /// Вызывается после метода GetData()
        /// </summary>
        protected override void DropTempTable()
        {
            ExecSQL("drop table t_svod");
        }
    }
}
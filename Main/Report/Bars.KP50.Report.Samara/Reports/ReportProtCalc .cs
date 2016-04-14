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
using System.IO;

namespace Bars.KP50.Report.Samara.Reports
{
    /// <summary>Пример написания отчета</summary>
    class ReportProtCalc : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Протокол расчета общедомового поправочного коэффициента"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Протокол расчета общедомового поправочного коэффициента"; }
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
            get { return Resources.ReportProtCalc; }
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
            string sql;
            string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" + (Month).ToString("00");
            sql = @"INSERT INTO t_svodProtCalc 
SELECT k.num_ls, k.nkvar, k.fio, coalesce(prm5.val_prm::integer, 0) as prm_5, coalesce(prm107.val_prm::integer, 0) as prm_107, 
CASE when coalesce(prm5.val_prm::integer, 0) != 0 AND c.is_device = 0 THEN c.c_calc/(prm5.val_prm::integer) ELSE 0 END as norm,
CASE when c.is_device != 0 THEN c_calc ELSE 0 END as withIPU, CASE when c.is_device = 0 THEN c_calc ELSE 0 END as withoutIPU
FROM bill01_data.kvar k " + 
" INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
" INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
" INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
" LEFT JOIN (SELECT val_prm, nzp from bill01_data.prm_1 where nzp_prm = 5 and is_actual != 100 AND dat_s < to_date('01." + (Month).ToString("00") + 
"." + Year + "','dd.mm.yyyy') and dat_po > to_date('01." + (Month).ToString("00") + "." + Year + 
@"','dd.mm.yyyy')) prm5 on prm5.nzp = k.nzp_kvar
LEFT JOIN (SELECT val_prm, nzp from bill01_data.prm_1 where nzp_prm = 107 and is_actual != 100 AND dat_s < to_date('01."+(Month).ToString("00")+
"."+Year+"','dd.mm.yyyy') and dat_po > to_date('01."+(Month).ToString("00")+"."+Year+@"','dd.mm.yyyy')) prm107 on prm107.nzp = k.nzp_kvar
LEFT JOIN (SELECT c_calc, is_device, nzp_kvar from "+chargeTable+" where nzp_serv = 25 and dat_charge is null) c on c.nzp_kvar = k.nzp_kvar " + 
" where 1=1 " + Raions + Streets + Houses + " order by k.nzp_dom, k.nkvar";
            ExecSQL(sql.ToString());
            
            var ds = new DataSet();

            sql = "SELECT * FROM t_svodProtCalc";
            var dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            ds.Tables.Add(dt);

            sql = "SELECT u.ulica || ', ' || d.ndom as address FROM bill01_data.dom d " +
                " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                " where 1=1 " + Raions + Streets + Houses;
            var dt2 = ExecSQLToTable(sql.ToString());
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

            report.SetParameterValue("date", DateTime.Today.ToShortDateString());

            report.SetParameterValue("period", months[Month] + " " + Year + " г.");
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            ExecSQL("create temp table t_svodProtCalc (num_ls INTEGER, " +
                "nkvar CHARACTER(10)," +
                "fio CHARACTER(50)," +
                "prm_5 INTEGER," +
                "prm_107 INTEGER," +
                "norm NUMERIC(14,2)," +
                "withIPU NUMERIC(14,2)," +
                "withoutIPU NUMERIC(14,2))");
        }

        /// <summary>
        /// Удаление временных таблиц.
        /// Вызывается после метода GetData()
        /// </summary>
        protected override void DropTempTable()
        {
            ExecSQL("drop table t_svodProtCalc");
        }
    }
}
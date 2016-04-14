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
    class ReportPack2 : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Отчет о собранных средствах"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета о собранных средствах"; }
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
            get { return Resources.ReportPack2; }
        }

        #region Значения параметров отчета

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Параметры</summary>
        private List<long> Bank { get; set; }

        /// <summary>Параметры</summary>
        private List<long> PackType { get; set; }

        #endregion

        /// <summary>
        /// Пользовательские параметры.
        /// Отображаются на форме печати.
        /// </summary>
        /// <returns>Список пользовательских параметров</returns>
        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            DateTime datS = curCalcMonthYear != null
                ? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
                    Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
                : DateTime.Now;
            DateTime datPo = curCalcMonthYear != null
                ? datS.AddMonths(1).AddDays(-1)
                : DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new ComboBoxParameter(true)             
                {
                    Code = "Bank",
                    Name = "Платежная система",
                    Value = "ООО «ЕИРЦ Г.О.ТОЛЬЯТТИ»",
                    StoreData = new List<object>
                    {
                        new { Id = "80018", Name = "ООО «ЕИРЦ Г.О.ТОЛЬЯТТИ»"},
                        new { Id = "80019", Name = "*Почта РОССИИ" },
                        new { Id = "80021", Name = "*Сбербанк" },
                        new { Id = "80022", Name = "*ЗАО Национальные кредитные карточки (NC" },
                        new { Id = "80023", Name = "*Касса ТСЖ" },
                        new { Id = "80024", Name = "*Пенсионный фонд" },
                        new { Id = "80026", Name = "АВБ" },
                        new { Id = "80027", Name = "*РПС Дымок" }
                    }
                },
                 new ComboBoxParameter(true)             
                {
                    Code = "PackType",
                    Name = "Тип пачки",
                    Value = "ООО «ЕИРЦ Г.О.ТОЛЬЯТТИ»",
                    StoreData = new List<object>
                    {
                        new { Id = "10", Name = "РЦ"},
                        new { Id = "20", Name = "УК" }
                    }
                }
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
            string finTable = pref + "_fin_" + (DatS.Year - 2000).ToString("00");
            string dataTable = pref + "_data";
            string sql = " SELECT pl.nzp_pack_ls as nzp_pack_ls, to_char(p.dat_pack, 'dd-mm-yyyy') as dat_vvod, b.bank as bank, sum(pl.g_sum_ls) as g_sum_ls, 0.00 as sum_ls, pl.num_ls as num_ls, " +
                        " ul.ulicareg || '. ' || ul.ulica || ', д. ' || d.ndom || ', кв. ' || k.nkvar as address, to_char(pl.date_distr::date, 'dd-mm-yyyy') as date_distr, pl.dat_month as dat_month" +
                        " FROM " + finTable + ".pack_ls pl " +
                        " INNER JOIN " + finTable + ".pack p on p.nzp_pack = pl.nzp_pack " +
                        " INNER JOIN fbill_kernel.s_bank b on b.nzp_bank = p.nzp_bank " +
                        " INNER JOIN " + dataTable + ".kvar k on k.num_ls = pl.num_ls " +
                        " INNER JOIN " + dataTable + ".dom d on d.nzp_dom = k.nzp_dom  " +
                        " INNER JOIN " + dataTable + ".s_ulica ul on ul.nzp_ul = d.nzp_ul " +
                        " where p.dat_pack >= '" + DatS + "' AND p.dat_pack <= '" + DatPo + "' " +
                        " and p.nzp_bank in (" + GetWhereBank() + ")" +
                        " and p.pack_type in (" + GetWherePackType() + ")" +
                        " group by 1,2,3,6,7,8,9 " +
                        " order by 2,3,6";
            var dt = ExecSQLToTable(sql.ToString());

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //dt.Rows[i]["num_ls"] = "40005" + dt.Rows[i]["num_ls"].ToString().PadLeft(6, '0');
                String month = Convert.ToDateTime(dt.Rows[i]["dat_month"]).Month.ToString().PadLeft(2, '0');
                Int32 year = Convert.ToDateTime(dt.Rows[i]["dat_month"]).Year;
                Int32 nzp_pack_ls = Convert.ToInt32(dt.Rows[i]["nzp_pack_ls"]);
                string chargeTable = localPref + "_charge_" + (year - 2000).ToString("00") + ".fn_supplier" + month;
                sql = "SELECT Case WHEN sum(sum_prih) != null OR sum(sum_prih)>0 THEN sum(sum_prih) ELSE 0.00 END FROM " + chargeTable + " where nzp_pack_ls = " + nzp_pack_ls;
                var sum_ls = ExecSQLToTable(sql.ToString());
                dt.Rows[i]["sum_ls"] = Convert.ToDecimal(sum_ls.Rows[0][0]);
            }
            var ds = new DataSet();
            dt.TableName = "Q_master";
            ds.Tables.Add(dt);
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

        private string GetWhereBank()
        {
            string whereBank = String.Empty;
            if (Bank != null)
            {
                foreach (Int64 nzp_bank in Bank)
                {
                    if (nzp_bank != 80027)
                        whereBank += whereBank.Length == 0 ? nzp_bank.ToString() : "," + nzp_bank;
                    else
                        whereBank += whereBank.Length == 0 ? nzp_bank.ToString() + ", 80028, 80020" : "," + nzp_bank + ", 80028, 80020";
                }
            }         
            else
                whereBank = "80018, 80019, 80020, 80021, 80022, 80023, 80024, 80026, 80027, 80028";
            return whereBank;
        }

        private string GetWherePackType()
        {
            string wherePackType = String.Empty;
            if (PackType != null)
            {
                foreach (Int64 pack_type in PackType)
                {
                    wherePackType += wherePackType.Length == 0 ? pack_type.ToString() : "," + pack_type;
                }
            }           
            else
                wherePackType = "10, 20";
            return wherePackType;
        }


        /// <summary>
        /// Метод для обработки значений пользовательских параметров.
        /// Вызывается перед методом GetData()
        /// </summary>
        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;

            Bank = UserParamValues["Bank"].GetValue<List<long>>();

            PackType = UserParamValues["PackType"].GetValue<List<long>>();
        }

        /// <summary>Подготовить отчет, например, добавить параметры вызова отчета, произвести другие действия перед сохранением</summary>
        /// <param name="report">Отчет</param>
        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("date", DateTime.Today.ToShortDateString());

            //report.SetParameterValue("supplier", Supp);
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            ExecSQL("create temp table t_svod_pack (nzp_pack_ls INTEGER," +
                "dat_vvod DATE," +
                "bank CHARACTER(40)," +
                "g_sum_ls NUMERIC(14,2) default 0," +
                "sum_ls NUMERIC(14,2) default 0," +
                "num_ls INTEGER," +
                "address CHARACTER(100)," +
                "date_distr DATE," +
                "dat_month DATE)");
        }

        /// <summary>
        /// Удаление временных таблиц.
        /// Вызывается после метода GetData()
        /// </summary>
        protected override void DropTempTable()
        {
            ExecSQL("drop table t_svod_pack");
        }
    }
}
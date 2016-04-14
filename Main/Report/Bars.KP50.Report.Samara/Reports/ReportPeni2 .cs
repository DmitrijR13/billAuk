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
    class ReportPeni2 : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Отчет по пени"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по пени"; }
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
            get { return Resources.ReportPeni; }
        }

        #region Значения параметров отчета

        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

        ///// <summary>Значение параметра "Год"</summary>
        //protected int YearFrom { get; set; }

        ///// <summary>Значение параметра "Месяц"</summary>
        //protected int MonthFrom { get; set; }

        ///// <summary>Значение параметра "Год"</summary>
        //protected int YearTo { get; set; }

        ///// <summary>Значение параметра "Месяц"</summary>
        //protected int MonthTo { get; set; }

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
                //new YearParameter { Code = "YearFrom", Name = "Год от", Value = DateTime.Today.Year },
                //new MonthParameter { Code = "MonthFrom", Name = "Месяц от", Value = DateTime.Today.Month },
                //new YearParameter { Code = "YearTo", Name = "Год до", Value = DateTime.Today.Year },
                //new MonthParameter { Code = "MonthTo", Name = "Месяц до", Value = DateTime.Today.Month },
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
            if (Nkvar_n == null || Nkvar_n == " " || Nkvar_n == String.Empty)
                Nkvar_n = "-";
            var pref = "bill01";
            string sql = "";
            if (Pkod == "")
            {
                sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar
                FROM " + pref + DBManager.sDataAliasRest + "kvar k " +
                    "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                    "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                    "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                    "where 1=1 " + Raions + Streets + Houses + " AND upper(k.nkvar) = upper('" + Nkvar + "') AND upper(k.nkvar_n) = upper('" + Nkvar_n + "')";
            }
            else
            {
                sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar
                FROM " + pref + DBManager.sDataAliasRest + "kvar k " +
                    "where pkod = " + Pkod;
            }
            ExecSQL(sql.ToString());
            string chargeTable = pref + "_charge_15.peni_debt_2015_25";
                sql = @"insert into t_svod
                       select date_from, date_to, sum(sum_debt_result), max(cnt_days), sum(sum_peni), 0, sum(sum_debt_result) + sum(sum_peni)
from bill01_charge_15.peni_debt_2015_25 
where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)
group by 1,2
order by 1,2";
                ExecSQL(sql.ToString());
            
            var ds = new DataSet();

            string sql4 = "SELECT date_from, date_to - 1 as date_to FROM t_svod order by 1 DESC, 2 DESC limit 1";
            var dt4 = ExecSQLToTable(sql4.ToString());
            dt4.TableName = "Q_master1";
            ds.Tables.Add(dt4);

            sql = "DELETE FROM t_svod where peni = 0 AND date_from in (SELECT date_from FROM (SELECT date_from, count(*) FROM t_svod group by 1 having count(*) = 1 order by 1) t1)";
            ExecSQL(sql.ToString());

            sql = "DELETE FROM t_svod where date_from in (SELECT date_from FROM (SELECT date_from, peni, count(*) FROM t_svod group by 1,2 having count(*) > 1 order by 1) t1)";
            ExecSQL(sql.ToString());

            sql = @"UPDATE t_svod SET date_to =  date_to + INTERVAL '- 1 day'";
            ExecSQL(sql.ToString());


            string sql2 = "SELECT * FROM t_svod";
            var dt = ExecSQLToTable(sql2.ToString());

            sql2 = "SELECT date_calc, sum(sum_peni), sum(sum_old_reval), sum(sum_peni) + sum(sum_old_reval) FROM bill01_charge_15.peni_calc_2015_25 where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1 order by 1";
            var dt2 = ExecSQLToTable(sql2.ToString());

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i == 0)
                    dt.Rows[i][5] = Convert.ToDecimal(dt.Rows[i][4]);
                else
                    dt.Rows[i][5] = Convert.ToDecimal(dt.Rows[i][4]) + Convert.ToDecimal(dt.Rows[i - 1][5]);
            }


            DateTime date_from = new DateTime();
            DateTime date_to = new DateTime();
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("date_from");
            dataTable.Columns.Add("date_to");
            dataTable.Columns.Add("dolg");
            dataTable.Columns.Add("days");
            dataTable.Columns.Add("peni");
            dataTable.Columns.Add("dolg_peni");
            dataTable.Columns.Add("sum_total");
            DataRow row2;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row2 = dataTable.NewRow();
                row2["date_from"] = Convert.ToDateTime(dt.Rows[i][0]).ToShortDateString();
                row2["date_to"] = Convert.ToDateTime(dt.Rows[i][1]).ToShortDateString();
                row2["dolg"] = Convert.ToDecimal(dt.Rows[i][2]);
                row2["days"] = Convert.ToInt32(dt.Rows[i][3]);
                row2["peni"] = Convert.ToDecimal(dt.Rows[i][4]);
                row2["dolg_peni"] = Convert.ToDecimal(dt.Rows[i][5]);
                row2["sum_total"] = Convert.ToDecimal(dt.Rows[i][6]);
                dataTable.Rows.Add(row2);
            }
            StreamWriter sw = new StreamWriter(@"C:\temp\error.log", false);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                if (Convert.ToDateTime(dataTable.Rows[i][0]) == date_from)
                {
                    try
                    {
                        sw.WriteLine(dataTable.Rows[i - 1][0].ToString());
                        sw.WriteLine(Convert.ToDateTime(dataTable.Rows[i - 1][1]).AddDays(1).ToShortDateString());
                        sw.WriteLine(dataTable.Rows[i - 1][1].ToString());
                        sw.WriteLine(Convert.ToDateTime(dataTable.Rows[i][1]).ToShortDateString());
                        dataTable.Rows[i - 1][0] += "\n" + Convert.ToDateTime(dataTable.Rows[i - 1][1]).AddDays(1).ToShortDateString();
                        dataTable.Rows[i - 1][1] += "\n" + Convert.ToDateTime(dataTable.Rows[i][1]).ToShortDateString();
                        if (Convert.ToDecimal(dataTable.Rows[i - 1][2]) <= Convert.ToDecimal(dataTable.Rows[i][2]))
                            dataTable.Rows[i - 1][2] = dataTable.Rows[i][2];
                        if (Convert.ToDecimal(dataTable.Rows[i - 1][3]) <= Convert.ToDecimal(dataTable.Rows[i][3]))
                            dataTable.Rows[i - 1][3] = dataTable.Rows[i][3];
                        if (Convert.ToDecimal(dataTable.Rows[i - 1][4]) <= Convert.ToDecimal(dataTable.Rows[i][4]))
                            dataTable.Rows[i - 1][4] = dataTable.Rows[i][4];
                        if (Convert.ToDecimal(dataTable.Rows[i - 1][5]) <= Convert.ToDecimal(dataTable.Rows[i][5]))
                            dataTable.Rows[i - 1][5] = dataTable.Rows[i][5];
                        dataTable.Rows.RemoveAt(i);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    date_from = Convert.ToDateTime(dataTable.Rows[i][0]);
                    dataTable.Rows[i][0] = "" + Convert.ToDateTime(dataTable.Rows[i][0]).ToShortDateString();
                    sw.WriteLine(dataTable.Rows[i][0].ToString());
                    dataTable.Rows[i][1] = "" + Convert.ToDateTime(dataTable.Rows[i][1]).ToShortDateString();
                    sw.WriteLine(dataTable.Rows[i][1].ToString());
                }
            }
            sw.Close();
            decimal dolg_peni = Convert.ToDecimal(dataTable.Rows[dataTable.Rows.Count - 1][5]);
            dataTable.TableName = "Q_master";
            ds.Tables.Add(dataTable);

            decimal totalPeni = 0;
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                totalPeni += Convert.ToDecimal(dataTable.Rows[i]["peni"]);
            }

            sql2 = "SELECT " + totalPeni + " as total_peni from t_svod";

            var dt6 = ExecSQLToTable(sql2.ToString());
            dt6.TableName = "Q_master6";
            ds.Tables.Add(dt6);

            sql2 = "SELECT dolg, sum_total from t_svod where date_from = (SELECT max(date_from) from t_svod)";

            var dt3 = ExecSQLToTable(sql2.ToString());
            dt3.TableName = "Q_master3";
            ds.Tables.Add(dt3);

            sql2 = "SELECT "+dolg_peni+" as dolg_peni from t_svod";

            var dt5 = ExecSQLToTable(sql2.ToString());
            dt5.TableName = "Q_master4";
            ds.Tables.Add(dt5);

               string sql3 = @"SELECT fio, num_ls, address
FROM(SELECT 1 as id, fio, num_ls FROM bill01_data.kvar where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t1
LEFT JOIN(
SELECT u.ulica || ', дом № ' || d.ndom || ', кв. ' || k.nkvar || ' ком. ' || coalesce(k.nkvar_n, '-') as address, 1 as id
FROM bill01_data.kvar k 
INNER JOIN bill01_data.dom d on d.nzp_dom = k.nzp_dom 
INNER JOIN bill01_data.s_ulica u on u.nzp_ul = d.nzp_ul 
INNER JOIN bill01_data.s_rajon r on r.nzp_raj = u.nzp_raj where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t2 on t2.id = t1.id";
          
            var dt7 = ExecSQLToTable(sql3.ToString());
            dt7.TableName = "Q_master2";
            ds.Tables.Add(dt7);
            return ds;
        }

        /// <summary>
        /// Метод для обработки значений пользовательских параметров.
        /// Вызывается перед методом GetData()
        /// </summary>
        protected override void PrepareParams()
        {
            //YearFrom = UserParamValues["YearFrom"].GetValue<int>();
            //MonthFrom = UserParamValues["MonthFrom"].GetValue<int>();
            //YearTo = UserParamValues["YearTo"].GetValue<int>();
            //MonthTo = UserParamValues["MonthTo"].GetValue<int>();
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
            ExecSQL("create temp table t_svod (date_from DATE, " +
                "date_to DATE," +
                "dolg NUMERIC(14,2) default 0," +
                "days INTEGER default 0," +
                "peni NUMERIC(14,2) default 0," +
                "dolg_peni NUMERIC(14,2) default 0," +
                "sum_total NUMERIC(14,2) default 0)");

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
        }
    }
}
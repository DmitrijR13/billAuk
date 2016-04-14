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
    class ReportGenerator : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Отчет по должникам"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по должникам"; }
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
            get { return Resources.ReportGenerator; }
        }

        #region Значения параметров отчета

        /// <summary>Значение параметра "Год"</summary>
        protected int? SrokDolgS { get; set; }

        /// <summary>Значение параметра "Месяц"</summary>
        protected int? SrokDolgPo { get; set; }

        /// <summary>Значение параметра "Год"</summary>
        protected decimal? DolgS { get; set; }

        /// <summary>Значение параметра "Месяц"</summary>
        protected decimal? DolgPo { get; set; }

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

        /// <summary>Значение параметра "Показать все"</summary>
        protected int printAll { get; set; }

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
                new AddressParameter(),
                new StringParameter{Code ="SrokDolgS", Name = "Долг с"},
                new StringParameter{Code ="SrokDolgPo", Name = "Долг по,"},
                new StringParameter{Code ="DolgS", Name = "Сумма долга с"},
                new StringParameter{Code ="DolgPo", Name = "Сумма долга по,"},
                new ComboBoxParameter
                {
                    Code = "printAll",
                    Name = "Выводить все",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Нет" },
                        new { Id = "2", Name = "Да" }
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
            string chargeTable = localPref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" + (Month).ToString("00");
            string perekidkaTable = localPref + "_charge_" + (Year - 2000).ToString("00") + ".perekidka";
            string sql = " SELECT u.ulica, d.ndom, k.nkvar, k.num_ls, k.fio, '' as days_count, sum(sum_insaldo) as sum_insaldo, sum(rsum_tarif) + sum(reval) - sum(sum_nedop) as sum_charge, " +
                         " 1.01 as sum_money, 1.01 as sum_outsaldo, coalesce(p.sum_rcl, 0) as sum_rcl, " +
                         " CASE WHEN p1.val_prm = '0' THEN 'неприватизированная' WHEN p1.val_prm = '1' THEN 'приватизированная' " +
                         " WHEN p1.val_prm = '6' THEN 'маневренный фонд' ELSE '' END as type, " +
                         " CASE WHEN p2.val_prm = '1' THEN 'изолированная' WHEN p2.val_prm = '2' THEN 'коммунальная' ELSE 'не указано' END as comf, k.nzp_kvar" +
                        " FROM " + pref + "_data.kvar k " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                        " INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                        " INNER JOIN " + chargeTable +" c on c.nzp_kvar = k.nzp_kvar " +
                        " LEFT JOIN (SELECT nzp_kvar, sum(sum_rcl) as sum_rcl from " + perekidkaTable + " where month_ = " + Month + " group by 1) p on p.nzp_kvar = k.nzp_kvar " +
                        " LEFT JOIN (SELECT nzp, max(val_prm) as val_prm from bill01_data.prm_1 where is_actual = 1 AND Extract(year from dat_po) = 3000 AND nzp_prm = 2009 group by 1) p1 on p1.nzp = k.nzp_kvar " +
                        " LEFT JOIN (SELECT nzp, max(val_prm) as val_prm from bill01_data.prm_1 where is_actual = 1 AND Extract(year from dat_po) = 3000 AND nzp_prm = 3 group by 1) p2 on p2.nzp = k.nzp_kvar " +
                        " where nzp_serv != 1 and dat_charge is null " + Raions + Streets + Houses + 
                        //" and k.pkod = 3040102008552 " +
                        " GROUP BY 1,2,3,4,5,11,12,13,14 " +
                        " ORDER BY 1,2,3,4,5";
            var dt = ExecSQLToTable(sql.ToString());
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                List<decimal> dolg = new List<decimal>();
                int days_dolg = 0;
                decimal money = 0;
                decimal sum_money = 0;
                int monthSSR = 0;
                if (Year == 2015)
                {
                    for (int y = 2015; y <= DateTime.Now.Year; y++)
                    {
                        if (y == 2015)
                        {
                            for (int month = Month; month <= 12; month++)
                            {
                                string fnTable = localPref + "_charge_" + (Year - 2000).ToString("00") + ".fn_supplier" + month.ToString("00");
                                sql = "SELECT sum(sum_prih) FROM " + fnTable + " where num_ls = " + dt.Rows[i][3].ToString();
                                var dtMoneyMonth = ExecSQLToTable(sql.ToString());
                                sum_money += (dtMoneyMonth.Rows[0][0] != null && dtMoneyMonth.Rows[0][0].ToString().Trim() != "") ? Convert.ToDecimal(dtMoneyMonth.Rows[0][0]) : 0;
                            }
                        }
                        else
                        {
                            for (int month = 1; month <= DateTime.Now.Month; month++)
                            {
                                string fnTable = localPref + "_charge_" + (Year - 2000).ToString("00") + ".fn_supplier" + month.ToString("00");
                                sql = "SELECT sum(sum_prih) FROM " + fnTable + " where num_ls = " + dt.Rows[i][3].ToString();
                                var dtMoneyMonth = ExecSQLToTable(sql.ToString());
                                sum_money += (dtMoneyMonth.Rows[0][0] != null && dtMoneyMonth.Rows[0][0].ToString().Trim() != "") ? Convert.ToDecimal(dtMoneyMonth.Rows[0][0]) : 0;
                            }
                        }
                    }                    
                }
                else
                {
                    for (int month = Month; month <= DateTime.Now.Month; month++)
                    {
                        string fnTable = localPref + "_charge_" + (Year - 2000).ToString("00") + ".fn_supplier" + month.ToString("00");
                        sql = "SELECT sum(sum_prih) FROM " + fnTable + " where num_ls = " + dt.Rows[i][3].ToString();
                        var dtMoneyMonth = ExecSQLToTable(sql);
                        sum_money += (dtMoneyMonth.Rows[0][0] != null && dtMoneyMonth.Rows[0][0].ToString().Trim() != "") ? Convert.ToDecimal(dtMoneyMonth.Rows[0][0]) : 0;
                    }
                }
                
                dt.Rows[i]["sum_money"] = sum_money;

                if (Month == DateTime.Now.Month && Year == DateTime.Now.Year)
                    dt.Rows[i]["sum_outsaldo"] = Convert.ToDecimal(dt.Rows[i]["sum_insaldo"]) - Convert.ToDecimal(dt.Rows[i]["sum_money"]);
                else
                    dt.Rows[i]["sum_outsaldo"] = Convert.ToDecimal(dt.Rows[i]["sum_insaldo"]) + Convert.ToDecimal(dt.Rows[i]["sum_charge"]) - Convert.ToDecimal(dt.Rows[i]["sum_money"]);

                decimal sumCharge = 0;
                if (Year != 2015)
                {
                    for (int y = 2015; y <= DateTime.Now.Year; y++)
                    {
                        if (y == 2015)
                        {
                            for (int month = 1; month <= 12; month++)
                            {
                                string chargeTableMonth = localPref + "_charge_" + (y - 2000).ToString("00") +
                                                          ".charge_" +
                                                          month.ToString("00");
                                sql = "SELECT sum(rsum_tarif) as sum_charge " +
                                      " FROM " + chargeTableMonth +
                                      " where nzp_serv != 1 AND nzp_kvar = " + dt.Rows[i][13].ToString() +
                                      " and dat_charge is null";
                                var dtChargeMonth = ExecSQLToTable(sql.ToString());
                                if (dtChargeMonth.Rows[0][0] != null && dtChargeMonth.Rows[0][0].ToString().Trim() != "" &&
                                    Convert.ToDecimal(dtChargeMonth.Rows[0][0].ToString().Trim()) != 0)
                                    monthSSR++;
                                sumCharge += (dtChargeMonth.Rows[0][0] != null &&
                                              dtChargeMonth.Rows[0][0].ToString().Trim() != "")
                                    ? Convert.ToDecimal(dtChargeMonth.Rows[0][0])
                                    : 0;
                            }
                        }
                        else
                        {
                            for (int month = 1; month < Month; month++)
                            {
                                if (month == DateTime.Now.Month)
                                    break;
                                string chargeTableMonth = localPref + "_charge_" + (y - 2000).ToString("00") + ".charge_" +
                                                          month.ToString("00");
                                sql = "SELECT sum(rsum_tarif) as sum_charge " +
                                      " FROM " + chargeTableMonth +
                                      " where nzp_serv != 1 AND nzp_kvar = " + dt.Rows[i][13].ToString() +
                                      " and dat_charge is null";
                                var dtChargeMonth = ExecSQLToTable(sql.ToString());
                                if (dtChargeMonth.Rows[0][0] != null && dtChargeMonth.Rows[0][0].ToString().Trim() != "" &&
                                    Convert.ToDecimal(dtChargeMonth.Rows[0][0].ToString().Trim()) != 0)
                                    monthSSR++;
                                sumCharge += (dtChargeMonth.Rows[0][0] != null &&
                                              dtChargeMonth.Rows[0][0].ToString().Trim() != "")
                                    ? Convert.ToDecimal(dtChargeMonth.Rows[0][0])
                                    : 0;
                            }
                        }
                        
                    }
                    
                }
                else
                {
                    for (int month = 1; month < Month; month++)
                    {
                        if (month == DateTime.Now.Month)
                            break;
                        string chargeTableMonth = localPref + "_charge_" + (Year - 2000).ToString("00") + ".charge_" +
                                                  month.ToString("00");
                        sql = "SELECT sum(rsum_tarif) as sum_charge " +
                              " FROM " + chargeTableMonth +
                              " where nzp_serv != 1 AND nzp_kvar = " + dt.Rows[i][13].ToString() +
                              " and dat_charge is null";
                        var dtChargeMonth = ExecSQLToTable(sql.ToString());
                        if (dtChargeMonth.Rows[0][0] != null && dtChargeMonth.Rows[0][0].ToString().Trim() != "" &&
                            Convert.ToDecimal(dtChargeMonth.Rows[0][0].ToString().Trim()) != 0)
                            monthSSR++;
                        sumCharge += (dtChargeMonth.Rows[0][0] != null &&
                                      dtChargeMonth.Rows[0][0].ToString().Trim() != "")
                            ? Convert.ToDecimal(dtChargeMonth.Rows[0][0])
                            : 0;
                    }    
                }
                      
                //monthSSR = (monthSSR == DateTime.Now.Month) ? monthSSR - 1 : monthSSR;
                decimal ssr = (monthSSR != 0) ? sumCharge / monthSSR : 0;
                decimal rez = (ssr != 0) ? Convert.ToDecimal(dt.Rows[i]["sum_outsaldo"]) / ssr : 0;
                if (rez <= 0)
                {
                    dt.Rows[i]["days_count"] = "0";
                }
                else if (rez < 1)
                {
                    dt.Rows[i]["days_count"] = "менее месяца";
                }
                else
                {
                    dt.Rows[i]["days_count"] = Math.Round(rez).ToString();
                }

            }
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i][7] = Convert.ToDecimal(dt.Rows[i][7]) + Convert.ToDecimal(dt.Rows[i][10]);
            }
            var ds = new DataSet();

            DataTable d1 = new DataTable();
            d1.Columns.Add("ulica");
            d1.Columns.Add("ndom");
            d1.Columns.Add("nkvar");
            d1.Columns.Add("num_ls");
            d1.Columns.Add("fio");
            d1.Columns.Add("days_count");
            d1.Columns.Add("sum_insaldo", typeof(Double));
            d1.Columns.Add("sum_charge", typeof(Double));
            d1.Columns.Add("sum_money", typeof(Double));
            d1.Columns.Add("sum_outsaldo", typeof(Double));
            d1.Columns.Add("sum_rcl");
            d1.Columns.Add("type");
            d1.Columns.Add("comf");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Decimal sum_ousaldo = Convert.ToDecimal(dt.Rows[i]["sum_outsaldo"]);
                Decimal dolg = dt.Rows[i]["days_count"].ToString() != "менее месяца"
                    ? Convert.ToInt32(dt.Rows[i]["days_count"])
                    : 0.5m;
                if (sum_ousaldo >= DolgS && sum_ousaldo <= DolgPo && dolg >= SrokDolgS && dolg <= SrokDolgPo)
                {
                    DataRow row;
                    row = d1.NewRow();
                    row["ulica"] = dt.Rows[i]["ulica"];
                    row["ndom"] = dt.Rows[i]["ndom"];
                    row["nkvar"] = dt.Rows[i]["nkvar"];
                    row["num_ls"] = dt.Rows[i]["num_ls"];
                    row["fio"] = dt.Rows[i]["fio"];
                    row["days_count"] = dt.Rows[i]["days_count"];
                    row["sum_insaldo"] = Convert.ToDecimal(dt.Rows[i]["sum_insaldo"]);
                    row["sum_charge"] = Convert.ToDecimal(dt.Rows[i]["sum_charge"]);
                    row["sum_money"] = Convert.ToDecimal(dt.Rows[i]["sum_money"]);
                    row["sum_outsaldo"] = Convert.ToDecimal(dt.Rows[i]["sum_outsaldo"]);
                    row["sum_rcl"] = dt.Rows[i]["sum_rcl"];
                    row["type"] = dt.Rows[i]["type"];
                    row["comf"] = dt.Rows[i]["comf"];
                    d1.Rows.Add(row);
                }
            }

            if (printAll == 2)
            {
                d1.TableName = "Q_master";
                ds.Tables.Add(d1);
            }
            else
            {
                DataTable d = new DataTable();
                d.Columns.Add("ulica");
                d.Columns.Add("ndom");
                d.Columns.Add("nkvar");
                d.Columns.Add("num_ls");
                d.Columns.Add("fio");
                d.Columns.Add("days_count");
                d.Columns.Add("sum_insaldo", typeof(Double));
                d.Columns.Add("sum_charge", typeof(Double));
                d.Columns.Add("sum_money", typeof(Double));
                d.Columns.Add("sum_outsaldo", typeof(Double));
                d.Columns.Add("sum_rcl");
                d.Columns.Add("type");
                d.Columns.Add("comf");
                for (int i = 0; i < d1.Rows.Count; i++)
                {
                    if (d1.Rows[i]["days_count"].ToString() != "0")
                    {
                        DataRow row;
                        row = d.NewRow();
                        row["ulica"] = d1.Rows[i]["ulica"];
                        row["ndom"] = d1.Rows[i]["ndom"];
                        row["nkvar"] = d1.Rows[i]["nkvar"];
                        row["num_ls"] = d1.Rows[i]["num_ls"];
                        row["fio"] = d1.Rows[i]["fio"];
                        row["days_count"] = d1.Rows[i]["days_count"];
                        row["sum_insaldo"] = Convert.ToDecimal(d1.Rows[i]["sum_insaldo"]);
                        row["sum_charge"] = Convert.ToDecimal(d1.Rows[i]["sum_charge"]);
                        row["sum_money"] = Convert.ToDecimal(d1.Rows[i]["sum_money"]);
                        row["sum_outsaldo"] = Convert.ToDecimal(d1.Rows[i]["sum_outsaldo"]);
                        row["sum_rcl"] = d1.Rows[i]["sum_rcl"];
                        row["type"] = d1.Rows[i]["type"];
                        row["comf"] = d1.Rows[i]["comf"];
                        d.Rows.Add(row);
                    }
                }
                d.TableName = "Q_master";
                ds.Tables.Add(d);
            }
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
            printAll = UserParamValues["printAll"].GetValue<int>();

            SrokDolgS = UserParamValues["SrokDolgS"].GetValue<int?>() ?? 0;
            SrokDolgPo = UserParamValues["SrokDolgPo"].GetValue<int?>() ?? 999;
            DolgS = UserParamValues["DolgS"].GetValue<decimal?>() ?? -100000000;
            DolgPo = UserParamValues["DolgPo"].GetValue<decimal?>() ?? 100000000;


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
            ExecSQL("create temp table t_svod (month INTEGER default 0,date_ CHARACTER(30), sum_tarif_total NUMERIC(14,2), sum_tarif_500 NUMERIC(14,2)," +
                "sum_tarif_7 NUMERIC(14,2) default 0," +
                "tarif_7 NUMERIC(14,2) default 0," +
                "sum_tarif_9 NUMERIC(14,2) default 0," +
                "tarif_9 NUMERIC(14,2) default 0," +
                "sum_tarif_15 NUMERIC(14,2) default 0," +
                "tarif_15 NUMERIC(14,2) default 0," +
                "sum_tarif_8 NUMERIC(14,2) default 0," +
                "tarif_8 NUMERIC(14,2) default 0," +
                "sum_tarif_17 NUMERIC(14,2) default 0," +
                "tarif_17 NUMERIC(14,2) default 0," +
                "sum_tarif_2 NUMERIC(14,2) default 0," +
                "tarif_2 NUMERIC(14,2) default 0," +
                "sum_tarif_22 NUMERIC(14,2) default 0," +
                "tarif_22 NUMERIC(14,2) default 0," +
                "sum_tarif_14 NUMERIC(14,2) default 0," +
                "tarif_14 NUMERIC(14,2) default 0," +
                "sum_tarif_25 NUMERIC(14,2) default 0," +
                "tarif_25 NUMERIC(14,2) default 0," +
                "sum_tarif_100018 NUMERIC(14,2) default 0," +
                "tarif_100018 NUMERIC(14,2) default 0," +
                "sum_money NUMERIC(14,2) default 0," +
                "dat_prih CHARACTER(10), reval NUMERIC(14,2) default 0," +
                "dolg_sum NUMERIC(14,2) default 0, dolg_sum_peni NUMERIC(14,2) default 0, " +
                "sum_money_peni NUMERIC(14,2) default 0,  reval_peni NUMERIC(14,2) default 0)");

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
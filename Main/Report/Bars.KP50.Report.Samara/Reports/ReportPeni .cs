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
    class ReportPeni : BaseSqlReport
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
            string chargeTable = pref + "_charge_15.peni_debt_201601_25_up";
                sql = @"insert into t_svod
                       select date_from, pd.date_to, 
                        CASE when sum_prih is null THEN 0 ELSE sum_prih END as sum_prih, dat_prih, sum(sum_debt_result), max(cnt_days), sum(sum_peni), 0, sum(sum_debt_result) + sum(sum_peni)
                        from bill01_charge_15.peni_debt_201512_25_up pd
                        left join (
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier02 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier03 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier04 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier05 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier06 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier07 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier08 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier09 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier10 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier11 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        UNION ALL
                        SELECT sum(sum_prih) as sum_prih, dat_prih
                        FROM bill01_charge_15.fn_supplier12 fs
                        INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                        WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                        ) t1 on t1.dat_prih = pd.date_to + INTERVAL '- 1 day'
                        where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)
                        group by 1,2,3,4
                        order by 1,2";
                ExecSQL(sql.ToString());

            Int32 curMonth = DateTime.Now.Month;
            for (int i = curMonth; i >= 1; i--)
            {
                try
                {
                    sql = @"select date_from, pd.date_to, 
                            CASE when sum_prih is null THEN 0 ELSE sum_prih END as sum_prih, dat_prih, sum(sum_debt_result), max(cnt_days), sum(sum_peni), 0, sum(sum_debt_result) + sum(sum_peni)
                            from bill01_charge_16.peni_debt_2016" + i.ToString("00") + @"_25_up pd
                            left join (
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier01 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier02 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier03 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier04 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier05 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_15.fn_supplier06 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier07 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier08 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier09 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier10 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier11 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier12 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            ) t1 on t1.dat_prih = pd.date_to + INTERVAL '- 1 day'
                            where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)
                            group by 1,2,3,4
                            order by 1,2";
                    var dtCheck = ExecSQLToTable(sql.ToString());
                    if (dtCheck.Rows.Count != 0)
                    {
                        sql = @"insert into t_svod
                           select date_from, pd.date_to, 
                            CASE when sum_prih is null THEN 0 ELSE sum_prih END as sum_prih, dat_prih, sum(sum_debt_result), max(cnt_days), sum(sum_peni), 0, sum(sum_debt_result) + sum(sum_peni)
                            from bill01_charge_16.peni_debt_2016" + i.ToString("00") + @"_25_up pd
                            left join (
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier01 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier02 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier03 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier04 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier05 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_15.fn_supplier06 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier07 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier08 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier09 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier10 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier11 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            UNION ALL
                            SELECT sum(sum_prih) as sum_prih, dat_prih
                            FROM bill01_charge_16.fn_supplier12 fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 2
                            ) t1 on t1.dat_prih = pd.date_to + INTERVAL '- 1 day'
                            where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)
                            group by 1,2,3,4
                            order by 1,2";
                        ExecSQL(sql.ToString());
                        break;
                    }
                    
                }
                catch
                {
                    continue;
                }
            }
            
            

                
            
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

            sql = "DELETE FROM t_svod where peni = 0";
            ExecSQL(sql.ToString());

            string sql2 = "SELECT * FROM t_svod";
            var dt = ExecSQLToTable(sql2.ToString());

            sql2 = @"SELECT date_to, sum(sum_peni) + sum(sum_old_reval) as dolg_epd FROM (SELECT date_to, sum_peni, sum_old_reval FROM bill01_charge_15.peni_calc_2015_25 where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)
                    and date_to > to_date('2015-05-01', 'yyyy-mm-dd')
                    UNION ALL 
                    SELECT date_to, sum_peni, sum_old_reval FROM bill01_charge_16.peni_calc_2016_25 where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)
                    and date_to > to_date('2015-05-01', 'yyyy-mm-dd')
                    ) t
                    group by 1 order by 1";
            var dtCharge = ExecSQLToTable(sql2.ToString());

            DataTable tableRez = new DataTable();
            tableRez.Columns.Add("date_from");
            tableRez.Columns.Add("date_to");
            tableRez.Columns.Add("sum_prih");
            tableRez.Columns.Add("dat_prih");
            tableRez.Columns.Add("dolg");
            tableRez.Columns.Add("days");
            tableRez.Columns.Add("peni");
            tableRez.Columns.Add("dolg_peni");
            tableRez.Columns.Add("sum_total");
            tableRez.Columns.Add("dolg_epd");
            tableRez.Columns.Add("difference");
            tableRez.Columns.Add("stavka");

            if (dt.Rows.Count != 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i == 0)
                        dt.Rows[i][7] = Convert.ToDecimal(dt.Rows[i][6]);
                    else
                        dt.Rows[i][7] = Convert.ToDecimal(dt.Rows[i][6]) + Convert.ToDecimal(dt.Rows[i - 1][7]);
                }

                DateTime date_from = new DateTime();
                DateTime date_to = new DateTime();
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("date_from");
                dataTable.Columns.Add("date_to");
                dataTable.Columns.Add("sum_prih");
                dataTable.Columns.Add("dat_prih");
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
                    row2["sum_prih"] = Convert.ToDecimal(dt.Rows[i][2]);
                    row2["dat_prih"] = 0;
                    row2["dolg"] = Convert.ToDecimal(dt.Rows[i][4]);
                    row2["days"] = Convert.ToInt32(dt.Rows[i][5]);
                    row2["peni"] = Convert.ToDecimal(dt.Rows[i][6]);
                    row2["dolg_peni"] = Convert.ToDecimal(dt.Rows[i][7]);
                    row2["sum_total"] = Convert.ToDecimal(dt.Rows[i][8]);
                    dataTable.Rows.Add(row2);
                }
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    if (Convert.ToDateTime(dataTable.Rows[i][0]) == date_from)
                    {
                        try
                        {
                            dataTable.Rows[i - 1][0] += "\n" +
                                                        Convert.ToDateTime(dataTable.Rows[i - 1][1])
                                                            .AddDays(1)
                                                            .ToShortDateString();
                            dataTable.Rows[i - 1][1] += "\n" +
                                                        Convert.ToDateTime(dataTable.Rows[i][1]).ToShortDateString();
                            if (Convert.ToDecimal(dataTable.Rows[i - 1][2]) <= Convert.ToDecimal(dataTable.Rows[i][2]))
                                dataTable.Rows[i - 1][2] = dataTable.Rows[i][2];
                            dataTable.Rows[i - 1][3] = 0;
                            if (Convert.ToDecimal(dataTable.Rows[i - 1][4]) <= Convert.ToDecimal(dataTable.Rows[i][4]))
                                dataTable.Rows[i - 1][4] = dataTable.Rows[i][4];
                            if (Convert.ToDecimal(dataTable.Rows[i - 1][5]) <= Convert.ToDecimal(dataTable.Rows[i][5]))
                                dataTable.Rows[i - 1][5] = dataTable.Rows[i][5];
                            if (Convert.ToDecimal(dataTable.Rows[i - 1][6]) <= Convert.ToDecimal(dataTable.Rows[i][6]))
                                dataTable.Rows[i - 1][6] = dataTable.Rows[i][6];
                            if (Convert.ToDecimal(dataTable.Rows[i - 1][7]) <= Convert.ToDecimal(dataTable.Rows[i][7]))
                                dataTable.Rows[i - 1][7] = dataTable.Rows[i][7];
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
                        dataTable.Rows[i][1] = "" + Convert.ToDateTime(dataTable.Rows[i][1]).ToShortDateString();
                    }
                }
                StreamWriter swPeni = new StreamWriter(@"C:\temp\Peni.log", true);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    if (dataTable.Rows[i][0].ToString().Substring(3, 2) !=
                        dataTable.Rows[i][1].ToString().Substring(3, 2))
                    {
                        int monthPerFrom = Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(3, 2));
                        int monthPerTo = Convert.ToInt32(dataTable.Rows[i][1].ToString().Substring(3, 2));
                        if (monthPerTo - monthPerFrom == 1)
                        {
                            int days_count = Convert.ToInt32(dataTable.Rows[i][5]);
                            decimal peni = Convert.ToDecimal(dataTable.Rows[i][6]);

                            row2 = tableRez.NewRow();
                            row2["date_from"] = dataTable.Rows[i][0].ToString();
                            row2["date_to"] =
                                new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                    Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(3, 2)),
                                    DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                        Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(3, 2)))).ToShortDateString
                                    ();
                            row2["sum_prih"] = 0;
                            row2["dat_prih"] = Convert.ToDecimal(dataTable.Rows[i][3]);
                            row2["dolg"] = Convert.ToDecimal(dataTable.Rows[i][4]);
                            row2["days"] = days_count - 10;
                            row2["peni"] = Math.Round(peni / days_count * (days_count - 10), 2);
                            row2["dolg_peni"] = Convert.ToDecimal(dataTable.Rows[i][7]);
                            row2["sum_total"] = Convert.ToDecimal(dataTable.Rows[i][8]);
                            row2["dolg_epd"] = 0;
                            row2["difference"] = 0;
                            row2["stavka"] = Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)) == 2015 ? 8.25m : 11m;
                            tableRez.Rows.Add(row2);

                            row2 = tableRez.NewRow();
                            row2["date_from"] =
                                new DateTime(Convert.ToInt32(dataTable.Rows[i][1].ToString().Substring(6, 4)),
                                    Convert.ToInt32(dataTable.Rows[i][1].ToString().Substring(3, 2)), 1).ToShortDateString();
                            row2["date_to"] = dataTable.Rows[i][1].ToString();
                            row2["sum_prih"] = Convert.ToDecimal(dataTable.Rows[i][2]);
                            row2["dat_prih"] = Convert.ToDecimal(dataTable.Rows[i][3]);
                            row2["dolg"] = Convert.ToDecimal(dataTable.Rows[i][4]);
                            row2["days"] = 10;
                            row2["peni"] = peni - Math.Round(peni / days_count * (days_count - 10), 2);
                            row2["dolg_peni"] = Convert.ToDecimal(dataTable.Rows[i][7]);
                            row2["sum_total"] = Convert.ToDecimal(dataTable.Rows[i][8]);
                            row2["dolg_epd"] = 0;
                            row2["difference"] = 0;
                            row2["stavka"] = Convert.ToInt32(dataTable.Rows[i][1].ToString().Substring(6, 4)) == 2015 ? 8.25m : 11m;
                            tableRez.Rows.Add(row2);
                        }
                        else
                        {
                            try
                            {
                                swPeni.WriteLine();
                                int curPerMonth = monthPerFrom;
                                int days_count = Convert.ToInt32(dataTable.Rows[i][5]);
                                int days_count_temp = Convert.ToInt32(dataTable.Rows[i][5]);
                                decimal peni = Convert.ToDecimal(dataTable.Rows[i][6]);
                                swPeni.WriteLine("curPerMonth=" + curPerMonth);
                                swPeni.WriteLine("days_count=" + days_count);
                                swPeni.WriteLine("peni=" + peni);
                                while (curPerMonth < monthPerTo)
                                {
                                    row2 = tableRez.NewRow();
                                    if (curPerMonth == monthPerFrom)
                                        row2["date_from"] =
                                            new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                                curPerMonth,
                                                Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(0, 2)))
                                                .ToShortDateString();
                                    else
                                        row2["date_from"] =
                                            new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                                curPerMonth, 1)
                                                .ToShortDateString();

                                    if (curPerMonth == monthPerFrom)
                                        swPeni.WriteLine("date_from=" + new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                                curPerMonth,
                                                Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(0, 2)))
                                                .ToShortDateString());
                                    else
                                        swPeni.WriteLine("date_from=" + new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                                curPerMonth, 1)
                                                .ToShortDateString());

                                    row2["date_to"] =
                                        new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth,
                                            DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                                curPerMonth)).ToShortDateString
                                            ();
                                    row2["sum_prih"] = 0;
                                    row2["dat_prih"] = Convert.ToDecimal(dataTable.Rows[i][3]);
                                    row2["dolg"] = Convert.ToDecimal(dataTable.Rows[i][4]);
                                    if (curPerMonth == monthPerFrom)
                                        row2["days"] = DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth) - Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(0, 2)) + 1;
                                    else
                                        row2["days"] = DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth);


                                    if (curPerMonth == monthPerFrom)
                                        swPeni.WriteLine("days=" + (DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth) - Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(0, 2)) + 1));
                                    else
                                        swPeni.WriteLine("days=" + DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth));

                                    if (curPerMonth == monthPerFrom)
                                        row2["peni"] = Math.Round(peni / days_count * (DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth) - Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(0, 2)) + 1), 2);
                                    else
                                        row2["peni"] = Math.Round(peni / days_count * (DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth)), 2);


                                    if (curPerMonth == monthPerFrom)
                                        swPeni.WriteLine("peni=" + Math.Round(peni / days_count * (DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth) - Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(0, 2)) + 1), 2));
                                    else
                                        swPeni.WriteLine("peni=" + Math.Round(peni / days_count * (DateTime.DaysInMonth(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth)), 2));

                                    row2["dolg_peni"] = Convert.ToDecimal(dataTable.Rows[i][7]);
                                    row2["sum_total"] = Convert.ToDecimal(dataTable.Rows[i][8]);
                                    row2["dolg_epd"] = 0;
                                    row2["difference"] = 0;
                                    row2["stavka"] = Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)) == 2015 ? 8.25m : 11m;
                                    tableRez.Rows.Add(row2);
                                    if (curPerMonth == monthPerFrom)
                                        days_count_temp = days_count_temp -
                                                      (DateTime.DaysInMonth(
                                                          Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                                          curPerMonth) -
                                                       Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(0, 2)) + 1);
                                    else
                                        days_count_temp = days_count_temp -
                                                      DateTime.DaysInMonth(
                                                          Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                                          curPerMonth);
                                    swPeni.WriteLine("days_count_temp = " + days_count_temp);
                                    curPerMonth++;
                                }
                                row2 = tableRez.NewRow();
                                row2["date_from"] = new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                            curPerMonth, 1).ToShortDateString();
                                row2["date_to"] =
                                    new DateTime(Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)),
                                        curPerMonth, days_count_temp).ToShortDateString();
                                row2["sum_prih"] = 0;
                                row2["dat_prih"] = Convert.ToDecimal(dataTable.Rows[i][3]);
                                row2["dolg"] = Convert.ToDecimal(dataTable.Rows[i][4]);
                                row2["days"] = days_count_temp;
                                row2["peni"] = Math.Round(peni / days_count * days_count_temp, 2);
                                row2["dolg_peni"] = Convert.ToDecimal(dataTable.Rows[i][7]);
                                row2["sum_total"] = Convert.ToDecimal(dataTable.Rows[i][8]);
                                row2["dolg_epd"] = 0;
                                row2["difference"] = 0;
                                row2["stavka"] = Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)) == 2015 ? 8.25m : 11m;
                                tableRez.Rows.Add(row2);
                            }
                            catch (Exception e )
                            {
                                swPeni.WriteLine("error = " + e.ToString());
                            }
                            
                        }
                    }
                    else
                    {
                        row2 = tableRez.NewRow();
                        row2["date_from"] = dataTable.Rows[i][0].ToString();
                        row2["date_to"] = dataTable.Rows[i][1].ToString();
                        row2["sum_prih"] = Convert.ToDecimal(dataTable.Rows[i][2]);
                        row2["dat_prih"] = Convert.ToDecimal(dataTable.Rows[i][3]);
                        row2["dolg"] = Convert.ToDecimal(dataTable.Rows[i][4]);
                        row2["days"] = Convert.ToInt32(dataTable.Rows[i][5]);
                        row2["peni"] = Convert.ToDecimal(dataTable.Rows[i][6]);
                        row2["dolg_peni"] = Convert.ToDecimal(dataTable.Rows[i][7]);
                        row2["sum_total"] = Convert.ToDecimal(dataTable.Rows[i][8]);
                        row2["dolg_epd"] = 0;
                        row2["difference"] = 0;
                        row2["stavka"] = Convert.ToInt32(dataTable.Rows[i][0].ToString().Substring(6, 4)) == 2015 ? 8.25m : 11m;
                        tableRez.Rows.Add(row2);
                    }
                }
                swPeni.Close();
                //for (int i = 0; i < tableRez.Rows.Count; i++)
                //{

                //    tableRez.Rows[i]["peni"] = Math.Round(Convert.ToDecimal(tableRez.Rows[i]["dolg"])*
                //                               (Convert.ToInt32(tableRez.Rows[i]["date_from"].ToString()
                //                                   .Substring(6, 4)) == 2015
                //                                   ? (8.25m/300/100)
                //                                   : (11m/300/100))*
                //                               Convert.ToDecimal(tableRez.Rows[i]["days"]), 2);
                //}


                for (int i = 0; i < tableRez.Rows.Count; i++)
                {
                    if (i == 0 || Convert.ToDecimal(tableRez.Rows[i][6]) == 0)
                        tableRez.Rows[i][7] = Convert.ToDecimal(tableRez.Rows[i][6]);
                    else
                        tableRez.Rows[i][7] = Convert.ToDecimal(tableRez.Rows[i][6]) +
                                              Convert.ToDecimal(tableRez.Rows[i - 1][7]);
                }
                List<string> appr = new List<string>();
                bool isWrite = false;
                string month = "";
                List<string> isWriteepd = new List<string>();
                //StreamWriter sw = new StreamWriter(@"C:\temp\error.log", true);
                //for (int i = 0; i < tableRez.Rows.Count; i++)
                //{
                //    if (i == tableRez.Rows.Count - 1)
                //    {
                //        for (int j = 0; j < dtCharge.Rows.Count; j++)
                //        {
                //            if (tableRez.Rows[i][1].ToString().Substring(3, 2) ==
                //                dtCharge.Rows[j][0].ToString().Substring(3, 2))
                //            {
                //                isWriteepd.Add(dtCharge.Rows[j][0].ToString());
                //                tableRez.Rows[i][9] = dtCharge.Rows[j][1].ToString();
                //            }
                //        }
                //    }
                //    else if (month != tableRez.Rows[i][1].ToString().Substring(3, 2) && !isWrite && i != 0)
                //    {
                //        month = tableRez.Rows[i][1].ToString().Substring(3, 2);
                //        for (int j = 0; j < dtCharge.Rows.Count; j++)
                //        {
                //            if (tableRez.Rows[i - 1][1].ToString().Substring(3, 2) ==
                //                dtCharge.Rows[j][0].ToString().Substring(3, 2))
                //            {
                //                isWriteepd.Add(dtCharge.Rows[j][0].ToString());
                //                tableRez.Rows[i - 1][9] = dtCharge.Rows[j][1].ToString();
                //            }
                //        }
                //    }
                //    else
                //    {
                //        month = tableRez.Rows[i][1].ToString().Substring(3, 2);
                //        isWrite = false;
                //        for (int j = 0; j < dtCharge.Rows.Count; j++)
                //        {
                //            if (tableRez.Rows[i][1].ToString() ==
                //                Convert.ToDateTime(dtCharge.Rows[j][0].ToString()).ToShortDateString())
                //            {
                //                isWriteepd.Add(dtCharge.Rows[j][0].ToString());
                //                tableRez.Rows[i][9] = dtCharge.Rows[j][1].ToString();
                //                isWrite = true;
                //            }
                //        }
                //    }

                //}
                
                //for (int j = 0; j < dtCharge.Rows.Count; j++)
                //{
                //    if (!isWriteepd.Contains(dtCharge.Rows[j][0].ToString()))
                //    {
                //        for (int i = 0; i < tableRez.Rows.Count; i++)
                //        {
                //            if (tableRez.Rows[i][1].ToString().Substring(3, 2) ==
                //               dtCharge.Rows[j][0].ToString().Substring(3, 2))
                //            {
                //                tableRez.Rows[i][9] = dtCharge.Rows[j][1].ToString();
                //                isWriteepd.Add(dtCharge.Rows[j][0].ToString());
                //            }
                //        }
                //    }
                //}
                //sw.Close();
                StreamWriter sw = new StreamWriter(@"C:\temp\dolgPeni.log", true);
                for (int i = 0; i < tableRez.Rows.Count; i++)
                {
                    try
                    {
                        sw.WriteLine(tableRez.Rows[i][1].ToString());
                        int month_ = Convert.ToInt32(tableRez.Rows[i][1].ToString().Substring(3, 2));
                        sw.WriteLine(month_);
                        int year_ = Convert.ToInt32(tableRez.Rows[i][1].ToString().Substring(6, 4));
                        sw.WriteLine(year_);
                        int day = Convert.ToInt32(tableRez.Rows[i][1].ToString().Substring(0, 2));
                        sw.WriteLine(day);
                        int dayInMonth = DateTime.DaysInMonth(year_, month_);
                        sw.WriteLine(dayInMonth);
                        if (dayInMonth == day)
                        {
                            string sqlPeniEpd = @"SELECT               
                                            sum(a.gsum_tarif) + sum(a.rsum_tarif - a.gsum_tarif + a.reval - a.sum_nedop) + sum(a.real_charge) - coalesce(sum(p.sum_rcl), 0) as real_charge         
                                            FROM  bill01_charge_" + (year_ - 2000).ToString("00") + @".charge_" +
                                                month_.ToString("00") + @" a  
                                            LEFT JOIN (SELECT nzp_serv, nzp_supp, sum(sum_rcl) as sum_rcl
		                                            FROM bill01_charge_" + (year_ - 2000).ToString("00") + @".perekidka p
		                                            INNER JOIN fbill_data.document_base d on d.nzp_doc_base = p.nzp_doc_base
		                                            WHERE d.comment = 'Выравнивание сальдо' and p.nzp_user = 1 and month_ = " +
                                                month_ +
                                                @" and nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)
		                                            group by 1,2) p on p.nzp_supp = a.nzp_supp and p.nzp_serv  = a.nzp_serv
                                            WHERE a.nzp_kvar=(SELECT nzp_kvar from t_temp_kvar) AND a.dat_charge is null  and a.nzp_serv = 500";
                            var dtPeniEpd = ExecSQLToTable(sqlPeniEpd);
                            tableRez.Rows[i][9] = dtPeniEpd != null && dtPeniEpd.Rows.Count > 0 && dtPeniEpd.Rows[0][0] != null && dtPeniEpd.Rows[0][0].ToString() != ""
                                ? dtPeniEpd.Rows[0][0].ToString()
                                : "0";
                        }
                        else
                        {
                            tableRez.Rows[i][9] = "0";
                        }
                    }
                    catch (Exception e)
                    {
                        sw.WriteLine(e.ToString());
                    }
                    
                }
                for (int i = 0; i < tableRez.Rows.Count; i++)
                {
                    sw.WriteLine(tableRez.Rows[i]["dolg_epd"].ToString());
                }
                sw.Close();

                for (int i = 0; i < tableRez.Rows.Count; i++)
                {
                    if (Convert.ToDecimal(tableRez.Rows[i]["dolg_epd"]) != 0)
                        tableRez.Rows[i]["difference"] = Convert.ToDecimal(tableRez.Rows[i]["dolg_epd"]) -
                                                         Convert.ToDecimal(tableRez.Rows[i]["dolg_peni"]);
                }
                decimal dolg_peni = Convert.ToDecimal(tableRez.Rows[tableRez.Rows.Count - 1][7]);
                tableRez.TableName = "Q_master";
                ds.Tables.Add(tableRez);

                decimal totalPeni = 0;
                for (int i = 0; i < tableRez.Rows.Count; i++)
                {
                    totalPeni += Convert.ToDecimal(tableRez.Rows[i]["peni"]);
                }

                sql2 = "SELECT " + totalPeni + " as total_peni from t_svod";

                var dt6 = ExecSQLToTable(sql2.ToString());
                dt6.TableName = "Q_master6";
                ds.Tables.Add(dt6);

                decimal totalDolgEpd = 0;
                for (int i = 0; i < tableRez.Rows.Count; i++)
                {
                    totalDolgEpd += Convert.ToDecimal(tableRez.Rows[i]["dolg_epd"]);
                }

                sql2 = "SELECT " + totalDolgEpd + " as total_dolg_epd from t_svod";

                var dt7 = ExecSQLToTable(sql2.ToString());
                dt7.TableName = "Q_master7";
                ds.Tables.Add(dt7);

                sql2 = "SELECT dolg, sum_total from t_svod where date_from = (SELECT max(date_from) from t_svod)";

                var dt3 = ExecSQLToTable(sql2.ToString());
                dt3.TableName = "Q_master3";
                ds.Tables.Add(dt3);

                sql2 = "SELECT " + dolg_peni + " as dolg_peni from t_svod";

                var dt5 = ExecSQLToTable(sql2.ToString());
                dt5.TableName = "Q_master4";
                ds.Tables.Add(dt5);
            }
            else
            {
                sql2 = "SELECT 0 as total_dolg_epd from t_svod";

                var dt7 = ExecSQLToTable(sql2.ToString());
                dt7.TableName = "Q_master7";
                ds.Tables.Add(dt7);

                sql2 = "SELECT dolg, sum_total from t_svod where date_from = (SELECT max(date_from) from t_svod)";

                var dt3 = ExecSQLToTable(sql2.ToString());
                dt3.TableName = "Q_master3";
                ds.Tables.Add(dt3);

                sql2 = "SELECT 0 as total_peni from t_svod";

                var dt6 = ExecSQLToTable(sql2.ToString());
                dt6.TableName = "Q_master6";
                ds.Tables.Add(dt6);

                tableRez.TableName = "Q_master";
                ds.Tables.Add(tableRez);
            }

               string sql3 = @"SELECT fio, num_ls, address
FROM(SELECT 1 as id, fio, num_ls FROM bill01_data.kvar where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t1
LEFT JOIN(
SELECT u.ulica || ', дом № ' || d.ndom || ', кв. ' || k.nkvar || ' ком. ' || coalesce(k.nkvar_n, '-') as address, 1 as id
FROM bill01_data.kvar k 
INNER JOIN bill01_data.dom d on d.nzp_dom = k.nzp_dom 
INNER JOIN bill01_data.s_ulica u on u.nzp_ul = d.nzp_ul where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t2 on t2.id = t1.id";
          
            var dt2 = ExecSQLToTable(sql3.ToString());
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
                "sum_money NUMERIC(14,2) default 0," +
                "dat_prih DATE," +
                "dolg NUMERIC(14,2) default 0," +
                "days INTEGER default 0," +
                "peni NUMERIC(14,2) default 0," +
                "dolg_peni NUMERIC(14,2) default 0," +
                "sum_total NUMERIC(14,2) default 0," +
                "dolg_epd NUMERIC(14,2) default 0)");
                

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
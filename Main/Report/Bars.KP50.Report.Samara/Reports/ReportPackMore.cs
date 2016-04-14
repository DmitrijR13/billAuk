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
    class ReportPackMore : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Распределение платежей детализация"; }
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
            get { return Resources.ReportPackMore; }
        }

        #region Значения параметров отчета

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

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
            var pref = "fbill";
            var localPref = "bill01";
            string finTable = pref + "_fin_" + (DatS.Year - 2000).ToString("00");
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

            sql = " SELECT pl.nzp_pack_ls as nzp_pack_ls, pl.dat_month as dat_month, pl.dat_vvod, k.nzp_kvar, b.bank as bank" +
                  " FROM " + finTable + ".pack_ls pl " +
                  " INNER JOIN " + finTable + ".pack p on p.nzp_pack = pl.nzp_pack " +
                  " INNER JOIN fbill_kernel.s_bank b on b.nzp_bank = p.nzp_bank " +
                  " INNER JOIN " + dataTable + ".kvar k on k.num_ls = pl.num_ls " +
                  " INNER JOIN " + dataTable + ".dom d on d.nzp_dom = k.nzp_dom  " +
                  " INNER JOIN " + dataTable + ".s_ulica ul on ul.nzp_ul = d.nzp_ul " +
                  " where pl.dat_vvod >= '" + DatS + "' AND pl.dat_vvod <= '" + DatPo + "' " +
                  " and k.nzp_kvar in (SELECT nzp_kvar from t_temp_kvar) order by 4,2";
            var dt = ExecSQLToTable(sql.ToString());
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                String month = Convert.ToDateTime(dt.Rows[i]["dat_month"]).Month.ToString().PadLeft(2, '0');
                Int32 year = Convert.ToDateTime(dt.Rows[i]["dat_month"]).Year;
                Int32 nzp_pack_ls = Convert.ToInt32(dt.Rows[i]["nzp_pack_ls"]);
                Int32 nzp_kvar = Convert.ToInt32(dt.Rows[i]["nzp_kvar"]);
                DateTime dat_vvod = Convert.ToDateTime(dt.Rows[i]["dat_vvod"]);
                String bank = dt.Rows[i]["bank"].ToString();
                string chargeTable = localPref + "_charge_" + (year - 2000).ToString("00");
                string kernelTable = pref + "_kernel";
                sql = @"INSERT INTO t_svod_pack
                        SELECT c.nzp_kvar, s.service, sup.name_supp, c.sum_insaldo, c.rsum_tarif, c.reval - c.sum_nedop + coalesce(p.sum_rcl, 0) as reval,
                        c.rsum_tarif + c.reval - c.sum_nedop + coalesce(p.sum_rcl, 0) as sum_charge, coalesce(pack.sum_prih, 0)::numeric(14,2) as sum_prih, c.sum_outsaldo,
                        ttk.address, ttk.num_ls, ttk.fio, to_date('" + dat_vvod.ToShortDateString() + @"', 'dd.mm.yyyy') as dat_vvod, '" + bank + @"' as bank
                        FROM " + chargeTable + @".charge_" + month + @" c
                        INNER JOIN t_temp_kvar ttk on ttk.nzp_kvar = c.nzp_kvar and ttk.nzp_kvar = " + nzp_kvar + @"
                        INNER JOIN " + kernelTable + @".services s on s.nzp_serv = c.nzp_serv
                        INNER JOIN " + kernelTable + @".supplier sup on sup.nzp_supp = c.nzp_supp
                        LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_rcl) as sum_rcl from " + chargeTable + @".perekidka 
	                        where month_ = " + month + @" group by 1,2) p on p.nzp_kvar = c.nzp_kvar and p.nzp_serv = c.nzp_serv
                        LEFT JOIN (SELECT sum(sum_prih) as sum_prih, nzp_serv, nzp_supp FROM (SELECT sum_prih, nzp_serv, nzp_supp FROM " + chargeTable + @".fn_supplier" + month + @" 
                        where nzp_pack_ls in (" + nzp_pack_ls + @") UNION ALL 
                        SELECT sum_prih, nzp_serv, nzp_supp FROM " + chargeTable + @".from_supplier where nzp_pack_ls in (" + nzp_pack_ls + @")
                        ) t group by 2,3) pack on pack.nzp_serv = c.nzp_serv and pack.nzp_supp = c.nzp_supp
                        where c.nzp_kvar in (" + nzp_kvar + @") and c.nzp_serv != 1
                        order by 10,13,2";
                ExecSQL(sql.ToString());
            }
            var ds = new DataSet();
            var dt2 = ExecSQLToTable("SELECT * FROM t_svod_pack order by 1,2");
            for (int i = 0; i < dt2.Rows.Count; i++)
            {
                dt2.Rows[i]["name_supp"] = dt2.Rows[i]["name_supp"].ToString().Split(',')[1];
            }
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
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;

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

            report.SetParameterValue("datS", DatS.ToShortDateString());

            report.SetParameterValue("datPo", DatPo.ToShortDateString());
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            ExecSQL("create temp table t_svod_pack (nzp_kvar INTEGER, service CHARACTER(100)," +
                "name_supp CHARACTER(100)," +
                "sum_insaldo NUMERIC(14,2) default 0," +
                "rsum_tarif NUMERIC(14,2) default 0," +
                "reval NUMERIC(14,2) default 0," +
                "sum_charge NUMERIC(14,2) default 0," +
                "sum_prih NUMERIC(14,2) default 0," +
                "sum_outsaldo NUMERIC(14,2) default 0, " +
                "address CHARACTER(100),num_ls CHARACTER(12),fio CHARACTER(100), dat_vvod DATE, bank CHARACTER(100))");

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
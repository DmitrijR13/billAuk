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
    class ReportJudgment : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Справка в суд"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по справке в суд"; }
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
            get { return Resources.ReportJudgment; }
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
            if (YearFrom == YearTo)
            {
                if (MonthTo == MonthFrom)
                {
                    string chargeTable = pref + "_charge_" + (YearTo - 2000).ToString("00") + ".charge_" + MonthTo.ToString("00");
                    string perekidkaTable = pref + "_charge_" + (YearTo - 2000).ToString("00") + ".perekidka";
                    string fnTable = pref + "_charge_" + (YearTo - 2000).ToString("00") + ".fn_supplier" + MonthTo.ToString("00");
                    sql = @"insert into t_svod
                            SELECT "+Convert.ToInt32(MonthTo.ToString("00"))  + @" as month, "+YearTo+" as year, '" + MonthTo.ToString("00") + "' || '.' || '" + (YearTo - 2000).ToString("00") + @"' as date_,  
                            t1.sum_tarif - t21.sum_tarif as sum_tarif_total, t21.sum_tarif as sum_tarif_500, t2.sum_tarif as sum_tarif_7, t2.tarif as tarif_7, 
                            t3.sum_tarif + coalesce(t22.sum_tarif, 0) + coalesce(t23.sum_tarif, 0) as sum_tarif_9, CASE WHEN (t3.tarif != 0) THEN t3.tarif ELSE t22.tarif END as tarif_9, t4.sum_tarif as sum_tarif_15, 
                            t4.tarif as tarif_15, t5.sum_tarif as sum_tarif_8, t5.tarif as tarif_8, t6.sum_tarif as sum_tarif_17, t6.tarif as tarif_17, t7.sum_tarif as sum_tarif_2, t7.tarif as tarif_2, 
                            t8.sum_tarif as sum_tarif_22, t8.tarif as tarif_22, 
                            t9.sum_tarif + coalesce(t24.sum_tarif, 0) + coalesce(t25.sum_tarif, 0) as sum_tarif_14, CASE WHEN (t9.tarif != 0) THEN t9.tarif ELSE t24.tarif END as tarif_14, 
                            t10.sum_tarif + coalesce(t13.sum_tarif, 0) as sum_tarif_25, 
                            CASE WHEN (t10.tarif != 0) THEN t10.tarif ELSE t13.tarif END as tarif_25, t14.sum_tarif as sum_tarif_100018, t14.tarif as tarif_100018,
                            t11.sum_money as sum_money, to_char(t12.dat_prih, 'yyyy-mm-dd') as dat_prih, coalesce(t15.reval, 0) + coalesce(t16.reval, 0) as reval, 0, 0, 
                            t17.sum_tarif as sum_tarif_100019, t17.tarif as tarif_100019,
                            coalesce(t18.sum_tarif, 0) + coalesce(t19.sum_tarif, 0) + coalesce(t20.sum_tarif, 0) as sum_tarif_6, CASE WHEN (t18.tarif != 0) THEN t18.tarif ELSE t19.tarif END as tarif_6,        
                            t26.sum_tarif as sum_tarif_26, t26.tarif as tarif_26, t27.sum_tarif + coalesce(t28.sum_tarif, 0) as sum_tarif_210, 
                            CASE WHEN (t27.tarif != 0) THEN t27.tarif ELSE t28.tarif END as tarif_210, t33.sum_tarif as sum_tarif_100020, t33.tarif as tarif_100020,
                            t34.sum_tarif as sum_tarif_100021, t34.tarif as tarif_100021
                            FROM (SELECT 1 as id, sum_tarif 
                            from " + chargeTable + @" c1
                            where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t1
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 7 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t2 on t1.id = t2.id
                            LEFT JOIN (SELECT 1 as id, sum_tarif 
                            from " + chargeTable + @" c1
                            where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t21 on t1.id = t21.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 9 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t3 on t1.id = t3.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 15 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t4 on t1.id = t4.id
                           LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 8 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t5 on t1.id = t5.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 17 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t6 on t1.id = t6.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 2 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t7 on t1.id = t7.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 22 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t8 on t1.id = t8.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 14 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t9 on t1.id = t9.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 25 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t10 on t1.id = t10.id
                            LEFT JOIN (SELECT 1 as id, sum_money
                            from " + chargeTable + @" c1
                            where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t11 on t1.id = t11.id
                            LEFT JOIN (SELECT 1 as id, reval
                            from " + chargeTable + @" c1
                            where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t15 on t1.id = t15.id
                            LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                            from " + perekidkaTable + @" c1
                            where month_ = " + MonthTo + @" AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t16 on t1.id = t16.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100018 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t14 on t1.id = t14.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100019 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t17 on t1.id = t17.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 515 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t13 on t1.id = t13.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 6 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t18 on t1.id = t18.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 510 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t19 on t1.id = t19.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 1010050 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t20 on t1.id = t20.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 513 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t22 on t1.id = t22.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 1010052 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t23 on t1.id = t23.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 514 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t24 on t1.id = t24.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 1010053 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t25 on t1.id = t25.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 26 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t26 on t1.id = t26.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 210 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t27 on t1.id = t27.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 516 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t28 on t1.id = t28.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100020 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t33 on t1.id = t33.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100021 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t34 on t1.id = t34.id
                            LEFT JOIN (SELECT max(dat_prih) as dat_prih, 1 as id 
                            FROM " + fnTable + @" fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t12 on t1.id = t12.id LIMIT 1";
                    ExecSQL(sql.ToString());
                }
                else
                {
                    for (int i = MonthFrom; i <= MonthTo; i++)
                    {
                        string chargeTable = pref + "_charge_" + (YearTo - 2000).ToString("00") + ".charge_" + i.ToString("00");
                        string perekidkaTable = pref + "_charge_" + (YearTo - 2000).ToString("00") + ".perekidka";
                        string fnTable = pref + "_charge_" + (YearTo - 2000).ToString("00") + ".fn_supplier" + i.ToString("00");
                        sql = @"insert into t_svod
                            SELECT " + Convert.ToInt32(i.ToString("00")) + @" as month, " + YearTo + " as year, '" + i.ToString("00") + "' || '.' || '" + (YearTo - 2000).ToString("00") + @"' as date_, 
                            t1.sum_tarif - t21.sum_tarif as sum_tarif_total, t21.sum_tarif as sum_tarif_500, t2.sum_tarif as sum_tarif_7, t2.tarif as tarif_7, 
                            t3.sum_tarif + coalesce(t22.sum_tarif, 0) + coalesce(t23.sum_tarif, 0) as sum_tarif_9, CASE WHEN (t3.tarif != 0) THEN t3.tarif ELSE t22.tarif END as tarif_9, t4.sum_tarif as sum_tarif_15, 
                            t4.tarif as tarif_15, t5.sum_tarif as sum_tarif_8, t5.tarif as tarif_8, t6.sum_tarif as sum_tarif_17, t6.tarif as tarif_17, t7.sum_tarif as sum_tarif_2, t7.tarif as tarif_2, 
                            t8.sum_tarif as sum_tarif_22, t8.tarif as tarif_22, 
                            t9.sum_tarif + coalesce(t24.sum_tarif, 0) + coalesce(t25.sum_tarif, 0) as sum_tarif_14, CASE WHEN (t9.tarif != 0) THEN t9.tarif ELSE t24.tarif END as tarif_14, 
                            t10.sum_tarif + coalesce(t13.sum_tarif, 0) as sum_tarif_25, 
                            CASE WHEN (t10.tarif != 0) THEN t10.tarif ELSE t13.tarif END as tarif_25, t14.sum_tarif as sum_tarif_100018, t14.tarif as tarif_100018,
                            t11.sum_money as sum_money, to_char(t12.dat_prih, 'yyyy-mm-dd') as dat_prih, coalesce(t15.reval, 0) + coalesce(t16.reval, 0) as reval, 0, 0, 
                            coalesce(t30.sum_money, 0) as sum_money_peni, coalesce(t31.reval, 0) + coalesce(t32.reval, 0) as reval_peni,
                            t17.sum_tarif as sum_tarif_100019, t17.tarif as tarif_100019,
                            coalesce(t18.sum_tarif, 0) + coalesce(t19.sum_tarif, 0) + coalesce(t20.sum_tarif, 0) as sum_tarif_6, CASE WHEN (t18.tarif != 0) THEN t18.tarif ELSE t19.tarif END as tarif_6,        
                            t26.sum_tarif as sum_tarif_26, t26.tarif as tarif_26, t27.sum_tarif + coalesce(t28.sum_tarif, 0) as sum_tarif_210, 
                            CASE WHEN (t27.tarif != 0) THEN t27.tarif ELSE t28.tarif END as tarif_210, t33.sum_tarif as sum_tarif_100020, t33.tarif as tarif_100020,
                            t34.sum_tarif as sum_tarif_100021, t34.tarif as tarif_100021
                            FROM (SELECT 1 as id, sum_tarif 
                            from " + chargeTable + @" c1
                            where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t1
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 7 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t2 on t1.id = t2.id
                            LEFT JOIN (SELECT 1 as id, sum(sum_tarif) as sum_tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t21 on t1.id = t21.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 9 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t3 on t1.id = t3.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 15 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t4 on t1.id = t4.id
                           LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 8 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t5 on t1.id = t5.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 17 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t6 on t1.id = t6.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 2 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t7 on t1.id = t7.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 22 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t8 on t1.id = t8.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 14 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t9 on t1.id = t9.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 25 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t10 on t1.id = t10.id
                            LEFT JOIN (SELECT 1 as id, sum_money
                            from " + chargeTable + @" c1
                            where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t11 on t1.id = t11.id
                            LEFT JOIN (SELECT 1 as id, reval
                            from " + chargeTable + @" c1
                            where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t15 on t1.id = t15.id
                            LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                            from " + perekidkaTable + @" c1
                            where month_ = "+i+@" AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t16 on t1.id = t16.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 515 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t13 on t1.id = t13.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 6 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t18 on t1.id = t18.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 510 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t19 on t1.id = t19.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 1010050 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t20 on t1.id = t20.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 513 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t22 on t1.id = t22.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 1010052 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t23 on t1.id = t23.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 514 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t24 on t1.id = t24.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 1010053 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t25 on t1.id = t25.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 26 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t26 on t1.id = t26.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 210 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t27 on t1.id = t27.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 516 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t28 on t1.id = t28.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100020 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t33 on t1.id = t33.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100021 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t34 on t1.id = t34.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100018 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t14 on t1.id = t14.id
                            LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                            from " + chargeTable + @" c1
                            where nzp_serv = 100019 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t17 on t1.id = t17.id
                            LEFT JOIN (SELECT max(dat_prih) as dat_prih, 1 as id 
                            FROM " + fnTable + @" fs
                            INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                            WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t12 on t1.id = t12.id 
                            LEFT JOIN (SELECT 1 as id, sum(sum_money) as sum_money
                            from " + chargeTable + @" c1
                            where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t30 on t30.id = t1.id
                            LEFT JOIN (SELECT 1 as id, sum(reval) as reval
                            from " + chargeTable + @" c1
                            where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t31 on t31.id = t1.id
                            LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                            from " + perekidkaTable + @" c1
                            where month_ = " + i + @" AND nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t32 on t32.id = t1.id
                            LIMIT 1";
                        ExecSQL(sql.ToString());
                    }
                    
                }
            }
            else
            {
                for (int y = YearFrom; y <= YearTo; y++)
                {
                    if (y == YearFrom)
                    {                     
                        for (int i = MonthFrom; i <= 12; i++)
                        {
                            string chargeTable = pref + "_charge_" + (y - 2000).ToString("00") + ".charge_" + i.ToString("00");
                            string perekidkaTable = pref + "_charge_" + (y - 2000).ToString("00") + ".perekidka";
                            string fnTable = pref + "_charge_" + (y - 2000).ToString("00") + ".fn_supplier" + i.ToString("00");
                            sql = @"insert into t_svod
                                SELECT " + Convert.ToInt32(i.ToString("00")) + @" as month, " + y + " as year,'" + i.ToString("00") + "' || '.' || '" + (y - 2000).ToString("00") + @"' as date_, 
                                t1.sum_tarif - t21.sum_tarif as sum_tarif_total, t21.sum_tarif as sum_tarif_500, t2.sum_tarif as sum_tarif_7, t2.tarif as tarif_7, 
                                t3.sum_tarif + coalesce(t22.sum_tarif, 0) + coalesce(t23.sum_tarif, 0) as sum_tarif_9, CASE WHEN (t3.tarif != 0) THEN t3.tarif ELSE t22.tarif END as tarif_9, t4.sum_tarif as sum_tarif_15, 
                                t4.tarif as tarif_15, t5.sum_tarif as sum_tarif_8, t5.tarif as tarif_8, t6.sum_tarif as sum_tarif_17, t6.tarif as tarif_17, t7.sum_tarif as sum_tarif_2, t7.tarif as tarif_2, 
                                t8.sum_tarif as sum_tarif_22, t8.tarif as tarif_22, 
                                t9.sum_tarif + coalesce(t24.sum_tarif, 0) + coalesce(t25.sum_tarif, 0) as sum_tarif_14, CASE WHEN (t9.tarif != 0) THEN t9.tarif ELSE t24.tarif END as tarif_14, 
                                t10.sum_tarif + coalesce(t13.sum_tarif, 0) as sum_tarif_25, 
                                CASE WHEN (t10.tarif != 0) THEN t10.tarif ELSE t13.tarif END as tarif_25, t14.sum_tarif as sum_tarif_100018, t14.tarif as tarif_100018,
                                t11.sum_money as sum_money, to_char(t12.dat_prih, 'yyyy-mm-dd') as dat_prih, coalesce(t15.reval, 0) + coalesce(t16.reval, 0) as reval, 0, 0, 
                                coalesce(t30.sum_money, 0) as sum_money_peni, coalesce(t31.reval, 0) + coalesce(t32.reval, 0) as reval_peni,
                                t17.sum_tarif as sum_tarif_100019, t17.tarif as tarif_100019,
                                coalesce(t18.sum_tarif, 0) + coalesce(t19.sum_tarif, 0) + coalesce(t20.sum_tarif, 0) as sum_tarif_6, CASE WHEN (t18.tarif != 0) THEN t18.tarif ELSE t19.tarif END as tarif_6,        
                                t26.sum_tarif as sum_tarif_26, t26.tarif as tarif_26, t27.sum_tarif + coalesce(t28.sum_tarif, 0) as sum_tarif_210, 
                                CASE WHEN (t27.tarif != 0) THEN t27.tarif ELSE t28.tarif END as tarif_210, t33.sum_tarif as sum_tarif_100020, t33.tarif as tarif_100020,
                                t34.sum_tarif as sum_tarif_100021, t34.tarif as tarif_100021   
                                FROM (SELECT 1 as id, sum_tarif 
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t1
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 7 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t2 on t1.id = t2.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_tarif) as sum_tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t21 on t1.id = t21.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 9 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t3 on t1.id = t3.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 15 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t4 on t1.id = t4.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 8 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t5 on t1.id = t5.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 17 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t6 on t1.id = t6.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 2 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t7 on t1.id = t7.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 22 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t8 on t1.id = t8.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 14 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t9 on t1.id = t9.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 25 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t10 on t1.id = t10.id
                                LEFT JOIN (SELECT 1 as id, sum_money
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t11 on t1.id = t11.id
                                LEFT JOIN (SELECT 1 as id, reval
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t15 on t1.id = t15.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                                from " + perekidkaTable + @" c1
                                where month_ = " + i + @" AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t16 on t1.id = t16.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 515 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t13 on t1.id = t13.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100018 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t14 on t1.id = t14.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100019 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t17 on t1.id = t17.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 6 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t18 on t1.id = t18.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 510 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t19 on t1.id = t19.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010050 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t20 on t1.id = t20.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 513 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t22 on t1.id = t22.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010052 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t23 on t1.id = t23.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 514 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t24 on t1.id = t24.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010053 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t25 on t1.id = t25.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 26 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t26 on t1.id = t26.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 210 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t27 on t1.id = t27.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 516 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t28 on t1.id = t28.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100020 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t33 on t1.id = t33.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100021 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t34 on t1.id = t34.id
                                LEFT JOIN (SELECT max(dat_prih) as dat_prih, 1 as id 
                                FROM " + fnTable + @" fs
                                INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                                WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t12 on t1.id = t12.id 
                                LEFT JOIN (SELECT 1 as id, sum(sum_money) as sum_money
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t30 on t30.id = t1.id
                                LEFT JOIN (SELECT 1 as id, sum(reval) as reval
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t31 on t31.id = t1.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                                from " + perekidkaTable + @" c1
                                where month_ = " + i + @" AND nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t32 on t32.id = t1.id
                                LIMIT 1";
                            ExecSQL(sql.ToString());
                        }

                    }
                    else if (y == YearTo)
                    {
                        for (int i = 1; i <= MonthTo; i++)
                        {
                            string chargeTable = pref + "_charge_" + (y - 2000).ToString("00") + ".charge_" + i.ToString("00");
                            string perekidkaTable = pref + "_charge_" + (y - 2000).ToString("00") + ".perekidka";
                            string fnTable = pref + "_charge_" + (y - 2000).ToString("00") + ".fn_supplier" + i.ToString("00");
                            sql = @"insert into t_svod
                                SELECT " + Convert.ToInt32(i.ToString("00")) + @" as month, " + y + " as year,'" + i.ToString("00") + "' || '.' || '" + (y - 2000).ToString("00") + @"' as date_, 
                                t1.sum_tarif - t21.sum_tarif as sum_tarif_total, t21.sum_tarif as sum_tarif_500, t2.sum_tarif as sum_tarif_7, t2.tarif as tarif_7, 
                                t3.sum_tarif + coalesce(t22.sum_tarif, 0) + coalesce(t23.sum_tarif, 0) as sum_tarif_9, CASE WHEN (t3.tarif != 0) THEN t3.tarif ELSE t22.tarif END as tarif_9, t4.sum_tarif as sum_tarif_15, 
                                t4.tarif as tarif_15, t5.sum_tarif as sum_tarif_8, t5.tarif as tarif_8, t6.sum_tarif as sum_tarif_17, t6.tarif as tarif_17, t7.sum_tarif as sum_tarif_2, t7.tarif as tarif_2, 
                                t8.sum_tarif as sum_tarif_22, t8.tarif as tarif_22, 
                                t9.sum_tarif + coalesce(t24.sum_tarif, 0) + coalesce(t25.sum_tarif, 0) as sum_tarif_14, CASE WHEN (t9.tarif != 0) THEN t9.tarif ELSE t24.tarif END as tarif_14, 
                                t10.sum_tarif + coalesce(t13.sum_tarif, 0) as sum_tarif_25, 
                                CASE WHEN (t10.tarif != 0) THEN t10.tarif ELSE t13.tarif END as tarif_25, t14.sum_tarif as sum_tarif_100018, t14.tarif as tarif_100018,
                                t11.sum_money as sum_money, to_char(t12.dat_prih, 'yyyy-mm-dd') as dat_prih, coalesce(t15.reval, 0) + coalesce(t16.reval, 0) as reval, 0, 0, 
                                coalesce(t30.sum_money, 0) as sum_money_peni, coalesce(t31.reval, 0) + coalesce(t32.reval, 0) as reval_peni,
                                t17.sum_tarif as sum_tarif_100019, t17.tarif as tarif_100019,
                                coalesce(t18.sum_tarif, 0) + coalesce(t19.sum_tarif, 0) + coalesce(t20.sum_tarif, 0) as sum_tarif_6, CASE WHEN (t18.tarif != 0) THEN t18.tarif ELSE t19.tarif END as tarif_6,        
                                t26.sum_tarif as sum_tarif_26, t26.tarif as tarif_26, t27.sum_tarif + coalesce(t28.sum_tarif, 0) as sum_tarif_210, 
                                CASE WHEN (t27.tarif != 0) THEN t27.tarif ELSE t28.tarif END as tarif_210, t33.sum_tarif as sum_tarif_100020, t33.tarif as tarif_100020,
                                t34.sum_tarif as sum_tarif_100021, t34.tarif as tarif_100021   
                                FROM (SELECT 1 as id, sum_tarif 
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t1
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 7 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t2 on t1.id = t2.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_tarif) as sum_tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t21 on t1.id = t21.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 9 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t3 on t1.id = t3.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 15 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t4 on t1.id = t4.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 8 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t5 on t1.id = t5.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 17 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t6 on t1.id = t6.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 2 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t7 on t1.id = t7.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 22 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t8 on t1.id = t8.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 14 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t9 on t1.id = t9.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 25 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t10 on t1.id = t10.id
                                LEFT JOIN (SELECT 1 as id, sum_money
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t11 on t1.id = t11.id
                                LEFT JOIN (SELECT 1 as id, reval
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t15 on t1.id = t15.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                                from " + perekidkaTable + @" c1
                                where month_ = " + i + @" AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t16 on t1.id = t16.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 515 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t13 on t1.id = t13.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100018 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t14 on t1.id = t14.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100019 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t17 on t1.id = t17.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 6 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t18 on t1.id = t18.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 510 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t19 on t1.id = t19.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010050 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t20 on t1.id = t20.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 513 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t22 on t1.id = t22.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010052 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t23 on t1.id = t23.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 514 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t24 on t1.id = t24.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010053 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t25 on t1.id = t25.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 26 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t26 on t1.id = t26.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 210 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t27 on t1.id = t27.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 516 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t28 on t1.id = t28.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100020 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t33 on t1.id = t33.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100021 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t34 on t1.id = t34.id
                                LEFT JOIN (SELECT max(dat_prih) as dat_prih, 1 as id 
                                FROM " + fnTable + @" fs
                                INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                                WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t12 on t1.id = t12.id 
                                LEFT JOIN (SELECT 1 as id, sum(sum_money) as sum_money
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t30 on t30.id = t1.id
                                LEFT JOIN (SELECT 1 as id, sum(reval) as reval
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t31 on t31.id = t1.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                                from " + perekidkaTable + @" c1
                                where month_ = " + i + @" AND nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t32 on t32.id = t1.id
                                LIMIT 1";
                            ExecSQL(sql.ToString());
                        }

                    }
                    else
                    {
                        for (int i = 1; i <= 12; i++)
                        {
                            string chargeTable = pref + "_charge_" + (y - 2000).ToString("00") + ".charge_" + i.ToString("00");
                            string perekidkaTable = pref + "_charge_" + (y - 2000).ToString("00") + ".perekidka";
                            string fnTable = pref + "_charge_" + (y - 2000).ToString("00") + ".fn_supplier" + i.ToString("00");
                            sql = @"insert into t_svod
                                SELECT " + Convert.ToInt32(i.ToString("00")) + @" as month, " + y + " as year,'" + i.ToString("00") + "' || '.' || '" + (y - 2000).ToString("00") + @"' as date_, 
                                t1.sum_tarif - t21.sum_tarif as sum_tarif_total, t21.sum_tarif as sum_tarif_500, t2.sum_tarif as sum_tarif_7, t2.tarif as tarif_7, 
                                t3.sum_tarif + coalesce(t22.sum_tarif, 0) + coalesce(t23.sum_tarif, 0) as sum_tarif_9, CASE WHEN (t3.tarif != 0) THEN t3.tarif ELSE t22.tarif END as tarif_9, t4.sum_tarif as sum_tarif_15, 
                                t4.tarif as tarif_15, t5.sum_tarif as sum_tarif_8, t5.tarif as tarif_8, t6.sum_tarif as sum_tarif_17, t6.tarif as tarif_17, t7.sum_tarif as sum_tarif_2, t7.tarif as tarif_2, 
                                t8.sum_tarif as sum_tarif_22, t8.tarif as tarif_22, 
                                t9.sum_tarif + coalesce(t24.sum_tarif, 0) + coalesce(t25.sum_tarif, 0) as sum_tarif_14, CASE WHEN (t9.tarif != 0) THEN t9.tarif ELSE t24.tarif END as tarif_14, 
                                t10.sum_tarif + coalesce(t13.sum_tarif, 0) as sum_tarif_25, 
                                CASE WHEN (t10.tarif != 0) THEN t10.tarif ELSE t13.tarif END as tarif_25, t14.sum_tarif as sum_tarif_100018, t14.tarif as tarif_100018,
                                t11.sum_money as sum_money, to_char(t12.dat_prih, 'yyyy-mm-dd') as dat_prih, coalesce(t15.reval, 0) + coalesce(t16.reval, 0) as reval, 0, 0, 
                                coalesce(t30.sum_money, 0) as sum_money_peni, coalesce(t31.reval, 0) + coalesce(t32.reval, 0) as reval_peni,
                                t17.sum_tarif as sum_tarif_100019, t17.tarif as tarif_100019,
                                coalesce(t18.sum_tarif, 0) + coalesce(t19.sum_tarif, 0) + coalesce(t20.sum_tarif, 0) as sum_tarif_6, CASE WHEN (t18.tarif != 0) THEN t18.tarif ELSE t19.tarif END as tarif_6,        
                                t26.sum_tarif as sum_tarif_26, t26.tarif as tarif_26, t27.sum_tarif + coalesce(t28.sum_tarif, 0) as sum_tarif_210, 
                                CASE WHEN (t27.tarif != 0) THEN t27.tarif ELSE t28.tarif END as tarif_210, t33.sum_tarif as sum_tarif_100020, t33.tarif as tarif_100020,
                                t34.sum_tarif as sum_tarif_100021, t34.tarif as tarif_100021      
                                FROM (SELECT 1 as id, sum_tarif 
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t1
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 7 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t2 on t1.id = t2.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_tarif) as sum_tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t21 on t1.id = t21.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 9 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t3 on t1.id = t3.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 15 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t4 on t1.id = t4.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 8 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t5 on t1.id = t5.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 17 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t6 on t1.id = t6.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 2 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t7 on t1.id = t7.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 22 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t8 on t1.id = t8.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 14 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t9 on t1.id = t9.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 25 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t10 on t1.id = t10.id
                                LEFT JOIN (SELECT 1 as id, sum_money
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t11 on t1.id = t11.id
                                LEFT JOIN (SELECT 1 as id, reval
                                from " + chargeTable + @" c1
                                where nzp_serv = 1 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null) t15 on t1.id = t15.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                                from " + perekidkaTable + @" c1
                                where month_ = " + i + @" AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t16 on t1.id = t16.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 515 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t13 on t1.id = t13.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100018 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t14 on t1.id = t14.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100019 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t17 on t1.id = t17.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 6 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t18 on t1.id = t18.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 510 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t19 on t1.id = t19.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010050 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t20 on t1.id = t20.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 513 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t22 on t1.id = t22.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010052 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t23 on t1.id = t23.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 514 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t24 on t1.id = t24.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 1010053 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t25 on t1.id = t25.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 26 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t26 on t1.id = t26.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 210 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t27 on t1.id = t27.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 516 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t28 on t1.id = t28.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100020 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t33 on t1.id = t33.id
                                LEFT JOIN (SELECT 1 as id, max(sum_tarif) as sum_tarif, max(tarif) as tarif
                                from " + chargeTable + @" c1
                                where nzp_serv = 100021 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t34 on t1.id = t34.id
                                LEFT JOIN (SELECT max(dat_prih) as dat_prih, 1 as id 
                                FROM " + fnTable + @" fs
                                INNER JOIN bill01_data.kvar k on k.num_ls = fs.num_ls
                                WHERE k.nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t12 on t1.id = t12.id 
                                LEFT JOIN (SELECT 1 as id, sum(sum_money) as sum_money
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t30 on t30.id = t1.id
                                LEFT JOIN (SELECT 1 as id, sum(reval) as reval
                                from " + chargeTable + @" c1
                                where nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) and dat_charge is null group by 1) t31 on t31.id = t1.id
                                LEFT JOIN (SELECT 1 as id, sum(sum_rcl) as reval
                                from " + perekidkaTable + @" c1
                                where month_ = " + i + @" AND nzp_serv = 500 AND nzp_kvar = (SELECT nzp_kvar from t_temp_kvar) group by 1) t32 on t32.id = t1.id
                                LIMIT 1";
                            ExecSQL(sql.ToString());
                        }

                    }
                }
                
            }

            string sql3 = @"SELECT fio, num_ls, address, privatized, t4.val_prm as total_area, t6.val_prm as living_area, t7.val_prm as people_count, t8.val_prm as people_living
FROM(SELECT 1 as id, fio, num_ls FROM bill01_data.kvar where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t1
LEFT JOIN(
SELECT u.ulica || ', дом № ' || d.ndom || ', кв. ' || k.nkvar || ' ком. ' || k.nkvar_n as address, 1 as id
FROM bill01_data.kvar k 
INNER JOIN bill01_data.dom d on d.nzp_dom = k.nzp_dom 
INNER JOIN bill01_data.s_ulica u on u.nzp_ul = d.nzp_ul 
INNER JOIN bill01_data.s_rajon r on r.nzp_raj = u.nzp_raj where nzp_kvar = (SELECT nzp_kvar from t_temp_kvar)) t2 on t2.id = t1.id
LEFT JOIN(
SELECT CASE WHEN val_prm = '1' THEN 'ПРИВАТИЗИРОВАНА' ELSE 'НЕПРИВАТИЗИРОВАНА' END as privatized, 1 as id 
FROM bill01_data.prm_1 where nzp_prm = 2009 and nzp = (SELECT nzp_kvar from t_temp_kvar) and is_actual = 1) t3 on t3.id = t1.id
LEFT JOIN(
SELECT val_prm, 1 as id   FROM bill01_data.prm_1 where nzp_prm = 4 and nzp = (SELECT nzp_kvar from t_temp_kvar)) t4 on t4.id = t1.id
LEFT JOIN(
SELECT val_prm, 1 as id   FROM bill01_data.prm_1 where nzp_prm = 6 and nzp = (SELECT nzp_kvar from t_temp_kvar)) t6 on t6.id = t1.id
LEFT JOIN(
SELECT val_prm , 1 as id  FROM bill01_data.prm_1  where nzp_prm = 1010270 and nzp = (SELECT nzp_kvar from t_temp_kvar)) t7 on t7.id = t1.id
LEFT JOIN(
SELECT val_prm , 1 as id  FROM bill01_data.prm_1 where nzp_prm = 5 and nzp = (SELECT nzp_kvar from t_temp_kvar)) t8 on t8.id = t1.id";

            sql = @"UPDATE t_svod set reval = 0, sum_tarif_total = 0, sum_tarif_500 = 0, sum_tarif_7 = 0, tarif_7 = 0, sum_tarif_9 = 0, tarif_9 = 0, sum_tarif_6 = 0, tarif_6 = 0, sum_tarif_15 = 0, 
                            tarif_15 = 0, sum_tarif_8 = 0, tarif_8 = 0, sum_tarif_17 = 0, tarif_17 = 0, sum_tarif_2 = 0, tarif_2 = 0, 
sum_tarif_22 = 0, tarif_22 = 0, sum_tarif_14 = 0, tarif_14 = 0, sum_tarif_25 = 0, tarif_25 = 0, sum_tarif_100018 = 0, tarif_100018 = 0, sum_tarif_100019 = 0, tarif_100019 = 0, 
sum_tarif_26 = 0, tarif_26 = 0, sum_tarif_210 = 0, tarif_210 = 0, sum_tarif_100020 = 0, tarif_100020 = 0, sum_tarif_100021 = 0, tarif_100021 = 0, reval_peni = 0
where month = " + MonthTo + " AND year = " + YearTo;
            ExecSQL(sql.ToString());
            if (YearFrom == YearTo)
            {
                for (int i = MonthFrom; i <= MonthTo; i++)
                {
                    if (i != MonthFrom)
                    {
                        sql = @"update t_svod set dolg_sum = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) + 
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) - coalesce(reval_peni, 0) + coalesce(sum_money_peni, 0) + 
                                    coalesce((SELECT coalesce(dolg_sum, 0) FROM t_svod where month = " + (i - 1) + @"), 0),  
                                    dolg_sum_peni = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) +
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) + coalesce(sum_tarif_500, 0) + 
                                    coalesce((SELECT coalesce(dolg_sum_peni, 0) FROM t_svod where month = " + (i - 1) + @"), 0)   
                                    where month = " + i;
                        ExecSQL(sql.ToString());
                    }
                    else
                    {
                        sql = @"update t_svod set dolg_sum = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) + 
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) - coalesce(reval_peni, 0) + coalesce(sum_money_peni, 0), 
                                    dolg_sum_peni = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) +
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) + coalesce(sum_tarif_500, 0)   
                                    where month = " + i;
                        ExecSQL(sql.ToString());
                    }
                }
            }
            else
            {
                for (int y = YearFrom; y <= YearTo; y++)
                {
                    if (y == YearFrom)
                    {
                        for (int i = MonthFrom; i <= 12; i++)
                        {
                            if (i != MonthFrom)
                            {
                                sql = @"update t_svod set dolg_sum = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) + 
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) - coalesce(reval_peni, 0) + coalesce(sum_money_peni, 0) + 
                                    coalesce((SELECT coalesce(dolg_sum, 0) FROM t_svod where year = " + y + " AND month = " + (i - 1) + @"), 0),  
                                    dolg_sum_peni = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) +
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) + coalesce(sum_tarif_500, 0) + 
                                    coalesce((SELECT coalesce(dolg_sum_peni, 0) FROM t_svod where year = " + y + " and month = " + (i - 1) + @"), 0)   
                                    where month = " + i + " AND year = " + y;
                                ExecSQL(sql.ToString());
                            }
                            else
                            {
                                sql = @"update t_svod set dolg_sum = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) + 
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) - coalesce(reval_peni, 0) + coalesce(sum_money_peni, 0), 
                                    dolg_sum_peni = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                    coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) +
                                    coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                    coalesce(reval, 0) - coalesce(sum_money, 0) + coalesce(sum_tarif_500, 0)   
                                    where month = " + i + " AND year = " + y;
                                ExecSQL(sql.ToString());
                            }
                        }
                    }
                    else if (y == YearTo)
                    {
                        for (int i = 1; i <= MonthTo; i++)
                        {
                            int t = i - 1;
                            int s = y;
                            if (i == 1)
                            {
                                t = 12;
                                s = s - 1;
                            }
                            sql = @"update t_svod set dolg_sum = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) + 
                                coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                coalesce(reval, 0) - coalesce(sum_money, 0) - coalesce(reval_peni, 0) + coalesce(sum_money_peni, 0) + 
                                coalesce((SELECT coalesce(dolg_sum, 0) FROM t_svod where year = " + s + " and month = " + t + @"), 0),  
                                dolg_sum_peni = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) + 
                                coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                coalesce(reval, 0) - coalesce(sum_money, 0) + coalesce(sum_tarif_500, 0) + 
                                coalesce((SELECT coalesce(dolg_sum_peni, 0) FROM t_svod where year = " + s + " and month = " + t + @"), 0)   
                                where month = " + i + " AND year = " + y;
                            ExecSQL(sql.ToString());
                           
                        }
                    }
                    else
                    {
                        for (int i = 1; i <= 12; i++)
                        {
                            int t = i - 1;
                            int s = y;
                            if (i == 1)
                            {
                                t = 12;
                                s = s - 1;
                            }
                            sql = @"update t_svod set dolg_sum = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) + 
                                coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                coalesce(reval, 0) - coalesce(sum_money, 0) - coalesce(reval_peni, 0) + coalesce(sum_money_peni, 0) + 
                                coalesce((SELECT coalesce(dolg_sum, 0) FROM t_svod where year = " + s + " and month = " + t + @"), 0),  
                                dolg_sum_peni = coalesce(sum_tarif_7, 0) + coalesce(sum_tarif_9, 0) + coalesce(sum_tarif_6, 0) + coalesce(sum_tarif_15, 0) + coalesce(sum_tarif_8, 0) + coalesce(sum_tarif_17, 0) +
                                coalesce(sum_tarif_2, 0) + coalesce(sum_tarif_22, 0) + coalesce(sum_tarif_14, 0) + coalesce(sum_tarif_25, 0) + coalesce(sum_tarif_100018, 0) + coalesce(sum_tarif_100019, 0) +
                                coalesce(sum_tarif_26, 0) + coalesce(sum_tarif_210, 0) + coalesce(sum_tarif_100020, 0) + coalesce(sum_tarif_100021, 0) +
                                coalesce(reval, 0) - coalesce(sum_money, 0) + coalesce(sum_tarif_500, 0) + 
                                coalesce((SELECT coalesce(dolg_sum_peni, 0) FROM t_svod where year = " + s + " and month = " + t + @"), 0)   
                                where month = " + i + " AND year = " + y;
                            ExecSQL(sql.ToString());

                        }
                    }
                }
                
            }
            sql = @"UPDATE t_svod set dolg_sum = 0, dolg_sum_peni = 0 where month = " + MonthTo + " AND year = " + YearTo;
            ExecSQL(sql.ToString());

            var dt2 = ExecSQLToTable(sql3.ToString());
            dt2.TableName = "Q_master2";
            var ds = new DataSet();
            ds.Tables.Add(dt2);

            string sql2 = "SELECT * FROM t_svod";

            var dt = ExecSQLToTable(sql2.ToString());
            dt.TableName = "Q_master";
            ds.Tables.Add(dt);

            int month = MonthTo == 1 ? 12 : MonthTo - 1;
            int year = MonthTo == 1 ? YearTo - 1 : YearTo;

            sql2 = "SELECT coalesce(sum_tarif_500, 0) as t_sum_tarif_500 FROM t_svod where month = " + month + " AND year = " + year;

            var dt3 = ExecSQLToTable(sql2.ToString());
            dt3.TableName = "Q_master3";
            ds.Tables.Add(dt3);

            return ds;
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
            ExecSQL("create temp table t_svod (month INTEGER default 0, year INTEGER default 0, date_ CHARACTER(30), sum_tarif_total NUMERIC(14,2), sum_tarif_500 NUMERIC(14,2)," +
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
                "dat_prih CHARACTER(10), " +
                "reval NUMERIC(14,2) default 0," +
                "dolg_sum NUMERIC(14,2) default 0, " +
                "dolg_sum_peni NUMERIC(14,2) default 0, " +
                "sum_money_peni NUMERIC(14,2) default 0,  " +
                "reval_peni NUMERIC(14,2) default 0, " +
                "sum_tarif_100019 NUMERIC(14,2) default 0, " +
                "tarif_100019 NUMERIC(14,2) default 0, " +
                "sum_tarif_6 NUMERIC(14,2) default 0, " +
                "tarif_6 NUMERIC(14,2) default 0, " +
                "sum_tarif_26 NUMERIC(14,2) default 0, " +
                "tarif_26 NUMERIC(14,2) default 0, " +
                "sum_tarif_210 NUMERIC(14,2) default 0, " +
                "tarif_210 NUMERIC(14,2) default 0, " +
                "sum_tarif_100020 NUMERIC(14,2) default 0, " +
                "tarif_100020 NUMERIC(14,2) default 0, " +
                "sum_tarif_100021 NUMERIC(14,2) default 0, " +
                "tarif_100021 NUMERIC(14,2) default 0)");

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
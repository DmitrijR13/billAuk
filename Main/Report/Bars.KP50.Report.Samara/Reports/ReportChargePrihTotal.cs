﻿using System;
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
    class ReportChargePrihTotal : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Отчет по начислению и оплате(ЛС)"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Формирование отчета по начислению и оплате(ЛС)"; }
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
            get { return Resources.ReportChargePrihTotal; }
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
                new AddressParameter()
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
            string finTable = pref + "_fin_" + (YearS - 2000).ToString("00");
            string dataTable = pref + "_data";

            string sql = "";
           
                sql = @"INSERT into t_temp_kvar
                SELECT k.nzp_kvar as nzp_kvar, town || ', ' || rajon || ',' || ulica || ' д.' || ndom as address, 
                town || ', ' || rajon || ',' || ulica || ' д.' || ndom || ' кв. ' || nkvar || ' ком. ' || CASE WHEN nkvar_n is null THEN '' ELSE nkvar_n END as kvar, 
                num_ls || '' as num_ls, fio
                FROM " + pref + DBManager.sDataAliasRest + "kvar k " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d on d.nzp_dom = k.nzp_dom " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica u on u.nzp_ul = d.nzp_ul " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                      "INNER JOIN " + pref + DBManager.sDataAliasRest + "s_town t on t.nzp_town = r.nzp_town " +
                      "where 1=1 " + Raions + Streets + Houses;

            ExecSQL(sql.ToString());

            String month = MonthS.ToString().PadLeft(2, '0');
            Int32 year = YearS;
            string chargeTable = localPref + "_charge_" + (year - 2000).ToString("00");
            string kernelTable = pref + "_kernel";
            sql = @"INSERT INTO t_svod_pack
                    SELECT s.service, ttk.num_ls, c.sum_insaldo, CASE WHEN c.nzp_serv in (6,9,14) THEN p1.val WHEN c.nzp_serv in (510,513,514) THEN p1.odn ELSE c.rsum_tarif END as rsum_tarif, c.reval + coalesce(p.sum_rcl, 0) as reval, 
                    c.sum_nedop as sum_nedop, CASE WHEN c.nzp_serv in (6,9,14) THEN p1.val + c.reval - c.sum_nedop + coalesce(p.sum_rcl, 0) 
                    WHEN c.nzp_serv in (510,513,514) THEN p1.odn + c.reval - c.sum_nedop + coalesce(p.sum_rcl, 0)  ELSE c.rsum_tarif + c.reval - c.sum_nedop + coalesce(p.sum_rcl, 0) END as sum_charge, 
                    coalesce(c.sum_money, 0)::numeric(14,2) as sum_prih, c.sum_outsaldo, ttk.address, ttk.kvar, coalesce(peni.sum_peni, 0) as sum_charge_peni, 0 as sum_prih_peni, c.nzp_kvar , c.nzp_serv              
                    FROM " + chargeTable + @".charge_" + month + @" c
                    INNER JOIN t_temp_kvar ttk on ttk.nzp_kvar = c.nzp_kvar
                    INNER JOIN " + kernelTable + @".services s on s.nzp_serv = c.nzp_serv
                    INNER JOIN " + kernelTable + @".supplier sup on sup.nzp_supp = c.nzp_supp
                    LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_peni) as sum_peni FROM " + chargeTable + @".peni_debt_2015_25 
                    where date_from >= '2015-" + month + @"-01' AND date_from < '2015-" + (MonthS + 1).ToString("00") + @"-01'  
                    group by 1,2) peni on peni.nzp_kvar = c.nzp_kvar and peni.nzp_serv = c.nzp_serv
                    LEFT JOIN (SELECT nzp_kvar, nzp_serv, sum(sum_rcl) as sum_rcl from " + chargeTable + @".perekidka 
	                    where month_ = " + month + @" group by 1,2) p on p.nzp_kvar = c.nzp_kvar and p.nzp_serv = c.nzp_serv
                    LEFT JOIN (SELECT sum(val) as val, sum(odn) as odn, nzp_serv, nzp_kvar FROM (SELECT max(c.tarif) * max(c.valm) as val, max(c.tarif) * max(c.dop87) as odn, 
                    c.nzp_serv, c.nzp_kvar FROM " + chargeTable + @".calc_gku_" + month + @" c where c.dat_charge is null and stek = 3 
	                group by 3, 4
	                UNION ALL 
	                SELECT 0 as val, max(c.tarif) * max(c.dop87) as odn, CASE WHEN c.nzp_serv = 6 THEN 510 WHEN c.nzp_serv = 9 THEN 513 WHEN c.nzp_serv = 14 
                    THEN 514 ELSE c.nzp_serv END as nzp_serv, 
	                c.nzp_kvar FROM " + chargeTable + @".calc_gku_" + month + @" c where c.dat_charge is null and stek = 3 and c.nzp_serv in (6,9,14)
	                group by 3, 4) t group by 3,4)
	                 p1 on p1.nzp_kvar = c.nzp_kvar and p1.nzp_serv = c.nzp_serv
                    where c.nzp_kvar in (SELECT nzp_kvar from t_temp_kvar) and c.nzp_serv != 1  and c.dat_charge is null";
            ExecSQL(sql.ToString());

            if (MonthS == 10)
            {
                ExecSQL("UPDATE t_svod_pack SET sum_charge_peni = 0");
            }
            ExecSQL("UPDATE t_svod_pack SET rsum_tarif = (SELECT sum(sum_charge_peni) from t_svod_pack t where t.address = t_svod_pack.address) where service = 'Пени'");
            if (MonthS == 9 && YearS == 2015)
            {
                sql = "UPDATE t_svod_pack SET sum_prih = (SELECT sum(sum_money) FROM bill01_charge_" + (YearS - 2000).ToString("00") + ".charge_" + MonthS.ToString("00")
                   + " t where t.nzp_serv = t_svod_pack.nzp_serv and t.nzp_kvar = t_svod_pack.nzp_kvar and nzp_serv != 1  and dat_charge is null)";
                ExecSQL(sql);
                sql = "UPDATE t_svod_pack SET sum_prih = sum_prih + (SELECT sum(sum_money) FROM bill01_charge_" + (YearS - 2000).ToString("00") + ".charge_" + (MonthS + 1).ToString("00")
                   + " t where t.nzp_serv = t_svod_pack.nzp_serv and t.nzp_kvar = t_svod_pack.nzp_kvar and nzp_serv != 1  and dat_charge is null)";
                ExecSQL(sql);
            }
            else
            {
                sql = "UPDATE t_svod_pack SET sum_prih = (SELECT sum(sum_money) FROM bill01_charge_" + (YearS - 2000).ToString("00") + ".charge_" + (MonthS + 1).ToString("00")
                    + " t where t.nzp_serv = t_svod_pack.nzp_serv and t.nzp_kvar = t_svod_pack.nzp_kvar and nzp_serv != 1  and dat_charge is null)";
                ExecSQL(sql);
            }
            var ds = new DataSet();
            var dt2 = ExecSQLToTable(@"SELECT address, kvar, num_ls, sum(sum_insaldo) as sum_insaldo,  sum(rsum_tarif) as rsum_tarif, 0.00 as one_time, sum(reval) as reval, sum(sum_nedop) as sum_nedop, sum(sum_charge) as sum_charge, 
sum(sum_charge_peni) as sum_charge_peni, sum(sum_prih) as sum_prih, sum(sum_prih_peni) as sum_prih_peni, sum(sum_outsaldo) as sum_outsaldo FROM (SELECT address, kvar, num_ls, service, sum(sum_insaldo) as sum_insaldo, CASE WHEN service ='Пени' THEN max(rsum_tarif) ELSE sum(rsum_tarif) END as rsum_tarif, sum(reval) as reval, sum(sum_nedop) as sum_nedop, sum(sum_charge) as sum_charge, 
sum(sum_charge_peni) as sum_charge_peni, sum(sum_prih) as sum_prih, sum(sum_prih_peni) as sum_prih_peni, sum(sum_outsaldo) as sum_outsaldo FROM t_svod_pack group by 1,2,3,4 order by 1,2,3,4) t1 group by 1,2,3 order by 1,2,3");
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
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
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
                "sum_insaldo NUMERIC(14,2) default 0," +
                "rsum_tarif NUMERIC(14,2) default 0," +
                "reval NUMERIC(14,2) default 0," +
                "sum_nedop NUMERIC(14,2) default 0," +
                "sum_charge NUMERIC(14,2) default 0," +
                "sum_prih NUMERIC(14,2) default 0," +
                "sum_outsaldo NUMERIC(14,2) default 0, " +
                "address CHARACTER(100), kvar CHARACTER(100)," +
                "sum_charge_peni NUMERIC(14,2) default 0, " +
                "sum_prih_peni NUMERIC(14,2) default 0, nzp_kvar INTEGER, nzp_serv INTEGER)");

            ExecSQL("create temp table t_temp_kvar (nzp_kvar INTEGER, address CHARACTER(100), kvar CHARACTER(100),num_ls CHARACTER(12),fio CHARACTER(100))");
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report7115 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.5 Отчет по начислениям по МКД и частному сектору"; }
        }

        public override string Description
        {
            get { return "71.1.5 Отчет по начислениям и оплатам для определенного поставщика по МКД и частному сектору"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup>(0); }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_1_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

       /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }


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
                new BankSupplierParameter()
            };
        }

        public override DataSet GetData()
        {

            #region Выборка по локальным банкам

            MyDataReader reader;

            var sql = " insert into t_nach (mkd, gil, vod_m3, vod_rub, kan_m3, kan_rub, sum_money) " +
                         " values ('Многоквартирные дома',0,0,0,0,0,0)  ";
            ExecSQL(sql);
            sql = " insert into t_nach (mkd, gil, vod_m3, vod_rub, kan_m3, kan_rub, sum_money) " +
                  " values ('Жилые дома (частный сектор)',0,0,0,0,0,0)  ";
            ExecSQL(sql);


            sql = " SELECT * " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    var chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                      "charge_" + month.ToString("00");

                    var revalTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                      "reval_" + month.ToString("00");


                    var calcTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                    "calc_gku_" + month.ToString("00");
                    if (TempTableInWebCashe(chargeTable) && TempTableInWebCashe(calcTable))
                    {

                        sql = " insert into t_mkd (nzp_kvar, nzp_dom , mkd )" +
                              " select k.nzp_kvar, d.nzp_dom, 0 as mkd " +
                              " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                              pref + DBManager.sDataAliasRest + "dom d, " +
                              pref + DBManager.sDataAliasRest + "s_ulica s " +
                              " where k.nzp_dom=d.nzp_dom and d.nzp_ul=s.nzp_ul ";
                        ExecSQL(sql);
                        //--
                        sql = "update t_mkd set mkd =1 where 0<(select count(*) " +
                              " from " + pref + DBManager.sDataAliasRest + "prm_2 p " +
                              " where nzp_prm=2030  and val_prm='1' and is_actual=1 " +
                              " and nzp=t_mkd.nzp_dom and dat_s<='" + DatPo.ToShortDateString() + "' " +
                              " and dat_po>= '" + DatS.ToShortDateString() + "') ";
                        ExecSQL(sql);

                        sql = " insert into t_nach (mkd, vod_m3, vod_rub, kan_m3, kan_rub, sum_money) " +
                              " select " +
                              " (case when mkd=1 then 'Многоквартирные дома' else 'Жилые дома (частный сектор)' end) as mkd, " +
                              " sum(case when nzp_serv=6 then ch.c_calc else 0 end) as vod_m3, " +
                              " sum(case when nzp_serv=6 then ch.sum_tarif+real_charge else 0 end) as vod_rub, " +
                              " sum(case when nzp_serv=7 then ch.c_calc else 0 end) as kan_m3, " +
                              " sum(case when nzp_serv=7 then ch.sum_tarif+real_charge else 0 end) as kan_rub, " +
                              " sum(ch.sum_money) as sum_money " +
                              " from " + chargeTable + " ch, t_mkd t " +
                              " where  nzp_serv IN (6,7) " + GetWhereSupp("ch.nzp_supp") + " and ch.nzp_kvar=t.nzp_kvar " +
                              " and dat_charge is null " +
                              " group by 1 ";
                        ExecSQL(sql);
                        if (TempTableInWebCashe(revalTable))
                        {
                            //Перерасчеты
                            sql = " insert into t_nach (mkd, vod_m3, vod_rub, kan_m3, kan_rub, sum_money) " +
                                  " select " +
                                  " (case when mkd=1 then 'Многоквартирные дома' else 'Жилые дома (частный сектор)' end) as mkd, " +
                                  " sum(case when nzp_serv=6 then ch.c_calc-c_calc_p else 0 end) as vod_m3, " +
                                  " sum(case when nzp_serv=6 then ch.reval else 0 end) as vod_rub, " +
                                  " sum(case when nzp_serv=7 then ch.c_calc-c_calc_p else 0 end) as kan_m3, " +
                                  " sum(case when nzp_serv=7 then ch.reval else 0 end) as kan_rub, " +
                                  " sum(0) " +
                                  " from " + revalTable + " ch, t_mkd t " +
                                  " where  nzp_serv IN (6,7) " + GetWhereSupp("ch.nzp_supp") +
                                  " and ch.nzp_kvar=t.nzp_kvar " +
                                  " group by 1 ";
                            ExecSQL(sql);
                        }

                        sql = " insert into t_gil (nzp_kvar, gil) " +
                              " select distinct nzp_kvar, gil " +
                              " from " + calcTable +
                              " where nzp_serv in(6,7)  " + GetWhereSupp("nzp_supp") +
                              " and stek = 3 " +
                              " and dat_charge is null ";
                        ExecSQL(sql);

                        sql = " insert into t_nach (mkd, gil) " +
                              " select  " +
                              " (case when mkd=1 then 'Многоквартирные дома' else 'Жилые дома (частный сектор)' end) as mkd, " +
                              " sum(round(c.gil)) as gil  " +
                              " from t_gil c, t_mkd t " +
                              " where c.nzp_kvar=t.nzp_kvar " +
                              " group by 1 ";
                        ExecSQL(sql);


                        ExecSQL("delete from t_mkd");
                    }
                }
            }

            #endregion

            #region Выборка на экран

            sql =
                " select mkd, sum(gil) as gil, sum(vod_m3) as vod_m3, sum(vod_rub) as vod_rub, " +
                " sum(kan_m3) as kan_m3, sum(kan_rub) as kan_rub, sum(sum_money) as sum_money " +
                " from t_nach group by 1 ";
            var dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            #endregion

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',',' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period;

            if (DatS == DatPo)
            {
                period = DatS.ToShortDateString() + " г.";
            }
            else
            {
                period = "период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
            }
            report.SetParameterValue("period", period);

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);

        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            var period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DatS = begin;
            DatPo = end;
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_nach (     " +
            " mkd character(50)," +
            " gil integer default 0," +
            " vod_m3 " + DBManager.sDecimalType + "(14,2) default 0.00, " +
            " vod_rub " + DBManager.sDecimalType + "(14,2) default 0.00, " +
            " kan_m3 " + DBManager.sDecimalType + "(14,2) default 0.00, " +
            " kan_rub " + DBManager.sDecimalType + "(14,2) default 0.00, " +
            " sum_money " + DBManager.sDecimalType + "(14,2) default 0.00 " + 
            " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " Create temp table t_mkd ( " +
            " nzp_kvar integer, " +
            " nzp_dom integer," +
            " mkd integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql); 

            sql = " Create temp table t_gil ( " +
            " nzp_kvar integer, " +
            " gil integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" drop table t_nach ", false);
                ExecSQL(" drop table t_mkd ", false);
                ExecSQL(" drop table t_gil ", false);
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Некорректное завершение отчета 71_1_5" + e.Message, MonitorLog.typelog.Warn, false);
            }
        }
    }
}

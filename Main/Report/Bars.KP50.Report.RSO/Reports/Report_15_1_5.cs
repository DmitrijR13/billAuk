using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RSO.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RSO.Reports
{
    public class Report1515 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.1.5 Отчет по начислениям по МКД и частному сектору"; }
        }

        public override string Description
        {
            get { return "15.1.5 Отчет по начислениям и оплатам для определенного поставщика по МКД и частному сектору"; }
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
            get { return Resources.Report_15_1_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }


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
                new SupplierAndBankParameter()
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

                sql = " Create temp table t_mkd ( nzp_kvar integer, " +
                      " nzp_dom integer," +
                      " mkd integer) " + DBManager.sUnlogTempTable;
                ExecSQL(sql);

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

                for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                         "charge_" + month.ToString("00");
                    string calcTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                       "calc_gku_" + month.ToString("00");

                    if (TempTableInWebCashe(chargeTable) && TempTableInWebCashe(calcTable))
                    {

                        sql = " insert into t_nach (mkd, gil, vod_m3, vod_rub, kan_m3, kan_rub, sum_money) " +
                              " select " +
                              " (case when mkd=1  then 'Многоквартирные дома' else 'Жилые дома (частный сектор)' end) as mkd, " +
                              " 0 as gil, " +
                              " sum(case when nzp_serv=6 then ch.c_calc else 0 end) as vod_m3, " +
                              " sum(case when nzp_serv=6 then ch.sum_tarif else 0 end) as vod_rub, " +
                              " sum(case when nzp_serv=7 then ch.c_calc else 0 end) as kan_m3, " +
                              " sum(case when nzp_serv=7 then ch.sum_tarif else 0 end) as kan_rub, " +
                              " sum(ch.sum_money) as sum_money " +
                              " from " + chargeTable + " ch, t_mkd t " +
                              " where  nzp_serv IN (6,7) " + GetWhereSupp() + " and ch.nzp_kvar=t.nzp_kvar " +
                              "  and dat_charge is null " +
                              " group by 1 ";
                        ExecSQL(sql);

                        sql = " insert into t_nach (mkd, gil, vod_m3, vod_rub, kan_m3, kan_rub, sum_money) " +
                              " select  " +
                              " (case when  mkd=1  then 'Многоквартирные дома' else 'Жилые дома (частный сектор)' end) as mkd, " +
                              " sum(round(c.gil)) as gil,  " +
                              " 0 as vod_m3,  " +
                              " 0 as vod_rub,  " +
                              " 0 as kan_m3,  " +
                              " 0 as kan_rub,  " +
                              " 0 as sum_money  " +
                              " from " + calcTable + " c, t_mkd t " +
                              " where nzp_serv in(6,7)  " + GetWhereSupp() + " " +
                              " and dat_charge is null and c.nzp_kvar=t.nzp_kvar " +
                              " group by 1 ";
                        ExecSQL(sql);
                    }

                }

                ExecSQL("drop table t_mkd");
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

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
        
        private string GetWhereSupp()
        {
            string result = String.Empty;
            if (Suppliers != null)
            {
                result = Suppliers.Aggregate(result, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            result = result.TrimEnd(',');


            if (!String.IsNullOrEmpty(result))
            {
                result = " AND nzp_supp in (" + result + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                var sql = " SELECT name_supp from " +
                          ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                          " WHERE nzp_supp > 0 " + result;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }
            return result;
        }

        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
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
            report.SetParameterValue("supp", SupplierHeader);
            report.SetParameterValue("raj", "");
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DatS = begin;
            DatPo = end;

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_nach (     ");
            sql.Append(" mkd character(50),");
            sql.Append(" gil integer default 0,");
            sql.Append(" vod_m3 " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" vod_rub " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" kan_m3 " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" kan_rub " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2) default 0.00 "); 
            sql.Append(" ) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_nach ", false);
            try
            {
                ExecSQL(" drop table t_mkd ", false);
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Некорректное завершение отчета 71_1_5" + e.Message, MonitorLog.typelog.Warn, false);
            }
        }
    }
}

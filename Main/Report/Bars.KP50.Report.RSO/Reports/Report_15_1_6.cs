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
    class Report1516 : BaseSqlReport
    {

        public override string Name
        {
            get { return "15.1.6 Отчет по начислениям и оплатам по ИПУ и нормативам"; }
        }

        public override string Description
        {
            get { return "15.1.6 Отчет по начислениям и оплатам для поставщиков по ИПУ и нормативам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup>(0); }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        
         public override IList<ReportKind> ReportKinds
        {
            get {return new List<ReportKind> { ReportKind.Base };}
        }

        protected override byte[] Template
        {
            get { return Resources.Report_15_1_6; }
        }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Поставщик</summary>
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
            IDataReader reader;


            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToStr().Trim();

                    for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                    {
                        var year = i/12;
                        var month = i%12;
                        if (month == 0)
                        {
                            year--;
                            month = 12;
                        }

                        string calcGkuTable = pref + "_charge_" + (year - 2000).ToString("00") +
                                              DBManager.tableDelimiter + "calc_gku_" + month.ToString("00");
                        string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                             "charge_" + month.ToString("00");

                        if (TempTableInWebCashe(calcGkuTable) && TempTableInWebCashe(chargeTable))
                        {
                            sql =
                                " INSERT INTO t_norm_imd(nzp_kvar, is_device, nzp_serv, numb_payer, sum_m_inwater, sum_r_inwater, sum_m_outwater, sum_r_outwater, sum_r_total) " +
                                " SELECT  nzp_kvar, " +
                                " is_device, nzp_serv, " +
                                " SUM(round(gil)) AS numb_payer, " +
                                " 0 AS sum_m_inwater, " +
                                " 0 AS sum_r_inwater, " +
                                " 0 AS sum_m_outwater, " +
                                " 0 AS sum_r_outwater, " +
                                " 0 AS sum_r_total " +
                                " FROM " + calcGkuTable +
                                " WHERE nzp_serv in(6,7) AND dat_charge is null " + GetWhereSupp() +
                                " GROUP BY nzp_kvar, is_device, nzp_serv ";
                            ExecSQL(sql);

                            sql =
                                " INSERT INTO t_norm_imd(nzp_kvar, is_device, nzp_serv, numb_payer, sum_m_inwater, sum_r_inwater, sum_m_outwater, sum_r_outwater, sum_r_total) " +
                                " SELECT nzp_kvar, " +
                                " is_device, nzp_serv, " +
                                " 0 AS numb_payer, " +
                                " SUM(CASE WHEN nzp_serv=6 THEN c_calc ELSE 0 END) AS sum_m_inwater, " +
                                " SUM(CASE WHEN nzp_serv=6 THEN sum_tarif ELSE 0 END) AS sum_r_inwater, " +
                                " SUM(CASE WHEN nzp_serv=7 THEN c_calc ELSE 0 END) AS sum_m_outwater, " +
                                " SUM(CASE WHEN nzp_serv=7 THEN sum_tarif ELSE 0 END) AS sum_r_outwater, " + 
                                " SUM(sum_money) AS sum_r_total " +
                                " FROM " + chargeTable +
                                " WHERE nzp_serv IN (6,7) AND dat_charge is null " + GetWhereSupp() +
                                " GROUP BY nzp_kvar, is_device, nzp_serv ";
                            ExecSQL(sql);
                        }

                    }
                }
            }

            reader.Close();
            #endregion

            sql = " UPDATE  t_norm_imd " +
                  " SET is_device = 1 " +
                  " WHERE is_device>0 ";
            ExecSQL(sql);
            // сбрасываются начисления за канализацию, там где есть водопроводный счетчик
            sql = " UPDATE  t_norm_imd " +
                  " set is_device = 1 " +
                  " where nzp_kvar in  " +
                  " (select nzp_kvar from t_norm_imd where is_device = 1 and nzp_serv = 6)   " +
                  " and nzp_serv = 7 ; ";
            ExecSQL(sql);

            sql = " SELECT is_device AS type_device, " +
                         " SUM(numb_payer) AS numb_payer, " +
                         " SUM(sum_m_inwater) AS sum_m_inwater, " +
                         " SUM(sum_r_inwater) AS sum_r_inwater, " +
                         " SUM(sum_m_outwater) AS sum_m_outwater, " +
                         " SUM(sum_r_outwater) AS sum_r_outwater, " +
                         " SUM(sum_r_total) AS sum_r_total " +
                  " FROM t_norm_imd " +
                  " GROUP BY is_device ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
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
            report.SetParameterValue("supplier", SupplierHeader);
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
                
            report.SetParameterValue("rajon", "");
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
            sql.Append(" CREATE TEMP TABLE t_norm_imd ( ");
            sql.Append(" nzp_kvar INTEGER, ");//*
            sql.Append(" nzp_serv INTEGER, ");
            sql.Append(" is_device INTEGER, ");
            sql.Append(" numb_payer INTEGER, ");
            sql.Append(" sum_m_inwater " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_r_inwater " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_m_outwater " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_r_outwater " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_r_total " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable);

            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_norm_imd ", true);
        }
    }
}

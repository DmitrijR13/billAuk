using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using FastReport.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.RSO.Properties;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report1536 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.3.6 Сводная ведомасть сборов"; }
        }

        public override string Description
        {
            get { return "Сводная ведомасть сборов"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_15_3_6; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary> Период - с </summary>
        protected DateTime DatS { get; set; }

        /// <summary> Период -  по </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        /// <summary> Количество организаций (средства РЦ) </summary>
        protected Int32 CountPayers10 { get; set; }

        /// <summary> Количество организаций (оплаты ПУ и УК) </summary>
        protected Int32 CountPayers20 { get; set; }


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

        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period = DatS == DatPo
                ? "за " + DatS.ToShortDateString()
                : "c " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString();
            report.SetParameterValue("period", period);

            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());

            report.SetParameterValue("count_payers10", CountPayers10);
            report.SetParameterValue("count_payers20", CountPayers20);
        }

        public override DataSet GetData()
        {
            string sql;
            string whereWp = GetWhereWp();
            string prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest;
            for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
            {
                var year = i/12;
                var month = i%12;
                if (month == 0)
                {
                    year--;
                }

                string finPack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                 DBManager.tableDelimiter + "pack ";
                string finPackLs = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                   DBManager.tableDelimiter + "pack_ls ";
                if (TempTableInWebCashe(finPackLs) && TempTableInWebCashe(finPack))
                {
                    sql = " INSERT INTO tt_report_15_3_6 ( nzp_pack )" +
                          " SELECT DISTINCT nzp_pack " +
                          " FROM " + finPackLs + " pl INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                          "kvar k ON k.num_ls = pl.num_ls " + whereWp;
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_15_3_6(dat_pack, pack_type, nzp_bank, sum_pack) " +
                          " SELECT dat_pack, pack_type, nzp_bank, sum_pack " +
                          " FROM " + finPack + " p INNER JOIN tt_report_15_3_6 t ON t.nzp_pack = p.nzp_pack " +
                          " WHERE pack_type IN ( 10,20 ) " +
                          " AND dat_pack >= '" + DatS.ToShortDateString() + "' " +
                          " AND dat_pack <= '" + DatPo.ToShortDateString() + "' ";
                    ExecSQL(sql);
                }
            }

            sql = " UPDATE t_report_15_3_6 " +
                  " SET payer = " + DBManager.sNvlWord +"( ( SELECT payer " +
                                                           " FROM " + prefKernel + "s_bank b INNER JOIN " + prefKernel + "s_payer p ON p.nzp_payer = b.nzp_payer " +
                                                           " WHERE b.nzp_bank = t_report_15_3_6.nzp_bank ),'Неопределённая организация' )";
            ExecSQL(sql);

            sql = " SELECT dat_pack, TRIM(payer) AS payer, SUM(sum_pack) AS sum_pack" +
                  " FROM t_report_15_3_6 " +
                  " WHERE pack_type = 10 " +
                  " GROUP BY 1,2 " +
                  " ORDER BY 1,2 "; 

            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";
            sql = " SELECT dat_pack, TRIM(payer) AS payer, SUM(sum_pack) AS sum_pack" +
                  " FROM t_report_15_3_6 " +
                  " WHERE pack_type = 20 " +
                  " GROUP BY 1,2 " +
                  " ORDER BY 1,2 ";

            

            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";
            var payers = new List<string>();

            foreach (DataRow row in dt1.Rows)
            {
                if (!payers.Contains(row["payer"].ToString()) && !row["payer"].IsNull())
                {
                    payers.Add(row["payer"].ToString());
                }
            }

            CountPayers10 = payers.Count;

            payers.Clear();

            foreach (DataRow row in dt2.Rows)
            {
                if (!payers.Contains(row["payer"].ToString()) && !row["payer"].IsNull())
                {
                    payers.Add(row["payer"].ToString());
                }
            }

            CountPayers20 = payers.Count;

            var ds = new DataSet();
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
           
            return ds;
        }

        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            whereWp = Banks != null
                ? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " WHERE nzp_wp IN (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_15_3_6(" +
                               " dat_pack DATE, " +
                               " pack_type SMALLINT, " +
                               " nzp_bank INTEGER, " +
                               " payer CHARACTER(40), " +
                               " sum_pack " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE tt_report_15_3_6( " +
                  " nzp_pack INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_15_3_6 ");
            ExecSQL(" DROP TABLE tt_report_15_3_6 ");
        }
    }
}

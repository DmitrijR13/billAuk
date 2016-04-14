using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Utils;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Bars.KP50.Report.RSO.Properties;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report1511 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.1.1 Сводный отчет по поступившим пачкам оплат в разрезе организаций осуществляющих прием платежей"; }
        }

        public override string Description
        {
            get { return "15.1.1 Сводный отчет по поступившим пачкам оплат в разрезе организаций осуществляющих прием платежей"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_15_1_1; }
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

        /// <summary> Тип пачки </summary>
        protected Byte TypePack { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }


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
                new SupplierAndBankParameter(),
                new ComboBoxParameter
                {
                    Name = "Тип пачки",
                    Code = "TypePack",
                    Value = 10,
                    MultiSelect = false,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new {Id = 10, Name = "средства РЦ"},
                        new {Id = 20, Name = "оплаты ПУ и УК"}
                    }
                }
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

            TypePack = UserParamValues["TypePack"].GetValue<Byte>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
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

            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
        }

        public override DataSet GetData()
        {
            string centralFin = ReportParams.Pref + "_fin_" + (DateTime.Now.Year - 2000).ToString("00") + DBManager.tableDelimiter + "pack ";
            string prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest + "supplier ";
            string sql;
            if (TempTableInWebCashe(centralFin))
            {

                sql = " INSERT INTO t_svod_otch_post(nzp_bank, num_pack, pack_type, dat_pack, file_name, sum_pack, name_supp)" + 
                             " SELECT nzp_bank, " +
                                    " num_pack, " +
                                    " pack_type, " +
                                    " dat_pack, " +
                                    " file_name, " +
                                    " sum_pack, " +
                                    " name_supp " +
                             " FROM " + centralFin + " f LEFT OUTER JOIN " + prefKernel + " s ON f.nzp_supp=s.nzp_supp " +
                             " WHERE dat_pack IS NOT NULL " +
                               " AND (nzp_pack<>par_pack or par_pack is null) " +
                               " AND dat_pack>='" + DatS.ToShortDateString() + "' " +
                               " AND dat_pack<='" + DatPo.ToShortDateString() + "' " +
                               " AND pack_type=" + TypePack +
                                 GetWhereSupp("f.");
                ExecSQL(sql);
            }

            sql = " SELECT distinct TRIM(bank) AS bank, " +
                         " num_pack, payer, dat_pack, file_name, sum_pack, pack_type, (CASE WHEN pack_type=20 THEN name_supp ELSE '' END) AS name_supp " +
                  " FROM t_svod_otch_post a INNER JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank b ON (a.nzp_bank=b.nzp_bank) " +
                                          " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest +"s_payer p ON b.nzp_payer=p.nzp_payer " +
                  " ORDER BY 1,2 ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        private string GetWhereSupp(string table)
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

            result = !String.IsNullOrEmpty(result) ? " AND " + table + "nzp_supp IN (" + result + ") " : String.Empty;
            return result;
        }


        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_svod_otch_post( " +
                               " nzp_bank INTEGER, " +
                               " num_pack CHARACTER(10), " +
                               " dat_pack DATE, " +
                               " pack_type SMALLINT, " +
                               " file_name CHARACTER(200), " +
                               " sum_pack " + DBManager.sDecimalType + "(14,2), " +
                               " name_supp CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_svod_otch_post");
        }
    }
}

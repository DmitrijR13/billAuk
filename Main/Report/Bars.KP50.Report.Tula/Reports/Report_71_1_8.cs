using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;


namespace Bars.KP50.Report.Tula.Reports
{
    class Report7118 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.8 Анализ потребления коммунальных услуг"; }
        }

        public override string Description
        {
            get { return "Анализ потребления коммунальных услуг"; }
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_1_8; }
        }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServiceHeader { get; set; }

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
                new BankSupplierParameter(),
                new ServiceParameter()
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

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

            Services = UserParamValues["Services"].GetValue<List<int>>();

        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period = DatS == DatPo ? "за " + DatS.ToShortDateString() + " г." :
            "за период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
            report.SetParameterValue("period", period);

            report.SetParameterValue("date",DateTime.Now.ToShortDateString());
            report.SetParameterValue("time",DateTime.Now.ToShortTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            string whereSupp = GetWhereSupp("c.nzp_supp"),
                    whereServ = GetWhereServ();

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetWhereWp();
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                var pref = reader["pref"].ToString().ToLower().Trim();

                for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
                {
                    var year = i / 12;
                    var month = i % 12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }
                    string calcGku = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + month.ToString("00");
                    string prefData = pref + DBManager.sDataAliasRest;

                    if (TempTableInWebCashe(calcGku))
                    {
                        sql = " INSERT INTO t_report_71_1_8 (num_ls, tarif, rash_norm_one, nzp_serv, is_device, valm) " +
                              " SELECT DISTINCT num_ls, tarif,  rash_norm_one, nzp_serv, is_device, (CASE WHEN is_device > 0 THEN valm END) AS valm " +
                              " FROM " + calcGku + " c INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                              " WHERE c.dat_charge IS NULL AND c.stek = 3 " + whereServ + whereSupp;
                        ExecSQL(sql);
                    }
                }
            }
            reader.Close();

            sql = " UPDATE t_report_71_1_8" +
                  " SET service = (SELECT service" +
                                 " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services s " +
                                 " WHERE s.nzp_serv = t_report_71_1_8.nzp_serv) ";
            ExecSQL(sql);

            sql = " SELECT DISTINCT num_ls, tarif, rash_norm_one, TRIM(service) AS service, (CASE WHEN is_device > 0 THEN 'есть' ELSE 'нет' END) AS is_device, valm " +
                  " FROM t_report_71_1_8 " +
                  " ORDER BY 1 ";

            DataTable tempTable = ExecSQLToTable(sql);
            tempTable.TableName = "Q_master";

            if (tempTable.Rows.Count > 70000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = tempTable.Rows.Cast<DataRow>().Skip(70000).ToArray();
                dtr.ForEach(tempTable.Rows.Remove);
            }

            if (tempTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = tempTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(tempTable.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(tempTable);

            return  ds;
        }

        private string GetWhereWp()
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

        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
                whereServ = whereServ.TrimEnd(',');
            }
            if (String.IsNullOrEmpty(whereServ))
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);


            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');

            }
            return whereServ;
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

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_report_71_1_8 ( " +
                               " num_ls INTEGER, " +
                               " tarif " + DBManager.sDecimalType + "(14,4), " +
                               " rash_norm_one " + DBManager.sDecimalType + "(14,7), " +
                               " nzp_serv INTEGER, " +
                               " service CHARACTER(100), " +
                               " is_device INTEGER, " +
                               " valm " + DBManager.sDecimalType + "(15,7)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_71_1_8 ");
        }
    }
}

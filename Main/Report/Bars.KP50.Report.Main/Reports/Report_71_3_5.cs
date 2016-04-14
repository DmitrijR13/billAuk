using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Main.Properties;
using  Bars.KP50.Utils;

namespace Bars.KP50.Report.Main.Reports
{
    class Report7135 : BaseSqlReport
    {
        public override string Name
        {
            get { return "Базовый - Сводный отчет по распределениям по опердням"; }
        }

        public override string Description
        {
            get { return "Сводный отчет по распределениям по опердням"; }
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
            get { return Resources.Report_71_3_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary> Период - с </summary>
        protected DateTime DatS { get; set; }

        /// <summary> Период -  по </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }
      
        /// <summary> Тип пачки </summary>
        protected Byte TypePack { get; set; }

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

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period = DatS == DatPo
                ? "за " + DatS.ToShortDateString()
                : "c " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString();
            report.SetParameterValue("period", period);

            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += "Тип пачки: " + (TypePack == 10 ? "средства РЦ" : "оплаты ПУ и УК");
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        public override DataSet GetData()
        {
            string whereWp = GetWhereWp(),
                    whereSupp = GetWhereSupp("pl.nzp_supp");
            string sql;

            for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
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
                string finGilSums = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                   DBManager.tableDelimiter + "gil_sums ";


                if (TempTableInWebCashe(finPack) && TempTableInWebCashe(finPackLs))
                {
                    sql = " INSERT INTO t_report_71_3_5 (nzp_pack, nzp_pack_ls, num_pack, dat_uchet, dat_uchet_p, dat_pack, sum_pack, " +
                                                            " count_kv,pack_type,file_name,g_sum_ls,dat_vvod,pkod, nzp_bank, info_num, kod_sum ) " +
                                 " SELECT pl.nzp_pack, " +
                                        " nzp_pack_ls, " +
                                        " p.num_pack, " +
                                        " pl.dat_uchet, " +
                                        " p.dat_uchet AS dat_uchet_p, " +
                                        " p.dat_pack, " +
                                        " p.sum_pack, " +
                                        " p.count_kv, " +
                                        " p.pack_type, " +
                                        " p.file_name, " +
                                        " pl.g_sum_ls, " +
                                        " pl.dat_vvod, " +
                                        " pl.pkod," +
                                        " p.nzp_bank," +
                                        " pl.info_num, " +
                                        " pl.kod_sum " +
                                 " FROM " + finPackLs + " pl INNER JOIN " + finPack + " p ON p.nzp_pack = pl.nzp_pack " +
                                 " WHERE pl.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                                   " AND pl.dat_uchet <= '" + DatPo.ToShortDateString() + "' " +
                                   " AND pack_type = " + TypePack +
                                   " AND num_ls IN (SELECT DISTINCT num_ls" +
                                                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " + whereWp + ") " + whereSupp ;
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_3_5 " +
                          " SET sum_oplat = (SELECT SUM(sum_oplat) " +
                                           " FROM " + finGilSums + " g " +
                                           " WHERE g.nzp_pack_ls = t_report_71_3_5.nzp_pack_ls) ";
                    ExecSQL(sql);
                
                }
            }

            sql = " UPDATE t_report_71_3_5 " +
                  " SET bank = " + DBManager.sNvlWord + "((SELECT payer " +
                                                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank b LEFT OUTER JOIN " +
                                                                    ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p ON p.nzp_payer = b.nzp_payer " +
                                                         " WHERE b.nzp_bank = t_report_71_3_5.nzp_bank),'') ";
            ExecSQL(sql);



            sql = " INSERT INTO ta_report_71_3_5 (nzp_pack_ls, dat_uchet, dat_uchet_p, g_sum_ls) " +
                  " SELECT nzp_pack_ls, dat_uchet, dat_uchet_p, MAX(g_sum_ls) AS g_sum_ls" +
                  " FROM t_report_71_3_5 " +
                  " GROUP BY 1,2,3 ";
            ExecSQL(sql);

            sql = " SELECT dat_uchet, dat_uchet_p, SUM(g_sum_ls) AS g_sum_ls, COUNT(nzp_pack_ls) AS count_kv " +
                  " FROM ta_report_71_3_5 " +
                  " GROUP BY 1,2 " +
                  " ORDER BY 1 DESC, 2 DESC ";
            DataTable temp1Table = ExecSQLToTable(sql);
            temp1Table.TableName = "Q_master1";

            string cur_schema = ReportParams.Pref + DBManager.sDataAliasRest;
#if PG
            cur_schema = "public.";
#endif
            sql = " SELECT nzp_pack, " + cur_schema +
                  "sortnum(num_pack) as ord_pack,  " +
                  "dat_pack, num_pack, dat_uchet_p AS dat_uchet, " +
                  " SUM(g_sum_ls) AS sum_pack, COUNT(nzp_pack_ls) AS count_kv, " +
                  " bank, " + (TypePack == 10 ? "'средства РЦ'" : "'оплаты ПУ и УК' ") + " AS pack_type, file_name " +
                  " FROM t_report_71_3_5 GROUP BY 1,2,3,4,5,8,9,10  ORDER BY 3,2,1";
            DataTable temp2Table = ExecSQLToTable(sql);
            temp2Table.TableName = "Q_master2";

            sql = " SELECT DISTINCT nzp_pack, pkod, dat_vvod, dat_uchet, sum_oplat, g_sum_ls, info_num, kod_sum  " +
                  " FROM t_report_71_3_5 ORDER BY 2 ";
            DataTable temp3Table = ExecSQLToTable(sql);
            temp3Table.TableName = "Q_master3";
            #region ограничение записей
            if (temp1Table.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = temp1Table.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(temp1Table.Rows.Remove);
            }
            if (temp2Table.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = temp2Table.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(temp2Table.Rows.Remove);
            }
            if (temp3Table.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = temp3Table.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(temp3Table.Rows.Remove);
            }
            #endregion

            var ds = new DataSet();
            ds.Tables.Add(temp1Table);
            ds.Tables.Add(temp2Table);
            ds.Tables.Add(temp3Table);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по банкам
        /// </summary>
        /// <returns></returns>
        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null)
                whereWp = BankSupplier.Banks != null
                ? BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " WHERE nzp_wp IN (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
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
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            if (!String.IsNullOrEmpty(whereSupp))
            {
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }

            if (String.IsNullOrEmpty(whereSupp)) return "";
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_71_3_5( " +
                               " nzp_pack INTEGER, " +
                               " nzp_pack_ls INTEGER, " +
                               " num_pack CHARACTER(10), " +
                               " dat_uchet DATE, " +
                               " dat_uchet_p DATE, " +
                               " dat_pack DATE, " +
                               " sum_pack " + DBManager.sDecimalType + "(14,2), " +
                               " count_kv INTEGER, " +
                               " nzp_bank INTEGER, " +
                               " bank CHARACTER(40), " +
                               " pack_type SMALLINT, " +
                               " file_name CHARACTER(200), " +
                               " g_sum_ls " + DBManager.sDecimalType + "(14,2), " +
                               " sum_oplat " + DBManager.sDecimalType + "(10,2), " +
                               " dat_vvod DATE, " +
                               " info_num INTEGER, " +
                               " kod_sum SMALLINT, " +
                               " pkod " + DBManager.sDecimalType + "(13,0)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE ta_report_71_3_5(" +
                  " nzp_pack_ls INTEGER, " +
                  " dat_uchet DATE, " +
                  " dat_uchet_p DATE, " +
                  " g_sum_ls " + DBManager.sDecimalType + "(14,2), " +
                  " count_kv INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_71_3_5 ");
            ExecSQL(" DROP TABLE ta_report_71_3_5 ");
        }
    }
}

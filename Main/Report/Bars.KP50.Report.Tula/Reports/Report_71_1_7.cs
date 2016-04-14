using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Utils;
using Bars.KP50.Report.Base;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report7117 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.7 Еженедельный отчет по платежам для Сбербанка"; }
        }

        public override string Description
        {
            get { return "Еженедельный отчет по платежам для Сбербанка"; }
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
            get { return Resources.Report_71_1_7; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary> Список идентификаторов районов </summary>
        private List<int> Rajon { get; set; }

        /// <summary> Список районов </summary>
        private string RajonHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }
     
        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary> Должность паспортистки </summary>
        private string PostDirector { get; set; }

        /// <summary> Должность паспортистки </summary>
        private string NameDirector { get; set; }

        /// <summary> Должность паспортистки </summary>
        private string PostPasport { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        private int PeriodParam { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new PeriodParameter(DateTime.Now.AddDays(-7), DateTime.Now),
                new ComboBoxParameter
                {
                   Code = "periodParam",
                   Name = "Тип периода",
                   Value = 1,
                   StoreData = new List<object>
                   {
                        new { Id = 1, Name = "по дате оплаты" },
                        new { Id = 2, Name = "по операционному дню" }
                   }
                },
                new SupplierAndBankParameter(),
                new RaionsParameter()
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

            Rajon = UserParamValues["Raions"].GetValue<List<int>>();
            PeriodParam = UserParamValues["periodParam"].GetValue<int>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period = DatS == DatPo ? "за " + DatS.ToShortDateString() + " г." :
                        "за период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
            report.SetParameterValue("period", period);
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("post_director", PostDirector ?? "");
            report.SetParameterValue("name_director", "/" + STCLINE.KP50.Global.Utils.GetCorrectFIO(NameDirector ?? "") + "/" );
            report.SetParameterValue("post_passport", PostPasport ?? "");
            report.SetParameterValue("name_passport", "/" + STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname) + "/");
            report.SetParameterValue("period_param", PeriodParam == 1 ? "по дате оплаты" : "по операционному дню");

            string headerParam = !string.IsNullOrEmpty(RajonHeader) ? "Район: " + RajonHeader : string.Empty;
            if (string.IsNullOrEmpty(headerParam))
            headerParam =  !string.IsNullOrEmpty(TerritoryHeader)
                    ? "Территория: " + TerritoryHeader
                    : string.Empty; 
            report.SetParameterValue("headerParam", headerParam);

        }

        public override DataSet GetData()
        {
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;
            var comment = new Dictionary<string, string>();

            string datParam = PeriodParam == 1 ? "dat_vvod" : "dat_uchet"; 

            string sql = " INSERT INTO num_ls_71_1_7( num_ls, nzp_town, town  )" +
                         " SELECT DISTINCT num_ls, r.nzp_town, TRIM(town) AS town  " +     // (CASE WHEN rajon='-' THEN town ELSE TRIM(town)||','||trim(rajon) END)
                         " FROM " + prefData + "kvar k INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                     " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                     " INNER JOIN " + prefData + "s_rajon r ON (r.nzp_raj = u.nzp_raj " + GetRajon() + ")" +
                                                     " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " + GetWhereWp();
            ExecSQL(sql);

            ExecSQL(DBManager.sUpdStat + " num_ls_71_1_7 ");

            for (int year = DatS.Year; year <= DatPo.Year; year++)
            {

                string prefFinXX = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") + DBManager.tableDelimiter;
                string fromFinNumLs = prefFinXX + "pack_ls pl INNER JOIN num_ls_71_1_7 n ON ( pl.num_ls = n.num_ls ) ",
                        fromInnerPack = " INNER JOIN " + prefFinXX + "pack p ON p.nzp_pack = pl.nzp_pack ",
                         fromInnerBank = " INNER JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank b ON (b.nzp_bank = p.nzp_bank "; 
                const string sbank = " AND b.nzp_payer = 7001) ",    //7001 - идентификатор Сбербанка
                             nosbank = " AND b.nzp_payer <> 7001) ";

                if (TempTableInWebCashe(prefFinXX + "pack_ls"))
                {

                    sql = " INSERT INTO t_report_71_1_7 ( nzp_town, town, sum_ls ) " +
                          " SELECT nzp_town, town, COUNT(num_ls) AS sum_ls " +
                          " FROM num_ls_71_1_7 " +
                          " GROUP BY 1,2 ";
                    ExecSQL(sql);

                    ExecSQL(DBManager.sUpdStat + " t_report_71_1_7 ");

                    sql = " UPDATE t_report_71_1_7 " +
                          " SET count_payment = ( SELECT COUNT(nzp_pack_ls) " +
                                                " FROM " + fromFinNumLs + fromInnerPack +
                                                " WHERE n.nzp_town = t_report_71_1_7.nzp_town " +
                                                  " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) >= '" + DatS.ToShortDateString() + "' " +
                                                  " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) <= '" + DatPo.ToShortDateString() + "'), " +
                              " sum_payment = ( SELECT SUM(g_sum_ls)" +
                                              " FROM " + fromFinNumLs + fromInnerPack +
                                              " WHERE n.nzp_town = t_report_71_1_7.nzp_town " +
                                                " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) >= '" + DatS.ToShortDateString() + "' " +
                                                " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) <= '" + DatPo.ToShortDateString() + "'), " +
                              " count_payment_sbank = ( SELECT COUNT(nzp_pack_ls) " +
                                                      " FROM " + fromFinNumLs +
                                                                 fromInnerPack +
                                                                 fromInnerBank + sbank +
                                                      " WHERE n.nzp_town = t_report_71_1_7.nzp_town " +
                                                        " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) >= '" + DatS.ToShortDateString() + "' " +
                                                        " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) <= '" + DatPo.ToShortDateString() + "'), " +
                              " sum_payment_sbank = ( SELECT SUM(g_sum_ls) " +
                                                    " FROM " + fromFinNumLs +
                                                               fromInnerPack +
                                                               fromInnerBank + sbank +
                                                    " WHERE n.nzp_town = t_report_71_1_7.nzp_town " +
                                                      " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) >= '" + DatS.ToShortDateString() + "' " +
                                                      " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) <= '" + DatPo.ToShortDateString() + "'), " +
                              " count_payment_nosbank = ( SELECT COUNT(nzp_pack_ls) " +
                                                        " FROM " + fromFinNumLs +
                                                                   fromInnerPack +
                                                                   fromInnerBank + nosbank +
                                                        " WHERE n.nzp_town = t_report_71_1_7.nzp_town " +
                                                          " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) >= '" + DatS.ToShortDateString() + "' " +
                                                          " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) <= '" + DatPo.ToShortDateString() + "'), " +
                              " sum_payment_nosbank = ( SELECT SUM(g_sum_ls) " +
                                                      " FROM " + fromFinNumLs +
                                                                 fromInnerPack +
                                                                 fromInnerBank + nosbank +
                                                      " WHERE n.nzp_town = t_report_71_1_7.nzp_town " + 
                                                        " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) >= '" + DatS.ToShortDateString() + "' " +
                                                        " AND (CASE WHEN pl." + datParam + " IS NULL THEN p." + datParam + " ELSE pl." + datParam + " END) <= '" + DatPo.ToShortDateString() + "') " ;
                    ExecSQL(sql);

                    sql = " INSERT INTO all_71_1_7 (nzp_town, town, sum_ls, count_payment, sum_payment, count_payment_sbank, sum_payment_sbank, count_payment_nosbank, sum_payment_nosbank ) " +
                          " SELECT nzp_town, town, sum_ls, count_payment, sum_payment, count_payment_sbank, sum_payment_sbank, count_payment_nosbank, sum_payment_nosbank " +
                          " FROM t_report_71_1_7 ";
                    ExecSQL(sql);

                    ExecSQL(" DELETE FROM t_report_71_1_7 ");

                    sql = " SELECT DISTINCT nzp_town, payer " +
                          " FROM " + fromFinNumLs +
                                     fromInnerPack +
                                     fromInnerBank + nosbank +
                                   " INNER JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp ON sp.nzp_payer = b.nzp_payer ";
                    DataTable banksTable = ExecSQLToTable(sql);
                    if (banksTable.Rows.Count > 0)
                    {
                        foreach (DataRow bankRow in banksTable.Rows)
                        {
                            string nzpTown = bankRow["nzp_town"].ToString().Trim(),
                                nzpBank = bankRow["payer"].ToString().Trim();
                            if (!string.IsNullOrEmpty(nzpTown) && !string.IsNullOrEmpty(nzpBank))
                            {
                                if (comment.ContainsKey(nzpTown) && !comment[nzpTown].Contains(nzpBank))
                                {
                                    comment[nzpTown] += nzpBank + ",";
                                }
                                else comment.Add(nzpTown, nzpBank + ",");
                            }
                        }
                    }
                }

            }

            #region Заполение параметров

            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 1048 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable postDTable = ExecSQLToTable(sql);
            if (postDTable.Rows.Count != 0)
                PostDirector = postDTable.Rows[0]["val_prm"].ToString().TrimEnd();

            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 1047 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable nameDTable = ExecSQLToTable(sql);
            if (nameDTable.Rows.Count != 0)
                NameDirector = nameDTable.Rows[0]["val_prm"].ToString().TrimEnd();

            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 578 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable postPTable = ExecSQLToTable(sql);
            if (postPTable.Rows.Count != 0)
                PostPasport = postPTable.Rows[0]["val_prm"].ToString().TrimEnd();

            #endregion

            sql = " INSERT INTO t_report_71_1_7 (nzp_town, town, sum_ls, count_payment, sum_payment, count_payment_sbank, sum_payment_sbank, count_payment_nosbank, sum_payment_nosbank ) " +
                  " SELECT nzp_town, " +
                         " town, " +
                         " MAX(sum_ls) AS sum_ls, " +
                         " SUM(count_payment) AS count_payment, " +
                         " SUM(sum_payment) AS sum_payment, " +
                         " SUM(count_payment_sbank) AS count_payment_sbank, " +
                         " SUM(sum_payment_sbank) AS sum_payment_sbank, " +
                         " SUM(count_payment_nosbank) AS count_payment_nosbank, " +
                         " SUM(sum_payment_nosbank) AS sum_payment_nosbank " +
                  " FROM all_71_1_7 " +
                  " GROUP BY 1,2 ";
            ExecSQL(sql);

            foreach (var nzpTown in comment.Keys)
            {
                sql = " UPDATE t_report_71_1_7" +
                      " SET comment = '" + comment[nzpTown].TrimEnd().TrimEnd(',') + "' " +
                      " WHERE nzp_town = " + nzpTown;
                ExecSQL(sql);
            }

            sql = " SELECT TRIM(town) AS town, sum_ls, count_payment, sum_payment, count_payment_sbank, sum_payment_sbank, " +
                                    " count_payment_nosbank, sum_payment_nosbank, TRIM(comment) AS comment " +
                  " FROM t_report_71_1_7 ";
            DataTable allTable = ExecSQLToTable(sql);
            allTable.TableName = "Q_master";

            if (allTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = allTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(allTable.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(allTable);

            return ds;
        }

        private string GetWhereWp()
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
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND k.nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point k WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        private string GetRajon()
        {
            var result = String.Empty;
            RajonHeader = String.Empty;
            if (Rajon != null)
            {
                result = Rajon.Aggregate(result, (current, nzpRaj) => current + (nzpRaj + ","));
            }
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND u.nzp_raj in (" + result + ")" : String.Empty;

            if (!String.IsNullOrEmpty(result))
            {
                RajonHeader = string.Empty;
                var sql = " SELECT TRIM(t.town) || '/' || trim(u.rajon) AS rajon, u.nzp_raj " +
                          " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon u, " +
                                     ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
                          " WHERE t.nzp_town = u.nzp_town   " + result +
                          " GROUP BY 1,2 " +
                          " ORDER BY 1,2";
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    RajonHeader += dr["rajon"].ToString().Trim() + ", ";
                }
                RajonHeader = RajonHeader.TrimEnd(',',' ');
            }
            return result;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_71_1_7 ( " +
                         " nzp_town INTEGER, " +
                         " nzp_bank INTEGER, " +
                         " town CHARACTER(100), " +
                         " sum_ls INTEGER, " +
                         " count_payment INTEGER, " +
                         " sum_payment " + DBManager.sDecimalType + "(14,2), " +
                         " count_payment_sbank INTEGER, " +
                         " sum_payment_sbank " + DBManager.sDecimalType + "(14,2), " +
                         " count_payment_nosbank INTEGER, " +
                         " sum_payment_nosbank " + DBManager.sDecimalType + "(14,2)," +
                         " comment CHARACTER(500)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE INDEX ix_t_report_71_1_7 ON t_report_71_1_7(nzp_town) ";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE all_71_1_7 ( " +
                         " nzp_town INTEGER, " +
                         " town CHARACTER(100), " +
                         " sum_ls INTEGER, " +
                         " count_payment INTEGER, " +
                         " sum_payment " + DBManager.sDecimalType + "(14,2), " +
                         " count_payment_sbank INTEGER, " +
                         " sum_payment_sbank " + DBManager.sDecimalType + "(14,2), " +
                         " count_payment_nosbank INTEGER, " +
                         " sum_payment_nosbank " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE num_ls_71_1_7(" +
                  " nzp_town INTEGER, " +
                  " town CHARACTER(100), " +
                  " num_ls INTEGER)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE INDEX ix_num_ls_71_1_7 ON num_ls_71_1_7(num_ls) ";
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_71_1_7");
            ExecSQL("DROP TABLE all_71_1_7");
            ExecSQL("DROP TABLE num_ls_71_1_7");
        }
    }
}

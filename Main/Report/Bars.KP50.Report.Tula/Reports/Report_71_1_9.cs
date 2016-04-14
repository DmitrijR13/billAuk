using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71015019 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.9 Отчет по изменениям"; }
        }

        public override string Description
        {
            get { return "Отчет по изменениям"; }
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
            get { return Resources.Report_71_1_9; }
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

        private List<byte> CategoryParameter { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }


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
                new ComboBoxParameter(multiSelect:true)
                {
                        Name = "Категория параметров",
                        TypeValue = typeof(List<byte>),
                        Code = "CategoryParameter",
                        Require = true,
                        MultiSelect = true,
                        StoreData = new List<object>
                        {
                             new { Id = 1, Name = "Квартирные параметры"},
                             new { Id = 2, Name = "Домовые параметры"},
                             new { Id = 3, Name = "Параметры расчета"},
                             new { Id = 4, Name = "Параметры территории"},
                             new { Id = 5, Name = "Параметры ЖЭУ"},
                             new { Id = 6, Name = "Системные параметры"},
                             new { Id = 7, Name = "Параметры поставщиков"}
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

            CategoryParameter = UserParamValues["CategoryParameter"].GetValue<List<byte>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period = DatS == DatPo
                            ? "за " + DatS.ToShortDateString()
                            : "за период c " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString();
            report.SetParameterValue("period", period);
            report.SetParameterValue("date",DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            #region заполнение временной таблицы
            string sql = " SELECT bd_kernel AS pref " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);

            while(reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string prefData = pref + DBManager.sDataAliasRest,
                         prefKernel = pref + DBManager.sKernelAliasRest;

                if (CategoryParameter.IsNotEmpty())
                {
                    foreach (byte numCategory in CategoryParameter)
                    {
                        switch (numCategory)
                        {
                            case 1:

                                #region Квартирные параметры

                                sql = " INSERT INTO t_lic_15_1_9(nzp_kvar, dat_when, num_ls, fio, name_prm, val_prm, name, comment) " +
                                      " SELECT nzp AS nzp_kvar, " +
                                             " dat_when," +
                                             " num_ls, " +
                                             " fio, " +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_1 p INNER JOIN " + prefData +"kvar k ON k.nzp_kvar = p.nzp " +
                                                                   " INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                   " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                sql = " INSERT INTO t_lic_15_1_9(nzp_kvar, dat_when, num_ls, fio, name_prm, val_prm, name, comment) " +
                                      " SELECT nzp AS nzp_kvar, " +
                                             " dat_when," +
                                             " num_ls, " +
                                             " fio, " +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_3 p INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = p.nzp " +
                                                                   " INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                   " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                sql = " UPDATE t_lic_15_1_9 " +
                                      " SET address = ( SELECT (CASE WHEN TRIM(rajon) = '-' THEN " + DBManager.sNvlWord + "(TRIM(town),'') " + 
                                                                                          " ELSE " + DBManager.sNvlWord + "(TRIM(rajon),'') END) || ', ' || " +
                                                             " (CASE WHEN TRIM(ulica) IS NULL THEN '' ELSE TRIM(ulica) || ', ' END) || " +
                                                             " (CASE WHEN TRIM(ndom) IS NULL THEN '' ELSE TRIM(ndom) || ', ' END) || " +
                                                             " (CASE WHEN TRIM(nkor) = '-' THEN '' ELSE " + DBManager.sNvlWord + "(TRIM(nkor) || ', ','') END) || " +
                                                             " (CASE WHEN TRIM(nkvar) ='-' THEN '' ELSE " + DBManager.sNvlWord + "(TRIM(nkvar),'') END) " +
                                      " FROM " + prefData + "kvar k INNER JOIN  " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                                  " INNER JOIN  " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                                  " INNER JOIN  " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                                  " INNER JOIN  " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                                      " WHERE t_lic_15_1_9.nzp_kvar = k.nzp_kvar ) ";
                                ExecSQL(sql);

                                #endregion

                                break;
                            case 2:

                                #region Домовые параметры

                                 sql = " INSERT INTO t_dom_15_1_9(nzp_dom, dat_when, name_prm, val_prm, name, comment) " +
                                      " SELECT nzp AS nzp_dom, " +
                                             " dat_when," +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_2 p INNER JOIN " + prefData +"dom d ON d.nzp_dom = p.nzp " +
                                                                   " INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                   " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                sql = " UPDATE t_dom_15_1_9 " +
                                      " SET address = ( SELECT (CASE WHEN TRIM(rajon) = '-' THEN " + DBManager.sNvlWord + "(TRIM(town),'') " + 
                                                                                          " ELSE " + DBManager.sNvlWord + "(TRIM(rajon),'') END) || ', ' || " +
                                                             " (CASE WHEN TRIM(ulica) IS NULL THEN '' ELSE TRIM(ulica) || ', ' END) || " +
                                                             " (CASE WHEN TRIM(ndom) IS NULL THEN '' ELSE TRIM(ndom) || ', ' END) || " +
                                                             " (CASE WHEN TRIM(nkor) = '-' THEN '' ELSE " + DBManager.sNvlWord + "(TRIM(nkor) || ', ','') END) " +
                                      " FROM " + prefData + "dom d  INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                                 " INNER JOIN  " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                                 " INNER JOIN  " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                                      " WHERE t_dom_15_1_9.nzp_dom = d.nzp_dom ) ";
                                ExecSQL(sql);

                                #endregion

                                break;
                            case 3:

                                #region Параметры расчёта

                                sql = " INSERT INTO t_payment_15_1_9(dat_when, name_prm, val_prm, name, comment) " +
                                      " SELECT dat_when," +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_5 p INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                   " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                #endregion

                                break;
                            case 4:

                                #region Параметры территории

                                sql = " INSERT INTO t_area_15_1_9(nzp_area, dat_when, name_prm, val_prm, name, comment) " +
                                      " SELECT nzp AS nzp_area, " +
                                             " dat_when," +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_7 p INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                   " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                sql = " UPDATE t_area_15_1_9 " +
                                      " SET area = ( SELECT TRIM(area) " +
                                      " FROM " + prefData +"s_area a " +
                                      " WHERE t_area_15_1_9.nzp_area = a.nzp_area ) ";
                                ExecSQL(sql);

                                #endregion

                                break;
                            case 5:

                                #region Параметры ЖЭУ

                                sql = " INSERT INTO t_geu_15_1_9(nzp_geu, dat_when, name_prm, val_prm, name, comment) " +
                                      " SELECT nzp AS nzp_geu, " +
                                             " dat_when," +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_8 p INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                   " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                sql = " UPDATE t_geu_15_1_9 " +
                                      " SET geu = ( SELECT TRIM(geu) " +
                                      " FROM " + prefData +"s_geu g " +
                                      " WHERE t_geu_15_1_9.nzp_geu = g.nzp_geu ) ";
                                ExecSQL(sql);

                                #endregion

                                break;
                            case 6:

                                #region Системные параметры (из локального банка)

                                sql = " INSERT INTO t_system_15_1_9(dat_when, name_prm, val_prm, name, comment) " +
                                      " SELECT dat_when," +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_10 p INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                   " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                #endregion

                                break;
                            case 7:

                                #region Параметры поставщиков

                                sql = " INSERT INTO t_supplier_15_1_9(nzp_supp, dat_when, name_prm, val_prm, name, comment) " +
                                      " SELECT nzp AS nzp_supp, " +
                                             " dat_when," +
                                             " name_prm, " +
                                             " val_prm, " +
                                             " name, " +
                                             " comment " +
                                      " FROM " + prefData + "prm_11 p INNER JOIN " + prefKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                                                    " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                                ExecSQL(sql);

                                sql = " UPDATE t_supplier_15_1_9 " +
                                      " SET name_supp = ( SELECT TRIM(name_supp) " +
                                      " FROM " + prefKernel + "supplier s " +
                                      " WHERE t_supplier_15_1_9.nzp_supp = s.nzp_supp ) ";
                                ExecSQL(sql);

                                #endregion

                                break;
                        }
                    }
                }

                
            }

            reader.Close();
            #endregion
            if (CategoryParameter.IsNotNull())
                if (CategoryParameter.Exists(numCategory => numCategory == 6))
                {
                    #region Системные параметры (из глобального банка)

                sql = " INSERT INTO t_system_15_1_9(dat_when, name_prm, val_prm, name, comment) " +
                      " SELECT dat_when," +
                             " name_prm, " +
                             " val_prm, " +
                             " name, " +
                             " comment " +
                      " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 p " +
                                            " INNER JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                                            " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user = p.nzp_user" +
                      " WHERE dat_when >= '" + DatS.ToShortDateString() + "' " +
                        " AND dat_when <= '" + DatPo.ToShortDateString() + "' ";
                ExecSQL(sql);

                #endregion
                }

            var ds = new DataSet();

            #region Выборка данных в таблицы

            sql = " SELECT dat_when, num_ls, TRIM(address) AS address, TRIM(fio) AS fio, " +
                                " TRIM(name_prm) AS name_prm, TRIM(val_prm) AS val_prm, TRIM(name) AS name, TRIM(comment) AS comment " +
                  " FROM t_lic_15_1_9 " +
                  " ORDER BY 1 DESC";

            DataTable licTable = ExecSQLToTable(sql);
            licTable.TableName = "licTable";

            if (licTable.Rows.Count >= 64800)
            {
                if (ReportParams.ExportFormat == ExportFormat.Excel2007)
                {
                    var dtr = licTable.Rows.Cast<DataRow>().Skip(64800).ToArray();
                    dtr.ForEach(licTable.Rows.Remove);
                    licTable.Rows.Add(DateTime.Now,0, "Не все записи уместились", "на одном листе", "", "", "","");
                }
            }

            ds.Tables.Add(licTable);

            sql = " SELECT dat_when, TRIM(address) AS address, TRIM(name_prm) AS name_prm, " +
                                " TRIM(val_prm) AS val_prm, TRIM(name) AS name, TRIM(comment) AS comment " +
                  " FROM t_dom_15_1_9 " +
                  " ORDER BY 1 DESC";

            DataTable domTable = ExecSQLToTable(sql);
            domTable.TableName = "domTable";
            #region ограничение записей
            if (domTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = domTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(domTable.Rows.Remove);
            }
            #endregion
            ds.Tables.Add(domTable);

            sql = " SELECT dat_when, TRIM(name_prm) AS name_prm, TRIM(val_prm) AS val_prm, TRIM(name) AS name, TRIM(comment) AS comment " +
                  " FROM t_payment_15_1_9 " +
                  " ORDER BY 1 DESC";

            DataTable paymentTable = ExecSQLToTable(sql);
            paymentTable.TableName = "paymentTable";
            #region ограничение записей
            if (paymentTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = paymentTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(paymentTable.Rows.Remove);
            }
            #endregion
            ds.Tables.Add(paymentTable);

            sql = " SELECT dat_when, TRIM(area) AS area, TRIM(name_prm) AS name_prm, TRIM(val_prm) AS val_prm, TRIM(name) AS name, TRIM(comment) AS comment " +
                  " FROM t_area_15_1_9 " +
                  " ORDER BY 1 DESC";

            DataTable areaTable = ExecSQLToTable(sql);
            areaTable.TableName = "areaTable";
            #region ограничение записей
            if (areaTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = areaTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(areaTable.Rows.Remove);
            }
            #endregion
            ds.Tables.Add(areaTable);

            sql = " SELECT dat_when, TRIM(geu) AS geu, TRIM(name_prm) AS name_prm, TRIM(val_prm) AS val_prm, TRIM(name) AS name, TRIM(comment) AS comment " +
                  " FROM t_geu_15_1_9 " +
                  " ORDER BY 1 DESC";

            DataTable geuTable = ExecSQLToTable(sql);
            geuTable.TableName = "geuTable";
            #region ограничение записей
            if (geuTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = geuTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(geuTable.Rows.Remove);
            }
            #endregion
            ds.Tables.Add(geuTable);

            sql = " SELECT dat_when, TRIM(name_prm) AS name_prm, TRIM(val_prm) AS val_prm, TRIM(name) AS name, TRIM(comment) AS comment " +
                  " FROM t_system_15_1_9 " +
                  " ORDER BY 1 DESC";

            DataTable systemTable = ExecSQLToTable(sql);
            systemTable.TableName = "systemTable";
            #region ограничение записей
            if (systemTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = systemTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(systemTable.Rows.Remove);
            }
            #endregion
            ds.Tables.Add(systemTable);

            sql = " SELECT dat_when, TRIM(name_supp) AS name_supp, TRIM(name_prm) AS name_prm, TRIM(val_prm) AS val_prm, TRIM(name) AS name, TRIM(comment) AS comment " +
                  " FROM t_supplier_15_1_9 " +
                  " ORDER BY 1 DESC";

            DataTable supplierTable = ExecSQLToTable(sql);
            supplierTable.TableName = "supplierTable";
            #region ограничение записей
            if (supplierTable.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = supplierTable.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(supplierTable.Rows.Remove);
            }
            #endregion
            ds.Tables.Add(supplierTable);

            #endregion

            return ds;
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

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_lic_15_1_9( " +
                            " nzp_kvar INTEGER, " +
                            " dat_when DATE, " +
                            " address CHARACTER(150), " +
                            " num_ls INTEGER, " +
                            " fio CHARACTER(50), " +
                            " name_prm CHARACTER(100), " +
                            " val_prm CHARACTER(128), " +
                            " name CHARACTER(40), " +
                            " comment CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_dom_15_1_9( " +
                            " nzp_dom INTEGER, " +
                            " dat_when DATE, " +
                            " address CHARACTER(150), " +
                            " name_prm CHARACTER(100), " +
                            " val_prm CHARACTER(20), " +
                            " name CHARACTER(40), " +
                            " comment CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_payment_15_1_9( " +
                            " dat_when DATE, " +
                            " name_prm CHARACTER(100), " +
                            " val_prm CHARACTER(20), " +
                            " name CHARACTER(40), " +
                            " comment CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_area_15_1_9( " +
                            " nzp_area INTEGER, " +
                            " dat_when DATE, " +
                            " area CHARACTER(40), " +
                            " name_prm CHARACTER(100), " +
                            " val_prm CHARACTER(100), " +
                            " name CHARACTER(40), " +
                            " comment CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_geu_15_1_9( " +
                            " nzp_geu INTEGER, " +
                            " dat_when DATE, " +
                            " geu CHARACTER(60), " +
                            " name_prm CHARACTER(100), " +
                            " val_prm CHARACTER(100), " +
                            " name CHARACTER(40), " +
                            " comment CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_system_15_1_9( " +
                            " dat_when DATE, " +
                            " name_prm CHARACTER(100), " +
                            " val_prm CHARACTER(100), " +
                            " name CHARACTER(40), " +
                            " comment CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_supplier_15_1_9( " +
                            " nzp_supp INTEGER, " +
                            " dat_when DATE, " +
                            " name_supp CHARACTER(100), " +
                            " name_prm CHARACTER(100), " +
                            " val_prm CHARACTER(20), " +
                            " name CHARACTER(40), " +
                            " comment CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_lic_15_1_9 ");
            ExecSQL(" DROP TABLE t_dom_15_1_9 ");
            ExecSQL(" DROP TABLE t_payment_15_1_9 ");
            ExecSQL(" DROP TABLE t_area_15_1_9 ");
            ExecSQL(" DROP TABLE t_geu_15_1_9 ");
            ExecSQL(" DROP TABLE t_system_15_1_9 ");
            ExecSQL(" DROP TABLE t_supplier_15_1_9 ");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Obninsk.Properties;
using Bars.KP50.Utils;
using FastReport;
using Newtonsoft.Json;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;

namespace Bars.KP50.Report.Obninsk.Reports
{
    class Report40GenDomParams : BaseSqlReportWithDates
    {
        public override string Name
        {
            get { return "40. Генератор по домовым параметрам"; }
        }

        public override string Description
        {
            get { return "40. Генератор по домовым параметрам"; }
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
            get { return Resources.Report_40GenDomParams; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }
        #region Параметры

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Поставщики</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Параметры</summary>
        private List<long> Params { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Превышение </summary>
        private bool RowCount { get; set; }

        /// <summary>Районы</summary>
        protected string AddressHeader { get; set; }

        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        private int stringsInPack = 50000;

        private bool isArchive = false;

        #endregion
        protected override Stream GetTemplate()
        {
            if (UserParamValues["ExportFormat"].GetValue<int>() == 1)
                return new MemoryStream(Resources.Report_40GenDomParams);

            return new MemoryStream(Template);
        }   

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter {Value = Operday.Month },
                new YearParameter {Value = Operday.Year},  
                new BankSupplierParameter(),    
                new AddressParameter(), 
                new DomParamsParameter{Require = true}
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
           var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());

            report.SetParameterValue("pPeriod", months[Month] + " " + Year + "г.");
     


            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми 40000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year =UserParamValues["Year"].Value.To<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
            Banks = BankSupplier.Banks;
            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            Params = UserParamValues["DomParams"].GetValue<List<long>>(); 
       }

        public override DataSet GetData()
        {
            GetWhereAdr();
            string wherePrm = GetPrms(),
                sql,
                dat_po = "'" + DateTime.DaysInMonth(Year, Month) + "." + Month.ToString("00") + "." + Year + "'",
                dat_s = "'01." + Month.ToString("00") + "." + Year + "'"; 


            foreach (var pref in PrefBanks)
            {
                string prmTable = pref + DBManager.sDataAliasRest + "prm_2 ",
                    res = pref + DBManager.sKernelAliasRest + "res_y ";
                sql = " INSERT INTO tmp_GenDom(nzp_dom, nzp_prm, nzp_res, val, val_sprav )" +
                      " SELECT sd.nzp_dom, t.nzp_prm, t.nzp_res, " +
                      " (CASE WHEN nzp_res IS NULL OR nzp_res<=0 THEN val_prm END)," +
                      " (CASE WHEN nzp_res IS NOT NULL AND nzp_res>0 THEN val_prm" + DBManager.sConvToInt + " END) " +
                      " FROM sel_dom sd JOIN " + prmTable + " prm2 ON sd.nzp_dom=prm2.nzp " +
                      " JOIN tmp_prm t ON t.nzp_prm=prm2.nzp_prm AND is_actual=1 " +
                      " AND dat_s<= "+dat_po+" AND dat_po>= "+dat_s;
                ExecSQL(sql);

                sql = " UPDATE tmp_GenDom SET val= (SELECT TRIM(name_y) " +
                      " FROM " + res + " r " +
                      " WHERE tmp_GenDom.nzp_res=r.nzp_res AND tmp_GenDom.val_sprav=r.nzp_y)" +
                      " WHERE val_sprav IS NOT NULL AND val IS NULL";
                ExecSQL(sql);
            }

            string Tdata = ReportParams.Pref + DBManager.sDataAliasRest;
            DataTable dt =
                ExecSQLToTable(
                    " SELECT area, (case when rajon='-' then town else trim(town)||','||trim(rajon) end) as rajon, " +
                    " ulica||' '||"+DBManager.sNvlWord+"(ulicareg,'') AS ulica," +
                    " idom, ndom||' '||(case when nkor is not null and nkor<>'-' then nkor else '' end) AS ndom, name_prm," +
                    " TRIM(val) as val_prm " +
                    " FROM tmp_GenDom t JOIN " + Tdata + "dom d ON t.nzp_dom=d.nzp_dom " +
                    " JOIN " + Tdata + "s_area a ON d.nzp_area=a.nzp_area " +
                    " JOIN " + Tdata + "s_ulica su ON d.nzp_ul=su.nzp_ul " +
                    " JOIN " + Tdata + "s_rajon sr ON su.nzp_raj=sr.nzp_raj " +
                    " JOIN " + Tdata + "s_town st ON sr.nzp_town=st.nzp_town " +
                    " JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest + "prm_name pn  ON t.nzp_prm=pn.nzp_prm " +
                    " ORDER BY area, town, rajon, ulica, ulicareg, idom, ndom, nkor, name_prm, val ");


            dt.TableName = "Q_master";
            RowCount = false;

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;

        }


        protected override void CreateTempTable()
        {    
            const  string sql = " create temp table sel_dom( nzp_dom  integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            const string sqlprm = " create temp table tmp_prm(" +
                                  " nzp_prm  integer," +
                                  " nzp_res integer," +
                                  " type_prm integer," +
                                  " name_prm char(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sqlprm);

            const string sqltab = " create temp table tmp_GenDom(" +
                                  " nzp_dom  integer," +
                                  " nzp_prm  integer," +
                                  " nzp_res integer," +
                                  " val_sprav integer," +
                                  " val char(150)) " + DBManager.sUnlogTempTable;
            ExecSQL(sqltab);
        }                              

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE sel_dom ");
            ExecSQL(" DROP TABLE tmp_GenDom ");
            ExecSQL(" DROP TABLE tmp_prm ");
        }

        /// <summary>Сформировать отчет</summary>
        /// <param name="ds">Данные отчета в виде DataSet</param>
        public override void Generate(DataSet ds)
        {
            if (ds.Tables["Q_master"].Rows.Count >= stringsInPack)
            {
                isArchive = true;
                var reports = new List<string>();
                var qm = ds.Tables["Q_master"].Copy();


                int count = 0;
                string nzpUtil = ReportParams.PathForSave.Substring(ReportParams.PathForSave.LastIndexOf('/') + 1,
                    ReportParams.PathForSave.LastIndexOf('.') - ReportParams.PathForSave.LastIndexOf('/') - 1);
                do
                {
                    ds.Tables.Remove("Q_master");
                    ds.Tables.Add(
                        qm.AsEnumerable()
                            .Skip(count * stringsInPack)
                            .Take(qm.Rows.Count - (count + 1) * stringsInPack > 0 ? stringsInPack : qm.Rows.Count)
                            .CopyToDataTable());
                    ds.Tables[ds.Tables.Count - 1].TableName = "Q_master";

                    var report = new FastReport.Report();
                    report.Load(GetTemplate());
                    report.RegisterData(ds);
                    PrepareReport(report);

                    var cleanPath = ReportParams.PathForSave;
                    ReportParams.PathForSave = ReportParams.PathForSave.Insert(ReportParams.PathForSave.LastIndexOf('.'),
                        "_" + (count * stringsInPack + 1) + "-" +
                        (qm.Rows.Count - (count + 1) * stringsInPack > 0
                            ? (count + 1) * stringsInPack
                            : qm.Rows.Count));
                    SaveReport(report);

                    reports.Add(ReportParams.PathForSave);
                    if (((count + 1) * stringsInPack) / qm.Rows.Count < 1)
                        SetProccessPercent((((count + 1) * stringsInPack) / qm.Rows.Count).ToDecimal());

                    ReportParams.PathForSave = cleanPath;
                    count++;
                } while (qm.Rows.Count - (count) * stringsInPack > 0);

                var exporter = GetExporter();
                if (reports.Count > 0)
                    Archive.GetInstance().Compress(Constants.ExcelDir.TrimEnd('/') + @"\" + Name + ".zip", reports.ToArray(), true);

                if (InputOutput.useFtp)
                {
                    ReportParams.PathForSave = InputOutput.SaveOutputFile(Constants.ExcelDir.TrimEnd('/') + @"\" + Name + ".zip");
                    ExecSQL(" update public.excel_utility " +
                            " set exc_path = '" + ReportParams.PathForSave + "' " +
                            " where nzp_exc = " + nzpUtil);
                    ExecSQL(" update public.excel_utility " +
                            " set file_name = '" + ReportParams.PathForSave.Substring(0, ReportParams.PathForSave.LastIndexOf('.')) + ".zip" + "' " +
                            " where nzp_exc = " + nzpUtil);
                }
            }
            else
                base.Generate(ds);
        }

        /// <summary>Сохранить отчет</summary>
        /// <param name="report">Отчет</param>
        protected override void SaveReport(FastReport.Report report)
        {
            var exporter = GetExporter();
            exporter.ShowProgress = false;
            var env = new EnvironmentSettings { ReportSettings = { ShowProgress = false } };
            report.Prepare();
            if (IsPreview)
            {
                report.SavePrepared(ReportParams.PathForSave);
            }
            else
            {
                exporter.Export(report, ReportParams.PathForSave);
            }
            if (!isArchive && InputOutput.useFtp)
            {
                ReportParams.PathForSave = InputOutput.SaveOutputFile(ReportParams.PathForSave);
            }
        } 


        #region Filtrs

        private void GetwhereWp()
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
            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());

                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
 }

        /// <summary>
        /// Получить условия органичения по поставщикам
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
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        private void GetWhereAdr()
        {
            GetwhereWp();

            string rajon = string.Empty,
                street = string.Empty,
                house = string.Empty,
                whereSupp=GetWhereSupp("t.nzp_supp"),
                dat_po= "'"+DateTime.DaysInMonth(Year, Month)+"."+Month.ToString("00")+"."+Year+"'",
                dat_s = "'01." + Month.ToString("00") + "." + Year+"'";    


            if (Address.Raions != null)
            {
                rajon = Address.Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ","));
                rajon = rajon.TrimEnd(',');
            }
            if (Address.Streets != null)
            {
                street = Address.Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ","));
                street = street.TrimEnd(',');
            }
            if (Address.Houses != null)
            {
                house = Address.Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ","));
                house = house.TrimEnd(',');
            }

            string result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            result = result.TrimEnd(',');
            result = !string.IsNullOrEmpty(result) ? " AND d.nzp_area in (" + result + ")" : string.Empty;
            result += !string.IsNullOrEmpty(rajon) ? " AND u.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !string.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !string.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;
            

            foreach (var pref in PrefBanks)
            {
                string prefData = pref + DBManager.sDataAliasRest;
                string sql = " INSERT INTO sel_dom (nzp_dom)" +
                             " SELECT k.nzp_dom " +
                             " FROM " + prefData + "kvar k " +
                             " JOIN " + prefData + "tarif t ON k.nzp_kvar=t.nzp_kvar AND is_actual=1" +
                             " AND dat_s <= " + dat_po + " AND dat_po>= " + dat_s + whereSupp +
                             " JOIN " + prefData + "dom d ON k.nzp_dom=d.nzp_dom " +
                             " JOIN " + prefData + "s_ulica u ON d.nzp_ul=u.nzp_ul " + result+
                             " GROUP BY 1 ";
                ExecSQL(sql); 
            }       
            ExecSQL("create index ix_tmpsk_ls_s2 on sel_dom(nzp_dom)");
        }

        private string GetPrms()
        {
            string prms="";                              
                prms = Params.Aggregate(prms, (current, nzpPrm) => current + (nzpPrm + ","));
                prms = prms.TrimEnd(',');
            string sql = " INSERT INTO tmp_prm(nzp_prm, nzp_res, name_prm)" +
                         " SELECT nzp_prm, nzp_res, name_prm " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "prm_name " +
                         " WHERE nzp_prm IN (" + prms + " )";
            ExecSQL(sql);
            return " AND prm2 nzp_prm IN (" + prms + ")";
        }


        #endregion

    }
}

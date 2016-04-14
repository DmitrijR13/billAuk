using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report300207 : BaseSqlReport
    {
        /// <summary>Наименование отчета</summary>
        public override string Name
        {
            get { return "30.2.7 Список собственников"; }
        }

        /// <summary>Описание отчета</summary>
        public override string Description
        {
            get { return "Список собственников"; }
        }

        /// <summary>Группа отчета</summary>
        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        /// <summary>Предварительный просмотр</summary>
        public override bool IsPreview
        {
            get { return false; }
        }

        /// <summary>Файл-шаблон отчета</summary>
        protected override byte[] Template
        {
            get { return Resources.Report_30_2_7; }
        }

        /// <summary>Тип отчета</summary>
        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        #region~~~~~~~~~~~~~~~~~~~~~Параметры~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>День</summary>
        private DateTime Date { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }
        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }
        /// <summary>Превышение </summary>
        private bool RowCount { get; set; }

        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }

        /// <summary>Улица</summary>
        private string Raions { get; set; }

        /// <summary>Улица</summary>
        private string Streets { get; set; }

        /// <summary>Дом</summary>
        private string Houses { get; set; }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Параметры для отображение на странице браузера</summary>
        /// <returns>Список параметров</returns>
        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new BankParameter(),
                new AddressParameter(),
             };
        }

        /// <summary>Заполнить параметры в отчете</summary>
        /// <param name="report">Объект формы отчета</param>
        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("date", Date);
            report.SetParameterValue("headerParam", TerritoryHeader); 
            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми 70000 строками. " +
                      "Выберите другой формат экспортируемого файла, " +
                      "либо поставьте другие ограничения для отчета"
                    : "");
        }

        /// <summary>Заполнить параметры</summary>
        protected override void PrepareParams()
        {
            Date = DateTime.Now;
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            if (Address.Raions != null)
            {
                Raions = String.Join(",", Address.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
                Raions = "and u.nzp_raj in (" + Raions + ") ";
            }
            if (Address.Streets != null)
            {
                Streets = String.Join(",", Address.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
                Streets = "and u.nzp_ul in (" + Streets + ") ";
            }
            if (Address.Houses != null)
            {
                Houses = String.Join(",", Address.Houses.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
                Houses = "and d.nzp_dom in ("+ Houses+ ") ";
            }
        }

        /// <summary>Выборка данных</summary>
        /// <returns>Кеш данных</returns>
        public override DataSet GetData()
        {
            GetwhereWp();   

            foreach (var pref in PrefBanks)
            {
                
                string kvarTable = pref + DBManager.sDataAliasRest + "kvar ";
                string domTable = pref + DBManager.sDataAliasRest + "dom ";
                string ulTable = pref + DBManager.sDataAliasRest + "s_ulica ";
                string prmTable = pref + DBManager.sDataAliasRest + "prm_1 ";
                string sobstwTable = pref + DBManager.sDataAliasRest + "sobstw ";
                string docsobstwTable = pref + DBManager.sDataAliasRest + "doc_sobstw ";
                string sdocsobstwTable = pref + DBManager.sDataAliasRest + "s_dok_sv ";
                string rajonTable = pref + DBManager.sDataAliasRest + "s_rajon ";
                string resTable = pref + DBManager.sKernelAliasRest + "res_y ";
                if (TempTableInWebCashe(kvarTable) &&
                    TempTableInWebCashe(domTable) &&
                    TempTableInWebCashe(ulTable) &&
                    TempTableInWebCashe(prmTable)&& 
                    TempTableInWebCashe(sobstwTable) &&
                    TempTableInWebCashe(resTable) &&
                    TempTableInWebCashe(docsobstwTable)
                    )
                {
                    string sql = " INSERT INTO t_kvars (nzp_area, rajon_t, nzp_kvar, num_ls, idom, ndom, " +
                                                         " nkor, ikvar, nkvar,nkvar_n, ulica, ulicareg) " +
                                 " SELECT d.nzp_area, rajon_t, k.nzp_kvar, num_ls, idom, ndom, nkor, " +
                                        " ikvar, nkvar,nkvar_n, ulica, ulicareg " +
                                 " FROM " + kvarTable + " k, " + domTable + " d, " + ulTable + " u,  " +  rajonTable +" r "+
                                 " WHERE k.nzp_dom = d.nzp_dom " +
                                   " AND d.nzp_ul = u.nzp_ul " +
                                   " AND u.nzp_raj = r.nzp_raj " + Raions + Streets + Houses;
                    ExecSQLToTable(sql);

                    sql = " INSERT INTO t_svod (nzp_area, rajon_t, num_ls, idom, ndom, nkor, ikvar, " +
                                                " nkvar,nkvar_n, ulica, ulicareg, fam, ima, otch, " +
                                                " dolya_up, dolya_down, dok_sv,  serij_sv, nomer_sv,  vid_mes_sv, vid_dat_sv ) " +
                        " SELECT nzp_area, rajon_t, num_ls, idom, ndom, nkor, ikvar, " +
                               " nkvar,nkvar_n, ulica, ulicareg, fam, ima, otch, d.dolya_up, d.dolya_down, " +
                               " dok_sv, d.serij_sv, d.nomer_sv,  d.vid_mes_sv, d.vid_dat_sv " +
                        " FROM t_kvars kv , " + 
                               sobstwTable + " sw LEFT OUTER JOIN " + docsobstwTable + " d ON sw.nzp_sobstw = d.nzp_sobstw " +
                                                " LEFT OUTER JOIN " + sdocsobstwTable + " sd ON d.nzp_dok_sv = sd.nzp_dok_sv " +
                        " WHERE kv.nzp_kvar=sw.nzp_kvar ";
                    ExecSQLToTable(sql);

                    sql = " UPDATE t_svod SET ob_s = " +
                          " (SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
                           " FROM " + prmTable + " p " +
                           " WHERE p.nzp_prm=4 " +
                             " AND p.nzp = t_svod.num_ls " +
                             " AND p.is_actual <> 100 " +
                             " AND p.dat_s <= '" + Date.ToShortDateString() + "' " +
                             " AND p.dat_po >= '" + Date.ToShortDateString() + "') ";
                    ExecSQLToTable(sql);

                    sql = " UPDATE t_svod SET ob_s = ob_s * dolya_up / dolya_down "+
                          " WHERE dolya_down <> 0 AND dolya_down IS NOT NULL";
                    ExecSQLToTable(sql);

                    sql = " UPDATE t_svod SET gil_s = " +
                          " (SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
                           " FROM " + prmTable + " p " +
                           " WHERE p.nzp_prm = 6 " +
                             " AND p.nzp=  t_svod.num_ls " +
                             " AND p.is_actual <> 100 " +
                             " AND p.dat_s <= '" + Date.ToShortDateString() + "' " +
                             " AND p.dat_po >= '" + Date.ToShortDateString() + "')";
                    ExecSQLToTable(sql);

                    sql = " UPDATE t_svod SET gil_s = gil_s * dolya_up / dolya_down " +
                          " WHERE dolya_down <> 0 AND dolya_down IS NOT NULL";
                    ExecSQLToTable(sql);

                    sql = " UPDATE t_svod SET type_sob = " +
                          " (SELECT MAX(name_y) " +
                           " FROM " + prmTable + " p, " + resTable + " y " +
                           " WHERE p.nzp_prm = 2009 " +
                             " AND y.nzp_res = 3001 " +
                             " AND TRIM(p.val_prm) = nzp_y " + DBManager.sConvToVarChar +
                             " AND p.nzp = t_svod.num_ls " +
                             " AND p.is_actual <> 100 " +
                             " AND p.dat_s <= '" + Date.ToShortDateString() + "' " +
                             " AND p.dat_po >= '" + Date.ToShortDateString() + "')";
                    ExecSQLToTable(sql);

                    sql = " UPDATE t_svod SET type_sob = " +
                          " (SELECT MAX(val_prm) " +
                           " FROM " + prmTable + " p " +
                           " WHERE p.nzp_prm = 8 " +
                             " AND p.nzp = t_svod.num_ls " +
                             " AND p.is_actual <> 100 " +
                             " AND p.dat_s <= '" + Date.ToShortDateString() + "' " +
                             " AND p.dat_po >= '" + Date.ToShortDateString() + "')" +
                           " WHERE type_sob IS NULL ";
                    ExecSQLToTable(sql);

                    sql = " INSERT INTO t_res SELECT * FROM t_svod ";
                    ExecSQLToTable(sql);

                    ExecSQLToTable("TRUNCATE t_kvars");
                    ExecSQLToTable("TRUNCATE t_svod");
                }
            }

            string sqlfin = " SELECT TRIM(area) AS area, " +
                                   " (CASE WHEN rajon_t IS NULL OR TRIM(rajon_t) = '-' THEN '' ELSE TRIM(rajon_t) END) AS rajon_t, " +
                                   " TRIM(ulica) || " +
                                   " (CASE WHEN ulicareg IS NULL OR TRIM(ulicareg) = '-' OR TRIM(ulicareg) = '' THEN '' ELSE ' ' || TRIM(ulicareg) END) || " +
                                   " (CASE WHEN ndom IS NULL OR TRIM(ndom) = '-' OR TRIM(ndom) = '' THEN '' ELSE ', д. ' || TRIM(ndom) END) || " +
                                   " (CASE WHEN nkor IS NULL OR TRIM(nkor) = '-' OR TRIM(nkor) = '' THEN '' ELSE ' корп. ' || TRIM(nkor) END) || " +
                                   " (CASE WHEN nkvar IS NULL OR TRIM(nkvar) = '-' OR TRIM(nkvar) = '' THEN '' ELSE ', кв.  ' || TRIM(nkvar) END) || " +
                                   " (CASE WHEN nkvar_n IS NULL OR TRIM(nkvar_n) = '-' OR TRIM(nkvar_n) = '' THEN '' ELSE ' комн. ' || TRIM(nkvar_n) END)  AS address, " +
                                   " TRIM(ulica) || " +
                                   " (CASE WHEN ulicareg IS NULL OR TRIM(ulicareg) = '-' OR TRIM(ulicareg) = '' " +
                                         " THEN '' ELSE ' ' || TRIM(ulicareg) END) AS ulica, " +
                                   " TRIM(ndom) || " +
                                   " (CASE WHEN nkor IS NULL OR TRIM(nkor) = '-' OR TRIM(nkor) = '' " +
                                         " THEN '' ELSE ' корп. ' || TRIM(nkor) END) AS ndom, " +
                                   " TRIM(nkvar) || " +
                                   " (CASE WHEN nkvar_n IS NULL OR TRIM(nkvar_n) = '-' OR TRIM(nkvar_n) = '' " +
                                         " THEN '' ELSE ' комн. ' || TRIM(nkvar_n) END) AS nkvar, " +
                                   " num_ls, " +
                                   " (CASE WHEN type_sob = '1' THEN 'Приватизирована' " +
                                         " WHEN type_sob = '0' THEN 'Не приватизирована' ELSE TRIM(type_sob) END) as type_sob, " +
                                   " (CASE WHEN dok_sv IS NULL OR TRIM(dok_sv) = '-' THEN '' ELSE TRIM(dok_sv) END) || " +
                                   " (CASE WHEN serij_sv IS NULL THEN '' ELSE ' ' || serij_sv END) || " +
                                   " (CASE WHEN nomer_sv IS NULL THEN '' ELSE ' ' || nomer_sv END) || " +
                                   " (CASE WHEN vid_mes_sv IS NULL OR TRIM(vid_mes_sv) = '-' THEN '' ELSE ' ' || TRIM(vid_mes_sv) END) || " +
                                   " (CASE WHEN vid_dat_sv IS NULL THEN '' ELSE ' ' || vid_dat_sv END) AS document,  " +
                                   " TRIM(fam) AS fam, " +
                                   " TRIM(ima) AS ima, " +
                                   " TRIM(otch) AS otch, " +
                                   " (CASE WHEN dolya_up = 0 THEN NULL ELSE dolya_up END) || " +
                                   " (CASE WHEN dolya_down IS NULL OR dolya_down = 0 THEN '' ELSE '/' || dolya_down END) AS dolya, " +
                                   " gil_s, ob_s  " +
                            " FROM t_res t, " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a " +
                            " WHERE a.nzp_area = t.nzp_area " +
                            " ORDER BY 1, 2, 4, idom, 5, ikvar, 6 ";
            DataTable dt;
            try
            {
                dt = ExecSQLToTable(sqlfin);

                var dv = new DataView(dt);
                dt = dv.ToTable();
                RowCount = false;
            }
            catch (Exception)
            {
                dt = ExecSQLToTable(DBManager.SetLimitOffset(sqlfin, 100000, 0));
               
                var dv = new DataView(dt);
                dt = dv.ToTable();
                RowCount = true;
            }
            dt.TableName = "Q_master";

            if (dt.Rows.Count >= 100000)
            {
                if (ReportParams.ExportFormat == ExportFormat.Excel2007)
                {
                    var dtr = dt.Rows.Cast<DataRow>().Skip(40000).ToArray();
                    dtr.ForEach(dt.Rows.Remove);
                }
                else
                {
                    var dtr = dt.Rows.Cast<DataRow>().Skip(100000).ToArray();
                    dtr.ForEach(dt.Rows.Remove);
                }
                RowCount = true;
            }
            else
            {
                RowCount = false;
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds; 
        }

        #region~~~~~~~~~~~~~~~~~~~~~~Фильтр~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Получить условия органичения по банкам</summary>
        private void GetwhereWp()
        {
            string whereWp = String.Empty;
            whereWp = Banks != null
                ? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');

            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point, " +
                                    " bd_kernel " +
                             " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                             " WHERE nzp_wp > 0 " + whereWp;
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

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Создание временных таблиц</summary>
        protected override void CreateTempTable()
        {
            string sql = " create temp table t_kvars( " +
                " nzp_area integer," +         
                " nzp_kvar integer," +
                         " num_ls integer," +
                         " idom integer," +
                         " ikvar integer," +
                         " ndom char(10)," +
                         " nkor char(10)," +
                         " nkvar char(10)," +
                         " nkvar_n char(10)," +
                         " ulicareg char(10)," +
                         " ulica char(60)," +
                         " rajon_t char(60)" +
                         " )";
            ExecSQL(sql);

            ExecSQL("create index ix_t_kvars_01 on t_kvars(nzp_kvar)"); ExecSQL("create index ix_t_kvars_02 on t_kvars(num_ls)");

            sql = " create temp table t_svod( " +
                  " nzp_area integer," +
                  " num_ls integer," +
                  " dat_rog char(20)," +
                  " fam char(60)," +
                  " ima char(60)," +
                  " otch char(60)," +
                  " idom integer," +
                  " nzp_sobstw integer," +
                  " dolya_down integer," +
                  " dolya_up integer," +
                  " ikvar integer," +
                  " ndom char(10)," +
                  " nkor char(10)," +
                  " nkvar char(10)," +
                  " nkvar_n char(10)," +
                  " type_sob char(50)," +
                  " dok_sv char(50)," +
                  " serij_sv char(10)," +
                  " nomer_sv char(15)," +
                  " gil_s  " + DBManager.sDecimalType + "(12,2)," +
                  " ob_s   " + DBManager.sDecimalType + "(12,2)," +
                  " vid_mes_sv char(70)," +
                  " vid_dat_sv date," +
                  " ulicareg char(10)," +
                  " ulica char(60)," +
                  " rajon_t char(60)" +
                  " )";
            ExecSQL(sql);


            sql = " create temp table t_res( " +
                 " nzp_area integer," +
                 " num_ls integer," +
                 " dat_rog char(20)," +
                 " fam char(60)," +
                 " ima char(60)," +
                 " otch char(60)," +
                 " idom integer," +
                 " nzp_sobstw integer," +
                 " dolya_down integer," +
                 " dolya_up integer," +
                 " ikvar integer," +
                 " ndom char(10)," +
                 " nkor char(10)," +
                 " nkvar char(10)," +
                 " nkvar_n char(10)," +
                 " type_sob char(50)," +
                 " dok_sv char(50)," +
                 " serij_sv char(10)," +
                 " nomer_sv char(15)," +
                 " gil_s  "+DBManager.sDecimalType+"(12,2),"  +
                 " ob_s   " + DBManager.sDecimalType+ "(12,2)," +
                 " vid_mes_sv char(70)," +
                 " vid_dat_sv date," +
                 " ulicareg char(10)," +
                 " ulica char(60)," +
                 " rajon_t char(60)" +
                 " )";
            ExecSQL(sql);

        }

        /// <summary>Удаление временных таблиц</summary>
        protected override void DropTempTable()
        {
            try { ExecSQL(" DROP TABLE t_svod; DROP TABLE t_kvars; DROP TABLE t_res;"); }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Список постоянно зарегистрированных в доме' " + e.Message, MonitorLog.typelog.Error, false);
            }
        }

    }
}
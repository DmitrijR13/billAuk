using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report710207 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.2.7 Список собственников"; }
        }

        public override string Description
        {
            get { return "71.2.7 Список собственников"; }
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
            get { return Resources.Report_71_2_7; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


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



        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new BankParameter(),
                new AddressParameter(),
             };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("date", Date);
            report.SetParameterValue("headerParam", TerritoryHeader); 
            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми 70000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }


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
                Houses = String.Join(",", Address.Houses.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());;
                Houses = "and d.nzp_dom in ("+ Houses+ ") ";
            }
        }

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
                    string sql =
                        " INSERT INTO t_kvars (nzp_area, rajon_t, nzp_kvar, num_ls, idom, ndom, nkor, ikvar, nkvar,nkvar_n, ulica, ulicareg) " +
                        " SELECT d.nzp_area,rajon_t,k.nzp_kvar, num_ls, idom, ndom, nkor, ikvar, nkvar,nkvar_n, ulica, ulicareg " +
                        " FROM " + kvarTable + " k, " + domTable + " d, " + ulTable + " u,  " +  rajonTable +" r "+
                        " WHERE  k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj=r.nzp_raj " + Raions + Streets + Houses;
                    ExecSQLToTable(sql);

                    
                    sql =
                        " INSERT INTO t_svod (nzp_area, rajon_t, num_ls, idom, ndom, nkor, ikvar, nkvar,nkvar_n, ulica, ulicareg, fam, ima, otch, dolya_up, dolya_down, dok_sv,  serij_sv, nomer_sv,  vid_mes_sv, vid_dat_sv ) " +
                        " SELECT nzp_area, rajon_t, num_ls, idom, ndom, nkor, ikvar, nkvar,nkvar_n, ulica, ulicareg, fam, ima, otch, d.dolya_up, d.dolya_down, dok_sv, d.serij_sv, d.nomer_sv,  d.vid_mes_sv, d.vid_dat_sv " +
                        " FROM t_kvars kv , " + sobstwTable + " sw left outer join " + docsobstwTable + " d on sw.nzp_sobstw =d.nzp_sobstw  left outer join " + sdocsobstwTable + " sd on d.nzp_dok_sv =sd.nzp_dok_sv " +
                        " WHERE kv.nzp_kvar=sw.nzp_kvar ";
                    ExecSQLToTable(sql);

                    sql =
                        " UPDATE t_svod SET ob_s= (SELECT max(val_prm" + DBManager.sConvToNum + ") " +
                        " FROM " + prmTable + " p " +
                        " WHERE p.nzp_prm=4 " +
                        "  and  p.nzp=  t_svod.num_ls and" +
                        "       p.is_actual<>100 and" +
                        "       p.dat_s <= '" + Date.ToShortDateString() + "' and " +
                        "       p.dat_po >= '" + Date.ToShortDateString() + "')";
                    ExecSQLToTable(sql);

                    sql =
                        " UPDATE t_svod SET ob_s= ob_s * dolya_up / dolya_down "+
                        " WHERE dolya_down <>0 and dolya_down is not null";
                    ExecSQLToTable(sql);

                    sql =
                        " UPDATE t_svod SET gil_s= (SELECT max(val_prm" + DBManager.sConvToNum + ") " +
                        " FROM " + prmTable + " p " +
                        " WHERE p.nzp_prm=6 " +
                        "  and  p.nzp=  t_svod.num_ls and" +
                        "       p.is_actual<>100 and" +
                        "       p.dat_s <= '" + Date.ToShortDateString() + "' and " +
                        "       p.dat_po >= '" + Date.ToShortDateString() + "')";
                    ExecSQLToTable(sql);

                    sql =
                        " UPDATE t_svod SET gil_s= gil_s * dolya_up / dolya_down " +
                        " WHERE dolya_down <>0 and dolya_down is not null";
                    ExecSQLToTable(sql);


                    
                    sql =
                        " UPDATE t_svod SET type_sob= (SELECT MAX(name_y) " +
                        " FROM " + prmTable + " p, " + resTable + " y " +
                        " WHERE p.nzp_prm=2009  and y.nzp_res=3001 and trim(p.val_prm) = nzp_y" +
                        DBManager.sConvToVarChar +
                        "  and  p.nzp=  t_svod.num_ls and" +
                        "       p.is_actual<>100 and" +
                        "       p.dat_s <= '" + Date.ToShortDateString() + "' and " +
                        "       p.dat_po >= '" + Date.ToShortDateString() + "')";
                    ExecSQLToTable(sql);

                    sql =
                        " UPDATE t_svod SET type_sob= (SELECT max(val_prm) " +
                        " FROM " + prmTable + " p " +
                        " WHERE p.nzp_prm=8 and " +
                        "       p.nzp=  t_svod.num_ls and" +
                        "       p.is_actual<>100 and" +
                        "       p.dat_s <= '" + Date.ToShortDateString() + "' and " +
                        "       p.dat_po >= '" + Date.ToShortDateString() + "')" +
                        " WHERE type_sob is null ";
                    ExecSQLToTable(sql);

                    sql =
                        " INSERT INTO t_res SELECT * from t_svod ";
                    ExecSQLToTable(sql);

                    ExecSQLToTable("truncate t_kvars");
                    ExecSQLToTable("truncate t_svod");



                }
            }

            string sqlfin =
                " select area,rajon_t, ulica, ulicareg, idom, ndom, nkor, ikvar, nkvar, nkvar_n, num_ls," +
                " fam, ima, otch, (CASE WHEN type_sob='1' THEN 'Приватизирована' WHEN type_sob='0' THEN 'Не приватизирована' ELSE type_sob END) as type_sob, " +
                " gil_s, ob_s, dolya_up, dolya_down, nomer_sv, dok_sv, serij_sv, vid_mes_sv, vid_dat_sv  " +
                " from t_res t, " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a where a.nzp_area=t.nzp_area order by 1,2,3,4,5,6,7,8 ";
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
                    EnumerableExtension.ForEach(dtr, dt.Rows.Remove);
                }
                else
                {
                    var dtr = dt.Rows.Cast<DataRow>().Skip(100000).ToArray();
                    EnumerableExtension.ForEach(dtr, dt.Rows.Remove);
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


        /// <summary>
        /// Получить условия органичения по банкам
        /// </summary>
        /// <returns></returns>
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
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            //string whereWpRes = !String.IsNullOrEmpty(whereWp) ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s,"
            //           + ReportParams.Pref + DBManager.sDataAliasRest + "kvar kv " +
            //           "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )" : String.Empty;
            return whereWp;
        }

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



        protected override void DropTempTable()
        {
            try { ExecSQL(" drop table t_svod; drop table t_kvars; drop table t_res;"); }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Список постоянно зарегистрированных в доме' " + e.Message, MonitorLog.typelog.Error, false);
            }
        }

    }
}

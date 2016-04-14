using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report7121 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.2.1 Выписка из лицевого счета"; }
        }

        public override string Description
        {
            get { return "Выписка из лицевого счета для Тулы"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Cards};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_2_1; }
        }

        /// <summary>Расчетный месяц</summary>
       // protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
       // protected int Year { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary>Номер лицевого счета</summary>
        protected string NumLs { get; set; }

        /// <summary>Номер лицевого счета</summary>
        protected string WherePlace { get; set; }

        /// <summary>Номер лицевого счета</summary>
        protected string Whom { get; set; }

        /// <summary> Должность паспортистки </summary>
        private string PostPasport { get; set; }

        /// <summary> Наименование РЦ </summary>
        private string ERC { get; set; }

        /// <summary> Признах пустоты таблицы </summary>
        private bool IsEmpty { get; set; }

        public override List<UserParam> GetUserParams()
        {
           // var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                //new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                //new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new StringParameter { Code="Vidana", Name = "Выдана: " },
                new StringParameter { Code="Dana", Name = "Дана для предоставления: " }
            };
        }

        public override DataSet GetData()
        {

            MyDataReader reader;

            
            DataTable dt;
            
            var sql = " select pref " +
                         "from  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                         "where nzp_kvar=" + ReportParams.NzpObject;
            ExecRead(out reader, sql);

            if (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();

                string prefData = pref + DBManager.sDataAliasRest;
                //Квартирные параметры

                sql = " INSERT INTO t_pers_account (ob_s, gil_s, count_room, is_priv, is_komm) " +
                      " SELECT CASE WHEN nzp_prm = 4 then val_prm end as ob_s, " +
                      "        CASE WHEN nzp_prm = 6 then val_prm end as gil_s, " +
                      "        CASE WHEN nzp_prm = 107 then val_prm end as count_room, " +
                      "        CASE WHEN nzp_prm = 8 then 1 end as is_priv, " +
                      "        CASE WHEN nzp_prm = 3 and val_prm = '2' then 1 end as is_komm " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      " WHERE nzp=" + ReportParams.NzpObject +
                      "        AND is_actual=1 " +
                      "        AND dat_s<=" + DBManager.sCurDate +
                      "        AND dat_po>=" + DBManager.sCurDate +
                      "        AND nzp_prm in (3,4,6,8,107)";
                ExecSQL(sql);


                //КОличество зарегистрированных

                sql = " INSERT INTO t_pers_account (pasp_gil_count) " +
                      " SELECT count(distinct nzp_gil) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "kart " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND isactual='1' " +
                      "        AND nzp_tkrt = 1 and nzp_kvar =(select max(nzp) from " +
                      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      "                WHERE nzp_prm=130  and is_actual=1" +
                      "                       AND  nzp=" + ReportParams.NzpObject +
                      "                       AND dat_s<=" + DBManager.sCurDate +
                      "                       AND dat_po>=" + DBManager.sCurDate + ")";
                ExecSQL(sql);



                sql = " INSERT INTO t_pers_account (pasp_gil_count) " +
                      " SELECT val_prm" + DBManager.sConvToInt + " " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p" +
                      " WHERE nzp_prm=5 and p.nzp=" + ReportParams.NzpObject +
                      "        AND p.is_actual=1  " +
                      "        AND p.dat_s<=" + DBManager.sCurDate +
                      "        AND p.dat_po>=" + DBManager.sCurDate +
                      "        AND 0 =(select count(*) from " +
                      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      "                WHERE nzp_prm=130  and is_actual=1" +
                      "                       AND  nzp=" + ReportParams.NzpObject +
                      "                       AND dat_s<=" + DBManager.sCurDate + "" +
                      "                       AND dat_po>=" + DBManager.sCurDate + ")";
                ExecSQL(sql);

                //Количество проживающих
                sql = " INSERT INTO t_pers_account (count_progiv) " +
                      " SELECT (select max(pasp_gil_count) FROM t_pers_account) + " + DBManager.sNvlWord + "(max(val_prm" + DBManager.sConvToInt + "),0) - count(distinct nzp_gilec) " +
                      " FROM " +
                        pref + DBManager.sDataAliasRest + "prm_1 p, " +
                        pref + DBManager.sDataAliasRest + "gil_periods gp " +
                      " WHERE gp.nzp_kvar=" + ReportParams.NzpObject +
                      "        AND gp.is_actual=1 " +
                      "        AND gp.dat_s<=" + DBManager.sCurDate +
                      "        AND gp.dat_po>=" + DBManager.sCurDate + 
                      "        AND gp.nzp_tkrt = 1 " +
                      "        AND gp.nzp_kvar = (select max(nzp) from " +
                      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      "                WHERE nzp_prm=131  and is_actual=1" +
                      "                       AND  nzp=" + ReportParams.NzpObject +
                      "                       AND dat_s<=" + DBManager.sCurDate +
                      "                       AND dat_po>=" + DBManager.sCurDate + ")";
                ExecSQL(sql);

                //Количество собственников

                sql = " INSERT INTO t_pers_account (sobstv_gil_count) " +
                      " SELECT count(*) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "sobstw " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND is_actual=1";
                ExecSQL(sql);


                //Количество зарегистрированных собственников

                sql = " INSERT INTO t_pers_account (sobstv_reg_count) " +
                      " SELECT count(*) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "sobstw a," +
                      pref + DBManager.sDataAliasRest + "kart b" +
                      " WHERE a.nzp_kvar=" + ReportParams.NzpObject +
                      "        AND a.is_actual=1 " +
                      "        AND b.isactual='1' " +
                      "        AND a.fam = b.fam " +
                      "        AND a.ima = b.ima " +
                      "        AND a.otch = b.otch " +
                      "        AND a.dat_rog = b.dat_rog ";
                ExecSQL(sql);



                //Проверяем есть ли наем

                sql = " INSERT INTO t_pers_account (has_naem) " +
                      " SELECT 1 " +
                      " FROM " + pref + DBManager.sDataAliasRest + "tarif " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND is_actual=1 " +
                      "        AND  nzp_serv=15 " +
                      "        AND  dat_s<=" + DBManager.sCurDate +
                      "        AND  dat_po>=" + DBManager.sCurDate;
                ExecSQL(sql);



                sql = " SELECT (case when town ='-' then '' else town end) as town, " +
                      " rajon, ulica, ndom, nkor, nkvar, " +
                      DBManager.sNvlWord + "(nkvar_n,'') as nkvar_n," +
                      " k.nzp_kvar, " + DBManager.sNvlWord + "(fio,'') as fio, " +
                      DBManager.sNvlWord + "(pkod,0) as pkod, num_ls," +
                      " max(" + DBManager.sNvlWord + "(t.ob_s,'')) as ob_s, " +
                      " max(" + DBManager.sNvlWord + "(t.gil_s,'')) as gil_s, " +
                      " max(" + DBManager.sNvlWord + "(t.count_room,'')) as count_room, " +
                      " max(" + DBManager.sNvlWord + "(t.is_priv,0)) as is_priv, " +
                      " max(" + DBManager.sNvlWord + "(t.is_komm,0)) as is_komm, " +
                      " max(" + DBManager.sNvlWord + "(t.has_naem,0)) as has_naem, " +
                      " max(" + DBManager.sNvlWord + "(t.pasp_gil_count,0)) as pasp_gil, " +
                      " max(" + DBManager.sNvlWord + "(t.count_progiv,0)) as count_progiv, " +
                      " max(" + DBManager.sNvlWord + "(t.sobstv_gil_count,0)) as sobstv_gil_count, " +
                      " max(" + DBManager.sNvlWord + "(t.sobstv_reg_count,0)) as sobstv_reg_count " +
                      " FROM " + pref + DBManager.sDataAliasRest + "kvar k, " +
                      "  t_pers_account t, " +
                      pref + DBManager.sDataAliasRest + "dom d, " +
                      pref + DBManager.sDataAliasRest + "s_ulica su, " +
                      pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                      pref + DBManager.sDataAliasRest + "s_town st " +
                      " WHERE k.nzp_dom=d.nzp_dom " +
                      "      AND d.nzp_ul=su.nzp_ul " +
                      "      AND su.nzp_raj=sr.nzp_raj " +
                      "      AND sr.nzp_town=st.nzp_town " +
                      "      AND k.nzp_kvar=" + ReportParams.NzpObject +
                      " group by 1,2,3,4,5,6,7,8,9,10,11 ";
                dt = ExecSQLToTable(sql);

                sql = " INSERT INTO t_report_71_2_1(fam, ima, otch, type_rod, dat_rog, dat_ofor ) " +
                      " SELECT fam, " +
                             " ima, " +
                             " otch, " +
                             " rodstvo AS type_rod, " +
                             " dat_rog, " +
                             " dat_ofor " +
                      " FROM " + prefData + "kart k, " +
                                 prefData + "kvar kv " +
                      " WHERE k.nzp_kvar = kv.nzp_kvar " +
                        " AND kv.num_ls = " + ReportParams.NzpObject +
                        " AND isactual = '1' ";
                ExecSQL(sql);

            }
            else
            {
                dt = new DataTable();
                dt.Columns.Add("ulica", typeof(string));
                dt.Columns.Add("ndom", typeof(string));
                dt.Columns.Add("nkvar", typeof(string));
                dt.Columns.Add("nkor", typeof(string));
                dt.Columns.Add("nkvar_n", typeof(string));
                dt.Columns.Add("fio", typeof(string));
                dt.Columns.Add("pkod", typeof(string));
                dt.Columns.Add("num_ls", typeof(string));
                dt.Columns.Add("ob_s", typeof(string));
                dt.Columns.Add("gil_s", typeof(string));
                dt.Columns.Add("count_room", typeof(string));
                dt.Columns.Add("is_priv", typeof(string));
                dt.Columns.Add("is_komm", typeof(string));
                dt.Columns.Add("has_naem", typeof(string));
                dt.Columns.Add("pasp_gil", typeof(string));
                dt.Columns.Add("count_progiv", typeof(string));
                dt.Columns.Add("sobstv_gil_count", typeof(string));
                dt.Columns.Add("sobstv_reg_count", typeof(string));


            }

            sql = " SELECT TRIM(fam) AS fam, TRIM(ima) AS ima, TRIM(otch) AS otch, TRIM(type_rod) AS type_rod, dat_ofor, dat_rog " +
                  " FROM t_report_71_2_1 " +
                  " ORDER BY 1,2,3 ";
            DataTable gilTable = ExecSQLToTable(sql);
            gilTable.TableName = "gilTable";
            IsEmpty = gilTable.Rows.Count == 0;
            if (gilTable.Rows.Count == 0)
            {
                gilTable.Reset();
                gilTable.Columns.Add("fam",typeof(string));
                gilTable.Columns.Add("ima", typeof(string));
                gilTable.Columns.Add("otch", typeof(string));
                gilTable.Columns.Add("type_rod", typeof(string));
                gilTable.Columns.Add("dat_ofor", typeof(string));
                gilTable.Columns.Add("dat_rog", typeof(string));
                gilTable.Rows.Add(new object[]{ "","","","","",""});
            }

            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 578 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable postTable = ExecSQLToTable(sql);
            if (postTable.Rows.Count != 0)
                PostPasport = postTable.Rows[0]["val_prm"].ToString().TrimEnd();
            
            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 80 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable ercTable = ExecSQLToTable(sql);
            if (ercTable.Rows.Count != 0)
                ERC = ercTable.Rows[0]["val_prm"].ToString().TrimEnd();
            
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(gilTable);

            return ds;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            //report.SetParameterValue("month", Month.ToString("00"));
            //report.SetParameterValue("year", Year);
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            report.SetParameterValue("month", curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month.ToString("00"));
            report.SetParameterValue("year", curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year.ToString("00"));
            //report.SetParameterValue("fio_obrasch", DDL_fio.SelectedItem.Text);
            report.SetParameterValue("fio_obrasch", Whom);
            report.SetParameterValue("number_vip", "_____");
            report.SetParameterValue("date_vipis", DateTime.Now.Date.ToShortDateString());
            report.SetParameterValue("dana", WherePlace);
            report.SetParameterValue("name_pasport", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("post_pasport", PostPasport);
            report.SetParameterValue("erc", ERC);
            report.SetParameterValue("is_empty",IsEmpty);
        }

        protected override void PrepareParams()
        {
            //Month = UserParamValues["Month"].GetValue<int>();
            //Year = UserParamValues["Year"].GetValue<int>();
            WherePlace = UserParamValues["Dana"].GetValue<string>();
            Whom = UserParamValues["Vidana"].GetValue<string>();

            //if (Month == 0)
            //{
            //    throw new ReportException("Не определено значение \"Расчетный месяц\"");
            //}

            //if (Year == 0)
            //{
            //    throw new ReportException("Не определено значение \"Расчетный год\"");
            //}
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_pers_account (     " +
                               " ob_s char(20)," +
                               " gil_s char(20)," +
                               " count_room char(20)," +
                               " is_priv integer," +
                               " is_komm integer," +
                               " has_naem integer," +
                               " pasp_gil_count integer default 0," +
                               " count_progiv integer default 0," +
                               " sobstv_gil_count integer default 0, " +
                               " sobstv_reg_count integer default 0 " +
                               " ) " + DBManager.sUnlogTempTable;

            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_report_71_2_1( " +
                        " fam CHARACTER(40), " +
                        " ima CHARACTER(40), " +
                        " otch CHARACTER(40), " +
                        " type_rod CHARACTER(30), " +
                        " dat_rog DATE, " +
                        " dat_ofor DATE) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_pers_account ", true);
            ExecSQL(" drop table t_report_71_2_1 ");
        }

    }
}


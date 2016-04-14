using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71211 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.2.1.1 Выписка из лицевого счета"; }
        }

        public override string Description
        {
            get { return "Выписка из лицевого счета"; }
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
            get { return Resources.Report_71_2_1_1; }
        }

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

        /// <summary> Имя начальника </summary>
        private string ChiefName { get; set; }
        /// <summary> Имя начальника </summary>
        private string Number { get; set; }
        /// <summary> тип собственности </summary>
        private string TypeSob { get; set; }
        /// <summary> собственник </summary>
        private bool isSob { get; set; }

        public override List<UserParam> GetUserParams()
        {
           // var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
               // new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
              //  new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new StringParameter { Code="Vidana", Name = "Выдана " },
                new StringParameter { Code="Dana", Name = "Дана для предоставления " },
                new StringParameter { Code="Number", Name = "Номер " },
                new StringParameter { Code="Chief", Name = "ФИО начальника " }
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
                      "        CASE WHEN nzp_prm = 3 and val_prm = '2' then 2  end as is_komm " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      " WHERE nzp=" + ReportParams.NzpObject +
                      "        AND is_actual=1 " +
                      "        AND dat_s<=" + DBManager.sCurDate +
                      "        AND dat_po>=" + DBManager.sCurDate +
                      "        AND nzp_prm in (3,4,6,8,107)";
                ExecSQL(sql);

                ExecSQL("INSERT INTO t_pers_account (ob_s) values(0)");

                //КОличество зарегистрированных
                sql = " UPDATE t_pers_account set pasp_gil_count =( " +
                      " SELECT count(distinct nzp_gil) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "kart " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND isactual='1' and dat_oprp is null " +
                      "        AND nzp_tkrt = 1 )";
                ExecSQL(sql);

                //sql = " UPDATE t_pers_account set pasp_gil_count =( " +
                //      " SELECT count(distinct nzp_gil) " +
                //      " FROM " + pref + DBManager.sDataAliasRest + "kart " +
                //      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                //      "        AND isactual='1' and dat_oprp is null" +
                //      "        AND nzp_tkrt = 1 and nzp_kvar =(select max(nzp) from " +
                //      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                //      "                WHERE nzp_prm=130  and is_actual=1" +
                //      "                       AND  nzp=" + ReportParams.NzpObject +
                //      "                       AND dat_s<=" + DBManager.sCurDate +
                //      "                       AND dat_po>=" + DBManager.sCurDate + "))";
                //ExecSQL(sql);



                //sql = " UPDATE t_pers_account SET pasp_gil_count=( " +
                //      " SELECT trim(val_prm)" + DBManager.sConvToInt + " " +
                //      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p" +
                //      " WHERE nzp_prm=5 and p.nzp=" + ReportParams.NzpObject +
                //      "        AND p.is_actual=1  " +
                //      "        AND p.dat_s<=" + DBManager.sCurDate +
                //      "        AND p.dat_po>=" + DBManager.sCurDate +
                //      "        AND 0 =(select count(*) from " +
                //      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                //      "                WHERE nzp_prm=130  and is_actual=1" +
                //      "                       AND  nzp=" + ReportParams.NzpObject +
                //      "                       AND dat_s<=" + DBManager.sCurDate + "" +
                //      "                       AND dat_po>=" + DBManager.sCurDate + "))";
                //ExecSQL(sql);

                ////Количество проживающих
                //sql = " INSERT INTO t_pers_account (count_progiv) " +
                //      " SELECT (select max(pasp_gil_count) FROM t_pers_account) + " + DBManager.sNvlWord + "(max(val_prm" + DBManager.sConvToInt + "),0) - count(distinct nzp_gilec) " +
                //      " FROM " +
                //        pref + DBManager.sDataAliasRest + "prm_1 p, " +
                //        pref + DBManager.sDataAliasRest + "gil_periods gp " +
                //      " WHERE gp.nzp_kvar=" + ReportParams.NzpObject +
                //      "        AND gp.is_actual=1 " +
                //      "        AND gp.dat_s<=" + DBManager.sCurDate +
                //      "        AND gp.dat_po>=" + DBManager.sCurDate + 
                //      "        AND gp.nzp_tkrt = 1 " +
                //      "        AND gp.nzp_kvar = (select max(nzp) from " +
                //      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                //      "                WHERE nzp_prm=131  and is_actual=1" +
                //      "                       AND  nzp=" + ReportParams.NzpObject +
                //      "                       AND dat_s<=" + DBManager.sCurDate +
                //      "                       AND dat_po>=" + DBManager.sCurDate + ")";
                //ExecSQL(sql);

                //Количество собственников

                sql = " UPDATE t_pers_account SET sobstv_gil_count=( " +
                      " SELECT count(*) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "sobstw " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND is_actual=1)";
                ExecSQL(sql);


                //Количество зарегистрированных собственников

                sql = " UPDATE t_pers_account SET sobstv_reg_count =( " +
                      " SELECT count(*) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "sobstw a," +
                      pref + DBManager.sDataAliasRest + "kart b" +
                      " WHERE a.nzp_kvar=" + ReportParams.NzpObject +
                      "        AND a.is_actual=1 " +
                      "        AND b.isactual='1' " +
                      "        AND upper(a.fam) = upper(b.fam) " +
                      "        AND upper(a.ima) = upper(b.ima) " +
                      "        AND b.dat_oprp is null " +
                      "        AND upper(a.otch) = upper(b.otch) " +
                      "        AND a.dat_rog = b.dat_rog )";
                ExecSQL(sql);

                //Проверяем есть ли наем 

                sql = " UPDATE t_pers_account SET has_naem =( " +
                      " SELECT 1 " +
                      " FROM " + pref + DBManager.sDataAliasRest + "tarif " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND is_actual=1 " +
                      "        AND  nzp_serv=15 " +
                      "        AND  dat_s<=" + DBManager.sCurDate +
                      "        AND  dat_po>=" + DBManager.sCurDate + ")";
                ExecSQL(sql);

                sql = " UPDATE t_pers_account SET total_ob_s =( " +
                      " SELECT sum(trim(val_prm)" + DBManager.sConvToNum + ") " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      " WHERE nzp in (select num_ls from " + pref + DBManager.sDataAliasRest +
                      ".kvar where nzp_dom =(select nzp_dom from " + pref + DBManager.sDataAliasRest +
                      ".kvar where nzp_kvar=" + ReportParams.NzpObject + ") and nkvar=(select nkvar from " + pref +
                      DBManager.sDataAliasRest + ".kvar where nzp_kvar=" + ReportParams.NzpObject + "))" +
                      "        AND nzp_prm=4 AND trim(val_prm) <> '' " +
                      "        AND  dat_s<=" + DBManager.sCurDate +
                      "        AND  dat_po>=" + DBManager.sCurDate + ")";
                ExecSQL(sql);


                sql = " UPDATE t_pers_account SET total_gil_s =( " +
                      " SELECT sum(trim(val_prm)" + DBManager.sConvToNum + ") " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      " WHERE nzp in (select num_ls from " + pref + DBManager.sDataAliasRest +
                      ".kvar where nzp_dom =(select nzp_dom from " + pref + DBManager.sDataAliasRest +
                      ".kvar where nzp_kvar=" + ReportParams.NzpObject + ") and nkvar=(select nkvar from " + pref +
                      DBManager.sDataAliasRest + ".kvar where nzp_kvar=" + ReportParams.NzpObject + "))" +
                      "        AND nzp_prm=6 AND trim(val_prm) <> '' " +
                      "        AND  dat_s<=" + DBManager.sCurDate +
                      "        AND  dat_po>=" + DBManager.sCurDate + ")";
                ExecSQL(sql);


                //sql = " SELECT (case when town ='-' then '' else town end) as town, " +
                //      " rajon, ulicareg, ulica, ndom, nkor, nkvar, " +
                //      DBManager.sNvlWord + "(nkvar_n,'') as nkvar_n," +
                //      " k.nzp_kvar, " + DBManager.sNvlWord + "(fio,'') as fio, " +
                //      DBManager.sNvlWord + "(pkod,0) as pkod, num_ls," +
                //      " " + DBManager.sNvlWord + "(t.ob_s,'') as ob_s, " +
                //      " " + DBManager.sNvlWord + "(t.gil_s,'') as gil_s, " +
                //      " " + DBManager.sNvlWord + "(t.count_room,'') as count_room, " +
                //      " " + DBManager.sNvlWord + "(t.is_priv,0) as is_priv, " +
                //      " " + DBManager.sNvlWord + "(t.is_komm,0) as is_komm, " +
                //      " " + DBManager.sNvlWord + "(t.has_naem,0) as has_naem, " +
                //      " " + DBManager.sNvlWord + "(t.pasp_gil_count,0) as pasp_gil, " +
                //      " " + DBManager.sNvlWord + "(t.count_progiv,0) as count_progiv, " +
                //      " " + DBManager.sNvlWord + "(t.sobstv_gil_count,0) as sobstv_gil_count, " +
                //      " " + DBManager.sNvlWord + "(t.sobstv_reg_count,0) as sobstv_reg_count " +
                //      " FROM " + pref + DBManager.sDataAliasRest + "kvar k, " +
                //      "  t_pers_account t, " +
                //      pref + DBManager.sDataAliasRest + "dom d, " +
                //      pref + DBManager.sDataAliasRest + "s_ulica su, " +
                //      pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                //      pref + DBManager.sDataAliasRest + "s_town st " +
                //      " WHERE k.nzp_dom=d.nzp_dom " +
                //      "      AND d.nzp_ul=su.nzp_ul " +
                //      "      AND su.nzp_raj=sr.nzp_raj " +
                //      "      AND sr.nzp_town=st.nzp_town " +
                //      "      AND k.nzp_kvar=" + ReportParams.NzpObject ;

                sql = " SELECT (case when town ='-' then '' else town end) as town, " +
                      " rajon, ulicareg, ulica, ndom, nkor, nkvar, " +
                      DBManager.sNvlWord + "(nkvar_n,'') as nkvar_n," +
                      " k.nzp_kvar, " + DBManager.sNvlWord + "(fio,'') as fio, " +
                      DBManager.sNvlWord + "(pkod,0) as pkod, num_ls," +
                      " max(" + DBManager.sNvlWord + "(t.ob_s,'')) as ob_s, " +
                      " max(" + DBManager.sNvlWord + "(t.gil_s,'')) as gil_s, " +
                      " max(" + DBManager.sNvlWord + "(t.total_ob_s,'')) as total_ob_s, " +
                      " max(" + DBManager.sNvlWord + "(t.total_gil_s,'')) as total_gil_s, " +
                      " max(" + DBManager.sNvlWord + "(t.count_room,'')) as count_room, " +
                      " max(" + DBManager.sNvlWord + "(t.is_priv,0)) as is_priv, " +
                      " max(" + DBManager.sNvlWord + "(t.is_komm,0)) as is_komm, " +
                      " max(" + DBManager.sNvlWord + "(t.has_naem,0)) as has_naem, " +
                      " max(" + DBManager.sNvlWord + "(t.pasp_gil_count,0)) as pasp_gil, " +
                      " max(" + DBManager.sNvlWord + "(t.count_progiv,0)) as count_progiv, " +
                      " max(" + DBManager.sNvlWord + "(t.sobstv_gil_count,0)) as sobstv_gil_count, " +
                      " max(" + DBManager.sNvlWord + "(t.sobstv_reg_count,0)) as sobstv_reg_count " +
                      " FROM t_pers_account t," + pref + DBManager.sDataAliasRest + "kvar k left outer join " +
                      "   " +
                      pref + DBManager.sDataAliasRest + "dom d on  k.nzp_dom=d.nzp_dom left outer join  " +
                      pref + DBManager.sDataAliasRest + "s_ulica su  on d.nzp_ul=su.nzp_ul left outer join  " +
                      pref + DBManager.sDataAliasRest + "s_rajon sr on su.nzp_raj=sr.nzp_raj  left outer join  " +
                      pref + DBManager.sDataAliasRest + "s_town st on sr.nzp_town=st.nzp_town" +
                      " WHERE  k.nzp_kvar=" + ReportParams.NzpObject +
                      " group by 1,2,3,4,5,6,7,8,9,10,11,12 ";
                dt = ExecSQLToTable(sql);

                //sql = " INSERT INTO t_report_71_2_1(fam, ima, otch, type_rod, dat_rog, dat_ofor ) " +
                //      " SELECT fam, " +
                //             " ima, " +
                //             " otch, " +
                //             " rodstvo AS type_rod, " +
                //             " dat_rog, " +                                                                               
                //             " dat_ofor " +
                //      " FROM " + prefData + "kart k, " +
                //                 prefData + "kvar kv " +
                //      " WHERE k.nzp_kvar = kv.nzp_kvar " +
                //        " AND kv.num_ls = " + ReportParams.NzpObject +
                //        " AND isactual = '1' ";
                //ExecSQL(sql);

                MyDataReader rdr;
                sql = " select val_prm,name_y from " + prefData + "prm_1 p," + pref + DBManager.sKernelAliasRest + "res_y y " +
                      " where val_prm=y.nzp_y::char and nzp_res=3017and nzp_prm=1373 " +
                      " and p.dat_s<=" + DBManager.sCurDate + " and dat_po>=" + DBManager.sCurDate + " and is_actual<>100 and nzp =" + ReportParams.NzpObject;
             
               ExecRead(out rdr, sql);
                if (rdr.Read())
                {
                    TypeSob = rdr["val_prm"].ToString().Trim();
                }

                sql = " select 1 as prm from " + prefData + "sobstw s, " + prefData + "kvar k " +
                      " where upper(trim(fam)||' '||trim(ima)||' '||trim(otch))= upper(k.fio) and k.nzp_kvar =" + ReportParams.NzpObject+
                      " and s.nzp_kvar=" + ReportParams.NzpObject;
                
                isSob = false;
                ExecRead(out rdr, sql);
                if (rdr.Read() && !isSob)
                {
                    isSob = rdr["prm"].ToString().Trim()=="1";
                }

            }
            else
            {
                dt = new DataTable();
                dt.Columns.Add("ulica", typeof(string));
                dt.Columns.Add("ulicareg", typeof(string));
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

            //sql = " SELECT TRIM(fam) AS fam, TRIM(ima) AS ima, TRIM(otch) AS otch, TRIM(type_rod) AS type_rod, dat_ofor, dat_rog " +
            //      " FROM t_report_71_2_1 " +
            //      " ORDER BY 1,2,3 ";
            //DataTable gilTable = ExecSQLToTable(sql);
            //gilTable.TableName = "gilTable";
            //IsEmpty = gilTable.Rows.Count == 0;
            //if (gilTable.Rows.Count == 0)
            //{
            //    gilTable.Reset();
            //    gilTable.Columns.Add("fam",typeof(string));
            //    gilTable.Columns.Add("ima", typeof(string));
            //    gilTable.Columns.Add("otch", typeof(string));
            //    gilTable.Columns.Add("type_rod", typeof(string));
            //    gilTable.Columns.Add("dat_ofor", typeof(string));
            //    gilTable.Columns.Add("dat_rog", typeof(string));
            //    gilTable.Rows.Add(new object[]{ "","","","","",""});
            //}

            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 578 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable postTable = ExecSQLToTable(sql);
            if (postTable.Rows.Count != 0)
                PostPasport = postTable.Rows[0]["val_prm"].ToString().TrimEnd();
            
            //sql = " SELECT val_prm" +
            //      " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
            //      " WHERE is_actual = 1" +
            //        " AND nzp_prm = 80 " +
            //        " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
            //        " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            //DataTable ercTable = ExecSQLToTable(sql);
            //if (ercTable.Rows.Count != 0)
            //    ERC = ercTable.Rows[0]["val_prm"].ToString().TrimEnd();

            sql = " SELECT TRIM(val_prm) AS name " +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + " prm_10 " +
                  " WHERE is_actual = 1 " +
                  " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                  " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' " +
                  " AND nzp_prm = 1295";
            DataTable chiefTable = ExecSQLToTable(sql);
            if (chiefTable.Rows.Count != 0)
            {
                ChiefName = chiefTable.Rows[0]["name"].ToString().Trim();
            }

            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
           // ds.Tables.Add(gilTable);

            return ds;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            //report.SetParameterValue("month", Month.ToString("00"));
            //report.SetParameterValue("year", Year);
            //report.SetParameterValue("fio_obrasch", DDL_fio.SelectedItem.Text);
            report.SetParameterValue("fio_obrasch", Whom);
            report.SetParameterValue("number_vip", "_____");
            report.SetParameterValue("date_vipis", DateTime.Now.Date.ToShortDateString());
            report.SetParameterValue("dana", WherePlace);
            report.SetParameterValue("name_pasport", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("post_pasport", PostPasport);
            //report.SetParameterValue("erc", ERC);
            report.SetParameterValue("is_empty",IsEmpty);
            report.SetParameterValue("pDate", DateTime.Now.ToString("dd/MM/yyyy"));
            report.SetParameterValue("name_nach", ChiefName ?? "");
            report.SetParameterValue("type_sob", TypeSob ?? "");
            report.SetParameterValue("isSob", isSob ? "1":"0");
            report.SetParameterValue("pNumber", Number);
        }

        protected override void PrepareParams()
        {
            //Month = UserParamValues["Month"].GetValue<int>();
            //Year = UserParamValues["Year"].GetValue<int>();
            WherePlace = UserParamValues["Dana"].GetValue<string>();
            Whom = UserParamValues["Vidana"].GetValue<string>();
            Number = UserParamValues["Number"].GetValue<string>();
            ChiefName = UserParamValues["Chief"].GetValue<string>();
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_pers_account (     " +
                               " ob_s char(20)," +
                               " gil_s char(20)," +
                               " total_ob_s char(20)," +
                               " total_gil_s char(20)," +
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

            //sql = " CREATE TEMP TABLE t_report_71_2_1( " +
            //            " fam CHARACTER(40), " +
            //            " ima CHARACTER(40), " +
            //            " otch CHARACTER(40), " +
            //            " type_rod CHARACTER(30), " +
            //            " dat_rog DATE, " +
            //            " dat_ofor DATE) " + DBManager.sUnlogTempTable;
            //ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_pers_account ", true);
           // ExecSQL(" drop table t_report_71_2_1 ");
        }

    }
}


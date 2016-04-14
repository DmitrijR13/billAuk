using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Main.Properties;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Main.Reports
{
    public class Unload : BaseSqlReport
    {
        public override string Name
        {
            get { return "Базовый - Результат загрузки файла"; }
        }

        public override string Description
        {
            get { return "Результат загрузки файла"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Unload; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Заголовок отчета</summary>
        private string Calc_month { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string File_name { get; set; }

        /// <summary>Параметры</summary>
        private int File { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new FileNameParameter()
            };
        }

        public override DataSet GetData()
        {
            #region параметры
            string sql =
                " SELECT fh.calc_date as calc_date, fi.pref as pref, trim(fi.loaded_name)||'('||fh.calc_date||')' as file_name " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi," +
                Points.Pref + DBManager.sUploadAliasRest + "file_head fh, " +
                ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                " WHERE fi.nzp_file = fh.nzp_file AND fi.nzp_file =" + File +
                " AND trim(fi.pref) = bd_kernel AND nzp_wp>1 " + GetWhereWp();

           DataTable dt = ExecSQLToTable(sql);

           Calc_month = dt.Rows[0]["calc_date"].ToString();
           File_name = dt.Rows[0]["file_name"].ToString();

           int year = Convert.ToInt32(Calc_month.Substring(6, 4));
           int month = Convert.ToInt32(Calc_month.Substring(3, 2));
            
            string pref = dt.Rows[0]["pref"].ToString().Trim();
            #endregion

            #region Общие показатели Q_mester1

            sql = " INSERT INTO t_general_q_master1" +
                  " (total_sq, kol_ls, kol_komm_kv, kol_odpu, kol_ipu)" +
                  " VALUES " +
                  " (" +
                      " (SELECT SUM(val_prm " + DBManager.sConvToNum + ")" +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1" +
                      " WHERE is_actual <> 100 AND nzp_prm = 4 AND " +
                      " user_del = " + File + " AND dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                      " AND dat_po > '" + Calc_month.Substring(0, 10) + "')," +
                      " (SELECT count(*) FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_kvar" +
                      " WHERE nzp_file = " + File + " and " + DBManager.sNvlWord + "(nzp_kvar,0) > 0)," +
                      " (SELECT count(*)" +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1" +
                      " WHERE is_actual <> 100 AND nzp_prm = 3 AND (val_prm" + DBManager.sConvToInt + ") = 1" +
                      " AND (val_prm " + DBManager.sConvToInt + ") = 2 AND " +
                      " user_del = " + File + " AND dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                      " AND dat_po > '" + Calc_month.Substring(0, 10) + "')," +
                      " (SELECT count(*) FROM " + Points.Pref + DBManager.sDataAliasRest + "counters_spis" +
                      " WHERE nzp_type = 1 AND user_block = " + File + " ), " +
                      " (SELECT count(*) FROM " + Points.Pref + DBManager.sDataAliasRest + "counters_spis" +
                      " WHERE nzp_type = 3 AND user_block = " + File + " ) " +
                  " )";
            ExecSQL(sql);

            sql = " SELECT total_sq, kol_ls, kol_komm_kv, kol_odpu, kol_ipu " +
                  " FROM t_general_q_master1 ";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";
            #endregion

            #region Площади в разрезе поставщик/услуга Q_master2
            //заполняем nzp_supp, nzp_serv
            sql = " INSERT INTO  t_sq_q_master2 (nzp_supp, nzp_serv) " +
                  " SELECT DISTINCT t.nzp_supp,  t.nzp_serv " +
                  " FROM " + pref + DBManager.sDataAliasRest + "tarif t " +
                  " WHERE t.dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                  " AND t.dat_po > '" + Calc_month.Substring(0, 10) + "' " +
                  " AND t.nzp_kvar in " +
                      "(SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_kvar" +
                      " WHERE nzp_file = " + File + " and " + DBManager.sNvlWord + "(nzp_kvar,0) > 0)";
            ExecSQL(sql);

            //заполняем total_sq
            sql = " UPDATE   t_sq_q_master2 set total_sq = " + 
                  " (SELECT " + DBManager.sNvlWord + "(sum(p1.val_prm " + DBManager.sConvToNum + "),0)" +
                  " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p1" +
                  " WHERE p1.is_actual <> 100 and p1.user_del = " + File +
                  " AND p1.nzp_prm = 4 AND p1.dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                  " AND p1.dat_po > '" + Calc_month.Substring(0, 10) + "'" +
                  " AND p1.nzp in " +
                      "(SELECT nzp_kvar FROM " + pref + DBManager.sDataAliasRest + "tarif t" +
                      " WHERE t_sq_q_master2.nzp_supp = t.nzp_supp and t_sq_q_master2.nzp_serv = t.nzp_serv " +
                      " AND dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                      " AND dat_po > '" + Calc_month.Substring(0, 10) + "'))";
            ExecSQL(sql);

            //заполняем total_sq
            sql = " UPDATE   t_sq_q_master2 set otap_sq = " +
                  " (SELECT " + DBManager.sNvlWord + "(sum(p1.val_prm " + DBManager.sConvToNum + "),0)" +
                  " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p1" +
                  " WHERE p1.is_actual <> 100 and p1.user_del = " + File +
                  " AND p1.nzp_prm = 133 AND p1.dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                  " AND p1.dat_po > '" + Calc_month.Substring(0, 10) + "'" +
                  " AND p1.nzp in " +
                      "(SELECT nzp_kvar FROM " + pref + DBManager.sDataAliasRest + "tarif t" +
                      " WHERE t_sq_q_master2.nzp_supp = t.nzp_supp and t_sq_q_master2.nzp_serv = t.nzp_serv " +
                      " AND dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                      " AND dat_po > '" + Calc_month.Substring(0, 10) + "'))";
            ExecSQL(sql);

            sql = " SELECT su.name_supp as supp, se.service as serv , t.total_sq, t.otap_sq " +
                  " FROM t_sq_q_master2 t, " +
                  Points.Pref + DBManager.sKernelAliasRest + "supplier su," +
                  Points.Pref + DBManager.sKernelAliasRest + "services se" +
                  " WHERE su.nzp_supp = t.nzp_supp and se.nzp_serv = t.nzp_serv";
            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";
            #endregion

            #region Формулы Q_master3

            sql = " INSERT INTO t_formul_q_master3(supp, serv, formul)" +
                  " SELECT DISTINCT  su.name_supp as supp,  se.service as serv, t.nzp_frm as formul" +
                  " FROM " + Points.Pref + DBManager.sKernelAliasRest + "supplier su," +
                  Points.Pref + DBManager.sKernelAliasRest + "services se," +
                  pref + DBManager.sDataAliasRest + "tarif t" + 
                  " WHERE t.nzp_supp = su.nzp_supp AND t.nzp_serv = se.nzp_serv" +
                  " AND t.dat_s <= '" + Calc_month.Substring(0, 10) + "' " +
                  " AND t.dat_po > '" + Calc_month.Substring(0, 10) + "'" +
                  " AND t.nzp_kvar in" +
                  " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                  " WHERE nzp_file = " + File + " and " + DBManager.sNvlWord + "(nzp_kvar,0) > 0)";
            ExecSQL(sql);

            sql = " SELECT supp, serv, formul " +
                  " FROM t_formul_q_master3 ";
            DataTable dt3 = ExecSQLToTable(sql);
            dt3.TableName = "Q_master3";
            #endregion

            #region Сводный по услуге  Q_master4
            string tableCharge = pref + "_charge_" + (year - 2000).ToString("00") +
                                 DBManager.tableDelimiter + "charge_" +
                                 month.ToString("00");

            if (TempTableInWebCashe(tableCharge))
            {
                sql = " insert into t_svod_serv_q_master4(nzp_serv, sum_insaldo_k, sum_insaldo_d, " +
                      " sum_insaldo, sum_real, reval, real_charge, sum_money, " +
                      " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                      " select nzp_serv , sum(case when sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k," +
                      " sum(case when sum_insaldo<0 then 0 else sum_insaldo end) as sum_insaldo_d," +
                      " sum(sum_insaldo) as sum_insaldo," +
                      " sum(sum_real) as sum_real," +
                      " sum(reval) as reval," +
                      " sum(real_charge) as real_charge," +
                      " sum(sum_money) as sum_money," +
                      " sum(case when sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_k," +
                      " sum(case when sum_outsaldo<0 then 0 else sum_outsaldo end) as sum_outsaldo_d," +
                      " sum(sum_outsaldo) as sum_outsaldo" +
                      " from " + tableCharge + " a " +
                      " where nzp_serv >1 and dat_charge is null and nzp_kvar in" +
                      " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_kvar" +
                      " WHERE nzp_file = " + File + " and " + DBManager.sNvlWord + "(nzp_kvar,0) > 0)" +
                      " group by 1";
                ExecSQL(sql);
            }

            sql = " select service, sum(sum_insaldo_k) as sum_insaldo_k," +
                  " sum(sum_insaldo_d) as sum_insaldo_d," +
                  " sum(sum_insaldo) as sum_insaldo," +
                  " sum(sum_real) as sum_real," +
                  " sum(reval) as reval," +
                  " sum(real_charge) as real_charge," +
                  " sum(reval) + sum(real_charge) as reval_charge," +
                  " sum(sum_money) as sum_money," +
                  " sum(sum_outsaldo_k) as sum_outsaldo_k," +
                  " sum(sum_outsaldo_d) as sum_outsaldo_d," +
                  " sum(sum_outsaldo) as sum_outsaldo" +
                  " from t_svod_serv_q_master4 a, " +
                  DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "services s " +
                  " where a.nzp_serv=s.nzp_serv" +
                  " group by service " +
                  " order by service ";
            DataTable dt4 = ExecSQLToTable(sql);
            dt4.TableName = "Q_master4";
            #endregion

            #region Сводный по поставщикам Q_master5
            if (TempTableInWebCashe(tableCharge))
            {
                sql = " insert into t_svod_supp_q_master5(nzp_supp, sum_insaldo_k, sum_insaldo_d, " +
                      " sum_insaldo, sum_real, reval, real_charge, sum_money, " +
                      " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                      " select nzp_supp , sum(case when sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k," +
                      " sum(case when sum_insaldo<0 then 0 else sum_insaldo end) as sum_insaldo_d," +
                      " sum(sum_insaldo) as sum_insaldo," +
                      " sum(sum_real) as sum_real," +
                      " sum(reval) as reval," +
                      " sum(real_charge) as real_charge," +
                      " sum(sum_money) as sum_money," +
                      " sum(case when sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_k," +
                      " sum(case when sum_outsaldo<0 then 0 else sum_outsaldo end) as sum_outsaldo_d," +
                      " sum(sum_outsaldo) as sum_outsaldo" +
                      " from " + tableCharge + " a " +
                      " where nzp_serv >1 and dat_charge is null and nzp_kvar in" +
                      " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_kvar" +
                      " WHERE nzp_file = " + File + " and " + DBManager.sNvlWord + "(nzp_kvar,0) > 0)" +
                      " group by 1";
                ExecSQL(sql);
            }

            sql = " select name_supp as supplier, sum(sum_insaldo_k) as sum_insaldo_k," +
                  " sum(sum_insaldo_d) as sum_insaldo_d," +
                  " sum(sum_insaldo) as sum_insaldo," +
                  " sum(sum_real) as sum_real," +
                  " sum(reval) as reval," +
                  " sum(real_charge) as real_charge," +
                  " sum(reval) + sum(real_charge) as reval_charge," +
                  " sum(sum_money) as sum_money," +
                  " sum(sum_outsaldo_k) as sum_outsaldo_k," +
                  " sum(sum_outsaldo_d) as sum_outsaldo_d," +
                  " sum(sum_outsaldo) as sum_outsaldo" +
                  " from t_svod_supp_q_master5 a, " +
                  DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "supplier s " +
                  " where a.nzp_supp=s.nzp_supp" +
                  " group by name_supp " +
                  " order by name_supp ";
            DataTable dt5 = ExecSQLToTable(sql);
            dt5.TableName = "Q_master5";
            #endregion

            var ds = new DataSet();
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            ds.Tables.Add(dt3);
            ds.Tables.Add(dt4);
            ds.Tables.Add(dt5);

            return ds;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("period_month",
                months[Convert.ToInt32(Calc_month.Substring(3, 2))] + " " + Convert.ToInt32(Calc_month.Substring(6, 4)));
            report.SetParameterValue("file_name", File_name);
        }
        
        private string GetWhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareParams()
        {
            File = Convert.ToInt32(UserParamValues["FileName"].Value);
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_general_q_master1(" +
                         " total_sq " + DBManager.sDecimalType + "(14,2)," +
                         " kol_ls INTEGER," +
                         " kol_komm_kv INTEGER," +
                         " kol_odpu INTEGER," +
                         " kol_ipu INTEGER)";

            ExecSQL(sql);

            sql = " create temp table t_sq_q_master2(" +
                         " nzp_supp integer," +
                         " nzp_serv integer," +
                         " total_sq " + DBManager.sDecimalType + "(14,2)," +
                         " otap_sq " + DBManager.sDecimalType + "(14,2))";

            ExecSQL(sql);

            sql = " create temp table t_formul_q_master3(" +
                         " supp CHAR(100)," +
                         " serv CHAR(100)," +
                         " formul INTEGER)";

            ExecSQL(sql);

            sql = " create temp table t_svod_serv_q_master4( " +
                  " nzp_serv integer," +
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_real " + DBManager.sDecimalType + "(14,2)," +
                  " reval " + DBManager.sDecimalType + "(14,2)," +
                  " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                  " real_charge " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_money " + DBManager.sDecimalType + "(14,2))";

            ExecSQL(sql);

            sql = " create temp table t_svod_supp_q_master5( " +
                  " nzp_supp integer," +
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_real " + DBManager.sDecimalType + "(14,2)," +
                  " reval " + DBManager.sDecimalType + "(14,2)," +
                  " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                  " real_charge " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_money " + DBManager.sDecimalType + "(14,2))";

            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_general_q_master1");
            ExecSQL("DROP TABLE t_sq_q_master2");
            ExecSQL("DROP TABLE t_formul_q_master3");
            ExecSQL("DROP TABLE t_svod_serv_q_master4");
            ExecSQL("DROP TABLE t_svod_supp_q_master5");
        }
    }
}

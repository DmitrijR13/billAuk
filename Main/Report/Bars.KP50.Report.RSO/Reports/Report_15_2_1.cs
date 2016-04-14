using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RSO.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report1521 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.2.1 Отчет по лицевому счету"; }
        }

        public override string Description
        {
            get { return "15.2.1 Отчет по лицевому счету"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_15_2_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }


        /// <summary>Лицевой счет</summary>
        protected string PersonalAccount { get; set; }

        /// <summary>ФИО</summary>
        protected string FIO { get; set; }

        /// <summary>Адрес</summary>
        protected string Address { get; set; }

        /// <summary>Номер квартиры</summary>
        protected string Kvar { get; set; }

        /// <summary>Кол-во жильцов</summary>
        protected string NumGil { get; set; }

        /// <summary>Общая площадь</summary>
        protected string ObPl { get; set; }

        /// <summary>Кол-во комнат</summary>
        protected string NumKom { get; set; }

        /// <summary> Основания </summary>
        protected string Osnovanya { get; set; }

        /// <summary> Должность паспортистки </summary>
        private string PostPasport { get; set; }

        /// <summary> Наименование РЦ </summary>
        private string ERC { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>();
        }

        protected override void PrepareParams() {}

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("pers_acc", PersonalAccount);
            report.SetParameterValue("fio", FIO);
            report.SetParameterValue("address", Address);
            report.SetParameterValue("kvar", Kvar);
            report.SetParameterValue("num_gil", NumGil);
            report.SetParameterValue("ob_pl", ObPl);
            report.SetParameterValue("num_kom", NumKom);
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("user", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("post_user", PostPasport);
            report.SetParameterValue("erc", ERC);
            report.SetParameterValue("osnovan",Osnovanya);
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            #region выборка в temp таблицу

            var sql = " SELECT pref " +
                      " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE nzp_kvar = " + ReportParams.NzpObject +
                        " AND nzp_wp > 1 " + GetwhereWp();
            ExecRead(out reader, sql);

            if(reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string prefData = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;

                sql = " INSERT INTO t_othet_lic_chet(nzp_kart, sort_sob, nzp_kvar, fam, ima, otch, dat_rog, dat_ofor, num_ls, type_rod, dok_sv, serij_sv, nomer_sv, vid_mes_sv, vid_dat_sv) " +
                      " SELECT nzp_kart, " +
                             " (CASE WHEN TRIM(TRIM(k.fam)||' '||TRIM(k.ima)||' '||TRIM(k.otch)) = TRIM(fio) THEN 1 ELSE 2 END) AS sort_sob,  " +
                             " k.nzp_kvar, " +
                             //" k.nzp_rod, " +
                             " k.fam, " +
                             " k.ima, " +
                             " k.otch, " +
                             " k.dat_rog, " +
                             " dat_ofor, " +
                             " num_ls, " +
                             " rodstvo AS type_rod," +
                             " dok_sv, serij_sv, nomer_sv, vid_mes_sv, vid_dat_sv " +
                      " FROM " + prefData + "kart k INNER JOIN " + prefData + "kvar kv ON k.nzp_kvar = kv.nzp_kvar " +
                                                  " LEFT OUTER JOIN " + prefData + "sobstw s ON s.nzp_kvar = k.nzp_kvar " +
                                                  " LEFT OUTER JOIN " + prefData + " s_dok_sv d ON d.nzp_dok_sv = s.nzp_dok_sv " +
                      " WHERE kv.nzp_kvar = " + ReportParams.NzpObject;
                ExecSQL(sql);

                sql = " INSERT INTO t_adr (nzp_kvar, town, rajon, ulica, ndom, nkor, nkvar) " +
                      " SELECT kv.nzp_kvar, " + 
                             " town, " +
                                   " rajon, " +
                                   " ulica, " +
                                   " ndom, " +
                                   " nkor, " +
                                   " nkvar " +
                            " FROM " + prefData + "kvar kv, " +
                                       prefData + "dom d, " +
                                       prefData + "s_ulica u, " +
                                       prefData + "s_rajon r, " +
                                       prefData + "s_town t " +
                            " WHERE kv.nzp_dom = d.nzp_dom " +
                              " AND d.nzp_ul = u.nzp_ul " +
                              " AND u.nzp_raj = r.nzp_raj " +
                      " AND r.nzp_town = t.nzp_town ";
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET town = (SELECT town FROM t_adr t WHERE t.nzp_kvar = t_othet_lic_chet.nzp_kvar), " +
                      " rajon = (SELECT rajon FROM t_adr t WHERE t.nzp_kvar = t_othet_lic_chet.nzp_kvar), " +
                      " ulica = (SELECT ulica FROM t_adr t WHERE t.nzp_kvar = t_othet_lic_chet.nzp_kvar), " +
                      " ndom = (SELECT ndom FROM t_adr t WHERE t.nzp_kvar = t_othet_lic_chet.nzp_kvar), " +
                      " nkor = (SELECT nkor FROM t_adr t WHERE t.nzp_kvar = t_othet_lic_chet.nzp_kvar), " +
                      " nkvar = (SELECT nkvar FROM t_adr t WHERE t.nzp_kvar = t_othet_lic_chet.nzp_kvar) ";
                ExecSQL(sql);

                ExecSQL(" DELETE FROM t_adr ");

                sql = " UPDATE t_othet_lic_chet " +
                      " SET ngil = " +
                         " (SELECT val_prm " +
                           " FROM " + prefData + "prm_1 " +
                           " WHERE nzp_prm = 5 " +
                             " AND is_actual<>100 " +
                             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "') ";
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET pl = " +
                         " (SELECT val_prm " +
                           " FROM " + prefData + "prm_1 " +
                           " WHERE nzp_prm = 4 " +
                             " AND is_actual<>100 " +
                             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "') ";
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET sum_kom = " +
                         " (SELECT val_prm " +
                           " FROM " + prefData + "prm_1 " +
                           " WHERE nzp_prm = 107 " +
                             " AND is_actual<>100 " +
                             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "') ";
                ExecSQL(sql);

                //sql = " UPDATE t_othet_lic_chet " +
                //      " SET type_gl = " +
                //         " (SELECT r.name_y " +
                //           " FROM " + prefData + "prm_1 p INNER JOIN " + prefKernel + "prm_name pr ON pr.nzp_prm = p.nzp_prm " +
                //                                        " INNER JOIN " + prefKernel + "res_y r ON (r.nzp_res = pr.nzp_res" +
                //                                                                             " AND r.nzp_y = (CASE WHEN p.val_prm IS NULL THEN 0 ELSE CAST(val_prm AS INTEGER) END)) " +
                //           " WHERE p.nzp_prm = 110 " +
                //             " AND is_actual<>100 " +
                //             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                //             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                //             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "') ";
                //ExecSQL(sql);

                //sql = " UPDATE t_othet_lic_chet " +
                //      " SET type_rod = " +
                //         " (SELECT rod " +
                //           " FROM " + prefData + "s_rod " +
                //           " WHERE nzp_rod = t_othet_lic_chet.nzp_rod) ";
                //ExecSQL(sql);

                sql = " SELECT (CASE WHEN TRIM(fam) IS NULL THEN '' ELSE TRIM(fam) END) || ' ' || " +
                             " (CASE WHEN TRIM(ima) IS NULL THEN '' ELSE TRIM(ima) END) || ' ' || " +
                             " (CASE WHEN TRIM(otch) IS NULL THEN '' ELSE TRIM(otch) END) AS fio " +
                      " FROM " + prefData + "sobstw s INNER JOIN " + prefData + "kvar k ON (k.nzp_kvar = s.nzp_kvar " +
                                                                                      " AND k.nzp_kvar = " + ReportParams.NzpObject + ") ";
                DataTable fioTable = ExecSQLToTable(sql);
                if (fioTable.Rows.Count > 0)
                {
                    FIO = fioTable.Rows[0]["fio"].ToString().Trim();
                }

                sql = " INSERT INTO t_othet_lic_chet_all " +
                      " SELECT * " +
                      " FROM t_othet_lic_chet ";
                ExecSQL(sql);

                sql = " DELETE FROM t_othet_lic_chet ";
                ExecSQL(sql);
            }
            reader.Close();
            #endregion

            sql = " SELECT TRIM(town) || '/' || TRIM(rajon) || ', ул.' || TRIM(ulica) || ', д.' || " +
                         " TRIM(ndom) || (CASE WHEN nkor='-' THEN '' ELSE ', корп.' || TRIM(nkor) END) AS address, " +
                         " (CASE WHEN nkvar='-' THEN '' ELSE TRIM(nkvar) END) AS kvar, ngil, pl, sum_kom, num_ls, " +
                         " TRIM(dok_sv) AS dok_sv, TRIM(serij_sv) AS serij_sv, TRIM(nomer_sv) AS nomer_sv, TRIM(vid_mes_sv) AS vid_mes_sv, vid_dat_sv " +
                  " FROM t_othet_lic_chet_all "; 
            DataTable head = ExecSQLToTable(sql);
            if (head.Rows.Count > 0)
            {
                PersonalAccount = head.Rows[0]["num_ls"].ToString();
                Address = head.Rows[0]["address"].ToString();
                Kvar = head.Rows[0]["kvar"].ToString();
                NumGil = head.Rows[0]["ngil"].ToString().Trim();
                ObPl = head.Rows[0]["pl"].ToString().Trim();
                NumKom = head.Rows[0]["sum_kom"].ToString().Trim();
                if (!string.IsNullOrEmpty(head.Rows[0]["dok_sv"].ToString()))
                {
                    Osnovanya = "Основание: " + head.Rows[0]["dok_sv"];
                    Osnovanya += string.IsNullOrEmpty(head.Rows[0]["serij_sv"].ToString())
                        ? ""
                        : ", " + head.Rows[0]["serij_sv"];
                    Osnovanya += string.IsNullOrEmpty(head.Rows[0]["nomer_sv"].ToString())
                        ? ""
                        : " " + head.Rows[0]["nomer_sv"];
                    Osnovanya += string.IsNullOrEmpty(head.Rows[0]["vid_mes_sv"].ToString())
                        ? ""
                        : ", " + head.Rows[0]["vid_mes_sv"];
                    DateTime vidDatSv;
                    Osnovanya += DateTime.TryParse(head.Rows[0]["vid_dat_sv"].ToString(), out vidDatSv) ? ", " + vidDatSv.ToShortDateString() : "";
                    Osnovanya = Osnovanya.TrimStart(',');
                }
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

            sql = " SELECT DISTINCT TRIM(fam) || ' ' || TRIM(ima) || ' ' || TRIM(otch) AS fio, " +
                         " type_rod, " +
                         " dat_rog, " +
                         " dat_ofor, sort_sob " +
                  " FROM t_othet_lic_chet_all " +
                  " ORDER BY sort_sob, 1 ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        private string GetwhereWp()
        {
            var result = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            return !String.IsNullOrEmpty(result) ? " and nzp_wp in (" + result + ") " : "";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_othet_lic_chet( " +
                            " nzp_kart INTEGER, " +
                            " sort_sob INTEGER, " +  
                            " nzp_kvar INTEGER, " +  
                            " num_ls INTEGER, " +
                            //" nzp_rod INTEGER, " +
                            " fam CHARACTER(40), " +
                            " ima CHARACTER(40), " +
                            " otch CHARACTER(40), " +
                            " town CHARACTER(30), " +
                            " rajon CHARACTER(30), " +
                            " ulica CHARACTER(40), " +
                            " ndom CHARACTER(10), " +
                            " nkor CHARACTER(3), " +
                            " nkvar CHARACTER(10), " +
                            " ngil CHARACTER(20), " +
                            " pl CHARACTER(20), " +
                            " sum_kom CHARACTER(20), " +
                            " dok_sv CHARACTER(30), " +
                            " serij_sv CHARACTER(10), " +
                            " nomer_sv CHARACTER(15), " +
                            " vid_mes_sv CHARACTER(70), " +
                            " vid_dat_sv DATE, " +
                            " type_rod CHARACTER(30), " +
                            " dat_rog DATE, " +
                            " dat_ofor DATE) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_othet_lic_chet_all( " +
                            " nzp_kart INTEGER, " +
                            " sort_sob INTEGER, " + 
                            " nzp_kvar INTEGER, " +
                            " num_ls INTEGER, " +
                            //" nzp_rod INTEGER, " +
                            " fam CHARACTER(40), " +
                            " ima CHARACTER(40), " +
                            " otch CHARACTER(40), " +
                            " town CHARACTER(30), " +
                            " rajon CHARACTER(30), " +
                            " ulica CHARACTER(40), " +
                            " ndom CHARACTER(10), " +
                            " nkor CHARACTER(3), " +
                            " nkvar CHARACTER(10), " +
                            " ngil CHARACTER(20), " +
                            " pl CHARACTER(20), " +
                            " sum_kom CHARACTER(20), " +
                            " dok_sv CHARACTER(30), " +
                            " serij_sv CHARACTER(10), " +
                            " nomer_sv CHARACTER(15), " +
                            " vid_mes_sv CHARACTER(70), " +
                            " vid_dat_sv DATE, " +
                            " type_rod CHARACTER(30), " +
                            " dat_rog DATE, " +
                            " dat_ofor DATE) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            
            sql = " CREATE TEMP TABLE t_adr( " +
                     " nzp_kvar INTEGER, " +
                     " town CHARACTER(30), " +
                     " rajon CHARACTER(30), " +
                     " ulica CHARACTER(40), " +
                     " ndom CHARACTER(10), " +
                     " nkor CHARACTER(3), " +
                     " nkvar CHARACTER(10)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_othet_lic_chet ");
            ExecSQL(" DROP TABLE t_othet_lic_chet_all ");
            ExecSQL(" DROP TABLE t_adr ");
        }
    }
}

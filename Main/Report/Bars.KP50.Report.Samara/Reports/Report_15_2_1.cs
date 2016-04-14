using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Samara.Reports
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
                List<ReportGroup> result = new List<ReportGroup> { ReportGroup.Cards };
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

        public override ReportKind ReportKind
        {
            get { return ReportKind.LC; }
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

        /// <summary>Тип жилья</summary>
        protected string Typegil { get; set; }

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
            report.SetParameterValue("type_gil", Typegil);
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу
            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " WHERE nzp_wp>1 ";
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string prefData = pref + DBManager.sDataAliasRest;

                sql = " INSERT INTO t_othet_lic_chet(nzp_kart, nzp_kvar, nzp_rod, fam, ima, otch, dat_rog, dat_ofor, num_ls) " +
                      " SELECT nzp_kart, " +
                             " k.nzp_kvar, " +
                             " nzp_rod, " +
                             " fam, " +
                             " ima, " +
                             " otch, " +
                             " dat_rog, " +
                             " dat_ofor, " +
                             " num_ls " +
                      " FROM " + prefData + "kart k, " +
                                 prefData + "kvar kv " +
                      " WHERE k.nzp_kvar = kv.nzp_kvar " +
                        " AND kv.num_ls = " + ReportParams.NzpObject;
                ExecSQL(sql);
#if PG
                sql = " UPDATE t_othet_lic_chet " +
                      " SET town = t.town, " +
                          " rajon=r.rajon, " +
                          " ulica = u.ulica, " +
                          " ndom=d.ndom, " +
                          " nkor = d.nkor, " +
                          " nkvar = kv.nkvar " +
                      " FROM " + prefData + "kvar kv, " +
                                 prefData + "dom d, " +
                                 prefData + "s_ulica u, " +
                                 prefData + "s_rajon r, " +
                                 prefData + "s_town t " +
                      " WHERE kv.nzp_dom = d.nzp_dom " +
                        " AND d.nzp_ul = u.nzp_ul " +
                        " AND u.nzp_raj = r.nzp_raj " +
                        " AND r.nzp_town = t.nzp_town " +
                        " AND kv.nzp_kvar = t_othet_lic_chet.nzp_kvar   ";
#else
                sql = " UPDATE t_othet_lic_chet " +
                      " SET (town, rajon, ulica, ndom, nkor, nkvar) = " +
                          " ((SELECT town, " +
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
                              " AND r.nzp_town = t.nzp_town " +
                              " AND kv.nzp_kvar = t_othet_lic_chet.nzp_kvar))   ";
#endif
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET (ngil) = " +
                         " ((SELECT val_prm " +
                           " FROM " + prefData + "prm_1 " +
                           " WHERE nzp_prm = 5 " +
                             " AND is_actual<>100 " +
                             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "')) ";
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET (pl) = " +
                         " ((SELECT val_prm " +
                           " FROM " + prefData + "prm_1 " +
                           " WHERE nzp_prm = 4 " +
                             " AND is_actual<>100 " +
                             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "')) ";
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET (sum_kom) = " +
                         " ((SELECT val_prm " +
                           " FROM " + prefData + "prm_1 " +
                           " WHERE nzp_prm = 107 " +
                             " AND is_actual<>100 " +
                             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "')) ";
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET (type_gl) = " +
                         " ((SELECT val_prm " +
                           " FROM " + prefData + "prm_1 " +
                           " WHERE nzp_prm = 110 " +
                             " AND is_actual<>100 " +
                             " AND nzp = t_othet_lic_chet.nzp_kvar " +
                             " AND dat_s <='" + DateTime.Now.ToShortDateString() + "' " +
                             " AND dat_po>='" + DateTime.Now.ToShortDateString() + "')) ";
                ExecSQL(sql);

                sql = " UPDATE t_othet_lic_chet " +
                      " SET (type_rod) = " +
                         " ((SELECT rod " +
                           " FROM " + prefData + "s_rod " +
                           " WHERE nzp_rod = t_othet_lic_chet.nzp_rod)) ";
                ExecSQL(sql);

                sql = " INSERT INTO t_othet_lic_chet_all " +
                      " SELECT * FROM t_othet_lic_chet ";
                ExecSQL(sql);

                sql = " DELETE FROM t_othet_lic_chet ";
                ExecSQL(sql);
            }
            reader.Close();
            #endregion

            sql = " SELECT TRIM(fam) || ' ' || TRIM(ima) || ' ' || TRIM(otch) AS fio, " +
                         " TRIM(town) || '/' || TRIM(rajon) || ', ул.' || TRIM(ulica) || ', д.' || TRIM(ndom) || (CASE WHEN nkor='-' THEN '' ELSE ', корп.' || TRIM(nkor) END) AS address, " +
                         " (CASE WHEN nkvar='-' THEN '' ELSE TRIM(nkvar) END) AS kvar, ngil, pl, sum_kom, TRIM(type_gl) AS type_gl, num_ls" +
                  " FROM t_othet_lic_chet_all " +
                  " WHERE nzp_rod = (SELECT MIN(nzp_rod) FROM t_othet_lic_chet_all) ";
            DataTable head = ExecSQLToTable(sql);
            if (head.Rows.Count > 0)
            {
                PersonalAccount = head.Rows[0]["num_ls"].ToString();
                FIO = head.Rows[0]["fio"].ToString();
                Address = head.Rows[0]["address"].ToString();
                Kvar = head.Rows[0]["kvar"].ToString();
                NumGil = head.Rows[0]["ngil"].ToString();
                ObPl = head.Rows[0]["pl"].ToString();
                NumKom = head.Rows[0]["sum_kom"].ToString();
                Typegil = head.Rows[0]["type_gl"].ToString();
            }

            sql = " SELECT TRIM(fam) || ' ' || TRIM(ima) || ' ' || TRIM(otch) AS fio, " +
                         " type_rod, " +
                         " dat_rog, " +
                         " dat_ofor " +
                  " FROM t_othet_lic_chet_all " +
                  " ORDER BY nzp_rod";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_othet_lic_chet( " +
                            " nzp_kart INTEGER, " +
                            " nzp_kvar INTEGER, " +
                            " num_ls INTEGER, " +
                            " nzp_rod INTEGER, " +
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
                            " type_gl CHARACTER(60), " +
                            " type_rod CHARACTER(30), " +
                            " dat_rog DATE, " +
                            " dat_ofor DATE) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_othet_lic_chet_all( " +
                            " nzp_kart INTEGER, " +
                            " nzp_kvar INTEGER, " +
                            " num_ls INTEGER, " +
                            " nzp_rod INTEGER, " +
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
                            " type_gl CHARACTER(60), " +
                            " type_rod CHARACTER(30), " +
                            " dat_rog DATE, " +
                            " dat_ofor DATE) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_othet_lic_chet ");
            ExecSQL(" DROP TABLE t_othet_lic_chet_all ");
        }
    }
}

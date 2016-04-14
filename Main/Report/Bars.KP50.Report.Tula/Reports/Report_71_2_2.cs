using System;
using System.Data;
using System.Collections.Generic;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.Tula.Properties;


namespace Bars.KP50.Report.Tula.Reports
{
    class Report7122 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.2.2 Выписка из домовой книги"; }
        }

        public override string Description
        {
            get { return "Выписка из домовой книги"; }
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_2_2; }
        }

        /// <summary> Адрес </summary>
        private string Address { get; set; }

        /// <summary> ЖЭУ </summary>
        private string Geu { get; set; }

        /// <summary> Статус жилья </summary>
        private string IsPrivatize { get; set; }

        /// <summary> Должность паспортиски </summary>
        private string PasportistkaPost { get; set; }

        /// <summary> Должность начальника </summary>
        private string ChiefPost { get; set; }

        /// <summary> Имя начальника </summary>
        private string ChiefName { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>();
        }

        protected override void PrepareParams()
        {}

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("Adres",Address ?? "");
            report.SetParameterValue("num_geu",Geu ?? "");
            report.SetParameterValue("privatiz",IsPrivatize ?? "");
            report.SetParameterValue("date",DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("dolgnost_pasport", PasportistkaPost ?? "");
            report.SetParameterValue("dolgnost_nach",ChiefPost ?? "");
            report.SetParameterValue("fim_pasportist", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("fim_nachPus",ChiefName ?? "");
        }

        public override DataSet GetData()
        {
            MyDataReader reader ;
            DataTable tempTable;

            var sql = " SELECT pref " +
                      " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject;
            ExecRead(out reader, sql);

            if(reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string prefData = pref + DBManager.sDataAliasRest;

                //sql = " INSERT INTO t_report_71_2_2(fam, ima, otch, dat_rog, landp, statp, rajonp, townp, " +
                //                                        " dat_prib, cel, rod, serij, nomer, vid_mes, vid_dat, jobname, jobpost, " +
                //                                                " tprp, dat_prop, dat_ubit, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku ) " +
                //      " SELECT TRIM(fam) AS fam, TRIM(ima) AS ima, TRIM(otch) AS otch, dat_rog, " +
                //             " TRIM(land) AS landp, " +     
                //             " TRIM(stat) AS statp, " +     
                //             " TRIM(rajon) AS rajonp, " +    
                //             " TRIM(town) AS townp, " +
                //             " dat_prib, TRIM(cel) AS cel, TRIM(rod) AS rod, " +
                //             " TRIM(serij) AS serij, TRIM(nomer)AS nomer, TRIM(vid_mes) AS vid_mes, vid_dat, " +
                //             " TRIM(jobname) AS jobname, TRIM(jobpost) AS jobpost, " +
                //             " (CASE TRIM(tprp)  WHEN 'П' THEN 'постоянная'" +
                //                               " WHEN 'В' THEN 'временная' ELSE '' END) AS tprp, " +
                //             " dat_prop, dat_ubit, " +
                //             " nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku " +
                //      " FROM " + prefData + "kart k INNER JOIN " + prefData + "kvar kv ON kv.nzp_kvar=k.nzp_kvar " +
                //                                  " LEFT OUTER JOIN " + prefData + "s_land l ON l.nzp_land = k.nzp_lnop " +
                //                                  " LEFT OUTER JOIN " + prefData + "s_stat s ON s.nzp_stat = k.nzp_stop " +
                //                                  " LEFT OUTER JOIN " + prefData + "s_town t ON t.nzp_town = k.nzp_tnop " +
                //                                  " LEFT OUTER JOIN " + prefData + "s_rajon r ON r.nzp_raj = k.nzp_rnop " +
                //                                  " LEFT OUTER JOIN " + prefData + "s_cel c ON c.nzp_cel = k.nzp_celp " +
                //                                  " LEFT OUTER JOIN " + prefData + "s_rod rd ON rd.nzp_rod = k.nzp_rod" +
                //      " WHERE kv.num_ls =" + ReportParams.NzpObject;
                //ExecSQL(sql);

                sql = " INSERT INTO t_report_71_2_2(fam, ima, otch, dat_rog, landp, statp, rajonp, townp, npunktp, rem_op, " +
                                                      " landu, statu, rajonu, townu, npunktu, rem_ku, " +
                                                        " dat_prib, dat_ubit, cel, rod, serij, nomer, vid_mes, vid_dat, kod_podrazd, jobname, jobpost, " +
                                                                " tprp, dat_prop ) " +
                      " SELECT TRIM(fam) AS fam, TRIM(ima) AS ima, TRIM(otch) AS otch, dat_rog, " +
                             " TRIM(strana_op) AS landp, TRIM(region_op) AS statp,   TRIM(okrug_op) AS rajonp, " +
                             " TRIM(gorod_op) AS townp,  TRIM(npunkt_op) AS npunktp, TRIM(rem_op) AS rem_op, " +
                             " TRIM(strana_ku) AS landu, TRIM(region_ku) AS statu,   TRIM(okrug_ku) AS rajonu, " +
                             " TRIM(gorod_ku) AS townu,  TRIM(npunkt_ku) AS npunktu, TRIM(rem_ku) AS rem_ku, " +
                             " (CASE WHEN k.nzp_tkrt = 1 THEN dat_ofor END) AS dat_prib, " +
                             " (CASE WHEN k.nzp_tkrt = 2 THEN dat_ofor END) AS dat_ubit, " +
                             " TRIM(cel) AS cel, TRIM(rod) AS rod, " +
                             " TRIM(serij) AS serij, TRIM(nomer)AS nomer, TRIM(vid_mes) AS vid_mes, vid_dat, kod_podrazd, " +
                             " TRIM(jobname) AS jobname, TRIM(jobpost) AS jobpost, " +
                             " (CASE TRIM(tprp)  WHEN 'П' THEN 'постоянная'" +
                                               " WHEN 'В' THEN 'временная' ELSE '' END) AS tprp, dat_prop " +
                      " FROM " + prefData + "kart k INNER JOIN " + prefData + "kvar kv ON kv.nzp_kvar=k.nzp_kvar " +
                                                  " LEFT OUTER JOIN " + prefData + "s_cel c ON c.nzp_cel = k.nzp_celp " +
                                                  " LEFT OUTER JOIN " + prefData + "s_rod rd ON rd.nzp_rod = k.nzp_rod" +
                      " WHERE kv.num_ls =" + ReportParams.NzpObject + " AND isactual = '1'";
                ExecSQL(sql);

                sql = " SELECT (CASE WHEN TRIM(rajon)='-' OR rajon IS NULL THEN (CASE WHEN town IS NULL THEN '' ELSE TRIM(town) || ',' END) ELSE TRIM(rajon) || ',' END) ||' ул. '|| " +
                                    " (CASE WHEN ulica IS NULL THEN '' ELSE TRIM(ulica) || ',' END) ||' дом '|| " +
                                    " (CASE WHEN ndom IS NULL THEN '' ELSE TRIM(ndom) END) ||" +
                                    " (CASE WHEN TRIM(nkor)='-' OR TRIM(nkor) IS NULL THEN '' ELSE ', корп. ' || TRIM(nkor) END) || " +
                                    " (CASE WHEN TRIM(nkvar)='-' OR TRIM(nkvar) IS NULL THEN '' ELSE ', кв. ' || TRIM(nkvar) END) AS adres, " +
                                    " geu, " +
                                    " (CASE val_prm WHEN '1' THEN 'приватизировано' " +
                                                  " WHEN '0' THEN 'не приватизировано' " +
                                                  "  ELSE '' END) AS is_stat " +
                      " FROM " + prefData + "kvar kv LEFT OUTER JOIN " + prefData + "dom d ON kv.nzp_dom = d.nzp_dom " +
                                                   " LEFT OUTER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                   " LEFT OUTER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                   " LEFT OUTER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                                                   " LEFT OUTER JOIN " + prefData + "s_geu g ON g.nzp_geu = kv.nzp_geu" +
                                                   " LEFT OUTER JOIN " + prefData + "prm_1 p ON (p.nzp = kv.nzp_kvar" +
                                                                                           " AND p.is_actual = 1 " +
                                                                                           " AND p.dat_s <= '" + DateTime.Now.Day + "." + DateTime.Now.Month + "." + DateTime.Now.Year + "' " +
                                                                                           " AND p.dat_po >= '" + DateTime.Now.Day + "." + DateTime.Now.Month + "." + DateTime.Now.Year + "' " +
                                                                                           " AND p.nzp_prm = 8) " +
                      " WHERE kv.num_ls = " + ReportParams.NzpObject;
                tempTable = ExecSQLToTable(sql);
                foreach (DataRow tempColumn in tempTable.Rows)
                {
                    Address = tempColumn["adres"].ToString().Trim();
                    Geu = tempColumn["geu"].ToString().Trim();
                    IsPrivatize=tempColumn["is_stat"].ToString().Trim();
                }
                tempTable.Reset();

                #region заполнение параметров

                sql = " SELECT TRIM(val_prm) AS post " +
                      " FROM " + prefData + " prm_10 " +
                      " WHERE is_actual = 1 " +
                        " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                        " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' " +
                        " AND nzp_prm = 578 ";
                MyDataReader reader1 ;
                ExecRead(out reader1, sql);
                if (reader1.Read())
                {
                    PasportistkaPost = reader1["post"].ToString().Trim();
                }

                sql = " SELECT TRIM(val_prm) AS post " +
                      " FROM " + prefData + " prm_10 " +
                      " WHERE is_actual = 1 " +
                        " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                        " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' " +
                        " AND nzp_prm = 1292";
                ExecRead(out reader1, sql);
                if (reader1.Read())
                {
                    ChiefPost = reader1["post"].ToString().Trim();
                }

                sql = " SELECT TRIM(val_prm) AS name " +
                      " FROM " + prefData + " prm_10 " +
                      " WHERE is_actual = 1 " +
                        " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                        " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' " +
                        " AND nzp_prm = 1295";
                ExecRead(out reader1, sql);
                if (reader1.Read())
                {
                    ChiefName = reader1["name"].ToString().Trim();
                }
                #endregion
            }

            sql = " SELECT TRIM(fam) AS fam, TRIM(ima) AS ima, TRIM(otch) AS otch, dat_rog, " +
                         " TRIM(landp) AS landp, TRIM(statp) AS statp, TRIM(rajonp) AS rajonp, " +
                            " TRIM(townp) AS townp, TRIM(npunktp) AS npunktp, TRIM(rem_op) AS rem_op, dat_prib, " +
                         " TRIM(cel) AS cel, TRIM(rod) AS rod, " +
                         " serij, nomer, vid_mes, vid_dat, kod_podrazd, " +
                         " TRIM(jobname) AS jobname, TRIM(jobpost) AS jobpost," +
                         " tprp, dat_prop, " +
                         " TRIM(landu) AS landu, TRIM(statu) AS statu, TRIM(rajonu) AS rajonu, " +
                            " TRIM(townu) AS townu, TRIM(npunktu) AS npunktu, TRIM(rem_ku) AS rem_ku, dat_ubit " +
                  " FROM t_report_71_2_2 ";
            tempTable = ExecSQLToTable(sql);
            tempTable.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(tempTable);
            return ds;
        }

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_report_71_2_2( " +
                                " fam CHARACTER(40), " +
                                " ima CHARACTER(40), " +
                                " otch CHARACTER(40), " +
                                " dat_rog DATE, " +
                                " landp CHARACTER(30), " +
                                " statp CHARACTER(30), " +
                                " rajonp CHARACTER(30), " +
                                " townp CHARACTER(30), " +
                                " npunktp CHARACTER(30), " +   //npunktu
                                " rem_op CHARACTER(40), " +
                                " dat_prib DATE, " +
                                " cel CHARACTER(80), " +
                                " rod CHARACTER(30), " +
                                " serij CHARACTER(10), " +
                                " nomer CHARACTER(7), " +
                                " vid_mes CHARACTER(70), " +
                                " vid_dat DATE, " +
                                " kod_podrazd CHARACTER(20), " +  //код подразделения
                                " jobname CHARACTER(40), " +
                                " jobpost CHARACTER(40), " +
                                " tprp CHARACTER(10), " +
                                " dat_prop DATE, " +
                                " dat_ubit DATE, " +
                                " landu CHARACTER(30)," +
                                " statu CHARACTER(30)," +
                                " rajonu CHARACTER(30)," +
                                " townu  CHARACTER(30)," +
                                " npunktu CHARACTER(30)," +
                                " rem_ku CHARACTER(40)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_71_2_2");
        }
    }
}

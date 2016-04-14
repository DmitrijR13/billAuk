using System;
using System.Data;
using System.Collections.Generic;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RSO.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report1524 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.2.4 Выписка из домовой книги"; }
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
            get { return Resources.Report_15_2_4; }
        }

        /// <summary> Адрес </summary>
        private string Address { get; set; }

        /// <summary> Должность паспортиски </summary>
        private string PasportistkaPost { get; set; }

        /// <summary> фио проживающего в квартире </summary>
        private int Fio { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new FioParameter { Require = true }
            };
        }

        protected override void PrepareParams()
        {
            Fio = UserParamValues["FIO"].GetValue<int>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("Adres", Address);
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("dolgnost_pasport", PasportistkaPost);
            report.SetParameterValue("fim_pasportist", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));  
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            DataTable tempTable;

            var sql = " SELECT pref " +
                      " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE num_ls=" + ReportParams.NzpObject;
            ExecRead(out reader, sql);

            if (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string prefData = pref + DBManager.sDataAliasRest;

                sql = " INSERT INTO t_report_15_2_4(nzp_kart, fam, ima, otch, dat_rog, landp, statp, rajonp, townp, npunktp, rem_op, " +
                                                     " landu, statu, rajonu, townu,npunktu, rem_ku, " +
                                                        " dat_prib, cel, serij, nomer, vid_mes, vid_dat, kod_podrazd, " +
                                                                " dat_ubit, dat_pvu, who_pvu, dat_svu ) " +
                      " SELECT nzp_kart, TRIM(fam) AS fam, TRIM(ima) AS ima, TRIM(otch) AS otch, dat_rog, " +
                             " TRIM(strana_op) AS landp, " +
                             " TRIM(region_op) AS statp, " +
                             " TRIM(okrug_op) AS rajonp, " +
                             " TRIM(gorod_op) AS townp, " +
                             " TRIM(npunkt_op) AS npunktp, " +
                             " TRIM(rem_op) AS rem_op, " +
                             " TRIM(strana_ku) AS landu, " +
                             " TRIM(region_ku) AS statu, " +
                             " TRIM(okrug_ku) AS rajonu, " +
                             " TRIM(gorod_ku) AS townu, " +
                             " TRIM(npunkt_ku) AS npunktu, " +
                             " TRIM(rem_ku) AS rem_ku, " +
                             " (CASE WHEN k.nzp_tkrt = 1 THEN dat_ofor END) AS dat_prib, " +
                             " TRIM(cel) AS cel, " +
                             " TRIM(serij) AS serij, " +
                             " TRIM(nomer) AS nomer, " +
                             " TRIM(vid_mes) AS vid_mes, vid_dat, kod_podrazd, " +
                             " (CASE WHEN k.nzp_tkrt = 2 THEN dat_ofor END) AS dat_ubit, dat_pvu, who_pvu, dat_svu " +
                      " FROM " + prefData + "kart k INNER JOIN " + prefData + "kvar kv ON kv.nzp_kvar=k.nzp_kvar " +
                                                  " LEFT OUTER JOIN " + prefData + "s_cel c ON c.nzp_cel = k.nzp_celp " +
                      " WHERE kv.num_ls =" + ReportParams.NzpObject + 
                        " AND isactual = '1' " +
                        " AND k.nzp_kart = " + Fio;
                ExecSQL(sql);

                sql = " UPDATE t_report_15_2_4 " +
                      " SET grgd = ( SELECT grgd " +
                                   " FROM " + prefData + "grgd g INNER JOIN " + prefData + "s_grgd sg ON sg.nzp_grgd = g.nzp_grgd" +
                                   " WHERE g.nzp_kart = t_report_15_2_4.nzp_kart ) ";
                ExecSQL(sql);

                sql = " SELECT (CASE WHEN TRIM(rajon)='-' OR rajon IS NULL THEN (CASE WHEN town IS NULL THEN '' ELSE TRIM(town) || ',' END) ELSE TRIM(rajon) || ',' END) ||' ул. '|| " +
                                    " (CASE WHEN ulica IS NULL THEN '' ELSE TRIM(ulica) || ',' END) ||' дом '|| " +
                                    " (CASE WHEN ndom IS NULL THEN '' ELSE TRIM(ndom) END) ||" +
                                    " (CASE WHEN TRIM(nkor)='-' OR TRIM(nkor) IS NULL THEN '' ELSE ', корп. ' || TRIM(nkor) END) || " +
                                    " (CASE WHEN TRIM(nkvar)='-' OR TRIM(nkvar) IS NULL THEN '' ELSE ', кв. ' || TRIM(nkvar) END) AS adres" +
                      " FROM " + prefData + "kvar kv LEFT OUTER JOIN " + prefData + "dom d ON kv.nzp_dom = d.nzp_dom " +
                                                   " LEFT OUTER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                   " LEFT OUTER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                   " LEFT OUTER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                      " WHERE kv.num_ls = " + ReportParams.NzpObject;
                tempTable = ExecSQLToTable(sql);
                foreach (DataRow tempColumn in tempTable.Rows)
                {
                    Address = tempColumn["adres"].ToString();
                }
                tempTable.Reset();
            }

            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 578 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable postTable = ExecSQLToTable(sql);
            if (postTable.Rows.Count != 0)
                PasportistkaPost = postTable.Rows[0]["val_prm"].ToString().TrimEnd(); 

            sql = " SELECT * " +
                  " FROM t_report_15_2_4 ";
            tempTable = ExecSQLToTable(sql);
            tempTable.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(tempTable);
            return ds;
        }

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_report_15_2_4( " +
                                " nzp_kart INTEGER, " +
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
                                " serij CHARACTER(10), " +
                                " nomer CHARACTER(7), " +
                                " vid_mes CHARACTER(70), " +   //Место удостоверения личности
                                " vid_dat DATE, " +
                                " kod_podrazd CHARACTER(20), " +  //код подразделения
                                " dat_pvu DATE, " +            //Дата постановки на воен. учет
                                " who_pvu CHARACTER(40)," +    //Наименование органа воен. учета
                                " dat_svu DATE, " +            //Дата снятия с  воен. учета
                                " grgd CHARACTER(30), " +      //гражданство
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
            ExecSQL("DROP TABLE t_report_15_2_4");
        }

    }
}

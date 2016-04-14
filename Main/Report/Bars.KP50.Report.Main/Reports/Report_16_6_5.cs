namespace Bars.KP50.Report.Main
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using System.Text;
    using Bars.KP50.Report;
    using Bars.KP50.Report.Base;
    using Bars.KP50.Report.Main.Properties;
    using Bars.KP50.Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Spis_dom : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.6.5 Список по домам поквартирно"; }
        }

        public override string Description
        {
            get { return "Список по домам поквартирно"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Reports);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        /// <summary>Управляющие компании</summary>
        protected int Areas { get; set; }

        /// <summary>Улица</summary>
        protected int Streets { get; set; }

        /// <summary>Дом</summary>
        protected int Dom { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new AreaParameter(false),
                new StreetsParameter(false),
                new StringParameter{ Code="Dom", Name="Номер дома"}
            };
        }

        protected override void PrepareReport(Report report)
        {
            report.SetParameterValue("dat_s", DateTime.Today.AddDays(-DateTime.Today.Day + 1));
            report.SetParameterValue("dat_po", DateTime.Today.AddMonths(1).AddDays(-DateTime.Today.Day));
            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
        }

        protected override byte[] Template
        {
            get { return Resources.Spis_dom; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        protected override void PrepareParams()
        {
            Areas = UserParamValues["Areas"].GetValue<int>();
            Streets = UserParamValues["Streets"].GetValue<int>();
            Dom = UserParamValues["Dom"].GetValue<int>();
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader = new MyDataReader();

            string where_street = "";
            string where_area = "";
            string where_dom = "";

            if (Areas != null)
            {
                where_area = Areas.ToString();
                where_area = "and k.nzp_area in(" + where_area + ") ";
            }

            if (Streets != null)
            {
                where_street = Streets.ToString();
                where_street = "and u.nzp_ul in(" + where_street + ") ";
            } 
            
            if (Dom != null)
            {
                where_dom = Dom.ToString();
                where_dom = "and d.ndom in(" + where_dom + ") ";
            }



            sql.Append(" insert into t_kvar_params (nzp, dat_s, dat_po, ob_s, gil_s, propis, privat) " +
                " select p1.nzp, p1.dat_s, p1.dat_po, p1.val_prm as ob_s, p2.val_prm as gil_as, p3.val_prm as propis, p4.val_prm as privat " +
                " from (select nzp, dat_s, dat_po, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 4) as p1 " +
                " left join (select nzp, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 6) as p2 on (p1.nzp=p2.nzp) " +
                " left join (select nzp, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 5) as p3 on (p2.nzp=p3.nzp) " +
                " left join (select nzp, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 8) as p4 on (p3.nzp=p4.nzp) ");
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" select area, town, rajon, ulica, ndom, nkvar, nkvar_n, fio, p.ob_s, p.gil_s, p.propis, p.privat " +
                " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
                " t_kvar_params p " +
                " where t.nzp_town = r.nzp_town " +
                " and r.nzp_raj = u.nzp_raj " +
                " and u.nzp_ul = d.nzp_ul " +
                " and d.nzp_dom = k.nzp_dom " +
                " and k.nzp_kvar = p.nzp " +
                " and k.nzp_area = a.nzp_area " +
                " and p.dat_s <= " + DBManager.sCurDate +
                " and p.dat_po >= " + DBManager.sCurDate + " " +
                  where_area + where_street + where_dom);

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append("create temp table t_kvar_params ("+
                " nzp integer, " +
                " dat_s date, " +
                " dat_po date, " +
                " ob_s "+ DBManager.sDecimalType + "(14,2), " +
                " gil_s "+ DBManager.sDecimalType + "(14,2), " +
                " propis integer, " +
                " privat integer) " 
                );
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_kvar_params ");
        }

    }
}

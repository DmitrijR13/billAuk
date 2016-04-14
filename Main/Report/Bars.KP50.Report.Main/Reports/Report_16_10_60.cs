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
    class Com_serv_odn : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.10.60 Начисления за коммунальные услуги и ОДН"; }
        }

        public override string Description
        {
            get { return "10.60 Начисления за коммунальные услуги и ОДН"; }
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

        protected override byte[] Template
        {
            get { return Resources._10_60_com_serv_odn; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        /// <summary>Дата с</summary>
        protected DateTime dats { get; set; }

        /// <summary>Дата по</summary>
        protected DateTime datpo { get; set; }




        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter(),
                new YearParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            StringBuilder sql = new StringBuilder();



            #region Ограничения

            #endregion


            sql.Remove(0, sql.Length);
            sql.Append(" insert into prev (num_ls ,ulica, ndom, nkor) " +
                " SELECT num_ls ,ulica, ndom, nkor " +
                " from " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                " where d.nzp_ul = u.nzp_ul and k.nzp_dom = d.nzp_dom" + 
                " group by 1,2,3,4 ");
            ExecSQL(sql.ToString());
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 ");
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_svod " +
                        " select ulica, ndom, nkor, count(c.num_ls) as kol_ls, " +
                        " sum(case when nzp_serv = 6 then sum_tarif else 0 end)as hv, " +
                        " sum(case when nzp_serv = 7 then sum_tarif else 0 end)as canal, " +
                        " sum(case when nzp_serv = 8 then sum_tarif else 0 end)as otopl, " +
                        " sum(case when nzp_serv = 9 then sum_tarif else 0 end)as gv, " +
                        " sum(case when nzp_serv = 14 then sum_tarif else 0 end)as hv_gvs, " +
                        " sum(case when nzp_serv = 25 then sum_tarif else 0 end)as el, " +
                        " sum(case when nzp_serv = 210 then sum_tarif else 0 end)as el_n, " +
                        " sum(case when nzp_serv = 510 then sum_tarif else 0 end)as hv_odn, " +
                        " sum(case when nzp_serv = 511 then sum_tarif else 0 end)as canal_odn, " +
                        " sum(case when nzp_serv = 512 then sum_tarif else 0 end)as otopl_odn, " +
                        " sum(case when nzp_serv = 513 then sum_tarif else 0 end)as gv_odn, " +
                        " sum(case when nzp_serv = 514 then sum_tarif else 0 end)as hv_gvs_odn, " +
                        " sum(case when nzp_serv = 515 then sum_tarif else 0 end)as el_odn, " +
                        " sum(case when nzp_serv = 516 then sum_tarif else 0 end)as  el_n_odn " +
                        " from " + pref + "_charge_" + (dats.Year-2000).ToString("00") + DBManager.tableDelimiter + "charge_" + dats.Month.ToString("00") + " c, " +
                        " prev p, " + pref + DBManager.sDataAliasRest + "prm_3 p3 " +
                        " where c.num_ls=p.num_ls and dat_charge is null " +
                        " and nzp_serv in (6,7,8,9,14,25,210,510,511,512,513,514,515,516) " +
                        " and p3.nzp_prm = 51 and p3.is_actual<>100 " +
                        " and p3.nzp=c.num_ls and p3.val_prm=1  " +
                        " and p3.dat_s<='" + dats.ToShortDateString() + "' and p3.dat_po>='" + datpo.ToShortDateString() + "'" +
                        " group by 1,2,3 ");

                    ExecSQL(sql.ToString());

            }
            reader.Close();
            DataTable dt = ExecSQLToTable(" select * from t_svod order by 1,2,3 ");
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            //report.SetParameterValue("month", dats.Month);
            //report.SetParameterValue("year", dats.Year);
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
        }

        protected override void PrepareParams()
        {
            dats = Convert.ToDateTime("01." + UserParamValues["Month"].GetValue<int>().ToString("00") + "." + UserParamValues["Year"].GetValue<int>().ToString());
            datpo = dats.AddMonths(1).AddDays(-1);
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table prev(" +
                " num_ls integer, " +
                " ulica character(40), " +
                " ndom character(10), " +
                " nkor character(3)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = " create temp table t_svod(" +
                " ulica character(40), " +
                " ndom character(10), " +
                " nkor character(3), " +
                " kol_ls integer, " +
                " hv " + DBManager.sDecimalType + "(14,2), " +
                " canal " + DBManager.sDecimalType + "(14,2), " +
                " otopl " + DBManager.sDecimalType + "(14,2), " +
                " gv " + DBManager.sDecimalType + "(14,2), " +
                " hv_gvs " + DBManager.sDecimalType + "(14,2), " +
                " el " + DBManager.sDecimalType + "(14,2), " +
                " el_n " + DBManager.sDecimalType + "(14,2), " +
                " hv_odn " + DBManager.sDecimalType + "(14,2), " +
                " canal_odn " + DBManager.sDecimalType + "(14,2), " +
                " otopl_odn " + DBManager.sDecimalType + "(14,2), " +
                " gv_odn " + DBManager.sDecimalType + "(14,2), " +
                " hv_gvs_odn " + DBManager.sDecimalType + "(14,2), " +
                " el_odn " + DBManager.sDecimalType + "(14,2), " +
                " el_n_odn " + DBManager.sDecimalType + "(14,2) " +
                " ) "+DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table prev; drop table t_svod; ", true);
        }

    }
}

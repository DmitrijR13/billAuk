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
    class Money_moving : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.10.3.3Ф Движение денежных средств (арендаторы) по аналитическому счету"; }
        }

        public override string Description
        {
            get { return "10.3.3Ф Движение денежных средств (арендаторы) по аналитическому счету"; }
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
            get { return Resources._10_3_3_F_Money_moving; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        /// <summary> с расчетного дня </summary>
        protected DateTime dat_s { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime dat_po { get; set; }




        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new PeriodParameter(DateTime.Now,DateTime.Now)
                //new AreaParameter(),
                //new SupplierParameter(),
                //new ServiceParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            StringBuilder sql = new StringBuilder();
            //string where_supp = "";
            //string where_area = "";
            //string where_serv = "";


            #region Ограничения
            //if (Suppliers != null)
            //{
            //    foreach (int nzp_supp in Suppliers)
            //        where_supp += nzp_supp.ToString() + ",";
            //    where_supp = where_supp.TrimEnd(',');
            //}

            //if (!String.IsNullOrEmpty(where_supp))
            //{
            //    where_supp = " and nzp_supp in(" + where_supp + ")";
            //    //Поставщики
            //    sql.Remove(0, sql.Length);
            //    sql.Append(" SELECT name_supp from ");
            //    sql.Append(ReportParams.Pref + DBManager.sKernelAliasRest + "supplier ");
            //    sql.Append(" WHERE nzp_supp > 0 " + where_supp);
            //    DataTable supp = ExecSQLToTable(sql.ToString());
            //    foreach (DataRow dr in supp.Rows)
            //    {
            //        SuppliersHeader += dr["name_supp"].ToString().Trim() + ",";
            //    }
            //    SuppliersHeader = SuppliersHeader.TrimEnd(',');
            //}
            //if (Services != null)
            //{
            //    foreach (int nzp_serv in Services)
            //        where_serv += nzp_serv.ToString() + ",";
            //    where_serv = where_serv.TrimEnd(',');
            //}

            //if (!String.IsNullOrEmpty(where_serv))
            //{
            //    where_serv = " and nzp_serv in(" + where_serv + ")";
            //    //Услуги
            //    sql.Remove(0, sql.Length);
            //    sql.Append(" SELECT service from ");
            //    sql.Append(ReportParams.Pref + DBManager.sKernelAliasRest + "services ");
            //    sql.Append(" WHERE nzp_serv > 0 " + where_serv);
            //    DataTable serv = ExecSQLToTable(sql.ToString());
            //    foreach (DataRow dr in serv.Rows)
            //    {
            //        ServicesHeader += dr["service"].ToString().Trim() + ",";
            //    }
            //    ServicesHeader = ServicesHeader.TrimEnd(',');
            //}
            //if (Areas != null)
            //{
            //    foreach (int nzp_area in Areas)
            //        where_area += nzp_area.ToString() + ",";
            //    where_area = where_area.TrimEnd(',');
            //}

            //if (!String.IsNullOrEmpty(where_area))
            //{
            //    //УК
            //    sql.Remove(0, sql.Length);
            //    sql.Append(" SELECT area from ");
            //    sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_area ");
            //    sql.Append(" WHERE nzp_area > 0 and nzp_area in(" + where_area + ") ");
            //    DataTable area = ExecSQLToTable(sql.ToString());
            //    foreach (DataRow dr in area.Rows)
            //    {
            //        AreasHeader += dr["area"].ToString().Trim() + ",";
            //    }
            //    AreasHeader = AreasHeader.TrimEnd(',');
            //    where_area = "and k.nzp_area in(" + where_area + ") ";
            //}


            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_svod (nzp_serv, sum_in) " +
                " select (case when nzp_serv=210 then 25 else nzp_serv end),sum(sum_in) " +
                " from " + ReportParams.Pref + "_fin_" + (dat_s.Year-2000).ToString("00") + DBManager.tableDelimiter + "fn_distrib_dom_" + dat_s.Month.ToString("00") + 
                " where dat_oper = '" + dat_s.ToShortDateString() + "' " +
                " and nzp_payer<>1997 " +
                " group by 1 ");
            ExecSQL(sql.ToString()); 
            
            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_svod (nzp_serv, sum_out) " +
                " select (case when nzp_serv=210 then 25 else nzp_serv end),sum(sum_out) " +
                " from " + ReportParams.Pref + "_fin_" + (dat_po.Year - 2000).ToString("00") + DBManager.tableDelimiter + "fn_distrib_dom_" + dat_po.Month.ToString("00") +
                " where dat_oper = '" + dat_po.ToShortDateString() + "' " +
                " and nzp_payer<>1997 " +
                " group by 1 ");
            ExecSQL(sql.ToString());


            for (int i = dat_s.Year * 12 + dat_s.Month; i < dat_po.Year * 12 + dat_po.Month + 1; i++)
            {
                int year = i / 12;
                int month = i % 12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_svod (nzp_serv,sum_zach,sum_send, sum_v, sum_izm) " +
                    " select (case when nzp_serv=210 then 25 else nzp_serv end), " +
                    " sum(sum_charge),  " +
                    " sum(case when sum_send>0 then sum_send else 0 end), " +
                    " sum(case when sum_send<0 then sum_send else 0 end), " +
                    " sum(sum_reval) " +
                    " from " + ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "fn_distrib_dom_" + month.ToString("00") +
                    " where dat_oper>= '" + dat_s.ToShortDateString() + "' " +
                    " and dat_oper<= '" + dat_po.ToShortDateString() + "' " +
                    " and nzp_payer<>1997 " +
                    " group by 1 ");
                ExecSQL(sql.ToString());
            }
            
            DataTable dt = ExecSQLToTable(
                  " select service, sum(sum_in) as sum_in ,sum(sum_zach) as sum_zach ,sum(sum_send) as sum_send ,sum(sum_v) as sum_v ,sum(sum_izm) as sum_izm, sum(sum_out) as sum_out " +
                  " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services se left outer join t_svod t on(se.nzp_serv = t.nzp_serv)  " +
                  " group by 1 " +
                  " order by 1 ");
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            string[] months = new string[] { "","Январь", "Февраль", "Март",
            "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
            "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("dats", dat_s.ToShortDateString());
            report.SetParameterValue("datpo", dat_po.ToShortDateString());
        }

        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            dat_s = d1;
            dat_po = d2;
            //Services = UserParamValues["Services"].GetValue<List<int>>();
            //Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
            //Areas = UserParamValues["Areas"].GetValue<List<int>>();

        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod(" +
            " nzp_serv integer, " +
            " sum_in " + DBManager.sDecimalType + "(14,2), " +
            " sum_zach " + DBManager.sDecimalType + "(14,2), " +
            " sum_send " + DBManager.sDecimalType + "(14,2), " +
            " sum_v " + DBManager.sDecimalType + "(14,2), " +
            " sum_izm " + DBManager.sDecimalType + "(14,2), " +
            " sum_out " + DBManager.sDecimalType + "(14,2)) " +
            DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}

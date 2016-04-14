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
    class Incom_status_F : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.10.4Ф Состояние поступлений"; }
        }

        public override string Description
        {
            get { return "10.4Ф Состояние поступлений"; }
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
            get { return Resources._10_4_F_incom_status; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        /// <summary>Месяц</summary>
        protected int Month { get; set; }

        /// <summary>Год</summary>
        protected int Year { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Услуги</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Поставщики</summary>
        protected string SuppliersHeader { get; set; }

        /// <summary>Управляющие компании</summary>
        protected string AreasHeader { get; set; }



        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter{ Value = DateTime.Today.Month },
                new YearParameter{ Value = DateTime.Today.Year },
                new AreaParameter(),
                new SupplierParameter(),
                new ServiceParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            StringBuilder sql = new StringBuilder();
            string where_supp = "";
            string where_area = "";
            string where_serv = "";
            string where_geu = "";


            #region Ограничения
            if (Suppliers != null)
            {
                foreach (int nzp_supp in Suppliers)
                    where_supp += nzp_supp.ToString() + ",";
                where_supp = where_supp.TrimEnd(',');
            }

            if (!String.IsNullOrEmpty(where_supp))
            {
                where_supp = " and nzp_supp in(" + where_supp + ")";
                //Поставщики
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT name_supp from ");
                sql.Append(ReportParams.Pref + DBManager.sKernelAliasRest + "supplier ");
                sql.Append(" WHERE nzp_supp > 0 " + where_supp);
                DataTable supp = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',');
            }
            if (Services != null)
            {
                foreach (int nzp_serv in Services)
                    where_serv += nzp_serv.ToString() + ",";
                where_serv = where_serv.TrimEnd(',');
            }

            if (!String.IsNullOrEmpty(where_serv))
            {
                where_serv = " and nzp_serv in(" + where_serv + ")";
                //Услуги
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT service from ");
                sql.Append(ReportParams.Pref + DBManager.sKernelAliasRest + "services ");
                sql.Append(" WHERE nzp_serv > 0 " + where_serv);
                DataTable serv = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ",";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',');
            }
            if (Areas != null)
            {
                foreach (int nzp_area in Areas)
                    where_area += nzp_area.ToString() + ",";
                where_area = where_area.TrimEnd(',');
            }

            if (!String.IsNullOrEmpty(where_area))
            {
                //УК
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT area from ");
                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_area ");
                sql.Append(" WHERE nzp_area > 0 and nzp_area in(" + where_area + ") ");
                DataTable area = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ",";
                }
                AreasHeader = AreasHeader.TrimEnd(',');
                where_area = "and k.nzp_area in(" + where_area + ") ";
            }


            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 ");
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_svod () " +
                       " select  " +                     
                         where_area + where_serv + where_supp + where_geu +
                       " group by  ");

                ExecSQL(sql.ToString());
            }
            reader.Close();
            DataTable dt = ExecSQLToTable(
                  " ");
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod(" +
            " nzp_area integer, " +
            " sum_outsaldo " + DBManager.sDecimalType + "(14,2)" + 
            " ) " +
            DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}

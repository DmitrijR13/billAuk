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
    class Saldo_nach_supp : BaseSqlReport
    {
        public override string Name
        {
            get { return "10.14.1 Сальдовая оборотная ведомость начислений и оплат по услугам в разрезе поставщиков"; }
        }

        public override string Description
        {
            get { return "10.14.1 Сальдовая оборотная ведомость начислений и оплат по услугам в разрезе поставщиков"; }
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
            get { return Resources._10_14_1_Saldo_nach_supp; }
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
                sql.Append(" insert into t_svod (nzp_area,nzp_geu,nzp_serv,nzp_supp,sum_insaldo,sum_2,rsum_lgota,sum_real,sum_5,sum_outsaldo,sum_nedop,sum_nedop_p, "
                     + " sum_money, money_from, money_del) "
                     + " select nzp_area, nzp_geu, (case when nzp_serv=210 then 25 else nzp_serv end), nzp_supp, "
                     + " sum(sum_insaldo) as sum_insaldo, "
                     + " sum(sum_tarif+sum_nedop+sum_lgota-rsum_lgota)    as sum_2, "
                     + " sum(rsum_lgota) as rsum_lgota, "
                     + " sum(sum_real) as sum_real, "
                     + " sum(nvl(real_charge,0)+nvl(reval,0)+sum_nedop_p) as sum_5, "
                     + " sum(sum_outsaldo) as sum_outsaldo, sum(sum_nedop+sum_lgota-rsum_lgota) as sum_nedop, "
                     + " sum(sum_nedop_p) as sum_last_nedop, "
                     + " sum(sum_money) as sum_money, "
                     + " sum(money_from) as money_from, "
                     + " sum(money_del) as money_del "
                     + " from " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00") + " c "
                     + " left outer join " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k on (k.nzp_kvar = c.nzp_kvar) " 
                     + " where nzp_serv>1 " 
                     +   where_area + where_serv + where_supp + where_geu +
                       " group by 1,2,3,4 ");

                ExecSQL(sql.ToString());
            }
            reader.Close();
            DataTable dt = ExecSQLToTable(
                  " select area, service, name_supp, " +
                  " sum(sum_insaldo) as sum_insaldo ,sum(sum_2) as sum_2 ,sum(rsum_lgota) as rsum_lgota ,sum(sum_real) as sum_real ,sum(sum_5) as sum_5, " +
                  " sum(sum_outsaldo) as sum_outsaldo, sum(sum_nedop) as sum_nedop ,sum(sum_nedop_p) as sum_nedop_p, " +
                  " sum(sum_money) as sum_money, sum(money_from) as money_from, sum(money_del)  as money_del " +
                  " from t_svod t, " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " + 
                    ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su " + 
                  " where t.nzp_area = a.nzp_area and t.nzp_serv = se.nzp_serv and t.nzp_supp = su.nzp_supp " +
                  " group by 1,2,3 ");
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
            string[] months2 = new string[] { "","Январе", "Феврале", "Марте",
            "Апреле", "Мае", "Июне", "Июле", "Августе", "Сентябре",
            "Октябре","Ноябре","Декабре"};
            int prevYear = Year;
            int prevMonth = Month - 1;
            if (prevMonth == 0) { prevYear -= 1; prevMonth = 12; } 
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("supp", SuppliersHeader);
            report.SetParameterValue("serv", ServicesHeader);
            report.SetParameterValue("month", months2[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("month1", months[prevMonth]);
            report.SetParameterValue("year1", prevYear);
            report.SetParameterValue("month2", months[Month]);
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
            " nzp_geu integer, " +
            " nzp_serv integer, " +
            " nzp_supp integer, " +
            " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
            " sum_2 " + DBManager.sDecimalType + "(14,2), " +
            " rsum_lgota " + DBManager.sDecimalType + "(14,2), " +
            " sum_real " + DBManager.sDecimalType + "(14,2), " +
            " sum_5 " + DBManager.sDecimalType + "(14,2), " +
            " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
            " sum_nedop_p " + DBManager.sDecimalType + "(14,2), " +
            " sum_money " + DBManager.sDecimalType + "(14,2), " +
            " money_from " + DBManager.sDecimalType + "(14,2), " +
            " money_del " + DBManager.sDecimalType + "(14,2), " +
            " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " +
            DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}

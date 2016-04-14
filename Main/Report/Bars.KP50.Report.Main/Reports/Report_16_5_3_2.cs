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
    class SaldoDom : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.5.3.2. Сальдовая ведомость по домам"; }
        }

        public override string Description
        {
            get { return "5.3.2. Сальдовая ведомость по домам"; }
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
            get { return Resources.SaldoDom; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Список Поставщиков в заголовке</summary>
        protected string SuppliersHeader { get; set; }

        /// <summary>Список балансодержателей (Управляющих компаний)</summary>
        protected string AreasHeader { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter{ Value = DateTime.Now.Month },
                new YearParameter{ Value = DateTime.Now.Year },
                new SupplierParameter(),
                new AreaParameter(),
                new ServiceParameter()              
            };
        }

        public override DataSet GetData()
        {
            StringBuilder sql = new StringBuilder();

            string where_supp = "";
            string where_serv = "";
            string where_area = "";



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
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',',' ');


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
                    ServicesHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',',' ');


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
                    AreasHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreasHeader = AreasHeader.TrimEnd(',',' ');
                where_area = "and k.nzp_area in(" + where_area + ") ";


            }


            #endregion

            #region Выборка по локальным банкам

            MyDataReader reader = new MyDataReader();
            sql.Remove(0, sql.Length);
            sql.Append(" select bd_kernel as pref ");
            sql.AppendFormat(" from {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" where nzp_wp>1 ");

            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_saldoDom (town, ulica, ndom, nkor, nkvar, sum_insaldo, rsum_tarif, sum_nedop, sum_nedop_p, sum_lgota, " +
                    " sum_real, real_charge, reval_nedop, sum_money, sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo, out_real, percent) " +
                    " select town, ulica, ndom, (case when nkor<>'-' then nkor end) as nkor,(case when ikvar<>0 then 'кв.'||ikvar end) as nkvar, " +
                    " sum(sum_insaldo) as sum_insaldo, " +
                    " sum(rsum_tarif) as rsum_tarif, " +
                    " sum(sum_nedop) as sum_nedop, " +
                    " sum(sum_nedop_p) as sum_nedop_p, " +
                    " sum(sum_lgota) as sum_lgota, " +
                    " sum(sum_real) as sum_real, " +
                    " sum(real_charge) as real_charge, " +
                    " sum(reval+sum_nedop_p) as reval_nedop, " +
                    " sum(sum_money) as sum_money, " +
                    " sum(CASE WHEN sum_outsaldo>0 then sum_outsaldo else 0 end) as sum_outsaldo_k, " +
                    " sum(CASE WHEN sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_d, " +
                    " sum(sum_outsaldo) as sum_outsaldo, " +
                    " sum((CASE WHEN sum_outsaldo>0 then sum_outsaldo else 0 end)-sum_real) as out_real, " +
                    " case when sum_real=0 then 0 else round((((CASE WHEN sum_outsaldo>0 then sum_outsaldo else 0 end)-sum_real)/sum_real)*100) end as percent " +
                    " from " + ReportParams.Pref + DBManager.sDataAliasRest + " kvar k, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + " dom d, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + " s_ulica u, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + " s_rajon r, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + " s_town t, " +
                      pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00") + " c " +
                    " where k.nzp_dom = d.nzp_dom " +
                    " and d.nzp_ul = u.nzp_ul " +
                    " and r.nzp_raj = u.nzp_raj " +
                    " and t.nzp_town = r.nzp_town " +
                    " and k.nzp_kvar = c.nzp_kvar " +
                    " and c.dat_charge is null " +
                    " and c.nzp_serv > 1 " +
                      where_serv + where_supp + where_area +
                    " group by 1,2,3,4,5,19 ");
                ExecSQL(sql.ToString());
            }

            reader.Close();

            #endregion

            #region Выборка на экран

            sql.Remove(0, sql.Length);
            sql.Append(" select town, ulica, ndom, nkor, nkvar, " +
                "sum(sum_insaldo) as sum_insaldo," +
                "sum(rsum_tarif) as rsum_tarif," +
                "sum(sum_nedop) as sum_nedop," +
                "sum(sum_nedop_p) as sum_nedop_p," +
                "sum(sum_lgota) as sum_lgota," +
                "sum(sum_real) as sum_real," +
                "sum(real_charge) as real_charge," +
                "sum(reval_nedop) as reval_nedop," +
                "sum(sum_money) as sum_money," +
                "sum(sum_outsaldo_k) as sum_outsaldo_k," +
                "sum(sum_outsaldo_d) as sum_outsaldo_d," +
                "sum(sum_outsaldo) as sum_outsaldo," +
                "sum(out_real) as out_real," +
                "sum(percent) as percent "+
                "from t_saldoDom "+
                "group by 1,2,3,4,5 "+
                "order by 1,2,3,4,5 ");
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            #endregion


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            string[] months = new string[] { "","январь", "февраль", "март",
            "апрель", "май", "июнь", "июль", "август", "сентябрь",
            "октябрь","ноябрь","декабрь"}; 
            report.SetParameterValue("supp", SuppliersHeader);
            report.SetParameterValue("area", AreasHeader);
            report.SetParameterValue("services", ServicesHeader);
            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            DataTable percentAll = ExecSQLToTable(" select round(sum(percent)/count(percent),2) as percentAll from t_saldoDom ");
            report.SetParameterValue("percentAll", percentAll.Rows[0][0].ToString());
        }

        protected override void PrepareParams()
        {
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
        }

        protected override void CreateTempTable()
        {

            string sql = " create temp table t_saldoDom (  " +
                         " town character(30), " +
                         " ulica character(40), " +
                         " ndom character(10), " +
                         " nkor character(3), " +
                         " nkvar character(10), " +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_p " + DBManager.sDecimalType + "(14,2), " +
                         " sum_lgota " + DBManager.sDecimalType + "(14,2), " +
                         " sum_real " + DBManager.sDecimalType + "(14,2), " +
                         " real_charge " + DBManager.sDecimalType + "(14,2), " +
                         " reval_nedop " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " +
                         " out_real " + DBManager.sDecimalType + "(14,2), " +
                         " percent integer " +
                         " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_saldoDom ", true);
        }

    }
}

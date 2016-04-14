using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;

namespace Bars.KP50.Report.RT.Reports
{
    class Incom_stReport16104m : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.10.4мФ Состояние поступлений"; }
        }

        public override string Description
        {
            get { return "10.4мФ Состояние поступлений"; }
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
            get { return Resources._10_4_mF_incom_status; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        
        /// <summary> с расчетного дня </summary>
        protected DateTime dat_s { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime dat_po { get; set; }

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
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            DateTime datS = curCalcMonthYear != null
                ? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
                    Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
                : DateTime.Now;
            DateTime datPo = curCalcMonthYear != null
                ? datS.AddMonths(1).AddDays(-1)
                : DateTime.Now;
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new PeriodParameter(datS, datPo),
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
                where_area = "and nzp_area in(" + where_area + ") ";
            }


            #endregion

            for (int i = dat_s.Year * 12 + dat_s.Month; i < dat_po.Year * 12 + dat_po.Month + 1; i++)
            {
                int year_ = i / 12;
                int month_ = i % 12;
                if (month_ == 0)
                {
                    year_--;
                    month_ = 12;
                }
                string distribXX = ReportParams.Pref + "_fin_" + (year_ - 2000).ToString("00") +
                        DBManager.tableDelimiter + "fn_distrib_dom_" + month_.ToString("00");
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_svod (nzp_area, nzp_serv, sum_prih, sum_ud) " +
                       " select nzp_area, nzp_serv,  sum(sum_rasp) as sum_prih, sum(sum_ud) as  sum_ud  " +
                       " from " + distribXX + " d " +
                       " where d.nzp_serv>1 and nzp_area not in (4000,5000)" +
                         where_area + where_serv + 
                       " and dat_oper<='" + dat_po.ToShortDateString() + "'" +
                       " and dat_oper>='" + dat_s.ToShortDateString() + "'" +
                       " group by 1,2 ");
                ExecSQL(sql.ToString());
            }

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 ");
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_svod (nzp_area, nzp_serv, sum_real, sum_charge) " +
                           " select nzp_area, nzp_serv, sum(sum_real) as sum_real," +
                           " sum(c.sum_charge) as sum_charge " +
                           " from " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_"+ Month.ToString("00") + " c, " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                           " where c.nzp_serv>1 and k.nzp_kvar = c.nzp_kvar and dat_charge is null " +
                             where_area + where_serv + where_supp +
                           " group by 1,2 ");
                    ExecSQL(sql.ToString());
            }
            reader.Close();
            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_svod3 (nzp_area, nzp_serv, sum_real, sum_charge, sum_prih, sum_ud)  ");
            sql.Append(" select nzp_area, nzp_serv, sum(sum_real), sum(sum_charge) as sum_charge,sum(sum_prih) as sum_prih, " +
                       "sum(sum_ud) as sum_ud from t_svod t group by 1,2  ");
            ExecSQL(sql.ToString());

      

            DataTable dt = ExecSQLToTable(
                  " select area, service, sum_charge, sum_prih, sum_real, " +
                  " (round(case when sum_charge>0 then sum_prih/sum_charge else 0 end,4)*100) as percent, sum_ud " +
                  " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services s,  " +
                  " t_svod3 t  " +
                  " where a.nzp_area = t.nzp_area  and s.nzp_serv = t.nzp_serv order by 1,2 ");                     
            dt.TableName = "Q_master";
           // dt2.TableName = "Q_master2";
            DataTable dt1 = ExecSQLToTable(
                  " select service, sum(sum_real) as sum_real, sum(sum_charge) as sum_charge, sum(sum_prih) as sum_prih, sum(sum_ud) as sum_ud " +
                  " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services s, t_svod3 t " +
                  " where s.nzp_serv = t.nzp_serv group by 1 order by 1 ");
            dt1.TableName = "Q_master1";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            //ds.Tables.Add(dt2);
            return ds;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string[] months = new string[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("dats", dat_s.ToShortDateString());
            report.SetParameterValue("datpo", dat_po.ToShortDateString());
            
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            string period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            dat_s = d1;
            dat_po = d2;
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod(" +
            " nzp_area integer, " +
            " nzp_serv integer, " +
            " sum_real " + DBManager.sDecimalType + "(14,2), " +
            " sum_charge " + DBManager.sDecimalType + "(14,2), " +
            " sum_prih " + DBManager.sDecimalType + "(14,2), " +
            " sum_ud " + DBManager.sDecimalType + "(14,2)" +
            " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = " create temp table t_svod3(" +
            " nzp_area integer, " +
            " nzp_serv integer, " +
            " sum_real " + DBManager.sDecimalType + "(14,2), " +
            " sum_charge " + DBManager.sDecimalType + "(14,2), " +
            " sum_prih " + DBManager.sDecimalType + "(14,2), " +
            " sum_ud " + DBManager.sDecimalType + "(14,2)" +
            " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod;  ", true);
            ExecSQL(" drop table t_svod3; ", true);
        }

    }
}

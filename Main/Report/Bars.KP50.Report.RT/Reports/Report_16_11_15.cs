using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;

namespace Bars.KP50.Report.RT.Reports
{
    class Report161115 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.11.15.Реестр оплаченных лицевых счетов"; }
        }

        public override string Description
        {
            get { return "11.15.Реестр оплаченных лицевых счетов"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Finans);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_11_15; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Дата с</summary>
        protected DateTime dat_s { get; set; }

        /// <summary>Дата по</summary>
        protected DateTime dat_po { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }


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
                new PeriodParameter(datS, datPo),
                new SupplierParameter()
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {

            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("dats", dat_s.ToShortDateString());
            report.SetParameterValue("datpo", dat_po.ToShortDateString());
            report.SetParameterValue("supp", SupplierHeader);
        }


        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            dat_s = d1;
            dat_po = d2;
            Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
        }

        public override DataSet GetData()
        {
            string sql;
            MyDataReader reader = new MyDataReader();

            string where_supp = String.Empty;
            if (Suppliers != null)
                where_supp = Suppliers.Aggregate(where_supp, (current, nzpSupp) => current + (nzpSupp + ","));
            where_supp = where_supp.TrimEnd(',');


            if (!String.IsNullOrEmpty(where_supp))
            {
                where_supp = " AND nzp_supp in (" + where_supp + ")";

                //Поставщики

                sql = " SELECT name_supp from " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                      " WHERE nzp_supp > 0 " + where_supp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }


            sql = " select bd_kernel as pref " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " where nzp_wp>1 ";

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                for (var i = dat_s.Year * 12 + dat_s.Month;
                         i < dat_po.Year * 12 + dat_po.Month + 1;
                         i++)
                {
                    var year = i / 12;
                    var month = i % 12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }
                    sql = " insert into t_svod (num_ls, dat_vvod, sum_prih) " +
                          " select k.num_ls, f.dat_prih::char(10) as dat_vvod, sum(f.sum_prih) as sum_prih " +
                          " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                            pref + "_charge_" + (year - 2000).ToString() + DBManager.tableDelimiter + "fn_supplier" + month.ToString("00") + " f " +
                          " where dat_uchet>='" + dat_s.ToShortDateString() + "' " +
                          " and dat_uchet<='" + dat_po.ToShortDateString() + "' " +
                          " and f.num_ls = k.num_ls and sum_prih<>0 " + where_supp +
                          " group by 1,2 ";
                    ExecSQL(sql);
                }

            }
            reader.Close();
            #region Выборка на экран
            sql = " select t.num_ls,ulica,idom, " +
                  " case when nkor<>'-' then nkor end as nkor, " +
                  " case when (nkvar<>'-') and (nkvar<>'0') then nkvar end as nkvar, " +
                  " ikvar,fio,dat_vvod,sum(sum_prih) as sum_prih " +
                  " from t_svod t, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                  " where t.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                  " group by 1,2,3,4,5,6,7,8 " +
                  " order by 1,2,3,4,6,8 ";
            #endregion
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;

        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod( " +
                         " num_ls integer, " +
                         " dat_vvod char(10), " +
                         " sum_prih " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod; ");
        }

    }
}

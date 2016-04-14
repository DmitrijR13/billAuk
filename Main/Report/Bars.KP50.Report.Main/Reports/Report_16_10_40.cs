namespace Bars.KP50.Report.Main
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using System.Text;
    using Report;
    using Base;
    using Properties;
    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Arendators : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.10.40 Поступления по арендаторам"; }
        }

        public override string Description
        {
            get { return "10.40 Поступления по арендаторам"; }
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
            get { return Resources._10_40_Arendators; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        /// <summary>Дата с</summary>
        protected DateTime dats { get; set; }

        /// <summary>Дата по</summary>
        protected DateTime datpo { get; set; }

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
                new PeriodParameter(DateTime.Now,DateTime.Now),
                new AreaParameter(),
                new SupplierParameter(),
                new ServiceParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            var sql = new StringBuilder();
            string whereSupp = GetWhereSupp();
            string whereArea = GetWhereArea();
            string whereServ = GetWhereServ();




            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 ");
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                for (int i = dats.Year * 12 + dats.Month; i < datpo.Year * 12 + datpo.Month + 1; i++)
                {
                    int year_ = i / 12;
                    int month_ = i % 12;
                    if (month_ == 0)
                    {
                        year_--;
                        month_ = 12;
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_svod(ndog, fio, num_ls, dat_uchet, nzp_serv, sum_prih) "+
                  " select p.val_prm as ndog, "+
                  "        fio, "+
                  "        k.num_ls, "+
                  "        dat_uchet, "+
                  "        nzp_serv, "+
                  "        sum(sum_prih) as sum_prih " +
                  " from   " + pref + "_charge_" + (year_-2000).ToString("00") + DBManager.tableDelimiter + "fn_supplier" + month_.ToString("00") + " a, "+
                           ReportParams.Pref + DBManager.sDataAliasRest + "kvar k left outer join "+
                           ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 p on (k.nzp_kvar = p.nzp and p.nzp_prm = 883 and is_actual<>100 and  " +
                  "        dat_s<='" + datpo.ToShortDateString() + "' and " +
                  "        dat_po>='" + dats.ToShortDateString() + "' ) " +
                  " where  a.num_ls = k.num_ls and " +
                  "        typek = 3 and " +
                  "        dat_uchet>='"+dats.ToShortDateString()+"' and "+
                  "        dat_uchet<='"+datpo.ToShortDateString()+"' " +
                           whereArea + whereServ + whereSupp +
                  "        group by 1,2,3,4,5 ");

                    ExecSQL(sql.ToString());
                }

            }
            reader.Close();
            DataTable dt = ExecSQLToTable(
                  " select dat_uchet, num_ls, fio, ndog, c.nzp_serv, service, " +
                  " sum(sum_prih) as sum_prih "+
                  " from   t_svod c, "+
                    ReportParams.Pref + DBManager.sKernelAliasRest +"services s"+
                  " where  c.nzp_serv = s.nzp_serv "+
                  " group by 1,2,3,4,5,6 "+
                  " order by 1,2,3,5 " );
            dt.TableName = "Q_master";
            DataTable dt1 = ExecSQLToTable(
                  " select dat_uchet, sum(sum_prih) as sum_prih " +
                  " from   t_svod c, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services s" +
                  " where  c.nzp_serv = s.nzp_serv " +
                  " group by 1 " +
                  " order by 1 ");
            dt1.TableName = "Q_master1";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            return ds;
        }


        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreasHeader = AreasHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',', ' ');

            }
            return whereServ;
        }


        protected override void PrepareReport(Report report)
        {
            report.SetParameterValue("dats", dats.ToShortDateString());
            report.SetParameterValue("datpo", datpo.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("area", AreasHeader);
            report.SetParameterValue("supp", SuppliersHeader);
            report.SetParameterValue("serv", ServicesHeader);
        }

        protected override void PrepareParams()
        {

            Services = UserParamValues["Services"].GetValue<List<int>>();
            Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

            string period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            dats = d1;
            datpo = d2;
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod(" +
            " nzp_dom integer, "+
            " nzp_serv integer, "+
            " fio char(100), "+
            " num_ls integer, "+
            " dat_uchet char(10), "+
            " ndog char(20), "+
            " sum_prih " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.TatEnergo.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.TatEnergo.Reports
{
    public class Report502 : BaseSqlReport
    {
        public override string Name
        {
            get { return "50.2 Ведомость должников "; }
        }

        public override string Description
        {
            get { return "50.2 Ведомость должников "; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.report50_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
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

        /// <summary>Районы</summary>
        protected List<int> Rajons { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string RajonHeader { get; set; }

        private string _ercName;

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new RaionsParameter(),
                new ServiceParameter(),
                new SupplierParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            _ercName = GetErcName();


            string prefSPoint = DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point ";

            string sql;
            DataTable dpref;
            if (TempTableInWebCashe(prefSPoint))
            {
                sql = " select bd_kernel as pref " +
                      " from " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                      " where nzp_wp>1 ";
                dpref = ExecSQLToTable(sql);
            }
            else
            {
                dpref = new DataTable();
                dpref.Columns.Add("pref");
                dpref.Rows.Add(ReportParams.Pref);
            }

            foreach (DataRow dr in dpref.Rows)
            {
                if (dr["pref"] == null) continue;
                string pref = dr["pref"].ToString().Trim();
                string chargeXX = pref + "_charge_" + (Year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "charge_" + Month.ToString("00");

                //Проверка на существование базы
                if (TempTableInWebCashe(chargeXX))
                {

                    sql = " insert into t_ensaldo (num_ls, sum_insaldo,  sum_real )" +
                          " select a.num_ls, sum(sum_insaldo)," +
                          " sum(sum_real) " +
                          " from " + chargeXX + " a, " + pref + DBManager.sDataAliasRest +"kvar k" +
                          " where a.nzp_kvar=k.nzp_kvar and dat_charge is null and nzp_serv>1 " +
                          GetWhereAdr() + GetWhereServ() + GetWhereSupp() +
                          " group by 1            ";
                    ExecSQL(sql);

                }
            }


            sql = " update t_ensaldo set sum_in6 = " +
                  " case when sum_insaldo >sum_real*6 then sum_real*6 else sum_insaldo end";
            ExecSQL(sql);

            sql = " update t_ensaldo set sum_in12 = " +
                  " case when sum_insaldo >sum_real*12 then sum_real*6 else sum_insaldo - sum_real*6 end"+
                  " where sum_insaldo>sum_real*6";
            ExecSQL(sql);

            sql = " update t_ensaldo set sum_in24 = " +
                  " case when sum_insaldo >sum_real*24 then sum_real*12 else sum_insaldo - sum_real*12 end" +
                  " where sum_insaldo>sum_real*12";
            ExecSQL(sql);

            sql = " update t_ensaldo set sum_in36 = " +
                  " case when sum_insaldo >sum_real*36 then sum_real*12 else sum_insaldo - sum_real*24 end" +
                  " where sum_insaldo>sum_real*24";
            ExecSQL(sql);

            sql = " update t_ensaldo set sum_in = sum_insaldo - sum_real*36" +
                  " where sum_insaldo>sum_real*36";
            ExecSQL(sql);


            sql = " select  " +
                  " trim(ulica)|| " +
                  " (case when ndom is null then '' else  " +
                  " ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                  " (case when nkvar is null then '' else  " +
                  " ' кв.'||trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end)  " +
                  " end ) as adr, " +
                  " fio, t.num_ls," +
                  " sum_insaldo, " +
                  " sum_in6, " +
                  " sum_in12, " +
                  " sum_in24, " +
                  " sum_in36, " +
                  " sum_in, " +
                  " sum_real " +
                  " from t_ensaldo t, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s " +
                  " where t.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = s.nzp_ul " +
                  " and sum_insaldo > sum_real " +
                  " order by 1,2,3,4,5,6,7,8                ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

   

        protected override void CreateTempTable()
        {


            const string sql = " create temp table t_ensaldo (     " +
                               " num_ls integer default 0," +
                               " sum_insaldo "+DBManager.sDecimalType+"(14,2) default 0.00," + //Входящее сальдо по населению
                               " sum_in6  " + DBManager.sDecimalType + "(14,2) default 0.00," + //долг до 6 месяцев
                               " sum_in12  " + DBManager.sDecimalType + "(14,2) default 0.00," + //долг от 6 месяцев до года
                               " sum_in24  " + DBManager.sDecimalType + "(14,2) default 0.00," + //долг от года до 2-х
                               " sum_in36  " + DBManager.sDecimalType + "(14,2) default 0.00," + //долг от 2-х до 3-х лет
                               " sum_in  " + DBManager.sDecimalType + "(14,2) default 0.00," + //долг свыше 3-х лет
                               " sum_real  " + DBManager.sDecimalType + "(14,2) default 0.00 " + //Начислено за месяц
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" drop table t_ensaldo ");
            }
            catch (Exception e)
            {
               MonitorLog.WriteLog("Отчет 50.2 "+e.Message,MonitorLog.typelog.Error,false);
            }
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("month", Month);
            report.SetParameterValue("year", Year);
            string headers = String.Empty;
            if (!String.IsNullOrEmpty(AreaHeader)) headers = AreaHeader;
        
            report.SetParameterValue("headers", headers);
            report.SetParameterValue("services", ServiceHeader);
            report.SetParameterValue("ercName", _ercName);
        }

        /// <summary>
        /// Получате условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            whereServ = whereServ.TrimEnd(',');
            if (String.IsNullOrEmpty(whereServ)) whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ) & String.IsNullOrEmpty(ServiceHeader))
            {
                string sql = " SELECT service from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "services " +
                             " WHERE nzp_serv > 1 " + whereServ;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ",";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',');
            }
            return whereServ;
        }

        /// <summary>
        /// Получает условия ограничения по территории
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr()
        {
            string whereAdr = String.Empty;
            if (Areas != null)
            {
                whereAdr = Areas.Aggregate(whereAdr, (current, nzpArea) => current + (nzpArea + ","));
            }

            whereAdr = whereAdr.TrimEnd(',');
            if (String.IsNullOrEmpty(whereAdr)) whereAdr = ReportParams.GetRolesCondition(Constants.role_sql_area);

            if (!String.IsNullOrEmpty(whereAdr))
            {
                whereAdr = " AND nzp_area in (" + whereAdr + ")";

                if (!String.IsNullOrEmpty(AreaHeader))
                {
                    string sql = " SELECT area from " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                                 " WHERE nzp_area > 0 " + whereAdr;
                    DataTable area = ExecSQLToTable(sql);
                    foreach (DataRow dr in area.Rows)
                    {
                        AreaHeader += dr["area"].ToString().Trim() + ",";
                    }
                    AreaHeader = AreaHeader.TrimEnd(',');
                }
            }
            return whereAdr;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            whereSupp = whereSupp.TrimEnd(',');
            if (String.IsNullOrEmpty(whereSupp)) whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            if (!String.IsNullOrEmpty(whereSupp)) whereSupp = " and nzp_supp in (" + whereSupp + ")";
            return whereSupp;
        }

     

        /// <summary>
        /// Получает условия ограничения по району
        /// </summary>
        /// <returns></returns>
        private string GetErcName()
        {
            string result = "Не определено наименование Расчетного центра";
            string sql = " select val_prm " +
                         " from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                         " where nzp_prm=80 and is_actual=1 and dat_s<=" + DBManager.sCurDate +
                         " and dat_po>=" + DBManager.sCurDate;
            DataTable erc = ExecSQLToTable(sql);
            if (erc != null)
                if (erc.Rows.Count > 0)
                {
                    result = erc.Rows[0]["val_prm"].ToString().Trim();
                }


            return result;
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();

            if (!String.IsNullOrEmpty(ReportParams.User.date_begin))
            {
                if (Convert.ToDateTime(ReportParams.User.date_begin) > new DateTime(Year, Month, 01))
                {
                    Month = Convert.ToDateTime(ReportParams.User.date_begin).Month;
                    Year = Convert.ToDateTime(ReportParams.User.date_begin).Year;
                }

            }
            Services = UserParamValues["Services"].Value.To<List<int>>();
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Rajons = UserParamValues["Raions"].Value.To<List<int>>();
    
        }
    }
}

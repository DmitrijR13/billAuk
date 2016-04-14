using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RSO.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.RSO.Reports
{
    public class Report1517 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.1.7 Ведомость должников "; }
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
            get { return Resources.Report_15_1_7; }
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

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

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

        private string _ercName;

        private bool _rowCount;

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new RaionsParameter(),
                new ServiceParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            _ercName = GetErcName();


            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetWhereWp();
            DataTable dpref = ExecSQLToTable(sql);
            var datS = new DateTime(Year, Month, 1);
            var datPo = datS.AddMonths(1).AddDays(-1);

            foreach (DataRow dr in dpref.Rows)
            {
                if (dr["pref"] == null) continue;
                string pref = dr["pref"].ToString().Trim();
                string chargeXx = pref + "_charge_" + (Year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string calcXx = pref + "_charge_" + (Year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00");

                //Проверка на существование базы
                if (TempTableInWebCashe(chargeXx) && TempTableInWebCashe(calcXx))
                {

                    sql = " insert into t_ensaldo (num_ls, ob_s, nzp_serv, gil, sum_insaldo, sum_real  )" +
                          " select ch.num_ls, val_prm, ch.nzp_serv, sum(round(gil)), sum(sum_insaldo), sum(sum_real) " +
                          " from " + pref + DBManager.sDataAliasRest + "kvar k, " + chargeXx + " ch " +
                          " left outer join " + calcXx + " cg on (ch.nzp_kvar = cg.nzp_kvar and cg.nzp_serv>1 and cg.dat_charge is null and cg.nzp_serv = ch.nzp_serv and cg.nzp_supp = ch.nzp_supp ) " +
                          " left outer join " + pref + DBManager.sDataAliasRest + "prm_1 p1 " + 
                          " on (ch.nzp_kvar = p1.nzp and is_actual = 1 and nzp_prm = 4 and dat_s<'" + datPo.ToShortDateString() + "' and dat_po> '" + datS.ToShortDateString() + "') " +
                          " where ch.num_ls=k.num_ls and ch.dat_charge is null and ch.nzp_serv>1 " +
                            GetWhereAdr() + GetWhereServ() + GetWhereSupp() +
                          " group by 1,2,3 ";
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
                  " trim(case when rajon<>'-' then rajon else town end)||' / '||" +
                  " trim(ulica)|| " +
                  " (case when ndom is null then '' else  " +
                  " ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                  " (case when nkvar is null then '' else  " +
                  " ' кв.'||trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end)  " +
                  " end ) as adr, " +
                  " fio, ob_s, gil, t.num_ls," +
                  " service, " +
                  " sum_insaldo, " +
                  " sum_in6, " +
                  " sum_in12, " +
                  " sum_in24, " +
                  " sum_in36, " +
                  " sum_in," +
                  " sum_real " +
                  " from t_ensaldo t, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town tow, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s " +
                  " where t.num_ls = k.num_ls " +
                  " and k.nzp_dom = d.nzp_dom " +
                  " and d.nzp_ul = u.nzp_ul " +
                  " and u.nzp_raj = r.nzp_raj " +
                  " and r.nzp_town = tow.nzp_town " +
                  " and t.nzp_serv = s.nzp_serv " +
                  " and sum_insaldo > sum_real " + GetWhereRaj() +
                  " order by 1,2,3,4,5,6,7,8 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            if (dt.Rows.Count > 70000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(70000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
                _rowCount = true;
            }
            else
            {
                _rowCount = false;
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

   

        protected override void CreateTempTable()
        {


            const string sql = " create temp table t_ensaldo (     " +
                               " num_ls integer default 0," +
                               " ob_s character(20)," +
                               " nzp_serv integer," +
                               " gil integer default 0," +
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

            report.SetParameterValue("excel",
                _rowCount
                    ? "Выборка записей ограничена первыми 70000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }

        /// <summary>
        /// Получить условия органичения по банкам
        /// </summary>
        /// <returns></returns>
        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
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
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND ch.nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ) & String.IsNullOrEmpty(ServiceHeader))
            {
                string sql = " SELECT service from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "services ch " +
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
            if (!String.IsNullOrEmpty(whereSupp)) whereSupp = " and ch.nzp_supp in (" + whereSupp + ")";
            return whereSupp;
        }        
        
        /// <summary>
        /// Получает условия ограничения по району
        /// </summary>
        /// <returns></returns>
        private string GetWhereRaj()
        {
            string whereRaj = String.Empty;
            if (Rajons != null)
                whereRaj = Rajons.Aggregate(whereRaj, (current, nzpRaj) => current + (nzpRaj + ","));
            whereRaj = whereRaj.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereRaj)) whereRaj = " and r.nzp_raj in (" + whereRaj + ")";
            return whereRaj;
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
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Rajons = UserParamValues["Raions"].Value.To<List<int>>();
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.MariEl.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;
using Newtonsoft.Json;


namespace Bars.KP50.Report.MariEl.Reports
{
    /// <summary>Сводный отчет по начислениям для Тулы</summary>
    public class Report120104 : BaseSqlReport
    {
        public override string Name
        {
            get { return "12.1.4 Сводный отчет по начислениям"; }
        }

        public override string Description
        {
            get { return "Сводный отчет по начислениям для Тулы"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_12_1_4; }
        }
        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок услуг</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary>Вид начислено</summary>
        protected int TypeNacl { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

     
        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }
       
        private int _DivideServs;
        
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
                new ComboBoxParameter
                {
                    Code = "ReportTitle",
                    Name = "Заголовок отчета",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Сводный отчет по начислениям за жилищно-коммунальные услуги" },
                        new { Id = "2", Name = "Сводный отчет по начислениям за наем жилого помещения" },
                        new { Id = "3", Name = "Сводный отчет по начислениям за жилищные услуги" },
                        new { Id = "4", Name = "Сводный отчет по начислениям за коммунальные услуги" }
                    }
                },
                new ComboBoxParameter
                {
                    Code = "TypeNacl",
                    Name = "Вид начислено",
                    Value = "3",
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Начислено к оплате" },
                        new { Id = "2", Name = "Расчитано по тарифу" },
                        new { Id = "3", Name = "Начислено за месяц (без учета долга) с уч. перерасчета" }
                    }
                },
                new BankSupplierParameter(),
                new ServiceParameter(),
                new AreaParameter(),  
                new ComboBoxParameter
                {
                    Code = "DivideServs",
                    Name = "Холодная вода и Теплоноситель",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new {Id = 1, Name = "Разделять дома"},
                        new {Id = 2, Name = "Не разделять дома"},
                    }
                },
            };
        }

        public override DataSet GetData()
        {
            /* Валидация параметров */

            MyDataReader reader;
            string whereSupp = GetWhereSupp("a.nzp_supp"),
                    whereServ = GetWhereServ("a."),
                     whereArea = GetWhereArea("k.");
            var sql  = " SELECT * "+
                   " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point "+
                   " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);

            bool listLc = GetSelectedKvars();

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    var chargeXx = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                   "charge_" + month.ToString("00");

                    string vidNach;
                    switch (TypeNacl)
                    {
                        case 1:
                            vidNach = "a.sum_charge";
                            break;
                        case 2:
                            vidNach = "a.rsum_tarif";
                            break;
                        default:
                            vidNach = "a.rsum_tarif + a.real_charge + a.reval";
                            break;
                    }


                    sql = " INSERT INTO t_nach (nzp_dom, nzp_serv, nzp_supp, sum_nach )" +
                          " SELECT  k.nzp_dom, a.nzp_serv, a.nzp_supp, sum(" + vidNach + ")  " +
                          " FROM " + chargeXx + " a, " +
                          (listLc ? " selected_kvars k " : pref + DBManager.sDataAliasRest + "kvar k ") + 
                          " WHERE a.nzp_kvar=k.nzp_kvar " +
                          "        AND dat_charge is null " +
                          "        AND a.nzp_serv>1 " +
                          whereArea + whereSupp + whereServ + 
                          " GROUP BY  1,2,3            ";
                    if (TempTableInWebCashe(chargeXx)) ExecSQL(sql);

                    if (TypeNacl == 3)
                    {
                        var perekidka = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                        "perekidka";
                        sql = " INSERT INTO t_nach (nzp_dom, nzp_serv, nzp_supp, sum_nach )" +
                              " SELECT  k.nzp_dom, a.nzp_serv, a.nzp_supp, -sum(sum_rcl)  " +
                              " FROM " + perekidka + " a, " +
                              (listLc ? " selected_kvars k " : pref + DBManager.sDataAliasRest + "kvar k ") +
                              " WHERE a.nzp_kvar=k.nzp_kvar AND type_rcl IN ( 100, 20) " +
                              "        AND month_=  " + month +
                              "        AND a.nzp_serv>1 " +
                              whereArea + whereSupp + whereServ +
                              " GROUP BY  1,2,3            ";
                        if (TempTableInWebCashe(perekidka)) ExecSQL(sql);

                    }

                }
                sql = " UPDATE t_nach " +
                      " SET name_nzp_14 = ( SELECT Trim(val_prm) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_2 p " +
                      " WHERE p.nzp_prm=3000014 " +
                      " AND p.nzp = t_nach.nzp_dom " +
                      " AND is_actual <> 100 )"+
                      " WHERE name_nzp_14 IS NULL AND (nzp_serv=14 OR nzp_serv=514)";
                ExecSQL(sql);

                sql = " UPDATE t_nach " +
                      " SET name_nzp_14 = 'Теплоноситель' " +
                      " WHERE name_nzp_14 IS NULL AND (nzp_serv=14 OR nzp_serv=514)";
                ExecSQL(sql);
            }

            reader.Close();

            if (_DivideServs == 1)
            {
                sql = " UPDATE t_nach SET divide_serv = 1" +
                      " WHERE Trim(name_nzp_14)<>'Теплоноситель' ";
                ExecSQL(sql);
            }


            sql = " SELECT divide_serv, sp.payer as principal, sa.payer as agent, " +
                  " (CASE WHEN t.nzp_serv=14 THEN name_nzp_14 WHEN t.nzp_serv=514 THEN 'ОДН-'||name_nzp_14 ELSE s.service END) AS service, " +
                  "        su.payer as name_supp, " +
                  "        sum(t.sum_nach) as sum_charge " +
                  " FROM t_nach t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sup, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sa, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer su " +
                  " WHERE t.nzp_supp = sup.nzp_supp " +
                  "        AND sup.nzp_payer_supp = su.nzp_payer " +
                  "        AND sup.nzp_payer_princip = sp.nzp_payer " +
                  "        AND sup.nzp_payer_agent = sa.nzp_payer " +
                  "        AND t.nzp_serv = s.nzp_serv " +
                  " GROUP BY 1,2,3,4,5 " +
                  " ORDER BY divide_serv, sp.payer, sa.payer,su.payer,service ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private string GetWhereServ(string tablePrefix)
        {
            var result = String.Empty;
            result = Services != null 
                ? Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ",")) 
                : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result)
                ? " AND " + tablePrefix + "nzp_serv in (" + result + ")"
                : String.Empty;

            //Дополнительно фильтруем услугив в зависисости от заголовка
            switch (ReportTitle)
            {
                case 1:
                    break;
                case 2:
                    result = " and nzp_serv=15 ";
                    break;
                case 3:
                    result += " and nzp_serv not in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "s_counts )";
                    break;
                case 4:
                    result += " and (nzp_serv in (select nzp_serv from " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts) " +
                                 " or nzp_serv in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "serv_odn)) ";
                    break;
            }
            if (!string.IsNullOrEmpty(result) && string.IsNullOrEmpty(ServiceHeader))
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services " + tablePrefix.TrimEnd('.') + " WHERE nzp_serv > 0 " + result;
                DataTable servTable = ExecSQLToTable(sql);
                foreach (DataRow row in servTable.Rows)
                {
                    ServiceHeader += row["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
            }
            return result;
        }

        private string GetWhereArea(string tablePrefix)
        {
            var result = String.Empty;
            if (Areas != null)
            {
                result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            result = result.TrimEnd(',');
            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + tablePrefix + "nzp_area in (" + result + ")";

                AreaHeader = String.Empty;
                var sql = " SELECT area from " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.')+
                      " WHERE " + tablePrefix + "nzp_area > 0 " + result;
                var area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',',' ');
            }
            return result;
        }

        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',',' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
            whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into selected_kvars (nzp_kvar, nzp_dom, nzp_area) " +
                                     " select nzp_kvar, nzp_dom, nzp_area from " + tSpls;
                        ExecSQL(sql);
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period;
            const string num = "1.1 ";

            if (DatS==DatPo)
            {
                period = DatS.ToShortDateString() + " г.";
            }
            else
            {
                period = "период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
            }
            report.SetParameterValue("period", period);

            report.SetParameterValue("ispolnitel", ReportParams.User.uname);
            var ercName = ExecScalar(" select distinct val_prm from " +
                                         ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 where nzp_prm = 80 and is_actual = 1 " +
                                       " and dat_s <'" + DateTime.Now.ToShortDateString() + "' and dat_po> '" + DateTime.Now.ToShortDateString() + "' ");
            report.SetParameterValue("ercName", ercName == null ? "" : ercName.ToString().Trim());

            var director = ExecScalar(" select distinct val_prm from " +
                                         ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 where nzp_prm = 1294 and is_actual = 1 " +
                                       " and dat_s <'" + DateTime.Now.ToShortDateString() + "' and dat_po> '" + DateTime.Now.ToShortDateString() + "' ");
            report.SetParameterValue("director", director == null ? "" : director.ToString().Trim());
            switch (ReportTitle)
            {
                case 1:
                    report.SetParameterValue("reportHeader", num + "Сводный отчет по начислениям за жилищно-коммунальные услуги");
                    break;
                case 2:
                    report.SetParameterValue("reportHeader", num + "Сводный отчет по начислениям за наем жилого помещения");
                    break;
                case 3:
                    report.SetParameterValue("reportHeader", num + "Сводный отчет по начислениям за жилищные услуги");
                    break;
                case 4:
                    report.SetParameterValue("reportHeader", num + "Сводный отчет по начислениям за коммунальные услуги");
                    break;

            }
            
            switch (TypeNacl)
            {
                case 1:
                    report.SetParameterValue("sumHeader", "Начислено к оплате");
                    break;
                case 2:
                    report.SetParameterValue("sumHeader", "Расчитано по тарифу");
                    break;
                case 3:
                    report.SetParameterValue("sumHeader", "Начислено за месяц (без учета долга) с уч. перерасчета");
                    break;
            }

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            /*
            var reportAreaList = UserParams["reportAreaList"].GetValue<List<UserParam>>();

            // Определение принциала
            var principal = reportAreaList.Aggregate(string.Empty, (current, kp) => current + (string.IsNullOrEmpty(current) ? string.Empty : ", ") + kp.Value);
            if (string.IsNullOrEmpty(principal))
            {
                var reportSuppList = UserParams["reportSuppList"].GetValue<List<UserParam>>();
                principal = reportSuppList.Aggregate(principal, (current, kp) => current + (string.IsNullOrEmpty(current) ? string.Empty : ", ") + kp.Value);
            }
            
            report.SetParameterValue("principal", principal);*/
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DatS = begin;
            DatPo = end;

            ReportTitle = UserParamValues["ReportTitle"].GetValue<int>();
            TypeNacl = UserParamValues["TypeNacl"].GetValue<int>();

            Services = UserParamValues["Services"].GetValue<List<int>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
            _DivideServs = UserParamValues["DivideServs"].GetValue<int>();   
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" create " + DBManager.sCrtTempTable + " table t_nach (     ");
            sql.Append(" nzp_dom integer default 0,");
            sql.Append(" nzp_area integer default 0,");
            sql.Append(" nzp_serv integer default 0,");
            sql.Append(" nzp_supp integer default 0,");
            sql.Append(" divide_serv integer default 0,");
            sql.Append(" name_nzp_14 char(20),");
            sql.Append(" sum_nach " + DBManager.sDecimalType + "(14,2) default 0.00 ");
            sql.Append(" ) " + DBManager.sUnlogTempTable);

            ExecSQL(sql.ToString());

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                const string sqls = " create temp table selected_kvars(" +
                                    " nzp_kvar integer, " +
                                    " nzp_dom integer, " +
                                    " nzp_geu integer, " +
                                    " nzp_area integer) " +
                                    DBManager.sUnlogTempTable;
                ExecSQL(sqls);
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_nach ", true);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
        }
    }
}
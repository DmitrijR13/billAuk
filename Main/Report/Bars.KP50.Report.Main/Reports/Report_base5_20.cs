using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Main.Properties;

namespace Bars.KP50.Report.Main.Reports
{
    class SaldoRepLS : BaseSqlReport
    {
        public override string Name
        {
            get { return "Базовый - Сальдовая ведомость по лицевым счетам"; }
        }

        public override string Description
        {
            get { return "5.20 Сальдовая ведомость по лицевым счетам (с квартиросъемщиками)"; }
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
            get { return Resources.SaldoRepLS; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base,  ReportKind.ListLC }; }
        }


        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

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

        /// <summary>Список групп</summary>
        protected string GroupsHeader { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        private int _typeAdres;

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter(),
                new ServiceParameter(),
                new ComboBoxParameter
                {
                    Code = "ShowTown",
                    Name = "Состав адреса",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "Район, населенный пункт, улица"},
                        new {Id = "2", Name = "Населенный пункт, улица"},
                        new {Id = "3", Name = "Улица"},
                    }
                },
            };
        }

        public override DataSet GetData()
        {

            #region Выборка по локальным банкам

            bool listLc = GetSelectedKvars();

            MyDataReader reader;

            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                sql = " insert into t_saldoLS (num_ls, sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, " +
                      " reval_charge, sum_money, money_del, sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo ) " +
                      " select k.num_ls,  " +
                      " sum(case when c.sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k, " +
                      " sum(case when c.sum_insaldo>0 then sum_insaldo else 0 end) as sum_insaldo_d, " +
                      " sum(sum_insaldo) as sum_insaldo, " +
                      " sum(sum_real) as sum_real, " +
                      " sum(real_charge) + sum(reval) as reval_charge, " +
                      " sum(sum_money) as sum_money, " +
                      " sum(money_del) as money_del, " +
                      " sum(case when c.sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_k, " +
                      " sum(case when c.sum_outsaldo>0 then sum_outsaldo else 0 end) as sum_outsaldo_d, " +
                      " sum(sum_outsaldo) as sum_outsaldo " +
                      " from " +
                      (listLc ? " selected_kvars k, " : ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, ") + 
                      pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" +
                      Month.ToString("00") + " c " +
                      " Where c.dat_charge is null " +
                      " and c.nzp_serv > 1 " +
                      " and k.num_ls = c.num_ls " +
                      GetWhereServ() + GetWhereSupp() + GetWhereArea() +
                      " group by 1 ";
                ExecSQL(sql);
            }

            reader.Close();

            #endregion

            #region Выборка на экран

            string adres;

            switch (_typeAdres)
            {
                case 1:
                    adres = "(case when rajon='-' " +
                            " then town else trim(town)||','||trim(rajon) end)||', '" +
                            "||trim(" + DBManager.sNvlWord + "(ulicareg,''))||' '||trim(ulica)";
                    break;
                case 2:
                    adres = "(case when rajon='-' " +
                            " then town else trim(rajon) end)||', '" +
                            "||trim(" + DBManager.sNvlWord + "(ulicareg,''))||' '||trim(ulica)";
                    break;
                case 3:
                    adres = "trim(" + DBManager.sNvlWord + "(ulicareg,''))||' '||trim(ulica)";
                    break;
                default: adres = "trim(" + DBManager.sNvlWord + "(ulicareg,''))||' '||trim(ulica)";
                    break;
            }
            

            sql =
                " select a.num_ls, " +
                " " + adres + " as ulica, " +
                " idom, ndom, (case when nkor ='-' then '' else nkor end) as nkor, " +
                " (case when (nkvar<>'0' and nkvar<>'-') then 'Кв.'||nkvar end) as nkvar, fio, " +
                " sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, " +
                " reval_charge, sum_money, money_del, sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo " +
                " from t_saldoLS a, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                " where a.num_ls=k.num_ls " +
                " and k.nzp_dom=d.nzp_dom" +
                " and d.nzp_ul=s.nzp_ul" +
                " and s.nzp_raj=sr.nzp_raj" +
                " and sr.nzp_town=st.nzp_town" +
                " order by 2,3,4,5,6,1";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }



        /// <summary>
        /// Выборка списка квартир в картотеке
        ///  </summary>
        /// <returns></returns>
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
                        string sql = " insert into selected_kvars (num_ls, nzp_area) " +
                                     " select num_ls, nzp_area from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_tmpsk_ls_01 in selected_kvars(num_ls) ");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars ");
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
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
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_supp);
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
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
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


        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("supp", SuppliersHeader);
            report.SetParameterValue("area", AreasHeader);
            report.SetParameterValue("services", ServicesHeader);
            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
        }

        protected override void PrepareParams()
        {
            //using (var sw = new StreamWriter(@"D:\1.txt")) sw.WriteLine("Begin!");
            //System.Threading.Thread.Sleep(new TimeSpan(0, 15, 0));
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            _typeAdres = UserParamValues["ShowTown"].GetValue<int>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_saldoLS (  " +
                               " num_ls integer default 0, " +
                               " sum_insaldo_k " + DBManager.sDecimalType + "(14,2), " +
                               " sum_insaldo_d " + DBManager.sDecimalType + "(14,2), " +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " sum_real " + DBManager.sDecimalType + "(14,2), " +
                               " reval_charge " + DBManager.sDecimalType + "(14,2), " +
                               " sum_money " + DBManager.sDecimalType + "(14,2), " +
                               " money_del " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2) " +
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " create temp table selected_kvars(" +
                    " num_ls integer, " +
                    " nzp_geu integer, " +
                    " nzp_area integer) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_saldoLS ", true);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
        }

    }
}

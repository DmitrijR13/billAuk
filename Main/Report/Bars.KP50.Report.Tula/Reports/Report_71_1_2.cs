using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Bars.KP50.Utils;


namespace Bars.KP50.Report.Tula.Reports
{
    public class Report7112 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.2 Сведения о должниках "; }
        }

        public override string Description
        {
            get { return "Сведения о должниках выводимые в виде списка с группировкой по домам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup>(0); }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_1_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }


        /// <summary>Заголовок отчета</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Заголовок услуг</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Расчетный месяц</summary>
        private int Month { get; set; }

        /// <summary>Расчетный год</summary>
        private int Year { get; set; }

        /// <summary>Квартира</summary>
        private string Nkvar { get; set; }

        /// <summary>Номер комнаты</summary>
        private string NkvarN { get; set; }

        /// <summary>Услуги</summary>
        private List<int> Services { get; set; }

        /// <summary>Районы</summary>
        private string AddressHeader { get; set; }

        /// <summary>Адрес</summary>
        private AddressParameterValue Address { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        private BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        private int IsOpen { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new AddressParameter(),
                new StringParameter{Code ="Nkvar", Name = "квартира"},
                new StringParameter{Code ="Nkvar_n", Name = "комната", Value = "-"},
                new ServiceParameter(),
                new BankSupplierParameter(),
                new ComboBoxParameter(false) {
                    Name = "Лицевой счет", 
                    Code = "IsOpen",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 1, Name = "Открыт"},
                        new { Id = 2, Name = "Закрыт"}
                    }
                }

            };
        }


        public override DataSet GetData()
        {

            GetSelectedKvars();
            #region Выборка по локальным банкам

            MyDataReader reader;

            var sql = " SELECT * " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetwhereWp();
            string whereAddress = GetWhereAdr();
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                CalcOneBank(pref, whereAddress);
            }
            reader.Close();

            #endregion

            #region Выборка на экран
            sql = " SELECT town, " +
                  " (case when rajon ='-' then ''||trim(town) else trim(town)||', '||trim(rajon) end) as rajon, " +
                  " trim("+DBManager.sNvlWord+"(ulicareg,'ул.'))||' '||trim(ulica) as ulica, idom, ndom," +
                  " (case when " + DBManager.sNvlWord + "(nkor,'-') ='-' then '' else 'к.'||trim(nkor) end) as nkor, " +
                  " k.nzp_dom, ikvar, nkvar, " +
                  " (case when " + DBManager.sNvlWord + "(nkvar_n,'-') ='-' then '' else 'к.'||trim(nkvar_n)  end) as nkvar_n , k.num_ls, " +
                  "        fio, sum_dolg,  " +
                  "        sum_dolg2, sum_dolg3, sum_dolg6, sum_dolg12, sum_dolg36, sum_nach  " +
                  " FROM t_dolg a, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                  " WHERE a.num_ls=k.num_ls " +
                  "        AND k.nzp_dom=d.nzp_dom " +
                  "        AND d.nzp_ul=su.nzp_ul " +
                  "        AND su.nzp_raj=sr.nzp_raj " +
                  "        and sr.nzp_town=st.nzp_town " +
                  "        ORDER BY 1,2,3,4,5,6,7,8,9,10 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private void CalcOneBank(string pref, string whereAddress)
        {
            string whereServ = GetWhereServ(),
                    whereSupp = GetWhereSupp("a.nzp_supp");
            int startMonth = GetStartMonthForBank(pref);
            int curMonth = Year * 12 + Month; 
            
            int year = startMonth / 12;
            int month = startMonth % 12;
            if (month == 0)
            {
                year--;
                month = 12;
            }

            var startDate = new DateTime(year,month,1);
            var curDate = startDate.AddMonths(curMonth - startMonth + 1).AddDays(-1);
      

            string sql = " Create temp table t_bankdolg( num_ls integer, " +
                  " month_ integer," +
                  " sum_dolg "+DBManager.sDecimalType+"(14,2)," +
                  " sum_real " + DBManager.sDecimalType + "(14,2))" +
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);

            string chargeX = pref + "_charge_" + (Year - 2000).ToString("00") +
                          DBManager.tableDelimiter + "charge_" + Month.ToString("00");

            //Выбирам первоначальное множестов ЛС
            sql = " insert into t_bankdolg(num_ls, month_, sum_dolg, sum_real)" +
                  " select a.num_ls, 0, sum(sum_insaldo), sum(sum_real) " +
                  " from " +chargeX+" a,"+
                  ((ReportParams.CurrentReportKind == ReportKind.ListLC) ? " selected_kvars k, " : pref + DBManager.sDataAliasRest + "kvar k, " )+
                  pref + DBManager.sDataAliasRest + "dom d, " +
                  pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  pref + DBManager.sDataAliasRest + "prm_3 p " +
                  " WHERE a.nzp_kvar=k.nzp_kvar " +
                  "        AND dat_charge is null " +
                  "        AND a.nzp_serv>1 and k.nzp_dom=d.nzp_dom " +
                  "        AND k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                  "        AND p.dat_s <= '" + curDate.ToShortDateString() + "' " +
                  "        AND p.dat_po >= '" + curDate.ToShortDateString() + "' " +
                  "        AND k.nzp_kvar = p.nzp and p.is_actual = 1 and p.nzp_prm = 51 " +
                  "        AND " + (IsOpen == 1 ? " p.val_prm = '1' " : " p.val_prm <> '1' ") +
                  whereAddress + whereSupp + whereServ +
                  " GROUP BY  1,2 "+
                  " Having sum(sum_insaldo)>0.001";
            ExecSQL(sql);

            ExecSQL("create index ix_tbd_01 on t_bankdolg(num_ls)");
            
            ExecSQL(DBManager.sUpdStat+" t_bankdolg");

            int countDolgMonth = 1;
            for (int i = curMonth; i > startMonth; i--)
            {
                year = i/12;
                month = i%12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }

                string chargeXx = pref + "_charge_" + (year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "charge_" + month.ToString("00");

                if (TempTableInWebCashe(chargeXx))
                {
                    //Увеличиваем количество месяцев задолженности на 1 для тех
                    //у кого положительное сальдо в месяце осталось
                    sql = " UPDATE t_bankdolg set month_=" + countDolgMonth + " " +
                          " where 0<(select sum(sum_insaldo)  " +
                          " FROM " + chargeXx + " a " +
                          " WHERE a.num_ls=t_bankdolg.num_ls " +
                          "        AND dat_charge is null " +
                          "        AND a.nzp_serv>1 " +
                          whereSupp + whereServ +
                          ") and month_= " + (countDolgMonth-1);
                    ExecSQL(sql);

                    //Увеличиваем сумму начисления для подсчета среднемесячного начисления
                    sql = " UPDATE t_bankdolg set sum_real = sum_real +" +
                          DBManager.sNvlWord + "((select sum(sum_real)  " +
                          " FROM " + chargeXx + " a " +
                          " WHERE a.num_ls=t_bankdolg.num_ls " +
                          "        AND dat_charge is null " +
                          "        AND a.nzp_serv>1 " +
                          whereSupp + whereServ +
                          "),0) where month_= " + countDolgMonth;
                    ExecSQL(sql);
                }
                countDolgMonth++;
            }

            sql = " insert into t_dolg (num_ls, sum_dolg, sum_dolg2, sum_dolg3, sum_dolg6, sum_dolg12, sum_dolg36, sum_nach)" +
                  " select num_ls,  " +
                  " case when month_<=2 then sum_dolg else 0 end, " +
                  " case when month_>2 and month_<=3 then sum_dolg else 0 end, " +
                  " case when month_>3 and month_<=6 then sum_dolg else 0 end, " +
                  " case when month_>6 and month_<=12 then sum_dolg else 0 end, " +
                  " case when month_>12 and month_<=36 then sum_dolg else 0 end, " +
                  " case when month_>36 then sum_dolg else 0 end, " +
                  " case when month_=0 then sum_real else sum_real/(month_) end " +
                  " from t_bankdolg ";
            ExecSQL(sql);
            ExecSQL("drop table t_bankdolg");
        }

        private int GetStartMonthForBank(string pref)
        {
            int result = Year*12 + Month;
            string sql = "select val_prm "+
                " from "+pref+DBManager.sDataAliasRest+"prm_10 "+
                " where nzp_prm=771 and is_actual=1 ";
            MyDataReader reader;
            ExecRead(out reader,sql);
            if (reader != null)
                if (reader.Read())
                {
                    if (!String.IsNullOrEmpty(reader["val_prm"].ToString().Trim()))
                    {
                        DateTime d = Convert.ToDateTime(reader["val_prm"].ToString().Trim());
                        result = d.Year*12 + d.Month;
                    }
                }

            return result;
        }

        private string GetWhereServ()
        {
            var result = String.Empty;
            result = Services != null 
                ? Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ",")) 
                : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND nzp_serv in (" + result + ")" : String.Empty;

            if (!String.IsNullOrEmpty(result) && string.IsNullOrEmpty(ServiceHeader))
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service " +
                             " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services " +
                             " WHERE nzp_serv > 0 " + result;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',',' ');
            }
            return result;
        }

        private string GetWhereAdr()
        {
            string rajon = String.Empty,
                street = String.Empty,
                house = String.Empty;
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;

            string result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            

            if (Address.Raions != null)
            {
                rajon = Address.Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ","));
                rajon = rajon.TrimEnd(',');
            }
            if (Address.Streets != null)
            {
                street = Address.Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ","));
                street = street.TrimEnd(',');
            }
            if (Address.Houses != null)
            {
                house = Address.Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ","));
                house = house.TrimEnd(',');
            }

            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND k.nzp_area in (" + result + ")" : String.Empty;
            if (!string.IsNullOrEmpty(house))
            {
                result += !String.IsNullOrEmpty(Nkvar.Trim())
                    ? " AND nkvar = " + STCLINE.KP50.Global.Utils.EStrNull(Nkvar) + ""
                    : String.Empty;
                result += NkvarN.Trim() != "-"
                    ? " AND nkvar_n = " + STCLINE.KP50.Global.Utils.EStrNull(NkvarN) + ""
                    : String.Empty;
            }
            result += !String.IsNullOrEmpty(rajon) ? " AND u.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !String.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !String.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;
            if (!String.IsNullOrEmpty(house))
            {
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor " +
                          " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                     " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                     " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
                          " WHERE nzp_dom IN (" + house + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader = "," + AddressHeader;
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                        ? "ул. " + row["ulica"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
                        ? row["ndom"].ToString().Trim() != "-"
                            ? "д. " + row["ndom"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
                        ? row["nkor"].ToString().Trim() != "-"
                            ? "кор. " + row["nkor"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !String.IsNullOrEmpty(Nkvar)
                        ? "кв. " + Nkvar + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(NkvarN)
                        ? NkvarN != "-"
                            ? "ком. " + NkvarN
                            : string.Empty
                        : string.Empty;
                    AddressHeader = AddressHeader.TrimEnd(',');

                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            else if (!String.IsNullOrEmpty(street))
            {
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica " +
                          " FROM " + prefData + "s_ulica u INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                         " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                          " WHERE nzp_ul IN (" + street + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                        ? "ул. " + row["ulica"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader = AddressHeader.TrimEnd(',');
                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            else if (!String.IsNullOrEmpty(rajon))
            {
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon " +
                          " FROM " + prefData + "s_rajon r INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                          " WHERE nzp_raj IN (" + rajon + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader = AddressHeader.TrimEnd(',');
                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            if (!string.IsNullOrEmpty(AddressHeader))
                AddressHeader = AddressHeader.TrimStart(',');


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
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
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
            if (BankSupplier.Banks != null)
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

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("day", "01");
            report.SetParameterValue("month", Month.ToString("00"));
            report.SetParameterValue("year", Year);

            string headerInfo = "Лицевой счет : " + (IsOpen == 1 ? "открыт" : "закрыт") + "\n";
            if (!string.IsNullOrEmpty(AddressHeader))
                 headerInfo += string.IsNullOrEmpty(AddressHeader) ? string.Empty : "Адрес: " + AddressHeader + "\n";
            else headerInfo += string.IsNullOrEmpty(TerritoryHeader) ? string.Empty : "Территория: " + TerritoryHeader + "\n";
            headerInfo += string.IsNullOrEmpty(SupplierHeader) ? string.Empty : "Поставщики: " + SupplierHeader + "\n";
            headerInfo += string.IsNullOrEmpty(ServiceHeader) ? string.Empty : "Услуги: " + ServiceHeader;
            report.SetParameterValue("headerInfo", headerInfo);

        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            IsOpen = UserParamValues["IsOpen"].GetValue<int>();

            Nkvar = UserParamValues["Nkvar"].GetValue<string>() ?? String.Empty;
            NkvarN = UserParamValues["Nkvar_n"].GetValue<string>() ?? String.Empty;
            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();

            Services = UserParamValues["Services"].GetValue<List<int>>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());


            if (Month == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный месяц\"");
            }

            if (Year == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный год\"");
            }
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
                        string sql = " insert into selected_kvars (nzp_kvar, num_ls, nzp_dom) " +
                                     " select nzp_kvar, num_ls, nzp_dom from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_dolg (  "+
                               " num_ls integer default 0,"+
                               " sum_dolg " + DBManager.sDecimalType + "(14,2) default 0.00, "+
                               " sum_dolg2 " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_dolg3 " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_dolg6 " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_dolg12 " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_dolg36 " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_nach " + DBManager.sDecimalType + "(14,2) default 0.00 " + 
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" create temp table selected_kvars(nzp_kvar integer, nzp_dom integer, nzp_wp integer, num_ls integer) " + DBManager.sUnlogTempTable);
            }  
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_dolg ", true);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" DROP TABLE  selected_kvars ");
            } 
        }

    }
}

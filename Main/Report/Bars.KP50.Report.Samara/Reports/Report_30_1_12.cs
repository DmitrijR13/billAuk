using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report30112 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.12 Отчет по списку должников"; }
        }

        public override string Description
        {
            get { return "30.1.12 Отчет по списку должников"; }
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
            get { return Resources.Report_30_1_12; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Дата</summary>
        private DateTime Date { get; set; }

        /// <summary>Период долга</summary>
        private List<int> Debt { get; set; }

        /// <summary>Районы</summary>
        private string Raions { get; set; }

        /// <summary>Улицы</summary>
        private string Streets { get; set; }

        /// <summary>Дома</summary>
        private string Houses { get; set; }

        /// <summary>Услуга</summary>
        private List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }

        /// <summary> Кольчество плательщиков </summary>
        private int CounterPerson { get; set; }



        public override List<UserParam> GetUserParams()
        {
          
            DateTime datS =  DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS) { Name = "Дата"},
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AddressParameter(),
                new ComboBoxParameter(true)
                {
                    Code = "Debt",
                    Name = "Период",
                    Value = "1",
                    Require = true,
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Менее 1 месяца" },
                        new { Id = "2", Name = "1 месяц" },
                        new { Id = "3", Name = "2 месяца" },
                        new { Id = "4", Name = "3 месяца" },
                        new { Id = "5", Name = "4 месяца" },
                        new { Id = "6", Name = "5 месяцев" },
                        new { Id = "7", Name = "6 месяцев" },
                        new { Id = "8", Name = "7 месяцев" },
                        new { Id = "9", Name = "8 месяцев" },
                        new { Id = "10", Name = "9 месяцев" },
                        new { Id = "11", Name = "10 месяцев" },
                        new { Id = "12", Name = "11 месяцев" },
                        new { Id = "13", Name = "12 месяцев" },
                        new { Id = "14", Name = "Более года" }
                    }
                }
            };
        }

        protected override void PrepareParams()
        {
            DateTime date;
            var period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out date);
            Date = date;
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Debt = UserParamValues["Debt"].GetValue<List<int>>();
            var adr = UserParamValues["Address"].GetValue<AddressParameterValue>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;

            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = adr.Raions.Aggregate(Raions, (current, nzpRajon) => current + (nzpRajon + ","));
                Raions = Raions.TrimEnd(',');
                Raions = "and r.nzp_raj in (" + Raions + ") ";
            }
            else return;

            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = adr.Streets.Aggregate(Streets, (current, nzpStreet) => current + (nzpStreet + ","));
                Streets = Streets.TrimEnd(',');
                Streets = "and u.nzp_ul in (" + Streets + ") ";
            }
            else return;

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                    Houses = "and d.ndom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }


        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Января","Февраля",
                 "Марта","Апреля","Мая","Июня","Июля","Августа","Сентября",
                 "Октября","Ноября","Декабря"};
            var months1 = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            var debts = new[] {"", "До 1 месяца", "1 месяц",
                "2 месяца", "3 месяца", "4 месяца",
                "5 месяцев", "6 месяцев", "7 месяцев", "8 месяцев",
                "9 месяцев", "10 месяцев", "11 месяцев" , "12 месяцев" , "Более 12 месяцев"};


            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("day", Date.Day);
            report.SetParameterValue("month", months[Date.Month]);
            report.SetParameterValue("month1", months1[Date.Month]);
            report.SetParameterValue("year", Date.Year);
            report.SetParameterValue("count_per", CounterPerson);
        }

        private string GetPeriod(int id)
        {
            string result = string.Empty;
            switch (id)
            {
                case 1: result = "Менее 1 месяца"; break;
                case 2: result = "1 месяц"; break;
                case 3: result = "2 месяца"; break;
                case 4: result = "3 месяца"; break;
                case 5: result = "4 месяца"; break;
                case 6: result = "5 месяцев"; break;
                case 7: result = "6 месяцев"; break;
                case 8: result = "7 месяцев"; break;
                case 9: result = "8 месяцев"; break;
                case 10: result = "9 месяцев"; break;
                case 11: result = "10 месяцев"; break;
                case 12: result = "11 месяцев"; break;
                case 13: result = "12 месяцев"; break;
                case 14: result = "Более года"; break;
            }
            return result;
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            CounterPerson = 0;
            MakeSelectedKvars();

            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel AS pref " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);



            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Date.Year - 2000).ToString("00") +
                                     DBManager.tableDelimiter + "charge_" + Date.Month.ToString("00");
                string prefData = pref + DBManager.sDataAliasRest,
                    prefKernel = pref + DBManager.sKernelAliasRest;
                if (TempTableInWebCashe(chargeTable))
                {

                    sql = " CREATE TEMP TABLE t_simple_dolg ( " +
                          " nzp_kvar integer, " +
                          " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                          " sum_money " + DBManager.sDecimalType + "(14,2), " +
                          " debt " + DBManager.sDecimalType + "(14,2)," +
                          " sum_pere " + DBManager.sDecimalType + "(14,2)," +
                          " sum_tarif " + DBManager.sDecimalType + "(14,2)," +
                          " sum_charge " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                    ExecSQL(sql);


                    sql = " INSERT INTO t_simple_dolg(nzp_kvar, sum_insaldo, sum_tarif, sum_money, " +
                          " debt, sum_pere,sum_charge) " +
                          " SELECT c.nzp_kvar, sum(sum_insaldo), sum(sum_tarif), sum(sum_money), " +
                          "       sum(case when sum_insaldo - sum_money>0 then sum_money " +
                          "                when sum_insaldo>0 then sum_insaldo else 0 end ) as debt, " +
                          "       sum(case when sum_insaldo - sum_money>0 then sum_insaldo - sum_money else 0 end), " +
                          "       sum(sum_charge) " +
                          " FROM " + chargeTable + " c, selected_kvars k " +
                          " WHERE c.nzp_kvar=k.nzp_kvar " +
                          "       AND nzp_serv>1 " +
                          "       AND dat_charge is null and sum_tarif >= 0 " +
                          GetWhereServ() + GetWhereSupp() +
                          " GROUP BY 1";
                    ExecSQL(sql);

                    ExecSQL("Create index ix_tmp_sk_098 on t_simple_dolg(nzp_kvar)");
                    ExecSQL(DBManager.sUpdStat + " t_simple_dolg");

                    sql = " SELECT COUNT(DISTINCT nzp_kvar) FROM t_simple_dolg ";
                    var numPer = ExecScalar(sql);
                    CounterPerson += numPer != null ? Convert.ToInt32(numPer) : 0; 

                    foreach (int debt in Debt)
                    {
                        int monthDolgPo = debt;
                        int monthDolgS = debt - 1;
                        if (debt == 14)
                        {
                            monthDolgS = 12;
                            monthDolgPo = 100000;
                        }
                        var period = GetPeriod(debt);

                        sql = " INSERT INTO t_otchet_dolg(num_period, period, nzp_kvar, sum_insaldo, sum_money, debt, sum_pere," +
                              " sum_charge, typ_sobs) " +
                              " SELECT " + debt + " AS num_period, '" + period + "' as period, nzp_kvar, sum_insaldo, sum_money, debt, " +
                              "       sum_pere, sum_charge,name_y AS typ_sobs" +
                              " FROM t_simple_dolg c  LEFT OUTER JOIN " + prefData + "prm_1 p ON (c.nzp_kvar = p.nzp" + " AND p.nzp_prm = 110) " +
                                                    " LEFT OUTER JOIN " + prefKernel + "res_y r ON (" + DBManager.sNvlWord + "(p.val_prm" + DBManager.sConvToInt + ",0) = r.nzp_y" +
                              " AND r.nzp_res= 22)" +
                              " WHERE sum_tarif > 0 and sum_insaldo < sum_tarif * " + monthDolgPo +
                              "        AND sum_insaldo > sum_tarif * " + monthDolgS;
                        ExecSQL(sql);

                        sql = " INSERT INTO t2_otchet_dolg(num_period, nzp_kvar) " +
                              " SELECT " + debt + " AS num_period, nzp_kvar " +
                              " FROM t_simple_dolg WHERE sum_tarif > 0 ";
                        ExecSQL(sql);
                    }

                    ExecSQL("drop table t_simple_dolg");
                }
            }

            reader.Close();
            #endregion

            sql = " UPDATE t2_otchet_dolg " +
                  " SET nzp_geu = " + 
                   DBManager.sNvlWord + "(( SELECT g.nzp_geu " +
                                         " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_geu g " +
                                            " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k ON (k.nzp_geu = g.nzp_geu " +
                                                                                                                  " AND k.nzp_kvar = t2_otchet_dolg.nzp_kvar)), 0) ";
            ExecSQL(sql);

            sql = " INSERT INTO tall_otchet_dolg(num_period, period, nzp_geu, geu, sum_dolg ) " +
                  " SELECT num_period, " +
                         " period, " +
                         " g.nzp_geu, " +
                         " geu,  " +
                         " SUM(sum_pere) AS sum_dolg " +
                  " FROM t_otchet_dolg t INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k ON k.nzp_kvar = t.nzp_kvar " +
                                       " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_geu g ON g.nzp_geu = k.nzp_geu  " +
                  " GROUP BY 1,2,3,4 " ;
            ExecSQL(sql);

            sql = " UPDATE tall_otchet_dolg " +
                  " SET count_dolg = (SELECT COUNT(t.nzp_kvar)" +
                                    " FROM t_otchet_dolg t INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k ON k.nzp_kvar = t.nzp_kvar " +
                                                         " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_geu g ON (g.nzp_geu = k.nzp_geu " +
                                                                                                                              " AND g.nzp_geu = tall_otchet_dolg.nzp_geu) " +
                                    " WHERE t.num_period = tall_otchet_dolg.num_period) ";
            ExecSQL(sql);

            sql = " SELECT num_period, period, nzp_geu, geu, count_dolg, sum_dolg " +
                  " FROM tall_otchet_dolg " +
                  " ORDER BY 1, 4 ";
            DataTable dt2 = ExecSQLToTable(sql); //(CASE WHEN (all_per IS NOT NULL AND all_per <> 0) THEN (count_dolg/all_per) * 100 ELSE 0 END) AS
            dt2.TableName = "Q_master2";

            sql = " SELECT geu,  " +
                  " (case when rajon ='-' then town else rajon end) as rajon, " +
                  " ulica,  idom, ndom,  nkor,  ikvar, nkvar,  num_ls,  fio, typ_sobs, " +
                  " sum_insaldo, " +
                  " sum_money, " +
                  " debt, " +
                  " sum_pere, " +
                  " sum_charge " +
                  " FROM t_otchet_dolg t, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_geu g " +
                  " WHERE t.nzp_kvar = k.nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND st.nzp_town = r.nzp_town " +
                  " AND k.nzp_geu = g.nzp_geu " +
                  " order by 2,1,3,4,5,6,7,8";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";

            sql = " SELECT num_period, period, geu,  " +
                  " (case when rajon ='-' then town else rajon end) as rajon, " +
                  " ulica,  idom, ndom,  nkor,  ikvar, nkvar,  num_ls,  fio, typ_sobs, " +
                  " sum_insaldo, " +
                  " sum_money, " +
                  " debt, " +
                  " sum_pere, " +
                  " sum_charge " +
                  " FROM t_otchet_dolg t, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_geu g " +
                  " WHERE t.nzp_kvar = k.nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND st.nzp_town = r.nzp_town " +
                  " AND k.nzp_geu = g.nzp_geu " +
                  " order by 1,3,2,4,5,6,7,8,9,10";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt2);
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt);
            return ds;
        }


        private void MakeSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                int startIndex = Constants.cons_Webdata.IndexOf("Database=", StringComparison.Ordinal) + 9;
                int endIndex = Constants.cons_Webdata.Substring(startIndex, Constants.cons_Webdata.Length - startIndex).IndexOf(";", StringComparison.Ordinal);
                var tSpls = Constants.cons_Webdata.Substring(startIndex, endIndex) + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";
                if (TempTableInWebCashe(tSpls))
                {
                    string sql = " insert into selected_kvars (nzp_kvar) " +
                                 " select nzp_kvar from " + tSpls;
                    ExecSQL(sql);
                }
            }
            else
            {
                string sql = " insert into selected_kvars  " +
                             " select k.nzp_kvar " +
                             " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                             "      " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                             "      " + ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                             "      " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                             " WHERE k.nzp_dom = d.nzp_dom " +
                             " AND d.nzp_ul = u.nzp_ul " +
                             " AND u.nzp_raj = r.nzp_raj " +
                             Raions + Streets + Houses;
                ExecSQL(sql);
            }
            ExecSQL("Create index ix_tmp_sk_09 on selected_kvars(nzp_kvar)");
            ExecSQL(DBManager.sUpdStat + " selected_kvars");
        }

        /// <summary>Ограничение на услуги</summary>
        private string GetWhereServ()
        {
            var result = String.Empty;
            result = Services != null ? Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ",")) :
                                            ReportParams.GetRolesCondition(Constants.role_sql_serv);
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND nzp_serv in (" + result + ")" : String.Empty;
            return result;
        }



        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) :
                                                ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND c.nzp_supp in (" + whereSupp + ")" : String.Empty;
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            whereWp = Banks != null ? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ",")) :
                                        ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_otchet_dolg ( " +
                               " num_period integer, " +
                               " period char(50), " +
                               " nzp_kvar integer, " +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " sum_money " + DBManager.sDecimalType + "(14,2), " +
                               " debt " + DBManager.sDecimalType + "(14,2)," +
                               " sum_pere " + DBManager.sDecimalType + "(14,2)," +
                               " sum_charge " + DBManager.sDecimalType + "(14,2)," +
                               " typ_sobs CHARACTER(60)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table selected_kvars(" +
                  " nzp_kvar integer) " +
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t2_otchet_dolg ( " +
                               " num_period integer, " +
                               " nzp_geu INTEGER, " +
                               " geu CHARACTER(60), " +
                               " nzp_kvar integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE tall_otchet_dolg ( " +
                               " num_period integer, " +
                               " period char(50), " +
                               " nzp_geu INTEGER, " +
                               " geu CHARACTER(60), " +
                               " count_dolg INTEGER DEFAULT 0, " +
                               " all_per INTEGER DEFAULT 0, " +
                               " sum_dolg " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_otchet_dolg ");
            ExecSQL(" DROP TABLE t2_otchet_dolg ");
            ExecSQL(" DROP TABLE tall_otchet_dolg ");
            ExecSQL(" drop table selected_kvars ", true); //
            if (TempTableInWebCashe("t_simple_dolg"))
                ExecSQL(" DROP TABLE t_simple_dolg ");
        }
    }
}

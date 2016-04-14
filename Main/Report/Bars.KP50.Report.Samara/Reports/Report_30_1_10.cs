using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
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
    class Report30110 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.10 Списки должников"; }
        }

        public override string Description
        {
            get { return "Списки должников"; }
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
            get { return Resources.Report_30_1_10; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Дата</summary>
        protected DateTime Date { get; set; }

        /// <summary>Период долга</summary>
        protected int Debt { get; set; }

        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }

        /// <summary>Услуга</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }



        public override List<UserParam> GetUserParams()
        {

            DateTime datS = DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS) { Name = "Дата"},
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AddressParameter(),
                new ComboBoxParameter
                {
                    Code = "Debt",
                    Name = "Период",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "До 1 месяца" },
                        new { Id = "2", Name = "От 1 до 3 месяцев" },
                        new { Id = "3", Name = "От 3 до 6 месяцев" },
                        new { Id = "4", Name = "От 6 месяцев до 1 года" },
                        new { Id = "5", Name = "От 1 года до 2 лет" },
                        new { Id = "6", Name = "От 2 года до 3 лет" },
                        new { Id = "7", Name = "Свыше 1 месяца" },
                        new { Id = "8", Name = "Свыше 3 месяцев" },
                        new { Id = "9", Name = "Свыше 6 месяцев" },
                        new { Id = "10", Name = "Свыше 1 года" },
                        new { Id = "11", Name = "Свыше 2 лет" },
                        new { Id = "12", Name = "Свыше 3 лет" }
                    }
                }
            };
        }

        protected override void PrepareParams()
        {
            DateTime date;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out date);
            Date = date;
            Services = UserParamValues["Services"].GetValue<List<int>>();
            AddressParameterValue adr = JsonConvert.DeserializeObject<AddressParameterValue>(UserParamValues["Address"].Value.ToString());

            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = "and r.nzp_raj in (" + String.Join(",", adr.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = "and u.nzp_ul in (" + String.Join(",", adr.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                Houses = "and d.ndom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }

            Debt = UserParamValues["Debt"].GetValue<int>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Января","Февраля",
                 "Марта","Апреля","Мая","Июня","Июля","Августа","Сентября",
                 "Октября","Ноября","Декабря"};
            var debts = new[] {"", "До 1 месяца", "От 1 до 3 месяцев",
                "От 3 до 6 месяцев", "От 6 месяцев до 1 года", "От 1 года до 2 лет",
                "От 2 года до 3 лет", "Свыше 1 месяца", "Свыше 3 месяцев", "Свыше 6 месяцев",
                "Свыше 1 года", "Свыше 2 лет", "Свыше 3 лет" };

            report.SetParameterValue("period", debts[Debt]);

            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString()); 
            report.SetParameterValue("day", Date.Day);
            report.SetParameterValue("month", months[Date.Month]);
            report.SetParameterValue("year", Date.Year);
            var agent = ExecSQLToTable(" select distinct val_prm from " +
                                         ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 where nzp_prm = 80 and is_actual = 1 " + 
                                       " and dat_s <'" + Date.ToShortDateString() + "' and dat_po> '" + Date.ToShortDateString() + "' ");
            report.SetParameterValue("agent", agent.Rows.Count > 0 ? agent.Rows[0][0].ToString().Trim() : "ООО \"Ассоциация Управляющих Компаний\"");
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            var sql = new StringBuilder();
            #region выборка в temp таблицу

            sql.Append(" SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp());

            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Date.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Date.Month.ToString("00");

                if (TempTableInWebCashe(chargeTable))
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_svod(nzp_kvar, sum_insaldo, sum_money, debt) " +
                               " SELECT nzp_kvar, sum_insaldo, sum_money, sum_insaldo - sum_money" +
                               " FROM " + chargeTable +
                               " WHERE ");
                    if (Debt == 1) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) < 1 ");
                    if (Debt == 2) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 1 and " +
                                                    " (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) < 3 ");
                    if (Debt == 3) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 3 and " +
                                                    " (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) < 6 ");
                    if (Debt == 4) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 6 and  " +
                                                    " (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) < 12 ");
                    if (Debt == 5) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 12 and " +
                                                    " (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) < 24 ");
                    if (Debt == 6) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 24 and  " +
                                                    " (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) < 36 ");
                    if (Debt == 7) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 1 ");
                    if (Debt == 8) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 3 ");
                    if (Debt == 9) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 6 ");
                    if (Debt == 10) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 12 ");
                    if (Debt == 11) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 24 ");
                    if (Debt == 12) sql.Append(" (case when sum_tarif <> 0 then sum_insaldo/sum_tarif end ) >= 36 ");
                    sql.Append(" and dat_charge is null and nzp_serv>1 and sum_insaldo > sum_money " + GetWhereServ() + GetWhereSupp());
                    ExecSQL(sql.ToString());
                }
            }

            reader.Close();
            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT geu, " +
                " case when rajon='-' then town else rajon end as rajon, " +
                " ulica, ndom, " + 
                " case when nkor<>'-' then nkor end as nkor, " +
                " case when nkvar<>'-' and nkvar<>'0' then 'кв.'||nkvar end as nkvar, num_ls, fio, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo, sum(sum_money) as sum_money, sum(debt) as debt ");
            sql.Append(" from t_svod ts, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "dom d, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_geu g ");
            sql.Append(" where ts.nzp_kvar = k.nzp_kvar ");
            sql.Append(" and k.nzp_dom = d.nzp_dom ");
            sql.Append(" and d.nzp_ul = u.nzp_ul ");
            sql.Append(" and u.nzp_raj = r.nzp_raj ");
            sql.Append(" and r.nzp_town = t.nzp_town ");
            sql.Append(" and k.nzp_geu = g.nzp_geu ");
            sql.Append(Raions + Streets + Houses);
            sql.Append(" group by 1,2,3,4,5,6,7,8 "); 
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>Ограничение на услуги</summary>
        private string GetWhereServ()
        {
            var result = String.Empty;
            if (Services != null)
            {
                result = Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
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
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
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

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_svod ( " +
                               " nzp_kvar integer, " +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " sum_money " + DBManager.sDecimalType + "(14,2), " +
                               " debt " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_svod ");
        }
    }
}

using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Samara
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using Report;
    using Base;
    using Properties;
    using Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Agent_rasch : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.3.1 Свод расчетов по агентским договорам"; }
        }

        public override string Description
        {
            get { return "Свод расчетов по агентским договорам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Finans};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Agent_rasch; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>дата с</summary>
        protected DateTime DatS { get; set; }

        /// <summary>дата по</summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {
           
            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new SupplierAndBankParameter()
            };
        }

        protected override void PrepareReport(Report report)
        {
            string[] months =
            {"","январь","февраль",
                "март","апрель","май","июнь","июль","август","сентябрь",
                "октябрь","ноябрь","декабрь"};
            report.SetParameterValue("dat_s", DatS.Day +"." + DatS.Month.ToString("00") + "." + DatS.Year.ToString("00") + "г.");
            report.SetParameterValue("dat_po", DatPo.Day + "." + DatPo.Month + "." + DatPo.Year.ToString("00") + "г.");
            report.SetParameterValue("dat", months[DatS.Month] + " " + DatS.Year + " года");
            // report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            // report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("dz", "12312312312");
            report.SetParameterValue("eirc", "12312312312");
            report.SetParameterValue("town", "г.о. Жигулевск");
        }


        protected override void PrepareParams()
        {
            DatS = Convert.ToDateTime("01." + UserParamValues["Month"].GetValue<int>() + "." + UserParamValues["Year"].GetValue<int>());
            DatPo = DatS.AddMonths(1).AddDays(-1);

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader;

            sql.Remove(0, sql.Length);
            sql.Append(" select bd_kernel as pref ");
            sql.AppendFormat(" from {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" where nzp_wp>1 " + GetwhereWp());


            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_agent_rasch " +
                    " select service, ordering AS sort, name_supp, " +
                    " sum(sum_insaldo) as sum_insaldo_d, " +
                    " sum(0) as sum_insaldo_k, " +
                    " sum(sum_charge) as sum_charge, " +
                    " sum(sum_money) as sum_money, " +
                    " sum(sum_outsaldo) as sum_outsaldo_d, " +
                    " sum(0) as sum_outsaldo_k " +
                    " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
                        pref + "_charge_" + (DatS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + DatS.Month.ToString("00") + " ch" +
                    " where se.nzp_serv = ch.nzp_serv " +
                    " and su.nzp_supp = ch.nzp_supp " +
                    " and se.nzp_serv = 15 " + GetWhereSupp() +
                    " group by 1,2,3 ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_agent_rasch1 " +
                    " select name_supp, " +
                    " sum(sum_insaldo) as sum_insaldo_d, " +
                    " sum(0) as sum_insaldo_k, " +
                    " sum(sum_charge) as sum_charge, " +
                    " sum(sum_money) as sum_money, " +
                    " sum(sum_outsaldo) as sum_outsaldo_d, " +
                    " sum(0) as sum_outsaldo_k " +
                    " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
                        pref + "_charge_" + (DatS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + DatS.Month.ToString("00") + " ch" +
                    " where se.nzp_serv = ch.nzp_serv " +
                    " and su.nzp_supp = ch.nzp_supp " + GetWhereSupp() + 
                    " group by 1 ");
                ExecSQL(sql.ToString());
                
            }

            sql.Remove(0, sql.Length);
            sql.Append(" select * from t_agent_rasch order by 2 ");
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            sql.Remove(0, sql.Length);
            sql.Append(" select * from t_agent_rasch1 order by 1 ");
            DataTable dt1 = ExecSQLToTable(sql.ToString());
            dt1.TableName = "Q_master1";
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);

            return ds;
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
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND ch.nzp_supp in (" + whereSupp + ")" : String.Empty;
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
            var sql = new StringBuilder();
            sql.Append("create temp table t_agent_rasch (" +
                " service character(100), " +
                " sort INTEGER, " +
                " name_supp character(100), " +
                " sum_insaldo_d " + DBManager.sDecimalType + "(14,2), " +
                " sum_insaldo_k " + DBManager.sDecimalType + "(14,2), " +
                " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                " sum_money " + DBManager.sDecimalType + "(14,2), " +
                " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2) ) "
                );
            ExecSQL(sql.ToString());
            sql.Remove(0,sql.Length);
            sql.Append("create temp table t_agent_rasch1 (" +
                " name_supp character(100), " +
                " sum_insaldo_d " + DBManager.sDecimalType + "(14,2), " +
                " sum_insaldo_k " + DBManager.sDecimalType + "(14,2), " +
                " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                " sum_money " + DBManager.sDecimalType + "(14,2), " +
                " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2) ) "
                );
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_agent_rasch ");
            ExecSQL(" drop table t_agent_rasch1 ");
        }

    }
}

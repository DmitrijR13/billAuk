

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using FastReport.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;


namespace Bars.KP50.Report.Tula.Reports
{
    /// <summary>Сводный отчет по начислениям для Тулы</summary>
    class Test_wp_supp : BaseSqlReport
    {
        public override string Name
        {
            get { return "тестовый отчет для SupplierAndBankParameter"; }
        }

        public override string Description
        {
            get { return "тест"; }
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
            get { return Resources.Test_wp_supp; }
        }
        public override IList<ReportKind> ReportKinds
        {
            get {return new List<ReportKind> { ReportKind.Base };}
        }

        /// <summary>Suppliers</summary>
        protected List<int> Suppliers { get; set; }

        /// <summary>Suppliers</summary>
        protected List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new SupplierAndBankParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            string sql = " SELECT bd_kernel as pref " +
             " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
             " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                sql = " insert into t_svod (pref, name_supp) " + 
                      " SELECT '" + pref + "' as pref, name_supp from " + pref + DBManager.sKernelAliasRest + "supplier " + 
                      " where 1 = 1 " + GetWhereSupp();
                ExecSQL(sql);
            }


            DataTable dt = ExecSQLToTable(" select * from t_svod ");
            dt.TableName = "Q_master";


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
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

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("my_wp_supp", "");
        }

        protected override void PrepareParams()
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }
        protected override void CreateTempTable()
        {
            ExecSQL(" create temp table t_svod (pref char(10), name_supp char (30)) ");
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ");
        }
    }
}
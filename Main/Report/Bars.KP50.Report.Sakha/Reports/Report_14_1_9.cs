using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Sakha.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
    public class Report140109 : BaseSqlReport
    {
      public override string Name
        {
            get { return "14.1.9 Свод по поступлениям за ЖКУ"; }
        }

        public override string Description
        {
            get { return "Свод по поступлениям за ЖКУ"; }
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
            get { return Resources.Report_14_1_9; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        #region параметры
        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный день </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Агенты</summary>
        protected List<int> Agents { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AgentHeader { get; set; }  

        /// <summary>Территория</summary>
        protected List<int> Banks { get; set; }

        /// <summary>
        /// Список схем данных
        /// </summary>
        private List<string> PrefList { get; set; }


        #endregion

        public override List<UserParam> GetUserParams()
        {              
            DateTime datS=DateTime.Now;
            DateTime datPo=DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new BankParameter(),
                new AgentParameter()
            };
        }
        protected override void PrepareParams()
        {
            Agents = UserParamValues["Agent"].Value.To<List<int>>();
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
        }

        public override DataSet GetData()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;
            string sql;

            PrefList = GetPrefList();
            
            #region Выборка по банкам
            foreach (string curPref in PrefList)
            {
                for (var i = DatS.Year*12 + DatS.Month; i <= DatPo.Year*12 + DatPo.Month ; i++)
                {
                    var year = i / 12;
                    var month = i % 12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }
                    string pack_ls = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                     DBManager.tableDelimiter + "pack_ls";

                    string pack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "pack";  

                    string supplier = curPref + "_charge_" + (year - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "fn_supplier" + month.ToString("00");

                    sql = " INSERT INTO t_14_9_sums (nzp_bank, nzp_area, sum_money, sum_peni) " +
                          " SELECT nzp_bank, nzp_area, sum (case when nzp_serv<>500 then sum_prih else 0 end), sum (case when nzp_serv=500 then sum_prih else 0 end)  " +
                          " FROM " + pack_ls + " pl, " +
                          pack + " p, " +
                          supplier + " s, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "kvar k" +
                          "  WHERE  s.nzp_pack_ls=pl.nzp_pack_ls and p.nzp_pack=pl.nzp_pack and k.num_ls=pl.num_ls" + GetWhereAgent("s.nzp_supp") +
                          "  AND  p.dat_uchet >= '" + DatS.ToShortDateString() +
                          "' AND p.dat_uchet <=  '" + DatPo.ToShortDateString() + "'" +
                          "  GROUP BY  1 , 2";
                    ExecSQL(sql);

                }

            }
            #endregion

            #region Выборка на экран

            sql = " SELECT  sum(sum_money) as sum , sum(sum_peni) as peni,  area as area " +
                  " FROM t_14_9_sums t, "
                  + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a"
                  + " WHERE t.nzp_area=a.nzp_area "
                  + " GROUP BY area "
                  + " ORDER BY area ";
            
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

        protected override void PrepareReport(FastReport.Report report)
        {    
            string period = " с " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString();
            string headerParam = !string.IsNullOrEmpty(AgentHeader) ? "Агенты: " + AgentHeader + "\n" : "Агенты: Все\n";
            headerParam += !string.IsNullOrEmpty(TerritoryHeader) ? "Округа: " + TerritoryHeader + "" : "Все округа"; 
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("pPeriod", period);
        } 


        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_14_9_sums (     " +
                               " nzp_bank integer default 0," +
                               " nzp_area integer default 0," +
                               " sum_money " + DBManager.sDecimalType + "(14,2) default 0.00, " + // Принято
                               " sum_peni " + DBManager.sDecimalType + "(14,2) default 0.00 " + // Пени
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_14_9_sums ", false);
        }

        /// <summary>
        /// Получение префиксов БД
        /// </summary>
        /// <returns></returns>
        private List<string> GetPrefList()
        {
            var prefList = new List<string>();
            MyDataReader reader;
            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                prefList.Add(Convert.ToString(reader["pref"]).Trim());
            }
            reader.Close();
            return prefList;
        }


        /// <summary>Ограничение по банкам данных</summary>
        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            whereWp = Banks != null
                ? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            TerritoryHeader = "";
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

        /// <summary>
        /// Получить условия органичения по агентам
        /// </summary>
        /// <returns></returns>
        private string GetWhereAgent(string fieldPref)
        {
            string whereSupp = String.Empty;
            string sup = string.Empty;
            if (Agents != null)
                {    
                    sup = Agents.Aggregate(sup, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_agent in (" + sup.TrimEnd(',') + ")";
                }
  
            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');

            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {  
                AgentHeader = String.Empty;
                string sql = " SELECT payer from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer " +
                             ( String.IsNullOrEmpty(sup.TrimEnd(','))?"":"WHERE nzp_payer in (" + sup.TrimEnd(',') + ")");
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    AgentHeader +=  dr["payer"].ToString().Trim() + ", ";
                }
                AgentHeader = AgentHeader.TrimEnd(',', ' ');
            }
            return String.IsNullOrEmpty(whereSupp) ? string.Empty : " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }



    }


}

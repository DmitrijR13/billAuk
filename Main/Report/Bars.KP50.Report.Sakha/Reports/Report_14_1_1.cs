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
    public class Report140101 : BaseSqlReport
    {
      public override string Name
        {
            get { return "14.1.1 Реестр поступлений по агентам"; }
        }

        public override string Description
        {
            get { return "Реестр поступлений по агентам"; }
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
            get { return Resources.Report_14_1_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        #region параметры
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Месяц</summary>
        protected int Month { get; set; }

        /// <summary>Год</summary>
        protected int Year { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }
        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }
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
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц", Value = DateTime.Today.Month},
                new YearParameter {Name = "Год", Value = DateTime.Today.Year},
                new AreaParameter(),
                new BankParameter()
            };
        }
        protected override void PrepareParams()
        {

            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
        }

        public override DataSet GetData()
        {
           
            string sql;

            string pack_ls=  ReportParams.Pref + "_fin_" + (Year - 2000).ToString("00") +
            DBManager.tableDelimiter + "pack_ls";

            string pack = ReportParams.Pref + "_fin_" + (Year - 2000).ToString("00") +
                         DBManager.tableDelimiter + "pack";  

            PrefList = GetPrefList();
            
            #region Выборка по банкам
            foreach (string curPref in PrefList)
            {
                string supplier = curPref + "_charge_" + (Year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "fn_supplier" + Month.ToString("00");

                sql = " INSERT INTO t_14_1_sums (nzp_bank, nzp_area, sum_money, sum_peni) " +
                      " SELECT nzp_bank, nzp_area, sum (case when nzp_serv<>500 then sum_prih else 0 end), sum (case when nzp_serv=500 then sum_prih else 0 end)  " +
                      " FROM " + pack_ls + " pl, " +
                                 pack + " p, " +
                                 supplier + " s, " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "kvar k" +
                      "  WHERE  s.nzp_pack_ls=pl.nzp_pack_ls and p.nzp_pack=pl.nzp_pack and k.num_ls=pl.num_ls" +GetWhereArea()+
                      "  GROUP BY  1 , 2";
                   ExecSQL(sql);

            }
            #endregion

            #region Выборка на экран

            sql = " SELECT  sum_money as sum , sum_peni as peni, sb.bank as bank, sp.payer as agent, area as area " +
                  " FROM t_14_1_sums t,  " + ReportParams.Pref + DBManager.sKernelAliasRest +"s_bank sb, "+ ReportParams.Pref + DBManager.sKernelAliasRest +"s_payer sp, "
                  + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a"
                  + " WHERE sb.nzp_bank=t.nzp_bank and sp.nzp_payer=sb.nzp_payer and t.nzp_area=a.nzp_area "
                  +" ORDER BY area, agent, bank";
            
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
            var months = new[]
            {
                "", "Январь", "Февраль",
                "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
                "Октябрь", "Ноябрь", "Декабрь"
            };

            string headerParam = !string.IsNullOrEmpty(AreaHeader) ? "Управляющая компания: " + AreaHeader + "\n" : "Управляющая компания: Все\n";
            headerParam += "Вид денег: Все виды\n";
            headerParam += "Агент: Все агенты\n";
            headerParam += !string.IsNullOrEmpty(TerritoryHeader) ? "Округа: " + TerritoryHeader + "" : "Все округа"; 
            //headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("pmonth", "За "+months[Month]+ " месяц "+Year);
        }


        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                AreaHeader = String.Empty;
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }



        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_14_1_sums (     " +
                               " nzp_bank integer default 0," +
                               " nzp_area integer default 0," +
                               " sum_money " + DBManager.sDecimalType + "(14,2) default 0.00, " + // Принято
                               " sum_peni " + DBManager.sDecimalType + "(14,2) default 0.00 " + // Пени
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_14_1_sums ", false);
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

    }


}

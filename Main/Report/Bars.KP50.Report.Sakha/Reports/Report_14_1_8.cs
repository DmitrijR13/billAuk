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
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
    public class Report140108 : BaseSqlReport
    {
      public override string Name
        {
            get { return "14.1.8 Реестр снятий по постащикам"; }
        }

        public override string Description
        {
            get { return "Реестр снятий по поставщикам"; }
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
            get { return Resources.Report_14_1_8; }
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
        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }    
        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }      
        /// <summary>Список схем данных </summary>
        private List<string> PrefList { get; set; }

        #endregion

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц", Value = DateTime.Today.Month},
                new YearParameter {Name = "Год", Value = DateTime.Today.Year},
                new AreaParameter(),
                new SupplierAndBankParameter(),
            };
        }
        protected override void PrepareParams()
        {

            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
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
           
            string sql;

            PrefList = GetPrefList();
            
            #region Выборка по банкам
            foreach (string curPref in PrefList)
            {
                string charge = curPref + "_charge_" + (Year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "charge_" + Month.ToString("00");

                sql = " INSERT INTO t_14_8_sums (nzp_area, nzp_supp, nzp_serv, is_device, sum_money) " +
                      " SELECT nzp_area, nzp_supp,nzp_serv, is_device, sum (real_charge - sum_tarif)  " +
                      " FROM " + charge + " ch, " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "kvar k" +
                      "  WHERE  k.num_ls=ch.num_ls " + GetWhereArea() + GetWhereSupp()+
                      "  GROUP BY nzp_area, nzp_supp, nzp_serv, is_device ";
                   ExecSQL(sql);

            }
            #endregion

            #region Выборка на экран

            sql = " SELECT  area as area,sp.name_supp as supplier, s.service as service, (case when is_device=1 then 'Приборы учета' else '' end) as device, sum_money as sum  " +
                  " FROM t_14_8_sums t,  " + ReportParams.Pref + DBManager.sKernelAliasRest +"services s, "+ ReportParams.Pref + DBManager.sKernelAliasRest +"supplier sp, "
                  + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a"
                  + " WHERE s.nzp_serv=t.nzp_serv and sp.nzp_supp=t.nzp_supp and t.nzp_area=a.nzp_area "
                  + " ORDER BY 1,2,3,4";
            
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
            string SuppliersHeader = GetListNameSupp();

            string headerParam = !string.IsNullOrEmpty(AreaHeader) ? "Управляющая компания: " + AreaHeader + "\n" : "Управляющая компания: Все\n";
            headerParam += "Вид снятия: Все виды";
            headerParam += !string.IsNullOrEmpty(SuppliersHeader) ? "Поставщики: " + SuppliersHeader + "\n" : "Все поставщики\n";
            headerParam += !string.IsNullOrEmpty(TerritoryHeader) ? "Округа: " + TerritoryHeader + "\n" : "Все округа\n";

            headerParam = headerParam.TrimEnd('\n');
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
            const string sql = " create temp table t_14_8_sums (     " +
                               " nzp_supp integer default 0," +
                               " nzp_area integer default 0," +
                               " nzp_serv integer default 0," +
                               " is_device integer default 0," +
                               " sum_money " + DBManager.sDecimalType + "(14,2) default 0.00 " + 
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_14_8_sums ", false);
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
        /// Получает наименования поставщиков
        /// </summary>
        /// <returns></returns>
        private string GetListNameSupp()
        {
            string names = "Все поставщики";
            if (Suppliers != null)
            {
                string sql = " SELECT name_supp FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier ch " +
                             " WHERE 1=1 " + GetWhereSupp();
                DataTable dt = ExecSQLToTable(sql);
                string[] listNames = new string[dt.Rows.Count];
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    listNames[i] = dt.Rows[i]["name_supp"] != DBNull.Value
                        ? dt.Rows[i]["name_supp"].ToString().Trim()
                        : "";
                }
                names = string.Join(",", listNames);
            }
            return names;
        }


    }


}

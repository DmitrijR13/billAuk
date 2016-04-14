using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Sakha.Properties;
using Bars.KP50.Utils;
using Castle.MicroKernel.ModelBuilder.Descriptors;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
    public class Report140110 : BaseSqlReport
    {
      public override string Name
        {
            get { return "14.1.10 Отчет кассира"; }
        }

        public override string Description
        {
            get { return "Отчет кассира"; }
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
            get { return Resources.Report_14_1_10; }
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

        /// <summary>Место формирования</summary>
        protected int CheckoutCounter { get; set; }

        /// <summary>Территория</summary>
        protected List<int> Banks { get; set; }

        /// <summary>
        /// Список схем данных
        /// </summary>
        private List<string> PrefList { get; set; }
        /// <summary>Заголовок место формирования</summary>
        protected string CheckoutCounterHeader { get; set; }


        #endregion

        public override List<UserParam> GetUserParams()
        {
            DateTime datS = DateTime.Now;
            DateTime datPo = DateTime.Now;

            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new BankParameter(),
                new CheckoutCounterParameter()
            };
        }
        protected override void PrepareParams()
        {
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            CheckoutCounter = UserParamValues["CheckoutCounter"].GetValue<int>();
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
            string whereCheckoutCounter = GetCheckoutCounter();
            #region Выборка по банкам
            foreach (string curPref in PrefList)
            {
                for (var i = DatS.Year * 12 + DatS.Month; i <= DatPo.Year * 12 + DatPo.Month; i++)
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

                    sql = " INSERT INTO t_14_10_sums (dat_uchet, nzp_bank, nzp_serv, sum_money) " +
                          " SELECT p.dat_uchet, nzp_bank, s.nzp_serv, sum (sum_prih)  " +
                          " FROM " + pack_ls + " pl, " +
                          pack + " p, " +
                          supplier + " s " + 
                          "  WHERE  s.nzp_pack_ls=pl.nzp_pack_ls and p.nzp_pack=pl.nzp_pack " + 
                          " AND  p.dat_uchet >= '" + DatS.ToShortDateString() +
                          "' AND p.dat_uchet <=  '" + DatPo.ToShortDateString() + "'" +
                          whereCheckoutCounter +
                          "  GROUP BY  1,2,3";
                    ExecSQL(sql);

                }  
            }

            for (var i = DatS.Year; i <= DatPo.Year; i++)
            {
                string pack_ls = ReportParams.Pref + "_fin_" + (i - 2000).ToString("00") +
                 DBManager.tableDelimiter + "pack_ls";

                string pack = ReportParams.Pref + "_fin_" + (i - 2000).ToString("00") +
                              DBManager.tableDelimiter + "pack";
                
                sql = " INSERT INTO t_14_10_sums (dat_uchet, nzp_bank, service, sum_money) " +
                      " SELECT p.dat_uchet, nzp_bank, 'прочие платежи', sum (g_sum_ls)  " +
                      " FROM " + pack_ls + " pl, " +
                      pack + " p " +
                      "  WHERE  p.nzp_pack=pl.nzp_pack and pl.dat_uchet is null " +
                      " AND  p.dat_uchet >= '" + DatS.ToShortDateString() +
                      "' AND p.dat_uchet <=  '" + DatPo.ToShortDateString() + "'" +
                      whereCheckoutCounter +
                      "  GROUP BY  1,2,3";
                ExecSQL(sql); 
            }


            sql = " UPDATE t_14_10_sums SET service = (" +
                      " SELECT service " +
                      " FROM "+ReportParams.Pref + DBManager.sKernelAliasRest +"services s " +
                      " WHERE s.nzp_serv=t_14_10_sums.nzp_serv)"+
                      " WHERE service is null " ;
               ExecSQL(sql);


            #endregion

            #region Выборка на экран

            sql = " SELECT dat_uchet as dat_uchet, sb.bank as bank, service, sum(sum_money) as sum " +
                  " FROM t_14_10_sums t,  " + ReportParams.Pref + DBManager.sKernelAliasRest +"s_bank sb " +
                  " WHERE sb.nzp_bank=t.nzp_bank  " +
                  " GROUP BY 1,2,3 " +
                  " ORDER BY 1,2,3";
            
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

        }    

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_14_10_sums (     " +
                               " dat_uchet date," +
                               " nzp_bank integer default 0," +
                               " nzp_serv integer default 0," +
                               " service  char(100)," +
                               " sum_money " + DBManager.sDecimalType + "(14,2) default 0.00 " + // Принято
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_14_10_sums ", false);
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
        /// Получить условия органичения по кассам
        /// </summary>
        /// <returns></returns>
        private string GetCheckoutCounter()
        {
            if ((CheckoutCounter != 0))
            {
                string sql = " SELECT bank FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank WHERE nzp_bank in (" + CheckoutCounter + ")";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    CheckoutCounterHeader += row["bank"].ToString().Trim() + ", ";
                }
                CheckoutCounterHeader = CheckoutCounterHeader.TrimEnd(',', ' ');
            }
            else
            {
                CheckoutCounterHeader = String.Empty;
            }

            string Res = (CheckoutCounter != 0) ? " and p.nzp_bank in ( "+ CheckoutCounter + ")" : String.Empty;
            return Res;
        }

    }


}

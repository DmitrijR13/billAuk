using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;


namespace Bars.KP50.Report.Tula.Reports
{
    public class Report7132 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.3.2 Справка по реквизитам поставщиков "; }
        }

        public override string Description
        {
            get { return "Справка по реквизитам поставщиков для Тулы"; }
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
            get { return Resources.Report_71_3_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>УК</summary>
        protected string AreasHeader { get; set; }

        /// <summary>Поставщики</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            DateTime datS = curCalcMonthYear != null
                ? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
                    Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
                : DateTime.Now;
            DateTime datPo = curCalcMonthYear != null
                ? datS.AddMonths(1).AddDays(-1)
                : DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new BankSupplierParameter(),
                new ServiceParameter()
            }; 
        }

        public override DataSet GetData()
        {
            /* Валидация параметров */

            MyDataReader reader;
            string whereServ = GetWhereServ();
            string whereSupp = GetWhereSupp("t.nzp_supp");

            var sql = " SELECT * "+
                      " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point "+
                      " WHERE nzp_wp>1 " + GetwhereWp();


            ExecRead(out reader, sql);
            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();
                var tarifTable = pref + "_data" + DBManager.tableDelimiter + "tarif ";

                sql = " insert into t_svod(nzp_serv, nzp_supp) " +
                      " select nzp_serv, nzp_supp " +
                      "       from " + tarifTable + 
                      " t , " + pref + DBManager.sDataAliasRest + "kvar k " +
                      " where is_actual=1 and t.nzp_kvar = k.nzp_kvar " +
                         whereSupp + whereServ +
                      " GROUP BY  1,2           ";
                ExecSQL(sql);


            }
            reader.Close();


            #region Выборка на экран

            sql = "SELECT payer, npayer, service, inn, kpp, rcount , " +
                  "       (case when b.nzp_prm=505 then val_prm end) as ur_adr, " +
                  "       (case when b.nzp_prm=1269 then val_prm end) as fact_adr  " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer a, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su " +
                  " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_9 b " +
                  "       ON su.nzp_payer_supp=b.nzp and b.nzp_prm in (505,1269) " +
                  "           AND b.is_actual=1 " +
                  "           AND b.dat_s<= '" + DatS.ToShortDateString() +"' " +
                  "           AND b.dat_po>='" + DatPo.ToShortDateString() + "' " +
                  " LEFT OUTER JOIN  " + ReportParams.Pref + DBManager.sDataAliasRest + "fn_bank f " +
                  " ON su.nzp_payer_supp=f.nzp_payer, " +
                  " t_svod lf,  " + ReportParams.Pref + DBManager.sKernelAliasRest + "services s " +
                  " WHERE su.nzp_supp=lf.nzp_supp and a.nzp_payer = su.nzp_payer_supp " +
                  " AND lf.nzp_serv=s.nzp_serv " +
                  " GROUP BY 1,2,3,4,5,6,7,8 ";
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

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
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
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',',' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');

            }
            return whereServ;
        }

        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
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
                TerritoryHeader = String.Empty;
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
            string period;

            if (DatS == DatPo)
            {
                period = DatS.ToShortDateString() + " г.";
            }
            else
            {
                period = "период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
            }
            report.SetParameterValue("period", period);

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DatS = begin;
            DatPo = end;

            Services = UserParamValues["Services"].GetValue<List<int>>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_svod (     " +
                               " nzp_serv integer, " +
                               " nzp_supp integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}

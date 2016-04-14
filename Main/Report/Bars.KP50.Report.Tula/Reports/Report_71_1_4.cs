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
    class Report7114 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.4 Список по МКД/ Частному сектору"; }
        }

        public override string Description
        {
            get { return "Список по МКД/ Частному сектору"; }
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
            get { return Resources.Report_71_1_4; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>МКД/частный сектор</summary>
        protected int Mkd { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

      /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter(),
                new ServiceParameter(),
                new ComboBoxParameter
                {
                    Code = "Mkd",
                    Name = "МКД/Частный сектор",
                    Value = "1",
                    DefaultValue = "1",
                    
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "МКД" },
                        new { Id = "2", Name = "Частный сектор" },
                    }
                },
              
            };
        }

        public override DataSet GetData()
        {


            #region Выборка по локальным банкам

            MyDataReader reader;
            string whereWP = GetwhereWp(),
                    whereServ = GetWhereServ(),
                     whereSupp = GetWhereSupp("g.nzp_supp");
            var sql = " SELECT * " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + whereWP;
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();
                var chargeXx = pref + "_charge_" + (Year - 2000).ToString("00") +
                    DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00");

                if (TempTableInWebCashe(chargeXx) && TempColumnInWebCashe(chargeXx, "rash_norm_one"))
                {
                    sql =
                        " INSERT INTO t_spis (nzp_kvar, nzp_dom, nkvar, nkvar_n, ikvar, num_ls, nzp_serv, rash_norm_one, tarif)" +
                        " SELECT k.nzp_kvar, k.nzp_dom, nkvar, nkvar_n, ikvar,num_ls, nzp_serv, rash_norm_one, tarif " +
                        " FROM " + chargeXx + " g, " + pref + DBManager.sDataAliasRest + "kvar k" +
                        " WHERE g.nzp_kvar=k.nzp_kvar and g.stek = 3 and  0" + (Mkd == 1 ? "<" : "=") +
                        " (select count(*) from " + pref +
                        DBManager.sDataAliasRest + "prm_2 a " +
                        " where nzp_prm=2030 and is_actual=1 " +
                        "       and g.nzp_dom=a.nzp and val_prm='1')" +
                        whereServ + whereSupp;

                    ExecSQL(sql);
                }


            }
            reader.Close();

            #endregion

            #region Выборка на экран

            ExecSQL(" create index t_spis_nzp_serv on t_spis(nzp_serv) ");

            sql = " UPDATE t_spis SET service = s.service" +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services s " +
                  " WHERE t_spis.nzp_serv = s.nzp_serv";
            ExecSQL(sql);

            ExecSQL(" create index t_spis_nzp_dom on t_spis(nzp_dom) ");


            sql = " SELECT town, rajon, ulica, idom, ndom, nkor, ikvar, " +
                  " nkvar, nkvar_n, service, rash_norm_one, tarif, num_ls " +
                  " FROM t_spis a," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                  "where a.nzp_dom=d.nzp_dom " +
                  "     and d.nzp_ul=u.nzp_ul " +
                  "     and u.nzp_raj=sr.nzp_raj" +
                  "     and sr.nzp_town=st.nzp_town" +
                  " order by town, rajon, ulica, idom,ndom, nkor, ikvar, nkvar,service ";

            DataTable dt = ExecSQLToTable(sql, 300);
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
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
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
                ServiceHeader = String.Empty;
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
        
        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null &&  BankSupplier.Banks != null)
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
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("date", months[Month]+" "+Year);
            report.SetParameterValue("year", Year);

            switch (Mkd)
            {
                case 1: 
                    report.SetParameterValue("mkd", "МКД");
                    break;
                case 2: 
                    report.SetParameterValue("mkd", "частному сектору");
                    break;
            }

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Mkd = UserParamValues["Mkd"].GetValue<int>();
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

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_spis (  " +
                               " nzp_kvar integer default 0, " +
                               " nzp_dom integer default 0, " +
                               " num_ls integer default 0, " +
                               " nkvar char(10), "+
                               " nkvar_n char(10), " +
                               " ikvar integer, " +
                               " nzp_serv integer," +
                               " service char(100), " +
                               " rash_norm_one " + DBManager.sDecimalType + "(14,2) default 0, " + 
                               " tarif " + DBManager.sDecimalType + "(14,2) default 0.00 " + 
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_spis ", true);
        }

    }
}

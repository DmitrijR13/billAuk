using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.MariEl.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;

namespace Bars.KP50.Report.MariEl.Reports
{
    public class Report1203004 : BaseSqlReport
    {
        public override string Name
        {
            get { return "12.3.4 Сводный отчет по принятым и перечисленным средствам (УК)"; }
        }

        public override string Description
        {
            get { return "Сводный отчет по принятым и перечисленным средствам для Тулы"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_12_3_4; }
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
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок услуг</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }


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
                new ComboBoxParameter
                {
                    Code = "ReportTitle",
                    Name = "Заголовок отчета",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "за жилищно-коммунальные услуги"},
                        new {Id = "2", Name = "за наем жилого помещения"},
                        new {Id = "3", Name = "за жилищные услуги"},
                        new {Id = "4", Name = "за коммунальные услуги"}
                    }
                },
                new BankSupplierParameter(),
                new ServiceParameter(),
            };
        }

        public override DataSet GetData()
        {
            string whereSupp = GetWhereSupp("a.nzp_supp"),
                whereWp=GetwhereWp(),
                whereServ=GetWhereServ();
            
            string sql = "create temp table sel_kvar(" +
                         " nzp_area integer," +
                         " nzp_wp integer," +
                         " pref char(10)," +
                         " nzp_dom integer)"+DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " insert into sel_kvar (nzp_area, nzp_dom, nzp_wp, pref)" +
                  " select max(nzp_area), nzp_dom, nzp_wp, pref " +
                  " from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar where nzp_kvar >1  " +
                  " group by 2,3,4";
            ExecSQL(sql);

            ExecSQL("create index ix_tmp_82 on sel_kvar(nzp_dom, nzp_area)");
            ExecSQL(DBManager.sUpdStat + " sel_kvar ");

            sql = " INSERT INTO t_payers(nzp_supp, nzp_payer)" +
                  " SELECT a.nzp_supp, fb.nzp_payer " +
                  " FROM " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "fn_dogovor_bank_lnk fdbl, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "fn_bank fb " +
                  " WHERE  fdbl.id = a.fn_dogovor_bank_lnk_id " +
                  "        AND fdbl.nzp_fb = fb.nzp_fb " +  
                  whereSupp+
                  " GROUP BY 1, 2 ";
            ExecSQL(sql);

            for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
            {
                int year = i / 12;
                int month = i % 12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }
                var distribXx = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                        DBManager.tableDelimiter + "fn_distrib_dom_" + month.ToString("00");




                //Проверка на существование базы

                if (TempTableInWebCashe(distribXx))
                {
                    sql = " INSERT INTO t_distrib (nzp_area,nzp_serv, nzp_supp, nzp_payer, sum_rasp, " +
                          " sum_ud, sum_charge, sum_send, sum_in, sum_out)" +
                          " SELECT d.nzp_area,a.nzp_serv, a.nzp_supp, a.nzp_payer, " +
                          " sum(case when t.nzp_payer = a.nzp_payer then  a.sum_rasp  else 0 end), " +
                          " sum(case when t.nzp_payer = a.nzp_payer then a.sum_ud else 0 end ), " +
                          " sum(case when t.nzp_payer = a.nzp_payer then a.sum_charge else 0 end ),  " +
                          " sum(a.sum_send), " +
                          " sum(case when dat_oper='" + DatS.ToShortDateString() + "' then a.sum_in else 0 end), " +
                          " sum(case when dat_oper='" + DatPo.ToShortDateString() + "' then a.sum_out else 0 end)" +
                          " FROM sel_kvar d, " + distribXx + " a   LEFT JOIN  t_payers t ON a.nzp_supp = t.nzp_supp  AND t.nzp_payer = a.nzp_payer " +
                          " WHERE a.nzp_dom=d.nzp_dom " +
                          "      AND dat_oper<='" + DatPo.ToShortDateString() + "'" +
                          "      AND dat_oper>='" + DatS.ToShortDateString() + "'" +
                          whereSupp + whereServ + whereWp +
                          " GROUP BY  1, 2, 3, 4          ";
                    ExecSQL(sql, true);
                }

            }
            InsertMoneyFrom(whereSupp, whereServ);

            ExecSQL("drop table sel_kvar");

            sql = " SELECT sa.payer as agent, sr.payer as principal, sp.payer as payee, " +
                  "        area, " +
                  "        sup.payer as name_supp, s.service,   " +
                  "        sum(t.sum_charge) as sum_charge, sum(sum_rasp) as sum_rasp, " +
                  "        sum(t.sum_ud) as sum_ud, sum(sum_send) as sum_send, " +
                  "        sum(t.sum_in) as sum_in, sum(sum_out) as sum_out, " +
                  " sum(money_from) as money_from " +
                  " FROM t_distrib t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sup, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sr, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sa, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_area sar " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  "        AND t.nzp_area = sar.nzp_area " +
                  "        AND su.nzp_payer_supp = sup.nzp_payer " +
                  "        AND su.nzp_payer_agent = sa.nzp_payer " +
                  "        AND su.nzp_payer_princip = sr.nzp_payer " +
                  "        AND t.nzp_payer = sp.nzp_payer " +
                  "        AND t.nzp_serv = s.nzp_serv " +
                  " GROUP BY 1,2,3,4,5,6 " +
                  " ORDER BY 1,2,3,4,5 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
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
        /// Получение оплат поступивших от поставщиков или не деньгами
        /// на р/сч
        /// </summary>
        private void InsertMoneyFrom(string whereSupp, string whereServ)
        {
            var sql = " SELECT * " +
                 " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point d " +
                 " WHERE nzp_wp>1 " + GetwhereWp1();
            MyDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                for (int i = DatS.Year ; i <= DatPo.Year; i++)
                {   
                    var fromSupplierTable = pref + "_charge_" + (i - 2000).ToString("00") + DBManager.tableDelimiter +
                                   "from_supplier ";

                    sql = " INSERT INTO t_distrib (nzp_area,nzp_serv, nzp_supp, nzp_payer, money_from )" +
                          " SELECT d.nzp_area, a.nzp_serv, a.nzp_supp, t.nzp_payer, sum(sum_prih)  " +
                          " FROM " + fromSupplierTable + " a, " +
                          pref + DBManager.sDataAliasRest + "kvar k, sel_kvar d, t_payers t " +
                          " WHERE a.num_ls=k.num_ls and  kod_sum in (49,50) " + 
                          "      AND a.nzp_serv>1 " +
                          "      AND k.nzp_dom = d.nzp_dom  " +
                          "      AND t.nzp_supp = a.nzp_supp " +
                          " and a.dat_uchet>='" + DatS.ToShortDateString() + "'" +
                          " and a.dat_uchet<='" + DatPo.ToShortDateString() + "'" +
                          whereSupp + whereServ +
                          " GROUP BY  1,2,3,4            ";
                    if (TempTableInWebCashe(fromSupplierTable)) ExecSQL(sql);
                }
            }

            reader.Close();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dats", DatS.ToShortDateString());
            report.SetParameterValue("datpo", DatPo.ToShortDateString());
            switch (ReportTitle)
            {
                case 1:
                    report.SetParameterValue("reportHeader", "за жилищно-коммунальные услуги");
                    break;
                case 2:
                    report.SetParameterValue("reportHeader", "наем жилого помещения");
                    break;
                case 3:
                    report.SetParameterValue("reportHeader", "жилищные услуги");
                    break;
                case 4:
                    report.SetParameterValue("reportHeader", "коммунальные услуги");
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
            ReportTitle = UserParamValues["ReportTitle"].GetValue<int>();

            Services = UserParamValues["Services"].GetValue<List<int>>();
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_distrib (     " +
                               " nzp_area integer default 0," +
                               " nzp_serv integer default 0," +
                               " nzp_supp integer default 0," +
                               " nzp_payer integer default 0," +
                               " sum_in " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Входящий остаток
                               " sum_send " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Перечислено
                               " sum_out " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Исходящий остаток
                               " sum_rasp " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Рапределено
                               " sum_ud " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Удержано
                               " sum_charge " + DBManager.sDecimalType + "(14,2) default 0.00, " + //К перечислению
                               " money_from " + DBManager.sDecimalType + "(14,2) default 0.00 " + //К перечислению
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL("create index ix_tmp_1 on t_distrib(nzp_area)");
            ExecSQL("create index ix_tmp_2 on t_distrib(nzp_supp)");
            

            ExecSQL(" create temp table t_payers( " +
                    " nzp_payer integer," +
                    " nzp_supp integer )");      
            ExecSQL("create index ix_tmp_3 on t_payers(nzp_supp)");

        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_distrib ", true);
            ExecSQL(" drop table t_payers ", true);
        }

        #region Фильтры
        private string GetwhereWp1()
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
            return whereWp;
        }

        /// <summary>
        /// Получате условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;

            whereServ = Services != null
                ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_serv);

            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;

            //Дополнительно фильтруем услугив в зависисости от заголовка
            switch (ReportTitle)
            {
                case 1:
                    break;
                case 2:
                    whereServ = " and nzp_serv=15 ";
                    break;
                case 3:
                    whereServ += " and nzp_serv not in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts )";
                    break;
                case 4:
                    whereServ += " and (nzp_serv in (select nzp_serv from " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts) " +
                        " or nzp_serv in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest + "serv_odn)) ";
                    break;
            }
            if (!string.IsNullOrEmpty(whereServ) && string.IsNullOrEmpty(ServiceHeader))
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services WHERE nzp_serv > 0 " + whereServ;
                DataTable servTable = ExecSQLToTable(sql);
                foreach (DataRow row in servTable.Rows)
                {
                    ServiceHeader += row["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
            }
            return whereServ;
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
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }


        private string GetwhereWp()
        {
            MyDataReader reader;
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
                string sql1 = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql1);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }

            string sql = " SELECT bd_kernel as pref " +
             " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
             " WHERE nzp_wp>1 " + whereWp;
            ExecRead(out reader, sql);
            whereWp = "";
            while (reader.Read())
            {
                whereWp = whereWp + " '" + reader["pref"].ToStr().Trim() + "', ";
            }
            whereWp = whereWp.TrimEnd(',', ' ');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND d.pref in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }
        #endregion
    }

}


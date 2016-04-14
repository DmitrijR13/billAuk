using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.MariEl.Properties;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.MariEl.Reports
{
  public  class Report120105 : BaseSqlReport
    {
        public override string Name
        {
            get { return "12.1.5 Информация о собираемости платежей с населения"; }
        }

        public override string Description
        {
            get { return "1.5 Информация о собираемости платежей с населения"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return UserParamValues["All"].GetValue<int>() == 2 ? Resources.Report_12_1_5 : Resources.Report_12_1_5_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base}; }
        }


        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный день </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Территории</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Ук</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Район</summary>
        protected List<int> Rajons { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Список районов в заголовке</summary>
        protected string RajonsHeader { get; set; }

        /// <summary>Список УК в заголовке</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        /// <summary>ЖЭУ</summary>
        protected List<int> Geus { get; set; }

        /// <summary>ЖЭУ</summary>
        protected string GeuHeader { get; set; }


        /// <summary>Принципалы</summary>
        protected List<int> Princ { get; set; }

        /// <summary>Принципалы</summary>
        protected string PrincHeader { get; set; }


        public override List<UserParam> GetUserParams()
        {   
            DateTime datPo = DateTime.Now;
            DateTime datS = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new BankParameter(),
                new GeuParameter(),
                new AreaParameter(),
                new PrincipalParameter(),
                new RaionsParameter(),
                new ServiceParameter(),
                new ComboBoxParameter(false)
                {
                    Code = "Grouper",
                    Name = "Группировать по",
                    Value = 1,
                    StoreData = new List<object>
                    {
                        new {Id = 1, Name = "Плательщику"},
                        new {Id = 2, Name = "ЖЭУ"},
                        new {Id = 3, Name = "ЖЭУ (с\\п)"}
                    }
                },
                new ComboBoxParameter(false)
                {
                    Code = "All",
                    Name = "Все поля",
                    Value = 2,
                    StoreData = new List<object>
                    {
                        new {Id = 1, Name = "Да"},
                        new {Id = 2, Name = "Нет"}
                    }
                }
            };
        }

        public override DataSet GetData()
        {
            #region Выборка по локальным банкам

            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
            string centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest;
            string whereServ = GetWhereServ();
            string whereRaj = GetRajon("u.");
            string whereArea = GetWhereArea();
            string whereGeu = GetWhereGeu("k.");
            string wherePrinc = GetWherePrinc("s.nzp_supp"); 
            GetwhereWp();
            string sql;
            string selectString = "d.nzp_geu, k.nzp_area, u.nzp_raj, c.nzp_serv, s.nzp_payer_princip, ";

            if (string.IsNullOrEmpty(whereServ) && !string.IsNullOrEmpty(whereRaj))
                selectString = "d.nzp_geu, k.nzp_area, u.nzp_raj, -100, s.nzp_payer_princip, ";
            if (string.IsNullOrEmpty(whereRaj) && !string.IsNullOrEmpty(whereServ))
                selectString = "d.nzp_geu, k.nzp_area, -100, c.nzp_serv, s.nzp_payer_princip, ";


            foreach (var pref in PrefBanks)
            {
                DateTime DatSSald = DatS.AddMonths(-1);
                DateTime DatPoSald = DatPo.AddMonths(-1);
                for (int i = DatSSald.Year * 12 + DatSSald.Month; i < DatPoSald.Year * 12 + DatPoSald.Month + 1; i++)
                {
                    int mo = i%12;
                    int ye = mo == 0 ? (i/12) - 1 : (i/12);
                    if (mo == 0) mo = 12;
                    string chargeYY = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                      "charge_" + mo.ToString("00");

                    string sumInsaldo = ((mo == DatSSald.Month) & (ye == DatSSald.Year)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((mo == DatPoSald.Month) & (ye == DatPoSald.Year)) ? "sum_outsaldo" : "0";   

                    if (TempTableInWebCashe(chargeYY))
                    {
                        sql =
                            " INSERT INTO t_rep_1_5 (nzp_geu, nzp_area, nzp_raj, nzp_serv, nzp_payer_princip," +
                            " sum_insaldo, rsum_tarif, sum_nedop, real_charge, reval, sum_vh, sum_outsaldo ) " +
                            " SELECT " + selectString +
                            " SUM(" + sumInsaldo + "), SUM(rsum_tarif), SUM(sum_nedop), " +
                            " SUM(real_charge), SUM(reval), " +
                            " SUM(rsum_tarif-sum_nedop+reval+real_charge), " +
                            " SUM(" + sumOutsaldo + ") AS sum_outsaldo " +
                            " FROM " +
                            chargeYY + " c, " + pref + DBManager.sDataAliasRest + "kvar k," +
                            pref + DBManager.sDataAliasRest + "dom d," +
                            pref + DBManager.sDataAliasRest + "s_ulica u, " +
                            Points.Pref + DBManager.sKernelAliasRest + "supplier s" +
                            " WHERE k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND s.nzp_supp = c.nzp_supp" +
                            " AND c.dat_charge IS NULL " +
                            " AND c.nzp_serv > 1 " +
                            " AND k.num_ls = c.num_ls " + whereServ + whereArea + whereRaj + whereGeu + wherePrinc +
                            " GROUP BY 1,2,3,4,5 ";
                        ExecSQL(sql);
                    } 
                }


                for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                {
                    int mo = i%12;
                    int ye = mo == 0 ? (i/12) - 1 : (i/12);
                    if (mo == 0) mo = 12;

                    string tableFnSupplier = pref + "_charge_" + (ye - 2000).ToString("00") +
                                             DBManager.tableDelimiter + "fn_supplier" + mo.ToString("00");  

                    if (TempTableInWebCashe(tableFnSupplier))
                    { 
                        sql =
                            " INSERT INTO t_rep_1_5 (nzp_geu, nzp_area, nzp_raj, nzp_serv, nzp_payer_princip," +
                            " sum_money ) " +
                            " SELECT " + selectString +
                            " SUM(sum_prih) " +
                            " FROM " +
                            tableFnSupplier + " c, " + pref + DBManager.sDataAliasRest + "kvar k," +
                            pref + DBManager.sDataAliasRest + "dom d," +
                            pref + DBManager.sDataAliasRest + "s_ulica u, " +
                            Points.Pref + DBManager.sKernelAliasRest + "supplier s" +
                            " WHERE k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul AND s.nzp_supp = c.nzp_supp" +
                            " AND c.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                            " AND c.dat_uchet <= '" + DatPo.ToShortDateString() + "'" +
                            " AND c.nzp_serv > 1 " +
                            " AND k.num_ls = c.num_ls " + whereServ + whereArea + whereRaj + wherePrinc +
                            " GROUP BY 1,2,3,4,5 ";
                        ExecSQL(sql);
                    }
                }

                for (int ye = DatS.Year; ye <= DatPo.Year; ye++)
                {

                    string tableFromSupplier = pref + "_charge_" + (ye - 2000).ToString("00") +
                                               DBManager.tableDelimiter + "from_supplier ";

                    if (TempTableInWebCashe(tableFromSupplier))
                    { 
                        sql =
                            " INSERT INTO t_rep_1_5 (nzp_geu, nzp_area, nzp_raj, nzp_serv, nzp_payer_princip," +
                            " sum_money ) " +
                            " SELECT " + selectString +
                            " SUM(sum_prih) " +
                            " FROM " +
                            tableFromSupplier + " c, " + pref + DBManager.sDataAliasRest + "kvar k," +
                            pref + DBManager.sDataAliasRest + "dom d," +
                            pref + DBManager.sDataAliasRest + "s_ulica u, " +
                            Points.Pref + DBManager.sKernelAliasRest + "supplier s" +
                            " WHERE k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul AND s.nzp_supp = c.nzp_supp" +
                            " AND c.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                            " AND c.dat_uchet <= '" + DatPo.ToShortDateString() + "'" +
                            " AND c.nzp_serv > 1 " +
                            " AND c.kod_sum in (49, 50, 35) " +
                            " AND k.num_ls = c.num_ls " + whereServ + whereArea + whereRaj + wherePrinc +
                            " GROUP BY 1,2,3,4,5 ";
                        ExecSQL(sql);
                    }


                }
            }       
            #endregion

            #region Выборка на экран

            var geu = string.Empty;
            var area = string.Empty;
            var rajon = string.Empty;
            var payer = string.Empty;
            var service = string.Empty;
            switch (UserParamValues["Grouper"].GetValue<int>())
            {
                case 1: geu = DBManager.sNvlWord + "(geu, 'НЕ ОПРЕДЕЛЕН') as group2 ";
                    area = " area AS group3 ";
                    rajon = " (CASE WHEN rajon='-'  THEN town ELSE TRIM(town)||','||TRIM(rajon) END) as group4 ";
                    payer = " p.payer as group1 ";
                    service = " service AS service ";
                    break;
                case 2: geu = DBManager.sNvlWord + "(geu, 'НЕ ОПРЕДЕЛЕН') as group1 ";
                    area = " area AS group3 ";
                    rajon = " (CASE WHEN rajon='-'  THEN town ELSE TRIM(town)||','||TRIM(rajon) END) as group4 ";
                    payer = " p.payer as group2 ";
                    service = " service AS service ";
                    break;
                case 3: geu = DBManager.sNvlWord + "(geu, 'НЕ ОПРЕДЕЛЕН') as service ";
                    area = " area AS group3 ";
                    rajon = " '' as group2 ";
                    payer = " '' as group1 ";
                    service = " '' AS group4 ";
                    break;
            }

            ExecSQL(" DELETE FROM t_rep_1_5 WHERE sum_money IS NULL and sum_vh = 0 ");
            sql =
                " SELECT " + service + " , " + geu + ", " + area + ", " + rajon + ", " + payer + ", " +
                " SUM(sum_insaldo) AS sum_insaldo, SUM(rsum_tarif) AS rsum_tarif, " +
                " SUM(sum_nedop) AS sum_nedop, SUM(real_charge) AS real_charge, " +
                " SUM(reval) AS reval, SUM(sum_money) AS sum_money, SUM(sum_insaldo) + SUM(rsum_tarif) + SUM(reval) - SUM(sum_money) AS sum_outsaldo, " +
                " SUM(sum_vh) AS sum_vh, SUM(sum_money)*100/SUM(sum_vh) AS sum_sn" +
                " FROM t_rep_1_5 t" +
                " LEFT OUTER JOIN " + centralData + "s_geu g ON t.nzp_geu=g.nzp_geu" +
                " INNER JOIN " + centralData + "s_area a ON t.nzp_area = a.nzp_area" +
                " LEFT OUTER JOIN " + centralData + "s_rajon r ON r.nzp_raj=t.nzp_raj " +
                " LEFT OUTER JOIN  " + centralData + "s_town st ON r.nzp_town = st.nzp_town" +
                " LEFT OUTER JOIN " + centralKernel + "services s ON t.nzp_serv=s.nzp_serv" +
                " LEFT OUTER JOIN " + centralKernel + "s_payer p ON t.nzp_payer_princip = p.nzp_payer" +
                //" WHERE sum_vh<>0 " + 
                " GROUP BY 1,2,3,4,5 " +
                //" HAVING SUM(sum_vh)<>0 " +
                " ORDER BY 1,2,3,4,5 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            DataTable d2t = ExecSQLToTable("select * from  t_rep_1_5");
            d2t.NewRow();
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



        /// <summary> Получить условия органичения по ЖЭУ </summary>
        private string GetWhereGeu(string pref)
        {
            var result = String.Empty;
            result = Geus != null
                ? Geus.Aggregate(result, (current, nzpGeu) => current + (nzpGeu + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_geu);

            result = result.TrimEnd(',');
            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + pref + "nzp_geu in (" + result + ") ";

                GeuHeader = String.Empty;
                var sql = " SELECT geu from " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_geu " + pref.TrimEnd('.') +
                      " WHERE " + pref + "nzp_geu > 0 " + result;
                var geu = ExecSQLToTable(sql);
                foreach (DataRow dr in geu.Rows)
                {
                    GeuHeader += dr["geu"].ToString().Trim() + ",";
                }
                GeuHeader = GeuHeader.TrimEnd(',');
            }
            return result;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                if (String.IsNullOrEmpty(ServiceHeader))
                {
                    ServiceHeader = string.Empty;
                    string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "services  WHERE nzp_serv > 0 " + whereServ;
                    DataTable serv = ExecSQLToTable(sql);
                    foreach (DataRow dr in serv.Rows)
                    {
                        ServiceHeader += dr["service"].ToString().Trim() + ", ";
                    }
                    ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
                }
            }
            return whereServ;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
        private string GetWherePrinc(string fieldPref)
        {
            string wherePrinc = String.Empty;

            if (Princ != null)
            {

                string supp = string.Empty;
                supp = Princ.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                wherePrinc += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
           

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            wherePrinc = wherePrinc.TrimEnd(',');


            if (!String.IsNullOrEmpty(wherePrinc) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    wherePrinc += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                PrincHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + wherePrinc;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    PrincHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                PrincHeader = PrincHeader.TrimEnd(',', ' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + wherePrinc + ")";
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
            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());

                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
            string whereWpRes = !String.IsNullOrEmpty(whereWp) ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s,"
                       + ReportParams.Pref + DBManager.sDataAliasRest + "kvar kv " +
                       "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )" : String.Empty;
            return whereWpRes;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Ограничение по районам
        /// </summary>
        /// <returns></returns>
        public string GetRajon(string filedPref)
        {
            string whereRajon = String.Empty;
            if (Rajons != null)
            {
                whereRajon = Rajons.Aggregate(whereRajon, (current, nzpRaj) => current + (nzpRaj + ","));
            }
            whereRajon = whereRajon.TrimEnd(',');
            whereRajon = !String.IsNullOrEmpty(whereRajon)
                ? " AND " + filedPref + ".nzp_raj in (" + whereRajon + ")"
                  : String.Empty;
            if (!String.IsNullOrEmpty(whereRajon))
            {
                RajonsHeader = String.Empty;
                string sql = " SELECT rajon from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon " +
                             filedPref.Trim('.') + "  WHERE " + filedPref + ".nzp_raj > 0 " + whereRajon;
                DataTable raj = ExecSQLToTable(sql);
                foreach (DataRow dr in raj.Rows)
                {
                    RajonsHeader += dr["rajon"].ToString().Trim() + ", ";
                }
                RajonsHeader = RajonsHeader.TrimEnd(',', ' ');
            }
            return whereRajon;
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            string period = "Период с " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString(); 

            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());

            string headerParam = period + "\n";
            headerParam += !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Поставщики: " + AreaHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(RajonsHeader) ? "Поставщики: " + RajonsHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Rajons = UserParamValues["Raions"].GetValue<List<int>>();
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Geus = UserParamValues["Geu"].GetValue<List<int>>();
            Princ = UserParamValues["Principal"].GetValue<List<int>>();

            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_rep_1_5 (  " +
                         " nzp_geu INTEGER DEFAULT 0, " +
                         " nzp_serv INTEGER DEFAULT 0, " +
                         " nzp_area INTEGER DEFAULT 0, " +
                         " nzp_raj INTEGER DEFAULT 0, " +
                         " nzp_payer_princip INTEGER DEFAULT 0, " +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                         " real_charge " + DBManager.sDecimalType + "(14,2), " +
                         " reval " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2), " +
                         " sum_vh " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_rep_1_5_1 on t_rep_1_5(nzp_serv) ");
            ExecSQL(" CREATE INDEX ix_t_rep_1_5_2 on t_rep_1_5(nzp_area) ");
            ExecSQL(" CREATE INDEX ix_t_rep_1_5_3 on t_rep_1_5(nzp_raj) ");
            ExecSQL(" CREATE INDEX ix_t_rep_1_5_4 on t_rep_1_5(nzp_geu) ");
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_rep_1_5 ", true);
        }

    }
}

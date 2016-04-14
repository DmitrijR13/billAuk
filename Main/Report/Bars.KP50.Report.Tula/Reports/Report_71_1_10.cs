using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71001010 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.10 Отчет по начислениям по нормативам"; }
        }

        public override string Description
        {
            get { return "Отчет по начислениям, по нормативам"; }
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
            get { return Resources.Report_71_1_10; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Заголовок поставщиков</summary>
        protected string SupplierHeader { get; set; }

     
        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        private int YearS { get; set; }
        private int MonthS { get; set; }

        private int YearPo { get; set; }
        private int MonthPo { get; set; }



        private string nzpServ { get; set; }

        public override List<UserParam> GetUserParams()
        {
            
            return new List<UserParam>
            {
               new MonthParameter {Name = "Месяц с", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = DateTime.Today.Year },
                new BankSupplierParameter(),
                 new ComboBoxParameter
                {
                    Code = "Serv",
                    Name = "Услуга",
                    Value = "6",
                    StoreData = new List<object>
                    {
                        new { Id = "6", Name = "Холодная вода" },
                        new { Id = "7", Name = "Водоотведение" },
                        new { Id = "8", Name = "Отопление" },
                        new { Id = "9", Name = "Горячая вода" }
                    }
                },
                
            };

        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            
            nzpServ = UserParamValues["Serv"].GetValue<int>().ToString();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

        }

        
        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS);
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("period_month", months[MonthS] + " " + YearS);
            }
            else
            {
                report.SetParameterValue("period_month", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo);

            }

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            switch (nzpServ)
            { 
                case "6":
                    report.SetParameterValue("serv", "холодная вода");
                    break; 
                case "7":
                    report.SetParameterValue("serv", "канализация"); 
                    break; 
                case "8":
                    report.SetParameterValue("serv", "отопление");
                    break;  
                case "9":
                    report.SetParameterValue("serv", "горячая вода");
                    break;   
            }
            if ((YearS == YearPo) && (MonthS == MonthPo)) report.SetParameterValue("hideGil", "0");
            else report.SetParameterValue("hideGil", "1");
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            var ds = new DataSet();
            string sql;
            if (nzpServ == "8")
            {
                #region заполнение временной таблицы

                sql = " SELECT bd_kernel AS pref, point " +
                             " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                             " WHERE nzp_wp>1 " + GetwhereWp();
                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    string pref = reader["pref"].ToString().Trim();
                    string point = reader["point"].ToString().Trim();
                    string prefData = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;

                    string gkuTable = pref + "_charge_" + (YearPo - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "calc_gku_" + MonthPo.ToString("00");
                    string countersTable = pref + "_charge_" + (YearPo - 2000).ToString("00") +
                                           DBManager.tableDelimiter + "counters_" + MonthPo.ToString("00");

                    sql = " Create temp table t_cursv8( " +
                          " nzp_dom integer," +
                          " nzp_kvar integer," +
                          " nzp_supp integer," +
                          " prizn integer default 0," +
                          " has_opu integer default 0," +
                          " is_mkd integer default 0," +
                          " heater char(100)," +
                          " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                          " sum_tarif " + DBManager.sDecimalType + "(14,2)," +
                          " reval " + DBManager.sDecimalType + "(14,2)," +
                          " real_charge " + DBManager.sDecimalType + "(14,2)," +
                          " sum_money " + DBManager.sDecimalType + "(14,2)," +
                          " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                          " c_calc " + DBManager.sDecimalType + "(14,7)," +
                          " pl_ob " + DBManager.sDecimalType + "(14,2)," +
                          " pl_otopl " + DBManager.sDecimalType + "(14,2)," +
                          " tarif " + DBManager.sDecimalType + "(14,2)," +
                          " gil integer)" + DBManager.sUnlogTempTable;
                    ExecSQL(sql);

                    for (int i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                    {
                        int ye = i / 12;
                        int mo = i % 12;
                        if (mo == 0)
                        {
                            ye--;
                            mo = 12;
                        }   
                        string chargeTable = pref + "_charge_" + (ye - 2000).ToString("00") +
                                             DBManager.tableDelimiter + "charge_" + mo.ToString("00");
                        string tablePerekidka = pref + "_charge_" + (ye - 2000).ToString("00") +
                                                DBManager.tableDelimiter +
                                                "perekidka";

                        string sumInsaldo = "0";
                        if ((ye == YearS) && (mo == MonthS)) sumInsaldo = "sum_insaldo";
                        string sumOutsaldo = "0";
                        if ((ye == YearPo) && (mo == MonthPo)) sumOutsaldo = "sum_outsaldo";


                        sql = " insert into t_cursv8(nzp_dom, nzp_kvar, nzp_supp, prizn,  " +
                              " sum_insaldo, sum_tarif, reval,  sum_money, sum_outsaldo, c_calc) " +
                              " select k.nzp_dom, k.nzp_kvar, nzp_supp, is_device%2 , " +
                              " sum(" + sumInsaldo + "), sum(sum_tarif), sum(reval),  " +
                              " sum(sum_money), sum(" + sumOutsaldo + "), sum(c_calc)  " +
                              " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                              " " + chargeTable + " b" +
                              " where k.nzp_kvar=b.nzp_kvar and b.nzp_serv= " + nzpServ +
                              GetWhereSupp("b.nzp_supp") +
                              " and b.dat_charge is null " +
                              " and abs(b.sum_insaldo)+abs(b.sum_tarif)+abs(b.reval)+" +
                              " abs(b.real_charge)+abs(b.sum_money)+abs(b.sum_outsaldo)>0.001" +
                              " group by 1,2,3,4";

                        ExecSQL(sql);

                        sql = " UPDATE t_cursv8 " +
                              " SET real_charge = ( SELECT SUM(sum_rcl)" +
                              " FROM " + tablePerekidka + " p " +
                              " WHERE p.type_rcl not in (100,20) " +
                              " AND p.nzp_kvar > 0 " +
                              " AND p.nzp_kvar = t_cursv8.nzp_kvar" +
                              " AND p.nzp_serv = " + nzpServ + GetWhereSupp("p.nzp_supp") +
                              " AND p.month_ = " + mo + ") ";
                        ExecSQL(sql);


                    }
                    sql = "create index ix_t_cursv801 on t_cursv8(nzp_kvar, nzp_supp)";
                    ExecSQL(sql);
                    sql = "create index ix_t_cursv802 on t_cursv8(nzp_dom)";
                    ExecSQL(sql);
                    sql = "create index ix_t_cursv803 on t_cursv8(nzp_kvar)";
                    ExecSQL(sql);

                    sql = " UPDATE t_cursv8 " +
                          " SET has_opu = 1" +
                          " WHERE nzp_dom in ( SELECT nzp_dom " +
                          " FROM " + countersTable + " c " +
                          " WHERE nzp_type=1 and nzp_counter>0 and stek in (1,2) " +
                          " AND t_cursv8.nzp_dom= c.nzp_dom" +
                          " AND c.nzp_serv = " + nzpServ + 
                          ") ";
                    ExecSQL(sql);


                    ExecSQL(DBManager.sUpdStat + " t_cursv8");

                    if ((YearS == YearPo) && (MonthS == MonthPo))
                    {
                        sql = " update t_cursv8 set gil = (select max(gil) " +
                              " from " + gkuTable + " a " +
                              " where t_cursv8.nzp_kvar=a.nzp_kvar and a.stek = 3 " +
                              " and t_cursv8.nzp_supp=a.nzp_supp " +
                              " and a.nzp_serv= " + nzpServ + " )";
                        ExecSQL(sql);

                    }

                    sql = " update t_cursv8 set c_calc = ( select rashod*tarif/trf1  " +
                          " from " + gkuTable + " a " +
                          " where t_cursv8.nzp_kvar=a.nzp_kvar and a.stek = 3 " +
                          " and t_cursv8.nzp_supp=a.nzp_supp " +
                          " and nzp_frm<>114 and trf1>0 " +
                          " and a.nzp_serv= " + nzpServ + " )";
                    ExecSQL(sql);                 
                    

                    sql = " update t_cursv8 set is_mkd = 1 " +
                          " where nzp_dom in (select nzp " +
                          " from " + prefData + "prm_2 " +
                          " where nzp_prm=2030 and is_actual=1 and val_prm='1')";
                    ExecSQL(sql);

                    sql = " update t_cursv8 set pl_ob = (select max(squ1) " +
                          " from " + countersTable + " c " +
                          " where t_cursv8.nzp_kvar=c.nzp_kvar and c.stek = 3 " +
                          " and c.nzp_serv= " + nzpServ + " )";
                    ExecSQL(sql);
                    sql = " update t_cursv8 set pl_otopl = (select max(squ2) " +
                          " from " + countersTable + " c " +
                          " where t_cursv8.nzp_kvar=c.nzp_kvar and c.stek = 3 " +
                          " and c.nzp_serv= " + nzpServ + " )";
                    ExecSQL(sql);

                    sql = " UPDATE t_cursv8 set heater = (" +
                          " SELECT max(val_prm) " +
                          " FROM " + pref + DBManager.sDataAliasRest + "prm_2 p " +
                          " WHERE p.nzp = t_cursv8.nzp_dom " +
                          " AND p.nzp_prm = 1010141 " +
                          " AND p.is_actual <> 100 " +
                          " AND dat_s <= DATE(" + DBManager.MDY(MonthPo, DateTime.DaysInMonth(YearPo, MonthPo), YearPo) + ") " +
                          " AND dat_po >= DATE(" + DBManager.MDY(MonthS, 01, YearS) + ")) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_cursv8 set prizn = 2 " +
                          " WHERE nzp_kvar in (" +
                          " SELECT nzp " +
                          " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p " +
                          " WHERE p.nzp = t_cursv8.nzp_dom " +
                          " AND p.nzp_prm = 1240 " +
                          " AND p.val_prm <> '1' " +
                          " AND p.is_actual <> 100 " +
                          " AND dat_s <= DATE(" + DBManager.MDY(MonthPo, DateTime.DaysInMonth(YearPo, MonthPo), YearPo) +
                          ") " +
                          " AND dat_po >= DATE(" + DBManager.MDY(MonthS, 01, YearS) + ")) ";
                    ExecSQL(sql);

                    #region Батарея тарифов
                    ExecSQL("drop table t_tarif");

                    sql = " select nzp_dom, nzp_supp,  max(val_prm"+DBManager.sConvToNum+") as tarif " +
                          " into temp t_tarif " +
                          " FROM t_cursv8 t left outer join " + pref + DBManager.sDataAliasRest +" prm_11 p on t.nzp_supp=p.nzp " +
                          " and is_actual<> 100 " +
                          " and nzp_prm=339 " +
                          " and dat_s <= DATE("+ DBManager.MDY(MonthPo, DateTime.DaysInMonth(YearPo, MonthPo), YearPo) + ") " +
                          " and dat_po >=  DATE(" +DBManager.MDY(MonthS, 01, YearS ) +  " )"+
                          " group by 1,2";
                    ExecSQL(sql);
                    ExecSQL("Create index ixt_t_tarif_01 on t_tarif(nzp_dom, nzp_supp)");
                    ExecSQL(DBManager.sUpdStat + " t_tarif");

                    ExecSQL("drop table ttarif"); 
                    sql = " select nzp_dom, nzp_supp, (case when val_prm is null or val_prm='' then tarif else val_prm" + DBManager.sConvToNum +
                          " end) as tarif" +
                          " into temp ttarif" +
                          " from  t_tarif t left outer join  "+ pref + DBManager.sDataAliasRest +"  prm_2 p on " +
                          " nzp=nzp_dom  " +
                          " and is_actual<> 100" +
                          " and nzp_prm=1062 " +
                          " and val_prm is not null " +
                          " and dat_s <= DATE(" + DBManager.MDY(MonthPo, DateTime.DaysInMonth(YearPo, MonthPo), YearPo) +
                          ") " +
                          " and dat_po >=  DATE(" + DBManager.MDY(MonthS, 01, YearS) + ") ";
                    ExecSQL(sql);



                    sql = " update t_cursv8 set tarif = (select max(tarif) " +
                          " from ttarif  " +
                          " where t_cursv8.nzp_dom=ttarif.nzp_dom " +
                          " and t_cursv8.nzp_supp=ttarif.nzp_supp) " +
                          " where tarif = 0 or tarif is null ";
                    ExecSQL(sql);

                    sql = " update t_cursv8 set c_calc = (" + DBManager.sNvlWord + "(sum_tarif,0)+" + DBManager.sNvlWord + "(reval,0)+" + DBManager.sNvlWord + "(real_charge,0))/tarif " +
                          " where t_cursv8.c_calc=0 or t_cursv8.c_calc is null  and tarif>0 ";
                    ExecSQL(sql);    


                    ExecSQL("drop table t_tarif");
                    #endregion

                    //sql = " UPDATE t_cursv8 " +
                    //      " SET c_calc =  " +
                    //      " ( SELECT val2 " +
                    //      " FROM " + countersTable + " c " +
                    //      " WHERE nzp_type=3  and stek =3 " +
                    //      " AND t_cursv8.nzp_kvar= c.nzp_kvar" +
                    //      " AND c.nzp_serv = " + nzpServ +
                    //      ") ";
                    //ExecSQL(sql);

                    sql = " insert into t_svod8(nzp_supp, heater, prizn, is_mkd, has_opu, nzp_dom,   " +
                          " sum_insaldo, sum_money, sum_tarif, reval, real_charge,  sum_outsaldo, " +
                          " c_calc,  gil, count_ls, pl_ob, pl_otopl) " +
                          " select nzp_supp, heater, prizn, is_mkd, has_opu, nzp_dom,  " +
                          " sum(sum_insaldo), sum(sum_money), sum(sum_tarif), sum(reval)," +
                          " sum( real_charge ), sum(sum_outsaldo), sum(c_calc), sum( gil )," +
                          " count(nzp_kvar), sum(pl_ob), sum(pl_otopl)" +  
                          " from t_cursv8 a " +
                          " group by 1,2,3,4,5,6 ";
                    ExecSQL(sql);

                    ExecSQL("drop table t_cursv8");
                }
                sql = "create index ix_t_svod802 on t_svod8(nzp_dom)";
                ExecSQL(sql);
                sql = "create index ix_t_svod901 on t_svod8(nzp_supp)";
                ExecSQL(sql);
                #endregion



            }

            else
            { 
                #region заполнение временной таблицы

                sql = " SELECT bd_kernel AS pref, point " +
                             " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                             " WHERE nzp_wp>1 " + GetwhereWp();
                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    string pref = reader["pref"].ToString().Trim();
                    string point = reader["point"].ToString().Trim();
                    string prefData = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;

                    for (int i = YearS*12 + MonthS; i < YearPo*12 + MonthPo + 1; i++)
                    {
                        int ye = i/12;
                        int mo = i%12;
                        if (mo == 0)
                        {
                            ye--;
                            mo = 12;
                        }
                        string chargeTable = pref + "_charge_" + (ye - 2000).ToString("00") +
                                             DBManager.tableDelimiter + "charge_" + mo.ToString("00");
                        string tablePerekidka = pref + "_charge_" + (ye - 2000).ToString("00") +
                                                DBManager.tableDelimiter +
                                                "perekidka";

                        string gkuTable = pref + "_charge_" + (ye - 2000).ToString("00") +
                                          DBManager.tableDelimiter + "calc_gku_" + mo.ToString("00");
                        string countersTable = pref + "_charge_" + (ye - 2000).ToString("00") +
                                               DBManager.tableDelimiter + "counters_" + mo.ToString("00");

                        string sumInsaldo = "0";
                        if ((ye == YearS) && (mo == MonthS)) sumInsaldo = "sum_insaldo";
                        string sumOutsaldo = "0";
                        if ((ye == YearPo) && (mo == MonthPo)) sumOutsaldo = "sum_outsaldo";


                        sql = " insert into t_cursv(nzp_dom, nzp_kvar, nzp_supp, is_device, tarif, " +
                              " sum_insaldo, sum_tarif, reval,real_charge,  sum_money, sum_outsaldo, c_calc) " +
                              " select k.nzp_dom, k.nzp_kvar, nzp_supp, is_device , max(tarif), " +
                              " sum(" + sumInsaldo + "), sum(sum_tarif), sum(reval), sum(real_charge), " +
                              " sum(sum_money), sum(" + sumOutsaldo + "), sum(c_calc)  " +
                              " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                              " " + chargeTable + " b" +
                              " where k.nzp_kvar=b.nzp_kvar and b.nzp_serv= " + nzpServ +
                              GetWhereSupp("b.nzp_supp") +
                              " and b.dat_charge is null " +
                              " group by 1,2,3,4";   
                        ExecSQL(sql);

                        //sql = " UPDATE t_cursv " +
                        //      " SET real_charge = ( SELECT SUM(sum_rcl)" +
                        //      " FROM " + tablePerekidka + " p " +
                        //      " WHERE p.type_rcl not in (100,20) " +
                        //      " AND p.nzp_kvar > 0 " +
                        //      " AND p.nzp_kvar = t_cursv.nzp_kvar" +
                        //      " AND p.nzp_serv = " + nzpServ + GetWhereSupp("p.nzp_supp") +
                        //      " AND p.month_ = " + mo + ") ";
                        //ExecSQL(sql);

                        if ((YearS == YearPo) && (MonthS == MonthPo))
                        {
                            sql = " update t_cursv set gil = (select max(gil) " +
                                  " from " + gkuTable + " a " +
                                  " where t_cursv.nzp_kvar=a.nzp_kvar and a.stek = 3 " +
                                  " and t_cursv.nzp_supp=a.nzp_supp " +
                                  " and a.nzp_serv= " + nzpServ + " )";
                            ExecSQL(sql);
                        }

                        sql = " update t_cursv set norm = (select max(rash_norm_one) " +
                              " from " + gkuTable + " a " +
                              " where t_cursv.nzp_kvar=a.nzp_kvar and a.stek = 3 " +
                              " and t_cursv.nzp_supp=a.nzp_supp" +
                              " and a.nzp_serv= " + nzpServ + ")";
                        ExecSQL(sql);

                        ExecSQL(" update t_cursv set norm = 0 where norm is null ");
                        ExecSQL(" update t_cursv set gil = 0 where gil is null ");

                        #region Батарея тарифов
                        ExecSQL("drop table t_tarif");

                        sql = " select nzp_dom, nzp_supp,  max(tarif) as tarif " +
                              " into temp t_tarif " +
                              " FROM t_cursv " +
                              " group by 1,2";
                        ExecSQL(sql);
                        ExecSQL("Create index ixt_t_tarif_01 on t_tarif(nzp_dom, nzp_supp)");
                        ExecSQL(DBManager.sUpdStat + " t_tarif");

                        ExecSQL("drop table t_servtarif");
                        sql = " SELECT nzp_supp, max(tarif) as tarif " +
                              " into temp t_servtarif " +
                              " FROM t_tarif" +
                              " group by 1 ";
                        ExecSQL(sql);


                        sql = " update t_cursv set tarif = (select tarif" +
                              " from t_tarif " +
                              " where t_cursv.nzp_dom=t_tarif.nzp_dom " +
                              " and t_cursv.nzp_supp=t_tarif.nzp_supp) " +
                              " where tarif = 0 ";
                        ExecSQL(sql);


                        sql = " update t_cursv set tarif = (select tarif" +
                              " from t_servtarif " +
                              " where t_cursv.nzp_supp=t_servtarif.nzp_supp) " +
                              " where tarif is null or tarif = 0";
                        ExecSQL(sql);


                        ExecSQL("drop table t_servtarif");
                        ExecSQL("drop table t_tarif");
                        #endregion

                        sql = " update t_cursv set is_mkd = 1 " +
                              " where nzp_dom in (select nzp " +
                              " from " + prefData + "prm_2 " +
                              " where nzp_prm=2030 and is_actual=1 and val_prm='1')";
                        ExecSQL(sql);

                        #region Получение нормы из counters_XX

                        sql = " update t_cursv set name_norm = (select max(name_y) " +
                              " from " + countersTable + " a, " + pref + DBManager.sKernelAliasRest + "res_y y " +
                              " where nzp_serv = 6 and a.cnt2=y.nzp_y  and y.nzp_res=a.cnt3 " +
                              " and t_cursv.nzp_kvar=a.nzp_kvar and stek=3) ";
                        ExecSQL(sql);

                        #endregion
                    }

                    #region Получение нормы для ЛС без расходов

                    string tableNum = "172";
                    if (nzpServ == "9") tableNum = "177";

                    sql = " select val_prm  " +
                          " from " + prefData + "prm_13 a " +
                          " where nzp_prm= " + tableNum +
                          " and is_actual = 1 " +
                          " and dat_s<=" + DBManager.MDY(MonthS, 01, YearS) + "" +
                          " and dat_po>=" + DBManager.MDY(MonthS, 01, YearS) + "";
                    object obj = ExecScalar(sql);

                    int nzpRes = obj != null ? Convert.ToInt32(obj) : 38;


                    #region Холодная вода

                    string nzpPrm = "7";
                    string nzpX = "= 1 ";

                    if (nzpServ == "7") nzpX = " in (3,4) ";
                    else if (nzpServ == "9")
                    {
                        nzpX = " = 2";
                        nzpPrm = "463";
                    }

                    sql = " update t_cursv set nzp_y = (select max(val_prm" + DBManager.sConvToNum + ") " +
                          " from " + prefData + "prm_1 p " +
                          " where nzp_prm=" + nzpPrm + " and is_actual=1 " +
                          " and t_cursv.nzp_kvar=p.nzp " +
                          " and dat_s<=" + DBManager.MDY(MonthS, 01, YearS) + "" +
                          " and dat_po>=" + DBManager.MDY(MonthS, 01, YearS) + ")" +
                          " where name_norm is null ";
                    ExecSQL(sql);

                    if (nzpServ == "9")
                    {
                        #region Потом по домам

                        sql = " update t_cursv set nzp_y = (select max(val_prm" + DBManager.sConvToNum + ") " +
                              " from " + prefData + "prm_2 p " +
                              " where nzp_prm=38 and is_actual=1 " +
                              " and t_cursv.nzp_dom=p.nzp " +
                              " and dat_s<=" + DBManager.MDY(MonthS, 01, YearS) + "" +
                              " and dat_po>=" + DBManager.MDY(MonthS, 01, YearS) + ")" +
                              " where name_norm is null and nzp_y is null";
                        ExecSQL(sql);

                        #endregion
                    }


                    sql = " update t_cursv set norm = (select max(value" + DBManager.sConvToNum + ") " +
                          " from  " + prefKernel + "res_values y" +
                          " where t_cursv.nzp_y =y.nzp_y" +
                          " and nzp_res=" + nzpRes + "  " +
                          " and nzp_x  " + nzpX + ") " +
                          " where nzp_y is not null and name_norm is null ";
                    ExecSQL(sql);

                    sql = " update t_cursv set name_norm = (select max(name_y) " +
                          " from " + prefKernel + "res_y y" +
                          " where t_cursv.nzp_y=y.nzp_y" +
                          " and nzp_res=" + nzpRes + ")" +
                          " where nzp_y is not null and name_norm is null ";
                    ExecSQL(sql);


                    sql = " update t_cursv set name_norm = (select max(name_y) " +
                          " from " + prefKernel + "res_y y" +
                          " where t_cursv.nzp_y=y.nzp_y" +
                          " and nzp_res=" + nzpRes + ")" +
                          " where nzp_y is not null and name_norm is null ";
                    ExecSQL(sql);

                    #endregion

                    #endregion

                    ExecSQL(" update t_cursv set name_norm = 'Не определен' where name_norm is null ");

                    sql = " insert into t_svod(point, principal, payer, name_norm, norm, " +
                          " sum_insaldo_mkd, sum_insaldo_chs, sum_money_mkd, sum_money_chs  , " +
                          " sum_tarif_mkd, sum_tarif_chs, reval_mkd, reval_chs, " +
                          " real_charge_mkd, real_charge_chs,  sum_outsaldo_mkd, sum_outsaldo_chs  , " +
                          " mkd_c_calc_norm, mkd_c_calc_ipu, chs_c_calc_norm, chs_c_calc_ipu  , " +
                          " mkd_gil_norm, mkd_gil_ipu, chs_gil_norm, chs_gil_ipu, " +
                          " mkd_ls,  chs_ls," +
                          " sum_tarif_m3_mkd_norm , " +
                          " sum_tarif_m3_chs_norm ," +
                          " sum_tarif_m3_mkd_ipu , " +
                          " sum_tarif_m3_chs_ipu , " +
                          " reval_m3_mkd_norm , " +
                          " reval_m3_chs_norm , " +
                          " reval_m3_mkd_ipu ," +
                          " reval_m3_chs_ipu , " +
                          " real_charge_m3_mkd_norm , " +
                          " real_charge_m3_chs_norm , " +
                          " real_charge_m3_mkd_ipu ," +
                          " real_charge_m3_chs_ipu  " +
                          ") " +
                          " select '" + point + "', pp.payer, p.payer, name_norm, norm,  " +
                          " sum(case when is_mkd = 1 then sum_insaldo else 0 end)," +
                          " sum(case when is_mkd = 0 then sum_insaldo else 0 end)," +
                          " sum(case when is_mkd = 1 then sum_money else 0 end)," +
                          " sum(case when is_mkd = 0 then sum_money else 0 end)," +
                          " sum(case when is_mkd = 1 then sum_tarif else 0 end)," +
                          " sum(case when is_mkd = 0 then sum_tarif else 0 end)," +
                          " sum(case when is_mkd = 1 then reval else 0 end)," +
                          " sum(case when is_mkd = 0 then reval else 0 end)," +
                          " sum(case when is_mkd = 1 then real_charge else 0 end)," +
                          " sum(case when is_mkd = 0 then real_charge else 0 end)," +
                          " sum(case when is_mkd = 1 then sum_outsaldo else 0 end)," +
                          " sum(case when is_mkd = 0 then sum_outsaldo else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device = 0 then c_calc else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device > 0 then c_calc else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device = 0 then c_calc else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device > 0 then c_calc else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device = 0 then gil else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device > 0 then gil else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device = 0 then gil else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device > 0 then gil else 0 end)," +
                          " sum(case when is_mkd = 1 then 1 else 0 end)," +
                          " sum(case when is_mkd = 0 then 1 else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device = 0 and tarif<>0 then sum_tarif/tarif else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device = 0 and tarif<>0 then sum_tarif/tarif else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device > 0 and tarif<>0 then sum_tarif/tarif else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device > 0 and tarif<>0 then sum_tarif/tarif else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device = 0 and tarif<>0 then reval/tarif else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device = 0 and tarif<>0 then reval/tarif else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device > 0 and tarif<>0 then reval/tarif else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device > 0 and tarif<>0 then reval/tarif else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device = 0 and tarif<>0 then real_charge/tarif else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device = 0 and tarif<>0 then real_charge/tarif else 0 end)," +
                          " sum(case when is_mkd = 1 and is_device > 0 and tarif<>0 then real_charge/tarif else 0 end)," +
                          " sum(case when is_mkd = 0 and is_device > 0 and tarif<>0 then real_charge/tarif else 0 end)" +
                          " from t_cursv a, " +
                          ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                          ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p," +
                          ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pp" +
                          " where a.nzp_supp = s.nzp_supp " +
                          " and s.nzp_payer_supp = p.nzp_payer " +
                          " and s.nzp_payer_princip = pp.nzp_payer " +
                          " group by 1,2,3,4,5 ";
                    ExecSQL(sql);

                    ExecSQL("truncate table t_cursv");
                }

                #endregion    

            }

            sql = " select principal, point, payer, name_norm,norm," +
                  " sum(sum_insaldo_mkd) as sum_insaldo_mkd, sum(sum_insaldo_chs) as sum_insaldo_chs," +
                  " sum(sum_money_mkd) as sum_money_mkd, sum(sum_money_chs) as sum_money_chs , " +
                  " sum(sum_tarif_mkd) as sum_tarif_mkd, sum(sum_tarif_chs) as sum_tarif_chs," +
                  " sum(reval_mkd) as reval_mkd, sum(reval_chs) as reval_chs, " +
                  " sum(real_charge_mkd) as real_charge_mkd, sum(real_charge_chs) as real_charge_chs," +
                  " sum(sum_outsaldo_mkd) as sum_outsaldo_mkd, sum(sum_outsaldo_chs) as sum_outsaldo_chs  , " +
                  " sum(mkd_gil_norm) as mkd_gil_norm, sum(mkd_gil_ipu) as mkd_gil_ipu, " +
                  " sum(mkd_gil_norm + mkd_gil_ipu) as mkd_gil, " +
                  " sum(chs_gil_norm) as chs_gil_norm, sum(chs_gil_ipu) as chs_gil_ipu, " +
                  " sum(chs_gil_norm + chs_gil_ipu) as chs_gil, " +
                  " sum(mkd_ls) as mkd_ls, sum(chs_ls) as chs_ls, " +
                  " sum(mkd_ls + chs_ls) as ls, " +
                  " sum(sum_tarif_m3_mkd_norm) as sum_tarif_m3_mkd_norm, sum(sum_tarif_m3_chs_norm) as sum_tarif_m3_chs_norm, " +
                  " sum(sum_tarif_m3_mkd_ipu) as sum_tarif_m3_mkd_ipu, sum(sum_tarif_m3_chs_ipu) as sum_tarif_m3_chs_ipu, " +
                  " sum(reval_m3_mkd_norm) as reval_m3_mkd_norm, sum(reval_m3_chs_norm) as reval_m3_chs_norm, " +
                  " sum(reval_m3_mkd_ipu) as reval_m3_mkd_ipu, sum(reval_m3_chs_ipu) as reval_m3_chs_ipu, " +
                  " sum(real_charge_m3_mkd_norm) as real_charge_m3_mkd_norm, sum(real_charge_m3_chs_norm) as real_charge_m3_chs_norm," +
                  " sum(real_charge_m3_mkd_ipu) real_charge_m3_mkd_ipu,sum(real_charge_m3_chs_ipu) as real_charge_m3_chs_ipu" +
                  " from t_svod " +
                  " group by principal, point, payer, name_norm,norm " +
                  " order by principal, point, payer, name_norm,norm ";

            DataTable supplierTable = ExecSQLToTable(sql);
            supplierTable.TableName = "Q_master";

            ds.Tables.Add(supplierTable);

            #region Отопление
            sql = " select pp.payer as Principal, p.payer as payer, heater, " +
                  " (case when prizn=0 then 'По нормативу' when prizn=1 then 'По ИПУ' end) as prizn," +
                  " sum(case when is_mkd=1 then sum_insaldo end) as sum_insaldo_mkd," +
                  " sum(case when is_mkd<>1 then sum_insaldo end) as sum_insaldo_chs," +
                  " sum(case when is_mkd=1 then sum_money end) as sum_money_mkd," +
                  " sum(case when is_mkd<>1 then sum_money end) as sum_money_chs," +
                  " sum(case when is_mkd=1 then sum_tarif end) as sum_tarif_mkd," +
                  " sum(case when is_mkd<>1 then sum_tarif end) as sum_tarif_chs," +
                  " sum(case when is_mkd=1 then reval end) as reval_mkd," +
                  " sum(case when is_mkd<>1 then reval end) as reval_chs," +
                  " sum(case when is_mkd=1 then real_charge end) as real_charge_mkd," +
                  " sum(case when is_mkd<>1 then real_charge end) as real_charge_chs," +
                  " sum(case when is_mkd=1 then sum_outsaldo end) as sum_outsaldo_mkd," +
                  " sum(case when is_mkd<>1 then sum_outsaldo end) as sum_outsaldo_chs" +
                  " from t_svod8 a, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pp" +
                  " where " +
                  " prizn<>2 " +
                  " and a.nzp_supp = s.nzp_supp " +
                  " and s.nzp_payer_supp = p.nzp_payer " +
                  " and s.nzp_payer_princip = pp.nzp_payer " +
                  " group by 1,2,3,4 ";
            DataTable supplierTable1 = ExecSQLToTable(sql);
            supplierTable1.TableName = "Q_master1";

            ds.Tables.Add(supplierTable1);

            sql = " select pp.payer as principal, p.payer as payer, heater, " +
                  " sum(case when (is_mkd=1 and  has_opu =1) then c_calc end) as c_calc_mkd_pu," +
                  " sum(case when (is_mkd<>1 and has_opu=1) then c_calc end) as c_calc_chs_pu," +
                  " sum(case when (is_mkd=1 and prizn=0) then c_calc end) as c_calc_mkd_norm," +
                  " sum(case when (is_mkd<>1 and prizn=0) then c_calc end) as c_calc_chs_norm," +
                  " sum(case when (is_mkd=1 and has_opu=1) then pl_ob end) as pl_ob_mkd_pu," +
                  " sum(case when (is_mkd<>1 and has_opu=1) then pl_ob end) as pl_ob_chs_pu," +
                  " sum(case when (is_mkd=1 and prizn=0) then pl_ob end) as pl_ob_mkd_norm," +
                  " sum(case when (is_mkd<>1 and prizn=0) then pl_ob end) as pl_ob_chs_norm," +
                  " sum(case when (is_mkd=1 and prizn=2) then pl_ob end) as pl_ob_mkd_io," +
                  " sum(case when (is_mkd<>1 and prizn=2) then pl_ob end) as pl_ob_chs_io," +
                  " sum(case when (is_mkd=1 and has_opu=1) then count_ls end) as count_ls_mkd_pu," +
                  " sum(case when (is_mkd<>1 and has_opu=1) then count_ls end) as count_ls_chs_pu," +
                  " sum(case when (is_mkd=1 and prizn=0) then count_ls end) as count_ls_mkd_norm," +
                  " sum(case when (is_mkd<>1 and prizn=0) then count_ls end) as count_ls_chs_norm," +
                  " sum(case when (is_mkd=1 and prizn=2) then count_ls end) as count_ls_mkd_io," +
                  " sum(case when (is_mkd<>1 and prizn=2) then count_ls end) as count_ls_chs_io" +
                  " from t_svod8 a, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pp" +
                  " where " +
                  " prizn<>2 " +
                  " and a.nzp_supp = s.nzp_supp " +
                  " and s.nzp_payer_supp = p.nzp_payer " +
                  " and s.nzp_payer_princip = pp.nzp_payer " +
                  " group by 1,2,3";
            DataTable supplierTable2 = ExecSQLToTable(sql);
            supplierTable2.TableName = "Q_master2";

            ds.Tables.Add(supplierTable2);

            sql = " select pp.payer as principal, p.payer as payer, heater, has_opu, " +
                  " (CASE WHEN TRIM(rajon)='-' OR rajon IS NULL THEN (CASE WHEN town IS NULL THEN '' ELSE TRIM(town)  END) ELSE TRIM(rajon) END) AS rajon," +
                  " ulicareg, ulica, ndom," +
                  " (CASE WHEN nkor IS NULL OR TRIM(nkor)='-' OR TRIM(nkor) ='' THEN '' ELSE ' корп. ' || TRIM(nkor) END) AS nkor, " +
                  " sum(c_calc) as c_calc," +
                  " sum(sum_money) as sum_money," +
                  " sum(sum_tarif) as sum_tarif," +
                  " sum(reval) as reval," +
                  " sum(real_charge) as real_charge," +
                  " sum(pl_ob) as pl_ob," +
                  " sum(pl_otopl) as pl_otopl," +
                  " sum(case when prizn=2 then pl_ob end) as pl_ob_io," +
                  " sum(case when prizn=1 then pl_ob end) as pl_ob_ipu," +
                  " sum(case when prizn=1 then count_ls end) as count_ls_ipu," +
                  " sum(case when prizn=0 then count_ls end) as count_ls_norm," +
                  " sum(case when prizn=2 then count_ls end) as count_ls_io," +
                  " sum(case when prizn=1 then gil end) as gil_ipu," +
                  " sum(case when prizn=0 then gil end) as gil_norm," +
                  " sum(case when prizn=2 then gil end) as gil_io" +
                  " from t_svod8 a, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pp," +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d " +
                  " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                  "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                  " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                  "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                  " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                  "s_town t ON t.nzp_town = r.nzp_town " +
                  " where a.nzp_dom = d.nzp_dom and " +
                  " prizn<>2 " +
                  " and a.nzp_supp = s.nzp_supp " +
                  " and s.nzp_payer_supp = p.nzp_payer " +
                  " and s.nzp_payer_princip = pp.nzp_payer " +
                  " group by 1,2,3,4,5,rajon, ulica, ulicareg, idom, ndom, nkor " +
                  " order by rajon, ulica, ulicareg, idom, ndom ";
            DataTable supplierTable3 = ExecSQLToTable(sql);
            supplierTable3.TableName = "Q_master3";

            ds.Tables.Add(supplierTable3); 
            #endregion  
           
            return ds;
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
            return whereWp;
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
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        protected override void CreateTempTable()
        {
            string sql;

            sql = " Create temp table t_cursv( " +
                  " nzp_dom integer," +
                  " nzp_kvar integer," +
                  " nzp_supp integer," +
                  " nzp_y integer default 0," +
                  " is_mkd integer default 0," +
                  " name_norm char(100)," +
                  " norm " + DBManager.sDecimalType + "(14,4)," +
                  " tarif " + DBManager.sDecimalType + "(14,4)," +
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_tarif " + DBManager.sDecimalType + "(14,2)," +
                  " reval " + DBManager.sDecimalType + "(14,2)," +
                  " real_charge " + DBManager.sDecimalType + "(14,2)," +
                  " sum_money " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                  " c_calc " + DBManager.sDecimalType + "(14,7)," +
                  " is_device integer," +
                  " gil integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

                sql = " CREATE TEMP TABLE t_svod8( " +
                      " nzp_supp integer, " +
                      " nzp_payer_supp integer, " +
                      " nzp_payer_princip integer, " +
                      " heater char(100)," +
                      " has_opu integer, " +
                      " nzp_dom integer, " +
                      " is_mkd integer default 0, " +
                      " prizn integer, "+ //0-norm 1-ipu 2-io
                      " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                      " sum_money  " + DBManager.sDecimalType + "(14,2), " +
                      " sum_tarif  " + DBManager.sDecimalType + "(14,2), " +
                      " reval  " + DBManager.sDecimalType + "(14,2), " +
                      " real_charge " + DBManager.sDecimalType + "(14,2), " +
                      " sum_outsaldo  " + DBManager.sDecimalType + "(14,2), " +
                      " c_calc " + DBManager.sDecimalType + "(14,7), " +
                      " gil integer, " +
                      " count_ls  " + DBManager.sDecimalType + "(14,2), " +
                      " pl_ob " + DBManager.sDecimalType + ", " +
                      " pl_otopl " + DBManager.sDecimalType + " " +
                      " ) " + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                sql = " CREATE TEMP TABLE t_svod( " +
                             " point char(100), " +
                             " payer char(40), " +
                             " principal char(40), " +
                             " name_norm char(100), " +
                             " norm  " + DBManager.sDecimalType + "(14,4), " +
                             " sum_insaldo_mkd  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_insaldo_chs  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_money_mkd  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_money_chs  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_tarif_mkd  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_tarif_chs  " + DBManager.sDecimalType + "(14,2), " +
                             " reval_mkd  " + DBManager.sDecimalType + "(14,2), " +
                             " reval_chs  " + DBManager.sDecimalType + "(14,2), " +
                             " real_charge_mkd  " + DBManager.sDecimalType + "(14,2), " +
                             " real_charge_chs  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_outsaldo_mkd  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_outsaldo_chs  " + DBManager.sDecimalType + "(14,2), " +
                             " mkd_c_calc_norm  " + DBManager.sDecimalType + "(14,2), " +
                             " mkd_c_calc_ipu  " + DBManager.sDecimalType + "(14,2), " +
                             " chs_c_calc_norm  " + DBManager.sDecimalType + "(14,2), " +
                             " chs_c_calc_ipu  " + DBManager.sDecimalType + "(14,2), " +
                             " mkd_gil_norm  " + DBManager.sDecimalType + "(14,2), " +
                             " mkd_gil_ipu  " + DBManager.sDecimalType + "(14,2), " +
                             " chs_gil_norm  " + DBManager.sDecimalType + "(14,2), " +
                             " chs_gil_ipu  " + DBManager.sDecimalType + "(14,2), " +
                             " mkd_ls  " + DBManager.sDecimalType + "(14,2), " +
                             " chs_ls  " + DBManager.sDecimalType + "(14,2), " +
                             " sum_tarif_m3_mkd_norm " + DBManager.sDecimalType + "(14,7), " +
                             " sum_tarif_m3_chs_norm " + DBManager.sDecimalType + "(14,7), " +
                             " sum_tarif_m3_mkd_ipu " + DBManager.sDecimalType + "(14,7), " +
                             " sum_tarif_m3_chs_ipu " + DBManager.sDecimalType + "(14,7), " +
                             " reval_m3_mkd_norm " + DBManager.sDecimalType + "(14,7), " +
                             " reval_m3_chs_norm " + DBManager.sDecimalType + "(14,7), " +
                             " reval_m3_mkd_ipu " + DBManager.sDecimalType + "(14,7)," +
                             " reval_m3_chs_ipu " + DBManager.sDecimalType + "(14,7), " +
                             " real_charge_m3_mkd_norm " + DBManager.sDecimalType + "(14,7), " +
                             " real_charge_m3_chs_norm " + DBManager.sDecimalType + "(14,7), " +
                             " real_charge_m3_mkd_ipu " + DBManager.sDecimalType + "(14,7)," +
                             " real_charge_m3_chs_ipu " + DBManager.sDecimalType + "(14,7), " +
                             " sum_tarif_m3_mkd " + DBManager.sDecimalType + "(14,7), " +
                             " sum_tarif_m3_chs " + DBManager.sDecimalType + "(14,7)," +
                             " reval_m3_mkd " + DBManager.sDecimalType + "(14,7), " +
                             " reval_m3_chs " + DBManager.sDecimalType + "(14,7), " +
                             " real_charge_m3_mkd " + DBManager.sDecimalType + "(14,7), " +
                             " real_charge_m3_chs " + DBManager.sDecimalType + "(14,7) " +
                             " ) " + DBManager.sUnlogTempTable;

            ExecSQL(sql);

            sql = "create index ix_t_cursv01 on t_cursv(nzp_kvar, nzp_supp)";
            ExecSQL(sql);
            sql = "create index ix_t_cursv02 on t_cursv(nzp_dom)";
            ExecSQL(sql);
            sql = "create index ix_t_cursv03 on t_cursv(nzp_kvar)";
            ExecSQL(sql);

            ExecSQL(DBManager.sUpdStat + " t_cursv");

        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_svod ");
            ExecSQL(" DROP TABLE t_svod8 ");
            ExecSQL(" DROP TABLE t_cursv ");
            ExecSQL(" DROP TABLE t_cursv8 ");
        }
    }
}

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
    class Report710120 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.20 Ежемесячный отчет в Правительство"; }
        }

        public override string Description
        {
            get { return "Ежемесячный отчет в Правительство"; }
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
            get { return Resources.Report_71_1_20; }
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

        /// <summary>Заголовок отчета принципалы</summary>
        protected string PrincipalHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }
        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        private int LC { get; set; }

        //делать детлизацию по ЛС 1
        private int LCTable { get; set; }
        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter(),
                new ComboBoxParameter
                {
                    Code = "Mkd",
                    Name = "МКД/Частный сектор",
                    Value = "3",
                    DefaultValue = "3",
                    
                    StoreData = new List<object>
                    {
                        new { Id = "3", Name = "Все" },
                        new { Id = "1", Name = "МКД" },
                        new { Id = "2", Name = "Частный сектор" },
                    }
                },

                new ComboBoxParameter
                {
                    Code = "LC",
                    Name = "ЛС",
                    Value = "1",
                    DefaultValue = "1",
                    
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Все" },
                        new { Id = "2", Name = "Только открытые" }
                    }
                }, 
                new ComboBoxParameter
                {
                    Code = "LCt",
                    Name = "Вид отчета",
                    Value = 0,
                    DefaultValue = 0,
                    StoreData = new List<object>
                    {
                        new { Id = 0, Name = "Только по муниципальным образованиям" },
                        new { Id = 1, Name = "С детализацией по ЛС" },
                    }      
                },
              
            };
        }

        public override DataSet GetData()
        {


            #region Выборка по локальным банкам

            string  sql,
                    whereWP = GetwhereWp(),
                    whereSupp = GetWhereSupp("ch.nzp_supp");
            string datS = "1." + Month + "." + Year,
                datPo = DateTime.DaysInMonth(Year, Month) + "." + Month + "." + Year;
            foreach (var pref in PrefBanks)
            {
                string tableCharge = pref + "_charge_" + (Year - 2000).ToString("00") +
                      DBManager.tableDelimiter + "charge_" +
                      Month.ToString("00");
                string tablePerekidka = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                        "perekidka";
                string baseData = pref + DBManager.sDataAliasRest;
                string baseKernel = pref + DBManager.sKernelAliasRest;
                string sqlMkd = "";

                switch (Mkd)
                {  
                    case 1:
                        sqlMkd = " and ch.nzp_kvar in (select nzp_kvar " +
                                 " from "+pref + DBManager.sDataAliasRest + "kvar k," +
                                 pref + DBManager.sDataAliasRest + "prm_2 p " +
                                 " where p.nzp=k.nzp_dom and nzp_prm=2030 and is_actual=1 and val_prm='1')";
                        break;
                    case 2:
                        sqlMkd = " and ch.nzp_kvar not in (select nzp_kvar" +
                                 " from " + pref + DBManager.sDataAliasRest + "kvar k," +
                                 pref + DBManager.sDataAliasRest + "prm_2 p " +
                                 " where p.nzp=k.nzp_dom and nzp_prm=2030 and is_actual=1 and val_prm='1')"; 
                        break;
                }
            
                string sqlLCwhere = "";
                if (LC == 2)
                {
                    sqlLCwhere = " and ch.nzp_kvar in (select nzp " +
                                 " from "+ baseData + "prm_3 p3 " +
                                 " where p3.val_prm = '1'   and " +
                                 "  p3.dat_s <= '" + datPo + "' " +
                                 "  AND p3.dat_po >= '" + datS + "' " +
                                 "  AND p3.is_actual <>100 " +
                                 "  AND p3.nzp_prm = 51 )";
                }

                if (TempTableInWebCashe(tableCharge))
                {
                    sql = " insert into t_svod(nzp_supp,  nzp_kvar, sum_outsaldo, sum_real, reval )" +
                          " select nzp_supp, ch.nzp_kvar, sum(sum_outsaldo), sum(sum_real) , sum(reval)" +
                          " from " + tableCharge + " ch , " + baseData + "kvar k, " + baseData +
                              "dom d, " + baseData + " s_ulica su " + 
                          " where ch.nzp_serv >1 and dat_charge is null " +
                          " and k.nzp_dom=d.nzp_dom and  ch.nzp_kvar=k.nzp_kvar and d.nzp_ul=su.nzp_ul " +
                          sqlMkd+ whereSupp +  sqlLCwhere +
                          " group by 1,2";
                    ExecSQL(sql);

                    //с сальдовкой не бьётся, но вообщет правильно
                    //sql = " insert into t_svod(nzp_supp,  num_ls, sum_outsaldo, sum_real, reval )" +
                    //      " select nzp_supp, ch.num_ls, sum(sum_outsaldo), sum(sum_real) , sum(reval)" +
                    //      " from " + tableCharge + " ch " +
                    //      " where ch.nzp_serv >1 and dat_charge is null " +
                    //      sqlMkd + whereSupp +
                    //      " group by 1,2";
                    //ExecSQL(sql);

                    sql = " insert into t_svod ( real_charge)" +
                          " SELECT SUM(sum_rcl)" +
                          " FROM " + tablePerekidka + " ch, " + baseData + "kvar k, " + baseData +
                          "dom d, " + baseData + " s_ulica su " +
                          " WHERE ch.type_rcl not in (100,20) " +
                          " and k.nzp_dom=d.nzp_dom  and ch.nzp_kvar=k.nzp_kvar and d.nzp_ul=su.nzp_ul " +
                          " AND ch.nzp_kvar > 0 and nzp_serv>0 " +
                          whereSupp + sqlLCwhere +sqlMkd +
                          " AND ch.month_ = " + Month;
                         
                    ExecSQL(sql); 
                    
                    //sql = " UPDATE t_svod " +
                    //      " SET real_charge = ( SELECT SUM(sum_rcl)" +
                    //      " FROM " + tablePerekidka + " ch " +
                    //      " WHERE ch.type_rcl not in (100,20) " +
                    //      " AND ch.nzp_kvar > 0 and nzp_serv>0 " +
                    //      " AND ch.num_ls = t_svod.num_ls " +
                    //      " AND ch.nzp_supp = t_svod.nzp_supp " +
                    //        whereSupp +
                    //      " AND ch.month_ = " + Month + ") ";
                    //ExecSQL(sql);

                    sql = " insert into t_svod_dolg ( nzp_kvar, sum_outsaldo)" +
                          " select nzp_kvar, sum(sum_outsaldo)" +
                          " from  t_svod " +
                          " group by 1"+
                          " having sum(sum_outsaldo)>10000";
                    ExecSQL(sql);

                    sql = " update t_svod_dolg set has_naem = 1 " +
                          " where nzp_kvar in (select nzp_kvar from " + tableCharge + " where nzp_serv=15 and isdel=0)";
                    ExecSQL(sql);

                    if (LCTable == 1)
                    {
                        
                        sql = " update t_svod_dolg set type_sob = (select name_y " +
                              " from " + baseData + "prm_1 p, " + baseKernel + "res_y y" +
                              " where nzp=nzp_kvar and y.nzp_res=3017 and nzp_prm=1373 and val_prm" + DBManager.sConvToInt + "=nzp_y and is_actual <> 100 " +
                              " and dat_s<='" + datPo + "' and dat_po>='" + datS + " ')";
                        ExecSQL(sql);

                        sql =
                            " INSERT INTO t_res_ls (bd_kernel, nzp_kvar, num_ls, fio, nzp_dom, ikvar, nkvar, nkvar_n, has_naem, type_sob, sum_outsaldo) " +
                            " SELECT '"+pref+"', k.nzp_kvar, num_ls, fio, nzp_dom, ikvar, nkvar, case when nkvar_n != '-' then nkvar_n end, has_naem, type_sob, sum_outsaldo " +
                            " FROM t_svod_dolg d, " + pref + DBManager.sDataAliasRest + "kvar k " +
                            " WHERE d.nzp_kvar=k.nzp_kvar";
                        ExecSQL(sql);             
                    }

                    sql = " update t_svod_dolg set has_naem = 1 " +
                          " where nzp_kvar in (select nzp from " + baseData+ "prm_1 " + 
                          " where nzp_prm=1373 and val_prm='3' and is_actual <> 100 " +
                          " and dat_s<='" + datPo + "' and dat_po>='" + datS + " ')" +
                          " and has_naem <>1 ";
                    ExecSQL(sql);

                    //if (LCTable == 1)
                    //{
                    //    sql = " update t_svod_dolg set type_sob = select(" +
                    //          " where num_ls in (select nzp from " + baseData + "prm_1 " +
                    //          " where nzp_prm=1373 and val_prm='3' and is_actual <> 100 " +
                    //          " and dat_s<='" + datPo + "' and dat_po>='" + datS + " ')" +
                    //          " and has_naem is null ";
                    //    ExecSQL(sql);  
                    //}

                    sql = " insert into t_res (bd_kernel, sum_nach )" +
                          " select '"+ pref +"',  sum(sum_real)+sum(reval)+sum(real_charge) "+
                          " from t_svod ";
                    ExecSQL(sql);

                    sql = " update t_res set sum_dolg = (select sum(sum_outsaldo) " +
                          " from t_svod_dolg )"+
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql); 

                    string strMkd = "";
                    string strMkdcond= "";

                //  switch (Mkd)
                //{  
                //    case 1:
                //        strMkd = " left outer join " +
                //                 pref + DBManager.sDataAliasRest + "prm_2 p2 on p2.nzp=k.nzp_dom and  p2.nzp_prm=2030 and p2.is_actual=1 ";
                //        strMkdcond = "and p2.val_prm='1' ";
                //                 break;
                //    case 2:
                //        strMkd = " left outer join " +
                //                 pref + DBManager.sDataAliasRest + "prm_2 p2 on p2.nzp=k.nzp_dom and p2.nzp_prm=2030 and p2.is_actual=1  ";
                //        strMkdcond = " and (p2.val_prm!='1' or p2.val_prm is null) ";
                //                 break;
                //}    
                    
                    //выбирает все ЛС с начислениями
                    sql = " update t_res set count_ls_nach = (select count(distinct nzp_kvar) " +
                          " from t_svod " +
                          " where sum_real > 0)" +
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);  

                  //выбирает открытые лс
                  //sql = " update t_res set count_ls_nach = (select count(distinct num_ls) " +
                  //      " from " + baseData + "kvar k left outer join " +
                  //      pref + DBManager.sDataAliasRest + "prm_3 p3 on p3.nzp=k.num_ls and " +
                  //      "  p3.dat_s <= '" + datPo + "' " +
                  //      " AND p3.dat_po >= '" + datS + "' " +
                  //      " AND p3.is_actual = 1 " +
                  //      strMkd +
                  //      " where p3.nzp_prm = 51 " +
                  //      " AND p3.val_prm = '1' " +
                  //      ")" +
                  //      " where bd_kernel='" + pref + "'";
                  //ExecSQL(sql);

                  //выбирает открытые лс
                  //sql = " update t_res set count_ls_nach = (select count(distinct num_ls) " +
                  //      " from " + baseData + "kvar k left outer join " +
                  //      pref + DBManager.sDataAliasRest + "prm_3 p3 on p3.nzp=k.nzp_kvar and " +
                  //      "  p3.dat_s <= '" + datPo + "' " +
                  //      " AND p3.dat_po >= '" + datS + "' " +
                  //      " AND p3.is_actual = 1 " +
                  //      " AND p3.nzp_prm = 51 " +
                  //      strMkd +
                  //      " where  " +
                  //      " p3.val_prm = '1' or p3.val_prm is null " + strMkdcond +
                  //      ")" +
                  //      " where bd_kernel='" + pref + "'";
                  //ExecSQL(sql);

                    sql = " update t_res set count_ls = (select  count(distinct nzp_kvar) " +
                          " from t_svod_dolg )" +
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);

                    sql = " update t_res set sum_dolg_10 = (select  sum(sum_outsaldo) " +
                          " from t_svod_dolg " +
                          " where sum_outsaldo<50000)"+
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);

                    sql = " update t_res set count_ls_10 = (select count(nzp_kvar) " +
                          " from t_svod_dolg " +
                          " where sum_outsaldo<50000)"+
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);


                    sql = " update t_res set sum_dolg_50 = (select sum(sum_outsaldo) " +
                          " from t_svod_dolg " +
                          " where sum_outsaldo>50000)"+
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);

                    sql = " update t_res set count_ls_50 = (select count(nzp_kvar) " +
                          " from t_svod_dolg " +
                          " where sum_outsaldo>50000)"+
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);

                    sql = " update t_res set count_ls_naem = (select count(nzp_kvar) " +
                          " from t_svod_dolg " +
                          " where has_naem=1)" +
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);

                    sql = " update t_res set  sum_dolg_naem = (select sum(sum_outsaldo)" +
                          " from t_svod_dolg " +
                          " where has_naem=1)" +
                          " where bd_kernel='" + pref + "'";
                    ExecSQL(sql);

                    //sql = " update t_res set  count_supp = (select count(distinct nzp_payer_supp) " +
                    //      " from " + ReportParams.Pref + DBManager.sKernelAliasRest +"supplier_point sp,"
                    //               + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point p,"
                    //               + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s " +
                    //      "  where sp.nzp_supp=s.nzp_supp and sp.nzp_wp=p.nzp_wp and p.bd_kernel = '"+pref+"')" +
                    //      "  where bd_kernel='" + pref + "'";
                    //ExecSQL(sql);

                    //sql = " update t_res set  count_princip = (select count(distinct nzp_payer_princip) " +
                    //      " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier_point sp,"
                    //               + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point p,"
                    //               + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s " +
                    //      "  where sp.nzp_supp=s.nzp_supp and sp.nzp_wp=p.nzp_wp and p.bd_kernel = '" + pref + "')" +
                    //      " where bd_kernel='"+pref+"'";
                    //ExecSQL(sql);

                    ExecSQL("truncate t_svod");
                    ExecSQL("truncate t_svod_dolg");
                }
            }
            #endregion

            #region Выборка на экран 

            sql = " select t.*,p.point from t_res t," + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point p " +
                  " where t.bd_kernel = p.bd_kernel order by point";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            if (LCTable == 1)
            {
                sql =
                    " select (case when sum_outsaldo<50000 then 0 else 1 end) as type, p.point, num_ls,  nkvar, nkvar_n, fio, sum_outsaldo, type_sob, has_naem ," +
                    "(CASE WHEN TRIM(rajon)='-' OR rajon IS NULL THEN (CASE WHEN town IS NULL THEN '' ELSE TRIM(town)  END) ELSE TRIM(rajon) END) AS rajon," +
                    " ulicareg, ulica, ndom, (CASE WHEN nkor IS NULL OR TRIM(nkor)='-' OR TRIM(nkor) ='' THEN '' ELSE ' корп. ' || TRIM(nkor) END) AS nkor" +
                    " from t_res_ls trls," + ReportParams.Pref + DBManager.sDataAliasRest + "dom d" +
                    " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                    "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                    " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                    "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                    " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                    "s_town t ON t.nzp_town = r.nzp_town,  " + ReportParams.Pref + DBManager.sKernelAliasRest  + "s_point p " +
                    " where trls.nzp_dom = d.nzp_dom and trls.bd_kernel = p.bd_kernel " +
                    " order by type, point, rajon, ulica, ulicareg, idom, ndom, ikvar ";
            }
            else
            {
                sql = "select 1 as type,  p.point, trls.bd_kernel, num_ls, nkvar, nkvar_n, fio, " +
                  "sum_outsaldo, type_sob, has_naem ,''as rajon, ''as nkor,''as ulicareg, " +
                  "''as ulica,''as  ndom  " +
                  " from t_res_ls trls," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_point p " +
                " where trls.bd_kernel = p.bd_kernel order by 1,2";
            }


            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";
            #endregion

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {  
                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")"; 
            }

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
            
            if (BankSupplier != null && BankSupplier.Principals != null)
            {  
                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")"; 
                           
                if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp)) 
                {  
                    if (!String.IsNullOrEmpty(oldsupp))
                        whereSupp += " AND nzp_supp in (" + oldsupp + ")";
                
                    //Принципалы
                    PrincipalHeader = String.Empty;
                    string sqlp = " SELECT distinct payer from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p " +
                             " WHERE nzp_payer > 0  and nzp_payer=nzp_payer_princip " + whereSupp;
                    DataTable princip = ExecSQLToTable(sqlp); 
                    foreach (DataRow dr in princip.Rows)  
                    {     
                        PrincipalHeader += dr["payer"].ToString().Trim() + ", "; 
                    }  
                    PrincipalHeader = PrincipalHeader.TrimEnd(',', ' ');   
                }   
            }  


            if (BankSupplier != null && BankSupplier.Agents != null)
            {   
                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }
             
            whereSupp = whereSupp.TrimEnd(',');

            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        
        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
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

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("date", months[Month]+" "+Year);
            report.SetParameterValue("pMonth", months[Month]);
            report.SetParameterValue("pYear", Year);

            switch (Mkd)
            {
                case 1:
                    report.SetParameterValue("houses", "(многоквартирные дома) ");
                    break;
                case 2:
                    report.SetParameterValue("houses", "(частный сектор) ");
                    break;
                case 3:
                    report.SetParameterValue("houses", "");
                    break;   
            }

            switch (LC)
            {
                case 1:
                    report.SetParameterValue("LC", "(по всем ЛС) ");
                    break;
                case 2:
                    report.SetParameterValue("LC", "(только по открытым ЛС) ");
                    break;
            }
            report.SetParameterValue("LCt", LCTable);
            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(PrincipalHeader) ? "Принципалы: " + PrincipalHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Mkd = UserParamValues["Mkd"].GetValue<int>();
            LC = UserParamValues["LC"].GetValue<int>();
            LCTable = UserParamValues["LCt"].GetValue<int>();
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
            string sql = " create temp table t_svod (  " +
                               " nzp_kvar integer, " + 
                               " has_naem integer default 0, " +
                               " nzp_supp integer, " +
                               " nzp_serv integer, " +
                               " sum_real " + DBManager.sDecimalType + "(14,2) default 0, " +//начислено
                               " reval " + DBManager.sDecimalType + "(14,2) default 0, " +//перерасчет
                               " real_charge " + DBManager.sDecimalType + "(14,2) default 0, " +//корректировка 
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0.00 " + 
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" create index svod_index on t_svod(nzp_kvar)");

            sql = " create temp table t_svod_dolg (  " +
                   " nzp_kvar integer, " +
                   " type_sob char(50)," +
                   " has_naem integer default 0, " +
                   " sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0.00 " +
                   " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" create index svod_index_dolg on t_svod_dolg(nzp_kvar)");

            sql = " create temp table t_res_ls (  " +
                  " bd_kernel char(20), " +
                  " nzp_kvar integer, " +
                  " num_ls integer, " + 
                  " has_naem integer default 0, " +
                  " fio char(50)," +
                  " type_sob char(50)," +
                  " ikvar integer, " +
                  " nkvar char(10)," +
                  " nkvar_n char(10)," +
                  " nzp_dom integer, " +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0.00 " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" create index svod_index_lc on t_res_ls(nzp_kvar)");

            const string sqlfin = " create temp table t_res (  " +
                   " bd_kernel char(20), " +
                   " count_supp integer, " +
                   " count_princip integer, " +
                   " count_ls_nach integer, " +
                   " count_ls integer, " +
                   " count_ls_10 integer, " +
                   " count_ls_50 integer, " +
                   " count_ls_naem integer, " +
                   " sum_nach " + DBManager.sDecimalType + "(14,2) default 0, " +
                   " sum_dolg " + DBManager.sDecimalType + "(14,2) default 0, " +
                   " sum_dolg_10 " + DBManager.sDecimalType + "(14,2) default 0, " +
                   " sum_dolg_50 " + DBManager.sDecimalType + "(14,2) default 0, " +
                   " sum_dolg_naem " + DBManager.sDecimalType + "(14,2) default 0 " + 
                   " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sqlfin);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
            ExecSQL(" drop table t_svod_dolg ", true);
            ExecSQL(" drop table t_res ", true);
            ExecSQL(" drop table t_res_ls ", true);
        }

    }
}

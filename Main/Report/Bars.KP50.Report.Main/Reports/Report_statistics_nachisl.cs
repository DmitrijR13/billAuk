using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Main.Properties;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Main.Reports
{
    class Statistics_Nachisl : BaseSqlReport
    {
        public override string Name
        {
            get { return "31.1.1 Статистика начислений"; }
        }

        public override string Description
        {
            get { return "Статистика начислений"; }
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
            get
            {
                var retRep = Resources.Web_nachisl_ls;
                switch (ReportType)
                {
                    case 1:
                        retRep = Resources.Web_nachisl_ls;
                        break;
                    case 2:
                        retRep = Resources.Web_nachisl_dom;
                        break;
                    case 3:
                        retRep = Resources.Web_nachisl_uch;
                        break;
                }
                return retRep;
            }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }


        /// <summary>
        /// Объект адрес
        /// </summary>
        protected AddressParameterValue adr { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected int ReportType { get; set; }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }
        /// <summary>Районы</summary>
        protected string Raions { get; set; }

        /// <summary>Улицы</summary>
        protected string Streets { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Дома</summary>
        protected string Houses { get; set; }
        
        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Номер дома</summary>
        protected string NUch { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }



        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter{ Value = DateTime.Now.Month },
                new YearParameter{ Value = DateTime.Now.Year },
                new AreaParameter(),
                new SupplierAndBankParameter(),
                new StringParameter{Code ="NUch", Name = "Участок"},
                new AddressParameter(),
                new ComboBoxParameter
                {
                    Code = "ReportType",
                    Name = "Тип отчета",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Статистика начислений по лицевым счетам" },
                        new { Id = "2", Name = "Статистика начислений по домам" },
                        new { Id = "3", Name = "Статистика начислений по участкам" }
                    }
                }           
            };
        }

        public override DataSet GetData()
        {

            #region Выборка по локальным банкам

            MyDataReader reader;

            string table_name = "t_stat_nach_for_ls";
            string table_name_for_one_bank = "t_stat_nach_for_ls_one_bank";
            string sql;

            try
            {
                sql = " drop table " + table_name_for_one_bank;
                ExecSQL(sql);
            }
            catch{}


            string ddat = "'01." + Month.ToString("00") + "." + Year.ToString("0000") + "'"; 
            

            sql = " select bd_kernel as pref " +
                         " from " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " where nzp_wp>1 " + GetWhereWp(); 

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                #region создание временной таблички для одного
                sql = " create temp table t_stat_nach_for_ls_one_bank (" +
                              " nzp_kvar INTEGER," +
                              " num_ls CHAR(14), " +
                              " nzp_dom INTEGER," +
                              " nzp_area INTEGER," +
                              " nzp_geu INTEGER," +
                              " nzp_ul INTEGER," +
                              " fio CHAR(100), " +
                              " ulica CHAR(40), " +
                              " ndom CHAR(14), " +
                              " idom INTEGER, " +
                              " ikvar INTEGER, " +
                              " pref CHAR(14), " +
                              " p1 " + DBManager.sDecimalType + "(14,2), " +
                              " p2 CHAR(20), " +
                              " p3 " + DBManager.sDecimalType + "(14,0), " +
                              " p4 " + DBManager.sDecimalType + "(14,0), " +
                              " p5 " + DBManager.sDecimalType + "(14,0), " +
                              " p6 " + DBManager.sDecimalType + "(14,2), " +
                              " p7 " + DBManager.sDecimalType + "(14,2), " +
                              " p8 " + DBManager.sDecimalType + "(14,0), " +
                              " p9 " + DBManager.sDecimalType + "(14,2), " +
                              " p10 " + DBManager.sDecimalType + "(14,2), " +
                              " p11 " + DBManager.sDecimalType + "(14,2), " +
                              " p12 " + DBManager.sDecimalType + "(14,2), " +
                              " p13 " + DBManager.sDecimalType + "(14,2), " +
                              " p14 " + DBManager.sDecimalType + "(14,2), " +
                              " p15 " + DBManager.sDecimalType + "(14,2), " +
                              " p16 " + DBManager.sDecimalType + "(14,2), " +
                              " p17 " + DBManager.sDecimalType + "(14,2), " +
                              " p18 " + DBManager.sDecimalType + "(14,2), " +
                              " p19 " + DBManager.sDecimalType + "(14,2), " +
                              " p20 " + DBManager.sDecimalType + "(14,2), " +
                              " p21 " + DBManager.sDecimalType + "(14,2), " +
                              " p22 " + DBManager.sDecimalType + "(14,2), " +
                              " p23 " + DBManager.sDecimalType + "(14,2), " +
                              " p24 " + DBManager.sDecimalType + "(14,2), " +
                              " real_ch " + DBManager.sDecimalType + "(14,2), " +
                              " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                              " oplata " + DBManager.sDecimalType + "(14,2) )";


                ExecSQL(sql);
                #endregion

                sql = " insert into " + table_name_for_one_bank + " (nzp_kvar, num_ls, nzp_dom, nzp_area, nzp_geu," +
                      " nzp_ul, fio, ulica, ndom, idom, ikvar, pref, " +
                      " p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17, p18, p19, p20, p21, p22, p23, p24, " +
                      " real_ch, sum_insaldo, oplata )" +

                      " select k.nzp_kvar , k.num_ls , k.nzp_dom, d.nzp_area, k.uch," +
                      " d.nzp_ul, k.fio, ulica, ndom, idom, ikvar, '" + pref +
                      "' ,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 " +
                      " from " + pref + DBManager.sDataAliasRest + " kvar k, " +
                      pref + DBManager.sDataAliasRest + " dom d, " +
                      pref + DBManager.sDataAliasRest + " s_ulica u, " +
                      pref + DBManager.sDataAliasRest + " s_rajon r " +
                      " where k.nzp_dom = d.nzp_dom " + GetWhereAdr("k.") + GetWhereGeu("k.") +
                      " and d.nzp_ul = u.nzp_ul and  r.nzp_raj = u.nzp_raj " + Raions + Streets + Houses;
                ExecSQL(sql);

                sql = "create index ix_" + table_name_for_one_bank + " on " + table_name_for_one_bank + " (nzp_dom , nzp_kvar)";
                ExecSQL(sql);


                #region параметры ЛС

                // количество счетов 

                sql = " update " + table_name_for_one_bank + " set p1 = " + DBManager.sNvlWord + "( " +
                      "(select  max( b.val_prm) from " + pref + DBManager.sDataAliasRest +
                      "prm_1 b where b.nzp_prm =21 and b.val_prm=1 and b.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between b.dat_s and b.dat_po and b.is_actual<>100), 0)";

                ExecSQL(sql);

                // приватизированных квартир


                sql = " update " + table_name_for_one_bank + " set p2 =  " + DBManager.sNvlWord + "( " +
                      "(select max( b.val_prm) from " + pref + DBManager.sDataAliasRest +
                      "prm_1 b where b.nzp_prm =8 and b.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between b.dat_s and b.dat_po  and b.is_actual<>100 ),0 )";

                ExecSQL(sql);

                sql = "update " + table_name_for_one_bank + " set p2 = 'да' where trim(p2) = '1'";

                ExecSQL(sql);

                sql = "update " + table_name_for_one_bank + " set p2 = 'нет' where p2 is null";

                ExecSQL(sql);

                // количество жильцов  

                sql = " update " + table_name_for_one_bank + " set p3 = " + DBManager.sNvlWord +
                      "( (select sum(" + DBManager.sNvlWord + "(a.val_prm,0) " + DBManager.sConvToNum + " ) from " +
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 5 and a.nzp=" +
                      table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0 )";

                ExecSQL(sql);

                // количество временно прибывших 

                sql = " update " + table_name_for_one_bank + " set p4 = " + DBManager.sNvlWord +
                      "( (select sum(" + DBManager.sNvlWord + "(a.val_prm,0) " + DBManager.sConvToNum + " ) from " +
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 131 and a.nzp=" +
                      table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po   and a.is_actual<>100), 0 )";

                ExecSQL(sql);

                // временно выбывшие 

                sql = " update " + table_name_for_one_bank + " set p5 = " + DBManager.sNvlWord +
                      "( (select sum(" + DBManager.sNvlWord + "(a.val_prm,0) " + DBManager.sConvToNum + " ) from " +
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 10 and a.nzp=" +
                      table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0)";

                ExecSQL(sql);

                // общая площадь

                sql = " update " + table_name_for_one_bank + " set p6 = " + DBManager.sNvlWord + "( " +
                      "(select sum(" + DBManager.sNvlWord + "(a.val_prm,0) " + DBManager.sConvToNum + " ) from " + 
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 4 and a.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0) ";

                ExecSQL(sql);

                // жилая площадь 

                sql = "update " + table_name_for_one_bank + " set p7 = " + DBManager.sNvlWord + "( " +
                      "(select sum(" + DBManager.sNvlWord + "(a.val_prm,0) " + DBManager.sConvToNum + " ) from " + 
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 6 and a.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0 )";

                ExecSQL(sql);

                // количество комнат 

                sql = "update " + table_name_for_one_bank + " set p8 = " + DBManager.sNvlWord + "(" +
                      "(select sum(" + DBManager.sNvlWord + "(a.val_prm,0) " + DBManager.sConvToNum + " ) from " + 
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 107 and a.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0 )";

                ExecSQL(sql);


                #endregion

                #region начисления

                string[] field_name = new string[]
                {
                    "p9", "p10", "p11", "p12", "p13", "p14", "p15", "p16", "p17", "p18", "p19", "p20", "p21", "p22",
                    "p23",
                    "p24"
                };
                int[] nzp_serv = new int[] {2, 266, 15, 6, 7, 8, 9, 16, 5, 26, 206, 515, 251, 274, 17, 464};
                string serv = "";

                for (int i = 0; i < field_name.Length; i++)
                {
                    serv += nzp_serv[i].ToString().Trim();
                    if ((i + 1) != field_name.Length) serv += ",";

                    sql = " update " + table_name_for_one_bank + " set " + field_name[i] + " = (" +
                          "Select  sum(c.sum_tarif +  c.reval + " + DBManager.sNvlWord + "(c.real_charge, 0))  from " +
                          pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                          " charge_" + Month.ToString("00") + " c " +
                          "Where c.nzp_kvar=" + table_name_for_one_bank +
                          ".nzp_kvar and c.dat_charge is null and c.nzp_serv > 1   and c.nzp_serv = " +
                          nzp_serv[i].ToString() +
                          " group by c.num_ls, c.nzp_serv)";

                    ExecSQL(sql);

                }

                sql = " update " + table_name_for_one_bank + " set real_ch = (" +
                      "Select  sum(c.real_charge)  from " + pref + "_charge_" + (Year - 2000).ToString("00") +
                      DBManager.tableDelimiter +
                      " charge_" + Month.ToString("00") + " c " +
                      "Where c.nzp_kvar=" + table_name_for_one_bank +
                      ".nzp_kvar and c.dat_charge is null and c.nzp_serv <>1  and c.nzp_serv in (" + serv + ") ) ";

                ExecSQL(sql);


                //участок

                sql = " update " + table_name_for_one_bank + " set nzp_geu = " +
                      "(Select  uch  from " + pref + DBManager.sDataAliasRest + " kvar k  Where k.nzp_kvar = " + table_name_for_one_bank + ".nzp_kvar ) ";

                ExecSQL(sql);


                // входящее сальдо

                sql = " update " + table_name_for_one_bank +
                      " set sum_insaldo = ( select sum(cast(nvl(a.sum_insaldo,0) as decimal(14,2))) from " + pref +
                      "_charge_" + (Year - 2000) + DBManager.tableDelimiter +
                      " charge_" + Month.ToString().PadLeft(2, '0') + " a  where a.nzp_kvar = " + table_name_for_one_bank +
                      ".nzp_kvar" +
                      " and a.num_ls = " + table_name_for_one_bank + ".num_ls and nzp_serv <> 1)";

                ExecSQL(sql);


                // Оплата

                sql = " update " + table_name_for_one_bank +
                      " set oplata = ( select sum(nvl(a.sum_money,0) " + DBManager.sConvToNum + " ) from " + pref + "_charge_" +
                      (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                      " charge_" + Month.ToString("00") + " a  where a.nzp_kvar = " + table_name_for_one_bank +
                      ".nzp_kvar and a.num_ls = " + table_name_for_one_bank + ".num_ls)";

                ExecSQL(sql);


                #endregion

                sql = "insert into " + table_name +
                    " select * from " + table_name_for_one_bank;

                ExecSQL(sql);

                sql = " drop table " + table_name_for_one_bank;


                ExecSQL(sql);

            }

            reader.Close();

            #endregion

            #region Выборка на экран
        
            switch (ReportType)
            {
                case 1:
                    sql =
                        "select num_ls, ulica, ndom as dom, nzp_geu as uch, ikvar as kv, " + DBManager.sNvlWord + "(p1,'1') as kolsch, replace(" + DBManager.sNvlWord + "(p2,''),'1','+') as privat," +
                        " fio, " + DBManager.sNvlWord + "(p3,0) as kolgil, " + DBManager.sNvlWord + "(p4,0) as kolvr, " + DBManager.sNvlWord + "(p5,0) as kolvibit, " +
                        " " + DBManager.sNvlWord + "(p6,0) as pl_ob, " + DBManager.sNvlWord + "(p7,0) as pl_gil, p8 as kolkom, sum_insaldo as in_saldo," + 
                        DBManager.sNvlWord + "(p9,0) as p1, " + DBManager.sNvlWord + "(p10,0) as p2," +
                        " " + DBManager.sNvlWord + "(p11,0) as p3, " + DBManager.sNvlWord + "(p12,0) as p4, " + DBManager.sNvlWord + "(p13,0) as p5, " + DBManager.sNvlWord + "(p14,0) as p6, " +
                        " " + DBManager.sNvlWord + "(p15,0) as p7, " + DBManager.sNvlWord + "(p16,0) as p8, " + DBManager.sNvlWord + "(p17,0) as p9, " + 
                        DBManager.sNvlWord + "(p18,0) as p10, " + DBManager.sNvlWord + "(p20,0) as p11, " + DBManager.sNvlWord + "(p19,0) as p12," +
                        " " + DBManager.sNvlWord + "(p21,0) as p21, " + DBManager.sNvlWord + "(p22,0) as p22," + DBManager.sNvlWord + "(p23,0) as p23, " + 
                        DBManager.sNvlWord + "(real_ch,0) as p13," +
                        " (" + DBManager.sNvlWord + "(sum_insaldo,0)+" + DBManager.sNvlWord + "(p9,0)+" + DBManager.sNvlWord + "(p10,0)+" + DBManager.sNvlWord + "(p11,0)+" + 
                        DBManager.sNvlWord + "(p12,0)+" + DBManager.sNvlWord + "(p13,0)+" + DBManager.sNvlWord + "(p14,0)+" + 
                        DBManager.sNvlWord + "(p15,0)+" + DBManager.sNvlWord + "(p16,0)+" + DBManager.sNvlWord + "(p17,0)" +
                        "+" + DBManager.sNvlWord + "(p18,0)+" + DBManager.sNvlWord + "(p19,0)+" + DBManager.sNvlWord + "(p20,0)+" + 
                        DBManager.sNvlWord + "(p21,0)+" + DBManager.sNvlWord + "(p22,0)+" + DBManager.sNvlWord + "(p23,0)) as p14, oplata " +
                        "from " + table_name +
                        " order by ulica,idom, ndom, ikvar";
                    break;
                case 2:
                    sql =
                        "select ulica, ndom as dom, nzp_geu as uch, sum(" + DBManager.sNvlWord + "(p6,0)) as obS," +
                        " sum(" + DBManager.sNvlWord + "(sum_insaldo,0)) as in_saldo, sum(" + DBManager.sNvlWord + "(p9,0)) as p1," +
                        " sum(" + DBManager.sNvlWord + "(p10,0)) as p2, sum(" + DBManager.sNvlWord + "(p11,0)) as p3, sum(" + DBManager.sNvlWord + "(p12,0)) as p4, " +
                        "  sum(" + DBManager.sNvlWord + "(p13,0)) as p5, sum(" + DBManager.sNvlWord + "(p14,0)) as p6, sum(" + DBManager.sNvlWord + "(p15,0)) as p7," +
                        " sum(" + DBManager.sNvlWord + "(p16,0)) as p8, sum(" + DBManager.sNvlWord + "(p17,0)) as p9, sum(" + DBManager.sNvlWord + "(p18,0)) as p10, " +
                        "  sum(" + DBManager.sNvlWord + "(p20,0)) as p11, sum(" + DBManager.sNvlWord + "(p19,0)) as p12, sum(" + DBManager.sNvlWord + "(p21,0)) as p21," +
                        " sum(" + DBManager.sNvlWord + "(p22,0)) as p22,sum(" + DBManager.sNvlWord + "(p23,0)) as p23, sum(" + DBManager.sNvlWord + "(real_ch,0)) as p13, " +
                        " sum((" + DBManager.sNvlWord + "(sum_insaldo,0)+" + DBManager.sNvlWord + "(p9,0)+" + DBManager.sNvlWord + "(p10,0)+" + DBManager.sNvlWord + "(p11,0)+" + 
                        DBManager.sNvlWord + "(p12,0)+" + DBManager.sNvlWord + "(p13,0)+" + DBManager.sNvlWord + "(p14,0)+" + 
                        DBManager.sNvlWord + "(p15,0)+" + DBManager.sNvlWord + "(p16,0)+" + DBManager.sNvlWord + "(p17,0)" +
                        "+" + DBManager.sNvlWord + "(p18,0)+" + DBManager.sNvlWord + "(p19,0)+" + DBManager.sNvlWord + "(p20,0)+" + DBManager.sNvlWord + "(p21,0)+" + 
                        DBManager.sNvlWord + "(p22,0)+" + DBManager.sNvlWord + "(p23,0))) as p14, sum(" + DBManager.sNvlWord + "(oplata,0)) as oplata " +
                        " from " + table_name + " group by nzp_dom, ulica, ndom, nzp_geu " +
                        " order by ulica,idom, ndom";
                    break;
                case 3:
                    sql =
                        "select nzp_geu as uch, sum(" + DBManager.sNvlWord + "(sum_insaldo,0)) as in_saldo," +
                        " sum(" + DBManager.sNvlWord + "(p9,0)) as p1, sum(" + DBManager.sNvlWord + "(p10,0)) as p2," +
                        " sum(" + DBManager.sNvlWord + "(p11,0)) as p3, sum(" + DBManager.sNvlWord + "(p12,0)) as p4, sum(" + DBManager.sNvlWord + "(p13,0)) as p5, " +
                        " sum(" + DBManager.sNvlWord + "(p14,0)) as p6, sum(" + DBManager.sNvlWord + "(p15,0)) as p7, sum(" + DBManager.sNvlWord + "(p16,0)) as p8," +
                        " sum(" + DBManager.sNvlWord + "(p17,0)) as p9, sum(" + DBManager.sNvlWord + "(p18,0)) as p10, sum(" + DBManager.sNvlWord + "(p20,0)) as p11, " +
                        " sum(" + DBManager.sNvlWord + "(p19,0)) as p12, sum(" + DBManager.sNvlWord + "(p21,0)) as p21,sum(" + DBManager.sNvlWord + "(p22,0)) as p22," +
                        " sum(" + DBManager.sNvlWord + "(p23,0)) as p23,  sum(" + DBManager.sNvlWord + "(real_ch,0)) as p13, " +
                        " sum((" + DBManager.sNvlWord + "(sum_insaldo,0)+" + DBManager.sNvlWord + "(p9,0)+" + 
                        DBManager.sNvlWord + "(p10,0)+" + DBManager.sNvlWord + "(p11,0)+" + 
                        DBManager.sNvlWord + "(p12,0)+" + DBManager.sNvlWord + "(p13,0)+" + DBManager.sNvlWord + "(p14,0)+" + 
                        DBManager.sNvlWord + "(p15,0)+" + DBManager.sNvlWord + "(p16,0)+" + DBManager.sNvlWord + "(p17,0)" +
                        "+" + DBManager.sNvlWord + "(p18,0)+" + DBManager.sNvlWord + "(p19,0)+" + DBManager.sNvlWord + "(p20,0)+" + DBManager.sNvlWord + "(p21,0)+" + 
                        DBManager.sNvlWord + "(p22,0)+" + DBManager.sNvlWord + "(p23,0))) as p14, sum(" + DBManager.sNvlWord + "(oplata,0)) as oplata  " +
                        " from " + table_name + " group by nzp_geu " +
                        " order by ulica, nzp_geu";
                    break;
            }
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private string GetWhereAdr(string tablePrefix)
        {
            var result = String.Empty;
            if (Areas != null)
            {
                result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            result = result.TrimEnd(',');
            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + tablePrefix + "nzp_area in (" + result + ")";
            }
            return result;
        }


        private string GetWhereGeu(string tablePrefix)
        {
            var result = String.Empty;

            result += !String.IsNullOrEmpty(NUch.Trim()) ? " AND " + tablePrefix + "uch in ( " + NUch + ")" : String.Empty;

            return result;
        }

        private string GetWhereWp()
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
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }


        protected override void PrepareReport(FastReport.Report report)
        {

            switch (ReportType)
            {
                case 1:
                    report.SetParameterValue("reportHeader", "Статистика начислений по лицевым счетам");
                    break;
                case 2:
                    report.SetParameterValue("reportHeader", "Статистика начислений по домам");
                    break;
                case 3:
                    report.SetParameterValue("reportHeader", "Статистика начислений по участкам");
                    break;
            }

            string[] MonthStr = new string[] { "", "январь", "февраль", "март", "апрель", "май", "июнь", 
                                            "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь" };
            report.SetParameterValue("month", MonthStr[Month]);
            report.SetParameterValue("year", Year.ToString());
        }

        protected override void PrepareParams()
        {
            adr = JsonConvert.DeserializeObject<AddressParameterValue>(UserParamValues["Address"].Value.ToString());
            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = "and r.nzp_raj in (" + String.Join(",", adr.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = "and u.nzp_ul in (" + String.Join(",", adr.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                    Houses = "and d.ndom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }
      
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();

            ReportType = UserParamValues["ReportType"].GetValue<int>();

            Areas = UserParamValues["Areas"].GetValue<List<int>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;

            NUch = UserParamValues["NUch"].GetValue<string>() ?? String.Empty;
        }

        protected override void CreateTempTable()
        {
            string sql = "";

                    sql = " create temp table t_stat_nach_for_ls (" +
                          " nzp_kvar INTEGER," +
                          " num_ls CHAR(14), " +
                          " nzp_dom INTEGER," +
                          " nzp_area INTEGER," +
                          " nzp_geu INTEGER," +
                          " nzp_ul INTEGER," +
                          " fio CHAR(100), " +
                          " ulica CHAR(40), " +
                          " ndom CHAR(14), " +
                          " idom INTEGER, " +
                          " ikvar INTEGER, " +
                          " pref CHAR(14), " +
                          " p1 " + DBManager.sDecimalType + "(14,2), " +
                          " p2 CHAR(20), " +
                          " p3 " + DBManager.sDecimalType + "(14,0), " +
                          " p4 " + DBManager.sDecimalType + "(14,0), " +
                          " p5 " + DBManager.sDecimalType + "(14,0), " +
                          " p6 " + DBManager.sDecimalType + "(14,2), " +
                          " p7 " + DBManager.sDecimalType + "(14,2), " +
                          " p8 " + DBManager.sDecimalType + "(14,0), " +
                          " p9 " + DBManager.sDecimalType + "(14,2), " +
                          " p10 " + DBManager.sDecimalType + "(14,2), " +
                          " p11 " + DBManager.sDecimalType + "(14,2), " +
                          " p12 " + DBManager.sDecimalType + "(14,2), " +
                          " p13 " + DBManager.sDecimalType + "(14,2), " +
                          " p14 " + DBManager.sDecimalType + "(14,2), " +
                          " p15 " + DBManager.sDecimalType + "(14,2), " +
                          " p16 " + DBManager.sDecimalType + "(14,2), " +
                          " p17 " + DBManager.sDecimalType + "(14,2), " +
                          " p18 " + DBManager.sDecimalType + "(14,2), " +
                          " p19 " + DBManager.sDecimalType + "(14,2), " +
                          " p20 " + DBManager.sDecimalType + "(14,2), " +
                          " p21 " + DBManager.sDecimalType + "(14,2), " +
                          " p22 " + DBManager.sDecimalType + "(14,2), " +
                          " p23 " + DBManager.sDecimalType + "(14,2), " +
                          " p24 " + DBManager.sDecimalType + "(14,2), " +
                          " real_ch " + DBManager.sDecimalType + "(14,2), " +
                          " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                          " oplata " + DBManager.sDecimalType + "(14,2) )";
           
      
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_stat_nach_for_ls ", true);

        }

    }
}

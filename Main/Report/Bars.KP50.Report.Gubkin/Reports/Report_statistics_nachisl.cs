using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.Gubkin.Properties;
using Constants = STCLINE.KP50.Global.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Gubkin.Reports
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
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
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
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
                ExecSQL(sql, false);
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
                              " kol_ls " + DBManager.sDecimalType + "(14,2), " +
                              " is_priv CHAR(20), " +
                              " kolgil " + DBManager.sDecimalType + "(14,0), " +
                              " kolvr " + DBManager.sDecimalType + "(14,0), " +
                              " kolvibit " + DBManager.sDecimalType + "(14,0), " +
                              " pl_ob " + DBManager.sDecimalType + "(14,2), " +
                              " pl_gil " + DBManager.sDecimalType + "(14,2), " +
                              " kolkom " + DBManager.sDecimalType + "(14,0), " +
                              " sod_i_rem_g " + DBManager.sDecimalType + "(14,2), " +
                              " z_tbo " + DBManager.sDecimalType + "(14,2), " +
                              " naim " + DBManager.sDecimalType + "(14,2), " +
                              " hv " + DBManager.sDecimalType + "(14,2), " +
                              " kanal " + DBManager.sDecimalType + "(14,2), " +
                              " otop " + DBManager.sDecimalType + "(14,2), " +
                              " gv " + DBManager.sDecimalType + "(14,2), " +
                              " v_tbo " + DBManager.sDecimalType + "(14,2), " +
                              " lift " + DBManager.sDecimalType + "(14,2), " +
                              " domofon " + DBManager.sDecimalType + "(14,2), " +
                              " kap_r " + DBManager.sDecimalType + "(14,2), " +
                              " odn_el " + DBManager.sDecimalType + "(14,2), " +
                              " kap_r_po_fz " + DBManager.sDecimalType + "(14,2), " +
                              " vznos_na_kap_r " + DBManager.sDecimalType + "(14,2), " +
                              " lest_kl " + DBManager.sDecimalType + "(14,2), " +
                              " real_ch " + DBManager.sDecimalType + "(14,2), " +
                              " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                              " oplata " + DBManager.sDecimalType + "(14,2) )";


                ExecSQL(sql);
                #endregion

                sql = " insert into " + table_name_for_one_bank + " (nzp_kvar, num_ls, nzp_dom, nzp_area, nzp_geu," +
                      " nzp_ul, fio, ulica, ndom, idom, ikvar, pref, " +
                      " kol_ls, is_priv, kolgil, kolvr, kolvibit, pl_ob, pl_gil, kolkom, sod_i_rem_g, z_tbo, naim," +
                      " hv, kanal, otop, gv, v_tbo,  lift, domofon, kap_r, odn_el, kap_r_po_fz, vznos_na_kap_r, lest_kl,  " +
                      " real_ch, sum_insaldo, oplata )" +

                      " select k.nzp_kvar , k.num_ls , k.nzp_dom, d.nzp_area, k.uch," +
                      " d.nzp_ul, k.fio, ulica, ndom, idom, ikvar, '" + pref +
                      "' ,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 " +
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

                sql = " update " + table_name_for_one_bank + " set kol_ls = " + DBManager.sNvlWord + "( " +
                      "(select  max( b.val_prm" + DBManager.sConvToNum + ") from " + pref + DBManager.sDataAliasRest +
                      "prm_1 b where b.nzp_prm =21 and (b.val_prm" + DBManager.sConvToNum + ")=1 and b.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between b.dat_s and b.dat_po and b.is_actual<>100), 0)";

                ExecSQL(sql);

                // приватизированных квартир


                sql = " update " + table_name_for_one_bank + " set is_priv =  " + DBManager.sNvlWord + "( " +
                      "(select max( b.val_prm " + DBManager.sConvToNum + ") from " + pref + DBManager.sDataAliasRest +
                      "prm_1 b where b.nzp_prm =8 and b.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between b.dat_s and b.dat_po  and b.is_actual<>100 ),0 )";

                ExecSQL(sql);

                sql = "update " + table_name_for_one_bank + " set is_priv = 'да' where trim(is_priv) = '1'";

                ExecSQL(sql);

                sql = "update " + table_name_for_one_bank + " set is_priv = 'нет' where is_priv is null";

                ExecSQL(sql);

                // количество жильцов  

                sql = " update " + table_name_for_one_bank + " set kolgil = " + DBManager.sNvlWord +
                      "( (select sum(" + DBManager.sNvlWord + "(a.val_prm" + DBManager.sConvToNum + ",0)  ) from " +
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 5 and a.nzp=" +
                      table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0 )";

                ExecSQL(sql);

                // количество временно прибывших 

                sql = " update " + table_name_for_one_bank + " set kolvr = " + DBManager.sNvlWord +
                      "( (select sum(" + DBManager.sNvlWord + "(a.val_prm" + DBManager.sConvToNum + ",0)  ) from " +
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 131 and a.nzp=" +
                      table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po   and a.is_actual<>100), 0 )";

                ExecSQL(sql);

                // временно выбывшие 

                sql = " update " + table_name_for_one_bank + " set kolvibit = " + DBManager.sNvlWord +
                      "( (select sum(" + DBManager.sNvlWord + "(a.val_prm" + DBManager.sConvToNum + ",0) " + DBManager.sConvToNum + " ) from " +
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 10 and a.nzp=" +
                      table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0)";

                ExecSQL(sql);

                // общая площадь

                sql = " update " + table_name_for_one_bank + " set pl_ob = " + DBManager.sNvlWord + "( " +
                      "(select sum(" + DBManager.sNvlWord + "(a.val_prm" + DBManager.sConvToNum + ",0) " + DBManager.sConvToNum + " ) from " + 
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 4 and a.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0) ";

                ExecSQL(sql);

                // жилая площадь 

                sql = "update " + table_name_for_one_bank + " set pl_gil = " + DBManager.sNvlWord + "( " +
                      "(select sum(" + DBManager.sNvlWord + "(a.val_prm" + DBManager.sConvToNum + ",0) " + DBManager.sConvToNum + " ) from " + 
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 6 and a.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0 )";

                ExecSQL(sql);

                // количество комнат 

                sql = "update " + table_name_for_one_bank + " set kolkom = " + DBManager.sNvlWord + "(" +
                      "(select sum(" + DBManager.sNvlWord + "(a.val_prm" + DBManager.sConvToNum + " ,0) " + DBManager.sConvToNum + " ) from " + 
                      pref + DBManager.sDataAliasRest + "prm_1 a where a.nzp_prm = 107 and a.nzp=" + table_name_for_one_bank + ".nzp_kvar " +
                      "and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100), 0 )";

                ExecSQL(sql);


                #endregion

                #region начисления

                string[] field_name = new string[]
                {
                    "sod_i_rem_g", "z_tbo", "naim", "hv", "kanal", "otop", "gv", "v_tbo", "lift", "domofon", "kap_r", "odn_el", "kap_r_po_fz", "vznos_na_kap_r",
                    "lest_kl"
                };
                int[] nzp_serv = new int[] {2, 266, 15, 6, 7, 8, 9, 16, 5, 26, 206, 515, 251, 274, 17};
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

                // входящее сальдо

                sql = " update " + table_name_for_one_bank +
                      " set sum_insaldo = ( select sum(a.sum_insaldo) from " + pref +
                      "_charge_" + (Year - 2000) + DBManager.tableDelimiter +
                      " charge_" + Month.ToString().PadLeft(2, '0') + " a  where a.nzp_kvar = " + table_name_for_one_bank +
                      ".nzp_kvar" +
                      " and a.num_ls = " + table_name_for_one_bank + ".num_ls " + DBManager.sConvToInt + " and nzp_serv <> 1)";

                ExecSQL(sql);


                // Оплата

                sql = " update " + table_name_for_one_bank +
                      " set oplata = ( select sum(a.sum_money ) from " + pref + "_charge_" +
                      (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                      " charge_" + Month.ToString("00") + " a  where a.nzp_kvar = " + table_name_for_one_bank +
                      ".nzp_kvar and a.num_ls = " + table_name_for_one_bank + ".num_ls" + DBManager.sConvToInt + ")";

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
                        "select num_ls, ulica, ndom as dom, nzp_geu as uch, ikvar as kv, " + DBManager.sNvlWord + "(kol_ls,'1') as kol_ls," +
                        " replace(" + DBManager.sNvlWord + "(is_priv,''),'1','+') as is_priv," +
                        " fio, kolgil, kolvr, kolvibit, " +
                        " pl_ob, pl_gil,  kolkom, sum_insaldo as in_saldo," +
                        " sod_i_rem_g as p1, z_tbo as p2," +
                        " naim as p3, hv as p4, kanal as p5, otop as p6, " +
                        " gv as p7, v_tbo as p8, lift as p9, " +
                        " domofon as p10, odn_el as p11, kap_r as p12," +
                        " kap_r_po_fz as p21, vznos_na_kap_r as p22, lest_kl as p23, " + 
                        " real_ch as p13," +
                        " (sum_insaldo + sod_i_rem_g + z_tbo + naim + hv + kanal + otop + gv + v_tbo + lift + domofon + kap_r + odn_el + kap_r_po_fz + " +
                        " vznos_na_kap_r + lest_kl ) as p14, oplata " +
                        "from " + table_name +
                        " order by ulica,idom, ndom, ikvar";
                    break;
                case 2:
                    sql =
                        "select ulica,  ndom as dom, idom, nzp_geu as uch, sum(pl_ob) as obS," +
                        " sum(sum_insaldo) as in_saldo, sum(sod_i_rem_g) as p1," +
                        " sum(z_tbo) as p2, sum(naim) as p3, sum(hv) as p4, " +
                        "  sum(kanal) as p5, sum(otop) as p6, sum(gv) as p7," +
                        " sum(v_tbo) as p8, sum(lift) as p9, sum(domofon) as p10, " +
                        "  sum(odn_el) as p11, sum(kap_r) as p12, sum(kap_r_po_fz) as p21," +
                        " sum(vznos_na_kap_r) as p22,sum(lest_kl) as p23, sum(real_ch) as p13, " +
                        " sum((sum_insaldo + sod_i_rem_g + z_tbo + naim +" +
                        " hv + kanal + otop + gv + v_tbo + lift  + domofon + kap_r + odn_el + kap_r_po_fz +" +
                        " vznos_na_kap_r + lest_kl )) as p14, sum(oplata) as oplata " +
                        " from " + table_name + " group by nzp_dom, ulica, idom, ndom, nzp_geu " +
                        " order by ulica, idom, ndom";
                    break;
                case 3:
                    sql =
                        "select nzp_geu as uch, sum(sum_insaldo) as in_saldo," +
                        " sum(sod_i_rem_g) as p1, sum(z_tbo) as p2," +
                        " sum(naim) as p3, sum(hv) as p4, sum(kanal) as p5, " +
                        " sum(otop) as p6, sum(gv) as p7, sum(v_tbo) as p8," +
                        " sum(lift) as p9, sum(domofon) as p10, sum(odn_el) as p11, " +
                        " sum(kap_r) as p12, sum(kap_r_po_fz) as p21,sum(vznos_na_kap_r) as p22," +
                        " sum(lest_kl) as p23,  sum(real_ch) as p13, " +
                        " sum((sum_insaldo + sod_i_rem_g + z_tbo + naim + hv + kanal + otop + gv + v_tbo + lift +" +
                        " domofon + kap_r + odn_el + kap_r_po_fz + vznos_na_kap_r + lest_kl )) as p14, sum( oplata ) as oplata  " +
                        " from " + table_name + " group by nzp_geu " +
                        " order by nzp_geu";
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
                          " kol_ls " + DBManager.sDecimalType + "(14,2), " +
                          " is_priv CHAR(20), " +
                          " kolgil " + DBManager.sDecimalType + "(14,0), " +
                          " kolvr " + DBManager.sDecimalType + "(14,0), " +
                          " kolvibit " + DBManager.sDecimalType + "(14,0), " +
                          " pl_ob " + DBManager.sDecimalType + "(14,2), " +
                          " pl_gil " + DBManager.sDecimalType + "(14,2), " +
                          " kolkom " + DBManager.sDecimalType + "(14,0), " +
                          " sod_i_rem_g " + DBManager.sDecimalType + "(14,2), " +
                          " z_tbo " + DBManager.sDecimalType + "(14,2), " +
                          " naim " + DBManager.sDecimalType + "(14,2), " +
                          " hv " + DBManager.sDecimalType + "(14,2), " +
                          " kanal " + DBManager.sDecimalType + "(14,2), " +
                          " otop " + DBManager.sDecimalType + "(14,2), " +
                          " gv " + DBManager.sDecimalType + "(14,2), " +
                          " v_tbo " + DBManager.sDecimalType + "(14,2), " +
                          " lift " + DBManager.sDecimalType + "(14,2), " +
                          " domofon " + DBManager.sDecimalType + "(14,2), " +
                          " kap_r " + DBManager.sDecimalType + "(14,2), " +
                          " odn_el " + DBManager.sDecimalType + "(14,2), " +
                          " kap_r_po_fz " + DBManager.sDecimalType + "(14,2), " +
                          " vznos_na_kap_r " + DBManager.sDecimalType + "(14,2), " +
                          " lest_kl " + DBManager.sDecimalType + "(14,2), " +
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

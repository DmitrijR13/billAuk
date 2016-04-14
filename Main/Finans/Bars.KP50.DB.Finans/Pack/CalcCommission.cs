using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using System.Data;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    class ClassCalcCommission : DataBaseHead
    {
        private IDbConnection _connDb;
        private IDataReader reader = null;

        private DateTime _operDate;
        private string sOperDate;

        private string _temp_table_distrib_dom = "";

        private string fn_percent = "";

        private string fn_perc = "";
        private string fn_perc_short = "";
        private string parent_fn_perc = "";
        
        private string fn_naud = "";
        private string fn_naud_short = "";
        private string parent_fn_naud = "";
        private string _limit = "";
        private string _whereDom = "";

        public ClassCalcCommission(IDbConnection conn_db, DateTime dat_oper, bool filterByDom)
        {
            _connDb = conn_db;
            _operDate = dat_oper;
            
            sOperDate = Utils.EStrNull(_operDate.ToShortDateString()) + sConvToDate;

            fn_percent = Points.Pref + "_data" + tableDelimiter + "fn_percent_dom";

            parent_fn_perc = GetFinYearPref(dat_oper.Year) + "fn_perc_dom";
            fn_perc = parent_fn_perc + GetMonthSuffix(dat_oper.Month);
            fn_perc_short = "fn_perc_dom" + GetMonthSuffix(dat_oper.Month);

            parent_fn_naud = GetFinYearPref(dat_oper.Year) + "fn_naud_dom";
            fn_naud = parent_fn_naud + GetMonthSuffix(dat_oper.Month);
            fn_naud_short = "fn_naud_dom" + GetMonthSuffix(dat_oper.Month);

#if PG
            _limit = " limit 1 ";    
#endif
            _whereDom = "";
            if (filterByDom)
            {
                _whereDom = " and exists (select 1 from {table} b where a.nzp_dom = b.nzp_dom " + _limit + ")";    
            }
        }

        private string GetDaySuffix(int day)
        {
            return "_" + day.ToString("00");
        }

        private string GetMonthSuffix(int month)
        {
            return "_" + month.ToString("00");
        }

        private string GetFinYearPref(int year)
        {
            return Points.Pref + "_fin_" + (year % 100).ToString("00") + DBManager.tableDelimiter;
        }

        /// <summary>
        /// Расчитать комиссию
        /// </summary>
        /// <param name="ptype"></param>
        /// <param name="ret"></param>
        public Returns CalcCommission(string tmp_distrib_dom)
        {
            _temp_table_distrib_dom = tmp_distrib_dom;
            _whereDom = _whereDom.Replace("{table}", _temp_table_distrib_dom);

            WriteLog(".....Расчёт комиссии за обслуживание за " + _operDate.ToShortDateString());

            try
            {
                CreateTempTablePercUd();

                ApplySimpleRules();

                ApplyDependentRules();

                SaveCommision();

                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
        }

        /// <summary>
        /// Заполнение вспомогательных таблиц для расчёта комиссии
        /// </summary>
        private void CreateTempTablePercUd()
        {
            //выбрать nzp_payer_naud,perc_ud из fn_percent, который удерживает проценты из суммы в _tempTableTttPaXX: [nzp_payer, nzp_area, nzp_serv, nzp_bank, nzp_supp]
            DropTempTablePercUd();

            string sql = " Create temp table ttt_perc_ud (" +
                " nzp_key   serial  not null, " +
                " nzp_payer integer default 0 not null, " +
                //" nzp_area  integer default 0 not null, " +
                " nzp_dom   integer default 0 not null, " +
                " nzp_serv  integer default 0 not null, " +
                //" nzp_serv_naud  integer default 0 not null, " +
                " nzp_bank  integer default 0 not null, " +
                " nzp_supp  integer default 0 not null, " +
                " nzp_payer_naud  integer default 0 not null, " +
                " sum_prih        " + DBManager.sDecimalType + "(14,2) default 0.00 not null, " +
                " sum_naud        " + DBManager.sDecimalType + "(14,2) default 0.00 not null, " +
                " perc_ud         " + DBManager.sDecimalType + "(5,2)  default 0.00 not null " +
                " )  " + DBManager.sUnlogTempTable;
            ExecSQLWE(_connDb, sql, true);

            sql = " Insert into ttt_perc_ud ( nzp_payer,  nzp_dom,nzp_serv, nzp_bank, nzp_supp, sum_prih, nzp_payer_naud, sum_naud, perc_ud ) " +
                " Select a.nzp_payer,  a.nzp_dom, a.nzp_serv, a.nzp_bank, a.nzp_supp, a.sum_rasp, " +
                    " b.nzp_payer as nzp_payer_naud, a.sum_rasp as sum_naud, max(b.perc_ud) as perc_ud " +
                " From " + _temp_table_distrib_dom + " a, " +
                    fn_percent + " b ," + 
                    Points.Pref + "_kernel" + tableDelimiter + "s_bank c " +
                " Where b.nzp_supp in (-1,a.nzp_supp) " +
                "   and b.nzp_serv_from in (-1,a.nzp_serv) " +
                //"   and b.nzp_area in (-1,a.nzp_area) " +
                "   and c.nzp_payer in (-1,a.nzp_bank) " +
                "   and b.nzp_bank in  (-1,c.nzp_bank)  " +
                "   and b.nzp_dom in  (-1,a.nzp_dom)   " +
                "   and " + sOperDate + " between b.dat_s  and b.dat_po " +
                //"   and b.dat_s  <= " + sDat_oper + sConvToDate +
                //"   and b.dat_po >= " + sDat_oper + sConvToDate +
                "   and perc_ud > 0 " + /*"   and perc_ud > 0.001 " +*/ // см. примеры в ApplySimpleRules
                "   and b.nzp_serv <= 0 and b.nzp_supp_snyat <= 0 " +
                " Group by 1,2,3,4,5,6,7,8 ";

            ExecSQLWE(_connDb, sql, true);

            ExecSQLWE(_connDb, " Create index ix0_ttt_pud on ttt_perc_ud (nzp_key) ", true);

            ExecSQLWE(_connDb, DBManager.sUpdStat + " ttt_perc_ud ", true);

            WriteLog("..........Заполнение вспомогательных таблиц для расчёта комиссии завершено");       
        }

        /// <summary>
        /// Применение простых правил удержания
        /// </summary>
        /// <param name="_connDb"></param>
        private void ApplySimpleRules()
        {
            WriteLog("..........Расчёт простых правил удержания начат");

            //расчет суммы удержания
            string sql = " Update ttt_perc_ud Set sum_naud = sum_prih * perc_ud / 100.0 ";
            ExecSQLWE(_connDb, sql, true);
            
            // ОЧЕНЬ ЧАСТНЫЙ СЛУЧАЙ
            // Выравнивание копеек под единственный частный случай: сумма процентов по домам равна 100%
            ExecSQLWE(_connDb, " Drop table ttt_perc_ud_2 ", false);
            
            sql = " Create temp table ttt_perc_ud_2 " +
                " ( nzp_payer integer default 0 not null, " +
                "   nzp_supp  integer default 0 not null, " +
                "   nzp_serv  integer default 0 not null, " +
                "   nzp_bank  integer default 0 not null, " +
                "   sum_prih     " + DBManager.sDecimalType + "(14,2) default 0.00 not null, " +
                "   sum_naud     " + DBManager.sDecimalType + "(14,2) default 0.00 not null, " +
                "   sum_perc_ud  " + DBManager.sDecimalType + "(14,2) default 0.00 not null  " +
                " )  " + DBManager.sUnlogTempTable;
            ExecSQLWE(_connDb, sql, true);
                        
            // определить максимальную распределенную сумму по дому, чтобы вычислить дельту, которая будет затем сторнивать сумму удержаний
            // вычислить сумму сумм удержаний и сумму процентов
            /*
            ... для выборки использовалось условие abs(sum_naud) > 0.001
            ... т.к. sum_naud имеет тип decimal(14,2), то количество знаков после запятой будет 2
            ... и под условие abs(sum_naud) > 0.001 должны попадать ненулевые суммы:
            ... abs(0.01) > 0.001 = true, abs(-0.1) > 0.001 = true, abs(0.00) > 0.001 = false
            */
            sql = " insert into ttt_perc_ud_2 (nzp_payer, nzp_supp, nzp_serv, nzp_bank, sum_prih, sum_naud, sum_perc_ud) " +
                " Select nzp_payer, nzp_supp, nzp_serv, nzp_bank, max(sum_prih), sum(sum_naud), max(perc_ud) " +
                " From ttt_perc_ud " +
                //" Where abs(sum_naud) > 0.001 " +
                " Where sum_naud <> 0 " +
                " Group by 1,2,3,4 ";
            ExecSQLWE(_connDb, sql, true);

            ExecSQL(_connDb, " Drop table ttt_perc_ud_sum_dlt ", false);
            sql = " Create temp table ttt_perc_ud_sum_dlt " +
                " ( nzp_payer integer default 0 not null, " +
                "   nzp_supp  integer default 0 not null, " +
                "   nzp_serv  integer default 0 not null, " +
                "   nzp_bank  integer default 0 not null, " +
                "   nzp_key   integer default 0 not null, " +
                "   sum_naud  " + DBManager.sDecimalType + "(14,2) default 0.00 not null, " +
                "   sum_dlt   " + DBManager.sDecimalType + "(14,2) default 0.00 not null " +
                " )  " + DBManager.sUnlogTempTable;
            ExecSQLWE(_connDb, sql, true);

            // опеределить записи со 100% удержанием и ненулевой дельтой + вычислить дельту
            /*
            ... для выборки использовалось условие abs(100 - sum_perc_ud) < 0.001 and abs(sum_prih - sum_naud) > 0.0001
            ... т.к. perc_ud имеет тип decimal(14,2), то sum_perc_ud также имеет тип decimal(14,2), и количество знаков после запятой у sum_perc_ud будет 2
            ... и под условие abs(100 - sum_perc_ud) < 0.001 должны попадать строки, у которых sum_perc_ud = 100:
            ... abs(100 - 99.99) < 0.001 = false, abs(100 - 100) < 0.001 = true
            
            ... sum_prih и sum_naud также имеют тип decimal(14,2) и под условие abs(sum_prih - sum_naud) > 0.0001 не попадут только те строки, у которых
            ... sum_prih - sum_naud = 0: abs(10 - 10) > 0.0001 = false, abs(10 - 9) > 0.0001 = true
            ... поэтому условие abs(sum_prih - sum_naud) > 0.0001 аналогично условию sum_prih - sum_naud <> 0 или sum_prih <> sum_naud
            */

            sql = " insert into ttt_perc_ud_sum_dlt (nzp_payer, nzp_supp, nzp_serv, nzp_bank, sum_dlt) " +
                " select nzp_payer, nzp_supp, nzp_serv, nzp_bank, sum_prih - sum_naud " +
                " From ttt_perc_ud_2 " +
                //" Where abs(100 - sum_perc_ud) < 0.001 and abs(sum_prih - sum_naud) > 0.0001"; 
                " Where sum_perc_ud = 100 " + 
                "   and sum_prih <> sum_naud"; // ненулевая дельта
            ExecSQLWE(_connDb, sql, true);

            // ... определить cуммы которые нужно сторнировать
            sql = " update ttt_perc_ud_sum_dlt b set " +
                " sum_naud = (select max(a.sum_naud) From ttt_perc_ud a " +
                " Where a.nzp_payer = b.nzp_payer " +
                "   and a.nzp_supp = b.nzp_supp " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and a.nzp_bank = b.nzp_bank)";
            ExecSQLWE(_connDb, sql, true);

            // ... определить ключи записей, у которых нужно сторнировать суммы
            sql = " update ttt_perc_ud_sum_dlt b set " +
                " nzp_key = (select min(a.nzp_key) From ttt_perc_ud a " +
                " Where a.nzp_payer = b.nzp_payer " +
                "   and a.nzp_supp  = b.nzp_supp " +
                "   and a.nzp_serv  = b.nzp_serv " +
                "   and a.nzp_bank  = b.nzp_bank " +
                "   and a.sum_naud  = b.sum_naud)";
            ExecSQLWE(_connDb, sql, true);

            //сторнировать дельту
            sql = " Update ttt_perc_ud a Set " +
                " sum_naud = sum_naud + (select sum(sum_dlt) from ttt_perc_ud_sum_dlt b where a.nzp_key = b.nzp_key) " +
                " Where exists (select 1 from ttt_perc_ud_sum_dlt c where c.nzp_key = a.nzp_key " + _limit + ")";
            ExecSQLWE(_connDb, sql, true);

            ExecSQLWE(_connDb, " Drop table ttt_perc_ud_2 ", false);
            ExecSQLWE(_connDb, " Drop table ttt_perc_ud_sum_dlt ", false);
            
            WriteLog("..........Расчёт простых правил удержания завершен");    
        }

        /// <summary>
        /// Пропорционально распеределить сумму удержания по домам 
        /// </summary>
        private void DistributeSumUdBetweenHouse(int nzp_supp_snyat, int nzp_serv)
        {
            string sql = " drop table t_ud_from_uk3 ";
            ExecSQLWE(_connDb, sql, false);

            sql = "create temp table t_ud_from_uk3 (" + 
                " id serial not null, " + 
                " nzp_supp integer, " + 
                " nzp_payer integer, " + 
                " nzp_dom integer, " + 
                " nzp_serv integer, " +
                " nzp_bank integer, " +
                " sum_ud_itogo     " + DBManager.sDecimalType + "(20,2) default 0, " + // распределяемая сумма удержания
                " sum_prih         " + DBManager.sDecimalType + "(20,2) default 0, " + // сумма, которая задает пропорцию
                " sum_prih_itogo   " + DBManager.sDecimalType + "(20,2) default 0, " + // сумма сумм, которые задают пропорции
                " sum_ud       " + DBManager.sDecimalType + "(20,2) default 0) " + // сумма удержания для дома, вычисленная как sum_ud = sum_ud_itogo * sum_prih / sum_prih_itogo 
                DBManager.sUnlogTempTable;
            ExecSQLWE(_connDb, sql, true);

            sql = " insert into t_ud_from_uk3 (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, sum_prih) " +
                " Select d.nzp_supp, d.nzp_payer, d.nzp_dom, d.nzp_serv, d.nzp_bank, sum(d.sum_rasp) sum_prih " +
                " From  " + _temp_table_distrib_dom + " d " + 
                " Where d.nzp_supp = " + nzp_supp_snyat;

            if (nzp_serv > 0)
            {
                sql += "   and nzp_serv = " + nzp_serv;
            }

            sql += " group by 1,2,3,4,5 having sum(sum_rasp) > 0 ";
            ExecSQLWE(_connDb, sql, true);

            // ... получить итоговые суммы
            Returns ret = new Returns(true);
            decimal sum_ud_itogo = 0;

            Decimal.TryParse(ExecScalar(_connDb, "select sum(sum_ud) from t_ud_from_uk2", out ret, true).ToString(), out sum_ud_itogo);
            decimal sum_prih_itogo = 0;
            Decimal.TryParse(ExecScalar(_connDb, "select sum(sum_prih) from t_ud_from_uk3", out ret, true).ToString(), out sum_prih_itogo);

            // ... проставить итоговые суммы
            sql = "update t_ud_from_uk3 set sum_prih_itogo = " + sum_prih_itogo.ToString().Replace(",", ".") + ", sum_ud_itogo = " + sum_ud_itogo.ToString().Replace(",", ".") + "; ";
            ExecSQLWE(_connDb, sql, true);

            // ... скорректировать распределяемые суммы
            sql = "update t_ud_from_uk3 set sum_ud_itogo =  case when sum_ud_itogo > sum_prih_itogo then sum_prih_itogo else sum_ud_itogo end;";
            ExecSQLWE(_connDb, sql, true);

            sql = "delete from t_ud_from_uk3 where sum_prih_itogo = 0;";
            ExecSQLWE(_connDb, sql, true);

            // ... вычислить суммы удержания по домам
            sql = "update t_ud_from_uk3 set sum_ud = sum_ud_itogo * sum_prih / sum_prih_itogo;";
            ExecSQLWE(_connDb, sql, true);

            // скорретировать сумму удержаний по домам
            // ... получить сумму удержаний по домам
            decimal dom_sum_ud_itogo = 0;
            Decimal.TryParse(ExecScalar(_connDb, "select sum(sum_ud) from t_ud_from_uk3", out ret, true).ToString(), out dom_sum_ud_itogo);

            if (sum_ud_itogo != dom_sum_ud_itogo && sum_ud_itogo != 0)
            {
                decimal max_sum_ud = 0;
                Decimal.TryParse(ExecScalar(_connDb, "select max(sum_ud) from t_ud_from_uk3", out ret, true).ToString(), out max_sum_ud);
                int id = 0;
                Int32.TryParse(ExecScalar(_connDb, "select min(id) from t_ud_from_uk3 where sum_ud = " + max_sum_ud, out ret, true).ToString(), out id);

                sql = "update t_ud_from_uk3 set sum_ud = sum_ud - " + (dom_sum_ud_itogo - sum_ud_itogo) + " where id = " + id;
                ExecSQLWE(_connDb, sql, true);
            }
        }

        /// <summary>
        /// Применение зависимых правил удержания
        /// </summary>
        private void ApplyDependentRules()
        {
            WriteLog("..........Расчёт зависимых правил удержания начат");
            
            string sql = "select p.nzp_payer as nzp_payer_to, p.nzp_serv_from, p.nzp_bank, p.nzp_serv, p.nzp_supp, p.nzp_supp_snyat, p.perc_ud, " +
                    "   s.nzp_payer_princip as nzp_payer " +
                    " from  " + fn_percent + " p, " +
                    Points.Pref + "_kernel" + DBManager.tableDelimiter + "supplier s " +
                    " where s.nzp_supp = p.nzp_supp " +
                    "   and (p.nzp_serv > 0 or p.nzp_supp_snyat > 0) " +
                    "   and " + sOperDate + " between p.dat_s and p.dat_po";
            ExecRead(_connDb, out reader, sql, true);

            while (reader.Read())
            {
                int nzp_payer_to = (int)reader["nzp_payer_to"];
                //int nzp_area = (int)reader["nzp_area"];
                int nzp_serv_from = (int)reader["nzp_serv_from"];
                int nzp_bank = (int)reader["nzp_bank"];
                int nzp_serv = (int)reader["nzp_serv"];
                int nzp_supp = (int)reader["nzp_supp"];

                //int nzp_serv_snyat = 0;
                //if (reader["nzp_serv_snyat"]!= DBNull.Value) nzp_serv_snyat = (int)reader["nzp_serv_snyat"];
                int nzp_supp_snyat = 0;
                if (reader["nzp_supp_snyat"] != DBNull.Value) nzp_supp_snyat = (int)reader["nzp_supp_snyat"];

                int nzp_payer_from = (int)reader["nzp_payer"]; // принципал
                int nzp_supp_from = (int)reader["nzp_supp"];

                decimal perc_ud = (decimal)reader["perc_ud"];

                if ((nzp_payer_from > 0) & (nzp_supp > 0))
                {
                    sql = " drop table t_ud_from_uk2 ";
                    ExecSQLWE(_connDb, sql, false);

                    sql = "create temp table t_ud_from_uk2 (" + 
                        " nzp_supp integer, " + 
                        " nzp_payer integer, " + 
                        " nzp_dom integer, " + 
                        " nzp_serv integer, " +
                        " nzp_bank integer, " +
                        " sum_prih " + DBManager.sDecimalType + "(20,2) default 0, " + 
                        " sum_ud " + DBManager.sDecimalType + "(20,2) default 0) " +
                        DBManager.sUnlogTempTable;
                    ExecSQLWE(_connDb, sql, true);

                    sql = " insert into t_ud_from_uk2 (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank,  sum_prih) " +
                        " Select d.nzp_supp, d.nzp_payer, d.nzp_dom, d.nzp_serv, d.nzp_bank, sum(d.sum_rasp) sum_prih " +
                        " From  " + _temp_table_distrib_dom + " d, " +
                        Points.Pref + "_kernel" + tableDelimiter + "supplier s " +
                        " Where d.nzp_supp = s.nzp_supp " +
                        "   and s.nzp_payer_princip = " + nzp_payer_from;

                    if (nzp_serv_from > 0)
                    {
                        sql += " and d.nzp_serv = " + nzp_serv_from;
                    }

                    if (nzp_bank > 0)
                    {
                        sql += " and d.nzp_bank = " + nzp_bank;
                    }
                    //nzp_dom
                    sql += " group by 1,2,3,4,5 ";
                    ExecSQLWE(_connDb, sql, true);

                    sql = " update  t_ud_from_uk2 set sum_ud = sum_prih * (" + perc_ud.ToString().Replace(",", ".") + "/100.0) ";
                    ExecSQLWE(_connDb, sql, true);

                    if (nzp_supp_snyat > 0)
                    {
                        // распределить сумму удержания по домам
                        DistributeSumUdBetweenHouse(nzp_supp_snyat, nzp_serv);

                        // Удержанные суммы
                        sql = " Insert into ttt_perc_ud (nzp_payer, nzp_payer_naud, nzp_supp, nzp_serv, nzp_dom, nzp_bank, perc_ud, sum_prih, sum_naud) " +
                            " Select " + nzp_payer_from + "," + nzp_payer_to + ", nzp_supp, nzp_serv, " +
                             " nzp_dom, nzp_bank, " + perc_ud.ToString().Replace(",", ".") + ", SUM(sum_prih),  SUM(sum_ud) " + 
                             " From t_ud_from_uk3 " + 
                             " group by 1,2,3,4,5,6,7 having  SUM(sum_ud)>0";
                        ExecSQLWE(_connDb, sql, true);

                        ExecSQLWE(_connDb, "drop table t_ud_from_uk3", true);
                    }
                    else
                    {
                        // Удержанные суммы
                        sql = " Insert into ttt_perc_ud  (nzp_payer, nzp_payer_naud, nzp_supp, nzp_serv, nzp_dom, nzp_bank, perc_ud, sum_prih, sum_naud) " +
                            " Select " + nzp_payer_from + "," + nzp_payer_to + "," + nzp_supp_from + "," + nzp_serv + "," + "nzp_dom,nzp_bank, " + perc_ud.ToString().Replace(",", ".") + ", SUM(sum_prih)" + ", SUM(sum_ud) " +
                            " From t_ud_from_uk2" +
                            "  group by 1,2,3,4,5,6,7 having  SUM(sum_ud)>0";
                        ExecSQLWE(_connDb, sql, true);
                    }

                    sql = " drop table t_ud_from_uk2 ";
                    ExecSQLWE(_connDb, sql, false);
                }
            }
            reader.Close();
            reader.Dispose();

            WriteLog("..........Расчёт зависимых правил удержания завершен");
        }

        /// <summary>
        /// Сохранение комиссии
        /// </summary>
        private void SaveCommision()
        {
#if PG
            ExecSQL(_connDb, "set search_path to '" + Points.Pref + "_fin_" + (_operDate.Year % 100).ToString("00") + "'", false);
#else
            ExecSQL(_connDb, "database " + Points.Pref + "_fin_" + (_datOper.Year % 100).ToString("00"), false);
#endif

            WriteLog("..........Сохранение комиссии начато");

            ExecSQLWE(_connDb, "create index IX_ttt_perc_ud_sum_naud on ttt_perc_ud(sum_naud)", true);
            ExecSQLWE(_connDb, DBManager.sUpdStat + " ttt_perc_ud", true);

            //сохранить суммы в fn_perc и fn_naud
            // ... удалить
            Returns ret = SaveFnPerc();
            if (!ret.result) throw new Exception(ret.text);

            ret = SaveFnNaud();
            if (!ret.result) throw new Exception(ret.text);

            using (ClassUpdateFnDistribDomXX updDistrib = new ClassUpdateFnDistribDomXX(_connDb, _operDate))
            {
                ret = updDistrib.UpdateSumUdAndSumNaud("ttt_perc_ud");
                if (!ret.result) throw new Exception(ret.text);
            }

            DropTempTablePercUd();
            
            WriteLog("..........Сохранение комиссии завершено");
        }

        private void WriteLog(string log)
        {
            MonitorLog.WriteLog(log, MonitorLog.typelog.Info, 1, 222, true);
        }

        /// <summary>
        /// Сохранить данные в fn_naud
        /// </summary>
        /// <returns></returns>
        private Returns SaveFnNaud()
        { 
            bool createFnNaud = !TempTableInWebCashe(_connDb, fn_naud);

            IDbTransaction transactionID = _connDb.BeginTransaction();
            try
            {
                string sql = "";
                
#if PG
                if (createFnNaud) 
                {
                    DateTime dDateFrom = new DateTime(_operDate.Year, _operDate.Month, 1);
                    string dateFrom = Utils.EStrNull(dDateFrom.Year + "-" + dDateFrom.Month.ToString("00") + "-" + dDateFrom.Day.ToString("00"));

                    DateTime dDateTo = new DateTime(_operDate.Year, _operDate.Month, 1).AddMonths(1).AddDays(-1);
                    string dateTo = Utils.EStrNull(dDateTo.Year + "-" + dDateTo.Month.ToString("00") + "-" + dDateTo.Day.ToString("00"));

                    sql = " CREATE TABLE " + fn_naud + " (CONSTRAINT CNSTR_" + fn_naud_short + "_dat_oper CHECK (dat_oper >= " + dateFrom + " and dat_oper <= " + dateTo + ")) " +
                        " INHERITS (" + parent_fn_naud + ") WITHOUT OIDS";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "ALTER TABLE " + fn_naud + " ADD CONSTRAINT PK_" + fn_naud_short + " PRIMARY KEY (nzp_naud)";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "create UNIQUE index IX_" + fn_naud_short + "_nzp_naud on " + fn_naud + " (nzp_naud)";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "create index IX_" + fn_naud_short + "_dat_oper on " + fn_naud + " (dat_oper)";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "create index IX_" + fn_naud_short + "_nzp_dom on " + fn_naud + " (nzp_dom)";
                    ExecSQLWE(_connDb, transactionID, sql, true);
                }
                ExecSQLWE(_connDb, DBManager.sUpdStat + " " + fn_naud, true);
                sql = " delete from " + fn_naud + " a where a.dat_oper = " + sOperDate + _whereDom;
                ExecSQLWE(_connDb, transactionID, sql, true);

                //fn_naud: удержанные суммы
                sql = " Insert into " + fn_naud + " (dat_oper, nzp_payer, nzp_dom, nzp_serv, nzp_payer_2, sum_prih, perc_ud, sum_ud, sum_naud, nzp_supp, nzp_geu, nzp_bank) " +
                    " Select " + sOperDate + ", nzp_payer, nzp_dom, nzp_serv, nzp_payer_naud, sum_prih, perc_ud, sum_naud, 0 as sum_naud, nzp_supp, 0 nzp_geu, nzp_bank " +
                    " From ttt_perc_ud " +
                    " Where sum_naud <> 0 " /*" Where abs(sum_naud) > 0.001 "*/; // см. примеры в ApplySimpleRules
                ExecSQLWE(_connDb, transactionID, sql, true);

                //fn_naud: начисленные суммы
                sql = " Insert into " + fn_naud + " (dat_oper, nzp_payer, nzp_dom, nzp_serv, nzp_payer_2, sum_prih, perc_ud, sum_ud, sum_naud, nzp_supp, nzp_geu, nzp_bank) " +
                    " Select " + sOperDate + ", nzp_payer_naud, nzp_dom, nzp_serv, nzp_payer,sum_prih, perc_ud, 0 sum_naud, sum_naud, nzp_supp, 0 nzp_geu, nzp_bank " +
                    " From ttt_perc_ud " +
                    " Where sum_naud <> 0 " /*" Where abs(sum_naud) > 0.001 " */; // см. примеры в ApplySimpleRules
                ExecSQLWE(_connDb, transactionID, sql, true);
                
#endif
                transactionID.Commit();

                return new Returns(true);
            }
            catch (Exception ex)
            {
                transactionID.Rollback();
                return new Returns(false, ex.Message);
            }
            finally
            {
                transactionID.Dispose();
            }
        }

        /// <summary>
        /// Сохранить fn_perc
        /// </summary>
        /// <returns></returns>
        private Returns SaveFnPerc()
        {
            bool createFnPerc = !TempTableInWebCashe(_connDb, fn_perc);

            IDbTransaction transactionID = _connDb.BeginTransaction();
            try
            {
                string sql = "";

#if PG
                if (createFnPerc)
                {
                    DateTime dDateFrom = new DateTime(_operDate.Year, _operDate.Month, 1);
                    string dateFrom = Utils.EStrNull(dDateFrom.Year + "-" + dDateFrom.Month.ToString("00") + "-" + dDateFrom.Day.ToString("00"));
                    
                    DateTime dDateTo = new DateTime(_operDate.Year, _operDate.Month, 1).AddMonths(1).AddDays(-1);
                    string dateTo = Utils.EStrNull(dDateTo.Year + "-" + dDateTo.Month.ToString("00") + "-" + dDateTo.Day.ToString("00"));

                    sql = " CREATE TABLE " + fn_perc + " (CONSTRAINT CNSTR_" + fn_perc_short + "_dat_oper CHECK (dat_oper >= " + dateFrom + " and dat_oper <= " + dateTo + ")) " +
                        " INHERITS (" + parent_fn_perc + ") WITHOUT OIDS";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "ALTER TABLE " + fn_perc + " ADD CONSTRAINT PK_" + fn_perc_short + " PRIMARY KEY (nzp_pr)";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "create UNIQUE index IX_" + fn_perc_short + "_nzp_pr on " + fn_perc + " (nzp_pr)";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "create index IX_" + fn_perc_short + "_dat_oper on " + fn_perc + " (dat_oper)";
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = "create index IX_" + fn_perc_short + "_nzp_dom on " + fn_perc + " (nzp_dom)";
                    ExecSQLWE(_connDb, transactionID, sql, true);
                }

                sql = " delete from " + fn_perc + " a where a.dat_oper = " + sOperDate + _whereDom;
                ExecSQLWE(_connDb, transactionID, sql, true);

                sql = " Insert into " + fn_perc + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_geu, sum_prih, sum_perc, perc_ud, dat_oper, nzp_bank) " +
                    " Select nzp_supp, nzp_payer_naud, nzp_dom,nzp_serv, -1 nzp_geu, sum_prih, sum_naud, perc_ud, " + sOperDate + ", nzp_bank " +
                    " From ttt_perc_ud " +
                    " Where sum_naud <> 0 " /*" Where abs(sum_naud) > 0.001 "*/;  // см. примеры в ApplySimpleRules
                ExecSQLWE(_connDb, transactionID, sql, true);

#endif
                transactionID.Commit();

                return new Returns(true);
            }
            catch (Exception ex)
            {
                transactionID.Rollback();
                return new Returns(false, ex.Message);
            }
            finally
            {
                transactionID.Dispose();
            }
        }

        private void DropTempTablePercUd()
        {
            ExecSQL(_connDb, " Drop table ttt_perc_ud ", false);
        }
    }
}

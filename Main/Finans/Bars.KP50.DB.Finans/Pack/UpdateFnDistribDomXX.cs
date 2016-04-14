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
    class ClassUpdateFnDistribDomXX: DataBaseHead
    {
        private IDbConnection _connDb;
        private const string _t_help_distrib = "t_help_distrib";
        private const string _temp_table_distrib_dom = "temp_table_distrib_dom";

        private DateTime _operDate;
        private string sOperDate = "";
        private string _limit = "";

        private string fn_distrib = "";
        private string fn_distrib_short = "";
        private string fn_distrib_prev = "";
        private string old_fn_distrib_prev = "";
        
        private string parent_fn_distrib = "";
        
        private string fn_sended = "";
        private string fn_pa_xx = "";
        private string fn_reval = "";

        // фильтрация домов по временной таблице _temp_table_dom
        private string _domFilterCondition = "";
        private string tagName = "{maintable}";
        private bool _filterByDom = false;

        public ClassUpdateFnDistribDomXX(IDbConnection conn_db, DateTime operDate) : this(conn_db, operDate, false, "") { }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="conn_db">Cоединение с базой</param>
        /// <param name="calc_month">расчетный месяц</param>
        /// <param name="calc_year">расчетный год</param>
        /// <param name="dateOper">операционный день</param>
        
        public ClassUpdateFnDistribDomXX(IDbConnection conn_db, DateTime operDate, bool filterByDom, string temp_dom_table)
        {
            _connDb = conn_db;
            _operDate = operDate;

#if PG
            _limit = " limit 1 ";
#endif
            _filterByDom = filterByDom;

            if (filterByDom)
            {
                _domFilterCondition = " and exists (select 1 from " + temp_dom_table + 
                    " where " + tagName + ".nzp_dom = " + temp_dom_table +".nzp_dom " + _limit + ")";
            }

            sOperDate = Utils.EStrNull(_operDate.ToShortDateString()) + DBManager.sConvToDate;

            // определить названия таблиц
            fn_distrib_short = "fn_distrib_dom" + GetMonthSuffix(operDate.Month) + GetDaySuffix(operDate.Day);
            fn_distrib = GetFinYearPref(operDate.Year) + fn_distrib_short;
            parent_fn_distrib = GetFinYearPref(operDate.Year) + "fn_distrib_dom" + GetMonthSuffix(operDate.Month);

            fn_sended = GetFinYearPref(operDate.Year) + "fn_sended_dom";
            fn_pa_xx = GetFinYearPref(operDate.Year) + "fn_pa_dom" + GetMonthSuffix(operDate.Month) + GetDaySuffix(operDate.Day);
            fn_reval = GetFinYearPref(operDate.Year) + "fn_reval_dom";

            //---------------------------------------------------------------------------
            DateTime prevOperDate = _operDate.AddDays(-1);
            fn_distrib_prev = GetFinYearPref(prevOperDate.Year) + "fn_distrib_dom" + GetMonthSuffix(prevOperDate.Month) + GetDaySuffix(prevOperDate.Day);
            old_fn_distrib_prev = GetFinYearPref(prevOperDate.Year) + "fn_distrib_dom" + GetMonthSuffix(prevOperDate.Month);

            //if (operDate.Day == 1)
            //{
            //    if (operDate.Month == 1)
            //    {
            //        fn_distrib_prev = GetFinYearPref(operDate.Year - 1) + "fn_distrib_dom" + GetMonthSuffix(12) + GetDaySuffix(1);
            //        old_fn_distrib_prev = GetFinYearPref(operDate.Year - 1) + "fn_distrib_dom" + GetMonthSuffix(12);
            //    }
            //    else
            //    {
            //        fn_distrib_prev = GetFinYearPref(operDate.Year) + "fn_distrib_dom" + GetMonthSuffix(operDate.Month - 1) + GetDaySuffix(1);
            //        old_fn_distrib_prev = GetFinYearPref(operDate.Year) + "fn_distrib_dom" + GetMonthSuffix(operDate.Month - 1);
            //    }
            //}
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
        /// Создать временную таблицу для информации по распределению
        /// </summary>
        public Returns CreateDistribDomTempTable()
        {
            try
            {
                ExecSQLWE(_connDb, " Drop table temp_table_distrib_dom", false);

                string sql = " Create temp table temp_table_distrib_dom (" +
                    // ключи 
                    " nzp_supp  integer, " +
                    " nzp_payer integer, " +
                    " nzp_dom   integer, " +
                    " nzp_serv  integer, " +
                    " nzp_bank  integer, " +
                    // суммы
                    " sum_in     " + sDecimalType + "(14,2) default 0.00 not null, " +
                    " sum_rasp   " + sDecimalType + "(14,2) default 0.00 not null, " +
                    " sum_ud     " + sDecimalType + "(14,2) default 0.00 not null, " +
                    " sum_naud   " + sDecimalType + "(14,2) default 0.00 not null, " +
                    " sum_reval  " + sDecimalType + "(14,2) default 0.00 not null, " +
                    " sum_send   " + sDecimalType + "(14,2) default 0.00 not null, " +
                    " sum_charge " + sDecimalType + "(14,2) default 0.00 not null, " +
                    " sum_out    " + sDecimalType + "(14,2) default 0.00 not null " +
                    ") " + sUnlogTempTable;

                ExecSQLWE(_connDb, sql, true);

                return new Returns(true, "temp_table_distrib_dom");
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Обновить входящее сальдо 
        /// </summary>
        /// <returns></returns>
        public Returns UpdateSumInSaldo()
        {
            try
            {
                PutSumInSaldoIntoTempTable(_temp_table_distrib_dom);
                CreateIndex(_temp_table_distrib_dom);
                
                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Сумма распределений
        /// </summary>
        /// <returns></returns>
        public Returns UpdateSumRasp()
        {
            try
            {
                GetDataIntoHelpDistrib("sum_rasp");
                InsertAbsentStringIntoDistribDom();
                UpdateDistribDomSumField("sum_rasp");
                DropHelpDistrib();
                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Перекидки
        /// </summary>
        /// <returns></returns>
        public Returns UpdateSumReval()
        {
            try
            {
                GetDataIntoHelpDistrib("sum_reval");
                InsertAbsentStringIntoDistribDom();
                UpdateDistribDomSumField("sum_reval");
                DropHelpDistrib();
                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Перечислено
        /// </summary>
        /// <returns></returns>
        public Returns UpdateSumSend()
        {
            try
            {
                GetDataIntoHelpDistrib("sum_send");
                InsertAbsentStringIntoDistribDom();
                UpdateDistribDomSumField("sum_send");
                DropHelpDistrib();
                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }
        
        /// <summary>
        /// Комиссия
        /// </summary>
        /// <param name="tempTable"></param>
        /// <returns></returns>
        public Returns UpdateSumUdAndSumNaud(string tempTable)
        {
            try
            {
                ExecSQLWE(_connDb, "Create index ix_" + tempTable + "_nzp_payer_naud on " + tempTable + " (nzp_payer_naud, nzp_supp, nzp_dom, nzp_serv, nzp_bank) ", true);
                ExecSQLWE(_connDb, DBManager.sUpdStat + " " + tempTable, true);
                
                GetDataIntoHelpDistrib("sum_naud", tempTable);
                InsertAbsentStringIntoDistribDom();
                UpdateDistribDomSumField("sum_naud");
                DropHelpDistrib();

                CreateIndex(tempTable);

                GetDataIntoHelpDistrib("sum_ud", tempTable);
                UpdateDistribDomSumField("sum_ud");
                DropHelpDistrib();

                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }
                
        /// <summary>
        /// Сохранение в базу
        /// </summary>
        /// <returns></returns>
        public Returns SaveDistribDom(int nzp_payer = 0)
        {
#if PG
            ExecSQL(_connDb, "set search_path to '" + Points.Pref + "_fin_" + (_operDate.Year % 100).ToString("00") + "'", false);
#else
            ExecSQL(_connDb, "database " + Points.Pref + "_fin_" + (_calcYear % 100).ToString("00"), false);
#endif
            bool tableExists = TempTableInWebCashe(_connDb, fn_distrib);
            
            using (IDbTransaction transactionID = _connDb.BeginTransaction())
            {
                try
                {
                    string sql = "";
                    if (!tableExists)
                    {
                        CreateInheritTable(transactionID);
                    }

                    sql = "DELETE FROM ONLY " + parent_fn_distrib + " where dat_oper = " + sOperDate;
                    ExecSQLWE(_connDb, transactionID, sql, true);

                    if (_filterByDom)
                    {
                        sql = "delete from " + fn_distrib + " t Where 1=1 " + _domFilterCondition.Replace(tagName, "t");
                    }
                    else if (nzp_payer > 0)
                    {
                        sql = " Delete from " + _temp_table_distrib_dom + " where nzp_payer = " + nzp_payer;    
                    }
                    else
                    {
                        sql = " truncate " + fn_distrib;
                    }

                    ExecSQLWE(_connDb, transactionID, sql, true);

                    sql = " insert into " + fn_distrib + "(nzp_payer, nzp_area, nzp_dom, nzp_serv, dat_oper, nzp_bank, nzp_supp, " +
                        " sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_send, sum_charge, sum_out) " +
                        " select nzp_payer, -1, nzp_dom, nzp_serv, " + sOperDate + ", nzp_bank, nzp_supp, " +
                        "   sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_send, " + 
                        "   (sum_rasp - sum_ud + sum_naud + sum_reval) as sum_charge, " +
                        "   (sum_in + sum_rasp - sum_ud + sum_naud + sum_reval - sum_send) as sum_out " +
                        " from " + _temp_table_distrib_dom;

                    ExecSQLWE(_connDb, transactionID, sql, true);

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
                    ExecSQL(_connDb, " Drop table " + _temp_table_distrib_dom, false);
                }
            }
        }

        /// <summary>
        /// Положить сумму по входящему сальдо во временную таблицу
        /// </summary>
        /// <param name="tempTablName"></param>
        private void PutSumInSaldoIntoTempTable(string tempTableName, int nzp_payer = 0)
        {
            DateTime d = _operDate.AddDays(-1);

            string tableName = fn_distrib_prev;
            bool getFromInheritTable = TempTableInWebCashe(_connDb, fn_distrib_prev);

            if (!getFromInheritTable)
            {
                tableName = old_fn_distrib_prev;
            }

            string sql = " insert Into " + tempTableName + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, sum_in) " +
                " Select a.nzp_supp, a.nzp_payer, a.nzp_dom, a.nzp_serv, a.nzp_bank, sum(a.sum_out) as sum_in " +
                " From " + tableName + " a " +
                " Where 1=1 " +
                (getFromInheritTable ? "" : " and a.dat_oper = " + Utils.EStrNull(d.ToShortDateString()) + sConvToDate) +
                (nzp_payer > 0 ? " and a.nzp_payer = " + nzp_payer : "") +
                // условие на дома
                _domFilterCondition.Replace(tagName, "a") +
                " Group by 1,2,3,4,5";
            ExecSQLWE(_connDb, sql, true);
        }

        /// <summary>
        /// Создание наследуемой таблицы
        /// </summary>
        /// <param name="transactionID"></param>
        private void CreateInheritTable(IDbTransaction transactionID)
        {
#if PG
            string dat_oper = Utils.EStrNull(_operDate.Year + "-" + _operDate.Month.ToString("00") + "-" + _operDate.Day.ToString("00"));
            string sql = " CREATE TABLE " + fn_distrib + " (CONSTRAINT CNSTR_" + fn_distrib_short + "_dat_oper CHECK (dat_oper = " + dat_oper + ")) " +
                " INHERITS (" + parent_fn_distrib + ") WITHOUT OIDS";
            ExecSQLWE(_connDb, transactionID, sql, true);

            sql = "ALTER TABLE " + fn_distrib + " ADD CONSTRAINT PK_" + fn_distrib_short + " PRIMARY KEY (nzp_dis)";
            ExecSQLWE(_connDb, transactionID, sql, true);

            sql = "create UNIQUE index IX_" + fn_distrib_short + "_nzp_dis on " + fn_distrib + " (nzp_dis)";
            ExecSQLWE(_connDb, transactionID, sql, true);

            sql = "create index IX_" + fn_distrib_short + "_dat_oper on " + fn_distrib + " (dat_oper)";
            ExecSQLWE(_connDb, transactionID, sql, true);

            sql = "create index IX_" + fn_distrib_short + "_nzp_payer on " + fn_distrib + " (nzp_payer)";
            ExecSQLWE(_connDb, transactionID, sql, true);

            sql = "create index IX_" + fn_distrib_short + "_nzp_dom on " + fn_distrib + " (nzp_dom)";
            ExecSQLWE(_connDb, transactionID, sql, true);
#endif
        }

        /// <summary>
        /// Подготовить временную таблицу для пересчета комиссии: поля sum_naud и sum_ud
        /// </summary>
        /// <returns></returns>
        public Returns PrepareTempTableForRecalcSumOutSaldo(int nzp_payer)
        {
            try
            {
                Returns ret = CreateDistribDomTempTable();
                if (!ret.result) throw new Exception(ret.text);

                // проверить, что таблица fn_distrib_dom_MM_DD существует
                bool fnDistribExists = TempTableInWebCashe(_connDb, fn_distrib);
                
                string sql = "insert into " + _temp_table_distrib_dom + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, sum_rasp, sum_ud, sum_naud, sum_reval, sum_send)" +
                    " Select nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, sum(sum_rasp), sum(sum_ud), sum(sum_naud), sum(sum_reval), sum(sum_send) " +
                    " From " + 
                    (fnDistribExists ? fn_distrib : parent_fn_distrib) + 
                    " Where 1=1 " + 
                    (!fnDistribExists ? " and dat_oper = " + sOperDate : "") +
                    (nzp_payer > 0 ? " and nzp_payer = " + nzp_payer : "") +
                    " Group by 1,2,3,4,5";

                ExecSQLWE(_connDb, sql, true);

                // создать индекс
                CreateIndex(_temp_table_distrib_dom);

                // создать вспомогательную таблицу _t_help_distrib
                CreateHelpDistribTempTable("sum_in");
                // положить в нее входящее сальдо
                PutSumInSaldoIntoTempTable(_t_help_distrib, nzp_payer);
                CreateIndex(_t_help_distrib);
                
                // обновить входящее сальдо
                sql = "update " + _temp_table_distrib_dom + " t set " +
                    " sum_in = (select sum(a.sum_in) From " + _t_help_distrib + " a " +
                        " Where t.nzp_supp  = a.nzp_supp " +
                        "   and t.nzp_payer = a.nzp_payer " +
                        "   and t.nzp_dom   = a.nzp_dom " +
                        "   and t.nzp_serv  = a.nzp_serv " +
                        "   and t.nzp_bank  = a.nzp_bank " +
                        ") " +
                    " Where exists (select 1 from " + _t_help_distrib + " a " +
                        " Where t.nzp_supp  = a.nzp_supp " +
                        "   and t.nzp_payer = a.nzp_payer " +
                        "   and t.nzp_dom   = a.nzp_dom " +
                        "   and t.nzp_serv  = a.nzp_serv " +
                        "   and t.nzp_bank  = a.nzp_bank " + _limit +
                        ") ";

                ExecSQLWE(_connDb, sql, true);

                DropHelpDistrib();

                return new Returns(true, _temp_table_distrib_dom);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Подготовить временную таблицу для пересчета комиссии: поля sum_naud и sum_ud
        /// </summary>
        /// <returns></returns>
        public Returns PrepareTempTableForRecalcCommission()
        {
            try
            {
                Returns ret = CreateDistribDomTempTable();
                if (!ret.result) throw new Exception(ret.text);

                // проверить, что таблица fn_distrib_dom_MM_DD существует
                bool fnDistribExists = TempTableInWebCashe(_connDb, fn_distrib);

                // положить данные
                string sql = "insert into " + _temp_table_distrib_dom + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, sum_in, sum_reval, sum_send)" +
                    " Select nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, sum(sum_in), sum(sum_reval), sum(sum_send) " +
                    " From " +
                    (fnDistribExists ? fn_distrib : parent_fn_distrib + " Where dat_oper = " + sOperDate) + 
                    " Group by 1,2,3,4,5";

                ExecSQLWE(_connDb, sql, true);

                // создать индекс
                CreateIndex(_temp_table_distrib_dom);
                
                return new Returns(true, _temp_table_distrib_dom);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Подготовить временную таблицу для пересчета перекидок: поле sum_reval
        /// </summary>
        /// <returns></returns>
        public Returns PrepareTempTableForRecalcReval()
        {
            try
            {
                Returns ret = CreateDistribDomTempTable();
                if (!ret.result) throw new Exception(ret.text);

                // проверить, что таблица fn_distrib_dom_MM_DD существует
                bool fnDistribExists = TempTableInWebCashe(_connDb, fn_distrib);

                // положить данные
                string sql = "insert into " + _temp_table_distrib_dom + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, " + 
                    "   sum_in, sum_rasp, sum_ud, sum_naud, sum_send) " +
                    " Select " +
                    " nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, " + 
                    "   sum(sum_in), sum(sum_rasp), sum(sum_ud), sum(sum_naud), sum(sum_send) " +
                    " From " + 
                    (fnDistribExists ? fn_distrib : parent_fn_distrib + " Where dat_oper = " + sOperDate) + 
                    " Group by 1,2,3,4,5";

                ExecSQLWE(_connDb, sql, true);

                // создать индекс
                CreateIndex(_temp_table_distrib_dom);

                return new Returns(true, _temp_table_distrib_dom);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Подготовить временную таблицу для пересчета сумм к перечислению: поле sum_send
        /// </summary>
        /// <returns></returns>
        public Returns PrepareTempTableForRecalcSumSend()
        {
            try
            {
                Returns ret = CreateDistribDomTempTable();
                if (!ret.result) throw new Exception(ret.text);

                // проверить, что таблица fn_distrib_dom_MM_DD существует
                bool fnDistribExists = TempTableInWebCashe(_connDb, fn_distrib);

                // положить данные
                string sql = "insert into " + _temp_table_distrib_dom + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, " +
                    "   sum_in, sum_rasp, sum_ud, sum_naud, sum_reval) " +
                    " Select " +
                    " nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, " +
                    "  sum(sum_in), sum(sum_rasp), sum(sum_ud), sum(sum_naud), sum(sum_reval) " +
                    " From " + 
                    (fnDistribExists ? fn_distrib : parent_fn_distrib + " Where dat_oper = " + sOperDate) + 
                    " Group by 1,2,3,4,5";

                ExecSQLWE(_connDb, sql, true);

                // создать индекс
                CreateIndex(_temp_table_distrib_dom);

                return new Returns(true, _temp_table_distrib_dom);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }
                
        /// <summary>
        /// Получить данные по сумме
        /// </summary>
        private void GetDataIntoHelpDistrib(string sumField, string tempTable = "")
        {
            CreateHelpDistribTempTable();
            var existsFnPaXx = true;
            var sql = "";
            // ... вставка данных
            if (sumField == "sum_rasp")
            {
                string s_nzp_payer_poluch = " (select nzp_payer from " +
                    Points.Pref + "_data" + tableDelimiter + "fn_bank bnk, " + 
                    Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank_lnk dbl " + 
                    " where bnk.nzp_fb = dbl.nzp_fb and b.fn_dogovor_bank_lnk_id = dbl.id) ";

                if (!TempTableInWebCashe(_connDb, null, fn_pa_xx)) existsFnPaXx = false;
                sql = "insert into " + _t_help_distrib + " (nzp_supp, nzp_payer, nzp_dom, nzp_serv, nzp_bank, kod, sum_) " +
                    " Select a.nzp_supp, " + //s_nzp_payer_poluch + 
                    " b.nzp_payer_princip " +
                      " as nzp_payer, a.nzp_dom, a.nzp_serv, a.nzp_bank, 1 as kod, sum(sum_prih) " +
                    " From " + fn_pa_xx + " a, " + Points.Pref + "_kernel" + tableDelimiter + "supplier" + " b " +
                    " Where a.nzp_supp = b.nzp_supp " +
                    "   and b.nzp_payer_princip is not null " +
                    _domFilterCondition.Replace(tagName, "a") + // условие на дома
                    " Group by 1,2,3,4,5";
            }
            else if (sumField == "sum_send" || sumField == "sum_reval")
            {
                string tableName = fn_reval;
                if (sumField == "sum_send")
                {
                    tableName = fn_sended;
                }
                
                sql = " insert into " + _t_help_distrib + " (nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank, kod, sum_) " +
                    " Select nzp_payer, nzp_supp, nzp_dom, nzp_serv, -1 as nzp_bank, 1 as kod, sum(" + sumField + ") " +
                    " From " + tableName +
                    " Where dat_oper = " + sOperDate +
                    _domFilterCondition.Replace(tagName, tableName) + // условие на дома
                    " Group by 1,2,3,4";
            }
            else
            {
                if (sumField == "sum_naud")
                {
                    sql = " insert into " + _t_help_distrib + " (nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank, kod, sum_) " +
                        " select nzp_payer_naud, nzp_supp, nzp_dom, nzp_serv, nzp_bank, 1 as kod, sum(sum_naud) " +
                        " From " + tempTable +  
                        " Where 1=1" +
                        _domFilterCondition.Replace(tagName, tempTable) + // условие на дома
                        " Group by 1,2,3,4,5 " + 
                        " having sum(sum_naud) <> 0 ";
                }
                else
                {
                    // sum_ud
                    // ... для этой суммы новые строки не порождаются, поэтому можно использовать условие sum_naud <> 0
                    // ... и можно не учитывать дома
                    sql = " insert into " + _t_help_distrib + " (nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank, kod, sum_) " +
                        " select nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank, 1 as kod, sum(sum_naud) " +
                        " From " + tempTable + 
                        " where sum_naud <> 0 " +
                        " Group by 1,2,3,4,5 ";
                }
            }
            if (existsFnPaXx) ExecSQLWE(_connDb, sql, true);

            CreateIndex(_t_help_distrib);
        }

        /// <summary>
        /// создать вспомогательную временную таблицу
        /// </summary>
        private void CreateHelpDistribTempTable(string sumFieldName = "sum_")
        {
            // ... создать вспомогательную временную таблицу
            DropHelpDistrib();

            string sql = " Create temp table " + _t_help_distrib + " (" +
                " nzp_payer integer, " +
                " nzp_supp  integer default 0, " +
                " nzp_dom   integer, " +
                " nzp_serv  integer, " +
                " nzp_bank  integer, " +
                " kod       integer, " +
                sumFieldName + " " + DBManager.sDecimalType + " (14,2) default 0.00 not null " +
                ") " + DBManager.sUnlogTempTable;

            ExecSQLWE(_connDb, sql, true);
        }

        /// <summary>
        /// Вставка недостающих строк
        /// </summary>
        /// <param name="tempTableName">Название временной таблицы</param>
        /// <param name="nzpPayerFieldName">Название поля с кодом агента</param>
        private void InsertAbsentStringIntoDistribDom()
        {
            // ... найти строки, которых нет в таблице temp_table_distrib_dom
            string sql = " Update " + _t_help_distrib + " hlp " +
                " Set kod = 0 " +
                " Where exists (Select 1 From " + _temp_table_distrib_dom + " dst " +
                    " Where dst.nzp_payer = hlp.nzp_payer " +  
                    "   and dst.nzp_supp  = hlp.nzp_supp " +
                    "   and dst.nzp_dom   = hlp.nzp_dom " +
                    "   and dst.nzp_serv  = hlp.nzp_serv " +
                    "   and dst.nzp_bank  = hlp.nzp_bank " + _limit +
                    " ) ";
            ExecSQLWE(_connDb, sql, true);

            sql = " Create index ix_" + _t_help_distrib + "_kod on " + _t_help_distrib + " (kod) ";
            ExecSQLWE(_connDb, sql, true);

            sql = DBManager.sUpdStat + " " + _t_help_distrib;
            ExecSQLWE(_connDb, sql, true);

            //... вставка строк в таблицу temp_table_distrib_dom
            sql = " insert into " + _temp_table_distrib_dom + " (nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank) " +
                " select nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank " +
                " from " + _t_help_distrib + " " +
                " where kod = 1 " +
                // вместо distinct
                " group by 1,2,3,4,5";
            ExecSQLWE(_connDb, sql, true);
        }

        /// <summary>
        /// Обновить поле с суммой в таблице
        /// </summary>
        private void UpdateDistribDomSumField(string sumField)
        {
            string sql = " Update " + _temp_table_distrib_dom + " dst " +
                " Set " + sumField + " = (" +
                    "Select sum(hlp.sum_) From " + _t_help_distrib + " hlp " +
                    " Where dst.nzp_payer = hlp.nzp_payer " +
                    "   and dst.nzp_supp  = hlp.nzp_supp " +
                    "   and dst.nzp_dom   = hlp.nzp_dom " +
                    "   and dst.nzp_serv  = hlp.nzp_serv " +
                    "   and dst.nzp_bank  = hlp.nzp_bank " +
                    " ) " +
                " Where exists (Select 1 From " + _t_help_distrib + " hlp " +
                    " Where dst.nzp_payer = hlp.nzp_payer " +
                    "   and dst.nzp_supp  = hlp.nzp_supp " +
                    "   and dst.nzp_dom   = hlp.nzp_dom " +
                    "   and dst.nzp_serv  = hlp.nzp_serv " +
                    "   and dst.nzp_bank  = hlp.nzp_bank " + _limit +
                    " ) ";
                
            ExecSQLWE(_connDb, sql, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tempTable"></param>
        private void DropHelpDistrib(bool dropIndex = false)
        {
            ExecSQLWE(_connDb, " Drop table " + _t_help_distrib, false);    
        }

        /// <summary>
        /// Создать индек по полям nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank
        /// </summary>
        /// <param name="tableName">Имя таблицы</param>
        private void CreateIndex(string tableName)
        {
            ExecSQLWE(_connDb, " Create index ix_" + tableName.Trim() + "_1 on " + tableName + " (nzp_payer, nzp_supp, nzp_dom, nzp_serv, nzp_bank)", true);
            ExecSQLWE(_connDb, " Create index ix_" + tableName.Trim() + "_2 on " + tableName + " (nzp_dom)", true);
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + tableName, true);
        }
    }
}
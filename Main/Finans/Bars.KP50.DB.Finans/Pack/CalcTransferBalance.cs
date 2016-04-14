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
    public partial class DbCalcPack : DataBaseHead
    {
        //-----------------------------------------------------------------------------
        /// <summary>
        /// расчет сальдо по перечислениям
        /// </summary>
        /// <param name="conn_db">Соединиение</param>
        /// <param name="OperDate">Опер. день</param>
        /// <param name="nzpWp">Код банка данных</param>
        /// <param name="nzpUser">Код пользователя</param>
        /// <param name="ret"></param>
        /// <param name="setTaskProgress"></param>
        /// <param name="pCountIteration"></param>
        /// <param name="pCurrIteration"></param>
        protected void DistribPaXX(IDbConnection conn_db, DateTime OperDate, int nzpWp, int nzpUser, out Returns ret, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress, int pCountIteration, ref int pCurrIteration)
        {
            ret = new Returns(true);
            using (CalcTransferBalance tranfer = new CalcTransferBalance(conn_db, OperDate, nzpWp, nzpUser))
            {
                ret = tranfer.DistribPaXX(setTaskProgress, pCountIteration, ref pCurrIteration);
            }
        }

        // учет перечисленных сумм
        //-----------------------------------------------------------------------------
        public Returns UpdateSend(IDbConnection conn_db, DateTime operDate)
        {
            Returns ret = new Returns(true);
            using (CalcTransferBalance tranfer = new CalcTransferBalance(conn_db, operDate))
            {
                ret = tranfer.RecalcSumSend();
            }

            return ret;
        }

        //учет процентов удержаний
        //-----------------------------------------------------------------------------
        protected Returns RecalcCommission(IDbConnection conn_db, DateTime operDate)
        {
            Returns ret = new Returns(true);
            using (CalcTransferBalance tranfer = new CalcTransferBalance(conn_db, operDate))
            {
                ret = tranfer.RecalcCommission();
            }
            return ret;
        }

        //учет перекидок
        //-----------------------------------------------------------------------------
        public Returns UpdateRevalSupp(IDbConnection conn_db, DateTime operDate)
        {
            Returns ret = new Returns(true);
            using (CalcTransferBalance tranfer = new CalcTransferBalance(conn_db, operDate))
            {
                ret = tranfer.RecalcReval();
            }
            return ret;
        }

        //обнвовление исходящего сальдо
        //-----------------------------------------------------------------------------
        protected Returns UpdateSumOutSaldo(IDbConnection conn_db, DateTime operDate, int nzp_payer)
        {
            Returns ret = new Returns(true);
            using (CalcTransferBalance tranfer = new CalcTransferBalance(conn_db, operDate))
            {
                ret = tranfer.RecalcSumOutSaldo(nzp_payer);
            }

            return ret;
        }
    }
    
    class CalcTransferBalance : DataBaseHead
    {
        private bool isDebug = false;
        private const int systemUserId = 1;

        private IDbConnection _connDb;

        private DateTime _operDate;
        private string sOperDate;

        
        private string child_fn_pa_xx = "";
        private string child_fn_pa_xx_short = "";
        
        private string parent_fn_pa_xx = "";
        private string parent_fn_pa_xx_short = "";
        
        private string pack = "";
        private string pack_ls = "";

        private string _temp_table_dom = "temp_table_dom";
        
        private int _nzp_wp = 0;
        private int _nzp_user = 0;
        
        // фильтровать данные по временной таблице temp_table_dom (банку данных)
        private bool _filterByLocalBank = false;
        private string _localPref = "";

        private string _limit = "";

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="setTaskProgress"></param>
        /// <param name="pCountIteration"></param>
        /// <param name="pCurrIteration"></param>
        public CalcTransferBalance(IDbConnection conn_db, DateTime operDate) : this(conn_db, operDate, 0, systemUserId) { }
        
        public CalcTransferBalance(IDbConnection conn_db, 
            DateTime OperDate, 
            int nzpWp,
            int nzpUser)
        {
            _connDb = conn_db;

            _operDate = OperDate;
            _nzp_wp = nzpWp;
            _nzp_user = nzpUser;
            if (_nzp_user <= 0) _nzp_user = systemUserId;

            sOperDate = Utils.EStrNull(_operDate.ToShortDateString()) + DBManager.sConvToDate;

            parent_fn_pa_xx_short = "fn_pa_dom_" + (_operDate.Month).ToString("00");
            parent_fn_pa_xx = Points.Pref + "_fin_" + (_operDate.Year % 100).ToString("00") + DBManager.tableDelimiter + parent_fn_pa_xx_short;

            child_fn_pa_xx_short = parent_fn_pa_xx_short + "_" + _operDate.Day.ToString("00");
            child_fn_pa_xx = Points.Pref + "_fin_" + (_operDate.Year % 100).ToString("00") + DBManager.tableDelimiter + child_fn_pa_xx_short;

            pack = Points.Pref + "_fin_" + (_operDate.Year % 100).ToString("00") + DBManager.tableDelimiter + "pack";
            pack_ls = Points.Pref + "_fin_" + (_operDate.Year % 100).ToString("00") + DBManager.tableDelimiter + "pack_ls";
        }

        
        /// <summary>
        /// Расчет сальдо по перечислениям
        /// </summary>
        /// <returns></returns>
        public Returns DistribPaXX(
            DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress, 
            int pCountIteration, 
            ref int pCurrIteration)
        {
#if PG
            _limit = " limit 1 ";
#endif
            try
            {
                if (_nzp_wp > 0)
                {
                    _localPref = Points.GetPref(_nzp_wp);
                    _filterByLocalBank = _localPref != "";                    
                }

                // загрузка домов
                if (_filterByLocalBank)
                {
                    GetTempTableDom(_localPref);
                }

                SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);
                WriteLog("Расчёт сальдо по перечислениям за " + _operDate.ToShortDateString());
                WriteLog(".....Начало расчета сальдо по перечислениям");

                CreatePaXX();
                
                // ... удалить предыдущее распределение
                ClearFnPaXXDom();
                WriteLog(".....Очистка предыдущего распределения завершена");
                SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);

                // ... заполнить fn_pa_dom_XX
                LoadFnPaXXDom();
                SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);

                // ... сохранить событие
                SaveTransferBalanceEvent();
                                
                Returns ret = new Returns(true);

                using (ClassUpdateFnDistribDomXX updFnDistrib = new ClassUpdateFnDistribDomXX(_connDb, _operDate, _filterByLocalBank, _temp_table_dom))
                {
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // ... расчет входящего сальдо
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // создать таблицу temp_table_distrib_dom
                    ret = updFnDistrib.CreateDistribDomTempTable();
                    if (!ret.result) throw new Exception(ret.text);
                    SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);

                    // таблица temp_table_distrib_dom
                    string temp_table_distrib_dom = ret.text;

                    ret = updFnDistrib.UpdateSumInSaldo();
                    if (!ret.result) throw new Exception(ret.text);

                    SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);
                    WriteLog(".....Расчёт входящего сальдо завершен");

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // ... учёт оплат
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    ret = updFnDistrib.UpdateSumRasp();
                    if (!ret.result) throw new Exception(ret.text);

                    SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);
                    WriteLog(".....Учёт оплат завершен");

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // ... учет процентов удержаний
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    using (ClassCalcCommission calcCommission = new ClassCalcCommission(_connDb, _operDate, _filterByLocalBank))
                    {
                        // фильтрация домов производится по таблице temp_table_distrib_dom
                        ret = calcCommission.CalcCommission(temp_table_distrib_dom);
                        if (!ret.result) throw new Exception(ret.text);
                    }

                    SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //учет fn_reval
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    WriteLog(".....Учёт перекидок начат");

                    ret = updFnDistrib.UpdateSumReval();
                    if (!ret.result) throw new Exception(ret.text);

                    SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);
                    WriteLog(".....Учёт перекидок завершен");

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //учет fn_send
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~            
                    WriteLog(".....Учёт перечислений начат");

                    ret = updFnDistrib.UpdateSumSend();
                    if (!ret.result) throw new Exception(ret.text);

                    SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);
                    WriteLog(".....Учёт перечислений завершен");

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //сохранение
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    ret = updFnDistrib.SaveDistribDom();
                    if (!ret.result) throw new Exception(ret.text);

                    SetProgress(setTaskProgress, pCountIteration, ref pCurrIteration);
                }

                WriteLog(".....Cовершено сохранение данных за " + _operDate.ToShortDateString());

                WriteLog("Окончание расчёта сальдо перечисления за " + _operDate.ToShortDateString());

                return new Returns(true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// получить дома из указанного банка
        /// </summary>
        private void GetTempTableDom(string pref)
        {
            ExecSQLWE(_connDb, " Drop table " + _temp_table_dom, false);

            string sql = " Create temp table " + _temp_table_dom + " (nzp_dom integer) " + sUnlogTempTable;
            ExecSQLWE(_connDb, sql, true);

            sql = " insert into " + _temp_table_dom + " (nzp_dom) " + 
                "select nzp_dom from " + pref.Trim() + "_data" + DBManager.tableDelimiter + "kvar group by 1";
            ExecSQLWE(_connDb, sql, true);

            ExecSQLWE(_connDb, "create index ix_" + _temp_table_dom + "_nzp_dom on " + _temp_table_dom + "(nzp_dom)", true);
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + _temp_table_dom, true);
        }
        
        /// <summary>
        /// Пересчет исходящего сальдо
        /// </summary>
        /// <returns></returns>
        public Returns RecalcSumOutSaldo(int nzp_payer)
        {
            try
            {
                using (ClassUpdateFnDistribDomXX updDistrib = new ClassUpdateFnDistribDomXX(_connDb, _operDate))
                {
                    // подготовить временную таблицу, в которой заполнены поля:
                    // sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_send
                    Returns ret = updDistrib.PrepareTempTableForRecalcSumOutSaldo(nzp_payer);
                    if (!ret.result) throw new Exception(ret.text);

                    ret = updDistrib.SaveDistribDom(nzp_payer);
                    if (!ret.result) throw new Exception(ret.text);
                }

                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Пересчет комиссии
        /// </summary>
        /// <returns></returns>
        public Returns RecalcCommission()
        {
            try
            {
                using (ClassUpdateFnDistribDomXX updDistrib = new ClassUpdateFnDistribDomXX(_connDb, _operDate))
                {
                    // подготовить временную таблицу, в которой заполнены поля:
                    // sum_in, sum_reval, sum_send
                    Returns ret = updDistrib.PrepareTempTableForRecalcCommission();
                    if (!ret.result) throw new Exception(ret.text);

                    string tempTable = ret.text;

                    // положить во временную таблицу распределенные оплаты
                    ret = updDistrib.UpdateSumRasp();
                    if (!ret.result) throw new Exception(ret.text);

                    // рассчитать комиссию
                    using (ClassCalcCommission calcCommission = new ClassCalcCommission(_connDb, _operDate, false))
                    {
                        ret = calcCommission.CalcCommission(tempTable);
                        if (!ret.result) throw new Exception(ret.text);
                    }

                    //сохранение
                    ret = updDistrib.SaveDistribDom();
                    if (!ret.result) throw new Exception(ret.text);
                }

                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

        }

        /// <summary>
        /// Пересчет перекидок
        /// </summary>
        /// <returns></returns>
        public Returns RecalcReval()
        {
            try
            {
                using (ClassUpdateFnDistribDomXX updDistrib = new ClassUpdateFnDistribDomXX(_connDb, _operDate))
                {
                    // подготовить временную таблицу, в которой заполнены поля:
                    // sum_in, sum_rasp, sum_ud, sum_naud, sum_send
                    Returns ret = updDistrib.PrepareTempTableForRecalcReval();
                    if (!ret.result) throw new Exception(ret.text);

                    string tempTable = ret.text;

                    // положить во временную таблицу суммы перекидок
                    ret = updDistrib.UpdateSumReval();
                    if (!ret.result) throw new Exception(ret.text);

                    //сохранение
                    ret = updDistrib.SaveDistribDom();
                    if (!ret.result) throw new Exception(ret.text);
                }

                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Пересчет сумм к перечислению
        /// </summary>
        /// <returns></returns>
        public Returns RecalcSumSend()
        {
            try
            {
                using (ClassUpdateFnDistribDomXX updDistrib = new ClassUpdateFnDistribDomXX(_connDb, _operDate))
                {
                    // подготовить временную таблицу, в которой заполнены поля:
                    // sum_in, sum_rasp, sum_ud, sum_naud, sum_reval
                    Returns ret = updDistrib.PrepareTempTableForRecalcSumSend();
                    if (!ret.result) throw new Exception(ret.text);

                    string tempTable = ret.text;

                    // положить во временную таблицу суммы перекидок
                    ret = updDistrib.UpdateSumSend();
                    if (!ret.result) throw new Exception(ret.text);

                    //сохранение
                    ret = updDistrib.SaveDistribDom();
                    if (!ret.result) throw new Exception(ret.text);
                }

                return new Returns(true);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        private void CreatePaXX()
        {
#if PG
            ExecSQL(_connDb, "set search_path to '" + Points.Pref + "_fin_" + (_operDate.Year % 100).ToString("00") + "'", false);
#else
            ExecSQL(_connDb, "database " + Points.Pref + "_fin_" + (_calcYear % 100).ToString("00"), false);
#endif
            
            if (TempTableInWebCashe(_connDb, child_fn_pa_xx))
            {
                return;
            }

            if (!TempTableInWebCashe(_connDb, parent_fn_pa_xx))
            {
                CreateParentFnPaXX();
            }

            CreateChildPaXX();
        }

        private void CreateParentFnPaXX()
        {
            string sql = " Create table " + parent_fn_pa_xx + "(" +
                " nzp_pk serial not null, " +
                " nzp_dom integer, " +
                " nzp_supp integer, " +
                " nzp_serv integer, " +
                " nzp_area integer, " +
                " nzp_geu integer, " +
                " sum_prih   " + DBManager.sDecimalType + "(14,2) default 0 not null," +
                " sum_prih_r " + DBManager.sDecimalType + "(14,2) default 0 not null, " +
                " sum_prih_g " + DBManager.sDecimalType + "(14,2) default 0 not null, " +
                " dat_oper date, " +
                " nzp_bl integer, " +
                " nzp_supp_w integer default 0, " +
                " nzp_area_w integer default 0, " +
                " nzp_bank integer default 0 )";
            ExecSQLWE(_connDb, sql, true);
        }

        private void CreateChildPaXX()
        {
            string sql = " CREATE TABLE " + child_fn_pa_xx + " (CONSTRAINT CNSTR_" + child_fn_pa_xx_short + "_dat_oper CHECK (dat_oper = " + sOperDate + ")) " +
                " INHERITS (" + parent_fn_pa_xx + ") WITHOUT OIDS";
            ExecSQLWE(_connDb, sql, true);

            sql = "ALTER TABLE " + child_fn_pa_xx + " ADD CONSTRAINT PK_" + child_fn_pa_xx_short + " PRIMARY KEY (nzp_pk)";
            ExecSQLWE(_connDb, sql, true);

            sql = "create UNIQUE index IX_" + child_fn_pa_xx_short + "_nzp_pk on " + child_fn_pa_xx + " (nzp_pk)";
            ExecSQLWE(_connDb, sql, true);

            sql = "create index IX_" + child_fn_pa_xx_short + "_dat_oper on " + child_fn_pa_xx + " (dat_oper)";
            ExecSQLWE(_connDb, sql, true);

            sql = "create index IX_" + child_fn_pa_xx_short + "_nzp_supp on " + child_fn_pa_xx + " (nzp_supp)";
            ExecSQLWE(_connDb, sql, true);

            sql = "create index IX_" + child_fn_pa_xx_short + "_nzp_dom on " + child_fn_pa_xx + " (nzp_dom)";
            ExecSQLWE(_connDb, sql, true);
        }

        private void SetProgress(DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress, int pCountIteration, ref int pCurrIteration)
        {
            if (setTaskProgress != null)
            {
                pCurrIteration++;
                setTaskProgress(((decimal)pCurrIteration) / pCountIteration);
            }
        }

        private void ClearFnPaXXDom()
        {
            string sql = "";

            if (_filterByLocalBank)
            {
                sql = "delete from " + child_fn_pa_xx + " pa where exists (select 1 from " + _temp_table_dom + " t where t.nzp_dom = pa.nzp_dom " + _limit + ")";
            }
            else
            {
                sql = "truncate " + child_fn_pa_xx;
            }
                
            Returns ret = ExecSQLWE(_connDb, sql, true);

            if (!ret.result)
            {
                WriteLog(".....Расчёт сальдо перечисления невозможен. Обнаружена взаимоблокировка при очистке таблицы");
                throw new Exception(ret.text);
            }
        }

        private void WriteLog(string log)
        {
            MonitorLog.WriteLog(log, MonitorLog.typelog.Info, 1, 222, true);
        }

        private void LoadFnPaXXDom()
        {
            Returns ret = new Returns(true);
            
            string pref = "";
            string fn_supplier = "";
            string sql = "";
            
            for (int i = 0; i < Points.PointList.Count; i++)
            {
                pref = Points.PointList[i].pref;

                if (_filterByLocalBank && pref != _localPref)
                {
                    continue;
                }
                
                fn_supplier = pref + "_charge_" + (_operDate.Year % 100).ToString("00") + DBManager.tableDelimiter + "fn_supplier" + _operDate.Month.ToString("00");

                sql = " Insert into " + child_fn_pa_xx + " (nzp_dom, nzp_supp, nzp_serv, nzp_area, nzp_geu, nzp_bank, dat_oper, sum_prih) " +
                    " Select k.nzp_dom, s.nzp_supp, s.nzp_serv, k.nzp_area, k.nzp_geu, b.nzp_payer, s.dat_uchet, sum(sum_prih) " +
                    " From  " + pref + "_data" + tableDelimiter + "kvar k," + 
                    fn_supplier + " s, " +
                    pack + " p, " +
                    pack_ls + " pl, " + 
                    Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_bank b " +
                    " Where k.num_ls = s.num_ls " +
                    "   and p.nzp_pack = pl.nzp_pack " +
                    "   and pl.nzp_pack_ls = s.nzp_pack_ls " +
                    "   and p.nzp_bank = b.nzp_bank " +
                    "   and s.dat_uchet = " + sOperDate +
                    " Group by 1,2,3,4,5,6,7 ";

                ExecSQLWE(_connDb, sql, true);
            
            }

        }

        // Сохранить запись в журнал событий
        public int SaveEvent(int nzp_dict, IDbConnection conn_db, int nzp_user, int nzp, string note)
        {
            string sql_text = "insert into " + Points.Pref + "_data" + tableDelimiter + "sys_events(date_, nzp_user, nzp_dict_event, nzp, note) values (" + sCurDateTime + "," + nzp_user + "," + nzp_dict + "," + nzp + ",'" + note.Replace("'", "") + "') ";
            Returns ret = ExecSQL(conn_db, sql_text, true);

            if (!ret.result)
            {
                return 0;
            }
            else return GetSerialValue(conn_db, null);
        }

        private void SaveTransferBalanceEvent()
        {
            int nzp_event = SaveEvent(7427, _connDb, _nzp_user, 0, "Операционный день " + _operDate.ToShortDateString());
            if ((nzp_event > 0) && (isDebug))
            {
                ExecSQL(_connDb, "insert into " + Points.Pref + "_data" + tableDelimiter + "sys_event_detail(nzp_event, table_, nzp) select " + nzp_event + ",'" + pack_ls + "',nzp_pack_ls from t_selkvar", true);
            }
        }
    }
}
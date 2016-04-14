using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Server;
using STCLINE.KP50.Global;
using System.Data;
using STCLINE.KP50.Interfaces;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Класс для взаимодействия с СЗ (загрузка/выгрузка)
    /// </summary>
    public class SuppChargeService : DataBaseHead
    {
        private int _month = 0;
        private int _year = 0;
        //private StringBuilder _sqlLog;
        
        private readonly IDbConnection _connDB;
        public SuppChargeService()
        {
            _connDB = DBManager.GetConnection(Constants.cons_Kernel);
            Returns ret = DBManager.OpenDb(_connDB, true);
            if (!ret.result) _connDB = null;
        }

        /// <summary>
        /// Выборка услуг из начислений сторонних поставщиков для сопоставления
        /// </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <returns></returns>
        public List<string> GetServiceNameLoadSuppCharge(int nzpLoad)
        {
            var result = new List<string>();

            if (_connDB == null) return result;

            try
            {
                string sql = " select distinct service " +
                             " from " + Points.Pref + "_upload" + tableDelimiter + "file_supp_charge " +
                             " where nzp_load = "+nzpLoad;

                var services = DBManager.ExecSQLToTable(_connDB, sql);
                foreach (DataRow service in services.Rows)
                {
                    result.Add(service["service"] ==null ? "" : service["service"].ToString().Trim());
                }

            }
            catch (Exception ex)
            {

                MonitorLog.WriteLog("Ошибка GetServiceNameLoadSuppNach " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
           
            return result;
        }


        /// <summary>
        /// Выборка услуг из начислений сторонних поставщиков для сопоставления
        /// </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <returns></returns>
        public Returns DisassemleLoadSuppCharge(int nzpLoad, Dictionary<string, int> services )
        {
            var result = Utils.InitReturns();

            if (_connDB == null) return result;
            if (nzpLoad <= 0) return result;

            try
            {
                string tableSuppCharge = Points.Pref + "_upload" + tableDelimiter + "file_supp_charge";
                string simpleLoad = Points.Pref + "_data" + tableDelimiter + "simple_load ";
                string sqls = "";

                #region Проставляем услуги и формулы
                foreach (KeyValuePair<string, int> service in services)
                {
                    sqls = " update " + tableSuppCharge +
                                 " set nzp_serv =" + service.Value +
                                 " where nzp_load = " + nzpLoad +
                                 " and service='" + service.Key + "'";
                    result = DBManager.ExecSQL(_connDB, sqls, true);
                    if (!result.result) throw new Exception(result.text);
                }

                // проставить формулу
                sqls = " update " + tableSuppCharge + " a set nzp_frm = " +
                    "(case when lower(measure) = 'куб.м.' then 1991 " +
                    "      when lower(measure) = 'кв.метр' then 1990 " +
                    "      when lower(measure) = 'гКал.' then 1989 " +
                    "      when lower(measure) = 'кВт*час' then 1988 " +
                    "      when lower(measure) = 'с чел.в мес.' then 1987 " +
                    "      when lower(measure) = 'с кв. в меc.' then 1986 " +
                    "      when lower(measure) = 'кг' then 1985 " +
                    " else 1991 end) " +
                    " where a.nzp_load = " + nzpLoad + " and a.nzp_serv is not null ";

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                #endregion

                // получить код и месяц
                sqls = " select year_, month_ from " + simpleLoad + " where nzp_load = " + nzpLoad;
                var datMonthTable = DBManager.ExecSQLToTable(_connDB, sqls);

                _year = Convert.ToInt32(datMonthTable.Rows[0][0]);
                _month = Convert.ToInt32(datMonthTable.Rows[0][1]);

                // получить префикс базы
                sqls = " select bd_kernel " +
                              " from " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                              " where nzp_wp in (select nzp_wp " +
                              " from " + simpleLoad +
                              " where nzp_load = " + nzpLoad + "group by 1)";
                object obj = DBManager.ExecScalar(_connDB, sqls, out result, true);
                if (!result.result) throw new Exception(result.text);
                string pref = obj != null && obj != DBNull.Value ? obj.ToString().Trim() : Points.Pref;

                // сохранение данных в тариф
                result = SaveTarif(tableSuppCharge, pref, nzpLoad);
                if (!result.result) throw new Exception(result.text);

                // сохранение данных в calc_gku
                result = SaveCalcGku(tableSuppCharge, pref, nzpLoad);
                if (!result.result) throw new Exception(result.text);

                // сохранение данных в charge
                result = SaveCharge(tableSuppCharge, pref, nzpLoad);
                if (!result.result) throw new Exception(result.text);
                
                // сохранение ПУ
                result = SaveCounters(nzpLoad, tableSuppCharge, pref);
                if (!result.result) throw new Exception(result.text);
            }
            catch (Exception ex)
            {
                _connDB.Close();
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                result = new Returns(false, ex.Message);
            }
            finally
            {
                _connDB.Close();
            }

            return result;
        }

        /// <summary>
        /// Создать таблицу под ЛС, для которых нет строк в базе
        /// </summary>
        /// <param name="UnfoundLs">Имя временной таблицы для ЛС</param>
        /// <returns></returns>
        private Returns CreateUnfoundLs(string UnfoundLs)
        {
            DBManager.ExecSQL(_connDB, "drop table " + UnfoundLs, true);

            string sqls = "create temp table " + UnfoundLs + " ( " +
                "nzp_kvar  integer, " +
                "nzp_serv  integer, " +
                "nzp_supp  integer, " +
                "nzp_frm   integer " +
                ")";

            return DBManager.ExecSQL(_connDB, sqls, true);
        }

        /// <summary>
        /// Создать таблицу под ЛС, для которых нет строк в базе
        /// </summary>
        /// <param name="UnfoundLs">Имя временной таблицы для ЛС</param>
        /// <returns></returns>
        private Returns CreateUnfoundLsIndex(string UnfoundLs)
        {
            Returns result = DBManager.ExecSQL(_connDB, "create index ix_" + UnfoundLs + "_1 on " + UnfoundLs + " (nzp_kvar, nzp_serv, nzp_supp, nzp_frm)", true);
            if (!result.result) return result;

            return DBManager.ExecSQL(_connDB, DBManager.sUpdStat + " " + UnfoundLs, true);
        }

        /// <summary>
        /// Вставка данных в таблицу tarif
        /// </summary>
        /// <param name="tableSuppCharge">Таблица-источник данных</param>
        /// <param name="pref">Префикс банка данных</param>
        /// <param name="nzpLoad">Ключ загрузки</param>
        /// <returns></returns>
        private Returns SaveTarif(string tableSuppCharge, string pref, int nzpLoad)
        {
            try
            {
                string UnfoundLs = "unfound_ls_tarif";

                #region 1. Заполнение временной таблицы
                //-------------------------------------------------------------------------------------------------------------------------
                // создать временную таблицу
                Returns result = CreateUnfoundLs(UnfoundLs);
                if (!result.result) throw new Exception(result.text);

                // заполнить временную таблицу данными, которых нет в tarif
                string sqls = "insert into " + UnfoundLs + " (nzp_kvar, nzp_serv, nzp_supp, nzp_frm) " +
                    " select a.nzp_kvar, a.nzp_serv, a.nzp_supp, a.nzp_frm " +
                    " from " + tableSuppCharge + " a " +
                    " where a.nzp_load = " + nzpLoad +
                    "   and a.nzp_serv is not null " +
                    "   and not exists (select 1 from " + pref + DBManager.sDataAliasRest + "tarif b " +
                        " where a.nzp_frm = b.nzp_frm " +
                        "   and a.nzp_serv = b.nzp_serv " +
                        "   and a.nzp_supp = b.nzp_supp " +
                        "   and a.nzp_kvar = b.nzp_kvar " +
                        "   and date('01." + _month.ToString("00") + "." + _year + "') between b.dat_s and b.dat_po " +
                        "   and b.is_actual = 1) ";
                
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                // создание индекса и обновление статистики
                result = CreateUnfoundLsIndex(UnfoundLs);
                if (!result.result) throw new Exception(result.text);
                //-------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 2. Вставка данных в таблицу tarif
                //-------------------------------------------------------------------------------------------------------------------------
                // записи, которых нет в базе
                string whereUnFindLs = " and exists (select 1 from " + UnfoundLs + " b " +
                       "    where b.nzp_kvar = a.nzp_kvar " +
                       "    and b.nzp_supp = a.nzp_supp " +
                       "    and b.nzp_serv = a.nzp_serv " +
                       "    and b.nzp_frm = a.nzp_frm) ";
                
                sqls = " insert into " + pref + DBManager.sDataAliasRest + "tarif " +
                       "(nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, " +
                       " is_actual, nzp_user, dat_when, is_unl, cur_unl, nzp_wp) " +
                       " select a.nzp_kvar, k.num_ls, a.nzp_serv, a.nzp_supp, " +
                       " a.nzp_frm, " +
                       " a.tarif, " +
                       " date('01." + _month.ToString("00") + "." + _year + "'), " +
                       " date('01.01.3000'), " +
                       " 1, " +
                       " -1000, " +
                       " " + sCurDate + ", 0, nzp_fsc, nzp_wp " +
                       " from " + tableSuppCharge + " a, " + pref + sDataAliasRest + "kvar k" +
                       " where a.nzp_kvar = k.nzp_kvar " +
                       "    and a.nzp_load = " + nzpLoad +
                    whereUnFindLs;

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                
                sqls = " update  " + tableSuppCharge + " a set nzp_tarif = (select t.nzp_tarif " +
                       " from " + pref + DBManager.sDataAliasRest + "tarif t " +
                       " where t.cur_unl = a.nzp_fsc) " +
                       " where nzp_load = " + nzpLoad +
                    whereUnFindLs;

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                sqls = " update  " + pref + DBManager.sDataAliasRest + "tarif a set cur_unl=0" +
                       " where a.cur_unl in (select nzp_fsc " +
                       " from " + tableSuppCharge +
                       " where nzp_load = " + nzpLoad + ")" +
                    whereUnFindLs;

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                //-------------------------------------------------------------------------------------------------------------------------
                #endregion

                DBManager.ExecSQL(_connDB, "drop table " + UnfoundLs, true);

                return result;
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Вставка данных в calc_gku
        /// </summary>
        /// <param name="tableSuppCharge">Таблица-источник данных</param>
        /// <param name="pref">Префикс банка данных</param>
        /// <param name="nzpLoad">Ключ загрузки</param>
        /// <returns></returns>
        private Returns SaveCalcGku(string tableSuppCharge, string pref, int nzpLoad)
        {
            try
            {
                string tableGkuName = pref + "_charge_" + (_year - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "calc_gku_" + _month.ToString("00");

                string sqls = " insert into " + tableGkuName +
                       "(nzp_kvar, nzp_dom, nzp_serv, nzp_supp, nzp_frm, tarif, rashod,valm, is_device) " +
                       " select a.nzp_kvar, k.nzp_dom, a.nzp_serv, a.nzp_supp, a.nzp_frm, " +
                       " CASE WHEN a.tarif>0 THEN a.tarif WHEN (a.tarif is NULL or a.tarif=0) THEN a.sum_real end ," +
                       " CASE WHEN a.c_calc>0 THEN a.c_calc WHEN (a.c_calc is NULL or a.c_calc=0) THEN 1 end , " +
                       " CASE WHEN a.c_calc>0 THEN a.c_calc WHEN (a.c_calc is NULL or a.c_calc=0) THEN 1 end , " +
                       " nzp_fsc  " +
                       " from " + tableSuppCharge + " a, " + pref + sDataAliasRest + "kvar k" +
                       " where nzp_load=" + nzpLoad + " and a.nzp_kvar=k.nzp_kvar " +
                       " and nzp_serv is not null ";

                Returns result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                
                sqls = " update  " + tableSuppCharge + " set nzp_cls=(select a.nzp_clc " +
                       " from " + tableGkuName + " a " +
                       " where a.is_device=" + tableSuppCharge + ".nzp_fsc) " +
                       " where nzp_load=" + nzpLoad;

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                
                sqls = " update  " + tableGkuName + " set is_device=0" +
                       " where is_device in (select nzp_fsc " +
                       " from " + tableSuppCharge +
                       " where nzp_load=" + nzpLoad + ")";

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                return result;
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Вставка данных в charge
        /// </summary>
        /// <param name="tableSuppCharge">Таблица-источник данных</param>
        /// <param name="pref">Префикс банка данных</param>
        /// <param name="nzpLoad">Ключ загрузки</param>
        /// <returns></returns>
        private Returns SaveCharge(string tableSuppCharge, string pref, int nzpLoad)
        {
            try
            {
                string UnfoundLs = "unfound_ls_charge";
                string tableChargeName = pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + _month.ToString("00");

                #region 1. Заполнение временной таблицы
                //-------------------------------------------------------------------------------------------------------------------------
                // строки, которые есть в базе
                // таблица связка между строками из supp_charge и charge
                string sqls = "create temp table supp_charge_link (" +
                    " nzp_charge integer, " +
                    " nzp_fsc    integer, " +
                    " tarif      " + DBManager.sDecimalType + "(14,2)," +
                    " c_calc     " + DBManager.sDecimalType + "(14,2)," +
                    " sum_real   " + DBManager.sDecimalType + "(14,2)," +
                    " sum_outsaldo " + DBManager.sDecimalType + "(14,2) " +
                    ") ";

                Returns result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                sqls = " insert into supp_charge_link (nzp_charge, nzp_fsc, tarif, c_calc, sum_real, sum_outsaldo) " +
                    " select b.nzp_charge, a.nzp_fsc, a.tarif, a.c_calc, a.sum_real, a.sum_outsaldo " + 
                    " from " + tableSuppCharge + " a, " + tableChargeName + " b " +
                    " where a.nzp_load = " + nzpLoad +
                    "   and a.nzp_serv is not null " +
                    "   and a.nzp_frm  = b.nzp_frm " +
                    "   and a.nzp_serv = b.nzp_serv " +
                    "   and a.nzp_supp = b.nzp_supp " +
                    "   and a.nzp_kvar = b.nzp_kvar ";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                
                // строки, которых нет в базе
                // создать временную таблицу
                result = CreateUnfoundLs(UnfoundLs);
                if (!result.result) throw new Exception(result.text);

                // заполнить временную таблицу данными, которых нет в charge
                sqls = "insert into " + UnfoundLs + " (nzp_kvar, nzp_serv, nzp_supp, nzp_frm) " +
                    " select a.nzp_kvar, a.nzp_serv, a.nzp_supp, a.nzp_frm " +
                    " from " + tableSuppCharge + " a " +
                    " where a.nzp_load = " + nzpLoad +
                    "   and a.nzp_serv is not null " +
                    "   and not exists (select 1 from " + tableChargeName + " b " +
                        " where a.nzp_frm = b.nzp_frm " +
                        "   and a.nzp_serv = b.nzp_serv " +
                        "   and a.nzp_supp = b.nzp_supp " +
                        "   and a.nzp_kvar = b.nzp_kvar) ";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                // создание индекса и обновление статистики временной таблицы
                result = CreateUnfoundLsIndex(UnfoundLs);
                if (!result.result) throw new Exception(result.text);
                //-------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 2. Вставка данных в таблицу charge для записей, которых нет в базе
                //-------------------------------------------------------------------------------------------------------------------------
                // записи, которых нет в базе
                string whereUnfoundLs = "and exists (select 1 from " + UnfoundLs + " b " +
                       "    where b.nzp_kvar = a.nzp_kvar " +
                       "    and b.nzp_supp = a.nzp_supp " +
                       "    and b.nzp_serv = a.nzp_serv " +
                       "    and b.nzp_frm = a.nzp_frm)";

                sqls = " insert into " + tableChargeName + "(" +
                        " nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, " +
                        " tarif, c_calc, rsum_tarif, " +
                        " sum_tarif, sum_real, sum_charge, sum_insaldo, sum_outsaldo, " +
                        " tarif_p, rsum_lgota, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p," +
                        " sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p," +
                        " sum_nedop, sum_nedop_p, reval, real_pere, sum_pere," +
                        " real_charge, sum_money, money_to, money_from ,money_del, " +
                        " sum_fakt, fakt_to, fakt_del,  izm_saldo, " +
                        " c_okaz, c_nedop, isdel, c_reval, tarif_f, " +
                        " tarif_f_p, isblocked) " +
                    " select " + 
                        " a.nzp_kvar, k.num_ls, a.nzp_serv, a.nzp_supp, a.nzp_frm, " +
                        " CASE WHEN a.tarif>0 THEN a.tarif WHEN (a.tarif is NULL or a.tarif=0) THEN a.sum_real end ," +
                       " CASE WHEN a.c_calc>0 THEN a.c_calc WHEN (a.c_calc is NULL or a.c_calc=0) THEN 1 end , " +
                        "a.sum_real, " +
                        " a.sum_real, a.sum_real, a.sum_real, a.sum_real, a.sum_outsaldo, " +
                        " 0,0,0,0,0,0,0,0,0,0," +
                        " 0,0,0,0,0,0,0,0,0,0," +
                        " 0,0,0,0,0,0,0,0,0," +
                    " nzp_fsc  " +
                    " from " + tableSuppCharge + " a, " + pref + sDataAliasRest + "kvar k" +
                    " where nzp_load=" + nzpLoad + " and a.nzp_kvar=k.nzp_kvar " +
                    " and nzp_serv is not null " +
                    whereUnfoundLs;
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                DataTable dt = ClassDBUtils.OpenSQL(" select " +
                        " a.nzp_kvar, k.num_ls, a.nzp_serv, a.nzp_supp, a.nzp_frm, " +
                        " a.tarif, a.c_calc, a.sum_real, " +
                        " a.sum_real, a.sum_real, a.sum_real, a.sum_real, a.sum_outsaldo, " +
                        " 0,0,0,0,0,0,0,0,0,0," +
                        " 0,0,0,0,0,0,0,0,0,0," +
                        " 0,0,0,0,0,0,0,0,0," +
                    " nzp_fsc  " +
                    " from " + tableSuppCharge + " a, " + pref + sDataAliasRest + "kvar k" +
                    " where nzp_load=" + nzpLoad + " and a.nzp_kvar=k.nzp_kvar " +
                    " and nzp_serv is not null " +
                    whereUnfoundLs, _connDB).GetData();

                sqls = " update  " + tableSuppCharge + " a set nzp_charge=(select c.nzp_charge " +
                    " from " + tableChargeName + " c " +
                    " where c.isblocked = a.nzp_fsc) " +
                    " where a.nzp_load = " + nzpLoad +
                    whereUnfoundLs;

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                sqls = " update  " + tableChargeName + " a set isblocked=0" +
                    " where isblocked in (select nzp_fsc " +
                    " from " + tableSuppCharge + "  " +
                    " where nzp_load=" + nzpLoad + ")" +
                    whereUnfoundLs;

                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                //-------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 3. Обновление данных в charge для записей, которые есть в базе
                //-------------------------------------------------------------------------------------------------------------------------
                // обновить данные в charge
                sqls = " update " +  tableChargeName + " k set " +
                    " tarif       = (select a.tarif    from supp_charge_link a where a.nzp_charge = k.nzp_charge), " +
                    " c_calc      = (select a.c_calc   from supp_charge_link a where a.nzp_charge = k.nzp_charge), " +
                    " rsum_tarif  = (select a.sum_real from supp_charge_link a where a.nzp_charge = k.nzp_charge), " +
                    " sum_tarif   = (select a.sum_real from supp_charge_link a where a.nzp_charge = k.nzp_charge), " +
                    " sum_real    = (select a.sum_real from supp_charge_link a where a.nzp_charge = k.nzp_charge), " +
                    " sum_charge  = (select a.sum_real from supp_charge_link a where a.nzp_charge = k.nzp_charge), " +
                    " sum_insaldo = (select a.sum_real from supp_charge_link a where a.nzp_charge = k.nzp_charge), " +
                    " sum_outsaldo = (select a.sum_outsaldo from supp_charge_link a where a.nzp_charge = k.nzp_charge) " +
                    " where exists (select 1 from supp_charge_link a where a.nzp_charge = k.nzp_charge) ";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                
                // проставить ссылки на строки charge в supp_charge
                sqls = " update " + tableSuppCharge + " a set " +
                    " nzp_charge = (select nzp_charge from supp_charge_link b where a.nzp_fsc = b.nzp_fsc) " +
                    " where exists (select 1 from supp_charge_link b where a.nzp_fsc = b.nzp_fsc) ";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                //-------------------------------------------------------------------------------------------------------------------------
                #endregion

                // Добавить период запрещения перерасчета
                AddProhibitedRecalc(tableSuppCharge, pref, nzpLoad);

                // удалить временную таблицу
                DBManager.ExecSQL(_connDB, "drop table " + UnfoundLs, true);
                DBManager.ExecSQL(_connDB, "drop table supp_charge_link", true);
                return result;
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Добавить период запрещения перерасчета
        /// </summary>
        /// <param name="tableSuppCharge"></param>
        /// <param name="pref"></param>
        /// <param name="nzpLoad"></param>
        private void AddProhibitedRecalc(string tableSuppCharge, string pref, int nzpLoad)
        {
            Returns result = new Returns(true);

            // Определим период запрета переасчета
            DateTime datS = new DateTime(_year, _month, 1);
            DateTime datPo = datS.AddMonths(1).AddDays(-1);
            // Проверим, имеются ли добавляемые записи
            string sqls = "select count(*) from " + Points.Pref + sDataAliasRest + "prohibited_recalc pr," + tableSuppCharge + " t " +
                " where t.nzp_kvar = pr.nzp_kvar " +
                "   and t.nzp_serv = pr.nzp_serv " +
                "   and t.nzp_supp = pr.nzp_supp " +
                "   and pr.dat_po = " + Utils.EStrNull(datPo.ToShortDateString()) +
                "   and pr.dat_s = " + Utils.EStrNull(datS.ToShortDateString()) +
                "   and pr.is_actual <> 100 " +
                "   and t.nzp_load = " + nzpLoad;
            int isPhRecalcExists = Convert.ToInt32(ExecScalar(_connDB, sqls, out result, true));
            if (!result.result) throw new Exception(result.text);

            // Если такой записи нет, вставляем
            if (isPhRecalcExists <= 0)
            {
                sqls = "insert into " + Points.Pref + sDataAliasRest + "prohibited_recalc (nzp_kvar, nzp_dom, nzp_serv, dat_s, dat_po, nzp_supp, is_actual) " +
                    "select t.nzp_kvar, k.nzp_dom, t.nzp_serv, date('" + datS.ToShortDateString() + "'), date('" + datPo.ToShortDateString() + "'), t.nzp_supp, 1 " +
                    "from " + tableSuppCharge + " t, " + pref + sDataAliasRest + "kvar k where t.nzp_load=" + nzpLoad + " and  t.nzp_kvar = k.nzp_kvar";
                ExecSQLWE(_connDB, sqls, true);
            }
        }

        /// <summary> Сохранение показаний счетчиков </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <param name="tableSuppCharge"></param>
        private Returns SaveCounters(int nzpLoad, string tableSuppCharge, string pref)
        {
            var result = Utils.InitReturns();
            
            try
            {
                #region Получение существующего Прибора учета
                string sqls = " update " + tableSuppCharge + " set  nzp_counter# = (select min(nzp_counter)" +
                              " from " + pref + DBManager.sDataAliasRest + "counters_spis b " +
                              " where " + tableSuppCharge + ".nzp_kvar= b.nzp and nzp_type=3 " +
                              " and " + tableSuppCharge + ".nzp_serv=b.nzp_serv " +
                              " and num_cnt = " + tableSuppCharge + ".num_cnt# " +
                              " and is_actual=1 )" +
                    " where nzp_load=" + nzpLoad + " and nzp_kvar is not null " +
                     " and nzp_serv is not null and val_cnt# is not null";
                for (int i = 1; i < 4; i++)
                {
                    result = DBManager.ExecSQL(_connDB, sqls.Replace("#", i.ToString(CultureInfo.InvariantCulture)), true);
                    if (!result.result) throw new Exception(result.text);
                }
                #endregion

                #region Генерация нового прибора учета

                sqls = " create temp table unfindCounters (" +
                       " nzp_kvar integer," +
                       " nzp_serv integer," +
                       " nzp_counter integer," +
                       " num integer," +
                       " num_cnt varchar(20))";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                sqls = " insert into unfindCounters ( nzp_kvar, nzp_serv, num, num_cnt) " +
                       " select nzp_kvar, nzp_serv, #, num_cnt# " +
                       " from " + tableSuppCharge +
                       " where nzp_load=" + nzpLoad + " " +
                       " and nzp_serv is not null and nzp_kvar is not null " +
                       " and nzp_counter# is null and val_cnt# is not null ";
                for (int i = 1; i < 4; i++)
                {
                    result = DBManager.ExecSQL(_connDB, sqls.Replace("#", i.ToString(CultureInfo.InvariantCulture)), true);
                    if (!result.result) throw new Exception(result.text);
                }

                sqls = " insert into " + pref + DBManager.sDataAliasRest + "counters_spis(nzp_type, nzp," +
                       " nzp_serv, nzp_cnttype, num_cnt, is_gkal, kod_pu, kod_info, is_actual, " +
                       " nzp_cnt, nzp_user, dat_when)" +
                       " select 3, nzp_kvar, s.nzp_serv, -1, num_cnt, 0, 0, 1, 1, " +
                       " nzp_cnt, 1, " + DBManager.sCurDateTime +
                       " from unfindCounters a, " + pref + DBManager.sKernelAliasRest + "s_counts s" +
                       " where a.nzp_serv=s.nzp_serv ";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);

                sqls = " update " + tableSuppCharge + " set  nzp_counter# = (select min(nzp_counter)" +
                       " from " + pref + DBManager.sDataAliasRest + "counters_spis b " +
                       " where " + tableSuppCharge + ".nzp_kvar= b.nzp and nzp_type=3 " +
                       " and " + tableSuppCharge + ".nzp_serv=b.nzp_serv " +
                       " and num_cnt = " + tableSuppCharge + ".num_cnt# and is_actual=1)" +
                       " where nzp_load=" + nzpLoad + " and nzp_kvar is not null " +
                       " and nzp_serv is not null and val_cnt# is not null";
                for (int i = 1; i < 4; i++)
                {
                    result = DBManager.ExecSQL(_connDB, sqls.Replace("#", i.ToString(CultureInfo.InvariantCulture)), true);
                    if (!result.result) throw new Exception(result.text);
                }
                #endregion

                DBManager.ExecSQL(_connDB, "drop table unfindCounters", true);
                
                #region Сохранение показания
                sqls = " Insert into " + pref + DBManager.sDataAliasRest + " counters " +
                    " (nzp_counter, ist, dat_uchet, val_cnt, is_actual, nzp_user, dat_when," +
                    " nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt) " +
                    " Select b.nzp_counter, " + (int)CounterVal.Ist.File + ", date('01." + _month + "." + _year + "'), val_cnt#, 1, -1, " + DBManager.sCurDate + ", " +
                    "   b.nzp, -404, b.nzp_serv, b.nzp_cnttype, b.num_cnt " +
                    " from " + pref + DBManager.sDataAliasRest + "counters_spis b, " + tableSuppCharge + " a " +
                    " where b.nzp_counter = a.nzp_counter# " +
                    "   and a.nzp_load =" + nzpLoad +
                    "   and a.nzp_kvar is not null " +
                    "   and a.nzp_serv is not null " +
                    "   and a.val_cnt# is not null ";
                for (int i = 1; i < 4; i++)
                {
                    result = DBManager.ExecSQL(_connDB, sqls.Replace("#", i.ToString(CultureInfo.InvariantCulture)), true);
                    if (!result.result) throw new Exception(result.text);
                }

                sqls = " Update " + tableSuppCharge + " a set nzp_cr# = " +
                    " (select max(b.nzp_cr) from " + pref + DBManager.sDataAliasRest + " counters b "  +
                    " where a.nzp_counter# = b.nzp_counter " + 
                    "   and a.val_cnt# = b.val_cnt " + 
                    "   and b.dat_uchet = date('01." + _month + "." + _year + "') " +
                    "   and b.ist = " + (int)CounterVal.Ist.File + ") " +
                    " Where a.nzp_load =" + nzpLoad +
                    "   and a.nzp_kvar is not null " +
                    "   and a.nzp_serv is not null " +
                    "   and a.val_cnt# is not null ";
 
                for (int i = 1; i < 4; i++)
                {
                    result = DBManager.ExecSQL(_connDB, sqls.Replace("#", i.ToString(CultureInfo.InvariantCulture)), true);
                    if (!result.result) throw new Exception(result.text);
                }

                // проставить num_ls
                result = DBManager.ExecSQL(_connDB, " update " + pref + DBManager.sDataAliasRest + " counters c set " +
                    " num_ls = (select a.num_ls from " + pref + DBManager.sDataAliasRest + "kvar a where a.nzp_kvar = c.nzp_kvar) where c.num_ls = -404 ", true);
                if (!result.result) throw new Exception(result.text);
                #endregion

                return result;
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Удаление начислений сторонних поставщиков 
        /// </summary>
        /// <param name="nzpLoad">Код загрузки</param>
        /// <returns></returns>
        public Returns DeleteSuppCharge(int nzpLoad)
        {
            var result = Utils.InitReturns();

            if (_connDB == null) return result;

            try
            {
                string tableSuppCharge = Points.Pref + "_upload" + tableDelimiter + "file_supp_charge ";
                string simpleLoad = Points.Pref + "_data" + tableDelimiter + "simple_load ";

                string sqls = " select year_, month_ from " + simpleLoad +
                             " where nzp_load = " + nzpLoad;
                var datMonth = DBManager.ExecSQLToTable(_connDB, sqls);

                var year = Convert.ToInt32(datMonth.Rows[0][0]);
                var month = Convert.ToInt32(datMonth.Rows[0][1]);

                sqls = " select bd_kernel " +
                       " from " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                       " where nzp_wp in (select nzp_wp " +
                       " from " + simpleLoad +
                       " where nzp_load=" + nzpLoad + "group by 1)";
                object obj = DBManager.ExecScalar(_connDB, sqls, out result, true);
                if (!result.result) throw new Exception(result.text);

                string pref = obj != null && obj != DBNull.Value ? obj.ToString().Trim() : Points.Pref;

                #region Удаляем из тарифа тариф
                sqls = " delete from  " + pref + DBManager.sDataAliasRest + "tarif " +
                       " where nzp_tarif in (select nzp_tarif " +
                       " from " + tableSuppCharge + "  " +
                       " where nzp_load=" + nzpLoad + ")";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                #endregion


                #region Удаляем из calc_gku
                string tableGkuName = pref + "_charge_" + (year - 2000).ToString("00") +
                                    DBManager.tableDelimiter + "calc_gku_" + month.ToString("00");

                sqls = " delete from  " + tableGkuName + " " +
                       " where nzp_clc in (select nzp_cls " +
                       " from " + tableSuppCharge + "  " +
                       " where nzp_load=" + nzpLoad + ")";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                #endregion

                #region Удаляем из charge
                string tableChargeName = pref + "_charge_" + (year - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "charge_" + month.ToString("00");

                sqls = " update " + tableChargeName + " set " + 
                    " tarif = 0, c_calc = 0, rsum_tarif = 0, sum_tarif = 0, sum_charge = 0, " +
                        " sum_real = 0, sum_insaldo = 0, sum_outsaldo = 0 " +
                    " where nzp_charge in (select nzp_charge " +
                    " from " + tableSuppCharge + "  " +
                    " where nzp_load=" + nzpLoad + ")";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                #endregion

                #region Удаляем из prohibited_recalc
                sqls = "update " + Points.Pref + sDataAliasRest + "prohibited_recalc p set is_actual=100 " +
                    "where exists (select 1 from " + tableSuppCharge + " t " +
                    " where t.nzp_kvar = p.nzp_kvar " +
                    " and t.nzp_serv = p.nzp_serv " +
                    " and t.nzp_supp = p.nzp_supp " +
                    " and t.nzp_kvar is not null " +
                    " and t.nzp_supp is not null " +
                    " and t.nzp_serv is not null " +
                   " and " + DBManager.MDY(month, 1, year) + " between p.dat_s and p.dat_po " +
                " and t.nzp_load = " + nzpLoad + ")" +
                " and p.is_actual = 1";
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                #endregion

                #region Перенос показаний в архив
                sqls = " Update " + pref + "_data" + DBManager.tableDelimiter + "counters a " +
                    " Set is_actual = 100, user_del = 1, dat_del = " + sCurDateTime +
                    " where a.nzp_cr in (select b.nzp_cr# from " + tableSuppCharge + " b where nzp_load = " + nzpLoad + ")";
                for (int i = 1; i < 4; i++)
                {
                    result = DBManager.ExecSQL(_connDB, sqls.Replace("#", i.ToString(CultureInfo.InvariantCulture)), true);
                    if (!result.result) throw new Exception(result.text);
                }
                #endregion

                #region Удаляем из file_supp_charge
                sqls = " delete from " + tableSuppCharge + "  " +
                       " where nzp_load=" + nzpLoad;
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                #endregion

                #region Удаляем из simple_load
                sqls = " delete from " + Points.Pref + "_data" + DBManager.tableDelimiter + "simple_load " +
                       " where nzp_load=" + nzpLoad;
                result = DBManager.ExecSQL(_connDB, sqls, true);
                if (!result.result) throw new Exception(result.text);
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + ex, MonitorLog.typelog.Error, 20, 201, true);
                result = new Returns(false, ex.Message);
            }
            finally
            {
                _connDB.Close();
            }

            return result;
        }


        public override void Dispose()
        {
            _connDB.Close();
        }
    }
}

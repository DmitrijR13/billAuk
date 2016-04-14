using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.DB.Finans.Source.WWBFeature
{
    class DbFtrSaveReestrCounters : DataBaseHead
    {
        private readonly FileNameStruct _fileArgs;
        private readonly DbLoadProtokol _loadProtokol;
        private IDbConnection _connDb;
        private Dictionary<string, Dictionary<int, decimal>> maxDiffBetweenValues;
        private string countersTempTable;

        public DbFtrSaveReestrCounters(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, string countersTempTable)
        {
            _fileArgs = fileArgs;
            _loadProtokol = loadProtokol;
            _connDb = connDb;
            maxDiffBetweenValues = GetMaxDiffBetweenValuesDict();
            this.countersTempTable = countersTempTable;
        }

        /// <summary>
        /// Сохранение платежей одной пачки
        /// </summary>
        /// <param name="year">Год в финансах</param>
        /// <returns></returns>
        public Returns SaveCounters(string year)
        {
            Returns ret = Utils.InitReturns();
            IDataReader readerBadCnt = null;
            IDataReader reader = null;
            ExecSQL(_connDb, "Drop table  tmp_counters", false);
            try
            {
                #region Заполняем временную таблицу со счетчиками
                string sql = " Create temp table tmp_counters (" +
                 " nzp_kvar integer, " +
                 " num_ls integer, " +
                 " nzp_counter integer," +
                 " nzp_serv integer default 0," +
                 " val_file " + DBManager.sDecimalType + "(14,4)," +
                 " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                 " nzp_wp integer," +
                 " pref char(10)," +
                 " point varchar(100)," +
                 " transaction_id varchar," +
                 " numCnt char(100)," +
                 " old_dat_uchet Date," +
                 " nzp_reestr_d integer," +
                 " bad_cnt integer default 0," +
                 " rashod  " + DBManager.sDecimalType + "(14,4) default 0," +
                 " max_diff " + DBManager.sDecimalType + "(14,4)," +
                 " pkod " + DBManager.sDecimalType + "(13,0) default 0," +
                 " cnt_stage integer)" + DBManager.sUnlogTempTable;
               ret=ExecSQL(_connDb, sql, true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка создания расширенной временой таблицы для показаний ПУ ");
                }

                sql = "INSERT INTO tmp_counters(pkod,nzp_kvar,num_ls, transaction_id, nzp_counter, val_file, nzp_wp, pref, point)" +
                      " SELECT c.pkod, k.nzp_kvar, k.num_ls, c.transaction_id, c.nzp_counter, c.val_cnt,  k.nzp_wp, k.pref, s.point " +
                      " FROM " + countersTempTable + " c, " +
                      Points.Pref + sDataAliasRest + "kvar k, " +
                      Points.Pref + sKernelAliasRest + "s_point s " +
                      " WHERE s.nzp_wp=k.nzp_wp " +
                      " AND c.pkod=k.pkod ";
                      //" AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                      //" AND date_plat_poruch='" + pack["date_plat_poruch"].ToString().Trim() + "'";
               ret= ExecSQL(_connDb, sql, true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка добавления записей в расширенную таблицу показаний ПУ");
                }

                ret = ExecSQL(_connDb, "create index ixtmp_tc_01 on tmp_counters(nzp_counter)", true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка добавления индекса на код счетчика в расширенную таблицу показаний ПУ");
                }

                ret = ExecSQL(_connDb, "create index ixtmp_tc_02 on tmp_counters(nzp_kvar)", true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка добавления индекса на ЛС счетчика в расширенную таблицу показаний ПУ");
                }

                ret = ExecSQL(_connDb, DBManager.sUpdStat + " tmp_counters", true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка анализа расширенной таблицы показаний ПУ");
                }

                //_loadProtokol.SetProcent(curProgress + step / 4, (int)StatusWWB.InProcess);
                #endregion

                #region Заполнение дополнительных полей

                sql = "SELECT pref, nzp_wp FROM tmp_counters WHERE nzp_wp IS NOT NULL GROUP BY 1,2 ";
               
                ret = ExecRead(_connDb, out reader, sql, true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка получения префиксов из расширенной временной таблицы счетчиков");
                }
                DataTable prefTable = new DataTable();
                prefTable.Load(reader);
                foreach (DataRow dr in prefTable.Rows)
                {
                    string pref=dr["pref"].ToString().Trim();
                    int nzp_wp = (int)dr["nzp_wp"];
                    
                    sql = " UPDATE tmp_counters t SET bad_cnt=1 WHERE nzp_wp = " + nzp_wp + " AND bad_cnt = 0 " +
                          "AND NOT EXISTS (SELECT 1 FROM " + pref + sDataAliasRest + "counters_spis cs " +
                          "WHERE cs.nzp_counter=t.nzp_counter AND cs.is_actual<>100) ";
                    ret=ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка проверки наличия счетчика в системе");
                    }
                    sql = " UPDATE tmp_counters SET nzp_serv=(select max(nzp_serv) " +
                          " FROM " + pref + sDataAliasRest + "counters_spis a" +
                          " WHERE tmp_counters.nzp_counter = a.nzp_counter and nzp_type=3) " +
                          " WHERE  nzp_wp = " + nzp_wp+ " AND bad_cnt=0";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения услуги счетчика");
                    }
                    sql = "UPDATE tmp_counters t SET old_dat_uchet=" +
                          "(SELECT max(dat_uchet) FROM " + pref + sDataAliasRest + "counters a " +
                         "WHERE t.nzp_counter = a.nzp_counter and a.is_actual=1) " +
                         "WHERE  nzp_wp = " + nzp_wp + " AND bad_cnt=0 ";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения последней даты учета показаний счетчиков");
                    }

                    sql = " UPDATE tmp_counters t SET val_cnt=(select max(val_cnt)  " +
                          " FROM " + pref + sDataAliasRest + "counters a" +
                          " WHERE t.nzp_counter = a.nzp_counter AND a.is_actual=1 " +
                          " AND a.dat_uchet=t.old_dat_uchet ) " +
                          " WHERE  nzp_wp = " + nzp_wp + " AND bad_cnt=0 " +
                          " AND old_dat_uchet  is not null";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения значений последних введенных показаний");
                    }

                    //Проставляем расход
                    sql = " update tmp_counters set rashod = val_file - val_cnt " +
                          " where val_cnt is not null  AND  nzp_wp = " + nzp_wp + " AND  bad_cnt =0  ";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка вычесления расхода счетчиков ");
                    }

                    sql = " UPDATE tmp_counters t SET numCnt=(select num_cnt  " +
                         " FROM " + pref + sDataAliasRest + "counters_spis a" +
                         " WHERE t.nzp_counter = a.nzp_counter ) " +
                         " WHERE  nzp_wp = " + nzp_wp + " AND  bad_cnt =0  ";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения заводского номера счетчика");
                    }

                    #region проставляем максимальную разницу по услугам

                    sql = " UPDATE tmp_counters SET max_diff= " + GetDiffByServ(pref, 25) +
                          " WHERE  nzp_wp = " + nzp_wp + " AND nzp_serv =25 AND bad_cnt=0";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения максимального отклонения для счетчиков по услуге электроснабжение ");
                    }
                    sql = " UPDATE tmp_counters SET max_diff= " + GetDiffByServ(pref, 8) +
                          " WHERE  nzp_wp = " + nzp_wp + " AND nzp_serv =8 AND bad_cnt=0 ";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения максимального отклонения для счетчиков по услуге отопление");
                    }
                    sql = " UPDATE tmp_counters SET max_diff= " + GetDiffByServ(pref.Trim(), 9) +
                          " WHERE  nzp_wp = " + nzp_wp + " AND nzp_serv =9 AND bad_cnt=0";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения максимального отклонения для счетчиков по услуге горячее водоснабжение");
                    }

                    sql = " UPDATE tmp_counters SET max_diff= " + GetDiffByServ(pref.Trim(), 10) +
                          " WHERE  nzp_wp = " + nzp_wp + " AND nzp_serv =10 AND bad_cnt=0";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения максимального отклонения для счетчиков по услуге газоснабжение");
                    }
                    sql = " UPDATE tmp_counters SET max_diff= 10000" +
                          " WHERE  nzp_wp = " + nzp_wp + " and bad_cnt=0 and max_diff is null ";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения максимального отклонения показаний счетчиков");
                    }
                    sql = " UPDATE tmp_counters SET bad_cnt= 2 WHERE nzp_wp = " + nzp_wp + " AND bad_cnt=0 AND rashod>max_diff ";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения счетчиков с расходом, превышающем максимальное отклонение");
                    }
                    sql = " UPDATE tmp_counters SET bad_cnt= 3 WHERE nzp_wp = " + nzp_wp + " AND bad_cnt=0 AND rashod<0";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка определения счетчиков с переходом через 0 ");
                    }

                    #endregion
                }

                #endregion
               // _loadProtokol.SetProcent(curProgress + step * 2 / 4, (int)StatusWWB.InProcess);

                #region Выбираем проблемные счетчики

                string commentIsPUValLoaded = " загружено предыдущее показание";//: " данные не загружены.");

                sql = " select * from tmp_counters   where bad_cnt>0";
                ret = ExecRead(_connDb, out readerBadCnt, sql, true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка получения не валидных данных счетчиков");
                }
                DataTable badCounterTable = new DataTable();
                badCounterTable.Load(readerBadCnt);
                foreach (DataRow dr in badCounterTable.Rows)
                {
                    switch ((int) dr["bad_cnt"])
                    {
                        case 1:
                            _loadProtokol.AddUnValidValsForCounter(
                                "",
                                "",
                                dr["val_file"].ToString().Trim(),
                                dr["pkod"].ToString().Trim(),
                                dr["pref"].ToString().Trim(),
                                dr["point"].ToString().Trim(),
                                "Счетчик не зарегистрирован в системе");
                            break;
                        case 2:
                            _loadProtokol.AddUnValidValsForCounter(
                                dr["nzp_counter"].ToString().Trim(),
                                dr["numCnt"].ToString().Trim(),
                                dr["val_file"].ToString().Trim(),
                                dr["pkod"].ToString().Trim(),
                                dr["pref"].ToString().Trim(),
                                dr["point"].ToString().Trim(),
                                "Слишком большое показание для счетчика," + commentIsPUValLoaded);
                            break;
                        case 3:
                            _loadProtokol.AddUnValidValsForCounter(
                                dr["nzp_counter"].ToString().Trim(),
                                dr["numCnt"].ToString().Trim(),
                                dr["val_file"].ToString().Trim(),
                                dr["pkod"].ToString().Trim(),
                                dr["pref"].ToString().Trim(),
                                dr["point"].ToString().Trim(),
                                "Переход через 0," + commentIsPUValLoaded);
                            break;
                    }

                }

                #endregion
               // _loadProtokol.SetProcent(curProgress + step * 3 / 4, (int)StatusWWB.InProcess);

                #region Сохраняем хорошие счетчики

                foreach (DataRow pref in prefTable.Rows)
                {
                    //получаем рассчетный месяц локального банка
                    var localDate = Points.GetCalcMonth(new CalcMonthParams(pref["pref"].ToString().Trim()));
                    if (localDate.month_ == 0 || localDate.year_ == 0) localDate = Points.CalcMonth;

                    string datNextMonth = "'" + new DateTime(localDate.year_, localDate.month_, 1).AddMonths(1)
                        .ToShortDateString() + "'";

                    sql = " insert into  " + Points.Pref + "_fin_" + year + tableDelimiter + "pu_vals  " +
                          " (nzp_pack_ls, num_ls, nzp_counter, val_cnt, dat_month, cur_unl) " +
                          " select pl.nzp_pack_ls, t.nzp_kvar, t.nzp_counter, t.val_file, " + datNextMonth + ",  " +
                          _fileArgs.KvitID +
                          " from " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls pl," +
                          " tmp_counters t " +
                          " WHERE t.pkod = pl.pkod AND t.transaction_id= pl.transaction_id " +
                          " AND t.nzp_wp = " + pref["nzp_wp"] +
                          " AND t.bad_cnt = 0";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка добавления счетчиков в системную таблицу");
                    }

                    //Для показаний вызывающих переход через ноль пишем предыдущие - теперь для всех регионов 
                    sql = " insert into  " + Points.Pref + "_fin_" + year + tableDelimiter + "pu_vals  " +
                          " (nzp_pack_ls, num_ls, nzp_counter, val_cnt, dat_month, cur_unl) " +
                          " select pl.nzp_pack_ls, t.nzp_kvar, t.nzp_counter, t.val_cnt, " + datNextMonth + ",  " +
                          _fileArgs.KvitID +
                          " from " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls pl," +
                          " tmp_counters t " +
                          " where t.pkod = pl.pkod and pl.transaction_id= t.transaction_id " +
                          " and t.nzp_wp = " + pref["nzp_wp"] +
                          " and bad_cnt IN (2,3)";
                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result)
                    {
                        throw new UserException("Ошибка записи предыдущих показаний для невалидных счетчиков в системную таблицу");
                    }
                }

                #endregion

             //   _loadProtokol.SetProcent(curProgress + step * 4 / 4, (int)StatusWWB.InProcess);

            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (readerBadCnt != null)
                {
                    readerBadCnt.Close();
                }
                ExecSQL(_connDb, "drop table tmp_counters", false);
            }

            return ret;
        }



        /// <summary>
        /// заполняем макимальную разницу показаний для отдельного банка
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <returns></returns>
        private Dictionary<int, decimal> GetMaxDiffBetweenValuesDictForOneBank(string pref)
        {
            var resDictionary = new Dictionary<int, decimal>
            {
                {25, GetMaxDiffBetweenValuesOneServ(pref, 2081)},
                {9, GetMaxDiffBetweenValuesOneServ(pref, 2082)},
                {6, GetMaxDiffBetweenValuesOneServ(pref, 2083)},
                {10, GetMaxDiffBetweenValuesOneServ(pref, 2084)}
            };
            return resDictionary;
        }

        /// <summary>
        ///  заполняем макимальную разницу показаний по одной услуге для отдельного банка
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <param name="param">код параметра</param>
        /// <returns></returns>
        private decimal GetMaxDiffBetweenValuesOneServ(string pref, int param)
        {
            Returns ret;
            string sql =
                " SELECT " + sNvlWord + "(max(p.val_prm " + sConvToNum + "), 10000) " +
                " FROM " + pref + sKernelAliasRest + "prm_name pn " +
                " LEFT JOIN " + pref + sDataAliasRest + "prm_10 p ON pn.nzp_prm = p.nzp_prm and p.is_actual = 1" +
                " WHERE pn.nzp_prm = " + param;
            object obj = ExecScalar(_connDb, null, sql, out ret, true);

            decimal result = obj == null || obj == DBNull.Value ? 10000m : Convert.ToDecimal(obj);
            return result;
        }



        /// <summary>
        /// заполняем максимальную разницу показаний по верхнему и всем локальным банкам
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<int, decimal>> GetMaxDiffBetweenValuesDict()
        {
            var resDictionary = new Dictionary<string, Dictionary<int, decimal>>();
            string sql =
                " SELECT distinct trim(bd_kernel) as bd_kernel " +
                " FROM " + Points.Pref + sKernelAliasRest + "s_point";
            var pref = ClassDBUtils.OpenSQL(sql, _connDb, null).resultData;
            foreach (DataRow r in pref.Rows)
                resDictionary.Add(r["bd_kernel"].ToString(),
                    GetMaxDiffBetweenValuesDictForOneBank(r["bd_kernel"].ToString()));
            return resDictionary;
        }

        /// <summary>
        /// Получить максимально допустимую разницу между показаниями ПУ
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        public decimal GetDiffByServ(string pref, int nzpServ)
        {
            if (maxDiffBetweenValues.ContainsKey(pref))
            {
                if (maxDiffBetweenValues[pref].ContainsKey(nzpServ))
                {
                    return maxDiffBetweenValues[pref][nzpServ];
                }


            }
            else if (maxDiffBetweenValues.ContainsKey(Points.Pref))
            {
                if (maxDiffBetweenValues[Points.Pref].ContainsKey(nzpServ))
                {
                    return maxDiffBetweenValues[Points.Pref][nzpServ];
                }
            }
            return 1000000;

        }



    }
}

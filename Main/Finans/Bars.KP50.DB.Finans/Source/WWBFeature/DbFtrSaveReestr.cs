using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.DB.Finans.Source.WWBFeature;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    class DbFtrSaveReestr : DataBaseHead
    {
        protected readonly FileNameStruct _fileArgs;
        protected readonly DbLoadProtokol _loadProtokol;
        protected IDbConnection _connDb;
        protected int _nzpBank;
        private string paymentTempTable;
        private string countersTempTable;
        public DbFtrSaveReestr(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank, string paymentTempTable, string countersTempTable)
        {
            _fileArgs = fileArgs;
            _loadProtokol = loadProtokol;
            _connDb = connDb;
            _nzpBank = nzpBank;
            this.paymentTempTable = paymentTempTable;
            this.countersTempTable = countersTempTable;
        }

        /// <summary>
        /// Управляющая функция
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SyncLsAndInsertPack(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                linkLSWithPkod();
                setBadPayments();
                saveToSystemReestrTables();
                _loadProtokol.SetProcent(40, -999);
                InsertPack(finder);
                analyzeFileReestrTable();
            }
            catch (UserException ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Взаимодействие с банком. Ошибка при загрузке файла: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
            }
            return ret;
        }

        protected Returns InsertPack(DataRow pack, string year, int insertedRowsForPack, decimal sumPack, string parPack, string operDay)
        {
            //записываем в pack
            string sql =
                "INSERT INTO " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                "(pack_type, nzp_bank,num_pack, par_pack , dat_pack, count_kv, " +
                " sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                " select 10, b.nzp_bank, '" + pack["nomer_plat_poruch"] + "'," + parPack + ",'" +
                pack["date_plat_poruch"] + "', " + insertedRowsForPack + ", " + sumPack + "," +
                insertedRowsForPack + ", 11, " + sCurDateTime + ",k.file_name, '" + operDay + "' " +
                "FROM " + Points.Pref + sDataAliasRest + "tula_s_bank b, " +
                Points.Pref + sDataAliasRest + "tula_kvit_reestr k " +
                " WHERE k.nzp_kvit_reestr=" + _fileArgs.KvitID +
                " AND b.branch_id = k.branch_id " +
                " AND b.is_actual<>100";
            return ExecSQL(_connDb, sql, true);
        }


        /// <summary>
        /// Связывает ЛС с платежными кодами
        /// </summary>
        private void linkLSWithPkod()
        {
            #region связываем с ЛС из системы
            string sql = "UPDATE  " + paymentTempTable + " p " +
                         "SET nzp_kvar=k.nzp_kvar " +
                         "FROM " + Points.Pref + sDataAliasRest + "kvar k " +
                         "WHERE k.pkod = p.pkod";
            if (!ExecSQL(_connDb, sql, true).result)
            {
                throw new UserException("Ошибка сопоставления платежных кодов c ЛС");
            }
            #endregion
        }

        /// <summary>
        /// Определить плохие оплаты, и записать их в протокол
        /// </summary>
        private void setBadPayments()
        {
            Returns ret = Utils.InitReturns();
            // Оплаты, в которых не сопоставились ЛС
            string query = "UPDATE " + paymentTempTable + " SET bad_payment=1 WHERE nzp_kvar is null";
            if (!ExecSQL(_connDb, query, true).result)
            {
                throw new UserException("Ошибка получения невалидных ЛС из временной таблицы оплат");
            }
            // Проверить оплаты на повторную загрузку
            query = " UPDATE " + paymentTempTable + " p SET bad_payment=2 WHERE EXISTS " +
                    "(SELECT 1  FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr k " +
                    " WHERE p.pkod=k.pkod AND p.transaction_id=k.transaction_id) AND bad_payment=0";
            ret = ExecSQL(_connDb, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка проверки оплат на повторную загрузку");
            }

            query = "SELECT * FROM " + paymentTempTable + " WHERE bad_payment>0";
            IDataReader reader = null;
            try
            {
                ret = ExecRead(_connDb, out reader, query, true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка получения невалидных оплат из временной таблицы оплат");
                }
                while (reader.Read())
                {
                    if (reader["bad_payment"]==DBNull.Value) continue;
                    int bad_payment = (int) reader["bad_payment"];
                    string pkod="0";
                    if (reader["pkod"] != DBNull.Value)
                    {
                        pkod=reader["pkod"].ToString();
                    }
                    string nzp_kvar = "0";
                    if (reader["nzp_kvar"] != DBNull.Value)
                    {
                        nzp_kvar = reader["nzp_kvar"].ToString();
                    }
                    string transaction_id = "";
                    if (reader["transaction_id"] != DBNull.Value)
                    {
                        transaction_id=reader["transaction_id"].ToString();
                    }
                    switch (bad_payment)
                    {
                        case 1:
                            _loadProtokol.AddComment("Платежный код "+pkod+ " не зарегистрирован в системе");
                            break;
                        case 2 :
                            _loadProtokol.AddComment("Оплата ЛС " + nzp_kvar + ", платежный код "+pkod+", транзакция "+transaction_id+
                            " уже была загружена и будет пропущена при загрузке");
                            break;
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

        }
        /// <summary>
        /// Сохраняет данные в таблицы tula_file_reestr и tula_counter_reestr
        /// </summary>
        private void saveToSystemReestrTables()
        {
            Returns ret = Utils.InitReturns();
            string query = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                    "(nzp_reestr_d, pkod, nzp_kvar, " +
                    "nzp_kvit_reestr, sum_charge," +
                    "transaction_id, date_plat_poruch, " +
                    "nomer_plat_poruch, service_field, payment_datetime, " +
                    "cnt1, val_cnt1, cnt2, val_cnt2," +
                    "cnt3, val_cnt3,cnt4, val_cnt4," +
                    "cnt5, val_cnt5,cnt6, val_cnt6) " +
                    "SELECT nzp_reestr_d, pkod, nzp_kvar," +
                    _fileArgs.KvitID + ", sum_charge, " +
                    "transaction_id, date_plat_poruch, " +
                    "nomer_plat_poruch, service_field, payment_date_time, " +
                    "cnt1, val_cnt1, cnt2, val_cnt2," +
                    "cnt3, val_cnt3,cnt4, val_cnt4," +
                    "cnt5, val_cnt5,cnt6, val_cnt6 " +
                    "FROM " + paymentTempTable + " WHERE bad_payment=0 ";
            ret = ExecSQL(_connDb, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка в процессе переноса данных в системный реестр оплат");
            }
            // обновим nzp_reestr_d во временной таблице для счечиков
            query = "UPDATE " + countersTempTable + " c SET nzp_reestr_d= (SELECT nzp_reestr_d FROM " + paymentTempTable + " p " +
                           "WHERE c.pkod=p.pkod AND c.transaction_id=p.transaction_id AND bad_payment=0)";
            ret = ExecSQL(_connDb, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка обновления колонки nzp_reestr_d во временной таблице для показаний ПУ");
            }
            query = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_counters_reestr (nzp_reestr_d, nzp_counter, cnt, val_cnt) " +
                    " SELECT nzp_reestr_d, nzp_counter, nzp_counter, val_cnt FROM " + countersTempTable + " WHERE nzp_reestr_d is not null";
            ret = ExecSQL(_connDb, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка в процессе переноса показаний ПУ в системный реестр ");
            }

        }

        /// <summary>
        /// Запись в систему: пачки, оплаты, показания ПУ
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns InsertPack(FilesImported finder)
        {
            Returns ret;
            string datPrev = "'" +
                             new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1)
                                 .ToShortDateString() + "'"; //предыдущий рассчетный месяц
            string year = (Points.DateOper.Year - 2000).ToString("00");

            string operDay = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString(
                "dd.MM.yyyy");
            int nzpUser = finder.nzp_user;

            //проверяем кол-во пачек. если >1 создаем суперпачку и связываем с ней остальные пачки
            string sql = " SELECT nomer_plat_poruch, date_plat_poruch " +
                         " FROM " + paymentTempTable + " WHERE bad_payment=0 " +
                         " GROUP BY nomer_plat_poruch, date_plat_poruch ";
            IDataReader reader = null;
            try
            {
                ret = ExecRead(_connDb, out reader, sql, true);
                if (!ret.result)
                {
                    throw new UserException("Ошибка получения пачек из временной таблицы оплат");
                }
                DataTable packs = new DataTable();
                packs.Load(reader);
                if (packs.Rows.Count > 1)
                {
                    int progress = 30/packs.Rows.Count;
                    int superPack = SaveSuperPack(year, operDay);

                    for (int i = 0; i < packs.Rows.Count; i++)
                    {
                        SaveOnePack(packs.Rows[i], year, superPack, operDay, nzpUser, datPrev);
                        _loadProtokol.SetProcent(40 + progress*i, (int) StatusWWB.InProcess);
                    }

                }
                else if (packs.Rows.Count == 1)
                {
                    SaveOnePack(packs.Rows[0], year, 0, operDay, nzpUser, datPrev);
                }
                _loadProtokol.SetProcent(70, (int) StatusWWB.InProcess);

                #region Сохранение счетчиков

                var dbSaveReestrCounters = new DbFtrSaveReestrCounters(_connDb, _fileArgs, _loadProtokol, countersTempTable);
                ret = dbSaveReestrCounters.SaveCounters(year);

                #endregion
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return ret;
        }

        private void analyzeFileReestrTable()
        {
            string query = "ANALYZE " + Points.Pref + sDataAliasRest + "tula_file_reestr";
            if (!ExecSQL(_connDb, query, true).result)
            {
                throw new UserException("Ошибка операции анализа реестра загруженных оплат, см. логи");    
            }
        }

        /// <summary>
        /// Сохранение одной пачки с платежами
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="idSuperPack"></param>
        /// <param name="operDay"></param>
        /// <param name="nzpUser"></param>
        /// <param name="datPrev"></param>
        /// <returns></returns>
        private Returns SaveOnePack(DataRow pack, string year, int idSuperPack,
            string operDay, int nzpUser, string datPrev)
        {
            Returns ret;
            string finAlias = Points.Pref + "_fin_" + year + tableDelimiter;
            decimal sumPack = 0;
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре
            string sql = " SELECT sum(sum_charge) FROM " + paymentTempTable +
                         " WHERE bad_payment=0" +
                         " AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                         " AND date_plat_poruch='" + pack["date_plat_poruch"] + "'";
            object obj = ExecScalar(_connDb, sql, out ret, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка получения значений сумм оплат из временной таблицы оплат");    
            }
            if (obj != null && obj != DBNull.Value)
            {
                sumPack = Convert.ToDecimal(obj);
            }
            else
            {
                throw new UserException("Ошибка получения значений сумм оплат из временной таблицы оплат. Вернулось не корректное значение суммы");    
            }

            //кол-во строк в пачке
            var insertedRowsForPack = 0;
            sql = "SELECT count(*) FROM " + paymentTempTable + " " +
                  "WHERE bad_payment=0 " +
                  "AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "' " +
                  "AND date_plat_poruch='" + pack["date_plat_poruch"] + "' ";
            obj = ExecScalar(_connDb, sql, out ret, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка получения количества строк в пачке из временной таблицы оплат");
            }
            if (obj != null && obj != DBNull.Value)
            {
                insertedRowsForPack = Convert.ToInt32(obj);
            }
            else
            {
                throw new UserException("Ошибка получения количества строк пачки из временной таблицы оплат. " +
                                        "Вернулось некорректное значение количества строк в пачке");    
            }
            _loadProtokol.CountInsertedRows += insertedRowsForPack;
            string parPack;
            if (idSuperPack == 0) parPack = "NULL";
            else parPack = idSuperPack.ToString(CultureInfo.InvariantCulture);

            //записываем в pack
            ret = InsertPack(pack, year, insertedRowsForPack, sumPack, parPack, operDay);
            if (!ret.result)
            {
                throw new UserException("Ошибка добавление записи о пачке оплат");
            }

            var thisPack = GetSerialValue(_connDb);
            _loadProtokol.NzpPack.Add(thisPack);
       
            //Добавляем оплаты по ЛС
            sql = " INSERT INTO " + finAlias + "pack_ls " +
                  " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                  " inbasket, alg, unl, incase, pkod, nzp_user,dat_month, transaction_id) " +
                  " SELECT " + thisPack + ", k.num_ls, p.sum_charge, 0 as sum_ls, 33, 1 as paysource," +
                  "  0 as id_bill , payment_date_time, " + //дата оплаты = дата из кода транзакции 
                  " (CASE WHEN length(p.transaction_id) >= 10 THEN substr(p.transaction_id,10,6) ELSE p.transaction_id END)" + sConvToInt + " as num_oper, " +
                  " 0 as inbasket, 0 as alg, 0 as unl, 0 as incase, p.pkod , " + nzpUser + " ," + datPrev + ", p.transaction_id " +
                  " FROM " + paymentTempTable + " p, " + Points.Pref + sDataAliasRest + "kvar k " +
                  " WHERE p.bad_payment=0 " +
                  " AND k.nzp_kvar=p.nzp_kvar " +
                  " AND p.nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                  " AND p.date_plat_poruch='" + pack["date_plat_poruch"].ToString().Trim() + "'";
            ret = ExecSQL(_connDb, sql, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка добавления записи оплат пачки");
            }
            return ret;
        }

        /// <summary>
        /// Сохранение cуперпачки в pack
        /// </summary>
        /// <param name="year"></param>
        /// <param name="sumPack"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        protected Returns InsertSuperPack(string year, decimal sumPack, string operDay)
        {
            string sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                  "(pack_type, nzp_bank,num_pack, dat_pack, count_kv, sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                  " select 10, 1000, substr(k.file_name,0,8), k.date_plat, " + _loadProtokol.CountInsertedRows + ", " +
                  sumPack + "," + _loadProtokol.CountInsertedRows + ", 11, " + sCurDateTime + ",k.file_name " +
                  ", '" + operDay + "' " +
                  " from " + Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k " +
                  " where k.nzp_kvit_reestr=" + _fileArgs.KvitID;
            return ExecSQL(_connDb, sql, true);
        }

        /// <summary>
        /// Сохранение суперпачки
        /// </summary>
        /// <param name="year"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        private int SaveSuperPack(string year, string operDay)
        {
            Returns ret;
            int result = 0;
            decimal sumPack;
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть 
            //загружены ранее в период.реестре
            string sql = " select sum(sum_charge) " +
                         " from " + paymentTempTable +
                         " where bad_payment=0";

            object obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                sumPack = Convert.ToDecimal(obj);
            }
            else
            {
                throw new UserException("Ошибка получения суммы оплат для суперпачки");
                //MonitorLog.WriteLog("Ошибка получения суммы оплат для суперпачки: " + sql,
                //    MonitorLog.typelog.Error, true);
                //return result;

            }
            ret = InsertSuperPack(year, sumPack, operDay);
            if (!ret.result)
            {
                throw new UserException("Ошибка сохранения суперпачки");
            }
            result = GetSerialValue(_connDb);
            sql = " UPDATE " + Points.Pref + "_fin_" + year + tableDelimiter + "pack " +
                  " SET par_pack=" + result + " " +
                  " WHERE nzp_pack=" + result;
            if (!ExecSQL(_connDb, sql, true).result)
            {
                throw new UserException("Ошибка проставления номера родительской пачки дочерним пачкам");
            }
            return result;
        }
    }
}

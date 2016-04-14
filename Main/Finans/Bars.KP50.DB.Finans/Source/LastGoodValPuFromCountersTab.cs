using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Finans.Source
{
    public class LastGoodValPuFromCountersTab
    {
        private readonly string localcounterstable;
        public LastGoodValPuFromCountersTab(string table)
        {
            localcounterstable = table;
        }
        /// <summary>
        /// Извлекает последние удачные данные по ПУ для ЛС из local_counters за дату max(период_local_counters меньше (datMonth + 2 месяца))
        /// Вызывается, если в таблице counters_ord таких записей для ЛС не обнаружилось
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="numLS"></param>
        /// <param name="datMonth"></param>
        /// <param name="puValsForOneLSFromPack"></param>
        /// <returns></returns>
        public Returns GetLastGoodValPUByOrderingFromDB(IDbConnection connDB, Pack_ls packLS, List<PuVals> puValsForOneLSFromPack, ref AddedPacksInfo packsInfo)
        {
            Returns ret = Utils.InitReturns();
            if (puValsForOneLSFromPack == null || puValsForOneLSFromPack.Count(pu => pu.nzp_counter <= 0) == 0) return ret;
            string datMonthAddedTwo = Convert.ToDateTime(packLS.dat_month).AddMonths(2).ToShortDateString();
            MyDataReader readerTemp = null;
            string sql = "SELECT num_ls, nzp_serv, nzp_counter, num_cnt,  dat_uchet, val_cnt from " + localcounterstable + " a " +
                         " where  num_ls = " + packLS.num_ls + " and is_actual=1 and dat_uchet=(select max(dat_uchet) from " + localcounterstable + " b " +
                         "where a.nzp_counter=b.nzp_counter and is_actual=1 and b.num_ls=" + packLS.num_ls + " and b.dat_uchet<='" + datMonthAddedTwo + "') order by nzp_serv, num_cnt, nzp_counter ";
            List<PuVals> puValsFromCountersTable = new List<PuVals>();
            try
            {
                #region Не стандартная схема
                // pu.dat_month - месяца оплаты счета, но в pu_vals ТЕПЕРЬ должен
                //лежать месяц учета показаний, поэтому увеличиваем на 2 месяца
                ret = DBManager.ExecRead(connDB, out readerTemp, sql, true);
                if (!ret.result)
                {
                    string msg = "Ошибка выборки значений счетчика(ов) для ЛС " + packLS.num_ls;
                    packsInfo.AddErrorMsg(msg, packLS.pref);
                    MonitorLog.WriteLog(msg + " (local_counters)" + sql, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                int i = 0;
                while (readerTemp.Read())
                {
                    if (readerTemp["nzp_serv"] == DBNull.Value) continue;
                    PuVals puLst = new PuVals();
                    puLst.nzp_serv = Convert.ToInt32(readerTemp["nzp_serv"].ToString().Trim());
                    puLst.nzp_counter = Convert.ToInt32(readerTemp["nzp_counter"].ToString().Trim());
                    puLst.num_cnt = readerTemp["num_cnt"].ToString().Trim();
                    puLst.dat_uchet = readerTemp["dat_uchet"].ToString();
                    puLst.ordering = ++i;
                    decimal d;
                    if (Decimal.TryParse(readerTemp["val_cnt"].ToString(), out d))
                    {
                        puLst.val_cnt = d;
                    }
                    else
                    {
                        packsInfo.AddErrorMsg("Ошибка получения показания ПУ для ЛС " + packLS.num_ls + ", номер счетчика " + puLst.num_cnt, packLS.pref);
                        string msg = "Ошибка получения показания ПУ из таблицы " + localcounterstable + " для " +
                                     "ЛС " + packLS.num_ls + ", " +
                                     "nzp_counter " + puLst.nzp_counter + ", " +
                                     "номер счетчика " + puLst.num_cnt + ", " +
                                     "услуга № " + puLst.nzp_serv + " (local_counters)";
                        MonitorLog.WriteLog(msg + " ", MonitorLog.typelog.Error, 20, 201, true);
                    }
                    puValsFromCountersTable.Add(puLst);
                }
                #endregion
            }
            catch (Exception ex)
            {
                ret.result = false;
                string msg = "Ошибка определения порядкого номера услуг в изменениях ЛС " + packLS.num_ls;
                packsInfo.AddErrorMsg(msg, packLS.pref);
                MonitorLog.WriteLog(msg + " (local_counters)" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (readerTemp != null)
                    readerTemp.Close();
            }
            getPuFromCountersTable(puValsForOneLSFromPack, ref puValsFromCountersTable, packLS, ref packsInfo);
            return ret;
        }

        private Returns getPuFromCountersTable(List<PuVals> puValsForOneLSFromPack, ref List<PuVals> puValsFromCountersTable, Pack_ls packLs, ref AddedPacksInfo packsInfo)
        {
            Returns ret = Utils.InitReturns();
            string shortDatMonths = packLs.dat_month.Length > 10 ? packLs.dat_month.Remove(10) : packLs.dat_month;
            foreach (PuVals puValPack in puValsForOneLSFromPack)
            {
                //  если в таблице counters не нашлось ни одного ПУ с соответствующим порядковым номером
                if (puValsFromCountersTable.Count(pu => pu.ordering == puValPack.ordering) == 0)
                {
                    packsInfo.AddWarnMsg("Не найден ПУ для ЛС " + packLs.num_ls + ", " +
                                         "номер счетчика " + puValPack.num_cnt + ", " +
                                         "показание из файла пачки " + puValPack.val_cnt + ", " +
                                         " дата учета из файла пачки " + shortDatMonths, packLs.pref);
                    continue;
                }
                PuVals puValFromCountersTable = puValsFromCountersTable.First(pu => pu.ordering == puValPack.ordering);
                puValPack.nzp_counter = puValFromCountersTable.nzp_counter;
                puValPack.nzp_serv = puValFromCountersTable.nzp_serv;
                puValPack.num_cnt = puValFromCountersTable.num_cnt;
                // Если значение ПУ из файла пачки меньше, чем из таблицы counters
                if (puValPack.val_cnt < puValFromCountersTable.val_cnt)
                {
                    //проставим предыдущее показание
                    puValPack.val_cnt = puValFromCountersTable.val_cnt;
                    string msg = "Обнаружен переход через ноль для ЛС " + packLs.num_ls + ", " +
                                 " № счетчика: " + puValPack.num_cnt + "," +
                                 " показание из файла пачки: " + puValPack.val_cnt + " от " + shortDatMonths + ", " +
                                 " будет загружено последнее введенное показание: " + puValFromCountersTable.val_cnt +
                                 " от " + (puValFromCountersTable.dat_uchet.Length > 10 ? puValFromCountersTable.dat_uchet.Remove(10) : puValFromCountersTable.dat_uchet);
                    packsInfo.AddWarnMsg(msg, packLs.pref);
                    MonitorLog.WriteLog(msg, MonitorLog.typelog.Error, 20, 201, true);
                }
            }
            return ret;
        }
    }
}

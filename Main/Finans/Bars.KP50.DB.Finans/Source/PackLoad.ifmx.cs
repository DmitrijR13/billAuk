using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Bars.KP50.DB.Finans.Source;
using FastReport;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbPack
    {
        private TypePayCode typePayCode = TypePayCode.Standart;
        public Returns UploadPack(Pack finder)
        {
            return new Returns(false, "Функция не реализована", -1);
        }


        /// <summary>
        /// Удаление пачки в случае ошибки в ходе загрузки
        /// </summary>
        /// <param name="listPack"></param>
        /// <param name="connDB">Подключение к финансовой базе</param>
        private void DeleteBadPack(List<Pack> listPack, IDbConnection connDB)
        {
            var sql = new StringBuilder();

            try
            {

                for (int i = 0; i <= listPack.Count - 1; i++)
                {

                    string baseName = Points.Pref + "_fin_" + (listPack[i].year_ % 100).ToString("00");

                    sql.Remove(0, sql.Length);
                    sql.Append(" DELETE from " + baseName + DBManager.tableDelimiter + "gil_sums " +
                               " where nzp_pack_ls in (select a.nzp_pack_ls " +
                               " from " + baseName + DBManager.tableDelimiter + "pack_ls a where a.nzp_pack = ");
                    sql.Append(listPack[i].nzp_pack + ")");
                    Returns ret = ExecSQL(connDB, sql.ToString(), true);
                    if (!ret.result)
                    {

                        MonitorLog.WriteLog("Ошибка удаления уточнения оплат " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }


                    sql.Remove(0, sql.Length);
                    sql.Append(" DELETE from " + baseName + DBManager.tableDelimiter + "pu_vals " +
                               " where nzp_pack_ls in (select a.nzp_pack_ls " +
                               " from " + baseName + DBManager.tableDelimiter + "pack_ls a where a.nzp_pack = ");
                    sql.Append(listPack[i].nzp_pack + ")");
                    ret = ExecSQL(connDB, sql.ToString(), true);
                    if (!ret.result)
                    {

                        MonitorLog.WriteLog("Ошибка удаления показаний счетчиков " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }


                    sql.Remove(0, sql.Length);
                    sql.Append("DELETE from " + baseName + DBManager.tableDelimiter + "pack_ls where nzp_pack = "
                    + listPack[i].nzp_pack);
                    ret = ExecSQL(connDB, sql.ToString(), true);
                    if (!ret.result)
                    {

                        MonitorLog.WriteLog("Ошибка удаления пачки " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append("DELETE from " + baseName + DBManager.tableDelimiter + "pack " +
                               "where nzp_pack = " + listPack[i].nzp_pack);
                    ret = ExecSQL(connDB, sql.ToString(), true);
                    if (!ret.result)
                    {

                        MonitorLog.WriteLog("Ошибка удаления пачки " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }

                    //если суперпачка есть и она пустая, удаляем
                    if (listPack[i].par_pack > 0)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(
                            " DELETE FROM " + baseName + DBManager.tableDelimiter + "pack " +
                            " WHERE nzp_pack = " + listPack[i].par_pack +
                            " AND NOT EXISTS (SELECT 1 FROM " + baseName + DBManager.tableDelimiter + "pack p" +
                            " WHERE p.par_pack = " + listPack[i].par_pack + " AND p.nzp_pack <> " + listPack[i].par_pack + ")");
                        ret = ExecSQL(connDB, sql.ToString(), true);
                        if (!ret.result)
                        {

                            MonitorLog.WriteLog("Ошибка удаления пачки " + (Constants.Viewerror ? "\n" +
                                sql + "(" + ret.sql_error + ")" : ""),
                                MonitorLog.typelog.Error, 20, 201, true);
                            return;
                        }
                    }

                    #region Добавление в sys_events события 'Удаление пачки оплат'
                    DbAdmin.InsertSysEvent(new SysEvents
                    {
                        pref = Points.Pref,
                        bank = baseName,
                        nzp_user = listPack[i].nzp_user,
                        nzp_dict = 6500,
                        nzp_obj = listPack[i].nzp_pack,
                        note = "Пачка оплат была успешно удалена"
                    }, connDB);
                    #endregion
                }
            }
            catch
            {
                MonitorLog.WriteLog("Неожиданная ошибка удаления пачки " + (Constants.Viewerror ? "\n" +
                           sql : ""),
                           MonitorLog.typelog.Error, 20, 201, true);
            }
        }

        /// <summary>
        /// Сохранение платежей в случае удачной загрузки
        /// </summary>
        /// <param name="listPack"></param>
        /// <param name="connDB">Подключение к финансовой базе</param>
        private Returns SetGoodLoadPack(List<Pack> listPack, IDbConnection connDB)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                for (int i = 0; i <= listPack.Count - 1; i++)
                {
                    string baseName = Points.Pref + "_fin_" + (listPack[i].year_ % 100).ToString("00");
                    string sql = "update " + baseName + DBManager.tableDelimiter + "pack_ls " +
                                 "set unl = 0 where nzp_pack = " + listPack[i].nzp_pack;
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка сохранения пачки, см. логи ";
                        MonitorLog.WriteLog("Ошибка сохранения пачки " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
            }
            catch
            {
                ret.text = "Ошибка сохранения пачки, см. логи ";
                MonitorLog.WriteLog("Неожиданная ошибка сохранения пачки " + (Constants.Viewerror ? "\n" : ""),
                           MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return ret;
            }
            return ret;
        }

        /// <summary>
        ///  Сохранение пачки
        /// </summary>
        /// <param name="pack">Пачка Pack</param>
        /// <param name="listLs">Список оплат</param>
        /// <param name="connDB">Подключение к финансовой базе</param>
        /// <returns></returns>
        private int SavePack(Pack pack, List<Pack_ls> listLs, IDbConnection connDB, out Returns ret)
        {
            var sql = new StringBuilder();
            ret = Utils.InitReturns();
            string baseName = Points.Pref + "_fin_" +
                              (pack.year_ % 100).ToString("00");

            try
            {
                #region Сохранение пачки
                DateTime datPack = Convert.ToDateTime(pack.dat_pack);
                sql.Append("INSERT into " + baseName + DBManager.tableDelimiter + "pack(pack_type, nzp_bank, ");
                sql.Append(" num_pack,dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, file_name)");
                sql.Append("VALUES(10, " + pack.nzp_bank + "," + pack.num + ",");
                sql.Append(MDY(datPack.Month, datPack.Day, datPack.Year) + "," +
                    MDY(Points.DateOper.Month, Points.DateOper.Day, Points.DateOper.Year) +
                    "," + pack.count_kv + ",");
                sql.Append(pack.sum_pack + ",11,'" + pack.erc_code + "','");
                sql.Append(pack.file_name + "')");

                ret = ExecSQL(connDB, sql.ToString(), true);
                if (!ret.result)
                {
                    ret.text = "Ошибка загрузки пачки, см. логи ";
                    MonitorLog.WriteLog("Ошибка загрузки пачки " + (Constants.Viewerror ? "\n" +
                        sql + "(" + ret.sql_error + ")" : ""),
                        MonitorLog.typelog.Error, 20, 201, true);
                    return 0;
                }
                pack.nzp_pack = DBManager.GetSerialValue(connDB);

                #endregion

                #region Сохранение списка ЛС

                for (int i = 0; i <= listLs.Count - 1; i++)
                {
                    DateTime d2 = Convert.ToDateTime(listLs[i].dat_month);
                    DateTime d3 = Convert.ToDateTime(listLs[i].dat_vvod);


                    string pkodPrefix = "0";
                    string pkod = listLs[i].pkod;
                    if (!String.IsNullOrEmpty(pkod) && pkod.Length > 3)
                    {
                        pkodPrefix = listLs[i].pkod.Substring(0, 3);
                    }
                    else
                    {
                        pkod = "0";
                    }
                    string month_from = "";
                    string month_to = "";
                    string fields = "";
                    if (DBManager.isTableHasColumn(connDB, "pack_ls", "type_pay", baseName))
                    {
                        month_from = listLs[i].month_from == DateTime.MinValue ? "null" : Utils.EStrNull(listLs[i].month_from.ToShortDateString()) + DBManager.sConvToDate;
                        month_to = listLs[i].month_to == DateTime.MinValue ? "null" : Utils.EStrNull(listLs[i].month_to.ToShortDateString()) + DBManager.sConvToDate;
                        fields = ", type_pay,month_from,month_to";
                    }



                    sql.Remove(0, sql.Length);
                    sql.Append("INSERT into " + baseName + tableDelimiter + "pack_ls(nzp_pack, prefix_ls, pkod, num_ls, g_sum_ls, geton_ls,");
                    sql.Append("sum_ls, dat_month, kod_sum, paysource, dat_vvod, anketa, inbasket, erc_code, unl, info_num, nzp_user" + fields + ")");
                    sql.Append("VALUES(" + pack.nzp_pack + "," + pkodPrefix + ", " + pkod + ",");
                    sql.Append(listLs[i].num_ls + ",");
                    sql.Append(listLs[i].g_sum_ls + ",0,0, " + MDY(d2.Month, d2.Day, d2.Year) + ",33,1,");
                    sql.Append(MDY(d3.Month, d3.Day, d3.Year) + ",'" + listLs[i].bank + "',");
                    sql.Append(listLs[i].inbasket > 0 ? "1," : "0,");
                    sql.Append("'" + listLs[i].erc_code + "', -1," + (i + 1) + ", " + listLs[i].nzp_user);
                    if (fields.Length > 0)
                        sql.Append("," + listLs[i].type_pay + "," + month_from + "," + month_to);
                    sql.Append(")");

                    ret = ExecSQL(connDB, sql.ToString(), true);
                    ret.tag = DBManager._affectedRowsCount;
                    if (!ret.result)
                    {
                        ret.text = "Ошибка загрузки ЛС, см логи ";
                        MonitorLog.WriteLog("Ошибка загрузки ЛС " + (Constants.Viewerror ? "\n" +
                        sql + "(" + ret.sql_error + ")" : ""),
                        MonitorLog.typelog.Error, 20, 201, true);
                        return -1;
                    }

                    listLs[i].nzp_pack_ls = DBManager.GetSerialValue(connDB);


                    if (listLs[i].inbasket != 0) // Добавляем в корзину
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append("INSERT INTO " + baseName + DBManager.tableDelimiter + "pack_ls_err");
                        sql.Append("(nzp_pack_ls, nzp_err, nzp_serv, note) ");
                        sql.Append("VALUES(" + listLs[i].nzp_pack_ls + ", 666,0, ");
                        sql.Append("'Не определен лс по пкоду " + listLs[i].pkod + "')");
                        ret = ExecSQL(connDB, sql.ToString(), true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка загрузки ЛС, см логи ";
                            MonitorLog.WriteLog("Ошибка загрузки ЛС " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                            return -1;
                        }
                    }


                }
                #endregion
            }
            catch
            {
                ret.text = "Неожиданная ошибка сохранения пачки, см логи ";
                ret.result = false;
                MonitorLog.WriteLog("Неожиданная ошибка сохранения пачки " + (Constants.Viewerror ? "\n" +
                           sql : ""),
                           MonitorLog.typelog.Error, 20, 201, true);
                return -1;
            }

            return pack.nzp_pack;
        }


        /// <summary>
        ///  Сохранение суперпачки
        /// </summary>
        /// <param name="listPack">Список подпачек суперпачки</param>
        /// <param name="connDB"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool SaveSuperPack(List<Pack> listPack, IDbConnection connDB, out Returns ret, IDbTransaction transaction = null)
        {
            ret = Utils.InitReturns();
            var sql = new StringBuilder();
            string baseName = "";
            var superpack = new Pack();
            try
            {
                for (int i = 0; i <= listPack.Count - 1; i++)
                {
                    baseName = Points.Pref + "_fin_" + (listPack[i].year_ % 100).ToString("00");
                    superpack.sum_pack = superpack.sum_pack + listPack[i].sum_pack;
                    superpack.dat_pack = listPack[i].dat_pack;
                    superpack.erc_code = listPack[i].erc_code;
                    superpack.file_name = listPack[i].file_name;
                }
                superpack.count_kv = listPack.Count;
                superpack.nzp_bank = 1000;
                superpack.nzp_pack = 0;
                superpack.num = "1";

                if (superpack.count_kv > 0)
                {
                    #region Сохранение пачки
                    MyDataReader reader;
                    sql.Remove(0, sql.Length);
                    sql.Append(" select num_pack from " + baseName + DBManager.tableDelimiter + "pack ");
                    sql.Append(" where nzp_pack=par_pack ");
                    ret = ExecRead(connDB, transaction, out reader, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка сохранения суперпачки, см. логи ";
                        MonitorLog.WriteLog("Ошибка сохранения суперпачки " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return false;
                    }
                    if (reader.Read())
                    {
                        if (reader["num_pack"] != DBNull.Value)
                            superpack.num = (Convert.ToInt32(reader["num_pack"]) + 1).ToString(CultureInfo.InvariantCulture);
                    }
                    reader.Close();

                    sql.Remove(0, sql.Length);
                    sql.Append("INSERT into " + baseName + DBManager.tableDelimiter + "pack(pack_type, nzp_bank, ");
                    sql.Append(" num_pack,dat_pack, dat_uchet, count_kv, sum_pack, flag, erc_code, file_name)");
                    sql.Append("VALUES(10, " + superpack.nzp_bank + "," + superpack.num + ",'");
                    sql.Append(superpack.dat_pack + "','" + Points.DateOper.ToShortDateString() + "'," + superpack.count_kv + ",");
                    sql.Append(superpack.sum_pack + ",11,'" + superpack.erc_code + "','");
                    sql.Append(superpack.file_name + "')");

                    ret = ExecSQL(connDB, transaction, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка загрузки пачки, см. логи ";
                        MonitorLog.WriteLog("Ошибка загрузки пачки " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return false;
                    }

                    ret.tag = superpack.nzp_pack = DBManager.GetSerialValue(connDB);
                    #endregion
                }

                for (int i = 0; i <= listPack.Count - 1; i++)
                {
                    baseName = Points.Pref + "_fin_" + (listPack[i].year_ % 100).ToString("00");
                    sql.Remove(0, sql.Length);
                    sql.Append("update " + baseName + DBManager.tableDelimiter + "pack " +
                               "set par_pack = " + superpack.nzp_pack + " where nzp_pack = ");
                    sql.Append(listPack[i].nzp_pack + " or nzp_pack = " + superpack.nzp_pack);
                    ret = ExecSQL(connDB, transaction, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка сохранения суперпачки, см. логи ";
                        MonitorLog.WriteLog("Ошибка сохранения суперпачки " + (Constants.Viewerror ? "\n" +
                            sql + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return false;
                    }
                }


            }
            catch
            {
                ret.result = false;
                ret.tag = 0;
                ret.text = "Неожиданная ошибка сохранения суперпачки, см. логи ";
                MonitorLog.WriteLog("Неожиданная ошибка сохранения суперпачки " + (Constants.Viewerror ? "\n" +
                           sql : ""),
                           MonitorLog.typelog.Error, 20, 201, true);
            }
            return true;
        }

        /// <summary>
        /// Проверка существования пачки в ходе загрузки
        /// </summary>
        /// <param name="pack">Пачка Pack</param>
        /// <param name="connDB">Подключение к финансовой базе</param>
        /// <returns></returns>
        private bool IsPackExists(Pack pack, IDbConnection connDB, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {

                string baseName = Points.Pref + "_fin_" +
                                  (pack.year_ % 100).ToString("00") + DBManager.tableDelimiter;

                string sql = "select count(*) as co from " + baseName + "pack where file_name = '" +
                             pack.file_name + "'  and dat_pack = '" + pack.dat_pack + "'" +
                             " and num_pack = " + Utils.EStrNull(pack.num, "") +
                             " and nzp_bank = " + pack.nzp_bank + " and sum_pack = " + pack.sum_pack;
                MyDataReader reader;
                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка проверки существования пачки, см. логи";
                    return false;
                }
                try
                {
                    if (reader.Read())
                        if (reader["co"] != DBNull.Value)
                        {
                            return Convert.ToInt32(reader["co"]) == 1;
                        }
                }
                finally
                {
                    reader.Close();
                }
                return false;
            }
            catch
            {
                ret.result = false;
                ret.text = "Ошибка поиска пачки " + pack.num;
                MonitorLog.WriteLog("Неожиданная ошибка поиска пачки " + pack.num,
                    MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
        }

        private string IsHasNewPlaceofMade(List<Pack_ls> dt, IDbConnection connDB)
        {
            string baseName = Points.Pref + "_kernel";
            string newBank = "";
            try
            {
                for (int i = 0; i < dt.Count - 1; i++)
                {

                    string sql = "select count(*) as co from " + baseName + DBManager.tableDelimiter + "s_bank    " +
                                 " where Upper(short_name) = Upper('" + dt[i].bank + "')";
                    MyDataReader reader;
                    Returns ret = ExecRead(connDB, out reader, sql, true);
                    if (!ret.result) continue;
                    try
                    {
                        if (reader.Read())
                            if (reader["co"] != DBNull.Value)
                                if (Convert.ToInt32(reader["co"]) == 0)
                                {
                                    if (newBank.IndexOf(dt[i].bank, StringComparison.Ordinal) < 0)
                                    {
                                        newBank = newBank + ", " + dt[i].bank;
                                    }
                                }

                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }
            catch
            {
                MonitorLog.WriteLog("Неожиданная ошибка поиска мест формирования IsHasNewPlaceofMade",
                MonitorLog.typelog.Error, 20, 201, true);
            }
            return newBank;
        }

        /// <summary>
        /// Получение кода банка места формирования
        /// </summary>
        /// <param name="namePlace">Текстовое имя места формирования</param>
        /// <param name="connDB">Подключение к финансовой базе</param>
        /// <returns></returns>
        private int GetPlaceofMade(string namePlace, IDbConnection connDB)
        {
            string baseName = Points.Pref + "_kernel";

            try
            {
                string sql = "select nzp_bank from " + baseName + DBManager.tableDelimiter +
                             "s_bank where Upper(short_name)='" + namePlace.ToUpper() + "' or " +
                             " Upper(bank)='" + namePlace.ToUpper() + "'";
                MyDataReader reader;
                Returns ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result) return 0;
                try
                {
                    if (reader.Read())
                        return reader["nzp_bank"] != DBNull.Value ? Convert.ToInt32(reader["nzp_bank"]) : 0;
                }
                finally
                {
                    reader.Close();
                }
                return 0;
            }
            catch
            {
                MonitorLog.WriteLog("Неожиданная ошибка поиска мест формирования GetPlaceofMade " + namePlace,
                MonitorLog.typelog.Error, 20, 201, true);
                return 0;
            }
        }

        /// <summary>
        /// Загрузка пачки  полученной из DBF
        /// </summary>
        /// <param name="nzpUser"></param>
        /// <param name="fileName">имя файла</param>
        /// <returns></returns>
        public Returns UploadPackFromDBF(string nzpUser, string fileName, out AddedPacksInfo packsInfo)
        {
            packsInfo = new AddedPacksInfo();
            var connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            DataTable dt;
            try
            {
                if (!ret.result) return new Returns(false, "Не коннекта к базе данных Webdata", -3);
                dt = DBManager.ExecSQLToTable(connWeb, "select * from t" + nzpUser + "_dbfpack_ls order by ind, nplp, pldat");
            }
            finally
            {
                connWeb.Close();
            }
            return DecodeDBF(dt, fileName, ref packsInfo);
        }





        /// <summary>
        /// Загрузка пачки  полученной из DBF
        /// </summary>
        /// <param name="dt">Список ЛС</param>
        /// <param name="fileName">имя файла</param>
        /// <returns></returns>
        public Returns UploadPackFromList(List<Pack_ls> dt, string fileName, ref AddedPacksInfo packsInfo)
        {
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result) return new Returns(false, "Не коннекта к базе данных " +
                Points.Pref, -3);

            int oldPlat = -1;
            int countBad = 0;
            string skipPack = "";
            var superPack = new Pack();
            Pack pack = null;
            var listPack = new List<Pack>();
            var listLs = new List<Pack_ls>();
            try
            {
                string newBank = IsHasNewPlaceofMade(dt, connDB);
                if (newBank != "")
                {
                    connDB.Close();
                    return new Returns(false, "Неизвестное(ые) места формирования электронной пачки " +
                                              newBank, -4);
                }
                for (int i = 0; i < dt.Count; i++)
                {
                    if (oldPlat.ToString(CultureInfo.InvariantCulture) != dt[i].num)
                    {
                        #region Новая пачка
                        if (pack != null)
                        {
                            if (pack.count_kv != 0)
                            {
                                // проверка пачки на существование
                                bool packexists = IsPackExists(pack, connDB, out ret);
                                if (!ret.result)
                                {
                                    return ret;
                                }
                                if (!packexists)
                                {
                                    pack.nzp_pack = SavePack(pack, listLs, connDB, out ret);
                                    packsInfo.InsertedCountRows += ret.tag;
                                    if (!ret.result)
                                    {
                                        return ret;
                                    }
                                    listPack.Add(pack);
                                    if (pack.nzp_pack < 0)
                                    {
                                        DeleteBadPack(listPack, connDB);
                                        connDB.Close();
                                        return new Returns(false, "Пачка не может быть сохранена в базе " + pack.num +
                                                                  newBank, -4);
                                    }
                                }
                                else
                                {
                                    //packsInfo.AddWarnMsg("Пачка " + pack.num + " от " + pack.bank + " за " +
                                    //                     pack.dat_pack.Substring(0, 10) + " уже была загружена и будет пропущена при загрузке");            
                                    //Пачка пропущена
                                    skipPack = skipPack + "№" + pack.num + " на  сумму " + pack.sum_pack +
                                               " руб. <br> ";
                                }
                                superPack.count_kv = superPack.count_kv + pack.count_kv;
                                superPack.sum_pack = superPack.sum_pack + pack.sum_pack;
                            }
                        }

                        listLs.Clear();
                        pack = new Pack
                        {
                            year_ = Points.DateOper.Year,
                            count_kv = 0,
                            sum_pack = 0,
                            erc_code = dt[i].erc_code,
                            dat_pack = dt[i].dat_pack,
                            bank = dt[i].bank,
                            num = dt[i].num,
                            file_name = dt[i].file_name
                        };
                        pack.nzp_bank = GetPlaceofMade(pack.bank, connDB);
                        if (!Int32.TryParse(pack.num, out oldPlat))
                        {
                            MonitorLog.WriteLog("Ошибка преобразования  в тип int переменной pack.num", MonitorLog.typelog.Error, 20, 201, true);
                        }
                        #endregion
                    }

                    var packLs = new Pack_ls
                    {
                        info_num = i + 1,
                        kod_sum = 33,
                        dat_vvod = dt[i].dat_vvod,
                        pm_note = dt[i].pm_note,
                        g_sum_ls = dt[i].g_sum_ls,
                        bank = dt[i].bank
                    };

                    if (pack != null)
                    {
                        pack.sum_pack = pack.sum_pack + packLs.g_sum_ls;
                        pack.count_kv++;
                    }

                    #region Номер лицевого счета

                    packLs.num_ls = 0;
                    packLs.pkod = dt[i].pkod;

                    long j;
                    if (Int64.TryParse(dt[i].pkod, out j))
                    {

                        string sql = " select num_ls from " +
                                     Points.Pref + DBManager.sDataAliasRest + "kvar where pkod=" + dt[i].pkod +
                                     " union  " +
                                     " select num_ls from " + Points.Pref + DBManager.sDataAliasRest + "kvar_pkodes a, " +
                                     Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                                     " where a.pkod = " + dt[i].pkod + " and a.nzp_kvar=k.nzp_kvar "
                            //+
                            //" union  " +
                            //" select num_ls from " + Points.Pref + DBManager.sDataAliasRest + "supplier_codes a, " +
                            //Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                            //" where a.pkod_supp = " + dt[i].pkod + " and a.nzp_kvar=k.nzp_kvar "
                                     ;

                        MyDataReader reader;
                        ret = ExecRead(connDB, out reader, sql, true);
                        if (ret.result)
                        {
                            if (reader.Read())
                            {
                                if (reader["num_ls"] != DBNull.Value)
                                    packLs.num_ls = Convert.ToInt32(reader["num_ls"]);
                                reader.Close();
                            }
                            else
                            {
                                packLs.inbasket = 666; //ошибка pkod
                                countBad++;
                            }
                        }

                    }
                    else
                    {
                        packLs.inbasket = 666; //ошибка pkod
                        countBad++;
                    }

                    #endregion

                    packLs.dat_month = dt[i].dat_month;
                    listLs.Add(packLs);
                }

                if (pack != null && pack.count_kv != 0)
                {
                    bool packexists = IsPackExists(pack, connDB, out ret);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    if (!packexists)
                    {
                        pack.nzp_pack = SavePack(pack, listLs, connDB, out ret);
                        packsInfo.InsertedCountRows += ret.tag;
                        if (!ret.result)
                        {
                            return ret;
                        }
                        listPack.Add(pack);
                        if (pack.nzp_pack < 0)
                        {
                            DeleteBadPack(listPack, connDB);
                            connDB.Close();
                            return new Returns(false, "Пачка не может быть сохранена в базе " + pack.num +
                                                      newBank, -4);
                        }
                    }
                    else
                    {
                        //packsInfo.AddWarnMsg("Пачка " + pack.num + " от " + pack.bank + " за " +
                        //                                pack.dat_pack.Substring(0, 10) + " уже была загружена и будет пропущена при загрузке");
                        //Пачка пропущена
                        skipPack = skipPack + "№" + pack.num + " на  сумму " + pack.sum_pack + " руб. <br> ";
                    }
                    superPack.count_kv = superPack.count_kv + pack.count_kv;
                    superPack.sum_pack = superPack.sum_pack + pack.sum_pack;
                }

                ret = SetGoodLoadPack(listPack, connDB);
                if (!ret.result)
                {
                    packsInfo.AddErrorMsg(ret.text);
                }
                SaveSuperPack(listPack, connDB, out ret);
                if (!ret.result)
                {
                    packsInfo.AddErrorMsg(ret.text);
                }
                packsInfo.InsertedNzpPack = ret.tag;
            }
            finally
            {
                connDB.Close();
            }
            try
            {
                foreach (Pack t in listPack)
                {
                    // Отдаем пачку на распределение
                    var db2 = new DbCalcPack();
                    db2.PackFonTasks(t.nzp_pack, t.year_, CalcFonTask.Types.DistributePack, out ret);
                    db2.Close();
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Неожиданная ошибка PackFonTasks" + ex.Message,
                    MonitorLog.typelog.Error, 20, 201, true);

            }
            if (skipPack != "")
            {
                packsInfo.AddWarnMsg("<b>Загрузка завершена</b> " + "<br><b>" +
                superPack.count_kv + "</b> платежей <br><b>" +
                superPack.sum_pack.ToString("0.00") + "</b> рублей, <br><b>" +
                countBad + "</b> платежей в корзине <br>  " +
                " <b>Следующие пачки были загружены ранее и в текущей загрузке проигнорированы</b> <br> " + skipPack);
                return new Returns(true, " ", 1);

            }
            return new Returns(true, "<b>Загрузка завершена</b> " + "<br> <b>" +
                                     superPack.count_kv + "<b> платежей <br> <b>" +
                                     superPack.sum_pack.ToString("0.00") + "</b> рублей, <br><b>" +
                                     countBad + "</b> платежей в корзине ", 1);
        }

        public Returns DecodeDBF(DataTable dt, string fileName, ref AddedPacksInfo packsInfo)
        {
            packsInfo = new AddedPacksInfo();
            #region Проверка формата таблицы
            if (dt.Rows.Count == 0) return new Returns(false, "Пустой файл");


            if (dt.Columns.Contains("PREDPR") &
               dt.Columns.Contains("PLDAT") &
               dt.Columns.Contains("NPLP") &
               dt.Columns.Contains("KODLS") &
               dt.Columns.Contains("GEU") &
               dt.Columns.Contains("DT") &
               dt.Columns.Contains("PLATA") &
               dt.Columns.Contains("MES_OPL"))
            {
            }
            else return new Returns(false, "Файл не правильной структуры", -2);
            #endregion

            var listLs = new List<Pack_ls>();
            DateTimeFormatInfo dfi = new CultureInfo("ru-RU", false).DateTimeFormat;
            NumberFormatInfo nfi = new CultureInfo("ru-RU", false).NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            nfi.CurrencyDecimalSeparator = ".";

            var dv = new DataView(dt) { Sort = "IND ASC, NPLP" };


            //Добавить сортировка в DT
            for (int i = 0; i <= dv.Count - 1; i++)
            {
                if ((dv[i]["DT"] != DBNull.Value) &
                    (dv[i]["IND"] != DBNull.Value) &

                    (dv[i]["PLATA"] != DBNull.Value) &
                    (dv[i]["PREDPR"] != DBNull.Value) &
                    (dv[i]["GEU"] != DBNull.Value) &
                    (dv[i]["KOD"] != DBNull.Value))
                {


                    var packLs = new Pack_ls
                    {
                        info_num = i + 1,
                        kod_sum = 33,
                        dat_vvod = Convert.ToDateTime(dv[i]["DT"]).ToShortDateString(),
                        pm_note = dv[i]["IND"].ToString().Trim(),
                        bank = dv[i]["IND"].ToString().Trim(),
                        num = dv[i]["NPLP"] == DBNull.Value ? "0" : dv[i]["NPLP"].ToString().Trim(),
                        dat_pack = Convert.ToDateTime(dv[i]["PLDAT"]).ToShortDateString(),
                        file_name = fileName,
                        erc_code = "630100000015"
                    };
                    Decimal dl;
                    if (Decimal.TryParse(dv[i]["PLATA"].ToString(), out dl))
                    {
                        packLs.g_sum_ls = dl;

                    }
                    else
                    {
                        packLs.g_sum_ls = 0;
                        packLs.pm_note = "Ошибка суммы " + dv[i]["PLATA"];
                        packsInfo.AddErrorMsg("Ошибка суммы " + dv[i]["PLATA"]);
                    }


                    #region Номер лицевого счета
                    packLs.pkod = dv[i]["PREDPR"].ToString().Trim() +
                                  dv[i]["GEU"].ToString().Trim() +
                                  dv[i]["KOD"].ToString().Trim();
                    if (dv[i]["KODLS"].ToString().Trim() != "")
                    {
                        packLs.pkod = packLs.pkod + dv[i]["KODLS"].ToString().Trim();
                    }
                    else
                    {
                        packLs.pkod = packLs.pkod + "0";
                    }


                    packLs.pkod = packLs.pkod + Utils.GetKontrSamara(packLs.pkod);
                    #endregion

                    #region Месяц оплаты
                    packLs.dat_month = DateTime.Today.AddDays(1 - DateTime.Today.Day).AddMonths(-1).ToString("dd.MM.yyyy");
                    if (dv[i]["MES_OPL"].ToString() != "")
                    {
                        try
                        {
                            packLs.dat_month = DateTime.ParseExact(dv[i]["MES_OPL"].ToString(),
                                "MMyy", dfi).ToString("dd.MM.yyyy");
                        }
                        catch (Exception ex)
                        {
                            packLs.dat_month =
                                DateTime.Today.AddDays(1 - DateTime.Today.Day).AddMonths(-1).ToString("dd.MM.yyyy");
                            string err = "Ошибка преобразования даты месяца оплаты " +
                                         Environment.NewLine +
                                         ex.Message +
                                         Environment.NewLine +
                                         "значение " + packLs.dat_month + " ожидаемое значение в формате ММГГ " +
                                         " по умолчанию взято значение " + packLs.dat_month;
                            packsInfo.AddErrorMsg(err);
                            MonitorLog.WriteLog(err,
                                MonitorLog.typelog.Error, 20, 201, true);
                        }
                    }
                    #endregion

                    listLs.Add(packLs);
                }
            }

            if (listLs.Count > 0)
            {
                return UploadPackFromList(listLs, fileName, ref packsInfo);
            }

            return new Returns(true, "Пустой файл", -2);
        }


        /// <summary>
        /// Функция добавляет место формирования электронной пачки
        /// </summary>
        /// <param name="connDB">Соединение с основной базой данных</param>
        /// <param name="bank">Место формирования</param>
        /// <returns>код места формирования</returns>
        public int SaveNewPlaceOfMade(IDbConnection connDB, string bank)
        {
            int nzpBank = 0;
            string sql = " insert into " + Points.Pref + DBManager.sKernelAliasRest +
                         "s_bank( bank, short_name, adress, phone, nzp_payer, nzp_geu) " +
                         " values ( '" + bank + "', null, null, null, null, null)";
            Returns ret = ExecSQL(connDB, sql, true);
            if (!ret.result)
            {

                MonitorLog.WriteLog("Ошибка сохранения места формирования пачки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                return nzpBank;
            }
            nzpBank = DBManager.GetSerialValue(connDB);
            return nzpBank;

        }

        public Returns SaveUniversalPackLs(IDbConnection connDB, string basefin, Pack_ls packLS)
        {
            Returns ret = Utils.InitReturns();

            var sql = new StringBuilder();

            #region Сохранение заголовка ЛС

            string field = "";
            if (packLS.nzp_supp > 0) //либо nzp_supp, либо nzp_payer
            {
                field = packLS.kod_sum == 49 ? ",nzp_payer" : ",nzp_supp"; //kod_sum=49 - оплаты от контрагентов
            }
            sql.Remove(0, sql.Length);
            sql.Append(" insert into " + basefin + DBManager.tableDelimiter + "pack_ls (nzp_pack, pkod, num_ls,  sum_ls, ");
            sql.Append(" g_sum_ls, kod_sum, paysource,  dat_vvod, inbasket, dat_month, ");
            sql.Append(" id_bill, info_num,   erc_code, geton_ls, sum_peni, prefix_ls, anketa" + field + ", old_num_ls)");
            sql.Append(" Values( " + packLS.nzp_pack + ",");
            sql.Append(" " + packLS.pkod + ", ");
            sql.Append(" " + packLS.num_ls + ",");
            sql.Append(" " + packLS.sum_ls + ", ");
            sql.Append(" " + packLS.g_sum_ls + ", ");
            sql.Append(" " + packLS.kod_sum + ", ");
            sql.Append(" " + packLS.paysource + ",  ");
            sql.Append("'" + packLS.dat_vvod.Substring(0, 10) + "', ");
            sql.Append(" " + packLS.inbasket + " , ");
            sql.Append("'" + packLS.dat_month.Substring(0, 10) + "', ");
            sql.Append(" " + packLS.id_bill + ", ");
            sql.Append(" " + packLS.info_num + ", ");
            sql.Append("'" + packLS.erc_code + "',0,0,0,''");
            if (packLS.nzp_supp > 0)
            {
                sql.Append(", " + packLS.nzp_supp + " ");
            }
            sql.Append(", '" + packLS.old_num_ls + "')");
            if (!ExecSQL(connDB, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения места платежа по ЛС";
                MonitorLog.WriteLog("Ошибка сохранения места платежа по ЛС" + sql, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return ret;
            }

            packLS.nzp_pack_ls = DBManager.GetSerialValue(connDB);


            #endregion

            #region Сохранение изменений жильца по оплатам
            if (packLS.gilSums != null)
            {
                string nzp_supp_field = "", nzp_supp = "";




                foreach (GilSum t in packLS.gilSums)
                {
                    if (packLS.version_pack == "!1.02")
                    {
                        nzp_supp_field = "nzp_supp,";
                        nzp_supp = " " + t.nzp_supp + ", ";
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into " + basefin + DBManager.tableDelimiter +
                               "gil_sums (nzp_pack_ls, nzp_serv, " + nzp_supp_field + " num_ls,  days_nedo, ");
                    sql.Append(" sum_oplat, dat_month, ordering)");
                    sql.Append(" Values( " + packLS.nzp_pack_ls + ",");
                    sql.Append(" " + t.nzp_serv + ",");
                    sql.Append(nzp_supp);
                    sql.Append(" " + packLS.num_ls + ",");
                    sql.Append((String.IsNullOrEmpty(t.day_nedo) ? "null" : "'" + t.day_nedo + "'") + ", ");
                    sql.Append((String.IsNullOrEmpty(t.sum_oplat) ? "null" : "'" + t.sum_oplat + "'") + ", ");
                    sql.Append("'" + packLS.dat_month.Substring(0, 10) + "', ");
                    sql.Append(" " + t.ordering + ")");
                    if (!ExecSQL(connDB, sql.ToString(), true).result)
                    {
                        ret.text = "Ошибка сохранения изменений по оплатам";
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка сохранения изменений по оплатам " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
            }
            #endregion

            #region Сохранение показаний счетчиков
            if (packLS.puVals != null)
            {
                // pu.dat_month - месяца оплаты счета, но в pu_vals ТЕПЕРЬ должен
                //лежать месяц учета показаний, поэтому увеличиваем на 2 месяца
                //поменять как в реестре
                var localDate = Points.GetCalcMonth(new CalcMonthParams(packLS.pref));
                if (localDate.month_ == 0 || localDate.year_ == 0) localDate = Points.CalcMonth;

                packLS.dat_month = new DateTime(localDate.year_, localDate.month_, 1).AddMonths(1)
                    .ToShortDateString();
                foreach (PuVals t in packLS.puVals)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into " + basefin + DBManager.tableDelimiter +
                               "pu_vals (nzp_pack_ls, nzp_serv, num_ls,  num_cnt, ");
                    sql.Append(" val_cnt, dat_month, nzp_counter, pu_order, cur_unl)");
                    sql.Append(" Values( " + packLS.nzp_pack_ls + ",");
                    sql.Append(" " + t.nzp_serv + ",");
                    sql.Append(" " + packLS.num_ls + ",");
                    sql.Append("'" + t.num_cnt + "', ");
                    sql.Append("'" + t.val_cnt + "', ");
                    sql.Append("'" + packLS.dat_month + "', ");
                    sql.Append(" " + t.nzp_counter + " , ");
                    sql.Append(" " + t.ordering + ", -1)");
                    if (!ExecSQL(connDB, sql.ToString(), true).result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка сохранения показаний счетчиков";
                        MonitorLog.WriteLog("Ошибка сохранения показаний счетчиков " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }

                }
            }
            #endregion

            return ret;
        }

        public Returns SaveUniversalPack(IDbConnection connDB, string basefin, Pack pack, ref AddedPacksInfo packsInfo)
        {
            Returns ret = Utils.InitReturns();
            MyDataReader reader;
            #region Определение места формирования электронной пачки
            pack.nzp_bank = GetPlaceofMade(pack.bank, connDB);
            if (pack.nzp_bank == 0)
            {
                pack.nzp_bank = SaveNewPlaceOfMade(connDB, pack.bank);
                if (pack.nzp_bank == 0)
                {
                    //исключительная ситуация не удалось добавить место формирования
                    ret.result = false;
                    ret.text = "Не удалось добавить новое место формирования";
                    return ret;
                }
            }
            #endregion

            #region Проверка, что такая пачка уже была загружена
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" select count(*) as co from  " + basefin + DBManager.tableDelimiter + "pack ");
            sql.Append(" where nzp_bank= " + pack.nzp_bank);
            sql.Append(" and num_pack='" + pack.num + "'");
            sql.Append(pack.is_super_pack ? " and par_pack=nzp_pack " : " and par_pack<>nzp_pack ");
            sql.Append(" and dat_pack = '" + pack.dat_pack.Substring(0, 10) + "'");
            if (!ExecRead(connDB, out reader, sql.ToString(), true).result)
            {
                ret.result = false;
                return ret;
            }
            if (reader.Read())
            {
                if (reader["co"] != DBNull.Value)
                    if (Convert.ToInt32(reader["co"]) > 0)
                    {
                        string warnmsg = "Пачка " + pack.num + " от " + pack.bank + " за " +
                                         pack.dat_pack.Substring(0, 10) + " уже была загружена и будет пропущена при загрузке";
                        packsInfo.AddWarnMsg(warnmsg);
                        ret.tag = -1;
                        ret.result = true;
                        return ret;
                    }
            }
            reader.Close();
            #endregion

            if (pack.listPackLs.Count > 0)
                ret = GetPackType(connDB, pack, ret);

          

            #region Сохранение подпачек, если они есть
            // перечень результатов проверки на повторную загрузку пачек
            List<int> resDoublePack = new List<int>();
            if (pack.listPack != null)
                if (pack.listPack.Count > 0)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("update " + basefin + DBManager.tableDelimiter + "pack " +
                               "set par_pack = nzp_pack where nzp_pack =" + pack.nzp_pack);
                    if (!ExecSQL(connDB, sql.ToString(), true).result)
                    {
                        ret.result = false;
                        ret.text = " Ошибка получения установки родительской пачки для пачки" +
                            pack.nzp_pack;
                        return ret;
                    }
                   
                    foreach (Pack t in pack.listPack)
                    {
                        t.par_pack = pack.nzp_pack;
                        t.version_pack = pack.version_pack;
                        ret = SaveUniversalPack(connDB, basefin, t, ref packsInfo);
                        if (!ret.result)
                        {
                           return ret;
                        }
                        // если ret.tag==-1, значит пачка загружается повторно
                        resDoublePack.Add(ret.tag);
                    }
                }
            // Если есть хотя бы одна пачка, которая загружается не повторно
            if (resDoublePack.Count > 0 && resDoublePack.Count(res=>res==0)==0)
            {
                return ret;
            }
            #endregion

            string field = "";
            if (pack.nzp_supp > 0) //либо nzp_supp, либо nzp_payer
            {
                if (pack.listPackLs.Count > 0)
                    field = pack.listPackLs[0].kod_sum == 49 ? "nzp_payer," : "nzp_supp,"; //kod_sum=49 - оплаты от контрагентов
            }

            #region Сохранение заголовка пачки
            sql.Remove(0, sql.Length);
            sql.Append(" insert into " + basefin + DBManager.tableDelimiter + "pack (par_pack, " +
                       "pack_type, nzp_bank, num_pack, ");
            sql.Append("  dat_pack, dat_uchet, count_kv, sum_pack,  real_count, flag, dat_vvod, ");
            sql.Append(" operday_payer, peni_pack,   erc_code, " + field + " file_name)");
            sql.Append(" Values( " + pack.par_pack + ",");
            sql.Append(" " + pack.pack_type + " , ");
            sql.Append(" " + pack.nzp_bank + ",");
            sql.Append(" '" + pack.num + "', ");
            sql.Append(" '" + pack.dat_pack.Substring(0, 10) + "', ");
            sql.Append(" '" + Points.DateOper.ToShortDateString() + "', ");
            sql.Append(" " + pack.count_kv + ", ");
            sql.Append(" " + pack.sum_pack + ",  ");
            sql.Append(" " + pack.count_kv + ", ");
            sql.Append(" 11 , ");
            sql.Append(" '" + pack.dat_pack.Substring(0, 10) + "', ");
            sql.Append(" '" + pack.dat_calc.Substring(0, 10) + "', ");
            sql.Append(" 0, ");
            sql.Append("'" + pack.erc_code + "', ");
            if (pack.nzp_supp > 0)
                sql.Append("'" + pack.nzp_supp + "', ");
            sql.Append("'" + pack.file_name + "')");

            if (!ExecSQL(connDB, sql.ToString(), true).result)
            {
                ret.text = "Ошибка при сохранение заголовка пачки";
                ret.result = false;
                return ret;
            }


            pack.nzp_pack = DBManager.GetSerialValue(connDB);
            #endregion

            #region Сохранение ЛС в пачке
            if (pack.listPackLs != null)
            {
                if (pack.listPackLs.Count > 0)
                {
                    packsInfo.InitOplatyProgress(pack.listPackLs.Count);
                }
                foreach (Pack_ls t in pack.listPackLs)
                {
                    t.nzp_pack = pack.nzp_pack;
                    t.version_pack = pack.version_pack;
                    ret = SaveUniversalPackLs(connDB, basefin, t);
                    packsInfo.OnSetProgress();
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }
            #endregion
            reader.Close();
            return ret;
        }

        private Returns GetPackType(IDbConnection connDB, Pack pack, Returns ret)
        {
            string sqlStr =
                " SELECT is_erc " +
                " FROM " + Points.Pref + sKernelAliasRest + "kodsum" +
                " WHERE kod = " + pack.listPackLs[0].kod_sum;
            var is_erc = ExecScalar(connDB, sqlStr, out ret, true);
            int kod_is_erc;
            if (is_erc != null && Int32.TryParse(is_erc.ToString(), out kod_is_erc))
            {
                pack.pack_type = (kod_is_erc == 1 ? 10 : 20);
            }
            else
            {
                //исключительная ситуация не удалось определить тип пачки по коду оплаты
                ret.result = false;
                ret.text = "Не удалось определить тип пачки по коду оплаты " + pack.listPackLs[0].kod_sum;
                return ret;
            }
            return ret;
        }


        /// <summary>
        /// Загрузка ПУ из таблице counters_ord
        /// </summary>
        /// <param name="connDB">Подключение к основной базе данных</param>
        /// <param name="numLS">Лицевой счет</param>
        /// <param name="countersTable">Таблица счетчиков</param>
        /// <param name="datMonth"></param>
        /// <param name="pu">Объект показания ПУ</param>
        /// <returns></returns>
        public Returns GetPUByOrdering(IDbConnection connDB, Pack_ls packLS, PuVals pu, ref AddedPacksInfo packsInfo)
        {
            Returns ret = Utils.InitReturns();
            MyDataReader readerTemp = null;
            DateTime dat_month = Points.GetCalcMonth(new CalcMonthParams(packLS.pref)).RecordDateTime;
            string countersTable = packLS.pref + "_charge_" + (dat_month.Year - 2000) +
                                              DBManager.tableDelimiter + "counters_ord";
            try
            {
                #region Стандартная схема через order_print и dat_month
                string sql = "select * " +
                             " from " + countersTable +
                             " where order_num = " + pu.ordering +
                             " and dat_month='" + dat_month.ToShortDateString() + "'" +
                             " and num_ls = " + packLS.num_ls + " order by nzp_serv ";
                ret = ExecRead(connDB, out readerTemp, sql, true);
                if (!ret.result)
                {
                    packsInfo.AddErrorMsg("Ошибка выборки значения ПУ для ЛС " + packLS.num_ls, packLS.pref);
                    string msgToLog = "Ошибка выборки значения ПУ из counter_ord для ЛС " + packLS.num_ls + ", порядковый номер: " + pu.ordering + " (counters_ord)";
                    MonitorLog.WriteLog(msgToLog + sql, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

                while (readerTemp.Read())
                {
                    if (pu.nzp_serv != 0) continue;
                    if (readerTemp["nzp_serv"] != DBNull.Value)
                    {
                        pu.nzp_serv = Convert.ToInt32(readerTemp["nzp_serv"].ToString().Trim());
                        pu.nzp_counter = Convert.ToInt32(readerTemp["nzp_counter"].ToString().Trim());
                        pu.num_cnt = readerTemp["num_cnt"].ToString().Trim();
                        pu.dat_uchet = readerTemp["dat_uchet"].ToString().Trim();

                        CheckPrevPUVal(connDB, packLS, ref pu, packsInfo, dat_month, countersTable);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                ret.result = false;
                string msg = "Ошибка определения порядкого номера услуг в изменениях " + packLS.num_ls;
                packsInfo.AddErrorMsg(msg, packLS.pref);
                MonitorLog.WriteLog(msg + " (counters_ord)" + ' ' + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (readerTemp != null)
                    readerTemp.Close();
            }
            return ret;
        }

        /// <summary>
        /// проверка предыдущего показания на предмет перехода через ноль
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="packLS"></param>
        /// <param name="pu"></param>
        /// <param name="packsInfo"></param>
        /// <param name="dat_month"></param>
        /// <param name="countersTable"></param>
        private static void CheckPrevPUVal(IDbConnection connDB, Pack_ls packLS, ref PuVals pu, AddedPacksInfo packsInfo,
            DateTime dat_month, string countersTable)
        {
            string sql;
            string dat_month_next = dat_month.AddMonths(1).ToShortDateString();
            sql = " SELECT val_cnt, dat_uchet" +
                  " FROM " + packLS.pref + DBManager.sDataAliasRest + "counters" +
                  " WHERE nzp_counter = " + pu.nzp_counter +
                  " AND is_actual <> 100 AND dat_uchet <= '" + dat_month_next + "'" +
                  " ORDER BY dat_uchet desc ";
            DataTable pu_val_dt = DBManager.ExecSQLToTable(connDB, sql);
            if (pu_val_dt.Rows.Count == 0) return;
            decimal d;
            if (Decimal.TryParse(pu_val_dt.Rows[0]["val_cnt"].ToString(), out d))
            {
                if (pu.val_cnt < d)
                {
                    string dat_uchet = pu_val_dt.Rows[0]["dat_uchet"].ToString();
                    //протставляем предыдущее показание
                    pu.val_cnt = d;
                    string msg = "Обнаружен переход через ноль для ЛС " + packLS.num_ls + ", " +
                                 " № счетчика: " + pu.num_cnt + "," +
                                 " показание из файла пачки: " + pu.val_cnt + " от " + dat_month_next +
                                 " будет загружено последнее введенное показание: " + d + " от " +
                                 (dat_uchet.Length > 10 ? dat_uchet.Remove(10) : dat_uchet);

                    packsInfo.AddWarnMsg(msg, packLS.pref);
                    MonitorLog.WriteLog(msg, MonitorLog.typelog.Error, 20, 201, true);
                }
            }
            else
            {
                string warnmsg = "Ошибка получения показания ПУ для ЛС " + packLS.num_ls;
                packsInfo.AddErrorMsg(warnmsg, packLS.pref);
                string msg = "Ошибка получения показания ПУ из таблицы " + countersTable + " для " +
                             "ЛС " + packLS.num_ls + ", " +
                             "nzp_counter " + pu.nzp_counter + ", " +
                             "номер счетчика " + pu.num_cnt + ", " +
                             "услуга № " + pu.nzp_serv + " (local_counters)";
                MonitorLog.WriteLog(msg, MonitorLog.typelog.Error, 20, 201, true);
            }
        }


        /// <summary>
        /// Получение кода услуги по ее порядковому номеру
        /// </summary>
        /// <param name="connDB">Подключение к основной базе данных</param>
        /// <param name="numLS">Лицевой счет</param>
        /// <param name="chargeTable">Таблица начислений</param>
        /// <param name="gs">Объект изменения внесенные жильцом</param>
        /// <returns></returns>
        public Returns GetNzpServByOrdering(IDbConnection connDB, int numLS, string chargeTable,
             GilSum gs)
        {

            Returns ret = Utils.InitReturns();
            try
            {
                MyDataReader readerTemp;
                #region Стандартная схема через order_print и dat_month

                ret = ExecRead(connDB, out readerTemp, "select nzp_serv, sum(sum_charge) as sum_charge " +
                    " from " + chargeTable +
                    " where order_print=" + gs.ordering +
                    " and num_ls = " + numLS + " group by nzp_serv order by nzp_serv ", true);
                if (!ret.result)
                {
                    ret.text = "Ошибка выборки начислений по ЛС";
                    MonitorLog.WriteLog(ret.text +
                        "select nzp_serv from " + chargeTable +
                        " where order_print=" + gs.ordering + " order by nzp_serv ",
                        MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

                while (readerTemp.Read())
                {
                    if (gs.nzp_serv == 0)
                    {
                        if (readerTemp["nzp_serv"] != DBNull.Value) gs.nzp_serv = Convert.ToInt32(readerTemp["nzp_serv"].ToString().Trim());
                    }

                    if (readerTemp["sum_charge"] != DBNull.Value)
                    {
                        if (Convert.ToDecimal(readerTemp["sum_charge"].ToString().Trim()) > 0.001m)
                        {
                            gs.nzp_serv = Convert.ToInt32(readerTemp["nzp_serv"].ToString().Trim());
                        }
                    }
                }
                readerTemp.Close();
                #endregion
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка определения порядкого номера услуг в изменениях " + numLS;
                MonitorLog.WriteLog("Ошибка определения порядкого номера услуг в изменениях " + numLS + ' ' +
                ex.Message,
                    MonitorLog.typelog.Error, 20, 201, true);
            }

            return ret;
        }


        /// <summary>
        /// Загрузка из веб базы данных по оплате по лицевым счетам
        /// </summary>
        /// <param name="connDB">Подключчение к основной базе данных</param>
        /// <param name="prefWeb">Алиас веб базы данных</param>
        /// <param name="pack">Пачка оплат</param>
        /// <param name="nzpSpack"></param>
        /// <returns>Результат загрузки</returns>
        public Returns GetListPackLsFromWeb(IDbConnection connDB, string prefWeb, ref Pack pack, int nzpSpack, int parPack, ref AddedPacksInfo packsInfo)
        {
            MyDataReader reader;
            MyDataReader reader2 = null;
            Returns ret;
            string msg = "";
            #region Простановка ЛС

            string sql = "";
            string temp_table = "temp_ls";
            ExecSQL(connDB, "Drop table " + temp_table, false);
            //Проставление лицевых счетов (и платежных кодов) в зависимости от типа платежного кода
            switch (typePayCode)
            {
                case TypePayCode.Standart:
                    // Старый запрос!!!! Выполнялся очень медленно
                    //sql = " update  " + prefWeb + DBManager.tableDelimiter + "source_pack_ls " +
                    //      " set num_ls = (select max(num_ls) " +
                    //      " from " + Points.Pref + DBManager.sDataAliasRest + "kvar where paycode=pkod) " +
                    //      " where nzp_spack=" + nzpSpack;
                    // С помощью временной таблицы выполняется быстрее
                    sql = "Create temp table " + temp_table + "(" +
                          "num_ls integer," +
                          "pkod numeric(13,0))";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }
                    sql = "create index ix_num_ls on " + temp_table + "(num_ls)";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }
                    sql = "create index ix_pkod on " + temp_table + "(pkod)";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }
                    sql = "insert into " + temp_table + " (num_ls, pkod) " +
                          "select max(k.num_ls), pkod from " + Points.Pref + sDataAliasRest + "kvar k, " +
                          sDefaultSchema + "source_pack_ls l   where l.paycode=k.pkod and l.nzp_spack=" + nzpSpack +
                          " group by pkod";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }
                    sql = "update " + sDefaultSchema + "source_pack_ls s set num_ls= (select num_ls from " +
                          temp_table + " where s.paycode=pkod ) where s.nzp_spack=" + nzpSpack;
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }

                    break;
                case TypePayCode.Specific:

                    #region Сбор ЛС соответствующих полю user_ls

                    // временная таблица 
                    sql = "Create temp table " + temp_table + "(" +
                          "num_ls integer," +
                          "user_ls char(100)," +
                          "pkod numeric(13,0))";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }
                    // индексы
                    sql = "create index ix_old_ls on " + temp_table + "(num_ls,pkod)";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }

                    sql = "create index ix_user_ls on " + temp_table + "(user_ls)";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }
                    // заполняем временную таблицу платежными кодами, ЛС и старыми ЛС (user_ls), ориентируясь на поле remark таблицы kvar
                    sql = "insert into " + temp_table + " (num_ls, pkod, user_ls) " +
                          "select max(k.num_ls), coalesce(max(k.pkod),0), user_ls " +
                          "from " + sDefaultSchema + "source_pack_ls s left outer join " +
                          Points.Pref + sDataAliasRest + "kvar k on s.user_ls= k.remark " +
                          "where s.nzp_spack=" + nzpSpack + " group by user_ls";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }
                    // обновить num_ls таблицы source_pack_ls
                    sql = "UPDATE " + sDefaultSchema + "source_pack_ls l SET num_ls =o.num_ls, paycode=o.pkod " +
                          "from (select num_ls, pkod, user_ls from " + temp_table + ") o where o.user_ls=l.user_ls and nzp_spack=" + nzpSpack;
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сопоставления платежных кодов с ЛС";
                        return ret;
                    }

                    #endregion

                    break;
            }

            ret = ExecSQL(connDB, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка проставления ЛС по платежным кодам";
                MonitorLog.WriteLog(ret.text + " " + sql, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            ExecSQL(connDB, "Drop table " + temp_table, false);

            sql = "update  " + prefWeb + DBManager.tableDelimiter + "source_pack_ls " +
                       "set pref = (select max(pref) " +
                       "from " + Points.Pref + DBManager.sDataAliasRest + "kvar where paycode=pkod) " +
                       " where nzp_spack=" + nzpSpack;
            ret = ExecSQL(connDB, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка проставления ЛС по платежным кодам";
                MonitorLog.WriteLog(ret.text + " " +
                    sql, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            sql = "update  " + prefWeb + DBManager.tableDelimiter + "source_pack_ls " +
                      "set pref = (select max(pref) " +
                      "from " + Points.Pref + DBManager.sDataAliasRest + "kvar where paycode = num_ls) " +
                      " where pref is null and nzp_spack=" + nzpSpack;
            ret = ExecSQL(connDB, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка проставления ЛС по платежным кодам";
                MonitorLog.WriteLog(ret.text + " " +
                    sql, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            #endregion


            #region Загрузка заголовка ЛС
            sql = "select * from  " + prefWeb + DBManager.tableDelimiter + "source_pack_ls " +
                  "where nzp_spack = " + nzpSpack;
            ret = ExecRead(connDB, out reader, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки списка ЛС ";
                MonitorLog.WriteLog("Ошибка выборки списка ЛС " +
                    sql, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            pack.listPackLs = new List<Pack_ls>();
            while (reader.Read())
            {
                var packLS = new Pack_ls { nzp_pack_ls = Convert.ToInt32(reader["nzp_spack_ls"].ToString().Trim()) };

                if (reader["paycode"] != DBNull.Value) packLS.pkod = reader["paycode"].ToString().Trim();
                if (reader["num_ls"] != DBNull.Value) packLS.num_ls = Convert.ToInt32(reader["num_ls"]);
                else //Если платежный код меньше 9 символов, то возможно это лицевой счет
                {
                    if (typePayCode == TypePayCode.Standart)
                    {
                        if (packLS.pkod.Length < 9) packLS.num_ls = Convert.ToInt32(packLS.pkod);
                        else packLS.num_ls = 0;
                    }
                }

                if (reader["pref"] != DBNull.Value) packLS.pref = reader["pref"].ToString().Trim();
                packLS.inbasket = packLS.num_ls == 0 ? 1 : 0;
                if (reader["dat_vvod"] != DBNull.Value) packLS.dat_vvod = reader["dat_vvod"].ToString().Trim();
                if (reader["dat_month"] != DBNull.Value) packLS.dat_month = reader["dat_month"].ToString().Trim();
                if (reader["sum_ls"] != DBNull.Value) packLS.sum_ls = Convert.ToDecimal(reader["sum_ls"].ToString().Trim());
                if (reader["g_sum_ls"] != DBNull.Value) packLS.g_sum_ls = Convert.ToDecimal(reader["g_sum_ls"].ToString().Trim());
                if (reader["paysource"] != DBNull.Value) packLS.paysource = Convert.ToInt32(reader["paysource"].ToString().Trim());
                if (reader["kod_sum"] != DBNull.Value) packLS.kod_sum = Convert.ToInt32(reader["kod_sum"].ToString().Trim());
                if (reader["id_bill"] != DBNull.Value) packLS.id_bill = Convert.ToInt32(reader["id_bill"].ToString().Trim());
                if (reader["info_num"] != DBNull.Value) packLS.info_num = Convert.ToInt32(reader["info_num"].ToString().Trim());
                if (reader["erc_code"] != DBNull.Value) packLS.erc_code = reader["erc_code"].ToString().Trim();
                // При специфичном плат коде
                if (typePayCode == TypePayCode.Specific)
                {
                    if (reader["user_ls"] != DBNull.Value)
                    {
                        packLS.old_num_ls = reader["user_ls"].ToString().Trim();
                    }
                    // проверяем наличие ЛС
                    if (packLS.num_ls <= 0)
                    {
                        // Запись о несопоставленном ЛС платежному коду
                        packsInfo.AddWarnMsg("Платежному коду " + packLS.old_num_ls + " несопоставлен ЛС", packLS.pref);
                    }
                }

                #region Загрузка изменений жильца

                sql = "select * from  " + prefWeb + DBManager.tableDelimiter + "source_gil_sums " +
                      "where nzp_spack_ls=" + packLS.nzp_pack_ls;
                ret = ExecRead(connDB, out reader2, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка выборки изменений жильца по ЛС";
                    MonitorLog.WriteLog("Ошибка выборки изменений жильца по ЛС " +
                        sql, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                packLS.gilSums = new List<GilSum>();
                while (reader2.Read())
                {
                    var gs = new GilSum { nzp_pack_ls = packLS.nzp_pack_ls };
                    if (reader2["days_nedo"] != DBNull.Value) gs.day_nedo = reader2["days_nedo"].ToString().Trim();
                    if (reader2["sum_oplat"] != DBNull.Value) gs.sum_oplat = reader2["sum_oplat"].ToString().Trim();
                    if (reader2["ordering"] != DBNull.Value) gs.ordering = Convert.ToInt32(reader2["ordering"].ToString().Trim());
                    if (reader2["nzp_serv"] != DBNull.Value) gs.nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                    if (reader2["nzp_supp"] != DBNull.Value) gs.nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);

                    if (packLS.kod_sum == 50 || packLS.kod_sum == 49) //Оплаты поставщиков, считаем ordering за nzp_serv//???
                    {
                        gs.nzp_serv = gs.ordering;
                    }
                    else//Остальные оплаты ищем по ordering в charge
                    {
                        if (packLS.pref != "")
                        {
                            string chargetable = packLS.pref + "_charge_" + packLS.dat_month.Substring(8, 2) +
                                                 DBManager.tableDelimiter + "charge_" +
                                                 packLS.dat_month.Substring(3, 2);

                            if (pack.version_pack != "!1.02") GetNzpServByOrdering(connDB, packLS.num_ls, chargetable, gs);
                            if (!ret.result)
                            {
                                packsInfo.AddErrorMsg(ret.text);
                            }
                            try
                            {
                                decimal sum_oplat;
                                decimal.TryParse(gs.sum_oplat, out sum_oplat);
                                if (gs.nzp_serv == 0 && sum_oplat != 0)
                                {
                                    string baseName = Points.Pref + "_fin_" + (pack.year_ % 100).ToString("00");
                                    var sql1 =
                                        string.Format("INSERT INTO " + baseName + tableDelimiter + "pack_ls_err " +
                                                      " (nzp_pack_ls, nzp_err, nzp_serv, note) " +
                                                      " VALUES(" + packLS.nzp_pack_ls + ", 404, 0, " +
                                                      " 'Ошибка определения услуги для уточнения оплаты в ЕПД');");
                                    ExecSQL(connDB, sql1, true);
                                }
                            }
                            catch (Exception ex)
                            {
                                string msgerr = "Ошибка при добавлении в таблицу pack_ls_err";
                                packsInfo.AddErrorMsg(msgerr);
                                MonitorLog.WriteLog(msg + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                            }
                        }
                    }
                    packLS.gilSums.Add(gs);
                }
                reader2.Close();

                #endregion

                #region Загрузка приборов учета

                sql = "select * from  " + prefWeb + DBManager.tableDelimiter + "source_pu_vals " +
                      "where nzp_spack_ls=" + packLS.nzp_pack_ls;
                ret = ExecRead(connDB, out reader2, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка выборки показаний по ПУ по ЛС ";
                    MonitorLog.WriteLog(ret.text + " " +
                        sql, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                packLS.puVals = new List<PuVals>();
                while (reader2.Read())
                {
                    var pu = new PuVals { nzp_pack_ls = packLS.nzp_pack_ls };
                    if (reader2["num_cnt"] != DBNull.Value) pu.num_cnt = reader2["num_cnt"].ToString().Trim();
                    if (reader2["val_cnt"] != DBNull.Value) pu.val_cnt = Convert.ToDecimal(reader2["val_cnt"].ToString().Trim());
                    if (reader2["ordering"] != DBNull.Value) pu.ordering = Convert.ToInt32(reader2["ordering"].ToString().Trim());
                    if (reader2["nzp_serv"] != DBNull.Value) pu.nzp_serv = Convert.ToInt32(reader2["nzp_serv"].ToString().Trim());

                    if (packLS.pref != "")
                    {
                        GetPUByOrdering(connDB, packLS, pu, ref packsInfo);
                    }
                    packLS.puVals.Add(pu);
                }
                reader2.Close();
                //////////////// костылечек
                // ищем в local_data.counters те счетчики, которых не оказалось в counters_ord
                LastGoodValPuFromCountersTab lgvPU = new LastGoodValPuFromCountersTab(packLS.pref + "_data" + DBManager.tableDelimiter + "counters");
                if (packLS.pref != "")
                    ret = lgvPU.GetLastGoodValPUByOrderingFromDB(connDB, packLS, packLS.puVals, ref packsInfo);
                //////////////////////
                #endregion
                pack.listPackLs.Add(packLS);
            }
            if (reader != null)
            {
                reader.Close();
            }

            if (reader2 != null)
            {
                reader2.Close();
            }

            //эта проверка только для обычной пачки, не суперпачки
            if (parPack > 0 && parPack != nzpSpack &&  pack.count_kv != pack.listPackLs.Count)
            {
                ret.result = false;
                ret.text = "Количество квитанций, объявленных в пачке, не совпадает с количеством перенесенных в основную таблицу оплат";
                MonitorLog.WriteLog(ret.text + " " +
                    sql, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }


            #endregion
            return ret;
        }

        /// <summary>
        /// функция сохраняет 
        /// </summary>
        /// <param name="nzpPack">код суперпачки из source_pack </param>
        /// <param name="nzpUser"></param>
        /// <returns></returns>
        public Returns UploadPackFromWeb(int nzpPack, int nzpUser, ref AddedPacksInfo packsInfo)
        {
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            string packMessage = "";
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД ";
                MonitorLog.WriteLog("SaveWebUniversalPack :" + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

#if PG
            ExecSQL(connWeb, " set search_path to 'public'", false);
#endif
            string prefWeb = DBManager.GetFullBaseName(connWeb);
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(connDB, true);
            if (!ret.result) return new Returns(false, "Не коннекта к базе данных " +
                Points.Pref, -3);

            string baseName = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00");


            var sql = new StringBuilder();

            var superPack = new Pack
            {
                year_ = Points.DateOper.Year
            };



            #region Загрузка данных из веб базы

            MyDataReader reader;
            #region Загрузка суперпачки
            sql.Remove(0, sql.Length);
            sql.Append("SELECT * FROM " + prefWeb + DBManager.tableDelimiter + "source_pack  ");
            sql.Append("WHERE nzp_spack = " + nzpPack);
            ret = ExecRead(connDB, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки суперпачки";
                MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            int parSPack = 0;
            if (reader.Read())
            {

                if (reader["place_of_made"] != DBNull.Value) superPack.bank = reader["place_of_made"].ToString().Trim();
                if (reader["num_pack"] != DBNull.Value) superPack.num = reader["num_pack"].ToString().Trim();
                if (reader["date_pack"] != DBNull.Value) superPack.dat_pack = reader["date_pack"].ToString().Trim();
                if (reader["date_oper"] != DBNull.Value) superPack.dat_calc = reader["date_oper"].ToString().Trim();
                if (reader["count_in_pack"] != DBNull.Value) superPack.count_kv = Convert.ToInt32(reader["count_in_pack"].ToString().Trim());
                if (reader["sum_pack"] != DBNull.Value) superPack.sum_pack = Convert.ToDecimal(reader["sum_pack"].ToString().Trim());
                if (reader["sum_geton"] != DBNull.Value) superPack.nzp_supp =
                    Convert.ToInt64(
                    Convert.ToDecimal(reader["sum_geton"].ToString().Trim()));
                if (reader["filename"] != DBNull.Value) superPack.file_name = reader["filename"].ToString().Trim();
                if (reader["erc_code"] != DBNull.Value) superPack.erc_code = reader["erc_code"].ToString().Trim();
                if (reader["version"] != DBNull.Value) superPack.version_pack = reader["version"].ToString().Trim();
                if (reader["par_pack"] != DBNull.Value)
                    Int32.TryParse(reader["par_pack"].ToString(), out parSPack);
            }
            reader.Close();
            ret = GetListPackLsFromWeb(connDB, prefWeb, ref superPack, nzpPack, parSPack, ref packsInfo);
            if (!ret.result)
            {
                return ret;
            }
            #endregion
            #region Загрузка подпачек
            sql.Remove(0, sql.Length);
            sql.Append("SELECT * FROM " + prefWeb + DBManager.tableDelimiter + "source_pack  ");
            sql.Append("WHERE nzp_spack<>par_pack and par_pack = " + nzpPack);
            ret = ExecRead(connDB, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки пачки";
                MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            superPack.listPack = new List<Pack>();
            while (reader.Read())
            {
                var pack = new Pack();
                if (reader["place_of_made"] != DBNull.Value) pack.bank = reader["place_of_made"].ToString().Trim();
                if (reader["num_pack"] != DBNull.Value) pack.num = reader["num_pack"].ToString().Trim();
                if (reader["date_pack"] != DBNull.Value) pack.dat_pack = reader["date_pack"].ToString().Trim();
                if (reader["date_oper"] != DBNull.Value) pack.dat_calc = reader["date_oper"].ToString().Trim();
                if (reader["count_in_pack"] != DBNull.Value) pack.count_kv = Convert.ToInt32(reader["count_in_pack"].ToString().Trim());
                if (reader["sum_pack"] != DBNull.Value) pack.sum_pack = Convert.ToDecimal(reader["sum_pack"].ToString().Trim());
                if (reader["sum_geton"] != DBNull.Value) pack.nzp_supp =
                                Convert.ToInt64(
                                Convert.ToDecimal(reader["sum_geton"].ToString().Trim()));
                if (reader["filename"] != DBNull.Value) pack.file_name = reader["filename"].ToString().Trim();
                if (reader["erc_code"] != DBNull.Value) pack.erc_code = reader["erc_code"].ToString().Trim();
                if (reader["version"] != DBNull.Value) pack.version_pack = reader["version"].ToString().Trim();
                pack.year_ = Points.DateOper.Year;

                int parPack = 0;
                if (reader["par_pack"] != DBNull.Value) Int32.TryParse(reader["par_pack"].ToString(), out parPack);

                ret = GetListPackLsFromWeb(connDB, prefWeb, ref  pack, Convert.ToInt32(reader["nzp_spack"]),
                    parPack, ref packsInfo);
                if (!ret.result)
                {
                    return ret;
                }
                superPack.listPack.Add(pack);
            }
            reader.Close();
            #endregion
            if (superPack.listPack.Count > 0)
            {
                packsInfo.InitPackProgress(superPack.listPack.Count);
            }

            ret = SaveUniversalPack(connDB, baseName, superPack, ref packsInfo);
            packsInfo.InsertedNzpPack = superPack.nzp_pack;
            if (!ret.result)
            {
                return ret;
            }
            #endregion
            return ret;
        }



    } //end class

} //end namespace
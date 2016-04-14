using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{

    public class DbReestrTula : DataBaseHead
    {

        public List<_reestr_unloads> LoadUploadedReestrList(Finder finder, out Returns ret)
        {


            var spis = new List<_reestr_unloads>();
            MyDataReader reader;
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            string skip = "";
            string rows = "";


#if PG
            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
#else
            if (finder.skip != 0)
            {
                skip = " skip " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " first " + finder.rows;
            }
#endif
            string sql;


            //выбрать список
#if PG
            sql = "select r.*,u.name from " + Points.Pref + "_data.tula_reestr_unloads r left outer join " + Points.Pref +
            "_data.users u on r.user_unloaded=u.nzp_user where r.is_actual<>100  order by nzp_reestr desc  " + skip + " " + rows + " ";
#else
            sql = ("select " + skip + " " + rows + " r.*,u.name " +
                         "from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_unloads r left outer join " +
                         Points.Pref + DBManager.sDataAliasRest + "users u on r.user_unloaded=u.nzp_user " +
                         " where r.is_actual<>100 order by nzp_reestr desc ");
#endif
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            try
            {
                //int i = 0;
                while (reader.Read())
                {
                    var zap = new _reestr_unloads();
                    if (reader["nzp_reestr"] == DBNull.Value) zap.nzp_reestr = 0;
                    else zap.nzp_reestr = (int)reader["nzp_reestr"];

                    zap.name_file = reader["name_file"] == DBNull.Value ? "" : reader["name_file"].ToString().Trim();

                    if (reader["date_unload"] == DBNull.Value) zap.date_unload = new DateTime().ToShortDateString();
                    else
                    {
                        var datUnload = (DateTime)reader["date_unload"];
                        zap.date_unload = datUnload.ToShortDateString();
                    }

                    zap.unloading_date = reader["unloading_date"] == DBNull.Value
                        ? ""
                        : reader["unloading_date"].ToString();

                    if (reader["user_unloaded"] == DBNull.Value) zap.user_unloaded = 0;
                    else zap.user_unloaded = (int)reader["user_unloaded"];

                    zap.name_user_unloaded = reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim();

                    if (reader["nzp_exc"] == DBNull.Value) zap.nzp_exc = 0;
                    else zap.nzp_exc = (int)reader["nzp_exc"];

                    spis.Add(zap);

                }

                //определить количество записей
                sql = "select count(*) from " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_reestr_unloads where is_actual<>100; ";
                object count = ExecScalar(connDB, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        connDB.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                reader.Close();

                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();


                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра" + err, MonitorLog.typelog.Error, 20, 201,
                    true);
                return null;
            }
            finally
            {
                connDB.Close();
            }
        }


        public List<_reestr_downloads> LoadDownloadedReestrList(Finder finder, out Returns ret)
        {


            var spis = new List<_reestr_downloads>();
            MyDataReader reader;
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            string skip = "";
            string rows = "";
#if PG
            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
#else
            if (finder.skip != 0)
            {
                skip = " skip " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " first " + finder.rows;
            }
#endif
            //выбрать список
            string sql;

#if PG
            sql = "select  r.*,s.name_type,u.name, bk.bank as branch_name,proc, status  " +
             " from " + Points.Pref + sDataAliasRest + "tula_reestr_downloads r " +
             " left outer join " + Points.Pref + sDataAliasRest + "users u on r.user_downloaded=u.nzp_user " +
             " left outer join " + Points.Pref + sDataAliasRest + "tula_reestr_sprav s on r.nzp_type=s.nzp_type " +
             " left outer join " + Points.Pref + sKernelAliasRest + "s_bank bk on bk.nzp_bank = r.nzp_bank  " +
             " order by nzp_download desc" + skip + " " + rows + "";
#else

            sql = "select " + skip + " " + rows + "  r.*,s.name_type,u.name, bk.bank as branch_name, proc, status " +
              " from " + Points.Pref + sDataAliasRest + "tula_reestr_downloads r " +
              " left outer join " + Points.Pref + sDataAliasRest + "users u on r.user_downloaded=u.nzp_user " +
              " left outer join " + Points.Pref + sDataAliasRest + "tula_reestr_sprav s on r.nzp_type=s.nzp_type " +
              " left outer join " + Points.Pref + sKernelAliasRest + "s_bank bk on bk.nzp_bank = r.nzp_bank  " +
              " order by nzp_download desc";
#endif
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                MonitorLog.WriteLog("Ошибка получения списка загрузок реестра", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            try
            {

                while (reader.Read())
                {
                    var zap = new _reestr_downloads();
                    if (reader["nzp_download"] == DBNull.Value) zap.nzp_download = 0;
                    else zap.nzp_download = (int)reader["nzp_download"];

                    zap.file_name = reader["file_name"] == DBNull.Value ? "" : reader["file_name"].ToString().Trim();

                    zap.name_type = reader["name_type"] == DBNull.Value ? "" : reader["name_type"].ToString().Trim();


                    if (reader["date_download"] == DBNull.Value) zap.date_download = "";
                    else
                    {
                        var dateDownload = (DateTime)reader["date_download"];
                        zap.date_download = dateDownload.ToString(CultureInfo.InvariantCulture);
                    }

                    if (reader["day"] == DBNull.Value || reader["month"] == DBNull.Value) zap.day_month = "";
                    else zap.day_month = ((int)reader["day"]) + "/" + ((int)reader["month"]);


                    zap.branch_name = reader["branch_name"] == DBNull.Value
                        ? ""
                        : reader["branch_name"].ToString().Trim();

                    zap.name_user_downloaded = reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim();

                    int nzp_status = reader["status"] == DBNull.Value ? -1 : Convert.ToInt32(reader["status"]);
                    zap.status = zap.getNameStatus(nzp_status);

                    if (reader["proc"] == DBNull.Value) zap.proc = 0d;
                    else zap.proc = Convert.ToDouble(reader["proc"]);

                    zap.nzp_exc = (reader["nzp_exc"] != DBNull.Value ? Convert.ToInt32(reader["nzp_exc"]) : 0);

                    zap.nzp_type = (reader["nzp_type"] != DBNull.Value ? Convert.ToInt32(reader["nzp_type"]) : 0);

                    if (zap.nzp_type == 3 || zap.nzp_type == 1)
                        zap.protocol = (zap.nzp_exc != 0 ? "Скачать" : "Не сформирован");

                    spis.Add(zap);

                }

                //определить количество записей
                sql = "select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads; ";
                object count = ExecScalar(connDB, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        connDB.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                reader.Close();

                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();


                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка получения списка выгрузок реестра" + err, MonitorLog.typelog.Error, 20, 201,
                    true);
                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        public Returns DeleteReestrTula(_reestr_unloads finder)
        {
            Returns ret;
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1) { result = false };
                return ret;
            }
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            if (!ExecSQL(connDB, " update " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_unloads set is_actual=100 " +
                                  " where nzp_reestr=" + finder.nzp_reestr, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка обновления реестра для Тулы";
            }
            return ret;
        }


        public Returns DeleteDownloadReestrTula(Finder finder, int nzpDownload)
        {
            Returns ret;
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1) { result = false };
                return ret;
            }
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;


            string sql = " select nzp_type from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                         " where nzp_download=" + nzpDownload + " ";
            object obj = ExecScalar(connDB, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }

            if (obj == null)
            {
                ret.result = false;
                ret.text = "Ошибка удаления данных";
                return ret;
            }

            int nzpType = Convert.ToInt32(obj);
            //Удаляем квитанцию, она удаляется только вместе с файлом реестра

            string finAlias = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") +
                              tableDelimiter;
            string Name = "";
            var datePlat = "";
            var datesPlat = new List<DateTime>();
            if (nzpType == 2 || nzpType == 4)
            {

                #region Получение имени файла по загрузке

                sql = " select nzp_kvit_reestr from " + Points.Pref + DBManager.sDataAliasRest +
                  "tula_kvit_reestr where nzp_download=" + nzpDownload + " ";
                var nzp_kvit = CastValue<int>(ExecScalar(connDB, sql, out ret, true));
                if (nzp_kvit > 0)
                {
                    sql = "select trim(file_name) from " + Points.Pref + DBManager.sDataAliasRest +
                          "tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    Name = CastValue<string>(ExecScalar(connDB, sql, out ret, true));

                    //дата оплаты из файла квитанции - для старых записей
                    sql = "select date_plat from " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    datesPlat.Add(CastValue<DateTime>(ExecScalar(connDB, sql, out ret, true)));

                    //получаем список дат оплат
                    sql = "select distinct payment_datetime from " + Points.Pref + DBManager.sDataAliasRest +
                          "tula_file_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    var dates = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                    if (dates != null)
                    {
                        for (int i = 0; i < dates.Rows.Count; i++)
                        {
                            datesPlat.Add(CastValue<DateTime>(dates.Rows[i]["payment_datetime"]));
                        }
                    }
                    //dat_vvod in (...)
                    datePlat = string.Join(",", datesPlat.Select(x => Utils.EStrNull(x.ToShortDateString())));

                }
                else
                {
                    sql = " select file_name from " + Points.Pref + DBManager.sDataAliasRest +
                   "tula_reestr_downloads where nzp_download=" + nzpDownload + " ";
                    object name = ExecScalar(connDB, sql, out ret, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    if (name == null)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка получения данных о удаляемом файле", MonitorLog.typelog.Error, true);
                        return ret;
                    }

                    string type;
                    var nameFile = Convert.ToString(name).Trim().Split('.');
                    if (nzpType == 2)
                    {
                        type = ".0" + nameFile[1].Substring(1, 2);
                    }
                    else
                    {
                        type = ".00" + nameFile[1].Substring(2);
                    }
                    Name = nameFile[0] + type;
                }

                #endregion

                #region Проверка распределенных сумм

     
                sql = " select count(*) as count_rasp " +
                      " from " + finAlias + "pack p," +
                      "      " + finAlias + "pack_ls pl " +
                      " where file_name=" + Utils.EStrNull(Name) +
                      " and p.nzp_pack=pl.nzp_pack " +
                      " and pl.dat_uchet is not null" +
                      (datesPlat.Count > 0 ? " and pl.dat_vvod in (" + datePlat + ")" : "");
                object countRasp = ExecScalar(connDB, sql, out ret, true);
                if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
                {
                    ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                             "сначала отмените распределение по данному реестру", -1) { result = false };
                    return ret;
                }

                sql = "select nzp_pack from " + finAlias + "pack_ls where nzp_pack in (select nzp_pack" +
                   " from " + finAlias + "pack " +
                   " where file_name=" + Utils.EStrNull(Name) + " )  " +
                   (datesPlat.Count > 0
                       ? " and dat_vvod in (" + datePlat + ")"
                       : "") +
                   " group by 1";
                var DT = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                List<string> l_nzp_pack = new List<string>();
                foreach (DataRow row in DT.Rows)
                {
                    l_nzp_pack.Add(CastValue<string>(row["nzp_pack"]));
                }
                var s_nzp_pack = string.Join(",", l_nzp_pack);
                if (s_nzp_pack == "") s_nzp_pack = "-1";

                //Удаление оплат
                sql = " delete  " +
                      " from " + finAlias + "pack_ls " +
                      " where nzp_pack in (select nzp_pack" +
                      " from " + finAlias + "pack " +
                      " where file_name=" + Utils.EStrNull(Name) + ")" +
                      (datesPlat.Count > 0 ? " and dat_vvod in (" + datePlat + ")" : "");
                ExecSQL(connDB, sql, true);


                //Удаление пачек
                sql = " delete  " +
                      " from " + finAlias + "pack " +
                      " where nzp_pack in (" + s_nzp_pack + ")";
                ExecSQL(connDB, sql, true);


                #endregion


                #region Удаляем записи в реестре загрузок


                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                      " where  nzp_download = " + nzpDownload + " ";
                if (!ExecSQL(connDB, sql.ToString(), true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                //Удаляем запись реестра
                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest +
                        "tula_reestr_downloads " +
                        " where  file_name = '" + Name + "' and date_download>=" + Utils.EStrNull(Points.DateOper.ToShortDateString());

                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion



                #region Удаляем счетчики

                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                      " where nzp_reestr_d in (select nzp_reestr_d " +
                      "from " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                      " where nzp_kvit_reestr=" + nzp_kvit + ")";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                #endregion

                #region Удаляем платежи
                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                     " where nzp_kvit_reestr=" + nzp_kvit + "";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

                #region Удаляем квитанцию
                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr " +
                      " where nzp_kvit_reestr=" + nzp_kvit + "";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

            }
            else
            {

                sql = " select distinct is_itog from " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr " +
                    " where file_name = (select d.file_name from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d where d.nzp_download = " + nzpDownload + ")";
                var isItog = CastValue<int>(ExecScalar(connDB, sql, out ret, true));

                if (isItog == 1)
                {
                    #region Проверка распределенных сумм

                    sql = " select count(*) as count_rasp " +
                          " from " + finAlias + "pack p," +
                          "      " + finAlias + "pack_ls pl, " +
                          "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                          " where  d.nzp_download=" + nzpDownload +
                          "     and p.file_name=d.file_name " +
                          "     and p.nzp_pack=pl.nzp_pack " +
                          "     and pl.dat_uchet is not null";
                    object countRasp = ExecScalar(connDB, sql, out ret, true);
                    if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
                    {
                        ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                                 "сначала отмените распределение по данному реестру", -1) { result = false };
                        return ret;
                    }

                    //Удаление оплат
                    sql = " delete  " +
                          " from " + finAlias + "pack_ls " +
                          " where nzp_pack in (select nzp_pack" +
                          " from " + finAlias + "pack p, " +
                          "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                          "  where d.nzp_download=" + nzpDownload +
                          "     and p.file_name=d.file_name)";
                    ExecSQL(connDB, sql, true);

                    //Удаление пачек
                    sql = " delete  " +
                          " from " + finAlias + "pack p " +
                          " where file_name in (select file_name" +
                          " from     " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                          "  where d.nzp_download=" + nzpDownload + ")";
                    ExecSQL(connDB, sql, true);


                    #endregion


                    #region Удаляем счетчики

                    sql = " delete " +
                          " from " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                          " where nzp_reestr_d in (select nzp_reestr_d " +
                          " from  " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr kv,  " +
                          Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr t, " +
                          Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                          " where trim(d.file_name)=trim(kv.file_name) " +
                          " and t.nzp_kvit_reestr =kv.nzp_kvit_reestr " +
                          " and d.nzp_download=" + nzpDownload + ") ";
                    if (!ExecSQL(connDB, sql, true).result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка удаления данных загрузки";
                        return ret;
                    }


                    #endregion

                    //Удаляем данные реестра
                    sql = " delete from  " + Points.Pref + DBManager.sDataAliasRest +
                          "tula_file_reestr where nzp_kvit_reestr in " +
                          " (select nzp_kvit_reestr from  " + Points.Pref + DBManager.sDataAliasRest +
                          "tula_kvit_reestr kv,  " +
                          Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                          " where trim(d.file_name)=trim(kv.file_name) " +
                          " and d.nzp_download=" + nzpDownload + ") ";
                    if (!ExecSQL(connDB, sql, true).result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка удаления данных реестра";
                        return ret;
                    }

                    #region Удаляем квитанцию
                    //sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr " +
                    //      " where file_name = (select d.file_name from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d where d.nzp_download = " + nzpDownload + ")";
                    //if (!ExecSQL(connDB, sql, true).result)
                    //{
                    //    ret.result = false;
                    //    ret.text = "Ошибка удаления данных загрузки";
                    //    return ret;
                    //}
                    #endregion

                    //Удаляем запись реестра
                    sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                          " where  nzp_download = " + nzpDownload + " ";
                    if (!ExecSQL(connDB, sql, true).result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка удаления данных загрузки";
                        return ret;
                    }
                }
                else
                {
                    ret = new Returns(false, "Файл реестра удаляется только вместе с файлом квитанции", -1) { result = false };
                    return ret;
                }

            }

            if (ret.result)
            {
                ret.text = "Успешно удалено";
                ret.tag = -1;
            }

            return ret;
        }

        public Returns DeleteDownloadReestrMariyEl(Finder finder, int nzpDownload)
        {
            Returns ret;
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1) { result = false };
                return ret;
            }
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;


            string finAlias = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") +
                              tableDelimiter;

            #region Проверка распределенных сумм
#warning Эта проверка не является правильной!!! Нужно вызывать одну фунцкию удаления оплаты
            var sql = " select count(*) as count_rasp " +
                    " from " + finAlias + "pack p," +
                    "      " + finAlias + "pack_ls pl, " +
                    "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                    " where  d.nzp_download=" + nzpDownload +
                    "     and p.file_name=d.file_name " +
                    "     and p.nzp_pack=pl.nzp_pack " +
                    "     and pl.dat_uchet is not null";
            object countRasp = ExecScalar(connDB, sql, out ret, true);
            if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
            {
                ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                         "сначала отмените распределение по данному реестру", -1) { result = false };
                return ret;
            }

            //Удаление оплат
            sql = " delete  " +
                  " from " + finAlias + "pack_ls " +
                  " where nzp_pack in (select nzp_pack" +
                  " from " + finAlias + "pack p, " +
                  "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                  "  where d.nzp_download=" + nzpDownload +
                  "     and p.file_name=d.file_name)";
            ExecSQL(connDB, sql, true);

            //Удаление пачек
            sql = " delete  " +
                  " from " + finAlias + "pack p " +
                  " where file_name in (select file_name" +
                  " from     " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                  "  where d.nzp_download=" + nzpDownload + ")";
            ExecSQL(connDB, sql, true);


            #endregion


            #region Удаляем счетчики

            sql = " delete " +
                  " from " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                  " where nzp_reestr_d in (select nzp_reestr_d " +
                  " from  " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr kv,  " +
                  Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr t, " +
                  Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                  " where trim(d.file_name)=trim(kv.file_name) " +
                  " and t.nzp_kvit_reestr =kv.nzp_kvit_reestr " +
                  " and d.nzp_download=" + nzpDownload + ") ";
            if (!ExecSQL(connDB, sql, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка удаления данных загрузки";
                return ret;
            }


            #endregion

            //Удаляем данные реестра
            sql = " delete from  " + Points.Pref + DBManager.sDataAliasRest +
                  "tula_file_reestr where nzp_kvit_reestr in " +
                  " (select nzp_kvit_reestr from  " + Points.Pref + DBManager.sDataAliasRest +
                  "tula_kvit_reestr kv,  " +
                  Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                  " where trim(d.file_name)=trim(kv.file_name) " +
                  " and d.nzp_download=" + nzpDownload + ") ";
            if (!ExecSQL(connDB, sql, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка удаления данных реестра";
                return ret;
            }


            //Удаляем запись реестра

            sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                  " where  nzp_download = " + nzpDownload + " ";
            if (!ExecSQL(connDB, sql, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка удаления данных загрузки";
                return ret;
            }


            if (ret.result)
            {
                ret.text = "Успешно удалено";
                ret.tag = -1;
            }

            return ret;

        }


        public List<tula_s_bank> LoadPayerAgents(Finder finder, out Returns ret)
        {


            var spis = new List<tula_s_bank>();
            MyDataReader reader;
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            string skip = "";
            string rows = "";


#if PG
            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
#else
            if (finder.skip != 0)
            {
                skip = " skip " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " first " + finder.rows;
            }
#endif
            string sql;


            //выбрать список
#if PG
            sql = "SELECT t.id,t.branch_id,t.branch_id_reestr,t.nzp_bank, t.format_number,  t.download_format_number, max(p.nzp_payer) as nzp_payer, max(b.bank) as branch_name" +
            " FROM " + Points.Pref + sDataAliasRest + "tula_s_bank t " +
            " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_bank b on t.nzp_bank=b.nzp_bank" +
            " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_payer p on b.nzp_payer=p.nzp_payer" +
            " WHERE t.is_actual<>100 " +
            " group by 1,2,3,4,5,6 order by id desc " + skip + " " + rows;
#else
            sql = "SELECT " + skip + " " + rows + " t.id,t.branch_id,t.branch_id_reestr,t.nzp_bank,max(p.nzp_payer) as nzp_payer, max(b.bank) as branch_name " +
            " FROM " + Points.Pref + sDataAliasRest + "tula_s_bank t " +
            " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_bank b on t.nzp_bank=b.nzp_bank" +
            " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_payer p on b.nzp_payer=p.nzp_payer" +
            " WHERE t.is_actual<>100 " +
            " group by 1,2,3,4 order by id desc";
#endif
            if (!ExecRead(connDB, out reader, sql, true).result)
            {
                connDB.Close();
                MonitorLog.WriteLog("Ошибка получения списка платежных агентов", MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    var zap = new tula_s_bank();
                    zap.id = CastValue<int>(reader["id"]);
                    zap.branch_name = CastValue<string>(reader["branch_name"]).Trim();
                    zap.branch_id = CastValue<string>(reader["branch_id"]).Trim();
                    zap.branch_id_reestr = CastValue<string>(reader["branch_id_reestr"]).Trim();
                    zap.nzp_bank = CastValue<int>(reader["nzp_bank"]);
                    zap.nzp_payer = CastValue<int>(reader["nzp_payer"]);
                    zap.FormatNumberUpload = CastValue<int>(reader["format_number"]);
                    zap.FormatNumberDownload = CastValue<int>(reader["download_format_number"]);
                    spis.Add(zap);
                }

                //определить количество записей
                sql = "select count(*) from " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_s_bank WHERE is_actual<>100; ";
                object count = ExecScalar(connDB, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        connDB.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return null;
                    }
                }

                reader.Close();

                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();


                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка получения списка платежных агентов" + err, MonitorLog.typelog.Error, 20, 201,
                    true);
                return null;
            }
            finally
            {
                connDB.Close();
            }
        }

        public Returns DeletePayerAgent(Finder finder, int id)
        {
            Returns ret;
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1) { result = false };
                return ret;
            }
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            string sql = "UPDATE " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank SET (is_actual,dat_del)=(100," + DBManager.sCurDateTime + ")" +
                "WHERE id=" + id;

            if (!ExecSQL(connDB, sql, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка удаления платежного агента";
                return ret;
            }

            return ret;
        }


        public Returns AddPayerAgent(Finder finder, tula_s_bank agent)
        {
            Returns ret;
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1) { result = false };
                return ret;
            }
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            ret = CheckPayerAgentId(connDB, finder, agent);
            if (!ret.result)
            {
                return ret;
            }

            string sql = "INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank " +
                "(nzp_bank, branch_id,branch_id_reestr,dat_add, format_number, download_format_number)  values (" + agent.nzp_bank + "," + Utils.EStrNull(agent.branch_id).ToUpper() + "," +
                Utils.EStrNull(agent.branch_id_reestr).ToUpper() + "," + DBManager.sCurDateTime + ", " + agent.FormatNumberUpload + ", " + agent.FormatNumberDownload + ")";

            if (!ExecSQL(connDB, sql.ToString(), true).result)
            {
                ret.result = false;
                ret.text = "Ошибка добавления платежного агента";
                return ret;
            }

            return ret;
        }

        public Returns SavePayerAgent(Finder finder, tula_s_bank agent)
        {
            Returns ret;
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1) { result = false };
                return ret;
            }
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            ret = CheckPayerAgentId(connDB, finder, agent);
            if (!ret.result)
            {
                return ret;
            }

            string sql = "UPDATE " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank " +
               " SET (nzp_bank, branch_id,branch_id_reestr, format_number, download_format_number)=" +
               "(" + agent.nzp_bank + "," + Utils.EStrNull(agent.branch_id).ToUpper() + "," + Utils.EStrNull(agent.branch_id_reestr).ToUpper() + ", " + agent.FormatNumberUpload + ", " + agent.FormatNumberDownload + ")" +
               " WHERE id=" + agent.id;

            if (!ExecSQL(connDB, sql.ToString(), true).result)
            {
                ret.result = false;
                ret.text = "Ошибка сохранения платежного агента";
                return ret;
            }

            return ret;
        }


        /// <summary>
        /// Проверка на незанятость идентификатора и уникальность банка
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="finder"></param>
        /// <param name="agent"></param>
        /// <returns>true - свободно, false - занято</returns>
        private Returns CheckPayerAgentId(IDbConnection connDB, Finder finder, tula_s_bank agent)
        {
            Returns ret;
            int count = 0;
            string exist = "";
            if (agent.id != 0)
            {
                exist = " AND id<>" + agent.id;
            }
            string sql = "SELECT count(*) FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank  " +
                   " WHERE is_actual<>100 and  (UPPER(branch_id)=" + Utils.EStrNull(agent.branch_id.ToUpper()) +
                   " OR UPPER(branch_id_reestr)=" + Utils.EStrNull(agent.branch_id_reestr.ToUpper()) + ")" +
                   exist;
            var obj = ExecScalar(connDB, sql, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                count = Convert.ToInt32(obj);
                if (count > 0)
                {
                    ret.text = "Данный идентификатор занят другим платежным агентом!";
                    ret.result = false;
                    return ret;
                }
            }

            //sql = "SELECT count(*) FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank  WHERE is_actual<>100 and  nzp_bank=" + agent.nzp_bank + exist;
            //obj = ExecScalar(connDB, sql.ToString(), out ret, true);
            //if (obj != null && obj != DBNull.Value)
            //{
            //    count = Convert.ToInt32(obj);
            //    if (count > 0)
            //    {
            //        ret.text = "Для этого банка уже существует идентификатор!";
            //        ret.result = false;
            //        return ret;
            //    }
            //}

            return ret;
        }



    }

}

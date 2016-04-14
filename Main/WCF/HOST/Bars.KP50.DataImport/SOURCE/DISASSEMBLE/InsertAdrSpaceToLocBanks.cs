using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class InsertAddrrSpaceIntoLocBanks  : DataBaseHeadServer
    {
        private readonly IDbConnection _conDb;
        public InsertAddrrSpaceIntoLocBanks (IDbConnection conDb)
        {
            _conDb = conDb;
        }

        /// <summary>
        ///  Спустить адресное пространство из верхнего банка в локальный
        /// для загруженных данных 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void Run(FilesDisassemble finder, ref Returns ret)
        {
            try
            {
                MonitorLog.WriteLog("Старт заполнения адресного пространства в нижнем банке", MonitorLog.typelog.Info, true);
               
                string sql =
                    " update " + Points.Pref + DBManager.sUploadAliasRest +
                    " files_imported set diss_status = 'Обновление адресного пространства' " +
                    " where nzp_file=" + finder.nzp_file;
                DBManager.ExecSQL(_conDb, null, sql, true);

                MonitorLog.WriteLog(InsertStat(finder).text, MonitorLog.typelog.Info, true);
                MonitorLog.WriteLog(InsertTown(finder).text, MonitorLog.typelog.Info, true);
                MonitorLog.WriteLog(InsertRajon(finder).text, MonitorLog.typelog.Info, true);
                MonitorLog.WriteLog(InsertUlica(finder).text, MonitorLog.typelog.Info, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры InsertAdrrSpaceIntoLocBanks:" + ex.Message);
            }
            MonitorLog.WriteLog("Успешно выполнено заполнение адресного пространства в нижнем банке", MonitorLog.typelog.Info, true);
        }

        /// <summary>
        /// Проверяем таблицу s_stat в локальном банке,
        /// при отсутствии данных - вставляем из верх. банка
        /// </summary>
        /// <param name="finder"></param>
        private Returns InsertStat(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            try
            {
                string sql =
                    " SELECT nzp_stat, nzp_land, stat, stat_t, soato " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_stat WHERE nzp_stat in " +
                    "    (select nzp_stat from " + Points.Pref + DBManager.sDataAliasRest + "s_town " +
                    "      where nzp_town in" +
                    "                    (select nzp_town from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    "                     where nzp_file = " + finder.nzp_file + "));";
                foreach (DataRow r in ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows)
                {
                    sql =
                        " SELECT * FROM " + finder.bank + DBManager.sDataAliasRest + "s_stat " +
                        " WHERE nzp_stat = " + r["nzp_stat"];
                    if (ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows.Count == 0)
                    {
                        sql =
                            " INSERT INTO " + finder.bank + DBManager.sDataAliasRest + "s_stat " +
                            " (nzp_stat, nzp_land, stat, stat_t, soato) " +
                            " VALUES " +
                            "( " + r["nzp_stat"].ToString().Trim() + ", " + r["nzp_land"] + ", " +
                            "'" + r["stat"].ToString().Trim() + "', '" + r["stat_t"].ToString().Trim() + "', '" +
                            r["soato"].ToString().Trim() + "'" +
                            ")";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка при добавлении адресного простанства на уровне областей";
                ret.result = false;
                ret.tag= - 1;
                throw new Exception("Ошибка выполнения процедуры InsertStat: " + ex.Message);
            }
            ret.result = true;
            ret.text = "Успешно добавлено адресное простанство на уровне областей";
            return ret;
        }

        /// <summary>
        /// Проверяем таблицу s_town в локальном банке,
        /// при отсутствии данных - вставляем из верх. банка
        /// </summary>
        /// <param name="finder"></param>
        private Returns InsertTown(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            try
            {
                string sql =
                    " SELECT nzp_town, nzp_stat, town, town_t, soato " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_town WHERE nzp_town in " +
                    "                    (select nzp_town from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    "                     where nzp_file = " + finder.nzp_file + ");";
                foreach (DataRow r in ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows)
                {
                    sql =
                        " SELECT * FROM " + finder.bank + DBManager.sDataAliasRest + "s_town " +
                        " WHERE nzp_town = " + r["nzp_town"];
                    if (ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows.Count == 0)
                    {
                        sql =
                            "INSERT INTO " + finder.bank + DBManager.sDataAliasRest + "s_town " +
                            " (nzp_town, nzp_stat, town, town_t, soato) " +
                            " VALUES " +
                            "( " + r["nzp_town"].ToString().Trim() + ", " + r["nzp_stat"] + ", " +
                            "'" + r["town"].ToString().Trim() + "', '" + r["town_t"].ToString().Trim() + "', '" +
                            r["soato"].ToString().Trim() + "'" +
                            ")";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка при добавлении адресного простанства на уровне городов/районов";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры InsertTown: " + ex.Message);
            }
            ret.result = true;
            ret.text = "Успешно добавлено адресное простанство на уровне городов/районов";
            return ret;
        }

        /// <summary>
        /// Проверяем таблицу s_rajon в локальном банке,
        /// при отсутствии данных - вставляем из верх. банка
        /// </summary>
        /// <param name="finder"></param>
        private Returns InsertRajon(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            try
            {
                string sql =
                    " SELECT  nzp_raj, nzp_town, rajon, rajon_t, soato " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_rajon WHERE nzp_town in " +
                    "                    (select nzp_town from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    "                     where nzp_file = " + finder.nzp_file + ");";
                foreach (DataRow r in ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows)
                {
                    sql =
                        " SELECT * FROM " + finder.bank + DBManager.sDataAliasRest + "s_rajon " +
                        " WHERE nzp_raj = " + r["nzp_raj"];
                    if (ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows.Count == 0)
                    {
                        sql =
                            " INSERT INTO " + finder.bank + DBManager.sDataAliasRest + "s_rajon " +
                            " ( nzp_raj, nzp_town, rajon, rajon_t, soato) " +
                            " VALUES " +
                            "( " + r["nzp_raj"].ToString().Trim() + ", " + r["nzp_town"] + ", " +
                            "'" + r["rajon"].ToString().Trim() + "', '" + r["rajon_t"].ToString().Trim() + "', '" +
                            r["soato"].ToString().Trim() + "'" +
                            ")";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка при добавлении адресного простанства на уровне населенных пунктов";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры InsertRajon: " + ex.Message);
            }
            ret.result = true;
            ret.text = "Успешно добавлено адресное простанство на уровне населенных пунктов";
            return ret;
        }

        /// <summary>
        /// Проверяем таблицу s_ulica в локальном банке,
        /// при отсутствии данных - вставляем из верх. банка
        /// </summary>
        /// <param name="finder"></param>
        private Returns InsertUlica(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            try
            {
                string sql =
                    " SELECT nzp_ul, nzp_raj, ulica, ulicareg, soato " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_ulica WHERE nzp_raj in " +
                    "    (select nzp_raj from " + Points.Pref + DBManager.sDataAliasRest + "s_rajon " +
                    "      where nzp_town in" +
                    "                    (select nzp_town from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    "                     where nzp_file = " + finder.nzp_file + "));";
                foreach (DataRow r in ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows)
                {
                    sql =
                        " SELECT * FROM " + finder.bank + DBManager.sDataAliasRest + "s_ulica " +
                        " WHERE nzp_ul = " + r["nzp_ul"];
                    if (ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows.Count == 0)
                    {
                        sql =
                            " INSERT INTO " + finder.bank + DBManager.sDataAliasRest + "s_ulica " +
                            " (nzp_ul, nzp_raj, ulica, ulicareg, soato) " +
                            " VALUES " +
                            "( " + r["nzp_ul"].ToString().Trim() + ", " + r["nzp_raj"] + ", " +
                            "'" + r["ulica"].ToString().Trim() + "', '" + r["ulicareg"].ToString().Trim() + "', '" +
                            r["soato"].ToString().Trim() + "'" +
                            ")";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка при добавлении адресного простанства на уровне улиц";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры InsertUlica: " + ex.Message);
            }
            ret.result = true;
            ret.text = "Успешно добавлено адресное простанство на уровне улиц";
            return ret;
        }
    }
}

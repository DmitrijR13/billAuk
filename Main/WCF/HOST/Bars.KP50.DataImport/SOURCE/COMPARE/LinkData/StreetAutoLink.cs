using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    /// <summary>
    /// Класс автоматического сопоставления улиц
    /// </summary>
    public class StreetLinkAuto : AbstractAutomaticLink
    {
        /// <summary>
        /// Создать временную таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override string CreateTempTable(IDbConnection conn_db)
        {
            string temp_table = "tmp_street_link_auto";

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " sid     integer, " +
                " town    varchar(100), " +
                " rajon   varchar(100), " +
                " ulica   varchar(120), " +
                " link_ul varchar(120), " +
                " cnt     integer default 0, " + // обязательное поле
                " nzp_ul  integer," +
                " nzp_raj integer)";
            ExecSQLWE(conn_db, sql);

            return temp_table;
        }

        /// <summary>
        /// Получить данные из буфера
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        protected override void GetBufferData(IDbConnection conn_db, int nzp_file)
        {
            string sql = "insert into " + tempTableName + " (sid, town, rajon, ulica, nzp_raj) " +
                " select fd.sid, fd.town, fd.rajon, trim(upper(fd.ulica)), fd.nzp_raj " +
                " From " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd " +
                " WHERE fd.nzp_file = " + nzp_file +
                "   AND " + sNvlWord + "(fd.nzp_ul, 0) = 0 ";
            ExecSQLWE(conn_db, sql);

            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_sid    on " + tempTableName + " (sid)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_1      on " + tempTableName + " (ulica, nzp_raj)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt    on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_ul on " + tempTableName + " (nzp_ul)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // ... замена ё
            sql = " update " + tempTableName + " set link_ul = REPLACE(ulica, 'Ё', 'Е')";
            ExecSQLWE(conn_db, sql);
        }

        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            // не сопоставлены населенные пункты для улиц
            string sql = "update " + tempTableName + " t set " +
                " cnt = " + WarningCode + ", message = " + globalsUtils.EStrNull("Не определен населенный пункт для улицы") +
                " where t.nzp_raj is null ";
            ExecSQLWE(conn_db, sql);

            string[] postfix = new string[] { "", " УЛ", " УЛ.", "  УЛ.", " ПЕР", " ПЕР.", " ПР-КТ", " ПР-КТ.", " КМ", " ПРОЕЗД", " ПРОСПЕКТ", " КМ УЛ.", " У", " У." };

            string where = "";
            // перебрать возможные типы улиц
            foreach (string post in postfix)
            {
                where = " and t.link_ul = trim(upper(trim(a.ulica))||'" + post + "') ";
                DefineLinkByCondition(conn_db, where);

                where = " and t.link_ul = trim('" + post + " " + "'||upper(trim(a.ulica))) ";
                DefineLinkByCondition(conn_db, where);
            }

            // ... соответствие 1 в 1
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd set " +
                " nzp_ul = (select t.nzp_ul from " + tempTableName + " t " +
                " where t.cnt = 1 and t.sid = fd.sid) " +
                " WHERE exists (select 1 from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) ";
            ExecSQLWE(conn_db, sql);
        }

        /// <summary>
        /// определение ссылок по условию
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="findKvar"></param>
        /// <param name="whereTempTable"></param>
        private void DefineLinkByCondition(IDbConnection conn_db, string condition, string whereTempTable = "")
        {
            // определить количество соответствующих записей
            string sql = "update " + tempTableName + " t set " +
                " cnt = (select count(*) From " + Points.Pref + DBManager.sDataAliasRest + "s_ulica a " +
                " Where t.nzp_raj = a.nzp_raj " +
                condition + ")" +
                " WHERE t.nzp_raj is not null " +
                "   and " + DBManager.sNvlWord + "(t.cnt, 0) <> 1 ";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            sql = "update " + tempTableName + " t set " +
                " nzp_ul = (select a.nzp_ul From " + Points.Pref + DBManager.sDataAliasRest + "s_ulica a " +
                " Where t.nzp_raj = a.nzp_raj " +
                condition + ") " +
                " WHERE t.cnt = 1 " +
                "   AND " + DBManager.sNvlWord + "(t.nzp_ul, 0) = 0";
            ExecSQLWE(conn_db, sql);
        }

        /// <summary>
        /// Формирование протокола
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="sbProtocol"></param>
        /// <param name="fileName"></param>
        /// <param name="fileNote"></param>
        protected override void GetProtocolInfo()
        {
            protocolFileName = "StreetLinkAuto";
            protocolNote = "Протокол автоматического сопоставления улиц";

            fieldsTempTable = new string[] { "town", "rajon", "ulica", "sid" };
            headersTempTable = new string[] { "Город/район", "населенный пункт", "улица", "ключ записи" };

            absentValuesMessage = "Найдены улицы, которых нет в базе данных";
            fewValuesMessage = "Найдены улицы, которым соответствуют несколько улиц в базе данных";
        }
    }
}
using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    /// <summary>
    /// Класс автоматического сопоставления улиц с названием '-'
    /// </summary>
    public class EmptyStreetLinkAuto : AbstractAutomaticLink
    {
        public EmptyStreetLinkAuto()
        {
            check_data_bank = true;
        }

        /// <summary>
        /// Создать временную таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override string CreateTempTable(IDbConnection conn_db)
        {
            string temp_table = "tmp_empty_street_link_auto";

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " sid       integer, " +
                " town      varchar(100), " +
                " rajon     varchar(100), " +
                " ulica     varchar(120), " +
                " cnt       integer, " + // обязательное поле
                " nzp_ul    integer," +
                " nzp_raj   integer)";
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
                " select fd.sid, fd.town, fd.rajon, trim(fd.ulica), fd.nzp_raj " +
                " From " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd " +
                " WHERE fd.nzp_file = " + nzp_file +
                "   AND " + sNvlWord + "(fd.nzp_ul, 0) = 0 " +
                "   AND trim(fd.ulica) = '-' ";
            ExecSQLWE(conn_db, sql);

            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_sid    on " + tempTableName + " (sid)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_1      on " + tempTableName + " (ulica, nzp_raj)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt    on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_ul on " + tempTableName + " (nzp_ul)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);
        }

        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            // ... не сопоставлены районы для улиц
            string sql = "update " + tempTableName + " t set " +
                " cnt = " + WarningCode + "," + " message = " + globalsUtils.EStrNull("Не определен населенный пункт для улицы") +
                " where t.nzp_raj is null ";
            ExecSQLWE(conn_db, sql);

            // ... получить количество соответствий
            sql = "update " + tempTableName + " t set " +
                " cnt = (select count(*) From " + Points.Pref + DBManager.sDataAliasRest + "s_ulica a " +
                " Where t.ulica = trim(upper(a.ulica)) " +
                "   and t.nzp_raj = a.nzp_raj) " +
                " WHERE t.nzp_raj is not null";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // ... получить ключи для однозначных соответствий
            sql = "update " + tempTableName + " t set " +
                " nzp_ul = (select a.nzp_ul From " + Points.Pref + DBManager.sDataAliasRest + "s_ulica a " +
                " Where t.ulica = trim(upper(a.ulica)) " +
                "   and t.nzp_raj = a.nzp_raj) " +
                " Where t.cnt = 1 ";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // ... соответствие 1 в 1
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd set " +
                " nzp_ul = (select t.nzp_ul from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) " +
                " WHERE exists (select 1 from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) ";
            ExecSQLWE(conn_db, sql);

            // ... добавить улицы
            // При добавлении улицы (AddNewStreet) определяются ссылки на буферные данные
            sql = "select nzp_raj, ulica from " + tempTableName + " where cnt = 0 ";
            IntfResultTableType rt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            List<int> selectedFiles = new List<int>();
            selectedFiles.Add(finder.nzp_file);

            ReturnsType ret = new ReturnsType();

            foreach (DataRow rr in rt.resultData.Rows)
            {
                UncomparedStreets finder_empty_street = new UncomparedStreets()
                {
                    nzp_raj = rr["nzp_raj"].ToString(),
                    nzp_ul = "",
                    ulica = rr["ulica"].ToString(),
                    bank = finder.bank,
                    nzp_user = finder.nzp_user
                };

                using (AddNewData nd = new AddNewData())
                {
                    ret = nd.AddNewStreet(finder_empty_street, selectedFiles, conn_db);
                }

                // ошибка при добавлении
                if (!ret.result)
                {
                    sql = "update " + tempTableName + " set cnt = " + WarningCode + ", " +
                        " message = " + globalsUtils.EStrNull(PrepareMessage(ret.text)) +
                        " where ulica = " + globalsUtils.EStrNull(finder_empty_street.ulica.ToString()) +
                            " and nzp_raj = " + finder_empty_street.nzp_raj +
                            " and cnt = 0 ";
                    ExecSQLWE(conn_db, sql);
                }
            }
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
            protocolFileName = "EmptyStreetLinkAuto";
            protocolNote = "Протокол автоматического добавления улиц с названием '-'";

            fieldsTempTable = new string[] { "town", "rajon", "ulica", "sid" };
            headersTempTable = new string[] { "Город/район", "населенный пункт", "улица", "ключ записи" };

            absentValuesMessage = "Были добавлены следующие улицы";
            fewValuesMessage = "Найдены улицы, которым соответствуют несколько улиц в базе данных";
        }
    }
}
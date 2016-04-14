using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    /// <summary>
    /// Класс автоматического сопоставления районов
    /// </summary>
    public class RajonLinkAuto : AbstractAutomaticLink
    {
        /// <summary>
        /// Создать временную таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override string CreateTempTable(IDbConnection conn_db)
        {
            string temp_table = "tmp_rajon_link_auto";

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " sid      integer, " +
                " town     varchar(100), " +
                " rajon    varchar(100), " +
                " cnt      integer, " + // обязательное поле
                " nzp_raj  integer," +
                " nzp_town integer)";
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
            string sql = "insert into " + tempTableName + " (sid, rajon, town, nzp_town) " +
                " select fd.sid, trim(upper(fd.rajon)), fd.town, fd.nzp_town " +
                " From " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd " +
                " WHERE fd.nzp_file = " + nzp_file +
                "   AND " + sNvlWord + "(fd.nzp_raj, 0) = 0 ";
            ExecSQLWE(conn_db, sql);

            // определить количество соответствующих записей
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_sid on " + tempTableName + " (sid)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_1   on " + tempTableName + " (rajon, nzp_town)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_raj on " + tempTableName + " (nzp_raj)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);
        }

        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            // не сопоставлены города для населенных пунктов
            string sql = "update " + tempTableName + " t set " +
                " cnt = " + WarningCode + ", message = " + globalsUtils.EStrNull("Не определен город/район для населенного пункта") +
                " where t.nzp_town is null ";
            ExecSQLWE(conn_db, sql);

            // определить количество сопоставлений
            sql = "update " + tempTableName + " t set " +
                " cnt = (select count(*) From " + Points.Pref + DBManager.sDataAliasRest + "s_rajon a " +
                " Where t.rajon = trim(upper(a.rajon)) " +
                "   and t.nzp_town = a.nzp_town) " +
                " WHERE t.nzp_town is not null ";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // проставить ключи для записей, для которых cnt = 1
            sql = "update " + tempTableName + " t set " +
                " nzp_raj = (select a.nzp_raj From " + Points.Pref + DBManager.sDataAliasRest + "s_rajon a " +
                " Where t.rajon = trim(upper(a.rajon)) " +
                "   and t.nzp_town = a.nzp_town) " +
                " Where t.cnt = 1";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // соответствие 1 в 1
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd set " +
                " nzp_raj = (select t.nzp_raj from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) " +
                " Where exists (select 1 from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) ";
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
            protocolFileName = "RajonLinkAuto";
            protocolNote = "Протокол автоматического сопоставления районов";

            fieldsTempTable = new string[] { "town", "rajon", "sid" };
            headersTempTable = new string[] { "Город/район", "населенный пункт", "ключ записи" };

            absentValuesMessage = "Найдены населенные пункты, которых нет в базе данных";
            fewValuesMessage = "Найдены населенные пункты, которым соответствуют несколько населенных пунктов в базе данных";
        }
    }
}
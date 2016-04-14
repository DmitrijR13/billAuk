using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public class ServiceAutoLink : AbstractAutomaticLink
    {
        /// <summary>
        /// Создать временную таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override string CreateTempTable(IDbConnection conn_db)
        {
            string temp_table = "tmp_service_link_auto";

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " id      integer, " +
                " service varchar(200), " +
                " cnt      integer, " + // обязательное поле
                " nzp_serv integer)";
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
            string sql = "insert into " + tempTableName + " (id, service) " +
                " select fd.id, upper(trim(fd.service)) " +
                " From " + Points.Pref + DBManager.sUploadAliasRest + "file_services fd " +
                " WHERE fd.nzp_file = " + nzp_file +
                "   AND " + sNvlWord + "(fd.nzp_serv, 0) = 0 ";
            ExecSQLWE(conn_db, sql);

            // определить количество соответствующих записей
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_id       on " + tempTableName + " (id)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_service  on " + tempTableName + " (service)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt      on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_serv on " + tempTableName + " (nzp_serv)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);
        }

        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            // определить количество сопоставлений
            string sql = "update " + tempTableName + " t set " +
                " cnt = (select count(*) From " + Points.Pref + DBManager.sKernelAliasRest + "services a Where t.service = upper(trim(a.service))) ";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // проставить ключи для записей, для которых cnt = 1
            sql = "update " + tempTableName + " t set " +
                " nzp_serv = (select a.nzp_serv From " + Points.Pref + DBManager.sKernelAliasRest + "services a Where t.service = upper(trim(a.service))) " +
                " Where t.cnt = 1";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // соответствие 1 в 1
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_services fd set " +
                " nzp_serv = (select t.nzp_serv from " + tempTableName + " t where t.cnt = 1 and t.id = fd.id) " +
                " Where exists (select 1 from " + tempTableName + " t where t.cnt = 1 and t.id = fd.id) ";
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
            protocolFileName = "ServiceLinkAuto";
            protocolNote = "Протокол автоматического сопоставления услуг";

            fieldsTempTable = new string[] { "service", "id" };
            headersTempTable = new string[] { "Услуга", "ключ записи" };

            absentValuesMessage = "Найдены услуги, которых нет в базе данных";
            fewValuesMessage = "Найдены услуги, которым соответствуют несколько услуг в базе данных";
        }
    }
}

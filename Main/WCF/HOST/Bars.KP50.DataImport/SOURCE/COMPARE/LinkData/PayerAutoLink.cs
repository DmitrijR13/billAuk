using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public class PayerAutoLink : AbstractAutomaticLink
    {
        private AutoLinkMode _autoLinkMode;

        public PayerAutoLink(AutoLinkMode linkMode)
        {
            _autoLinkMode = linkMode;

            if (_autoLinkMode == AutoLinkMode.Add)
            {
                // проверить, что указан банк данных
                check_data_bank = true;
            }
        }

        /// <summary>
        /// Создать временную таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override string CreateTempTable(IDbConnection conn_db)
        {
            string temp_table = "tmp_payer_link_auto";
            if (_autoLinkMode == AutoLinkMode.Add) temp_table += "_with_add";

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " id               integer, " +
                " cnt              integer, " +
                " nzp_payer        integer, " +
                " link_urlic_name  varchar(250), " +
                " urlic_name       varchar(250), " +
                " urlic_name_s     varchar(100))";
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
            string sql = "insert into " + tempTableName + " (id, urlic_name, urlic_name_s) " +
                " select fu.id, UPPER(TRIM(fu.urlic_name)), UPPER(TRIM(fu.urlic_name_s)) " +
                " From " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic fu " +
                " WHERE fu.nzp_file = " + nzp_file +
                "   AND " + sNvlWord + "(fu.nzp_payer, 0) = 0 ";
            ExecSQLWE(conn_db, sql);

            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_id         on " + tempTableName + " (id)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_1          on " + tempTableName + " (link_urlic_name, urlic_name_s)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt        on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_payer  on " + tempTableName + " (nzp_payer)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_urlic_name on " + tempTableName + " (urlic_name)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            sql = "SELECT point FROM " +
                Points.Pref + DBManager.sKernelAliasRest + "s_point p, " +
                Points.Pref + DBManager.sUploadAliasRest + "files_imported i " +
                " WHERE lower(trim(p.bd_kernel)) =  lower(trim(i.pref)) " +
                "   and i.nzp_file =  " + nzp_file;

            Returns ret = new Returns(true);
            string bd_name = ExecScalar(conn_db, sql, out ret, true).ToString();
            if (!ret.result) throw new Exception(ret.text);
            if (bd_name.Length > 3) bd_name = bd_name.Substring(0, 3);

            // ... добавить банк данных к сравниваемому именю
            sql = " update " + tempTableName + " set link_urlic_name = urlic_name || '(" + bd_name.Trim().ToUpper() + ")'";
            ExecSQLWE(conn_db, sql);
        }

        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            // почистить кривые ссылки
            string sql =
                    " UPDATE  " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic fu " +
                    " SET nzp_payer = null " +
                    " WHERE NOT EXISTS (SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_payer p WHERE p.nzp_payer = fu.nzp_payer) " +
                    " AND nzp_file = " + finder.nzp_file;
            ExecSQLWE(conn_db, sql);

            // определить количество соответствующих записей
            sql = "update " + tempTableName + " t set " +
                " cnt = (select count(*) From " + Points.Pref + DBManager.sKernelAliasRest + "s_payer a " +
                " WHERE t.link_urlic_name = UPPER(TRIM(a.npayer)) " +
                "   AND t.urlic_name_s    = UPPER(TRIM(a.payer)) ) ";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // проставить ключи
            sql = "update " + tempTableName + " t set " +
                " nzp_payer = (select a.nzp_payer From " + Points.Pref + DBManager.sKernelAliasRest + "s_payer a " +
                " WHERE t.link_urlic_name = UPPER(TRIM(a.npayer)) " +
                "   AND t.urlic_name_s    = UPPER(TRIM(a.payer)) ) " +
                " WHERE t.cnt = 1";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // соответствие 1 в 1
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic fu set " +
                " nzp_payer = (select t.nzp_payer from " + tempTableName + " t " +
                " where t.cnt = 1 and t.id = fu.id) " +
                " WHERE exists (select 1 from " + tempTableName + " t where t.cnt = 1 and t.id = fu.id) ";
            ExecSQLWE(conn_db, sql);

            // добавить юр. лица
            if (_autoLinkMode == AutoLinkMode.Add)
            {
                AddNewPayer(conn_db, finder);
            }
        }

        /// <summary>
        /// добавить новые юр. лица
        /// </summary>
        /// <param name="conn_db"></param>
        private void AddNewPayer(IDbConnection conn_db, FilesImported finder)
        {
            // ... добавить новые юр. лица
            string sql = "select urlic_name from " + tempTableName + " where cnt = 0 ";
            IntfResultTableType rt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            List<int> selectedFiles = new List<int>();
            selectedFiles.Add(finder.nzp_file);

            ReturnsType ret = new ReturnsType();
            foreach (DataRow rr in rt.resultData.Rows)
            {
                UncomparedPayer finder_payer = new UncomparedPayer()
                {
                    payer = rr["urlic_name"].ToString().Trim(),
                    bank = finder.bank,
                    nzp_user = finder.nzp_user
                };

                using (AddNewData nd = new AddNewData())
                {
                    ret = nd.AddNewPayer(finder_payer, selectedFiles, conn_db);
                }

                if (!ret.result)
                {
                    sql = "update " + tempTableName + " set cnt = " + WarningCode + ", " +
                        " message = " + globalsUtils.EStrNull(PrepareMessage(ret.text)) +
                        " where urlic_name = " + globalsUtils.EStrNull(finder_payer.payer);
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
            if (_autoLinkMode == AutoLinkMode.Add)
            {
                protocolFileName = "PayerLinkAutoWithAdd";
                protocolNote = "Протокол автоматического сопоставления юридических лиц с добавлением";
                absentValuesMessage = "Добавлены следующие юридические лица";
            }
            else
            {
                protocolFileName = "PayerLinkAuto";
                protocolNote = "Протокол автоматического сопоставления юридических лиц";
                absentValuesMessage = "Найдены юридические лица, которых нет в базе данных";
            }

            fieldsTempTable = new string[] { "urlic_name", "urlic_name_s", "id" };
            headersTempTable = new string[] { "Наименование ЮЛ", "краткое наименование", "ключ записи" };

            fewValuesMessage = "Найдены юридические лица, которым соответствуют несколько юридических лиц в базе данных";
        }
    }
}
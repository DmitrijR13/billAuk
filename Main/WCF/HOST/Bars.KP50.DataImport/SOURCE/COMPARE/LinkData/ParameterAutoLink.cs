using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;
using System.Collections.Generic;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public class ParameterAutoLink : AbstractAutomaticLink
    {
        private AutoLinkMode _autoLinkMode;
        private string[] prmTables = new string[] { "file_typeparams", "file_blag", "file_gaz", "file_voda" };
        private string[] prmTypes = new string[] { "доп. параметр", "тип благоустройства дома", "типы дома по газу", "тип дома по воде" };

        public ParameterAutoLink(AutoLinkMode linkMode)
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
            string temp_table = "tmp_parameter_link_auto";
            if (_autoLinkMode == AutoLinkMode.Add) temp_table += "_with_add";

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " id          integer, " +
                " nzp_prm     integer, " +
                " prm_type_id integer, " +
                " prm_num INTEGER," +
                " prm_type    varchar(255), " +
                " cnt         integer, " +
                " nzp_payer   integer, " +
                " prm_name    varchar(200) )";
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
            CompareParams(conn_db, nzp_file);

            string sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams " +
                         " SET prm_num = (SELECT CASE WHEN level_= 1 THEN 10 WHEN level_ = 2 THEN 2 " +
                         " WHEN level_ = 3 THEN 1 WHEN level_ = 4 OR level_ = 5 THEN 17 WHEN level_ = 6 THEN 9" +
                         " WHEN level_ = 7 THEN 5 END " +
                         " FROM  " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams p" + " " +
                         " WHERE p.id = " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams.id )" +
                         " WHERE EXISTS (SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams fp " +
                         " WHERE fp.nzp_file = " + nzp_file + " AND fp.id = " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams.id)";
            ExecSQLWE(conn_db, sql);

            sql = "insert into " + tempTableName + " (id, prm_type_id, prm_name, prm_type, prm_num) " +
                " select fp.id, 0, TRIM(UPPER(fp.prm_name)), " + globalsUtils.EStrNull(prmTypes[0]) + ", prm_num " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams fp " +
                " WHERE " + DBManager.sNvlWord + "(fp.nzp_prm, 0) = 0 " +
                " AND fp.nzp_file = " + nzp_file;
            ExecSQLWE(conn_db, sql);



            for (int prmTypeId = 1; prmTypeId <= 3; prmTypeId++)
            {
                sql = "insert into " + tempTableName + " (id, prm_type_id, prm_name, prm_type, prm_num) " +
                    " select fp.id, " + prmTypeId + ", TRIM(UPPER(fp.name)), " + globalsUtils.EStrNull(prmTypes[prmTypeId]) + ", 2" +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + prmTables[prmTypeId] + " fp " +
                    " WHERE " + DBManager.sNvlWord + "(fp.nzp_prm, 0) = 0 " +
                    "   AND fp.nzp_file = " + nzp_file;
                ExecSQLWE(conn_db, sql);
            }

            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_id      on " + tempTableName + " (id)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_1       on " + tempTableName + " (prm_type_id, prm_name)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt     on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_prm on " + tempTableName + " (nzp_prm)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);
        }

        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            string[] prmTables = new string[] { "file_typeparams", "file_blag", "file_gaz", "file_voda" };
            string sql = "";

            for (int prmTypeId = 0; prmTypeId <= 3; prmTypeId++)
            {
                // определить количество соответствующих записей
                sql = "update " + tempTableName + " t set " +
                    " cnt = (select count(*) From " + Points.Pref + DBManager.sKernelAliasRest + 
                    "prm_name a WHERE t.prm_name = TRIM(UPPER(a.name_prm)) AND a.prm_num = t.prm_num ) " +
                    " WHERE t.prm_type_id = " + prmTypeId ;
                ExecSQLWE(conn_db, sql);
                ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

                // проставить ключи
                sql = "update " + tempTableName + " t set " +
                    " nzp_prm = (select max(a.nzp_prm) From " + Points.Pref + DBManager.sKernelAliasRest +
                    "prm_name a WHERE t.prm_name = TRIM(UPPER(a.name_prm))  AND a.prm_num = t.prm_num  ) " +
                    " WHERE t.prm_type_id = " + prmTypeId +
                    "   AND t.cnt = 1";
                ExecSQLWE(conn_db, sql);
                ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

                // соответствие 1 в 1
                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + prmTables[prmTypeId] + " fp set " +
                    " nzp_prm = (select max(t.nzp_prm) from " + tempTableName + " t " +
                    " where t.cnt = 1 " + 
                    "   and t.id = fp.id " +
                    "   and t.prm_type_id = " + prmTypeId + ") " +
                    " WHERE exists (select 1 from " + tempTableName + " t " + 
                        " where t.cnt = 1 " +
                        "   and t.id = fp.id " +
                        "   and t.prm_type_id = " + prmTypeId + ")";
                ExecSQLWE(conn_db, sql);
            }

            // добавить юр. лица
            if (_autoLinkMode == AutoLinkMode.Add)
            {
                AddNewParams(conn_db, finder);
            }
        }

        /// <summary>
        /// добавить новые юр. лица
        /// </summary>
        /// <param name="conn_db"></param>
        private void AddNewParams(IDbConnection conn_db, FilesImported finder)
        {
            // ... добавить новые юр. лица
            string sql = "select prm_name, prm_type_id, prm_num from " + tempTableName + " where cnt = 0 ";
            IntfResultTableType rt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            int prm_type_id = -1;

            List<int> selectedFiles = new List<int>();
            selectedFiles.Add(finder.nzp_file);
            
            foreach (DataRow rr in rt.resultData.Rows)
            {
                prm_type_id = Convert.ToInt32(rr["prm_type_id"]);

                UncomparedParTypes finder_param = new UncomparedParTypes()
                {
                    name_prm = rr["prm_name"].ToString().Trim(),
                    prm_num = (Int32)rr["prm_num"],
                    bank = finder.bank,
                    nzp_user = finder.nzp_user
                };

                using (AddNewData nd = new AddNewData())
                {
                    switch (prm_type_id)
                    {
                        case 0:
                            // Типы доп. параметров
                            nd.AddNewParType(finder_param, selectedFiles, conn_db);
                            break;
                        case 1:
                            // Типы благоустройства дома
                            nd.AddNewParBlag(finder_param, selectedFiles, conn_db);
                            break;
                        case 2:
                            // Типы дома по газу
                            nd.AddNewParGas(finder_param, selectedFiles, conn_db);
                            break;
                        case 3:
                            // Типы дома по воде
                            nd.AddNewParWater(finder_param, selectedFiles, conn_db);
                            break;
                    }
                }
            } // foreach
        }

        protected override void GetProtocolInfo()
        {
            if (_autoLinkMode == AutoLinkMode.Add)
            {
                protocolFileName = "ParamLinkAuto";
                protocolNote = "Протокол автоматического сопоставления параметров с добавлением";
                absentValuesMessage = "Добавлены следующие параметры";
            }
            else
            {
                protocolFileName = "ParamLinkAutoWithoutAdd";
                protocolNote = "Протокол автоматического сопоставления параметров";
                absentValuesMessage = "Найдены параметры, которых нет в базе данных";
            }

            fieldsTempTable = new string[] { "prm_type", "prm_name", "id" };
            headersTempTable = new string[] { "Тип параметра", "название параметра", "ключ записи" };

            fewValuesMessage = "Найдены параметры, которым соответствуют несколько параметров в базе данных";
        }

        private void CompareParams(IDbConnection connDb, int nzp_file)
        {
            string sql = "SELECT v.version_name FROM " + Points.Pref + sUploadAliasRest + "files_imported f, " +
                         " " + Points.Pref + sUploadAliasRest + "file_versions v " +
                         " WHERE f.nzp_version = v.nzp_version AND f.nzp_file = " + nzp_file;

            DataTable dtLoad = DBManager.ExecSQLToTable(connDb, sql);
            string versionName = "";
            foreach (DataRow r in dtLoad.Rows)
            {
                versionName = r["version_name"].ToString();
            }

            if (versionName.Trim() == "1.3.8.1")
            {
                sql = " UPDATE " + Points.Pref + sUploadAliasRest + "file_typeparams SET nzp_prm = " +
                      " (SELECT nzp_prm FROM " + Points.Pref + sKernelAliasRest + "prm_name p WHERE p.nzp_prm = " + Points.Pref + sUploadAliasRest + "file_typeparams.id_prm) " +
                      " WHERE EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "prm_name n WHERE n.nzp_prm = " + Points.Pref + sUploadAliasRest + "file_typeparams.id_prm)";
                ExecSQL(connDb, sql);
            }
        }

    }
}

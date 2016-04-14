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
    /// Класс автоматического сопоставления улиц
    /// </summary>
    class HouseAutoLink : AbstractAutomaticLink
    {
        private AutoLinkMode _autoLinkMode;

        public HouseAutoLink(AutoLinkMode linkMode)
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
            string temp_table = "tmp_house_link_auto";
            if (_autoLinkMode == AutoLinkMode.Add)
            {
                temp_table = "tmp_house_link_auto_with_add";
            }

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " sid       integer, " +
                " town      varchar(100), " +
                " rajon     varchar(100), " +
                " ulica     varchar(120), " +
                " ndom      varchar(30), " +
                " nkor      varchar(10), " +
                " cnt       integer default 0, " + // обязательное поле
                " nzp_dom   integer, " +
                " nzp_area  integer, " +
                " area_id   " + DBManager.sDecimalType + "(18,0), " +
                " nzp_ul    integer, " +
                " nzp_raj   integer, " +
                " nzp_town  integer " +
                ")";
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
            string sql = "insert into " + tempTableName + " (sid, town, rajon, ulica, ndom, nkor, nzp_ul, nzp_raj, nzp_town, area_id) " +
                " select fd.sid, fd.town, fd.rajon, fd.ulica, trim(upper(fd.ndom)), trim(upper(fd.nkor)), fd.nzp_ul, fd.nzp_raj, fd.nzp_town, fd.area_id " +
                " From " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd " +
                " WHERE fd.nzp_file = " + nzp_file +
                "   AND " + DBManager.sNvlWord + "(fd.nzp_dom, 0) = 0 ";
            ExecSQLWE(conn_db, sql);

            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_sid     on " + tempTableName + " (sid)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_1       on " + tempTableName + " (ndom, nkor, nzp_ul)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt     on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_dom on " + tempTableName + " (nzp_dom)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            DefineArea(conn_db, nzp_file);
        }

        /// <summary>
        /// Определить УК
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_file"></param>
        private void DefineArea(IDbConnection conn_db, int nzp_file)
        {
            string tmp_area = tempTableName.Trim() + "_area";

            ExecSQL(conn_db, "drop table " + tmp_area, false);

            string sql = "create temp table " + tmp_area + " (" +
                " sid       integer, " +
                " nzp_area  integer " +
                ")";
            ExecSQLWE(conn_db, sql);

            sql = "insert into " + tmp_area + " (sid, nzp_area) " +
                " select t.sid, a.nzp_area " +
                " from " + tempTableName + " t, " +
                Points.Pref + DBManager.sDataAliasRest + "dom a, " +
                Points.Pref + DBManager.sDataAliasRest + "s_area s, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_urlic u " +
                " WHERE " + DBManager.sNvlWord + "(t.nzp_ul, 0) = a.nzp_ul " +
                "   and trim(upper(a.ndom)) = t.ndom " +
                "   and trim(upper(a.nkor)) = t.nkor" +
                "   and u.nzp_payer = s.nzp_payer " +
                "   and a.nzp_area = s.nzp_area " +
                "   and u.urlic_id = t.area_id " +
                "   and u.nzp_file = " + nzp_file +
                " group by 1,2 " +
                " having count(*) = 1 ";
            ExecSQLWE(conn_db, sql);

            ExecSQLWE(conn_db, "create index ix_" + tmp_area + "_sid on " + tmp_area + " (sid)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tmp_area);

            sql = "update " + tempTableName + " t set " +
                " nzp_area = (select a.nzp_area from " + tmp_area + " a where a.sid = t.sid)";
            ExecSQLWE(conn_db, sql);

            ExecSQL(conn_db, "drop table " + tmp_area, false);
        }

        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            // ... почистить кривые ссылки
            string sql = " UPDATE  " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd  " +
                " SET nzp_dom = null " +
                " WHERE not exists (SELECT 1 FROM " + Points.Pref + DBManager.sDataAliasRest + "dom d WHERE d.nzp_dom = fd.nzp_dom) " +
                "   and fd.nzp_file = " + finder.nzp_file;
            ExecSQLWE(conn_db, sql);

            // не сопоставлены улицы для домов
            sql = "update " + tempTableName + " t set " +
                " cnt = " + WarningCode + ", message = " + globalsUtils.EStrNull("Не определена улица для дома. ") +
                " where t.nzp_ul is null ";
            ExecSQLWE(conn_db, sql);

            string where =
               " AND trim(upper(a.ndom)) = t.ndom " +
               " AND trim(upper(a.nkor)) = t.nkor" +
               " AND a.nzp_area = t.nzp_area " +
               " AND a.nzp_ul > 0 " +
               " AND a.ndom is not null " +
               " AND a.nkor is not null";
            DefineLinkByCondition(conn_db, where, " and t.nzp_area is not null");

            where =
               " AND trim(upper(a.ndom)) = t.ndom " +
               " AND trim(upper(a.nkor)) = t.nkor" +
               " AND a.nzp_ul > 0 " +
               " AND a.ndom is not null " +
               " AND a.nkor is not null";
            DefineLinkByCondition(conn_db, where);

            // ... соответствие 1 в 1
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd set " +
                " nzp_dom = (select t.nzp_dom from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) " +
                " WHERE exists (select 1 from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) ";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + Points.Pref + DBManager.sUploadAliasRest + "file_dom ");

            // добавить дома
            if (_autoLinkMode == AutoLinkMode.Add)
            {
                AddNewHouse(conn_db, finder);
            }

            // ... обновить коды домов в file_kvar
            sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk SET " +
                " nzp_dom = (SELECT d.nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d " +
                "   WHERE d.id = fk.dom_id AND d.nzp_file = " + finder.nzp_file + ") " +
                " WHERE fk.nzp_file = " + finder.nzp_file;
            ExecSQLWE(conn_db, sql);

            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar ");
        }

        /// <summary>
        /// определение ссылок по условию
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="findKvar"></param>
        /// <param name="whereTempTable"></param>
        private void DefineLinkByCondition(IDbConnection conn_db, string condition, string whereTempTable = "")
        {
            string sql = "update " + tempTableName + " t set " +
               " cnt = (select count(*) From " + Points.Pref + DBManager.sDataAliasRest + "dom a " +
               " Where a.nzp_ul = t.nzp_ul " +
               condition + ") " +
               " WHERE t.nzp_ul is not null " +
               "    AND " + DBManager.sNvlWord + "(t.cnt, 0) <> 1 " +
               whereTempTable;
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            sql = "update " + tempTableName + " t set " +
                " nzp_dom = (select a.nzp_dom From " + Points.Pref + DBManager.sDataAliasRest + "dom a " +
                " WHERE a.nzp_ul = t.nzp_ul " +
                condition + ") " +
                " where t.cnt = 1 " +
                "   and " + DBManager.sNvlWord + "(t.nzp_dom, 0) = 0";
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);
        }

        /// <summary>
        /// добавить новые дома
        /// </summary>
        /// <param name="conn_db"></param>
        private void AddNewHouse(IDbConnection conn_db, FilesImported finder)
        {
            // ... добавить новые дома
            string sql = "select distinct ndom, nkor, nzp_dom, nzp_town, nzp_raj, nzp_ul, town, rajon, ulica " +
                " from " + tempTableName + " where cnt = 0 " +
                " order by ndom, town, rajon, ulica";
            IntfResultTableType rt = ClassDBUtils.OpenSQL(sql, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            List<int> selectedFiles = new List<int>();
            selectedFiles.Add(finder.nzp_file);

            ReturnsType ret = new ReturnsType(true);

            foreach (DataRow rr in rt.resultData.Rows)
            {
                UncomparedHouses finder_house = new UncomparedHouses()
                {
                    dom = rr["ndom"].ToString() + "/" + rr["nkor"],
                    nzp_dom = "",
                    nzp_ul = rr["nzp_ul"].ToString(),
                    nzp_raj = rr["nzp_raj"].ToString(),
                    nzp_town = rr["nzp_town"].ToString(),
                    bank = finder.bank,
                    nzp_user = finder.nzp_user
                };

                using (AddNewData nd = new AddNewData())
                {
                    ret = nd.AddNewHouse(finder_house, selectedFiles, conn_db);
                }

                // ошибка при добавлении
                if (!ret.result)
                {
                    sql = "update " + tempTableName + " set cnt = " + WarningCode + ", " +
                        " message = " + globalsUtils.EStrNull(PrepareMessage(ret.text)) +
                        " where ndom = " + globalsUtils.EStrNull(rr["ndom"].ToString()) +
                            " and nkor = " + globalsUtils.EStrNull(rr["nkor"].ToString()) +
                            " and nzp_ul = " + finder_house.nzp_ul +
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
            if (_autoLinkMode == AutoLinkMode.Add)
            {
                protocolFileName = "HouseLinkAuto";
                protocolNote = "Протокол автоматического сопоставления домов";
                absentValuesMessage = "Добавлены следующие дома";
            }
            else
            {
                protocolFileName = "HouseLinkAutoWithoutAdd";
                protocolNote = "Протокол автоматического сопоставления домов без добавления";
                absentValuesMessage = "Найдены дома, которых нет в базе данных";
            }

            fieldsTempTable = new string[] { "town", "rajon", "ulica", "ndom", "nkor", "sid" };
            headersTempTable = new string[] { "Город/район", "населенный пункт", "улица", "номер дома", "корпус", "ключ записи" };

            fewValuesMessage = "Найдены дома, которым соответствуют несколько домов в базе данных";
        }
    }
}
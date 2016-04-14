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
    class LsAutoLink : AbstractAutomaticLink
    {
        private DateTime _calcDate;
        private List<string> _prefList;

        /// <summary>
        /// Создать временную таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override string CreateTempTable(IDbConnection conn_db)
        {
            string temp_table = "tmp_ls_link_auto";

            ExecSQL(conn_db, "drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " sid      integer, " +
                " town     varchar(100), " +
                " rajon    varchar(100), " +
                " ulica    varchar(120), " +
                " ndom     varchar(30), " +
                " nkor     varchar(10), " +
                " nkvar    varchar(30), " +
                " nkvar_n  varchar(10), " +
                " id       varchar(40), " +
                " cnt      integer, " + // обязательное поле
                " nzp_dom  integer, " +
                " pref     varchar(200), " +
                " nzp_kvar integer " +
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
            string sql = "insert into " + tempTableName + " (sid, town, rajon, ulica, ndom, nkor, nkvar, nkvar_n, id, nzp_dom) " +
                " select fd.sid, dom.town, dom.rajon, dom.ulica, dom.ndom, dom.nkor, trim(upper(fd.nkvar)), trim(upper(fd.nkvar_n)), trim(upper(fd.id)), fd.nzp_dom " +
                " From " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fd " +
                    " left outer join " + Points.Pref + DBManager.sUploadAliasRest + "file_dom dom on dom.nzp_dom  = fd.nzp_dom and dom.nzp_file = fd.nzp_file " +
                " WHERE fd.nzp_file  = " + nzp_file +
                "   AND " + DBManager.sNvlWord + "(fd.nzp_kvar, 0) = 0 ";
            ExecSQLWE(conn_db, sql);

            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_sid      on " + tempTableName + " (sid)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_1        on " + tempTableName + " (nkvar_n, id, nzp_dom)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_2        on " + tempTableName + " (nkvar, nkvar_n, nzp_dom)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_cnt      on " + tempTableName + " (cnt)");
            ExecSQLWE(conn_db, "create index ix_" + tempTableName + "_nzp_kvar on " + tempTableName + " (nzp_kvar)");
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            // определить префиксы
            sql = "update " + tempTableName + " t set " +
                " pref = (select d.pref from " + Points.Pref + DBManager.sDataAliasRest + "dom d where d.nzp_dom = t.nzp_dom) " +
                " WHERE t.nzp_dom is not null ";
            ExecSQLWE(conn_db, sql);

            _calcDate = GetFileCalcDate(conn_db, nzp_file);
            _prefList = GetPrefList(conn_db);
        }

        /// <summary>
        /// Получить расчетный месяц из файла
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_file"></param>
        /// <returns></returns>
        private DateTime GetFileCalcDate(IDbConnection conn_db, int nzp_file)
        {
            IDataReader reader = null;
            DateTime calcDate = new DateTime();

            try
            {
                string sql = "select calc_date from " + Points.Pref + DBManager.sUploadAliasRest + "file_head where nzp_file = " + nzp_file;
                Returns ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (reader.Read())
                {
                    calcDate = Convert.ToDateTime(reader["calc_date"]);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return calcDate;
        }

        /// <summary>
        /// Получить список префиксов
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        private List<string> GetPrefList(IDbConnection conn_db)
        {
            IDataReader reader = null;
            List<string> list = new List<string>();

            try
            {
                string sql = "select distinct pref from " + tempTableName;
                Returns ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    list.Add(reader["pref"].ToString().Trim());
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return list;
        }


        /// <summary>
        /// Определить ссылки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override void DefineBufferLink(IDbConnection conn_db, FilesImported finder)
        {
            string sql = "";

            // не сопоставлены дома для ЛС
            sql = "update " + tempTableName + " t set " +
                " cnt = " + WarningCode + ", message = " + globalsUtils.EStrNull("Не определен дом для ЛС") +
                " where t.nzp_dom is null ";
            ExecSQLWE(conn_db, sql);

            // ... определить по номеру квартиры, номеру комнаты, коду ЛС
            string where =
                " AND trim(upper(a.nkvar))   = t.nkvar " +
                " AND trim(upper(a.nkvar_n)) = t.nkvar_n " +
                " AND trim(upper(a.remark))  = t.id";
            DefineLinkByCondition(conn_db, where);

            // ... определить по номеру квартиры, номеру комнаты
            where =
                " AND trim(upper(a.nkvar))   = t.nkvar " +
                " AND trim(upper(a.nkvar_n)) = t.nkvar_n ";
            DefineLinkByCondition(conn_db, where);

            // ... определить по номеру квартиры, коду ЛС и признаку, что ЛС открыт
            where =
                " AND trim(upper(a.nkvar))  = t.nkvar " +
                " AND trim(upper(a.remark)) = t.id " +
                " AND trim(a.is_open)       = '1' ";
            DefineLinkByCondition(conn_db, where);

            // ... определить по номеру квартиры, коду ЛС и наличию начислений
            _calcDate = _calcDate.AddMonths(-1);
            string charge = "";
            foreach (string pref in _prefList)
            {
                charge = pref + "_charge_" + (_calcDate.Year % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + _calcDate.Month.ToString("00");
                if (TempTableInWebCashe(charge))
                {
                    where =
                        " AND trim(upper(a.nkvar))   = t.nkvar " +
                        " AND trim(upper(a.remark))  = t.id " +
                        " AND a.nzp_kvar in (select cc.nzp_kvar from " + charge + " cc where cc.rsum_tarif > 0) " +
                        " AND a.nzp_kvar not in (select cc.nzp_kvar from " + charge + " cc where cc.nzp_serv = 1) ";

                    DefineLinkByCondition(conn_db, where, " AND t.pref = " + globalsUtils.EStrNull(pref));
                }
            }

            where =
                "   AND trim(upper(a.nkvar))  = t.nkvar " +
                "   AND trim(upper(a.remark)) = t.id ";
            DefineLinkByCondition(conn_db, where);

            // соответствие 1 в 1
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fd set " +
                " nzp_kvar = (select t.nzp_kvar from " + tempTableName + " t where t.cnt = 1 and t.sid = fd.sid) " +
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
            string sql = "update " + tempTableName + " t set " +
                " cnt = (select count(*) From " + Points.Pref + DBManager.sDataAliasRest + "kvar a " +
                " Where a.nzp_dom = t.nzp_dom " + condition + ") " +
                " WHERE t.nzp_dom is not null " +
                "   AND " + DBManager.sNvlWord + "(t.cnt, 0) <> 1 " +
                whereTempTable;
            ExecSQLWE(conn_db, sql);
            ExecSQLWE(conn_db, DBManager.sUpdStat + " " + tempTableName);

            sql = "update " + tempTableName + " t set " +
                " nzp_kvar = (select nzp_kvar From " + Points.Pref + DBManager.sDataAliasRest + "kvar a " +
                " Where a.nzp_dom = t.nzp_dom " + condition + ") " +
                " WHERE t.cnt = 1 " +
                "   AND " + DBManager.sNvlWord + "(t.nzp_kvar, 0) = 0" +
                whereTempTable;
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
            protocolFileName = "LsLinkAuto";
            protocolNote = "Протокол автоматического сопоставления лицевых счетов ";

            fieldsTempTable = new string[] { "town", "rajon", "ulica", "ndom", "nkor", "nkvar", "nkvar_n", "id", "sid" };
            headersTempTable = new string[] { "Город/район", "населенный пункт", "улица", "номер дома", "корпус", "кв. №", "ком. №", "код ЛС", "ключ записи" };

            absentValuesMessage = "Найдены ЛС, которых нет в базе данных";
            fewValuesMessage = "Найдены ЛС, которым соответствуют несколько ЛС в базе данных";
        }
    }
}
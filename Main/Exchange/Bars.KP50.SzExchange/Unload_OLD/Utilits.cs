using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.Unload
{
    public class Utilits : DataBaseHeadServer
    {
        /// <summary>
        /// Загрузка параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <param name="nzp_group"></param>
        /// <param name="dat_po"></param>
        /// <returns></returns>
        public Returns LoadParamsSz(FilesImported finder, IDbConnection conn_db, int nzp_group, string dat_po)
        {
            Returns ret = Utils.InitReturns();
            string sql;
            string s;

            MonitorLog.WriteLog("Старт загрузки параметров", MonitorLog.typelog.Info, true);

            sql = "DROP TABLE t_prm";
            DBManager.ExecSQL(conn_db, null, sql, false);

            sql = " CREATE TEMP TABLE t_prm" +
                  "(nzp_prm INTEGER)";
            DBManager.ExecSQL(conn_db, null, sql, true);

            #region Вставка параметров

            int[] vals = new int[] { 1, 4, 7, 19, 59, 107, 131, 133, 322, 323, 324, 325, 327, 449, 551, 552, 1199 };
            foreach (int val in vals)
            {
                sql = " INSERT INTO t_prm VALUES(" + val + ")";
                DBManager.ExecSQL(conn_db, null, sql, true);
            }

            #endregion Вставка параметров

            sql = " CREATE INDEX ix5346 ON t_prm(nzp_prm)";
            DBManager.ExecSQL(conn_db, null, sql, true);

            sql = " ANALYZE t_prm"; //Postgres
            //sql = " UPDATE STATISTICS FOR TABLE t_prm"; //Informix
            DBManager.ExecSQL(conn_db, null, sql, true);

            sql = "DROP TABLE t_prm_1";
            DBManager.ExecSQL(conn_db, null, sql, false);

            if (nzp_group > 0)
            {
                s = " AND nzp IN(" +
                    "  SELECT nzp FROM " + finder.bank + DBManager.sDataAliasRest + "link_group " +
                    "  WHERE nzp_group = cast(nzp_group as CHARACTER))";
            }
            else
                s = "";

            //sql = " SELECT nzp, nzp_prm, val_prm " +
            //      " FROM " + finder.bank + DBManager.sDataAliasRest + "prm_1 " +      //Informix
            //      " WHERE is_actual = 1" +
            //      " AND dat_s <= " + dat_po + s +  
            //      " INTO TEMP t_prm_1";
            sql = " SELECT nzp, nzp_prm, val_prm " +
                  " INTO TEMP t_prm_1" +
                  " FROM " + finder.bank + DBManager.sDataAliasRest + "prm_1 " +     //Postgres
                  " WHERE is_actual = 1" +
                  " AND dat_s <= " + " cast(' " + dat_po + " ' as date) " + s;
            DBManager.ExecSQL(conn_db, null, sql, true);

            sql = " CREATE INDEX ix_tmp_8752 ON t_prm_1" +
                  "(nzp_prm, nzp)";
            DBManager.ExecSQL(conn_db, null, sql, true);

            sql = " ANALYZE t_prm_1"; //Postgres
            //sql = " UPDATE STATISTICS FOR TABLE t_prm"; //Informix
            DBManager.ExecSQL(conn_db, null, sql, true);

            MonitorLog.WriteLog("Окончание загрузки параметров", MonitorLog.typelog.Info, true);

            return ret;
        }

        /// <summary>
        /// Признак выгрузки в Челны
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public bool UnloadForChelny(FilesImported finder, IDbConnection conn_db)
        {
            string sql;
            bool is_nch = false;
            sql =
                " SELECT COUNT (*) as cnt " +
                " FROM " + finder.bank + DBManager.sDataAliasRest + "prefer" +
                " WHERE p_name = 'nzp_town_rg' " +
                " AND p_value = '16587'";
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count > 0)
            {
                is_nch = true;
            }
            return is_nch;
        }

        /// <summary>
        /// Признак выгрузки в Казань
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public bool UnloadForKazan(FilesImported finder, IDbConnection conn_db)
        {
            string sql;
            bool is_kazan = false;

            //проверка признака выгрузки в Казань ДОДЕЛАТЬ!


            sql = " "; //проверка по каким таблицам?
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count == 0)
                is_kazan = true;

            return is_kazan;
        }

        /// <summary>
        /// Загрузка справочника с территориями
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        public void LoadArea(FilesImported finder, IDbConnection conn_db)
        {
            string sql;

            sql = " DROP TABLE t_area";
            DBManager.ExecSQL(conn_db, null, sql, false);

            sql = " CREATE TEMP TABLE t_area (" +
                       " nzp_area INTEGER, " +
                       " area CHAR(40), " +
                       " typehos INTEGER " +
                       ")";
            DBManager.ExecSQL(conn_db, null, sql, true);

            sql = " DELETE FROM t_area";
            DBManager.ExecSQL(conn_db, null, sql, true);

            sql =
                " SELECT nzp_area, area, 1 as typehos " +
                " FROM " + finder.bank + DBManager.sDataAliasRest + "s_area order by 2";
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            foreach (DataRow rr in dt.Rows)
            {
                sql = " INSERT INTO t_area(nzp_area, area, typehos) " +
                      " VALUES(" + rr["nzp_area"].ToString() + ", " + rr["area"].ToString() + ", " + rr["typehos"] +
                      " )";
                DBManager.ExecSQL(conn_db, null, sql, true);
            }

        }

        /// <summary>
        /// Группы с невыгруженными ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public int ExcludeLs(FilesImported finder, IDbConnection conn_db)
        {
            string sql;

            sql = " INSERT INTO " + DBManager.sDataAliasRest + " s_group (ngroup) " +
                  " VALUES (' Выгрузка в СЗ  " + DateTime.Now.ToString() + " Не выгруженные ЛС') ";
            DBManager.ExecSQL(conn_db, null, sql, true);

            int nzpBadGroup = GetSerialValue(conn_db);

            return nzpBadGroup;
        }

        /// <summary>
        /// Получение наименования банка
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        public void SetBankName(FilesImported finder, IDbConnection conn_db)
        {
            string sql;

            sql =
                " SELECT val_prm " +
                " FROM " + finder.bank + DBManager.sDataAliasRest + "prm_10 " +
                " WHERE nzp_prm = 164 " +
                " AND is_actual <> 100 " +
                " AND dat_s <= " + DateTime.Now.ToString() + " " +          //'and dat_s<="today" and dat_po>="today" '; dat_s<="today" - ??
                " AND dat_po >= " + DateTime.Now.ToString() + " ";
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();

            if (dt.Rows.Count != 0)
            {
                foreach (DataRow rr in dt.Rows)
                {
                    MonitorLog.WriteLog("Выгрузка от Управляющей компании (" + rr["val_prm"] + ")", MonitorLog.typelog.Info, true);
                }
            }
            else
                MonitorLog.WriteLog("Выгрузка от Управляющей компании", MonitorLog.typelog.Info, true);
        }

        /// <summary>
        /// Удаление временных таблиц
        /// </summary>
        /// <param name="conn_db"></param>
        public void DropTempTable(IDbConnection conn_db)
        {
            string sql;
            string[] table =
            {
                "t_raj", "t_adres", "t_gil", "t_pere", "t_selflgot", 
                "t_semlgot", "t_charge266", "sel_minlg", "tmp_lgcharge2", 
                "t_pere1", "t_area", "t_badlgot"
            };

            foreach (var s in table)
            {
                sql =
                " DROP TABLE " + s;
                DBManager.ExecSQL(conn_db, null, sql, false);
            }
        }

        /// <summary>
        /// Получаем строку с допустимыми льготами
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <param name="result"></param>
        public void GetLgotaString(FilesImported finder, IDbConnection conn_db)
        {
            string LgotKatSpis = "";
            string sql;


            sql = " SELECT kod_cz " +
                  " FROM s_calc_lgota order by 1 ";
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count != 0)
            {
                foreach (DataRow rr in dt.Rows)
                {
                    LgotKatSpis = LgotKatSpis + '|' + rr["kod_cz"].ToString();

                    if (LgotKatSpis != "")
                    {
                        LgotKatSpis = LgotKatSpis + '|';
                    }
                    //result.Append(LgotKatSpis);
                }
            }
        }
    }
}

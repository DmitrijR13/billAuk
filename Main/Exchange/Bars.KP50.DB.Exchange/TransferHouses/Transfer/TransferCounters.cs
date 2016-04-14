using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Castle.Core.Internal;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using DataTable = System.Data.DataTable;
using Points = STCLINE.KP50.Interfaces.Points;

namespace Bars.KP50.DB.Exchange.TransferHouses
{
    [TransferAttributes(Name = "counters", Descr = "Перенос показаний счетчиков", Priority = TransferPriority.Counters, Enabled = true)]
    public class TransferCounters : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            string sql;

            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var _fields = GetFields("counters", HouseParams.fPoint.pref + sDataAliasRest);

                //проверка на пересечение nzp_counter при переносе в банк приемник
                sql =
                    " DROP TABLE temp_tt";
                ExecSQL(sql);
                sql =
                    " SELECT DISTINCT nzp_counter " +
                    " INTO TEMP temp_tt" +
                    " FROM " + HouseParams.fPoint.pref + "_data.counters " +
                    " WHERE nzp_counter IN " +
                    "   ( SELECT nzp_counter " +
                    "     FROM " + HouseParams.tPoint.pref + "_data.counters)" +
                    " AND nzp_kvar IN (" + where + ")";
                ExecSQL(sql);

                sql =
                    " SELECT * FROM temp_tt";
                DataTable dt3 = ExecSQLToTable(sql);
                //если пересечения по коду есть, то перенумеровываем эти коды по переносимым ЛС
                if (dt3.Rows.Count > 0)
                {
                    sql =
                        " DROP TABLE temp_count";
                    ExecSQL(sql);

                    sql =
                        " SELECT nzp_counter, nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_prov," +
                        " dat_provnext, dat_uchet, val_cnt, norma, is_actual, nzp_user, dat_when, dat_close," +
                        " cur_unl, nzp_wp, ist, dat_oblom, dat_poch, dat_del, user_del, month_calc, dat_s, dat_po, dat_block, user_block" +
                        " INTO TEMP temp_count" +
                        " FROM " + HouseParams.fPoint.pref + "_data.counters" +
                        " WHERE nzp_counter IN (SELECT nzp_counter FROM temp_tt)";
                    ExecSQL(sql);

                    //создаем промежуточную таблицу
                    ExecSQL(string.Format(" DROP TABLE {0}counters_arch", HouseParams.fPoint.pref + sDataAliasRest),
                        false);
                    
                    ExecSQL(
                        string.Format(
                            " CREATE TABLE {0}counters_arch(new_nzp_counter integer, " +
                            " nzp_counter integer, nzp_kvar integer, num_ls integer, nzp_serv integer, nzp_cnttype integer, num_cnt character(40)," +
                            " dat_prov date, dat_provnext date, dat_uchet date, val_cnt double precision, " +
                            " norma double precision, is_actual integer, nzp_user integer, dat_when date, dat_close date, " +
                            " cur_unl integer default 0, nzp_wp integer default 1, ist integer default 0, dat_oblom date, " +
                            " dat_poch date, dat_del date, user_del integer, month_calc date, dat_s date, dat_po date," +
                            " dat_block timestamp without time zone, user_block integer)",
                            HouseParams.fPoint.pref + sDataAliasRest));

                    //переносим nzp_counter из временной таблицы в промежуточную таблицу
                    ExecSQL(string.Format(" INSERT INTO {0}counters_arch" +
                                          " (nzp_counter, nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_prov," +
                                          " dat_provnext, dat_uchet, val_cnt, norma, is_actual, nzp_user, dat_when, dat_close," +
                                          " cur_unl, nzp_wp, ist, dat_oblom, dat_poch, dat_del, user_del, month_calc, dat_s, " +
                                          " dat_po, dat_block, user_block) " +
                                          " SELECT * FROM temp_count", HouseParams.fPoint.pref + sDataAliasRest));

                    //проставляем новые кода new_nzp_counter из таблицы counters_spis_arch
                    sql =
                        " UPDATE " + HouseParams.fPoint.pref + sDataAliasRest + "counters_arch c" +
                        " SET new_nzp_counter = (" +
                        " SELECT new_nzp_counter " +
                        " FROM " + HouseParams.fPoint.pref + sDataAliasRest + "counters_spis_arch cs" +
                        " WHERE c.nzp_counter = cs.nzp_counter)";
                    ExecSQL(sql);

                    //перенос данных из старого банка в новый
                    ExecSQL(string.Format(" INSERT INTO {0}counters " +
                                          " (nzp_counter, nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_prov," +
                                          " dat_provnext, dat_uchet, val_cnt, norma, is_actual, nzp_user, dat_when, dat_close," +
                                          " cur_unl, nzp_wp, ist, dat_oblom, dat_poch, dat_del, user_del, month_calc, dat_s, " +
                                          " dat_po, dat_block, user_block)" +
                                          " SELECT new_nzp_counter, nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_prov," +
                                          " dat_provnext, dat_uchet, val_cnt, norma, is_actual, nzp_user, dat_when, dat_close," +
                                          " cur_unl, nzp_wp, ist, dat_oblom, dat_poch, dat_del, user_del, month_calc, dat_s, " +
                                          " dat_po, dat_block, user_block " +
                                          " FROM {1}counters_arch where nzp_kvar in ({2})",
                        HouseParams.tPoint.pref + sDataAliasRest, HouseParams.fPoint.pref + sDataAliasRest, where));
                }
                else
                {
                    //перенос данных из старогог банка в новый
                    ExecSQL(string.Format(" insert into {0}counters({3})" +
                                " (select {3} from {1}counters where nzp_kvar in ({2}))",
                            HouseParams.tPoint.pref + sDataAliasRest,
                            HouseParams.fPoint.pref + sDataAliasRest, where, _fields));
                }
                
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                    
                ExecSQL(string.Format("delete from {0}counters where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}counters where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("counters", HouseParams.fPoint.pref + sDataAliasRest);
                    
                ExecSQL(string.Format(" insert into {0}counters({3})" +
                            " (select {3} from {1}counters where nzp_kvar in ({2}))",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("counters", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("counters", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "counters_xx", Descr = "Перенос счетчиков", Priority = TransferPriority.Counters_xx, Enabled = true)]
    public class TransferCounters_xx : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            string sql;
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                sql =
                    " SELECT TRIM(SUBSTRING(table_schema from position('_' in table_schema) + 1 for length(table_schema) - 1)) as table_schema, " +
                    " TRIM(table_name) as table_name " +
                    " FROM information_schema.tables " +
                    " WHERE table_schema like '" + HouseParams.fPoint.pref + "_charge___' " +
                    " AND table_name like 'counters___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                        ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " (select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2}))",
                            HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString(),
                            HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString(), where, _fields));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            string sql;

            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                sql =
                   " SELECT TRIM(SUBSTRING(table_schema from position('_' in table_schema) + 1 for length(table_schema) - 1)) as table_schema, " +
                   " TRIM(table_name) as table_name " +
                   " FROM information_schema.tables " +
                   " WHERE table_schema like '" + HouseParams.fPoint.pref + "_charge___' " +
                   " AND table_name like 'counters___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                        ExecSQL(string.Format("delete from {0}." + rr["table_name"].ToString() + " where nzp_kvar in ({1})",
                            HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString(), where));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            string sql;

            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                sql =
                   " SELECT TRIM(SUBSTRING(table_schema from position('_' in table_schema) + 1 for length(table_schema) - 1)) as table_schema, " +
                   " TRIM(table_name) as table_name " +
                   " FROM information_schema.tables " +
                   " WHERE table_schema like '" + HouseParams.fPoint.pref + "_charge___' " +
                   " AND table_name like 'counters___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                        
                    ExecSQL(string.Format("delete from {0}." + rr["table_name"].ToString() + " where nzp_kvar in ({1})",
                            HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString(), where));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            string sql;

            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                sql =
                   " SELECT TRIM(SUBSTRING(table_schema from position('_' in table_schema) + 1 for length(table_schema) - 1)) as table_schema, " +
                   " TRIM(table_name) as table_name " +
                   " FROM information_schema.tables " +
                   " WHERE table_schema like '" + HouseParams.fPoint.pref + "_charge___' " +
                   " AND table_name like 'counters___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                        ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " (select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2}))",
                            HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString(),
                            HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString(), where, _fields));

                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            string sql;

            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                sql =
                   " SELECT TRIM(SUBSTRING(table_schema from position('_' in table_schema) + 1 for length(table_schema) - 1)) as table_schema, " +
                   " TRIM(table_name) as table_name " +
                   " FROM information_schema.tables " +
                   " WHERE table_schema like '" + HouseParams.fPoint.pref + "_charge___' " +
                   " AND table_name like 'counters___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var obj1 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0}." + rr["table_name"].ToString() + " where nzp_kvar in ({1})",
                            HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString(), where));
                    var obj2 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0}." + rr["table_name"].ToString() + " where nzp_kvar in ({1})",
                            HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString(), where));
                    if (obj1 != obj2)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            string sql;
            
            sql =
                   " SELECT TRIM(SUBSTRING(table_schema from position('_' in table_schema) + 1 for length(table_schema) - 1)) as table_schema, " +
                   " TRIM(table_name) as table_name " +
                   " FROM information_schema.tables " +
                   " WHERE table_schema like '" + HouseParams.fPoint.pref + "_charge___' " +
                   " AND table_name like 'counters___'";
            DataTable dt = ExecSQLToTable(sql);

            foreach (DataRow rr in dt.Rows)
            {
                var oldBankTableField = GetTableFieldsList(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                var newBankTableField = GetTableFieldsList(rr["table_name"].ToString(), HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString());
                if ((oldBankTableField.Any(field => !newBankTableField.Contains(field))) && newBankTableField.Count != 0)
                {
                    return false;
                }
                //если кол-во полей в таблице в которую переносим = 0, значит ее не существует, поэтому создаем ее
                if (newBankTableField.Count == 0)
                {
                    sql =
                        " SELECT column_name AS name, data_type AS type, column_default " +
                        " FROM information_schema.columns " +
                        " WHERE table_name = '" + rr["table_name"] + "' " +
                        " AND table_schema = '" + HouseParams.fPoint.pref + "_" + rr["table_schema"] + "' ";
                    DataTable data_t = ExecSQLToTable(sql);

                    sql =
                        " CREATE TABLE " + HouseParams.tPoint.pref + "_" + rr["table_schema"] + "." +
                        rr["table_name"] + " (";
                    foreach (DataRow r in data_t.Rows)
                    {
                        sql += r["name"] + "  " + (r["column_default"].ToString().IndexOf("next", StringComparison.InvariantCulture) > -1 ? "SERIAL NOT NULL" : r["type"].ToString() +
                            (!r["column_default"].ToString().IsNullOrEmpty() ? " default " + r["column_default"] : " ")) + ", ";
                    }
                    sql += ")";
                    sql = sql.Replace(", )", ")");
                    ExecSQL(sql);

                }
            }
            return true;
        }
    }

    [TransferAttributes(Name = "counters_spis", Descr = "Перенос счетчиков", Priority = TransferPriority.CounterSpis, Enabled = true)]
    public class TransferCounterSpis : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                string sql;
                var where = string.Join(",", kvarList.ToArray());

                //проверка на пересечение nzp_counter при переносе в банк приемник
                sql =
                    " DROP TABLE temp_tt";
                ExecSQL(sql);

                sql =
                    " SELECT DISTINCT nzp_counter " +
                    " INTO TEMP temp_tt" +
                    " FROM " + HouseParams.fPoint.pref + "_data.counters_spis " +
                    " WHERE nzp_counter IN " +
                    "   ( SELECT nzp_counter " +
                    "     FROM " + HouseParams.tPoint.pref + "_data.counters_spis)" +
                    " AND nzp IN (" + where + ")" +
                    " AND nzp_type = 3";
                ExecSQL(sql);

                sql =
                    " SELECT * FROM temp_tt";
                DataTable dt3 = ExecSQLToTable(sql);

                //если пересечения по коду есть, то перенумеровываем эти коды по переносимым ЛС
                if (dt3.Rows.Count > 0)
                {
                    sql =
                        " DROP TABLE temp_cr";
                    ExecSQL(sql, false);

                    sql =
                        " SELECT nzp_counter, nzp_type, nzp, nzp_serv, nzp_cnttype, num_cnt, is_gkal, kod_pu, kod_info, dat_prov, " +
                        " dat_provnext, dat_oblom, dat_poch, dat_close, comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl," +
                        " cnt_ls, dat_block, user_block, month_calc, user_del, dat_del, dat_s, dat_po" +
                        " INTO TEMP temp_cr" +
                        " FROM " + HouseParams.fPoint.pref + "_data.counters_spis" +
                        " WHERE nzp_counter IN (SELECT nzp_counter FROM temp_tt)";
                    ExecSQL(sql);

                    //создаем промежуточную таблицу 
                    ExecSQL(
                        string.Format(" DROP TABLE {0}counters_spis_arch", HouseParams.fPoint.pref + sDataAliasRest),
                        false);
                    
                    ExecSQL(
                        string.Format(
                            " CREATE TABLE {0}counters_spis_arch(new_nzp_counter integer default nextval('{1}counters_spis_nzp_counter_seq'::regclass), " +
                            " nzp_counter integer, nzp_type integer, nzp integer, nzp_serv integer, nzp_cnttype integer, num_cnt character(40)," +
                            " is_gkal integer, kod_pu integer, kod_info integer, dat_prov date, dat_provnext date, " +
                            " dat_oblom date, dat_poch date, dat_close date, comment character(60), " +
                            " is_actual integer, nzp_cnt integer, nzp_user integer, dat_when date, " +
                            " is_pl integer, cnt_ls integer, dat_block date, user_block integer, month_calc date, " +
                            " user_del integer, dat_del date, dat_s date, dat_po date)",
                            HouseParams.fPoint.pref + sDataAliasRest, Points.Pref + DBManager.sDataAliasRest));

                    //переносим nzp_counter из временной таблицы в промежуточную таблицу, получая таким образом новые коды new_nzp_counter
                    ExecSQL(string.Format(" INSERT INTO {0}counters_spis_arch" +
                                          " (nzp_counter, nzp_type, nzp, nzp_serv, nzp_cnttype, num_cnt, is_gkal, kod_pu, kod_info, dat_prov, " +
                                          " dat_provnext, dat_oblom, dat_poch, dat_close, comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl," +
                                          " cnt_ls, dat_block, user_block, month_calc, user_del, dat_del, dat_s, dat_po) " +
                                          " SELECT * FROM temp_cr", HouseParams.fPoint.pref + sDataAliasRest));

                    //перенос данных из старого банка в новый
                    ExecSQL(string.Format(" INSERT INTO {0}counters_spis " +
                                          " ( nzp_counter, nzp_type, nzp, nzp_serv, nzp_cnttype, num_cnt, is_gkal, kod_pu, kod_info, dat_prov, " +
                                          " dat_provnext, dat_oblom, dat_poch, dat_close, comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl," +
                                          " cnt_ls, dat_block, user_block, month_calc, user_del, dat_del, dat_s, dat_po)" +
                                          " SELECT new_nzp_counter, nzp_type, nzp, nzp_serv, nzp_cnttype, num_cnt, is_gkal, kod_pu, kod_info, dat_prov, " +
                                          " dat_provnext, dat_oblom, dat_poch, dat_close, comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl," +
                                          " cnt_ls, dat_block, user_block, month_calc, user_del, dat_del, dat_s, dat_po " +
                                          " FROM {1}counters_spis_arch ",
                        HouseParams.tPoint.pref + sDataAliasRest, HouseParams.fPoint.pref + sDataAliasRest));
                }
                else
                {
                    var _fields = GetFields("counters_spis", HouseParams.fPoint.pref + sDataAliasRest);
                    //тип = 3 - ЛС
                    ExecSQL(string.Format(" insert into {0}counters_spis(nzp_counter, {3}) " +
                                          " select nzp_counter, {3} " +
                                          " from {1}counters_spis where nzp in ({2}) and nzp_type = 3",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, where, _fields));
                }
                
            }
                //тип = 1 - дом
                ExecSQL(string.Format(" insert into {0}counters_spis " +
                                      " select * " +
                                      " from {1}counters_spis where nzp = {2} and nzp_type = 1 and nzp_counter not in (select nzp_counter from {0}counters_spis)",
                           HouseParams.tPoint.pref + sDataAliasRest,
                           HouseParams.fPoint.pref + sDataAliasRest, HouseParams.current_house.nzp_dom));
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}counters_spis where nzp in ({1}) and nzp_type = 3",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
            ExecSQL(string.Format("delete from {0}counters_spis where nzp = {1} and nzp_type = 1",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.current_house.nzp_dom));
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}counters_spis where nzp in ({1}) and nzp_type = 3",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
            ExecSQL(string.Format("delete from {0}counters_spis where nzp = {1} and nzp_type = 1",
                      HouseParams.tPoint.pref + sDataAliasRest,
                      HouseParams.current_house.nzp_dom));
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("counters_spis", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}counters_spis({3}) " +
                                          " select {3} " +
                                          " from {1}counters_spis where nzp in ({2}) and nzp_type = 3",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));
            }
            ExecSQL(string.Format(" insert into {0}counters_spis " +
                                         " select * " +
                                         " from {1}counters_spis where nzp = {2} and nzp_type = 1",
                       HouseParams.fPoint.pref + sDataAliasRest,
                       HouseParams.tPoint.pref + sDataAliasRest, HouseParams.current_house.nzp_dom));
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters_spis where (nzp in ({1}) and nzp_type = 3) or (nzp = {2} and nzp_type = 1)",
                        HouseParams.fPoint.pref + sDataAliasRest, where, HouseParams.current_house.nzp_dom));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters_spis where (nzp in ({1}) and nzp_type = 3) or (nzp = {2} and nzp_type = 1)",
                        HouseParams.tPoint.pref + sDataAliasRest, where, HouseParams.current_house.nzp_dom));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("counters_spis", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("counters_spis", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "prm_17", Descr = "Перенос параметров счетчиков", Priority = TransferPriority.Prm17, Enabled = false)]
    public class TransferPrm17 : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var counterList = GetNzpCounters(HouseParams.fPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                var _fields = GetFields("prm_17", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format(" insert into {0}prm_17({3}) " +
                                          " (select {3} from {1}prm_17 where nzp = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_counter, _fields));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var counterList = GetNzpCounters(HouseParams.fPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format("delete from {0}prm_17 where nzp = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_counter));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var counterList = GetNzpCounters(HouseParams.tPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format("delete from {0}prm_17 where nzp = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_counter));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var counterList = GetNzpCounters(HouseParams.tPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                var _fields = GetFields("prm_17", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format(" insert into {0}prm_17({3}) " +
                                          " (select {3} from {1}prm_17 where nzp = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_counter, _fields));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var counterList = GetNzpCounters(HouseParams.tPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                var where = string.Join(",", counterList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_17 where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_17 where nzp in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("prm_17", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("prm_17", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }

        private List<int> GetNzpCounters(string pref)
        {
            var where = string.Join(",", GetKvars().ToArray());
            if (where.Length == 0) return new List<int>();
            var resList = new List<int>();
            MyDataReader reader;
            ExecRead(out reader, string.Format("select nzp_counter from {0}counter_spis where (nzp in ({1}) and nzp_type = 3) or (nzp = {2} and nzp_type = 1)",
                pref, where, HouseParams.current_house.nzp_dom));
            while (reader.Read())
            {
                resList.Add(reader["nzp_counter"] != DBNull.Value ? Convert.ToInt32(reader["nzp_counter"]) : 0);
            }
            reader.Close();
            return resList;
        }
    }

    [TransferAttributes(Name = "counters_dom", Descr = "Перенос показаний домовых счетчиков", Priority = TransferPriority.CountersDom, Enabled = true)]
    public class TransferCountersDom : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("counters_dom", HouseParams.fPoint.pref + sDataAliasRest);
            ExecSQL(string.Format("insert into {0}counters_dom({3}) (select {3} from {1}counters_dom where nzp_dom = {2})", HouseParams.tPoint.pref + sDataAliasRest,
                HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override void DeleteDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}counters_dom where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest,
                 house.nzp_dom));
        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}counters_dom where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest,
                 house.nzp_dom));
        }

        protected override void RollbackDeletedData()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("counters_dom", HouseParams.fPoint.pref + sDataAliasRest);
            ExecSQL(string.Format("insert into {0}counters_dom({3}) (select {3} from {1}counters_dom where nzp_dom = {2})", HouseParams.fPoint.pref + sDataAliasRest,
                HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override bool CompareDataInTables()
        {
            var house = HouseParams.current_house;
            var obj1 = ExecScalar<Int32>(string.Format("select count(*) from {0}counters_dom where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            var obj2 = ExecScalar<Int32>(string.Format("select count(*) from {0}counters_dom where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            if (obj1 != obj2)
            {
                return false;
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("counters_dom", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("counters_dom", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "counters_domspis", Descr = "Перенос домовых счетчиков", Priority = TransferPriority.CountersDomSpis, Enabled = true)]
    public class TransferCountersDomSpis : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("counters_domspis", HouseParams.fPoint.pref + sDataAliasRest);
            ExecSQL(string.Format("insert into {0}counters_domspis({3}) (select {3} from {1}counters_domspis where nzp_dom = {2})", HouseParams.tPoint.pref + sDataAliasRest,
                HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override void DeleteDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}counters_domspis where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest,
                 house.nzp_dom));
        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}counters_domspis where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest,
                 house.nzp_dom));
        }

        protected override void RollbackDeletedData()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("counters_domspis", HouseParams.fPoint.pref + sDataAliasRest);
            ExecSQL(string.Format("insert into {0}counters_domspis({3}) (select {3} from {1}counters_domspis where nzp_dom = {2})", HouseParams.fPoint.pref + sDataAliasRest,
                HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override bool CompareDataInTables()
        {
            var house = HouseParams.current_house;
            var obj1 = ExecScalar<Int32>(string.Format("select count(*) from {0}counters_domspis where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            var obj2 = ExecScalar<Int32>(string.Format("select count(*) from {0}counters_domspis where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            if (obj1 != obj2)
            {
                return false;
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("counters_domspis", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("counters_domspis", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "counters_link", Descr = "Перенос счетчиков", Priority = TransferPriority.CountersLink, Enabled = true)]
    public class TransferCountersLink : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("counters_link", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}counters_link({3})" +
                            " (select {3} from {1}counters_link where nzp_kvar in ({2}))",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}counters_link where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}counters_link where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("counters_link", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}counters_link({3})" +
                            " (select {3} from {1}counters_link where nzp_kvar in ({2}))",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters_link where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters_link where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("counters_link", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("counters_link", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "counters_group", Descr = "Перенос показаний групповых счетчиков", Priority = TransferPriority.CountersGroup, Enabled = true)]
    public class TransferCountersGroup : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {
            var counterList = GetNzpCounters(HouseParams.fPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                var _fields = GetFields("counters_group", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format(" insert into {0}counters_group({3}) " +
                                          " (select {3} from {1}counters_group where nzp = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_counter, _fields));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var counterList = GetNzpCounters(HouseParams.fPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format("delete from {0}counters_group where nzp = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_counter));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var counterList = GetNzpCounters(HouseParams.tPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format("delete from {0}counters_group where nzp = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_counter));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var counterList = GetNzpCounters(HouseParams.tPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                var _fields = GetFields("counters_group", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_counter in counterList)
                {
                    ExecSQL(string.Format(" insert into {0}counters_group({3}) " +
                                          " (select {3} from {1}counters_group where nzp = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_counter, _fields));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var counterList = GetNzpCounters(HouseParams.tPoint.pref + sDataAliasRest);
            if (counterList.Count > 0)
            {
                var where = string.Join(",", counterList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters_group where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}counters_group where nzp in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("counters_group", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("counters_group", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }

        private List<int> GetNzpCounters(string pref)
        {
            var where = string.Join(",", GetKvars().ToArray());
            if (where.Length == 0) return new List<int>();
            var resList = new List<int>();
            MyDataReader reader;
            ExecRead(out reader, string.Format("select nzp_counter from {0}counters_link where nzp_kvar in({1})",
                pref, where));
            while (reader.Read())
            {
                resList.Add(reader["nzp_counter"] != DBNull.Value ? Convert.ToInt32(reader["nzp_counter"]) : 0);
            }
            reader.Close();
            return resList;
        }
    }
}

using System;
using System.Data;
using System.Linq;
using Castle.Core.Internal;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Exchange.TransferHouses
{
    [TransferAttributes(Name = "charge_XX.reval_xx", Descr = "Перенос начислений", Priority = TransferPriority.ChargeReval, Enabled = true)]
    public class TransferChargeReval : Transfer
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
                    " AND table_name like 'reval___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                        
                    ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2})",
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
                   " AND table_name like 'reval___'";
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
                   " AND table_name like 'reval___'";
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
                   " AND table_name like 'reval___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                        
                    ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2})",
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
                   " AND table_name like 'reval___'";
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
            string sql1;

            sql =
                   " SELECT TRIM(SUBSTRING(table_schema from position('_' in table_schema) + 1 for length(table_schema) - 1)) as table_schema, " +
                   " TRIM(table_name) as table_name " +
                   " FROM information_schema.tables " +
                   " WHERE table_schema like '" + HouseParams.fPoint.pref + "_charge___' " +
                   " AND table_name like 'reval___'";
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
                if(newBankTableField.Count == 0)
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

    [TransferAttributes(Name = "charge_XX.charge_xx", Descr = "Перенос начислений", Priority = TransferPriority.Charge, Enabled = true)]
    public class TransferCharge : Transfer
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
                    " AND table_name like 'charge___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                        
                    ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2})",
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
                   " AND table_name like 'charge___'";
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
                   " AND table_name like 'charge___'";
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
                   " AND table_name like 'charge___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                        ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2})",
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
                   " AND table_name like 'charge___'";
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
                   " AND table_name like 'charge___'";
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

    [TransferAttributes(Name = "charge_XX.fn_supplierxx", Descr = "Перенос оплат", Priority = TransferPriority.ChargeFnSupplier, Enabled = true)]
    public class TransferChargeFnSupplier : Transfer
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
                    " AND table_name like 'fn_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                        
                    ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({2}))",
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
                   " AND table_name like 'fn_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                        ExecSQL(string.Format("delete from {0}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
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
                   " AND table_name like 'fn_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                        ExecSQL(string.Format("delete from {0}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
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
                   " AND table_name like 'fn_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                        ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({2}))",
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
                   " AND table_name like 'fn_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var obj1 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0}." + rr["table_name"].ToString() + " where num_ls in " +
                                                        "( select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
                            HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString(), where));
                    var obj2 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0}." + rr["table_name"].ToString() + " where num_ls in " +
                                                        "( select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
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
                   " AND table_name like 'fn_supplier__'";
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

    [TransferAttributes(Name = "charge_XX.from_supplierxx", Descr = "Перенос оплат", Priority = TransferPriority.ChargeFromSupplier, Enabled = true)]
    public class TransferChargeFromSupplier : Transfer
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
                    " AND table_name like 'from_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                        
                    ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({2}))",
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
                   " AND table_name like 'from_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                        ExecSQL(string.Format("delete from {0}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
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
                   " AND table_name like 'from_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                        
                    ExecSQL(string.Format("delete from {0}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
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
                   " AND table_name like 'from_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                        ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where num_ls in (" +
                                              "select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({2}))",
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
                   " AND table_name like 'from_supplier__'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var obj1 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0}." + rr["table_name"].ToString() + " where num_ls in " +
                                                        "( select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
                            HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString(), where));
                    var obj2 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0}." + rr["table_name"].ToString() + " where num_ls in " +
                                                        "( select num_ls from " + HouseParams.fPoint.pref + sDataAliasRest + "kvar where nzp_kvar in ({1}))",
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
                   " AND table_name like 'from_supplier__'";
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

    [TransferAttributes(Name = "charge_XX.perekidka", Descr = "Перенос перекидок", Priority = TransferPriority.Perekidka, Enabled = true)]
    public class TransferPerekidka : Transfer
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
                    " AND table_name like 'perekidka'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                        ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2})",
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
                   " AND table_name like 'perekidka'";
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
                   " AND table_name like 'perekidka'";
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
                   " AND table_name like 'perekidka'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                        
                    ExecSQL(string.Format(" insert into {0}." + rr["table_name"].ToString() + "({3})" +
                                " select {3} from {1}." + rr["table_name"].ToString() + " where nzp_kvar in ({2})",
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
                   " AND table_name like 'perekidka'";
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
                   " AND table_name like 'perekidka'";
            DataTable dt = ExecSQLToTable(sql);

            foreach (DataRow rr in dt.Rows)
            {
                var oldBankTableField = GetTableFieldsList(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());
                var newBankTableField = GetTableFieldsList(rr["table_name"].ToString(), HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString());
                if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
                {
                    return false;
                }
            }
            return true;
        }
    }

    [TransferAttributes(Name = "gil_xx", Descr = "Перенос жильцов", Priority = TransferPriority.Gil_xx, Enabled = true)]
    public class TransferGil_xx : Transfer
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
                    " AND table_name like 'gil___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    string archTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString() + "_arch";
                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();
                    string toTable = HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    var _fields = GetFields(rr["table_name"].ToString(),
                        HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                    //создаем промежуточную таблицу
                    ExecSQL(string.Format(" DROP TABLE {0}", archTable), false);
                    ExecSQL(string.Format(" CREATE TABLE {0} AS SELECT " + _fields + " FROM {1} WHERE nzp_kvar IN ({2})",
                            archTable, fromTable, where));

                    //добавляем поля для нового кода жильца в промежуточную таблицу 
                    ExecSQL(string.Format(" ALTER TABLE {0} ADD COLUMN new_nzp_gil integer",
                        archTable));

                    //проставляем новые коды жильцов в промежуточную таблицу 
                    ExecSQL(string.Format(" UPDATE {0} SET new_nzp_gil = COALESCE((SELECT MAX(a.new_nzp_gil) FROM {1}gilec_arch a WHERE {0}.nzp_gil = a.nzp_gil), 0)",
                           archTable, HouseParams.fPoint.pref + sDataAliasRest));

                    //переносим данные из старого банка в новый
                    ExecSQL(string.Format(
                        " INSERT INTO {1}(nzp_dom, nzp_kvar, dat_charge, cur_zap, nzp_gil, dat_s, " +
                        "                 dat_po, stek, cnt1, cnt2, cnt3, val1, val2, val3, val4, val5, kod_info, val6)" +
                        " SELECT nzp_dom, nzp_kvar, dat_charge, cur_zap, new_nzp_gil, dat_s, " +
                        "        dat_po, stek, cnt1, cnt2, cnt3, val1, val2, val3, val4, val5, kod_info, val6" +
                        " FROM {0}",
                        archTable, toTable));
                    
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
                   " AND table_name like 'gil___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    ExecSQL(string.Format("delete from {0} where nzp_kvar in ({1})",
                        fromTable, where));
                    
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
                   " AND table_name like 'gil___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    string toTable = HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    ExecSQL(string.Format("delete from {0} where nzp_kvar in ({1})", toTable, where));
                    
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
                   " AND table_name like 'gil___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    
                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();
                    string archTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString() + "_arch";

                    ExecSQL(string.Format(" insert into {0}(nzp_dom, nzp_kvar, dat_charge, cur_zap, nzp_gil, dat_s, " +
                        "                 dat_po, stek, cnt1, cnt2, cnt3, val1, val2, val3, val4, val5, kod_info, val6) " +
                                          " select nzp_dom, nzp_kvar, dat_charge, cur_zap, nzp_gil, dat_s, " +
                        "                 dat_po, stek, cnt1, cnt2, cnt3, val1, val2, val3, val4, val5, kod_info, val6" +
                                          " from {1} where nzp_kvar in ({2}))",
                            fromTable, archTable, where));                   
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
                   " AND table_name like 'gil___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();
                    string toTable = HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    var obj1 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0} where nzp_kvar in ({1})",
                            fromTable, where));
                    var obj2 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0} where nzp_kvar in ({1})",
                            toTable, where));
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
                   " AND table_name like 'gil___'";
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

    [TransferAttributes(Name = "calc_gku_xx", Descr = "Перенос жильцов", Priority = TransferPriority.Calc_gku, Enabled = true)]
    public class TransferCalc_gku : Transfer
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
                    " AND table_name like 'calc_gku___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();
                    string toTable = HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    ExecSQL(string.Format(" insert into {0}({3})" +
                                " select {3} from {1} where nzp_kvar in ({2})", toTable, fromTable, where, _fields));                 
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
                   " AND table_name like 'calc_gku___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                          rr["table_name"].ToString();

                    ExecSQL(string.Format("delete from {0} where nzp_kvar in ({1})", fromTable, where));
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
                   " AND table_name like 'calc_gku___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    string toTable = HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    ExecSQL(string.Format("delete from {0} where nzp_kvar in ({1})", toTable, where));
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
                   " AND table_name like 'calc_gku___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    var _fields = GetFields(rr["table_name"].ToString(), HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString());

                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();
                    string toTable = HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    ExecSQL(string.Format(" insert into {0}({3})" +
                                " (select {3} from {1} where nzp_kvar in {2})", fromTable, toTable, where, _fields));
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
                   " AND table_name like 'calc_gku___'";
                DataTable dt = ExecSQLToTable(sql);

                foreach (DataRow rr in dt.Rows)
                {
                    string fromTable = HouseParams.fPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();
                    string toTable = HouseParams.tPoint.pref + "_" + rr["table_schema"].ToString() + "." +
                                       rr["table_name"].ToString();

                    var obj1 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0} where nzp_kvar in ({1})", fromTable, where));
                    var obj2 =
                        ExecScalar<Int32>(string.Format("select count(*) from {0} where nzp_kvar in ({1})", toTable, where));
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
                   " AND table_name like 'calc_gku___'";
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
}

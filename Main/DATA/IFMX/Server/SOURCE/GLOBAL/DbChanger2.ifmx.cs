using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    public partial class DbChanger : DataBaseHead
    {
        public void SetUpdates2(out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

            ret = LoadPoints(conn_db);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }

            Pack2_0001_AlterJrn(conn_db, out ret);
            Pack2_0002_CreateSettings(conn_db, out ret);
            Pack2_0003_AlterLSSaldo(conn_db, out ret);
            //Pack2_0004_DropSmsTables(conn_db, out ret);

#if PG
            ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
            ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
#endif
            
            conn_db.Close();
        }

        //----------------------------------------------------------------------
        private void Pack2_0001_AlterJrn(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
#if PG
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg.jrn_upg_nedop"))
            {
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_supg'", false);
                ExecSQL(conn_db, "alter table " + Points.Pref + "_supg. jrn_upg_nedop add exc_path CHAR(200)", false);
                //ExecSQL(conn_db, "alter table \'are\'.jrn_upg_nedop modify(d_when datetime year to minute)", false);
            }
#else
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg:jrn_upg_nedop"))
            {
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_supg", false);
                ExecSQL(conn_db, "alter table \'are\'.jrn_upg_nedop add(exc_path CHAR(200))", false);
                //ExecSQL(conn_db, "alter table \'are\'.jrn_upg_nedop modify(d_when datetime year to minute)", false);
            }
#endif
            else ret = Utils.InitReturns();
        }

        //----------------------------------------------------------------------
        private void Pack2_0002_CreateSettings(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
#if PG
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_supg.settings"))
            {
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_supg'", false);
                ExecSQL(conn_db, "create table " + Points.Pref + "_supg. settings (set_id integer, set_value CHAR(20))", false);
            }
#else
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_supg:settings"))
            {
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_supg", false);
                ExecSQL(conn_db, "create table settings (set_id integer, set_value CHAR(20))", false);
            }
#endif
            else ret = Utils.InitReturns();
        }
        
        //----------------------------------------------------------------------
        private void Pack2_0003_AlterLSSaldo(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
#if PG
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg.ls_saldo"))
            {
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_supg'", false);
                ExecSQL(conn_db, "alter table " + Points.Pref + "_supg. ls_saldo add saldo_date date", false);
                //ExecSQL(conn_db, "alter table " + Points.Pref + "_supg. jrn_upg_nedop alter column d_when datetime year to minute)", false);
            }

#else
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg:ls_saldo"))
            {
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_supg", false);
                ExecSQL(conn_db, "alter table ls_saldo add(saldo_date date)", false);
                //ExecSQL(conn_db, "alter table \'are\'.jrn_upg_nedop modify(d_when datetime year to minute)", false);
            }
#endif
            else ret = Utils.InitReturns();
        }

        //----------------------------------------------------------------------
        private void Pack2_0004_DropSmsTables(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
#if PG
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg.message_outbound"))
            {
                //ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_supg'", false);
                ExecSQL(conn_db, "drop table " + Points.Pref + "_supg. message_outbound", false);
            }
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg.s_receiver"))
            {
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_supg'", false);
                ExecSQL(conn_db, "drop table " + Points.Pref + "_supg. s_receiver", false);
            }
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg.s_message_status"))
            {
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_supg'", false);
                ExecSQL(conn_db, "drop table" + Points.Pref + "_supg.  s_message_status", false);
            }

#else
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg:message_outbound"))
            {
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_supg", false);
                ExecSQL(conn_db, "drop table \'are\'.message_outbound", false);
            }
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg:s_receiver"))
            {
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_supg", false);
                ExecSQL(conn_db, "drop table \'are\'.s_receiver", false);
            }
            if (TempTableInWebCashe(conn_db, Points.Pref + "_supg:s_message_status"))
            {
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_supg", false);
                ExecSQL(conn_db, "drop table \'are\'.s_message_status", false);
            }
#endif
        }


    }

}
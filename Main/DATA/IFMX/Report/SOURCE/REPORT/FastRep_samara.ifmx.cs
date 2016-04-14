using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;
using System.IO;
using System.Data.OleDb;
using SevenZip;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep : ExcelRepClient
    {


        public DataTable GetListDomFaktura(ReportPrm prm, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            MyDataReader reader = null;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Формирование счетов. Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion

            string tXX_spls = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + "t" 
                + prm.nzp_user + "_spls";


            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";

            #region создание временной таблицы
            ExecSQL(conn_db, " drop table t_nach ", false);
            sql.Remove(0, sql.Length);
            sql.Append(" create " + sCrtTempTable + " table t_nach (     ");
            sql.Append(" count_ls integer default 0,");
            sql.Append(" nzp_dom integer default 0");
            sql.Append(" ) " + sUnlogTempTable);
            ExecSQL(conn_db, sql.ToString(), true);

            ExecSQL(conn_db, " drop table sel_kvar ", false);
            sql.Remove(0, sql.Length);
            sql.Append("CREATE TEMP TABLE sel_kvar(" +
                       " nzp_kvar integer, " +
                       " nzp_dom integer, " +
                       " nzp_geu integer, " +
                       " pref char(10)) " + DBManager.sUnlogTempTable);
            ExecSQL(conn_db, sql.ToString(), true);

            sql.Remove(0, sql.Length);
            sql.Append(" insert into sel_kvar(nzp_kvar, nzp_dom, pref)");
            sql.Append(" select nzp_kvar, nzp_dom, pref ");
            sql.Append(" from  " + tXX_spls + " ");
            ExecSQL(conn_db, sql.ToString(), true);


            ExecSQL(conn_db, "create index ix_tmp01192 on sel_kvar(nzp_kvar)", true);
            ExecSQL(conn_db, DBManager.sUpdStat + " sel_kvar", true);

        
          
            #endregion

            try
            {
                #region Выборка по локальным банкам



                sql.Remove(0, sql.Length);
                sql.Append(" select pref ");
                sql.Append(" from  sel_kvar group by 1");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    throw new Exception("Ошибка формирования списка квартир для отчета");

                }
                while (reader.Read())
                {
                    string pref = reader["pref"].ToString().ToLower().Trim();
                    string chargeXX = pref + "_charge_" + (prm.year - 2000).ToString("00") +
                        DBManager.tableDelimiter + "charge_" + prm.month.ToString("00");

                    ExecSQL(conn_db, "drop table t_nachtmp", false);

                    sql.Remove(0, sql.Length);
                    sql.Append(" CREATE TEMP TABLE t_nachtmp(");
                    sql.Append(" nzp_kvar integer,   ");
                    sql.Append(" nzp_dom integer)"+DBManager.sUnlogTempTable);
                    ExecSQL(conn_db, sql.ToString(), true);

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_nachtmp (nzp_kvar, nzp_dom )");
                    sql.Append(" SELECT a.nzp_kvar, k.nzp_dom   ");
                    sql.Append(" FROM " + chargeXX + " a, sel_kvar k");
                    sql.Append(" WHERE a.nzp_kvar=k.nzp_kvar ");
                    sql.Append("        AND dat_charge is null and sum_charge<>0");
                    sql.Append("        AND a.nzp_serv>1 ");
                    sql.Append(" GROUP BY 1,2 ");
                    ExecSQL(conn_db, sql.ToString(), true);

                    sql.Remove(0, sql.Length);
                    sql.Append(" UPDATE t_nachtmp SET nzp_dom = (");
                    sql.Append("            SELECT max(nzp_dom_base) ");
                    sql.Append("            FROM " + pref + DBManager.sDataAliasRest + "link_dom_lit d");
                    sql.Append("            WHERE  t_nachtmp.nzp_dom=d.nzp_dom) ");
                    sql.Append(" WHERE nzp_dom IN (SELECT nzp_dom FROM " +
                        pref + DBManager.sDataAliasRest + "link_dom_lit d)");
                    ExecSQL(conn_db, sql.ToString(), true);

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_nach (nzp_dom, count_ls )");
                    sql.Append(" SELECT nzp_dom, count(nzp_kvar)   ");
                    sql.Append(" FROM  t_nachtmp ");
                    sql.Append(" GROUP BY 1 ");
                    ExecSQL(conn_db, sql.ToString(), true);

                    ExecSQL(conn_db, "drop table t_nachtmp", true);

                        
                }
                reader.Close();

                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT geu, ulica, idom, ndom, nkor, sum(count_ls) as count_ls");
                sql.Append(" FROM t_nach t, " + Points.Pref + DBManager.sDataAliasRest + "dom d, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_ulica su, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_geu sg ");
                sql.Append(" WHERE t.nzp_dom = d.nzp_dom ");
                sql.Append("        AND d.nzp_ul = su.nzp_ul ");
                sql.Append("        AND d.nzp_geu = sg.nzp_geu ");
                sql.Append(" GROUP BY 1,2,3,4,5 ");
                sql.Append(" ORDER BY 1,2,3,4,5 ");
                DT = DBManager.ExecSQLToTable(conn_db, sql.ToString());
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_nach ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            return DT;

        }
      


    }   
}


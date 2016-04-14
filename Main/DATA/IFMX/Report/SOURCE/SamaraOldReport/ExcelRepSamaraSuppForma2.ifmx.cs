using System;
using System.Data;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;
using System.Collections.Generic;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {
        public DataTable GetSpravSuppNach(List<Prm> listprm, out Returns ret, string nzpUser)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);




            #region Подключение к БД
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            MyDataReader reader = null;


            ret = OpenDb(connWeb, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                connWeb.Close();
                return null;
            }

            #endregion


            string tXXSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + nzpUser + "_spls";
            connWeb.Close();
            var localTable = new DataTable();

            try
            {

                #region Выборка по локальным банкам



                ExecSQL(connDB, "drop table t_svod", false);
                ExecSQL(connDB, "drop table t_svod_perekidka", false);



                string sql = " create temp table t_svod( " +
                             " nzp_geu integer, " +
                             " nzp_serv integer, " +
                             " nzp_supp integer, " +
                             " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                             " sum_prih " + DBManager.sDecimalType + "(14,2), " +
                             " sum_rcl " + DBManager.sDecimalType + "(14,2), " +
                             " kod_rs integer, " + //код счета
                             " sum_uk " + DBManager.sDecimalType + "(14,2))" +
                             DBManager.sUnlogTempTable;

                ExecSQL(connDB, sql, true);

                sql = " create temp table t_svod_perekidka( " +
                             " nzp_serv integer, " +
                             " nzp_supp integer, " +
                             " sum_rcl " + DBManager.sDecimalType + "(14,2))" +
                             DBManager.sUnlogTempTable;

                ExecSQL(connDB, sql, true);



                ExecSQL(connDB, "drop table sel_kvar3", false);

                sql = " CREATE TEMP TABLE sel_kvar3 ( " +
                      "    nzp_kvar integer, " +
                      "    nzp_geu integer, " +
                      "    pkod " + DBManager.sDecimalType + ", " +
                      "    num_ls integer, " +
                      "    pref char(10)) " + DBManager.sUnlogTempTable;
                ExecSQL(connDB, sql, true);


                sql = " INSERT INTO sel_kvar3(nzp_kvar, num_ls, nzp_geu, pkod, pref)  " +
                      " select nzp_kvar, num_ls, nzp_geu, pkod, pref " +
                      " from  " + tXXSpls + "";
                ExecSQL(connDB, sql, true);

                ExecSQL(connDB, "create index ixselkv_01 on sel_kvar3(num_ls) ", true);
                ExecSQL(connDB, DBManager.sUpdStat + " sel_kvar3", true);

                string kapr = (listprm[0].nzp_serv == -206 ? " and nzp_serv <> 206 " :
                       listprm[0].nzp_serv == 206 ? " and nzp_serv=206 " : "");

                ExecRead(connDB, out reader, "select pref from sel_kvar3 group by 1 ", true);

                while (reader.Read())
                {
                    if (reader["pref"] == null) continue;

                    string pref = reader["pref"].ToString().Trim();
                    string dbCharge = pref + "_charge_" + (listprm[0].year_ - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "charge_" + listprm[0].month_.ToString("00");
                    string dbChargePerekidka = pref + "_charge_" + (listprm[0].year_ - 2000).ToString("00") +
                                     DBManager.tableDelimiter + "perekidka";

                    string dbFnSupplier = pref + "_charge_" + (listprm[0].year_ - 2000).ToString("00") +
                                          DBManager.tableDelimiter + "fn_supplier" + listprm[0].month_.ToString("00");
                    string dbFromSupplier = pref + "_charge_" + (listprm[0].year_ - 2000).ToString("00") +
                                                            DBManager.tableDelimiter + "from_supplier";
                    string dbPackLs = Points.Pref + "_fin_" + (listprm[0].year_ - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "pack_ls ";
                    string dbPack = Points.Pref + "_fin_" + (listprm[0].year_ - 2000).ToString("00") +
                                    DBManager.tableDelimiter + "pack ";



                    #region Выборка данных

                    sql = " INSERT INTO t_svod(nzp_geu,nzp_serv, nzp_supp, kod_rs, sum_charge) " +
                          " SELECT t.nzp_geu, nzp_serv, nzp_supp,-1, sum(rsum_tarif) + sum(reval) - abs(sum(sum_nedop)) as sum_charge " +
                          " FROM " + dbCharge + " a," +
                          "        sel_kvar3 t" +
                          " WHERE a.num_ls=t.num_ls " +
                          "       AND dat_charge is null " +
                          "       AND a.nzp_serv>1" + kapr +
                          " GROUP BY 1,2,3,4 ";
                    ExecSQL(connDB, sql, true);

                    sql = " INSERT INTO t_svod_perekidka " +
                         " SELECT nzp_serv, nzp_supp, sum(sum_rcl) as sum_charge " +
                         " FROM " + dbChargePerekidka +
                         " WHERE month_ = " + listprm[0].month_.ToString("00") +
                         " AND nzp_kvar in (SELECT nzp_kvar from sel_kvar3) " +
                         " GROUP BY 1,2 ";
                    ExecSQL(connDB, sql, true);

                    sql = " UPDATE t_svod SET sum_rcl = " +
                         " (SELECT sum_rcl FROM t_svod_perekidka WHERE t_svod.nzp_serv = t_svod_perekidka.nzp_serv AND t_svod.nzp_supp = t_svod_perekidka.nzp_supp) ";
                    ExecSQL(connDB, sql, true);

                    sql = " UPDATE t_svod SET sum_charge = sum_charge + coalesce(sum_rcl,0)";
                    ExecSQL(connDB, sql, true);


                    sql = " INSERT INTO t_svod(nzp_geu, nzp_serv, nzp_supp, kod_rs, sum_prih) " +
                          " SELECT t.nzp_geu,a.nzp_serv, a.nzp_supp, " +
                          "        (case when sb.nzp_payer = 80001  then substring(t.pkod::char(13),1,3)||'00' " +
                          "              when p.pack_type = 20  then '40' " +
                          "              else  substring(t.pkod::char(13),1,3) end)::integer as kod_rs," +
                          "        sum(sum_prih) as sum_prih " +
                          " FROM " + dbFnSupplier + " a," +
                          dbPackLs + " b, " +
                          dbPack + " p, " +
                          " sel_kvar3 t, " +
                          Points.Pref + DBManager.sKernelAliasRest + "s_bank sb " +
                          " WHERE a.num_ls=t.num_ls " +
                          "       AND a.nzp_pack_ls=b.nzp_pack_ls " +
                          "       AND b.nzp_pack=p.nzp_pack " +
                          "       AND p.nzp_bank=sb.nzp_bank " + kapr +
                          " GROUP BY 1,2,3,4 ";
                    ExecSQL(connDB, sql, true);




                    sql = " INSERT INTO t_svod(nzp_geu, nzp_serv, nzp_supp, kod_rs, sum_prih) " +
                        " SELECT t.nzp_geu,a.nzp_serv, a.nzp_supp, " +
                        "        40 as kod_rs," +
                        "        sum(sum_prih) as sum_prih " +
                        " FROM " + dbFromSupplier + " a," +
                        dbPackLs + " b, " +
                        dbPack + " p, " +
                        " sel_kvar3 t, " +
                        Points.Pref + DBManager.sKernelAliasRest + "s_bank sb " +
                        " WHERE a.num_ls=t.num_ls " +
                        "       AND a.nzp_pack_ls=b.nzp_pack_ls " +
                        "       AND b.dat_uchet>='01." + listprm[0].month_ + "." + listprm[0].year_ + "'" +
                        "       AND b.dat_uchet<='" + DateTime.DaysInMonth(listprm[0].year_, listprm[0].month_) +
                        "." + listprm[0].month_ + "." + listprm[0].year_ + "'" +
                        "       AND b.nzp_pack=p.nzp_pack " +
                        "       AND p.nzp_bank=sb.nzp_bank " + kapr +
                        " GROUP BY 1,2,3,4 ";
                    ExecSQL(connDB, sql, true);

                    #endregion
                }

                reader.Close();



                ExecSQL(connDB, "drop table t_supp", false);

                sql = " CREATE TEMP TABLE t_supp( nzp_supp integer)" +
                    DBManager.sUnlogTempTable;
                ExecSQL(connDB, sql, true);



                sql = "insert into t_supp " +
                      "select nzp_supp " +
                      "from t_svod " +
                      "where nzp_serv = 9  group by 1 ";
                ExecSQL(connDB, sql, true);


                sql = "update t_svod set nzp_serv=9 " +
                      "where nzp_serv = 14 and nzp_supp in (select nzp_supp from t_supp)";
                ExecSQL(connDB, sql, true);


                sql = "update t_svod set nzp_serv=513 " +
                      "where nzp_serv = 514 and nzp_supp in (select nzp_supp from t_supp)";
                ExecSQL(connDB, sql, true);

                ExecSQL(connDB, "drop table t_supp", true);


                string field = ",kod40 " + DBManager.sDecimalType + "(14,2) default 0";


                sql = " select kod_rs" +
                      " from t_svod where kod_rs >0 and kod_rs<>40 " +
                      " group by 1 ";

                ExecRead(connDB, out reader, sql, true);
                while (reader.Read())
                {
                    if (reader["kod_rs"] != DBNull.Value)
                        field += ",kod" + reader["kod_rs"].ToString().Trim() + " " +
                            DBManager.sDecimalType + "(14,2)";

                }
                reader.Close();

                ExecSQL(connDB, "drop table t_sv", false);


                sql = " CREATE TEMP TABLE t_sv (tip integer, ord_ integer, geu char(100)," +
                      "                        service char(100), name_supp char(100), " +
                      "                        nzp_geu integer, nzp_serv integer, " +
                      "                        nzp_supp integer," +
                      "                        sum_charge " + DBManager.sDecimalType + "(14,2)," +
                      "                        sum_prih " + DBManager.sDecimalType + "(14,2)" + field + ")"
                      + DBManager.sUnlogTempTable;
                ExecSQL(connDB, sql, true);


                sql = " INSERT INTO t_sv(tip, ord_, geu, service, name_supp, nzp_geu, nzp_serv,nzp_supp, " +
                      "            sum_charge, sum_prih) " +
                      " SELECT 2 as tip,1 as ord_, geu, service, name_supp,  " +
                      "        a.nzp_geu, a.nzp_serv, a.nzp_supp, " +
                      "        sum(" + DBManager.sNvlWord + "(a.sum_charge,0)) as sum_charge, " +
                      "        sum(" + DBManager.sNvlWord + "(a.sum_prih,0)) as sum_prih " +
                      " FROM t_svod a, " +
                      Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                      Points.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                      Points.Pref + DBManager.sDataAliasRest + "s_geu sg " +
                      " WHERE a.nzp_supp=su.nzp_supp and a.nzp_serv=s.nzp_serv " +
                      "       AND a.nzp_geu=sg.nzp_geu and " +
                      "       abs(" + DBManager.sNvlWord + "(sum_charge,0)) + " +
                      "       abs(" + DBManager.sNvlWord + "(sum_prih,0))>0.001" +
                      " group by 1,2,3,4,5,6,7,8 ";
                ExecSQL(connDB, sql, true);


                sql = " INSERT INTO t_sv(tip, ord_, geu, service, name_supp, nzp_geu,nzp_serv,nzp_supp, " +
                      "            sum_charge, sum_prih) " +
                      " SELECT 2 as tip,2 as ord_,geu, service, 'ВСЕГО',  " +
                      "        a.nzp_geu, a.nzp_serv, -1001, " +
                      "        sum(" + DBManager.sNvlWord + "(sum_charge,0)) as sum_charge, " +
                      "        sum(" + DBManager.sNvlWord + "(sum_prih,0)) as sum_prih " +
                      " from t_svod a, " +
                      Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                      Points.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                      Points.Pref + DBManager.sDataAliasRest + "s_geu sg " +
                      " WHERE a.nzp_supp=su.nzp_supp and a.nzp_serv=s.nzp_serv " +
                      "        AND a.nzp_geu=sg.nzp_geu and " +
                      "        abs(" + DBManager.sNvlWord + "(sum_charge,0)) + " +
                      "        abs(" + DBManager.sNvlWord + "(sum_prih,0))>0.001" +
                      " GROUP BY 1,2,3,4,5,6,7,8 ";
                ExecSQL(connDB, sql, true);


                sql = " INSERT INTO t_sv(tip, ord_, geu, service, name_supp, nzp_geu, nzp_serv,nzp_supp, " +
                      " sum_charge, sum_prih) " +
                      " SELECT 1 as tip,1 as ord_,'-1',service, name_supp,  " +
                      "        -1, a.nzp_serv, a.nzp_supp, " +
                      "        sum(" + DBManager.sNvlWord + "(sum_charge,0)) as sum_charge, " +
                      "        sum(" + DBManager.sNvlWord + "(sum_prih,0)) as sum_prih " +
                      " from t_svod a, " +
                      Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                      Points.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                      Points.Pref + DBManager.sDataAliasRest + "s_geu sg " +
                      " where a.nzp_supp=su.nzp_supp AND a.nzp_serv=s.nzp_serv " +
                      "        AND a.nzp_geu=sg.nzp_geu AND" +
                      "        abs(" + DBManager.sNvlWord + "(sum_charge,0)) + " +
                      "        abs(" + DBManager.sNvlWord + "(sum_prih,0))>0.001" +
                      " group by 1,2,3,4,5,6,7,8 ";
                ExecSQL(connDB, sql, true);


                sql = " INSERT INTO t_sv(tip, ord_, geu, service, name_supp, nzp_geu, nzp_serv,nzp_supp, " +
                      "                sum_charge, sum_prih) " +
                      " SELECT 1 as tip,2 as ord_,'-1', service, 'ВСЕГО',  " +
                      "        -1, a.nzp_serv, -1001, " +
                      "        sum(" + DBManager.sNvlWord + "(sum_charge,0)) as sum_charge, " +
                      "        sum(" + DBManager.sNvlWord + "(sum_prih,0)) as sum_prih " +
                      " from t_svod a, " +
                      Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                      Points.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                      Points.Pref + DBManager.sDataAliasRest + "s_geu sg " +
                      " WHERE a.nzp_supp=su.nzp_supp AND a.nzp_serv=s.nzp_serv " +
                      "        AND a.nzp_geu=sg.nzp_geu AND " +
                      "        abs(" + DBManager.sNvlWord + "(sum_charge,0)) + " +
                      "        abs(" + DBManager.sNvlWord + "(sum_prih,0))>0.001" +
                      " GROUP BY 1,2,3,4,5,6,7,8 ";
                ExecSQL(connDB, sql, true);


                sql = " select kod_rs" +
                      " from t_svod where kod_rs >0 " +
                      " group by 1 ";

                ExecRead(connDB, out reader, sql, true);
                while (reader.Read())
                {
                    string kodRs = reader["kod_rs"].ToString().Trim();

                    sql = " UPDATE t_sv set kod" + kodRs + "=" +
                          "            (SELECT sum(" + DBManager.sNvlWord + "(sum_prih,0)) as sum_prih " +
                          "             FROM t_svod " +
                          "             WHERE t_sv.nzp_serv=t_svod.nzp_serv " +
                          "                    AND t_sv.nzp_supp=t_svod.nzp_supp " +
                          "                    AND t_sv.nzp_geu=t_svod.nzp_geu " +
                          "                    AND kod_rs=" + kodRs + ")" +
                          " WHERE nzp_supp>0 AND nzp_geu<>-1";
                    ExecSQL(connDB, sql, true);


                    sql = " UPDATE t_sv set kod" + kodRs + "=" +
                          "                (SELECT sum(" + DBManager.sNvlWord + "(sum_prih,0)) as sum_prih " +
                          "                FROM t_svod " +
                          "                WHERE t_sv.nzp_serv=t_svod.nzp_serv " +
                          "                      AND t_sv.nzp_geu=t_svod.nzp_geu " +
                          "                      AND kod_rs=" + kodRs + ")" +
                          " WHERE nzp_supp=-1001 AND nzp_geu<>-1";
                    ExecSQL(connDB, sql, true);




                    sql = " UPDATE t_sv set kod" + kodRs + "=" +
                          "            (SELECT sum(" + DBManager.sNvlWord + "(sum_prih,0)) as sum_prih " +
                          "             FROM t_svod " +
                          "             WHERE t_sv.nzp_serv=t_svod.nzp_serv " +
                          "                    AND t_sv.nzp_supp=t_svod.nzp_supp " +
                          "                    AND kod_rs=" + kodRs + ")" +
                          " WHERE nzp_supp>0 and nzp_geu=-1";
                    ExecSQL(connDB, sql, true);



                    sql = " UPDATE t_sv set kod" + kodRs + "=" +
                          "            (SELECT sum(" + DBManager.sNvlWord + "(sum_prih,0)) as sum_prih " +
                          "             FROM t_svod " +
                          "             WHERE t_sv.nzp_serv=t_svod.nzp_serv " +
                          "                    AND kod_rs=" + kodRs + ")" +
                          " WHERE nzp_supp=-1001 and nzp_geu=-1";
                    ExecSQL(connDB, sql, true);
                }
                reader.Close();



                sql = " select * " +
                      " from t_sv  " +
                      " order by tip,geu,ord_,name_supp,service";

                localTable = DBManager.ExecSQLToTable(connDB, sql);

                ExecSQL(connDB, "drop table t_sv", false);

                localTable.Columns.Remove("ord_");


                #endregion
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Ошибка выборки " + e.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
            }
            finally
            {
                if (reader != null) reader.Close();
                ExecSQL(connDB, "drop table sel_kvar3", true);
                connDB.Close();
            }
            return localTable;

        }


    }
}

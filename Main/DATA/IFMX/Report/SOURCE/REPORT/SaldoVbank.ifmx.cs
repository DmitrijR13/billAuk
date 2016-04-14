using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using FastReport;
using System.IO;
using System.Data.OleDb;
using SevenZip;

using STCLINE.KP50.Interfaces;
using Globals.SOURCE.Utility;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep : ExcelRepClient
    {

        /// <summary>
        /// Выгрузка сальдо в банк
        /// </summary>
        /// <returns></returns>
        /// 


        public Returns GetSaldo_v_bank(out Returns ret, SupgFinder finder, string year, string month)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }


            //Имя файла отчета
            string fileNameIn = "saldo_" + year + month + "_" + DateTime.Now.Ticks;
            var excelRepDb = new ExcelRep();
            var sql = new StringBuilder();

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка сальдо в банк за " + month + "." + year
            });
            if (!ret.result) return ret;

            var nzpExc = ret.tag;

            IDbConnection connDb = null;
            MyDataReader reader = null;

            var tmpLocal = "tmp_saldo_local";
            var tmpMain = "tmp_saldo_main";
            string dop_param = "";
            try
            {
                IDbConnection conn_web = DBManager.newDbConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    return ret;
                }
#if PG
                string tXX_spls = defaultPgSchema + ".t" + finder.nzp_user + "_spls";
#else
                string tXX_spls = conn_web.Database + ":t" + finder.nzp_user + "_spls";
#endif
                var prefs = new List<string>();

                conn_web.Close();

                string s_year = year.Substring(2);

                string conn_kernel = Points.GetConnByPref(Points.Pref);
                connDb = DBManager.newDbConnection(conn_kernel);
                ret = OpenDb(connDb, true);
                if (!ret.result)
                {
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                    conn_web.Close();
                    return ret;
                }

                ExecSQL(connDb, "drop table t_saldov", false);
#if PG
                ExecSQL(connDb, "select * into unlogged t_saldov from " + tXX_spls, true);
#else
                ExecSQL(connDb, "select * from " + tXX_spls + " into temp t_saldov with no log ", true);
#endif


                sql = new StringBuilder("select distinct pref from t_saldov");
                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    return ret;
                }

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value) prefs.Add(Convert.ToString(reader["pref"]).Trim());
                }
                reader.Close();

                #region Получение сальдо в банк

                ret = CreateSaldoVBankTempTable(connDb, tmpMain);
                if (!ret.result) return ret;

                foreach (var pref in prefs)
                {
                    //выгрузить все кроме поставщика ==finder.nzp_supp
                    //оставляем только те ЛС у которых есть услуги других поставщиков
                    if (finder.act_flag == 2)
                    {
                        ret = ExecSQL(connDb, "delete from t_saldov where pref='"+pref+"' and nzp_kvar not in (select nzp_kvar from " + pref + "_charge_" +
                                s_year + tableDelimiter + "charge_" + month + " where nzp_supp<>" + finder.nzp_supp + " and nzp_serv > 1 and dat_charge is null  group by nzp_kvar) ", true);
                        if (!ret.result) return ret;
                    }
                    //только те ЛС у которых есть начисления по выбранному поставщику
                    if (finder.act_flag == 1)
                    {
                        ret = ExecSQL(connDb, "delete from t_saldov where pref='" + pref + "' and nzp_kvar not in (select nzp_kvar from " + pref + "_charge_" +
                            s_year + tableDelimiter + "charge_" + month + " where nzp_supp=" + finder.nzp_supp + " and nzp_serv > 1 and dat_charge is null  group by nzp_kvar) ", true);
                        if (!ret.result) return ret;
                    }

                    ret = CreateSaldoVBankTempTable(connDb, tmpLocal);
                    if (!ret.result) return ret;

                    sql = new StringBuilder();
                    sql.Append(" insert into " + tmpLocal +
                               " (pref, num_ls,tsg,vu,sumn,peni,sumd,predpr,geu,kod,kodls,kc,rso,ulica,ndom,nkvar,fio) ");
                    sql.Append(" Select " + Utils.EStrNull(pref) +
                               ", k.num_ls,'' as tsg, '01' as vu, 0 as sumn, 0 as peni, 0 as sumd");

                    string pkod = "k.pkod";
                    if (finder.act_flag == 1) pkod = "sp.pkod_supp";
#if PG
                    sql.Append(", substring("+pkod+"::varchar(13) from 1 for 3)as predpr, ");
                    sql.Append("substring("+pkod+"::varchar(13) from 4 for 2)as geu, ");
                    sql.Append("substring("+pkod+"::varchar(13) from 6 for 5)as kod, ");
                    sql.Append("substring(" + pkod + "::varchar(13) from 11 for 1)::integer as kodls, ");
                    sql.Append("substring("+pkod+"::varchar(13) from 12 for 2)as kc ");
#else
                    sql.Append(", substr(" + pkod + ",1,3) as predpr, ");
                    sql.Append(" substr(" + pkod + ",4,2)as geu, ");
                    sql.Append(" substr(" + pkod + ",6,5)as kod, ");
                    sql.Append(" substr(" + pkod + ",11,1)as kodls, ");
                    sql.Append(" substr(" + pkod + ",12,2)as kc");
#endif
                    sql.Append(", '00' as rso,");
                    sql.Append(" u.ulica,d.ndom,k.nkvar,k.fio");
                    sql.Append(" From " + pref + "_data" + tableDelimiter + "kvar k");
                    sql.Append(", " + pref + "_data" + tableDelimiter + "dom d");
                    sql.Append(", " + pref + "_data" + tableDelimiter + "s_ulica u");
                    sql.Append(", " + pref + "_data" + tableDelimiter + "s_area a");
                    sql.Append(", " + pref + "_data" + tableDelimiter + "s_geu g");
                    if (finder.act_flag == 1) sql.Append(", " + pref + "_data" + tableDelimiter + "supplier_codes sp");
                    sql.Append(" Where k.nzp_dom = d.nzp_dom");
                    sql.Append(" and d.nzp_ul  = u.nzp_ul");
                    sql.Append(" and k.nzp_area = a.nzp_area");
                    sql.Append(" and k.nzp_geu  = g.nzp_geu");
                    sql.Append(" and k.num_ls > 0");
                    if (finder.act_flag == 1) sql.Append(" and sp.nzp_supp=" + finder.nzp_supp + " and sp.nzp_kvar=k.nzp_kvar");
                    sql.Append(" and k.nzp_kvar in (select nzp_kvar from t_saldov)");

                    ret = ExecSQL(connDb, sql.ToString());
                    if (!ret.result) return ret;

                    ExecSQL(connDb, "create index " + tmpLocal + "_1 on " + tmpLocal + "(num_ls)");
                    ExecSQL(connDb, sUpdStat +" "+ tmpLocal, true);

                    //начисления по поставщикам кроме выбранного
                    if (finder.act_flag == 2)
                    {
                        dop_param = "and nzp_supp<>" + finder.nzp_supp;
                    }
                    //начисления по выбранному поставщику
                    if (finder.act_flag == 1)
                    {
                        dop_param = "and nzp_supp=" + finder.nzp_supp;
                    }

                    sql = new StringBuilder();
                    sql.Append(" UPDATE " + tmpLocal + " SET sumn = (SELECT sum(m.SUM_CHARGE) from " + pref + "_charge_" +
                               s_year + tableDelimiter + "charge_" + month + " m ");
                    sql.Append(" where nzp_serv > 1 and dat_charge is null and m.num_ls = " + tmpLocal + ".num_ls " + dop_param + ") ");
                    ret = ExecSQL(connDb, null, sql.ToString(), true);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    sql = new StringBuilder();
                    sql.Append(" UPDATE " + tmpLocal + " SET sumd = (SELECT sum(m.SUM_INSALDO-m.sum_money) from " +
                               pref + "_charge_" + s_year + tableDelimiter + "charge_" + month + " m ");
                    sql.Append(" where nzp_serv>1 and dat_charge is null and m.num_ls=" + tmpLocal + ".num_ls " + dop_param + ")");
                    ret = ExecSQL(connDb, sql.ToString());
                    if (!ret.result)
                    {
                        return ret;
                    }

                    ret = ExecSQL(connDb, "insert into " + tmpMain + " select * from " + tmpLocal);
                    if (!ret.result) return ret;
                }

                sql = new StringBuilder(" update " + tmpMain +
                                        " set kodls = case when kodls = '0' then null else kodls end ");
                ret = ExecSQL(connDb, sql.ToString(), true);
                if (!ret.result)
                {
                    return ret;
                }
                sql.Remove(0, sql.Length);

                sql = new StringBuilder("select count(*) as num from " + tmpMain);
                var obj = ExecScalar(connDb, sql.ToString(), out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                var num = Convert.ToInt32(obj);

                ExecSQL(connDb, "create index " + tmpMain + "_1 on " + tmpMain + "(predpr)", true);
                ExecSQL(connDb, sUpdStat +" "+ tmpMain, true);

                sql = new StringBuilder("select * from " + tmpMain + " order by predpr");
                ret = ExecRead(connDb, null, out reader, sql.ToString(), true);
                if (!ret.result) return ret;
                sql.Remove(0, sql.Length);
                #endregion


                var eDBF = new exDBF(fileNameIn);
                eDBF.AddColumn("TSG", Type.GetType("System.String"), 1, 0);
                eDBF.AddColumn("VU", Type.GetType("System.String"), 2, 0);
                eDBF.AddColumn("SUMN", Type.GetType("System.Decimal"), 16, 2);
                eDBF.AddColumn("PENI", Type.GetType("System.Decimal"), 12, 2);
                eDBF.AddColumn("SUMD", Type.GetType("System.Decimal"), 17, 2);
                eDBF.AddColumn("MES_OPL", Type.GetType("System.String"), 4, 0);
                eDBF.AddColumn("PREDPR", Type.GetType("System.String"), 3, 0);
                eDBF.AddColumn("RSO", Type.GetType("System.String"), 2, 0);
                eDBF.AddColumn("GEU", Type.GetType("System.String"), 2, 0);
                eDBF.AddColumn("KOD", Type.GetType("System.String"), 5, 0);
                eDBF.AddColumn("KODLS", Type.GetType("System.String"), 1, 0);
                eDBF.AddColumn("ADR", Type.GetType("System.String"), 46, 0);
                eDBF.AddColumn("KC", Type.GetType("System.String"), 2, 0);
                eDBF.AddColumn("FIO", Type.GetType("System.String"), 25, 0);
                eDBF.AddColumn("IMYA", Type.GetType("System.String"), 15, 0);
                eDBF.AddColumn("OTCH", Type.GetType("System.String"), 20, 0);

                //var strPath = InputOutput.GetOutputDir();
                var strPath = Constants.Directories.ReportDir;
                var strFilePath = Path.Combine(strPath, fileNameIn + ".DBF");
                eDBF.Save(strPath, 866);

                string[] names;
                string fio, first_name, name, second_name;
                string TSG, VU, MES_OPL, PREDP, RSO, GEU, KOD, KODLS, ADR, KC, FIO, IMYA, OTCH;
                decimal SUMN, PENI, SUMD;

                Utils.setCulture();

                int i = 0;
                while (reader.Read())
                {
                    i++;
                    fio = reader["fio"] != DBNull.Value
                        ? ((string)reader["fio"]).Trim().Replace("'", "''")
                        : "";
                    names = fio.Split(' ');

                    first_name = (names.Length == 3 ? names[0] : fio);
                    name = (names.Length == 3 ? names[1] : "");
                    second_name = (names.Length == 3 ? names[2] : "");

                    TSG = (reader["tsg"] != DBNull.Value ? ((string)reader["tsg"]).Trim() : "");
                    VU = (reader["vu"] != DBNull.Value ? ((string)reader["vu"]).Trim() : "");
                    SUMN = (reader["sumn"] != DBNull.Value ? ((Decimal)reader["sumn"]) : 0);
                    PENI = (reader["peni"] != DBNull.Value ? ((Decimal)reader["peni"]) : 0);
                    SUMD = (reader["sumd"] != DBNull.Value ? ((Decimal)reader["sumd"]) : 0);
                    MES_OPL = month + s_year;
                    PREDP = (reader["predpr"] != DBNull.Value ? ((string)reader["predpr"]).ToString() : "");
                    RSO = (reader["rso"] != DBNull.Value ? ((string)reader["rso"]).Trim() : "");
                    GEU = (reader["geu"] != DBNull.Value ? ((string)reader["geu"]).ToString() : "");
                    KOD = (reader["kod"] != DBNull.Value ? ((string)reader["kod"]).ToString() : "");
                    KODLS = (reader["kodls"] != DBNull.Value ? ((int)reader["kodls"]).ToString() : "");
                    ADR = (reader["ulica"] != DBNull.Value ? ((string)reader["ulica"]).Trim().Replace("'", "''") : "") + "," + (reader["ndom"] != DBNull.Value ? ((string)reader["ndom"]).Trim() : "") + "-" + (reader["nkvar"] != DBNull.Value ? ((string)reader["nkvar"]).Trim() : "");
                    KC = (reader["kc"] != DBNull.Value ? ((string)reader["kc"]).ToString() : "");
                    FIO = first_name;
                    IMYA = name;
                    OTCH = second_name;

                    var row = eDBF.DataTable.NewRow();

                    row["TSG"] = TSG;
                    row["VU"] = VU;
                    row["SUMN"] = SUMN;
                    row["PENI"] = PENI;
                    row["SUMD"] = SUMD;
                    row["MES_OPL"] = MES_OPL;
                    row["PREDPR"] = PREDP;
                    row["RSO"] = RSO;
                    row["GEU"] = GEU;
                    row["KOD"] = KOD;
                    row["KODLS"] = KODLS;
                    row["ADR"] = ADR;
                    row["KC"] = KC;
                    row["FIO"] = FIO;
                    row["IMYA"] = IMYA;
                    row["OTCH"] = OTCH;

                    eDBF.DataTable.Rows.Add(row);

                    if (i % 100 == 0)
                    {
                        excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal)i) / num });
                        eDBF.Append(strFilePath);
                        eDBF.DataTable.Rows.Clear();
                    }
                }
                reader.Close();

                if (eDBF.DataTable.Rows.Count > 0) eDBF.Append(strFilePath);

                //if (InputOutput.useFtp)
                //{
                //    fileNameIn = InputOutput.SaveOutputFile(strFilePath);
                //}
                //else 
                fileNameIn += ".DBF";

                reader = null;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetSaldo_v_bank:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(connDb, "drop table " + tmpLocal, false);
                ExecSQL(connDb, "drop table " + tmpMain, false);

                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileNameIn });
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                excelRepDb.Close();

                if (reader != null) reader.Close();
                if (connDb != null) connDb.Close();
            }

            return ret;
        }

        private Returns CreateSaldoVBankTempTable(IDbConnection connection, string tableName)
        {
            ExecSQL(connection, "drop table " + tableName, false);

            var sql = new StringBuilder();
            sql.Append(" CREATE temp TABLE " + tableName + " ( ");
            sql.Append(" pref           char(20),");
            sql.Append(" num_ls           integer,");
            sql.Append(" tsg          char(20),");
            sql.Append(" vu           char(20),");
            sql.Append(" sumn       numeric(14,2),");
            sql.Append(" peni        numeric(14,2),");
            sql.Append(" sumd     numeric(14,2),");
            sql.Append(" predpr        char(3),");
            sql.Append(" geu        char(2),");
            sql.Append(" kod        char(5),");
            sql.Append(" kodls       integer,");
            sql.Append(" kc       char(2),");
            sql.Append(" rso      char(2),");
            sql.Append(" ulica  char(100),");
            sql.Append(" ndom    char(20),");
            sql.Append(" nkvar   char(20),");
            sql.Append(" fio   char(250)");
            sql.Append(" ) " + sUnlogTempTable);

            return ExecSQL(connection, sql.ToString(), true);
        }
    }
}


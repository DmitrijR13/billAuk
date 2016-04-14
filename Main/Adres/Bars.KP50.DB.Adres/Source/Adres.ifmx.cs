using System.Linq;
using System.Threading.Tasks;
using Bars.KP50.Utils;
using Microsoft.SqlServer.Server;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;

namespace STCLINE.KP50.DataBase
{
    //using System.Text.RegularExpressions;

    public partial class DbAdres : DbAdresKernel
    {
#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif







        //----------------------------------------------------------------------
        public _Rekvizit GetLsRevizit(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            string pref = finder.pref;
            if (pref == "") pref = Points.Pref;



            IDbConnection conn_db = GetConnection(Points.GetConnByPref(pref));

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }


            var rekvizit = new _Rekvizit();

            string sql = "";
            IDataReader reader;

            if (finder.nzp_kvar != 0)
            {
                sql =
                    " SELECT a.* " +
                    " FROM " +
                    pref + DBManager.sDataAliasRest + "s_bankstr a, " +
                    pref + DBManager.sDataAliasRest + "kvar b " +
                    " WHERE a.nzp_area=b.nzp_area " +
                    " AND a.nzp_geu=b.nzp_geu " +
                    " AND nzp_kvar = " + finder.nzp_kvar;
            }
            else
            {
                sql =
                    " SELECT * " +
                    " FROM " +
                    pref + DBManager.sDataAliasRest + "s_bankstr a " +
                    " WHERE nzp_area = " + finder.nzp_area + " and nzp_geu=" + finder.nzp_geu + " Order by nzp_area, nzp_geu";
            }
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            if (reader.Read())
            {
                rekvizit.filltext = 1;

                for (int i = 1; i <= 27; i++)
                {
                    try
                    {
                        if (reader["sb" + i] != DBNull.Value && Convert.ToString(reader["sb" + i]).Trim() != "")
                            rekvizit.bank += Convert.ToString(reader["sb" + i]).Trim();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        break;
                    }
                }


                if (reader["filltext"].ToString().Trim() != "1")
                {
                    try
                    {
                        rekvizit.nzp_geu = reader["nzp_geu"] != DBNull.Value ? Convert.ToInt32(reader["nzp_geu"]) : 0;
                        rekvizit.nzp_area = reader["nzp_area"] != DBNull.Value ? Convert.ToInt32(reader["nzp_area"]) : 0;
                        rekvizit.poluch = reader["sb1"] != DBNull.Value ? reader["sb1"].ToString().Trim() : string.Empty;
                        rekvizit.bank = reader["sb2"] != DBNull.Value ? reader["sb2"].ToString().Trim() : string.Empty;
                        rekvizit.rschet = reader["sb3"] != DBNull.Value ? reader["sb3"].ToString().Trim() : string.Empty;
                        rekvizit.korr_schet = reader["sb4"] != DBNull.Value ? reader["sb4"].ToString().Trim() : string.Empty;
                        rekvizit.bik = reader["sb5"] != DBNull.Value ? reader["sb5"].ToString().Trim() : string.Empty;
                        rekvizit.inn = reader["sb6"] != DBNull.Value ? reader["sb6"].ToString().Trim() : string.Empty;
                        rekvizit.phone = reader["sb7"] != DBNull.Value ? reader["sb7"].ToString().Trim() : string.Empty;
                        rekvizit.adres = reader["sb8"] != DBNull.Value ? reader["sb8"].ToString().Trim() : string.Empty;
                        rekvizit.pm_note = reader["sb9"] != DBNull.Value ? reader["sb9"].ToString().Trim() : string.Empty;
                        rekvizit.poluch2 = reader["sb10"] != DBNull.Value ? reader["sb10"].ToString().Trim() : string.Empty;
                        rekvizit.bank2 = reader["sb11"] != DBNull.Value ? reader["sb11"].ToString().Trim() : string.Empty;
                        rekvizit.rschet2 = reader["sb12"] != DBNull.Value ? reader["sb12"].ToString().Trim() : string.Empty;
                        rekvizit.korr_schet2 = reader["sb13"] != DBNull.Value ? reader["sb13"].ToString().Trim() : string.Empty;
                        rekvizit.bik2 = reader["sb14"] != DBNull.Value ? reader["sb14"].ToString().Trim() : string.Empty;
                        rekvizit.inn2 = reader["sb15"] != DBNull.Value ? reader["sb15"].ToString().Trim() : string.Empty;
                        rekvizit.phone2 = reader["sb16"] != DBNull.Value ? reader["sb16"].ToString().Trim() : string.Empty;
                        rekvizit.adres2 = reader["sb17"] != DBNull.Value ? reader["sb17"].ToString().Trim() : string.Empty;
                        rekvizit.kpp = reader["sb18"] != DBNull.Value ? reader["sb18"].ToString().Trim() : string.Empty;
                        rekvizit.kpp2 = reader["sb19"] != DBNull.Value ? reader["sb19"].ToString().Trim() : string.Empty;
                        rekvizit.fax = reader["sb20"] != DBNull.Value ? reader["sb20"].ToString().Trim() : string.Empty;
                        rekvizit.site = reader["sb21"] != DBNull.Value ? reader["sb21"].ToString().Trim() : string.Empty;
                        rekvizit.email = reader["sb22"] != DBNull.Value ? reader["sb22"].ToString().Trim() : string.Empty;
                        rekvizit.worktime = reader["sb23"] != DBNull.Value ? reader["sb23"].ToString().Trim() : string.Empty;
                        rekvizit.fax2 = reader["sb24"] != DBNull.Value ? reader["sb24"].ToString().Trim() : string.Empty;
                        rekvizit.site2 = reader["sb25"] != DBNull.Value ? reader["sb25"].ToString().Trim() : string.Empty;
                        rekvizit.email2 = reader["sb26"] != DBNull.Value ? reader["sb26"].ToString().Trim() : string.Empty;
                        rekvizit.worktime2 = reader["sb27"] != DBNull.Value ? reader["sb27"].ToString().Trim() : string.Empty;
                        rekvizit.filltext = 0;
                    }
                    catch (IndexOutOfRangeException) { }
                }
            }
            reader.Close();

            rekvizit.code_uk = "0";



            if (finder.nzp_kvar != 0)
            {
                sql = " Select * from " + pref + DBManager.sDataAliasRest + "prm_8 a, " + pref + DBManager.sDataAliasRest + "kvar b " +
                                                 " where a.nzp=b.nzp_geu  and a.is_actual<>100 and " +
                                                 " dat_s<=current_date and dat_po>=current_date and nzp_prm=714 " +
                                                 " and nzp_kvar = " + finder.nzp_kvar;
            }
            else
            {
                sql = " Select * from " + pref + DBManager.sDataAliasRest + "prm_8 " +
                                 " where nzp=" + finder.nzp_geu + "  and is_actual<>100 and " +
                                 " dat_s<=" + DBManager.sCurDate + " and dat_po>=" + DBManager.sCurDate + " and nzp_prm=714 ";
            }
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            if (reader.Read())
                if (reader["val_prm"] != DBNull.Value) rekvizit.code_uk = Convert.ToString(reader["val_prm"]).Trim();
            reader.Close();

            if (DBManager.getServer(conn_db) != "")
            {
                DbTables tables = new DbTables(conn_db);
                sql = " select re.remark from " + tables.s_remark + " re where re.nzp_area=" + finder.nzp_area + "";
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                if (reader.Read())
                {
                    if (reader["remark"] != DBNull.Value)
                    {
                        string remark = (System.Convert.ToString(reader["remark"]).Trim());
                        Encoding codepage = Encoding.Default;
                        try
                        {
                            rekvizit.remark = codepage.GetString(Convert.FromBase64String(remark.Trim()));
                        }
                        catch
                        {
                            rekvizit.remark = remark;
                        }
                    }
                }

            }
            conn_db.Close();

            if (rekvizit.code_uk == "0")
            {
                IDbConnection conn_central = GetConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_central, true);
                if (!ret.result)
                {
                    conn_central.Close();
                    MonitorLog.WriteLog("Ошибка определения центральной базы данных  ", MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }


                sql = " Select * from " + Points.Pref + DBManager.sKernelAliasRest + "s_erc_code where is_current = 1 ";

                if (!ExecRead(conn_central, out reader, sql, true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                    conn_central.Close();
                    return null;
                }
                else
                {
                    if (reader.Read())
                        if (reader["erc_code"] != DBNull.Value) rekvizit.code_uk = Convert.ToString(reader["erc_code"]).Trim();
                    reader.Close();
                    conn_central.Close();

                }
            }
            return rekvizit;
        }

        //----------------------------------------------------------------------
        public bool SaveLsRevizit(string pref, _Rekvizit uk, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (pref == "")
            {
                pref = Points.Pref;
            }
            // Проверка того, что ввел пользователь
            ret = checkUserInputs(uk);
            if (!ret.result && ret.tag == -1)
            {
                return false;
            }
            IDbConnection connDb = GetConnection(Points.GetConnByPref(pref));
            IDataReader reader;

            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при подключении к базе данных";
                ret.tag = -1;
                return false;
            }


            bool hasRecord = false;
            string sql =
                " SELECT * FROM " +
                pref + DBManager.sDataAliasRest + "s_bankstr a " +
                " WHERE nzp_area = " + uk.nzp_area +
                " AND nzp_geu = " + uk.nzp_geu;
            ret = DBManager.ExecRead(connDb, null, out reader, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки";
                ret.tag = -1;
                connDb.Close();
                return false;
            }
            if (reader.Read())
            {
                hasRecord = true;
            }
            reader.Close();


            #region Подготовка

            string val_st = "";
            if (uk.filltext == 1)
            {
                var delimiters = new char[] { '\r', '\n' };
                string[] st = uk.bank.Split(delimiters);
                int i = 0;
                foreach (string s in st)
                {
                    if (i < 27)
                        val_st = val_st + s.Replace("'", "") + "','";
                    i++;
                }
                for (int j = i; j < 27; j++)
                {
                    val_st = val_st + "','";
                }
                if (val_st != "") val_st = val_st.Substring(0, val_st.Length - 2);
            }

            #endregion


            if (hasRecord)
            {
                if (val_st != "")
                {
                    sql =
                        " UPDATE  " + pref + DBManager.sDataAliasRest + "s_bankstr SET " +
                        " (sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, " +
                        " sb11, sb12, sb13, sb14, sb15, sb16, sb17, sb18, sb19, " +
                        " sb20, sb21, sb22, sb23, sb24, sb25, sb26, sb27, filltext) " +
                        " = ('" + val_st + ", " + uk.filltext + ")" +
                        " WHERE nzp_area = " + uk.nzp_area +
                        " AND nzp_geu = " + uk.nzp_geu;
                }
                else
                {
                    sql = " UPDATE  " + pref + DBManager.sDataAliasRest + "s_bankstr SET " +
                          "(sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, " +
                        " sb11, sb12, sb13, sb14, sb15, sb16, sb17, sb18, sb19, " +
                        " sb20, sb21, sb22, sb23, sb24, sb25, sb26, sb27, filltext) " +
                          " = ('" +
                          uk.poluch.Replace("'", "") + "','" +
                          uk.bank.Replace("'", "") + "','" +
                          uk.rschet.Replace("'", "") + "','" +
                          uk.korr_schet.Replace("'", "") + "','" +
                          uk.bik.Replace("'", "") + "','" +
                          uk.inn.Replace("'", "") + "','" +
                          uk.phone.Replace("'", "") + "','" +
                          uk.adres.Replace("'", "") + "','" +
                          uk.pm_note.Replace("'", "") + "','" +
                          uk.poluch2.Replace("'", "") + "','" +
                          uk.bank2.Replace("'", "") + "','" +
                          uk.rschet2.Replace("'", "") + "','" +
                          uk.korr_schet2.Replace("'", "") + "','" +
                          uk.bik2.Replace("'", "") + "','" +
                          uk.inn2.Replace("'", "") + "','" +
                          uk.phone2.Replace("'", "") + "','" +
                          uk.adres2.Replace("'", "") + "','" +
                          uk.kpp.Replace("'", "") + "','" +
                          uk.kpp2.Replace("'", "") + "','" +
                          uk.fax.Replace("'", "") + "','" +
                          uk.site.Replace("'", "") + "','" +
                          uk.email.Replace("'", "") + "','" +
                          uk.worktime.Replace("'", "") + "','" +
                          uk.fax2.Replace("'", "") + "','" +
                          uk.site2.Replace("'", "") + "','" +
                          uk.email2.Replace("'", "") + "','" +
                          uk.worktime2.Replace("'", "") + "','" +
                          uk.filltext + "')" +
                          " WHERE nzp_area = " + uk.nzp_area +
                          " AND nzp_geu = " + uk.nzp_geu;
                }
            }
            else
            {
                if (val_st != "")
                {
                    sql =
                        " INSERT INTO  " + pref + DBManager.sDataAliasRest + "s_bankstr " +
                        " (nzp_area, nzp_geu, sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, " +
                        " sb11, sb12, sb13, sb14, sb15, sb16, sb17, sb18, sb19, " +
                        " sb20, sb21, sb22, sb23, sb24, sb25, sb26, sb27, filltext) " +
                        " VALUES (" + uk.nzp_area + "," + uk.nzp_geu + ",'" + val_st + "," + uk.filltext + ")";
                }
                else
                {
                    sql = " INSERT INTO   " + pref + DBManager.sDataAliasRest + "s_bankstr " +
                          "(nzp_area, nzp_geu, sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, " +
                          " sb11, sb12, sb13, sb14, sb15, sb16, sb17, sb18, sb19, " +
                          " sb20, sb21, sb22, sb23, sb24, sb25, sb26, sb27, filltext) " +
                          " VALUES (" + uk.nzp_area + "," + uk.nzp_geu + ",'" +
                          uk.poluch.Replace("'", "") + "','" +
                          uk.bank.Replace("'", "") + "','" +
                          uk.rschet.Replace("'", "") + "','" +
                          uk.korr_schet.Replace("'", "") + "','" +
                          uk.bik.Replace("'", "") + "','" +
                          uk.inn.Replace("'", "") + "','" +
                          uk.phone.Replace("'", "") + "','" +
                          uk.adres.Replace("'", "") + "','" +
                          uk.pm_note.Replace("'", "") + "','" +
                          uk.poluch2.Replace("'", "") + "','" +
                          uk.bank2.Replace("'", "") + "','" +
                          uk.rschet2.Replace("'", "") + "','" +
                          uk.korr_schet2.Replace("'", "") + "','" +
                          uk.bik2.Replace("'", "") + "','" +
                          uk.inn2.Replace("'", "") + "','" +
                          uk.phone2.Replace("'", "") + "','" +
                          uk.adres2.Replace("'", "") + "','" +
                          uk.kpp.Replace("'", "") + "','" +
                          uk.kpp2.Replace("'", "") + "','" +
                          uk.fax.Replace("'", "") + "','" +
                          uk.site.Replace("'", "") + "','" +
                          uk.email.Replace("'", "") + "','" +
                          uk.worktime.Replace("'", "") + "','" +
                          uk.fax2.Replace("'", "") + "','" +
                          uk.site2.Replace("'", "") + "','" +
                          uk.email2.Replace("'", "") + "','" +
                          uk.worktime2.Replace("'", "") + "','" +
                          uk.filltext + "')";
                }
            }

            if (!ExecRead(connDb, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка сохранения реквизитов " + sql, MonitorLog.typelog.Error, 20, 201, true);
                connDb.Close();
                ret.result = false;
                ret.text = "Ошибка сохранения реквизитов";

                return false;
            }

            DbTables tables = new DbTables(connDb);

#if PG
            sql = "WITH upsert as" +
                  "(update " + tables.s_remark + " b set (remark) = (" + Utils.EStrNull(uk.remark, 2000, "") +
                  ") from (Select nzp_area  from " + pref + "_data.s_bankstr where nzp_area=" + uk.nzp_area +
                  " limit 1) d where b.nzp_area = d.nzp_area  RETURNING b.*)" +
                  "insert into " + pref + "_data.s_remark select a.nzp_area, 0, 0,  " + Utils.EStrNull(uk.remark, 2000, "") +
                  "  from (Select nzp_area  from " + pref + "_data.s_bankstr where nzp_area= " + uk.nzp_area +
                  " limit 1) a where a.nzp_area not in (select b.nzp_area from upsert b);";

#else
            Encoding codepage = Encoding.Default;
            sql = " MERGE INTO " + tables.s_remark + " b" +
                                  " USING (Select FIRST 1 nzp_area  from " + pref + "_data:s_bankstr where nzp_area= " + uk.nzp_area + ") e" +
                                   " ON (b.nzp_area = e.nzp_area)" +
                                   " WHEN MATCHED THEN" +
                //" update set (remark) = (" + Utils.EStrNull(uk.remark) + ") " +

                                   " update set (remark) = ('" +
                                   System.Convert.ToBase64String(codepage.GetBytes(uk.remark)) + "') " +
                                   " WHEN NOT MATCHED THEN" +
                                   " insert (nzp_area, nzp_geu, nzp_dom, remark) values (e.nzp_area, 0, 0, '" +
                                   System.Convert.ToBase64String(codepage.GetBytes(uk.remark)) + "')";
#endif
            if (!ExecRead(connDb, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка сохранения примечания " + sql, MonitorLog.typelog.Error, 20, 201, true);
                connDb.Close();
                ret.result = false;
                ret.text = "Ошибка сохранения примечания";

                return false;
            }

            connDb.Close();
            return true;
        }
        /// <summary>
        /// Проверка того, что ввел пользователь
        /// </summary>
        /// <param name="uk"></param>
        /// <returns></returns>
        private Returns checkUserInputs(_Rekvizit uk)
        {
            Returns ret = Utils.InitReturns();
            bool isFreeFill = uk.filltext == 1;
            Dictionary<string[], int> checkLenDict;
            // Если установлена галочка произвольного ввода
            if (isFreeFill)
            {
                // то проверяем только примечание и поле для произвольного ввода реквизитов
                checkLenDict = new Dictionary<string[], int>
                {
                    {new[] {uk.bank, "произвольного ввода"}, 100},
                    {new[] {uk.remark, "Примечание"}, 2000}
                };
            }
            else
            {
                // иначе, проверяем все поля, кроме поля произвольного ввода реквизитов

                // Словарь полей со значениями, которые должны проходить проверку на длину и на числовое значение
                Dictionary<string[], int> checkIsNumerAndLenDict = new Dictionary<string[], int>
                {
                    {new[] {uk.rschet, "Расчетный счет (Расчетный центр)"}, 20},
                    {new[] {uk.korr_schet , "Корр. счет (Расчетный центр)"}, 20},
                    {new[] {uk.kpp, "КПП (Расчетный центр)"}, 9},
                    {new[] {uk.bik, "БИК (Расчетный центр)"}, 9},
                    {new[] {uk.rschet2, "Расчетный счет (Исполнитель)"}, 20},
                    {new[] {uk.korr_schet2, "Корр. счет (Исполнитель)"}, 20},
                    {new[] {uk.kpp2, "КПП (Исполнитель)"}, 9},
                    {new[] {uk.bik2, "БИК (Исполнитель)"}, 9},
                };
                ret = checkLengthAndIsNumeric(checkIsNumerAndLenDict, true);
                if (!ret.result && ret.tag == -1)
                {
                    return ret;
                }
                //Для ИНН отдельная проверка, т.к. ИНН может иметь длину 10 или 12 символов
                ret = checkInn(uk.inn, "Расчетный центр");
                if (!ret.result && ret.tag == -1)
                {
                    return ret;
                }
                //Для ИНН отдельная проверка, т.к. ИНН может иметь длину 10 или 12 символов
                ret = checkInn(uk.inn2, "Исполнитель");
                if (!ret.result && ret.tag == -1)
                {
                    return ret;
                }
                // Словарь полей со значениями, которые должны проходить проверку только на длину
                checkLenDict = new Dictionary<string[], int>
                {
                    {new[] {uk.poluch, "Наименование (Расчетный центр)"}, 100},
                    {new[] {uk.bank, "Банк (Расчетный центр)"}, 100},
                    {new[] {uk.adres, "Адрес (Расчетный центр)"}, 50},
                    {new[] {uk.pm_note, "Примечание (Расчетный центр)"}, 100},
                    {new[] {uk.phone, "Телефон (Расчетный центр)"}, 50},
                    {new[] {uk.poluch2, "Наименование (Исполнитель)"}, 100},
                    {new[] {uk.bank2, "Банк (Исполнитель)"}, 100},
                    {new[] {uk.adres2, "Адрес (Исполнитель)"}, 50},
                    {new[] {uk.phone2, "Телефон (Исполнитель)"}, 50},
                    {new[] {uk.fax, "Факс (Расчетный центр)"}, 100},
                    {new[] {uk.site, "Сайт (Расчетный центр)"}, 100},
                    {new[] {uk.email, "Электронная почта (Расчетный центр)"}, 100},
                    {new[] {uk.worktime, "Режим работы (Расчетный центр)"}, 100},
                    {new[] {uk.fax2, "Факс (Исполнитель)"}, 100},
                    {new[] {uk.site2, "Сайт (Исполнитель)"}, 100},
                    {new[] { uk.email2 , "Электронная почта (Исполнитель)"}, 100},
                    {new[] {uk.worktime2, "Режим работы (Исполнитель)"}, 100},
                    {new[] {uk.remark, "Примечание"}, 2000},
                   
                };
            }
            return checkLengthAndIsNumeric(checkLenDict, false);
        }
        /// <summary>
        /// Для ИНН отдельная проверка, т.к. ИНН может иметь длину 10 или 12 символов
        /// </summary>
        /// <param name="inn"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private Returns checkInn(string inn, string source)
        {
            Returns ret = Utils.InitReturns();
            if (String.IsNullOrWhiteSpace(inn)) return ret;
            string innTrim = inn.Trim();
            if (innTrim != "" && (innTrim.Length != 10 && innTrim.Length != 12))
            {
                ret.tag = -1;
                ret.result = false;
                ret.text = "Непустое поле ИНН (" + source + ") должно содержать 10 или 12 символов";
                return ret;
            }
            if (innTrim.Contains(".") || innTrim.Contains(","))
            {
                ret.tag = -1;
                ret.result = false;
                ret.text = "Непустое поле ИНН (" + source + ") должно содержать только цифры ";
                return ret;
            }
            Decimal parseDecimal;
            if (!Decimal.TryParse(innTrim, out parseDecimal))
            {
                ret.tag = -1;
                ret.result = false;
                ret.text = "Непустое поле ИНН (" + source + ") должно содержать только цифры ";
                return ret;
            }
            return ret;
        }
        /// <summary>
        /// Проверяет содержимое полей ввода
        /// </summary>
        /// <param name="limitDictionary"></param>
        /// <param name="isNumericCheck">true- проверять еще и на численное значение</param>
        /// <returns></returns>
        private Returns checkLengthAndIsNumeric(Dictionary<string[], int> limitDictionary, bool isNumericCheck)
        {
            Returns ret = Utils.InitReturns();
            foreach (KeyValuePair<string[], int> keyValuePair in limitDictionary)
            {
                if (isNumericCheck)
                {
                    if (keyValuePair.Key[0].Trim() != "" && keyValuePair.Key[0].Trim().Length != keyValuePair.Value)
                    {
                        ret.tag = -1;
                        ret.result = false;
                        ret.text = "Непустое поле " + keyValuePair.Key[1] + " должно содержать " + keyValuePair.Value +
                        " символов";
                        return ret;
                    }

                    if (keyValuePair.Key[0].Trim() != "")
                    {
                        if (keyValuePair.Key[0].Contains(".") || keyValuePair.Key[0].Contains(","))
                        {
                            ret.tag = -1;
                            ret.result = false;
                            ret.text = "Непустое поле " + keyValuePair.Key[1] + " должно содержать только цифры ";
                            return ret;
                        }
                        Decimal parseDecimal;
                        if (!Decimal.TryParse(keyValuePair.Key[0].Trim(), out parseDecimal))
                        {
                            ret.tag = -1;
                            ret.result = false;
                            ret.text = "Непустое поле " + keyValuePair.Key[1] + " должно содержать только цифры ";
                            return ret;
                        }
                    }
                }
                else
                {
                    if (keyValuePair.Key[0].Trim() != "" && keyValuePair.Key[0].Trim().Length > keyValuePair.Value)
                    {
                        ret.tag = -1;
                        ret.result = false;
                        ret.text = "Непустое поле " + keyValuePair.Key[1] + " должно содержать не более " + keyValuePair.Value +
                                   " символов";
                        return ret;
                    }
                }
            }
            return ret;
        }


        //----------------------------------------------------------------------
        public string GetKolGil(MonthLs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string kol_gil = "0";
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return kol_gil;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return kol_gil;
            }

            //заполнить webdata:tXX_spls
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_db.Close();
                return kol_gil;
            }

            string sql = "";

            IDataReader reader;

            //finder.

#if PG
            sql = " Select " + finder.pref + "_data.get_kol_gil('01." + finder.dat_month.Month.ToString() + "." +
                finder.dat_month.Year.ToString() + "','" + DateTime.DaysInMonth(finder.dat_month.Year, finder.dat_month.Month).ToString() +
                "." + finder.dat_month.Month.ToString() + "." + finder.dat_month.Year.ToString() + "',15,nzp_kvar) as kol_gil " +
                   "from " + finder.pref + "_data.kvar b " +
                  " where  nzp_kvar = " + finder.nzp_kvar.ToString();
#else
            sql = " Select " + finder.pref + "_data:get_kol_gil('01." + finder.dat_month.Month.ToString() + "." +
                finder.dat_month.Year.ToString() + "','" + DateTime.DaysInMonth(finder.dat_month.Year, finder.dat_month.Month).ToString() +
                "." + finder.dat_month.Month.ToString() + "." + finder.dat_month.Year.ToString() + "',15,nzp_kvar) as kol_gil " +
                   "from " + finder.pref + "_data:kvar b " +
                  " where  nzp_kvar = " + finder.nzp_kvar.ToString();
#endif
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            else
            {
                if (reader.Read())
                {
                    if (reader["kol_gil"] != DBNull.Value) kol_gil = Convert.ToString(reader["kol_gil"]);

                }
                reader.Close();
                conn_db.Close();
            }
            return kol_gil;
        }



        /// <summary>
        /// групповая операция исправить данные домов
        /// </summary>
        /// <param name="dom">данные для изменения</param>
        /// <param name="ret">результат выполнения операции</param>
        public void UpdateGroup(Dom dom, out Returns ret)
        {
            //инициализация результата
            ret = Utils.InitReturns();

            if (dom.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Пользователь не определен";
                return;
            }

            //соединение с основной БД
            string conn_kernel = Points.GetConnByPref(dom.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

            //соединение с кэш БД 
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            //сформировать запрос для сохранения
            string what = "";
            if (dom.nzp_area > 0) what += " nzp_area = " + dom.nzp_area.ToString();
            if (dom.nzp_geu > 0)
            {
                if (what != "") what += " , ";
                what += " nzp_geu = " + dom.nzp_geu.ToString();
            }
            if (what == "")
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Нет данных для сохранения";
                conn_db.Close();
                conn_web.Close();
                return;
            }

            //получить список префиксов в выбранном списке домов в кэше
            List<string> preflist = new List<string>();
            IDataReader reader;
            string tXX_spdom = "t" + Convert.ToString(dom.nzp_user) + "_spdom";
            string sql = "select distinct pref from " + tXX_spdom + " where mark =1";
            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                ret.result = false;
                conn_db.Close();
                conn_web.Close();
                return;
            }
            try
            {
                while (reader.Read())
                    if (reader["pref"] != DBNull.Value) preflist.Add(((string)reader["pref"]).Trim());
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка UpdateGroup домов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return;
            }

            //Андрей, объясни мне что это за код
            foreach (string pref in preflist)
            {

                sql = "select count(*) as num from " + pref + sDataAliasRest + "s_area where nzp_area = " + dom.nzp_area;

                IDbCommand cmd = DBManager.newDbCommand(sql, conn_db);
                try
                {
                    string s = Convert.ToString(cmd.ExecuteScalar());
                    ret.tag = Convert.ToInt32(s);
                    if (ret.tag == 0)
                    {
                        sql = " Insert into " + pref + sDataAliasRest + "s_area (nzp_area, area, nzp_supp) select nzp_area, area, nzp_supp " +
                              " From " + Points.Pref + sDataAliasRest + "s_area " +
                              " Where " + " nzp_area = " + dom.nzp_area;

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи данных дома в групповых операциях при добавлении Управляющей организации в банк " + pref;
                            conn_db.Close();
                            conn_web.Close();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    conn_db.Close();
                    conn_web.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    string err;
                    if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
                    MonitorLog.WriteLog("Ошибка записи данных дома в групповых операциях при проверке Управляющих организаций" + err, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }

                sql = "select count(*) as num from " + pref + sDataAliasRest + "s_geu where nzp_geu = " + dom.nzp_geu;

                cmd = DBManager.newDbCommand(sql, conn_db);
                try
                {
                    string s = Convert.ToString(cmd.ExecuteScalar());
                    ret.tag = Convert.ToInt32(s);
                    if (ret.tag == 0)
                    {
                        sql = "insert into " + pref + sDataAliasRest + "s_geu (nzp_geu, geu)" +
                              " select nzp_geu, geu from " + Points.Pref + sDataAliasRest + "s_geu where " +
                            " nzp_geu = " + dom.nzp_geu;

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи данных дома в групповых операциях при добавлении ЖЭУ в банк " + pref;
                            conn_db.Close();
                            conn_web.Close();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    conn_db.Close();
                    conn_web.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    string err;
                    if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
                    MonitorLog.WriteLog("Ошибка записи данных дома в групповых операциях при проверке ЖЭУ" + err, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }

                DbTables tables = new DbTables(conn_db);
                if (dom.clear_remark == true) { dom.remark = ""; };
                if (dom.remark == "" && dom.clear_remark == true || dom.remark != "")
                {
#if PG
                    ////Сначала меняем существующие
                    //sql = " UPDATE  " + tables.s_remark + " SET remark = " + Utils.EStrNull(dom.remark) +
                    //      " WHERE nzp_dom in (select d.nzp_dom from " + pgDefaultDb + "." + tXX_spdom +" d " +
                    //      "                   where mark=1 and d.pref = '" + pref + "')";
                    //ret = ExecSQL(conn_db, sql, true);
                    //if (!ret.result)
                    //{
                    //    ret.text = "Ошибка записи данных дома в групповых операциях";
                    //    conn_db.Close();
                    //    conn_web.Close();
                    //    return;
                    //}
                    ////Добавляем новые 
                    //sql = " insert into " + tables.s_remark + " (nzp_area, nzp_geu, nzp_dom, remark) " +
                    //      " select 0, 0, nzp_dom, " + Utils.EStrNull(dom.remark) +
                    //      " from " + pgDefaultDb + "." + tXX_spdom + " d " +
                    //      "                   where mark=1 and d.pref = '" + pref + "'";

                    sql = "WITH upsert as" +
                    "(update " + tables.s_remark + " b set (remark) = (" + Utils.EStrNull(dom.remark) + ") from (select d.nzp_dom from " + pgDefaultDb + "." + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "') d where b.nzp_dom = d.nzp_dom RETURNING b.*)" +
                    "insert into " + tables.s_remark + " select 0, 0, a.nzp_dom,  " + Utils.EStrNull(dom.remark) + "  from (select d.nzp_dom from " + pgDefaultDb + "." + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "') a where a.nzp_dom not in (select b.nzp_dom from upsert b);";


#else
                    sql = " MERGE INTO " + tables.s_remark + " b" +
                                               " USING (select d.nzp_dom from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "') e" +
                                                " ON (b.nzp_dom = e.nzp_dom)" +
                                                " WHEN MATCHED THEN" +
                                                " update set (remark) = (" + Utils.EStrNull(dom.remark) + ") " +
                                                " WHEN NOT MATCHED THEN" +
                                                " insert (nzp_area, nzp_geu, nzp_dom, remark) values (0, 0, e.nzp_dom, " + Utils.EStrNull(dom.remark) + ")";
#endif

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи данных дома в групповых операциях";
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                }

                sql = "update " + pref + sDataAliasRest + "dom " +
                            " Set " + what +
                            " Where nzp_dom in (select d.nzp_dom from " + DBManager.GetFullBaseName(conn_web) + tableDelimiter + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "')";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных дома в групповых операциях";
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                sql = "update " + pref + "_data" + tableDelimiter + "kvar " +
                "set  " + what +
#if PG
 " Where nzp_dom in (select d.nzp_dom from " + DBManager.GetFullBaseName(conn_web) + "." + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "')";
#else
                " Where nzp_dom in (select d.nzp_dom from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "')";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных дома в групповых операциях";
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }

            string dop = "";
            if (dom.area.Trim() != "") dop += " , area = '" + dom.area.Trim() + "'";
            if (dom.geu.Trim() != "") dop += " , geu = '" + dom.geu.Trim() + "'";
            sql = "update " + tXX_spdom + " set " + what + dop + " where mark= 1";
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи данных дома в групповых операциях";
                conn_db.Close();
                conn_web.Close();
                return;
            }

            //обновить информацию в центральном банке
            sql = "select pref, nzp_dom from " + tXX_spdom + " where mark = 1";

            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                ret.result = false;
                conn_db.Close();
                conn_web.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    Dom finderls = new Dom();
                    if (reader["pref"] != DBNull.Value) finderls.pref = ((string)reader["pref"]).Trim();
                    if (reader["nzp_dom"] != DBNull.Value) finderls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);

                    ret = RefreshDom(conn_db, finderls);
                    if (!ret.result)
                    {
                        ret.text = "Не удалось корректно обновить данные в центральном БД";
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка UpdateGroup домов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return;
            }

            conn_web.Close();
            conn_db.Close();
            return;
        }

        /// <summary>
        /// Групповая операция с лицевыми счетами
        /// </summary>
        /// <param name="ls">данные для изменения</param>
        /// <param name="ret">результат</param>
        public Returns UpdateGroup(Ls ls)
        {
            //инициализация результата
            Returns ret = Utils.InitReturns();

            if (ls.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Пользователь не определен";
                return ret;
            }

            //соединение с основной БД
            string conn_kernel = Points.GetConnByPref(ls.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            //соединение с кэш БД 
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            //сформировать запрос для сохранения
            string what = "";
            if (ls.nzp_area > 0) what += " nzp_area = " + ls.nzp_area.ToString();
            if (ls.nzp_geu > 0)
            {
                if (what != "") what += " , ";
                what += " nzp_geu = " + ls.nzp_geu.ToString();
            }
            if (ls.uch != "")
            {
                if (what != "") what += " , ";
                what += " uch = " + ls.uch;
            }
            if (what == "")
            {
                ret.result = false;
                ret.text = "Нет данных для сохранения";
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            //получить список префиксов в выбранном списке домов в кэше
            List<string> preflist = new List<string>();
            IDataReader reader;
            string tXX_selectedls = "t" + Convert.ToString(ls.nzp_user) + "_selectedls" + ls.listNumber;
            string tXX_spls = "t" + Convert.ToString(ls.nzp_user) + "_spls";

            if (!TempTableInWebCashe(conn_web, tXX_selectedls))
            {
                ret.result = false;
                ret.text = "Список лицевых счетов не выбран";
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            string sql = "select distinct pref from " + tXX_selectedls + " where mark=1";
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }
            try
            {
                while (reader.Read())
                    if (reader["pref"] != DBNull.Value) preflist.Add(((string)reader["pref"]).Trim());
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка UpdateGroup лицевых счетов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            foreach (string pref in preflist)
            {
                sql = "select count(*) as num from " + pref + "_data" + DBManager.tableDelimiter + "s_area where nzp_area = " + ls.nzp_area;
                IDbCommand cmd = DBManager.newDbCommand(sql, conn_db);
                try
                {
                    string s = Convert.ToString(cmd.ExecuteScalar());
                    ret.tag = Convert.ToInt32(s);
                    if (ret.tag == 0)
                    {
#if PG
                        sql = "insert into " + pref + "_data.s_area (nzp_area, area, nzp_supp, nzp_payer) select nzp_area, area, nzp_supp, nzp_payer from " + Points.Pref + "_data.s_area where " +
                            " nzp_area = " + ls.nzp_area;
#else
                        sql = "insert into " + pref + "_data:s_area (nzp_area, area, nzp_supp, nzp_payer) select nzp_area, area, nzp_supp, nzp_payer from " + Points.Pref + "_data:s_area where " +
                            " nzp_area = " + ls.nzp_area;
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи данных лиц счета в групповых операциях при добавлении Управляющей организации в банк " + pref;
                            conn_db.Close();
                            conn_web.Close();
                            return ret;
                        }
                    }
                }
                catch (Exception ex)
                {
                    conn_db.Close();
                    conn_web.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    string err;
                    if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
                    MonitorLog.WriteLog("Ошибка записи данных лиц счета в групповых операциях при проверке Управляющих организаций" + err, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

#if PG
                sql = "select count(*) as num from " + pref + "_data.s_geu where nzp_geu = " + ls.nzp_geu;
#else
                sql = "select count(*) as num from " + pref + "_data:s_geu where nzp_geu = " + ls.nzp_geu;
#endif
                cmd = DBManager.newDbCommand(sql, conn_db);
                try
                {
                    string s = Convert.ToString(cmd.ExecuteScalar());
                    ret.tag = Convert.ToInt32(s);
                    if (ret.tag == 0)
                    {
#if PG
                        sql = "insert into " + pref + "_data.s_geu (nzp_geu, geu) select nzp_geu, geu from " + Points.Pref + "_data.s_geu where " +
                            " nzp_geu = " + ls.nzp_geu;
#else
                        sql = "insert into " + pref + "_data:s_geu (nzp_geu, geu) select nzp_geu, geu from " + Points.Pref + "_data:s_geu where " +
                            " nzp_geu = " + ls.nzp_geu;
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи данных лиц счета в групповых операциях при добавлении ЖЭУ в банк " + pref;
                            conn_db.Close();
                            conn_web.Close();
                            return ret;
                        }
                    }
                }
                catch (Exception ex)
                {
                    conn_db.Close();
                    conn_web.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    string err;
                    if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
                    MonitorLog.WriteLog("Ошибка записи данных лиц счета в групповых операциях при проверке ЖЭУ" + err, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

#if PG
                sql = "update " + pref + "_data.kvar " +
                            " Set " + what +
                            " Where nzp_kvar in (select k.nzp_kvar from " + pgDefaultDb + "." + tXX_selectedls + " k where k.pref = '" + pref + "' and mark = 1)";
#else
                sql = "update " + pref + "_data:kvar " +
                            " Set " + what +
                            " Where nzp_kvar in (select k.nzp_kvar from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_selectedls + " k where k.pref = '" + pref + "' and mark = 1)";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи данных лиц счета в групповых операциях";
                    conn_db.Close();
                    conn_web.Close();
                    return ret;
                }
            }


            what = "";
            if (ls.nzp_area > 0) what += " nzp_area = " + ls.nzp_area.ToString();
            if (ls.nzp_geu > 0)
            {
                if (what != "") what += " , ";
                what += " nzp_geu = " + ls.nzp_geu.ToString();
            }
            if (what != "")
            {

                if (TempTableInWebCashe(conn_web, tXX_spls))
                {
                    sql = "update " + tXX_spls + " set " + what + " where nzp_kvar in (select nzp_kvar from " + tXX_selectedls + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи данных лиц счетов в групповых операциях";
                        conn_db.Close();
                        conn_web.Close();
                        return ret;
                    }
                }
            }

            //обновить информацию в центральном банке
            sql = "select pref, nzp_kvar from " + tXX_selectedls + " where mark = 1";
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }
            try
            {
                while (reader.Read())
                {
                    Ls finderls = new Ls();
                    if (reader["pref"] != DBNull.Value) finderls.pref = ((string)reader["pref"]).Trim();
                    if (reader["nzp_kvar"] != DBNull.Value) finderls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);

                    ret = RefreshKvar(conn_db, null, finderls);
                    if (!ret.result)
                    {
                        ret.text = "Не удалось корректно обновить данные в центральном БД";
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка UpdateGroup лицевых счетов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }


            conn_web.Close();
            conn_db.Close();
            return ret;
        }





        public Prm FindPrmValueForOpenLs(IDbConnection conn_db, IDbTransaction transaction, Prm finder, out Returns ret)
        {
            Prm prm = new Prm();

            DateTime dat = new DateTime(finder.year_, finder.month_, 1);
#if PG
            string prm_N_p = finder.pref + "_data." + "prm_1 p ";
            string prm_3 = finder.pref + "_data." + "prm_3";
            string sqldop = "Select nzp_kvar from " + finder.pref + "_data.kvar k " +
                  " Where k.nzp_dom = " + finder.nzp + " and (select max(val_prm) from " + prm_3 + " p3 where p3.nzp_prm=51 and p3.nzp=k.nzp_kvar " +
                  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p3.dat_s and p3.dat_po and p3.is_actual <> 100)='1'";
            string type = "numeric(14,2)";
            if (finder.nzp_prm == 5 || finder.nzp_prm == 2005) type = "int";
            string sqlstr = "Select sum(cast(coalesce(p.val_prm,'0') as " + type + ")) as val From " + prm_N_p +
                  " Where  p.nzp_prm = " + finder.nzp_prm + " and p.nzp in (" + sqldop + ") " +
                  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po and p.is_actual <> 100";
#else
            string prm_N_p = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":" + "prm_1 p ";
            string prm_3 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":" + "prm_3";
            string sqldop = "Select nzp_kvar from " + finder.pref + "_data@" + DBManager.getServer(conn_db) + ":kvar k " +
                  " Where k.nzp_dom = " + finder.nzp + " and (select max(val_prm) from " + prm_3 + " p3 where p3.nzp_prm=51 and p3.nzp=k.nzp_kvar " +
                  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p3.dat_s and p3.dat_po and p3.is_actual <> 100)=1";
            string type = "decimal(14,2)";
            if (finder.nzp_prm == 5 || finder.nzp_prm == 2005) type = "int";
            string sqlstr = "Select sum(cast(nvl(p.val_prm,0) as " + type + ")) as val From " + prm_N_p +
                  " Where  p.nzp_prm = " + finder.nzp_prm + " and p.nzp in (" + sqldop + ") " +
                  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po and p.is_actual <> 100";
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sqlstr, true);
            if (!ret.result)
            {
                reader.Close();
                return prm;
            }
            if (reader.Read())
            {
                if (reader["val"] != DBNull.Value) prm.val_prm = Convert.ToString(reader["val"]);
                prm.nzp_prm = finder.nzp_prm;
            }
            reader.Close();
            return prm;
        }





        //----------------------------------------------------------------------
        public Returns Generator(List<Prm> listprm, int nzp_user)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            //string tXX_spls = "t" + nzp_user + "_spls";
            //string tXX_spls_full = conn_web.Database + "@" + conn_web.Server + ":" + tXX_spls;


            //постройка данных
            FindPrmAll(conn_web, conn_db, listprm, nzp_user, out ret);

            conn_db.Close();
            conn_web.Close();


            return ret;
        }
        //----------------------------------------------------------------------
        public Returns Generator(List<int> listint, int nzp_user, int yy, int mm)
        //----------------------------------------------------------------------
        {
            return Generator(listint, nzp_user, yy, mm, true);
        }
        //----------------------------------------------------------------------
        public Returns Generator(List<int> listint, int nzp_user, int yy, int mm, bool all_ls)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            //постройка данных
            FindSaldoAll(conn_web, conn_db, listint, nzp_user, yy, mm, all_ls, out ret);

            conn_db.Close();
            conn_web.Close();


            return ret;
        }
        //----------------------------------------------------------------------
        private bool FindSaldoAll(IDbConnection conn_web, IDbConnection conn_db, List<int> listint, int nzp_user, int yy, int mm, bool all_ls, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string tXX_spls = "t" + nzp_user + "_spls";
#if PG
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

            string tXX_spall = "t" + nzp_user + "_saldoall ";
#if PG
            string tXX_spall_full = pgDefaultDb + "." + tXX_spall;
#else
            string tXX_spall_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spall;
#endif


            /*
            0- Услуга
            1- Поставщик
            2- Тариф
            3- Вход. сальдо
            4- Расчет за месяц
            5- Перерасчет
            6- Изменения
            7- Недопоставка
            8- Оплачено
            9- К оплате
            10-Исход. сальдо
            */

            string s_sel =
                " Insert into " + tXX_spall_full +
                " Select t.nzp_kvar";

            string s_sel_sum = "";
            string s_table_sum = "";

            string s_sel_serv = "";
            string s_table_serv = "";
            string s_sel_supp = "";
            string s_table_supp = "";

            int groupby = 2;
            string s_table =
                " Create table " + tXX_spall +
                " ( no serial not null, nzp_kvar integer ";

            foreach (int i in listint)
            {
                switch (i)
                {
                    case 0:
                        {
                            s_sel_serv = ", service,ordering ";
                            s_table_serv = ", service char(40), ordering integer ";
                            groupby += 2;
                            break;
                        }
                    case 1:
                        {
                            s_sel_supp = ", name_supp ";
                            s_table_supp = ", name_supp char(100) ";
                            groupby += 1;
                            break;
                        }
                    case 2:
                        {
                            s_sel_sum += ", max(tarif) as tarif ";
#if PG
                            s_table_sum += ", tarif numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", tarif decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 3:
                        {
                            s_sel_sum += ", sum(sum_insaldo) as sum_insaldo ";
#if PG
                            s_table_sum += ", sum_insaldo numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", sum_insaldo decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 4:
                        {
                            s_sel_sum += ", sum(sum_tarif) as sum_tarif ";
#if PG
                            s_table_sum += ", sum_tarif numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", sum_tarif decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 5:
                        {
                            s_sel_sum += ", sum(reval) as reval ";
#if PG
                            s_table_sum += ", reval numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", reval decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 6:
                        {
                            s_sel_sum += ", sum(real_charge) as real_charge ";
#if PG
                            s_table_sum += ", real_charge numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", real_charge decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 7:
                        {
                            s_sel_sum += ", sum(sum_nedop) as sum_nedop ";
#if PG
                            s_table_sum += ", sum_nedop numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", sum_nedop decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 8:
                        {
                            s_sel_sum += ", sum(sum_money) as sum_money ";
#if PG
                            s_table_sum += ", sum_money numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", sum_money decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 9:
                        {
                            s_sel_sum += ", sum(sum_charge) as sum_charge ";
#if PG
                            s_table_sum += ", sum_charge numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", sum_charge decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                    case 10:
                        {
                            s_sel_sum += ", sum(sum_outsaldo) as sum_outsaldo ";
#if PG
                            s_table_sum += ", sum_outsaldo numeric(14,2) default 0.00 ";
#else
                            s_table_sum += ", sum_outsaldo decimal(14,2) default 0.00 ";
#endif

                            break;
                        }
                }
            }

            ExecSQL(conn_web, " Drop table " + tXX_spall, false);

            //создать t_saldoall
            s_table = s_table + s_table_serv.Replace("#nzp_serv#", "nzp_serv") + s_table_supp.Replace("#nzp_supp#", "nzp_supp") + s_table_sum + ")";
            ret = ExecSQL(conn_web, s_table, true);
            if (!ret.result)
            {
                return false;
            }
#if PG
            ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
            ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif



            s_sel = s_sel + s_sel_serv + s_sel_supp + s_sel_sum +
                " From " + tXX_spls_full + " t, CHARGE_XX ch, SERVICES_XX s, SUPPLIER_XX p " +
                " Where t.nzp_kvar = ch.nzp_kvar " +
                "   and ch.nzp_serv > 1 and dat_charge is null " +
                "   and ch.nzp_serv = s.nzp_serv " +
                "   and ch.nzp_supp = p.nzp_supp " +
                " Group by 1 ";

            for (int i = 2; i <= groupby; i++)
                s_sel = s_sel + "," + i;


            // цикл по pref 
            IDataReader reader;
#if PG
            if (!ExecRead(conn_db, out reader, " Select distinct pref From " + tXX_spls_full, true).result)
#else
            if (!ExecRead(conn_db, out reader, " Select unique pref From " + tXX_spls_full, true).result)
#endif
            {
                return false;
            }
            try
            {
                //заполнить t_saldoall
                while (reader.Read())
                {
                    string pref = (string)(reader["pref"]);
                    pref = pref.Trim();

#if PG
                    string charge_xx = pref + "_charge_" + (yy % 100).ToString("00") + ".charge_" + mm.ToString("00");
                    string services = pref + "_kernel.services ";
                    string supplier = pref + "_kernel.supplier ";
#else
                    string charge_xx = pref + "_charge_" + (yy % 100).ToString("00") + ":charge_" + mm.ToString("00");
                    string services = pref + "_kernel:services ";
                    string supplier = pref + "_kernel:supplier ";
#endif


                    string s_sql;
                    s_sql = s_sel.Replace("CHARGE_XX", charge_xx);
                    s_sql = s_sql.Replace("SERVICES_XX", services);
                    s_sql = s_sql.Replace("SUPPLIER_XX", supplier);

                    ret = ExecSQL(conn_db, s_sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

                return false;
            }


            ExecSQL(conn_web, " Create unique index ix1_" + tXX_spall + " on " + tXX_spall + "(no)", true);
            ExecSQL(conn_web, " Create index ix2_" + tXX_spall + " on " + tXX_spall + "(nzp_kvar)", true);
#if PG
            ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
            ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif



            if (all_ls)
            {
                //добавить недостающие лс в saldoall
                ExecSQL(conn_web, " Drop table ttt1_sld ", false);

#if PG
                ExecSQL(conn_web,
                                    " Select distinct nzp_kvar  Into UNLOGGED ttt1_sld  From " + tXX_spls +
                                    " Where nzp_kvar not in ( Select nzp_kvar From " + tXX_spall + " ) "
                                    , true);
#else
                ExecSQL(conn_web,
                    " Select unique nzp_kvar From " + tXX_spls +
                    " Where nzp_kvar not in ( Select nzp_kvar From " + tXX_spall + " ) " +
                    " Into temp ttt1_sld With no log "
                    , true);
#endif

                ExecSQL(conn_web,
                    " Insert into " + tXX_spall + " (nzp_kvar) Select nzp_kvar From ttt1_sld "
                    , true);

                ExecSQL(conn_web, " Drop table ttt1_sld ", false);

#if PG
                ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
                ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif

            }


            //пример выборки
            /*
            s_sel = " Select a.* From t1_saldoall a, t_spls b Where a.nzp_kvar = b.nzp_kvar Order by b.num_ls";
            string s_order = "";
            if (isTableHasColumn(conn_web, "t1_saldoall", "ordering"))
                s_order = " ordering ";
            if (isTableHasColumn(conn_web, "t1_saldoall", "name_supp"))
                s_order += ", name_supp ";
            s_sel += s_order;
            */

            return true;
        }
        //----------------обертка для FindSaldoAll-------------------------
        public bool FindSaldoAll(string conn_web, string conn_db, List<int> listint, int nzp_user, int yy, int mm, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web_ = GetConnection(conn_web);//new IDbConnection(conn_web);
            IDbConnection conn_db_ = GetConnection(conn_db);//new IDbConnection(conn_db);

            ret = OpenDb(conn_web_, true);
            if (ret.result)
            {
                ret = OpenDb(conn_db_, true);
            }
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return false;
            }

            return this.FindSaldoAll(conn_web_, conn_db_, listint, nzp_user, yy, mm, true, out ret);
        }


        //        //----------------------------------------------------------------------
        //        private bool FindPrmAll(IDbConnection conn_web, IDbConnection conn_db, List<Prm> listprm, int nzp_user, out Returns ret)
        //        //----------------------------------------------------------------------
        //        {
        //#if PG
        //            string tXX_spls_full ="public"+ ".t" + nzp_user + "_spls";
        //#else
        //            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + nzp_user + "_spls";
        //#endif

        //            ret = Utils.InitReturns();

        //            int l_prm_xx = 30;  //максимальное число prm_xx
        //            int l_nzp_prm = 100; //максимальное кол-во nzp_prm

        //            bool[] ar = new bool[l_prm_xx];

        //            ExecSQL(conn_db, " Drop table ttt_prmall ", false);

        //            if (!(listprm != null && listprm.Count > 0))
        //            {
        //                return false;
        //            }

        //            string p_1 = "";

        //            foreach (Prm prm in listprm)
        //            {
        //                ar[prm.prm_num] = true;
        //                p_1 += "," + prm.nzp_prm;
        //            }

        //            string sqlp = "";
        //            bool b_first = true;

        //            //-------------------- цикл по pref ------------------------
        //            IDataReader reader;
        //#if PG
        //            if (!ExecRead(conn_db, out reader, " Select distinct pref From " + tXX_spls_full, true).result)
        //#else
        //            if (!ExecRead(conn_db, out reader, " Select unique pref From " + tXX_spls_full, true).result)
        //#endif

        //            {
        //                return false;
        //            }
        //#if PG
        //            string into_temp = " INTO    UNLOGGED  ttt_prmall";
        //#else

        //#endif
        //            try
        //            {
        //                while (reader.Read())
        //                {
        //                    string pref = (string)(reader["pref"]);
        //                    pref = pref.Trim();

        //                    sqlp = "";

        //                    for (int i = 1; i <= l_prm_xx - 1; i++)
        //                    {
        //                        if (!ar[i]) continue;

        //                        if (sqlp != "")
        //                        {
        //                            sqlp += " Union ";
        //#if PG
        //                            into_temp = "";
        //#else

        //#endif
        //                        }

        //#if PG                        
        //                        sqlp +=
        //                                                " Select distinct '" + pref + "' as pref, t.nzp_kvar, p.nzp_prm, p.val_prm, n.nzp_res, 0 as nzp_yy " + into_temp+
        //                                                " From " + tXX_spls_full + " t, " + pref + "_data.prm_" + i + " p, " + pref + "_kernel.prm_name n " +
        //                                                " Where p.nzp_prm = n.nzp_prm " +
        //                                                "   and p.nzp_prm in (-1" + p_1 + ")" +
        //                                                "   and p.is_actual <> 100 " +
        //                                                "   and p.dat_s <= current_date " +
        //                                                "   and p.dat_po>= current_date ";
        //#else
        //                        sqlp +=
        //                                                " Select unique '" + pref + "' as pref, t.nzp_kvar, p.nzp_prm, p.val_prm, n.nzp_res, 0 as nzp_yy " +
        //                                                " From " + tXX_spls_full + " t, " + pref + "_data:prm_" + i + " p, " + pref + "_kernel:prm_name n " +
        //                                                " Where p.nzp_prm = n.nzp_prm " +
        //                                                "   and p.nzp_prm in (-1" + p_1 + ")" +
        //                                                "   and p.is_actual <> 100 " +
        //                                                "   and p.dat_s <= today " +
        //                                                "   and p.dat_po>= today ";
        //#endif



        //                        if (i == 1 || i == 3 || i == 15)
        //                            sqlp += " and t.nzp_kvar = p.nzp ";
        //                        else
        //                            sqlp += " and t.nzp_dom = p.nzp ";
        //                    }

        //                    if (b_first)
        //                    {
        //#if PG

        //#else
        //                        sqlp += " Into temp ttt_prmall With no log ";
        //#endif

        //                        b_first = false;

        //                        ret = ExecSQL(conn_db, sqlp, true);
        //                        if (!ret.result)
        //                        {
        //                            return false;
        //                        }

        //                        ret = ExecSQL(conn_db, " Create index ix1_ttt_prmall on ttt_prmall (nzp_kvar,nzp_prm,nzp_res) ", true);
        //                        if (!ret.result)
        //                        {
        //                            return false;
        //                        }
        //                        ret = ExecSQL(conn_db, " Create index ix2_ttt_prmall on ttt_prmall (pref,nzp_res) ", true);
        //                        if (!ret.result)
        //                        {
        //                            return false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        sqlp = " Insert into ttt_prmall (pref,nzp_kvar,nzp_prm,val_prm,nzp_res,nzp_yy) Select * from ( " + sqlp + " )";

        //                        ret = ExecSQL(conn_db, sqlp, true);
        //                        if (!ret.result)
        //                        {
        //                            return false;
        //                        }
        //                    }

        //#if PG
        //                    ExecSQL(conn_db, " analyze ttt_prmall ", true);
        //#else
        //                    ExecSQL(conn_db, " Update statistics for table ttt_prmall ", true);
        //#endif


        //                    //заменить справочные значения
        //#if PG
        //                    ret = ExecSQL(conn_db,
        //                                     " Update ttt_prmall " +
        //                                     " SET nzp_yy = CAST(val_prm as INTEGER) +0 " +
        //                                     " Where pref = '" + pref + "'" +
        //                                       " and nzp_res is not null "
        //                                     , true);
        //#else
        //                    ret = ExecSQL(conn_db,
        //                                     " Update ttt_prmall " +
        //                                     " Set nzp_yy = val_prm+0 " +
        //                                     " Where pref = '" + pref + "'" +
        //                                       " and nzp_res is not null "
        //                                     , true);
        //#endif
        //                    if (!ret.result)
        //                    {
        //                        return false;
        //                    }
        //#if PG
        //                    ret = ExecSQL(conn_db,
        //                        " Update ttt_prmall " +
        //                        " Set val_prm = ( Select max(name_y) From " + pref + "_kernel.res_y ry " +
        //                                        " Where ttt_prmall.nzp_res = ry.nzp_res and nzp_yy = ry.nzp_y )::char(20) " +
        //                        " Where pref = '" + pref + "'" +
        //                        "   and nzp_res is not null " +
        //                        "   and nzp_res in ( Select nzp_res From " + pref + "_kernel.res_y  ) "
        //                        , true);
        //#else
        //                    ret = ExecSQL(conn_db,
        //                        " Update ttt_prmall " +
        //                        " Set val_prm = ( Select max(name_y) From " + pref + "_kernel:res_y ry " +
        //                                        " Where ttt_prmall.nzp_res = ry.nzp_res and nzp_yy = ry.nzp_y ) " +
        //                        " Where pref = '" + pref + "'" +
        //                        "   and nzp_res is not null " +
        //                        "   and nzp_res in ( Select nzp_res From " + pref + "_kernel:res_y  ) "
        //                        , true);
        //#endif

        //                    if (!ret.result)
        //                    {
        //                        return false;
        //                    }


        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                reader.Close();

        //                ret.result = false;
        //                ret.text = ex.Message;

        //                string err;
        //                if (Constants.Viewerror)
        //                    err = " \n " + ex.Message;
        //                else
        //                    err = "";

        //                MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

        //                return false;
        //            }





        //            //построить таблицу по выбранным параметрам!
        //            string tXX_spall = "t" + nzp_user + "_prmall ";
        //#if PG
        //            string tXX_spall_full = "public." + tXX_spall;
        //#else
        //            string tXX_spall_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spall;
        //#endif

        //            ExecSQL(conn_web, " Drop table " + tXX_spall, false);

        //            sqlp = " Create table " + tXX_spall +
        //                   " ( nzp_kvar integer ";

        //            int l_cur = 0;
        //            int[] ar_prm = new int[l_nzp_prm];



        //            //IDataReader reader;
        //#if PG
        //            if (!ExecRead(conn_db, out reader, " Select distinct nzp_prm From ttt_prmall ", true).result)
        //#else
        //            if (!ExecRead(conn_db, out reader, " Select unique nzp_prm From ttt_prmall ", true).result)
        //#endif

        //            {
        //                return false;
        //            }
        //            try
        //            {
        //                while (reader.Read())
        //                {
        //                    l_cur += 1;
        //                    if (l_cur > l_nzp_prm) break;

        //                    int nzp_prm = (int)(reader["nzp_prm"]);
        //                    //sqlp += " ,val_" + nzp_prm + " char(40)";

        //                    ar_prm[l_cur] = nzp_prm;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                reader.Close();

        //                ret.result = false;
        //                ret.text = ex.Message;

        //                string err;
        //                if (Constants.Viewerror)
        //                    err = " \n " + ex.Message;
        //                else
        //                    err = "";

        //                MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

        //                return false;
        //            }

        //            //надо соблюсти порядок следования параметров!
        //            foreach (Prm prm in listprm)
        //            {
        //                for (int i = 1; i <= l_cur; i++)
        //                {
        //                    if (ar_prm[i] == prm.nzp_prm)
        //                        sqlp += " ,val_" + ar_prm[i] + " char(40)";
        //                }
        //            }
        //            sqlp += " ) ";


        //            //создать таблицу
        //            ret = ExecSQL(conn_web, sqlp, true);
        //            if (!ret.result)
        //            {
        //                reader.Close();
        //                return false;
        //            }

        //            //вставить лицевые счета
        //#if PG
        //            ret = ExecSQL(conn_db,
        //                " Insert into " + tXX_spall_full + " (nzp_kvar) Select distinct nzp_kvar From ttt_prmall "
        //                , true);
        //#else
        //            ret = ExecSQL(conn_db,
        //                " Insert into " + tXX_spall_full + " (nzp_kvar) Select unique nzp_kvar From ttt_prmall "
        //                , true);
        //#endif

        //            if (!ret.result)
        //            {
        //                return false;
        //            }

        //            ret = ExecSQL(conn_web, " Create index ix_" + tXX_spall + " on " + tXX_spall + " (nzp_kvar) ", true);
        //            if (!ret.result)
        //            {
        //                return false;
        //            }
        //#if PG
        //            ExecSQL(conn_web, " analyze " + tXX_spall, true);
        //#else
        //            ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
        //#endif




        //            //выставить значения в строку
        //            for (int i = 1; i <= l_cur; i++)
        //            {
        //                ret = ExecSQL(conn_db,
        //                    " Update " + tXX_spall_full +
        //                    " Set val_" + ar_prm[i] + " = ( " +
        //                                " Select max(val_prm) From ttt_prmall p " +
        //                                " Where p.nzp_kvar = " + tXX_spall_full + ".nzp_kvar " +
        //                                "   and p.nzp_prm = " + ar_prm[i] + " ) " +
        //                    " Where 0 < ( Select count(*) From ttt_prmall p " +
        //                                " Where p.nzp_kvar = " + tXX_spall_full + ".nzp_kvar " +
        //                                "   and p.nzp_prm = " + ar_prm[i] + " ) "
        //                    , true);
        //                if (!ret.result)
        //                {
        //                    return false;
        //                }
        //            }

        //            ExecSQL(conn_db, " Drop table ttt_prmall ", false);


        //            return true;
        //        }


        //----------------------------------------------------------------------
        private bool FindPrmAll(IDbConnection conn_web, IDbConnection conn_db, List<Prm> listprm, int nzp_user, out Returns ret)
        //----------------------------------------------------------------------
        {
            string tXX_spls_full = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + "t" + nzp_user + "_spls";

            ret = Utils.InitReturns();

            int l_prm_xx = 30;  //максимальное число prm_xx
            int l_nzp_prm = 100; //максимальное кол-во nzp_prm

            bool[] ar = new bool[l_prm_xx];

            ExecSQL(conn_db, " Drop table ttt_prmall ", false);

            if (!(listprm != null && listprm.Count > 0))
            {
                return false;
            }


            string sqlp = " Create temp table ttt_prmall( " +
                         " pref char(10)," +
                         " nzp_kvar integer," +
                         " nzp_prm integer," +
                         " val_prm char(100)," +
                         " nzp_res integer," +
                         " nzp_yy integer) " +
                         DBManager.sUnlogTempTable;
            ret = ExecSQL(conn_db, sqlp, true);
            if (!ret.result)
            {
                return false;
            }

            string p_1 = "";

            foreach (Prm prm in listprm)
            {
                ar[prm.prm_num] = true;
                p_1 += "," + prm.nzp_prm;
            }


            bool b_first = true;

            //-------------------- цикл по pref ------------------------
            MyDataReader reader;
            if (!ExecRead(conn_db, out reader, " Select distinct pref From " + tXX_spls_full, true).result)
            {
                return false;
            }

            try
            {
                while (reader.Read())
                {
                    string pref = (string)(reader["pref"]);
                    pref = pref.Trim();

                    for (int i = 1; i <= l_prm_xx - 1; i++)
                    {
                        if (!ar[i]) continue;

                        sqlp = "insert into ttt_prmall (pref,nzp_kvar,nzp_prm,val_prm,nzp_res,nzp_yy)" +
                               " Select distinct '" + pref +
                               "' as pref, t.nzp_kvar, p.nzp_prm, p.val_prm, n.nzp_res, 0 as nzp_yy " +
                               " From " + tXX_spls_full + " t, " +
                               pref + DBManager.sDataAliasRest + "prm_" + i + " p, " +
                               pref + DBManager.sKernelAliasRest + " prm_name n " +
                               " Where p.nzp_prm = n.nzp_prm " +
                               "   and p.nzp_prm in (-1" + p_1 + ")" +
                               "   and p.is_actual <> 100 " +
                               "   and p.dat_s <=  " + DBManager.sCurDate +
                               "   and p.dat_po>=  " + DBManager.sCurDate;



                        if (i == 1 || i == 3 || i == 15)
                            sqlp += " and t.nzp_kvar = p.nzp ";
                        else
                            sqlp += " and t.nzp_dom = p.nzp ";

                        ret = ExecSQL(conn_db, sqlp, true);
                        if (!ret.result)
                        {
                            return false;
                        }


                    }

                    if (b_first)
                    {
                        ret = ExecSQL(conn_db, " Create index ix1_ttt_prmall on ttt_prmall (nzp_kvar,nzp_prm,nzp_res) ",
                            true);
                        if (!ret.result)
                        {
                            return false;
                        }
                        ret = ExecSQL(conn_db, " Create index ix2_ttt_prmall on ttt_prmall (pref,nzp_res) ", true);
                        if (!ret.result)
                        {
                            return false;
                        }
                        b_first = false;
                    }



                    ExecSQL(conn_db, DBManager.sUpdStat + " ttt_prmall ", true);


                    //заменить справочные значения
                    ret = ExecSQL(conn_db,
                                     " Update ttt_prmall " +
                                     " Set nzp_yy = val_prm" + DBManager.sConvToInt +
                                     " Where pref = '" + pref + "'" +
                                       " and nzp_res is not null "
                                     , true);
                    if (!ret.result)
                    {
                        return false;
                    }
                    DataTable dt = DBManager.ExecSQLToTable(conn_db, "select * from ttt_prmall");

                    ret = ExecSQL(conn_db,
                        " Update ttt_prmall " +
                        " Set val_prm = ( Select SUBSTRING(max(name_y), 1, 100) From " + pref + DBManager.sKernelAliasRest + "res_y ry " +
                                        " Where ttt_prmall.nzp_res = ry.nzp_res and nzp_yy = ry.nzp_y )" +
                        " Where pref = '" + pref + "'" +
                        "   and nzp_res is not null " +
                        "   and nzp_res in ( Select nzp_res From " + pref + DBManager.sKernelAliasRest + "res_y  ) "
                        , true);


                    if (!ret.result)
                    {
                        return false;
                    }

                    dt = DBManager.ExecSQLToTable(conn_db, "select * from ttt_prmall");
                }
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

                return false;
            }





            //построить таблицу по выбранным параметрам!
            string tXX_spall = "t" + nzp_user + "_prmall ";
            string tXX_spall_full = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + tXX_spall;

            ExecSQL(conn_web, " Drop table " + tXX_spall, false);

            sqlp = " Create table " + tXX_spall +
                   " ( nzp_kvar integer ";

            int l_cur = 0;
            int[] ar_prm = new int[l_nzp_prm];


            if (!ExecRead(conn_db, out reader, " Select distinct nzp_prm From ttt_prmall ", true).result)
            {
                return false;
            }
            try
            {
                while (reader.Read())
                {
                    l_cur += 1;
                    if (l_cur > l_nzp_prm) break;

                    int nzp_prm = (int)(reader["nzp_prm"]);
                    //sqlp += " ,val_" + nzp_prm + " char(40)";

                    ar_prm[l_cur] = nzp_prm;
                }
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

                return false;
            }

            //надо соблюсти порядок следования параметров!
            foreach (Prm prm in listprm)
            {
                for (int i = 1; i <= l_cur; i++)
                {
                    if (ar_prm[i] == prm.nzp_prm)
                        sqlp += " ,val_" + ar_prm[i] + " char(100)";
                }
            }
            sqlp += " ) ";


            //создать таблицу
            ret = ExecSQL(conn_web, sqlp, true);
            if (!ret.result)
            {
                reader.Close();
                return false;
            }

            //вставить лицевые счета
            ret = ExecSQL(conn_db,
                " Insert into " + tXX_spall_full + " (nzp_kvar) Select distinct nzp_kvar From ttt_prmall "
                , true);

            if (!ret.result)
            {
                return false;
            }

            ret = ExecSQL(conn_web, " Create index ix_" + tXX_spall + " on " + tXX_spall + " (nzp_kvar) ", true);
            if (!ret.result)
            {
                return false;
            }
            ExecSQL(conn_web, DBManager.sUpdStat + " " + tXX_spall, true);




            //выставить значения в строку
            for (int i = 1; i <= l_cur; i++)
            {
                ret = ExecSQL(conn_db,
                    " Update " + tXX_spall_full +
                    " Set val_" + ar_prm[i] + " = ( " +
                                " Select max(val_prm) From ttt_prmall p " +
                                " Where p.nzp_kvar = " + tXX_spall_full + ".nzp_kvar " +
                                "   and p.nzp_prm = " + ar_prm[i] + " ) " +
                    " Where 0 < ( Select count(*) From ttt_prmall p " +
                                " Where p.nzp_kvar = " + tXX_spall_full + ".nzp_kvar " +
                                "   and p.nzp_prm = " + ar_prm[i] + " ) "
                    , true);
                if (!ret.result)
                {
                    return false;
                }
            }

            ExecSQL(conn_db, " Drop table ttt_prmall ", false);


            return true;
        }


        //----------------------------------------------------------------------
        private bool FindPrmOverLs(IDbConnection conn_db, Ls finder, out Returns ret, out bool fls, out bool fdom)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            fls = false;
            fdom = false;

            ExecSQL(conn_db, " Drop table ttt_prmls ", false);

            //if (finder.num_ls > 0 || !string.IsNullOrEmpty(finder.pkod))
            //    return false;

            if (finder.dopParams != null && finder.dopParams.Count > 0)
            {
                string sqlp = "";
                string prm_n;

                foreach (Prm prm in finder.dopParams)
                {
                    string val_prm = prm.val_prm;
#if PG
                    prm_n = finder.pref + "_data.prm_" + prm.prm_num;
#else
                    prm_n = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_" + prm.prm_num;
#endif


                    if (!TempTableInWebCashe(conn_db, null, prm_n)) continue;
                    if (!fls) fls = (prm.prm_num == 1 || prm.prm_num == 3);
                    if (!fdom) fdom = (prm.prm_num == 2 || prm.prm_num == 4);

                    if (sqlp != "") sqlp += " Union ";

                    string date_where = "";

                    if (!string.IsNullOrEmpty(prm.dat_po))
                    {
                        date_where += " and dat_s <= '" + prm.dat_po + "'";
                    }
                    if (!string.IsNullOrEmpty(prm.dat_s))
                    {
                        date_where += " and dat_po >= '" + prm.dat_s + "'";
                    }

                    if (!string.IsNullOrEmpty(prm.dat_when_po))
                    {
                        date_where += " and dat_when <= '" + prm.dat_when_po + "'";
                    }

                    if (!string.IsNullOrEmpty(prm.dat_when))
                    {
                        if (string.IsNullOrEmpty(prm.dat_when_po))
                            date_where += " and dat_when = '" + prm.dat_when + "'";
                        else
                            date_where += " and dat_when >= '" + prm.dat_when + "'";
                    }

#if PG
                    string val = "coalesce(val_prm,'0')";
#else
                    string val = "nvl(val_prm,'0')";
#endif

                    string ss = "'";

                    if (prm.type_prm == "date")
                    {
#if PG
                        val = " cast (coalesce(val_prm,to_date('1,1,1991', 'MM,DD,YYYY')) as date) ";
#else
                        val = " cast (nvl(val_prm,mdy(1,1,1991)) as date) ";
#endif

                    }
                    if (prm.type_prm == "int")
                    {
#if PG
                        val = "coalesce(val_prm,'0')" + sConvToInt;
#else
                        val = "nvl(val_prm,'0')+0";
#endif

                        ss = "";
                    }
                    if (prm.type_prm == "float")
                    {
#if PG
                        val = "coalesce(val_prm,'0')::numeric(14,8) ";
#else
                        val = "nvl(val_prm,'0')+0.00";

#endif
                        ss = "";


                    }

                    if (prm.type_prm == "bool")
                    {
                        val_prm = prm.val_prm == "Да" || prm.val_prm == "1" ? "1" : "0";
                    }


                    //Ошибка Эли!!
                    if ((prm.type_prm == "sprav") && (prm.val_prm == "-1"))
                    {
                        val_prm = "";
                    }

                    if (prm.criteria == enCriteria.missing) // если поиск по отсутствию параметра
                    {
                        sqlp += " select " + prm.prm_num + " as tab, nzp_kvar as nzp  From " + finder.pref + "_data" + tableDelimiter + "kvar where nzp_kvar not in " +
                            " (select distinct nzp from " + finder.pref + "_data" + tableDelimiter + "prm_" + prm.prm_num +
                            " Where is_actual <> 100 " + date_where + " group by nzp, nzp_prm having nzp_prm = " + prm.nzp_prm + " ) ";
                    }
                    else
                    {
                        sqlp += " Select " + prm.prm_num + " as tab, nzp  From " + finder.pref + "_data" + tableDelimiter + "prm_" + prm.prm_num +
                            " Where is_actual <> 100 and nzp_prm = " + prm.nzp_prm + date_where;

                        if (!string.IsNullOrEmpty(val_prm) && !string.IsNullOrEmpty(prm.val_prm_po))
                        {
                            string prm_val_prm = ss + val_prm + ss;
                            string prm_val_prm_po = ss + prm.val_prm_po + ss;
#if PG
                            string s1 = val + " >= '" + prm_val_prm + "' and " + val + " <= '" + prm_val_prm_po + "'";
#else
                            string s1 = val + " >= " + prm_val_prm + " and " + val + " <= " + prm_val_prm_po;
#endif
                            if (prm.criteria == enCriteria.not_equal)
                                s1 = " not (" + s1 + ")";

                            sqlp += " and " + s1;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(val_prm))
                            {
                                string prm_val_prm = ss + val_prm + ss;

                                string s1 = " = ";

                                if (prm.criteria == enCriteria.not_equal)
                                    s1 = " <> ";

                                sqlp += " and " + val + s1 + prm_val_prm;
                            }
                        }
                    }
                }

                if (sqlp != "")
                {
#if PG
                    sqlp = sqlp.AddIntoStatement("Into temp ttt_prmls");
#else
                    sqlp += " Into temp ttt_prmls With no log ";
#endif


                    ret = ExecSQL(conn_db, sqlp, true);
                    if (!ret.result)
                    {
                        return false;
                    }

                    ret = ExecSQL(conn_db, " Create index ix1_ttt_prmls on ttt_prmls (tab,nzp) ", true);
                    if (!ret.result)
                    {
                        return false;
                    }
                    ret = ExecSQL(conn_db, " Create index ix2_ttt_prmls on ttt_prmls (nzp,tab) ", true);
                    if (!ret.result)
                    {
                        return false;
                    }

#if PG
                    ExecSQL(conn_db, " analyze ttt_prmls ", true);
#else
                    ExecSQL(conn_db, " Update statistics for table ttt_prmls ", true);
#endif

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }


        //----------------------------------------------------------------------
        public void FindLs(Ls finder, out Returns ret) //найти и заполнить список адресов
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (!Points.IsFabric || (finder.nzp_wp > 0))
            {
                //поиск в конкретном банке
                FindLs00(finder, out ret, 0);
            }
            else
            {
                //параллельный поиск по серверам БД
                FindLsInThreads(finder, out ret);
            }
        }
        //----------------------------------------------------------------------
        private void FindLsInThreads(Ls finder, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta";

            //создать таблицу контроля
            if (TableInWebCashe(conn_web, tXX_meta))
            {
                ExecSQL(conn_web, " Drop table " + tXX_meta, false);
            }

#if PG
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_meta +
                      " ( nzp_server integer," +
                      "   dat_in     timestamp, " +
                      "   dat_work   timestamp, " +
                      "   dat_out    timestamp, " +
                      "   kod        integer default 0 " +
                      " ) ", true);
#else
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_meta +
                      " ( nzp_server integer," +
                      "   dat_in     datetime year to minute, " +
                      "   dat_work   datetime year to minute, " +
                      "   dat_out    datetime year to minute, " +
                      "   kod        integer default 0 " +
                      " ) ", true);
#endif

            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
#if PG
#else
            ret = ExecSQL(conn_web, " Alter table " + tXX_meta + "  lock mode (row) ", true);
#endif

            //открываем цикл по серверам БД
            foreach (_Server server in Points.Servers)
            {
                int nzp_server = server.nzp_server;
                System.Threading.Thread thServer =
                                new System.Threading.Thread(delegate() { FindLs01(finder, nzp_server); });
                thServer.Start();
                //MyThread1.Join();
            }

            //а пока создаим кэш-таблицы лицевых счетов и домов, чтобы время не терять пока потоки делают свою работу
            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
            string tXX_spdom = "t" + Convert.ToString(finder.nzp_user) + "_spdom";

            using (DbAdresClient db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, true);
            }
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            CreateTableWebDom(conn_web, tXX_spdom, true, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            Thread.Sleep(1000); //подождем, чтобы процессы стартовали

            //дождаться и соединить результаты
            while (true)
            {
                IDataReader reader;
                string sql = " Select * From " + tXX_meta + " Where kod in (-1,0,1) ";

                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }

                bool b = true;
                try
                {
                    while (reader.Read())
                    {
                        int kod = (int)reader["kod"];
                        int nzp_server = (int)reader["nzp_server"];

                        if (kod == 1) //банк готов!
                        {
                            //заполняем буфер в кеше
                            string tXX_spls_local = "t" + Convert.ToString(finder.nzp_user) + "_spls" + nzp_server;
                            string tXX_spdom_local = "t" + Convert.ToString(finder.nzp_user) + "_spdom" + nzp_server;

                            ret = ExecSQL(conn_web, " Insert into " + sDefaultSchema + tXX_spls +
                                " Select * From " + sDefaultSchema + tXX_spls_local, true);
                            if (!ret.result)
                            {
                                reader.Close();
                                conn_web.Close();
                                return;
                            }
                            ret = ExecSQL(conn_web, " Insert into " + sDefaultSchema + tXX_spdom +
                                " Select * From " + sDefaultSchema + tXX_spdom_local, true);
                            if (!ret.result)
                            {
                                reader.Close();
                                conn_web.Close();
                                return;
                            }

                            //признак, что буфер заполнен
                            ret = ExecSQL(conn_web,
                                " Update " + tXX_meta +
                                " Set kod = 2, dat_out = current  " +
                                " Where nzp_server = " + nzp_server, true);
                            if (!ret.result)
                            {
                                reader.Close();
                                conn_web.Close();
                                return;
                            }

                            //reader.Close();
                            b = false;
                            break;

                        }
                        if (kod == -1) //ошибка выполнения!!
                        {
                            reader.Close();
                            conn_web.Close();
                            ret.result = false;
                            return;
                        }
                        if (kod == 0) //еще есть невыполненные задания
                        {
                            b = false;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    conn_web.Close();

                    ret.result = false;
                    ret.text = ex.Message;

                    MonitorLog.WriteLog("Ошибка контроля выполнения " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }
                reader.Close();

                if (b)
                {
                    //все потоки выполнились, выходим 
                    break;
                }
                Thread.Sleep(1000); //продолжаем ждать
            }




            //построим индексы
            using (DbAdresClient db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, false);
            }
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            CreateTableWebDom(conn_web, tXX_spdom, false, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            conn_web.Close();
        }
        //----------------------------------------------------------------------
        private void FindLs01(Ls finder, int nzp_server) //вызов из потока
        //----------------------------------------------------------------------
        {
            Returns ret = new Returns();
            FindLs00(finder, out ret, nzp_server);

            if (!ret.result)
            {
                //вылетел по ошибке - надо собщить контролю
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return;

                string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta";

                //создать таблицу контроля
                ExecSQL(conn_web,
                    " Update " + tXX_meta +
                    " Set kod = -1, dat_work = current " +
                    " Where nzp_server = " + nzp_server, true);

                conn_web.Close();
            }
        }

        private void AddToUserProc(Finder finder, string table_name, string procId, IDbConnection conn_web)
        {
#if PG
            ExecSQL(conn_web,
                    " Delete from " + pgDefaultDb + "." + "user_processes where table_name = '" + table_name + "'", true);
            ExecSQL(conn_web, "insert into " + pgDefaultDb + "." + "user_processes (nzp_user, table_name, procId) " +
                "values (" + finder.nzp_user + ",'" + table_name + "', '" + procId + "')", true);
#else
            ExecSQL(conn_web,
                    " Delete from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes where table_name = '" + table_name + "'", true);
            ExecSQL(conn_web, "insert into " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes (nzp_user, table_name, procId) " +
                "values (" + finder.nzp_user + ",'" + table_name + "', '" + procId + "')", true);
#endif

        }

        private string GetUserProcId(string table_name, IDbConnection conn_web, out Returns ret)
        {
#if PG
            string sql = "select procId from " + pgDefaultDb + "." + "user_processes " +
                " where table_name = '" + table_name + "'";
#else
            string sql = "select procId from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes " +
                " where table_name = '" + table_name + "'";
#endif

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return "";
            string prId = "";
            try
            {
                if (reader.Read())
                    if (reader["procId"] != DBNull.Value)
                        prId = ((string)reader["procId"]).Trim();
                reader.Close();
                reader.Dispose();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetUserProcId\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return prId;
        }

        private void FindLs00(Ls finder, out Returns ret, int nzp_server) //найти и заполнить список адресов
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Проверка finder

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            string conn_kernel = Points.GetConnKernel(finder.nzp_wp, nzp_server);
            if (conn_kernel == "")
            {
                ret.result = false;
                ret.text = "Не определен connect к БД";
                return;
            }

            #endregion

            #region соединение conn_web

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            #endregion

            #region проверка существования таблицы user_processes в БД

#if PG
            if (!TempTableInWebCashe(conn_web, pgDefaultDb + "." + "user_processes"))
#else
            if (!TempTableInWebCashe(conn_web, conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes"))
#endif

            {
                ret.result = false;
                ret.text = "Нет таблицы user_processes";
                conn_web.Close();
                MonitorLog.WriteLog("Ошибка FindLs00: Нет таблицы user_processes ", MonitorLog.typelog.Error, 20, 201, true);
                return;
            }

            #endregion

            #region сообщить контролю, что процесс стартовал

            string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta";
            if (nzp_server > 0)
                ExecSQL(conn_web,
                    " Insert into " + tXX_meta + "(nzp_server, dat_in) " +
                    " Values (" + nzp_server + ", current )", true);

            #endregion

            string procId = Guid.NewGuid().ToString();

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

            #region обновить данные в таблице user_processes

            AddToUserProc((Finder)finder, tXX_spls, procId, conn_web);

            #endregion

            #region сохранение finder

            SaveFinder(finder, Constants.page_spisls);

            #endregion

            #region соединение conn_db

            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            #endregion

            DbTables tables = new DbTables(conn_db);

            decimal d = 0;
            Decimal.TryParse(finder.pkod.Trim(), out d);

            #region заполнение whereString

            //в случае true- разрешает применять шаблоны поиска, т.е. при указании пользователем, одного из параметров 
            // ( ЛС, Платежгого кода, номера дома или nzp_kvar) игнорируются все шаблоны поиска
            bool isEnableFindByPattern = false;
            List<int> listPoint = new List<int>();
            string wherePointList = ""; // условие для банков
            List<int> listGeu = new List<int>();
            string whereGeuList = ""; // условие для ЖЭУ
            List<int> listArea = new List<int>();
            string whereAreaList = "";// условие для УК
            string cur_pref;

            string whereString = " and d.nzp_dom = -1111 "; //чтобы ничего не выбиралось

            // Указан ЛС
            if (finder.num_ls > Constants._ZERO_)
            {
                whereString = " and k.num_ls = " + finder.num_ls;
            }
            // Указан платежный код
            else if (finder.pkod != "" && d != 0)
            {
                if (!GlobalSettings.NewGeneratePkodMode)
                {
                    whereString = " and k.pkod = " + finder.pkod;
                }
            }
            else if (finder.pkod10 > 0 && !Points.IsSmr)
            {
                whereString = " and k.pkod10 = " + finder.pkod10;
            }
            else
            {
                isEnableFindByPattern = true; // разрешить применение шаблонов поиска
                StringBuilder swhere = new StringBuilder();
                int i;
                // Для нулевого платежного кода  разрешается применять дополнительные фильтры
                if (finder.pkod != "" && d == 0)
                {
                    swhere.Append(" and  " + sNvlWord + "(k.pkod,0) = " + finder.pkod);
                }
                if (finder.pkod10 > 0 && Points.IsSmr) swhere.Append(" and k.pkod10 = " + finder.pkod10);

                if (finder.typek > 0) swhere.Append(" and k.typek = " + finder.typek.ToString());

                if (finder.uch.Trim() != "") swhere.Append(" and k.uch = " + Convert.ToInt32(finder.uch));
                // Формирование условий для УК
                if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
                {
                    listArea.AddRange(finder.list_nzp_area);
                    whereAreaList = " and k.nzp_area in (" + String.Join(",", finder.list_nzp_area) + ")";
                }
                else if (finder.nzp_area > 0)
                {
                    whereAreaList = " and k.nzp_area = " + finder.nzp_area;
                    listArea.Add(finder.nzp_area);
                }
                // формирование уловий для ЖЭУ
                if (finder.nzp_geu > 0)
                {
                    whereGeuList = " and k.nzp_geu = " + finder.nzp_geu;
                    listGeu.Add(finder.nzp_geu);
                }
                if (finder.nzp_town > 0) swhere.Append(" and t.nzp_town = " + finder.nzp_town);
                if (finder.nzp_raj > 0) swhere.Append(" and r.nzp_raj = " + finder.nzp_raj);
                if (finder.nzp_ul > 0) swhere.Append(" and u.nzp_ul = " + finder.nzp_ul);
                //  Указан дом
                if (finder.nzp_dom > 0)
                {
                    swhere.Append(" and k.nzp_dom = " + finder.nzp_dom);
                }
                else
                {
                    if (finder.ndom_po != "")
                    {
                        i = Utils.GetInt(finder.ndom_po);
                        if (i > 0) swhere.Append(" and d.idom <= " + i);

                        i = Utils.GetInt(finder.ndom);
                        if (i > 0) swhere.Append(" and d.idom >= " + i);
                    }
                    else if (finder.ndom != "") swhere.Append(" and upper(d.ndom) = " + Utils.EStrNull(finder.ndom.ToUpper()));
                }
                //корпус
                if (finder.nkor != "") swhere.Append(" and upper(d.nkor) = " + Utils.EStrNull(finder.nkor.ToUpper()));

                // Указана квартира
                if (finder.nzp_kvar > 0)
                {
                    swhere.Append(" and k.nzp_kvar = " + finder.nzp_kvar);
                }
                else
                {
#if PG
                    if (finder.stateID > 0) swhere.Append(" and cast(k.is_open as integer) = " + finder.stateID);
#else
                if (finder.stateID > 0) swhere.Append(" and k.is_open = " + finder.stateID);

#endif

                    else if (finder.stateIDs != null && finder.stateIDs.Count > 0)
                    {
#if PG
                        swhere.Append(" and cast(k.is_open as integer) in (" + String.Join(",", finder.stateIDs) + ")");
#else
                     swhere.Append( " and k.is_open in (" + String.Join(",", finder.stateIDs) + ")"); 

#endif
                    }
                    else swhere.Append(" and k.is_open in ('" + Ls.States.Open.GetHashCode() + "','" + Ls.States.Closed.GetHashCode() + "')");

                    if (finder.nkvar_po != "")
                    {
                        i = Utils.GetInt(finder.nkvar_po);
                        if (i > 0) swhere.Append(" and k.ikvar <= " + i);

                        i = Utils.GetInt(finder.nkvar);
                        if (i > 0) swhere.Append(" and k.ikvar >= " + i);
                    }
                    else if (finder.nkvar != "") swhere.Append(" and k.nkvar = " + Utils.EStrNull(finder.nkvar));
                }
                if (finder.fio != "") swhere.Append(" and upper(k.fio) like '%" + finder.fio.ToUpper() + "%'");

                if (finder.nkvar_n != "") swhere.Append(" and upper(k.nkvar_n)= '" + finder.nkvar_n.ToUpper().Trim() + "'");

                if (finder.phone != "") swhere.Append(" and upper(k.phone) like '%" + finder.phone.ToUpper() + "%'");

                if (finder.porch != "") swhere.Append(" and k.porch=" + finder.porch.Trim());

                if (!String.IsNullOrWhiteSpace(finder.remark)) swhere.Append(" and lower(k.remark) like lower('%" + finder.remark.Trim() + "%')");

                whereString = swhere.ToString();

                // Формирование условий для банков
                // Выбран  один банк
                if (finder.nzp_wp > 0)
                {
                    listPoint.Add(finder.nzp_wp);
                    wherePointList = " and k.nzp_wp=" + finder.nzp_wp;
                }
                // задано несколько банков
                else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
                {
                    listPoint.AddRange(finder.dopPointList);
                    wherePointList = " and k.nzp_wp in (" + String.Join(",", finder.dopPointList) + ")";
                }

                
            }
            // ограничения по ролям
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_area) whereAreaList = getAvaliableRolesVal(role.val, listArea, "k.nzp_area"); //" and k.nzp_area in (" + role.val + ")";
                        else if (role.kod == Constants.role_sql_wp) wherePointList = getAvaliableRolesVal(role.val, listPoint, "k.nzp_wp"); // " and k.nzp_wp in (" + role.val + ")";
                        else if (role.kod == Constants.role_sql_geu) whereGeuList = getAvaliableRolesVal(role.val, listGeu, "k.nzp_geu"); //" and k.nzp_geu in (" + role.val + ")";
                    }
                }
            }
            // формирование базового условия основного запроса
            whereString += whereAreaList + whereGeuList;

            #endregion

            #region Формирование from основного запроса

            string fromSql = "";
            // таблица kvar в любом случае
            fromSql += " from " + tables.kvar + " k, ";
            if (GlobalSettings.NewGeneratePkodMode && d > 0)
            {
                //платежный код введен
                fromSql += tables.kvar_pkodes + " kp where k.nzp_kvar = kp.nzp_kvar and kp.pkod = " + d;
            }
            else
            {
                fromSql += tables.dom + " d , " + Points.Pref + sDataAliasRest + "s_ulica u , "
                           + Points.Pref + sDataAliasRest + "s_rajon r ," + Points.Pref + sDataAliasRest + "s_town t ";
                whereString += " and k.nzp_dom = d.nzp_dom and k.nzp_wp is not null and u.nzp_ul = d.nzp_ul and r.nzp_raj = u.nzp_raj and r.nzp_town=t.nzp_town";
            }

            #endregion

            #region наименование таблиц tXX_spls/tXX_spls_full/tXX_meta

            if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom)) tXX_spls += "dom";
            if (nzp_server > 0) tXX_spls += nzp_server.ToString();
#if PG
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
                string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

            #endregion

            #region создать кэш-таблицу

            using (DbAdresClient db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, true);
            }
            if (!ret.result) return;

            #endregion

         

            int key;
            // Основное тело запроса
            string sql = " Insert into " + tXX_spls_full +
                         " (nzp_kvar,remark, pref, num_ls, pkod, pkod10, nzp_dom, nzp_area, nzp_geu, typek, fio, nkvar, ikvar, nkvar_n, nzp_town, nzp_raj, " +
                         "  nzp_ul, town, rajon,  ndom, nkor, idom, sostls, stypek, ulica, adr) " +
                         " Select distinct k.nzp_kvar, k.remark, k.pref, k.num_ls, k.pkod, k.pkod10, k.nzp_dom," +
                         "  k.nzp_area, k.nzp_geu, k.typek, k.fio, " + sNvlWord + "(k.nkvar), k.ikvar, " +
                         " " + sNvlWord + "(k.nkvar_n)," +
                         " t.nzp_town, r.nzp_raj, d.nzp_ul,  " + sNvlWord + "(t.town,''), " +
                         sNvlWord + "(r.rajon,''), " + sNvlWord + "(d.ndom), " + sNvlWord + "(d.nkor), d.idom, " +
                         " case when k.is_open = '1' then 'открыт'" +
                         " when k.is_open = '2' then 'закрыт'" +
                         " when k.is_open = '3' then 'неопределено' else 'неопределено' end," +
                         " case when k.typek = 1 then 'население'" +
                         " when k.typek = 2 then 'бюджет'" +
                         " when k.typek = 3 then 'арендаторы' else 'неопределено'" +
                         " end," +
                         " trim(" + sNvlWord + "(ulicareg,''))||' '||trim(" + sNvlWord + "(ulica,''))" +
                         "   || ' / '||trim(" + sNvlWord + "(rajon,'')) as ulica, " +

                         " trim(" + sNvlWord + "(ulicareg,'улица'))||' '||trim(" + sNvlWord + "(ulica,''))" +
                         "|| ' / '||trim(" + sNvlWord + "(r.rajon,'')) || ' / '||trim(" + sNvlWord +
                         "(t.town,''))||'   дом '" +
                         "||  trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord +
                         "(nkor,''))||'  кв. '||trim(" + sNvlWord + "(nkvar,''))||'  ком. '||trim(" + sNvlWord +
                         "(nkvar_n,''))"

                         + fromSql +
                         " Where 1=1 " + whereString;
            // Если поиск по шаблонам разрешен И один из шаблонов поиска оказался не пустым
            if (isEnableFindByPattern && ((finder.dopFind != null && finder.dopFind.Count > 0) ||
                                          (finder.dopUsl != null && finder.dopUsl.Count > 0) ||
                                          (finder.dopParams != null && finder.dopParams.Count > 0)))
            {
                // извлечем банки с префиксами, которые отфильтрует уловие where
                string sqlNzpWpPref = "select " + sUniqueWord + " k.nzp_wp, k.pref " + fromSql + " Where 1=1 " + whereString + wherePointList;
                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sqlNzpWpPref, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
                try
                {
                    // для каждой извлеченной записи собираем дополнительные условия в соответствии с указанными шаблонами поиска
                    while (reader.Read())
                    {
                        cur_pref = reader["pref"] != DBNull.Value ? ((string)reader["pref"]).Trim() : "";
                        int nzp_wp = reader["nzp_wp"] != DBNull.Value ? (int)reader["nzp_wp"] : 0;
                        if (cur_pref == "") continue;
                        if (nzp_wp <= 0) continue;
                        string localWhereString = "";

                        #region добавление доп условий к главному запросу и его выполнение

                        RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(cur_pref));
                        if (finder.dopFind != null)
                        {
                            foreach (string s in finder.dopFind)
                            {
                                if (string.IsNullOrWhiteSpace(s)) continue;
                                var subQueryOperator = s.IndexOf("select 1", StringComparison.OrdinalIgnoreCase) != -1
                                    ? "EXISTS"
                                    : "0 <";
                                localWhereString += " and " + subQueryOperator + " (" + s.Replace("PREFX", cur_pref)
                                    .Replace("{ALIAS}", "k")
                                    .Replace("CNTRPRFX", Points.Pref)
                                    .Replace("CYEAR", (r_m.year_ % 100).ToString("00"))
                                    .Replace("CMONTH", r_m.month_.ToString("00")) + ")";
                            }
                        }
                        if (finder.dopUsl != null)
                        {
                            foreach (string s in finder.dopUsl)
                            {
                                if (string.IsNullOrWhiteSpace(s)) continue;
                                localWhereString += " and " + s.Replace("PREFX", cur_pref).Replace("{ALIAS}", "k").Replace("CNTRPRFX", Points.Pref).
                                    Replace("CYEAR", (r_m.year_ % 100).ToString("00")).Replace("CMONTH", r_m.month_.ToString("00")) + "";
                            }
                        }
                        Ls prmfound = new Ls();
                        finder.CopyAttributes(prmfound);
                        prmfound.pref = cur_pref;
                        bool fls;
                        bool fdom;
                        // шаблон по параметрам
                        bool findPrmOverLs = FindPrmOverLs(conn_db, prmfound, out ret, out fls, out fdom);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            conn_web.Close();
                            return;
                        }
                        if (findPrmOverLs)
                        {
                            if (fls) localWhereString += " and exists ( Select 1 From ttt_prmls p Where k.nzp_kvar=p.nzp and tab in (1,3) ) ";
                            if (fdom) localWhereString += " and exists ( Select 1 From ttt_prmls p Where k.nzp_dom=p.nzp and  tab in (2,4) ) ";
                        }
                        // основной запрос
                        string insertSql = sql + localWhereString + " and k.nzp_wp=" + nzp_wp;
                        key = LogSQL(conn_web, finder.nzp_user, " completed: " + insertSql);
                        ret = ExecSQL(conn_db, insertSql, true);
                        if (!ret.result)
                        {
                            if (key > 0) LogSQL_Error(conn_web, key, ret.text);

                            conn_db.Close();
                            conn_web.Close();
                            return;
                        }

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    conn_db.Close();
                    conn_web.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    string err;
                    if (Constants.Viewerror)
                        err = " \n " + ex.Message;
                    else
                        err = "";
                    MonitorLog.WriteLog(" FindLS00(). Ошибка поиска ЛС по дополнительным шаблонам " + err, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }
                finally
                {
                    if (reader != null) reader.Close();
                }
            }
            //  Если все шаблоны поиска оказались пустыми
            // то просто вставляем записи по условию
            else
            {
                string insertsql = sql + wherePointList;
                ret = ExecSQL(conn_db, insertsql, true);
                key = LogSQL(conn_web, finder.nzp_user, " completed: " + insertsql);
                if (!ret.result)
                {
                    if (key > 0) LogSQL_Error(conn_web, key, ret.text);
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }

            ret = Fill_tXX_spls(conn_db, conn_web, null, tXX_spls_full, nzp_server);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            #region создаем индексы на tXX_spls

            using (DbAdresClient db = new DbAdresClient())
            {
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                ret = db.CreateTableWebLs(conn_web, tXX_spls, false);
            }
            if (!ret.result) return;

            #endregion

            #region проверка равенства procId и procid из таблицы user_processes

            string actualProcId = GetUserProcId(tXX_spls, conn_web, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            if (actualProcId != "" && actualProcId != procId)
            {
                ret.result = false;
                ret.text = "Обнаружен новый запрос поиска";
                ret.tag = -1;
                conn_db.Close();
                conn_web.Close();
                return;
            }

            #endregion

            #region заполнение списка домов

            if (ret.result)
            {
                if (!Utils.GetParams(finder.prms, Constants.page_perechen_lsdom))
                {
                    //проверить наличие spdom, если нет, то создать 
                    Dom Dom = new Dom();
                    Dom.nzp_user = finder.nzp_user;
                    Dom.spls = pgDefaultDb + "." + tXX_spls;
                    Dom.nzp_wp = finder.nzp_wp;
                    FindDom00(conn_db, null, conn_web, null, Dom, out ret, nzp_server); //найти и заполнить список домов
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                }
            }

            #endregion

            #region сообщить контролю, что все успешо выполнено

            if (nzp_server > 0)
                ExecSQL(conn_web,
                    " Update " + tXX_meta +
                    " Set kod = 1, dat_work = current " +
                    " Where nzp_server = " + nzp_server
                    , true);

            #endregion

            conn_db.Close();
            conn_web.Close();
        } //FindSpisLs



        public Returns FindLsFromDeptorSpis00(Ls finder) //найти и заполнить список адресов по выбранному списку должников
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            #region Проверка finder

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            #endregion

            #region соединение conn_web

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            #endregion

            #region соединение conn_db

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            #endregion

            try
            {

                #region проверка существования таблицы user_processes в БД

#if PG
                if (!TempTableInWebCashe(conn_web, pgDefaultDb + "." + "user_processes"))
#else
            if (!TempTableInWebCashe(conn_web, conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes"))
#endif

                {
                    ret.result = false;
                    ret.text = "Нет таблицы user_processes";
                    conn_web.Close();
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                        ": Нет таблицы user_processes ", MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

                #endregion

                string procId = Guid.NewGuid().ToString();

                string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

                #region обновить данные в таблице user_processes

                AddToUserProc((Finder) finder, tXX_spls, procId, conn_web);

                #endregion

                #region сохранение finder

                SaveFinder(finder, Constants.page_spisls);

                #endregion


                DbTables tables = new DbTables(conn_db);

                decimal d = 0;
                Decimal.TryParse(finder.pkod.Trim(), out d);

                #region заполнение whereString

                List<int> listPoint = new List<int>();
                string wherePointList = ""; // условие для банков
                List<int> listGeu = new List<int>();
                string whereGeuList = ""; // условие для ЖЭУ
                List<int> listArea = new List<int>();
                string whereAreaList = ""; // условие для УК

                string whereString = "";

                // ограничения по ролям
                if (finder.RolesVal != null)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_area)
                                whereAreaList = getAvaliableRolesVal(role.val, listArea, "k.nzp_area");
                                //" and k.nzp_area in (" + role.val + ")";
                            else if (role.kod == Constants.role_sql_wp)
                                wherePointList = getAvaliableRolesVal(role.val, listPoint, "k.nzp_wp");
                                // " and k.nzp_wp in (" + role.val + ")";
                            else if (role.kod == Constants.role_sql_geu)
                                whereGeuList = getAvaliableRolesVal(role.val, listGeu, "k.nzp_geu");
                            //" and k.nzp_geu in (" + role.val + ")";
                        }
                    }
                }
                // формирование базового условия основного запроса
                whereString += whereAreaList + whereGeuList;

                #endregion

                #region Формирование from основного запроса

#if PG
                if (!TempTableInWebCashe(conn_web, pgDefaultDb + "." + "t" + Convert.ToString(finder.nzp_user) + "_debt"))
#else
            if (!TempTableInWebCashe(conn_web, conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" +  "t" + Convert.ToString(finder.nzp_user) + "_debt"))
#endif

                {
                    ret.result = false;
                    ret.text = "Нет таблицы t" + Convert.ToString(finder.nzp_user) + "_debt";
                    conn_web.Close();
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                        ": Нет таблицы  t" + Convert.ToString(finder.nzp_user) + "_debt",
                        MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }

#if PG
                string tXX_debt = pgDefaultDb + tableDelimiter + "t" + Convert.ToString(finder.nzp_user) + "_debt";
#else
            string tXX_debt = conn_web.Database + "@" + DBManager.getServer(conn_web) + tableDelimiter + "t" + Convert.ToString(finder.nzp_user) + "_debt";
#endif

                string fromSql = "";
                // таблица kvar в любом случае
                fromSql +=
                    " from " + tXX_debt + " debt," + tables.kvar + " k, " + tables.dom + " d , " + 
                    Points.Pref + sDataAliasRest + "s_ulica u , "
                    + Points.Pref + sDataAliasRest + "s_rajon r ," + Points.Pref + sDataAliasRest + "s_town t ";
                whereString +=
                    " and debt.nzp_kvar = k.nzp_kvar and k.nzp_dom = d.nzp_dom and k.nzp_wp is not null" +
                    " and u.nzp_ul = d.nzp_ul and r.nzp_raj = u.nzp_raj and r.nzp_town=t.nzp_town";


                #endregion

                #region наименование таблиц tXX_spls/tXX_spls_full/tXX_meta

                //if (nzp_server > 0) tXX_spls += Constants.cons_Kernel.ToString();
#if PG
                string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
                string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

                #endregion

                #region создать кэш-таблицу

                using (DbAdresClient db = new DbAdresClient())
                {
                    ret = db.CreateTableWebLs(conn_web, tXX_spls, true);
                }
                if (!ret.result) return ret;

                #endregion

                int key;
                // Основное тело запроса
                string sql = " Insert into " + tXX_spls_full +
                             " (nzp_kvar,pref, num_ls, pkod, pkod10, nzp_dom, nzp_area, nzp_geu, typek, fio, nkvar, ikvar, nkvar_n, nzp_town, nzp_raj, nzp_ul, town, rajon,  ndom, nkor, idom) " +
                             "Select distinct k.nzp_kvar, k.pref, k.num_ls, k.pkod, k.pkod10, k.nzp_dom, k.nzp_area, k.nzp_geu, k.typek, k.fio, k.nkvar, k.ikvar, k.nkvar_n," +
                             " t.nzp_town, r.nzp_raj, d.nzp_ul,  " + sNvlWord + "(t.town,''), " + sNvlWord +
                             "(r.rajon,''), d.ndom, d.nkor, d.idom "
                             + fromSql + " Where 1=1 " + whereString;

                string insertsql = sql + wherePointList;
                ret = ExecSQL(conn_db, insertsql, true);
                key = LogSQL(conn_web, finder.nzp_user, " completed: " + insertsql);
                if (!ret.result)
                {
                    if (key > 0) LogSQL_Error(conn_web, key, ret.text);
                    conn_db.Close();
                    conn_web.Close();
                    return ret;
                }

                ret = Fill_tXX_spls(conn_db, conn_web, null, tXX_spls_full, 0);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return ret;
                }

                #region создаем индексы на tXX_spls

                using (DbAdresClient db = new DbAdresClient())
                {
#if PG
                    ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                    ret = db.CreateTableWebLs(conn_web, tXX_spls, false);
                }
                if (!ret.result) return ret;

                #endregion

                #region проверка равенства procId и procid из таблицы user_processes

                string actualProcId = GetUserProcId(tXX_spls, conn_web, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return ret;
                }

                if (actualProcId != "" && actualProcId != procId)
                {
                    ret.result = false;
                    ret.text = "Обнаружен новый запрос поиска";
                    ret.tag = -1;
                    conn_db.Close();
                    conn_web.Close();
                    return ret;
                }

                #endregion
            }
            finally
            {
                conn_db.Close();
                conn_web.Close();
            }
            return ret;
        } //FindLsFromDeptorSpis
        
        /// <summary>
        /// Проверяет соответствие заданных пользователем параметров (банков, жэу, УК) значениям RolesVal
        /// </summary>
        /// <param name="roleval"></param>
        /// <param name="listFromUser"></param>
        /// <param name="nameColumn"></param>
        /// <returns></returns>
        private string getAvaliableRolesVal(string roleval, List<int> listFromUser, string nameColumn)
        {
            // Если RolesVal пуст
            if (String.IsNullOrWhiteSpace(roleval))
            {
                return String.Empty;
            }
            // Если список параметров от пользователя пуст
            if (listFromUser == null || listFromUser.Count <= 0)
            {
                // значения формируются из RoleVal
                return " and " + nameColumn + " in (" + roleval + ")";
            }

            string[] arrRolesVal = roleval.Split(',');
            // получаем пересечение данных
            List<int> filteredList = new List<int>();
            foreach (int nzp in listFromUser)
            {
                foreach (var role in arrRolesVal)
                {
                    if (nzp.ToString() != role) continue;
                    filteredList.Add(nzp);
                }
            }
            //если ничего не отфильтровалось
            if (filteredList.Count <= 0)
            {
                return " and " + nameColumn + " in (" + roleval + ")";
            }
            return " and " + nameColumn + " in (" + String.Join(",", filteredList) + ")";
        }

        public Returns Fill_tXX_spls(IDbConnection conn_db, IDbConnection conn_web, IDbTransaction transaction, Ls finder, string temp_table, int nzp_server)
        {
            #region наименование таблиц tXX_spls/tXX_spls_full/tXX_meta
            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
            if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom)) tXX_spls += "dom";
            if (nzp_server > 0) tXX_spls += nzp_server.ToString();
#if PG
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            #endregion
            Returns ret;
            DbTables tables = new DbTables(conn_db);

            #region создать кэш-таблицу
            using (DbAdresClient db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, true);
            }
            if (!ret.result) return ret;
            #endregion

            #region запись данных из temp_table в кэш таблицу spls
            StringBuilder sql = new StringBuilder(" Insert into " + tXX_spls_full + " (nzp_kvar,pref) Select nzp_kvar,pref From " + temp_table);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;
            #endregion


            sql.Remove(0, sql.Length);
            sql.AppendFormat(" update {0} set num_ls=k.num_ls,pkod10=k.pkod10,{1}nzp_dom=k.nzp_dom, ", tXX_spls_full, GlobalSettings.NewGeneratePkodMode ? "" : "pkod=k.pkod,");
            sql.Append(" nzp_area=k.nzp_area,nzp_geu=k.nzp_geu,typek=k.typek,fio=k.fio,nkvar=k.nkvar,ikvar=k.ikvar,nkvar_n =k.nkvar_n ");
            sql.AppendFormat(" from {0}_data.kvar k where k.nzp_kvar = {1}.nzp_kvar",
                Points.Pref, tXX_spls_full);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            if (GlobalSettings.NewGeneratePkodMode)
            {
                sql.Remove(0, sql.Length);
                sql.AppendFormat("update {0} set pkod = (select pkod from {1} where is_princip=0 and is_default=1 and nzp_kvar ={0}.nzp_kvar  limit 1)",
                    tXX_spls_full, tables.kvar_pkodes);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;
            }

            sql.Remove(0, sql.Length);
            sql.AppendFormat(" update {0} set nzp_ul=d.nzp_ul,ndom=d.ndom, nkor=d.nkor, idom=d.idom ", tXX_spls_full);
            sql.AppendFormat(" from {0}_data.dom d where d.nzp_dom = {1}.nzp_dom", Points.Pref, tXX_spls_full);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.AppendFormat(" update {0} ", tXX_spls_full);
            if (DBManager.TempColumnInWebCashe(conn_db, Points.Pref + DBManager.sDataAliasRest + "s_ulica", "ulicareg"))
            {
                sql.AppendFormat(
                    " set ulica=(select trim(" + sNvlWord + "(ulicareg,''))||' '||trim(" + sNvlWord + "(ulica,'')) from {0}{1}s_ulica where nzp_ul = {2}.nzp_ul), ",
                    Points.Pref, DBManager.sDataAliasRest, tXX_spls_full);
                sql.AppendFormat(
                    " adr=(select trim(" + sNvlWord + "(ulicareg,''))||' '||trim(" + sNvlWord +
                    "(ulica,'')) from {0}{1}s_ulica where nzp_ul = {2}.nzp_ul), ", Points.Pref, DBManager.sDataAliasRest,
                    tXX_spls_full);
            }
            else
            {
                sql.AppendFormat(" set ulica=(select trim(" + sNvlWord + "(ulica,'')) from {0}{1}s_ulica where nzp_ul = {2}.nzp_ul), ",
                    Points.Pref, DBManager.sDataAliasRest, tXX_spls_full);
                sql.AppendFormat(
                    " adr=(select 'улица '||trim(" + sNvlWord +
                    "(ulica,'')) from {0}{1}s_ulica where nzp_ul = {2}.nzp_ul), ", Points.Pref, DBManager.sDataAliasRest,
                    tXX_spls_full);
            }
            sql.AppendFormat(" nzp_raj=(select nzp_raj from {0}{1}s_ulica where nzp_ul = {2}.nzp_ul) ", Points.Pref, DBManager.sDataAliasRest, tXX_spls_full);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.AppendFormat("update {0} set rajon= r.rajon," +
                                           " nzp_town=r.nzp_town, ulica = " + sNvlWord + "(ulica,'') || ' / '||trim(" + sNvlWord + "(r.rajon,'')), adr = {0}.adr || ' / '||trim(" + sNvlWord + "(r.rajon,'')) from ", tXX_spls_full);
            sql.AppendFormat("  {0}_data.s_rajon r where r.nzp_raj = {1}.nzp_raj", Points.Pref, tXX_spls_full);

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.AppendFormat("update {0} set town = trim(" + sNvlWord + "(t.town,'')), adr = {0}.adr || ' / '||trim(" + sNvlWord + "(t.town,''))||'   дом '" +
                             "||  trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord + "(nkor,''))||'  кв. '||trim(" + sNvlWord + "(nkvar,''))||'  ком. '||trim(" + sNvlWord + "(nkvar_n,'')) from ", tXX_spls_full);
            sql.AppendFormat("{0}_data.s_town t where t.nzp_town = {1}.nzp_town", Points.Pref, tXX_spls_full);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set stypek = (select {1} stypek from {2} t where {0}.typek = t.nzp_y  and t.nzp_res = 9999)", tXX_spls_full, "t.name_y".CastTo("CHARACTER", "20"), tables.res_y);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set stypek = (case when {0}.typek = 1 then 'население'" +
            //                 " when {0}.typek = 2 then 'бюджет'" +
            //                 " when {0}.typek = 3 then 'арендаторы' else 'неопределено'" +
            //                 " end)", tXX_spls_full);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;


            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set sostls ", tXX_spls_full);
            //sql.AppendFormat("  = (select ry.name_y as sostls from {0} ry, {1} k where cast(k.is_open as integer) = ry.nzp_y and k.nzp_kvar={2}.nzp_kvar and ry.nzp_res = 18)  ", tables.res_y, tables.kvar, tXX_spls_full);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set sostls = case when k.is_open = '1' then 'открыт'" +
            //                 " when k.is_open = '2' then 'закрыт'" +
            //                 " when k.is_open = '3' then 'неопределено' else 'неопределено'" +
            //                 " end from {1} k " +
            //                 " where {0}.nzp_kvar=k.nzp_kvar", tXX_spls_full, tables.kvar);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.Append("update " + tXX_spls_full + " set ");
            sql.Append(" has_pu = 0 ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.AppendFormat("select " + sUniqueWord + " pref from {0}", tXX_spls_full);
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result) return ret;

            while (reader.Read())
            {
                string cur_pref = reader["pref"] != DBNull.Value ? ((string)reader["pref"]).Trim() : "";
                if (cur_pref == "") continue;

                #region обновление данных в temp_tXX_spls
                if (TempTableInWebCashe(conn_db, null, cur_pref + "_data" + tableDelimiter + "counters_spis"))
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat(" Update {0} Set has_pu = 1 ", tXX_spls_full);
                    sql.AppendFormat(" Where pref = '{0}'", cur_pref);
                    sql.AppendFormat("   and nzp_kvar in ( Select nzp From {0}_data{1}counters_spis ", cur_pref, tableDelimiter);
                    sql.AppendFormat(" Where nzp_type = {0}", (int)CounterKinds.Kvar);
                    sql.Append("   and is_actual <> 100 ) ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;

                    sql.Remove(0, sql.Length);
                    sql.AppendFormat(" Update {0} Set has_pu = 1 ", tXX_spls_full);
                    sql.AppendFormat(" Where pref = '{0}'", cur_pref);
                    sql.AppendFormat("   and exists ( Select 1 From {0}_data{1}counters_spis cs, {0}_data{1}counters_link cl ", cur_pref, tableDelimiter);
                    sql.AppendFormat(" Where cs.nzp_type in ({0},{1}) ", (int)CounterKinds.Group, (int)CounterKinds.Communal);
                    sql.Append("   and cs.nzp_counter = cl.nzp_counter");
                    sql.Append("   and cs.is_actual <> 100 ");
                    sql.Append("   and " + tXX_spls_full + ".nzp_kvar = cl.nzp_kvar)");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                }

                #endregion
            }
            reader.Close();
            string sort_schema = Points.Pref + "_data";
#if PG
            sort_schema = pgDefaultSchema;
#endif
            sql.Remove(0, sql.Length);
            sql.Append("update " + tXX_spls_full + " set ");
            sql.Append(" ikvar_n = " + sort_schema + tableDelimiter + "sortnum(nkvar_n) ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.Append("update " + tXX_spls_full + " set ");
            sql.Append(" mark = 1 ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            if (Points.IsSmr)
            {
                sql.Remove(0, sql.Length);
                sql.Append("update " + tXX_spls_full + " set num_ls_litera = case when substr(pkod||'',11,1) = '0' then pkod10||'' else pkod10||' '||substr(pkod||'',11,1) end");
                ret = ExecSQL(conn_db, sql.ToString(), true);
            }

            #region создаем индексы на tXX_spls
            using (DbAdresClient db = new DbAdresClient())
            {
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                ret = db.CreateTableWebLs(conn_web, tXX_spls, false);
            }
            if (!ret.result) return ret;
            #endregion

            return ret;
        }

        public Returns Fill_tXX_spls(IDbConnection conn_db, IDbConnection conn_web, IDbTransaction transaction, string tXX_spls_full, int nzp_server)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            DbTables tables = new DbTables(conn_db);
            if (GlobalSettings.NewGeneratePkodMode)
            {
                sql.Remove(0, sql.Length);
                sql.AppendFormat("update {0} set pkod = (select pkod from {1} where is_princip=0 and is_default=1 and nzp_kvar ={0}.nzp_kvar " +
                                 " limit 1) ",
                    tXX_spls_full, tables.kvar_pkodes);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;
            }
            // обновление колонок с улицей и адресом
            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} ", tXX_spls_full);
            //// если колонка  "ulicareg" в таблице s_ulica существует
            ////if (DBManager.TempColumnInWebCashe(conn_db, Points.Pref + DBManager.sDataAliasRest + "s_ulica", "ulicareg"))
            ////{
            //    //sql.AppendFormat(
            //    //    " set ulica=" +
            //    //    "   trim(" + sNvlWord + "(ulicareg,''))||' '||trim(" + sNvlWord + "(ulica,''))" +
            //    //    "   || ' / '||trim(" + sNvlWord + "(rajon,''))," +
            //    //    " adr=trim(" + sNvlWord + "(ulicareg,''))||' '||trim(" + sNvlWord + "(ulica,''))" +
            //    //    "   || ' / '||trim(" + sNvlWord + "(r.rajon,'')) || ' / '||trim(" + sNvlWord + "(t.town,''))||'   дом '" +
            //    //    "   ||  trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord + "(nkor,''))" +
            //    //    "   ||'  кв. '||trim(" + sNvlWord + "(nkvar,''))||'  ком. '||trim(" + sNvlWord + "(nkvar_n,''))" +
            //    //    " from {0}{1}s_ulica u, {0}{1}s_rajon r, {0}{1}s_town t  " +
            //    //    " where u.nzp_ul = {2}.nzp_ul and r.nzp_raj= {2}.nzp_raj  " +
            //    //    " and t.nzp_town= {2}.nzp_town  ", Points.Pref, DBManager.sDataAliasRest,
            //    //    tXX_spls_full);


            //if (!ret.result) return ret;

            ////}
            ////else
            ////{
            ////    // если колонки ulicareg нет, то в адресе добавляется слово улица
            ////    sql.AppendFormat(
            ////        " set ulica=(select trim(" + sNvlWord + "(ulica,''))" +
            ////        "|| ' / '||trim(" + sNvlWord + "(rajon,''))" +
            ////        "from {0}{1}s_ulica u, {0}{1}s_rajon r where u.nzp_ul = {2}.nzp_ul and r.nzp_raj= {2}.nzp_raj  ), ",
            ////        Points.Pref, DBManager.sDataAliasRest, tXX_spls_full);
            ////    sql.AppendFormat(
            ////        " adr=(select 'улица '||trim(" + sNvlWord + "(ulica,''))" +
            ////        "|| ' / '||trim(" + sNvlWord + "(r.rajon,'')) || ' / '||trim(" + sNvlWord + "(t.town,''))||'   дом '" +
            ////        "||  trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord + "(nkor,''))||'  кв. '||trim(" + sNvlWord + "(nkvar,''))||'  ком. '||trim(" + sNvlWord + "(nkvar_n,''))" +
            ////        "from {0}{1}s_ulica u, {0}{1}s_rajon r, {0}{1}s_town t  where u.nzp_ul = {2}.nzp_ul and r.nzp_raj= {2}.nzp_raj  and t.nzp_town= {2}.nzp_town ) ", Points.Pref, DBManager.sDataAliasRest,
            ////        tXX_spls_full);
            ////}
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set stypek = (select {1} stypek from {2} t where {0}.typek = t.nzp_y  and t.nzp_res = 9999) ", tXX_spls_full, "t.name_y".CastTo("CHARACTER", "20"), tables.res_y);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set stypek = (case when {0}.typek = 1 then 'население'" +
            //                 " when {0}.typek = 2 then 'бюджет'" +
            //                 " when {0}.typek = 3 then 'арендаторы' else 'неопределено'" +
            //                 " end)", tXX_spls_full);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            // обновление колонки sostls (открыт, закрыт, неопределено)
            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set sostls ", tXX_spls_full);
            //sql.AppendFormat("  = (select ry.name_y as sostls from {0} ry, {1} k where k.is_open = cast (ry.nzp_y as char(1)) and k.nzp_kvar={2}.nzp_kvar and ry.nzp_res = 18) ", tables.res_y, tables.kvar, tXX_spls_full);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" update {0} set sostls = case when k.is_open = '1' then 'открыт'" +
            //                 " when k.is_open = '2' then 'закрыт'" +
            //                 " when k.is_open = '3' then 'неопределено' else 'неопределено'" +
            //                 " end from {1} k " +
            //                 " where {0}.nzp_kvar=k.nzp_kvar", tXX_spls_full, tables.kvar);
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            //if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.AppendFormat("select " + sUniqueWord + " pref from {0}", tXX_spls_full);
            IDataReader reader = null;
            try
            {
                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result) return ret;
                // Обновление наличия ПУ по выбранным записям
                while (reader.Read())
                {
                    string pref = reader["pref"] != DBNull.Value ? ((string)reader["pref"]).Trim() : "";
                    if (pref == "") continue;
                    if (!TempTableInWebCashe(conn_db, null, pref + "_data" + tableDelimiter + "counters_spis")) continue;
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat(" Update {0} t Set has_pu = 1 ", tXX_spls_full);
                    sql.AppendFormat(" Where pref = {0}", Utils.EStrNull(pref));
                    sql.Append(" and has_pu<>1 ");
                    sql.AppendFormat("   and exists ( Select 1 From {0}_data{1}counters_spis s ", pref, tableDelimiter);
                    sql.AppendFormat(" Where nzp_type = {0}", (int)CounterKinds.Kvar);
                    sql.Append("   and is_actual <> 100  and t.nzp_kvar=s.nzp) ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;

                    sql.Remove(0, sql.Length);
                    sql.AppendFormat(" Update {0} sp Set has_pu = 1 ", tXX_spls_full);
                    sql.AppendFormat(" Where pref = {0}", Utils.EStrNull(pref));
                    sql.Append(" and has_pu<>1 ");
                    sql.AppendFormat(
                        "   and exists ( Select 1 From {0}_data{1}counters_spis cs, {0}_data{1}counters_link cl ", pref, tableDelimiter);
                    sql.AppendFormat(" Where cs.nzp_type in ({0},{1}) ", (int)CounterKinds.Group,
                        (int)CounterKinds.Communal);
                    sql.Append("   and cs.nzp_counter = cl.nzp_counter");
                    sql.Append("   and cs.is_actual <> 100 ");
                    sql.Append("   and sp.nzp_kvar = cl.nzp_kvar)");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";
                MonitorLog.WriteLog(" FindLS00(). Ошибка поиска ЛС по дополнительным шаблонам " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            string sort_schema = Points.Pref + "_data";
#if PG
            sort_schema = pgDefaultSchema;
#endif
            // перевод nkvar_n в тип integer и сохраняет это значение в колонке ikvar_n
            sql.Remove(0, sql.Length);
            sql.Append("update " + tXX_spls_full + " set ");
            sql.Append(" ikvar_n = " + sort_schema + tableDelimiter + "sortnum(nkvar_n) where nkvar_n is not null and trim(nkvar_n)<>'' and trim(nkvar_n)<>'-'");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;
            if (Points.IsSmr)
            {
                sql.Remove(0, sql.Length);
                sql.Append("update " + tXX_spls_full + " set num_ls_litera = case when substr(pkod||'',11,1) = '0' then pkod10||'' else pkod10||' '||substr(pkod||'',11,1) end ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
            }
            return ret;
        }

        private Returns SaveFinder(Ls finder, int nzp_page)
        {
            Returns ret = SaveFinder((Dom)finder, nzp_page);
            if (!ret.result) return ret;
            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spfinder = "t" + Convert.ToString(finder.nzp_user) + "_spfinder";

            if (ret.result)
            {
                string sql = "";

                if (finder.dopFind != null &&
                    (finder.dopFind.Contains(Pages.ReportDebtorExtJs.ToString()) || finder.dopFind.Contains(Constants.page_reportlistdeptor.ToString())))
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'По списку должников из t" + finder.nzp_user + "_debt \',\' \'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (Points.IsSmr)
                {
                    if (finder.pkod10 > 0)
                    {
                        sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.pkod10.ToString() + "\'," + nzp_page.ToString() + ")";
                        ret = ExecSQL(conn_web, sql, true);
                        if (!ret.result)
                        {
                            conn_web.Close();
                            return ret;
                        }
                    }
                }
                else if (finder.num_ls > 0)
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.num_ls.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.pkod.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Платежный код\',\'" + finder.pkod.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.town.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Город/район\',\'" + finder.town.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.rajon.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Населенный пункт\',\'" + finder.rajon.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.stateID > 0)
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Состояние\',\'" + finder.state + "\'," + nzp_page + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.typek > 0)
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Тип счета\',\'" + finder.stypek.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.porch.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Подъезд\',\'" + finder.porch.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nkvar.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Квартира с\',\'" + finder.nkvar.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nkvar_po.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Квартира по\',\'" + finder.nkvar_po.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.nkvar_n.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Комната\',\'" + finder.nkvar_n.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.phone.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Телефон квартиры\',\'" + finder.phone.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.fio.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Квартиросъемщик\',\'" + finder.fio.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }

                if (finder.uch.Trim() != "")
                {
                    sql = "insert into " + tXX_spfinder + " values (0,\'Участок\',\'" + finder.uch.ToString() + "\'," + nzp_page.ToString() + ")";
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
            }
            conn_web.Close();
            return ret;
        }

        private Returns SaveFinder(IDbConnection connection, IDbTransaction transaction, Dom finder, int nzp_page)
        {
            //todo pg
            string tXX_spfinder = "t" + Convert.ToString(finder.nzp_user) + "_spfinder";

            Returns ret = new Returns(true);

            //проверка наличия таблицы в БД
            if (!TableInWebCashe(connection, tXX_spfinder))
            {
                //создать таблицу webdata
                ret = ExecSQL(connection,
                          " Create table " + tXX_spfinder +
                          " (nzp_finder serial, " +
                          "  name char(100), " +
                          "  value char(255), " +
                          "  nzp_page integer " +
                          " ) ", true);
            }
            if (!ret.result) return ret;

            string sql = "delete from " + tXX_spfinder + " where nzp_page = " + nzp_page.ToString();
            ret = ExecSQL(connection, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            sql = "";
            if (finder.nzp_wp > 0)
            {

                sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'"
                    + (finder.point.Length > 250 ? finder.point.Substring(0, 255) : finder.point) + "\'," + nzp_page.ToString() + ")";
            }
            else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'" +
                    (finder.point.Length > 250 ? finder.point.Substring(0, 255) : finder.point) + "\'," + nzp_page.ToString() + ")";
            }
            if (sql != "")
            {
                ret = ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            sql = "";
            if (finder.nzp_area > 0)
                sql = "insert into " + tXX_spfinder + " values (0,\'Управляющая организация\',\'" +
                    (finder.area.Length > 250 ? finder.area.Substring(0, 250) : finder.area) + "\'," + nzp_page.ToString() + ")";

            else if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
                sql = "insert into " + tXX_spfinder + " values (0,\'Управляющая организация\',\'" +
                    (finder.area.Length > 250 ? finder.area.Substring(0, 250) : finder.area) + "\'," + nzp_page.ToString() + ")";

            if (sql != "")
            {
                ret = ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            if (finder.nzp_geu > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Отделение\',\'" + finder.geu.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            if (finder.nzp_ul > 0)
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Улица\',\'" + finder.ulica.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            if (finder.ndom.Trim() != "")
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Номер дома с\',\'" + finder.ndom.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            if (finder.ndom_po.Trim() != "")
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Номер дома по\',\'" + finder.ndom_po.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            if (finder.nkor.Trim() != "")
            {
                sql = "insert into " + tXX_spfinder + " values (0,\'Корпус\',\'" + finder.nkor.ToString() + "\'," + nzp_page.ToString() + ")";
                ret = ExecSQL(connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            return ret;
        }

        private Returns SaveFinder(Dom finder, int nzp_page)
        {
            if (finder.nzp_user <= 0)
            {
                return new Returns(false, "Пользователь не определен");
            }

            Returns ret = Utils.InitReturns();

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            ret = SaveFinder(conn_web, null, finder, nzp_page);

            conn_web.Close();
            return ret;
        }

        //----------------------------------------------------------------------
        private void CreateTableWebDom(IDbConnection conn_web, string tXX_spdom, bool onCreate, out Returns ret) //
        //----------------------------------------------------------------------
        {
            if (onCreate)
            {
                ret = DBManager.DbCreateTable(DBManager.ConnectToDb.Web,
                    DBManager.CreateTableArgs.DropIfExists, DBManager.GetFullBaseName(conn_web), tXX_spdom,
                    "nzp_dom integer", "nzp_ul integer", "nzp_area integer", "nzp_geu integer", "nzp_wp integer",
                    "area char(60)", "geu char(60)", "ulica char(40)", "ulicareg char(40)", "rajon char(40)",
                    "town char(40)", "ndom char(150)", "idom integer", "pref char(10)", "point char(60)",
                    "mark integer", "has_pu integer");

                /*
                if (TableInWebCashe(conn_web, tXX_spdom))
                {
                    ExecSQL(conn_web, " Drop table " + tXX_spdom, false);
                }

                //создать таблицу webdata:tXX_spDom
                ret = ExecSQL(conn_web,
                          " Create table " + tXX_spdom +
                          " ( nzp_dom    integer, " +
                          "   nzp_ul     integer, " +
                          "   nzp_area   integer, " +
                          "   nzp_geu    integer, " +
                          "   nzp_wp     integer, " +

                          "   area     char(60)," +
                          "   geu      char(60)," +

                          "   ulica    char(40)," +
                          "   ulicareg    char(40)," +
                          "   rajon    char(40)," +
                          "   town    char(40)," +
                          "   ndom     char(20)," +
                          "   idom     integer, " +
                          "   pref     char(10)," +
                          "   point    char(60)," +
                          "   mark     integer," +
                          "   has_pu   integer" +
                          " ) ", true);
                if (!ret.result)
                {
                    return;
                }
                */
            }
            else
            {
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_spdom + " on " + tXX_spdom + " (nzp_dom) ", true);
                ret = ExecSQL(conn_web, " Create index ix2_" + tXX_spdom + " on " + tXX_spdom + " (ulica,idom) ", true);
                ret = ExecSQL(conn_web, " Create index ix3_" + tXX_spdom + " on " + tXX_spdom + " (area,ulica,idom) ", true);

                if (!ret.result)
                {
#if PG
                    ret = ExecSQL(conn_web, " analyze  " + tXX_spdom, true);
#else
                    ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spdom, true);
#endif

                }
            }
        }
        //----------------------------------------------------------------------
        public void FindDom(Dom finder, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            FindDom00(finder, out ret, 0);
        }

        /// <summary>
        /// Найти и заполнить список домов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <param name="nzp_server"></param>
        private void FindDom00(Dom finder, out Returns ret, int nzp_server)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }

            string conn_kernel = Points.GetConnKernel(finder.nzp_wp, nzp_server);
            if (conn_kernel == "")
            {
                ret = new Returns(false, "Не определен connect к БД");
                return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            FindDom00(conn_db, null, conn_web, null, finder, out ret, nzp_server);

            conn_db.Close(); //закрыть соединение с основной базой
            conn_web.Close();
            return;
        }

        private void FindDom00(IDbConnection conn_db, IDbTransaction trans_db, IDbConnection conn_web, IDbTransaction trans_web, Dom finder, out Returns ret, int nzp_server) //найти и заполнить список домов
        {
            string tXX_spdom = "t" + finder.nzp_user + "_spdom" + (nzp_server > 0 ? nzp_server.ToString() : "");
#if PG
            string tXX_spdom_full = pgDefaultDb + "." + tXX_spdom;
#else
            string tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom;
#endif


            string procId = Guid.NewGuid().ToString();

            #region обновить данные в таблице user_processes
            AddToUserProc((Finder)finder, tXX_spdom, procId, conn_web);
            #endregion

            #region сохранение finder
            ret = SaveFinder(conn_web, trans_web, finder, Constants.page_spisdom);
            if (!ret.result) return;
            #endregion

            #region условия whereString
            string whereString;

            List<int> listPoint = new List<int>();
            string wherePointList = ""; // условие для банков
            List<int> listGeu = new List<int>();
            string whereGeuList = ""; // условие для ЖЭУ
            List<int> listArea = new List<int>();
            string whereAreaList = "";// условие для УК

            if (finder.spls != "")
            {
                whereString = " and exists ( Select 1 From " + finder.spls + " spls where spls.nzp_dom=d.nzp_dom) ";
            }
            else
            {
                StringBuilder swhere = new StringBuilder();
                int i;

                if (finder.nzp_dom > 0) swhere.Append(" and d.nzp_dom = " + finder.nzp_dom.ToString());

                //if (finder.nzp_area > 0) swhere.Append(" and d.nzp_area = " + finder.nzp_area.ToString());
                //Формирование условий для УК
                if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
                {
                    listArea.AddRange(finder.list_nzp_area);
                    whereAreaList = " and d.nzp_area in (" + String.Join(",", finder.list_nzp_area) + ")";
                }
                else if (finder.nzp_area > 0)
                {
                    listArea.Add(finder.nzp_area);
                    whereAreaList = " and d.nzp_area = " + finder.nzp_area;
                }
                // Формирование условий для ЖЭУ
                if (finder.nzp_geu > 0)
                {
                    listGeu.Add(finder.nzp_geu);
                    whereGeuList = " and d.nzp_geu = " + finder.nzp_geu;
                }

                if (finder.ndom_po != "")
                {
                    i = Utils.GetInt(finder.ndom_po);
                    if (i > 0) swhere.Append(" and d.idom <= " + i.ToString());

                    i = Utils.GetInt(finder.ndom);
                    if (i > 0) swhere.Append(" and d.idom >= " + i.ToString());
                }
                else if (finder.ndom != "") swhere.Append(" and upper(d.ndom) = " + Utils.EStrNull(finder.ndom.ToUpper()));
                // Формирование 
                if (finder.nzp_wp > 0)
                {
                    listPoint.Add(finder.nzp_wp);
                    wherePointList = " and d.nzp_wp = " + finder.nzp_wp;
                }
                else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
                {
                    listPoint.AddRange(finder.dopPointList);
                    wherePointList = " and d.nzp_wp in (" + String.Join(",", finder.dopPointList) + ")";
                }

                whereString = swhere.ToString();
            }

            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_area) whereAreaList = getAvaliableRolesVal(role.val, listArea, "d.nzp_area"); // " and d.nzp_area in (" + role.val + ")";
                        else if (role.kod == Constants.role_sql_wp) wherePointList = getAvaliableRolesVal(role.val, listPoint, "d.nzp_wp");//" and d.nzp_wp in (" + role.val + ")";
                        else if (role.kod == Constants.role_sql_geu) whereGeuList = getAvaliableRolesVal(role.val, listGeu, "d.nzp_geu");// " and d.nzp_geu in (" + role.val + ")";
                    }
                }
            }
            whereString += wherePointList + whereGeuList + whereAreaList;
            #endregion

            #region выборка из таблицы dom при условии whereString
            DbTables tables = new DbTables(conn_db);
            ExecSQL(conn_db, "drop table t_selected_dom", false);


            string sql =
                "select d.* " +
#if PG
 "into temp t_selected_dom " +
#endif
 " from " + tables.dom + " d ";
            if (finder.nzp_ul > 0 || finder.nzp_town > 0 || finder.nzp_raj > 0)
            {
                sql += ", " + Points.Pref + sDataAliasRest + "s_ulica u ";
                if (finder.nzp_raj > 0 || finder.nzp_town > 0)
                {
                    sql += ", " + Points.Pref + sDataAliasRest + "s_rajon r ";
                }
            }

            sql += " Where d.nzp_wp is not null " + whereString;
            if (finder.nzp_ul > 0 || finder.nzp_town > 0 || finder.nzp_raj > 0)
            {
                sql += " and u.nzp_ul = d.nzp_ul";
                if (finder.nzp_ul > 0) sql += " and u.nzp_ul = " + finder.nzp_ul;
                if (finder.nzp_raj > 0 || finder.nzp_town > 0)
                {
                    sql += " and r.nzp_raj = u.nzp_raj ";
                    if (finder.nzp_raj > 0) sql += " and r.nzp_raj = " + finder.nzp_raj;
                    if (finder.nzp_town > 0) sql += " and r.nzp_town = " + finder.nzp_town;
                }
            }

#if PG
#else
            sql += " into temp t_selected_dom with no log";
#endif

            int key = LogSQL(conn_web, finder.nzp_user, tXX_spdom_full + ": " + sql);

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                if (key > 0) LogSQL_Error(conn_web, key, ret.text);
                return;
            }

            ret = ExecSQL(conn_db, "Create index ix_selected_dom_1 on t_selected_dom (nzp_wp)", true);
            ret = ExecSQL(conn_db, "Create index ix_selected_dom_2 on t_selected_dom (pref)", true);
            ret = ExecSQL(conn_db, "Create index ix_selected_dom_3 on t_selected_dom (nzp_dom)", true);
            ret = ExecSQL(conn_db, "Create index ix_selected_dom_4 on t_selected_dom (nzp_ul)", true);
#if PG
            ret = ExecSQL(conn_db, "analyze t_selected_dom", true);
#else
            ret = ExecSQL(conn_db, "Update statistics for table t_selected_dom", true);
#endif

            #endregion

            ret = DBManager.DbCreateTable(conn_db, DBManager.CreateTableArgs.DropIfExists, true, DBManager.GetFullBaseName(conn_db), "temp_tXX_spdom",
                            "nzp_dom INTEGER", "nzp_ul INTEGER", "nzp_area INTEGER", "nzp_geu INTEGER", "nzp_wp INTEGER", "area CHAR(60)",
                            "geu CHAR(60)", "ulica CHAR(40)", "ulicareg CHAR(40)", "rajon CHAR(40)", "town CHAR(40)", "ndom CHAR(150)",
                            "idom INTEGER", "pref CHAR(10)", "point CHAR(30)", "mark INTEGER", "has_pu INTEGER");

            /*
            #region удаление временной таблицы temp_tXX_spls(содержит результат поиска)
            ExecSQL(conn_db, "drop table temp_tXX_spdom", false);
            #endregion

            #region создание таблицы temp_tXX_spdom
            ret = ExecSQL(conn_db, "CREATE temp TABLE temp_tXX_spdom( " +
                            "    nzp_dom INTEGER, " +
                            "    nzp_ul INTEGER, " +
                            "    nzp_area INTEGER, " +
                            "    nzp_geu INTEGER, " +
                            "    nzp_wp INTEGER, " +
                            "    area CHAR(60), " +
                            "    geu CHAR(60), " +
                            "    ulica CHAR(40), " +
                            "    ulicareg CHAR(40), " +
                            "    rajon CHAR(40), " +
                            "    town CHAR(40), " +
                            "    ndom CHAR(20), " +
                            "    idom INTEGER, " +
                            "    pref CHAR(10), " +
                            "    point CHAR(30), " +
                            "    mark INTEGER, " +
                            "    has_pu INTEGER)", true);
            if (!ret.result)
            {
                return;
            }
            #endregion
            */

            #region запрос
#if PG
            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append("insert into temp_tXX_spdom (pref, nzp_wp, point, nzp_dom, nzp_ul, nzp_area, nzp_geu, idom, ndom, ulica, ulicareg, rajon, town, area, geu, mark, has_pu)");
            sqlBuilder.AppendFormat(" Select d.pref, d.nzp_wp, {0}, d.nzp_dom, d.nzp_ul, d.nzp_area, d.nzp_geu, d.idom,  ", "p.point".CastTo("CHARACTER", "30"));
            sqlBuilder.Append(" 'дом ' || trim(coalesce(d.ndom,''))||' корп. '||trim(coalesce(d.nkor,'')) as ndom, ");
            sqlBuilder.Append(" u.ulica, u.ulicareg, r.rajon, t.town, area, geu, 1 as mark, 0 as has_pu ");
            sqlBuilder.Append(" From t_selected_dom d");
            sqlBuilder.AppendFormat(" left outer join {0} p on p.nzp_wp = d.nzp_wp", tables.point);
            sqlBuilder.AppendFormat(" left outer join {0} u on d.nzp_ul = u.nzp_ul", tables.ulica);
            sqlBuilder.AppendFormat(" left outer join {0} r on u.nzp_raj = r.nzp_raj", tables.rajon);
            sqlBuilder.AppendFormat(" left outer join {0} t on r.nzp_town = t.nzp_town", tables.town);
            sqlBuilder.AppendFormat(" left outer join {0} a on d.nzp_area = a.nzp_area", tables.area);
            sqlBuilder.AppendFormat(" left outer join {0} g on d.nzp_geu = g.nzp_geu", tables.geu);

            var sqlInsert = sqlBuilder.ToString();

#else
            string sqlInsert = //" Insert into " + tXX_spdom_full + " (pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica, area,geu,mark) " +
                "insert into temp_tXX_spdom (pref, nzp_wp, point, nzp_dom, nzp_ul, nzp_area, nzp_geu, idom, ndom, ulica, ulicareg, rajon, town, area, geu, mark, has_pu)" +
                " Select d.pref, d.nzp_wp, p.point, d.nzp_dom, d.nzp_ul, d.nzp_area, d.nzp_geu, d.idom,  " +
                " 'дом ' || trim(nvl(d.ndom,''))||' корп. '||trim(nvl(d.nkor,'')) as ndom, " +
                " u.ulica, u.ulicareg, r.rajon, t.town, area, geu, 1 as mark, 0 as has_pu " +
                " From t_selected_dom d" +
                    ", " + tables.ulica + " u" +
                    ", " + tables.point + " p" +
                    ", outer (" + tables.rajon + " r, outer " + tables.town + " t)" +
                    ", outer " + tables.area + " a" +
                    ", outer " + tables.geu + " g" +
                    " Where p.nzp_wp = d.nzp_wp and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj and r.nzp_town = t.nzp_town and d.nzp_area = a.nzp_area and d.nzp_geu = g.nzp_geu";
#endif

            #endregion

            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                #region выполнение запроса

                //записать текст sql в лог-журнал
                key = LogSQL(conn_web, finder.nzp_user, tXX_spdom_full + ":" + sql);

                ret = ExecSQL(conn_db, sqlInsert, true);
                if (!ret.result)
                {
                    if (key > 0) LogSQL_Error(conn_web, key, ret.text);
                    return;
                }
                #endregion
            }
            else
            {
#if PG
                sql = "select distinct pref from t_selected_dom";
#else
                sql = "select unique pref from t_selected_dom";

#endif
                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return;
                }

                string cur_pref;

                while (reader.Read())
                {

                    cur_pref = reader["pref"] != DBNull.Value ? ((string)reader["pref"]).Trim() : "";

                    if (cur_pref == "") continue;

                    #region доп условия  к запросу
                    whereString = "";
                    RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(cur_pref));

                    if (finder.dopFind != null)
                        if (finder.dopFind.Count > 0) //учесть дополнительные шаблоны
                        {
                            foreach (string s in finder.dopFind)
                            {
                                if (s.Trim() != "") whereString += " and 0 < (" + s.Replace("PREFX", cur_pref).Replace("{ALIAS}", "d").Replace("CYEAR", (r_m.year_ % 100).ToString("00")).Replace("CMONTH", r_m.month_.ToString("00")) + ")";
                            }
                        }

                    if (finder.dopUsl != null)
                    {
                        foreach (var s in finder.dopUsl)
                        {
                            if (string.IsNullOrWhiteSpace(s)) continue;
                            whereString += " and " + s.Replace("PREFX", cur_pref).Replace("{ALIAS}", "k").Replace("CNTRPRFX", Points.Pref).
                                Replace("CYEAR", (r_m.year_ % 100).ToString("00")).Replace("CMONTH", r_m.month_.ToString("00")) + "";
                        }
                    }

                    sql = string.Format(
                                        "{0} {1} d.pref = {2}",
                        sqlInsert,
                        sqlInsert.IndexOf("where", StringComparison.OrdinalIgnoreCase) == -1 ? "where" : "and",
                        Utils.EStrNull(cur_pref));
                    sql += whereString;
                    #endregion

                    #region выполнение запроса
                    //записать текст sql в лог-журнал
                    key = LogSQL(conn_web, finder.nzp_user, tXX_spdom_full + ":" + sql);

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        if (key > 0) LogSQL_Error(conn_web, key, ret.text);
                        return;
                    }
                    #endregion

                    #region обновление таблицы temp_tXX_spdom
#if PG
                    if (TempTableInWebCashe(conn_db, null, cur_pref + "_data.counters_spis"))
#else
                    if (TempTableInWebCashe(conn_db, null, cur_pref + "_data:counters_spis"))
#endif
                    {
#if PG
                        sql = "Update temp_tXX_spdom " +// tXX_spdom_full +
                            " Set has_pu = 1" +
                            " Where pref = '" + cur_pref + "'" +
                                " and exists (select 1 from " + cur_pref + "_data.counters_spis" +
                                                " where nzp_type = " + (int)CounterKinds.Dom +
                                                    " and is_actual <> 100 and nzp=nzp_dom)";
#else
                        sql = "Update temp_tXX_spdom " +// tXX_spdom_full +
                            " Set has_pu = 1" +
                            " Where pref = '" + cur_pref + "'" +
                                " and nzp_dom in (select nzp from " + cur_pref + "_data:counters_spis" +
                                                " where nzp_type = " + (int)CounterKinds.Dom +
                                                    " and is_actual <> 100)";
#endif

                        ret = ExecSQL(conn_db, sql.ToString(), true);
                        if (!ret.result)
                        {
                            return;
                        }

#if PG
                        sql = " Update temp_tXX_spdom " + //tXX_spdom_full +
                            " Set has_pu = 1 " +
                            " Where pref = '" + cur_pref + "'" +
                            "   and exists ( Select 1 From " + cur_pref + "_data.counters_spis cs, " + cur_pref + "_data.counters_link cl, " + tables.kvar + " k " +
                                                " Where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
                                                "   and cs.nzp_counter = cl.nzp_counter" +
                                                "   and cs.is_actual <> 100 " +
                                                " and cl.nzp_kvar = k.nzp_kvar" +
                                                " and k.nzp_dom = temp_tXX_spdom" /*+ tXX_spdom_full */+ ".nzp_dom)";
#else
                        sql = " Update temp_tXX_spdom " + //tXX_spdom_full +
                            " Set has_pu = 1 " +
                            " Where pref = '" + cur_pref + "'" +
                            "   and exists ( Select 1 From " + cur_pref + "_data:counters_spis cs, " + cur_pref + "_data:counters_link cl, " + tables.kvar + " k " +
                                                " Where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
                                                "   and cs.nzp_counter = cl.nzp_counter" +
                                                "   and cs.is_actual <> 100 " +
                                                " and cl.nzp_kvar = k.nzp_kvar" +
                                                " and k.nzp_dom = temp_tXX_spdom" /*+ tXX_spdom_full */+ ".nzp_dom)";
#endif

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            return;
                        }
                    }
                    #endregion
                }
            }

            #region проверка равенства procId и procid из таблицы user_processes
            string actualProcId = GetUserProcId(tXX_spdom, conn_web, out ret);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            if (actualProcId != "" && actualProcId != procId)
            {
                ret.result = false;
                ret.text = "Обнаружен новый запрос поиска";
                ret.tag = -1;
                conn_db.Close();
                conn_web.Close();
                return;
            }
            #endregion

            #region создать кэш-таблицу
            CreateTableWebDom(conn_web, tXX_spdom, true, out ret);
            if (!ret.result) return;
            #endregion

            #region запись данных из temp_tXX_spdom в кэш таблицу spdom
            sql = " Insert into " + tXX_spdom_full +
                " (pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica,ulicareg,rajon,town, area,geu,mark,has_pu) " +
                " select pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica,ulicareg,rajon,town, area,geu,mark,has_pu from temp_tXX_spdom";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return;
            #endregion

            ExecSQL(conn_db, "drop table temp_tXX_spdom", false);

            #region создаем индексы на tXX_spDom
            CreateTableWebDom(conn_web, tXX_spdom, false, out ret);
            #endregion
        }

        public Returns ChangeAddressLs(Finder finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);
            if (finder.pref == "") return new Returns(false, "Не выбран банк данных", -1);
            if (finder.dopFind.Count == 0) return new Returns(false, "Новый адрес не выбран", -1);

            int nzp_dom = 0;
            Int32.TryParse(finder.dopFind[0], out nzp_dom);
            if (nzp_dom == 0) return new Returns(false, "Новый адрес не выбран", -1);
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string t = "";
#if PG
            t = pgDefaultSchema;
#else
            t = conn_web.Database + "@" + DBManager.getServer(conn_web);
#endif
            string tXX = t + tableDelimiter + "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;
            string tXX_spls = t + tableDelimiter + "t" + Convert.ToString(finder.nzp_user) + "_spls";

            conn_web.Close();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region проверка существования таблицы tXX в БД
            if (!TempTableInWebCashe(conn_db, tXX))
            {
                ret.text = "Нет выбранных лицевых счетов";
                ret.tag = -1;
                return ret;
            }
            #endregion

            string intotempifmx = "", intotemppg = "";
#if PG
            intotemppg = "into temp tempsells";
#else                
            intotempifmx = " Into temp tempsells with no log ";
#endif

            #region Записать во временную таблицу ЛС, у которых надо поменять адрес
            ExecSQL(conn_db, "drop table tempsells", false);
            StringBuilder sql = new StringBuilder("select nzp_dom, nzp_kvar, adr " +
                intotemppg +
                " from " + tXX +
                " where mark = 1 and pref = '" + finder.pref + "'" + intotempifmx);

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            DbTables tables = new DbTables(conn_db);

            #region получить адрес нового дома
            sql.Remove(0, sql.Length);
            sql.Append("select u.nzp_ul, r.nzp_raj, ulica, ndom,  ");
            sql.Append(" trim(" + sNvlWord + "(ulicareg,'улица'))||' '||trim(" + sNvlWord + "(ulica,''))||' / '||trim(" + sNvlWord + "(rajon,'')) ||' / '|| trim(" + sNvlWord + "(town,'')) ||'   дом '||  ");
            sql.Append(" trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord + "(nkor,'')) as adr ");
            sql.Append("from ");
            sql.Append(tables.town + " t, " + tables.rajon + " r, " + tables.ulica + " u, " + tables.dom + " d ");
            sql.Append("where d.nzp_dom = " + nzp_dom + " and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj and t.nzp_town = r.nzp_town");
            MyDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            string newadr = "";
            Dom dom = new Dom();
            if (reader.Read())
            {
                if (reader["adr"] != DBNull.Value) dom.adr = Convert.ToString(reader["adr"]);
                if (reader["ulica"] != DBNull.Value) dom.ulica = Convert.ToString(reader["ulica"]);
                if (reader["ndom"] != DBNull.Value) dom.ndom = Convert.ToString(reader["ndom"]);
                if (reader["nzp_ul"] != DBNull.Value) dom.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                if (reader["nzp_raj"] != DBNull.Value) dom.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
            }
            newadr = dom.adr + "(код дома " + nzp_dom + ")";
            #endregion

            string loc_kvar = "";
#if PG
            loc_kvar = finder.pref + "_data" + tableDelimiter + "kvar";
#else
            loc_kvar = finder.pref + "_data" + "@" + DBManager.getServer(conn_web) + tableDelimiter + "kvar";
#endif
            string where = "and nzp_kvar in (select nzp_kvar from tempsells)";

            sql.Remove(0, sql.Length);

            IDbTransaction transaction = conn_db.BeginTransaction();
            #region обновить kvar в центральном банке
            sql.Append("update " + tables.kvar + " set nzp_dom = " + nzp_dom + " where pref = '" + finder.pref + "' ");
            sql.Append(where);
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            #endregion

            #region обновить kvar в локальном банке
            sql.Remove(0, sql.Length);
            sql.Append("update " + loc_kvar + " set nzp_dom = " + nzp_dom + " where 1 = 1 ");
            sql.Append(where);
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            #endregion

            #region Для корректного отображения Сальдо по дому - очистить таблицу fn_ukrgudom
            sql.Remove(0, sql.Length);
            var dt = ClassDBUtils.OpenSQL(string.Format("select dbname from {0}s_baselist where idtype = 4;", Points.Pref + sKernelAliasRest), conn_db, transaction).resultData;
            foreach (DataRow db_name in dt.Rows)
            {
                sql.Append("delete from " + CastValue<string>(db_name["dbname"]).Trim() + ".fn_ukrgudom where nzp_dom in (select nzp_dom from tempsells) and month_ = " + Points.CalcMonth.month_ + " and year_ = " + Points.CalcMonth.year_ + ";");
            }
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            #endregion

            #region обновить sysevent
            ret = new Returns(
            DbAdmin.InsertSysEventChangeAdr(new SysEvents()
            {
                pref = finder.pref,
                nzp_user = finder.nzp_user,
                note = newadr
            }, transaction, conn_db));

            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            #endregion

            #region обновление данных в выбранном списке л/с
            if (TempTableInWebCashe(conn_db, tXX_spls))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update " + tXX_spls);
                sql.Append(" set nzp_dom = " + nzp_dom + ", nzp_ul = " + dom.nzp_ul + ", adr = '" + dom.adr + "' || 'кв.' || " + tXX_spls + ".nkvar, ulica = '" + dom.ulica + "', ndom = '" + dom.ndom + "',");
                sql.Append(" idom = " + Utils.GetInt(dom.ndom) + " ");
                sql.Append(" where nzp_kvar in (select nzp_kvar from tempsells)");
                ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }
            }
            #endregion


            transaction.Commit();
            #region Добавление задачи на Подсчет статистике по дому
            var houses = ClassDBUtils.OpenSQL(string.Format("select max(d.nzp_dom),d.pref,d.nzp_wp from " + Points.Pref + sDataAliasRest + "dom d,tempsells t where d.nzp_dom = t.nzp_dom group by 2,3"),
                conn_db).resultData;
            var calcfon = new CalcFonTask(Points.GetCalcNum(CastValue<string>(houses.Rows[0][1]).Trim()))
            {
                TaskType = CalcFonTask.Types.taskCalcReport,
                Status = FonTask.Statuses.New,
                nzp = CastValue<int>(houses.Rows[0][0]),
                month_ = Points.CalcMonth.month_,
                year_ = Points.CalcMonth.year_,
                pref = CastValue<string>(houses.Rows[0][1]).Trim(),
                nzpt = CastValue<int>(houses.Rows[0][2])
            };
            var dbCalc = new DbCalcQueueClient();
            ret = dbCalc.AddTask(conn_db, null, calcfon);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            ExecSQL(conn_db, "drop table tempsells", false);
            conn_db.Close();
            return ret;
        }

        //----------------------------------------------------------------------
        public void FindUlica(Ulica finder, out Returns ret) //найти и заполнить список улиц
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tXX_spul = "t" + Convert.ToString(finder.nzp_user) + "_spul";

            if (finder.spls.Trim() != "")
            {
                //вызов из создания spls
                IDataReader reader;
                if (ExecRead(conn_web, out reader, " Select * From " + tXX_spul, false).result)
                {
                    //таблицы была создана, ничего не дедаем, выходим
                    conn_web.Close();
                    return;
                }
                reader.Close();
            }
            if (TableInWebCashe(conn_web, tXX_spul))
            {
                ExecSQL(conn_web, " Drop table " + tXX_spul, false);
            }

            //создать таблицу webdata:tXX_spul
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_spul +
                      " ( nzp_ul   integer, " +
                      "   ulica    char(80) " +
                      " ) ", true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //заполнить webdata:tXX_spDom
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

#if PG
            string webdata = conn_web.Database
                ;
#else
            string webdata = conn_web.Database + "@" + DBManager.getServer(conn_web);
#endif

            string tXX_spul_full = webdata + ":" + tXX_spul;

            string whereString;
            if (finder.spls != "")
                whereString = " and u.nzp_ul in ( Select nzp_ul From " + finder.spls + " ) ";
            else
            {
                StringBuilder swhere = new StringBuilder();

                if (finder.nzp_ul > 0)
                {
                    swhere.Append(" and d.nzp_ul = " + finder.nzp_ul.ToString());
                }
                if (finder.ulica != "")
                {
#if PG
                    swhere.Append(" and upper(ulica) SIMILAR TO upper( '" + finder.ulica + "*')");
#else
                    swhere.Append(" and upper(ulica) matches upper( '" + finder.ulica + "*')");
#endif

                }

                whereString = swhere.ToString();
            }

            StringBuilder sql = new StringBuilder();

            sql.Append(" Insert into " + tXX_spul_full + " (nzp_ul,ulica) ");
#if PG
            sql.Append(" Select nzp_ul, trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) ");
            sql.Append(" From " + Points.Pref + "_data.s_ulica u left outer join " + Points.Pref + "_data.s_rajon r ");
#else
            sql.Append(" Select nzp_ul, trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) ");
            sql.Append(" From " + Points.Pref + "_data:s_ulica u, outer " + Points.Pref + "_data:s_rajon r ");
#endif

#if PG
            sql.Append(" on u.nzp_raj=r.nzp_raj ");
#else
            sql.Append(" Where u.nzp_raj=r.nzp_raj ");

#endif
            sql.Append(whereString);

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            conn_db.Close(); //закрыть соединение с основной базой

            //далее работаем с кешем
            //создаем индексы на tXX_spDom
            string ix = "ix" + Convert.ToString(finder.nzp_user) + "_spul";

            ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_spul + " (nzp_ul) ", true);
            if (!ret.result)
            {
#if PG
                ret = ExecSQL(conn_web, " analyze  " + tXX_spul, true);
#else
                ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spul, true);
#endif

            }

            conn_web.Close();
            return;
        }//FindUlica

        //----------------------------------------------------------------------
        public ReturnsObjectType<List<Ulica>> LoadUlica(Ulica finder, IDbConnection connectionID)
        //----------------------------------------------------------------------
        {
            string whereString = "";
            string fromString = "";
            string town = "";

            string newUlica = String.IsNullOrEmpty(finder.ulica) ? "" : Utils.EStrNull(finder.ulica).Replace("'", "").Trim();
            var tables = new DbTables(connectionID);
            if (newUlica != "")
            {
                whereString = " and  upper(u.ulica) LIKE upper( '%" + newUlica + "%') ";

            }
            if ((finder.nzp_area > 0 || (finder.list_nzp_area.Count > 0)) || ((finder.list_nzp_wp.Count > 0)))
            {
                fromString = Points.Pref + DBManager.sDataAliasRest + "dom d, ";

                whereString += " and d.nzp_ul = u.nzp_ul ";


                if (finder.list_nzp_area.Count > 0)
                {
                    string str = "";
                    for (int i = 0; i < finder.list_nzp_area.Count; i++)
                    {
                        if (finder.list_nzp_area[i] != 0)
                        {
                            if (String.IsNullOrEmpty(str)) str += finder.list_nzp_area[i];
                            else str += ", " + finder.list_nzp_area[i];
                        }
                    }

                    if (!String.IsNullOrEmpty(str))
                    {
                        if (str.IndexOf(',') < 0)
                            whereString += " and d.nzp_area = " + str;
                        else
                            whereString += " and d.nzp_area in (" + str + ")";
                    }
                }
                else if (finder.nzp_area > 0)
                {
                    whereString += " and d.nzp_area = " + finder.nzp_area;
                }

                if (finder.list_nzp_wp.Count > 0)
                {
                    string str = "";
                    for (int i = 0; i < finder.list_nzp_wp.Count; i++)
                    {
                        if (String.IsNullOrEmpty(str)) str += finder.list_nzp_wp[i];
                        else str += ", " + finder.list_nzp_wp[i];
                    }
                    if (str.IndexOf(',') < 0)
                        whereString += " and d.nzp_wp = " + str;
                    else
                        whereString += " and d.nzp_wp in (" + str + ")";

                    MyDataReader reader;
                    string sqlt = "select distinct t.nzp_town from " + tables.town + " t, " + tables.dom + " d where d.nzp_wp in (" + str + ") and t.nzp_town = d.nzp_town";
                    if (ExecRead(out reader, sqlt).result)
                    {
                        while (reader.Read())
                        {
                            if (town == "") town += reader["nzp_town"].ToString();
                            else town += ", " + reader["nzp_town"].ToString();
                        }
                    }
                }
            }

            string whereRaj = String.Empty;
            string whereTown = String.Empty;
            //Фильтр по населенному пункту
            if (finder.nzp_town > 0)
            {
                whereRaj = string.Format("and t.nzp_town = {0}", finder.nzp_town);
            }

            //Фильтр по населенному пункту
            if (finder.nzp_raj > 0)
            {
                whereRaj = string.Format(" and r.nzp_raj = {0}", finder.nzp_raj);
            }
            if (!string.IsNullOrEmpty(finder.nzp_rajs))
            {
                whereRaj = string.Format(" and r.nzp_raj IN ({0})", finder.nzp_rajs);
            }



            ExecSQL(connectionID, " Drop table sqlt1", false);


            string sqlt1 = " Create temp table sqlt1 (" +
                           " nzp_raj integer, " +
                           " rajon char(30), " +
                           " town char(30), " +
                           " nzp_town integer) " + DBManager.sUnlogTempTable;
            if (!ExecSQL(connectionID, sqlt1, true).result)
            {
                connectionID.Close();
                return null;
            }

            sqlt1 = " insert into sqlt1(rajon, nzp_town, town, nzp_raj)" +
                    " Select r.rajon, r.nzp_town, t.town, r.nzp_raj " +
                    " from " + tables.rajon + " r, " + tables.town + " t " +
                    "  Where r.nzp_town = t.nzp_town " + whereRaj + whereTown;
            if (town != "") sqlt1 += " and r.nzp_town in (" + town + ")";
            if (!ExecSQL(connectionID, sqlt1, true).result)
            {
                connectionID.Close();
                return null;
            }
            ExecSQL(connectionID, "create index ix_tmpr_01 on sqlt1(nzp_raj)", true);
            ExecSQL(connectionID, DBManager.sUpdStat + " sqlt1", true);

            sqlt1 = " Select count(*) " +
                   " from " + tables.ulica + " u, " + fromString + " sqlt1  r " +
                   " Where u.nzp_raj = r.nzp_raj " + whereString;
            Returns ret;
            object count = ExecScalar(connectionID, sqlt1, out ret, true);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadUlica " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            sqlt1 = " Select distinct u.nzp_ul, u.ulica, u.ulicareg, r.rajon, r.nzp_town, r.nzp_raj, town " +
                    " from " + tables.ulica + " u, " + fromString + " sqlt1  r " +
                    " Where u.nzp_raj = r.nzp_raj " + whereString +
                    " order by ulica, ulicareg, rajon, town";
            if (!ExecSQL(connectionID, sqlt1, true).result)
            {
                connectionID.Close();
                return null;
            }


            DataTable dt = ClassDBUtils.OpenSQL(sqlt1, connectionID).GetData();

            ExecSQL(connectionID, " Drop table sqlt1", false);
            List<Ulica> ulicaList = Utility.OrmConvert.ConvertDataRows(dt.Rows, DbAdresClient.ToUlicaValue);

            if (ulicaList != null)
            {
                if (finder.skip > 0 && ulicaList.Count > finder.skip) ulicaList.RemoveRange(0, finder.skip);
                if (finder.rows > 0 && ulicaList.Count > finder.rows) ulicaList.RemoveRange(finder.rows, ulicaList.Count - finder.rows);
                return new ReturnsObjectType<List<Ulica>>(ulicaList) { tag = recordsTotalCount };
            }
            return new ReturnsObjectType<List<Ulica>> { tag = 0 };


        }

        public List<_Geu> LoadGeu(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            Geus spis = new Geus();
            spis.GeuList.Clear();

            string where = "";

            if (finder.dopFind != null && finder.dopFind.Count > 0)
                where += " and upper(geu) like '%" + finder.dopFind[0].ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_geu) where += " and nzp_geu in (" + role.val + ")";

            if (finder.pref.Trim() == "") finder.pref = Points.Pref;
            string wherePoint = "";
            if (finder.dopPointList != null)
            {
                if (finder.dopPointList.Count > 0)
                {
                    string str = "";
                    for (int i = 0; i < finder.dopPointList.Count; i++)
                    {
                        if (i == 0) str += finder.dopPointList[i];
                        else str += ", " + finder.dopPointList[i];
                    }
                    if (str != "")
                    {

                        wherePoint += " AND nzp_geu IN (	SELECT DISTINCT		K .nzp_geu	FROM "
                            + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k where k. nzp_wp  IN (" + str + ")) ";
                    }
                }
            }

            //Определить общее количество записей
            string sql = "Select count(*) From " + finder.pref + "_data" + tableDelimiter + "s_geu Where 1 = 1 " + where;

            object count = ExecScalar(sql, out ret);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadGeu " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            //выбрать список
            MyDataReader reader;
            if (!ExecRead(out reader,
                " Select nzp_geu, geu From " + finder.pref + "_data" + tableDelimiter + "s_geu Where 1 = 1 " + where + wherePoint + " Order by geu").result)
            {
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    _Geu zap = new _Geu();

                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

                    spis.GeuList.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                return spis.GeuList;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения справочника отделений " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
            }
        }

        //----------------------------------------------------------------------
        public Returns WebArea()
        //----------------------------------------------------------------------
        {
            return WebArea(0, true);
        }
        //----------------------------------------------------------------------
        public Returns WebArea(int nzp_server, bool is_insert)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string s_area = "s_area" + srv;
#if PG
            string s_area_full = pgDefaultDb + "." + s_area;
#else
            string s_area_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + s_area;
#endif

            using (DbAdresClient db = new DbAdresClient())
            {
                db.CreateWebArea(conn_web, s_area, out ret);
            }
            conn_web.Close();

            if (is_insert)
            {
                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                //выбрать список
#if PG
                ret = ExecSQL(conn_db,
                    " Insert into " + s_area_full + " (nzp_area, area, nzp_supp) " +
                    " Select nzp_area, area, nzp_supp From " + Points.Pref + "_data.s_area", true);
#else
                ret = ExecSQL(conn_db,
                    " Insert into " + s_area_full + " (nzp_area, area, nzp_supp) " +
                    " Select nzp_area, area, nzp_supp From " + Points.Pref + "_data:s_area", true);
#endif


                conn_db.Close();
            }

            return ret;
        }
        //----------------------------------------------------------------------
        public Returns WebGeu()
        //----------------------------------------------------------------------
        {
            return WebGeu(0, true);
        }
        //----------------------------------------------------------------------
        public Returns WebGeu(int nzp_server, bool is_insert)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string srv = "";
            if (nzp_server > 0)
                srv = "_" + nzp_server;

            string s_geu = "s_geu" + srv;
#if PG
            string s_geu_full = pgDefaultDb + "." + s_geu;
#else
            string s_geu_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + s_geu;
#endif


            using (DbAdresClient db = new DbAdresClient())
            {
                db.CreateWebGeu(conn_web, s_geu, out ret);
            }
            conn_web.Close();

            if (is_insert)
            {
                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                //выбрать список
#if PG
                ret = ExecSQL(conn_db,
                    " Insert into " + s_geu_full + " (nzp_geu, geu) " +
                    " Select nzp_geu, geu From " + Points.Pref + "_data.s_geu", true);
#else
                ret = ExecSQL(conn_db,
                    " Insert into " + s_geu_full + " (nzp_geu, geu) " +
                    " Select nzp_geu, geu From " + Points.Pref + "_data:s_geu", true);
#endif


                conn_db.Close();
            }

            return ret;
        }




        //обновлние АП
        //----------------------------------------------------------------------
        bool DropRefresh(IDbConnection conn_db)
        //----------------------------------------------------------------------
        {
            ExecSQL(conn_db, "Drop table t_sost_ls", false);
            ExecSQL(conn_db, "Drop table ttt_all", false);
            ExecSQL(conn_db, "Drop table ttt_kvar", false);
            ExecSQL(conn_db, "Drop table ttt_all_dom", false);
            ExecSQL(conn_db, "Drop table ttt_dom", false);
            ExecSQL(conn_db, "Drop table ttt_area", false);
            ExecSQL(conn_db, "Drop table ttt_geu", false);
            ExecSQL(conn_db, "Drop table ttt_ulica", false);
            ExecSQL(conn_db, "Drop table ttt_land", false);
            ExecSQL(conn_db, "Drop table ttt_stat", false);
            ExecSQL(conn_db, "Drop table ttt_town", false);
            ExecSQL(conn_db, "Drop table ttt_rajon", false);
            ExecSQL(conn_db, "Drop table ttt_rajondom", false);
            return false;
        }

        public Returns RefreshAP(Finder finder)
        {
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                return new Returns(false, "Функция обновления адресного пространства недоступна, т.к. установлен режим работы с центральным банком данных", -1);
            }

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            Returns ret2;
            foreach (_Point point in Points.PointList)
            {
                RefreshAP(conn_db, point.pref, out ret2);
                if (!ret2.result)
                {
                    if (ret2.tag >= 0) return ret2;
                    else
                    {
                        ret.text += (ret.text != "" ? ", " : "") + ret2.text;
                        ret.tag = ret2.tag;
                    }
                }
            }

            conn_db.Close();

            if (ret.text != "") ret.text = "Обновление адресного пространства прошло с предупреждениями: " + ret.text;
            return ret;
        }

        //----------------------------------------------------------------------
        public bool RefreshAP(IDbConnection conn_db, string pref, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            DropRefresh(conn_db);
            string ol_srv = "@" + DBManager.getServer(conn_db);
            string sql;

            DbTables tables = new DbTables(conn_db);

#if PG
            string local_kvar = pref + "_data.kvar";
            string local_dom = pref + "_data.dom";
            string s_point = Points.Pref + "_kernel.s_point";
#else
            string local_kvar = pref + "_data@" + DBManager.getServer(conn_db) + ":kvar";
            string local_dom = pref + "_data@" + DBManager.getServer(conn_db) + ":dom";
            string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
#endif


            if (!TempTableInWebCashe(conn_db, local_kvar))
            {
                ret.result = false;
                ret.text = "Банк данных \"" + Points.GetPoint(pref).point + "\" не доступен";
                ret.tag = -1;
                return false;
            }

            if (Points.Pref != pref)
            {
                //лицевые счета
#if PG
                sql =
                        " Select nzp_kvar, num_ls, nzp_dom  Into temp ttt_all From " + local_kvar;
                /*" Where nzp_kvar in " +
                    " ( Select nzp From " + pref + "_data" + ol_srv + ".prm_3 " +
                    "   Where nzp_prm = 51 and today between dat_s and dat_po"+
                        "  and is_actual = 1 " +
                        "  and trim(coalesce(val_prm,'0')) in ('1','2') " +
                    " ) " +*/
                ;
#else
                sql =
                                    " Select nzp_kvar, num_ls, nzp_dom From " + local_kvar +
                    /*" Where nzp_kvar in " +
                        " ( Select nzp From " + pref + "_data" + ol_srv + ":prm_3 " +
                        "   Where nzp_prm = 51 and today between dat_s and dat_po"+
                            "  and is_actual = 1 " +
                            "  and trim(nvl(val_prm,'0')) in ('1','2') " +
                        " ) " +*/
                                    " Into temp ttt_all with no log ";
#endif


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                ret = ExecSQL(conn_db, "Create index inx_tall_1 on ttt_all (nzp_kvar)", true);
                ret = ExecSQL(conn_db, "Create index inx_tall_2 on ttt_all (num_ls)", true);
                ret = ExecSQL(conn_db, "Create index inx_tall_3 on ttt_all (nzp_dom)", true);
#if PG
                ret = ExecSQL(conn_db, "analyze ttt_all", true);
#else
                ret = ExecSQL(conn_db, "Update statistics for table ttt_all", true);
#endif


#if PG
                sql =
                    " Select a.nzp_kvar,nzp_area,nzp_geu,a.nzp_dom,nkvar,nkvar_n,a.num_ls,fio,ikvar " +
                     " Into temp ttt_kvar " +
                    //" From " + local_kvar + " a, ttt_all b " +
                    //" Where a.nzp_kvar = b.nzp_kvar " +
                    " From " + local_kvar + " a " +
                    " Where 1=1 " +
                    "   and a.num_ls > 0 " +
                    "   and  not exists ( Select 1 From " + tables.kvar + " c where c.nzp_kvar=a.nzp_kvar) ";
#else
                sql =
                    " Select a.nzp_kvar,nzp_area,nzp_geu,a.nzp_dom,nkvar,nkvar_n,a.num_ls,fio,ikvar " +
                    " From " + local_kvar + " a, ttt_all b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and a.num_ls > 0 " +
                    "   and a.nzp_kvar not in ( Select nzp_kvar From " + tables.kvar + ") " +
                    " Into temp ttt_kvar with no log ";
#endif


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.kvar +
                    " (nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar) " +
                    " Select nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar " +
                    " From ttt_kvar ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

#if PG
                //sql = " Update " + tables.kvar +
                //    " Set pref = '" + pref + "'" +
                //        ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
                //       ", is_open = coalesce(( Select max(val_prm) From " + pref + "_data.prm_3 " +
                //     " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and current_date between dat_s and dat_po and is_actual = 1)::int, " + Ls.States.Undefined.GetHashCode() + ") " +
                //    " Where exists ( Select 1 From ttt_all t where " + tables.kvar + ".nzp_kvar=t.nzp_kvar )";

                // Обновляем префиксы
                sql = " Update " + tables.kvar + " a Set pref = '" + pref + "' from " + local_kvar + " b  Where a.nzp_kvar=b.nzp_kvar and (a.pref is null OR a.pref<>" + "'" + pref + "')";
                ret = ExecSQL(conn_db, sql, true);
                // Получаем nzp_wp для дальнейшего update
                if (!ret.result) return DropRefresh(conn_db);
                sql = "select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "'";
                object nzp_wp = ExecScalar(conn_db, sql, out ret, false);
                int parsed_nzp_wp;
                if (!int.TryParse(nzp_wp.ToString(), out parsed_nzp_wp))
                {
                    MonitorLog.WriteLog("Неудачное преобразование object nzp_wp в тип int в методе RefreshAP()", MonitorLog.typelog.Error, true);
                    return DropRefresh(conn_db);
                }
                // Обновляем nzp_wp
                if (!ret.result) return DropRefresh(conn_db);
                sql = " Update " + tables.kvar + " a Set nzp_wp=" + parsed_nzp_wp + " from " + local_kvar + " b  where a.nzp_kvar=b.nzp_kvar and (nzp_wp is null  OR nzp_wp<>" + parsed_nzp_wp + ")";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                // Собираем состояния лицевых счетов в одну таблицу
                sql = "Select nzp_kvar, coalesce(val_prm::int, " + Ls.States.Undefined.GetHashCode() + ") as is_open into temp t_sost_ls " +
                       "From " + pref + "_data.prm_3 p, " + tables.kvar + " k Where k.nzp_kvar = p.nzp " +
                       "and pref ='" + pref + "' and nzp_prm=51 " +
                       "and current_date between dat_s " +
                       "and dat_po and is_actual=1 ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) 
                    return DropRefresh(conn_db);
                
                //если мусор в данных, то состояние ЛС - неопределено
                sql = "update t_sost_ls set is_open = " + Ls.States.Undefined.GetHashCode() + " where is_open not in (" + Ls.States.Open.GetHashCode() +
                    "," + Ls.States.Closed.GetHashCode() + "," + Ls.States.Undefined.GetHashCode() + ")";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return DropRefresh(conn_db);

                // обновление состояний лицевых счетов
                sql = "update " + tables.kvar + " k set is_open= tl.is_open " +
                      "from t_sost_ls tl where k.nzp_kvar=tl.nzp_kvar and (k.is_open is null OR k.is_open<>cast(tl.is_open as char(1)))";
                //ret= portionUpdate(conn_db, sql,  Convert.ToInt32(o));
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) 
                    return DropRefresh(conn_db);
                //ret = ExecSQL(conn_db, sql, true);
                //if (!ret.result) return DropRefresh(conn_db);

#else
                sql = " Update " + tables.kvar +
                    " Set pref = '" + pref + "'" +
                        ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
                        ", is_open = nvl(( Select max(val_prm) From " + pref + "_data" + ol_srv + ":prm_3 " +
                                    " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and today between dat_s and dat_po and is_actual = 1), " + Ls.States.Undefined.GetHashCode() + ") " +
                    " Where nzp_kvar in ( Select nzp_kvar From ttt_all )";
#endif



                //дома
#if PG
                sql =
                    " Select nzp_dom Into temp ttt_all_dom From " + local_dom;
#else
                sql =
                    " Select nzp_dom From " + local_dom +
                    " Into temp ttt_all_dom with no log ";
#endif


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

#if PG
                sql =
                    " Select nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
                    "        idom,ndom,nkor,indecs,nzp_bh,kod_uch Into temp ttt_dom From " + local_dom + " ld " +
                       " Where not exists        " +
                    " ( Select nzp_dom From " + tables.dom + " td where td.nzp_dom=ld.nzp_dom ) ";
                //" Where nzp_dom not in        " +
                //" ( Select nzp_dom From " + tables.dom + ") ";
#else
                sql =
                                    " Select nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
                                    "        idom,ndom,nkor,indecs,nzp_bh,kod_uch From " + local_dom +
                                    " Where nzp_dom not in        " +
                                    " ( Select nzp_dom From " + tables.dom + ") " +
                                    " Into temp ttt_dom with no log ";
#endif


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.dom +
                    " (nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
                    "        idom,ndom,nkor,indecs,nzp_bh,kod_uch ) " +
                    " Select nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
                    "        idom,ndom,nkor,indecs,nzp_bh,kod_uch From ttt_dom ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Update " + tables.dom +
                    " Set pref = '" + pref + "'" +
                    ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
                    " Where exists ( Select 1 From ttt_all_dom td where " + tables.dom + ".nzp_dom=td.nzp_dom  )";
                //" Where nzp_dom in ( Select nzp_dom From ttt_all_dom )";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //area
#if PG
                sql =
                    " Select nzp_area,area, nzp_payer Into temp ttt_area From " + pref + "_data.s_area la " +
                     " Where not exists  " +
                    " ( Select 1 From " + tables.area + " ta where la.nzp_area=ta.nzp_area) ";
                //" Where nzp_area not in " +
                //" ( Select nzp_area From " + tables.area + ") ";
#else
                sql =
                                   " Select nzp_area,area From " + pref + "_data" + ol_srv + ":s_area " +
                                   " Where nzp_area not in " +
                                   " ( Select nzp_area From " + tables.area + ") " +
                                   " Into temp ttt_area with no log ";
#endif


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.area + " (nzp_area,area, nzp_payer) " +
                    " Select nzp_area,area, nzp_payer From ttt_area ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //geu
#if PG
                sql =
                    " Select nzp_geu,geu Into temp ttt_geu From " + pref + "_data.s_geu  lg" +
                    " where not exists ( Select 1 From " + tables.geu + " tg where tg.nzp_geu=lg.nzp_geu) ";
                //" Where nzp_geu not in " +
                //" ( Select nzp_geu From " + tables.geu + " ) ";
#else
                sql =
                                   " Select nzp_geu,geu From " + pref + "_data" + ol_srv + ":s_geu " +
                                   " Where nzp_geu not in " +
                                   " ( Select nzp_geu From " + tables.geu + " ) " +
                                   " Into temp ttt_geu with no log ";
#endif


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.geu + " (nzp_geu,geu) " +
                    " Select nzp_geu,geu From ttt_geu ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //ulica
#if PG
                sql =
                    " Select nzp_ul,ulica,nzp_raj Into temp ttt_ulica From " + pref + "_data.s_ulica lu " +
                    "where not exists (select 1 from " + tables.ulica + " tu where lu.nzp_ul=tu.nzp_ul)";
                //" Where nzp_ul not in " +
                //" ( Select nzp_ul From " + tables.ulica + " ) ";
#else
                sql =
                                    " Select nzp_ul,ulica,nzp_raj From " + pref + "_data" + ol_srv + ":s_ulica " +
                                    " Where nzp_ul not in " +
                                    " ( Select nzp_ul From " + tables.ulica + " ) " +
                                    " Into temp ttt_ulica with no log ";
#endif


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.ulica + " (nzp_ul,ulica,nzp_raj) " +
                    " Select nzp_ul,ulica,nzp_raj From ttt_ulica ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //kvar
#if PG
                // можно было бы обновлять все разом (как это было раньше), но в Postges выполняется очень долго.
                // поэтому пришлось разбить на маленькие запросы в цикле
                string[] columns =
                {
                    "nzp_area", "nzp_geu", "nzp_dom", "num_ls", "nkvar", "nkvar_n", "porch",
                    "phone", "uch", "ikvar", "fio", "pkod", "pkod10", "typek", "remark"
                };
                for (int i = 0; i < columns.Length; i++)
                {
                    sql = "update " + tables.kvar + " t set " + columns[i] + "= k." + columns[i] + " From " + local_kvar + " k " +
                          " where t.nzp_kvar= k.nzp_kvar " +
                          "and ( (" + "t." + columns[i] + " is null and k." + columns[i] + " is not null) OR " + "t." + columns[i] + "<> k." + columns[i] + ")";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return DropRefresh(conn_db);
                }
                //sql = "Update " + tables.kvar + " Set nzp_area = k.nzp_area, nzp_geu = k.nzp_geu, nzp_dom = k.nzp_dom, " +
                //      " num_ls=k.num_ls, nkvar=k.nkvar, nkvar_n=k.nkvar_n, porch=k.porch, phone=k.phone, uch=k.uch, " +
                //      " ikvar=k.ikvar, fio=k.fio, pkod=k.pkod, pkod10=k.pkod10, typek=k.typek, remark=k.remark " +
                //      " From " + local_kvar + " k " +
                //      " Where " + tables.kvar + ".nzp_kvar = k.nzp_kvar and exists (Select 1 from ttt_all t where  "+tables.kvar + ".nzp_kvar =t.nzp_kvar )"; 
                // + tables.kvar + ".nzp_kvar in ( Select nzp_kvar From ttt_all )";
#else
                sql =
                                    " Update " + tables.kvar +
                                    " Set (nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark) =" +
                                    " ((Select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where " + tables.kvar + ".nzp_kvar = k.nzp_kvar ))" +
                                    " Where nzp_kvar in ( Select nzp_kvar From ttt_all ) ";
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //dom
#if PG
                sql = " Update " + tables.dom +
                      " Set nzp_area = d.nzp_area, nzp_geu = d.nzp_geu, nzp_bh = d.nzp_bh, nzp_ul=d.nzp_ul, ndom=d.ndom, nkor=d.nkor " +
                      " From " + local_dom + " d " +
                      " Where " + tables.dom + ".nzp_dom=d.nzp_dom and exists (Select 1 from ttt_all_dom t where " +
                      tables.dom + ".nzp_dom=t.nzp_dom )";
                //"" + tables.dom + ".nzp_dom in ( Select nzp_dom From ttt_all_dom ) ";
#else
                sql =
                                   " Update " + tables.dom +
                                   " Set (nzp_area, nzp_geu, nzp_geu, nzp_bh, nzp_ul, ndom, nkor) = ((Select nzp_area, nzp_geu, nzp_geu, nzp_bh, nzp_ul, ndom, nkor From " + local_dom + " d Where " + tables.dom + ".nzp_dom=d.nzp_dom))" +
                                   " Where nzp_dom in ( Select nzp_dom From ttt_all_dom ) ";
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //land
#if PG
                sql =
                    " Select nzp_land, land, land_t, soato Into temp ttt_land From " + pref + "_data.s_land  tl" +
                    " where not exists (Select 1 from " + tables.land + " l where l.nzp_land=tl.nzp_land )";
                //" Where nzp_land not in " +
                //" ( Select nzp_land From " + tables.land + ") ";
#else
                sql =
                    " Select nzp_land, land, land_t, soato From " + pref + "_data" + ol_srv + ":s_land " +
                    " Where nzp_land not in " +
                    " ( Select nzp_land From " + tables.land + ") " +
                    " Into temp ttt_land with no log ";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //если мусор в данных
                sql = "delete from ttt_land t where exists (select 1 from " + tables.land + " tt where t.land = tt.land)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.land + " ( nzp_land, land, land_t, soato) " +
                    " Select  nzp_land, land, land_t, soato From ttt_land " +
                    " WHERE NOT EXISTS (SELECT 1 FROM " + tables.land + " l WHERE l.nzp_land = ttt_land.nzp_land)";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //stat
#if PG
                sql =
                    " Select nzp_stat, nzp_land, stat, stat_t, soato Into temp ttt_stat From " + pref + "_data.s_stat ls " +
                    "where not exists (select 1 from " + tables.stat + " ts where ts.nzp_stat=ls.nzp_stat )";
                //" Where nzp_stat not in " +
                //" ( Select nzp_stat From " + tables.stat + ") ";
#else
                sql =
                    " Select nzp_stat, nzp_land, stat, stat_t, soato From " + pref + "_data" + ol_srv + ":s_stat " +
                    " Where nzp_stat not in " +
                    " ( Select nzp_stat From " + tables.stat + ") " +
                    " Into temp ttt_stat with no log ";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.stat + " (nzp_stat, nzp_land, stat, stat_t, soato) " +
                    " Select nzp_stat, nzp_land, stat, stat_t, soato From ttt_stat ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //town
#if PG
                sql =
                    " Select nzp_town, nzp_stat, town, town_t, soato Into temp ttt_town From " + pref + "_data.s_town lt " +
                    "where not exists (select 1 from " + tables.town + " tt where tt.nzp_town=lt.nzp_town)";
                //" Where nzp_town not in " +
                //" ( Select nzp_town From " + tables.town + ") ";
#else
                sql =
                    " Select nzp_town, nzp_stat, town, town_t, soato From " + pref + "_data" + ol_srv + ":s_town " +
                    " Where nzp_town not in " +
                    " ( Select nzp_town From " + tables.town + ") " +
                    " Into temp ttt_town with no log ";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.town + " (nzp_town, nzp_stat, town, town_t, soato) " +
                    " Select nzp_town, nzp_stat, town, town_t, soato From ttt_town ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //s_rajon
#if PG
                sql =
                    " Select nzp_raj, nzp_town, rajon, rajon_t, soato  Into unlogged ttt_rajon " +
                     " From " + pref + "_data.s_rajon " +
                    " Where not  exists " +
                    " ( Select nzp_raj From " + tables.rajon + " r where r.nzp_raj = nzp_raj) ";
#else
                sql =
                    " Select nzp_raj, nzp_town, rajon, rajon_t, soato From " + pref + "_data" + ol_srv + ":s_rajon " +
                    " Where nzp_raj not in " +
                    " ( Select nzp_raj From " + tables.rajon + ") " +
                    " Into temp ttt_rajon with no log ";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Insert into " + tables.rajon + " (nzp_raj, nzp_town, rajon, rajon_t, soato) " +
                    " Select nzp_raj, nzp_town, rajon, rajon_t, soato From ttt_rajon ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                //s_rajondom
#if PG
                string local_rajondom = pref + "_data.s_rajon_dom";
#else
                string local_rajondom = pref + "_data" + ol_srv + ":s_rajon_dom";
#endif
                if (TempTableInWebCashe(conn_db, local_rajondom) && TempTableInWebCashe(conn_db, tables.rajon_dom))
                {
#if PG
                    sql =
                        " Select nzp_raj_dom, rajon_dom, alt_rajon_dom Into temp ttt_rajondom From " + local_rajondom + " lr " +
                        " where not exists (select 1 from " + tables.rajon_dom + " tr where tr.nzp_raj_dom=lr.nzp_raj_dom)";
                    //" Where nzp_raj_dom not in " +
                    //" ( Select nzp_raj_dom From " + tables.rajon_dom + ") ";
#else
                    sql =
                        " Select nzp_raj_dom, rajon_dom, alt_rajon_dom From " + local_rajondom +
                        " Where nzp_raj_dom not in " +
                        " ( Select nzp_raj_dom From " + tables.rajon_dom + ") " +
                        " Into temp ttt_rajondom with no log ";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return DropRefresh(conn_db);

                    sql =
                        " Insert into " + tables.rajon_dom + " (nzp_raj_dom, rajon_dom, alt_rajon_dom) " +
                        " Select nzp_raj_dom, rajon_dom, alt_rajon_dom From ttt_rajondom ";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return DropRefresh(conn_db);
                }

            }
            else
            {
#if PG
                sql = " Update " + tables.kvar +
                    " Set pref = '" + pref + "'" +
                        ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
                        ", is_open = coalesce(( Select max(val_prm) From " + pref + "_data.prm_3 " +
                                    " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and current_date between dat_s and dat_po and is_actual = 1)::int, " + Ls.States.Undefined.GetHashCode() + ") ";
#else
                sql = " Update " + tables.kvar +
                    " Set pref = '" + pref + "'" +
                        ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
                        ", is_open = nvl(( Select max(val_prm) From " + pref + "_data" + ol_srv + ":prm_3 " +
                                    " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and today between dat_s and dat_po and is_actual = 1), " + Ls.States.Undefined.GetHashCode() + ") ";
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return DropRefresh(conn_db);

                sql =
                    " Update " + tables.dom +
                    " Set pref = '" + pref + "'" +
                    ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')";
            }

            //supplier_codes
            if (DBManager.TableInBase(conn_db, null, Points.Pref + "_data", "supplier_codes"))
            {
                string suppCodesLocal = DBManager.GetFullBaseName(conn_db, pref + "_data", "supplier_codes");
                string suppCodesCentral = DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "supplier_codes");

                ret = ExecSQL(conn_db, String.Format("Delete from {0} where nzp_kvar in (select nzp_kvar from " + local_kvar + ")", suppCodesCentral), true);
                if (!ret.result) return DropRefresh(conn_db);

                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendFormat(" Insert into {0} ", suppCodesCentral);
                sqlBuilder.AppendFormat(" select * from {0} ", suppCodesLocal);
                ret = ExecSQL(conn_db, sqlBuilder.ToString(), true);
                if (!ret.result) return DropRefresh(conn_db);
            }

            DropRefresh(conn_db);
            return true;
        }


        public Returns RefreshKvar(IDbConnection conn_db, IDbTransaction transaction, Ls finder)
        {
            if (finder.pref == "") return new Returns(false, "Не задан префикс БД");

            DbTables tables = new DbTables(conn_db);
#if PG
            string local_kvar = finder.pref + "_data.kvar";
            string s_point = Points.Pref + "_kernel.s_point";
            string prm_3 = finder.pref + "_data.prm_3";
#else
            string local_kvar = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":kvar";
            string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
            string prm_3 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_3";
#endif


            string sql = "select nzp_kvar from " + tables.kvar + " where nzp_kvar = " + finder.nzp_kvar;
            IDataReader reader;
            Returns ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) return ret;

            if (!reader.Read())
            {
                int code = GetAreaCodes(conn_db, transaction, finder, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка получения текущего area_codes.code", MonitorLog.typelog.Error, true);
                }
                string cd = "0";
                if (code > 0) cd = code.ToString();

                sql =
                 " Insert into " + tables.kvar +
                 " (nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar,area_code) " +
                 " Select nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar, " + cd +
                 " From " + local_kvar + " where nzp_kvar = " + finder.nzp_kvar;

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
            }
            reader.Close();
            reader.Dispose();

            sql = " Update " + tables.kvar +
             " Set pref = '" + finder.pref + "'" +
                ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + finder.pref + "')" +
             " Where nzp_kvar =" + finder.nzp_kvar;

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;

#if PG
            sql = " Update " + tables.kvar +
             " Set is_open = coalesce(" + ("( Select max(val_prm) From " + prm_3 +
                             " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and current_date between dat_s and dat_po and is_actual = 1)").CastTo("INTEGER") + ", " + Ls.States.Undefined.GetHashCode() + ") " +
             " Where nzp_kvar =" + finder.nzp_kvar;
#else
            sql = " Update " + tables.kvar +
             " Set is_open = nvl(( Select max(val_prm) From " + prm_3 +
                             " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and today between dat_s and dat_po and is_actual = 1), " + Ls.States.Undefined.GetHashCode() + ") " +
             " Where nzp_kvar =" + finder.nzp_kvar;
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;

#if PG
            sql = " Update " + tables.kvar;
            sql = sql.UpdateSet(
                                "nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark",
                "nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark", "sub");
            sql += " from ( select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where k.nzp_kvar = " + finder.nzp_kvar + ") as sub" + " Where nzp_kvar = " + finder.nzp_kvar;
#else
            sql = " Update " + tables.kvar +
             " Set (nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark) =" +
             " ((Select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where " + tables.kvar + ".nzp_kvar = k.nzp_kvar ))" +
             " Where nzp_kvar = " + finder.nzp_kvar;
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            return ret;
        }

        public Returns RefreshDom(IDbConnection conn_db, Dom finder)
        {
            if (finder.pref == "") return new Returns(false, "Не задан префикс БД");

            DbTables tables = new DbTables(conn_db);
#if PG
            string local_dom = finder.pref + "_data.dom";
            string s_point = Points.Pref + "_kernel.s_point";
#else
            string local_dom = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":dom";
            string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
#endif


            string sql = "select nzp_dom from " + tables.dom + " where nzp_dom = " + finder.nzp_dom;
            IDataReader reader;
            Returns ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return ret;

            if (!reader.Read())
            {
                sql =
                 " Insert into " + tables.dom +
                 " (nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) " +
                 " Select nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj " +
                 " From " + local_dom + " where nzp_dom = " + finder.nzp_dom;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
            }
            reader.Close();
            reader.Dispose();

            sql = " Update " + tables.dom +
             " Set pref = '" + finder.pref + "'" +
                ", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + finder.pref + "')" +
             " Where nzp_dom =" + finder.nzp_dom;

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;


#if PG
            sql = "WITH tt AS " +
                  "(SELECT d.nzp_area, " +
                          "d.nzp_geu, " +
                          "d.nzp_ul, " +
                          "d.idom, " +
                          "d.ndom, " +
                          "d.nkor, " +
                          "d.nzp_land, " +
                          "d.nzp_stat, " +
                          "d.nzp_town, " +
                          "d.nzp_raj " +
                   "FROM " + local_dom + " d WHERE d.nzp_dom = " + finder.nzp_dom + ") " +
                   "UPDATE " + tables.dom + " " +
                   "SET nzp_area = tt.nzp_area, " +
                   "nzp_geu = tt.nzp_geu, " +
                   "nzp_ul = tt.nzp_ul, " +
                   "idom = tt.idom, " +
                   "ndom = tt.ndom, " +
                   "nkor = tt.nkor, " +
                   "nzp_land = tt.nzp_land, " +
                   "nzp_stat = tt.nzp_stat, " +
                   "nzp_town = tt.nzp_town, " +
                   "nzp_raj = tt.nzp_raj " +
                   "FROM tt " +
                   "WHERE nzp_dom = " + finder.nzp_dom;
#else
            sql = " Update " + tables.dom +
                " Set (nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) =" +
                " ((Select nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj From " + local_dom + " d Where " + tables.dom + ".nzp_dom = d.nzp_dom ))" +
                " Where nzp_dom = " + finder.nzp_dom;
#endif

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

            sql = "update " + tables.kvar + " k " +
                   " set nzp_area = d.nzp_area, nzp_geu =  d.nzp_geu " +
                   " from " + tables.dom + " d " +
                   " where k.nzp_dom = d.nzp_dom and k.nzp_dom = " + finder.nzp_dom;
            ret = ExecSQL(conn_db, sql, true);

            return ret;
        }



        public ReturnsObjectType<Ls> GetLsLocation(Ls finder, IDbConnection connection)
        {
            var wk = new WorkTempKvar();
            var result = wk.GetLsLocation(finder, connection, null);
            wk.Close();
            return result;
        }

        public Returns DeleteLs(Ls finder)
        {
            var wk = new WorkTempKvar();
            var result = wk.DeleteLs(finder);
            wk.Close();
            return result;
        }


        //----------------------------------------------------------------------
        public List<Ulica> UlicaLoad(Ulica finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.pref == "") finder.pref = Points.Pref;
            List<Ulica> spis = new List<Ulica>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //DbTables tables = new DbTables(conn_db);
            var s_ulica = finder.pref + "_data" + tableDelimiter + "s_ulica";

            // Условия поиска
            string where = "";

            if ((finder.nzp_ul != 0) && (finder.nzp_ul != Constants._ZERO_))
                where += " and s.nzp_ul = " + finder.nzp_ul;

            if ((finder.nzp_raj != 0) && (finder.nzp_raj != Constants._ZERO_))
                where += " and s.nzp_raj = " + finder.nzp_raj;

            if (finder.ulica.Trim() != "")
                where += " and upper(s.ulica) like '%" + finder.ulica.ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

            //if (finder.RolesVal != null)
            //    foreach (_RolesVal role in finder.RolesVal)
            //        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) where += " and s.nzp_supp in (" + role.val + ")";

            //Определить общее количество записей
            string sql = "Select count(*) From " + s_ulica + " s Where 1 = 1 " + where;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка UlicaLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

            //выбрать список
            sql = " Select * " +
                  " From " + s_ulica + " s " +
                  " Where 1 = 1 " + where +
                  " Order by ulica";

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    Ulica zap = new Ulica();
                    zap.num = i.ToString();

                    if (reader["nzp_ul"] != DBNull.Value) zap.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["ulica"] != DBNull.Value) zap.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ulicareg"] != DBNull.Value) zap.ulicareg = Convert.ToString(reader["ulicareg"]).Trim();

                    spis.Add(zap);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                ret.tag = recordsTotalCount;

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка улиц " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Prefer GetPrefer(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Prefer prfr = new Prefer();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //выбрать 
#if PG
            String sql = " Select * " + " From " + Points.Pref + "_data.prefer s ";
#else
            String sql = " Select * " + " From " + Points.Pref + "_data:prefer s ";
#endif


            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                int nzp = 0;
                while (reader.Read())
                {
                    if (Convert.ToString(reader["p_name"]).Trim() == "land_rg") prfr.land = Convert.ToString(reader["p_value"]).Trim();
                    if (Convert.ToString(reader["p_name"]).Trim() == "nzp_land_rg")
                    {
                        if (!Int32.TryParse(Convert.ToString(reader["p_value"]).Trim(), out nzp)) nzp = 0;
                        prfr.nzp_land = nzp;
                    }
                    if (Convert.ToString(reader["p_name"]).Trim() == "stat_rg") prfr.stat = Convert.ToString(reader["p_value"]).Trim();
                    if (Convert.ToString(reader["p_name"]).Trim() == "nzp_stat_rg")
                    {
                        if (!Int32.TryParse(Convert.ToString(reader["p_value"]).Trim(), out nzp)) nzp = 0;
                        prfr.nzp_stat = nzp;
                    }
                    if (Convert.ToString(reader["p_name"]).Trim() == "town_rg") prfr.town = Convert.ToString(reader["p_value"]).Trim();
                    if (Convert.ToString(reader["p_name"]).Trim() == "nzp_town_rg")
                    {
                        if (!Int32.TryParse(Convert.ToString(reader["p_value"]).Trim(), out nzp)) nzp = 0;
                        prfr.nzp_town = nzp;
                    }
                    if (Convert.ToString(reader["p_name"]).Trim() == "rajon_rg") prfr.rajon = Convert.ToString(reader["p_value"]).Trim();
                    if (Convert.ToString(reader["p_name"]).Trim() == "nzp_raj_rg")
                    {
                        if (!Int32.TryParse(Convert.ToString(reader["p_value"]).Trim(), out nzp)) nzp = 0;
                        prfr.nzp_raj = nzp;
                    }
                }
                reader.Close();
                conn_db.Close();
                return prfr;

            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения настройки на район " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        /// <summary>
        /// Создать кэш-таблицу для последних показаний приборов учета
        /// </summary>
        private void CreateTableWebUniquePointAreaGet(IDbConnection conn_web, string tXX_cv, bool onCreate, out Returns ret) //
        {
            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX_cv))
                {
                    ExecSQL(conn_web, " Drop table " + tXX_cv, false);
                }

                //создать таблицу webdata:tXX_cv
#if PG
                ret = ExecSQL(conn_web,
                      " Create table " + tXX_cv + "(" +
                      " nzp_wp   integer," +
                      " nzp_area integer," +
                      " nzp_geu  integer," +
                      " point    CHARACTER(100)," +
                      " area     CHARACTER(40)," +
                      " geu      CHARACTER(60) " +
                      ")", true);
#else
                ret = ExecSQL(conn_web,
                      " Create table " + tXX_cv + "(" +
                      " nzp_wp   integer," +
                      " nzp_area integer," +
                      " nzp_geu  integer," +
                      " point    nchar(100)," +
                      " area     nchar(40)," +
                      " geu      nchar(60) " +
                      ")", true);
#endif


                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_cv + " on " + tXX_cv + " (nzp_wp) ", true);
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_2" + tXX_cv + " on " + tXX_cv + " (nzp_area) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_3" + tXX_cv + " on " + tXX_cv + " (nzp_geu) ", true); }
            }
        }

        /// <summary>
        /// Получить cписок всех уникальных сочетаний банков данных, Управляющая организация, отделений
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Ls> GetUniquePointAreaGeu(Ls finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return null;
            }

            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string from = Points.Pref + DBManager.sDataAliasRest + "kvar k, " +
                (finder.withUk ? Points.Pref + DBManager.sDataAliasRest + "s_area a, " : string.Empty) +
                (finder.withGeu ? Points.Pref + DBManager.sDataAliasRest + "s_geu g, " : string.Empty) +
                (finder.withUchastok ? Points.Pref + DBManager.sKernelAliasRest + "s_point p, " : string.Empty);
            from = from.TrimEnd(',', ' ');

            string where = (finder.withUk ? " k.nzp_area = a.nzp_area and " : string.Empty) +
                           (finder.withGeu ? " k.nzp_geu = g.nzp_geu and " : string.Empty) +
                           (finder.withUchastok ? " k.pref = p.bd_kernel and " : string.Empty);
            where = where.Substring(0, where.Length - 4);

            string sql = " Select distinct " +
                         ((finder.withUk ? " k.nzp_area, a.area, " : string.Empty) +
                         (finder.withGeu ? " k.nzp_geu, g.geu, " : string.Empty) +
                         (finder.withUchastok ? " p.nzp_wp, p.point, " : string.Empty)).TrimEnd(',', ' ') +
                         " From " + from + " Where " + where + " Order by " +
                         ((finder.withUchastok ? " p.point, " : string.Empty) +
                         (finder.withUk ? " a.area, " : string.Empty) +
                         (finder.withGeu ? " g.geu, " : string.Empty)).TrimEnd(',', ' ');

            IDataReader reader;

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                List<Ls> Spis = new List<Ls>();
                Ls zap;

                int i = 0;
                while (reader.Read())
                {
                    zap = new Ls();

                    i++;
                    zap.num = i.ToString();
                    if (finder.withUchastok)
                    {
                        if (reader["nzp_wp"] != DBNull.Value) zap.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                        if (reader["point"] != DBNull.Value) zap.point = Convert.ToString(reader["point"]);
                    }
                    if (finder.withUk)
                    {
                        if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                        if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                    }
                    if (finder.withGeu)
                    {
                        if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                        if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();
                    }

                    Spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                ret.tag = i--;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret = new Returns(false, ex.Message, -1);
                MonitorLog.WriteLog("Ошибка заполнения списка для добавления заданий на формирование платежных документов\n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<Vill> LoadVill(Vill finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            string where = "";
            string sql = "";

            if (finder.nzp_vill > 0)
            {
                where += " and nzp_vill = " + finder.nzp_vill;
            }

            if (finder.vill.Trim() != "")
            {
                where += " and vill like%'" + finder.vill + "'%";
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            IDataReader reader = null;
            List<Vill> list = new List<Vill>();
            try
            {
                DbTables tables = new DbTables(conn_db);

                #region определить количество записей
                sql = "select count(*) from " + tables.vill + " v, " + tables.sr_rajon + " r where r.kod_raj = v.kod_raj " + where;
                object count = ExecScalar(conn_db, sql, out ret, true);
                int recordsTotalCount;
                try { recordsTotalCount = Convert.ToInt32(count); }
                catch (Exception e)
                {
                    ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка LoadVill " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

                sql = "select nzp_vill, vill, rajon " +
                        "from  " + tables.vill + " v, " + tables.sr_rajon + " r where r.kod_raj = v.kod_raj " + where + " order by vill";

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    Vill vill = new Vill();
                    vill.num = i.ToString();
                    if (reader["nzp_vill"] != DBNull.Value) vill.nzp_vill = Convert.ToDecimal(reader["nzp_vill"]);
                    if (reader["vill"] != DBNull.Value) vill.vill = Convert.ToString(reader["vill"]).Trim();

                    if (reader["rajon"] != DBNull.Value) vill.vill += " / " + Convert.ToString(reader["rajon"]).Trim();

                    if (finder.nzp_vill > 0)
                    {
                        sql = "select count(*) from " + tables.rajon_vill + " where nzp_vill = " + vill.nzp_vill;
                        object obj = ExecScalar(conn_db, sql, out ret, true);
                        try { vill.vill += " (Количество населенных пунктов: " + Convert.ToString(obj) + ")"; }
                        catch (Exception e)
                        {
                            ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                            MonitorLog.WriteLog("Ошибка LoadVill " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            return null;
                        }
                    }

                    list.Add(vill);
                    if (finder.rows > 0 && list.Count >= finder.rows) break;
                }
                reader.Close();

                ret.tag = recordsTotalCount;
                return list;
            }
            catch (Exception ex)
            {

                CloseReader(ref reader);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadVill " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<Rajon> LoadVillRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            string where = "";
            string sql = "";

            if (finder.nzp_raj > 0)
            {
                where += " and nzp_raj = " + finder.nzp_raj;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);
            string table_rajon_vill = "";
            if (finder.nzp_vill > 0 && finder.mode == Constants.act_mode_view)
            {
                table_rajon_vill = ", " + tables.rajon_vill + " rv ";
                where += " and rv.nzp_raj = r.nzp_raj and rv.nzp_vill = " + finder.nzp_vill;
            }

            if (finder.nzp_vill > 0 && finder.mode == Constants.act_mode_edit)
            {
                where += " and r.nzp_raj not in (select rvill.nzp_raj from " + tables.rajon_vill + " rvill where rvill.nzp_vill <> " + finder.nzp_vill + ")";
            }

            IDataReader reader = null, reader2 = null;
            List<Rajon> list = new List<Rajon>();
            try
            {

                #region определить количество записей
                sql = "select count(*) from " + tables.rajon + " r, " + tables.town + " t, " + tables.stat + " s " +
                    table_rajon_vill + " where t.nzp_town = r.nzp_town and s.nzp_stat = t.nzp_stat and s.nzp_stat=cast ((select p_value from " + tables.prefer + " where p_name='nzp_stat_rg') as integer) " + where;
                object count = ExecScalar(conn_db, sql, out ret, true);
                int recordsTotalCount;
                try { recordsTotalCount = Convert.ToInt32(count); }
                catch (Exception e)
                {
                    ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка LoadVillRajon " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                #endregion

                sql = "select trim(t.town) || '/' || trim(r.rajon) as rajon, r.nzp_raj " +
                      "from " + tables.rajon + " r, " + tables.town + " t, " + tables.stat + " s " + table_rajon_vill +
                      " where t.nzp_town = r.nzp_town and s.nzp_stat = t.nzp_stat and s.nzp_stat=cast ((select p_value from " + tables.prefer + " where p_name='nzp_stat_rg') as integer) " + where +
                      " order by t.town, r.rajon";

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    Rajon raj = new Rajon();
                    raj.num = i.ToString();
                    if (reader["nzp_raj"] != DBNull.Value) raj.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                    if (reader["rajon"] != DBNull.Value) raj.rajon = Convert.ToString(reader["rajon"]);

                    if (finder.mode == Constants.act_mode_edit)
                    {
                        sql = "select nzp_vill from " + tables.rajon_vill + " where nzp_raj = " + raj.nzp_raj;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            CloseReader(ref reader);
                            conn_db.Close();
                            return null;
                        }
                        if (reader2.Read())
                        {
                            if (reader2["nzp_vill"] != DBNull.Value) raj.nzp_vill = Convert.ToDecimal(reader2["nzp_vill"]);
                        }
                        reader2.Close();
                    }
                    else raj.nzp_vill = finder.nzp_vill;

                    list.Add(raj);
                    if (finder.rows > 0 && list.Count >= finder.rows) break;
                }


                reader.Close();

                ret.tag = recordsTotalCount;
                return list;
            }
            catch (Exception ex)
            {

                CloseReader(ref reader);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadVillRajon " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<RajonDom> LoadRajonDom(RajonDom finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            var connectionString = Points.GetConnByPref("");
            var connDb = GetConnection(connectionString);
            ret = OpenDb(connDb, true);
            if (!ret.result) return null;

            var tables = new DbTables(connDb);

            MyDataReader reader = null;
            var list = new List<RajonDom>();
            try
            {
                ret = ExecRead(connDb, out reader, " SELECT nzp_raj_dom, rajon_dom, alt_rajon_dom FROM " + tables.rajon_dom + " ORDER BY 1 ", true);
                if (!ret.result) return null;
                while (reader.Read())
                {
                    var raj = new RajonDom();
                    if (reader["nzp_raj_dom"] != DBNull.Value) raj.nzp_raj_dom = Convert.ToInt32(reader["nzp_raj_dom"]);
                    if (reader["rajon_dom"] != DBNull.Value) raj.rajon_dom = Convert.ToString(reader["rajon_dom"]).Trim();
                    if (reader["alt_rajon_dom"] != DBNull.Value) raj.alt_rajon_dom = Convert.ToString(reader["alt_rajon_dom"]).Trim();
                    list.Add(raj);
                }
                if (list.FirstOrDefault(l => l.nzp_raj_dom == -1) != null)
                    list.Remove(list.First(l => l.nzp_raj_dom == -1));
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadRajonDom " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                connDb.Close();
            }
        }

        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            string where = String.Empty;



            if (finder.nzp_raj > 0)
            {
                where += " and r.nzp_raj = " + finder.nzp_raj;
            }

            if (finder.nzp_raj > 0)
            {
                where += " and t.nzp_town = " + finder.nzp_town;
            }


            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection connDb = GetConnection(connectionString);
            ret = OpenDb(connDb, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(connDb);

            MyDataReader reader = null;
            var list = new List<Rajon>();
            try
            {


                string sql = " select trim(t.town) || '/' || trim(r.rajon) as rajon, r.nzp_raj " +
                        " from " + tables.rajon + " r, " + tables.town + " t,  " + tables.ulica + " u, " + tables.dom + " d " +
                        " where t.nzp_town = r.nzp_town  and r.nzp_raj=u.nzp_raj and u.nzp_ul=d.nzp_ul " +
                        where +
                        " group by 1,2 " +
                        " order by 1,2";

                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result) return null;
                while (reader.Read())
                {
                    var raj = new Rajon();
                    if (reader["nzp_raj"] != DBNull.Value) raj.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                    if (reader["rajon"] != DBNull.Value) raj.rajon = Convert.ToString(reader["rajon"]);
                    list.Add(raj);
                }

                return list;
            }
            catch (Exception ex)
            {

                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadVillRajon " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                connDb.Close();
            }
        }

        public Returns SaveVillRajon(Rajon finder, List<Rajon> list_checked)
        {
            Returns ret = Utils.InitReturns();
            if (!(finder.nzp_user > 0))
            {
                ret.text = "Пользователь не задан";
                ret.result = false;
                return ret;
            }

            if (list_checked.Count == 0)
            {
                ret.text = "Нет данных для сохранения";
                ret.result = false;
                return ret;
            }

            if (!(finder.nzp_vill > 0))
            {
                ret.text = "МО не задано";
                ret.result = false;
                return ret;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string sql = "";

            DbTables tables = new DbTables(conn_db);

            try
            {
                IDbTransaction transaction;
                transaction = conn_db.BeginTransaction();
                string str_nzp_raj = "";
                foreach (Rajon nzp in list_checked)
                {
                    if (str_nzp_raj == "") str_nzp_raj += nzp.nzp_raj.ToString();
                    else str_nzp_raj += "," + nzp.nzp_raj.ToString();
                }

                sql = "delete from " + tables.rajon_vill +
                        " where nzp_vill = " + finder.nzp_vill; //+" and nzp_raj not in ("+str_nzp_raj+")";

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }

                foreach (Rajon raj in list_checked)
                {
                    sql = "insert into " + tables.rajon_vill + " (nzp_raj, nzp_vill) " +
                          " values (" + raj.nzp_raj + "," + finder.nzp_vill + ")";

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                }



                transaction.Commit();
                return ret;

            }
            catch (Exception ex)
            {
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveVillRajon " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
        }


        /// <summary>
        /// Процедура генерации платежного кода лицевым счетам  (для тех у кого его нет)
        /// </summary>
        /// <returns></returns>
        public Returns GeneratePkodToLs()
        {
            IDbConnection conn_db = DBManager.newDbConnection(Constants.cons_Kernel);
            Returns ret = Utils.InitReturns();
            try
            {
                //Цикл
                foreach (_Point p in Points.PointList)
                {
                    //получить список квартир

                }


                return ret = Utils.InitReturns();
            }
            catch (Exception)
            {
                return ret = Utils.InitReturns();
            }
            finally
            {
                conn_db.Close();
            }

        }

        /// <summary>
        /// Данные для выписки из лицевого счета по поданным показаниям квартирных приборов учета
        /// </summary>
        /// <returns></returns>
        public DataTable PrepareLsPuVipiska(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return null;
            }

            // проверка улицы
            if (finder.nzp_kvar <= 0)
            {
                ret = new Returns(false, "Не задан лицевой счет", -2);
                return null;
            }

            // проверка улицы
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не задан префикс", -3);
                return null;
            }
            //-----------------------------------------------------------------------
            #endregion

            string _where = " and cs.nzp = " + finder.nzp_kvar;

            #region собрать условие
            //------------------------------------------------------------------------------------------------------------------------------------------------------------------            
            // роли
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                        switch (role.kod)
                        {
                            case Constants.role_sql_serv:
                                _where += " and cc.nzp_serv in (" + role.val + ")";
                                break;
                        }
            }
            //------------------------------------------------------------------------------------------------------------------------------------------------------------------                   
            #endregion

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            table.Columns.Add("service", typeof(string));

            table.Columns.Add("dat_uchet", typeof(string));
            table.Columns.Add("dat_uchet_po", typeof(string));

            table.Columns.Add("num", typeof(string));
            table.Columns.Add("dat_s", typeof(string));

            table.Columns.Add("dat_close", typeof(string));
            table.Columns.Add("val_cnt_s", typeof(decimal));
            table.Columns.Add("mmnog", typeof(decimal));
            table.Columns.Add("val_cnt", typeof(decimal));
            table.Columns.Add("rashod", typeof(string));
            table.Columns.Add("rashod_d", typeof(decimal));

            IDataReader reader;

            CounterVal cv;
            List<CounterVal> listVal = new List<CounterVal>();

            try
            {
                #region
                //----------------------------------------------------------------------------              
#if PG
#warning Вероятна ошибка для постгре, пока не знаю как отловить этот запрос
                string sql = " Select s.service, v.dat_uchet, " +
                    " cs.dat_close, t.mmnog, t.cnt_stage, v.val_cnt, " +
                    " p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
                    " min(f.dat_uchet) as dat_s, min(f.val_cnt) as val_cnt_s " +
                    " From " +
                        finder.pref + "_data.counters_spis cs," +
                        finder.pref + "_kernel.s_counts cc, " +
                        finder.pref + "_kernel.s_counttypes t, " +
                        finder.pref + "_kernel.s_measure m, " +
                        finder.pref + "_kernel.services s, " +
                        finder.pref + "_data.counters v " +
                        " left outer join " + finder.pref + "_data.counters p on (p.nzp_counter = v.nzp_counter) " +
                        " left outer join " + finder.pref + "_data.counters f on (f.nzp_counter = v.nzp_counter) " +
                    " Where cs.nzp_cnttype = t.nzp_cnttype " +
                        " and cs.nzp_serv = s.nzp_serv " +
                        " and cs.nzp_serv = cc.nzp_serv " +
                        " and cc.nzp_measure = m.nzp_measure " +
                        " and cs.nzp_type = 3 " +
                        " and cs.nzp_counter = v.nzp_counter " +
                        " and cs.is_actual <> 100 " +
                        " and v.is_actual <> 100 and v.val_cnt is not null " +
                        " and p.is_actual <> 100 and p.val_cnt is not null " +
                        " and p.dat_uchet = (select max(dat_uchet) from " + finder.pref + "_data.counters b where b.nzp_counter = cs.nzp_counter and b.val_cnt is not null and b.is_actual <> 100 and b.dat_uchet < v.dat_uchet) " +
                        " and f.is_actual <> 100 and f.val_cnt is not null " +
                        _where +
                    " group by 1,2,3,4,5,6,7,8 " +
                    " Order by 1,2,10";
#else
                string sql = " Select s.service, v.dat_uchet, " +
                    " cs.dat_close, t.mmnog, t.cnt_stage, v.val_cnt, " +
                    " p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
                    " min(f.dat_uchet) as dat_s, min(f.val_cnt) as val_cnt_s " +
                    " From " +
                        finder.pref + "_data:counters_spis cs," +
                        finder.pref + "_kernel:s_counts cc, " +
                        finder.pref + "_kernel:s_counttypes t, " +
                        finder.pref + "_kernel:s_measure m, " +
                        finder.pref + "_kernel:services s, " +
                        finder.pref + "_data:counters v " +
                        " left outer join " + finder.pref + "_data:counters p on (p.nzp_counter = v.nzp_counter) " +
                        " left outer join " + finder.pref + "_data:counters f on (f.nzp_counter = v.nzp_counter) " +
                    " Where cs.nzp_cnttype = t.nzp_cnttype " +
                        " and cs.nzp_serv = s.nzp_serv " +
                        " and cs.nzp_serv = cc.nzp_serv " +
                        " and cc.nzp_measure = m.nzp_measure " +
                        " and cs.nzp_type = 3 " +
                        " and cs.nzp_counter = v.nzp_counter " +
                        " and cs.is_actual <> 100 " +
                        " and v.is_actual <> 100 and v.val_cnt is not null " +
                        " and p.is_actual <> 100 and p.val_cnt is not null " +
                        " and p.dat_uchet = (select max(dat_uchet) from " + finder.pref + "_data:counters b where b.nzp_counter = cs.nzp_counter and b.val_cnt is not null and b.is_actual <> 100 and b.dat_uchet < v.dat_uchet) " +
                        " and f.is_actual <> 100 and f.val_cnt is not null " +
                        _where +
                    " group by 1,2,3,4,5,6,7,8 " +
                    " Order by 1,2,10";
#endif


                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    cv = new CounterVal();
                    if (reader["service"] != DBNull.Value) cv.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["dat_uchet"] != DBNull.Value) cv.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();

                    cv.dat_uchet_po = cv.dat_uchet;

                    if (reader["dat_s"] != DBNull.Value) cv.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                    if (reader["dat_close"] != DBNull.Value) cv.dat_close = Convert.ToDateTime(reader["dat_close"]).ToShortDateString();
                    if (reader["val_cnt_s"] != DBNull.Value) cv.val_cnt_s = Convert.ToString(reader["val_cnt_s"]).Trim();
                    if (reader["mmnog"] != DBNull.Value) cv.mmnog = Convert.ToDecimal(reader["mmnog"]);
                    if (reader["val_cnt"] != DBNull.Value) cv.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
                    if (reader["val_cnt_pred"] != DBNull.Value) cv.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred"]);
                    if (reader["dat_uchet_pred"] != DBNull.Value) cv.dat_uchet_pred = Convert.ToString(reader["dat_uchet_pred"]);
                    cv.rashod_d = cv.calculatedRashod;

                    listVal.Add(cv);
                }

                if (listVal.Count > 0)
                {
                    #region привести список к виду, который требуется в отчете
                    //-----------------------------------------------------------------
                    string dat_uchet = listVal[0].dat_uchet;

                    int i = 0;
                    int cnt;
                    double totalRashod;

                    while (i < listVal.Count)
                    {
                        int j = i + 1;
                        listVal[i].num = 1.ToString("00");
                        cnt = 1;
                        totalRashod = listVal[i].rashod_d;

                        if (j >= listVal.Count) break;

                        while (listVal[i].dat_uchet == listVal[j].dat_uchet)
                        {
                            listVal[j].dat_uchet = "";
                            cnt += 1;
                            listVal[j].num = cnt.ToString("00");
                            totalRashod += listVal[j].rashod_d;
                            listVal[j].rashod = "";
                        }

                        listVal[i].rashod = totalRashod.ToString("n").Replace(".", ",");

                        i += cnt;
                    }
                    //-----------------------------------------------------------------
                    #endregion

                    for (i = 0; i < listVal.Count; i++)
                    {
                        if (listVal[i].dat_close.Trim() == "") listVal[i].dat_close = "  /  /    ";

                        table.Rows.Add(
                            listVal[i].service,
                            listVal[i].dat_uchet.Replace(".", "/"),
                            listVal[i].dat_uchet_po,
                            listVal[i].num,
                            listVal[i].dat_s.Replace(".", "/"),
                            listVal[i].dat_close.Replace(".", "/"),
                            Convert.ToDecimal(listVal[i].val_cnt_s),
                            listVal[i].mmnog,
                            Convert.ToDecimal(listVal[i].val_cnt),
                            listVal[i].rashod,
                            listVal[i].rashod_d
                        );
                    }

                }

                reader.Close();
                reader = null;
                //----------------------------------------------------------------------------               
                #endregion

            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                conn_db.Close();
                listVal.Clear();

                return null;
            }

            conn_db.Close();
            listVal.Clear();

            return table;
        }


        public int Management_companies(string pref, StreamWriter writer, IDbConnection conn, bool flag, int year, int month)
        {
            string sqlString = "";
            string month_new = "";
            string year_new = "";
            if (month == 12)
            {
                month_new = "01";
                year_new = (++year).ToString();
            }
            else
            {
                month_new = month.ToString();
                year_new = year.ToString();
            }

            if (!ExecSQL(conn,
           " drop table t_temp;", false).result) { }

            sqlString = "create temp table t_temp( " +
                        "nzp_area integer, " +
                        "area char(25), " +
                        " y_adr char(100), " +
                       " fact_adr char(100), " +
                       "inn char(20), " +
#if PG
 " kpp char(20)) UNLOGGED";
#else
 " kpp char(20)) with no log";
#endif

            ClassDBUtils.OpenSQL(sqlString, conn);

#if PG
            sqlString = "insert into t_temp (nzp_area,area) select nzp_area,area from " + pref + "_data.s_area a";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set y_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set fact_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set inn=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=876 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set kpp=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=877 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
#else
            sqlString = "insert into t_temp (nzp_area,area) select nzp_area,area from " + pref + "_data:s_area a";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set y_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set fact_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set inn=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=876 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update t_temp set kpp=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=877 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
            ClassDBUtils.OpenSQL(sqlString, conn);
#endif

            sqlString = "select * from t_temp order by area";
            IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);

            if (!flag)
            {
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    writer.Write("2|");
                    writer.Write((dt.resultData.Rows[i]["nzp_area"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["area"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["y_adr"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["fact_adr"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["inn"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["kpp"]).ToString().Trim() + "|||||");
                    writer.WriteLine();
                }
            }
            else
            {
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    writer.Write("2|");
                    writer.Write((dt.resultData.Rows[i]["nzp_area"]).ToString().Trim() + "|");
                    writer.Write(("Территория " + dt.resultData.Rows[i]["nzp_area"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["y_adr"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["fact_adr"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["inn"]).ToString().Trim() + "|");
                    writer.Write((dt.resultData.Rows[i]["kpp"]).ToString().Trim() + "|||||");
                    writer.WriteLine();
                }
            }
            sqlString = "drop table t_temp";
            ClassDBUtils.OpenSQL(sqlString, conn);
            return dt.resultData.Rows.Count;
        }

        public int Homes(List<string> pref, StreamWriter writer, IDbConnection conn, bool flag, int year, int month)
        {
            string sqlString = "";
            string month_new = "";
            string year_new = "";
            if (month == 12)
            {
                month_new = "01";
                year_new = (++year).ToString();
            }
            else
            {
                month_new = month.ToString();
                year_new = year.ToString();
            }
            if (!ExecSQL(conn,
           " drop table t_temp;", false).result) { }

            IDataReader reader = null;
#if PG
            sqlString = "create temp table t_temp( ykds integer, nzp_dom char(20),nzp_town integer,nzp_raj integer,nzp_ul integer, town char(30), rajon char(30), ulica char(40), " +
       " ndom char(10), nkor char(3), nzp_area integer, etaj integer, date_postr date, obch_ploch numeric, mest_obch_pol numeric, " +
       " polezn_ploch numeric, kol_ls integer, kol_str integer ) UNLOGGED;";
#else
            sqlString = "create temp table t_temp( ykds integer, nzp_dom char(20),nzp_town integer,nzp_raj integer,nzp_ul integer, town char(30), rajon char(30), ulica char(40), " +
       " ndom char(10), nkor char(3), nzp_area integer, etaj integer, date_postr date, obch_ploch decimal, mest_obch_pol decimal, " +
       " polezn_ploch decimal, kol_ls integer, kol_str integer ) with no log;";
#endif

            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "insert into t_temp (nzp_dom,nzp_town,town,nzp_raj,rajon,nzp_ul,ulica,ndom,nzp_area,nkor) select d.nzp_dom,t.nzp_town,t.town,r.nzp_raj,r.rajon,u.nzp_ul,u.ulica,d.ndom,d.nzp_area,d.nkor " +
                "from " + p + "_data.s_area a," + p + "_data.s_town t, " + p + "_data.s_rajon r," + p + "_data.s_ulica u, " + p + "_data.dom d " +
                "where a.nzp_area=d.nzp_area and t.nzp_town=r.nzp_town and r.nzp_raj=u.nzp_raj and d.nzp_ul  = u.nzp_ul ";
#else
                sqlString = "insert into t_temp (nzp_dom,nzp_town,town,nzp_raj,rajon,nzp_ul,ulica,ndom,nzp_area,nkor) select d.nzp_dom,t.nzp_town,t.town,r.nzp_raj,r.rajon,u.nzp_ul,u.ulica,d.ndom,d.nzp_area,d.nkor " +
                "from " + p + "_data:s_area a," + p + "_data:s_town t, " + p + "_data:s_rajon r," + p + "_data:s_ulica u, " + p + "_data:dom d " +
                "where a.nzp_area=d.nzp_area and t.nzp_town=r.nzp_town and r.nzp_raj=u.nzp_raj and d.nzp_ul  = u.nzp_ul ";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update t_temp set ykds=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_4 p where nzp_prm=890 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update t_temp set ykds=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_4 p where nzp_prm=890 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update t_temp set etaj=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=37 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update t_temp set etaj=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=37 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update t_temp set date_postr=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=150 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update t_temp set date_postr=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=150 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update t_temp set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=40 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update t_temp set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=40 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update t_temp set mest_obch_pol=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=2049 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update t_temp set mest_obch_pol=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=2049 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update t_temp set polezn_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=36 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update t_temp set polezn_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=36 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

#if PG
            sqlString = "update t_temp set kol_ls=(select count(*) from temp_ls l where t_temp.nzp_dom=l.nzp_dom)";
#else
            sqlString = "update t_temp set kol_ls=(select count(*) from temp_ls l where t_temp.nzp_dom=l.nzp_dom)";
#endif

            ClassDBUtils.OpenSQL(sqlString, conn);

#if PG
            sqlString = "update t_temp set kol_str=(select count(*) from temp_counters l where t_temp.nzp_dom=l.nzp_dom)";
#else
            sqlString = "update t_temp set kol_str=(select count(*) from temp_counters l where t_temp.nzp_dom=l.nzp_dom)";
#endif

            ClassDBUtils.OpenSQL(sqlString, conn);

            sqlString = "select * from t_temp order by town, rajon, ulica, ndom";
            int i = 0;

            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                return 0;
            }
            try
            {
                if (reader != null)
                {

                    while (reader.Read())
                    {
                        string str = "3|" +
                        (reader["ykds"] != DBNull.Value ? ((int)reader["ykds"]) + "|" : "|") +
                        (reader["nzp_dom"] != DBNull.Value ? ((string)reader["nzp_dom"]).ToString().Trim() + "|" : "|") +
                        (flag != true ? (reader["town"] != DBNull.Value ? ((string)reader["town"]).ToString().Trim() + "|" : "|") : reader["nzp_town"] != DBNull.Value ? "Город " + ((int)reader["nzp_town"]) + "|" : "|") +
                        (flag != true ? (reader["rajon"] != DBNull.Value ? ((string)reader["rajon"]).ToString().Trim() + "|" : "|") : reader["nzp_raj"] != DBNull.Value ? "Район " + ((int)reader["nzp_raj"]) + "|" : "|") +
                        (flag != true ? (reader["ulica"] != DBNull.Value ? ((string)reader["ulica"]).ToString().Trim() + "|" : "|") : reader["nzp_ul"] != DBNull.Value ? "Улица " + ((int)reader["nzp_ul"]) + "|" : "|") +
                        (reader["ndom"] != DBNull.Value ? ((string)reader["ndom"]).ToString().Trim() + "|" : "|") +
                        (reader["nkor"] != DBNull.Value ? ((string)reader["nkor"]).ToString().Trim() + "|" : "|") +
                        (reader["nzp_area"] != DBNull.Value ? ((int)reader["nzp_area"]) + "|" : "|") +
                        (reader["etaj"] != DBNull.Value ? ((int)reader["etaj"]) + "|" : "|") +
                        (reader["date_postr"] != DBNull.Value ? ((DateTime)reader["date_postr"]).ToString("dd.MM.yyyy") + "|" : "|") +
                        (reader["obch_ploch"] != DBNull.Value ? ((Decimal)reader["obch_ploch"]).ToString("0.00").Trim() + "|" : "|") +
                        (reader["mest_obch_pol"] != DBNull.Value ? ((Decimal)reader["mest_obch_pol"]).ToString("0.00").Trim() + "|" : "|") +
                        (reader["polezn_ploch"] != DBNull.Value ? ((Decimal)reader["polezn_ploch"]).ToString("0.00").Trim() + "||" : "||") +
                        (reader["kol_ls"] != DBNull.Value ? ((int)reader["kol_ls"]) + "|" : "|") +
                        (reader["kol_str"] != DBNull.Value ? ((int)reader["kol_str"]) + "|" : "|");
                        writer.WriteLine(str);
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                return 0;
            }

            writer.Flush();
            return i;
        }

        public StreamWriter LsUpload(List<string> pref, StreamWriter writer, IDbConnection conn, bool is_il, int year, int month)
        {

            string sqlString = "";
            string month_new = "";
            string year_new = "";
            if (month == 12)
            {
                month_new = "01";
                year_new = (++year).ToString();
            }
            else
            {
                month_new = month.ToString();
                year_new = year.ToString();
            }
            if (!ExecSQL(conn,
            " drop table temp_ls;", false).result) { }

#if PG
            sqlString = "  create temp table temp_ls(ukas integer, nzp_dom integer, num_ls char(20), typek integer, " +
              " fio char(100), nkvar char(10), nkvar_n char(3),date_ls_open date,date_ls_close date, nzp_kvar integer, kol_prib integer, " +
              " kol_vr_prib integer, kol_vr_ubiv integer,kol_kom integer,obch_ploch numeric, gil_ploch numeric,otapl_ploch numeric, " +
              " kom_kvar integer,  nal_el_plit integer, nal_gaz_plit integer,  nal_gaz_kol integer,nal_ognev_plit integer,kod_tip_gil integer, " +
              " kod_tip_gil_otopl integer,kod_tip_gil_kan integer, nal_zab integer,kol_uslyga integer,kol_perer integer, kol_ind_prib_ucheta integer " +
              " ) ";
#else
            sqlString = "  create temp table temp_ls(ukas integer, nzp_dom integer, num_ls char(20), typek integer, " +
              " fio char(100), nkvar char(10), nkvar_n char(3),date_ls_open date,date_ls_close date, nzp_kvar integer, kol_prib integer, " +
              " kol_vr_prib integer, kol_vr_ubiv integer,kol_kom integer,obch_ploch decimal, gil_ploch decimal,otapl_ploch decimal, " +
              " kom_kvar integer,  nal_el_plit integer, nal_gaz_plit integer,  nal_gaz_kol integer,nal_ognev_plit integer,kod_tip_gil integer, " +
              " kod_tip_gil_otopl integer,kod_tip_gil_kan integer, nal_zab integer,kol_uslyga integer,kol_perer integer, kol_ind_prib_ucheta integer " +
              " )with no log ";
#endif

            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "   insert into temp_ls(nzp_dom,num_ls,typek, fio, nkvar, nkvar_n, nzp_kvar, kol_prib)  select k.nzp_dom,k.num_ls,( case when k.typek=3 then 2 when k.typek<>3 then 1 end ),k.fio,k.nkvar,k.nkvar_n,k.nzp_kvar," + p + "_data.get_kol_gil(mdy(" + month + ",1," + year + "),mdy(" + month_new + ",1," + year_new + "),15,k.nzp_kvar,0) from " + p + "_data.kvar k  ";
#else
                sqlString = "   insert into temp_ls(nzp_dom,num_ls,typek, fio, nkvar, nkvar_n, nzp_kvar, kol_prib)  select k.nzp_dom,k.num_ls,( case when k.typek=3 then 2 when k.typek<>3 then 1 end ),k.fio,k.nkvar,k.nkvar_n,k.nzp_kvar," + p + "_data:get_kol_gil(mdy(" + month + ",1," + year + "),mdy(" + month_new + ",1," + year_new + "),15,k.nzp_kvar,0) from " + p + "_data:kvar k  ";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set ukas=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_15 p where nzp_prm=162" +
                            " and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set ukas=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_15 p where nzp_prm=162" +
                            " and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set date_ls_open=(select min(dat_s) from " + p + "_data.prm_3 p where nzp_prm=51 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 " +
                            "and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='1')";
#else
                sqlString = "update temp_ls set date_ls_open=(select min(dat_s) from " + p + "_data:prm_3 p where nzp_prm=51 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 " +
                            "and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='1')";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set date_ls_close=(select max(dat_s) from " + p + "_data.prm_3 p where nzp_prm=51 and" +
                            " temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='2')";
#else
                sqlString = "update temp_ls set date_ls_close=(select max(dat_s) from " + p + "_data:prm_3 p where nzp_prm=51 and" +
                            " temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='2')";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set kol_vr_prib=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p" +
                            " where nzp_prm=131 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set kol_vr_prib=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p" +
                            " where nzp_prm=131 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls set kol_vr_ubiv= (select count( distinct nzp_gilec) from " + p + "_data.gil_periods p" +
                            " where temp_ls.nzp_kvar=p.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "  update temp_ls set kol_vr_ubiv= (select count( unique nzp_gilec) from " + p + "_data:gil_periods p" +
                            " where temp_ls.nzp_kvar=p.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set kol_kom=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p" +
                            " where nzp_prm=107 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set kol_kom=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p" +
                            " where nzp_prm=107 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where" +
                            " nzp_prm=4 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where" +
                            " nzp_prm=4 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set gil_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where nzp_prm=6 and " +
                            "temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set gil_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=6 and " +
                            "temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set otapl_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p" +
                            " where nzp_prm=133 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set otapl_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p" +
                            " where nzp_prm=133 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls set kom_kvar = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls set kom_kvar= (select 1 from " + p + "_data.prm_1 p where p.val_prm='2' and p.nzp_prm=3 and p.nzp=temp_ls.nzp_kvar " +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "  update temp_ls set kom_kvar= (select 1 from " + p + "_data:prm_1 p where p.val_prm='2' and p.nzp_prm=3 and p.nzp=temp_ls.nzp_kvar " +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls set nal_el_plit = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls set nal_el_plit= (select 1 from " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=19 and" +
                            " p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "  update temp_ls set nal_el_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=19 and" +
                            " p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls set nal_gaz_plit = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls set nal_gaz_plit= (select 1 from " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=551" +
                            " and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "  update temp_ls set nal_gaz_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=551" +
                            " and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls set nal_gaz_kol = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls set nal_gaz_kol= (select 1 from  " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=1 and p.nzp=temp_ls.nzp_kvar " +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "  update temp_ls set nal_gaz_kol= (select 1 from  " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=1 and p.nzp=temp_ls.nzp_kvar " +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls set nal_ognev_plit = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls set nal_ognev_plit= (select 1 from " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=1172 and p.nzp=temp_ls.nzp_kvar " +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "  update temp_ls set nal_ognev_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=1172 and p.nzp=temp_ls.nzp_kvar " +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set kod_tip_gil=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where nzp_prm=7 and temp_ls.nzp_dom=p.nzp" +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set kod_tip_gil=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=7 and temp_ls.nzp_dom=p.nzp" +
                            " and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }


            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where nzp_prm=894 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=894 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=38 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=38 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls set  nal_zab = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls set nal_zab= (select 1 from " + p + "_data.prm_2 p where p.val_prm='1' and p.nzp_prm=35 and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
                sqlString = "  update temp_ls set nal_zab= (select 1 from " + p + "_data:prm_2 p where p.val_prm='1' and p.nzp_prm=35 and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            sqlString = "update temp_ls set  kol_uslyga = (select count(*) from tmp_inf_serv s where temp_ls.num_ls=s.num_ls)";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update temp_ls set  kol_perer = (select count(*) from temp_ls_perer s where temp_ls.nzp_kvar=s.nzp_kvar)";
            ClassDBUtils.OpenSQL(sqlString, conn);
            //     sqlString = "update temp_ls set  kol_ind_prib_ucheta = (select count(*) from temp_counters_ind s where temp_ls.num_ls=s.num_ls)";
            //    ClassDBUtils.OpenSQL(sqlString, conn);

            return writer;
        }

        public int WriteLs(StreamWriter writer, IDbConnection conn, bool is_il)
        {

            IDataReader reader = null;
            string sqlString = "";
            sqlString = " select * from temp_ls order by  nzp_dom,fio, nkvar, nkvar_n, nzp_kvar, num_ls";
            int i = 0;
            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                return 0;
            }
            try
            {
                if (reader != null)
                {

                    while (reader.Read())
                    {
                        string[] names = (reader["fio"] != DBNull.Value ? ((string)reader["fio"]).ToString().Trim() : "").Split(' ');

                        string first_name = (names.Length == 3 ? names[0] : "");
                        string name = (names.Length == 3 ? names[1] : "");
                        string second_name = (names.Length == 3 ? names[2] : "");

                        string str = "4|" +
                  (reader["ukas"] != DBNull.Value ? ((int)reader["ukas"]) + "|" : "|") +
                  (reader["nzp_dom"] != DBNull.Value ? ((int)reader["nzp_dom"]) + "|" : "|") +
                  (reader["num_ls"] != DBNull.Value ? ((string)reader["num_ls"]).ToString().Trim() + "|" : "|") +
                  (reader["typek"] != DBNull.Value ? ((int)reader["typek"]) + "|" : "|") +
                  (is_il != true ? (first_name + "|" + name + "|" + second_name + "||") : "ФИО " + (reader["num_ls"] != DBNull.Value ? ((string)reader["num_ls"]).ToString().Trim() + "|" : "|")) +
                  (reader["nkvar"] != DBNull.Value ? ((string)reader["nkvar"]).ToString().Trim() + "|" : "|") +
                  (reader["nkvar_n"] != DBNull.Value ? ((string)reader["nkvar_n"]).ToString().Trim() + "|" : "|") +
                  (reader["date_ls_open"] != DBNull.Value ? ((DateTime)reader["date_ls_open"]).ToString("dd.MM.yyyy") + "||" : "||") +
                  (reader["date_ls_close"] != DBNull.Value ? ((DateTime)reader["date_ls_close"]).ToString("dd.MM.yyyy") + "||" : "||") +
                  (reader["nzp_kvar"] != DBNull.Value ? ((int)reader["nzp_kvar"]) + "|" : "|") +
                  (reader["kol_prib"] != DBNull.Value ? ((int)reader["kol_prib"]) + "|" : "|") +
                  (reader["kol_vr_prib"] != DBNull.Value ? ((int)reader["kol_vr_prib"]) + "|" : "|") +
                  (reader["kol_vr_ubiv"] != DBNull.Value ? ((int)reader["kol_vr_ubiv"]) + "|" : "|") +
                  (reader["kol_kom"] != DBNull.Value ? ((int)reader["kol_kom"]) + "|" : "|") +
                  (reader["obch_ploch"] != DBNull.Value ? ((Decimal)reader["obch_ploch"]).ToString("0.00").Trim() + "|" : "|") +
                  (reader["gil_ploch"] != DBNull.Value ? ((Decimal)reader["gil_ploch"]).ToString("0.00").Trim() + "|" : "|") +
                  (reader["otapl_ploch"] != DBNull.Value ? ((Decimal)reader["otapl_ploch"]).ToString("0.00").Trim() + "||" : "||") +
                  (reader["kom_kvar"] != DBNull.Value ? ((int)reader["kom_kvar"]) + "|" : "|") +
                  (reader["nal_el_plit"] != DBNull.Value ? ((int)reader["nal_el_plit"]) + "|" : "|") +
                  (reader["nal_gaz_plit"] != DBNull.Value ? ((int)reader["nal_gaz_plit"]) + "|" : "|") +
                  (reader["nal_gaz_kol"] != DBNull.Value ? ((int)reader["nal_gaz_kol"]) + "|" : "|") +
                  (reader["nal_ognev_plit"] != DBNull.Value ? ((int)reader["nal_ognev_plit"]) + "||" : "||") +
                  (reader["kod_tip_gil"] != DBNull.Value ? ((int)reader["kod_tip_gil"]) + "|" : "|") +
                  (reader["kod_tip_gil_otopl"] != DBNull.Value ? ((int)reader["kod_tip_gil_otopl"]) + "|" : "|") +
                  (reader["kod_tip_gil_kan"] != DBNull.Value ? ((int)reader["kod_tip_gil_kan"]) + "|" : "|") +
                  (reader["nal_zab"] != DBNull.Value ? ((int)reader["nal_zab"]) + "||" : "||") +
                  (reader["kol_uslyga"] != DBNull.Value ? ((int)reader["kol_uslyga"]) + "|" : "|") +
                  (reader["kol_perer"] != DBNull.Value ? ((int)reader["kol_perer"]) + "|" : "|") +
                  (reader["kol_ind_prib_ucheta"] != DBNull.Value ? ((int)reader["kol_ind_prib_ucheta"]) + "|" : "|");

                        writer.WriteLine(str);
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                return 0;
            }

            writer.Flush();

            return i;
        }

        public string Title(string pref, StreamWriter writer, IDbConnection conn, int month, int year, int count)
        {
            string sqlString = "";
#if PG
            sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=80 and is_actual<>100 and current_date between dat_s and dat_po";
#else
            sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=80 and is_actual<>100 and today between dat_s and dat_po";
#endif
            IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
            string naim_org = "";
            try
            {
                naim_org = (dt.resultData.Rows[0]["val_prm"]).ToString().Trim();
            }
            catch { }
            string nomer = "1";
            string time = DateTime.Now.ToString("dd.MM.yyyy");

#if PG
            sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=96 and is_actual<>100 and current_date between dat_s and dat_po";
#else
            sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=96 and is_actual<>100 and today between dat_s and dat_po";
#endif
            dt = ClassDBUtils.OpenSQL(sqlString, conn);
            string telephone = "";
            try
            {
                telephone = (dt.resultData.Rows[0]["val_prm"]).ToString().Trim();
            }
            catch { }
            string fio = "Иванов Иван Иванович";
            string kol_vo = count.ToString();

            string str = "1|" + naim_org + "||||" + nomer + "|" + time + "|" + telephone + "|" + fio + "|" + month + "." + year.ToString() + "|" + kol_vo;

            return str;
        }

        /// <summary>
        /// Обновляет в центральном банке и выбранном списке лицевых счетов пользователя состояния одного лицевого счета
        /// </summary>
        /// <param name="pref">Префикс базы данных</param>
        /// <param name="nzp_kvar">Код лицевого счета</param>
        /// <param name="nzp_user">Код пользователя</param>
        /// <returns></returns>
        public Returns RefreshLsState(string pref, int nzp_kvar, int nzp_user)
        {
            if (pref == "") return new Returns(false, "Префикс не задан");
            if (nzp_kvar <= 0) return new Returns(false, "ЛС не задан");
            if (nzp_user <= 0) return new Returns(false, "Пользователь не задан");

            DbTables tables = GetDbTablesInstance();

            string prm_3 = pref + "_data" + tableDelimiter + "prm_3";

            string sql = " Update " + tables.kvar +
                " Set is_open = " + sNvlWord + "(" + ("( Select max(val_prm) From " + prm_3 +
                    " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and " + sCurDate + " between dat_s and dat_po and is_actual = 1)").CastTo("INTEGER") + ", " + (int)Ls.States.Undefined + ") " +
                " Where nzp_kvar = " + nzp_kvar;

            Returns ret = ExecSQL(sql);
            if (!ret.result) return ret;

            #region наименование таблиц tXX_spls/tXX_spls_full
            string tXX_spls = "t" + nzp_user + "_spls";
#if PG
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;

            conn_web.Close();
#endif
            #endregion

            if (!TempTableInWebCashe(tXX_spls_full))
            {
                return ret;
            }

            sql = " update " + tXX_spls_full + " set sostls = (select max(case when is_open = '" + (int)Ls.States.Open + "' then 'открыт' else 'закрыт' end) from " + tables.kvar + " where nzp_kvar = " + nzp_kvar + ") " +
                " where nzp_kvar = " + nzp_kvar;
            ret = ExecSQL(sql);

            return ret;
        }

        /// <summary>
        /// Обновляет в центральном банке и выбранном списке ЛС пользователя состояния лицевых счетов из выбранного списка ЛС 
        /// </summary>
        /// <param name="pref">Префикс базы данных</param>
        /// <param name="nzp_user">Код пользователя</param>
        /// <returns></returns>
        public Returns RefreshListLsStates(int nzp_user)
        {
            if (nzp_user <= 0) return new Returns(false, "Пользователь не задан");

            Returns ret = Utils.InitReturns();

            #region наименование таблиц tXX_spls/tXX_spls_full
            string tXX_spls = "t" + nzp_user + "_spls";
#if PG
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;

            conn_web.Close();
#endif
            #endregion

            if (!TempTableInWebCashe(tXX_spls_full))
            {
                return ret;
            }

            DbTables tables = GetDbTablesInstance();

            string sql = "select distinct pref from " + tXX_spls_full;
            MyDataReader reader;
            ret = ExecRead(out reader, sql);
            if (!ret.result) return ret;

            try
            {
                string pref, prm_3;

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                    {
                        pref = Convert.ToString(reader["pref"]).Trim();
                        prm_3 = pref + "_data" + tableDelimiter + "prm_3";

                        sql = " Update " + tables.kvar +
                            " Set is_open = " + sNvlWord + "(" + ("( Select max(val_prm) From " + prm_3 +
                                " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and " + sCurDate + " between dat_s and dat_po and is_actual = 1)").CastTo("INTEGER") + ", " + (int)Ls.States.Undefined + ") " +
                            " Where nzp_kvar in (select nzp_kvar from " + tXX_spls_full + " where pref = " + Utils.EStrNull(pref) + ")";

                        ret = ExecSQL(sql);
                        if (!ret.result) return ret;
                    }
                }
            }
            finally
            {
                reader.Close();
            }

            sql = " update " + tXX_spls_full + " set sostls = (select max(case when is_open = '" + (int)Ls.States.Open + "' then 'открыт' else 'закрыт' end) from " + tables.kvar + " where nzp_kvar = " + tXX_spls_full + ".nzp_kvar) ";
            ret = ExecSQL(sql);

            return ret;
        }

        /// <summary>
        /// Обновляет в центральном банке и выбранном списке ЛС пользователя состояния лицевых счетов всех домов из выбранного списка домов 
        /// </summary>
        /// <param name="pref">Префикс базы данных</param>
        /// <param name="nzp_user">Код пользователя</param>
        /// <returns></returns>
        public Returns RefreshListDomLsStates(int nzp_user)
        {
            if (nzp_user <= 0) return new Returns(false, "Пользователь не задан");

            Returns ret = Utils.InitReturns();

            #region наименование таблиц tXX_spls/tXX_spls_full
            string tXX_spdom = "t" + nzp_user + "_spdom";
            string tXX_spls = "t" + nzp_user + "_spls";
#if PG
            string tXX_spdom_full = pgDefaultDb + "." + tXX_spdom;
            string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom;
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;

            conn_web.Close();
#endif
            #endregion

            if (!TempTableInWebCashe(tXX_spdom_full))
            {
                return ret;
            }

            DbTables tables = GetDbTablesInstance();

            string sql = "select distinct pref from " + tXX_spdom_full;
            MyDataReader reader;
            ret = ExecRead(out reader, sql);
            if (!ret.result) return ret;

            try
            {
                string pref, prm_3;

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                    {
                        pref = Convert.ToString(reader["pref"]).Trim();
                        prm_3 = pref + "_data" + tableDelimiter + "prm_3";

                        sql = " Update " + tables.kvar +
                            " Set is_open = " + sNvlWord + "(" + ("( Select max(val_prm) From " + prm_3 +
                                " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and " + sCurDate + " between dat_s and dat_po and is_actual = 1)").CastTo("INTEGER") + ", " + (int)Ls.States.Undefined + ") " +
                            " Where nzp_dom in (select nzp_dom from " + tXX_spdom_full + " where pref = " + Utils.EStrNull(pref) + ")";

                        ret = ExecSQL(sql);
                        if (!ret.result) return ret;
                    }
                }
            }
            finally
            {
                reader.Close();
            }

            if (!TempTableInWebCashe(tXX_spls_full))
            {
                return ret;
            }

            sql = " update " + tXX_spls_full + " set sostls = (select max(case when is_open = '" + (int)Ls.States.Open + "' then 'открыт' else 'закрыт' end) from " + tables.kvar + " where nzp_kvar = " + tXX_spls_full + ".nzp_kvar) " +
                " where nzp_dom in (select nzp_dom from " + tXX_spdom_full + ")";
            ret = ExecSQL(sql);

            return ret;
        }

        /// <summary>
        /// Выгрузка сальдо в банк
        /// </summary>
        /// <returns></returns>
        /// 

        public DataTable PrepareGubCurrCharge(Charge finder, int reportId, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return null;
            }

            if (finder.dat_calc.Length == 0)
            {
                ret = new Returns(false, "Не задан расчетный месяц", -1);
                return null;
            }

            DateTime calc_month;

            try
            {
                calc_month = Convert.ToDateTime(finder.dat_calc);
            }
            catch
            {
                ret = new Returns(false, "Неверный формат даты расчетного месяца", -1);
                return null;
            }
            //-----------------------------------------------------------------------
            #endregion

            string _where_kvar = "";
            string _where_serv = "";
            string _where_wp = "";

            #region собрать условие
            //------------------------------------------------------------------------------------------------------------------------------------------------------------------            
            // роли
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                        switch (role.kod)
                        {
                            case Constants.role_sql_serv:
                                _where_serv += " and cc.nzp_serv in (" + role.val + ")";
                                break;
                            case Constants.role_sql_area:
                                _where_kvar += " and k.nzp_area in (" + role.val + ")";
                                break;
                            case Constants.role_sql_geu:
                                _where_kvar += " and k.nzp_geu in (" + role.val + ")";
                                break;
                            case Constants.role_sql_wp:
                                _where_wp += " and k.nzp_wp in (" + role.val + ")";
                                break;
                        }
            }

            if (finder.nzp_ul > 0) _where_kvar += " and d.nzp_ul = " + finder.nzp_ul;
            if (finder.nzp_dom > 0) _where_kvar += " and k.nzp_dom = " + finder.nzp_dom;
            if (finder.nzp_kvar > 0) _where_kvar += " and k.nzp_kvar = " + finder.nzp_kvar;
            if (finder.nzp_area > 0) _where_kvar += " and k.nzp_area = " + finder.nzp_area;
            //------------------------------------------------------------------------------------------------------------------------------------------------------------------                   
            #endregion

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            IDataReader reader;
            List<string> prefList = new List<string>();

            try
            {
                // получить список префиксов
#if PG
                string sql = " Select distinct k.pref from " + Points.Pref + "_data.kvar k, " + Points.Pref + "_data.dom d Where d.nzp_dom = k.nzp_dom " + _where_kvar + _where_wp;
#else
                string sql = " Select distinct k.pref from " + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d Where d.nzp_dom = k.nzp_dom " + _where_kvar + _where_wp;
#endif

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value) prefList.Add(Convert.ToString(reader["pref"]).Trim());
                }

                DataTable total = null;

                #region
                //----------------------------------------------------------------------------              
                foreach (string cur_pref in prefList)
                {
                    string charge = cur_pref + "_charge_" + (calc_month.Year % 100).ToString("00") + ":charge_" + calc_month.Month.ToString("00");

#if PG
                    sql = " select a.area, u.ulica, d.idom, d.ndom, s.ordering, s.service_small as service, " +
                        (reportId == Constants.act_report_gub_curr_charge ? " sum(ch.sum_charge) as sum_charge " : " sum(ch.sum_money) as sum_charge ") +
                        " from " + cur_pref + "_data.s_ulica u, " + cur_pref + "_data.dom d, " + cur_pref + "_data.kvar k, " + cur_pref + "_data.s_area a, " + cur_pref + "_kernel.services s, " + charge + " ch " +
                        " where u.nzp_ul = d.nzp_ul " +
                            " and d.nzp_dom = k.nzp_dom " +
                            " and k.nzp_area = a.nzp_area " +
                            " and ch.nzp_kvar = k.nzp_kvar " +
                            " and s.nzp_serv = ch.nzp_serv " +
                            " and s.nzp_serv > 1 " + _where_kvar + _where_serv +
                        " group by 1,2,3,4,5,6 " +
                        " order by 1,2,3,4,5,6";
#else
                    sql = " select a.area, u.ulica, d.idom, d.ndom, s.ordering, s.service_small as service, " +
                        (reportId == Constants.act_report_gub_curr_charge ? " sum(ch.sum_charge) as sum_charge " : " sum(ch.sum_money) as sum_charge ") +
                        " from " + cur_pref + "_data:s_ulica u, " + cur_pref + "_data: dom d, " + cur_pref + "_data: kvar k, " + cur_pref + "_data: s_area a, " + cur_pref + "_kernel:services s, " + charge + " ch " +
                        " where u.nzp_ul = d.nzp_ul " +
                            " and d.nzp_dom = k.nzp_dom " +
                            " and k.nzp_area = a.nzp_area " +
                            " and ch.nzp_kvar = k.nzp_kvar " +
                            " and s.nzp_serv = ch.nzp_serv " +
                            " and s.nzp_serv > 1 " + _where_kvar + _where_serv +
                        " group by 1,2,3,4,5,6 " +
                        " order by 1,2,3,4,5,6";
#endif

                    IntfResultTableType dt = ClassDBUtils.OpenSQL(sql, conn_db);

                    if (total == null)
                    {
                        total = dt.GetData().Copy();
                    }
                    else
                    {
                        foreach (DataRow dr in dt.GetData().Rows) total.Rows.Add(dr.ItemArray);
                    }
                }
                //----------------------------------------------------------------------------               
                #endregion

                DataView dv = new DataView(total);
                dv.Sort = "area asc, ulica asc, idom asc, ndom asc, ordering asc";
                DataTable sortedDt = dv.ToTable();

                DataTable table = sortedDt.DefaultView.ToTable(false, "area", "ulica", "ndom", "service", "sum_charge");
                table.TableName = "Q_master";

                conn_db.Close();
                return table;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                conn_db.Close();
                return null;
            }
        }

        public Returns DbUpdateMovedHousesPkod(string connString)
        {
            IDbConnection conn_web = DBManager.newDbConnection(connString);
            IDbTransaction transaction = null;

            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            try
            {
                string sql = "select bd_kernel from s_point where nzp_graj = 0";
                IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_web, transaction);
                Points.Pref = IDbCommand.ExecuteScalar().ToString().Trim();
                Points.IsSmr = true;
                IDbCommand.Dispose();

                sql = " select bd_kernel from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_point where nzp_graj = 1 ";
                IDbCommand = DBManager.newDbCommand(sql, conn_web);
                var dt = ClassDBUtils.OpenSQL(sql, conn_web, transaction);
                IDbCommand.Dispose();
                var resRows = dt.resultData.Rows;

                MyDataReader reader = null;
                string pref;

                for (int i = 0; i < resRows.Count; i++)
                {
                    pref = resRows[i]["bd_kernel"].ToString().Trim();

                    // определение платежного кода
                    sql = "SELECT d.nzp_kvar_n FROM " + Points.Pref + "_data" + tableDelimiter + "dom_moved d, " + pref + "_data" + tableDelimiter + "kvar k where k.nzp_kvar = d.nzp_kvar_n and d.is_to_move = 1";

                    ret = ExecRead(conn_web, transaction, out reader, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    var counter = 0;
                    while (reader.Read())
                    {
                        counter++;
                        var nzp_kvar_n = (reader["nzp_kvar_n"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_kvar_n"]) : 0;

                        //получить платежный код
                        string pkod = GeneratePkod(conn_web, null, new Ls() { nzp_kvar = nzp_kvar_n, pref = pref }, out ret);
                        //pkod = GeneratePkodOneLS(new Ls(), transaction,)

                        //проапдейтить локальную базу
                        sql = "update " + pref + "_data" + tableDelimiter + "kvar set pkod = " + pkod + " where nzp_kvar = " + nzp_kvar_n;
                        IDbCommand = DBManager.newDbCommand(sql, conn_web, transaction);
                        ClassDBUtils.ExecSQL(sql, conn_web, transaction);
                        IDbCommand.Dispose();

                        //проапдейтить центральную базу
                        sql = "update " + Points.Pref + "_data" + tableDelimiter + "kvar set pkod = " + pkod + " where nzp_kvar = " + nzp_kvar_n;
                        IDbCommand = DBManager.newDbCommand(sql, conn_web, transaction);
                        ClassDBUtils.ExecSQL(sql, conn_web, transaction);
                        IDbCommand.Dispose();
                    }
                    reader.Close();
                }

            }
            catch (DbException ex)
            {
                ret.text = ex.Message;
                ret.result = false;
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_web.Close();
            }
            return ret;
        }



        /// <summary>
        /// Генерация платежных кодов
        /// </summary>
        /// <param name="finder">nzp_user</param>
        /// <returns>результат</returns>
        public Returns GeneratePkodFon(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            DbAdresKernel db = new DbAdresKernel();
            ret =
                db.GeneratePkodOnLsList(new Ls()
                {
                    nzp_user = finder.nzp_user
                });
            db.Close();

            return ret;
        }

        public new Returns NewGeneratePkod(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            DbAdresKernel db = new DbAdresKernel();
            ret =
                db.NewGeneratePkod(new Finder()
                {
                    nzp_user = finder.nzp_user,
                    dopFind = finder.dopFind
                });

            db.Close();

            return ret;
        }

        public List<Ls> LoadLsData(Ls finder, out Returns ret) //найти и заполнить адрес
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<Ls> Listls = new List<Ls>();

            string pref = Points.Pref;

            #region соединение с БД
            string conn_kernel = Points.GetConnByPref(pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return Listls;
            #endregion

            Listls = LoadLsData(conn_db, finder, out ret);

            conn_db.Close();
            return Listls;
        }

        public List<Ls> LoadLsData(IDbConnection conn_db, Ls finder, out Returns ret) //найти и заполнить адрес для nzp_kvar
        {
            ret = new Returns(true);
            List<Ls> Listls = new List<Ls>();
            IDataReader reader;

            string swhere = ""; //условия
            StringBuilder sql = new StringBuilder();
            sql.Append("drop table tmpaddress");
            ExecSQL(conn_db, sql.ToString(), false);

            sql.Remove(0, sql.Length);
            sql.Append("create temp table tmpaddress");
            sql.Append("(nzp_wp integer, ");
            sql.Append("num_ls integer, ");
            sql.Append("pkod10 integer, ");
            sql.Append("nzp_dom integer,");
#if PG
            sql.Append("pkod numeric(13,0),");
#else
            sql.Append("pkod decimal(13,0),");
#endif
            sql.Append("fio char(50),");
            sql.Append("nzp_kvar integer, ");
            sql.Append("nzp_area integer, ");
            sql.Append("area char(40), ");
            sql.Append("nzp_geu integer, ");
            sql.Append("geu char(60),");
            sql.Append("nzp_ul integer,");
            sql.Append("nzp_raj integer,  ");
            sql.Append("nzp_town integer,  ");
            sql.Append("pref char(10),");
            sql.Append("is_open char(1),");
            sql.Append("state char(255),");
            sql.Append("ulicareg char(40),");
            sql.Append("ulica char(40),");
            sql.Append("rajon char(30),");
            sql.Append("ndom char(10), ");
            sql.Append("nkor char(30),");
            sql.Append("nkvar char(10),");
            sql.Append("nkvar_n char(10));");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(conn_db);

            if (finder.nzp_kvar > 0) swhere += " and nzp_kvar = " + finder.nzp_kvar;
            if (finder.num_ls > 0) swhere += " and num_ls = " + finder.num_ls;
            string kpwhere = "";
            if (!GlobalSettings.NewGeneratePkodMode)
            {
                if (finder.pkod != "") swhere += " and pkod = " + finder.pkod;
            }
            else
            {
                if (finder.pkod != "") kpwhere = " and nzp_kvar in (select nzp_kvar from " + tables.kvar_pkodes + " where pkod = " + finder.pkod + ")";
            }
            if (finder.fio != "") swhere += " and lower(fio) like lower('%" + finder.fio + "%')";
            if (finder.pkod10 > 0) swhere += " and pkod10 = " + finder.pkod10;
            if (finder.num_ls_s != "")
            {
                sql.Remove(0, sql.Length);
                sql.Append("drop table tmptablealiasls");
                ret = ExecSQL(conn_db, sql.ToString(), false);

                sql.Remove(0, sql.Length);
                sql.Append("create temp table tmptablealiasls");
                sql.Append("(nzp_kvar integer, ");
                sql.Append("pref char(10) )");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("delete from tmptablealiasls");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                }

                for (int i = 0; i < Points.PointList.Count; i++)
                {
                    string tablename = Points.PointList[i].pref + "_data" + tableDelimiter + "alias_ls";
                    if (!TempTableInWebCashe(conn_db, tablename)) continue;

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into tmptablealiasls (nzp_kvar, pref)");
                    sql.Append(" select nzp_kvar, '" + Points.PointList[i].pref + "' from " + tablename +
                               " where numls = '" + finder.num_ls_s + "'");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return null;
                }
                swhere += " and nzp_kvar in (select nzp_kvar from tmptablealiasls)";
            }

            sql.Remove(0, sql.Length);
            sql.Append(" insert into tmpaddress (num_ls, pkod10, nzp_dom, pkod, fio, nzp_kvar, nkvar, nkvar_n, pref, ");
            sql.Append(" nzp_area, nzp_geu, nzp_wp, is_open)  ");
            sql.Append(" select num_ls, pkod10, nzp_dom, pkod, fio, nzp_kvar, nkvar, nkvar_n, pref, nzp_area, nzp_geu, nzp_wp, is_open ");
            sql.Append(" from " + tables.kvar);
            sql.Append(" where 1=1 " + swhere + kpwhere);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            if (GlobalSettings.NewGeneratePkodMode)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update tmpaddress set pkod = (select max(pkod) from " + tables.kvar_pkodes + " where nzp_kvar=tmpaddress.nzp_kvar and is_princip = 0)  ");
                sql.Append(" where " + sNvlWord + "(nzp_area,0)>0; ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return null;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" update tmpaddress set area = (select area from " + tables.area + " where nzp_area=tmpaddress.nzp_area)  ");
            sql.Append(" where " + sNvlWord + "(nzp_area,0)>0; ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            sql.Remove(0, sql.Length);
            sql.Append(" update tmpaddress set geu = (select geu from " + tables.geu + " where nzp_geu=tmpaddress.nzp_geu)  ");
            sql.Append(" where " + sNvlWord + "(nzp_geu,0)>0;  ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" update tmpaddress set nzp_ul = (select nzp_ul from " + tables.dom + " where nzp_dom=tmpaddress.nzp_dom), " +
            " ndom = (select ndom from " + tables.dom + " where nzp_dom=tmpaddress.nzp_dom)," +
            " nkor = (select nkor from " + tables.dom + " where nzp_dom=tmpaddress.nzp_dom) ");
#else
            sql.Append(" update tmpaddress set (nzp_ul, ndom, nkor) = ((select nzp_ul, ndom, nkor from " + tables.dom + " where nzp_dom=tmpaddress.nzp_dom)) ");
#endif
            sql.Append(" where " + sNvlWord + "(nzp_dom,0)>0;  ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" update tmpaddress set ulicareg = (select ulicareg from " + tables.ulica + " where nzp_ul=tmpaddress.nzp_ul)," +
            " ulica = (select ulica from " + tables.ulica + " where nzp_ul=tmpaddress.nzp_ul)," +
            " nzp_raj = (select nzp_raj from " + tables.ulica + " where nzp_ul=tmpaddress.nzp_ul)  ");
#else
            sql.Append(" update tmpaddress set (ulicareg, ulica, nzp_raj) = ((select ulicareg, ulica, nzp_raj from " + tables.ulica + " where nzp_ul=tmpaddress.nzp_ul))  ");
#endif
            sql.Append(" where " + sNvlWord + "(nzp_ul,0)>0;  ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" update tmpaddress set rajon = (select rajon from " + tables.rajon + " where nzp_raj=tmpaddress.nzp_raj), " +
            "nzp_town = (select nzp_town from " + tables.rajon + " where nzp_raj=tmpaddress.nzp_raj) ");
#else
            sql.Append(" update tmpaddress set (rajon, nzp_town) = ((select rajon,nzp_town from " + tables.rajon + " where nzp_raj=tmpaddress.nzp_raj)) ");
#endif
            sql.Append(" where " + sNvlWord + "(nzp_raj,0)>0; ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            sql.Remove(0, sql.Length);
            sql.Append(" update tmpaddress set state = (select name_y from " + tables.res_y + " where tmpaddress.is_open = nzp_y::char and nzp_res = 18); ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return null;

            sql.Remove(0, sql.Length);
            sql.Append(" select count(*)  ");
            sql.Append(" from tmpaddress;  ");
            object count = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (ret.result)
            {
                try
                {
                    ret.tag = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

            sql.Remove(0, sql.Length);
            sql.Append(" select   ");
            sql.Append(" nzp_wp, num_ls, pkod10, nzp_dom, pkod, fio, nzp_kvar,  ");
            sql.Append(" nzp_area, area, nzp_geu, geu, nzp_ul, nzp_raj,  ");
            sql.Append(" trim(" + sNvlWord + "(ulicareg,'улица'))||' '||trim(" + sNvlWord + "(ulica,''))||' / '||trim(" + sNvlWord + "(rajon,''))||'   дом '||  ");
            sql.Append(" trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord + "(nkor,''))||'  кв. '||trim(" + sNvlWord + "(nkvar,''))||'  ком. '|| ");
            sql.Append(" trim(" + sNvlWord + "(nkvar_n,'')) as adr,  ");
            sql.Append(" round(pkod)||'' as spkod, pref, is_open, state  ");
            sql.Append(" from tmpaddress;  ");


            if (sql.Length <= 0) return Listls;

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result) return Listls;
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (finder.skip > 0 && finder.skip >= i) continue;

                    Ls ls = new Ls();

                    if (reader["adr"] != DBNull.Value) ls.adr = Convert.ToString(reader["adr"]);
                    if (reader["pref"] != DBNull.Value) ls.pref = ((string)reader["pref"]).Trim();
                    if (reader["nzp_area"] != DBNull.Value) ls.nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_geu"] != DBNull.Value) ls.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["nzp_kvar"] != DBNull.Value) ls.nzp_kvar = (int)reader["nzp_kvar"];
                    if (reader["nzp_dom"] != DBNull.Value) ls.nzp_dom = (int)reader["nzp_dom"];
                    if (reader["nzp_ul"] != DBNull.Value) ls.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["nzp_wp"] != DBNull.Value) ls.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["num_ls"] != DBNull.Value) ls.num_ls = (int)(reader["num_ls"]);
                    if (reader["pkod10"] != DBNull.Value) ls.pkod10 = (int)(reader["pkod10"]);
                    if (reader["spkod"] != DBNull.Value) ls.pkod = Convert.ToString(reader["spkod"]).Trim();
                    if (reader["fio"] != DBNull.Value) ls.fio = Convert.ToString(reader["fio"]);
                    if (reader["state"] != DBNull.Value) ls.state = ((string)reader["state"]).Trim();
                    if (reader["geu"] != DBNull.Value) ls.geu = Convert.ToString(reader["geu"]);
                    if (reader["area"] != DBNull.Value) ls.area = Convert.ToString(reader["area"]);
                    if (reader["is_open"] != DBNull.Value)
                    {
                        int stateID;
                        if (Int32.TryParse(((string)reader["is_open"]).Trim(), out stateID))
                            ls.stateID = stateID;
                        else
                        {
                            ls.stateID = 0;
                        }
                    }

                    Listls.Add(ls);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();

                sql.Append("drop table tmpaddress");
                ExecSQL(conn_db, sql.ToString(), false);

                sql.Append("drop table tmptablealiasls");
                ExecSQL(conn_db, sql.ToString(), false);
            }
            catch (Exception ex)
            {
                reader.Close();
                reader.Dispose();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка заполнения Адреса в финансах " + err, MonitorLog.typelog.Error, 20, 201, true);
            }

            return Listls;
        }

        public List<KvarPkodes> GetPkodes(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<KvarPkodes> list = new List<KvarPkodes>();

            DbTables tables = new DbTables(conn_db);
            StringBuilder sql = new StringBuilder("select pkod, id, payer, is_princip from " + tables.kvar_pkodes + " kp " +
                " left outer join " + tables.payer + " p on kp.nzp_payer = p.nzp_payer " +
                " where is_default = 1 and nzp_kvar = " + finder.nzp_kvar);

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке платежных кодов лицевого счета";
                return null;
            }

            try
            {
                while (reader.Read())
                {
                    KvarPkodes zap = new KvarPkodes();
                    zap.id = reader["id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["id"]);
                    zap.pkod_text = reader["pkod"] == DBNull.Value ? "" : Convert.ToString(reader["pkod"]);
                    zap.pkod_text += reader["is_princip"] == DBNull.Value ? "" : (Convert.ToInt32(reader["is_princip"]) == 0 ? " / Агент" : " / Принципал");
                    zap.pkod_text += reader["payer"] == DBNull.Value ? "" : " / " + Convert.ToString(reader["payer"]);
                    zap.pkod = reader["pkod"] == DBNull.Value ? "" : Convert.ToString(reader["pkod"]);
                    list.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return list;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки действующих платежных кодов текущего лицевого счета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public void SaveSupplierLs(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return;
            }

            //-----------------------------------------------------------------------------------
            #endregion
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;
            StringBuilder sql = new StringBuilder();
            string note = String.Empty;
            if (finder.mode == 1)
            {
                sql.Append("Insert into " + finder.pref + "_data" + tableDelimiter + "alias_ls (nzp_kvar,numls,nzp_supp,comment) values ("
                    + finder.nzp_kvar + ",'" + finder.numls + "'," + finder.nzp_supp + ",'" + finder.comment + "');");
                note = "Добавление нового кода ЛС поставщика (" + finder.nzp_supp + ")";
            }
            else
            {
                sql.Append("Update " + finder.pref + "_data" + tableDelimiter + "alias_ls set(numls,nzp_supp,comment)=");
                sql.Append("('" + finder.numls + "'," + finder.nzp_supp + ",'" + finder.comment.Trim() + "') where nzp_supp=" + finder.supplier + " and nzp_kvar=" + finder.nzp_kvar + ";");
                note = "Изменение кода ЛС поставщика (" + finder.nzp_supp + ")";
            }
            sql.Append(" insert into " + Points.Pref + "_data" + tableDelimiter + "sys_events (date_,nzp_user,nzp_dict_event,nzp,note) values");
            sql.Append(" (" + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.000")) + "," + finder.nzp_user + ",6495," + finder.nzp_kvar + ", " + Utils.EStrNull(note) + ");");
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при сохранении текущего лицевого счета";
                return;
            }
            conn_db.Close();
            return;
        }

        public bool SaveLsGroup(KP50.Interfaces.Group finder, List<string> groupList, out Returns ret) //загрузить группы текущего лицевого счета
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return false;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return false;
            }

            //-----------------------------------------------------------------------------------
            #endregion

            #region подключение к базе
            //-----------------------------------------------------------------------------------
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;
            //-----------------------------------------------------------------------------------
            #endregion

            string sqlString = "";
#if PG
            string pref = finder.pref + "_data.";
#else
            string pref = finder.pref + "_data:";

#endif
            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            #region сохранение
            //-----------------------------------------------------------------------------------
            sqlString = "delete from " + pref + "link_group where nzp = " + finder.nzp_kvar.ToString();

            ret = ExecSQL(conn_db, transaction, sqlString, true);
            if (ret.result)
            {
                for (int i = 0; i < groupList.Count; i++)
                {
                    sqlString = "insert into " + pref + "link_group (nzp_group, nzp) values (" + groupList[i] + "," + finder.nzp_kvar.ToString() + ")";
                    ret = ExecSQL(conn_db, transaction, sqlString, true);

                    if (!ret.result) break;
                }

                if (ret.result)
                {
                    if (transaction != null) transaction.Commit();
                }
                else
                {
                    ret.text = "Ошибка сохранения групп лицевого счета";
                    if (transaction != null) transaction.Rollback();
                }

            }
            else
            {
                ret.text = "Ошибка сохранения групп лицевого счета";
                if (transaction != null)
                    transaction.Rollback();
            }
            //-----------------------------------------------------------------------------------
            #endregion

            return ret.result;

        }//SaveLsGroup

        public bool SaveListGroupLSBySelectedDoms(KP50.Interfaces.Group finder, out Returns ret) //включить/исключить выбранный список в/из групп
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных

            if (finder.pref == "")
            {
                ret = new Returns(false, "Не выбран банк данных", -1);
                return ret.result;
            }
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return ret.result;
            }
            if (finder.ngroup == "")
            {
                ret = new Returns(false, "Нет выбранных групп", -1);
                return ret.result;
            }

            #endregion

            #region подключение к базе

            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;

            #endregion

            string tXX =  "t" + finder.nzp_user + "_spdom";
            string tXX_full = sDefaultSchema + tXX;

            if (!TableInWebCashe(conn_db, tXX))
            {
                ret = new Returns(false, "Нет выбранных домов", -1);
                return ret.result;
            }
            string sqlString = "";

            IDbTransaction transaction = null;
            try
            {
                transaction = conn_db.BeginTransaction();
                string[] array = finder.ngroup.Split(',');
                // по префиксу определим код банка 
                int nzp_wp = Points.GetPoint(finder.pref).nzp_wp;
                if (nzp_wp <= 0)
                {
                    ret = new Returns(false, "Не верные входные параметры. Не определился код банка данных", -1);
                    return ret.result;
                }
                // для каждой группы 
                foreach (string nzp in array)
                {
                    // сначала удаляем ЛС выбранных домов соответствующего банка
                    sqlString = "delete from " + finder.pref + "_data.link_group where nzp_group = " + nzp +
                                " and nzp in (select k.nzp_kvar from " + tXX_full + " d, " + Points.Pref + sDataAliasRest + "kvar k " +
                                "where k.nzp_wp=d.nzp_wp and d.mark=1 and k.nzp_dom=d.nzp_dom and d.nzp_wp=" + nzp_wp + ")";
                    // а если операция добавления в группу, то вставляем
                    ret = ExecSQL(conn_db, transaction, sqlString, true);
                    if (!Utils.GetParams(finder.prms, Constants.act_add_ingroup)) continue;
                    if (ret.result)
                    {
                        sqlString = "insert into " + finder.pref + "_data.link_group (nzp_group, nzp) " +
                                    "select " + nzp + ", k.nzp_kvar from " + tXX_full + " d, " + Points.Pref + sDataAliasRest + "kvar k " +
                                    "where k.nzp_wp=d.nzp_wp and d.mark=1 and  k.is_open='1' and k.nzp_dom=d.nzp_dom and d.nzp_wp=" + nzp_wp;

                        ret = ExecSQL(conn_db, transaction, sqlString, true);
                        if (!ret.result) break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                if (ret.result)
                {
                    if (transaction != null) transaction.Commit();
                }
                else
                {
                    if (transaction != null) transaction.Rollback();
                    MonitorLog.WriteLog(@"SaveListGroupLSBySelectedDoms(). Ошибка добавления/удаления ЛС выбранных домов в группу/из группы " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                }
                conn_db.Close();
            }
            return ret.result;
        } //SaveListGroupLSBySelectedDoms

        public bool SaveListGroup(KP50.Interfaces.Group finder, out Returns ret) //включить/исключить выбранный список в/из групп
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных
            if (finder.pref == "")
            {
                ret = new Returns(false, "Префикс не задан", -1);
                return ret.result;
            }
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return ret.result;
            }
            if (finder.ngroup == "")
            {
                ret = new Returns(false, "Нет выбранных групп", -1);
                return ret.result;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return false;
            //проверить
            string tXX = "t" + finder.nzp_user.ToString() + "_spls";
#if PG
            //  string tXX_full = conn_web.Database + "." + tXX;
            string tXX_full = "public." + tXX;

#else
            string tXX_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX;

#endif
            if (!TableInWebCashe(conn_web, tXX))
            {
                ret = new Returns(false, "Нет выбранных лицевых счетов", -1);
                return ret.result;
            }

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;
            #endregion

            string sqlString = "";

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            if (Utils.GetParams(finder.prms, Constants.act_add_ingroup))//добавление
            {
                string[] array = finder.ngroup.Split(',');
                foreach (string nzp in array)
                {
#if PG
                    sqlString = "delete from " + finder.pref + "_data.link_group where nzp_group = " + nzp +
                                " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";
#else
                    sqlString = "delete from " + finder.pref + "_data:link_group where nzp_group = " + nzp +
                                " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";

#endif
                    ret = ExecSQL(conn_db, transaction, sqlString, true);
                    if (ret.result)
                    {
#if PG
                        sqlString = "insert into " + finder.pref + "_data.link_group (nzp_group, nzp) " +
                                    " select " + nzp + ", nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1";
#else
                        sqlString = "insert into " + finder.pref + "_data:link_group (nzp_group, nzp) " +
                                    " select " + nzp + ", nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1";

#endif
                        ret = ExecSQL(conn_db, transaction, sqlString, true);
                        if (!ret.result) break;
                    }
                    else break;
                }
                if (ret.result)
                {
                    if (transaction != null) transaction.Commit();
                }
                else
                {
                    if (transaction != null) transaction.Rollback();
                }
            }
            else
            {
                if (Utils.GetParams(finder.prms, Constants.act_del_outgroup))//удаление
                {
                    string[] array = finder.ngroup.Split(',');
                    foreach (string nzp in array)
                    {
#if PG
                        sqlString = "delete from " + finder.pref + "_data.link_group where nzp_group = " + nzp +
                                    " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";
#else
                        sqlString = "delete from " + finder.pref + "_data:link_group where nzp_group = " + nzp +
                                    " and nzp in (select nzp_kvar from " + tXX_full + " where pref = '" + finder.pref + "' and mark=1)";

#endif
                        ret = ExecSQL(conn_db, transaction, sqlString, true);
                        if (!ret.result) break;
                    }
                    if (ret.result)
                    {
                        if (transaction != null) transaction.Commit();
                    }
                    else
                    {
                        if (transaction != null) transaction.Rollback();
                    }
                }
            }

            conn_web.Close();
            conn_db.Close();
            return ret.result;

        }//SaveListGroup

        public List<Area_ls> LoadCurrentLsSupplier(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return null;
            }

            //-----------------------------------------------------------------------------------
            #endregion
            List<Area_ls> ls_list = new List<Area_ls>();
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            List<Area_ls> list = new List<Area_ls>();
            StringBuilder str = new StringBuilder();
            str.Append(
                "select a.nzp_kvar,a.numls,a.nzp_supp,a.comment,max(s.date_) as date_,s.nzp_user," +
                DBManager.sNvlWord + "(u.comment, u.name) as name,supp.name_supp" +
                " from " + finder.pref + "_data" + tableDelimiter + "alias_ls a " +
                "left outer join " + Points.Pref + "_data" + tableDelimiter + "sys_events s" +
                " on a.nzp_kvar=s.nzp and s.nzp_dict_event=6495 and s.note like '%('||a.nzp_supp||')%'" +
                " left outer join " + Points.Pref + "_data" + tableDelimiter + "users u on u.nzp_user=s.nzp_user," +
                finder.pref + "_kernel" + tableDelimiter + "supplier supp" +
                " where supp.nzp_supp=a.nzp_supp and a.nzp_kvar=" + finder.nzp_kvar + " group by 1,2,3,4,6,7,8");
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, str.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке текущего лицевого счета";
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Area_ls zap = new Area_ls();

                    zap.num = i.ToString();
                    zap.nzp_kvar = reader["nzp_kvar"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_kvar"]);
                    zap.numls = reader["numls"] == DBNull.Value ? "" : reader["numls"].ToString().Trim();
                    zap.nzp_supp = reader["nzp_supp"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_supp"]);
                    zap.supplier = reader["name_supp"] == DBNull.Value ? "" : reader["name_supp"].ToString().Trim();
                    zap.comment = reader["comment"] == DBNull.Value ? "" : reader["comment"].ToString();
                    zap.changes = (reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim()) + (reader["date_"] == DBNull.Value ? "" : " (" + reader["date_"].ToString() + ")");
                    list.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return list;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки кодов поставщиков текущего лицевого счета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<KP50.Interfaces.Group> LoadCurrentLsGroup(KP50.Interfaces.Group finder, out Returns ret) //загрузить группы текущего лицевого счета
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return null;
            }

            //-----------------------------------------------------------------------------------
            #endregion

            List<Group> Spis = new List<Group>();

            Spis.Clear();

            //выбрать общее кол-во
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string pref = finder.pref + "_data.";
            //выбрать группы текущего лицевого счета
            string sqlStr =
                 " Select '" + finder.pref + "' as pref , " +
                 "   g.nzp_group, g.ngroup " +
                 " From " + pref + "link_group l, " + pref + "s_group g " +
                 " Where l.nzp_group = g.nzp_group " +
                 "   and l.nzp = " + finder.nzp_kvar.ToString() +
                 " order by g.ngroup ";
#else
            string pref = finder.pref + "_data:";
            //выбрать группы текущего лицевого счета
            string sqlStr =
                 " Select '" + finder.pref + "' as pref , " +
                 "   g.nzp_group, g.ngroup " +
                 " From " + pref + "link_group l, " + pref + "s_group g " +
                 " Where l.nzp_group = g.nzp_group " +
                 "   and l.nzp = " + finder.nzp_kvar.ToString() +
                 " order by g.ngroup ";
#endif

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sqlStr, true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке групп текущего лицевого счета";
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Group zap = new KP50.Interfaces.Group();

                    zap.num = i.ToString();

                    // код группы
                    if (reader["nzp_group"] == DBNull.Value)
                        zap.nzp_group = 0;
                    else
                        zap.nzp_group = (int)reader["nzp_group"];
                    // название группы
                    if (reader["ngroup"] == DBNull.Value)
                        zap.ngroup = "";
                    else
                        zap.ngroup = (string)reader["ngroup"];

                    zap.pref = (string)reader["pref"];

                    Spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки групп текущего лицевого счета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//LoadCurrentLsGroup

        public Area_ls LoadCurrentAliasLs(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return null;
            }

            //-----------------------------------------------------------------------------------
            #endregion
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            StringBuilder str = new StringBuilder();
            str.Append("select a.nzp_kvar,a.numls,a.nzp_supp,a.comment,max(s.date_) as date_,s.nzp_user,u.comment as name,supp.name_supp from " + finder.pref + "_data" + tableDelimiter + "alias_ls a left outer join " + finder.pref + "_data" + tableDelimiter
                + "sys_events s on a.nzp_kvar=s.nzp and s.nzp_dict_event=6495 and s.note like '%('||a.nzp_supp||')%' " +
                " left outer join " + Points.Pref + "_data" + tableDelimiter + "users u on u.web_user=s.nzp_user," +
                finder.pref + "_kernel" + tableDelimiter + "supplier supp where supp.nzp_supp=a.nzp_supp and a.nzp_kvar=" + finder.nzp_kvar + "and a.nzp_supp=" + finder.nzp_supp + " group by 1,2,3,4,6,7,8");
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, str.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке текущего лицевого счета";
                return null;
            }

            try
            {
                Area_ls zap = new Area_ls();
                while (reader.Read())
                {
                    zap.nzp_kvar = reader["nzp_kvar"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_kvar"]);
                    zap.numls = reader["numls"] == DBNull.Value ? "" : reader["nzp_kvar"].ToString();
                    zap.nzp_supp = reader["nzp_supp"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_supp"]);
                    zap.supplier = reader["name_supp"] == DBNull.Value ? "" : reader["name_supp"].ToString().Trim();
                    zap.comment = reader["comment"] == DBNull.Value ? "" : reader["comment"].ToString();
                    zap.changes = (reader["name"] == DBNull.Value ? "" : reader["name"].ToString().Trim()) + (reader["date_"] == DBNull.Value ? "" : " (" + reader["date_"].ToString() + ")");
                }

                reader.Close();
                conn_db.Close();
                return zap;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки кодов поставщиков текущего лицевого счета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }

        /// <summary>
        /// Получить список групп, удовлетворяющих условиям поиска
        /// </summary>
        /// <param name="finder">Объект для поиска</param>
        /// <param name="ret">Объект с результатами поиска</param>
        /// <returns>Список групп</returns>
        public List<Group> GetListGroup(Group finder, out Returns ret)
        {
            ret = Utils.InitReturns(); //Инициализация результирующего объекта

            #region Проверка входных данных
            if (!(finder.nzp_user > 0))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Пользователь не известен";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return null;
            }
            #endregion

            #region Условия поиска
            string where = "";
            if (finder.nzp_group > 0) where += " and nzp_group = " + finder.nzp_group;
            if (finder.dopFind != null && finder.dopFind.Count > 0) where += " and nzp_group in (" + finder.dopFind[0] + ")";

            string skip = "";
            if (finder.skip > 0)
            {
#if PG
                skip = " offset " + finder.skip;
#else
                skip = " skip " + finder.skip;
#endif
            }

            #endregion

            #region Наименование таблицы
#if PG
            string table = finder.pref + "_data.s_group";
#else
            string table = finder.pref + "_data:s_group";
#endif
            #endregion

            #region запрос
#if PG
            string sql = "select * from " + table + " where 1=1 " + where + " " + skip;
#else
            string sql = "select" + skip + " * from " + table + " where 1=1 " + where;
#endif
            #endregion

            #region Соединение с БД
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            #region определить число записей
            int total_record_count = 0;
            object count = ExecScalar(conn_db, " Select count(*) From " + table + " where 1=1 " + where, out ret, true);
            if (ret.result)
            {
                total_record_count = Convert.ToInt32(count);
            }
            #endregion

            #region Выполнить запрос
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            #endregion

            #region Получить данные
            List<Group> list = new List<Group>();
            Group group;
            try
            {
                int row = 0;
                while (reader.Read())
                {
                    group = new Group();
                    row++;
                    group.num = row.ToString();
                    if (reader["nzp_group"] != DBNull.Value) group.nzp_group = (int)reader["nzp_group"];
                    if (reader["ngroup"] != DBNull.Value) group.ngroup = (string)reader["ngroup"];
                    list.Add(group);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка при определении списка объектов на карте: " + err, MonitorLog.typelog.Error, 20, 201, true);
            }
            #endregion

            #region закрыть соединение
            ret.tag = total_record_count;
            conn_db.Close();
            #endregion

            return list;
        }

        //----------------------------------------------------------------------
        public void FindGroupLs(Group finder, out Returns ret) //найти и заполнить список ПУ
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (!(finder.nzp_user > 0))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Пользователь не известен";
                return;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return;
            }
            //-----------------------------------------------------------------------------------
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
#if PG
            ret = ExecSQL(conn_web, " set search_path to 'public'", true);
            if (!ret.result) return;
#endif

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user) + "_s_group";


            ExecSQL(conn_web, " Drop table " + tXX_cnt, false);

            //создать таблицу webdata:tXX_cnt
            ret = ExecSQL(conn_web,
                      " Create table " + tXX_cnt +
                      " ( nzp_group integer, " +
                      "   ngroup    char(80)," +
                      "   txt1      char(60)," +
                      "   txt2      char(60)," +
                      "   pref      char(20)  " +
                      " ) ", true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //заполнить webdata:tXX_cnt
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

#if PG
            string tXX_cnt_full = /*pgDefaultSchema*/ "public." + tXX_cnt;
#else
            string tXX_cnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cnt;

#endif
            #region Условия поиска
            //-----------------------------------------------------------------------------------          
            string where = "";
            if (finder.nzp_group > 0) where += " and nzp_group = " + finder.nzp_group;
            if (finder.ngroup.Trim() != "") where += " and lower(ngroup) like '%" + finder.ngroup.Trim().ToLower() + "%'";
            /*  if (finder.nzp_kvar > 0)
              {
                  where += " and nzp_group not in (select l.nzp_group from "+finder.pref+"_data:link_group l where l.nzp = "+finder.nzp_kvar+")";
              }*/
            //-----------------------------------------------------------------------------------     
            #endregion

            StringBuilder sql = new StringBuilder();

            sql.Append(" Insert into " + tXX_cnt_full +
                           " (pref, nzp_group, ngroup, txt1, txt2) ");
#if PG
            sql.Append(" Select distinct '" + finder.pref + "', g.nzp_group, g.ngroup, g.txt1, g.txt2 ");
#else
            sql.Append(" Select unique '" + finder.pref + "', g.nzp_group, g.ngroup, g.txt1, g.txt2 ");
#endif
#if PG
            sql.Append(" From " + finder.pref + "_data.s_group g ");
#else
            sql.Append(" From " + finder.pref + "_data:s_group g ");

#endif
            sql.Append(" where 1=1");
            sql.Append(where);

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            conn_db.Close(); //закрыть соединение с основной базой

            //далее работаем с кешем
            //создаем индексы на tXX_cnt
            string ix = "ix" + Convert.ToString(finder.nzp_user) + "_s_group";

            ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_cnt + " (nzp_group,pref) ", true);
            if (ret.result)
            {
#if PG
                ret = ExecSQL(conn_web, " analyze  " + tXX_cnt, true);
#else
                ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_cnt, true);

#endif
            }

            conn_web.Close();

            return;
        }//FindGroupLs

        public List<_RajonDom> FindRajonDom(Finder finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс базы данных");
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

#if PG
            string sql = "Select nzp_raj_dom, rajon_dom, alt_rajon_dom from " + finder.pref + "_data.s_rajon_dom Order by rajon_dom";
#else
            string sql = "Select nzp_raj_dom, rajon_dom, alt_rajon_dom from " + finder.pref + "_data:s_rajon_dom Order by rajon_dom";

#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<_RajonDom> list = new List<_RajonDom>();
            try
            {
                while (reader.Read())
                {
                    _RajonDom raj = new _RajonDom();
                    raj.nzp_raj_dom = reader["nzp_raj_dom"] != DBNull.Value ? Convert.ToInt32(reader["nzp_raj_dom"]) : 0;
                    raj.rajon_dom = reader["rajon_dom"] != DBNull.Value ? Convert.ToString(reader["rajon_dom"]).Trim() : "";
                    raj.alt_rajon_dom = reader["alt_rajon_dom"] != DBNull.Value ? Convert.ToString(reader["alt_rajon_dom"]).Trim() : "";

                    list.Add(raj);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка районоа " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                list.Clear();
            }
            if (reader != null) reader.Close();
            conn_db.Close();
            return list;
        }

        /// <summary>
        /// создать новую группу
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CreateNewGroup(Group finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user <= 0) return new Returns(false, "Не задан пользователь", -1);
            if (finder.ngroup == "") return new Returns(false, "Не задано наименование группы", -1);
            if (finder.pref.Trim() == "") return new Returns(false, "Не задан префикс", -1);

            string sql = "";
            IDataReader reader;

            #region Соединение с БД
            IDbConnection conn_db = GetConnection(Points.GetConnByPref(finder.pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            try
            {
                if (finder.nzp_group > 0)
                {
                    #region редактирования названия существующего

                    sql = " SELECT nzp_group FROM " + Points.Pref + sKernelAliasRest + "s_group_check" +
                          " WHERE nzp_group = " + finder.nzp_group;
                    DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                    if (dt.Rows.Count > 0) return new Returns(false, "Нельзя менять название системной группе", -1);

                    sql = " UPDATE " + finder.pref + sDataAliasRest + "s_group" +
                          " SET ngroup = " + Utils.EStrNull(finder.ngroup.Trim().ToUpper()) +
                          " WHERE nzp_group = " + finder.nzp_group;

                    ret = ExecRead(conn_db, out reader, sql, true);
                    #endregion
                }
                else
                {
                    #region Добавить новую группу

            #region Проверить существование группы в бд

                    sql = " select count(*) as num from " + finder.pref + sDataAliasRest + "s_group" +
                          " where ngroup = " + Utils.EStrNull(finder.ngroup.Trim().ToUpper());

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            int num = 0;
            try
            {
                if (reader.Read())
                    if (reader["num"] != DBNull.Value) num = Convert.ToInt32(reader["num"]);
                if (num > 0)
                {
                    conn_db.Close();
                    return new Returns(false, "Группа с таким наименованием уже существует", -1);
                }
            }
            catch (Exception ex)
            {
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                        MonitorLog.WriteLog("Ошибка при создании новой группы " + err, MonitorLog.typelog.Error, 20, 201,
                            true);
                return new Returns(false, ex.Message);
            }
                    finally
                    {
                        reader.Close();
                    }

            #endregion

                    sql = "insert into " + finder.pref + sDataAliasRest + "s_group (ngroup)" +
                          " values (" + Utils.EStrNull(finder.ngroup.Trim().ToUpper()) + ")";

            ret = ExecSQL(conn_db, sql, true);
                    conn_db.Close();

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при создании новой группы " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
            conn_db.Close();
            }
            return ret;
        }

        public Returns DeleteGroup(Group finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Не задан пользователь", -1);
            if (finder.nzp_group < 0) return new Returns(false, "Не выбрана группа для удаления", -1);
            if (finder.pref.Trim() == "") return new Returns(false, "Не задан префикс", -1);

            #region Соединение с БД
            var connDb = GetConnection(Points.GetConnByPref(finder.pref));
            var ret = OpenDb(connDb, true);
            if (!ret.result) return ret;
            #endregion

            try
            {
                var sql = " SELECT nzp_group FROM " + Points.Pref + sKernelAliasRest + "s_group_check" +
                      " WHERE nzp_group = " + finder.nzp_group;
                var dt = DBManager.ExecSQLToTable(connDb, sql);
                if (dt.Rows.Count > 0) return new Returns(false, "Нельзя удалять системную группу", -1);

                sql = " delete FROM " + finder.pref + sDataAliasRest + "link_group" +
                        " WHERE nzp_group = " + finder.nzp_group;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result) return ret;

                sql = " delete from " + finder.pref + sDataAliasRest + "s_group" +
                        " WHERE nzp_group = " + finder.nzp_group;
                ret = ExecSQL(connDb, sql, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при удалении группы " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDb.Close();
            }
            return ret;
        }

        public void AddIntoLinkGroup(ref Returns ret, IDbConnection connection, string pref, int ngroup, int nzp_user)
        {
            ExecSQL(string.Format("insert into {0}link_group select {1},nzp_kvar from t{2}_spls ", pref + DBManager.sDataAliasRest, ngroup, nzp_user));
        }

        public void DeleteSupplierLs(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            #region Проверка входных данных
            //-----------------------------------------------------------------------------------
            if (finder.pref == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Префикс не задан";
                return;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Лицевой счет не задан";
                return;
            }
            #endregion
            string conn_kernel = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;
            string str = "delete from " + finder.pref + "_data" + tableDelimiter + "alias_ls where nzp_kvar=" + finder.nzp_kvar + " and nzp_supp=" + finder.nzp_supp;
            if (!ExecSQL(conn_db, str.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при удалении кода лицевого счета";
                return;
            }
            conn_db.Close();
            return;
        }

        /// <summary>
        /// Определить состояние ЛС на stateValidOn или сегодня
        /// </summary>
        public Returns LoadLsState(Ls finder, IDbConnection connection, IDbTransaction transaction)
        {
            Returns ret = Utils.InitReturns();

            DateTime d;
            if (!DateTime.TryParse(finder.stateValidOn, out d)) d = DateTime.Now;

#if PG
            string res_y = Points.Pref + "_kernel.res_y";
#else
            string res_y = Points.Pref + "_kernel@" + DBManager.getServer(connection) + ":res_y";

#endif
            string s = "";

            if (!GlobalSettings.WorkOnlyWithCentralBank)
            {
                if (finder.pref == "") return new Returns(false, "Не указан префикс БД");

#if PG
                string prm_3 = finder.pref + "_data.prm_3";
#else
                string prm_3 = finder.pref + "_data@" + DBManager.getServer(connection) + ":prm_3";
#endif

                if (!TempTableInWebCashe(connection, transaction, prm_3)) return ret;

#if PG
                s = " Select p.dat_s, ry.nzp_y, ry.name_y" +
                    " From " + prm_3 + " p, " + res_y + " ry " +
                    " Where nzp = " + finder.nzp_kvar +
                        " and nzp_prm = 51 and is_actual <> 100 " +
                        " and to_date('" + d.Month + "," + d.Day + "," + d.Year + "', 'MM,DD,YYYY') between  dat_s and  dat_po " +
                        " and ry.nzp_res = 18 and trim(p.val_prm) = trim(ry.nzp_y||'')";
#else
                s = " Select p.dat_s, ry.nzp_y, ry.name_y" +
                    " From " + prm_3 + " p, " + res_y + " ry " +
                    " Where nzp = " + finder.nzp_kvar +
                        " and nzp_prm = 51 and is_actual <> 100 " +
                        " and mdy(" + d.Month + "," + d.Day + "," + d.Year + ") between  dat_s and  dat_po " +
                        " and ry.nzp_res = 18 and trim(p.val_prm) = trim(ry.nzp_y||'')";
#endif

            }
            else
            {
                if (d < System.Convert.ToDateTime(Points.DateOper.Date))
                {
#if PG
                    s = " Select to_date('" + d.Month + "," + d.Day + "," + d.Year + "', 'MM,DD,YYYY') dat_s, ry.nzp_y, ry.name_y" +
                        " From " + res_y + " ry " +
                        " Where ry.nzp_res = 18 and \'3\' = trim(ry.nzp_y||'')";
#else
                    s = " Select mdy(" + d.Month + "," + d.Day + "," + d.Year + ") dat_s, ry.nzp_y, ry.name_y" +
                        " From " + res_y + " ry " +
                        " Where ry.nzp_res = 18 and \'3\' = trim(ry.nzp_y||'')";
#endif

                }
                else
                {
#if PG
                    string kvar = Points.Pref + "_data.kvar";
#else
                    string kvar = Points.Pref + "_data@" + DBManager.getServer(connection) + ":kvar";

#endif
#if PG
                    s = " Select to_date('" + System.Convert.ToDateTime(Points.DateOper.Date).ToString("MM,dd,yyyy") + "', 'MM,DD,YYYY') dat_s, ry.nzp_y, ry.name_y" +
                        " From " + kvar + " k, " + res_y + " ry " +
                        " Where k.nzp_kvar = " + finder.nzp_kvar +
                            " and ry.nzp_res = 18 and trim(k.is_open) = trim(ry.nzp_y||'')";
#else
                    s = " Select mdy(" + System.Convert.ToDateTime(Points.DateOper.Date).ToString("MM,dd,yyyy") + ") dat_s, ry.nzp_y, ry.name_y" +
                        " From " + kvar + " k, " + res_y + " ry " +
                        " Where k.nzp_kvar = " + finder.nzp_kvar +
                            " and ry.nzp_res = 18 and trim(k.is_open) = trim(ry.nzp_y||'')";
#endif

                }
            }

            IDataReader readerDO;
            ret = ExecRead(connection, transaction, out readerDO, s, true);
            if (!ret.result)
            {
                return ret;
            }

            if (readerDO.Read())
            {
                if (readerDO["dat_s"] != DBNull.Value)
                {
                    d = Convert.ToDateTime(readerDO["dat_s"]);
                    if (d > new DateTime(1900, 1, 1))
                        finder.stateValidOn = d.ToShortDateString();
                }
                if (readerDO["nzp_y"] != DBNull.Value) finder.stateID = (int)readerDO["nzp_y"];
                if (readerDO["name_y"] != DBNull.Value) finder.state = ((string)readerDO["name_y"]).Trim();
            }
            readerDO.Close();
            return ret;
        }
    }
}
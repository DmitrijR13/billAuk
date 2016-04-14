using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Bars.KP50.DataImport.SOURCE.EXCHANGE;
using Bars.KP50.Utils;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport
{
    public class DBMakePacks : DbAdminClient
    {
        private readonly IDbConnection conn_db;

        public StringBuilder sbPacks { get; private set; }

        public DBMakePacks(IDbConnection con_db)
        {
            conn_db = con_db;
            sbPacks = new StringBuilder();
        }

        public Returns MakePacks(FilesImported finder)
        {
            Returns ret = new Returns();

            DbFileLoader fl = new DbFileLoader(conn_db);
            try
            {
                string sql = "";

                #region проверяем количество выбранных файлов
                if (finder.selectedFiles.Count != 1)
                {
                    ret.text = "Для формирования пачки должен быть выбран один файл. Выбрано:" + finder.selectedFiles.Count;
                    ret.result = false;
                    ret.tag = -1;
                    return ret;
                }
                finder.nzp_file = Convert.ToInt32(finder.selectedFiles[0]);
                #endregion

                #region выбираем всех поставщиков и по каждому формируем пачку

                if ((new string[] { "1.2.1", "1.2.2" }).Count(ver => ver == finder.format_version) > 0) 
                {
                    sql =
                        " SELECT DISTINCT s.nzp_supp as nzp_supp " +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_supp s" +
                        " WHERE s.supp_id = fs.supp_id AND s.nzp_file = fs.nzp_file AND " +
                        " fs.nzp_file = " + finder.nzp_file;
                }
                else //if ((new string[] { "1.3.2", "1.3.3", "1.3.4", "1.3.5", "1.3.6", "1.3.7", "1.3.8" }).Count(ver => ver == finder.format_version) > 0) 
                {
                    sql =
                        " SELECT DISTINCT s.nzp_supp as nzp_supp " +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_dog s" +
                        " WHERE s.dog_id = fs.dog_id AND s.nzp_file = fs.nzp_file AND " +
                        " fs.nzp_file = " + finder.nzp_file;
                }
                DataTable dtSupp = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                foreach (DataRow r in dtSupp.Rows)
                {
                    ret = MakeOnePackOplat(Convert.ToInt32(r["nzp_supp"].ToString()), finder);
                    if (!ret.result)
                    {
                        ret.tag = -1;
                        return ret;
                    }
                }
                #endregion


                string fullPath = InputOutput.GetInputDir() + "u_" + finder.nzp_user + "ПачкаОплатПофайлу_" + finder.nzp_file +"_"+ DateTime.Now.Ticks + ".txt";

               // DbDataUnload exch = new DbDataUnload();
                string fn4 = "";
                
                finder.nzp_exc = DbFileLoader.AddMyFile("Пачки оплат по файлу" + finder.nzp_file, finder);
                var files = new Dictionary<StringBuilder, string>();
                files.Add(sbPacks, fullPath);
                ret = DbDataUnload.Compress(files);
                if (!ret.result)
                {
                    return ret;
                }

                fullPath = ret.text;

                if (InputOutput.useFtp)
                {
                    fn4 =
                        InputOutput.SaveInputFile(fullPath);
                }

                fl.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = finder.nzp_exc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path =
                        InputOutput.useFtp
                            ? fn4
                            : fullPath
                });
            }
            catch (Exception ex)
            {
                fl.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = finder.nzp_exc,
                    status = ExcelUtility.Statuses.Failed
                });
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка формирования пачек оплат по файлу наследуемой информации";
                MonitorLog.WriteLog("Ошибка формирования пачек оплат по файлу наследуемой информации: " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;

            }

            return ret;
        }

        private Returns MakeOnePackOplat(int nzp_supp, FilesImported finder)
        {
            Returns ret = new Returns();

            try
            {
                string sql;
                var ms = new MemoryStream();
                StringBuilder sb = new StringBuilder();
                 
                #region переменные для полей с постоянными для всей пачки значениями

                #region константы и поля для пачки оплат
                //наимнование пункта приема платежа
                string ppp_name = "from load file";
                //номер пачки (номер файла код поставщика)
                string num_pack = finder.nzp_file.ToString() + nzp_supp.ToString();
                if (num_pack.Length > 9) num_pack = num_pack.Substring(0, 9);
                // дата формирования пачки (1 число след месяца)
                string dat_form;
                //дата операционного дня (1 число след месяца)
                string oper_day;
                #endregion

                #region константы и поля для строк оплат
                //код вида платежа
                int pay_type = 50;
                //источник платежа
                int pay_from = 1;
                //день, месяц, год получения платежа (последний день месяца)
                string dat_plat;
                //месяц и год, за который произведена оплата
                string month_opl;
                #endregion

                //формат файла
                string format = "!1.00";
                //нулевое числовое поле
                int zero_num = 0;
                //нулевая строка 
                string zero_string = "0000";


                #region заполняем даты

                sql =
                    " SELECT calc_date" +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_head " +
                    " WHERE nzp_file = " + finder.nzp_file;
                DataTable Date = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                string calc_date = Date.Rows[0]["calc_date"].ToString().Substring(0,10);
                month_opl = calc_date.Substring(0, 10);
                dat_plat =
                    DateTime.DaysInMonth(Convert.ToInt32(calc_date.Substring(6, 4)),
                        Convert.ToInt32(calc_date.Substring(3, 2))).ToString() + "." +
                    calc_date.Substring(3, 7);
                DateTime dt = new DateTime(Convert.ToInt32(calc_date.Substring(6, 4)), Convert.ToInt32(calc_date.Substring(3, 2)), 1);
                DateTime dt1 = dt.AddMonths(1);
                dat_form = "01." + dt1.Month.ToString("00") + "." + dt1.Year.ToString("0000");
                oper_day = dat_form;
               
                #endregion

                #endregion

                #region формируем и заполняем временную табличку

                string table = "_temp_table_info_pack";
                try
                {
                    sql = "DROP TABLE " + table;
                    ret = ExecSQL(conn_db, sql, false);
                }
                catch { }

                sql = " CREATE TEMP TABLE " + table + "(" +
                      " pkod " + DBManager.sDecimalType + "(13,0)," +
                      " nzp_kvar " + DBManager.sDecimalType + "(14,0)," +
                      " nzp_area " + DBManager.sDecimalType + "(14,0)," +
                      " sum_nach " + DBManager.sDecimalType + "(14,2)," +
                      " sum_money " + DBManager.sDecimalType + "(14,2)," +
                      " nzp_serv " + DBManager.sDecimalType + "(14,0) ) " + 
                      DBManager.sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);


                if ((new string[] { "1.2.1", "1.2.2" }).Count(ver => ver == finder.format_version) > 0) 
                {
                    sql =
                        " INSERT INTO " + table +
                        " ( pkod, nzp_kvar, nzp_area, sum_nach, sum_money, nzp_serv )" +
                        " SELECT DISTINCT k.pkod, fk.nzp_kvar, k.nzp_area, fs.sum_nach, fs.sum_money, fss.nzp_serv " +
                        " FROM " + finder.bank + "_data" + tableDelimiter + "kvar k, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_serv fs, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_services fss, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_supp s, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                        " WHERE k.nzp_kvar = fk.nzp_kvar AND fs.ls_id = fk.id AND s.supp_id = fs.supp_id " +
                        " AND fss.id_serv = fs.nzp_serv and s.nzp_supp = " + nzp_supp +
                        " AND fs.nzp_file = fss.nzp_file AND fs.nzp_file = s.nzp_file AND" +
                        " fs.nzp_file = fk.nzp_file AND" + //" fs.nzp_file = fa.nzp_file AND " +
                        " fs.nzp_file = " + finder.nzp_file + //" AND k.nzp_area = fa.nzp_area" +
                        //" AND " + sNvlWord + "(k.pkod, 0) > 0" +
                        " AND fs.sum_money > 0";
                }
                else //if ((new string[] { "1.3.2", "1.3.3", "1.3.4", "1.3.5", "1.3.6", "1.3.7", "1.3.8" }).Count(ver => ver == finder.format_version) > 0) 
                {
                    sql =
                        " INSERT INTO " + table +
                        " ( pkod, nzp_kvar, nzp_area, sum_nach, sum_money, nzp_serv )" +
                        " SELECT DISTINCT k.pkod, fk.nzp_kvar, k.nzp_area, fs.sum_nach, fs.sum_money, fss.nzp_serv " +
                        " FROM " + finder.bank + "_data" + tableDelimiter + "kvar k, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_serv fs, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_services fss, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_dog s, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                        " WHERE k.nzp_kvar = fk.nzp_kvar AND fs.ls_id = fk.id AND s.dog_id = fs.dog_id " +
                        " AND fss.id_serv = fs.nzp_serv and s.nzp_supp = " + nzp_supp +
                        " AND fs.nzp_file = fss.nzp_file AND fs.nzp_file = s.nzp_file AND" +
                        " fs.nzp_file = fk.nzp_file AND" + //" fs.nzp_file = fa.nzp_file AND " +
                        " fs.nzp_file = " + finder.nzp_file + //" AND k.nzp_area = fa.nzp_area" +
                        //" AND " + sNvlWord + "(k.pkod, 0) > 0" +
                        " AND fs.sum_money > 0";
                    
                }
                ret = ExecSQL(conn_db, sql, true);
                #endregion

                #region Формируем заголовок пачки оплат

                sql = 
                    " SELECT nzp_area, SUM(sum_nach) as sum_nach, SUM(sum_money) as sum_money, count(distinct nzp_kvar) as kol " +
                    " FROM " + table + 
                    " GROUP BY 1";
                DataTable dtHead = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                if (dtHead.Rows.Count == 0) return ret;
                var head = "###" + "|" + ppp_name + "|" + dtHead.Rows[0]["nzp_area"] + "|" + num_pack + 
                    "|" + DateTime.Today.ToString("dd.MM.yyyy") + "|" + oper_day +
                    "|" + dtHead.Rows[0]["kol"] + "|" + dtHead.Rows[0]["sum_nach"] + "|" + dtHead.Rows[0]["sum_money"] +
                    "|" + nzp_supp + "|" + dtHead.Rows[0]["sum_money"] + "|" + zero_num + "|" + format + "|";

                sb.Append(head + Environment.NewLine);
                #endregion

                #region формируем строки по ЛС

                string oneLS;

                sql =
                    " select nzp_kvar, nzp_area, SUM(sum_nach) as sum_nach, SUM(sum_money) as sum_money " +
                    " FROM " + table +
                    " GROUP BY 1, 2";
                DataTable dtLs = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                foreach (DataRow r in dtLs.Rows)
                {
                    #region формируем значения для вставки
                    //номер справки формируем из номера ЛС:если длина ЛС больше 9, обрезаем, меньше - заполняем 10...0
                    int sprav_num;
                    if (r["nzp_kvar"].ToString().Length > 9)
                    {
                        sprav_num = Convert.ToInt32(r["nzp_kvar"].ToString().Substring(0, 9));
                    }
                    else
                        sprav_num = Convert.ToInt32(r["nzp_kvar"].ToString().PadLeft(8, '0').PadLeft(9, '1'));
                    //по услугам
                    var serv = "";
                    sql =
                        " SELECT nzp_serv, sum_money" +
                        " FROM " + table +
                        " WHERE nzp_kvar = " + r["nzp_kvar"].ToString();
                    DataTable dtServ = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                    foreach (DataRow rr in dtServ.Rows)
                    {
                        serv += rr["nzp_serv"].ToString() + "," + "," + String.Format(rr["sum_money"].ToString(), "C")  + ";";
                    }
                    int kol_str_serv = dtServ.Rows.Count;
                    #endregion

                    oneLS = 
                        "@@@" + "|" + sprav_num + "|" + r["nzp_area"] + "|" + pay_type + "|" + pay_from + 
                        "|" + r["nzp_kvar"] + "|" + dat_plat + "|" + calc_date + "|" + zero_string + 
                        "|" + r["sum_nach"] + "|" + r["sum_money"] + "|" + zero_num + "|" + zero_num +
                        "|" + kol_str_serv + "|" + r["sum_money"] + "|" + serv + "|" + "|";


                    sb.Append(oneLS + Environment.NewLine);
                }

                #endregion

                //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\Pack.txt", true))
                //{
                //    file.Write(sb.ToString());
                //}
                sbPacks.Append(sb.ToString());
    
                byte[] inputBytes = Encoding.ASCII.GetBytes(sb.ToString());
                ms.Write(inputBytes, 0, inputBytes.Length);
                AddedPacksInfo insertedPackInfo= new AddedPacksInfo();
                DbPack db = new DbPack();
                DataTable dtPack = db.LoadUniversalFormat(ms, "Из файла наследуемой информации " + finder.nzp_file.ToString(), insertedPackInfo);
                if (dtPack.IsNotNull() && dtPack.Rows.Count > 0)
                {
                    StringBuilder error = new StringBuilder();
                    foreach (DataRow r in dtPack.Rows)
                    {
                        error.Append(r["mes"] + Environment.NewLine);
                    }
                    ret.result = false;
                    ret.text = "Ошибка формирования пачки оплат для поставщика с кодом " + nzp_supp +
                               ". Смотрите журнал ошибок.";
                    MonitorLog.WriteLog(
                        "Ошибка формирования пачки оплат для поставщика с кодом " + nzp_supp + Environment.NewLine +
                        error.ToString(), MonitorLog.typelog.Error, true);
                    return ret;
                }



            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка формирования пачки оплат для поставщика с кодом " + nzp_supp +
                           ". Смотрите журнал ошибок.";
                MonitorLog.WriteLog("Ошибка формирования пачки оплат для поставщика с кодом " + nzp_supp +
                           ": " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }
            
            return ret;
        }
    }
}

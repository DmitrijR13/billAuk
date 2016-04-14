using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Gubkin
{
    public class Oplats : DbAdminClient
    {
        // Считывание реестра Сбербанка файл 2
        public Returns ReadReestrFromCbb(FilesImported finderpack, FilesImported finder, string connectionString)
        {
            Returns ret = Utils.InitReturns();


            //считали пачки
            ret = ReadReestrPack(finderpack);

            // Thread.Sleep(5000);
            // ret = ReadReestrPack(finder);

            //подключение к БД
            string connectionString2 = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString2);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;


            #region Считываем реестр каждую операцию

            //директория файла
            // string fDirectory = Path.Combine(Constants.FilesDir, Constants.ImportDir).Replace("/", "\\");
            string fDirectoryPackLs = Constants.Directories.ImportDir.Replace("/", "\\");
            string fileNamePackLs = Path.Combine(fDirectoryPackLs, finder.saved_name);
            if (InputOutput.useFtp) InputOutput.DownloadFile(finder.saved_name, fileNamePackLs);

            #region Считываем файл
            byte[] bufferPackLs = new byte[0];

            if (System.IO.File.Exists(fileNamePackLs) == false)
            {
                ret.result = false;
                ret.text = "Файл отсутствует по указанному пути";
                ret.tag = -1;
                return ret;
            }
            System.IO.FileStream fstreamPackLs;
            try
            {
                fstreamPackLs = new System.IO.FileStream(fileNamePackLs, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);


                bufferPackLs = new byte[fstreamPackLs.Length];
                fstreamPackLs.Position = 0;
                fstreamPackLs.Read(bufferPackLs, 0, bufferPackLs.Length);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка открытия файла " + fileNamePackLs + " " + ex.Message,
                    MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Файл недоступен по указанному пути";
                ret.tag = -1;
                return ret;
            }

            // fDirectory = "D:\\work.php\\KOMPLAT.50\\WEB\\WebKomplat5\\ExcelReport\\import\\";
            //имя файла
            //string fileName = Path.Combine(fDirectory, finder.saved_name).Replace("/", "\\");

            //#region Считываем файл
            //byte[] buffer = new byte[0];

            //MonitorLog.WriteLog(fileName, MonitorLog.typelog.Warn, 20, 201, true);
            ////if (System.IO.File.Exists(fileName) == false)
            ////{
            ////    ret.result = false;
            ////    ret.text = "Файл отсутствует по указанному пути " + fileName;
            ////    ret.tag = -1;

            ////    MonitorLog.WriteLog("Файл отсутствует по указанному пути " + fileName,
            ////        MonitorLog.typelog.Error, true);

            ////    return ret;
            ////}
            //System.IO.FileStream fstreamLS;
            //try
            //{
            //    fileName = @Path.GetFullPath(fileName);
            //    fstreamLS = new System.IO.FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
            //    buffer = new byte[fstreamLS.Length];
            //    fstreamLS.Position = 0;
            //    fstreamLS.Read(buffer, 0, buffer.Length);

            //}
            //catch (Exception ex)
            //{
            //    MonitorLog.WriteLog("Ошибка открытия файла " + fileName + " " + ex.Message,
            //        MonitorLog.typelog.Warn, true);
            //    ret.result = false;
            //    ret.text = "Файл недоступен по указанному пути";
            //    ret.tag = -1;
            //    return ret;
            //}


            string tehPlanFileString = System.Text.Encoding.GetEncoding(1251).GetString(bufferPackLs);
            string[] stSplit = { System.Environment.NewLine };
            string[] fileStrings = tehPlanFileString.Split(stSplit, StringSplitOptions.None);
            #endregion


            #region Переменные
            List<string> sqlStr = new List<string>();
            StringBuilder err = new StringBuilder();

            #endregion



            #region Формирование запросов
            foreach (string str in fileStrings)
            {
                //защита от пустых строк(пустые строки для сохранения нумерации)
                if (str.Trim() == "")
                {
                    continue;
                }

                ret = Utils.InitReturns();

                string sql = "";
                //массив значений строки
                string[] vals = str.Split(new char[] { '|' }, StringSplitOptions.None);
                Array.ForEach(vals, x => x = x.Trim());

                if (vals.Length == 0)
                {
                    continue;
                }

                //номер строки в файле
                string rowNumber = Environment.NewLine + " (строка " + (Array.IndexOf(fileStrings, str) + 1) + ") ";
                ret.text += rowNumber;

                int i = 0;



                sql = " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "_reestr ( datpaytorder, numpaytorder, datpayt, numfilial, kodoperator, numoperation, sumofpayment, persaccklient, filename)";

                sql += " VALUES (";


                //1. Дата платежного поручения
                string datPaytOrder = CheckType.CheckDateTime(vals[i], true, ref ret);
                if ((!ret.result) || (vals[i].Length != 10))
                {
                    err.Append(rowNumber + ret.text + " Дата платежного поручения");
                }
                sql += datPaytOrder + ", ";
                i++;

                //2. Номер платежного поручения с ведущими нулями
                string numPaytOrder = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                if ((!ret.result) || (vals[i].Length != 6))
                {
                    err.Append(rowNumber + ret.text + " Номер платежного поручения");
                }
                sql += numPaytOrder + ", ";
                i++;

                //3. Дата оплаты квитанции
                string datPayt = CheckType.CheckDateTime(vals[i], true, ref ret);
                if ((!ret.result) || (vals[i].Length != 10))
                {
                    err.Append(rowNumber + ret.text + " Дата платежного поручения");
                }
                sql += datPayt + ", ";
                i++;

                //4. Номер филиала
                string numFilial = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                if ((!ret.result) || (vals[i].Length != 5))
                {
                    err.Append(rowNumber + ret.text + " Номер филиала");
                }
                sql += numFilial + ", ";
                i++;

                //5. Код оператора, принявшего платеж
                string kodOperator = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                if ((!ret.result) || (vals[i].Length != 5))
                {
                    err.Append(rowNumber + ret.text + " Код оператора, принявшего платеж");
                }
                sql += kodOperator + ", ";
                i++;

                //6. Номер операции
                string numOperation = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                if ((!ret.result) || (vals[i].Length != 6))
                {
                    err.Append(rowNumber + ret.text + " Номер операции");
                }
                sql += numOperation + ", ";
                i++;

                //7. Сумма платежа с ведущими нулями               

                string sumOfPayment = CheckType.CheckDecimal(DeleteFirstZeros(vals[i]), true, false, 0, null, ref ret);
                if (!ret.result)
                {
                    err.Append(rowNumber + ret.text + " Сумма платежа");
                }
                if (sumOfPayment == "") sumOfPayment = "0";
                sql += sumOfPayment + ", ";
                i++;


                //8. л/с клиента
                string persAccKlient = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                if ((!ret.result) || (vals[i].Length != 7))
                {
                    err.Append(rowNumber + ret.text + " л/с клиента");
                }
                sql += persAccKlient + ", ";
                i++;

                //9.имя файла
                string filePackName = Convert.ToString(finder.loaded_name);
                string filePackName1 = Path.GetFileName(filePackName);
                if (filePackName1 == "")
                {
                    err.Append(rowNumber + ret.text + " Имя файла");
                }
                sql += "'" + filePackName1 + "') ";
                i++;

                if (sql.Trim() != "")
                {
                    sqlStr.Add(sql);
                }
            }

            fstreamPackLs.Close();
            #endregion


            #region Запись в БД
            ////IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ////string connectionString = Points.GetConnByPref(conn_db);
            //IDbConnection conn_db = GetConnection(connectionString);
            //ret = OpenDb(conn_db, true);
            //if (!ret.result) return ret;

            try
            {
                //очищаем табличку
                string sql = "delete from " + Points.Pref + "_data" + tableDelimiter + "_reestr";
                var dt1 = ClassDBUtils.OpenSQL(sql, conn_db);

                if (err.Length > 0)
                {
                    MonitorLog.WriteLog(err.ToString(), MonitorLog.typelog.Error, true);
                    ret.text = "Имеются ошибки в файле с оплатами по лицевым счетам";
                    ret.tag = -1;
                    ret.result = false;
                    return ret;
                }
                foreach (string s in sqlStr)
                {
                    ret = ExecSQL(conn_db, s, true);
                    if (!ret.result) return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры ReadReestrFromCbb : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }


            #endregion

            ret.text = "Файл успешно загружен.";
            ret.tag = -1;

            #endregion


            #region Проверка правильности файла



            bool isCorrect = true;
            int j = 0;
            //открыли табличку с пачками
            string sql1 = "select  numpaytorder, sumofpayt, numofpayt from " + Points.Pref + "_data" + tableDelimiter + "_reestrpack";
            var dt = ClassDBUtils.OpenSQL(sql1, conn_db);


            STCLINE.KP50.Utility.ClassLog.InitializeLog(AppDomain.CurrentDomain.BaseDirectory, "chargeLoad.log");


            #region Проверяем по каждой строчке таблицы пачек
            while ((isCorrect) && (j < dt.resultData.Rows.Count))
            {
                //номер платежного поручения
                int numPaytOrder = Convert.ToInt32(dt.resultData.Rows[j]["numpaytorder"].ToString().Trim());
                //количество платежей
                int numOfPayt = Convert.ToInt32(dt.resultData.Rows[j]["numofpayt"].ToString().Trim());
                //общая сумма платежей
                double sumOfPayt = Convert.ToDouble(dt.resultData.Rows[j]["sumofpayt"].ToString().Trim());

                sql1 = "select count(distinct a.id) as kolstr from " + Points.Pref + "_data" + tableDelimiter + "_reestr a," + Points.Pref + "_data" + tableDelimiter + "_reestrpack b where (b.numpaytorder = " + numPaytOrder + ")" +
                    "and(a.numpaytorder  = b.numpaytorder )";
                DataTable dtr1 = ClassDBUtils.OpenSQL(sql1, conn_db, ClassDBUtils.ExecMode.Log).GetData();
                int countStr = Convert.ToInt32(dtr1.Rows[0]["kolstr"]);

                if (numOfPayt != countStr)
                {
                    isCorrect = false;
                    ret.text = "Файлы некорректны, количество отдельных оплат не совпадает с количеством оплат в пачке ";
                    ret.result = false;
                    return ret;
                }

                sql1 = "select sum(distinct a.sumofpayment) as sum1 from " + Points.Pref + "_data" + tableDelimiter + "_reestr a," + Points.Pref + "_data" + tableDelimiter + "_reestrpack b where (b.numpaytorder = " + numPaytOrder + ")" +
                    "and(a.numpaytorder  = b.numpaytorder )";
                DataTable dtr2 = ClassDBUtils.OpenSQL(sql1, conn_db, ClassDBUtils.ExecMode.Log).GetData();
                double countSum = Convert.ToDouble(dtr2.Rows[0]["sum1"]);

                if (sumOfPayt != countSum) isCorrect = false;

                j++;

            }
            #endregion


            if (isCorrect == false)
            {
                ret.text = "Файлы некорректны, имеются ошибки связности. ";
                ret.result = false;
                return ret;
            }

            #endregion

            //  finally
            //{
            conn_db.Close();
            //}

            WriteToFin();
            File.Delete(fileNamePackLs);

            return ret;
        }

        //считывание пачек реестров
        public Returns ReadReestrPack(FilesImported finderpack)
        {
            Returns ret = Utils.InitReturns();



            #region считываем реестр с пачками
            //директория файла
            string fDirectoryPack = Constants.Directories.ImportDir.Replace("/", "\\");
            // fDirectory = "D:\\work.php\\KOMPLAT.50\\WEB\\WebKomplat5\\ExcelReport\\import\\";

            //имя файла
            string fileNamePack = Path.Combine(fDirectoryPack, finderpack.saved_name);
            if (InputOutput.useFtp) InputOutput.DownloadFile(finderpack.saved_name, fileNamePack);

            #region Считываем файл
            var bufferPack = new byte[0];

            if (File.Exists(fileNamePack) == false)
            {
                ret.result = false;
                ret.text = "Файл отсутствует по указанному пути";
                ret.tag = -1;
                return ret;
            }
            System.IO.FileStream fstreamPack;
            try
            {
                fstreamPack = new System.IO.FileStream(fileNamePack, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);


                bufferPack = new byte[fstreamPack.Length];
                fstreamPack.Position = 0;
                fstreamPack.Read(bufferPack, 0, bufferPack.Length);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка открытия файла " + fileNamePack + " " + ex.Message,
                    MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Файл недоступен по указанному пути";
                ret.tag = -1;
                return ret;
            }


            string tehPlanFileStringPack = System.Text.Encoding.GetEncoding(1251).GetString(bufferPack);
            string[] stSplitPack = { System.Environment.NewLine };
            string[] fileStringsPack = tehPlanFileStringPack.Split(stSplitPack, StringSplitOptions.None);
            #endregion

            #region Переменные
            List<string> sqlStrPack = new List<string>();
            StringBuilder errPack = new StringBuilder();

            #endregion

            //подключение к БД
            string connectionString2 = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString2);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region Формирование запросов
            foreach (string str in fileStringsPack)
            {
                //защита от пустых строк(пустые строки для сохранения нумерации)
                if (str.Trim() == "")
                {
                    continue;
                }

                ret = Utils.InitReturns();

                string sql = "";
                //массив значений строки
                string[] vals = str.Split(new char[] { '|' }, StringSplitOptions.None);
                Array.ForEach(vals, x => x = x.Trim());

                if (vals.Length == 0)
                {
                    continue;
                }

                //номер строки в файле
                string rowNumber = Environment.NewLine + " (строка " + (Array.IndexOf(fileStringsPack, str) + 1).ToString() + ") ";
                ret.text += rowNumber;

                int i = 0;



                sql = " INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "_reestrpack ( datpaytorder, numpaytorder, sumofpayt, numofpayt, filename)";
                sql += " VALUES (";


                //1. Дата платежного поручения
                string datPaytOrder = CheckType.CheckDateTime(vals[i], true, ref ret);
                if ((!ret.result) || (vals[i].Length != 10))
                {
                    errPack.Append(rowNumber + ret.text + " Дата платежного поручения");
                }
                sql += datPaytOrder + ", ";
                i++;

                //2. Номер платежного поручения с ведущими нулями
                string numPaytOrder = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                if ((!ret.result) || (vals[i].Length != 6))
                {
                    errPack.Append(rowNumber + ret.text + " Номер платежного поручения");
                }
                sql += numPaytOrder + ", ";
                i++;

                //3. Сумма платежа с ведущими нулями  
                string sumOfPayment = CheckType.CheckDecimal(DeleteFirstZeros(vals[i]), true, false, 0, null, ref ret);
                if (!ret.result)
                {
                    errPack.Append(rowNumber + ret.text + " Сумма платежа");
                }
                if (sumOfPayment == "") sumOfPayment = "0";
                sql += sumOfPayment + ", ";
                i++;

                //4. Количество пачек
                string numOfPayt = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                if ((!ret.result) || (vals[i].Length != 6))
                {
                    errPack.Append(rowNumber + ret.text + " Количество пачек");
                }
                sql += numOfPayt + ", ";
                i++;

                //5.имя файла
                string filePackName = Convert.ToString(finderpack.loaded_name);
                string filePackName1 = Path.GetFileName(filePackName);
                if (filePackName1 == "")
                {
                    errPack.Append(rowNumber + ret.text + " Имя файла");
                }
                sql += "'" + filePackName1 + "') ";
                i++;


                if (sql.Trim() != "")
                {
                    sqlStrPack.Add(sql);
                }
            }

            fstreamPack.Close();
            #endregion

            #region Запись в БД
            ////IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ////string connectionString = Points.GetConnByPref(conn_db);
            //IDbConnection conn_db = GetConnection(connectionString);
            //ret = OpenDb(conn_db, true);
            //if (!ret.result) return ret;

            try
            {
                //очищаем табличку
                string sql = "delete from " + Points.Pref + "_data" + tableDelimiter + "_reestrpack";
                ExecSQL(conn_db, sql, true);

                if (errPack.Length > 0)
                {
                    MonitorLog.WriteLog(errPack.ToString(), MonitorLog.typelog.Error, true);
                    ret.text = "Имеются ошибки в файле с пачками оплат";
                    ret.tag = -1;
                    ret.result = false;
                    return ret;
                }
                foreach (string s in sqlStrPack)
                {
                    ret = ExecSQL(conn_db, s, true);
                    if (!ret.result) return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры ReadReestrPack : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }


            #endregion

            #endregion

            conn_db.Close();

            //FileAttributes attributes = File.GetAttributes(fileNamePack);
            //if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            //{
            //    attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
            //    File.SetAttributes(fileNamePack, attributes);
            //}
            File.Delete(fileNamePack);

            return ret;
        }


        //Перезапись в нужную таблицу
        private Returns WriteToFin()
        {
            Returns ret = Utils.InitReturns();

            #region подключение к БД
            //Подключаемся к БД
            string connectionString2 = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString2);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            STCLINE.KP50.Utility.ClassLog.InitializeLog("c:\\", "chargeLoad.log");

            #region перезапись пачки реестров
            string dat_oper_day = "";
            string sql = "Select dat_oper From " + Points.Pref + "_data" + tableDelimiter + "fn_curoperday ";
            var dtr = ClassDBUtils.OpenSQL(sql, conn_db);
            foreach (DataRow rrR in dtr.resultData.Rows)
            {
                dat_oper_day = Convert.ToString(rrR["dat_oper"]).Substring(0, 10);
            }


            sql = "select id, datpaytorder, numpaytorder, sumofpayt, numofpayt, filename from " + Points.Pref + "_data" + tableDelimiter + "_reestrpack";
            var dt = ClassDBUtils.OpenSQL(sql, conn_db);

            foreach (DataRow rr1 in dt.resultData.Rows)
            {
                //уникальный код пачки
                string idPack1 = Convert.ToString(rr1["id"]);
                int idPack;
                //номер платежного поручения
                int numPaytOrder = Convert.ToInt32(rr1["numpaytorder"]);
                //количество платежей
                int numOfPayt = Convert.ToInt32(rr1["numofpayt"]);
                //общая сумма платежей
                string sumOfPayt = Convert.ToString(rr1["sumofpayt"]);
                //дата пачки
                string datPaytOrder1 = Convert.ToString(rr1["datpaytorder"]);
                string datPaytOrder = datPaytOrder1.Substring(0, 10);
                //месяц, за который осуществляется оплата
                string month = "01" + datPaytOrder.Substring(2);
                //имя файла
                string filePackName = Convert.ToString(rr1["filename"]);

                string sql1 = "insert into " + Points.Pref + "_fin_" + dat_oper_day.Substring(8, 2) + tableDelimiter + "pack (nzp_pack, par_pack, pack_type, nzp_bank, nzp_supp, nzp_oper, num_pack, dat_uchet, dat_pack, " +
                "num_charge, yearr, count_kv, sum_pack, geton_pack, real_sum, real_geton, real_count, flag, dat_vvod, islock, operday_payer, " +
                " peni_pack, sum_rasp, sum_nrasp, erc_code, dat_inp, time_inp, file_name)" +
#if PG
 "values ( default ," +
#else
 "values ( 0 ," +
#endif
 " null, 10, 1999, null, null, '" + numPaytOrder + "', '" + dat_oper_day + "', '" + datPaytOrder + "', " +
                " null, null, " + numOfPayt + ", " + sumOfPayt + ", null, null, null, 0, 11, '" + datPaytOrder + "', null, null, " +
                "  '0', '0', null, null, '" + datPaytOrder + "', null, '" + filePackName + "');";
                ret = ExecSQL(conn_db, sql1, true);
                idPack = GetSerialValue(conn_db);
                if (!ret.result) return ret;



            #endregion

                #region перезапись отдельных реестров

                sql = "select id, datpaytorder, numpaytorder, datpayt, numfilial, kodoperator, numoperation, sumofpayment, persaccklient, filename from  "
                      + Points.Pref + "_data" + tableDelimiter + "_reestr where numpaytorder=" + numPaytOrder.ToString();
                dt = ClassDBUtils.OpenSQL(sql, conn_db);

                foreach (DataRow rr2 in dt.resultData.Rows)
                {
                    //уникальный код квитанции
                    int kodKvitan = Convert.ToInt32(rr2["numoperation"]);
                    //код пачки  
                    //int numPaytOrder = Convert.ToInt32(rr1["numpaytorder"]);
                    //string sql3 = "select nzp_pack from " + Points.Pref + "_fin_13:pack where num_pack =" + Convert.ToInt32(rr2["numpaytorder"]) + ";";
                    //var dt3 = ClassDBUtils.OpenSQL(sql3, conn_db);
                    //int count1 = dt3.resultData.Rows.Count;
                    //numPaytOrder = Convert.ToInt32(dt3.resultData.Rows[count1 - 1]["nzp_pack"]);
                    //номер лицевого счета
                    int persAccKlient = Convert.ToInt32(rr2["persaccklient"]);
                    //платежный номер
                    sql1 = "select pkod from " + Points.Pref + "_data" + tableDelimiter + "kvar where nzp_kvar =" + persAccKlient + ";";
                    var dt1 = ClassDBUtils.OpenSQL(sql1, conn_db);
                    string pkod;
                    if (dt1.resultData.Rows.Count == 0)
                    {
                        //ret.text = "Нет личного счета клиента в таблице квартир ";
                        //ret.result = false;
                        //return ret;
                        pkod = "";
                    }
                    else
                    {
                        pkod = Convert.ToString(dt1.resultData.Rows[0]["pkod"]);
                    }
                    if (pkod == "") //pkod = "null";
                        pkod = "31";
                    //префикс БД - первые три цифры платежного номера
                    string prefix;
                    if (pkod == "null") prefix = "null";
                    else if (pkod.Length < 3) prefix = pkod;
                    else prefix = pkod.Substring(0, 3);
                    //сумма оплаты
                    sumOfPayt = Convert.ToString(rr2["sumofpayment"]);
                    //начислено к оплате                
                    //дата оплаты квитанции
                    datPaytOrder1 = Convert.ToString(rr2["datpayt"]);
                    datPaytOrder = datPaytOrder1.Substring(0, 10);
                    //месяц, за который осуществляется оплата
                    month = "01" + datPaytOrder.Substring(2);
                    //номер квитанции


                    string sql2 = "insert into " + Points.Pref + "_fin_" + dat_oper_day.Substring(8, 2) + tableDelimiter + "pack_ls (nzp_pack, prefix_ls, pkod, num_ls, g_sum_ls," +
                    "sum_ls, geton_ls, sum_peni, dat_month, kod_sum, nzp_supp, paysource, id_bill, dat_vvod," +
                    "dat_uchet, info_num, anketa,inbasket, alg, unl, date_distr, date_rdistr, nzp_user, incase, nzp_rs, erc_code, distr_month) values" +
                    "(" + idPack + ", " + prefix + ", " + pkod + ", " + persAccKlient + ", " + sumOfPayt + ", '0', '0', '0', '" + month + "'," +
                    "33, 0, null, 0, '" + datPaytOrder + "', null, '" + kodKvitan + "', null, 0, 0, 0, null, null, 0, 0, 0, null, null);";

                    ret = ExecSQL(conn_db, sql2, true);
                    if (!ret.result) return ret;

                }
            }

                #endregion

            conn_db.Close();

            return ret;
        }

        // Убираем лидирующие нули
        private static string DeleteFirstZeros(string str)
        {
            int i = 0;

            if (str[i] == '0')
                while ((i < str.Length) && (str[i] == '0'))
                    i++;

            String strResult;
            if (str[i] == '.')
                strResult = str.Remove(0, i - 1);
            else
                strResult = str.Remove(0, i);

            return strResult;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Castle.Core.Internal;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.IO;
using System.Threading;
using System.Security.Permissions;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Constants = STCLINE.KP50.Global.Constants;


namespace STCLINE.KP50.DataBase
{
    using global::Bars.KP50.Utils;
    using System.Text.RegularExpressions;

    public static class WorkWithFileStream
    {
        public static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
    }

    public partial class DbCounter : DbCounterKernel
    {
        //file_type==1 переодический реестр  
        //file_type==2 квитанция переодического реестра
        //file_type==3 итоговый реестр
        //file_type==4 квитанция итогового реестра 
        private struct FileName
        {
            public int fileType { get; set; }
            public string number { get; set; }
            public int month { get; set; }
            public int day { get; set; }
            public int unique_number { get; set; }
            public string otd_number { get; set; }
            public int rows_count { get; set; }
            public decimal sum_charge { get; set; }
        }

        private struct Reestr_head
        {
            public string date { get; set; }
            public string file_name { get; set; }
            public int kod_dopol { get; set; }
            public int file_line_count { get; set; }
            public decimal sum_plat { get; set; }
            public string is_kvit { get; set; }
            public int nzp_bank { get; set; }
            public string branch_id { get; set; }
        }

        private struct Reestr_body
        {
            public string ls_kod { get; set; }
            public decimal sum_plat { get; set; }
            public string transaction_id { get; set; }
            public string nomer_plat_poruch { get; set; }
            public string date_plat_poruch { get; set; }
            public string cnt1 { get; set; }
            public string val_cnt1 { get; set; }
            public string cnt2 { get; set; }
            public string val_cnt2 { get; set; }
            public string cnt3 { get; set; }
            public string val_cnt3 { get; set; }
            public string cnt4 { get; set; }
            public string val_cnt4 { get; set; }
            public string cnt5 { get; set; }
            public string val_cnt5 { get; set; }
            public string cnt6 { get; set; }
            public string val_cnt6 { get; set; }
        }
        public int kvit_id;
        public string back_transaction = "";
        public string not_exist_branch = "";
        public List<int> nzp_pack = new List<int>();
        public bool kvit = true;
        public int nzp_download;
        public string comment = "";
        public string uncorrect_rows = "";

        private FileName FileArgs;
        public DataTable DT = new DataTable();
        public DataTable DT1 = new DataTable();
        public int CountInsertedRows = 0;
        public decimal sum_pack;
        public decimal TotalSumPack;
        public int num_row = 0; //номер строки для логов

      

        public FilesImported FastCheck(FilesImported finder, out Returns ret)
        {
            //директория файла   
            string fDirectory = "";
            Utils.setCulture(); // установка региональных настроек
            if (InputOutput.useFtp)
            {
                fDirectory = InputOutput.GetInputDir();
                InputOutput.DownloadFile(finder.loaded_name, fDirectory + finder.saved_name, true);
            }
            else
            {
                fDirectory = Constants.Directories.ImportAbsoluteDir;
            }
            finder.ex_path = Path.Combine(fDirectory, finder.saved_name);
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return finder;
            }

            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return finder;
            }
            try
            {
                //проверка на существование такого же загруженного файла
                if (!ExistFile(conn_db, finder.saved_name))
                {
                    ret.text = "\nФайл с таким именем уже был загружен";
                    ret.result = false;
                    ret.tag = 996;
                    if (InputOutput.useFtp) System.IO.File.Delete(finder.ex_path);
                    return finder;
                }


                FileName FileArgs = getFileName(finder.saved_name);
                if (FileArgs.fileType == 1 || FileArgs.fileType == 3)
                {
                    kvit = false;
                }

                if (FileArgs.fileType == 0)
                {
                    ret.text = "Расширение файла недопустимо для этого банка.";
                    ret.result = false;
                    if (InputOutput.useFtp) System.IO.File.Delete(finder.ex_path);
                    return finder;
                }

                //проверка на существование отделения банка
                ret = CheckBank(conn_db, FileArgs);
                if (!ret.result)
                {
                    if (ret.tag == 999) // предложить создать нового платежного агента
                    {
                        ret.text = not_exist_branch;
                        ret.sql_error = FileArgs.otd_number.ToUpper();
                    }
                    return finder;
                }

                if (!kvit)
                    //проверка на существование квитанции для реестра
                    if (!CheckReestrName(conn_db, finder.saved_name, FileArgs))
                    {
                        ret.result = false;
                        ret.tag = 995;
                        ret.text = "\nДля файла итогового реестра не найдена квитанция";
                        return finder;
                    }

                if (!kvit)
                {
                    ret = CheckClearQueue(conn_db);
                    if (!ret.result)
                    {
                        ret.tag = 998;//статус "очередь занята"
                        return finder;
                    }
                }
                //проверка на длину номера транзакции, если >26, предупреждаем.
                //if (!kvit)
                //{
                //    string[] ReestrStrings = ReadReestrFile(finder.ex_path);
                //    ret = CheckNumTransaction(ReestrStrings);
                //    if (!ret.result)
                //    {
                //        ret.tag = 994;
                //        return finder;
                //    }

                //}
            }
            finally
            {
                conn_db.Close();
            }

            return finder;
        }

        public void UploadReestr(FilesImported finder, ref Returns ret)
        {

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            Utils.setCulture(); // установка региональных настроек
            FileName FileArgs = getFileName(finder.saved_name);
            if (FileArgs.fileType == 1 || FileArgs.fileType == 3)
            {
                kvit = false;
            }

            string dat = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало  тек.расчетного месяца
            string dat_next = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1).ToShortDateString() + "'";
            string dat_prev = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1).ToShortDateString() + "'";
            string month = Points.CalcMonth.month_.ToString("00");
            string year = (Points.CalcMonth.year_ - 2000).ToString("00");


            //получаем массив строк из файла
            string[] ReestrStrings = ReadReestrFile(finder.ex_path);

            //удаляем промежуточный файл на хосте
            if (InputOutput.useFtp) System.IO.File.Delete(finder.ex_path);

            //заголовочная структура
            Reestr_head Head_reestr = new Reestr_head();
            Reestr_body Body_reestr = new Reestr_body();

            FileArgs.rows_count = ReestrStrings.Length;
            finder.count_rows = FileArgs.rows_count;
            try
            {
                //запись в реестр загрузок
                InsertIntoTulaDownloaded(conn_db, finder.saved_name, FileArgs, finder);
                SetProcent(conn_db, nzp_download, 0, (int)StatusWWB.InProcess);
                double current = 0;
                double cost_one_row = Math.Round(30d / finder.count_rows, 4);
                int counter = 0;

                if (!kvit)
                {
                    //вызываем проверку на ошибки в заполнении строк
                    ret = FindErrors(ReestrStrings);
                    if (!ret.result)
                    {
                        uncorrect_rows = ret.text;
                        ret.text = "";
                        return;
                    }
                }

                //сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
                if (FileArgs.fileType == 1 || FileArgs.fileType == 3)
                {
                    ret = CheckReestrAtr(conn_db, FileArgs, finder.saved_name);
                    if (!ret.result)
                        return;
                }
                ////проверка на меньшее кол-во строк в итоговом реестре, чем в сумме за день по периодическим реестрам(откат транзакции в банке) 
                //if (FileArgs.fileType == 4)
                //{
                //    //всегда возвращает ret.result==true 
                //    ret = ReestrBackTransaction(conn_db, FileArgs, Head_reestr);
                //    if (!ret.result)
                //        return;
                //}

                foreach (var str in ReestrStrings)
                {
                    num_row++;
                    counter++;
                    #region Разделить строку по полям для дальнейшего разбора
                    string[] fields;
                    if (FileArgs.fileType == 2 || FileArgs.fileType == 4)
                        fields = str.Split('|');
                    else fields = str.Split(';');
                    #endregion Разделить строку по полям для дальнейшего разбора
                    switch (FileArgs.fileType)
                    {
                        case 3:
                            {
                                Body_reestr = CheakBody(fields);
                                //проверка на существование оплаты(если уже есть, то не пишем)
                                if (ReestrStringAnalis(conn_db, finder.saved_name, Body_reestr))
                                {

                                    bool result = InsertFileReestr(conn_db, Body_reestr, finder.saved_name, FileArgs.fileType);
                                    if (result == false)
                                    {
                                        ret.result = false;
                                        string num_str = "\n Номер строки с ошибкой: " + num_row;
                                        if (comment.Contains("\n"))
                                        {
                                            ret.text = "\nОшибка при записи данных итогового реестра:" + comment + num_str;
                                        }
                                        ret.text = "\nОшибка при записи данных итогового реестра" + num_str;
                                        return;
                                    }
                                    CountInsertedRows++;
                                }
                            } break;
                        case 4:
                            {
                                Head_reestr = CheakHead(fields);
                                Head_reestr.branch_id = FileArgs.otd_number;
                                if ((ret = KvitVerify(conn_db, FileArgs, Head_reestr.file_name)).result)
                                {
                                    Head_reestr.is_kvit = "1";
                                    bool result = InsertKvitReestr(conn_db, Head_reestr);

                                    if (result == false)
                                    {
                                        ret.result = false;
                                        ret.text = "\nОшибка при записи данных квитанции итогового реестра";
                                        MonitorLog.WriteLog("Ошибка добавления итогового реестра ", MonitorLog.typelog.Error, 20, 201, true);
                                        return;
                                    }

                                }

                            } break;
                        default:
                            {
                                ret.text = "\nНеверный формат файла. Количество полей в файле не совпадает с форматом.";
                                ret.result = false;
                                MonitorLog.WriteLog("Неверное количество полей в файле ", MonitorLog.typelog.Error, 20, 201, true);
                            } break;
                    }
                    FileArgs.sum_charge += Body_reestr.sum_plat;
                    if (counter > 10)
                    {
                        counter = 0;
                        current += cost_one_row * 10;
                        SetProcent(conn_db, nzp_download, current, (int)StatusWWB.InProcess);
                    }
                }

                if (ret.result)
                {

                    if (FileArgs.fileType == 1 || FileArgs.fileType == 3)
                    {

                        DT1.TableName = "cnt";
                        DT1.Columns.Add(new DataColumn("nzp_counter"));
                        DT1.Columns.Add(new DataColumn("value"));
                        finder.sum = FileArgs.sum_charge;

                        //записываем данные в систему: pack,pack_ls,pu_vals 
                        if (!SyncLsAndInsertPack(conn_db, FileArgs, kvit_id, finder).result)
                        {
                            ret.result = false;
                            MonitorLog.WriteLog("Ошибка записи пачки оплат в систему", MonitorLog.typelog.Error, 20, 201, true);
                            ret.text = "\nОшибка при записи данных в систему";
                        }

                    }
                    if (ret.result)
                    {
                        SetProcent(conn_db, nzp_download, 100, (int)StatusWWB.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                string num_str = "\n Номер строки с ошибкой: " + num_row;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\nОшибка при обработке файла. " + num_str;

            }
            if (ret.result)
            {
                if (!kvit) PackDist(nzp_pack, finder); //распределение пачек                
            }
            conn_db.Close();

            //предупреждения
            if (back_transaction != "")
            {
                ret.text += " " + back_transaction;
            }
            if (not_exist_branch != "")
            {
                ret.text += " " + not_exist_branch;
            }

            return;
        }

        public string[] ReadReestrFile(string path)
        {
            #region Открыть файл
            System.IO.FileStream fstream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            byte[] buffer = new byte[fstream.Length];
            fstream.Position = 0;
            fstream.Read(buffer, 0, buffer.Length);
            string ReestrFileString = System.Text.Encoding.GetEncoding(1251).GetString(buffer);
            string[] stSplit = { System.Environment.NewLine };
            string[] ReestrStrings = ReestrFileString.Split(stSplit, StringSplitOptions.RemoveEmptyEntries);
            fstream.Close();
            return ReestrStrings;
            #endregion
        }

        private bool SetProcent(int nzp_download, double proc, int status)
        {
            bool result = true;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            OpenDb(conn_db, true);
            result = SetProcent(conn_db, nzp_download, proc, status);
            return result;
        }

        private bool SetProcent(IDbConnection conn_db, int nzp_download, double proc)
        {
            bool result = true;
            int status = -999;
            result = SetProcent(conn_db, null, nzp_download, proc, status);
            return result;
        }

        private bool SetProcent(IDbConnection conn_db, int nzp_download, double proc, int status)
        {
            bool result = true;
            result = SetProcent(conn_db, null, nzp_download, proc, status);
            return result;
        }

        private bool SetProcent(IDbConnection conn_db, IDbTransaction transaction, int nzp_download, double proc, int status)
        {
            bool result = true;
            string sql = "";
            if (proc > 0)
            {
                sql = "UPDATE " + Points.Pref + sDataAliasRest + "tula_reestr_downloads SET proc=(" + proc + ") WHERE nzp_download=" + nzp_download;
                Returns ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка обновления статуса загрузки реестра", MonitorLog.typelog.Info, 20, 201, true);
                    result = false;
                }
            }
            if (status != -999)
            {
                sql = "UPDATE " + Points.Pref + sDataAliasRest + "tula_reestr_downloads SET status=(" + status + ") WHERE nzp_download=" + nzp_download;
                if (!ExecSQL(conn_db, transaction, sql, true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления статуса загрузки реестра", MonitorLog.typelog.Info, 20, 201, true);
                    result = false;
                }
            }

            return result;
        }



        private Returns GetProtocolWWB(FilesImported finder, bool result)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);

            if (!ret.result)
            {
                return ret;
            }
            FileArgs = getFileName(finder.saved_name);
            FileArgs.rows_count = finder.count_rows;
            #region Формирование протокола
            //  запись в БД о постановки в поток(статус 0)
            ret = AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Загрузка реестра в биллинговую систему за " + FileArgs.day.ToString("00") + "." + FileArgs.month.ToString("00"),
                is_shared = 1
            });
            if (!ret.result) return ret;

            try
            {
                int nzpExc = ret.tag;
                var DT = new DataTable();
                string path = "";
                //Имя файла отчета
                string file_name = "";
                int tag = 0;
                string status_name = "Успешно";
                DataTable Info = new DataTable();

                StringBuilder sql = new StringBuilder();

                sql.Remove(0, sql.Length);
                sql.Append("select nzp_kvit_reestr from " + Points.Pref + sDataAliasRest + "tula_kvit_reestr where file_name='" + finder.saved_name + "' ");
                object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (obj != null && obj != DBNull.Value)
                {
                    kvit_id = Convert.ToInt32(obj);
                }

                sql.Remove(0, sql.Length);
                sql.Append(" select pkod, sum_charge from " + Points.Pref + sDataAliasRest + "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + " and nzp_kvar is null");

                DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;

                sql.Remove(0, sql.Length);

                sql.Append(" SELECT d.*,b.bank FROM " + Points.Pref + sDataAliasRest + "tula_reestr_downloads d ");
                sql.Append(" LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_bank b  on b.nzp_bank=d.nzp_bank");
                sql.Append(" WHERE nzp_download=" + nzp_download + "");
                Info = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;

                sql.Remove(0, sql.Length);

                sql.Append(" SELECT * FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr ");
                sql.Append(" WHERE nzp_kvit_reestr=" + kvit_id);
                DataTable Kvit = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;



                FastReport.Report rep = new FastReport.Report();

                if (!result)
                {
                    DT.Rows.Clear();
                }

                DT.TableName = "ls";
                DT1.TableName = "cnt";
                DataSet fDataSet = new DataSet();


                if (DT1.Columns.Count == 0)
                {
                    DT1.TableName = "cnt";
                    DT1.Columns.Add(new DataColumn("nzp_counter"));
                    DT1.Columns.Add(new DataColumn("value"));
                }

                if (DT.Rows.Count > 0 || DT1.Rows.Count > 0 || comment.ToString() != "")
                {
                    SetProcent(conn_db, nzp_download, 100, (int)StatusWWB.WithErrors);
                    tag = 2;
                    status_name = "Загружено с ошибками";
                }
                if (result && uncorrect_rows != "" )
                {
                    tag = 2;
                    status_name = "Загружено с ошибками";
                }

                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    finder.sum -= (DT.Rows[i]["sum_charge"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[i]["sum_charge"]) : 0);
                    CountInsertedRows -= 1;
                }


                fDataSet.Tables.Add(DT);
                fDataSet.Tables.Add(DT1);

                string template = "protocol_workwithbank.frx";
                rep.Load(PathHelper.GetReportTemplatePath(template));

                rep.RegisterData(fDataSet);
                rep.GetDataSource("ls").Enabled = true;
                rep.GetDataSource("cnt").Enabled = true;

                //установка параметров отчета
                rep.SetParameterValue("file_name", (Info.Rows[0]["file_name"] != DBNull.Value ? Info.Rows[0]["file_name"].ToString() : ""));

                if (!result) status_name = "Ошибка";

                rep.SetParameterValue("status", status_name);

                rep.SetParameterValue("count_rows", FileArgs.rows_count);
                rep.SetParameterValue("count_inserted_rows", CountInsertedRows);
                rep.SetParameterValue("sum_plat", TotalSumPack);

                rep.SetParameterValue("count_rows_kvit", (Kvit.Rows[0]["count_rows"] != DBNull.Value ? Kvit.Rows[0]["count_rows"].ToString() : ""));
                rep.SetParameterValue("sum_plat_kvit", (Kvit.Rows[0]["sum_plat"] != DBNull.Value ? Kvit.Rows[0]["sum_plat"].ToString() : ""));

                rep.SetParameterValue("sum_in_plat", (finder.sum));
                rep.SetParameterValue("branch_name", (Info.Rows[0]["bank"] != DBNull.Value ? Info.Rows[0]["bank"].ToString() : ""));
                if (result)
                    rep.SetParameterValue("text", (FileArgs.rows_count != CountInsertedRows ? "Загружено " + CountInsertedRows + " записей из " + FileArgs.rows_count : ""));
                rep.SetParameterValue("comment", comment);
                rep.SetParameterValue("uncorrect_rows", uncorrect_rows);



                rep.Prepare();


                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();
                file_name = "protocol_WWB" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                    DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xlsx";
                export_xls.ShowProgress = false;
                MonitorLog.WriteLog(file_name, MonitorLog.typelog.Info, 20, 201, true);
                try
                {
                    export_xls.Export(rep, Path.Combine(InputOutput.GetOutputDir(), file_name));
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }

                ret.result = true;
                rep.Dispose();

                //перенос  на ftp сервер
                path = InputOutput.SaveOutputFile(Path.Combine(InputOutput.GetOutputDir(), file_name));


                if (ret.result)
                {
                    ret = ExecSQL(conn_db, "UPDATE " + Points.Pref + sDataAliasRest + "tula_reestr_downloads SET nzp_exc=" + nzpExc + " WHERE nzp_download=" + nzp_download, true);
                    ret.tag = tag;
                    SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = path });
                }
                else
                {
                    SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
            #endregion
            }
            finally
            {
                if (conn_db != null)
                    conn_db.Close();

            }
            return ret;
        }



        private Reestr_head CheakHead(string[] fields)
        {
            Reestr_head Head_reestr = new Reestr_head();
            int index = 0;

            if (fields[index].Trim() != "")
            {
                Head_reestr.date = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                Head_reestr.file_name = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                Head_reestr.kod_dopol = Convert.ToInt32(fields[index].Trim());
            }
            index++;

            if (fields[index].Trim() != "")
            {
                Head_reestr.file_line_count = Convert.ToInt32(fields[index].Trim());
            }
            index++;

            if (fields[index].Trim() != "")
            {
                Head_reestr.sum_plat = Convert.ToDecimal(fields[index].Trim());
            }

            return Head_reestr;
        }

        private bool InsertKvitReestr(IDbConnection conn_db, Reestr_head Head_reestr)
        {
            bool result = true;
            string sqlStr = "";
            try
            {

                sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_kvit_reestr (date_plat,file_name,kod_dop,count_rows,sum_plat,is_itog, branch_id ) VALUES " +
                                    " ( '" + Head_reestr.date + "', '" + Head_reestr.file_name + "', " + Head_reestr.kod_dopol + ", " +
                                    Head_reestr.file_line_count + ", " + Head_reestr.sum_plat + ", " + Head_reestr.is_kvit + ", '" + Head_reestr.branch_id + "')";

                Returns ret = ExecSQL(conn_db, sqlStr, true);
                if (!ret.result)
                {
                    result = false;
                    MonitorLog.WriteLog("Неожиданная ошибка сохранения  " + (Constants.Viewerror ? "\n" +
                              sqlStr.ToString() : ""),
                              MonitorLog.typelog.Error, 20, 201, true);
                }


            }
            catch (Exception ex)
            {
                result = false;
                MonitorLog.WriteException("Ошибка сохранения  " + (Constants.Viewerror ? "\n" +
                          sqlStr.ToString() : ""), ex);
            }
            return result;
        }

        private Reestr_body CheakBody(string[] fields)
        {
            Reestr_body body = new Reestr_body();
            int index = 0;
            bool add = false;
            if (fields[index].Trim() != "")
            {
                body.ls_kod = fields[index].Trim();
                if (body.ls_kod.Length != 13)
                {
                    MonitorLog.WriteLog("Ошибка, неверный лицевой счет ", MonitorLog.typelog.Error, 20, 201, true);
                }
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.sum_plat = Convert.ToDecimal(fields[index].Trim());
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.transaction_id = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.nomer_plat_poruch = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.date_plat_poruch = Convert.ToDateTime(fields[index].Trim()).ToString("dd.MM.yyyy");
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.cnt1 = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.val_cnt1 = fields[index].Trim();
                if (body.cnt1 == "НЕТ" && body.val_cnt1 != "НЕТ") add = true;
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.cnt2 = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.val_cnt2 = fields[index].Trim();
                if (body.cnt2 == "НЕТ" && body.val_cnt2 != "НЕТ") add = true;
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.cnt3 = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.val_cnt3 = fields[index].Trim();
                if (body.cnt3 == "НЕТ" && body.val_cnt3 != "НЕТ") add = true;
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.cnt4 = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.val_cnt4 = fields[index].Trim();
                if (body.cnt4 == "НЕТ" && body.val_cnt4 != "НЕТ") add = true;
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.cnt5 = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.val_cnt5 = fields[index].Trim();
                if (body.cnt5 == "НЕТ" && body.val_cnt5 != "НЕТ") add = true;
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.cnt6 = fields[index].Trim();
            }
            index++;

            if (fields[index].Trim() != "")
            {
                body.val_cnt6 = fields[index].Trim();
                if (body.cnt6 == "НЕТ" && body.val_cnt6 != "НЕТ") add = true;
            }
            index++;

            if (add)
            {
                uncorrect_rows += "Строка " + num_row + ": Нераспределенные показания приборов учета (нет номеров ПУ);\r\n";
            }

            return body;
        }

        private Returns CheckReestrAtr(IDbConnection conn_db, FileName file, string file_name)
        {
            Returns ret = Utils.InitReturns();
            string sqlString = "";
            int num;

            sqlString = "SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr where file_name='" + file_name + "' and sum_plat=" + TotalSumPack + " and count_rows=" + file.rows_count;

            object obj = ExecScalar(conn_db, sqlString, out ret, true);
            num = Convert.ToInt32(obj);
            if (num == 0)
            {
                ret.result = false;
                ret.text = "\nВ файле реестра сумма платежей или количество строк не совпадает с данными в квитанции";
            }
            else
            {
                ret.result = true;
            }
            return ret;
        }

        private Returns ReestrBackTransaction(IDbConnection conn_db, FileName file, Reestr_head Header_reestr)
        {
            Returns ret = Utils.InitReturns();
            string sqlString = "";
            int num;
            int count_rows = Header_reestr.file_line_count; //кол-во строк в итоговом реестре

            sqlString = "select " + sNvlWord + "(sum(count_rows),0) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr" +
                        " where file_name like '" + DBManager.sRegularExpressionAnySymbol + file.number + file.month + file.day + DBManager.sRegularExpressionAnySymbol + "' and is_itog<>1 ";

            object obj = ExecScalar(conn_db, sqlString, out ret, true);
            num = Convert.ToInt32(obj); //кол-во строк в таблице (сумма переодических реестров за за день по отделению)
            if (num > count_rows)
            {
                back_transaction = "\r\nВ квитанции итогового реестре указано меньшее количество строк, чем в сумме по периодическим реестрам за этот день.";
            }
            ret.result = true;
            return ret;
        }

        private bool InsertFileReestr(IDbConnection conn_db, Reestr_body Body_reestr, string file_name, int file_type)
        {
            bool result = true;
            string sqlStr = "";

            if (ReestrStringAnalis(conn_db, file_name, Body_reestr) == false)
            {
                comment = "\nЗагружаемые данные уже были загружены ранее, уникальное сочетание кода лицевого счета и номера транзакции уже есть в загруженных данных.";
                return false;
            }

            try
            {
                Returns ret;
                sqlStr.Remove(0, sqlStr.Length);

                sqlStr = "select nzp_kvit_reestr from " + Points.Pref + sDataAliasRest + "tula_kvit_reestr where file_name='" + file_name + "' ";

                object obj = ExecScalar(conn_db, sqlStr, out ret, true);
                if (obj != null && obj != DBNull.Value)
                {
                    kvit_id = Convert.ToInt32(obj);
                    if (kvit_id == 0)
                    {
                        MonitorLog.WriteLog("obj=null", MonitorLog.typelog.Info, 20, 201, true);
                        result = false;
                        comment = "\nНе найдена соответствующая квитанция";
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Не найдена соответствующая квитанция", MonitorLog.typelog.Error, 20, 201, true);
                }


                if (Body_reestr.nomer_plat_poruch == null || Body_reestr.date_plat_poruch == null)
                {
                    result = false;
                    comment = "\nВ файле реестра не заполнены поля: \"Номер платежного поручения\" или \"Дата платежного поручения\"";
                    return false;
                }

                sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                         " (pkod,nzp_kvit_reestr,sum_charge,transaction_id,nomer_plat_poruch," +
                         " date_plat_poruch,cnt1,val_cnt1,cnt2,val_cnt2,cnt3,val_cnt3,cnt4,val_cnt4,cnt5," +
                         " val_cnt5,cnt6,val_cnt6 ) " +
                         " VALUES " + " ( " + Body_reestr.ls_kod + ", " + kvit_id + ", " +
                         Body_reestr.sum_plat + ", " + "'" + Body_reestr.transaction_id + "'" + ", " +
                    ((Body_reestr.nomer_plat_poruch == "НЕТ") ? "null" : "'" + Body_reestr.nomer_plat_poruch) + "', " +
                    ((Body_reestr.date_plat_poruch == "НЕТ") ? "null" : "'" + Body_reestr.date_plat_poruch + "'") + ", " +
                    CheckNumCounter(Body_reestr.cnt1, Body_reestr.val_cnt1) + ", " +
                    CheckNumCounter(Body_reestr.cnt2, Body_reestr.val_cnt2) + ", " +
                    CheckNumCounter(Body_reestr.cnt3, Body_reestr.val_cnt3) + ", " +
                    CheckNumCounter(Body_reestr.cnt4, Body_reestr.val_cnt4) + ", " +
                    CheckNumCounter(Body_reestr.cnt5, Body_reestr.val_cnt5) + ", " +
                    CheckNumCounter(Body_reestr.cnt6, Body_reestr.val_cnt6) + ")";

                ret = ExecSQL(conn_db, sqlStr, true);
                if (!ret.result)
                {
                    result = false;
                    MonitorLog.WriteLog("Ошибка сохранения  " + (Constants.Viewerror ? "\n" +
                              sqlStr.ToString() : ""),
                              MonitorLog.typelog.Error, 20, 201, true);
                    comment += "\nОшибка сохранения данных реестра";
                    if (Body_reestr.transaction_id.Length > 26)
                        comment += ". Превышена длина поля \"Номер транзакции\", по формату длина поля составляет 26 символов";

                }
            }
            catch (Exception ex)
            {
                result = false;
                MonitorLog.WriteException("Ошибка сохранения  " + (Constants.Viewerror ? "\n" +
                          sqlStr.ToString() : ""),
                          ex);
            }

            return result;
        }

        private static string CheckNumCounter(string numCnt, string valCnt)
        {
            var numResult = numCnt == "НЕТ" || numCnt == "" || numCnt == " " || numCnt == null
                ? "null"
                : "'" + numCnt + "'";
            var valResult = (valCnt == "НЕТ" || valCnt == "" || valCnt == " " || valCnt == null)
                ? "null"
                : valCnt;

            return numResult + ',' + valResult;
        }

        private bool ReestrStringAnalis(IDbConnection conn_db, string file_name, Reestr_body Body_reestr)
        {
            bool result = true;

            string sqlStr = "select * from " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr where pkod=" + Body_reestr.ls_kod + " and transaction_id='" + Body_reestr.transaction_id + "'";

            var dt = ClassDBUtils.OpenSQL(sqlStr, conn_db);
            if (dt.resultData.Rows.Count > 0)
            {
                result = false;
            }
            else result = true;

            return result;
        }

        private bool CheckReestrName(IDbConnection conn_db, string file_name, FileName file)
        {
            bool result = true;

            string sqlStr = "select * from " + Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr where file_name='" + file_name + "' ";

            if (file.fileType == 3)
            {
                sqlStr += " and is_itog=1";
            }
            var dt = ClassDBUtils.OpenSQL(sqlStr, conn_db);
            if (dt.resultData.Rows.Count > 0)
            {
                result = true;
            }
            else result = false;
            return result;
        }

        private int getSelectedFileNames(IDbConnection conn_db, string file_name, int file_type)
        {
            int num;
            Returns ret;

            string sqlString = "SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr where file_name='" + file_name + "'";

            object obj = ExecScalar(conn_db, sqlString, out ret, true);
            return num = Convert.ToInt32(obj);
        }

        private int GetNzpBankByBranchId(IDbConnection conn_db, string branch_id)
        {
            Returns ret;
            int nzp_bank = 0;
            string sqlString = "SELECT nzp_bank FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank where is_actual<>100 and branch_id=" + Utils.EStrNull(branch_id.Trim().ToUpper());
            object obj = ExecScalar(conn_db, sqlString, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                int.TryParse(obj.ToString(), out nzp_bank);
            }
            return nzp_bank;
        }

        private void InsertIntoTulaDownloaded(IDbConnection conn_db, string file_name, FileName file, Finder finder)
        {
            int nzpUser = finder.nzp_user;
            Returns ret = new Returns(true);

            /*DbWorkUser db = new DbWorkUser();
            finder.pref = Points.Pref;
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();*/
            finder.nzp_user_main = nzpUser;

            int nzp_bank = GetNzpBankByBranchId(conn_db, file.otd_number);
            if (nzp_bank == 0)
            {
                ret.result = false;
                ret.text = "\r\n Не определен банк";
                return;
            }

            var date_d = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss").Replace('.', '-');

            string sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_reestr_downloads (file_name,nzp_type,date_download,user_downloaded,day,month, branch_id,nzp_bank) VALUES " +
                " ( '" + file_name + "'," + file.fileType + ",'" + date_d + "'," + nzpUser + "," + file.day + "," + file.month + ",'" + file.otd_number + "'," + nzp_bank + " )";

            ClassDBUtils.ExecSQL(sqlStr, conn_db);
            nzp_download = GetSerialValue(conn_db);
        }

        private Returns KvitVerify(IDbConnection conn_db, FileName file_args, string file_name)
        {
            Returns ret = Utils.InitReturns();
            string sqlString = "";
            int num;

            sqlString = "SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr where file_name='" + file_name + "'";

            object obj = ExecScalar(conn_db, sqlString, out ret, true);
            num = Convert.ToInt32(obj);
            if (num > 0 && file_args.fileType == 2)
            {
                ret.text = "";
                ret.result = false;
                return ret;
            }
            if (file_args.fileType == 2)
            {
                ret.text = "";
            }
            else ret.text = "";
            ret.result = true;
            return ret;
        }


        private bool ExistFile(IDbConnection conn_db, string file_name)
        {
            bool ret = true;
            string sqlString = "";
            int num;
            Returns retur = Utils.InitReturns();

            sqlString = "SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_reestr_downloads where file_name='" + file_name + "'";

            object obj = ExecScalar(conn_db, sqlString, out retur, true);
            num = Convert.ToInt32(obj);
            if (num > 0)
            {
                ret = false;
            }
            return ret;
        }

        private FileName getFileName(string fileName)
        {
            FileName name = new FileName();
            string[] fileN = fileName.Split('.');
            try
            {
                name.number = fileN[0].Substring(0, 5);
                name.month = int.Parse(fileN[0].Substring(5, 1), NumberStyles.AllowHexSpecifier);
                name.day = Convert.ToInt32(fileN[0].Substring(6, 2));

                switch (fileN[1].Substring(0, 2))
                {
                    case "KV":
                        {
                            name.fileType = 4;
                            name.otd_number = fileN[1].Substring(2);
                        } break;

                    case "00":
                        {
                            name.fileType = 3;
                            name.otd_number = fileN[1].Substring(2);
                        } break;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора имени файла: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return name;
        }

        private Returns CheckBank(IDbConnection conn_db, FileName fileArgs)
        {
            Returns ret = Utils.InitReturns();

            string sqlString = "select nzp_bank from " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank where is_actual<>100 and  branch_id='" + fileArgs.otd_number.ToUpper() + "'";

            object obj = ExecScalar(conn_db, sqlString, out ret, true);
            if (!ret.result)
            {
                ret.result = false;
                return ret;
            }
            string otd = Convert.ToString(obj);
            if (otd.Length == 0)
            {
                ret.tag = 999;
                ret.result = false;
                not_exist_branch = "\r\nПлатежный агент не определен. Выберите банк из списка вручную и снова загрузите файл";
            }
            return ret;
        }

        private Returns CheckClearQueue(IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            int count = 0;
            string sqlString = "select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads where status=" + (int)StatusWWB.InProcess;

            object obj = ExecScalar(conn_db, sqlString, out ret, true);
            if (!ret.result)
            {
                ret.result = false;
                return ret;
            }
            if (obj != null && obj != DBNull.Value)
            {
                count = Convert.ToInt32(obj);
            }
            if (count > 0)
            {
                ret.result = false;
                ret.text = "\nОчередь загрузки переполнена, попробуйте позже.";
            }
            return ret;
        }

        /// <summary>
        /// Управляющая функция
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="FileArgs"></param>
        /// <param name="kvit_id"></param>
        /// <param name="Finder"></param>
        /// <returns></returns>
        private Returns SyncLsAndInsertPack(IDbConnection conn_db, FileName FileArgs, int kvit_id, FilesImported Finder)
        {
            Returns ret;
            ret = Utils.InitReturns();
            string sql = "";

            #region связываем с ЛС из системы
            sql = " update  " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr set nzp_kvar= " +
                                 " (select k.nzp_kvar from " + Points.Pref + "_data" + tableDelimiter + "kvar k where k.pkod=" + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr.pkod) " +
                                 " where nzp_kvit_reestr=" + kvit_id + " ";

            if (!ExecSQL(conn_db, sql, true).result)
            {
                comment += "Ошибка сопоставления платежных кодов";
                ret.text = "Ошибка сопоставления платежных кодов";
                ret.result = false;
                MonitorLog.WriteLog("Ошибка сопоставления платежных кодов: " + sql.ToString(), MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

            SetProcent(conn_db, nzp_download, 40);
            //запись в систему показаний ПУ и оплат 
            ret = InsertPack(conn_db,   FileArgs, kvit_id, Finder);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи реестра " + FileArgs.number + FileArgs.month + FileArgs.day + FileArgs.otd_number + " в систему: " + ret.text, MonitorLog.typelog.Error, true);
            }

            return ret;
        }



        //запись в систему: пачки, оплаты, показания ПУ
        private Returns InsertPack(IDbConnection conn_db, /*IDbTransaction transaction,*/ FileName FileArgs, int kvit_id, FilesImported Finder)
        {
            Returns ret;
            ret = Utils.InitReturns();
            string sql = "";
            string dat = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало  тек.расчетного месяца
            string dat_next = ""; //след.рассчетный месяц
            string dat_prev = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1).ToShortDateString() + "'"; //предыдущий рассчетный месяц
            string month = Points.CalcMonth.month_.ToString("00");
            string year = (Points.CalcMonth.year_ - 2000).ToString("00");
            int SuperPack = 0;
            string pref_s = "";
            Dictionary<string, Dictionary<int, decimal>> maxDiffBetweenValues = GetMaxDiffBetweenValuesDict(conn_db); 


            #region Пачки
            
                //проверяем кол-во пачек. если >1 создаем суперпачку и связываем с ней остальные пачки
                sql = "SELECT nomer_plat_poruch, date_plat_poruch " +
                      "FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr WHERE nzp_kvit_reestr=" +
                      kvit_id + "" +
                      "GROUP BY nomer_plat_poruch, date_plat_poruch ";
                var packs = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            IDbTransaction transaction = conn_db.BeginTransaction();
            try
            {
                if (packs.Rows.Count > 1)
                {

                    #region Пишем суперпачку если нужно


                    //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре

                    sql = "select sum(sum_charge) from " + Points.Pref + sDataAliasRest +
                          "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + "";

                    object obj = ExecScalar(conn_db, transaction, sql, out ret, true);

                    if (obj != null && obj != DBNull.Value)
                    {
                        sum_pack = Convert.ToDecimal(obj);
                    }
                    else
                    {
                        MonitorLog.WriteLog("Ошибка получения суммы оплат для суперпачки: " + sql.ToString(),
                            MonitorLog.typelog.Error, true);
                    }

                    string operDay =
                        (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString(
                            "dd.MM.yyyy");
                    //записываем в pack

                    sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                          "(pack_type, nzp_bank,num_pack, dat_pack, count_kv, sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                          " select 10, 1000, substr(k.file_name,0,8), k.date_plat, " + CountInsertedRows + ", " +
                          sum_pack + "," + CountInsertedRows + ", 11, " + sCurDateTime + ",k.file_name " +
                          //nzp_bank=1000 -суперпачка
                          ", '" + operDay + "' " +
                          " from " + Points.Pref + "_data" + tableDelimiter + "tula_s_bank b, " + Points.Pref + "_data" +
                          tableDelimiter + "tula_kvit_reestr k " +
                          " where k.nzp_kvit_reestr=" + kvit_id + " and b.branch_id=k.branch_id and b.is_actual<>100 ";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи пачек оплаты";
                        MonitorLog.WriteLog("Ошибка записи в таблицу pack: " + sql.ToString(), MonitorLog.typelog.Error,
                            true);
                        transaction.Rollback();
                        return ret;
                    }
                    SuperPack = GetSerialValue(conn_db, transaction);
                    //nzp_pack.Add(SuperPack);

                    sql = "update " + Points.Pref + "_fin_" + year + tableDelimiter + "pack set (par_pack)=(" +
                          SuperPack + ") where nzp_pack=" + SuperPack;
                    if (!ExecSQL(conn_db, transaction, sql, true).result)
                    {
                        ret.text = "Ошибка записи пачек оплаты";
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка записи в таблицу pack: " + sql.ToString(), MonitorLog.typelog.Error,
                            true);
                        transaction.Rollback();
                        return ret;
                    }

                    //получаем локального пользователя
                    int nzpUser = Finder.nzp_user;
                    
                    /*DbWorkUser db = new DbWorkUser();
                    int nzpUser = db.GetLocalUser(conn_db, transaction, Finder, out ret);
                    db.Close();
                    if (!ret.result)
                    {
                        conn_db.Close();
                        transaction.Rollback();
                        return ret;
                    }*/

                    #endregion

                    #region Пишем пачки связанные с суперпачкой

                    for (int i = 0; i < packs.Rows.Count; i++)
                    {
                        //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре
                        sql = " SELECT sum(sum_charge) FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr" +
                              " WHERE nzp_kvit_reestr=" + kvit_id + " AND nomer_plat_poruch=" + "'" +
                              packs.Rows[i]["nomer_plat_poruch"] + "'" +
                              " AND date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"] + "'";
                        obj = ExecScalar(conn_db, transaction, sql, out ret, true);

                        if (obj != null && obj != DBNull.Value)
                        {
                            sum_pack = Convert.ToDecimal(obj);
                        }
                        else
                        {
                            MonitorLog.WriteLog("Ошибка получения суммы оплат для пачки: " + sql.ToString(),
                                MonitorLog.typelog.Error, true);
                        }


                        //кол-во строк в пачке
                        var insertedRowsForPack = 0;
                        sql = " SELECT count(*) FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr" +
                              " WHERE nomer_plat_poruch=" + "'" + packs.Rows[i]["nomer_plat_poruch"] + "'" +
                              " AND date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"] + "' ";
                        obj = ExecScalar(conn_db, transaction, sql, out ret, true);

                        if (obj != null && obj != DBNull.Value)
                        {
                            insertedRowsForPack = Convert.ToInt32(obj);
                        }
                        else
                        {
                            MonitorLog.WriteLog("Ошибка получения кол-ва строк для пачки: " + sql.ToString(),
                                MonitorLog.typelog.Error, true);
                        }
                        //записываем в pack

                        sql = " INSERT INTO " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack  " +
                              "(pack_type, nzp_bank,num_pack,par_pack, dat_pack, count_kv, sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                              " select 10, b.nzp_bank, " + "'" + packs.Rows[i]["nomer_plat_poruch"] + "'" + "," +
                              SuperPack + ",'" + packs.Rows[i]["date_plat_poruch"] + "', " + insertedRowsForPack + ", " +
                              +sum_pack + "," + insertedRowsForPack + ", 11, " + sCurDateTime + ",k.file_name " +
                              ", '" + operDay + "' " +
                              " from " + Points.Pref + "_data" + tableDelimiter + "tula_s_bank b, " + Points.Pref +
                              "_data" + tableDelimiter + "tula_kvit_reestr k " +
                              " where k.nzp_kvit_reestr=" + kvit_id +
                              " and b.branch_id=k.branch_id and b.is_actual<>100";
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи пачек оплаты";
                            MonitorLog.WriteLog("Ошибка записи в таблицу pack: " + sql.ToString(),
                                MonitorLog.typelog.Error, true);
                            transaction.Rollback();
                            return ret;
                        }
                        var this_pack = GetSerialValue(conn_db, transaction);
                        nzp_pack.Add(this_pack);


                        #region pack_ls - старый код

                        /*
                    //записываем в pack_ls
                    sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                               " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                               " inbasket, alg, unl, incase, pkod, nzp_user,dat_month) " +
#if PG
 " select " + this_pack + ", k1.num_ls, f.sum_charge,0,33,1,0,k.date_plat,cast(substr(f.transaction_id,21,6) as integer) as num_oper, " +
#else
 " select " + this_pack + ", k1.num_ls, f.sum_charge,0,33,1,0,k.date_plat,substr(f.transaction_id,21,6) as num_oper, " +
#endif
 " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0,f.pkod, " + nzpUser + " " + "," + dat_prev + " " +
                               " from " +
                               Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                               Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                               " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter + "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                               " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                               " and k.nzp_kvit_reestr=" + kvit_id + " and f.nomer_plat_poruch=" + "'" + packs.Rows[i]["nomer_plat_poruch"] + "'" +
                               " and f.date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"] + "' ";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи оплат по лицевым счетам";
                        MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    transaction.Rollback();
                        return ret;
                    }

                    //записываем не соотнесенные с системой ЛС в pack_ls_err  
                    sql = " insert into " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack_ls_err " +
                                " (nzp_pack_ls, nzp_err, note) " +
                                " select pl.nzp_pack_ls, 666, f.pkod " +
                                " from  " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack_ls pl, " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                                " where f.nzp_kvar is null and f.nzp_kvit_reestr=" + kvit_id + " and pl.pkod=f.pkod and f.nomer_plat_poruch=" + "'" + packs.Rows[i]["nomer_plat_poruch"] + "'" +
                               " and f.date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"] + "'";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                        MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls_err: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    transaction.Rollback();
                        return ret;
                    }
                     */

                        #endregion

                        #region записываем в pack_ls где pkod<14

#if PG
                        sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                              " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                              " inbasket, alg, unl, incase, pkod, nzp_user,dat_month) " +
                              " select " + this_pack + ", k1.num_ls, f.sum_charge,0,33,1,0,k.date_plat, " +
                              "  cast(substr(f.transaction_id,21,6) as integer) as num_oper, " +
                              " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0, f.pkod , " + nzpUser + " " +
                              "," + dat_prev + " " +
                              " from " +
                              Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                              Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                              " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter +
                              "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                              " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                              "       and k.nzp_kvit_reestr=" + kvit_id + " " +
                              "       and f.nomer_plat_poruch='" + packs.Rows[i]["nomer_plat_poruch"] + "'" +
                              "       and f.date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"].ToString().Trim() +
                              "' and length(cast(f.pkod as varchar))<14";
#else
                sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                           " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                           " inbasket, alg, unl, incase, pkod, nzp_user,dat_month) " +
                           " select " + this_pack + ", k1.num_ls, f.sum_charge,0,33,1,0,k.date_plat,substr(f.transaction_id,21,6) as num_oper, " +
                           " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0,f.pkod, " + nzpUser + " " + "," + dat_prev + " " +
                           " from " +
                            Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                            Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                           " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter + "kvar k1 on f.nzp_kvar=k1.nzp_kvar  " +
                           " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                           "       and k.nzp_kvit_reestr=" + kvit_id + " " +
                           "       and f.nomer_plat_poruch=" + packs.Rows[i]["nomer_plat_poruch"] +
                           "       and f.date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"] + "' and length(cast(f.pkod as char(20)))<14";
#endif
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка записи оплат по лицевым счетам";
                            MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls: " + sql.ToString(),
                                MonitorLog.typelog.Error, true);
                            transaction.Rollback();
                            return ret;
                        }

                        //записываем не соотнесенные с системой ЛС в pack_ls_err  

                        sql = " insert into " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack_ls_err " +
                              " (nzp_pack_ls, nzp_err, note) " +
                              " select pl.nzp_pack_ls, 666,f.pkod " +
                              " from  " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack_ls pl, " +
                              Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                              " where f.nzp_kvar is null and f.nzp_kvit_reestr=" + kvit_id + " and pl.pkod=f.pkod ";

                        if (!ExecSQL(conn_db, transaction, sql, true).result)
                        {
                            ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                            ret.result = false;
                            MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls_err: " + sql.ToString(),
                                MonitorLog.typelog.Error, true);
                            transaction.Rollback();
                            return ret;
                        }

                        #endregion

                        #region        записываем в pack_ls те оплаты в которых pkod>13

                        //получаем список оплат с pkod>13
#if PG
                        sql = "SELECT pkod FROM " + Points.Pref + sDataAliasRest +
                              "tula_file_reestr WHERE length(cast(pkod as varchar))>13 and nzp_kvit_reestr=" + kvit_id +
                              " and nomer_plat_poruch='" + packs.Rows[i]["nomer_plat_poruch"] + "'" +
                              " and date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"].ToString().Trim() + "'";
#else
                    sql = "SELECT pkod FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr WHERE length(cast(pkod as char(20)))>13 and nzp_kvit_reestr=" + kvit_id +
                    " and nomer_plat_poruch='" + packs.Rows[i]["nomer_plat_poruch"] + "'" +
                    " and date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"].ToString().Trim() + "'";
#endif

                        DataTable LsPkodOver13 = ClassDBUtils.OpenSQL(sql, conn_db, transaction).resultData;
                        if (LsPkodOver13.Rows.Count > 0)
                        {
                            for (int c = 0; c < LsPkodOver13.Rows.Count; c++)
                            {
#if PG
                                sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                                      " (nzp_pack,  g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                                      " inbasket, alg, unl, incase, nzp_user,dat_month) " +
                                      " select " + this_pack + ", f.sum_charge,0,33,1,0,k.date_plat, " +
                                      "  cast(substr(f.transaction_id,21,6) as integer) as num_oper, " +
                                      " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0, " + nzpUser + " " +
                                      "," + dat_prev + " " +
                                      " from " +
                                      Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                                      Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                                      " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter +
                                      "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                                      " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                                      "       and k.nzp_kvit_reestr=" + kvit_id + " " +
                                      "       and f.nomer_plat_poruch='" + packs.Rows[i]["nomer_plat_poruch"] + "'" +
                                      "       and f.date_plat_poruch='" +
                                      packs.Rows[i]["date_plat_poruch"].ToString().Trim() + "' and f.pkod=" +
                                      LsPkodOver13.Rows[c]["pkod"].ToString().Trim();
#else
                sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                           " (nzp_pack, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                           " inbasket, alg, unl, incase,  nzp_user,dat_month) " +
                           " select " + this_pack + ",  f.sum_charge,0,33,1,0,k.date_plat,substr(f.transaction_id,21,6) as num_oper, " +
                           " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0, " + nzpUser + " " + "," + dat_prev + " " +
                           " from " +
                            Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                            Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                           " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter + "kvar k1 on f.nzp_kvar=k1.nzp_kvar  " +
                           " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                           "       and k.nzp_kvit_reestr=" + kvit_id + " " +
                           "       and f.nomer_plat_poruch=" + packs.Rows[i]["nomer_plat_poruch"] +
                           "       and f.date_plat_poruch='" + packs.Rows[i]["date_plat_poruch"] + "' and f.pkod="+ LsPkodOver13.Rows[c]["pkod"].ToString().Trim();
#endif
                                ret = ExecSQL(conn_db, transaction, sql, true);
                                if (!ret.result)
                                {
                                    ret.text = "Ошибка записи оплат по лицевым счетам";
                                    MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls: " + sql.ToString(),
                                        MonitorLog.typelog.Error, true);
                                    transaction.Rollback();
                                    return ret;
                                }
                                int nzp_pack_ls = GetSerialValue(conn_db, transaction);

                                //записываем в pack_ls_err  те ЛС у которых lenght(pkod)>13

                                sql = " insert into " + Points.Pref + "_fin_" + year + "" + tableDelimiter +
                                      "pack_ls_err " +
                                      " (nzp_pack_ls, nzp_err, note) values (" + nzp_pack_ls + ",666," +
                                      LsPkodOver13.Rows[c]["pkod"].ToString().Trim() + ") ";

                                if (!ExecSQL(conn_db, transaction, sql, true).result)
                                {
                                    ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                                    ret.result = false;
                                    MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls_err: " + sql.ToString(),
                                        MonitorLog.typelog.Error, true);
                                    transaction.Rollback();
                                    return ret;
                                }
                            }
                        }

                        #endregion

                    }

                    #endregion
                }
                if (packs.Rows.Count == 1)
                {
                    #region Если только одна пачка


                    //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре

                    sql = "SELECT sum(sum_charge) FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr" +
                          " WHERE nzp_kvit_reestr=" + kvit_id + " and trim(" + sNvlWord + "(nomer_plat_poruch,'0'))='" +
                          packs.Rows[0]["nomer_plat_poruch"] + "'" +
                          " and trim(" + sNvlWord + "(date_plat_poruch,'0'))='" +
                          packs.Rows[0]["date_plat_poruch"].ToString().Trim() + "'";


                    object obj = ExecScalar(conn_db, transaction, sql, out ret, true);

                    if (obj != null && obj != DBNull.Value)
                    {
                        sum_pack = Convert.ToDecimal(obj);
                    }
                    else
                    {
                        MonitorLog.WriteLog("Ошибка получения суммы оплат для пачки: " + sql.ToString(),
                            MonitorLog.typelog.Error, true);
                    }

                    string operDay =
                        (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString(
                            "dd.MM.yyyy");


                    //кол-во строк в пачке
                    var insertedRowsForPack = 0;
                    sql = " SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr" +
                          " WHERE trim(" + sNvlWord + "(nomer_plat_poruch,'0'))='" + packs.Rows[0]["nomer_plat_poruch"] +
                          "'" +
                          " and trim(" + sNvlWord + "(date_plat_poruch,'0'))='" +
                          packs.Rows[0]["date_plat_poruch"].ToString().Trim() + "'";
                    obj = ExecScalar(conn_db, transaction, sql, out ret, true);

                    if (obj != null && obj != DBNull.Value)
                    {
                        insertedRowsForPack = Convert.ToInt32(obj);
                    }
                    else
                    {
                        MonitorLog.WriteLog("Ошибка получения кол-ва строк для пачки: " + sql.ToString(),
                            MonitorLog.typelog.Error, true);
                    }
                    //записываем в pack

                    sql = " INSERT INTO " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack  " +
                          "(pack_type, nzp_bank,num_pack, dat_pack, count_kv, sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                          " SELECT 10, b.nzp_bank, " + packs.Rows[0]["nomer_plat_poruch"] + ",'" +
                          packs.Rows[0]["date_plat_poruch"].ToString().Trim() + "', " +
                          insertedRowsForPack + ", " + sum_pack + "," + insertedRowsForPack + ", 11, " + sCurDateTime +
                          ",k.file_name " + ", '" + operDay + "' " +
                          " FROM " + Points.Pref + sDataAliasRest + "tula_s_bank b, " + Points.Pref + sDataAliasRest +
                          "tula_kvit_reestr k " +
                          " WHERE k.nzp_kvit_reestr=" + kvit_id + " and b.branch_id=k.branch_id and b.is_actual<>100";
                    if (!ExecSQL(conn_db, transaction, sql, true).result)
                    {
                        ret.text = "Ошибка записи пачек оплаты";
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка записи в таблицу pack: " + sql.ToString(), MonitorLog.typelog.Error,
                            true);
                        transaction.Rollback();
                        return ret;
                    }
                    var this_pack = GetSerialValue(conn_db, transaction);
                    nzp_pack.Add(this_pack);

                    //получаем локального пользователя
                    int nzpUser = Finder.nzp_user;

                    /*DbWorkUser db = new DbWorkUser();
                    int nzpUser = db.GetLocalUser(conn_db, transaction, Finder, out ret);
                    db.Close();
                    if (!ret.result)
                    {
                        conn_db.Close();
                        transaction.Rollback();
                        return ret;
                    }*/


                    #region записываем в pack_ls где pkod<14

                    sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                          " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                          " inbasket, alg, unl, incase, pkod, nzp_user,dat_month) " +
                          " select " + this_pack + ", k1.num_ls, f.sum_charge,0,33,1,0,k.date_plat, " +
                          " (substr(f.transaction_id,21,6) " + sConvToInt + ") as num_oper, " +
                          " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0, f.pkod , " + nzpUser + " " +
                          "," + dat_prev + " " +
                          " from " +
                          Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                          Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                          " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                          " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                          "       and k.nzp_kvit_reestr=" + kvit_id + " " +
                          "       and f.nomer_plat_poruch='" + packs.Rows[0]["nomer_plat_poruch"] + "'" +
                          "       and f.date_plat_poruch='" + packs.Rows[0]["date_plat_poruch"].ToString().Trim() +
                          "' and length(trim(f.pkod " + sConvToVarChar + "))<14";

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка записи оплат по лицевым счетам";
                        MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls: " + sql.ToString(),
                            MonitorLog.typelog.Error, true);
                        transaction.Rollback();
                        return ret;
                    }

                    //записываем не соотнесенные с системой ЛС в pack_ls_err  

                    sql = " insert into " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack_ls_err " +
                          " (nzp_pack_ls, nzp_err, note) " +
                          " select pl.nzp_pack_ls, 666,f.pkod " +
                          " from  " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack_ls pl, " + Points.Pref +
                          sDataAliasRest + "tula_file_reestr f " +
                          " where f.nzp_kvar is null and f.nzp_kvit_reestr=" + kvit_id + " and pl.pkod=f.pkod ";

                    if (!ExecSQL(conn_db, transaction, sql, true).result)
                    {
                        ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls_err: " + sql.ToString(),
                            MonitorLog.typelog.Error, true);
                        transaction.Rollback();
                        return ret;
                    }

                    #endregion


                    #region        записываем в pack_ls те оплаты в которых pkod>13

                    //получаем список оплат с pkod>13
#if PG
                    sql = "SELECT pkod FROM " + Points.Pref + sDataAliasRest +
                          "tula_file_reestr WHERE length(cast(pkod as varchar))>13 and nzp_kvit_reestr=" + kvit_id +
                          " and nomer_plat_poruch='" + packs.Rows[0]["nomer_plat_poruch"] + "'" +
                          " and date_plat_poruch='" + packs.Rows[0]["date_plat_poruch"].ToString().Trim() + "'";
#else
                sql = "SELECT pkod FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr WHERE length(cast(pkod as char(20)))>13 and nzp_kvit_reestr=" + kvit_id +
                    " and nomer_plat_poruch='" + packs.Rows[0]["nomer_plat_poruch"] + "'" +
                    " and date_plat_poruch='" + packs.Rows[0]["date_plat_poruch"].ToString().Trim() + "'";
#endif

                    DataTable LsPkodOver13 = ClassDBUtils.OpenSQL(sql, conn_db, transaction).resultData;
                    if (LsPkodOver13.Rows.Count > 0)
                    {
                        for (int c = 0; c < LsPkodOver13.Rows.Count; c++)
                        {
#if PG
                            sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                                  " (nzp_pack,  g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                                  " inbasket, alg, unl, incase, nzp_user,dat_month) " +
                                  " select " + this_pack + ", f.sum_charge,0,33,1,0,k.date_plat, " +
                                  "  cast(substr(f.transaction_id,21,6) as integer) as num_oper, " +
                                  " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0, " + nzpUser + " " + "," +
                                  dat_prev + " " +
                                  " from " +
                                  Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                                  Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                                  " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter +
                                  "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                                  " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                                  "       and k.nzp_kvit_reestr=" + kvit_id + " " +
                                  "       and f.nomer_plat_poruch='" + packs.Rows[0]["nomer_plat_poruch"] + "'" +
                                  "       and f.date_plat_poruch='" +
                                  packs.Rows[0]["date_plat_poruch"].ToString().Trim() + "' and f.pkod=" +
                                  LsPkodOver13.Rows[c]["pkod"].ToString().Trim();
#else
                sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls " +
                           " (nzp_pack, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                           " inbasket, alg, unl, incase,  nzp_user,dat_month) " +
                           " select " + this_pack + ",  f.sum_charge,0,33,1,0,k.date_plat,substr(f.transaction_id,21,6) as num_oper, " +
                           " (case when f.nzp_kvar is not null then 0 else 1 end), 0,0,0, " + nzpUser + " " + "," + dat_prev + " " +
                           " from " +
                            Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                            Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                           " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter + "kvar k1 on f.nzp_kvar=k1.nzp_kvar  " +
                           " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                           "       and k.nzp_kvit_reestr=" + kvit_id + " " +
                           "       and f.nomer_plat_poruch=" + packs.Rows[0]["nomer_plat_poruch"] +
                           "       and f.date_plat_poruch='" + packs.Rows[0]["date_plat_poruch"] + "' and f.pkod="+ LsPkodOver13.Rows[c]["pkod"].ToString().Trim();
#endif
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            if (!ret.result)
                            {
                                ret.text = "Ошибка записи оплат по лицевым счетам";
                                MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls: " + sql.ToString(),
                                    MonitorLog.typelog.Error, true);
                                transaction.Rollback();
                                return ret;
                            }
                            int nzp_pack_ls = GetSerialValue(conn_db, transaction);

                            //записываем в pack_ls_err  те ЛС у которых lenght(pkod)>13

                            sql = " insert into " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack_ls_err " +
                                  " (nzp_pack_ls, nzp_err, note) values (" + nzp_pack_ls + ",666," +
                                  LsPkodOver13.Rows[c]["pkod"].ToString().Trim() + ") ";

                            if (!ExecSQL(conn_db, transaction, sql, true).result)
                            {
                                ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                                ret.result = false;
                                MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls_err: " + sql.ToString(),
                                    MonitorLog.typelog.Error, true);
                                transaction.Rollback();
                                return ret;
                            }
                        }
                    }

                    #endregion

                    #endregion
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MonitorLog.WriteLog("Ошибка в функции InsertPack " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка при загрузке пачек оплат в режиме взаимодействие с Банком");
            }
            finally
            {
                if (transaction.Connection.State == ConnectionState.Open)
                    transaction.Commit();
            }
            #endregion


            //цикл по пачкам
            for (int p = 0; p < packs.Rows.Count; p++)
            {
                #region ПУ


                //достаем ПУ по ЛС

                sql = "select pkod from " + Points.Pref + sDataAliasRest + "tula_file_reestr" +
                      "  where nzp_kvit_reestr=" + kvit_id + " and nomer_plat_poruch='" + packs.Rows[p]["nomer_plat_poruch"] + "'" +
                      " and date_plat_poruch='" + packs.Rows[p]["date_plat_poruch"].ToString().Trim() + "'";
                var dt = ClassDBUtils.OpenSQL(sql, conn_db, transaction);
                if (dt.resultCode == -1)
                {
                    MonitorLog.WriteLog("Ошибка получения списка платежных кодов, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = "Ошибка записи показаний приборов учета";
                    return ret;
                }

                //цикл по платежным кодам
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    try
                    {
                        //достаем pkod и по нему вытаскиваем показания приборов учета
                        Decimal pkod = ((Decimal) (dt.resultData.Rows[i]["pkod"]));

                        sql = "select  cnt1 as cnt, val_cnt1  as val " +
                              " from " + Points.Pref + "_data" + tableDelimiter +
                              "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + "  and pkod=" + pkod +
                              "  and cnt1 is not null" +
                              " union all " +
                              " select cnt2 as cnt, val_cnt2 as val " +
                              " from " + Points.Pref + "_data" + tableDelimiter +
                              "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + " and pkod=" + pkod +
                              " and cnt2 is not null " +
                              " union all " +
                              " select  cnt3 as cnt, val_cnt3 as val " +
                              " from " + Points.Pref + "_data" + tableDelimiter +
                              "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + " and pkod=" + pkod +
                              "  and cnt3 is not null " +
                              " union all " +
                              " select  cnt4 as cnt, val_cnt4 as val " +
                              " from " + Points.Pref + "_data" + tableDelimiter +
                              "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + " and pkod=" + pkod +
                              " and cnt4 is not null " +
                              " union all " +
                              " select  cnt5 as cnt, val_cnt5 as val " +
                              " from " + Points.Pref + "_data" + tableDelimiter +
                              "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + " and pkod=" + pkod +
                              " and cnt5 is not null " +
                              " union all " +
                              " select  cnt6 as cnt, val_cnt6 as val " +
                              " from " + Points.Pref + "_data" + tableDelimiter +
                              "tula_file_reestr where nzp_kvit_reestr=" + kvit_id + " and pkod=" + pkod +
                              " and cnt6 is not null ";

                        var pu = ClassDBUtils.OpenSQL(sql, conn_db, transaction);
                        if (pu.resultCode == -1)
                        {
                            MonitorLog.WriteLog("Ошибка получения счет, sql: " + sql.ToString(),
                                MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = "Ошибка выборки списка параметров";
                            return ret;
                        }
                        //записываем в pu_vals показания ПУ
                        for (int k = 0; k < pu.resultData.Rows.Count; k++)
                        {
                            string cnt = ((string) (pu.resultData.Rows[k]["cnt"])).Trim();
                            decimal val = (pu.resultData.Rows[k]["val"] != DBNull.Value
                                ? ((decimal) (pu.resultData.Rows[k]["val"]))
                                : 0);
                            string[] cnts = cnt.Split('@');
                            int nzp_counter = 0;
                            int nzp_serv = 0;
                            if (cnts.Length > 1)
                            {
                                nzp_counter = (cnts[1] != "" ? int.Parse(cnts[1]) : 0);
                            }
                            if (cnts.Length == 1)
                            {
                                nzp_counter = int.Parse(cnt);
                            }
                            if (cnts.Length != 0)
                            {
                                if (nzp_counter != 0)
                                {
                                    #region проверка на существование счетчика в системе

                                    object exist;
                                    int exists = 0;
                                    //получаем префикс банка банных для данного л/с, после чего определяем по нему наличие счетчика в системе и рассчетный месяц в локальном банке этого л/с
                                    sql = "select k.pref from " + Points.Pref + sDataAliasRest + "kvar k where k.pkod=" +
                                          dt.resultData.Rows[i]["pkod"];
                                    var pref = ExecScalar(conn_db, transaction, sql, out ret, true);
                                    if (!ret.result)
                                    {
                                        MonitorLog.WriteLog(
                                            "Ошибка получения префикса банка данных, sql: " + sql.ToString(),
                                            MonitorLog.typelog.Error, true);
                                        ret.result = false;
                                        ret.text = "Ошибка получения префикса банка данных";
                                        return ret;
                                    }
                                    if (pref == null || pref == DBNull.Value)
                                    {
                                        dat_next = "'" +
                                                   new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1)
                                                       .AddMonths(1).ToShortDateString() + "'";
                                        //берем рассчетный месяц из центрального банка данных
                                    }
                                    else
                                    {
                                        //получаем префикс локального банка данных
                                        pref_s = Convert.ToString(pref).Trim();
                                        //получаем рассчетный месяц локального банка
                                        var local_date = Points.GetCalcMonth(new CalcMonthParams(pref_s));
                                        dat_next = "'" +
                                                   new DateTime(local_date.year_, local_date.month_, 1).AddMonths(1)
                                                       .ToShortDateString() + "'";
                                    }

                                    try
                                    {
                                        //если префикса нет, то пропускаем
                                        if (pref_s == "")
                                        {
                                            continue;
                                        }
                                        sql = "select nzp_serv from " + pref_s + sDataAliasRest +
                                              "counters_spis where nzp_counter=" + nzp_counter + "";

                                        exist = ExecScalar(conn_db, transaction, sql, out ret, true);

                                        if (!ret.result)
                                        {
                                            ret.result = false;
                                            ret.text = "Ошибка проверки существования счетчика в системе";
                                            return ret;
                                        }
                                        if (exist != null || exist != DBNull.Value)
                                        {
                                            nzp_serv = exist.ToInt();
                                            exists += Convert.ToInt32(exist);
                                        }


                                        if (exists == 0)
                                            //добавляем в список несопоставленных счетчиков, который выводится при формировании протокола
                                        {
                                            DataRow dr = DT1.NewRow();
                                            dr["nzp_counter"] = nzp_counter;
                                            dr["value"] = val;
                                            DT1.Rows.Add(dr);
                                        }
                                        else
                                        {
                                            #endregion

                                            if (
                                                !CheckZeroCrossAndBigVal(conn_db, maxDiffBetweenValues, pkod,
                                                    nzp_counter, val, cnt, nzp_serv, pref_s, dat_next))
                                                continue;


                                            //запись в pu_vals 

                                            sql =
                                                " insert into  " + Points.Pref + "_fin_" + year + tableDelimiter +
                                                "pu_vals  " +
                                                " (nzp_pack_ls, num_ls, nzp_counter, val_cnt, dat_month, cur_unl) " +
                                                " select pl.nzp_pack_ls, f.nzp_kvar, " + nzp_counter + "," + val + ", " +
                                                dat_next + ", f.nzp_kvit_reestr " +
                                                " from " + Points.Pref + "_fin_" + year + "" + tableDelimiter +
                                                "pack_ls pl," + Points.Pref + "_data" + tableDelimiter +
                                                "tula_file_reestr f " +
                                                " where f.pkod = pl.pkod and f.nzp_kvit_reestr=" + kvit_id +
                                                " and pl.pkod=" + pkod + " and f.nomer_plat_poruch='" +
                                                packs.Rows[p]["nomer_plat_poruch"] + "'" +
                                                " and f.date_plat_poruch='" + packs.Rows[p]["date_plat_poruch"] + "'";
                                            if (!ExecSQL(conn_db, transaction, sql, true).result)
                                            {
                                                ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                                                ret.result = false;
                                                MonitorLog.WriteLog(
                                                    "Ошибка записи в таблицу pu_vals: " + sql.ToString(),
                                                    MonitorLog.typelog.Error, true);
                                                return ret;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MonitorLog.WriteLog(
                                            "проверка на существование счетчика в системе " + ex.Message,
                                            MonitorLog.typelog.Error, true);
                                    }
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        comment += "\n Ошибка при добавлении показания ПУ для одного ЛС " + dt.resultData.Rows[i]["pkod"];
                        MonitorLog.WriteLog("Ошибка при добавлении показания ПУ для одного ЛС " + dt.resultData.Rows[i]["pkod"] + " : " + ex.Message + "\n" + ex.StackTrace,
                                            MonitorLog.typelog.Error, true);
                    }
                }
                #endregion
            }
            return ret;
        }
         /// <summary>
         /// заполняем максимальную разницу показаний по верхнему и всем локальным банкам
         /// </summary>
         /// <param name="conn_db"></param>
         /// <returns></returns>
        private Dictionary<string, Dictionary<int, decimal>> GetMaxDiffBetweenValuesDict(IDbConnection conn_db)
        {
            Dictionary<string, Dictionary<int, decimal>> resDictionary = new Dictionary<string, Dictionary<int, decimal>>();
            string sql = 
                " SELECT distinct trim(bd_kernel) as bd_kernel " +
                " FROM " + Points.Pref + sKernelAliasRest + "s_point";
            var pref = ClassDBUtils.OpenSQL(sql, conn_db, null).resultData;
            foreach (DataRow r in pref.Rows)
                resDictionary.Add(r["bd_kernel"].ToString(), GetMaxDiffBetweenValuesDictForOneBank(conn_db, r["bd_kernel"].ToString()));
            return resDictionary;
        }

        /// <summary>
        /// заполняем макимальную разницу показаний для отдельного банка
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="pref">префикс банка</param>
        /// <returns></returns>
        private Dictionary<int, decimal> GetMaxDiffBetweenValuesDictForOneBank(IDbConnection conn_db, string pref)
        {
            Dictionary<int, decimal> resDictionary = new Dictionary<int, decimal>();
            resDictionary.Add(25, GetMaxDiffBetweenValuesOneServ(conn_db, pref, 2081)); //электроснабжение
            resDictionary.Add(9, GetMaxDiffBetweenValuesOneServ(conn_db, pref, 2082)); //ГВС
            resDictionary.Add(6, GetMaxDiffBetweenValuesOneServ(conn_db, pref, 2083)); //ХВС
            resDictionary.Add(10, GetMaxDiffBetweenValuesOneServ(conn_db, pref, 2084)); //газ
            return resDictionary;
        }

        /// <summary>
        ///  заполняем макимальную разницу показаний по одной услуге для отдельного банка
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="pref">префикс банка</param>
        /// <param name="param">код параметра</param>
        /// <returns></returns>
        private decimal GetMaxDiffBetweenValuesOneServ(IDbConnection conn_db, string pref, int param)
        {
            Returns ret = new Returns();
            string sql =
                " SELECT " + sNvlWord + "(max(p.val_prm " + sConvToNum + "), 10000) " +
                " FROM " + pref + sKernelAliasRest + "prm_name pn " +
                " LEFT JOIN " + pref + sDataAliasRest + "prm_10 p ON pn.nzp_prm = p.nzp_prm and a.is_actual=1" +
                " WHERE pn.nzp_prm = " + param;
            decimal result = CollectionExtensions.IsNullOrEmpty(ExecScalar(conn_db, null, sql, out ret, true).ToString()) ? 
                10000m : Convert.ToDecimal(ExecScalar(conn_db, null, sql, out ret, true));
            return result;
        }

        private bool CheckZeroCrossAndBigVal(IDbConnection conn_db, Dictionary<string, Dictionary<int, decimal>> maxDiff,
            decimal pkod, int nzp_counter, decimal val, string cnt, int nzp_serv, string pref, string dat_next)
        {
            string sql;

            //проверка на большое значение
            if (val > 10000000m)
            {
                comment += "\n Слишком большое показание " + val + " для счетчика " + cnt + ", данные не загружены.";
                return false;
            }
            try
            {
                sql = " SELECT nzp_kvar FROM " + Points.Pref + sDataAliasRest + "kvar" +
                      " WHERE pkod = " + pkod;
                var dt = ClassDBUtils.OpenSQL(sql, conn_db, null);
                string nzp_kvar = dt.resultData.Rows[0]["nzp_kvar"].ToString().Trim();

                //проверка перехода через ноль

                sql = " SELECT val_cnt, dat_uchet FROM  " + pref + sDataAliasRest + "counters " +
                      " WHERE nzp_counter = " + nzp_counter +
                      " AND nzp_kvar =" + nzp_kvar +
                      " ORDER BY dat_uchet desc";
                dt = ClassDBUtils.OpenSQL(sql, conn_db, null);

                if (dt.resultData.Rows.Count > 0)
                {
                    decimal previosVal = dt.resultData.Rows[0]["val_cnt"].ToDecimal();
                    if (previosVal.ToDecimal() > val)
                    {
                        comment += "\n Показание " + val + " для счетчика " + cnt + " лицевого счета " + pkod +
                                   " переходит через ноль, данные не загружены.";
                        return false;
                    }
                    if ((val - previosVal.ToDecimal()) >
                        Math.Min(maxDiff[pref][nzp_serv], maxDiff[Points.Pref][nzp_serv]))
                    {
                        comment += "\n Слишком большое показание " + val + " для счетчика " + cnt + " лицевого счета " +
                                   pkod + ", данные не загружены.";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке на переход через ноль/слишком большое значение CheckZeroCrossAndBigVal " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return true;
        }



        public Returns PackDist(List<int> nzp_pack, FilesImported Finder)
        {
            Returns ret = Utils.InitReturns();
            ret.result = true;
            #region распределение пачек
            //в зависимости от настроек распределяем пачки сразу 
            if (Points.packDistributionParameters.DistributePackImmediately)
            {

                DbCalcPack db1 = new DbCalcPack();
                DbPack pack = new DbPack();
                for (int i = 0; i < nzp_pack.Count; i++)
                {
                    Pack finder = new Pack();
                    db1.PackFonTasks(nzp_pack[i], Finder.nzp_user, CalcFonTask.Types.DistributePack, out ret);  // Отдаем пачку на распределение                 
                    finder.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                    finder.nzp_user = Finder.nzp_user;
                    finder.nzp_pack = nzp_pack[i];
                    finder.year_ = Points.CalcMonth.year_;
                    if (ret.result)
                    {
                        db1.UpdatePackStatus(finder);
                    }
                }
                db1.Close();
                pack.Close();
            }


            return ret;
        }
            #endregion


        public Returns AddMyFile(ExcelUtility finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region Проверка на существование таблицы excel_utility, если нет, то создаем
            if (!TempTableInWebCashe(conn_db, sDefaultSchema+"excel_utility"))
            {
                ret = ExecSQL(conn_db,
                    " create table " + sDefaultSchema + "excel_utility " +
                    " ( nzp_exc      serial not null, " +
                    " nzp_user     integer not null, " +
                    " prms         char(200) not null, " +
                    " stats        integer default 0, " +
                    " dat_in       " + DBManager.sDateTimeType + ", " +
                    " dat_start    " + DBManager.sDateTimeType + ", " +
                    " dat_out      " + DBManager.sDateTimeType + ", " +
                    " tip          integer default 0 not null, " +
                    " rep_name     char(100),    " +
                    " exc_path     char(200), " +
                    " exc_comment  char(200), " +
                    " dat_today    date,   " +
                    " progress     integer default 0, " +
                    " is_shared INTEGER default 0, " +
                    " file_extension CHAR(10), " +
                    " file_name    char(200)" +
                    " ) ", true);

                if (!ret.result) return ret;

                //создаем индексы
#if PG
                ExecSQL(conn_db, " create distinct index public.ix_exc_1 on public.excel_utility (nzp_exc); ", true);
                ExecSQL(conn_db, " create        index public.ix_exc_2 on public.excel_utility (nzp_user, dat_in); ", true);
                ExecSQL(conn_db, " analyze excel_utility ", true);
#else
                ExecSQL(conn_db, " create unique index webdb.ix_exc_1 on webdb.excel_utility (nzp_exc); ", true);
                ExecSQL(conn_db, " create        index webdb.ix_exc_2 on webdb.excel_utility (nzp_user, dat_in); ", true);
                ExecSQL(conn_db, " Update statistics for table excel_utility ", true);
#endif
            }
            else
            {
                ret = AddFieldToTable(conn_db, "excel_utility", "progress", "integer default 0");
                if (!ret.result) return ret;
            }
            #endregion

            StringBuilder sql = new StringBuilder();
            sql.Append("insert into excel_utility (nzp_user, stats, prms, dat_in, rep_name, exc_comment, dat_today, exc_path,is_shared) ");
            sql.Append(" values (" + finder.nzp_user +
                ", " + (int)finder.status +
                ", " + Utils.EStrNull(finder.prms, "empty") +
                "," + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                ", " + Utils.EStrNull(finder.rep_name) +
                ", " + Utils.EStrNull(finder.exec_comment) +
                ", " + Utils.EStrNull(DateTime.Now.ToShortDateString()) +
                ", " + Utils.EStrNull(finder.exc_path) +
                ", " + finder.is_shared +
                ")");

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            int id = GetSerialValue(conn_db);

            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                ExecSQL(conn_db, "update excel_utility set dat_start = dat_in where nzp_exc = " + id, true);
            }

            conn_db.Close();
            sql.Remove(0, sql.Length);

            ret.tag = id;

            return ret;
        }

        /// <summary>
        /// Смена статуса задания
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SetMyFileState(ExcelUtility finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            StringBuilder sql = new StringBuilder();
            sql.Append(" update excel_utility set stats = " + (int)finder.status);
            if (finder.status == ExcelUtility.Statuses.InProcess)
            {
                sql.Append(", dat_start = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else if (finder.status == ExcelUtility.Statuses.Success || finder.status == ExcelUtility.Statuses.Failed)
            {
                sql.Append(", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            if (finder.exc_path != "") sql.Append(", exc_path = " + Utils.EStrNull(finder.exc_path));
            sql.Append(" where nzp_exc =" + finder.nzp_exc);

            ret = ExecSQL(conn_db, sql.ToString(), true);

            conn_db.Close();
            sql.Remove(0, sql.Length);

            return ret;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public Returns FindErrors(string[] rows)
        {
            Returns ret = Utils.InitReturns();
            Utils.setCulture(); // установка региональных настроек
            Dictionary<int, string> num_rows = new Dictionary<int, string>();
            for (int i = 0; i < rows.Length; i++)
            {
                bool add = false;
                string err = "";

                //проверяем структуру строки на целостность
                string[] row_el = rows[i].Split(new string[] { ";", "|" });
                if (row_el.Length != 17)
                {
                    add = true;
                    err += (err != "" ? ", Нарушен формат реестра: Неверное число полей в строке" : " Нарушен формат реестра: Неверное число полей в строке");
                }

                //число полей по формату
                if (row_el.Length == 17)
                {
                    //Код лицевого счета
                    if (!Regex.IsMatch(row_el[0], @"^\d+$") || row_el[0].Trim().Length>20)
                    {
                        add = true;
                        err += (err != "" ? ", Нарушен формат поля \"Код лицевого счета\"" : "Нарушен формат поля \"Код лицевого счета\"");
                    }

                    //Сумма платежа           
                    if (!Regex.IsMatch(row_el[1], @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        add = true;
                        err += (err != "" ? ", Нарушен формат поля \"Сумма платежа\"" : "Нарушен формат поля \"Сумма платежа\"");
                    }
                    else
                    {
                        decimal sum_opl = 0;
                        Decimal.TryParse(row_el[1], out sum_opl);
                        TotalSumPack += sum_opl;
                    }

                    //Номер транзакции 
                    if (!Regex.IsMatch(row_el[2], @"\d{26}"))
                    {
                        add = true;
                        err += (err != "" ? ", Нарушен формат поля \"Номер транзакции\"" : "Нарушен формат поля \"Номер транзакции\"");
                    }

                    //Номер платежного поручения 
                    if (!Regex.IsMatch(row_el[3], @"^[0-9]\d*$"))
                    {
                        add = true;
                        err += (err != "" ? ", Нарушен формат поля \"Номер платежного поручения\"" : "Нарушен формат поля \"Номер платежного поручения\"");
                    }

                    //Дата платёжного поручения
                    if (!Regex.IsMatch(row_el[4], @"^(\d{2}).\d{2}.(\d{4})$"))
                    {
                        add = true;
                        err += (err != "" ? ", Нарушен формат поля \"Дата платёжного поручения\"" : "Нарушен формат поля \"Дата платёжного поручения\"");
                    }

                    int start_pos = 4;
                    //пробегаем по номерам ПУ и их показаниям
                    for (int j = 1; j <= 12; j += 2)
                    {
                        //Код счетчика 
                        if (!Regex.IsMatch(row_el[start_pos + j], @"^[0-9]+(\.[0-9]+)?$|^НЕТ$"))
                        {
                            add = true;
                            err += (err != "" ? ", Нарушен формат поля \"Код счетчика №" + (j / 2 + 1) + "\"" : "Нарушен формат поля \"Код счетчика №" + (j / 2 + 1) + "\"");
                        }

                        //Показание счетчика текущее
                        if (!Regex.IsMatch(row_el[start_pos + j + 1], @"^[0-9]+(\.[0-9]+)?$|^НЕТ$"))
                        {
                            add = true;
                            err += (err != "" ? ", Нарушен формат поля \"Показание счетчика текущее №" + (j / 2 + 1) + "\"" : "Нарушен формат поля \"Показание счетчика текущее №" + (j / 2 + 1) + "\"");
                        }
                    }

                }

                //добавляем номер строки в список строк с ошибками
                if (add) num_rows.Add(i + 1, err);
            }
            if (num_rows.Count > 0)
            {
                ret.result = false;
                ret.text = "Номера строк с ошибками:\r\n";
                foreach (var num in num_rows)
                {
                    ret.text += "Строка: " + num.Key + ", Ошибки: " + num.Value + ";\r\n";
                }
                ret.text = ret.text.Remove(ret.text.Length - 1, 1);
            }

            return ret;
        }


        /// <summary>
        /// Проверка длины нмоера транзакции
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public Returns CheckNumTransaction(string[] rows)
        {
            Returns ret = Utils.InitReturns();

            for (int i = 0; i < rows.Length; i++)
            {

                //проверяем структуру строки на целостность
                string[] row_el = rows[i].Split(new string[] { ";", "|" });
                if (row_el.Length != 17)
                {
                    //простро переходим к проверке формата
                    return ret;
                }
                else
                {
                    if (row_el[2].Trim().Length > 26)
                    {
                        ret.result = false;
                        ret.tag = 994;
                        ret.text = "\nВ реестре присутствуют строки с длинной поля \"Номер транзакции\" больше 26 символов, все равно загрузить файл?";
                        return ret;
                    }
                }

            }

            return ret;
        }


    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bars.KP50.DataImport.SOURCE.UTILS;
using Bars.KP50.Utils;
using FastReport;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Excel = Microsoft.Office.Interop.Excel; 

namespace Bars.KP50.Load.Obninsk
{
    public class ImportParams : BaseSqlLoad
    {
        protected override void PrepareParams()
        { 
        }

        public override string Name
        {
            get {return "Загрузка параметров"; }
        }

        public override string Description
        {
            get { return "Загрузка параметров из режима \"Экспорт и импорт параметров\""; }
        }

        protected override byte[] Template
        {
            get { return null; }
        }

        public override List<Report.UserParam> GetUserParams()
        {
            return null;
        }


        private string copyiedTemplate = String.Empty;
        //Приложение Excel
        protected Excel.Application ExlApp;
        //
        public Excel.Workbook ExlWb;
        //
        protected Excel.Worksheet ExlWs;
        //
        object misValue = System.Reflection.Missing.Value;

        protected System.Data.DataTable errDT = new DataTable();

        public override void LoadData()
        {
            errDT.Columns.Add("mes", typeof(string));

            //Создание сервера Excel
            ExlApp = new Excel.Application();
            //отключить предупреждения
            ExlApp.DisplayAlerts = false;

            //Создание рабочей книги
            ExlWb = ExlApp.Workbooks.Add(misValue);
            //Получение ссылки на лист1
            ExlWs = (Excel.Worksheet)ExlWb.Worksheets.get_Item(1);
            ExlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;

            if (!CheckFileAccess(TemporaryFileName))
            {
                Protokol.AddUnrecognisedRow("Файл " + FileName +
                                    " не найден");
                return;
            }
            try
            {
                copyiedTemplate = TemporaryFileName.Replace(".xls", DateTime.Now.GetHashCode() + ".xls");
                File.Copy(TemporaryFileName, copyiedTemplate);
                //открыть шаблон
                this.ExlWb = this.ExlApp.Workbooks.Open(copyiedTemplate, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                //обновить ссылка на страницу(обязательно при смене ссылки на книгу)
                this.RefreshWs();
                //Количество строк с данными
                int iRowCount = this.ExlWs.UsedRange.Rows.Count;

                //количество столбцов с данными
                if (this.ExlWs.UsedRange.Columns.Count < 6)
                {
                    Protokol.AddUnrecognisedRow("Форматом предусмотрено 6 полей. Файл " + FileName +
                                        " не соответствует установленному формату");
                    return;
                }
                int iColumnCount = this.ExlWs.UsedRange.Columns.Count - 1;
                var lastCol = (char)('A' + iColumnCount);
                

                Microsoft.Office.Interop.Excel.Range excells1;
                string sql;
                Returns ret = new Returns(true);

                try
                {
                    sql = " DROP TABLE " + Points.Pref + DBManager.sUploadAliasRest + "importparam ";
                    ExecSQL(sql, false);
                    sql =
                        "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "importparam(" +
                        " nzp INTEGER," +
                        " nzp_prm INTEGER, " +
                        " date_s DATE," +
                        " date_po DATE," +
                        " val_prm CHAR(100)," +
                        " is_actual bool," +
                        " nzp_file INTEGER," +
                        " prm_num INTEGER," +
                        " pref CHAR(10))";
                    ExecSQL(sql, false);
                }
                catch { }

                #region считываем в табличку
                excells1 = ExlWs.get_Range("A" + 1, lastCol.ToString() + iRowCount);
                object[,] cellArray = excells1.Value2;
                
                for (int curRow = 2; curRow <= iRowCount; curRow++)
                {
                    try
                    {
                        if (cellArray[curRow, 1] == null || string.IsNullOrEmpty(cellArray[curRow, 1].ToString()))
                        {
                            DataRow dr = errDT.NewRow();
                            dr.SetField("mes", "Строка номер " + curRow + " не прошла загрузку. Поле код объекта не заполнено.");
                            errDT.Rows.Add(dr);
                            continue;
                        }
                        if (cellArray[curRow, 2] == null || string.IsNullOrEmpty(cellArray[curRow, 2].ToString()))
                        {
                            DataRow dr = errDT.NewRow();
                            dr.SetField("mes", "Строка номер " + curRow + " не прошла загрузку. Поле код параметра не заполнено.");
                            errDT.Rows.Add(dr);
                            continue;
                        }
                        if (cellArray[curRow, 3] == null || string.IsNullOrEmpty(cellArray[curRow, 3].ToString()))
                        {
                            DataRow dr = errDT.NewRow();
                            dr.SetField("mes", "Строка номер " + curRow + " не прошла загрузку. Поле дата начала не заполнено.");
                            errDT.Rows.Add(dr);
                            continue;
                        }
                        if (cellArray[curRow, 4] == null || string.IsNullOrEmpty(cellArray[curRow, 4].ToString()))
                        {
                            DataRow dr = errDT.NewRow();
                            dr.SetField("mes", "Строка номер " + curRow + " не прошла загрузку. Поле дата окончания не заполнено.");
                            errDT.Rows.Add(dr);
                            continue;
                        }
                        if (cellArray[curRow, 5] == null || string.IsNullOrEmpty(cellArray[curRow, 5].ToString()))
                        {
                            DataRow dr = errDT.NewRow();
                            dr.SetField("mes", "Строка номер " + curRow + " не прошла загрузку. Поле значение параметра не заполнено.");
                            errDT.Rows.Add(dr);
                            continue;
                        }
                        if (cellArray[curRow, 6] == null || string.IsNullOrEmpty(cellArray[curRow, 6].ToString()))
                        {
                            DataRow dr = errDT.NewRow();
                            dr.SetField("mes", "Строка номер " + curRow + " не прошла загрузку. Поле актуальность не заполнено.");
                            errDT.Rows.Add(dr);
                            continue;
                        }
                        string dat_s;
                        string dat_po;
                        double dat_num;
                        if (Double.TryParse(cellArray[curRow, 3].ToString(), out dat_num))
                        {
                            dat_s = DateTime.FromOADate(dat_num).ToShortDateString();
                        }
                        else dat_s = cellArray[curRow, 3].ToString();
                        if (Double.TryParse(cellArray[curRow, 4].ToString(), out dat_num))
                        {
                            dat_po = DateTime.FromOADate(dat_num).ToShortDateString();
                        }
                        else dat_po = cellArray[curRow, 4].ToString();
                        sql =
                            " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                            " (nzp, nzp_prm, date_s, date_po, val_prm, is_actual, nzp_file)" +
                            " VALUES" +
                            " (" + cellArray[curRow, 1] + ", " + cellArray[curRow, 2] + ", " +
                            "'" + dat_s + "'," + "  '" + dat_po + "'," +
                            " '" + cellArray[curRow, 5] + "', " + cellArray[curRow, 6].ToBool() + ", " +
                            NzpLoad + ")";
                        ExecSQL(sql);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Импорт параметров ImportParams.cs " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                        DataRow dr = errDT.NewRow();
                        dr.SetField("mes", "Строка номер " + curRow + " не прошла загрузку. Проверьте соответствие формату.");
                        errDT.Rows.Add(dr);
                    }
                }
                #endregion

                if (errDT.Rows.Count > 0)
                {
                    sql = " DELETE FROM " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                          " WHERE nzp_file = " + NzpLoad;
                    ExecSQL(sql, false);
                }

                if (errDT != null && errDT.Rows.Count != 0)
                {
                    foreach (DataRow row in errDT.Rows)
                    {
                        Protokol.AddUnrecognisedRow(row.Field<string>("mes"));
                    }
                    Protokol.CountInsertedRows = 0;
                }

                else
                {
                    Protokol.CountInsertedRows = iRowCount;
                    Disassemble(ret);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Импорт параметров ImportParams.cs " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
        }

        private void Disassemble(Returns ret)
        {
            string sql;
            
            try
            {
                #region определяем тип параметра
                //квартирный
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                    " SET prm_num = 1 " +
                    " WHERE nzp_file = " + NzpLoad + 
                    " AND EXISTS" +
                    " (SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "prm_name p" +
                    "  WHERE p.prm_num = 1 " +
                    "  AND p.nzp_prm = " + Points.Pref + DBManager.sUploadAliasRest + "importparam.nzp_prm)";
                ExecSQL(sql);
                //домовой
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                    " SET prm_num = 2 " +
                    " WHERE nzp_file = " + NzpLoad +
                    " AND EXISTS" +
                    " (SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "prm_name p" +
                    "  WHERE p.prm_num = 2 " +
                    "  AND p.nzp_prm = " + Points.Pref + DBManager.sUploadAliasRest + "importparam.nzp_prm)";
                ExecSQL(sql);

                sql = " SELECT DISTINCT nzp_prm FROM " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                      " WHERE prm_num is null AND nzp_file = " + NzpLoad;
                DataTable dtPrm = ExecSQLToTable(sql);
                if (dtPrm.Rows.Count > 0)
                {
                    List<string> list = new List<string>();
                    foreach (DataRow r in dtPrm.Rows)
                    {
                        list.Add(r["nzp_prm"].ToString());
                    }
                    Protokol.AddUnrecognisedRow("Не были опознаны параметры " + String.Join(", ", list));
                    return;
                }
                #endregion

                #region определяем префикс
                //квартирный
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                    " SET pref = " +
                    " (SELECT k.pref FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar k" +
                    " WHERE k.nzp_kvar = " + Points.Pref + DBManager.sUploadAliasRest + "importparam.nzp)" +
                    " WHERE nzp_file = " + NzpLoad +
                    " AND prm_num = 1 AND pref is NULL";
                ExecSQL(sql);
                //домовой
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                    " SET pref = " +
                    " (SELECT d.pref FROM " + Points.Pref + DBManager.sDataAliasRest + "dom d" +
                    " WHERE d.nzp_dom = " + Points.Pref + DBManager.sUploadAliasRest + "importparam.nzp)" +
                    " WHERE nzp_file = " + NzpLoad +
                    " AND prm_num = 2 AND pref is NULL";
                ExecSQL(sql);


                sql = " SELECT DISTINCT nzp FROM " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                      " WHERE pref is null AND nzp_file = " + NzpLoad;
                DataTable dtPref = ExecSQLToTable(sql);
                if (dtPref.Rows.Count > 0)
                {
                    List<string> list = new List<string>();
                    foreach (DataRow r in dtPref.Rows)
                    {
                        list.Add(r["nzp"].ToString());
                    }
                    Protokol.AddUnrecognisedRow("Не определен банк для объектов с кодами " + String.Join(", ", list));
                    return;
                }
                #endregion

                #region Цикл по каждому префиксу, потом по каждому типу параметров

                sql = " SELECT DISTINCT pref FROM " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                      " WHERE nzp_file = " + NzpLoad;
                DataTable dtPrefForCycle = ExecSQLToTable(sql);

                foreach (DataRow row in dtPrefForCycle.Rows)
                {
                    ret = WriteParam(ret, row["pref"].ToString().Trim(), 1);
                    ret = WriteParam(ret, row["pref"].ToString().Trim(), 2);
                }
                #endregion

            }
            catch (Exception ex)
            {
                Protokol.AddUnrecognisedRow("Ошибка разбора файла");
                MonitorLog.WriteLog("Импорт параметров ImportParams.cs " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error,
                    true);
            }
        }

        private Returns WriteParam(Returns ret, string pref, int prm_num)
        {
            string sql;
            string ttable = "t_for_ls_prm_dis";
            try
            {
                sql = "DROP TABLE " + ttable;
                ExecSQL(sql, false);
            }
            catch
            {
            }
            sql = " CREATE TEMP TABLE " + ttable + " (" +
                  " nzp INTEGER," +
                  " nzp_prm INTEGER," +
                  " dat_po DATE," +
                  " dat_s DATE," +
                  " val_prm CHAR(200)," +
                  " is_actual INTEGER, " +
                  " user_del INTEGER," +
                  " cur_unl INTEGER," +
                  " nzp_user INTEGER)";
            ExecSQL(sql);

            sql =
                " INSERT INTO " + ttable +
                " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, cur_unl, nzp_user)" +
                " SELECT DISTINCT val_prm, nzp_prm, nzp, date_s, date_po, " +
                " CASE WHEN is_actual THEN 1 ELSE 0 END, " + NzpLoad + ", 1, " + Nzp_user +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "importparam" +
                " WHERE nzp_file = " + NzpLoad + " AND prm_num = " + prm_num + " AND trim(pref) = '" + pref.Trim() + "'";
            ExecSQL(sql);


            var lp = new LoadPrm(OpenConnection());
            ret = lp.SetPrm(prm_num, ttable, pref);
            if (!ret.result)
            {
                Protokol.AddUnrecognisedRow("Ошибка записи параметров, смотрите лог ошибок");
                MonitorLog.WriteLog("Ошибка записи параметров в банк "  + pref, MonitorLog.typelog.Error, true);
            }
            return ret;
        }

        private bool CheckFileAccess(string fileName)
        {
            bool result = false;
            int countDenied = 0;
            while (!result && countDenied < 240)//240 раз 20 минут ждет доступа к файлу
            {
                try
                {
                    using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        result = true;
                        fs.Close();
                    }
                }
                catch
                {
                    result = false;
                }
                Thread.Sleep(5000);
                countDenied++;

            }
            return result;
        }

        private void RefreshWs()
        {
            ExlWs = (Microsoft.Office.Interop.Excel.Worksheet)ExlWb.Worksheets.get_Item(1); ;
            ExlWs.Visible = Microsoft.Office.Interop.Excel.XlSheetVisibility.xlSheetVisible;
        }

        protected override void InsertReestr()
        {
            var myFile = new DBMyFiles();
            var ret = myFile.AddFile(new ExcelUtility
            {
                nzp_user = ReportParams.User.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = Name,
                is_shared = 1,
                progress = 0
            });
            if (!ret.result) return;
            NzpExcelUtility = ret.tag;

            string sqlStr = "INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                            "(file_name, nzp, month_, year_, " +
                            " created_by, created_on, tip, download_status ) " +
                            "VALUES " +
                            " ( '" + FileName + "'," +
                            0 + "," + DateLoad.Month + "," + DateLoad.Year + "," +
                            ReportParams.User.nzp_user + ", " + DBManager.sCurDateTime + ", " + (int)SimpLoadTypeFile + "," + 2 + " )";

            ExecSQL(sqlStr);
            NzpLoad = GetSerialValue();
        }

        public override string GetProtocolName()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
           
            #region Формирование протокола

          

            var myFile = new DBMyFiles();


            string statusName = "Успешно";
            int download_status = 1;
            var rep = new FastReport.Report();
            if (Protokol.CountInsertedRows == 0)
            {
                statusName = "Не загружен";
                download_status = 3;
                if (Protokol.UnrecognizedRows.Rows.Count == 0 && Protokol.Comments.Rows.Count == 0 && Protokol.UncorrectRows.Rows.Count == 0)
                {
                    Protokol.AddComment("Файл " + FileName +
                                      " не соответствует установленному формату ");
                }
                Protokol.SetProcent(100, ExcelUtility.Statuses.Failed);
            }
            else
            {
                Protokol.SetProcent(100, ExcelUtility.Statuses.Failed);
                if (Protokol.UnrecognizedRows.Rows.Count > 0 || Protokol.Comments.Rows.Count > 0)
                {
                    statusName = "Загружено с ошибками";
                    download_status = 0;
                }
                if (Protokol.UncorrectRows.Rows.Count > 0)
                {
                    statusName = "Загружено с ошибками";
                    download_status = 0;
                }
            }
            var debt = new DataTable("debt");
            debt.Columns.Add(new DataColumn("bank", typeof(string)));
            debt.Columns.Add(new DataColumn("fio", typeof(string)));
            debt.Columns.Add(new DataColumn("adres", typeof(string)));
            debt.Columns.Add(new DataColumn("sum_money", typeof(float)));
            debt.Columns.Add(new DataColumn("sum_debt", typeof(float)));

            var env = new EnvironmentSettings();
            env.ReportSettings.ShowProgress = false;
            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(Protokol.UnrecognizedRows);
            fDataSet.Tables.Add(Protokol.Comments);
            fDataSet.Tables.Add(Protokol.UncorrectRows);
            fDataSet.Tables.Add(debt);
            string template = PathHelper.GetReportTemplatePath("protokol_load_opl.frx");
            rep.Load(template);
            rep.RegisterData(fDataSet);
            rep.GetDataSource("comment").Enabled = true;
            rep.GetDataSource("unrecog").Enabled = true;
            rep.GetDataSource("uncorrect").Enabled = true;
            rep.SetParameterValue("status", statusName);
            rep.SetParameterValue("count_rows", Protokol.CountInsertedRows);
            rep.SetParameterValue("file_name", FileName);
            rep.Pages[2].Visible = false;
            //хоть страницу 2 мы не показываем, но все равно он просит инициализацию полей
            rep.SetParameterValue("debt_comment", "");
            rep.Prepare();

            var exportXls = new FastReport.Export.OoXML.Excel2007Export();
            string fileName = "protocol_import_param_" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                              DateTime.Now.ToLongTimeString().Replace(":", "_") + ".xlsx";
            exportXls.ShowProgress = false;
            MonitorLog.WriteLog(fileName, MonitorLog.typelog.Info, 20, 201, true);
            try
            {
                if (!Directory.Exists(Constants.Directories.ReportDir))
                {
                    Directory.CreateDirectory(Constants.Directories.ReportDir);
                }
                exportXls.Export(rep, Path.Combine(Constants.Directories.ReportDir, fileName));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }

            rep.Dispose();


            //перенос  на ftp сервер
            if (InputOutput.useFtp)
            {
                fileName = InputOutput.SaveOutputFile(STCLINE.KP50.Global.Constants.Directories.ReportDir + fileName);
            }

            ProtocolFileName = STCLINE.KP50.Global.Constants.Directories.ReportDir + fileName;


            ExecSQL(" UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                    " SET nzp_exc = " + NzpExcelUtility + ", download_status = " + download_status + "," +
                    " temp_file = '" + TemporaryFileName + "', nzp = " + Nzp +
                    " WHERE nzp_load = " + NzpLoad);

            myFile.SetFileState(new ExcelUtility
            {
                nzp_exc = NzpExcelUtility,
                status = ExcelUtility.Statuses.Success,
                exc_path = fileName,
                progress = 100
            });

            #endregion

            return ProtocolFileName;
        }


    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bars.KP50.DB.Faktura;
using Bars.KP50.Faktura.Source.BillProvider;
using Bars.KP50.Utils;
using FastReport;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using Globals.SOURCE.Container;

namespace Bars.KP50.Faktura.Source.Base
{
    //----------------------------------------------------------------------
    public partial class DbFaktura
    //----------------------------------------------------------------------
    {
        Dictionary<int, string> _formulList;
        private STCLINE.KP50.Interfaces.Faktura _finder;
        // ConvertTarif _convertedTarifs = null;

        IDbConnection _conDb;


        /// <summary>
        /// Возвращает код счет-фактуры по умолчанию, по nzp_wp или nzp_kvar
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public int GetDefaultFaktura(Ls finder, out Returns ret)
        {
            int FakturaKod = 0;
            int nzp_wp = 0;
            var connDb = DBManager.GetConnection(Constants.cons_Kernel);
            try
            {
                ret = DBManager.OpenDb(connDb, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Счет-квитанция : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return FakturaKod;
                }
                string s = "";
                //если nzp_kvar известен
                if (finder.nzp_kvar > 0)
                {
                    ret = STCLINE.KP50.Global.Utils.InitReturns();
                    s = "SELECT nzp_wp FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar WHERE nzp_kvar=" + finder.nzp_kvar;
                    var obj = DBManager.ExecScalar(connDb, s, out ret, true);
                    if (!ret.result) return FakturaKod;
                    if (obj != DBNull.Value && obj != null)
                        nzp_wp = Convert.ToInt32(obj);
                }
                else
                    if (finder.nzp_wp > 0)
                    {
                        nzp_wp = finder.nzp_wp;
                    }
                if (finder.nzp_user > 0 && nzp_wp <= 0)
                {
                    IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);
                    ret = DBManager.OpenDb(connWeb, true);
                    string tXxSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + finder.nzp_user + "_spls";
                    connWeb.Close();
                    s = "SELECT DISTINCT  k.nzp_wp FROM " + Points.Pref + DBManager.sDataAliasRest +
                        "kvar k , " + tXxSpls + " ls where k.nzp_kvar=ls.nzp_kvar ";
                    DataTable DT = ClassDBUtils.OpenSQL(s, connDb).GetData();
                    if (DT.Rows.Count > 0)
                    {
                        if (DT.Rows.Count > 1)
                        {
                            nzp_wp = 0;
                        }
                        else
                        {
                            nzp_wp = (DT.Rows[0]["nzp_wp"] != DBNull.Value ? Convert.ToInt32(DT.Rows[0]["nzp_wp"]) : 0);
                        }
                    }
                }

                s = " SELECT DISTINCT kind_proc " +
                           " FROM " + Points.Pref + DBManager.sDataAliasRest + "proc_factura " +
                           " WHERE nzp_wp=" + nzp_wp;
                var kind = DBManager.ExecScalar(connDb, s, out ret, true);
                if (!ret.result) return FakturaKod;
                if (kind != DBNull.Value && kind != null)
                    FakturaKod = Convert.ToInt32(kind);
                StreamWriter sw = new StreamWriter(@"C:\temp\peopleError.txt", false);
                sw.WriteLine(FakturaKod);
                sw.Close();
                return FakturaKod;
            }
            finally
            {
                DBManager.CloseDb(connDb);
            }
        }

        /// <summary>
        /// Получение произвольного текста для платежного документа
        /// </summary>
        /// <param name="facturaName">Название платежного документа</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string GetCustomTextFaktura(string facturaName, out Returns ret)
        {
            string returnValue = "";
            var connDb = DBManager.GetConnection(Constants.cons_Kernel);
            try
            {
                ret = DBManager.OpenDb(connDb, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Счет-квитанция : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return returnValue;
                }
                string sqlString = string.Format("select custom_text from {0}{1}s_listfactura where name_rus='{2}'", Points.Pref, DBManager.sKernelAliasRest, facturaName);
                var obj = DBManager.ExecScalar(connDb, sqlString, out ret, true);
                if (obj != DBNull.Value && obj != null && ret.result)
                    returnValue = Convert.ToString(obj);
            }
            finally
            {
                DBManager.CloseDb(connDb);
            }

            return returnValue;
        }

        /// <summary>
        /// Получение произвольного текста для платежного документа
        /// </summary>
        /// <param name="facturaName">Название платежного документа</param>
        /// <param name="newCustomText"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public void SaveCustomTextFaktura(string facturaName, string newCustomText, out Returns ret)
        {
            var connDb = DBManager.GetConnection(Constants.cons_Kernel);
            const int maxTextLength = 1000;
            try
            {
                ret = DBManager.OpenDb(connDb, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Счет-квитанция : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return;
                }
                if (newCustomText.Length > maxTextLength)
                {
                    newCustomText = newCustomText.Substring(0, maxTextLength);
                }
                string sqlString = string.Format("update {0}{1}s_listfactura set custom_text='{2}' where name_rus='{3}'", Points.Pref, DBManager.sKernelAliasRest, newCustomText, facturaName);
                ret = DBManager.ExecSQL(connDb, sqlString, true);
            }
            finally
            {
                DBManager.CloseDb(connDb);
            }
        }

        /// <summary>
        /// Возвращает список счетов-квитанций
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<string> GetListFaktura(out Returns ret)
        {
            var connDb = DBManager.GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            var lFactura = new List<string>();
            try
            {
                ret = DBManager.OpenDb(connDb, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Счет-квитанция : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return new List<string>();
                }

                ret = STCLINE.KP50.Global.Utils.InitReturns();
                string s = " SELECT * " +
                           " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_listfactura " +
                           " WHERE townfilter >0 AND kind>=100" +
                           " ORDER BY default_ desc, name_rus  ";
                IDbCommand cmd = DBManager.newDbCommand(s, connDb);

                try
                {
                    IDataReader reader = cmd.ExecuteReader();


                    while (reader.Read())
                    {
                        lFactura.Add(reader["kind"].ToString().Trim() + "=" + reader["name_rus"].ToString().Trim());
                    }
                    reader.Close();
                    reader.Dispose();
                    cmd.Dispose();
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Счет-квитанция :ошибка при выборе типов квитанций " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.result = false;
                }


            }
            finally
            {
                DBManager.CloseDb(connDb);
            }

            //var billProvider = IocContainer.Current.Resolve<IBillProvider>();
            //if (billProvider != null)
            //{
            //    var listBill = billProvider.GetBills();
            //    lFactura.AddRange(listBill.Select(billInfo => billInfo.Code + "=" + billInfo.Name));
            //}
            //StreamWriter sw = new StreamWriter(@"C:\temp\people123.txt", false);
            //foreach (string str in lFactura)
            //    sw.WriteLine(str);
            //sw.Close();
            return lFactura;
        }


        /// <summary> Сформировать счет фактуру
        /// </summary>
        public List<string> GetFaktura(STCLINE.KP50.Interfaces.Faktura finder, out Returns ret)
        {
            //StreamWriter sw = new StreamWriter(@"C:\temp\people123.txt", false);
            //sw.WriteLine("Sfsafgdsagdsgdsg");
            
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            List<string> lString;

            _finder = finder;

            _conDb = DBManager.GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            try
            {
                ret = DBManager.OpenDb(_conDb, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Счет-квитанция : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return new List<string>();
                }



                if (finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One)
                {
                    //sw.WriteLine("MakeSingleFaktura");
                    lString = MakeSingleFaktura(out ret);
                }
                else
                    if (finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Web)
                    {
                        //sw.WriteLine("MakeWebFaktura");
                        lString = MakeWebFaktura(out ret);
                    }
                    else
                    {
                        //sw.WriteLine("MakeGroupFaktura");
                        lString = MakeGroupFaktura(out ret);
                    }

            }
            finally
            {
                DBManager.CloseDb(_conDb);
            }
            //sw.WriteLine("End");
           // sw.Close();
            return lString;

        }//GetFaktura


        /// <summary>
        /// Получение счета по одному ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<string> MakeSingleFaktura(out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            string fileName;
            int typeAlg;
            ret.result = false;

            var resultfacturaName = new List<string>();

            if (_finder.pref == "")
            {
                return resultfacturaName;
            }
            //Определяем имя фактуры и тип алгоритма заполнения
            ret = GetFakturaById(out typeAlg, out fileName);
            if (!ret.result)
            {
                return resultfacturaName;
            }

            if (!File.Exists(PathHelper.GetReportTemplatePath(fileName)))
            {
                return resultfacturaName;
            }

            DataSet fDataSet;
            var maxKodOldFaktura = (int)Enum.GetValues(typeof(STCLINE.KP50.Interfaces.Faktura.FakturaTypes)).Cast<STCLINE.KP50.Interfaces.Faktura.FakturaTypes>().Max();
            // Заполняем данные
            if (((_finder.idFaktura > maxKodOldFaktura) & (_finder.idFaktura != 1006)) | (_finder.idFaktura == 102))
            {
                fDataSet = GetGroupDataSet2(_finder, out ret);
            }
            else
            {
                fDataSet = GetGroupDataSet(out ret);
            }


            if (!ret.result || fDataSet == null || fDataSet.Tables.Count == 0 || fDataSet.Tables[0].Rows.Count == 0)
            {
                StreamWriter sw = new StreamWriter(@"C:\Temp\BuildFakePage1.txt");
                sw.WriteLine("1");
                sw.WriteLine(fDataSet.Tables.Count);
                sw.WriteLine(fDataSet.Tables[0].Rows.Count);
                sw.Close();
                BuildFakePage(resultfacturaName, out ret);
                return resultfacturaName;
            }
            SaveSf(fDataSet.Tables[0]);

            using (var rep = new FastReport.Report())
            {
                var env = new EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                rep.Load(PathHelper.GetReportTemplatePath(fileName));
                rep.RegisterData(fDataSet);
                rep.GetDataSource("Q_master").Enabled = true;
                try
                {
                    rep.Prepare();
                    ret.text = "Счет сформирован";
                    ret.tag = rep.Report.PreparedPages.Count;

                    if (_finder.resultFileType == STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.FPX)
                    {

                        var ms = new MemoryStream { Position = 0 };
                        rep.SavePrepared(ms);
                        ms.Flush();
                        ms.Close();


                        string destinationFilename = Convert.ToBase64String(ms.ToArray());
                        ms.Close();
                        resultfacturaName.Add(destinationFilename);
                        return resultfacturaName;
                    }
                    else
                    {
                        var exportPdf = new FastReport.Export.Pdf.PDFExport { ShowProgress = false };
                        var ms = new MemoryStream { Position = 0 };
                        exportPdf.Export(rep, ms);
                        ms.Flush();
                        ms.Close();

                        string destinationFilename = Convert.ToBase64String(ms.ToArray());
                        ms.Close();

                        resultfacturaName.Add(destinationFilename);
                        return resultfacturaName;
                    }



                }
                catch (Exception ex)
                {
                    ret.text = "Счет не сформированы";
                    MonitorLog.WriteLog(
                        "Ошибка формирования счета 111111" + ex.Message + " # " +
                        (ex.InnerException == null ? "" : ex.InnerException.Message), MonitorLog.typelog.Error, 20, 201,
                        true);
                }
            }

            return resultfacturaName;

        } //MakeSingleFaktura

        /// <summary>
        /// Формирование счета для Веба
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<string> MakeWebFaktura(out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            string fileName;
            int typeAlg;
            ret.result = false;

            var resultfacturaName = new List<string>();

            if (_finder.pref == "")
            {
                return resultfacturaName;
            }
            //Определяем имя фактуры и тип алгоритма заполнения
            ret = GetFakturaById(out typeAlg, out fileName);
            if (!ret.result)
            {
                return resultfacturaName;
            }

            if (!File.Exists(Directory.GetCurrentDirectory() + @"\Template\" + fileName))
            {
                return resultfacturaName;
            }


            // Заполняем данные
            DataSet fDataSet;
            var maxKodOldFaktura = (int)Enum.GetValues(typeof(STCLINE.KP50.Interfaces.Faktura.FakturaTypes)).Cast<STCLINE.KP50.Interfaces.Faktura.FakturaTypes>().Max();

            if ((_finder.idFaktura > maxKodOldFaktura) & (_finder.idFaktura != 1006))
            {
                fDataSet = GetGroupDataSet2(_finder, out ret);
            }
            else
            {
                fDataSet = GetGroupDataSet(out ret);
            }
            if (!ret.result)
            {
                return resultfacturaName;
            }

            if (fDataSet == null || fDataSet.Tables.Count == 0 || fDataSet.Tables[0].Rows.Count == 0)
            {
                BuildFakePage(resultfacturaName, out ret);

                return resultfacturaName;
            }




            SaveSf(fDataSet.Tables[0]);

            using (var rep = new FastReport.Report())
            {
                var env = new EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                rep.Load(PathHelper.GetReportTemplatePath(fileName));
                rep.RegisterData(fDataSet);
                rep.GetDataSource("Q_master").Enabled = true;
                try
                {
                    rep.Prepare();
                    ret.tag = rep.Report.PreparedPages.Count;
                    string destinationFilename = Constants.Directories.BillWebDir + _finder.destFileName;
                    if (_finder.resultFileType == STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.FPX)
                    {
                        destinationFilename += ".fpx";
                        rep.SavePrepared(destinationFilename);
                    }
                    else
                    {
                        destinationFilename += ".pdf";
                        var exportPdf = new FastReport.Export.Pdf.PDFExport { ShowProgress = false };
                        exportPdf.Compressed = false;
                        exportPdf.Export(rep, destinationFilename + ".pdf");
                    }

                    if (File.Exists(destinationFilename))
                    {
                        if (InputOutput.useFtp) destinationFilename = InputOutput.SaveBill(destinationFilename);
                        //FtpUtility ftp = new FtpUtility("192.168.179.15", "Administrator", "пароль");
                        //ftp.UploadFile(destinationFilename, "bill\\web");
                    }
                    else
                    {
                        MonitorLog.WriteLog("Файл " + destinationFilename + " не сформирован ", MonitorLog.typelog.Error,
                            20, 201, true);
                    }
                    resultfacturaName.Add(destinationFilename);

                    return resultfacturaName;


                }
                catch (Exception ex)
                {
                    ret.text = "";
                    MonitorLog.WriteLog("Ошибка формирования счета 22222" + ex.Message, MonitorLog.typelog.Error, 20, 201,
                        true);
                }

            }

            return resultfacturaName;

        }


        /// <summary>
        /// Заглушка используемая, в том случае, если начислений нет
        /// </summary>
        /// <param name="resultfacturaName"></param>
        private void BuildFakePage(List<string> resultfacturaName, out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            using (var rep = new FastReport.Report())
            {
                var env = new EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                rep.Load(PathHelper.GetReportTemplatePath("FakePage.frx"));
                rep.SetParameterValue("header", "");
                try
                {
                    rep.Prepare();
                    ret.tag = 1;
                    string destinationFilename;
                    #region Единичное формирование счета
                    if (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One)
                    {
                        if (_finder.resultFileType == STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.FPX)
                        {

                            var ms = new MemoryStream { Position = 0 };
                            rep.SavePrepared(ms);
                            ms.Flush();
                            ms.Close();


                            destinationFilename = Convert.ToBase64String(ms.ToArray());
                            ms.Close();
                            resultfacturaName.Add(destinationFilename);

                        }
                        else
                        {
                            var exportPdf = new FastReport.Export.Pdf.PDFExport { ShowProgress = false };
                            var ms = new MemoryStream { Position = 0 };
                            exportPdf.Compressed = false;
                            exportPdf.Export(rep, ms);
                            ms.Flush();
                            ms.Close();

                            destinationFilename = Convert.ToBase64String(ms.ToArray());
                            ms.Close();

                            resultfacturaName.Add(destinationFilename);

                        }
                        return;
                    }
                    #endregion

                    #region Для Веба

                    if (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Web)
                    {
                        destinationFilename = Constants.Directories.BillWebDir + _finder.destFileName;

                        if (_finder.resultFileType == STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.FPX)
                        {
                            destinationFilename += ".fpx";
                            rep.SavePrepared(destinationFilename);
                        }
                        else
                        {
                            destinationFilename += ".pdf";
                            var exportPdf = new FastReport.Export.Pdf.PDFExport { ShowProgress = false };
                            exportPdf.Compressed = false;
                            exportPdf.Export(rep, destinationFilename);
                        }

                        if (InputOutput.useFtp
                            && (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Web))
                            destinationFilename = InputOutput.SaveBill(destinationFilename);
                        resultfacturaName.Add(destinationFilename);
                        return;
                    }

                    #endregion

                    #region Массовая печать
                    if (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Group ||
                        _finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Bank)
                    {
                        destinationFilename = Constants.ExcelDir + _finder.destFileName + "_" +
                                                          Process.GetCurrentProcess().Id + '_' +
                                                            "00_" + DateTime.Now.GetHashCode();
                    }
                    else
                    {
                        destinationFilename = Constants.Directories.BillWebDir + _finder.destFileName;
                    }

                    if (_finder.resultFileType == STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.FPX)
                    {
                        destinationFilename += ".fpx";
                        rep.SavePrepared(destinationFilename);
                    }
                    else
                    {
                        destinationFilename += ".pdf";
                        var exportPdf = new FastReport.Export.Pdf.PDFExport { ShowProgress = false };
                        exportPdf.Compressed = false;
                        exportPdf.Export(rep, destinationFilename);
                    }

                    if (File.Exists(destinationFilename))
                    {
                    }
                    else
                    {
                        MonitorLog.WriteLog("Файл " + destinationFilename + " не сформирован ", MonitorLog.typelog.Error,
                            20, 201, true);
                    }
                    resultfacturaName.Add(destinationFilename);

                    #endregion

                }
                catch (Exception ex)
                {
                    ret.text = "";
                    MonitorLog.WriteLog("Ошибка формирования счета 333333" + ex.Message, MonitorLog.typelog.Error, 20, 201,
                        true);
                }

            }

        } //MakeSingleFaktura

        /// <summary>
        /// Массовое формирование счетов
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<string> MakeGroupFaktura(out Returns ret)
        {
            //StreamWriter sw = new StreamWriter(@"C:\temp\people999.txt", false);
            var resultfacturaName = new List<string>();
            string fileName;
            int typeAlg;

            #region Инициализация

            //Определяем имя фактуры и тип алгоритма заполнения
            ret = GetFakturaById(out typeAlg, out fileName);
            if (!ret.result)
            {
                return resultfacturaName;
            }

            if (!File.Exists(PathHelper.GetReportTemplatePath(fileName)))
            {
                return resultfacturaName;
            }

            #endregion
            //sw.WriteLine(typeAlg.ToString());
            //sw.WriteLine(fileName);
            
            #region Заполняем данные
            DataSet fDataSet;
            //Получаем максимальный код счет-фактур для старого алгоритма
            var maxKodOldFaktura = (int)Enum.GetValues(typeof(STCLINE.KP50.Interfaces.Faktura.FakturaTypes)).Cast<STCLINE.KP50.Interfaces.Faktura.FakturaTypes>().Max();

            if (((_finder.idFaktura > maxKodOldFaktura) & (_finder.idFaktura != 1006)) | (_finder.idFaktura == 102))
            {
                //sw.WriteLine("GetGroupDataSet2");
                fDataSet = GetGroupDataSet2(_finder, out ret);
            }
            else
            {
                //sw.WriteLine("GetGroupDataSet");
                fDataSet = GetGroupDataSet(out ret);
            }
            

            #endregion
            UpdateBillFon(60);

            if (!ret.result || fDataSet == null || fDataSet.Tables.Count == 0 || fDataSet.Tables[0].Rows.Count == 0)
            {
                //sw.WriteLine("BuildFakePage");
                //sw.WriteLine(fDataSet.Tables.Count);
                //sw.WriteLine(fDataSet.Tables[0].Rows.Count);
                BuildFakePage(resultfacturaName, out ret);
            }
            else
            {
                //sw.WriteLine("SaveSf");
                SaveSf(fDataSet.Tables[0]);

                #region Формирование счетов

                int maxPackCount = fDataSet.Tables[0].Rows.Count / Math.Max(_finder.countListInPack, 1);
                if (maxPackCount == fDataSet.Tables[0].Rows.Count) maxPackCount = 1;
                if (maxPackCount * Math.Max(_finder.countListInPack, 1) < fDataSet.Tables[0].Rows.Count)
                {
                    maxPackCount++;
                }
                int count_printed_rows = 0;
                int count_repeat = 2;
                do
                {
                    if (count_repeat < 2)
                    {
                        MonitorLog.WriteLog("Ошибка формирования счета, запущена повторная попытка ",
                            MonitorLog.typelog.Error, 20, 201, true);
                    }
                    MonitorLog.WriteLog(
                        "Формирование " + maxPackCount + " пачек(пачки), число квитанций :" +
                        fDataSet.Tables[0].Rows.Count + ", банк данных:" + _finder.pref, MonitorLog.typelog.Info, true);
                    if (fDataSet.Tables[0].Rows.Count == 0)
                    {
                        MonitorLog.WriteLog("Ноль записей в DataSet ", MonitorLog.typelog.Info, true);
                    }
                    count_printed_rows = 0;

                    decimal increm = 30 / (decimal)maxPackCount;
                    for (int i = 0; i < maxPackCount; i++)
                    {
                        var clonedataset = new DataSet();
                        //var listDataTableClones = (from DataTable table in fDataSet.Tables select table.Clone()).ToList();
                        int fromNumber = i * Math.Max(_finder.countListInPack, 1);
                        int toNumber = (i + 1) * Math.Max(_finder.countListInPack, 1);
                        for (int tableNumber = 0; tableNumber < fDataSet.Tables.Count; tableNumber++)
                        {
                            var t = fDataSet.Tables[tableNumber].Copy();
                            if (t.Rows.Count != 0)
                            {
                                int fromLocal = fromNumber;
                                int toLocal = toNumber;
                                if (tableNumber != 0 && t.Columns["number"] != null)
                                {
                                    fromLocal = t.AsEnumerable().ToList().FindLastIndex(r => Convert.ToInt32(r["number"]) < fromNumber);
                                    toLocal = t.AsEnumerable().ToList().FindLastIndex(r => Convert.ToInt32(r["number"]) <= toNumber);
                                }
                                clonedataset.Tables.Add(
                                    t.AsEnumerable()
                                        .Skip(fromLocal != -1 ? fromLocal : fromNumber)
                                        .Take(Math.Min(toLocal != -1 ? toLocal : toNumber,
                                            fDataSet.Tables[tableNumber].Rows.Count))
                                        .CopyToDataTable());
                            }
                            else
                                clonedataset.Tables.Add(t);
                            clonedataset.Tables[tableNumber].TableName = tableNumber == 0
                                ? "Q_master"
                                : "Q_master" + tableNumber;
                        }

                        //DataTable dtCurrent = fDataSet.Tables[0].Clone();

                        //for (int j = fromNumber; j < Math.Min(toNumber, fDataSet.Tables[0].Rows.Count); j++)
                        //{
                        //    foreach (DataTable table in listDataTableClones)
                        //    {
                        //        table.ImportRow();
                        //    }
                        //    dtCurrent.ImportRow(fDataSet.Tables[0].Rows[j]);
                        //}
                        //clonedataset.Tables.Add(dtCurrent);
                        int uniqueName = DateTime.Now.GetHashCode();
                        
                        #region Формирование ЕПД
                        using (var rep = new FastReport.Report())
                        {
                            var env = new EnvironmentSettings();
                            env.ReportSettings.ShowProgress = false;
                            rep.Load(PathHelper.GetReportTemplatePath(fileName));
                            rep.RegisterData(clonedataset);
                            for (int tableNumber = 0; tableNumber < clonedataset.Tables.Count; tableNumber++)
                                rep.GetDataSource(clonedataset.Tables[tableNumber].TableName).Enabled = true;
                            //StreamWriter sw1 = new StreamWriter(@"C:\Temp\QWERTY.txt");
                            try
                            {
                                //sw.WriteLine("01");
                                try
                                {
                                    rep.Prepare();
                                }
                                catch (Exception)
                                {
                                    
                                }
                                
                                //sw.WriteLine("02");
                                ret.text = "Счет сформирован";
                                #region 24.10.2014
                                //ret.tag = rep.Report.PreparedPages.Count;
                                //проверяем на int отношения количество отпечатанных страниц / количество записей в датасете
                                //если оно int - значит все правильно, просто на одну квитанцию - больше одного листа 
                                int pages_rcds_relation = rep.Report.PreparedPages.Count / clonedataset.Tables[0].Rows.Count;
                                //sw.WriteLine("03");
                                if (((double)rep.Report.PreparedPages.Count / (double)clonedataset.Tables[0].Rows.Count).Equals(rep.Report.PreparedPages.Count / clonedataset.Tables[0].Rows.Count))
                                {
                                    ret.tag = clonedataset.Tables[0].Rows.Count;
                                }
                                else
                                {
                                    ret.tag = rep.Report.PreparedPages.Count;
                                }

                                #endregion



                                //sw.WriteLine("1");
                                //ret.tag = rep.Report.PreparedPages.Count;

                                count_printed_rows += ret.tag;
                                MonitorLog.WriteLog(
                                    "Сформирована пачка №" + (i + 1) + " из " + maxPackCount + ", число квитанций :" +
                                    ret.tag, MonitorLog.typelog.Info, true);
                                //sw.WriteLine("2");
                                string destinationFilename = Constants.ExcelDir + _finder.destFileName + "_" +
                                                             Process.GetCurrentProcess().Id + '_' +
                                                             i.ToString("00") + '_' +
                                                             uniqueName + '_' + (fromNumber + 1) + '_' +
                                                             toNumber;

                                //sw.WriteLine("3 = " + destinationFilename);
                                if (_finder.resultFileType == STCLINE.KP50.Interfaces.Faktura.FakturaFileTypes.FPX)
                                {
                                    rep.SavePrepared(destinationFilename + ".fpx");
                                    rep.Dispose();
                                    resultfacturaName.Add(destinationFilename + ".fpx");
                                }
                                else
                                {
                                    var exportPdf = new FastReport.Export.Pdf.PDFExport { ShowProgress = false };
                                    exportPdf.Compressed = false;
                                    exportPdf.Export(rep, destinationFilename + ".pdf");
                                    exportPdf.Dispose();
                                    resultfacturaName.Add(destinationFilename + ".pdf");
                                }
                                //sw.WriteLine("4");
                                clonedataset.Dispose();
                                //sw.WriteLine("5");
                                //sw.Close();

                            }
                            catch (Exception ex)
                            {
                                //sw.WriteLine(ex.ToString());
                                //sw.Close();
                                ret.text = "Счет не сформированы";
                                fDataSet.Dispose();
                                MonitorLog.WriteLog("Ошибка формирования счета 444444" + ex.Message, MonitorLog.typelog.Error,
                                    20, 201,
                                    true);
                                fDataSet.Dispose();
                                rep.Dispose();
                                return null;


                            }
                        }
                        #endregion

                        UpdateBillFon(60 + Decimal.ToInt32((i + 1) * increm));
                    }

                    count_repeat--;
                    if (fDataSet.Tables[0].Rows.Count == 0) count_printed_rows = 0;
                } while (fDataSet.Tables[0].Rows.Count != count_printed_rows && count_repeat > 0);
                // повторяем формирование, если число распечатанных ЕПД не соотвествует заявленному два раза

                fDataSet.Dispose();

                #endregion
            }
            //sw.Close();

            UpdateBillFon(92);
            #region Архивация
            try
            {
                string fileN = DateTime.Now.ToShortDateString() + "_" +
                                DateTime.Now.GetHashCode() +"_"+Process.GetCurrentProcess().Id+"_"+
                                Thread.CurrentThread.ManagedThreadId;

                fileName = _finder.year_ + _finder.month_ + "_" + fileN + ".zip";
                if (resultfacturaName.Count != 0)
                    Archive.GetInstance(ArchiveFormat.Zip).Compress(Constants.ExcelDir + @"\" + fileName, resultfacturaName.ToArray(), true);
            }
            catch (Exception ex)
            {
                ret.text = "Счет сформированы, но не могут быть добавлены в архив, возможно на сервере отсутствует архиватор";
                ret.result = false;
                ret.tag = -1;
                MonitorLog.WriteLog("Ошибка архивирования счета " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            #endregion


            #region Удаление файлов
            try
            {
                foreach (string t in resultfacturaName)
                {
                    try
                    {
                        File.Delete(t);
                    }
                    catch (Exception e)
                    {
                        MonitorLog.WriteLog("Счет-квитанция : Ошибка удаления файла " + e.Message, MonitorLog.typelog.Error, true);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text = "Счет сформированы, но система не может получить к ним доступ ";
                MonitorLog.WriteLog("Ошибка формирования счета 55555555" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;

            }
            resultfacturaName.Clear();
            #endregion



            if (File.Exists(Constants.ExcelDir + @"\" + fileName))
            {
                if (InputOutput.useFtp) fileName = InputOutput.SaveBill(Constants.ExcelDir + @"\" + fileName);
                //FtpUtility ftp = new FtpUtility("192.168.179.15", "Administrator", "ledorub8");
                //ftp.UploadFile(STCLINE.KP50.Global.Constants.ExcelDir + @"\"+ fileName, "bill");
            }
            else
            {
                MonitorLog.WriteLog("Файл " + Constants.ExcelDir + @"\" + fileName + " не сформирован ", MonitorLog.typelog.Error, 20, 201, true);
            }

            UpdateBillFon(100);
            resultfacturaName.Add(fileName);
            ret.text = fileName == "" ? "Нет сформированных счетов" : "Счета сформированы";
            return resultfacturaName;

        } //MakeGroupFaktura

        /// <summary>
        /// Получение имени файла по типу алгоритма счета
        /// </summary>
        /// <param name="typeAlg"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Returns GetFakturaById(out int typeAlg, out string fileName)
        {

            typeAlg = 1;
            fileName = "";
            #region Подключение к БД

            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();


            #endregion

            string s = " SELECT * " +
                       " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_listfactura " +
                       " WHERE kind = " + _finder.idFaktura;

            IDbCommand cmd = DBManager.newDbCommand(s, _conDb);
            try
            {
                IDataReader reader = cmd.ExecuteReader();


                if (reader.Read())
                {
                    fileName = reader["file_name"].ToString().Trim();
                }
                reader.Close();
                reader.Dispose();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Счет-квитанция :ошибка при выборе типов квитанций " + ex.Message,
                    MonitorLog.typelog.Error, true);

            }
            if (fileName.IndexOf("~/App_Data/", StringComparison.Ordinal) > -1) fileName = fileName.Substring(11);

            if (fileName != "") return ret;

            #region Получение имени файла счета
            //var billProvider = IocContainer.Current.Resolve<IBillProvider>();
            //if (billProvider != null)
            //{
            //    var listBill = billProvider.GetBills();
            //    foreach (var billInfo in listBill)
            //    {
            //        if (billInfo.Code == _finder.idFaktura.ToString()) fileName = billInfo.FileName;
            //    }
            //    return ret;
            //}
            #endregion


            fileName = "demo.frx";
            switch (_finder.idFaktura)
            {
                case 100: fileName = "samara354.frx"; break;
                case 199: fileName = "samara354.frx"; break;
                case 101: fileName = "samara_dolg.frx"; break;
                case 102: fileName = "saha.frx"; break;
                case 103: fileName = "demo.frx"; break;
                case 104: fileName = "gubkin354.frx"; break;
                case 105: fileName = "Kapr.frx"; break;
                case 106: fileName = "astrahan354.frx"; break;
                case 1006: fileName = "astrahan354_old.frx"; break;
                case 107: fileName = "sam_bar.frx"; break;
                case 122: fileName = "proseka.frx"; break;
                case 108: fileName = "sam_bar2.frx"; break;
                //case 199: fileName = "samaraNew.frx"; break;
                //case 108: fileName = "Kaluga354.frx"; break;
                //case 122: fileName = "tula354_2.frx"; break;
            }

            return ret;
        }

        /// <summary>
        /// Получение списка коммунальных услуг
        /// </summary>
        /// <param name="prefKernel"></param>
        /// <param name="bill"></param>
        /// <param name="ret"></param>
        public void GetListKommServ(string prefKernel, ref BaseFactura bill, out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            IDataReader reader = null;
            bill.ListKommServ.Clear();

            string s = " SELECT nzp_serv  " +
                       " FROM " + prefKernel + "grpserv_schet a " +
                       " WHERE nzp_grpserv = 2 ";

            IDbCommand cmd = DBManager.newDbCommand(s, _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {


                    if (reader["nzp_serv"] != DBNull.Value)
                    {
                        var bs = new BaseServ(false)
                        {
                            Serv = { NzpServ = Convert.ToInt32(reader["nzp_serv"]) },
                            KommServ = true
                        };
                        bill.ListKommServ.Add(bs);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Отсутствует необходима таблица " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

        }


        /// <summary>
        /// Получение  банковских реквизитов УК
        /// </summary>
        /// <param name="spis"></param>
        /// <param name="nzpArea"></param>
        /// <param name="nzpGeu"></param>
        /// <param name="pkod"></param>
        /// <returns></returns>
        public _Rekvizit GetUkBankRekvizit(List<_Rekvizit> spis, int nzpArea, int nzpGeu, string pkod)
        {
            foreach (_Rekvizit t in spis)
            {
                if (nzpArea == 5000)
                {
                    if ((t.nzp_area == nzpArea) & (t.nzp_geu == nzpGeu)) return t;
                }
                else
                    if (t.nzp_area == nzpArea) return t;
            }

            var uk = new _Rekvizit
            {
                poluch = "не определены банковские реквизиты ",
                poluch2 = "не определены банковские реквизиты"
            };


            return uk;
        }

        /// <summary>
        /// Получение списка банковских реквизитов УК
        /// </summary>
        /// <param name="prefData">Префикс БД</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<_Rekvizit> GetListUkBankRekvizit(string prefData, out Returns ret)
        {
            IDataReader reader;
            var spis = new List<_Rekvizit>();
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            string s = " SELECT *  " +
                       " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_bankstr " +
                       " ORDER BY nzp_area, nzp_geu  ";

            IDbCommand cmd = DBManager.newDbCommand(s, _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var uk = new _Rekvizit();

                    if (reader["nzp_geu"] != DBNull.Value) uk.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    if (reader["nzp_area"] != DBNull.Value) uk.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["sb1"] != DBNull.Value) uk.poluch = Convert.ToString(reader["sb1"]).Trim();
                    if (reader["sb2"] != DBNull.Value) uk.bank = Convert.ToString(reader["sb2"]).Trim();
                    if (reader["sb3"] != DBNull.Value) uk.rschet = Convert.ToString(reader["sb3"]).Trim();
                    if (reader["sb4"] != DBNull.Value) uk.korr_schet = Convert.ToString(reader["sb4"]).Trim();
                    if (reader["sb5"] != DBNull.Value) uk.bik = Convert.ToString(reader["sb5"]).Trim();
                    if (reader["sb6"] != DBNull.Value) uk.inn = Convert.ToString(reader["sb6"]).Trim();
                    if (reader["sb7"] != DBNull.Value) uk.phone = Convert.ToString(reader["sb7"]).Trim();
                    if (reader["sb8"] != DBNull.Value) uk.adres = Convert.ToString(reader["sb8"]).Trim();
                    if (reader["sb9"] != DBNull.Value) uk.pm_note = Convert.ToString(reader["sb9"]).Trim();
                    if (reader["sb10"] != DBNull.Value) uk.poluch2 = Convert.ToString(reader["sb10"]).Trim();
                    if (reader["sb11"] != DBNull.Value) uk.bank2 = Convert.ToString(reader["sb11"]).Trim();
                    if (reader["sb12"] != DBNull.Value) uk.rschet2 = Convert.ToString(reader["sb12"]).Trim();
                    if (reader["sb13"] != DBNull.Value) uk.korr_schet2 = Convert.ToString(reader["sb13"]).Trim();
                    if (reader["sb14"] != DBNull.Value) uk.bik2 = Convert.ToString(reader["sb14"]).Trim();
                    if (reader["sb15"] != DBNull.Value) uk.inn2 = Convert.ToString(reader["sb15"]).Trim();
                    if (reader.FieldCount > 17)
                    {
                        if (reader["sb16"] != DBNull.Value) uk.phone2 = Convert.ToString(reader["sb16"]).Trim();
                        if (reader["sb17"] != DBNull.Value) uk.adres2 = Convert.ToString(reader["sb17"]).Trim();
                        if (reader["filltext"] != DBNull.Value) uk.filltext = Convert.ToInt32(reader["filltext"]);
                    }
                    else
                    {
                        uk.filltext = 1;
                    }


                    spis.Add(uk);

                }
                reader.Close();
                cmd.Dispose();

                cmd = DBManager.newDbCommand(s, _conDb);

                s = " SELECT *  " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "prm_10 " +
                    " where nzp_prm=80 and is_actual<>100 order by dat_po desc";
                cmd = DBManager.newDbCommand(s, _conDb);
                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    foreach (_Rekvizit uk in spis)
                    {
                        uk.ercName = reader["val_prm"].ToString().Trim();
                    }
                }
                reader.Close();


                ret.result = true;
                if (spis.Count > 0)
                    return spis;
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки 26 |" + s, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = "Ошибка выборки 26 |" + s;
            }
            finally
            {
                cmd.Dispose();
            }

            s = " SELECT *  " +
                " FROM " + prefData + "s_bankstr " +
                " ORDER BY nzp_area, nzp_geu  ";

            cmd = DBManager.newDbCommand(s, _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var uk = new _Rekvizit();

                    if (reader["nzp_geu"] != DBNull.Value) uk.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    if (reader["nzp_area"] != DBNull.Value) uk.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["sb1"] != DBNull.Value) uk.poluch = Convert.ToString(reader["sb1"]).Trim();
                    if (reader["sb2"] != DBNull.Value) uk.bank = Convert.ToString(reader["sb2"]).Trim();
                    if (reader["sb3"] != DBNull.Value) uk.rschet = Convert.ToString(reader["sb3"]).Trim();
                    if (reader["sb4"] != DBNull.Value) uk.korr_schet = Convert.ToString(reader["sb4"]).Trim();
                    if (reader["sb5"] != DBNull.Value) uk.bik = Convert.ToString(reader["sb5"]).Trim();
                    if (reader["sb6"] != DBNull.Value) uk.inn = Convert.ToString(reader["sb6"]).Trim();
                    if (reader["sb7"] != DBNull.Value) uk.phone = Convert.ToString(reader["sb7"]).Trim();
                    if (reader["sb8"] != DBNull.Value) uk.adres = Convert.ToString(reader["sb8"]).Trim();
                    if (reader["sb9"] != DBNull.Value) uk.pm_note = Convert.ToString(reader["sb9"]).Trim();
                    if (reader["sb10"] != DBNull.Value) uk.poluch2 = Convert.ToString(reader["sb10"]).Trim();
                    if (reader["sb11"] != DBNull.Value) uk.bank2 = Convert.ToString(reader["sb11"]).Trim();
                    if (reader["sb12"] != DBNull.Value) uk.rschet2 = Convert.ToString(reader["sb12"]).Trim();
                    if (reader["sb13"] != DBNull.Value) uk.korr_schet2 = Convert.ToString(reader["sb13"]).Trim();
                    if (reader["sb14"] != DBNull.Value) uk.bik2 = Convert.ToString(reader["sb14"]).Trim();
                    if (reader["sb15"] != DBNull.Value) uk.inn2 = Convert.ToString(reader["sb15"]).Trim();
                    if (reader.FieldCount > 17)
                    {
                        if (reader["sb16"] != DBNull.Value) uk.phone2 = Convert.ToString(reader["sb16"]).Trim();
                        if (reader["sb17"] != DBNull.Value) uk.adres2 = Convert.ToString(reader["sb17"]).Trim();
                        if (reader["filltext"] != DBNull.Value) uk.filltext = Convert.ToInt32(reader["filltext"]);
                    }
                    else
                    {
                        uk.filltext = 1;
                    }


                    spis.Add(uk);

                }
                reader.Close();
                cmd.Dispose();

                s = " SELECT *  " +
                    " FROM " + prefData + "prm_10 " +
                    " where nzp_prm=80 and is_actual<>100 order by dat_po desc";
                cmd = DBManager.newDbCommand(s, _conDb);
                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    foreach (_Rekvizit uk in spis)
                    {
                        uk.ercName = reader["val_prm"].ToString().Trim();
                    }
                }
                reader.Close();
                ret.result = true;
                return spis;
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки 27 |" + s, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = "Ошибка выборки 27 |" + s;
                return null;
            }
            finally
            {
                cmd.Dispose();
            }



        }

        /// <summary>
        /// Подсчитываем временно выбывших в текущем месяце
        /// </summary>
        /// <param name="prefData"></param>
        /// <param name="nzpKvar"></param>
        /// <param name="datCalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string GetGilVrVib(string prefData,
            int nzpKvar, string datCalc, out Returns ret)
        {
            string gilPeriods = "";
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            string s = " SELECT dat_s, dat_po  " +
                       " FROM " + prefData + "gil_periods " +
                       " WHERE nzp_kvar=" + nzpKvar + " and is_actual=1 " +
                       "        AND dat_s <= date('" + datCalc + "')" +
                       "        AND dat_po >= date('" + datCalc + "') " +
                       " UNION " +
                       " SELECT a.dat_s, a.dat_po  " +
                       " FROM " + prefData + "gil_periods a," + prefData + "must_calc b" +
                       " WHERE MDY(b.month_,01, b.year_) = date('" + datCalc + "') " +
                       "        AND a.nzp_kvar=b.nzp_kvar " +
                       "        AND kod1 = 6 " +//Жильцы
                       "        AND b.nzp_kvar=" + nzpKvar +
                       "        AND a.dat_s <= b.dat_po" +
                       "        AND a.dat_po >= b.dat_s ";

            IDbCommand cmd = DBManager.newDbCommand(s, _conDb);

            try
            {
                IDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {


                    if ((reader["dat_s"] != DBNull.Value) & (reader["dat_po"] != DBNull.Value))
                    {
                        gilPeriods = gilPeriods + ", " + ((DateTime)reader["dat_s"]).ToShortDateString() + "-" +
                            ((DateTime)reader["dat_po"]).ToShortDateString();
                    }
                }
                reader.Close();
                ret.result = true;
                if (gilPeriods != "") gilPeriods = gilPeriods.Substring(1);
                return gilPeriods;
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки 28|" + s, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = "Ошибка выборки 28|" + s;
                return gilPeriods;
            }
            finally
            {
                cmd.Dispose();
            }



        }



        /// <summary>
        /// Получение начисления для г.Самары
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachSamara(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {
            bill.RevalOtopl307 = 0;
            IDataReader reader = null;
            String month = tableCharge.Split('_')[3];

            string sql = " SELECT s.ordering, s.service_name as service, m.measure, su.name_supp as name_supp, a.tarif," +
                         "         a.nzp_serv, m.nzp_measure, a.nzp_frm, a.nzp_supp, max(a.is_device) as is_device," +
                         "         sum(a.gsum_tarif) as rsum_tarif, sum(a.sum_charge) as sum_charge,             " +
                         "         sum(a.rsum_tarif - a.gsum_tarif + a.reval - a.sum_nedop) as reval,            " +
                         "         sum(0) as sum_sn, sum(a.real_charge) - coalesce(sum(p.sum_rcl), 0) as real_charge, max(a.c_calc) as c_calc, " +
                         "         sum(a.sum_money) as sum_money, sum(a.rsum_tarif - a.gsum_tarif) as reval_gil, " +
                         "         sum(a.sum_insaldo) as sum_insaldo,  max(a.c_reval) as c_reval,                " +
                         "         sum(a.sum_nedop) as sum_nedop,  sum(a.sum_outsaldo) as sum_outsaldo           " +
                         " FROM  " + tableCharge + " a " +
                         " LEFT JOIN (SELECT nzp_serv, nzp_supp, sum(sum_rcl) as sum_rcl " +
		                 " FROM bill01_charge_16.perekidka p " +
		                 " INNER JOIN fbill_data.document_base d on d.nzp_doc_base = p.nzp_doc_base " +
		                 " WHERE nzp_kvar=" + nzpKvar + " AND d.comment = 'Выравнивание сальдо' and p.nzp_user = 1 and month_ = " + month + 
		                 " group by 1,2) p on p.nzp_supp = a.nzp_supp and p.nzp_serv  = a.nzp_serv, " +
                         Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                         Points.Pref + DBManager.sKernelAliasRest + "s_measure m, " +
                         Points.Pref + DBManager.sKernelAliasRest + "supplier su " +
                         " WHERE a.nzp_kvar=" + nzpKvar +
                         "        AND a.nzp_serv=s.nzp_serv    " +
                         "        AND a.nzp_serv<>268    " +
                         "        AND a.dat_charge is null       " +
                         "        AND a.nzp_serv>1             " +
                         "        AND a.nzp_supp = su.nzp_supp " +
                         "        AND s.nzp_measure=m.nzp_measure" +
                         " GROUP BY 1,2,3,4,5,6,7,8,9" +
                         " ORDER BY ordering,nzp_serv, nzp_frm desc ";

            IDbCommand cmd = DBManager.newDbCommand(sql, _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());

                    serv.Serv.NameServ = reader["service"].ToString().Trim();
                    if (((serv.Serv.NzpServ > 5) & (serv.Serv.NzpServ < 11)) || (serv.Serv.NzpServ == 25) ||
                        ((serv.Serv.NzpServ > 509) & (serv.Serv.NzpServ < 518)))
                    {
                        serv.Serv.NameSupp = "-" + reader["name_supp"].ToString().Trim();
                    }

                    if ((serv.Serv.NzpServ == 14) & (Int32.Parse(reader["nzp_supp"].ToString()) == 612))
                    {
                        serv.Serv.NameServ = "Хол.вода на ГВС";
                    }

                    if ((serv.Serv.NzpServ == 14))
                    {
                        serv.Serv.NameSupp = "-" + reader["name_supp"].ToString().Trim();
                    }

                    serv.Serv.Ordering = Int32.Parse(reader["ordering"].ToString());
                    serv.Serv.NzpMeasure = Int32.Parse(reader["nzp_measure"].ToString());
                    serv.Serv.NzpFrm = Convert.ToInt32(reader["nzp_frm"]);

                    serv.Serv.Measure = reader["measure"].ToString().Trim();
                    if (serv.Serv.NzpServ == 9)
                        if (Convert.ToInt32(reader["nzp_frm"]) == 0)
                        {
                            serv.Serv.NzpMeasure = 4;
                        }
                    if (serv.Serv.NzpServ == 8)
                        if (Convert.ToInt32(reader["nzp_frm"]) == 0)
                        {
                            serv.Serv.NzpMeasure = 4;
                        }
                    if (serv.Serv.NzpMeasure == 4) serv.Serv.Measure = "Гкал.";
                    GetMeasureByFrm(Convert.ToInt32(reader["nzp_frm"]), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);
                    serv.Serv.OldMeasure = serv.Serv.NzpMeasure;
                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.IsDevice = Int32.Parse(reader["is_device"].ToString());
                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RevalGil = Decimal.Parse(reader["reval_gil"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumSn = Decimal.Parse(reader["sum_sn"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012

                    //if (convertedTarifs != null)
                    //{
                    //    convertedTarifs.ReplaceServiceFrm(ref serv, Convert.ToInt32(reader["nzp_frm"]));
                    //}

                    serv.CopyToOdn();
                    bill.AddServ(serv);
                }

            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка начислений " + e.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

            //Учитываем корректировки по отоплению отдельно
            if (bill.Month == 5)
            {
                Returns ret;
                sql = " SELECT  sum(sum_rcl)  " +
                      " FROM  " +
                      tableCharge.Replace(DBManager.tableDelimiter + "charge_" + bill.Month.ToString("00"),
                          DBManager.tableDelimiter + "perekidka") + " a " +
                      " WHERE a.nzp_kvar=" + nzpKvar +
                      "        AND a.nzp_serv=8    " +
                      "        AND a.type_rcl = 110    " +
                      "        AND month_=  " + bill.Month;
                object obj = DBManager.ExecScalar(_conDb, sql, out ret, true);
                if (obj != null)
                {
                    decimal.TryParse(obj.ToString(), out bill.RevalOtopl307);
                }

            }
            return true;

        }

        /// <summary>
        /// Получение начислений для Самары по поставщикам
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachSamaraSupp(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {

            IDataReader reader = null;
            var sql = new StringBuilder();
            sql.Append(" SELECT s.ordering, s.service_name as service, m.measure, su.name_supp as name_supp, a.tarif,");
            sql.Append("         a.nzp_serv, m.nzp_measure, a.nzp_frm, a.nzp_supp, max(a.is_device) as is_device,");
            sql.Append("         sum(a.gsum_tarif) as rsum_tarif, sum(a.sum_charge) as sum_charge,             ");
            sql.Append("         sum(a.rsum_tarif - a.gsum_tarif + a.reval - a.sum_nedop) as reval,            ");
            sql.Append("         sum(0) as sum_sn, sum(a.real_charge) as real_charge, max(a.c_calc) as c_calc, ");
            sql.Append("         sum(a.sum_money) as sum_money, sum(a.rsum_tarif - a.gsum_tarif) as reval_gil, ");
            sql.Append("         sum(a.sum_insaldo) as sum_insaldo,  max(a.c_reval) as c_reval,                ");
            sql.Append("         sum(a.sum_nedop) as sum_nedop,  sum(a.sum_outsaldo) as sum_outsaldo           ");
            sql.Append(" FROM  " + tableCharge + " a, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "s_measure m, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su ");
            sql.Append(" WHERE a.nzp_kvar=" + nzpKvar);
            sql.Append("        AND a.nzp_serv=s.nzp_serv    ");
            sql.Append("        AND a.dat_charge is null       ");
            sql.Append("        AND a.nzp_serv>1             ");
            sql.Append("        AND a.nzp_supp = su.nzp_supp ");
            sql.Append("        AND s.nzp_measure=m.nzp_measure");
            sql.Append(" GROUP BY 1,2,3,4,5,6,7,8,9");
            sql.Append(" ORDER BY ordering,nzp_serv, nzp_frm desc ");

            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());

                    serv.Serv.NameServ = reader["service"].ToString();
                    if (((serv.Serv.NzpServ > 5) & (serv.Serv.NzpServ < 11)) || (serv.Serv.NzpServ == 25) ||
                        ((serv.Serv.NzpServ > 509) & (serv.Serv.NzpServ < 518)))
                    {
                        serv.Serv.NameSupp = "-" + reader["name_supp"];
                    }

                    if ((serv.Serv.NzpServ == 14) & (Int32.Parse(reader["nzp_supp"].ToString()) == 612))
                    {
                        serv.Serv.NameServ = "Хол.вода на ГВС";
                    }

                    if ((serv.Serv.NzpServ == 14))
                    {
                        serv.Serv.NameSupp = "-" + reader["name_supp"];
                    }

                    serv.Serv.Ordering = Int32.Parse(reader["ordering"].ToString());
                    serv.Serv.NzpMeasure = Int32.Parse(reader["nzp_measure"].ToString());
                    serv.Serv.NzpFrm = Convert.ToInt32(reader["nzp_frm"]);

                    serv.Serv.Measure = reader["measure"].ToString();
                    if (serv.Serv.NzpServ == 9)
                        if (Convert.ToInt32(reader["nzp_frm"]) == 0)
                        {
                            serv.Serv.NzpMeasure = 4;
                        }
                    if (serv.Serv.NzpServ == 8)
                        if (Convert.ToInt32(reader["nzp_frm"]) == 0)
                        {
                            serv.Serv.NzpMeasure = 4;
                        }
                    GetMeasureByFrm(Convert.ToInt32(reader["nzp_frm"]), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);
                    serv.Serv.OldMeasure = serv.Serv.NzpMeasure;
                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.NzpSupp = Int32.Parse(reader["nzp_supp"].ToString());
                    serv.Serv.IsDevice = Int32.Parse(reader["is_device"].ToString());
                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RevalGil = Decimal.Parse(reader["reval_gil"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumSn = Decimal.Parse(reader["sum_sn"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012

                    //if (convertedTarifs != null)
                    //{
                    //    convertedTarifs.ReplaceServiceFrm(ref serv, Convert.ToInt32(reader["nzp_frm"]));
                    //}

                    serv.CopyToOdn();
                    bill.AddServ(serv);
                    bill.AddSupp(serv);
                }
                return true;
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка начислений " + e.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

        }

        /// <summary>
        /// Получение начислений для г.Губкин
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachGubkin(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {
            IDataReader reader = null;
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT s.ordering, s.service_name as service, m.measure,");
            sql.Append("         a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm, sum(0) as sum_sn, ");
            sql.Append("         sum(a.gsum_tarif) as rsum_tarif, sum(a.sum_nedop) as sum_nedop, ");
            sql.Append("         sum(a.rsum_tarif - a.gsum_tarif + a.reval - a.sum_nedop) as reval,  ");
            sql.Append("         sum(a.real_charge) as real_charge, sum(a.sum_outsaldo) as sum_outsaldo, ");
            sql.Append("         sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money, ");
            sql.Append("         sum(a.rsum_tarif - a.gsum_tarif) as reval_gil, ");
            sql.Append("         sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc, max(a.c_reval) as c_reval ");
            sql.Append(" FROM  " + tableCharge + " a, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "s_measure m, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su ");
            sql.Append(" WHERE a.nzp_kvar=" + nzpKvar);
            sql.Append("        AND a.nzp_serv=s.nzp_serv ");
            sql.Append("        AND a.dat_charge is null ");
            sql.Append("        AND a.nzp_serv>1 ");
            sql.Append("        AND a.nzp_supp = su.nzp_supp ");
            sql.Append("        AND s.nzp_measure=m.nzp_measure");
            sql.Append(" GROUP BY 1,2,3,4,5,6,7");
            sql.Append(" ORDER BY ordering,nzp_serv, service ");
            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());
                    serv.Serv.NameServ = reader["service"].ToString();
                    serv.Serv.Ordering = Int32.Parse(reader["ordering"].ToString());
                    serv.Serv.NzpMeasure = Int32.Parse(reader["nzp_measure"].ToString());
                    serv.Serv.Measure = reader["measure"].ToString();
                    GetMeasureByFrm((reader["nzp_frm"] != DBNull.Value ? Convert.ToInt32(reader["nzp_frm"]) : 0), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RevalGil = Decimal.Parse(reader["reval_gil"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumSn = Decimal.Parse(reader["sum_sn"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012
                    serv.CopyToOdn();
                    bill.AddServ(serv);
                }
                return true;
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка начислений " + e.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }

            }

        }


        /// <summary>
        /// Получение начислений для Сахи
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachSaha(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {
            IDataReader reader = null;
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT s.ordering, s.service_name as service, m.measure, name_supp,");
            sql.Append("         a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm, ");
            sql.Append("         sum(a.rsum_tarif) as rsum_tarif, sum(a.sum_tarif) as sum_tarif, ");
            sql.Append("         sum(a.sum_nedop) as sum_nedop, max(a.c_reval) as c_reval ,");
            sql.Append("         sum(a.reval) as reval, sum(a.sum_lgota) as sum_lgota, ");
            sql.Append("         sum(a.real_charge) as real_charge, sum(a.sum_outsaldo) as sum_outsaldo, ");
            sql.Append("         sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money, sum(0) as reval_gil,");
            sql.Append("         sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc");
            sql.Append(" FROM  " + tableCharge + " a, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s,  ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "s_measure m, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su  ");
            sql.Append(" WHERE a.nzp_kvar=" + nzpKvar);
            sql.Append("        AND a.nzp_serv=s.nzp_serv ");
            sql.Append("        AND a.dat_charge is null  ");
            sql.Append("        AND a.nzp_serv>1 ");
            sql.Append("        AND s.nzp_measure=m.nzp_measure ");
            sql.Append("        AND a.nzp_supp=su.nzp_supp ");
            sql.Append(" GROUP BY 1,2,3,4,5,6, 7,8");
            sql.Append(" ORDER BY ordering, nzp_serv,service ");
            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());
                    serv.Serv.NameServ = reader["service"].ToString();
                    serv.Serv.NameSupp = reader["name_supp"].ToString();
                    serv.Serv.NzpMeasure = Int32.Parse(reader["nzp_measure"].ToString());
                    serv.Serv.Measure = reader["measure"].ToString();
                    GetMeasureByFrm(Convert.ToInt32(reader["nzp_frm"]), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumTarif = Decimal.Parse(reader["sum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumLgota = Decimal.Parse(reader["sum_lgota"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012
                    serv.CopyToOdn();
                    bill.AddServ(serv);
                }
                return true;
            }
            catch
            {


                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }

            }

        }


        /// <summary>
        /// Получение стандартных начислений
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachStd(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {
            IDataReader reader = null;
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT s.ordering, s.service_name as service, m.measure, ");
            sql.Append("        a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm, sum(a.sum_outsaldo) as sum_outsaldo, ");
            sql.Append("        sum(a.rsum_tarif) as rsum_tarif, sum(a.sum_tarif) as sum_tarif, sum(a.sum_nedop) as sum_nedop,");
            sql.Append("        sum(a.reval) as reval, sum(a.sum_lgota) as sum_lgota, sum(a.sum_pere) as sum_pere, ");
            sql.Append("        sum(a.sum_dlt_tarif_p) as sum_sn, sum(a.real_charge) as real_charge, ");
            sql.Append("        sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money,sum(0) as reval_gil, ");
            sql.Append("        sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc, max(a.c_reval) as c_reval, ");
            sql.Append("        sum(a.c_okaz) as c_okaz, sum(a.sum_fakt) as sum_fakt ");
            sql.Append(" FROM  " + tableCharge + " a, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s,  ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "s_measure m, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su  ");
            sql.Append(" WHERE a.nzp_kvar=" + nzpKvar);
            sql.Append("        AND a.nzp_serv=s.nzp_serv ");
            sql.Append("        AND a.dat_charge is null ");
            sql.Append("        AND a.nzp_serv > 1 ");
            sql.Append("        AND s.nzp_measure=m.nzp_measure ");
            sql.Append("        AND a.nzp_supp=su.nzp_supp ");
            sql.Append(" GROUP BY 1,2,3,4,5,6,7");
            sql.Append(" ORDER BY ordering, nzp_serv, service ");
            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());
                    serv.Serv.NameServ = reader["service"].ToString();
                    serv.Serv.NzpMeasure = Int32.Parse(reader["nzp_measure"].ToString());
                    serv.Serv.Measure = reader["measure"].ToString();
                    GetMeasureByFrm(Convert.ToInt32(reader["nzp_frm"]), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumTarif = Decimal.Parse(reader["sum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumPere = Decimal.Parse(reader["sum_pere"].ToString());
                    serv.Serv.SumSn = Decimal.Parse(reader["sum_sn"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumLgota = Decimal.Parse(reader["sum_lgota"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012
                    serv.Serv.COkaz = reader["c_okaz"] != DBNull.Value ? Convert.ToInt32(reader["c_okaz"]) : 0;
                    serv.CopyToOdn();
                    bill.AddServ(serv);
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }

            }

        }


        /// <summary>
        /// Получение начислений Астрахани
        /// </summary>
        /// <param name="tableCharge"></param>
        /// <param name="nzpKvar"></param>
        /// <param name="bill"></param>
        /// <returns></returns>
        private bool GetNachAstrahan(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {

            IDataReader reader = null;
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT s.ordering, s.service_name as service, m.measure, su.name_supp, ");
            sql.Append("         a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm, b.bank_name, b.rcount,");
            sql.Append("         sum(a.rsum_tarif) as rsum_tarif, sum(a.sum_tarif) as sum_tarif, ");
            sql.Append("         sum(a.reval) as reval, sum(a.sum_lgota) as sum_lgota, sum(a.sum_nedop) as sum_nedop,");
            sql.Append("         sum(a.real_charge) as real_charge, sum(a.sum_outsaldo) as sum_outsaldo, ");
            sql.Append("         sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money,sum(0) as reval_gil, ");
            sql.Append("         sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc, max(a.c_reval) as c_reval, ");
            sql.Append("         sum(a.c_okaz) as c_okaz ");
            sql.Append(" FROM  " + tableCharge + " a, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "s_measure m, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "s_payer p ");
            sql.Append("        left join " + Points.Pref + DBManager.sDataAliasRest + "fn_bank b  ");
            sql.Append("        ON b.nzp_payer = p.nzp_payer ");
            sql.Append(" WHERE a.nzp_kvar=" + nzpKvar);
            sql.Append("        AND a.nzp_serv=s.nzp_serv ");
            sql.Append("        AND a.dat_charge is null ");
            sql.Append("        AND a.nzp_serv > 1 ");
            sql.Append("        AND s.nzp_measure=m.nzp_measure ");
            sql.Append("        AND a.nzp_supp=su.nzp_supp ");
            sql.Append("        AND su.nzp_supp = p.nzp_supp ");
            sql.Append(" GROUP BY 1,2,3,4,5,6,7,8,9,10");
            sql.Append(" ORDER BY ordering, nzp_serv, service ");
            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());
                    serv.Serv.NameServ = reader["service"].ToString();
                    serv.Serv.NzpMeasure = Int32.Parse(reader["nzp_measure"].ToString());
                    serv.Serv.Measure = reader["measure"].ToString();
                    GetMeasureByFrm(Convert.ToInt32(reader["nzp_frm"]), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumTarif = Decimal.Parse(reader["sum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.NameSupp = reader["name_supp"].ToString();
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumLgota = Decimal.Parse(reader["sum_lgota"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012
                    serv.Serv.COkaz = reader["c_okaz"] != DBNull.Value ? Convert.ToInt32(reader["c_okaz"]) : 0;
                    serv.Serv.SuppRekv = (reader["bank_name"] != DBNull.Value ? reader["bank_name"].ToString().Trim() : "") + " " +
                                         (reader["rcount"] != DBNull.Value ? reader["rcount"].ToString().Trim() : "");
                    serv.CopyToOdn();
                    bill.AddSupp(serv);
                    bill.AddServ(serv);
                }
                return true;
            }
            catch
            {


                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

        }


        /// <summary>
        /// Получение начислений по капремонту
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachKapremont(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {
            IDataReader reader = null;
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT s.ordering, s.service_name as service, m.measure, ");
            sql.Append("         a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm, ");
            sql.Append("         sum(a.rsum_tarif) as rsum_tarif, sum(a.sum_tarif) as sum_tarif,");
            sql.Append("         sum(a.reval) as reval, sum(a.sum_lgota) as sum_lgota, sum(a.sum_nedop) as sum_nedop,");
            sql.Append("         sum(a.real_charge) as real_charge, sum(a.sum_outsaldo) as sum_outsaldo, ");
            sql.Append("         sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money, sum(0) as reval_gil, ");
            sql.Append("         sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc, max(a.c_reval) as c_reval ");
            sql.Append(" FROM  " + tableCharge + " a, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s,  ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "s_measure m, ");
            sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su  ");
            sql.Append(" WHERE a.nzp_kvar=" + nzpKvar);
            sql.Append("        AND a.nzp_serv=s.nzp_serv AND a.nzp_serv in (206,267) ");
            sql.Append("        AND a.dat_charge is null ");
            sql.Append("        AND a.nzp_serv>1 ");
            sql.Append("        AND s.nzp_measure=m.nzp_measure ");
            sql.Append("        AND a.nzp_supp=su.nzp_supp ");
            sql.Append(" GROUP BY 1,2,3,4,5,6,7");
            sql.Append(" ORDER BY ordering, nzp_serv, service ");
            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());
                    serv.Serv.NameServ = reader["service"].ToString();
                    serv.Serv.NzpMeasure = Int32.Parse(reader["nzp_measure"].ToString());
                    serv.Serv.Measure = reader["measure"].ToString();
                    GetMeasureByFrm(Convert.ToInt32(reader["nzp_frm"]), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumTarif = Decimal.Parse(reader["sum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumLgota = Decimal.Parse(reader["sum_lgota"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012
                    serv.CopyToOdn();
                    bill.AddServ(serv);
                }

                return true;
            }
            catch
            {

                return false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }

            }

        }


        ///// <summary>
        ///// Получение начислений по Туле
        ///// </summary>
        ///// <param name="tableCharge">Таблица начислений</param>
        ///// <param name="nzpKvar">Код лицевого счета</param>
        ///// <param name="bill">Счет</param>
        ///// <returns></returns>
        //private bool GetNachTula(string tableCharge, int nzpKvar, ref BaseFactura bill)
        //{
        //    IDataReader reader = null;

        //    string sql = " SELECT s.ordering, s.service_name as service, m.measure, su.payer as name_supp,su.inn," +
        //                 "        a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm,a.nzp_supp, max(is_device) as is_device, " +
        //                 "        sum(a.gsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop," +
        //                 "        sum(a.rsum_tarif - a.gsum_tarif + a.reval - a.sum_nedop) as reval, sum(0) as sum_sn, " +
        //                 "        sum(a.real_charge) as real_charge, sum(a.sum_outsaldo) as sum_outsaldo, " +
        //                 "        sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money, sum(a.rsum_tarif-a.gsum_tarif) as reval_gil, " +
        //                 "        sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc, max(a.c_reval) as c_reval " +
        //        //                 "        , max(coalesce(b.bank_name,'')) bank_name, max(coalesce(b.rcount,'')) rcount " +         
        //                 " FROM  " +
        //                 Points.Pref + DBManager.sKernelAliasRest + "services s, " +
        //                 Points.Pref + DBManager.sKernelAliasRest + "s_measure m, " +
        //                 "        " + tableCharge + " a, " +
        //                 "        " + Points.Pref + DBManager.sKernelAliasRest + "supplier sup " +
        //                 "        left join  " + Points.Pref + DBManager.sKernelAliasRest + "s_payer su " +
        //                 "        ON sup.nzp_payer_supp = su.nzp_payer " +
        //        //                 "        left join " + Points.Pref + DBManager.sDataAliasRest + "fn_bank b  " +
        //        //               "        ON b.nzp_payer = su.nzp_payer " +
        //                 " WHERE a.nzp_kvar=" + nzpKvar +
        //                 "        AND a.nzp_supp=sup.nzp_supp " +
        //                 "        AND a.nzp_serv=s.nzp_serv " +
        //                 "        AND a.dat_charge is null " +
        //                 "        AND a.nzp_serv>1  " +
        //                 "        AND s.nzp_measure=m.nzp_measure" +
        //                 " GROUP BY 1,2,3,4,5,6,7,8,9,10" +
        //                 " ORDER BY ordering,nzp_serv ";
        //    IDbCommand cmd = DBManager.newDbCommand(sql, _conDb);

        //    try
        //    {
        //        reader = cmd.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            BaseServ serv;
        //            if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
        //            {
        //                serv = new BaseServ(true);
        //            }
        //            else
        //            {
        //                serv = new BaseServ(false);
        //            }
        //            serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());

        //            serv.Serv.NameServ = reader["service"] != DBNull.Value ? reader["service"].ToString().Trim() : "";

        //            serv.Serv.NameSupp = reader["name_supp"] == DBNull.Value ? "Контрагент не определен" : reader["name_supp"].ToString().Trim();

        //            serv.Serv.SuppRekv = reader["inn"] != DBNull.Value ? reader["inn"].ToString() : "";
        //            //                    serv.Serv.SuppRekv = (((reader["inn"] != DBNull.Value) && (reader["inn"].ToString().Trim() != "")) ? "ИНН " + reader["inn"].ToString().Trim() + " " : "") +
        //            //                                        (reader["bank_name"].ToString().Trim() != "" ? ", банк " + reader["bank_name"].ToString().Trim() : "") +
        //            //                                         (reader["rcount"].ToString().Trim() != "" ? ", р/с " + reader["rcount"].ToString().Trim() : "");

        //            serv.Serv.Ordering = reader["ordering"] != DBNull.Value ? Int32.Parse(reader["ordering"].ToString()) : 0;
        //            serv.Serv.NzpMeasure = reader["nzp_measure"] != DBNull.Value ? Int32.Parse(reader["nzp_measure"].ToString()) : 0;
        //            serv.Serv.NzpSupp = reader["nzp_supp"] != DBNull.Value ? Int32.Parse(reader["nzp_supp"].ToString()) : 0;

        //            serv.Serv.Measure = reader["measure"] != DBNull.Value ? reader["measure"].ToString() : "";

        //            GetMeasureByFrm((reader["nzp_frm"] != DBNull.Value ? Convert.ToInt32(reader["nzp_frm"]) : 0), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

        //            serv.Serv.OldMeasure = serv.Serv.NzpMeasure;
        //            serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

        //            serv.Serv.IsDevice = Int32.Parse(reader["is_device"].ToString());
        //            serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
        //            serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
        //            serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
        //            serv.Serv.RevalGil = Decimal.Parse(reader["reval_gil"].ToString());
        //            serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
        //            serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
        //            serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
        //            serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
        //            serv.Serv.SumSn = Decimal.Parse(reader["sum_sn"].ToString());
        //            serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
        //            serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
        //            serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
        //            if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0;
        //            serv.CopyToOdn();
        //            bill.AddSupp(serv);
        //            bill.AddServ(serv);
        //        }
        //        cmd.Dispose();
        //        reader.Close();
        //        reader.Dispose();
        //        //return true;
        //    }
        //    catch (Exception e)
        //    {
        //        MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка начислений " + e.Message, MonitorLog.typelog.Error, true);
        //        if (cmd != null)
        //        {
        //            cmd.Dispose();
        //        }
        //        if (reader != null)
        //        {
        //            reader.Close();
        //            reader.Dispose();
        //        }
        //        return false;
        //    }

        //    //сообщение для л/счета
        //    if (DBManager.TempTableInWebCashe(_conDb, Points.Pref + DBManager.sDataAliasRest + "kvar_factura_msg"))
        //    {
        //        sql = " SELECT a.msg FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar_factura_msg a " +
        //                  " WHERE a.nzp_kvar = " + nzpKvar + " and a.period = '" +
        //                  Convert.ToDateTime("01." + _finder.month_ + "." + _finder.year_).ToShortDateString() + "'";
        //        cmd = DBManager.newDbCommand(sql, _conDb);
        //        string msg = "";
        //        try
        //        {
        //            reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                if (msg.Equals(""))
        //                    msg = reader["msg"] != DBNull.Value ? reader["msg"].ToString() : "";
        //                else
        //                    msg = msg + (reader["msg"] != DBNull.Value ? "\r\n" + reader["msg"].ToString() : "");
        //            }
        //            ((TulaFaktura) bill).MessageForLs = msg;
        //            return true;
        //        }
        //        catch (Exception e)
        //        {
        //            MonitorLog.WriteLog(
        //                "Счет-квитанция :  Ошибка при выборке сообщения для лс  " + nzpKvar + "\r\n" + e.Message,
        //                MonitorLog.typelog.Error, true);

        //            return false;
        //        }
        //        finally
        //        {
        //            if (cmd != null)
        //            {
        //                cmd.Dispose();
        //            }
        //            if (reader != null)
        //            {
        //                reader.Close();
        //                reader.Dispose();
        //            }
        //        }
        //    }
        //    return true;

        //}


        /// <summary>
        /// Получение начислений по Туле
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachTula(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {
            IDataReader reader_dog = null;
            string baseData = tableCharge.Substring(0, tableCharge.IndexOf('_')) + "_data";
            string datCalc = "01." + _finder.month_ + "." + _finder.year_;
            string spec_shet = "";
            string kotlovoj_shet = "";
            string sql;
            if ((int)STCLINE.KP50.Interfaces.Faktura.FakturaTypes.TulaFakturaMod == _finder.idFaktura)
            {
                #region Выборка договора по л/счету
                sql = " SELECT kotlovoj_schet, spec_schet, contract_content, contract_footer " +
                             "       FROM public.fn_Contract_Tula('" + nzpKvar + "')  ";
                IDbCommand cmd_dog = DBManager.newDbCommand(sql, _conDb);
                try
                {
                    reader_dog = cmd_dog.ExecuteReader();
                    if (reader_dog.Read())
                    {
                        ((TulaFaktura)bill).ContractContentLs = reader_dog["contract_content"] != DBNull.Value ? reader_dog["contract_content"].ToString() : "";
                        ((TulaFaktura)bill).ContractFooterLs = reader_dog["contract_footer"] != DBNull.Value ? reader_dog["contract_footer"].ToString() : "";
                        spec_shet = reader_dog["spec_schet"] != DBNull.Value ? reader_dog["spec_schet"].ToString().Trim() : "";
                        kotlovoj_shet = reader_dog["kotlovoj_schet"] != DBNull.Value ? reader_dog["kotlovoj_schet"].ToString().Trim() : "";
                    }
                }
                catch (Exception e)
                {
                    MonitorLog.WriteLog(
                        "Счет-квитанция :  Ошибка чтения текста договора для лс  " + nzpKvar.ToString() + "\r\n" + e.Message,
                        MonitorLog.typelog.Error, true);
                    if (cmd_dog != null)
                    {
                        cmd_dog.Dispose();
                    }
                    if (reader_dog != null)
                    {
                        reader_dog.Close();
                        reader_dog.Dispose();
                    }
                    return false;
                }
                finally
                {
                    if (cmd_dog != null)
                    {
                        cmd_dog.Dispose();
                    }
                    if (reader_dog != null)
                    {
                        reader_dog.Close();
                        reader_dog.Dispose();
                    }
                }
                #endregion
            }

            IDataReader reader = null;
            sql = " SELECT s.ordering, s.service_name as service, m.measure, su.payer as name_supp,su.inn, b.bank_name, b.rcount," +
                         "        a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm,a.nzp_supp, max(is_device) as is_device, " +
                         "        sum(a.gsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop," +
                         "        sum(a.rsum_tarif - a.gsum_tarif + a.reval - a.sum_nedop) as reval, sum(0) as sum_sn, " +
                         "        sum(a.real_charge) as real_charge, sum(a.sum_outsaldo) as sum_outsaldo, " +
                         "        sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money, sum(a.rsum_tarif-a.gsum_tarif) as reval_gil, " +
                         "        sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc, max(a.c_reval) as c_reval " +
                         " FROM  " +
                         Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                         Points.Pref + DBManager.sKernelAliasRest + "s_measure m, " +
                         "        " + tableCharge + " a, " +
                         "        " + Points.Pref + DBManager.sKernelAliasRest + "supplier sup, " +
                                  Points.Pref + DBManager.sKernelAliasRest + "s_payer su " +
                         "       left join " + Points.Pref + DBManager.sDataAliasRest + "fn_bank b  " +
                         "       ON b.nzp_payer = su.nzp_payer " +
                         " WHERE a.nzp_kvar=" + nzpKvar +
                         "        AND a.nzp_supp=sup.nzp_supp " +
                         "        AND a.nzp_serv=s.nzp_serv " +
                         "        AND a.dat_charge is null " +
                         "        AND a.nzp_serv>1  " +
                         "        AND s.nzp_measure=m.nzp_measure" +
                         "        AND sup.nzp_payer_supp = su.nzp_payer " +
                         " GROUP BY 1,2,3,4,5,6,7,8,9,10,11,12" +
                         " ORDER BY ordering,nzp_serv ";
            IDbCommand cmd = DBManager.newDbCommand(sql, _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());

                    serv.Serv.NameServ = reader["service"] != DBNull.Value ? reader["service"].ToString().Trim() : "";

                    serv.Serv.NameSupp = reader["name_supp"] == DBNull.Value ? "Контрагент не определен" : reader["name_supp"].ToString().Trim();

                    serv.Serv.SuppRekv = reader["inn"] != DBNull.Value ? reader["inn"].ToString() : "";

                    if ((int)STCLINE.KP50.Interfaces.Faktura.FakturaTypes.TulaFakturaMod == _finder.idFaktura)
                    {
                        #region если есть спецсчет - публикуем в квитанции
                        serv.Serv.SuppRekv = (reader["inn"] != DBNull.Value ? "ИНН " + reader["inn"].ToString() : "");

                        if (serv.Serv.NzpServ == 206 && reader["inn"].ToString().Trim().Equals("7103520526"))
                        {
                            if (!spec_shet.Equals(""))
                            {
                                serv.Serv.SuppRekv = serv.Serv.SuppRekv + "\r\nспец.счет " + spec_shet;
                            }
                            else if (!kotlovoj_shet.Equals(""))
                            {
                                serv.Serv.SuppRekv = serv.Serv.SuppRekv + "\r\nсчет " + kotlovoj_shet;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        if (serv.Serv.NzpServ == 206 && reader["inn"].ToString().Trim().Equals("7103520526"))
                        {
                            string sql_schet = "";
                            kotlovoj_shet = "40603810401250002628";
                            sql_schet = " SELECT p.val_prm " +
                                        "      FROM " + baseData + ".kvar a " +
                                        "        LEFT JOIN " + baseData + ".prm_2 p " +
                                        "          ON p.nzp_prm = 2486 AND a.nzp_dom = p.nzp AND p.is_actual = 1 AND p.dat_s <= current_date AND p.dat_po >= current_date " +
                                        "      WHERE a.nzp_kvar = " + nzpKvar + ";";
                            IDbCommand cmd_schet = DBManager.newDbCommand(sql_schet, _conDb);
                            try
                            {
                                spec_shet = cmd_schet.ExecuteScalar().ToString();
                                if (!spec_shet.Equals(""))
                                {
                                    serv.Serv.SuppRekv = serv.Serv.SuppRekv + "\r\nспец.счет " + spec_shet;
                                }
                                else
                                {
                                    serv.Serv.SuppRekv = serv.Serv.SuppRekv + "\r\nсчет " + kotlovoj_shet;
                                }
                            }
                            catch (Exception e)
                            {
                                MonitorLog.WriteLog(
                                    "Счет-квитанция : Ошибка при чтении данных по спецсчету из prm_2 " + e.Message,
                                    MonitorLog.typelog.Error, true);
                                return false;
                            }
                            finally
                            {
                                if (cmd_schet != null)
                                {
                                    cmd_schet.Dispose();
                                }
                            }
                        }
                        else
                        {
                            serv.Serv.SuppRekv = (reader["inn"] != DBNull.Value ? reader["inn"].ToString() : "");
                        }
                    }

                    serv.Serv.Ordering = reader["ordering"] != DBNull.Value ? Int32.Parse(reader["ordering"].ToString()) : 0;
                    serv.Serv.NzpMeasure = reader["nzp_measure"] != DBNull.Value ? Int32.Parse(reader["nzp_measure"].ToString()) : 0;
                    serv.Serv.NzpSupp = reader["nzp_supp"] != DBNull.Value ? Int32.Parse(reader["nzp_supp"].ToString()) : 0;

                    serv.Serv.Measure = reader["measure"] != DBNull.Value ? reader["measure"].ToString() : "";

                    GetMeasureByFrm((reader["nzp_frm"] != DBNull.Value ? Convert.ToInt32(reader["nzp_frm"]) : 0), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

                    serv.Serv.OldMeasure = serv.Serv.NzpMeasure;
                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.IsDevice = Int32.Parse(reader["is_device"].ToString());
                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RevalGil = Decimal.Parse(reader["reval_gil"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumSn = Decimal.Parse(reader["sum_sn"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0;
                    serv.CopyToOdn();
                    bill.AddSupp(serv);
                    bill.AddServ(serv);
                }
                cmd.Dispose();
                reader.Close();
                reader.Dispose();
                //return true;
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка начислений " + e.Message, MonitorLog.typelog.Error, true);
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                return false;
            }

            //сообщение для л/счета
            sql = " SELECT a.msg FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar_factura_msg a " +
                    " WHERE a.nzp_kvar = " + nzpKvar + " and a.period = '" + Convert.ToDateTime("01." + _finder.month_ + "." + _finder.year_).ToShortDateString() + "'";
            cmd = DBManager.newDbCommand(sql, _conDb);
            string msg = "";
            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (msg.Equals(""))
                        msg = reader["msg"] != DBNull.Value ? reader["msg"].ToString() : "";
                    else
                        msg = msg + (reader["msg"] != DBNull.Value ? "\r\n" + reader["msg"].ToString() : "");
                }
                ((TulaFaktura)bill).MessageForLs = msg;
                return true;
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog(
                    "Счет-квитанция :  Ошибка при выборке сообщения для лс  " + nzpKvar + "\r\n" + e.Message,
                    MonitorLog.typelog.Error, true);

                return false;
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }


        }



        /// <summary>
        /// Получение начислений по Калуге
        /// </summary>
        /// <param name="tableCharge">Таблица начислений</param>
        /// <param name="nzpKvar">Код лицевого счета</param>
        /// <param name="bill">Счет</param>
        /// <returns></returns>
        private bool GetNachKaluga(string tableCharge, int nzpKvar, ref BaseFactura bill)
        {
            IDataReader reader = null;

            string sql = " SELECT s.ordering, s.service_name as service, m.measure, su.payer as name_supp,su.inn," +
                         "        a.tarif,a.nzp_serv, m.nzp_measure, a.nzp_frm,a.nzp_supp, max(is_device) as is_device, " +
                         "        sum(a.gsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop," +
                         "        sum(a.rsum_tarif - a.gsum_tarif + a.reval - a.sum_nedop) as reval, sum(0) as sum_sn, " +
                         "        sum(a.real_charge) as real_charge, sum(a.sum_outsaldo) as sum_outsaldo, " +
                         "        sum(a.sum_charge) as sum_charge, sum(a.sum_money) as sum_money, sum(a.rsum_tarif-a.gsum_tarif) as reval_gil, " +
                         "        sum(a.sum_insaldo) as sum_insaldo, max(a.c_calc) as c_calc, max(a.c_reval) as c_reval, sum(sum_rcl) as sum_rcl " +
                         " FROM  " +
                         Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                         Points.Pref + DBManager.sKernelAliasRest + "s_measure m, " +
                         "        " + tableCharge + " a left join " +
                         "        " + tableCharge.Substring(0, tableCharge.IndexOf(DBManager.tableDelimiter)) + DBManager.tableDelimiter +
                         "perekidka p on (p.nzp_kvar=a.nzp_kvar AND p.nzp_supp=a.nzp_supp AND p.nzp_serv=a.nzp_serv AND p.type_rcl = 21 " +
                //" AND date_rcl = '" + new DateTime(bill.Year, bill.Month, 1).ToShortDateString() + "'), " +
                         " AND month_ = " + bill.Month + "), " +
                         "        " + Points.Pref + DBManager.sKernelAliasRest + "supplier sup " +
                         "        left join  " + Points.Pref + DBManager.sKernelAliasRest + "s_payer su " +
                         "        ON sup.nzp_payer_supp = su.nzp_payer " +
                         " WHERE a.nzp_kvar=" + nzpKvar +
                         "        AND a.nzp_supp=sup.nzp_supp " +
                         "        AND a.nzp_serv=s.nzp_serv " +
                         "        AND a.dat_charge is null " +
                         "        AND a.nzp_serv>1  " +
                         "        AND s.nzp_measure=m.nzp_measure" +
                         " GROUP BY 1,2,3,4,5,6,7,8,9,10" +
                         " ORDER BY ordering,nzp_serv ";
            IDbCommand cmd = DBManager.newDbCommand(sql, _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader["nzp_serv"].ToString()) > 509) & (Int32.Parse(reader["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader["nzp_serv"].ToString());

                    serv.Serv.NameServ = reader["service"] != DBNull.Value ? reader["service"].ToString().Trim() : "";

                    serv.Serv.NameSupp = reader["name_supp"] == DBNull.Value ? "Контрагент не определен" : reader["name_supp"].ToString().Trim();



                    serv.Serv.SuppRekv = reader["inn"] != DBNull.Value ? reader["inn"].ToString() : "";

                    serv.Serv.Ordering = reader["ordering"] != DBNull.Value ? Int32.Parse(reader["ordering"].ToString()) : 0;
                    serv.Serv.NzpMeasure = reader["nzp_measure"] != DBNull.Value ? Int32.Parse(reader["nzp_measure"].ToString()) : 0;
                    serv.Serv.NzpSupp = reader["nzp_supp"] != DBNull.Value ? Int32.Parse(reader["nzp_supp"].ToString()) : 0;

                    serv.Serv.Measure = reader["measure"] != DBNull.Value ? reader["measure"].ToString() : "";

                    GetMeasureByFrm((reader["nzp_frm"] != DBNull.Value ? Convert.ToInt32(reader["nzp_frm"]) : 0), ref serv.Serv.Measure, ref serv.Serv.NzpMeasure);

                    serv.Serv.OldMeasure = serv.Serv.NzpMeasure;
                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; // Добавил Андрей Кайнов 19.12.2012
                    serv.Serv.Compensation = reader["sum_rcl"] != DBNull.Value ? Convert.ToDecimal(reader["sum_rcl"]) : 0;

                    serv.Serv.IsDevice = Int32.Parse(reader["is_device"].ToString());
                    serv.Serv.RsumTarif = Decimal.Parse(reader["rsum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader["reval"].ToString());
                    serv.Serv.RevalGil = Decimal.Parse(reader["reval_gil"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader["sum_insaldo"].ToString());
                    serv.Serv.SumSn = Decimal.Parse(reader["sum_sn"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0;
                    serv.CopyToOdn();
                    bill.AddSupp(serv);
                    bill.AddServ(serv);
                }
                cmd.Dispose();
                reader.Close();
                reader.Dispose();
                return true;
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка начислений " + e.Message, MonitorLog.typelog.Error, true);
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                return false;
            }

        }


        /// <summary>
        /// Загрузка формул расчета
        /// </summary>
        /// <param name="prefKernel">Префикс базы данных</param>
        /// <param name="ret">Результат операции</param>
        private void LoadFormulList(string prefKernel, out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            IDataReader reader = null;
            if (_formulList == null) _formulList = new Dictionary<int, string>();
            else _formulList.Clear();
            string s = " SELECT nzp_frm, a.nzp_measure, measure  " +
                       " FROM " + prefKernel + DBManager.sKernelAliasRest + "formuls a, " +
                                  prefKernel + DBManager.sKernelAliasRest + "s_measure b" +
                       " WHERE a.nzp_measure = b.nzp_measure ";

            IDbCommand cmd = DBManager.newDbCommand(s, _conDb);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {


                    if (reader["nzp_frm"] != DBNull.Value)
                    {
                        _formulList.Add(Convert.ToInt32(reader["nzp_frm"]),
                            reader["nzp_measure"].ToString().Trim() + "=" +
                            reader["measure"].ToString().Trim());
                    }
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборке списка формул" + e.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }


        }




        /// <summary>
        /// Определение единицы измерения и кода единицы измерения по формуле
        /// </summary>
        /// <param name="nzpFrm">код формулы</param>
        /// <param name="edIzmer">единица измрения</param>
        /// <param name="nzpMeasure">код единица измерения</param>
        private void GetMeasureByFrm(int nzpFrm, ref string edIzmer, ref int nzpMeasure)
        {
            if (_formulList != null && nzpFrm != 0)
            {
                var formula = from cust in _formulList
                              where cust.Key == nzpFrm
                              select cust.Value;

                foreach (string value in formula)
                {
                    nzpMeasure = Convert.ToInt32(value.Substring(0, value.IndexOf("=", StringComparison.Ordinal)));
                    if (value.IndexOf("=", StringComparison.Ordinal) > -1)
                    {
                        edIzmer = value.Substring(value.IndexOf("=", StringComparison.Ordinal) + 1, value.Length -
                            value.IndexOf("=", StringComparison.Ordinal) - 1);
                    }

                }

            }


        }

        /// <summary>
        /// Подготавливает данные для группового или массового формирования счетов
        /// </summary>
        /// <param name="fBlock"></param>
        /// <param name="ret"></param>
        private void PrepareGroupTempTable(FakturaBlockTable fBlock, out Returns ret)
        {
            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            var sql = new StringBuilder();
            string tXxSpls;
            string datCalc = "01." + _finder.month_ + "." + _finder.year_;
            try
            {
                ret = DBManager.OpenDb(connWeb, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Формирование счетов. Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return;
                }
                tXxSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + _finder.nzp_user + "_spls";
            }
            finally
            {
                connWeb.Close();
            }



            DBManager.ExecSQL(_conDb, " drop table fsel_kvar", false);


            sql.Remove(0, sql.Length);
            sql.Append(" Create temp table fsel_kvar ( " +
                       " nzp_kvar integer, " +
                       " num_ls integer, " +
                       " pkod " + DBManager.sDecimalType + "(13,0), " +
                       " nzp_dom integer, " +
                       " nzp_ul integer, " +
                       " typek integer default 0, " +
                       " fio char(100), " +
                       " ulica char(100), " +
                       " ndom char(20), " +
                       " idom integer, " +
                       " ikvar integer, " +
                       " nkvar char(20), " +
                       " nkvar_n char(10), " +
                       " nzp_geu integer default 0," +
                       " nzp_area integer default 0, " +
                       " uch integer default 0, " +
                       " pref char(10) ) " + DBManager.sUnlogTempTable);
            DBManager.ExecSQL(_conDb, sql.ToString(), false);

            #region Выборка списка квартир

            if ((_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One) ||
                (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Web) ||
                (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Bank))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO fsel_kvar ( nzp_kvar, num_ls, pkod, nzp_dom, nzp_ul, typek, ");
                sql.Append("        fio, ulica, ndom, ");
                sql.Append("        idom, ikvar, nkvar,nkvar_n, ");
                sql.Append("        nzp_geu, nzp_area, uch, pref) ");
                sql.Append(" SELECT k.nzp_kvar, k.num_ls, k.pkod, k.nzp_dom, d.nzp_ul, k.typek, ");
                sql.Append("        k.fio,");
                sql.Append(" (trim(" + DBManager.sNvlWord + "(s.ulicareg,''))||' '||s.ulica) as ulica, ");
                sql.Append("        trim(d.ndom)||' '||(case when trim(coalesce(d.nkor,'')) not in ('', '-', '*') then 'корп.'||trim(d.nkor) else '' end) as ndom, ");
                sql.Append("        d.idom, k.ikvar, trim(" + DBManager.sNvlWord + "(k.nkvar,'')) as nkvar, trim(" + DBManager.sNvlWord + "(k.nkvar_n,'')) as nkvar_n,");
                sql.Append("        k.nzp_geu, k.nzp_area, 0 as uch, k.pref ");
                sql.Append(" FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar k , ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "dom d, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_ulica s ");
                sql.Append(" WHERE k.num_ls>0 ");
                sql.Append("        AND k.nzp_dom=d.nzp_dom ");
                sql.Append("        AND d.nzp_ul=s.nzp_ul ");

                if (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One ||
                   _finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Web)
                    sql.Append("        AND k.nzp_kvar = " + _finder.nzp_kvar);

                if (_finder.nzp_area > 0)
                    sql.Append(" AND k.nzp_area = " + _finder.nzp_area);
                if (_finder.nzp_geu > 0)
                    sql.Append(" AND k.nzp_geu = " + _finder.nzp_geu);
                if (_finder.pref != "")
                    sql.Append(" AND k.pref = '" + _finder.pref + "'");

                if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
                {
                    _conDb.Close();
                    return;
                }
            }
            else if (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Group)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO fsel_kvar ( nzp_kvar, num_ls, pkod, nzp_dom, nzp_ul, typek, ");
                sql.Append("        fio, ulica, ndom, ");
                sql.Append("        idom, ikvar, nkvar, nkvar_n, ");
                sql.Append("        nzp_geu, nzp_area, uch, pref) ");
                sql.Append(" SELECT nzp_kvar, num_ls, pkod, nzp_dom, s.nzp_ul, typek, fio, ");
                sql.Append("        (trim(" + DBManager.sNvlWord + "(ulicareg,''))||' '||s.ulica) as ulica, ");
                sql.Append("        trim(ndom)||' '||(case when trim(coalesce(nkor,'')) not in ('', '-', '*') then 'корп.'||trim(nkor) else '' end) as ndom, ");      
                sql.Append(" idom, ikvar,");
                sql.Append("        trim(" + DBManager.sNvlWord + "(nkvar,'')) as nkvar, trim(" + DBManager.sNvlWord + "(nkvar_n,'')) as nkvar_n, ");
                sql.Append("        nzp_geu, nzp_area, 0 as uch, pref ");
                sql.Append(" FROM " + tXxSpls + " sp, " + Points.Pref + DBManager.sDataAliasRest + "s_ulica s ");
                sql.Append(" WHERE  num_ls>0 AND sp.nzp_ul = s.nzp_ul ");
                if (_finder.nzp_area > 0)
                    sql.Append(" AND nzp_area = " + _finder.nzp_area);
                if (_finder.nzp_geu > 0)
                    sql.Append(" AND nzp_geu = " + _finder.nzp_geu);
                if (_finder.pref != "")
                    sql.Append(" AND pref = '" + _finder.pref + "'");
                if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
                {
                    _conDb.Close();
                    return;
                }
            }

            #endregion

            #region Создание временных табличек

            DBManager.ExecSQL(_conDb, " drop table t_fkvar_prm", false);
            sql.Remove(0, sql.Length);

            sql.Append(" Create temp table t_fkvar_prm (");
            sql.Append(" nzp_kvar integer, ");
            sql.Append(" nzp_dom integer, ");
            sql.Append(" num_ls char(10), ");
            sql.Append(" typek char(10), ");
            sql.Append(" nzp_dom_base integer, ");
            sql.Append(" indecs integer, ");
            sql.Append(" is_open integer default 0, ");
            sql.Append(" is_print integer default 1, ");
            sql.Append(" is_komm integer default 0, ");
            sql.Append(" fio char(40), ");
            sql.Append(" et integer, ");
            sql.Append(" count_gil integer, ");
            sql.Append(" count_gil_time integer, ");
            sql.Append(" count_domgil integer, ");
            sql.Append(" count_gilp integer, ");
            sql.Append(" count_gil_time_epd integer, ");
            sql.Append(" is_paspgil integer default 0, ");
            sql.Append(" pl_kvar " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" pl_kvar_gil " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" pl_dom " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" pl_mop " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" otop_norm_k " + DBManager.sDecimalType + "(14,7), ");//норматив отопления для коммунальных квартир
            sql.Append(" otop_norm_i " + DBManager.sDecimalType + "(14,7), ");//норматив отопления для изолированных квартир
            sql.Append(" gvs_norm_gkal " + DBManager.sDecimalType + "(14,7), ");//норматив на подогрев 1 куб.м. воды
            sql.Append(" hasElDpu integer, ");
            sql.Append(" hasHvsDpu integer, ");
            sql.Append(" hasGvsDpu integer, ");
            sql.Append(" hasGazDpu integer, ");
            sql.Append(" hasOtopDpu integer, ");
            sql.Append(" privat integer, ");
            sql.Append(" open_vodozabor integer) " + DBManager.sUnlogTempTable); //домовой параметр - Открытый водозабор горячей воды 
            if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
            {
                _conDb.Close();
                return;
            }

            DBManager.ExecSQL(_conDb, " insert into t_fkvar_prm(nzp_kvar, num_ls, nzp_dom, typek) SELECT nzp_kvar, num_ls, nzp_dom, " +
                                      " case when typek = 3 then 'нежилого' else 'жилого' end as typek FROM fsel_kvar", true);
            DBManager.ExecSQL(_conDb, " create index ix_fselkv_01 on t_fkvar_prm(nzp_kvar)", true);
            DBManager.ExecSQL(_conDb, DBManager.sUpdStat + " t_fkvar_prm", true);

            DBManager.ExecSQL(_conDb, " drop table t_freasonReval", false);
            sql.Remove(0, sql.Length);
            sql.Append(" Create temp table t_freasonReval (");
            sql.Append(" nzp_kvar integer, ");
            sql.Append(" nzp_serv integer, ");
            sql.Append(" source char(250), ");
            sql.Append(" new_counters integer, ");
            sql.Append(" kod_reval integer, ");
            sql.Append(" dat_plomb char(10), ");
            sql.Append(" num_cnt char(20), ");
            sql.Append(" nedop integer, ");
            sql.Append(" izm_comment char(100), ");
            sql.Append(" counters integer)  " + DBManager.sUnlogTempTable);
            if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
            {
                _conDb.Close();
                return;
            }



            DBManager.ExecSQL(_conDb, " drop table t_fVolume", false);
            sql.Remove(0, sql.Length);
            sql.Append(" Create temp table t_fVolume (");
            sql.Append(" nzp_kvar integer, ");
            sql.Append(" nzp_serv integer, ");
            sql.Append(" cnt_stage integer, ");
            sql.Append(" hvscnt2 integer default 0, ");
            sql.Append(" hvscnt3 integer default 0, ");
            sql.Append(" gvscnt2 integer default 0, ");
            sql.Append(" gvscnt3 integer default 0, ");
            sql.Append(" pu " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" norm " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" norm_full " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" pu_odn " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" norm_odn " + DBManager.sDecimalType + "(14,4), ");
            sql.Append(" koef " + DBManager.sDecimalType + "(14,7),  ");
            sql.Append(" counters integer)  " + DBManager.sUnlogTempTable);
            if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
            {
                _conDb.Close();
                return;
            }
            DBManager.ExecSQL(_conDb, " drop table t_fPerekidka", false);
            sql.Remove(0, sql.Length);
            sql.Append(" Create temp table t_fPerekidka (");
            sql.Append(" nzp_kvar integer, ");
            sql.Append(" nzp_serv integer, ");
            sql.Append(" comment char(100), ");
            sql.Append(" sum_rcl Decimal(14,4))  " + DBManager.sUnlogTempTable);
            if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
            {
                _conDb.Close();
                return;
            }
            #endregion

            MyDataReader goodReader;
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT pref FROM fsel_kvar GROUP BY 1 ORDER BY 1 ");
            if (!DBManager.ExecRead(_conDb, out goodReader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Формирование счетов. Ошибка создания " + sql, MonitorLog.typelog.Error, 20, 201, true);
                _conDb.Close();
                return;
            }
            while (goodReader.Read())
            {
#if PG
                string baseData = goodReader["pref"].ToString().Trim() + "_data.";
                string baseKernel = goodReader["pref"].ToString().Trim() + "_kernel.";
                string tablePerekidka = goodReader["pref"].ToString().Trim() + "_charge_" + (_finder.year_ - 2000).ToString("00") +
                    ".perekidka ";
                string tableCounters = goodReader["pref"].ToString().Trim() + "_charge_" + (_finder.year_ - 2000).ToString("00") +
                    ".counters_" + _finder.month_.ToString("00");
                string tableCorrect = goodReader["pref"].ToString().Trim() + "_data" +
                      ".counters_correct ";
                string tablegil = goodReader["pref"].ToString().Trim() + "_charge_" + (_finder.year_ - 2000).ToString("00") +
                                     ".gil_" + _finder.month_.ToString("00");
#else
                string baseData = goodReader["pref"].ToString().Trim() + "_data@" + DBManager.getServer(_conDb) + DBManager.tableDelimiter;

                string tablePerekidka = goodReader["pref"].ToString().Trim() + "_charge_" + (_finder.year_ - 2000).ToString("00") +
                    "@" + DBManager.getServer(_conDb) + DBManager.tableDelimiter + "perekidka ";
                string tableCounters = goodReader["pref"].ToString().Trim() + "_charge_" + (_finder.year_ - 2000).ToString("00") +
                    "@" + DBManager.getServer(_conDb) + ":counters_" + _finder.month_.ToString("00");
                string tablegil = goodReader["pref"].ToString().Trim() + "_charge_" + (_finder.year_ - 2000).ToString("00") +
                                    "@" + DBManager.getServer(_conDb) + DBManager.tableDelimiter + "gil_" + _finder.month_.ToString("00");
#endif

                #region Квартирные параметры
                //Проставляем базовый дом
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET nzp_dom_base = ");
                sql.Append("        (SELECT a.nzp_dom_base ");
                sql.Append("         FROM " + baseData + "link_dom_lit a ");
                sql.Append("         WHERE t_fkvar_prm.nzp_dom=a.nzp_dom)");
                sql.Append(" WHERE 0 < (SELECT count(*)  ");
                sql.Append("            FROM " + baseData + "link_dom_lit k ");
                sql.Append("            WHERE k.nzp_dom =t_fkvar_prm.nzp_dom)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" update t_fkvar_prm SET indecs = ");
                sql.Append("        (SELECT max(a.val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("         FROM " + baseData + "prm_2 a ");
                sql.Append("         WHERE t_fkvar_prm.nzp_dom=a.nzp ");
                sql.Append("                AND a.nzp_prm=68   ");
                sql.Append("                AND a.is_actual=1  ");
                sql.Append("                AND a.dat_s<='" + datCalc + "'");
                sql.Append("                AND a.dat_po>='" + datCalc + "')");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "prm_2 k ");
                sql.Append("            WHERE k.nzp = t_fkvar_prm.nzp_dom ");
                sql.Append("                   AND k.nzp_prm = 68 ");
                sql.Append("                   AND k.is_actual = 1)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Количество проживающих
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET count_gil = (");
                sql.Append("        SELECT max(val_prm" + DBManager.sConvToNum + ") as count_gil");
                sql.Append("        FROM " + baseData + "prm_1 p");
                sql.Append("        WHERE p.nzp_prm=5 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Количество временно проживающих
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET count_gil_time = (");
                sql.Append("        SELECT max(val_prm" + DBManager.sConvToNum + ") as count_gil");
                sql.Append("        FROM " + baseData + "prm_1 p");
                sql.Append("        WHERE p.nzp_prm=131 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Количество проживающих по прописке
                sql.Remove(0, sql.Length);
                sql.Append(" update t_fkvar_prm SET count_gilp = (");
                sql.Append("        SELECT max(p.val_prm" + DBManager.sConvToNum + " ) as count_gil ");
                sql.Append("        FROM " + baseData + "prm_1 p ");
                sql.Append("        WHERE p.nzp_prm=1010270 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                //Не отражать прибывших в ЕПД
                sql.Remove(0, sql.Length);
                sql.Append(" update t_fkvar_prm SET count_gil_time_epd = (");
                sql.Append("        SELECT max(p.val_prm" + DBManager.sConvToNum + " ) as count_gil ");
                sql.Append("        FROM " + baseData + "prm_1 p ");
                sql.Append("        WHERE p.nzp_prm=1377 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Площадь общая
                sql.Remove(0, sql.Length);
                sql.Append(" update t_fkvar_prm SET pl_kvar = (");
                sql.Append("        SELECT max(Replace(p.val_prm,',','.')" + DBManager.sConvToNum + ") as pl_kvar");
                sql.Append("        FROM " + baseData + "prm_1 p ");
                sql.Append("        WHERE p.nzp_prm=4 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                //Жилая площадь
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET pl_kvar_gil = (");
                sql.Append("        SELECT max(Replace(p.val_prm,',','.')" + DBManager.sConvToNum + ") as pl_kvar");
                sql.Append("        FROM " + baseData + "prm_1 p ");
                sql.Append("        WHERE p.nzp_prm=6 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Приватизированность
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET privat = 1 ");
                sql.Append(" WHERE 0<(SELECT count(*)  as privat ");
                sql.Append("          FROM " + baseData + "prm_1 p ");
                sql.Append("          WHERE p.nzp_prm=8 ");
                sql.Append("                 AND p.is_actual=1 ");
                sql.Append("                 AND p.dat_s<='" + datCalc + "'");
                sql.Append("                 AND p.val_prm='1' ");
                sql.Append("                 AND p.dat_po>='" + datCalc + "'");
                sql.Append("                 AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" AND 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Благоустроенность
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET is_komm = 1 ");
                sql.Append(" WHERE 0<(SELECT count(*)  as komm ");
                sql.Append("          FROM " + baseData + "prm_1 p ");
                sql.Append("          WHERE p.nzp_prm=3 ");
                sql.Append("                 AND p.is_actual=1 ");
                sql.Append("                 AND p.dat_s<='" + datCalc + "'");
                sql.Append("                 AND p.val_prm='2' ");
                sql.Append("                 AND p.dat_po>='" + datCalc + "'");
                sql.Append("                 AND p.nzp = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                //не печатать ежемесячный счет
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET is_print = 0 ");
                sql.Append(" WHERE 0<(SELECT count(*)   ");
                sql.Append("          FROM " + baseData + "prm_1 p ");
                sql.Append("          WHERE p.nzp_prm=23 ");
                sql.Append("                 AND p.is_actual=1 ");
                sql.Append("                 AND p.dat_s<='" + datCalc + "'");
                sql.Append("                 AND p.dat_po>='" + datCalc + "'");
                sql.Append("                 AND p.nzp = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Благоустроенность
                sql.Remove(0, sql.Length);
                sql.Append(" update t_fkvar_prm SET et = (");
                sql.Append("        SELECT max(p.val_prm" + DBManager.sConvToNum + ") as et");
                sql.Append("        FROM " + baseData + "prm_1 p ");
                sql.Append("        WHERE p.nzp_prm=2 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Приватизированность
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET is_paspgil = 1 ");
                sql.Append(" WHERE 0<(SELECT count(*)  as paspgil ");
                sql.Append("          FROM " + baseData + "prm_1 p ");
                sql.Append("          WHERE p.nzp_prm=130 ");
                sql.Append("                 AND p.is_actual=1 ");
                sql.Append("                 AND p.dat_s<='" + datCalc + "'");
                sql.Append("                 AND p.val_prm='1' ");
                sql.Append("                 AND p.dat_po>='" + datCalc + "'");
                sql.Append("                 AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" AND 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //Фио квартиросъемщика
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET fio = (");
                sql.Append("        SELECT max(p.val_prm) ");
                sql.Append("        FROM " + baseData + "prm_3 p ");
                sql.Append("        WHERE p.nzp_prm=46 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" WHERE 0 < (SELECT count(*) ");
                sql.Append("            FROM " + baseData + "kvar k ");
                sql.Append("            WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET is_open = (");
                sql.Append("        SELECT max(1) ");
                sql.Append("        FROM " + baseData + "prm_3 p ");
                sql.Append("        WHERE p.nzp_prm=51 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.val_prm='1'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_kvar)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);



                //Площадь дома
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET pl_dom =(");
                sql.Append("        SELECT max(val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("        FROM " + baseData + "prm_2 p ");
                sql.Append("        WHERE p.nzp_prm=40 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET pl_dom =(");
                sql.Append("        SELECT sum(p.val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("        FROM " + baseData + "prm_2 p, ");
                sql.Append(baseData + "link_dom_lit d ");
                sql.Append("        WHERE p.nzp_prm=40 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp=d.nzp_dom ");
                sql.Append("               AND d.nzp_dom_base = t_fkvar_prm.nzp_dom_base)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar =t_fkvar_prm.nzp_kvar ) ");
                sql.Append("                 AND t_fkvar_prm.nzp_dom_base is not null ");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //площадь мест общего пользования дома
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET pl_mop =(");
                sql.Append("        SELECT max(p.val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("        FROM " + baseData + "prm_2 p ");
                sql.Append("        WHERE p.nzp_prm=2049 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp=t_fkvar_prm.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET pl_mop =(");
                sql.Append("        SELECT sum(p.val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("        FROM " + baseData + "prm_2 p, ");
                sql.Append(baseData + "link_dom_lit d ");
                sql.Append("        WHERE p.nzp_prm=2049 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.nzp=d.nzp_dom ");
                sql.Append("               AND d.nzp_dom_base = t_fkvar_prm.nzp_dom_base ");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp=d.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar) ");
                sql.Append("       AND t_fkvar_prm.nzp_dom_base is not null ");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                //Количество проживающих в доме
                DBManager.ExecSQL(_conDb, "drop table t_12", false);

                sql.Remove(0, sql.Length);
                sql.Append(" CREATE TEMP TABLE t_12 ( ");
                sql.Append(" nzp_dom integer, ");
                sql.Append(" count_gil integer) " + DBManager.sUnlogTempTable);
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_12(nzp_dom, count_gil) ");
                sql.Append(" SELECT nzp_dom, sum(cnt2-val5+val3) as count_gil ");
                sql.Append(" FROM " + tablegil + " a");
                sql.Append(" WHERE exists (SELECT 1 ");
                sql.Append("               FROM t_fkvar_prm b ");
                sql.Append("               WHERE a.nzp_dom=b.nzp_dom)");
                sql.Append("       AND stek=3 ");
                sql.Append(" GROUP BY 1 ");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_12 SET count_gil = " + DBManager.sNvlWord + "((");
                sql.Append(" SELECT sum(a.cnt2 - a.val5 + a.val3) as count_gil ");
                sql.Append(" FROM " + tablegil + " a");
                sql.Append(" WHERE exists (SELECT 1 ");
                sql.Append("               FROM t_fkvar_prm d, ");
                sql.Append(baseData + "link_dom_lit k ");
                sql.Append("               WHERE a.nzp_dom=k.nzp_dom ");
                sql.Append("                      AND d.nzp_dom_base=k.nzp_dom_base ");
                sql.Append("                      AND d.nzp_dom_base is not null)),0) ");
                sql.Append(" WHERE exists (SELECT  1 ");
                sql.Append("               FROM " + baseData + "link_dom_lit t ");
                sql.Append("               WHERE t.nzp_dom=t_12.nzp_dom)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET count_domgil =(");
                sql.Append("        SELECT sum(t_12.count_gil) ");
                sql.Append("        FROM t_12 ");
                sql.Append("        WHERE t_12.nzp_dom = t_fkvar_prm.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM t_12 k ");
                sql.Append("          WHERE k.nzp_dom = t_fkvar_prm.nzp_dom)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                DBManager.ExecSQL(_conDb, "drop table t_12", true);


                //норматив отопления для изолированных квартир
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET otop_norm_i =(");
                sql.Append("        SELECT max(p.val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("        FROM " + baseData + "prm_2 p ");
                sql.Append("        WHERE p.nzp_prm = 723 ");
                sql.Append("              AND p.is_actual = 1 ");
                sql.Append("              AND p.dat_s <= '" + datCalc + "'");
                sql.Append("              AND p.dat_po >= '" + datCalc + "'");
                sql.Append("              AND p.nzp = t_fkvar_prm.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar =t_fkvar_prm.nzp_kvar) ");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET otop_norm_k =(");
                sql.Append("        SELECT max(p.val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("        FROM " + baseData + "prm_2 p ");
                sql.Append("        WHERE p.nzp_prm=2074 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "' ");
                sql.Append("               AND p.nzp=t_fkvar_prm.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar =t_fkvar_prm.nzp_kvar) ");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                //норматив по горячей воде для подогрева 1 куб.м.
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET gvs_norm_gkal =(");
                sql.Append("        SELECT max(p.val_prm" + DBManager.sConvToNum + ") ");
                sql.Append("        FROM " + baseData + "prm_2 p ");
                sql.Append("        WHERE p.nzp_prm = 436 ");
                sql.Append("               AND p.is_actual = 1 ");
                sql.Append("               AND p.dat_s <= '" + datCalc + "'");
                sql.Append("               AND p.dat_po >= '" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar) ");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                //Открытый водозабор горячей воды
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_fkvar_prm SET open_vodozabor =(");
                sql.Append("        SELECT max(" + DBManager.sNvlWord + "(val_prm" + DBManager.sConvToNum + ",0)) ");
                sql.Append("        FROM " + baseData + "prm_2 p ");
                sql.Append("        WHERE p.nzp_prm=35 ");
                sql.Append("               AND p.is_actual=1 ");
                sql.Append("               AND p.dat_s<='" + datCalc + "'");
                sql.Append("               AND p.dat_po>='" + datCalc + "'");
                sql.Append("               AND p.nzp = t_fkvar_prm.nzp_dom)");
                sql.Append(" WHERE 0<(SELECT count(*) ");
                sql.Append("          FROM " + baseData + "kvar k ");
                sql.Append("          WHERE k.nzp_kvar = t_fkvar_prm.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);


                #endregion
                if (fBlock.HasRevalReasonBlock)
                {
                    #region Причины перерасчета Недопоставки
                    //ИЗНАЧАЛЬНО БЫЛО ТАК У БАРСА
                    /*string sql0 = " INSERT into t_freasonReval(nzp_serv, nzp_kvar, source,izm_comment)" +
                                  " SELECT a.nzp_serv, a.nzp_kvar,'Недопоставка ',''" +
                                  " FROM  " + baseData + "nedop_kvar a, fsel_kvar b " +
                                  " WHERE a.nzp_kvar=b.nzp_kvar AND month_calc = " +
                                  DBManager.MDY(_finder.month_, 01, _finder.year_) +
                                  " AND is_actual=1  " +
                                  " GROUP BY 1,2,3,4";*/
                    string sql0 = " INSERT into t_freasonReval(nzp_serv, nzp_kvar, source,izm_comment)" +
                                  " SELECT a.nzp_serv, a.nzp_kvar,'Недопоставка ', 'Недоставка с '|| TO_CHAR(a.dat_s, 'DD.mm.yyyy') || ' по ' || TO_CHAR(a.dat_po, 'DD.mm.yyyy')" +
                                  " FROM  " + baseData + "nedop_kvar a, fsel_kvar b " +
                                  " WHERE a.nzp_kvar=b.nzp_kvar AND month_calc = " +
                                  DBManager.MDY(_finder.month_, 01, _finder.year_) +
                                  " AND is_actual=1  " +
                                  " GROUP BY 1,2,3,4";
                    DBManager.ExecSQL(_conDb, sql0, true);


                    //string sql0 = " INSERT into t_freasonReval(nzp_serv, nzp_kvar, source,izm_comment)" +
                    //              " SELECT a.nzp_serv, a.nzp_kvar,'Недопоставка ','Недопоставка '||trim(p.name)" +
                    //              " FROM  " + baseData + "nedop_kvar a, " + baseData +
                    //              "upg_s_kind_nedop p ,fsel_kvar b " +
                    //              " WHERE a.nzp_kvar=b.nzp_kvar AND month_calc = " +
                    //              DBManager.MDY(_finder.month_, 01, _finder.year_) +
                    //              " AND is_actual=1 and a.nzp_kind=p.nzp_kind and p.kod_kind=1 " +
                    //              " GROUP BY 1,2,3,4";
                    //DBManager.ExecSQL(_conDb, sql0, true);

                    //string sql0 = " INSERT into t_freasonReval(nzp_serv, nzp_kvar, source, izm_comment)" +
                    //              " SELECT a.nzp_serv, a.nzp_kvar,'Недопоставка '," +
                    //              " 'Недопоставка '||trim(" + DBManager.sNvlWord + "(a.comment,''))  " +
                    //              " FROM  " + baseData + "nedop_kvar a, fsel_kvar b " +
                    //              " WHERE a.nzp_kvar=b.nzp_kvar AND month_calc = " +
                    //              DBManager.MDY(_finder.month_, 01, _finder.year_) +
                    //              " AND is_actual=1 " +
                    //              " GROUP BY 1,2,3,4";
                    //DBManager.ExecSQL(_conDb, sql0, true);


                    //Счетчики
                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT into t_freasonReval(nzp_serv, nzp_kvar, source)");
                    sql.Append(" SELECT a.nzp_serv, a.nzp_kvar,'Cчетчики' ");
                    sql.Append(" FROM  " + baseData + "counters a, fsel_kvar k ");
                    sql.Append(" WHERE a.nzp_kvar=k.nzp_kvar AND month_calc = MDY(");
                    sql.Append(_finder.month_ + ",01," + _finder.year_ + ") ");
                    sql.Append(" AND is_actual<>100 AND dat_uchet<MDY(");
                    sql.Append(_finder.month_ + ",01," + _finder.year_ + ")");
                    sql.Append(" GROUP BY 1,2,3 ");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    //Счетчики
                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT into t_freasonReval(nzp_serv, nzp_kvar, source, num_cnt,  dat_plomb)");
                    sql.Append(" SELECT a.nzp_serv, k.nzp_kvar,'Новые счетчики', a.num_cnt, p.val_prm  ");
                    sql.Append(" FROM  " + baseData + "counters_spis a, " + baseData + "prm_17 p, fsel_kvar k ");
                    sql.Append(" WHERE a.nzp_type=3 AND a.nzp=k.nzp_kvar AND p.month_calc = MDY(");
                    sql.Append(_finder.month_ + ",01," + _finder.year_ + ") ");
                    sql.Append(" AND a.is_actual<>100 AND p.is_actual<>100 AND p.nzp_prm=2027 AND a.nzp_counter=p.nzp");
                    sql.Append(" GROUP BY 1,2,3,4,5 ");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    //перекидки
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_freasonReval(nzp_serv, nzp_kvar,source, izm_comment)");
                    sql.Append(" SELECT a.nzp_serv, b.nzp_kvar, 'Перекидки',c.comment  ");
                    sql.Append(" FROM  " + tablePerekidka + " a, fsel_kvar b, fbill_data.document_base c ");
                    sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar  AND a.nzp_doc_base = c.nzp_doc_base");
                    sql.Append(" AND month_=" + _finder.month_);
                    sql.Append(" AND c.comment != 'Выравнивание сальдо'");
                    sql.Append(" GROUP BY 1,2,3,4");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    /*sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_freasonReval(nzp_serv, nzp_kvar,source, izm_comment)");
                    sql.Append(" SELECT a.nzp_serv, b.nzp_kvar, 'Перекидки',a.comment  ");
                    sql.Append(" FROM  " + tablePerekidka + " a, fsel_kvar b ");
                    sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar  ");
                    sql.Append(" AND month_=" + _finder.month_);
                    sql.Append(" GROUP BY 1,2,3,4");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);*/


                    //Must_calc
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_freasonReval(nzp_serv, nzp_kvar,source, izm_comment,kod_reval )");
                    sql.Append(" SELECT a.nzp_serv, b.nzp_kvar, 'Must_calc',dat_s||'-'||dat_po, kod1   ");
                    sql.Append(" FROM  " + baseData + "must_calc a, fsel_kvar b ");
                    sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar ");
                    sql.Append("        AND nzp_serv>1  ");
                    sql.Append("        AND month_=" + _finder.month_);
                    sql.Append("        AND year_=" + _finder.year_);
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);



                    #endregion
                }
                if (fBlock.HasPerekidkiSamaraBlock)
                {
                    #region Перекидки
                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT into t_fPerekidka(nzp_kvar, nzp_serv, sum_rcl, comment) ");
                    sql.Append(" SELECT a.nzp_kvar, a.nzp_serv, sum(a.sum_rcl) as sum_rcl, 'qqqqqqq' ");
                    sql.Append(" FROM  " + tablePerekidka + " a, fsel_kvar b ");
                    sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar AND type_rcl = 63 ");
                    sql.Append("         AND month_=" + _finder.month_);
                    sql.Append(" GROUP BY 1,2");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    #endregion
                }
                if (fBlock.HasServiceVolumeBlock)
                {
                    #region Расходы по ЛС
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_fVolume(nzp_kvar, nzp_serv, pu, norm, norm_full, ");
                    sql.Append(" pu_odn, norm_odn, koef, cnt_stage, hvscnt2, hvscnt3, ");
                    sql.Append(" gvscnt2, gvscnt3)  ");
                    sql.Append(" SELECT a.nzp_kvar,a.nzp_serv, ");
                    sql.Append(" sum(case when stek = 3 AND cnt_stage=1 then val2+val1+dlt_reval else 0 end ) as pu, ");
                    sql.Append(" sum(case when stek = 30 AND cnt1>0 then val1/cnt1 else 0 end) as norma, ");
                    sql.Append(" sum(case when stek = 3 then val1+dlt_reval else 0 end) as norm_full, ");
                    sql.Append(" sum(case when cnt_stage =1 AND stek = 3 AND dop87<0 AND val1+val2+dlt_reval > 0  AND  ");
                    sql.Append(" abs(dop87)>val1+val2+dlt_reval then -(val1+val2+dlt_reval) ");
                    sql.Append("  when cnt_stage =1 AND stek = 3 AND  dop87<0 AND val1+val2+dlt_reval > 0 AND abs(dop87)<val1+val2+dlt_reval then dop87 ");
                    sql.Append("  when cnt_stage =1 AND stek = 3 AND dop87<0 AND val1+val2+dlt_reval<0  then  0 ");
                    sql.Append("  when cnt_stage =1 AND stek = 3 AND dop87>0 then dop87 else 0 end) as pu_odn, ");
                    sql.Append(" sum(case when cnt_stage <1 AND stek = 3 AND dop87<0 AND val1+val2+dlt_reval > 0  AND  ");
                    sql.Append(" abs(dop87)>val1+val2+dlt_reval then -(val1+val2+dlt_reval) ");
                    sql.Append("  when cnt_stage <1 AND stek = 3 AND  dop87<0 AND val1+val2+dlt_reval > 0 AND abs(dop87)<val1+val2+dlt_reval then dop87 ");
                    sql.Append("  when cnt_stage <1 AND stek = 3 AND dop87<0 AND val1+val2+dlt_reval<0  then  0 ");
                    sql.Append("  when cnt_stage <1 AND stek = 3 AND dop87>0 then dop87 else 0 end) as norm_odn, ");
                    sql.Append(" max(case when stek = 3 then kf307 else 0 end), max(cnt_stage), ");
                    sql.Append(" max(case when nzp_serv = 6 then cnt2 else 0 end), ");
                    sql.Append(" max(case when nzp_serv = 6 then cnt3 else 0 end), ");
                    sql.Append(" max(case when nzp_serv = 9 then cnt2 else 0 end), ");
                    sql.Append(" max(case when nzp_serv = 9 then cnt3 else 0 end) ");
                    sql.Append("  FROM " + tableCounters + " a, fsel_kvar b ");
                    sql.Append("  WHERE a.nzp_kvar=b.nzp_kvar AND dat_charge is null ");
                    sql.Append("  AND stek in (3,30) AND nzp_type=3 AND kod_info<>24 ");
                    sql.Append("  GROUP BY 1,2  ");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    sql.Remove(0, sql.Length);
                    sql.Append(" update t_fVolume SET norm = " + DBManager.sNvlWord + "((SELECT max(value" + DBManager.sConvToNum + ") FROM ");
                    sql.Append(Points.Pref + DBManager.sKernelAliasRest + "res_values WHERE nzp_res= t_fVolume.hvscnt3 ");
                    sql.Append(" AND nzp_x=1 AND nzp_y=t_fVolume.hvscnt2),0) ");
                    sql.Append(" WHERE norm = 0 AND nzp_serv=6 ");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    sql.Remove(0, sql.Length);
                    sql.Append(" update t_fVolume SET norm = " + DBManager.sNvlWord + "((SELECT max(value" + DBManager.sConvToNum + ") FROM ");
                    sql.Append(Points.Pref + DBManager.sKernelAliasRest + "res_values WHERE nzp_res= t_fVolume.gvscnt3 ");
                    sql.Append(" AND nzp_x=2 AND nzp_y=t_fVolume.gvscnt2),0) ");
                    sql.Append(" WHERE norm = 0 AND nzp_serv=9 ");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    //sql.Remove(0, sql.Length);
                    //sql.Append(" update t_fVolume SET norm = coalesce((SELECT max(value+0) FROM ");
                    //sql.Append(Points.Pref + "_kernel.res_values WHERE nzp_res= t_fVolume.gvscnt3 ");
                    //sql.Append(" AND nzp_x=3 AND nzp_y=t_fVolume.gvscnt2),0) ");
                    //sql.Append(" WHERE norm = 0 AND nzp_serv=7 ");
                    //if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    //{
                    //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //    conn_db.Close();
                    //    return;
                    //}
                    //sql.Remove(0, sql.Length);
                    //sql.Append(" update t_fVolume SET norm = coalesce((SELECT max(value+0) FROM ");
                    //sql.Append(Points.Pref + "_kernel.res_values WHERE nzp_res= t_fVolume.gvscnt3 ");
                    //sql.Append(" AND nzp_x=3 AND nzp_y=t_fVolume.gvscnt2),0) ");
                    //sql.Append(" WHERE norm = 0 AND nzp_serv=7 ");
                    //if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    //{
                    //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //    conn_db.Close();
                    //    return;
                    //}
                    #endregion
                }
                sql.Remove(0, sql.Length);
                sql.Append(" update fsel_kvar SET uch = (");
                sql.Append(" SELECT uch FROM " + baseData + "kvar a ");
                sql.Append(" WHERE a.nzp_kvar=fsel_kvar.nzp_kvar)");
                DBManager.ExecSQL(_conDb, sql.ToString(), true);

                if (fBlock.HasBezenchuk)
                {
                    sql = new StringBuilder();
                    DBManager.ExecSQL(_conDb, "drop table TempBezenchukPrm", false);
                    sql.Append("CREATE TEMP TABLE TempBezenchukPrm ");
                    sql.Append("(nzp_dom integer, vol_odpu " + DBManager.sDecimalType + "(14,4), vol_otop " + DBManager.sDecimalType);
                    sql.Append("(14,4),vol_gvs " + DBManager.sDecimalType + "(14,4))" + DBManager.sUnlogTempTable);
                    if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Формирование счетов. Ошибка создания " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        _conDb.Close();
                        return;
                    }
                    sql = new StringBuilder();
                    sql.Append("INSERT INTO TempBezenchukPrm (nzp_dom, vol_odpu,vol_otop,vol_gvs) ");
                    sql.Append(" SELECT t.nzp_dom, ");
                    sql.Append(" max(case when p2.nzp_prm=652 then p2.val_prm end) as vol_odpu, ");
                    sql.Append(" max(case when p2.nzp_prm=1104 then p2.val_prm end) as vol_otop, ");
                    sql.Append(" max(case when p2.nzp_prm=1106 then p2.val_prm end) as vol_gvs ");
                    sql.Append(" FROM " + baseData + "prm_2 p2,t_fkvar_prm t  where p2.nzp=t.nzp_dom and p2.is_actual<>100 and " + DBManager.sCurDate + " between p2.dat_s and p2.dat_po group by t.nzp_dom ");
                    ret = DBManager.ExecSQL(_conDb, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Формирование счетов. Ошибка создания " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        _conDb.Close();
                        return;
                    }
                }

            }
            goodReader.Close();


            DBManager.ExecSQL(_conDb, "create index ix_fselkv_03 on t_freasonReval(nzp_kvar, nzp_serv)", true);
            DBManager.ExecSQL(_conDb, DBManager.sUpdStat + " t_freasonReval", true);
            DBManager.ExecSQL(_conDb, "create index ix_fselkv_04 on t_fVolume(nzp_kvar, nzp_serv)", true);
            DBManager.ExecSQL(_conDb, DBManager.sUpdStat + " t_fVolume", true);
            DBManager.ExecSQL(_conDb, "create index ix_fselkv_06 on t_fPerekidka(nzp_kvar, nzp_serv)", true);
            DBManager.ExecSQL(_conDb, DBManager.sUpdStat + " t_fPerekidka", true);

        }

        public void UpdateBillFon(Int32 iCount)
        {
            if (_finder.workRegim != STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Bank) return;

            var dba = new DbBillQueueClient();
            try
            {
                dba.SetTaskProgress(Convert.ToInt32(_finder.pm_note), (decimal)iCount / 100);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                dba.Close();
            }
        }


        /// <summary>
        /// Заполнение расходов по услугам по домам с Арендаторами
        /// </summary>
        /// <param name="numLs">Лиецовой счет</param>
        /// <param name="lastDay">Последний день месяца</param>
        /// <returns></returns>
        public string GetNumAct(int numLs, string lastDay)
        {
            #region Заполнение расходоп по услугам по домам с Арендаторами
            var sql = new StringBuilder();
            MyDataReader reader;
            string numAct = "";
            //Первая часть арендаторов
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM " + Points.Pref + DBManager.sDataAliasRest + "num_acts s ");
            sql.Append(" WHERE dat_act='" + lastDay + "'");
            sql.Append(" AND num_ls= " + numLs + "'");

            DBManager.ExecRead(_conDb, out reader, sql.ToString(), true);
            if (reader.Read()) // Такой акт уже есть
            {
                numAct = reader["num_act"].ToString();

            }
            else
            {
                reader.Close();
                IDbTransaction tr = _conDb.BeginTransaction();
                try
                {

                    sql.Remove(0, sql.Length);
                    sql.Append(" Select max(num_act) as num_act From " + Points.Pref + DBManager.sDataAliasRest + "num_acts  ");
                    sql.Append(" WHERE year_= " + lastDay.Substring(lastDay.Length - 4) + ")");

                    DBManager.ExecRead(_conDb, tr, out reader, sql.ToString(), true);
                    if (reader.Read())
                    {
                        numAct = (Int32.Parse(reader["num_act"].ToString()) + 1).ToString(CultureInfo.InvariantCulture);
                    }
                    reader.Close();
                    sql.Remove(0, sql.Length);
                    sql.Append("insert into " + Points.Pref + DBManager.sDataAliasRest + "num_acts(num_ls,dat_act,year_,num_act)");
                    sql.Append("values(" + numLs + "','" + lastDay + "'," + lastDay.Substring(6, 4) + "," + numAct + ")");
                    DBManager.ExecSQL(_conDb, sql.ToString(), true);

                    if (tr != null) tr.Commit();

                }
                catch
                {
                    if (tr != null) tr.Rollback();
                    MonitorLog.WriteLog("Ошибка выборки 29 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    return "";
                }

            }

            return numAct;
            #endregion
        }


        /// <summary>
        /// Получение данных по арендаторам
        /// </summary>
        /// <param name="baseData">Префикс БД</param>
        /// <param name="nzpKvar">Код Лицевого счета</param>
        /// <param name="lastDay">Последний день</param>
        /// <param name="bill"></param>
        /// <returns></returns>
        public bool GetArendData(string baseData, int nzpKvar, string lastDay, ref BaseFactura bill)
        {
            MyDataReader reader;
            var sql = new StringBuilder();

            #region Номер договора

            sql.Remove(0, sql.Length);
            sql.Append(" Select nzp_prm, val_prm From " + baseData + "prm_1 ");
            sql.Append(" WHERE nzp=" + nzpKvar + " AND nzp_prm in (883, 884, 886, 1014, 965, 1013)");

            DBManager.ExecRead(_conDb, out reader, sql.ToString(), true);
            while (reader.Read())
            {
                //.numAct = (Int32.Parse(reader["num_act"].ToString()) + 1).ToString();
                switch (Int32.Parse(reader["nzp_prm"].ToString()))
                {
                    case 883: bill.ArendNumDog = reader["val_prm"].ToString().Trim(); break;
                    case 884: bill.ArendDatDog = reader["val_prm"].ToString().Trim(); break;
                    case 886: bill.ArendInnDog = reader["val_prm"].ToString().Trim(); break;
                    case 1014: bill.ArendKppDog = reader["val_prm"].ToString().Trim(); break;
                    case 965: bill.ArendFullName = reader["val_prm"].ToString().Trim(); break;
                    case 1013: bill.ArendUrAdr = reader["val_prm"].ToString().Trim(); break;
                }
            }
            reader.Close();

            if (bill.ArendFullName != "")
            {


                sql.Remove(0, sql.Length);
                sql.Append(" Select nzp_prm, val_prm From " + baseData + "prm_1 ");
                sql.Append(" WHERE nzp=" + nzpKvar + " AND nzp_prm in (883, 884, 886, 1014, 965, 1013)");

                DBManager.ExecRead(_conDb, out reader, sql.ToString(), true);
            }


            #endregion
            return true;
        }



        /// <summary>
        /// Процедура получения счета 
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        private DataSet GetGroupDataSet(out Returns ret)
        {
            StreamWriter sw = new StreamWriter(@"C:\temp\people888.txt", false);
            UpdateBillFon(0);
            var fDataSet = new DataSet();

            LoadFormulList(Points.Pref, out ret);
            //LoadFormulDefinitionList(out ret, finder.year_, finder.month_);            

            var sql = new StringBuilder();
            string datCalc = "01." + _finder.month_ + "." + _finder.year_;
            string lastDay = DateTime.DaysInMonth(_finder.year_, _finder.month_) + "." +
                _finder.month_ + "." + _finder.year_;
            int countKvit = 0;
            sw.WriteLine(_finder.idFaktura);
            
            try
            {

                #region Запонение таблицы для отчета
                BaseFactura bill = null;

                switch (_finder.idFaktura)
                {
                    case 100:
                        {
                            bill = new SamaraFaktura();
                            break;
                        }
                    case 196:
                        {
                            bill = new SamaraFaktura();
                            break;
                        }
                    case 101:
                        {
                            bill = new SamaraFakturaDolg();
                            break;
                        }
                    case 102:
                        {
                            bill = new SahaFaktura();
                            break;
                        }
                    case 103:
                        {
                            bill = new StandartFaktura();
                            break;
                        }
                    case 104:
                        {
                            bill = new GubkinFaktura();
                            break;
                        }
                    case 105:
                        {
                            bill = new KapremontFaktura();
                            break;
                        }
                    case 106:
                        {
                            bill = new AstrahanFaktura();
                            break;
                        }
                    case 1006:
                        {
                            bill = new AstrahanFaktura();
                            break;
                        }
                    case 107:
                    case 122:
                    case 108:
                        {
                            //bill = new TulaFaktura();
                            //((TulaFaktura) bill).Dolgnik = _finder.withDolg;
                            bill = new SamaraFaktura();
                            break;
                        }
                    case 118:
                        {
                            bill = new KalugaFaktura();
                            break;
                        }
                    case 109:
                        {
                            bill = new KznUyutdFaktura();
                            if ((_finder.month_ + _finder.year_ * 12) > (08 + 2013 * 12))
                            {
                                bill.FakturaBlocks.HasDomRashodBlock = false;
                                bill.FakturaBlocks.HasServiceVolumeBlock = true;
                            }

                            break;
                        }

                    case 111:
                        {
                            bill = new ZelFaktura();
                            break;
                        }

                    case 112:
                        {
                            bill = new ZhigulFaktura();
                            break;
                        }
                    case 113:
                        {
                            bill = new SuppSamaraFaktura();
                            break;
                        }
                    case 114:
                        {
                            bill = new NorthOsetiaFactura();
                            break;
                        }
                    case 115:
                        {
                            bill = new NorthOsetiaFactura(_finder);
                            break;
                        }
                    case 116:
                        {
                            bill = new KapremontFaktura();
                            break;
                        }
                    case 117:
                        {
                            bill = new ZhigulFakturaKapr();
                            break;
                        }
                    case 188:
                        {
                            bill = new BezenchukFakturaKapr();
                            break;
                        }
                    case 119:
                        {
                            bill = new ZhigulFakturaKaprOtopl();
                            break;
                        }
                    case 120:
                        {
                            bill = new BezenchukFakturaKaprOtopl();
                            break;
                        }
                    //case 122:
                    case 123:
                        {
                            bill = new TulaFaktura();
                            break;
                        }

                }

                if (bill == null)
                {
                    bill = new SamaraFaktura();
                }

                if (bill.DbfakturaCounters == null)
                    bill.DbfakturaCounters = new DbFakturaCounters(_conDb, _finder.month_, _finder.year_);

                //Заполнение данных для формирования счета
                try
                {
                    PrepareGroupTempTable(bill.FakturaBlocks, out ret);
                }
                catch (Exception ex)
                {
                    sw.WriteLine("error1");
                    MonitorLog.WriteLog("Ошибка заполнения пред таблиц " + ex.Message, MonitorLog.typelog.Error, true);
                }

                DataTable table = bill.MakeTable();
                if (table == null)
                {
                    sw.WriteLine("error2");
                    MonitorLog.WriteLog("Ошибка создания счета квитанции, заголовок", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return fDataSet;
                }

                fDataSet.Tables.Add(table);

                #endregion
                UpdateBillFon(20);
                int maxCountLs = 1;
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT count(*) as co ");
                sql.Append(" FROM fsel_kvar a left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_geu sg");
                sql.Append(" on a.nzp_geu=sg.nzp_geu ");
                sql.Append(" ," + Points.Pref + DBManager.sDataAliasRest + "s_ulica u ");
                sql.Append(" ," + Points.Pref + DBManager.sDataAliasRest + "s_rajon r ");
                sql.Append(" WHERE a.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj ");
                if (_finder.idFaktura != 101 && _finder.idFaktura != 114 && _finder.idFaktura != 115)
                {
                    sql.Append(" AND nzp_kvar in (SELECT nzp_kvar FROM t_fkvar_prm WHERE is_open = 1 ");
                    if (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Bank || _finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Group) sql.Append("and is_print = 1 ");
                    sql.Append(") ");
                } 
                //if (_finder.idFaktura == 107)
                //{
                //    var dat = new DateTime(_finder.year_, _finder.month_, 1);
                //    sql.Append(" AND nzp_kvar NOT in (SELECT nzp FROM " + Points.Pref + DBManager.sDataAliasRest + "prm_1" + 
                //               " WHERE nzp_prm = 1377 AND is_actual = 1 AND val_prm = '1' " + 
                //               " AND dat_s <='" + dat.AddMonths(1).ToShortDateString() + "' AND dat_po >='" + dat.ToShortDateString() + "') ");
                //}
                MyDataReader goodreader;
                if (!DBManager.ExecRead(_conDb, out goodreader, sql.ToString(), true).result)
                {
                    sw.WriteLine("error3");
                    MonitorLog.WriteLog("Ошибка выборки 30 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                    _conDb.Close();
                    return null;
                }
                if (goodreader.Read())
                {
                    if (goodreader["co"] != DBNull.Value)
                        maxCountLs = Int32.Parse(goodreader["co"].ToString().Trim());
                }
                sw.WriteLine(maxCountLs.ToString());
                goodreader.Close();
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT a.*,geu, rajon,t.town ");
                sql.Append(" FROM fsel_kvar a left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_geu sg");
                sql.Append(" on a.nzp_geu=sg.nzp_geu ");
                sql.Append(" ," + Points.Pref + DBManager.sDataAliasRest + "s_ulica u ");
                sql.Append(" ," + Points.Pref + DBManager.sDataAliasRest + "s_rajon r ");
                sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_town t on r.nzp_town=t.nzp_town");
                sql.Append(" WHERE a.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj ");
                if (_finder.idFaktura != 101 && _finder.idFaktura != 114 && _finder.idFaktura != 115)
                {
                    sql.Append(" AND nzp_kvar in (SELECT nzp_kvar FROM t_fkvar_prm WHERE is_open = 1 ");
                    if (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Bank || _finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Group) sql.Append("and is_print = 1 ");
                    sql.Append(") ");
                }
                sql.Append(" ORDER BY geu,rajon,ulica, idom, ndom, ikvar, nkvar ");

                if (!DBManager.ExecRead(_conDb, out goodreader, sql.ToString(), true).result)
                {
                    sw.WriteLine("error4");
                    MonitorLog.WriteLog("Ошибка выборки 31 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                    _conDb.Close();
                    return null;
                }
                string oldBaseKernel = "";
                string tableRemark = Points.Pref + DBManager.sDataAliasRest + "s_remark";
                int oldDom = -1;
                while (goodreader.Read())
                {
                    bill.Month = _finder.month_;
                    bill.Year = _finder.year_;
                    bill.FullMonthName = _finder.YM.name_month;
                    bill.BillRegim = _finder.workRegim;
                    if ((countKvit % 100) == 0) UpdateBillFon(20 + 40 * countKvit / maxCountLs);
                    countKvit++;

                    string pref = goodreader["pref"].ToString().Trim();

                    string baseData = pref + "_data" + DBManager.tableDelimiter;
                    string tablePackLs = Points.Pref + "_fin_" + (_finder.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "pack_ls";
                    string baseKernel = pref + "_kernel" + DBManager.tableDelimiter;
                    string tableCharge = pref + "_charge_" +
                    (_finder.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + _finder.month_.ToString("00");
                    string tableSz = pref + "_charge_" +
                    (_finder.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_sz_fin_" + _finder.month_.ToString("00");
                    string tableCounters = pref + "_charge_" +
                    (_finder.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "counters_" + _finder.month_.ToString("00");
                    string tableRashNorm = pref + "_charge_" +  
                    (_finder.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + _finder.month_.ToString("00");
                    string tableGil = pref + "_charge_" +
                    (_finder.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "gil_" + _finder.month_.ToString("00");

                    bill.NumLs = Convert.ToString(goodreader["num_ls"]);

                    int nzpKvar = Convert.ToInt32(goodreader["nzp_kvar"]);
                    sw.WriteLine(nzpKvar.ToString());
                    int nzpDom = Convert.ToInt32(goodreader["nzp_dom"]);

                    int nzpArea = goodreader["nzp_area"] != DBNull.Value ? Convert.ToInt32(goodreader["nzp_area"]) : 0;

                    int nzpGeu = goodreader["nzp_geu"] != DBNull.Value ? Convert.ToInt32(goodreader["nzp_geu"]) : 0;

                    int numLs = Convert.ToInt32(goodreader["num_ls"]);
                    bool isPasp = false;

                    bill.Pref = pref;

                    GetListKommServ(baseKernel, ref bill, out ret);
                    #region Загрузка списка объединяемых услуг

                    IDataReader reader;
                    if (oldBaseKernel != baseKernel)
                    {
                        bill.CUnionServ.MasterList.Clear();
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT s1.ordering as ord_base, s1.service_name as serv_base, ");
                        sql.Append("        s1.ed_izmer as ed_izmer_base, a.nzp_serv_base, ");
                        sql.Append("        s2.ordering as ord_uni, s2.service_name as serv_uni, ");
                        sql.Append("        s2.ed_izmer as ed_izmer_uni, a.nzp_serv_uni ");
                        sql.Append(" FROM  " + Points.Pref + DBManager.sKernelAliasRest + "service_union a, ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s1, ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s2 ");
                        sql.Append(" WHERE a.nzp_serv_base=s1.nzp_serv ");
                        sql.Append("        AND a.nzp_serv_uni=s2.nzp_serv ");
                        IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                var servBase = new BaseServ(false)
                                {
                                    Serv =
                                    {
                                        NzpServ = Int32.Parse(reader["nzp_serv_base"].ToString()),
                                        NameServ = reader["serv_base"].ToString(),
                                        Measure = reader["ed_izmer_base"].ToString(),
                                        Ordering = Int32.Parse(reader["ord_base"].ToString())
                                    }
                                };
                                var servUni = new BaseServ(false)
                                {
                                    Serv =
                                    {
                                        NzpServ = Int32.Parse(reader["nzp_serv_uni"].ToString()),
                                        NameServ = reader["serv_uni"].ToString(),
                                        Measure = reader["ed_izmer_uni"].ToString(),
                                        Ordering = Int32.Parse(reader["ord_uni"].ToString())
                                    }
                                };
                                bill.CUnionServ.AddServ(servBase, servUni);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd.Dispose();
                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 32 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        oldBaseKernel = baseKernel;
                    }
                    #endregion

                    List<_Rekvizit> spis = GetListUkBankRekvizit(baseData, out ret);
                    IDbCommand cmd1;

                    if (bill.FakturaBlocks.HasAdrBlock)
                    {
                        #region Получаем характеристики счета
                        bill.PayerFio = Convert.ToString(goodreader["fio"]).Trim();
                        bill.LicSchet = nzpKvar.ToString(CultureInfo.InvariantCulture);
                        bill.Typek = goodreader["typek"].ToString().Trim() != "3" ? "жилого" : "нежилого";

                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT * ");
                        sql.Append(" FROM t_fkvar_prm ");
                        sql.Append(" WHERE nzp_kvar = " + nzpKvar);
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            if (reader.Read())
                            {
                                if (reader["count_gil"] != DBNull.Value)
                                    bill.CountGil = Convert.ToInt32(reader["count_gil"]);
                                if (reader["count_gilp"] != DBNull.Value)
                                    bill.CountRegisterGil = Convert.ToInt32(reader["count_gilp"]);
                                if (reader["count_gil_time"] != DBNull.Value)
                                    bill.CountArriveGil = Convert.ToInt32(reader["count_gil_time"]);
                                if (reader["count_gil_time_epd"] != DBNull.Value)
                                    bill.CountGilWithoutArrived = Convert.ToInt32(reader["count_gil_time_epd"]);
                                if (reader["pl_kvar"] != DBNull.Value)
                                    bill.FullSquare = Convert.ToDecimal(reader["pl_kvar"]);
                                if (reader["pl_kvar_gil"] != DBNull.Value)
                                    bill.LiveSquare = Convert.ToDecimal(reader["pl_kvar_gil"]);

                                if (reader["indecs"] != DBNull.Value)
                                    bill.Indecs = reader["indecs"].ToString().Trim();

                                if (reader["privat"] != DBNull.Value)
                                    bill.Ownflat = true;
                                if (reader["fio"] != DBNull.Value)
                                    if (reader["fio"].ToString().Trim() != "")
                                        bill.PayerFio = reader["fio"].ToString().Trim();
                                if (reader["pl_dom"] != DBNull.Value)
                                    bill.DomSquare = Convert.ToDecimal(reader["pl_dom"]);
                                if (reader["pl_mop"] != DBNull.Value)
                                    bill.MopSquare = Convert.ToDecimal(reader["pl_mop"]);
                                if (reader["count_domgil"] != DBNull.Value)
                                    bill.CountDomGil = Convert.ToInt32(reader["count_domgil"]);

                                if (reader["is_paspgil"] != DBNull.Value)
                                    isPasp = Convert.ToInt32(reader["is_paspgil"]) == 1;


                                /*if (reader["hasEldpu"] != DBNull.Value)
                                    bill.hasElDpu = System.Convert.ToInt32(reader["hasEldpu"]) > 0;
                                if (reader["hasHvsdpu"] != DBNull.Value)
                                    bill.hasHvsDpu = System.Convert.ToInt32(reader["hasHvsdpu"]) > 0;
                                if (reader["hasGvsdpu"] != DBNull.Value)
                                    bill.hasGvsDpu = System.Convert.ToInt32(reader["hasGvsdpu"]) > 0;
                                if (reader["hasGazdpu"] != DBNull.Value)
                                    bill.hasGazDpu = System.Convert.ToInt32(reader["hasGazdpu"]) > 0;
                                if (reader["hasOtopdpu"] != DBNull.Value)
                                    bill.hasOtopDpu = System.Convert.ToInt32(reader["hasOtopdpu"]) > 0;*/
                                if (reader["is_komm"] != DBNull.Value)
                                    bill.IsolateFlat = Convert.ToInt32(reader["is_komm"]) == 0;
                                if (bill.IsolateFlat)
                                {
                                    if (reader["otop_norm_i"] != DBNull.Value)
                                        bill.OtopNorm = Convert.ToDecimal(reader["otop_norm_i"]);
                                }
                                else
                                {
                                    if (reader["otop_norm_k"] != DBNull.Value)
                                        bill.OtopNorm = Convert.ToDecimal(reader["otop_norm_k"]);
                                }

                                if (reader["gvs_norm_gkal"] != DBNull.Value)
                                    bill.GvsNormGkal = Convert.ToDecimal(reader["gvs_norm_gkal"]);

                                if (reader["et"] != DBNull.Value)
                                    bill.Stage = reader["et"].ToString().Trim();

                                if (reader["open_vodozabor"] != DBNull.Value)
                                    bill.HasOpenVodozabor = Convert.ToInt32(reader["open_vodozabor"]) == 1;
                                else bill.HasOpenVodozabor = false;
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 33 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        StreamWriter sw2 = new StreamWriter(@"C:\temp\peopleggggg.txt", true);
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT nzp, val_prm, dat_s, dat_po FROM bill01_data.prm_2  ");
                        sql.Append(" where nzp_prm = 2074 AND is_actual <> 100 and dat_po >= current_date AND nzp in (SELECT nzp_dom from bill01_data.kvar where nzp_kvar =" + nzpKvar);
                        sql.Append(")");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            if (reader.Read())
                            {
                                if (reader["val_prm"] != DBNull.Value)
                                {
                                    sw.WriteLine(reader["val_prm"].ToString());
                                    bill.RashDpuPu = reader["val_prm"].ToString();
                                }

                            }
                            sw2.Close();
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch(Exception e)
                        {
                            sw2.WriteLine(e.ToString());
                            sw2.Close();
                            MonitorLog.WriteLog("Ошибка выборки 33 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }


                        bill.Town = Convert.ToString(goodreader["town"]).Trim();

                        bill.Rajon = Convert.ToString(goodreader["rajon"]).Trim();

                        bill.Ulica = Convert.ToString(goodreader["ulica"]).Trim();
                        if (bill.Ulica.Length > 2)
                        {
                            if (bill.Ulica.Substring(bill.Ulica.Length - 3, 3) == "/ -")
                            {
                                bill.Ulica = bill.Ulica.Substring(0, bill.Ulica.Length - 3).Trim();
                            }
                        }

                        bill.NumberDom = Convert.ToString(goodreader["ndom"]).Trim();
                        if (bill.NumberDom.Length > 0)
                        {
                            if (bill.NumberDom.Substring(bill.NumberDom.Length - 1, 1) == "-")
                            {
                                bill.NumberDom = bill.NumberDom.Substring(0, bill.NumberDom.Length - 1).Trim();
                            }
                        }

                        bill.NumberFlat = Convert.ToString(goodreader["nkvar"]).Trim();

                        if (bill.NumberFlat.Length > 0)
                        {
                            if (bill.NumberFlat.Substring(bill.NumberFlat.Length - 1, 1) == "-")
                            {
                                bill.NumberFlat = bill.NumberFlat.Substring(0, bill.NumberFlat.Length - 1).Trim();
                            }
                        }

                        bill.NumberRoom = Convert.ToString(goodreader["nkvar_n"]).Trim();
                        if (bill.NumberRoom.Length > 0)
                        {
                            if (bill.NumberRoom.Substring(bill.NumberRoom.Length - 1, 1) == "-")
                            {
                                bill.NumberRoom = bill.NumberRoom.Substring(0, bill.NumberRoom.Length - 1).Trim();
                            }
                        }

                        if (goodreader["pkod"] != DBNull.Value)
                        {
                            bill.Pkod = goodreader["pkod"].ToString().Length >= 13 ? goodreader["pkod"].ToString().Substring(0, 13) : goodreader["pkod"].ToString();
                        }
                        else
                        {
                            bill.Pkod = "0000000000000";
                        }



                        bill.Geu = goodreader["geu"] != DBNull.Value ? Convert.ToString(goodreader["geu"]).Trim() : "";
                        bill.Ud = goodreader["geu"] != DBNull.Value ? Convert.ToString(goodreader["uch"]).Trim() : "";
                        bill.NzpArea = nzpArea;
                        bill.NzpGeu = nzpGeu;

                        #endregion
                    }

                    #region Загрузка начислений

                    bool resLoadNach;
                    switch (_finder.idFaktura)
                    {
                        case 100:
                        case 196:
                        case 101:
                            {
                                resLoadNach = GetNachSamara(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 102:
                            {
                                resLoadNach = GetNachSaha(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }

                        case 103:
                            {
                                resLoadNach = GetNachStd(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }

                        case 104:
                            {
                                resLoadNach = GetNachGubkin(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 105:
                            {
                                resLoadNach = GetNachKapremont(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 106:
                        case 1006:
                            {
                                resLoadNach = GetNachAstrahan(tableCharge,
                                    nzpKvar, ref bill);
                                break;
                            }
                        case 107:
                        case 122:
                        case 108:
                            {
                                //resLoadNach = GetNachTula(tableCharge,
                          //nzpKvar, ref bill);
                                resLoadNach = GetNachSamara(tableCharge,
                         nzpKvar, ref bill);
                                break;
                            }
                        case 188:
                            {
                                resLoadNach = GetNachKaluga(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 109:
                        case 110:
                            {
                                resLoadNach = GetNachStd(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 111:
                            {
                                resLoadNach = GetNachStd(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 112:
                            {
                                resLoadNach = GetNachSamara(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 113:
                            {
                                resLoadNach = GetNachSamaraSupp(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 114:
                        case 115:
                            {
                                resLoadNach = GetNachStd(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 116:
                            {
                                resLoadNach = GetNachKapremont(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        case 117:
                        case 118:
                        case 119:
                        case 120:
                            {
                                resLoadNach = GetNachSamara(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        //case 122:
                        case 123:
                            {
                                resLoadNach = GetNachTula(tableCharge,
                          nzpKvar, ref bill);
                                break;
                            }
                        default: resLoadNach = true; break;
                    }

                    if (resLoadNach != true)
                    {
                        MonitorLog.WriteLog("Ошибка выборки 34 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                        _conDb.Close();
                        return null;
                    }
                    #endregion


                    if (bill.FakturaBlocks.HasNormblock)
                    {
                        #region Загружаем нормативы по услугам

                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT nzp_serv, rashod_norm, gil");
                        sql.Append(" FROM " + tableRashNorm + " a ");
                        sql.Append(" WHERE a.nzp_kvar=" + nzpKvar);
                        sql.Append(" AND a.stek = 3 ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                foreach (BaseServ t in bill.ListServ)
                                {
                                    if (t.Serv.NzpServ == Convert.ToInt32(reader["nzp_serv"]))
                                    {

                                        if ((t.Serv.NzpServ == 6) ||
                                            (t.Serv.NzpServ == 7) ||
                                            (t.Serv.NzpServ == 9))
                                        {
                                            if (Convert.ToInt32(reader["gil"]) > 0)
                                            {
                                                t.Serv.Norma = Convert.ToDecimal(reader["rashod_norm"]) /
                                                               Convert.ToInt32(reader["gil"]);
                                            }
                                            if (t.Serv.NzpServ == 7) bill.KanNormCalc = t.Serv.Norma;
                                        }

                                    }
                                }
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 35 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }


                        #endregion
                    }
                    if (bill.FakturaBlocks.HasServiceVolumeBlock)
                    {
                        #region Загружаем расходы по ЛС
                        decimal sumKanNorm = 0;
                        bill.HvsNorm = 0;
                        bill.GvsNorm = 0;
                        var dat = new DateTime(_finder.year_, _finder.month_, 1);                      
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT s.service_name as service, a.nzp_serv, ");
                        //sql.Append("        a.rashod-(case when a.dop87<0 then a.dop87 else 0 end) as rashod, ");//Полный расход без учета ОДН
                        sql.Append("        (dlt_reval+valm) as rashod, ");//Полный расход без учета ОДН
                        sql.Append("        a.rashod_norm , ");//Расход по нормативу
                        sql.Append("        a.dop87  as odn, ");//ОДН
                        sql.Append("        a.is_device,squ, ");//0-норматив 1 по счетчику 9 по среднему
                        sql.Append("        a.gil, a.valm, ");
                        sql.Append("        a.rashod as rashod_all, a.rsh1, a.rsh2,");
                        sql.Append("        a.rash_norm_one, a.tarif ");//Количество проживающих
                        sql.Append(" FROM " + tableRashNorm + " a, " + baseKernel + "services s ");
                        sql.Append(" WHERE a.nzp_serv=s.nzp_serv AND a.nzp_kvar=" + nzpKvar + " and a.nzp_serv<>500");
                        sql.Append(" AND a.stek = 3");
                        sql.Append(" AND a.dat_s >= '" + dat.ToShortDateString() + "' ");
                        sql.Append(" AND a.dat_po < '" + dat.AddMonths(1).ToShortDateString() + "' ");
                        sql.Append(" ORDER BY 1  ");
                        int steps = 0;
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {

                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var servVolume = new ServVolume
                                {
                                    ServiceName = reader["service"].ToString(),
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString())
                                };


                                decimal rashod = reader["rashod"] != null
                                    ? Decimal.Parse(reader["rashod"].ToString())
                                    : 0;
                                steps = 1;
                                decimal rashodOdn = reader["odn"] != null
                                    ? Decimal.Parse(reader["odn"].ToString())
                                    : 0;
                                steps = 2;
                                decimal rashNormOne =
                                    reader["rash_norm_one"] != null
                                        ? Decimal.Parse(reader["rash_norm_one"].ToString())
                                        : 0;
                                steps = 3;
                                decimal rsh2 = reader["rsh2"] != null
                                    ? Decimal.Parse(reader["rsh2"].ToString())
                                    : 0;
                                steps = 4;
                                decimal rashodAll = reader["rashod_all"] != null
                                    ? Decimal.Parse(reader["rashod_all"].ToString())
                                    : 0;
                                steps = 5;
                                decimal tarif = reader["tarif"] != null
                                    ? Decimal.Parse(reader["tarif"].ToString())
                                    : 0;
                                steps = 6;

                                int gil = reader["gil"] != null
                                    ? (int)Decimal.Parse(reader["gil"].ToString())
                                    : 0;
                                steps = 7;
                                decimal squ = reader["squ"] != null
                                    ? Decimal.Parse(reader["squ"].ToString())
                                    : 0;
                                steps = 8;
                                decimal rashodNorm = reader["rashod_norm"] != null
                                    ? Decimal.Parse(reader["rashod_norm"].ToString())
                                    : 0;
                                steps = 9;
                                int isDevice = reader["is_device"] != null
                                    ? (int)Decimal.Parse(reader["is_device"].ToString())
                                    : 0;
                                steps = 10;
                                if ((tarif <= 0.0001m) && (rashodOdn == 0m))
                                {
                                    rashod = 0;
                                    rashodOdn = 0;
                                }

                                if ((servVolume.NzpServ == 6) ||
                                         (servVolume.NzpServ == 7) ||
                                         (servVolume.NzpServ == 9) ||
                                         (servVolume.NzpServ == 25) ||
                                         (servVolume.NzpServ == 210) ||
                                         (servVolume.NzpServ == 10) ||
                                         (servVolume.NzpServ == 14)
                                    )
                                {
                                    if (gil > 0)
                                    {
                                        servVolume.NormaVolume = rashodNorm / gil;
                                    }
                                    else
                                    {
                                        servVolume.NormaVolume = rashNormOne;
                                    }
                                }
                                servVolume.IsPu = isDevice;


                                if ((rashodAll <= 0m) & (rashod > 0m))
                                {
                                    if ((rashodOdn < 0m) & (rashodOdn + rashod < -0.00001m))
                                    {
                                        rashodOdn = -rashod;
                                    }
                                }
                                else
                                    if ((rashodAll <= 0m) & (rashod <= 0m) & (rashodOdn < 0m))
                                    {
                                        rashodOdn = 0;
                                    }


                                servVolume.NormaFullVolume = rashod;
                                if (servVolume.IsPu != 0)
                                {
                                    servVolume.PUVolume = rashod;
                                    servVolume.OdnFlatPuVolume = rashodOdn;

                                }
                                else
                                {
                                    servVolume.OdnFlatNormVolume = rashodOdn;
                                }


                                if ((servVolume.NzpServ == 6) || (servVolume.NzpServ == 9))
                                {
                                    sumKanNorm += servVolume.NormaVolume;
                                }
                                if (servVolume.NzpServ == 6) bill.HvsNorm = servVolume.NormaVolume;
                                if (servVolume.NzpServ == 9)
                                {
                                    bill.GvsNorm = servVolume.NormaVolume;
                                    if (_finder.idFaktura == 112 || _finder.idFaktura == 117)
                                    {
                                        if (rsh2 > 0m)
                                        {
                                            bill.GvsNorm = rashNormOne / rsh2;
                                        }
                                    }
                                }

                                if ((servVolume.NzpServ == 8) & (rashod == 0))
                                    servVolume.NormaFullVolume = rsh2;


                                bill.CalcSquare = squ;
                                bill.FullSquare = (bill.CalcSquare < bill.FullSquare
                                    ? bill.FullSquare
                                    : bill.CalcSquare);
                                bill.AddVolume(servVolume);

                            }
               
                            reader.Close();
               
                            reader.Dispose();
                      
                            cmd1.Dispose();
                 
                        
                        }
                        catch (Exception ex)
                        {

                            MonitorLog.WriteLog("Ошибка выборки 36 |" + sql + " steps = " + steps + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }


                        if (sumKanNorm > 0.001m && _finder.idFaktura != 1077 && _finder.idFaktura != 123)
                        {
                            var servVolume = new ServVolume
                            {
                                NormaVolume = sumKanNorm,
                                PUVolume = 0,
                                OdnFlatNormVolume = 0,
                                OdnFlatPuVolume = 0,
                                ServiceName = "Водоотведение",
                                NzpServ = 7
                            };


                            bill.AddVolume(servVolume);
                        }

                        #endregion
                    }


                    if (bill.FakturaBlocks.HasRTCountersBlock)
                    {
                        #region Загружаем показания счетчиков по ЛС
                        DateTime firstDayNextMonth = Convert.ToDateTime("01." + _finder.month_ + "." +
                            _finder.year_).AddMonths(1);
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT service_name as service, a.nzp_serv, sc.cnt_stage,");
                        sql.Append(" a.nzp_cnttype, sc.formula, a.num_cnt, a.dat_uchet, a.val_cnt, ");
                        sql.Append(" service_small, name_y, cs.dat_prov, cs.dat_provnext ");
                        sql.Append(" FROM " + baseKernel + "services s, ");
                        sql.Append(baseData + "counters_spis cs,");
                        sql.Append(baseKernel + "s_counttypes sc, ");
                        sql.Append(baseData + "counters a left join ");
                        sql.Append("  " + baseData + "prm_17 p on  p.is_actual=1 ");
                        sql.Append("                  AND p.dat_s<= " + DBManager.sCurDate);
                        sql.Append("                    AND p.dat_po>= " + DBManager.sCurDate);
                        sql.Append("                    AND a.nzp_counter=p.nzp ");
                        sql.Append("                    AND p.nzp_prm=974 ");
                        sql.Append("  left join " + baseKernel + "res_y v on p.val_prm" + DBManager.sConvToNum + "=v.nzp_y ");
                        sql.Append("                              AND v.nzp_res=9990  ");
                        sql.Append("          Where a.nzp_serv=s.nzp_serv ");
                        sql.Append("                 AND cs.nzp_counter = a.nzp_counter");
                        sql.Append("                 AND a.nzp_cnttype = sc.nzp_cnttype ");
                        sql.Append("                 AND a.dat_close is null ");
                        sql.Append("                 AND a.is_actual = 1 ");
                        sql.Append("                 AND a.nzp_kvar = " + nzpKvar);
                        sql.Append("                 AND a.dat_uchet>=Date('01.01." + (bill.Year - 1) + "') ");
                        sql.Append("                 AND a.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
                        sql.Append("                 AND a.dat_uchet=( ");
                        sql.Append("                        SELECT max(dat_uchet) ");
                        sql.Append("                        FROM " + baseData + "counters c ");
                        sql.Append("              Where a.nzp_counter = c.nzp_counter ");
                        sql.Append("                AND c.dat_uchet <= '" + firstDayNextMonth.ToShortDateString() + "'");
                        sql.Append("                AND c.is_actual = 1  ) ");
                        sql.Append("            AND 0=( ");
                        sql.Append("              SELECT count(*) FROM " + baseData + "counters_spis d                                               ");
                        sql.Append("              Where a.nzp_counter=d.nzp_counter                                                                     ");
                        sql.Append("                     AND d.is_actual = 1 ");
                        sql.Append("                     AND d.dat_close is not null)");

                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var counters = new Counters
                                {
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString()),
                                    ServiceName = reader["service"].ToString(),
                                    Value = Decimal.Parse(reader["val_cnt"].ToString()),
                                    DatUchet = (DateTime)reader["dat_uchet"]
                                };
                                counters.ValuePred = counters.Value;
                                counters.Place = reader["name_y"] != DBNull.Value ? reader["name_y"].ToString() : "";
                                counters.NumCounters = reader["num_cnt"].ToString().Trim();
                                counters.DatUchetPred = counters.DatUchet;
                                counters.CntStage = Int32.Parse(reader["cnt_stage"].ToString());
                                decimal mnogitel;
                                counters.Formula = Decimal.TryParse(reader["formula"].ToString(), out mnogitel) ? mnogitel : 0;

                                if (reader["dat_provnext"] != DBNull.Value)
                                    counters.DatProv = ((DateTime)reader["dat_provnext"]).ToShortDateString();
                                else if (reader["dat_prov"] != DBNull.Value)
                                {
                                    counters.DatProv = ((DateTime)reader["dat_prov"]).ToShortDateString();
                                }
                                bill.AddCounters(counters);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch (Exception ex)
                        {

                            MonitorLog.WriteLog("Ошибка выборки 1 |" + sql + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }



                        #endregion
                    }

                    if (bill.FakturaBlocks.HasRTCountersDoubleBlock)
                    {
                        #region Загружаем показания счетчиков по ЛС
                        DateTime firstDayNextMonth = Convert.ToDateTime("01." + _finder.month_ + "." +
                            _finder.year_).AddMonths(1);

                        sql.Remove(0, sql.Length);
                        sql.Append(" Select s.ordering, s.service_name as service, a.nzp_serv, a.num_cnt, ");
                        sql.Append("        a.nzp_cnttype, a.dat_uchet, a.val_cnt, ");
                        sql.Append("        b.num_cnt as num_cnt2, b.dat_uchet as dat_uchet2, b.val_cnt as val_cnt2, ");
                        sql.Append("        sc.cnt_stage, formula, name_y, cs.dat_prov, cs.dat_provnext  ");
                        sql.Append(" From " + baseKernel + "services s, ");
                        sql.Append("                  " + baseKernel + "s_counttypes sc, ");
                        sql.Append("                 " + baseData + "counters a left join ");
                        sql.Append("                 " + baseData + "counters b on a.nzp_counter=b.nzp_counter ");
                        sql.Append("                 AND b.dat_uchet<a.dat_uchet ");
                        sql.Append("                 AND a.is_actual=b.is_actual ");
                        sql.Append("                 AND b.dat_uchet=( ");
                        sql.Append("                        Select max(dat_uchet) ");
                        sql.Append("                        From " + baseData + "counters c ");
                        sql.Append("                        WHERE a.nzp_counter=c.nzp_counter ");
                        sql.Append("                               AND c.dat_uchet<a.dat_uchet ");
                        sql.Append("                               AND c.is_actual = 1 ),  ");
                        sql.Append(baseData + "counters_spis cs ");
                        sql.Append("                  left join " + baseData + "prm_17 p ");
                        sql.Append("                  on cs.nzp_counter=p.nzp  ");
                        sql.Append("                        AND p.nzp_prm=974");
                        sql.Append("                        AND p.is_actual=1 ");
                        sql.Append("                        AND p.dat_s<=" + DBManager.sCurDate);
                        sql.Append("                        AND p.dat_po>=" + DBManager.sCurDate);
                        sql.Append("                  left join " + baseKernel + "res_y v ");
                        sql.Append("                  on p.val_prm" + DBManager.sConvToNum + "=v.nzp_y ");
                        sql.Append("                              AND v.nzp_res=9990  ");
                        sql.Append("          WHERE a.nzp_serv=s.nzp_serv ");
                        sql.Append("                 AND a.nzp_counter=cs.nzp_counter ");
                        sql.Append("                 AND a.nzp_cnttype=sc.nzp_cnttype ");
                        sql.Append("                 AND a.dat_close is null ");
                        sql.Append("                 AND a.is_actual=1 ");
                        sql.Append("                 AND a.nzp_kvar=" + nzpKvar);
                        sql.Append("                 AND a.dat_uchet>=Date('01.01." + (bill.Year - 1) + "') ");
                        sql.Append("                 AND a.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
                        sql.Append("                 AND a.dat_uchet=( ");
                        sql.Append("              SELECT max(dat_uchet) From " + baseData + "counters c ");
                        sql.Append("              WHERE a.nzp_counter=c.nzp_counter ");
                        sql.Append("                AND c.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
                        sql.Append("                AND c.is_actual =  1) ");
                        sql.Append("            AND 0=( ");
                        sql.Append("              Select count(*) From " + baseData + "counters_spis d                                               ");
                        sql.Append("              WHERE a.nzp_counter=d.nzp_counter                                                                    ");
                        sql.Append("                AND d.is_actual = 1  ");
                        sql.Append("                AND d.dat_close is not null) ");
                        sql.Append("  ORDER BY ordering,2,4,5 ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var counters = new Counters
                                {
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString()),
                                    ServiceName = reader["service"].ToString(),
                                    Place = reader["name_y"] != DBNull.Value ? reader["name_y"].ToString() : "",
                                    Value = Decimal.Parse(reader["val_cnt"].ToString()),
                                    DatUchet = (DateTime)reader["dat_uchet"],
                                    ValuePred =
                                        reader["val_cnt2"] != DBNull.Value
                                            ? Decimal.Parse(reader["val_cnt2"].ToString())
                                            : 0,
                                    NumCounters = reader["num_cnt"].ToString().Trim(),
                                    DatUchetPred =
                                        reader["dat_uchet2"] != DBNull.Value
                                            ? (DateTime)reader["dat_uchet2"]
                                            : DateTime.Parse("01.01.1900"),
                                    CntStage = Int32.Parse(reader["cnt_stage"].ToString())
                                };
                                decimal mnogitel;
                                if (reader["formula"] != DBNull.Value)
                                    counters.Formula = Decimal.TryParse(reader["formula"].ToString(), out mnogitel) ? mnogitel : 0;
                                else counters.Formula = 1;

                                if (reader["dat_provnext"] != DBNull.Value)
                                    counters.DatProv = ((DateTime)reader["dat_provnext"]).ToShortDateString();
                                else if (reader["dat_prov"] != DBNull.Value)
                                {
                                    counters.DatProv = ((DateTime)reader["dat_prov"]).ToShortDateString();
                                }
                                bill.AddCounters(counters);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch (Exception ex)
                        {

                            MonitorLog.WriteLog("Ошибка выборки 2 |" + sql + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        #endregion
                    }


                    //if (bill.FakturaBlocks.HasRTCountersDoubleDomBlock)
                    if (1==1)
                    {
                        #region Загружаем показания счетчиков по Дому
                        DateTime firstDayNextMonth = Convert.ToDateTime("01." + _finder.month_ + "." +
                            _finder.year_).AddMonths(1);

                        sql.Remove(0, sql.Length);
                        sql.Append(" Select s.ordering, service_name as service, a.nzp_serv, a.num_cnt, ");
                        sql.Append(" a.nzp_cnttype, a.dat_uchet, a.val_cnt, ");
                        sql.Append(" b.num_cnt as num_cnt2, b.dat_uchet as dat_uchet2, ");
                        sql.Append(" b.val_cnt as val_cnt2, sc.cnt_stage, formula, cs.dat_prov, ");
                        sql.Append(" cs.dat_provnext, a.is_gkal, sm.measure  ");
                        sql.Append(" From " + baseKernel + "services s, ");
                        sql.Append("                  " + baseKernel + "s_counttypes sc,  ");
                        sql.Append(baseData + "counters_spis cs,  ");
                        sql.Append(baseKernel + "s_counts st,");
                        sql.Append(baseKernel + "s_measure sm, ");
                        sql.Append("                 " + baseData + "counters_dom a left join ");
                        sql.Append("                 " + baseData + "counters_dom b on  ");
                        sql.Append("                    a.nzp_counter=b.nzp_counter ");
                        sql.Append("                AND b.dat_uchet<a.dat_uchet ");
                        sql.Append("                AND a.is_actual=b.is_actual ");
                        sql.Append("                AND b.dat_uchet=( ");
                        sql.Append("              Select max(dat_uchet) From " + baseData + "counters_dom c ");
                        sql.Append("              Where a.nzp_counter=c.nzp_counter ");
                        sql.Append("                AND c.dat_uchet<a.dat_uchet ");
                        sql.Append("                AND c.is_actual = 1) ");
                        sql.Append("          Where a.nzp_serv=s.nzp_serv AND a.nzp_counter=cs.nzp_counter");
                        sql.Append("            AND cs.nzp_cnt=st.nzp_cnt AND st.nzp_measure=sm.nzp_measure ");
                        sql.Append("            AND a.nzp_cnttype=sc.nzp_cnttype ");
                        sql.Append("            AND a.dat_close is null ");
                        sql.Append("            AND a.is_actual=1 ");
                        sql.Append("            AND a.nzp_dom=" + nzpDom);
                        sql.Append("            AND a.dat_uchet>=Date('01.01." + (bill.Year - 1) + "') ");
                        sql.Append("            AND a.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
                        sql.Append("            AND a.dat_uchet=( ");
                        sql.Append("              Select max(dat_uchet) From " + baseData + "counters_dom c ");
                        sql.Append("              Where a.nzp_counter=c.nzp_counter ");
                        sql.Append("                AND c.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
                        sql.Append("                AND c.is_actual = 1 ) ");
                        sql.Append("            AND 0=( ");
                        sql.Append("              Select count(*) From " + baseData + "counters_spis d                                          ");
                        sql.Append("              Where a.nzp_counter=d.nzp_counter                                                                     ");
                        sql.Append("                AND d.is_actual = 1 AND d.dat_close is not null)                                      ");
                        sql.Append("  ORDER BY ordering,2,4,5 ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var counters = new Counters
                                {
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString()),
                                    Measure = reader["measure"].ToString().Trim(),
                                    ServiceName = reader["service"].ToString().Trim(),
                                    Value = Decimal.Parse(reader["val_cnt"].ToString()),
                                    DatUchet = (DateTime)reader["dat_uchet"],
                                    ValuePred =
                                        reader["val_cnt2"] != DBNull.Value
                                            ? Decimal.Parse(reader["val_cnt2"].ToString())
                                            : 0,
                                    NumCounters = reader["num_cnt"].ToString().Trim(),
                                    DatUchetPred =
                                        reader["dat_uchet2"] != DBNull.Value
                                            ? (DateTime)reader["dat_uchet2"]
                                            : DateTime.Parse("01.01.1900"),
                                    CntStage = Int32.Parse(reader["cnt_stage"].ToString())
                                };
                                decimal mnogitel;
                                if (reader["formula"] != DBNull.Value)
                                    counters.Formula = Decimal.TryParse(reader["formula"].ToString(), out mnogitel) ? mnogitel : 0;
                                else
                                    counters.Formula = 1;
                                if (reader["dat_provnext"] != DBNull.Value)
                                    counters.DatProv = ((DateTime)reader["dat_provnext"]).ToShortDateString();
                                else if (reader["dat_prov"] != DBNull.Value)
                                {
                                    counters.DatProv = ((DateTime)reader["dat_prov"]).ToShortDateString();
                                }
                                counters.IsGkal = false;

                                if (reader["is_gkal"] != DBNull.Value)
                                    if (reader["is_gkal"].ToString().Trim() == "1")
                                        counters.IsGkal = true;
                                bill.AddDomCounters(counters);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch (Exception ex)
                        {

                            MonitorLog.WriteLog("Ошибка выборки 3 |" + sql + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }

                        #endregion
                    }


                    if (bill.FakturaBlocks.HasCountersBlock)
                    {
                        #region Загружаем показания счетчиков по ЛС
                        DateTime firstDayNextMonth = Convert.ToDateTime("01." + _finder.month_ + "." +
                            _finder.year_).AddMonths(1);

                        #region старый способ
                        //sql.Remove(0, sql.Length);
                        //sql.Append(" SELECT service_name as service, a.nzp_serv, sc.cnt_stage,");
                        //sql.Append(" a.nzp_cnttype, sc.formula, a.num_cnt, a.dat_uchet, a.val_cnt, ");
                        //sql.Append(" service_small, name_y, cs.dat_prov, cs.dat_provnext ");
                        //sql.Append(" FROM " + baseKernel + "services s, ");
                        //sql.Append(baseData + "counters_spis cs,");
                        //sql.Append(baseKernel + "s_counttypes sc, ");
                        //sql.Append(baseData + "counters a left join ");
                        //sql.Append("  " + baseData + "prm_17 p on  p.is_actual=1 ");
                        //sql.Append("                  AND p.dat_s<= " + DBManager.sCurDate);
                        //sql.Append("                    AND p.dat_po>= " + DBManager.sCurDate);
                        //sql.Append("                    AND a.nzp_counter=p.nzp ");
                        //sql.Append("                    AND p.nzp_prm=974 ");
                        //sql.Append("  left join " + baseKernel + "res_y v on p.val_prm" + DBManager.sConvToNum + "=v.nzp_y ");
                        //sql.Append("                              AND v.nzp_res=9990  ");
                        //sql.Append("          Where a.nzp_serv=s.nzp_serv ");
                        //sql.Append("                 AND cs.nzp_counter = a.nzp_counter");
                        //sql.Append("                 AND a.nzp_cnttype = sc.nzp_cnttype ");
                        //sql.Append("                 AND a.dat_close is null ");
                        //sql.Append("                 AND a.is_actual = 1 ");
                        //sql.Append("                 AND a.nzp_kvar = " + nzpKvar);
                        //sql.Append("                 AND a.dat_uchet>=Date('01.01." + (bill.Year - 1) + "') ");
                        //sql.Append("                 AND a.dat_uchet<='" + firstDayNextMonth.ToShortDateString() + "'");
                        //sql.Append("                 AND a.dat_uchet=( ");
                        //sql.Append("                        SELECT max(dat_uchet) ");
                        //sql.Append("                        FROM " + baseData + "counters c ");
                        //sql.Append("              Where a.nzp_counter = c.nzp_counter ");
                        //sql.Append("                AND c.dat_uchet <= '" + firstDayNextMonth.ToShortDateString() + "'");
                        //sql.Append("                AND c.is_actual = 1  ) ");
                        //sql.Append("            AND 0=( ");
                        //sql.Append("              SELECT count(*) FROM " + baseData + "counters_spis d                                               ");
                        //sql.Append("              Where a.nzp_counter=d.nzp_counter                                                                     ");
                        //sql.Append("                     AND d.is_actual = 1 ");
                        //sql.Append("                     AND d.dat_close is not null)");
                        #endregion

                        string sql1 = @"drop table t_serv";
                        DBManager.ExecSQL(_conDb, sql1, false);

                        sql1 =     @"    Create temp table t_serv(service char(100), nzp_serv integer, cnt_stage integer, nzp_cnttype integer, formula char(100), num_cnt char(100), 
                                        dat_uchet date, val_cnt decimal(13,2), service_small char(20), name_y char(255), dat_prov date, dat_provnext date, nzp_counter integer, dat_close date, 
                                        is_actual integer, dat_uchet2 date)";
                        DBManager.ExecSQL(_conDb, sql1, false);

                        sql1 = @"INSERT INTO t_serv SELECT service_name AS service,        cs.nzp_serv,        sc.cnt_stage,        cs.nzp_cnttype,        sc.formula,        cs.num_cnt,        
a.dat_uchet,        a.val_cnt,        service_small,        name_y,        cs.dat_prov,        cs.dat_provnext, a.nzp_counter, a.dat_close, a.is_actual, a.dat_uchet    FROM  " + baseData + 
 "counters_spis cs          left outer join  " + baseData + "counters a  on   cs.nzp_counter = a.nzp_counter     LEFT OUTER JOIN  " + baseKernel + 
 "s_counttypes sc on   a.nzp_cnttype = sc.nzp_cnttype     LEFT OUTER JOIN  " + baseKernel + "services s on s.nzp_serv=cs.nzp_serv    LEFT JOIN " + baseData 
 + "prm_17 p ON p.is_actual=1        AND p.dat_s<= current_date       AND p.dat_po>= current_date       AND a.nzp_counter=p.nzp and trim(p.val_prm)<>''       AND p.nzp_prm=974        LEFT JOIN " 
 + baseKernel + "res_y v ON p.val_prm::numeric=v.nzp_y        AND v.nzp_res=9990        WHERE  0=        (SELECT count(*)        FROM " + baseData + 
 "counters_spis d        WHERE cs.nzp_counter=d.nzp_counter        AND d.is_actual = 1        AND d.dat_close IS NOT NULL AND d.dat_close <= current_date) and  cs.nzp =" +  nzpKvar + 
 " UNION ALL        SELECT service_name AS service,        cs1.nzp_serv,        sc.cnt_stage,        cs1.nzp_cnttype,        sc.formula,        cs1.num_cnt,       cs.dat_uchet,       cs.val_cnt,     " +
 "   service_small,     name_y,        cs1.dat_prov,         cs1.dat_provnext,        cs.nzp_counter,        a.dat_close,        cs.is_actual,        cs.dat_uchet    FROM  " + baseData 
 + "counters_link cl     INNER JOIN " + baseData + "counters_group cs on cs.nzp_counter = cl.nzp_counter     INNER JOIN " + baseData + 
 "counters_spis cs1 on cs1.nzp_counter = cs.nzp_counter     left outer join  " + baseData + "counters a  on   cs.nzp_counter = a.nzp_counter     LEFT OUTER JOIN  " + baseKernel + 
 "s_counttypes sc on   a.nzp_cnttype = sc.nzp_cnttype   LEFT OUTER JOIN  " + baseKernel + "services s on s.nzp_serv=cs1.nzp_serv    LEFT JOIN " + baseData + 
 "prm_17 p ON p.is_actual=1        AND p.dat_s<= current_date       AND p.dat_po>= current_date    AND a.nzp_counter=p.nzp and trim(p.val_prm)<>''       AND p.nzp_prm=974        LEFT JOIN " 
 + baseKernel + "res_y v ON p.val_prm::numeric=v.nzp_y        AND v.nzp_res=9990        WHERE  0=      (SELECT count(*)        FROM " + baseData 
 + "counters_spis d        WHERE cs.nzp_counter=d.nzp_counter        AND d.is_actual = 1        AND d.dat_close IS NOT NULL AND d.dat_close <= current_date) and   cl.nzp_kvar = "
 + nzpKvar;
                        DBManager.ExecSQL(_conDb, sql1, false);
                        sql1 =      "       SELECT service, nzp_serv, cnt_stage, nzp_cnttype, formula, num_cnt, dat_uchet, val_cnt, service_small, name_y, dat_prov, dat_provnext " +
                                    "       FROM t_serv t " +
                                    "       WHERE dat_close IS NULL AND is_actual = 1      " +
                                    "       AND (dat_uchet2=     (SELECT max(dat_uchet)   FROM " + baseData + "counters c  WHERE t.nzp_counter = c.nzp_counter " +
                                    "       AND c.dat_uchet <= '" + firstDayNextMonth.ToShortDateString() + "' " +
                                    "       AND c.is_actual = 1) OR dat_uchet2=     (SELECT max(dat_uchet)   FROM " + baseData + 
                                    " counters_group c  WHERE t.nzp_counter = c.nzp_counter AND c.dat_uchet <= '"
                                    + firstDayNextMonth.ToShortDateString() + "' " + " AND c.is_actual = 1)) AND dat_uchet2>=Date('01.01." + (bill.Year - 1) + "')   AND dat_uchet2<='" + firstDayNextMonth.ToShortDateString() + "' ";


                        //StreamWriter sw2 = new StreamWriter(@"C:/Temp/error2.txt", false);
                        //sw2.WriteLine("1");
                        //sw2.WriteLine(_conDb.ConnectionTimeout);
                        cmd1 = DBManager.newDbCommand(sql1, _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            //sw2.WriteLine("2");
                            while (reader.Read())
                            {
                                var counters = new Counters
                                {
                                    NzpServ = reader["nzp_serv"] != DBNull.Value ? Int32.Parse(reader["nzp_serv"].ToString()) : 0,
                                    ServiceName = reader["service"] != DBNull.Value ? reader["service"].ToString() : "",
                                    Value = reader["val_cnt"] != DBNull.Value ? Decimal.Parse(reader["val_cnt"].ToString()) : 0,
                                    DatUchet = reader["dat_uchet"] != DBNull.Value ? (DateTime)reader["dat_uchet"] : DateTime.MinValue
                                };
                                counters.ValuePred = counters.Value;
                                counters.Place = reader["name_y"] != DBNull.Value ? reader["name_y"].ToString() : "";
                                counters.NumCounters = reader["num_cnt"] != DBNull.Value ? reader["num_cnt"].ToString().Trim() : "";
                                counters.DatUchetPred = counters.DatUchet;
                                counters.CntStage = reader["cnt_stage"] != DBNull.Value ? Int32.Parse(reader["cnt_stage"].ToString()) : 0;
                                decimal mnogitel;
                                counters.Formula = Decimal.TryParse(reader["formula"] != DBNull.Value ? reader["formula"].ToString() : "0", out mnogitel) ? mnogitel : 0;

                                if (reader["dat_provnext"] != DBNull.Value)
                                    counters.DatProv = ((DateTime)reader["dat_provnext"]).ToShortDateString();
                                else if (reader["dat_prov"] != DBNull.Value)
                                {
                                    counters.DatProv = ((DateTime)reader["dat_prov"]).ToShortDateString();
                                }
                                bill.AddCounters(counters);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                            //sw2.Close();
                        }
                        catch (Exception ex)
                        {
                            //sw2.Close();
                            MonitorLog.WriteLog("Ошибка выборки | 4" + sql1 + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }



                        #endregion
                    }

                    if (bill.FakturaBlocks.HasCountersDoubleBlock)
                    {
                        #region Загружаем показания счетчиков по ЛС

                        sql.Remove(0, sql.Length);
                        sql.Append(" Select s.ordering, s.service_name as service, cs.nzp_serv, cs.num_cnt, " +
                                   "        cs.nzp_cnttype, d.dat_s, d.val_s, d.dat_po, d.val_po, " +
                                   "        sc.cnt_stage, sc.mmnog as formula, v.name_y, cs.dat_prov, cs.dat_provnext  " +
                                   " FROM " + baseKernel + "services s, " +
                                   "        " + baseKernel + "s_counttypes sc, " +
                                   "        " + baseData + "counters_spis cs  " +
                                   "        left join " + baseData + "prm_17 p on  " +
                                   "                    p.is_actual=1 AND p.dat_s<=" + DBManager.sCurDate +
                                   "                    AND p.dat_po>=" + DBManager.sCurDate +
                                   "                           AND cs.nzp_counter=p.nzp AND p.nzp_prm=974 " +
                                   "         left join " + baseKernel + "res_y v on " + DBManager.sNvlWord + "(p.val_prm,'0')" + DBManager.sConvToNum + "=v.nzp_y " +
                                   "                                        AND v.nzp_res=9990 " +
                                   "         left join " + tableCounters + " d on cs.nzp_counter=d.nzp_counter " +
                                   "                                        AND d.nzp_type=3 and d.stek=1 " +
                                   " Where cs.nzp_serv=s.nzp_serv " +
                                   "        AND sc.nzp_cnttype=cs.nzp_cnttype " +
                                   "        AND cs.dat_close is null " +
                                   "        AND cs.nzp = " + nzpKvar +
                                   "        AND cs.nzp_type=3 " +
                                   "  ORDER BY ordering,2,4,5 ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var counters = new Counters
                                {
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString()),
                                    ServiceName = reader["service"].ToString(),
                                    Place = reader["name_y"] != DBNull.Value ? reader["name_y"].ToString().Trim() : "",
                                    Value =
                                        reader["val_po"] != DBNull.Value
                                            ? Decimal.Parse(reader["val_po"].ToString())
                                            : 0,
                                    DatUchet =
                                        reader["dat_s"] != DBNull.Value
                                            ? (DateTime)reader["dat_po"]
                                            : DateTime.Parse("01.01.1900"),
                                    ValuePred =
                                        reader["val_s"] != DBNull.Value ? Decimal.Parse(reader["val_s"].ToString()) : 0,
                                    NumCounters = reader["num_cnt"].ToString().Trim(),
                                    DatUchetPred =
                                        reader["dat_s"] != DBNull.Value
                                            ? (DateTime)reader["dat_s"]
                                            : DateTime.Parse("01.01.1900"),
                                    CntStage = Int32.Parse(reader["cnt_stage"].ToString()),
                                    Formula =
                                        reader["formula"] != DBNull.Value
                                            ? Decimal.Parse(reader["formula"].ToString())
                                            : 1,
                                    DatProv =
                                        reader["dat_provnext"] != DBNull.Value
                                            ? ((DateTime)reader["dat_provnext"]).ToShortDateString()
                                            : ""
                                };
                                if (counters.DatProv == "")
                                    counters.DatProv = reader["dat_prov"] != DBNull.Value ? ((DateTime)reader["dat_prov"]).ToShortDateString() : "";

                                bill.AddCounters(counters);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch (Exception ex)
                        {

                            MonitorLog.WriteLog("Ошибка выборки 5 |" + sql + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        #endregion
                    }


                    if (bill.FakturaBlocks.HasCountersDoubleDomBlock)
                    {
                        #region Загружаем показания счетчиков по Дому

                        sql.Remove(0, sql.Length);
                        sql.Append(" Select s.ordering, service_name as service, cs.nzp_serv, cs.num_cnt, " +
                                   "        cs.nzp_cnttype, d.dat_s, d.val_s, d.dat_po, " +
                                   "        d.val_po, sc.cnt_stage, sc.mmnog as formula, cs.dat_prov, " +
                                   "        cs.dat_provnext, cs.is_gkal, sm.measure  " +
                                   " From " + baseKernel + "services s, " +
                                           baseKernel + "s_counttypes sc,  " +
                                           baseKernel + "s_counts st, " +
                                           baseKernel + "s_measure sm, " +
                                           baseData + "counters_spis cs  " +
                                   " left join " + tableCounters + " d " +
                                   " on cs.nzp_counter=d.nzp_counter and d.nzp_type=1 and d.stek = 1 " +
                                   " Where cs.nzp_serv=s.nzp_serv " +
                                   "       AND cs.nzp_cnt=st.nzp_cnt " +
                                   "       AND st.nzp_measure=sm.nzp_measure " +
                                   "       AND cs.nzp_cnttype=sc.nzp_cnttype " +
                                   "       AND cs.dat_close is null " +
                                   "       AND cs.nzp=" + nzpDom +
                                   "       AND cs.nzp_type=1 " +
                                   " ORDER BY ordering,2,4,5 ");

                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var counters = new Counters
                                {
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString()),
                                    Measure = reader["measure"].ToString().Trim(),
                                    ServiceName = reader["service"].ToString().Trim(),
                                    Value =
                                        reader["val_po"] != DBNull.Value
                                            ? Decimal.Parse(reader["val_po"].ToString())
                                            : 0,
                                    DatUchet =
                                        reader["dat_s"] != DBNull.Value
                                            ? (DateTime)reader["dat_po"]
                                            : DateTime.Parse("01.01.1900"),
                                    ValuePred =
                                        reader["val_s"] != DBNull.Value ? Decimal.Parse(reader["val_s"].ToString()) : 0,
                                    NumCounters = reader["num_cnt"].ToString().Trim(),
                                    DatUchetPred =
                                        reader["dat_s"] != DBNull.Value
                                            ? (DateTime)reader["dat_po"]
                                            : DateTime.Parse("01.01.1900"),
                                    CntStage = Int32.Parse(reader["cnt_stage"].ToString()),
                                    Formula =
                                        reader["formula"] != DBNull.Value
                                            ? Decimal.Parse(reader["formula"].ToString())
                                            : 1,
                                    DatProv =
                                        reader["dat_provnext"] != DBNull.Value
                                            ? ((DateTime)reader["dat_provnext"]).ToShortDateString()
                                            : ""
                                };
                                if (counters.DatProv == "")
                                    counters.DatProv = reader["dat_prov"] != DBNull.Value ? ((DateTime)reader["dat_prov"]).ToShortDateString() : "";
                                counters.IsGkal = false;

                                if (reader["is_gkal"] != DBNull.Value)
                                    if (reader["is_gkal"].ToString().Trim() == "1")
                                        counters.IsGkal = true;
                                bill.AddDomCounters(counters);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch (Exception ex)
                        {

                            MonitorLog.WriteLog("Ошибка выборки 6 |" + sql + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }

                        #endregion
                    }

                    if (bill.FakturaBlocks.HasNewCountersBlock)
                    {
                        bill.NewlistCounters = bill.DbfakturaCounters.LoadSingeLsCounters(pref, nzpKvar, bill.Pkod);
                        bill.NewlistDomCounters = bill.DbfakturaCounters.LoadDoubleDomCounters(pref, nzpDom);
                    }

                    if (bill.FakturaBlocks.HasNewDoubleCountersBlock)
                    {
                        bill.NewlistCounters = bill.DbfakturaCounters.LoadDoubleLsCounters(pref, nzpKvar, bill.Pkod);
                        bill.NewlistDomCounters = bill.DbfakturaCounters.LoadDoubleDomCounters(pref, nzpDom);
                    }

                    if (bill.FakturaBlocks.HasServiceVolumeBlock)
                    {

                        #region Загружаем расходы по дому
                        sql.Remove(0, sql.Length);
                        if (_finder.month_ == 1 && _finder.year_ == 2015)
                        {
                            if (nzpDom == 51)
                            {
                                sql.Append("  select s.service as service, c.nzp_serv, c.kod_info, max(cnt_stage) as cnt_stage, sum(dlt_calc) as dlt_calc, ");
                                sql.Append(" (case when c.nzp_serv = 7 then 0 else sum(dop87) end) as dpu_odn, sum(val1) as rashod ,0 as dpu, 0 as dpu_cut, 0 as norm_odn, 0 as kf307, 0 as counter_mop");
                                sql.Append(" from bill01_charge_15.counters1503_01 c inner join bill01_kernel.services s on c.nzp_serv = s.nzp_serv ");
                                sql.Append(" where nzp_dom=" + nzpDom + " and nzp_type=3 and stek=3 group by 1,2,3");
                            }
                            else
                            {
                                sql.Append("  select s.service as service, c.nzp_serv, c.kod_info, max(cnt_stage) as cnt_stage, sum(dlt_calc) as dlt_calc, ");
                                sql.Append(" (case when c.nzp_serv = 7 then 0 else sum(dop87) end) as dpu_odn, sum(val1) as rashod ,0 as dpu, 0 as dpu_cut, 0 as norm_odn, 0 as kf307, 0 as counter_mop");
                                sql.Append(" from bill01_charge_15.counters1502_01 c inner join bill01_kernel.services s on c.nzp_serv = s.nzp_serv ");
                                sql.Append(" where nzp_dom=" + nzpDom + " and nzp_type=3 and stek=3 group by 1,2,3");
                            }
                        }
                        else
                        {
                            sql.Append(" SELECT s.service_name as service, a.nzp_serv, a.kod_info, a.cnt_stage, ");
                            sql.Append("        a.dlt_calc, ");//Сумма расходов по ЛС
                            sql.Append("        a.kf_dpu_ls as dpu_odn, ");//ОДН по дому
                            sql.Append("        a.val1 + a.val2 + a.dlt_reval + a.dlt_real_charge as rashod, ");//Расход по квартирам дома
                            sql.Append("        case when a.cur_zap = 1 then (case when a.kod_info>100 then a.val3 else a.val4 end)-a.val1 -a.val2 -a.dlt_reval ");
                            sql.Append("        -a.dlt_real_charge else (case when a.kod_info>100 then a.val3 else a.val4 end) end as dpu, ");
                            sql.Append("        a.val3 as dpu_cut, ");//Обрезанный расход по дому нормативом
                            sql.Append("        " + DBManager.sNvlWord + "(a.vl210,0) as norm_odn, ");//Норматив на 1 квм. по ЛС
                            sql.Append("        a.kf307,  ");//Коэффициент коррекции
                            sql.Append("        a.cur_zap as counter_mop");//Признак счетчика ОДН
                            sql.Append(" FROM " + tableCounters + " a," + baseKernel + "services s ");
                            sql.Append(" WHERE a.nzp_dom = " + nzpDom);
                            sql.Append("        AND dat_charge is null ");
                            sql.Append("        AND a.nzp_serv=s.nzp_serv AND   a.nzp_serv != 8  ");
                            sql.Append("        AND stek = 3 ");
                            sql.Append("        AND nzp_type=1 UNION ALL ");
                            sql.Append(" SELECT s.service_name as service, a.nzp_serv, a.kod_info, a.cnt_stage, ");
                            sql.Append("        a.dlt_calc as dlt_calc, ");//Сумма расходов по ЛС
                            sql.Append("        a.kf_dpu_ls as dpu_odn, ");//ОДН по дому
                            sql.Append("        a.val1 + a.val2 + a.dlt_reval + a.dlt_real_charge as rashod, ");//Расход по квартирам дома
                            sql.Append("        case when a.cur_zap = 1 then (case when a.kod_info>100 then a.val3 else a.val4 end)-a.val1 -a.val2 -a.dlt_reval ");
                            sql.Append("        -a.dlt_real_charge else (case when a.kod_info>100 then a.val3 else a.val4 end) end as dpu, ");
                            sql.Append("        a.val3 as dpu_cut, ");//Обрезанный расход по дому нормативом
                            sql.Append("        " + DBManager.sNvlWord + "(a.vl210,0) as norm_odn, ");//Норматив на 1 квм. по ЛС
                            sql.Append("        a.kf307,  ");//Коэффициент коррекции
                            sql.Append("        a.cur_zap as counter_mop");//Признак счетчика ОДН
                            sql.Append(" FROM " + tableCounters + " a," + baseKernel + "services s ");
                            sql.Append(" WHERE a.nzp_dom = " + nzpDom);
                            sql.Append("        AND dat_charge is null ");
                            sql.Append("        AND a.nzp_serv=s.nzp_serv AND  a.nzp_serv = 8 ");
                            sql.Append("        AND stek = 9 ");
                            sql.Append("        AND nzp_type=1 ");
                        }
                        if (!DBManager.ExecSQL(_conDb, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки 7 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            _conDb.Close();
                            return null;
                        }
                        decimal domBValueForHvsNaGvs = 0;
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        //StreamWriter sw1 = new StreamWriter(@"C:\temp\epd.txt", true);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var servVolume = new ServVolume
                                {
                                    DomArendatorsVolume = 0,
                                    DomLiftVolume = Decimal.Parse(reader["dpu_cut"].ToString()),
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString()),
                                    ServiceName = reader["service"].ToString().Trim()
                                };


                                if (reader["cnt_stage"].ToString() == "1" || (reader["cnt_stage"].ToString() == "5" && servVolume.NzpServ == 8)
                                    || (reader["cnt_stage"].ToString() == "1" && servVolume.NzpServ == 9)
                                    || (reader["cnt_stage"].ToString() == "0" && servVolume.NzpServ == 6)
                                    || (_finder.month_ == 1 && _finder.year_ == 2015))
                                {
                                    switch (servVolume.NzpServ)
                                    {
                                        case 25: bill.HasElDpu = true; break;
                                        case 6: bill.HasHvsDpu = true; break;
                                        case 9: bill.HasGvsDpu = true; break;
                                        case 10: bill.HasGazDpu = true; break;
                                        case 8: bill.HasOtopDpu = true; break;
                                    }
                                }

                                if (((servVolume.NzpServ == 88) & (reader["kod_info"].ToString() == "24")) ||
                                    ((servVolume.NzpServ == 99) & (reader["kod_info"].ToString() == "25")))
                                {

                                }
                                else
                                {
                                    if (_finder.month_ == 1 && _finder.year_ == 2015)
                                    {
                                        


                                        servVolume.AllLsVolume = Decimal.Parse(reader["dlt_calc"].ToString());
                                        servVolume.OdnDomVolume = Decimal.Parse(reader["dpu_odn"].ToString());
                                        servVolume.DomVolume = Decimal.Parse(reader["rashod"].ToString());
                                        servVolume.DomLiftVolume = Decimal.Parse(reader["dpu_cut"].ToString());
                                    }
                                    else
                                    {
                                        servVolume.AllLsVolume = Decimal.Parse(reader["dlt_calc"].ToString());
                                        servVolume.DomVolume = Decimal.Parse(reader["dpu"].ToString());
                                        servVolume.OdnDomVolume = Decimal.Parse(reader["dpu_odn"].ToString());
                                        servVolume.DomLiftVolume = Decimal.Parse(reader["dpu_cut"].ToString());
                                        if (_finder.idFaktura == 100 || _finder.idFaktura == 112 || _finder.idFaktura == 117 || _finder.idFaktura == 118
                                            || _finder.idFaktura == 107 || _finder.idFaktura == 108 || _finder.idFaktura == 122)
                                        {
                                            if (servVolume.NzpServ == 999999)
                                                servVolume.OdnDomVolume = servVolume.DomVolume -
                                                    Decimal.Parse(reader["rashod"].ToString());

                                            servVolume.DomVolume = Decimal.Parse(reader["rashod"].ToString());

                                            if (reader["counter_mop"].ToString() == "1")
                                            {
                                                //servVolume.DomVolume = 0;
                                                servVolume.OdnDomVolume = Decimal.Parse(reader["dpu"].ToString());
                                            }

                                        }
                                        else
                                        {
                                            servVolume.DomVolume = Decimal.Parse(_finder.idFaktura == 111
                                                ? reader["rashod"].ToString()
                                                : reader["dpu_cut"].ToString());
                                        }
                                    }
                                }
                                if (reader["counter_mop"].ToString() == "1" && !(_finder.month_ == 1 && _finder.year_ == 2015))
                                {
                                    //servVolume.DomVolume = 0;
                                    servVolume.OdnDomVolume = Decimal.Parse(reader["dpu"].ToString());
                                }

                                if ((servVolume.NzpServ == 9) & (Decimal.Parse(reader["norm_odn"].ToString()) > 0.00001m))
                                    bill.Kfodngvs = Decimal.Parse(reader["norm_odn"].ToString()).ToString("0.00##");
                                else if ((servVolume.NzpServ == 6) & (Decimal.Parse(reader["norm_odn"].ToString()) > 0.00001m))
                                    bill.Kfodnhvs = Decimal.Parse(reader["norm_odn"].ToString()).ToString("0.00##");
                                else if ((servVolume.NzpServ == 25) & (Decimal.Parse(reader["norm_odn"].ToString()) > 0.00001m))
                                    bill.KfodnEl = Decimal.Parse(reader["norm_odn"].ToString()).ToString("0.00##");

                                servVolume.Kf307 = Decimal.Parse(reader["norm_odn"].ToString());
                                if (servVolume.NzpServ != 7)
                                {
                                    //sw1.WriteLine(servVolume.ServiceName);
                                    //sw1.WriteLine(servVolume.OdnDomVolume);
                                    //sw1.WriteLine(servVolume.DomVolume);
                                    //sw1.Close();
                                    if (servVolume.ServiceName == "Горячая вода")
                                    {
                                        //var servVolume1 = new ServVolume
                                        //{
                                        //    DomArendatorsVolume = 0,
                                        //    DomLiftVolume = 0,
                                        //    NzpServ = 14,
                                        //    ServiceName = "ХВС для ГВС",
                                        //    AllLsVolume = 0,
                                        //    OdnDomVolume = 0,
                                        //    DomVolume = Decimal.Parse(reader["rashod"].ToString())
                                        //};
                                        //bill.AddDomVolume(servVolume1);
                                    }
                                    bill.AddDomVolume(servVolume);
                                }
                            }
                            //sw.Close();
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch (Exception ex)
                        {

                            MonitorLog.WriteLog("Ошибка выборки 8 |" + sql + " " + ex + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }





                        #endregion
                    }


                    if (bill.FakturaBlocks.HasPerekidkiSamaraBlock)
                    {
                        #region Загружаем перекидки по причинам ОДН для Самары

                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT nzp_serv, sum_rcl, comment ");
                        sql.Append(" FROM  t_fPerekidka a ");
                        sql.Append(" WHERE nzp_kvar=" + nzpKvar);
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                bill.AddPerekidkaOdn(Int32.Parse(reader["nzp_serv"].ToString()),
                                    Decimal.Parse(reader["sum_rcl"].ToString()));
                            }
                            reader.Close();
                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 9 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }


                        #endregion
                    }

                    if (bill.FakturaBlocks.HasRevalReasonBlock)
                    {
                        #region Загружаем причины перерасчета
                        //Недопоставки
                        sql.Remove(0, sql.Length);

                        sql.Append(" UPDATE ");
                        sql.Append(" t_fReasonReval  ");
                        sql.Append(" SET izm_comment = 'Недопоставка 2' WHERE (izm_comment is null OR izm_comment = '')  AND source LIKE '%Недопоставка%'");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        sql.Remove(0, sql.Length);

                        sql.Append(" SELECT *  ");
                        sql.Append(" FROM t_fReasonReval a ");
                        sql.Append(" WHERE nzp_kvar=" + nzpKvar);
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                if (reader["source"].ToString().Trim() == "Недопоставка")
                                    bill.AddReasonReval(Int32.Parse(reader["nzp_serv"].ToString()), reader["izm_comment"].ToString().Trim(), "");

                                else if (reader["source"].ToString().Trim() == "Счетчики")
                                    bill.AddReasonReval(Int32.Parse(reader["nzp_serv"].ToString()), " Показания счетчиков", "");

                                else if (reader["source"].ToString().Trim() == "Новые счетчики")
                                    bill.AddReasonReval(Int32.Parse(reader["nzp_serv"].ToString()),
                                        " Счетчик №" + reader["num_cnt"].ToString().Trim()
                                        + " опломбирован " + reader["dat_plomb"].ToString().Trim(), "");

                                else if (reader["source"].ToString().Trim() == "Перекидки")
                                {
                                    if (reader["izm_comment"].ToString().Trim() != "")
                                        bill.AddReasonReval(Int32.Parse(reader["nzp_serv"].ToString()),
                                            reader["izm_comment"].ToString().Trim(), "");
                                }
                                else if (reader["source"].ToString().Trim() == "Must_calc")
                                {
                                    if (reader["kod_reval"].ToString().Trim() != "")
                                    {
                                        switch (Int32.Parse((reader["kod_reval"]).ToString()))
                                        {
                                            case 1: bill.AddReasonReval(Int32.Parse(reader["nzp_serv"].ToString()),
                                               "Характеристики жилья", reader["izm_comment"].ToString().Trim()); break;
                                            case 2: bill.AddReasonReval(Int32.Parse(reader["nzp_serv"].ToString()),
                                           "Период действия услуги", reader["izm_comment"].ToString().Trim()); break;
                                            case 8: bill.AddReasonReval(Int32.Parse(reader["nzp_serv"].ToString()),
                                           "Домовые счетчики", reader["izm_comment"].ToString().Trim()); break;

                                        }
                                    }

                                }
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 10 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }

                        #endregion
                    }

                    if (bill.FakturaBlocks.HasRemarkblock)
                    {
                        #region Прмечания в счете
                        if (oldDom != nzpDom)
                        {
                            bill.AreaRemark = "";
                            bill.GeuRemark = "";
                            bill.DomRemark = "";
                            oldDom = nzpDom;
                            sql.Remove(0, sql.Length);
                            sql.Append(" SELECT 1 as ord, remark ");
                            sql.Append(" FROM " + tableRemark + "  ");
                            sql.Append(" WHERE nzp_dom=" + nzpDom);
                            sql.Append(" union all ");
                            sql.Append(" SELECT 2 , remark ");
                            sql.Append(" FROM " + tableRemark + "  ");
                            sql.Append(" WHERE nzp_geu=" + bill.NzpGeu);
                            sql.Append(" union all ");
                            sql.Append(" SELECT 3, remark ");
                            sql.Append(" FROM " + tableRemark + "  ");
                            sql.Append(" WHERE nzp_area=" + bill.NzpArea);
                            cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                            try
                            {
                                reader = cmd1.ExecuteReader();
                                while (reader.Read())
                                {
                                    if (reader["ord"] != DBNull.Value)
                                    {
                                        switch (Convert.ToInt32(reader["ord"]))
                                        {
                                            case 1:
                                                { bill.DomRemark = reader["remark"] != DBNull.Value ? reader["remark"].ToString().Trim() : ""; }
                                                break;
                                            case 2:
                                                { bill.GeuRemark = reader["remark"] != DBNull.Value ? reader["remark"].ToString().Trim() : ""; }
                                                break;
                                            case 3:
                                                {
                                                    if (reader["remark"] != DBNull.Value)
                                                    {
                                                        Encoding codepage = Encoding.Default;
                                                        try
                                                        {
                                                            bill.AreaRemark = codepage.GetString(Convert.FromBase64String((reader["remark"].ToString().Trim())));
                                                        }
                                                        catch
                                                        {
                                                            bill.AreaRemark = reader["remark"].ToString().Trim();
                                                        }
                                                    }
                                                    else bill.AreaRemark = "";

                                                }
                                                break;

                                        }

                                    }


                                }
                                reader.Close();
                                reader.Dispose();
                                cmd1.Dispose();

                            }
                            catch
                            {
                                MonitorLog.WriteLog("Ошибка выборки 11 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                                cmd1.Dispose();
                                _conDb.Close();
                                return null;
                            }

                        }
                        #endregion
                    }

                    if (bill.FakturaBlocks.HasDatOplBlock)
                    {
                        #region Дата последней оплаты в счете
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT * ");
                        sql.Append(" FROM " + tablePackLs + " a, " + baseData + "kvar k ");
                        sql.Append(" WHERE a.num_ls=k.num_ls ");
                        sql.Append(" AND nzp_kvar=" + nzpKvar);
                        sql.Append(" AND dat_uchet <= Date('" + lastDay + "') order by dat_vvod desc");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                if (reader["dat_vvod"] != DBNull.Value)
                                {
                                    if ((bill.LastSumOplat == 0) || (
                                        DateTime.Parse(reader["dat_uchet"].ToString()) >=
                                        DateTime.Parse("01." + _finder.month_ + "." + _finder.year_ + "")))
                                    {

                                        if (bill.DateOplat == "")
                                            bill.DateOplat = DateTime.Parse(reader["dat_vvod"].ToString()).ToShortDateString();
                                        bill.LastSumOplat += Decimal.Parse(reader["g_sum_ls"].ToString());
                                    }

                                }


                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 12 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }


                        #endregion
                    }



                    if (bill.FakturaBlocks.HasCalcGil)
                    {
                        #region Количество проживающих
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT sum(cnt2) as countGil, sum(val5) as countDepartureGil,");
                        sql.Append(" sum(val3) as countArriveGil, sum(val4) as paspgil ");
                        sql.Append(" FROM " + tableGil);
                        sql.Append(" WHERE stek=3 and nzp_kvar=" + nzpKvar);
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                if (reader["countGil"] != DBNull.Value)
                                {
                                    Int32.TryParse(reader["countGil"].ToString(), out bill.CountGil);
                                    bill.CountArriveGil = Convert.ToInt32(
                                        Math.Round(Decimal.Parse(reader["countArriveGil"].ToString())));

                                    bill.CountDepartureGil = Convert.ToInt32(
                                        Math.Round(Decimal.Parse(reader["countDepartureGil"].ToString())));
                                    if (isPasp) bill.CountRegisterGil = bill.CountArriveGil + Convert.ToInt32(
                                        Math.Round(Decimal.Parse(reader["paspgil"].ToString())));
                                }


                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 13 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }


                        #endregion
                    }

                    if (bill.FakturaBlocks.HasSzBlock)
                    {
                        #region Загрузка информации Социальной защиты
                        sql.Remove(0, sql.Length);
                        sql.Append(" Select fam, ima, otch, drog, sum(case when nzp_exp in (6,120) then sum_must else 0 end) as ls_smo, ");
                        sql.Append("         sum(case when ((nzp_exp>10) AND (nzp_exp<20))or(nzp_exp=128) ");
                        sql.Append(" then sum_must else 0 end) as ls_edv, ");
                        sql.Append("         sum(case when ((nzp_exp<=10)and(nzp_exp<>6))or(nzp_exp=21) ");
                        sql.Append(" then sum_must else 0 end) as ls_lgota, ");
                        sql.Append("         sum(case when nzp_exp=28 then sum_must else 0 end) as ls_tepl, ");
                        sql.Append("         sum(case when nzp_exp=3 then sum_must else 0 end) as ls_sv ");
                        sql.Append("  From " + tableSz + " a, " + baseData + "kvar k ");
                        sql.Append("  Where a.num_ls=k.num_ls ");
                        sql.Append("    AND nzp_exp>0 ");
                        sql.Append(" AND k.nzp_kvar=" + nzpKvar);
                        sql.Append(" group by 1,2,3,4");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var sg = new SzGilec
                                {
                                    Fam = reader["fam"] != DBNull.Value ? reader["fam"].ToString().Trim() : "",
                                    Ima = reader["ima"] != DBNull.Value ? reader["ima"].ToString().Trim() : "",
                                    Otch = reader["otch"] != DBNull.Value ? reader["otch"].ToString().Trim() : "",
                                    DatRog = reader["drog"] != DBNull.Value ? reader["drog"].ToString().Trim() : ""
                                };

                                Decimal.TryParse(reader["ls_edv"].ToString(), out sg.SumEdv);
                                Decimal.TryParse(reader["ls_smo"].ToString(), out sg.SumSubs);
                                Decimal.TryParse(reader["ls_tepl"].ToString(), out sg.SumTepl);
                                Decimal.TryParse(reader["ls_lgota"].ToString(), out sg.SumLgota);
                                Decimal.TryParse(reader["ls_sv"].ToString(), out sg.SumSv);
                                bill.SzInformation.AddGilec(sg);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 14 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        #endregion
                    }

                    if (bill.FakturaBlocks.HasAreaDataBlock)
                    {
                        #region Заполнение реквизитов по территории

                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT area, remark ");
                        sql.Append(" FROM " + baseData + "s_area a LEFT JOIN " + Points.Pref + DBManager.sDataAliasRest + "s_remark r ");
                        sql.Append(" ON a.nzp_area = r.nzp_area WHERE a.nzp_area = " + nzpArea);
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            reader.Read();
                            bill.AreaName = reader["area"] != DBNull.Value ? reader["area"].ToString().Trim() : "";
                            bill.AreaRemark = reader["remark"] != DBNull.Value ? reader["remark"].ToString().Trim() : "";
                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 15 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT   nzp_prm, val_prm ");
                        sql.Append(" FROM " + Points.Pref + DBManager.sDataAliasRest + "prm_7  ");
                        sql.Append(" WHERE nzp=" + nzpArea);
                        sql.Append("        AND is_actual=1 ");
                        sql.Append("        AND dat_s<=" + DBManager.sCurDate);
                        sql.Append("        AND dat_po>=" + DBManager.sCurDate);
                        sql.Append("        AND nzp_prm  in (576, 294, ");
                        sql.Append("        296, 581, 582, 583, 584, 585, 1333) ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                switch (Int32.Parse(reader["nzp_prm"].ToString()))
                                {
                                    case 294: bill.AreaAdsPhone = reader["val_prm"].ToString().Trim(); break;
                                    case 296: bill.AreaAdr = reader["val_prm"].ToString().Trim(); break;
                                    case 576: bill.AreaDirectorFio = reader["val_prm"].ToString().Trim(); break;
                                    case 581: bill.AreaDirectorPost = reader["val_prm"].ToString().Trim(); break;
                                    case 582: bill.AreaEmail = reader["val_prm"].ToString().Trim(); break;
                                    case 583: bill.AreaWeb = reader["val_prm"].ToString().Trim(); break;
                                    case 584: bill.AreaPhone = reader["val_prm"].ToString().Trim(); break;
                                    case 585: bill.AreaFax = reader["val_prm"].ToString().Trim(); break;
                                    case 1333: bill.AreaWorkTime = reader["val_prm"].ToString().Trim(); break;
                                }
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();
                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 16 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        #endregion
                    }

                    if (bill.FakturaBlocks.HasGeuDataBlock)
                    {
                        #region Заполнение реквизитов по ЖЭУ

                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT  nzp_prm, val_prm ");
                        sql.Append(" FROM " + baseData + "prm_8  ");
                        sql.Append(" WHERE nzp=" + nzpGeu);
                        sql.Append("        AND is_actual=1 ");
                        sql.Append("        AND dat_s<=" + DBManager.sCurDate);
                        sql.Append("        AND dat_po>=" + DBManager.sCurDate);
                        sql.Append("        AND nzp_prm  in (65, 73, 714, 716, 1210, 875) ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                switch (Int32.Parse(reader["nzp_prm"].ToString()))
                                {
                                    case 73: bill.GeuName = reader["val_prm"].ToString().Trim(); break;
                                    case 716: bill.GeuPref = reader["val_prm"].ToString().Trim(); break;
                                    case 1210: bill.GeuDatPlat = reader["val_prm"].ToString().Trim(); break;
                                    case 875: bill.GeuAdr = reader["val_prm"].ToString().Trim(); break;
                                    case 65: bill.GeuPhone = reader["val_prm"].ToString().Trim(); break;
                                    case 714: bill.GeuKodErc = reader["val_prm"].ToString().Trim(); break;
                                }

                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 17 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        if (bill.GeuKodErc == "")
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" SELECT  erc_code ");
                            sql.Append(" FROM " + baseKernel + "s_erc_code  ");
                            sql.Append(" WHERE is_current=1 ");
                            cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                            try
                            {
                                reader = cmd1.ExecuteReader();
                                if (reader.Read())
                                {
                                    bill.GeuKodErc = reader["erc_code"].ToString().Trim();
                                }
                                reader.Close();
                                reader.Dispose();
                                cmd1.Dispose();

                            }
                            catch
                            {
                                MonitorLog.WriteLog("Ошибка выборки 18 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                                cmd1.Dispose();
                                _conDb.Close();
                                return null;
                            }
                        }

                        #endregion
                    }

                    if (bill.FakturaBlocks.HasUpravDomBlock)
                    {
                        #region Заполнение данных Старших по домам

                        sql.Remove(0, sql.Length);
                        sql.Append("  Select trim(fam)||' '||trim(ima)||' '||' '||trim(otch)||' '||trim(adres) as mdom ");
                        sql.Append("  From " + baseData + "h_master a,");
                        sql.Append(baseData + "h_link b ");
                        sql.Append("  Where a.nzp_hm=b.nzp_hm ");
                        sql.Append("        AND nzp_dom= " + nzpDom);
                        sql.Append("        AND kod=2 ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);
                        try
                        {
                            reader = cmd1.ExecuteReader();
                            if (reader.Read())
                            {
                                if (reader["mdom"] != DBNull.Value)
                                {
                                    bill.UpravDom = reader["mdom"].ToString().Trim();
                                }


                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {
                            MonitorLog.WriteLog("Ошибка выборки 19 |" + sql, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        #endregion
                    }


                    if (bill.FakturaBlocks.HasDomRashodBlock)
                    {
                        #region Заполнение расходоп по услугам по домам с Арендаторами
                        //Первая часть арендаторов
                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT service, a.nzp_serv, kod_info, sum(A.val3 - a.val2 - a.val1 - a.dlt_cur) as odn, ");
                        sql.Append("        sum(a.val3 + a.pu7kw) as rashod, ");
                        sql.Append("        max(a.kf307) as kf307, sum(a.squ1) as squ, sum(a.dop87) as dop87, ");
                        sql.Append("        sum(a.val1 + a.val2) as rash_kv, sum(a.pu7kw) as pu7kw ");
                        sql.Append(" FROM " + tableCounters + " a, ");
                        sql.Append(baseKernel + "services s ");
                        sql.Append(" WHERE a.nzp_dom=" + nzpDom);
                        sql.Append("        AND dat_charge is null ");
                        sql.Append("        AND a.nzp_serv=s.nzp_serv ");
                        sql.Append("        AND stek=3 AND nzp_type=1 ");
                        sql.Append(" GROUP BY 1,2,3         ");
                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                var servVolume = new ServVolume
                                {
                                    DomArendatorsVolume = 0,
                                    DomLiftVolume = Decimal.Parse(reader["dpu_cut"].ToString()),
                                    NzpServ = Int32.Parse(reader["nzp_serv"].ToString()),
                                    ServiceName = reader["service"].ToString().Trim(),
                                    OdnDomVolume =
                                        Decimal.Parse(Int32.Parse(reader["kod_info"].ToString()) == 27
                                            ? reader["dop87"].ToString()
                                            : reader["odn"].ToString())
                                };

                                if ((Decimal.Parse(reader["rashod"].ToString()) == 0) &
                                    (servVolume.NzpServ == 8))
                                {
                                    servVolume.DomVolume = Decimal.Parse(reader["rash_kv"].ToString());
                                }
                                else
                                {
                                    servVolume.DomVolume = Decimal.Parse(reader["rashod"].ToString());
                                }
                                servVolume.DomArendatorsVolume = Decimal.Parse(reader["pu7kw"].ToString());
                                servVolume.Kf307 = Decimal.Parse(reader["kf307"].ToString());

                                bill.AddDomVolume(servVolume);
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {

                            MonitorLog.WriteLog("Ошибка выборки 20 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }
                        //Вторая часть арендаторов

                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT nzp_serv, rvaldlt, sum_ls_val + sum_ls_210val as sum_ls_val,  ");
                        sql.Append(" sum_ls_norm+sum_ls_210norm  as sum_ls_norm, cnt_pl_pu, cnt_pl_norm,");
                        sql.Append(" rval_now, rval_pred, dnow, dpred, sum_ls_a_val, sum_ls_a_norm ");
                        sql.Append(" FROM " + baseData + "counters_correct a ");
                        sql.Append(" WHERE a.nzp_dom=" + nzpDom + " AND dat_month = dat_charge");
                        sql.Append(" AND dat_month =MDY(" + _finder.month_ + ",01," + _finder.year_ + ")");

                        cmd1 = DBManager.newDbCommand(sql.ToString(), _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            while (reader.Read())
                            {
                                foreach (ServVolume t in bill.ListVolume)
                                {
                                    if (t.NzpServ == Int32.Parse(reader["nzp_serv"].ToString()))
                                        t.DomArendatorsVolume += Decimal.Parse(reader["sum_ls_a_val"].ToString());
                                }
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {

                            MonitorLog.WriteLog("Ошибка выборки 21 |" + sql + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }

                        #endregion
                    }


                    if (bill.FakturaBlocks.HasBezenchuk)
                    {
                        try
                        {
                            #region Получение общедомых нужд на отопление Гкал,ГВС Гкал, Расход ОДПУ ГВС Гкал
                            sql = new StringBuilder();
                            sql.Append("SELECT max(vol_otop) as vol_otop,max(vol_gvs) as vol_gvs, max(vol_odpu) as vol_odpu  FROM TempBezenchukPrm where nzp_dom=" + nzpDom);
                            DataTable DT = ClassDBUtils.OpenSQL(sql.ToString(), _conDb).resultData;
                            if (DT.Rows.Count > 0)
                            {
                                decimal vol_otop = (DT.Rows[0]["vol_otop"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[0]["vol_otop"]) : 0);
                                decimal vol_gvs = (DT.Rows[0]["vol_gvs"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[0]["vol_gvs"]) : 0);
                                bill.RashodOdpuGkal = (DT.Rows[0]["vol_odpu"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[0]["vol_odpu"]) : 0);
                                for (int i = 0; i < bill.ListServ.Count; i++)
                                {
                                    if (bill.ListServ[i].Serv.NzpServ == 88) bill.ListServ[i].Serv.OdnDomVolumePu = vol_otop;
                                    if (bill.ListServ[i].Serv.NzpServ == 99) bill.ListServ[i].Serv.OdnDomVolumePu = vol_gvs;
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog("Ошибка выборки 22 |" + sql.ToString() + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            _conDb.Close();
                            return null;
                        }


                            #endregion
                    }

                    if (bill.FakturaBlocks.HasSupplierPkod)
                    {
                        string sqls = " select * from " + pref + DBManager.sDataAliasRest + "supplier_codes " +
                                      " where nzp_kvar =" + nzpKvar;
                        cmd1 = DBManager.newDbCommand(sqls, _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            if (reader.Read())
                            {

                                bill.PkodKapr = reader["pkod_supp"].ToString().Trim();
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {

                            MonitorLog.WriteLog("Ошибка выборки 23 |" + sqls + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }

                        //Количество этажей
                        int stageCount = 6;
                        sqls = " select * from " + pref + DBManager.sDataAliasRest + "prm_2 " +
                            " where nzp_prm= 37 and nzp =" + nzpDom +
                            " and is_actual =1 and dat_s<='" + datCalc + "' and dat_po>='" + datCalc + "'";
                        cmd1 = DBManager.newDbCommand(sqls, _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            if (reader.Read())
                            {

                                try
                                {
                                    stageCount = Convert.ToInt32(reader["val_prm"].ToString().Trim());
                                }
                                catch
                                {
                                    MonitorLog.WriteLog("Неопределено количество этажей для  " + nzpDom + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                                }

                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {

                            MonitorLog.WriteLog("Ошибка выборки 24 |" + sqls + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }


                        //Тариф по капремонту на дом
                        sqls = " select * from " + pref + DBManager.sDataAliasRest + "prm_2 " +
                               " where nzp_prm= 686 and nzp =" + nzpDom +
                               " and is_actual =1 and dat_s<='" + datCalc + "' and dat_po>='" + datCalc + "'";
                        cmd1 = DBManager.newDbCommand(sqls, _conDb);

                        try
                        {
                            reader = cmd1.ExecuteReader();
                            if (reader.Read())
                            {

                                bill.TarifKapr = reader["val_prm"].ToString().Trim();
                            }
                            else
                            {
                                if (stageCount < 6)
                                    bill.TarifKapr = "5.07"; //!!!!временно
                                else bill.TarifKapr = "5.84"; //!!!!временно
                            }
                            reader.Close();
                            reader.Dispose();
                            cmd1.Dispose();

                        }
                        catch
                        {

                            MonitorLog.WriteLog("Ошибка выборки 25 |" + sqls + " " + countKvit, MonitorLog.typelog.Error, 20, 201, true);
                            cmd1.Dispose();
                            _conDb.Close();
                            return null;
                        }

                    }


                    if (bill.FakturaBlocks.HasArendBlock)
                    {
                        bill.ArendNumAct = GetNumAct(numLs, lastDay);

                    }

                    if (bill.FakturaBlocks.HasGilPeriodsBlock)
                    {
                        bill.GilPeriods = GetGilVrVib(baseData, nzpKvar, datCalc, out ret);
                    }

                    if (bill.FakturaBlocks.HasNewNachBlock)
                    {
                        if (bill.DbfakturaCharge == null)
                            bill.DbfakturaCharge = new DbFakturaCharge(_conDb, bill.Month, bill.Year);

                        bill.DbfakturaCharge.PreLoadNach(tableCharge, nzpKvar, nzpArea);

                    }

                    if (bill.FakturaBlocks.HasPrintOrdering)
                    {
                        if (bill.DbfakturaOrdering == null)
                            bill.DbfakturaOrdering = new DbFakturaOrdering(_conDb, tableCharge);
                        else
                            bill.DbfakturaOrdering.SetChargeTable(tableCharge);

                    }

                    try
                    {
                        bill.Rekvizit = GetUkBankRekvizit(spis, bill.NzpArea, bill.NzpGeu, bill.Pkod);
                  
                        //bill.SumTicket = bill.SummaryServ.Serv.SumCharge;
                        bill.SumTicket = bill.SummaryServ.Serv.RsumTarif + 
                            (bill.SummaryServ.Serv.Reval + bill.SummaryServ.Serv.RealCharge) +
                            bill.SummaryServ.Serv.SumInsaldo - bill.SummaryServ.Serv.SumMoney;
                     
                        bill.FinalPass(_finder);
                        if (bill.DoPrint())
                        {
                         
                            bill.FillRow(table);
                
                        }
                        else
                        {
                            //sw.WriteLine("eeeeee = " + _finder.idFaktura);
                            if (
                                (_finder.idFaktura == (int)STCLINE.KP50.Interfaces.Faktura.FakturaTypes.TulaFaktura) ||
                                (_finder.idFaktura == 100) ||
                                (_finder.idFaktura == 107) ||
                                (_finder.idFaktura == 108) ||
                                (_finder.idFaktura == 122) ||
                                (_finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One) ||
                                (_finder.idFaktura == (int)STCLINE.KP50.Interfaces.Faktura.FakturaTypes.TulaFaktura))
                            {
                          
                                bill.FillRow(table);
                          
                            }
                        }
                    
                        if (bill.FakturaBlocks.HasPrintOrdering)
                        {
                         
                            if (bill.DbfakturaOrdering != null)
                            {
                          
                                bill.OrderPrint(nzpKvar);
                              
                                bill.DbfakturaOrdering.UpdateOrderPrint();
                             
                            }


                        }
                    
                        ret.text = bill.GeuKodErc + "|" + bill.Shtrih;
                  
                   
                        
                    }
                    catch (Exception ex)
                    {
               
                        MonitorLog.WriteLog("Ошибка при выгрузке данных для отчета Формирование квитанций(процедура GetGroupDataSet), 123" +
                                            "pkod = " + bill.Pkod + ", месяц.год: " + _finder.month_ + "." + _finder.year_ + " " +
                                             ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    }
                    bill.Clear();
                }
                goodreader.Close();
                DBManager.ExecSQL(_conDb, " drop table t_fkvar_prm", false);
                DBManager.ExecSQL(_conDb, " drop table t_freasonReval", false);
                DBManager.ExecSQL(_conDb, " drop table t_fVolume", false);
                DBManager.ExecSQL(_conDb, " drop table t_fDomVolume", false);
                DBManager.ExecSQL(_conDb, " drop table t_fPerekidka", false);
                UpdateBillFon(60);
                sw.WriteLine("rrtrytry");
                sw.Close();
                return fDataSet;
            }
            catch (Exception e)
            {
                //sw.WriteLine("error5");
                MonitorLog.WriteLog("Неожиданная ошибка формирования счетов " + e.Message + " " +
                sql, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;

            }

        }







        /////////////////////////////////////////////////////////////////
        /////////////////////////отладка для самары////////////////////////
        /////////////////////////////////////////////////////////////////

        /// <summary>
        /// Сохранение параметров счета в БД
        /// </summary>
        /// <param name="table"></param>
        public void SaveSf(DataTable table)
        {

            #region Подключение к БД
            IDbConnection connDb = DBManager.GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            if (!DBManager.OpenDb(connDb, true).result)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return;
            }


            #endregion

            var sql = new StringBuilder();
            bool b = false;
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest + "prm_5 ");
            sql.Append(" WHERE nzp_prm=100000 AND is_actual<>100");
            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), connDb);
            IDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
                if (reader["val_prm"].ToString().Trim() == "1") b = true;
            reader.Close();

            if (b == false)
            {
                cmd.Dispose();
                connDb.Close();
                return;
            }


            for (int i = 0; i < table.Rows.Count; i++)
            {

                DataRow dr = table.Rows[i];
                #region Сохранение Заголовка
                sql.Remove(0, sql.Length);
                sql.Append(" insert into " + Points.Pref + DBManager.sDataAliasRest + "a_sobitsLs(month_,year_,pkod,privat, ");
                sql.Append(" ls, num_ls, fio,  uk, shtrih, geu,  adr, count_gil,count_propis, ");
                sql.Append(" pl,  pl_dom, pl_mop, count_gil_all, nomer_doc, ispolnitel, ");
                sql.Append(" sum_charge, sum_oplat, bank)  ");
                sql.Append(" values(" + dr["month_"] + "," + dr["year_"] + "," + dr["pkod"] + ",");
                sql.Append(dr["priv"].ToString().Trim() == "Приватизирована" ? "1,'" : "0,'");
                sql.Append(dr["ls"] + "', '" + dr["num_ls"] + "','" + dr["Platelchik"] + "', '" + dr["str_rekv1"] + "',");
                sql.Append(dr["vars"] + " ," + dr["ngeu"] + ",'" + dr["ulica"] + "," + dr["numdom"] + "-" +
                     dr["kvnum"] + "', " + dr["kolgil"] + "," + dr["kolgil2"] + ", ");
                sql.Append(dr["kv_pl"] + " ,  " + dr["pl_dom"] + ", " + dr["pl_mop"] + ",");
                sql.Append(dr["dom_gil"] + ",'',''," + dr["sum_ticket"] + "," + dr["sum_money"] + ",'')");

                cmd = DBManager.newDbCommand(sql.ToString(), connDb);

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при выгрузке данных для отчета Формирование квитанций(процедура GetGroupDataSet), " +
                                         ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                cmd.Dispose();


                #endregion

                #region Сохранение Услуг

                for (int j = 1; j < 20; j++)
                {
                    if (dr["name_serv" + j].ToString().Trim() != "")
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into " + Points.Pref + DBManager.sDataAliasRest + "a_sobitsLsServ(month_, year_,  pkod, ");
                        sql.Append(" serv_name, nzp_serv, ed_izmer, nzp_measure, rashod, type_rashod, rashod_odn, ");
                        sql.Append(" type_rashod_odn, tarif, sum_tarif, sum_charge, sum_tarif_all, sum_tarif_odn, ");
                        sql.Append(" sum_charge_odn, sum_charge_all, kol_odn, kol_ind, norma, norma_odn, kpkz)  ");
                        sql.Append(" values(" + dr["month_"] + "," + dr["year_"] + "," + dr["pkod"] + ",");
                        if (dr["name_serv" + j].ToString() == "Ремонт жил.помещ.")
                            sql.Append("'Ремонт жил.помещ.',2,");
                        else if (dr["name_serv" + j].ToString() == "Ремонт жил.помещ.")
                            sql.Append("'" + dr["name_serv" + j] + "',233,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Хол.водоснабж", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',6,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Водоотведение", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',7,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Отопление", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',8,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Э\\э МОП, лифт", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',11,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Т/энергия на ГВС", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',9,");
                        else if (dr["name_serv" + j].ToString() == "Радиоточка")
                            sql.Append("'" + dr["name_serv" + j] + "',12,");
                        else if (dr["name_serv" + j].ToString() == "Антенна")
                            sql.Append("'" + dr["name_serv" + j] + "',13,");
                        else if (dr["name_serv" + j].ToString() == "Наем жил.помещ.")
                            sql.Append("'" + dr["name_serv" + j] + "',15,");
                        else if (dr["name_serv" + j].ToString() == "Содерж.жил.помещ.")
                            sql.Append("'" + dr["name_serv" + j] + "',17,");
                        else if (dr["name_serv" + j].ToString() == "Выгребная яма")
                            sql.Append("'" + dr["name_serv" + j] + "',24,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Э/энергия", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',25,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Т/носитель на ГВС", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',14,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Хол.вода на ГВС", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',14,");
                        else if (dr["name_serv" + j].ToString().IndexOf("Газ", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',10,");
                        else if (dr["name_serv" + j].ToString().IndexOf("ППА", StringComparison.Ordinal) > -1)
                            sql.Append("'" + dr["name_serv" + j] + "',233,");
                        else if (dr["name_serv" + j].ToString() == "ВДГО")
                            sql.Append("'" + dr["name_serv" + j] + "',22,");
                        else if (dr["name_serv" + j].ToString() == "Услуги вахтеров")
                            sql.Append("'" + dr["name_serv" + j] + "',230,");
                        else if (dr["name_serv" + j].ToString() == "Уборка подъездов")
                            sql.Append("'" + dr["name_serv" + j] + "',18,");
                        else sql.Append("'" + dr["name_serv" + j] + "',0,");
                        if (dr["measure" + j].ToString() == "кв.метр")
                            sql.Append("'кв.метр.',1,");
                        else if (dr["measure" + j].ToString() == "с чел.в мес.")
                            sql.Append("'с чел.в мес.',2,");
                        else if (dr["measure" + j].ToString() == "куб.м")
                            sql.Append("'куб.м',3,");
                        else if (dr["measure" + j].ToString() == "Гкал.")
                            sql.Append("'Гкал',4,");
                        else if (dr["measure" + j].ToString() == "кВт*час")
                            sql.Append("'кВт*час',5,");
                        else sql.Append("'" + dr["measure" + j] + "',0,");

                        string typeRashod = "0";
                        string typeRashodOdn = "0";

                        string rashod = dr["c_calc" + j].ToString();

                        if (rashod.IndexOf("(", StringComparison.Ordinal) > 0)
                        {
                            typeRashod = rashod.Substring(rashod.IndexOf("(", StringComparison.Ordinal) + 1, 1);
                            rashod = rashod.Substring(0, rashod.IndexOf("(", StringComparison.Ordinal));
                        }

                        string rashodOdn = dr["c_calc_odn" + j].ToString();
                        if (rashodOdn.IndexOf("(", StringComparison.Ordinal) > 0)
                        {
                            typeRashodOdn = rashodOdn.Substring(rashodOdn.IndexOf("(", StringComparison.Ordinal) + 1, 1);
                            rashodOdn = rashodOdn.Substring(0, rashodOdn.IndexOf("(", StringComparison.Ordinal));
                        }

                        if (rashod == "") rashod = "0";
                        if (rashodOdn == "") rashodOdn = "0";
                        string rsumTarif = dr["rsum_tarif" + j].ToString() == "" ? "0" : dr["rsum_tarif" + j].ToString();
                        string rsumTarifOdn = dr["rsum_tarif_odn" + j].ToString() == "" ? "0" : dr["rsum_tarif_odn" + j].ToString();

                        string rsumTarifAll = dr["rsum_tarif_all" + j].ToString() == "" ? "0" : dr["rsum_tarif_all" + j].ToString();


                        string sumCharge = dr["sum_charge" + j].ToString() == "" ? "0" : dr["sum_charge" + j].ToString();
                        string sumChargeOdn = dr["sum_charge_odn" + j].ToString() == "" ? "0" : dr["sum_charge_odn" + j].ToString();
                        string sumChargeAll = dr["sum_charge_all" + j].ToString() == "" ? "0" : dr["sum_charge_all" + j].ToString();
                        string tarif = dr["tarif" + j].ToString() == "" ? "0" : dr["tarif" + j].ToString();

                        string rashDpuPu = dr["rash_dpu_pu" + j].ToString() == "" ? "0" : dr["rash_dpu_pu" + j].ToString();

                        string rashDpuOdn = dr["rash_dpu_odn" + j].ToString() == "" ? "0" : dr["rash_dpu_odn" + j].ToString();
                        string rashNorm = dr["rash_norm" + j].ToString() == "" ? "0" : dr["rash_norm" + j].ToString();
                        string rashNormOdn = dr["rash_norm_odn" + j].ToString() == "" ? "0" : dr["rash_norm_odn" + j].ToString();
                        string rashPu = dr["rash_pu" + j].ToString() == "" ? "0" : dr["rash_pu" + j].ToString();


                        sql.Append(rashod + ", '" + typeRashod + "', '" + rashodOdn + "',");
                        sql.Append(typeRashodOdn + " ," + tarif + ",'" + rsumTarif + "','");
                        sql.Append(sumCharge + "','" + rsumTarifAll + "', '");
                        sql.Append(rsumTarifOdn + "','" + sumChargeOdn + "', '");
                        sql.Append(sumChargeAll + "' ,  '" + rashDpuPu + "', '");
                        sql.Append(rashDpuOdn + "','");
                        sql.Append(rashNorm + "','" + rashNormOdn + "','" + rashPu + "')");



                        cmd = DBManager.newDbCommand(sql.ToString(), connDb);

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog("Ошибка при выгрузке данных для отчета Формирование квитанций(процедура GetGroupDataSet), " +
                         ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        }
                        cmd.Dispose();
                    }
                }
                #endregion
            }
            // cmd.Dispose();
            connDb.Close();

        }

        public void Close()
        {

        }
    }

}



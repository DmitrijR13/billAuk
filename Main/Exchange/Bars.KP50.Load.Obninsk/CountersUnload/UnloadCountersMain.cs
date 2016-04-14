using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bars.KP50.Load.Obninsk.CountersUnload.Interfaces;
using Bars.KP50.Report;
using FastReport;
using Globals.SOURCE.Container;
using Newtonsoft.Json;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using ReportParams = Bars.KP50.Report.Base.ReportParams;

namespace Bars.KP50.Load.Obninsk.CountersUnload
{
    public class UnloadCountersMain:BaseSqlLoad
    {
        private IUnloadCounters unloadCounters;
        private List<int> list_nzp_wp= new List<int>(); 
        public UnloadCountersMain(IUnloadCounters unloadCounters)
        {
            this.unloadCounters = unloadCounters;
        }
        public override string Name
        {
            get { return unloadCounters.Name; }
        }

        public override string Description
        {
            get { return  unloadCounters.Description; }
        }

     

        public override void LoadData()
        {
            StreamWriter writer=null;
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            try
            {
                CultureInfo ci = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
                ci.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = ci;
                GetFileName();
                FileStream memstr = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));
                unloadCounters.Init(Protokol,Connection, writer,NzpExcelUtility);
                unloadCounters.Unload(list_nzp_wp);
                closeWriter(writer);
                // архивируем файл
                GetArchive(out ret);
                if (!ret.result)
                {
                    throw new Exception(ret.text);
                }
            }
            catch (Exception ex)
            {
                Protokol.AddUncorrectedRow("В процессе выгрузки произошли ошибки, см. логи");
                MonitorLog.WriteLog("Ошибка в процессе выгрузки в последних показаний ПУ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                closeWriter(writer);
            }
        }


        public override string GetProtocolName()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (!ret.result)
            {
                return String.Empty;
            }
            FastReport.Report rep = null;
            try
            {
                #region Формирование протокола

                string statusName = "Успешно";
                rep = new FastReport.Report();
                ExcelUtility.Statuses statusUnloadFile = ExcelUtility.Statuses.Success;
                if (Protokol.UnrecognizedRows.Rows.Count > 0 || Protokol.Comments.Rows.Count > 0)
                {
                    Protokol.SetProcent(100, ExcelUtility.Statuses.Success);
                    statusName = "Выгружено с ошибками";
                }
                if (Protokol.UncorrectRows.Rows.Count > 0)
                {
                    statusUnloadFile = ExcelUtility.Statuses.Failed;
                    statusName = "Не выгружено";
                }

                var env = new EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                DataSet fDataSet = new DataSet();
                fDataSet.Tables.Add(Protokol.UnrecognizedRows);
                fDataSet.Tables.Add(Protokol.Comments);
                fDataSet.Tables.Add(Protokol.UncorrectRows);
                string template = PathHelper.GetReportTemplatePath("protocol_cnt.frx");
                rep.Load(template);
                rep.RegisterData(fDataSet);
                rep.GetDataSource("comment").Enabled = true;
                rep.GetDataSource("unrecog").Enabled = true;
                rep.GetDataSource("uncorrect").Enabled = true;
                rep.SetParameterValue("status", statusName);
                rep.SetParameterValue("count_rows", Protokol.CountInsertedRows);
                rep.SetParameterValue("file_name", FileName);
                rep.SetParameterValue("protokol_title", "Протокол выгрузки последних показаний счетчиков из биллинговой системы");
                rep.Prepare();
                var exportXls = new FastReport.Export.OoXML.Excel2007Export();
                string fileName = "protocol_" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                                  DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xlsx";
                exportXls.ShowProgress = false;
                MonitorLog.WriteLog(fileName, MonitorLog.typelog.Info, 20, 201, true);

                if (!Directory.Exists(Constants.Directories.ReportDir))
                {
                    Directory.CreateDirectory(Constants.Directories.ReportDir);
                }
                exportXls.Export(rep, Path.Combine(Constants.Directories.ReportDir, fileName));
                // перенос  на ftp сервер
                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(STCLINE.KP50.Global.Constants.Directories.ReportDir + fileName);
                }
                ProtocolFileName = STCLINE.KP50.Global.Constants.Directories.ReportDir + fileName;

                #endregion

                using (var myFile = new DBMyFiles())
                {
                    // обновить мои файлы, чтобы появился файл для скачивания только  в случае успешной выгрузки 
                    if (statusUnloadFile != ExcelUtility.Statuses.Failed)
                    {
                        myFile.SetFileState(new ExcelUtility
                        {
                            nzp_exc = NzpExcelUtility,
                            status = ExcelUtility.Statuses.Success,
                            exc_path = FileName
                        });
                    }
                    // добавление протокола в мои файлы
                    ret = myFile.AddFile(new ExcelUtility
                    {
                        nzp_user = Nzp_user,
                        status = ExcelUtility.Statuses.InProcess,
                        rep_name = "Протокол выгрузки последних показаний ПУ",
                        is_shared = 1
                    });
                    if (!ret.result)
                    {
                        throw new ReportException("Ошибка добавления протокола выгрузки последних показаний ПУ");
                    }
                    int nzp_exc = ret.tag;
                    myFile.SetFileState(new ExcelUtility
                    {
                        nzp_exc = nzp_exc,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = fileName
                    });
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(unloadCounters.Name + ". Ошибка формирования протокола " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (rep != null)
                {
                    rep.Dispose();
                }
            }
            return ProtocolFileName;
        }

        protected override void InsertReestr()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            using (var myFile = new DBMyFiles())
            {
                ret = myFile.AddFile(new ExcelUtility
                {
                    nzp_user = Nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = Name,
                    is_shared = 1
                });
                if (!ret.result)
                {
                    throw new ReportException("Ошибка вставки записи в таблицу Мои файлы");
                }
                NzpExcelUtility = ret.tag;
            }
        }
        private void closeWriter(StreamWriter writer)
        {
            if (writer == null) return;
            // Не следуйте совету Resharper, если он ругается на эту строку
            if (writer.BaseStream == null) return;
            writer.Flush();
            writer.Close();
        }
        protected virtual void GetArchive(out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            try
            {
                string fileName = FileName.Replace("csv", "7z");

                if (File.Exists(FileName))
                    if (!Archive.GetInstance(ArchiveFormat.SevenZip).Compress(fileName, new[] { FileName }, true))
                        throw new Exception("Ошибка при архивации файла");

                //FileName = fileName;

                if (InputOutput.useFtp)
                {
                    if (File.Exists(fileName))
                        FileName =
                            InputOutput.SaveOutputFile(fileName);
                }
                else
                {
                    FileName = fileName;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка архивирования файла " + ex, MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, "Ошибка при архивации файла");
            }
        }
        private void GetFileName()
        {
            string fileName = "LastValPU" + "_" +
                              DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".csv";
            FileName = Path.Combine(Constants.Directories.ReportDir, fileName);
            if (FileName == null)
            {
                throw new Exception("Выгрузка счетчиков. Ошибка формирования имени файла в методе GetFileName()");
            }
        }
        protected override byte[] Template
        {
            get {return null; }
        }

        public override List<UserParam> GetUserParams()
        {
            return null;
        }

        protected override void PrepareParams()
        {
        }
        protected override NameValueCollection GetNameValueCollection(FilesImported finder)
        {
            list_nzp_wp = finder.ListNzpWp;
            return new NameValueCollection
            {
                {
                    "SystemParams", JsonConvert.SerializeObject( new
                    {
                        NzpUser = finder.nzp_user,
                    })
                }
            };
        }

        public override void SetLoadParameters(NameValueCollection reportParameters)
        {
            Container = Container ?? IocContainer.Current;
            ReportParams = ReportParams ?? new ReportParams(Container);
            SystemParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(reportParameters["SystemParams"]);

            Nzp_user = SystemParams.ContainsKey("NzpUser")
                ? Convert.ToInt32(SystemParams["NzpUser"] ?? "-1")
                : -1;
            int nzpUser = SystemParams.ContainsKey("NzpUser") ? Int32.Parse(SystemParams["NzpUser"]) : -1;
            var dbAdmin = new DbAdminClient();
            ReportParams.User = dbAdmin.GetUsers(new User { nzp_user = nzpUser, nzpuser = nzpUser }).GetData().FirstOrDefault() ??
                                new BaseUser();

            var db = new DbUserClient();
            ReportParams.User.RolesVal = new List<_RolesVal>();
            ReportParams.User.nzp_user = nzpUser;
            db.GetRolesVal(Constants.roleReport, nzpUser, ref ReportParams.User.RolesVal);
            db.Close();

            if (string.IsNullOrEmpty(Constants.cons_Kernel))
            {
                throw new ReportException("Не указана строка соединения");
            }
        }

        protected override void DropTempTable()
        {
           if (unloadCounters!=null) unloadCounters.Dispose();
        }
    }
}

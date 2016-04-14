using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Bars.KP50.Load.Obninsk.CountersLoad.Interfaces;
using Bars.KP50.Report;
using FastReport;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Load.Obninsk
{
    /// <summary>
    /// Класс для загрузки счетчиков
    /// </summary>
   public sealed class CountersLoadMain:BaseSqlLoad
   {
       private ICountersLoad cntLoad;
       public CountersLoadMain(ICountersLoad cntLoad)
       {
           this.cntLoad = cntLoad;
       }

       public override string Name
        {
            get { return cntLoad.Name+" из файла "+Path.GetFileName(FileName); }
        }

        public override string Description
        {
            get { return cntLoad.Description + " из файла " + Path.GetFileName(FileName); }
        }

        public override List<UserParam> GetUserParams()
        {
          return  new List<UserParam>();
        }

        /// <summary>
        /// основной метод загрузки счетчиков
        /// </summary>
        public override void LoadData()
        {
            cntLoad.Init(Connection, Protokol, SetProcessPercentEvArgs, Nzp_user);
            // при неудачном разборе строк в базу ничего не сохраняем
            if (!cntLoad.ParseFileRows(TemporaryFileName, NzpLoad)) return;
            cntLoad.SaveCountersInDB(FileName);
        }
        /// <summary>
       /// Сформировать протокол
       /// </summary>
       /// <returns></returns>
        public override string GetProtocolName()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (!ret.result)
            {
                return String.Empty;
            }

            #region Формирование протокола

            FastReport.Report rep = null;
            try
            {
                var myFile = new DBMyFiles();
                string statusName = "Успешно";
                rep = new FastReport.Report();
                if (Protokol.UnrecognizedRows.Rows.Count > 0 || Protokol.Comments.Rows.Count > 0)
                {
                    Protokol.SetProcent(100, ExcelUtility.Statuses.Failed);
                    statusName = "Загружено с ошибками";
                }
                if (Protokol.UncorrectRows.Rows.Count > 0)
                {
                    statusName = "Не загружено";
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
                rep.SetParameterValue("protokol_title", "Протокол загрузки счетчиков в биллинговую систему");
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
                ExecSQL("UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                        " SET nzp_exc=" + NzpExcelUtility +
                        " WHERE nzp_load=" + NzpLoad);

                myFile.SetFileState(new ExcelUtility
                {
                    nzp_exc = NzpExcelUtility,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = fileName
                });
                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(cntLoad.Name+ ". Ошибка формирования протокола " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                   (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (rep != null)
                {
                    rep.Dispose();
                }
            }
            #endregion

            return ProtocolFileName;
        }
       /// <summary>
       /// Удаляем временные таблицы
       /// </summary>
        protected override void DropTempTable()
        {
            if (cntLoad != null)
            {
                cntLoad.Dispose();
            }
        }

        protected override byte[] Template
        {
            get { return new byte[1]; }
        }

        protected override void PrepareParams()
        {
           
        }
    }
}

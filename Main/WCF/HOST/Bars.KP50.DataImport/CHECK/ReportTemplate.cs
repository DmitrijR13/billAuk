using System;
using System.Data;
using System.IO;
using FastReport;
using FastReport.Export.OoXML;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK
{
    public abstract class ReportTemplate
    {
        protected string reportFrxSource;
        protected string fileName;
        protected string fullFileName;
        protected IDbConnection conn_db;
        protected _Point Bank;
        protected User User;
        protected int Year;
        protected int Month;

        public Returns GetReport()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            try
            {
                int nzpExc;
                ExcelRepClient excelRep = new ExcelRepClient();
                ret = excelRep.AddMyFile(new ExcelUtility()
                {
                    nzp_user = User.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = fullFileName,
                    is_shared = 1
                });
                if (!ret.result)
                {
                    return new Returns(false, "Ошибка постановки отчета", -1);
                }
                nzpExc = ret.tag;

                DropTempTable();
                CreateTempTable();
                FillTempTable();

                DataSet ds_rep = AddDataSource();

                FastReport.Report rep = new FastReport.Report();
                rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + reportFrxSource);

                SetParamValues(rep);

                rep.RegisterData(ds_rep);

                FastReport.EnvironmentSettings env = new EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                rep.Prepare();

                fileName = Path.GetFileNameWithoutExtension(fileName) + ".xlsx";
                string filePath = Constants.Directories.ReportDir + fileName;
                (new Excel2007Export()).Export(rep, filePath);
                // rep.SavePrepared(filePath);

                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(filePath);
                }

                var myFile = new DBMyFiles();
                myFile.SetFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = fileName
                });

                SetNzpExc(nzpExc);


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета \"" + fullFileName + "\" " + ex.Message,
                    MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = "Ошибка формирования отчета";
                ret.tag = -1;
            }
            finally
            {
                DropTempTable();
            }

            return ret;
        }
      
        /// <summary>
        /// создание временных таблиц
        /// </summary>
        protected abstract void CreateTempTable();
        /// <summary>
        /// удаление временных таблиц
        /// </summary>
        protected abstract void DropTempTable();
        /// <summary>
        /// подтягивание данных для отчета во временные таблицы
        /// </summary>
        protected abstract void FillTempTable();
        /// <summary>
        /// Заполнение параметров отчета
        /// </summary>
        /// <param name="rep"></param>
        protected abstract void SetParamValues(FastReport.Report rep);
        /// <summary>
        /// заполнение DataSet для отчета
        /// </summary>
        /// <returns></returns>
        protected abstract DataSet AddDataSource();
        /// <summary>
        /// выставление nzp_exc в ту таблицу, где собираются данные 
        /// </summary>
        /// <param name="nzp_exc"></param>
        protected abstract void SetNzpExc(int nzp_exc);
    }
}

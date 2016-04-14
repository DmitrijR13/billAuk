using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Задача на разбор файла при загрузке
    /// </summary>
    public class DisassembleFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {

            Returns ret = Utils.InitReturns();
            var finder = new FilesDisassemble();

            #region Подключение к БД

            IDbConnection conDB = DBManager.GetConnection(Constants.cons_Kernel);
            ret = DBManager.OpenDb(conDB, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                    "при  при обработке задачи разбора файла (taskDisassembleFile)",
                    MonitorLog.typelog.Error, true);
                {

                    return ret;
                }
            }

            #endregion Подключение к БД

            try
            {
                finder = JsonConvert.DeserializeObject<FilesDisassemble>(_task.parameters);
                DbDisassembleFile disFile = new DbDisassembleFile(conDB);
                ret = Utils.InitReturns();
                ret = disFile.SelectDissMethod(finder, ref ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при обработке задачи разбора файла (taskDisassembleFile)" + Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции разбора.";
                ret.result = false;
                ret.tag = -1;
            }
            finally
            {
                conDB.Close();
            }

            return ret;
            
        }

        public DisassembleFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

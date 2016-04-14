using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Расчет без перерасчета
    /// </summary>
    public class CalcWithOutRevalFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            string pref = Points.GetPref(_task.nzpt);
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(_task.nzp), pref, _task.year_,
                _task.month_, _task.year_, _task.month_, _task.nzp_user, _task.nzp_key);

            #region Заполняем параметр для paramcalc для тестового расчета
            //Сергей 16.12.2014
            paramcalc.id_bill_pref = DbCalc.GetTestCalcPref(_task.parameters);
            #endregion

            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            //расчет без перерасчета
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            paramcalc.calcfon = _task;
            //paramcalc.b_again = false;
            paramcalc.b_reval = false;
            paramcalc.b_must = false;

            /*
                    public const int taskDefault    = 0;   //полный цикл, по-умолчанию
                    public const int taskFull       = 1;   //полный расчет (CalcReportXX в отдельный процесс)
                    public const int taskSaldo      = 2;   //расчет только сальдо (CalcReportXX в отдельный процесс)
                    public const int taskCalcGil    = 101; //CalcGilXX      
                    public const int taskCalcRashod = 111; //CalcRashod     
                    public const int taskCalcNedo   = 121; //CalcNedo       
                    public const int taskCalcGku    = 131; //CalcGkuXX      
                    public const int taskCalcCharge = 141; //CalcChargeXX, после выполнения вызывает CalcReportXX
                    public const int taskCalcReport = 200; //CalcReportXX   
             
                    public bool b_gil;
                    public bool b_rashod;
                    public bool b_nedo;
                    public bool b_gku;
                    public bool b_charge;
                    public bool b_report;
                    public bool b_again;
                    public bool b_reval;
                    public bool b_must;
            */

            paramcalc.b_gil = (_task.calcFull || _task.TaskType == CalcFonTask.Types.taskCalcGil);
            paramcalc.b_rashod = (_task.calcFull || _task.TaskType == CalcFonTask.Types.taskCalcRashod);
            paramcalc.b_nedo = (_task.calcFull || _task.TaskType == CalcFonTask.Types.taskCalcNedo);
            paramcalc.b_gku = (_task.calcFull || _task.TaskType == CalcFonTask.Types.taskCalcGku);
            paramcalc.b_charge = (_task.calcFull || _task.TaskType == CalcFonTask.Types.taskCalcCharge ||
                                  _task.TaskType == CalcFonTask.Types.taskSaldo);
            paramcalc.b_report = (_task.calcFull || _task.TaskType == CalcFonTask.Types.taskCalcReport ||
                                  _task.TaskType == CalcFonTask.Types.taskSaldo);

            paramcalc.b_refresh = (_task.TaskType == CalcFonTask.Types.taskRefreshAP); //обновлние АП

            if (paramcalc.b_refresh)
            {
                string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom + "/" +
                           paramcalc.calc_yy + "/" + paramcalc.calc_mm + "/" +
                           paramcalc.cur_yy + "/" + paramcalc.cur_mm;

                MonitorLog.WriteLog("Старт RefreshAP: " + s, MonitorLog.typelog.Info, 1, 2, true);


                string conn_kernel = Points.GetConnByPref(paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                Returns ret = OpenDb(conn_db, true);
                try
                {


                    if (ret.result)
                    {
                        DbAdres db = new DbAdres();
                        bool b = db.RefreshAP(conn_db, paramcalc.pref, out ret);
                        db.Close();

                        if (!b)
                        {
                            MonitorLog.WriteLog("Ошибка RefreshAP: " + ret.text, MonitorLog.typelog.Error, 222, 222,
                                true);
                            conn_db.Close();
                            return ret;
                        }
                    }

                }
                finally
                {
                    conn_db.Close();
                }

            }
            Returns ret1;
            DbCalcCharge dbc = new DbCalcCharge();
            dbc.CalcFull(paramcalc, out ret1);
            dbc.Close();
            return ret1;
        }

        public CalcWithOutRevalFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

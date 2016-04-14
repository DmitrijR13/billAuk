using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Автоматическая смена операционного дня
    /// </summary>
    public class ChangeOperDayFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            // добавить задачу на смену операционного дня
            //-------------------------------------------------------------------------------------------
            DateTime dat_when;

            if (DateTime.TryParse(_task.dat_when, out dat_when))
                dat_when = Convert.ToDateTime(_task.dat_when);
            else dat_when = DateTime.Today;

            CalcFonTask newCalcFon = new CalcFonTask(Points.GetCalcNum(0));
            newCalcFon.TaskType = CalcFonTask.Types.taskAutomaticallyChangeOperDay;
            newCalcFon.Status = FonTask.Statuses.New; //на выполнение    

            newCalcFon.txt = "Автоматическая смена операционного дня в " + dat_when.Hour.ToString("00") + ":" +
                             dat_when.Minute.ToString("00");
            newCalcFon.year_ = DateTime.Today.Year;
            newCalcFon.month_ = 0;
            newCalcFon.dat_when = dat_when.AddDays(1).ToString("yyyy-MM-dd HH:mm");

            Returns ret;
            using (DbCalcQueueClient dbCalc = new DbCalcQueueClient())
            {
                ret = dbCalc.AddTask(newCalcFon);
            }
            if (!ret.result) return ret; // return ret;

            // проверить, что операционный день можно поменять
            //-------------------------------------------------------------------------------------------
            OperDayFinder finder = new OperDayFinder();
            finder.nzp_user = 1;
            finder.mode = OperDayFinder.Mode.CloseOperDay.GetHashCode();

            string date_oper = ""; //операционный день
            string filename; //имя файла отчета
            RecordMonth cm; //расчетный месяц

            DbPack dbpack = new DbPack();
            ret = dbpack.ChangeOperDay(finder, out date_oper, out filename, out cm);

            if (!ret.result || ret.tag < 0) return ret; // return ret;

            DateTime new_date_oper;
            if (DateTime.TryParse(date_oper, out new_date_oper))
            {
              //  Points.DateOper = new_date_oper;
            }
            return ret;
        }

        public ChangeOperDayFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }

    /// <summary>
    /// Ручная смена операционного дня
    /// </summary>
    public class ManualChangeOperDayFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret = Utils.InitReturns();
            DateTime old = Points.DateOper;
            var finder = new OperDayFinder();
            string date_oper = "";
            // проверить, что операционный день можно поменять
            //-------------------------------------------------------------------------------------------
            try
            {
                finder = JsonConvert.DeserializeObject<OperDayFinder>(_task.parameters);
                using (DbCalcQueueClient dbc = new DbCalcQueueClient(_task))
                {
                    using (var db = new DbPack())
                    {
                        ret = db.ChangeOperDay(finder, out date_oper, dbc.SetTaskProgress);
                    }
                }
                DateTime new_date_oper;
              
                if (DateTime.TryParse(date_oper, out new_date_oper))
                {
                    if (new_date_oper == old)
                    {
                        if (ret.tag < 0) _task.txt = ". " + ret.text;
                        else _task.txt = ". Смены операционного дня не произошло. Смотрите отчет на странице Контроль распределения оплат.";
                    }
                    else
                        _task.txt = ". Операционный день изменился на " + new_date_oper.ToShortDateString() + ".";
                }
                else
                {
                    if (ret.tag < 0) _task.txt = ". " + ret.text;
                    else _task.txt = ". Смены операционного дня не произошло. Смотрите отчет на странице Контроль распределения оплат.";
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при проверках перед операционного дня (ChangeOperDay)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции проверки перед закрытием операционного дня.";
                ret.result = false;
                ret.tag = -1;
            }
            return ret;
        }

        public ManualChangeOperDayFonTask(CalcFonTask task)
            : base(task)
        {

        }

    }

    /// <summary>
    /// Ручная смена операционного дня
    /// </summary>
    public class StartControlPaysFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret = Utils.InitReturns();
            var finder = new Payments();
            // проверить, что операционный день можно поменять
            //-------------------------------------------------------------------------------------------
            try
            {
                finder = JsonConvert.DeserializeObject<Payments>(_task.parameters);
                ret = Utils.InitReturns();
                using (DbCalcQueueClient dbc = new DbCalcQueueClient(_task))
                {
                    using (var db = new DbPack())
                    {
                        db.GenConDistrPaymentsPDF(finder, dbc.SetTaskProgress);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при проверках перед закрытием месяца (CheckBeforeClosingMonth)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции проверки перед закрытием месяца.";
                ret.result = false;
                ret.tag = -1;
            }
            return ret;
        }

        public StartControlPaysFonTask(CalcFonTask task)
            : base(task)
        {

        }

    }
}

using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Расчет одного лицевого счета
    /// </summary>
    public class CalcLsFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            
            string pref = Points.GetPref(_task.nzpt);

            var paramcalc = new CalcTypes.ParamCalc(Convert.ToInt32(_task.nzp), 0,
                pref, _task.year_, _task.month_, _task.year_, _task.month_)
            {
                id_bill_pref = DbCalc.GetTestCalcPref(_task.parameters),
                b_report = false,
                b_reval = true,
                b_must = true
            };

            Returns ret;
            using (var db = new DbCalcCharge())
            {
                db.CalcRevalXX(paramcalc, out ret);
            }
            return ret;
        }

        public CalcLsFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

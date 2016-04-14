using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Расчет с перерасчетом
    /// </summary>
    public class CalcWithRevalFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            string pref = Points.GetPref(_task.nzpt);
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(_task.nzp), pref, _task.year_,
                _task.month_, _task.year_, _task.month_, _task.nzp_user, _task.nzp_key);

            #region Заполняем параметр для paramcalc для тестового расчета
            //Сергей 16.12.2014
            paramcalc.id_bill_pref = DbCalc.GetTestCalcPref(_task.parameters);
            #endregion
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            //расчет с перерасчетом
            //+++++++++++++++++++++++++++++++++++++++++++++++++++

            //paramcalc.b_again = false;
            paramcalc.b_report = false;
            paramcalc.b_reval = true;
            paramcalc.b_must = true;
            DbCalcCharge db = new DbCalcCharge();
            bool b = db.CalcRevalXX(paramcalc, out ret);
            db.Close();
            return ret;
        }


        public CalcWithRevalFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Удаление реестра перекидок
    /// </summary>
    public class DeleteReestrFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (var db = new DbCharge())
            {
                var finder = new ParamsForGroupPerekidki
                {
                    nzp_reestr = Convert.ToInt32(_task.nzp),
                    nzp_user = 1,
                    dat_uchet = (new DateTime(_task.year_, _task.month_, 1)).ToShortDateString()
                };
                ret = db.DeleteFromReestrPerekidok(finder);
            }
            return ret;
        }

        public DeleteReestrFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }


    /// <summary>
    /// Расчет начислений для реестра перекидок
    /// </summary>
    public class CalcChargeForReestrPerekidok : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;

            using (IDbConnection connDB = GetConnection(Constants.cons_Kernel))
            {
                ret = OpenDb(connDB, true);
                if (!ret.result) return ret; // return ret;

                var reestr = new ParamsForGroupPerekidki
                {
                    nzp_reestr = Convert.ToInt32(_task.nzp),
                    dat_uchet = (new DateTime(_task.year_, _task.month_, 1)).ToShortDateString()
                };

                using (var dbc = new DbCalcQueueClient(_task))
                {
                    using (var db = new DbCalcCharge())
                    {
                        db.CalcChargeXXForReestrPerekidok(connDB, null, reestr, dbc.SetTaskProgress);
                    }
                }

            }

            return ret;
        }

        public CalcChargeForReestrPerekidok(CalcFonTask task)
            : base(task)
        {

        }

    }
}

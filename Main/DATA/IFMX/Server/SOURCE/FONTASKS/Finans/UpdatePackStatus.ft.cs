using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Обновление статуса пачки в финансах
    /// </summary>
    public class UpdatePackStatusFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
           
            Returns ret = Utils.InitReturns();
            using (var dbPack = new DbPack())
            {
                var finder = new PackFinder
                {
                    nzp_user = 1,
                    year_ = _task.year_,
                    nzp_pack = Convert.ToInt32(_task.nzp)
                };

                ReturnsType ret2 = dbPack.Upd_SUM_RASP_and_SUM_NRASP(finder);
                ret.result = ret2.result;
                ret.sql_error = ret2.sql_error;
                ret.text = ret2.text;
                ret.tag = ret2.tag;
            }
            return ret;
        }

        public UpdatePackStatusFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

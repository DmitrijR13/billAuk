using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;



namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Расчет одного лицевого счета
    /// </summary>
    public class CalcAnalizFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            // подсчет аналитики
            int year_ = _task.year_;

            using (var dbAnaliz = new DbAnaliz())
            {
                dbAnaliz.LoadAnaliz1(out ret, year_, true);
            }
            return ret;
        }

        public CalcAnalizFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Базовый класс фоновой задачи
    /// </summary>
    public class BaseFonTask : DataBaseHead
    {
        protected CalcFonTask _task;
        public virtual Returns StartTask()
        {
            return new Returns(true);
        }

        public BaseFonTask(CalcFonTask task)
        {
            _task = task;
        }

        public CalcFonTask GetTask()
        {
            return _task;
        }

    }
}

using Bars.KP50.DB.Admin.Source.OrderSequence;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Упорядовачивание последовательностей первичных ключей в БД
    /// </summary>
    public class OrderSequencesFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {

            Returns ret;
            // упорядочивание последовательностей
            using (OrderingSequence db = new OrderingSequence())
            {
                ret = db.DoOrderSequences();
            }
            return ret;
        }

        public OrderSequencesFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }


    /// <summary>
    /// Добавление первичных ключей
    /// </summary>
    public class AddPrimaryKeyFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (var db = new DbAdmin())
            {
                ret = db.AddPrimaryKey(_task.nzp_user);
            }
            return ret;
        }

        public AddPrimaryKeyFonTask(CalcFonTask task)
            : base(task)
        {

        }

    }


    /// <summary>
    /// Установка индексов в БД
    /// </summary>
    public class AddIndexesFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (var db = new DbAdmin())
            {
                ret = db.AddIndexes(_task.nzp_user, _task.parameters);
            }
            return ret;
        }

        public AddIndexesFonTask(CalcFonTask task)
            : base(task)
        {

        }

    }


    /// <summary>
    /// Установка внешних ключей в БД
    /// </summary>
    public class AddForeignKeyFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (var db = new DbAdmin())
            {
                ret = db.AddPrimaryKey(_task.nzp_user);
            }
            return ret;
        }

        public AddForeignKeyFonTask(CalcFonTask task)
            : base(task)
        {

        }

    }


    /// <summary>
    /// 
    /// </summary>
    public class RefreshLsTarifFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (var db = new Supg())
            {
                ret.result = db.FillLSTarif(out ret);
            }
            return ret;
        }

        public RefreshLsTarifFonTask(CalcFonTask task)
            : base(task)
        {

        }

    }
}

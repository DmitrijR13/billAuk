using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Обновление адресного пространства
    /// </summary>
    public class UpdateAdressFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
          // обновление адресного пространства
            Finder finder = new Finder();
            Returns ret;
            using (DbAdres dbAdres = new DbAdres())
            {
                ret = dbAdres.RefreshAP(finder);
            }
            return ret;
        }

        public UpdateAdressFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

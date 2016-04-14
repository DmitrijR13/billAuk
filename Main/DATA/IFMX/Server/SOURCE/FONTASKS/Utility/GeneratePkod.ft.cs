using System.Globalization;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Генерация платежных кодов
    /// </summary>
    public class GeneratePkod : BaseFonTask
    {
        public override Returns StartTask()
        {

            Returns ret;
            using (var db = new DbAdres())
            {
                if (GlobalSettings.NewGeneratePkodMode)
                {
                    var finder = new Finder
                    {
                        nzp_user = _task.nzp_user,
                        dopFind = new List<string> {_task.QueueNumber + _task.nzp_key.ToString(CultureInfo.InvariantCulture)}
                    };
                    db.NewGeneratePkod(finder);
                }
                else
                {
                    db.GeneratePkodFon(new Finder {nzp_user = _task.nzp_user});
                }

                using (IDbConnection conndb = GetConnection(Constants.cons_Kernel))
                {
                    ret = OpenDb(conndb, true);
                    if (!ret.result) return ret; // return ret;

                    ExecSQL(conndb, "drop table " + sDefaultSchema + ".t" + _task.QueueNumber + _task.nzp_key, false);
                }
            }
            return ret;
        }

        public GeneratePkod(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}

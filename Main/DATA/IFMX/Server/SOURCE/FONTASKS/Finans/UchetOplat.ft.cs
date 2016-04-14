using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Учет оплат по территории
    /// </summary>
    public class UchetOplatForArea : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;

            using (IDbConnection connDB = GetConnection(Constants.cons_Kernel))
            {
                ret = OpenDb(connDB, true);
                if (!ret.result) return ret; // return ret ;

                var charge = new Charge
                {
                    nzp_area = Convert.ToInt32(_task.nzp),
                    year_ = _task.year_,
                    month_ = _task.month_
                };

                using (var dbc = new DbCalcQueueClient(_task))
                {
                    using (var db = new DbCalcCharge())
                    {
                        db.CalcChargeXXUchetOplatForArea(connDB, null, charge, dbc.SetTaskProgress);
                    }
                }

            }
            return ret;
        }

        public UchetOplatForArea(CalcFonTask task)
            : base(task)
        {
            
        }

    }


    /// <summary>
    /// Учет оплат для банка данных ?
    /// </summary>
    public class CalcChargeUchetOplatForBank : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (IDbConnection connDB = GetConnection(Constants.cons_Kernel))
            {
                ret = OpenDb(connDB, true);

                if (!ret.result) return ret; // return ret;

                Pack fPack = JsonConvert.DeserializeObject<Pack>(_task.parameters);

                using (var db = new DbCalcCharge())
                {
                    db.CalcChargeXXUchetOplatForBank(connDB, Points.GetPref(_task.nzpt), fPack);
                }

            }

            return ret;
        }

        public CalcChargeUchetOplatForBank(CalcFonTask task)
            : base(task)
        {

        }

    }
}

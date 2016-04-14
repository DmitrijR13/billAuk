using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class OverPayments : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            var finder = JsonConvert.DeserializeObject<OverPaymentsParams>(_task.parameters);
            using (DbCalcQueueClient dbc = new DbCalcQueueClient(_task))
            {
                using (var db = new DbCalcPack())
                {
                    ret = db.InsertOverPayments(finder, dbc.SetTaskProgress);
                }
            }

            return ret;
        }

        public OverPayments(CalcFonTask task)
            : base(task)
        {
            
        }
    }

    public class DistrOverPayments : BaseFonTask
    {
        public override Returns StartTask()
        {
           Returns ret;
            //var finder = JsonConvert.DeserializeObject<DistrOverPaymentsParams>(_task.parameters); 
            ////using (DbSprav db = new DbSprav())
            ////{
            ////    ret = db.PrepareOverpaymentDataForDistrib();
            ////}
            ////if (!ret.result) return ret;
            //using (var dbc = new DbCalcPack())
            //{
            //    ret = dbc.DistribOverPayments(finder);
            //}
            //return ret;

            var finder = JsonConvert.DeserializeObject<DistrOverPaymentsParams>(_task.parameters);
            using (DbCalcQueueClient dbc1 = new DbCalcQueueClient(_task))
            {
                using (var dbc = new DbCalcPack())
                {
                    ret = dbc.DistribOverPayments(finder, dbc1.SetTaskProgress);
                    if (ret.result)
                    {
                        ret = dbc.GetCurOverPaymentsProcId();
                        if (!ret.result)
                            MonitorLog.WriteLog("Ошибка проставления статуса calcfon.TaskType == " +
                                                "CalcFonTask.Types.taskBalanceRedistr " + ret.text,
                                MonitorLog.typelog.Error, true);
                        else
                        {
                            var finderFon = new OverpaymentStatusFinder
                            {
                                id = ret.tag,
                                nzp_status = (int)OverpaymentStatusFinder.Statuses.overpDistrib,
                                nzp_fon_distrib = _task.nzp_key,
                                nzp_user = finder.nzp_user,
                                is_actual = 100
                            };
                            ret = dbc.SetStatusOverpaymentManProc(finderFon);
                        }
                    }
                }
            }
            return ret;
        }

        public DistrOverPayments(CalcFonTask task)
            : base(task)
        {

        }
    }
}

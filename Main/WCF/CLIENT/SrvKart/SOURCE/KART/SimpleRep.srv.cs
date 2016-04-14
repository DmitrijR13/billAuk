using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using STCLINE.KP50.REPORT;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    public class srv_SimpleRep :  I_SimpleRep
    {
     

        /// <summary>
        /// Отчет:Выписка из лицевого счета по счетчикам для Самары
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetCountersSprav(ReportPrm prm)
        {

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту

                cli_SimpleRep cli = new cli_SimpleRep(WCFParams.AdresWcfHost.CurT_Server);
                DataTable DT = cli.GetCountersSprav(prm);
                return DT;
            }
            else
            {
                try
                {
                    DataTable DT;
                    using (SimpleRep simpleRep = new SimpleRep())
                    {
                        DT = simpleRep.GetCountersSprav(prm);
                    }
                    return DT;
                }
                catch (Exception ex)
                {
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetCountersSprav() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            }
        
        }


        /// <summary>
        /// Возвращает набор данных для неотложного отчета
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetReportTable(ReportPrm prm)
        {

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту

                cli_SimpleRep cli = new cli_SimpleRep(WCFParams.AdresWcfHost.CurT_Server);
                DataTable DT = cli.GetReportTable(prm);
                return DT;
            }
            else
            {
                try
                {
                    DataTable DT;
                    using (SimpleRep simpleRep = new SimpleRep())
                    {
                        DT = simpleRep.GetReportTable(prm);
                    }
                    return DT;
                }
                catch (Exception ex)
                {
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetReportTable() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            }
        
        }


       
    }
}

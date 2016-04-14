using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using STCLINE.KP50.SrvBase;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            if (SrvRun.ConsolArgs(args)) return;

            SrvRun.srMode = SrvRun.enSrvRun.sr_Host;
            //SrvRun.httpTest = true;

            SrvRun.Broker = SrvRun.enBroker.Multi; //

            SrvRun.StartHostProgram(args);
            Console.WriteLine("Сервис остановлен, нажмите ВВОД");
            Console.ReadKey();
            MonitorLog.Close("Остановка хостинга");
        }
    }


}

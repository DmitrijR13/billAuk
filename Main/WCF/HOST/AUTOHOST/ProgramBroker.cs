using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using STCLINE.KP50.Server;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Host
{
    class ProgramBroker
    {
        static void Main(string[] args)
        {
            if (SrvRun.ConsolArgs(args)) return;

            SrvRun.MessageOutputMode = SrvRun.MessageOutputModes.Console;
            //SrvRun.httpTest = true;

            SrvRun.ProgramRole = SrvRun.ProgramRoles.Broker; //хост-посредник

            SrvRun.StartHostProgram();
            Console.WriteLine("Сервис остановлен, нажмите ВВОД");
            Console.ReadKey();
            MonitorLog.Close("Остановка хостинга");
        }
    }


}

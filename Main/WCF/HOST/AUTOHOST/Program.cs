using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using STCLINE.KP50.Server;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            if (SrvRun.ConsolArgs(args)) return;

            SrvRun.ProgramRole = SrvRun.ProgramRoles.Host;

            SrvRun.StartHostProgram();

            Console.WriteLine("");
            Console.WriteLine("Работа завершена. Нажмите любую клавишу для выхода...");

            Console.ReadKey();

            MonitorLog.Close("Остановка хостинга");
        }
    }


}

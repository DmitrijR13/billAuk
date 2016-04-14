using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

using STCLINE.KP50.Server;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.WinSrv
{
    public partial class WinSrv : ServiceBase
    {
        public WinSrv()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SrvRun.MessageOutputMode = SrvRun.MessageOutputModes.File;
            SrvRun.ProgramRole = SrvRun.ProgramRoles.Host;
            SrvRun.StartHostProgram();
        }

        protected override void OnStop()
        {
            SrvRun.TaskStop();
            MonitorLog.Close("Остановка хостинга");
        }
    }
}

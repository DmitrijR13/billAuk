using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using STCLINE.KP50.Global;
using STCLINE.KP50.WinLogin;

namespace STCLINE.KP50.HostMan
{
    static class ProgramHostMan
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //MonitorLog.StartLog("STCLINE.KP50.HostMan", "Запуск хост-менеджера");

            FrmLogin login = new FrmLogin();
            login.ShowDialog();
            bool access = login.access;
            login.Dispose();

            if (access) Application.Run(new FrmHostMan());
        }
    }
}

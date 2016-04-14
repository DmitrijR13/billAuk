using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BARS
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new ServiceTest());
            Application.Run(new TestCounterService());
        }
    }
}

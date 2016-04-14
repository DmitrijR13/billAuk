using System;
using System.Windows.Forms;


namespace webroles
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Общее искл:"+ex.Message);
            }
           
           
 
        }
    }
}

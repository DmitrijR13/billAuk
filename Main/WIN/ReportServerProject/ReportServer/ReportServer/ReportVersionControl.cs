using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace ReportServer
{
    /// <summary>
    /// класс для работы с версиями отчетов
    /// </summary>
    public static class ReportVersionControl
    {
        /// <summary>
        /// справочник отчетов
        /// </summary>
        public static Dictionary<int, Assembly> reportLibrary = new Dictionary<int, Assembly>();

        /// <summary>
        /// обновляет реализацию отчетов в памяти
        /// </summary>
        public static void AddReportImpl(Dictionary<int, string> newReports)
        {
            foreach (KeyValuePair<int, string> report in newReports)
            {
                //загрузка реализации отчета в базу
                Assembly classLibrary = null;
                using (FileStream fs = File.Open(Directory.GetCurrentDirectory() + "/dlls/" + report.Value + ".dll", FileMode.Open))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        int read = 0;
                        while ((read = fs.Read(buffer, 0, 1024)) > 0)
                            ms.Write(buffer, 0, read);
                        classLibrary = Assembly.Load(ms.ToArray());

                        //в случае успеха обновляем ссылку на реализацию отчета, либо добавляем реализацию
                        if (reportLibrary.ContainsKey(report.Key))
                            reportLibrary[report.Key] = classLibrary;
                        else
                            reportLibrary.Add(report.Key, classLibrary);
                    }
                }
            }
        }

        /// <summary>
        /// обновляет реализацию отчета в памяти
        /// </summary>
        public static void AddReportImpl(int idReport, string dllName)
        {

            //загрузка реализации отчета в базу
            Assembly classLibrary = null;
            using (FileStream fs = File.Open(Directory.GetCurrentDirectory() + "/dlls/" + dllName + ".dll", FileMode.Open))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int read = 0;
                    while ((read = fs.Read(buffer, 0, 1024)) > 0)
                        ms.Write(buffer, 0, read);
                    classLibrary = Assembly.Load(ms.ToArray());

                    //в случае успеха обновляем ссылку на реализацию отчета, либо добавляем реализацию
                    if (reportLibrary.ContainsKey(idReport))
                        reportLibrary[idReport] = classLibrary;
                    else
                        reportLibrary.Add(idReport, classLibrary);
                }
            }
        }
    }
}

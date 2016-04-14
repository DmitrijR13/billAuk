using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using System.Diagnostics;
using System.Threading;
using IntfClassLibrary;
using System.Reflection;
using System.IO;
using IBM.Data.Informix;


namespace ReportServer
{
    public static class PrepareReport
    {
        /// <summary>
        /// процедура выполнения отчета
        /// </summary>
        /// <returns></returns>
        public static void Prepare(ReportParams report)
        {
            IfxConnection connWeb = null;
            IfxConnection connKernel = null;
            try
            {
                //создание отчета
                connWeb = new IfxConnection(report.reportFinder.connWebString);
                connKernel = new IfxConnection(report.reportFinder.connKernelString);
                Type type = (Type)ReportVersionControl.reportLibrary[report.id].GetExportedTypes()[0];
                if (type.GetInterface("IntfClass") != null)
                ((IntfClass)Activator.CreateInstance(type)).runReport((object)report, connWeb, connKernel);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ReportManager.sSource, "Ошибка формирования отчета, функция Prepare в проекте ReportServer: " + ex.Message, EventLogEntryType.Error);
                //обновление статуса отчета
                _DBReport db = new _DBReport();
                db.UpdateReportStatus(report, -1, ReportManager.connWeb);
                db.UpdateReportStatusOldTable(report, -1, ReportManager.connWeb, false);
            }
            finally
            {
                if (connWeb != null)
                    connWeb.Close();
                if (connKernel != null)
                    connKernel.Close();
            }
        }
    }
}



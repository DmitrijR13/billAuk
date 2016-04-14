using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBM.Data.Informix;

namespace ReportServer.INTF
{
    public interface IntfClass
    {
        /// <summary>
        /// вызов отчета на выполнение
        /// </summary>
        /// <param name="reportObject">объект отчета</param>
        /// <param name="conn_web">соединение с web базой</param>
        /// <param name="conn_db">соединение с основной БД</param>
        void runReport(object reportObject, IfxConnection conn_web, IfxConnection conn_db);
    }
}

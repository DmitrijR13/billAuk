using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using STCLINE.KP50.Global;
using System.Data;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_SimpleRep
    {

        [OperationContract]
        DataTable GetCountersSprav(ReportPrm prm);

        [OperationContract]
        DataTable GetReportTable(ReportPrm prm);

       
    }
    
}

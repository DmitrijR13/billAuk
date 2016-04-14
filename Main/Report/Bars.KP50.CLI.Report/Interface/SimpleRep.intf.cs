using STCLINE.KP50.Global;
using System;
using System.Data;
using System.ServiceModel;

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

    public interface ISimpleRepRemoteObject : I_SimpleRep, IDisposable { }
}

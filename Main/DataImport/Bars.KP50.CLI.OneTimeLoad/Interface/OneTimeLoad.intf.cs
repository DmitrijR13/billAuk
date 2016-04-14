
using STCLINE.KP50.Global;
using System;
using System.ServiceModel;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_OneTimeLoad
    {
        [OperationContract]
        Returns UploadUESCharge(FilesImported finder);

        [OperationContract]
        Returns UploadMURCPayment(FilesImported finder);

        [OperationContract]
        Returns ReadReestrFromCbb(FilesImported finderpack, FilesImported finder, string connectionString);
    }

    public interface IOneTimeLoadRemoteObject : I_OneTimeLoad, IDisposable { }
}

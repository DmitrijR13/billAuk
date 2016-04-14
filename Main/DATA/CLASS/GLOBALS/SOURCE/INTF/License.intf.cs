using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using STCLINE.KP50.Global;
using System.ComponentModel;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_License
    {
        [OperationContract]
        Returns CheckLicense(int nzp_user, string key);

        [OperationContract]
        string GetRequestKey(int nzp_user);
    }
}

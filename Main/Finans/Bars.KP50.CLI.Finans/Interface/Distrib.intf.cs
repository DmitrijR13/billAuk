using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Distrib
    {
        [OperationContract]
        List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<MoneyNaud> GetMoneyNaud(MoneyNaud finder, enSrvOper oper, out Returns ret);
        [OperationContract]
        List<MoneyDistrib> GetMoneyDistribNew(MoneyDistrib finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<MoneyNaud> GetMoneyNaudNew(MoneyNaud finder, enSrvOper oper, out Returns ret);
    }

    public interface IDistribRemoteObject : I_Distrib, IDisposable
    {
    }
}

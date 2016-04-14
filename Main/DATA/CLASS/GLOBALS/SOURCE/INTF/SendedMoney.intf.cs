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
    public interface I_SendedMoney
    {
        [OperationContract]
        string LoadSendedMoney(MoneySended finder);

        [OperationContract]
        Returns SaveSendedMoney(List<MoneySended> list);

        [OperationContract]
        string LoadSendedMoneyNew(MoneySended finder);

        [OperationContract]
        Returns SaveSendedMoneyNew(List<MoneySended> list);
    }

    public interface ISendedMoneyRemoteObject : I_SendedMoney, IDisposable
    {
    }
}
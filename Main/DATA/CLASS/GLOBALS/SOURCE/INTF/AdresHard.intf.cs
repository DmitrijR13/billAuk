using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_AdresHard
    {
        [OperationContract]
        List<Ls> LoadLs(Ls finder, out Returns ret);
        [OperationContract]
        Returns UpdateLsInCache(Ls finder);
        [OperationContract]
        string GetFakturaName(Ls finder, out Returns ret);
        [OperationContract]
        List<MapObject> GetMapObjects(MapObject finder, out Returns ret);

        [OperationContract]
        Returns SaveGeu(Geu finder);


        [OperationContract]
        List<Ls> GetLs(Ls finder, enSrvOper srv, out Returns ret);

        [OperationContract]
        List<Ls> GetLs2(Ls finder, Service servfinder, out Returns ret);

        [OperationContract]
        Returns SaveUlica(Ulica finder);

        [OperationContract]
        List<_Area> GetArea(Finder finder, out Returns ret, out DateTime serverBeginTime, out DateTime serverEndTime);

        [OperationContract]
        List<_Area> LoadAreaForKvar(Finder finder, out Returns ret);

        [OperationContract]
        List<_Area> LoadAreaPayer(Finder finder, out Returns ret);

        [OperationContract]
        int UpdateLs(Ls finder, out Returns ret);
        [OperationContract]
        int UpdateDom(Dom finder, out Returns ret);

        [OperationContract]
        Returns GeneratePkodFon(Finder finder);
        
        [OperationContract]
        Returns GenerateLsPu(Ls finder, List<Counter> CounterList);

        [OperationContract]
        List<SplitLsParams> ExecuteSplitLS(List<SplitLsParams> listPrm, List<Perekidka> listPerekidka, List<Kart> listGilec, out Returns ret);
    }

    public interface IAdresRemoteObject : I_AdresHard, IDisposable
    {
    }
    
}


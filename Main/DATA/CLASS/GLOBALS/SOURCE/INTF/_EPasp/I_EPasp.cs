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
    public interface I_EPasp
    {
        /// <summary>Внешняя обертка запуска на выполнение формирования Xml файла</summary>
        [OperationContract]
        IntfResultType PrepareEPaspXml(int _year, int _month, int _nzp_dom);

        /// <summary>Пример метода: выборка справочника услуг</summary>
        [OperationContract]
        IntfResultType SelectServiceSample();
    }

    [Serializable]
    public class mabService
    {
        public int serviceId { get; set; }
        public string serviceName { get; set; }
    }

}

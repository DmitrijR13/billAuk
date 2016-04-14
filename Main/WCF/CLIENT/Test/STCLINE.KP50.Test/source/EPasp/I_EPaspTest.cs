using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Test
{
    [ServiceContract]
    public interface I_Charge
    {
    }


    [ServiceContract]
    public interface I_EPasp
    {
        /// <summary>Подготовить электронный адрес</summary>
        [OperationContract]
        IntfResultType UnloadEPaspXml(int _year, int _month, int _nzp_dom);

        /// <summary>Пример метода: выборка справочника услуг</summary>
        [OperationContract]
        IntfResultType SelectServiceSample();
    }

}

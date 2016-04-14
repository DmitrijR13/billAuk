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
    public interface I_Nebo
    {
        /// <summary>
        /// <summary>Функция формирования справочника "Небо"</summary>
        /// </summary>
        /// <returns></returns>
        
        [OperationContract]
        IntfResultObjectType<List<NeboService>> GetServiceList();

        [OperationContract]
        IntfResultObjectType<List<NeboDom>> GetDomList();

        [OperationContract]
        IntfResultObjectType<List<NeboSupplier>> GetSupplierList();

        [OperationContract]
        IntfResultObjectType<List<NeboRenters>> GetRentersList();

        [OperationContract]
        IntfResultObjectType<List<NeboArea>> GetAreaList();
        
        /// <summary>
        /// Функция получения списка реестров для "Небо"
        /// </summary>
        /// <param name="nzp_nebo_reestr"></param>
        /// <returns></returns>
        [OperationContract]
        IntfResultObjectType<List<NeboReestr>> GetReestrInfo(int nzp_nebo_reestr);


        /// <summary>
        /// Функция получения реестра "Cальдовая информация" для "Небо"
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IntfResultObjectType<List<NeboSaldo>> GetSaldoReestr(NeboSaldo request);

        /// <summary>
        /// Функция получения реестра "Начисления по поставщикам" для "Небо"
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IntfResultObjectType<List<NeboSupp>> GetSuppReestr(NeboSupp request);



        /// <summary>
        /// Функция формирования реестра для "Небо"
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Returns CreateReestrNebo(int nzp_type);
    }




}
        
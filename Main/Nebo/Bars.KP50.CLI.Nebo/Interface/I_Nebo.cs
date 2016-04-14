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
        IntfResultObjectType<List<NeboService>> GetServiceList(int nzp_area, RequestPaging paging);

        [OperationContract]
        IntfResultObjectType<List<NeboDom>> GetDomList(int nzp_area, RequestPaging paging);

        [OperationContract]
        IntfResultObjectType<List<NeboSupplier>> GetSupplierList(int nzp_area, RequestPaging paging);

        [OperationContract]
        IntfResultObjectType<List<NeboRenters>> GetRentersList(int nzp_area, RequestPaging paging);

        [OperationContract]
        IntfResultObjectType<List<NeboArea>> GetAreaList(int nzp_area);
        
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
        IntfResultObjectType<List<NeboSaldo>> GetSaldoReestr(NeboSaldo request, RequestPaging paging);

        /// <summary>
        /// Функция получения реестра "Начисления по поставщикам" для "Небо"
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IntfResultObjectType<List<NeboSupp>> GetSuppReestr(NeboSupp request);

        /// <summary>
        /// Функция формирования сальдового реестра или реестра по оплатам для "Небо"
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Returns CreateReestrNebo(int nzp_type, string dat_s,string dat_po);

        /// <summary>
        /// Получение реестра оплат - население для "Небо"
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IntfResultObjectType<List<NeboPaymentReestr>> GetPaymentReestr(int nzp_nebo_reestr, int nzp_area, RequestPaging paginig);
    }
}
        
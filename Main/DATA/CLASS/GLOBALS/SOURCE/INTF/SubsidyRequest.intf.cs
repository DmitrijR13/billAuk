using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
   

    /// <summary>
    /// Интерфейс заявки субсидий 
    /// </summary>
    [ServiceContract]
    public interface I_SubsidyRequest
    {

        [OperationContract]
        Returns AddSubsidyRequest(ref FinRequest finRequest);

        [OperationContract]
        Returns UpdateSubsidyRequest(FinRequest finRequest);

        [OperationContract]
        List<int> ListAvailableSubsidyStatus(int nzpStatus, out Returns ret);

        [OperationContract]
        Returns CheckSubsidyRequestStatus(FinRequest finRequest);

        [OperationContract]
        Returns CalcPartSaldoSubsidy(FinRequest finRequest, List<Payer> listPayers);

        [OperationContract]
        Returns RequestFonTasks(int nzpReq, out Returns ret);
    }

    /// <summary>
    /// Состояния заданий
    /// </summary>
    public enum SubsidyRequestStates
    {
        /// <summary>
        /// Формируется
        /// </summary>
        inProcess = 1,

        /// <summary>
        /// Внесеа 
        /// </summary>
        Placed = 2,

        /// <summary>
        /// Частично перечислен
        /// </summary>
        PartFinalized = 3,

        /// <summary>
        /// Полностью перечислен
        /// </summary>
        Finalized = 4,


        /// <summary>
        /// Удалена
        /// </summary>
        Deleted = 5,


    }


}

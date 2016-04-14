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
    public interface I_MustCalc
    {       
        [OperationContract]
        Returns OperationsWithMustCalc(MustCalc finder, MustCalcOperations operation);
        [OperationContract]
        Returns SaveSpLsMustCalc(MustCalc finder, List<Service> services);
        [OperationContract]
        List<MustCalc> LoadMustCalc(MustCalc finder, out Returns ret);
    }

    public enum MustCalcOperations
    {
        /// <summary>
        /// сохранить
        /// </summary>
        Save,
        Delete
    }

    /// <summary>
    /// Причины перерасчетов начислений
    /// </summary>
    public enum MustCalcReasons
    {
        /// <summary>
        /// Причина не определена
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Изменение значения параметра
        /// </summary>
        Parameter = 1,

        /// <summary>
        /// Добавление/закрытие услуги лицевому счету
        /// </summary>
        Service = 2,

        /// <summary>
        /// Недопоставка
        /// </summary>
        Nedop = 3,

        /// <summary>
        /// Показание прибора учета
        /// </summary>
        Counter = 4,

        /// <summary>
        /// Льгота
        /// </summary>
        Lgota = 5,

        /// <summary>
        /// Жилец
        /// </summary>
        Gil = 6,

        /// <summary>
        /// Установка перерасчета пользователем
        /// </summary>
        Manual,

        /// <summary>
        /// Показание домового прибора учета
        /// </summary>
        DomCounter = 8,

        /// <summary>
        /// Расход жильца
        /// </summary>
        GilRashod = 9
    }
        
    public class MustCalc: Finder
    {
        [DataMember]
        public int nzp_kvar { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public int month_ { get; set; }
        [DataMember]
        public int year_ { get; set; }
        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public int cnt_add { get; set; }
        [DataMember]
        public int kod1 { get; set; }
        [DataMember]
        public int kod2 { get; set; }
        [DataMember]
        public string calcmonth { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public string supplier { get; set; }
        [DataMember]
        public string kod1_str { get; set; }
        [DataMember]
        public string kod2_str { get; set; }
        [DataMember]
        public string dat { get; set; }

        public MustCalc()
            : base()
        {
            nzp_kvar = 0;
            nzp_serv = 0;
            nzp_supp = 0;
            month_ = 0;
            year_ = 0;
            dat_s = "";
            dat_po = "";
            cnt_add = 0;
            kod1 = 0;
            kod2 = 0;
            calcmonth = "";
            service = "";
            supplier = "";
            kod1_str = "";
            kod2_str = "";
            dat = "";
        }
    }

    
    //----------------------------------------------------------------------
    public struct _BufCalc
    //----------------------------------------------------------------------
    {
        public string dat_s,dat_po;
        public int nzp_kvar,nzp_serv,nzp_supp,cnt_add,kod1,kod2,nzp_user;
    }

}


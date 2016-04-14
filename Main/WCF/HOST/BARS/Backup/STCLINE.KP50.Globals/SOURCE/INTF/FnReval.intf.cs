using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Data;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_FnReval
    {
        /// <summary> 
        /// Загрузить список перераспределений сумм
        /// </summary>
        [OperationContract]
        List<FnReval> LoadFnReval(FnRevalFinder finder, out Returns ret);

        /// <summary> 
        /// Выполнить операцию над перераспределением сумм и вернуь результат
        /// </summary>
        [OperationContract]
        Returns OperateWithFnReval(FnReval finder, FnReval.Operations oper);
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class FnReval : Finder
    {
        /// <summary>
        /// Операции с перераспределениями сумм 
        /// </summary>
        public enum Operations
        {
            Save = 1,
            Delete = 2
        }

        private string _area;
        private string _service;
        private string _payer;
        private string _area_2;
        private string _service_2;
        private string _payer_2;
        private string _comment;
        private string _user;

        [DataMember]
        public int nzp_reval { get; set; }

        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public string area { get { return Utils.ENull(_area); } set { _area = value; } }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get { return Utils.ENull(_service); } set { _service = value; } }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public string payer { get { return Utils.ENull(_payer); } set { _payer = value; } }

        [DataMember]
        public int nzp_reval_2 { get; set; }
        [DataMember]
        public int nzp_area_2 { get; set; }
        [DataMember]
        public string area_2 { get { return Utils.ENull(_area_2); } set { _area_2 = value; } }
        [DataMember]
        public int nzp_serv_2 { get; set; }
        [DataMember]
        public string service_2 { get { return Utils.ENull(_service_2); } set { _service_2 = value; } }
        [DataMember]
        public int nzp_payer_2 { get; set; }
        [DataMember]
        public string payer_2 { get { return Utils.ENull(_payer_2); } set { _payer_2 = value; } }

        [DataMember]
        public DateTime dat_oper { get; set; }
        [DataMember]
        public decimal sum_reval { get; set; }
        [DataMember]
        public string comment { get { return Utils.ENull(_comment); } set { _comment = value; } }

        [DataMember]
        public string user_ { get { return Utils.ENull(_user); } set { _user = value; } }
        [DataMember]
        public DateTime dat_when { get; set; }

        public FnReval()
            : base()
        {
            this.nzp_reval = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class FnRevalFinder : FnReval
    {
        [DataMember]
        public DateTime dat_oper_po { get; set; }

        public FnRevalFinder()
            : base() { }
    }
}

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
    public interface I_AdminHard
    {
        /// <summary>
        /// Подготовить данные для печати ЛС
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <returns>результат</returns>
        [OperationContract]
        Returns PreparePrintInvoices(List<PointForPrepare> finder);

        [OperationContract]
        Returns UploadInDb(FilesImported finder, UploadOperations operation, UploadMode mode);
    }
    [DataContract]
    public class PointForPrepare : Finder
    {
        public PointForPrepare()
            : base()
        {
            is_active = true;
        }

        [DataMember]
        public bool mark { get; set; }

        [DataMember]
        public int nzp_wp { get; set; }

        [DataMember]
        public string point { get; set; }

        [DataMember]
        public string calc_month_name { get; set; }

        [DataMember]
        public int calc_month { get; set; }

        [DataMember]
        public bool is_active { get; set; }

        [DataMember]
        public DateTime PrepareDate { set; get; }

        [DataMember]
        public string pref { set; get; }
    }


    public enum UploadOperations
    {
        Area,
        Dom,
        Kvar,
        Supp
    }


    public enum UploadMode
    {
        Add,
        Update
    }
}


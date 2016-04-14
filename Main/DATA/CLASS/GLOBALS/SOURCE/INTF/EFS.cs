using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using STCLINE.KP50.Global;
using System.Data;


namespace STCLINE.KP50.Interfaces
{
    [DataContract]
    public class EFSReestr : Finder
    {
        public enum ReestrStatuses
        {
            /// <summary> Пачки не сформированы
            /// </summary>
            PackNotForm = 0,
            /// <summary> Пачки в процессе формирования
            /// </summary>
            PackForming = 1,

            /// <summary> Пачки сформированы
            /// </summary>
            PackIsForm = 2
        }

        [DataMember]
        public int nzp_efs_reestr { get; set; }
        [DataMember]
        public string file_name { get; set; }
        [DataMember]
        public string date_uchet { get; set; }
        [DataMember]
        public string changed { get; set; }

        [DataMember]
        public int packstatus { get; set; }
        [DataMember]
        public int status { get; set; }

        [DataMember]
        public string strpackstatus { get; set; }

        [DataMember]
        public string strstatus { get; set; }

        [DataMember]
        public string comment { get; set; }

        [DataMember]
        public string file_link { get; set; }

        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public string protocol_name { get; set; }

        public EFSReestr()
            : base()
        {
            nzp_efs_reestr = nzp_exc = 0;
            file_name = file_link = protocol_name = "";
            date_uchet = "";
            changed = comment = "";
            packstatus = -1;
            status = 0;
            strpackstatus = strstatus = "";
        }
    }

    public class EFSPay : EFSReestr
    {
        [DataMember]
        public int nzp_pay { get; set; }
        [DataMember]
        public decimal id_pay { get; set; }
        [DataMember]
        public string id_serv { get; set; }
        [DataMember]
        public decimal ls_num { get; set; }
        [DataMember]
        public decimal summa { get; set; }
        [DataMember]
        public string pay_date { get; set; }
        [DataMember]
        public string barcode { get; set; }
        [DataMember]
        public string address { get; set; }
        [DataMember]
        public int plpor_num { get; set; }
        [DataMember]
        public string plpor_date { get; set; }

        public EFSPay()
            : base()
        {
            nzp_pay = 0;
            id_pay = 0;
            id_serv = "";
            ls_num = 0;
            summa = 0;
            pay_date = "";
            barcode = "";
            address = "";
            plpor_num = 0;
            plpor_date = "";
        }
    }

    [DataContract]
    public class EFSCnt : EFSReestr
    {
        [DataMember]
        public decimal id_pay { get; set; }
        [DataMember]
        public int nzp_cnt { get; set; }
        [DataMember]
        public int cnt_num { get; set; }
        [DataMember]
        public decimal cnt_val { get; set; }
        [DataMember]
        public decimal cnt_val_be { get; set; }

        public EFSCnt()
            : base()
        {
            id_pay = 0;
            nzp_cnt = 0;
            cnt_num = 0;
            cnt_val = 0;
            cnt_val_be = 0;
        }

    }
}


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
    public interface I_Money
    {
        [OperationContract]
        List<Money> GetMoney(Money finder, out Returns ret);

        [OperationContract]
        List<Money> FindMoney(Money finder, out Returns ret);

        [OperationContract]
        Money LoadMoney(Money finder, out Returns ret);

        [OperationContract]
        List<Money> GetMoneyUchet(Money finder, out Returns ret);

        [OperationContract]
        void CalcDistrib(DateTime dat_s, DateTime dat_po, out Returns ret);

        [OperationContract]
        string CheckCalcMoney(int cur_yy, int cur_mm);

        [OperationContract]
        List<AccountPayment> GetAccountPayment(AccountPayment finder, out Returns ret);

        [OperationContract]
        List<Upload_payment> GetUploadPayments(Finder finder, out Returns ret);
        
        [OperationContract]
        List<Upload_payment_files> GetUploadPaymentsFiles(Finder finder, out Returns ret);

        [OperationContract]
        void DeletePayments(Delete_payment finder, out Returns ret);

        [OperationContract]
        void DeleteAllPayments(Delete_payment finder, out Returns ret);

    }

    //----------------------------------------------------------------------
    public static class Moneys
    //----------------------------------------------------------------------
    {
        public static List<Money> MoneysList = new List<Money>(); //
    }
    //----------------------------------------------------------------------
    [DataContract]
    public class Money : Ls
    //----------------------------------------------------------------------
    {
        string _name_bank;
        string _dat_month;
        string _dat_vvod;
        string _dat_uchet;
        string _service_name;

        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public long nzp_pack_ls { get; set; }
        [DataMember]
        public string pack { get; set; }
        [DataMember]
        public decimal sum_ls { get; set; }
        [DataMember]
        public string name_bank { get { return Utils.ENull(_name_bank); } set { _name_bank = value; } }
        [DataMember]
        public string dat_month { get { return Utils.ENull(_dat_month); } set { _dat_month = value; } }
        [DataMember]
        public string dat_vvod { get { return Utils.ENull(_dat_vvod); } set { _dat_vvod = value; } }
        [DataMember]
        public string dat_uchet { get { return Utils.ENull(_dat_uchet); } set { _dat_uchet = value; } }
        [DataMember]
        public int is_uchet { get; set; }

        [DataMember]
        public int visible { get; set; }
        [DataMember]
        public bool callFromFindMoney;

        [DataMember]
        public int info_num { get; set; }

        [DataMember]
        public long nzp_to { get; set; }
        [DataMember]
        public decimal nzp_serv { get; set; }
        [DataMember]
        public string service_name { get { return Utils.ENull(_service_name); } set { _service_name = value; } }
        [DataMember]
        public decimal sum_tarif { get; set; }
        [DataMember]
        public decimal sum_charge { get; set; }
        [DataMember]
        public decimal sum_prih_dolg { get; set; }
        [DataMember]
        public decimal sum_prih_user { get; set; }
        [DataMember]
        public decimal sum_prih_avans { get; set; }


        public Money()
            : base()
        {
            pack = "";
            year_ = 0;
            nzp_pack_ls = 0;
            sum_ls = 0;
            name_bank = "";
            dat_month = "";
            dat_vvod = "";
            dat_uchet = "";
            is_uchet = 0;
            info_num = 0;

            callFromFindMoney = false;
            visible = 1;

            // поля из TO_SUPPL
            nzp_to = 0;
            nzp_serv = 0;
            service_name = "";
            sum_tarif = 0;
            sum_charge = 0;
            sum_prih_dolg = 0;
            sum_prih_user = 0;
            sum_prih_avans = 0;
        }
    }


    [DataContract]
    public class Upload_payment : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_download { get; set; }

        [DataMember]
        public int nzp_user { get; set; }

        [DataMember]
        public string user_name { get; set; }

        [DataMember]
        public int nzp_bank { get; set; }

        [DataMember]
        public string bank_name { get; set; }

        [DataMember]
        public int nzp_clibank_type { get; set; }

        [DataMember]
        public string clibank_type { get; set; }

        [DataMember]
        public string file_name { get; set; }

        [DataMember]
        public string date_upload { get; set; }

        [DataMember]
        public bool check { get; set; }

        [DataMember]
        public bool check1 { get; set; }

        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public string ex_path { get; set; }

        [DataMember]
        public int nzp_bank_client_log { get; set; }

        [DataMember]
        public string money_send { get; set; }
    }


    [DataContract]
    public class Upload_payment_files : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_download { get; set; }

        [DataMember]
        public int nzp { get; set; }

        [DataMember]
        public string date_reestr { get; set; }

        [DataMember]
        public string num_reestr { get; set; }

        [DataMember]
        public string user { get; set; }

        [DataMember]
        public string total_money { get; set; }
    }

    [DataContract]
    public class Delete_payment : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public List<int> download_list { get; set; }

        [DataMember]
        public int reestr_id  { get; set; }
    }

    [DataContract]
    public class AccountPayment : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_payment { get; set; }

        [DataMember]
        public decimal payment_id { get; set; }

        [DataMember]
        public string dat_month { get; set; }

        [DataMember]
        public string web_client_name { get; set; }

        [DataMember]
        public string payment_date { get; set; }

        [DataMember]
        public decimal sum_pay { get; set; }

        [DataMember]
        public string dat_when { get; set; }

        public AccountPayment()
            : base()
        {
            nzp_payment = 0;
            payment_date = "";
            payment_id = 0;
            dat_month = "";
            web_client_name = "";
            sum_pay = 0;
            dat_when = "";
        }
    }
}

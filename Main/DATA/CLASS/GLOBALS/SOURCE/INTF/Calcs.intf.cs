using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Calcs
    {
        [OperationContract]
        List<Calcs> GetDomCalcsCollection(Calcs finder, out Returns ret);

        [OperationContract]
        List<Calcs> GetGrPuCalcsCollection(Calcs finder, out Returns ret);

        [OperationContract]
        List<Calcs> GetKvarCalcsCollection(Calcs finder, out Returns ret);

        [OperationContract]
        Returns CheckDatabaseExist(string pref, enDataBaseType en, string year_form, string year_to);
    }

    [DataContract]
    public class Calcs : Ls
    {
        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public int month_ { get; set; }

        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public int month { get; set; }

        [DataMember]
        public int year { get; set; }

        [DataMember]
        public decimal rashod { get; set; }
        [DataMember]
        public decimal val1 { get; set; }
        [DataMember]
        public decimal val2 { get; set; }
        [DataMember]
        public decimal val3 { get; set; }
        [DataMember]
        public decimal val4 { get; set; }
        [DataMember]
        public decimal sum_val1_val2 { get; set; }
        [DataMember]
        public decimal rvirt { get; set; }
        [DataMember]
        public decimal squ1 { get; set; }
        [DataMember]
        public decimal squ2 { get; set; }
        [DataMember]
        public decimal gil1 { get; set; }
        [DataMember]
        public decimal gil2 { get; set; }

        [DataMember]
        public int cnt1 { get; set; }
        [DataMember]
        public int cnt2 { get; set; }
        [DataMember]
        public int cnt3 { get; set; }
        [DataMember]
        public int cnt4 { get; set; }
        [DataMember]
        public int cnt5 { get; set; }

        [DataMember]
        public decimal dop87 { get; set; }
        [DataMember]
        public decimal pu7kw { get; set; }
        [DataMember]
        public decimal gl7kw { get; set; }
        [DataMember]
        public decimal vl210 { get; set; }
        [DataMember]
        public decimal kf307 { get; set; }
        [DataMember]
        public decimal kf307n { get; set; }
        [DataMember]
        public decimal dlt_in { get; set; }
        [DataMember]
        public decimal dlt_cur { get; set; }
        [DataMember]
        public decimal dlt_reval { get; set; }
        [DataMember]
        public decimal dlt_real_charge { get; set; }
        [DataMember]
        public decimal dlt_calc { get; set; }

        [DataMember]
        public decimal kf_dpu_ls { get; set; }

        [DataMember]
        public decimal dlt_out { get; set; }
        [DataMember]
        public string ed_serv { get; set; }
        [DataMember]
        public int kod_info { set; get; }

        [DataMember]
        public int nzp_counter { set; get; }

        [DataMember]
        public string num_cnt { set; get; }
        [DataMember]
        public string formula { set; get; }

        public Calcs()
            : base()
        {
            nzp_serv = 0;
            nzp_supp = 0;
            service = "";
            nzp_counter = 0;
            num_cnt = "";
            month_ = 0;
            year_ = 0;
            month = 0;
            year = 0;

            rashod = 0; //учтенный расход
            val1 = 0; //нормативный расход
            val2 = 0; //расход по КПУ
            val3 = 0; //расход по ДПУ
            val4 = 0;
            sum_val1_val2 = 0;
            rvirt = 0;
            squ1 = 0; //площадь всех лс
            squ2 = 0; //площадь всех лс без КПУ
            gil1 = 0; //кол-во жильцоы с учетом временных выбывших
            gil2 = 0;
            cnt1 = 0; //кол-во жильцов
            cnt2 = 0;
            cnt3 = 0;
            cnt4 = 0;
            cnt5 = 0;
            dop87 = 0;
            pu7kw = 0;
            gl7kw = 0;
            vl210 = 0;
            kf307 = 0; //коэффициент П307
            dlt_in = 0;
            dlt_cur = 0;
            dlt_reval = 0;
            dlt_calc = 0; //распределенный расход
            dlt_real_charge = 0;
            dlt_out = 0;
            ed_serv = "";
            kod_info = 0;
            kf_dpu_ls = 0;
        }

        /*
         домовые расходы      stek =3 and nzp_type=1 and nzp_kvar = 0 and nzp_dom= xxx and dat_charge is null
         порядок вывода: val1,val2,val3,squ1,squ2,cnt1,gil1,dlt_calc,kf307
          
         квартирные расходы   stek =3 and nzp_type=3 and nzp_kvar =  xxx and dat_charge is null
         порядок вывода: rashod,val1,val2,squ1,cnt1,gil1
        */
    }

    [DataContract]
    public class PackLog
    {
        [DataMember]
        public string tableName { set; get; }

        [DataMember]
        public int nzp_pack { set; get; }

        [DataMember]
        public int nzp_pack_ls { set; get; }

        [DataMember]
        public string dat_oper { set; get; }

        [DataMember]
        public int nzp_wp { set; get; }

        [DataMember]
        public int tip { set; get; }

        [DataMember]
        public bool err { set; get; }

        [DataMember]
        public string message { set; get; }

        public PackLog()
        {
            this.tableName = "";
            this.nzp_pack = 0;
            this.nzp_pack_ls = 0;
            this.dat_oper = "";
            this.nzp_wp = 0;
            this.tip = 0;
            this.message = "";
        }

    }

}

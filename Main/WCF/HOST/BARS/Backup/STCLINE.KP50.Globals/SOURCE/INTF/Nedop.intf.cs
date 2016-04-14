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
    public interface I_Nedop
    {
        [OperationContract]
        List<Nedop> GetNedop(Nedop finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<_Service> GetServicesForNedop(Nedop finder, out Returns ret);

        [OperationContract]
        List<NedopType> GetNedopTypeForNedop(Nedop finder, out Returns ret);

        //возвращает причины недопоставки
        [OperationContract]
        List<NedopType> GetNedopWorkType(Nedop finder, out Returns ret);

        [OperationContract]
        void SaveNedop(Nedop finder, Nedop additionalFinder, out Returns ret);

        /// <summary> Разблокировать недопоставки
        /// </summary>
        [OperationContract]
        Returns UnlockNedop(Nedop finder);

        /// <summary> Сформировать список ЛС, имеющих заданную недопоставку
        /// </summary>
        [OperationContract]
        Returns FindLSDomFromDomNedop(Nedop finder);
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Nedop: Ls
    //----------------------------------------------------------------------
    {
        string _dat_s;
        string _dat_po;
        string _dat_when;

        [DataMember]
        public long nzp_nedop { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public long nzp_supp { get; set; }
        [DataMember]
        public string supplier { get; set; }
        [DataMember]
        public string dat_s { get { return Utils.ENull(_dat_s); } set { _dat_s = value; } }
        [DataMember]
        public string dat_po { get { return Utils.ENull(_dat_po); } set { _dat_po = value; } }
        [DataMember]
        public string tn { get; set; }
        [DataMember]
        public string comment { get; set; }
        [DataMember]
        public int is_actual { get; set; }
        //[DataMember]
        //public string remark { get; set; }
        [DataMember]
        public string dat_when { get { return Utils.ENull(_dat_when); } set { _dat_when = value; } }
        [DataMember]
        public long nzp_user_when { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public int nzp_kind { get; set; }
        [DataMember]
        public string kind { get; set; }
        [DataMember]
        public string block { get; set; }

        //продолжительность недопоставки
        [DataMember]
        public string duration { get; set; }
        [DataMember]
        public int cnt_ls { get; set; }

        public Nedop()
            : base()
        {
            _dat_s = "";
            _dat_po = "";
            _dat_when = "";

            nzp_nedop = 0;
            nzp_serv = 0;
            service = "";
            nzp_supp = 0;
            supplier = "";
            tn = "";
            comment = "";
            is_actual = 0;
            //remark = "";
            nzp_user_when = 0;
            user_name = "";
            nzp_kind = 0;
            kind = "";
            block = "";
            duration = "";
            cnt_ls = 0;
        }
    }

    [DataContract]
    public class NedopType
    {
        [DataMember]
        public int nzp_kind { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public int is_param { get; set; }

        [DataMember]
        public string nedop_name { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public string key { get; set; }

        [DataMember]
        public int nzp_work_type { get; set; }

        [DataMember]
        public string name_work_type { get; set; }

        public NedopType(): base()
        {
            nzp_kind = 0;
            nzp_serv = 0;
            nedop_name = "";
            service = "";
            is_param = 0;
            key = "";
            nzp_work_type = 0;
            name_work_type = "";
        }
    }

    [DataContract]
    public class NedopInfo
    {
        //ЖЭУ
        [DataMember]
        public string geu { get; set; }

        //номер акта
        [DataMember]
        public int nzp_act { get; set; }

        //адрес
        [DataMember]
        public string address  { get; set; }

        //номер входящего документа
        [DataMember]
        public string plan_number  { get; set; }

        //дата входящего документа
        [DataMember]
        public string plan_date { get; set; }

        //содержание
        [DataMember]
        public string comment { get; set; }

        //номер акта
        [DataMember]
        public string number { get; set; }

        //Дата, время включения
        [DataMember]
        public string connect_date { get; set; }

        //Дата, время отключения
        [DataMember]
        public string disconnect_date { get; set; }

        //ответственный
        [DataMember]
        public string officer { get; set; }

        //поставщик
        [DataMember]
        public string name_supp { get; set; }

        //ЦТП
        [DataMember]
        public string ctp { get; set; }

        //дата акта
        [DataMember]
        public string act_date { set; get; }

        //температура
        [DataMember]
        public string tn { set; get; }

        //услуга
        [DataMember]
        public string name_serv { set; get; }
        
        //тип недопоставки
        [DataMember]
        public string name_nedop { set; get; }

        //список улиц для акта
        [DataMember]
        public List<string> address_list { set; get; }

        //список ЖЭУ для акта
        [DataMember]
        public List<string> geu_list { set; get; }

        public NedopInfo()
            : base()
        {
            nzp_act = 0;
            geu = "";
            address = "";
            name_nedop = "";
            comment = "";
            connect_date = "";
            disconnect_date = "";
            officer = "";
            name_supp = "";
            ctp = "";
            act_date = "";
            tn = "";
        }
    }
}

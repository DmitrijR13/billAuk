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
   
    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "PublicServiceType")]
    public class NeboService
    //----------------------------------------------------------------------
    {
        /// <summary>Наименование услуги</summary>
        [DataMember(Name = "serviceName", Order = 10)]
        public string service { get; set; }

        /// <summary>Код услуги</summary>
        [DataMember(Name = "serviceID", Order = 20)]
        public int nzp_serv { get; set; }

        /// <summary>Код управляющей компании</summary>
        [DataMember(Name = "managingCompanyID", Order = 70)]
        public int nzp_area { get; set; }   

        public NeboService()
        {
            service = "";
            nzp_serv = 0;
            nzp_area = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "HouseType")]
    public class NeboDom
    //----------------------------------------------------------------------
    {
        /// <summary>Район/Крупный город</summary>
        [DataMember(Name = "city", Order = 10)]
        public string rajon { get; set; }

        /// <summary>Населенный пункт</summary>
        [DataMember(Name = "town", Order = 20)]
        public string town { get; set; }

        /// <summary>Улица</summary>
        [DataMember(Name = "street", Order = 30)]
        public string ulica { get; set; }

        /// <summary>Номер дома</summary>
        [DataMember(Name = "houseNumber", Order = 40)]
        public string ndom { get; set; }

        /// <summary>Корпус дома</summary>
        [DataMember(Name = "buildingNumber", Order = 50)]
        public string nkor { get; set; }

        /// <summary>Код дома</summary>
        [DataMember(Name = "houseID", Order = 60)]
        public int nzp_dom { get; set; }

        /// <summary>Код управляющей компании</summary>
        [DataMember(Name = "managingCompanyID", Order = 70)]
        public int nzp_area { get; set; }   
        

        public NeboDom()
        {
            rajon = "";
            town = "";
            ulica = "";
            ndom = "";
            nkor = "";
            nzp_dom = 0;
            nzp_area = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "SupplierType")]
    public class NeboSupplier
    //----------------------------------------------------------------------
    {
        /// <summary>ИНН поставщика</summary>
        [DataMember(Name = "inn", Order = 10)]
        public int inn_supp { get; set; }

        /// <summary>КПП поставщика</summary>
        [DataMember(Name = "kpp", Order = 20)]
        public int kpp_supp { get; set; }

        /// <summary>Наименование поставщика</summary>
        [DataMember(Name = "supplierName", Order = 30)]
        public string name_supp { get; set; }

        /// <summary>Код поставщика</summary>
        [DataMember(Name = "supplierID", Order = 40)]
        public int nzp_supp { get; set; }

        /// <summary>Код управляющей компании</summary> 
        [DataMember(Name = "managingCompanyID", Order = 50)]
        public int nzp_area { get; set; }   


        public NeboSupplier()
        {
            inn_supp = 0;
            kpp_supp = 0;
            name_supp = "";
            nzp_supp = 0;
            nzp_area = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "RenterAccountType")]
    public class NeboRenters
    //----------------------------------------------------------------------
    {
        /// <summary>Код арендатора</summary>
        [DataMember(Name = "renterID", Order = 10)]
        public int renter_id { get; set; }

        /// <summary>ИНН арендатора</summary>
        [DataMember(Name = "inn", Order = 11)]
        public string inn_kvar { get; set; }

        /// <summary>КПП арендатора</summary>
        [DataMember(Name = "kpp", Order = 12)]
        public string kpp_kvar { get; set; }

        /// <summary>Номер лицевого арендатора</summary>
        [DataMember(Name = "accountNumber", Order = 20)]
        public decimal  pkod { get; set; }

        /// <summary>Код дома</summary>
        [DataMember(Name = "houseID", Order = 30)]
        public int nzp_dom { get; set; }

        /// <summary>Номер квартиры</summary>
        [DataMember(Name = "flatNumber", Order = 40)]
        public string nkvar { get; set; }

        /// <summary>Номер комнаты</summary>
        [DataMember(Name = "roomNumber", Order = 50)]
        public string nkvar_n { get; set; }

        /// <summary>Описание объекта</summary>
        [DataMember(Name = "description", Order = 60)]
        public string description { get; set; }

        /// <summary>Код управляющей компании</summary> 
        [DataMember(Name = "managingCompanyID", Order = 70)]
        public int nzp_area { get; set; }   

        public NeboRenters()
        {
            renter_id = 0;
            inn_kvar = "";
            kpp_kvar = "";
            pkod = 0;
            nzp_dom = 0;
            nkvar = "";
            nkvar_n = "";
            description = "";
            nzp_area = 0;
        }
    }
     
     //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "ManagingCompanyType")]
    public class NeboArea
    //----------------------------------------------------------------------
    {
        /// <summary>ИНН управляющей компании</summary>
        [DataMember(Name = "inn", Order = 10)]
        public int inn_area { get; set; }

        /// <summary>КПП управляющей компании</summary>
        [DataMember(Name = "kpp", Order = 20)]
        public int kpp_area { get; set; }

        /// <summary>Наименование управляющей компании</summary>
        [DataMember(Name = "managingCompanyName", Order = 30)]
        public string area { get; set; }

        /// <summary>Юр. адрес управляющей компании</summary>
        [DataMember(Name = "address", Order = 30)]
        public string address_area { get; set; }

        /// <summary>Код управляющей компании</summary>
        [DataMember(Name = "managingCompanyID", Order = 30)]
        public int nzp_area { get; set; }

        public NeboArea()
        {
            inn_area = 0;
            kpp_area = 0;
            area = "";
            address_area = "";
            nzp_area = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
    public enum en_Supsidy_oper
    {
        GetRequests,
        GetRequestsForSubsidy,
        LoadSubsReqDetails,
        LoadPayersFromSubsReqDetails,
        LoadSubsContract,
        LoadSubContractForPayer
    }

    /// <summary>
    /// Интерфейс Субсидий
    /// </summary>
    [ServiceContract]
    public interface I_Subsidy
    {
        /// <summary>
        /// Получить список заявок на финансирование
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <returns>Список заявок на финансирование</returns>
        [OperationContract]
        List<FinRequest> GetFinRequestsList(FinRequest finder, en_Supsidy_oper oper, out Returns ret);

        [OperationContract]
        List<FnSubsReqDetails> LoadSubsReqDetails(FnSubsReqDetailsForSearch finder, en_Supsidy_oper oper, out Returns ret);

        [OperationContract]
        Returns SaveSubsReqDetails(List<FnSubsReqDetails> listfinder);

        [OperationContract]
        List<FnSubsOrder> LoadSubsOrder(FnSubsOrder finder, out Returns ret);

        [OperationContract]
        Returns SaveSubsOrder(FnSubsOrderForSave finder);

        [OperationContract]
        List<FnSubsSaldo> GetSubsSaldo(FnSubsSaldo finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<FnSubsContract> LoadSubsContract(FnSubsContractForSearch finder, en_Supsidy_oper oper, out Returns ret);

        [OperationContract]
        List<FnAgreement> GetAgreementsList(FnAgreement finder, out Returns ret);

        [OperationContract]
        List<FnAgreement> GetAgreementTypes(out Returns ret);

        [OperationContract]
        FnAgreement AddUpdateAgreement(FnAgreement finder, out Returns ret);

        [OperationContract]
        bool DelAgreement(FnAgreement finder, out Returns ret);

        [OperationContract]
        Returns MakeOperation(Finder finder, SubsidyOperations operation);

        [OperationContract]
        Returns OperateWithOrder(FnSubsOrder finder, SubsidyOrderOperations operation);

        [OperationContract]
        List<PercPt> GetPercPtList(PercPt finder, out Returns ret);

        [OperationContract]
        Returns LoadKassPlan(string kassPlanFileString, FnSubsContract subsContract, out FnSubsKassPlan kassPlan);

        [OperationContract]
        Returns DeleteKassPlan(FnSubsKassPlan subsKassPlan);

        [OperationContract]
        Returns LoadMonthPlan(string monthPlanFileString, FnSubsContract subsContract, out FnSubsMonthPlan monthPlan);

        [OperationContract]
        Returns DeleteMonthPlan(FnSubsMonthPlan subsMonthPlan);

        [OperationContract]
        PercPt UpdatePercPt(PercPt finder, out Returns ret);

        [OperationContract]
        List<FnSubsKassPlan> GetSubsKassPlan(FnSubsKassPlan finder, out Returns ret);

        [OperationContract]
        FnSubsMonthPlan GetFnSubsMonthPlans(FnSubsMonthPlan finder, out Returns ret);

        [OperationContract]
        List<FnSubsAct> LoadSubsAct(FnSubsActForSearch finder, out Returns ret);

        [OperationContract]
        Returns LoadTehHarGilFond(string fileName, FnSubsContract subsContract);

        [OperationContract]
        List<FnSubsTehHarGilFond> GetHarGilFondList(FnSubsTehHarGilFond finder, out Returns ret);

        [OperationContract]
        FnSubsTehHarGilFond UpdateHarGilFond(FnSubsTehHarGilFond finder, out Returns ret);

        [OperationContract]
        ReturnsType LoadAktSubsidy_rust(string filename, FnSubsActForSearch finger);

        [OperationContract]
        Returns DeleteSubsAct(FnSubsActForSearch finder);

        [OperationContract]
        Returns UseActOfSupply(FnSubsActForSearch finder);

        [OperationContract]
        Returns UseTehHarGilFond(FnSubsTehHarGilFond finder);

        [OperationContract]
        Returns GetCountContractFromTables(FnAgreement finder, out Returns ret);
    }

    public enum SubsidyOperations
    {
        /// <summary>
        /// Распределить суммы на перечисление по лицевым счетам
        /// </summary>
        DistributeOrders
    }

    public enum SubsidyOrderOperations
    {
        /// <summary>
        /// Отменить распределение приказа по лицевым счетам
        /// </summary>
        CancelDistribution,
        DistributeOrders,

        /// <summary>
        /// Удалить приказ
        /// </summary>
        Delete
    }

    public enum SubsidyActOperations
    {
        UseActOfSupply,
        UseTehHarGilFond
    }

    /// <summary>
    /// Класс заявок на финансирование
    /// </summary>
    [DataContract]
    public class FinRequest : Finder
    {
        /// <summary>
        /// nzp
        /// </summary>
        [DataMember]
        public int nzp_req { set; get; }

        //Номер заявки
        [DataMember]
        public string num_request { set; get; }

        /// <summary>
        /// Сумма заявки 
        /// </summary>
        [DataMember]
        public decimal sum_request { set; get; }

        /// <summary>
        /// Строковое представление суммы
        /// </summary>
        [DataMember]
        public string sum_request_str { set; get; }

        /// <summary>
        /// Дата заявки
        /// </summary>
        [DataMember]
        public string date_request { set; get; }

        /// <summary>
        /// Месяц, за который формируется заявка
        /// </summary>
        [DataMember]
        public string date_month { set; get; }

        /// <summary>
        /// Комментарий
        /// </summary>
        [DataMember]
        public string comment { set; get; }

        /// <summary>
        /// Код пользователя, внесшего финансирование
        /// </summary>
        [DataMember]
        public int created_by { set; get; }

        /// <summary>
        /// Пользователь, внесший финансирование
        /// </summary>
        [DataMember]
        public string user_created_by { set; get; }

        /// <summary>
        /// Логин полььзователя, анесшего финансирование
        /// </summary>
        [DataMember]
        public string login_created_by { set; get; }

        /// <summary>
        /// Дата внесения финансирования
        /// </summary>
        [DataMember]
        public string created_on { set; get; }

        /// <summary>
        /// Код пользователя, сделавшего последние изменения
        /// </summary>
        [DataMember]
        public int changed_by { set; get; }

        /// <summary>
        /// Пользователя, сделавший последние изменения
        /// </summary>
        [DataMember]
        public string user_changed_by { set; get; }

        /// <summary>
        /// Логин пользователя, сделавшего последние изменения
        /// </summary>
        [DataMember]
        public string login_changed_by { set; get; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        [DataMember]
        public string changed_on { set; get; }

        /// <summary>
        /// ключ Статус финансирования
        /// </summary>
        [DataMember]
        public int nzp_status { set; get; }

        /// <summary>
        /// Статус финансирования
        /// </summary>
        [DataMember]
        public string status { set; get; }

        /// <summary>
        /// Год
        /// </summary>
        [DataMember]
        public int year { set; get; }

        public FinRequest()
        {
            nzp_req = 0;
            num_request = "";
            sum_request = 0;
            sum_request_str = "";
            date_request = "";
            date_month = "";
            comment = "";
            created_by = 0;
            user_created_by = "";
            created_on = "";
            changed_by = 0;
            user_changed_by = "";
            changed_on = "";
            nzp_status = 0;
            status = "";
            year = 0;
            login_created_by = "";
            login_changed_by = "";

        }
    }

    [DataContract]
    public class FnSubsReqDetails : Finder
    {
        [DataMember]
        public int nzp_req_det { get; set; }
        [DataMember]
        public int nzp_req { get; set; }
        [DataMember]
        public int nzp_town { get; set; }
        [DataMember]
        public string town { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public string payer { get; set; }
        [DataMember]
        public int nzp_order { get; set; }
        [DataMember]
        public string order { get; set; }
        [DataMember]
        public int nzp_contract { get; set; }
        [DataMember]
        public string contract { get; set; }
        [DataMember]
        public int nzp_order_payer { get; set; }
        [DataMember]
        public decimal sum_pere { get; set; }
        [DataMember]
        public decimal sum_plan { get; set; }
        [DataMember]
        public decimal sum_charge { get; set; }
        [DataMember]
        public decimal koef { get; set; }
        [DataMember]
        public decimal sum_izm { get; set; }
        [DataMember]
        public decimal sum_request { get; set; }
        [DataMember]
        public string changed_by { get; set; }
        [DataMember]
        public string changed_on { get; set; }      
        [DataMember]
        public int year { get; set; }
        [DataMember]
        public string date_month { get; set; }
        [DataMember]
        public decimal sum_prih { get; set; }

        public FnSubsReqDetails()
            : base()
        {
            nzp_req_det = 0;
            nzp_req = 0;
            nzp_town = 0;
            town = "";
            nzp_serv = 0;
            service = "";
            nzp_payer = 0;
            payer = "";
            nzp_order = 0;
            order = "";
            sum_pere = 0;
            sum_plan = 0;
            sum_charge = 0;
            koef = 0;
            sum_izm = 0;
            sum_request = 0;
            changed_by = "";
            changed_on = "";         
            year = 0;
            date_month = "";
            nzp_order_payer = 0;
            nzp_contract = 0;
            contract = "";
            sum_prih = 0;
        }
    }

    [DataContract]
    public class FnSubsReqDetailsForSearch : Finder
    {
        [DataMember]
        public List<int> towns { get; set; }
        [DataMember]
        public List<int> services { get; set; }
        [DataMember]
        public List<int> payers { get; set; }
        [DataMember]
        public int year { get; set; }
        [DataMember]
        public int month { get; set; }
        [DataMember]
        public int nzp_req { get; set; }
        [DataMember]
        public int nzp_req_det { get; set; }
        [DataMember]
        public int nzp_order { get; set; }
        [DataMember]
        public bool withorder { get; set; }
        [DataMember]
        public string date_month { get; set; }

        public FnSubsReqDetailsForSearch()
            : base()
        {
            towns = new List<int>();
            services = new List<int>();
            payers = new List<int>();
            year = 0;
            month = 0;
            nzp_req = 0;
            nzp_order = 0;
            nzp_req_det = 0;
            withorder = false;
            date_month = "";
        }
    }

    public enum RequestStatus
    {
        Formed = 1,//формируется
        Entered = 2,//внесен
        ParticalPerechisl = 3,//частично перечислен
        Perechisl = 4//перечислено
    }
        
    [DataContract]
    public class FnSubsOrder : Finder
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_order { get; set; }       
        [DataMember]
        public string num_doc { get; set; }
        [DataMember]
        public string dat_doc { get; set; }
        [DataMember]
        public decimal sum_order { get; set; }
        [DataMember]
        public string comment { get; set; }       
        [DataMember]
        public string date_month { get; set; }
        [DataMember]
        public string created_by { get; set; }
        [DataMember]
        public string changed_by { get; set; }
        [DataMember]
        public int year { get; set; }

        public FnSubsOrder()
            : base()
        {
            num = 0;
            nzp_order = 0;
            num_doc = "";         
            dat_doc = "";
            sum_order = 0;
            comment = "";
            date_month = "";
            created_by = "";
            changed_by = "";
            year = 0;
        }
    }

    [DataContract]
    public class FnSubsOrderForSave : FnSubsOrder
    {
        [DataMember]
        public List<FnSubsReqDetails> nzp_payers { get; set; }

        [DataMember]
        public int nzp_req { get; set; }

        [DataMember]
        public List<FnSubsReqDetails> nzp_payers_clear_link { get; set; }

        public FnSubsOrderForSave()
            : base()
        {
            nzp_payers = new List<FnSubsReqDetails>();
            nzp_payers_clear_link = new List<FnSubsReqDetails>();
            nzp_req = 0;
        }
    }

    [DataContract]
    public class FnSubsSaldo : Finder
    {
        [DataMember]
        public int nzp_subs { get; set; }
        
        [DataMember]
        public string town { get; set; }

        [DataMember]
        public int nzp_town { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public string payer { get; set; }

        [DataMember]
        public int nzp_payer { get; set; }

        [DataMember]
        public string date_month { get; set;}

        [DataMember]
        public string date_month_po { get; set; }

        [DataMember]
        public decimal sum_insaldo { get; set;}
  
        [DataMember]
        public decimal sum_request { get; set;}

        [DataMember]
        public decimal sum_charge { get; set;}

        [DataMember]
        public decimal sum_order { get; set;}

        [DataMember]
        public decimal sum_mismatch { get; set;}

        [DataMember]
        public decimal sum_outsaldo { get; set;}

        [DataMember]
        public string groupby { get; set; }

        [DataMember]
        public string num { get; set; }

        public FnSubsSaldo()
            : base()
        {
            nzp_subs = 0;           
            town = "";
            nzp_town = 0;
            service = "";
            nzp_serv = 0;
            nzp_payer = 0;
            payer = "";
            date_month = "";
            date_month_po = "";
            sum_insaldo = 0;
            sum_charge = 0;
            sum_request = 0;
            sum_order = 0;
            sum_mismatch = 0;
            sum_outsaldo = 0;
            groupby = "";
            num = "";
        }
    }

    [DataContract]
    public class FnSubsContract : Finder
    {
        [DataMember]
        public int nzp_contract { get; set; }
        [DataMember]
        public string contract { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public int nzp_town { get; set; }
        [DataMember]
        public string num_doc { get; set; }
        [DataMember]
        public string date_doc { get; set; }
        [DataMember]
        public string date_end { get; set; }
        [DataMember]
        public int nzp_fb { get; set; }
        [DataMember]
        public int nzp_type { get; set; }
        [DataMember]
        public string comment { get; set; }
        [DataMember]
        public string created_by { get; set; }
        [DataMember]
        public string changed_by { get; set; }

        public FnSubsContract()
            : base()
        {
            contract = "";
            nzp_contract = 0;
            nzp_payer = 0;
            nzp_town = 0;
            num_doc = "";
            date_doc = "";
            date_begin = "";
            date_end = "";
            nzp_fb = 0;
            nzp_type = 0;
            comment = "";
            created_by = "";
            changed_by = "";
        }
    }

    [DataContract]
    public class FnSubsContractForSearch : Finder
    {
        [DataMember]
        public int nzp_contract { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public string date_oper { get; set; }
        [DataMember]
        public int nzp_order { get; set; }

        public FnSubsContractForSearch()
            : base()
        {
            nzp_contract = 0;
            nzp_payer = 0;
            nzp_order = 0;            
            date_oper = "";
        }
    }

    /// <summary>
    /// Класс для соглашений с подрядчиками
    /// </summary>
    [DataContract]
    public class FnAgreement : Finder
    {
        /// <summary>
        /// Ключ
        /// </summary>
        [DataMember]
        public int nzp_contract { set; get; }

        /// <summary>
        /// Подрядчик
        /// </summary>
        [DataMember]
        public int nzp_payer { set; get; }

        /// <summary>
        /// Город
        /// </summary>
        [DataMember]
        public int nzp_town { set; get; }

        /// <summary>
        /// Номер документа
        /// </summary>
        [DataMember]
        public string num_doc { set; get; }

        /// <summary>
        /// Дата документа
        /// </summary>
        [DataMember]
        public string date_doc { set; get; }

        ///// <summary>
        ///// Дата начала
        ///// </summary>
        //[DataMember]
        //public string date_bein { set; get; }

        /// <summary>
        /// Дата окончания
        /// </summary>
        [DataMember]
        public string date_end { set; get; }

        [DataMember]
        public int nzp_fb { set; get; }


        [DataMember]
        public int nzp_type { set; get; }

        /// <summary>
        /// Тип соглашения
        /// </summary>
        [DataMember]
        public string type { set; get; }

        /// <summary>
        /// Комментарий
        /// </summary>
        [DataMember]
        public string comment { set; get; }

        /// <summary>
        /// Кем создан
        /// </summary>
        [DataMember]
        public int created_by { set; get; }

        /// <summary>
        /// Кем создан пользователь
        /// </summary>
        [DataMember]
        public string created_by_user { set; get; }


        /// <summary>
        /// Логин пользователя, кем создано
        /// </summary>
        [DataMember]
        public string login_created_by { set; get; }

        /// <summary>
        /// Когда создан
        /// </summary>
        [DataMember]        
        public string created_on { set; get; }

        /// <summary>
        /// Кем изменен
        /// </summary>
        [DataMember]
        public int changed_by { set; get; }

        /// <summary>
        /// Кем изменен пользователь
        /// </summary>
        [DataMember]
        public string changed_by_user { set; get; }


        /// <summary>
        /// Кем изменен login
        /// </summary>
        [DataMember]
        public string login_changed_by { set; get; }

        /// <summary>
        /// Когда изменен
        /// </summary>
        [DataMember]
        public string changed_on { set; get; }

        /// <summary>
        /// Подрядчик
        /// </summary>
        [DataMember]
        public string payer { set; get; }

        /// <summary>
        /// Район/Город
        /// </summary>
        [DataMember]
        public string town { set; get; }        

        public FnAgreement()
        {
            nzp_contract = 0;
            nzp_payer = 0;
            nzp_town = 0;
            nzp_town = 0;
            num_doc = "";
            date_doc = "";
            //date_bein = "";
            date_end = "";
            nzp_fb = 0;
            nzp_type = 0;
            comment = "";
            created_by = 0;
            created_on = "";
            changed_by = 0;
            changed_on = "";
            payer = "";
            town = "";
            created_by_user = "";
            changed_by_user = "";
            login_created_by = "";
            login_changed_by = "";

        }

    }


    /// <summary>
    /// Класс справочника уровня платежей граждан
    /// </summary>
    [DataContract]
    public class PercPt : Finder
    {
        [DataMember]
        public int nzp_perc { set; get; }

        [DataMember]
        public int nzp_serv { set; get; }

        [DataMember]
        public string service { set; get; }

        [DataMember]
        public int nzp_supp { set; get; }

        [DataMember]
        public string name_supp { set; get; }

        [DataMember]
        public decimal nzp_vill { set; get; }

        [DataMember]
        public string vill { set; get; }

        [DataMember]
        public int etag { set; get; }

        [DataMember]
        public string dat_s { set; get; }

        [DataMember]
        public string dat_po { set; get; }

        [DataMember]
        public decimal perc { set; get; }

    }

    /// <summary>
    /// Класс кассового плана
    /// </summary>
    [DataContract]
    public class FnSubsKassPlan : Finder
    {
        [DataMember]
        public int nzp_skp { set; get; }

        /// <summary>
        /// соглашение
        /// </summary>
        [DataMember]
        public FnSubsContract contact { set; get; }

        /// <summary>
        /// Наименование организации сформировавшей кассовый план
        /// </summary>
        [DataMember]
        public string name_org { set; get; }

        /// <summary>
        /// ИНН
        /// </summary>
        [DataMember]
        public string inn { set; get; }

        /// <summary>
        /// КПП
        /// </summary>
        [DataMember]
        public string kpp { set; get; }

        /// <summary>
        /// Ответственный
        /// </summary>
        [DataMember]
        public string responsible { set; get; }

        /// <summary>
        /// Актуальность плана 1 - действует, 100 - в архиве
        /// </summary>
        [DataMember]
        public int is_actual { set; get; }

        /* уже есть в Finder
        /// <summary>
        /// Период плана, дата начала
        /// </summary>
        [DataMember]
        public string date_begin { set; get; }
        */

        /// <summary>
        /// Период плана, дата окончания
        /// </summary>
        [DataMember]
        public string date_create_plan { set; get; }

        /// <summary>
        /// Период плана, дата окончания
        /// </summary>
        [DataMember]
        public string date_end { set; get; }

        /// <summary>
        /// Раскладка суммы плана по месяцам
        /// </summary>
        [DataMember]
        public decimal[] sum_plan { set; get; }

        /// <summary>
        /// Общая суммы плана 
        /// </summary>
        [DataMember]
        public decimal sum_plan_all { set; get; }

        /// <summary>
        /// Сумма по соглашению
        /// </summary>
        [DataMember]
        public decimal sum_contract { set; get; }

        /// <summary>
        /// Сумма недопоставки предыдущего года
        /// </summary>
        [DataMember]
        public decimal sum_nedop { set; get; }
        
        /// <summary>
        /// Сумма перечисленная по доп соглашению
        /// </summary>
        [DataMember]
        public decimal sum_dop_contract { set; get; }

        /// <summary>
        /// Всего подлежит финансированию ст4= ст1-ст2-ст3
        /// </summary>
        [DataMember]
        public decimal fin_tot { set; get; }

        /// <summary>
        /// Кем создан
        /// </summary>
        [DataMember]
        public string created_by { get; set; }

        public FnSubsKassPlan()
            : base()
        {
            nzp_skp = 0;
            contact = new FnSubsContract();
            name_org = "";
            inn = "";
            kpp = "";
            responsible = "";
            is_actual = 1;
            date_begin = "";
            date_end = "";
            sum_contract = 0;
            sum_nedop = 0;
            sum_dop_contract = 0;
            sum_plan_all = 0;
            created_by = "";

        

            sum_plan = new decimal[12];

            for (int i = 0; i < 12; i++)
            {
                sum_plan[i] = 0;
            }

        }

    }


    public class FnSubsPlanValues
    {
        /// <summary>
        /// Код услуги
        /// </summary>
        [DataMember]
        public int nzp_serv { set; get; }

        /// <summary>
        /// Наименование услуги
        /// </summary>
        [DataMember]
        public string service { set; get; }        


        /// <summary>
        /// Код единицы измерения
        /// </summary>
        [DataMember]
        public int nzp_measure { set; get; }

        /// <summary>
        /// Единицы измерения
        /// </summary>
        [DataMember]
        public string measure { set; get;  }

        /// <summary>
        /// Средний тариф
        /// </summary>
        [DataMember]
        public decimal middle_tarif { set; get; }

        /// <summary>
        /// сумма расходов по месяцам
        /// </summary>
        [DataMember]
        public decimal sum_c_calc { set; get; }

        /// Общая сумма субсидии
        /// </summary>
        [DataMember]
        public decimal sum_subsidy_all { set; get; }

        /// <summary>
        /// Раскладка расхода плана по месяцам
        /// </summary>
        [DataMember]
        public decimal[] c_calc { set; get; }

        /// <summary>
        ///  Раскладка расхода плана по кварталам
        /// </summary>
        [DataMember]
        public decimal[] c_calc_kv { set; get; }

        /// <summary>
        /// Раскладка тарифов плана по месяцам
        /// </summary>
        [DataMember]
        public decimal[] tarif { set; get; }

        /// <summary>
        ///  Раскладка тарифов плана по кварталам
        /// </summary>
        [DataMember]
        public decimal[] tarif_kv { set; get; }

        /// <summary>
        /// Раскладка суммы плана по месяцам
        /// </summary>
        [DataMember]
        public decimal[] sum_subsidy { set; get; }

        /// <summary>
        ///  Раскладка суммы плана по кварталам
        /// </summary>
        [DataMember]
        public decimal[] sum_subsidy_kv { set; get; }

        public FnSubsPlanValues()
            : base()
        {


            nzp_serv = 0;
            nzp_measure = 0;
            middle_tarif = 0;
            sum_c_calc = 0;
            sum_subsidy_all = 0;

            c_calc = new decimal[12];
            for (int i = 0; i < 12; i++)
            {
                c_calc[i] = 0;
            }

            tarif = new decimal[12];
            for (int i = 0; i < 12; i++)
            {
                tarif[i] = 0;
            }

            sum_subsidy = new decimal[12];
            for (int i = 0; i < 12; i++)
            {
                sum_subsidy[i] = 0;
            }

            c_calc_kv = new decimal[4];
            tarif_kv = new decimal[4];
            sum_subsidy_kv = new decimal[4];

            for (int i = 0; i < 4; i++)
            {
                c_calc_kv[i] = 0;
                tarif_kv[i] = 0;
                sum_subsidy_kv[i] = 0;
            }


        }
    }

    [DataContract]
    public class FnSubsMonthPlan : FnSubsKassPlan
    {
        /// <summary>
        /// Код помесячного плана
        /// </summary>
        [DataMember]
        public int nzp_smp { set; get; }
        
        /// <summary>
        /// Список услуг плана
        /// </summary>
        [DataMember]
        public List<FnSubsPlanValues> listServices { set; get; }

        public FnSubsMonthPlan()
            : base()
        {
            listServices = new List<FnSubsPlanValues>();
        }
    }

    [DataContract]
    public class FnSubsAct : Finder
    {
        /// <summary>
        /// Перечеслитель статусов s_act_status
        /// </summary>
        public enum Statuses
        {
            /// <summary>
            /// Генерируется
            /// </summary>
            Generating = 1,

            /// <summary>
            /// Сгенерирован
            /// </summary>
            Generated = 2,

            /// <summary>
            /// Загружается
            /// </summary>
            Loading = 3,

            /// <summary>
            /// Загружен
            /// </summary>
            Loaded = 4,

            /// <summary>
            /// В процессе учета
            /// </summary>
            Considering = 5,

            /// <summary>
            /// Учтен
            /// </summary>
            Considered = 6,

            /// <summary>
            /// Удален
            /// </summary>
            Deleted = 7,

            /// <summary>
            /// Учтен с ошибками
            /// </summary>
            ConsideredWithErrors = 8
        }

        [DataMember]
        public int nzp_sa { get; set; }
        [DataMember]
        public int nzp_contract { get; set; }
        [DataMember]
        public int nzp_ev { get; set; }
        [DataMember]
        public int num_act { get; set; }
        [DataMember]
        public string dat_act { get; set; }
        [DataMember]
        public int nzp_status { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public decimal inn { get; set; }
        [DataMember]
        public decimal kpp { get; set; }
        [DataMember]
        public decimal nzp_vill { get; set; }
        [DataMember]
        public string created { get; set; }

        [DataMember]
        public string otch_period { get; set; }

        [DataMember]
        public string filename { get; set; }

        public FnSubsAct()
            : base()
        {
            nzp_sa = 0;
            nzp_contract = 0;
            nzp_ev = 0;
            num_act = 0;
            dat_act = "";
            nzp_status = 0;
            status = "";
            name_supp = "";
            inn = 0;
            kpp = 0;
            nzp_vill = 0;
            created = "";
            otch_period = "";
            filename = "";
        }
    }

    [DataContract]
    public class FnSubsActForSearch : Finder
    {
        [DataMember]
        public int nzp_sa { get; set; }
        [DataMember]
        public int nzp_contract { get; set; }
        [DataMember]
        public string dat_month { get; set; }
        [DataMember]
        public int year { get; set; }

        public FnSubsActForSearch()
            : base()
        {
            nzp_sa = 0;
            nzp_contract = 0;
            dat_month = "";
            year = 0;
        }
    }

    /// <summary>
    ///ВРЕМЕННЫЙ!  Класс для файлов для сохранения
    /// </summary>
    [DataContract]
    public class FileToSave : Finder
    {        
        /// <summary>
        /// путь для сохранения файла
        /// </summary>
        [DataMember]
        public string path { set; get; }

        /// <summary>
        /// Файл - байты
        /// </summary>
        [DataMember]
        public byte[] fileBuffer { set; get; }


        /// <summary>
        /// Ключ для составлении имени файла
        /// </summary>
        [DataMember]
        public int nzp { set; get; }
    }

    /// <summary>
    /// Класс технических характеристик жилого фонда
    /// </summary>
    [DataContract]
    public class FnSubsTehHarGilFond : Finder
    {
        /// <summary>
        /// Перечеслитель статусов характеристик жилого  фонда
        /// </summary>
        public enum Statuses
        {
            /// <summary>
            /// Генерируется
            /// </summary>
            Generating = 1,

            /// <summary>
            /// Сгенерирован
            /// </summary>
            Generated = 2,

            /// <summary>
            /// Загружается
            /// </summary>
            Loading = 3,

            /// <summary>
            /// Загружен
            /// </summary>
            Loaded = 4,

            /// <summary>
            /// В процессе учета
            /// </summary>
            Considering = 5,

            /// <summary>
            /// Учтен
            /// </summary>
            Considered = 6,

            /// <summary>
            /// Удален
            /// </summary>
            Deleted = 7,

            /// <summary>
            /// Учтен с ошибками
            /// </summary>
            ConsideredWithErrors = 8
        }

        [DataMember]
        public int nzp_zgt { set; get; }

        /// <summary>
        /// соглашение
        /// </summary>
        [DataMember]
        public FnSubsContract contact { set; get; }

        
        /// <summary>
        /// Номер документа
        /// </summary>
        [DataMember]
        public string num_th { set; get; }

        /// <summary>
        /// дата документа
        /// </summary>
        [DataMember]
        public string dat_th { set; get; }

        /// <summary>
        /// код статуса
        /// </summary>
        [DataMember]
        public int nzp_status { set; get; }

        /// <summary>
        /// Статус
        /// </summary>
        [DataMember]
        public string status { set; get; }        

        /// <summary>
        /// Наименование организации сформировавшей документ
        /// </summary>
        [DataMember]
        public string name_org { set; get; }


        /// <summary>
        /// Наименование подразделения организации сформировавшей документ
        /// </summary>
        [DataMember]
        public string name_podr { set; get; }

        /// <summary>
        /// ИНН
        /// </summary>
        [DataMember]
        public string inn { set; get; }

        /// <summary>
        /// КПП
        /// </summary>
        [DataMember]
        public string kpp { set; get; }

        /// <summary>
        /// код муниципального образования
        /// </summary>
        [DataMember]
        public int nzp_vill { set; get; }

        /// <summary>
        /// Муниципальное образование
        /// </summary>
        [DataMember]
        public string vill { set; get; }

        

        /* уже есть в Finder
        /// <summary>
        /// Период плана, дата начала
        /// </summary>
        [DataMember]
        public string date_begin { set; get; }
        */

        /// <summary>
        /// Период плана, дата окончания
        /// </summary>
        [DataMember]
        public string date_create_plan { set; get; }

        /// <summary>
        /// Период плана, дата окончания
        /// </summary>
        [DataMember]
        public string date_end { set; get; }

        /// <summary>
        /// район
        /// </summary>
        [DataMember]
        public string rajon { set; get; }

        /// <summary>
        /// Префикс района (Р-н, Г)
        /// </summary>
        [DataMember]
        public string rajon_prefix { set; get; }

        /// <summary>
        /// Кем создан
        /// </summary>
        [DataMember]
        public string created_by { get; set; }

        /// <summary>
        /// Когда создано
        /// </summary>
        [DataMember]
        public string created_on { set; get; }

        /// <summary>
        /// Кто изменил
        /// </summary>
        [DataMember]
        public string changed_by { get; set; } 
       



        public FnSubsTehHarGilFond()
            : base()
        {
            nzp_zgt = 0;
            contact = new FnSubsContract();
            name_org = "";
            inn = "";
            kpp = "";
            nzp_vill = 0;
            date_begin = "";
            date_end = "";
            created_by = "";
            changed_by = "";


        }

    }
}



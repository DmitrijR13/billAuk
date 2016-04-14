using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using STCLINE.KP50.Global;
using System.Data;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Sprav
    {
        [OperationContract]
        bool TableExists(Finder finder, out Returns ret);

        [OperationContract]
        Returns WebDataTable(Finder finder, enSrvOper srv);

        [OperationContract]
        TDocumentBase GetDocumentBase(TDocumentBase finder, out Returns ret);

        [OperationContract]
        List<_Service> ServiceLoad(Service finder, out Returns ret);

        [OperationContract]
        List<_Service> CountsLoad(Finder finder, out Returns ret);

        [OperationContract]
        List<_Service> CountsLoadFilter(Finder finder, out Returns ret, int nzp_kvar);

        [OperationContract]
        List<_Point> PointLoad_WebData(Finder finder, out Returns ret);

        [OperationContract]
        List<_Point> PointLoad(out Returns ret, out _PointWebData p);

        [OperationContract]
        List<_ResY> ResYLoad(out Returns ret);

        [OperationContract]
        List<_TypeAlg> TypeAlgLoad(out Returns ret);

        [OperationContract]
        List<_Help> LoadHelp(int nzp_user, int cur_page, out Returns ret);

        [OperationContract]
        string GetInfo(long kod, int tip, out Returns ret);

        [OperationContract]
        List<_Supplier> SupplierLoad(Supplier finder, enTypeOfSupp type, out Returns ret);

        [OperationContract]
        List<_Supplier> LoadSupplierByArea(Supplier finder, out Returns ret); // Загрузка договоров относительно территории

        [OperationContract]
        List<FileName> FileNameLoad(FileName finder, out Returns ret);

        [OperationContract]
        Returns SaveSupplier(Supplier finder);

        [OperationContract]
        Returns SavePayer(Payer finder);

        [OperationContract]
        Returns SavePayerContract(Payer finder);

        [OperationContract]
        Returns SavePayerContractNewFd(Payer finder);

        [OperationContract]
        Returns SaveBank(Bank finder);

        [OperationContract]
        List<Payer> PayerBankLoad(Payer finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<Payer> BankPayerLoad(Payer finder, out Returns ret);

        [OperationContract]
        void PayerBankForIssrpF101(out Returns ret);

        [OperationContract]
        List<Payer> PayerBankLoadContract(Payer finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<Namereg> NameregLoad(Namereg finder, out Returns ret);

        [OperationContract]
        PackDistributionParameters GetPackDistributionParameters(out Returns ret);

        [OperationContract]
        Returns RefreshSpravClone(Finder finder);

        [OperationContract]
        List<Town> LoadTown(Town finder, out Returns ret);

        [OperationContract]
        List<_reestr_unloads> LoadUploadedReestrList(Finder finder, out Returns ret);

        [OperationContract]
        List<_reestr_downloads> LoadDownloadedReestrList(Finder finder, out Returns ret);

        [OperationContract]
        List<unload_exchange_sz> LoadListExchangeSZ(Finder finder, out Returns ret);

        [OperationContract]
        List<Payer> LoadPayerTypes(Finder finder, out Returns ret);

        [OperationContract]
        Returns ContrRenameDog(Payer finder);

        [OperationContract]
        List<Measure> LoadMeasure(Measure finder, out Returns ret);

        [OperationContract]
        List<CalcMethod> LoadCalcMethod(CalcMethod finder, out Returns ret);

        [OperationContract]
        List<Land> LoadLand(Land finder, out Returns ret);

        [OperationContract]
        List<Stat> LoadStat(Stat finder, out Returns ret);

        [OperationContract]
        List<Town> LoadTown2(Town finder, out Returns ret);

        [OperationContract]
        List<Rajon> LoadRajon(Rajon finder, out Returns ret);

        [OperationContract]
        List<PackTypes> LoadPackTypes(PackTypes finder, out Returns ret);

        [OperationContract]
        Returns DeleteReestrTula(_reestr_unloads finder);

        [OperationContract]
        Returns DeleteDownloadReestrTula(Finder finder, int nzp_reestr);

        [OperationContract]
        Returns DeleteDownloadReestrMariyEl(Finder finder, int nzp_reestr);

        [OperationContract]
        List<BankPayers> BankPayersLoad(Supplier finder, enTypeOfSupp type, out Returns ret);

        [OperationContract]
        List<BankPayers> BankPayersLoadBC(Supplier finder, enTypeOfSupp type, out Returns ret);

        [OperationContract]
        List<Payer> GetPayersDogovor(int nzpUser, Payer.ContragentTypes typePayer, out Returns ret);

        [OperationContract]
        List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret);

        [OperationContract]
        List<Bank> GetBanksExecutingPayments(int nzpUser, out Returns ret);

        [OperationContract]
        List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret);

        [OperationContract]
        List<int> LoadListUchastok(Finder finder, out Returns ret);

        /// <summary>
        /// Возвращает список формул расчета
        /// </summary>
        /// <param name="finder">Параметры поиска</param>
        /// <param name="ret">Результат выполнения операции</param>
        /// <returns>Список формул</returns>
        [OperationContract]
        List<Formuls> GetFormuls(FormulsFinder finder, out Returns ret);

        /// <summary>
        /// Загрузка справочника платежных агентов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<tula_s_bank> LoadPayerAgents(Finder finder, out Returns ret);

        /// <summary>
        /// Удаление из справочника платежных агентов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        Returns DeletePayerAgent(Finder finder, int id);

        [OperationContract]
        Returns SaveContract(ContractFinder finder, enSrvOper oper);


        [OperationContract]
        List<ContractClass> ContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret);

        [OperationContract]
        List<int> BanksForOneSuppLoad(Supplier finder, out Returns ret, out bool IfCanChangePayers);

        [OperationContract]
        List<Payer> PayersLoad(Payer finder, out Returns ret);

        /// <summary>
        /// Добавление платежного агента
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        Returns AddPayerAgent(Finder finder, tula_s_bank agent);


        /// <summary>
        /// Сохранение платежного агента
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        Returns SavePayerAgent(Finder finder, tula_s_bank agent);

        /// <summary>
        /// получить список контрагентов 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<Payer> LoadPayers(Payer finder, out Returns ret);

        [OperationContract]
        List<Payer> LoadPayersNewFd(Payer finder, out Returns ret);

        /// <summary>
        /// получить список контрагентов для формы 'настройка уникальных кодов' 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<Payer> LoadPayersContragents(Payer finder, out Returns ret);

        /// <summary>
        /// Получения списка кодов квитанций
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<KodSum> GeListKodSum(KodSum finder, out Returns ret);

        /// <summary>
        /// Слияние конрагентов
        /// </summary>
        /// <param name="original_id"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        [OperationContract]
        Returns MergeContr(Payer finder, List<int> list);

        [OperationContract]
        Returns DeletePayerContract(Payer finder);
       
        [OperationContract]
        Returns UpdateCashSpravTable(Finder finder);

        [OperationContract]
        List<Supplier> LoadDogovorByPoints(Finder finder, out Returns ret);

        [OperationContract]
        List<Finder> LoadPointsByScopeDogovor(ScopeAdress finder, out Returns ret);

        [OperationContract]
        Returns AddNewToScopeAdress(ScopeAdress finder);

        [OperationContract]
        List<ScopeAdress> GetAdressesByScope(ScopeAdress finder, out Returns ret);

        [OperationContract]
        Returns DeleteAdressFromScope(ScopeAdress finder);

        [OperationContract]
        Returns CheckUsingScopeByChilds(ScopeAdress finder);

        [OperationContract]
        Returns SaveSupplierChanges(ContractFinder finder, enSrvOper oper);
        [OperationContract]
        List<ContractClass> NewFdContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret);

        [OperationContract]
        RecordMonth GetCalcMonth();

        [OperationContract]
        List<PrmTypes> LoadKodSum(Finder finder, out Returns ret);

        [OperationContract]
        List<_ResY> LoadResY(string find_nzp_res, out Returns ret);

        [OperationContract]
        List<PrmTypes> GetListNzpCntServ(Finder finder, out Returns ret);

        /// <summary>
        /// сохранение договоров ЖКУ, которые будут участвовать в управлении переплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="oper"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        [OperationContract]
        Returns SaveContractAllowOv(Finder finder, enSrvOper oper, List<ContractClass> list);

        /// <summary>
        /// список отобранных переплат для управления переплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<OverpaymentForDistrib> GetOverpaymentForDistrib(OverpaymentForDistrib finder, out Returns ret);

        /// <summary>
        /// Сохраняем выбранность переплат
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns SaveSelectedOverpaymentForDistrib(OverpaymentForDistrib finder, List<OverpaymentForDistrib> list);

        /// <summary>
        /// Выбранные договора для распределения в режиме управления переплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns GetSelectedDogForDistribOv(OverpaymentForDistrib finder);

        /// <summary>
        /// Прерывание процесса управления переплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns InterruptOverpaymentProcess(OverpaymentForDistrib finder);

        /// <summary>
        /// Список кодов домов у контрагентов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<House_kodes> GetAliasDomList(House_kodes finder, out Returns ret);

        /// <summary>
        /// Сохранение/редактирование  кодов домов у контрагентов
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns EditAliasDomList(House_kodes finder);

        /// <summary>
        /// удаление  кодов домов у контрагентов
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns DeleteAliasDomList(House_kodes finder);
        /// <summary>
        /// Обновляет таблицу supplier_point
        /// </summary>
        /// <param name="finderScopeAdress"></param>
        /// <returns></returns>
        [OperationContract]
        Returns RefreshBanksForContract(ScopeAdress finderScopeAdress);
        /// <summary>
        /// Проверяет наличие открытых услуг на ЛС по указанным адресам
        /// </summary>
        /// <param name="scopeAdress"></param>
        /// <returns></returns>
        [OperationContract]
        Returns CheckToOpenServOnLSByAdress(List<ScopeAdress> scopeAdress);

    }

    public interface ISpravRemoteObject : I_Sprav, IDisposable { }

    //----------------------------------------------------------------------
    [DataContract]
    public struct _TypeAlg
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_type_alg { get; set; }
        [DataMember]
        public string name_type { get; set; }
    }
    //----------------------------------------------------------------------
    public static class TypeAlgs
    //----------------------------------------------------------------------
    {
        public static List<_TypeAlg> AlgList = new List<_TypeAlg>();
    }

    //----------------------------------------------------------------------
    public class Services //
    //----------------------------------------------------------------------
    {
        public List<_Service> ServiceList = new List<_Service>(); //
        public List<_Service> CountsList = new List<_Service>(); //
    }


    //----------------------------------------------------------------------
    [DataContract]
    public struct _ResY  //справочник res_y
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_res;
        [DataMember]
        public int nzp_y;
        [DataMember]
        public string name_y;
    };
    //----------------------------------------------------------------------
    public static class ResYs //основные справочники (потом сделать как базовый класс)
    //----------------------------------------------------------------------
    {
        public enum ResTypes
        {
            /// <summary>
            /// Состояния лицевых счетов (открыт, закрыт, не определено)
            /// </summary>
            LsState = 18,

            /// <summary>
            /// Типы лицевых счетов (население, бюджет (нежилые), прочее)
            /// </summary>
            LsType = 9999
        }

        public static List<_ResY> ResYList = new List<_ResY>(); //
    }

    /// <summary>
    ///Группы документов 
    /// </summary>
    public enum DocGroups
    {
        /// <summary>
        /// Перекидки
        /// </summary>
        Perekidki = 1
    }




    [DataContract]
    public class BankPayers : Finder
    //----------------------------------------------------------------------
    {
        string _adres_supp;
        string _phone_supp;
        string _geton_plat;
        string _kod_supp;
        //string _summ_supp;
        string _name_supp;

        [DataMember]
        public long num { get; set; }
        [DataMember]
        public long nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get { return Utils.ENull(_name_supp); } set { _name_supp = value; } }
        [DataMember]
        public int _checked { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public int have_proc { get; set; }
        [DataMember]
        public Boolean check { get; set; }

        [DataMember]
        public string adres_supp { get { return Utils.ENull(_adres_supp); } set { _adres_supp = value; } }
        [DataMember]
        public string phone_supp { get { return Utils.ENull(_phone_supp); } set { _phone_supp = value; } }
        [DataMember]
        public string geton_plat { get { return Utils.ENull(_geton_plat); } set { _geton_plat = value; } }
        [DataMember]
        public string kod_supp { get { return Utils.ENull(_kod_supp); } set { _kod_supp = value; } }
        [DataMember]
        public string summ_supp { get; set; }

    }


    [DataContract]
    public class Supplier : Finder
    {
        [DataMember]
        public long nzp_supp { get; set; }

        [DataMember]
        public string name_supp { get; set; }

        public Supplier()
            : base()
        {
            nzp_supp = 0;
            name_supp = "";
        }
    }

    [DataContract]
    public class FileName : Finder
    {
        [DataMember]
        public long nzp_file { get; set; }
        [DataMember]
        public string file_name { get; set; }

        public FileName()
            : base()
        {
            nzp_file = 0;
            file_name = "";
        }
    }

    [DataContract]
    public class ContractFinder : Finder
    {
        [DataMember]
        public long nzp_supp { get; set; }

        [DataMember]
        public string name_supp { get; set; }

        [DataMember]
        public int nzp_payer_agent { get; set; }

        [DataMember]
        public int nzp_payer_princip { get; set; }

        [DataMember]
        public int nzp_payer_supp { get; set; }
        [DataMember]
        public int nzp_payer_podr { get; set; }
        [DataMember]
        public List<int> nzp_wp { get; set; }

        [DataMember]
        public Int16 dpd { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_scope { get; set; }
        [DataMember]
        public int nzp_fb { get; set; }
        [DataMember]
        public int nzp_fd { get; set; }
        [DataMember]
        public int fn_dogovor_bank_lnk_id { get; set; }

        [DataMember]
        public int allow_overpayments { get; set; }

        public ContractFinder()
            : base()
        {
            nzp_supp = 0;
            name_supp = "";
            allow_overpayments = 0;
            nzp_payer_agent = nzp_payer_princip = nzp_payer_supp = 0;
            nzp_wp = new List<int>();
            dpd = 0;
            nzp_serv = 0;
            fn_dogovor_bank_lnk_id = 0;
        }
    }

    [DataContract]
    public class ContractClass
    {
        [DataMember]
        public int num { get; set; }

        [DataMember]
        public long nzp_supp { get; set; }

        [DataMember]
        public int nzp_payer_agent { get; set; }
        [DataMember]
        public string agent { get; set; }
        [DataMember]
        public int nzp_payer_princip { get; set; }
        [DataMember]
        public string princip { get; set; }
        [DataMember]
        public int nzp_payer_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int nzp_payer_podr { get; set; }
        [DataMember]
        public string podr { get; set; }
        [DataMember]
        public string adres_supp { get; set; }
        [DataMember]
        public int nzp_fb { get; set; }
        [DataMember]
        public int nzp_scope { get; set; }

        [DataMember]
        public string geton_plat { get; set; }

        [DataMember]
        public int have_proc { get; set; }

        [DataMember]
        public string kod_supp { get; set; }

        [DataMember]
        public Int16 dpd { get; set; }

        [DataMember]
        public List<int> nzp_wp { get; set; }        

        [DataMember]
        public int mark { get; set; }

        [DataMember]
        public Int16 allow_overpayments { get; set; }

        public ContractClass()
            : base()
        {
            nzp_supp = 0;
            nzp_fb = 0;
            nzp_scope = 0;
            nzp_payer_podr = allow_overpayments = 0;
            agent = "";
            princip = "";
            name_supp = adres_supp = geton_plat = kod_supp = "";
            nzp_payer_agent = nzp_payer_princip = have_proc = nzp_payer_supp = num = 0;
            nzp_wp = new List<int>();
            dpd = 0;
            mark = 0;
        }
    }


    [DataContract]
    public class OverpaymentForDistrib : Finder
    {
        [DataMember]
        public int id { get; set; }

        [DataMember]
        public int nzp_payer { get; set; }

        [DataMember]
        public string payer { get; set; }

        [DataMember]
        public string sum_payer { get; set; }

        [DataMember]
        public decimal sum_negative_outsaldo_payer { get; set; }

        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public string supp { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public decimal sum_outsaldo { get; set; }

        [DataMember]
        public int ls_count { get; set; }

        [DataMember]
        public bool only_positive { get; set; }

        [DataMember]
        public bool only_negative { get; set; }

        [DataMember]
        public bool only_negative_for_intf { get; set; }

        /// <summary>
        /// 1 - установить все, 2- снять все, 0 - игнорировать
        /// </summary>
        [DataMember]
        public int all_selected { get; set; }

        [DataMember]
        public bool mark { get; set; }

        [DataMember]
        public int num { get; set; }

        public OverpaymentForDistrib()
            : base()
        {
            id = nzp_payer = nzp_supp = nzp_serv = num = 0;
            sum_negative_outsaldo_payer = sum_outsaldo = ls_count = 0;
            sum_payer = "";
            payer = supp = service = "";
            mark = only_positive = only_negative = only_negative_for_intf = false;
            all_selected = 0;
        }
    }

    [DataContract]
    public class AutoPerekidka
    {
        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public long nzp_supp_1 { get; set; }

        [DataMember]
        public long nzp_supp_2 { get; set; }

        [DataMember]
        public string pref { get; set; }
        public AutoPerekidka()
            : base()
        {
            nzp_supp_1 = nzp_supp_2 = nzp_serv = 0;
            pref = "";
        }
    }

    public enum enTypeOfSupp
    {
        /// <summary>
        /// все поставщики
        /// </summary>
        None = 1,
        /// <summary>
        /// поставщики, которые отсутствуют в списке контрагентов
        /// </summary>
        NotInListPayers = 2
    }



    //----------------------------------------------------------------------
    [DataContract]
    public class Payer : Finder
    //----------------------------------------------------------------------
    {
        string _payer;
        string _npayer;
        string _bank;
        string _name_supp;
        int _nzp_bank;
        int _nzp_payer;
        string _short_name;
        string _adress;
        string _phone;

        [DataMember]
        public int nzp_payer { get { return _nzp_payer; } set { _nzp_payer = value; } }

        [DataMember]
        public string payer { get { return Utils.ENull(_payer); } set { _payer = value; } }
        [DataMember]
        public string npayer { get { return Utils.ENull(_npayer); } set { _npayer = value; } }
        [DataMember]
        public long nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get { return Utils.ENull(_name_supp); } set { _name_supp = value; } }
        [DataMember]
        public long is_erc { get; set; }
        [DataMember]
        public long is_bank { get; set; }
        [DataMember]
        public int _checked { get; set; }

        [DataMember]
        public int nzp_bank { get { return _nzp_bank; } set { _nzp_bank = value; } }
        [DataMember]
        public new string bank { get { return Utils.ENull(_bank); } set { _bank = value; } }
        [DataMember]
        public string short_name { get { return Utils.ENull(_short_name); } set { _short_name = value; } }
        [DataMember]
        public string adress { get { return Utils.ENull(_adress); } set { _adress = value; } }
        [DataMember]
        public string phone { get { return Utils.ENull(_phone); } set { _phone = value; } }

        //ИНН
        [DataMember]
        public string inn { set; get; }

        //КПП
        [DataMember]
        public string kpp { set; get; }

        //Фильтрация типов контрагентов
        [DataMember]
        public List<int> include_types { get; set; }
        [DataMember]
        public List<int> exclude_types { get; set; }

        /// <summary>
        /// Код типа контрагента (ПУ, УК, Организация по приему платежей и т.д.)
        /// </summary>
        [DataMember]
        public int nzp_type { get; set; }

        /// <summary>
        /// Тип контрагента
        /// </summary>
        [DataMember]
        public string type_name { set; get; }

        [DataMember]
        public string ks { set; get; }

        [DataMember]
        public string bik { set; get; }

        [DataMember]
        public string city { set; get; }

        [DataMember]
        public string nzp_payers { set; get; }

        [DataMember]
        public int id_bc_type { set; get; }

        public enum ContragentTypes
        {
            /// <summary>Неопределенное значение</summary>
            DontUseType = -100,

            /// <summary>Неопределенное значение</summary>
            None = 0,

            /// <summary>Системный</summary>
            System = 1,

            /// <summary>Поставщик услуг</summary>
            ServiceSupplier = 2,

            /// <summary>Управляющая организация</summary>
            UK = 3,

            /// <summary>Организация, осуществляющая прием платежей</summary>
            PayingAgent = 4,

            /// <summary>Агент (Расчетный центр)</summary>
            Agent = 5,

            /// <summary>Ресурсоснабжающая организация</summary>
            ResourceSupplying = 6,

            /// <summary>Арендатор жилья</summary>
            Tenant = 7,

            /// <summary>Банк</summary>
            Bank = 8,

            /// <summary>Субабонент</summary>
            Subsubscriber = 9,

            /// <summary>Принципал</summary>
            Princip = 10,

            /// <summary>Подрядчик</summary>
            Podr = 11,

            /// <summary>Клиринговая система</summary>
            ClearingSystem = 12
        }


        public Payer()
            : base()
        {
            nzp_payer = 0;
            nzp_payers = "";
            payer = "";
            npayer = "";
            nzp_supp = 0;
            name_supp = "";
            short_name = "";
            adress = "";
            phone = "";
            is_erc = 0;
            is_bank = 0;
            _checked = 0;
            ks = city = bik = "";
            inn = "";
            kpp = "";
            nzp_type = 0;
            type_name = "";
            id_bc_type = 0;
        }
    }

    /// <summary> Доп. информация о контрагенте в "Выгрузке банк-клиент" </summary>
    [DataContract]
    public class InfoPayerBankClient : Payer
    {
        /// <summary> Порядковый номер </summary>
        [DataMember]
        public Int32 OrderNumber { get; set; }

        /// <summary> Идентификатор реквизита договора </summary>
        [DataMember]
        public Int32 nzp_fd { get; set; }

        /// <summary> Идентификатор договора ЖКУ </summary>
        [DataMember]
        public Int32 nzp_supp { get; set; }

        /// <summary> Номер договора  </summary>
        [DataMember]
        public string num_dog { get; set; }

        /// <summary> Банк контрагента </summary>
        [DataMember]
        public string payer_bank { get; set; }

        /// <summary> Тип банковского формата выгрузки </summary>
        [DataMember]
        public string bc_type { get; set; }

        /// <summary> Сумма перечислений контагенту </summary>
        [DataMember]
        public Decimal sum_send { get; set; }

        /// <summary> Признак выбора </summary>
        public Boolean IsCheck { get; set; }

        public InfoPayerBankClient()
        {
            num_dog = string.Empty;
            payer_bank = string.Empty;
            bc_type = string.Empty;
            sum_send = 0.00m;
            IsCheck = false;
        }
    }


    /// <summary>
    /// Зарезервированные банки (пункты приема платежей, филиалы)
    /// </summary>
    public enum Banks
    {
        /// <summary>
        /// Суперпачка
        /// </summary>
        Superpack = 1000,

        /// <summary>
        /// Ручной платеж
        /// </summary>
        ManualPayment = 1998,

        /// <summary>
        /// ЕРЦ
        /// </summary>
        ERC = 1999,

        /// <summary>
        /// Безналичный платеж
        /// </summary>
        NonCashPayment = 79998
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Bank : Finder
    //----------------------------------------------------------------------
    {
        string _bank;
        string _short_name;
        string _adress;
        string _phone;
        string _payer;
        string _npayer;

        [DataMember]
        public int nzp_bank { get; set; }
        [DataMember]
        public new string bank { get { return Utils.ENull(_bank); } set { _bank = value; } }
        [DataMember]
        public string short_name { get { return Utils.ENull(_short_name); } set { _short_name = value; } }
        [DataMember]
        public string adress { get { return Utils.ENull(_adress); } set { _adress = value; } }
        [DataMember]
        public string phone { get { return Utils.ENull(_phone); } set { _phone = value; } }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public string payer { get { return Utils.ENull(_payer); } set { _payer = value; } }
        [DataMember]
        public string npayer { get { return Utils.ENull(_npayer); } set { _npayer = value; } }
        //[DataMember]
        //public int nzp_geu { get; set; }
        [DataMember]
        public int _checked { get; set; }

        public Bank()
            : base()
        {
            nzp_bank = 0;
            bank = "";
            nzp_payer = 0;
            short_name = "";
            adress = "";
            phone = "";
            payer = "";
            npayer = "";
            _checked = 0;
        }
    }





    [DataContract]
    public class Namereg : Finder
    {
        [DataMember]
        public int kod_namereg { get; set; }
        [DataMember]
        public string namereg { get; set; }
        [DataMember]
        public string ogrn { get; set; }
        [DataMember]
        public string inn { get; set; }
        [DataMember]
        public string kpp { get; set; }
        [DataMember]
        public string adr_namereg { get; set; }
        [DataMember]
        public string tel_namereg { get; set; }
        [DataMember]
        public string dolgnost { get; set; }
        [DataMember]
        public string fio_namereg { get; set; }

        public Namereg()
            : base()
        {
            kod_namereg = 0;
            namereg = "";
            ogrn = "";
            inn = "";
            kpp = "";
            adr_namereg = "";
            tel_namereg = "";
            dolgnost = "";
            fio_namereg = "";
        }
    }

    [DataContract]
    public class Measure : Finder
    {
        [DataMember]
        public int nzp_measure { get; set; }
        [DataMember]
        public string measure { get; set; }

        public Measure()
            : base()
        {
            nzp_measure = 0;
            measure = "";
        }
    }

    [DataContract]
    public class PackTypes : Finder
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string type_name { get; set; }

        public PackTypes()
            : base()
        {
            id = 0;
            type_name = "";
        }
    }

    [DataContract]
    public class CalcMethod : Finder
    {
        [DataMember]
        public int nzp_calc_method { get; set; }
        [DataMember]
        public string method_name { get; set; }

        public CalcMethod()
            : base()
        {
            nzp_calc_method = 0;
            method_name = "";
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public struct _reestr_unloads //реестр выгрузок для БС в Тулу
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_reestr { get; set; }
        [DataMember]
        public string name_file { get; set; }
        [DataMember]
        public string date_unload { get; set; }
        [DataMember]
        public string unloading_date { get; set; }
        [DataMember]
        public int user_unloaded { get; set; }
        [DataMember]
        public string name_user_unloaded { get; set; }
        [DataMember]
        public string ex_path { get; set; }
        [DataMember]
        public int nzp_exc { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
    };

    //----------------------------------------------------------------------
    [DataContract]
    public struct _reestr_downloads //реестр загрузок для БС в Тулу
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_download { get; set; }
        [DataMember]
        public string file_name { get; set; }
        [DataMember]
        public int nzp_type { get; set; }
        [DataMember]
        public string date_download { get; set; }
        [DataMember]
        public int user_downloaded { get; set; }
        [DataMember]
        public string name_user_downloaded { get; set; }
        [DataMember]
        public string branch_name { get; set; }
        [DataMember]
        public string day_month { get; set; }
        [DataMember]
        public string name_type { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public double proc { get; set; }
        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public string exc_path { get; set; }

        [DataMember]
        public string protocol { get; set; }

        public string getNameStatus(int nzp_status)
        {
            switch (nzp_status)
            {
                case (int)StatusWWB.InProcess: return "Загружается";
                case (int)StatusWWB.Success: return "Успешно";
                case (int)StatusWWB.Fail: return "Ошибка";
                case (int)StatusWWB.WithErrors: return "Загружено с ошибками";
                case (int)StatusWWB.FormatErr: return "Файл с нарушением формата";
                default: return "Статус отсутствует";
            }
        }

    };
    public enum StatusWWB
    {
        /// <summary>
        /// загружается
        /// </summary>
        InProcess = 0,
        /// <summary>
        /// успешно загружен
        /// </summary>
        Success = 1,
        /// <summary>
        /// Ошибка при загрузке
        /// </summary>
        Fail = -1,
        /// <summary>
        /// с ошибками
        /// </summary>
        WithErrors = 2,
        /// <summary>
        /// Нарушен формат
        /// </summary>
        FormatErr = 3


    }


    //----------------------------------------------------------------------
    [DataContract]
    public struct unload_exchange_sz //реестр выгрузок для БС в Тулу
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_ex_sz { get; set; }
        [DataMember]
        public string file_name { get; set; }
        [DataMember]
        public string dat_upload { get; set; }
        [DataMember]
        public string name_user_unloaded { get; set; }
        [DataMember]
        public double proc { get; set; }

        [DataMember]
        public string point { get; set; }

    };

    //----------------------------------------------------------------------
    [DataContract]
    public class tula_s_bank //справочник Платежных агентов
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public int nzp_bank { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public string branch_id { get; set; }
        [DataMember]
        public string branch_id_reestr { get; set; }
        [DataMember]
        public string branch_name { get; set; }
        [DataMember]
        public int FormatNumberUpload { get; set; }
        [DataMember]
        public int FormatNumberDownload { get; set; }

    };


    [DataContract]
    public class BCTypes : Finder
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string name_ { get; set; }

        [DataMember]
        public int is_active { get; set; }

        public BCTypes()
            : base()
        {
            id = 0;
            name_ = "";
            is_active = 0;
        }
    }

    [DataContract]
    public class KodSum : Finder
    {
        [DataMember]
        public int kod { get; set; }
        [DataMember]
        public string comment { get; set; }
        [DataMember]
        public int cnt_shkodes { get; set; }
        [DataMember]
        public int is_id_bill { get; set; }

        public KodSum()
            : base()
        {
            kod = 0;
            comment = "";
            cnt_shkodes = -1;
            is_id_bill = -1;
        }
    }

    [DataContract]
    public class ScopeAdress :Dom
    {
        [DataMember]
        public int cur_nzp_scope { get; set; }
        [DataMember]
        public int parent_nzp_scope { get; set; }
        [DataMember]
        public List<int> childs_nzp_scope { get; set; }
        [DataMember]
        public int nzp_scope_adres { get; set; }
        [DataMember]
        public int order_num { get; set; }
        [DataMember]
        public List<int> nzp_scope_adres_list { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        public enum ScopeUntil
        {
            None=0,
            Bank=1,
            Town=2,
            Rajon=3,
            Ulica=4,
            Dom=5
        }

        public ScopeAdress()
        {
            nzp_town = 0;
            nzp_raj = 0;
            nzp_ul = 0;
            nzp_dom = 0;
            cur_nzp_scope = 0;
            parent_nzp_scope = 0;
            childs_nzp_scope = new List<int>();
            nzp_scope_adres = 0;
            order_num = 0;
            nzp_scope_adres_list = new List<int>();
            nzp_supp = 0;
        }

        public ScopeUntil ScopeLevel
        {
            get
            {
                if (nzp_wp > 0 && nzp_town <= 0 && nzp_raj <= 0 && nzp_ul <= 0 && nzp_dom <= 0)
                {
                    return ScopeUntil.Bank;
                }
                if (nzp_wp > 0 && nzp_town >= 0 && nzp_raj <= 0 && nzp_ul <= 0 && nzp_dom <= 0)
                {
                    return ScopeUntil.Town;
                }
                if (nzp_wp > 0 && nzp_town > 0 && nzp_raj > 0 && nzp_ul <= 0 && nzp_dom <= 0)
                {
                    return ScopeUntil.Rajon;
                }
                if (nzp_wp > 0 && nzp_town > 0 && nzp_raj > 0 && nzp_ul > 0 && nzp_dom <= 0)
                {
                    return ScopeUntil.Ulica;
                }
                if (nzp_wp > 0 && nzp_town > 0 && nzp_raj > 0 && nzp_ul > 0 && nzp_dom > 0)
                {
                    return ScopeUntil.Dom;
                }
                return  ScopeUntil.None;
            }
        }

        public void SetAdress()
        {
            if (nzp_wp > 0 && nzp_town <= 0 && nzp_raj <= 0 && nzp_ul <= 0 && nzp_dom <= 0)
            {
                town = "Все города/районы";
                rajon = "Все населенные пункты";
                ulica = "Все улицы";
                ndom = "Все дома";
            }
            if (nzp_wp > 0 && nzp_town > 0 && nzp_raj <= 0 && nzp_ul <= 0 && nzp_dom <= 0)
            {
                rajon = "Все населенные пункты";
                ulica = "Все улицы";
                ndom = "Все дома";
    }

            if (nzp_wp > 0 && nzp_town > 0 && nzp_raj > 0 && nzp_ul <= 0 && nzp_dom <= 0)
            {
                ulica = "Все улицы";
                ndom = "Все дома";
            }

            if (nzp_wp > 0 && nzp_town > 0 && nzp_raj > 0 && nzp_ul > 0 && nzp_dom <= 0)
            {
                ndom = "Все дома";
            }
        }

    }
}


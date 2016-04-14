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
    public interface I_Pack
    {
        /// <summary> Выполнить операцию над пачкой и вернуть пачку
        /// </summary>
        [OperationContract]
        Pack OperateWithPackAndGetIt(PackFinder finder, enSrvOper oper, out Returns ret);

        /// <summary> Выполнить операцию над пачкой и вернуь результат
        /// </summary>
        [OperationContract]
        Returns OperateWithPack(PackFinder finder, Pack.PackOperations oper);

        [OperationContract]
        Pack_ls OperateWithPackLsAndGetIt(Pack_ls finder, Pack_ls.OperationsWithGetting oper, out Returns ret);

        [OperationContract]
        Returns OperateWithPackLs(Pack_ls finder, Pack_ls.OperationsWithoutGetting oper);

        [OperationContract]
        Returns OperateWithListPackLs(List<Pack_ls> finder, enSrvOper oper);

        [OperationContract]
        Returns UploadPackFromDBF(string nzp_user, string fileName);

        /// <summary> Загрузить место формирования
        /// </summary>
        [OperationContract]
        List<Bank> LoadBankForKassa(Bank finder, out Returns ret);

        /// <summary>
        /// список улиц
        /// </summary>
        [OperationContract]
        List<Ulica> LoadUlica(Ulica finder, out Returns ret);

        /// <summary>
        /// список домов
        /// </summary>
        [OperationContract]
        List<Dom> LoadDom(Dom finder, out Returns ret);
        
        /// <summary>
        /// список лицевых счетов
        /// </summary>
        [OperationContract]
        List<Ls> GetPackLsList(string finder, out Returns ret);

        /// <summary>
        /// список квартир
        /// </summary>
        [OperationContract]
        List<Ls> LoadKvar(Ls finder, out Returns ret);

        [OperationContract]
        List<Ls> LoadLsForKassa(Ls finder, out Returns ret);

        [OperationContract]
        List<Pack_ls> GetPackLs(Pack_ls finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<Pack> GetPack(PackFinder finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns SaveOperDay(Pack finder);

        [OperationContract]
        List<Pack_log> GetPackLog(Pack_log finder, out Returns ret);

        [OperationContract]
        List<BankRequisites> GetBankRequisites(BankRequisites finder, out Returns ret);

        [OperationContract]
        List<DogovorRequisites> GetDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<ContractRequisites> GetContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret);
        
        [OperationContract]
        bool ChangeBankRequisites(BankRequisites finder,enSrvOper oper ,out Returns ret);

        [OperationContract]
        bool ChangeDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        bool ChangeContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<FnSupplier> GetFnSupplier(FnSupplier finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns CancelPlat(Finder finder, List<Pack_ls> list);

        [OperationContract]
        List<Pack_errtype> LoadErrorTypes(Finder finder, out Returns ret);

        [OperationContract]
        List<Pack_errtype> GetBasketErr(Pack_ls finder, out Returns ret);

        [OperationContract]
        List<PackStatus> GetPackStatus(PackStatus finder, out Returns ret);

        [OperationContract]
        DataSet GetDistribLog(PackFinder finder, out Returns ret);

        [OperationContract]
        ReturnsType FindErrorInPackLs(PackFinder finder);

        [OperationContract]
        ReturnsType FindErrorInFnSupplier(PackFinder finder);

        [OperationContract]
        ReturnsType GenContDistribPayments(Payments finder);

        [OperationContract]
        decimal GetLsSum(Saldo finder, GetLsSumOperations operation, out Returns ret);

        [OperationContract]
        Returns UploadPackFromWeb(int nzpPack, int nzp_user);

        [OperationContract]
        Returns PutTaskDistribLs(Dictionary<int,int> listPackLs, int nzp_user);

        [OperationContract]
        List<ChargeForDistribSum> GetSumsForDistrib(ChargeForDistribSum finder, out Returns ret);

        [OperationContract]
        Returns SaveManualDistrib(List<ChargeForDistribSum> listfinder);

        [OperationContract]
        Returns CreatePackOverPayment(Pack finder);

        [OperationContract]
        Returns BankPayment(FinderAddPackage finder);

        [OperationContract]
        Returns SaveCheckSend(Delete_payment finder, out Returns ret);

        [OperationContract]
        List<BankPayers> FormingSpisok(List<BankPayers> allSpisok, FinderAddPackage finder, out Returns ret);

        [OperationContract]
        List<_TypeBC> LoadTypeBC(out Returns ret);

        [OperationContract]
        Returns AddFormat();

        [OperationContract]
        Returns SaveFormat(_TypeBC typ);

        [OperationContract]
        Returns DeleteFormat(_TypeBC typ);

        [OperationContract]
        List<_TagsBC> GetListTags(int indexFormat, out Returns ret);
    }

    public enum GetLsSumOperations
    { 
        GetNachKOplate,
        GetCurrentDolg, 
        GetSumOutSaldo
    }


    /// <summary>
    /// Операции с оплатами
    /// </summary>
    public enum PackLsOperations
    {        
        Distribute = 1,
        NotDistribute = 2
    }

    /// <summary>
    /// в корзине оплата
    /// </summary>
    public enum PriznakBasket
    {
        InBasket = 1,
        NotBasket = 2
    }

    [DataContract]
    public class PackStatus : Ls
    {
        [DataMember]
        public int nzp_st { get; set; }

        [DataMember]
        public string name_st { get; set; }

        public PackStatus()
            : base()
        {
            nzp_st = 0;
            name_st = "";
        }
    }

    [DataContract]
    public class Pack : Ls
    {
        /// <summary> Статусы пачки
        /// </summary>
        public enum Statuses
        {
            /// <summary> Открытая пачка (Касса)
            /// </summary>
            Open = 0,
            /// <summary> Пачка закрыта, но не выгружена в финансы (Касса)
            /// </summary>
            ClosedButNotUnloaded = -1,

            /// <summary> Пачка соответствует выписке банка, не распределена
            /// </summary>
            CorrespondToPackAndNotDistributed = 11,

            /// <summary> Пачка не распределена
            /// </summary>
            NotDistributed = 23,

            /// <summary> Пачка распределена
            /// </summary>
            Distributed = 21,

            /// <summary> Пачка распределена с ошибками
            /// </summary>
            DistributedWithErrors = 22,

            /// <summary> Ожидает распределения
            /// </summary>
            WaitingForDistribution = 41,

            /// <summary> Ожидает отмены распределения
            /// </summary>
            WaitingForCancellationOfDistribution = 42,

            /// <summary>
            /// не закрыта
            /// </summary>
            NotClosed =51

        }

        /// <summary>
        /// Операции с пачками
        /// </summary>
        public enum PackOperations
        {
            CloseKassaPack = 1,
            Delete = 2,
            Distribute = 3,
            CancelDistribution = 4,
            CancelDistributionAndDelete = 5,
            GetNextNumPackForOverPay = 6
        }
        
        [DataMember]
        public int nzp_pack { get; set; }

        [DataMember]
        public int nzp_bank { get; set; }

        [DataMember]
        public string bank { get; set; }

        [DataMember]
        public int num_pack { get; set; }
        
        [DataMember]
        public string snum_pack { get; set; }

        [DataMember]
        public string dat_pack { get; set; }

        [DataMember]
        public string dat_pack_po { get; set; }

        [DataMember]
        public string time_pack { get; set; }

        [DataMember]
        public string dat_uchet { get; set; }

        [DataMember]
        public int count_kv { get; set; }

        [DataMember]
        public decimal sum_pack { get; set; }

        [DataMember]
        public decimal sum_nach { get; set; }

        [DataMember]
        public decimal sum_izm { get; set; }

        [DataMember]
        public int flag { get; set; }

        [DataMember]
        public string status { get; set; }

        [DataMember]
        public string file_name { get; set; }

        [DataMember]
        public string erc_code { get; set; }

        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public int nzp_payer { get; set; }

        [DataMember]
        public int nzp_payer2 { get; set; }

        [DataMember]
        public string name_supp { get; set; }

        [DataMember]
        public string payer { get; set; }

        [DataMember]
        public int par_pack { get; set; }

        [DataMember]
        public string version_pack { get; set; }

        [DataMember]
        public List<Pack_ls> listPackLs { get; set; }

        [DataMember]
        public List<Pack> listPack { get; set; } //Список подпачек

        [DataMember]
        public decimal sum_distr { get; set; }

        [DataMember]
        public decimal sum_not_distr { get; set; }

        // признак суперпачки
        [DataMember]
        public bool is_super_pack
        {
            get { return par_pack > 0 && nzp_pack == par_pack; }
            set { }
        }

        [DataMember]
        public string super_pack
        {
            get { return is_super_pack ? "Да" : ""; }
            set { }
        }

        [DataMember]
        public int pack_type { get; set; }

        public Pack()
            : base()
        {
            nzp_pack = 0;
            nzp_bank = 0;
            bank = "";
            num_pack = 0;
            snum_pack = "";
            dat_pack = "";
            dat_pack_po = "";
            time_pack = "";
            dat_uchet = "";
            count_kv = 0;
            sum_pack = 0;
            sum_nach = 0;
            sum_izm = 0;
            flag = 0;
            status = "";
            erc_code = "";
            year_ = 0;
            file_name = "";
            nzp_payer = 0;
            nzp_payer2 = 0;
            payer = "";
            par_pack = 0;
            version_pack = "!1.00";
            sum_distr = sum_not_distr = 0;
            pack_type = 0;
            name_supp = "";
        }
    }

    [DataContract]
    public class PackFinder : Pack
    {
        [DataMember]
        public string dat_pack_po { get; set; }

        [DataMember]
        public string dat_uchet_po { get; set; }

        [DataMember]
        public bool isCalcItogo { get; set; }

        [DataMember]
        public decimal sum_pack_po { get; set; }

        [DataMember]
        public string dat_vvod_s { get; set; }

        [DataMember]
        public string dat_vvod_po { get; set; }

        [DataMember]
        public decimal g_sum_ls_s { get; set; }

        [DataMember]
        public decimal g_sum_ls_po { get; set; }

        [DataMember]
        public int status_for_opl { get; set; }
        
        [DataMember]
        public int inbasket { get; set; }

        [DataMember]
        public int nzp_supp { get; set; }

        public PackFinder()
            : base()
        {
            dat_pack_po = "";
            dat_uchet_po = "";
            isCalcItogo = true;
            sum_pack_po = 0;
            dat_vvod_s = "";
            dat_vvod_po = "";
            g_sum_ls_s = 0;
            g_sum_ls_po = 0;
            status_for_opl = 0;
            inbasket = 0;
            nzp_supp = 0;
        }
    }

    [DataContract]
    public class Pack_ls : Pack
    {
        /// <summary>
        /// Операции с оплатами с возвратом оплаты
        /// </summary>
        public enum OperationsWithGetting
        {
            LoadFromKassa,
            SaveToKassa,
            SavePackLs
        }

        /// <summary>
        /// Операции с оплатами без возврата оплаты
        /// </summary>
        public enum OperationsWithoutGetting
        {
            DeleteFromKassa,
            DeleteFromFinances,
            FinanceSave,
            ChangeCase,
            ShowInCase,
            CheckPkod,
            Distribute,
            CancelDistribution
        }

        /// <summary>
        /// Перечислитель ошибок
        /// </summary>
        public enum err {
            lsIsClosed = 1001
        }

        [DataMember]
        public int nzp_pack_ls { get; set; }

        [DataMember]
        public int prefix_ls { get; set; }        

        [DataMember]
        public decimal g_sum_ls { get; set; }

        [DataMember]
        public decimal g_sum_ls_po { get; set; }

        [DataMember]
        public decimal sum_ls { get; set; }

        [DataMember]
        public decimal sum_peni { get; set; }

        [DataMember]
        public string dat_month { get; set; }

        [DataMember]
        public int kod_sum { get; set; }

        [DataMember]
        public int paysource { get; set; } //0 - источник платежа 1 - по умолчанию

        [DataMember]
        public int id_bill { get; set; } //код квитанции  номер квитанции в месяце - 0
   
        [DataMember]
        public string dat_vvod { get; set; }

        [DataMember]
        public string dat_vvod_po { get; set; }

        [DataMember]
        public string dat_uchet_po { get; set; }
   
        [DataMember]
        public int info_num { get; set; }

        [DataMember]
        public int inbasket { get; set; } //признак нахождения оплаты в корзине

        [DataMember]
        public int incase { get; set; } //признак нахождения оплаты в портфеле

        [DataMember]
        public int unl { get; set; } // платеж перегружен / не перегружен - при выгрузке 1, по умолчанию 0   
     
        [DataMember]
        public string alg { get; set; }

        [DataMember]
        public int count_nedop { get; set; } //Количество указанных плательщиком недопоставок

        [DataMember]
        public int count_izm { get; set; } //Количество указанных плательщиком изменений в суммах оплаты

        [DataMember]
        public List<PuVals> puVals { get; set; }

        [DataMember]
        public List<GilSum> gilSums { get; set; }

        [DataMember]
        public int nzp_err { get; set; }

        /// <summary>
        /// Новый ЛС вместо закрытого
        /// </summary>
        [DataMember]
        public string nzp_kvar_new { get; set; }

        /// <summary>
        /// Новый прификс
        /// </summary>
        [DataMember]
        public string pref_new { get; set; }

        /// <summary>
        /// Новый номер ЛС вместо закрытого
        /// </summary>
        [DataMember]
        public string num_ls_new { get; set; }

        /// <summary>
        /// Новый  платежный код вместо закрытого
        /// </summary>
        [DataMember]
        public string pkod_new { get; set; }

        [DataMember]
        public bool is_manual { get; set; }
        [DataMember]
        public string errors { get; set; }

        [DataMember]
        public int page { get; set; }// из какой страницы вызывается

        public Pack_ls()
            : base()
        {
            alg = "";
            g_sum_ls_po = 0;
            dat_uchet_po = "";
            dat_vvod_po = "";
            nzp_err = 0;
            nzp_pack_ls = 0;
            prefix_ls = 0;
            g_sum_ls = 0;
            sum_ls = 0;
            sum_peni = 0;          
            dat_month = "";
            kod_sum = 0;
            paysource = 0;
            id_bill = 0;
            dat_vvod = "";
            info_num = 0;
            unl = 0;
            incase = 0;
            count_izm = 0;
            count_nedop = 0;
            nzp_kvar_new = "";
            pref_new = "";
            num_ls_new = "";
            pkod_new = "";
            is_manual = false;
            errors = "";
            page = 0;
        }
    }

    [DataContract]
    public class GilSum  
    {
        [DataMember]
        public int nzp_sums { get; set; }

        [DataMember]
        public int nzp_pack_ls { get; set; }

        [DataMember]
        public string sum_oplat { get; set; } //Сумма оплаты, может быть пустой, если только недопоставку указать

        [DataMember]
        public string day_nedo { get; set; }//Количество дней недопоставки, может быть пустым, если ввели только сумму оплаты

        [DataMember]
        public int ordering { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        public GilSum()
            : base()
        {
           
            nzp_sums = 0;
            nzp_pack_ls = 0;
            sum_oplat = "";
            day_nedo = "";
            ordering = 0;
            nzp_serv = 0;
        }
    }


    [DataContract]
    public class PuVals
    {
        [DataMember]
        public int nzp_spv { get; set; }

        [DataMember]
        public int nzp_pack_ls { get; set; }

        [DataMember]
        public string num_cnt { get; set; }

        [DataMember]
        public decimal val_cnt { get; set; }

        [DataMember]
        public int ordering { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        public PuVals()
            : base()
        {

            nzp_spv = 0;
            nzp_pack_ls = 0;
            num_cnt = "";
            val_cnt = 0;
            ordering = 0;
            nzp_serv = 0;
        }
    }
    [DataContract]
    public class Pack_log : Pack_ls
    {
        [DataMember]
        public int nzp_plog { get; set; }

        [DataMember]
        public string dat_oper { get; set; }

        [DataMember]
        public string dat_log { get; set; }

        [DataMember]
        public string txt_log { get; set; }

        [DataMember]
        public int tip_log { get; set; }

        public Pack_log()
            : base()
        {
            nzp_plog = 0;
            dat_oper = "";
            dat_log = "";
            txt_log = "";
            tip_log = 0;
        }
    }

    [DataContract]
    public class Pack_errtype: Pack_ls 
    {       
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string note { get; set; }

        public Pack_errtype()
            : base()
        {
            nzp_err = 0;
            name = "";
            note = "";
        }
    }

   
    [DataContract]
    public class BankRequisites : Ls
    {
        //nzp_fb
        [DataMember]
        public int nzp_fb { set; get;}

        [DataMember]
        public long nzp_payer { set; get; }

        //номер
        [DataMember]
        public int num_count { set; get; }

        //Название банка
        [DataMember]
        public string bank_name { set; get; }

        //Расчетный счет
        [DataMember]
        public string rcount { set; get; }

        //Корр. счет
        [DataMember]
        public string kcount { set; get; }

        //Бик
        [DataMember]
        public string bik { set; get; }

        //Населенный пункт
        [DataMember]
        public string npunkt { set; get; }

        [DataMember]
        public string dat_when { set; get; }


        public BankRequisites()
            : base()
        {
            nzp_fb = 0;
            nzp_payer = 0;           
            num_count = 0;
            bank_name = "";
            rcount = "";
            kcount = "";
            bik = "";
            npunkt = "";
        }
   
   
    }


    [DataContract]
    public class ContractRequisites : Ls
    {
        //серийный номер
        [DataMember]
        public int nzp_con { set; get; }

        //номер договора 
        [DataMember]
        public string num_dog { set; get; }

        //номер юридического лица – стороны  Б договора
        [DataMember]
        public int nzp_payer { set; get; }

        //наименование юридического лица – стороны  Б договора
        [DataMember]
        public string payer { set; get; }

        //серийный ключ таблицы fn_bank
        [DataMember]
        public int nzp_fb { set; get; }

        //дата начала действия договора
        [DataMember]
        public string dat_s { set; get; }

        //дата окончания действия договора
        [DataMember]
        public string dat_po { set; get; }

        //код поставщика - стороны Б договора
        [DataMember]
        public string kod_supp { set; get; }

        //расчетный счет - стороны Б договора
        [DataMember]
        public string rcount { set; get; }

        //вид расчетного счета - стороны Б договора
        [DataMember]
        public int nzp_osnov { set; get; }

        //вид расчетного счета - стороны Б договора
        [DataMember]
        public string osnov { set; get; }

        //банк - стороны Б договора
        [DataMember]
        public string bank_name { set; get; }

        //БИК
        [DataMember]
        public string bik { set; get; }

        //кор.счет
        [DataMember]
        public string kcount { set; get; }

        //адрес банка
        [DataMember]
        public string npunkt { set; get; }

        //примечание к реквизитам стороны Б договора
        [DataMember]
        public string comment { set; get; }

        //флаг 
        [DataMember]
        public int area_flag { set; get; }

        //nzp_supp
        [DataMember]
        public int nzp_supp { set; get; }

        //name supp
        [DataMember]
        public string name_supp { set; get; }

        public ContractRequisites()
            : base()
        {
            nzp_con = 0;
            num_dog = "";
            nzp_payer = 0;
            payer = "";
            dat_s = "";
            dat_po = "";
            kod_supp = "";
            rcount = "";
            nzp_osnov = -1;
            bank_name = "";
            bik = "";
            kcount = "";
            npunkt = "";
            comment = "";
            area_flag = -1;
        }
    }

    [DataContract]
    public class DogovorRequisites : Ls
    {
        [DataMember]
        public int nzp_fd { set; get; }

        [DataMember]
        public long nzp_payer_ar { set; get; }

        [DataMember]
        public long nzp_payer { set; get; }

        [DataMember]
        public int nzp_fb { set; get; }

        [DataMember]
        public int nzp_osnov { set; get; }

        [DataMember]
        public string osnov { set; get; }

        [DataMember]
        public string num_dog { set; get; }

        [DataMember]
        public string dat_dog { set; get; }

        [DataMember]
        public string target { set; get; }

        [DataMember]
        public string kpp { set; get; }

        [DataMember]
        public string dat_when { set; get; }

        [DataMember]
        public string rschet { set; get; }

        [DataMember]
        public string dat_s { set; get; }

        [DataMember]
        public string dat_po { set; get; }

        [DataMember]
        public string period_deistv { set; get; }

        [DataMember]
        public decimal max_sum { set; get; }

        public DogovorRequisites()
            : base()
        {
            nzp_fd = 0;
            nzp_payer_ar = 0;
            nzp_payer = 0;
            nzp_fb = 0;
            nzp_osnov = 0;
            num_dog = "";
            dat_dog = "";
            target = "";
            kpp = "";
            dat_s = dat_po = period_deistv = "";
            dat_when = "";
            max_sum = 0;
        }
    }

    [DataContract]
    public class FnSupplier : Pack_ls
    {
        [DataMember]
        public int nzp_serv { set; get; }

        [DataMember]
        public string service { set; get; }

        [DataMember]
        public long nzp_supp { set; get; }

        [DataMember]
        public string name_supp { set; get; }

        [DataMember]
        public decimal sum_prih { set; get; }

        [DataMember]
        public decimal s_user { set; get; }

        [DataMember]
        public decimal s_dolg { set; get; }

        [DataMember]
        public decimal s_forw { set; get; }

        [DataMember]
        public decimal sum_outsaldo { set; get; }

        [DataMember]
        public decimal sum_insaldo { set; get; }

        /// <summary>
        /// Распределено ранее
        /// </summary>
        [DataMember]
        public decimal sum_prih_prev { set; get; }

        /// <summary>
        /// показывать ли эталонные колонки распределения
        /// 1 - да, иначе - нет
        /// </summary>
        [DataMember]
        public int show_etalon { set; get; }

        /// <summary>
        /// исходящее сальдо c учетом оплаты
        /// </summary>
        [DataMember]
        public decimal sum_outsaldo_with_opl { set; get; }

        /// <summary>
        /// исходящее сальдо c учетом оплаты
        /// </summary>
        [DataMember]
        public decimal sum_insaldo_with_opl { set; get; }

        /// <summary>
        /// Начислено к оплате
        /// </summary>
        [DataMember]
        public decimal sum_charge { set; get; }

        /// <summary>
        /// Начислено к оплате c учетом оплаты
        /// </summary>
        [DataMember]
        public decimal sum_charge_with_opl { set; get; }

        /// <summary>
        /// Начислено за месяц без недопоставки
        /// </summary>
        [DataMember]
        public decimal rsum_tarif { set; get; }
        
        [DataMember]
        public int is_del { set; get; }

        public FnSupplier()
            : base()
        {
            nzp_serv = 0;
            service = "";
            nzp_supp = 0;
            name_supp = "";
            sum_prih = 0;
            sum_prih_prev = 0;
            s_user = 0;
            s_dolg = 0;
            s_forw = 0;
            show_etalon = 0;
            sum_outsaldo = 0;
            sum_insaldo = 0;
            sum_charge = 0;
            rsum_tarif = 0;
            is_del = -1;
            sum_outsaldo_with_opl = 0;
            sum_charge_with_opl = 0;
        }
    }

    [DataContract]
    public class FinderAddPackage : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public List<_Service> services { get; set; }
        [DataMember]
        public List<_Area> area { get; set; }
        [DataMember]
        public List<_Supplier> payers { get; set; }
        [DataMember]
        public List<Bank> banks { get; set; }
        [DataMember]
        public bool check { get; set; }
        [DataMember]
        public double summ_s { get; set; }
        [DataMember]
        public double summ_po { get; set; }
        [DataMember]
        public string date_s { get; set; }
        [DataMember]
        public string date_po { get; set; }
    }

    [DataContract]
    public class ChargeForDistribSum : Finder
    {
        [DataMember]
        public int nzp_serv { set; get; }

        [DataMember]
        public string service { set; get; }

        [DataMember]
        public int nzp_supp { set; get; }

        [DataMember]
        public string name_supp { set; get; }  

        [DataMember]
        public decimal sum { set; get; }

        [DataMember]
        public decimal distr_sum { set; get; }

        [DataMember]
        public int month_ { set; get; }

        [DataMember]
        public int year_ { set; get; }

        [DataMember]
        public int nzp_kvar { set; get; }

        [DataMember]
        public int etalon { set; get; }

        [DataMember]
        public int nzp_pack_ls { set; get; }

        [DataMember]
        public int pack_year { set; get; }

        [DataMember]
        public string dat_month { set; get; }

        public ChargeForDistribSum()
            : base()
        {
            dat_month = "";
            nzp_serv = nzp_supp = 0;
            service = name_supp = "";
            sum = 0;
            month_ = 0;
            year_ = 0;
            nzp_kvar = 0;
            etalon = 0;
            pack_year = nzp_pack_ls = 0;
            distr_sum = 0;
        }
    }

}

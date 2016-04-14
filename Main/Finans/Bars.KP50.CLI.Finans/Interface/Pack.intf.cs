using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Data;
using System.Collections;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Pack
    {
        /// <summary>
        /// Загрузка квитанции о реестре оплат
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns UploadKvitReestr(FilesImported finder);

        /// <summary>
        /// Проверка перед загрузкой файлареестра или квитанции от банка с оплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        FilesImported FastCheck(FilesImported finder, out Returns ret);

        /// <summary> Выполнить операцию над пачкой и вернуть пачку
        /// </summary>
        [OperationContract]
        Pack OperateWithPackAndGetIt(PackFinder finder, enSrvOper oper, out Returns ret);

        /// <summary> Выполнить операцию над пачкой и вернуь результат
        /// </summary>
        [OperationContract]
        Returns OperateWithPack(PackFinder finder, Pack.PackOperations oper);

        [OperationContract]
        Returns ChangeCasePack(PackFinder finder, List<Pack_ls> list);

        [OperationContract]
        Returns ChangeChoosenPlsInCase(Finder finder);

        [OperationContract]
        Returns PackLsInCaseChangeMark(Finder finder, List<Pack_ls> listChecked, List<Pack_ls> listUnchecked);

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
        /// загрузить список банков
        /// </summary>
        [OperationContract]
        List<Bank> LoadListBanks(Bank finder, out Returns ret);

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
        string LoadUniversalFormat(string body, string filename);

        [OperationContract]
        Returns SaveOperDay(Pack finder);

        [OperationContract]
        DateTime GetOperDay(out Returns ret);

        [OperationContract]
        List<Pack_log> GetPackLog(Pack_log finder, out Returns ret);

        [OperationContract]
        List<BankRequisites> GetBankRequisites(BankRequisites finder, out Returns ret);

        [OperationContract]
        List<BankRequisites> NewFdGetBankRequisites(BankRequisites finder, out Returns ret);

        [OperationContract]
        List<BankRequisites> GetRsForERCDogovor(BankRequisites finder, out Returns ret);

        [OperationContract]
        List<DogovorRequisites> GetDogovorERCList(DogovorRequisites finder, out Returns ret);

        [OperationContract]
        List<DogovorRequisites> GetDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<DogovorRequisites> GetDogovorRequisitesSupp(DogovorRequisites finder, out Returns ret);

        [OperationContract]
        List<BankRequisites> GetSourceBankList(BankRequisites finder, out Returns ret);

        [OperationContract]
        List<ContractRequisites> GetContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        bool ChangeBankRequisites(BankRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        bool ChangeDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        bool ChangeDogovorRequisitesSupp(DogovorRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        bool ChangeContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<FnSupplier> GetFnSupplier(FnSupplier finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns CancelPlat(Finder finder);

        //[OperationContract]
        //Returns CancelPlat2(Finder finder, List<Pack_ls> list);

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
        ReturnsType GenContDistribPaymentsPDF(Payments finder);

        [OperationContract]
        Returns MakeContDistribPayments(Payments finder);

        [OperationContract]
        decimal GetLsSum(Saldo finder, GetLsSumOperations operation, out Returns ret);

        [OperationContract]
        Returns UploadPackFromWeb(int nzpPack, int nzp_user);

        [OperationContract]
        Returns PutTaskDistribLs(Dictionary<int, int> listPackLs, int nzp_user);

        [OperationContract]
        List<ChargeForDistribSum> GetSumsForDistrib(ChargeForDistribSum finder, out Returns ret);

        [OperationContract]
        Returns SaveManualDistrib(List<ChargeForDistribSum> listfinder);

        [OperationContract]
        Returns DeleteManualDistrib(Pack_ls finder);

        [OperationContract]
        Returns GetPrincipForManualDistrib(List<ChargeForDistribSum> listfinder, out List<ChargeForDistribSum> res);

        [OperationContract]
        Returns CreatePackOverPayment(Pack finder);

        [OperationContract]
        Returns СreateUploading(FilterForBC finder);

        [OperationContract]
        Returns SaveCheckSend(int nzpUser, List<FilesUploadingBC> files);

        [OperationContract]
        List<FormatBC> GetFormats(int nzpUser, out Returns ret, List<int> formats);

        [OperationContract]
        FormatBC GetFormat(int nzpUser, int idFormat, out Returns ret);

        [OperationContract]
        int AddFormat(int nzpUser, string nameFormat, out Returns ret);

        [OperationContract]
        Returns SaveFormat(int nzpUser, FormatBC typ);

        [OperationContract]
        Returns DeleteFormat(int nzpUser, int idFormat);

        [OperationContract]
        List<TagBC> GetTags(int nzpUser, int indexFormat, out Returns ret);

        [OperationContract]
        TagBC GetTag(int nzpUser, int idTag, out Returns ret);

        [OperationContract]
        List<InfoPayerBankClient> GetInfoPayers(FilterForBC finder, out Returns ret);

        [OperationContract]
        List<InfoPayerBankClient> GetTransfersPayer(FilterForBC finder, out Returns ret);

        [OperationContract]
        List<InfoPayerBankClient> GetDogovorsWithTransfers(FilterForBC finder, out Returns ret);

        [OperationContract]
        int AddTag(int nzpUser, TagBC tag, out Returns ret);

        [OperationContract]
        Returns DeleteTag(int nzpUser, int idTag);

        [OperationContract]
        Returns SaveTag(int nzpUser, TagBC tag);

        [OperationContract]
        Returns UpTag(int nzpUser, int idTag);

        [OperationContract]
        Returns DownTag(int nzpUser, int idTag);

        [OperationContract]
        List<ValueTagBC> GetTagValues(int nzpUser, out Returns ret);

        [OperationContract]
        List<TypeTagBC> GetTagTypes(int nzpUser, out Returns ret);

        [OperationContract]
        Returns FormPacksSbPay(EFSReestr finder, PackFinder packfinder);

        [OperationContract]
        Returns UploadChangesServSupp(ReestrChangesServSupp finder);

        [OperationContract]
        List<ReestrChangesServSupp> GetReestrChangesServSupp(ReestrChangesServSupp finder, out Returns ret);

        [OperationContract]
        Returns CheckingReturnOnPrevDay();

        [OperationContract]
        Returns ReDistributePackLs(Finder finder);

        [OperationContract]
        Pack GetOperDaySettings(Finder packFinder, out Returns ret);

        [OperationContract]
        Returns SaveOperDaySettings(Pack finder);

        [OperationContract]
        Returns ChangeOperDay(OperDayFinder finder, out string date_oper, out string filename, out RecordMonth calcmonth);

        [OperationContract]
        List<string> GetRS(Pack_ls finder, out Returns ret);

        [OperationContract]
        List<Pack_ls> GetKodSumList(Pack_ls finder, out Returns ret);
        /// <summary>
        /// Получает список имен файлов, используется в findpack.aspx для поиска имен файлов с автодополнением
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<Pack> GetFilesName(Pack finder, out Returns ret);
        /// <summary>
        /// Получает информацию о договоре ЖКУ 
        /// </summary>
        [OperationContract]
        SupplierInfo GetSupplierInfo(SupplierInfo finder, out Returns ret);
        /// <summary>
        /// Обновляет область дейтствия договора ЖКУ 
        /// </summary>
        [OperationContract]
        Returns UpdateSupplierScope(SupplierInfo finder);
        /// <summary>
        /// Получает дочерние области действия договора ЕРЦ 
        /// </summary>
        [OperationContract]
        List<int> GetDogovorERCChildsScope(SupplierInfo finder, out Returns ret);
        /// <summary>
        /// Получает список договоров ЕРЦ по заданному агенту, принципалу
        /// </summary>
        [OperationContract]
        List<DogovorRequisites> GetListDogERCByAgentAndPrincip(DogovorRequisites finder, out Returns ret);

        [OperationContract]
        Returns CheckPackLsToDeleting(Pack_ls finder);

        [OperationContract]
        Returns SelectOverPayments(OverPaymentsParams finder);

        /// <summary>
        /// Статус процесса управления переплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<OverpaymentStatusFinder> GetOverpaymentManStatus(Finder finder, out Returns ret);

        /// <summary>
        /// Ставим статус процесса  управления переплатами
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns SetStatusOverpaymentManProc(OverpaymentStatusFinder finder);

        [OperationContract]
        Returns CheckChoosenOverPyment(OverpaymentStatusFinder finder);

        [OperationContract]
        List<SettingsPackPrms> OperateSettingsPack(SettingsPackPrms finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<FilesUploadingOnWebBC> GetFilesUploading(int nzpUser, int idReestr, int skip, int rows, out Returns ret);

        [OperationContract]
        List<UploadingOnWebBC> GetListUploading(int nzpUser, int skip, int rows, out Returns ret);

        [OperationContract]
        UploadingOnWebBC GetUploading(int nzpUser, int idReestr, out Returns ret);
    }

    public interface IPackRemoteObject : I_Pack, IDisposable { }

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
    public class OperDayFinder : Finder
    {
        /// <summary> режим работы
        /// </summary>
        public enum Mode
        {
            /// <summary> Перейти на следующий операционный день
            /// </summary>
            CloseOperDay = 0,

            /// <summary>
            /// Вернуться назад на предыдущий опер день
            /// </summary>
            GoBackOperDay = 1,

            /// <summary>
            /// Перейти на заданный операционный день
            /// </summary>
            GoDefinedOperDay = 2
        }

        [DataMember]
        public int mode { get; set; }
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
            NotClosed = 51

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
            GetNextNumPackForOverPay = 6,
            Edit = 7,
            ChangeCasePack = 8,
            ShowButtonCase = 9
        }

        [DataMember]
        public int nzp_pack { get; set; }

        [DataMember]
        public int nzp_bank { get; set; }

        //[DataMember]
        //public string bank { get; set; }

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
        public string dat_uchet_po { get; set; }

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

        //nzp_payer - оплата от контрагента (чтобы не было путаницы)
        [DataMember]
        public int nzp_payer_contragent { get; set; }

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

        [DataMember]
        public bool isCurrentMonth { get; set; }

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

        [DataMember]
        public int oper_day_change_mode { get; set; }


        [DataMember]
        public long nzp_supp { get; set; }


        [DataMember]
        public string oper_day_change_time { get; set; }

        public Pack()
            : base()
        {
            nzp_pack = 0;
            nzp_bank = 0;
            nzp_supp = 0;
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
            dat_uchet_po = "";
            oper_day_change_mode = 0;
            oper_day_change_time = "";

            isCurrentMonth = false;
        }
    }

    [DataContract]
    public class PackForCase
    {
        [DataMember]
        public int nzp_pack { get; set; }

        [DataMember]
        public int par_pack { get; set; }

        [DataMember]
        public int nzp_bank { get; set; }
        
        [DataMember]
        public string num_pack { get; set; }
        
        [DataMember]
        public string file_name { get; set; }

        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public string nzp_payer { get; set; }

        [DataMember]
        public string nzp_supp { get; set; }
        
        [DataMember]
        public int new_nzp_pack { get; set; }

        public PackForCase()
            : base()
        {
            nzp_pack = 0;
            nzp_bank = 0;
            nzp_supp = "";
            num_pack = "";
            year_ = 0;
            file_name = "";
            nzp_payer = "";
            nzp_supp = "";
            new_nzp_pack = 0;
            par_pack = 0;
        }
    }

    [DataContract]
    public class PackFinder : Pack
    {
        //[DataMember]
        //public string dat_pack_po { get; set; }

        //[DataMember]
        //public string dat_uchet_po { get; set; }

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

        [DataMember]
        public string pls_dat_uchet_po { get; set; }

        [DataMember]
        public string pls_dat_uchet { get; set; }

        [DataMember]
        public int putincase { get; set; }
        //код квитанции
        [DataMember]
        public int kod_sum { get; set; }

        public PackFinder()
            : base()
        {
            dat_pack_po = "";
            putincase = 0;
            pls_dat_uchet = "";
            pls_dat_uchet_po = "";
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
            kod_sum = 0;

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
            SavePackLs,
            AutoSavePackLs //автоматизированный ввод
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
            CancelDistribution,
            BlockPackLs,
            UnBlockPackLs
        }

        public enum FinYearsToShow
        {
            CurrentYear, 
            PreviousYears,
            AllYears
        }

        /// <summary>
        /// Перечислитель ошибок
        /// </summary>
        public enum err
        {
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
        public string kod_sum_name { get; set; }

        [DataMember]
        public int paysource { get; set; } //0 - источник платежа 1 - по умолчанию

        [DataMember]
        public int id_bill { get; set; } //код квитанции  номер квитанции в месяце - 0

        [DataMember]
        public string dat_vvod { get; set; }

        [DataMember]
        public string distr_month { get; set; }

        [DataMember]
        public string dat_vvod_po { get; set; }

        //[DataMember]
        //public string dat_uchet_po { get; set; }

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

        /// <summary>
        /// 1 = удаление из gil_sums
        /// </summary>
        [DataMember]
        public int manual_mode { get; set; }

        [DataMember]
        public string errors { get; set; }

        [DataMember]
        public int page { get; set; }// из какой страницы вызывается

        [DataMember]
        public int status_for_opl { get; set; }

        [DataMember]
        public int type_pay { get; set; }

        [DataMember]
        public int nzp { get; set; }

        [DataMember]
        public DateTime month_from { get; set; }

        [DataMember]
        public DateTime month_to { get; set; }

        [DataMember]
        public string plspayer { get; set; }

        [DataMember]
        public string plssupplier { get; set; }

        [DataMember]
        public string date_distr { get; set; }

        [DataMember]
        public FinYearsToShow finYearsToShow { get; set; }

        [DataMember]
        public string old_num_ls { get; set; }

        [DataMember]
        public string calc_month { get; set; }

        public Pack_ls()
            : base()
        {
            date_distr = "";
            nzp = 0;
            alg = "";
            g_sum_ls_po = 0;
            dat_uchet_po = "";
            dat_vvod_po = "";
            nzp_err = 0;
            nzp_pack_ls = 0;
            prefix_ls = 0;
            status_for_opl = 0;
            g_sum_ls = 0;
            sum_ls = 0;
            sum_peni = 0;
            dat_month = "";
            kod_sum = 0;
            kod_sum_name = "";
            paysource = 0;
            id_bill = 0;
            dat_vvod = "";
            info_num = 0;
            unl = 0;
            calc_month = "";
            incase = 0;
            count_izm = 0;
            manual_mode = 0;
            count_nedop = 0;
            nzp_kvar_new = "";
            pref_new = "";
            num_ls_new = "";
            pkod_new = "";
            is_manual = false;
            errors = "";
            page = 0;
            type_pay = 0;
            month_from = DateTime.MinValue;
            month_to = DateTime.MinValue;
            distr_month = "";
            plspayer = "";
            plssupplier = "";
            finYearsToShow = FinYearsToShow.AllYears;
            old_num_ls = "";
        }

        public override string ToString()
        {
            string s = base.ToString() +
                       " nzp_pack_ls = " + nzp_pack_ls +
                       " prefix_ls = " + prefix_ls +
                       " g_sum_ls = " + g_sum_ls +
                       " g_sum_ls_po = " + g_sum_ls_po +
                       " sum_ls = " + sum_ls +
                       " sum_peni = " + sum_peni +
                       " dat_month = " + dat_month +
                       " kod_sum = " + kod_sum +
                       " paysource = " + paysource +
                       " id_bill = " + id_bill +
                       " dat_vvod = " + dat_vvod +
                       " dat_vvod_po = " + dat_vvod_po +
                       " dat_uchet_po = " + dat_uchet_po +
                       " info_num = " + info_num +
                       " inbasket = " + inbasket +
                       " incase = " + incase +
                       " unl = " + unl +
                       " alg = " + alg +
                       " count_nedop = " + count_nedop +
                       " count_izm = " + count_izm +
                       " nzp_err = " + nzp_err +
                       " nzp_kvar_new = " + nzp_kvar_new +
                       " pref_new  = " + pref_new +
                       " num_ls_new  = " + num_ls_new +
                       " pkod_new  = " + pkod_new +
                       " is_manual  = " + is_manual +
                       " manual_mode  = " + manual_mode +
                       " errors  = " + errors +
                       " page  = " + page;
            return s;
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

        [DataMember]
        public int nzp_supp { get; set; }

        public GilSum()
            : base()
        {

            nzp_sums = 0;
            nzp_pack_ls = 0;
            sum_oplat = "";
            day_nedo = "";
            ordering = 0;
            nzp_serv = 0;
            nzp_supp = 0;
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


        [DataMember]
        public int nzp_counter { get; set; }
    
        [DataMember]
        public string dat_uchet { get; set; }

        public PuVals()
            : base()
        {
            dat_uchet = "";
            nzp_spv = 0;
            nzp_pack_ls = 0;
            num_cnt = "";
            val_cnt = 0;
            ordering = 0;
            nzp_serv = 0;
            nzp_counter = 0;
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
    public class Pack_errtype : Pack_ls
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
        public int nzp_fb { set; get; }
        [DataMember]
        public int nzp_fd { set; get; }

        //ключ для fn_dogovor_bank_lnk
        [DataMember]
        public int id { set; get; }

        [DataMember]
        public long nzp_payer { set; get; }

        [DataMember]
        public long nzp_payer_bank { set; get; }

        [DataMember]
        public long nzp_payer_agent { set; get; }

        [DataMember]
        public long nzp_payer_princip { set; get; }

        //номер
        [DataMember]
        public int num_count { set; get; }
        //банк
        [DataMember]
        public int nzp_bank { set; get; }

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

        [DataMember]
        public string rasch_full { set; get; }

        [DataMember]
        public string poluch { set; get; }

        [DataMember]
        public Decimal max_sum { set; get; }

        [DataMember]
        public Decimal min_sum { set; get; }

        [DataMember]
        public int priznak_perechisl { set; get; }

        [DataMember]
        public string naznplat { set; get; }

        [DataMember]
        public int is_default { set; get; }

        private string rcountfull;
        public BankRequisites()
            : base()
        {
            nzp_fb = 0;
            nzp_bank = 0;
            poluch = "";
            nzp_payer = 0;
            nzp_payer_bank = 0;
            nzp_payer_agent = 0;
            nzp_payer_princip = 0;
            num_count = 0;
            bank_name = "";
            rcount = "";
            id = 0;
            kcount = "";
            bik = "";
            npunkt = "";
            rasch_full = "";
            priznak_perechisl = 1;
            max_sum = 0;
            min_sum = 0;
            naznplat = "";
            rcountfull = "";
            is_default = 0;
        }
        public string rcount_full
        {
            get
            {
                if (!String.IsNullOrEmpty(bank_name))
                {
                    rcountfull ="Банк: "+ bank_name.Trim() + ", ";
                }
                if (!String.IsNullOrEmpty(rcount))
                {
                    rcountfull += "р/с: " + rcount.Trim() + ", ";
                }
                if (!String.IsNullOrEmpty(poluch))
                {
                    rcountfull += "получатель: " + poluch.Trim();
                }
                return rcountfull;
            }
            set { rcountfull = value; }

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
        public long nzp_payer_princip { set; get; }

        [DataMember]
        public long nzp_payer_agent { set; get; }

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
        public string dat_dog_s { set; get; }
        [DataMember]
        public string dat_dog_po { set; get; }

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

        [DataMember]
        public decimal min_sum { set; get; }

        /// <summary>
        /// признак перечисления: 1 - одной суммой; 2 - в разрезе услуг 
        /// </summary>
        [DataMember]
        public int priznak_perechisl { set; get; }

        [DataMember]
        public string vrazrserv { set; get; }

        [DataMember]
        public string naznplat { set; get; }

        //список банков
        [DataMember]
        public ArrayList bank_list { set; get; }
        [DataMember]
        public int nzp_supp { set; get; }
        [DataMember]
        public string name_supp { set; get; }

        [DataMember]
        public string agent { set; get; }

        [DataMember]
        public string principal { set; get; }
        [DataMember]
        public int nzp_scope { set; get; }

        private string name_dog = "";

     
        public DogovorRequisites()
            : base()
        {
            nzp_scope = 0;
            nzp_fd = 0;
            nzp_payer_ar = 0;
            nzp_payer = 0;
            nzp_payer_princip = 0;
            nzp_payer_agent = 0;
            nzp_fb = 0;
            nzp_osnov = 0;
            num_dog = "";
            dat_dog = "";
            target = "";
            agent = "";
            principal = "";
            kpp = "";
            dat_s = dat_po = period_deistv = "";
            dat_when = "";
            max_sum = 0;
            min_sum = 0;
            priznak_perechisl = 0;
            vrazrserv = "";
            naznplat = "";
            name_dog = "";
            bank_list = new ArrayList();
            dat_dog_s = "";
            dat_dog_po = "";
        }

        [DataMember]
        public string name_dogovor
        {
            get
            {
                List<string> list = new List<string>();
                if (!String.IsNullOrEmpty(principal.Trim()))
                {
                    list.Add(principal.Trim());
                }
                if (!String.IsNullOrEmpty(agent.Trim()))
                {
                    list.Add("/"+agent.Trim());
                }
                if (!String.IsNullOrEmpty(num_dog.Trim()))
                {
                    list.Add(" №"+num_dog.Trim());
                }
                if (!String.IsNullOrEmpty(dat_dog.Trim()))
                {
                    if (dat_dog.Trim().Length > 10)
                    {
                        dat_dog = dat_dog.Trim().Remove(10);
                    }
                    list.Add(" от " + dat_dog.Trim());
                }
                if (list.Count > 0)
                {
                    name_dog = String.Join("", list);  
                }
                return name_dog;
            }
            set
            {
                name_dog = value;
            }
        }
    }

    [DataContract]
    public class FnSupplier : Pack_ls
    {
        [DataMember]
        public int nzp_serv { set; get; }

        [DataMember]
        public string service { set; get; }

        //[DataMember]
        //public long nzp_supp { set; get; }

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
        public decimal sum_money { set; get; }

        [DataMember]
        public int is_del { set; get; }

        [DataMember]
        public int onlyCharge { get; set; }

        [DataMember]
        public string dateCharge { get; set; }

        public FnSupplier()
            : base()
        {
            sum_money = 0;
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
            onlyCharge = 0;
            dateCharge = "";
        }
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
        public int ordering { set; get; }

        [DataMember]
        public string dat_month { set; get; }

        [DataMember]
        public int num { set; get; }

        [DataMember]
        public int nzp_payer_princip { get; set; }

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
            ordering = 0;
            num = 0;
            etalon = 0;
            pack_year = nzp_pack_ls = 0;
            distr_sum = 0;
            nzp_payer_princip = 0;
        }
    }

    [DataContract]
    public class ReestrChangesServSupp : Finder
    {
        [DataMember]
        public int nzp_reestr { set; get; }

        [DataMember]
        public string dat_month { set; get; }

        [DataMember]
        public string file_name { set; get; }

        [DataMember]
        public string file_link { set; get; }

        [DataMember]
        public string uploaded { set; get; }

        [DataMember]
        public int status { set; get; }

        [DataMember]
        public int nzp_exc { set; get; }

        [DataMember]
        public string status_name { set; get; }

        [DataMember]
        public string comment { set; get; }

        public ReestrChangesServSupp()
            : base()
        {
            nzp_reestr = status = nzp_exc = 0;
            dat_month = file_name = uploaded = comment = status_name = file_link = "";
        }
    }

    public class UpdChangesServSupp : Finder
    {
        public enum ChangedStatuses
        {
            /// <summary> новая запись
            /// </summary>
            New = 1,

            /// <summary> запись удалена
            /// </summary>
            Deleted = 2,

            /// <summary> запись не изменилась
            /// </summary>
            NotChanges = 3,

            /// <summary> Запись изменилась
            /// </summary>
            Changes = 4
        }

        [DataMember]
        public int nzp_changes { set; get; }

        [DataMember]
        public int nzp_reestr { set; get; }

        [DataMember]
        public int nzp_serv { set; get; }

        [DataMember]
        public string service { set; get; }

        [DataMember]
        public int nzp_supp { set; get; }

        [DataMember]
        public string name_supp { set; get; }

        [DataMember]
        public string inn { set; get; }

        [DataMember]
        public string kpp { set; get; }

        [DataMember]
        public string rchet { set; get; }

        [DataMember]
        public string bik { set; get; }

        [DataMember]
        public int status { set; get; }

        [DataMember]
        public int month_ { set; get; }

        public UpdChangesServSupp()
            : base()
        {
            nzp_changes = nzp_reestr = nzp_serv = nzp_supp = status = month_ = 0;
            inn = kpp = bik = rchet = service = name_supp = "";
        }
    }

    public class SupplierInfo : Finder
    {
        [DataMember]
        public int nzp_supp { set; get; }

        [DataMember]
        public string name_supp { set; get; }
        [DataMember]
        public int nzp_payer_princip { set; get; }

        [DataMember]
        public string principal_fd { set; get; }

        [DataMember]
        public string principal { set; get; }
        [DataMember]
        public int nzp_payer_agent { set; get; }

        [DataMember]
        public string agent { set; get; }
        [DataMember]
        public string agent_fd { set; get; }

        [DataMember]
        public int nzp_payer_podr { set; get; }

        [DataMember]
        public string podr { set; get; }

        [DataMember]
        public int nzp_fd { set; get; }

        [DataMember]
        public string num_dog { set; get; }
        [DataMember]
        public int nzp_payer { set; get; }
        [DataMember]
        public string payer { set; get; }

        [DataMember]
        public string name_dog { set; get; }

        [DataMember]
        public int nzp_fb { set; get; }
        [DataMember]
        public string bank_name { set; get; }

        [DataMember]
        public string rcount { set; get; }

        [DataMember]
        public int parent_nzp_scope { set; get; }

        [DataMember]
        public int nzp_payer_supp { set; get; }
        [DataMember]
        public string supplier { set; get; }

        [DataMember]
        public int nzp_scope { set; get; }
        [DataMember]
        public List<int> list_nzp_wp { set; get; }
        [DataMember]
        public string dat_dog { set; get; }
        private string  name_parent_dog { set; get; }
        [DataMember]
        public string kpp { set; get; }
        [DataMember]
        public string bik { set; get; }
        [DataMember]
        public string ks { set; get; }

        [DataMember]
        public int fn_dogovor_bank_lnk_id { set; get; }
        [DataMember]
        public string payer_bank { set; get; }

        private string rcountfull;
        public SupplierInfo()
        {
            fn_dogovor_bank_lnk_id = 0;
            kpp = "";
            bik = "";
            ks = "";
            dat_dog = "";
            agent_fd = "";
            principal_fd = "";
            nzp_supp = 0;
            name_supp = "";
            nzp_payer_princip = 0;
            principal = "";
            nzp_payer_agent = 0;
            agent = "";
            nzp_payer_podr = 0;
            podr = "";
            nzp_fd = 0;
            num_dog = "";
            nzp_payer = 0;
            payer = "";
            name_dog = "";
            nzp_fb = 0;
            rcount = "";
            supplier = "";
            name_parent_dog = "";
            list_nzp_wp= new List<int>();
            payer_bank = "";
            rcountfull = "";
        }
        public string name_dogovor
        {
            get
            {
                List<string> list = new List<string>();
                if (!String.IsNullOrEmpty(principal.Trim()))
                {
                    list.Add(principal_fd.Trim());
                }
                if (!String.IsNullOrEmpty(agent.Trim()))
                {
                    list.Add("/" + agent_fd.Trim());
                }
                if (!String.IsNullOrEmpty(num_dog.Trim()))
                {
                    list.Add(" №" + num_dog.Trim());
                }
                if (!String.IsNullOrEmpty(dat_dog.Trim()))
                {
                    if (dat_dog.Trim().Length > 10)
                    {
                        dat_dog = dat_dog.Trim().Remove(10);
                    }
                    list.Add(" от " + dat_dog.Trim());
                }
                if (list.Count > 0)
                {
                    name_parent_dog = String.Join("", list);
                }
                return name_parent_dog;
            }
            set
            {
                name_parent_dog = value;
            }
        }
        /// <summary>
        /// Формирует строку расчетного счета
        /// </summary>
        /// <param name="bank">Банк получатель</param>
        /// <param name="rcount">расчетный счет</param>
        /// <param name="payer">получатель</param>
        /// <returns></returns>
        //public string RcountFull(string payer_bank_prm = "", string rcount_prm = "", string payer_prm = "")
        //{
        //    //Если входные параметры не заданы, то расчетный счет будет формироваться из полей этого класса
        //    if (String.IsNullOrEmpty(payer_bank_prm) && String.IsNullOrEmpty(rcount_prm) && String.IsNullOrEmpty(payer_prm))
        //    {
        //        return getRcountFull(payer_bank, rcount, payer);
        //    }
        //    return getRcountFull(payer_bank_prm, rcount_prm, payer_prm);
        //}

        //private string getRcountFull(string payer_bank_prm, string rcount_prm, string payer_prm)
        //{
        //    string rcountfull = "";
        //    if (!String.IsNullOrEmpty(payer_bank_prm))
        //    {
        //        rcountfull = payer_bank_prm.Trim() + " ";
        //    }
        //    if (!String.IsNullOrEmpty(rcount_prm))
        //    {
        //        rcountfull += "р/с " + rcount_prm.Trim() + " ";
        //    }
        //    if (!String.IsNullOrEmpty(payer_prm))
        //    {
        //        rcountfull += "получатель: " + payer_prm.Trim();
        //    }
        //    return rcountfull;
        //}
        public string rcount_full
            {
            get
        {
                if (!String.IsNullOrEmpty(payer_bank))
                {
                    rcountfull ="Банк: "+ payer_bank.Trim() + ", ";
                }
                if (!String.IsNullOrEmpty(rcount))
                {
                    rcountfull += "р/с:" + rcount.Trim() + ", ";
                }
                if (!String.IsNullOrEmpty(payer))
                {
                    rcountfull += "получатель: " + payer.Trim();
                }
            return rcountfull;
        }
            set { rcountfull = value; }
        }
    }

    [DataContract]
    public class OverPaymentsParams
    {
        [DataMember]
        public List<string> prefs { get; set; }

        [DataMember]
        public bool select_only_isdel { get; set; }

        [DataMember]
        public bool uchet_peni { get; set; }

        [DataMember]
        public int nzp_user { get; set; }

        [DataMember]
        public DateTime dat_s { get; set; }

        [DataMember]
        public DateTime dat_po { get; set; }

        public OverPaymentsParams()
            : base()
        {
            nzp_user = 0;
            prefs = new List<string>();
            select_only_isdel = true;
            uchet_peni = true;
            dat_s = dat_po = DateTime.MinValue;
        }
    }

   
    [DataContract]
    public class DistrOverPaymentsParams
    {
        /// <summary>
        /// Переплату распределять только в пределах долга
        /// </summary>
        [DataMember]
        public bool distr_within_dolg { get; set; }

        /// <summary>
        /// Снимать оплату только в пределах поступления денежных средств
        /// </summary>
        [DataMember]
        public bool remove_within_distr { get; set; }

        [DataMember]
        public int nzp_user { get; set; }

        /// <summary>
        /// 1 - в порядке уменьшения переплаты, 2 - увеличения
        /// </summary>
        [DataMember]
        public int ordering { get; set; }

        public DistrOverPaymentsParams()
            : base()
        {
            distr_within_dolg = remove_within_distr  = false;
            ordering = 1;
            nzp_user = 0;
        }
    }

    /// <summary>
    /// Класс для информации о добавленных пачках
    /// </summary>
    public class AddedPacksInfo
    {
        // id строки, прогресс которой будут обновляться
        public int Nzp { get; set; }
        // Событие для простановки прогресса загружаемых пачек
        public event Func<int,decimal,Returns> PackLoadProgress;
        // Количество частей загрузки
        public int CountPartOfLoad { get; set; }
        private decimal packProgress;
        private decimal oplatyProgress;
        private decimal totalPersent;
        /// <summary>
        /// Количество вставленных строк
        /// </summary>
        public int InsertedCountRows { get; set; }

        /// <summary>
        /// Номер пачки
        /// </summary>
        public int InsertedNzpPack { get; set; }

        /// <summary>
        /// Список предупреждений в процессе загрузки пачки
        /// </summary>
        public List<Msgs> WarningMessages = new List<Msgs>();

        /// <summary>
        /// Список ошибок в процессе загрузки пачек
        /// </summary>
        public List<Msgs> ErrorMessages = new List<Msgs>();

        // Класс сообщений
        public class Msgs
        {
            private string pref;

            public string Point
            {
                get { return Points.GetPoint(pref).point; }
                set { pref = value; }

            }

            public string Message;
        }

        public AddedPacksInfo()
        {
            CountPartOfLoad = 2;
        }

        public void AddWarnMsg(string message, string pref = "")
        {
            Msgs msg= new Msgs();
            msg.Message = message;
            msg.Point = pref;
            WarningMessages.Add(msg);
        }
        public void AddErrorMsg(string message, string pref = "")
        {
            Msgs msg = new Msgs();
            msg.Message = message;
            msg.Point = pref;
            ErrorMessages.Add(msg);
        }

        public void InsertErrMsg(DataTable dtError)
        {
            foreach (Msgs msg in ErrorMessages)
            {
                dtError.Rows.Add((int)DownloadMessageTypes.Error, msg.Message, msg.Point);
            }
        }

        public void InsertWarnMsg(DataTable dtError)
        {
            foreach (Msgs msg in WarningMessages)
            {
                dtError.Rows.Add((int)DownloadMessageTypes.Warning, msg.Message, msg.Point);
            }
        }


        public void InitPackProgress(int countPack)
        {
            packProgress = 1 / Convert.ToDecimal(CountPartOfLoad) / Convert.ToDecimal(countPack);
        }

        public void InitOplatyProgress(int countOplaty)
        {
            oplatyProgress = packProgress / countOplaty;
        }

        public void OnSetProgress()
        {
            totalPersent += oplatyProgress;
            if (totalPersent > 1) totalPersent = 1;
            Func<int, decimal, Returns> temp = PackLoadProgress;
            if (temp != null) temp(Nzp, totalPersent);
        }

       
    }



    [DataContract]
    public class OverpaymentStatusFinder
    {
        [DataMember]
        public int id { get; set; }

        [DataMember]
        public int nzp_status { get; set; }

        [DataMember]
        public string status { get; set; }

        [DataMember]
        public int nzp_user { get; set; }

        [DataMember]
        public string user { get; set; }

        [DataMember]
        public int nzp_fon_selection { get; set; }

        [DataMember]
        public int nzp_fon_distrib { get; set; }

        [DataMember]
        public DateTime dat_when { get; set; }

        [DataMember]
        public int is_actual { get; set; }

        public OverpaymentStatusFinder()
            : base()
        {
            id = nzp_status = nzp_user = nzp_fon_distrib = nzp_fon_selection = 0;
            user = status = "";
        }
        public enum Statuses
        {
            overpSelection = 1,
            overpDistrib = 2
        }

    }
    [DataContract]
    public class SettingsPackPrms : Finder
    {
        [DataMember]
        public int nzp_bank { get; set; }

        [DataMember]
        public int nzp_type_pack { get; set; }

        [DataMember]
        public string type_pack_name { get; set; }
        [DataMember]
        public List<PackTypes> type_pack_list { get; set; }

        public string types_pack_names
        {
            get { return String.Join(", ", type_pack_list.Select(t=>t.type_name)); }
        }

        public SettingsPackPrms()
        {
            nzp_type_pack = 0;
            type_pack_name = "";
            nzp_bank = 0;
            type_pack_list= new List<PackTypes>();
        }

    }

    /// <summary>Фильтр для режима "Банк-клиент"</summary>
    [DataContract]
    public class FilterForBC
    {
        /// <summary>Идентификатор пользователя</summary>
        [DataMember]
        public int IdUser { get; set; }

        /// <summary>Кол-во допустимых элементов</summary>
        [DataMember]
        public int Limit { get; set; }

        /// <summary>Позиция начала элемента</summary>
        [DataMember]
        public int OffSet { get; set; }

        /// <summary>Услуги</summary>
        [DataMember]
        public List<_Service> Services { get; set; }

        /// <summary>Агенты</summary>
        [DataMember]
        public List<Payer> Agents { get; set; }

        /// <summary>Принципалы</summary>
        [DataMember]
        public List<Payer> Principals { get; set; }

        /// <summary>Поставщики</summary>
        [DataMember]
        public List<Payer> Suppliers { get; set; }

        /// <summary>Получатели</summary>
        [DataMember]
        public List<_Supplier> Payees { get; set; }

        /// <summary>Банки</summary>
        [DataMember]
        public List<Bank> Banks { get; set; }

        /// <summary>Договоры</summary>
        [DataMember]
        public List<DogovorRequisites> DogovorRequisiteses { get; set; }

        public FilterForBC() {
            Agents = new List<Payer>();
            Principals = new List<Payer>();
            Suppliers = new List<Payer>();
            Services = new List<_Service>();
            Payees = new List<_Supplier>();
            Banks = new List<Bank>();
            DogovorRequisiteses = new List<DogovorRequisites>();
        }

    }

    /// <summary>Форматы выгрузки в системы банк-клиент</summary>
    [DataContract]
    public class FormatBC
    {
        /// <summary>Идентификатор формата</summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>Наименование формата</summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>Используется формат</summary>
        [DataMember]
        public bool IsActive { get; set; }

        /// <summary>Теги формата</summary>
        [DataMember]
        public List<TagBC> Tags { get; set; }
    }

    /// <summary>Теги форматов в системы банк-клиент</summary>
    [DataContract]
    public class TagBC
    {
        /// <summary>Идентификатор тега</summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>Формат</summary>
        [DataMember]
        public FormatBC Format { get; set; }

        /// <summary>Тип тега</summary>
        [DataMember]
        public TypeTagBC TypeTag { get; set; }

        /// <summary>Порядковый номер тега</summary>
        [DataMember]
        public int Num { get; set; }

        /// <summary>Наименование тега</summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>Описание тега</summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>Значение тега</summary>
        [DataMember]
        public ValueTagBC Value { get; set; }

        /// <summary>Обязателный для заполнения</summary>
        [DataMember]
        public bool IsRequared { get; set; }

        /// <summary>Видимый тег</summary>
        [DataMember]
        public bool IsShowEmpty { get; set; }

        public TagBC() {
            TypeTag = new TypeTagBC { Id = 1, Name = string.Empty };
            Name = string.Empty;
            Description = string.Empty;
            IsRequared = false;
            IsShowEmpty = false;
            Value = new ValueTagBC { Id = 0, Description = string.Empty, Name = string.Empty };
        }
    }

    /// <summary>Тип тега в системы банк-клиент</summary>
    [DataContract]
    public class TypeTagBC
    {
        /// <summary>Идентификатор тега</summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>Наименование типа тега</summary>
        [DataMember]
        public string Name { get; set; }
    }

    /// <summary>Значение тега в системы банк-клиент</summary>
    [DataContract]
    public class ValueTagBC
    {
        /// <summary>Идентификатор тега</summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>Наименование значения тега</summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>Описание значения тега</summary>
        [DataMember]
        public string Description { get; set; }
    }

    /// <summary>Файлы выгрузки (Банк-клиент)</summary>
    [DataContract]
    public class FilesUploadingOnWebBC : FilesUploadingBC
    {
        /// <summary>Порядковый номер</summary>
        [DataMember]
        public int Order { get; set; }

        /// <summary>Сумма перечислений</summary>
        [DataMember]
        public Decimal SumTransfer { get; set; }
    }

    /// <summary>Файлы выгрузки (Банк-клиент)</summary>
    [DataContract]
    public class FilesUploadingBC
    {
        /// <summary>Идентификатор</summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>Выгрузка</summary>
        [DataMember]
        public UploadingBC Uploading { get; set; }

        /// <summary>Формат банка</summary>
        [DataMember]
        public FormatBC Format { get; set; }

        /// <summary>Банк</summary>
        [DataMember]
        public Bank Bank { get; set; }

        /// <summary>Наименование файла</summary>
        [DataMember]
        public string FileName { get; set; }

        /// <summary>Идентификатор файла в excel_utility</summary>
        [DataMember]
        public int NzpExc { get; set; }

        /// <summary>Состояние - Не обработано ли банком</summary>
        [DataMember]
        public bool IsTreaster { get; set; }
    }

    ///// <summary>Формат Банк - клиент в namespace Money</summary>
    //[DataContract]
    //public class FormatBCForMoney
    //{
    //    /// <summary>Идентификатор формата</summary>
    //    [DataMember]
    //    public int Id { get; set; }

    //    /// <summary>Наименование формата</summary>
    //    [DataMember]
    //    public string Name { get; set; }

    //    /// <summary>Инициализация формата</summary>
    //    /// <param name="name">Наименование формата</param>
    //    public FormatBCForMoney(string name) {
    //        Id = 0;
    //        Name = name;
    //    }
    //}

    ///// <summary>Банк в namespace Money</summary>
    //[DataContract]
    //public class BankForMoney
    //{
    //    /// <summary>Идентификатор банка</summary>
    //    [DataMember]
    //    public int Id { get; set; }

    //    /// <summary>Наименование банка</summary>
    //    [DataMember]
    //    public string Name { get; set; }

    //    /// <summary>Инициализация банка</summary>
    //    /// <param name="name">Наименование банка</param>
    //    public BankForMoney(string name) {
    //        Id = 0;
    //        Name = name;
    //    }
    //}

    /// <summary>Выгрузка (Банк-клиент)</summary>
    [DataContract]
    public class UploadingBC
    {
        /// <summary>Идентификатор выгрузки</summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>Дата выгрузки</summary>
        [DataMember]
        public DateTime DateReestr { get; set; }

        /// <summary>Номер выгрузки</summary>
        [DataMember]
        public int NumReestr { get; set; }

        /// <summary>Пользователь</summary>
        [DataMember]
        public User User { get; set; }
    }

    /// <summary>Выгрузка - на web (Банк-клиент)</summary>
    [DataContract]
    public class UploadingOnWebBC : UploadingBC
    {
        /// <summary>Порядковый номер</summary>
        [DataMember]
        public int Order { get; set; }

        /// <summary>Итоговая сумма перечислений</summary>
        [DataMember]
        public Decimal TotalSumTransfer { get; set; }
    }

    /// <summary>Тип тега</summary>
    public enum TypeTagBcEnum
    {
        /// <summary>Заголовок</summary>
        Header = 1,
        /// <summary>Тело</summary>
        Body = 2,
        /// <summary>Подвал</summary>
        Footer = 3
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Data;
using STCLINE.KP50.Global;
using System.Collections;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Debitor
    {

        [OperationContract]
        Deal LoadDealInfo(DealFinder finder, out Returns ret);

        [OperationContract]
        List<Agreement> GetAgreements(DealFinder finder, out Returns ret);

        [OperationContract]
        List<Deal> GetDealStatuses(DealFinder finder, out Returns ret);

        [OperationContract]
        Returns SaveDealChanges(Deal finder);

        [OperationContract]
        Returns SaveDebtChanges(Deal finder);

        [OperationContract]
        List<Deal> GetArgStatus(Deal finder, out Returns ret);

        [OperationContract]
        List<AgreementDetails> GetArgDetail(Agreement finder, out Returns ret);

        [OperationContract]
        List<deal_states_history> GetDealHistory(Deal finder, out Returns ret);

        [OperationContract]
        Dictionary<string, Dictionary<int, string>> GetDebitorLists(DealFinder finder, out Returns ret);

        [OperationContract]
        List<Debt> GetDebitors(DebtFinder finder, out Returns ret);

        [OperationContract]
        List<Deal> GetDeals(DealFinder finder, out Returns ret);

        [OperationContract]
        void AgreementOpers(List<AgreementDetails> finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        lawsuit_Data GetLavsuit(int nzp_lawsuit, int nzp_deal, out Returns ret);

        [OperationContract]
        void SetLavsuit(lawsuit_Data Data, out Returns ret);

        [OperationContract]
        void DeleteLavsuit(int nzp_lawsuit, out Returns ret);

        [OperationContract]
        List<DealCharge> GetDealCharges(Deal finder, int yy, int mm, out Returns ret);

        [OperationContract]
        List<SettingsRequisites> GetSettingArea(SettingsRequisites finder, out Returns ret);

        [OperationContract]
        List<Supplier> GetSupplier(Deal finder, out Returns ret);

        [OperationContract]
        List<Service> GetService(Deal finder, int nzp_supp, out Returns ret);

        [OperationContract]
        Returns GetDDLstDealOperations(out lawsuit_Files lstPreCourt, out lawsuit_Files lstCourt);

        [OperationContract]
        List<lawsuit_Data> GetLawSuits(Deal finder, out Returns ret);

        [OperationContract]
        void SaveSetting(List<SettingsRequisites> finder, out Returns ret);

        [OperationContract]
        int AddGroupOperation(Deal finder, int nzp_oper, ReportType type, out Returns ret);

        [OperationContract]
        Returns AddPerekidka(Deal finder, decimal money);

        [OperationContract]
        Returns CloseDeal(Deal finder);

        [OperationContract]
        Deal CreateDeal(Deal finder, out Returns ret);

        [OperationContract]
        decimal GetLawsuitPrice(int nzp_deal, out Returns ret);

        [OperationContract]
        bool ExistDeal(int nzp_kvar, out Returns ret);

        [OperationContract]
        Returns GetAllAgrementsReport(DateTime? dat_s, DateTime? dat_po, int user, int area);
    }


    [DataContract(Namespace = Constants.Linespace, Name = "Deal")]
    public class Deal : Ls
    {
        /// <summary>Ключ</summary>
        [DataMember(Name = "nzp_deal", Order = 10)]
        public int nzp_deal { get; set; }

        ///// <summary>ссылка на ЛС</summary>
        //[DataMember(Name = "nzp_kvar", Order = 20)]
        //public int nzp_kvar { get; set; }

        /// <summary>кол-во детей до 18 лет</summary>
        [DataMember(Name = "children_count", Order = 30)]
        public int children_count { get; set; }

        ///// <summary>Кто добавил</summary>
        //[DataMember(Name = "nzp_user", Order = 40)]
        //public int nzp_user { get; set; }

        /// <summary>Ответственный</summary> 
        [DataMember(Name = "responsible_name", Order = 50)]
        public string responsible_name { get; set; }

        /// <summary>дата фиксации долга</summary> 
        [DataMember(Name = "debt_fix_date", Order = 60)]
        public DateTime debt_fix_date { get; set; }

        /// <summary>сумма задолженности</summary>
        [DataMember(Name = "debt_money", Order = 70)]
        public decimal debt_money { get; set; }

        /// <summary>мдентификатор статуса дела</summary>
        [DataMember(Name = "nzp_deal_status", Order = 80)]
        public int nzp_deal_status { get; set; }

        /// <summary>Название статуса дела</summary>
        [DataMember(Name = "status", Order = 80)]
        public string status { get; set; }

        /// <summary>Примечание</summary>
        [DataMember(Name = "comment", Order = 90)]
        public string comment { get; set; }

        ///// <summary>код УК</summary>
        //[DataMember(Name = "nzp_area", Order = 100)]
        //public int nzp_area { get; set; }

        ///// <summary>ФИО</summary>
        //[DataMember(Name = "fio", Order = 110)]
        //public string fio { get; set; }

        ///// <summary>Адрес</summary>
        //[DataMember(Name = "adr", Order = 120)]
        //public string adr { get; set; }

        /// <summary>Приватизированно 1-да, 0-нет</summary>
        [DataMember(Name = "is_priv", Order = 130)]
        public string is_priv { get; set; }

        /// <summary>Дата рождения</summary>
        [DataMember(Name = "dat_rog", Order = 140)]
        public DateTime dat_rog { get; set; }

        /// <summary>Вид документа</summary>
        [DataMember]
        public int dok { get; set; }

        /// <summary>Серия документа</summary>
        [DataMember]
        public string serij { get; set; }

        /// <summary>Номер документа</summary>
        [DataMember]
        public string nomer { get; set; }

        /// <summary>Место выдачи</summary>
        [DataMember]
        public string vid_mes { get; set; }

        /// <summary>Дата выдачи</summary>
        [DataMember]
        public DateTime vid_dat { get; set; }

        [DataMember]
        public int nzp_group { set; get; }

        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }



        public Deal()
        {
            nzp_deal = 0;
            nzp_kvar = 0;
            children_count = 0;
            nzp_user = 0;
            responsible_name = "";
            debt_fix_date = DateTime.MinValue;
            debt_money = 0;
            nzp_deal_status = 0;
            comment = "";
            nzp_serv = 0;
            nzp_supp = 0;

        }
        //получение имени статуса по коду
        public string getNameDealStatus(int nzp_status)
        {
            switch (nzp_status)
            {
                case (int)EnumDealStatuses.Close: return "Закрыто";
                case (int)EnumDealStatuses.Registered: return "Зарегистрировано";
                case (int)EnumDealStatuses.Reminder: return "Напоминание выдано";
                case (int)EnumDealStatuses.Notice: return "Уведомление выдано";
                case (int)EnumDealStatuses.Warning: return "Предупреждение выдано";
                case (int)EnumDealStatuses.AgreementSigned: return "Соглашение подписано";
                case (int)EnumDealStatuses.AgreementViolated: return "Соглашение нарушено";
                case (int)EnumDealStatuses.OrderFormed: return "Судебный приказ сформирован";
                case (int)EnumDealStatuses.LawsuitSubmitted: return "Иск подан в суд";
                default: return "Статус отсутствует";
            }
        }
    }

    [DataContract(Namespace = Constants.Linespace, Name = "Debitor")]
    public class Debt : Ls
    {
        /// <summary>
        /// дело
        /// </summary>
        [DataMember]
        public string deal { get; set; }

        /// <summary>Ключ</summary>
        [DataMember(Name = "nzp_debt", Order = 10)]
        public int nzp_debt { get; set; }

        ///// <summary>адрес</summary>
        //[DataMember(Name = "adr", Order = 110)]
        //public string adr { get; set; }

        ///// <summary>ФИО</summary>
        //[DataMember(Name = "fio", Order = 110)]
        //public string fio { get; set; }

        ///// <summary>телефон</summary>
        //[DataMember(Name = "phone", Order = 110)]
        //public string phone { get; set; }

        /// <summary>сумма долга</summary>
        [DataMember(Name = "debt_money", Order = 110)]
        public decimal debt_money { get; set; }

        /// <summary>количество детей</summary>
        [DataMember]
        public int children_count { get; set; }

        /// <summary>Приватизированно 1-да, 0-нет</summary>
        [DataMember(Name = "is_priv", Order = 130)]
        public string is_priv { get; set; }

        /// <summary>Дата рождения</summary>
        [DataMember(Name = "dat_rog", Order = 140)]
        public DateTime dat_rog { get; set; }


        [DataMember]
        public int nzp_group { set; get; }

        public Debt()
        {
            nzp_debt = 0;
            nzp_kvar = 0;
            nzp_user = 0;
            children_count = 0;
        }
    }

    [DataContract]
    public class Agreement : Deal
    {
        /// <summary>Ключ</summary>
        [DataMember(Name = "nzp_agr", Order = 10)]
        public int nzp_agr { get; set; }

        /// <summary>Номер соглашения</summary>
        [DataMember(Name = "number", Order = 20)]
        public string number { get; set; }

        /// <summary>Детали соглашения</summary>
        [DataMember(Name = "arg_detail", Order = 25)]
        public string arg_detail
        {
            get { return "Соглашение №" + number + " от " + agr_dat.ToString("dd.MM.yyyy") + "г. на " + agr_money + " руб."; }
            set { }
        }

        /// <summary>Дата соглашения</summary>
        [DataMember(Name = "agr_dat", Order = 30)]
        public DateTime agr_dat { get; set; }

        /// <summary>Сумма рассрочки</summary>
        [DataMember(Name = "agr_money", Order = 40)]
        public decimal agr_money { get; set; }

        /// <summary>Кол-во месяцев рассрочки</summary> 
        [DataMember(Name = "agr_month_count", Order = 50)]
        public int agr_month_count { get; set; }

        ///// <summary>ключ-ссылка на дело</summary> 
        //[DataMember(Name = "nzp_deal", Order = 60)]
        //public int nzp_deal { get; set; }

        /// <summary>статус соглашения</summary>
        [DataMember(Name = "nzp_agr_status", Order = 70)]
        public int nzp_agr_status { get; set; }

        public Agreement()
        {
            nzp_agr = 0;
            number = "";
            agr_dat = DateTime.MinValue;
            agr_money = 0;
            agr_month_count = 0;
            nzp_deal = 0;
            nzp_agr_status = 0;
        }

    }

    [DataContract(Namespace = Constants.Linespace, Name = "Setting")]
    public class SettingsRequisites : Ls
    {
        /// <summary>Ключ</summary>
        [DataMember(Name = "nzp_setting", Order = 10)]
        public int nzp_setting { get; set; }

        ///// <summary>Ключ УК</summary>
        //[DataMember(Name = "nzp_area", Order = 20)]
        //public int nzp_area { get; set; }

        /// <summary>поставщик</summary>
        [DataMember(Name = "nzp_supp", Order = 20)]
        public int nzp_supp { get; set; }

        ///// <summary>Управляющая компания</summary>
        //[DataMember(Name = "area", Order = 30)]
        //public string area { get; set; }

        /// <summary>Фамилия директора</summary>
        [DataMember(Name = "fio_dir", Order = 40)]
        public string fio_dir { get; set; }

        /// <summary>Номер соглашения</summary>
        [DataMember(Name = "Фамилия дир. СВЗ за ЖКХ", Order = 50)]
        public string fio_svz { get; set; }

        ///// <summary>Город</summary>
        //[DataMember(Name = "town", Order = 60)]
        //public string town { get; set; }

        /// <summary>Улица</summary>
        [DataMember(Name = "street", Order = 70)]
        public string street { get; set; }

        /// <summary>Дом</summary>
        [DataMember(Name = "dom", Order = 80)]
        public string dom { get; set; }

        /// <summary>Квартира</summary>
        [DataMember(Name = "kvnum", Order = 90)]
        public string kvnum { get; set; }

        ///// <summary>Телефон</summary>
        //[DataMember(Name = "phone", Order = 100)]
        //public string phone { get; set; }

        public SettingsRequisites()
        {
            nzp_setting = 0;
            nzp_area = 0;
            area = "";
            fio_dir = "";
            fio_svz = "";
            town = "";
            street = "";
            dom = "";
            kvnum = "";
            phone = "";
        }
    }

    [DataContract]
    public class AgreementDetails : Agreement
    {
        /// <summary>Ключ</summary>
        [DataMember(Name = "nzp_agr_det", Order = 10)]
        public int nzp_agr_det { get; set; }

        ///// <summary>Номер соглашения</summary>
        //[DataMember(Name = "nzp_agr", Order = 20)]
        //public int nzp_agr { get; set; }

        /// <summary>Месяц рассрочки</summary>
        [DataMember(Name = "dat_month", Order = 30)]
        public DateTime dat_month { get; set; }

        /// <summary>Сумма входящего сальдо</summary>
        [DataMember(Name = "sum_insaldo", Order = 40)]
        public decimal sum_insaldo { get; set; }

        /// <summary>Сумма оплаты</summary> 
        [DataMember(Name = "sum_money", Order = 50)]
        public decimal sum_money { get; set; }

        /// <summary>Сумма исходящего сальдо</summary> 
        [DataMember(Name = "sum_outsaldo", Order = 60)]
        public decimal sum_outsaldo { get; set; }

        public AgreementDetails()
        {
            nzp_agr_det = 0;
            nzp_agr = 0;
            dat_month = DateTime.MinValue;
            sum_insaldo = 0;
            sum_money = 0;
            sum_outsaldo = 0;
        }

    }

    /// <summary> /// Статусы дел /// </summary>
    public enum EnumDealStatuses
    {
        /// <summary>
        /// Тип не задан
        /// </summary>
        None = 0,

        /// <summary>
        /// "Закрыто"
        /// </summary>
        Close = 1,

        /// <summary>
        /// Списан долг
        /// </summary>
        Debt = 2,

        /// <summary>
        /// "Зарегистрировано"
        /// </summary>
        Registered = 3,

        /// <summary>
        /// "Напоминание выдано"
        /// </summary>
        Reminder = 4,

        /// <summary>
        /// "Уведомление выдано"
        /// </summary>
        Notice = 5,

        /// <summary>
        /// "Предупреждение выдано"
        /// </summary>
        Warning = 6,

        /// <summary>
        /// ""Соглашение подписано""
        /// </summary>
        AgreementSigned = 7,

        /// <summary>
        /// "Соглашение нарушено"
        /// </summary>
        AgreementViolated = 8,

        /// <summary>
        /// "Судебный приказ сформирован"
        /// </summary>
        OrderFormed = 9,

        /// <summary>
        /// "Иск подан в суд"
        /// </summary>
        LawsuitSubmitted = 10

    }

    /// <summary> /// Отношения для шаблона поиска(>=,<=,=) /// </summary>
    public enum EnumMarks
    {
        /// <summary>
        /// отсутствие значени
        /// </summary>
        None = 0,

        /// <summary>
        /// =
        /// </summary>
        Equal = 1,

        /// <summary>
        /// >=
        /// </summary>
        MoreEqual = 2,

        /// <summary>
        /// <=
        /// </summary>
        LessEqual = 3

    }

    /// <summary> 
    /// Типы операций 
    ///  </summary>
    public enum EnumOpers
    {
        /// <summary>
        /// Оплата по соглашению
        /// </summary>
        PayOnAgreement = 1,

        /// <summary>
        /// Неоплата по соглашению
        /// </summary>
        NoPayOnAgreement = 2,

        /// <summary>
        /// Закрытие соглашения
        /// </summary>
        CloseAgreement = 3,

        /// <summary>
        /// Выдача напоминания
        /// </summary>
        GiveNotice = 4,

        /// <summary>
        /// Выдача уведомления
        /// </summary>
        GiveNotification = 5,

        /// <summary>
        /// Выдача предупреждения
        /// </summary>
        GiveWarning = 6,

        /// <summary>
        /// Создание соглашения
        /// </summary>
        CreateAgreement = 8,

        /// <summary>
        /// Выдача судебного приказа
        /// </summary>
        GiveLawOrder = 9,

        /// <summary>
        /// Выдача иска
        /// </summary>
        GiveLawSuit = 10,

        /// <summary>
        /// Создание дела
        /// </summary>
        CreateDebt = 11,

        /// <summary>
        /// Закрытие дела
        /// </summary>
        CloseDeal = 12,

        /// <summary>
        /// Списание долга
        /// </summary>
        DebtDown = 13
    }

    [DataContract]
    public class DealFinder : Ls
    {
        /// <summary>
        /// типы операции
        /// </summary>
        public enum Operations
        {
            /// <summary>
            /// поиск
            /// </summary>
            Find = 0,

            /// <summary>
            /// получить данные
            /// </summary>
            Get = 1
        }

        /// <summary>
        /// тип операции
        /// </summary>
        [DataMember]
        public Operations operation { set; get; }

        ///// <summary>
        ///// объек для сортировки
        ///// </summary>
        //[DataMember]
        //public List<_OrderingField> orderings { get; set; }

        /// <summary>
        /// дело
        /// </summary>
        [DataMember]
        public string deal { get; set; }

        /// <summary>
        /// идентификатор дела
        /// </summary>
        [DataMember]
        public int nzp_deal { get; set; }

        /// <summary>
        /// идентификатор соглашения
        /// </summary>
        [DataMember]
        public int nzp_agr { get; set; }

        /// <summary>
        /// Код статуса дела
        /// </summary>
        [DataMember]
        public int nzp_deal_stat { get; set; }

        /// <summary>
        /// Тип жилья
        /// </summary>
        [DataMember]
        public int type_gil { get; set; }

        /// <summary>
        /// Название статуса дела
        /// </summary>
        [DataMember]
        public string status { get; set; }

        /// <summary>
        /// сумма задолженности
        /// </summary>
        [DataMember]
        public decimal debt_money { get; set; }

        /// <summary>
        /// имя ответственного
        /// </summary> 
        [DataMember]
        public string responsible_name { get; set; }

        /// <summary>
        /// сумма долга
        /// </summary> 
        [DataMember]
        public decimal sum_debt { get; set; }

        ///// <summary>
        ///// условие поиска по сумме
        ///// </summary> 
        //public EnumMarks mark { get; set; }

        /// <summary>
        /// дата фиксации долга
        /// </summary> 
        [DataMember]
        public DateTime debt_fix_date { get; set; }

        /// <summary>
        /// дата последнего платежа с
        /// </summary> 
        [DataMember]
        public DateTime last_payment_from { get; set; }

        /// <summary>
        /// дата последнего платежа по
        /// </summary> 
        [DataMember]
        public DateTime last_payment_to { get; set; }

        /// <summary>
        /// дети до 18 лет
        /// </summary> 
        [DataMember]
        public bool children { get; set; }

        public DealFinder()
        {
            nzp_deal = 0;
            nzp_agr = 0;
            nzp_deal_stat = 0;
            type_gil = 0;
            responsible_name = "";
            sum_debt = 0;
            mark = EnumMarks.None.GetHashCode();
            debt_fix_date = DateTime.MinValue;
            last_payment_from = DateTime.MinValue;
            last_payment_to = DateTime.MinValue;
        }
    }

    [DataContract]
    public class DebtFinder : Ls
    {
        /// <summary>
        /// дело
        /// </summary>
        [DataMember]
        public string deal { get; set; }

        /// <summary>
        /// типы операции
        /// </summary>
        public enum Operations
        {
            /// <summary>
            /// поиск
            /// </summary>
            Find = 0,

            /// <summary>
            /// получить данные
            /// </summary>
            Get = 1
        }

        /// <summary>
        /// тип операции
        /// </summary>
        [DataMember]
        public Operations operation { set; get; }

        /// сумма долга с
        /// </summary> 
        [DataMember]
        public decimal sum_debt_from { get; set; }

        /// <summary>
        /// сумма долга по
        /// </summary>
        [DataMember]
        public decimal sum_debt_to { get; set; }

        /// <summary>
        /// есть дети до 18 лет
        /// </summary>
        [DataMember]
        public bool have_child { get; set; }

        /// <summary>
        /// кол-во месяцев долга
        /// </summary>
        [DataMember]
        public int month_count { get; set; }

        /// <summary>
        /// кол-во детей до 18 лет
        /// </summary>
        [DataMember]
        public int children_count { get; set; }

        /// <summary>
        /// Ключ
        /// </summary>
        [DataMember]
        public int nzp_debt { get; set; }

        /// <summary>
        /// Приватизированно 1-да, 0-нет
        /// </summary>
        [DataMember]
        public string is_priv { get; set; }

        /// <summary>
        /// сумма долга
        /// </summary>
        [DataMember]
        public decimal debt_money { get; set; }

        public DebtFinder()
        {
            sum_debt_from = 0;
            sum_debt_to = 0;
            have_child = false;
            month_count = 0;
        }
    }

    /// <summary>
    /// Статусы групповых операций
    /// </summary>
    public enum s_opers_statuses
    {
        /// <summary>
        /// Готова для обработки
        /// </summary>
        Ready = 1,

        /// <summary>
        /// Поставлена в очередь
        /// </summary>
        InProcess = 2,

        /// <summary>
        /// Успешно выполнена
        /// </summary>
        Success = 3,

        /// <summary>
        /// Ошибка выполнения
        /// </summary>
        Error = 4
    }

    /// <summary>
    /// Статусы соглашений
    /// </summary>
    public enum s_agr_statuses
    {
        /// <summary>
        /// Исполняется
        /// </summary>
        Сelebrates = 1,

        /// <summary>
        /// Исполнено
        /// </summary>
        Fulfilled = 2,

        /// <summary>
        /// Нарушено
        /// </summary>
        Violated = 3,

        /// <summary>
        /// Дело закрыто
        /// </summary>
        DealClosed = 4
    }

    /// <summary>
    /// Класс истории статусов дел
    /// </summary>
    [DataContract]
    public class deal_states_history : Finder
    {
        /// <summary>
        /// Ключ
        /// </summary>
        [DataMember]
        public int nzp_deal_state { set; get; }

        /// <summary>
        /// Ключ дела
        /// </summary>
        [DataMember]
        public int nzp_deal { set; get; }

        /// <summary>
        /// Дата смены статуса дела
        /// </summary>
        [DataMember]
        public DateTime date_state { set; get; }

        /// <summary>
        /// Задолжность по делу на момент смены статуса
        /// </summary>
        [DataMember]
        public decimal debt_money { set; get; }

        /// <summary>
        /// Начисления
        /// </summary>
        [DataMember]
        public decimal plus { set; get; }

        /// <summary>
        /// Оплаты
        /// </summary>
        [DataMember]
        public decimal minus { set; get; }

        /// <summary>
        /// Код операции
        /// </summary>
        [DataMember]
        public int nzp_oper { set; get; }

        /// <summary>
        /// Операция
        /// </summary>
        [DataMember]
        public string oper { set; get; }

    }

    public enum EnumLawsuitStatuses
    {
        None = 0,
        Adopted = 1,
        Satisfied = 2,
        Rejected = 3
    }

    public enum EnumLawsuitSector
    {
        None = 0,
        Sector1 = 1,
        Sector2 = 2,
        Sector3 = 3
    }

    /// <summary>
    /// Статусы дел
    /// </summary>
    public enum s_deal_statuses
    {
        /// <summary>
        /// Закрыто
        /// </summary>
        Closed = 1,

        /// <summary>
        /// ЗАрегистрировано
        /// </summary>
        Registred = 3,

        /// <summary>
        /// Напоминание выдано
        /// </summary>
        ReminderIssued = 4,

        /// <summary>
        /// Уведомление выдано
        /// </summary>
        NoticeIssued = 5,

        /// <summary>
        /// Предупреждение выдано
        /// </summary>
        WarningIssued = 6,

        /// <summary>
        /// Соглашение подписано
        /// </summary>
        AgreementSigned = 7,

        /// <summary>
        /// Соглашение нарушено
        /// </summary>
        AgreementViolated = 8,

        /// <summary>
        /// Приказ сформирован
        /// </summary>
        WritFormed = 9,

        /// <summary>
        /// Иск подан в суд
        /// </summary>
        LawsuitFiled = 10
    }

    [DataContract]
    public class lawsuit_File
    {
        [DataMember]
        public string strName;

        [DataMember]
        public string strPath;

        public lawsuit_File(string Name, string Path)
        {
            this.strName = Name;
            this.strPath = Path;
        }
    }

    [DataContract]
    public class lawsuit_Files
    {
        [DataMember]
        public List<lawsuit_File> lstFiles;

        public lawsuit_Files()
        {
            lstFiles = new List<lawsuit_File>();
        }

        public void Add(string Name, string Path)
        {
            this.lstFiles.Add(new lawsuit_File(Name, Path));
        }
    }

    [DataContract]
    public class lawsuit_Sector
    {
        [DataMember]
        public int intSectorId;

        [DataMember]
        public string strSectorName;

        public lawsuit_Sector(int SectorId, string SectorName)
        {
            this.intSectorId = SectorId;
            this.strSectorName = SectorName;
        }
    }

    [DataContract]
    public class lawsuit_Sectors
    {
        [DataMember]
        public List<lawsuit_Sector> lstSectors;

        public lawsuit_Sectors()
        {
            lstSectors = new List<lawsuit_Sector>();
        }
        public void Add(int SectorId, string SectorName)
        {
            lstSectors.Add(new lawsuit_Sector(SectorId, SectorName));
        }
    }

    [DataContract]
    public class lawsuit_Status
    {
        [DataMember]
        public string strName;

        [DataMember]
        public int intId;

        public lawsuit_Status(int Id, string Name)
        {
            this.intId = Id;
            this.strName = Name;
        }
    }

    [DataContract]
    public class lawsuit_Statuses
    {
        [DataMember]
        public List<lawsuit_Status> lstStatuses;

        public lawsuit_Statuses()
        {
            this.lstStatuses = new List<lawsuit_Status>();
        }
        public void Add(int Id, string Name)
        {
            this.lstStatuses.Add(new lawsuit_Status(Id, Name));
        }
    }

    [DataContract]
    public class lawsuit_Data : Deal
    {
        [DataMember]
        public lawsuit_Files Files { get; set; }

        [DataMember]
        public lawsuit_Sectors Sectors { get; set; }

        [DataMember]
        public lawsuit_Statuses Statuses { get; set; }

        [DataMember]
        public int nzp_lawsuit { get; set; }

        [DataMember]
        public int number { get; set; }

        [DataMember]
        public int nzp_sector { get; set; }

        [DataMember]
        public decimal lawsuit_price { get; set; }

        [DataMember]
        public DateTime lawsuit_date { get; set; }

        [DataMember]
        public string presenter { get; set; }

        [DataMember]
        public int nzp_lawsuit_status { get; set; }

        [DataMember]
        public string lawsuit_status { get; set; }

        [DataMember]
        public decimal tax { get; set; }


        public lawsuit_Data()
        {
            nzp_lawsuit = 0;
            this.Files = new lawsuit_Files();
            this.Sectors = new lawsuit_Sectors();
            this.Statuses = new lawsuit_Statuses();
        }
    }

    [DataContract]
    public class DealCharge
    {
        /// <summary>
        /// ключ
        /// </summary> 
        [DataMember]
        public int nzp_charge { get; set; }

        /// <summary>
        /// Имя услуги
        /// </summary>
        [DataMember]
        public string service_name { get; set; }

        /// <summary>
        /// Имя поставщика
        /// </summary>
        [DataMember]
        public string name_supp { get; set; }

        /// <summary>
        /// сумма вх. остатка
        /// </summary>
        [DataMember]
        public decimal sum_insaldo { get; set; }

        /// <summary>
        /// сумма исх. остатка
        /// </summary>
        [DataMember]
        public decimal sum_outsaldo { get; set; }

        /// <summary>
        /// сумма начислений
        /// </summary>
        [DataMember]
        public decimal sum_nach { get; set; }

        /// <summary>
        /// сумма к оплате
        /// </summary>
        [DataMember]
        public decimal sum_charge { get; set; }

        public DealCharge()
        {
            nzp_charge = 0;
            service_name = "";
            name_supp = "";
            sum_insaldo = 0;
            sum_outsaldo = 0;
            sum_nach = 0;
            sum_charge = 0;
        }
    }
}
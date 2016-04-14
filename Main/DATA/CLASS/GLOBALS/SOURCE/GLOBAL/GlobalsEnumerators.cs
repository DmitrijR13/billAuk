using System;
using System.Text;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;
using Globals.SOURCE.INTF;

namespace STCLINE.KP50.Global
{
    //----------------------------------------------------------------------
    public enum enSrvOper
    //----------------------------------------------------------------------
    {
        SrvGet,
        SrvFind,
        NewFdSrvFind,
        SrvFindPercentDom,
        SrvGetPercentDomLog,
        SrvLoad,
        SrvAdd,
        SrvEdit,
        SrvChangePrioritet,
        SrvFindUserVals,

        SrvWebArea,
        SrvWebGeu,
        SrvWebSupp,
        SrvWebServ,
        SrvWebPoint,
        SrvWebPrm,

        SrvGetBillCharge,
        SrvGetNewBillCharge,
        SrvFindCalcSz,
        SrvGetCalcSz,

        SrvFindVal,
        SrvLoadCntTypeUchet,
        SrvGetMaxDatUchet,

        SrvFindLastCntVal,
        SrvGetLastCntVal,
        SrvGetOdpuRashod,

        SrvGetLsGroupCounter,
        SrvGetLsDomNotGroupCnt,

        SrvAddLsForGroupCnt,
        SrvDelLsFromGroupCnt,

        SrvFindLsServicePeriods,
        NewFdSrvFindLsServicePeriods,

        SrvFindChargeStatistics, // для страницы "Статистика о начислениях"
        SrvGetChargeStatistics,

        SrvFindChargeStatisticsSupp, // для страницы "Статистика о начислениях", договора
        SrvGetChargeStatisticsSupp,

        srvFindPrmTarif, // для страницы справочник тарифов
        srvFindPrmTarifCalculation, //калькуляция тарифа на услугу Содержание жилья
        srvFindPrmCalculation, //заголовки калькуляций тарифа на услугу Содержание жилья
        srvFindPrmCalculationFormuls, //получить список формул

        srvSave,
        srvSaveMain,
        srvClose,
        srvDelete,
        srvCancelDistribution,  // отмена распределения пачки
        srvDistribute,          // распределить пачку
        srvSaveCountersCurrVals, // сохранить текущие показания ПУ
        srvChangeCase,//изменить признак в портфеле или нет
        srvShowInCase,//проверка возможности поместить в портфель
        srvBasketDistribute,//распределить в корзине
        srvBasketRepair,//исправить
        srvCheckPkod,//проверить наличия pkod в БД
        srvDeleteListPackLS, //удалить список оплат
        srvReplaceToCurFinYear, //перенести оплаты в теккущий фин. год

        srvFinanceLoad,
        srvFinanceFind,
        srvKassaFind,
        srvFinanceSave,
        srvGetCase,
        srvFindCase,
        srvGetBasket,
        srvReallocatePackInBasket,

        srvAddPrmCalculation,
        srvUpdateTarifCalculation,
        srvAddPrmTarifCalculation,
        sqrDelPrmTarifCalculation,
        sqrDelPrmCalculation,

        srvAddAgreement, //Добавляет соглашение к делу о долге
        srvDelAgreement, //Удаляет соглашение в деле о долге

        Bank,
        Principal,
        Agent,
        Supplier,
        Payer,
        PayerReferencedFromBank,
        BankForSaldoPoPerechisl,

        FindAvailableServices,
        FindAvailableServNewDoc,

        AddBankRequisites,   //Добавление подрядчику банковских реквизитов
        NewFdAddBankRequisites,
        DelBankRequisites,   //Удаление у подрядчика банковских реквизитов
        DelBankRequisitesNewFd,
        UpdateBankRequisites, //Обновление у подрядчика банковских реквизитов
        NewFdUpdateBankRequisites,
        AddBankRequisitesContr,
        UpdateBankRequisitesContr,

        GetDogovorList,        //Получить список договоров
        GetOsnovList,          //Получить список оснований для договора
        AddDogovorRequisites,  //Добавить подрядчику договор
        DelDogovorRequisites,  //Удалить договор
        UpdateDogovorRequisites,//Обновить договор

        AddERCDogovorRequisites,  //Добавить подрядчику договор
        DelERCDogovorRequisites,  //Удалить договор
        UpdateERCDogovorRequisites,//Обновить договор

        GetContractList,        //Получить список контрактов
        AddContractRequisites,  //Добавить контракт
        DelContractRequisites,  //Удалить контракт
        UpdateContractRequisites,//Обновить контракт

        GetSupp,                   //Получить список поставщиков по ЛС
        GetAreaLS,                 //Получить список УК по ЛС
        GetBanks,                  //Получить список банковских реквизитов
        GetPlannedWorks,           //Получить список проводимых работ по данному адресу
        GetWorksType,              //Получить список типов работ
        AddPlannedWork,            //Добавить новую плановую работу,
        GetPlannedWork,            //получить данные по плановой работе
        GetPlannedWorkKvar,        //получить данные по плановым работам квартиры
        UpdatePlannedWork,         //обновить плановую работу

        GetClaimCatalog,           //получить справочник претензий
        GetDestName,               //получить список имен претензий

        GetServiceCatalog,         //получить справочник служб, организаций
        UpdateServiceCatelog,      //обновить справочник служб, организаций

        GetPlannedWorksSupp,       //Получить отчет списка плановых работ - сведения по отключениям услуг по поставщикам
        GetPlannedWorksActs,       //Получить отчет списка плановых работ - сведения по отключениям услуг
        GetPlannedWorksNone,       //Получить отчет списка плановых работ - акты по отключениям услуг
        GetNedopList,              //Получить отчет по недопоставкам

        sprav_updateClaims,        //обновить справочник претензий

        GetInfoFromService,        //Получить отчет информация, полученная ОДДС
        GetAppInfoFromService,     //Получить отчет приложение к информации, полученной ОДДС
        GetJoborderPeriodOutstand, //Получить отчет списка невыполненных нарядов-заказов к концу периода
        GetCountOrderReadres,      //Получить отчет списка переадресаций заявок, принятых ОДДС
        GetMessageList,            //Получить отчет список сообщений, зарегестрированных ОДДС
        GetMessageQuestList        //Получить отчет список сообщений, зарегестрированных ОДДС(опрос)
    }
    //----------------------------------------------------------------------
    public enum enFldType
    //----------------------------------------------------------------------
    {
        t_int,
        t_date,
        t_datetime,
        t_string,
        t_decimal
    }
    //----------------------------------------------------------------------
    public enum enDopFindType
    //----------------------------------------------------------------------
    {
        dft_CntKvar,
        dft_CntDom
    }
    //----------------------------------------------------------------------
    public enum enIntvType
    //----------------------------------------------------------------------
    {
        intv_Hour,
        intv_Day,
        intv_Month
    }
    public enum enCriteria
    //----------------------------------------------------------------------
    {
        equal,
        not_equal,
        greater,
        greater_or_equal,
        less,
        less_or_equal,
        missing
    }

    //----------------------------------------------------------------------
    public enum enMustCalcType
    //----------------------------------------------------------------------
    {
        None,
        mcalc_Serv, //сразу по услуге
        mcalc_Prm1, //через prm_frm,l_foss,tarif
        mcalc_Prm2, //через kvar,prm_frm,l_foss,tarif
        mcalc_Gil,  //pere_gilec
        Counter,
        DomCounter,
        GroupCounter,
        Nedop,
        Prm17       // параметр прибора учета
    }
    //----------------------------------------------------------------------
    public enum enDataBaseType
    //----------------------------------------------------------------------
    {
        charge,
        fin,
        data,
        kernel
    }

    //----------------------------------------------------------------------
    public enum ProcessTypes
    {
        None = 0x00,
        CalcSaldoUK = 0x01,
        CalcNach = 0x02,
        Bill = 0x03,
        PayDoc = 0x05
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
    /// <summary>
    /// Типы проводок 
    /// </summary>

    public enum s_prov_types
    {
        /// <summary> Отсутствует значение </summary>
        None = 0,
        /// <summary> Начисления </summary>
        Charges = 1,
        /// <summary> Оплаты на счет РЦ </summary>
        Payment = 2,
        /// <summary> Оплаты от поставщиков </summary>
        PaymentFromSupp = 3,
        /// <summary> Перекидки оплат </summary>
        Perekidki = 4,
        /// <summary> Недопоставки </summary>
        Nedop = 5,
        /// <summary> Перерасчет </summary>
        Reval = 6,
        /// <summary> Входящее сальдо </summary>
        InSaldo = 7,
        /// <summary> Текущие задолженности до 01.01.2016 (с учетом оплат) </summary>
        OldCalculatedDebt = 8,
        /// <summary> Перекидки/корректировки </summary>
        RealCharge = 9,
        /// <summary> Задолженности образованные после 01.01.2016 и свыше 90 дней </summary>
        CalculatedDebt = 10 
    }

    /// <summary>
    /// причины отмена начисления пени
    /// </summary>
    public enum SPeniOff
    {
        /// <summary>
        /// Нет отмены начислений - default
        /// </summary>
        No = 1,
        /// <summary>
        /// Отмена начисления пени за период
        /// </summary>
        ParametrTurnOffPeniOnPeriod = 2,
        /// <summary>
        /// Отмена начисления пени при вх.сальдо=0 или меньше
        /// </summary>
        ParametrTurnOffPeniWitnZeroInSaldo = 3
    }

    public enum TablesForPeniCalc
    {
        PeniProvodki = 1,
        PeniDebt = 2,
        PeniCalc = 3,
        PeniOff = 4,
        PeniProvodkiRefs = 5,
        PeniDebtRefs = 6
    }

    /// <summary>
    /// Отображение тарифов в разрезах
    /// </summary>
    public enum TypeTarif
    {
        /// <summary>
        /// ЛС
        /// </summary>
        Ls = 1,
        /// <summary>
        /// Домов
        /// </summary>
        House = 2,
        /// <summary>
        /// Договоров ЖКУ
        /// </summary>
        Supplier = 3,
        /// <summary>
        /// Банков данных
        /// </summary>
        DataBase = 4
    }

    /// <summary>
    /// Типы действий для пени
    /// </summary>
    public enum peni_actions_type
    {
        /// <summary> отсутствует </summary>
        None = 0,
        /// <summary> Запись проводок по закрытому опердню</summary>
        InsertProvCloseDay = 1,
        /// <summary> Запись проводок по закрытому месяцу </summary>
        InsertProvCloseMonth = 2,
        /// <summary> Архивация проводок </summary>
        InsertArch = 3,
        /// <summary> Запись задолженностей и пени </summary>
        InsertCalcDebtAndPeni = 4,
        /// <summary> Запись проводок для первого запуска расчета пени в банке данных </summary>
        PrepareFirstCalcPeni = 5,
        /// <summary> Переформирование проводок для списка ЛС</summary>
        RePrepareProvs = 6
    }
    /// <summary>
    /// Типы задолженностей для пени
    /// </summary>
    public enum s_peni_type_debt
    {
        /// <summary> Отсутствует </summary>
        None = 0,
        /// <summary> Начисление задолженности </summary>
        IncDebt = 1,
        /// <summary> Снятие задолженности </summary>
        DelDebt = 2

    }
    /// <summary>
    /// типы простых загрузок
    /// </summary>
    public enum SimpleLoadTypeFile
    {
        None = 0,
        /// <summary>
        /// Загрузка от сторонних поставщиков
        /// </summary>
        SuppCharges = 1,
        /// <summary>
        /// Загрузка оплат из кассы
        /// </summary>
        LoadOplFromKassa = 2,
        /// <summary>
        /// Загрузка платежей
        /// </summary>
        LoadPayments = 3,
        /// <summary>
        /// Импортирование параметров
        /// </summary>
        ImportParam = 4,
        /// <summary>
        /// загрузка показаний счетчиков от РСО
        /// </summary>
        LoadCountersRSO = 5
    }

    /// <summary>
    /// Типы периодов для счетчиков
    /// </summary>
    public enum TypeBoundsCounters
    {
        /// <summary>
        /// Отсутствует описание
        /// </summary>
        None = 0,
        /// <summary>
        /// Период поломки
        /// </summary>
        Breaking = 1,
        /// <summary>
        /// Межповерочный период
        /// </summary>
        Verification = 2
    }
    /// <summary>
    /// Способ обработки периода для счетчкиов
    /// </summary>
    public enum TypeAlgoritmsBounds
    {
        /// <summary>
        /// Отсутствует описание
        /// </summary>
        None = 0,
        /// <summary>
        /// Неработоспособный период
        /// </summary>
        NotWork = 1,
        /// <summary>
        /// Работоспособный период
        /// </summary>
        Work = 2
    }
    /// <summary>
    /// Статусы загрузок
    /// </summary>
    public enum DownloadStatuses
    {
        /// <summary>
        /// С ошибками
        /// </summary>
        LoadedWithErrors = 0,
        /// <summary>
        /// Успешно
        /// </summary>
        Success = 1,
        /// <summary>
        /// Загружается
        /// </summary>
        InProgress = 2,
        /// <summary>
        /// Не загружен
        /// </summary>
        NotLoaded = 3,
    }
    /// <summary>
    /// Типы сообщений, полученные в процессе загрузки
    /// </summary>
    public enum DownloadMessageTypes
    {
        /// <summary>
        /// ошибка
        /// </summary>
        Error = 0,
        /// <summary>
        /// Предупреждение
        /// </summary>
        Warning = 1,
        /// <summary>
        /// Комментарий
        /// </summary>
        Comment = 2
    }

    /// <summary>
    /// типы загрузки файла оплат и показаний ПУ
    /// </summary>
    public enum SimpleLoadPayOrIpuType
    {
        /// <summary>
        /// загружать только платежи
        /// </summary>
        Pay = 1,
        /// <summary>
        /// загружать только показания ПУ
        /// </summary>
        Ipu = 2,
        /// <summary>
        /// загружать и платежи и показания
        /// </summary>
        PayAndIpu = 3
    }

    public enum TypePrepareProvs
    {
        /// <summary> переформировать проводки по одному ЛС</summary>
        OneLs = 1,
        /// <summary> переформировать проводки по выбранному списку ЛС</summary>
        ListLs = 2
    }

    /// <summary>
    /// Типы платежных кодов, используется при загрузке оплат из кассы
    /// </summary>
    public enum TypePayCode
    {
        Standart = 1,// стандартный (состоит из 10 или 13 цифр)
        Specific = 2 // специфичный (состоит из букв, цифр и может быть длиной отличной от 10 или 13 символов)
    }
    /// <summary>
    /// Типы кэш-таблиц
    /// </summary>
    public enum TypesCacheTables
    {
        Ls = 1,
        House = 2
    }

}

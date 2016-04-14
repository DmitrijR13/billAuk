using System;
using System.Text;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;


using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.Global
{
    //----------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "Retcode")]
    public struct Returns //возвращаемый результат
    //----------------------------------------------------------------------
    {
        [DataMember(Name = "result", Order = 0)]
        public bool result;
        [DataMember(Name = "text", Order = 1)]
        public string text;
        [DataMember(Name = "tag", Order = 2)]
        public int tag;
        [DataMember(Name = "sql_error", Order = 3)]
        public string sql_error;

        public Returns(bool _result, string _text, int _tag)
        {
            result = _result;
            text = _text;
            sql_error = "";
            tag = _tag;
        }

        public Returns(bool _result, string _text)
        {
            result = _result;
            text = _text;
            sql_error = "";
            tag = 0;
        }

        public Returns(bool _result)
        {
            result = _result;
            text = "";
            sql_error = "";
            tag = 0;
        }
    };

    //----------------------------------------------------------------------
    public enum enSrvOper
    //----------------------------------------------------------------------
    {
        SrvGet,
        SrvFind,
        SrvLoad,
        SrvAdd,
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

        SrvFindChargeStatistics, // для страницы "Статистика о начислениях"
        SrvGetChargeStatistics,

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

        srvFinanceLoad,
        srvFinanceFind,
        srvKassaFind,
        srvFinanceSave,
        srvGetCase,
        srvGetBasket,
        srvReallocatePackInBasket,

        srvAddPrmCalculation,
        srvUpdateTarifCalculation,
        srvAddPrmTarifCalculation,
        sqrDelPrmTarifCalculation,
        sqrDelPrmCalculation,

        Bank,
        Payer,
        PayerReferencedFromBank,

        FindAvailableServices,

        AddBankRequisites,   //Добавление подрядчику банковских реквизитов
        DelBankRequisites,   //Удаление у подрядчика банковских реквизитов
        UpdateBankRequisites, //Обновление у подрядчика банковских реквизитов

        GetDogovorList,        //Получить список договоров
        GetOsnovList,          //Получить список оснований для договора
        AddDogovorRequisites,  //Добавить подрядчику договор
        DelDogovorRequisites,  //Удалить договор
        UpdateDogovorRequisites,//Обновить договор

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
        less_or_equal
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
    static public class CalcThreads
    //----------------------------------------------------------------------
    {
        public static int maxCalcThreads = 11; //11;
        //public static int curCalcThreads = 4; //11;
    }


    //----------------------------------------------------------------------
    static public class Connections
    //----------------------------------------------------------------------
    {
        public static string cert_portal = "";
        public static string cert_client = "";

        public static int max_connections = 200;
        private static int cur_connection = 0;

        /// <summary>
        /// Признак, разрешена ли одновременная работа нескольких пользователей под одним логином
        /// </summary>
        public static bool IsAllowedOneUserHasSeveralSessions = false;

        public static bool Inc_connection()
        {
            if ( !Valid_connection() ) 
                return false;

            Interlocked.Increment(ref cur_connection);
            return ( Valid_connection() );
        }
        public static void Dec_connection()
        {
            Interlocked.Decrement(ref cur_connection);
        }
        public static void Set_connection(int value)
        {
            Interlocked.Exchange(ref cur_connection, value);
        }
        public static bool Valid_connection()
        {
            return (cur_connection <= max_connections);
        }
    }

    /// <summary>
    /// Страницы
    /// </summary>
    static public class Pages
    {
        public const int CalcMonth = 268;           //расчетный месяц
        public const int ServicePriorities = 269;   //приоритеты услуг при распределении оплат
        public const int Settings = 270;            //настройки
        public const int AdminSettings = 271;       //настройки
        public const int Payments = 274;            //форма просмотра списка фактов финансирования
        public const int MenuRequest = 275;
        public const int MenuSubsidy = 276;
        
        public const int Payment = 277;             //
        public const int SubsidySaldo = 278;
        public const int LsCharges = 281;           //расчет дотаций
        public const int Vills = 283;               //справочник муниципальных образований
        public const int ActsOfSupply = 287;        //Акты о фактической поставке        
        public const int Housingstockdescrs = 288; //Форма просмотра ТХ-ЖФ-1
        public const int MenuContragent = 290;
        public const int OLAP = 292;            // OLAP
        public const int UploadData = 293;      // загрузка данных в систему из файлов разных форматов
        public const int MustCalcGroup = 295;
        public const int MustCalc = 296;
        public const int GroupPerekidki = 299; 
        public const int ReestrPerekidok = 300; //реестр перекидок
        public const int Service = 301; //добавление собственных услуг
        public const int PerekidkaLsToLs = 302;
        public const int ReestrPerekidokLs = 303; //реестр перекидок по выбранному лицевому счету
        public const int MoveOverPayments = 304;
        public const int Exchange = 305;
        public const int DefaultAddress = 306;
        public const int KLADR = 307; // КЛАДР
        public const int WorkWithBank = 308; // workwithbank.aspx
        public const int ChangeArea = 309; // changearea.aspx
        public const int BankPayment = 310; // bank-client
        public const int AddPack = 311;
        public const int AddPackLs = 312;
        public const int FilesLoadBank = 314; // Настройка расчета
        public const int SetChanges = 315; // Настройка расчета
        public const int PerekidkaLs = 316;
        public const int ExchangeSZ = 317; //Взаимодействие с соц.защитой
        public const int NeboReestr = 318; //Взаимодействие с соц.защитой
        public const int BankClientFormats = 319;
        public const int SuppExchange  = 321; 

        /// <summary>
        /// Группы страниц
        /// </summary>
        public enum Groups
        {
            /// <summary>
            /// Список лицевых счетов
            /// </summary>
            ListLs = 41
        }
    }

    /// <summary>
    /// Действия
    /// </summary>
    static public class Actions
    {
        public const int ShowConfirmedCounterReadings = 71;        // показать утвержденные показания ПУ
        public const int ShowNotConfirmedCounterReadings = 72;    // показать введенные показания ПУ

        public const int CounterReadingsEnteredByOperator = 111;    //источник показаний - оператор
        public const int CounterReadingsUploadedFromFile = 112;      //источник показаний - файл с показаниями с сайта (для Самары)
        public const int TestBeforeClosingCalcMonth = 131;  //выполнить проверки на возможность закрытия месяца
        public const int CloseCalcMonth = 132;              //закрыть месяц
        public const int MoveServiceUp = 133;               //переместить услугу вверх
        public const int MoveServiceDown = 134;             //переместить услугу вниз
        public const int RefreshDictFromPrimaryBank = 137;  //обновить на клоне справочники из основного банка данных
        public const int AddedSubsidyPayment = 141;
        public const int DistributeSubsidyOrders = 147;         //распределить приказы на перечисление субсидий по лицевым счетам
        public const int CancelSubsidyOrderDistribution = 148;  //отменить распределение приказы на перечисление субсидий по лицевым счетам
        public const int DeleteSubsidyOrder = 149;              //удалить приказ на перечисление субсидий
        public const int UploadAct = 154;                       //загрузить акт
        public const int TakeAct = 155;                         //учесть акт
        public const int act_load_charact_gf = 156;             //загрузить характеристики жилого фонда
        public const int Consider_gf = 157;                     //учесть характеритики жилого фонда
        public const int DeleteAct = 158;                       //удалить акт
        public const int DeleteCashPlan = 161;                  //удалить кассовый план
        public const int DeleteMonthCashPlan = 162;             //Удалить помесячное распределение
        public const int UploadFile = 163;                  //загрузить файл
        public const int DisassembleFile = 166;                  //разобрать файл
        public const int Add = 169;                  //добавить
        public const int Save = 170;                  //сохранить
        public const int Reallocate = 171;                  //повторно распределить оплаты по новым ЛС      
        public const int CalcForIPU = 172;                  //расчет для ИПУ       
        public const int Realize = 174;                  //выполнить
        public const int CounterReadingsEnteredByGilec = 175; //показания ПУ, введенные жильцом (например, через портал)
        public const int AddFormula = 176;                  //добавить формулу
        public const int NewOperation = 179;                  //добавить формулу
        public const int UchetOplat = 181;                  //учет оплат
        public const int CreatePack = 182;                  //создать пачку
        public const int StandardFormat = 870;              //Стандартный формат загрузки данных
        public const int ChangeArea = 184;                  //Перенести выбранный список домов в новую УК
        public const int AddPackage = 185;                  //Перенести выбранный список домов в новую УК
        public const int DelPackage = 186;                  //Перенести выбранный список домов в новую УК
        public const int UploadKLADR = 187;                  //Загрузить адресное пространство из КЛАДР
        public const int DownloadFile = 188;                   //выгрузить файл
        public const int OpenUpload = 189;                 //открыть реестр
        public const int CreateReestr = 191;                   //сформировать реестр
        public const int SetChangesAct = 329;                  //настройка расчетов
        public const int Saldo_5_10 = 212;                      //Сальдовая ведомость 5.10
    }

    /// <summary>
    /// 
    /// </summary>
    static public class WebSetups
    {
        // Коды параметров из настроек
        public const int smtp_host = 1;
        public const int smtp_port = 2;
        public const int smtp_user_name = 3;
        public const int smtp_user_pwd = 4;
        public const int smtp_from_name = 5;
        public const int smtp_from_email = 6;

        /// <summary>
        /// Признак, что работать надо только с центральным банком данных и не обращаться к локальным
        /// </summary>
        public const int WorkOnlyWithCentralBank = 7;
    }

    /// <summary>
    /// Регионы
    /// </summary>
    public static class Regions
    {
        /// <summary>
        /// Справочник регионов
        /// </summary>
        public enum Region
        {
            None = 0,
            Tatarstan = 16,
            Belgorodskaya_obl = 31,
            Samarskaya_obl = 63,
            Tulskaya_obl = 71
        }

        /// <summary>
        /// Возвращает регион по коду
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Region GetById(int id)
        {
            if (id == (int)Region.Samarskaya_obl) return Region.Samarskaya_obl;
            else if (id == (int)Region.Belgorodskaya_obl) return Region.Belgorodskaya_obl;
            else if (id == (int)Region.Tulskaya_obl) return Region.Tulskaya_obl;
            else if (id == (int)Region.Tatarstan) return Region.Tatarstan;
            else return Region.None;
        }
    }

    //----------------------------------------------------------------------
    static public class Constants  //глобальные константы
    //----------------------------------------------------------------------
    {
        #region Excel константы                
        //Приложение Excel(массив потоков)
        public static Thread[] ExcelThreads = new Thread[30];//пул из 30 потоков для выгрузки в Excel

        //Очередь на свободные потоки для Excel
        public static System.Collections.Queue ExcelQueue = new System.Collections.Queue();

        //поток для опроса очереди на выполнение отчета
        public static Thread QThreadExcel = null;

        //Признак наличия Excel
        public static bool ExcelIsInstalled = false;

        //путь на web для хранения временных файлов
        public static string TmpFilesDirWeb = "tmp/";

        /// <summary>
        /// Пути к отчетам, счетам и т.д.
        /// </summary>
        public class Directories
        {
            static string _FilesDir = "~/files/";
            static string _AbsoluteRootPath = "";

            const string REPORTS_DIR = "reports/";
            const string BILL_DIR = "bill/";
            const string WEB_DIR = "web/";
            const string IMPORT_DIR = "import/";

            const string SUBSIDY_DIR = "subsidy/";
            const string ACTS_OF_SUPPLY_DIR = "acts/";
            const string THGF_DIR = "thgf/";

            /// <summary>
            /// Устанавливает абсолютный путь корня сайта
            /// </summary>
            /// <param name="path">Путь к корню сайта</param>
            public static void SetRootAbsolutePath(string path)
            {
                _AbsoluteRootPath = path;
            }

            /// <summary>
            /// Путь к папке с платежными документами
            /// </summary>
            public static string BillDir
            {
                get 
                {
                    return FilesDir + BILL_DIR;
                }
            }

            /// <summary>
            /// Путь к папке с платежными документами
            /// </summary>
            public static string BillAbsoluteDir
            {
                get
                {
                    return BillDir.Replace("~", _AbsoluteRootPath);
                }
            }

            /// <summary>
            /// Путь к папке с платежными документами для портала
            /// </summary>
            public static string BillWebDir
            {
                get
                {
                    return BillDir + WEB_DIR;
                }
            }

            /// <summary>
            /// Путь к папке с платежными документами для портала
            /// </summary>
            public static string BillWebAbsoluteDir
            {
                get
                {
                    return BillWebDir.Replace("~", _AbsoluteRootPath);
                }
            }

            /// <summary>
            /// Путь к папке с выходными документами
            /// </summary>
            public static string FilesDir
            {
                get
                {
                    return _FilesDir;
                }
                set
                {
                    _FilesDir = value;
                }
            }

            /// <summary>
            /// Путь к папке с отчетами
            /// </summary>
            public static string ReportDir
            {
                get
                {
                    return FilesDir + REPORTS_DIR;
                }
            }

            /// <summary>
            /// Абсолютный путь к папке с отчетами
            /// </summary>
            public static string ReportsAbsoluteDir
            {
                get
                {
                    return ReportDir.Replace("~", _AbsoluteRootPath);
                }
            }

            /// <summary>
            /// Путь к папке со входными документами
            /// </summary>
            public static string ImportDir
            {
                get
                {
                    return FilesDir + IMPORT_DIR;
                }
            }

            /// <summary>
            /// Путь к папке со входными документами
            /// </summary>
            public static string ImportAbsoluteDir
            {
                get
                {
                    return ImportDir.Replace("~", _AbsoluteRootPath);
                }
            }

            /// <summary>
            /// Акты о фактической поставке
            /// </summary>
            public static string ActsOfSupplyDir
            {
                get
                {
                    return FilesDir + SUBSIDY_DIR + ACTS_OF_SUPPLY_DIR;
                }
            }

            /// <summary>
            /// Акты о фактической поставке
            /// </summary>
            public static string ActsOfSupplyAbsoluteDir
            {
                get
                {
                    return ActsOfSupplyDir.Replace("~", _AbsoluteRootPath);
                }
            }

            /// <summary>
            /// ТХЖФ
            /// </summary>
            public static string THGFDir
            {
                get
                {
                    return FilesDir + SUBSIDY_DIR + THGF_DIR;
                }
            }

            /// <summary>
            /// ТХЖФ
            /// </summary>
            public static string THGFAbsoluteDir
            {
                get
                {
                    return THGFDir.Replace("~", _AbsoluteRootPath);
                }
            }
        }

        /// <summary>
        /// Путь к папке с отчетами (устарело, используйте Constants.Directories.ReportDir)
        /// </summary>
        public static string ExcelDir
        {
            get
            {
                return Constants.Directories.ReportDir;
            }
        }

        /// <summary>
        /// Путь к папке с выходными документами
        /// </summary>
        public static string FilesDir
        {
            get
            {
                return Constants.Directories.FilesDir;
            }
        }

        #endregion

        public static string Login             = "";
        public static string Password          = "";        
        public const string Linespace          = "http://www.stcline.ru";
        public const string Kassa_3_0 = "WorkOnlyWithCentralBank";

        static public List<_Arms> ArmList = new List<_Arms>();
        static public List<_Pages>    Pages    = new List<_Pages>();
        static public List<_PageShow> PageShow = new List<_PageShow>();
        static public List<_Actions>  Actions  = new List<_Actions>();
        static public List<_ActShow>  ActShow  = new List<_ActShow>();
        static public List<_ActLnk>   ActLnk   = new List<_ActLnk>();
        static public List<_SysPort>  SysPort  = new List<_SysPort>();
        static public Dictionary<int, List<_PageShow>> DictPagesShow = new Dictionary<int, List<_PageShow>>();
        static public Dictionary<int, _Pages> DictPages = new Dictionary<int, _Pages>();
        static public List<_Menu> Menu = new List<_Menu>();
        static public Dictionary<int, List<_ActShow>> DictActionShow = new Dictionary<int, List<_ActShow>>();
        static public Dictionary<int, _Actions> DictActions = new Dictionary<int, _Actions>();

        static public List<_ExtMM>    ExtMM    = new List<_ExtMM>(); //главное меню в ExtJS
        static public List<_ExtPM>    ExtPM    = new List<_ExtPM>(); //подменю главного меню в ExtJS

        public static string VersionWeb = "5.0.20131124";
        public static string VersionSrv = "5.0.20131124";

        public static int VersionDB = 2;

        public static string DefaultAspx = "";

        public static string cons_Webdata;      //ConnectionString Webdata
        public static string cons_Kernel;       //ConnectionString Kernel
        public static string cons_Portal = "";  //ConnectionString Portal
        public static string cons_User;         //User string
        public static bool   Viewerror;         //расшифровка ошибки в логе
        public static bool   Debug;                 
        
        /// <summary>
        /// записывать в лог каждый шаг для отладки
        /// </summary>
        public static bool Trace;

        public const string access_error = "Сервис временно недоступен. Попробуйте выполнить операцию позже.";
        public const int access_code = -1000;
        public const string name_logfile = "Komplat50Log"; //название лог-журнала
        public const int    AllZap       = -101;           //все записи в справочнике
        public const int    DefaultZap   = -102;           //значение по-умолчанию
        public const string ChooseData   = "<Выберите данные>";
        public const string ChooseServ   = "<Выберите услугу>";
        public const string ChooseSupp   = "<Выберите поставщика>";

        //параметры
        public const int regprm_ls = 1;
        public const int regprm_dom = 2;
        public const int regprm_odn = 3;

        // источники показаний ПУ
        public const int ist = 9; 

        //Роли
        public const int roleKartoteka = 10;
        public const int roleAnalitika = 11;
        public const int roleAdministrator = 12;
        public const int rolePriboriUcheta = 13;
        public const int rolePasportistka = 14;
        public const int roleFinance = 15;
        public const int roleHidePersonalInfo = 18; // роль - скрывать персональные данные
        public const int roleSupg = 19;
        public const int roleKassa = 20;
        public const int roleSubsidy = 22;
        public const int roleCalcSubsidy = 23;
        public const int roleRaschetNachisleniy = 921;
        public const int roleUpgOperator = 934;
        public const int roleUpgDispetcher = 935;
        public const int roleUpgPodratchik = 936;
        public const int roleUpgUK = 937;
        public const int roleUpgAdministrator = 919;


        // типы карточек жильца
        public const int typKrtPrib = 1;
        public const int typKrtUbit = 2;
        public const string typKrtPribName = "ПРИБЫТИЕ";
        public const string typKrtUbitName = "УБЫТИЕ";

        // тип прописки
        public const string tprpConst = "П";
        public const string tprpTemp = "В";
        public const string tprpConstName = "ЖИТЕЛЬСТВА";
        public const string tprpTempName = "ПРЕБЫВАНИЯ";

        // СУПГ
        public const string zTypeExternalSource = "99";
        public const bool flEmptyAddress = true;    // признак возможность принимать заявки с пустыми адресами
        public const string NzpEmptyAddress = "0";   // nzp_kvar пустого адреса

        //Виды уникальных кодов для выборки данных
        public const int    selNzp_kvar = 1;                // по л/с   - nzp_kvar
        public const int    selNzp_dom  = 2;                // по домам - nzp_dom

        public const int    acc_in         = 1;             //log_access: вход
        public const int    acc_exit       = 2;             //log_access: выход
        public const int    acc_failure    = 3;             //log_access: несанкционированный вход

        public const string _UNDEF_        = "_UNDEF_";     // значение неопределнного поля
        public const int    _ZERO_         = -999987654;    // значение неопределнного поля
        public static int     users_min     = 60;            // таймаут повторного входа в минутах 
        public const int    blocking_lifetime = 7;          // период действия блокировки записей
        public const int    recovery_link_lifetime = 24;   // время жизни ссылки на восстановление пароля в часах

        public const int    workinfon           = -999;  //признак вызова фоновой службы

        public const int    arm_kartoteka       = 10;   //Картотека
        public const int    arm_analitika       = 11;   //Аналитика
        public const int    arm_admin           = 12;   //Администратор

        public const int    page_login          = 0;     //login.aspx
        public const int    page_default        = 1;     //default.aspx
                                            
        public const int    page_ps             = 10;    //подсистемы      

        public const int    page_myreport   = 5;        //myreport.aspx - файлы пользователя
        public const int    page_settings   = 6;        //settings.aspx - настройки пользователя

        //30 - шаблоны поиска       
        public const int    page_findls     = 31;       //findls.aspx поиск по л/с
        public const int    page_findprm    = 32;       //findprm.aspx
        public const int    page_findch     = 33;       //findch.aspx
        public const int    page_findgil3   = 34;       //findgil3.aspx
        public const int    page_findgil    = 34;       //findgil.aspx
        public const int    page_findcnt    = 35;       //findcnt.aspx
        public const int    page_findnedop  = 36;       //findnd.aspx
        public const int    page_findodn    = 37;       //findodn.aspx
        public const int    page_findserv   = 38;       //findserv.aspx
        public const int    page_findsupg   = 39;       //findsupg.aspx

        //40 - выбранные списки
        public const int    page_spis           = 40;    //списки
        public const int    page_spisls         = 41;    //spisls.aspx
        public const int    page_spisdom        = 42;    //spisdom.aspx
                                            
        public const int    page_spisul         = 43;    //spisul.aspx
        public const int    page_spisar         = 44;    //spisar.aspx
        public const int    page_spisgeu        = 45;    //spisgeu.aspx
        public const int    page_spisbd         = 46;    //spisbd.aspx
        public const int    page_domul          = 47;    //дома улицы
        public const int    page_backtoprm      = 48;    //Вернуться на список параметров
        public const int    page_spissupp       = 49;    //spis_supp.aspx
        public const int    page_perechen_lsdom = 50;    //перечень счетов дома
        public const int    page_spisprm        = 51;    //spisprm.aspx / baselist.aspx
        public const int    page_spispu         = 53;    //counters.aspx
                                            
        public const int    page_spisval        = 54;    //spisval.aspx для квартирных ПУ
        public const int    page_spisnd         = 55;    //spisnd.aspx
        public const int    page_spisgil        = 56;    //spisgil.aspx
        public const int    page_spisdomls      = 57;    //baselist.aspx
        public const int    page_spisuldom      = 58;    //дома данной улицы
        public const int    page_spisprmdom     = 59;    //spisprm.aspx / baselist.aspx
        public const int    page_spisvaldom     = 61;    //spisval.aspx для домовых ПУ        
                                            
        public const int    page_puls           = 62;    //КПУ
        public const int    page_pudom          = 63;    //ДПУ
       
        //public const int    page_prm       = 64;    //Значение одного квартирного параметра
        public const int    page_spisvalgroup   = 66;    //spisval.aspx для групповых ПУ (от квартиры)
        public const int    page_spisvalgroupdom= 67;    //spisval.aspx для групповых ПУ (от дома)
        public const int    page_counterscardls = 68;    //counterscard.aspx квартирный прибор
        public const int    page_counterscarddom= 69;    //counterscard.aspx домовой прибор
                                            
        public const int    page_data           = 70;    //данные лс
        public const int    page_datadom        = 71;    //данные дома
        public const int    page_аnalytics      = 72;    //аналитика
        public const int    page_dictionaries   = 73;    //справочники
        public const int    page_datapack       = 76;    //данные о пачке
        public const int    page_operations     = 77;    //пункт меню - Операции
        public const int    page_data_about_order = 78;    //данные о заявке
                                            
        public const int    page_aa_adres       = 81;    //адресное пространство
        public const int    page_aa_supp        = 82;    //анализ по поставщикам
        public const int    page_counterscard   = 91;    //counterscard.aspx

        public const int    page_gil            = 91;    //gil.aspx - карточка жильца (Комлат 2.0)

        public const int    page_pugroup        = 92;    //counters.aspx список групповых приборов
        public const int    page_counterscardgroup= 93;  //counterscard.aspx групповой прибор
        public const int    page_countertypes   = 94;    //countertypes.aspx
        public const int    page_spisserv       = 95;    //spisserv.aspx перечень услуг
        public const int    page_supp_formuls   = 96;    //supp_formuls.aspx     
        public const int    page_groupls        = 97;    //groupls
        public const int    page_cardls         = 98;    //cardls
        public const int    page_carddom        = 99;    //carddom    
        public const int    page_groupspisprmls = 100;      // spisprm - групповые операции: характеристики жилья для выбранных лицевых счетов
        public const int    page_groupspisprmdom= 102;      // spisprm - групповые операции: характеристики жилья для выбранных домов
        public const int    page_groupprmls     = 101;          // prm - групповые операции с выбранной квартирной характеристикой жилья для выбранных лицевых счетов
        public const int    page_groupprmdom    = 103;          // prm - групповые операции с выбранной квартирной характеристикой жилья для выбранных домов
        public const int    page_groupspisserv  = 104;          // serv - групповые операции с выбранной услугой для выбранных лицевых счетов
        public const int    page_group_supp_formuls = 105;      //serv 
        public const int    page_statcharge     = 106;          //статистика по начислениям 
        public const int    page_distrib        = 107;          //distrib
        public const int    page_groupcardls    = 108;          //групповые операции реквизиты л/с  
        public const int    page_groupcarddom   = 109;          //групповые операции реквизиты дома
        public const int    page_groupnedop     = 110;          //групповые операции недопоставки
        public const int    page_changesostls   = 111;          //характеристики жилья изменение состояния л/с
        public const int    page_statchargedom  = 112;          //статистика по начислениям по дому

        public const int    page_charge         = 120;   //начисления
        public const int    page_charges        = 122;   //charges.aspx
        public const int    page_listpays       = 123;   //listpays.aspx
        public const int    page_odn            = 124;   //odn.aspx
                                            
        public const int    page_saldols        = 121;   //saldols.aspx
        public const int    page_saldodom       = 126;   //saldodom.aspx
        public const int    page_saldouk        = 127;   //saldouk.aspx
        public const int    page_saldosupp      = 130;   //saldosupp.aspx
                                            
        public const int    page_bill           = 129;   //bill.aspx    
        public const int    page_billrt         = 131;   //billrt.aspx    
        public const int    page_pay            = 132;   //pay.aspx
        public const int    page_reportls       = 133;   //report.aspx по лицевому счету
        public const int    page_reportlistls   = 134;   //report.aspx по выбранным спискам ЛС
        public const int    page_reportgil      = 135;   //report.aspx по жильцу
        public const int    page_reportlistgil  = 136;   //report.aspx по выбранным спискам жильцов
        public const int    page_reportdom      = 137;   //report.aspx по выбранному дому
        public const int    page_reportlist     = 138;   //report.aspx по списку заявок
        public const int    page_reportlistplan = 139;   //report.aspx по списку плановых работ

        public const int    page_users          = 151;   //users.aspx
        public const int    page_roles          = 152;   //roles.aspx
        public const int    page_usercard       = 153;   //usercard.aspx
        public const int    page_rolecard       = 154;   //rolecard.aspx
        public const int    page_access         = 155;   //access.aspx

        public const int page_processes         = 161;      //processes.aspx
        public const int page_kvargil           = 162;      //spisgil.aspx - поквартирная карточка (Комлат 2.0)
        public const int page_spisgilper        = 163;      //spisglp.aspx - список периодов временного убытия жильца (Комлат 2.0)
        public const int page_glp               = 164;      //glp.aspx - период временного убытия жильца (Комлат 2.0)
        public const int page_perekidki         = 165;      //perekidki.aspx - изменения сальдо
        public const int page_rashod_kvar       = 166;      //rashod.aspx - расход по квартире
        public const int page_rashod_dom        = 167;      //rashod.aspx - расход по дому
        public const int page_tarifs            = 168;      //tarifs.aspx - тарифы
        public const int page_one_tarif         = 169;      //prm.aspx - значения одного тарифа
        public const int page_sysparams         = 170;      //sysparams.aspx - системные параметры
        public const int page_prm_pu_kvar       = 171;      //prm.aspx - значения параметра прибора учета (из данных о квартире)
        public const int page_prm_pu_dom        = 172;      //prm.aspx - значения параметра прибора учета (из данных о доме)
        public const int page_prm_kvar          = 173;      //prm.aspx - значения параметра (из реквизитов ЛС)
        public const int page_prm_supp          = 174;      //prm.aspx - значения параметра (из списка параметров поставщика)
        public const int page_suppparams        = 175;      //suppparams.aspx
        public const int page_frmparams         = 176;      //frmparams.aspx
        public const int page_spissobstw        = 177;      //spissobstw.aspx
        public const int page_kartsobstw        = 178;      //kartsobstw.aspx
        public const int page_group_nedop_dom   = 179;      //spisnd.aspx - групповые операции с домовыми недопоставками
        public const int page_group_spis_ls_prm_dom     = 180;  //spisprm.aspx - список квартирных параметров для груп опер по домам 
        public const int page_group_ls_prm_dom          = 181;  //prm.aspx - групповые операции с квартирной характеристикой для лицевхы счетов выбранных домов
        public const int page_group_spis_serv_dom       = 182;  //spisserv.aspx - групповые операции с услугами для ЛС выбранных домов
        public const int page_group_supp_formuls_dom    = 183;  //serv.aspx - групповые операции с услугой для ЛС выбранных домов
        public const int page_report_odn        = 184;      //reportodn.aspx - протокол расчета ОДН
        public const int page_spispu_communal   = 185;      //counters.aspx - список коммунальных ПУ
        public const int page_spisval_communal  = 186;      //spisval.aspx - список показаний коммунальных ПУ
        public const int page_pu_communal       = 187;      //countercard.aspx - карточка коммунального ПУ
        public const int page_prm_dom           = 188;      //prm.aspx - значения параметра (из реквизитов дома)
        public const int page_supg_kvar_orders  = 189;      //order.aspx - заявки лицевого счета
        public const int page_spisservdom       = 190;      //перечень услуг для дома
        public const int page_spisnddom         = 191; //недопоставки по дому
        public const int page_prmodn            = 192; //параметры настройки ОДН
        public const int page_joborder          = 193; //наряд-заказ
        public const int page_spisgilhistory    = 194; //история жильца
        public const int page_findgroupls       = 195; //поиск по группам
        public const int page_group             = 196; //групповые операции добавления(исключения) выбранного списка в группу (из группы)
        public const int page_spis_order        = 197; //список выбранных заявок
        public const int page_pack              = 198; //пачка платежей pack.aspx
        public const int page_pack_ls           = 199; //платеж pack_ls.aspx
        public const int page_upload_pack       = 200; //загрузка пачки uploadpack.aspx
        public const int page_finances_findpack = 201; // шаблон поиска по оплатам
        public const int page_finances_pack     = 202; //пачка оплат
        public const int page_finances_operday  = 203; //операционный день
        public const int page_finances_pack_ls  = 204; //квитанция об оплате
        public const int page_supg_order        = 205; //одна претензия
        public const int page_report_common     = 206; //отчеты по всему банку
        public const int page_supg_arm_operator = 207; //АРМ оператора
        public const int page_supg_raw_orders   = 208; //raworders.aspx
        public const int page_incoming_job_orders = 209; //список нарядов-заказов на выполнение
        public const int page_prm_norms         = 210; //norms.aspx
        public const int page_sprav_cel_prib    = 211; //sprav.aspx - цель прибытия
        public const int page_sprav_docs        = 212; //sprav.aspx - документы
        public const int page_sprav_rodst       = 213; //sprav.aspx - родственные отношения
        public const int page_sprav_grazhd      = 214; //sprav.aspx - гражданства
        public const int page_sprav_adresses    = 215; //address.aspx - страна, регион, город, район, нас. пункт
        public const int page_sprav_rajon_doma  = 216; //sprav.aspx - районы дома
        public const int page_sprav_organ_reg_ucheta    = 217; //sprav.aspx - орган регистрационного учета
        public const int page_sprav_mesto_vidachi_doc   = 218; //sprav.aspx - место выдачи документа
        public const int page_sprav_doc_sobst   = 219; //sprav.aspx - документы о собственности
        public const int page_supg_nedop        = 220; //nedop.aspx - выгрузка недопоставок
        public const int page_services          = 221; //services.aspx - справочник услуг
        public const int page_service_params    = 222; //servparams.aspx - список параметров выбранной услуги
        public const int page_prm_serv          = 223; //prm.aspx - значения одного параметра услуги
        public const int page_available_services = 224; //availableservices.aspx - доступные услуги
        public const int page_available_service = 225; //availableservice.aspx - доступная услуга
        public const int page_find_server       = 226; //findserver.aspx - писк по серверам
        public const int page_area_params       = 227; //areaparams.aspx - список параметров выбранной территории
        public const int page_prm_area          = 228; //prm.aspx - значения одного параметра территории
        public const int page_geu_params        = 229; //geuparams.aspx - список параметров выбранного участка
        public const int page_prm_geu           = 230; //prm.aspx - значения одного параметра участка
        public const int page_area_requisites   = 231; //arearequisites.aspx - реквизиты территории
        public const int page_payer_requisites  = 232; //payerrequisites.aspx - реквизиты подрядчика
        public const int page_payer_contracts   = 233; //contracts.aspx - договоры с подрядчиком
        public const int page_payer_transfer    = 234; //payertransfer.aspx - перечисления подрядчикам
        public const int page_menu_oper_day     = 235; //пункт меню операционный день
        public const int page_supg_kvar_job_order = 238;//kvarjoborder.aspx - все наряды заказы по квартире
        public const int page_percent = 239;            //percent.aspx - процент удержания
        public const int page_ls_contracts      = 240; //lscontracts.aspx - список договоров по ЛС
        public const int page_counter_readings = 241; //counterreadings.aspx - списочный ввод показаний ПУ (сразу по всему дому)
        public const int page_add_period_ub_to_selected = 242; //glp.aspx - добавление периода убытия выбранным жильцам
        public const int page_upload_counter_values = 243; //uploadcounters.aspx - загрузка показаний ПУ из файла
        public const int page_credit             = 244; //рассрочка
        public const int page_find_planned_works = 245; //поиск плановых работ
        public const int page_planned_works      = 246; //список плановых работ
        public const int page_planned_work_add   = 247; //плановая работа для добавления
        public const int page_sprav_servorgs     = 248; //справочник служб / организаций
        public const int page_planned_work_show  = 250; //плановая работа для просмотра
        public const int page_planned_work_ls    = 251; //плановая работа по ЛС
        public const int page_claimcatalog       = 252; //справочник претензий
        public const int page_case               = 253; //портфель
        public const int page_analisis           = 255;//страница для гистограмм
        public const int page_contractorcatalog  = 256;  //страница справочник подрядчиков
        public const int page_bankcatalog        = 257;  //страница справочник банков
        public const int page_basket             = 258;//корзина
        public const int page_gendomls           = 260; // генерация ЛС по дому
        public const int page_streetcatalog      = 261; //справочник улиц
        public const int page_correctsaldo       = 262; //корректировка сальдо
        public const int page_groupprm           = 263; //групповой ввод характеристик жилья
        public const int page_genlspu            = 264;  //генерация приборов учета по списку ЛС
        public const int page_condistrpayments   = 265;  //Контроль распределения оплат
        //public const int page_prm_dom = 266;  //значения одного домового параметра
        public const int page_addtask            = 266; // добавление задания (пересечение с предыдущей константой)
        public const int page_survey_job_orders  = 267;//список нарядов-заказов для опроса
        public const int page_calc_month         = 268; //расчетный месяц
        public const int page_percpt             = 284; //справочник уровня платежей граждан
        public const int page_cashplan           = 285; //загрузка кассового плана

        //Дотации
        public const int page_requests          = 272;  //Шаблон поиска по заявкам
        public const int page_request           = 273;  //Заявка на финансирование
        public const int page_agreements        = 279;  //Список соглашений с подрядчиками
        public const int page_agreement         = 280;  //Карточка соглашения с подрядчиком

        // Рассылка сообщений
        public const int page_messagelist       = 282; // список сообщений 
        public const int page_newmessage        = 286; // новое сообщение        
        public const int page_phonesprav        = 289; // справочник телефонов

        public const int page_refresh_kp_sprav  = 297; //обновление данных комплат в УПГ
        public const int page_sprav_themes      = 298; //Справочник классификация сообщений УПГ
        
                                           
        public const int    act_groupby_month   = 520;   //Группировать по месяцу
        public const int    act_groupby_service = 521;   //Группировать по услуге
        public const int    act_groupby_supplier= 522;   //Группировать по поставщику
        public const int    act_groupby_formula = 523;   //Группировать по формуле
        public const int    act_groupby_area    = 524;   //По территориям
      
        public const int    act_groupby_geu     = 525;   //По отделениям
        public const int    act_groupby_bd      = 526;   //По банкам данных
        public const int    act_groupby_dom     = 527;   //По домам
                                                       
        public const int    act_show_saldo      = 528;   //Показывать сальдовые показатели    
        public const int    act_groupby_device  = 529;   //Группировка норматив / прибор учета    
        
        public const int    act_groupby_payer   = 536;   //Группировать по плательщикам
        public const int    act_groupby_bank    = 537;   //Группировать по банкам
        public const int    act_groupby_date    = 538;   //Группировать по датам
        public const int    act_groupby_town   = 539;   //По районам

        public const int    sortby_adr          = 601;   //"Сортировать по адресу"
        public const int    sortby_ls           = 602;   //"Сортировать по лс"  
        public const int    sortby_ul           = 603;   //"Сортировать по улице"    
        public const int    sortby_uk           = 604;   //"Сортировать по УК"    
        public const int    sortby_serv         = 605;   //"Сортировать по услуге"    
        public const int    sortby_supp         = 606;   //"Сортировать по поставщику"    
        public const int    sortby_fiodr        = 607;   //"Сортировать по ФИОДР"    

        public const int    sortby_login        = 608;   //"Сортировать по логину"    
        public const int    sortby_username     = 609;   //"Сортировать по имени пользователя"    
        public const int    sortby_nzp_user     = 1608;  //
        public const int    sortby_email        = 1609;  //
                                         
                                         
        public const int    menu_help           = 3;     //help
        public const int    menu_previos        = 4;     //previos page
        public const int    menu_myfiles        = 5;     //мои файлы
        public const int    menu_exit           = 999;   //exit
        public const int    menu_seans          = 950;   //seans
        public const int    menu_not            = 949;   //нет данных
                                         
        public const int    act_find            = 1;     //поиск
        public const int    act_erase           = 2;     //очистка ШП
        public const int    act_open            = 3;     //открыть данные
        public const int    act_add             = 4;     //добавить   
        public const int    act_refresh         = 5;     //обновить
        public const int    act_showmap         = 7;     //показать карту
        public const int    act_print           = 8;     //Открыть для печати
        public const int    act_block           = 9;     //Заблокировать/разблокировать пользователя
 
        public const int    act_process_start   = 10; //запустить обработчик заданий
        public const int    act_process_pause   = 11; //приостановить обработчик
        public const int    act_delete_all      = 12; //удалить все
        public const int    act_delete_session  = 13; //удалить сессию пользователя
        public const int    act_open_puindication  = 14; //открыть показания ПУ
        public const int    act_reset_user_pwd  = 15; //сбросить пароль выбранного пользователя
        public const int    act_save_val        = 16; //сохранить значение
        public const int    act_del_val         = 17; //удалить значение параметра
        public const int    act_add_nedop       = 18; //добавить недопоставку
        public const int    act_del_nedop       = 19; //удалить недопоставку

        public const int    act_showallprm      = 20; //показать все параметры

        public const int    act_add_serv        = 21; //добавить период действия услуги, назначить поставщика и формулу расчета
        public const int    act_del_serv        = 22; //удалить период действия услуги
        public const int    act_add_gil         = 23; //добавить нового жильца
        public const int    act_copy_ls         = 24; //копировать л/с
        public const int    act_add_dom         = 25; //создать дом
        public const int    act_delete          = 26; // удалить запись
        public const int    act_del_pu          = 64; //удалить ПУ
        
        public const int    act_aa_recalc       = 65;    //подсчет агрегированных сумм
        public const int    act_aa_refresh      = 66;    //обновить данные
        public const int    act_save            = 61;    //сохранить данные
        public const int    act_prm             = 67;    //Параметры - архив / история

        public const int    act_update_role_filters = 68;   // обновить списки территорий, участков, услуг, поставщиков, банков данных, которые применяются для ограничения прав доступа ролей
        public const int    act_get_report      = 69;       //действие получить отчет
        public const int    act_calc            = 70;       // рассчитать начисления
        public const int    act_export_to_excel = 73;       // выгрузить данные в excel
        public const int    act_enter_archive   = 74;       // войти в архив
        public const int    act_exit_archive    = 75;       // выйти из архива
        public const int    act_open_period_serv= 76;       // перейти к просмотру периодов действия услуги, поставщиков и формул расчета
        public const int    act_open_prm_serv   = 77;       // перейти к просмотру параметров для выбранной услуги
        public const int act_copy = 78;                     //копировать
        public const int act_paste = 79;                    //вставить
        public const int act_showls = 80;                   //показать л/с
        public const int act_add_joborder = 81;             //добавить наряд-заказ
        public const int act_add_ingroup = 82;              //добавить список в группу
        public const int act_del_outgroup = 83;             //исключить список из группы
        public const int act_go_back_to_orders = 84;        //вернуться к списку претензий лицевого счета
        public const int act_add_pack           = 85;       //добавить пачку
        public const int act_close_pack         = 86;       //закрыть пачку
        public const int act_add_pack_ls        = 87;       //добавить новый платеж
        public const int act_upload_pack        = 88;       //загрузить пачку
        public const int act_find_kassa = 89;               //найти
        public const int act_del_pack_ls = 90;              //удалить оплату
        public const int act_cancel_pack_distribution = 91; //отменить распределение пачки оплат
        public const int act_delete_pack = 92;              //удалить пачку оплат
        public const int act_distribute_pack = 93;          //распределить пачку оплат
        public const int act_add_zvk = 94;                  //добавить заявку
        public const int act_clear_users_cache = 95;        //удалить выбранные списки пользователей и другие пользовательские таблицы из кэша
        public const int act_add_user = 96;                 //добавить пользователя
        public const int act_sentJobOrder_toExecute = 97;   //Отправить наряд-заказ на исполнение
        public const int act_checkJobOrder_toExecute = 98;  //Отметить получение наряд-заказа

        public const int act_set_zakaz_act_actual   = 100;    //выставление признака формирования недопоставки
        public const int act_close_zakaz_act_actual = 101;  //снятие признака формирования недопоставки
        public const int act_open_params            = 103;  //показать параметры
        public const int act_open_area_requisites   = 104;  //перейти к реквизитам территории
        public const int act_open_payer_transfer    = 107;  //перейти к перечислениям пожрядчикам
        public const int act_calc_saldo             = 108;  //расчет сальдо поставщика
        public const int act_add_period_ub_to_selected = 109;  //добавить период убытия выбранным жильцам
        public const int act_upload_counter_values  = 110;  //загрузить показания ПУ из файла
        public const int act_restart_hosting = 113;
        public const int act_incase = 114;//поместить в портфель
        public const int act_outcase = 115;//убрать из портфеля
        public const int act_cancelplat = 116;//отмена платежей
        public const int act_action = 117;//выполнить(для гистограммы)
        public const int act_repair_one = 118;// исправить 1 
        public const int act_repair_select = 119;// исправ выбран, 
        public const int act_distrib_one = 120;// распр1,
        public const int act_distrib_select = 121;//распр выбран
        public const int act_gen_dom_ls = 122;    //генерировать ЛС
        public const int act_gen_pu = 123;        //генерировать ПУ
        public const int act_cont_distrib           = 124; //контроль распределения - отчет
        public const int act_find_error_distrib     = 125;//найти ошибку распределения
        public const int act_find_error_payment     = 126;//найти ошибка в оплатах
        public const int act_open_pack_ls = 127;    //открыть квитанцию
        public const int act_print_show = 128;     //Версия для печати
        public const int act_refresh_addresses = 129;     //Обновить адресное пространство в центральном банке
        public const int act_closeJobOrder_toExecute = 130;  //Закрыть наряд-заказ
        public const int act_cancelJobOrder                          = 136; //Отменить полученный наряд-заказ        
        public const int act_req_edit                                = 138; //исправить заявку на финансирование
        public const int act_req_approve                             = 139; //утвердить заявку на финансирование
        public const int act_req_del                                 = 140; //удалить заявку на финансирование
        public const int act_req_add                                 = 142; //добавляет новую заявку на финансирование
        public const int act_page_refresh                            = 143; //обновляет текущую страницу
        public const int act_agreement_add                           = 145; //добавить соглашение с подрядчиком
        public const int act_agreement_del                           = 146; //Удалить соглашение с подрядчиком
        public const int act_new_sms                = 150;   // Новое сообщение
        public const int act_send_sms               = 151;   // Отправить сообщение
        public const int act_del_sms                = 152;   // Удалить сообщение
        public const int act_load_cashplan                          = 153;//загрузить кассовый план        
        public const int act_charge                                 = 164;//рассчитать
        public const int act_exec_refresh                           = 173;  //выполнить обновление данных комплат в УПГ
        public const int act_calc_odpu_rashod                       = 177;  // подсчитать расход для ОДПУ 

        public const int act_report_spravka_po_smerti                = 201; // Справка по смерти ф5 для Самары
        public const int act_report_spravka_o_sostave_semji          = 202; // справка о составе семьи        
        public const int act_report_spravka_s_mesta_reg_v_sud        = 203; // Справка с места регистрации (в суд) для Самары
        public const int act_report_spravka_na_privatizac            = 204; // Справка для приватизации
        public const int act_report_liсevoi_schet                    = 206; // Справка для лицевого счета
        public const int act_report_listok_ubit                      = 218; // адресный листок убытия
        public const int act_report_listok_pribit                    = 219; // адресный листок прибытия
        public const int act_report_zay_reg_preb                     = 214; // заявление о регистрации по месту пребывания
        public const int act_report_zay_reg_git_f6                   = 215; // заявление о снятии с регистрационного учета по месту жительства
        public const int act_report_zay_reg_git                      = 216; // заявление о регистрации по месту жительства
        public const int act_report_rfl1                             = 223; // сведения о регистрации гражданина рф по месту жительства рфл1
        public const int act_report_listok_stat_prib                 = 224; // листок статистического учета прибытия
        public const int act_report_spis_reg_snyat                   = 220; // сведения о регистрации граждан и снятии с регистрационного учета
        public const int act_report_spis_vuchet                      = 221; // сведения о регистрации граждан и снятии с регистрационного учета
        public const int act_report_spis_smena_dok                   = 222; // реестр граждан, сменивших или получивших удостоверение личности
        public const int act_report_spis_gil                         = 225; // универсальный реестр граждан
        public const int act_report_smena_passp                      = 226; //заявление на смену(выдачу) паспорта
        public const int act_report_listok_stat_ubit                 = 227; // листок статистического учета выбытия
        public const int act_report_report_prm                       = 228; // генератор отчетов по параметрам
        public const int act_report_kart_analis                      = 229; // карта аналитического учета
        public const int act_report_sverka_rashet                    = 230; // генератор отчетов по параметрам - сверка расчетов с жильцом
        public const int act_report_dom_nach                         = 231; // расшифровка по домам - начисления
        public const int act_report_sprav_suppnach                   = 232; // справка по поставщикам коммунальных услуг
        public const int act_report_sprav_hasdolg                    = 233; // справка по отключениям подачи коммунальных услуг        
        public const int act_report_sprav_otkl_uslug                 = 234; // справка по отключениям подачи коммунальных услуг
        public const int act_report_sprav_v_sud                      = 235; // справка для предъявления в суд
        public const int act_report_lic_schet_excel                  = 236; // справка о лицевом счете
        public const int act_report_kart_registr                     = 237; // карточка регистрации
        public const int act_report_sprav_reg                        = 238; // Справка архивная
        public const int act_report_sprav_suppnachhar                = 239; // справка по поставщикам коммунальных услуг с характеристиками
        public const int act_report_izvechenie_za_mesyac             = 240; //извещение за месяц 
        public const int act_report_kvar_kart                        = 241; //поквартирная карточка
        public const int act_report_spravsmg                         = 242; //справка с места жительства
        public const int act_report_spravpozapros_smr                = 243; //справка по запросам
        public const int act_report_spravpozapros                    = 339; //справка по запросам
        public const int act_report_serv_supp_nach_tula              = 340; //Отчет по начислениям для Тулы
        public const int act_report_serv_supp_money_tula             = 341; //Отчет по поступлениям для Тулы
        public const int act_report_vipis_counters                   = 342; //Выписка по счетчикам для самары


        public const int act_report_oplata_uslug_za_post_uslugi      = 244; //оплата гражданами-получателями коммунальных услуг за поставленные услуги
        public const int act_report_Inf_PoRashet_SNasel              = 245; //Информация по расчетам с населением
        public const int act_report_energo_act                       = 246; //Акт сверки по Энергосбыту
        public const int act_report_report_Nachisleniya              = 247; //Генератор по начислениям
        public const int act_report_sprav_pu_ls                      = 248; //Справка о начислениях по квартирным ПУ
        public const int act_report_norma_potr                       = 249; //Сводная ведомость по нормативам потребления
        public const int act_report_sos_gil_fond                     = 250; //Информация по расчетам с населением
        public const int act_report_dom_nach_pere                    = 251; //Акт сверки по Энергосбыту
        public const int act_report_sprav_nach_pu                    = 252; //Генератор по начислениям
        public const int act_report_spis_dolg                        = 253; //Список должников с указанием срока задолженности
        public const int act_report_ved_dolg                         = 254; //Сведения о просроченной задолженности
        public const int act_report_nach_opl_serv                    = 255; //Справка по начислению платы за услуги
        public const int act_report_dom_odpu                         = 256; //Информация по расчетам с населением
        public const int act_report_ved_opl                          = 257; //Акт сверки по Энергосбыту
        public const int act_report_ved_pere                         = 258; //Генератор по начислениям
        public const int act_report_make_kvit                        = 259; //Генератор по начислениям
        public const int act_report_protokol_odn                     = 260; //оплата гражданами-получателями коммунальных услуг за поставленные услуги сводный отчет
        public const int act_report_calc_tarif                       = 261; //Калькуляция тарифа по услуге содержание жилья
        public const int act_report_oplata_uslug_za_post_uslugi_svod = 262; //оплата гражданами-получателями коммунальных услуг за поставленные услуги сводный отчет
        public const int act_report_order_list                       = 263; // отчет 1.5. Список заявок
        public const int act_report_count_orders_serv                = 264; // отчет 2.1. количество заявлений, направленных по услугам за период
        public const int act_report_count_orders_supp                = 265; // отчет 2.2. количество заявлений, направленных по поставщикам за период
        public const int act_report_10_14_3                          = 266; // отчет сальдовая оборотная ведомость 10.14.3
        public const int act_report_10_14_1                          = 267; // отчет сальдовая оборотная ведомость 10.14.1
        public const int act_report_spravsmg2_smr                    = 268; //справка для незарегистрированного собственника
        public const int act_report_spravsmg2                        = 337; //справка для незарегистрированного собственника
        public const int act_report_zakaz                            = 269; //отчет по заказам
        public const int act_report_sprav_group                      = 270; //отчет по услугам группы  Содержание жилья 
        public const int act_report_sprav_po_otkl_usl_dom_vinovnik   = 271; //отчет по услугам группы  Содержание жилья
        public const int act_report_sprav_po_otkl_usl_geu_vinovnik   = 272; //отчет по услугам группы  Содержание жилья 
        public const int act_report_planned_works_list_supp          = 273; // отчет 3.1. Список плановых работ - сведения по отключениям услуг по поставщикам
        public const int act_report_planned_works_list               = 274; // отчет 3.2. Список плановых работ - сведения по отключениям услуг
        public const int act_report_planned_works_list_act           = 275; // отчет 3.3. Список плановых работ - акты по отключениям услуг
        public const int act_report_count_joborder_dest              = 276; // отчет 2.3. Количество нарядов-заказов по неисправностям

        public const int act_report_info_from_service                = 279; // отчет 1.1. Информация, полученная ОДДС
        public const int act_report_appinfo_from_service             = 278; // отчет 1.2. Приложение к информации, полученной ОДДС
        public const int act_report_joborder_period_outstand         = 277; // отчет 1.4. Список невыполненных нарядов-заказов к концу периода

        public const int act_report_count_order_readres              = 280; // отчет 2.4. Количество переадресаций заявок, принятых ОДДС
        public const int act_report_message_list                     = 281; // отчет 1.3.1. Список сообщений, зарегестрированных ОДДС
        public const int act_report_message_quest_list               = 282; // отчет 1.3.2. Список сообщений, зарегестрированных ОДДС(опрос)
        public const int act_report_spis_gil_mod = 283; // универсальный реестр граждан модифицированный
        public const int act_report_sprav_reg_old = 284; // архивная справка - 2
        public const int act_report_sverka_day = 285; // сверка поступлений за день
        public const int act_report_sverka_period = 286; // сверка поступлений за период
        public const int act_report_control_distrib_payments = 287; // контроль распределения оплат

        public const int act_report_register_counters = 288; //реестр счетчиков по лицевым счетам
        public const int act_report_nach_supp = 289;//начисления по поставщикам
        public const int act_report_sald_statment_services = 290; //Сальдовая оборотная ведомость по услугам
        public const int act_report_charges = 291;//начисления по поставщикам

        public const int act_report_pu_data = 292; // данные ПУ по жилым домам
        public const int act_report_rashod_pu = 293; // отчет по расходу на дома
        public const int act_report_prot_calc_odn = 294; // протокол рассчитанныз значений ОДН
        public const int act_report_pasp_ras     = 295; //отчет рассогласование с паспортисткой для Самары
        public const int act_report_sprav_supp = 299; //отчет по услугам группы  Содержание жилья 
        public const int act_report_ls_pu_vipiska = 300; //выпиcка из ЛС о поданных показаниях КПУ 
        public const int act_report_spravka_na_privatizac2_smr = 301; // Справка для приватизации2
        public const int act_report_spravka_na_privatizac2 = 338; // Справка для приватизации2
        public const int act_report_prot_calc_odn2 = 302; // протокол рассчитанныз значений ОДН расширенный
        public const int act_report_upload_kassa_3_0= 303; // выгрузка в кассу 3.0

        public const int daily_payments = 304; //отчет
        public const int act_report_sverka_month = 305; //отчет сверка за месяц
        public const int act_report_saldo_v_bank = 306; // выгрузка сальдо для банка (dbf)
        public const int act_report_gub_curr_charge = 307;  // состояние текущих начислений по домам (Губкин)
        public const int act_report_gub_itog_oplat = 308;   // итоги оплат по домам
        public const int act_report_sprav_suppnachhar2 = 309; // справка по поставщикам форма 3
        public const int act_report_nach_opl_serv2 = 310; //Справка по начислению платы за услуги форма 2
        public const int stat_gilfond = 311; //отчет статистика состояния жилищного фонда
        public const int act_report_raspiska_docum                   = 312; //отчет Расписка в получении документов
        public const int act_report_svid_reg_preb                    = 313; //отчет Свидетельство о регистрации по месту пребывания
        public const int act_report_spravpozapros_gub                = 314; //отчет Справка с места жительства для г.Губкин
        public const int act_report_vipiska_ls                       = 315; //отчет Выписка из лицевого счета
        public const int act_report_zay_privat                       = 316; //отчет Заявление на приватизацию
        public const int act_report_nachisl_ls                       = 317; //отчет Статистика начислений по лицевым счетам
        public const int act_report_nachisl_dom                      = 318; //отчет Статистика начислений по домам
        public const int act_report_nachisl_uch                      = 319; //отчет Статистика начислений по участкам
        public const int act_report_upload_ipu = 320; //выгрузка данных о показаниях ПУ
        public const int act_report_upload_charge = 321; //выгрузка данных о начислениях
        public const int act_report_upload_reestr = 322; // выгрузка реестра для загрузки в БС
        public const int act_report_soc_protection = 323; // выгрузка начислений в орган социальной защиты населения

        public const int act_report_saldo_ved_energo = 324; //50.1 Сальдовая ведомость по энергосбыту
        public const int act_report_dolg_ved_energo = 325; //50.2 Ведомость должников

        public const int act_report_protocol_sver_data = 326;//протокол сверки данных
        public const int act_report_protocol_sver_data_ls_dom = 327;//сверка характеристик лицевых счетов и домов
        public const int act_report_pasp_ras_gub = 328; //отчет рассогласование с паспортисткой для г.Губкин
        public const int act_report_sprav_na_prog = 330;//справка на проживание и состав семьи
        public const int act_report_sprav_o_smert_kazan = 331;//справка о смерти(для г.Казань) 

        public const int act_report_vrem_zareg = 332; //информация о временно зарегистрированных
        public const int act_report_sobsv = 333; //информация о собственниках
        public const int act_report_voenkomat = 334; //для военкомата
        public const int act_report_vip_kvar = 335; //выписка на жилое помещение
        public const int act_report_vip_dom_gas = 336; //выписка для гор газа


        public const int    act_aa_showuk       = 721;   //разрез: УК
        public const int    act_aa_showbd       = 722;   //разрез: Банки данных
        public const int    act_aa_showul       = 723;   //разрез: Улицы

        public const int    act_as_showsupp     = 724;   //разрез: Поставщики
        public const int    act_as_showserv     = 725;   //разрез: Услуги
        public const int    act_as_showuk       = 726;   //разрез: УК

        public const int    act_findls          = 501;   //ШП по адресам
        public const int    act_findprm         = 502;   //ШП по параметрам
        public const int    act_findch          = 503;   //ШП по начислениям
        public const int    act_findgil3        = 504;   //ШП жильцам                                 
        public const int    act_findpu          = 505;   //ШП показаний ПУ
        public const int    act_findnedop       = 506;   //ШП по недопоставкам
        public const int    act_findodn         = 507;   //ШП ОДН
        public const int    act_findserv        = 508;   //ШП по услугам и поставщикам
        public const int act_findsupg           = 509;   //ШП по Заявкам СУПГ                                 
        public const int act_findgroup          = 510;   //ШП по группам              
        public const int act_findpack           = 511;   //ШП по оплатам
        public const int act_findplannedworks   = 512;   //ШП по плановым работам

        //public const int    act_AdresEE         = 508;   //Адреса Энергосбыта

        public const int act_mode_edit          = 611;   //"Открыть данные на изменение"
        public const int act_mode_view          = 610;   //"Открыть данные на просмотр"
        public const int act_online             = 530;   //"Сейчас на сайте"
        public const int act_blocked            = 531;   //"Заблокированные пользователи"

        public const int act_process_in_queue   = 532;   //Процессы в очереди
        public const int act_process_active     = 533;   //Запущенные
        public const int act_process_finished   = 534;   //Завершенные
        public const int act_process_with_errors= 535;   //С ошибками
                                                
        public const int rowsview_20            = 701;   //"Выводить по 20 записей"
        public const int rowsview_50            = 702;   //"Выводить по 50 записей"
        public const int rowsview_100           = 703;   //"Выводить по 100 записей"
        public const int rowsview_10            = 705;   //"Выводить по 10 записей"
        public const int act_ubil               = 851;   //  Показать убывших
        public const int act_actual             = 852;   //  Показать историю
        public const int act_neuch              = 853;   //  Показать неучитываемые
        public const int act_arx                = 854;   //  Показать архивные карточки

        public const int act_open_gilkart       = 861;   //  Открыть карточку жильца
        public const int act_open_gilper        = 862;   //  Открыть периоды временного убытия жильца
        public const int act_open_dossier       = 863;   //  открыть досье на жильца
                                 
        public const int role_sql               = 101;   //  
        public const int role_sql_wp            = 101;   //  
        public const int role_sql_area          = 102;   //  
        public const int role_sql_geu           = 103;   //  
        public const int role_sql_subrole       = 105;   // роль с дополнительным функционалом к выбранной роли / подсистеме
        public const int role_sql_prm           = 106;   // ограничение по параметрам
        public const int role_sql_ext           = 107;   // расширение роли дополнительными ролями
        public const int role_sql_supp          = 120;   //  
        public const int role_sql_serv          = 121;   //
        public const int role_sql_bank          = 122;   //
        public const int role_sql_payer         = 123;   //
        public const int role_sql_server        = 124;   //  ограничение по серверу
        public const int role_sql_town = 125;

        public const int role_ext_mm            = 201;   //  ExtJS MainMenu
        public const int role_ext_pm            = 202;   //  ExtJS подпункты
        public const int role_valid_srv         = 210;   //  разрешение на выполнение rest-сервиса
        public const int role_invalid_id        = 211;   //  закрытые ext_ID
        
        //Коды ошибок вызова BARS-сервисов 
        public const int svc_normal     =  0;    
        public const int svc_wrongdata  = -1;   
        public const int svc_sqlerror   = -2;   
        public const int svc_pk_Format  = -3;   
        public const int svc_pk_Prefix  = -4;   
        public const int svc_pk_NumLs   = -5;   
        public const int svc_pk_Bit     = -6;
        public const int svc_pk_NotUk   = -7;
        public const int svc_pk_NotLs   = -8;   

        //для автоматического обновления
        public const string encryptpathkey    = "zRZ1/BHciushX1T9Ks5WwdRNjgjEEGzFypuKZTmkAOE=";//ключ для шифрования пути к архиву
        public const string encryptrequestkey = "4PSckw3IcgQ/at00qGJ2RPcvDvmr=UfjcSm64cXLycw";//ключ для шифрования запроса

        public static string[] svc_errors = new string[]
        {
        /*svc_normal     =  0 = */ "нормальное завершение",
        /*svc_data       = -1 = */ "неверные входные данные",
        /*svc_sql        = -2 = */ "ошибка выборки данных",
        /*svc_pk_Format  = -3 = */ "неверный формат лицевого счета",
        /*svc_pk_Prefix  = -4 = */ "неверный префикс УК в лицевом счете",
        /*svc_pk_NumLs   = -5 = */ "неверный номер лицевого счета",
        /*svc_pk_Bit     = -6 = */ "неверный контрольный бит в лицевом счете",
        /*svc_pk_NotUk   = -7 = */ "данный лицевой счет не обслуживается",
        /*svc_pk_NotLs   = -8 = */ "лицевой счет не определен в базе данных"
        };

        public const int getInfo_supp = 1;
        public const int getInfo_area = 2;   

        public static int RowsGrid(string rowsview)
        {
            int i = 20;//число отображаемых записей по умолчанию
            int rw = 0;
            int.TryParse(rowsview, out rw);
            switch (rw)
            {
                case Constants.rowsview_10: i = 10; break;
                case Constants.rowsview_20:  i = 20; break;
                case Constants.rowsview_50:  i = 50; break;
                case Constants.rowsview_100: i = 100; break;

            }
            return i;
        }

    }
    //----------------------------------------------------------------------
    static public class Utils //утилиты
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        static public string RunFile (string rf)
        //----------------------------------------------------------------------
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = rf;
            proc.EnableRaisingEvents = true;

            //proc.Exited += new EventHandler(proc_Exited);

            try
            {
                proc.Start();
                proc.WaitForExit();

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        //----------------------------------------------------------------------
        static public Returns InitReturns() //инициализация переменной Returns
        //----------------------------------------------------------------------
        {
            Returns ret;
            ret.result = true;
            ret.text   = "";
            ret.tag    = 0;
            ret.sql_error = "";
            return ret;
        }

        //----------------------------------------------------------------------
        static public void UserLogin(string cons_User, out string Login, out string Password) //вытащить логины
        //----------------------------------------------------------------------
        {
            int l = cons_User.Length;
            int k = cons_User.LastIndexOf(";");

            string[] result = cons_User.Split(new string[] { ";" }, StringSplitOptions.None);

            try
            {
                Login    = result[0].Trim();
                Password = result[1].Trim();
            }
            catch
            {
                Login = "";
                Password = "";
            }
        }
        //----------------------------------------------------------------------
        static public string IfmxDatabase(string st) //данные коннекта
        //----------------------------------------------------------------------
        {
            string srv = "";
            string bds = "";
            string usr = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("Database") & (!zap.StartsWith("Database L") ))
                {
                    try
                    {
                        srv = result2[1];
                    }
                    catch
                    {
                    }
                }
                else
                    if (zap.StartsWith("Server"))
                    {
                        try
                        {
                            bds = result2[1];
                        }
                        catch
                        {
                        }
                    }
                    else
                        if (zap.StartsWith("UID"))
                        {
                            try
                            {
                                usr = result2[1];
                            }
                            catch
                            {
                            }
                        }
            }

            return bds.Trim() + "@" + srv.Trim() + "  (" + usr.Trim() + ")";
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbDatabase(string st) //данные коннекта FireBird
        //----------------------------------------------------------------------
        {
            string srv = "";
            string bds = "";
            string usr = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if ( zap.StartsWith("data source") )
                {
                    try
                    {
                        srv = result2[1];
                    }
                    catch
                    {
                    }
                }
                else
                    if (zap.StartsWith("initial catalog"))
                    {
                        try
                        {
                            bds = result2[1];
                        }
                        catch
                        {
                        }
                    }
                    else
                        if (zap.StartsWith("user id"))
                        {
                            try
                            {
                                usr = result2[1];
                            }
                            catch
                            {
                            }
                        }
            }

            return bds.Trim() + ":" + srv.Trim() + "  (" + usr.Trim() + ")";
        }


        //----------------------------------------------------------------------
        static public string GetCorrectFIO(string st)
        //----------------------------------------------------------------------
        {
            if (st.Trim() == "") return st;

            //string[] masStr = st.Split(" ", StringSplitOptions.None);
            char[] delimiterChars = { ' ', ',', '.', ':' };
            string[] masStr = st.Split(delimiterChars);

            int i = 1;
            StringBuilder resStr = new StringBuilder();

            foreach (string st_ in masStr)
            {
                switch (i)
                {
                    case 1: resStr.Append(st_.Trim());
                        break;
                    case 2:
                        if (st_.Trim().Length>1) resStr.Append(" " + st_.Trim().Substring(0, 1) + ".");
                        break;
                    case 3: if (st_.Trim().Length > 1) resStr.Append(" " + st_.Trim().Substring(0, 1) + ".");
                        break;
                    default:
                        break;
                }
                i++;
            }
            if (i != 4) return st.Trim();
            else return resStr.ToString();

        }

        //----------------------------------------------------------------------
        static public string IfmxGetPref(string kernel) //вытащить префикс
        //----------------------------------------------------------------------
        {
            if (kernel == null) return "";
            int k, l;
            k = kernel.LastIndexOf("_kernel");
            if ((k-9)>0) //вызов из ConnectionString
            {
                string s;
                k = kernel.LastIndexOf("_kernel");
                s = kernel.Substring(k - 9, 9);
                l = s.Length;
                k = s.LastIndexOf("=");
                return (s.Substring(k + 1, l - k - 1)).Trim();
            }
            else  //вызов из названии
            {
                return (kernel.Substring(0, k)).Trim();
            }
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbInitialCatalog(string st) //вытащить исходный каталог
        //----------------------------------------------------------------------
        {
            string dir = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("initial catalog"))
                {
                    try
                    {
                        dir = result2[1];
                        break;
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            if (dir == "") return "";

            string[] result3 = dir.Split(new string[] { "\\\\" }, StringSplitOptions.None);
            string ndir = "";
            int l = result3.Length;

            foreach (string zap in result3)
            {
                if (l != 1) ndir += zap + "\\\\";
                l -= 1;
            }

            return ndir;
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbChangeDir(string st, string pref) //заменить путь к базе на pref
        //----------------------------------------------------------------------
        {
            string dir = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("initial catalog"))
                {
                    try
                    {
                        dir = result2[1];
                        break;
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            if (dir == "") return "";

            string s = st.Replace(dir, pref);
            return s;
        }
        //----------------------------------------------------------------------
        static public bool ValInString (string st_in, string st_val, string st_split) 
        //----------------------------------------------------------------------
        {
            string[] result = st_in.Split(new string[] { st_split }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                if (zap.Trim() == st_val.Trim())
                    return true;
            }

            return false;
        }
        //----------------------------------------------------------------------
        static public bool IsInRoleVal (List<_RolesVal> RolesVal, string val, int tip, int kod)
        //----------------------------------------------------------------------
        {
            bool b = true; //по-умолчанию разрешаем доступ

            if (RolesVal != null)
                if (RolesVal.Count > 0)
                {

                    foreach (_RolesVal role in RolesVal)
                    {
                        if (role.tip == tip & role.kod == kod)
                        {
                            b = false; //ограничения определены

                            if (ValInString(role.val, val, ",")) return true;
                        }
                    }
                }

            return b; 
        }
        //----------------------------------------------------------------------
        public static string IfmxFormatDatetimeToHour(string datahour, out Returns ret)
        //----------------------------------------------------------------------
        {
            //привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч"
            ret = new Returns(false);
            string outs = "";

            if (String.IsNullOrEmpty(datahour))
            {
                return outs;
            }

            datahour = datahour.Trim();

            string[] mas1 = datahour.Split(new string[] { " " }, StringSplitOptions.None);

            string dt = "";
            string hm = "";
            try
            {
                dt = mas1[0].Trim(); 
                hm = mas1[1].Trim();

                if (String.IsNullOrEmpty(dt) || String.IsNullOrEmpty(hm))
                {
                    return outs;
                }

                string[] mas2 = dt.Split(new string[] { "." }, StringSplitOptions.None);
                string[] mas3 = hm.Split(new string[] { ":" }, StringSplitOptions.None);

                outs = mas2[2].Trim() + "-" + mas2[1].Trim() + "-" + mas2[0].Trim() + " " + mas3[0].Trim();
                ret.result = true;
            }
            catch
            {
                return outs;
            }

            return outs;
        }
        //----------------------------------------------------------------------
        public static bool is_UNDEF_(string s) //
        //----------------------------------------------------------------------
        {
            return s == Constants._UNDEF_;
        }
        //----------------------------------------------------------------------
        public static string blank_UNDEF_(string s)
        //----------------------------------------------------------------------
        {
            if (is_UNDEF_(s))
                return "";
            else
                return s;
        }

        //----------------------------------------------------------------------
        /// <summary> Подготовка строки для вставки в SQL-запрос (экранирование символов, добавление внешних кавычек)
        /// </summary>
        public static string EStrNull(string s)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, 255, "NULL");
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, byte l)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, l, "NULL");
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, string defaultValue)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, 255, defaultValue);
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, byte l, string defaultValue)
        //----------------------------------------------------------------------
        {
            if (s == null) s = "";
            else s = s.Trim();
            if (s == "")
            {
                if (defaultValue.ToUpper() == "NULL")
                    return " " + defaultValue + " ";
                else s = defaultValue;
            }
            if (s.Length > l) s = s.Substring(0, l);
#if PG
            return "'" + s.Replace("'", "\"") + "'";
#else
            return "'" + s.Replace("'", "''") + "'";
#endif
        }

        //----------------------------------------------------------------------
        public static int EInt0(string s)
        //----------------------------------------------------------------------
        {
            try
            {
                int i;
                int.TryParse(s, out i);
                return i;
            }
            catch
            {
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public static long ELong0(string s)
        //----------------------------------------------------------------------
        {
            try
            {
                long i;
                Int64.TryParse(s, out i);
                return i;
            }
            catch
            {
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public static string ENull(string s)
        //----------------------------------------------------------------------
        {
            if (s == null)
                return "";
            else return s.Trim();
        }
        //----------------------------------------------------------------------
        public static string EFlo0(decimal f)
        //----------------------------------------------------------------------
        {
            return EFlo0(f.ToString(), "0.00");
        }
        //----------------------------------------------------------------------
        public static string EFlo0(string f)
        //----------------------------------------------------------------------
        {
            return EFlo0(f, "0.00");
        }
        //----------------------------------------------------------------------
        public static string EFlo0(string f, string _default)
        //----------------------------------------------------------------------
        {
            if (f.Trim() == "")
                return _default;
            else
            {
                NumberFormatInfo nfi = new CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";
                double d = Double.Parse(f.Replace(",", ".").Replace(" ",""), NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign, nfi);
                return d.ToString("G", nfi);
            }
        }
        //----------------------------------------------------------------------
        public static string FormatDate(string d)
        //----------------------------------------------------------------------
        {
            try
            {
                DateTime dt = DateTime.ParseExact(d, "dd.MM.yyyy", new CultureInfo("ru-RU"));
                return String.Format("{0:dd.MM.yyyy}", dt);
            }
            catch
            {
                return "";
            }
        }

        //----------------------------------------------------------------------
        public static string EDateNull(string d)
        //----------------------------------------------------------------------
        {
            if ((d == null) || (d.Trim() == ""))
            {
                return "null";

            }
            else
            {
                return "'" + d.Trim() + "'";
            }
        }

        //----------------------------------------------------------------------
        public static string FormatDateMDY(string d)
        //----------------------------------------------------------------------
        {
            try
            {
                DateTime dt = DateTime.ParseExact(d, "dd.MM.yyyy", new CultureInfo("ru-RU"));
                return "mdy("+String.Format("{0:MM,dd,yyyy}", dt) +")";
            }
            catch
            {
                return "";
            }
        }

        /// <summary> проверяет наличие параметра в строке параметров, а также определяет порядковый номер параметра
        /// </summary>
        public static bool GetParams(string prms, int p)
        {
            int num;
            return GetParams(prms, p.ToString(), out num);
        }
        public static bool GetParams(string prms, string p)
        {
            int num;
            return GetParams(prms, p, out num);
        }
        public static bool GetParams(string prms, int p, out int sequenceNumber)
        {
            return GetParams(prms, p.ToString(), out sequenceNumber);
        }
        public static bool GetParams(string prms, string p, out int sequenceNumber)
        {
            sequenceNumber = -1;
            if (prms == null) return false;

            //Regex reg = new Regex(@",\d+", RegexOptions.IgnoreCase);
            Regex reg = new Regex(@"[^,]+", RegexOptions.IgnoreCase);

            foreach (Match match in reg.Matches(prms))
            {
                sequenceNumber++;
                if (match.Value == p) return true;
            }
            return false;
        }

        /// <summary>Удаляет параметр из строки параметров
        /// </summary>
        /// <param name="prms">Строка с параметрами в формате ,p1,p2,p3,... </param>
        /// <param name="p">Удаляемый параметр</param>
        /// <returns>Измененная строка параметров</returns>
        public static string RemoveParam(string prms, string p)
        {
            if (prms == null) return prms;

            string[] arr = prms.Split(',');
            string res = "";

            for (int i=0; i < arr.Length; i++)
            {
                if (arr[i] != p) res += "," + arr[i];
            }
            return res;
        }

        public class CssClasses
        {
            private List<string> _classes;
            private int _count;
            
            public CssClasses()
            {
                _classes = new List<string>();
                _count = 0;
            }

            public CssClasses(string classes)
            {
                string[] cls = classes.Trim().Split(' ');
                _classes = cls.ToList<string>();
                _count = _classes.Count;
            }

            public CssClasses AddClass(string className)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_classes[i] == className) return this;
                }
                _classes.Add(className);
                _count++;
                return this;
            }

            public CssClasses RemoveClass(string className)
            {
                if (className != "")
                {
                    while (_classes.IndexOf(className) >= 0) _classes.Remove(className);
                }
                _count = _classes.Count;
                return this;
            }

            public override string ToString()
            {
                string result = "";
                for (int i = 0; i < _count; i++)
                {
                    result += " " + _classes[i];
                }
                return result.Trim();
            }
        }

        //----------------------------------------------------------------------
        public static int PutIdMonth(int y, int m)
        //----------------------------------------------------------------------
        {
            int i = 0;
            int.TryParse(y.ToString() + m.ToString("00"), out i);
            return i;
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(int id, ref int y, ref int m)
        //----------------------------------------------------------------------
        {
            GetIdMonth(id.ToString(), ref y, ref m);
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(string id, ref int y, ref int m)
        //----------------------------------------------------------------------
        {
            try
            {
                y = Convert.ToInt32( id.Substring(0, 4) );
                m = Convert.ToInt32( id.Substring(4, 2) );
            }
            catch
            {
                y = 0; 
                m = 0;
            }
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(string id, ref RecordMonth rm)
        //----------------------------------------------------------------------
        {
            try
            {
                rm.year_  = Convert.ToInt32(id.Substring(0, 4));
                rm.month_ = Convert.ToInt32(id.Substring(4, 2));
            }
            catch
            {
                rm.year_  = 0;
                rm.month_ = 0;
            }
        }
        //----------------------------------------------------------------------
        public static int GetInt(string s)
        //----------------------------------------------------------------------
        {
            if (s == "")
                return 0;
            else
            {
                int i;
                try
                {
                    i = Convert.ToInt32(s);
                    return i;
                }
                catch  { }

                int l = s.Length;
                int k = 1;
                while (k < l)
                {
                    s = s.Substring(0, l - k);
                    try
                    {
                        i = Convert.ToInt32(s);
                        return i;
                    }
                    catch { }

                    k = k + 1;
                }
                return 0;
            }
        }
        //----------------------------------------------------------------------
        // Поиск и выдача целого значения из строкового списка (pFindString) значений разделенных 
        // символьной строкой (pDelimiter) по позиции в списке (pPositionInSpis)
        public static Returns FindKeyFromSpis(string pFindString, string pDelimiter, int pPositionInSpis, out long nzp)
        //----------------------------------------------------------------------
        {
            Returns oResult;

            oResult.result = false;
            oResult.text = string.Empty;
            oResult.tag = 0;
            oResult.sql_error = "";
            nzp = 0;

            if (pPositionInSpis >= 0)
            {
                MatchCollection myFind = Regex.Matches(pFindString, pDelimiter, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                int iposN = 0; int iposK = 0; int iCount = 0;
                foreach (Match nextFind in myFind)
                {
                    iposK = nextFind.Index;
                    if (iCount == pPositionInSpis)
                    {
                        oResult.result = true;
                        oResult.text = pFindString.Substring(iposN, iposK - iposN);
                        break;
                    }
                    iCount++; iposN = iposK + 1;
                }
                int.TryParse(oResult.text, out oResult.tag);
                Int64.TryParse(oResult.text, out nzp);
            }
            return oResult;
        }
        //----------------------------------------------------------------------
        // Поиск и выдача целого значения из строкового списка (pFindString) значений разделенных 
        // символьной строкой (pDelimiter) по позиции в списке (pPositionInSpis)
        public static Returns FindKeyFromSpis(string pFindString, string pDelimiter, int pPositionInSpis)
        //----------------------------------------------------------------------
        {
            long nzp;
            return FindKeyFromSpis(pFindString, pDelimiter, pPositionInSpis, out nzp);
        }
        //----------------------------------------------------------------------
        // Поиск и выдача целого значения из строкового списка (pFindString) значений разделенных 
        // символьной строкой (pDelimiter) по позиции в списке (pPositionInSpis)
        public static Returns FindPosFromSpis(string pFindString, string pDelimiter, long pValInSpis)
        //----------------------------------------------------------------------
        {
            Returns oResult;

            oResult.result = false;
            oResult.text = string.Empty;
            oResult.sql_error = "";
            oResult.tag = 0;

            MatchCollection myFind = Regex.Matches(pFindString, pDelimiter, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            int iposN = 0; int iposK = 0; int iCount = 0;
            foreach (Match nextFind in myFind)
            {
                iposK = nextFind.Index;
                if (pFindString.Substring(iposN, iposK - iposN) == pValInSpis.ToString())
                {
                    oResult.result = true;
                    oResult.tag = iCount;
                    break;
                }
                iCount++; iposN = iposK + 1;
            }
            return oResult;
        }

        //----------------------------------------------------------------------
        public static string GetSN(string sn)
        //----------------------------------------------------------------------
        {
            string[] result = sn.Split(new string[] { "-" }, StringSplitOptions.None);
            sn = "";
            for (int i = 0; i < result.Length; i = i + 1)
            {
                sn += result[i];
            }

            return (sn.Trim()).ToUpper();
        }
        //----------------------------------------------------------------------
        public static Int64 BarcodeCRC10(string barcode)
        //----------------------------------------------------------------------
        {
            char c = Convert.ToChar("0");
            barcode = barcode.PadLeft(9, c);

            int sum = 0;
            string s = "";
            for (int i = 1; i <= barcode.Length; i++)
            {
                s = barcode.Substring(i - 1, 1);
                if (i != 10)
                {
                    if ( i % 2 == 0 )
                        sum = sum + 3 * Convert.ToInt32(s);
                    else
                        sum = sum + Convert.ToInt32(s);
                }
            }
            s = barcode.Trim() + Convert.ToString((10 - sum % 10) % 10);
            
            return Convert.ToInt64(s);
        }


        //----------------------------------------------------------------------
        public static Int64 BarcodeCRC13(string barcode)
        //----------------------------------------------------------------------
        {
            char c = Convert.ToChar("0");
            barcode = barcode.PadLeft(12, c);

            int sum = 0;
            string s = "";
            for (int i = 0; i < barcode.Length; i++)
            {
                s = barcode.Substring(i, 1);
                if (i != 12)
                {
                    if (i % 2 == 0)
                        sum = sum + 3 * Convert.ToInt32(s);
                    else
                        sum = sum + Convert.ToInt32(s);
                }
            }
            s = barcode.Trim() + Convert.ToString((10 - sum % 10) % 10);

            return Convert.ToInt64(s);
        }

        //----------------------------------------------------------------------
        public static long EncodePKod(string kod_erc, int num_ls)
        //----------------------------------------------------------------------
        {
            string s =num_ls.ToString();
            char c = Convert.ToChar("0");
            s = s.PadLeft(9 - kod_erc.Length, c);

            return BarcodeCRC10(kod_erc.ToString() + s);
        }
        //----------------------------------------------------------------------
        public static Returns DecodePKod(string pkod, out int kod_erc, out int num_ls)
        //----------------------------------------------------------------------
        {   
            Returns ret = Utils.InitReturns();
            ret.result = false;
            kod_erc = 0;
            num_ls  = 0;

            if (pkod.Length != 10)
            {
                ret.tag    = Constants.svc_pk_Format;
                return ret;
            }

            long l;
            if (!Int64.TryParse(pkod, out l))
            {
                ret.tag    = Constants.svc_pk_Format;
                return ret;
            }


            string s = pkod.Substring(0,1);
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (s == "3") //Челны или НКамск
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                if (!int.TryParse(pkod.Substring(0, 2), out kod_erc))
                {
                    ret.tag = Constants.svc_pk_Prefix;
                }
                else
                {
                    if (!int.TryParse(pkod.Substring(2, 7), out num_ls))
                    {
                        ret.tag = Constants.svc_pk_NumLs;
                    }
                }
            }
            else 
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (s == "2") //Казань, РТ
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                if (!int.TryParse(pkod.Substring(0, 3), out kod_erc))
                {
                    ret.tag = Constants.svc_pk_Prefix;
                }
                else
                {
                    if (!int.TryParse(pkod.Substring(3, 6), out num_ls))
                    {
                        ret.tag = Constants.svc_pk_NumLs;
                    }
                }
            }
            else 
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (s == "5") //Лайтовцы
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                if (!int.TryParse(pkod.Substring(0, 4), out kod_erc))
                {
                    ret.tag = Constants.svc_pk_Prefix;
                }
                else
                {
                    if (!int.TryParse(pkod.Substring(4, 5), out num_ls))
                    {
                        ret.tag = Constants.svc_pk_NumLs;
                    }
                }
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            else //не определен префикс
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                ret.tag = Constants.svc_pk_Prefix;


            if (ret.tag < 0)
            {
                return ret;
            }

            int k = 0;
            if (!int.TryParse(pkod.Substring(9, 1), out k))
            {
                ret.tag  = Constants.svc_pk_Bit;
                return ret;
            }

            Int64 test = EncodePKod(kod_erc.ToString(), num_ls);

            if (test.ToString() != pkod)
            {
                ret.tag  = Constants.svc_pk_Bit;
                return ret;
            }

            return Utils.InitReturns();;
        }
        //----------------------------------------------------------------------
        public static string GetKontrSamara(string als)
        //----------------------------------------------------------------------
        {

            int sum_mod = 0;
            int i;
            int j;
            int first_k;
            int second_k;
            string ss;

            for (i = 0; i < als.Length; i++)
            {
                ss = als.Substring(i, 1);
                j = Convert.ToInt32(ss);
                if (i % 2 == 0)
                {
                    switch (j)
                    {
                        case 0: sum_mod = sum_mod + 4; break;
                        case 1: sum_mod = sum_mod + 6; break;
                        case 2: sum_mod = sum_mod + 8; break;
                        case 3: sum_mod = sum_mod + 1; break;
                        case 4: sum_mod = sum_mod + 3; break;
                        case 5: sum_mod = sum_mod + 5; break;
                        case 6: sum_mod = sum_mod + 7; break;
                        case 7: sum_mod = sum_mod + 9; break;
                        case 8: sum_mod = sum_mod + 2; break;
                        case 9: sum_mod = sum_mod + 0; break;
                    }
                }
                else sum_mod = sum_mod + j;
            }
            first_k = (10 - sum_mod % 10) % 10;

            sum_mod = 0;

            for (i = 0; i < Math.Min(11, als.Length); i++)
            {
                j = Convert.ToInt16(als.Substring(i, 1));
                switch (i + 1)
                {
                    case 1: sum_mod = sum_mod + j * 6; break;
                    case 2: sum_mod = sum_mod + j * 5; break;
                    case 3: sum_mod = sum_mod + j * 4; break;
                    case 4: sum_mod = sum_mod + j * 3; break;
                    case 5: sum_mod = sum_mod + j * 2; break;
                    case 6: sum_mod = sum_mod + j * 1; break;
                    case 7: sum_mod = sum_mod + j * 1; break;
                    case 8: sum_mod = sum_mod + j * 2; break;
                    case 9: sum_mod = sum_mod + j * 3; break;
                    case 10: sum_mod = sum_mod + j * 4; break;
                    case 11: sum_mod = sum_mod + j * 5; break;
                }
            }

            second_k = (10 - (sum_mod % 10)) % 10;

            return first_k.ToString() + second_k.ToString();

        }

        //----------------------------------------------------------------------
        /// <summary> Установить региональные настройки
        /// </summary>
        public static void setCulture()
        //----------------------------------------------------------------------
        { 
            CultureInfo culture = new CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            culture.DateTimeFormat.ShortTimePattern = "HH:mm:ss";
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }

        public static DataTable ConvertDBFtoDataTable(System.IO.Stream fs, out Returns ret)
        {
            return ConvertDBFtoDataTable(fs, "", out ret);
        }

        public static DataTable ConvertDBFtoDataTable(System.IO.Stream fs, string codePage, out Returns ret)
        {
            //Описание формата DBF файлов:
            //  1. http://www.hardline.ru/3/36/687/
            //  2. http://articles.org.ru/docum/dbfall.php
            //      FoxBASE+/dBASE III +, без memo - 0х03
            //  3. http://ru.wikipedia.org/wiki/DBF
            DataTable dt = new DataTable();
            ret = InitReturns();

            int tag = 0;
            try
            {
                // определение кодировки файла
                byte[] buffer = new byte[1];
                fs.Position = 0x00;
                fs.Read(buffer, 0, buffer.Length);
                if (buffer[0] != 0x03)
                {
                    ret.result = false;
                    ret.text = "Данный формат DBF файла не поддерживается";
                    ret.tag = -1;
                    return null;
                }

                // определение кодировки файла (взято из http://ru.wikipedia.org/wiki/DBF)
                Encoding encoding;
                if (codePage == "866") encoding = Encoding.GetEncoding(866);
                else if (codePage == "1251") encoding = Encoding.GetEncoding(1251);
                else
                {
                    buffer = new byte[1];
                    fs.Position = 0x1D;
                    fs.Read(buffer, 0, buffer.Length);
                    if (buffer[0] != 0x65 &&    //Codepage_866_Russian_MSDOS
                        buffer[0] != 0x26 &&    //кодовая страница 866 DOS Russian
                        buffer[0] != 0xC9 &&    //Codepage_1251_Russian_Windows
                        buffer[0] != 0x57)    //кодовая страница 1251 Windows ANSI
                    {
                        ret = new Returns(false, "Кодовая страница не задана или не поддерживается", -1);
                        return null;
                    }
                    if (buffer[0] == 0x65 || buffer[0] == 0x26)
                        encoding = Encoding.GetEncoding(866);
                    else
                        encoding = Encoding.GetEncoding(1251);
                }
                
                buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
                fs.Position = 4;
                fs.Read(buffer, 0, buffer.Length);
                int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
                buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
                fs.Position = 8;
                fs.Read(buffer, 0, buffer.Length);
                int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
                string[] FieldName = new string[FieldCount]; // Массив названий полей
                string[] FieldType = new string[FieldCount]; // Массив типов полей
                byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
                byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
                buffer = new byte[32 * FieldCount]; // Описание полей: 32 байтa * кол-во, начиная с 33-го
                fs.Position = 32;
                fs.Read(buffer, 0, buffer.Length);
                int FieldsLength = 0;
                DataColumn col;
                for (int i = 0; i < FieldCount; i++)
                {
                    // Заголовки
                    FieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
                    FieldType[i] = "" + (char)buffer[i * 32 + 11];
                    FieldSize[i] = buffer[i * 32 + 16];
                    FieldDigs[i] = buffer[i * 32 + 17];
                    FieldsLength = FieldsLength + FieldSize[i];
                    // Создаю колонки
                    switch (FieldType[i])
                    {
                        case "L": dt.Columns.Add(FieldName[i], Type.GetType("System.Boolean")); break;
                        case "D": dt.Columns.Add(FieldName[i], Type.GetType("System.DateTime")); break;
                        case "N":
                            {
                                if (FieldDigs[i] == 0)
                                    dt.Columns.Add(FieldName[i], Type.GetType("System.Int32"));
                                else
                                {
                                    col = new DataColumn(FieldName[i], Type.GetType("System.Decimal"));
                                    col.ExtendedProperties.Add("precision", FieldSize[i]);
                                    col.ExtendedProperties.Add("scale", FieldDigs[i]);
                                    col.ExtendedProperties.Add("length", FieldSize[i] + FieldDigs[i]);
                                    dt.Columns.Add(col);
                                }
                                break;
                            }
                        case "F": dt.Columns.Add(FieldName[i], Type.GetType("System.Double")); break;
                        default:
                            col = new DataColumn(FieldName[i], Type.GetType("System.String"));
                            col.MaxLength = FieldSize[i];
                            dt.Columns.Add(col);
                            break;
                    }
                }
                fs.ReadByte(); // Пропускаю разделитель схемы и данных
                System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("ru-RU", false).DateTimeFormat;
                System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";


                buffer = new byte[FieldsLength];
                dt.BeginLoadData();
                //fs.ReadByte(); // Пропускаю стартовый байт элемента данных
                int delPriznak = 0;
                char[] numericValidChars = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
                                        
                for (int j = 0; j < RowsCount; j++)
                {

                    delPriznak = fs.ReadByte(); // Пропускаю стартовый байт элемента данных

                    fs.Read(buffer, 0, buffer.Length);
                    System.Data.DataRow R = dt.NewRow();
                    int Index = 0;



                    for (int i = 0; i < FieldCount; i++)
                    {

                        string l = encoding.GetString(buffer, Index, FieldSize[i]).TrimEnd(new char[] { (char)0x00, (char)0x20 });
                        Index = Index + FieldSize[i];

                        if (l != "")
                            switch (FieldType[i])
                            {
                                case "L": R[i] = l == "T" ? true : false; break;
                                case "D":
                                    try
                                    {
                                        R[i] = DateTime.ParseExact(l, "yyyyMMdd", dfi);
                                    }
                                    catch
                                    {
                                        tag = -1;
                                        throw new Exception("Ожидалась дата в формате ГГГГММДД в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + l);
                                    }
                                    break;
                                case "N":
                                    {
                                        l = l.Trim().Replace(",", ".");
                                        string val = "";
                                        foreach (char c in l.ToCharArray()) if (numericValidChars.Contains(c)) val += c; else break;

                                        if (FieldDigs[i] == 0)
                                        {
                                            try
                                            {
                                                R[i] = int.Parse(val, nfi);
                                            }
                                            catch
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось целое число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + val);
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                R[i] = decimal.Parse(val, nfi);
                                            }
                                            catch
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось вещественное число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + val);
                                            }
                                        }
                                        break;
                                    }
                                case "F": R[i] = double.Parse(l.Trim(), nfi); break;
                                default: R[i] = l; break;
                            }
                        else
                            R[i] = DBNull.Value;
                    }
                    if (delPriznak == 32)
                        dt.Rows.Add(R);
                }
                dt.EndLoadData();
                fs.Close();
                return dt;
            }
            catch (Exception e)
            {
                ret.result = false;
                if (tag < 0) ret.text = e.Message;
                else ret.text = "Ошибка конвертации DBF файла";
                ret.tag = tag;
                MonitorLog.WriteLog("Ошибка конвертации DBF файла: " + e.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        static public string GetSupgCurDate(string DFormat, string TFormat)
        //----------------------------------------------------------------------
        {
            string rStr = "";
            if (Points.IsDemo)
            {
                if (DFormat == "D") rStr = System.Convert.ToDateTime(Points.DateOper.Date).ToString("dd.MM.yyyy");
                if (DFormat == "T") rStr = System.Convert.ToDateTime(Points.DateOper.Date).ToString("yyyy-MM-dd");
                if (DFormat == "F") rStr = System.Convert.ToDateTime(Points.DateOper.Date).ToString("yyyy_MM_dd");
            }
            else
            {
                if (DFormat == "D") rStr = System.Convert.ToDateTime(DateTime.Now.ToString()).ToString("dd.MM.yyyy");
                if (DFormat == "T") rStr = System.Convert.ToDateTime(DateTime.Now.ToString()).ToString("yyyy-MM-dd");
                if (DFormat == "F") rStr = System.Convert.ToDateTime(DateTime.Now.ToString()).ToString("yyyy_MM_dd");
            }

            if (TFormat != "")
            {
                if (DFormat != "F")                
                {
                    if (TFormat == "H") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH");
                    if (TFormat == "m") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH:mm");
                    if (TFormat == "s") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH:mm:ss");
                }
                else
                {
                    if (TFormat == "H") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH");
                    if (TFormat == "m") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH_mm");
                    if (TFormat == "s") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH:mm:ss");
                }

            }

            return rStr;

        }
    }

    //Ксласс объекта информации об боновлениях
    [DataContract]
    public class UpData //Данные об обновлении
    {
        double version;
        string Status;
        string Date;
        string TypeUp;
        string path;
        string Key;
        string Soup;
        string Nzp;
        string Web_path;
        string Report;

        [DataMember]
        public double Version
        {
            set { version = value; }
            get { return version; }
        }

        [DataMember]
        public string status
        {
            set { Status = value; }
            get { return Status; }
        }

        [DataMember]
        public string date
        {
            set { Date = value; }
            get { return Date; }
        }

        [DataMember]
        public string typeUp
        {
            set { TypeUp = value; }
            get { return TypeUp; }
        }

        [DataMember]
        public string Path
        {
            set { path = value; }
            get { return path; }
        }

        [DataMember]
        public string key
        {
            set { Key = value; }
            get { return Key; }
        }

        [DataMember]
        public string soup
        {
            set { Soup = value; }
            get { return Soup; }
        }

        [DataMember]
        public string nzp
        {
            set { Nzp = value; }
            get { return Nzp; }
        }

        [DataMember]
        public string web_path
        {
            set { Web_path = value; }
            get { return Web_path; }
        }

        [DataMember]
        public string report
        {
            set { Report = value; }
            get { return Report; }
        }

        public UpData()
        {
            this.version = -1;
            this.Status = "NO DATA";
            this.TypeUp = "NO DATA";
            this.path = "NO DATA";
            this.Key = "NO DATA";
            this.Soup = "NO DATA";
            this.Nzp = "NO DATA";
            this.Web_path = "NO DATA";
            this.Report = "NO DATA";

        }
    }



    public static class WCFParams
    {
//        public static WCFParamsType wcfParams;
        public static WCFParamsType AdresWcfWeb;
        public static WCFParamsType AdresWcfHost;
    }

    public class WCFParamsType //параметры wcf по-умолчанию
    //----------------------------------------------------------------------
    {
        public enum bindWCF
        {
            Pipe,
            TCP
        }
        public enum enBroker
        {
            None,
            Local,
            Server
        }

        public enBroker Broker = enBroker.None;

        public string Adres = "";
        public string BrokerAdres = "";
        public string SupgAdres = "";
        public string HttpAdres = "";
        
        public bool IsCredential = true;
        public string Login = "";
        public string Password = "";

        public int CurT_Server = 0;

        public string srvAdres = "/adres"; //
        public string srvSprav = "/sprav"; //
        public string srvCounter = "/counter"; //
        public string srvCharge = "/charge"; //
        public string srvAdmin = "/admin"; //
        public string srvPrm = "/prm"; //
        public string srvGilec = "/gilec"; //
        public string srvMoney = "/money"; //
        public string srvOdn = "/odn"; //
        public string srvAnaliz = "/analiz"; //
        public string srvNedop = "/nedop";
        public string srvFon = "/fon";
        public string srvServ = "/serv";
        public string srvPatch = "/patch";
        public string srvEditInterData = "/editinterdata";
        public string srvExcel = "/excel";
        public string srvCalcs = "/calcs";
        public string srvSupg = "/supg";
        public string srvPack = "/pack";
        public string srvFnReval = "/fnreval";
        public string srvFaktura = "/faktura";
        public string srvSubsidy = "/subsidy";
        public string srvSubsidyRequest = "/subsidyrequest";
        public string srvSmsMessage = "/smsmessage";
        public string srvMustCalc = "/mustcalc";
        public string srvMulti = "/multi";
        public string srvArchive = "/archive";
        public string srvLicense = "/license";
        public string srvEPasp = "/epasp";
        public string srvNebo = "/nebo";
    }

    #region для универсального сервера отчета

    /// <summary>
    /// класс для параметров отчета
    /// </summary>
    [Serializable]
    public class ReportParams : EntityDescription
    {
        //базовый конструктор
        public ReportParams()
        {
            isSendEmail = false;
            fileType = ReportType.Pdf;
            prmsStr = "";
            param = new List<ReportParameters>();
            dicts = new List<Dict>();
            reportFinder = new ReportFinder();
            prms = new List<int>();
            selectedValues = new Dictionary<int, string>();
        }

        /// <summary>
        /// параметры отчета для шаблона отчета
        /// </summary>
        [DataMember]
        public List<ReportParameters> param { set; get; }

        /// <summary>
        /// справочник выбранных значений
        /// </summary>
        [DataMember]
        public Dictionary<int, string> selectedValues { set; get; }

        /// <summary>
        /// название файла выгружаемого отчета
        /// </summary>
        [DataMember]
        public string fileName { set; get; }

        /// <summary>
        /// идентификатор выгрузки отчета в базе
        /// </summary>
        [DataMember]
        public int nzp { set; get; }

        /// <summary>
        /// объект для работы с переменными программы Комплат
        /// </summary>
        [DataMember]
        public ReportFinder reportFinder { set; get; }

        /// <summary>
        /// наименование dll файла отчета
        /// </summary>
        [DataMember]
        public string dllName { set; get; }

        /// <summary>
        /// время записи отчета в базу
        /// </summary>
        public string date_in { set; get; }

        /// <summary>
        /// {параметр: значение}
        /// </summary>
        public string prmsStr { set; get; }

        /// <summary>
        /// справочники
        /// </summary>
        [DataMember]
        public List<Dict> dicts { set; get; }

        /// <summary>
        /// даты/периоды
        /// </summary>
        [DataMember]
        public List<DatePeriod> datePeriods { set; get; }

        /// <summary>
        /// тип файла отчета
        /// </summary>
        [DataMember]
        public ReportType fileType { set; get; }

        /// <summary>
        /// признак : отослать по email после выгрузки
        /// true: отправить
        /// </summary>
        [DataMember]
        public bool isSendEmail { set; get; }

        /// <summary>
        /// приоритет отчета
        /// </summary>
        [DataMember]
        public int priority { set; get; }

        /// <summary>
        /// идентификатор пользователя
        /// </summary>
        [DataMember]
        public int nzp_user { set; get; }

        /// <summary>
        /// список параметров отчета
        /// </summary>
        [DataMember]
        public List<int> prms { set; get; }

        /// <summary>
        /// комментарий
        /// </summary>
        [DataMember]
        public string comment { set; get; }

        /// <summary>
        /// задача запущена/не запущена
        /// </summary>
        [DataMember]
        public bool isRunned { set; get; }

        /// <summary>
        /// задача
        /// </summary>
        [DataMember]
        public Action<ReportParams> work { set; get; }

        /// <summary>
        /// название шаблона отчета
        /// </summary>
        [DataMember]
        public string ftemplateName { set; get; }

        /// <summary>
        /// путь к шаблону отчета
        /// </summary>
        [DataMember]
        public string ftemplatePath { set; get; }


        /// <summary>
        /// путь к сохранению готового отчета
        /// </summary>
        [DataMember]
        public string exportPath { set; get; }


        /// <summary>
        /// Запускает отчет
        /// </summary>
        public void Execute(ReportParams prm)
        {
            work(prm);
        }

        public void AddParameter(string name, string value)
        {
            ReportParameters p = new ReportParameters(name, value);
            param.Add(p);
        }

        /// <summary>
        /// Приоритет задачи(устанавливается только при создании задачи)
        /// </summary>
        public int getPriority
        {
            get { return priority; }
        }

        /// <summary>
        /// Запущена ли задача(true - запущена, false - стоит в очереди на выполнение)
        /// </summary>
        public bool IsRunned
        {
            get { return isRunned; }
        }

        /// <summary>
        /// получение приоритета
        /// </summary>
        /// <param name="tip"></param>
        /// <returns></returns>
        public static ReportPriority getPriorityEnum(int priority)
        {
            switch (priority)
            {
                case (int)ReportPriority.High: return ReportPriority.High;
                case (int)ReportPriority.Low: return ReportPriority.Low;
                case (int)ReportPriority.Normal: return ReportPriority.Normal;
                default: return ReportPriority.None;
            }
        }
    }

    [Serializable]
    [DataContract]
    public class ReportFinder
    {
        /// <summary>
        /// префикс базы данных
        /// </summary>
        [DataMember]
        public string pref { set; get; }

        /// <summary>
        /// текущий расчетный месяц
        /// </summary>
        [DataMember]
        public int calcMonth { set; get; }

        /// <summary>
        /// текущий расчетный год
        /// </summary>
        [DataMember]
        public int calcYear { set; get; }

        /// <summary>
        /// строка подключения
        /// </summary>
        [DataMember]
        public string connWebString { set; get; }

        /// <summary>
        /// строка подключения
        /// </summary>
        [DataMember]
        public string connKernelString { set; get; }

        /// <summary>
        /// список префиксов БД
        /// </summary>
        [DataMember]
        public List<_Point> pointList { set; get; }
    }


    /// <summary>
    /// параметры отчета
    /// </summary>
    [Serializable]
    [DataContract]
    public class ReportParameters
    {
        /// <summary>
        /// конструктор по умолчанию
        /// </summary>
        public ReportParameters(string pname, string pvalue)
        {
            name = pname;
            value = pvalue;
        }

        /// <summary>
        /// имя/название
        /// </summary>
        [DataMember]
        public string name { set; get; }

        /// <summary>
        /// значение параметра
        /// </summary>
        [DataMember]
        public string value { set; get; }
    }

    /// <summary>
    /// универсальный класс
    /// </summary>
    [Serializable]
    [DataContract]
    public class EntityDescription
    {
        /// <summary>
        /// идентификатор
        /// </summary>
        [DataMember]
        public int id { set; get; }

        /// <summary>
        /// имя/название
        /// </summary>
        [DataMember]
        public string name { set; get; }

        /// <summary>
        /// имя параметра для доступа к значению 
        /// элемента в шаблоне отчета
        /// </summary>
        [DataMember]
        public string paramName { set; get; }

        /// <summary>
        /// текст/лейбл
        /// </summary>
        [DataMember]
        public string label { set; get; }

        /// <summary>
        /// дополнительное описание для пользователя
        /// (пример: выберите значение...)
        /// </summary>
        [DataMember]
        public string text { set; get; }
    }

    /// <summary>
    /// класс справочник
    /// </summary>
    [Serializable]
    [DataContract]
    public class Dict : EntityDescription
    {
        //базовый конструктор
        public Dict()
        {
            extraParam = new ExtraParams();
        }

        /// <summary>
        /// имя спрвочника для отображения(label)
        /// </summary>
        [DataMember]
        public string shortName { set; get; }

        /// <summary>
        /// тип справочника
        /// </summary>
        [DataMember]
        public int type { set; get; }

        /// <summary>
        /// данные справочника
        /// </summary>
        [DataMember]
        public List<DictionaryItem> items { set; get; }

        /// <summary>
        /// признак возможности множественного выбора
        /// </summary>
        [DataMember]
        bool isMultiselect { set; get; }

        /// <summary>
        /// элементы управления справочников
        /// </summary>
        [DataMember]
        public ExtraParams extraParam { set; get; }

        /// <summary>
        /// получение типа
        /// </summary>
        /// <param name="tip"></param>
        /// <returns></returns>
        public ExtraParamEditor getTypeEnum(int type)
        {
            switch (type)
            {
                case (int)ExtraParamEditor.CheckBox: return ExtraParamEditor.CheckBox;
                case (int)ExtraParamEditor.ComboBox: return ExtraParamEditor.ComboBox;
                case (int)ExtraParamEditor.RadioButton: return ExtraParamEditor.RadioButton;
                case (int)ExtraParamEditor.TextBox: return ExtraParamEditor.TextBox;
                default: return ExtraParamEditor.None;
            }
        }
    }

    /// <summary>
    /// класс элемент
    /// </summary>
    [Serializable]
    [DataContract]
    public class DictionaryItem
    {
        /// <summary>
        /// ключ
        /// </summary>
        [DataMember]
        public int key { set; get; }

        /// <summary>
        /// значение
        /// </summary>
        [DataMember]
        public string value { set; get; }

        /// <summary>
        /// признак выбранности элемента
        /// </summary>
        [DataMember]
        public bool isSelect { set; get; }
    }

    /// <summary>
    /// класс даты
    /// </summary>]
    [Serializable]
    [DataContract]
    public class DatePeriod : EntityDescription
    {
        /// <summary>
        /// дата с
        /// </summary>
        [DataMember]
        public DatePeriodItem from { set; get; }

        /// <summary>
        /// дата по
        /// </summary>
        [DataMember]
        public DatePeriodItem to { set; get; }

        /// <summary>
        /// тип
        /// </summary>
        [DataMember]
        public int type { set; get; }

        /// <summary>
        /// формат отображения
        /// </summary>
        [DataMember]
        public string format { set; get; }

        /// <summary>
        /// получение типа
        /// </summary>
        /// <param name="tip"></param>
        /// <returns></returns>
        public static DatePeriodType getTypeEnum(int type)
        {
            switch (type)
            {
                case (int)DatePeriodType.Day: return DatePeriodType.Day;
                case (int)DatePeriodType.Month: return DatePeriodType.Month;
                case (int)DatePeriodType.Quarter: return DatePeriodType.Quarter;
                case (int)DatePeriodType.Week: return DatePeriodType.Week;
                case (int)DatePeriodType.Year: return DatePeriodType.Year;
                default: return DatePeriodType.None;
            }
        }
    }

    /// <summary>
    /// класс элемента класса даты
    /// </summary>
    [Serializable]
    [DataContract]
    public class DatePeriodItem
    {
        /// <summary>
        /// значение
        /// </summary>
        [DataMember]
        public DateTime value { set; get; }

        /// <summary>
        /// значение по умолчанию
        /// </summary>
        [DataMember]
        public DateTime defaultValue { set; get; }
    }

    /// <summary>
    /// дополнительный параметр
    /// </summary>
    [Serializable]
    [DataContract]
    public class ExtraParams
    {
        //базовый конструктор
        public ExtraParams()
        {
            checkBoxes = new CheckBoxEditor();
            textBoxes = new TextBoxEditor();
            radioButtons = new RadioButtonEditor();
            comboBoxes = new ComboBoxEditor();
        }

        /// <summary>
        /// чекбоксы
        /// </summary>
        [DataMember]
        public CheckBoxEditor checkBoxes { set; get; }

        /// <summary>
        /// текстбоксы
        /// </summary>
        [DataMember]
        public TextBoxEditor textBoxes { set; get; }

        /// <summary>
        /// радиобаттоны
        /// </summary>
        [DataMember]
        public RadioButtonEditor radioButtons { set; get; }

        /// <summary>
        /// комбобоксы
        /// </summary>
        public ComboBoxEditor comboBoxes { set; get; }
    }

    /// <summary>
    /// класс элемента чекбокса доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class CheckBoxEditor : EntityDescription
    {
        /// <summary>
        /// значение
        /// </summary>
        [DataMember]
        public string value { set; get; }

        /// <summary>
        /// выбранность по умолчанию
        /// </summary>
        [DataMember]
        public bool isSelect { set; get; }
    }

    /// <summary>
    /// класс элемента текстбокса доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class TextBoxEditor : EntityDescription
    {
        /// <summary>
        /// значение по умолчанию
        /// </summary>
        [DataMember]
        public string defaultValue { set; get; }

        /// <summary>
        /// валидация/маска текстбокса
        /// </summary>
        [DataMember]
        public string validation { set; get; }
    }

    /// <summary>
    /// класс элемента радиобаттона доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class RadioButtonEditor : EntityDescription
    {
        /// <summary>
        /// элементы
        /// bool: true - элемент выбран
        /// string: значение
        /// </summary>
        [DataMember]
        public Dictionary<bool, string> values { set; get; }

        HashSet<string> secondary = new HashSet<string>(/*StringComparer.InvariantCultureIgnoreCase*/);

        public void AddValue(bool k, string v)
        {
            if (values.ContainsKey(k))
            {
                throw new Exception("RadioButton уже имеет выбранный элемент");
            }

            if (secondary.Add(v))
            {
                throw new Exception("RadioButton с таким значением уже был добавлен");
            }
            values.Add(k, v);
        }
    }

    /// <summary>
    /// класс элемента комбобокса доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class ComboBoxEditor : EntityDescription
    {
        /// <summary>
        /// значения
        /// int - ключ
        /// string - отображаемое значение
        /// </summary>
        [DataMember]
        public Dictionary<int, string> values { set; get; }

        /// <summary>
        /// выбранный элемент по умолчанию
        /// </summary>
        public int defaultValue { set; get; }
    }

    /// <summary>
    /// перечислитель тип доп. параметра
    /// </summary>
    public enum ExtraParamEditor
    {
        /// <summary>
        /// не задано
        /// </summary>
        None = 0,

        /// <summary>
        /// чекбокс
        /// </summary>
        CheckBox = 1,

        /// <summary>
        /// текстбокс
        /// </summary>
        TextBox = 2,

        /// <summary>
        /// радиобаттон
        /// </summary>
        RadioButton = 3,

        /// <summary>
        /// комбобокс
        /// </summary>
        ComboBox = 4
    }

    /// <summary>
    /// тип файла отчета
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// .xls файл
        /// </summary>
        Excel = 1,

        /// <summary>
        /// .pdf файд
        /// </summary>
        Pdf = 2
    }

    /// <summary>
    /// перечислитель приоритета отчета
    /// </summary>
    public enum ReportPriority
    {
        /// <summary>
        /// не задано
        /// </summary>
        None = 0,

        /// <summary>
        /// Высокий приоритет
        /// </summary>
        High = 2,
        /// <summary>
        /// Средний приоритет
        /// </summary>
        Normal = 1,
        /// <summary>
        /// Низкий приоритет
        /// </summary>
        Low = 0
    }

    /// <summary>
    /// перечислитель тип периода
    /// </summary>
    public enum DatePeriodType
    {
        /// <summary>
        /// не задано
        /// </summary>
        None = 0,

        /// <summary>
        /// день
        /// </summary>
        Day = 1,

        /// <summary>
        /// неделя
        /// </summary>
        Week = 2,

        /// <summary>
        /// месяц
        /// </summary>
        Month = 3,

        /// <summary>
        /// квартал
        /// </summary>
        Quarter = 4,

        /// <summary>
        /// месяц
        /// </summary>
        Year = 5
    }

    /// <summary>
    /// перечислитель тип отчета
    /// </summary>
    public enum ReportSverType
    {
        /// <summary>
        /// лицевые счета
        /// </summary>
        Ls = 1,

        /// <summary>
        /// дома
        /// </summary>
        Dom = 2,

        /// <summary>
        /// услуги
        /// </summary>
        Service = 3
    }

    #endregion

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Castle.Components.DictionaryAdapter;
using Globals.Properties;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Global
{
    /// <summary>
    /// Страницы
    /// </summary>
    static public class Pages
    {
        public const int NewAreaCodes = 9;
        public const int LoadRateCounters = 10; // Загрузка расхода по счетчикам
        public const int Chd = 11; // Выгрузка в ЦХД
        public const int SuppCharges = 12;          //загрузка начислений сторонних поставщиков
        public const int TransferHouses = 14;       //перенос домов из одного локального банка в другой
        public const int ConractorMerger = 15;          //слияние контрагентов
        public const int WorkWithBankMl = 17;          //взаимодействие с банком для Марий Эл
        public const int TuningDb = 18;          //настройка базы данных
        public const int RecalcPeni = 19; // Перерасчет пени
        public const int DisableChargePeni = 20; // Перерасчет пени
        public const int TypesTempDeparture = 21; // Типы временного убытия
        public const int PrepareFirstCalcPeni = 22; // Подготовка расчета первого запуска пени
        public const int AutoPerekidka = 23; // автоматические перекидки
        public const int AutoAddPackLs = 24; // автоматический ввод оплат через штрих-код
        public const int PagePlaceRequirement = 25; // места требования
        public const int CreateDom = 26; // Создание дома
        public const int ContragentParams = 28; // Параметры  котрагента
        public const int ContragentPrm = 29;  // Параметры  котрагента
        public const int UnloadPayments = 58;  // Параметры  котрагента
        public const int SplitLs = 85;  // Разделить лс
        public const int EditNewContracts = 86;  // Редактирование договоров ЖКУ
        public const int QuickAddPackLs = 87; // быстрый ввод оплат   
        public const int DisableMcLs = 88; // Периоды запрета перерасчета
        public const int QuickAddPack = 89; // Добавление пачки в режиме быстрый ввод оплат
        public const int ImportExportParam = 113; // импорт экспорт параметров
        public const int NewPayerTransferSupp = 141; // новые сальдо по перечислениям
        public const int NewDistribDomSupp = 144; // новые сальдо по перечислениям
        public const int DisableMustCalcGroup = 147; //Групповая операция запрета перерасчета по перидам
        public const int ZvkListPage = 197;         //список заявок
        public const int NewContractors = 145;  //новые контрагенты
        public const int ListProvs = 146;           //список проводок
        public const int CalcMonth = 268;           //расчетный месяц
        public const int ServicePriorities = 269;   //приоритеты услуг при распределении оплат
        public const int Settings = 270;            //настройки
        public const int AdminSettings = 271;       //настройки
        public const int Payments = 274;            //форма просмотра списка фактов финансирования
        public const int MenuRequest = 275;
        public const int MenuSubsidy = 276;
        public const int IndividualPu = 13; // Индивидуальные приборы учета
        public const int Payment = 277;             //
        public const int SubsidySaldo = 278;
        public const int LsCharges = 281;           //расчет дотаций
        public const int Vills = 283;               //справочник муниципальных образований
        public const int ActsOfSupply = 287;        //Акты о фактической поставке  y      
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
        public const int AreaCodes = 320;
        public const int SuppExchange = 321;
        public const int FindDebt = 322;//шаблон поиска по долгам
        public const int FindDeals = 323;//шаблон поиска по делам
        public const int Deal = 326; //Дело
        public const int Agreement = 327;//соглашение
        public const int LawSute = 328;//иск
        public const int debitors = 324;//должники
        public const int deals = 325;//дела
        public const int DebtChange = 329;
        public const int debtsettings = 330;//настройки
        public const int DealOperations = 332;
        public const int ReportBase = 333;
        public const int UploadEFS = 334;
        public const int UploadVTB24 = 336;
        public const int ReportLsExtJs = 337;
        public const int ReportListLsExtJs = 338;
        public const int ServDependencies = 339; // 
        public const int fastpu = 335;//Быстрый ввод показаний ПУ
        public const int DistribDom = 340;
        public const int Formuls = 341; //справочник формул
        public const int SupplierLsList = 344;
        public const int SupplierLs = 346;
        public const int Params = 349;
        public const int PercentDom = 350;            //percent_dom.aspx - процент удержания
        public const int ReportPersonExtJs = 351;  //справка для жильцов
        public const int Contracts = 352;  //договоры
        public const int NewContracts = 65;  //договоры ЖКУ
        public const int DistribDomSupp = 353;
        public const int PercentDomSupp = 354;            //percent_dom_supp.aspx - процент удержания
        public const int Contractors = 355;
        public const int PayerRevalSupp = 356;
        public const int ContractMenu = 357;  //пункт меню Договор
        public const int ContractDetails = 358; //contract_details.aspx - реквизиты договоров
        public const int NewContractDetails = 60; //new_contract_details.aspx - договоры Ерц
        public const int PayerTransferSupp = 359; //payertransfer_supp.aspx - перечисления контрагентам по договорам
        public const int PageContracts = 352;
        public const int Areas = 360;
        public const int StatCharge = 361;
        public const int StatChargeDom = 362;
        public const int ChangeLsAddress = 363;
        public const int OverpaymentManager = 366;
        public const int SupplierDomList = 367;
        public const int ReportDebtorExtJs = 369;   //отчет по списку лс 2, сформированном по выбранному списку должников
        public const int GroupsTarifs = 378;
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
        public const int RefreshSpisok = 5;     //обновить список
        public const int RefreshData = 66;     //обновить данные
        public const int ShowConfirmedCounterReadings = 71;        //показать утвержденные показания ПУ
        public const int ShowNotConfirmedCounterReadings = 72;     //показать введенные показания ПУ
        public const int Distribute = 93;     //показать введенные показания ПУ

        public const int CounterReadingsEnteredByOperator = 111;    //источник показаний - оператор
        public const int CounterReadingsUploadedFromFile = 112;     //источник показаний - файл с показаниями с сайта (для Самары)
        public const int OpenCalcMonth = 138;               //открыть месяц
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
        public const int UploadFile = 163;                      //загрузить файл
        public const int DisassembleFile = 166;                 //разобрать файл
        public const int Add = 169;                         //добавить
        public const int Save = 170;                        //сохранить
        public const int Reallocate = 171;                  //повторно распределить оплаты по новым ЛС      
        public const int CalcForIPU = 172;                  //расчет для ИПУ
        public const int GenPkod = 871;                     //сгенерировать платежные коды
        public const int Realize = 174;                     //выполнить
        public const int CounterReadingsEnteredByGilec = 175;   //показания ПУ, введенные жильцом (например, через портал)
        public const int AddFormula = 176;                      //добавить формулу
        public const int NewOperation = 179;                    //добавить формулу
        public const int UchetOplat = 181;                  //учет оплат
        public const int CreatePack = 182;                  //создать пачку
        public const int StandardFormat = 870;              //Стандартный формат загрузки данных
        public const int ChangeArea = 184;                  //Перенести выбранный список домов в новую УК
        public const int AddPackage = 185;                  //Перенести выбранный список домов в новую УК
        public const int DelPackage = 186;                  //Перенести выбранный список домов в новую УК
        public const int UploadKLADR = 187;                 //Загрузить адресное пространство из КЛАДР
        public const int DownloadFile = 188;                //выгрузить файл
        public const int OpenUpload = 189;                  //открыть реестр
        public const int CreateReestr = 191;                //сформировать реестр
        public const int SetChangesAct = 329;               //настройка расчетов
        public const int Saldo_5_10 = 212;                  //Сальдовая ведомость 5.10
        public const int FindDebt = 513;                    //шаблон поиска по долгам
        public const int FindDeals = 514;                   //шаблон поиска по делам
        public const int OpenAgreement = 192;               //открыть соглашение
        public const int OpenLawSute = 193;                 //открыть иск
        public const int NeboIncome = 195;                  //сформировать реестр по оплатам
        public const int SetKeys = 196;
        public const int DebtChange = 194;
        public const int CreateDeal = 198;                  //создать дело для должника
        public const int CloseDeal = 199;                   //закрыть дело
        public const int OpenDeal = 200;                    //открыть дело
        public const int PacksForm = 541;                   //сформировать пачки
        public const int CounterReadingsEnteredFromPortal = 542; //показания ПУ, введенные через портал
        public const int ConfirmVals = 543; //утвердить показания ПУ
        public const int ToTransfer = 544; //Учесть к перечислению
        public const int PrepareData = 545; //Подготовить данные
        public const int RestartJob = 546; // Перезапустить задание
        public const int CancelJob = 547; // Отменить задание
        public const int CloseOperDay = 548; // Закрыть операционный день
        public const int GoBackOperDay = 549; // Вернуться на предыдущий операционный день
        public const int ReDistribute = 550; // перераспределить оплаты
        public const int RefreshStatus = 554; // обновить статус
        public const int Download = 555; // обновить статус
        public const int ReCalcKomiss = 875; //пересчитать комиссию
        public const int EditPack = 876; //редактировать пачку
        public const int PackOutCase = 877; //пачку убрать из портфеля
        public const int SaveInDb = 881;// Сохранить в БД
        public const int Merge = 882;// Объединить 
        public const int RecreatePlatAfterCancel = 883;//пересоздать платежи после отмены
        public const int FormatVersion3 = 886;// Версия формата 3 для Марий ЭЛ
        public const int FormatVersion2_1 = 898;// Версия формата 2.1
        public const int FormatVersion2_2 = 899;// Версия формата 2.2
        public const int RecalcPeniAct = 904;// Кнопка перерасчет пени
        public const int PrepareFirstCalcPeniAct = 905;// Кнопка перерасчет пени
        public const int DeleteCurrentRole = 908;// Кнопка  удалить текущую роль
        public const int AutoInput = 909;// Кнопка  автоматический ввод
        public const int CalcDomList = 912;// Кнопка Расчет списка домов
        public const int AddNewGroup = 913;// Кнопка Добавить новую группу
        public const int SaveAndClosePack = 916;// Кнопка сохранить и закрыть пачку
        public const int QuickAddPackLs = 917;// Кнопка быстрый ввод оплат
        public const int ReplacePackLsToCurFinYear = 919;// Кнопка перенести в текущий фин год
        public const int EditScopeDog = 920;// Редактировать область дейтсвия выбранного договора
        public const int PrepareProvs = 922; //Переформировать проводки
        public const int InterruptProcess = 926; //Прервать процесс
        public const int SelectOverpayments = 927; //Отобрать переплаты
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

    //----------------------------------------------------------------------
    static public class Constants  //глобальные константы
    //----------------------------------------------------------------------
    {
        public static bool UseOpenAMAuthentication { get { return false; } }
        public static bool UseExtendedConnectionfactory { get { return false; } }
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
                    var directory = FilesDir + BILL_DIR;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    return directory;
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
                    var directory = BillDir + WEB_DIR;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    return directory;
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
                    var directory = _FilesDir;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    return directory;
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
                    var directory = FilesDir + REPORTS_DIR;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    return directory;
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
                    var directory = FilesDir + IMPORT_DIR;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    return directory;
                    
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
                    var directory = FilesDir + SUBSIDY_DIR + ACTS_OF_SUPPLY_DIR;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    return directory;
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
                    var directory = FilesDir + SUBSIDY_DIR + THGF_DIR;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    return directory;
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

        public static string Login = "";
        public static string Password = "";
        public const string Linespace = "http://www.stcline.ru";
        public const string Kassa_3_0 = "WorkOnlyWithCentralBank";

        static public List<_Arms> ArmList = new List<_Arms>();
        static public List<_Pages> Pages = new List<_Pages>();
        //static public List<_PageShow> PageShow = new List<_PageShow>();
        static public List<_Actions> Actions = new List<_Actions>();
        static public List<_ActShow> ActShow = new List<_ActShow>();
        static public List<_ActLnk> ActLnk = new List<_ActLnk>();
        static public List<_SysPort> SysPort = new List<_SysPort>();
        static public Dictionary<int, List<_PageShow>> DictPagesShow = new Dictionary<int, List<_PageShow>>();
        static public Dictionary<int, _Pages> DictPages = new Dictionary<int, _Pages>();
        static public List<_Menu> Menu = new List<_Menu>();
        static public Dictionary<int, List<_ActShow>> DictActionShow = new Dictionary<int, List<_ActShow>>();
        static public Dictionary<int, _Actions> DictActions = new Dictionary<int, _Actions>();

        static public List<_ExtMM> ExtMM = new List<_ExtMM>(); //главное меню в ExtJS
        static public List<_ExtPM> ExtPM = new List<_ExtPM>(); //подменю главного меню в ExtJS


        //public static string VersionSrv = "2015.055." + DateTime.Now.ToString("MMdd.HHmm") + " от 22.04.2015";
        public static string VersionSrv = "2015.064." + DateTime.Now.ToString("MMdd.HHmm") + " от 02.11.2015(104)";
        public static string VersionWeb = VersionSrv;

        public static int VersionDB = 28;

        public static string DefaultAspx = "";

        public static string cons_Webdata;      //ConnectionString Webdata
        public static string cons_Kernel;       //ConnectionString Kernel
        public static string cons_Portal = "";  //ConnectionString Portal
        public static string cons_User;         //User string
        public static bool Viewerror;         //расшифровка ошибки в логе
        public static bool Debug;

        /// <summary>
        /// записывать в лог каждый шаг для отладки
        /// </summary>
        public static bool Trace;
        /// <summary>
        /// Значения уникальны для каждого потока
        /// </summary>
        [ThreadStatic]
        public static bool TraceLong;
        [ThreadStatic]
        public static double ThresHoldTimeQuery = 1;//1 секунда
        [ThreadStatic]
        public static int ThresHoldAffectedRows = 1000;//1000 записей
        //наименование договора
        /// <summary>
        /// именительный падеж (Поставщик/Договор)
        /// </summary>
        public static string ContractNameIP = Resources.ContractNameIP; //"Договор";

        /// <summary>
        /// именительный падеж с wbr (Пос<wbr>тав<wbr>щик/Дого<wbr>вор)
        /// </summary>
        public static string ContractNameIPWbr = Resources.ContractNameIPWbr;//"Дого<wbr>вор";

        /// <summary>
        /// множественное число (Поставщики/Договоры)
        /// </summary>
        public static string ContractNameMCh = Resources.ContractNameMCh; //"Договоры";

        /// <summary>
        /// именительный падеж единственное (множественное) число (Поставщик(и)/Договор(ы))
        /// </summary>
        public static string ContractNameEChMChIP = Resources.ContractNameEChMChIP; //"Договор(ы)";

        /// <summary>
        /// родительный падеж (Поставщика/Договора)
        /// </summary>
        public static string ContractNameRP = Resources.ContractNameRP;// "Договора";

        /// <summary>
        /// родительный падеж единственное (множественное) число (Поставщика(ов)/Договора(ов))
        /// </summary>
        public static string ContractNameEChMChRP = Resources.ContractNameEChMChRP;//"Договора(ов)";

        /// <summary>
        /// родительный падеж множественное число (Поставщиков/Договоров)
        /// </summary>
        public static string ContractNameMChRP = Resources.ContractNameMChRP; //"Договоров";

        /// <summary>
        /// дательный падеж (Поставщику/Договору)
        /// </summary>
        public static string ContractNameDP = Resources.ContractNameDP;// "Договору";

        /// <summary>
        /// дательный падеж множественное число (Поставщикам/Договорам)
        /// </summary>
        public static string ContractNameMChDP = Resources.ContractNameMChDP;//"Договорам";

        /// <summary>
        /// винительный падеж (Поставщика/Договор)
        /// </summary>
        public static string ContractNameVP = Resources.ContractNameVP;//"Договор"; 

        /// <summary>
        /// винительный падеж единственное (множественное) число (Поставщика(ов)/Договор(ы))
        /// </summary>
        public static string ContractNameEChMChVP = Resources.ContractNameEChMChVP;//"Договор(ы)";

        /// <summary>
        /// творительный падеж (Поставщиком/Договором)
        /// </summary>
        public static string ContractNameTP = Resources.ContractNameTP;// "Договором";

        /// <summary>
        /// предложный падеж (Поставщике/Договоре)
        /// </summary>
        public static string ContractNamePP = Resources.ContractNamePP;//"Договоре";

        public const string access_error = "Сервис временно недоступен. Попробуйте выполнить операцию позже.";
        public const int access_code = -1000;
        public const string name_logfile = "Komplat50Log"; //название лог-журнала
        public const int AllZap = -101;           //все записи в справочнике
        public const int DefaultZap = -102;           //значение по-умолчанию
        public const string ChooseData = "<Выберите данные>";
        public const string ChooseServ = "<Выберите услугу>";
        public const string ChooseSupp = "<Выберите договор>";

        //пользователь "Автоматическая рассрочка"
        public const int AutoDefeferredPayUserID = -88888888;

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
        public const int roleNormAdd = 918;
        public const int roleDebt = 920;
        public const int rolePsDebt = 28;
        public const int roleRaschetNachisleniy = 921;
        public const int roleReport = 30;
        public const int roleSpravEdit = 933;
        public const int roleUpgOperator = 934;
        public const int roleUpgDispetcher = 935;
        public const int roleUpgPodratchik = 936;
        public const int roleUpgUK = 937;
        public const int roleUpgAdministrator = 919;
        public const int roleFinSpravChange = 942;
        public const int roleChangeSaldoOplatami = 956;


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
        public const int selNzp_kvar = 1;                // по л/с   - nzp_kvar
        public const int selNzp_dom = 2;                // по домам - nzp_dom

        public const int acc_in = 1;             //log_access: вход
        public const int acc_exit = 2;             //log_access: выход
        public const int acc_failure = 3;             //log_access: несанкционированный вход

        public const string _UNDEF_ = "_UNDEF_";     // значение неопределнного поля
        public const int _ZERO_ = -999987654;    // значение неопределнного поля
        public static int users_min = 60;            // таймаут повторного входа в минутах 
        public const int blocking_lifetime = 7;          // период действия блокировки записей
        public const int recovery_link_lifetime = 24;   // время жизни ссылки на восстановление пароля в часах

        public const int workinfon = -999;  //признак вызова фоновой службы

        public const int arm_kartoteka = 10;   //Картотека
        public const int arm_analitika = 11;   //Аналитика
        public const int arm_admin = 12;   //Администратор

        public const int page_login = 0;     //login.aspx
        public const int page_default = 1;     //default.aspx

        public const int page_ps = 10;    //подсистемы      

        public const int page_myreport = 5;        //myreport.aspx - файлы пользователя
        public const int page_settings = 6;        //settings.aspx - настройки пользователя

        public const int page_perekidkioplatami = 7;        //perekidkioplatami.aspx - изменения сальдо оплатами
        public const int page_prm_normatives = 8; //normatives.aspx - отображение и редактирование нормативов


        public const int page_responsible = 27;   // otvetstv.aspx
        //30 - шаблоны поиска       
        public const int page_findls = 31;       //findls.aspx поиск по л/с 
        public const int page_findprm = 32;       //findprm.aspx
        public const int page_findch = 33;       //findch.aspx
        public const int page_findgil3 = 34;       //findgil3.aspx
        public const int page_findgil = 34;       //findgil.aspx
        public const int page_findcnt = 35;       //findcnt.aspx
        public const int page_findnedop = 36;       //findnd.aspx
        public const int page_findodn = 37;       //findodn.aspx
        public const int page_findserv = 38;       //findserv.aspx
        public const int page_findsupg = 39;       //findsupg.aspx

        //40 - выбранные списки
        public const int page_spis = 40;    //списки
        public const int page_spisls = 41;    //spisls.aspx
        public const int page_spisdom = 42;    //spisdom.aspx

        public const int page_spisul = 43;    //spisul.aspx
        public const int page_spisar = 44;    //spisar.aspx
        public const int page_spisgeu = 45;    //spisgeu.aspx
        public const int page_spisbd = 46;    //spisbd.aspx
        public const int page_domul = 47;    //дома улицы
        public const int page_backtoprm = 48;    //Вернуться на список параметров
        public const int page_spissupp = 49;    //spis_supp.aspx
        public const int page_perechen_lsdom = 50;    //перечень счетов дома
        public const int page_spisprm = 51;    //spisprm.aspx / baselist.aspx
        public const int page_spispu = 53;    //counters.aspx

        public const int page_spisval = 54;    //spisval.aspx для квартирных ПУ
        public const int page_spisnd = 55;    //spisnd.aspx
        public const int page_spisgil = 56;    //spisgil.aspx
        public const int page_spisdomls = 57;    //baselist.aspx
        public const int page_spisuldom = 58;    //дома данной улицы
        public const int page_spisprmdom = 59;    //spisprm.aspx / baselist.aspx
        public const int page_spisvaldom = 61;    //spisval.aspx для домовых ПУ        

        public const int page_puls = 62;    //КПУ
        public const int page_pudom = 63;    //ДПУ

        //public const int    page_prm       = 64;    //Значение одного квартирного параметра
        public const int page_spisvalgroup = 66;    //spisval.aspx для групповых ПУ (от квартиры)
        public const int page_spisvalgroupdom = 67;    //spisval.aspx для групповых ПУ (от дома)
        public const int page_counterscardls = 68;    //counterscard.aspx квартирный прибор
        public const int page_counterscarddom = 69;    //counterscard.aspx домовой прибор

        public const int page_data = 70;    //данные лс
        public const int page_datadom = 71;    //данные дома
        public const int page_аnalytics = 72;    //аналитика
        public const int page_dictionaries = 73;    //справочники
        public const int page_datapack = 76;    //данные о пачке
        public const int page_operations = 77;    //пункт меню - Операции
        public const int page_data_about_order = 78;    //данные о заявке

        public const int page_aa_adres = 81;    //адресное пространство
        public const int page_aa_supp = 82;    //анализ по поставщикам
        public const int page_sprav_remark = 90;    //справочник примечаний
        public const int page_counterscard = 91;    //counterscard.aspx

        public const int page_gil = 91;    //gil.aspx - карточка жильца (Комлат 2.0)

        public const int page_pugroup = 92;    //counters.aspx список групповых приборов
        public const int page_counterscardgroup = 93;  //counterscard.aspx групповой прибор
        public const int page_countertypes = 94;    //countertypes.aspx
        public const int page_spisserv = 95;    //spisserv.aspx перечень услуг
        public const int newpage_spisserv = 116;    //spisserv.aspx перечень услуг
        public const int page_supp_formuls = 96;    //supp_formuls.aspx     
        public const int newpage_supp_formuls = 119;    //supp_formuls.aspx     
        public const int page_groupls = 97;    //groupls
        public const int page_cardls = 98;    //cardls
        public const int page_carddom = 99;    //carddom    
        public const int page_groupspisprmls = 100;      // spisprm - групповые операции: характеристики жилья для выбранных лицевых счетов
        public const int page_groupspisprmdom = 102;      // spisprm - групповые операции: характеристики жилья для выбранных домов
        public const int page_groupprmls = 101;          // prm - групповые операции с выбранной квартирной характеристикой жилья для выбранных лицевых счетов
        public const int page_groupprmdom = 103;          // prm - групповые операции с выбранной квартирной характеристикой жилья для выбранных домов
        public const int page_groupspisserv = 104;          // serv - групповые операции с выбранной услугой для выбранных лицевых счетов
        public const int page_newfdgroupspisserv = 117;          // serv - групповые операции с выбранной услугой для выбранных лицевых счетов
      
        public const int page_group_supp_formuls = 105;      //serv 
        public const int newpage_group_supp_formuls = 132; 
        public const int page_statcharge = 106;          //статистика по начислениям 
        public const int page_distrib = 107;          //distrib
        public const int page_groupcardls = 108;          //групповые операции реквизиты л/с  
        public const int page_groupcarddom = 109;          //групповые операции реквизиты дома
        public const int page_groupnedop = 110;          //групповые операции недопоставки
        public const int page_changesostls = 111;          //характеристики жилья изменение состояния л/с
        public const int page_statchargedom = 112;          //статистика по начислениям по дому
        public const int page_new_available_services = 114; //new_availableservices.aspx - доступные услуги (новая)
        public const int page_new_available_service = 115; //new_availableservice.aspx - доступная услуга (новая)

        public const int page_charge = 120;   //начисления
        public const int page_charges = 122;   //charges.aspx
        public const int page_listpays = 123;   //listpays.aspx
        public const int page_odn = 124;   //odn.aspx

        public const int page_saldols = 121;   //saldols.aspx
        public const int page_saldodom = 126;   //saldodom.aspx
        public const int page_saldouk = 127;   //saldouk.aspx
        public const int page_saldosupp = 130;   //saldosupp.aspx

        public const int page_bill = 129;   //bill.aspx    
        public const int page_billrt = 131;   //billrt.aspx    
        public const int page_pay = 132;   //pay.aspx
        public const int page_reportls = 133;   //report.aspx по лицевому счету
        public const int page_reportlistls = 134;   //report.aspx по выбранным спискам ЛС
        public const int page_reportgil = 135;   //report.aspx по жильцу
        public const int page_reportlistgil = 136;   //report.aspx по выбранным спискам жильцов
        public const int page_reportdom = 137;   //report.aspx по выбранному дому
        public const int page_reportlist = 138;   //report.aspx по списку заявок
        public const int page_reportlistplan = 139;   //report.aspx по списку плановых работ
        public const int page_new_spisservdom = 142;      //перечень услуг для дома
        public const int page_reportlistdeptor = 370;   //отчеты по списку лс, сформированном по выбранному списку должников

        public const int page_users = 151;   //users.aspx
        public const int page_roles = 152;   //roles.aspx
        public const int page_usercard = 153;   //usercard.aspx
        public const int page_rolecard = 154;   //rolecard.aspx
        public const int page_access = 155;   //access.aspx

        public const int page_processes = 161;      //processes.aspx
        public const int page_kvargil = 162;      //spisgil.aspx - поквартирная карточка (Комлат 2.0)
        public const int page_spisgilper = 163;      //spisglp.aspx - список периодов временного убытия жильца (Комлат 2.0)
        public const int page_glp = 164;      //glp.aspx - период временного убытия жильца (Комлат 2.0)
        public const int page_perekidki = 165;      //perekidki.aspx - изменения сальдо
        public const int page_rashod_kvar = 166;      //rashod.aspx - расход по квартире
        public const int page_rashod_dom = 167;      //rashod.aspx - расход по дому
        public const int page_tarifs = 168;      //tarifs.aspx - тарифы
        public const int page_one_tarif = 169;      //prm.aspx - значения одного тарифа
        public const int page_sysparams = 170;      //sysparams.aspx - системные параметры
        public const int page_prm_pu_kvar = 171;      //prm.aspx - значения параметра прибора учета (из данных о квартире)
        public const int page_prm_pu_dom = 172;      //prm.aspx - значения параметра прибора учета (из данных о доме)
        public const int page_prm_kvar = 173;      //prm.aspx - значения параметра (из реквизитов ЛС)
        public const int page_prm_supp = 174;      //prm.aspx - значения параметра (из списка параметров поставщика)
        public const int page_suppparams = 175;      //suppparams.aspx
        public const int page_frmparams = 176;      //frmparams.aspx
        public const int page_spissobstw = 177;      //spissobstw.aspx
        public const int page_kartsobstw = 178;      //kartsobstw.aspx
        public const int page_group_nedop_dom = 179;      //spisnd.aspx - групповые операции с домовыми недопоставками
        public const int page_group_spis_ls_prm_dom = 180;  //spisprm.aspx - список квартирных параметров для груп опер по домам 
        public const int page_group_ls_prm_dom = 181;  //prm.aspx - групповые операции с квартирной характеристикой для лицевхы счетов выбранных домов
        public const int page_group_spis_serv_dom = 182;  //spisserv.aspx - групповые операции с услугами для ЛС выбранных домов
        public const int page_newfd_group_spis_serv_dom = 118;  //spisserv.aspx - групповые операции с услугами для ЛС выбранных домов
        public const int page_group_supp_formuls_dom = 183;  //serv.aspx - групповые операции с услугой для ЛС выбранных домов
        public const int newpage_group_supp_formuls_dom = 140;  //serv.aspx - групповые операции с услугой для ЛС выбранных домов
        public const int page_report_odn = 184;      //reportodn.aspx - протокол расчета ОДН
        public const int page_spispu_communal = 185;      //counters.aspx - список коммунальных ПУ
        public const int page_spisval_communal = 186;      //spisval.aspx - список показаний коммунальных ПУ
        public const int page_pu_communal = 187;      //countercard.aspx - карточка коммунального ПУ
        public const int page_prm_dom = 188;      //prm.aspx - значения параметра (из реквизитов дома)
        public const int page_supg_kvar_orders = 189;      //order.aspx - заявки лицевого счета
        public const int page_spisservdom = 190;      //перечень услуг для дома
        public const int page_spisnddom = 191; //недопоставки по дому
        public const int page_prmodn = 192; //параметры настройки ОДН
        public const int page_joborder = 193; //наряд-заказ
        public const int page_spisgilhistory = 194; //история жильца
        public const int page_findgroupls = 195; //поиск по группам
        public const int page_group = 196; //групповые операции добавления(исключения) выбранного списка в группу (из группы)
        public const int page_spis_order = 197; //список выбранных заявок
        public const int page_pack = 198; //пачка платежей pack.aspx
        public const int page_pack_ls = 199; //платеж pack_ls.aspx
        public const int page_upload_pack = 200; //загрузка пачки uploadpack.aspx
        public const int page_finances_findpack = 201; // шаблон поиска по оплатам
        public const int page_finances_pack = 202; //пачка оплат
        public const int page_finances_operday = 203; //операционный день
        public const int page_finances_pack_ls = 204; //квитанция об оплате
        public const int page_supg_order = 205; //одна претензия
        public const int page_report_common = 206; //отчеты по всему банку
        public const int page_supg_arm_operator = 207; //АРМ оператора
        public const int page_supg_raw_orders = 208; //raworders.aspx
        public const int page_incoming_job_orders = 209; //список нарядов-заказов на выполнение
        public const int page_prm_norms = 210; //norms.aspx
        public const int page_sprav_cel_prib = 211; //sprav.aspx - цель прибытия
        public const int page_sprav_docs = 212; //sprav.aspx - документы
        public const int page_sprav_rodst = 213; //sprav.aspx - родственные отношения
        public const int page_sprav_grazhd = 214; //sprav.aspx - гражданства
        public const int page_sprav_adresses = 215; //address.aspx - страна, регион, город, район, нас. пункт
        public const int page_sprav_rajon_doma = 216; //sprav.aspx - районы дома
        public const int page_sprav_organ_reg_ucheta = 217; //sprav.aspx - орган регистрационного учета
        public const int page_sprav_mesto_vidachi_doc = 218; //sprav.aspx - место выдачи документа
        public const int page_sprav_doc_sobst = 219; //sprav.aspx - документы о собственности
        public const int page_supg_nedop = 220; //nedop.aspx - выгрузка недопоставок
        public const int page_services = 221; //services.aspx - справочник услуг
        public const int page_service_params = 222; //servparams.aspx - список параметров выбранной услуги
        public const int page_prm_serv = 223; //prm.aspx - значения одного параметра услуги
        public const int page_available_services = 224; //availableservices.aspx - доступные услуги
        public const int page_available_service = 225; //availableservice.aspx - доступная услуга
        public const int page_find_server = 226; //findserver.aspx - писк по серверам
        public const int page_area_params = 227; //areaparams.aspx - список параметров выбранной управляющей организации
        public const int page_prm_area = 228; //prm.aspx - значения одного параметра управляющих организаций
        public const int page_geu_params = 229; //geuparams.aspx - список параметров выбранного участка
        public const int page_prm_geu = 230; //prm.aspx - значения одного параметра участка
        public const int page_area_requisites = 231; //arearequisites.aspx - реквизиты управляющих организаций
        public const int page_payer_requisites = 232; //payerrequisites.aspx - реквизиты подрядчика
        public const int newpage_payer_requisites = 143;//new_payerrequisites.aspc.cs - новые реквизиты контрагентов
        public const int page_payer_contracts = 233; //contracts.aspx - договоры с подрядчиком
        public const int page_payer_transfer = 234; //payertransfer.aspx - перечисления подрядчикам
        public const int page_menu_oper_day = 235; //пункт меню операционный день
        public const int page_supg_kvar_job_order = 238;//kvarjoborder.aspx - все наряды заказы по квартире
        public const int page_percent = 239;            //percent.aspx - процент удержания
        public const int page_ls_contracts = 240; //lscontracts.aspx - список договоров по ЛС
        public const int page_counter_readings = 241; //counterreadings.aspx - списочный ввод показаний ПУ (сразу по всему дому)
        public const int page_add_period_ub_to_selected = 242; //glp.aspx - добавление периода убытия выбранным жильцам
        public const int page_upload_counter_values = 243; //uploadcounters.aspx - загрузка показаний ПУ из файла
        public const int page_credit = 244; //рассрочка
        public const int page_find_planned_works = 245; //поиск плановых работ
        public const int page_planned_works = 246; //список плановых работ
        public const int page_planned_work_add = 247; //плановая работа для добавления
        public const int page_sprav_servorgs = 248; //справочник служб / организаций
        public const int page_planned_work_show = 250; //плановая работа для просмотра
        public const int page_planned_work_ls = 251; //плановая работа по ЛС
        public const int page_claimcatalog = 252; //справочник претензий
        public const int page_case = 253; //портфель
        public const int page_analisis = 255;//страница для гистограмм
        public const int page_contractorcatalog = 256;  //страница справочник подрядчиков
        public const int page_bankcatalog = 257;  //страница справочник банков
        public const int page_basket = 258;//корзина
        public const int page_gendomls = 260; // генерация ЛС по дому
        public const int page_streetcatalog = 261; //справочник улиц
        public const int page_correctsaldo = 262; //корректировка сальдо
        public const int page_groupprm = 263; //групповой ввод характеристик жилья
        public const int page_genlspu = 264;  //генерация приборов учета по списку ЛС
        public const int page_condistrpayments = 265;  //Контроль распределения оплат
        //public const int page_prm_dom = 266;  //значения одного домового параметра
        public const int page_addtask = 266; // добавление задания (пересечение с предыдущей константой)
        public const int page_survey_job_orders = 267;//список нарядов-заказов для опроса
        public const int page_calc_month = 268; //расчетный месяц
        public const int page_percpt = 284; //справочник уровня платежей граждан
        public const int page_cashplan = 285; //загрузка кассового плана

        //Дотации
        public const int page_requests = 272;  //Шаблон поиска по заявкам
        public const int page_request = 273;  //Заявка на финансирование
        public const int page_agreements = 279;  //Список соглашений с подрядчиками
        public const int page_agreement = 280;  //Карточка соглашения с подрядчиком

        // Рассылка сообщений
        public const int page_messagelist = 282; // список сообщений 
        public const int page_newmessage = 286; // новое сообщение        
        public const int page_phonesprav = 289; // справочник телефонов

        public const int page_refresh_kp_sprav = 297; //обновление данных комплат в УПГ
        public const int page_sprav_themes = 298; //Справочник классификация сообщений УПГ


        public const int page_prepareprintinvoices = 342; //форма подготовки данных для печати счетов
        public const int page_jobs = 343;                 // список задач в очереди
        public const int page_ls_events = 345;          // история событий ЛС
        public const int page_dom_events = 347;          // история событий дома
        public const int page_download_logs = 348;          // история событий дома

        public const int groupbylsdom = 368;          // группы лицевых счетов по выбранному списку домов
        public const int workwithcountersreadings = 374;  // загрузка и выгрузка показаний счетчиков (Показания счетчиков)

        public const int page_spisval_communalkv = 371;          // показания общеквартирных счетчиков в меню ЛС
        public const int page_spispu_groupkv = 372;      //counters.aspx - список групповых ПУ
        public const int page_spispu_communalkv = 373;      //counters.aspx - список коммунальных ПУ
        public const int page_settings_create_pack = 375;      // страница настроек создания пачек

        public const int act_groupby_month = 520;   //Группировать по месяцу
        public const int act_groupby_service = 521;   //Группировать по услуге
        public const int act_groupby_supplier = 522;   //Группировать по поставщику
        public const int act_groupby_formula = 523;   //Группировать по формуле
        public const int act_groupby_area = 524;   //По управляющим организациям

        public const int act_groupby_geu = 525;   //По отделениям
        public const int act_groupby_bd = 526;   //По банкам данных
        public const int act_groupby_dom = 527;   //По домам

        public const int act_show_saldo = 528;   //Показывать сальдовые показатели    
        public const int act_groupby_device = 529;   //Группировка норматив / прибор учета    

        public const int act_groupby_payer = 536;   //Группировать по плательщикам
        public const int act_groupby_bank = 537;   //Группировать по банкам
        public const int act_groupby_date = 538;   //Группировать по датам
        public const int act_groupby_town = 539;   //По районам

        public const int act_groupby_princip = 540;   //По принципалам
        public const int act_groupby_supp = 541;   //По поставщикам
        public const int act_groupby_agent = 542;   //По контрагентам
        public const int act_groupby_podr = 543;   //По 

        public const int sortby_adr = 601;   //"Сортировать по адресу"
        public const int sortby_ls = 602;   //"Сортировать по лс"  
        public const int sortby_ul = 603;   //"Сортировать по улице"    
        public const int sortby_uk = 604;   //"Сортировать по УК"    
        public const int sortby_serv = 605;   //"Сортировать по услуге"    
        public const int sortby_supp = 606;   //"Сортировать по поставщику"    
        public const int sortby_fiodr = 607;   //"Сортировать по ФИОДР"    

        public const int sortby_login = 608;   //"Сортировать по логину"    
        public const int sortby_username = 609;   //"Сортировать по имени пользователя"    
        public const int sortby_nzp_user = 1608;  //
        public const int sortby_email = 1609;  //


        public const int menu_help = 3;     //help
        public const int menu_previos = 4;     //previos page
        public const int menu_myfiles = 5;     //мои файлы
        public const int menu_exit = 999;   //exit
        public const int menu_seans = 950;   //seans
        public const int menu_not = 949;   //нет данных

        public const int act_find = 1;     //поиск
        public const int act_erase = 2;     //очистка ШП
        public const int act_open = 3;     //открыть данные
        public const int act_add = 4;     //добавить   
        public const int act_refresh = 5;     //обновить
        public const int act_showmap = 7;     //показать карту
        public const int act_print = 8;     //Открыть для печати
        public const int act_block = 9;     //Заблокировать/разблокировать пользователя

        public const int act_process_start = 10; //запустить обработчик заданий
        public const int act_process_pause = 11; //приостановить обработчик
        public const int act_delete_all = 12; //удалить все
        public const int act_delete_session = 13; //удалить сессию пользователя
        public const int act_open_puindication = 14; //открыть показания ПУ
        public const int act_reset_user_pwd = 15; //сбросить пароль выбранного пользователя
        public const int act_save_val = 16; //сохранить значение
        public const int act_del_val = 17; //удалить значение параметра
        public const int act_add_nedop = 18; //добавить недопоставку
        public const int act_del_nedop = 19; //удалить недопоставку

        public const int act_showallprm = 20; //показать все параметры

        public const int act_add_serv = 21; //добавить период действия услуги, назначить поставщика и формулу расчета
        public const int act_del_serv = 22; //удалить период действия услуги
        public const int act_add_gil = 23; //добавить нового жильца
        public const int act_copy_ls = 24; //копировать л/с
        public const int act_add_dom = 25; //создать дом
        public const int act_delete = 26; // удалить запись
        public const int act_del_pu = 64; //удалить ПУ

        public const int act_aa_recalc = 65;    //подсчет агрегированных сумм
        public const int act_aa_refresh = 66;    //обновить данные
        public const int act_save = 61;    //сохранить данные
        public const int act_prm = 67;    //Параметры - архив / история

        public const int act_update_role_filters = 68;   // обновить списки управляющих организаций, участков, услуг, поставщиков, банков данных, которые применяются для ограничения прав доступа ролей
        public const int act_get_report = 69;       //действие получить отчет
        public const int act_calc = 70;       // рассчитать начисления
        public const int act_export_to_excel = 73;       // выгрузить данные в excel
        public const int act_enter_archive = 74;       // войти в архив
        public const int act_exit_archive = 75;       // выйти из архива
        public const int act_open_period_serv = 76;       // перейти к просмотру периодов действия услуги, поставщиков и формул расчета
        public const int act_open_prm_serv = 77;       // перейти к просмотру параметров для выбранной услуги
        public const int act_copy = 78;                     //копировать
        public const int act_paste = 79;                    //вставить
        public const int act_showls = 80;                   //показать л/с
        public const int act_add_joborder = 81;             //добавить наряд-заказ
        public const int act_add_ingroup = 82;              //добавить список в группу
        public const int act_del_outgroup = 83;             //исключить список из группы
        public const int act_go_back_to_orders = 84;        //вернуться к списку претензий лицевого счета
        public const int act_add_pack = 85;       //добавить пачку
        public const int act_close_pack = 86;       //закрыть пачку
        public const int act_add_pack_ls = 87;       //добавить новый платеж
        public const int act_upload_pack = 88;       //загрузить пачку
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

        public const int act_set_zakaz_act_actual = 100;    //выставление признака формирования недопоставки
        public const int act_close_zakaz_act_actual = 101;  //снятие признака формирования недопоставки
        public const int act_open_params = 103;  //показать параметры
        public const int act_open_area_requisites = 104;  //перейти к реквизитам управляющих организаций
        public const int act_open_payer_transfer = 107;  //перейти к перечислениям пожрядчикам
        public const int act_calc_saldo = 108;  //расчет сальдо поставщика
        public const int act_add_period_ub_to_selected = 109;  //добавить период убытия выбранным жильцам
        public const int act_upload_counter_values = 110;  //загрузить показания ПУ из файла
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
        public const int act_cont_distrib = 124; //контроль распределения - отчет
        public const int act_find_error_distrib = 125;//найти ошибку распределения
        public const int act_find_error_payment = 126;//найти ошибка в оплатах
        public const int act_open_pack_ls = 127;    //открыть квитанцию
        public const int act_print_show = 128;     //Версия для печати
        public const int act_refresh_addresses = 129;     //Обновить адресное пространство в центральном банке
        public const int act_closeJobOrder_toExecute = 130;  //Закрыть наряд-заказ
        public const int act_cancelJobOrder = 136; //Отменить полученный наряд-заказ        
        public const int act_req_edit = 138; //исправить заявку на финансирование
        public const int act_req_approve = 139; //утвердить заявку на финансирование
        public const int act_req_del = 140; //удалить заявку на финансирование
        public const int act_req_add = 142; //добавляет новую заявку на финансирование
        public const int act_page_refresh = 143; //обновляет текущую страницу
        public const int act_agreement_add = 145; //добавить соглашение с подрядчиком
        public const int act_agreement_del = 146; //Удалить соглашение с подрядчиком
        public const int act_new_sms = 150;   // Новое сообщение
        public const int act_send_sms = 151;   // Отправить сообщение
        public const int act_del_sms = 152;   // Удалить сообщение
        public const int act_load_cashplan = 153;//загрузить кассовый план        
        public const int act_charge = 164;//рассчитать
        public const int act_exec_refresh = 173;  //выполнить обновление данных комплат в УПГ
        public const int act_calc_odpu_rashod = 177;  // подсчитать расход для ОДПУ 

        public const int act_move_to = 540;   //перейти к делу

        public const int act_report_spravka_po_smerti = 201; // Справка по смерти ф5 для Самары
        public const int act_report_spravka_po_den_smert = 906; // Справка по день смерти
        public const int act_report_sprav_o_sost_sem_T = 884; // справка о составе семьи для Тулы
        public const int act_report_spravka_o_sostave_semji = 202; // справка о составе семьи        
        public const int act_report_spravka_s_mesta_reg_v_sud = 203; // Справка с места регистрации (в суд) для Самары
        public const int act_report_spravka_na_privatizac = 204; // Справка для приватизации
        public const int act_report_liсevoi_schet = 206; // Справка для лицевого счета
        public const int act_report_spravka_sostav_semji = 208; // Справка о составе семьи
        public const int act_report_vipiska_dom = 211; // Справка о составе семьи
        public const int act_report_listok_ubit = 218; // адресный листок убытия
        public const int act_report_listok_pribit = 219; // адресный листок прибытия
        public const int act_report_zay_reg_preb = 214; // заявление о регистрации по месту пребывания
        public const int act_report_zay_reg_git_f6 = 215; // заявление о снятии с регистрационного учета по месту жительства
        public const int act_report_zay_reg_git = 216; // заявление о регистрации по месту жительства
        public const int act_report_rfl1 = 223; // сведения о регистрации гражданина рф по месту жительства рфл1
        public const int act_report_rfl2 = 954; // сведения о регистрации гражданина рф по месту жительства рфл2
        public const int act_report_listok_stat_prib = 224; // листок статистического учета прибытия
        public const int act_report_spis_reg_snyat = 220; // сведения о регистрации граждан и снятии с регистрационного учета
        public const int act_report_spis_vuchet = 221; // сведения о регистрации граждан и снятии с регистрационного учета
        public const int act_report_spis_smena_dok = 222; // реестр граждан, сменивших или получивших удостоверение личности
        public const int act_report_spis_gil = 225; // универсальный реестр граждан
        public const int act_report_smena_passp = 226; //заявление на смену(выдачу) паспорта
        public const int act_report_listok_stat_ubit = 227; // листок статистического учета выбытия
        public const int act_report_report_prm = 228; // генератор отчетов по параметрам
        public const int act_report_kart_analis = 229; // карта аналитического учета
        public const int act_report_sverka_rashet = 230; // генератор отчетов по параметрам - сверка расчетов с жильцом
        public const int act_report_dom_nach = 231; // расшифровка по домам - начисления
        public const int act_report_sprav_suppnach = 232; // справка по поставщикам коммунальных услуг
        public const int act_report_sprav_hasdolg = 233; // справка по отключениям подачи коммунальных услуг        
        public const int act_report_sprav_otkl_uslug = 234; // справка по отключениям подачи коммунальных услуг
        public const int act_report_sprav_v_sud = 235; // справка для предъявления в суд
        public const int act_report_lic_schet_excel = 236; // справка о лицевом счете
        public const int act_report_kart_registr = 237; // карточка регистрации
        public const int act_report_sprav_reg = 238; // Справка архивная
        public const int act_report_sprav_suppnachhar = 239; // справка по поставщикам коммунальных услуг с характеристиками
        public const int act_report_izvechenie_za_mesyac = 240; //извещение за месяц 
        public const int act_report_kvar_kart = 241; //поквартирная карточка
        public const int act_report_spravsmg = 242; //справка с места жительства
        public const int act_report_spravpozapros_smr = 243; //справка по запросам
        public const int act_report_spravpozapros = 339; //справка по запросам
        public const int act_report_serv_supp_nach_tula = 340; //Отчет по начислениям для Тулы
        public const int act_report_serv_supp_money_tula = 341; //Отчет по поступлениям для Тулы
        public const int act_report_vipis_counters = 342; //Выписка по счетчикам для самары
        public const int act_report_list_dom_faktura = 346; //Реестр выданных квитанций по домам
        public const int act_report_vipis_ls_tula = 351; //Выписка из лицевого счета для Тулы
        public const int act_report_svid_registr = 879; //свидетельство о регистрации по МЖ      
        public const int act_report_listok_stat_ubit_obn = 936; //Листок статистического учета выбытия
        public const int act_report_listok_stat_prib_obn = 937; //Листок статистического учета прибытия 
        public const int act_report_zay_reg_git_obn = 938; //Заявление регистрация по МЖ форма 6
        public const int act_report_svid_reg_reb_obn = 939; //Свидетельство с МЖ ребенка Обнинск
        //Должники
        public const int act_report_AllOver = 343; //Общий
        public const int act_report_AllAgreement = 344; //Досудебной работы
        public const int act_report_AllPrikazt = 345; //Судебной работы


        public const int act_report_oplata_uslug_za_post_uslugi = 244; //оплата гражданами-получателями коммунальных услуг за поставленные услуги
        public const int act_report_Inf_PoRashet_SNasel = 245; //Информация по расчетам с населением
        public const int act_report_energo_act = 246; //Акт сверки по Энергосбыту
        public const int act_report_report_Nachisleniya = 247; //Генератор по начислениям
        public const int act_report_sprav_pu_ls = 248; //Справка о начислениях по квартирным ПУ
        public const int act_report_norma_potr = 249; //Сводная ведомость по нормативам потребления
        public const int act_report_sos_gil_fond = 250; //Информация по расчетам с населением
        public const int act_report_dom_nach_pere = 251; //Акт сверки по Энергосбыту
        public const int act_report_sprav_nach_pu = 252; //Генератор по начислениям
        public const int act_report_spis_dolg = 253; //Список должников с указанием срока задолженности
        public const int act_report_ved_dolg = 254; //Сведения о просроченной задолженности
        public const int act_report_nach_opl_serv = 255; //Справка по начислению платы за услуги
        public const int act_report_dom_odpu = 256; //Информация по расчетам с населением
        public const int act_report_ved_opl = 257; //Акт сверки по Энергосбыту
        public const int act_report_ved_pere = 258; //Генератор по начислениям
        public const int act_report_make_kvit = 259; //Генератор по начислениям
        public const int act_report_protokol_odn = 260; //оплата гражданами-получателями коммунальных услуг за поставленные услуги сводный отчет
        public const int act_report_calc_tarif = 261; //Калькуляция тарифа по услуге содержание жилья
        public const int act_report_oplata_uslug_za_post_uslugi_svod = 262; //оплата гражданами-получателями коммунальных услуг за поставленные услуги сводный отчет
        public const int act_report_order_list = 263; // отчет 1.5. Список заявок
        public const int act_report_count_orders_serv = 264; // отчет 2.1. количество заявлений, направленных по услугам за период
        public const int act_report_count_orders_supp = 265; // отчет 2.2. количество заявлений, направленных по поставщикам за период
        public const int act_report_10_14_3 = 266; // отчет сальдовая оборотная ведомость 10.14.3
        public const int act_report_10_14_1 = 267; // отчет сальдовая оборотная ведомость 10.14.1
        public const int act_report_spravsmg2_smr = 268; //справка для незарегистрированного собственника
        public const int act_report_spravsmg2 = 337; //справка для незарегистрированного собственника
        public const int act_report_zakaz = 269; //отчет по заказам
        public const int act_report_sprav_group = 270; //отчет по услугам группы  Содержание жилья 
        public const int act_report_sprav_po_otkl_usl_dom_vinovnik = 271; //отчет по услугам группы  Содержание жилья
        public const int act_report_sprav_po_otkl_usl_geu_vinovnik = 272; //отчет по услугам группы  Содержание жилья 
        public const int act_report_planned_works_list_supp = 273; // отчет 3.1. Список плановых работ - сведения по отключениям услуг по поставщикам
        public const int act_report_planned_works_list = 274; // отчет 3.2. Список плановых работ - сведения по отключениям услуг
        public const int act_report_planned_works_list_act = 275; // отчет 3.3. Список плановых работ - акты по отключениям услуг
        public const int act_report_count_joborder_dest = 276; // отчет 2.3. Количество нарядов-заказов по неисправностям

        public const int act_report_info_from_service = 279; // отчет 1.1. Информация, полученная ОДДС
        public const int act_report_appinfo_from_service = 278; // отчет 1.2. Приложение к информации, полученной ОДДС
        public const int act_report_joborder_period_outstand = 277; // отчет 1.4. Список невыполненных нарядов-заказов к концу периода

        public const int act_report_count_order_readres = 280; // отчет 2.4. Количество переадресаций заявок, принятых ОДДС
        public const int act_report_message_list = 281; // отчет 1.3.1. Список сообщений, зарегестрированных ОДДС
        public const int act_report_message_quest_list = 282; // отчет 1.3.2. Список сообщений, зарегестрированных ОДДС(опрос)
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
        public const int act_report_pasp_ras = 295; //отчет рассогласование с паспортисткой для Самары
        public const int act_report_sprav_supp = 299; //отчет по услугам группы  Содержание жилья 
        public const int act_report_ls_pu_vipiska = 300; //выпиcка из ЛС о поданных показаниях КПУ 
        public const int act_report_spravka_na_privatizac2_smr = 301; // Справка для приватизации2
        public const int act_report_spravka_na_privatizac2 = 338; // Справка для приватизации2
        public const int act_report_prot_calc_odn2 = 302; // протокол рассчитанныз значений ОДН расширенный
        public const int act_report_upload_kassa_3_0 = 303; // выгрузка в кассу 3.0
        public const int act_report_unload_kaprem = 910; //выгрузка в капремонт

        public const int daily_payments = 304; //отчет
        public const int act_report_sverka_month = 305; //отчет сверка за месяц
        public const int act_report_saldo_v_bank = 306; // выгрузка сальдо для банка (dbf)
        public const int act_report_gub_curr_charge = 307;  // состояние текущих начислений по домам (Губкин)
        public const int act_report_gub_itog_oplat = 308;   // итоги оплат по домам
        public const int act_report_sprav_suppnachhar2 = 309; // справка по поставщикам форма 3
        public const int act_report_nach_opl_serv2 = 310; //Справка по начислению платы за услуги форма 2
        public const int stat_gilfond = 311; //отчет статистика состояния жилищного фонда
        public const int act_report_raspiska_docum = 312; //отчет Расписка в получении документов
        public const int act_report_svid_reg_preb = 313; //отчет Свидетельство о регистрации по месту пребывания
        public const int act_report_spravpozapros_gub = 314; //отчет Справка с места жительства для г.Губкин
        public const int act_report_vipiska_ls = 315; //отчет Выписка из лицевого счета
        public const int act_report_zay_privat = 316; //отчет Заявление на приватизацию
        public const int act_report_nachisl_ls = 317; //отчет Статистика начислений по лицевым счетам
        public const int act_report_nachisl_dom = 318; //отчет Статистика начислений по домам
        public const int act_report_nachisl_uch = 319; //отчет Статистика начислений по участкам
        public const int act_report_upload_ipu = 320; //выгрузка данных о показаниях ПУ
        public const int act_report_upload_charge = 321; //выгрузка данных о начислениях
        public const int act_report_upload_reestr = 322; // выгрузка реестра для загрузки в БС
        public const int act_report_soc_protection = 323; // выгрузка начислений в орган социальной защиты населения
        public const int act_report_vip_ls = 911; //отчет Выписка из ЛС (71.2.1.1)

        public const int act_report_saldo_ved_energo = 324; //50.1 Сальдовая ведомость по энергосбыту
        public const int act_report_dolg_ved_energo = 325; //50.2 Ведомость должников

        public const int act_report_protocol_sver_data = 326;//протокол сверки данных
        public const int act_report_protocol_sver_data_ls_dom = 327;//сверка характеристик лицевых счетов и домов
        public const int act_report_pasp_ras_gub = 328; //отчет рассогласование с паспортисткой для г.Губкин
        public const int act_report_sprav_na_prog = 330;//справка на проживание и состав семьи
        public const int act_report_sprav_o_smert_kazan = 331;//справка о смерти(для г.Казань)
        public const int act_report_sprav_po_mest_treb= 878;//40 справка по месту требования

        public const int act_report_vip_dom = 887;   // выписка из домовой книги для Калуге
        public const int act_report_vip_dom_f1 = 902;   // выписка из домовой книги форма 1 для тулы
        public const int act_report_vip_fin_ls = 918;   // Выписка из финансового лицевого счета
        public const int act_report_adresn_listok_pribit = 888;//Адресный листок прибытия
        public const int act_report_sprav_o_smert = 889; //Справка о смерти
        public const int act_report_sprav_s_mesta_git = 890; // Справка с места жительства
        public const int act_report_adresn_listok_ubit = 891;//Адресный листок убытия
        public const int act_report_zayav_o_reg_po_mest_git = 892; //Заявление о регистрации по мету жительства
        public const int act_report_kartoch_reg = 893; //Карточка регистрации
        public const int act_report_listok_st_uch_migr_prib = 894; //Листок ст учета мигр (прибытие)
        public const int act_report_talon_st_uch_migr_vibit = 895; //Талон ст учета мигранта (выбытие)
        public const int act_report_sveden_o_reg_FL_MG = 896; //Сведения о регистрации ФЛ по МЖ
        public const int act_report_spravka_o_sostave_semi = 897; //Справка о составе семьи
        public const int act_report_spravkanezareg = 907;  //Справка для незарегистрированного
        public const int act_report_propiski = 953; //Карточка прописки

        public const int act_report_vrem_zareg = 332; //информация о временно зарегистрированных
        public const int act_report_sobsv = 333; //информация о собственниках
        public const int act_report_voenkomat = 334; //для военкомата
        public const int act_report_vip_kvar = 335; //выписка на жилое помещение
        public const int act_report_vip_dom_gas = 336; //выписка для гор газа
        public const int act_report_address_dept = 900;   //расчет задолженности
        public const int act_report_charge_unload = 903;   //расчет задолженности
        public const int act_report_sved_dolznik = 347; //отчет "Сведения о должниках"

        public const int act_aa_showuk = 721;   //разрез: УК
        public const int act_aa_showbd = 722;   //разрез: Банки данных
        public const int act_aa_showul = 723;   //разрез: Улицы

        public const int act_as_showsupp = 724;   //разрез: Поставщики
        public const int act_as_showserv = 725;   //разрез: Услуги
        public const int act_as_showuk = 726;   //разрез: УК

        public const int act_findls = 501;   //ШП по адресам
        public const int act_findprm = 502;   //ШП по параметрам
        public const int act_findch = 503;   //ШП по начислениям
        public const int act_findgil3 = 504;   //ШП жильцам                                 
        public const int act_findpu = 505;   //ШП показаний ПУ
        public const int act_findnedop = 506;   //ШП по недопоставкам
        public const int act_findodn = 507;   //ШП ОДН
        public const int act_findserv = 508;   //ШП по услугам и поставщикам
        public const int act_findsupg = 509;   //ШП по Заявкам СУПГ                                 
        public const int act_findgroup = 510;   //ШП по группам              
        public const int act_findpack = 511;   //ШП по оплатам
        public const int act_findplannedworks = 512;   //ШП по плановым работам
        public const int act_finddebt = 513;   //ШП по долгам
        public const int act_findeals = 514;   //ШП по делам
        //public const int    act_AdresEE         = 508;   //Адреса Энергосбыта

        public const int act_mode_edit = 611;   //"Открыть данные на изменение"
        public const int act_mode_view = 610;   //"Открыть данные на просмотр"
        public const int act_online = 530;   //"Сейчас на сайте"
        public const int act_blocked = 531;   //"Заблокированные пользователи"

        public const int act_process_in_queue = 532;   //Процессы в очереди
        public const int act_process_active = 533;   //Запущенные
        public const int act_process_finished = 534;   //Завершенные
        public const int act_process_with_errors = 535;   //С ошибками

        public const int rowsview_20 = 701;   //"Выводить по 20 записей"
        public const int rowsview_50 = 702;   //"Выводить по 50 записей"
        public const int rowsview_100 = 703;   //"Выводить по 100 записей"
        public const int rowsview_10 = 705;   //"Выводить по 10 записей"
        public const int act_ubil = 851;   //  Показать убывших
        public const int act_actual = 852;   //  Показать историю
        public const int act_neuch = 853;   //  Показать неучитываемые
        public const int act_arx = 854;   //  Показать архивные карточки

        public const int act_open_gilkart = 861;   //  Открыть карточку жильца
        public const int act_open_gilper = 862;   //  Открыть периоды временного убытия жильца
        public const int act_open_dossier = 863;   //  открыть досье на жильца

        public const int act_transfer = 880;   // перенести 
        public const int act_delete_personal_account = 931;// удалить лс
 
      

        public const int act_gil_owner = 551;   // скопировать в собственники
        public const int act_responsible = 552;   //сделать отвественным
        public const int act_set_responsible = 914;   //назначить отвественного

        public const int act_auto_add_pack_ls = 909; //автоматический ввод оплат
        public const int act_quick_add_pack_ls = 917; //автоматический ввод оплат

        public const int act_add_gil_zap = 915;     //добавить запись для существующего жильца   

        public const int act_replace_packls_to_cur_fin_year = 919;     //добавить запись для существующего жильца   

        public const int role_sql = 101;   //  
        public const int role_sql_wp = 101;   //  
        public const int role_sql_area = 102;   //  
        public const int role_sql_geu = 103;   //  
        public const int role_sql_subrole = 105;   // роль с дополнительным функционалом к выбранной роли / подсистеме
        public const int role_sql_prm = 106;   // ограничение по параметрам
        public const int role_sql_ext = 107;   // расширение роли дополнительными ролями
        public const int role_sql_supp = 120;   //  
        public const int role_sql_serv = 121;   //
        public const int role_sql_bank = 122;   //
        public const int role_sql_payer = 123;   //
        public const int role_sql_server = 124;   //  ограничение по серверу
        public const int role_sql_town = 125;

        public const int role_ext_mm = 201;   //  ExtJS MainMenu
        public const int role_ext_pm = 202;   //  ExtJS подпункты
        public const int role_valid_srv = 210;   //  разрешение на выполнение rest-сервиса
        public const int role_invalid_id = 211;   //  закрытые ext_ID

        //Коды ошибок вызова BARS-сервисов 
        public const int svc_normal = 0;
        public const int svc_wrongdata = -1;
        public const int svc_sqlerror = -2;
        public const int svc_pk_Format = -3;
        public const int svc_pk_Prefix = -4;
        public const int svc_pk_NumLs = -5;
        public const int svc_pk_Bit = -6;
        public const int svc_pk_NotUk = -7;
        public const int svc_pk_NotLs = -8;

        //для автоматического обновления
        public const string encryptpathkey = "zRZ1/BHciushX1T9Ks5WwdRNjgjEEGzFypuKZTmkAOE=";//ключ для шифрования пути к архиву
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
                case Constants.rowsview_20: i = 20; break;
                case Constants.rowsview_50: i = 50; break;
                case Constants.rowsview_100: i = 100; break;

            }
            return i;
        }
        /// <summary>
        /// Максимальное значение поля расхода по ПУ 
        /// </summary>
        public const int max_val_for_pu = 9000000;

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
        public string srvAdresHard = "/adreshard"; //
        public string srvDistrib = "/distrib";
        public string srvOneTimeLoad = "/onetimeload"; //
        public string srvDataImport = "/dataimport"; //
        public string srvSprav = "/sprav"; //
        public string srvCounter = "/counter"; //
        public string srvCharge = "/charge"; //
        public string srvAdmin = "/admin"; //
        public string srvAdminHard = "/adminhard"; //
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
        public string srvSimpleRep = "/simplerep";
        public string srvCalcs = "/calcs";
        public string srvSupg = "/supg";
        public string srvDebitor = "/debitor";
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
        public string srvBaseReport = "/baseReport";
        public string srvExchange = "/exchange";
        public string srvSendedMoney = "/sendedmoney";
        public string srvUnlPassport = "/unlpassport";
    }

}

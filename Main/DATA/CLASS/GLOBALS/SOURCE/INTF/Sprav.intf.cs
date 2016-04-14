using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
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
        Returns SaveSupplier(Supplier finder);

        [OperationContract]
        Returns SavePayer(Payer finder);

        [OperationContract]
        Returns SaveBank(Bank finder);

        [OperationContract]
        List<Payer> PayerBankLoad(Payer finder, enSrvOper oper, out Returns ret);

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
        List<BankPayers> BankPayersLoad(Supplier finder, enTypeOfSupp type, out Returns ret);

        [OperationContract]
        List<BankPayers> BankPayersLoadBC(Supplier finder, enTypeOfSupp type, out Returns ret);

        [OperationContract]
        List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret);

        [OperationContract]
        List<Bank> GetBankType(Bank finder, out Returns ret);

        [OperationContract]
        List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret);

        /// <summary>
        /// Возвращает список формул расчета
        /// </summary>
        /// <param name="finder">Параметры поиска</param>
        /// <param name="ret">Результат выполнения операции</param>
        /// <returns>Список формул</returns>
        [OperationContract]
        List<Formuls> GetFormuls(FormulsFinder finder, out Returns ret);
    }


    //----------------------------------------------------------------------
    [DataContract]
    public struct _PointWebData   //данные об основной базе для web-клиента
    //----------------------------------------------------------------------
    {
        [DataMember]
        public bool isDemo;
        [DataMember]
        public bool is50;
        [DataMember]
        public bool isSamara;         //признак базы Самары - пока в лоб!
        [DataMember]
        public int region;          //регион
        [DataMember]
        public bool isPoint;          //имеется ли центральная база
        [DataMember]
        public bool isFabric;         //установлена фабрика серверов БД
        [DataMember]
        public bool isBroker;         //выполняется коннект к брокеру

        [DataMember]
        public RecordMonth calcMonth; //текущий расчетный месяц
        [DataMember]
        public Dictionary<int, RecordMonth> calcMonthAreas; //текущий расчетный месяц
        [DataMember]
        public List<RecordMonth> calcMonths; //список расчетных месяцев
        [DataMember]
        public RecordMonth beginWork; //дата начала работы системы
        [DataMember]
        public RecordMonth beginCalc; //дата начала расчетов (глубина перерасчетов)
        [DataMember]
        public DateTime dateOper;     //опердень
        /// <summary>
        /// Тип финансовой системы
        /// </summary>
        [DataMember]
        public bool isFinances;   //наличие финсистемы
        [DataMember]
        public bool isInitSuccessfull;   //данные (признаки, расчетные месяцы, банки данных и т.д.) при первом запуске успешно загружены
        /// <summary>
        /// Признак, сохранять ли вводимые показания ПУ в основной банк
        /// </summary>
        [DataMember]
        public bool SaveCounterReadingsToRealBank;
        [DataMember]
        public bool isClone;
        /// <summary>
        /// Запускать фоновые потоки обработки задач
        /// </summary>
        [DataMember]
        public bool StartBackgroundThreads;

        [DataMember]
        public bool isCalcSubsidy;

        [DataMember]
        public RecalcModes recalcMode;

        [DataMember]
        public string MainPageName;

        [DataMember]
        public bool isIpuHasNgpCnt;

        [DataMember]
        public bool isUseSeries; 

        public _PointWebData(bool b)
        {
            isDemo = false;
            is50 = false;
            isSamara = false;
            isPoint = false;
            isFabric = false;
            isBroker = false;
            region = (int)Regions.Region.None;
            calcMonth = new RecordMonth();
            calcMonthAreas = new Dictionary<int,RecordMonth>();
            beginWork = new RecordMonth();
            beginCalc = new RecordMonth();
            calcMonths = new List<RecordMonth>();
            dateOper = DateTime.MinValue;
            isFinances = false;
            isInitSuccessfull = false;
            SaveCounterReadingsToRealBank = false;
            isClone = false;
            StartBackgroundThreads = true;
            isCalcSubsidy = false;
            MainPageName = "";
            recalcMode = RecalcModes.None;
            isIpuHasNgpCnt = false;
            isUseSeries = false;
        }

    }
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Point   //банк данных (s_point)
    //----------------------------------------------------------------------
    {
        string _pref;
        string _point;
        string _bd_old;

        [DataMember]
        public int nzp_graj { get; set; }
        [DataMember]
        public int n { get; set; }
        [DataMember]
        public int flag { get; set; }
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public string point { get { return Utils.ENull(_point); } set { _point = value; } }
        [DataMember]
        public string pref { get { return Utils.ENull(_pref); } set { _pref = value; } } //либо префикс ifmx-базы, либо путь к fdb-базе
        [DataMember]
        public string ol_server { get { return Utils.ENull(_bd_old); } set { _bd_old = value; } }
        [DataMember]
        public string b_kod_erc;

        [DataMember]
        public RecordMonth BeginWork; //дата начала работы системы
        [DataMember]
        public RecordMonth BeginCalc; //дата начала расчетов (глубина перерасчетов)

        [DataMember]
        public int nzp_server { get; set; }  //местоположение сервера БД

        [DataMember]
        public RecordMonth CalcMonth;

        //доступные расчетные месяцы
        [DataMember]
        public List<RecordMonth> CalcMonths;

    };

        //----------------------------------------------------------------------
    [DataContract]
    public struct _Server   //сервер БД (servers)
    //----------------------------------------------------------------------
    {
        string _ip_adr;
        string _login;
        string _pwd;
        string _nserver;
        string _ol_server;
        string _conn;

        [DataMember]
        public bool is_valid { get; set; }

        [DataMember]
        public int nzp_server { get; set; }
        [DataMember]
        public string ip_adr { get { return Utils.ENull(_ip_adr); } set { _ip_adr = value; } }
        [DataMember]
        public string login { get { return Utils.ENull(_login); } set { _login = value; } } //
        [DataMember]
        public string pwd { get { return Utils.ENull(_pwd); } set { _pwd = value; } } //
        [DataMember]
        public string nserver { get { return Utils.ENull(_nserver); } set { _nserver = value; } } //
        [DataMember]
        public string ol_server { get { return Utils.ENull(_ol_server); } set { _ol_server = value; } } //
        [DataMember]
        public string conn { get { return Utils.ENull(_conn); } set { _conn = value; } } //
    };

    /// <summary>
    /// Режимы перерасчета начислений
    /// </summary>
    public enum RecalcModes
    {
        /// <summary>
        /// Не задан
        /// </summary>
        None = 0,

        /// <summary>
        /// Автоматический перерасчет начислений при изменении параметров, влияющих на расчет
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Автоматический перерасчет начислений при изменении параметров, но с возможностью отмены перерасчета
        /// </summary>
        AutomaticWithCancelAbility = 2,

        /// <summary>
        /// Перерасчеты выполняются только по требованию пользователя
        /// </summary>
        Manual = 3
    }

        /// <summary>
    /// Перечислитель 
    /// </summary>
    [DataContract]
    public enum FunctionsTypesGeneratePkod
    {
        /// <summary>
        /// Стандартная генерация
        /// </summary>
        [EnumMember]
        standart = 1,

        /// <summary>
        /// Для Самары
        /// </summary>
        [EnumMember]
        samara = 2,

        /// <summary>
        /// Для Татарстана
        /// </summary>
        [EnumMember]
        tat = 3
    }


    public class PackDistributionParameters
    {
        /// <summary>
        /// Стратегии распределения оплат
        /// </summary>
        public enum Strategies
        {
            /// <summary>
            /// стандартная схема, погашение недействующих услуг приоритетно
            /// </summary>
            InactiveServicesFirst = 1,

            /// <summary>
            /// стандартная схема, погашение действующих услуг приоритетно
            /// </summary>
            ActiveServicesFirst = 2,

            /// <summary>
            /// стандартная схема, погашение действующих и недействующих услуг равноправно
            /// </summary>
            NoPriority = 3,

            /// <summary>
            /// Пропорционально вх. сальдо (если оплата до 20 числа),
            /// Пропорционально начислению за месяц с учетом недопоставок (если оплата 20 числа или позднее)
            /// </summary>
            Samara = 0
        }

        /// <summary>
        /// Способы начисления к оплате
        /// </summary>
        public enum ChargeMethods
        {
            /// <summary>
            /// Исходящее сальдо
            /// </summary>
            Outsaldo = 1,

            /// <summary>
            /// Положительная часть исходящего сальдо
            /// </summary>
            PositiveOutsaldo = 2,

            /// <summary>
            /// Начисления за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат
            /// </summary>
            MonthlyCalculationWithChangesAndOverpayment = 3,

            /// <summary>
            /// Положительная часть начислений за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат
            /// </summary>
            PositiveMonthlyCalculationWithChangesAndOverpayment = 4,

            /// <summary>
            /// Начисления за месяц с учетом перерасчетов, недопоставок и изменений сальдо
            /// </summary>
            MonthlyCalculationWithChanges = 5,

            /// <summary>
            /// Положительная часть начислений за месяц с учетом перерасчетов, недопоставок и изменений сальдо
            /// </summary>
            PositiveMonthlyCalculationWithChanges = 6
        }

        /// <summary>
        /// Эталон для первичного распределения оплат
        /// </summary>
        public enum PaymentDistributionMethods
        {
            /// <summary>
            /// Положительная часть начислено к оплате прошлого месяца
            /// </summary>
            LastMonthPositiveSumCharge = 1,

            /// <summary>
            /// Положительная часть начслено к оплате
            /// </summary>
            LastMonthSumCharge = 2,

            /// <summary>
            /// Положительная часть входящего сальдо текущего месяца
            /// </summary>
            CurrentMonthPositiveSumInsaldo = 3,

            /// <summary>
            /// Входящее сальдо текущего месяца
            /// </summary>
            CurrentMonthSumInsaldo = 4
        }

        /// <summary>
        /// Приоритет погашения долга действующих / недействующих услуг
        /// </summary>
        public Strategies strategy { get; set; }

        /// <summary>
        /// Распределять ли пачки сразу после загрузки
        /// </summary>
        public bool DistributePackImmediately = false;

        /// <summary>
        /// Рассчитывать суммы к перечислению автоматически при распределении/откате оплат
        /// </summary>
        public bool CalcDistributionAutomatically = false;

        /// <summary>
        /// Выполнять ли протоколирование процесса распределения оплат
        /// </summary>
        public bool EnableLog = false;

        /// <summary>
        /// Способ начисления к оплате
        /// </summary>
        public ChargeMethods chargeMethod { get; set; }

        /// <summary>
        /// Способ начисления к оплате
        /// </summary>
        public PaymentDistributionMethods distributionMethod { get; set; }

        /// <summary>
        /// Плательщик заполняет оплату по услугам
        /// </summary>
        public bool AllowSelectServicesWhilePaying = false;

        /// <summary>
        /// Список услуг, имеющих приоритет при распределении оплат
        /// </summary>
        public List<Service> PriorityServices { get; set; }
    }
    //----------------------------------------------------------------------
    public static class Points               //список доступных банков данных
    //----------------------------------------------------------------------
    {
        public static _PointWebData pointWebData;  //необходимые данные об основной базе для web-клиента
        public static _Point Point;

        //public static bool IsSmr;   //признак базы Самары - пока в лоб!
        //public static bool IsPoint; //имеется ли центральная база
        //public static bool IsFabric;//установлена фабрика серверов БД
        //public static DateTime dateOper = DateTime.MinValue;
        //public static RecordMonth CalcMonth; //текущий расчетный месяц
        //public static List<RecordMonth> CalcMonths = new List<RecordMonth>(); //список расчетных месяцев
        //public static RecordMonth BeginWork; //дата начала работы системы
        //public static RecordMonth BeginCalc; //дата начала расчетов (глубина перерасчетов)

        public static bool Is50             { get { return pointWebData.is50; } set { pointWebData.is50 = value; } }
        public static bool IsDemo           { get { return pointWebData.isDemo; } set { pointWebData.isDemo = value; } }
        /// <summary>
        /// Регион установки
        /// </summary>
        public static Regions.Region Region            { get { return Regions.GetById(pointWebData.region); } set { pointWebData.region = (int)value; } }
        public static bool IsSmr            { get { return pointWebData.isSamara; } set { pointWebData.isSamara = value; } }
        public static bool IsPoint          { get { return pointWebData.isPoint; } set { pointWebData.isPoint = value; } }
        public static bool IsFabric         { get { return pointWebData.isFabric; } set { pointWebData.isFabric = value; } }
        public static bool IsCalcSubsidy    { get { return pointWebData.isCalcSubsidy; } set { pointWebData.isCalcSubsidy = value; } }
        /// <summary>
        /// Режим перерасчета начислений
        /// </summary>
        public static RecalcModes RecalcMode { get { return pointWebData.recalcMode; } set { pointWebData.recalcMode = value; } }
        /// <summary>
        /// Тип финансовой системы
        /// </summary>
        public static bool isFinances { get { return pointWebData.isFinances; } set { pointWebData.isFinances = value; } }
        public static DateTime DateOper     { get { return pointWebData.dateOper; } set { pointWebData.dateOper = value; } }
        public static RecordMonth CalcMonth { get { return pointWebData.calcMonth; } set { pointWebData.calcMonth = value; } }
        public static Dictionary<int, RecordMonth> calcMonthAreas { get { return pointWebData.calcMonthAreas; } set { pointWebData.calcMonthAreas = value; } }
        public static RecordMonth BeginWork { get { return pointWebData.beginWork; } set { pointWebData.beginWork = value; } }
        public static RecordMonth BeginCalc { get { return pointWebData.beginCalc; } set { pointWebData.beginCalc = value; } }
        public static bool isInitSuccessfull { get { return pointWebData.isInitSuccessfull; } set { pointWebData.isInitSuccessfull = value; } }   //данные (признаки, расчетные месяцы, банки данных и т.д.) при первом запуске успешно загружены
        /// <summary>
        /// Признак, сохранять ли вводимые показания ПУ в основной банк
        /// </summary>
        public static bool SaveCounterReadingsToRealBank { get { return pointWebData.SaveCounterReadingsToRealBank; } set { pointWebData.SaveCounterReadingsToRealBank = value; } }
        /// <summary>
        /// Признак, что используется клон базы данных
        /// </summary>
        public static bool isClone { get { return pointWebData.isClone; } set { pointWebData.isClone = value; } }
        /// <summary>
        /// Запускать фоновые потоки обработки задач
        /// </summary>
        public static bool StartBackgroundThreads { get { return pointWebData.StartBackgroundThreads; } set { pointWebData.StartBackgroundThreads = value; } }

        /// <summary>
        /// Параметры распределения оплат в финансовой системе
        /// </summary>
        public static PackDistributionParameters packDistributionParameters;
        
        public static List<RecordMonth> CalcMonths { get { return pointWebData.calcMonths; } set { pointWebData.calcMonths = value; } }
         
        public static bool IsMultiHost       { get { return MultiHost.IsMultiHost; }  } //признак мультихостинга (Минстрой)
        public static string Pref;           //префикс центральной базы ifmx:erc_kernel или исходный каталог для fdb:tk0
        public static string g_kod_erc;      //глобальный префикс платежного кода (default)

        public static List<_Point> PointList = new List<_Point>(); //список БД
        public static List<_Server> Servers  = new List<_Server>(); //список локальных серверов БД

        public static string mainPageName { get { return pointWebData.MainPageName; } set { pointWebData.MainPageName = value; } }

        /// <summary>
        /// Признак, что в таблице counters есть поле ngp_cnt
        /// если хотя бы в одном банке поля ngp_cnt нет, то IsIpuHasNgpCnt = false 
        /// </summary>
        public static bool IsIpuHasNgpCnt { get { return pointWebData.isIpuHasNgpCnt; } set { pointWebData.isIpuHasNgpCnt = value; } }

        /// <summary>
        /// Параметр управления новыми записями через series
        /// </summary>
        public static bool isUseSeries { get { return pointWebData.isUseSeries; } set { pointWebData.isUseSeries = value; } }

        /// <summary>
        /// Тип функции генерации платежных кодов
        /// </summary>
        public static FunctionsTypesGeneratePkod functionTypeGeneratePkod { set; get; }

        //
        public static void SetPointWebData(_PointWebData p)
        {
            Points.IsDemo = p.isDemo;
            Points.Is50   = p.is50;
            Points.IsSmr = p.isSamara;
            Points.Region = Regions.GetById(p.region);
            Points.IsPoint = p.isPoint;
            Points.CalcMonth = p.calcMonth;
            Points.calcMonthAreas = p.calcMonthAreas;
            Points.BeginWork = p.beginWork;
            Points.BeginCalc = p.beginCalc;
            Points.CalcMonths = p.calcMonths;
            Points.DateOper = p.dateOper;
            Points.isFinances = p.isFinances;
            Points.isInitSuccessfull = p.isInitSuccessfull;
            Points.SaveCounterReadingsToRealBank = p.SaveCounterReadingsToRealBank;
            Points.isClone = p.isClone;
            Points.StartBackgroundThreads = p.StartBackgroundThreads;
            Points.IsCalcSubsidy = p.isCalcSubsidy;
            Points.mainPageName = p.MainPageName;
            Points.RecalcMode = p.recalcMode;
            Points.IsIpuHasNgpCnt = p.isIpuHasNgpCnt;
            Points.isUseSeries = p.isUseSeries;
        }

        public static _PointWebData GetPointWebData()
        {
            _PointWebData p = new _PointWebData(false);

            p.isDemo = Points.IsDemo;
            p.is50 = Points.Is50;
            p.isSamara = Points.IsSmr;
            p.region = (int)Points.Region;
            p.isPoint = Points.IsPoint;
            p.calcMonth = Points.CalcMonth;
            p.calcMonthAreas = Points.calcMonthAreas;
            p.beginWork = Points.BeginWork;
            p.beginCalc = Points.BeginCalc;
            p.calcMonths = Points.CalcMonths;
            p.dateOper = Points.DateOper;
            p.isFinances = Points.isFinances;
            p.isInitSuccessfull = Points.isInitSuccessfull;
            p.SaveCounterReadingsToRealBank = Points.SaveCounterReadingsToRealBank;
            p.isClone = Points.isClone;
            p.StartBackgroundThreads = Points.StartBackgroundThreads;
            p.isCalcSubsidy = Points.IsCalcSubsidy;
            p.MainPageName = Points.mainPageName;
            p.recalcMode = Points.RecalcMode;
            p.isIpuHasNgpCnt = Points.IsIpuHasNgpCnt;
            p.isUseSeries = Points.isUseSeries;
            return p;
        }

        //
        public static _Server GetServer(int nzp_server)
        {
            foreach (_Server zap in Points.Servers)
            {
                if (nzp_server == zap.nzp_server)
                {
                    return zap;
                }
            }
            return new _Server(); //ошибка!
        }

        //определение префиксов Points.PointList
        static _Point GetPoint(string pref, List<_Point> pointlist)
        {
            int nzp_wp;
            if (Int32.TryParse(pref, out nzp_wp))
            {
                foreach (_Point point in pointlist)
                {
                    if (point.nzp_wp.ToString() == pref) return point;
                }
            }
            else
            {
                foreach (_Point point in pointlist)
                {
                    if (point.pref == pref) return point;
                }
            }
            return new _Point();
        }
        static _Point GetPoint(int nzp_wp, List<_Point> pointlist)
        {
            foreach (_Point point in pointlist)
            {
                if (point.nzp_wp == nzp_wp) return point;
            }
            return new _Point();
        }
        static string GetPref(int nzp_wp, List<_Point> pointlist)
        {
            if (nzp_wp > 0)
                return GetPoint(nzp_wp, pointlist).pref;

            return Points.Pref;
        }
        static int GetPref(string pref, List<_Point> pointlist)
        {
            if (!String.IsNullOrEmpty(pref))
                return GetPoint(pref, pointlist).nzp_wp;

            return 0;
        }

        public static _Point GetPoint(string pref)
        {
            return GetPoint(pref, Points.PointList);
        }
        public static _Point GetPoint(int nzp_wp)
        {
            return GetPoint(nzp_wp, Points.PointList);
        }
        public static string GetPref(int nzp_wp)
        {
            return GetPref(nzp_wp, Points.PointList);
        }
        public static int GetPref(string pref)
        {
            return GetPref(pref, Points.PointList);
        }

        //процедура достает connection по nzp_wp или pref
        public static string GetConnByPref(int nzp_wp, int nzp_server, string pref)
        {
            string conn = Constants.cons_Kernel; //по-умолчанию
            if (!IsFabric)
                return conn;

            if (nzp_wp < 1 && nzp_server < 1 && String.IsNullOrEmpty(pref))
                return conn;

            if (nzp_server < 1)
            {
                foreach (_Point point in PointList)
                {
                    if (point.nzp_wp == nzp_wp || point.pref == pref)
                    {
                        nzp_server = point.nzp_server;
                        break;
                    }
                }
            }

            if (nzp_server < 1)
                return conn;

            foreach (_Server server in Servers)
            {
                if (server.nzp_server == nzp_server)
                {
                    conn = server.conn;
                    break;
                }
            }
            return conn;
        }
        public static string GetConnByPref(int nzp_wp, int nzp_server)
        {
            return GetConnByPref(nzp_wp, nzp_server, "");
        }
        public static string GetConnByPref(string pref)
        {
            return GetConnByPref(0, 0, pref);
        }
        public static string GetConnByServer(int nzp_server)
        {
            return GetConnByPref(0, nzp_server, "");
        }
        public static string GetConnByWp(int nzp_wp)
        {
            return GetConnByPref(nzp_wp, 0, "");
        }
        public static string GetConnKernel(int nzp_wp, int nzp_server)
        {
            return GetConnByPref(nzp_wp, nzp_server, "");
        }
        
        //вытащить номер потока расчета
        public static int GetCalcNum(string pref)
        {
            return GetCalcNum(0, pref);
        }
        public static int GetCalcNum(int nzp_wp)
        {
            return GetCalcNum(nzp_wp, "");
        }
        public static int GetCalcNum(int nzp_wp, string pref)
        {
            int num  = 0;
            if (nzp_wp < 1 && String.IsNullOrEmpty(pref))
                return num;

            foreach (_Point point in PointList)
            {
                if (point.nzp_wp == nzp_wp || point.pref == pref)
                {
                    num = point.flag;
                    break;
                }
            }

            return num;
        }

        /// <summary>
        /// получить расчетный месяц
        /// </summary>
        public static RecordMonth GetCalcMonth(CalcMonthParams prms)
        {
            if (prms.pref != "" && prms.pref != Points.Pref && prms!= null)
            {
                return GetPoint(prms.pref).CalcMonth;
            }
            else return Points.CalcMonth;          
        }

    }
    //----------------------------------------------------------------------
    public struct RecordMonth    //запись месяца
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int year_;        //год
        [DataMember]
        public int month_;       //месяц

        [DataMember]
        public string sid        //стринговый id (ГодМесяц)
        {
            get
            {
                return Utils.PutIdMonth(year_, month_).ToString();
            }
            set
            {
                Utils.GetIdMonth(value, ref year_, ref month_);
            }
        }
        [DataMember]
        public int id           //интегерный id (ГодМесяц)
        {
            get
            {
                return Utils.PutIdMonth(year_,month_);
            }
            set
            {
                Utils.GetIdMonth(value, ref year_, ref month_);
            }
        }
        [DataMember]
        public string name 
        { 
            get 
            {
                if (month_==0)
                    return year_.ToString()+" год";
                else
                    return year_.ToString() + "-" + month_.ToString("00");
            } 
        }
        [DataMember]
        public string name_month
        {
            get
            {
                string s = "Не определено";
                switch (month_)
                {
                    case 1: { s = "Январь";  break; }
                    case 2: { s = "Февраль"; break; }
                    case 3: { s = "Март";    break; }
                    case 4: { s = "Апрель";  break; }
                    case 5: { s = "Май";     break; }
                    case 6: { s = "Июнь";    break; }
                    case 7: { s = "Июль";    break; }
                    case 8: { s = "Август";  break; }
                    case 9: { s = "Сентябрь";break; }
                    case 10:{ s = "Октябрь"; break; }
                    case 11:{ s = "Ноябрь";  break; }
                    case 12:{ s = "Декабрь"; break; }
                }

                return s + " " + year_.ToString();
            }
        }
    }
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
    [DataContract]
    public class _TypeBC : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string name_type { get; set; }
        [DataMember]
        public bool is_active { get; set; }
        [DataMember]
        public List<_TagsBC> tags { get; set; }
    }

    [DataContract]
    public class _TagsBC : Finder
    //----------------------------------------------------------------------
    {      
        [DataMember]
        public byte idTypeTag { get; set; }
        [DataMember]
        public string nameTypeTag { get; set; }
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string nameTag { get; set; }
        [DataMember]
        public string descrTag { get; set; }
        [DataMember]
        public string isRequared { get; set; }
        [DataMember]
        public string isShowEmpty { get; set; }
        [DataMember]
        public Int16 idField { get; set; }
        [DataMember]
        public string nameField { get; set; }
        [DataMember]
        public Int16 num { get; set; }
        [DataMember]
        public int id_bc_type { get; set; }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public struct _Service
    //----------------------------------------------------------------------
    {
        //service_small, service_name, ed_izmer, type_lgot, nzp_frm, ordering "+
        string _service;
        string _service_small;
        string _service_name;
        string _ed_izmer;

        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_cnt { get; set; }
        [DataMember]
        public string service { get { return Utils.ENull(_service); } set { _service = value; } }
        [DataMember]
        public int _checked { get; set; }

        [DataMember]
        public string service_small { get { return Utils.ENull(_service_small); } set { _service_small = value; } }
        [DataMember]
        public string service_name { get { return Utils.ENull(_service_name); } set { _service_name = value; } }
        [DataMember]
        public string ed_izmer { get { return Utils.ENull(_ed_izmer); } set { _ed_izmer = value; } }

        [DataMember]
        public int type_lgot { get; set; }
        [DataMember]
        public int nzp_frm { get; set; }
        [DataMember]
        public int nzp_measure { get; set; }
        [DataMember]
        public int ordering { get; set; }
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

    //----------------------------------------------------------------------
    [DataContract]
    public struct _Supplier
    //----------------------------------------------------------------------
    {
        string _adres_supp;
        string _phone_supp;
        string _geton_plat;
        string _kod_supp;
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
        public string adres_supp { get { return Utils.ENull(_adres_supp); } set { _adres_supp = value; } }
        [DataMember]
        public string phone_supp { get { return Utils.ENull(_phone_supp); } set { _phone_supp = value; } }
        [DataMember]
        public string geton_plat { get { return Utils.ENull(_geton_plat); } set { _geton_plat = value; } }
        [DataMember]
        public string kod_supp { get { return Utils.ENull(_kod_supp); } set { _kod_supp = value; } }

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
    public class CalcMonthParams 
    {      
        [DataMember]
        public string pref { get; set; }

        public CalcMonthParams()
            : base()
        {
            pref = "";
        }

        public CalcMonthParams(string _pref)
        {
            pref = _pref;
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

    /// <summary>
    /// Зарезервированные подрядчики
    /// </summary>
    public enum Payers
    {
        /// <summary>
        /// Ручной платеж
        /// </summary>
        ManualPayment = 1998,

        /// <summary>
        /// Диспетчерская
        /// </summary>
        DispatchingOffice = 79997,

        /// <summary>
        /// Безналичный платеж
        /// </summary>
        NonCashPayment = 79998,

        /// <summary>
        /// Суперпачка
        /// </summary>
        Superpack = 79999
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Payer: Finder
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
        public string bank { get { return Utils.ENull(_bank); } set { _bank = value; } }
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
        public int id_bc_type { set; get; }

        public enum ContragentTypes
        {
            /// <summary>
            /// Неопределенное значение
            /// </summary>
            None = 0,

            /// <summary>
            /// Системный
            /// </summary>
            System = 1,

            /// <summary>
            /// Поставщик услуг
            /// </summary>
            ServiceSupplier = 2,

            /// <summary>
            /// Управляющая организация
            /// </summary>
            UK = 3,

            /// <summary>
            /// Организация, осуществляющая прием платежей
            /// </summary>
            PayingAgent = 4,

            /// <summary>
            /// Прочие
            /// </summary>
            Others = 5
        }


        public Payer(): base()
        {
            nzp_payer = 0;
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
            inn = "";
            kpp = "";
            nzp_type = 0;
            type_name = "";
            id_bc_type = 0;
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
    public class Bank: Finder
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
        public string bank { get { return Utils.ENull(_bank); } set { _bank = value; } }
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

    //----------------------------------------------------------------------
    public struct _Pages //страница aspx
    //----------------------------------------------------------------------
    {
        public int nzp_page;
        public string page_url;
        public string page_menu;
        public string page_name;
        public string page_help;
        public int? group_id;
    };
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Help //помощь
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_hlp { get; set; }
        [DataMember]
        public int cur_page { get; set; }
        [DataMember]
        public int tip { get; set; }
        [DataMember]
        public int kod { get; set; }
        [DataMember]
        public int sort { get; set; }
        [DataMember]
        public string hlp { get; set; }
    };
    //----------------------------------------------------------------------
    public struct _PageShow //взаимосвязи страниц
    //----------------------------------------------------------------------
    {
        public int cur_page;
        public int page_url;
        //public string prm_url;
        public int up_kod;
        public int sort_kod;
        public string img_url;
    };
    public struct _RolePages //права доступа для nzp_user
    //----------------------------------------------------------------------
    {
        public int id;
        public int nzp_page;
        public int nzp_role;
    };
    public struct _RoleActions//права доступа для nzp_user
    //----------------------------------------------------------------------
    {
        public int id;
        public int nzp_page;
        public long nzp_act;
        public int nzp_role;
    }
    //----------------------------------------------------------------------
    public struct _Actions //действия
    //----------------------------------------------------------------------
    {
        public int nzp_act;
        public string act_name;
        public string act_hlp;
    };
    //----------------------------------------------------------------------
    public struct _ActShow //отображение страница - действие
    //----------------------------------------------------------------------
    {
        public int cur_page;
        public int nzp_act;
        public int act_tip;     //тип действий: 0-меню, 1-чекбокслист, 2-дропдаунлист
        public int act_dd;      //группировка дропдаунлистов(act_tip=2): 1-actmenu, 3,4-в списках кол-во строк или сортировка
        public int sort_kod;
        public string img_url;
    };
    //----------------------------------------------------------------------
    public struct _ActLnk //связка страница - действие
    //----------------------------------------------------------------------
    {
        public int cur_page;
        public int nzp_act;
        public int page_url;
    };
    //----------------------------------------------------------------------
    public struct _SysPort //настройка портала
    //----------------------------------------------------------------------
    {
        public int    num_prtd;
        public string val_prtd;
    };
    //----------------------------------------------------------------------
    [DataContract]
    public struct _Arms //АРМы для nzp_user
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int nzp_role { get; set; }
        [DataMember]
        public int page_url { get; set; }
        [DataMember]
        public string role { get; set; }
        [DataMember]
        public string img_url { get; set; }
        [DataMember]
        public string url { get; set; }
    };
    //----------------------------------------------------------------------
    public struct _Roles //права доступа для nzp_user
    //----------------------------------------------------------------------
    {
        public int nzp_role;
        public int cur_page;
        public long kod;
        public int tip;
    };
    public struct _Menu //Меню
    //----------------------------------------------------------------------
    {
        public int cur_page;
        public int page_url;
        //public string prm_url;
        public int up_kod;
        public int sort_kod;
    }
    /// <summary>
    /// права доступа для nzp_user
    /// </summary>
    [DataContract]
    public struct _RolesVal
    {
        [DataMember]
        public int nzp_role { get; set; }
        [DataMember]
        public int tip { get; set; }
        [DataMember]
        public long kod { get; set; }
        [DataMember]
        public string val { get; set; }
    };


    //----------------------------------------------------------------------
    public struct _ExtMM //главное меню в ExtJS
    //----------------------------------------------------------------------
    {
        public int nzp_mm;
        public string mm_text;
        public int mm_sort;
    };
    //----------------------------------------------------------------------
    public struct _ExtPM //подменю в ExtJS
    //----------------------------------------------------------------------
    {
        public int nzp_pm;
        public int nzp_mm;
        public string pm_text;
        public string pm_action;
        public string pm_control;
        public int pm_sort;
    };

    //----------------------------------------------------------------------
    public class LogHis //история сессии
    //----------------------------------------------------------------------
    {
        public LogHis()
        {
            nzp_page = 0;
            kod1 = 0;
            kod2 = 0;
            kod3 = 0;
            idses = "";
        }
        public int nzp_page { get; set; }
        public string idses { get; set; }
        public int kod1 { get; set; }
        public int kod2 { get; set; }
        public int kod3 { get; set; }
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
    };

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
    public class Payments : Finder
    {
        [DataMember]
        public string dat_s { set; get; }
        [DataMember]
        public string dat_po { set; get; }
        [DataMember]
        public List<_Point> points { set; get; }
        [DataMember]
        public string uname { set; get; }
        [DataMember]
        public string name_user { set; get; }
        [DataMember]
        public DataSet data { set; get; }
        [DataMember]
        public int nzp_area { set; get; }
        [DataMember]
        public bool checkCanChangeOperDay { set; get; }
        [DataMember]
        public bool prepareContrDistribReport { set; get; }

        public Payments()
            : base()
        {
            checkCanChangeOperDay = false;
            // подготовить отчет "Контроль распределения оплат"
            prepareContrDistribReport = true;
        }
    }
}


using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;
using System.ComponentModel;
using System.Linq;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_FonTask
    {
        [OperationContract]
        FonProcessorStates PutState(FonProcessorCommands command, out Returns ret);

        [OperationContract]
        FonProcessorStates GetState(out Returns ret);

        [OperationContract]
        void CalcFonTask(int number);
    }

    /// <summary>
    /// Состояния процессора
    /// </summary>
    public enum FonProcessorStates
    {
        //None,
        //Idle,    //бездействие
        Stopped, //остановлена
        Work,    //работает
        //Error    //ошибка
    }

    /// <summary>
    /// Команды процессору обработки заданий
    /// </summary>
    public enum FonProcessorCommands
    {
        /// <summary>
        /// Не определена
        /// </summary>
        None,

        /// <summary>
        /// Запустить
        /// </summary>
        Start,

        /// <summary>
        /// Остановить
        /// </summary>
        Stop
    }

    /// <summary>
    /// Состояния заданий
    /// </summary>
    public enum TaskStates
    {
        /// <summary>
        /// Не определено
        /// </summary>
        None = -2,

        /// <summary>
        /// В процессе выполнения возникли ошибки
        /// </summary>
        Error = -1,

        /// <summary>
        /// Выполняется
        /// </summary>
        InProcess = 0,

        /// <summary>
        /// Выполнено
        /// </summary>
        Done = 2,

        /// <summary>
        /// В очереди на выполнение
        /// </summary>
        New = 3
    }

    /// <summary>
    /// Базовый класс для очереди задач
    /// </summary>
    [DataContract]
    public class TaskQueue
    {
        /// <summary>
        /// Тип задания
        /// </summary>
        [DataMember]
        public ProcessTypes taskType;
        [DataMember]
        public FonProcessorStates cur_state; //текущее состояние процессора
        [DataMember]
        public FonProcessorCommands act_state; //действие над процессором
        [DataMember]
        public string msg;
        [DataMember]
        public int target;

        public TaskQueue(FonProcessorCommands command)
        {
            taskType = ProcessTypes.None;
            cur_state = FonProcessorStates.Stopped;
            act_state = command;
            msg = "";
            target = 1;  //saldo
        }
    }

    [DataContract]
    public class CalcQueue : TaskQueue
    {
        public int Number;

        public CalcQueue(FonProcessorCommands command, int number)
            : base(command)
        {
            Number = number;
        }
    }


    /// <summary>
    /// Структура для расчета и постановки задач в очередь
    /// </summary>
    //    public struct CalcFonTask //структура calcfon - фоновые задания
    //    {
    //        FonTask.Statuses _status;

    //        public int number;

    //            public bool callReportAlone //вызвать calcReport в отдельном потоке расчета
    //        {
    //            get
    //            {
    //                return (task == CalcFonTask.Types.taskFull || task == CalcFonTask.Types.taskCalcCharge || task == CalcFonTask.Types.taskSaldo || task == CalcFonTask.Types.taskWithReval);
    //            }
    //        }
    //        public bool calcFull //вызывается полный расчет (расчет всех инградиентов)
    //        {
    //            get
    //            {
    //                return (task == CalcFonTask.Types.taskFull || task == (int)CalcFonTask.Types.taskDefault);
    //            }
    //        }

    //        public int nzp_key;
    //        public long nzp;  //=nzp_dom или nzp_pack (для task = 222)
    //        public int nzpt;  //=nzp_wp!
    //        public int yy;
    //        public int mm;
    //        public int cur_yy;
    //        public int cur_mm;
    //        public CalcFonTask.Types task;
    //        public string dat_when;
    //        public int nzp_user;
    //        public string txt;

    //        public FonTask.Statuses status
    //        {
    //            get { return _status; }
    //            set { _status = value; }
    //        }

    //        private int kod_info
    //        {
    //            get { return (int)_status; }
    //            set
    //            {
    //                _status = FonTaskStatus.GetStatusId(value);
    //            }
    //        }

    //        public int prior;
    //        public string parameters;   //дополнительное поле для параметров (в БД имеет тип VARCHAR(255))

    //        public CalcFonTask(int _num)
    //            : base()
    //        {
    //            number = _num;

    //            nzp_key = 0;
    //            nzp_user = 0;
    //            nzp = 0;
    //            nzpt = 0;
    //            yy = Points.CalcMonth.year_;
    //            mm = Points.CalcMonth.month_;
    //            cur_yy = Points.CalcMonth.year_;
    //            cur_mm = Points.CalcMonth.month_;
    //            task = (int)CalcFonTask.Types.taskDefault;
    //            _status = FonTask.Statuses.Completed;
    //            prior = 1000;
    //            txt = "";
    //            dat_when = "";
    //            parameters = "";
    //            progress = 0;
    //        }
    //}


    /// <summary>
    /// Фоновое задание
    /// </summary>
    [DataContract]
    public class FonTask : Finder
    {
        /// <summary>
        /// Статусы задания
        /// </summary>
        public enum Statuses
        {
            /// <summary>
            /// Состояние не определено
            /// </summary>
            None = -10,

            /// <summary>
            /// В очереди
            /// </summary>
            InQueue = 3,

            /// <summary>
            /// В процессе выполнения
            /// </summary>
            InProcess = 0,

            /// <summary>
            /// Успешно выполнено
            /// </summary>
            Completed = 2,

            /// <summary>
            /// Не выполнено
            /// </summary>
            Failed = -1,


            New = 1,

            /// <summary>
            /// Выполнено с ошибками
            /// </summary>
            WithErrors = -2,

            #region QTask states
            /// <summary>
            /// Во время обработки возникло системное исключение
            /// </summary>
            [Description("Во время обработки возникло системное исключение")]
            Aborted = 320,

            /// <summary>
            /// Отменено по требованию пользователя
            /// </summary>
            [Description("Отменено по требованию пользователя")]
            Cancelled = 310,

            /// <summary>
            /// Выполнено
            /// </summary>
            [Description("Выполнено")]
            Executed = 300,

            /// <summary>
            /// Запрошена отмена
            /// </summary>
            [Description("Запрошена отмена")]
            CancellationRequired = 210,

            /// <summary>
            /// Запрошено возобновление
            /// </summary>
            [Description("Запрошено возобновление")]
            ResumeRequired = 205,

            /// <summary>
            /// Запрошена приостановка обработки
            /// </summary>
            [Description("Запрошена приостановка обработки")]
            SuspendRequired = 200,

            /// <summary>
            /// Ожидает свободный поток для возобновления
            /// </summary>
            [Description("Ожидает свободный поток для возобновления")]
            WaitingForFreeThread = 115,

            /// <summary>
            /// Приостановлено по требованию пользователя
            /// </summary>
            [Description("Приостановлено по требованию пользователя")]
            Suspended = 110,

            /// <summary>
            /// Выполняется
            /// </summary>
            [Description("Выполняется")]
            Executing = 105,

            /// <summary>
            /// Поставлено в очередь на обработку
            /// </summary>
            [Description("Поставлено в очередь на обработку")]
            Queued = 100,

            /// <summary>
            /// Новое задание
            /// </summary>
            [Description("Новое задание")]
            QTaskNew = 400,
            #endregion
        }

        protected Statuses status;
        protected int kod_info;

        public static Statuses GetStatusById(int id)
        {
            // Я гений!11
            try { return (Statuses)id; }
            catch { return Statuses.None; }
        }

        /// <summary>
        /// Возвращает значение аттрибута Description
        /// </summary>
        /// <typeparam name="T">enum type</typeparam>
        /// <param name="enumerationValue">enum value</param>
        /// <returns>Значение аттрибута Description</returns>
        public static string GetDescription<T>(T enumerationValue)
            where T : struct
        {
            try
            {
                if (!enumerationValue.GetType().IsEnum) throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
                var attribute = typeof(T).GetMember(enumerationValue.ToString()).SingleOrDefault().
                    GetCustomAttributes(typeof(DescriptionAttribute), false).
                    SingleOrDefault() as DescriptionAttribute;
                return attribute.Description;
            }
            catch (Exception ex) { MonitorLog.WriteException("Can't load description of type " + enumerationValue, ex); }
            return string.Empty;
        }

        public static string GetStatusName(Statuses status)
        {
            switch (status)
            {
                case Statuses.InQueue: return "В очереди";
                case Statuses.InProcess: return "Выполняется";
                case Statuses.Completed: return "Выполнено";
                case Statuses.Failed: return "Ошибка";
                case Statuses.New: return "Новое";
                case Statuses.WithErrors: return "Выполнено с ошибками";
                default: return GetDescription<Statuses>(status);
            }
        }

        public static string GetStatusNameById(int id)
        {
            Statuses status = GetStatusById(id);
            return GetStatusName(status);
        }

        public void SetStatus(Statuses status)
        {
            kod_info = (int)status;
        }

        [DataMember]
        public int num { get; set; }

        [DataMember]
        public int KodInfo
        {
            get
            {
                return kod_info;
            }
            set
            {
                kod_info = value;
                status = GetStatusById(value);
            }
        }

        [DataMember]
        public int nzp_key { get; set; }

        [DataMember]
        public Statuses Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                kod_info = (int)status;
            }
        }

        [DataMember]
        public virtual string StatusName
        {
            get
            {
                switch (status)
                {
                    case Statuses.InProcess:
                    case Statuses.Executing:
                    case Statuses.Suspended:
                        return GetStatusName(status) + " (" + progress.ToString("P") + ")";
                    default :
                        return GetStatusName(status);
                }
            }
            set { }
        }

        static public int getKodInfo(int nzpAct)
        {
            switch (nzpAct)
            {
                case Constants.act_process_active: return 0;
                case Constants.act_process_finished: return 2;
                case Constants.act_process_in_queue: return 3;
                case Constants.act_process_with_errors: return -1;
                default: return Constants._ZERO_;
            }
        }

        [DataMember]
        public string dat_in { get; set; }
        [DataMember]
        public string dat_in_po { get; set; }
        [DataMember]
        public string dat_work { get; set; }
        [DataMember]
        public string dat_out { get; set; }
        [DataMember]
        public string txt { get; set; }
        public string prms { get; set; }

        [DataMember]
        public string processName { get { return "Фоновый процесс от " + dat_in; } }
        [DataMember]
        public string processType { get { return "Фоновый процесс"; } }
        [DataMember]
        public ProcessTypes processTypeID { get { return ProcessTypes.None; } }
        [DataMember]
        public decimal progress { get; set; }

        public string dat_when;

        public FonTask()
            : base()
        {
            num = 0;
            nzp_key = 0;
            kod_info = Constants._ZERO_;
            dat_in = "";
            dat_in_po = "";
            dat_work = "";
            dat_out = "";
            txt = "";
            prms = "";
            progress = 0;
            dat_when = "";
        }
    }

    [DataContract]
    public class FonTaskWithYearMonth : FonTask
    {
        public RecordMonth YM, YM_po;

        [DataMember]
        public int month_
        {
            get { return YM.month_; }
            set { YM.month_ = value; }
        }
        [DataMember]
        public int year_
        {
            get { return YM.year_; }
            set { YM.year_ = value; }
        }

        [DataMember]
        public string year_month
        {
            get { return YM.name; }
        }

        [DataMember]
        public int month_po
        {
            get { return YM_po.month_; }
            set { YM_po.month_ = value; }
        }
        [DataMember]
        public int year_po
        {
            get { return YM_po.year_; }
            set { YM_po.year_ = value; }
        }
        public FonTaskWithYearMonth()
            : base()
        {
            YM = Points.CalcMonth;
            YM_po = Points.CalcMonth;
        }
    }

    [DataContract]
    public class SaldoFonTask : FonTaskWithYearMonth
    {
        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public string area { get; set; }

        [DataMember]
        public new string processType { get { return "Расчет сальдо УК"; } }

        [DataMember]
        public new ProcessTypes processTypeID { get { return ProcessTypes.CalcSaldoUK; } }

        [DataMember]
        public new string processName
        {
            get { return processType + " для " + area + " за " + year_month + " от " + dat_in; }
        }

        public SaldoFonTask()
            : base()
        {
            nzp_area = 0;
            area = "";
        }
    }

    [DataContract]
    public class CalcFonTask : FonTaskWithYearMonth
    {
        private Types taskType;
        private int task;

        /// <summary>
        /// Типы задач
        /// </summary>
        public enum Types
        {
            /// <summary>
            /// Неизвестный тип задания
            /// </summary>
            Unknown = -1,

            taskDefault = 0,   //полный цикл для текущего месяца, по-умолчанию
            taskFull = 1,   //полный расчет текущего месяца (CalcReportXX в отдельный процесс)
            taskSaldo = 2,   //расчет только сальдо текущего месяца (CalcReportXX в отдельный процесс)
            taskWithReval = 3,   //расчет с перерасчетом (CalcReportXX считается вместе)

            /// <summary>
            /// Учет распределенных оплат в лицевых счета для управляющей компании
            /// </summary>
            uchetOplatArea = 4,

            /// <summary>
            /// Учет распределенных оплат в лицевых счета для банка данных
            /// </summary>
            uchetOplatBank = 5,
            taskWithRevalOntoListHouses = 6,   //расчет с перерасчетом по списку домов

            taskRefreshAP = 10,  //Обновление АП
            taskKvar = 33,  //расчет с перерасчетом одного лицевого счета

            taskCalcGil = 101, //CalcGilXX      
            taskCalcRashod = 111, //CalcRashod     
            taskCalcNedo = 121, //CalcNedo       
            taskCalcGku = 131, //CalcGkuXX      
            taskCalcCharge = 141, //CalcChargeXX, после выполнения вызывает CalcReportXX в отдельный процесс
            taskCalcChargeForReestr = 142,
            taskCalcChargeForDelReestr = 143,
            taskCalcReport = 200, //CalcReportXX   

            DistributePack = 222,
            DistributeOneLs = 228,
            CancelPackDistribution = 223,
            CancelDistributionAndDeletePack = 224,
            UpdatePackStatus = 227,
            taskCalcSubsidyRequest = 225, //Расчет списка к перечислению
            taskCalcSaldoSubsidy = 226, //Расчет сальдо по подрядчикам
            taskBalanceSelect = 230,//Выбрать дебетовое и кредитовое сальдо ЛС для перераспределения
            taskBalanceRedistr = 231,//перераспределение переплат внутри ЛС

            taskGetFakturaWeb = 301,
            taskToTransfer = 302, //Учесть к перечислению,
            taskPreparePrintInvoices = 303, // подготовка данных для печати счетов
            taskAutomaticallyChangeOperDay = 304, // автоматическая смена операционного дня
            taskDisassembleFile = 305, //Разбор файла "ХАРАКТЕРИСТИКИ ЖИЛОГО ФОНДА И НАЧИСЛЕНИЯ ЖКУ"
            taskLoadFile = 306, //Загрузка файла "ХАРАКТЕРИСТИКИ ЖИЛОГО ФОНДА И НАЧИСЛЕНИЯ ЖКУ"
            taskGeneratePkod = 307, //Генерация платежного кода
            taskLoadKladr = 308, //Загрузка КЛАДР
            taskRecalcDistribSumOutSaldo = 309,
            taskUpdateAddress = 310,
            taskCalculateAnalytics = 311,
            taskLoadFileFromSZ = 312, //Загрузка файла из СЗ
            taskUnloadFileForSZ = 313, //Выгрузка файла для СЗ
            taskLoadFileFromSZpss = 314,//Загрузка файла из СЗ "ответ-поставщику"
            taskInsertProvOnClosedOperDay = 315,//Запись проводок по закрытому опер.дню
            taskInsertProvOnClosedCalcMonth = 316,//Запись проводок по закрытому расчетному месяцу
            taskRePrepareProvOnClosedCalcMonth = 317,//Перезаписать проводки по УК/список ЛС
            ReCalcKomiss = 875, // пересчитать комиссию
            OrderSequences = 876, // упорядочить последовательности
            AddPrimaryKey = 877, //Добавить первичные ключи
            AddIndexes = 878, //Добавить индексы
            AddForeignKey = 879, //Добавить внешние ключи 
            CheckBeforeClosingMonth = 880, //Проверки перед закрытием месяца
            TaskRefreshLSTarif = 881, //Обновление информации о поставщиках по лицевым счетам
            taskChangeOperDay = 882, // смена операционного дня
            taskStartControlPays = 883, // задача на получение отчета контроля оплат
            taskExportParam = 884, //экспорт параметров в ПС администратор
            SetNotNull = 885, //ограничения NOT NULL
            taskLoadFileOnly = 886,
            taskCheckStep = 887,
            taskGenLsPu = 888 //групповые операции- генерация ИПУ
        }


        public static Types GetTypeById(int taskId)
        {
            if (!Enum.IsDefined(typeof(CalcFonTask.Types), taskId))
                taskId = -1;
            return (CalcFonTask.Types)Enum.Parse(typeof(CalcFonTask.Types), taskId.ToString());
        }

        public static bool TaskPack(CalcFonTask.Types task) //признак распределения оплаты
        {
            return (task == CalcFonTask.Types.DistributePack || task == CalcFonTask.Types.CancelPackDistribution || task == CalcFonTask.Types.CancelDistributionAndDeletePack);
        }

        #region Признаки расчета (целый банк дом квартира)
        public static bool TaskCalc(CalcFonTask.Types task) //признак расчета целого банка или дома
        {
            return (task == CalcFonTask.Types.taskDefault || task == CalcFonTask.Types.taskFull || task == CalcFonTask.Types.taskSaldo || task == CalcFonTask.Types.taskWithReval ||
                    task == CalcFonTask.Types.taskCalcGil || task == CalcFonTask.Types.taskCalcRashod || task == CalcFonTask.Types.taskCalcNedo || task == CalcFonTask.Types.taskCalcGku ||
                    task == CalcFonTask.Types.taskCalcCharge || task == CalcFonTask.Types.taskCalcReport || task == CalcFonTask.Types.taskGetFakturaWeb || task == CalcFonTask.Types.taskWithRevalOntoListHouses);
        }

        public static bool TaskCalcKvar(CalcFonTask.Types task) //признак расчета одного лицевого счета
        {
            return (task == CalcFonTask.Types.taskKvar);
        }
        #endregion Признаки расчета (целый банк дом квартира)

        public static bool TaskRefresh(CalcFonTask.Types task) //признак обновления АП
        {
            return (task == CalcFonTask.Types.taskRefreshAP);
        }

        public static bool TaskEqual(CalcFonTask.Types existingTask, CalcFonTask.Types newTask) //признак выполнения идентичной задачи
        {
            return existingTask == newTask;

            //if (TaskPack(task_in) && TaskPack(task))
            //    return true;
            //if (TaskRefresh(task_in) && TaskRefresh(task))
            //    return true;
            //if (TaskCalc(task_in) && TaskCalc(task))
            //    return true;
            //if (TaskCalcKvar(task_in) && TaskCalcKvar(task))
            //    return true;

            //return false;
        }

        /// <summary> Код дома или 0 - полный расчет
        /// </summary>
        [DataMember]
        public int nzp { get; set; }

        [DataMember]
        public int nzpt { get; set; }

        /// <summary> Разновидность расчета (0 - полный)
        /// </summary>
        [DataMember]
        public int Task
        {
            get
            {
                return task;
            }
            set
            {
                task = value;
                taskType = GetTypeById(value);
            }
        }

        /// <summary> Разновидность расчета (0 - полный)
        /// </summary>
        [DataMember]
        public Types TaskType
        {
            get
            {
                return taskType;
            }
            set
            {
                taskType = value;
                task = (int)value;
            }
        }

        /// <summary> Приоритет
        /// </summary>
        [DataMember]
        public int prior { get; set; }

        /// <summary> Номер очереди заданий (соответствует номеру таблицы calc_fon_0, calc_fon_1, ...)
        /// </summary>
        [DataMember]
        public int QueueNumber { get; set; }

        [DataMember]
        public new string processType { get { return "Задачи"; } }

        [DataMember]
        public new ProcessTypes processTypeID { get { return ProcessTypes.CalcNach; } }

        [DataMember]
        public new string processName
        {
            get
            {
                if (QueueNumber == 65535)
                    return txt;

                switch (taskType)
                {
                    case Types.CancelDistributionAndDeletePack: return "Удаление пачки оплат";
                    case Types.CancelPackDistribution: return "Отмена распределения пачки оплат";
                    case Types.DistributeOneLs: return "Распределение оплаты";
                    case Types.DistributePack: return "Распределение пачки оплат";
                    case Types.taskAutomaticallyChangeOperDay: return "Смена операционного дня по расписанию";
                    case Types.taskCalcCharge: return "Расчет начислений и сальдо";
                    case Types.taskCalcGil: return "Расчет количества жильцов";
                    case Types.taskCalcGku: return "Расчет тарифов и учет расходов";
                    case Types.taskCalcNedo: return "Расчет недопоставок";
                    case Types.taskCalcRashod: return "Расчет расходов коммунальных";
                    case Types.taskCalcReport: return "Подсчет сводной информации по домам";
                    case Types.taskDisassembleFile: return "Разбор файла с наследуемой информацией";
                    case Types.taskFull: return "Полный расчет без перерасчета";
                    case Types.taskGeneratePkod: return "Присвоение платежных кодов";
                    case Types.taskGetFakturaWeb: return "Формирование платежного документа для портала";
                    case Types.taskKvar: return "Расчет лицевого счета";
                    case Types.taskLoadFile: return "Загрузка файла";
                    case Types.taskLoadFileFromSZ: return "Загрузка файла из СЗ";
                    case Types.taskLoadFileFromSZpss: return "Загрузка файла из СЗ ответ-поставщику";
                    case Types.taskLoadFileOnly: return "Загрузка из файла";
                    case Types.taskUnloadFileForSZ: return "Выгрузка файла для СЗ";
                    case Types.taskLoadKladr: return "Загрузка КЛАДР";
                    case Types.taskPreparePrintInvoices: return "Подготовка данных для печати счетов";
                    case Types.taskRefreshAP: return "Обновление адресов центрального банка";
                    case Types.taskSaldo: return "Расчет сальдо текущего месяца";
                    case Types.taskToTransfer: return "Учет средств к перечислению поставщикам услуг";
                    case Types.taskWithReval: return "Полный расчет с перерасчетом";
                    case Types.taskWithRevalOntoListHouses: return "Полный расчет с перерасчетом по списку выбранных домов";
                    case Types.uchetOplatArea: return "Учет распределенных оплат в лицевых счетах управляющей компании";
                    case Types.taskCalcChargeForReestr: return "Подсчет сальдо для реестра";
                    case Types.taskCalcChargeForDelReestr: return "Подсчет сальдо для лицевых счетов, входящих в удаленный реестр";
                    case Types.uchetOplatBank: return "Учет распределенных оплат в лицевых счетах банка данных";
                    case Types.UpdatePackStatus: return "Обновление статуса пачки";
                    case Types.ReCalcKomiss: return "Расчет комиссий с оплат";
                    case Types.taskRecalcDistribSumOutSaldo: return "Перерасчет исходящего сальдо";
                    case Types.taskUpdateAddress: return "Обновление адресов";
                    case Types.taskCalculateAnalytics: return "Подсчет аналитики";
                    case Types.OrderSequences: return "Упорядочивание последовательностей";
                    case Types.AddPrimaryKey: return "Добавление первичных ключей";
                    case Types.AddIndexes: return "Добавление индексов";
                    case Types.AddForeignKey: return "Добавление внешних ключей";
                    case Types.CheckBeforeClosingMonth: return "Проверка перед закрытием месяца";
                    case Types.TaskRefreshLSTarif: return "Обновление информации о поставщиках по лицевым счетам";
                    case Types.taskStartControlPays: return "Выполнение отчета \'Контроль распределения оплат\'";
                    case Types.taskChangeOperDay: return "Выполнение операции Закрытия операционного дня";
                    case Types.taskExportParam: return "Экспорт параметров в ПС Администратор";
                    case Types.SetNotNull: return "Установка ограничений для полей обязательных для заполнения";
                    case Types.taskCheckStep: return "Проверка целостности данных";
                    case Types.taskGenLsPu: return "Генерация индивидуальных ПУ";
                    case Types.taskBalanceSelect: return "Отбор переплат";
                    case Types.taskBalanceRedistr: return "Распределение переплат";
                    case Types.taskInsertProvOnClosedCalcMonth: return "Запись проводок по закрытому расчетному месяцу";
                    case Types.taskInsertProvOnClosedOperDay: return "Запись проводок по закрытому оперативному дню";
                    case Types.taskRePrepareProvOnClosedCalcMonth: return "Переформирование проводок";
                    default: return "";
                }
                //string name = processType;
                //if (nzp > 0) name += " дома";
                //name += " за " + year_month + " от " + dat_in;
                //return name;
            }
        }

        public string parameters;   //дополнительное поле для параметров (в БД имеет тип VARCHAR(255))

        public bool callReportAlone //вызвать calcReport в отдельном потоке расчета
        {
            get
            {
                return (taskType == CalcFonTask.Types.taskFull || taskType == CalcFonTask.Types.taskCalcCharge
                    || taskType == CalcFonTask.Types.taskSaldo || taskType == CalcFonTask.Types.taskWithReval
                    || taskType == CalcFonTask.Types.taskWithRevalOntoListHouses);
            }
        }
        public bool calcFull //вызывается полный расчет (расчет всех инградиентов)
        {
            get
            {
                return (taskType == CalcFonTask.Types.taskFull || taskType == CalcFonTask.Types.taskDefault);
            }
        }

        public CalcFonTask()
            : base()
        {
            nzp = Constants._ZERO_;
            nzpt = 0;
            task = Constants._ZERO_;
            prior = 0;
            QueueNumber = Constants._ZERO_;
        }

        public CalcFonTask(int queueNumber)
            : this()
        {
            QueueNumber = queueNumber;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendFormat("Название задачи: '{0}'{1}", processName == null ? "" : processName.Trim(), Environment.NewLine);
            sb.AppendFormat("Тип задачи: '{0}'{1}", taskType, Environment.NewLine);
            sb.AppendFormat("ID пользователя, запустившего задачу: {0}{1}", nzp_user, Environment.NewLine);
            sb.AppendFormat("Номер очереди заданий: {0}{1}", QueueNumber, Environment.NewLine);
            sb.AppendFormat("Дополнительные параметры: '{0}'{1}", parameters == null ? "" : parameters.Trim(), Environment.NewLine);
            return sb.ToString();
        }
    }

    [DataContract]
    public class BillFonTask : FonTaskWithYearMonth
    {
        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public int nzp_geu { get; set; }
        [DataMember]
        public string geu { get; set; }
        [DataMember]
        public int count_list_in_pack { get; set; }
        [DataMember]
        public int kod_sum_faktura { get; set; }
        [DataMember]
        public string result_file_type { get; set; }
        [DataMember]
        public int id_faktura { get; set; }
        [DataMember]
        public bool with_dolg { get; set; }
        [DataMember]
        public bool with_uk { get; set; }
        [DataMember]
        public bool with_geu { get; set; }
        [DataMember]
        public bool with_uchastok { get; set; }
        [DataMember]
        public bool with_close_ls { get; set; }
        [DataMember]
        public bool with_zero { get; set; }

        [DataMember]
        public string file_name { get; set; }

        [DataMember]
        public new string processType { get { return "Формирование платежных документов"; } }

        [DataMember]
        public new ProcessTypes processTypeID { get { return ProcessTypes.Bill; } }

        [DataMember]
        public new string processName
        {
            get { return processType + " для: управляющей организации: " + area + ", локальный банк: " + point + ", отделение:" + geu + " за " + year_month + " от " + dat_in; }
        }

        [DataMember]
        public override string StatusName
        {
            get
            {
                Statuses stat = GetStatusById(kod_info);

                if (stat == Statuses.InProcess || stat == Statuses.Failed)
                    return GetStatusName(stat) + " (" + progress.ToString("P") + ")";
                else return GetStatusName(stat);
            }
            set { }
        }

        public BillFonTask()
            : base()
        {
            nzp_area = 0;
            area = "";
            nzp_geu = 0;
            geu = "";
            nzp_wp = 0;
            point = "";
            count_list_in_pack = 0;
            kod_sum_faktura = 0;
            result_file_type = "";
            id_faktura = 0;
            with_dolg = false;
            with_geu = with_uchastok = with_uk = false;
            file_name = "";
            with_close_ls = false;
            with_zero = false;
        }

        public BillFonTask(int id, decimal prgrss)
            : this()
        {
            nzp_key = id;
            progress = prgrss;
        }
    }

}

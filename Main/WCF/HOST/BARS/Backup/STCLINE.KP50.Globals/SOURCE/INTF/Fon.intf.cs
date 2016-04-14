using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_FonTask
    {
        [OperationContract]
        FonTask PutState(FonTask fon, out Returns ret);

        [OperationContract]
        FonTask GetState(out Returns ret);

        [OperationContract]
        void CalcFon(int number);
    }

    /// <summary>
    /// Команды процессору обработки заданий и состояния процессора
    /// </summary>
    public enum enFonState
    {
        act_start, //на запуск
        act_pause, //приостановить 
        act_break, //на остановку

        none,      //бездействие

        cur_stop,  //остановлена
        cur_work,  //работает
        cur_error  //ошибка
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
    /// Базовый класс для фоновых задач
    /// </summary>
    [DataContract]
    public class FonTask
    {
        /// <summary>
        /// Тип задания
        /// </summary>
        [DataMember]
        public ProcessTypes taskType;
        [DataMember]
        public enFonState cur_state; //текущее состояние процесса
        [DataMember]
        public enFonState act_state; //действие над процессом
        [DataMember]
        public string msg;
        [DataMember]
        public int target;

        public FonTask()
        {
            taskType = ProcessTypes.None;
            cur_state = enFonState.none;
            act_state = enFonState.none;
            msg       = "";
            target    = 1;  //saldo
        }
    }

    public class FonTaskType
    {
        public static FonTaskTypeIds GetIdByInt(int taskId)
        {
            if (taskId == (int)FonTaskTypeIds.taskDefault) return FonTaskTypeIds.taskDefault;
            else if (taskId == (int)FonTaskTypeIds.taskFull) return FonTaskTypeIds.taskFull;
            else if (taskId == (int)FonTaskTypeIds.taskSaldo) return FonTaskTypeIds.taskSaldo;
            else if (taskId == (int)FonTaskTypeIds.taskWithReval) return FonTaskTypeIds.taskWithReval;
            else if (taskId == (int)FonTaskTypeIds.taskCalcGil) return FonTaskTypeIds.taskCalcGil;
            else if (taskId == (int)FonTaskTypeIds.taskCalcRashod) return FonTaskTypeIds.taskCalcRashod;
            else if (taskId == (int)FonTaskTypeIds.taskCalcNedo) return FonTaskTypeIds.taskCalcNedo;
            else if (taskId == (int)FonTaskTypeIds.taskCalcGku) return FonTaskTypeIds.taskCalcGku;
            else if (taskId == (int)FonTaskTypeIds.taskCalcCharge) return FonTaskTypeIds.taskCalcCharge;
            else if (taskId == (int)FonTaskTypeIds.taskCalcReport) return FonTaskTypeIds.taskCalcReport;
            else if (taskId == (int)FonTaskTypeIds.DistributePack) return FonTaskTypeIds.DistributePack;
            else if (taskId == (int)FonTaskTypeIds.DistributeOneLs) return FonTaskTypeIds.DistributeOneLs;
            else if (taskId == (int)FonTaskTypeIds.CancelPackDistribution) return FonTaskTypeIds.CancelPackDistribution;
            else if (taskId == (int)FonTaskTypeIds.CancelDistributionAndDeletePack) return FonTaskTypeIds.CancelDistributionAndDeletePack;
            else if (taskId == (int)FonTaskTypeIds.UpdatePackStatus) return FonTaskTypeIds.UpdatePackStatus;
            else if (taskId == (int)FonTaskTypeIds.uchetOplatArea) return FonTaskTypeIds.uchetOplatArea;
            else if (taskId == (int)FonTaskTypeIds.taskGetFakturaWeb) return FonTaskTypeIds.taskGetFakturaWeb;
            else return FonTaskTypeIds.Unknown;
        }

        public static bool TaskPack(FonTaskTypeIds task) //признак распределения оплаты
        {
            return (task == FonTaskTypeIds.DistributePack || task == FonTaskTypeIds.CancelPackDistribution || task == FonTaskTypeIds.CancelDistributionAndDeletePack);
        }

        #region Признаки расчета (целый банк дом квартира)
        public static bool TaskCalc(FonTaskTypeIds task) //признак расчета целого банка или дома
        {
            return (task == FonTaskTypeIds.taskDefault || task == FonTaskTypeIds.taskFull || task == FonTaskTypeIds.taskSaldo || task == FonTaskTypeIds.taskWithReval ||
                    task == FonTaskTypeIds.taskCalcGil || task == FonTaskTypeIds.taskCalcRashod || task == FonTaskTypeIds.taskCalcNedo || task == FonTaskTypeIds.taskCalcGku ||
                    task == FonTaskTypeIds.taskCalcCharge || task == FonTaskTypeIds.taskCalcReport || task == FonTaskTypeIds.taskGetFakturaWeb);
        }

        public static bool TaskCalcKvar(FonTaskTypeIds task) //признак расчета одного лицевого счета
        {
            return (task == FonTaskTypeIds.taskKvar);
        }
        #endregion Признаки расчета (целый банк дом квартира)

        public static bool TaskRefresh(FonTaskTypeIds task) //признак обновления АП
        {
            return (task == FonTaskTypeIds.taskRefreshAP);
        }

        public static bool TaskEqual(FonTaskTypeIds task_in, FonTaskTypeIds task) //признак выполнения идентичной задачи
        {
            if (TaskPack(task_in) && TaskPack(task))
                return true;
            if (TaskRefresh(task_in) && TaskRefresh(task))
                return true;
            if (TaskCalc(task_in) && TaskCalc(task))
                return true;
            if (TaskCalcKvar(task_in) && TaskCalcKvar(task))
                return true;

            return false;
        }
    }

    /// <summary>
    /// Типы задач
    /// </summary>
    public enum FonTaskTypeIds
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
        
        taskRefreshAP  = 10,  //Обновление АП
        taskKvar       = 33,  //расчет с перерасчетом одного лицевого счета

        taskCalcGil = 101, //CalcGilXX      
        taskCalcRashod = 111, //CalcRashod     
        taskCalcNedo = 121, //CalcNedo       
        taskCalcGku = 131, //CalcGkuXX      
        taskCalcCharge = 141, //CalcChargeXX, после выполнения вызывает CalcReportXX в отдельный процесс
        taskCalcReport = 200, //CalcReportXX   

        DistributePack = 222,
        DistributeOneLs = 228,
        CancelPackDistribution = 223,
        CancelDistributionAndDeletePack = 224,
        UpdatePackStatus = 227,
        taskCalcSubsidyRequest = 225, //Расчет списка к перечислению
        taskCalcSaldoSubsidy = 226, //Расчет сальдо по подрядчикам
        taskGetFakturaWeb = 301
    }

    public struct CalcFon //структура calcfon - фоновые задания
    {
        int _number;

        public int number
        {
            get
            {
                return _number;
            }
            set
            {
                _number = value;
            }

        }
        public bool callReportAlone //вызвать calcReport в отдельном потоке расчета
        {
            get
            {
                return (task == FonTaskTypeIds.taskFull || task == FonTaskTypeIds.taskCalcCharge || task == FonTaskTypeIds.taskSaldo || task == FonTaskTypeIds.taskWithReval);
            }
        }
        public bool calcFull //вызывается полный расчет (расчет всех инградиентов)
        {
            get
            {
                return (task == FonTaskTypeIds.taskFull || task == (int)FonTaskTypeIds.taskDefault);
            }
        }
        public int nzp_key;
        public long nzp;  //=nzp_dom или nzp_pack (для task = 222)
        public int nzpt;  //=nzp_wp!
        public int nzp_user;
        public int yy;
        public int mm;
        public int cur_yy;
        public int cur_mm;
        public FonTaskTypeIds task;
        public int kod_info;
        public int prior;
        public string txt;

        public CalcFon(int _num)
        {
            _number = _num;

            nzp_key = 0;
            nzp_user = 0;
            nzp = 0;
            nzpt = 0;
            yy = Points.CalcMonth.year_;
            mm = Points.CalcMonth.month_;
            cur_yy = Points.CalcMonth.year_;
            cur_mm = Points.CalcMonth.month_;
            task = (int)FonTaskTypeIds.taskDefault;
            kod_info = 0;
            prior = 1000;
            txt = "";
        }
    }
   
}

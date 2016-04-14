using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Web.Script.Serialization;

namespace Bars.Billing.IncrementalDataLoader
{
    public static class ConfigurateReader
    {
        public static string[] GetConfig()
        {
            return new[] { 
                ConfigurationManager.AppSettings["connString"], 
                ConfigurationManager.AppSettings["psql"], 
                ConfigurationManager.AppSettings["server"],
                ConfigurationManager.AppSettings["database"],
                ConfigurationManager.AppSettings["port"],
                ConfigurationManager.AppSettings["user"],
                ConfigurationManager.AppSettings["password"] };
        }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class AssemblyAttribute : Attribute
    {
        /// <summary>
        /// Наименование формата
        /// </summary>
        public string FormatName { get; set; }
        /// <summary>
        /// Версия формата
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Имя при регистрации
        /// </summary>
        public string RegistrationName { get; set; }
        /// <summary>
        /// Путь до загружаемого файла
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Наименование загружаемого файла
        /// </summary>
        public string FileName { get; set; }
        [ScriptIgnore]
        public Type type { get; set; }
    }
    public class AssemblyAtr
    {
        /// <summary>
        /// Наименование формата
        /// </summary>
        public string FormatName { get; set; }
        /// <summary>
        /// Версия формата
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Имя при регистрации
        /// </summary>
        public string RegistrationName { get; set; }
        /// <summary>
        /// Путь до загружаемого файла
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Наименование загружаемого файла
        /// </summary>
        public string FileName { get; set; }
        [ScriptIgnore]
        public Type type { get; set; }
    }

    /// <summary>
    /// Статусы проверщика
    /// </summary>
    public enum Statuses
    {
        [Description("Добавлена")]
        Added = 1,//Добавлена
        [Description("Выполняется")]
        Execute = 2,//Выполняется
        [Description("Остановлено")]
        Stopped = 3,//Остановлено
        [Description("Завершено")]
        Finished = 4,//Завершено
        [Description("Завершено с ошибкой(-ами)")]
        Error = 5,//Завершено с ошибкой
        [Description("В очереди на выполнение")]
        InQueue = 6,//В очереди на выполнени
    }
    public class MainHeader : Header
    {
        public int num_load { get; set; }
        public DateTime? dat_month { get; set; }
        public string org_name { get; set; }
        public string org_podr_name { get; set; }
        public string org_inn { get; set; }
        public string org_kpp { get; set; }
        public int org_file_num { get; set; }
        public string sender_phone { get; set; }
        public string sender_fio { get; set; }
        public int count_ls { get; set; }
        public decimal sum_charge { get; set; }
        public DateTime? date_load { get; set; }
        public string file_name { get; set; }
        public Int64 file_size { get; set; }
        public Int16 isclosed { get; set; }
        public DateTime? dat_closed { get; set; }
        public Int16 is_actual { get; set; }
        public DateTime? dat_revert { get; set; }
        public int nzp_user { get; set; }
        public int nzp_user_revert { get; set; }
    }
    public class Header
    {
        public int lineType
        {
            get { return 1; }
        }
        public string formatVersion { get; set; }
        public string nameOrgPasses { get; set; }
        public string podrOrgPasses { get; set; }
        public string INN { get; set; }
        private string _KPP { get; set; }
        public string KPP
        {
            get { return string.IsNullOrEmpty(_KPP) ? "0" : _KPP; }
            set { _KPP = value; }
        }
        public string raschSchet { get; set; }
        public string fileNumber { get; set; }
        public string fileDate { get; set; }
        public string passNumber { get; set; }
        public string passName { get; set; }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public string lsCount { get; set; }

        public string datSysStart { get; set; }

        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim())).ToArray();
            return string.Join("|", strs);
        }

    }
    public class Request : MainHeader
    {
        public long GisFileId { get; set; }

        /// <summary>
        /// Номер элемента 
        /// </summary>
        public int Num { get; set; }
        public int nzp_load { get; set; }
        public string schema { get; set; }
        public string db { get; set; }
        public string connectionString { get; set; }
        /// <summary>
        /// Наименование файла
        /// </summary>
        public string FileName { get; set; }
        public string Path { get; set; }
        public string RegistrationName { get; set; }
        /// <summary>
        /// Наименование формата
        /// </summary>
        public string Format { get; set; }
        /// <summary>
        ///Версия формата
        /// </summary>
        public double Version { get; set; }
        /// <summary>
        /// Строка с результатом
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// Строка со статусом
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Статус в формате enum
        /// </summary>
        public Statuses StatusID { get; set; }
        /// <summary>
        /// Прогресс выполнения
        /// </summary>
        public decimal progress { get; set; }
        /// <summary>
        /// Ссылка на протокол
        /// </summary>
        public string link { get; set; }
        /// <summary>
        /// Полное наименование типа, необходимо для загрузки задача из файла
        /// </summary>
        public string TypeName { get; set; }

        public int month { get; set; }
        public int year { get; set; }
        [ScriptIgnore]
        public Type type { get; set; }

        public string AbsolutePath { get; set; }
        public DateTime? date_charge { get; set; }
    }
    public class Returns
    {
        public Returns(bool result = true, string resultMessage = "")
        {
            this.result = result;
            this.resultMessage = resultMessage;
        }
        /// <summary>
        /// Сообщение
        /// </summary>
        public string resultMessage { get; set; }
        /// <summary>
        /// Рузультат
        /// </summary>
        public bool result { get; set; }
    }
    public class ProgressArgs : EventArgs
    {

        public ProgressArgs(decimal progress, int nzp_load)
        {
            this.progress += progress;
            this.nzp_load = nzp_load;
            if (this.progress > 1) this.progress = 1;
        }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int nzp_load { get; private set; }
        /// <summary>
        /// Текущий прогресс
        /// </summary>
        public decimal progress { get; private set; }
    }
    /// <summary>
    /// Класс параметров события остановки потока
    /// </summary>
    public class StopArgs : EventArgs
    {
        public StopArgs(int nzp_load, bool is_alive)
        {
            this.nzp_load = nzp_load;
            this.is_alive = is_alive;
        }
        public bool is_alive { get; set; }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int nzp_load { get; private set; }
    }
    /// <summary>
    /// Класс события отправки сообщения клиентской программе
    /// </summary>
    public class SendArgs : EventArgs
    {

        public SendArgs(string Message, Statuses result, int nzp_load)
        {
            this.Message = Message;
            this.nzp_load = nzp_load;
            this.result = result;
        }
        public SendArgs(string Message, Statuses result, int nzp_load, string link)
        {
            this.Message = Message;
            this.nzp_load = nzp_load;
            this.result = result;
            this.link = link;
        }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int nzp_load { get; private set; }
        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get; private set; }
        /// <summary>
        /// Результат
        /// </summary>
        public Statuses result { get; set; }
        public string link { get; set; }
    }

    public delegate void ProgressEventHandler(object sender, ProgressArgs e);
    public delegate void StopEventHandler(object sender, StopArgs e);
    public delegate void SendEventHandler(object sender, SendArgs e);

    public class User
    {
        public int nzp_user { get; set; }
        public int kod_uk { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public string username { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public int roles { get; set; }
        public int is_actual { get; set; }
    }

    public class ConfigurationParams
    {
        public string connectionString { get; set; }
        public string database { get; set; }
        public string port { get; set; }
        public string server { get; set; }
        public string password { get; set; }
        public string user { get; set; }
        public string psqlPath { get; set; }
    }

    public class OtherParams
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public int nzp_user { get; set; }
        public string RegistrationName { get; set; }//Наименование формата
        public long GisFileId { get; set; }
        public string OriginalName { get; set; }

        /// <summary>
        /// Активность логгера
        /// true: включен, false: выключен 
        /// </summary>
        public bool EnableLogger { get; set; }

    }

    public class LoadProtocolEntity
    {
        /// <summary>
        /// Платежный код
        /// </summary>
        public long AccountPaymentCode { get; set; }

        /// <summary>
        /// ФИО Квартиросъемщика / Собственника / Нанимателя
        /// </summary>
        public string Fio { get; set; }

        /// <summary>
        /// Номер квартиры
        /// </summary>
        public string FlatNumber { get; set; }

        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string ErrorText { get; set; }

        /// <summary>
        /// Код ЕРЦ
        /// </summary>
        public long ErcCode { get; set; }
        
        /// <summary>
        /// Расчетный месяц
        /// </summary>
        public string ChargeMonth { get; set; }

    }

    public enum ELogType
    {
        Error = 1,
        Info = 2,
        Warn = 3,
        Debug = 4,
        Fatal = 5
    }

    /*
    public class ListQueue<T> : List<T>
    {
        new public void Add(T item) { throw new NotSupportedException(); }
        new public void AddRange(IEnumerable<T> collection) { throw new NotSupportedException(); }
        new public void Insert(int index, T item) { throw new NotSupportedException(); }
        new public void InsertRange(int index, IEnumerable<T> collection) { throw new NotSupportedException(); }
        new public void Reverse() { throw new NotSupportedException(); }
        new public void Reverse(int index, int count) { throw new NotSupportedException(); }
        new public void Sort() { throw new NotSupportedException(); }
        new public void Sort(Comparison<T> comparison) { throw new NotSupportedException(); }
        new public void Sort(IComparer<T> comparer) { throw new NotSupportedException(); }
        new public void Sort(int index, int count, IComparer<T> comparer) { throw new NotSupportedException(); }

        public void Enqueue(T item)
        {
            base.Add(item);
        }

        public T Dequeue()
        {
            var t = base[0];
            base.RemoveAt(0);
            return t;
        }

        public T Peek()
        {
            return base[0];
        }
    }
    */
}

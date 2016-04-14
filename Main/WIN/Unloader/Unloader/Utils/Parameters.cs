using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unloader
{
    /// <summary>
    /// Атрибуты класса формата загрузки
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssembleAttribute : Attribute
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
    }

    public class AppConfig
    {
        /// <summary>Имя системного пользователя</summary>
        public string UserName { get; set; }

        /// <summary>Пароль системного пользователя</summary>
        public string UserPassword { get; set; }

        /// <summary>Таймаут повторного входа в минутах</summary>
        public int UsersTimeout { get; set; }

        /// <summary>Отчеты выполняются удаленно (отдельным сервисов)</summary>
        public bool RemoteExecuteReport { get; set; }

        /// <summary>Адрес сервера очередей</summary>
        public string QueueHost { get; set; }

        /// <summary>Параметры wcf для хоста</summary>
        public WCFParamsType AddressWcfWeb { get; set; }

        /// <summary>Параметры wcf для web</summary>
        public WCFParamsType AddressWcfHost { get; set; }

        /// <summary>Основные настройки БД</summary>
        public DbParams MainDbParams { get; set; }

        /// <summary>Настройки БД хост</summary>
        public DbParams HostDbParams { get; set; }

        /// <summary>Настройки БД web</summary>
        public DbParams WebDbParams { get; set; }

        /// <summary> Логгировать ли все события/запросы в хосте </summary>
        public bool FullLogging { get; set; }
    }

    public class DbParams
    {
        /// <summary>Строка подключения</summary>
        public string ConnectionString { get; set; }

        /// <summary>Имя главной схемы/базы</summary>
        public string MainSchemaName { get; set; }

        /// <summary>Время ожидания снятия блокировки с таблиц</summary>
        public int DbWaitingTimeout { get; set; }
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
        Error = 5//Завершено с ошибкой
    }

    /// <summary>
    /// Класс параметров события обновления Прогресса
    /// </summary>
    public class ProgressArgs : EventArgs
    {

        public ProgressArgs(decimal progress, int unloadID)
        {
            this.progress += progress;
            this.unloadID = unloadID;
            if (this.progress > 1) this.progress = 1;
        }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int unloadID { get; private set; }
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
        public StopArgs(int unloadID, bool is_alive)
        {
            this.unloadID = unloadID;
            this.is_alive = is_alive;
        }
        public bool is_alive { get; set; }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int unloadID { get; private set; }
    }
    /// <summary>
    /// Класс события отправки сообщения клиентской программе
    /// </summary>
    public class SendArgs : EventArgs
    {

        public SendArgs(string Message, Statuses result, int unloadID)
        {
            this.Message = Message;
            this.unloadID = unloadID;
            this.result = result;
        }
        public SendArgs(string Message, Statuses result, int unloadID, string link)
        {
            this.Message = Message;
            this.unloadID = unloadID;
            this.result = result;
            this.link = link;
        }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int unloadID { get; private set; }
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

    public class Point
    {
        public int nzp_wp { get; set; }
        public string point { get; set; }
        public string pref { get; set; }
    }

    public class Points : Point
    {
        public List<Point> pointList { get; set; }
    }

    public class Request
    {
        /// <summary>
        /// Номер элемента 
        /// </summary>
        public int Number { get; set; }
        public Points points { get; set; }
        public int unloadID { get; set; }
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
    }
}

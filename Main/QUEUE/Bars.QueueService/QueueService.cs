namespace Bars.QueueService
{
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.ServiceProcess;

    using Bars.QueueListener;

    using NLog;

    /// <summary>
    /// The queue service.
    /// </summary>
    public partial class QueueService : ServiceBase
    {
        private readonly string[] _queueList;
        private readonly string _rabbitMqHost;

        public QueueService()
        {
            InitializeComponent();

            var queueListStr = ConfigurationManager.AppSettings["queueList"];
            _rabbitMqHost = ConfigurationManager.AppSettings["rabbitMqHost"];
            
            _queueList = queueListStr == null ? new string[0] : queueListStr.Split(',').Select(x => x.Trim()).ToArray();

            var logger = LogManager.GetLogger("nlogger");
            logger.Info("Init service");
            logger.Info("rabbitMqHost: \"{0}\"; QueueList: \"{1}\";", _rabbitMqHost, queueListStr);
        }
        
        private Listener Listener { get; set; }

        protected override void OnStart(string[] args)
        {
            var logger = LogManager.GetLogger("nlogger");
            logger.Info("Start listeners");

            Listener = new Listener(_rabbitMqHost, _queueList);
            Listener.Listen();
        }

        protected override void OnStop()
        {
            Listener.Dispose();
        }
    }
}
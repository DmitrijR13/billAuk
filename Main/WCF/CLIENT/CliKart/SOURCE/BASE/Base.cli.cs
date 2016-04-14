using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System.Reflection;
using System.Threading;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Client
{
    public abstract class cli_Base  //реализация клиента 
    {
        int nzpServer;

        protected static TimeSpan warningTimeout = new TimeSpan(0, 0, 5);

        public cli_Base(int nzpServer)
        {
            this.nzpServer = nzpServer;
        }

        public cli_Base()
        {
            this.nzpServer = nzpServer;
        }

        //DateTime createTime;

        //public cli_Base()
        //{
        //    createTime = DateTime.Now;
        //    int a, b;
        //    ThreadPool.GetAvailableThreads(out a, out b);
        //    MonitorLog.WriteLog("Поток " + System.Threading.Thread.CurrentThread.ManagedThreadId + ". На момент создания объекта " + this + " доступное число потоков - " + a + ", доступное число асинхронных потоков I/O - " + b, MonitorLog.typelog.Warn, true);
        //    MonitorLog.WriteLog("Поток " + System.Threading.Thread.CurrentThread.ManagedThreadId + ". Объект " + this + " создан " + createTime.TimeOfDay, MonitorLog.typelog.Warn, true);
        //}

        //~cli_Base()
        //{
        //    MonitorLog.WriteLog("Поток " + System.Threading.Thread.CurrentThread.ManagedThreadId + ". Объект " + this + " создан " + createTime.TimeOfDay + ", уничтожен " + DateTime.Now.TimeOfDay, MonitorLog.typelog.Warn, true);
        //}

        public static string GetConfPref()
        {
            return DataBaseHead.ConfPref;
        }

        private DateTime queryStartTime;
        private string operationContractName = "";

        protected void BeforeStartQuery(string operationContractName)
        {
            queryStartTime = DateTime.Now;
            this.operationContractName = operationContractName;
        }

        protected void AfterStopQuery(DateTime? serverBeginTime, DateTime? serverEndTime)
        {
            TimeSpan ts = DateTime.Now - queryStartTime;
            if (ts > new TimeSpan(0, 0, 5))
            {
                MonitorLog.WriteLog("Время выполнения функции " + operationContractName + " составило " + ts +
                    Environment.NewLine + "Начало вызова на клиенте: " + queryStartTime +
                    (serverBeginTime != null ? Environment.NewLine + "Начало обработки вызова сервером: " + serverBeginTime : "") +
                    (serverBeginTime != null ? Environment.NewLine + "Окончание обработки вызова сервером: " + serverEndTime : "") +
                    Environment.NewLine + "Окончание вызова на клиенте: " + DateTime.Now
                , MonitorLog.typelog.Warn, true);
            }
        }

        protected void AfterStopQuery()
        {
            AfterStopQuery(null, null);
        }

#if PG
#else
        public static void SetWaitingTimeout(int timeout)
        {
            DataBaseHead.WaitingTimeout = timeout;
        }
#endif

        protected I getRemoteObject<I>(string address)
            where I : class
        {
            _RServer zap = MultiHost.GetServer(nzpServer);
            string addrHost = "";

            if (Points.IsMultiHost && nzpServer > 0)
            {
                addrHost = zap.ip_adr + address;
                return HostChannel.CreateInstance<I>(zap.login, zap.pwd, addrHost, "");
                //return HostChannel.CreateChannel<I>(zap.login, zap.pwd, addrHost, "");
            }
            else
            {
                addrHost = WCFParams.AdresWcfWeb.Adres + address;
                zap.rcentr = "<локально>";
                //return HostChannel.CreateChannel<I>(addrHost);
                return HostChannel.CreateInstance<I>(addrHost);
            }
        }
    }

    //----------------------------------------------------------------------
    static public class HostChannel
    //----------------------------------------------------------------------
    {
        public struct HostRecord
        {
            public HostRecord(ServiceHost host, string adress)
            {
                Host = host;
                Adress = adress;
            }
            public readonly ServiceHost Host;
            public readonly string Adress;
        }
        static readonly NetTcpBinding Binding;
        static Dictionary<Type, HostRecord> m_Hosts = new Dictionary<Type, HostRecord>();
        static public string timespan;

        static HostChannel()
        {
            Binding = new NetTcpBinding();
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                foreach (KeyValuePair<Type, HostRecord> pair in m_Hosts)
                {
                    pair.Value.Host.Close();
                }
            };
        }

        public static I CreateInstance<I>(string hostAdress)
            where I : class
        {
            return CreateInstance<I>(Constants.Login, Constants.Password, hostAdress);
        }

        public static I CreateInstance<I>(string login, string pwd, string hostAdress)
            where I : class
        {
            return CreateInstance<I>(login, pwd, hostAdress, "");
        }

        public static I CreateInstance<I>(string login, string pwd, string hostAdress, string service)
            where I : class
        {
            //увеличим время выполнения (по-умолчанию 10 минут)
            //if (typeof(I) == typeof(STCLINE.KP50.Interfaces.I_Admin))
            //{
                //timespan = "00:25:00";
            //} 
            //else if (timespan == null || timespan == "" || TimeSpan.Parse(timespan) < TimeSpan.Parse("00:10:00"))
            //{
            //    timespan = "00:10:00";
            //}


            if (timespan == null || timespan == "" || TimeSpan.Parse(timespan) < TimeSpan.Parse("00:25:00"))
            {
                timespan = "00:25:00";
            }
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.MaxReceivedMessageSize = 1024 * 1024; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
            Binding.ReaderQuotas.MaxStringContentLength = 32768; // 2147483647; // 32768 * 2;

            switch (service)
            {
                case "Patch":
                    Binding.MaxReceivedMessageSize = 2147483647; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
                    Binding.ReaderQuotas.MaxStringContentLength = 2147483647; // 2147483647; // 32768 * 2;
                    Binding.ReaderQuotas.MaxArrayLength = 2147483647;
                    Binding.ReaderQuotas.MaxDepth = 32;
                    Binding.ReaderQuotas.MaxBytesPerRead = 4096;
                    Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
                    Binding.MaxBufferPoolSize = 2147483647;
                    Binding.MaxBufferSize = 2147483647;
                    Binding.TransferMode = TransferMode.Streamed;
                    break;
                default:
                    Binding.MaxReceivedMessageSize = 1024 * 1024; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
                    Binding.ReaderQuotas.MaxStringContentLength = 1024 * 1024; // 2147483647; // 32768 * 2;
                    Binding.ReaderQuotas.MaxArrayLength = 16384;
                    Binding.ReaderQuotas.MaxDepth = 32;
                    Binding.ReaderQuotas.MaxBytesPerRead = 4096;
                    Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
                    Binding.MaxBufferPoolSize = 524288;
                    Binding.MaxBufferSize = 1048576;
                    Binding.TransferMode = TransferMode.Buffered;
                    break;
            }

            ChannelFactory<I> factory = new ChannelFactory<I>(Binding);
            factory.Credentials.Windows.ClientCredential = new NetworkCredential(login, pwd);

            return factory.CreateChannel(new EndpointAddress(hostAdress));
        }

        public static void CloseProxy<I>(I instance) where I : class
        {
            ICommunicationObject proxy = instance as ICommunicationObject;
            proxy.Close();
            proxy = null;

            IDisposable d = instance as IDisposable;
            if (d != null) d.Dispose();
            d = null;
        }

        public static I CreateChannel<I>(string hostAdress)
            where I : class
        {
            return CreateChannel<I>(Constants.Login, Constants.Password, hostAdress, "");
        }

        public static I CreateChannel<I>(string login, string pwd, string hostAdress, string service)
            where I : class
        {
            //увеличим время выполнения (по-умолчанию 10 минут)
            //if (typeof(I) == typeof(STCLINE.KP50.Interfaces.I_Admin))
            //{
            //timespan = "00:25:00";
            //} 
            //else if (timespan == null || timespan == "" || TimeSpan.Parse(timespan) < TimeSpan.Parse("00:10:00"))
            //{
            //    timespan = "00:10:00";
            //}


            if (timespan == null || timespan == "" || TimeSpan.Parse(timespan) < TimeSpan.Parse("00:25:00"))
            {
                timespan = "00:25:00";
            }
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.MaxReceivedMessageSize = 1024 * 1024; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
            Binding.ReaderQuotas.MaxStringContentLength = 32768; // 2147483647; // 32768 * 2;

            switch (service)
            {
                case "Patch":
                    Binding.MaxReceivedMessageSize = 2147483647; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
                    Binding.ReaderQuotas.MaxStringContentLength = 2147483647; // 2147483647; // 32768 * 2;
                    Binding.ReaderQuotas.MaxArrayLength = 2147483647;
                    Binding.ReaderQuotas.MaxDepth = 32;
                    Binding.ReaderQuotas.MaxBytesPerRead = 4096;
                    Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
                    Binding.MaxBufferPoolSize = 2147483647;
                    Binding.MaxBufferSize = 2147483647;
                    Binding.TransferMode = TransferMode.Streamed;
                    break;
                default:
                    Binding.MaxReceivedMessageSize = 1024 * 1024; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
                    Binding.ReaderQuotas.MaxStringContentLength = 1024 * 1024; // 2147483647; // 32768 * 2;
                    Binding.ReaderQuotas.MaxArrayLength = 16384;
                    Binding.ReaderQuotas.MaxDepth = 32;
                    Binding.ReaderQuotas.MaxBytesPerRead = 4096;
                    Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
                    Binding.MaxBufferPoolSize = 524288;
                    Binding.MaxBufferSize = 1048576;
                    Binding.TransferMode = TransferMode.Buffered;
                    break;
            }

            ChannelFactory<I> factory = new ChannelFactory<I>(Binding);
            factory.Credentials.Windows.ClientCredential = new NetworkCredential(login, pwd);

            return factory.CreateChannel(new EndpointAddress(hostAdress));
        }
    }
}

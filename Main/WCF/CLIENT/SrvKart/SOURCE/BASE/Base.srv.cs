using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using Bars.KP50.DataImport.SOURCE.Srv;
using Bars.KP50.Gubkin.Srv;
using Globals.SOURCE.Container;
using Globals.SOURCE.INTF.Report;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.SrvKart.SOURCE.REPORT;
using STCLINE.KP50.Utility;
using Bars.KP50.Utils;

using Castle.MicroKernel.ComponentActivator;
using Castle.MicroKernel.Registration;
using Castle.Windsor;


namespace STCLINE.KP50.Server
{
    using Globals.SOURCE;
    using Globals.SOURCE.Config;

    //----------------------------------------------------------------------
    static public class AdresWCF_delete //параметры wcf по-умолчанию
    //----------------------------------------------------------------------
    {
        public enum bindWCF
        {
            Pipe,
            TCP
        }

        public static string Adres = "";
        public static string BrokerAdres = "";
        public static string HttpAdres = "";

        public static bool IsCredential = true;
        public static string Login = "";
        public static string Password = "";

        public static string srvAdres = "/adres"; //
        public static string srvSprav = "/sprav"; //
        public static string srvCounter = "/counter"; //
        public static string srvCharge = "/charge"; //
        public static string srvAdmin = "/admin"; //
        public static string srvPrm = "/prm"; //
        public static string srvGilec = "/gilec"; //
        public static string srvMoney = "/money"; //
        public static string srvOdn = "/odn"; //
        public static string srvAnaliz = "/analiz"; //
        public static string srvNedop = "/nedop";
        public static string srvFon = "/fon";
        public static string srvServ = "/serv";
        public static string srvPatch = "/patch";
        public static string srvTest = "/test";
        public static string srvEditInterData = "/editinterdata";
        public static string srvExcel = "/excel";
        public static string srvSimpleRep = "/simplerep";
        public static string srvCalcs = "/calcs";
        public static string srvSupg = "/supg";
        public static string srvPack = "/pack";
        public static string srvFnReval = "/fnreval";
        public static string srvFaktura = "/faktura";
        public static string srvMustCalc = "/mustcalc";
        public static string srvMulti = "/multi";
        public static string srvLicense = "/license";
        public static string srvBaseReport = "/baseReport";
    }


    //----------------------------------------------------------------------
    static public class HttpHostBase
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
        static readonly WebHttpBinding Binding;
        static Dictionary<Type, HostRecord> m_Hosts = new Dictionary<Type, HostRecord>();
        static public string timespan;

        static HttpHostBase()
        {
            Binding = new WebHttpBinding();
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                foreach (KeyValuePair<Type, HostRecord> pair in m_Hosts)
                {
                    pair.Value.Host.Close();
                }
            };
        }

        public static I CreateInstance<S, I>(string hostAdress)
            where I : class
            where S : I
        {
            //увеличим время выполнения (по-умолчанию 10 минут)
            if (timespan == null || timespan == "" || TimeSpan.Parse(timespan) < TimeSpan.Parse("00:10:00"))
            {
                timespan = "00:10:00";
            }
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.MaxReceivedMessageSize = 1024 * 1024; //тоже сделать через параметр
            Binding.ReaderQuotas.MaxStringContentLength = 32768;
            Binding.TransferMode = TransferMode.Buffered;
            Binding.MaxBufferSize = 1024 * 1024;
            Binding.ReaderQuotas.MaxArrayLength = 16384;
            Binding.ReaderQuotas.MaxDepth = 32;
            Binding.ReaderQuotas.MaxBytesPerRead = 4096;
            Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
            Binding.MaxBufferPoolSize = 524288;

            ChannelFactory<I> factory = new ChannelFactory<I>(Binding);
            factory.Credentials.Windows.ClientCredential = new NetworkCredential(Constants.Login, Constants.Password);

            EndpointAddress epa = new EndpointAddress(hostAdress);

            return factory.CreateChannel(epa);
        }


        public static HostRecord StartHost<S, I>(string hostAdress)
            where I : class
            where S : I
        {
            HostRecord hostRecord;
            if (m_Hosts.ContainsKey(typeof(S)))
            {
                hostRecord = m_Hosts[typeof(S)];
                return hostRecord;
            }

            ServiceHost host = new ServiceHost(typeof(S), new Uri[] { });
            //string adress = BaseAdress.ToString() + Guid.NewGuid().ToString();

            ServiceMetadataBehavior metadataBehavior;
            metadataBehavior = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (metadataBehavior == null)
            {
                metadataBehavior = new ServiceMetadataBehavior();
                metadataBehavior.HttpGetEnabled = true;

                metadataBehavior.HttpGetUrl = new Uri(WCFParams.AdresWcfHost.HttpAdres);
                host.Description.Behaviors.Add(metadataBehavior);
            }


            //увеличим время выполнения (по-умолчанию 10 минут)
            if (timespan == null || timespan == "") timespan = "00:10:00";
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.MaxReceivedMessageSize = 1024 * 1024; //тоже сделать через параметр
            Binding.ReaderQuotas.MaxStringContentLength = 32768;
            Binding.TransferMode = TransferMode.Buffered;
            Binding.MaxBufferSize = 1024 * 1024;
            Binding.ReaderQuotas.MaxArrayLength = 16384;
            Binding.ReaderQuotas.MaxDepth = 32;
            Binding.ReaderQuotas.MaxBytesPerRead = 4096;
            Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
            Binding.MaxBufferPoolSize = 524288;

            hostRecord = new HostRecord(host, hostAdress);
            m_Hosts.Add(typeof(S), hostRecord);

            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(I), Binding, hostAdress);
            ep.Behaviors.Add(new WebHttpBehavior());

            host.Open();

            return hostRecord;
        }

        public static void CloseProxy<I>(I instance) where I : class
        {
            ICommunicationObject proxy = instance as ICommunicationObject;
            proxy.Close();
        }
    }


    //----------------------------------------------------------------------
    static public class HostBase
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

        static HostBase()
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

        public static I CreateInstance<S, I>(string hostAdress)
            where I : class
            where S : I
        {
            return CreateInstance<S, I>(Constants.Login, Constants.Password, hostAdress);
        }

        public static I CreateInstance<S, I>(string login, string pwd, string hostAdress)
            where I : class
            where S : I
        {
            //увеличим время выполнения (по-умолчанию 10 минут)
            if (timespan == null || timespan == "" || TimeSpan.Parse(timespan) < TimeSpan.Parse("00:10:00"))
            {
                timespan = "00:10:00";
            }
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.MaxReceivedMessageSize = 1024 * 1024; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
            Binding.ReaderQuotas.MaxStringContentLength = 32768; // 2147483647; // 32768 * 2;
            Binding.TransferMode = TransferMode.Buffered;
            Binding.MaxBufferSize = 1024 * 1024;
            Binding.ReaderQuotas.MaxArrayLength = 16384;
            Binding.ReaderQuotas.MaxDepth = 32;
            Binding.ReaderQuotas.MaxBytesPerRead = 4096;
            Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
            Binding.MaxBufferPoolSize = 524288;

            ChannelFactory<I> factory = new ChannelFactory<I>(Binding);
            factory.Credentials.Windows.ClientCredential = new NetworkCredential(login, pwd);

            return factory.CreateChannel(new EndpointAddress(hostAdress));
        }

        // перегрузка для I_Patch
        public static I CreateInstance<S, I>(string login, string pwd, string hostAdress, string Service)
            where I : class
            where S : I
        {

            if (timespan == null || timespan == "" || TimeSpan.Parse(timespan) < TimeSpan.Parse("00:10:00"))
            {
                timespan = "00:10:00";
            }
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            switch (Service)
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


        public static HostRecord StartHost<S, I>(string hostAdress)
            where I : class
            where S : I
        {
            HostRecord hostRecord;
            if (m_Hosts.ContainsKey(typeof(S)))
            {
                hostRecord = m_Hosts[typeof(S)];
                return hostRecord;
            }

            ServiceHost host = new ServiceHost(typeof(S), new Uri[] { });
            //string adress = BaseAdress.ToString() + Guid.NewGuid().ToString();

            //увеличим время выполнения (по-умолчанию 10 минут)
            if (timespan == null || timespan == "") timespan = "00:10:00";
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.MaxReceivedMessageSize = 1024 * 1024; // 2147483647; // 1024 * 1024 * 1; //тоже сделать через параметр
            Binding.MaxBufferSize = 1024 * 1024;
            Binding.ReaderQuotas.MaxStringContentLength = 32768; // 2147483647; // 32768;
            Binding.TransferMode = TransferMode.Buffered;
            Binding.ReaderQuotas.MaxArrayLength = 16384;
            Binding.ReaderQuotas.MaxDepth = 32;
            Binding.ReaderQuotas.MaxBytesPerRead = 4096;
            Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
            Binding.MaxBufferPoolSize = 524288;

            hostRecord = new HostRecord(host, hostAdress);
            m_Hosts.Add(typeof(S), hostRecord);
            host.AddServiceEndpoint(typeof(I), Binding, hostAdress);

            host.Open();

            return hostRecord;
        }

        // перегрузка для I_Patch
        public static HostRecord StartHost<S, I>(string hostAdress, string Service)
            where I : class
            where S : I
        {
            HostRecord hostRecord;
            if (m_Hosts.ContainsKey(typeof(S)))
            {
                hostRecord = m_Hosts[typeof(S)];
                return hostRecord;
            }

            ServiceHost host = new ServiceHost(typeof(S), new Uri[] { });
            //string adress = BaseAdress.ToString() + Guid.NewGuid().ToString();

            //увеличим время выполнения (по-умолчанию 10 минут)
            if (timespan == null || timespan == "") timespan = "00:10:00";
            Binding.OpenTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.CloseTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.SendTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            Binding.ReceiveTimeout = TimeSpan.Parse(timespan);//("00:10:00");
            switch (Service)
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
                    Binding.TransferMode = TransferMode.Buffered;
                    Binding.MaxReceivedMessageSize = 1024 * 1024; // 2147483647; // 1024 * 1024 * 1024; //тоже сделать через параметр
                    Binding.ReaderQuotas.MaxStringContentLength = 1024 * 1024; // 2147483647; // 32768 * 2;
                    Binding.ReaderQuotas.MaxArrayLength = 16384;
                    Binding.ReaderQuotas.MaxDepth = 32;
                    Binding.ReaderQuotas.MaxBytesPerRead = 4096;
                    Binding.ReaderQuotas.MaxNameTableCharCount = 16384;
                    Binding.MaxBufferPoolSize = 524288;
                    Binding.MaxBufferSize = 1048576;
                    break;
            }

            hostRecord = new HostRecord(host, hostAdress);
            m_Hosts.Add(typeof(S), hostRecord);
            host.AddServiceEndpoint(typeof(I), Binding, hostAdress);

            host.Open();

            return hostRecord;
        }

        public static void CloseProxy<I>(I instance) where I : class
        {
            ICommunicationObject proxy = instance as ICommunicationObject;
            proxy.Close();
        }

        public static bool ContainsHost<I>(I host) where I : class
        {
            return m_Hosts.ContainsKey(typeof(I));
        }

        public static void CloseAllHost(bool withoutArchive)
        {
            // выбирает все оконечные точки или только без /archive
            Dictionary<Type, HostRecord> dictHosts = !withoutArchive ? m_Hosts : m_Hosts.Where(x => x.Key != typeof(srv_Archive)).ToDictionary(x => x.Key, x => x.Value);
            foreach (var oneHost in dictHosts)
            {
                oneHost.Value.Host.Close();
                m_Hosts.Remove(oneHost.Key);
            }
        }
    }



    static public class SrvRun
    {
        /// <summary>
        /// Способы вывода сособщений
        /// </summary>
        public enum MessageOutputModes //перенести в Globals
        {
            /// <summary>
            /// Вывод сообщений в консоль
            /// </summary>
            Console,

            /// <summary>
            /// Вывод сообщений в файл
            /// </summary>
            File,

            /// <summary>
            /// Вывод сообщений на win-форме
            /// </summary>
            WinForm
        }

        /// <summary>
        /// Способ вывода сообщений
        /// </summary>
        static public MessageOutputModes MessageOutputMode;

        static public TextBox tb_Message;

        static public void WriteMessage(string message)
        {
            switch (MessageOutputMode)
            {
                case MessageOutputModes.Console:
                    {
                        Console.WriteLine(message);
                        break;
                    }
                case MessageOutputModes.WinForm:
                    {
                        tb_Message.AppendText("\r\n" + message);
                        break;
                    }
                default:
                    {
                        Console.WriteLine(message);
                        break;
                    }
            }
        }

        public enum ProgramRoles
        {
            /// <summary>
            /// Хост-приложение
            /// </summary>
            Host,

            /// <summary>
            /// Приложение-посредник, перенаправляющий вызовы на реальное Хост-приложение
            /// </summary>
            Broker,

            /// <summary>
            /// Мультихостинг
            /// </summary>
            Multi
        }

        private static ProgramRoles _programRole = ProgramRoles.Host;
        public static ProgramRoles ProgramRole
        {
            get
            {
                return _programRole;
            }
            set
            {
                _programRole = value;
                SrvRunProgramRole.IsHost = isHost;
                SrvRunProgramRole.IsBroker = isBroker;
                SrvRunProgramRole.IsMulti = isMulti;
            }
        }

        public static bool isHost { get { return ProgramRole == ProgramRoles.Host; } }
        public static bool isBroker { get { return ProgramRole == ProgramRoles.Broker; } }
        public static bool isMulti { get { return ProgramRole == ProgramRoles.Multi; } }

        //public static bool isBroker = false; //хост-посредник
        //public static bool isBroker = true; //false; //хост-посредник

        //static bool httpTest = false;
        static bool onlyCalc = false;
        static string[] arrCalc = new string[4] { "0", "1", "2", "3" }; //= string.Split(new string[] { ";" }, StringSplitOptions.None);

        static Thread thread_saldo;
        static Thread thread_bill;
        static Thread[] thread_calcs = new Thread[CalcThreads.maxCalcThreads];

        //----------------------------------
        //static void Test()
        ////----------------------------------
        //{
        //    cli_Fon cli = new cli_Fon(WCFParams.AdresWcfHost.CurT_Server);

        //    FonTask fon = new FonTask();
        //    fon.cur_state = enFonState.none;
        //    fon.act_state = enFonState.act_start;

        //    Returns ret = Utils.InitReturns();
        //    cli.PutState(fon, out ret);
        //}

        //----------------------------------
        //static public void Tracer(string mes)
        ////----------------------------------
        //{
        //    switch (MessageOutputMode)
        //    {
        //        case MessageOutputModes.Console:
        //            {
        //                //Console.WriteLine(mes);
        //                break;
        //            }
        //        case MessageOutputModes.WinForm:
        //            {
        //                tb_Message.AppendText("\r\n " + mes);
        //                break;
        //            }
        //    }
        //}
        //----------------------------------
        public static void TcpHostingStart(string srvAdres)
        //----------------------------------
        {
            //если только расчет, то хостинг не вызывается
            if (onlyCalc) return;
            HostBase.timespan = "00:20:00"; //потом переделать!
            HostBase.StartHost<srv_Admin, I_Admin>(srvAdres + WCFParams.AdresWcfHost.srvAdmin);
            HostBase.StartHost<srv_AdminHard, I_AdminHard>(srvAdres + WCFParams.AdresWcfHost.srvAdminHard);
            HostBase.timespan = "";
            HostBase.StartHost<srv_Adres, I_Adres>(srvAdres + WCFParams.AdresWcfHost.srvAdres);

            HostBase.StartHost<srv_AdresHard, I_AdresHard>(srvAdres + WCFParams.AdresWcfHost.srvAdresHard);
            HostBase.StartHost<srv_DataImport, I_DataImport>(srvAdres + WCFParams.AdresWcfHost.srvDataImport);
            HostBase.StartHost<srv_OneTime, I_OneTimeLoad>(srvAdres + WCFParams.AdresWcfHost.srvOneTimeLoad);


            HostBase.StartHost<srv_Sprav, I_Sprav>(srvAdres + WCFParams.AdresWcfHost.srvSprav);
            HostBase.StartHost<srv_Counter, I_Counter>(srvAdres + WCFParams.AdresWcfHost.srvCounter);
            HostBase.StartHost<srv_Prm, I_Prm>(srvAdres + WCFParams.AdresWcfHost.srvPrm);
            HostBase.StartHost<srv_Nedop, I_Nedop>(srvAdres + WCFParams.AdresWcfHost.srvNedop);
            HostBase.StartHost<srv_Serv, I_Serv>(srvAdres + WCFParams.AdresWcfHost.srvServ);

            HostBase.timespan = "20:59:59"; //потом переделать!
            HostBase.StartHost<srv_Charge, I_Charge>(srvAdres + WCFParams.AdresWcfHost.srvCharge);
            HostBase.StartHost<srv_Distrib, I_Distrib>(srvAdres + WCFParams.AdresWcfHost.srvDistrib);
            HostBase.StartHost<srv_Analiz, I_Analiz>(srvAdres + WCFParams.AdresWcfHost.srvAnaliz);
            HostBase.StartHost<srv_SendedMoney, I_SendedMoney>(srvAdres + WCFParams.AdresWcfHost.srvSendedMoney);
            

            HostBase.timespan = "";
            HostBase.StartHost<srv_Gilec, I_Gilec>(srvAdres + WCFParams.AdresWcfHost.srvGilec);
            HostBase.StartHost<srv_Money, I_Money>(srvAdres + WCFParams.AdresWcfHost.srvMoney);
            HostBase.StartHost<srv_Odn, I_Odn>(srvAdres + WCFParams.AdresWcfHost.srvOdn);
            HostBase.StartHost<srv_EditInterData, I_EditInterData>(srvAdres + WCFParams.AdresWcfHost.srvEditInterData);
            HostBase.StartHost<srv_Fon, I_FonTask>(srvAdres + WCFParams.AdresWcfHost.srvFon);

            HostBase.timespan = "03:00:00"; // для обновлений
            HostBase.StartHost<srv_Patch, I_Patch>(srvAdres + WCFParams.AdresWcfHost.srvPatch, "Patch");

            HostBase.timespan = "";
            HostBase.StartHost<srv_Pack, I_Pack>(srvAdres + WCFParams.AdresWcfHost.srvPack);
            HostBase.StartHost<srv_FnReval, I_FnReval>(srvAdres + WCFParams.AdresWcfHost.srvFnReval);
            HostBase.StartHost<srv_ExcelRep, I_ExcelRep>(srvAdres + WCFParams.AdresWcfHost.srvExcel);
            HostBase.StartHost<srv_Calcs, I_Calcs>(srvAdres + WCFParams.AdresWcfHost.srvCalcs);
            HostBase.StartHost<srv_supg, I_Supg>(srvAdres + WCFParams.AdresWcfHost.srvSupg);
            HostBase.StartHost<srv_Faktura, I_Faktura>(srvAdres + WCFParams.AdresWcfHost.srvFaktura);
            HostBase.StartHost<srv_SimpleRep, I_SimpleRep>(srvAdres + WCFParams.AdresWcfHost.srvSimpleRep);
            HostBase.StartHost<srv_SmsMessage, I_SmsMessage>(srvAdres + WCFParams.AdresWcfHost.srvSmsMessage);
            HostBase.StartHost<srv_MustCalc, I_MustCalc>(srvAdres + WCFParams.AdresWcfHost.srvMustCalc);
            if (!HostBase.ContainsHost<srv_Archive>(new srv_Archive())) HostBase.StartHost<srv_Archive, I_Archive>(srvAdres + WCFParams.AdresWcfHost.srvArchive);
            HostBase.StartHost<srv_License, I_License>(srvAdres + WCFParams.AdresWcfHost.srvLicense);
            HostBase.StartHost<ReportService, IReportService>(srvAdres + WCFParams.AdresWcfHost.srvBaseReport);

            if (SrvRun.isMulti)
            {
                HostBase.StartHost<srv_Multi, I_Multi>(srvAdres + WCFParams.AdresWcfHost.srvMulti);
            }

            HostBase.StartHost<srv_EPasp, I_EPasp>(srvAdres + WCFParams.AdresWcfHost.srvEPasp);
            HostBase.StartHost<srv_Nebo, I_Nebo>(srvAdres + WCFParams.AdresWcfHost.srvNebo);
            HostBase.StartHost<srv_Debitor, I_Debitor>(srvAdres + WCFParams.AdresWcfHost.srvDebitor);
            HostBase.StartHost<srv_Exchange, I_Exchange>(srvAdres + WCFParams.AdresWcfHost.srvExchange);
            //Test();
        }

        //----------------------------------
        static public bool ConsolArgs(string[] args)
        //----------------------------------
        {
            Constants.Trace = false;

            if (args == null || args.Length < 1) return false;

            int i;
            string arg, nextArg;

            for (i = 0; i < args.Length; i++)
            {
                arg = args[i].Trim().ToLower();
                nextArg = i < args.Length - 1 ? args[i + 1].Trim().ToLower() : "";

                switch (arg)
                {
                    case "trace":
                        Constants.Trace = true;
                        break;

                    case "crc":
                        if (String.IsNullOrEmpty(nextArg))
                        {
                            Console.WriteLine("Не заданы параметры аргумента " + arg);
                            return true;
                        }
                        Console.WriteLine("In: " + nextArg);
                        nextArg = Encryptor.Encrypt(nextArg, null);
                        Console.WriteLine("Out: " + nextArg);
                        break;

                    case "crd":
                        if (String.IsNullOrEmpty(nextArg))
                        {
                            Console.WriteLine("Не заданы параметры аргумента " + arg);
                            return true;
                        }
                        Console.WriteLine("In: " + nextArg);
                        nextArg = Encryptor.Decrypt(nextArg, null);
                        Console.WriteLine("Out: " + nextArg);
                        break;

                    case "onlycalc":
                        if (String.IsNullOrEmpty(nextArg))
                        {
                            Console.WriteLine("Не заданы параметры аргумента " + arg);
                            return true;
                        }
                        onlyCalc = true;
                        arrCalc = nextArg.Split(new string[] { "," }, StringSplitOptions.None);
                        break;
                }
            }

            if (Constants.Trace) return false;

            return true;

            /*i = args.Length;
            if (i > 1)
            {
                //управляющие команды
                string val1 = args[0];
                string val2 = args[1];

                if (String.IsNullOrEmpty(val1))
                {
                    Console.WriteLine("Не указан 2-й аргумент");
                    return true;
                }

                switch (val1)
                {

                    case "crc":
                        {
                            Console.WriteLine("In: " + val2);
                            val2 = Encryptor.Encrypt(val2, null);
                            Console.WriteLine("Out: " + val2);
                            break;
                        }
                    case "crd":
                        {
                            Console.WriteLine("In: " + val2);
                            val2 = Encryptor.Decrypt(val2, null);
                            Console.WriteLine("Out: " + val2);
                            break;
                        }

                    case "onlycalc":
                        {
                            onlyCalc = true;
                            arrCalc = val2.Split(new string[] { "," }, StringSplitOptions.None);
                            //break;
                            return false;  //надо запустить программу
                        }
                }

                return true;
            }
            return false;*/
        }

        /// <summary>
        /// Считывание настроек из файла конфигурации
        /// </summary>
        /// <param name="ConfPref"></param>
        /// <returns></returns>
        static bool LoadDataConfig(string ConfPref)
        {
            Utility.ClassLog.InitializeLog(
                Environment.CurrentDirectory,
                System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".log", "") + ".log");

            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции LoadDataConfig");
            Constants.Debug = true;
            Constants.Viewerror = true;

            WriteMessage("Лог-файл: " + Utility.ClassLog.GetLogFileName());
            WriteMessage("Загрузка файла настроек..." + DateTime.Now.ToString());

            try
            {
                /*#if DEBUG
                    string applicationName =
                        Environment.GetCommandLineArgs()[0];
                #else
                    string applicationName =
                        Environment.GetCommandLineArgs()[0]+ ".exe";
                #endif

                    Console.WriteLine(Environment.CurrentDirectory);
                    Console.WriteLine(applicationName);

                    string exePath = System.IO.Path.Combine(Environment.CurrentDirectory, System.IO.Path.GetFileName(applicationName)) + ".config";
                Console.WriteLine(exePath);
                Configuration configuration = ConfigurationManager.OpenExeConfiguration(exePath);

                AppSettingsSection valueCollection = configuration.AppSettings;*/

                #region параметры подсоединения к базе данных web-сервера
                Constants.cons_Webdata = /*valueCollection.Settings[ConfPref + "1"].Value;*/ ConfigurationManager.AppSettings[ConfPref + "1"];
                Constants.cons_Webdata = Encryptor.Decrypt(Constants.cons_Webdata, null);
                #endregion

                #region данные учетной записи пользователя ОС
                Constants.cons_User = /*valueCollection.Settings[ConfPref + "2"].Value;*/ ConfigurationManager.AppSettings[ConfPref + "2"];
                Constants.cons_User = Encryptor.Decrypt(Constants.cons_User, null);
                Utils.UserLogin(Constants.cons_User, out Constants.Login, out Constants.Password);
                #endregion

                #region адрес хоста
                string sAdresWCF = /*valueCollection.Settings[ConfPref + "3"].Value;*/ ConfigurationManager.AppSettings[ConfPref + "3"];
                WCFParams.AdresWcfHost.Adres = Encryptor.Decrypt(sAdresWCF, null);
                #endregion

                #region параметры подключения к основной базе
                if (isHost)
                {
                    Constants.cons_Kernel = /*valueCollection.Settings[ConfPref + "4"].Value;*/ ConfigurationManager.AppSettings[ConfPref + "4"];
                    Constants.cons_Kernel = Encryptor.Decrypt(Constants.cons_Kernel, null);
                }
                else
                {
                    Constants.cons_Kernel = "---";
                }
                #endregion

                #region адрес брокера
                if (isBroker)
                {
                    //для брокера надо считать web-адрес BrokerAdres
                    string s = /*valueCollection.Settings[ConfPref + "5"].Value;*/ ConfigurationManager.AppSettings[ConfPref + "5"];
                    if (String.IsNullOrEmpty(s))
                    {
                        WCFParams.AdresWcfHost.Adres = "";
                    }
                    else
                    {
                        WCFParams.AdresWcfHost.BrokerAdres = Encryptor.Decrypt(s, null);
                    }
                }
                #endregion

                #region Настройки директорий
                Constants.Directories.FilesDir = /*valueCollection.Settings[ConfPref + "6"].Value;*/ ConfigurationManager.AppSettings[ConfPref + "6"];
                if (String.IsNullOrEmpty(Constants.Directories.FilesDir))
                    Constants.Directories.FilesDir = "";
                else
                {
                    string dir = Encryptor.Decrypt(Constants.Directories.FilesDir, null);
                    if (!dir.EndsWith("\\") && !dir.EndsWith("/"))
                        dir += '\\';
                    Constants.Directories.FilesDir = dir;
                    //Constants.Directories.FilesDir = Encryptor.Decrypt(Constants.Directories.FilesDir, null);

                }
                #endregion

                bool isParamDefined = false;

                #region Признак, стартовать фоновые потоки
                Points.StartBackgroundThreads = true;
                string prm = "yes";
                try
                {
                    prm = ConfigurationManager.AppSettings[ConfPref + "8"];
                    isParamDefined = !String.IsNullOrEmpty(prm);
                }
                catch { }

                if (isParamDefined)
                {
                    try
                    {
                        prm = Encryptor.Decrypt(prm, null);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при чтении параметра 8 файла конфигурации:\n" + ex.Message, MonitorLog.typelog.Error, true);
                        Constants.Login = "";
                        throw new Exception("Ошибка при чтении параметра 8 файла конфигурации:\n" + ex.Message);
                    }
                    Points.StartBackgroundThreads = (prm.Trim().ToLower() == "yes");
                }
                #endregion

#if PG
                // считывание префикса
                string Pref = /*valueCollection.Settings[ConfPref + "3"].Value;*/ ConfigurationManager.AppSettings[ConfPref + "10"];
                Points.Pref = Encryptor.Decrypt(Pref, null);
#else
                #region Время ожидания снятия блокировки с таблиц
                isParamDefined = false;
                try
                {
                    prm = ConfigurationManager.AppSettings["DbWaitingTimeout"];
                    isParamDefined = !String.IsNullOrEmpty(prm);
                }
                catch { }

                if (isParamDefined)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(prm))
                        {
                            prm = Encryptor.Decrypt(prm, null);
                            int timeout = Convert.ToInt32(prm);
                            if (timeout >= 0)
                                DataBaseHead.WaitingTimeout = timeout;
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при распознавании значения параметра DbWaitingTimeout файла конфигурации:\n" + ex.Message, MonitorLog.typelog.Error, true);
                        Constants.Login = "";
                        throw new Exception("Ошибка при распознавании значения параметра DbWaitingTimeout файла конфигурации:\n" + ex.Message, ex);
                    }
                }
                #endregion
#endif
                #region Настройки на ftp сервер
                InputOutput.ftpParams = new FtpParams();
                InputOutput.useFtp = false;
                isParamDefined = false;
                try
                {
                    prm = ConfigurationManager.AppSettings["FtpHostAddress"];
                    isParamDefined = !String.IsNullOrEmpty(prm);
                }
                catch { }

                if (isParamDefined)
                {
                    try
                    {
                        InputOutput.ftpParams.Address = Encryptor.Decrypt(prm, null);

                        string userName = ConfigurationManager.AppSettings["FtpUserName"];
                        string userPwd = ConfigurationManager.AppSettings["FtpUserPassword"];

                        InputOutput.ftpParams.Credentials = new NetworkCredential(Encryptor.Decrypt(userName, null), Encryptor.Decrypt(userPwd, null));

                        prm = ConfigurationManager.AppSettings["FtpUseProxy"];
                        InputOutput.ftpParams.UseProxy = Encryptor.Decrypt(prm, null).ToLower().Trim() == "yes";

                        InputOutput.useFtp = true;
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при распознавании параметров ftp:\n" + ex.Message, MonitorLog.typelog.Error, true);
                        Constants.Login = "";
                        throw new Exception("Ошибка при распознавании параметров ftp:\n" + ex.Message, ex);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                if (Constants.Trace) Utility.ClassLog.WriteLog("Функция LoadDataConfig: " + ex.Message);
                MonitorLog.WriteLog("Функция LoadDataConfig: " + ex.Message, MonitorLog.typelog.Error, true);
                WriteMessage("Ошибка чтения файла настроек " + ex.Message);

                return false;
            }

            if (Constants.cons_Kernel == null ||
                 Constants.cons_Webdata == null ||
                 Constants.Login == "" ||
                 Constants.Password == "" ||
                 WCFParams.AdresWcfHost.Adres == "")
            {
                if (Constants.Trace) Utility.ClassLog.WriteLog("Функция LoadDataConfig: Не определены параметры .config");
                WriteMessage("Не определены параметры .config");
                return false;
            }
            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции LoadDataConfig");
            return true;
        }

        public static void StartHostProgram(bool start_host)
        {
            Start2(start_host);
        }

        public static void StartHostProgram()
        {
            StartHostProgram(true);
        }

        static void Start2(bool start_host)
        {
            
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции Start2");
  
            IocContainer.InitContainer(true);
            IocContainer.Current.Register(Component.For<IConfigProvider>().UsingFactoryMethod(x => FileConfigProvider.Init()).LifestyleSingleton());
            IocContainer.Current.Resolve<IConfigProvider>().GetConfig();
            IocContainer.InitLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));
            IocContainer.LoadReports();
            IocContainer.LoadBills();

            WCFParams.AdresWcfHost = new WCFParamsType();

            if (!LoadDataConfig(DataBaseHead.ConfPref)) return;
            MonitorLog.StartLog("STCLINE.KP50.Host", "Старт хост-приложения");

            int a,b;
            ThreadPool.GetMinThreads(out a, out b);
            MonitorLog.WriteLog("Минимальное число потоков, которые пул потоков создает по запросу - " + a + ", минимальное число асинхронных потоков I/O, которые пул создает по запросу - " + b, MonitorLog.typelog.Warn, true);
            ThreadPool.GetMaxThreads(out a, out b);
            MonitorLog.WriteLog("Максимальное число потоков - " + a + ", максимальное число асинхронных потоков I/O - " + b, MonitorLog.typelog.Warn, true);

            try
            {
                #region Проверка миграции пользователей
                var dbAdmin = new DbAdmin();
                Returns ret;

                ret = dbAdmin.CheckUserMigrated();
                if (!ret.result && ret.tag != -999999999)
                {
                    Console.WriteLine(ret.text);
                    return;
                }
                else if (!ret.result && ret.tag == -999999999)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Не выполнен скрипт миграции пользователей");
                    return;
                }
                #endregion

                #region Инициализация объекта для работы с входящими/исходящими файлами

                InputOutput.InitializeFileManager(InputOutput.useFtp
                    ? FileManager.GetFtpInstance(Constants.Directories.FilesDir)
                    : FileManager.GetFolderInstance(Constants.Directories.FilesDir));

                #endregion

                #region Удаление файлов

                try
                {
                    if (InputOutput.useFtp)
                    {
                        if (Directory.Exists(Constants.Directories.FilesDir))
                        {
                            var directoryInfo = new DirectoryInfo(Constants.Directories.FilesDir);
                            foreach (var file in directoryInfo.GetFiles())
                            {
                                file.Delete();
                            }

                            foreach (var dir in directoryInfo.GetDirectories())
                            {
                                dir.Delete(true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка удаления файлов из кэша: " + ex, MonitorLog.typelog.Error, true);
                }

                #endregion

                #region Вывод сообщений с параметрами конфигурации

                var msg2 = "\r\nВерсия сборки: " + Constants.VersionSrv +
                           "\r\n\r\nДиректория с файлами: " + Constants.Directories.FilesDir +
                           "\r\n" +
                           (InputOutput.useFtp
                               ? "Параметры ftp: " + InputOutput.ftpParams.Address + " (" +
                                 InputOutput.ftpParams.Credentials.UserName + ")"
                               : "") +
                           "\r\n\r\nКэш БД: " + Utils.IfmxDatabase(Constants.cons_Webdata);
                WriteMessage(msg2);
                var msg = msg2;

                if (isHost)
                {
                    msg2 = "Основная БД: " + Utils.IfmxDatabase(Constants.cons_Kernel);
                    WriteMessage(msg2);
                    msg += msg2;
                }

                #endregion

                #region Признак, что работать надо только с центральным банком данных и не трогать локальные

                GlobalSettings.WorkOnlyWithCentralBank = false;
                GlobalSettings.NewGeneratePkodMode = false;

                try
                {
                    #region зашифрование паролей

#if PG
                //dbAdmin.UpdatePwds();
#endif

                    #endregion

                    var finder = new Setup {nzp_param = WebSetups.WorkOnlyWithCentralBank};
                    var setup = dbAdmin.GetSetup(finder, out ret);

                    if (ret.result) // параметр задан
                    {
                        if (setup.value == "1")
                        {
                            GlobalSettings.WorkOnlyWithCentralBank = true;
                            msg = "Установлен режим работы с центральным банком";
                            MonitorLog.WriteLog(msg, MonitorLog.typelog.Info, true);
                            if (isHost)
                            {
                                WriteMessage(msg);
                            }
                        }
                    }
                    else // параметр не задан, не продолжаем работу
                    {
                        if (Constants.Trace)
                            Utility.ClassLog.WriteLog(
                                "Функция Start2: Не считан параметр режима работы только с центральным банком");
                        MonitorLog.WriteLog("Признак работы только с центральным банком: " + ret.text,
                            MonitorLog.typelog.Error, true);
                        WriteMessage("Ошибка при считывании параметра работы только с центральным банком данных");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Start2\n" + ex.Message, MonitorLog.typelog.Error, true);
                    WriteMessage("Ошибка при считывании параметра работы только с центральным банком данных\n" +
                                 ex.Message);
                    return;
                }
                finally
                {
                    dbAdmin.Close();
                }

                #endregion
               // GlobalSettings.NewGeneratePkodMode = true;
                //проверка наличия обновления БД
                if (isHost && start_host)
                {
                    // Новый механизм обновления
                    int intUpdatesAvalible;
#if !PG
Points.Pref = Utils.IfmxGetPref(Constants.cons_Kernel);
#endif
                    ProcessStartInfo pi = new ProcessStartInfo();
                    pi.CreateNoWindow = false;
                    pi.UseShellExecute = false;
                    pi.WindowStyle = ProcessWindowStyle.Hidden;
                    pi.FileName = "KP50.DataBase.Update.exe";

                    try
                    {

                        pi.Arguments =
                            string.Format(
                                "{0} --connection-string \"{1}\" --version -01 --prefix {2} --assembly \"{3}\" --no-help",
                                DBManager.tableDelimiter == ":" ? "--informix" : "--postgresql",
                                Constants.cons_Kernel.Replace("UID", "User ID").Replace("Pwd", "Password"),
                                Points.Pref, "KP50.DataBase.UpdateAssembly.dll");

                        if (Constants.cons_Kernel != Constants.cons_Webdata &&
                            !string.IsNullOrEmpty(Constants.cons_Webdata.Trim()))
                            pi.Arguments += string.Format(" --web-connection \"{0}\"",
                                Constants.cons_Webdata.Replace("UID", "User ID").Replace("Pwd", "Password"));
                        using (Process execProc = Process.Start(pi))
                        {
                            execProc.WaitForExit();
                            if (execProc.ExitCode < 0)
                            {
                                Console.WriteLine("Обновление прошло с ошибками. Обратитесь к разработчику.");
                                return;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка обновления базы данных: " + ex.Message, MonitorLog.typelog.Error,
                            true);
                        return;
                    }

                    /*
                    //todo: При работе только с центральным банком проверять версию БД только центрального банка, не поднимать версию лок банков
                    var patcher = new Patcher(RunFrom.Host);
                    if (!patcher.CheckPatches(RunFrom.Host))
                    {
                        if (Constants.Trace) Utility.ClassLog.WriteLog("Функция Start2:Патчер вернул false");
                        return;
                    }
                    */
                }

                WCFParams.AdresWcfWeb = new WCFParamsType {Adres = WCFParams.AdresWcfHost.Adres}; // для брокера

                Utils.setCulture();

                if (isMulti)
                {
                    //загрузка списка доступных хостов
                    //DbStartMulti(out ret);
                    var db = new DbMultiHostClient();
                    ret = db.LoadMultiHost();
                    db.Close();

                    MultiHost.IsMultiHost = true;
                    Points.isInitSuccessfull = true;
                }
                if (isHost)
                {
#if !PG
                    Points.Pref = Utils.IfmxGetPref(Constants.cons_Kernel);
#endif

                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //прогнать исправления!
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    if (!onlyCalc)
                    {
                        var dbch = new DbChanger();
                        try
                        {
                            //dbch.SetUpdates(out ret);
                            //dbch.SetUpdates2(out ret);
                        }
                        catch (Exception ex)
                        {
                            WriteMessage("Ошибка при выполнении обновлений БД\n" + ex.Message);
                            return;
                        }
                        finally
                        {
                            dbch.Close();
                        }
                    }

                    var db = new DbSprav();
                    try
                    {
                        
                        Points.SetGetDateOperDelegate(DbFinUtils.GetOperDay);

                        List<string> warnPoint;
                        if (!db.PointLoad(GlobalSettings.WorkOnlyWithCentralBank, out ret, out warnPoint) ||
                            string.IsNullOrEmpty(Points.Pref))
                        {
                            if (Constants.Trace) Utility.ClassLog.WriteLog("Функция Start2: PointLoad вернул false");

                            if (string.IsNullOrEmpty(Points.Pref))
                                ret.text += "; pref пустой";

                            MonitorLog.WriteLog("Ошибка доступа (s_point) " + ret.text, MonitorLog.typelog.Error, false);
                            WriteMessage("Ошибка доступа (s_point) " + ret.text);

                            db.Close();

                            return;
                        }
                        else if (ret.result)
                        {
                            if (ret.tag < 0)
                            {
                                WriteMessage(ret.text);
                            }
                            else if (warnPoint != null && warnPoint.Count > 0)
                            {
                                foreach (var warn in warnPoint)
                                    WriteMessage(warn +"\n");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteMessage("Ошибка при выполнении обновлений БД\n" + ex.Message);
                    }
                    finally
                    {
                        db.Close();
                    }

                    if (!onlyCalc)
                    {
                        var dbch = new DbChanger();
                        try
                        {
                            dbch.SetAfterPointLoadUpdates(out ret);
                        }
                        catch (Exception ex)
                        {
                            WriteMessage("Ошибка при выполнении обновлений БД\n" + ex.Message);
                            return;
                        }
                        finally
                        {
                            dbch.Close();
                        }
                    }
                }

                try
                {
                    FileUtility.CreateDirectory(Constants.Directories.FilesDir);
                    FileUtility.CreateDirectory(Constants.Directories.ReportDir);
                    FileUtility.CreateDirectory(Constants.Directories.BillDir);
                    FileUtility.CreateDirectory(Constants.Directories.BillWebDir);
                    /*if (Points.IsCalcSubsidy)
                {
                    FileUtility.CreateDirectory(Constants.FilesDir + Constants.SubsidyDir);
                    FileUtility.CreateDirectory(Constants.FilesDir + Constants.SubsidyDir + Constants.ActsOfSupplyDir);
                    FileUtility.CreateDirectory(Constants.FilesDir + Constants.SubsidyDir + Constants.THGFDir);
                }*/
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при создании директорий:\n" + ex.Message, MonitorLog.typelog.Error, true);
                }

                if (!isBroker)
                {
                    //Заполнение ExcelIsInstalled
                    var excDb = new ExcelRep();
                    Constants.ExcelIsInstalled = excDb.GetFlagExcelIsInstalled(ref ret);
                    excDb.Close();
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка получения флага ExcelIsInstalled - по умолчанию установлено false",
                            MonitorLog.typelog.Error, true);
                    }
                }

                msg2 = "\r\nРежимы работы:";
                WriteMessage(msg2);
                msg += msg2;

                if (start_host)
                {
                    if (!onlyCalc)
                    {
                        WriteMessage("- Сервисы");

                        if (isBroker || isHost)
                        {
                            if (isBroker)
                            {
                                TcpHostingStart(WCFParams.AdresWcfHost.BrokerAdres);
                                msg2 = "  Брокер-адрес: " + WCFParams.AdresWcfHost.BrokerAdres;
                                WriteMessage(msg2);
                                msg += msg2;
                            }
                            else if (isHost)
                            {
                                TcpHostingStart(WCFParams.AdresWcfHost.Adres);
                            }

                            msg2 = "  Хост-адрес: " + WCFParams.AdresWcfHost.Adres +
                                   "\r\n  Логин: " + Constants.Login;
                            WriteMessage(msg2);
                            msg += msg2;
                        }
                        else if (isMulti)
                        {
                            msg2 = "  Mультихостинг";
                            WriteMessage(msg2);
                            msg += msg2;
                        }
                    }

                    if (Points.StartBackgroundThreads)
                   // if (false)
                    {
                        WriteMessage("\r\n- Обработка фоновых задач (Очереди задач: " + string.Join(",", arrCalc) + ")");
                        TaskStarting();
                    }

                    //MonitorLog.StartLog("STCLINE.KP50.Host", conn);
                    MonitorLog.WriteLog(msg, MonitorLog.typelog.Info, false);

                    switch (MessageOutputMode)
                    {
                        case MessageOutputModes.Console:
                        {
                            WriteMessage(" ");
                            WriteMessage("Для ВЫХОДА нажмите клавишу Q");

                            //флаг включения таймера(вручную)
                            bool flagOnOffTimer = false;
                            if (flagOnOffTimer)
                            {
                                //Старт таймера апдейтера
                                TimerUpdater.CreateTimer();
                            }

                            ConsoleKeyInfo key;
                            do
                                key = Console.ReadKey(true); while (key.Key != ConsoleKey.Q);

                            if (flagOnOffTimer)
                            {
                                //-----остановка таймера------------
                                TimerUpdater.timer.Enabled = true;
                                TimerUpdater.timer.Close();
                                //----------------------------------
                            }

                            WriteMessage("");
                            WriteMessage("Выполняется завершение работы...");

                            TaskStop();
                            break;
                        }
                    }
                }
                else
                {
                    msg2 = "- Отсутствуют";
                    WriteMessage(msg2);
                    msg += msg2;

                    MonitorLog.WriteLog(msg, MonitorLog.typelog.Info, false);
                }
            }
            catch (Exception ex)
            {

                MonitorLog.WriteLog("Не обработанное исключение " + ex.Message, MonitorLog.typelog.Error, true);
            }

   

        }

        /// <summary>
        /// Запускает обработчик фоновых заданий
        /// </summary>
        public static void TaskStarting()
        {
            if (!Points.StartBackgroundThreads || !isHost) return;

            if (!Points.isFinances)
            {
                #region Запуск обработчика очереди расчета сальдо УК
                TaskQueues.fon_saldo = new TaskQueue(FonProcessorCommands.Start);

                //расчет сальдо УК
                SaldoQueueProcessor fons = new SaldoQueueProcessor(TaskQueues.fon_saldo);
                thread_saldo = new Thread(fons.ProcessQueue);
                thread_saldo.IsBackground = true;
                thread_saldo.Start();
                #endregion
            }

            #region Запуск обработчиков очередей расчета
            for (int i = 0; i < CalcThreads.maxCalcThreads; i++)
            {
                //запуск только конкретных calc_fon
                if (!arrCalc.Contains(i.ToString()))
                {
                    TaskQueues.fon_calc[i] = new CalcQueue(FonProcessorCommands.Stop, i);
                    continue;
                }

                TaskQueues.fon_calc[i] = new CalcQueue(FonProcessorCommands.Start, i);

                CalcQueueProcessor fon = new CalcQueueProcessor(TaskQueues.fon_calc[i]);
                thread_calcs[i] = new Thread(new ThreadStart(fon.ProcessQueue));
                thread_calcs[i].IsBackground = true;
                thread_calcs[i].Start();
            }
            #endregion

            #region Запуск обработчика очереди формирования платежных документов
            TaskQueues.fon_bill = new TaskQueue(FonProcessorCommands.Start);

            BillQueueProcessor billFon = new BillQueueProcessor(TaskQueues.fon_bill);
            thread_bill = new Thread(billFon.ProcessQueue);
            thread_bill.IsBackground = true;
            thread_bill.Start();
            #endregion
        }

        /// <summary>
        /// Останавливает обработку фоновых заданий
        /// </summary>
        public static void TaskStop()
        {
            if (!isHost || !Points.StartBackgroundThreads) return;

            //передача обработчикам задач команды на остановку работы
            if (TaskQueues.fon_saldo != null) TaskQueues.fon_saldo.act_state = FonProcessorCommands.Stop;
            if (TaskQueues.fon_bill != null) TaskQueues.fon_bill.act_state = FonProcessorCommands.Stop;
            foreach (CalcQueue queue in TaskQueues.fon_calc)
                if (queue != null) queue.act_state = FonProcessorCommands.Stop;

            //дожидаемся остановки обработки очередей задач
            if (TaskQueues.fon_saldo != null)
            {
                while (TaskQueues.fon_saldo.cur_state != FonProcessorStates.Stopped)
                {
                    Thread.Sleep(3000);
                }
                if (thread_saldo != null) thread_saldo.Abort(); //убиваем поток
            }

            while (TaskQueues.fon_bill.cur_state != FonProcessorStates.Stopped)
            {
                Thread.Sleep(3000);
            }
            if (thread_bill != null) thread_bill.Abort(); //убиваем поток

            //даем 3 секунды, чтобы процесс расчета начислений остановился
            for (int i = 0; i < CalcThreads.maxCalcThreads; i++)
            {
                while (TaskQueues.fon_calc[i].cur_state != FonProcessorStates.Stopped)
                {
                    Thread.Sleep(3000);
                }
                if (thread_calcs[i] != null) thread_calcs[i].Abort(); //убиваем поток
            }
        }

        //----------------------------------
        private static void PortalHostingStart(STCLine.Portal.Data.SrvRun.EnumDbType dbType)
        //----------------------------------
        {
            STCLine.Portal.Data.SrvRun.Configure
            (
                WCFParams.AdresWcfHost.Adres, dbType, Constants.cons_Kernel,
                delegate(string msg, System.Diagnostics.EventLogEntryType type)
                {
                    MonitorLog.WriteLog(msg, (MonitorLog.typelog)(type), 99, 99, true);
                }
            );
            STCLine.Portal.Data.SrvRun.Start(null);
        }

    }


    //класс для проверки версии и выполнения обновления

    public enum RunFrom
    {
        Host = 0,
        HostMan = 1
    }


    public class Patcher
    {
        readonly List<PatchInfo> _pointsInfo;

        public Patcher(RunFrom runFromApplication)
        {
            var db = new DbPatch();
            _pointsInfo = db.CreateTableForPatcher(runFromApplication == RunFrom.HostMan);
        }

        public bool CheckPatches(RunFrom runFromApplication)
        {
            if (_pointsInfo == null)
            {
                Console.WriteLine("Ошибка подготовки обновления. Обратитесь к разработчику.");
                return false;
            }
            var needPatch = new List<PatchInfo>();
            foreach (var pi in _pointsInfo)
            {
                //if (pi.version < 0)
                //{
                //    Console.WriteLine("Ошибка предыдущего обновления. Обратитесь к разработчику.");
                //    return false;
                //}

                if (pi.locked > 0)
                    if (Utils.CreateMD5StringHash(Environment.UserName + Environment.MachineName) != pi.locked_by)
                    {
                        Console.WriteLine("Обновление заблокировано другим пользователем. Обратитесь к разработчику.");
                        return false;
                    }
                    else
                    {
                        needPatch.Add(pi);
                        continue;
                    }

                if (pi.version == Constants.VersionDB)
                {
                    continue;
                }

                if (pi.version < Constants.VersionDB)
                {
                    switch (runFromApplication)
                    {
                        case RunFrom.Host:
                        case RunFrom.HostMan:
                            needPatch.Add(pi);
                            break;
                        default:
                            Console.WriteLine("Неизвестный параметр запуска.");
                            return false;
                    }
                }
                else
                {
                    Console.WriteLine("Старое приложение. Обратитесь к разработчику.");
                    return false;
                }
            }

            if (needPatch.Count > 0 && runFromApplication == RunFrom.Host)
            {
                Console.WriteLine("\nНеобходимо обновление БД. Выполнить его сейчас? Y - Да, N - Нет");

                ConsoleKeyInfo key;
                do
                    key = Console.ReadKey(true);
                while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N);

                if (key.Key != ConsoleKey.Y)
                {
                    Console.WriteLine(
                        "\nОбновление можно выполнить при следующем запуске приложения или запустив HostMan.exe через пункт меню \"2. Хостинг - Обновить\"");
                    return false;
                }
            }

            foreach (var pi in needPatch)
            {
                var db = new DbPatch();
                pi.locked = 1;
                pi.locked_by = Utils.CreateMD5StringHash(Environment.UserName + Environment.MachineName);
                db.SetUpdateStatus(pi, pi.version);
                if (!Patch(pi))
                {
                    Console.WriteLine("Ошибка обновления. Обратитесь к разработчику.");
                    MonitorLog.WriteLog("Ошибка обновления. Префикс БД: " + pi.pref + ", версия обновления: " + pi.version, MonitorLog.typelog.Error, true);
                    db = new DbPatch();
                    db.SetUpdateStatus(pi, pi.version - 1);
                    return false;
                }
                pi.locked = 0;
                pi.locked_by = "";
                db = new DbPatch();
                db.SetUpdateStatus(pi, pi.version);
            }

            return true;
        }

        static bool Patch(PatchInfo pi)
        {
            MonitorLog.WriteLog("Обновление БД с версии " + pi.version + " до версии " + Constants.VersionDB + ".", MonitorLog.typelog.Info, true);
            pi.version++;
            #region Проверка существования обновлений
            string PatchDirectory = Path.Combine(Directory.GetCurrentDirectory(), "patches");
            if (!Directory.Exists(PatchDirectory))
            {
                MonitorLog.WriteLog("Не найдена папка \"patches\" с обновлениями!", MonitorLog.typelog.Error, true);
                return false;
            }

            int StartUpdate = pi.version;
            int EndUpdate = Constants.VersionDB;
            for (int i = StartUpdate; i <= EndUpdate; i++)
            {
                string name = string.Format("{0:0000}", i);
                if (!Directory.Exists(Path.Combine(PatchDirectory, name)))
                {
                    MonitorLog.WriteLog("Не найдена папка \"" + name + "\" с обновлениями!", MonitorLog.typelog.Error, true);
                    return false;
                }
                if (!File.Exists(Path.Combine(Path.Combine(PatchDirectory, name), "patch.inic")))
                {
                    MonitorLog.WriteLog("Не найден файл \"patch.inic\" в папке \"" + name + "\"", MonitorLog.typelog.Error, true);
                    return false;
                }
            }
            #endregion

            #region Выполнение обновления
            for (int i = StartUpdate; i <= EndUpdate; i++)
            {
                string name = string.Format("{0:0000}", i);
                List<FileInfo> files = new List<FileInfo>();
                string path = Path.Combine(PatchDirectory, name);
                StreamReader sr = new StreamReader(Path.Combine(path, "patch.inic"));
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    str = StringCrypter.Decrypt(str, StringCrypter.pass);
                    if (str.Trim() == string.Empty)
                        continue;

                    string[] masstr = Regex.Replace(str.Trim(), "\\s+", " ").Split(new char[1] { ' ' });
                    if ((masstr[1] == "all") || ((masstr[1] == "local") && pi.wp != 1) || ((masstr[1] == "central") && pi.wp == 1))
                    {
                        if (masstr.Length == 2)
                        {
                            // если нет ограничений
                            files.Add(new FileInfo(Path.Combine(path, masstr[0])));
                        }
                        else
                        {
                            // если есть ограничения и известен номер банка
                            string bankNumbers = "," + Regex.Replace(string.Join("", masstr.Skip(2).ToArray()).Trim(), "\\s", ""); // получаем ограничения вида ",123123,1231,231"
                            if (bankNumbers.Contains("-") && !string.IsNullOrEmpty(pi.bank_number))
                            {
                                if (!bankNumbers.Contains(",-" + pi.bank_number))
                                {
                                    // если не входит в ограничение "кроме"
                                    files.Add(new FileInfo(Path.Combine(path, masstr[0])));
                                }
                            }
                            else
                            {
                                if (bankNumbers.Contains("," + pi.bank_number))
                                {
                                    // если входит в разрешенные
                                    files.Add(new FileInfo(Path.Combine(path, masstr[0])));
                                }
                            }
                        }
                    }
                }

                foreach (var file in files)
                {
                    if (!File.Exists(file.FullName))
                    {
                        MonitorLog.WriteLog("Не найден файл \"" + file + "\" в папке \"" + name + "\"", MonitorLog.typelog.Error, true);
                        return false;
                    }
                }

                if (files.Count > 0)
                {
                    PatchExecuter pe = new PatchExecuter(false) { MainDir = Path.Combine(PatchDirectory, name), Pref = pi.pref };
                    if (!pe.isNewPatches(files.ToArray())) return false;
                }
                pi.version = i;
            }
            #endregion
            return true;
        }
    }
}

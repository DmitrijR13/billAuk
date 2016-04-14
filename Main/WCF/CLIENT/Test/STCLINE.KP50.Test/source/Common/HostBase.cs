using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Net;

namespace STCLINE.KP50.Test
{
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

        public static I CreateInstance<I>(string hostAdress, string Login, string Password)
            where I : class
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

            ChannelFactory<I> factory = new ChannelFactory<I>(Binding);
            if (Login != "")
            {
                factory.Credentials.Windows.ClientCredential = new NetworkCredential(Login, Password);
            }

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
            Binding.MaxReceivedMessageSize = 1024 * 1024; //тоже сделать через параметр
            Binding.ReaderQuotas.MaxStringContentLength = 32768;

            hostRecord = new HostRecord(host, hostAdress);
            m_Hosts.Add(typeof(S), hostRecord);
            host.AddServiceEndpoint(typeof(I), Binding, hostAdress);

            host.Open();

            return hostRecord;
        }

        public static void OpenProxy<I>(I instance) where I : class
        {
            ICommunicationObject proxy = instance as ICommunicationObject;
            proxy.Open();
        }
        public static void CloseProxy<I>(I instance) where I : class
        {
            ICommunicationObject proxy = instance as ICommunicationObject;
            proxy.Close();
        }

    }
}

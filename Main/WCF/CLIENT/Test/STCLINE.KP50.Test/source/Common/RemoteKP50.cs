using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Test
{
    public delegate T RemoteDataDelegateA1<I, T>(I remotePersonalAccount)
        where T : class
        where I : class;

    internal static class RemoteKP50
    {
        public static string kp50_Address = "net.tcp://localhost:8008/srv";
        public static string kp50_Login = "";
        public static string kp50_Password = "";

        private static readonly Dictionary<Type, string> srvRemote = new Dictionary<Type, string>()
        {
            {typeof(I_Charge), "/charge"},
            {typeof(I_EPasp), "/epasp"},
            {typeof(I_Nebo), "/nebo"}
        };

        private static T RemoteCallA1<I, T>(RemoteDataDelegateA1<I, T> remoteCallAction)
            where T : class
            where I : class
        {
            I remoteProxy = HostBase.CreateInstance<I>(
                RemoteKP50.kp50_Address + srvRemote[typeof(I)],
                RemoteKP50.kp50_Login,
                RemoteKP50.kp50_Password);
            HostBase.OpenProxy(remoteProxy);
            try
            {
                return remoteCallAction(remoteProxy);
            }
            finally
            {
                HostBase.CloseProxy(remoteProxy);
            }
        }
        public static T GetData<T>(RemoteDataDelegateA1<I_Charge, T> remoteCallAction) where T : class
        {
            return RemoteCallA1(remoteCallAction);
        }
        public static T GetData<T>(RemoteDataDelegateA1<I_EPasp, T> remoteCallAction) where T : class
        {
            return RemoteCallA1(remoteCallAction);
        }
        public static T GetData<T>(RemoteDataDelegateA1<I_Nebo, T> remoteCallAction) where T : class
        {
            return RemoteCallA1(remoteCallAction);
        }
    }
}

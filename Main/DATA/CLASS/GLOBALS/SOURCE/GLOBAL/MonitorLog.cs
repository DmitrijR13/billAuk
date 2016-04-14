using System;
using System.Diagnostics;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.Global
{
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Threading;

    using Globals.SOURCE;
    using Globals.SOURCE.Container;
    
    //----------------------------------------------------------------------
    static public class MonitorLog
    //----------------------------------------------------------------------
    {
        static MonitorLog()
        {
            Archive.onInstanceCreated += new EventHandler<EventArgs>((sender, args) =>
                (sender as IArchive).onExceptionThrowed += new EventHandler<ArchiveExceptionEventArgs>(
                    (o, eventArgs) => MonitorLog.WriteLog(string.Format("Ошибка архивации файла: {0}", eventArgs.InheritException.Message), typelog.Error, true)));
        }

        private static ILog _logger;

        private static ILog Logger
        {
            get { return _logger ?? (_logger = IocContainer.Current.Resolve<ILog>()); }
        }

        public enum typelog : int
        {
            Error = EventLogEntryType.Error,
            Info = EventLogEntryType.Information,
            Warn = EventLogEntryType.Warning
        }

        public static void SetLogger(ILog logger)
        {
            _logger = logger;
        }

        static public void WriteLog(string mes, typelog t, bool to_imp)
        {
            switch (t)
            {
                case typelog.Error:
                    Logger.Error("Поток " + Thread.CurrentThread.ManagedThreadId + " " + mes + Environment.NewLine);
                    break;
                case typelog.Warn:
                    Logger.Warning("Поток " + Thread.CurrentThread.ManagedThreadId + " " + mes + Environment.NewLine);
                    break;
                case typelog.Info:
                    Logger.Info("Поток " + Thread.CurrentThread.ManagedThreadId + " " + mes);
                    break;
            }
        }

        static public void WriteLog(string mes, typelog t, int eventID, short category, bool to_imp)
        {
            WriteLog(mes, t, to_imp);
        }

        static public void StartLog(string source_name, string mes)
        {
            WriteLog(mes, typelog.Info, true);
        }

        static public void Close(string mes)
        {
            WriteLog(mes, typelog.Info, true);
        }

        static public void WriteException(string mes, Exception ex)
        {
            Logger.Error(mes, ex);
        }

        static public void WriteException(string mes)
        {
            Logger.Error(mes);
        }

        #region страрое логгирование в журнал приложений и служб
//        static public Returns ret;

//        static private string source;
//        static private string UserName;
//        static private string Password;

//        private const int LOGON32_INTERACTIV = 2;
//        private const int LOGON32_NETWORK    = 3;
//        private const int LOGON32_PROVIDER   = 0;

//        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
//        static extern int LogonUser (string lpszUserName, string lpszDomain, string lpszPassword, int dwLogonType,
//              int dwLogonProvider, ref IntPtr phToken);

//        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//        static extern int DuplicateToken(IntPtr hToken, int impersanationLevel, ref IntPtr hNewToken);

//        static WindowsImpersonationContext impContext;
//        static WindowsIdentity newIdentity;

//        const int counts = 10000;
//        public enum typelog : int
//        {
//            Error = EventLogEntryType.Error,
//            Info  = EventLogEntryType.Information,
//            Warn  = EventLogEntryType.Warning
//        }
//        static EventLog elog;

//        public static void SetLogger(ILog logger)
//        {
//        }

//        static private bool Impersanate()
//        {
//            try
//            {
//                impContext = null;
//                IntPtr token = IntPtr.Zero;
//                IntPtr tokenDuplicate = IntPtr.Zero;

//                if (LogonUser(UserName, Environment.MachineName, Password, LOGON32_NETWORK, LOGON32_PROVIDER, ref token) != 0)
//                {
//                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
//                    {
//                        newIdentity = new WindowsIdentity(tokenDuplicate);
//                        impContext = newIdentity.Impersonate();
//                    }
//                    ret.result = true;
//                    return true;
//                }
//            }
//            catch (Exception ex)
//            {
//                ret.text = "Ошибка имперсонации пользователя " + UserName + ": " + ex;
//                ret.result = false;
//            }
//            return false;
//        }

//        static public void StartLog(string source_name, string mes)
//        {
//            /*
//            int l = Constants.cons_User.Length;
//            int k = Constants.cons_User.LastIndexOf(";");

//            UserName = (Constants.cons_User.Substring(0, k)).Trim();
//            Password = (Constants.cons_User.Substring(k + 1, l - k - 1)).Trim();
//            */

//            UserName = Constants.Login;
//            Password = Constants.Password;

//            source = source_name;

//            elog = new EventLog();
//            elog.Log = Constants.name_logfile;
//            elog.Source = source;


//            if (Impersanate())
//            {
//                if (!EventLog.SourceExists(source))
//                    EventLog.CreateEventSource(source, Constants.name_logfile);


//                //elog.EntryWritten += new EntryWrittenEventHandler(OnEntryWritten);
//                if (mes.Trim() != "") WriteLog(mes, typelog.Info,false);

//                if (impContext != null) impContext.Undo();
//                impContext = null;
//            }

//        }

//        static public void Close(string mes)
//        {
//            if (Impersanate())
//            {
//                if (mes.Trim() != "") WriteLog(mes, typelog.Info, false);

//                elog.Close();
//                elog = null;

//                if (impContext != null) impContext.Undo();
//                impContext = null;
//            }
//        }

//        static public void WriteLog(string mes, MonitorLog.typelog t, bool to_imp)
//        {
//            bool b;
//            if (to_imp)
//                b = Impersanate();
//            else
//                b = true;

//            if (b)
//            {
//                if (elog.Entries.Count > counts)
//                {
//                    elog.Clear();
//                    elog.WriteEntry("Журнал очищен", EventLogEntryType.Information);
//                }

//                elog.WriteEntry(mes, (EventLogEntryType)t);
//            }
//        }

//        static public void WriteLog(string mes, typelog t, int eventID, short category, bool to_imp)
//        {
//#if DEBUG
//            mes = string.Concat(mes, Environment.NewLine, new StackTrace(Thread.CurrentThread, true));
//#endif
//            bool b;
//            if (to_imp)
//                b = Impersanate();
//            else
//                b = true;

//            if (b)
//            {
//                if (elog.Entries.Count > counts)
//                {
//                    elog.Clear();
//                    elog.WriteEntry("Журнал очищен", EventLogEntryType.Information);
//                }

//                elog.WriteEntry(mes, (EventLogEntryType)t, eventID, category);
//            }
//        }

//        static void OnEntryWritten(object sender, EntryWrittenEventArgs e)
//        {
//            Console.WriteLine("Запись в лог {0}. Время {1}. Позиция {2}",
//                  ((EventLog)sender).LogDisplayName,
//                  e.Entry.TimeWritten,
//                  e.Entry.Index);
//        }

//        static public void WriteException(string mes, Exception ex)
//        {
//            WriteLog(mes + ex.ToString(), typelog.Error, false);
        //        
    //}

        #endregion страрое логгирование в журнал приложений и служб
    }
}

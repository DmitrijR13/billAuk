using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace STCLINE.KP50.Global
{
    using System.Threading;
using NLog;

    //----------------------------------------------------------------------
    static public class MonitorLog2
    //----------------------------------------------------------------------
    {
        static public Returns ret;
        const int counts = 100000;
        static EventLog elog;

        public enum typelog : int
        {
            Error = EventLogEntryType.Error,
            Info = EventLogEntryType.Information,
            Warn = EventLogEntryType.Warning
        }
        public enum operlog
        {
            Start,
            Close,
            Write
        }

        static public void StartLog(string source_name, string mes)
        {
            OperationLog(operlog.Start, source_name, mes, typelog.Info, 0, 0);
        }
        static public void Close(string mes)
        {
            OperationLog(operlog.Close, "", mes, typelog.Info, 0, 0);
        }
        static public void WriteLog(string mes, typelog t, bool to_imp)
        {
            //to_imp не используется
            OperationLog(operlog.Write, "", mes, t, 0, 0);
        }
        static public void WriteLog(string mes, typelog t, int eventID, short category, bool to_imp)
        {
            //to_imp не используется
            OperationLog(operlog.Write, "", mes, t, eventID, category);
        }


        static public void OperationLog(operlog opl, string source, string mes, typelog t, int eventID, short category)
        {
            // поднимаем себе привилегии для записи в системный лог
            System.Security.Principal.WindowsImpersonationContext wic =
                   System.Security.Principal.WindowsIdentity.Impersonate(IntPtr.Zero);

            // далее действия с большими привилегиями
            if (opl == operlog.Start)
            {
                if (!EventLog.SourceExists(source))
                    EventLog.CreateEventSource(source, Constants.name_logfile);

                elog = new EventLog();
                elog.Log = Constants.name_logfile;
                elog.Source = source;
            }

            if (mes.Trim() != "") WriteLog(mes, t, eventID, category);

            if (opl == operlog.Close)
            {
                elog.Close();
                elog = null;
            }

            // возвращаем привелегии как было
            wic.Undo();
        }

        static public void WriteLog(string mes, typelog t, int eventID, short category)
        {
            if (elog.Entries.Count > counts)
            {
                elog.Clear();
                elog.WriteEntry("Журнал очищен", EventLogEntryType.Information);
            }

            if (eventID != 0 || category != 0)
                elog.WriteEntry(mes, (EventLogEntryType)t, eventID, category);
            else
                elog.WriteEntry(mes, (EventLogEntryType)t);
        }
    }





    //----------------------------------------------------------------------
    static public class MonitorLog
    //----------------------------------------------------------------------
    {
#if PG
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public enum typelog : int
        {
            Error = EventLogEntryType.Error,
            Info = EventLogEntryType.Information,
            Warn = EventLogEntryType.Warning
        }

        static public void WriteLog(string mes, typelog t, bool to_imp)
        {
            switch (t)
            {
                case typelog.Error:
                    logger.Error(mes + Environment.NewLine);
                    break;
                case typelog.Warn:
                    logger.Warn(mes + Environment.NewLine);
                    break;
                case typelog.Info:
                    logger.Info(mes);
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
            logger.ErrorException(mes, ex);
        }
#else
        static public Returns ret;

        static private string source;
        static private string UserName;
        static private string Password;

        private const int LOGON32_INTERACTIV = 2;
        private const int LOGON32_NETWORK    = 3;
        private const int LOGON32_PROVIDER   = 0;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        static extern int LogonUser (string lpszUserName, string lpszDomain, string lpszPassword, int dwLogonType,
              int dwLogonProvider, ref IntPtr phToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int DuplicateToken(IntPtr hToken, int impersanationLevel, ref IntPtr hNewToken);

        static WindowsImpersonationContext impContext;
        static WindowsIdentity newIdentity;

        const int counts = 10000;
        public enum typelog : int
        {
            Error = EventLogEntryType.Error,
            Info  = EventLogEntryType.Information,
            Warn  = EventLogEntryType.Warning
        }
        static EventLog elog;

        static private bool Impersanate()
        {
            try
            {
                impContext = null;
                IntPtr token = IntPtr.Zero;
                IntPtr tokenDuplicate = IntPtr.Zero;

                if (LogonUser(UserName, Environment.MachineName, Password, LOGON32_NETWORK, LOGON32_PROVIDER, ref token) != 0)
                {
                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                    {
                        newIdentity = new WindowsIdentity(tokenDuplicate);
                        impContext = newIdentity.Impersonate();
                    }
                    ret.result = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка имперсонации пользователя " + UserName + ": " + ex;
                ret.result = false;
            }
            return false;
        }

        static public void StartLog(string source_name, string mes)
        {
            /*
            int l = Constants.cons_User.Length;
            int k = Constants.cons_User.LastIndexOf(";");

            UserName = (Constants.cons_User.Substring(0, k)).Trim();
            Password = (Constants.cons_User.Substring(k + 1, l - k - 1)).Trim();
            */

            UserName = Constants.Login;
            Password = Constants.Password;

            source = source_name;

            elog = new EventLog();
            elog.Log = Constants.name_logfile;
            elog.Source = source;


            if (Impersanate())
            {
                if (!EventLog.SourceExists(source))
                    EventLog.CreateEventSource(source, Constants.name_logfile);


                //elog.EntryWritten += new EntryWrittenEventHandler(OnEntryWritten);
                if (mes.Trim() != "") WriteLog(mes, typelog.Info,false);

                if (impContext != null) impContext.Undo();
                impContext = null;
            }

        }

        static public void Close(string mes)
        {
            if (Impersanate())
            {
                if (mes.Trim() != "") WriteLog(mes, typelog.Info, false);

                elog.Close();
                elog = null;

                if (impContext != null) impContext.Undo();
                impContext = null;
            }
        }

        static public void WriteLog(string mes, typelog t, bool to_imp)
        {
            bool b;
            if (to_imp)
                b = Impersanate();
            else
                b = true;

            if (b)
            {
                if (elog.Entries.Count > counts)
                {
                    elog.Clear();
                    elog.WriteEntry("Журнал очищен", EventLogEntryType.Information);
                }

                elog.WriteEntry(mes, (EventLogEntryType)t);
            }
        }

        static public void WriteLog(string mes, typelog t, int eventID, short category, bool to_imp)
        {
#if DEBUG
            mes = string.Concat(mes, Environment.NewLine, new StackTrace(Thread.CurrentThread, true));
#endif
            bool b;
            if (to_imp)
                b = Impersanate();
            else
                b = true;

            if (b)
            {
                if (elog.Entries.Count > counts)
                {
                    elog.Clear();
                    elog.WriteEntry("Журнал очищен", EventLogEntryType.Information);
                }

                elog.WriteEntry(mes, (EventLogEntryType)t, eventID, category);
            }
        }

        static void OnEntryWritten(object sender, EntryWrittenEventArgs e)
        {
            Console.WriteLine("Запись в лог {0}. Время {1}. Позиция {2}",
                  ((EventLog)sender).LogDisplayName,
                  e.Entry.TimeWritten,
                  e.Entry.Index);
        }

        static public void WriteException(string mes, Exception ex)
        {
            WriteLog(mes + ex.ToString(), typelog.Error, false);
        }
#endif
    }
}

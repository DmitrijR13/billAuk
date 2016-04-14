using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace STCLINE.KP50.Utility
{
    public delegate void WriteLogDelegate (string msg, System.Diagnostics.EventLogEntryType type);

    public static class ClassLog
    {
        static ClassLog()
        {
            OnWriteLog = null;
        }

        //----------------------------------------------------------------------
        // Статус сообщения
        // Зыкин А.А.
        //----------------------------------------------------------------------
        public enum WriteLogStatus
        {
            Error = 1,
            Warning = 2,
            Information = 3
        }

        //----------------------------------------------------------------------
        //статичкие поле с именем и путем лог-файла 
        //----------------------------------------------------------------------
        private static string logFileName = "KP5.log";
        private static string logFilePath = @"c:\";
        private static bool usingLog = true;


        public static void SetLogFileName(string cfgLogPath, string logFile)
        {
            if (cfgLogPath.Trim() != "") ClassLog.logFilePath = cfgLogPath;
            else ClassLog.logFilePath = System.IO.Path.GetDirectoryName(logFile);
            ClassLog.logFileName = System.IO.Path.GetFileName(logFile);
        }
        public static string GetLogFileName()
        {
            return System.IO.Path.Combine(ClassLog.logFilePath, ClassLog.logFileName);
        }
        public static void LoggingOff()
        {
            ClassLog.usingLog = false;
        }
        public static void LoggingOn()
        {
            ClassLog.usingLog = true;
        }


        //----------------------------------------------------------------------

        public static void InitializeLog(string cfgLogPath, string hostLog, Utility.WriteLogDelegate onWriteLog)
        {
            ClassLog.OnWriteLog = null;
            if (onWriteLog != null)
            {
                ClassLog.OnWriteLog = delegate(string msg, System.Diagnostics.EventLogEntryType type)
                {
                    onWriteLog(msg, type);
                };
            }

            if ((hostLog ?? "") != "")
            {
                ClassLog.SetLogFileName(cfgLogPath, hostLog);
            }
        }
        public static void InitializeLog(string cfgLogPath, string hostLog)
        {
            InitializeLog(cfgLogPath, hostLog, null);
        }

        //----------------------------------------------------------------------
        //статичкие поле с именем и путем лог-файла 
        //----------------------------------------------------------------------
        public static WriteLogDelegate OnWriteLog = null;


        //----------------------------------------------------------------------
        //статичный метод. Вывод в файл протокола
        //----------------------------------------------------------------------
        static public void WriteLog(string msg)
        {
            ClassLog.WriteLog(new Exception(msg), WriteLogStatus.Information, "", "", "");
        }
        static public void WriteLog(Exception ex)
        {
            ClassLog.WriteLog(ex, WriteLogStatus.Error, "", "", "");
        }
        static public void WriteLog(Exception ex, string moduleName, string methodName, string userName)
        {
            ClassLog.WriteLog(ex, WriteLogStatus.Error, moduleName, methodName, userName);
        }

        static public void WriteLog(Exception ex, WriteLogStatus status,
                                    string moduleName, string methodName, string userName)
        {
            if (!ClassLog.usingLog) return;

            string _InnerException = "";
            string _StackTrace = "";

            if (ex.InnerException != null)
                _InnerException = Environment.NewLine + "=========================[InnerException]=========================" + Environment.NewLine +
                ex.InnerException.Message + Environment.NewLine;

            if (ex.StackTrace != null)
                _StackTrace = Environment.NewLine + "=========================[StackTrace]=========================" + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine;

          
            string hdr = DateTime.Now.ToString();
           
            string body = "";

            if (status == WriteLogStatus.Information)
                body = " " + ex.Message;
            else
                body =
                        "Системная ошибка!" + Environment.NewLine +
                        "=========================[Message]=========================" + Environment.NewLine +
                        ex.Message + Environment.NewLine +
                        _InnerException +
                        _StackTrace;

            string msg =
                Environment.NewLine + hdr + ": " +
                "[" + userName + "]" +
                "[" + moduleName + "]" +
                "[" + methodName + "]: " +
                body;

            //По умолчанию вывод в лог файл
            if (ClassLog.OnWriteLog == null || logFilePath != "")
            {
                string logFileName = @Path.Combine(ClassLog.logFilePath, ClassLog.logFileName);

                File.AppendAllText(logFileName, msg, System.Text.Encoding.GetEncoding("windows-1251"));
            }

            //Если задан делегат, то выводим с помощью него
            if (ClassLog.OnWriteLog != null)
            {
                OnWriteLog(msg, ToEventLogEntryType(status));
            }
        }

        public static void WriteFile(string fileName, string msg)
        {
            File.AppendAllText(fileName, msg, System.Text.Encoding.GetEncoding("windows-1251"));
        }

        private static System.Diagnostics.EventLogEntryType ToEventLogEntryType(WriteLogStatus status)
        {
            System.Diagnostics.EventLogEntryType type;
            switch (status)
            {
                case WriteLogStatus.Error:
                    type = System.Diagnostics.EventLogEntryType.Error;
                    break;
                case WriteLogStatus.Information:
                    type = System.Diagnostics.EventLogEntryType.Information;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false, "Не обработанное значение статуса WriteLogStatus" + status.ToString());
                    type = System.Diagnostics.EventLogEntryType.Error;
                    break;
            }
            return type;
        }


    }
}

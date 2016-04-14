using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KP50.DataBase.Update
{
    class UpdateInfo
    {
        private enum ArgType
        {
            None,
            Provider,
            Assembly,
            Prefix,
            Config,
            ConnectionString,
            WebConnection,
            Version,
            Help
        }

        public string Provider { get; private set; }
        public string ConnectionString { get; private set; }
        public string WebConnection { get; private set; }
        public string AssemblyPath { get; private set; }
        public string CentralPrefix { get; private set; }
        public long Version { get; private set; }
        public bool CheckUpdate { get; private set; }
        public bool NoHelp { get; private set; }
        public bool IgnoreLocalBank { get; private set; }

        public UpdateInfo(string[] args)
        {
            ArgType curr_arg = ArgType.None;
            CheckUpdate = false;
            NoHelp = false;
            IgnoreLocalBank = false;
            bool WaitArgument = false;
            foreach (string arg in args)
            {
                if ((arg.Contains("-") && arg.Length == 2) || arg.Contains("--"))
                {
                    if (WaitArgument) throw new ArgumentException(string.Format("Ожидается значение для ключа \"{0}\".", Enum.GetName(typeof(ArgType), curr_arg)));
                    switch (arg.TrimEnd().ToLower())
                    {
                        case "-i":
                        case "--informix":
                            this.Provider = "Informix";
                            WaitArgument = false;
                            break;
                        case "-p":
                        case "--postgresql":
                            this.Provider = "PostgreSQL";
                            WaitArgument = false;
                            break;
                        case "-a":
                        case "--assembly":
                            curr_arg = ArgType.Assembly;
                            WaitArgument = true;
                            break;
                        case "-f":
                        case "--prefix":
                            curr_arg = ArgType.Prefix;
                            WaitArgument = true;
                            break;
                        case "-c":
                        case "--config":
                            curr_arg = ArgType.Config;
                            throw new NotSupportedException();
                        case "-s":
                        case "--connectionstring":
                        case "--connection-string":
                            curr_arg = ArgType.ConnectionString;
                            WaitArgument = true;
                            break;
                        case "-w":
                        case "--webconnection":
                        case "--web-connection":
                            curr_arg = ArgType.WebConnection;
                            WaitArgument = true;
                            break;
                        case "-v":
                        case "--version":
                            curr_arg = ArgType.Version;
                            WaitArgument = true;
                            break;
                        case "-u":
                        case "--checkupdates":
                        case "--check-updates":
                            this.CheckUpdate = true;
                            WaitArgument = false;
                            break;
                        case "-n":
                        case "--no-help":
                            this.NoHelp = true;
                            WaitArgument = false;
                            break;
                        case "-l":
                        case "--ignore-local-bank":
                            this.IgnoreLocalBank = true;
                            WaitArgument = false;
                            break;
                        case "-h":
                        case "--help":
                            throw new SystemException();
                        default:
                            throw new ArgumentException(string.Format("Unknown key \"{0}\".", arg));
                    }
                }
                else
                {
                    if (!WaitArgument) throw new ArgumentException("Ожидается имя ключа.");
                    switch (curr_arg)
                    {
                        case ArgType.Assembly:
                            if (!File.Exists(Path.GetFullPath(arg))) throw new FileNotFoundException(string.Format("Assembly file \"{0}\" not found.", arg));
                            this.AssemblyPath = arg;
                            WaitArgument = false;
                            break;
                        case ArgType.Config:
                            if (!File.Exists(Path.GetFullPath(arg))) throw new FileNotFoundException(string.Format("Assembly file \"{0}\" not found.", arg));
                            throw new NotSupportedException();
                        case ArgType.ConnectionString:
                            if (string.IsNullOrWhiteSpace(arg)) throw new ArgumentNullException("Connection string can't be set null value.");
                            this.ConnectionString = arg;
                            WaitArgument = false;
                            break;
                        case ArgType.WebConnection:
                            if (string.IsNullOrWhiteSpace(arg)) throw new ArgumentNullException("Connection string can't be set null value.");
                            this.WebConnection = arg;
                            WaitArgument = false;
                            break;
                        case ArgType.Prefix:
                            if (string.IsNullOrWhiteSpace(arg)) throw new ArgumentNullException("Connection string can't be set null value.");
                            this.CentralPrefix = arg;
                            WaitArgument = false;
                            break;
                        case ArgType.Version:
                            try { this.Version = Convert.ToInt64(arg); }
                            catch (Exception) { throw new ArgumentException(string.Format("Can't convert argument \"{0}\" to Int64 type.", arg)); }
                            WaitArgument = false;
                            break;
                        default:
                            throw new ArgumentException(string.Format("Argumet \"{0}\" is not valid for key {1}.", arg, Enum.GetName(typeof(ArgType), curr_arg)));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(this.Provider)) throw new SystemException("Провайдер не задан или не существует.");
            if (string.IsNullOrWhiteSpace(this.ConnectionString) && string.IsNullOrWhiteSpace(this.WebConnection)) throw new SystemException("Отсутствует строка подключения.");
            if (string.IsNullOrWhiteSpace(this.AssemblyPath)) throw new SystemException("Не задана библиотека со сборками.");
            if (!string.IsNullOrWhiteSpace(this.CentralPrefix) && string.IsNullOrWhiteSpace(this.ConnectionString)) throw new SystemException("Не задан префикс центрального банка.");
            if (this.Version == 0) this.Version = -1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using STCLINE.KP50.Interfaces;
using System.Data;
using STCLINE.KP50.Global;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace STCLINE.KP50.DataBase
{
    public class PatchExecuter : DataBaseHead
    {
        //Тип параметров(переменных) которые могут встретиться в sq файле
        private class PVarParam
        {
            public string NameParam;
            public string ValueParam;
        }

        private class POperParam
        {
            public string NameParam;
            public string ValueParam;
        }

        private class PSpisFunc
        {
            public string NameFunc;
            public bool Result;
            public bool Farcall;
            public List<string> ParamString;
            public List<string> Spis;
        }

        #region Переменные
        public IDbConnection conn, conn2;
        StringBuilder sql = new StringBuilder();

        bool Flag = false;

        int CurrentNumber;//Номер текущего знака в строке
        ArrayList ListVar;//Список переменных
        ArrayList ListOper;//Список операторов
        FileInfo[] FileList;//Список файлов патчей
        string ErrorLog;//Имя файла с ошибками
        public string MainDir; //путь до каталога с патчами
        public string Pref;
        string unloaddir;//каталог выгрузки относительно каталога unl
        string LoadDir;//каталог загрузки относительно каталога unl
        bool isCritical;//признак критичности выполняемой секции
        bool isCancel;//Признак аварийного завершения
        bool isExit;//Признак выхода, если предидущая операция завершилась неудачно
        string nzp_patch;//Уникальный код выполняемого патча
        bool IoRes;//Результат выполнения последней операции
        int Numstring;//Номер строки в списке строк функции
        //ArrayList FuncList;//список функций
        List<PSpisFunc> FuncList; // список функции
        PSpisFunc CurrentFunc;//Текущая разбираемая функция
        bool isFunction;//Если в данный момент обрабатывается функция
        string MainBD;//Имя БД на которой будет производиться загрузка выгрузка
        string JarFile;// название файла архива
        bool Debug;
        string BaseName;
        bool trys;

        bool isIfFunc = false; //функция при if
        int beginYear = 0;
        int endYear = DateTime.Now.Year % 100;
        bool toFile; // выполнять скрипт или писать в файл
        //var XlApp, XlBook, XlSheet, XlSheets, Range; // Excel 97
        //property Flag:boolean read FFlag write FFlag  default false;//флаг неуспешного выполнения патча

        //string f;//Файл из которого читается текст обновления
        StreamReader f;//Файл из которого читается текст обновления
        StreamWriter f_cmd;//Файл который будет выполнен с внешней командой(например команда dbexport)
        StreamWriter f_scr; // Файл, в который будет записан скрипт обновления
        #endregion

        //конструктор
        public PatchExecuter(bool scriptInFile)
        {
            conn = GetConnection(Constants.cons_Kernel);
            conn2 = GetConnection(Constants.cons_Kernel);
            OpenDb(conn, true);
            OpenDb(conn2, true);

            IDataReader reader;
#if PG
            var ret = ExecRead(conn, out reader, "SELECT MIN(yearr), MAX(yearr) FROM " + Points.Pref + "_kernel.s_baselist WHERE idtype IN (1,4)", false);
#else
            var ret = ExecRead(conn, out reader, "SELECT MIN(yearr), MAX(yearr) FROM " + Points.Pref + "_kernel:s_baselist WHERE idtype IN (1,4)", false);
#endif
            if (ret.result && reader.Read())
            {
                beginYear = Convert.ToInt32(reader[0]) % 100;
                endYear = Convert.ToInt32(reader[1]) % 100;
            }
        }

        // Деструктор
        ~PatchExecuter()
        {
            //if ((conn != null) && (conn.State == System.Data.ConnectionState.Open)) conn.Close();
            //if ((conn2 != null) && (conn2.State == System.Data.ConnectionState.Open)) conn2.Close();
        }

        //функция прверки существования процесса
        public bool CheckNameProcess(int idProcess)
        {
            #region OLD
            //Process[] p = Process.GetProcesses();
            //for (int i = 0; i < p.Length; i++)
            //{
            //    if (p[i].Id == idProcess)
            //    {
            //        return true;
            //    }
            //}
            #endregion
            List<Process> list = new List<Process>(Process.GetProcesses());
            return list.Find(x => x.Id == idProcess) != null;
        }

        //уничтожает временные структуры
        void DestroyVarOper()
        {
            ListVar = null;
            ListOper = null;
            FileList = null;
        }

        //возвращает слово из строки
        string GetWord(string source)
        {
            source = source.Substring(CurrentNumber, source.Length - CurrentNumber);
            string RegExWord = "(?i)(?<=^[\\s_]*)((?<Word>\"[^\"]*\")|((#?)(?<Word>[0-9A-Za-z]+)))";
            Regex regex = new Regex(RegExWord);
            Match match = regex.Match(source);
            if (match.Success)
            {
                string s = match.Groups["Word"].ToString().Trim();
                CurrentNumber = s.Length + source.IndexOf(s) - CurrentNumber;
                return s;
            }
            else
            {
                return "";
            }
        }

        //Поиск значения переменной в таблице переменных
        string PoiskVar(string s)
        {
            foreach (PVarParam t in ListVar)
            {
                if (t.NameParam.Equals(s, StringComparison.InvariantCultureIgnoreCase)) return t.ValueParam;
            }
            return "-1";
        }

        //Установка операторов
        bool SetOper()
        {
            ListOper = new ArrayList();
            POperParam t = new POperParam();
            t.NameParam = "архив";
            t.ValueParam = "1";
            ListOper.Add(t);
            return true;
        }

        //Установка параметров
        bool SetParam()
        {
            ListVar = new ArrayList();
            PVarParam t = new PVarParam();
            t.NameParam = "pref";
            t.ValueParam = Pref;
            ListVar.Add(t);

            t = new PVarParam();
            t.NameParam = "server";
            t.ValueParam = DBManager.getServer(conn);
            ListVar.Add(t);

            t = new PVarParam();
            t.NameParam = "oldpref";
#if PG
            sql = new StringBuilder("select dbname from s_baselist sb, Logtodb ld, s_logicDBlist sl " +
                                    " where sb.nzp_bl=ld.nzp_bl and ld.nzp_ldb=sl.nzp_ldb " +
                                    " and sl.LDBName=\"KZN_OLD\" and idtype=2");
#else
sql = new StringBuilder("select dbname from s_baselist sb, Logtodb ld, s_logicDBlist sl " +
                        " where sb.nzp_bl=ld.nzp_bl and ld.nzp_ldb=sl.nzp_ldb " +
                        " and sl.LDBName=\"KZN_OLD\" and idtype=2");
#endif
            Returns ret = Utils.InitReturns();

            try
            {
                IDataReader reader;
                ret = ExecRead(conn, out reader, sql.ToString(), false);

                if (reader != null)
                {
                    if ((reader.Read()) && (reader["dbname"] != DBNull.Value))
                    {
                        string s = Convert.ToString(reader["dbname"]).Trim();
                        t.ValueParam = s.Substring(0, s.IndexOf("_data"));
                    }
                }
                else
                {
                    t.ValueParam = Points.Pref;
                }
                ListVar.Add(t);
            }
            catch (Exception ex)
            {
                //MonitorLog.WriteLog("Ошибка обновления. " + ex.Message, MonitorLog.typelog.Error, true);
                MonitorLog.WriteException("Ошибка обновления.", ex);
            }
            return true;
        }

        //Поиск новых патчей в каталоге патчей
        bool SearchFiles()
        {
            DirectoryInfo di = new DirectoryInfo(MainDir);
            FileList = di.GetFiles("*.sqc");
            return FileList.Length > 0;
        }

        //Проверка возможности транзакции
        bool IsTransaction()
        {
            try
            {
                IDbTransaction trans = conn.BeginTransaction();
                trans.Rollback();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Просмотр лога
        void ViewLog()
        {
            if ((!Flag) && File.Exists(ErrorLog))
            {
                Process.Start("notepad.exe", ErrorLog);
                return;
            }
        }

        //Ошибка в критической секции
        void CriticalTermination()
        {
            isCancel = true;
        }

        //возвращает результаты выполнения последнего патча
        bool GetLastPatchError()
        {
            //IfxConnection conn = GetConnection(Constants.cons_Kernel);
            Returns ret = Utils.InitReturns();
#if PG
            sql = new StringBuilder(" SELECT priznak FROM " + Pref + "_kernel.patches "
                        + " WHERE nzp_patch in ( "
                        + " SELECT Max(nzp_patch) as nzp_patch from patches)");
#else
            sql = new StringBuilder(" SELECT priznak FROM patches "
                        + " WHERE nzp_patch in ( "
                        + " SELECT Max(nzp_patch) as nzp_patch from patches)");
#endif
            try
            {
                int priznak = Convert.ToInt32(ExecScalar(conn, sql.ToString(), out ret, true));
                return true;
            }
            catch (Exception ex)
            {
                //MonitorLog.WriteLog("Ошибка получения результата последнего патча: " + ex.Message, MonitorLog.typelog.Error, true);
                MonitorLog.WriteException("Ошибка получения результата последнего патча.", ex);
                conn.Close();
                return false;
            }
        }

        //Записывает результат выполнения патча в таблицу
        void WriteBase()
        {
#if PG
  sql = new StringBuilder("set search_path to '" + Pref + "_kernel'");
#else
  sql = new StringBuilder("database " + conn.Database);
#endif
            Returns ret = Utils.InitReturns();

            try
            {
                ret = ExecSQL(conn, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Не найдена основная база. WriteBase.", MonitorLog.typelog.Error, true);
                    return;
                }
            }
            catch (Exception ex)
            {
                //MonitorLog.WriteLog("Ошибка выбора базы. WriteBase. " + ex.Message, MonitorLog.typelog.Error, true);
                MonitorLog.WriteException("Ошибка выбора базы. WriteBase.", ex);
                return;
            }

#if PG
            sql = new StringBuilder("UPDATE patches SET (version,priznak)=(now(),");
#else
sql = new StringBuilder("UPDATE patches SET (version,priznak)=(current year to minute,");
#endif
            if (!Flag)
            {
                if (isCancel)
                {
                    sql.Append("3)");
                }
                else
                {
                    sql.Append("2)");
                }
            }
            else
            {
#if PG
                sql.Append("4) WHERE nzp_patch = " + nzp_patch);
#else
                sql.Append("4) WHERE nzp_patch = " + nzp_patch);
#endif
            }

            try
            {
                ret = ExecSQL(conn, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Не удалось обновить таблицу. WriteBase.", MonitorLog.typelog.Error, true);
                    return;
                }
            }
            catch (Exception ex)
            {
                //MonitorLog.WriteLog("Ошибка обновления таблицы. WriteBase. " + ex.Message, MonitorLog.typelog.Error, true);
                MonitorLog.WriteException("Ошибка обновления таблицы. WriteBase. ", ex);
                return;
            }
        }

        //Записывает признак выполнения начала обновления в базу
        void WriteStart()
        {
#if PG
            sql = new StringBuilder("set search_path to '" + Pref + "_kernel'");
#else
            sql = new StringBuilder("database " + conn.Database);
#endif
            Returns ret = Utils.InitReturns();
            try
            {
                ret = ExecSQL(conn, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Не найдена основная база. WriteStart.", MonitorLog.typelog.Error, true);
                    return;
                }
            }
            catch (Exception ex)
            {
                //MonitorLog.WriteLog("Ошибка выбора базы. WriteStart. " + ex.Message, MonitorLog.typelog.Error, true);
                MonitorLog.WriteException("Ошибка выбора базы. WriteStart. ", ex);
                conn.Close();
                return;
            }
#if PG
            sql = new StringBuilder("INSERT INTO patches (version,priznak) VALUES (now(),1)");
#else
            sql = new StringBuilder("INSERT INTO patches (version,priznak) VALUES (current year to minute,1)");
#endif
            try
            {
                ret = ExecSQL(conn, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Не записать признак обновления. WriteStart.", MonitorLog.typelog.Error, true);
                    return;
                }
#if PG
                sql = new StringBuilder("SELECT nzp_patch from patches ORDER BY nzp_patch DESC LIMIT 1");
#else
sql = new StringBuilder("SELECT FIRST 1 nzp_patch from patches ORDER BY nzp_patch DESC");
#endif
                nzp_patch = Convert.ToString(ExecScalar(conn, sql.ToString(), out ret, true));
            }
            catch (Exception ex)
            {
                //MonitorLog.WriteLog("Ошибка записи признака обновления. WriteStart. " + ex.Message, MonitorLog.typelog.Error, true);
                MonitorLog.WriteException("Ошибка записи признака обновления. WriteStart. ", ex);
                return;
            }
        }

        //Возвращает прочитанную строку из файла или временного списка
        bool GetString(out string st, bool stayComment)
        {
            st = "";
            if (!isFunction)
            {
                if ((st = f.ReadLine()) == null)
                {
                    return false;
                }
                if (st.Trim() != "")
                {
                    st = StringCrypter.Decrypt(st, StringCrypter.pass);
                    if (st.Contains("--"))
                        if (stayComment)
                        {
                            st = Encoding.GetEncoding("iso-8859-5").GetString(Encoding.GetEncoding(1251).GetBytes(st));
                        }
                        else
                        {
                            if (st.IndexOf("--") == 0) st = "";
                            else st = st.Substring(0, st.IndexOf("--"));
                        }
                }
            }
            else
            {
                Numstring++;
                if (Numstring < CurrentFunc.Spis.Count)
                {
                    st = CurrentFunc.Spis[Numstring];
                }
                else
                {
                    return false;
                }
            }
            st = st.Trim();
            return true;
        }

        // возвращает функцию по имени
        PSpisFunc FindFunc(string nameFunc)
        {
            if (nameFunc.IndexOf('(') < 0) return null;
            if (FuncList == null) return null;

            string s = nameFunc.Substring(0, nameFunc.IndexOf('(')).Trim();

            foreach (var fun in FuncList)
            {
                if (fun.NameFunc.Equals(s, StringComparison.OrdinalIgnoreCase)) return fun;
            }
            return null;
        }

        //Уничтожение списка переменных и функций после "отработки" файла
        void DeleteOldVarPer()
        {
            if (FuncList != null)
            {
                int i = 0;
                while (i < FuncList.Count)
                {
                    if (!FuncList[i].Farcall)
                    {
                        FuncList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            if (ListVar.Count >= 2)
            {
                ListVar.RemoveRange(2, ListVar.Count - 2);
            }
        }

        //запись строки в лог файл
        void WriteDebug(string s)
        {
            if (Debug)
            {
                StreamWriter sw = new StreamWriter(Path.Combine(MainDir, "Debug.dbg"), true);
                sw.WriteLine(s);
                sw.Close();
            }
        }

        //Пишет в cmd-шный файл переменные среды Informix'a
        void MakeEnvironment()
        {
            f_cmd.WriteLine(@"set INFORMIXDIR=C:\INFORMIX");
            f_cmd.WriteLine(@"set INFORMIXSERVER=" + DBManager.getServer(conn));
            f_cmd.WriteLine(@"set ONCONFIG=ONCONFIG." + DBManager.getServer(conn));
            f_cmd.WriteLine(@"set PATH=C:\INFORMIX\bin;%PATH%;");
            f_cmd.WriteLine(@"set INFORMIXSQLHOSTS=\\" + Environment.MachineName);
            f_cmd.WriteLine(@"set DBTEMP=C:\INFORMIX\infxtmp");
            f_cmd.WriteLine(@"set CLIENT_LOCALE=ru_RU.915");
            f_cmd.WriteLine(@"set DB_LOCALE=ru_RU.915");
            f_cmd.WriteLine(@"set SERVER_LOCALE=ru_RU.915");
            f_cmd.WriteLine(@"set DBLANG=EN_US.CP1252");
            f_cmd.WriteLine(@"set DBDATE=DMY4.");
            f_cmd.WriteLine(@"set DBMONEY=.");
            f_cmd.WriteLine(@"set DBNLS=2");
            f_cmd.WriteLine(@"mode con codepage select=866");
        }

        //Заносит функию в память
        void ReadFunction(string s, bool call)
        {
            if (FuncList == null) FuncList = new List<PSpisFunc>();
            string s1 = s.Substring(s.IndexOf("sqfunction", StringComparison.OrdinalIgnoreCase) + 10, s.Length - s.IndexOf("sqfunction", StringComparison.OrdinalIgnoreCase) - 10);
            if ((s.IndexOf('(') < 0) || (s.IndexOf(')') < 0)) return;

            PSpisFunc fun = new PSpisFunc();
            fun.NameFunc = s1.Substring(0, s1.IndexOf('(')).Trim();
            fun.Spis = new List<string>();
            fun.Farcall = call;
            fun.ParamString = new List<string>();
            string s2 = s1.Substring(s1.IndexOf('(') + 1, s1.LastIndexOf(')') - s1.IndexOf('(') - 1).Trim();

            while (s2.IndexOf(',') >= 0)
            {
                s1 = s2.Substring(0, s2.IndexOf(','));
                fun.ParamString.Add(s1);
                s2 = s2.Substring(s2.IndexOf(',') + 1, s2.Length - s2.IndexOf(',') - 1).Trim();
            }
            fun.ParamString.Add(s2);

            while (!f.EndOfStream)
            {
                GetString(out s1, true);
                if (string.Equals(s1, "end sqfunction;", StringComparison.OrdinalIgnoreCase)) break;
                if (s1.Trim() != "") fun.Spis.Add(s1);
            }
            FuncList.Add(fun);
        }

        void SetSqVar(string s)
        {
            string param = GetWord(s);
            if (s[CurrentNumber] == '=')
            {
                CurrentNumber++;
            }
            string znach = s.Substring(CurrentNumber, s.Length - CurrentNumber);
            PVarParam t = new PVarParam();
            t.NameParam = param;
            t.ValueParam = znach;
            ListVar.Add(t);
        }

        //устанавливает критическую секцию
        void SetCriticalSection(string s)
        {
            if (s.Length <= 2) return;

            if ((s[2] == 'c') && (s[3] == '+')) isCritical = true;
            else if ((s[2] == 'c') && (s[3] == '-')) isCritical = false;

            WriteDebug(s);
        }

        //Запись ошибок выполнения в файл
        void WriteLog(string s, bool reg)
        {
            if ((!isCritical) && (trys)) return;

            WriteDebug(s);
            string ErrorDir = Path.Combine(MainDir, "log");
            if (!Directory.Exists(ErrorDir)) Directory.CreateDirectory(ErrorDir);
            StreamWriter f1 = new StreamWriter(Path.Combine(ErrorDir, ErrorLog), true);
            CurrentNumber = 0;
            IoRes = false;
            f1.WriteLine(s);
            Flag = false;

            if (reg)
            {
                f1.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm") + sql.ToString());
            }
            else
            {
                f1.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            }

            if (!isCritical)
            {
                f1.WriteLine("Критическая ошибка во время выполнения обновления БД");
                CriticalTermination();
            }
            f1.Close();
        }

        //Возвращает строку с подставленными переменными
        string ReplaceVar(string s)
        {
            //Переменные, которые находятся в строке начинаются с #
            int i = s.IndexOf('#');
            string s1 = s;
            CurrentNumber = 0;
            while (i >= 0)
            {
                if (i > 0) CurrentNumber = i;
                string s2 = GetWord(s1);
                string s3 = PoiskVar(s2);
                if (!string.IsNullOrEmpty(s3) && s3 != "-1")
                {
                    Regex regex = new Regex("#" + s2);//new Regex("#" + s2 + "(?=[\\s!@$%^&*()_\\-+='\"<>?/{}.,:;]+)");
                    s1 = regex.Replace(s1, s3);
                }
                else
                {
                    WriteLog("Неопределенная переменная " + s2, false);
                    return null;
                }
                i = s1.IndexOf('#');
            }
            return s1.Trim();
        }

        //архивирует sq-шные файлы
        void MoveToArchive()
        {
            StreamWriter sw = new StreamWriter(Path.Combine(MainDir, "1.cmd"));
            //Если каталога аривов не, то создааем его
            string TempDir = Path.Combine(MainDir, "temp");
            if (!Directory.Exists(TempDir))
            {
                Directory.CreateDirectory(TempDir);
            }
            string sourceDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(TempDir);
            JarFile = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");
            sw.WriteLine("jar a \"" + Path.Combine(Path.Combine(MainDir, "temp"), JarFile) + ".j\" *.sq");
            sw.WriteLine("attrib -r -s " + Path.Combine(MainDir, "*.*"));
            //sw.WriteLine("del " + Path.Combine(MainDir, "*.sq"));
            sw.WriteLine("exit");
            sw.Close();

            Process cmd = new Process();
            cmd.StartInfo.WorkingDirectory = MainDir;
            cmd.StartInfo.FileName = "1.cmd";
            cmd.StartInfo.Arguments = "";
            cmd.Start();
            //ожидаем завершение выполнения командного файла
            while (CheckNameProcess(cmd.Id))
            {
                System.Threading.Thread.Sleep(500);
            }

            try
            {
                File.Delete(Path.Combine(MainDir, "1.cmd"));
            }
            catch
            {
                WriteLog("Невозможно удалить файл 1.cmd", false);
            }
            Directory.SetCurrentDirectory(sourceDir);
        }

        //Сохраняет выгрузки из таблиц в архив
        void SaveUnl()
        {
            string LocalDir;
            System.Threading.Thread.Sleep(100);
            try
            {
                File.Delete(Path.Combine(MainDir, "2.cmd"));
            }
            catch { };
            System.Threading.Thread.Sleep(100);
            StreamWriter BatFile = new StreamWriter(Path.Combine(MainDir, "2.cmd"), false);
            string UnlDir = Path.Combine(MainDir, "unl");
            if (!Directory.Exists(UnlDir))
            {
                Directory.CreateDirectory(UnlDir);
            }
            string sourceDir = Directory.GetCurrentDirectory();
            if (unloaddir.IndexOf(':') >= 0)
            {
                LocalDir = unloaddir;
            }
            else
            {
                if (unloaddir == "")
                {
                    LocalDir = UnlDir;
                }
                else
                {
                    LocalDir = Path.Combine(MainDir, unloaddir);
                }
            }

            try
            {
                StreamWriter f_SQL = new StreamWriter(Path.Combine(LocalDir, "p.sql"), false);
                f_SQL.WriteLine("p");
                f_SQL.Close();
            }
            catch { }

            if (!File.Exists(Path.Combine(LocalDir, "p.sql")))
            {
                try
                {
                    Directory.CreateDirectory(LocalDir);
                }
                catch
                {
                    WriteLog("Невозможно создать каталог выгрузки !!", false);
                    LocalDir = UnlDir;
                }
            }

            try
            {
                File.Delete(Path.Combine(LocalDir, "p.sql"));
            }
            catch { }

            Directory.SetCurrentDirectory(MainDir);
            BatFile.WriteLine("attrib -r -s " + Path.Combine(MainDir, "*.*"));
            BatFile.WriteLine("jar a \"" + Path.Combine(LocalDir, DateTime.Now.ToString("yyyy_MM_dd_HH_mm")) + "u.j\" *.unl");
            BatFile.WriteLine("del \"" + Path.Combine(MainDir, "*.unl"));
            BatFile.WriteLine("exit");
            BatFile.Close();

            Process cmd = new Process();
            cmd.StartInfo.WorkingDirectory = MainDir;
            cmd.StartInfo.FileName = "2.cmd";
            cmd.StartInfo.Arguments = "";
            cmd.Start();
            //ожидаем завершение выполнения командного файла
            while (CheckNameProcess(cmd.Id))
            {
                System.Threading.Thread.Sleep(500);
            }
            cmd.Close();

            try
            {
                File.Delete(Path.Combine(MainDir, "2.cmd"));
            }
            catch
            {
                WriteLog("Невозможно удалить файл 2.cmd", false);
            }
            Directory.SetCurrentDirectory(sourceDir);
        }

        //загрузка таблицы по частям из одной в другую
        bool insTmp(string s)
        {
            IoRes = true;
            string s1 = s.Trim();
            s1 = s1.Substring(s1.IndexOf(' ') + 1, s1.Length - s1.IndexOf(' ') - 1);
            string FromTable = s1.Substring(0, s1.IndexOf(',')).Trim();
            string ToTable = s1.Substring(s1.IndexOf(',') + 1, s.Length - s1.IndexOf(',') - 1).Trim().Replace(";", "");

            try
            {
                Returns ret = Utils.InitReturns();
#if PG
                string sql = " SELECT oid,* from " + FromTable;
#else
                string sql = " SELECT rowid,* from " + FromTable;
#endif
                IDataReader reader;
                ret = ExecRead(conn2, out reader, sql, true);
                if ((!ret.result) || (reader == null)) return true;
                while (reader.Read())
                {
#if PG
                    string sql2 = " INSERT INTO " + ToTable
                                            + " SELECT * FROM " + FromTable + " a"
                                            + " WHERE a.oid=" + Convert.ToString(reader["oid"]);
#else
string sql2 = " INSERT INTO " + ToTable
                        + " SELECT * FROM " + FromTable + " a"
                        + " WHERE a.rowid=" + Convert.ToString(reader["rowid"]);
#endif
                    ret = ExecSQL(conn2, sql2, true);
                    if ((!ret.result) || (reader == null)) return true;
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, true);
                return true;
            }
            return true;
        }

        //Создает архив базы данных в каталоге
        bool MakeArchive(string s)
        {
            string buf;//Промежуточная строка для записи в файл

            #region Получаем имя БД
            string BaseName = GetWord(s);//Имя базы данных, которую необходимо сохранить
            int i = CurrentNumber;//Промежуточна переменная для хранения курсора в строке
            BaseName = ReplaceVar(BaseName);

            if (s[i] == '_')
            {
                CurrentNumber = i + 1;
                buf = GetWord(s);
                i = CurrentNumber;
                BaseName += '_' + ReplaceVar(buf);
            }

            CurrentNumber = i + 1;
            WriteDebug(BaseName);
            #endregion

            #region Получаем каталог, в который нужно сохранить БД
            string SaveDir = s.Substring(CurrentNumber, s.Length - CurrentNumber).Trim();//Каталог куда будет сохранена база данных
            if (SaveDir[SaveDir.Length - 1] == ';')
            {
                SaveDir = SaveDir.Substring(0, s.Length - 1);
            }

            WriteDebug(SaveDir);
            #endregion

            #region Создание папки для обновления
            string SourceDir = Directory.GetCurrentDirectory();//Каталог из которого запустили программу
            //Если каталог не существует пытаемся создать его
            if (!Directory.Exists(SaveDir))
            {
                try
                {
                    Directory.CreateDirectory(SaveDir);
                    WriteDebug("Создана папка " + SaveDir);
                }
                catch
                {
                    if (!Directory.Exists(Path.Combine(SaveDir, "arc")))
                    {
                        SaveDir = Path.Combine(SaveDir, "arc");
                        Directory.CreateDirectory(SaveDir);

                    }
                }
            }
            #endregion

            #region Создаем командый файл содержащий перечь переменных среды и команду для выгрузки БД
            f_cmd = new StreamWriter(Path.Combine(SaveDir, "archive.cmd"));
            MakeEnvironment();
            buf = "dbexport " + BaseName;
            WriteDebug(buf);
            f_cmd.WriteLine(buf);
            buf = "exit";
            f_cmd.WriteLine(buf);
            f_cmd.Close();
            Directory.SetCurrentDirectory(SaveDir);
            #endregion

            #region Выполняем командный файл
            Process cmd = new Process();
            cmd.StartInfo.WorkingDirectory = SaveDir;
            cmd.StartInfo.FileName = "archive.cmd";
            cmd.StartInfo.Arguments = "";
            cmd.Start();
            //ожидаем завершение выполнения командного файла
            while (CheckNameProcess(cmd.Id))
            {
                System.Threading.Thread.Sleep(500);
            }
            cmd.Close();

            try
            {
                File.Delete(Path.Combine(SaveDir, "archive.cmd"));
            }
            catch { }
            #endregion

            #region Производим проверку на успешное выполнение выгрузки БД
            bool Success = false;//Признак успешного сохранения БД
            using (var fexp = File.Open(Path.Combine(SaveDir, "dbexport.out"), FileMode.Open, FileAccess.Read))
            {
                var buff = new byte[30];
                fexp.Position = fexp.Length - 30;
                fexp.Read(buff, 0, 30);
                Success = Encoding.ASCII.GetString(buff).IndexOf("dbexport completed", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            Directory.SetCurrentDirectory(SourceDir);
            #endregion

            #region Если выгрузка завершилась неудачно, то вызываем аварийную ситуацию
            if (!Success)
            {
                isCritical = true;
                WriteLog("Аварийное завершение архивации!!!", false);
            }
            #endregion

            return true;
        }

        //выполняет sql
        bool RunQuery(string s)
        {
            string s1 = ReplaceVar(s); // Промежуточная строка для хранения строки Sql запроса
            Returns ret = Utils.InitReturns();
            bool Res = true;
            sql = new StringBuilder();
            sql.Append(s1 + Environment.NewLine);

            while ((s[s.Length - 1] != ';') && (Res))
            {
                Res = GetString(out s, false);
                if (s == "")
                {
                    s = " ";
                    continue;
                }
                else if (s.Length > 1)
                {
                    if ((s.Substring(0, 2) == "--") || (s.Substring(0, 2) == "//")) continue;
                }
                s1 = ReplaceVar(s);
                sql.Append(s1 + Environment.NewLine);
            }

            try
            {
                WriteDebug(sql.ToString());
#if PG
                if (sql.ToString().Contains("set search_path to"))
                {
                    // подключение к схеме
                    string schema_name = Regex.Match(sql.ToString(), "(?<=set search_path to '?)\\w+").Value;
                    if (!string.IsNullOrEmpty(schema_name))
                    {
                        int count = Convert.ToInt32(ExecScalar(conn, "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = '" + schema_name + "';", out ret, false));
                        if (count == 0)
                        {
                            // схемы нет
                            IoRes = trys;
                            return trys;
                        }
                        else
                        {
                            ret = ExecSQL(conn, sql.ToString(), !trys);
                            IoRes = ret.result || trys;
                            return ret.result || trys;
                        }
                    }
                    else
                    {
                        // не найдено имя схемы
                        IoRes = trys;
                        return trys;
                    }
                }
                else
#endif
                ret = ExecSQL(conn, sql.ToString(), !trys && !isIfFunc);
                IoRes = ret.result || trys;
                return ret.result || trys;
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, true);
                return trys;
            }
        }

        //Выполняет загрузку процедуры в БД
        string MakeProcedure(string s2)
        {
            Returns ret = Utils.InitReturns();
            sql = new StringBuilder();
            sql.Append(ReplaceVar(s2) + Environment.NewLine);
            bool Res1 = true;
            string s = "";//Промежуточная строка для хранения строки процедуры считываемой из файла

#if PG
            while (!Regex.Match(s, "(?i)end;\\s*\\$\\$\\s*language\\s+plpgsql\\s*;").Success && (Res1))
#else
            while (!Regex.Match(s, "(?i)end procedure(;?)").Success && (Res1))
#endif
            {
                Res1 = GetString(out s, true);
                //if (string.IsNullOrEmpty(s)) continue;
                //Пропускаем коментарии
                //if ((s.Length > 1) && (s.Substring(0, 2) == "--")) continue;
                sql.Append(ReplaceVar(s) + Environment.NewLine);
            }

            if (sql.ToString().Length <= 6) return "";
            try
            {
                WriteDebug(sql.ToString());
                ret = ExecSQL(conn, sql.ToString(), !trys && !isIfFunc);
                IoRes = true;
            }
            catch
            {
                WriteLog("Ошибка при выполнении процедуры " + sql.ToString(), false);
            }
            return "";
        }

        //Выполняет загрузку процедуры в БД
        string MakeFunction(string s2)
        {
            Returns ret = Utils.InitReturns();
            sql = new StringBuilder();
            sql.Append(ReplaceVar(s2) + Environment.NewLine);
            bool Res1 = true;
            string s = "";//Промежуточная строка для хранения строки процедуры считываемой из файла

#if PG
            while (!Regex.Match(s, "(?i)end;\\s*\\$\\$\\s*language\\s+plpgsql\\s*;").Success && (Res1))
#else
            while (!Regex.Match(s, "(?i)end function(;?)").Success && (Res1))
#endif
            {
                Res1 = GetString(out s, true);
                //if (string.IsNullOrEmpty(s)) continue;
                //Пропускаем коментарии
                //if ((s.Length > 1) && (s.Substring(0, 2) == "--")) continue;
                sql.Append(ReplaceVar(s) + Environment.NewLine);
            }

            if (sql.ToString().Length <= 6) return "";
            try
            {
                WriteDebug(sql.ToString());
                ret = ExecSQL(conn, sql.ToString(), !trys && !isIfFunc);
                IoRes = true;
            }
            catch
            {
                WriteLog("Ошибка при выполнении процедуры " + sql.ToString(), false);
            }
            return "";
        }

        //выгрузка таблицы
        bool UnloadTable(string s)
        {
            IoRes = true;
            string buf;

            #region создание cmd файла
            f_cmd = new StreamWriter(Path.Combine(MainDir, "run.cmd"), false);
            MakeEnvironment();
            if (MainBD == "")
            {
                buf = "dbaccess -e " + conn.Database + " unl.sql >" + ErrorLog + " 2>&1";
            }
            else
            {
                buf = "dbaccess -e " + MainBD + " unl.sql >" + ErrorLog + " 2>&1";
            }
            WriteDebug(buf);
            f_cmd.WriteLine(buf);
            buf = "exit";
            WriteDebug(buf);
            f_cmd.WriteLine(buf);
            f_cmd.Close();
            #endregion

            #region создание sql файла
            using (StreamWriter f_sql = new StreamWriter(Path.Combine(MainDir, "unl.sql"), false))
            {
                buf = ReplaceVar(s);
                f_sql.WriteLine(buf);
                WriteDebug(buf);
                bool Res1 = true;
                while ((buf[buf.Length - 1] != ';') && (Res1))
                {
                    Res1 = GetString(out buf, false);
                    if (!Res1) break;
                    if (buf == "")
                    {
                        buf = " ";
                        continue;
                    }
                    else if ((buf.Length > 1) && (buf.Substring(0, 2) == "--")) continue;
                    buf = ReplaceVar(buf);
                    f_sql.WriteLine(buf);
                    WriteDebug(buf);
                }
                f_sql.Close();
            }
            #endregion

            #region выполнение командного файла
            //ожидаем завершение выполнения командного файла
            Process cmd = new Process();
            cmd.StartInfo.WorkingDirectory = MainDir;
            cmd.StartInfo.FileName = "run.cmd";
            cmd.StartInfo.Arguments = "";
            cmd.Start();
            //ожидаем завершение выполнения командного файла
            while (CheckNameProcess(cmd.Id))
            {
                System.Threading.Thread.Sleep(500);
            }
            cmd.Close();
            #endregion

            #region чтение лога
            string[] lines = File.ReadAllLines(Path.Combine(MainDir, ErrorLog));
            foreach (string s1 in lines)
            {
                if (s1.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0) continue;
                buf = "";
                foreach (string s2 in lines)
                {
                    if (!string.IsNullOrEmpty(s2.Trim())) buf += Environment.NewLine + s2;
                }
                WriteLog(buf, false);
                break;
            }
            #endregion

            return true;
        }

        //загрузка таблицы
        bool LoadTable(string s)
        {
            IoRes = true;
            string SourceDir = Directory.GetCurrentDirectory();
            string buf;

            #region проверка возможности создания файла
            string LocalDir = (LoadDir.IndexOf(':') >= 0) ? LoadDir : Path.Combine(MainDir, LoadDir);
            using (StreamWriter f_sql = new StreamWriter(Path.Combine(LocalDir, "p.sql")))
            {
                f_sql.WriteLine("p");
                f_sql.Close();
            }
            if (File.Exists(Path.Combine(LocalDir, "p.sql")))
            {
                Directory.SetCurrentDirectory(LocalDir);
            }
            else
            {
                WriteLog("не найден каталог загрузки", false);
                LocalDir = MainDir;
            }

            try
            {
                File.Delete(Path.Combine(LocalDir, "p.sql"));
            }
            catch { }
            #endregion

            #region создание cmd файла
            f_cmd = new StreamWriter(Path.Combine(MainDir, "run.cmd"), false);
            MakeEnvironment();
            if (MainBD == "")
            {
                buf = "dbaccess -e " + conn.Database + " load.sql >" + ErrorLog + " 2>&1";
            }
            else
            {
                buf = "dbaccess -e " + MainBD + " load.sql >" + ErrorLog + " 2>&1";
            }
            WriteDebug(buf);
            f_cmd.WriteLine(buf);
            buf = "exit";
            WriteDebug(buf);
            f_cmd.WriteLine(buf);
            f_cmd.Close();
            #endregion

            #region создание sql файла
            using (StreamWriter f_sql = new StreamWriter(Path.Combine(MainDir, "load.sql"), false))
            {
                buf = ReplaceVar(s);
                f_sql.WriteLine(buf);
                WriteDebug(buf);
                bool Res1 = true;
                while ((buf[buf.Length - 1] != ';') && (Res1))
                {
                    Res1 = GetString(out buf, false);
                    if (!Res1) break;
                    if (buf == "")
                    {
                        buf = " ";
                        continue;
                    }
                    else if ((buf.Length > 1) && (buf.Substring(0, 2) == "--")) continue;
                    buf = ReplaceVar(buf);
                    f_sql.WriteLine(buf);
                    WriteDebug(buf);
                }
                f_sql.Close();
            }
            #endregion

            #region выполнение командного файла
            //ожидаем завершение выполнения командного файла
            Process cmd = new Process();
            cmd.StartInfo.WorkingDirectory = MainDir;
            cmd.StartInfo.FileName = "run.cmd";
            cmd.StartInfo.Arguments = "";
            cmd.Start();
            //ожидаем завершение выполнения командного файла
            while (CheckNameProcess(cmd.Id))
            {
                System.Threading.Thread.Sleep(500);
            }
            cmd.Close();
            #endregion

            #region чтение лога
            string[] lines = File.ReadAllLines(Path.Combine(MainDir, ErrorLog));
            foreach (string s1 in lines)
            {
                if (s1.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0) continue;
                buf = "";
                foreach (string s2 in lines)
                {
                    if (!string.IsNullOrEmpty(s2.Trim())) buf += Environment.NewLine + s2;
                }
                WriteLog(buf, false);
                break;
            }
            #endregion

            #region удаление файлов
            try
            {
                File.Delete(Path.Combine(LocalDir, "run.cmd"));
            }
            catch (Exception ex)
            {
                Flag = false;
                MonitorLog.WriteLog("Ошибка обновления: " + ex.Message, MonitorLog.typelog.Error, true);
            }

            try
            {
                File.Delete(Path.Combine(LocalDir, "load.sql"));
            }
            catch (Exception ex)
            {
                Flag = false;
                MonitorLog.WriteLog("Ошибка обновления: " + ex.Message, MonitorLog.typelog.Error, true);
            }

            try
            {
                File.Delete(Path.Combine(LocalDir, ErrorLog));
            }
            catch (Exception ex)
            {
                Flag = false;
                MonitorLog.WriteLog("Ошибка обновления: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            #endregion

            Directory.SetCurrentDirectory(SourceDir);
            return true;
        }

        //сбрасывает тразакции index = 0 Set, index = 1 Reset
        bool SetResetTransaction(int index, string s)
        {
            #region создание cmd файла
            f_cmd = new StreamWriter(Path.Combine(MainDir, "run.cmd"), false);
            MakeEnvironment();
            string baseN = ReplaceVar(s.Substring(CurrentNumber + 2, s.Length - CurrentNumber - 2));
            string buf = "ontape -s ";
            buf += index == 0 ? "-U " + baseN : "-N " + baseN;
            WriteDebug(buf);
            if (buf[buf.Length - 1] == ';') buf.Remove(buf.Length - 1);
            f_cmd.WriteLine(buf);
            buf = "exit";
            f_cmd.WriteLine(buf);
            f_cmd.Close();
            #endregion

            #region подключение к БД sysmaster
#if PG
            ExecSQL(conn, "set search_path to 'public'", true);
            ExecSQL(conn2, "set search_path to 'public'", true);
            WriteDebug("set search_path to 'public'");
            WriteDebug("set search_path to 'public'");
#else
  ExecSQL(conn, "database sysmaster", true);
            ExecSQL(conn2, "database sysmaster", true);
            WriteDebug("database sysmaster");
            WriteDebug("database sysmaster");
#endif
            #endregion

            #region выполнение командного файла
            Process cmd = Process.Start("cmd.exe", "/k \"" + Path.Combine(MainDir, "run.cmd") + "\"");
            //ожидаем завершение выполнения командного файла
            while (CheckNameProcess(cmd.Id))
            {
                System.Threading.Thread.Sleep(500);
            }
            #endregion

            try
            {
                #region Восстановление подключения к БД
#if PG
                ExecSQL(conn, "set search_path to '" + baseN + "'", true);
                ExecSQL(conn2, "set search_path to '" + baseN + "'", true);
                WriteDebug("set search_path to " + baseN);
                WriteDebug("set search_path to " + baseN);
#else
 ExecSQL(conn, "database " + baseN, true);
                ExecSQL(conn2, "database " + baseN, true);
                WriteDebug("database " + baseN);
                WriteDebug("database " + baseN);
#endif
                #endregion

                #region Проверка
                bool trans = IsTransaction();
                if (!trys && trans && (index == 1))
                {
                    isCritical = true;
                    WriteLog("Транзакции не сняты, выполнение обновления будет прервано ", false);
                }
                if (!trys && !trans && (index == 0))
                {
                    isCritical = true;
                    WriteLog("Транзакции не установлены, выполнение обновления будет прервано, установите транзакции вручную", false);
                }
                #endregion
            }
            catch
            {
                WriteLog("Переключение на БД " + baseN + " не удалось", true);
            }

            #region удаление run.cmd
            try
            {
                File.Delete(Path.Combine(MainDir, "run.cmd"));
            }
            catch (Exception ex)
            {
                Flag = false;
                //MonitorLog.WriteLog("Ошибка обновления: " + ex.Message, MonitorLog.typelog.Error, true);
                MonitorLog.WriteException("Ошибка обновления: ", ex);
            }
            #endregion

            return true;
        }

        //загрузка таблицы по частям
        bool LoadTableTXT(string s)
        {
            IoRes = true;
            string LocalDir = LoadDir.IndexOf(':') >= 0 ? LoadDir : Path.Combine(MainDir, LoadDir);
            if (!Directory.Exists(LocalDir))
            {
                WriteLog("Не найден каталог загрузки", false);
                LocalDir = MainDir;
            }
            string s1 = Path.Combine(LocalDir, s.Trim().Substring(s.IndexOf(' ') + 1, s.Length - s.IndexOf(' ') - 1));
            s1 = ReplaceVar(s1);
            string[] stl = File.ReadAllLines(s1);
            GetString(out s1, false);
            s1 = ReplaceVar(s1).Replace(";", "");
            for (int i = 0; i < stl.Length; i++)
            {
                string buf = stl[i].Substring(0, stl[i].Length - 1); // удаление последнего символа "|"
                string[] masbuf = buf.Split(new char[1] { '|' });
                buf = (masbuf[0].Trim() == "") ? "NULL" : "\'" + masbuf[0] + "\'";
                for (int j = 1; j < masbuf.Length; j++)
                {
                    buf += (masbuf[j].Trim() == "") ? ",NULL" : ",\'" + masbuf[j] + "\'";
                }

                try
                {
                    string sql = s1 + " VALUES (" + buf + ")";
                    Returns ret = Utils.InitReturns();
                    ret = ExecSQL(conn, sql, !trys && !isIfFunc);
                    if (!ret.result) return false;
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message, true);
                    break;
                }
            }
            return true;
        }

        //выгрузка таблицы
        bool UnloadTableTXT(string s)
        {
            IoRes = true;
            string LocalDir = LoadDir.IndexOf(':') >= 0 ? LoadDir : Path.Combine(MainDir, LoadDir);
            if (!Directory.Exists(LocalDir)) Directory.CreateDirectory(LocalDir);

            string s1 = Path.Combine(LocalDir, s.Trim().Substring(s.IndexOf(' ') + 1, s.Length - s.IndexOf(' ') - 1));
            s1 = ReplaceVar(s1);
            StreamWriter f_txt = new StreamWriter(s1);
            GetString(out s1, false);
            string s2 = "";
            while (s1[s1.Trim().Length - 1] != ';')
            {
                s2 += ' ' + s1;
                if (!GetString(out s1, false)) break;
            }
            if ((s1.Length > 0) && (s1[s1.Trim().Length - 1] == ';')) s2 += ' ' + s1;

            try
            {
                string sql = ReplaceVar(s2);
                IDataReader reader;
                Returns ret = Utils.InitReturns();
                ret = ExecRead(conn, out reader, sql, !trys);

                if ((!ret.result) || (reader == null))
                {
                    f_txt.Close();
                    return true;
                }

                while (reader.Read())
                {
                    s2 = "";
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (reader.GetFieldType(i) == typeof(DateTime))
                        {
                            s2 += Convert.ToDateTime(reader[i]).ToString("dd.MM.yyyy HH.mm") + "|";
                        }
                        else
                        {
                            s2 += Convert.ToString(reader[i]) + "|";
                        }
                    }
                    f_txt.WriteLine(s2);
                }
                reader.Close();
                f_txt.Close();
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, true);
                f_txt.Close();
            }

            return true;
        }

        //Грузит БД
        bool DBImportBase(string s)
        {
            #region Получаем имя БД
            BaseName = GetWord(s);
            int i = CurrentNumber;
            string buf;
            BaseName = ReplaceVar(BaseName);
            if (s[i] == '_')
            {
                CurrentNumber = i + 1;
                buf = GetWord(s);
                i = CurrentNumber;
                BaseName += '_' + ReplaceVar(buf);
            }
            CurrentNumber = i + 1;
            WriteDebug(BaseName);
            #endregion

            #region Получаем каталог, из которого который нужно загрузить БД
            LoadDir = s.Substring(CurrentNumber - 1, s.Length - CurrentNumber + 1);
            if (LoadDir[LoadDir.Length - 1] == ';') LoadDir = LoadDir.Substring(0, LoadDir.Length - 1);
            WriteDebug(LoadDir);
            string SourceDir = Directory.GetCurrentDirectory();
            #endregion

            #region если каталог не существует пытаемся создать его
            if (!Directory.Exists(LoadDir))
            {
                try
                {
                    Directory.CreateDirectory(LoadDir);
                    WriteDebug("Create " + LoadDir);
                }
                catch
                {
                    LoadDir = Path.Combine(LoadDir, "arc");
                    if (!Directory.Exists(LoadDir)) Directory.CreateDirectory(LoadDir);
                }
            }
            #endregion

            #region создаем командый файл содержащий перечь переменных среды и команду для выгрузки БД
            f_cmd = new StreamWriter(Path.Combine(LoadDir, "archive.cmd"));
            MakeEnvironment();
            buf = "dbexport " + BaseName;
            WriteDebug(buf);
            f_cmd.WriteLine(buf);
            buf = "exit";
            f_cmd.WriteLine(buf);
            f_cmd.Close();
            Directory.SetCurrentDirectory(LoadDir);
            #endregion

            #region Выполняем командный файл
            Process cmd = Process.Start("cmd.exe", "/k \"" + Path.Combine(LoadDir, "archive.cmd") + "\"");
            //ожидаем завершение выполнения командного файла
            while (CheckNameProcess(cmd.Id))
            {
                System.Threading.Thread.Sleep(500);
            }

            try
            {
                File.Delete(Path.Combine(LoadDir, "archive.cmd"));
            }
            catch { }
            #endregion

            #region Производим проверку на успешное выполнение выгрузки БД
            bool Success = false;//Признак успешного сохранения БД
            using (var fexp = File.Open(Path.Combine(LoadDir, "dbexport.out"), FileMode.Open, FileAccess.Read))
            {
                var buff = new byte[30];
                fexp.Position = fexp.Length - 30;
                fexp.Read(buff, 0, 30);
                Success = Encoding.ASCII.GetString(buff).IndexOf("dbexport completed", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            Directory.SetCurrentDirectory(SourceDir);
            #endregion

            #region если выгрузка завершилась неудачно, то вызываем аварийную ситуацию
            if (!Success)
            {
                isCritical = true;
                WriteLog("Аварийное завершение загрузки", false);
            }
            #endregion

            return true;
        }

        //Запуск функции
        PSpisFunc RunFunction(string s)
        {
            int StackNum = -1;
            PSpisFunc StackFunc = null;
            if (isFunction)
            {
                StackFunc = CurrentFunc;
                StackNum = Numstring;
            }
            PSpisFunc fun = null;
            string s1 = s.Substring(0, s.IndexOf('(')).Trim();
            if ((s.IndexOf(')') < 0) || (FuncList == null)) return null;
            for (int i = 0; i < FuncList.Count; i++)
            {
                fun = null;
                if (string.Equals(FuncList[i].NameFunc, s1, StringComparison.OrdinalIgnoreCase))
                {
                    fun = FuncList[i];
                    CurrentFunc = fun;
                    break;
                }
            }
            if (fun == null) return null;
            string s2 = ReplaceVar(s.Substring(s.IndexOf('(') + 1, s.IndexOf(')') - s.IndexOf('(') - 1));
            if (s2.Contains("$allmonths"))
            {
                //bool old_try = trys;
                //trys = true;
                for (int i = 1; i <= 12; i++)
                {
                    RunFunction(s.Replace("$allmonths", i.ToString("00")));
                }
                //trys = old_try;
                return null;
            }
            else if (s2.Contains("$allyears"))
            {
                //bool old_try = trys;
                //trys = true;

                if (beginYear > endYear)
                {
                    for (int i = beginYear; i <= 99; i++)
                    {
                        RunFunction(s.Replace("$allyears", i.ToString("00")));
                    }
                    for (int i = 0; i <= endYear; i++)
                    {
                        RunFunction(s.Replace("$allyears", i.ToString("00")));
                    }
                }
                else
                {
                    for (int i = beginYear; i <= endYear; i++)
                    {
                        RunFunction(s.Replace("$allyears", i.ToString("00")));
                    }
                }
                //trys = old_try;
                return null;
            }
            else
            {
                int j = 0;
                bool isVar = false;
                while (s2.IndexOf(',') >= 0)
                {
                    if (j > fun.ParamString.Count - 1) return null;
                    isVar = false;
                    for (int k = 0; k < ListVar.Count; k++)
                    {
                        PVarParam LastVar = (PVarParam)ListVar[k];
                        if (fun.ParamString[j] == LastVar.NameParam)
                        {
                            LastVar.ValueParam = s2.Substring(0, s2.IndexOf(','));
                            ListVar[k] = LastVar;
                            isVar = true;
                            break;
                        }
                    }
                    if (!isVar)
                    {
                        PVarParam Vars = new PVarParam();
                        Vars.NameParam = fun.ParamString[j];
                        Vars.ValueParam = s2.Substring(0, s2.IndexOf(','));
                        ListVar.Add(Vars);
                    }
                    s2 = s2.Substring(s2.IndexOf(',') + 1, s2.Length - s2.IndexOf(',') - 1);
                    j++;
                }

                isVar = false;
                for (int k = 0; k < ListVar.Count; k++)
                {
                    PVarParam LastVar = (PVarParam)ListVar[k];
                    if (fun.ParamString[j] == LastVar.NameParam)
                    {
                        LastVar.ValueParam = s2;
                        ListVar[k] = LastVar;
                        isVar = true;
                        break;
                    }
                }

                if (!isVar)
                {
                    PVarParam Vars = new PVarParam();
                    Vars.NameParam = fun.ParamString[j];
                    Vars.ValueParam = s2;
                    ListVar.Add(Vars);
                }
                Numstring = 0;
                isFunction = true;
                bool ResFunc = true;
                while (Numstring < fun.Spis.Count)
                {
                    IoRes = true;
                    s = fun.Spis[Numstring].Trim();
                    if (s != "")
                    {
                        if ((s[0] == '/') && (s[2] != '/'))
                        {
                            SetCriticalSection(s);
                        }
                        else
                        {
                            ChoiseBranch(s);
                            if (ResFunc) ResFunc = IoRes;
                        }
                    }
                    Numstring++;
                }

                if (StackNum == -1)
                {
                    isFunction = false;
                }
                else
                {
                    CurrentFunc = StackFunc;
                    Numstring = StackNum;
                }
                fun.Result = ResFunc;
                return fun;
            }
        }

        //Запуск условия
        bool RunIF(string s)
        {
            string regif = "if\\s+(?<ifFunc>.+?)\\s+then\\s+(?<thenFunc>.+?)(\\s+else\\s+(?<elseFunc>.+?)\\s*)?;";
            string s1 = s;
            bool Res1 = true;
            string buf = s1;
            while ((s1[s1.Length - 1] != ';') && (Res1))
            {
                Res1 = GetString(out s1, false);
                buf += ' ' + s1;
            }

            buf = buf.Trim();
            if (buf.IndexOf("then", StringComparison.OrdinalIgnoreCase) < 0) return true;
            if (buf.IndexOf(";", StringComparison.OrdinalIgnoreCase) < 0) return true;

            #region разбиваем строку на if, then и else
            Regex regex = new Regex(regif);
            Match match = regex.Match(buf);
            if (!match.Success) return true;
            string ifFunc = match.Groups["ifFunc"].Value;
            string thenFunc = match.Groups["thenFunc"].Value;
            string elseFunc = match.Groups["elseFunc"].Value;
            if ((ifFunc == "") || (thenFunc == "") || (ifFunc.IndexOf("then", StringComparison.OrdinalIgnoreCase) >= 0)
                || (thenFunc.IndexOf("else", StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            #endregion

            #region выполняем функцию
            isIfFunc = true;
            PSpisFunc fun = RunFunction(ifFunc);
            isIfFunc = false;
            WriteDebug(ifFunc);
            if (fun == null) return true;
            if (fun.Result)
            {
                RunFunction(thenFunc);
            }
            else if (elseFunc != "") RunFunction(elseFunc);
            #endregion

            return true;
        }

        //Запуск цикла
        bool RunFOR(string s)
        {
            #region считывание оператора for
            string regfor = "for\\s+(?<value>.+?)\\s*:=\\s*(?<beginValue>.+?)\\s+to\\s+(?<endValue>.+?)\\s+do\\s+(?<forFunc>.+?)\\s*;";
            string buf = "";

            bool Res1 = true;
            string s1 = s;
            while ((s1[s1.Length - 1] != ';') && (Res1))
            {
                buf += ' ' + s1;
                Res1 = GetString(out s1, false);
            }

            buf += s1;
            if (buf[buf.Length - 1] != ';') buf += ';';
            #endregion

            #region разбиваем строку на for to do func
            Regex regex = new Regex(regfor);
            Match match = regex.Match(buf);
            if (!match.Success) return true;
            string value = match.Groups["value"].Value;
            string forFunc = match.Groups["forFunc"].Value;
            int beginValue, endValue;
            if ((!int.TryParse(match.Groups["beginValue"].Value, out beginValue)) || (!int.TryParse(match.Groups["endValue"].Value, out endValue))
                || (value == "") || (forFunc == ""))
            {
                WriteLog("ошибка преобразования переменной", false);
                return true;
            }
            #endregion

            #region подготовка функции
            PVarParam Vars = null;
            foreach (PVarParam varparam in ListVar)
            {
                Vars = varparam;
                if (string.Equals(Vars.NameParam, value, StringComparison.OrdinalIgnoreCase))
                {
                    Vars.ValueParam = beginValue.ToString();
                    break;
                }
                Vars = null;
            }

            if (Vars == null)
            {
                Vars = new PVarParam();
                Vars.NameParam = value;
                Vars.ValueParam = beginValue.ToString();
                ListVar.Add(Vars);
            }

            WriteDebug(forFunc);
            WriteDebug(beginValue.ToString());
            WriteDebug(endValue.ToString());
            #endregion

            #region выполнение функции
            int j = ListVar.IndexOf(Vars);
            if (j < 0) return true;
            for (int i = beginValue; i <= endValue; i++)
            {
                Vars = (PVarParam)ListVar[j];
                Vars.ValueParam = i.ToString();
                ListVar[j] = Vars;
                RunFunction(ReplaceVar(forFunc));
            }
            #endregion

            return true;
        }

        //Выбор ветви
        bool ChoiseBranch(string s)
        {
            bool runString = false;//Признак "выполнения" строки
            CurrentNumber = 0;
            if ((s.Substring(0, 2) == "--") || (s.Substring(0, 2) == "//")) return true;
            string s2 = GetWord(s); //Промежуточная строка содержащая первое слово строки

            if (s2.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                s2 = GetWord(s);
                if (s2.Equals("procedure", StringComparison.OrdinalIgnoreCase))
                {
                    MakeProcedure(s);
                    runString = true;
                }
                else if (s2.Equals("function", StringComparison.OrdinalIgnoreCase))
                {
                    MakeFunction(s);
                    runString = true;
                }
            }
            else if (s2.Equals("settransaction", StringComparison.OrdinalIgnoreCase))
            {
                SetResetTransaction(0, s);
                runString = true;
            }
            else if (s2.Equals("resettransaction", StringComparison.OrdinalIgnoreCase))
            {
                SetResetTransaction(1, s);
                runString = true;
            }
            else if (s2.Equals("load", StringComparison.OrdinalIgnoreCase))
            {
                LoadTable(s);
                runString = true;
            }
            else if (s2.Equals("setvar", StringComparison.OrdinalIgnoreCase))
            {
                SetSqVar(s);
                runString = true;
            }
            else if (s2.Equals("loadtxt", StringComparison.OrdinalIgnoreCase))
            {
                LoadTableTXT(s);
                runString = true;
            }
            else if (s2.Equals("instmp", StringComparison.OrdinalIgnoreCase))
            {
                insTmp(s);
                runString = true;
            }
            else if (s2.Equals("loaddir", StringComparison.OrdinalIgnoreCase))
            {
                LoadDir = s.Substring(CurrentNumber, s.Length - CurrentNumber).Trim(); ;
                if (LoadDir[LoadDir.Length - 1] == ';') LoadDir = LoadDir.Substring(0, LoadDir.Length - 1);
                runString = true;
            }
            else if (s2.Equals("try", StringComparison.OrdinalIgnoreCase))
            {
                trys = true;
                runString = true;
            }
            else if (s2.Equals("except", StringComparison.OrdinalIgnoreCase))
            {
                trys = false;
                runString = true;
            }
            else if (s2.Equals("unload", StringComparison.OrdinalIgnoreCase))
            {
                UnloadTable(s);
                runString = true;
            }
            else if (s2.Equals("unloadtxt", StringComparison.OrdinalIgnoreCase))
            {
                UnloadTableTXT(s);
                runString = true;
            }
            // пока нет
            //else if (s2.Equals("unloadxls", StringComparison.OrdinalIgnoreCase))
            //{
            //    UnloadTableXLS(s);
            //    runString = true;
            //}
            if (s2.Equals("dbexport", StringComparison.OrdinalIgnoreCase))
            {
                MakeArchive(s);
                runString = true;
            }
            if (s2.Equals("dbimport", StringComparison.OrdinalIgnoreCase))
            {
                DBImportBase(s);
                runString = true;
            }
            if (s2.Equals("sqlresult", StringComparison.OrdinalIgnoreCase))
            {
                isExit = !IoRes;
                isCritical = false;
                runString = true;
            }
            if (s2.Equals("if", StringComparison.OrdinalIgnoreCase))
            {
                RunIF(s);
                runString = true;
            }
            if (s2.Equals("maindb", StringComparison.OrdinalIgnoreCase))
            {
                MainBD = ReplaceVar(s.Trim().Substring(7, s.Trim().Length - 7));
                if ((MainBD.Trim() != "") && (MainBD[MainBD.Length - 1] == ';')) MainBD = MainBD.Substring(0, MainBD.Length - 1);
                runString = true;
            }
            if (s2.Equals("for", StringComparison.OrdinalIgnoreCase))
            {
                RunFOR(s);
                runString = true;
            }
            if (s2.Equals("database", StringComparison.OrdinalIgnoreCase))
            {
                BaseName = ReplaceVar(s.Substring(8, s.Length - 8).Trim());
                WriteDebug(BaseName);
            }
            if (s2.Equals("setdebug", StringComparison.OrdinalIgnoreCase))
            {
                Debug = true;
                WriteDebug(BaseName);
                runString = true;
            }
            if (s2.Equals("resetdebug", StringComparison.OrdinalIgnoreCase))
            {
                Debug = false;
                runString = true;
            }
            if (s2.Equals("sqfunction", StringComparison.OrdinalIgnoreCase))
            {
                ReadFunction(s, false);
                runString = true;
            }
            if (FindFunc(s) != null)
            {
                RunFunction(s);
                runString = true;
            }
            if (s2.Equals("far", StringComparison.OrdinalIgnoreCase))
            {
                s2 = s.Trim();
                ReadFunction(s.Substring(3, s.Length - 3).Trim(), true);
                runString = true;
            }
            if (s2.Equals("connect", StringComparison.OrdinalIgnoreCase))
            {
                s2 = GetWord(s);
#if !PG
                if (conn.State != ConnectionState.Closed) conn.Close();
                if (conn2.State != ConnectionState.Closed) conn2.Close();
                //conn.Dispose();
                //conn2.Dispose();
                conn = GetConnection(s2.Equals("webdata", StringComparison.OrdinalIgnoreCase) ? Constants.cons_Webdata : Constants.cons_Kernel);
                conn2 = GetConnection(s2.Equals("webdata", StringComparison.OrdinalIgnoreCase) ? Constants.cons_Webdata : Constants.cons_Kernel);
                if (conn.State == ConnectionState.Closed) conn.Open();
                if (conn2.State == ConnectionState.Closed) conn2.Open();
#endif
                runString = true;
            }

            if (!runString && !RunQuery(s) && !isIfFunc)
            {
                Flag = false;
                MonitorLog.WriteLog("Ошибка обновления.", MonitorLog.typelog.Error, true);
            }

            return true;
        }

        // запуск обновления БД
        void Init()
        {
            string s = "";//Промежуточная строка для хранения строки из файла
            isCancel = false;
            if (FileList == null) SearchFiles();
            SetParam();
            SetOper();
            MainBD = "";
            trys = false;
            Array.Sort(FileList, (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));

            for (int i = 0; i < FileList.Length; i++)
            {
                isCritical = false;
                f = new StreamReader(Path.Combine(MainDir, FileList[i].Name));
                ErrorLog = Path.GetFileNameWithoutExtension(FileList[i].Name) + ".log";
                try
                {
                    File.Delete(Path.Combine(Path.Combine(MainDir, ".log"), ErrorLog));
                }
                catch { }

                while ((!f.EndOfStream) && (!isExit))
                {
                    GetString(out s, false);
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (s.Length < 2) continue;
                        if ((s[0] == '/') && (s[1] != '/'))
                        {
                            SetCriticalSection(s);
                            continue;
                        }
                        ChoiseBranch(s);
                        if (isCancel) break;
                    }
                }
                isExit = false;
                f.Close();
                DeleteOldVarPer();
            }

            DestroyVarOper();
            //ViewLog();
            MoveToArchive();
            SaveUnl();

            s = conn.Database;
#if PG
            sql = new StringBuilder("set search_path to '" + conn.Database + "'");
#else
            sql = new StringBuilder("database " + conn.Database);
#endif
            ExecSQL(conn, sql.ToString(), true);
        }

        //Проверка на присутствие новых патчей и запуск процедуры обновления,если они есть
        public bool isNewPatches()
        {
            Flag = true;
            isExit = false;
            unloaddir = "";
            LoadDir = "";

            BaseName = conn.Database;
            if (!GetLastPatchError()) return false;
            //MainDir = Path.Combine(Directory.GetCurrentDirectory(), @"patches\");

            //Если каталог обновлений не создан, то создаем его
            if (!Directory.Exists(MainDir)) Directory.CreateDirectory(MainDir);

            string[] files = Directory.GetFiles(MainDir, "*.sqc");

            #region запуск скриптов
            if (files.Length > 0)
            {
                try
                {
                    sql = new StringBuilder();
#if PG
                    sql.Append(" create table patches (nzp_patch serial,  " +
                                               " version char(100),                       " +
                                               " priznak integer,                         " +
                                               " comment char(100))                       ");
#else
  sql.Append(" create table patches (nzp_patch serial,  " +
                             " version char(100),                       " +
                             " priznak integer,                         " +
                             " comment char(100))                       ");
#endif
                    ExecSQL(conn, sql.ToString(), true);
                }
                catch { }

                if (true)
                {
                    WriteStart();
                    Init();
                    if (Flag)
                    {
                        MessageBox.Show("Обновление базы успешно завершено.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    else
                    {
                        if (!isCancel)
                        {
                            MessageBox.Show("Обновление прошло с ошибками!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show("Обновление аварийно завершено!" + Environment.NewLine + "Обратитесь к разработчикам.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    WriteBase();
                }
            }
            #endregion

            return false;
        }

        //Проверка на присутствие новых патчей и запуск процедуры обновления (файлы в параметре). Не isNewPatches_2()!
        public bool isNewPatches(FileInfo[] files)
        {
            FileList = files;
            if (toFile)
            {
                if (!Directory.Exists(Path.Combine(Path.GetTempPath(), "KOMPLAT")))
                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "KOMPLAT"));
                string scrDir = Path.Combine(Path.GetTempPath(), "KOMPLAT\\" + DateTime.Now.ToString("yyyy-MM-dd"));
                if (Directory.Exists(scrDir))
                    Directory.Delete(scrDir, true);
                f_scr = new StreamWriter(scrDir + conn.Database);
            }
            Flag = true;
            isExit = false;
            isExit = false;
            unloaddir = "";
            LoadDir = "";

            BaseName = conn.Database;
            if (!GetLastPatchError()) return false;
            //MainDir = Path.Combine(Directory.GetCurrentDirectory(), @"\OUT\LINE\");

            //Если каталог обновлений не создан, то создаем его
            if (!Directory.Exists(MainDir)) Directory.CreateDirectory(MainDir);

            //files = Directory.GetFiles(MainDir, "*.sq");

            #region запуск скриптов
            if (files.Length > 0)
            {
                try
                {
#if PG
                    string sql = " create table patches (nzp_patch serial,  " +
                                                 " version char(100),                       " +
                                                 " priznak integer,                         " +
                                                 " comment char(100))                       ";
#else
string sql = " create table patches (nzp_patch serial,  " +
                             " version char(100),                       " +
                             " priznak integer,                         " +
                             " comment char(100))                       ";
#endif
                    ExecSQL(conn, sql, false);
                }
                catch { }

                WriteStart();
                Init();
                if (Flag)
                {
                    //MessageBox.Show("Обновление базы успешно завершено.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (!isCancel)
                    {
                        MessageBox.Show("Обновление прошло с ошибками!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    else
                    {
                        MessageBox.Show("Обновление аварийно завершено!" + Environment.NewLine + "Обратитесь к разработчикам.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                WriteBase();
            }
            #endregion
            if (toFile)
            {
                f_scr.Close();
            }
            return true;
        }
    }

    /// <summary>
    /// Класс для шифрования/дешифрования строк
    /// </summary>
    public static class StringCrypter
    {
        public const string pass = "U9A=4{VSW4k~5G_Lt_]EDev)U\"k(GNm}e9qL4).AwIoewE:9],";

        private const string initVector = "jd854jf8ewj568fj";

        private const int keysize = 256;

        public static string Encrypt(string plainText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        }
    }
}

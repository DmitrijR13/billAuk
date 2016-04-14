using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;

namespace Bars.KP50.DB.DbSqAdmin
{
    public class DbSqAdmin : DataBaseHeadServer
    {
        private string[] _stringsFromFile;
        private List<FuncStruct> _funcList;
        private StringBuilder _errors;
        private bool _writeLog;
        private string _path;

        public DbSqAdmin()
        {
            _funcList = new List<FuncStruct>();
            _errors = new StringBuilder();
            _writeLog = true;
            _path = @"PatchesBars\";
        }

        public delegate void FileLoadEventHandler(int CurrentProgress, int TotalProgress);

        public event FileLoadEventHandler oneFileLoad;

        public delegate void ExecResultEventHandler(string message);

        public event ExecResultEventHandler SendExecResult;

        public void Run(object container)
        {
            FileInfo[] files = container as FileInfo[];

            IDbConnection conn = ServerConnection;
            if (conn.State != ConnectionState.Open)
            {
                SendExecResult("Нет соединения к базе! Проверьте настройки.");
                return;
            }

            bool result = true;

            try
            {
                int CurrentProgress = 0;
                foreach (var oneFile in files)
                {
                    using (StreamReader sr = new StreamReader(oneFile.FullName, Encoding.GetEncoding(1251)))
                    {
                        string stringFromFile;
                        List<string> lstFileData = new List<string>();
                        //считываем sq-файл в массив строк
                        while ((stringFromFile = sr.ReadLine()) != null)
                            lstFileData.Add(stringFromFile);
                        _stringsFromFile = lstFileData.Where(row => !string.IsNullOrEmpty(row)).ToArray();
                        //обрабатываем sq-файл
                        result = ChoiceBranch(_stringsFromFile);
                        if (oneFileLoad != null) oneFileLoad(++CurrentProgress, files.Count());
                    }
                    WriteLogFile(oneFile.FullName);
                    _errors.Remove(0, _errors.Length);
                }

                MoveToArchive("*.sq", "tmp");

                MoveToArchive("*.log", "log");
            }
            catch (Exception ex)
            {
                result = false;
                _errors.Append(Environment.NewLine + "[!!!] Ошибка выполнения! Текст ошибки: " + ex.Message);
                WriteLogFile(_path + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_GeneralError.log");
            }
            if (!result)
            {
                SendExecResult("Обновление прошло с ошибками! Смотрите лог ошибок");
                return;
            }

            SendExecResult("Обновление выполнено успешно!");
            return;
        }

        /// <summary>
        /// Структура для хранения ф-ций
        /// </summary>
        private struct FuncStruct
        {
            public string name;
            public string[] prms;
            public StringBuilder body;

        }

        /// <summary>
        /// Обработка массива строк из sq-файла
        /// </summary>
        /// <returns></returns>
        /// 
        private bool ChoiceBranch(string[] stringsArr)
        {
            string tmpSqlQuery = "";
            bool result = true;

            for (int currRowNumber = 0; currRowNumber < stringsArr.Count(); currRowNumber++)
            {
                #region обработка комментариев

                if (stringsArr[currRowNumber][0] == '-' && stringsArr[currRowNumber][1] == '-')
                {
                    continue;
                }

                if (stringsArr[currRowNumber][0] == '/' && stringsArr[currRowNumber][1] == '/')
                {
                    continue;
                }

                #endregion обработка комментариев


                #region обработка try-except

                if (stringsArr[currRowNumber].ToUpper().Contains("TRY"))
                {
                    _writeLog = false;
                    continue;
                }

                if (stringsArr[currRowNumber].ToUpper().Contains("EXCEPT"))
                {
                    _writeLog = true;
                    continue;
                }

                #endregion обработка try-except

                #region обработка описания ф-ций

                if (stringsArr[currRowNumber].ToUpper().Contains("FUNCTION"))
                {
                    currRowNumber = ReadFunction(currRowNumber);
                    continue;
                }

                #endregion обработка описания ф-ций

                #region обработка вызова ф-ций

                if (FindFunction(stringsArr[currRowNumber]))
                {
                    //текущая строка - вызов ф-ции
                    //выполняем ф-цию
                    result = RunFunction(stringsArr[currRowNumber]);
                    continue;
                }

                #endregion обработка вызова ф-ций


                tmpSqlQuery += stringsArr[currRowNumber] + " ";
                if (tmpSqlQuery.Contains(";"))
                {
                    result = ExecTmpSQL(tmpSqlQuery);
                    tmpSqlQuery = "";
                    continue;
                }
            }
            return result;
        }

        /// <summary>
        /// Возвращает номер строки, с которого надо продолжить разбор
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private int ReadFunction(int startIndex)
        {

            if (_funcList == null)
            {
                _funcList = new List<FuncStruct>();
            }

            string[] oneFuncHead = _stringsFromFile[startIndex].Split(new[] {"(", ")", " ", ",", ";"},
                StringSplitOptions.RemoveEmptyEntries);

            int k = 2;
            int prmsCount = oneFuncHead.Count() - 2;
            string[] oneFuncPrms = new string[prmsCount];
            for (int j = 0; j < prmsCount; j++)
            {
                oneFuncPrms[j] = oneFuncHead[k++];
            }

            int i = startIndex + 1;
            StringBuilder oneFuncBody = new StringBuilder();

            while (true)
            {
                if (_stringsFromFile[i].ToUpper().Contains("END FUNCTION"))
                    break;
                oneFuncBody.Append(_stringsFromFile[i] + " ");
                i++;
            }

            //добавляем в список функций
            _funcList.Add(new FuncStruct()
            {
                name = oneFuncHead[1],
                prms = oneFuncPrms,
                body = oneFuncBody
            });

            return i++;
        }

        /// <summary>
        /// Проверяет - не является ли строка вызовом функции
        /// </summary>
        /// <returns>Возвращает истину, если данная строка - вызов ф-ции</returns>
        private bool FindFunction(string oneString)
        {
            if (_funcList == null)
            {
                //список функций пуст
                return false;
            }

            for (int i = 0; i < _funcList.Count; i++)
            {
                if (oneString.ToUpper().Replace(" ", "").Contains(_funcList[i].name.ToUpper() + "("))
                {
                    //строка является ф-цией
                    return true;
                }
            }
            return false;
        }

        private bool RunFunction(string oneString)
        {
            bool result = true;
            string[] callingStr = oneString.Split(new[] { "(", ")", " ", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);


            for (int i = 0; i < _funcList.Count; i++)
            {
                if (callingStr[0] == _funcList[i].name)
                {
                    if (callingStr.Count() - 1 != _funcList[i].prms.Count())
                    {
                        //не совпадает кол-во параметров
                        return false;
                    }


                    //TODO: добавить обработку входных параметров

                    string tmpBuffer = "";

                    tmpBuffer = _funcList[i].body.ToString();
                    tmpBuffer =
                        tmpBuffer.Replace("TRY", "TRY;")
                            .Replace("try", "try;")
                            .Replace("EXCEPT", "EXCEPT;")
                            .Replace("except", "except;");

                    for (int j = 0; j < _funcList[i].prms.Count(); j++)
                    {
                        tmpBuffer = tmpBuffer.Replace("#" + _funcList[i].prms[j], callingStr[j + 1]);
                    }


                    string[] queryArray = tmpBuffer.Split(new[] {";", "\n", Environment.NewLine},
                        StringSplitOptions.RemoveEmptyEntries);

                    foreach (var oneSql in queryArray)
                    {
                        #region обработка комментариев и пустых строк

                        if (oneSql[0] == '-' && oneSql[1] == '-')
                        {
                            //пропускаем комментарии
                            continue;
                        }

                        if (oneSql[0] == '/' && oneSql[1] == '/')
                        {
                            //пропускаем комментарии
                            continue;
                        }

                        if (oneSql.Trim() == "")
                        {
                            //пропускаем пустые строки
                            continue;
                        }

                        #endregion обработка комментариев и пустых строк


                        if (oneSql.Trim().ToUpper() == "TRY")
                        {
                            _writeLog = false;
                            continue;
                        }

                        if (oneSql.Trim().ToUpper() == "EXCEPT")
                        {
                            _writeLog = true;
                            continue;
                        }

                        if (!ExecTmpSQL(oneSql))
                        {
                            result = false;
                        }
                    }
                }
            }
            return result;
        }

        private bool ExecTmpSQL(string sql)
        {
            Returns ret = ExecSQL(sql, false);
            if (!ret.result && _writeLog)
            {
                _errors.Append(ret.text + Environment.NewLine);
                return false;
            }
            return true;
        }

        private void MoveToArchive(string filesExtension, string folderName)
        {
            FileInfo[] files = new DirectoryInfo(_path).GetFiles(filesExtension);
            string outputArchiveFullName = _path + "\\" + folderName + "\\" +
                                           DateTime.Now.ToString("yyyy-MM_dd_HH_mm_ss") + "_" +
                                           filesExtension.Replace("*.", "") +
                                           ".zip";

            string[] filesFullNames = new string[files.Count()];

            int i = -1;
            foreach (var oneFile in files)
            {
                filesFullNames[++i] = oneFile.FullName;
            }


            CheckDirectory(outputArchiveFullName);

            if (!Archive.GetInstance().Compress(outputArchiveFullName, filesFullNames, true))
            {
                _errors.Append("Ошибка при архивации '" + filesExtension + "' файлов!");
            }
        }


        private void WriteLogFile(string directory)
        {
            string fullLogPath = _path + "\\" + System.IO.Path.GetFileNameWithoutExtension(directory) + ".log";
            if (_errors.Length != 0)
            {
                CheckDirectory(fullLogPath);

                //записываем в файл
                StreamWriter sw = new StreamWriter(fullLogPath, false, System.Text.Encoding.GetEncoding(1251));
                sw.Flush();
                sw.Write(_errors);
                sw.Close();
                sw.Dispose();
            }
        }

        /// <summary>
        /// проверяем директорию, если не существует - создаем
        /// </summary>
        /// <param name="path"></param>
        private void CheckDirectory(string path)
        {
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            }
        }


    }
}
using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.IO;
using System.Text;
using STCLINE.KP50;
using System.Collections.Generic;
using Bars.KP50.DataImport.SOURCE.EXCHANGE;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public enum AutoLinkMode
    {
        WithoutAdd = 1,
        Add = 2
    }

    /// <summary>
    /// Класс автоматического сопоставления данных
    /// </summary>
    abstract public class AbstractAutomaticLink : DataBaseHeadServer
    {
        protected bool check_data_bank = false;

        protected string tempTableName { get; set; }
        protected string protocolFileName { get; set; }
        protected string protocolNote { get; set; }
        protected string[] fieldsTempTable { get; set; }
        protected string[] headersTempTable { get; set; }

        protected string absentValuesMessage { get; set; }
        protected string fewValuesMessage { get; set; }

        private const int messageMaxLength = 1000;
        protected const int WarningCode = -999;

        protected abstract void GetProtocolInfo();
        protected abstract string CreateTempTable(IDbConnection conn_db);
        protected abstract void GetBufferData(IDbConnection conn_db, int nzp_file);
        protected abstract void DefineBufferLink(IDbConnection conn_db, FilesImported finder);

        private bool CanGetProtocol(IDbConnection conn_db)
        {
            Returns ret = new Returns(true);
            int cnt = Convert.ToInt32(ExecScalar(conn_db, "select count(*) from " + tempTableName + " where cnt <> 1", out ret, true));

            if (cnt == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Проверка данных
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns CheckData(FilesImported finder)
        {
            if (finder.nzp_user <= 0)
            {
                return new Returns(false, " Не задан пользователь ", -1);
            }

            if (check_data_bank)
            {
                if (finder.bank.Trim() == "")
                {
                    return new Returns(false, " Не задан банк данных ", -1);
                }
            }

            return new Returns(true);
        }

        /// <summary>
        /// Автоматическое сопоставление
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns AutoLinkData(FilesImported finder)
        {
            Returns ret = new Returns(true, "Выполнено", 0);

            ret = CheckData(finder);
            if (!ret.result) return ret;

            FilesImported oneFile = new FilesImported() { bank = finder.bank, nzp_user = finder.nzp_user };

            // счетчик протоколов
            int protocolCount = 0;

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    GetProtocolInfo();

                    // отсортировать коды файлов
                    finder.selectedFiles.Sort(); ;

                    for (int i = 0; i < finder.selectedFiles.Count; i++)
                    {
                        oneFile.nzp_file = finder.selectedFiles[i];

                        // ... создать временную таблицу
                        tempTableName = CreateTempTable(conn_db);

                        // ... добавить колонку под сообщения
                        string sql = "alter table " + tempTableName + " add message varchar (" + messageMaxLength + ") default '' ";
                        ExecSQLWE(conn_db, sql);

                        // ... получить данные из буфера
                        GetBufferData(conn_db, oneFile.nzp_file);

                        // ... определить ссылки для буфера
                        DefineBufferLink(conn_db, oneFile);

                        if (CanGetProtocol(conn_db))
                        {
                            StringBuilder sbProtocol = new StringBuilder();
                            GetProtocol(conn_db, out sbProtocol);

                            ret = SaveProtocol(conn_db, oneFile, sbProtocol);
                            if (!ret.result) throw new Exception(ret.text);

                            protocolCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AutoLinkData : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new Returns(false, "Ошибка выполнения", -1);
                }
                finally
                {
                    ExecSQL(conn_db, "drop table " + tempTableName, false);
                }
            }

            // были сформированы протоколы
            if (protocolCount > 0)
            {
                ret = new Returns(true, "Выполнено с предупреждениями. См. протокол в 'Мои файлы'", -1000);
            }

            return ret;
        }

        /// <summary>
        /// Подготовить сообщение для сохранения во временную таблицу
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected string PrepareMessage(string message)
        {
            if (message.Length > messageMaxLength)
            {
                message = message.Substring(0, messageMaxLength);
            }

            return message;
        }

        /// <summary>
        /// Cохранить протокол
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="sbProtocol"></param>
        /// <param name="fileName"></param>
        /// <param name="fileNote"></param>
        /// <returns></returns>
        private Returns SaveProtocol(IDbConnection conn_db, FilesImported finder, StringBuilder sbProtocol)
        {
            Returns ret = new Returns(true);
            string fullPath = InputOutput.GetInputDir() + "u_" + finder.nzp_user + "_" + protocolFileName + "_" + finder.nzp_file + "_" + DateTime.Now.Ticks + ".txt";

            using (DbFileLoader fl = new DbFileLoader(conn_db))
            {
                try
                {
                    string fn4 = "";
                    finder.nzp_exc = DbFileLoader.AddMyFile(protocolNote + " по файлу " + finder.nzp_file, finder);

                    var files = new Dictionary<StringBuilder, string>();
                    files.Add(sbProtocol, fullPath);
                    ret = DbDataUnload.Compress(files);
                    if (!ret.result) throw new Exception(ret.text);

                    fullPath = ret.text;
                    if (InputOutput.useFtp)
                    {
                        fn4 = InputOutput.SaveOutputFile(fullPath);
                    }

                    fl.SetMyFileState(new ExcelUtility()
                    {
                        nzp_exc = finder.nzp_exc,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = InputOutput.useFtp ? fn4 : fullPath
                    });
                }
                catch (Exception ex)
                {
                    fl.SetMyFileState(new ExcelUtility()
                    {
                        nzp_exc = finder.nzp_exc,
                        status = ExcelUtility.Statuses.Failed
                    });

                    ret = new Returns(false, "Ошибка формирования протокола", -1);
                    MonitorLog.WriteLog("Ошибка формирования протокола " + fullPath + ": " + ex.Message, MonitorLog.typelog.Error, true);
                }

                return ret;
            }
        }

        /// <summary>
        /// Формирование протокола
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="sbProtocol"></param>
        /// <param name="fileName"></param>
        /// <param name="fileNote"></param>
        private void GetProtocol(IDbConnection conn_db, out StringBuilder sbProtocol)
        {
            sbProtocol = new StringBuilder();

            string fields = "";
            for (int i = 0; i < fieldsTempTable.Length; i++)
            {
                if (fields != "") fields += ", ";
                fields += fieldsTempTable[i];
            }

            // ... совпало
            sbProtocol.Append("[---------------------------------------------------------------------]" + Environment.NewLine);
            sbProtocol.Append("Установлены ссылки для записей:" + Environment.NewLine);

            IntfResultTableType rt = ClassDBUtils.OpenSQL("select " + fields + " from " + tempTableName + " where cnt = 1  order by " + fields, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            for (int i = 0; i < rt.resultData.Rows.Count; i++)
            {
                sbProtocol.Append(GetProtocolString(rt.resultData.Rows[i], fieldsTempTable, headersTempTable) + Environment.NewLine);
            }
            sbProtocol.Append(Environment.NewLine);

            // ... нет совпадений
            sbProtocol.Append("[---------------------------------------------------------------------]" + Environment.NewLine);
            sbProtocol.Append(absentValuesMessage + ":" + Environment.NewLine);


            rt = ClassDBUtils.OpenSQL("select " + fields + " from " + tempTableName + " where cnt = 0  order by " + fields, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            for (int i = 0; i < rt.resultData.Rows.Count; i++)
            {
                sbProtocol.Append(GetProtocolString(rt.resultData.Rows[i], fieldsTempTable, headersTempTable) + Environment.NewLine);
            }
            sbProtocol.Append(Environment.NewLine);

            // ... несколько совпадений
            sbProtocol.Append("[---------------------------------------------------------------------]" + Environment.NewLine);
            sbProtocol.Append(fewValuesMessage + ":" + Environment.NewLine);

            rt = ClassDBUtils.OpenSQL("select " + fields + " from " + tempTableName + " where cnt > 1  order by " + fields, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            for (int i = 0; i < rt.resultData.Rows.Count; i++)
            {
                sbProtocol.Append(GetProtocolString(rt.resultData.Rows[i], fieldsTempTable, headersTempTable) + Environment.NewLine);
            }
            sbProtocol.Append(Environment.NewLine);

            // ... предупреждения при добавлении
            sbProtocol.Append("[---------------------------------------------------------------------]" + Environment.NewLine);
            sbProtocol.Append("Сообщения при добавлении:" + Environment.NewLine);

            rt = ClassDBUtils.OpenSQL("select " + fields + ", message from " + tempTableName + " where cnt = " + WarningCode + "  order by " + fields, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            string[] addFieldsTempTable = new string[fieldsTempTable.Length + 1];
            Array.Copy(fieldsTempTable, addFieldsTempTable, fieldsTempTable.Length);
            addFieldsTempTable[addFieldsTempTable.Length - 1] = "message";

            string[] addHeadersTempTable = new string[headersTempTable.Length + 1];
            Array.Copy(headersTempTable, addHeadersTempTable, headersTempTable.Length);
            addHeadersTempTable[addHeadersTempTable.Length - 1] = "сообщение";

            for (int i = 0; i < rt.resultData.Rows.Count; i++)
            {
                sbProtocol.Append(GetProtocolString(rt.resultData.Rows[i], addFieldsTempTable, addHeadersTempTable) + Environment.NewLine);
            }

            sbProtocol.Append("[---------------------------------------------------------------------]");
        }

        /// <summary>
        /// Сформировать строчку протокола
        /// </summary>
        /// <param name="row"></param>
        /// <param name="fieldNames"></param>
        /// <param name="fieldHeaders"></param>
        /// <returns></returns>
        protected string GetProtocolString(DataRow row, string[] fieldNames, string[] fieldHeaders)
        {
            string message = "";

            for (int i = 0; i < fieldNames.Length; i++)
            {
                if (message != "") message += ", ";
                message += fieldHeaders[i] + ": " + row[fieldNames[i]].ToString();
            }

            return message;
        }
    }
}

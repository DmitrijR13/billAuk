using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Класс для данных генерации платежного кода
    /// </summary>
    public class PaymentCodeRequest
    {
        public int parentID;

        public string parentName;

        public int keyID;

        public int activeCode;

        public int pkod10;

        public string pkod;

        public PaymentCodeRequest()
        {
            ClearField();
        }

        public void ClearField()
        {
            parentID = 0;
            parentName = "";
            keyID = 0;

            activeCode = 0;
            pkod10 = 0;
            pkod = "";
        }
    }

    /// <summary>
    /// Класс генерации платежных кодов
    /// </summary>
    public abstract class AbstractPaymentCode : DataBaseHeadServer
    {
        /// <summary>
        /// Сгенерировать платежные коды по списку ЛС
        /// </summary>
        /// <param name="finder">используется dopFind[0], в котором передается код таблицы с выбранным ЛС</param>
        /// <returns></returns>
        public Returns GenerateLsListPaymentCode(Finder finder)
        {
            string preptable = "";

            try
            {
                Returns ret = new Returns(true);

                using (DBMyFiles dbRep2 = new DBMyFiles())
                {
                    //добавим информацию о протоколе генерации в мои файлы
                    ret = dbRep2.AddFile(new ExcelUtility()
                    {
                        nzp_user = finder.nzp_user,
                        status = ExcelUtility.Statuses.InProcess,
                        rep_name = "Протокол генерации платежных кодов",
                        is_shared = 1
                    });

                    int nzpExc = ret.tag;

                    dbRep2.SetFileProgress(nzpExc, (decimal)0.1);

                    // подготовить временную таблицу _preptable с данными для генерации платежных кодов
                    PrepareTableDataForGenerate(finder, out preptable);

                    dbRep2.SetFileProgress(nzpExc, (decimal)0.3);

                    // сообщения об ошибках
                    List<string> listErrMsg = new List<string>();
                    List<string> listErrPkod = new List<string>();

                    // генерация плат. кодов производится для каждой строки таблицы _preptable
                    IntfResultTableType dt = OpenSQL("select * from " + preptable);
                    if (dt.resultCode < 0) throw new Exception(dt.resultMessage);

                    int lsCount = dt.resultData.Rows.Count;
                    PaymentCodeRequest request = new PaymentCodeRequest();

                    for (int rowCount = 0; rowCount < dt.resultData.Rows.Count; rowCount++)
                    {
                        dbRep2.SetFileProgress(nzpExc, (decimal)0.3 + rowCount / 100);

                        // заполнить поля объекта request, который используется для генерации платежного кода
                        request.ClearField();
                        FillPaymentCodeRequest(dt.resultData.Rows[rowCount], ref request);

                        // результаты генерации в объекте request
                        ret = GeneratePaymentCode(ref request);

                        if (ret.result)
                        {
                            // сохранить в таблицу _preptable полученные коды
                            UpdateCodesInPreparedTableData(ref request, preptable);
                        }
                        else
                        {
                            if (ret.tag < 0) listErrPkod.Add(ret.text);
                            else listErrMsg.Add(ret.text);
                        }
                    }

                    dbRep2.SetFileProgress(nzpExc, (decimal)0.7);

                    // подготовить таблицу с дублирующимися платежными кодами
                    PrepareDublicatePaymentCodeTable(preptable);

                    List<string> succesPaymentCodeList = new List<string>();
                    List<string> dubplicatePaymentCodeList = new List<string>();

                    if (!(listErrPkod.Count == lsCount || listErrMsg.Count == lsCount))
                    {
                        dbRep2.SetFileProgress(nzpExc, (decimal)0.9);

                        // сохранить успешно сгенерированные платежные коды
                        SavePaymentCode(finder, preptable);

                        GetSuccessPaymentCodeList(preptable, out succesPaymentCodeList);
                    }

                    GetDublicatePaymentCodeList(out dubplicatePaymentCodeList);

                    // сформировать протокол о генерации платежных кодов
                    string filename;
                    SaveProtocol(finder, lsCount, succesPaymentCodeList, listErrMsg, dubplicatePaymentCodeList, out filename);

                    dbRep2.SetFileStatus(nzpExc, ExcelUtility.Statuses.Success);
                    dbRep2.SetFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = filename });

                    dbRep2.Close();
                }

                return ret;
            }
            catch (Exception ex)
            {
                if (preptable == "") MonitorLog.WriteLog("Нет данных для генерации платежных кодов.", MonitorLog.typelog.Error, true);
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Сгенерировать платежный код
        /// </summary>
        /// <param name="regionCode">код региона</param>
        /// <param name="areaCode">код организации</param>
        /// <param name="pkod10">код10</param>
        /// <returns>платежный код</returns>
        protected Returns GeneratePaymentCode(ref PaymentCodeRequest request)
        {
            Returns ret = new Returns(true);

            request.activeCode = 0;
            request.pkod10 = 0;
            request.pkod = "";

            try
            {
                int activeCode = 0;
                int pkod10 = 0;     // идентификатор абонента в платежном коде 
                string pkod = "";   // платежный код 

                // 1. Получить активный код
                ret = GetActiveCode(request, out activeCode);
                if (!ret.result)
                {
                    throw new Exception((ret.tag < 0 ? "Невозможно получить активный код " + GetParentName(request.parentName) : "Ошибка получения активного кодa " + GetParentName(request.parentName)) +
                        ": " + ret.text);
                }

                // 2. Получить pkod10
                int curActiveCode = 0;

                // ... в результате выполнения GetNextPkod10 может измениться текущий активный код
                ret = GetNextPkod10(request, activeCode, out curActiveCode, out pkod10);
                if (curActiveCode != activeCode) activeCode = curActiveCode;

                // 3. Проверки
                if (pkod10 == 0 || !ret.result || Points.Region.GetHashCode() <= 0 || activeCode <= 0)
                {
                    return new Returns(false,
                        String.Format("Ошибка получения идентификатора абонента в платежном коде: regionCode={0}, areaCode={1}, pkod10={2} ",
                        Points.Region.GetHashCode(), activeCode, pkod10),
                        -999);
                }

                if (Points.Region.GetHashCode() > 99 || activeCode > 99999 || pkod10 > 99999)
                {
                    throw new Exception(String.Format("Ошибка генерации платежного кода. Несовпадение количества символов в составных частях. Код региона: {0}, " +
                        "Код " + GetParentName(request.parentName) + ": {1}, Пкод10: {2}",
                        Points.Region.GetHashCode(), activeCode, pkod10));
                }

                // 4. Генерация платежного кода

                // 4.1. Сформировать платежный код
                pkod = FormPaymentCode(activeCode, pkod10);

                // 4.2. Добавить контрольную сумму
                pkod = GetPaymentCodeWithCheckSum(pkod);

                request.activeCode = activeCode;
                request.pkod10 = pkod10;
                request.pkod = pkod;

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при генерации платежных кодов. " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, ex.Message, 0);
            }
        }

        /// <summary>
        /// Подготовка таблицы _preptable с данными для генерации платежных кодов
        /// </summary>
        /// <param name="finder">используется dopFind[0], в котором передается код таблицы с выбранным ЛС</param>
        /// <returns></returns>
        protected Returns PrepareTableDataForGenerate(Finder finder, out string preptable)
        {
            Returns ret = Utils.InitReturns();
            preptable = "";

#if PG
            ExecSQL("set search_path to 'public'", false);
#endif

            //список выбранных лицевых счетов, после шаблона поиска, сохраненных в таблицу tXX,
            //где XX - номер очереди & уникальный код задачи в этой очереди
            //ЛС отфильтрованы mark=1
            string tXX = "t";
            if (finder.dopFind != null && finder.dopFind.Count > 0 && finder.dopFind[0] != "")
            {
                tXX += finder.dopFind[0];
            }

            if (!TempTableInWebCashe(tXX))
            {
                ret = new Returns(false, "Нет выбранных ЛС");
                MonitorLog.WriteLog("Ошибка генерации платежных кодов: " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            preptable = "";
            FillTableDataForGenerate(tXX, out preptable);
            
            return ret;
        }

        /// <summary>
        /// Заполнение таблицы для генерации платежных кодов
        /// </summary>
        /// <param name="lsTableName">Название таблицы из кэша с информацией по ЛС</param>
        /// <param name="preptable">Название таблицы с данными для генерации платежных кодов</param>
        /// <returns></returns>
        protected abstract Returns FillTableDataForGenerate(string lsTableName, out string preptable);

        /// <summary>
        /// Формирование платежного кода
        /// </summary>
        /// <param name="activeCode">Активный код</param>
        /// <param name="pkod10">pkod10</param>
        /// <returns></returns>
        protected string FormPaymentCode(int activeCode, int pkod10)
        {
            return Points.Region.GetHashCode().ToString("00") + activeCode.ToString("00000") + pkod10.ToString("00000");
        }

        /// <summary>
        /// Получить активный код
        /// </summary>
        /// <param name="finder">Finder</param>
        /// <param name="activeCode">Активный код</param>
        /// <returns>Returns c результатом</returns>
        /// <summary>
        protected Returns GetActiveCode(PaymentCodeRequest request, out int activeCode)
        {
            activeCode = Constants._ZERO_;
            Returns ret = new Returns(true);
            MyDataReader reader = null;

            string sql = "select code from " + Points.Pref + "_data" + DBManager.tableDelimiter + "area_codes where is_active = 1 " +
                // условие на родительский ключ
                GetParentIDCondition(request.parentID);

            if (!ExecRead(out reader, sql).result)
            {
                return new Returns(false, "Ошибка получения данных из area_codes");
            }

            if (reader.Read())
            {
                if (reader["code"] != DBNull.Value) activeCode = Convert.ToInt32(reader["code"]);
            }
            reader.Close();

            if (activeCode == Constants._ZERO_)
            {
                return new Returns(false, "Не установлен код " + GetParentName(request.parentName), -1);
            }

            return ret;
        }
        
        /// <summary>
        /// Получить следующее значение из последовательности kvar_pkod10_[active_code]_seq в центральном банке данных [central_bank]_data 
        /// </summary>
        /// <param name="active_code">Активный код</param>
        /// <param name="pkod10"></param>
        /// <returns></returns>
        protected Returns GetPkod10FromSequence(int active_code, out int pkod10)
        {
            Returns ret = new Returns(true);
            pkod10 = Constants._ZERO_;

            string seqName = "kvar_pkod10_" + active_code + "_seq";

            string sql = " SELECT " +
#if PG
                " nextval('" + Points.Pref + "_data." + seqName + "') as pkod10";
#else
                Points.Pref + "_data:" + seqName + ".nextval as pkod10 from " + Points.Pref + "_data:dual";
#endif

            MyDataReader reader = null;
            ret = ExecRead(out reader, sql);
            if (!ret.result)
            {
                ret = new Returns(false, "Ошибка получения pkod10");
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            if (reader.Read())
            {
                if (reader["pkod10"] != DBNull.Value) pkod10 = Convert.ToInt32(reader["pkod10"]);
            }

            reader.Close();

            return ret;
        }

        /// <summary>
        /// Получить следующий pkod10 
        /// </summary>
        /// <param name="finder">Finder</param>
        /// <param name="pkod10">pkod10 - идентификатора абонента в платежном коде</param>
        /// <returns></returns>
        protected Returns GetNextPkod10(PaymentCodeRequest request, int activeCode, out int curActiveCode, out int pkod10)
        {
            Returns ret = new Returns(true);
            pkod10 = Constants._ZERO_;
            // текущий активный код может измениться в результате выполнения процедуры GetNextPkod10
            curActiveCode = activeCode;
            
            //получить следующий pkod10 из последовательности {Pref}_data.kvar_pkod10_{active_code}_seq
            ret = GetPkod10FromSequence(activeCode, out pkod10);
            if (!ret.result) return ret;

            if (pkod10 > 99999)
            {
                pkod10 = Constants._ZERO_;

                int curInActiveCode = 0;
                MyDataReader reader = null;
                
                // получить список неактивных кодов
                string table_area_code = Points.Pref + "_data" + tableDelimiter + "area_codes";

                string sql = "select code from " + table_area_code + " where code > " + activeCode + " and is_active = 0 " + GetParentIDCondition(request.parentID) +
                    " order by code";
                
                if (!ExecRead(out reader, sql).result)
                {
                    ret = new Returns(false, "Ошибка получения данных из area_codes");
                    return ret;
                }

                while (reader.Read())
                {
                    pkod10 = Constants._ZERO_;
                    curInActiveCode = Constants._ZERO_;

                    // получить неактивный код curActiveCode
                    if (reader["code"] != DBNull.Value) curInActiveCode = Convert.ToInt32(reader["code"]);

                    // сделать все коды неактивными, кроме curActiveCode
                    sql = "update " + table_area_code + " set is_active = 0 where code <> " + curInActiveCode + GetParentIDCondition(request.parentID);
                    ret = ExecSQL(sql, true);
                    if (!ret.result) return ret;

                    // сделать активным код curActiveCode
                    sql = "update " + table_area_code + " set is_active = 1 where code = " + curInActiveCode + GetParentIDCondition(request.parentID);
                    ret = ExecSQL(sql, true);
                    if (!ret.result) return ret;

                    // получить следующее значение из последовательности kvar_pkod10_[curActiveCode]_seq
                    ret = GetPkod10FromSequence(curInActiveCode, out pkod10);
                    if (!ret.result) return ret;

                    if (pkod10 <= 99999)
                    {
                        curActiveCode = curInActiveCode;
                        break;
                    }
                } // while

                reader.Close();
                
                if (pkod10 == Constants._ZERO_)
                {
                    return new Returns(false, "Дополнительных кодов для " + GetParentName(request.parentName) + " нет", -1);
                }
                return ret;
            }
            else
            {
                if (pkod10 <= 0)
                {
                    return new Returns(false, "Дополнительных кодов для " + GetParentName(request.parentName) + " нет", -1);
                }
               
                return ret;
            }
        }

        /// <summary>
        /// Заполнение объекта PaymentCodeRequest для генерации платежного кода
        /// </summary>
        /// <param name="dr">Строка DataTable</param>
        protected abstract void FillPaymentCodeRequest(DataRow dr, ref PaymentCodeRequest req);

        /// <summary>
        /// Обновление активного кода, идентификатора абонента в платежном коде, сгенерированного платежного кода во временной таблице preptable
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        private Returns UpdateCodesInPreparedTableData(ref PaymentCodeRequest req, string preptable)
        {
            string sql = "update " + preptable + " set " +
                "area_code = " + req.activeCode + "," +
                " pkod = " + req.pkod + "," +
                " pkod10 = " + req.pkod10 +
                " where id = " + req.keyID;
            return ExecSQL(sql.ToString(), true);
        }

        /// <summary>
        /// Получить условие на внешний ключ в таблице area_codes
        /// Например: " and nzp_area = " + finder.nzp_area  или " and nzp_payer = " + finder.nzp_payer
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        protected abstract string GetParentIDCondition(int parentID);

        protected abstract string GetParentName(string parentName);

        /// <summary>
        /// Вычислить контрольную сумму и добавить ее к платежному коду
        /// </summary>
        /// <param name="pkod"></param>
        /// <returns></returns>
        protected string GetPaymentCodeWithCheckSum(string pkod)
        {
            int sum = 0;

            // цифры в нечетных позициях умножаем на 3 и складываем результат с цифрами в четных позициях 
            for (int charCnt = 0; charCnt < pkod.Length; charCnt++)
            {
                sum += (charCnt + 1) % 2 == 0 ? Convert.ToInt32(pkod[charCnt].ToString()) : Convert.ToInt32(pkod[charCnt].ToString()) * 3;
            }
            
            //определяем дополнение до 10
            int dop10 = 10 - sum % 10;

            return pkod + (dop10 == 10 ? 0 : dop10);
        }
    
        /// <summary>
        /// Сохранить протокол с результатами генерации платежных кодов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="lsCount"></param>
        /// <param name="listSucces">Список cгенерированных и сохраненных платежных кодов</param>
        /// <param name="listErrMsg"></param>
        /// <param name="listDublicates"></param>
        /// <returns></returns>
        protected Returns SaveProtocol(Finder finder, int lsCount, List<string> listSucces, List<string> listErrMsg, List<string> listDublicates, out string filename)
        {
            Returns ret = new Returns(true);
            filename = "";

            //получить имя пользователя, запустившего генерацию плат кодов, чтобы отразить его в протоколе
            string sql = "select uname from " + DBManager.sDefaultSchema + DBManager.tableDelimiter + "users " +
                " where nzp_user=" + finder.nzp_user;

            var obj = ExecScalar(sql.ToString(), out ret);
            finder.webLogin = (string)obj;

            //сохранение результатов в файл
            //наименование файла
            int k = 0;
            filename = "prot_genpkod_" + finder.nzp_user + "_";
            while (System.IO.File.Exists(Constants.ExcelDir + filename + k + ".txt")) k++;
            filename = filename + k + ".txt";
            string fullfilename = Constants.ExcelDir + filename;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(Constants.ExcelDir + filename, true, Encoding.GetEncoding(1251)))
            {
                //запись в файл
                file.WriteLine("Протокол результатов генерации платежных кодов от " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                file.WriteLine("Пользователь: " + finder.webLogin + " (код " + finder.nzp_user + ")");
                file.WriteLine("Всего обработано ЛС: " + lsCount.ToString() + ".");
                file.WriteLine("Всего сгенерированных платежных кодов: " + listSucces.Count + ".");
                file.WriteLine("Всего ошибок: " + listErrMsg.Count + ".");
                file.WriteLine("Всего дублирований платежных кодов: " + listDublicates.Count + ".");

                //запись ошибок
                if (listErrMsg.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список ошибок");
                    file.WriteLine();
                    foreach (string val in listErrMsg)
                    {
                        file.WriteLine(val);
                    }
                }
                if (listDublicates.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список дублирований платежных кодов");
                    file.WriteLine();
                    int c = 0;
                    foreach (string val in listDublicates)
                    {
                        c++;
                        file.WriteLine(c + ". " + val);
                    }
                }

                //запись результатов
                if (listSucces.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список сгенерированных платежных кодов");
                    file.WriteLine();
                    int c1 = 0;
                    foreach (string val in listSucces)
                    {
                        c1++;
                        file.WriteLine(c1 + ". " + val);
                    }
                }
            }

            if (InputOutput.useFtp) filename = InputOutput.SaveOutputFile(fullfilename);

            return ret;
        }

        /// <summary>
        /// Получить список cгенерированных и сохраненных платежных кодов
        /// </summary>
        /// <param name="listSucces">Список сообщений</param>
        /// <returns></returns>
        protected abstract Returns GetSuccessPaymentCodeList(string preptable, out List<string> listSucces);

        /// <summary>
        /// Получить список дублирующихся платежных кодов
        /// </summary>
        /// <param name="listSucces">Список сообщений</param>
        /// <returns></returns>
        protected abstract Returns GetDublicatePaymentCodeList(out List<string> listDuplicate);

        /// <summary>
        /// В таблицу tlogs записать дублирующиеся платежные коды
        /// </summary>
        /// <returns></returns>
        protected abstract Returns PrepareDublicatePaymentCodeTable(string preptable);

        /// <summary>
        /// Сохранить сгенерированные платежные коды из _preptable, исключая те, которые попали в таблицу tlogs
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        protected abstract Returns SavePaymentCode(Finder finder, string preptable);
    }
}
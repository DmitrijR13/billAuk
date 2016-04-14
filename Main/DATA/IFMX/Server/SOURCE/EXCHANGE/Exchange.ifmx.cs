using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;
using FastReport;
using System.Globalization;
using SevenZip;
using System.Xml;

namespace STCLINE.KP50.DataBase
{
    public partial class DbExchange : DataBaseHead
    {
        #region Загрузка оплат от ВТБ24
        /// <summary>
        /// Данные об операциях
        /// </summary>
        public struct VTB24Reestr
        {
            public string Uni { get; set; }
            public string Number { get; set; }
            public string DateOperation { get; set; }
            public string Account { get; set; }
            public string Amount { get; set; }
            public string Commission { get; set; }
        }
        /// <summary>
        /// Мета-данные файла
        /// </summary>
        public struct VTB24MetaData
        {
            public string MessageDate { get; set; }
            public string Sender { get; set; }
            public string Reciever { get; set; }
            public string ID { get; set; }
            public string Type { get; set; }
            public string TotalAmount { get; set; }
            public string TotalCommission { get; set; }
            public string Count { get; set; }
        }

        public VTB24MetaData Meta;
        public int nzpUser = 0;
        public string ErrorMessage; //Ошибки для протокола
        public int VTB24ReestrID = 0;
        public bool Success = true;
        public string Errors = ""; //Ошибки на вывод

        public Returns UploadVTB24(FilesImported finder)
        {

            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);


            #region Определение локального пользователя
            DbWorkUser db = new DbWorkUser();
            nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result) return ret;
            #endregion

            #region Разбираем файл
            //директория файла

            string fDirectory = "";
            if (InputOutput.useFtp)
            {
                fDirectory = InputOutput.GetInputDir();
                InputOutput.DownloadFile(finder.loaded_name, fDirectory + finder.saved_name, true);
            }
            else
            {
                fDirectory = Constants.Directories.ImportAbsoluteDir;
            }

            //string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
            string fileName = Path.Combine(fDirectory, finder.saved_name);

            List<VTB24Reestr> Items = GetParsedItems(fileName); //возвращает список операций
            if (Items == null)
            {
                conn_db.Close();
                ret.result = false;
                ret.text = "\n Ошибка при обработке xml файла";
                return ret;
            }
            #endregion

            #region Делаем запись о реестре
            ret = InsertReestrHeader(conn_db, finder);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            #region Записываем данные реестра
            IDbTransaction transaction = conn_db.BeginTransaction();
            ret = InsertReestrRows(conn_db, transaction, Items);
            if (!ret.result)
            {
                Errors += "\n " + ret.text;
                Success = false;
                transaction.Rollback();
            }
            else
            {
                transaction.Commit();
            }
            #endregion

            #region Сопоставить Платежные коды с ЛС в системе
            if (Success)
            {
                ret = CompareLsWithPkod(conn_db);
                if (!ret.result)
                {
                    ret.text = "\n Ошибка сопоставления ЛС в системе";
                    Errors += "\n " + ret.text;
                }
            }
            #endregion

            #region Записываем логи
            ret = InsertLogs(conn_db);
            if (!ret.result)
            {
                Errors += "\n " + ret.text;
                conn_db.Close();
                return ret;
            }
            #endregion

            #region Обновление статуса реестра
            ret = UpdateStatusReestr(conn_db, transaction, null);
            if (!ret.result)
            {
                Errors += "\n " + ret.text;
                conn_db.Close();
                return ret;
            }
            #endregion


            conn_db.Close();
            ret.result = Success;
            ret.text = Errors;
            ret.tag = VTB24ReestrID;
            return ret;
        }



        /// <summary>
        /// Функция разбора xml файла
        /// </summary>
        /// <param name="Url">Путь к файлу</param>
        /// <returns></returns>
        public List<VTB24Reestr> GetParsedItems(string Url)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(Url);
                List<VTB24Reestr> Collection = new List<VTB24Reestr>();

                XmlNode Message = xmlDoc.SelectSingleNode("//Message");
                //данные о файле
                Meta.MessageDate = ParseNodeValue(Message, "@Date");
                Meta.Sender = ParseNodeValue(Message, "@Sender");
                Meta.Reciever = ParseNodeValue(Message, "@Reciever");
                Meta.ID = ParseNodeValue(Message, "@ID");
                Meta.Type = ParseNodeValue(Message, "@Type");
                //итого по файлу
                XmlNode Total = xmlDoc.SelectSingleNode("//Total");
                Meta.TotalAmount = ParseNodeValue(Total, "@Amount");
                Meta.TotalCommission = ParseNodeValue(Total, "@Commission");
                Meta.Count = ParseNodeValue(Total, "@Count");

                //список операций
                XmlNodeList Items = xmlDoc.SelectNodes("//Operations/Operation");
                foreach (XmlNode node in Items)
                {
                    VTB24Reestr currentNode = new VTB24Reestr();
                    currentNode.Uni = ParseNodeValue(node, "@Uni");
                    currentNode.Number = ParseNodeValue(node, "@Number");
                    currentNode.DateOperation = ParseNodeValue(node, "@DateOperation");
                    currentNode.Account = ParseNodeValue(node, "@Account");
                    currentNode.Amount = ParseNodeValue(node, "@Amount");
                    currentNode.Commission = ParseNodeValue(node, "@Commission");
                    Collection.Add(currentNode);
                }

                return Collection;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора xml файла :" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;

            }
        }

        /// <summary>
        /// Функция получения значения атрибута
        /// </summary>
        /// <param name="item"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private string ParseNodeValue(XmlNode item, string propertyName)
        {
            XmlNode propertyNode = item.SelectSingleNode(propertyName);
            string value = string.Empty;
            if (propertyNode != null) value = propertyNode.InnerText;
            return value;
        }

        /// <summary>
        /// Функция записи заголовка реестра(tula_vtb24_d)
        /// </summary>
        /// <returns></returns>
        public Returns InsertReestrHeader(IDbConnection conn_db, FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();

            try
            {
                string message_date = Convert.ToDateTime(Meta.MessageDate).ToString("dd.MM.yyyy");
                decimal total_amount = Convert.ToDecimal(Meta.TotalAmount);
                decimal commission = Convert.ToDecimal(Meta.TotalCommission);
                int count_oper = Convert.ToInt32(Meta.Count);
                string file_name = finder.saved_name;
                int id = Convert.ToInt32(Meta.ID);

                sql.Remove(0, sql.Length);
                sql.Append("SELECT COUNT(*) FROM " + Points.Pref + sDataAliasRest + "tula_vtb24_d WHERE TRIM(file_name)='" + finder.saved_name.Trim() + "'");
                sql.Append(" and file_id=" + Meta.ID + " and message_date='" + message_date + "' and nzp_status<>" + (int)StatusVTB24.Fail + "");
                object count = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (ret.result)
                {
                    try
                    {
                        if (Convert.ToInt32(count) > 0)
                        {
                            ret.result = false;
                            ret.text = "\n Файл с таким именем, датой и номером уже был успешно загружен.";
                            return ret;
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка при получении данных о загруженных реестрах: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }

                sql.Remove(0, sql.Length);
                sql.Append("INSERT INTO " + Points.Pref + sDataAliasRest + "tula_vtb24_d  ");
                sql.Append("(message_date, total_amount, commission, count_oper, download_date, user_d, file_name, file_id ");
                sql.Append(",file_type, sender, receiver, nzp_status, nzp_bank) VALUES ");
                sql.Append(" (" + Utils.EStrNull(message_date) + "," + total_amount + "," + commission + "," + count_oper + "," + sCurDateTime + "," + nzpUser + ",'" + file_name + "'," + id);
                sql.Append("," + Utils.EStrNull(Meta.Type) + "," + Utils.EStrNull(Meta.Sender) + "," + Utils.EStrNull(Meta.Reciever) + "," + (int)StatusVTB24.None + "," + finder.nzp_bank + ")");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при записи заголовка файла реестра: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "\n Ошибка при разборе файла";
                    return ret;
                }

                VTB24ReestrID = GetSerialValue(conn_db);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Неверные значения полей заголовка файла :" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = "\n Ошибка при разборе файла";
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Записывает данные реестра 
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <param name="Items">Список объектов-строк реестра</param>
        /// <returns></returns>
        public Returns InsertReestrRows(IDbConnection conn_db, IDbTransaction transaction, List<VTB24Reestr> Items)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();

            for (int i = 0; i < Items.Count(); i++)
            {
                int num = 0;
                try
                {
                    string operation_uni = Items[i].Uni.Trim(); num++;
                    int number = Convert.ToInt32(Items[i].Number); num++;
                    string date_operation = Convert.ToDateTime(Items[i].DateOperation).ToString("yyyy-MM-dd hh:mm:ss"); num++;
                    string account = Convert.ToDecimal(Items[i].Account).ToString("0"); num++;
                    decimal amount = Convert.ToDecimal(Items[i].Amount); num++;
                    decimal commision = Convert.ToDecimal(Items[i].Commission); num++;
                    decimal sum_money = amount - commision;

                    sql.Remove(0, sql.Length);
                    sql.Append("INSERT INTO " + Points.Pref + sDataAliasRest + "tula_vtb24 (vtb24_down_id, operation_uni, number, date_operation, account, amount, commission, sum_money) VALUES ");
                    sql.Append("(" + VTB24ReestrID + "," + Utils.EStrNull(operation_uni) + "," + number + "," + Utils.EStrNull(date_operation) + "," + account + ", " + amount + "," + commision + "," + sum_money + ")");
                    ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка при записи данных файла: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "\n Ошибка при записи данных реестра";
                        return ret;
                    }

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("Ошибка функции записи данных реестра InsertReestrRows ", ex);
                    ErrorMessage = "Неверное значение поля в файле:";
                    switch (num)
                    {
                        case 0: ErrorMessage += " Uni;"; break;
                        case 1: ErrorMessage += " Number;"; break;
                        case 2: ErrorMessage += " DateOperation;"; break;
                        case 3: ErrorMessage += " Account;"; break;
                        case 4: ErrorMessage += " Amount;"; break;
                        case 5: ErrorMessage += " Commission;"; break;
                        default: break;
                    }
                    ErrorMessage += "Данные строки с ошибкой: Uni='" + Items[i].Uni + "'; Number='" + Items[i].Number + "'; DateOperation='" + Items[i].DateOperation + "';"
                        + "Account='" + Items[i].Account + "'; Amount='" + Items[i].Amount + "'; Commission='" + Items[i].Commission + "'  ";
                    ret.result = false;
                    ret.text = "\n Ошибка при записи данных реестра";
                    return ret;
                }
            }

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT SUM(amount) total_amount, SUM(commission) commission, SUM(1) count_oper ");
            sql.Append(" FROM " + Points.Pref + sDataAliasRest + "tula_vtb24 WHERE vtb24_down_id=" + VTB24ReestrID);
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction).resultData;
            if (DT.Rows.Count == 0)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка при проверке данных файла: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            }
            //Сверка данных файла с данными в заголовке файла
            if (DT.Rows[0]["total_amount"].ToString() != Meta.TotalAmount)
            {
                ret.result = false;
                ret.text = "\n Данные в заголовке файла не совпадают с данными в файле: поле TotalAmount=" + Meta.TotalAmount + ", а сумма по строкам файла =" + DT.Rows[0]["total_amount"].ToString() + " ";
                return ret;
            }
            if (DT.Rows[0]["commission"].ToString() != Meta.TotalCommission)
            {
                ret.result = false;
                ret.text = "\n Данные в заголовке файла не совпадают с данными в файле: поле TotalCommission=" + Meta.TotalCommission + ",а сумма по строкам файла =" + DT.Rows[0]["commission"].ToString() + " ";
                return ret;
            }
            if (DT.Rows[0]["count_oper"].ToString() != Meta.Count)
            {
                ret.result = false;
                ret.text = "\n Данные в заголовке файла не совпадают с данными в файле: поле Count=" + Meta.Count + ",а сумма по строкам файла =" + DT.Rows[0]["count_oper"].ToString() + " ";
                return ret;
            }
            return ret;
        }

        /// <summary>
        /// Запись логов 
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public Returns InsertLogs(IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            if (Success)
            {
                sql.Append("INSERT INTO " + Points.Pref + sDataAliasRest + "tula_vtb24_log (vtb24_down_id,date_log) VALUES (" + VTB24ReestrID + "," + sCurDateTime + ") ");
            }
            else
            {
                sql.Append("INSERT INTO " + Points.Pref + sDataAliasRest + "tula_vtb24_log (vtb24_down_id,errors,date_log) ");
                sql.Append(" VALUES (" + VTB24ReestrID + ", " + Utils.EStrNull(ErrorMessage) + ", " + sCurDateTime + ") ");
            }
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при записи логов: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка записи результата загрузки";
                return ret;
            }
            return ret;
        }



        /// <summary>
        /// Обновление статуса загрузки 
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public Returns UpdateStatusReestr(IDbConnection conn_db, IDbTransaction transaction, ExFinder finder)
        {
            Returns ret = Utils.InitReturns();
            bool create_trans = false;
            if (conn_db == null)
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
            }
            if (transaction == null)
            {
                transaction = conn_db.BeginTransaction();
                create_trans = true;
            }


            StringBuilder sql = new StringBuilder();
            if (finder == null)
            {
                if (Success)
                {
                    sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "tula_vtb24_d SET(nzp_status)=(" + (int)StatusVTB24.None + ") WHERE  vtb24_down_id=" + VTB24ReestrID + "");
                }
                else
                {
                    sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "tula_vtb24_d SET(nzp_status)=(" + (int)StatusVTB24.Fail + ") WHERE vtb24_down_id=" + VTB24ReestrID + "");
                }
                ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при обновлении статуса загрузки: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "\n Ошибка при обновлении статуса загрузки";
                    return ret;
                }
            }
            else
            {

                switch (finder.Status)
                {
                    case (int)StatusVTB24.Success:
                        {
                            sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "tula_vtb24_d SET(nzp_status)=(" + (int)StatusVTB24.Success + ") WHERE  vtb24_down_id=" + finder.VTB24ReestrID + "");
                        } break;
                    case (int)StatusVTB24.WithErrors:
                        {
                            sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "tula_vtb24_d SET(nzp_status)=(" + (int)StatusVTB24.WithErrors + ") WHERE  vtb24_down_id=" + finder.VTB24ReestrID + "");

                        } break;
                    case (int)StatusVTB24.Distributed:
                        {
                            sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "tula_vtb24_d SET(nzp_status)=(" + (int)StatusVTB24.Distributed + ") WHERE  vtb24_down_id=" + finder.VTB24ReestrID + "");
                        } break;
                    case (int)StatusVTB24.DistFail:
                        {
                            sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "tula_vtb24_d SET(nzp_status)=(" + (int)StatusVTB24.DistFail + ") WHERE  vtb24_down_id=" + finder.VTB24ReestrID + "");
                        } break;
                }
                ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при обновлении статуса загрузки: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "\n Ошибка при обновлении статуса загрузки";
                    return ret;
                }
            }
            if (create_trans) transaction.Commit();
            return ret;
        }




        public List<VTB24Info> GetReestrsVTB24(ExFinder finder, out Returns ret)
        {

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "\n Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            List<VTB24Info> res = new List<VTB24Info>();
            string exc = conn_web.Database + "@" + DBManager.getServer(conn_web) + tableDelimiter + "excel_utility";
            conn_web.Close();
            string only_success = "";
            if (finder.success) only_success = " and nzp_status<>" + (int)StatusVTB24.Fail + " ";
            StringBuilder sql = new StringBuilder();


            //string success = "";
            string skip = "";
            string rows = "";
            string limit = "";
            string offset = "";
#if PG
            if (finder.skip != 0)
            {
                offset = " OFFSET " + finder.skip;
            }
            if (finder.rows != 0)
            {
                limit = " LIMIT " + finder.rows;
            }
#else
            if (finder.skip != 0)
            {
                skip = " SKIP " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " FIRST " + finder.rows;
            }
#endif

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT " + rows + skip + " v.*, u.name as user_name, (CASE when e.stats=2 then e.nzp_exc end) nzp_exc, v.nzp_status, v.nzp_bank, p.bank ");
            sql.Append(" FROM  " + Points.Pref + sDataAliasRest + "users u,  " + Points.Pref + sDataAliasRest + "tula_vtb24_d v LEFT OUTER JOIN " + exc + " e ON v.nzp_exc=e.nzp_exc ");
            sql.Append(" LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_bank p ON v.nzp_bank=p.nzp_bank " + limit + offset);
            sql.Append(" WHERE u.nzp_user=v.user_d " + only_success + " ORDER BY v.vtb24_down_id DESC ");
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;

            try
            {
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    VTB24Info row = new VTB24Info();
                    row.vtb24_down_id = (DT.Rows[i]["vtb24_down_id"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["vtb24_down_id"]) : 0);
                    row.file_name = (DT.Rows[i]["file_name"] != DBNull.Value ? Convert.ToString(DT.Rows[i]["file_name"]).Trim() : "");
                    row.message_date = (DT.Rows[i]["message_date"] != DBNull.Value ? Convert.ToDateTime(DT.Rows[i]["message_date"]) : DateTime.MinValue);
                    row.total_amount = (DT.Rows[i]["total_amount"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[i]["total_amount"]) : 0);
                    row.commission = (DT.Rows[i]["commission"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[i]["commission"]) : 0);
                    row.count_oper = (DT.Rows[i]["count_oper"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["count_oper"]) : 0);
                    row.download_date = (DT.Rows[i]["download_date"] != DBNull.Value ? Convert.ToDateTime(DT.Rows[i]["download_date"]) : DateTime.MinValue);
                    row.user_name = (DT.Rows[i]["user_name"] != DBNull.Value ? Convert.ToString(DT.Rows[i]["user_name"]).Trim() : "");
                    row.nzp_exc = (DT.Rows[i]["nzp_exc"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["nzp_exc"]) : 0);
                    row.protocol = (row.nzp_exc != 0 ? "Скачать" : "Не сформирован");
                    row.status = (DT.Rows[i]["nzp_status"] != DBNull.Value ? row.getNameStatusVTB24(Convert.ToInt32(DT.Rows[i]["nzp_status"])) : "");
                    row.nzp_bank = (DT.Rows[i]["nzp_bank"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["nzp_bank"]) : 0);
                    row.bank = (DT.Rows[i]["bank"] != DBNull.Value ? Convert.ToString(DT.Rows[i]["bank"]).Trim() : "");
                    res.Add(row);
                }

                sql.Remove(0, sql.Length);
                sql.Append("SELECT COUNT(*) FROM  " + Points.Pref + sDataAliasRest + "tula_vtb24_d " + (finder.success == true ? "where nzp_status<>" + (int)StatusVTB24.Fail + "" : ""));
                object count = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (ret.result)
                {
                    try
                    {
                        ret.tag = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка при получении данных о загруженных реестрах: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при получении данных о загруженных реестрах: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }

            return res;
        }

        /// <summary>
        /// Удаление оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns DeleteReestrVTB24(ExFinder finder)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "\n Не определен пользователь";
                return ret;
            }
            IDbTransaction transaction = conn_db.BeginTransaction();

            sql.Append(" DELETE FROM " + Points.Pref + sDataAliasRest + "tula_vtb24 WHERE vtb24_down_id=" + finder.VTB24ReestrID + "");
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при удалении оплат от ВТБ24: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при удалении оплат";
                return ret;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" DELETE FROM " + Points.Pref + sDataAliasRest + "tula_vtb24_d WHERE vtb24_down_id=" + finder.VTB24ReestrID + "");
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при удалении оплат от ВТБ24: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при удалении оплат";
                return ret;
            }

            if (ret.result)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }
            return ret;
        }

        /// <summary>
        /// Получение данных для формирования протокола ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public ProtocolVTB24 GetProtocolVTB24(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ProtocolVTB24 res = new ProtocolVTB24();
            StringBuilder sql = new StringBuilder();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "\n Не определен пользователь";
                return res;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT d.*, u.name as user_name FROM " + Points.Pref + sDataAliasRest + "tula_vtb24_d d,  " + Points.Pref + sDataAliasRest + "users u ");
            sql.Append(" WHERE vtb24_down_id = " + finder.VTB24ReestrID + " and user_d = u.nzp_user");
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;

            try
            {
                VTB24Info inf = new VTB24Info();
                res.file_name = (DT.Rows[0]["file_name"] != DBNull.Value ? Convert.ToString(DT.Rows[0]["file_name"]).Trim() : "");
                res.message_date = (DT.Rows[0]["message_date"] != DBNull.Value ? Convert.ToDateTime(DT.Rows[0]["message_date"]) : DateTime.MinValue);
                res.total_amount = (DT.Rows[0]["total_amount"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[0]["total_amount"]) : 0);
                res.commission = (DT.Rows[0]["commission"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[0]["commission"]) : 0);
                res.count_oper = (DT.Rows[0]["count_oper"] != DBNull.Value ? Convert.ToInt32(DT.Rows[0]["count_oper"]) : 0);
                res.download_date = (DT.Rows[0]["download_date"] != DBNull.Value ? Convert.ToDateTime(DT.Rows[0]["download_date"]) : DateTime.MinValue);
                res.user_name = (DT.Rows[0]["user_name"] != DBNull.Value ? Convert.ToString(DT.Rows[0]["user_name"]).Trim() : "");
                res.receiver = (DT.Rows[0]["receiver"] != DBNull.Value ? Convert.ToString(DT.Rows[0]["receiver"]).Trim() : "");
                res.file_id = (DT.Rows[0]["file_id"] != DBNull.Value ? Convert.ToInt32(DT.Rows[0]["file_id"]) : 0);
                res.status = (DT.Rows[0]["nzp_status"] != DBNull.Value ? inf.getNameStatusVTB24(Convert.ToInt32(DT.Rows[0]["nzp_status"])) : "");

            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при формировании протокола для ВТБ24: " + sql.ToString(), ex);
                ret.text = "\n Ошибка при формировании протокола";
                ret.result = false;
                return res;
            }

            //Получаем данные  по сопоставленным ЛС
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT sum(1)  compared_count, sum(amount) compared_amount FROM " + Points.Pref + sDataAliasRest + "tula_vtb24 WHERE vtb24_down_id = " + finder.VTB24ReestrID + " and nzp_kvar is not null ");
            DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;
            try
            {
                res.compared_count = (DT.Rows[0]["compared_count"] != DBNull.Value ? Convert.ToInt32(DT.Rows[0]["compared_count"]) : 0);
                res.compared_amount = (DT.Rows[0]["compared_amount"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[0]["compared_amount"]) : 0);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при формировании протокола для ВТБ24: " + sql.ToString(), ex);
                ret.text = "\n Ошибка при формировании протокола";
                ret.result = false;
                return res;
            }



            //Получаем данные  по сопоставленным ЛС
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT errors FROM " + Points.Pref + sDataAliasRest + "tula_vtb24_log WHERE vtb24_down_id = " + finder.VTB24ReestrID);
            DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;
            try
            {
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    res.errors += "\n" + (DT.Rows[i]["errors"] != DBNull.Value ? Convert.ToString(DT.Rows[i]["errors"]).Trim() : "");
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при формировании протокола для ВТБ24: " + sql.ToString(), ex);
                ret.text = "\n Ошибка при формировании протокола";
                ret.result = false;
                return res;
            }


            sql.Remove(0, sql.Length);
            sql.Append(" SELECT account,operation_uni FROM " + Points.Pref + sDataAliasRest + "tula_vtb24 WHERE vtb24_down_id = " + finder.VTB24ReestrID + " and nzp_kvar is null ");
            DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;
            try
            {
                res.Rows = new List<VTB24ReestrRow>();
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    VTB24ReestrRow row = new VTB24ReestrRow();
                    row.account = (DT.Rows[i]["account"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[i]["account"]) : 0);
                    row.operation_uni = (DT.Rows[i]["operation_uni"] != DBNull.Value ? Convert.ToString(DT.Rows[i]["operation_uni"]).Trim() : "");
                    res.Rows.Add(row);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при формировании протокола для ВТБ24: " + sql.ToString(), ex);
                ret.text = "\n Ошибка при формировании протокола";
                ret.result = false;
                return res;
            }

            return res;
        }

        /// <summary>
        /// Получение ссылки-ключа на протокол для реестров
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="nzp_exc"></param>
        /// <returns></returns>
        public Returns UpdateExcVTB24(ExFinder finder, int nzp_exc)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "\n Не определен пользователь";
                return ret;
            }
            StringBuilder sql = new StringBuilder();

            sql.Remove(0, sql.Length);
            sql.Append("UPDATE  " + Points.Pref + sDataAliasRest + "tula_vtb24_d SET (nzp_exc)=(" + nzp_exc + ") WHERE vtb24_down_id=" + finder.VTB24ReestrID);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при обновлении статуса протокола для ВТБ24: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при обновлении статуса протокола";
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Сопоставление pkod с nzp_kvar для ВТБ24
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public Returns CompareLsWithPkod(IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();

            sql.Remove(0, sql.Length);
            sql.Append("UPDATE " + Points.Pref + sDataAliasRest + "tula_vtb24 SET(nzp_kvar)=((SELECT kv.nzp_kvar FROM ");
            sql.Append(Points.Pref + sDataAliasRest + "kvar kv WHERE kv.pkod=" + Points.Pref + sDataAliasRest + "tula_vtb24.account and ");
            sql.Append(Points.Pref + sDataAliasRest + "tula_vtb24.vtb24_down_id=" + VTB24ReestrID + ")) WHERE " + Points.Pref + sDataAliasRest + "tula_vtb24.vtb24_down_id=" + VTB24ReestrID);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при сопоставлении pkod: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при сопоставлении ЛС в системе";
                return ret;
            }


            return ret;
        }

        /// <summary>
        /// Проверяем возможность распредления пачки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool CanDistr(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            StringBuilder sql = new StringBuilder();
            #region Проверка статуса
            int count = 0;
            sql.Remove(0, sql.Length);
            sql.Append("SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_vtb24_d WHERE vtb24_down_id=" + finder.VTB24ReestrID);
            sql.Append(" and nzp_status in (" + (int)StatusVTB24.DistFail + "," + (int)StatusVTB24.WithErrors + "," + (int)StatusVTB24.Success + ") ");
            var obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                count = Convert.ToInt32(obj);
            }
            else
            {

                MonitorLog.WriteLog("Ошибка получения данных о пачке: " + sql.ToString(), MonitorLog.typelog.Error, true);
                return false;
            }

            if (count == 0) return false;
            #endregion
            #region Проверка на наличие оплат для распределения
            count = 0;
            sql.Remove(0, sql.Length);
            sql.Append("SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_vtb24 WHERE vtb24_down_id=" + finder.VTB24ReestrID);
            sql.Append(" and nzp_kvar is not null ");
            obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                count = Convert.ToInt32(obj);
            }
            else
            {

                MonitorLog.WriteLog("Ошибка получения данных о пачке: " + sql.ToString(), MonitorLog.typelog.Error, true);
                return false;
            }

            if (count == 0)
            {
                ret.result = false;
                ret.text = "\n Нет данных для распределения.";
                return false;
            }
            #endregion
            ret = DistPack(conn_db, finder);//вызываем распрделение пачки

            return true;
        }


        /// <summary>
        /// Распределение пачек для оплат от ВТБ24
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns DistPack(IDbConnection conn_db, ExFinder finder)
        {
            Returns ret = Utils.InitReturns();
            if (conn_db == null)
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
            }
            IDbTransaction transaction = conn_db.BeginTransaction();
            if (!ret.result) return ret;

            StringBuilder sql = new StringBuilder();
            string dat = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало  тек.расчетного месяца
            string dat_next = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1).ToShortDateString() + "'"; //след.рассчетный месяц
            string dat_prev = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1).ToShortDateString() + "'"; //предыдущий рассчетный месяц
            string month = Points.CalcMonth.month_.ToString("00");
            string year = (Points.CalcMonth.year_ - 2000).ToString("00");
            string operDay = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString("dd.MM.yyyy");
            decimal SUM_MONEY = 0;
            int COUNT = 0;
            int NZP_BANK = finder.nzp_bank;

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT SUM(sum_money) sum_money, SUM(1) count FROM " + Points.Pref + sDataAliasRest + "tula_vtb24 WHERE vtb24_down_id = " + finder.VTB24ReestrID + " and nzp_kvar is not null ");
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db, transaction).resultData;
            try
            {
                SUM_MONEY = (DT.Rows[0]["sum_money"] != DBNull.Value ? Convert.ToDecimal(DT.Rows[0]["sum_money"]) : 0);
                COUNT = (DT.Rows[0]["count"] != DBNull.Value ? Convert.ToInt32(DT.Rows[0]["count"]) : 0);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при распределении пачки: " + sql.ToString(), ex);
                ret.text = "\n Ошибка при распределении пачки";
                ret.result = false;
                finder.Status = (int)StatusVTB24.DistFail;
                UpdateStatusReestr(conn_db, transaction, finder);
                return ret;
            }


            #region Запись  в pack

            sql.Remove(0, sql.Length);
            sql.Append(" INSERT INTO " + Points.Pref + "_fin_" + year + "" + tableDelimiter + "pack  ");
            sql.Append("(pack_type, nzp_bank,num_pack, dat_pack, count_kv, sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) ");
            sql.Append(" SELECT 10," + NZP_BANK + ",v.file_id,message_date," + COUNT + "," + SUM_MONEY + "," + COUNT + ",11," + sCurDateTime + ",v.file_name,'" + operDay + "'");
            sql.Append(" FROM " + Points.Pref + sDataAliasRest + "tula_vtb24_d v WHERE vtb24_down_id=" + finder.VTB24ReestrID + " ");
            if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
            {
                ret.text = "Ошибка записи пачек оплаты";
                ret.result = false;
                MonitorLog.WriteLog("Ошибка записи в таблицу pack: " + sql.ToString(), MonitorLog.typelog.Error, true);
                return ret;
            }
            var nzp_pack = GetSerialValue(conn_db, transaction);



            #endregion
            //получаем локального пользователя
            DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            //записываем в pack_ls

            sql.Remove(0, sql.Length);
            sql.Append(" INSERT INTO " + Points.Pref + "_fin_" + year + tableDelimiter + "pack_ls ");
            sql.Append(" (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, ");
            sql.Append(" inbasket, alg, unl, incase, pkod, nzp_user,dat_month) ");
            sql.Append(" SELECT " + nzp_pack + ", v.nzp_kvar,v.sum_money, 0,33,1,0,v.date_operation,v.number,0,0,0,0,v.account," + nzpUser + " " + "," + dat_prev);
            sql.Append(" FROM " + Points.Pref + sDataAliasRest + "tula_vtb24 v WHERE vtb24_down_id=" + finder.VTB24ReestrID);
            sql.Append(" and v.nzp_kvar is not null");
            if (!ExecSQL(conn_db, transaction, sql.ToString(), true).result)
            {
                ret.text = "Ошибка записи оплат по лицевым счетам";
                ret.result = false;
                MonitorLog.WriteLog("Ошибка записи в таблицу pack_ls: " + sql.ToString(), MonitorLog.typelog.Error, true);
                return ret;
            }




            #region распределение пачек
            //в зависимости от настроек распределяем пачки сразу 
            if (Points.packDistributionParameters.DistributePackImmediately)
            {

                DbCalcPack db1 = new DbCalcPack();
                DbPack DbPack = new DbPack();

                Pack pack = new Pack();
                db1.PackFonTasks(nzp_pack, finder.nzp_user, FonTaskTypeIds.DistributePack, out ret, conn_db, transaction);  // Отдаем пачку на распределение                 
                pack.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                pack.nzp_user = finder.nzp_user;
                pack.nzp_pack = nzp_pack;
                pack.year_ = Points.CalcMonth.year_;
                if (ret.result)
                {
                    DbPack.UpdatePackStatus(pack);
                }

                db1.Close();
                DbPack.Close();
            }
            #endregion

            if (!ret.result)
            {
                finder.Status = (int)StatusVTB24.DistFail;
                transaction.Rollback();
                UpdateStatusReestr(conn_db, null, finder);

            }
            else
            {
                finder.Status = (int)StatusVTB24.Distributed;
                UpdateStatusReestr(conn_db, transaction, finder);
                transaction.Commit();
            }
            return ret;
        }



        #endregion Загрузка оплат от ВТБ24

    }
}



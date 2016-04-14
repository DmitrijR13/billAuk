using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bars.KP50.DataImport.CHECK;
using Bars.KP50.DB.Exchange.TransferHouses;
using Castle.Components.DictionaryAdapter.Xml;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Data;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using STCLINE.KP50.Utility;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Component = Castle.MicroKernel.Registration.Component;


namespace STCLINE.KP50.DataBase
{
    public class DbExchange : DataBaseHeadServer
    {
        #region Загрузка оплат от ВТБ24
        /// <summary>
        /// Данные об операциях
        /// </summary>
        public class VTB24Reestr
        {
            public string Uni { get; set; }
            public string Number { get; set; }
            public string DateOperation { get; set; }
            public string Account { get; set; }
            public string Amount { get; set; }
            public string Commission { get; set; }

            public VTB24Reestr()
            {
                Uni = "";
                Number = "";
                DateOperation = "";
                Account = "";
                Commission = "0";

            }
        }
        /// <summary>
        /// Мета-данные файла
        /// </summary>
        public class VTB24MetaData
        {
            private string bf_TotalCommission;
            public string MessageDate { get; set; }
            public string Sender { get; set; }
            public string Reciever { get; set; }
            public string ID { get; set; }
            public string Type { get; set; }
            public string TotalAmount { get; set; }

            public string TotalCommission
            {
                get { return bf_TotalCommission; }
                set
                {
                    if (value == "")
                    {
                        bf_TotalCommission = "0";
                    }
                    else
                    {
                        bf_TotalCommission = value;
                    }
                }
            }

            public string Count { get; set; }

        }

        public VTB24MetaData Meta = new VTB24MetaData();
        public int nzpUser = 0;
        public string ErrorMessage; //Ошибки для протокола
        public int VTB24ReestrID = 0;
        public bool Success = true;
        public string Errors = ""; //Ошибки на вывод

        public Returns UploadVTB24(FilesImported finder)
        {
            Utils.setCulture();
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);


            #region Определение локального пользователя
            nzpUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result) return ret;*/
            #endregion

            #region Разбираем файл
            //директория файла

            string fDirectory = "";
            if (InputOutput.useFtp)
            {
                fDirectory = InputOutput.GetInputDir();
                InputOutput.DownloadFile(finder.ex_path, Path.Combine(fDirectory, finder.ex_path), true);
            }
            else
            {
                fDirectory = Constants.Directories.ImportAbsoluteDir;
            }

            //string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
            string fileName = Path.Combine(fDirectory, finder.ex_path);

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
                var stream = new StreamReader(Url, GetEncoding(Url));
                xmlDoc.Load(stream);
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
                    currentNode.Account = ParseNodeValue(node, "@Code");
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

            decimal commission = 0;
            try
            {
                string message_date = Convert.ToDateTime(Meta.MessageDate).ToString("dd.MM.yyyy");
                decimal total_amount = Convert.ToDecimal(Meta.TotalAmount);
                decimal.TryParse(Meta.TotalCommission, out commission);

                int count_oper = Convert.ToInt32(Meta.Count);
                string file_name = finder.saved_name;
                int id = Convert.ToInt32(Meta.ID);

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
            decimal commision = 0;
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
                    decimal.TryParse(Items[i].Commission, out commision); num++;
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
            if (CastValue<decimal>(DT.Rows[0]["total_amount"]) != CastValue<decimal>(Meta.TotalAmount))
            {
                ret.result = false;
                ret.text = "\n Данные в заголовке файла не совпадают с данными в файле: поле TotalAmount=" + Meta.TotalAmount + ", а сумма по строкам файла =" + DT.Rows[0]["total_amount"].ToString() + " ";
                return ret;
            }
            if (CastValue<decimal>(DT.Rows[0]["commission"]) != CastValue<decimal>(Meta.TotalCommission))
            {
                ret.result = false;
                ret.text = "\n Данные в заголовке файла не совпадают с данными в файле: поле TotalCommission=" + Meta.TotalCommission + ",а сумма по строкам файла =" + DT.Rows[0]["commission"].ToString() + " ";
                return ret;
            }
            if (CastValue<int>(DT.Rows[0]["count_oper"]) != CastValue<int>(Meta.Count))
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
            string exc = "";
#if PG
            exc = DBManager.sDefaultSchema + "excel_utility";
#else
			exc = conn_web.Database + "@" + DBManager.getServer(conn_web) + tableDelimiter + "excel_utility";
#endif

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
            sql.Append(" LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_bank p ON v.nzp_bank=p.nzp_bank ");
            sql.Append(" WHERE u.nzp_user=v.user_d " + only_success + " ORDER BY v.vtb24_down_id DESC " + limit + offset);
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
            string finAlias = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") +
                              tableDelimiter;
            #region Проверка распределенных сумм


            var sqlStr = " select count(*) as count_rasp " +
                   " from " + finAlias + "pack p," +
                   "      " + finAlias + "pack_ls pl, " +
                   "      " + Points.Pref + sDataAliasRest + "tula_vtb24_d tv " +
                   " where trim(p.file_name) = trim(tv.file_name) " +
                   " and p.nzp_pack = pl.nzp_pack " +
                   " and pl.dat_uchet is not null " +
                   " and trim(p.num_pack) = tv.file_id::varchar " +
                   " and p.dat_pack = tv.message_date and tv.vtb24_down_id = " + finder.VTB24ReestrID;

            var countRasp = ExecScalar(conn_db, sqlStr, out ret, true);
            if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
            {
                ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                         "сначала отмените распределение по данному реестру", -1) { result = false };
                return ret;
            }

            IDbTransaction transaction = conn_db.BeginTransaction();

            sqlStr = "select nzp_pack from " + finAlias + "pack_ls where nzp_pack in (select nzp_pack" +
               " from " + finAlias + "pack p, " +
               " " + Points.Pref + sDataAliasRest + "tula_vtb24_d tv " +
               " where p.file_name = tv.file_name and trim(p.num_pack) = tv.file_id::varchar and p.dat_pack = tv.message_date and tv.vtb24_down_id = " + finder.VTB24ReestrID + ")  " +
               " group by 1";
            var DT = ClassDBUtils.OpenSQL(sqlStr, conn_db, transaction).resultData;
            var l_nzp_pack = (from DataRow row in DT.Rows select CastValue<string>(row["nzp_pack"])).ToList();
            var s_nzp_pack = string.Join(",", l_nzp_pack);
            if (s_nzp_pack == "") s_nzp_pack = "-1";

            //Удаление оплат
            sqlStr = " delete  " +
                  " from " + finAlias + "pack_ls " +
                  " where nzp_pack in (select nzp_pack" +
                  " from " + finAlias + "pack p, " +
                  Points.Pref + sDataAliasRest + "tula_vtb24_d tv " +
                  " where trim(p.file_name) = trim(tv.file_name) and trim(p.num_pack) = tv.file_id::varchar and p.dat_pack = tv.message_date and tv.vtb24_down_id = " + finder.VTB24ReestrID + ")";
            ret = ExecSQL(conn_db, transaction, sqlStr, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при удалении оплат от ВТБ24: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при удалении оплат";
                transaction.Rollback();
                return ret;
            }

            //Удаление пачек
            sqlStr = " delete  " +
                  " from " + finAlias + "pack " +
                  " where nzp_pack in (" + s_nzp_pack + ")";
            ret = ExecSQL(conn_db, transaction, sqlStr, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при удалении оплат от ВТБ24: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при удалении оплат";
                transaction.Rollback();
                return ret;
            }

            #endregion

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
            //     string month = Points.CalcMonth.month_.ToString("00");
            string year = (Points.DateOper.Year - 2000).ToString("00");
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

            int nzpUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/

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

                Pack pack = new Pack();
                db1.PackFonTasks(nzp_pack, Points.DateOper.Year, finder.nzp_user,
                    CalcFonTask.Types.DistributePack, out ret,
                    conn_db, transaction);  // Отдаем пачку на распределение  


                pack.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                pack.nzp_user = finder.nzp_user;
                pack.nzp_pack = nzp_pack;
                pack.year_ = Points.DateOper.Year;
                if (ret.result)
                {
                    db1.UpdatePackStatus(pack);
                }

                db1.Close();

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


        #region Взаимодействие со сторонними поставщиками

        public int NzpReestr;
        public string reestr_ex_supp_sync_ls = Points.Pref + sDataAliasRest + "reestr_ex_supp_sync_ls";
        public string reestr_ex_supp_change_ls = Points.Pref + sDataAliasRest + "reestr_ex_supp_change_ls";
        public string ex_supp_versions = Points.Pref + sDataAliasRest + "ex_supp_versions";

        public Returns InsertRow(Object obj, string TableName)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db;
            IDbTransaction transaction = null;
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            conn_db = DBManager.newDbConnection(conn_kernel);

            if (!OpenDb(conn_db, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return ret;
            }
            return InsertRow(conn_db, transaction, obj, TableName);
        }


        public Returns InsertRow(IDbConnection conn_db, Object obj, string TableName)
        {
            IDbTransaction transaction = null;
            return InsertRow(conn_db, transaction, obj, TableName);
        }


        public Returns InsertRow(IDbConnection conn_db, IDbTransaction transaction, Object obj, string TableName)
        {
            Returns ret = Utils.InitReturns();

            Type typeCLass = obj.GetType();


            List<FieldInfo> fields = typeCLass.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).ToList();
            string sql = "";
            sql = "INSERT INTO " + TableName;
            string param = "";
            param += "(";
            string values = "";
            values += "(";
            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                if (value != null && value.ToString() != "")
                {


                    string field_name = GetFieldName(field.Name);
                    if (field_name == "nzp_reestr") continue;

                    bool NA = false;
                    switch (field.FieldType.Name)
                    {
                        case "DateTime":
                            {
                                DateTime date = Convert.ToDateTime(value);
                                value = Utils.EStrNull(date.ToString("yyyy-MM-dd HH:mm:ss"));
                            } break;
                        case "String":
                            {
                                value = Utils.EStrNull(value.ToString());
                            } break;
                        case "Int32":
                            {
                                if ((int)value < 0) NA = true;
                            } break;
                    }
                    if (NA == true) continue;
                    param += field_name + ",";
                    values += value + ",";
                }
            }
            if (param != "") param = param.Remove(param.Length - 1, 1) + ")";
            if (values != "") values = values.Remove(values.Length - 1, 1) + ")";
            sql += " " + param + " VALUES " + values;

            if (!ExecSQL(conn_db, transaction, sql, true).result)
            {
                ret.result = false;
                return ret;
            }

            ret.tag = GetSerialValue(conn_db, transaction);


            return ret;

        }


        public List<Object> ReestrPager(Object obj, string TableName, int rows, int skip, out Returns ret, string OrderBy)
        {
            ret = Utils.InitReturns();
            IDbConnection conn_db;
            //IDbTransaction transaction = null;
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            conn_db = DBManager.newDbConnection(conn_kernel);

            if (!OpenDb(conn_db, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }
            return ReestrPager(conn_db, obj, TableName, rows, skip, out ret, OrderBy);
        }

        public List<Object> ReestrPager(IDbConnection conn_db, Object obj, string TableName, int rows, int skip, out Returns ret, string OrderBy)
        {
            IDbTransaction transaction = null;
            return ReestrPager(conn_db, transaction, obj, TableName, rows, skip, out  ret, OrderBy);
        }
        public List<Object> ReestrPager(IDbConnection conn_db, Object obj, string TableName, int rows, int skip, out Returns ret)
        {
            IDbTransaction transaction = null;
            string OrderBy = "";
            return ReestrPager(conn_db, transaction, obj, TableName, rows, skip, out  ret, OrderBy);
        }

        /// <summary>
        /// Pager получает даннные из базы по типу входного класса obj
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="transaction">Транзакция</param>
        /// <param name="obj">Входной класс</param>
        /// <param name="TableName">Имя таблицы с указанием БД</param>
        /// <param name="rows">Число строк</param>
        /// <param name="skip">Пропустить строк</param>
        /// <param name="ret">Returns</param>
        /// <returns></returns>
        public List<Object> ReestrPager(IDbConnection conn_db, IDbTransaction transaction, Object obj, string TableName, int rows, int skip, out Returns ret, string OrderBy)
        {
            ret = Utils.InitReturns();

            Type typeCLass = obj.GetType();
            List<FieldInfo> fields = typeCLass.BaseType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();

            #region FIRST SKIP
            string sql = "";
            sql = "SELECT  ";
            string Offset = "";
            string Limit = "";
            string Skip = "";
            string Rows = "";
            if (OrderBy != "") OrderBy = " ORDER BY " + OrderBy + " DESC ";

#if PG
            if (skip != 0)
            {
                Offset = " OFFSET " + skip;
            }
            if (rows != 0)
            {
                Limit = " LIMIT " + rows;
            }
#else
			if (skip != 0)
			{
				Skip = " SKIP " + skip;
			}
			if (rows != 0)
			{
				Rows = " FIRST " + rows;
			}
#endif
            #endregion

            sql += Skip + " " + Rows;
            string values = "";
            foreach (var field in fields)
            {
                string field_name = GetFieldName(field.Name);
                values += field_name + ",";
            }

            if (values != "") values = values.Remove(values.Length - 1, 1);
            sql += " " + values + " FROM " + TableName + OrderBy + Limit + " " + Offset;

            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;
            var Count = ExecScalar(conn_db, transaction, "SELECT count(*) FROM " + TableName, out ret, true);
            if (!ret.result)
            {
                return null;
            }
            if (Count != DBNull.Value) ret.tag = Convert.ToInt32(Count);
            List<Object> result = new List<Object>();

            for (int i = 0; i < DT.Rows.Count; i++)
            {
                Object resObj = GetInstance<Object>(obj);

                DataTable table = new DataTable("");
                foreach (var field in fields)
                {
                    string field_name = GetFieldName(field.Name);
                    var x = DT.Rows[i][field_name];

                    PropertyInfo prop = typeCLass.GetProperty(field_name, BindingFlags.Public | BindingFlags.Instance);
                    if (null != prop && prop.CanWrite && x != DBNull.Value)
                    {
                        prop.SetValue(resObj, x, null);
                    }

                }
                result.Add(resObj);
            }
            return result;

        }

        public T GetInstance<T>(Object obj)
        {
            Type typeCLass = obj.GetType();
            return (T)Activator.CreateInstance(typeCLass);
        }

        public string GetFieldName(string name)
        {
            return name.Replace(">", "").Replace("<", "").Replace("k__BackingField", "");
        }
        public string GetKodSuppByNzp(IDbConnection conn_db, int nzp_supp)
        {
            string sql = "SELECT kod_supp from " + Points.Pref + sKernelAliasRest + "supplier where nzp_supp=" + nzp_supp;
            DataTable DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            if (DT.Rows.Count == 0) return "";
            return DT.Rows[0]["kod_supp"].ToString();
        }
        public int GetNumFilesOnMonth(IDbConnection conn_db, string TableName)
        {
            var dat_s = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); //начало  тек. месяца
            var dat_po = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1); //конец пред.расчетного месяца
            string sql = "SELECT max(num_file) as num from " + TableName + " where date_unload " +
            " between mdy(" + dat_s.Month + "," + dat_s.Day + "," + dat_s.Year + ") and mdy(" + dat_po.Month + "," + dat_po.Day + "," + dat_po.Year + ")";

            DataTable DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            if (DT.Rows.Count == 0) return 0;
            if (DT.Rows[0]["num"] == DBNull.Value) return 0;
            return Convert.ToInt32(DT.Rows[0]["num"]);
        }
        public int GetCurVersion(IDbConnection conn_db)
        {

            DataTable DT = ClassDBUtils.OpenSQL("SELECT max(iversion) as version from " + ex_supp_versions, conn_db).resultData;
            if (DT.Rows.Count == 0) return 0;
            if (DT.Rows[0]["version"] == DBNull.Value) return 0;
            return Convert.ToInt32(DT.Rows[0]["version"]);
        }

        public string GetSuppNameByNzp(IDbConnection conn_db, int nzp_supp)
        {
            DataTable DT = ClassDBUtils.OpenSQL("SELECT max(name_supp) as name_supp from " + Points.Pref + sKernelAliasRest + "supplier where nzp_supp=" + nzp_supp, conn_db).resultData;
            if (DT.Rows.Count == 0) return "";
            if (DT.Rows[0]["name_supp"] == DBNull.Value) return "";
            return DT.Rows[0]["name_supp"].ToString().Trim();
        }

        public Returns UpdateValue(IDbConnection conn_db, string Name, object Value, string TableName, int ID)
        {
            Returns ret = Utils.InitReturns();
            ret = ExecSQL(conn_db, "UPDATE " + TableName + " SET " + Name + "=" + Value.ToString() + " WHERE nzp_reestr=" + ID, true);
            return ret;
        }

        public string GetUserNameByNzp(IDbConnection conn_db, int nzp_user)
        {
            DataTable DT = ClassDBUtils.OpenSQL("SELECT max(name) as user_name from " + Points.Pref + sDataAliasRest + "users where nzp_user=" + nzp_user, conn_db).resultData;
            if (DT.Rows.Count == 0) return "";
            if (DT.Rows[0]["user_name"] == DBNull.Value) return "";
            return DT.Rows[0]["user_name"].ToString().Trim();
        }
        public string GetVersionByNzp(IDbConnection conn_db, int iversion)
        {
            DataTable DT = ClassDBUtils.OpenSQL("SELECT version from " + ex_supp_versions + " where iversion=" + iversion, conn_db).resultData;
            if (DT.Rows.Count == 0) return "";
            if (DT.Rows[0]["version"] == DBNull.Value) return "";
            return DT.Rows[0]["version"].ToString().Trim();
        }

        /// <summary>
        /// Выгрузка: файл синхронизации
        /// </summary>
        /// <returns></returns>
        /// 
        public Returns FileSyncLS(ExFinder finder)
        {

            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.result = false;
                return ret;
            }
            //путь, по которому скачивается файл
            var dir = "";
            string FileName = "";

#warning reference on CLI.REPORT
            ExcelRepClient exc = new ExcelRepClient();
            StringBuilder sql = new StringBuilder();

            //запись в БД о постановки в поток(статус 0)
            ret = exc.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка ЛС и адресов",
                is_shared = 1
            });
            if (!ret.result) return ret;

            int nzpExc = ret.tag;

            IDbConnection conn_db = null;

            string dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало  тек.расчетного месяца


            try
            {
                string conn_kernel = Points.GetConnByPref(Points.Pref);
                conn_db = DBManager.newDbConnection(conn_kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    exc.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                    conn_db.Close();
                    return ret;
                }


                #region определение локального пользователя
                int nzpUser = finder.nzp_user;

                /*DbWorkUser db = new DbWorkUser();
                Finder f_user = new Finder();
                f_user.nzp_user = finder.nzp_user;
                int nzpUser = db.GetLocalUser(conn_db, f_user, out ret);
                db.Close();
                if (!ret.result)
                {
                    return ret;
                }*/
                #endregion

                List<string> prefs = new List<string>();
                foreach (var point in Points.PointList)
                {
                    for (int i = 0; i < finder.ListNzpWp.Count; i++)
                    {
                        if (point.nzp_wp == finder.ListNzpWp[i])
                        {
                            prefs.Add(point.pref);
                        }
                    }
                }

                string nzp_wp = "";
                if (finder.ListNzpWp.Count > 0) nzp_wp = "and k.nzp_wp in (";
                for (int i = 0; i < finder.ListNzpWp.Count; i++)
                {
                    nzp_wp += finder.ListNzpWp[i] + (i != finder.ListNzpWp.Count - 1 ? ", " : ")");
                }

                ReestrExSuppSyncLs Reestr = new ReestrExSuppSyncLs();
                Reestr.kod_supp = finder.nzp_supp.ToString();//GetKodSuppByNzp(conn_db, finder.nzp_supp).Trim();
                Reestr.num_file = GetNumFilesOnMonth(conn_db, reestr_ex_supp_sync_ls) + 1;
                Reestr.version = GetCurVersion(conn_db);
                Reestr.file_name = "ls_" + Reestr.kod_supp + "_" + DateTime.Now.ToString("yyyyMM") + "_" + Reestr.version.ToString("00") + "_" + Reestr.num_file.ToString("000") + ".txt";
                Reestr.date_unload = DateTime.Now;
                Reestr.nzp_user = nzpUser;
                Reestr.saldo_date = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1); //начало  тек.расчетного месяца
                Reestr.status = (int)StatusUnload.InProcess;
                Reestr.nzp_supp = finder.nzp_supp;
                Reestr.nzp_file_link = nzpExc;

                Reestr.nzp_reestr = InsertRow(conn_db, Reestr, reestr_ex_supp_sync_ls).tag;
                NzpReestr = Reestr.nzp_reestr;


                //получаем ПСС
                sql = new StringBuilder();
                string temp_pss = "temp_pss" + nzpUser;

                ExecSQL(conn_db, "drop table " + temp_pss, false);
                ret = ExecSQL(conn_db,
                    "create temp table " + temp_pss + " (nzp_kvar integer, val_prm numeric(20,0)) " + sUnlogTempTable,
                    true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + temp_pss + "_1 on " + temp_pss + "(nzp_kvar, val_prm)", true);

                foreach (var point in prefs)
                {
                    sql = new StringBuilder();
                    sql.Append("insert into " + temp_pss + " (nzp_kvar, val_prm) ");
                    sql.Append(" select distinct p1.nzp, cast(replace(p1.val_prm,' ','') as " + sDecimalType + ") from " +
                              point + "_data" + tableDelimiter + "prm_15 p1 ");
                    sql.Append(" where p1.is_actual <> 100 and " + dat_s + " between dat_s and dat_po ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + temp_pss, false);

                }


                //получаем номер ЛС поставщика
                sql = new StringBuilder();
                string temp_supp_ls = "temp_supp_ls" + nzpUser;

                ExecSQL(conn_db, "drop table " + temp_supp_ls, false);
                ret = ExecSQL(conn_db,
                    "create temp table " + temp_supp_ls + " (nzp_kvar integer, num_ls numeric(20,0)) " + sUnlogTempTable,
                    true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + temp_supp_ls + "_1 on " + temp_supp_ls + "(nzp_kvar, num_ls)", true);

                foreach (var point in prefs)
                {
                    sql = new StringBuilder();
                    sql.Append("insert into " + temp_supp_ls + " (nzp_kvar, num_ls) ");
                    sql.Append(" select distinct p1.nzp_kvar, cast(replace(p1.numls,' ','') as " + sDecimalType + ") from " +
                              point + "_data" + tableDelimiter + "alias_ls p1 ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + temp_supp_ls, false);

                }

                sql = new StringBuilder();
                sql.Append("  select k.pkod, p.val_prm as pss, max(n.num_ls) as supp_num_ls,   max(trim(a.town)) as town,  ");
                sql.Append(" max((case when trim(" + sNvlWord + "(g.rajon,''))='-' then ' ' else trim(" + sNvlWord + "(g.rajon,'')) end)) as rajon, ");
                sql.Append(" max(trim(" + sNvlWord + "(u.ulica,''))) as ulica, max(trim(" + sNvlWord + "(d.ndom,''))) as dom, max((case when trim(" + sNvlWord + "(d.nkor,''))='-' then '' else ' '|| ");
                sql.Append(" trim(" + sNvlWord + "(d.nkor,'')) end)) nkor, max(k.nkvar) nkvar, max(k.nkvar_n) nkvar_n, k.fio");
                sql.Append(" from " + Points.Pref + sDataAliasRest + "kvar k ");
                sql.Append(" left outer join " + temp_pss + " p on k.nzp_kvar=p.nzp_kvar ");
                sql.Append(" left outer join " + temp_supp_ls + " n on k.nzp_kvar=n.nzp_kvar ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "dom d  ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_ulica u  ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_rajon g ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_town a ");
                sql.Append(" on g.nzp_town = a.nzp_town  ");
                sql.Append(" on u.nzp_raj  = g.nzp_raj ");
                sql.Append(" on d.nzp_ul  = u.nzp_ul ");
                sql.Append(" on k.nzp_dom = d.nzp_dom ");
                sql.Append(" where k.is_open=1 and k.pkod>0 and k.pkod is not null " + nzp_wp);
                sql.Append(" group by pkod,p.val_prm,k.fio ");
                sql.Append(" order by pkod");

                var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                if (DT.resultCode == -1)
                {
                    MonitorLog.WriteLog("Ошибка получения данных в функции FileSyncLS:\n" + DT.resultMessage + "\n " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                if (InputOutput.useFtp)
                {
                    dir = InputOutput.GetInputDir();
                }
                else
                {
                    dir = Constants.ExcelDir;
                }
                FileStream memstr = new FileStream(Path.Combine(dir, Reestr.file_name), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                StreamWriter writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                try
                {
                    string s = "";

                    for (int j = 0; j < DT.resultData.Rows.Count; j++)
                    {
                        s = (DT.resultData.Rows[j]["pkod"] != DBNull.Value ? ((Decimal)DT.resultData.Rows[j]["pkod"]).ToString("0").Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["pss"] != DBNull.Value ? ((Decimal)DT.resultData.Rows[j]["pss"]).ToString("0").Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["supp_num_ls"] != DBNull.Value ? ((int)DT.resultData.Rows[j]["supp_num_ls"]).ToString("0").Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["town"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["town"]).Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["rajon"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["rajon"]).Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["ulica"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["ulica"]).Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["dom"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["dom"]).Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["nkor"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["nkor"]).Trim() + "|" : "|") + "|" + //литера
                        (DT.resultData.Rows[j]["nkvar"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["nkvar"]).Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["nkvar_n"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["nkvar_n"]).Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["fio"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["fio"]).Trim() : "");


                        writer.WriteLine(s);
                        s = "";
                        if (j % 100 == 0) exc.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal)j) / DT.resultData.Rows.Count });
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в функции FileSyncLS при записи в файл:\n" + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }

                ret = UpdateValue(conn_db, "count_rows", DT.resultData.Rows.Count, reestr_ex_supp_sync_ls, NzpReestr);
                writer.Flush();
                writer.Close();
                memstr.Close();
                DT = null;
                FileName = Reestr.file_name;

                dir = Path.Combine(InputOutput.GetInputDir(), FileName);
                //перенос  на ftp сервер
                dir = InputOutput.SaveOutputFile(dir);

            }
            catch (Exception ex)
            {

                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции FileSyncLS:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (ret.result)
                {
                    ret = UpdateValue(conn_db, "status", ((int)StatusUnload.Success), reestr_ex_supp_sync_ls, NzpReestr);
                    exc.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    exc.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = dir });

                }
                else
                {
                    ret = UpdateValue(conn_db, "status", ((int)StatusUnload.Fail), reestr_ex_supp_sync_ls, NzpReestr);
                    exc.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                exc.Close();

                if (conn_db != null) conn_db.Close();
            }

            return ret;
        }
        /// <summary>
        /// Pager для реестра файлов синхронизации
        /// </summary>
        /// <param name="rows">число строк</param>
        /// <param name="skip">пропустить</param>
        /// <param name="ret">Returns</param>
        /// <returns></returns>
        public List<IReestrExSuppSyncLs> GetReestrSyncLs(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "\n Не определен пользователь";
                return null;
            }

            IDbConnection conn_db;
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            conn_db = DBManager.newDbConnection(conn_kernel);

            if (!OpenDb(conn_db, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            string TableName = reestr_ex_supp_sync_ls;
            IReestrExSuppSyncLs obj = new IReestrExSuppSyncLs();
            List<IReestrExSuppSyncLs> res = ReestrPager(conn_db, obj, TableName, finder.rows, finder.skip, out ret, "nzp_reestr").Cast<IReestrExSuppSyncLs>().ToList();
            for (int i = 0; i < res.Count(); i++)
            {
                res[i].name_supp = GetSuppNameByNzp(conn_db, res[i].nzp_supp);
                res[i].getNameStatusFileSync(res[i].status);
                res[i].user_name = GetUserNameByNzp(conn_db, res[i].nzp_user);
                res[i].version_name = GetVersionByNzp(conn_db, res[i].version);
            }
            return res;
        }



        /// <summary>
        /// Выгрузка: файл изменений ЛС
        /// </summary>
        /// <returns></returns>
        /// 
        public Returns FileChangeLS(ExFinder finder)
        {

            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.result = false;
                return ret;
            }
            //путь, по которому скачивается файл
            var dir = "";
            string FileName = "";

#warning reference on CLI.REPORT
            ExcelRepClient exc = new ExcelRepClient();
            StringBuilder sql = new StringBuilder();

            //запись в БД о постановки в поток(статус 0)
            ret = exc.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка изменений характеристик ЛС c " + finder.date_begin,
                is_shared = 1
            });
            if (!ret.result) return ret;

            int nzpExc = ret.tag;

            IDbConnection conn_db = null;

            string dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало  тек.расчетного месяца


            try
            {
                string conn_kernel = Points.GetConnByPref(Points.Pref);
                conn_db = DBManager.newDbConnection(conn_kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    exc.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                    conn_db.Close();
                    return ret;
                }


                #region определение локального пользователя
                int nzpUser = finder.nzp_user;

                /*DbWorkUser db = new DbWorkUser();
                Finder f_user = new Finder();
                f_user.nzp_user = finder.nzp_user;
                int nzpUser = db.GetLocalUser(conn_db, f_user, out ret);
                db.Close();
                if (!ret.result)
                {
                    return ret;
                }*/
                #endregion

                List<string> prefs = new List<string>();
                foreach (var point in Points.PointList)
                {
                    for (int i = 0; i < finder.ListNzpWp.Count; i++)
                    {
                        if (point.nzp_wp == finder.ListNzpWp[i])
                        {
                            prefs.Add(point.pref);
                        }
                    }
                }

                string nzp_wp = "";
                if (finder.ListNzpWp.Count > 0) nzp_wp = "and k.nzp_wp in (";
                for (int i = 0; i < finder.ListNzpWp.Count; i++)
                {
                    nzp_wp += finder.ListNzpWp[i] + (i != finder.ListNzpWp.Count - 1 ? ", " : ")");
                }

                ReestrExSuppChangeLs Reestr = new ReestrExSuppChangeLs();
                Reestr.kod_supp = finder.nzp_supp.ToString();//GetKodSuppByNzp(conn_db, finder.nzp_supp).Trim();
                Reestr.num_file = GetNumFilesOnMonth(conn_db, reestr_ex_supp_change_ls) + 1;
                Reestr.version = GetCurVersion(conn_db);
                Reestr.file_name = "prm" + Reestr.kod_supp + "_" + finder.date_begin.Replace(".", "") + "_" + Reestr.version.ToString("00") + "_" + Reestr.num_file.ToString("000") + ".txt";
                Reestr.date_unload = DateTime.Now;
                Reestr.nzp_user = nzpUser;
                Reestr.dat_s = Convert.ToDateTime(finder.date_begin);
                Reestr.status = (int)StatusUnload.InProcess;
                Reestr.nzp_supp = finder.nzp_supp;
                Reestr.nzp_file_link = nzpExc;

                Reestr.nzp_reestr = InsertRow(conn_db, Reestr, reestr_ex_supp_change_ls).tag;
                NzpReestr = Reestr.nzp_reestr;

                #region временная табличка
                sql = new StringBuilder();
                string temp_change_ls = "temp_change_ls" + nzpUser;
                ExecSQL(conn_db, "drop table " + temp_change_ls, false);
                ret = ExecSQL(conn_db,
               "create temp table " + temp_change_ls + " (nzp_kvar integer,pkod numeric(13,0), pss numeric(15,0),num_ls numeric (20,0)" +
               " ,adr char(200),ob_pl numeric(15,2), ot_pl numeric(15,2),kol_gil integer,pu char(500)) " + sUnlogTempTable,
               true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + temp_change_ls + "_1 on " + temp_change_ls + "(nzp_kvar,pkod)", true);

                #endregion

                #region получаем ПСС
                sql = new StringBuilder();
                string temp_pss = "temp_pss_change" + nzpUser;

                ExecSQL(conn_db, "drop table " + temp_pss, false);
                ret = ExecSQL(conn_db,
                    "create temp table " + temp_pss + " (nzp_kvar integer, val_prm numeric(20,0)) " + sUnlogTempTable,
                    true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + temp_pss + "_1 on " + temp_pss + "(nzp_kvar, val_prm)", true);

                foreach (var point in prefs)
                {
                    sql = new StringBuilder();
                    sql.Append("insert into " + temp_pss + " (nzp_kvar, val_prm) ");
                    sql.Append(" select distinct p1.nzp, cast(replace(p1.val_prm,' ','') as " + sDecimalType + ") from " +
                              point + "_data" + tableDelimiter + "prm_15 p1 ");
                    sql.Append(" where p1.is_actual <> 100 and " + dat_s + " between dat_s and dat_po ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + temp_pss, false);

                }
                #endregion

                #region получаем номер ЛС поставщика
                sql = new StringBuilder();
                string temp_supp_ls = "temp_supp_ls_change" + nzpUser;

                ExecSQL(conn_db, "drop table " + temp_supp_ls, false);
                ret = ExecSQL(conn_db,
                    "create temp table " + temp_supp_ls + " (nzp_kvar integer, num_ls numeric(20,0)) " + sUnlogTempTable,
                    true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + temp_supp_ls + "_1 on " + temp_supp_ls + "(nzp_kvar, num_ls)", true);

                foreach (var point in prefs)
                {
                    sql = new StringBuilder();
                    sql.Append("insert into " + temp_supp_ls + " (nzp_kvar, num_ls) ");
                    sql.Append(" select distinct p1.nzp_kvar, cast(replace(p1.numls,' ','') as " + sDecimalType + ") from " +
                              point + "_data" + tableDelimiter + "alias_ls p1 ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + temp_supp_ls, false);

                }

                #endregion

                sql = new StringBuilder();
                sql.Append(" INSERT INTO " + temp_change_ls + "(nzp_kvar,pkod,pss,num_ls,adr)");
                sql.Append(" select k.nzp_kvar, k.pkod, p.val_prm as pss, max(n.num_ls) as num_ls, max(trim(a.town)||','||(case when trim(" + sNvlWord + "(g.rajon,''))='-' then '' else trim(" + sNvlWord + "(g.rajon,''))||',' end) ");
                sql.Append(" ||trim(" + sNvlWord + "(u.ulica,''))||','||trim(" + sNvlWord + "(d.ndom,''))||','||(case when trim(" + sNvlWord + "(d.nkor,''))='-' then '' else ");
                sql.Append(" trim(" + sNvlWord + "(d.nkor,''))||',' end)||trim(" + sNvlWord + "(k.nkvar,''))||','|| trim(" + sNvlWord + "(k.nkvar_n,''))) as adr");
                sql.Append(" from " + Points.Pref + sDataAliasRest + "kvar k ");
                sql.Append(" left outer join " + temp_pss + " p on k.nzp_kvar=p.nzp_kvar ");
                sql.Append(" left outer join " + temp_supp_ls + " n on k.nzp_kvar=n.nzp_kvar ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "dom d ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_ulica u  ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_rajon g ");
                sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_town a ");
                sql.Append(" on g.nzp_town = a.nzp_town  ");
                sql.Append(" on u.nzp_raj  = g.nzp_raj ");
                sql.Append(" on d.nzp_ul  = u.nzp_ul ");
                sql.Append(" on k.nzp_dom = d.nzp_dom ");
                sql.Append(" where k.is_open=" + (int)Ls.States.Open + " and k.pkod>0 and k.pkod is not null " + nzp_wp);
                sql.Append(" group by k.nzp_kvar,pkod,p.val_prm");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, sUpdStat + " " + temp_change_ls, false);

                #region получаем измененные параметры ЛС
                sql = new StringBuilder();
                string temp_changed_param = "temp_changed_param" + nzpUser;

                ExecSQL(conn_db, "drop table " + temp_changed_param, false);
                ret = ExecSQL(conn_db,
                    "create temp table " + temp_changed_param + " (nzp_kvar integer, nzp_prm integer, val_prm char(20)) " + sUnlogTempTable,
                    true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + temp_changed_param + "_1 on " + temp_changed_param + "(nzp_kvar, nzp_prm)", true);

                //получаем изменненые параметры
                foreach (var point in prefs)
                {
                    sql = new StringBuilder();
                    sql.Append(" insert into " + temp_changed_param + " (nzp_kvar, nzp_prm,val_prm ) ");
                    sql.Append(" select distinct p1.nzp, nzp_prm, max(val_prm) from  " + point + "_data" + tableDelimiter + "prm_1 p1 ");
                    sql.Append(" where p1.is_actual<>100 and p1.nzp_prm in (4,5,10,131,133) and p1.dat_when>='" + finder.date_begin + "' and " + sCurDate + " between p1.dat_s and p1.dat_po");
                    sql.Append(" group by p1.nzp,p1.nzp_prm ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + temp_changed_param, false);
                }

                //получаем суммарное кол-во жильцов по лс с измененными параметрами 5,10,131
                foreach (var point in prefs)
                {
                    sql = new StringBuilder();
                    sql.Append(" UPDATE " + temp_changed_param + " SET (val_prm)= ");
                    sql.Append(" ((SELECT sum(cast(p1.val_prm as integer)) as sum_kol_gil FROM " + point + "_data" + tableDelimiter + "prm_1 p1 WHERE p1.is_actual<>100 and " + sCurDate + " between p1.dat_s and p1.dat_po ");
                    sql.Append(" and " + temp_changed_param + ".nzp_kvar=p1.nzp and nzp_prm in (5,10,131))) WHERE nzp_prm=5 ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + temp_changed_param, false);
                }
                //убираем не нужные теперь параметры кол-ва жильцов:131,10
                ret = ExecSQL(conn_db, "DELETE FROM " + temp_changed_param + " WHERE nzp_prm in (10,131)", true);
                if (!ret.result) return ret;

                #endregion

                //пишем параметры в общую таблицу выборки
                sql = new StringBuilder();
                sql.Append(" UPDATE " + temp_change_ls + " SET (ob_pl,ot_pl,kol_gil)=");
                sql.Append(" ((SELECT max(case when nzp_prm=4 then val_prm end) as ob_pl,");
                sql.Append("  max(case when nzp_prm=133 then val_prm end) as ot_pl,");
                sql.Append("  max(case when nzp_prm=5 then val_prm end) as kol_gil ");
                sql.Append(" FROM " + temp_changed_param + " prm WHERE " + temp_change_ls + ".nzp_kvar=prm.nzp_kvar))");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;



                #region Приборы учета
                sql = new StringBuilder();
                string temp_changed_pu = "temp_changed_pu" + nzpUser;

                ExecSQL(conn_db, "drop table " + temp_changed_pu, false);
                ret = ExecSQL(conn_db,
                    "create temp table " + temp_changed_pu + " (nzp_kvar integer, nzp_counter integer, num_cnt char(20), val_cnt float, dat_uchet date) " + sUnlogTempTable,
                    true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, " create index ix_" + temp_changed_pu + "_1 on " + temp_changed_pu + "(nzp_kvar, nzp_counter)", true);

                //получаем измененные показания ПУ
                foreach (var point in prefs)
                {
                    sql = new StringBuilder();
                    sql.Append(" insert into " + temp_changed_pu + " (nzp_kvar,nzp_counter,val_cnt,dat_uchet,num_cnt) ");
                    sql.Append(" select distinct s.nzp,c.nzp_counter, max(c.val_cnt),c.dat_uchet,c.num_cnt ");
                    sql.Append(" from " + point + "_data" + tableDelimiter + "counters c, " + point + "_data:counters_spis s ");
                    sql.Append(" where c.dat_close is null and c.is_actual<>100 and c.dat_when>='" + finder.date_begin + "'");
                    sql.Append(" and c.dat_uchet=(select max(c2.dat_uchet) from " + point + "_data" + tableDelimiter + "counters c2 where c2.nzp_counter=c.nzp_counter and c2.is_actual<>100)");
                    sql.Append(" and c.nzp_counter=s.nzp_counter and s.dat_close is null and s.is_actual<>100 and s.nzp_type in (3,4) ");
                    sql.Append(" group by s.nzp,c.nzp_counter,c.dat_uchet,c.num_cnt");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                    ExecSQL(conn_db, sUpdStat + " " + temp_changed_pu, false);
                }


                sql = new StringBuilder();


                //нумерация ПУ по ЛС
                string temp_changed_pu_vals = "temp_changed_pu_vals" + nzpUser;
                ExecSQL(conn_db, "drop table " + temp_changed_pu_vals, false);
                sql.Append(" select  (select count(*) from " + temp_changed_pu + " b where a.nzp_kvar =b.nzp_kvar  and a.nzp_counter<= b.nzp_counter) as num,  a.* ");
                sql.Append(" from " + temp_changed_pu + " a   order by a.nzp_kvar,1  asc ");
                sql.Append(" into temp " + temp_changed_pu_vals + "; ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;

                ExecSQL(conn_db, " create index ix_" + temp_changed_pu_vals + "_1 on " + temp_changed_pu_vals + "(nzp_kvar, nzp_counter)", true);

                //пишем все ПУ в строку в финальную выборку
                sql = new StringBuilder();
                sql.Append(" update " + temp_change_ls + " ");
                sql.Append(" set(pu)= ");
                sql.Append(" ((select   ");
                sql.Append(" max(case when num=1 then trim(" + sNvlWord + "(t.num_cnt,''))||','||t.val_cnt||','||t.dat_uchet||';' else '' end) || ");
                sql.Append(" max(case when num=2 then trim(" + sNvlWord + "(t.num_cnt,''))||','||t.val_cnt||','||t.dat_uchet||';' else '' end) || ");
                sql.Append(" max(case when num=3 then trim(" + sNvlWord + "(t.num_cnt,''))||','||t.val_cnt||','||t.dat_uchet||';' else '' end) || ");
                sql.Append(" max(case when num=4 then trim(" + sNvlWord + "(t.num_cnt,''))||','||t.val_cnt||','||t.dat_uchet||';' else '' end) || ");
                sql.Append(" max(case when num=5 then trim(" + sNvlWord + "(t.num_cnt,''))||','||t.val_cnt||','||t.dat_uchet||';' else '' end) || ");
                sql.Append(" max(case when num=6 then trim(" + sNvlWord + "(t.num_cnt,''))||','||t.val_cnt||','||t.dat_uchet||';' else '' end)    ");
                sql.Append(" from " + temp_changed_pu_vals + " t where " + temp_change_ls + ".nzp_kvar=t.nzp_kvar group by t.nzp_kvar))  ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;

                #endregion

                //удаляем ЛС по которым не было изменений
                ret = ExecSQL(conn_db, "DELETE FROM " + temp_change_ls + " WHERE (ob_pl is null and ot_pl is null and kol_gil is null and pu is null)", true);
                if (!ret.result) return ret;


                var DT = ClassDBUtils.OpenSQL("SELECT * FROM " + temp_change_ls, conn_db);
                if (DT.resultCode == -1)
                {
                    MonitorLog.WriteLog("Ошибка получения данных в функции FileChangeLS:\n" + DT.resultMessage + "\n " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                if (InputOutput.useFtp)
                {
                    dir = InputOutput.GetInputDir();
                }
                else
                {
                    dir = Constants.ExcelDir;
                }
                FileStream memstr = new FileStream(Path.Combine(dir, Reestr.file_name), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                StreamWriter writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));
                int err = 0;
                try
                {
                    string s = "";

                    for (int j = 0; j < DT.resultData.Rows.Count; j++)
                    {
                        err++;
                        s = (DT.resultData.Rows[j]["pkod"] != DBNull.Value ? ((Decimal)DT.resultData.Rows[j]["pkod"]).ToString("0").Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["pss"] != DBNull.Value ? ((Decimal)DT.resultData.Rows[j]["pss"]).ToString("0").Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["num_ls"] != DBNull.Value ? ((int)DT.resultData.Rows[j]["num_ls"]).ToString("0").Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["adr"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["adr"]).Trim() + "|" : "|") +
                        (DT.resultData.Rows[j]["ob_pl"] != DBNull.Value ? ((Decimal)DT.resultData.Rows[j]["ob_pl"]) + "|" : "|") +
                        (DT.resultData.Rows[j]["ot_pl"] != DBNull.Value ? ((Decimal)DT.resultData.Rows[j]["ot_pl"]) + "|" : "|") +
                        (DT.resultData.Rows[j]["kol_gil"] != DBNull.Value ? ((int)DT.resultData.Rows[j]["kol_gil"]) + "|" : "|") +
                        (DT.resultData.Rows[j]["pu"] != DBNull.Value ? ((string)DT.resultData.Rows[j]["pu"]) + "|" : "|");
                        writer.WriteLine(s);
                        s = "";
                        if (j % 100 == 0) exc.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal)j) / DT.resultData.Rows.Count });
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в функции FileChangeLS при записи в файл:\n" + ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }

                ret = UpdateValue(conn_db, "count_rows", DT.resultData.Rows.Count, reestr_ex_supp_change_ls, NzpReestr);
                writer.Flush();
                writer.Close();
                memstr.Close();
                DT = null;
                FileName = Reestr.file_name;

                dir = Path.Combine(InputOutput.GetInputDir(), FileName);
                //перенос  на ftp сервер
                dir = InputOutput.SaveOutputFile(dir);

            }
            catch (Exception ex)
            {

                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции FileChangeLS:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (ret.result)
                {
                    ret = UpdateValue(conn_db, "status", ((int)StatusUnload.Success), reestr_ex_supp_change_ls, NzpReestr);
                    exc.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    exc.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = dir });

                }
                else
                {
                    ret = UpdateValue(conn_db, "status", ((int)StatusUnload.Fail), reestr_ex_supp_change_ls, NzpReestr);
                    exc.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                exc.Close();

                if (conn_db != null) conn_db.Close();
            }

            return ret;
        }


        /// <summary>
        /// Pager для реестра файлов изменений ЛС
        /// </summary>
        /// <param name="rows">число строк</param>
        /// <param name="skip">пропустить</param>
        /// <param name="ret">Returns</param>
        /// <returns></returns>
        public List<IReestrExSuppChangeLs> GetReestrChangeLs(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "\n Не определен пользователь";
                return null;
            }

            IDbConnection conn_db;
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            conn_db = DBManager.newDbConnection(conn_kernel);

            if (!OpenDb(conn_db, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            string TableName = reestr_ex_supp_change_ls;
            IReestrExSuppChangeLs obj = new IReestrExSuppChangeLs();
            List<IReestrExSuppChangeLs> res = ReestrPager(conn_db, obj, TableName, finder.rows, finder.skip, out ret, "nzp_reestr").Cast<IReestrExSuppChangeLs>().ToList();
            for (int i = 0; i < res.Count(); i++)
            {
                res[i].name_supp = GetSuppNameByNzp(conn_db, res[i].nzp_supp);
                res[i].getNameStatusChangeLs(res[i].status);
                res[i].user_name = GetUserNameByNzp(conn_db, res[i].nzp_user);
                res[i].version_name = GetVersionByNzp(conn_db, res[i].version);
            }
            return res;
        }


        /// <summary>
        /// Удаление реестров
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns DeleteReestrRow(ExFinder finder)
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
            string Table = "";
            switch (finder.activeTab)
            {
                case 0:
                    {

                    } break;
                case 1:
                    {

                    } break;
                case 2:
                    {
                        Table = reestr_ex_supp_sync_ls;
                    } break;
                case 3:
                    {
                        Table = reestr_ex_supp_change_ls;
                    } break;

            }
            sql.Append(" DELETE FROM " + Table + " WHERE nzp_reestr=" + finder.ReestrID + "");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при удалении записи: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при удалении записи";
                return ret;
            }

            return ret;
        }
        #endregion


        #region Обмен с соцзащитой
        /// <summary> Обработка файла загруженный от соц.защиты </summary>
        /// <param name="finder">Параметр с базовой информацией</param>
        /// <param name="fileName">Наименование загружаемого файла</param>
        /// <param name="fileNameFull">Полный путь к файлу</param>
        /// <param name="encodingValue">Кодировка символов</param>
        /// <param name="listWP">Идентификатор банка данных</param>
        /// <returns>Параметр с результатом выполнение функции</returns>
        public Returns GetUploadExchangeSZ(Finder finder, string fileName, string fileNameFull, string encodingValue, List<int> listWP)
        {
            Returns ret = Utils.InitReturns();
            int gExSz = -1;

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return ret;
            }

            #region объявление переменных
            IDbConnection connDB = null;
            string prefData = Points.Pref + DBManager.sDataAliasRest;

            #endregion

            #region  получение префиксов


            IDictionary<int, string> listPref = new Dictionary<int, string>();
            foreach (var point in Points.PointList)
            {
                if (listWP.Contains(point.nzp_wp))
                {
                    listPref.Add(point.nzp_wp, point.pref);
                }
            }

            #endregion

            try
            {
                #region соединение с БД

                connDB = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(connDB, true);
                if (!ret.result) return ret;

                #endregion

                #region заносим запись в таблицу файлов

                string sql = " INSERT INTO " + prefData + "tula_ex_sz (file_name,dat_upload, nzp_user) " +
                             " VALUES ('" + fileName + "', " + DBManager.sCurDateTime + "," + finder.nzp_user + ")";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    MonitorLog.WriteLog("GetUploadExchangeSZ: Ошибка записи в таблицу tula_ex_sz, sql: " + sql, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка записи в реестр загрузок";
                    ret.result = false;
                    return ret;
                }
                int nzpExSZ = gExSz = GetSerialValue(connDB); //получили номер реестра для загрузки

                foreach (var wp in listWP)
                {
                    sql = " INSERT INTO " + prefData + "tula_ex_sz_wp(nzp_ex_sz, nzp_wp) " +
                          " VALUES (" + nzpExSZ + "," + wp + ") ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }

                #endregion

                #region Загрузка DBF файла во временную таблицу

                var dbUtils = new DBUtils();
                try
                {
                    if (!string.IsNullOrEmpty(fileNameFull))
                    {
                        string localfileName = Path.Combine(Constants.FilesDir, fileName);
                        if (File.Exists(localfileName))
                        {
                            DateTime now = DateTime.Now;
                            localfileName = localfileName.ToLower().TrimEnd('p', 'i', 'z', '.') +
                                            now.Year + now.Month + now.Day +
                                            now.Hour + now.Minute + now.Second + now.Millisecond + ".zip";
                        }
                        if (InputOutput.useFtp)
                        {
                            if (!InputOutput.DownloadFile(fileNameFull, localfileName))
                                throw new Exception("Не удалось загрузить файл с сервера");
                        }
                        else localfileName = fileNameFull;

                        DataTable dt;
                        using (var fs = new FileStream(localfileName, FileMode.Open, FileAccess.Read) { Position = 0 })
                        {
                            dt = Utils.ConvertDBFtoDataTable(fs, encodingValue, out ret, fileName);
                            if (!ret.result) throw new Exception(ret.text);
                        }
                        if (dt.Rows.Count == 0) throw new Exception("Не обнаружено записей в файле");

                        Utils.setCulture();
                        ret = dbUtils.SaveDataTable(connDB, finder, "exchange_sz", dt);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                    else throw new Exception("Не определен полный путь к файлу");
                }
                catch (Exception ex)
                {
                    throw new Exception("Загрузка файла прошла с ошибкой. " + ex.Message);
                }
                finally
                {
                    dbUtils.Close();
                }

                #endregion

                //получаем имя таблички на web
                string nameTable = DBManager.GetFullBaseName(connDB) + DBManager.tableDelimiter + "t" + finder.nzp_user + "_exchange_sz";

                string sqlServ = string.Empty;

                for (int i = 1; i <= 15; i++)
                    sqlServ += string.Format(" gku{0}, tarif{0}, sum{0}, fakt{0}, norm{0}, sumz{0}, klmz{0}, ozs{0}, sumozs{0}, org{0}, vidtar{0}, koef{0}, lchet{0}, ", i);

                //переносим даннные из таблички с web
                sql = " INSERT INTO " + prefData + "tula_ex_sz_file " +
                      " ( sl, famil, imja, otch, drog, strahnm, nasp, nylic, ndom, nkorp, nkw, nkomn, kolk, lchet, " +
                        " vidgf, privat, opl, otpl, oplj, otplj, kolzr, kolzrp, kolpr, prz, prn, prk, " + sqlServ + " nzp_ex_sz) " +
                      " SELECT sl, famil, imja, otch, drog, strahnm, nasp, nylic, ndom, nkorp, nkw, nkomn, " +
                             " kolk, lchet, vidgf, privat, opl, otpl, oplj, otplj, kolzr, kolzrp, kolpr, prz, prn, prk, " + sqlServ + nzpExSZ +
                      " FROM " + nameTable;
                ret = ExecSQL(connDB, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("GetUploadExchangeSZ: Ошибка записи в таблицу tula_ex_sz_file, sql: " + sql, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при записи данных из файла в систему";
                    return ret;
                }

                ExecSQL(connDB, DBManager.sUpdStat + prefData + "tula_ex_sz_file ", false);

                const string tPrm2004 = "t_prm_2004";
                const string tLchetDecimal = "t_lchet_decimal";

                var tulaBD = new[] { 59, 60, 62, 63 }; // банк данных: ТУЛА, Тула1, Тула3, Тула КР
                string oldLS = string.Empty;
                DataTable td;
                if (tulaBD.Contains(listPref.Keys.ToList()[0]))
                {
                    #region Тульские банки данных

                    #region создание временных таблиц

                    //содержит лицевые счета из параметра "Старый лицевой счет"
                    ExecSQL(connDB, "DROP TABLE " + tPrm2004, false);
                    ret = ExecSQL(connDB, "CREATE TEMP TABLE " + tPrm2004 + " (" +
                                            " nzp_kvar INTEGER, " +
                                            " val_prm CHARACTER(20)) " + sUnlogTempTable);
                    if (!ret.result) throw new Exception(ret.text);
                    ExecSQL(connDB, " CREATE INDEX ix_" + tPrm2004 + "_2 ON " + tPrm2004 + "(nzp_kvar, val_prm)");

                    //Содержит лицевые счета, содержащие в загруженном файле
                    ExecSQL(connDB, " DROP TABLE " + tLchetDecimal, false);
                    ret = ExecSQL(connDB, " CREATE TEMP TABLE " + tLchetDecimal + " (" +
                                            " id INTEGER, " +
                                            " sl SMALLINT, " +
                                            " lchet CHARACTER(20)," +
                                            " nzp_kvar INTEGER) " + sUnlogTempTable);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #region заполнение временных таблиц

                    foreach (int nzpWP in listWP)
                    {
                        string localPrefData = listPref[nzpWP] + DBManager.sDataAliasRest;
                        //сопоставляем с номером лс в системе(параметр "Старый лицевой счет") 
                        sql = " INSERT INTO " + tPrm2004 + " (nzp_kvar, val_prm) " +
                              " SELECT DISTINCT p1.nzp, " +
                                     " regexp_replace(TRIM(p1.val_prm),'^0+', '') AS val_prm " +
                              " FROM " + localPrefData + "prm_1 p1 " +
                              " WHERE p1.nzp_prm = 2004 AND p1.is_actual <> 100 ";
                        ret = ExecSQL(connDB, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                    ExecSQL(connDB, sUpdStat + " " + tPrm2004, false);

                    //заносим л/с
                    sql = " INSERT INTO " + tLchetDecimal + " (id,sl,lchet)" +
                          " SELECT id, sl, regexp_replace(TRIM(lchet),'^0+', '') AS lchet  " +
                          " FROM " + prefData + "tula_ex_sz_file " +
                              " WHERE lchet IS NOT NULL " +
                                " AND nzp_kvar IS NULL AND nzp_ex_sz = " + nzpExSZ;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    ExecSQL(connDB, sUpdStat + " " + tLchetDecimal, false);

                    #endregion

                    #region проверка дублирования поля LCHET

                    sql = " SELECT lchet " +
                          " FROM " + tLchetDecimal +
                          " WHERE sl = 1 " +
                          " GROUP BY lchet " +
                          " HAVING COUNT(lchet) > 1 ";

                    oldLS = string.Empty;
                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["lchet"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: дублируется поле LCHET ' - " + oldLS,
                           MonitorLog.typelog.Warn, true);

                    sql = " DELETE FROM " + tLchetDecimal +
                          " WHERE lchet IN ( SELECT lchet " +
                                           " FROM " + tLchetDecimal +
                                           " WHERE sl = 1 " +
                                           " GROUP BY lchet " +
                                           " HAVING COUNT(lchet) > 1 ) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #region проверка дублирования старых лицевых счетов в биллинге

                    sql = " SELECT val_prm " +
                          " FROM " + tPrm2004 +
                          " GROUP BY val_prm " +
                          " HAVING COUNT(nzp_kvar) > 1 ";

                    oldLS = string.Empty;
                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["val_prm"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: дублируется параметр 'Старый лицевой счет' - " + oldLS,
                           MonitorLog.typelog.Warn, true);

                    //удаляем дублирующие л/с
                    sql = " DELETE FROM " + tPrm2004 + " " +
                          " WHERE val_prm IN (SELECT val_prm " +
                                            " FROM " + tPrm2004 +
                                            " GROUP BY val_prm " +
                                            " HAVING COUNT(nzp_kvar) > 1 ) ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    ExecSQL(connDB, sUpdStat + " " + tPrm2004, false);

                    #endregion

                    #region сопоставление лицевых счетов

                    //сопоставление с параметром "Старый лицевой счет"
                    sql = " UPDATE " + tLchetDecimal + " SET nzp_kvar = t.nzp_kvar " +
                          " FROM " + tPrm2004 + " t " +
                          " WHERE t.val_prm = lchet ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    //сопоставление с pkod
                    sql = " UPDATE " + tLchetDecimal + " SET nzp_kvar = k.nzp_kvar " +
                          " FROM " + prefData + "kvar k " +
                          " WHERE regexp_replace(" + tLchetDecimal + ".lchet, '[^0-9]', '', 'g') <> '' " +
                            " AND " + tLchetDecimal + ".nzp_kvar IS NULL " +
                            " AND k.pkod = regexp_replace(" + tLchetDecimal + ".lchet, '[^0-9]', '', 'g') " + DBManager.sConvToNum;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #region LCHET не найдены в биллинге

                    sql = " SELECT lchet " +
                          " FROM " + tLchetDecimal +
                          " WHERE nzp_kvar IS NULL ";

                    oldLS = string.Empty;
                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["lchet"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: лицевые счета загруженные из файла не найдены в биллинге - " + oldLS,
                           MonitorLog.typelog.Warn, true);

                    //удаляем удаляемый не определённые лицевые счета
                    sql = " DELETE FROM " + tLchetDecimal +
                          " WHERE nzp_kvar IS NULL ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #region обновляем в основной таблице иденитификатор nzp_kvar

                    //Обновляем основную таблицу 
                    sql = " UPDATE  " + prefData + "tula_ex_sz_file " +
                          " SET nzp_kvar = t.nzp_kvar " +
                          " FROM " + tLchetDecimal + " t " +
                          " WHERE t.id = " + prefData + "tula_ex_sz_file.id " +
                            " AND " + prefData + "tula_ex_sz_file.nzp_kvar IS NULL " +
                            " AND " + prefData + "tula_ex_sz_file.nzp_ex_sz = " + nzpExSZ;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #endregion
                }
                else
                {
                    #region Все банки данных, кроме тульских

                    #region создание временных таблиц

                    //содержит лицевые счета из параметра "Старый лицевой счет"
                    ExecSQL(connDB, "DROP TABLE " + tPrm2004, false);
                    ret = ExecSQL(connDB, "CREATE TEMP TABLE " + tPrm2004 + " (" +
                                            " nzp_kvar INTEGER, " +
                                            " val_prm CHARACTER(20)) " + sUnlogTempTable);
                    if (!ret.result) throw new Exception(ret.text);
                    ExecSQL(connDB, " CREATE INDEX ix_" + tPrm2004 + "_2 ON " + tPrm2004 + "(nzp_kvar, val_prm)");

                    //Содержит лицевые счета, содержащие в загруженном файле
                    ExecSQL(connDB, " DROP TABLE " + tLchetDecimal, false);
                    ret = ExecSQL(connDB, " CREATE TEMP TABLE " + tLchetDecimal + " (" +
                                            " id INTEGER, " +
                                            " nzp_kvar INTEGER, " +
                                            " lchet CHARACTER(20)) " + sUnlogTempTable);
                    if (!ret.result) throw new Exception(ret.text);

                    ExecSQL(connDB, " CREATE INDEX ix_" + tLchetDecimal + "_1 ON " + tLchetDecimal + "(id, lchet)");

                    #endregion

                    #region заполнение временных таблиц
                    foreach (int nzpWP in listWP)
                    {
                        string localPrefData = listPref[nzpWP] + DBManager.sDataAliasRest;
                        //сопоставляем с номером лс в системе(параметр "Старый лицевой счет") 
                        sql = " INSERT INTO " + tPrm2004 + " (nzp_kvar, val_prm) " +
                              " SELECT DISTINCT p1.nzp, " +
                                     " TRIM(p1.val_prm) " +
                              " FROM " + localPrefData + "prm_1 p1 " +
                              " WHERE p1.nzp_prm = 2004 " +
                                " AND p1.is_actual <> 100 ";
                        ret = ExecSQL(connDB, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                    ExecSQL(connDB, sUpdStat + " " + tPrm2004, false);

                    //заносим корректные л/с
                    sql = " INSERT INTO " + tLchetDecimal + " (id,lchet)" +
                          " SELECT id, " +
                                 " TRIM(lchet) AS lchet " +
                          " FROM " + prefData + "tula_ex_sz_file " +
                          " WHERE lchet IS NOT NULL " +
                            " AND nzp_kvar IS NULL AND nzp_ex_sz = " + nzpExSZ;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    ExecSQL(connDB, sUpdStat + " " + tLchetDecimal, false);

                    #endregion

                    #region проверка параметра "Старый лицевой счет"

                    //проверка корректности
                    sql = " SELECT k.num_ls " +
                          " FROM " + tPrm2004 + " p INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = p.nzp_kvar" +
                          " WHERE val_prm = '' " +
                          " GROUP BY k.num_ls ";

                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["num_ls"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: в лицевом(-ых) счете(-ах) " + oldLS + " некорректный параметр 'Старый лицевой счет' ",
                           MonitorLog.typelog.Warn, true);

                    //удаляем значения с ''
                    sql = " DELETE FROM " + tPrm2004 +
                          " WHERE val_prm = '' ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    //---------------------------------------------

                    //проверка дублированости
                    sql = " SELECT val_prm " +
                          " FROM " + tPrm2004 +
                          " GROUP BY val_prm " +
                          " HAVING COUNT(nzp_kvar) > 1 ORDER BY 1 ";

                    oldLS = string.Empty;
                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["val_prm"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: дублируется параметр 'Старый лицевой счет' - " + oldLS,
                           MonitorLog.typelog.Warn, true);

                    //удаляем дублирующие л/с
                    sql = " DELETE FROM " + tPrm2004 + " " +
                          " WHERE val_prm IN (SELECT val_prm " +
                                            " FROM " + tPrm2004 +
                                            " GROUP BY val_prm " +
                                            " HAVING COUNT(nzp_kvar) > 1 ) ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    ExecSQL(connDB, sUpdStat + " " + tPrm2004, false);

                    #endregion

                    #region проверка поля LCHET в загруженном файле

                    //проверка корректности
                    sql = " SELECT id " +
                          " FROM " + tLchetDecimal +
                          " WHERE lchet = '' ";

                    oldLS = string.Empty;
                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["id"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: в загружаемом файле некорректный параметр(-ы) id: " + oldLS,
                           MonitorLog.typelog.Warn, true);

                    //удаляем значения с ''
                    sql = " DELETE FROM " + tLchetDecimal +
                          " WHERE lchet = '' ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    //---------------------------------------------

                    //проверка дублированности
                    sql = " SELECT lchet " +
                          " FROM " + tLchetDecimal +
                          " GROUP BY lchet " +
                          " HAVING COUNT(lchet) > 1 ";

                    oldLS = string.Empty;
                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["lchet"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: дублируется поле LCHET ' - " + oldLS,
                           MonitorLog.typelog.Warn, true);

                    #endregion

                    #region Сопоставление л/с

                    //Сопоставление по pkod
                    sql = " UPDATE " + tLchetDecimal + " SET nzp_kvar = kv.nzp_kvar " +
                           " FROM " + prefData + "kvar kv " +
                           " WHERE TRIM(kv.pkod " + DBManager.sConvToChar + "(20)) = TRIM(" + tLchetDecimal + ".lchet) ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    //Сопоставление по параметру "Старый лицевой счет"
                    sql = " UPDATE " + tLchetDecimal + " SET nzp_kvar = t.nzp_kvar " +
                          " FROM " + tPrm2004 + " t " +
                          " WHERE TRIM(t.val_prm) = TRIM(" + tLchetDecimal + ".lchet) " +
                            " AND " + tLchetDecimal + ".nzp_kvar IS NULL ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #region LCHET не найдены в биллинге

                    sql = " SELECT TRIM(lchet) AS lchet " +
                          " FROM " + tLchetDecimal +
                          " WHERE nzp_kvar IS NULL ";

                    oldLS = string.Empty;
                    td = DBManager.ExecSQLToTable(connDB, sql);
                    oldLS = td.Rows.Cast<DataRow>()
                        .Aggregate(oldLS, (current, value) => current + (value["lchet"].ToString().Trim() + ",")).TrimEnd(',');

                    if (oldLS != string.Empty) MonitorLog.WriteLog(
                           "Соц. защита: лицевые счета из загруженного файла не найдены  - " + oldLS,
                           MonitorLog.typelog.Warn, true);

                    //удаляем л/с не имеющиеся в таблице tula_ex_sz_file
                    sql = " DELETE FROM " + tLchetDecimal +
                          " WHERE nzp_kvar IS NULL ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #region обновляем в основной таблице иденитификатор nzp_kvar

                    sql = " UPDATE  " + prefData + "tula_ex_sz_file " +
                          " SET nzp_kvar = t.nzp_kvar " +
                          " FROM " + tLchetDecimal + " t " +
                          " WHERE t.id = " + prefData + "tula_ex_sz_file.id " +
                            " AND " + prefData + "tula_ex_sz_file.nzp_ex_sz = " + nzpExSZ;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #endregion
                }
                ExecSQL(connDB, "UPDATE " + prefData + "tula_ex_sz SET proc = 1 WHERE nzp_ex_sz = " + nzpExSZ, false);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetUploadExchangeSZ:: Ошибка записи данных в систему: " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка записи данных в систему";
                ret.result = false;
                return ret;
            }
            finally
            {
                if (gExSz != -1 && !ret.result)
                    ExecSQL(connDB, "UPDATE " + prefData + "tula_ex_sz SET proc = -1 WHERE nzp_ex_sz = " + gExSz, false);
                if (connDB != null) connDB.Close();
            }

            return ret;
        }

        #region удаление файлов обмена
        public Returns DeleteFromExchangeSZ(Finder finder, int nzpExSZ)
        {
            Returns ret;
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1) { result = false };
                return ret;
            }

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            string sql = " DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_ex_sz_file WHERE nzp_ex_sz = " + nzpExSZ;
            ret = ExecSQL(connDB, sql, true);
            if (!ret.result) return ret;

            sql = " DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_ex_sz where nzp_ex_sz=" + nzpExSZ;
            ret = ExecSQL(connDB, sql, true);
            if (!ret.result) return ret;

            sql = " DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + "tula_ex_sz_wp where nzp_ex_sz=" + nzpExSZ;
            ret = ExecSQL(connDB, sql, true);
            if (!ret.result) return ret;

            connDB.Close();
            return ret;
        }
        #endregion

        #endregion

        public List<SimpleLoadClass> GetSimpleLoadData(FilesImported finder, out Returns ret)
        {

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);


            List<SimpleLoadClass> list = new List<SimpleLoadClass>();
            int i;
            DbTables tables = new DbTables(conn_db);
            switch (finder.SimpLdFileType)
            {
                // Формирование FROM, Where при извлечении данных, связанных со сторонними поставщиками
                case SimpleLoadTypeFile.SuppCharges:
                    string dopSelectFromAndWhere = ", s.name_supp FROM " + tables.simple_load + " sl " +
                " LEFT OUTER JOIN " + tables.supplier + " s on s.nzp_supp = sl.nzp  WHERE sl.tip=" + (int)finder.SimpLdFileType;
                    i = 0;
                    list = selectSimpleLoadData(conn_db, finder, dopSelectFromAndWhere, ref i, out ret);
                    conn_db.Close();
                    return list;
                case SimpleLoadTypeFile.LoadOplFromKassa:
                    // при извлечении данных, связанные с загрузкой оплат из кассы
                    #region Извлечение годов для таблиц nftul_fin_xx и формирование select и from для извлечения информации о загруженных оплатах
                    string getYearSql = "select distinct year_ from " + tables.simple_load + " sl where sl.tip=" +
                                        (int)finder.SimpLdFileType;
                    List<string> dopSelectFromWhereList = new List<string>();
                    MyDataReader readerOplFrKassa;
                    ret = ExecRead(conn_db, out readerOplFrKassa, getYearSql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }
                    try
                    {
                        while (readerOplFrKassa.Read())
                        {
                            if (readerOplFrKassa["year_"] == DBNull.Value) continue;
                            string year = ((int)readerOplFrKassa["year_"] % 100).ToString("00");
                            dopSelectFromWhereList.Add(
                            ", f.num_pack, f.sum_pack, f.dat_pack FROM " + tables.simple_load + " sl left outer join " +
                          Points.Pref + "_fin_" + year +
                          DBManager.tableDelimiter + "pack f on (sl.nzp=f.nzp_pack) " +
                          "WHERE sl.year_=" + readerOplFrKassa["year_"] + " and sl.tip=" + (int)finder.SimpLdFileType);
                        }
                        readerOplFrKassa.Close();
                    }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        readerOplFrKassa.Close();
                        ret = new Returns(false, ex.Message);
                        list = null;
                        MonitorLog.WriteLog("Ошибка selectSimpleLoadData " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    }
                    #endregion
                    i = 0;
                    // Для каждого найденного года
                    foreach (string singleDop in dopSelectFromWhereList)
                    {
                        // извлекаем строки для отображения в таблице
                        List<SimpleLoadClass> singleList = selectSimpleLoadData(conn_db, finder, singleDop, ref i, out ret);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return null;
                        }
                        if (singleList != null && singleList.Count > 0)
                        {
                            {
                                if (list != null) list.AddRange(singleList);
                            }
                        }
                    }
                    conn_db.Close();
                    return list;
                case SimpleLoadTypeFile.LoadPayments:
                    string sql = ", data_type FROM " + tables.simple_load + " sl,  " + Points.Pref + DBManager.sDataAliasRest + "simple_pay_file f " +
                        " WHERE sl.tip = " + (int)finder.SimpLdFileType + " AND sl.nzp_load = f.nzp_load ";
                    i = 0;
                    list = selectSimpleLoadData(conn_db, finder, sql, ref i, out ret);
                    conn_db.Close();
                    return list;
                case SimpleLoadTypeFile.ImportParam:
                    {
                        sql = " FROM " + tables.simple_load + " sl " +
                            " WHERE sl.tip = " + (int)finder.SimpLdFileType;
                        i = 0;
                        list = selectSimpleLoadData(conn_db, finder, sql, ref i, out ret);
                        conn_db.Close();
                        return list;
                    }
                default: return null;
            }

        }

        private List<SimpleLoadClass> selectSimpleLoadData(IDbConnection conn_db, FilesImported finder,
            string dopSelectaAndFrom, ref int i, out Returns ret)
        {
            List<SimpleLoadClass> list = new List<SimpleLoadClass>();
            MyDataReader readerMain = null;
            StringBuilder sql = new StringBuilder(
                " SELECT  sl.nzp, sl.nzp_load, sl.nzp_exc, sl.created_on, " +
                " sl.month_, sl.year_, sl.nzp_wp, sl.file_name, sl.nzp_supp, sl.temp_file, sl.download_status, sl.parsing_status, sl.tip " +
                 dopSelectaAndFrom + " and is_actual<>100 "+
                " ORDER BY created_on desc");
            ret = ExecRead(conn_db, out readerMain, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                //int i = 0;
                while (readerMain.Read())
                {
                    i++;
                    if (finder.skip > 0 && i <= finder.skip) continue;
                    if (i > finder.skip + finder.rows) continue;
                    SimpleLoadClass sl = new SimpleLoadClass();
                    if (readerMain["nzp_load"] != DBNull.Value) sl.nzp_load = Convert.ToInt32(readerMain["nzp_load"]);
                    if (readerMain["nzp_exc"] != DBNull.Value) sl.nzp_exc = Convert.ToInt32(readerMain["nzp_exc"]);
                    if (readerMain["created_on"] != DBNull.Value) sl.created_on = Convert.ToDateTime(readerMain["created_on"]).ToShortDateString();
                    if (readerMain["month_"] != DBNull.Value) sl.month_ = Convert.ToInt32(readerMain["month_"]);
                    if (readerMain["year_"] != DBNull.Value) sl.year_ = Convert.ToInt32(readerMain["year_"]);
                    sl.calc_month = Utils.GetMonthName(sl.month_) + " " + sl.year_;
                    if (readerMain["nzp"] != DBNull.Value) sl.nzp_supp = Convert.ToInt32(readerMain["nzp"]);
                    if (readerMain["nzp_supp"] != DBNull.Value) sl.nzp_supp = Convert.ToInt32(readerMain["nzp_supp"]);
                    sl.point = Points.GetPoint(sl.nzp_wp).point;
                    if (readerMain["file_name"] != DBNull.Value) sl.file_name = Convert.ToString(readerMain["file_name"]);
                    if (readerMain["temp_file"] != DBNull.Value) sl.temp_file = Convert.ToString(readerMain["temp_file"]);
                    if (readerMain["download_status"] != DBNull.Value) sl.download_status = Convert.ToInt32(readerMain["download_status"]);
                    if (readerMain["tip"] != DBNull.Value) sl.tip = Convert.ToInt32(readerMain["tip"]);
                    switch (finder.SimpLdFileType)
                    {
                        case SimpleLoadTypeFile.SuppCharges:
                            if (readerMain["name_supp"] != DBNull.Value)
                                sl.name_supp = Convert.ToString(readerMain["name_supp"]);
                            if (readerMain["nzp_wp"] != DBNull.Value)
                            {
                                sl.nzp_wp = Convert.ToInt32(readerMain["nzp_wp"]);
                                sl.point = Points.GetPoint(sl.nzp_wp).point;
                            }
                            if (readerMain["parsing_status"] != DBNull.Value)
                                sl.parsing_status = Convert.ToInt32(readerMain["parsing_status"]);
                            break;
                        case SimpleLoadTypeFile.LoadOplFromKassa:
                            if (readerMain["num_pack"] != DBNull.Value)
                                sl.num_pack = Convert.ToString(readerMain["num_pack"]);
                            if (readerMain["sum_pack"] != DBNull.Value)
                                sl.sum_pack = Convert.ToString(readerMain["sum_pack"]);
                            if (readerMain["dat_pack"] != DBNull.Value)
                            {
                                string dat = Convert.ToString(readerMain["dat_pack"]);
                                sl.dat_pack = dat.Length > 10 ? dat.Remove(10) : dat;
                            }
                            break;
                        case SimpleLoadTypeFile.LoadPayments:
                            int type = readerMain["data_type"] != DBNull.Value ? Convert.ToInt32(readerMain["data_type"]) : 1;
                            switch (type)
                            {
                                case 1:
                                    sl.data_type = "Оплаты";
                                    break;
                                case 2:
                                    sl.data_type = "Показания ПУ";
                                    break;
                                case 3:
                                    sl.data_type = "Оплаты, показания ПУ";
                                    break;
                            }
                            break;
                    }
                    sl.num = i.ToString();
                    list.Add(sl);
                }
                ret.tag = i;
                readerMain.Close();
            }
            catch (Exception ex)
            {
                conn_db.Close();
                readerMain.Close();
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog("Ошибка selectSimpleLoadData " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            return list;
        }

        public Returns Delete(SimpleLoadClass finder)
        {
            if (finder.nzp_load <= 0)
            {
                return new Returns(false, "Нет данных для удаления", -1);
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            DbTables tables = new DbTables(conn_db);
            StringBuilder sql = new StringBuilder("delete from " + tables.simple_load + " where nzp_load  = " + finder.nzp_load);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при удалении записи из таблицы загрузок: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при удалении записи";
            }
            return ret;
        }

        public Returns StartTransfer(TransferParams finder)
        {
            var IocContainer = new TransferObjectsInstaller();
            var ret = new Returns();
            try
            {
                string transferData = "Перенос домов из одного локального банка в другой_";
                string transferDataLog = "Перенос домов из одного локального банка в другой_";

                //Создаем вспомогательный контейнер для работы с ExcelUtility
                var excUtility = new ExcelUtility()
                {
                    nzp_user = finder.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = String.Format("Перенос домов из '{0}' в '{1}'", finder.fPoint.point, finder.tPoint.point),
                    is_shared = 1
                };

                //Формируем полный путь к файлу 
                transferData = CreateFileFullName(transferData + DateTime.Now.ToString("yyyyMMddHHmmss"));
                transferDataLog = CreateFileFullName(transferDataLog + DateTime.Now.ToString("yyyyMMddHHmmss") + ".Log");

                //Записываем в таблицу excel_utility
                var myFiles = new DBMyFiles();

                //Считываем присвоенный код в таблице excel_utility
                finder.nzp_exc = excUtility.nzp_exc = myFiles.AddFile(excUtility).tag;

                //Имя сохраненного файла
                finder.saved_name = transferData;
                //Лог ошибок
                finder.saved_name_log = transferDataLog;

                IocContainer.Register();
                finder.transfer_id = TransferProgress.InsertIntoTransferDataLog(finder.user.nzpuser);
                IocContainer.Container.Register(Component.For<TransferParams>().UsingFactoryMethod(() => finder));
                IocContainer.koeff = 1m / finder.houses.Count;

                if (IocContainer.Comparer(finder.transfer_id, finder.fPoint.point, finder.tPoint.point, ref CommentList))
                {
                    SetProcessProgress(0.3m, finder.nzp_exc);
                    Parallel.ForEach(finder.houses, new ParallelOptions { MaxDegreeOfParallelism = 1 }, house =>
                    {
                        finder.current_house = house;
                        List<Transfer> ExecuteTransfer;
                        var result = IocContainer.TransferExecute(finder.transfer_id, finder.fPoint.point, finder.tPoint.point, out ExecuteTransfer, ref CommentList);
                        SetProcessProgress(0.8m, finder.nzp_exc);
                        if (result)
                        {
                            result = IocContainer.DeleteExecute(finder.transfer_id, finder.fPoint.point, ExecuteTransfer, ref CommentList);
                            if (result)
                                TransferProgress.InsertIntoHouseLog(finder.transfer_id, house.nzp_dom, 1, "");
                            SetProcessProgress(1m, finder.nzp_exc);
                            //Пишем комментарий в лог
                            string message = String.Format("Перенос домов из банка '{0}' в банк '{1}' завершен успешно!", finder.fPoint.point, finder.tPoint.point);
                            CommentList.Add(message);
                        }
                    });
                    TransferProgress.UpdateProgress(1, finder.transfer_id, 2);
                }

                //Запись в файл
                Filing(GetComment(), finder.saved_name_log);

                //Архивация
                string outputArchiveName = null;
                Archive.GetInstance(ArchiveFormat.Zip)
                    .Compress((outputArchiveName = transferData.Replace(".txt", ".zip")),
                        new[] { transferDataLog}, true);

                //Сохранение на ftp
                if (InputOutput.useFtp)
                    excUtility.exc_path = InputOutput.SaveOutputFile(outputArchiveName);

                //Обновить статус в excel_utility
                var myFile = new DBMyFiles();
                
                myFile.SetFileState(new ExcelUtility {nzp_exc = finder.nzp_exc, status = ExcelUtility.Statuses.Success});
                myFile.SetFilePath(new ExcelUtility(){nzp_exc = finder.nzp_exc, exc_path = Path.GetFileName(outputArchiveName)});
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при переносе домов: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.text = "\n Ошибка при переносе домов";
            }
            return ret;
        }

        /// <summary>
        /// Список комментариев
        /// </summary>
        private List<string> CommentList = new List<string>();

        /// <summary>
        /// Запись комментария в журнал выгрузки
        /// </summary>
        /// <param name="comment"></param>
        public void AddComment(string comment)
        {
            if (comment == String.Empty) return;
            CommentList.Add(comment);
        }

        public string GetComment()
        {
            string ret = "";

            foreach (var element in CommentList)
            {
                ret += element + "\n";
            }

            return ret;
        }

        public void Filing(string str, string path)
        {
            //проверяем - есть ли директория, если ее нет, то создаем
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            //необходимо выгружать именно в кодировке 866
            StreamWriter writer = new StreamWriter(path, true, System.Text.Encoding.GetEncoding(1251));
            writer.WriteLine(str);
            writer.Close();
        }

        private string CreateFileFullName(string shortName)
        {
            return String.Format("{0}{1}{2}",
                    InputOutput.GetOutputDir(),
                    shortName,
                    ".txt"
                    );
        }

        protected void SetProcessProgress(decimal progress, int nzp_exc)
        {
            var myFile = new DBMyFiles();
            myFile.SetFileProgress(nzp_exc, progress);
        }



        public Returns CheckLoadFileExixstsEchange(Finder finder)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.text = "Не задан пользователь";
                ret.result = false;
                return ret;
            }
            if (finder.pref == "")
            {
                finder.pref = Points.Pref + sDataAliasRest;
            }

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = DBManager.GetConnection(conn_kernel);
            ret = DBManager.OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            int parsedCount;
            if (finder.dopFind.Count < 3)
            {
                ret.result = false;
                return ret;
            }
            string tableName = finder.dopFind[0];
            string filename = finder.dopFind[1];
            string nameColumn = finder.dopFind[2];
            string newFileName = filename;
            string sql = "select count(*) from " + finder.pref + tableName + " where " + nameColumn + "='" + newFileName + "'";
            object count = DBManager.ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }
            if (!Int32.TryParse(count.ToString(), out parsedCount))
            {
                MonitorLog.WriteLog(
                    "Ошибка преобразования переменной count типа object в переменную типа int в методе LoadOplatyKassa.CheckExixstsFile()",
                    MonitorLog.typelog.Error, true);
                return ret;
            }
            if (parsedCount > 0)
            {
                ret.tag = -1;
                ret.text = "Файл с таким именем уже существует";
            }
            return ret;
        }

        public List<PrmTypes> GetParamSprav(ParamCommon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            string where = "";
            if (finder.name_prm != "") where = " AND name_prm ilike '%" + finder.name_prm + "%' ";
            try
            {
                string sql =
                    " SELECT nzp_prm, name_prm " +
                    " FROM " + Points.Pref + sKernelAliasRest + "prm_name " +
                    " WHERE prm_num in (1,2) " + where +
                    " ORDER BY name_prm";
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                foreach (DataRow row in dt.Rows)
                {
                    PrmTypes pt = new PrmTypes();
                    pt.id = Convert.ToInt32(row["nzp_prm"]);
                    pt.type_name = row["name_prm"].ToString().Trim();
                    list.Add(pt);
                }
            }
            finally
            {
                conn_db.Close();
            }
            return list;
        }

        public Returns ExportParam(ExportParamsFinder finder)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string temp = "temp_ImportParamAdm" + finder.nzp_user;
            //имя файла                                                         
            string fullFileName = "Выгрузка параметров в ПС Администратор";
            try
            {
                int nzpExc;
                ExcelRepClient excelRep = new ExcelRepClient();
                ret = excelRep.AddMyFile(new ExcelUtility()
                {
                    nzp_user = finder.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = fullFileName,
                    is_shared = 1
                });
                if (!ret.result)
                {
                    return new Returns(false, "Ошибка постановки отчета", -1);
                }
                nzpExc = ret.tag;

                string nzp_prm = String.Join(",", finder.paramsNzp);
                string limit = finder.numer > 0 ? " limit " + finder.numer : "";
                string pref = (from point in Points.PointList
                               where point.nzp_wp == finder.nzp_wp
                               select point.pref).FirstOrDefault();
                string sql;

                sql = " DROP TABLE " + temp;
                DBManager.ExecSQL(conn_db, sql, false);
                sql =
                    " CREATE TEMP TABLE " + temp +
                    " (nzp " + DBManager.sDecimalType + "(13,0), " +
                    " adres CHAR(200))";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " INSERT INTO " + temp +
                    " (nzp,  adres)" +
                    " SELECT DISTINCT  k.nzp_kvar, " +
                    " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(d.ndom)||'/'||trim(d.nkor)" +
                    "||' кв.'||trim(k.nkvar) as adres " +
                    " FROM " + pref + DBManager.sDataAliasRest + "dom d," +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    pref + DBManager.sDataAliasRest + "kvar k " +
                    " WHERE k.nzp_dom = d.nzp_dom AND " +
                    " d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " INSERT INTO " + temp +
                    " (nzp,  adres)" +
                    " SELECT DISTINCT  d.nzp_dom," +
                    " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(d.ndom)||'/'||trim(d.nkor) as adres " +
                    " FROM " + pref + DBManager.sDataAliasRest + "dom d," +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon r" +
                    " WHERE " +
                    " d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
                DBManager.ExecSQL(conn_db, sql, true);

                sql = " CREATE INDEX inx_" + temp + " on " + temp + " (nzp)";
                DBManager.ExecSQL(conn_db, sql, true);
                sql = DBManager.sUpdStat + " " + temp;
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " SELECT pxx.nzp, pxx.dat_s, pxx.dat_po, pxx.is_actual, trim(pxx.val_prm) as val_prm, " +
                    " p.nzp_prm, p.name_prm, t.adres " +
                    " FROM " + Points.Pref + sKernelAliasRest + "prm_name p," +
                    pref + sDataAliasRest + "prm_1 pxx," +
                    temp + " t " +
                    " WHERE p.nzp_prm IN (" + nzp_prm + ")" +
                    " AND pxx.dat_s <= '" + finder.dat_po + "'" +
                    " AND pxx.dat_po >= '" + finder.dat_s + "'" +
                    " AND pxx.nzp_prm = p.nzp_prm AND t.nzp = pxx.nzp" +
                    " UNION " +
                    " SELECT pxx2.nzp, pxx2.dat_s, pxx2.dat_po, pxx2.is_actual, trim(pxx2.val_prm) as val_prm, " +
                    " p2.nzp_prm, p2.name_prm, t2.adres " +
                    " FROM " + Points.Pref + sKernelAliasRest + "prm_name p2," +
                    pref + sDataAliasRest + "prm_2 pxx2," +
                    temp + " t2 " +
                    " WHERE p2.nzp_prm IN (" + nzp_prm + ")" +
                    " AND pxx2.dat_s <= '" + finder.dat_po + "'" +
                    " AND pxx2.dat_po >= '" + finder.dat_s + "'" +
                    " AND pxx2.nzp_prm = p2.nzp_prm AND t2.nzp = pxx2.nzp" +
                    " ORDER BY name_prm " + limit;
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);

                #region создаем Excel

                object misValue = System.Reflection.Missing.Value;
                //Создание сервера Excel
                Excel.Application ExlApp = new Excel.Application();
                //отключить предупреждения
                ExlApp.DisplayAlerts = false;
                //Создание рабочей книги
                Excel.Workbook ExlWb = ExlApp.Workbooks.Add(misValue);
                //Получение ссылки на лист1
                Excel.Worksheet ExlWs = (Excel.Worksheet)ExlWb.Worksheets.get_Item(1);
                ExlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;
                ExlWs.Rows.Font.Name = "Arial";

                #region Заголовки
                ExlWs.Cells[1, 1] = "Код объекта";
                ExlWs.Cells[1, 2] = "Код параметра";
                ExlWs.Cells[1, 3] = "Дата начала периода действия";
                ExlWs.Cells[1, 4] = "Дата окончания периода действия";
                ExlWs.Cells[1, 5] = "Значение параметра";
                ExlWs.Cells[1, 6] = "Актуальность";
                if (finder.is_export_names_checked)
                {
                    ExlWs.Cells[1, 7] = "Наименование параметра";
                    ExlWs.Cells[1, 8] = "Адрес объекта";
                }
                #endregion

                int i = 2;
                foreach (DataRow row in dt.Rows)
                {
                    ExlWs.Cells[i, 1] = row["nzp"].ToString();
                    ExlWs.Cells[i, 2] = row["nzp_prm"].ToString();
                    ExlWs.Cells[i, 3] = row["dat_s"].ToString().Substring(0, 10);
                    ExlWs.Cells[i, 4] = row["dat_po"].ToString().Substring(0, 10);
                    ExlWs.Cells[i, 5] = row["val_prm"].ToString();
                    ExlWs.Cells[i, 6] = row["is_actual"].ToString().Trim() != "1" ? "0" : "1";
                    if (finder.is_export_names_checked)
                    {
                        ExlWs.Cells[i, 7] = row["name_prm"].ToString();
                        ExlWs.Cells[i, 8] = row["adres"].ToString();
                    }

                    i++;
                }

                //Устанавливаем формат
                ExlApp.DefaultSaveFormat = Excel.XlFileFormat.xlWorkbookNormal;
                //???
                //ExlWb.Saved = true;
                //Не Отображать сообщение о замене существующего
                ExlApp.DisplayAlerts = false;
                //Формат сохраняемого файла
                //CurrentExcellApp.DefaultSaveFormat = Excel.XlFileFormat.xlExcel9795;
                string fileName = "ExportParam_" + finder.nzp_user + ".xls";
                string filePath = Constants.Directories.ReportDir + fileName;

                // rep.SavePrepared(filePath);


                try
                {
                    ExlWb.SaveAs(filePath, Excel.XlFileFormat.xlWorkbookNormal, //Excel.XlFileFormat.xlExcel9795,
                        Type.Missing, Type.Missing, Type.Missing,
                        Type.Missing, Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing,
                        Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                    ExlWb.Save();
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при сохранения документа : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = ex.Message;
                    ret.result = false;

                    return ret;
                }
                finally
                {
                    ExlWb.Close();
                    ExlApp.Quit();
                    Marshal.ReleaseComObject(ExlWs);
                    Marshal.ReleaseComObject(ExlWb);
                    Marshal.ReleaseComObject(ExlApp);
                    ExlWs = null;
                    ExlWb = null;
                    ExlApp = null;
                    GC.Collect();
                }

                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                }

                var myFile = new DBMyFiles();
                myFile.SetFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = fileName
                });

                #endregion
            }
            finally
            {
                string sql = " DROP TABLE " + temp;
                DBManager.ExecSQL(conn_db, sql, false);
                conn_db.Close();
            }

            return ret;
        }

        public Returns CheckSimpleLoadFileExixsts(FilesImported finder)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.text = "Не задан пользователь";
                ret.result = false;
                return ret;
            }

            //#region подключение к базе

            //IDbConnection conn_db = DBManager.GetConnection(conn_kernel);
            //ret = DBManager.OpenDb(conn_db, true);
            //if (!ret.result) return ret;
            //#endregion
            int parsedCount;
            string fulltableName;
            string nameColumn;
            string sql = "";
            switch (finder.SimpLdFileType)
            {
                case SimpleLoadTypeFile.LoadOplFromKassa:
                    fulltableName = DBManager.sDefaultSchema + "source_pack";
                    nameColumn = "filename";
                    sql = "select exists (select 1 from " + fulltableName + " where is_actual<>100 and " + nameColumn + "='" + finder.saved_name + "')";
                    break;
                case SimpleLoadTypeFile.LoadPayments:
                    fulltableName = Points.Pref + DBManager.sDataAliasRest + "simple_load l";
                    string typeFileTable = Points.Pref + DBManager.sDataAliasRest + "simple_pay_file f";
                    string whereString = "";
                    nameColumn = "l.file_name";
                    sql = "select exists (select 1 from " + fulltableName + ", " + typeFileTable + " where " +
                          nameColumn + "='" + finder.saved_name + "' ";
                    switch (finder.LoadPayOrIpuType)
                    {
                        case SimpleLoadPayOrIpuType.Ipu:
                        case SimpleLoadPayOrIpuType.Pay:
                            whereString = " AND (f.data_type = " + (int)finder.LoadPayOrIpuType + " OR f.data_type = " + (int)SimpleLoadPayOrIpuType.PayAndIpu + "))";
                            break;
                        case SimpleLoadPayOrIpuType.PayAndIpu:
                            whereString = " AND (f.data_type = " + (int)SimpleLoadPayOrIpuType.Ipu + " OR f.data_type = " + (int)SimpleLoadPayOrIpuType.Pay +
                                " OR f.data_type = " + (int)SimpleLoadPayOrIpuType.PayAndIpu + "))";
                            break;
                    }
                    sql += whereString;
                    break;
                    case SimpleLoadTypeFile.LoadCountersRSO:
                    fulltableName = Points.Pref + DBManager.sDataAliasRest + "simple_load l";
                    nameColumn = "l.file_name";
                    sql = "select exists (select 1 from " + fulltableName + " where " + nameColumn + "='" 
                        + finder.saved_name + "' and tip ="+(int)SimpleLoadTypeFile.LoadCountersRSO+")";
                    break;
                default:
                    ret.result = false;
                    ret.text = "Не задан тип загрузки";
                    return ret;
            }

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            using (IDbConnection conn_db = DBManager.GetConnection(conn_kernel))
            {
                ret = DBManager.OpenDb(conn_db, true);
                if (!ret.result) return ret;
                object isFileNameExists = DBManager.ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (!(bool)isFileNameExists) return ret;
                ret.tag = -1;
                ret.text = "Файл с таким именем уже существует";
                return ret;
            }
        }

        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }

        public string GetUserName(ExFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var user_name = "";
            var conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            user_name = GetUserNameByNzp(conn_db, finder.nzp_user);
            conn_db.Close();
            return user_name;
        }

        public Returns CheckVtb24(IDbConnection conn_db, FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();

            decimal commission = 0;
            try
            {
                string message_date = Convert.ToDateTime(Meta.MessageDate).ToString("dd.MM.yyyy");
                decimal total_amount = Convert.ToDecimal(Meta.TotalAmount);
                decimal.TryParse(Meta.TotalCommission, out commission);

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

        public Returns CheckVtb24(FilesImported finder)
        {
            Utils.setCulture();
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);


            #region Разбираем файл
            //директория файла
            string fDirectory = "";
            if (InputOutput.useFtp)
            {
                fDirectory = InputOutput.GetInputDir();
                InputOutput.DownloadFile(finder.ex_path, Path.Combine(fDirectory, finder.ex_path), true);
            }
            else
            {
                fDirectory = Constants.Directories.ImportAbsoluteDir;
            }

            //string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
            string fileName = Path.Combine(fDirectory, finder.ex_path);

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
            ret = CheckVtb24(conn_db, finder);
            if (!ret.result)
            {
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

        public Returns MoveLoadToArchive(SimpleLoadClass finder)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            string sql = "update " + Points.Pref + sDataAliasRest + "simple_load set is_actual=100 where nzp_load="+finder.nzp_load;
            try
            {
               ret = ExecSQL(conn_db, sql);
            }
            finally
            {
                if (conn_db!=null) conn_db.Close();
            }
            return ret;
        }

        public Returns MoveLoadedSourcePackToArchive(SimpleLoadClass finder)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
           
            try
            {
                string basefin = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00")+tableDelimiter+"pack";
                string sql = "select exists (select 1 from " + basefin + " where file_name=(select file_name from "
                    +Points.Pref+sDataAliasRest+"simple_load where nzp_load="+finder.nzp_load+"))";
                bool res= (bool)ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (res)
                {
                    ret.tag = -1;
                    ret.text = "Удаление записи по выбранной загрузке невозможно. В ПС Финансы загружены пачки из этого файла." +
                               "Сначала необходимо удалить эти пачки.";
                    return ret;
                }
                sql = "update " + sDefaultSchema + "source_pack set is_actual=100 where filename=(select file_name from "
                    +Points.Pref+sDataAliasRest+"simple_load where nzp_load="+finder.nzp_load+")";
                ret = ExecSQL(conn_db, sql);
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                }
            }
            return ret;
        }
    }

}

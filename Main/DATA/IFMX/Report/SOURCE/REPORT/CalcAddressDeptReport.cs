using System;
using System.Collections.Generic;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Linq;
using System.IO;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep
    {
        /// <summary>
        /// Отчет "Расчет задолженности по оплате за ЖКУ по адресу"
        /// </summary>
        /// <returns>Результат</returns>
        public Returns GetCalcAddressDeptReport(Dept finder)
        {
            ClassCalcAddressDeptReport classCalcAddressDeptReport = new ClassCalcAddressDeptReport();
            return classCalcAddressDeptReport.GetReport(finder);
        }

        /// <summary>
        /// Получить ключи адреса (nzp_town, nzp_raj, nzp_ul, nzp_dom) по ключу nzp_kvar
        /// </summary>
        /// <returns>Результат</returns>
        public Returns GetAddressID(int nzp_kvar, out Ls ls)
        {
            ClassGetAddressID classGetAddressID = new ClassGetAddressID();
            return classGetAddressID.GetAddressID(nzp_kvar, out ls);
        }
    }

    /// <summary>
    /// Класс для получения ключей адреса по nzp_kvar
    /// </summary>
    class ClassGetAddressID : DataBaseHead
    {
        /// <summary>
        /// Получить ключи 
        /// </summary>
        /// <param name="conn_db">Соединение с базой</param>
        /// <param name="nzp_kvar">Код квартиры</param>
        /// <param name="ret">Результат</param>
        /// <returns>Строка адреса</returns>
        public Returns GetAddressID(int nzp_kvar, out Ls ls)
        {
            Returns ret = new Returns(true);
            IDataReader reader = null;
            IDbConnection conn_db = null;

            ls = new Ls();

            try
            {
                conn_db = DBManager.newDbConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);
                
                // получить адрес из базы
                string sql = "select r.nzp_town, r.nzp_raj, u.nzp_ul, d.nzp_dom " +
                    " from " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k " +
                    " where r.nzp_raj = u.nzp_raj " +
                    " and u.nzp_ul = d.nzp_ul " +
                    " and d.nzp_dom = k.nzp_dom " +
                    " and k.nzp_kvar = " + nzp_kvar;

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (reader.Read())
                {
                    if (reader["nzp_town"] != DBNull.Value) ls.nzp_town = (int)reader["nzp_town"];
                    if (reader["nzp_raj"] != DBNull.Value) ls.nzp_raj = (int)reader["nzp_raj"];
                    if (reader["nzp_ul"] != DBNull.Value) ls.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["nzp_dom"] != DBNull.Value) ls.nzp_dom = (int)reader["nzp_dom"];
                    ls.nzp_kvar = nzp_kvar;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }
                
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return ret;
        }
    }
    
    class ClassCalcAddressDeptReport : DataBaseHead
    {
        // префикс локального банка данных
        private string _pref = "";

        // дата начала периода
        private DateTime _datS;

        // дата конца периода
        private DateTime _datPo;
        
        
        /// <summary>
        /// Получить отчет
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        public Returns GetReport(Dept finder)
        {
            Returns ret = new Returns(true);

            // проверка параметров отчета
            ret = CheckInputPrm(finder);
            if (!ret.result) return ret;

            _datS = Convert.ToDateTime(finder.dat_s);
            _datPo = Convert.ToDateTime(finder.dat_po);

            int month_count = (_datPo.Year - _datS.Year) * 12 - _datS.Month + _datPo.Month + 1;

            IDbConnection conn_db = null;
            
            try
            {
                conn_db = DBManager.newDbConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

                // получить префикс
                _pref = GetPref(conn_db, finder.nzp_kvar, out ret);
                if (!ret.result) throw new Exception(ret.text);

                // создать временную таблицу
                ret = CreateTempTable(conn_db);
                if (!ret.result) throw new Exception(ret.text);

                DateTime cur_date = _datS; 
                // вставка записей
                for (int i = 0; i <  month_count; i++)
                {
                    ret = InsertData(conn_db, finder.nzp_kvar, cur_date);
                    if (!ret.result) throw new Exception(ret.text);
                    cur_date = cur_date.AddMonths(1);
                }

                // получить отчет
                ret = GetReport(conn_db, finder);
                if (!ret.result) throw new Exception(ret.text);
            }
            catch (Exception ex)
            {
                if (conn_db != null) conn_db.Close();
                MonitorLog.WriteLog("Ошибка в функции GetReport класса CalcAddressDeptReport :\n" + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка при получении отчета \"Расчет задолженности по оплате ЖКУ по адресу\"");
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
            }

            return ret;
        }

        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        /// <returns>Результат</returns>
        private Returns CheckInputPrm(Dept finder)
        {
            Returns ret = new Returns(false);

            if (finder.nzp_user < 1)
            {
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (finder.nzp_kvar < 1)
            {
                ret.text = "Не выбрана квартира";
                ret.tag = 100;
                return ret;
            }

            DateTime tmp_date = new DateTime();

            if (!DateTime.TryParse(finder.dat_s, out tmp_date))
            {
                ret.text = "Неверная дата начала периода";
                ret.tag = 100;
                return ret;
            }

            if (!DateTime.TryParse(finder.dat_po, out tmp_date))
            {
                ret.text = "Неверная дата конца периода";
                ret.tag = 100;
                return ret;
            }

            ret.result = true;
            return ret;
        }

        /// <summary>
        /// Подготовить часть адреса
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        protected string PrepareAddressPart(string addr)
        {
            addr = addr.Trim();
            if (addr == "") return "";
            else return addr;
        }

        /// <summary>
        /// Получить адрес ЛС, для которого выполняется отчет
        /// </summary>
        /// <param name="conn_db">Соединение с базой</param>
        /// <param name="nzp_kvar">Код квартиры</param>
        /// <param name="ret">Результат</param>
        /// <returns>Строка адреса</returns>
        protected string GetAddress(IDbConnection conn_db, int nzp_kvar, out Returns ret)
        {
            ret = new Returns(true);
            IDataReader reader = null;
            string address = "";
                
            try
            {
                // получить адрес из базы
                string sql = "select t.town, r.rajon, u.ulica, u.ulicareg, d.ndom, d.nkor, k.nkvar, k.nkvar_n " +
                    " from " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_town t, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "s_rajon r, " +
                    Points.Pref + "_data" + DBManager.tableDelimiter + "s_ulica u, " +
                    _pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                    _pref + "_data" + DBManager.tableDelimiter + "kvar k " +
                    " where t.nzp_town = r.nzp_town " +
                    " and r.nzp_raj = u.nzp_raj " +
                    " and u.nzp_ul = d.nzp_ul " +
                    " and d.nzp_dom = k.nzp_dom " +
                    " and k.nzp_kvar = " + nzp_kvar;

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return "";

                if (reader.Read())
                {
                    // город/район
                    address += reader["town"].ToString().Trim();

                    // нас пункт
                    string rajon = PrepareAddressPart(reader["rajon"].ToString());

                    if (rajon != "")
                    {
                        if (address != "") address += ",";
                        address += rajon;
                    }

                    // улица, тип улицы (УЛ, ПР-Т...)
                    string ulica = PrepareAddressPart(reader["ulica"].ToString());
                    string ulicareg = PrepareAddressPart(reader["ulicareg"].ToString());

                    if (ulica != "")
                    {
                        if (ulicareg != "")
                        {
                            ulica = ulicareg + " " + ulica;
                        }

                        if (address != "") address += ",";
                        address += " " + ulica;
                    }

                    // дом, корпус
                    string ndom = PrepareAddressPart(reader["ndom"].ToString());
                    string nkor = PrepareAddressPart(reader["nkor"].ToString());

                    if (ndom != "")
                    {
                        if (nkor != "")
                        {
                            ndom += " к." + nkor;
                        }

                        if (address != "") address += ", ";
                        address += "д." + ndom;
                    }

                    // № квартиры
                    string nkvar = PrepareAddressPart(reader["nkvar"].ToString());

                    if (nkvar != "")
                    {
                        if (address != "") address += ",";
                        address += " кв." + nkvar;
                    }
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            
            return address;
        }

        /// <summary>
        /// Получить входящее сальдо
        /// </summary>
        /// <param name="conn_db">Соединение с базой</param>
        /// <param name="nzp_kvar">Код квартиры</param>
        /// <param name="ret">Результат</param>
        /// <returns>Строка адреса</returns>
        protected decimal GetSumInSaldo(IDbConnection conn_db, int nzp_kvar, DateTime charge_date, out Returns ret)
        {
            ret = new Returns(true);
            IDataReader reader = null;
            decimal sum_insaldo = 0;

            try
            {
                string charge_table = _pref + "_charge_" + (charge_date.Year % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + charge_date.Month.ToString("00");
                if (!TempTableInWebCashe(conn_db, charge_table)) return 0;
                
                // получить адрес из базы
                string sql = "select sum(sum_insaldo) as sum_insaldo " +
                    " from " + charge_table +
                    " where nzp_kvar = " + nzp_kvar +
                    " AND nzp_serv > 1 ";

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return 0;

                if (reader.Read())
                {
                    sum_insaldo = (reader["sum_insaldo"]!=DBNull.Value ? (decimal)reader["sum_insaldo"] : 0) ;
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return sum_insaldo;
        }

        /// <summary>
        /// Получить отччет
        /// </summary>
        /// <param name="conn_db">Соединение с базой</param>
        /// <param name="address">Адреса</param>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        protected Returns GetReport(IDbConnection conn_db, Dept finder)
        {
            Returns ret = new Returns(true);
            
            try
            {
                // получить адрес
                string address = GetAddress(conn_db, finder.nzp_kvar, out ret);
                if (!ret.result) throw new Exception(ret.text);

                // получить входящее сальдо
                decimal sum_insaldo = GetSumInSaldo(conn_db, finder.nzp_kvar, _datS, out ret);
                if (!ret.result) throw new Exception(ret.text);
                
                DataTable dataTable = ClassDBUtils.OpenSQL("select * from tmp_report_address_dept", "dept", conn_db).GetData();

                DataSet ds_rep = new DataSet();
                ds_rep.Tables.Add(dataTable);           

                FastReport.Report rep = new FastReport.Report();
                rep.Load(System.IO.Directory.GetCurrentDirectory() + @"\Template\web_900.frx");

                rep.RegisterData(ds_rep);

                //параметры
                rep.SetParameterValue("dat_s", finder.dat_s);
                rep.SetParameterValue("dat_po", finder.dat_po);
                rep.SetParameterValue("address", address);
                rep.SetParameterValue("sum_insaldo", sum_insaldo);
                rep.SetParameterValue("uname", finder.webUname);
                
                string fileName = "";
                string filePath = "";
                FastReport.EnvironmentSettings env = new FastReport.EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                rep.Prepare();
            
                var dir = "";
                if (InputOutput.useFtp) dir = InputOutput.GetOutputDir();
                else dir = STCLINE.KP50.Global.Constants.ExcelDir;

                fileName = (finder.nzp_user * DateTime.Now.Second) + "_" + DateTime.Now.Ticks + "_web_900.fpx";
                filePath = dir + fileName;

                rep.SavePrepared(filePath);

                if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(Path.Combine(dir, filePath));
                return new Returns(true, fileName);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Вставка данных во временную таблицу
        /// </summary>
        /// <param name="conn_db">Соединение с базой</param>
        /// <param name="nzp_kvar">Код квартиры</param>
        /// <param name="charge_date">Месяц расчета</param>
        /// <returns>Returns</returns>
        protected Returns InsertData(IDbConnection conn_db, int nzp_kvar, DateTime charge_date)
        {
            string charge_table = _pref + "_charge_" + (charge_date.Year % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + charge_date.Month.ToString("00");
            if (!TempTableInWebCashe(conn_db, charge_table)) return new Returns(true);
            
            string sql = "insert into tmp_report_address_dept(year_, month_, sum_real, sum_money, debt_relief) " + 
                " select " + charge_date.Year + "," + charge_date.Month + "," +
                "   sum(sum_real + reval), sum(sum_money), sum(real_charge)" +
                " from " + charge_table +
                " where nzp_kvar = " + nzp_kvar +
                  " AND nzp_serv > 1 ";

            return ExecSQL(conn_db, sql);
        }

        /// <summary>
        /// Создание временной таблицы
        /// </summary>
        /// <param name="conn_db">Соединение с базой</param>
        /// <returns>Returns</returns>
        protected Returns CreateTempTable(IDbConnection conn_db)
        {
            ExecSQL(conn_db, "drop table tmp_report_address_dept", false);
            
            string sql = String.Concat("create temp table tmp_report_address_dept (",
                " year_ integer, ",
                " month_ integer, ",
                " sum_real ", DBManager.sDecimalType, ", ",
                " sum_money ", DBManager.sDecimalType, ", ",
                " debt_relief ", DBManager.sDecimalType, ")"
                );

            return ExecSQL(conn_db, sql);
        }

        /// <summary>
        /// Получить префикс
        /// </summary>
        /// <param name="conn_db">Соединение с базой</param>
        /// <param name="nzp_kvar">Код квартиры</param>
        /// <param name="ret"></param>
        /// <returns>Префикс строка</returns>
        protected string GetPref(IDbConnection conn_db, int nzp_kvar, out Returns ret)
        {
            ret = new Returns(true);
            IDataReader reader = null;
            string pref = "";
            
            try
            {
                ret = ExecRead(conn_db, out reader, "select distinct trim(pref) pref from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar where nzp_kvar = " + nzp_kvar, true);
                if (!ret.result) throw new Exception(ret.text);
                
                if (reader.Read())
                {
                    pref = reader["pref"].ToString();
                }
                else
                {
                    ret = new Returns(false, "Не удалось определить префикс");
                }
            }
            catch
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            
            return pref;
        }
    }
}

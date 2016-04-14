using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Dapper;
using Npgsql;

namespace Bars.Billing.IncrementalDataLoader.Utils.DbUtils
{
    public class Db
    {
        public static void SetCulture()
        {
            var culture = new CultureInfo("ru-RU")
            {
                NumberFormat = { NumberDecimalSeparator = ".", CurrencyDecimalSeparator = "." },
                DateTimeFormat = { ShortDatePattern = "dd.MM.yyyy" }
            };
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }
        public IDbConnection conn { get; set; }

        private int defaultDbTimeOut = 3600; //3600sec, 60 min, 1 hour

        public Db(string connectionString)
        {
            conn = new NpgsqlConnection(connectionString);
        }
        /// <summary>
        /// Получить все записи таблицы imports
        /// </summary>
        /// <returns></returns>
        public List<Request> GetImportValues()
        {
            conn.Open();
            List<Request> list = null;
            try
            {
                if (conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM information_schema.tables " +
                    " WHERE table_schema = 'public' " +
                    " AND table_name = 'sys_imports'", null, null, null, null) == 0)

                    conn.Execute("  CREATE TABLE public.sys_imports " +
                        " ( " +
                        "   nzp_load integer, " +
                        "    Num integer, " +
                        "    FileName text, " +
                        "    Path text, " +
                        "    RegistrationName text, " +
                        "    Format text, " +
                        "    Version text, " +
                        "    Result text, " +
                        "    Status text, " +
                        "    StatusID integer, " +
                        "    progress numeric, " +
                        "    link text, " +
                        "    TypeName text, " +
                        "    AbsolutePath text," +
                        "    schema text," +
                        "    date_charge date, " +
                        "    gis_loaded_file_regiseter_id bigint NOT NULL, " +
                        "  CONSTRAINT sys_imports_pkey PRIMARY KEY (nzp_load), " +
                        "  CONSTRAINT sys_imports_nzp_load_fkey FOREIGN KEY (nzp_load) " +
                        "         REFERENCES imports (nzp_load) MATCH SIMPLE " +
                        "         ON UPDATE NO ACTION ON DELETE NO ACTION " +
                        " );", null);

                list = conn.Query<Request>(
                    " SELECT * FROM public.imports i, public.sys_imports si " +
                    " WHERE i.is_actual<>100 " +
                    " AND i.nzp_load = si.nzp_load " +
                    " ORDER BY i.nzp_load", null)
                    .ToList();
            }
            finally
            {
                conn.Close();
            }
            return list;
        }

        public List<Request> UpdateImportValue(List<Request> reqList)
        {
            conn.Open();
            try
            {
                SetCulture();
                reqList.ForEach(req =>
                {
                    conn.Execute("UPDATE imports " +
                                 " SET num_load=" + req.num_load + ", dat_month=" + (req.dat_month == null ? "NULL" : "'" + req.dat_month + "'") + ", org_name='" +
                                 req.org_name + "', org_podr_name='" + req.org_podr_name + "', " +
                                 " org_inn='" + req.org_inn + "', org_kpp='" + req.org_kpp + "', org_file_num=" +
                                 req.org_file_num + ", sender_phone='" + req.sender_phone + "', sender_fio='" + req.sender_fio + "', " +
                                 " count_ls=" + req.count_ls + ", sum_charge=" + req.sum_charge + ", date_load=" + (req.date_load == null ? "NULL" : "'" +
                                 req.date_load + "'") + ", file_name='" + req.file_name + "', file_size=" + req.file_size + ", " +
                                 " isclosed=" + req.isclosed + ", dat_closed=" + (req.dat_closed == null ? "NULL" : "'" + req.dat_closed + "'") + ", is_actual=" +
                                 req.is_actual + ", dat_revert=" + (req.dat_revert == null ? "NULL" : "'" + req.dat_revert + "'") + ", nzp_user=null" +
                                 ", " +
                                 " nzp_user_revert=null" +
                                 " WHERE nzp_load = " + req.nzp_load + "; ", null);

                    conn.Execute("UPDATE sys_imports " +
                                 " SET num=" + req.Num + ", filename='" + req.FileName + "', path='" + req.Path +
                                 "', registrationname='" + req.RegistrationName + "', format='" + req.Format + "', " +
                                 " version='" + req.Version + "', result='" + (req.Result == null ? "" : req.Result.Replace("\'", "\'\'")) + "', status='" + req.Status +
                                 "', statusid=" + (int)req.StatusID + ", progress=" + req.progress + ", link='" +
                                 (req.link == null ? "" : req.link.Replace("\'", "\'\'")) + "', " +
                                 " typename='" + req.TypeName + "' ,schema = '" + req.schema + "',AbsolutePath = '" + (req.AbsolutePath ?? "") + "',date_charge = '" + req.date_charge + "' " +
                                 " WHERE nzp_load = " + req.nzp_load + "; ", null);
                });
            }
            finally
            {
                conn.Close();
            }
            return reqList;
        }

        /// <summary>
        /// метод добавления записи о выгрузке 
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public Request Insert(Request req)
        {
            conn.Open();
            try
            {
                req.nzp_load = conn.ExecuteScalar<int>("insert into public.imports(num_load,is_actual,nzp_user) values ((select count(*)+1 from public.imports),1," + req.nzp_user + ") returning nzp_load;", null, null, null, null);
                conn.Execute(
                    "insert into public.sys_imports(nzp_load,num,filename,path,registrationname,format,version,result,status,statusid,progress,link,typename,AbsolutePath,schema,date_charge,gis_loaded_file_regiseter_id ) values " +
                    "(" + req.nzp_load + "," + req.Num + ",'" + req.FileName + "','" + req.Path + "','" +
                    req.RegistrationName + "','" + req.Format + "'," +
                    "" + req.Version + ",'" + req.Result + "','" + req.Status + "'," + (int)req.StatusID + "," +
                    req.progress + ",'" + req.link + "','" + req.TypeName + "','" + req.AbsolutePath + "','" +
                    req.schema + "','" + req.date_charge + "'," + req.GisFileId + ")", null);
            }
            finally
            {
                conn.Close();
            }
            return req;
        }
        /// <summary>
        /// Метод удаления сведений о выгрузке
        /// </summary>
        /// <param name="nzp_load"></param>
        /// <returns></returns>
        public Returns Delete(int nzp_load)
        {
            conn.Open();
            try
            {
                conn.Execute("update public.imports set is_actual = 100 where nzp_load = " + nzp_load, null);
                conn.Execute("delete from public.sys_imports where nzp_load = " + nzp_load, null);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return new Returns();
        }
        /// <summary>
        /// Метод проверки на существование пользователя
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public User CheckUser(string login, string password)
        {
            conn.Open();
            try
            {
                return conn.Query<User>(string.Format("select * from users where trim(login) = trim('{0}') and trim(password) = trim('{1}') and is_actual <> 100", login, password), null).FirstOrDefault();
            }
            finally
            {
                conn.Close();
            }
        }
        /// <summary>
        /// Метод получения всех схем БД
        /// </summary>
        /// <returns></returns>
        public List<string> GetSchemas()
        {
            conn.Open();
            try
            {
                return conn.Query<string>(string.Format("select schema_name from information_schema.schemata where schema_name <> 'information_schema' and schema_name !~ E'^pg_' order by schema_name"), null).ToList();
            }
            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// Сохранение пользователя
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public User SaveUser(User user)
        {
            conn.Open();
            try
            {
                if (user.nzp_user == 0)
                {
                    user.nzp_user = conn.ExecuteScalar<int>("Insert into public.users set (kod_uk,login,password,username,phone,email,roles,is_actual) values " +
                                  " (@kod_uk,@login,@password,@username,@phone,@email,@roles,@is_actual) returning nzp_user", user, null, null, null);
                }
                else
                {
                    conn.Execute("Update public.users set (kod_uk,login,password,username,phone,email,roles,is_actual) = " +
                                        " (@kod_uk,trim(@login),trim(@password),trim(@username),trim(@phone),trim(@email),@roles,@is_actual) where nzp_user = @nzp_user", user);
                }
            }
            finally
            {
                conn.Close();
            }
            return user;
        }

        public string SelectSchema(string schema_code)
        {
            string str;
            conn.Open();
            try
            {
                str = conn.ExecuteScalar<string>("select pref from gkh_pref where upper(id_case) = upper('" + schema_code + "')", null, null, null, null);
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                conn.Close();
            }
            return str;
        }

        public bool CheckGkh(string id_case, string bank, DateTime charge_date)
        {
            var is_kernel = false;
            var is_data = false;
            if (bank.Contains("kernel")) return true;
            conn.Open();
            try
            {
                is_kernel = conn.ExecuteScalar<int>("select count(*) from gkh_check where case_id = '" + id_case +
                                             "' and bank = 'kernel' and charge_date = '" + charge_date + "'", null, null, null, null) > 0;
                if (bank.Contains("data") && is_kernel) return true;
                else if (bank.Contains("data")) return false;
                is_data = conn.ExecuteScalar<int>("select count(*) from gkh_check where case_id = '" + id_case +
                                                "' and bank = 'data' and charge_date = '" + charge_date + "'", null, null, null, null) > 0;
                if (is_data && is_kernel) return true;
                else return false;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                conn.Close();
            }
        }
        
        /// <summary>
        /// Открытие соединения с БД
        /// </summary>
        /// <returns></returns>
        private void OpenConnection()
        {
            try
            {
                switch (conn.State)
                {
                    case ConnectionState.Closed:
                        conn.Open();
                        break;
                    case ConnectionState.Broken:
                        conn.Close();
                        conn.Open();
                        break;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Ошибка при открытии соединения с БД! ", exception);
            }
        }

        /// <summary>
        /// Закрытие соединения с БД
        /// </summary>
        /// <returns></returns>
        private void CloseConnection()
        {
            try
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Ошибка при закрытии соединения с БД!", exception);
            }
        }


        /// <summary>
        /// Выполнение sql-запроса без транзакции
        /// </summary>
        /// <param name="sqlQuery">Текст sql-запроса</param>
        /// <returns></returns>
        public void ExecuteSql(string sqlQuery)
        {
            ExecuteSql(sqlQuery, conn, null, 3600);
        }

        /// <summary>
        /// Создание нового запроса 
        /// </summary>
        /// <param name="commandText">Текст команды</param>
        /// <param name="connection">Соединение</param>
        /// <param name="transaction">Транзакция</param>
        /// <param name="timeout">Таймаут времени выполнения</param>
        private IDbCommand NewDbCommand(string commandText, IDbConnection connection, IDbTransaction transaction,
            int timeout)
        {
            var dbCommand = connection.CreateCommand();
            dbCommand.CommandText = commandText;
            dbCommand.Connection = connection;
            dbCommand.Transaction = transaction;
            dbCommand.CommandTimeout = timeout;
            return dbCommand;
        }

        /// <summary>
        /// Выполнение sql-запроса
        /// </summary>
        /// <param name="sqlQuery">Текст sql-запроса</param>
        /// <param name="connection">Соединение</param>
        /// <param name="transaction">Транзакция</param>
        /// <param name="dbCommandTimeout">Таймаут выполнения</param>
        /// <returns></returns>
        private void ExecuteSql(string sqlQuery, IDbConnection connection, IDbTransaction transaction, int dbCommandTimeout)
        {
            try
            {
                OpenConnection();

                using (IDbCommand myCommand = NewDbCommand(sqlQuery, connection, transaction, dbCommandTimeout))
                {
                    myCommand.ExecuteNonQuery();
                }

            }
            catch (Exception exception)
            {
                throw new Exception(
                    String.Format(" Ошибка при выполнении sql-запроса! \n Текст sql-запроса: {0}", sqlQuery), exception);
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Передача потока напрямую в БД
        /// </summary>
        /// <param name="sqlQuery">Sql-запрос (copy ... from stdin )</param>
        /// <param name="stream">Поток</param>
        public void CopyIn(string sqlQuery, Stream stream)
        {
            OpenConnection();

            var cmd = (NpgsqlCommand) NewDbCommand(sqlQuery, conn, null, defaultDbTimeOut);
            
            var copyIn = new NpgsqlCopyIn(
                cmd,
                (NpgsqlConnection)conn, stream);
            copyIn.Start();
            copyIn.End();
            CloseConnection();
        }
    }
}
